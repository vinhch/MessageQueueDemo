using System;
using System.Threading.Tasks;

namespace CHV.Infrastructure.MessageBus
{
    public interface IMessageRespondent : IDisposable
    {
        Task RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> respondHandler);
    }
}
