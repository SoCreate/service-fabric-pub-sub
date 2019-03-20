using System.Fabric;
using System.Threading.Tasks;
using PubSubDemo.SampleEvents;
using ServiceFabric.PubSubActors.Subscriber;

namespace PubSubDemo.SampleStatefulSubscriber
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
    }
}
