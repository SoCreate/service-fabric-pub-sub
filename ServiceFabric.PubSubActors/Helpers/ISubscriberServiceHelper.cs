using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Threading.Tasks;
using ServiceFabric.PubSubActors.Interfaces;
using ServiceFabric.PubSubActors.SubscriberServices;

namespace ServiceFabric.PubSubActors.Helpers
{
    public interface ISubscriberServiceHelper
    {
        /// <summary>
        /// Registers this Actor as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <returns></returns>
        Task RegisterMessageTypeAsync(StatelessService service, Type messageType,
            Uri brokerServiceName = null, string listenerName = null);

        /// <summary>
        /// Unregisters this Actor as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <returns></returns>
        Task UnregisterMessageTypeAsync(StatelessService service, Type messageType, bool flushQueue,
            Uri brokerServiceName = null);

        /// <summary>
        /// Registers this Actor as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <returns></returns>
        Task RegisterMessageTypeAsync(StatefulService service, Type messageType,
            Uri brokerServiceName = null, string listenerName = null);

        /// <summary>
        /// Unregisters this Actor as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <returns></returns>
        Task UnregisterMessageTypeAsync(StatefulService service, Type messageType, bool flushQueue,
            Uri brokerServiceName = null);

        Task SubscribeAsync(ISubscriberService service, ServiceReference serviceReference);

        Task ProccessMessageAsync(MessageWrapper messageWrapper);

        ServiceReference CreateServiceReference(StatelessService service, string listenerName = null);

        ServiceReference CreateServiceReference(StatefulService service, string listenerName = null);
    }
}