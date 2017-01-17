using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Common.DataContracts;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Runtime;
using ServiceFabric.PubSubActors.Helpers;
using ServiceFabric.PubSubActors.Interfaces;
using ServiceFabric.PubSubActors.SubscriberServices;

namespace SubscribingStatelessService
{
	/// <summary>
	/// The FabricRuntime creates an instance of this class for each service type instance. 
	/// </summary>
	internal sealed class SubscribingStatelessService : StatelessService, ISubscribingStatelessService
	{
	    private readonly ISubscriberServiceHelper _subscriberServiceHelper;

		public SubscribingStatelessService(StatelessServiceContext serviceContext) : base(serviceContext)
		{
            _subscriberServiceHelper = new SubscriberServiceHelper(new BrokerServiceLocator());
        }

		protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
		{
			yield return new ServiceInstanceListener(p => new SubscriberCommunicationListener(this, p), "StatelessSubscriberCommunicationListener");
			yield return new ServiceInstanceListener(context => new FabricTransportServiceRemotingListener(context, this), "StatelessFabricTransportServiceRemotingListener");
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

		public async Task RegisterAsync()
		{
			await this.RegisterMessageTypeAsync(typeof(PublishedMessageOne));
            //await this.RegisterMessageTypeWithBrokerServiceAsync(typeof(PublishedMessageTwo));
		    await _subscriberServiceHelper.RegisterMessageTypeAsync(this, typeof(PublishedMessageTwo));
		}

        public async Task UnregisterAsync()
		{
			await this.UnregisterMessageTypeAsync(typeof(PublishedMessageOne), true);
            //await this.UnregisterMessageTypeWithBrokerServiceAsync(typeof(PublishedMessageTwo), true);
            await _subscriberServiceHelper.UnregisterMessageTypeAsync(this, typeof(PublishedMessageTwo), true);
        }

        public Task ReceiveMessageAsync(MessageWrapper message)
		{
			var payload = this.Deserialize<PublishedMessageOne>(message);
			ServiceEventSource.Current.ServiceMessage(this, $"Received message: {payload.Content}");
			//TODO: handle message
			return Task.FromResult(true);
		}
	}

	public interface ISubscribingStatelessService : ISubscriberService
	{
		Task UnregisterAsync();

		Task RegisterAsync();
	}
}
