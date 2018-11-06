using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using SimpleMessagePipelineTests.TestEntities;

namespace SimpleMessagePipelineTests
{
    public class FullPipelineWithDeserializationTests
    {
        // Various test classes:
        public class TestEventHandler : IHandler<TestEvent>
        {
            private readonly IExecutionContext<TransportMessage>
                _executionContext;

            public TestEventHandler(
                IExecutionContext<TransportMessage> executionContext)
            {
                _executionContext = executionContext;
            }

            public async Task Handle(TestEvent msg)
            {
                int z = await Task.FromResult(1);
                TestEventHandlerInvocationStats.HandledEvents
                    .Add(Tuple.Create(
                        _executionContext.Id,
                        _executionContext.TransportMessage,
                        msg));
            }
        }

        public static class TestEventHandlerInvocationStats
        {
            public static List<Tuple<Guid, TransportMessage, TestEvent>>
                HandledEvents =
                    new List<Tuple<Guid, TransportMessage, TestEvent>>();
        }

        public class
            TestIocManagement<TTransportMessage> : IIocManagement<
                TTransportMessage>
        {
            private Guid _scopeId;

            public TestIocManagement(Guid scopeId)
            {
                _scopeId = scopeId;
            }

            public IServiceCollection CreateServiceCollection()
            {
                var serviceCollection = new ServiceCollection();
                serviceCollection
                    .AddScoped<ExecutionContext<TTransportMessage>>();
                serviceCollection
                    .AddScoped<IExecutionContext<TTransportMessage>>(
                        context =>
                            context
                                .GetRequiredService<
                                    ExecutionContext<TTransportMessage>>());
                serviceCollection
                    .AddScoped<ISetExecutionContext<TTransportMessage>>(
                        context =>
                            context
                                .GetRequiredService<
                                    ExecutionContext<TTransportMessage>>());
                serviceCollection
                    .AddScoped<IHandler<TestEvent>, TestEventHandler>();
                return serviceCollection;
            }

            public void InitialiseScope(
                IServiceProvider scopedServiceProvider,
                TTransportMessage transportMessage)
            {
                ISetExecutionContext<TTransportMessage> executionContextSetter =
                    scopedServiceProvider
                        .GetService<ISetExecutionContext<TTransportMessage>>();
                Guid id = _scopeId;
                executionContextSetter.Id = id;
                executionContextSetter.TransportMessage = transportMessage;
            }
        }

        public class TransportMessage
        {
            public JObject Metadata { get; set; }
            public string Message { get; set; } // typically event or command
        }

        public class MessageTransform
            : ITransportToDomainMessageTransform<TransportMessage, object>
        {
            public Option<object> ToDomainMessage(
                TransportMessage transportMessage)
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
                _typeLookup = typeLookup ??
                              throw new ArgumentNullException(
                                  nameof(typeLookup));
            }
        }


        [Fact]
        public async void BasicPipelineTest()
        {
            TestEvent testEvent = new TestEvent(Guid.NewGuid());
            TransportMessage transportMessage = new TransportMessage()
            {
                Message = JsonConvert.SerializeObject(testEvent),
                Metadata = JObject.FromObject(new
                {
                    messageType = TestEvent.MessageType
                })
            };


            var scopeId = Guid.NewGuid();

            var messageSource =
                new TestMessageSource<TransportMessage>(transportMessage);
            IIocManagement<TransportMessage> iocManagement =
                new TestIocManagement<TransportMessage>(scopeId);

            Dictionary<string, Type> typeLookup = new Dictionary<string, Type>()
            {
                {TestEvent.MessageType, typeof(TestEvent)}
            };

            // Initialize Ioc
            IServiceCollection serviceCollection =
                iocManagement.CreateServiceCollection();
            ServiceProvider rootServiceProvider =
                serviceCollection.BuildServiceProvider();

            Either<IPipelineError, Tuple<TransportMessage, object>>
                processedMessage = await MessagePipeline.Run(
                    messageSource,
                    new MessageTransform(typeLookup),
                    rootServiceProvider,
                    iocManagement);

            messageSource.AckCount.Should().Be(1);
            TestEventHandlerInvocationStats.HandledEvents.Single().Should()
                .BeEquivalentTo(
                    Tuple.Create<Guid, TransportMessage, TestEvent>(scopeId,
                        transportMessage, testEvent));

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