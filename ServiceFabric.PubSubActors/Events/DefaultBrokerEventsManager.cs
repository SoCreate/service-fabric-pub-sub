using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceFabric.PubSubActors.State;

namespace ServiceFabric.PubSubActors.Events
{
    public class DefaultBrokerEventsManager : IBrokerEventsManager
    {
        public event Func<string, ReferenceWrapper, string, Task> Subscribed;
        public event Func<string, ReferenceWrapper, string, Task> Unsubscribed;
        public event Func<string, ReferenceWrapper, MessageWrapper, Task> MessageReceived;
        public event Func<string, ReferenceWrapper, MessageWrapper, Task> MessageDelivered;
        public event Func<string, ReferenceWrapper, MessageWrapper, Exception, Task> MessageDeliveryFailed;

        private readonly Dictionary<string, QueueStats> _stats = new Dictionary<string, QueueStats>();

        public async Task OnSubscribedAsync(string queueName, ReferenceWrapper subscriber, string messageTypeName)
        {
            if (Subscribed != null)
            {
                await Subscribed.Invoke(queueName, subscriber, messageTypeName);
            }
            _stats[queueName] = new QueueStats
            {
                QueueName = queueName,
                ServiceName = subscriber.Name
            };
        }

        public async Task OnUnsubscribedAsync(string queueName, ReferenceWrapper subscriber, string messageTypeName)
        {
            if (Unsubscribed != null)
            {
                await Unsubscribed.Invoke(queueName, subscriber, messageTypeName);
            }
            _stats.Remove(queueName);
        }

        public async Task OnMessageReceivedAsync(string queueName, ReferenceWrapper subscriber, MessageWrapper messageWrapper)
        {
            if (MessageReceived != null)
            {
                await MessageReceived?.Invoke(queueName, subscriber, messageWrapper);
            }
            _stats[queueName].TotalReceived++;
        }

        public async Task OnMessageDeliveredAsync(string queueName, ReferenceWrapper subscriber, MessageWrapper messageWrapper)
        {
            if (MessageDelivered != null)
            {
                await MessageDelivered?.Invoke(queueName, subscriber, messageWrapper);
            }
            _stats[queueName].TotalDelivered++;
        }

        public async Task OnMessageDeliveryFailedAsync(string queueName, ReferenceWrapper subscriber, MessageWrapper messageWrapper, Exception exception)
        {
            if (MessageDeliveryFailed != null)
            {
                await MessageDeliveryFailed?.Invoke(queueName, subscriber, messageWrapper, exception);
            }
            _stats[queueName].TotalDeliveryFailures++;
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
