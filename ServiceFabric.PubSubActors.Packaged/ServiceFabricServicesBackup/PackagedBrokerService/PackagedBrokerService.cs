using System;
using System.Fabric;
using ServiceFabric.PubSubActors;

namespace PackagedBrokerService
{
    /// <remarks>
	/// Default implementation of <see cref="BrokerService"/>. This is a <see cref="StatefulService"/> that serves as a Broker that accepts messages 
	/// from Actors & Services calling <see cref="PublisherServiceExtensions.PublishMessageToBrokerServiceAsync"/> and <see cref="PublisherActorExtensions.PublishMessageToBrokerServiceAsync"/>.
	/// and forwards them to <see cref="ISubscriberActor"/> Actors and <see cref="ISubscriberService"/> Services.
	/// Every message type is mapped to one of the partitions of this service.
	/// </remarks>
    internal sealed class PackagedBrokerService : BrokerService
    {
        public PackagedBrokerService(StatefulServiceContext context)
            : base(context)
        {
            ServiceEventSourceMessageCallback = (message) => ServiceEventSource.Current.ServiceMessage(this, message);

            Period = TimeSpan.FromMilliseconds(200);    //process queued items every 200 ms.
            DueTime = TimeSpan.FromSeconds(10);         //sleep for 10 seconds before processing queued items
        }        
    }
}
