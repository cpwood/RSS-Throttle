using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RssThrottle;
using Azure.Storage.Blobs;
using System.Net;

namespace CWood
{
    public static class feed
    {
        [FunctionName("feed")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Received RSS request");

            var qs = req.QueryString.Value;
            log.LogInformation(qs);

            var blobServiceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("RSS_CONN"));
            var containerClient = blobServiceClient.GetBlobContainerClient("rss");

            var service = new FeedService(new BlobStorageCache(containerClient));
            var parameters = new Parameters();
            parameters.Parse(qs);
            var result = await service.ProcessAsync(parameters);
            
            return new ContentResult
            {
                Content = result,
                ContentType = "application/rss+xml",
                StatusCode = (int)HttpStatusCode.OK
            };
        }
    }
}
