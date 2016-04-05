using System;
using System.Diagnostics;
using System.Fabric;
using System.Threading;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace PubSubActor
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
				ActorRuntime.RegisterActorAsync<PubSubActor>().GetAwaiter().GetResult();
				Thread.Sleep(Timeout.Infinite);  // Prevents this host process from terminating to keep the service host process running.

			}
			catch (Exception e)
			{
				ActorEventSource.Current.ActorHostInitializationFailed(e);
				throw;
			}
		}
	}
}
