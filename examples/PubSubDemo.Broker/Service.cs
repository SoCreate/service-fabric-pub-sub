using System.Fabric;
using ServiceFabric.PubSubActors;

namespace PubSubDemo.Broker
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class Service : BrokerService
    {
        public Service(StatefulServiceContext context) : base(context)
        {
            ServiceEventSourceMessageCallback = message => ServiceEventSource.Current.ServiceMessage(context, message);
        }
    }
}
