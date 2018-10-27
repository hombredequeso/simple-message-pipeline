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
            IIocManagement iocManagement = new TestIocManagement(scopeId);
            
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

    public interface IIocManagement
    {
        IServiceCollection CreateServiceCollection();
        void InitialiseScope(IServiceProvider scopedServiceProvider);
    }


    public interface ISetExecutionContext
    {
        Guid Id { get; set; }
    }

    public interface IExecutionContext
    {
        Guid Id { get; }
    }
    
    public class ExecutionContext: ISetExecutionContext, IExecutionContext
    {
        public Guid Id { get; set; }
    }
}