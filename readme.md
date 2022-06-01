## Usage / Config
#### EmailAPI
- Needs Service Bus connection string in ```settings.json```: ```"ServiceBusConnectionAppSetting": "XXX"```
- Resource: POST api/emailAPI

#### queTaskOrchestrator
- remove entity from the Service Bus connection string


#### Blob Notes
Storage account for every user or container or just blobs?


#### TODO:
- Simplify code: Functions in to the same folder because we use the EmailPOCO? How the namespace work in c#?

## Implementation
#### EmailAPI
- Runs as in-process
- Uses async pattern to send message to the que and http response to the user

#### Service Bus Orchestrator

Input trigger from Service Bus orchestrates following functions:

- Blob (message in)
    - <email><date>.json
- MySQL (message in)
    - Stored MySQL procedure handles data insertion and checks if 10 email attributes exits. Returns them in FIFO order.
- Message (10 attributes in)
    - Blob
    - Table

## Next steps
- For the managed identity: https://docs.microsoft.com/en-us/azure/azure-functions/functions-identity-based-connections-tutorial-2
- Convert in-process to isolated process

## Notes of the chosen technologies
#### Service Bus vs Storage Que
- [x] Service Bus
    - Your solution requires the queue to provide a guaranteed first-in-first-out (FIFO) ordered delivery.
    - Your solution needs to support automatic duplicate detection.
        - Could be used if every email message attribute would be send in its own message to the Service Bus

- [ ] Storage Que
    - our application wants to track progress for processing a message in the queue. It's useful if the worker processing a message crashes. Another worker can then use that information to continue from where the prior worker left off.

Service Bus quarantees FIFO, but Storage Que would be more robust in the end?





