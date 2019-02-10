using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.PubSubActors.Helpers;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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
    public sealed class StatefulSubscriberServiceBootstrapper<TService> : SubscriberServiceBootstrapper
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
            SubscribeStateful(_subscriberServiceHelper, service).ConfigureAwait(false).GetAwaiter().GetResult();
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
    public sealed class StatelessSubscriberServiceBootstrapper<TService> : SubscriberServiceBootstrapper
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
            SubscribeStateless(_subscriberServiceHelper, service).ConfigureAwait(false).GetAwaiter().GetResult();
            return service;
        }
    }

    /// <summary>
    /// Shared base for subscriber service bootstrappers.
    /// </summary>
    public abstract class SubscriberServiceBootstrapper
    {
        /// <summary>
        /// Discovers all annotated methods.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
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
                        Handler = m => (Task) method.Invoke(this, new[] {m})
                    };
                }
            }
        }

        /// <summary>
        /// Creates subscriptions for all annotated receive methods.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="helper"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public async Task SubscribeStateless<TService>(ISubscriberServiceHelper helper, TService service)
            where TService : StatelessService, ISubscriberService
        {
            var subscriptions = DiscoverSubscribers(service.GetType());

            foreach (var subscription in subscriptions)
            {
                await helper.RegisterMessageTypeAsync(service, subscription.MessageType);
            }
        }

        /// <summary>
        /// Creates subscriptions for all annotated receive methods.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="helper"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public async Task SubscribeStateful<TService>(ISubscriberServiceHelper helper, TService service)
            where TService : StatefulService, ISubscriberService
        {
            var subscriptions = DiscoverSubscribers(service.GetType());

            foreach (var subscription in subscriptions)
            {
                await helper.RegisterMessageTypeAsync(service, subscription.MessageType);
            }
        }
    }
}
