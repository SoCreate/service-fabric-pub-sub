using System;
using System.Fabric;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
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
        private IServiceProxyFactory _serviceProxyFactory;

        [IgnoreDataMember]
        private IServiceProxyFactory ServiceProxyFactory
        {
            get
            {
                if (_serviceProxyFactory == null)
                {
                    _serviceProxyFactory = new ServiceProxyFactory();
                }

                return _serviceProxyFactory;
            }
        }

        public override string Name
        {
            get { return ServiceReference.Description; }
        }

        /// <summary>
        /// Gets the wrapped <see cref="ServiceReference"/>
        /// </summary>
        [DataMember]
        public ServiceReference ServiceReference { get; private set; }

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
        /// <param name="serviceProxyFactory">Optionak</param>
        public ServiceReferenceWrapper(ServiceReference serviceReference, IServiceProxyFactory serviceProxyFactory = null)
        {
            _serviceProxyFactory = serviceProxyFactory ?? new ServiceProxyFactory();
            ServiceReference = serviceReference ?? throw new ArgumentNullException(nameof(serviceReference));
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
            if (other?.ServiceReference?.PartitionGuid == null) return false;
            return Equals(other.ServiceReference.PartitionGuid, ServiceReference.PartitionGuid);
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
            // ReSharper disable NonReadonlyMemberInGetHashCode  - need to support Serialization.
            return ServiceReference.PartitionGuid.GetHashCode();
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
            ServicePartitionKey partitionKey;
            switch (ServiceReference.PartitionKind)
            {
                case ServicePartitionKind.Singleton:
                    partitionKey = ServicePartitionKey.Singleton;
                    break;
                case ServicePartitionKind.Int64Range:
                    partitionKey = new ServicePartitionKey(ServiceReference.PartitionKey);
                    break;
                case ServicePartitionKind.Named:
                    partitionKey = new ServicePartitionKey(ServiceReference.PartitionName);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            var client = ServiceProxyFactory.CreateServiceProxy<ISubscriberService>(ServiceReference.ServiceUri, partitionKey);
            return client.ReceiveMessageAsync(message);
        }
    }
}