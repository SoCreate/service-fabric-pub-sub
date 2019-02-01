using System;
using System.Threading.Tasks;

namespace ServiceFabric.PubSubActors.SubscriberServices
{
    public interface ISubscriberStatelessService : ISubscriberService
    {
        /// <summary>
        /// Unsubscribe from the messageType.
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="flush"></param>
        /// <returns></returns>
        Task UnsubscribeAsync(Type messageType, bool flush = true);

        /// <summary>
        /// Subscribe to the messageType.
        /// </summary>
        /// <param name="messageType"></param>
        /// <returns></returns>
        Task SubscribeAsync(Type messageType);
    }
}