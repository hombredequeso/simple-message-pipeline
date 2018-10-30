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
        Option<TDomainMessage> ToDomainMessage(TTransportMessage transportMessage);
    }

    public static class MessagePipeline
    {
        public static async Task<Either<
                MessagePipelineError, 
                Tuple<TTransportMessage, TDomainMessage>>>
            Run<TTransportMessage, TDomainMessage>(
                IMessageSource<TTransportMessage> messageSource,
                ITransportToDomainMessageTransform<TTransportMessage, TDomainMessage> messageTransform,
                ServiceProvider rootServiceProvider,
                IIocManagement<TTransportMessage> iocManagement
        )
        {
            Option<TTransportMessage> transportMessageO = 
                await messageSource.Poll();
            Option<Either<MessagePipelineError, Tuple<TTransportMessage, TDomainMessage>>> 
                processMessageResult =
                await transportMessageO.MapAsync(
                    transportMessage =>
                        ProcessTransportMessage(
                            messageTransform,
                            rootServiceProvider,
                            iocManagement,
                            transportMessage)).ToOption();
            
            Either<MessagePipelineError, Tuple<TTransportMessage, TDomainMessage>> 
                resFinal = processMessageResult.Match(
                mpr => mpr,
                () => Prelude.Left(MessagePipelineError.NoTransportMessageAvailable)
            );
            
            resFinal.IfRight(m => messageSource.Ack(m.Item1));
            
            return resFinal;
        }

        public enum MessagePipelineError
        {
            NoTransportMessageAvailable = 1,
            ErrorParsingTransportMessage,
            ExceptionHandlingMessage
        }

        private static Task<Either<
                MessagePipelineError, 
                Tuple<TTransportMessage, TDomainMessage>>>
            ProcessTransportMessage<TTransportMessage, TDomainMessage>(
                ITransportToDomainMessageTransform<TTransportMessage,
                    TDomainMessage> messageTransform,
                ServiceProvider rootServiceProvider,
                IIocManagement<TTransportMessage> iocManagement,
                TTransportMessage transportMessage)
        {
            Option<TDomainMessage> domainMessageO =
                messageTransform.ToDomainMessage(transportMessage);
            
            OptionAsync<Either<MessagePipelineError, Tuple<TTransportMessage, TDomainMessage>>> result2 = 
                domainMessageO.MapAsync(d =>
                    HandleDomainMessage(
                        rootServiceProvider,
                        iocManagement,
                        transportMessage,
                        d));

            return result2.Match(
                sm => sm,
                () => Prelude.Left(MessagePipelineError
                    .ErrorParsingTransportMessage));
        }
        
        private static async Task<Either<
                                    MessagePipelineError, 
                                    Tuple<TTransportMessage, TDomainMessage>>>
            HandleDomainMessage<TTransportMessage, TDomainMessage>(
                ServiceProvider rootServiceProvider,
                IIocManagement<TTransportMessage> iocManagement,
                TTransportMessage transportMessage,
                TDomainMessage domainMessage)
        {
            using (IServiceScope scope = rootServiceProvider.CreateScope())
            {
                var scopeServiceProvider = scope.ServiceProvider;
                iocManagement.InitialiseScope(scopeServiceProvider, transportMessage);
                var handler = GetHandler(domainMessage, scopeServiceProvider);

                try
                {
                    Task t = (Task) handler.Method.Invoke(handler.Object,
                        new[] {(object) domainMessage});
                    await t.ConfigureAwait(false);
                    return Prelude.Right(
                        Tuple.Create(transportMessage,
                        domainMessage));
                }
                catch (Exception e)
                {
                    Console.WriteLine(
                        $"Error handling message: {e.Message}");
                    return Prelude.Left(MessagePipelineError.ExceptionHandlingMessage);
                }
            }
        }

        public static ObjectHander GetHandler<TDomainMessage>(
            TDomainMessage domainMessage, IServiceProvider serviceProvider)
        {
            Type msgType = domainMessage.GetType();
            Type handlerType =
                typeof(IHandler<>).MakeGenericType(msgType);

            object handler =
                serviceProvider.GetService(handlerType);

            MethodInfo handleMethod = handlerType.GetMethod("Handle");
            
            return new ObjectHander(handler, handleMethod);
        }

        public struct ObjectHander
        {
            public ObjectHander(object o, MethodInfo method)
            {
                Object = o ?? throw new ArgumentNullException(nameof(o));
                Method = method ??
                         throw new ArgumentNullException(nameof(method));
            }

            public object Object { get; set; }
            public MethodInfo Method { get; set; }
        }
    }
}
    
    
