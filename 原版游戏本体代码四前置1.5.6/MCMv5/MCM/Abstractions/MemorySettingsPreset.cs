using System;
using MCM.Abstractions.Base;

namespace MCM.Abstractions;

public sealed class MemorySettingsPreset : ISettingsPreset
{
	private readonly Func<BaseSettings> _template;

	public string SettingsId { get; }

	public string Id { get; }

	public string Name { get; }

	public MemorySettingsPreset(string settingId, string id, string name, Func<BaseSettings> template)
	{
		SettingsId = settingId;
		Id = id;
		Name = name;
		_template = template;
	}

	public BaseSettings LoadPreset()
	{
		return _template();
	}

	public bool SavePreset(BaseSettings settings)
	{
		return true;
	}
}
