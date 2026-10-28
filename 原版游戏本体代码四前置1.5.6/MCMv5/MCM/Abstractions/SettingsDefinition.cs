using System.Collections.Generic;
using MCM.Abstractions.Base;

namespace MCM.Abstractions;

public class SettingsDefinition
{
	public string SettingsId { get; }

	public string DisplayName { get; }

	public List<SettingsPropertyGroupDefinition> SettingPropertyGroups { get; }

	public SettingsDefinition(string id)
	{
		SettingsId = id;
		BaseSettings baseSettings = BaseSettingsProvider.Instance?.GetSettings(id);
		DisplayName = baseSettings?.DisplayName ?? "ERROR";
		SettingPropertyGroups = baseSettings?.GetSettingPropertyGroups() ?? new List<SettingsPropertyGroupDefinition>();
	}

	public SettingsDefinition(string id, string displayName, List<SettingsPropertyGroupDefinition> settingsPropertyGroups)
	{
		SettingsId = id;
		DisplayName = displayName;
		SettingPropertyGroups = settingsPropertyGroups;
	}

	public SettingsDefinition(string id, string displayName, IEnumerable<SettingsPropertyDefinition> settingsProperties)
	{
		SettingsId = id;
		DisplayName = displayName;
		SettingPropertyGroups = SettingsUtils.GetSettingsPropertyGroups('/', settingsProperties);
	}
}
