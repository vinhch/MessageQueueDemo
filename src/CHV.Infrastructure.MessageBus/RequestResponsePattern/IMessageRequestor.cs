using System;
using System.Threading.Tasks;

namespace CHV.Infrastructure.MessageBus
{
    public interface IMessageRequestor : IDisposable
    {
        Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message);
    }
}
