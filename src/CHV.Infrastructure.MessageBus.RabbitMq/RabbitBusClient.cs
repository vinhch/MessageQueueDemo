using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;

namespace CHV.Infrastructure.MessageBus.RabbitMq
{
    public abstract class RabbitBusClient : IDisposable
    {
        protected readonly IModel _channel;
        protected readonly IConnection _connection;
        protected readonly string _exchangeName;
        protected readonly string _exchangeType;
        protected readonly string _queueName;
        protected readonly string _routingKey;

        protected bool _disposed;
        protected JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        protected EventingBasicConsumer _consumer;
        protected virtual void CreateConsumer()
        {
            if (_consumer != null) return;

            _consumer = new EventingBasicConsumer(_channel);
            _channel.BasicConsume(queue: _queueName, noAck: false, consumer: _consumer);
        }

        /*
         * To create exchange, must have exchangeName & exchangeType
         * To create a queue must have queueName
         * To start add messages to a queues, must bind exchanges to queues using routingKeys
         *
         * Publish & Request need routingKey and exchangeName
         * Subscribe & Respond need queueName
         */
        public RabbitBusClient(string uri, string exchangeName = "", string exchangeType = "", string queueName = "", string routingKey = "")
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

            // create a exchange
            if (!string.IsNullOrWhiteSpace(_exchangeName) && !string.IsNullOrWhiteSpace(_exchangeType))
            {
                _channel.ExchangeDeclare(exchange: _exchangeName, type: _exchangeType);
            }

            // create a queue
            if (!string.IsNullOrWhiteSpace(_queueName))
            {
                _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false);
            }

            // create a binding between exchange and queue using routingKey
            if (!string.IsNullOrWhiteSpace(_routingKey)
                && !string.IsNullOrWhiteSpace(_exchangeName)
                && !string.IsNullOrWhiteSpace(_queueName))
            {
                _channel.QueueBind(queue: _queueName, exchange: _exchangeName,
                    routingKey: _routingKey, arguments: null);
            }

            // Configure how we receive messages
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false); // Process only one message at a time
        }

        public virtual void Dispose()
        {
            if (_disposed) return;
            _channel.Dispose();
            _connection.Dispose();
            _disposed = true;
        }
    }
}
