using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ServiceFabric.PubSubActors.State;

namespace ServiceFabric.PubSubActors.Helpers
{
    public interface IBrokerClient
    {
        /// <summary>
        /// Publish a message of type <typeparam name="T"></typeparam>.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task PublishMessageAsync<T>(T message) where T : class;

        /// <summary>
        /// Registers this Service or Actor as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <param name="referenceWrapper"></param>
        /// <param name="messageType"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        Task SubscribeAsync<T>(ReferenceWrapper referenceWrapper, Type messageType, Func<T, Task> handler) where T : class;

        /// <summary>
        /// Unregisters this Service or Actor as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <param name="referenceWrapper"></param>
        /// <param name="messageType"></param>
        /// <returns></returns>
        Task UnsubscribeAsync(ReferenceWrapper referenceWrapper, Type messageType);

        /// <summary>
        /// Given a <see cref="MessageWrapper"/>, call the handler given for that type when <see cref="SubscribeAsync{T}"/> was called.
        /// </summary>
        /// <param name="messageWrapper"></param>
        /// <returns></returns>
        Task ProcessMessageAsync(MessageWrapper messageWrapper);

        /// <summary>
        /// Get the current stats from the Broker for each queue.  Includes information on how many messages have been
        /// received and delivered.
        /// </summary>
        /// <returns></returns>
        Task<Dictionary<string, List<QueueStats>>> GetBrokerStatsAsync();

        /// <summary>
        /// Deletes the queue <paramref name="queueName"/> from the Broker.  Useful for a Broker Monitor service to unsubscribe
        /// a service or actor from a message type.  The queueName can be found with a call to <see cref="GetBrokerStatsAsync"/>.
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns></returns>
        Task UnsubscribeByQueueNameAsync(string queueName);
    }
}