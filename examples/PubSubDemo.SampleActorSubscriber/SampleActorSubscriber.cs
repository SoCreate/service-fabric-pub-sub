using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using PubSubDemo.SampleActorSubscriber.Interfaces;
using PubSubDemo.SampleEvents;
using ServiceFabric.PubSubActors.Helpers;
using ServiceFabric.PubSubActors.State;

namespace PubSubDemo.SampleActorSubscriber
{
    [ActorService(Name = nameof(SampleActorSubscriber))]
    [StatePersistence(StatePersistence.None)]
    internal class SampleActorSubscriber : Actor, ISampleActorSubscriber
    {
        private readonly IBrokerClient _brokerClient;

        public SampleActorSubscriber(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
            _brokerClient = new BrokerClient();
        }

        public Task ReceiveMessageAsync(MessageWrapper message)
        {
            return _brokerClient.ProcessMessageAsync(message);
        }

        public Task Subscribe()
        {
            return _brokerClient.SubscribeAsync<SampleEvent>(this, HandleMessageSampleEvent);
        }

        private Task HandleMessageSampleEvent(SampleEvent message)
        {
            ActorEventSource.Current.ActorMessage(this, $"Received message: {message.Message}");
            return Task.CompletedTask;
        }
    }
}
