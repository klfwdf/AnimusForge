using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TaleWorlds.Core;

public class BodyPropertiesJsonConverter : JsonConverter
{
	public override bool CanWrite => true;

	public override bool CanConvert(Type objectType)
	{
		return typeof(BodyProperties).IsAssignableFrom(objectType);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		BodyProperties.FromString((string?)JObject.Load(reader)["_data"], out var bodyProperties);
		return bodyProperties;
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		JProperty content = new JProperty("_data", ((BodyProperties)value/*cast due to .constrained prefix*/).ToString());
		JObject jObject = new JObject();
		jObject.Add(content);
		jObject.WriteTo(writer);
	}
}
