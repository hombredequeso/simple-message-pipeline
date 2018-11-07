using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using SimpleMessagePipelineTests.TestEntities;
using SimpleMessagePipelineTests.Util;

namespace SimpleMessagePipelineTests
{
    public class HandlerThrowsExceptionPipelineTest
    {
        public class TransportMessage
        {
            public TransportMessage(object message)
            {
                Message = message;
            }

            public object Message { get; }
        }

        public class SimpleMessageTransform
            : ITransportToDomainMessageTransform<TransportMessage, object>
        {
            public Option<object> ToDomainMessage(
                TransportMessage transportMessage)
            {
                return transportMessage.Message;
            }
        }


        public class SimpleTestIocManagement<THandler, TDomainEvent, TStats>
            : IIocManagement<TransportMessage>
            where
            THandler : class,
            IHandler<TDomainEvent>
            where TStats : class
        {
            private List<Guid> _scopeId;

            public SimpleTestIocManagement(params Guid[] scopeId)
            {
                _scopeId = scopeId.ToList();
            }

            public IServiceCollection CreateServiceCollection()
            {
                var serviceCollection = new ServiceCollection();
                serviceCollection
                    .AddScoped<ExecutionContext<TransportMessage>>();
                serviceCollection
                    .AddScoped<IExecutionContext<TransportMessage>>(
                        context =>
                            context
                                .GetRequiredService<
                                    ExecutionContext<TransportMessage>>());
                serviceCollection
                    .AddScoped<ISetExecutionContext<TransportMessage>>(
                        context =>
                            context
                                .GetRequiredService<
                                    ExecutionContext<TransportMessage>>());


                serviceCollection.AddSingleton<TStats>();
                // Glue:
                serviceCollection.AddScoped<IHandler<TDomainEvent>, THandler>();

                return serviceCollection;
            }

            public void InitialiseScope(
                IServiceProvider scopedServiceProvider,
                TransportMessage transportMessage)
            {
                ISetExecutionContext<TransportMessage> executionContextSetter =
                    scopedServiceProvider
                        .GetService<ISetExecutionContext<TransportMessage>>();

                _scopeId.HeadAndTail().Match(
                    ht =>
                    {
                        Guid id = ht.Item1;
                        _scopeId = ht.Item2.ToList();
                        executionContextSetter.Id = id;
                        executionContextSetter.TransportMessage =
                            transportMessage;
                    },
                    () => throw new Exception("ran out of scopeId's")
                );
            }
        }


        public class ThrowExceptionEventHandler : IHandler<TestEvent>
        {
            public async Task Handle(TestEvent msg)
            {
                await Task.FromResult(1); // Dummy statement, doing stuff...
                throw new Exception("FAIL, FAIL, FAIL");
            }
        }

        public class TestStats
        {
        }


        [Fact]
        public async void
            When_Handler_Throws_Exception_It_Is_Returned_In_Result()
        {
            TestEvent testEvent = new TestEvent(Guid.NewGuid());
            TransportMessage transportMessage =
                new TransportMessage(testEvent);
            var scopeId = Guid.NewGuid();

            var messageSource =
                new TestMessageSource<TransportMessage>(transportMessage);
            IIocManagement<TransportMessage> iocManagement =
                new SimpleTestIocManagement<ThrowExceptionEventHandler,
                    TestEvent, TestStats>(scopeId);

            // Initialize Ioc
            IServiceCollection serviceCollection =
                iocManagement.CreateServiceCollection();
            ServiceProvider rootServiceProvider =
                serviceCollection.BuildServiceProvider();

            Either<IPipelineError, Tuple<TransportMessage, object>>
                processedMessage = await MessagePipeline.Run(
                    messageSource,
                    new SimpleMessageTransform(),
                    rootServiceProvider,
                    iocManagement);

            messageSource.AckCount.Should().Be(0);
            processedMessage.BiIter(
                right => "Should be left".AssertFail(),
                left =>
                {
                    left.Should()
                        .BeOfType<MessageHandlingException<TransportMessage,
                            object>>();
                    left.As<MessageHandlingException<TransportMessage, object>
                        >()
                        .Should().BeEquivalentTo(new
                        {
                            TransportMessage = transportMessage,
                            DomainMessage = testEvent
                        });
                });
        }
    }
}