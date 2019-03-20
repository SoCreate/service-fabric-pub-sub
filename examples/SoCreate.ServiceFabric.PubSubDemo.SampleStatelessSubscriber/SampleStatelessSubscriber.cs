using System.Fabric;
using System.Threading.Tasks;
using SoCreate.ServiceFabric.PubSubDemo.SampleEvents;
using SoCreate.ServiceFabric.PubSub.Subscriber;

namespace SoCreate.ServiceFabric.PubSubDemo.SampleStatelessSubscriber
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class SampleStatelessSubscriber : SubscriberStatelessServiceBase
    {
        public SampleStatelessSubscriber(StatelessServiceContext context) : base(context)
        {
            Logger = message => ServiceEventSource.Current.ServiceMessage(Context, message);
        }

        [Subscribe]
        private Task HandleSampleEvent(SampleEvent sampleEvent)
        {
            ServiceEventSource.Current.ServiceMessage(Context, $"Processing {sampleEvent.GetType()}: {sampleEvent.Message} on SampleStatelessSubscriber");
            return Task.CompletedTask;
        }
    }
}
