namespace SimpleMessagePipelineTests.TestEntities
{
    public class TestMessageSource<TMsg> : IMessageSource<TMsg>
    {
        private TMsg _message;
        public int AckCount { get; private set; }
        
        public TestMessageSource(TMsg message)
        {
            _message = message;
        }

        public TMsg Poll()
        {
            return _message;
        }

        public void Ack(TMsg msg)
        {
            ++AckCount;
        }
    }
}