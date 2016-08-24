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

        protected BrokerService(StatefulServiceContext serviceContext)
                   : base(serviceContext)
        {
            RegisterBrokerService();
        }

        protected BrokerService(StatefulServiceContext serviceContext, IReliableStateManagerReplica reliableStateManagerReplica)
            : base(serviceContext, reliableStateManagerReplica)
        {
            RegisterBrokerService();
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
                    await tran.CommitAsync();
                }
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
                    try
                    {
                        await subscriber.QueueSemaphore.WaitAsync(cancellationToken);
                        var subscriberQueueResult = await StateManager.TryGetAsync<IReliableQueue<MessageWrapper>>(subscriber.SubscriberMessageQueueID);
                        if (!subscriberQueueResult.HasValue) return;
                        var subscriberQueue = subscriberQueueResult.Value;

                        using (var tran = StateManager.CreateTransaction())
                        {
                            await ProcessQueueAsync(tran, subscriber.ReferenceWrapper, subscriberQueue, cancellationToken);
                            await tran.CommitAsync();
                        }
                    }
                    finally
                    {
                        subscriber.QueueSemaphore.Release();
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
            string deadLetterQueueID = Guid.NewGuid().ToString("N");
            var brokerServiceState = new BrokerServiceState
            {
                SubscriberMessageQueueID = messageQueueID,
                SubscriberDeadLetterQueueID = deadLetterQueueID,
                ReferenceWrapper = reference,
                MessageType = messageTypeName,
            };

            using (var tran = StateManager.CreateTransaction())
            {
                //keep subscriber
                var state = await dictionary.GetOrAddAsync(tran, messageTypeName, mtn => new HashSet<BrokerServiceState> {brokerServiceState});
                lock (_persistedStateLock)
                {
                    if (!state.Contains(brokerServiceState))
                    {
                        state.Add(brokerServiceState);
                    }
                }
                //create subscriber queues
                await StateManager.GetOrAddAsync<IReliableQueue<MessageWrapper>>(tran, messageQueueID);
                await StateManager.GetOrAddAsync<IReliableQueue<MessageWrapper>>(tran, deadLetterQueueID);

                await tran.CommitAsync();
            }

            //keep in local memory
            lock (_localStateLock)
            {
                HashSet<BrokerServiceState> list = _messageTypeSubscribers.GetOrAdd(messageTypeName, mtn => new HashSet<BrokerServiceState>());
                list.Add(brokerServiceState);
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
            BrokerServiceState brokerServiceStateEntry;

            using (var tran = StateManager.CreateTransaction())
            {
                var stateResult = await dictionary.TryGetValueAsync(tran, messageTypeName);
                if (!stateResult.HasValue) return;
                var state = stateResult.Value;
                brokerServiceStateEntry = state.SingleOrDefault(s => s.ReferenceWrapper == reference);

                if (brokerServiceStateEntry != null)
                {
                    lock (_persistedStateLock)
                    {
                        state.Remove(brokerServiceStateEntry);
                    }
                }

                await tran.CommitAsync();
            }
            //flush and remove queue
            if (brokerServiceStateEntry != null && flushQueue)
            {
                var queueResult = await StateManager.TryGetAsync<IReliableQueue<MessageWrapper>>(brokerServiceStateEntry.SubscriberMessageQueueID);
                if (!queueResult.HasValue) return;
                var queue = queueResult.Value;
                using (var tran = StateManager.CreateTransaction())
                {
                    await ProcessQueueAsync(tran, reference, queue, CancellationToken.None);
                    await StateManager.RemoveAsync(tran, brokerServiceStateEntry.SubscriberMessageQueueID);
                }
            }

            //remove from local memory
            if (brokerServiceStateEntry != null)
            {
                HashSet<BrokerServiceState> list;
                if (_messageTypeSubscribers.TryGetValue(messageTypeName, out list))
                {
                    lock (_localStateLock)
                    {
                        list.Remove(brokerServiceStateEntry);
                    }
                }
            }
        }

        /// <summary>
        /// When overridden, handles an undeliverable message <paramref name="message"/> for listener <paramref name="reference"/>.
        /// By default, it will be added to the 'dead letter queue'.
        /// </summary>
        /// <param name="tran">Active StateManager Transaction</param>
        /// <param name="reference"></param>
        /// <param name="message"></param>
        protected virtual async Task HandleUndeliverableMessageAsync(ITransaction tran, ReferenceWrapper reference, MessageWrapper message)
        {
            var deadLetters = await GetOrAddDeadLetterQueueAsync(tran, reference);
            var count = await deadLetters.GetCountAsync(tran);
            ServiceEventSourceMessage($"Adding undeliverable message to Dead Letter Queue (Listener: {reference.Name}, Dead Letter Queue depth:{count})");
            await ValidateQueueDepthAsync(tran, reference, deadLetters);
            await deadLetters.EnqueueAsync(tran, message);
        }

        /// <summary>
        /// Returns a 'dead letter queue' for the provided Reference, to store undeliverable messages.
        /// Returns null for unregistered references.
        /// </summary>
        /// <param name="tran">Active StateManager Transaction</param>
        /// <param name="reference"></param>
        /// <returns></returns>
        private async Task<IReliableQueue<MessageWrapper>> GetOrAddDeadLetterQueueAsync(ITransaction tran, ReferenceWrapper reference)
        {
            var brokerStatesResult = await StateManager.TryGetAsync<IReliableDictionary<ReferenceWrapper, BrokerServiceState>>(StateKey);
            if (!brokerStatesResult.HasValue) return null;

            var brokerStateResult = await brokerStatesResult.Value.TryGetValueAsync(tran, reference);
            if (!brokerStateResult.HasValue) return null;

            var queue = await StateManager.GetOrAddAsync<IReliableQueue<MessageWrapper>>(tran, brokerStateResult.Value.SubscriberDeadLetterQueueID);

            return queue;
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
        }

        /// <summary>
        /// Forwards all published messages to one subscriber.
        /// </summary>        
        /// <returns></returns>        
        private async Task ProcessQueueAsync(ITransaction tran, ReferenceWrapper reference, IReliableQueue<MessageWrapper> queue, CancellationToken cancellationToken)
        {
            int messagesProcessed = 0;
            long depth = await queue.GetCountAsync(tran);
            if (depth == 0) return;

            ServiceEventSourceMessage($"Processing {depth} queued messages for '{reference.Name}'.");
            var result = await queue.TryPeekAsync(tran);

            while (result.HasValue && !cancellationToken.IsCancellationRequested)
            {
                MessageWrapper message = result.Value;
                //ServiceEventSourceMessage($"Publishing message to subscriber {reference.Name}");
                try
                {
                    await reference.PublishAsync(message);
                    ServiceEventSourceMessage($"Published message {++messagesProcessed} of {depth} to subscriber {reference.Name}");
                    await queue.TryDequeueAsync(tran);
                }
                catch (Exception ex)
                {
                    await HandleUndeliverableMessageAsync(tran, reference, message);
                    ServiceEventSourceMessage($"Suppressed error while publishing message to subscribe {reference.Name}. Error: {ex}.");
                }
                //next item
                result = await queue.TryPeekAsync(tran);

            }
            if (messagesProcessed > 0)
            {
                ServiceEventSourceMessage($"Processed {messagesProcessed} queued messages for '{reference.Name}'.");
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
        /// Registers this instance as the BrokerService for this Application.
        /// </summary>
        private void RegisterBrokerService()
        {
            FabricClient fc = new FabricClient();
            fc.PropertyManager.PutPropertyAsync(new Uri(Context.CodePackageActivationContext.ApplicationName), nameof(BrokerService), Context.ServiceName.ToString());
        }

        /// <summary>
        /// Copies persisted state into memory for performance:
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task InitializeAsync(CancellationToken cancellationToken)
        {
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
