using MCM.Abstractions;
using MCM.Abstractions.Base.Global;
using MCM.Abstractions.Global;

namespace MCM.Implementation.Global;

internal sealed class FluentGlobalSettingsContainer : BaseSettingsContainer<FluentGlobalSettings>, IFluentGlobalSettingsContainer, IGlobalSettingsContainer, ISettingsContainer, ISettingsContainerHasSettingsDefinitions, ISettingsContainerCanOverride, ISettingsContainerCanReset, ISettingsContainerPresets, ISettingsContainerHasSettingsPack
{
	public void Register(FluentGlobalSettings settings)
	{
		if (!(settings.GetType() != typeof(FluentGlobalSettings)))
		{
			RegisterSettings(settings);
		}
	}

	public void Unregister(FluentGlobalSettings settings)
	{
		if (!(settings.GetType() != typeof(FluentGlobalSettings)) && LoadedSettings.ContainsKey(settings.Id))
		{
			LoadedSettings.Remove(settings.Id);
		}
	}
}
