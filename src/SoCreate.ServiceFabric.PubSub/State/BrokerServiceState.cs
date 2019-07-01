using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace SoCreate.ServiceFabric.PubSub.State
{
    [DataContract]
    internal sealed class BrokerServiceState
    {
        private static readonly IEnumerable<SubscriptionDetails> Empty = ImmutableList<SubscriptionDetails>.Empty;

        [DataMember]
        public readonly string MessageTypeName;

        [DataMember]
        public IEnumerable<SubscriptionDetails> Subscribers { get; private set; }

        public BrokerServiceState(string messageTypeName, IEnumerable<SubscriptionDetails> subscribers = null)
        {
            MessageTypeName = messageTypeName;
            Subscribers = subscribers != null ? subscribers.ToImmutableList() : Empty;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            // Convert the deserialized collection to an immutable collection
            Subscribers = Subscribers.ToImmutableList();
        }

        /// <summary>
        /// Returns a cloned instance with the same subscribers as the original, plus the new <paramref name="subscriber"/>
        /// </summary>
        /// <param name="current"></param>
        /// <param name="subscriber"></param>
        /// <returns></returns>
        public static BrokerServiceState AddSubscriber(BrokerServiceState current, SubscriptionDetails subscriber)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (subscriber == null) throw new ArgumentNullException(nameof(subscriber));
            if (current.Subscribers.Any(s => s.ServiceOrActorReference.Equals(subscriber.ServiceOrActorReference)))
            {
                return current;
            }

            var clone = new BrokerServiceState(current.MessageTypeName, ((ImmutableList<SubscriptionDetails>)current.Subscribers).Add(subscriber));
            return clone;
        }

        /// <summary>
        /// Returns a cloned instance with the same subscribers as the original, minus the new <paramref name="subscriber"/>
        /// </summary>
        /// <param name="current"></param>
        /// <param name="subscriber"></param>
        /// <returns></returns>
        public static BrokerServiceState RemoveSubscriber(BrokerServiceState current, SubscriptionDetails subscriber)
        {
            if (subscriber == null) throw new ArgumentNullException(nameof(subscriber));

            return RemoveSubscriber(current, subscriber.ServiceOrActorReference);
        }

        /// <summary>
        /// Returns a cloned instance with the same subscribers as the original, minus the new <paramref name="subscriber"/>
        /// </summary>
        /// <param name="current"></param>
        /// <param name="subscriber"></param>
        /// <returns></returns>
        public static BrokerServiceState RemoveSubscriber(BrokerServiceState current, ReferenceWrapper subscriber)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (subscriber == null) throw new ArgumentNullException(nameof(subscriber));

            if (current.Subscribers.All(s => !s.ServiceOrActorReference.Equals(subscriber)))
            {
                return current;
            }

            var clone = new BrokerServiceState(current.MessageTypeName, ((ImmutableList<SubscriptionDetails>)current.Subscribers).RemoveAll(s => s.ServiceOrActorReference.Equals(subscriber)));
            return clone;
        }
    }
    
    internal static class BrokerServiceStateExtensions
    {
        internal static async Task AddOrUpdateSubscription(
            this IReliableDictionary<string, BrokerServiceState> brokerState, ITransaction tx, string queues,
            SubscriptionDetails subscriptionDetails)
        {
            BrokerServiceState AddValueFactory(string key)
            {
                var newState = new BrokerServiceState(subscriptionDetails.MessageTypeName);
                newState = BrokerServiceState.AddSubscriber(newState, subscriptionDetails);
                return newState;
            }

            BrokerServiceState UpdateValueFactory(string key, BrokerServiceState current)
            {
                var newState = BrokerServiceState.AddSubscriber(current, subscriptionDetails);
                return newState;
            }

            await brokerState.AddOrUpdateAsync(tx, queues, AddValueFactory, UpdateValueFactory);
        }

        internal static async Task RemoveSubscription(
            this IReliableDictionary<string, BrokerServiceState> brokerState, ITransaction tx, string queues,
            SubscriptionDetails subscriptionDetails)
        {
            var subscribers = await brokerState.TryGetValueAsync(tx, queues, LockMode.Update);
            if (subscribers.HasValue)
            {
                var newState = BrokerServiceState.RemoveSubscriber(subscribers.Value, subscriptionDetails);
                await brokerState.SetAsync(tx, queues, newState);
            }
        }
    }
}
