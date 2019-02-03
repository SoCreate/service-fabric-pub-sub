using System;
using System.Threading.Tasks;

namespace ServiceFabric.PubSubActors.Helpers
{
    public interface ISubscriberHelper
    {
        /// <summary>
        /// Registers this Actor as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <returns></returns>
        Task RegisterMessageTypeAsync(Type messageType, bool flushQueue, Uri brokerServiceName = null, string listenerName = null);

        Task RegisterMessageTypeAsync(Type messageType, Uri brokerServiceName = null, string listenerName = null);

        Task UnregisterMessageTypeAsync(Type messageType, Uri brokerServiceName = null);

        Task UnregisterMessageTypeAsync(Type messageType, bool flushQueue, Uri brokerServiceName = null);
    }
}