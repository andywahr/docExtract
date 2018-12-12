using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using DocExtract.Functions.Dispatcher.Models;
using System.Linq;
using DocExtract.Functions.Common.Models;

namespace DocExtract.Functions.Dispatcher
{
    public static class IdentifyDocumentFunction
    {
        [FunctionName("IdentifyDocument")]
        public static async Task Run([BlobTrigger("rawdocuments/{name}", Connection = "Documents")]CloudBlockBlob fileBlob, 
                                     [Blob("parseddocuments", Connection = "Documents")]CloudBlobContainer parsedDirectory,
                                      ILogger log, Microsoft.Azure.WebJobs.ExecutionContext context, 
                                      CancellationToken cancellationToken)
        {
            try
            {
                if (fileBlob.Metadata.ContainsKey("DocumentType"))
                {
                    //skip already processed documents
                    return;
                }

                var config = new ConfigurationBuilder()
                            .SetBasePath(context.FunctionAppDirectory)
                            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                            .AddEnvironmentVariables()
                            .Build();

                string predictionKey = config["PredictionKey"];
                string predictionProjectId = config["PredictionProjectId"];
                string cognativeKey = config["CognativeKey"];
                double minimumPrecision = double.Parse(config["MinimalPrecision"]);
                string computerVisionServiceHostName = config["ComputerVisionServiceHostName"];
                string customVisionServiceHostName = config["CustomVisionServiceHostName"];

                string documentURLWithSAS = GetBlobSasUri(fileBlob);
                string tagName = await AnalyseImageAsync(documentURLWithSAS, predictionKey, predictionProjectId, minimumPrecision, customVisionServiceHostName, cancellationToken);

                if (!tagName.IsEmpty())
                {
                    MortgageApplicationDocument mortgageApplicationDocument = new MortgageApplicationDocument();
                    mortgageApplicationDocument.PopuplateFromBlobProperties(fileBlob);
                    mortgageApplicationDocument.DateParsed = DateTimeOffset.UtcNow;

                    var parsedResults = await ParseDocumentTextAsync(documentURLWithSAS, cognativeKey, computerVisionServiceHostName, mortgageApplicationDocument.FileName, cancellationToken);
                    string parsedFileName = $"{tagName}/{fileBlob.Name.Trim('/')}";

                    CloudBlockBlob blob = parsedDirectory.GetBlockBlobReference(parsedFileName);
                    mortgageApplicationDocument.SetBlobProperties(blob);
                    blob.Properties.ContentType = "application/json";

                    await blob.UploadTextAsync(JsonConvert.SerializeObject(parsedResults));
                }
                else
                {
                    tagName = "N/A";
                }

                fileBlob.Metadata.Add("DocumentType", tagName);
                await fileBlob.SetMetadataAsync();
            }
            catch ( Exception ex )
            {
                log.LogError($"Identity Document failed: {ex.ToString()}");
            }
        }

        private static async Task<string> AnalyseImageAsync(string documentURL, string predictionKey, string predictionProjectId, double minimumPrecision, string serviceHostName, CancellationToken cancellationToken)
        {
            // Request headers
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Prediction-Key", predictionKey);

            // Request parameters
            var uri = $"https://{serviceHostName}/customvision/v2.0/Prediction/{predictionProjectId}/url";

            HttpResponseMessage response;

            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes("{'Url':'" + documentURL + "'}");

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var prediction = await response.Content.ReadAsAsync<ImagePredictions>();

                    if ( prediction != null && prediction.Predictions.SafeAny() )
                    {
                        var possibilites = prediction.Predictions.Where(p => p.Probability >= minimumPrecision);
                         
                        if (possibilites.SafeAny() )
                        {
                            if (possibilites.Count() > 1)
                            {
                                // need to do some logging/error handling
                                return possibilites.OrderByDescending(p => p.Probability).First().TagName;
                            }
                            else
                            {
                                return possibilites.First().TagName;
                            }
                        }
                    }
                }
            }

            return string.Empty;
        }

        private static string GetBlobSasUri(CloudBlockBlob blob, string policyName = null)
        {
            string sasBlobToken;

            if (policyName == null)
            {
                // Create a new access policy and define its constraints.
                // Note that the SharedAccessBlobPolicy class is used both to define the parameters of an ad-hoc SAS, and
                // to construct a shared access policy that is saved to the container's shared access policies.
                SharedAccessBlobPolicy adHocSAS = new SharedAccessBlobPolicy()
                {
                    // When the start time for the SAS is omitted, the start time is assumed to be the time when the storage service receives the request.
                    // Omitting the start time for a SAS that is effective immediately helps to avoid clock skew.
                    SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                    Permissions = SharedAccessBlobPermissions.Read /*| SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.Create */
                };

                // Generate the shared access signature on the blob, setting the constraints directly on the signature.
                sasBlobToken = blob.GetSharedAccessSignature(adHocSAS);

                //Console.WriteLine("SAS for blob (ad hoc): {0}", sasBlobToken);
                //Console.WriteLine();
            }
            else
            {
                // Generate the shared access signature on the blob. In this case, all of the constraints for the
                // shared access signature are specified on the container's stored access policy.
                sasBlobToken = blob.GetSharedAccessSignature(null, policyName);

                //Console.WriteLine("SAS for blob (stored access policy): {0}", sasBlobToken);
                //Console.WriteLine();
            }

            // Return the URI string for the container, including the SAS token.
            return blob.Uri + sasBlobToken;
        }

        private static async Task<ProcessedDocument> ParseDocumentTextAsync(string documentURL, string APIKey, string serviceHostName, string documentName, CancellationToken cancellationToken)
        {
            // Request headers
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", APIKey);

            // Request parameters
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["mode"] = "Printed";
            var uri = $"https://{serviceHostName}/vision/v2.0/recognizeText?" + queryString;

            HttpResponseMessage response;

            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes("{'url':'" + documentURL + "'}");

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content, cancellationToken);

                string locationURL = string.Empty;
                if (response.IsSuccessStatusCode)
                {
                    if (response.Headers.TryGetValues("Operation-Location", out IEnumerable<string> values))
                    {
                        IEnumerator<string> e = values.GetEnumerator();
                        e.MoveNext();
                        locationURL = e.Current;
                    }
                }

                if (locationURL == "")
                {
                    System.Console.WriteLine("Error: could not retrieve async. operation URL");
                    return null;
                }

                bool isOperationCompleted = false;
                int count = 0;

                while ((!isOperationCompleted) && (count < 10))
                {
                    response = await client.GetAsync(locationURL, HttpCompletionOption.ResponseContentRead, cancellationToken);
                    if (!response.IsSuccessStatusCode)
                    {
                        System.Console.WriteLine("Error: " + response.ToString());
                        return null;
                    }

                    string responsePayload = await response.Content.ReadAsStringAsync();
                    dynamic dynObj = JsonConvert.DeserializeObject(responsePayload);
                    string status = dynObj.status;

                    if (!string.Equals(status, "Succeeded"))
                    {
                        await Task.Delay(5000);
                        count++;
                        continue;
                    }

                    return new ProcessedDocument(documentName, dynObj.recognitionResult);
                }
            }

            return null;
        }
    }
}
