using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Common.DataContracts;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using ServiceFabric.PubSubActors.PublisherServices;
using System.Fabric;
using ServiceFabric.PubSubActors.Helpers;

namespace PublishingStatelessService
{
	/// <summary>
	/// The FabricRuntime creates an instance of this class for each service type instance. 
	/// </summary>
	internal sealed class PublishingStatelessService : StatelessService, IPublishingStatelessService
	{
	    private readonly IPublisherServiceHelper _publisherServiceHelper;

		public PublishingStatelessService(StatelessServiceContext serviceContext) : base(serviceContext)
		{
            _publisherServiceHelper = new PublisherServiceHelper(new BrokerServiceLocator());
		}

		/// <summary>
		/// Optional override to create listeners (like tcp, http) for this service instance.
		/// </summary>
		/// <returns>The collection of listeners.</returns>
		protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            yield return new ServiceInstanceListener(context => new FabricTransportServiceRemotingListener(context, this), "StatelessFabricTransportServiceRemotingListener");
		}

    /// <summary>
    /// This is the main entry point for your service instance.
    /// </summary>
    /// <param name="cancelServiceInstance">Canceled when Service Fabric terminates this instance.</param>
    protected override async Task RunAsync(CancellationToken cancelServiceInstance)
		{
			// TODO: Replace the following sample code with your own logic.

			int iterations = 0;
			// This service instance continues processing until the instance is terminated.
			while (!cancelServiceInstance.IsCancellationRequested)
			{

				// Log what the service is doing
				ServiceEventSource.Current.ServiceMessage(this, "Working-{0}", iterations++);

				// Pause for 1 second before continue processing.
				await Task.Delay(TimeSpan.FromSeconds(1), cancelServiceInstance);
			}
		}

		async Task<string> IPublishingStatelessService.PublishMessageOneAsync()
		{
			ServiceEventSource.Current.ServiceMessage(this, "Publishing Message");
			await this.PublishMessageAsync(new PublishedMessageOne { Content = "Hello PubSub World, from Service, using Broker Actor!!" });
			return "Message published to broker actor";
		}

        async Task<string> IPublishingStatelessService.PublishMessageTwoAsync()
        {
            ServiceEventSource.Current.ServiceMessage(this, "Publishing Message");

            await _publisherServiceHelper.PublishMessageAsync(this, new PublishedMessageTwo { Content = "Hello PubSub World, from Service, using Broker Service!" });

            return "Message published to broker service";
        }
    }

	
}
