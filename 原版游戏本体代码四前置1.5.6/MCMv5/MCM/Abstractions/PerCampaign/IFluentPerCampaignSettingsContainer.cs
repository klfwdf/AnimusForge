using MCM.Abstractions.Base.PerCampaign;

namespace MCM.Abstractions.PerCampaign;

public interface IFluentPerCampaignSettingsContainer : IPerCampaignSettingsContainer, ISettingsContainer, ISettingsContainerHasSettingsDefinitions, ISettingsContainerCanOverride, ISettingsContainerCanReset, ISettingsContainerPresets, ISettingsContainerHasUnavailable, ISettingsContainerHasSettingsPack, ISettingsContainerCanInvalidateCache
{
	void Register(FluentPerCampaignSettings settings);

	void Unregister(FluentPerCampaignSettings settings);
}
