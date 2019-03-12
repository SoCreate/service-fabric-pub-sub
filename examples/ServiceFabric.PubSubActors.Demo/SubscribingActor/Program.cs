using System;
using System.Diagnostics;
using System.Fabric;
using System.Threading;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using ServiceFabric.PubSubActors.Interfaces;
using SubscribingActor.Interfaces;

namespace SubscribingActor
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
				ActorRuntime.RegisterActorAsync<SubscribingActor>().GetAwaiter().GetResult();
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
