using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.PubSubActors.Helpers;
using System;
using System.Fabric;
using ServiceFabric.PubSubActors.Interfaces;

namespace ServiceFabric.PubSubActors.SubscriberServices
{
    /// <summary>
    /// Factory for Stateful subscriber services, automatically registers subscriptions for messages.
    /// Use <see cref="SubscribeAttribute"/> to mark receiving methods.
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <example>
    ///   ServiceRuntime.RegisterServiceAsync("SubscriberServiceType",
    /// context =&gt; new StatelessSubscriberServiceBootstrapper&lt;SubscriberService&gt;(context,
    /// ctx =&gt; new SubscriberService(ctx)).Build())
    /// .GetAwaiter().GetResult();
    /// </example>
    public sealed class StatefulSubscriberServiceBootstrapper<TService>
        where TService : StatefulService, ISubscriberService
    {
        private readonly StatefulServiceContext _context;
        private readonly Func<StatefulServiceContext, TService> _serviceFactory;
        private readonly ISubscriberServiceHelper _subscriberServiceHelper;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="serviceFactory"></param>
        /// <param name="subscriberServiceHelper"></param>
        public StatefulSubscriberServiceBootstrapper(StatefulServiceContext context,
            Func<StatefulServiceContext, TService> serviceFactory,
            ISubscriberServiceHelper subscriberServiceHelper = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
            _subscriberServiceHelper = subscriberServiceHelper ?? new SubscriberServiceHelper();
        }

        /// <summary>
        /// Creates a new instance of <typeparamref name="TService"/> using the factory method.
        /// Registers all subscriptions.
        /// </summary>
        /// <returns></returns>
        public TService Build()
        {
            var service = _serviceFactory(_context);
            _subscriberServiceHelper.SubscribeAsync(_subscriberServiceHelper.CreateServiceReference(service), _subscriberServiceHelper.DiscoverMessageHandlers(service).Keys)
                .ConfigureAwait(false).GetAwaiter().GetResult();
            return service;
        }
    }

    /// <summary>
    /// Factory for Stateful subscriber services, automatically registers subscriptions for messages.
    /// Use <see cref="SubscribeAttribute"/> to mark receiving methods.
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <example>
    ///   ServiceRuntime.RegisterServiceAsync("SubscriberServiceType",
    /// context =&gt; new StatelessSubscriberServiceBootstrapper&lt;SubscriberService&gt;(context,
    /// ctx =&gt; new SubscriberService(ctx)).Build())
    /// .GetAwaiter().GetResult();
    /// </example>
    public sealed class StatelessSubscriberServiceBootstrapper<TService>
        where TService : StatelessService, ISubscriberService
    {
        private readonly StatelessServiceContext _context;
        private readonly Func<StatelessServiceContext, TService> _serviceFactory;
        private readonly ISubscriberServiceHelper _subscriberServiceHelper;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="serviceFactory"></param>
        /// <param name="subscriberServiceHelper"></param>
        public StatelessSubscriberServiceBootstrapper(StatelessServiceContext context,
            Func<StatelessServiceContext, TService> serviceFactory,
            ISubscriberServiceHelper subscriberServiceHelper = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
            _subscriberServiceHelper = subscriberServiceHelper ?? new SubscriberServiceHelper();
        }

        /// <summary>
        /// Creates a new instance of <typeparamref name="TService"/> using the factory method.
        /// Registers all subscriptions.
        /// </summary>
        /// <returns></returns>
        public TService Build()
        {
            var service = _serviceFactory(_context);
            // TODO: We have to create a ServiceReference manually because the Partition information doesn't exist yet.  This only works if the ServicePartitionKind is Singleton.
            var serviceReference = new ServiceReference
            {
                ApplicationName = _context.CodePackageActivationContext.ApplicationName,
                PartitionKind = ServicePartitionKind.Singleton,
                ServiceUri = _context.ServiceName,
                PartitionGuid = _context.PartitionId,
                ListenerName = null
            };
            _subscriberServiceHelper.SubscribeAsync(serviceReference, _subscriberServiceHelper.DiscoverMessageHandlers(service).Keys)
                .ConfigureAwait(false).GetAwaiter().GetResult();
            return service;
        }
    }
}
