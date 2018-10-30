using System;
using System.Collections.Generic;
using FluentAssertions;
using LanguageExt;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace SimpleMessagePipelineTests.MessageTransforms
{
    public class SampleEvent : IEquatable<SampleEvent>
    {
        public bool Equals(SampleEvent other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SampleEvent) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(SampleEvent left, SampleEvent right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SampleEvent left, SampleEvent right)
        {
            return !Equals(left, right);
        }

        public SampleEvent(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("id cannot be Guid.Empty");
            Id = id;
        }

        public Guid Id { get; }

        public static string MessageType =
            "simplemessagepipelinetests.testentities.sampleEvent:1.0";
    }
    

    public static class MessageDeserializer
    {
        public static Option<object> Deserialize(
            IDictionary<string, Type> typeLookup,
            string msgType,
            string serializedMessage)
        {
            return 
                typeLookup.TryGetValue(msgType)
                .Map(typ =>
                    JsonConvert.DeserializeObject(serializedMessage, typ));
        }
    }
    
    public class MessageDeserializerTests
    {
        [Fact]
        public void DeserializeMessage_Returns_None_When_No_Type_Available()
        {
            var typeLookup = new Dictionary<string, Type>();
            Option<object> domainMessage = MessageDeserializer.Deserialize(
                typeLookup,
                SampleEvent.MessageType,
                JsonConvert.SerializeObject(new SampleEvent(Guid.NewGuid()))
            );
            domainMessage.IsNone.Should().BeTrue();
        }
        
        [Fact]
        public void DeserializeMessage_Returns_Some_When_Type_Available()
        {
            var typeLookup = new Dictionary<string, Type>()
            {
                {SampleEvent.MessageType, typeof(SampleEvent)}
            };
            Option<object> domainMessage = MessageDeserializer.Deserialize(
                typeLookup,
                SampleEvent.MessageType,
                JsonConvert.SerializeObject(new SampleEvent(Guid.NewGuid()))
            );
            domainMessage.IsSome.Should().BeTrue();
        }
        
        [Fact]
        public void DeserializeMessage_Correctly_Deserializes_Message()
        {
            var typeLookup = new Dictionary<string, Type>()
            {
                {SampleEvent.MessageType, typeof(SampleEvent)}
            };
            
            var sampleEvent = new SampleEvent(Guid.NewGuid());
            
            Option<object> domainMessage = MessageDeserializer.Deserialize(
                typeLookup,
                SampleEvent.MessageType,
                JsonConvert.SerializeObject(sampleEvent)
            );
            
            domainMessage.Match(
                m => m.As<SampleEvent>().Should().Be(sampleEvent),
                () => "should be Some".AssertFail());
        }
    }
}