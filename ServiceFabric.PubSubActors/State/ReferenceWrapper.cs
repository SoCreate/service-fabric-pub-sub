using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
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
        private readonly string[] _routingKeyValue;
        private IHashingHelper _hashingHelper;

        /// <summary>
        /// Gets a hash helper to determine consistent string hashes.
        /// </summary>
        protected IHashingHelper HashingHelper
        {
            get
            {
                if (_hashingHelper == null)
                {
                    _hashingHelper = new HashingHelper();
                }

                return _hashingHelper;
            }
        }

        /// <summary>
        /// Gets a logical name for this reference
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the optional routing key.
        /// </summary>
        [DataMember]
        public string RoutingKey { get; private set; }

        /// <inheritdoc />
        public abstract bool Equals(ReferenceWrapper other);

        /// <summary>
        /// Creates a new instance with an optional routing key.
        /// </summary>
        /// <param name="routingKey">Optional routing key to filter messages based on content. 'Key=Value' where Key is a message property path and Value is the value to match with message payload content.</param>
        protected ReferenceWrapper(string routingKey = null)
        {
            _routingKeyValue = routingKey?.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
            if (_routingKeyValue != null && _routingKeyValue.Length != 2) throw new ArgumentException($"When {nameof(routingKey)} is provided, it must be similar to 'Key=Value'.");
            RoutingKey = routingKey;
        }

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
        /// Creates a queue name to use for this reference. (not message specific)
        /// </summary>
        /// <returns></returns>
        public string GetQueueName()
        {
            return GetHashCode().ToString();
        }
        /// <summary>
        /// Creates a dead-letter queue name to use for this reference. (not message specific)
        /// </summary>
        /// <returns></returns>
        public string GetDeadLetterQueueName()
        {
            return $"DeadLetters_{GetQueueName()}";
        }

        /// <summary>
        /// Determines whether to deliver the message to the subscriber, based on <see cref="RoutingKey"/> and <see cref="MessageWrapper.Payload"/>.
        /// Not intended to be called from user code.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool ShouldDeliverMessage(MessageWrapper message)
        {
            if (!(Interfaces.MessageWrapperExtensions.PayloadSerializer is DefaultPayloadSerializer))
                return true;
            if (_routingKeyValue == null)
                return true;

            var token = Interfaces.MessageWrapperExtensions.PayloadSerializer.Deserialize<JToken>(message.Payload);
            string value = (string)token.SelectToken(_routingKeyValue[0]);

            return string.Equals(_routingKeyValue[1], value, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}