using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Playerdom.Shared.Objects;
using Playerdom.Shared.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Playerdom.Shared
{
    public static class PlayerdomJsonSettings
    {
        public static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            Converters = new List<JsonConverter>() { new XnaConverter() },
            SerializationBinder = new KnownTypesBinder()
        };
    }

    public class KnownTypesBinder : ISerializationBinder
    {
        public IList<Type> KnownTypes { get; set; } = new List<Type>()
        {
            typeof(GameObject),
            typeof(Entity),
            typeof(Player),
            typeof(Townsman),
            typeof(Enemy),
            typeof(Bullet),
            typeof(Dictionary<Guid, GameObject>),
            typeof(Dictionary<Guid, Entity>),
            typeof(Dictionary<string, object>),
            typeof(Dictionary<Guid, Dictionary<string, object>>),
            typeof(Keys),
            typeof(Keys[])
        };

        public Type BindToType(string assemblyName, string typeName)
        {
            return KnownTypes.SingleOrDefault(t => t.UnderlyingSystemType.ToString() == typeName);
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.UnderlyingSystemType.ToString();
        }
    }

    public class XnaConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Point) ||
                objectType == typeof(Vector2) ||
                objectType == typeof(Guid);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType == typeof(Point))
            {
                int xValue = 0;
                int yValue = 0;
                bool gotX = false;
                bool gotY = false;


                while (reader.Read())
                {
                    if (reader.TokenType != JsonToken.PropertyName)
                        break;

                    var propertyName = (string)reader.Value;
                    if (!reader.Read())
                        continue;

                    if (propertyName == "X")
                    {
                        xValue = serializer.Deserialize<int>(reader);
                        gotX = true;
                    }

                    if (propertyName == "Y")
                    {
                        yValue = serializer.Deserialize<int>(reader);
                        gotY = true;
                    }
                }

                if (!(gotX && gotY))
                {
                    throw new InvalidDataException("A Point must have an X and Y properties.");
                }

                return new Point(xValue, yValue);
            }
            else if (objectType == typeof(Vector2))
            {
                float xValue = 0;
                float yValue = 0;
                bool gotX = false;
                bool gotY = false;


                while (reader.Read())
                {
                    if (reader.TokenType != JsonToken.PropertyName)
                        break;

                    var propertyName = (string)reader.Value;
                    if (!reader.Read())
                        continue;

                    if (propertyName == "X")
                    {
                        xValue = serializer.Deserialize<int>(reader);
                        gotX = true;
                    }

                    if (propertyName == "Y")
                    {
                        yValue = serializer.Deserialize<int>(reader);
                        gotY = true;
                    }
                }

                if (!(gotX && gotY))
                {
                    throw new InvalidDataException("A Vector2 must have an X and Y properties.");
                }

                return new Vector2(xValue, yValue);
            }
            else if (objectType == typeof(Guid))
            {
                bool gotId = false;
                string id = "";

                while (reader.Read())
                {
                    if (reader.TokenType != JsonToken.PropertyName)
                        break;

                    var propertyName = (string)reader.Value;
                    if (!reader.Read())
                        continue;

                    if (propertyName == "Id")
                    {
                        id = serializer.Deserialize<string>(reader);
                        gotId = true;
                    }
                }

                if (!gotId)
                {
                    throw new InvalidDataException("A GUID must have a properly-formatted ID field");
                }

                return new Guid(id);
            }


            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value.GetType() == typeof(Point))
            {
                Point p = (Point)value;

                writer.WriteStartObject();
                writer.WritePropertyName("X");
                serializer.Serialize(writer, p.X);
                writer.WritePropertyName("Y");
                serializer.Serialize(writer, p.Y);
                writer.WriteEndObject();
            }
            else if (value.GetType() == typeof(Vector2))
            {
                Vector2 v = (Vector2)value;

                writer.WriteStartObject();
                writer.WritePropertyName("X");
                serializer.Serialize(writer, v.X);
                writer.WritePropertyName("Y");
                serializer.Serialize(writer, v.Y);
                writer.WriteEndObject();
            }
            else if (value.GetType() == typeof(Guid))
            {
                Guid g = (Guid)value;

                writer.WriteStartObject();
                writer.WritePropertyName("Id");
                serializer.Serialize(writer, g.ToString());
                writer.WriteEndObject();
            }
        }
    }
}
