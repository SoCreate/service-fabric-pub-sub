using System;
using System.Diagnostics;
using System.Fabric;
using System.Threading;
using Microsoft.ServiceFabric.Actors;
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
				// Creating a FabricRuntime connects this host process to the Service Fabric runtime on this node.
				using (FabricRuntime fabricRuntime = FabricRuntime.Create())
				{
					// This line registers your actor class with the Fabric Runtime.
					// The contents of your ServiceManifest.xml and ApplicationManifest.xml files
					// are automatically populated when you build this project.
					// For information, see http://aka.ms/servicefabricactorsplatform
					fabricRuntime.RegisterActor<SubscribingActor>();
					Thread.Sleep(Timeout.Infinite);  // Prevents this host process from terminating so services keeps running.
				}
			}
			catch (Exception e)
			{
				ActorEventSource.Current.ActorHostInitializationFailed(e.ToString());
				throw;
			}
		}
	}
}
