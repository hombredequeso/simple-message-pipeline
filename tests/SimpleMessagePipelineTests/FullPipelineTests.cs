using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SimpleMessagePipelineTests.TestEntities;
using Xunit;

namespace SimpleMessagePipelineTests
{
    public class FullPipelineTests
    {
        [Fact]
        public void BasicPipelineTest()
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
            
            // Start main loop...
            TransportMessage nextTransportMessage = messageSource.Poll();
            var nextMessage = nextTransportMessage.Message;

            TransportMessage processedMessage = MessagePipeline.Run(
                messageSource, 
                new MessageTransform(), 
                // tm => tm.Message,
                rootServiceProvider, 
                iocManagement);
            messageSource.AckCount.Should().Be(1);
            processedMessage.Should().Be(transportMessage);

            TestEventHandlerInvocationStats.HandledEvents.Single().Should().Be(
                Tuple.Create(scopeId, testEvent));
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