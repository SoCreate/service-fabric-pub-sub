using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace SoCreate.ServiceFabric.PubSub.State
{
    internal class SubscriptionUnordered : ISubscription
    {
        public SubscriptionDetails SubscriptionDetails { get; }
        private readonly IReliableStateManager _stateManager;
        private IReliableConcurrentQueue<MessageWrapper> Queue { get; set; }

        public SubscriptionUnordered(IReliableStateManager stateManager, SubscriptionDetails subscriptionDetails)
        {
            _stateManager = stateManager;
            SubscriptionDetails = subscriptionDetails;
        }

        public async Task InitializeAsync()
        {
            Queue = await _stateManager.GetOrAddAsync<IReliableConcurrentQueue<MessageWrapper>>(SubscriptionDetails.QueueName);
        }

        public async Task InitializeAsync(ITransaction tx)
        {
            Queue = await _stateManager.GetOrAddAsync<IReliableConcurrentQueue<MessageWrapper>>(tx, SubscriptionDetails.QueueName);
        }

        public async Task EnqueueMessageAsync(ITransaction tx, MessageWrapper message)
        {
            var queueResult = await _stateManager.TryGetAsync<IReliableConcurrentQueue<MessageWrapper>>(SubscriptionDetails.QueueName);
            if (!queueResult.HasValue) return;

            await queueResult.Value.EnqueueAsync(tx, message);
        }

        public async Task<ConditionalValue<MessageWrapper>> DequeueMessageAsync(ITransaction tx, CancellationToken cancellationToken)
        {
            return await Queue.TryDequeueAsync(tx, cancellationToken);
        }

        public Task<long> GetQueueCount(CancellationToken cancellationToken)
        {
            return Task.FromResult(Queue.Count);
        }

        public async Task DeliverMessageAsync(MessageWrapper messageWrapper, IProxyFactories proxyFactories)
        {
            await SubscriptionDetails.ServiceOrActorReference.PublishAsync(messageWrapper, proxyFactories);
        }
    }
}