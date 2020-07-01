using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;

namespace SoCreate.ServiceFabric.PubSub.State
{
    public interface ISubscription
    {
        SubscriptionDetails SubscriptionDetails { get; }

        Task InitializeAsync();

        Task InitializeAsync(ITransaction tx);

        Task EnqueueMessageAsync(ITransaction tx, MessageWrapper message);

        Task<ConditionalValue<MessageWrapper>> DequeueMessageAsync(ITransaction tx, CancellationToken cancellationToken);

        Task<long> GetQueueCount(CancellationToken cancellationToken);

        Task DeliverMessageAsync(MessageWrapper messageWrapper, IProxyFactories proxyFactories);
    }
}