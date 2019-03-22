using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting;
using SoCreate.ServiceFabric.PubSub.State;

namespace SoCreate.ServiceFabric.PubSub
{
    /// <summary>
    /// Acts as a registry for Subscriber Actors and Services that publishing Actors and Services can publish to.
    /// </summary>
    public interface IBrokerService : IService
    {
        /// <summary>
        /// Registers a Service or Actor as a subscriber for messages.
        /// </summary>
        /// <param name="reference">Reference to the service or actor to register.</param>
        /// <param name="messageTypeName">The full type name of the message to subscribe to.</param>
        Task SubscribeAsync(ReferenceWrapper reference, string messageTypeName);

        /// <summary>
        /// Unregisters a Service or Actor as a subscriber for messages.
        /// </summary>
        /// <param name="messageTypeName">The full type name of the message to subscribe to.</param>
        /// <param name="reference">Reference to the service or actor to unregister.</param>
        Task UnsubscribeAsync(ReferenceWrapper reference, string messageTypeName);

        /// <summary>
        /// Takes a published message and forwards it (indirectly) to all Subscribers.
        /// </summary>
        /// <param name="message">The message to publish</param>
        /// <returns></returns>
        Task PublishMessageAsync(MessageWrapper message);

        /// <summary>
        /// Returns a list of <see cref="QueueStats"/> objects providing the total number of messages received and
        /// delivered for each queue.
        /// </summary>
        /// <returns></returns>
        Task<QueueStatsWrapper> GetBrokerStatsAsync();
    }
}
