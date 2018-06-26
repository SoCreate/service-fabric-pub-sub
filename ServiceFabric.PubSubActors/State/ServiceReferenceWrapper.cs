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
        public ServiceReferenceWrapper(ServiceReference serviceReference)
        {
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
            return Equals(other?.GetHashCode(), GetHashCode());
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
            // ReSharper disable NonReadonlyMemberInGetHashCode - need to support Serialization.
            var identifier = GetServiceIdentifier();
            return identifier.GetHashCode();
        }

        /// <summary>
        /// Gets a string that can be hashed and compared to see if one service reference points to the same service as another.
        /// </summary>
        /// <returns></returns>
        private string GetServiceIdentifier()
        {
            string identifier = $"{ServiceReference.ApplicationName}-{ServiceReference.ServiceUri}";

            switch (ServiceReference.PartitionKind)
            {
                case ServicePartitionKind.Singleton:
                    break;
                case ServicePartitionKind.Int64Range:
                    identifier = $"{identifier}-{ServiceReference.PartitionKey}";
                    break;
                case ServicePartitionKind.Named:
                    identifier = $"{identifier}-{ServiceReference.PartitionName}";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return identifier;
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

        /// <inheritdoc />
        public override Task PublishAsync(MessageWrapper message)
        {
            return MessageWrapperExtensions.PublishAsync(this, message);
        }
    }


    internal static class MessageWrapperExtensions
    {
        private static readonly Lazy<IServiceProxyFactory> ServiceProxyFactoryLazy = new Lazy<IServiceProxyFactory>(()=> new ServiceProxyFactory());


        /// <summary>
        /// Attempts to publish the message to a listener.
        /// </summary>
        /// <param name="wrapper"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Task PublishAsync(this ServiceReferenceWrapper wrapper, MessageWrapper message)
        {
            ServicePartitionKey partitionKey;
            switch (wrapper.ServiceReference.PartitionKind)
            {
                case ServicePartitionKind.Singleton:
                    partitionKey = ServicePartitionKey.Singleton;
                    break;
                case ServicePartitionKind.Int64Range:
                    partitionKey = new ServicePartitionKey(wrapper.ServiceReference.PartitionKey);
                    break;
                case ServicePartitionKind.Named:
                    partitionKey = new ServicePartitionKey(wrapper.ServiceReference.PartitionName);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            var client = ServiceProxyFactoryLazy.Value.CreateServiceProxy<ISubscriberService>(wrapper.ServiceReference.ServiceUri, partitionKey);
            return client.ReceiveMessageAsync(message);
        }

        /// <summary>
        /// Attempts to publish the message to a listener.
        /// </summary>
        /// <param name="wrapper"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Task PublishAsync(this ActorReferenceWrapper wrapper, MessageWrapper message)
        {
            ISubscriberActor actor = (ISubscriberActor)wrapper.ActorReference.Bind(typeof(ISubscriberActor));
            return actor.ReceiveMessageAsync(message);
        }
    }
}