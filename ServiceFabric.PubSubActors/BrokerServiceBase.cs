using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.PubSubActors.Helpers;
using ServiceFabric.PubSubActors.State;
using ServiceFabric.PubSubActors.Subscriber;

namespace ServiceFabric.PubSubActors
{
    /// <remarks>
    /// Base class for a <see cref="StatefulService"/> that serves as a Broker that accepts messages from Actors &
    /// Services and forwards them to <see cref="ISubscriberActor"/> Actors and <see cref="ISubscriberService"/>
    /// Services.  Every message type is mapped to one of the partitions of this service.
    /// </remarks>
    public abstract class BrokerServiceBase : StatefulService, IBrokerService
    {
        private readonly ManualResetEventSlim _initializer = new ManualResetEventSlim(false);
        private readonly ConcurrentDictionary<string, ReferenceWrapper> _queues =
            new ConcurrentDictionary<string, ReferenceWrapper>();

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        /// <summary>
        /// Gets the state key for all subscriber queues.
        /// </summary>
        protected const string Subscribers = "Queues";

        /// <summary>
        /// The name that the <see cref="ServiceReplicaListener"/> instance will get.
        /// </summary>
        public const string ListenerName = BrokerServiceListenerSettings.ListenerName;

        /// <summary>
        /// When Set, this callback will be used to trace Service messages to.
        /// </summary>
        protected Action<string> ServiceEventSourceMessageCallback { get; set; }

