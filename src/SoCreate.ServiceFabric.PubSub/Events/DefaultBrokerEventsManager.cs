using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SoCreate.ServiceFabric.PubSub.State;

namespace SoCreate.ServiceFabric.PubSub.Events
{
    public class DefaultBrokerEventsManager : IBrokerEventsManager
    {
        public event Func<string, ReferenceWrapper, string, Task> Subscribed;
        public event Func<string, ReferenceWrapper, string, Task> Unsubscribed;
        public event Func<string, ReferenceWrapper, MessageWrapper, Task> MessageReceived;
        public event Func<string, ReferenceWrapper, MessageWrapper, Task> MessageDelivered;
        public event Func<string, ReferenceWrapper, MessageWrapper, Exception, Task> MessageDeliveryFailed;

        private readonly ConcurrentDictionary<string, QueueStats> _stats = new ConcurrentDictionary<string, QueueStats>();

        public async Task OnSubscribedAsync(string queueName, ReferenceWrapper subscriber, string messageTypeName)
        {
            var onSubscribed = Subscribed;
            if (onSubscribed != null)
            {
                await onSubscribed.Invoke(queueName, subscriber, messageTypeName);
            }

            var stats = new QueueStats
            {
                QueueName = queueName,
                ServiceName = subscriber.Name
            };
            _stats.TryAdd(queueName, stats);
        }

        public async Task OnUnsubscribedAsync(string queueName, ReferenceWrapper subscriber, string messageTypeName)
        {
            var onUnsubscribed = Unsubscribed;
            if (onUnsubscribed != null)
            {
                await onUnsubscribed.Invoke(queueName, subscriber, messageTypeName);
            }
            _stats.TryRemove(queueName, out _);
        }

        public async Task OnMessageReceivedAsync(string queueName, ReferenceWrapper subscriber, MessageWrapper messageWrapper)
        {
            var onMessageReceived = MessageReceived;
            if (onMessageReceived != null)
            {
                await onMessageReceived.Invoke(queueName, subscriber, messageWrapper);
            }

            if (_stats.TryGetValue(queueName, out var stats))
            {
                stats.TotalReceived++;
            }
        }

        public async Task OnMessageDeliveredAsync(string queueName, ReferenceWrapper subscriber, MessageWrapper messageWrapper)
        {
            var onMessageDelivered = MessageDelivered;
            if (onMessageDelivered != null)
            {
                await onMessageDelivered.Invoke(queueName, subscriber, messageWrapper);
            }

            if (_stats.TryGetValue(queueName, out var stats))
            {
                stats.TotalDelivered++;
            }
        }

        public async Task OnMessageDeliveryFailedAsync(string queueName, ReferenceWrapper subscriber, MessageWrapper messageWrapper, Exception exception, int throttleFactor)
        {
            var onMessageDeliveryFailed = MessageDeliveryFailed;
            if (onMessageDeliveryFailed != null)
            {
                await onMessageDeliveryFailed.Invoke(queueName, subscriber, messageWrapper, exception);
            }

            subscriber.SkipCount = throttleFactor;
            if (_stats.TryGetValue(queueName, out var stats))
            {
                stats.TotalDeliveryFailures++;
            }
        }

        public Task<List<QueueStats>> GetStatsAsync()
        {
            return Task.FromResult(_stats.Values.Select(stat =>
            {
                stat.Time = DateTime.UtcNow;
                return stat;
            }).ToList());
        }
    }
}
