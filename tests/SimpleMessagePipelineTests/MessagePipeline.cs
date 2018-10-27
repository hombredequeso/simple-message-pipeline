using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleMessagePipelineTests
{
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
            IIocManagement iocManagement
        )
        {
            TTransportMessage nextTransportMessage = messageSource.Poll();
            TDomainMessage nextMessage = 
                messageTransform.ToDomainMessage(nextTransportMessage);

            using (IServiceScope scope = rootServiceProvider.CreateScope())
            {
                // Setup the pipeline for running through the current msg:
                IServiceProvider scopeServiceProvider = scope.ServiceProvider;
                iocManagement.InitialiseScope(scopeServiceProvider);

                Type msgType = nextMessage.GetType();
                Type handlerType = typeof(IHandler<>).MakeGenericType(msgType);

                var handler = scopeServiceProvider.GetService(handlerType);
                MethodInfo handleMethod = handlerType.GetMethod("Handle");

                try
                {
                    handleMethod.Invoke(handler, new[] {(object) nextMessage});
                    messageSource.Ack(nextTransportMessage);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error handling message: {e.Message}");
                }
            }
            return nextTransportMessage;
        }
    }
}