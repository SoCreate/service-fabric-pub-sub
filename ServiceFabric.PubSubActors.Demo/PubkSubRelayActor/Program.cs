using System;
using System.Diagnostics;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace PubkSubRelayActor
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
				// This line registers an Actor Service to host your actor class with the Service Fabric runtime.
				// The contents of your ServiceManifest.xml and ApplicationManifest.xml files
				// are automatically populated when you build this project.
				// For more information, see http://aka.ms/servicefabricactorsplatform

				ActorRuntime.RegisterActorAsync<PubkSubRelayActor>(
				   (context, actorType) => new ActorService(context, actorType, (svc, id) => new PubkSubRelayActor(svc, id))).GetAwaiter().GetResult();

				Thread.Sleep(Timeout.Infinite);
			}
			catch (Exception e)
			{
				ActorEventSource.Current.ActorHostInitializationFailed(e.ToString());
				throw;
			}
		}
	}
}
