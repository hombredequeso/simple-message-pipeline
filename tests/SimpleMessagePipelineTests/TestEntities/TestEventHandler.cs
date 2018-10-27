using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleMessagePipelineTests.TestEntities
{
    public class TestEventHandler : IHandler<TestEvent>
    {
        private readonly IExecutionContext<TransportMessage> _executionContext;

        public TestEventHandler(IExecutionContext<TransportMessage> executionContext)
        {
            _executionContext = executionContext;
        }

        public async Task Handle(TestEvent msg)
        {
            int z = await Task.FromResult(1);
            TestEventHandlerInvocationStats.HandledEvents
                .Add(Tuple.Create(
                    _executionContext.Id, 
                    _executionContext.TransportMessage, 
                    msg));
        }
    }

    public static class TestEventHandlerInvocationStats
    {
        public static List<Tuple<Guid, TransportMessage, TestEvent>> HandledEvents = 
            new List<Tuple<Guid, TransportMessage, TestEvent>>();
    }
}