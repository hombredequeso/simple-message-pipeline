# Simple Message Pipeline

Prototype for a simple message pipeline in C# (dotnet core).
Directed towards use cases like polling for messages from aws SQS.

## Getting Started

The simplest place to start to get a general understanding of how the pipeline is intended to work is the test in SimplePipelineTest, called One_Run_Through_Pipeline_With_TransportMessage_Succeeds().

This test illustrates a single successful run through the message pipeline. Many of the classes required to use the pipeline are included in the SimplePipelineTest file for simplicity of reading. Other test classes used can be found in the TestEntities folder.

To use the pipeline, the following classes are required to be implemented:
- TTransportMessage: a class representing a transport message off a bus.
- ITransportToDomainMessageTransform<TTransportMessage, TDomainMessage>: a class with a method to get a domain message from the transport message.
- Domain Messages.
- IHandler<TDomainMessage>. Handlers for the domain messages.



### Prerequisites

[.NET Core SDK] (https://www.microsoft.com/net/download)


### Installing


## Running the tests


```
cd tests
dotnet test
```

## Versioning

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the [tags on this repository](https://github.com/your/project/tags). 

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

