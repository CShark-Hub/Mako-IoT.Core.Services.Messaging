using System.Reflection;
using MakoIoT.Core.Services.Interface;
using MakoIoT.Core.Services.Messaging.Extensions;
using MakoIoT.Messages;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MakoIoT.Core.Services.Messaging
{
    public class MessageBus : IMessageBus
    {
        private readonly IDictionary<string, Type> _consumerTypes = new Dictionary<string, Type>();
        private readonly IList<string> _subscriptions = new List<string>();

        private readonly ICommunicationService _communicationService;
        private readonly ILogger<MessageBus> _logger;
        private readonly IObjectFactory _objectFactory;
        private readonly HashSet<Assembly> _lookupAssemblies = new HashSet<Assembly>();

        public MessageBus(ICommunicationService communicationService, ILogger<MessageBus> logger, IObjectFactory objectFactory)
        {
            _communicationService = communicationService;
            _logger = logger;
            _objectFactory = objectFactory;

            _communicationService.MessageReceived += OnMessageReceived;
        }

        public void Start()
        {
            _communicationService.Connect(_subscriptions.ToArray());
        }

        public void Stop()
        {
            _communicationService.Disconnect();
        }

        public void RegisterDirectMessageConsumer(Type messageType, Type consumerType)
        {
            _consumerTypes.Add(messageType.FullName, consumerType);
            _lookupAssemblies.Add(messageType.Assembly);
        }

        public void RegisterSubscriptionConsumer(Type messageType, Type consumerType)
        {
            _consumerTypes.Add(messageType.FullName, consumerType);
            _subscriptions.Add(messageType.FullName);
            _lookupAssemblies.Add(messageType.Assembly);
        }

        public void Publish(IMessage message, bool delay = false)
        {
            var envelopeString = WrapMessage(message);

            if (_communicationService.CanSend && !delay)
            {
                _communicationService.Publish(envelopeString, message.MessageType);
            }
        }

        public void Send(IMessage message, string recipient)
        {
            var envelopeString = WrapMessage(message);

            if (_communicationService.CanSend)
            {
                _communicationService.Send(envelopeString, recipient);
            }
        }

        private void OnMessageReceived(object sender, string e)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Objects,

                };
                settings.Converters.Add(new TypePropJsonConverter()
                    .WithLookupAssemblies(_lookupAssemblies)
                    .WithTypeProp(typeof(IMessage), nameof(IMessage.MessageType)));


                var envelope = JsonConvert.DeserializeObject<Envelope>(e, settings);

                if (!_consumerTypes.ContainsKey(envelope.Message.MessageType))
                {
                    _logger.LogWarning($"No consumer for message type {envelope.Message.MessageType}");
                    return;
                }

                _logger.LogDebug($"Received message of type {envelope.Message.MessageType}");

                dynamic consumer = _objectFactory.Create(_consumerTypes[envelope.Message.MessageType]);

                var ctt = typeof(ConsumeContext<>);
                var context = ctt.MakeGenericType(FindType(envelope.Message.MessageType))
                    .GetConstructor(new[] { typeof(Envelope) }).Invoke(new object[] { envelope });

                consumer.Consume(context as dynamic);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error processing message");
            }
        }

        private string WrapMessage(IMessage message)
        {
            var envelope = new Envelope
            {
                OriginTime = DateTime.UtcNow,
                MessageId = Guid.NewGuid().ToString(),
                Sender = _communicationService.ClientName,
                SenderAddress = _communicationService.ClientAddress,
                Message = message
            };

            var envelopeString = JsonConvert.SerializeObject(envelope);

            _logger.LogDebug(envelopeString);
            return envelopeString;
        }

        private Type FindType(string typeName)
        {
            var type = Type.GetType(typeName);

            if (type != null)
                return type;

            foreach (var tm in _lookupAssemblies)
            {
                type = tm.GetType(typeName);
                if (type != null)
                    return type;
            }

            throw new TypeLoadException($"Type {typeName} not found");
        }
    }
}
