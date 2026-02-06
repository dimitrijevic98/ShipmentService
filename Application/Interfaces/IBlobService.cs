using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IBlobService
    {
        Task UploadAsync(string blobName, Stream data, string contentType);
        Task<Stream> DownloadAsync(string blobName);
        public Task DeleteIfExistsAsync(string blobName);
    }
}