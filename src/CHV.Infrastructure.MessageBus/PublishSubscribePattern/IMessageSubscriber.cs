using System;
using System.Reactive;
using System.Threading.Tasks;

namespace CHV.Infrastructure.MessageBus
{
    public interface IMessageSubscriber : IDisposable
    {
        IObservable<Unit> Subscribe<TMessage>(Func<TMessage, Task> subscribeHandler);
    }
}
