using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using SoCreate.ServiceFabric.PubSubDemo.SampleEvents;
using SoCreate.ServiceFabric.PubSub.Helpers;
using SoCreate.ServiceFabric.PubSub.State;
using SoCreate.ServiceFabric.PubSub.Subscriber;

namespace SoCreate.ServiceFabric.PubSubDemo.LoadTest
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class LoadTest : SubscriberStatelessServiceBase
    {
        private int Delay { get; } = 10;

        private int Amount { get; } = 100;

        private int MessageTypeCount { get; } = 2;

        /// <summary>
        /// Use BrokerServiceUnordered as the Broker instead of the standard ordered BrokerService.
        /// This is not supported at the moment.  To test with BrokerServiceUnordered, edit the Broker class to extend BrokerServiceUnordered.
        /// </summary>
        private bool UseConcurrentBroker { get; } = false;

        private readonly List<Type> _messageTypes = new List<Type>();

	    private readonly BrokerClient _brokerClient;

        private readonly Dictionary<string, HashSet<Guid>> _messagesReceived = new Dictionary<string, HashSet<Guid>>();

        private Stopwatch ReceiveTimer { get; set; }

        public LoadTest(StatelessServiceContext context)
            : base(context)
        {
            _brokerClient = new BrokerClient();
        }

        protected override async Task OnOpenAsync(CancellationToken cancellationToken)
        {
            if (Delay > 0)
            {
                ServiceEventSource.Current.ServiceMessage(Context, $"Sleeping for {Delay} seconds.");
                await Task.Delay(TimeSpan.FromSeconds(Delay), cancellationToken);
            }

            var allMessages = CreateMessages();

            // Subscribe to all types
            foreach (var type in _messageTypes)
            {
                await _brokerClient.SubscribeAsync<object>(this.CreateReferenceWrapper(), type, message => null);
            }

            ServiceEventSource.Current.ServiceMessage(Context, "Load Test begin.");

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = -1
            };
            Stopwatch sw = Stopwatch.StartNew();
            Parallel.For(0, allMessages.Length, options, i =>
            {
                var message = allMessages[i];
                _brokerClient.PublishMessageAsync(message).ConfigureAwait(false).GetAwaiter().GetResult();
            });
            sw.Stop();

            ServiceEventSource.Current.ServiceMessage(Context, $"In {sw.ElapsedMilliseconds}ms - Published {Amount} instances of Message Types {string.Join(", ", _messageTypes.Select(t => t.FullName))}.");
        }

        /// <summary>
        /// Overriding the method from the base class because we can handle all message types from one method and the base
        /// method tries to load an assembly that doesn't exist because we created types dynamically.
        /// </summary>
        /// <param name="messageWrapper"></param>
        /// <returns></returns>
        public override Task ReceiveMessageAsync(MessageWrapper messageWrapper)
        {
            if (ReceiveTimer == null)
            {
                ReceiveTimer = Stopwatch.StartNew();
            }

            var message = MessageWrapperExtensions.PayloadSerializer.Deserialize<SampleEvent>(messageWrapper.Payload);
            var set = _messagesReceived[messageWrapper.MessageType];
            if (!set.Add(message.Id))
            {
                ServiceEventSource.Current.ServiceMessage(Context, $"Received duplicate Message ID {message.Id}.");
            }

            ServiceEventSource.Current.ServiceMessage(Context,$"Load Test Subscriber received Message {messageWrapper.MessageType} {message.Id}.");

            foreach (var hashSet in _messagesReceived.Values)
            {
                if (hashSet.Count != Amount / MessageTypeCount)
                {
                    return Task.CompletedTask;
                }
            }

            ServiceEventSource.Current.ServiceMessage(Context,$"Load Test Subscriber received {Amount} messages in {ReceiveTimer.ElapsedMilliseconds}ms.");

            return Task.CompletedTask;
        }

        private object[] CreateMessages()
        {
            var asmName = new AssemblyName("LoadTestEvents");
            var asmBuild = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
            var modBuild = asmBuild.DefineDynamicModule("Module");
            for (var i = 1; i < MessageTypeCount+1; i++)
            {
                var messageTypeName = "SampleEvent" + i;
                var tb = modBuild.DefineType(messageTypeName, TypeAttributes.Public, typeof(SampleEvent));
                tb.DefineDefaultConstructor(MethodAttributes.Public);
                _messageTypes.Add(tb.CreateType());

                ServiceEventSource.Current.ServiceMessage(Context, $"Created Message Type {messageTypeName} ({i} of {MessageTypeCount}).");
                _messagesReceived[messageTypeName] = new HashSet<Guid>();
            }

            var messagesPerType = new Dictionary<Type, List<object>>();

            var basketCount = Amount / MessageTypeCount;
            for (var i = 0; i < MessageTypeCount; i++)
            {
                var messages = new List<object>();
                messagesPerType.Add(_messageTypes[i], messages);

                for (var j = 0; j < basketCount; j++)
                {
                    var message = Activator.CreateInstance(_messageTypes[i]);
                    messages.Add(message);
                }
            }

            return messagesPerType.Values.SelectMany(l => l).ToArray();
        }
    }
}
