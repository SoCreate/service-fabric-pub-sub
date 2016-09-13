using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace ServiceFabric.PubSubActors.Helpers
{
    /// <summary>
    /// Common operations for Actors to become Subscribers
    /// </summary>
    public class SubscriberActorHelper : ISubscriberActorHelper
    {

        private readonly IBrokerServiceLocator _brokerServiceLocator;

        public SubscriberActorHelper()
        {
            _brokerServiceLocator = new BrokerServiceLocator();
        }

        public SubscriberActorHelper(IBrokerServiceLocator brokerServiceLocator)
        {
            _brokerServiceLocator = brokerServiceLocator;
        }
        /// <summary>
        /// Registers this Actor as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <returns></returns>
        public async Task RegisterMessageTypeAsync(ActorBase actor, Type messageType, Uri brokerServiceName = null)
        {
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (brokerServiceName == null)
            {
                brokerServiceName = await PublisherServiceHelper.DiscoverBrokerServiceNameAsync();
                if (brokerServiceName == null)
                {
                    throw new InvalidOperationException("No brokerServiceName was provided or discovered in the current application.");
                }
            }
            var brokerService = await _brokerServiceLocator.GetBrokerServiceForMessageAsync(messageType.Name, brokerServiceName);
            await brokerService.RegisterSubscriberAsync(ActorReference.Get(actor), messageType.FullName);
        }

        /// <summary>
        /// Unregisters this Actor as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <returns></returns>
        public async Task UnregisterMessageTypeAsync(ActorBase actor, Type messageType, bool flushQueue,
            Uri brokerServiceName = null)
        {
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            if (actor == null) throw new ArgumentNullException(nameof(actor));

            if (brokerServiceName == null)
            {
                brokerServiceName = await PublisherServiceHelper.DiscoverBrokerServiceNameAsync();
                if (brokerServiceName == null)
                {
                    throw new InvalidOperationException("No brokerServiceName was provided or discovered in the current application.");
                }
            }
            var brokerService = await _brokerServiceLocator.GetBrokerServiceForMessageAsync(messageType.Name, brokerServiceName);
            await brokerService.UnregisterSubscriberAsync(ActorReference.Get(actor), messageType.FullName, flushQueue);
        }
    }
}