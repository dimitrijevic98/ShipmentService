using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Azure
{
    public class BlobService : IBlobService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly ILogger<BlobService> _logger;
        public BlobService(BlobServiceClient client, IConfiguration config, ILogger<BlobService> logger)
        {
            _containerClient = client.GetBlobContainerClient(config["Azure:BlobStorage:ContainerName"]);
            
            _logger= logger;
        }

        public async Task UploadAsync(string blobName, Stream data, string contentType)
        {
            try
            {
                _logger.LogInformation("Uploading blob {BlobName}", blobName);

                await _containerClient.CreateIfNotExistsAsync();

                var blobClient = _containerClient.GetBlobClient(blobName);

                await blobClient.UploadAsync(data, new BlobHttpHeaders { ContentType = contentType });

                _logger.LogInformation("Blob {BlobName} uploaded successfully", blobName);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error uploading blob {BlobName}", blobName);

                throw;
            }            
        }

        public async Task<Stream> DownloadAsync(string blobName)
        {
            try
            {
                _logger.LogInformation("Downloading blob {BlobName}", blobName);

                var blobClient = _containerClient.GetBlobClient(blobName);

                var response = await blobClient.DownloadStreamingAsync();

                _logger.LogInformation("Blob {BlobName} downloaded", blobName);

                return response.Value.Content;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error downloading blob {BlobName}", blobName);

                throw;
            }
        }

        public Task DeleteIfExistsAsync(string blobName)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            blobClient.DeleteIfExistsAsync();
           
            return Task.CompletedTask;
        }
    }
}