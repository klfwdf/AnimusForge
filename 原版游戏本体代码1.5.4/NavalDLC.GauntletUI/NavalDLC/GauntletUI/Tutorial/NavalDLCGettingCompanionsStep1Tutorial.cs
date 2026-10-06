using NavalDLC.Storyline;
using NavalDLC.Storyline.Quests;
using SandBox.GauntletUI.Tutorial;
using StoryMode.GauntletUI.Tutorial;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace NavalDLC.GauntletUI.Tutorial;

[Tutorial("GettingCompanionsStep1")]
public class NavalDLCGettingCompanionsStep1Tutorial : GettingCompanionsStep1Tutorial
{
	public override bool IsConditionsMetForActivation()
	{
		Settlement currentSettlement = Settlement.CurrentSettlement;
		if (currentSettlement != null && currentSettlement == NavalStorylineData.HomeSettlement && Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(InquireAtOstican)))
		{
			return false;
		}
		return base.IsConditionsMetForActivation();
	}
}
