using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.PubSubActors.Helpers;
using System;
using System.Fabric;
using System.Fabric.Description;
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
    public sealed class StatefulSubscriberServiceBootstrapper<TService>
        where TService : StatefulService, ISubscriberService
    {
        private readonly StatefulServiceContext _context;
        private readonly Func<StatefulServiceContext, TService> _serviceFactory;
        private readonly Action<string> _loggingCallback;
        private readonly ISubscriberServiceHelper _subscriberServiceHelper;
        private readonly FabricClient _fabricClient;
        private long _filterId;
        private TService _service;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="serviceFactory"></param>
        /// <param name="subscriberServiceHelper"></param>
        /// <param name="loggingCallback">Optional logging callback.</param>
        public StatefulSubscriberServiceBootstrapper(StatefulServiceContext context,
            Func<StatefulServiceContext, TService> serviceFactory,
            ISubscriberServiceHelper subscriberServiceHelper = null,
            Action<string> loggingCallback = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
            _loggingCallback = loggingCallback;
            _subscriberServiceHelper = subscriberServiceHelper ?? new SubscriberServiceHelper();
            _fabricClient = new FabricClient(FabricClientRole.User);
            _fabricClient.ServiceManager.ServiceNotificationFilterMatched += ServiceNotificationFilterMatched;
        }

        /// <summary>
        /// Creates a new instance of <typeparamref name="TService"/> using the factory method.
        /// Registers all subscriptions.
        /// </summary>
        /// <returns></returns>
        public TService Build()
        {
            _service = _serviceFactory(_context);
            Task.Run(async () =>
            {
                _loggingCallback?.Invoke($"Registering for notifications about service '{_context.ServiceName}'.");
                try
                {
                    var filter = new ServiceNotificationFilterDescription
                    {
                        Name = _context.ServiceName,
                        MatchPrimaryChangeOnly = true
                    };
                    _filterId = await _fabricClient.ServiceManager.RegisterServiceNotificationFilterAsync(filter);

                    _loggingCallback?.Invoke(
                        $"Succesfully registered for notifications about service '{_context.ServiceName}'.");
                }
                catch (Exception ex)
                {
                    _loggingCallback?.Invoke(
                        $"Failed to register for notifications about service '{_context.ServiceName}'. Error: {ex}");
                }
            });
            return _service;
        }

        /// <summary>
        /// Called when the created service has a change in its endpoints.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ServiceNotificationFilterMatched(object sender, EventArgs e)
        {
            var args = (FabricClient.ServiceManagementClient.ServiceNotificationEventArgs)e;

            if (args.Notification.Endpoints.Count == 0)
            {
                //service deleted
                await UnregisterSubscriptions()
                    .ConfigureAwait(false);
            }
            else
            {
                //service created or moved
                await RegisterSubscriptions()
                    .ConfigureAwait(false);
            }
        }

        private async Task RegisterSubscriptions()
        {
            _loggingCallback?.Invoke($"Registering subscriptions for service '{_context.ServiceName}'.");
            try
            {
                await _subscriberServiceHelper.SubscribeAsync(
                        _subscriberServiceHelper.CreateServiceReference(_service),
                        _subscriberServiceHelper.DiscoverMessageHandlers(_service).Keys)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _loggingCallback?.Invoke(
                    $"Failed to register for notifications about service '{_context.ServiceName}'. Error: {ex}");
            }
        }

        private async Task UnregisterSubscriptions()
        {
            _loggingCallback?.Invoke($"Unregistering subscriptions for deleted service '{_context.ServiceName}'.");
            await _fabricClient.ServiceManager.UnregisterServiceNotificationFilterAsync(_filterId)
                .ConfigureAwait(false);
            try
            {
                await _subscriberServiceHelper.SubscribeAsync(
                        _subscriberServiceHelper.CreateServiceReference(_service),
                        _subscriberServiceHelper.DiscoverMessageHandlers(_service).Keys)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _loggingCallback?.Invoke(
                    $"Failed to register for notifications about service '{_context.ServiceName}'. Error: {ex}");
            }
        }

    }
}
