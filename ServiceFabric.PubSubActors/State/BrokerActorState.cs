using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.ServiceFabric.Actors;

namespace ServiceFabric.PubSubActors.State
{
	/// <summary>
	/// State for <see cref="BrokerActor"/>. Contains a regular queue and a dead letter queue for every registered Actor.
	/// </summary>
	[DataContract]
	public sealed class BrokerActorState
	{
		/// <summary>
		/// Contains messages that could not be be sent to subscribed Actors. (has a limit)
		/// </summary>
		[DataMember]
		public Dictionary<ActorReference, Queue<QueuedMessageWrapper>> ActorDeadLetters { get; set; }
		
		/// <summary>
		/// Contains messages to be sent to subscribed Actors.
		/// </summary>
		[DataMember]
		public Dictionary<ActorReference, Queue<QueuedMessageWrapper>> ActorMessages{ get; set; }
	}
}