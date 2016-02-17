using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using ServiceFabric.PubSubActors.Interfaces;

namespace ServiceFabric.PubSubActors.SubscriberActors
{
	/// <summary>
	/// Base class of a <see cref="StatelessActor"/> that can receive published messages from <see cref="ServiceFabric.PubSubActors.PublisherActors"/> Actors.
	/// </summary>
	public abstract class StatelessSubscriberActor : StatelessActor
	{
		/// <summary>
		/// Registers this Actor as a subscriber for messages of type <paramref name="messageType"/>.
		/// </summary>
		/// <returns></returns>
		public Task RegisterMessageTypeAsync(Type messageType) 
		{
			return this.CommonRegisterAsync(messageType);
		}

		/// <summary>
		/// Deserializes the provided <paramref name="message"/> Payload into an intance of type <typeparam name="TResult"></typeparam>
		/// </summary>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="message"></param>
		/// <returns></returns>
		protected TResult Deserialize<TResult>(MessageWrapper message)
		{
			return this.CommonDeserialize<TResult>(message);
		}
	}
}