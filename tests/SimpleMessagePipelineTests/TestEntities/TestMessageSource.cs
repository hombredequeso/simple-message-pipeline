using System.Threading.Tasks;

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

        public Task<TMsg> Poll()
        {
            return Task.FromResult(_message);
        }

        public Task Ack(TMsg msg)
        {
            ++AckCount;
            return Task.CompletedTask;
        }
    }
}