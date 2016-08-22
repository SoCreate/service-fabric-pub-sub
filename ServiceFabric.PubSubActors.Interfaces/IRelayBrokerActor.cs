using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace ServiceFabric.PubSubActors.Interfaces
{
	/// <summary>
	/// Acts as a relay registry for Subscriber Actors that <see cref="IBrokerActor"/> -Actors can publish to.
	/// Don't forget to mark implementing <see cref="Actor"/> classes with
	/// the attribute <see cref="ActorServiceAttribute"/> like: [ActorService(Name = nameof(IRelayBrokerActor))]
	/// </summary>
	public interface IRelayBrokerActor : ISubscriberActor
	{
		/// <summary>
		/// Registers an Actor as a subscriber for messages.
		/// </summary>
		/// <param name="actor">Reference to the actor to register.</param>
		Task RegisterSubscriberAsync(ActorReference actor);

		/// <summary>
		/// Unregisters an Actor as a subscriber for messages.
		/// </summary>
		/// <param name="actor">Reference to the actor to unregister.</param>
		/// <param name="flushQueue">Publish any remaining messages.</param>
		Task UnregisterSubscriberAsync(ActorReference actor, bool flushQueue);

		/// <summary>
		/// Registers a service as a subscriber for messages.
		/// </summary>
		/// <param name="service">Reference to the actor to register.</param>
		Task RegisterServiceSubscriberAsync(ServiceReference service);

		/// <summary>
		/// Unregisters a service as a subscriber for messages.
		/// </summary>
		/// <param name="actor">Reference to the actor to unregister.</param>
		/// <param name="flushQueue">Publish any remaining messages.</param>
		Task UnregisterServiceSubscriberAsync(ServiceReference actor, bool flushQueue);

		
	}
}
