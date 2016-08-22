using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceFabric.PubSubActors.State
{
    [DataContract]
    public class BrokerServiceState
    {
        [DataMember]
        public string SubscriberMessageQueueID { get; set; }

        [DataMember]
        public string SubscriberDeadLetterQueueID { get; set; } = Guid.NewGuid().ToString("N");

        [DataMember]
        public HashSet<string> MessageTypeNames { get; set; } = new HashSet<string>();
    }
}
