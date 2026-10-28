using System.Collections.Generic;
using MCM.Abstractions.Base;
using MCM.Abstractions.GameFeatures;

namespace MCM.Abstractions;

public sealed class MemorySettingsFormat : ISettingsFormat
{
	private readonly Dictionary<string, BaseSettings> _settings = new Dictionary<string, BaseSettings>();

	public IEnumerable<string> FormatTypes { get; } = new string[1] { "memory" };

	public BaseSettings Load(BaseSettings settings, GameDirectory directory, string filename)
	{
		if (_settings.TryGetValue(directory.Path + filename, out BaseSettings value) || settings != value)
		{
			SettingsUtils.OverrideSettings(settings, value);
		}
		return settings;
	}

	public bool Save(BaseSettings settings, GameDirectory directory, string filename)
	{
		_settings[directory.Path + filename] = settings;
		return true;
	}
}
