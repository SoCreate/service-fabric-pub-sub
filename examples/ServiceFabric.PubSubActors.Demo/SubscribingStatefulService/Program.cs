using System;
using System.Diagnostics;
using System.Fabric;
using System.Threading;
using Microsoft.ServiceFabric.Services.Runtime;

namespace SubscribingStatefulService
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
				ServiceRuntime.RegisterServiceAsync("SubscribingStatefulServiceType", context => new SubscribingStatefulService(context)).GetAwaiter().GetResult();
				ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(SubscribingStatefulService).Name);
				Thread.Sleep(Timeout.Infinite);  // Prevents this host process from terminating to keep the service host process running.
			}
			catch (Exception e)
			{
				ServiceEventSource.Current.ServiceHostInitializationFailed(e);
				throw;
			}
		}
	}
}
