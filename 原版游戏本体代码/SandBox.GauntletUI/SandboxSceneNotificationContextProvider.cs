using System;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;

namespace SandBox.GauntletUI
{
	// Token: 0x02000013 RID: 19
	public class SandboxSceneNotificationContextProvider : ISceneNotificationContextProvider
	{
		// Token: 0x060000D6 RID: 214 RVA: 0x00007B82 File Offset: 0x00005D82
		public bool IsContextAllowed(SceneNotificationData.RelevantContextType relevantType)
		{
			return relevantType != SceneNotificationData.RelevantContextType.Map || GameStateManager.Current.ActiveState is MapState;
		}
	}
}
