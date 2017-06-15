using System;
using System.Reactive;

namespace CHV.Infrastructure.MessageBus
{
    public interface IMessagePublisher : IDisposable
    {
        IObservable<Unit> Publish<TMessage>(TMessage message);
    }
}
