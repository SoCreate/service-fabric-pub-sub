using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using SoCreate.ServiceFabric.PubSub.Helpers;
using SoCreate.ServiceFabric.PubSub.State;

namespace SoCreate.ServiceFabric.PubSub
{
    public class BrokerClient : IBrokerClient
    {
        private readonly IBrokerServiceLocator _brokerServiceLocator;

        /// <summary>
        /// The message types that this service subscribes to and their respective handler methods.
        /// </summary>
        protected Dictionary<Type, Func<object, Task>> Handlers { get; set; } = new Dictionary<Type, Func<object, Task>>();

        /// <summary>
        /// A list of QueueStats for each queue on the Broker Service.  Hold up to <see cref="QueueStatCapacity"/> items at a time.
        /// </summary>
        private Dictionary<string, List<QueueStats>> _queueStats = new Dictionary<string, List<QueueStats>>();

        /// <summary>
        /// A dictionary of Reference Wrappers, keyed by queue name, representing all queues on the Broker Service.
        /// </summary>
        private Dictionary<string, ReferenceWrapper> _subscriberReferences = new Dictionary<string, ReferenceWrapper>();

        public int QueueStatCapacity { get; set; } = 100;

        public BrokerClient(IBrokerServiceLocator brokerServiceLocator = null)
        {
            _brokerServiceLocator = brokerServiceLocator ?? new BrokerServiceLocator();
        }

        /// <inheritdoc />
        public async Task PublishMessageAsync<T>(T message) where T : class
        {
            var brokerService = await _brokerServiceLocator.GetBrokerServiceForMessageAsync(message);
            await brokerService.PublishMessageAsync(message.CreateMessageWrapper());
        }

        /// <inheritdoc />
        public async Task SubscribeAsync<T>(ReferenceWrapper referenceWrapper, Type messageType, Func<T, Task> handler) where T : class
        {
            Handlers[messageType] = message => handler((T)message);
            var brokerService = await _brokerServiceLocator.GetBrokerServiceForMessageAsync(messageType);
            await brokerService.SubscribeAsync(referenceWrapper, messageType.FullName);
        }

        /// <inheritdoc />
        public async Task UnsubscribeAsync(ReferenceWrapper referenceWrapper, Type messageType)
        {
            var brokerService = await _brokerServiceLocator.GetBrokerServiceForMessageAsync(messageType);
            await brokerService.UnsubscribeAsync(referenceWrapper, messageType.FullName);
        }

        /// <inheritdoc />
        public Task ProcessMessageAsync(MessageWrapper messageWrapper)
        {
            var message = messageWrapper.CreateMessage();
            var messageType = message.GetType();

            while (messageType != null)
            {
                if (Handlers.TryGetValue(messageType, out var handler))
                {
                    return handler(message);
                }
                messageType = messageType.BaseType;
            }

            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, List<QueueStats>>> GetBrokerStatsAsync()
        {
            var tasks = from brokerService in await _brokerServiceLocator.GetBrokerServicesForAllPartitionsAsync()
                        select brokerService.GetBrokerStatsAsync();

            var allStats = await Task.WhenAll(tasks.ToList());
            foreach (var stat in allStats.SelectMany(stat => stat.Stats))
            {
                if (!_queueStats.ContainsKey(stat.QueueName))
                {
                    _queueStats[stat.QueueName] = new List<QueueStats>();
                }
                _queueStats[stat.QueueName].Add(stat);
                if (_queueStats[stat.QueueName].Count > QueueStatCapacity)
                {
                    _queueStats[stat.QueueName].RemoveRange(0, _queueStats[stat.QueueName].Count - QueueStatCapacity);
                }
            }

            _subscriberReferences = allStats.SelectMany(stat => stat.Queues).ToDictionary(i => i.Key, i => i.Value);

            return _queueStats;
        }

        /// <inheritdoc />
        public async Task UnsubscribeByQueueNameAsync(string queueName)
        {
            if (!_subscriberReferences.ContainsKey(queueName))
            {
                await GetBrokerStatsAsync();
            }

            if (_subscriberReferences.TryGetValue(queueName, out var referenceWrapper))
            {
                var messageType = queueName.Split('_')[0];
                var brokerService = await _brokerServiceLocator.GetBrokerServiceForMessageAsync(messageType);
                await brokerService.UnsubscribeAsync(referenceWrapper, messageType);
            }
        }
    }

