using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.PubSubActors.Interfaces;
using ServiceFabric.PubSubActors.State;
using System;
using System.Collections.Concurrent;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using System.Collections.Generic;
using ServiceFabric.PubSubActors.PublisherActors;
using ServiceFabric.PubSubActors.SubscriberServices;
using System.Linq;

namespace ServiceFabric.PubSubActors
{
    /// <remarks>
	/// Base class for a <see cref="StatefulService"/> that serves as a Broker that accepts messages 
	/// from Actors & Services calling <see cref="PublisherActorExtensions.PublishMessageAsync"/>
	/// and forwards them to <see cref="ISubscriberActor"/> Actors and <see cref="ISubscriberService"/> Services.
	/// Every message type is mapped to one of the partitions of this service.
	/// </remarks>
    public abstract class BrokerService : StatefulService, IBrokerService
    {
        //non persisted state:
        private readonly ManualResetEvent _isInitialized = new ManualResetEvent(false);
        private readonly ConcurrentDictionary<string, HashSet<BrokerServiceState>> _messageTypeSubscribers = new ConcurrentDictionary<string, HashSet<BrokerServiceState>>();
        private readonly object _localStateLock = new object();
        private readonly object _persistedStateLock = new object();

        private const string StateKey = "__state__";
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        /// <summary>
        /// The name that the <see cref="ServiceReplicaListener"/> instance will get.
        /// </summary>
        public const string ListenerName = "StatefulBrokerServiceFabricTransportServiceRemotingListener";

        /// <summary>
        /// Indicates the maximum size of the Dead Letter Queue for each registered <see cref="ReferenceWrapper"/>. (Default: 100)
        /// </summary>
        protected int MaxDeadLetterCount { get; set; } = 100;
        /// <summary>
        /// Gets or sets the interval to wait before starting to publish messages. (Default: 5s after Activation)
        /// </summary>
        protected TimeSpan DueTime { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets the interval to wait between batches of publishing messages. (Default: 5s)
        /// </summary>
        protected TimeSpan Period { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// When Set, this callback will be used to trace Service messages to.
        /// </summary>
        protected Action<string> ServiceEventSourceMessageCallback { get; set; }

        private int _publishCounter;

        protected BrokerService(StatefulServiceContext serviceContext, bool enableAutoDiscovery = true)
                   : base(serviceContext)
        {
            if (enableAutoDiscovery)
            {
                RegisterBrokerService();
            }
        }

        protected BrokerService(StatefulServiceContext serviceContext, IReliableStateManagerReplica reliableStateManagerReplica, bool enableAutoDiscovery = true)
            : base(serviceContext, reliableStateManagerReplica)
        {
            if (enableAutoDiscovery)
            {
                RegisterBrokerService();
            }
        }

        /// <summary>
        /// Registers an Actor as a subscriber for messages.
        /// </summary>
        /// <param name="actor">Reference to the actor to register.</param>
        /// <param name="messageTypeName">Full type name of message object.</param>
        public Task RegisterSubscriberAsync(ActorReference actor, string messageTypeName)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (string.IsNullOrWhiteSpace(messageTypeName)) throw new ArgumentException(nameof(messageTypeName));

            var actorReference = new ActorReferenceWrapper(actor);
            return RegisterSubscriberPrivateAsync(actorReference, messageTypeName);
        }

        /// <summary>
        /// Unregisters an Actor as a subscriber for messages.
        /// </summary>
        /// <param name="actor">Reference to the actor to unsubscribe.</param>
        /// <param name="messageTypeName">Full type name of message object.</param>
        /// <param name="flushQueue">Publish any remaining messages.</param>
        public Task UnregisterSubscriberAsync(ActorReference actor, string messageTypeName, bool flushQueue)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (string.IsNullOrWhiteSpace(messageTypeName)) throw new ArgumentException(nameof(messageTypeName));

            var actorReference = new ActorReferenceWrapper(actor);
            return UnregisterSubscriberPrivateAsync(actorReference, messageTypeName, flushQueue);
        }

