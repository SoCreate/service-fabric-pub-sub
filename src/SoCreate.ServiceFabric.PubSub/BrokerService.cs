using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Fabric;
using System.Threading.Tasks;
using System.Threading;
using SoCreate.ServiceFabric.PubSub.Events;
using SoCreate.ServiceFabric.PubSub.Helpers;
using SoCreate.ServiceFabric.PubSub.State;
using SoCreate.ServiceFabric.PubSub.Subscriber;

namespace SoCreate.ServiceFabric.PubSub
{
    /// <remarks>
    /// Base class for a <see cref="StatefulService"/> that serves as a Broker that accepts messages
    /// from Actors & Services calling <see cref="IBrokerClient.PublishMessageAsync{T}"/>
    /// and forwards them to <see cref="ISubscriberActor"/> Actors and <see cref="ISubscriberService"/> Services with strict ordering, so less performant than <see cref="BrokerServiceUnordered"/>.
    /// Every message type is mapped to one of the partitions of this service.
    /// </remarks>
    public abstract class BrokerService : BrokerServiceBase
    {
        /// <summary>
        /// Creates a new instance using the provided context and registers this instance for automatic discovery if needed.
        /// </summary>
        /// <param name="serviceContext"></param>
        /// <param name="enableAutoDiscovery"></param>
        /// <param name="brokerEventsManager"></param>
        protected BrokerService(StatefulServiceContext serviceContext, bool enableAutoDiscovery = true, IBrokerEventsManager brokerEventsManager = null)
            : base(serviceContext, enableAutoDiscovery, brokerEventsManager)
        {
        }

        /// <summary>
        /// Creates a new instance using the provided context and registers this instance for automatic discovery if needed.
        /// </summary>
        /// <param name="serviceContext"></param>
        /// <param name="reliableStateManagerReplica"></param>
        /// <param name="enableAutoDiscovery"></param>
        /// <param name="brokerEventsManager"></param>
        protected BrokerService(StatefulServiceContext serviceContext, IReliableStateManagerReplica2 reliableStateManagerReplica, bool enableAutoDiscovery = true, IBrokerEventsManager brokerEventsManager = null)
            : base(serviceContext, reliableStateManagerReplica, enableAutoDiscovery, brokerEventsManager)
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
            var queue = await TimeoutRetryHelper.Execute((token, state) => StateManager.GetOrAddAsync<IReliableQueue<MessageWrapper>>(queueName), cancellationToken: cancellationToken);
            long messageCount = await TimeoutRetryHelper.ExecuteInTransaction(StateManager, (tx, token, state) => queue.GetCountAsync(tx), cancellationToken: cancellationToken);

            if (messageCount == 0L) return;
            messageCount = Math.Min(messageCount, MaxDequeuesInOneIteration);

            ServiceEventSourceMessage($"Processing {messageCount} items from queue {queue.Name} for subscriber: {subscriber.Name}");

            for (long i = 0; i < messageCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await TimeoutRetryHelper.ExecuteInTransaction(StateManager, async (tx, token, state) =>
                {
                    var message = await queue.TryDequeueAsync(tx);
                    if (message.HasValue)
                    {
                        try
                        {
                            await subscriber.PublishAsync(message.Value);
                            await BrokerEventsManager.OnMessageDeliveredAsync(queueName, subscriber, message.Value);
                        }
                        catch (Exception ex)
                        {
                            await BrokerEventsManager.OnMessageDeliveryFailedAsync(queueName, subscriber, message.Value, ex);
                            throw;
                        }
                    }
                }, cancellationToken: cancellationToken);
            }
        }

        protected sealed override async Task EnqueueMessageAsync(MessageWrapper message, Reference subscriber, ITransaction tx)
        {
            var queueResult = await StateManager.TryGetAsync<IReliableQueue<MessageWrapper>>(subscriber.QueueName);
            if (!queueResult.HasValue) return;

            await queueResult.Value.EnqueueAsync(tx, message);
        }

        protected sealed override Task CreateQueueAsync(ITransaction tx, string queueName)
        {
            return StateManager.GetOrAddAsync<IReliableQueue<MessageWrapper>>(tx, queueName);
        }
    }
}