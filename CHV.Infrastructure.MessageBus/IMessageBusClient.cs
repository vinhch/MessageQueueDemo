using System;

namespace CHV.Infrastructure.MessageBus
{
    public interface IMessageBusClient : IMessagePublisher, IMessageSubscriber, IDisposable
    {
    }
}
