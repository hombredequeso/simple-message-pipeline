using System.Threading.Tasks;
using LanguageExt;

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

        public Task<Option<TMsg>> Poll()
        {
            return Task.FromResult(Option<TMsg>.Some(_message));
        }

        public Task Ack(TMsg msg)
        {
            ++AckCount;
            return Task.CompletedTask;
        }
    }
}