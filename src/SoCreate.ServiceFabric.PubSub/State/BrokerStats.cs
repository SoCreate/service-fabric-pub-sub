using System.Collections.Generic;

namespace SoCreate.ServiceFabric.PubSub.State
{
    public class BrokerStats
    {
        public Dictionary<string, ReferenceWrapper> Subscribers { get; set; }
        public Dictionary<string, List<QueueStats>> QueueStats { get; set; }
    }
}