using System;
using BUTR.DependencyInjection.Logger;
using MCM.Abstractions;
using MCM.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MCM.Implementation;

public sealed class DropdownJsonConverter : JsonConverter
{
	private readonly IBUTRLogger _logger;

	private readonly Func<string, object?> _getSerializationProperty;

	public DropdownJsonConverter(IBUTRLogger logger, Func<string, object?> getSerializationProperty)
	{
		_logger = logger;
		_getSerializationProperty = getSerializationProperty;
	}

	public override bool CanConvert(Type objectType)
	{
		return SettingsUtils.IsForGenericDropdown(objectType);
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		JToken jToken = JToken.FromObject(new SelectedIndexWrapper(value).SelectedIndex);
		jToken.WriteTo(writer);
	}

	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		try
		{
			if (existingValue == null)
			{
				existingValue = _getSerializationProperty(reader.Path);
			}
			if (existingValue == null)
			{
				return null;
			}
			SelectedIndexWrapper selectedIndexWrapper = new SelectedIndexWrapper(existingValue);
			JToken jToken = JToken.Load(reader);
			selectedIndexWrapper.SelectedIndex = (int.TryParse(jToken.ToString(), out var result) ? result : selectedIndexWrapper.SelectedIndex);
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "Error while deserializing Dropdown");
		}
		return existingValue;
	}
}
