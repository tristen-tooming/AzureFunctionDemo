using System;
using System.IO;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace emailProcessWorker
{
    public static class queTaskOrchestrator
    {
        [FunctionName("queTaskOrchestrator")]
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            string messageString = context.GetInput<string>();
            EmailPOCO message = JsonConvert.DeserializeObject<EmailPOCO>(messageString); // Validates the message and converts field values to EmailPOCO schema

            // Handle blob
            await context.CallActivityAsync<string>("queTaskOrchestrator_ToBlob", message); // Take just the message string

            // TODO: message to email POCO in function
            string tenAttributes = await context.CallActivityAsync<string>("queTaskOrchestrator_ToMySQL", message);

            if (tenAttributes is not null)
            {
                string parsedMessage = $@"
                “Congratulate!
                We have received following 10 unique attributes from you{tenAttributes}
                Best regards, Millisecond”
                ";
                log.LogInformation(parsedMessage);
                await context.CallActivityAsync<string>("queTaskOrchestratorCongratulateToTable", parsedMessage);
                await context.CallActivityAsync<string>("queTaskOrchestratorCongratulateToBlob", parsedMessage);
            }

            log.LogInformation("Orchestrator task completed");

            // Needed or not?
            string completed = "completed";
            return completed;
        }

        [FunctionName("queTaskOrchestrator_ToBlob")]
        public static string ToBlob(
            [ActivityTrigger] EmailPOCO message,
            [Blob("emails", FileAccess.Write, Connection = "BlobConnector")] BlobContainerClient outputContainer,
            ILogger log)
        {
            string toBlob = null;
            string guid = Guid.NewGuid().ToString();
            log.LogInformation($"Handling account: {message.Email}");
            log.LogInformation($"Working in Blob container: {outputContainer.Name}");

            BlobClient blob = outputContainer.GetBlobClient($"{message.Email}/{message.SendDate}/{guid}.json");
            toBlob = JsonConvert.SerializeObject(message);

            // Setting blob headers (Not working here at least, throws blob does not exists error)
            // BlobHttpHeaders headers = new BlobHttpHeaders{ContentType = "application/json"};
            // blob.SetHttpHeaders(headers);

            // Upload to Blob Storage
            var content = Encoding.UTF8.GetBytes(toBlob);
            using(var ms = new MemoryStream(content))
                blob.Upload(ms);

            return null; // Needed?
        }

        [FunctionName("queTaskOrchestrator_ToMySQL")]
        public static string ToMySQL(
            [ActivityTrigger] EmailPOCO message,
            ILogger log)
        {   
            string tenAttributes = null;
            var sqlConnectionString = Environment.GetEnvironmentVariable("MySQLConnector");

            // MySQL
            foreach (string messageAttribute in message.Attributes)
            {   
                // New connection needs to be opened after ExecuteReader?
                using (var conn = new MySqlConnection(sqlConnectionString))
                {
                    log.LogInformation("Opening MySQL connection");
                    conn.Open();

                    log.LogInformation($"email: {message.Email}, date: {message.SendDate}; Inserting item(s): {message.Attributes}");

                    // To Sql per attribute
                    try
                    {
                        using (var cmd = conn.CreateCommand())
                        {   
                            cmd.CommandText = @"tbl_insert";
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@_SenderKey", message.Key);
                            cmd.Parameters.AddWithValue("@_Email", message.Email);
                            cmd.Parameters.AddWithValue("@_SendDate", message.SendDate);
                            cmd.Parameters.AddWithValue("@_EmailAttribute", messageAttribute);

                            MySqlDataReader reader = cmd.ExecuteReader();

                            // Has 10 attributes
                            if (reader.HasRows)
                            {
                                log.LogInformation($"email: {message.Email}, date: {message.SendDate}; 10 email attributes sent today. Processing them...");
                                while (reader.Read())
                                    {
                                        // Parse string
                                        tenAttributes = tenAttributes + ", " + reader.GetString(0);
                                    }
                            }
                        }
                    }
                    catch (MySqlException ex)
                    {
                        // 1062 is for duplicate data in database by index
                        int errorcode = ex.Number;
                        if (errorcode != 1062)
                        {   
                            // TODO: Throw exception
                            log.LogCritical(errorcode.ToString());
                            log.LogCritical(ex.GetBaseException().ToString());
                        }
                    }
                }
            }
            return tenAttributes;
        }

        [FunctionName("queTaskOrchestratorCongratulateToTable")]
        public static string CongratulateToTable(
            [ActivityTrigger] IDurableActivityContext context,
            ILogger log
        )
        {
            string parsed_message = context.GetInput<string>();
            var sqlConnectionString = Environment.GetEnvironmentVariable("MySQLConnector");

            using (var conn = new MySqlConnection(sqlConnectionString))
            {
                log.LogInformation("Opening MySQL connection");
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO SendEmails VALUES(?parsedEmail)";
                    cmd.Parameters.AddWithValue("?parsedEmail", parsed_message);
                    cmd.ExecuteNonQuery();
                }
            return null;
            }
        }

        [FunctionName("queTaskOrchestratorCongratulateToBlob")]
        public static string CongratulateToBlob(
            [ActivityTrigger] IDurableActivityContext context,
            [Blob("parsedemails", FileAccess.Write, Connection = "BlobConnector")] BlobContainerClient outputContainer,
            ILogger log
        )
        { 
            string parsed_message = context.GetInput<string>();
            string guid = Guid.NewGuid().ToString();
            BlobClient blob = outputContainer.GetBlobClient($"{guid}.txt");

            var content = Encoding.UTF8.GetBytes(parsed_message);
            using(var ms = new MemoryStream(content))
                blob.Upload(ms);

            return null;
        }

        // Starter
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