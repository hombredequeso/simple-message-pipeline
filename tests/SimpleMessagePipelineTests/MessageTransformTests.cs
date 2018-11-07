using System;
using System.Collections.Generic;
using FluentAssertions;
using LanguageExt;
using Newtonsoft.Json;
using SimpleMessagePipelineTests.TestEntities;
using SimpleMessagePipelineTests.Util;
using Xunit;

namespace SimpleMessagePipelineTests
{
    public class MessageDeserializerTests
    {
        [Fact]
        public void DeserializeMessage_Returns_None_When_No_Type_Available()
        {
            var typeLookup = new Dictionary<string, Type>();
            Option<object> domainMessage = MessageDeserializer.Deserialize(
                typeLookup,
                TestEvent.MessageType,
                JsonConvert.SerializeObject(new TestEvent(Guid.NewGuid()))
            );
            domainMessage.IsNone.Should().BeTrue();
        }
        
        [Fact]
        public void DeserializeMessage_Returns_Some_When_Type_Available()
        {
            var typeLookup = new Dictionary<string, Type>()
            {
                {TestEvent.MessageType, typeof(TestEvent)}
            };
            Option<object> domainMessage = MessageDeserializer.Deserialize(
                typeLookup,
                TestEvent.MessageType,
                JsonConvert.SerializeObject(new TestEvent(Guid.NewGuid()))
            );
            domainMessage.IsSome.Should().BeTrue();
        }
        
        [Fact]
        public void DeserializeMessage_Correctly_Deserializes_Message()
        {
            var typeLookup = new Dictionary<string, Type>()
            {
                {TestEvent.MessageType, typeof(TestEvent)}
            };
            
            var testEvent = new TestEvent(Guid.NewGuid());
            
            Option<object> domainMessage = MessageDeserializer.Deserialize(
                typeLookup,
                TestEvent.MessageType,
                JsonConvert.SerializeObject(testEvent)
            );
            
            domainMessage.Match(
                m => m.As<TestEvent>().Should().Be(testEvent),
                () => "should be Some".AssertFail());
        }
    }
}