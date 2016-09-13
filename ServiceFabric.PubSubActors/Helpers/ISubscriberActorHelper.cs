using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace ServiceFabric.PubSubActors.Helpers
{
    public interface ISubscriberActorHelper
    {
        /// <summary>
        /// Registers this Actor as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <returns></returns>
        Task RegisterMessageTypeAsync(ActorBase actor, Type messageType, Uri brokerServiceName = null);

        /// <summary>
        /// Unregisters this Actor as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <returns></returns>
        Task UnregisterMessageTypeAsync(ActorBase actor, Type messageType, bool flushQueue,
            Uri brokerServiceName = null);
    }
}