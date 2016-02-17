using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace ServiceFabric.PubSubActors.Interfaces
{
	/// <summary>
	/// Acts as a registry for Subscriber Actors that publishing Actors can publish to.
	/// Don't forget to mark implementing <see cref="ActorBase"/> classes with
	/// the attribute <see cref="ActorServiceAttribute"/> like: [ActorService(Name = nameof(IBrokerActor))]
	/// </summary>
	public interface IBrokerActor : IActor
	{
		/// <summary>
		/// Registers this Actor as a subscriber for messages.
		/// </summary>
		/// <param name="actor">Reference to the actor to register.</param>
		Task RegisterSubscriberAsync(ActorReference actor);

		/// <summary>
		/// Takes a published message and forwards it (indirectly) to all Subscribers.
		/// </summary>
		/// <param name="message">The message to publish</param>
		/// <returns></returns>
		Task PublishMessageAsync(MessageWrapper message);

		/// <summary>
		/// Unregisters this Actor as a subscriber for messages.
		/// </summary>
		/// <param name="actor">Reference to the actor to unregister.</param>
		/// <param name="flushQueue">Publish any remaining messages.</param>
		Task UnregisterSubscriberAsync(ActorReference actor, bool flushQueue);
	}
}
