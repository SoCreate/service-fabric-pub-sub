using System.Runtime.Serialization;

namespace SoCreate.ServiceFabric.PubSub.State
{
    [DataContract]
    public class SubscriptionDetails
    {
        [DataMember]
        public ReferenceWrapper ServiceOrActorReference { get; private set; }

        [DataMember]
        public string MessageTypeName { get; private set; }
        
        [DataMember]
        public bool IsOrdered { get; private set; }

        [DataMember] 
        public readonly string QueueName;

        public SubscriptionDetails(ReferenceWrapper serviceOrActorReference, string messageTypeName, bool isOrdered = true)
        {
            ServiceOrActorReference = serviceOrActorReference;
            MessageTypeName = messageTypeName;
            IsOrdered = isOrdered;
            QueueName = CreateQueueName(serviceOrActorReference, messageTypeName);
        }
        
        public static string CreateQueueName(ReferenceWrapper referenceWrapper, string messageTypeName)
        {
            return $"{messageTypeName}_{referenceWrapper.GetQueueName()}";
        }
    }
}