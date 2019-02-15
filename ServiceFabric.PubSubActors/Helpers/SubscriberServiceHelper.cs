﻿using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.PubSubActors.Interfaces;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ServiceFabric.PubSubActors.SubscriberServices;

namespace ServiceFabric.PubSubActors.Helpers
{
    public class SubscriberServiceHelper : ISubscriberServiceHelper
    {
        /// <summary>
        /// When Set, this callback will be used to trace Service messages to.
        /// </summary>
        private readonly Action<string> _logCallback;

        private readonly IBrokerServiceLocator _brokerServiceLocator;

        public SubscriberServiceHelper()
        {
            _brokerServiceLocator = new BrokerServiceLocator();
        }

        public SubscriberServiceHelper(Action<string> logCallback = null)
        {
            _brokerServiceLocator = new BrokerServiceLocator();
            _logCallback = logCallback;
        }

        public SubscriberServiceHelper(IBrokerServiceLocator brokerServiceLocator, Action<string> logCallback = null)
        {
            _brokerServiceLocator = brokerServiceLocator;
            _logCallback = logCallback;
        }

        /// <summary>
        /// Registers this Actor as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <returns></returns>
        public async Task RegisterMessageTypeAsync(StatelessService service, Type messageType,
            Uri brokerServiceName = null, string listenerName = null)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            var serviceReference = CreateServiceReference(service, listenerName);
            await RegisterMessageTypeAsync(serviceReference, messageType, brokerServiceName);
        }

        private async Task RegisterMessageTypeAsync(ServiceReference serviceReference, Type messageType, Uri brokerServiceName = null)
        {
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            if (brokerServiceName == null)
            {
                brokerServiceName = await PublisherServiceHelper.DiscoverBrokerServiceNameAsync();
                if (brokerServiceName == null)
                {
                    throw new InvalidOperationException("No brokerServiceName was provided or discovered in the current application.");
                }
            }
            var brokerService = await _brokerServiceLocator.GetBrokerServiceForMessageAsync(messageType.Name, brokerServiceName);
            await brokerService.RegisterServiceSubscriberAsync(serviceReference, messageType.FullName);
        }