        /// <summary>
        /// Registers a service as a subscriber for messages.
        /// </summary>
        /// <param name="messageTypeName">Full type name of message object.</param>
        /// <param name="service">Reference to the service to register.</param>
        public Task RegisterServiceSubscriberAsync(ServiceReference service, string messageTypeName)
        {
            if (string.IsNullOrWhiteSpace(messageTypeName)) throw new ArgumentException(nameof(messageTypeName));
            if (service == null) throw new ArgumentNullException(nameof(service));

            var serviceReference = new ServiceReferenceWrapper(service);
            return RegisterSubscriberPrivateAsync(serviceReference, messageTypeName);
        }

        /// <summary>
        /// Unregisters a service as a subscriber for messages.
        /// </summary>
        /// <param name="messageTypeName">Full type name of message object.</param>
        /// <param name="service">Reference to the actor to unsubscribe.</param>
        /// <param name="flushQueue">Publish any remaining messages.</param>
        public Task UnregisterServiceSubscriberAsync(ServiceReference service, string messageTypeName, bool flushQueue)
        {
            if (string.IsNullOrWhiteSpace(messageTypeName)) throw new ArgumentException(nameof(messageTypeName));
            if (service == null) throw new ArgumentNullException(nameof(service));

            var serviceReference = new ServiceReferenceWrapper(service);
            return UnregisterSubscriberPrivateAsync(serviceReference, messageTypeName, flushQueue);
        }

        /// <summary>
        /// Takes a published message and forwards it (indirectly) to all Subscribers.
        /// </summary>
        /// <param name="message">The message to publish</param>
        /// <returns></returns>
        public async Task PublishMessageAsync(MessageWrapper message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            await WaitForInitializeAsync();

            //get subscribers:
            var subscribers = GetSubscribersForMessage(message);
            if (subscribers == null || subscribers.Count == 0) return;

            //publish to all subscribers

            using (var tran = StateManager.CreateTransaction())
            {
                foreach (var subscriber in subscribers)
                {
                    try
                    {
                        await subscriber.QueueSemaphore.WaitAsync();
                        var subscriberQueueResult = await StateManager.TryGetAsync<IReliableQueue<MessageWrapper>>(subscriber.SubscriberMessageQueueID);
                        if (!subscriberQueueResult.HasValue) return;
                        var queue = subscriberQueueResult.Value;
                        await queue.EnqueueAsync(tran, message);
                    }
                    finally
                    {
                        subscriber.QueueSemaphore.Release();
                    }
                }
                await tran.CommitAsync();
            }


            if (++_publishCounter % 100 == 0)
            {
                ServiceEventSourceMessage($"Published {_publishCounter} messages so far.");
            }
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            yield return new ServiceReplicaListener(context => new Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Runtime.FabricTransportServiceRemotingListener(context, this), ListenerName);
        }

        /// <summary>
        /// Services that want to implement a processing loop which runs when it is primary
        /// and has write status, just override this method with their logic.
        /// </summary>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            //map persisted state into local state:
            await InitializeAsync(cancellationToken);

            //sleep before start pumping messages:
            await Task.Delay(DueTime, cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                List<BrokerServiceState> subscribers;
                lock (_localStateLock)
                {
                    subscribers = _messageTypeSubscribers.Values.SelectMany(s => s)
                            .Distinct(new BrokerServiceStateReferenceEqualComparer())
                            .ToList();
                }

                Parallel.ForEach(subscribers, async subscriber =>
                {
                    using (var tran = StateManager.CreateTransaction())
                    {
                        try
                        {
                            await ProcessQueueAsync(tran, subscriber, cancellationToken);
                            await tran.CommitAsync();
                        }
                        catch (Exception ex)
                        {
                            ServiceEventSourceMessage($"Failed to publish messages of type '{subscriber.MessageType}' to subscriber '{subscriber.ReferenceWrapper.Name}'. Error:'{ex}'.");
                            tran.Abort();
                        }
                    }
                });

                await Task.Delay(Period, cancellationToken);
            }
        }

        /// <summary>
        /// Gets all currently subscribed services and actors for the given message type. May return null.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private HashSet<BrokerServiceState> GetSubscribersForMessage(MessageWrapper message)
        {
            HashSet<BrokerServiceState> subscribers;
            lock (_localStateLock)
            {
                if (_messageTypeSubscribers.TryGetValue(message.MessageType, out subscribers) && subscribers != null && subscribers.Count > 0)
                {
                    //copy local to release lock
                    subscribers = new HashSet<BrokerServiceState>(subscribers);
                }
            }

            return subscribers;
        }

