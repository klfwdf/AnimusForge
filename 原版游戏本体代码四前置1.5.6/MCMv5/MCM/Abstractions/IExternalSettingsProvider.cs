using System.Collections.Generic;
using MCM.Abstractions.Base;

namespace MCM.Abstractions;

public interface IExternalSettingsProvider
{
	IEnumerable<SettingsDefinition> SettingsDefinitions { get; }

	BaseSettings? GetSettings(string id);

	void SaveSettings(BaseSettings settings);

	void ResetSettings(BaseSettings settings);

	void OverrideSettings(BaseSettings settings);

	IEnumerable<ISettingsPreset> GetPresets(string id);
}
