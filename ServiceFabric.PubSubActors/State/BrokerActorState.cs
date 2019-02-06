using ServiceFabric.PubSubActors.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceFabric.PubSubActors.State
{
    /// <summary>
    /// State for <see cref="BrokerActor"/>. Contains a regular queue and a dead letter queue for every registered listener.
    /// </summary>
    [Obsolete("This class will be removed in the next major upgrade. Use the BrokerService instead.")]
    [DataContract]
    public sealed class BrokerActorState
    {
        /// <summary>
        /// Contains messages that could not be be sent to subscribed listeners. (has a limit)
        /// </summary>
        [DataMember]
        public Dictionary<ReferenceWrapper, Queue<MessageWrapper>> SubscriberDeadLetters { get; set; }

        /// <summary>
        /// Contains messages to be sent to subscribed listeners.
        /// </summary>
        [DataMember]
        public Dictionary<ReferenceWrapper, Queue<MessageWrapper>> SubscriberMessages { get; set; }
    }
}