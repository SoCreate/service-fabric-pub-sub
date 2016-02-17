using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using ServiceFabric.PubSubActors.Interfaces;

namespace ServiceFabric.PubSubActors.PublisherActors
{
	/// <summary>
	/// Base class of a <see cref="StatelessActor"/> that can publish messages to <see cref="ISubscriberActor"/> Actors.
	/// </summary>
	public abstract class StatelessPublisherActor : StatelessActor
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