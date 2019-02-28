using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.PubSubActors.State;

namespace ServiceFabric.PubSubActors.Helpers
{
    public interface IBrokerClient
    {
        /// <summary>
        /// Publish a message.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task PublishMessageAsync(object message);

        /// <summary>
        /// Registers this StatelessService as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="messageType"></param>
        /// <param name="handler"></param>
        /// <param name="listenerName"></param>
        /// <returns></returns>
        Task SubscribeAsync<T>(StatelessService service, Type messageType, Func<T, Task> handler, string listenerName = null) where T : class;

        /// <summary>
        /// Registers this StatefulService as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="messageType"></param>
        /// <param name="handler"></param>
        /// <param name="listenerName"></param>
        /// <returns></returns>
        Task SubscribeAsync<T>(StatefulService service, Type messageType, Func<T, Task> handler, string listenerName = null) where T : class;

        /// <summary>
        /// Registers this Actor as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="messageType"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        Task SubscribeAsync<T>(ActorBase actor, Type messageType, Func<T, Task> handler) where T : class;

        /// <summary>
        /// Unregisters this StatelessService as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="messageType"></param>
        /// <param name="flush"></param>
        /// <returns></returns>
        Task UnsubscribeAsync(StatelessService service, Type messageType, bool flush);

        /// <summary>
        /// Unregisters this StatefulService as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="messageType"></param>
        /// <param name="flush"></param>
        /// <returns></returns>
        Task UnsubscribeAsync(StatefulService service, Type messageType, bool flush);

        /// <summary>
        /// Unregisters this Actor as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="messageType"></param>
        /// <param name="flush"></param>
        /// <returns></returns>
        Task UnsubscribeAsync(ActorBase actor, Type messageType, bool flush);

        /// <summary>
        /// Given a <see cref="MessageWrapper"/>, call the handler given for that type when <see cref="SubscribeAsync"/> was called.
        /// </summary>
        /// <param name="messageWrapper"></param>
        /// <returns></returns>
        Task ProcessMessageAsync(MessageWrapper messageWrapper);
    }
}