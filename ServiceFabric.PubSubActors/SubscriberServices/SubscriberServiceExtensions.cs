using System;
using System.Fabric;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Services.Runtime;
using Newtonsoft.Json;
using ServiceFabric.PubSubActors.Interfaces;

namespace ServiceFabric.PubSubActors.SubscriberServices
{
	public static class SubscriberServiceExtensions
	{
		/// <summary>
		/// Deserializes the provided <paramref name="message"/> Payload into an intance of type <typeparam name="TResult"></typeparam>
		/// </summary>
		/// <typeparam name="TResult"></typeparam>
		/// <returns></returns>
		public static TResult Deserialize<TResult>(this StatefulServiceBase service, MessageWrapper message)
		{
			if (message == null) throw new ArgumentNullException(nameof(message));
			if (string.IsNullOrWhiteSpace(message.Payload)) throw new ArgumentNullException(nameof(message.Payload));

			var payload = JsonConvert.DeserializeObject<TResult>(message.Payload);
			return payload;
		}

		/// <summary>
		/// Deserializes the provided <paramref name="message"/> Payload into an intance of type <typeparam name="TResult"></typeparam>
		/// </summary>
		/// <typeparam name="TResult"></typeparam>
		/// <returns></returns>
		public static TResult Deserialize<TResult>(this StatelessService service, MessageWrapper message)
		{
			if (message == null) throw new ArgumentNullException(nameof(message));
			if (string.IsNullOrWhiteSpace(message.Payload)) throw new ArgumentNullException(nameof(message.Payload));

			var payload = JsonConvert.DeserializeObject<TResult>(message.Payload);
			return payload;
		}

		/// <summary>
		/// Registers a service as a subscriber for messages of type <paramref name="messageType"/>.
		/// </summary>
		/// <param name="service">The service registering itself as a subscriber for messages of type <paramref name="messageType"/></param>
		/// <param name="messageType">The type of message to register for (each message type has its own <see cref="IBrokerActor"/> instance)</param>
		/// <returns></returns>
		public static Task RegisterMessageTypeAsync(this StatefulServiceBase service, Type messageType)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (messageType == null) throw new ArgumentNullException(nameof(messageType));
			return RegisterMessageTypeAsync(service.Context, service.GetServicePartition().PartitionInfo, messageType);
		}

