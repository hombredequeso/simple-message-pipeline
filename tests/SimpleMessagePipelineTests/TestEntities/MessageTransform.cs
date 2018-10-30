using LanguageExt;

namespace SimpleMessagePipelineTests.TestEntities
{
    public class MessageTransform 
        : ITransportToDomainMessageTransform<TransportMessage, object>
    {
        public Option<object> ToDomainMessage(TransportMessage transportMessage)
        {
            return new Some<object>(transportMessage.Message);
        }
    }
}