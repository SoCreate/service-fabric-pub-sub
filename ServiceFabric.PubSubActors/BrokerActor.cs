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
		/// Registers this Actor as a subscriber for messages.
		/// </summary>
		/// <param name="actor">Reference to the actor to register.</param>
		public Task RegisterSubscriberAsync(ActorReference actor)
		{
			ActorEventSourceMessage($"Registering Subscriber '{actor.ServiceUri}' for messages of type '{_messageType}'");

			var actorReference = new ActorReferenceWrapper(actor);
			if (!State.ActorMessages.ContainsKey(actorReference))
			{
				State.ActorMessages.Add(actorReference, new Queue<QueuedMessageWrapper>());
			}
			return Task.FromResult(true);
		}

		/// <summary>
		/// Takes a published message and forwards it (indirectly) to all Subscribers.
		/// </summary>
		/// <param name="message">The message to publish</param>
		/// <returns></returns>
		public Task PublishMessageAsync(MessageWrapper message)
		{
			ActorEventSourceMessage($"Publishing message of type '{message.MessageType}'");

			foreach (var actorRef in State.ActorMessages.Keys)
			{
				State.ActorMessages[actorRef].Enqueue(new QueuedMessageWrapper
				{
					MessageWrapper = message,
					DequeueCount = 0
				});
			}

			return Task.FromResult(true);
		}

		/// <summary>
		/// Unregisters this Actor as a subscriber for messages.
		/// </summary>
		/// <param name="actor">Reference to the actor to unsubscribe.</param>
		/// <param name="flushQueue">Publish any remaining messages.</param>
		public async Task UnregisterSubscriberAsync(ActorReference actor, bool flushQueue)
		{
			if (actor == null) throw new ArgumentNullException(nameof(actor));

			var actorReference = new ActorReferenceWrapper(actor);
			Queue<QueuedMessageWrapper> queue;
			if (flushQueue && State.ActorMessages.TryGetValue(actorReference, out queue))
			{
				await ProcessQueueAsync(new KeyValuePair<ActorReferenceWrapper, Queue<QueuedMessageWrapper>>(actorReference, queue));
			}
			State.ActorMessages.Remove(actorReference);
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
					ActorMessages = new Dictionary<ActorReferenceWrapper, Queue<QueuedMessageWrapper>>(),
					ActorDeadLetters = new Dictionary<ActorReferenceWrapper, Queue<QueuedMessageWrapper>>()
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
		/// When overridden, handles an undeliverable message <paramref name="message"/> for Actor <paramref name="actorReference"/>.
		/// By default, it will be added to State.ActorDeadLetters.
		/// </summary>
		/// <param name="actorReference"></param>
		/// <param name="message"></param>
		protected virtual void HandleUndeliverableMessage(ActorReferenceWrapper actorReference, QueuedMessageWrapper message)
		{
			ActorEventSourceMessage($"Adding undeliverable message to Actor Dead Letter Queue (Actor:{actorReference.ActorReference.ActorId})");
			var deadLetters = GetOrAddActorDeadLetterQueue(actorReference);
			ValidateQueueDepth(actorReference, deadLetters);
			deadLetters.Enqueue(message);
		}

		/// <summary>
		/// Returns a 'dead letter queue' for the provided ActorReference, to store undeliverable messages.
		/// </summary>
		/// <param name="actorReference"></param>
		/// <returns></returns>
		private Queue<QueuedMessageWrapper> GetOrAddActorDeadLetterQueue(ActorReferenceWrapper actorReference)
		{
			Queue<QueuedMessageWrapper> actorDeadLetters;
			if (!State.ActorDeadLetters.TryGetValue(actorReference, out actorDeadLetters))
			{
				actorDeadLetters = new Queue<QueuedMessageWrapper>();
				State.ActorDeadLetters[actorReference] = actorDeadLetters;
			}

			return actorDeadLetters;
		}

		/// <summary>
		/// Ensures the Queue depth is less than the allowed maximum.
		/// </summary>
		/// <param name="actorReference"></param>
		/// <param name="actorDeadLetters"></param>
		private void ValidateQueueDepth(ActorReferenceWrapper actorReference, Queue<QueuedMessageWrapper> actorDeadLetters)
		{
			var queueDepth = actorDeadLetters.Count;
			if (queueDepth > MaxDeadLetterCount)
			{
				ActorEventSourceMessage(
					$"Actor Dead Letter Queue for Actor '{actorReference.ActorReference.ActorId}' has {queueDepth} items, which is more than the allowed {MaxDeadLetterCount}. Clearing it.");
				actorDeadLetters.Clear();
			}
		}

		/// <summary>
		/// Callback that is called from a timer. Forwards all published messages to subscribers.
		/// </summary>
		/// <returns></returns>
		private async Task ProcessQueuesAsync()
		{
			foreach (var actorMessageQueue in State.ActorMessages)
			{
				await ProcessQueueAsync(actorMessageQueue);
			}
		}

		/// <summary>
		/// Forwards all published messages to one subscriber.
		/// </summary>
		/// <param name="actorMessageQueue"></param>
		/// <returns></returns>
		private async Task ProcessQueueAsync(KeyValuePair<ActorReferenceWrapper, Queue<QueuedMessageWrapper>> actorMessageQueue)
		{
			ActorEventSourceMessage(
				$"Processing {actorMessageQueue.Value.Count} queued messages for Actor '{actorMessageQueue.Key.ActorReference.ActorId}'.");
			int messagesProcessed = 0;
			while (actorMessageQueue.Value.Count > 0)
			{
				var message = actorMessageQueue.Value.Peek();
				message.DequeueCount++;

				await PublishMessageToActorAsync(actorMessageQueue.Key, message);

				messagesProcessed++;
				actorMessageQueue.Value.Dequeue();
			}

			ActorEventSourceMessage($"Processed {messagesProcessed} queued messages.");
		}

		/// <summary>
		/// Attempts to publish the message to an Actor, using a retry mechanism.
		/// </summary>
		/// <param name="reference"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		private async Task PublishMessageToActorAsync(ActorReferenceWrapper reference, QueuedMessageWrapper message)
		{
			ISubscriberActor actor = (ISubscriberActor)reference.ActorReference.Bind(typeof(ISubscriberActor));
			ActorEventSourceMessage($"Publishing message to subscribed Actor {reference.ActorReference.ActorId}");
            try
			{
				await actor.ReceiveMessageAsync(message.MessageWrapper);
				ActorEventSourceMessage($"Published message to subscribed Actor {reference.ActorReference.ActorId}");
			}
			catch (Exception ex)
			{
				HandleUndeliverableMessage(reference, message);
				ActorEventSourceMessage($"Suppressed error while publishing message to subscribed Actor {reference.ActorReference.ActorId}. Error: {ex}.");
			}
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
