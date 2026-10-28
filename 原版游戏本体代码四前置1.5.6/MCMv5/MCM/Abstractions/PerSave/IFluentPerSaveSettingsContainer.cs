using MCM.Abstractions.Base.PerSave;

namespace MCM.Abstractions.PerSave;

public interface IFluentPerSaveSettingsContainer : IPerSaveSettingsContainer, ISettingsContainer, ISettingsContainerHasSettingsDefinitions, ISettingsContainerCanOverride, ISettingsContainerCanReset, ISettingsContainerPresets, ISettingsContainerHasUnavailable, ISettingsContainerHasSettingsPack, ISettingsContainerCanInvalidateCache
{
	void Register(FluentPerSaveSettings settings);

	void Unregister(FluentPerSaveSettings settings);
}