        /// <summary>
        /// Gets or sets the interval to wait before starting to publish messages. (Default: 5s after Activation)
        /// </summary>
        protected TimeSpan DueTime { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets the interval to wait between batches of publishing messages. (Default: 5s)
        /// </summary>
        protected TimeSpan Period { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Get or Sets the maximum period to process messages before allowing enqueuing
        /// </summary>
        protected TimeSpan MaxProcessingPeriod { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Gets or Sets the maximum number of messages to de-queue in one iteration of process queue
        /// </summary>
        protected long MaxDequeuesInOneIteration { get; set; } = 100;

        /// <summary>
        /// Creates a new instance using the provided context and registers this instance for automatic discovery if needed.
        /// </summary>
        /// <param name="serviceContext"></param>
        /// <param name="enableAutoDiscovery"></param>
        protected BrokerServiceBase(StatefulServiceContext serviceContext, bool enableAutoDiscovery = true)
            : base(serviceContext)
        {
            if (enableAutoDiscovery)
            {
                new BrokerServiceLocator().RegisterAsync(Context.ServiceName)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
        }

        /// <summary>
        /// Creates a new instance using the provided context and registers this instance for automatic discovery if needed.
        /// </summary>
        /// <param name="serviceContext"></param>
        /// <param name="reliableStateManagerReplica"></param>
        /// <param name="enableAutoDiscovery"></param>
        protected BrokerServiceBase(StatefulServiceContext serviceContext,
            IReliableStateManagerReplica2 reliableStateManagerReplica, bool enableAutoDiscovery = true)
            : base(serviceContext, reliableStateManagerReplica)
        {
            if (enableAutoDiscovery)
            {
                new BrokerServiceLocator().RegisterAsync(Context.ServiceName)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
        }

        /// <summary>
        /// Registers a Service or Actor <paramref name="reference"/> as subscriber for messages of type <paramref name="messageTypeName"/>
        /// </summary>
        /// <param name="reference">Reference to the Service or Actor to register.</param>
        /// <param name="messageTypeName">Full type name of message object.</param>
        /// <returns></returns>
        public async Task SubscribeAsync(ReferenceWrapper reference, string messageTypeName)
        {
            await WaitForInitializeAsync(CancellationToken.None);

            var myDictionary = await TimeoutRetryHelper.Execute((token, state) => StateManager.GetOrAddAsync<IReliableDictionary<string, BrokerServiceState>>(messageTypeName));

            await TimeoutRetryHelper.ExecuteInTransaction(StateManager, async (tx, token, state) =>
            {
                var queueName = CreateQueueName(reference, messageTypeName);

                Func<string, BrokerServiceState> addValueFactory = key =>
                {
                    var newState = new BrokerServiceState(messageTypeName);
                    var subscriber = new Reference(reference, queueName);
                    newState = BrokerServiceState.AddSubscriber(newState, subscriber);
                    return newState;
                };

                Func<string, BrokerServiceState, BrokerServiceState> updateValueFactory = (key, current) =>
                {
                    var subscriber = new Reference(reference, queueName);
                    var newState = BrokerServiceState.AddSubscriber(current, subscriber);
                    return newState;
                };

                await myDictionary.AddOrUpdateAsync(tx, Subscribers, addValueFactory, updateValueFactory);

                await CreateQueueAsync(tx, queueName);

                _queues.AddOrUpdate(queueName, reference, (key, old) => reference);
                ServiceEventSourceMessage($"Registered subscriber: {reference.Name}");
            }, cancellationToken: CancellationToken.None);
        }

        /// <summary>
        /// Unregisters a Service or Actor <paramref name="reference"/> as subscriber for messages of type <paramref name="messageTypeName"/>
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="messageTypeName"></param>
        /// <returns></returns>
        public async Task UnsubscribeAsync(ReferenceWrapper reference, string messageTypeName)
        {
            await WaitForInitializeAsync(CancellationToken.None);

            var myDictionary = await TimeoutRetryHelper.Execute((token, state) => StateManager.GetOrAddAsync<IReliableDictionary<string, BrokerServiceState>>(messageTypeName));
            var queueName = CreateQueueName(reference, messageTypeName);

            await TimeoutRetryHelper.ExecuteInTransaction(StateManager, async (tx, token, state) =>
            {
                var subscribers = await myDictionary.TryGetValueAsync(tx, Subscribers, LockMode.Update);
                if (subscribers.HasValue)
                {
                    var newState = BrokerServiceState.RemoveSubscriber(subscribers.Value, reference);
                    await myDictionary.SetAsync(tx, Subscribers, newState);
                }


                await StateManager.RemoveAsync(tx, queueName);

                ServiceEventSourceMessage($"Unregistered subscriber: {reference.Name}");
                _queues.TryRemove(queueName, out reference);
            });
        }

        /// <summary>
        /// Takes a published message and forwards it (indirectly) to all Subscribers.
        /// </summary>
        /// <param name="message">The message to publish</param>
        /// <returns></returns>
        public async Task PublishMessageAsync(MessageWrapper message)
        {
            await WaitForInitializeAsync(CancellationToken.None);

            var myDictionary = await TimeoutRetryHelper.Execute((token, state) => StateManager.GetOrAddAsync<IReliableDictionary<string, BrokerServiceState>>(message.MessageType));

            var subscribers = await TimeoutRetryHelper.ExecuteInTransaction(StateManager, async (tx, token, state) =>
            {
                var result = await myDictionary.TryGetValueAsync(tx, Subscribers);
                if (result.HasValue)
                {
                    return result.Value.Subscribers.ToArray();
                }
                return null;
            });

            if (subscribers == null || subscribers.Length == 0) return;

            ServiceEventSourceMessage($"Publishing message '{message.MessageType}' to {subscribers.Length} subscribers.");

            await TimeoutRetryHelper.ExecuteInTransaction(StateManager, async (tx, token, state) =>
            {
                foreach (var subscriber in subscribers)
                {
                    await EnqueueMessageAsync(message, subscriber, tx);
                    _queues[subscriber.QueueName].TotalReceived++;
                }
                ServiceEventSourceMessage($"Published message '{message.MessageType}' to {subscribers.Length} subscribers.");
            });
        }

        public Task<QueueStatsWrapper> GetBrokerStatsAsync()
        {
            return Task.FromResult(new QueueStatsWrapper
            {
                Queues = _queues.ToDictionary(reference => reference.Key, reference => reference.Value),
                Stats = _queues.Select(queue => new QueueStats
                {
                    QueueName = queue.Key,
                    ServiceName = queue.Value.Name,
                    Time = DateTime.UtcNow,
                    TotalReceived = queue.Value.TotalReceived,
                    TotalDelivered = queue.Value.TotalDelivered
                })
                .ToList()
            });
        }

        protected abstract Task EnqueueMessageAsync(MessageWrapper message, Reference subscriber, ITransaction tx);


        /// <summary>
        /// Starts a loop that processes all queued messages.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            await WaitForInitializeAsync(cancellationToken);

            ServiceEventSourceMessage($"Sleeping for {DueTime.TotalMilliseconds}ms before starting to publish messages.");

            await Task.Delay(DueTime, cancellationToken);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                //process messages for given time, then allow other transactions to enqueue messages
                var cts = new CancellationTokenSource(MaxProcessingPeriod);
                var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
                try
                {
                    var elements = _queues.ToArray();
                    var tasks = new List<Task>(elements.Length);

                    foreach (var element in elements)
                    {
                        var subscriber = element.Value;
                        string queueName = element.Key;
                        tasks.Add(ProcessQueues(linkedTokenSource.Token, subscriber, queueName));
                    }
                    await Task.WhenAll(tasks);
                }
                catch (TaskCanceledException)
                {//swallow and move on..
                }
                catch (OperationCanceledException)
                {//swallow and move on..
                }
                catch (ObjectDisposedException)
                {//swallow and move on..
                }
                catch (Exception ex)
                {
                    ServiceEventSourceMessage($"Exception caught while processing messages:'{ex.Message}'");
                    //swallow and move on..
                }
                finally
                {
                    linkedTokenSource.Dispose();
                }
                await Task.Delay(Period, cancellationToken);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        /// <inheritdoc />
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            //add the pubsub listener
            yield return new ServiceReplicaListener(context => new FabricTransportServiceRemotingListener(context, this), ListenerName);
        }

        /// <summary>
        /// Blocks the calling thread until <see cref="InitializeAsync"/> is complete.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task WaitForInitializeAsync(CancellationToken cancellationToken)
        {
            if (_initializer.IsSet) return;
            await Task.Run(() => InitializeAsync(cancellationToken), cancellationToken);
            _initializer.Wait(cancellationToken);
        }

        /// <summary>
        /// Loads all registered message queues from state and keeps them in memory. Avoids some locks in the statemanager.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (_initializer.IsSet) return;
            try
            {
                _semaphore.Wait(cancellationToken);

                if (_initializer.IsSet) return;
                await TimeoutRetryHelper.ExecuteInTransaction(StateManager, async (tx, token, state) =>
                {
                    _queues.Clear();
                    var enumerator = StateManager.GetAsyncEnumerator();
                    while (await enumerator.MoveNextAsync(cancellationToken))
                    {
                        var current = enumerator.Current as IReliableDictionary<string, BrokerServiceState>;
                        if (current == null) continue;


                        var result = await current.TryGetValueAsync(tx, Subscribers);
                        if (!result.HasValue) continue;

                        var subscribers = result.Value.Subscribers.ToList();
                        foreach (var subscriber in subscribers)
                        {
                            _queues.TryAdd(subscriber.QueueName, subscriber.ServiceOrActorReference);
                        }
                    }
                }, cancellationToken: cancellationToken);
                _initializer.Set();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Sends out queued messages for the provided queue.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="subscriber"></param>
        /// <param name="queueName"></param>
        /// <returns></returns>
        protected abstract Task ProcessQueues(CancellationToken cancellationToken, ReferenceWrapper subscriber, string queueName);

        /// <summary>
        /// Outputs the provided message to the <see cref="ServiceEventSourceMessageCallback"/> if it's configured.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="caller"></param>
        protected void ServiceEventSourceMessage(string message, [CallerMemberName] string caller = "unknown")
        {
            ServiceEventSourceMessageCallback?.Invoke($"{caller} - {message}");
        }

        protected abstract Task CreateQueueAsync(ITransaction tx, string queueName);

        /// <summary>
        /// Creates a queuename to use for this reference. (message specific)
        /// </summary>
        /// <returns></returns>
        private static string CreateQueueName(ReferenceWrapper reference, string messageTypeName)
        {
            return $"{messageTypeName}_{reference.GetQueueName()}";
        }
    }
}