        private async Task<IReliableDictionary<string, HashSet<BrokerServiceState>>> GetOrAddStateAsync()
        {
            IReliableDictionary<string, HashSet<BrokerServiceState>> dictionary = null;
            try
            {
                await _semaphore.WaitAsync();
                dictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, HashSet<BrokerServiceState>>>(StateKey);
            }
            catch (Exception ex)
            {
                ServiceEventSourceMessage($"Failed to acquire semaphore lock. Error:'{ex}'");
            }
            finally
            {
                _semaphore.Release();
            }

            return dictionary;
        }

        /// <summary>
        /// Registers a ReferenceWrapper as a subscriber for messages.
        /// </summary>
        /// <param name="reference">Reference to register.</param>
        /// <param name="messageTypeName"></param>
        private async Task RegisterSubscriberPrivateAsync(ReferenceWrapper reference, string messageTypeName)
        {
            await WaitForInitializeAsync();

            ServiceEventSourceMessage($"Registering Subscriber '{reference.Name}' for messages of type {messageTypeName}.");

            var dictionary = await GetOrAddStateAsync();
            string messageQueueID = reference.GetHashCode().ToString();
            string deadLetterQueueID = messageQueueID + "_deadletters";
            var brokerServiceState = new BrokerServiceState
            {
                SubscriberMessageQueueID = messageQueueID,
                SubscriberDeadLetterQueueID = deadLetterQueueID,
                ReferenceWrapper = reference,
                MessageType = messageTypeName,
            };
            HashSet<BrokerServiceState> set;
            //check in local memory
            lock (_localStateLock)
            {
                set = _messageTypeSubscribers.GetOrAdd(messageTypeName, mtn => new HashSet<BrokerServiceState>());
                if (set.Contains(brokerServiceState, new BrokerServiceStateReferenceEqualComparer()))
                {
                    //already registered
                    return;
                }
            }

            using (var tran = StateManager.CreateTransaction())
            {
                //keep subscriber
                var state = await dictionary.GetOrAddAsync(tran, messageTypeName, mtn => new HashSet<BrokerServiceState>());

                if (!state.Contains(brokerServiceState, new BrokerServiceStateReferenceEqualComparer()))
                {
                    lock (_persistedStateLock)
                    {
                        state.Add(brokerServiceState);
                    }

                    await dictionary.SetAsync(tran, messageTypeName, state);
                    //create subscriber queues
                    await StateManager.GetOrAddAsync<IReliableQueue<MessageWrapper>>(tran, messageQueueID);
                    await StateManager.GetOrAddAsync<IReliableQueue<MessageWrapper>>(tran, deadLetterQueueID);
                    await tran.CommitAsync();
                }
            }

            //keep in local memory
            lock (_localStateLock)
            {
                if (!set.Contains(brokerServiceState, new BrokerServiceStateReferenceEqualComparer()))
                {
                    set.Add(brokerServiceState);
                }
            }
        }

        /// <summary>
        /// Unregisters a ReferenceWrapper as a subscriber for messages.
        /// </summary>
        /// <param name="reference">Reference to unsubscribe.</param>
        /// <param name="messageTypeName"></param>
        /// <param name="flushQueue">Publish any remaining messages.</param>
        private async Task UnregisterSubscriberPrivateAsync(ReferenceWrapper reference, string messageTypeName, bool flushQueue)
        {
            await WaitForInitializeAsync();

            ServiceEventSourceMessage($"Unegistering Subscriber '{reference.Name}' for messages of type {messageTypeName}.");

            var dictionary = await GetOrAddStateAsync();
            BrokerServiceState subscriber = null;

            using (var tran = StateManager.CreateTransaction())
            {
                try
                {
                    var stateResult = await dictionary.TryGetValueAsync(tran, messageTypeName);
                    if (!stateResult.HasValue) return;
                    var state = stateResult.Value;
                    subscriber = state.SingleOrDefault(s => s.ReferenceWrapper == reference);

                    if (subscriber != null)
                    {
                        lock (_persistedStateLock)
                        {
                            state.Remove(subscriber);
                        }
                        await dictionary.SetAsync(tran, messageTypeName, state);
                    }

                    await tran.CommitAsync();
                }
                catch (Exception ex)
                {
                    ServiceEventSourceMessage($"Failed to publish messages of type '{subscriber?.MessageType}' to subscriber '{subscriber?.ReferenceWrapper.Name}'. Error:'{ex}'.");
                    tran.Abort();
                }
            }

            //flush and remove queue
            if (subscriber != null)
            {
                if (flushQueue)
                {
                    using (var tran = StateManager.CreateTransaction())
                    {
                        await ProcessQueueAsync(tran, subscriber, CancellationToken.None);
                        await StateManager.RemoveAsync(tran, subscriber.SubscriberMessageQueueID);
                    }
                }

                //remove from local memory
                HashSet<BrokerServiceState> set;
                if (_messageTypeSubscribers.TryGetValue(messageTypeName, out set))
                {
                    lock (_localStateLock)
                    {
                        var local = set.SingleOrDefault(s => s.ReferenceWrapper == subscriber.ReferenceWrapper);
                        set.Remove(local);
                    }
                }
            }
        }

