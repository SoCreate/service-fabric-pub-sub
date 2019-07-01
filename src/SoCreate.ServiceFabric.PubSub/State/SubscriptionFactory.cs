using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;

namespace SoCreate.ServiceFabric.PubSub.State
{
    public class SubscriptionFactory
    {
        private readonly IReliableStateManager _stateManager;

        public SubscriptionFactory(IReliableStateManager stateManager)
        {
            _stateManager = stateManager;
        }
        
        public async Task<ISubscription> CreateAsync(SubscriptionDetails subscriptionDetails)
        {
            var subscription = Create(subscriptionDetails);
            await subscription.InitializeAsync();

            return subscription;
        }

        public async Task<ISubscription> CreateAsync(ITransaction tx, SubscriptionDetails subscriptionDetails)
        {
            var subscription = Create(subscriptionDetails);
            await subscription.InitializeAsync(tx);

            return subscription;
        }

        private ISubscription Create(SubscriptionDetails subscriptionDetails)
        {
            var subscriptionType = subscriptionDetails.IsOrdered ? typeof(SubscriptionOrdered) : typeof(SubscriptionUnordered);
            return (ISubscription)Activator.CreateInstance(subscriptionType, _stateManager, subscriptionDetails);
        }
    }
}