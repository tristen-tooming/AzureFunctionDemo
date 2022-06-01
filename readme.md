## Usage / Config

- There exists a Postman collection for integration tests
- Technologies:
    - Azure Functions: v4
    - MySQL: 8
    - .NEt: 6 (in-process)
- Scripts for initializing MySQL in folder `.\sqlCommands`

#### EmailAPI
Needs:
```Json
"ServiceBusConnectionAppSetting": "XXX"
```

#### queTaskOrchestrator
Needs:
```Json
"ServiceBusConnector": "XXX",
"BlobConnector": "XXX",
"MySQLConnector": "XXX"
```
(Remove entity from the Service Bus connection string)


#### TODO:
- Simplify code: Functions in to the same folder because we use the `MessagePOCO`?

## Implementation
#### EmailAPI
- Runs as in-process
- Uses async pattern to send message to the que and http response to the user
- Returns `202`

Takes in in body:
```Json
{
    "Key": "XXX",
    "Email": "XXX",
    "Attributes": ["one", "two", ..., "n"]
}
```

#### Service Bus Orchestrator

Input trigger from Service Bus orchestrates following functions:

- _func_ ToBlob `EmailPOCO` -> `null`
    - Creates folder structure in to emails container: {email}/{date}.json
    - Stored as json list and days email attributes are append to that list
- _func_ ToMySQL `EmailPOCO` -> `[]` or `[1, 2, 3, 4, 5, 6, 7, 8, 9, 10]`
    - Stored MySQL procedure handles data insertion and checks if 10 email attributes exits. Returns them in FIFO order.
    - __There can be a bug if inserting multiple items at the sametime__
        - Shows in logs that list of 10 attributes is returned multiple times?
- _func_ CongratulateToBlob `parsed_message` -> `null`
- _func_ CongratulateToTable `parsed_message` -> `null`

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





