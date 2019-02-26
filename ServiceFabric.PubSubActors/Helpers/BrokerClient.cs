using System;
using System.Collections.Generic;
using System.Fabric;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.PubSubActors.Interfaces;
using ServiceFabric.PubSubActors.State;

namespace ServiceFabric.PubSubActors.Helpers
{
    public class BrokerClient : IBrokerClient
    {
        private readonly IBrokerServiceLocator _brokerServiceLocator;

        /// <summary>
        /// The message types that this service subscribes to and their respective handler methods.
        /// </summary>
        protected Dictionary<Type, Func<object, Task>> Handlers { get; set; } = new Dictionary<Type, Func<object, Task>>();

        public BrokerClient(IBrokerServiceLocator brokerServiceLocator = null)
        {
            _brokerServiceLocator = brokerServiceLocator ?? new BrokerServiceLocator();
        }

        /// <summary>
        /// Publish a message.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task PublishMessageAsync(object message)
        {
            var brokerService = await _brokerServiceLocator.GetBrokerServiceForMessageAsync(message);
            await brokerService.PublishMessageAsync(message.CreateMessageWrapper());
        }

        /// <inheritdoc/>
        public Task SubscribeAsync<T>(StatelessService service, Type messageType, Func<T, Task> handler, string listenerName = null) where T : class
        {
            return SubscribeAsync(CreateReferenceWrapper(service, listenerName), messageType, handler);
        }

        /// <inheritdoc/>
        public Task SubscribeAsync<T>(StatefulService service, Type messageType, Func<T, Task> handler, string listenerName = null) where T : class
        {
            return SubscribeAsync(CreateReferenceWrapper(service, listenerName), messageType, handler);
        }

        /// <inheritdoc/>
        public Task SubscribeAsync<T>(ActorBase actor, Type messageType, Func<T, Task> handler) where T : class
        {
            return SubscribeAsync(CreateReferenceWrapper(actor), messageType, handler);
        }

        /// <inheritdoc/>
        public Task UnsubscribeAsync(StatelessService service, Type messageType, bool flush)
        {
            return UnsubscribeAsync(CreateReferenceWrapper(service), messageType, flush);
        }

        /// <inheritdoc/>
        public Task UnsubscribeAsync(StatefulService service, Type messageType, bool flush)
        {
            return UnsubscribeAsync(CreateReferenceWrapper(service), messageType, flush);
        }

        /// <inheritdoc/>
        public Task UnsubscribeAsync(ActorBase actor, Type messageType, bool flush)
        {
            return UnsubscribeAsync(CreateReferenceWrapper(actor), messageType, flush);
        }

        public Task ProcessMessageAsync(MessageWrapper messageWrapper)
        {
            var messageType = Assembly.Load(messageWrapper.Assembly).GetType(messageWrapper.MessageType, true);

            while (messageType != null)
            {
                if (Handlers.TryGetValue(messageType, out var handler))
                {
                    return handler(messageWrapper.CreateMessage());
                }
                messageType = messageType.BaseType;
            }

            return Task.FromResult(true);
        }

        /// <summary>
        /// Registers a Service or Actor as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <returns></returns>
        private async Task SubscribeAsync<T>(ReferenceWrapper referenceWrapper, Type messageType, Func<T, Task> handler) where T : class
        {
            Handlers[messageType] = message => handler((T)message);
            var brokerService = await _brokerServiceLocator.GetBrokerServiceForMessageAsync(messageType.FullName);
            await brokerService.SubscribeAsync(referenceWrapper, messageType.FullName);
        }

        /// <summary>
        /// Unregisters a Service or Actor as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <returns></returns>
        private async Task UnsubscribeAsync(ReferenceWrapper referenceWrapper, Type messageType, bool flushQueue)
        {
            var brokerService = await _brokerServiceLocator.GetBrokerServiceForMessageAsync(messageType.FullName);
            await brokerService.UnsubscribeAsync(referenceWrapper, messageType.FullName, flushQueue);
        }

        /// <summary>
        /// Create a ReferenceWrapper object given this StatelessService.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="listenerName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private ReferenceWrapper CreateReferenceWrapper(StatelessService service, string listenerName = null)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            var servicePartition = GetPropertyValue<StatelessService, IServicePartition>(service, "Partition");
            return new ServiceReferenceWrapper(CreateServiceReference(service.Context, servicePartition.PartitionInfo, listenerName));
        }

        /// <summary>
        /// Create a ReferenceWrapper object given this StatefulService.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="listenerName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private ReferenceWrapper CreateReferenceWrapper(StatefulService service, string listenerName = null)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            var servicePartition = GetPropertyValue<StatefulService, IServicePartition>(service, "Partition");
            return new ServiceReferenceWrapper(CreateServiceReference(service.Context, servicePartition.PartitionInfo, listenerName));
        }

        /// <summary>
        /// Create a ReferenceWrapper object given this Actor.
        /// </summary>
        /// <param name="actor"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private ReferenceWrapper CreateReferenceWrapper(ActorBase actor)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            return new ActorReferenceWrapper(ActorReference.Get(actor));
        }

        /// <summary>
        /// Creates a <see cref="ServiceReference"/> for the provided service context and partition info.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="info"></param>
        /// <param name="listenerName">(optional) The name of the listener that is used to communicate with the service</param>
        /// <returns></returns>
        private ServiceReference CreateServiceReference(ServiceContext context, ServicePartitionInformation info, string listenerName = null)
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

        private TProperty GetPropertyValue<TClass, TProperty>(TClass instance, string propertyName)
        {
            return (TProperty)(typeof(TClass)
                .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic)?
                .GetValue(instance) ?? throw new ArgumentNullException($"Unable to find property: '{propertyName}' on: '{instance}'"));
        }
    }
}