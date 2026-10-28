using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;

namespace StoryMode.GameComponents;

public class StoryModeCutsceneSelectionModel : CutsceneSelectionModel
{
	public override SceneNotificationData GetKingdomDestroyedSceneNotification(Kingdom kingdom)
	{
		if (StoryModeManager.Current.MainStoryLine.PlayerSupportedKingdom == kingdom)
		{
			return new SupportedFactionDefeatedSceneNotificationItem(kingdom, StoryModeManager.Current.MainStoryLine.IsOnImperialQuestLine);
		}
		return base.BaseModel.GetKingdomDestroyedSceneNotification(kingdom);
	}
}
