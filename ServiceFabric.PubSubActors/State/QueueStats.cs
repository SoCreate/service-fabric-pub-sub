using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceFabric.PubSubActors.State
{
    [DataContract]
    public class QueueStats
    {
        [DataMember]
        public string QueueName { get; set; }
        [DataMember]
        public string ServiceName { get; set; }
        [DataMember]
        public DateTime Time { get; set; }
        [DataMember]
        public ulong TotalReceived { get; set; }
        [DataMember]
        public ulong TotalDelivered { get; set; }
        [DataMember]
        public ulong TotalDeliveryFailures { get; set; }
    }

    [DataContract]
    public class QueueStatsWrapper
    {
        [DataMember]
        public Dictionary<string, ReferenceWrapper> Queues { get; set; }
        [DataMember]
        public IEnumerable<QueueStats> Stats { get; set; }
    }
}