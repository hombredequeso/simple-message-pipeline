namespace SimpleMessagePipelineTests.TestEntities
{
    public class TransportMessage
    {
        public TransportMessage(object message)
        {
            Message = message;
        }

        public object Message { get; }
    }
}