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
        private readonly string _queueName;

        private bool _disposed;
        private JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        public RabbitBusClient(string uri, string exchangeName, string queueName)
        {
            _exchangeName = exchangeName;
            _queueName = queueName;
            var factory = new ConnectionFactory
            {
                Uri = uri,
                AutomaticRecoveryEnabled = true
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
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
                _channel.ExchangeDeclare(_exchangeName, "fanout");
                var json = JsonConvert.SerializeObject(message, _jsonSerializerSettings);
                var bytes = Encoding.UTF8.GetBytes(json);
                _channel.BasicPublish(_exchangeName, "", null, bytes);
            });
        }

        public IObservable<Unit> Subscribe<TMessage>(Func<TMessage, Task> subscribeHandlerMethod)
        {
            return Observable.Start(() =>
            {
                _channel.ExchangeDeclare(_exchangeName, "fanout");
                _channel.QueueDeclare(_queueName, true, false, false, null);
                _channel.QueueBind(_queueName, _exchangeName, "");

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += async (sender, e) =>
                {
                    var body = e.Body;
                    var json = Encoding.UTF8.GetString(body);
                    var message = JsonConvert.DeserializeObject<TMessage>(json, _jsonSerializerSettings);
                    await subscribeHandlerMethod(message);
                    _channel.BasicAck(e.DeliveryTag, false);
                };

                _channel.BasicConsume(_queueName, true, consumer);
            });
        }
    }
}
