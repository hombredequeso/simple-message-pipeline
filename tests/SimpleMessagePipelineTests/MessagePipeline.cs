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
        public static async 
            Task<Either<IPipelineError, Tuple<TTransportMessage, TDomainMessage>>>
            Run<TTransportMessage, TDomainMessage>(
                IMessageSource<TTransportMessage> messageSource,
                ITransportToDomainMessageTransform<TTransportMessage, TDomainMessage> messageTransform,
                ServiceProvider rootServiceProvider,
                IIocManagement<TTransportMessage> iocManagement
        )
        {
            Option<TTransportMessage> transportMessageO = 
                await messageSource.Poll();
            Option<Either<IPipelineError, Tuple<TTransportMessage, TDomainMessage>>> 
                processMessageResult =
                await transportMessageO.MapAsync(
                    transportMessage =>
                        ProcessTransportMessage(
                            messageTransform,
                            rootServiceProvider,
                            iocManagement,
                            transportMessage)).ToOption();
            
            Either<IPipelineError, Tuple<TTransportMessage, TDomainMessage>> 
                resFinal = processMessageResult.Match(
                    mpr => mpr,
                    () => Prelude.Left<IPipelineError>(new NoTransportMessageAvailable()));
            
            resFinal.IfRight(m => messageSource.Ack(m.Item1));
            
            return resFinal;
        }
        

        private static Task<Either<
                IPipelineError, 
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
            
            OptionAsync<Either<IPipelineError, Tuple<TTransportMessage, TDomainMessage>>> result2 = 
                domainMessageO.MapAsync(d =>
                    HandleDomainMessage(
                        rootServiceProvider,
                        iocManagement,
                        transportMessage,
                        d));

        Task<Either<
                IPipelineError, 
                Tuple<TTransportMessage, TDomainMessage>>>
            res = result2.Match(
                sm => sm,
                () => Prelude.Left((IPipelineError)new ErrorParsingTransportMessage()));
            
            return res;
        }
        
        private static Task<Either<
                                    IPipelineError, 
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
                Either<IPipelineError, ObjectHander> handler = 
                    GetHandler(domainMessage, scopeServiceProvider);
                var res = handler.BindAsync(h =>
                    HandleMessage(h, transportMessage, domainMessage));
                return res;
            }
        }
        
        private static async Task<Either<
                                    IPipelineError, 
                                    Tuple<TTransportMessage, TDomainMessage>>>
            HandleMessage<TTransportMessage, TDomainMessage>(
                ObjectHander handler,
                TTransportMessage transportMessage,
                TDomainMessage domainMessage)
        {
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
                IPipelineError err = new MessageHandlingException(e);
                return Prelude.Left(err);
            }
        }

        public static 
            Either<IPipelineError, ObjectHander>
                GetHandler<TDomainMessage>(
                    TDomainMessage domainMessage, 
                    IServiceProvider serviceProvider)
        {
            Type msgType = domainMessage.GetType();
            Type handlerType =
                typeof(IHandler<>).MakeGenericType(msgType);

            object handler;
            try
            {
                handler =
                    serviceProvider.GetService(handlerType);
            }
            catch (Exception e)
            {
                return Prelude.Left<IPipelineError>(
                    new UnableToConstructHandler(e));
            }

            MethodInfo handleMethod = handlerType.GetMethod("Handle");
            if (handleMethod == null)
                return Prelude.Left<IPipelineError>(
                    new NoHandleMethodOnHandler());
            
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
    
