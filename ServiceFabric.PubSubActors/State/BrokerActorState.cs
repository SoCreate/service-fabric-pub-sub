using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceFabric.PubSubActors.Interfaces;

namespace ServiceFabric.PubSubActors.State
{
	/// <summary>
	/// State for <see cref="BrokerActor"/>. Contains a regular queue and a dead letter queue for every registered listener.
	/// </summary>
	[DataContract]
	public sealed class BrokerActorState
	{
		/// <summary>
		/// Contains messages that could not be be sent to subscribed listeners. (has a limit)
		/// </summary>
		[DataMember]
		public Dictionary<ReferenceWrapper, Queue<MessageWrapper>> SubscriberDeadLetters { get; set; }

		/// <summary>
		/// Contains messages to be sent to subscribed listeners.
		/// </summary>
		[DataMember]
		public Dictionary<ReferenceWrapper, Queue<MessageWrapper>> SubscriberMessages{ get; set; }
	}
}