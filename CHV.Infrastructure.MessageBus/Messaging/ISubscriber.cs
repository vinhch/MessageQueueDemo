using System;

namespace CHV.Infrastructure.MessageBus
{
    public interface ISubscriber : IDisposable, IConsumer
    {
    }
}
