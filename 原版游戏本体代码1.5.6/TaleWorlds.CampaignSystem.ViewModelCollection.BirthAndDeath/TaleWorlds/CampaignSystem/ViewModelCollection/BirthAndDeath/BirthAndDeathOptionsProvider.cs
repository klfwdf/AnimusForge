using System.Collections.Generic;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.BirthAndDeath;

public class BirthAndDeathOptionsProvider : ICampaignOptionProvider
{
	public IEnumerable<ICampaignOptionData> GetGameplayCampaignOptions()
	{
		yield return new BooleanCampaignOptionData("IsLifeDeathCycleEnabled", 890, CampaignOptionEnableState.Disabled, () => (!CampaignOptions.IsLifeDeathCycleDisabled) ? 1f : 0f, delegate(float value)
		{
			CampaignOptions.IsLifeDeathCycleDisabled = value == 0f;
		});
	}

	public IEnumerable<ICampaignOptionData> GetCharacterCreationCampaignOptions()
	{
		yield return new BooleanCampaignOptionData("IsLifeDeathCycleEnabled", 890, CampaignOptionEnableState.DisabledLater, () => (!CampaignOptions.IsLifeDeathCycleDisabled) ? 1f : 0f, delegate(float value)
		{
			CampaignOptions.IsLifeDeathCycleDisabled = value == 0f;
		});
	}
}