        /// <summary>
        /// Ensures the Queue depth is less than the allowed maximum.
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="tran">Active StateManager Transaction</param>
        /// <param name="deadLetters"></param>
        private async Task ValidateQueueDepthAsync(ITransaction tran, ReferenceWrapper reference, IReliableQueue<MessageWrapper> deadLetters)
        {
            var queueDepth = await deadLetters.GetCountAsync(tran);
            if (queueDepth > MaxDeadLetterCount)
            {
                ServiceEventSourceMessage(
                    $"Dead Letter Queue for Subscriber '{reference.Name}' has {queueDepth} items, which is more than the allowed {MaxDeadLetterCount}. Clearing it.");
                await deadLetters.ClearAsync();
            }
            else
            {
                ServiceEventSourceMessage(
                  $"Dead Letter Queue for Subscriber '{reference.Name}' has {queueDepth} items!");
            }
        }

        /// <summary>
        /// Forwards all published messages to one subscriber.
        /// </summary>        
        /// <returns></returns>        
        private async Task ProcessQueueAsync(ITransaction tran, BrokerServiceState subscriber, CancellationToken cancellationToken)
        {
            try
            {
                await subscriber.QueueSemaphore.WaitAsync(cancellationToken);
                
                //get subscriber queue
                var subscriberQueueResult = await StateManager.TryGetAsync<IReliableQueue<MessageWrapper>>(subscriber.SubscriberMessageQueueID);
                if (!subscriberQueueResult.HasValue) return;
                var subscriberQueue = subscriberQueueResult.Value;
                int messagesProcessed = 0;
                long depth = await subscriberQueue.GetCountAsync(tran);
                if (depth == 0) return;

                //lazy to deadletter queue.
                var deadLetterQueueResult = new Lazy<Task<IReliableQueue<MessageWrapper>>>(async () =>
                {
                    var queueResult = await StateManager.TryGetAsync<IReliableQueue<MessageWrapper>>(subscriber.SubscriberDeadLetterQueueID);
                    if (!queueResult.HasValue) return null;
                    var deadLetterQueue = queueResult.Value;
                    return deadLetterQueue;
                });
                
                ServiceEventSourceMessage($"Processing queu with about {depth} messages for '{subscriber.ReferenceWrapper.Name}'.");
                var result = await subscriberQueue.TryPeekAsync(tran);

                while (result.HasValue && !cancellationToken.IsCancellationRequested)
                {
                    MessageWrapper message = result.Value;
                    try
                    {
                        await subscriber.ReferenceWrapper.PublishAsync(message);
                        ServiceEventSourceMessage($"Published message {++messagesProcessed} to subscriber {subscriber.ReferenceWrapper.Name}");
                        await subscriberQueue.TryDequeueAsync(tran);
                    }
                    catch (Exception ex)
                    {
                        var deadLetterQueue = await deadLetterQueueResult.Value;
                        await deadLetterQueue.EnqueueAsync(tran, message);
                        ServiceEventSourceMessage($"Suppressed error while publishing message to subscribe {subscriber.ReferenceWrapper.Name}. Error: {ex}.");
                        await ValidateQueueDepthAsync(tran, subscriber.ReferenceWrapper, deadLetterQueue);
                    }
                    //fetch next item
                    result = await subscriberQueue.TryPeekAsync(tran);

                }
                if (messagesProcessed > 0)
                {
                    ServiceEventSourceMessage($"Processed {messagesProcessed} queued messages for '{subscriber.ReferenceWrapper.Name}'.");
                }
            }
            finally
            {
                subscriber.QueueSemaphore.Release();
            }
        }

