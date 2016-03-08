using System;
using Microsoft.ServiceFabric.Actors;
using ServiceFabric.PubSubActors.Interfaces;

namespace ServiceFabric.PubSubActors.PublisherActors
{
	/// <summary>
	/// Base class of a <see cref="StatefulActor{TState}"/> that can publish messages to <see cref="ISubscriberActor"/> Actors.
	/// </summary>
	/// <typeparam name="TState">Serializable state</typeparam>
	[Obsolete("Replaced by ActorBase extension method 'PublisherActorExtensions.PublishMessageAsync'")]
	public abstract class StatefulPublisherActor<TState> : StatefulActor<TState> where TState : class
	{
	}

	/// <summary>
	/// Base class of a <see cref="StatefulActor"/> that can publish messages to <see cref="ISubscriberActor"/> Actors.
	/// </summary>
	[Obsolete("Replaced by ActorBase extension method 'PublisherActorExtensions.PublishMessageAsync'")]
	public abstract class StatefulPublisherActor : StatefulActor
	{
	}
}