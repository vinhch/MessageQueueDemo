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
        private string _replyQueueName = "amq.rabbitmq.reply-to";
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
            //_channel.QueueBind(_queueName, _exchangeName, _routingKey, null);
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
                _channel.BasicConsume(_queueName, false, consumer);

                consumer.Received += async (sender, e) =>
                {
                    var body = e.Body;
                    var json = Encoding.UTF8.GetString(body);
                    var message = JsonConvert.DeserializeObject<TMessage>(json, _jsonSerializerSettings);
                    await subscribeHandlerMethod(message);

                    _channel.BasicAck(deliveryTag: e.DeliveryTag, multiple: false);
                };

            });
        }

        // ~Publish
        public async Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message)
        {
            var corrId = Guid.NewGuid().ToString();

            var props = _channel.CreateBasicProperties();
            props.ReplyTo = _replyQueueName;
            props.CorrelationId = corrId;

            var json = JsonConvert.SerializeObject(message, _jsonSerializerSettings);
            var bytes = Encoding.UTF8.GetBytes(json);
            _channel.BasicPublish(_exchangeName, _routingKey, props, bytes);

            #region read response message from the callback_queue
            var consumer = new EventingBasicConsumer(_channel);
            _channel.BasicConsume(_replyQueueName, true, consumer);

            var eventArgsObservable = Observable
                .FromEventPattern<BasicDeliverEventArgs>(
                    h => consumer.Received += h,
                    h => consumer.Received -= h)
                .Select(x => x.EventArgs);

            var ea = await eventArgsObservable.FirstAsync(s => s.BasicProperties.CorrelationId == corrId);
            var responseJson = Encoding.UTF8.GetString(ea.Body);
            return JsonConvert.DeserializeObject<TResponse>(responseJson, _jsonSerializerSettings);
            #endregion
        }

        // ~Subscribe
        public async Task RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> messageHandlerMethod)
        {
            var consumer = new EventingBasicConsumer(_channel);
            _channel.BasicConsume(_queueName, false, consumer);

            object response = null;

            consumer.Received += async (sender, e) =>
            {
                var props = e.BasicProperties;
                var replyProps = _channel.CreateBasicProperties();
                replyProps.CorrelationId = props.CorrelationId;

                var body = e.Body;
                try
                {
                    var json = Encoding.UTF8.GetString(body);
                    var message = JsonConvert.DeserializeObject<TRequest>(json, _jsonSerializerSettings);

                    response = await messageHandlerMethod(message);
                }
                catch (Exception ex)
                {
                    response = ex;
                }
                finally
                {
                    var responseJson = JsonConvert.SerializeObject(response, _jsonSerializerSettings);
                    var responseBytes = Encoding.UTF8.GetBytes(responseJson);

                    //_channel.BasicPublish(exchange: "", routingKey: props.ReplyTo, basicProperties: replyProps,
                    //    body: responseBytes);
                    _channel.BasicPublish(exchange: "", routingKey: _replyQueueName, basicProperties: replyProps,
                        body: responseBytes);
                    _channel.BasicAck(deliveryTag: e.DeliveryTag, multiple: false);
                }
            };
        }
    }
}
