using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ServiceFabric.PubSubActors.Interfaces;
using ServiceFabric.PubSubActors.SubscriberServices;

namespace ServiceFabric.PubSubActors.State
{
	/// <summary>
	/// Persistable reference to a Service.
	/// </summary>
	[DataContract]
	public class ServiceReferenceWrapper : ReferenceWrapper
	{
		private readonly string _id;

		public override string Name
		{
			get { return ServiceReference.ID; }
		}

		/// <summary>
		/// Gets the wrapped <see cref="ServiceReference"/>
		/// </summary>
		[DataMember]
		public ServiceReference ServiceReference { get; set; }

		/// <summary>
		/// Creates a new instance, for Serializer use only.
		/// </summary>
		[Obsolete("Only for Serializer use.")]
		public ServiceReferenceWrapper()
		{
		}

		/// <summary>
		/// Creates a new instance using the provided <see cref="ServiceReference"/>.
		/// </summary>
		/// <param name="serviceReference"></param>
		public ServiceReferenceWrapper(ServiceReference serviceReference)
		{
			if (serviceReference == null) throw new ArgumentNullException(nameof(serviceReference));
			ServiceReference = serviceReference;
			_id = serviceReference.ID;
		}

		/// <summary>
		/// Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		/// <returns>
		/// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
		/// </returns>
		/// <param name="other">An object to compare with this object.</param>
		public bool Equals(ServiceReferenceWrapper other)
		{
			if (other?.ServiceReference?.ID == null) return false;
			return Equals(other.ServiceReference.ID, _id);
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
			return Equals(obj as ServiceReferenceWrapper);
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

		/// <summary>
		/// Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		/// <returns>
		/// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
		/// </returns>
		/// <param name="other">An object to compare with this object.</param>
		public override bool Equals(ReferenceWrapper other)
		{
			return Equals(other as ServiceReferenceWrapper);
		}

		/// <summary>
		/// Attempts to publish the message to a listener.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public override Task PublishAsync(MessageWrapper message)
		{
			var client = SubscriberServicePartitionClient.Create(ServiceReference);
			return client.ReceiveMessageAsync(message);
		}
	}
}