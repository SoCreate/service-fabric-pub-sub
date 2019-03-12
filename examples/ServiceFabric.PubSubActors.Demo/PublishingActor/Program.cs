using System;
using System.Diagnostics;
using System.Fabric;
using System.Threading;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using ServiceFabric.PubSubActors;
using ServiceFabric.PubSubActors.Helpers;

namespace PublishingActor
{
	internal static class Program
	{
		/// <summary>
		/// This is the entry point of the service host process.
		/// </summary>
		private static void Main()
		{
			try
			{
                var locator = new BrokerServiceLocator();
                var publisherActorHelper = new PublisherActorHelper(locator);

                ActorRuntime.RegisterActorAsync<PublishingActor>(
                    (context, actorType) => new ActorService(context, actorType, (svc, id) => new PublishingActor(svc, id, publisherActorHelper))).GetAwaiter().GetResult();
                Thread.Sleep(Timeout.Infinite);  // Prevents this host process from terminating so services keeps running.
			}
			catch (Exception e)
			{
				ActorEventSource.Current.ActorHostInitializationFailed(e);
				throw;
			}
		}
	}
}
