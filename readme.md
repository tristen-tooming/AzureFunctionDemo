## Usage / Config

- There exists a Postman collection for integration tests
- Technologies:
    - Azure Functions: v4
    - MySQL: 8
    - .NEt: 6 (in-process)
- Scripts for initializing MySQL in folder `.\sqlCommands`
- In blob we need container `emails` and `parsedemails` (Should just check the existence in the code)

Into appliation settings:
```Json
        "ServiceBusConnector": "Endpoint=;SharedAccessKeyName=;SharedAccessKey=;EntityPath=",

        "BlobConnector": "DefaultEndpointsProtocol=https;AccountName=;EndpointSuffix=core.windows.net",

        "MySQLConnector": "Server=;Port=3309;Database=;Uid=user@database;Pwd=;"
```

#### emailProcessWorker

Takes in body:
```Json
{
    "Key": "",
    "Email": "",
    "Attributes": ["", "", ""]
}
```

Returns `202`

#### V1 vs V2
V1 inserts message per attribute to the Service Bus and V2 insert full message to the bus.

V1
- Milliseconds key can be used in the blob naming
- Uses more resources
- Messages are in blob per revieved attribute
- Can this be quicker? -- Connections are opened in pararrel, but REST API endpoint is then slower

V2
- Less resources and quicker
- Messages are in the blob as recieved

#### TODO:
- Simplify code: Functions in to the same folder because we use the `MessagePOCO`?
- Check if DATE() can be removed from MySQL scripts
- API endpoint works quicker because no loop

## Implementation
#### EmailAPI
- Runs as in-process
- Uses async pattern to send message to the que and http response to the user
- Attributes list is decostructed and parsed into single messages. This way we know which element was first to handle per timestamp

#### Service Bus Orchestrator

Input trigger from Service Bus orchestrates following functions:

- _func_ ToBlob `EmailPOCO` -> `null`
    - Creates folder structure into emails container: `{email}/{date}/{guid}.json` per message
- _func_ ToMySQL `EmailPOCO` -> `[]` or `[1, 2, 3, 4, 5, 6, 7, 8, 9, 10]`
    - Stored MySQL procedure handles data insertion and checks if 10 email attributes exits. Returns them in FIFO order.
    - Stores data into `Emails` and `EmailAttributes` tables
- _func_ CongratulateToBlob `parsed_message` -> `null`
    - Sends data to SendEmails Table on MySQL db
- _func_ CongratulateToTable `parsed_message` -> `null`
    - Sends data to `<GUID>.txt` file into parsedemails container

## Next steps
- For the managed identity: https://docs.microsoft.com/en-us/azure/azure-functions/functions-identity-based-connections-tutorial-2
- Convert in-process to isolated process -> needed?
- Better error handling (nonexistent at the moment)

## Notes of the chosen technologies
#### Service Bus vs Storage Que
- [x] Service Bus
    - Your solution requires the queue to provide a guaranteed first-in-first-out (FIFO) ordered delivery.
    - Your solution needs to support automatic duplicate detection.
        - Could be used if every email message attribute would be send in its own message to the Service Bus

- [ ] Storage Que
    - our application wants to track progress for processing a message in the queue. It's useful if the worker processing a message crashes. Another worker can then use that information to continue from where the prior worker left off.

Service Bus quarantees FIFO, but Storage Que would be more robust in the end?





