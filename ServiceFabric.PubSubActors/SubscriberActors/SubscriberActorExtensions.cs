using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using Newtonsoft.Json;
using ServiceFabric.PubSubActors.Interfaces;

namespace ServiceFabric.PubSubActors.SubscriberActors
{
	/// <summary>
	/// Common operations for Actors to become Subscribers
	/// </summary>
	public static class SubscriberActorExtensions
	{
		/// <summary>
		/// Registers this Actor as a subscriber for messages of type <paramref name="messageType"/> using a <see cref="IRelayBrokerActor"/> approach.   
		/// The relay actor will register itself as subscriber to the broker actor, creating a fan out pattern for scalability.
		/// </summary>
		/// <param name="actor">The actor registering itself as a subscriber for messages of type <paramref name="messageType"/></param>
		/// <param name="messageType">The type of message to register for (each message type has its own <see cref="IBrokerActor"/> instance)</param>
		/// <param name="relayBrokerActorId">The ID of the relay broker to register with. Remember this ID in the caller, if you ever need to unregister.</param>
		/// <param name="sourceBrokerActorId">(optional) The ID of the source <see cref="IBrokerActor"/> to use as the source for the <paramref name="relayBrokerActorId"/> 
		/// Remember this ID in the caller, if you ever need to unregister.
		/// If not specified, the default <see cref="IBrokerActor"/> for the message type <paramref name="messageType"/> will be used.</param>
		/// <returns></returns>
		public static async Task RegisterMessageTypeWithRelayBrokerAsync(this ActorBase actor, Type messageType, ActorId relayBrokerActorId, ActorId sourceBrokerActorId)
		{
			if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            if (relayBrokerActorId == null) throw new ArgumentNullException(nameof(relayBrokerActorId));

			if (sourceBrokerActorId == null)
			{
				sourceBrokerActorId = new ActorId(messageType.FullName);
			}
			IRelayBrokerActor relayBrokerActor = ActorProxy.Create<IRelayBrokerActor>(relayBrokerActorId, actor.ApplicationName, nameof(IRelayBrokerActor));
			IBrokerActor brokerActor = ActorProxy.Create<IBrokerActor>(sourceBrokerActorId, actor.ApplicationName, nameof(IBrokerActor));

			//register relay as subscriber for broker
			await brokerActor.RegisterSubscriberAsync(ActorReference.Get(relayBrokerActor));
			//register caller as subscriber for relay broker
			await relayBrokerActor.RegisterSubscriberAsync(ActorReference.Get(actor));
		}

		/// <summary>
		/// Unregisters this Actor as a subscriber for messages of type <paramref name="messageType"/> using a <see cref="IRelayBrokerActor"/> approach.   
		/// </summary>
		/// <param name="actor">The actor registering itself as a subscriber for messages of type <paramref name="messageType"/></param>
		/// <param name="messageType">The type of message to unregister for (each message type has its own <see cref="IBrokerActor"/> instance)</param>
		/// <param name="relayBrokerActorId">The ID of the relay broker to unregister with.</param>
		/// <param name="sourceBrokerActorId">(optional) The ID of the source <see cref="IBrokerActor"/> that was used as the source for the <paramref name="relayBrokerActorId"/>
		/// If not specified, the default <see cref="IBrokerActor"/> for the message type <paramref name="messageType"/> will be used.</param>
		/// <param name="flushQueue">Publish any remaining messages.</param>
		/// <returns></returns>
		public static async Task UnregisterMessageTypeWithRelayBrokerAsync(this ActorBase actor, Type messageType, ActorId relayBrokerActorId, ActorId sourceBrokerActorId, bool flushQueue)
		{
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
			if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (relayBrokerActorId == null) throw new ArgumentNullException(nameof(relayBrokerActorId));

			if (sourceBrokerActorId == null)
			{
				sourceBrokerActorId = new ActorId(messageType.FullName);
			}
			IRelayBrokerActor relayBrokerActor = ActorProxy.Create<IRelayBrokerActor>(relayBrokerActorId, actor.ApplicationName, nameof(IRelayBrokerActor));
			IBrokerActor brokerActor = ActorProxy.Create<IBrokerActor>(sourceBrokerActorId, actor.ApplicationName, nameof(IBrokerActor));

			//unregister relay as subscriber for broker
			await brokerActor.UnregisterSubscriberAsync(ActorReference.Get(relayBrokerActor), flushQueue);
			//unregister caller as subscriber for relay broker
			await relayBrokerActor.UnregisterSubscriberAsync(ActorReference.Get(actor), flushQueue);
		}

		/// <summary>
		/// Registers this Actor as a subscriber for messages of type <paramref name="messageType"/>.
		/// </summary>
		/// <returns></returns>
		public static async Task RegisterMessageTypeAsync(this ActorBase actor, Type messageType)
		{
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
			if (actor == null) throw new ArgumentNullException(nameof(actor));
            ActorId actorId = new ActorId(messageType.FullName);
			IBrokerActor brokerActor = ActorProxy.Create<IBrokerActor>(actorId, actor.ApplicationName, nameof(IBrokerActor));
			await brokerActor.RegisterSubscriberAsync(ActorReference.Get(actor));
		}

		/// <summary>
		/// Unregisters this Actor as a subscriber for messages of type <paramref name="messageType"/>.
		/// </summary>
		/// <returns></returns>
		public static async Task UnregisterMessageTypeAsync(this ActorBase actor, Type messageType, bool flushQueue)
		{
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
			if (actor == null) throw new ArgumentNullException(nameof(actor));
            ActorId actorId = new ActorId(messageType.FullName);
			IBrokerActor brokerActor = ActorProxy.Create<IBrokerActor>(actorId, actor.ApplicationName, nameof(IBrokerActor));
			await brokerActor.UnregisterSubscriberAsync(ActorReference.Get(actor), flushQueue);
		}

        /// <summary>
		/// Registers this Actor as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
		/// </summary>
		/// <returns></returns>
		[Obsolete("Use ServiceFabric.PubSubActors.Helpers.SubscriberActorHelper for testability")]
        public static async Task RegisterMessageTypeWithBrokerServiceAsync(this ActorBase actor, Type messageType, Uri brokerServiceName = null)
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
            await brokerService.RegisterSubscriberAsync(ActorReference.Get(actor), messageType.FullName);
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
        /// Deserializes the provided <paramref name="message"/> Payload into an intance of type <typeparam name="TResult"></typeparam>
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public static TResult Deserialize<TResult>(this ActorBase actor, MessageWrapper message)
		{
			var payload = JsonConvert.DeserializeObject<TResult>(message.Payload);
			return payload;
		}
	}
}
