using System;
using System.Fabric;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using SoCreate.ServiceFabric.PubSub.Subscriber;

namespace SoCreate.ServiceFabric.PubSub.State
{
    /// <summary>
    /// Persistable reference to a Service.
    /// </summary>
    [DataContract]
    public class ServiceReferenceWrapper : ReferenceWrapper
    {
        public override string Name => ServiceReference.Description;

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
        /// <param name="routingKey">Optional routing key to filter messages based on content. 'Key=Value' where Key is a message property path and Value is the value to match with message payload content.</param>
        public ServiceReferenceWrapper(ServiceReference serviceReference, string routingKey = null)
            : base(routingKey)
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
            return unchecked((int)HashingHelper.HashString(identifier));
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
        public override Task PublishAsync(MessageWrapper message, IProxyFactories proxyFactories)
        {
            if (ShouldDeliverMessage(message))
            {
                var client = proxyFactories.CreateServiceProxy<ISubscriberService>(ServiceReference);

                return client.ReceiveMessageAsync(message);
            }

            return Task.FromResult(true);
        }
    }
}