using System;
using BUTR.DependencyInjection.Logger;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MCM.Implementation;

public sealed class BaseSettingsJsonConverter : JsonConverter
{
	private readonly IBUTRLogger _logger;

	private readonly Action<string, object?> _addSerializationProperty;

	private readonly Action _clearSerializationProperties;

	public BaseSettingsJsonConverter(IBUTRLogger logger, Action<string, object?> addSerializationProperty, Action clearSerializationProperties)
	{
		_logger = logger;
		_addSerializationProperty = addSerializationProperty;
		_clearSerializationProperties = clearSerializationProperties;
	}

	public override bool CanConvert(Type objectType)
	{
		return objectType.IsSubclassOf(typeof(BaseSettings));
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		if (!(value is BaseSettings settings))
		{
			return;
		}
		JObject jObject = new JObject();
		foreach (ISettingsPropertyDefinition allSettingPropertyDefinition in settings.GetAllSettingPropertyDefinitions())
		{
			if (allSettingPropertyDefinition.SettingType != SettingType.Button)
			{
				jObject.Add(allSettingPropertyDefinition.Id, (allSettingPropertyDefinition.PropertyReference.Value == null) ? null : JToken.FromObject(allSettingPropertyDefinition.PropertyReference.Value, serializer));
			}
		}
		jObject.WriteTo(writer);
	}

	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		if (!(existingValue is BaseSettings settings))
		{
			return existingValue;
		}
		try
		{
			JObject jObject = JObject.Load(reader);
			_clearSerializationProperties();
			foreach (ISettingsPropertyDefinition allSettingPropertyDefinition in settings.GetAllSettingPropertyDefinitions())
			{
				if (allSettingPropertyDefinition.SettingType != SettingType.Button && jObject.TryGetValue(allSettingPropertyDefinition.Id, out JToken value))
				{
					_addSerializationProperty(value.CreateReader().Path, allSettingPropertyDefinition.PropertyReference.Value);
					allSettingPropertyDefinition.PropertyReference.Value = value.ToObject(allSettingPropertyDefinition.PropertyReference.Type, serializer);
				}
			}
			_clearSerializationProperties();
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "Error while deserializing Settings");
		}
		return existingValue;
	}
}
