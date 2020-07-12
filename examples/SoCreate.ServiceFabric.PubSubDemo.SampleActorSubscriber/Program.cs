using System;
using System.Threading;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using SoCreate.ServiceFabric.PubSubDemo.Common.Configuration;
using SoCreate.ServiceFabric.PubSubDemo.SampleActorSubscriber.Interfaces;

namespace SoCreate.ServiceFabric.PubSubDemo.SampleActorSubscriber
{
    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                // This line registers an Actor Service to host your actor class with the Service Fabric runtime.
                // The contents of your ServiceManifest.xml and ApplicationManifest.xml files
                // are automatically populated when you build this project.
                // For more information, see https://aka.ms/servicefabricactorsplatform

                ActorRuntime.RegisterActorAsync<SampleActorSubscriber>(
                   (context, actorType) => new SampleActorSubscriberService(
                       context,
                       actorType,
                       (actorService, actorId) => new SampleActorSubscriber(actorService, actorId))).GetAwaiter().GetResult();

                ISampleActorSubscriber actorSubscriber;

                if (FabricConfiguration.UseCustomServiceRemotingClientFactory)
                {
                    actorSubscriber = FabricConfiguration.GetProxyFactories().CreateActorProxy<ISampleActorSubscriber>(new Uri("fabric:/PubSubDemo/SampleActorSubscriberService"), ActorId.CreateRandom());
                }
                else
                {
                    actorSubscriber = ActorProxy.Create<ISampleActorSubscriber>(ActorId.CreateRandom(), new Uri("fabric:/PubSubDemo/SampleActorSubscriberService"));
                }

                actorSubscriber.Subscribe().GetAwaiter().GetResult();

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ActorEventSource.Current.ActorHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}