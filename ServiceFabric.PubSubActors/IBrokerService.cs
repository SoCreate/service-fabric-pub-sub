using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting;

namespace ServiceFabric.PubSubActors
{
    /// <summary>
    /// Acts as a registry for Subscriber Actors and Services that publishing Actors and Services can publish to.
    /// </summary>
    public interface IBrokerService : IService
    {
        /// <summary>
        /// Registers an Actor as a subscriber for messages.
        /// </summary>
        /// <param name="actor">Reference to the actor to register.</param>
        /// <param name="messageTypeName">The full type name of the message to subscribe to.</param>
        Task RegisterSubscriberAsync(Microsoft.ServiceFabric.Actors.ActorReference actor, string messageTypeName);

        /// <summary>
        /// Unregisters an Actor as a subscriber for messages.
        /// </summary>
        /// <param name="messageTypeName">The full type name of the message to subscribe to.</param>
        /// <param name="actor">Reference to the actor to unregister.</param>
        /// <param name="flushQueue">Publish any remaining messages.</param>
        Task UnregisterSubscriberAsync(Microsoft.ServiceFabric.Actors.ActorReference actor, string messageTypeName, bool flushQueue);

        /// <summary>
        /// Registers a service as a subscriber for messages.
        /// </summary>
        /// <param name="messageTypeName">The full type name of the message to subscribe to.</param>
        /// <param name="service">Reference to the Service to register.</param>
        Task RegisterServiceSubscriberAsync(Interfaces.ServiceReference service, string messageTypeName);

        /// <summary>
        /// Unregisters a service as a subscriber for messages.
        /// </summary>
        /// <param name="messageTypeName">The full type name of the message to subscribe to.</param>
        /// <param name="service">Reference to the Service to unregister.</param>
        /// <param name="flushQueue">Publish any remaining messages.</param>
        Task UnregisterServiceSubscriberAsync(Interfaces.ServiceReference service, string messageTypeName, bool flushQueue);

        /// <summary>
        /// Takes a published message and forwards it (indirectly) to all Subscribers.
        /// </summary>
        /// <param name="message">The message to publish</param>
        /// <returns></returns>
        Task PublishMessageAsync(Interfaces.MessageWrapper message);
    }
}
