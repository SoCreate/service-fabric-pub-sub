using System;
using System.Threading.Tasks;
using Common.DataContracts;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using PublishingActor.Interfaces;

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
		async Task<string> IPublishingActor.PublishMessageOneAsync()
		{
			ActorEventSource.Current.ActorMessage(this, "Publishing Message");
			await this.PublishMessageAsync(new PublishedMessageOne {Content = "Hello PubSub World, from Actor, using Broker Actor!" });
            await this.PublishMessageToBrokerServiceAsync(new PublishedMessageTwo { Content = "If you see this, something is wrong!" });

            return "Message published to broker actor";
		}

        async Task<string> IPublishingActor.PublishMessageTwoAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Publishing Message");

            await this.PublishMessageToBrokerServiceAsync(new PublishedMessageOne { Content = "If you see this, something is wrong!" });

            await this.PublishMessageToBrokerServiceAsync(new PublishedMessageTwo { Content = "Hello PubSub World, from Actor, using Broker Service!" });
            return "Message published to broker service";
        }
    }
}
