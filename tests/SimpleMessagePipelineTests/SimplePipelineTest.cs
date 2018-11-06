using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using SimpleMessagePipelineTests.TestEntities;
using Xunit;

namespace SimpleMessagePipelineTests.Simple
{
    //=========================================================================
    // Common Infrastructural classes:
    
    // Something representing a TransportMessage
    public class TransportMessage
    {
        public TransportMessage(object message)
        {
            Message = message;
        }
        public object Message { get; }
    }
    
    // A class providing a function to get a DomainMessage from a TransportMessage
    // Most likely the DomainMessage will be an object, unless the entire
    // system only ever has one type of message, or a common message base
    // class exists (generally, probably not desirable).
    public class SimpleMessageTransform 
        : ITransportToDomainMessageTransform<TransportMessage, object>
    {
        public Option<object> ToDomainMessage(TransportMessage transportMessage)
        {
            return transportMessage.Message;
        }
    }
    
    // An implementation of IIocManagement<TTransportMessage>
    // Tpically, everything in CreateServiceCollection is likely to have
    // a lifetime of 'Scope', meaning that one instance will be
    // created per processing of a TransportMessage.
    public class SimpleTestIocManagement: IIocManagement<TransportMessage>
    {
        private List<Guid> _scopeId;

        public SimpleTestIocManagement(params Guid[] scopeId)
        {
            _scopeId = scopeId.ToList();
        }

        public IServiceCollection CreateServiceCollection()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<ExecutionContext<TransportMessage>>();
            serviceCollection.AddScoped<IExecutionContext<TransportMessage>>(
                context => context.GetRequiredService<ExecutionContext<TransportMessage>>());
           serviceCollection.AddScoped<ISetExecutionContext<TransportMessage>>(
                context => context.GetRequiredService<ExecutionContext<TransportMessage>>());

            serviceCollection.AddSingleton<TestEventHandlerInvocationStats>();
            
            // Glue:
            serviceCollection.AddScoped<IHandler<TestEvent>, TestEventHandler>();
            
            return serviceCollection;
        }

