using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SoCreate.ServiceFabric.PubSub.State;

namespace SoCreate.ServiceFabric.PubSub.Events
{
    public interface IBrokerEvents
    {
        event Func<string, ReferenceWrapper, string, Task> Subscribed;
        event Func<string, ReferenceWrapper, string, Task> Unsubscribed;
        event Func<MessageWrapper, Task> MessagePublished;
        event Func<string, ReferenceWrapper, MessageWrapper, Task> MessageQueuedToSubscriber;
        event Func<string, ReferenceWrapper, MessageWrapper, Task> MessageDelivered;
        event Func<string, ReferenceWrapper, MessageWrapper, Exception, Task> MessageDeliveryFailed;
    }

    public interface IBrokerEventsManager : IBrokerEvents
    {
        Task OnSubscribedAsync(string queueName, ReferenceWrapper subscriber, string messageTypeName);
        Task OnUnsubscribedAsync(string queueName, ReferenceWrapper subscriber, string messageTypeName);
        Task OnMessagePublishedAsync(MessageWrapper messageWrapper);
        Task OnMessageQueuedToSubscriberAsync(string queueName, ReferenceWrapper subscriber, MessageWrapper messageWrapper);
        Task OnMessageDeliveredAsync(string queueName, ReferenceWrapper subscriber, MessageWrapper messageWrapper);
        Task OnMessageDeliveryFailedAsync(string queueName, ReferenceWrapper subscriber, MessageWrapper messageWrapper, Exception exception, int throttleFactor = 0);
        Task<List<QueueStats>> GetStatsAsync();
    }
}
