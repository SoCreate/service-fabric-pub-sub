using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using SoCreate.ServiceFabric.PubSub.Helpers;

namespace SoCreate.ServiceFabric.PubSub.State
{
    internal class SubscriptionOrdered : ISubscription
    {
        private readonly IReliableStateManager _stateManager;
        private IReliableQueue<MessageWrapper> Queue { get; set; }

        public SubscriptionDetails SubscriptionDetails { get; }

        public SubscriptionOrdered(IReliableStateManager stateManager, SubscriptionDetails subscriptionDetails)
        {
            _stateManager = stateManager;
            SubscriptionDetails = subscriptionDetails;
        }

        public async Task InitializeAsync()
        {
            Queue = await _stateManager.GetOrAddAsync<IReliableQueue<MessageWrapper>>(SubscriptionDetails.QueueName);
        }

        public async Task InitializeAsync(ITransaction tx)
        {
            Queue = await _stateManager.GetOrAddAsync<IReliableQueue<MessageWrapper>>(tx, SubscriptionDetails.QueueName);
        }

        public async Task EnqueueMessageAsync(ITransaction tx, MessageWrapper message)
        {
            var queueResult = await _stateManager.TryGetAsync<IReliableQueue<MessageWrapper>>(SubscriptionDetails.QueueName);
            if (!queueResult.HasValue) return;

            await queueResult.Value.EnqueueAsync(tx, message);
        }

        public async Task<ConditionalValue<MessageWrapper>> DequeueMessageAsync(ITransaction tx, CancellationToken cancellationToken)
        {
            return await Queue.TryDequeueAsync(tx);
        }

        public async Task<long> GetQueueCount(CancellationToken cancellationToken)
        {
            return await TimeoutRetryHelper.ExecuteInTransaction(_stateManager, (tx, token, state) => Queue.GetCountAsync(tx), cancellationToken: cancellationToken);
        }

        public async Task DeliverMessageAsync(MessageWrapper messageWrapper, IProxyFactories proxyFactories)
        {
            await SubscriptionDetails.ServiceOrActorReference.PublishAsync(messageWrapper, proxyFactories);
        }
    }
}