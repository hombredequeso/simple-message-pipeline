using System;

namespace SimpleMessagePipelineTests.TestEntities
{
    public class ExecutionContext<TTransportMessage>
        : ISetExecutionContext<TTransportMessage>, 
            IExecutionContext<TTransportMessage>
    {
        public Guid Id { get; set; }
        public TTransportMessage TransportMessage { get; set; }
    }
    
    public interface ISetExecutionContext<TTransportMessage>
    {
        Guid Id { get; set; }
        TTransportMessage TransportMessage { get; set; }
    }

    public interface IExecutionContext<TTransportMessage>
    {
        Guid Id { get; }
        TTransportMessage TransportMessage { get; }
    }
}