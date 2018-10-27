namespace SimpleMessagePipelineTests.TestEntities
{
    public class MessageTransform 
        : ITransportToDomainMessageTransform<TransportMessage, object>
    {
        public object ToDomainMessage(TransportMessage transportMessage)
        {
            return transportMessage.Message;
        }
    }
}