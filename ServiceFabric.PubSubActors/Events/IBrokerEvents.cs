using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ServiceFabric.PubSubActors.State;

namespace ServiceFabric.PubSubActors.Events
{
    public interface IBrokerEvents
    {
        event Func<string, ReferenceWrapper, string, Task> Subscribed;
        event Func<string, ReferenceWrapper, string, Task> Unsubscribed;
        event Func<string, ReferenceWrapper, MessageWrapper, Task> MessageReceived;
        event Func<string, ReferenceWrapper, MessageWrapper, Task> MessageDelivered;
        event Func<string, ReferenceWrapper, MessageWrapper, Exception, Task> MessageDeliveryFailed;
    }

    public interface IBrokerEventsManager : IBrokerEvents
    {
        Task OnSubscribedAsync(string queueName, ReferenceWrapper subscriber, string messageTypeName);
        Task OnUnsubscribedAsync(string queueName, ReferenceWrapper subscriber, string messageTypeName);
        Task OnMessageReceivedAsync(string queueName, ReferenceWrapper subscriber, MessageWrapper messageWrapper);
        Task OnMessageDeliveredAsync(string queueName, ReferenceWrapper subscriber, MessageWrapper messageWrapper);
        Task OnMessageDeliveryFailedAsync(string queueName, ReferenceWrapper subscriber, MessageWrapper messageWrapper, Exception exception);
        Task<List<QueueStats>> GetStatsAsync();
    }
}