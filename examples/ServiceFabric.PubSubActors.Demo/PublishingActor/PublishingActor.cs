using System;
using System.Threading.Tasks;
using Common.DataContracts;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using PublishingActor.Interfaces;
using ServiceFabric.PubSubActors.Helpers;
using ServiceFabric.PubSubActors.PublisherActors;

namespace PublishingActor
{
	/// <remarks>
	/// Each ActorID maps to an instance of this class.
	/// The IPublishingActor interface (in a separate DLL that client code can
	/// reference) defines the operations exposed by PublishingActor objects.
	/// </remarks>
	[StatePersistence(StatePersistence.None)]
	internal class PublishingActor : Actor, IPublishingActor
	{
	    private readonly IPublisherActorHelper _publisherActorHelper;

	    public PublishingActor(ActorService actorService, ActorId actorId, IPublisherActorHelper publisherActorHelper = null) 
            : base(actorService, actorId)
	    {
	        _publisherActorHelper = publisherActorHelper ?? new PublisherActorHelper(new BrokerServiceLocator());
	    }

	    async Task<string> IPublishingActor.PublishMessageOneAsync()
		{
			ActorEventSource.Current.ActorMessage(this, "Publishing Message");
            //using broker actor
			await this.PublishMessageAsync(new PublishedMessageOne {Content = "Hello PubSub World, from Actor, using Broker Actor!" });

            return "Message published to broker actor";
		}

        async Task<string> IPublishingActor.PublishMessageTwoAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Publishing Message");
            //using broker service
            //wrong type:
            await _publisherActorHelper.PublishMessageAsync(this, new PublishedMessageOne { Content = "If you see this, something is wrong!" });
            //right type:
            await _publisherActorHelper.PublishMessageAsync(this, new PublishedMessageTwo { Content = "Hello PubSub World, from Actor, using Broker Service!" });
            
            return "Message published to broker service";
        }
    }
}
