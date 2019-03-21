using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using SoCreate.ServiceFabric.PubSub;
using SoCreate.ServiceFabric.PubSubDemo.SampleActorSubscriber.Interfaces;
using SoCreate.ServiceFabric.PubSubDemo.SampleEvents;
using SoCreate.ServiceFabric.PubSub.State;

namespace SoCreate.ServiceFabric.PubSubDemo.SampleActorSubscriber
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
