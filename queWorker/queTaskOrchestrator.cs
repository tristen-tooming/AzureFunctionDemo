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

using System.Data;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data.Common;
using System.Data.SqlClient;

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
            await context.CallActivityAsync<string>("queTaskOrchestrator_ToBlob", message); // Take just the message string
            var message_attributes = await context.CallActivityAsync<string>("queTaskOrchestrator_ToMySQL", message);

            if (message_attributes is not null)
            {
                string parsed_message = $@"
                “Congratulate!\n
                We have received following 10 unique attributes from you: {message_attributes}\n
                Best regards, Millisecond”
                ";
                await context.CallActivityAsync<string>("queTaskOrchestrator_parsed_ToTable", parsed_message);
                await context.CallActivityAsync<string>("queTaskOrchestrator_ToTable", parsed_message);
            }

            log.LogInformation("Orchestrator task completed");

            // Needed or not?
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
        public static string ToMySQL(
            [ActivityTrigger] EmailPOCO message,
            ILogger log)
        {   
            string message_attributes = null;
            var sqlConnectionString = Environment.GetEnvironmentVariable("MySQLConnector");

            // MySQL
            /* TODO: Implement try catch block. 
            If duplicate entry: System.Private.CoreLib: Exception while executing function: testSQL. MySql.Data: Duplicate entry 'banana-2020-02-02-10' for key 'emailattributes.idx_email_attributes_all'.
            */
            using (var conn = new MySqlConnection(sqlConnectionString))
            {
                log.LogInformation("Opening connection");
                conn.Open();

                // Connection will be closed by the 'using' block
                using (var cmd = conn.CreateCommand())
                {   
                    cmd.CommandText = @"tbl_insert";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@_SenderKey", message.Key);
                    cmd.Parameters.AddWithValue("@_Email", message.Email);
                    cmd.Parameters.AddWithValue("@_SendDate", message.Date);
                    cmd.Parameters.AddWithValue("@_Milliseconds", message.Milliseconds);
                    cmd.Parameters.AddWithValue("@_EmailAttribute", "");

                    MySqlDataReader reader = cmd.ExecuteReader();

                    log.LogInformation($"Reader has rows: {reader.HasRows}");
                    if (reader.HasRows)
                    {
                        log.LogInformation("10 email attributes send today. Processing them...");
                        while (reader.Read())
                            {
                                message_attributes = message_attributes + ", " + reader.GetString(0);
                            }
                    }
                    else 
                    {
                        log.LogInformation("Todays attribute count <10 or >10");
                    }

                }
            }
            return message_attributes;
        }

        [FunctionName("queTaskOrchestrator_parsed_ToTable")]
        public static string ParsedToTable(
            [ActivityTrigger] string parsed_message,
            ILogger log
        )
        {
            return null;
        }

        [FunctionName("queTaskOrchestrator_parsed_ToTable")]
        public static string ParsedToBlob(
            [ActivityTrigger] string parsed_message,
            ILogger log
        )
        {
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