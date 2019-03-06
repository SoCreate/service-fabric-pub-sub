using System;
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
    }
}