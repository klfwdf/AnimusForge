using System;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001FC RID: 508
	public abstract class SceneModel : MBGameModel<SceneModel>
	{
		// Token: 0x06001F48 RID: 8008
		public abstract string GetConversationSceneForMapPosition(CampaignVec2 campaignPosition);

		// Token: 0x06001F49 RID: 8009
		public abstract string GetBattleSceneForMapPatch(MapPatchData mapPatch, bool isNavalEncounter);
	}
}
