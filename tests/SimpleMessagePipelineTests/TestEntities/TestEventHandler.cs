﻿using System;
using System.Collections.Generic;

namespace SimpleMessagePipelineTests.TestEntities
{
    public class TestEventHandler : IHandler<TestEvent>
    {
        private IExecutionContext<TransportMessage> _executionContext;

        public TestEventHandler(IExecutionContext<TransportMessage> executionContext)
        {
            _executionContext = executionContext;
        }

        public void Handle(TestEvent msg)
        {
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