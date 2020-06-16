using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SoCreate.ServiceFabric.PubSub.Subscriber
{
    /// <summary>
    /// Marks a service method as being capable of receiving messages.
    /// Follows convention that method has signature 'Task MethodName(MessageType message)'
    /// Polymorphism is supported.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class SubscribeAttribute : Attribute
    {
        public QueueType QueueType { get; }

        public string RoutingKeyName { get; }
        public string RoutingKeyValue { get; }

        public Func<object, Task> Handler { get; set; }
        
        public SubscribeAttribute(QueueType type = QueueType.Ordered, string routingKeyName = null, string routingKeyValue = null)
        {
            QueueType = type;
            RoutingKeyName = routingKeyName;
            RoutingKeyValue = routingKeyValue;
        }
    }

    public enum QueueType
    {
        Ordered,
        Unordered
    }

    public static class SubscriberExtensions
    {
        public static Dictionary<Type, SubscribeAttribute> DiscoverSubscribeAttributes(this ISubscriberService service)
        {
            return DiscoverHandlers(service);
        }

        public static Dictionary<Type, SubscribeAttribute> DiscoverSubscribeAttributes(this ISubscriberActor service)
        {
            return DiscoverHandlers(service);
        }

        private static Dictionary<Type, SubscribeAttribute> DiscoverHandlers(object service)
        {
            var subscribeAttributes = new Dictionary<Type, SubscribeAttribute>();
            var taskType = typeof(Task);
            var methods = service.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var method in methods)
            {
                var subscribeAttribute = method.GetCustomAttributes(typeof(SubscribeAttribute), false)
                    .Cast<SubscribeAttribute>()
                    .SingleOrDefault();

                if (subscribeAttribute == null) continue;

                var parameters = method.GetParameters();
                if (parameters.Length != 1 || !taskType.IsAssignableFrom(method.ReturnType)) continue;

                subscribeAttribute.Handler = m => (Task) method.Invoke(service, new[] {m});
                subscribeAttributes[parameters[0].ParameterType] = subscribeAttribute;
            }

            return subscribeAttributes;
        }
    }
}