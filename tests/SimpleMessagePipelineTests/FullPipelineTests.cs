using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleMessagePipelineTests.MessageTransforms;
using SimpleMessagePipelineTests.TestEntities;
using Xunit;

namespace SimpleMessagePipelineTests
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
    
    public class FullPipelineWithDeserializationTests
    {
        [Fact]
        public async void BasicPipelineTest()
        {
            // Test setup
            TestEvent testEvent= new TestEvent(Guid.NewGuid());
            TransportMessage transportMessage = new TransportMessage()
            {
                Message = JsonConvert.SerializeObject(testEvent),
                Metadata = JObject.FromObject(new
                {
                    messageType = TestEvent.MessageType
                })
            };
            
            TransportMessage transportMessage2 = new TransportMessage()
            {
                Message = JsonConvert.SerializeObject(testEvent),
                Metadata = JObject.FromObject(new
                {
                    messageType = TestEvent.MessageType
                })
            };
            
            transportMessage.Should().BeEquivalentTo(transportMessage2);
            
            var scopeId = Guid.NewGuid();
            
            var messageSource = new TestMessageSource<TransportMessage>(transportMessage);
            IIocManagement<TransportMessage> iocManagement = 
                new TestIocManagement<TransportMessage>(scopeId);
            
            Dictionary<string, Type> typeLookup= new Dictionary<string, Type>()
            {
                {TestEvent.MessageType, typeof(TestEvent)}
            };
            
            // Initialize Ioc
            IServiceCollection serviceCollection = iocManagement.CreateServiceCollection();
            ServiceProvider rootServiceProvider = serviceCollection.BuildServiceProvider();
            
            Either<IPipelineError, Tuple<TransportMessage, object>> 
                processedMessage = await MessagePipeline.Run(
                    messageSource, 
                    new MessageTransform(typeLookup), 
                    rootServiceProvider, 
                    iocManagement);
            
            messageSource.AckCount.Should().Be(1);
            TestEventHandlerInvocationStats.HandledEvents.Single().Should().BeEquivalentTo(
                Tuple.Create<Guid, TransportMessage, TestEvent>(scopeId, transportMessage, testEvent));
            
            processedMessage.BiIter(
                right =>
                {
                    var castTestValue = Tuple.Create(right.Item1,
                        (TestEvent) right.Item2);
                    castTestValue.Should()
                        .BeEquivalentTo(Tuple.Create(transportMessage,
                            testEvent));
                },
                left => "Should be right".AssertFail()
            );
            
        }
        
    }
}
