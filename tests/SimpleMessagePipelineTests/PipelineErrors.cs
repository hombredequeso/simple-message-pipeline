using System;

namespace SimpleMessagePipelineTests
{
    public interface IPipelineError
    {}
    
    public class MessageHandlingException: IPipelineError
    {
        public Exception Exception { get; }
        public MessageHandlingException(Exception exception)
        {
            Exception = exception;
        }
    }

    public class UnableToConstructHandler : IPipelineError
    {
        public UnableToConstructHandler(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; }
    }
    
    public class NoHandleMethodOnHandler: IPipelineError
    {}
    
    public class NoTransportMessageAvailable: IPipelineError
    {}
    
    public class ErrorParsingTransportMessage: IPipelineError
    {}

}