﻿using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading.Tasks;

namespace CHV.Infrastructure.MessageBus.RabbitMq
{
    public class RespondClientBus : RabbitBusClient, IMessageRespondent
    {
        public RespondClientBus(string uri, string exchangeName = "", string exchangeType = "",
            string queueName = "", string routingKey = "")
            : base(uri, exchangeName, exchangeType, queueName, routingKey)
        {
        }

        /*
         * ~Subscribe
         */
        public async Task RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> messageHandlerMethod)
        {
            var consumer = new EventingBasicConsumer(_channel);
            _channel.BasicConsume(_queueName, false, consumer);

            object response = null;

            consumer.Received += async (sender, eventArgs) =>
            {
                var props = eventArgs.BasicProperties;
                var replyProps = _channel.CreateBasicProperties();
                replyProps.CorrelationId = props.CorrelationId;

                var body = eventArgs.Body;
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

                    _channel.BasicPublish(exchange: "", routingKey: props.ReplyTo, basicProperties: replyProps,
                        body: responseBytes);
                    //_channel.BasicPublish(exchange: "", routingKey: _replyQueueName, basicProperties: replyProps,
                    //    body: responseBytes);
                    _channel.BasicAck(deliveryTag: eventArgs.DeliveryTag, multiple: false);
                }
            };
        }
    }
}
