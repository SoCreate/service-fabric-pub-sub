using System.Threading.Tasks;
using ServiceFabric.PubSubActors.State;

namespace ServiceFabric.PubSubActors.Stats
{
    public interface IStatsCollector
    {
        Task OnMessageReceived(ReferenceWrapper subscriber, MessageWrapper messageWrapper);
        Task OnMessageDelivered(ReferenceWrapper subscriber, MessageWrapper messageWrapper);
    }
}