        public void InitialiseScope(
            IServiceProvider scopedServiceProvider,
            TransportMessage transportMessage)
        {
            ISetExecutionContext<TransportMessage> executionContextSetter = scopedServiceProvider
                .GetService<ISetExecutionContext<TransportMessage>>();
            
            _scopeId.HeadAndTail().Match(
                ht =>
                {
                    Guid id = ht.Item1;
                    _scopeId = ht.Item2.ToList();
                    executionContextSetter.Id = id;
                    executionContextSetter.TransportMessage = transportMessage;
                },
                () => throw new Exception("ran out of scopeId's")
            );
        }
    }
    
    
    //=========================================================================
    // Events and Handlers
    // With any luck, all that needs to be done to make the system work is
    // add a new event, handler, and some glue (see 'glue' above!)
    
    // TestEvent is in TestEntities
    
    
    // It is unlikely that most handlers would need the IExecutionContext
    // It is only used here for illustrative purposes.
    public class TestEventHandler : IHandler<TestEvent>
    {
        private readonly IExecutionContext<TransportMessage> _executionContext;
        private readonly TestEventHandlerInvocationStats _stats;

        public TestEventHandler(
            IExecutionContext<TransportMessage> executionContext,
            TestEventHandlerInvocationStats stats)
        {
            _executionContext = executionContext;
            _stats = stats;
        }

        public async Task Handle(TestEvent msg)
        {
            int z = await Task.FromResult(1);
            _stats.HandledEvents
                .Add(Tuple.Create(
                    _executionContext.Id, 
                    _executionContext.TransportMessage, 
                    msg));
        }
    }
    
    public class TestEventHandlerInvocationStats
    {
        public List<Tuple<Guid, TransportMessage, TestEvent>> HandledEvents = 
            new List<Tuple<Guid, TransportMessage, TestEvent>>();
    }
    
    
    public class SimplePipelineTest
    {
        [Fact]
        public async void One_Run_Through_Pipeline_With_TransportMessage_Succeeds()
        {
            TestEvent testEvent= new TestEvent(Guid.NewGuid());
            TransportMessage transportMessage = new TransportMessage(testEvent);
            var scopeId = Guid.NewGuid();
            
            var messageSource = new TestMessageSource<TransportMessage>(transportMessage);
            IIocManagement<TransportMessage> iocManagement = 
                new SimpleTestIocManagement(scopeId);
            
            // Initialize Ioc
            IServiceCollection serviceCollection = iocManagement.CreateServiceCollection();
            ServiceProvider rootServiceProvider = serviceCollection.BuildServiceProvider();
            
            Either<IPipelineError, Tuple<TransportMessage, object>> 
                processedMessage = await MessagePipeline.Run(
                    messageSource, 
                    new SimpleMessageTransform(), 
                    rootServiceProvider, 
                    iocManagement);
            
            messageSource.AckCount.Should().Be(1);
            rootServiceProvider.GetService<TestEventHandlerInvocationStats>()
                .HandledEvents.Single().Should().BeEquivalentTo(
                    Tuple.Create(scopeId, transportMessage, testEvent));
            
            processedMessage.BiIter(
                right =>
                {
                    // Sadly, have to cast )-:
                    var castTestValue = Tuple.Create(right.Item1,
                        (TestEvent) right.Item2);
                    castTestValue.Should()
                        .BeEquivalentTo(Tuple.Create(transportMessage,
                            testEvent));
                },
                left => "Should be right".AssertFail()
            );
        }

        [Fact]
        public async void One_Run_Through_Pipeline_With_No_TransportMessage_Does_Nothing()
        {
            var messageSource = new TestMessageSource<TransportMessage>();
            IIocManagement<TransportMessage> iocManagement = 
                new SimpleTestIocManagement(Guid.NewGuid());
            
            // Initialize Ioc
            IServiceCollection serviceCollection = iocManagement.CreateServiceCollection();
            ServiceProvider rootServiceProvider = serviceCollection.BuildServiceProvider();
            
            Either<IPipelineError, Tuple<TransportMessage, object>> 
                processedMessage = await MessagePipeline.Run(
                    messageSource, 
                    new SimpleMessageTransform(), 
                    rootServiceProvider, 
                    iocManagement);
            
            messageSource.AckCount.Should().Be(0);
            rootServiceProvider.GetService<TestEventHandlerInvocationStats>()
                .HandledEvents.Count.Should().Be(0);
            
            processedMessage.BiIter(
                right =>
                {
                    "Should be right".AssertFail();
                },
                left => left.Should().BeOfType<NoTransportMessageAvailable>()
            );
        }
        
        [Fact]
        public async void Multiple_Runs_Through_Pipeline_Processes_Provided_Messages()
        {
            int runCount = 10;
            var scopeIds = Enumerable.Range(0, runCount)
                .Select(i => Guid.NewGuid())
                .ToList();
            var testEvents = Enumerable.Range(0, runCount)
                .Select(i => new TestEvent(Guid.NewGuid())).ToList();
            var transportEvents = testEvents
                .Select(t => new TransportMessage(t))
                .ToArray();

            List<Tuple<Guid, TransportMessage, TestEvent>> expectedHandledEvents = 
                scopeIds
                    .Zip(transportEvents)
                    .Zip(testEvents, (a, b) => Tuple.Create(a.Item1, a.Item2, b))
                    .ToList();
            
            var messageSource = new TestMessageSource<TransportMessage>(transportEvents);
            IIocManagement<TransportMessage> iocManagement = 
                new SimpleTestIocManagement(scopeIds.ToArray());
            
            // Initialize Ioc
            IServiceCollection serviceCollection = iocManagement.CreateServiceCollection();
            ServiceProvider rootServiceProvider = serviceCollection.BuildServiceProvider();

            List<Either<IPipelineError, Tuple<TransportMessage, object>>> runResults =
                new List<Either<IPipelineError, Tuple<TransportMessage, object>>>();
            for (var i = 0; i < runCount; i++)
            {
                Either<IPipelineError, Tuple<TransportMessage, object>> 
                    processedMessage = await MessagePipeline.Run(
                        messageSource, 
                        new SimpleMessageTransform(), 
                        rootServiceProvider, 
                        iocManagement);
                runResults.Add(processedMessage);
            }

            runResults.Count.Should().Be(runCount);
            messageSource.AckCount.Should().Be(runCount);
            rootServiceProvider.GetService<TestEventHandlerInvocationStats>()
                .HandledEvents.Count.Should().Be(runCount);

            rootServiceProvider.GetService<TestEventHandlerInvocationStats>()
                .HandledEvents.Should().BeEquivalentTo(expectedHandledEvents);
        }
    }
}