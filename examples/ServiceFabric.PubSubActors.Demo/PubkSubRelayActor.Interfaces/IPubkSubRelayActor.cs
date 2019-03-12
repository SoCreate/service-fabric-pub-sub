using Microsoft.ServiceFabric.Actors;
using ServiceFabric.PubSubActors.Interfaces;

namespace PubkSubRelayActor.Interfaces
{
	/// <summary>
	/// This interface defines the methods exposed by an actor.
	/// Clients use this interface to interact with the actor that implements it.
	/// </summary>
	public interface IPubkSubRelayActor : IRelayBrokerActor, IActor
	{
	}
}
