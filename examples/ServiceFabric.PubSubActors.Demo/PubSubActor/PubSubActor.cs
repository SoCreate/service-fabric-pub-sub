using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using PubSubActor.Interfaces;
using ServiceFabric.PubSubActors.Interfaces;

namespace PubSubActor
{
	/// <remarks>
	/// The default Broker Actor (instance per message type)
	/// </remarks>
	[ActorService(Name = nameof(IBrokerActor))]
	internal class PubSubActor : ServiceFabric.PubSubActors.BrokerActor, IPubSubActor
	{
		public PubSubActor(ActorService actorService, ActorId actorId) 
            :base(actorService, actorId)
		{
			ActorEventSourceMessageCallback = message => ActorEventSource.Current.ActorMessage(this, message);
		}
	}
}
