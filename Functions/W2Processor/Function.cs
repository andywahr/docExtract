using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using DocExtract.Functions.Common;
using DocExtract.Functions.Common.Models;

namespace DocExtract.Functions.W2Processor
{
    public static class Function
    {
        [FunctionName("W2Processor")]
        public static async Task Run([BlobTrigger("parseddocuments/w2/{name}", Connection = "Documents")]CloudBlockBlob parsedBlob,
                                     [Blob("parseddocuments", Connection = "Documents")]CloudBlobContainer blobDirectory,
                                     [Blob("rawdocuments/{name}", FileAccess.Read)]Stream originalImage,
                                     [Blob("parseddocuments/markedup/{name}", FileAccess.ReadWrite)]CloudBlockBlob markedUpImage,
                                      ILogger log, Microsoft.Azure.WebJobs.ExecutionContext context, CancellationToken cancellationToken)
        {
            try
            {
                MortgageApplicationDocument mortgageApplicationDocument = new MortgageApplicationDocument();
                mortgageApplicationDocument.PopuplateFromBlobProperties(parsedBlob);
                mortgageApplicationDocument.HasParsedResults = true;
                mortgageApplicationDocument.DocumentType = "w2";

                string jsonContents = await parsedBlob.DownloadTextAsync();
                ProcessedDocument document = JsonConvert.DeserializeObject<ProcessedDocument>(jsonContents);

                CloudBlockBlob finalBlob = blobDirectory.GetBlockBlobReference(parsedBlob.Name.Replace("w2/", "final/"));
                mortgageApplicationDocument.Status = MortgageApplicationStatus.Processed;
                mortgageApplicationDocument.DateProcessed = DateTimeOffset.UtcNow;
                mortgageApplicationDocument.SetBlobProperties(finalBlob);

                FindDataLine(document, "SSN", "Social Security Number", mortgageApplicationDocument, "a. Employee's social security number", "social security number", "Employee's soc. sec. number", "Employee's social security number");
                FindMoneyLine(document, "Wages", "Total Wages", mortgageApplicationDocument, "Wages");
                FindMoneyLine(document, "FedTax", "Federal Tax Witholding", mortgageApplicationDocument, "2. Federal income tax", "Federal income tax withheld");
                FindDataLine(document, "CopyType", "Copy Type", mortgageApplicationDocument, "Copy");
                FindMoneyLine(document, "SocialSecurityWage", "Social Security Wage", mortgageApplicationDocument, "Social security wages");
                FindMoneyLine(document, "SocialSecurityTax", "Social Security Tax", mortgageApplicationDocument, "Social security tax withheld");
                FindMoneyLine(document, "MedicareWage", "Medicare Wage", mortgageApplicationDocument, "Medicare wages and tips");
                FindMoneyLine(document, "MedicareTax", "Medicare Tax", mortgageApplicationDocument, "Medicare tax withheld");
                FindMoneyLine(document, "SocialSecurityTips", "Social Security Tips", mortgageApplicationDocument, "Social security tips");
                FindDataLine(document, "EmployerDetails", "Employer Details", mortgageApplicationDocument, "Employer's name, address, and ZIP code");
                FindDataLine(document, "EmployeeDetails", "Employee Details", mortgageApplicationDocument, "Employee's first name and initial", "Last name", "Employee's name, address, and ZIP code", "Employee's name (first, middle indial, last)");

                await finalBlob.UploadTextAsync(JsonConvert.SerializeObject(mortgageApplicationDocument));
                await MarkupService.HighlightHits(document, originalImage, markedUpImage);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "W2 Processor Failed");
            }
        }

        static Regex CLEANUP = new Regex("[\\s'.,]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static Regex CLEANUPMONEY = new Regex("[\\s',]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static DataField FindMoneyLine(ProcessedDocument document, string name, string title, MortgageApplicationDocument mortgageApplication, params string[] titlesToFind)
        {
            DataField dataField = FindDataLine(document, name, title, mortgageApplication, titlesToFind);

            if (dataField != null)
            {
                double tmpVal;
                if (double.TryParse(CLEANUPMONEY.Replace(dataField.Value, ""), out tmpVal))
                {
                    dataField.Value = tmpVal.ToString("c");
                }
            }

            return dataField;
        }

        private static DataField FindDataLine(ProcessedDocument document, string name, string title, MortgageApplicationDocument mortgageApplication, params string[] titlesToFind)
        {
            ProcessedLine foundLine = null;

            foreach (var titleToFind in titlesToFind)
            {
                string cleanTitle = CLEANUP.Replace(titleToFind, "");
                foundLine = document.Lines.Where(l => CLEANUP.Replace(l.Text, "").IndexOf(cleanTitle, StringComparison.OrdinalIgnoreCase) > -1).FirstOrDefault();

                if (foundLine != null)
                {
                    break;
                }
            }

            var dataLine = foundLine?.BoundingBox.FindClosestsBelow(document.Lines) ?? null;

            if (dataLine == null)
            {
                return null;
            }

            DataField dataField = new DataField()
            {
                FieldName = name,
                FieldTitle = title,
                LabelBox = foundLine.BoundingBox,
                ValueBox = dataLine.BoundingBox,
                Value = dataLine.Text
            };

            if (mortgageApplication.DataFields == null) mortgageApplication.DataFields = new List<DataField>();

            mortgageApplication.DataFields.Add(dataField);

            return dataField;
        }
    }
}
