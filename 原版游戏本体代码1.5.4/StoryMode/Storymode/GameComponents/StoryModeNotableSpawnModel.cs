using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;

namespace StoryMode.GameComponents;

public class StoryModeNotableSpawnModel : NotableSpawnModel
{
	public override int GetTargetNotableCountForSettlement(Settlement settlement, Occupation occupation)
	{
		if (!StoryModeManager.Current.MainStoryLine.TutorialPhase.IsCompleted && settlement.StringId == "village_ES3_2")
		{
			return 0;
		}
		return base.BaseModel.GetTargetNotableCountForSettlement(settlement, occupation);
	}
}
