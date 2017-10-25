using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.PubSubActors.Interfaces;
using ServiceFabric.PubSubActors.PublisherActors;
using ServiceFabric.PubSubActors.State;
using ServiceFabric.PubSubActors.SubscriberServices;

namespace ServiceFabric.PubSubActors
{
	/// <remarks>
	/// Base class for a <see cref="StatefulService"/> that serves as a Broker that accepts messages 
	/// from Actors & Services calling <see cref="PublisherActorExtensions.PublishMessageAsync"/>
	/// and forwards them to <see cref="ISubscriberActor"/> Actors and <see cref="ISubscriberService"/> Services without strict ordering, so more performant than <see cref="BrokerService"/>.
	/// Every message type is mapped to one of the partitions of this service.
	/// </remarks>
	public abstract class BrokerServiceUnordered : BrokerServiceBase
	{
		/// <summary>
		/// Creates a new instance using the provided context and registers this instance for automatic discovery if needed.
		/// </summary>
		/// <param name="serviceContext"></param>
		/// <param name="enableAutoDiscovery"></param>
		protected BrokerServiceUnordered(StatefulServiceContext serviceContext, bool enableAutoDiscovery = true)
			: base(serviceContext, enableAutoDiscovery)
		{
		}

		/// <summary>
		/// Creates a new instance using the provided context and registers this instance for automatic discovery if needed.
		/// </summary>
		/// <param name="serviceContext"></param>
		/// <param name="reliableStateManagerReplica"></param>
		/// <param name="enableAutoDiscovery"></param>
		protected BrokerServiceUnordered(StatefulServiceContext serviceContext, IReliableStateManagerReplica2 reliableStateManagerReplica, bool enableAutoDiscovery = true)
			: base(serviceContext, reliableStateManagerReplica, enableAutoDiscovery)
		{
		}

		/// <summary>
		/// Sends out queued messages for the provided queue.
		/// </summary>
		/// <param name="cancellationToken"></param>
		/// <param name="subscriber"></param>
		/// <param name="queueName"></param>
		/// <returns></returns>
		protected sealed override async Task ProcessQueues(CancellationToken cancellationToken, ReferenceWrapper subscriber, string queueName)
		{
			var queue = await TimeoutRetryHelper.Execute((token, state) => StateManager.GetOrAddAsync<IReliableConcurrentQueue<MessageWrapper>>(queueName), cancellationToken: cancellationToken);
			long messageCount = queue.Count;

			if (messageCount == 0L) return;
			messageCount = Math.Min(messageCount, MaxDequeuesInOneIteration);

			ServiceEventSourceMessage($"Processing {messageCount} items from queue {queue.Name} for subscriber: {subscriber.Name}");

			for (long i = 0; i < messageCount; i++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				await TimeoutRetryHelper.ExecuteInTransaction(StateManager, async (tx, token, state) =>
				{
					var result = await queue.TryDequeueAsync(tx, cancellationToken);
					if (result.HasValue)
					{
						await subscriber.PublishAsync(result.Value);
					}
				}, cancellationToken: cancellationToken);
			}
		}

		protected override async Task EnqueueMessageAsync(MessageWrapper message, Reference subscriber, ITransaction tx)
		{
			var queueResult = await StateManager.TryGetAsync<IReliableConcurrentQueue<MessageWrapper>>(subscriber.QueueName);
			if (!queueResult.HasValue) return;

			await queueResult.Value.EnqueueAsync(tx, message);
		}

		protected sealed override Task CreateQueueAsync(ITransaction tx, string queueName)
		{
			return StateManager.GetOrAddAsync<IReliableConcurrentQueue<MessageWrapper>>(tx, queueName);
		}
	}
}