using System;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleMessagePipelineTests.TestEntities
{
    public class TestIocManagement: IIocManagement
    {
        private Guid _scopeId;

        public TestIocManagement(Guid scopeId)
        {
            _scopeId = scopeId;
        }

        public IServiceCollection CreateServiceCollection()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<ExecutionContext>();
            serviceCollection.AddScoped<IExecutionContext>(context => context.GetRequiredService<ExecutionContext>());
            serviceCollection.AddScoped<ISetExecutionContext>(context => context.GetRequiredService<ExecutionContext>());
            serviceCollection
                .AddScoped<IHandler<TestEvent>, TestEventHandler>();
            return serviceCollection;
        }

        public void InitialiseScope(IServiceProvider scopedServiceProvider)
        {
            ISetExecutionContext executionContextSetter = scopedServiceProvider
                .GetService<ISetExecutionContext>();
            Guid id = _scopeId;
            executionContextSetter.Id = id;
        }
    }
}