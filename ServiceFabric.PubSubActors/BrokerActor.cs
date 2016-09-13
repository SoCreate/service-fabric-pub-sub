using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using ServiceFabric.PubSubActors.Interfaces;
using ServiceFabric.PubSubActors.PublisherActors;
using ServiceFabric.PubSubActors.State;
using ServiceFabric.PubSubActors.SubscriberServices;

namespace ServiceFabric.PubSubActors
{
    /// <remarks>
    /// Base class for a Stateful Actor that serves as a Broker that accepts messages 
    /// from Actors & Services calling <see cref="PublisherActorExtensions.PublishMessageAsync"/>
    /// and forwards them to <see cref="ISubscriberActor"/> Actors and <see cref="ISubscriberService"/> Services.
    /// Every message type results in 1 BrokerActor instance.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
	public abstract class BrokerActor : Actor, IBrokerActor
	{
		private string _messageType;
		private IActorTimer _timer;
		private const string StateKey = "__state__";

        /// <summary>
        /// Initializes a new instance of <see cref="BrokerActor" />
        /// </summary>
        /// <param name="actorService">
        /// The <see cref="Microsoft.ServiceFabric.Actors.Runtime.ActorService" /> that will host this actor instance.
        /// </param>
        /// <param name="actorId">
        /// The <see cref="Microsoft.ServiceFabric.Actors.ActorId" /> for this actor instance.
        /// </param>
        protected BrokerActor(ActorService actorService, ActorId actorId) 
            : base(actorService, actorId)
	    {
	    }

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
		/// When Set, this callback will be used to trace Actor messages to.
		/// </summary>
		protected Action<string> ActorEventSourceMessageCallback { get; set; }

		/// <summary>
		/// Registers an Actor as a subscriber for messages.
		/// </summary>
		/// <param name="actor">Reference to the actor to register.</param>
		public async Task RegisterSubscriberAsync(ActorReference actor)
		{
			if (actor == null) throw new ArgumentNullException(nameof(actor));

			ActorEventSourceMessage($"Registering Subscriber '{actor.ServiceUri}' for messages of type '{_messageType}'");

			var actorReference = new ActorReferenceWrapper(actor);
			var state = await StateManager.GetStateAsync<BrokerActorState>(StateKey);
			if (!state.SubscriberMessages.ContainsKey(actorReference))
			{
				state.SubscriberMessages.Add(actorReference, new Queue<MessageWrapper>());
			}
		}

		/// <summary>
		/// Unregisters an Actor as a subscriber for messages.
		/// </summary>
		/// <param name="actor">Reference to the actor to unsubscribe.</param>
		/// <param name="flushQueue">Publish any remaining messages.</param>
		public async Task UnregisterSubscriberAsync(ActorReference actor, bool flushQueue)
		{
			if (actor == null) throw new ArgumentNullException(nameof(actor));
            ActorEventSourceMessage($"Unregistering Subscriber '{actor.ServiceUri}' for messages of type '{_messageType}'");

            var actorReference = new ActorReferenceWrapper(actor);
			Queue<MessageWrapper> queue;
			var state = await StateManager.GetStateAsync<BrokerActorState>(StateKey);
			if (flushQueue && state.SubscriberMessages.TryGetValue(actorReference, out queue))
			{
				await ProcessQueueAsync(new KeyValuePair<ReferenceWrapper, Queue<MessageWrapper>>(actorReference, queue));
			}
			state.SubscriberMessages.Remove(actorReference);
		}

