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

using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using System.Runtime.Serialization.Json;

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
            dynamic message = JsonConvert.DeserializeObject<MessagePOCO>(messageString); // Validates the message and converts field values to EmailPOCO schema

            // Handle blob exists
            await context.CallActivityAsync<string>("queTaskOrchestrator_ToBlob", message); // Take just the message string
            var message_attributes = await context.CallActivityAsync<string>("queTaskOrchestrator_ToMySQL", message);

            if (message_attributes is not null)
            {
                log.LogInformation($"Hitted items: {message_attributes}");
                string parsed_message = $@"
                “Congratulate!\n
                We have received following 10 unique attributes from you {message_attributes}\n
                Best regards, Millisecond”
                ";
                await context.CallActivityAsync<string>("queTaskOrchestrator_parsed_ToTable", parsed_message);
                await context.CallActivityAsync<string>("queTaskOrchestrator_parsed_ToBlob", parsed_message);
            }

            log.LogInformation("Orchestrator task completed");

            // Needed or not?
            string completed = "completed";
            return completed;
        }

        [FunctionName("queTaskOrchestrator_ToBlob")]
        public static string ToBlob(
            [ActivityTrigger] MessagePOCO message,
            [Blob("emails", FileAccess.Write, Connection = "BlobConnector")] BlobContainerClient outputContainer,
            ILogger log)
        {
            string toBlob = null;
            log.LogInformation($"Handling account: {message.Email}");
            log.LogInformation($"Working in Blob container: {outputContainer.Name}");

            // Json list. This list is used later on to add existing content if blob already exists
            List<MessagePOCO> MessageList = new List<MessagePOCO>();
            MessageList.Add(message);
            BlobClient blob = outputContainer.GetBlobClient($"{message.Email}/{message.Date}.json");

            log.LogInformation(blob.AccountName);
            log.LogInformation(blob.Name);
            log.LogInformation(blob.Uri.ToString());

            // Setting blob headers (Not working here at least, throws blob does not exists error)
            // BlobHttpHeaders headers = new BlobHttpHeaders{ContentType = "application/json"};
            // blob.SetHttpHeaders(headers);

            // Blob mangling
            if (blob.Exists())
            {       
                BlobDownloadResult blobData = blob.DownloadContent();
                string blobStringData = blobData.Content.ToString();
                log.LogInformation(blobStringData);
                List<MessagePOCO> blobContent = JsonConvert.DeserializeObject<List<MessagePOCO>>(blobStringData);
                blobContent.AddRange(MessageList); // Adding message in to list
                toBlob = JsonConvert.SerializeObject(blobContent);
                blob.Delete(); // Only way? If blob exists we cannot upload to it. This should be in try catch block and upload old content if error
            }
            else
            {
                toBlob = JsonConvert.SerializeObject(MessageList);
            }

            // Upload to Blob Storage
            var content = Encoding.UTF8.GetBytes(toBlob);
            using(var ms = new MemoryStream(content))
                blob.Upload(ms);

            return null; // Needed?
        }

        [FunctionName("queTaskOrchestrator_ToMySQL")]
        public static string ToMySQL(
            [ActivityTrigger] MessagePOCO message,
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
                log.LogInformation("Opening MySQL connection");
                conn.Open();

                // Connection will be closed by the 'using' block
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {   
                        log.LogInformation($"email: {message.Email}, date: {message.Date}; Inserting item: {message.SingleAttribute}");
                        cmd.CommandText = @"tbl_insert";
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@_SenderKey", message.Key);
                        cmd.Parameters.AddWithValue("@_Email", message.Email);
                        cmd.Parameters.AddWithValue("@_SendDate", message.Date);
                        cmd.Parameters.AddWithValue("@_Milliseconds", message.Milliseconds);
                        cmd.Parameters.AddWithValue("@_EmailAttribute", message.SingleAttribute);

                        MySqlDataReader reader = cmd.ExecuteReader();

                        if (reader.HasRows)
                        {
                            log.LogInformation($"email: {message.Email}, date: {message.Date}; 10 email attributes sent today. Processing them...");
                            while (reader.Read())
                                {
                                    message_attributes = message_attributes + ", " + reader.GetString(0);
                                }
                        }
                        else 
                        {
                            log.LogInformation($"email: {message.Email}, date: {message.Date}; Todays attribute count <10 or >10");
                        }

                    }
                }
                // Needs better error handler
                catch (MySqlException ex)
                {
                    int errorcode = ex.Number;
                    if (errorcode != 1062) // If not duplicate data by database index
                    {
                        log.LogCritical(errorcode.ToString());
                        log.LogCritical(ex.GetBaseException().ToString());
                    }
                    else
                    {
                        log.LogInformation($"email: {message.Email}, date: {message.Date}; item '{message.SingleAttribute}' already in database");
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

        [FunctionName("queTaskOrchestrator_parsed_ToBlob")]
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