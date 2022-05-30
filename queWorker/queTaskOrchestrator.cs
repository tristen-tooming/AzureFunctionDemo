using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;


using System.IO;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.ServiceBus;
using Azure.Storage.Blobs;
using Newtonsoft.Json;

using System.Configuration;
using Microsoft.Azure.Storage;

namespace queWorker
{
    public static class queTaskOrchestrator
    {
        [FunctionName("queTaskOrchestrator")]
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var data_string = context.GetInput<string>();

            log.LogInformation($"Orchestrated message '{queueItem}'.");
            await context.CallActivityAsync<string>("queTaskOrchestrator_ToBlob", null, data_string);

            string completed = "completed";

            return completed;
        }

        [FunctionName("queTaskOrchestrator_ToBlob")]
        public static string ToBlob(
            [ActivityTrigger] IDurableActivityContext context)
            // [Blob("samples/{name}", FileAccess.Write)] Stream myBlob) // Can we use name from the context.GetInput<Data>?.Name
            //http://dontcodetired.com/blog/post/Understanding-Azure-Durable-Functions-Part-6-Activity-Functions-with-Additional-Input-Bindings
        {
            var data_string = context.GetInput<string>();

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            client = storageAccount.CreateCloudBlobClient();
            container = client.GetContainerReference("Demo");
            await container.CreateIfNotExistsAsync();
            blob = container.GetBlockBlobReference(name);
            blob.Properties.ContentType = "application/json";
            blob.UploadFromStreamAsync(new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))); // Use <>
            
            return null;
        }

        [FunctionName("queTaskOrchestratorStarter")]
            public static async Task Run(
            // Message message should be used because Json Serializer is build in and we can use 
            // ApplicationProperties to include other parameters
            [ServiceBusTrigger("emailque", Connection = "ServiceBusConnector")] string message, 
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            //log.LogInformation($"Working with message: {messageId}");
            //var msg = JsonConvert.DeserializeObject(message.Body);
            log.LogInformation(message);

            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync<string>("queTaskOrchestrator", null, message);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }
    }
}