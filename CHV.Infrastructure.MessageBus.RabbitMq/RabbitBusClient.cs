using RawRabbit;
using RawRabbit.Configuration;
using RawRabbit.vNext;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CHV.Infrastructure.MessageBus.RabbitMq
{
    public class RabbitBusClient : IMessageBusClient
    {
        private readonly IBusClient _rawRabbitClient;

        public RabbitBusClient()
        {
            var rawRabbitConfig = new RawRabbitConfiguration();
            _rawRabbitClient = BusClientFactory.CreateDefault(rawRabbitConfig);
        }

        public void Dispose()
        {
            var shutDownTask = _rawRabbitClient.ShutdownAsync(TimeSpan.Zero);
            shutDownTask.GetAwaiter().GetResult();
        }

        public IObservable<Unit> Publish<TMessage>(TMessage message)
        {
            return Observable.Start(() => 
            {
                _rawRabbitClient.PublishAsync(message);
            });
        }

        public IObservable<Unit> Subscribe<TMessage>(Func<TMessage, Task> subscribeHandlerMethod)
        {
            return Observable.Start(() =>
            {
                _rawRabbitClient.SubscribeAsync<TMessage>(async (msg, context) =>
                {
                    await subscribeHandlerMethod(msg);
                });
            });
        }
    }
}
