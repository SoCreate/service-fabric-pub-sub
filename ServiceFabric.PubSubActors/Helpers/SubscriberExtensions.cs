using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ServiceFabric.PubSubActors.Interfaces;
using ServiceFabric.PubSubActors.SubscriberServices;

namespace ServiceFabric.PubSubActors.Helpers
{
    /// <summary>
    /// Marks a service method as being capable of receiving messages.
    /// Follows convention that method has signature 'Task MethodName(MessageType message)'
    /// Polymorphism is supported.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class SubscribeAttribute : Attribute
    {
    }

    public static class SubscriberExtensions
    {
        public static Dictionary<Type, Func<object, Task>> DiscoverMessageHandlers(this ISubscriberService service)
        {
            return DiscoverHandlers(service);
        }

        public static Dictionary<Type, Func<object, Task>> DiscoverMessageHandlers(this ISubscriberActor service)
        {
            return DiscoverHandlers(service);
        }

        private static Dictionary<Type, Func<object, Task>> DiscoverHandlers(object service)
        {
            var handlers = new Dictionary<Type, Func<object, Task>>();
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

                handlers[parameters[0].ParameterType] = m => (Task) method.Invoke(service, new[] {m});
            }

            return handlers;
        }
    }
}