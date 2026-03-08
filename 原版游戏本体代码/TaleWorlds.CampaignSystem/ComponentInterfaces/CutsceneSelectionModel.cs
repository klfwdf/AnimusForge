using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001F4 RID: 500
	public abstract class CutsceneSelectionModel : MBGameModel<CutsceneSelectionModel>
	{
		// Token: 0x06001F0C RID: 7948
		public abstract SceneNotificationData GetKingdomDestroyedSceneNotification(Kingdom kingdom);
	}
}
