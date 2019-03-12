using System.Collections.Generic;

namespace ServiceFabric.PubSubActors.State
{
    public class BrokerStats
    {
        public Dictionary<string, ReferenceWrapper> Subscribers { get; set; }
        public Dictionary<string, List<QueueStats>> QueueStats { get; set; }
    }
}