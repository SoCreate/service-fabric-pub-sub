using Microsoft.ServiceFabric.Actors.Runtime;
using PubkSubRelayActor.Interfaces;
using ServiceFabric.PubSubActors;
using ServiceFabric.PubSubActors.Interfaces;

namespace PubkSubRelayActor
{
	/// <remarks>
	/// A named relaying broker that has an <see cref="IBrokerActor"/> as its source and regular subscribers.
	/// </remarks>
	[StatePersistence(StatePersistence.Persisted)]
	[ActorService(Name = nameof(IRelayBrokerActor))]
	internal class PubkSubRelayActor : RelayBrokerActor, IPubkSubRelayActor
	{
		public PubkSubRelayActor()
		{
			ActorEventSourceMessageCallback = message => ActorEventSource.Current.ActorMessage(this, message);
		}
	}
}
