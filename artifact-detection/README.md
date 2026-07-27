# artifact-automation-service-detection
AI model as a service for artifact-automation project


## Code structure

artifact-automation-service-detection
 -- resources [static files]
    -- model-source-code [trained model and model testcode]

 -- src [artifact project source code]
    -- messaging [data communication between processes]
        -- signalr-client [connect to a c# signalr hub embeded in a webapi server]