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
using Azure.Storage.Blobs;

namespace queWorker
{
    public static class queTaskOrchestrator
    {
        [FunctionName("queTaskOrchestrator")]
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var queueItem = context.GetInput<ServiceBusMessage>();
            await context.CallActivityAsync<string>("queTaskOrchestrator_ToBlob", "Test");

            string completed = "completed";

            return completed;
        }

        [FunctionName("queTaskOrchestrator_ToBlob")]
        public static string ToBlob(
            [ActivityTrigger] IDurableActivityContext context)
        {

            return null;
        }

        [FunctionName("queTaskOrchestratorStarter")]
        public static async Task Run(
            [ServiceBusTrigger("emailque", Connection = "ServiceBusConnector")] ServiceBusMessage queItem,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            //log.LogInformation($"Working with message: {messageId}");

            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync<ServiceBusMessage>("queTaskOrchestrator", null, queItem);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }
    }
}