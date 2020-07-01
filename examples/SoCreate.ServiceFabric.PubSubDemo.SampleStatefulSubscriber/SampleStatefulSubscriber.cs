using System.Fabric;
using System.Threading.Tasks;
using SoCreate.ServiceFabric.PubSubDemo.SampleEvents;
using SoCreate.ServiceFabric.PubSub.Subscriber;
using System.Collections.Generic;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
using SoCreate.ServiceFabric.PubSubDemo.Common.Configuration;
using SoCreate.ServiceFabric.PubSubDemo.Common.Remoting;

namespace SoCreate.ServiceFabric.PubSubDemo.SampleStatefulSubscriber
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class SampleStatefulSubscriber : SubscriberStatefulServiceBase
    {
        public SampleStatefulSubscriber(StatefulServiceContext context) : base(context, FabricConfiguration.GetBrokerClient())
        {
            Logger = message => ServiceEventSource.Current.ServiceMessage(Context, message);
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

        [Subscribe]
        private Task HandleSampleEvent(SampleEvent sampleEvent)
        {
            ServiceEventSource.Current.ServiceMessage(Context, $"Processing {sampleEvent.GetType()}: {sampleEvent.Message} on SampleStatefulSubscriber");
            return Task.CompletedTask;
        }

        [Subscribe(QueueType.Unordered)]
        private Task HandleSampleUnorderedEvent(SampleUnorderedEvent sampleEvent)
        {
            ServiceEventSource.Current.ServiceMessage(Context, $"Processing {sampleEvent.GetType()}: {sampleEvent.Message} on SampleStatelessSubscriber");
            return Task.CompletedTask;
        }
    }
}