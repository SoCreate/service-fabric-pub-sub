using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Common.DataContracts;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.PubSubActors.Interfaces;
using ServiceFabric.PubSubActors.SubscriberServices;

namespace SubscribingToRelayStatefulService
{
	/// <summary>
	/// An instance of this class is created for each service replica by the Service Fabric runtime.
	/// </summary>
	internal sealed class SubscribingToRelayStatefulService : StatefulService, ISubscriberService
	{
		private const string WellKnownRelayBrokerId = "WellKnownRelayBroker";

		public SubscribingToRelayStatefulService(StatefulServiceContext serviceContext) : base(serviceContext)
		{
		}

		public SubscribingToRelayStatefulService(StatefulServiceContext serviceContext, IReliableStateManagerReplica reliableStateManagerReplica) : base(serviceContext, reliableStateManagerReplica)
		{
		}

		protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
		{
			yield return new ServiceReplicaListener(p => new SubscriberCommunicationListener(this, p), "RelayStatefullSubscriberCommunicationListener");
		}

		protected override async Task OnOpenAsync(ReplicaOpenMode openMode, CancellationToken cancellationToken)
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
					ServiceEventSource.Current.ServiceMessage(this, $"Registered Service:'{nameof(SubscribingToRelayStatefulService)}' Replica:'{Context.ReplicaId}' as Subscriber.");
					break;
				}
				catch (Exception ex)
				{
					if (retries++ < maxRetries)
					{
						await Task.Delay(TimeSpan.FromMilliseconds(500));
						continue;
					}
					ServiceEventSource.Current.ServiceMessage(this, $"Failed to register Service:'{nameof(SubscribingToRelayStatefulService)}' Replica:'{Context.ReplicaId}' as Subscriber. Error:'{ex}'");
					break;
				}
			}
		}

		public Task RegisterAsync()
		{
			//register as subscriber for this type of messages at the relay broker
			//using the default Broker for the message type as source for the relay broker
			return this.RegisterMessageTypeWithRelayBrokerAsync(typeof(PublishedMessageOne), new ActorId(WellKnownRelayBrokerId), null);
		}

		public Task UnregisterAsync()
		{
			//unregister as subscriber for this type of messages at the relay broker
			return this.UnregisterMessageTypeWithRelayBrokerAsync(typeof(PublishedMessageOne), new ActorId(WellKnownRelayBrokerId), null, true);
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