		/// <summary>
		/// Registers a service as a subscriber for messages.
		/// </summary>
		/// <param name="service">Reference to the service to register.</param>
		public async Task RegisterServiceSubscriberAsync(ServiceReference service)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));

			ActorEventSourceMessage($"Registering Subscriber '{service.ServiceUri}' for messages of type '{_messageType}'");

			var serviceReference = new ServiceReferenceWrapper(service);
			var state = await StateManager.GetStateAsync<BrokerActorState>(StateKey);
			if (!state.SubscriberMessages.ContainsKey(serviceReference))
			{
				state.SubscriberMessages.Add(serviceReference, new Queue<MessageWrapper>());
			}
		}

		/// <summary>
		/// Unregisters a service as a subscriber for messages.
		/// </summary>
		/// <param name="service">Reference to the actor to unsubscribe.</param>
		/// <param name="flushQueue">Publish any remaining messages.</param>
		public async Task UnregisterServiceSubscriberAsync(ServiceReference service, bool flushQueue)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));

            ActorEventSourceMessage($"Unregistering Subscriber '{service.ServiceUri}' for messages of type '{_messageType}'");

            var serviceReference = new ServiceReferenceWrapper(service);
			Queue<MessageWrapper> queue;
			var state = await StateManager.GetStateAsync<BrokerActorState>(StateKey);
			if (flushQueue && state.SubscriberMessages.TryGetValue(serviceReference, out queue))
			{
				await ProcessQueueAsync(new KeyValuePair<ReferenceWrapper, Queue<MessageWrapper>>(serviceReference, queue));
			}
			state.SubscriberMessages.Remove(serviceReference);
		}

		/// <summary>
		/// Takes a published message and forwards it (indirectly) to all Subscribers.
		/// </summary>
		/// <param name="message">The message to publish</param>
		/// <returns></returns>
		public async Task PublishMessageAsync(MessageWrapper message)
		{
			ActorEventSourceMessage($"Publishing message of type '{message.MessageType}'");
			var state = await StateManager.GetStateAsync<BrokerActorState>(StateKey);
			foreach (var actorRef in state.SubscriberMessages.Keys)
			{
				state.SubscriberMessages[actorRef].Enqueue(message);
			}
		}

		/// <summary>
		/// This method is called whenever an actor is activated. 
		/// Creates the initial state object and starts the Message forwarding timer.
		/// </summary>
		protected override async Task OnActivateAsync()
		{
			if (Id.Kind != ActorIdKind.String)
				throw new InvalidOperationException("BrokerActor can only be created using a String ID. The ID should be the Full Name of the Message Type.");

			_messageType = Id.GetStringId();


			if (!await StateManager.ContainsStateAsync(StateKey))
			{
				// This is the first time this actor has ever been activated.
				// Set the actor's initial state values.
				var state = new BrokerActorState
				{
					SubscriberMessages = new Dictionary<ReferenceWrapper, Queue<MessageWrapper>>(),
					SubscriberDeadLetters = new Dictionary<ReferenceWrapper, Queue<MessageWrapper>>()
				};
				await StateManager.TryAddStateAsync(StateKey, state);
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

		}

		/// <summary>
		/// This method is called right before the actor is deactivated. 
		/// Unregisters the timer.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Threading.Tasks.Task">Task</see> that represents outstanding OnDeactivateAsync operation.
		/// </returns>
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
		protected virtual async Task HandleUndeliverableMessageAsync(ReferenceWrapper reference, MessageWrapper message)
		{
            var deadLetters = await GetOrAddActorDeadLetterQueueAsync(reference);
            ActorEventSourceMessage($"Adding undeliverable message to Actor Dead Letter Queue (Listener: {reference.Name}, Dead Letter Queue depth:{deadLetters.Count})");

            ValidateQueueDepth(reference, deadLetters);
			deadLetters.Enqueue(message);
		}

		/// <summary>
		/// Returns a 'dead letter queue' for the provided ActorReference, to store undeliverable messages.
		/// </summary>
		/// <param name="actorReference"></param>
		/// <returns></returns>
		private async Task<Queue<MessageWrapper>> GetOrAddActorDeadLetterQueueAsync(ReferenceWrapper actorReference)
		{
			Queue<MessageWrapper> actorDeadLetters;
			var state = await StateManager.GetStateAsync<BrokerActorState>(StateKey);
			if (!state.SubscriberDeadLetters.TryGetValue(actorReference, out actorDeadLetters))
			{
				actorDeadLetters = new Queue<MessageWrapper>();
				state.SubscriberDeadLetters[actorReference] = actorDeadLetters;
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
			var state = await StateManager.GetStateAsync<BrokerActorState>(StateKey);
			foreach (var actorMessageQueue in state.SubscriberMessages)
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
			int messagesProcessed = 0;
            
			while (actorMessageQueue.Value.Count > 0)
			{
				var message = actorMessageQueue.Value.Peek();
				//ActorEventSourceMessage($"Publishing message to subscribed Actor {actorMessageQueue.Key.Name}");
				try
				{
					await actorMessageQueue.Key.PublishAsync(message);
					ActorEventSourceMessage($"Published message {++messagesProcessed} of {actorMessageQueue.Value.Count} to subscribed Actor {actorMessageQueue.Key.Name}");
					actorMessageQueue.Value.Dequeue();
				}
				catch (Exception ex)
				{
					await HandleUndeliverableMessageAsync(actorMessageQueue.Key, message);
					ActorEventSourceMessage($"Suppressed error while publishing message to subscribed Actor {actorMessageQueue.Key.Name}. Error: {ex}.");
				}
			}
		    if (messagesProcessed > 0)
		    {
		        ActorEventSourceMessage($"Processed {messagesProcessed} queued messages for '{actorMessageQueue.Key.Name}'.");
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
