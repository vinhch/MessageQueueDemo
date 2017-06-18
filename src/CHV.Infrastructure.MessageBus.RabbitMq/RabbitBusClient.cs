using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CHV.Infrastructure.MessageBus.RabbitMq
{
    public class RabbitBusClient : IMessageBusClient
    {
        private readonly IModel _channel;
        private readonly IConnection _connection;
        private readonly string _exchangeName;
        private readonly string _exchangeType;
        private readonly string _queueName;
        private readonly string _routingKey;

        private bool _disposed;
        private JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        public RabbitBusClient(string uri, string exchangeName, string exchangeType, string queueName, string routingKey = "")
        {
            _exchangeName = exchangeName;
            _exchangeType = exchangeType;
            _queueName = queueName;
            _routingKey = routingKey;

            var factory = new ConnectionFactory
            {
                Uri = uri,
                AutomaticRecoveryEnabled = true
            };
            _connection = factory.CreateConnection();

            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(_exchangeName, _exchangeType);
            _channel.QueueDeclare(_queueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(_queueName, _exchangeName, _routingKey, null);
            // Configure how we receive messages
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false); // Process only one message at a time
        }

        public void Dispose()
        {
            if (_disposed) return;
            _channel.Dispose();
            _connection.Dispose();
            _disposed = true;
        }

        public IObservable<Unit> Publish<TMessage>(TMessage message)
        {
            return Observable.Start(() =>
            {
                var json = JsonConvert.SerializeObject(message, _jsonSerializerSettings);
                var bytes = Encoding.UTF8.GetBytes(json);
                _channel.BasicPublish(_exchangeName, _routingKey, null, bytes);
            });
        }

        public IObservable<Unit> Subscribe<TMessage>(Func<TMessage, Task> subscribeHandlerMethod)
        {
            return Observable.Start(() =>
            {
                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += async (sender, e) =>
                {
                    var body = e.Body;
                    var json = Encoding.UTF8.GetString(body);
                    var message = JsonConvert.DeserializeObject<TMessage>(json, _jsonSerializerSettings);
                    await subscribeHandlerMethod(message);

                    _channel.BasicAck(deliveryTag: e.DeliveryTag, multiple: false);
                };

                _channel.BasicConsume(_queueName, false, consumer);
            });
        }
    }
}
