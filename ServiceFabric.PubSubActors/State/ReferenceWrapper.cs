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
	public abstract class ReferenceWrapper : IEquatable<ReferenceWrapper>, IComparable<ReferenceWrapper>
	{
		public abstract string Name { get; }

		public abstract bool Equals(ReferenceWrapper other);

		/// <summary>
		/// Attempts to publish the message to a listener.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public abstract Task PublishAsync(MessageWrapper message);

        int IComparable<ReferenceWrapper>.CompareTo(ReferenceWrapper other)
        {
            if (other == null) return -1;
            return other.GetHashCode().CompareTo(GetHashCode());
        }

        /// <summary>
        /// Creates a queuename to use for this reference. (not message specific)
        /// </summary>
        /// <returns></returns>
	    public string GetQueueName()
	    {
	        return GetHashCode().ToString();
	    }
        /// <summary>
        /// Creates a deadletter queuename to use for this reference. (not message specific)
        /// </summary>
        /// <returns></returns>
        public string GetDeadLetterQueueName()
        {
            return $"DeadLetters_{GetQueueName()}";
        }
    }
}