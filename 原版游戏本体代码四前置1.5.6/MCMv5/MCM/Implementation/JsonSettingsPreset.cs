using System;
using System.Collections.Generic;
using System.Text;
using BUTR.DependencyInjection;
using BUTR.DependencyInjection.Logger;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.GameFeatures;
using Newtonsoft.Json;

namespace MCM.Implementation;

public sealed class JsonSettingsPreset : ISettingsPreset
{
	private class PresetContainerDefinition
	{
		[JsonProperty(Order = 1)]
		public string Id { get; set; } = string.Empty;

		[JsonProperty(Order = 2)]
		public string Name { get; set; } = string.Empty;
	}

	private sealed class PresetContainer : PresetContainerDefinition
	{
		[JsonProperty(Order = 3)]
		public BaseSettings? Settings { get; set; }
	}

	private readonly GameFile _file;

	private readonly Func<BaseSettings> _getNewSettings;

	private readonly JsonSerializerSettings _jsonSerializerSettings;

	private readonly object _lock = new object();

	private readonly Dictionary<string, object?> _existingObjects = new Dictionary<string, object>();

	public string SettingsId { get; }

	public string Id { get; }

	public string Name { get; }

	public static string? GetPresetId(string content)
	{
		try
		{
			return JsonConvert.DeserializeObject<PresetContainerDefinition>(content)?.Id;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static JsonSettingsPreset? FromFile(BaseSettings settings, GameFile file)
	{
		return FromFile(settings.Id, file, settings.CreateNew);
	}

	public static JsonSettingsPreset? FromFile(string settingsId, GameFile file, Func<BaseSettings> getNewSettings)
	{
		IFileSystemProvider service = GenericServiceProvider.GetService<IFileSystemProvider>();
		if (service == null)
		{
			return null;
		}
		byte[] array = service.ReadData(file);
		if (array == null)
		{
			return null;
		}
		string value = Encoding.UTF8.GetString(array);
		PresetContainerDefinition presetContainerDefinition = JsonConvert.DeserializeObject<PresetContainerDefinition>(value);
		if (presetContainerDefinition == null)
		{
			return null;
		}
		return new JsonSettingsPreset(settingsId, presetContainerDefinition.Id, presetContainerDefinition.Name, file, getNewSettings);
	}

	public JsonSettingsPreset(BaseSettings settings, string id, string name, GameFile file)
		: this(settings.Id, id, name, file, settings.CreateNew)
	{
	}

	public JsonSettingsPreset(string settingsId, string id, string name, GameFile file, Func<BaseSettings> getNewSettings)
	{
		SettingsId = settingsId;
		Id = id;
		Name = name;
		_file = file;
		_getNewSettings = getNewSettings;
		IBUTRLogger<JsonSettingsPreset> logger = GenericServiceProvider.GetService<IBUTRLogger<JsonSettingsPreset>>() ?? new DefaultBUTRLogger<JsonSettingsPreset>();
		_jsonSerializerSettings = new JsonSerializerSettings
		{
			Formatting = Formatting.Indented,
			Converters = 
			{
				(JsonConverter)new BaseSettingsJsonConverter(logger, AddSerializationProperty, ClearSerializationProperties),
				(JsonConverter)new DropdownJsonConverter(logger, GetSerializationProperty)
			}
		};
	}

	public BaseSettings LoadPreset()
	{
		BaseSettings baseSettings = _getNewSettings();
		IFileSystemProvider service = GenericServiceProvider.GetService<IFileSystemProvider>();
		if (service == null)
		{
			return baseSettings;
		}
		byte[] array = service.ReadData(_file);
		if (array == null)
		{
			SavePreset(baseSettings);
			return baseSettings;
		}
		string value = Encoding.UTF8.GetString(array);
		lock (_lock)
		{
			try
			{
				PresetContainer target = new PresetContainer
				{
					Id = Id,
					Name = Name,
					Settings = baseSettings
				};
				JsonConvert.PopulateObject(value, target, _jsonSerializerSettings);
			}
			catch (Exception)
			{
			}
		}
		return baseSettings;
	}

	public bool SavePreset(BaseSettings settings)
	{
		IFileSystemProvider service = GenericServiceProvider.GetService<IFileSystemProvider>();
		if (service == null)
		{
			return false;
		}
		try
		{
			PresetContainer value = new PresetContainer
			{
				Id = Id,
				Name = Name,
				Settings = settings
			};
			string s = JsonConvert.SerializeObject(value, _jsonSerializerSettings);
			service.WriteData(_file, Encoding.UTF8.GetBytes(s));
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private object? GetSerializationProperty(string path)
	{
		lock (_lock)
		{
			object value;
			return _existingObjects.TryGetValue(path, out value) ? value : null;
		}
	}

	private void AddSerializationProperty(string path, object? value)
	{
		_existingObjects.Add(path, value);
	}

	private void ClearSerializationProperties()
	{
		_existingObjects.Clear();
	}
}
