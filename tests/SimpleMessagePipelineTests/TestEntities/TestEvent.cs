using System;

namespace SimpleMessagePipelineTests.TestEntities
{
    public class TestEvent
    {
        public TestEvent(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; }
    }
}