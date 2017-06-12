using System;

namespace CHV.Infrastructure.MessageBus.Messaging
{
    public interface IPublisher : IDisposable, IProducer
    {
    }
}
