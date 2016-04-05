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
	public static class ServiceExtensions
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
		/// <returns></returns>
		public static Task RegisterMessageTypeAsync(this StatefulServiceBase service, Type messageType)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			return RegisterMessageTypeAsync(service.Context, service.GetServicePartition().PartitionInfo, messageType);
		}

		/// <summary>
		/// Registers a service as a subscriber for messages of type <paramref name="messageType"/>.
		/// </summary>
		/// <returns></returns>
		public static Task RegisterMessageTypeAsync(this StatelessService service, Type messageType)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			return RegisterMessageTypeAsync(service.Context, service.GetServicePartition().PartitionInfo, messageType);
		}

		/// <summary>
		/// Unregisters a service as a subscriber for messages of type <paramref name="messageType"/>.
		/// </summary>
		/// <returns></returns>
		public static Task UnregisterMessageTypeAsync(this StatelessService service, Type messageType, bool flushQueue)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			return UnregisterMessageTypeAsync(service.Context, service.GetServicePartition().PartitionInfo, messageType, flushQueue);
		}

		/// <summary>
		/// Unregisters a service as a subscriber for messages of type <paramref name="messageType"/>.
		/// </summary>
		/// <returns></returns>
		public static Task UnregisterMessageTypeAsync(this StatefulServiceBase service, Type messageType, bool flushQueue)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			return UnregisterMessageTypeAsync(service.Context, service.GetServicePartition().PartitionInfo, messageType, flushQueue);
		}

		/// <summary>
		/// Gets the Partition info for the provided StatefulServiceBase instance.
		/// </summary>
		/// <param name="serviceBase"></param>
		/// <returns></returns>
		public static IStatefulServicePartition GetServicePartition(this StatefulServiceBase serviceBase)
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
		public static IStatelessServicePartition GetServicePartition(this StatelessService serviceBase)
		{
			if (serviceBase == null) throw new ArgumentNullException(nameof(serviceBase));
			return (IStatelessServicePartition)serviceBase
				.GetType()
				.GetProperty("Partition", BindingFlags.Instance | BindingFlags.NonPublic)
				.GetValue(serviceBase);
		}

		/// <summary>
		/// Registers a service as a subscriber for messages of type <paramref name="messageType"/>.
		/// </summary>
		/// <returns></returns>
		internal static async Task RegisterMessageTypeAsync(this ServiceReference serviceReference, Type messageType)
		{
			ActorId actorId = new ActorId(messageType.FullName);
			IBrokerActor brokerActor = ActorProxy.Create<IBrokerActor>(actorId, serviceReference.ApplicationName, nameof(IBrokerActor));

			await brokerActor.RegisterServiceSubscriberAsync(serviceReference);
		}

		/// <summary>
		/// Unregisters a service as a subscriber for messages of type <paramref name="messageType"/>.
		/// </summary>
		/// <returns></returns>
		internal static async Task UnregisterMessageTypeAsync(this ServiceReference serviceReference, Type messageType, bool flushQueue)
		{
			ActorId actorId = new ActorId(messageType.FullName);
			IBrokerActor brokerActor = ActorProxy.Create<IBrokerActor>(actorId, serviceReference.ApplicationName, nameof(IBrokerActor));

			await brokerActor.UnregisterServiceSubscriberAsync(serviceReference, flushQueue);
		}

		/// <summary>
		/// Creates a <see cref="ServiceReference"/> for the provided service context and partition info.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="info"></param>
		/// <returns></returns>
		internal static ServiceReference CreateServiceReference(ServiceContext context, ServicePartitionInformation info)
		{
			var serviceReference = new ServiceReference
			{
				ApplicationName = context.CodePackageActivationContext.ApplicationName,
				PartitionKind = info.Kind,
				ServiceUri = context.ServiceName,
				PartitionGuid = context.PartitionId,
			};

			var longInfo = info as Int64RangePartitionInformation;
			
			//unsure why this is lowkey
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
		private static Task RegisterMessageTypeAsync(ServiceContext context, ServicePartitionInformation info, Type messageType)
		{
			var serviceReference = CreateServiceReference(context, info);
			return RegisterMessageTypeAsync(serviceReference, messageType);
		}

		/// <summary>
		/// Unregisters a service as a subscriber for messages of type <paramref name="messageType"/>.
		/// </summary>
		/// <returns></returns>
		private static Task UnregisterMessageTypeAsync(ServiceContext context, ServicePartitionInformation info, Type messageType, bool flushQueue)
		{
			var serviceReference = CreateServiceReference(context, info);
			return UnregisterMessageTypeAsync(serviceReference, messageType, flushQueue);
		}
	}
}
