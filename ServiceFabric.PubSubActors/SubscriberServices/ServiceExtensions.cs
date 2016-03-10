using System;
using System.Fabric;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
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
		public static TResult Deserialize<TResult>(this StatefulService service, MessageWrapper message)
		{
			var payload = JsonConvert.DeserializeObject<TResult>(message.Payload);
			return payload;
		}

		/// <summary>
		/// Deserializes the provided <paramref name="message"/> Payload into an intance of type <typeparam name="TResult"></typeparam>
		/// </summary>
		/// <typeparam name="TResult"></typeparam>
		/// <returns></returns>
		public static TResult Deserialize<TResult>(this StatelessServiceBase service, MessageWrapper message)
		{
			var payload = JsonConvert.DeserializeObject<TResult>(message.Payload);
			return payload;
		}

		/// <summary>
		/// Registers a service as a subscriber for messages of type <paramref name="messageType"/>.
		/// </summary>
		/// <returns></returns>
		public static Task RegisterMessageTypeAsync(this StatefulService service, Type messageType)
		{
			return RegisterMessageTypeAsync(service.ServiceInitializationParameters, service.ServicePartition.PartitionInfo, messageType);
		}

		/// <summary>
		/// Registers a service as a subscriber for messages of type <paramref name="messageType"/>.
		/// </summary>
		/// <returns></returns>
		public static Task RegisterMessageTypeAsync(this StatelessService service, Type messageType)
		{
			return RegisterMessageTypeAsync(service.ServiceInitializationParameters, service.ServicePartition.PartitionInfo, messageType);
		}

		/// <summary>
		/// Unregisters a service as a subscriber for messages of type <paramref name="messageType"/>.
		/// </summary>
		/// <returns></returns>
		public static Task UnregisterMessageTypeAsync(this StatelessService service, Type messageType, bool flushQueue)
		{
			return UnregisterMessageTypeAsync(service.ServiceInitializationParameters, service.ServicePartition.PartitionInfo, messageType, flushQueue);
		}

		/// <summary>
		/// Unregisters a service as a subscriber for messages of type <paramref name="messageType"/>.
		/// </summary>
		/// <returns></returns>
		public static Task UnregisterMessageTypeAsync(this StatefulService service, Type messageType, bool flushQueue)
		{
			return UnregisterMessageTypeAsync(service.ServiceInitializationParameters, service.ServicePartition.PartitionInfo, messageType, flushQueue);
		}

		/// <summary>
		/// Registers a service as a subscriber for messages of type <paramref name="messageType"/>.
		/// </summary>
		/// <returns></returns>
		internal static async Task RegisterMessageTypeAsync(this ServiceReference serviceReference, Type messageType)
		{
			ActorId actorId = new ActorId(messageType.FullName);
			IBrokerActor brokerActor = ActorProxy.Create<IBrokerActor>(actorId, serviceReference.ApplicationName, nameof(IBrokerActor));
			//await Task.FromResult(true);
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
			//await Task.FromResult(true);

			await brokerActor.UnregisterServiceSubscriberAsync(serviceReference, flushQueue);
		}

		/// <summary>
		/// Creates a <see cref="ServiceReference"/> for the provided service parameters and partition info.
		/// </summary>
		/// <param name="parameters"></param>
		/// <param name="info"></param>
		/// <returns></returns>
		internal static ServiceReference CreateServiceReference(ServiceInitializationParameters parameters,
			ServicePartitionInformation info)
		{
			var serviceReference = new ServiceReference
			{
				ApplicationName = parameters.CodePackageActivationContext.ApplicationName,
				PartitionKind = info.Kind,
				ServiceUri = parameters.ServiceName,
				PartitionGuid = parameters.PartitionId,
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
		private static Task RegisterMessageTypeAsync(ServiceInitializationParameters parameters, ServicePartitionInformation info, Type messageType)
		{
			var serviceReference = CreateServiceReference(parameters, info);
			return RegisterMessageTypeAsync(serviceReference, messageType);
		}

		/// <summary>
		/// Unregisters a service as a subscriber for messages of type <paramref name="messageType"/>.
		/// </summary>
		/// <returns></returns>
		private static Task UnregisterMessageTypeAsync(ServiceInitializationParameters parameters, ServicePartitionInformation info, Type messageType, bool flushQueue)
		{
			var serviceReference = CreateServiceReference(parameters, info);
			return UnregisterMessageTypeAsync(serviceReference, messageType, flushQueue);
		}

	}
}
