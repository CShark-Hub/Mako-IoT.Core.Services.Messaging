using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MakoIoT.Core.Services.Messaging.Extensions
{
    public class TypePropJsonConverter : JsonConverter
    {
        private readonly IDictionary<Type, string> _typePropTypeMapping = new Dictionary<Type, string>();
        private readonly List<Assembly> _lookupAssemblies = new List<Assembly>();

        public TypePropJsonConverter WithTypeProp(Type interfaceType, string concreteTypePropertyName)
        {
            _typePropTypeMapping.Add(interfaceType, concreteTypePropertyName);
            return this;
        }

        public TypePropJsonConverter WithLookupAssembly(Assembly assembly)
        {
            _lookupAssemblies.Add(assembly);
            return this;
        }

        public TypePropJsonConverter WithLookupAssemblies(IEnumerable<Assembly> assemblies)
        {
            _lookupAssemblies.AddRange(assemblies);
            return this;
        }

        public override bool CanConvert(Type objectType)
        {
            return _typePropTypeMapping.Keys.Any(t => t.IsAssignableFrom(objectType));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);

            var type = FindType((string)obj[_typePropTypeMapping[objectType]]);
            var ctor = type.GetConstructor(new Type[0]);
            if (ctor == null)
                throw new TypeLoadException($"Parameter-less constructor not found in type {type.FullName}");
            var item = ctor.Invoke(null);

            serializer.Populate(obj.CreateReader(), item);

            return item;

        }


        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {

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


            foreach (var tm in _typePropTypeMapping.Keys)
            {
                type = tm.Assembly.GetType(typeName);
                if (type != null)
                    return type;
            }

            throw new TypeLoadException($"Type {typeName} not found");
        }
    }
}
