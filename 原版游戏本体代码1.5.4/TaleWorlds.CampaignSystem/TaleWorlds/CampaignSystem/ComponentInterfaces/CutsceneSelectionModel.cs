using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class CutsceneSelectionModel : MBGameModel<CutsceneSelectionModel>
{
	public abstract SceneNotificationData GetKingdomDestroyedSceneNotification(Kingdom kingdom);
}
