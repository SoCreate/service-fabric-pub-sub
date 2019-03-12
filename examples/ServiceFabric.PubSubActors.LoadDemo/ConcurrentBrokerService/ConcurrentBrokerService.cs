using System;
using System.Fabric;
using ServiceFabric.PubSubActors.Helpers;

namespace ConcurrentBrokerService
{
	/// <summary>
	/// Broker for pub sub messaging using ConcurrentQueue.
	/// </summary>
	internal sealed class BrokerService : ServiceFabric.PubSubActors.BrokerServiceUnordered
	{
		public BrokerService(StatefulServiceContext context)
			: base(context, enableAutoDiscovery: false)
		{
			ServiceEventSourceMessageCallback = (message) => ServiceEventSource.Current.ServiceMessage(Context, message);

			Period = TimeSpan.FromMilliseconds(200);
			DueTime = TimeSpan.FromSeconds(5);

			new BrokerServiceLocator().RegisterAsync(Context.ServiceName)
					.ConfigureAwait(false)
					.GetAwaiter()
					.GetResult();
		}
	}
}
