using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.PubSubActors;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Runtime;

namespace PubSubService
{
    /// <summary>
    /// Service that acts as a Broker to subscribe and publish to.
    /// </summary>
    internal sealed class PubSubService : BrokerService
    {
        public PubSubService(StatefulServiceContext context)
            : base(context)
        {
            ServiceEventSourceMessageCallback = message => ServiceEventSource.Current.ServiceMessage(this, message);
        }        
    }
}
