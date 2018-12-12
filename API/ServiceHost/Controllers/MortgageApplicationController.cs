using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using DocExtract.API.ServiceHost.Helpers;
using DocExtract.API.ServiceHost.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DocExtract.API.ServiceHost.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowAll")]
    public class MortgageApplicationController : ControllerBase
    {
        BlobRequestOptions _retryPolicy = new BlobRequestOptions() { RetryPolicy = new Microsoft.WindowsAzure.Storage.RetryPolicies.ExponentialRetry(TimeSpan.FromMilliseconds(500), 5) };
        string _storageAccountName;

        public MortgageApplicationController(IConfiguration configuration)
        {
            _storageAccountName = configuration["DocumentStorageAccount"];
        }

        [HttpGet("{mortgageApplicationId}")]
        public async Task<ActionResult<MortgageApplication>> GetMortgageApplication(string mortgageApplicationId, CancellationToken cancellationToken)
        {
            var blobs = await StorageHelper.GetBlobs(_storageAccountName, "rawdocuments", $"{mortgageApplicationId.ToLower()}/");

            MortgageApplication mortgageApplication = new MortgageApplication() { MortgageApplicationId = mortgageApplicationId, Documents = new List<MortgageApplicationDocument>() };

            if (blobs.SafeAny())
            {
                foreach (var blob in blobs)
                {
                    MortgageApplicationDocument doc = new MortgageApplicationDocument();
                    doc.PopuplateFromBlobProperties(blob);
                    string documentId = doc.DocumentId;
                    CloudBlockBlob finalBlob = await StorageHelper.GetBlob(_storageAccountName, "parseddocuments", $"final/{doc.MortgageApplicationId}/{documentId}");

                    MortgageApplicationDocument finalDoc = Newtonsoft.Json.JsonConvert.DeserializeObject<MortgageApplicationDocument>(await finalBlob.DownloadTextAsync());
                    mortgageApplication.Documents.Add(finalDoc);

                    var markedupBlob = await StorageHelper.GetBlob(_storageAccountName, "parseddocuments", $"markedup/{mortgageApplicationId.ToLower()}/{documentId}");

                    finalDoc.HasParsedResults = true;
                    if (await markedupBlob.ExistsAsync())
                    {
                        finalDoc.HasMarkedupResults = true;
                    }
                }
            }
            else
            {
                return NotFound();
            }

            return Ok(mortgageApplication);
        }

        [HttpGet()]
        public async Task<ActionResult<List<MortgageApplication>>> GetAllMortgageApplication(CancellationToken cancellationToken)
        {
            List<MortgageApplication> mortgageApplications = new List<MortgageApplication>();
            var dirs = await StorageHelper.GetBlobDirectories(_storageAccountName, "rawdocuments");

            foreach ( var dir in dirs.OrderBy(o => o.Prefix) )
            {
                mortgageApplications.Add(new MortgageApplication() {  MortgageApplicationId = dir.Prefix.Replace("final/", "").Trim('/') });
            }

            return mortgageApplications;
        }
    }
}
