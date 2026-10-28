using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using BUTR.DependencyInjection;
using BUTR.DependencyInjection.Logger;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.GameFeatures;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MCM.Implementation;

public abstract class BaseJsonSettingsFormat : ISettingsFormat
{
	private readonly object _lock = new object();

	protected readonly Dictionary<string, object?> _existingObjects = new Dictionary<string, object>();

	protected readonly IBUTRLogger Logger;

	protected virtual JsonSerializerSettings JsonSerializerSettings { get; }

	public virtual IEnumerable<string> FormatTypes { get; } = new string[1] { "json" };

	protected BaseJsonSettingsFormat(IBUTRLogger logger)
	{
		Logger = logger;
		JsonSerializerSettings = new JsonSerializerSettings
		{
			Formatting = Formatting.Indented,
			Converters = 
			{
				(JsonConverter)new BaseSettingsJsonConverter(logger, AddSerializationProperty, ClearSerializationProperties),
				(JsonConverter)new DropdownJsonConverter(logger, GetSerializationProperty)
			}
		};
	}

	public string SaveJson(BaseSettings settings)
	{
		return JsonConvert.SerializeObject(settings, JsonSerializerSettings);
	}

	public BaseSettings LoadFromJson(BaseSettings settings, string content)
	{
		TryLoadFromJson(ref settings, content);
		return settings;
	}

	protected bool TryLoadFromJson(ref BaseSettings settings, string content)
	{
		lock (_lock)
		{
			try
			{
				if (!TryParse(content, out JObject jObject))
				{
					return false;
				}
				using JsonReader reader = jObject.CreateReader();
				JsonSerializer serializer = JsonSerializer.CreateDefault(JsonSerializerSettings);
				Type type = settings.GetType();
				BaseSettingsJsonConverter baseSettingsJsonConverter = JsonSerializerSettings.Converters.OfType<BaseSettingsJsonConverter>().FirstOrDefault();
				if (baseSettingsJsonConverter != null && baseSettingsJsonConverter.CanConvert(type))
				{
					baseSettingsJsonConverter.ReadJson(reader, type, settings, serializer);
				}
			}
			catch (JsonSerializationException)
			{
				return false;
			}
		}
		return true;
	}

	public virtual bool Save(BaseSettings settings, GameDirectory directory, string filename)
	{
		IFileSystemProvider service = GenericServiceProvider.GetService<IFileSystemProvider>();
		if (service == null)
		{
			return false;
		}
		GameFile orCreateFile = service.GetOrCreateFile(directory, filename + ".json");
		if ((object)orCreateFile == null)
		{
			return false;
		}
		string s = SaveJson(settings);
		try
		{
			return service.WriteData(orCreateFile, Encoding.UTF8.GetBytes(s));
		}
		catch (Exception)
		{
			return false;
		}
	}

	public virtual BaseSettings Load(BaseSettings settings, GameDirectory directory, string filename)
	{
		IFileSystemProvider service = GenericServiceProvider.GetService<IFileSystemProvider>();
		if (service == null)
		{
			return settings;
		}
		GameFile file = service.GetFile(directory, filename + ".json");
		if ((object)file == null)
		{
			return settings;
		}
		byte[] array = service.ReadData(file);
		if (array == null)
		{
			Save(settings, directory, filename);
			return settings;
		}
		try
		{
			if (!TryLoadFromJson(ref settings, Encoding.UTF8.GetString(array)))
			{
				Save(settings, directory, filename);
			}
		}
		catch (Exception)
		{
		}
		return settings;
	}

	protected object? GetSerializationProperty(string path)
	{
		lock (_lock)
		{
			object value;
			return _existingObjects.TryGetValue(path, out value) ? value : null;
		}
	}

	protected void AddSerializationProperty(string path, object? value)
	{
		_existingObjects.Add(path, value);
	}

	protected void ClearSerializationProperties()
	{
		_existingObjects.Clear();
	}

	private static bool TryParse(string content, [NotNullWhen(true)] out JObject? jObject)
	{
		try
		{
			jObject = JObject.Parse(content);
			return true;
		}
		catch (JsonReaderException)
		{
			jObject = null;
			return false;
		}
	}
}
