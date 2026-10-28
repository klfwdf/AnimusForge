using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TaleWorlds.Diamond;

public class AccessObjectJsonConverter : JsonConverter
{
	public override bool CanWrite => false;

	public override bool CanConvert(Type objectType)
	{
		return typeof(AccessObject).IsAssignableFrom(objectType);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		JObject jObject = JObject.Load(reader);
		AccessObject accessObject;
		switch ((string?)jObject["Type"])
		{
		case "Steam":
			accessObject = new SteamAccessObject();
			break;
		case "Epic":
			accessObject = new EpicAccessObject();
			break;
		case "GOG":
			accessObject = new GOGAccessObject();
			break;
		case "GDK":
			accessObject = new GDKAccessObject();
			break;
		case "PS":
			accessObject = new PSAccessObject();
			break;
		case "Test":
			accessObject = new TestAccessObject();
			break;
		default:
			return null;
		}
		serializer.Populate(jObject.CreateReader(), accessObject);
		return accessObject;
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
	}
}
