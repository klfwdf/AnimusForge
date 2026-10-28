using System.Collections.Generic;
using MCM.Abstractions.Base;

namespace MCM.Abstractions;

public abstract class BaseSettingsProvider
{
	public static BaseSettingsProvider? Instance { get; internal set; }

	public abstract IEnumerable<SettingsDefinition> SettingsDefinitions { get; }

	public abstract BaseSettings? GetSettings(string id);

	public abstract void SaveSettings(BaseSettings settings);

	public abstract void ResetSettings(BaseSettings settings);

	public abstract void OverrideSettings(BaseSettings settings);

	public abstract IEnumerable<ISettingsPreset> GetPresets(string id);

	public abstract IEnumerable<UnavailableSetting> GetUnavailableSettings();

	public abstract IEnumerable<SettingSnapshot> SaveAvailableSnapshots();

	public abstract IEnumerable<BaseSettings> LoadAvailableSnapshots(IEnumerable<SettingSnapshot> snapshots);
}
