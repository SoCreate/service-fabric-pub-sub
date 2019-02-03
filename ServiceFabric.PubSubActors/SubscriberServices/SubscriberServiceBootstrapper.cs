using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.PubSubActors.Helpers;
using ServiceFabric.PubSubActors.State;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ServiceFabric.PubSubActors.SubscriberServices
{
    public class SubscriberServiceBootstrapper : ISubscriberServiceBootstrapper, IDisposable
    {
        private readonly IServiceProxyFactory _serviceProxyFactory;
        private long _filterId;
        private FabricClient _fabricClient;
        private bool _isInitialized;

        public SubscriberServiceBootstrapper(IServiceProxyFactory serviceProxyFactory)
        {
            _serviceProxyFactory = serviceProxyFactory ?? throw new ArgumentNullException(nameof(serviceProxyFactory));
        }

        public async Task Initialize()
        {
            if (_isInitialized) return;

            _fabricClient = new FabricClient(FabricClientRole.User);
            _fabricClient.ServiceManager.ServiceNotificationFilterMatched += ServiceNotificationFilterMatched;
            var filter = new ServiceNotificationFilterDescription
            {
                Name = new Uri("fabric:"),
                MatchPrimaryChangeOnly = true
            };
            _filterId = await _fabricClient.ServiceManager.RegisterServiceNotificationFilterAsync(filter);
            _isInitialized = true;
        }

        public Task Uninitialize()
        {
            if (!_isInitialized) return Task.FromResult(true);

            return _fabricClient.ServiceManager.UnregisterServiceNotificationFilterAsync(_filterId);
        }

        private async void ServiceNotificationFilterMatched(object sender, EventArgs e)
        {
            var args = (FabricClient.ServiceManagementClient.ServiceNotificationEventArgs)e;

            if (args.Notification.Endpoints.Count == 0)
            {
                //service deleted

            }
            else
            {
                //service created or moved
                var serviceName = args.Notification.ServiceName;
                var applicationName = new Uri(serviceName.Host);
                var service = await _fabricClient.ServiceManager.GetServiceDescriptionAsync(serviceName);
                ServicePartitionKey partitionKey;
                switch (args.Notification.PartitionInfo.Kind)
                {
                    case ServicePartitionKind.Singleton:
                        partitionKey = ServicePartitionKey.Singleton;
                        break;
                    case ServicePartitionKind.Int64Range:
                        partitionKey = new ServicePartitionKey(args.Notification.PartitionInfo.AsLongPartitionInfo().LowKey);
                        break;
                    case ServicePartitionKind.Named:
                        partitionKey = new ServicePartitionKey(args.Notification.PartitionInfo.AsNamedPartitionInfo().Name);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var proxy = _serviceProxyFactory.CreateServiceProxy<ISubscriberService>(serviceName, partitionKey);

            }
        }

        public IEnumerable<SubscriptionDefinition> DiscoverSubscribers(Type serviceType)
        {
            Type taskType = typeof(Task);
            var methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var method in methods)
            {
                var handlesAttribute = method.GetCustomAttributes(typeof(SubscribeAttribute), false)
                    .Cast<SubscribeAttribute>()
                    .SingleOrDefault();

                if (handlesAttribute == null) continue;
                var parameters = method.GetParameters();
                if (parameters.Length != 1) continue;
                if (!taskType.IsAssignableFrom(method.ReturnType)) continue;

                //exact match
                //or overload
                if (parameters[0].ParameterType == handlesAttribute.MessageType
                    || handlesAttribute.MessageType.IsAssignableFrom(parameters[0].ParameterType))
                {
                    yield return new SubscriptionDefinition
                    {
                        MessageType = handlesAttribute.MessageType,
                        Handler = m => (Task)method.Invoke(this, new[] { m })
                    };
                }
            }
        }

        public async Task Subscribe<TService>(ISubscriberServiceHelper helper, TService service)
            where TService : StatelessService, ISubscriberService
        {
            var subscriptions = DiscoverSubscribers(service.GetType());

            foreach (var subscription in subscriptions)
            {
                await helper.RegisterMessageTypeAsync(service, subscription.MessageType);

            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public interface ISubscriberServiceBootstrapper
    {
        IEnumerable<SubscriptionDefinition> DiscoverSubscribers(Type serviceType);

    }
}