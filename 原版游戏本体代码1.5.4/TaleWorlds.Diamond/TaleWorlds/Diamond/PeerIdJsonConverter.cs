using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TaleWorlds.Diamond;

public class PeerIdJsonConverter : JsonConverter
{
	public override bool CanWrite => true;

	public override bool CanConvert(Type objectType)
	{
		return typeof(PeerId).IsAssignableFrom(objectType);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		return PeerId.FromString((string?)JObject.Load(reader)["_peerId"]);
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		JProperty content = new JProperty("_peerId", ((PeerId)value/*cast due to .constrained prefix*/).ToString());
		JObject jObject = new JObject();
		jObject.Add(content);
		jObject.WriteTo(writer);
	}
}
