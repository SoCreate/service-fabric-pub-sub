using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;

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
        public ReferenceWrapper ReferenceWrapper { get; set; }

        [DataMember]
        public string MessageType { get; set; }

        private SemaphoreSlim _queueSemaphore;

        [IgnoreDataMember]
        public SemaphoreSlim QueueSemaphore
        {
            get
            {
                if (_queueSemaphore == null)
                {
                    _queueSemaphore = new SemaphoreSlim(1);
                }
                return _queueSemaphore;
            }
        } 
    }

    public class BrokerServiceStateReferenceEqualComparer : IEqualityComparer<BrokerServiceState>
    {
        public bool Equals(BrokerServiceState x, BrokerServiceState y)
        {
            if (x == null || y == null) return false;
            return Equals(x.ReferenceWrapper, y.ReferenceWrapper);
        }

        public int GetHashCode(BrokerServiceState obj)
        {
            if (obj == null) return -1;
            return obj.ReferenceWrapper.GetHashCode();
        }
    }
}
