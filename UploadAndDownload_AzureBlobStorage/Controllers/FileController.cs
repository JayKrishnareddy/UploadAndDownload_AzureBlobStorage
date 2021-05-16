using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace UploadAndDownload_AzureBlobStorage.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public FileController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost(nameof(UploadFile))]
        public async Task<IActionResult> UploadFile(IFormFile files)
        {
            string systemFileName = files.FileName;
            string blobstorageconnection = _configuration.GetValue<string>("BlobConnectionString");
            // Retrieve storage account from connection string.
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);
            // Create the blob client.
            CloudBlobClient blobClient = cloudStorageAccount.CreateCloudBlobClient();
            // Retrieve a reference to a container.
            CloudBlobContainer container = blobClient.GetContainerReference(_configuration.GetValue<string>("BlobContainerName"));
            // This also does not make a service call; it only creates a local object.
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(systemFileName);
            await using (var data = files.OpenReadStream())
            {
                await blockBlob.UploadFromStreamAsync(data);
            }
            return Ok("File Uploaded Successfully");
        }
        [HttpPost(nameof(DownloadFile))]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            CloudBlockBlob blockBlob;
            await using (MemoryStream memoryStream = new MemoryStream())
            {
                string blobstorageconnection = _configuration.GetValue<string>("BlobConnectionString");
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);
                CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(_configuration.GetValue<string>("BlobContainerName"));
                blockBlob = cloudBlobContainer.GetBlockBlobReference(fileName);
                await blockBlob.DownloadToStreamAsync(memoryStream);
            }

            Stream blobStream = blockBlob.OpenReadAsync().Result;
            return File(blobStream, blockBlob.Properties.ContentType, blockBlob.Name);
        }
        [HttpDelete(nameof(DeleteFile))]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            string blobstorageconnection = _configuration.GetValue<string>("BlobConnectionString");
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);
            CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            string strContainerName = _configuration.GetValue<string>("BlobContainerName");
            CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(strContainerName);
            var blob = cloudBlobContainer.GetBlobReference(fileName);
            await blob.DeleteIfExistsAsync();
            return Ok("File Deleted");
        }
    }
}
