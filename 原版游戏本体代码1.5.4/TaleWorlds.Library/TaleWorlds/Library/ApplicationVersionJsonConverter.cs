using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TaleWorlds.Library;

public class ApplicationVersionJsonConverter : JsonConverter
{
	public override bool CanWrite => true;

	public override bool CanConvert(Type objectType)
	{
		return typeof(ApplicationVersion).IsAssignableFrom(objectType);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		return ApplicationVersion.FromString((string?)JObject.Load(reader)["_version"]);
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		JProperty content = new JProperty("_version", ((ApplicationVersion)value/*cast due to .constrained prefix*/).ToString());
		JObject jObject = new JObject();
		jObject.Add(content);
		jObject.WriteTo(writer);
	}
}
