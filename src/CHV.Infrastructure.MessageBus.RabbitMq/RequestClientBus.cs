﻿using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CHV.Infrastructure.MessageBus.RabbitMq
{
    public class RequestClientBus : RabbitBusClient, IMessageRequestor
    {
        private string _replyQueueName = "amq.rabbitmq.reply-to";

        public RequestClientBus(string uri, string exchangeName = "", string exchangeType = "",
            string queueName = "", string routingKey = "", string replyQueueName = "amq.rabbitmq.reply-to")
            : base(uri, exchangeName, exchangeType, queueName, routingKey)
        {
            _replyQueueName = replyQueueName;
        }

        /*
         * ~Publish
         * Note: The client needs to create its consumer before publishing the request
         * (otherwise the broker can't substitute reply_to correctly,
         * and you see that ChannelClose exception as the result,
         * or other exceptions)
         */
        public async Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message)
        {
            var corrId = Guid.NewGuid().ToString();

            #region create a consumer first to listen the reply
            var consumer = new EventingBasicConsumer(_channel);
            _channel.BasicConsume(_replyQueueName, true, consumer);

            var eventArgsObservable = Observable
                .FromEventPattern<BasicDeliverEventArgs>(
                    h => consumer.Received += h,
                    h => consumer.Received -= h)
                .Select(x => x.EventArgs);
            #endregion

            #region publishing message
            var props = _channel.CreateBasicProperties();
            props.ReplyTo = _replyQueueName;
            props.CorrelationId = corrId;

            var json = JsonConvert.SerializeObject(message, _jsonSerializerSettings);
            var bytes = Encoding.UTF8.GetBytes(json);
            _channel.BasicPublish(_exchangeName, _routingKey, props, bytes);
            #endregion

            #region read response message from the callback_queue
            var ea = await eventArgsObservable.FirstAsync(s => s.BasicProperties.CorrelationId == corrId);
            var responseJson = Encoding.UTF8.GetString(ea.Body);
            return JsonConvert.DeserializeObject<TResponse>(responseJson, _jsonSerializerSettings);
            #endregion
        }
    }
}
