using System;
using System.Collections.Generic;
using System.Fabric;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
using SoCreate.ServiceFabric.PubSub;
using SoCreate.ServiceFabric.PubSubDemo.Common.Configuration;
using SoCreate.ServiceFabric.PubSubDemo.Common.Remoting;

namespace SoCreate.ServiceFabric.PubSubDemo.Broker
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class Broker : BrokerService
    {
        public Broker(StatefulServiceContext context) : base(context, proxyFactories: FabricConfiguration.GetProxyFactories())
        {
            ServiceEventSourceMessageCallback = message => ServiceEventSource.Current.ServiceMessage(context, message);
            Period = TimeSpan.FromMilliseconds(200);
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            if (FabricConfiguration.UseCustomServiceRemotingClientFactory)
            {
                return new[]
                {
                    new ServiceReplicaListener(context => new FabricTransportServiceRemotingListener(context, this, serializationProvider: new ServiceRemotingJsonSerializationProvider()), ListenerName)
                };
            }
            else
            {
                return base.CreateServiceReplicaListeners();
            }
        }
    }
}