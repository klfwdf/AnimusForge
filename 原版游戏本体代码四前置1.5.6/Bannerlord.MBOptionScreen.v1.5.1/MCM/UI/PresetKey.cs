using MCM.Abstractions;
using TaleWorlds.Localization;

namespace MCM.UI;

internal sealed record PresetKey
{
	public string Id { get; init; }

	public string Name => new TextObject(_name).ToString();

	private readonly string _name;

	public PresetKey(ISettingsPreset preset)
	{
		Id = preset.Id;
		_name = preset.Name;
	}

	public PresetKey(string id, string name)
	{
		Id = id;
		_name = name;
	}

	public override string ToString()
	{
		return Name;
	}

	public override int GetHashCode()
	{
		return Id.GetHashCode();
	}

	public bool Equals(PresetKey? other)
	{
		return Id.Equals(other?.Id);
	}
}
