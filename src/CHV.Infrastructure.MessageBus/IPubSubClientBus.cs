using System;

namespace CHV.Infrastructure.MessageBus
{
    public interface IPubSubClientBus : IMessagePublisher, IMessageSubscriber, IDisposable
    {
    }
}
