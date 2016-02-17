using System.Runtime.Serialization;
using ServiceFabric.PubSubActors.Interfaces;

namespace ServiceFabric.PubSubActors.State
{
	/// <summary>
	/// Combines a <see cref="MessageWrapper"/> and a DequeueCount.
	/// </summary>
	[DataContract]
	public class QueuedMessageWrapper
	{
		[DataMember]
		public int DequeueCount { get; set; }

		[DataMember]
		public MessageWrapper MessageWrapper { get; set; }
	}
}