using System.Fabric;
using System.Threading.Tasks;
using SoCreate.ServiceFabric.PubSubDemo.SampleEvents;
using SoCreate.ServiceFabric.PubSub.Subscriber;

namespace SoCreate.ServiceFabric.PubSubDemo.SampleStatefulSubscriber
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class SampleStatefulSubscriber : SubscriberStatefulServiceBase
    {
        public SampleStatefulSubscriber(StatefulServiceContext context) : base(context)
        {
            Logger = message => ServiceEventSource.Current.ServiceMessage(Context, message);
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
