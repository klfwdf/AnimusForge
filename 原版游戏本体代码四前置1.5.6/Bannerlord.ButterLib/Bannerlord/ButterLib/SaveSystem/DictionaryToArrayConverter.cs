using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Bannerlord.ButterLib.SaveSystem;

public sealed class DictionaryToArrayConverter : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		if (!typeof(IDictionary).IsAssignableFrom(objectType))
		{
			return TypeImplementsGenericInterface(objectType, typeof(IDictionary<, >));
		}
		return true;
	}

	private static bool TypeImplementsGenericInterface(Type concreteType, Type interfaceType)
	{
		return concreteType.GetInterfaces().Any((Type i) => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType);
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		if (!(value is IDictionary dictionary))
		{
			return;
		}
		Type[] genericArguments = value.GetType().GetGenericArguments();
		writer.WriteStartArray();
		foreach (DictionaryEntry item in dictionary)
		{
			if (item.Key != null)
			{
				serializer.Serialize(writer, item.Key, genericArguments[0]);
				serializer.Serialize(writer, item.Value, genericArguments[1]);
			}
		}
		writer.WriteEndArray();
	}

	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		IDictionary dictionary = existingValue as IDictionary;
		if (dictionary == null)
		{
			dictionary = (IDictionary)serializer.ContractResolver.ResolveContract(objectType).DefaultCreator();
		}
		if (reader.TokenType == JsonToken.StartArray)
		{
			reader.Read();
			Type[] genericArguments = objectType.GetGenericArguments();
			while (reader.TokenType != JsonToken.EndArray)
			{
				object obj = serializer.Deserialize(reader, genericArguments[0]);
				reader.Read();
				object value = serializer.Deserialize(reader, genericArguments[1]);
				reader.Read();
				if (obj != null)
				{
					if (dictionary.Contains(obj))
					{
						dictionary[obj] = value;
					}
					else
					{
						dictionary.Add(obj, value);
					}
				}
			}
		}
		else if (reader.TokenType == JsonToken.StartObject)
		{
			serializer.Populate(reader, dictionary);
		}
		return dictionary;
	}
}
