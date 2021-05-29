using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using RssThrottle.Feeds;

namespace RssThrottle.Caching
{
    public class BlobStorageCache : ICache
    {
        private readonly BlobContainerClient _client;

        public BlobStorageCache(BlobContainerClient client)
        {
            _client = client;
        }
        
        public async Task CacheAsync(Parameters parameters, DateTime cacheUntil, string rss)
        {
            await using var ms = new MemoryStream();
            await ms.WriteAsync(Encoding.UTF8.GetBytes(rss));
            ms.Position = 0;
            
            var blobClient = _client.GetBlobClient($"{parameters.Hash}.xml");
            await blobClient.UploadAsync(ms, true);

            var dictionary = new Dictionary<string, string> {{"Expires", cacheUntil.ToString("O")}};
            await blobClient.SetMetadataAsync(dictionary);
        }

        public async Task<string> GetFromCacheAsync(Parameters parameters)
        {
            var blobClient = _client.GetBlobClient($"{parameters.Hash}.xml");

            if (!await blobClient.ExistsAsync())
                return null;

            var properties = await blobClient.GetPropertiesAsync();
            var expires = DateTime.Parse(properties.Value.Metadata["Expires"]);

            if (DateTime.UtcNow > expires)
            {
                await blobClient.DeleteAsync();
                return null;
            }

            var download = await blobClient.DownloadAsync();

            using var reader = new StreamReader(download.Value.Content);
            return await reader.ReadToEndAsync();
        }
    }
}