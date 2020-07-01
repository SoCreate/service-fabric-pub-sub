using System.Fabric;
using System.Threading.Tasks;
using SoCreate.ServiceFabric.PubSubDemo.SampleEvents;
using SoCreate.ServiceFabric.PubSub.Subscriber;
using System.Collections.Generic;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
using SoCreate.ServiceFabric.PubSubDemo.Common.Configuration;
using SoCreate.ServiceFabric.PubSubDemo.Common.Remoting;

namespace SoCreate.ServiceFabric.PubSubDemo.SampleStatelessSubscriber
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class SampleStatelessSubscriber : SubscriberStatelessServiceBase
    {
        public SampleStatelessSubscriber(StatelessServiceContext context) : base(context, FabricConfiguration.GetBrokerClient())
        {
            Logger = message => ServiceEventSource.Current.ServiceMessage(Context, message);
        }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            if (FabricConfiguration.UseCustomServiceRemotingClientFactory)
            {
                return new[]
                {
                    new ServiceInstanceListener(context => new FabricTransportServiceRemotingListener(context, this, serializationProvider: new ServiceRemotingJsonSerializationProvider()), ListenerName)
                };
            }
            else
            {
                return base.CreateServiceInstanceListeners();
            }
        }

        [Subscribe]
        private Task HandleSampleEvent(SampleEvent sampleEvent)
        {
            ServiceEventSource.Current.ServiceMessage(Context, $"Processing {sampleEvent.GetType()}: {sampleEvent.Message} on SampleStatelessSubscriber");
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