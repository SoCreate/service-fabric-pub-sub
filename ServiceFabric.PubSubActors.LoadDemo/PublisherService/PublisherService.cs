using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Reflection;
using System.Reflection.Emit;
using ServiceFabric.PubSubActors.Helpers;
using ServiceFabric.PubSubActors.PublisherServices;

namespace PublisherService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class PublisherService : StatelessService
    {


        public PublisherService(StatelessServiceContext context)
            : base(context)
        { }

       

        /// <summary>
        /// Publish the configured amount of messages.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            int delay;
            string setting = GetConfigurationValue(Context, "MessageSettings", "InitialDelaySec");
            if (!string.IsNullOrWhiteSpace(setting) && int.TryParse(setting, out delay))
            {
                ServiceEventSource.Current.ServiceMessage(this, $"Sleeping for {delay} seconds.");

                await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
            }


            int ammount;
            setting = GetConfigurationValue(Context, "MessageSettings", "Amount");
            if (string.IsNullOrWhiteSpace(setting) || !int.TryParse(setting, out ammount))
            {
                return;
            }


            int messageTypeCount;
            setting = GetConfigurationValue(Context, "MessageSettings", "MessageTypeCount");
            if (string.IsNullOrWhiteSpace(setting) || !int.TryParse(setting, out messageTypeCount))
            {
                return;
            }

            AssemblyName asmName = new AssemblyName();
            asmName.Name = "Dynamic";
            AssemblyBuilder asmBuild = Thread.GetDomain().DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder modBuild = asmBuild.DefineDynamicModule("Module", "Dynamic.dll");
            List<Type> types = new List<Type>(messageTypeCount);
            for (int i = 0; i < messageTypeCount; i++)
            {
                string messageTypeName = "DataContract" + i;
                TypeBuilder tb = modBuild.DefineType(messageTypeName, TypeAttributes.Public, typeof(Common.DataContracts.DataContract));
                tb.DefineDefaultConstructor(MethodAttributes.Public);
                types.Add(tb.CreateType());

                ServiceEventSource.Current.ServiceMessage(this, $"Created Message Type {messageTypeName} ({i} of {messageTypeCount}.");

            }

            Dictionary<Type, List<object>> messagesPerType = new Dictionary<Type, List<object>>();

            int basketCount = ammount / messageTypeCount;
            for (int i = 0; i < messageTypeCount; i++)
            {
                var messages = new List<object>();
                messagesPerType.Add(types[i], messages);

                for (int j = 0; j < basketCount; j++)
                {
                    var message = Activator.CreateInstance(types[i]);
                    messages.Add(message);
                }
            }

            var allMessages = messagesPerType.Values.SelectMany(l => l).ToArray();
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = -1
            };

            
            var helper = new PublisherServiceHelper();
            var brokerSvcLocator = new BrokerServiceLocator();
            var brokerSvcLocation = await brokerSvcLocator.LocateAsync();

            //sending starts here:

            Stopwatch sw = Stopwatch.StartNew();
            Parallel.For(0, allMessages.Length, options, i =>
            {
                var message = allMessages[i];
                helper.PublishMessageAsync(this, message, brokerSvcLocation).ConfigureAwait(false).GetAwaiter().GetResult();
            });
            sw.Stop();

            ServiceEventSource.Current.ServiceMessage(this, $"In {sw.ElapsedMilliseconds}ms - Published {ammount} instances of Message Types {string.Join(", ", types.Select(t => t.FullName))}.");

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }

        private static string GetConfigurationValue(StatelessServiceContext context, string sectionName, string parameterName)
        {
            var configSection = context.CodePackageActivationContext.GetConfigurationPackageObject("Config");
            var section = (configSection?.Settings.Sections.Contains(sectionName) ?? false) ? configSection?.Settings.Sections[sectionName] : null;
            string endPointType = (section?.Parameters.Contains(parameterName) ?? false) ? section.Parameters[parameterName].Value : null;
            return endPointType;
        }
    }
}
