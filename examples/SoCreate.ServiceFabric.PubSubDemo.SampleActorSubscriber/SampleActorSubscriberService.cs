using System;
using System.Collections.Generic;
using System.Fabric;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Remoting.V2.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Actors.Remoting.V2.Runtime;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.Runtime;
using SoCreate.ServiceFabric.PubSubDemo.Common.Configuration;
using SoCreate.ServiceFabric.PubSubDemo.Common.Remoting;

namespace SoCreate.ServiceFabric.PubSubDemo.SampleActorSubscriber
{
    internal class SampleActorSubscriberService : ActorService
    {
        public SampleActorSubscriberService(StatefulServiceContext context, ActorTypeInformation actorTypeInfo, Func<ActorService, ActorId, ActorBase> actorFactory) : base(context, actorTypeInfo, actorFactory)
        {
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            if (FabricConfiguration.UseCustomServiceRemotingClientFactory)
            {
                return new[]
                {
                    new ServiceReplicaListener(context => new FabricTransportActorServiceRemotingListener(this, serializationProvider: new ServiceRemotingJsonSerializationProvider()))
                };
            }
            else
            {
                return base.CreateServiceReplicaListeners();
            }
        }
    }
}