    public static class BrokerClientExtensions
    {
        // subscribe/unsubscribe using Generic type (useful when subscibing manually)

        /// <summary>
        /// Registers this StatelessService as a subscriber for messages of type <typeparam name="T"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <param name="brokerClient"></param>
        /// <param name="service"></param>
        /// <param name="handler"></param>
        /// <param name="listenerName"></param>
        /// <param name="routingKey">Optional routing key to filter messages based on content. 'Key=Value' where Key is a message property path and Value is the value to match with message payload content.</param>
        /// <returns></returns>
        public static Task SubscribeAsync<T>(this IBrokerClient brokerClient, StatelessService service, Func<T, Task> handler, string listenerName = null, string routingKey = null) where T : class
        {
            return brokerClient.SubscribeAsync(CreateReferenceWrapper(service, listenerName, routingKey), typeof(T), handler);
        }

        /// <summary>
        /// Registers this StatefulService as a subscriber for messages of type <typeparam name="T"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <param name="brokerClient"></param>
        /// <param name="service"></param>
        /// <param name="handler"></param>
        /// <param name="listenerName"></param>
        /// <param name="routingKey">Optional routing key to filter messages based on content. 'Key=Value' where Key is a message property path and Value is the value to match with message payload content.</param>
        /// <returns></returns>
        public static Task SubscribeAsync<T>(this IBrokerClient brokerClient, StatefulService service, Func<T, Task> handler, string listenerName = null, string routingKey = null) where T : class
        {
            return brokerClient.SubscribeAsync(CreateReferenceWrapper(service, listenerName, routingKey), typeof(T), handler);
        }

        /// <summary>
        /// Registers this Actor as a subscriber for messages of type <typeparam name="T"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <param name="brokerClient"></param>
        /// <param name="actor"></param>
        /// <param name="handler"></param>
        /// <param name="routingKey">Optional routing key to filter messages based on content. 'Key=Value' where Key is a message property path and Value is the value to match with message payload content.</param>
        /// <returns></returns>
        public static Task SubscribeAsync<T>(this IBrokerClient brokerClient, ActorBase actor, Func<T, Task> handler, string routingKey = null) where T : class
        {
            return brokerClient.SubscribeAsync(CreateReferenceWrapper(actor, routingKey), typeof(T), handler);
        }

        /// <summary>
        /// Unregisters this StatelessService as a subscriber for messages of type <typeparam name="T"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <param name="brokerClient"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public static Task UnsubscribeAsync<T>(this IBrokerClient brokerClient, StatelessService service) where T : class
        {
            return brokerClient.UnsubscribeAsync(CreateReferenceWrapper(service), typeof(T));
        }

        /// <summary>
        /// Unregisters this StatefulService as a subscriber for messages of type <typeparam name="T"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <param name="brokerClient"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public static Task UnsubscribeAsync<T>(this IBrokerClient brokerClient, StatefulService service) where T : class
        {
            return brokerClient.UnsubscribeAsync(CreateReferenceWrapper(service), typeof(T));
        }

        /// <summary>
        /// Unregisters this Actor as a subscriber for messages of type <typeparam name="T"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <param name="brokerClient"></param>
        /// <param name="actor"></param>
        /// <returns></returns>
        public static Task UnsubscribeAsync<T>(this IBrokerClient brokerClient, ActorBase actor) where T : class
        {
            return brokerClient.UnsubscribeAsync(CreateReferenceWrapper(actor), typeof(T));
        }

        // subscribe/unsubscribe using Type (useful when processing Subscribe attributes)

        internal static Task SubscribeAsync<T>(this IBrokerClient brokerClient, StatelessService service, Type messageType, Func<T, Task> handler, string listenerName = null, string routingKey = null) where T : class
        {
            return brokerClient.SubscribeAsync(CreateReferenceWrapper(service, listenerName, routingKey), messageType, handler);
        }

