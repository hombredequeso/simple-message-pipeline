using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;

namespace SimpleMessagePipelineTests.TestEntities
{
    public class TestMessageSource<TMsg> : IMessageSource<TMsg>
    {
        private List<TMsg> _messages;
        public int AckCount { get; private set; }
        
        public TestMessageSource(params TMsg[] message)
        {
            _messages = message.ToList();
        }

        public Task<Option<TMsg>> Poll()
        {
            Option<Tuple<TMsg, IEnumerable<TMsg>>> headAndTailO = 
                _messages.HeadAndTail();

            // Mutation of state isolated to this:
            foreach (var ht in headAndTailO)
            {
                _messages = ht.Item2.ToList();
            }
            
            return Task.FromResult(
                headAndTailO.Map(ht => ht.Item1));
        }

        public Task Ack(TMsg msg)
        {
            ++AckCount;
            return Task.CompletedTask;
        }
    }
}