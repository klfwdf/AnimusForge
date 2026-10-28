using System;
using Newtonsoft.Json;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.ButterLib.SaveSystem;

public sealed class MBGUIDConverter : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		if (!(objectType == typeof(MBGUID)))
		{
			return objectType == typeof(MBGUID?);
		}
		return true;
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		if (value is MBGUID mBGUID)
		{
			serializer.Serialize(writer, mBGUID.InternalValue);
		}
		else
		{
			serializer.Serialize(writer, null);
		}
	}

	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		uint? num = serializer.Deserialize<uint?>(reader);
		if (num.HasValue)
		{
			uint valueOrDefault = num.GetValueOrDefault();
			return new MBGUID(valueOrDefault);
		}
		return null;
	}
}
