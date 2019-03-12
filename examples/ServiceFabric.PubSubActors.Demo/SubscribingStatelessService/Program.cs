using System;
using System.Diagnostics;
using System.Fabric;
using System.Threading;
using Microsoft.ServiceFabric.Services.Runtime;

namespace SubscribingStatelessService
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
				ServiceRuntime.RegisterServiceAsync("SubscribingStatelessServiceType", context => new SubscribingStatelessService(context)).GetAwaiter().GetResult();
				ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(SubscribingStatelessService).Name);
				Thread.Sleep(Timeout.Infinite);  // Prevents this host process from terminating so services keeps running.
			}
			catch (Exception e)
			{
				ServiceEventSource.Current.ServiceHostInitializationFailed(e);
				throw;
			}
		}
	}
}
