using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CHV.Infrastructure.MessageBus.RabbitMq
{
    public class PubSubClientBus : RabbitBusClient, IPubSubClientBus
    {
        public PubSubClientBus(string uri, string exchangeName = "", string exchangeType = "", string queueName = "", string routingKey = "")
            : base(uri, exchangeName, exchangeType, queueName, routingKey)
        {
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

                consumer.Received += async (sender, eventArgs) =>
                {
                    var body = eventArgs.Body;
                    var json = Encoding.UTF8.GetString(body);
                    var message = JsonConvert.DeserializeObject<TMessage>(json, _jsonSerializerSettings);
                    await subscribeHandlerMethod(message);

                    _channel.BasicAck(deliveryTag: eventArgs.DeliveryTag, multiple: false);
                };

            });
        }
    }
}
