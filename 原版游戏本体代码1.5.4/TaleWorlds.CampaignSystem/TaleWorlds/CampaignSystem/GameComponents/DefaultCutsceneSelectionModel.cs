using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultCutsceneSelectionModel : CutsceneSelectionModel
{
	public override SceneNotificationData GetKingdomDestroyedSceneNotification(Kingdom kingdom)
	{
		return new KingdomDestroyedSceneNotificationItem(kingdom, CampaignTime.Now);
	}
}
