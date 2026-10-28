using MCM.Abstractions.Base.Global;

namespace MCM.Abstractions.Global;

public interface IFluentGlobalSettingsContainer : IGlobalSettingsContainer, ISettingsContainer, ISettingsContainerHasSettingsDefinitions, ISettingsContainerCanOverride, ISettingsContainerCanReset, ISettingsContainerPresets, ISettingsContainerHasSettingsPack
{
	void Register(FluentGlobalSettings settings);

	void Unregister(FluentGlobalSettings settings);
}
