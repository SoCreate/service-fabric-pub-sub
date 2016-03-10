using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.DataContracts;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.PubSubActors.Interfaces;
using ServiceFabric.PubSubActors.SubscriberServices;

namespace SubscribingStatefulService
{
	/// <summary>
	/// The FabricRuntime creates an instance of this class for each service type instance.
	/// </summary>
	internal sealed class SubscribingStatefulService : StatefulService, ISubscriberService
	{
		protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
		{
			BeginRegistration();
			yield return new ServiceReplicaListener(p => new SubscriberCommunicationListener(this, p));

		}

		private void BeginRegistration()
		{
			ThreadPool.QueueUserWorkItem(async _ =>
			{
				int retries = 0;
				const int maxRetries = 10;
				Thread.Yield();
				while (true)
				{
					try
					{
						await RegisterAsync();
						ServiceEventSource.Current.ServiceMessage(this, $"Registered Service:'{nameof(SubscribingStatefulService)}' Replica:'{ServiceInitializationParameters.ReplicaId}' as Subscriber.");
						break;
					}
					catch (Exception ex)
					{
						if (retries++ < maxRetries)
						{
							await Task.Delay(TimeSpan.FromMilliseconds(500));
							continue;
						}
						ServiceEventSource.Current.ServiceMessage(this, $"Failed to register Service:'{nameof(SubscribingStatefulService)}' Replica:'{ServiceInitializationParameters.ReplicaId}' as Subscriber. Error:'{ex}'");
						break;
					}
				}
			});
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
