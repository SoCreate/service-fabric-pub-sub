using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using ServiceFabric.PubSubActors.Interfaces;
using ServiceFabric.PubSubActors.PublisherActors;
using ServiceFabric.PubSubActors.State;

namespace ServiceFabric.PubSubActors
{
	/// <remarks>
	/// Base class for a Stateful Actor that serves as a Broker that accepts messages 
	/// from <see cref="StatefulPublisherActor"/> Actors and/or <see cref="StatelessPublisherActor"/> Actors
	/// and forwards them to <see cref="ISubscriberActor"/> Actors.
	/// Every message type results in 1 BrokerActor instance.
	/// </remarks>
	public abstract class BrokerActor : StatefulActor<BrokerActorState>, IBrokerActor
	{
		private string _messageType;
		private IActorTimer _timer;

		/// <summary>
		/// Indicates the maximum size of the Dead Letter Queue for each registered <see cref="ActorReference"/>. (Default: 100)
		/// </summary>
		protected int MaxDeadLetterCount { get; set; } = 100;
		/// <summary>
		/// Gets or sets the interval to wait before starting to publish messages. (Default: 5s after Activation)
		/// </summary>
		protected TimeSpan DueTime { get; set; } = TimeSpan.FromSeconds(5);

		/// <summary>
		/// Gets or sets the interval to wait between batches of publishing messages. (Default: 5s)
		/// </summary>
		protected TimeSpan Period { get; set; } = TimeSpan.FromSeconds(5);

		/// <summary>
		/// Gets or sets the maximum amount of attempts to broadcast a message to subscribers. (default: 5)
		/// </summary>
		[Obsolete("No longer used.")]
		protected int MaxRetryCount { get; set; } = 5;

		/// <summary>
		/// Gets or sets the delay between retrying attempts to broadcast a message to subscribers. (default: 1s)
		/// </summary>
		[Obsolete("No longer used.")]
		protected TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(1);

		/// <summary>
		/// When Set, this callback will be used to trace Actor messages to.
		/// </summary>
		protected Action<string> ActorEventSourceMessageCallback { get; set; }

		/// <summary>
		/// Registers an Actor as a subscriber for messages.
		/// </summary>
		/// <param name="actor">Reference to the actor to register.</param>
		public Task RegisterSubscriberAsync(ActorReference actor)
		{
			if (actor == null) throw new ArgumentNullException(nameof(actor));

			ActorEventSourceMessage($"Registering Subscriber '{actor.ServiceUri}' for messages of type '{_messageType}'");

			var actorReference = new ActorReferenceWrapper(actor);
			if (!State.SubscriberMessages.ContainsKey(actorReference))
			{
				State.SubscriberMessages.Add(actorReference, new Queue<MessageWrapper>());
			}
			return Task.FromResult(true);
		}
		
		/// <summary>
		/// Unregisters an Actor as a subscriber for messages.
		/// </summary>
		/// <param name="actor">Reference to the actor to unsubscribe.</param>
		/// <param name="flushQueue">Publish any remaining messages.</param>
		public async Task UnregisterSubscriberAsync(ActorReference actor, bool flushQueue)
		{
			if (actor == null) throw new ArgumentNullException(nameof(actor));

			var actorReference = new ActorReferenceWrapper(actor);
			Queue<MessageWrapper> queue;
			if (flushQueue && State.SubscriberMessages.TryGetValue(actorReference, out queue))
			{
				await ProcessQueueAsync(new KeyValuePair<ReferenceWrapper, Queue<MessageWrapper>>(actorReference, queue));
			}
			State.SubscriberMessages.Remove(actorReference);
		}

