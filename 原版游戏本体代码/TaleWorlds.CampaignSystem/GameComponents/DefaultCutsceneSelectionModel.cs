using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000109 RID: 265
	public class DefaultCutsceneSelectionModel : CutsceneSelectionModel
	{
		// Token: 0x06001730 RID: 5936 RVA: 0x0006C1DF File Offset: 0x0006A3DF
		public override SceneNotificationData GetKingdomDestroyedSceneNotification(Kingdom kingdom)
		{
			return new KingdomDestroyedSceneNotificationItem(kingdom, CampaignTime.Now);
		}
	}
}
