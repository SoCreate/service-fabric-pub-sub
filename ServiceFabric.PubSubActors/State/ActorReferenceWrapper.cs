using System;
using System.Runtime.Serialization;
using Microsoft.ServiceFabric.Actors;

namespace ServiceFabric.PubSubActors.State
{
	/// <summary>
	/// Encapsulates an <see cref="Microsoft.ServiceFabric.Actors.ActorReference"/> to make it equatable.
	/// </summary>
	[DataContract]
	public class ActorReferenceWrapper : IEquatable<ActorReferenceWrapper>
    {
		private readonly ActorId _id;

		/// <summary>
		/// Gets the wrapped <see cref="Microsoft.ServiceFabric.Actors.ActorReference"/>
		/// </summary>
		[DataMember]
		public ActorReference ActorReference { get; set; }

		/// <summary>
		/// Creates a new instance, for Serializer use only.
		/// </summary>
		[Obsolete("Only for Serializer use.")]
		public ActorReferenceWrapper()
		{
		}

		/// <summary>
		/// Creates a new instance using the provided <see cref="Microsoft.ServiceFabric.Actors.ActorReference"/>.
		/// </summary>
		/// <param name="actorReference"></param>
		public ActorReferenceWrapper(ActorReference actorReference)
	    {
		    if (actorReference == null) throw new ArgumentNullException(nameof(actorReference));
		    ActorReference = actorReference;
			_id = actorReference.ActorId;
	    }

		/// <summary>
		/// Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		/// <returns>
		/// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
		/// </returns>
		/// <param name="other">An object to compare with this object.</param>
		public bool Equals(ActorReferenceWrapper other)
	    {
		    if (other == null) return false;
		    return Equals(other.ActorReference.ActorId, _id);
	    }

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <returns>
		/// true if the specified object  is equal to the current object; otherwise, false.
		/// </returns>
		/// <param name="obj">The object to compare with the current object. </param><filterpriority>2</filterpriority>
		public override bool Equals(object obj)
	    {
		    return Equals(obj as ActorReferenceWrapper);
	    }

		/// <summary>
		/// Serves as a hash function for a particular type. 
		/// </summary>
		/// <returns>
		/// A hash code for the current object.
		/// </returns>
		public override int GetHashCode()
	    {
		    return _id.GetHashCode();
	    }
    }
}
