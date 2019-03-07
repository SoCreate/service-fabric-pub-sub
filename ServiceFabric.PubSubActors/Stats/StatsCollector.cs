using System.Threading.Tasks;
using ServiceFabric.PubSubActors.State;

namespace ServiceFabric.PubSubActors.Stats
{
    public class StatsCollector : IStatsCollector
    {
        public Task OnMessageReceived(ReferenceWrapper subscriber, MessageWrapper messageWrapper)
        {
            subscriber.TotalReceived++;
            return Task.FromResult(true);
        }

        public Task OnMessageDelivered(ReferenceWrapper subscriber, MessageWrapper messageWrapper)
        {
            subscriber.TotalDelivered++;
            return Task.FromResult(true);
        }
    }
}
