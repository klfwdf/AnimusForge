using System;
using Bannerlord.ButterLib.Common.Extensions;
using Bannerlord.ButterLib.ObjectSystem;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.ButterLib.SaveSystem;

public sealed class MBObjectBaseConverter : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		return typeof(MBObjectBase).IsAssignableFrom(objectType);
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		if (value is MBObjectBase mBObjectBase)
		{
			(ButterLibSubModule.Instance?.GetServiceProvider()?.GetService<IMBObjectKeeper>())?.Keep(mBObjectBase);
			serializer.Serialize(writer, mBObjectBase.Id);
		}
		else
		{
			serializer.Serialize(writer, null);
		}
	}

	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		MBGUID? mBGUID = serializer.Deserialize<MBGUID?>(reader);
		if (mBGUID.HasValue)
		{
			MBGUID valueOrDefault = mBGUID.GetValueOrDefault();
			return (ButterLibSubModule.Instance?.GetServiceProvider()?.GetService<IMBObjectFinder>())?.Find(valueOrDefault, objectType);
		}
		return null;
	}
}
