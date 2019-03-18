using System.Fabric;
using System.Threading.Tasks;
using PubSubDemo.SampleEvents;
using ServiceFabric.PubSubActors.Subscriber;

namespace PubSubDemo.SampleStatelessSubscriber
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class Service : SubscriberStatelessServiceBase
    {
        public Service(StatelessServiceContext context) : base(context)
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
