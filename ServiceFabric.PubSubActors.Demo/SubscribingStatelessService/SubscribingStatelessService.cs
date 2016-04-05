using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Common.DataContracts;
using ServiceFabric.PubSubActors.Interfaces;
using ServiceFabric.PubSubActors.SubscriberServices;

namespace SubscribingStatelessService
{
	/// <summary>
	/// The FabricRuntime creates an instance of this class for each service type instance. 
	/// </summary>
	internal sealed class SubscribingStatelessService : StatelessService, ISubscriberService
	{
		public SubscribingStatelessService(StatelessServiceContext serviceContext) : base(serviceContext)
		{
		}

		protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
		{
			yield return new ServiceInstanceListener(p => new SubscriberCommunicationListener(this, p));
		}

		protected override async Task OnOpenAsync(CancellationToken cancellationToken)
		{
			await TryRegisterAsync();
		}

		private async Task TryRegisterAsync()
		{
			int retries = 0;
			const int maxRetries = 10;
			Thread.Yield();
			while (true)
			{
				try
				{
					await RegisterAsync();
					ServiceEventSource.Current.ServiceMessage(this, $"Registered Service:'{nameof(SubscribingStatelessService)}' Instance:'{Context.InstanceId}' as Subscriber.");
					break;
				}
				catch (Exception ex)
				{
					if (retries++ < maxRetries)
					{
						await Task.Delay(TimeSpan.FromMilliseconds(500));
						continue;
					}
					ServiceEventSource.Current.ServiceMessage(this, $"Failed to register Service:'{nameof(SubscribingStatelessService)}' Instance:'{Context.InstanceId}' as Subscriber. Error:'{ex}'");
					break;
				}
			}
		}

		public Task RegisterAsync()
		{
			return this.RegisterMessageTypeAsync(typeof(PublishedMessageOne));
		}

		public Task UnregisterAsync()
		{
			return this.UnregisterMessageTypeAsync(typeof(PublishedMessageOne), true);
		}

		public Task ReceiveMessageAsync(MessageWrapper message)
		{
			var payload = this.Deserialize<PublishedMessageOne>(message);
			ServiceEventSource.Current.ServiceMessage(this, $"Received message: {payload.Content}");
			//TODO: handle message
			return Task.FromResult(true);
		}
	}
}
