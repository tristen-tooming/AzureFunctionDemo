using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

using System;
using System.IO;
using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage;
using Newtonsoft.Json;

using System.Configuration;

namespace queWorker
{
    public static class queTaskOrchestrator
    {
        [FunctionName("queTaskOrchestrator")]
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var messageString = context.GetInput<string>();
            dynamic message = JsonConvert.DeserializeObject<EmailPOCO>(messageString); // Validates the message and converts field values to EmailPOCO schema

            log.LogInformation($"Orchestrated message '{message.Email}'.");
            await context.CallActivityAsync<string>("queTaskOrchestrator_ToBlob", message);

            string completed = "completed";

            return completed;
        }

        [FunctionName("queTaskOrchestrator_ToBlob")]
        // TODO: just pass string to the function and upload it
        public static string ToBlob(
            [ActivityTrigger] EmailPOCO message,
            [Blob("emails", FileAccess.Write, Connection = "BlobConnector")] BlobContainerClient outputContainer,
            ILogger log)
        {
            log.LogInformation($"Handling account: {message.Email}");
            log.LogInformation($"Working in Blob container: {outputContainer.Name}");
            // TODO: Log for each day and email
            BlobClient blob = outputContainer.GetBlobClient($"{message.Email}_{DateTime.Today.ToString()}.json");
            var content = Encoding.UTF8.GetBytes(message.Email);
            using(var ms = new MemoryStream(content))
                blob.Upload(ms);
            
            return null; // Needed?
        }

        [FunctionName("queTaskOrchestrator_ToMySQL")]
        public static string ToBlob(
            [ActivityTrigger] EmailPOCO message,
            [Blob("emails", FileAccess.Write, Connection = "BlobConnector")] BlobContainerClient outputContainer,
            ILogger log)
        {   
            return null; // Needed?
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