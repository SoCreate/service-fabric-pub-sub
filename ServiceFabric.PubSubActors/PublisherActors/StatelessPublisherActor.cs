using System;
using Microsoft.ServiceFabric.Actors;
using ServiceFabric.PubSubActors.Interfaces;

namespace ServiceFabric.PubSubActors.PublisherActors
{
	/// <summary>
	/// Base class of a <see cref="StatelessActor"/> that can publish messages to <see cref="ISubscriberActor"/> Actors.
	/// </summary>
	[Obsolete("Replaced by ActorBase extension method 'PublisherActorExtensions.PublishMessageAsync'")]
	public abstract class StatelessPublisherActor : StatelessActor
	{
		
	}
}