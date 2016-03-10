using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ServiceFabric.PubSubActors.Interfaces;

namespace ServiceFabric.PubSubActors.State
{
	/// <summary>
	/// Encapsulates an <see cref="Microsoft.ServiceFabric.Actors.ActorReference"/> or <see cref="ServiceReference"/> to make it equatable.
	/// </summary>
	[DataContract]
	[KnownType(typeof(ActorReferenceWrapper))]
	[KnownType(typeof(ServiceReferenceWrapper))]
	public abstract class ReferenceWrapper : IEquatable<ReferenceWrapper>
	{
		public abstract string Name { get; }

		public abstract bool Equals(ReferenceWrapper other);

		/// <summary>
		/// Attempts to publish the message to a listener.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public abstract Task PublishAsync(MessageWrapper message);
	}
}