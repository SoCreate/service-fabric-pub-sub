using Microsoft.ServiceFabric.Actors;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace SubscribingActor
{
	[EventSource(Name = "MyCompany-MyServiceFabricApp-SubscribingActor")]
	internal sealed class ActorEventSource : EventSource
	{
		public static ActorEventSource Current = new ActorEventSource();

		[NonEvent]
		public void Message(string message, params object[] args)
		{
			if (this.IsEnabled())
			{
				string finalMessage = string.Format(message, args);
				this.Message(finalMessage);
			}
		}

		[Event(1, Level = EventLevel.Informational, Message = "{0}")]
		public void Message(string message)
		{
			if (this.IsEnabled())
			{
				this.WriteEvent(1, message);
			}
		}

		[NonEvent]
		public void ActorMessage(ActorBase actor, string message, params object[] args)
		{
			if (this.IsEnabled())
			{
				string finalMessage = string.Format(message, args);
				this.ActorMessage(
					actor.GetType().ToString(),
					actor.Id.ToString(),
					actor.ActorService.Context.CodePackageActivationContext.ApplicationTypeName,
					actor.ActorService.Context.CodePackageActivationContext.ApplicationName,
					actor.ActorService.Context.ServiceTypeName,
					actor.ActorService.Context.ServiceName.ToString(),
					actor.ActorService.Context.PartitionId,
					actor.ActorService.Context.ReplicaId,
					FabricRuntime.GetNodeContext().NodeName,
					finalMessage);
			}
		}

		[NonEvent]
		public void ActorHostInitializationFailed(Exception e)
		{
			if (this.IsEnabled())
			{
				this.ActorHostInitializationFailed(e.ToString());
			}
		}

		[Event(2, Level = EventLevel.Informational, Message = "{9}")]
		private void ActorMessage(
			string actorType,
			string actorId,
			string applicationTypeName,
			string applicationName,
			string serviceTypeName,
			string serviceName,
			Guid partitionId,
			long replicaOrInstanceId,
			string nodeName,
			string message)
		{
			this.WriteEvent(
				2,
				actorType,
				actorId,
				applicationTypeName,
				applicationName,
				serviceTypeName,
				serviceName,
				partitionId,
				replicaOrInstanceId,
				nodeName,
				message);
		}

		[Event(3, Level = EventLevel.Error, Message = "Actor host initialization failed")]
		private void ActorHostInitializationFailed(string exception)
		{
			this.WriteEvent(3, exception);
		}
	}
}
