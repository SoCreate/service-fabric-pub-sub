using System;
using System.Fabric;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Communication.Wcf.Client;
using ServiceFabric.PubSubActors.Interfaces;

namespace ServiceFabric.PubSubActors.SubscriberServices
{
	internal class SubscriberServicePartitionClient : ServicePartitionClient<WcfCommunicationClient<ISubscriberService>>, ISubscriberService
	{
        private static readonly WcfCommunicationClientFactory _factory = new WcfCommunicationClientFactory();

		public static SubscriberServicePartitionClient Create(ServiceReference subscriberServiceReference)
		{
			
			SubscriberServicePartitionClient client;
			switch (subscriberServiceReference.PartitionKind)
			{
				case ServicePartitionKind.Singleton:
					client = new SubscriberServicePartitionClient(_factory, subscriberServiceReference.ServiceUri);
					break;
				case ServicePartitionKind.Int64Range:
					if (subscriberServiceReference.PartitionID == null)
						throw new InvalidOperationException("subscriberReference is missing its partition id.");
					client = new SubscriberServicePartitionClient(_factory, subscriberServiceReference.ServiceUri, subscriberServiceReference.PartitionID.Value);
					break;
				case ServicePartitionKind.Named:
					client = new SubscriberServicePartitionClient(_factory, subscriberServiceReference.ServiceUri, subscriberServiceReference.PartitionName);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return client;
		}


		private SubscriberServicePartitionClient(ICommunicationClientFactory<WcfCommunicationClient<ISubscriberService>> factory, Uri serviceName)
			: base(factory, serviceName)
		{
		}

		private SubscriberServicePartitionClient(ICommunicationClientFactory<WcfCommunicationClient<ISubscriberService>> factory, Uri serviceName, string partitionKey)
			: base(factory, serviceName, new ServicePartitionKey(partitionKey))
		{
		}

		private SubscriberServicePartitionClient(ICommunicationClientFactory<WcfCommunicationClient<ISubscriberService>> factory, Uri serviceName, long partitionKey)
			: base(factory, serviceName, new ServicePartitionKey(partitionKey))
		{ 
		}

		
		public Task ReceiveMessageAsync(MessageWrapper message)
		{
			return InvokeWithRetryAsync(
			   client => client.Channel.ReceiveMessageAsync(message));
		}
	}

	internal class WcfCommunicationClientFactory : WcfCommunicationClientFactory<ISubscriberService>
	{
		public WcfCommunicationClientFactory()
			: base(CreateBinding())
		{
		}

		private static Binding CreateBinding()
		{
			return BindingFactory.CreateBinding();
		}
	}
}
