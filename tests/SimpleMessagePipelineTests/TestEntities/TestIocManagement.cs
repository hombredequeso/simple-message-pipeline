using System;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleMessagePipelineTests.TestEntities
{
    public class TestIocManagement<TTransportMessage>: IIocManagement<TTransportMessage>
    {
        private Guid _scopeId;

        public TestIocManagement(Guid scopeId)
        {
            _scopeId = scopeId;
        }

        public IServiceCollection CreateServiceCollection()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<ExecutionContext<TTransportMessage>>();
            serviceCollection.AddScoped<IExecutionContext<TTransportMessage>>(
                context => context.GetRequiredService<ExecutionContext<TTransportMessage>>());
            serviceCollection.AddScoped<ISetExecutionContext<TTransportMessage>>(
                context => context.GetRequiredService<ExecutionContext<TTransportMessage>>());
            serviceCollection
                .AddScoped<IHandler<TestEvent>, TestEventHandler>();
            return serviceCollection;
        }

        public void InitialiseScope(
            IServiceProvider scopedServiceProvider,
            TTransportMessage transportMessage)
        {
            ISetExecutionContext<TTransportMessage> executionContextSetter = scopedServiceProvider
                .GetService<ISetExecutionContext<TTransportMessage>>();
            Guid id = _scopeId;
            executionContextSetter.Id = id;
            executionContextSetter.TransportMessage = transportMessage;
        }
    }
}