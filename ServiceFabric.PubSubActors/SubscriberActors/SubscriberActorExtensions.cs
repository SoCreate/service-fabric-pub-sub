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
		/// Registers this Actor as a subscriber for messages of type <paramref name="messageType"/>.
		/// </summary>
		/// <returns></returns>
		public static async Task RegisterMessageTypeAsync(this ActorBase actor, Type messageType)
		{
			if (actor == null) throw new ArgumentNullException(nameof(actor));
			ActorId actorId = new ActorId(messageType.FullName);
			IBrokerActor brokerActor = ActorProxy.Create<IBrokerActor>(actorId, actor.ApplicationName, nameof(IBrokerActor));
			await brokerActor.RegisterSubscriberAsync(ActorReference.Get(actor));
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
