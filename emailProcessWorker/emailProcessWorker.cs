using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using System.IO;
using Microsoft.AspNetCore.Http;

/* 
Best example:
https://github.com/mspnp/cloud-design-patterns/blob/master/async-request-reply/src/AsyncProcessingWorkAcceptor.cs

When used EmailPOCO email in HttpTrigger line it returned List as null
*/

namespace emailProcessWorker
{
    public static class emailProcessWorker
    {
        [FunctionName("emailProcessWorker")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            [ServiceBus("emailque", Connection = "ServiceBusConnector")] IAsyncCollector<ServiceBusMessage> OutMessages,
            ILogger log)
        {
            log.LogInformation("EmailProcessorWorker Started");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject<EmailPOCO>(requestBody); // Validates the message and converts field values to EmailPOCO schema
            string messagePayload = JsonConvert.SerializeObject(data);

            var message = new ServiceBusMessage(messagePayload);
            message.ApplicationProperties["RequestSubmittedAt"] = DateTime.Now;
                
            log.LogInformation($"Send message {messagePayload}");
            await OutMessages.AddAsync(message);

            return (ActionResult) new AcceptedResult();
        }
    }
}