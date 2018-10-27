using System;
using System.Collections.Generic;

namespace SimpleMessagePipelineTests.TestEntities
{
    public class TestEventHandler : IHandler<TestEvent>
    {
        private IExecutionContext _executionContext;

        public TestEventHandler(IExecutionContext executionContext)
        {
            _executionContext = executionContext;
        }

        public void Handle(TestEvent msg)
        {
            TestEventHandlerInvocationStats.HandledEvents
                .Add(Tuple.Create(_executionContext.Id, msg));
        }
    }

    public static class TestEventHandlerInvocationStats
    {
        public static List<Tuple<Guid, TestEvent>> HandledEvents = 
            new List<Tuple<Guid, TestEvent>>();
    }
}