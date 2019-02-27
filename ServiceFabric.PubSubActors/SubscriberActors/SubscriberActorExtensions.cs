using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using ServiceFabric.PubSubActors.Interfaces;
using System;
using System.Threading.Tasks;

namespace ServiceFabric.PubSubActors.SubscriberActors
{
	/// <summary>
	/// Common operations for Actors to become Subscribers
	/// </summary>
	public static class SubscriberActorExtensions
	{
		/// <summary>
		/// Registers this Actor as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
		/// </summary>
		/// <returns></returns>
		[Obsolete("Use ServiceFabric.PubSubActors.Helpers.SubscriberActorHelper for testability")]
        public static async Task RegisterMessageTypeWithBrokerServiceAsync(this ActorBase actor, Type messageType, Uri brokerServiceName = null, string routingKey = null)
        {
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (brokerServiceName == null)
            {
                brokerServiceName = await PublisherServices.PublisherServiceExtensions.DiscoverBrokerServiceNameAsync(new Uri(actor.ApplicationName));
                if (brokerServiceName == null)
                {
                    throw new InvalidOperationException("No brokerServiceName was provided or discovered in the current application.");
                }
            }
            var brokerService = await PublisherActors.PublisherActorExtensions.GetBrokerServiceForMessageAsync(messageType.Name, brokerServiceName);
            await brokerService.RegisterSubscriberAsync(ActorReference.Get(actor), messageType.FullName, routingKey);
        }

        /// <summary>
        /// Unregisters this Actor as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <returns></returns>
		[Obsolete("Use ServiceFabric.PubSubActors.Helpers.SubscriberActorHelper for testability")]
        public static async Task UnregisterMessageTypeWithBrokerServiceAsync(this ActorBase actor, Type messageType, bool flushQueue, Uri brokerServiceName = null)
        {
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            if (actor == null) throw new ArgumentNullException(nameof(actor));

            if (brokerServiceName == null)
            {
                brokerServiceName = await PublisherServices.PublisherServiceExtensions.DiscoverBrokerServiceNameAsync(new Uri(actor.ApplicationName));
                if (brokerServiceName == null)
                {
                    throw new InvalidOperationException("No brokerServiceName was provided or discovered in the current application.");
                }
            }
            var brokerService = await PublisherActors.PublisherActorExtensions.GetBrokerServiceForMessageAsync(messageType.Name, brokerServiceName);
            await brokerService.UnregisterSubscriberAsync(ActorReference.Get(actor), messageType.FullName, flushQueue);
        }

        /// <summary>
        /// Deserializes the provided <paramref name="messageWrapper"/> Payload into an intance of type <typeparam name="TResult"></typeparam>
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public static TResult Deserialize<TResult>(this ActorBase actor, MessageWrapper messageWrapper)
        {
            return messageWrapper.CreateMessage<TResult>();
        }
    }
}
