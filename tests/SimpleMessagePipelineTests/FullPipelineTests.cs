using System;
using System.Linq;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using SimpleMessagePipelineTests.TestEntities;
using Xunit;

namespace SimpleMessagePipelineTests
{
    public class FullPipelineTests
    {
        [Fact]
        public async void BasicPipelineTest()
        {
            // Test setup
            TestEvent testEvent= new TestEvent(Guid.NewGuid());
            TransportMessage transportMessage = new TransportMessage(testEvent);
            var scopeId = Guid.NewGuid();
            
            var messageSource = new TestMessageSource<TransportMessage>(transportMessage);
            IIocManagement<TransportMessage> iocManagement = 
                new TestIocManagement<TransportMessage>(scopeId);
            
            // Initialize Ioc
            IServiceCollection serviceCollection = iocManagement.CreateServiceCollection();
            ServiceProvider rootServiceProvider = serviceCollection.BuildServiceProvider();
            
            Option<TransportMessage> processedMessage = await MessagePipeline.Run(
                messageSource, 
                new MessageTransform(), 
                rootServiceProvider, 
                iocManagement);
            messageSource.AckCount.Should().Be(1);
            processedMessage.Match(
                m => m.Should().Be(transportMessage),
                () => Assert.True(false, "should be some")
            );

            TestEventHandlerInvocationStats.HandledEvents.Single().Should().Be(
                Tuple.Create(scopeId, transportMessage, testEvent));
        }
    }

    public interface ISetExecutionContext<TTransportMessage>
    {
        Guid Id { get; set; }
        TTransportMessage TransportMessage { get; set; }
    }

    public interface IExecutionContext<TTransportMessage>
    {
        Guid Id { get; }
        TTransportMessage TransportMessage { get; }
    }
    
    public class ExecutionContext<TTransportMessage>
        : ISetExecutionContext<TTransportMessage>, 
            IExecutionContext<TTransportMessage>
    {
        public Guid Id { get; set; }
        public TTransportMessage TransportMessage { get; set; }
    }
}