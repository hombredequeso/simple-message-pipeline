using System;
using System.Reflection;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleMessagePipelineTests
{
    public interface IIocManagement<TTransportMessage>
    {
        IServiceCollection CreateServiceCollection();
        void InitialiseScope(IServiceProvider scopedServiceProvider, TTransportMessage transportMessage);
    }

    public interface IMessageSource<TTransportMessage>
    {
        Task<Option<TTransportMessage>> Poll();
        Task Ack(TTransportMessage msg);
    }

    public interface IHandler<TMessage>
    {
        Task Handle(TMessage msg);
    }

    public interface ITransportToDomainMessageTransform<TTransportMessage, TDomainMessage>
    {
        TDomainMessage ToDomainMessage(TTransportMessage transportMessage);
    }

    public static class MessagePipeline
    {
        public static Task<Option<TTransportMessage>> Run<TTransportMessage, TDomainMessage>(
            IMessageSource<TTransportMessage> messageSource,
            ITransportToDomainMessageTransform<TTransportMessage, TDomainMessage> messageTransform,
            ServiceProvider rootServiceProvider,
            IIocManagement<TTransportMessage> iocManagement
        )
        {
            return 
                messageSource.Poll()
                .MapAsync(transportMessage =>
                    TransportMessageZ(
                        messageSource, 
                        messageTransform, 
                        rootServiceProvider, 
                        iocManagement, 
                        transportMessage)
                ).ToOption();
        }

        private static async Task<TTransportMessage>
            TransportMessageZ<TTransportMessage, TDomainMessage>(
                IMessageSource<TTransportMessage> messageSource,
                ITransportToDomainMessageTransform<TTransportMessage,
                    TDomainMessage> messageTransform,
                ServiceProvider rootServiceProvider,
                IIocManagement<TTransportMessage> iocManagement,
                TTransportMessage transportMessage)
        {
            using (IServiceScope scope = rootServiceProvider.CreateScope())
            {
                TDomainMessage domainMessage =
                    messageTransform.ToDomainMessage(transportMessage);
                // Setup the pipeline for running through the current msg:
                IServiceProvider scopeServiceProvider =
                    scope.ServiceProvider;
                iocManagement.InitialiseScope(scopeServiceProvider,
                    transportMessage);

                Type msgType = domainMessage.GetType();
                Type handlerType =
                    typeof(IHandler<>).MakeGenericType(msgType);

                object handler = scopeServiceProvider.GetService(handlerType);
                
                MethodInfo handleMethod = handlerType.GetMethod("Handle");

                try
                {
                    Task t = (Task) handleMethod.Invoke(handler,
                        new[] {(object) domainMessage});
                    await t.ConfigureAwait(false);
                    await messageSource.Ack(transportMessage);
                }
                catch (Exception e)
                {
                    Console.WriteLine(
                        $"Error handling message: {e.Message}");
                }
            }
            return transportMessage;
        }
    }
}