		/// <summary>
		/// Registers a service as a subscriber for messages of type <paramref name="messageType"/>.
		/// </summary>
		/// <param name="service">The service registering itself as a subscriber for messages of type <paramref name="messageType"/></param>
		/// <param name="messageType">The type of message to register for (each message type has its own <see cref="IBrokerActor"/> instance)</param>
		/// <returns></returns>
		public static Task RegisterMessageTypeAsync(this StatelessService service, Type messageType)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (messageType == null) throw new ArgumentNullException(nameof(messageType));
			return RegisterMessageTypeAsync(service.Context, service.GetServicePartition().PartitionInfo, messageType);
		}

		/// <summary>
		/// Unregisters a service as a subscriber for messages of type <paramref name="messageType"/>.
		/// </summary>
		/// <param name="service">The service unregistering itself as a subscriber for messages of type <paramref name="messageType"/></param>
		/// <param name="messageType">The type of message to unregister for (each message type has its own <see cref="IBrokerActor"/> instance)</param>
		/// <param name="flushQueue">Publish any remaining messages.</param>
		/// <returns></returns>
		public static Task UnregisterMessageTypeAsync(this StatelessService service, Type messageType, bool flushQueue)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (messageType == null) throw new ArgumentNullException(nameof(messageType));
			return UnregisterMessageTypeAsync(service.Context, service.GetServicePartition().PartitionInfo, messageType, flushQueue);
		}

		/// <summary>
		/// Unregisters a service as a subscriber for messages of type <paramref name="messageType"/>.
		/// </summary>
		/// <param name="service">The service unregistering itself as a subscriber for messages of type <paramref name="messageType"/></param>
		/// <param name="messageType">The type of message to unregister for (each message type has its own <see cref="IBrokerActor"/> instance)</param>
		/// <param name="flushQueue">Publish any remaining messages.</param>
		/// <returns></returns>
		public static Task UnregisterMessageTypeAsync(this StatefulServiceBase service, Type messageType, bool flushQueue)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (messageType == null) throw new ArgumentNullException(nameof(messageType));
			return UnregisterMessageTypeAsync(service.Context, service.GetServicePartition().PartitionInfo, messageType, flushQueue);
		}

		/// <summary>
		/// Registers a Service as a subscriber for messages of type <paramref name="messageType"/> using a <see cref="IRelayBrokerActor"/> approach.   
		/// The relay actor will register itself as subscriber to the broker actor, creating a fan out pattern for scalability.
		/// </summary>
		/// <param name="service">The service registering itself as a subscriber for messages of type <paramref name="messageType"/></param>
		/// <param name="messageType">The type of message to register for (each message type has its own <see cref="IBrokerActor"/> instance)</param>
		/// <param name="relayBrokerActorId">The ID of the relay broker to register with. Remember this ID in the caller, if you ever need to unregister.</param>
		/// <param name="sourceBrokerActorId">(optional) The ID of the source <see cref="IBrokerActor"/> to use as the source for the <paramref name="relayBrokerActorId"/> 
		/// Remember this ID in the caller, if you ever need to unregister.
		/// If not specified, the default <see cref="IBrokerActor"/> for the message type <paramref name="messageType"/> will be used.</param>
		/// <returns></returns>
		public static Task RegisterMessageTypeWithRelayBrokerAsync(this StatefulServiceBase service, Type messageType, ActorId relayBrokerActorId, ActorId sourceBrokerActorId)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (messageType == null) throw new ArgumentNullException(nameof(messageType));
			if (relayBrokerActorId == null) throw new ArgumentNullException(nameof(relayBrokerActorId));

			return RegisterMessageTypeWithRelayBrokerAsync(service.Context, service.GetServicePartition().PartitionInfo, messageType, relayBrokerActorId, sourceBrokerActorId);
		}

		/// <summary>
		/// Registers a Service as a subscriber for messages of type <paramref name="messageType"/> using a <see cref="IRelayBrokerActor"/> approach.   
		/// The relay actor will register itself as subscriber to the broker actor, creating a fan out pattern for scalability.
		/// </summary>
		/// <param name="service">The service registering itself as a subscriber for messages of type <paramref name="messageType"/></param>
		/// <param name="messageType">The type of message to register for (each message type has its own <see cref="IBrokerActor"/> instance)</param>
		/// <param name="relayBrokerActorId">The ID of the relay broker to register with. Remember this ID in the caller, if you ever need to unregister.</param>
		/// <param name="sourceBrokerActorId">(optional) The ID of the source <see cref="IBrokerActor"/> to use as the source for the <paramref name="relayBrokerActorId"/> 
		/// Remember this ID in the caller, if you ever need to unregister.
		/// If not specified, the default <see cref="IBrokerActor"/> for the message type <paramref name="messageType"/> will be used.</param>
		/// <returns></returns>
		public static Task RegisterMessageTypeWithRelayBrokerAsync(this StatelessService service, Type messageType, ActorId relayBrokerActorId, ActorId sourceBrokerActorId)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (messageType == null) throw new ArgumentNullException(nameof(messageType));
			if (relayBrokerActorId == null) throw new ArgumentNullException(nameof(relayBrokerActorId));

			return RegisterMessageTypeWithRelayBrokerAsync(service.Context, service.GetServicePartition().PartitionInfo, messageType, relayBrokerActorId, sourceBrokerActorId);
		}

		/// <summary>
		/// Unregisters a Service as a subscriber for messages of type <paramref name="messageType"/> using a <see cref="IRelayBrokerActor"/> approach.   
		/// </summary>
		/// <param name="service">The service registering itself as a subscriber for messages of type <paramref name="messageType"/></param>
		/// <param name="messageType">The type of message to unregister for (each message type has its own <see cref="IBrokerActor"/> instance)</param>
		/// <param name="relayBrokerActorId">The ID of the relay broker to unregister with.</param>
		/// <param name="sourceBrokerActorId">(optional) The ID of the source <see cref="IBrokerActor"/> that was used as the source for the <paramref name="relayBrokerActorId"/>
		/// If not specified, the default <see cref="IBrokerActor"/> for the message type <paramref name="messageType"/> will be used.</param>
		/// <param name="flushQueue">Publish any remaining messages.</param>
		/// <returns></returns>
		public static Task UnregisterMessageTypeWithRelayBrokerAsync(this StatefulServiceBase service, Type messageType, ActorId relayBrokerActorId, ActorId sourceBrokerActorId, bool flushQueue)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (messageType == null) throw new ArgumentNullException(nameof(messageType));
			if (relayBrokerActorId == null) throw new ArgumentNullException(nameof(relayBrokerActorId));

			return UnregisterMessageTypeWithRelayBrokerAsync(service.Context, service.GetServicePartition().PartitionInfo, messageType, relayBrokerActorId, sourceBrokerActorId, flushQueue);
		}

		/// <summary>
		/// Unregisters a Service as a subscriber for messages of type <paramref name="messageType"/> using a <see cref="IRelayBrokerActor"/> approach.   
		/// </summary>
		/// <param name="service">The service registering itself as a subscriber for messages of type <paramref name="messageType"/></param>
		/// <param name="messageType">The type of message to unregister for (each message type has its own <see cref="IBrokerActor"/> instance)</param>
		/// <param name="relayBrokerActorId">The ID of the relay broker to unregister with.</param>
		/// <param name="sourceBrokerActorId">(optional) The ID of the source <see cref="IBrokerActor"/> that was used as the source for the <paramref name="relayBrokerActorId"/>
		/// If not specified, the default <see cref="IBrokerActor"/> for the message type <paramref name="messageType"/> will be used.</param>
		/// <param name="flushQueue">Publish any remaining messages.</param>
		/// <returns></returns>
		public static Task UnregisterMessageTypeWithRelayBrokerAsync(this StatelessService service, Type messageType, ActorId relayBrokerActorId, ActorId sourceBrokerActorId, bool flushQueue)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (messageType == null) throw new ArgumentNullException(nameof(messageType));
			if (relayBrokerActorId == null) throw new ArgumentNullException(nameof(relayBrokerActorId));

			return UnregisterMessageTypeWithRelayBrokerAsync(service.Context, service.GetServicePartition().PartitionInfo, messageType, relayBrokerActorId, sourceBrokerActorId, flushQueue);
		}

		

		/// <summary>
		/// Gets the Partition info for the provided StatefulServiceBase instance.
		/// </summary>
		/// <param name="serviceBase"></param>
		/// <returns></returns>
		private static IStatefulServicePartition GetServicePartition(this StatefulServiceBase serviceBase)
		{
			if (serviceBase == null) throw new ArgumentNullException(nameof(serviceBase));
			return (IStatefulServicePartition)serviceBase
				.GetType()
				.GetProperty("Partition", BindingFlags.Instance | BindingFlags.NonPublic)
				.GetValue(serviceBase);
		}

		/// <summary>
		/// Gets the Partition info for the provided StatelessService instance.
		/// </summary>
		/// <param name="serviceBase"></param>
		/// <returns></returns>
		private static IStatelessServicePartition GetServicePartition(this StatelessService serviceBase)
		{
			if (serviceBase == null) throw new ArgumentNullException(nameof(serviceBase));
			return (IStatelessServicePartition)serviceBase
				.GetType()
				.GetProperty("Partition", BindingFlags.Instance | BindingFlags.NonPublic)
				.GetValue(serviceBase);
		}
		
		/// <summary>
		/// Creates a <see cref="ServiceReference"/> for the provided service context and partition info.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="info"></param>
		/// <returns></returns>
		private static ServiceReference CreateServiceReference(ServiceContext context, ServicePartitionInformation info)
		{
			var serviceReference = new ServiceReference
			{
				ApplicationName = context.CodePackageActivationContext.ApplicationName,
				PartitionKind = info.Kind,
				ServiceUri = context.ServiceName,
				PartitionGuid = context.PartitionId,
			};

			var longInfo = info as Int64RangePartitionInformation;
			
			if (longInfo != null)
			{
				serviceReference.PartitionID = longInfo.LowKey;
			}
			else
			{
				var stringInfo = info as NamedPartitionInformation;
				if (stringInfo != null)
				{
					serviceReference.PartitionName = stringInfo.Name;
				}
			}
			return serviceReference;
		}

		/// <summary>
		/// Registers a service as a subscriber for messages of type <paramref name="messageType"/>.
		/// </summary>
		/// <returns></returns>
		private static async Task RegisterMessageTypeAsync(ServiceContext context, ServicePartitionInformation info, Type messageType)
		{
			var serviceReference = CreateServiceReference(context, info);
			ActorId actorId = new ActorId(messageType.FullName);
			IBrokerActor brokerActor = ActorProxy.Create<IBrokerActor>(actorId, serviceReference.ApplicationName, nameof(IBrokerActor));

			await brokerActor.RegisterServiceSubscriberAsync(serviceReference);
		}

		/// <summary>
		/// Unregisters a service as a subscriber for messages of type <paramref name="messageType"/>.
		/// </summary>
		/// <returns></returns>
		private static async Task UnregisterMessageTypeAsync(ServiceContext context, ServicePartitionInformation info, Type messageType, bool flushQueue)
		{
			var serviceReference = CreateServiceReference(context, info);
			ActorId actorId = new ActorId(messageType.FullName);
			IBrokerActor brokerActor = ActorProxy.Create<IBrokerActor>(actorId, serviceReference.ApplicationName, nameof(IBrokerActor));

			await brokerActor.UnregisterServiceSubscriberAsync(serviceReference, flushQueue);
		}

		/// <summary>
		/// Registers a service as a subscriber for messages of type <paramref name="messageType"/> using a relay broker.
		/// </summary>
		/// <returns></returns>
		private static async Task RegisterMessageTypeWithRelayBrokerAsync(ServiceContext context, ServicePartitionInformation info, Type messageType, ActorId relayBrokerActorId, ActorId sourceBrokerActorId)
		{
			var serviceReference = CreateServiceReference(context, info);
		
			if (sourceBrokerActorId == null)
			{
				sourceBrokerActorId = new ActorId(messageType.FullName);
			}
			IRelayBrokerActor relayBrokerActor = ActorProxy.Create<IRelayBrokerActor>(relayBrokerActorId, serviceReference.ApplicationName, nameof(IRelayBrokerActor));
			IBrokerActor brokerActor = ActorProxy.Create<IBrokerActor>(sourceBrokerActorId, serviceReference.ApplicationName, nameof(IBrokerActor));

			//register relay as subscriber for broker
			await brokerActor.RegisterSubscriberAsync(ActorReference.Get(relayBrokerActor));
			//register caller as subscriber for relay broker
			await relayBrokerActor.RegisterServiceSubscriberAsync(serviceReference);
		}

		/// <summary>
		/// Unregisters a service as a subscriber for messages of type <paramref name="messageType"/> using a relay broker.
		/// </summary>
		/// <returns></returns>
		private static async Task UnregisterMessageTypeWithRelayBrokerAsync(ServiceContext context, ServicePartitionInformation info, Type messageType, ActorId relayBrokerActorId, ActorId sourceBrokerActorId, bool flushQueue)
		{
			var serviceReference = CreateServiceReference(context, info);

			if (sourceBrokerActorId == null)
			{
				sourceBrokerActorId = new ActorId(messageType.FullName);
			}
			IRelayBrokerActor relayBrokerActor = ActorProxy.Create<IRelayBrokerActor>(relayBrokerActorId, serviceReference.ApplicationName, nameof(IRelayBrokerActor));
			IBrokerActor brokerActor = ActorProxy.Create<IBrokerActor>(sourceBrokerActorId, serviceReference.ApplicationName, nameof(IBrokerActor));

			//register relay as subscriber for broker
			await brokerActor.UnregisterSubscriberAsync(ActorReference.Get(relayBrokerActor), flushQueue);
			//register caller as subscriber for relay broker
			await relayBrokerActor.UnregisterServiceSubscriberAsync(serviceReference, flushQueue);
		}
	}
}
