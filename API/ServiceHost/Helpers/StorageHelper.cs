using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocExtract.API.ServiceHost.Helpers
{
    public class StorageHelper
    {
        public static async Task<CloudBlobContainer> GetContainer(string storageAccountConnectionString, string containerName)
        {            
            CloudStorageAccount sa = CloudStorageAccount.Parse(storageAccountConnectionString);
            CloudBlobClient blobClient = sa.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            await container.CreateIfNotExistsAsync();
            return container;
        }

        public static async Task<CloudBlockBlob> GetBlob(string storageAccountName, string containerName, string blobName)
        {
            var container = await GetContainer(storageAccountName, containerName);
            return container.GetBlockBlobReference(blobName);
        }

        public static async Task<string> GetAccessTokenAsync()
        {
            var tokenProvider = new AzureServiceTokenProvider();
            return await tokenProvider.GetAccessTokenAsync("https://storage.azure.com/");
        }

        public static async Task<List<Cloud​Blob​Directory>> GetBlobDirectories(string storageAccountName, string containerName, string prefix = "")
        {
            var container = await StorageHelper.GetContainer(storageAccountName, containerName);
            var blobList = await container.ListBlobsSegmentedAsync(prefix, false, BlobListingDetails.None, int.MaxValue, null, null, null);

            return (from blob in blobList
                                 .Results
                                 .OfType<CloudBlobDirectory>()
                    select blob).ToList();
        }

        public static async Task<List<CloudBlockBlob>> GetBlobs(string storageAccountName, string containerName, string prefix = "")
        {
            var container = await StorageHelper.GetContainer(storageAccountName, containerName);
            var blobList = await container.ListBlobsSegmentedAsync(prefix, false, BlobListingDetails.Metadata, int.MaxValue, null, null, null);

            return (from blob in blobList
                                 .Results
                                 .OfType<CloudBlockBlob>()
                    select blob).ToList();
        }
    }
}
