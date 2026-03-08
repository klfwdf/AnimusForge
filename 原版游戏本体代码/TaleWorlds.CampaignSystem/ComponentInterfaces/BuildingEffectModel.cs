using System;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001D6 RID: 470
	public abstract class BuildingEffectModel : MBGameModel<BuildingEffectModel>
	{
		// Token: 0x06001E40 RID: 7744
		public abstract ExplainedNumber GetBuildingEffect(Building building, BuildingEffectEnum effect);
	}
}