        internal static Task SubscribeAsync<T>(this IBrokerClient brokerClient, StatefulService service, Type messageType, Func<T, Task> handler, string listenerName = null, string routingKey = null) where T : class
        {
            return brokerClient.SubscribeAsync(CreateReferenceWrapper(service, listenerName, routingKey), messageType, handler);
        }

        internal static Task SubscribeAsync<T>(this IBrokerClient brokerClient, ActorBase actor, Type messageType, Func<T, Task> handler, string routingKey = null) where T : class
        {
            return brokerClient.SubscribeAsync(CreateReferenceWrapper(actor, routingKey), messageType, handler);
        }

        internal static Task UnsubscribeAsync(this IBrokerClient brokerClient, StatelessService service, Type messageType)
        {
            return brokerClient.UnsubscribeAsync(CreateReferenceWrapper(service), messageType);
        }

        internal static Task UnsubscribeAsync(this IBrokerClient brokerClient, StatefulService service, Type messageType)
        {
            return brokerClient.UnsubscribeAsync(CreateReferenceWrapper(service), messageType);
        }

        internal static Task UnsubscribeAsync(this IBrokerClient brokerClient, ActorBase actor, Type messageType)
        {
            return brokerClient.UnsubscribeAsync(CreateReferenceWrapper(actor), messageType);
        }

        /// <summary>
        /// Create a ReferenceWrapper object given this StatelessService.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="listenerName"></param>
        /// <param name="routingKey">Optional routing key to filter messages based on content. 'Key=Value' where Key is a message property path and Value is the value to match with message payload content.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ReferenceWrapper CreateReferenceWrapper(this StatelessService service, string listenerName = null, string routingKey = null)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            var servicePartition = GetPropertyValue<StatelessService, IServicePartition>(service, "Partition");
            return new ServiceReferenceWrapper(CreateServiceReference(service.Context, servicePartition.PartitionInfo, listenerName), routingKey);
        }

        /// <summary>
        /// Create a ReferenceWrapper object given this StatefulService.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="listenerName"></param>
        /// <param name="routingKey">Optional routing key to filter messages based on content. 'Key=Value' where Key is a message property path and Value is the value to match with message payload content.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ReferenceWrapper CreateReferenceWrapper(this StatefulService service, string listenerName = null, string routingKey = null)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            var servicePartition = GetPropertyValue<StatefulService, IServicePartition>(service, "Partition");
            return new ServiceReferenceWrapper(CreateServiceReference(service.Context, servicePartition.PartitionInfo, listenerName), routingKey);
        }

        /// <summary>
        /// Create a ReferenceWrapper object given this Actor.
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="routingKey">Optional routing key to filter messages based on content. 'Key=Value' where Key is a message property path and Value is the value to match with message payload content.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ReferenceWrapper CreateReferenceWrapper(this ActorBase actor, string routingKey = null)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            return new ActorReferenceWrapper(ActorReference.Get(actor), routingKey);
        }

        /// <summary>
        /// Creates a <see cref="ServiceReference"/> for the provided service context and partition info.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="info"></param>
        /// <param name="listenerName">(optional) The name of the listener that is used to communicate with the service</param>
        /// <returns></returns>
        private static ServiceReference CreateServiceReference(ServiceContext context, ServicePartitionInformation info, string listenerName = null)
        {
            var serviceReference = new ServiceReference
            {
                ApplicationName = context.CodePackageActivationContext.ApplicationName,
                PartitionKind = info.Kind,
                ServiceUri = context.ServiceName,
                PartitionGuid = context.PartitionId,
                ListenerName = listenerName
            };

            if (info is Int64RangePartitionInformation longInfo)
            {
                serviceReference.PartitionKey = longInfo.LowKey;
            }
            else if (info is NamedPartitionInformation stringInfo)
            {
                serviceReference.PartitionName = stringInfo.Name;
            }

            return serviceReference;
        }

        private static TProperty GetPropertyValue<TClass, TProperty>(TClass instance, string propertyName)
        {
            return (TProperty)(typeof(TClass)
               .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic)?
               .GetValue(instance) ?? throw new ArgumentNullException($"Unable to find property: '{propertyName}' on: '{instance}'"));
        }
    }
}