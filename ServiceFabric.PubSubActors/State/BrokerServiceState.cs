using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Collections.Immutable;

namespace ServiceFabric.PubSubActors.State
{
    [DataContract]
    internal sealed class BrokerServiceState
    {
        private static readonly IEnumerable<Reference> Empty = ImmutableList<Reference>.Empty;

        [DataMember]
        public readonly string MessageTypeName;

        [DataMember]
        public IEnumerable<Reference> Subscribers { get; private set; }

        public BrokerServiceState(string messageTypeName, IEnumerable<Reference> subscribers = null)
        {
            MessageTypeName = messageTypeName;
            Subscribers = subscribers != null ? subscribers.ToImmutableList() : Empty;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            // Convert the deserialized collection to an immutable collection
            Subscribers = Subscribers.ToImmutableList();
        }

        /// <summary>
        /// Returns a cloned instance with the same subscribers as the original, plus the new <paramref name="subscriber"/>
        /// </summary>
        /// <param name="current"></param>
        /// <param name="subscriber"></param>
        /// <returns></returns>
        public static BrokerServiceState AddSubscriber(BrokerServiceState current, Reference subscriber)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (subscriber == null) throw new ArgumentNullException(nameof(subscriber));
            if (current.Subscribers.Any(s => s.ServiceOrActorReference.Equals(subscriber.ServiceOrActorReference)))
            {
                return current;
            }

            var clone = new BrokerServiceState(current.MessageTypeName, ((ImmutableList<Reference>)current.Subscribers).Add(subscriber));
            return clone;
        }

        /// <summary>
        /// Returns a cloned instance with the same subscribers as the original, minus the new <paramref name="subscriber"/>
        /// </summary>
        /// <param name="current"></param>
        /// <param name="subscriber"></param>
        /// <returns></returns>
        public static BrokerServiceState RemoveSubscriber(BrokerServiceState current, Reference subscriber)
        {
            if (subscriber == null) throw new ArgumentNullException(nameof(subscriber));

            return RemoveSubscriber(current, subscriber.ServiceOrActorReference);
        }

        /// <summary>
        /// Returns a cloned instance with the same subscribers as the original, minus the new <paramref name="subscriber"/>
        /// </summary>
        /// <param name="current"></param>
        /// <param name="subscriber"></param>
        /// <returns></returns>
        public static BrokerServiceState RemoveSubscriber(BrokerServiceState current, ReferenceWrapper subscriber)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (subscriber == null) throw new ArgumentNullException(nameof(subscriber));

            if (current.Subscribers.All(s => !s.ServiceOrActorReference.Equals(subscriber)))
            {
                return current;
            }

            var clone = new BrokerServiceState(current.MessageTypeName, ((ImmutableList<Reference>)current.Subscribers).RemoveAll(s => s.ServiceOrActorReference.Equals(subscriber)));
            return clone;
        }
    }

    [DataContract]
    public class Reference
    {
        [DataMember]
        public ReferenceWrapper ServiceOrActorReference { get; private set; }

        [DataMember] public readonly string QueueName;

        [DataMember] public readonly string DeadLetterQueueName;

        public Reference(ReferenceWrapper serviceOrActorReference, string queueName, string deadLetterQueueName)
        {
            ServiceOrActorReference = serviceOrActorReference;
            QueueName = queueName;
            DeadLetterQueueName = deadLetterQueueName;
        }
    }
}
