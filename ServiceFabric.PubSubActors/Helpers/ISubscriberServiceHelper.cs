using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Look for Subscribe attributes and create a list of SubscriptionDefinitions to map message types to handlers.
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        Dictionary<Type, Func<object, Task>> DiscoverMessageHandlers(ISubscriberService service);

        /// <summary>
        /// Subscribe to all message types in <paramref name="messageTypes"/>.
        /// </summary>
        /// <param name="serviceReference"></param>
        /// <param name="messageTypes"></param>
        /// <param name="broker"></param>
        /// <returns></returns>
        Task SubscribeAsync(ServiceReference serviceReference, IEnumerable<Type> messageTypes, Uri broker = null);

        /// <summary>
        /// Given the <paramref name="messageWrapper"/>, invoke the appropriate handler in <paramref name="handlers"/>.
        /// </summary>
        /// <param name="messageWrapper"></param>
        /// <param name="handlers"></param>
        /// <returns></returns>
        Task ProccessMessageAsync(MessageWrapper messageWrapper, Dictionary<Type, Func<object, Task>> handlers);

        /// <summary>
        /// Creates a ServiceReference object for a StatelessService.  Used for subscribing/unsubscribing.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="listenerName"></param>
        /// <returns></returns>
        ServiceReference CreateServiceReference(StatelessService service, string listenerName = null);

        /// <summary>
        /// Creates a ServiceReference object for a StatelessService.  Used for subscribing/unsubscribing.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="listenerName"></param>
        /// <returns></returns>
        ServiceReference CreateServiceReference(StatefulService service, string listenerName = null);
    }
}