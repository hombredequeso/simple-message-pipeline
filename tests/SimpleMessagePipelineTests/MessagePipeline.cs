using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleMessagePipelineTests
{
    public interface IIocManagement<TTransportMessage>
    {
        IServiceCollection CreateServiceCollection();
        void InitialiseScope(IServiceProvider scopedServiceProvider, TTransportMessage transportMessage);
    }

    public interface IMessageSource<TMessage>
    {
        TMessage Poll();
        void Ack(TMessage msg);
    }

    public interface IHandler<TMessage>
    {
        void Handle(TMessage msg);
    }

    public interface ITransportToDomainMessageTransform<TTransportMessage, TDomainMessage>
    {
        TDomainMessage ToDomainMessage(TTransportMessage transportMessage);
    }

    public static class MessagePipeline
    {
        public static TTransportMessage Run<TTransportMessage, TDomainMessage>(
            IMessageSource<TTransportMessage> messageSource,
            ITransportToDomainMessageTransform<TTransportMessage, TDomainMessage> messageTransform,
            ServiceProvider rootServiceProvider,
            IIocManagement<TTransportMessage> iocManagement
        )
        {
            TTransportMessage transportMessage = messageSource.Poll();
            TDomainMessage domainMessage = 
                messageTransform.ToDomainMessage(transportMessage);

            using (IServiceScope scope = rootServiceProvider.CreateScope())
            {
                // Setup the pipeline for running through the current msg:
                IServiceProvider scopeServiceProvider = scope.ServiceProvider;
                iocManagement.InitialiseScope(scopeServiceProvider, transportMessage);

                Type msgType = domainMessage.GetType();
                Type handlerType = typeof(IHandler<>).MakeGenericType(msgType);

                var handler = scopeServiceProvider.GetService(handlerType);
                MethodInfo handleMethod = handlerType.GetMethod("Handle");

                try
                {
                    handleMethod.Invoke(handler, new[] {(object) domainMessage});
                    messageSource.Ack(transportMessage);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error handling message: {e.Message}");
                }
            }
            return transportMessage;
        }
    }
}