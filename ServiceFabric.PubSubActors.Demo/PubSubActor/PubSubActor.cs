using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using PubSubActor.Interfaces;
using ServiceFabric.PubSubActors.Interfaces;

namespace PubSubActor
{
	/// <remarks>
	/// Each ActorID maps to an instance of this class.
	/// The IProjName  interface (in a separate DLL that client code can
	/// reference) defines the operations exposed by ProjName objects.
	/// </remarks>
	[ActorService(Name = nameof(IBrokerActor))]
	internal class PubSubActor : ServiceFabric.PubSubActors.BrokerActor, IPubSubActor
	{
		public PubSubActor() 
		{
			ActorEventSourceMessageCallback = message => ActorEventSource.Current.ActorMessage(this, message);
		}
	}
}
