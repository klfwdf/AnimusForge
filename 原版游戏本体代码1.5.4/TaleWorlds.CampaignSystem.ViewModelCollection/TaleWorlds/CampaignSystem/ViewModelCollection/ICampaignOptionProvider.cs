using System.Collections.Generic;

namespace TaleWorlds.CampaignSystem.ViewModelCollection;

public interface ICampaignOptionProvider
{
	IEnumerable<ICampaignOptionData> GetGameplayCampaignOptions();

	IEnumerable<ICampaignOptionData> GetCharacterCreationCampaignOptions();
}
