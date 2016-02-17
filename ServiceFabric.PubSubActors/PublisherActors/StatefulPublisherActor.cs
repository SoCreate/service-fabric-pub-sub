using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using ServiceFabric.PubSubActors.Interfaces;

namespace ServiceFabric.PubSubActors.PublisherActors
{
	/// <summary>
	/// Base class of a <see cref="StatefulActor{TState}"/> that can publish messages to <see cref="ISubscriberActor"/> Actors.
	/// </summary>
	/// <typeparam name="TState">Serializable state</typeparam>
	public abstract class StatefulPublisherActor<TState> : StatefulActor<TState> where TState : class
	{
		/// <summary>
		/// Publishes the provided <paramref name="message"/> to all registered <see cref="ISubscriberActor"/> Actors. 
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		protected Task PublishMessageAsync(object message)
		{
			return this.CommonPublishMessageAsync(message);
		}
	}

	/// <summary>
	/// Base class of a <see cref="StatefulActor"/> that can publish messages to <see cref="ISubscriberActor"/> Actors.
	/// </summary>
	public abstract class StatefulPublisherActor : StatefulActor
	{
		/// <summary>
		/// Publishes the provided <paramref name="message"/> to all registered <see cref="ISubscriberActor"/> Actors. 
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		protected Task PublishMessageAsync(object message)
		{
			return this.CommonPublishMessageAsync(message);
		}
	}
}