		/// <summary>
		/// Registers a service as a subscriber for messages.
		/// </summary>
		/// <param name="service">Reference to the service to register.</param>
		public Task RegisterServiceSubscriberAsync(ServiceReference service)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));

			ActorEventSourceMessage($"Registering Subscriber '{service.ServiceUri}' for messages of type '{_messageType}'");

			var serviceReference = new ServiceReferenceWrapper(service);
			if (!State.SubscriberMessages.ContainsKey(serviceReference))
			{
				State.SubscriberMessages.Add(serviceReference, new Queue<MessageWrapper>());
			}
			return Task.FromResult(true);
		}

		/// <summary>
		/// Unregisters a service as a subscriber for messages.
		/// </summary>
		/// <param name="service">Reference to the actor to unsubscribe.</param>
		/// <param name="flushQueue">Publish any remaining messages.</param>
		public async Task UnregisterServiceSubscriberAsync(ServiceReference service, bool flushQueue)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));

			var serviceReference = new ServiceReferenceWrapper(service);
			Queue<MessageWrapper> queue;
			if (flushQueue && State.SubscriberMessages.TryGetValue(serviceReference, out queue))
			{
				await ProcessQueueAsync(new KeyValuePair<ReferenceWrapper, Queue<MessageWrapper>>(serviceReference, queue));
			}
			State.SubscriberMessages.Remove(serviceReference);
		}

		/// <summary>
		/// Takes a published message and forwards it (indirectly) to all Subscribers.
		/// </summary>
		/// <param name="message">The message to publish</param>
		/// <returns></returns>
		public Task PublishMessageAsync(MessageWrapper message)
		{
			ActorEventSourceMessage($"Publishing message of type '{message.MessageType}'");

			foreach (var actorRef in State.SubscriberMessages.Keys)
			{
				State.SubscriberMessages[actorRef].Enqueue(message);
			}

			return Task.FromResult(true);
		}

		/// <summary>
		/// This method is called whenever an actor is activated. 
		/// Creates the initial state object and starts the Message forwarding timer.
		/// </summary>
		protected override Task OnActivateAsync()
		{
			if (Id.Kind != ActorIdKind.String)
				throw new InvalidOperationException("BrokerActor can only be created using a String ID. The ID should be the Full Name of the Message Type.");

			_messageType = Id.GetStringId();


			if (State == null)
			{
				// This is the first time this actor has ever been activated.
				// Set the actor's initial state values.
				State = new BrokerActorState
				{
					SubscriberMessages = new Dictionary<ReferenceWrapper, Queue<MessageWrapper>>(),
					SubscriberDeadLetters = new Dictionary<ReferenceWrapper, Queue<MessageWrapper>>()
				};

				ActorEventSourceMessage("State initialized.");
			}

			if (_timer == null)
			{
				_timer = RegisterTimer(async _ =>
				{
					await ProcessQueuesAsync();
				}, null, DueTime, Period);

				ActorEventSourceMessage("Timer initialized.");
			}

			return Task.FromResult(true);
		}

		/// <summary>
		/// This method is called right before the actor is deactivated. 
		/// Unregisters the timer.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Threading.Tasks.Task">Task</see> that represents outstanding OnDeactivateAsync operation.
		/// </returns>
		[Readonly]
		protected override Task OnDeactivateAsync()
		{
			if (_timer != null)
			{
				UnregisterTimer(_timer);
			}
			_timer = null;

			return Task.FromResult(true);
		}

		/// <summary>
		/// When overridden, handles an undeliverable message <paramref name="message"/> for listener <paramref name="reference"/>.
		/// By default, it will be added to State.ActorDeadLetters.
		/// </summary>
		/// <param name="reference"></param>
		/// <param name="message"></param>
		protected virtual void HandleUndeliverableMessage(ReferenceWrapper reference, MessageWrapper message)
		{
			ActorEventSourceMessage($"Adding undeliverable message to Actor Dead Letter Queue (Listener: {reference.Name})");
			var deadLetters = GetOrAddActorDeadLetterQueue(reference);
			ValidateQueueDepth(reference, deadLetters);
			deadLetters.Enqueue(message);
		}

		/// <summary>
		/// Returns a 'dead letter queue' for the provided ActorReference, to store undeliverable messages.
		/// </summary>
		/// <param name="actorReference"></param>
		/// <returns></returns>
		private Queue<MessageWrapper> GetOrAddActorDeadLetterQueue(ReferenceWrapper actorReference)
		{
			Queue<MessageWrapper> actorDeadLetters;
			if (!State.SubscriberDeadLetters.TryGetValue(actorReference, out actorDeadLetters))
			{
				actorDeadLetters = new Queue<MessageWrapper>();
				State.SubscriberDeadLetters[actorReference] = actorDeadLetters;
			}

			return actorDeadLetters;
		}

		/// <summary>
		/// Ensures the Queue depth is less than the allowed maximum.
		/// </summary>
		/// <param name="reference"></param>
		/// <param name="deadLetters"></param>
		private void ValidateQueueDepth(ReferenceWrapper reference, Queue<MessageWrapper> deadLetters)
		{
			var queueDepth = deadLetters.Count;
			if (queueDepth > MaxDeadLetterCount)
			{
				ActorEventSourceMessage(
					$"Dead Letter Queue for Subscriber '{reference.Name}' has {queueDepth} items, which is more than the allowed {MaxDeadLetterCount}. Clearing it.");
				deadLetters.Clear();
			}
		}

		/// <summary>
		/// Callback that is called from a timer. Forwards all published messages to subscribers.
		/// </summary>
		/// <returns></returns>
		private async Task ProcessQueuesAsync()
		{
			foreach (var actorMessageQueue in State.SubscriberMessages)
			{
				await ProcessQueueAsync(actorMessageQueue);
			}
		}

		/// <summary>
		/// Forwards all published messages to one subscriber.
		/// </summary>
		/// <param name="actorMessageQueue"></param>
		/// <returns></returns>
		private async Task ProcessQueueAsync(KeyValuePair<ReferenceWrapper, Queue<MessageWrapper>> actorMessageQueue)
		{
			ActorEventSourceMessage(
				$"Processing {actorMessageQueue.Value.Count} queued messages for '{actorMessageQueue.Key.Name}'.");
			int messagesProcessed = 0;


			while (actorMessageQueue.Value.Count > 0)
			{
				var message = actorMessageQueue.Value.Peek();
				ActorEventSourceMessage($"Publishing message to subscribed Actor {actorMessageQueue.Key.Name}");
				try
				{
					await actorMessageQueue.Key.PublishAsync(message);
					ActorEventSourceMessage($"Published message to subscribed Actor {actorMessageQueue.Key.Name}");

					messagesProcessed++;
					actorMessageQueue.Value.Dequeue();
				}
				catch (Exception ex)
				{
					HandleUndeliverableMessage(actorMessageQueue.Key, message);
					ActorEventSourceMessage($"Suppressed error while publishing message to subscribed Actor {actorMessageQueue.Key.Name}. Error: {ex}.");
				}
			}

			ActorEventSourceMessage($"Processed {messagesProcessed} queued messages for '{actorMessageQueue.Key.Name}'.");
		}



		/// <summary>
		/// Outputs the provided message to the <see cref="ActorEventSourceMessageCallback"/> if it's configured.
		/// </summary>
		/// <param name="message"></param>
		private void ActorEventSourceMessage(string message)
		{
			ActorEventSourceMessageCallback?.Invoke(message);
		}
	}
}
