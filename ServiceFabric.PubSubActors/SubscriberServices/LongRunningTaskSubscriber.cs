using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Newtonsoft.Json;
using ServiceFabric.PubSubActors.Interfaces;

namespace ServiceFabric.PubSubActors.SubscriberServices
{
    /// <summary>
    /// Base implementation of a <see cref="ISubscriberService"/> that runs long running tasks without delaying <see cref="ISubscriberService.ReceiveMessageAsync"/>.
    /// </summary>
    public abstract class LongRunningTaskSubscriberService : StatefulService, ISubscriberService
    {
        private readonly ITypeLocator _typeLocator;
        private readonly string _queueName;

        /// <summary>
        /// Gets or sets the interval to wait between batches of publishing messages. (Default: 5s)
        /// </summary>
        protected TimeSpan Period { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// When Set, this callback will be used to trace Service messages to.
        /// </summary>
        protected Action<string> ServiceEventSourceMessageCallback { get; set; }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="serviceContext"></param>
        /// <param name="typeLocator"></param>
        protected LongRunningTaskSubscriberService(StatefulServiceContext serviceContext, ITypeLocator typeLocator)
            : base(serviceContext)
        {
            _typeLocator = typeLocator ?? new TypeLocator(GetType().Assembly);
            _queueName = $"TaskQueue-{GetType().FullName}";
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="serviceContext"></param>
        /// <param name="reliableStateManagerReplica"></param>
        /// <param name="typeLocator"></param>
        protected LongRunningTaskSubscriberService(StatefulServiceContext serviceContext, IReliableStateManagerReplica2 reliableStateManagerReplica, ITypeLocator typeLocator)
            : base(serviceContext, reliableStateManagerReplica)
        {
            _typeLocator = typeLocator ?? new TypeLocator(GetType().Assembly);
            _queueName = $"TaskQueue-{GetType().FullName}";
        }

        /// <inheritdoc />
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners(); //remoting listener
        }

        /// <inheritdoc />
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var queue = await TimeoutRetryHelper.Execute(
                (token, state) => StateManager.GetOrAddAsync<IReliableQueue<MessageWrapper>>(_queueName),
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            while (true)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    //peek task description
                    var taskDescriptionMessage = await TimeoutRetryHelper
                        .ExecuteInTransaction(StateManager, 
                            (tran, token, state) => queue.TryPeekAsync(tran, TimeSpan.FromSeconds(4), token), cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    if (!taskDescriptionMessage.HasValue)
                    {
                        continue;
                    }

                    //deserialize task description, create task implementation
                    var description = this.Deserialize<TaskDescription>(taskDescriptionMessage.Value);
                    var implementation = TaskDescription.ToTaskImplementation(_typeLocator, description);
                    if (implementation == null)
                    {
                        ServiceEventSourceMessageCallback?
                            .Invoke($"Received TaskDescription message with type {description.TaskType} and id {description.TaskId} that could not be transformed into a TaskImplementation type.");
                        continue;
                    }

                    //run task
                    ServiceEventSourceMessageCallback?
                        .Invoke($"Processing TaskDescription message with type {description.TaskType} and id {description.TaskId}, using implementation {implementation.GetType().Name}.");

                    await implementation.ExecuteAsync().ConfigureAwait(false);
                    var taskDescriptionMessageDequeued = await TimeoutRetryHelper
                        .ExecuteInTransaction(StateManager, 
                            (tran, token, state) => queue.TryDequeueAsync(tran, TimeSpan.FromSeconds(4), token), cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    //dequeue task, compare it with the peeked task
                    if (taskDescriptionMessageDequeued.HasValue &&
                        taskDescriptionMessageDequeued.Value.Payload.GetHashCode()
                        == taskDescriptionMessage.Value.Payload.GetHashCode())
                    {
                        ServiceEventSourceMessageCallback?
                            .Invoke($"Processed TaskDescription message with type {description.TaskType} and id {description.TaskId}, using implementation {implementation.GetType().Name}.");
                    }
                    else
                    {
                        ServiceEventSourceMessageCallback?
                            .Invoke($"Unexpected error. The peeked message is not the dequeued message.");
                    }
                }
                catch (TaskCanceledException)
                {//swallow and move on..
                }
                catch (OperationCanceledException)
                {//swallow and move on..
                }
                catch (ObjectDisposedException)
                {//swallow and move on..
                }
                catch (Exception ex)
                {
                    ServiceEventSourceMessageCallback?.Invoke($"Exception caught while processing messages:'{ex.Message}'");
                    //swallow and move on..
                }
                finally
                {
                    await Task.Delay(Period, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc />
        public async Task ReceiveMessageAsync(MessageWrapper message)
        {
            //assume that message contains 'TaskDescription'
            var description = this.Deserialize<TaskDescription>(message);
            if (description == null) return; //wrong message
            
            var queue = await TimeoutRetryHelper.Execute((token, state) => StateManager.GetOrAddAsync<IReliableQueue<MessageWrapper>>(_queueName));
            await TimeoutRetryHelper
                .ExecuteInTransaction(StateManager, (tran, token, state) => queue.EnqueueAsync(tran, message))
                .ConfigureAwait(false);

        }
    }

    public class TaskDescription
    {
        public Guid TaskId { get; }

        public string TaskType { get; }

        public string TaskPayload { get; }

        protected TaskDescription(Guid taskId, string taskType, string taskPayload)
        {
            TaskType = taskType;
            TaskPayload = taskPayload;
            TaskId = taskId;
        }

        public static TaskDescription ToTaskDescription(ITaskImplementation taskImplementation)
        {
            if (taskImplementation == null) throw new ArgumentNullException(nameof(taskImplementation));
            var description = new TaskDescription(Guid.NewGuid(), taskImplementation.GetType().FullName, JsonConvert.SerializeObject(taskImplementation));
            return description;
        }

        public static ITaskImplementation ToTaskImplementation(ITypeLocator typeLocator, TaskDescription taskDescription)
        {
            return typeLocator.LocateAndCreate(taskDescription);
        }
    }

    /// <summary>
    /// Helper that maps type name to type for <see cref="ITaskImplementation"/>.
    /// </summary>
    public interface ITypeLocator
    {
        /// <summary>
        /// Takes the name provided as <paramref name="taskDescription"/>, locates a <see cref="ITaskImplementation"/> of that name
        /// , creates an instance and populates it by using <see cref="TaskDescription.TaskPayload"/>.
        /// </summary>
        /// <param name="taskDescription"></param>
        /// <returns></returns>
        ITaskImplementation LocateAndCreate(TaskDescription taskDescription);
    }

    /// <summary>
    /// Helper class that maps type name to type for <see cref="TaskImplementation"/>.
    /// </summary>
    public class TypeLocator : ITypeLocator
    {
        private readonly Dictionary<string, Type> _registeredTaskTypes = new Dictionary<string, Type>();

        /// <summary>
        /// Creates a new instance that scans the provided <see cref="Assembly"/> for concrete 
        /// implementations of type <see cref="TaskImplementation"/>.
        /// </summary>
        /// <param name="taskAssembly"></param>
        public TypeLocator(Assembly taskAssembly)
        {
            var type = typeof(ITaskImplementation);

            foreach (var taskType in taskAssembly.GetTypes()
                .Where(t => t.IsClass
                            && !t.IsAbstract
                            && type.IsAssignableFrom(t)
                            && t.FullName != null))
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                _registeredTaskTypes.Add(taskType.FullName, taskType);
            }
        }

        /// <inheritdoc />
        public ITaskImplementation LocateAndCreate(TaskDescription taskDescription)
        {
            if (_registeredTaskTypes.TryGetValue(taskDescription.TaskType, out var taskImplementationType))
            {
                return (ITaskImplementation)JsonConvert.DeserializeObject(taskDescription.TaskPayload, taskImplementationType);
            }
            return null;
        }
    }
    /// <summary>
    /// Describes a long running task.
    /// </summary>
    public interface ITaskImplementation
    {
        /// <summary>
        /// Executes the task. 
        /// </summary>
        /// <returns></returns>
        Task ExecuteAsync();
    }

    /// <summary>
    /// Example of a long running task, sleeps for <see cref="SleepPeriodMs"/> milliseconds.
    /// </summary>
    public class SleepTask : ITaskImplementation
    {
        /// <summary>
        /// Sleep period in ms.
        /// </summary>
        public int SleepPeriodMs { get; set; } = 10 * 1000;

        /// <inheritdoc />
        public async Task ExecuteAsync()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(SleepPeriodMs));
        }
    }
}
