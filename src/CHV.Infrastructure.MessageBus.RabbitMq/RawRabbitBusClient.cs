using RawRabbit;
using RawRabbit.Configuration;
using RawRabbit.vNext;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CHV.Infrastructure.MessageBus.RabbitMq
{
    public class RawRabbitBusClient : IMessageBusClient
    {
        private readonly IBusClient _client;

        public RawRabbitBusClient(string uri, string exchangeName, string queueName)
        {
            var rawRabbitConfig = new RawRabbitConfiguration();
            _client = BusClientFactory.CreateDefault(rawRabbitConfig);
        }

        public void Dispose()
        {
            var shutDownTask = _client.ShutdownAsync(TimeSpan.Zero);
            shutDownTask.GetAwaiter().GetResult();
        }

        public IObservable<Unit> Publish<TMessage>(TMessage message)
        {
            return Observable.StartAsync(async () =>
            {
                await _client.PublishAsync(message);
            });
        }

        public IObservable<Unit> Subscribe<TMessage>(Func<TMessage, Task> subscribeHandlerMethod)
        {
            return Observable.Start(() =>
            {
                _client.SubscribeAsync<TMessage>(async (msg, context) =>
                {
                    await subscribeHandlerMethod(msg);
                });
            });
        }

        // ~Publish
        public Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message)
        {
            throw new NotImplementedException();
        }

        // ~Subscribe
        public Task RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> messageHandlerMethod)
        {
            throw new NotImplementedException();
        }
    }
}
