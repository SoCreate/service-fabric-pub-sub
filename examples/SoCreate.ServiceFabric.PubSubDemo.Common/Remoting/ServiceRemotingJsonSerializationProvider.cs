using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Services.Remoting.V2;
using Microsoft.ServiceFabric.Services.Remoting.V2.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SoCreate.ServiceFabric.PubSubDemo.Common.Remoting
{
    // example based on: https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-reliable-services-communication-remoting
    public class ServiceRemotingJsonSerializationProvider : IServiceRemotingMessageSerializationProvider
    {
        public IServiceRemotingMessageBodyFactory CreateMessageBodyFactory()
        {
            return new JsonMessageBodyFactory();
        }

        public IServiceRemotingRequestMessageBodySerializer CreateRequestMessageSerializer(
            Type serviceInterfaceType, IEnumerable<Type> requestWrappedTypes, IEnumerable<Type> requestBodyTypes = null)
        {
            return new ServiceRemotingRequestJsonMessageBodySerializer(serviceInterfaceType, requestWrappedTypes, requestBodyTypes);
        }

        public IServiceRemotingResponseMessageBodySerializer CreateResponseMessageSerializer(
            Type serviceInterfaceType, IEnumerable<Type> responseWrappedTypes, IEnumerable<Type> responseBodyTypes = null)
        {
            return new ServiceRemotingResponseJsonMessageBodySerializer(serviceInterfaceType, responseWrappedTypes, responseBodyTypes);
        }
    }

    internal static class ServiceRemotingJsonSerializerSettings
    {
        public static readonly JsonSerializerSettings Default = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All,
            Converters = new List<JsonConverter>()
            {
                new ActorIdJsonConverter()
            }
        };
    }

    public class JsonRemotingRequestBody : IServiceRemotingRequestMessageBody
    {
        private readonly JsonSerializer jsonSerializer;
        private readonly JObject requestBodyObject;

        public JsonRemotingRequestBody(JObject requestBody)
        {
            this.jsonSerializer = JsonSerializer.Create(ServiceRemotingJsonSerializerSettings.Default);
            this.requestBodyObject = requestBody;
        }

        public object GetParameter(int position, string paramName, Type paramType)
        {
            if (this.requestBodyObject.TryGetValue(paramName, out JToken parameterToken))
            {
                return parameterToken?.ToObject(paramType, this.jsonSerializer);
            }

            // should never happen.
            throw new ArgumentException($"Argument {paramName} not found.");
        }

        public void SetParameter(int position, string parameName, object parameter)
        {
            this.requestBodyObject.Add(
                parameName,
                parameter == null ? null : JToken.FromObject(parameter, this.jsonSerializer));
        }

        public override string ToString()
        {
            return this.requestBodyObject.ToString();
        }
    }

    public class ActorIdJsonConverter : JsonConverter
    {
        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ActorId);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (!typeof(ActorId).IsAssignableFrom(objectType))
            {
                throw new JsonSerializationException($"Unexpected type '{objectType.Name}'. Target type is not an ActorId.");
            }

            SerializedActorId serializedActorId = serializer.Deserialize<SerializedActorId>(reader);

            return serializedActorId != null ? (ActorId)serializedActorId : null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            if (value is ActorId actorId)
            {
                serializer.Serialize(writer, (SerializedActorId)actorId);
                return;
            }

            throw new JsonSerializationException($"Unexpected type '{value.GetType().Name}'. Object is not an ActorId.");
        }
    }

    public class SerializedActorId
    {
        public Guid GuidId { get; set; }

        public ActorIdKind Kind { get; set; }

        public long LongId { get; set; }

        public string StringId { get; set; }

        public static implicit operator ActorId(SerializedActorId serializedActorId)
        {
            switch (serializedActorId.Kind)
            {
                case ActorIdKind.Guid: return new ActorId(serializedActorId.GuidId);
                case ActorIdKind.Long: return new ActorId(serializedActorId.LongId);
                case ActorIdKind.String: return new ActorId(serializedActorId.StringId);
            }

            throw new ArgumentException("Unknown ActorIdKind: " + serializedActorId.Kind);
        }

        public static implicit operator SerializedActorId(ActorId actorId)
        {
            SerializedActorId serializedActorId = new SerializedActorId()
            {
                Kind = actorId.Kind
            };

            switch (actorId.Kind)
            {
                case ActorIdKind.Guid:
                    serializedActorId.GuidId = actorId.GetGuidId();
                    break;

                case ActorIdKind.Long:
                    serializedActorId.LongId = actorId.GetLongId();
                    break;

                case ActorIdKind.String:
                    serializedActorId.StringId = actorId.GetStringId();
                    break;

                default:
                    throw new ArgumentException("Unknown ActorIdKind: " + actorId.Kind);
            }

            return serializedActorId;
        }
    }

    public class ServiceRemotingRequestJsonMessageBodySerializer : IServiceRemotingRequestMessageBodySerializer
    {
        private readonly Type[] parameterTypes;
        private readonly Type serviceInterfaceType;
        private readonly Type[] requestWrappedTypes;

        public ServiceRemotingRequestJsonMessageBodySerializer(
            Type serviceInterfaceType, IEnumerable<Type> requestWrappedTypes, IEnumerable<Type> requestBodyTypes = null)
        {
            this.serviceInterfaceType = serviceInterfaceType;
            this.requestWrappedTypes = requestWrappedTypes.ToArray();
            this.parameterTypes = requestBodyTypes?.ToArray();
        }

        public IServiceRemotingRequestMessageBody Deserialize(IIncomingMessageBody messageBody)
        {
            using (StreamReader streamReader = new StreamReader(messageBody.GetReceivedBuffer()))
            using (JsonReader reader = new JsonTextReader(streamReader))
            {
                JsonSerializer serializer = JsonSerializer.Create(ServiceRemotingJsonSerializerSettings.Default);
                JObject deserialized = serializer.Deserialize<JObject>(reader);
                return new JsonRemotingRequestBody(deserialized);
            }
        }

        public IOutgoingMessageBody Serialize(IServiceRemotingRequestMessageBody serviceRemotingRequestMessageBody)
        {
            if (serviceRemotingRequestMessageBody == null)
            {
                return null;
            }

            string json = serviceRemotingRequestMessageBody.ToString();
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            ArraySegment<byte> segment = new ArraySegment<byte>(bytes);
            return new OutgoingMessageBody(new[] { segment });
        }
    }

    public class ServiceRemotingResponseJsonMessageBodySerializer : IServiceRemotingResponseMessageBodySerializer
    {
        private readonly Type[] parameterTypes;
        private readonly Type serviceInterfaceType;
        private readonly Type[] responseWrappedTypes;

        public ServiceRemotingResponseJsonMessageBodySerializer(
            Type serviceInterfaceType, IEnumerable<Type> responseWrappedTypes, IEnumerable<Type> responseBodyTypes = null)
        {
            this.serviceInterfaceType = serviceInterfaceType;
            this.responseWrappedTypes = responseWrappedTypes?.ToArray();
            this.parameterTypes = responseBodyTypes?.ToArray();
        }

        public IServiceRemotingResponseMessageBody Deserialize(IIncomingMessageBody messageBody)
        {
            using (StreamReader streamReader = new StreamReader(messageBody.GetReceivedBuffer()))
            using (JsonTextReader reader = new JsonTextReader(streamReader))
            {
                JsonSerializer serializer = JsonSerializer.Create(ServiceRemotingJsonSerializerSettings.Default);

                return serializer.Deserialize<JsonRemotingResponseBody>(reader);
            }
        }

        public IOutgoingMessageBody Serialize(IServiceRemotingResponseMessageBody responseMessageBody)
        {
            string json = JsonConvert.SerializeObject(
                responseMessageBody, ServiceRemotingJsonSerializerSettings.Default);

            byte[] bytes = Encoding.UTF8.GetBytes(json);
            ArraySegment<byte> segment = new ArraySegment<byte>(bytes);
            return new OutgoingMessageBody(new[] { segment });
        }
    }

    public class JsonMessageBodyFactory : IServiceRemotingMessageBodyFactory
    {
        public IServiceRemotingRequestMessageBody CreateRequest(
            string interfaceName, string methodName, int numberOfParameters, object wrappedRequestObject)
        {
            return new JsonRemotingRequestBody(new JObject());
        }

        public IServiceRemotingResponseMessageBody CreateResponse(
            string interfaceName, string methodName, object wrappedResponseObject)
        {
            return new JsonRemotingResponseBody();
        }
    }

    public class JsonRemotingResponseBody : IServiceRemotingResponseMessageBody
    {
        public object Value { get; set; }

        public object Get(Type paramType)
        {
            return this.Value;
        }

        public void Set(object response)
        {
            this.Value = response;
        }
    }
}