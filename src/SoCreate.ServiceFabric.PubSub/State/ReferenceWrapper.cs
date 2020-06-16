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
        /// <param name="routingKeyName">Optional Message property path used for routing</param>
        /// <param name="routingKeyValue">Optional value to match with message payload content used for routing.</param>
        protected ReferenceWrapper(string routingKeyName = null, string routingKeyValue = null)
        {
            
            if ((routingKeyName is object && routingKeyValue is null && routingKeyName.Length > 0) || (routingKeyName is null && routingKeyValue is object)) throw new ArgumentException($"{nameof(routingKeyName)} and {nameof(RoutingKeyValue)} must both be provided. RoutingKeyName must have a length of atleast 1");
            RoutingKeyName = routingKeyName;
            RoutingKeyValue = routingKeyValue;
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
        /// Determines whether to deliver the message to the subscriber, based on <see cref="RoutingKeyName"/> and <see cref="MessageWrapper.Payload"/>.
        /// Not intended to be called from user code.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool ShouldDeliverMessage(MessageWrapper message)
        {
            if (!(MessageWrapperExtensions.PayloadSerializer is DefaultPayloadSerializer))
                return true;
            if (RoutingKeyName is null || RoutingKeyValue is null)
                return true;

            var token = MessageWrapperExtensions.PayloadSerializer.Deserialize<JToken>(message.Payload);
            string value = (string)token.SelectToken(RoutingKeyName);
            if(value is null)
                return false;
            var regex = new Regex("^" + Regex.Escape(RoutingKeyValue).Replace(@"\*", ".*") + "$");
            return regex.IsMatch(value);
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