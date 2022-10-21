using MakoIoT.Core.Services.Messaging.Extensions;
using MakoIoT.Messages;
using Newtonsoft.Json;
using Xunit;

namespace MakoIoT.Core.Services.Messaging.Test
{
    public class SerializationTests
    {
        [Fact]
        public void SerializeObject_should_serialize_properties_of_concrete_type()
        {
            string messageText = "Message\":{\"Text\":\"Hello!\",\"MessageType\":\"MakoIoT.Core.Services.Messaging.Test.SerializationTests+TestMessage\"}";

            var envelope = new Envelope
            {
                Message = new TestMessage { Text = "Hello!" }
            };
            
            string result = JsonConvert.SerializeObject(envelope);

            Assert.Contains(messageText, result);

        }

        [Fact]
        public void DeserializeObject_given_TypePropJsonConverter_with_propertyTypeMapping_should_create_concrete_type()
        {
            string messageText = "{\"Message\":{\"Text\":\"Hello!\",\"MessageType\":\"MakoIoT.Core.Services.Messaging.Test.SerializationTests+TestMessage\"}}";

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,

            };
            settings.Converters.Add(new TypePropJsonConverter()
                .WithTypeProp(typeof(IMessage), nameof(IMessage.MessageType))
                .WithLookupAssembly(this.GetType().Assembly));

            var envelope = JsonConvert.DeserializeObject<Envelope>(messageText, settings);

            Assert.IsType<TestMessage>(envelope.Message);
            Assert.Equal("Hello!", ((TestMessage)envelope.Message).Text);

        }

        public class TestMessage : IMessage
        {

            public string Text { get; set; }
            public string MessageType => this.GetType().FullName;
        }
    }
}