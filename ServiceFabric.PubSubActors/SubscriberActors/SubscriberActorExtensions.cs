using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Newtonsoft.Json;
using ServiceFabric.PubSubActors.Interfaces;

namespace ServiceFabric.PubSubActors.SubscriberActors
{
	/// <summary>
	/// Common operations for Actors of type <see cref="ServiceFabric.PubSubActors.SubscriberActors"/>
	/// </summary>
	internal static class SubscriberActorExtensions
	{
		/// <summary>
		/// Registers this Actor as a subscriber for messages of type <paramref name="messageType"/>.
		/// </summary>
		/// <returns></returns>
		public static async Task CommonRegisterAsync(this ActorBase actor, Type messageType)
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
		public static TResult CommonDeserialize<TResult>(this ActorBase actor, MessageWrapper message)
		{
			var payload = JsonConvert.DeserializeObject<TResult>(message.Payload);
			return payload;
		}
	}
}
