using System;
using System.Fabric;
using SoCreate.ServiceFabric.PubSub;

namespace SoCreate.ServiceFabric.PubSubDemo.Broker
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class Broker : BrokerService
    {
        public Broker(StatefulServiceContext context) : base(context)
        {
            ServiceEventSourceMessageCallback = message => ServiceEventSource.Current.ServiceMessage(context, message);
            Period = TimeSpan.FromMilliseconds(200);
        }
    }
}
