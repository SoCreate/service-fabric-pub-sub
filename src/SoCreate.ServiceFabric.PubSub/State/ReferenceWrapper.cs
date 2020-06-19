using System;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SoCreate.ServiceFabric.PubSub.Helpers;

namespace SoCreate.ServiceFabric.PubSub.State
{
    /// <summary>
    /// Encapsulates an <see cref="Microsoft.ServiceFabric.Actors.ActorReference"/> or <see cref="ServiceReference"/> to make it equatable.
    /// </summary>
    [DataContract]
    [KnownType(typeof(ActorReferenceWrapper))]
    [KnownType(typeof(ServiceReferenceWrapper))]
    public abstract class ReferenceWrapper : IEquatable<ReferenceWrapper>, IComparable<ReferenceWrapper>
    {
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
        /// Gets the optional routing key name.
        /// </summary>
        [DataMember]
        public string RoutingKeyName { get; private set; }
        /// <summary>
        /// Gets the optional routing key value.
        /// </summary>
        [DataMember]
        public string RoutingKeyValue { get; private set; }

        public int SkipCount { get; set; }

        /// <inheritdoc />
        public abstract bool Equals(ReferenceWrapper other);

        /// <summary>
        /// Creates a new instance with an optional routing key.
        /// </summary>
        /// <param name="routingKey">Optional routing key to filter messages based on content. 'Key=Value' where Key is a message property path and Value is the value to match with message payload content.</param>
        protected ReferenceWrapper(string routingKey = null)
        {
            var routingKeyArray = routingKey?.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
            if (!(routingKeyArray is null) && routingKeyArray.Length != 2)
            {
                throw new ArgumentException($"When {nameof(routingKey)} is provided, it must be similar to 'Key=Value'.");
            }
            else if (routingKeyArray is object)
            {
                RoutingKeyName = routingKeyArray[0];
                RoutingKeyValue = routingKeyArray[1];
            }
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
        /// Determines whether to deliver the message to the subscriber, based on <see cref="RoutingKeyName"/>, <see cref="RoutingKeyValue"/> and <see cref="MessageWrapper.Payload"/>.
        /// Not intended to be called from user code.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool ShouldDeliverMessage(MessageWrapper message)
        {
            if (string.IsNullOrWhiteSpace(RoutingKeyName))
                return true;
            if (!(MessageWrapperExtensions.PayloadSerializer is DefaultPayloadSerializer))
                return true;
            var token = MessageWrapperExtensions.PayloadSerializer.Deserialize<JToken>(message.Payload);
            string value = (string)token.SelectToken(RoutingKeyName);

            return new Regex(RoutingKeyValue).IsMatch(value);    
        }

        public bool ShouldProcessMessages()
        {
            if (SkipCount > 0)
            {
                SkipCount--;
                return false;
            }
            return true;
        }
    }
}