using System;

namespace SimpleMessagePipelineTests
{
    public interface IPipelineError
    {}
    
    public class MessageHandlingException<TTransportMessage, TDomainMessage>: IPipelineError
    {
        public readonly TTransportMessage TransportMessage;
        public readonly TDomainMessage DomainMessage;
        public Exception Exception { get; }
        
        public MessageHandlingException(
            TTransportMessage transportMessage,
            TDomainMessage domainMessage,
            Exception exception)
        {
            TransportMessage = transportMessage;
            DomainMessage = domainMessage;
            Exception = exception;
        }
    }

    public class UnableToConstructHandler : IPipelineError
    {
        public UnableToConstructHandler(Exception exception)
        {
            Exception = exception;
        }

        public System.Exception Exception { get; }
    }
    
    public class NoHandleMethodOnHandler: IPipelineError
    {}
    
    public class NoTransportMessageAvailable: IPipelineError
    {}
    
    public class ErrorParsingTransportMessage: IPipelineError
    {}

}