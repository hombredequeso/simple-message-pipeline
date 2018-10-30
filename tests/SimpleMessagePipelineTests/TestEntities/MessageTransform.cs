using System;
using System.Collections.Generic;
using LanguageExt;
using SimpleMessagePipelineTests.MessageTransforms;
using Newtonsoft.Json.Linq;

namespace SimpleMessagePipelineTests.TestEntities
{
    public class MessageTransform 
        : ITransportToDomainMessageTransform<TransportMessage, object>
    {
        public Option<object> ToDomainMessage(TransportMessage transportMessage)
        {
            var msgType = transportMessage.Metadata["messageType"]
                .Value<string>();
            var domainMessage = transportMessage.Message;
            Option<object> result = MessageDeserializer.Deserialize(
                _typeLookup,
                msgType,
                domainMessage);
            return result;
        }

        private IDictionary<string, Type> _typeLookup;
        public MessageTransform(IDictionary<string, Type> typeLookup)
        {
            _typeLookup = typeLookup ?? throw new ArgumentNullException(nameof(typeLookup));
        }
    }
    
}