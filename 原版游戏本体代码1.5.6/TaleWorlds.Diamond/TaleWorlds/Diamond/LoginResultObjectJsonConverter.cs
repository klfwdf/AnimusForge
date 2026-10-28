using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TaleWorlds.Diamond;

public class LoginResultObjectJsonConverter : JsonConverter
{
	private static readonly Dictionary<string, Type> _knownTypes;

	public override bool CanWrite => true;

	static LoginResultObjectJsonConverter()
	{
		_knownTypes = (from t in (from a in AppDomain.CurrentDomain.GetAssemblies()
				where !a.GlobalAssemblyCache
				select a).SelectMany((Assembly a) => a.GetTypes())
			where t.IsSubclassOf(typeof(LoginResultObject))
			select t).ToDictionary((Type item) => item.FullName, (Type item) => item);
	}

	public override bool CanConvert(Type objectType)
	{
		return typeof(LoginResultObject).IsAssignableFrom(objectType);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		JObject jObject = JObject.Load(reader);
		string key = (string?)jObject["_type"];
		if (_knownTypes.TryGetValue(key, out var value))
		{
			LoginResultObject loginResultObject = (LoginResultObject)Activator.CreateInstance(value);
			serializer.Populate(jObject.CreateReader(), loginResultObject);
			return loginResultObject;
		}
		return null;
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		JProperty content = new JProperty("_type", value.GetType().FullName);
		JObject jObject = new JObject();
		jObject.Add(content);
		PropertyInfo[] properties = value.GetType().GetProperties();
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (propertyInfo.CanRead)
			{
				object value2 = propertyInfo.GetValue(value);
				if (value2 != null)
				{
					jObject.Add(propertyInfo.Name, JToken.FromObject(value2, serializer));
				}
			}
		}
		jObject.WriteTo(writer);
	}
}
