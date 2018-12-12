using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;
using DocExtract.API.ServiceHost.Helpers;
using DocExtract.API.ServiceHost.Models;
using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace DocExtract.API.ServiceHost.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowAll")]
    public class DocumentController : ControllerBase
    {
        BlobRequestOptions _retryPolicy = new BlobRequestOptions() { RetryPolicy = new Microsoft.WindowsAzure.Storage.RetryPolicies.ExponentialRetry(TimeSpan.FromMilliseconds(500), 5) };
        string _storageAccountName;

        public DocumentController(IConfiguration configuration)
        {
            _storageAccountName = configuration["DocumentStorageAccount"];
        }

        [HttpPost("{mortgageApplicationId}"), DisableRequestSizeLimit]
        public async Task<ActionResult<UploadDocumentResult>> UploadDocument(IFormFile file, string mortgageApplicationId, CancellationToken cancellationToken)
        {
            UploadDocumentResult result = new UploadDocumentResult();
            if (mortgageApplicationId.IsEmpty())
            {
                result.Message = "Mortgage Application Id not specified";
                return result;
            }

            if (file == null || file.FileName.IsEmpty())
            {
                result.Message = "No Files Uploaded";
                return result;
            }

            MortgageApplicationDocument fileResult = new MortgageApplicationDocument();

            fileResult.FileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
            fileResult.DocumentId = Guid.NewGuid().ToString("n");
            fileResult.MortgageApplicationId = mortgageApplicationId;
            fileResult.DateUploaded = DateTimeOffset.UtcNow;
            var blob = await StorageHelper.GetBlob(_storageAccountName, "rawdocuments", $"{mortgageApplicationId.ToLower()}/{fileResult.DocumentId}");
            blob.Metadata.Add("MortgageApplicationId", mortgageApplicationId);
            blob.Metadata.Add("FileName", fileResult.FileName);
            blob.Metadata.Add("CreateTime", fileResult.DateUploaded.ToString());
            blob.Metadata.Add("DocumentId", fileResult.DocumentId);
            blob.Properties.ContentType = file.ContentType;

            using (var fileStream = file.OpenReadStream())
            {
                await blob.UploadFromStreamAsync(fileStream, null, _retryPolicy, null, cancellationToken);
                result.FilesUploaded.Add(fileResult);
            }

            result.Succeeded = true;
            return result;
        }

        [HttpGet("{mortgageApplicationId}/{documentId}")]
        public async Task<IActionResult> GetDocumentStatus(string mortgageApplicationId, string documentId, CancellationToken cancellationToken)
        {
            var blob = await StorageHelper.GetBlob(_storageAccountName, "rawdocuments", $"{mortgageApplicationId.ToLower()}/{documentId}");

            if (!(await blob.ExistsAsync()))
            {
                return NotFound();
            }

            MortgageApplicationDocument doc = new MortgageApplicationDocument();
            doc.PopuplateFromBlobProperties(blob);

            var finalBlob = await StorageHelper.GetBlob(_storageAccountName, "parseddocuments", $"final/{mortgageApplicationId.ToLower()}/{documentId}");

            if (await finalBlob.ExistsAsync())
            {
                doc.Status = MortgageApplicationStatus.Processed;
                doc.HasParsedResults = true;
            }

            var markedupBlob = await StorageHelper.GetBlob(_storageAccountName, "parseddocuments", $"markedup/{mortgageApplicationId.ToLower()}/{documentId}");

            if (await markedupBlob.ExistsAsync())
            {
                doc.HasMarkedupResults = true;
            }

            return Ok(doc);
        }

        [HttpGet("raw/{mortgageApplicationId}/{documentId}")]
        public async Task<IActionResult> GetByMortgageAndDocument(string mortgageApplicationId, string documentId, CancellationToken cancellationToken)
        {
            string blobName = $"{mortgageApplicationId.ToLower()}/{documentId}";
            var blob = await StorageHelper.GetBlob(_storageAccountName, "rawdocuments", blobName);
            if (await blob.ExistsAsync())
            {
                await blob.FetchAttributesAsync();
                return File(await blob.OpenReadAsync(), blob.Properties.ContentType);
            }

            return NotFound();
        }

        [HttpGet("markedup/{mortgageApplicationId}/{documentId}")]
        public async Task<IActionResult> GetMarkedupByMortgageAndDocument(string mortgageApplicationId, string documentId, CancellationToken cancellationToken)
        {
            string blobName = $"markedup/{mortgageApplicationId.ToLower()}/{documentId}";
            var blob = await StorageHelper.GetBlob(_storageAccountName, "parseddocuments", blobName);
            if (await blob.ExistsAsync())
            {
                await blob.FetchAttributesAsync();
                return File(await blob.OpenReadAsync(), blob.Properties.ContentType);
            }

            return NotFound();
        }

    }
}