        /// <summary>
        /// Outputs the provided message to the <see cref="ServiceEventSourceMessageCallback"/> if it's configured.
        /// </summary>
        /// <param name="message"></param>
        private void ServiceEventSourceMessage(string message)
        {
            ServiceEventSourceMessageCallback?.Invoke(message);
        }

        /// <summary>
        /// Registers this instance as the (one and only) BrokerService for this Application. This makes it discoverable to other services without them knowing this service's name.
        /// </summary>
        private void RegisterBrokerService()
        {
            var fc = new FabricClient();
            fc.PropertyManager.PutPropertyAsync(new Uri(Context.CodePackageActivationContext.ApplicationName), nameof(BrokerService), Context.ServiceName.ToString());
        }

        /// <summary>
        /// Copies persisted state into memory for performance:
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task InitializeAsync(CancellationToken cancellationToken)
        {
            //wait until usable;
            while (Partition.WriteStatus != PartitionAccessStatus.Granted && Partition.WriteStatus != PartitionAccessStatus.NotPrimary)
            {
                await Task.Delay(100, cancellationToken);
            }

            if (Partition.WriteStatus == PartitionAccessStatus.NotPrimary)
            {
                throw new Exception("Stopping a broker service InitializeAsync on a secondary replica. Not an issue.");
            }

            var messageSubscriberInfo = await GetOrAddStateAsync();
            using (var tran = StateManager.CreateTransaction())
            {
                //iterate over all subscribers, copy them into a local dictionary:
                var enumerable = await messageSubscriberInfo.CreateEnumerableAsync(tran, EnumerationMode.Unordered);
                var enumerator = enumerable.GetAsyncEnumerator();
                while (await enumerator.MoveNextAsync(cancellationToken))
                {
                    var stateEntries = enumerator.Current.Value;
                    var messageTypeName = enumerator.Current.Key;
                    foreach (var stateEntry in stateEntries)
                    {
                        var services = _messageTypeSubscribers.GetOrAdd(messageTypeName, mtn => new HashSet<BrokerServiceState>());
                        lock (_localStateLock)
                        {
                            services.Add(stateEntry);
                        }
                    }
                }
                await tran.CommitAsync();
            }
            _isInitialized.Set();
        }


        private Task WaitForInitializeAsync()
        {
            return _isInitialized.WaitOneAsync();
        }
    }

    internal static class WaitHandleExt
    {

        public static async Task<bool> WaitOneAsync(this WaitHandle handle, int millisecondsTimeout,
            CancellationToken cancellationToken)
        {
            RegisteredWaitHandle registeredHandle = null;
            CancellationTokenRegistration tokenRegistration = default(CancellationTokenRegistration);
            try
            {
                var tcs = new TaskCompletionSource<bool>();
                registeredHandle = ThreadPool.RegisterWaitForSingleObject(
                    handle,
                    (state, timedOut) => ((TaskCompletionSource<bool>)state).TrySetResult(!timedOut),
                    tcs,
                    millisecondsTimeout,
                    true);
                tokenRegistration = cancellationToken.Register(
                    state => ((TaskCompletionSource<bool>)state).TrySetCanceled(),
                    tcs);
                return await tcs.Task;
            }
            finally
            {
                if (registeredHandle != null)
                    registeredHandle.Unregister(null);
                tokenRegistration.Dispose();
            }
        }

        public static Task<bool> WaitOneAsync(this WaitHandle handle, TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            return handle.WaitOneAsync((int)timeout.TotalMilliseconds, cancellationToken);
        }

        public static Task<bool> WaitOneAsync(this WaitHandle handle, CancellationToken cancellationToken)
        {
            return handle.WaitOneAsync(Timeout.Infinite, cancellationToken);
        }
        public static Task<bool> WaitOneAsync(this WaitHandle handle)
        {
            return handle.WaitOneAsync(Timeout.Infinite, CancellationToken.None);
        }
    }
}