        /// <summary>
        /// Unregisters this Actor as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <returns></returns>
        public async Task UnregisterMessageTypeAsync(StatelessService service, Type messageType, bool flushQueue,
            Uri brokerServiceName = null)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            if (brokerServiceName == null)
            {
                brokerServiceName = await PublisherServiceHelper.DiscoverBrokerServiceNameAsync();
                if (brokerServiceName == null)
                {
                    throw new InvalidOperationException(
                        "No brokerServiceName was provided or discovered in the current application.");
                }
            }
            var brokerService =
                await _brokerServiceLocator.GetBrokerServiceForMessageAsync(messageType.Name, brokerServiceName);
            var serviceReference = CreateServiceReference(service);
            await brokerService.UnregisterServiceSubscriberAsync(serviceReference, messageType.FullName, flushQueue);
        }

        /// <summary>
        /// Registers this Actor as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <returns></returns>
        public async Task RegisterMessageTypeAsync(StatefulService service, Type messageType,
            Uri brokerServiceName = null, string listenerName = null)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            var serviceReference = CreateServiceReference(service, listenerName);
            await RegisterMessageTypeAsync(serviceReference, messageType, brokerServiceName);
        }

        /// <summary>
        /// Unregisters this Actor as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <returns></returns>
        public async Task UnregisterMessageTypeAsync(StatefulService service, Type messageType, bool flushQueue,
            Uri brokerServiceName = null)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            if (brokerServiceName == null)
            {
                brokerServiceName = await _brokerServiceLocator.LocateAsync();
                if (brokerServiceName == null)
                {
                    throw new InvalidOperationException(
                        "No brokerServiceName was provided or discovered in the current application.");
                }
            }
            var brokerService =
                await _brokerServiceLocator.GetBrokerServiceForMessageAsync(messageType.Name, brokerServiceName);
            var serviceReference = CreateServiceReference(service);
            await brokerService.UnregisterServiceSubscriberAsync(serviceReference, messageType.FullName, flushQueue);
        }

        /// <inheritdoc/>
        public async Task SubscribeAsync(ServiceReference serviceReference, IEnumerable<Type> messageTypes, Uri broker = null)
        {
            foreach (var messageType in messageTypes)
            {
                try
                {
                    await RegisterMessageTypeAsync(serviceReference, messageType, broker);
                    LogMessage($"Registered Service:'{serviceReference.ApplicationName}' as Subscriber of {messageType}.");
                }
                catch (Exception ex)
                {
                    LogMessage($"Failed to register Service:'{serviceReference.ApplicationName}' as Subscriber of {messageType}. Error:'{ex}'.");
                }
            }
        }

        /// <inheritdoc/>
        public Dictionary<Type, Func<object, Task>> DiscoverMessageHandlers(ISubscriberService service)
        {
            Dictionary<Type, Func<object, Task>> handlers = new Dictionary<Type, Func<object, Task>>();
            Type taskType = typeof(Task);
            var methods = service.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var method in methods)
            {
                var subscribeAttribute = method.GetCustomAttributes(typeof(SubscribeAttribute), false)
                    .Cast<SubscribeAttribute>()
                    .SingleOrDefault();

                if (subscribeAttribute == null) continue;

                var parameters = method.GetParameters();
                if (parameters.Length != 1 || !taskType.IsAssignableFrom(method.ReturnType)) continue;

                handlers[parameters[0].ParameterType] = m => (Task) method.Invoke(service, new[] {m});
            }

            return handlers;
        }

        /// <inheritdoc/>
        public Task ProccessMessageAsync(MessageWrapper messageWrapper, Dictionary<Type, Func<object, Task>> handlers)
        {
            var messageType = Assembly.Load(messageWrapper.Assembly).GetType(messageWrapper.MessageType, true);

            while (messageType != null)
            {
                if (handlers.TryGetValue(messageType, out var handler))
                {
                    return handler(messageWrapper.CreateMessage());
                }
                messageType = messageType.BaseType;
            }

            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public ServiceReference CreateServiceReference(StatelessService service, string listenerName = null)
        {
            return CreateServiceReference(service.Context, GetServicePartition(service).PartitionInfo, listenerName);
        }

        /// <inheritdoc/>
        public ServiceReference CreateServiceReference(StatefulService service, string listenerName = null)
        {
            return CreateServiceReference(service.Context, GetServicePartition(service).PartitionInfo, listenerName);
        }

        /// <summary>
        /// Gets the Partition info for the provided StatefulServiceBase instance.
        /// </summary>
        /// <param name="serviceBase"></param>
        /// <returns></returns>
        private IStatefulServicePartition GetServicePartition(StatefulServiceBase serviceBase)
        {
            if (serviceBase == null) throw new ArgumentNullException(nameof(serviceBase));
            return (IStatefulServicePartition)serviceBase
                .GetType()
                .GetProperty("Partition", BindingFlags.Instance | BindingFlags.NonPublic)?
                .GetValue(serviceBase) ?? throw new ArgumentNullException($"Unable to find partition information for service: {serviceBase}");
        }

        /// <summary>
        /// Gets the Partition info for the provided StatelessService instance.
        /// </summary>
        /// <param name="serviceBase"></param>
        /// <returns></returns>
        private static IStatelessServicePartition GetServicePartition(StatelessService serviceBase)
        {
            if (serviceBase == null) throw new ArgumentNullException(nameof(serviceBase));
            return (IStatelessServicePartition)serviceBase
                .GetType()
                .GetProperty("Partition", BindingFlags.Instance | BindingFlags.NonPublic)?
                .GetValue(serviceBase) ?? throw new ArgumentNullException($"Unable to find partition information for service: {serviceBase}");
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

        /// <summary>
        /// Outputs the provided message to the <see cref="_logCallback"/> if it's configured.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="caller"></param>
        protected void LogMessage(string message, [CallerMemberName] string caller = "unknown")
        {
            _logCallback?.Invoke($"{caller} - {message}");
        }
    }

    /// <summary>
    /// Marks a service method as being capable of receiving messages.
    /// Follows convention that method has signature 'Task MethodName(MessageType message)'
    /// Polymorphism is supported.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class SubscribeAttribute : Attribute
    {
    }
}