using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x0200018C RID: 396
	public abstract class PartyNavigationModel : MBGameModel<PartyNavigationModel>
	{
		// Token: 0x06001BE7 RID: 7143
		public abstract bool CanPlayerNavigateToPosition(CampaignVec2 vec2, out MobileParty.NavigationType navigationType);

		// Token: 0x06001BE8 RID: 7144
		public abstract float GetEmbarkDisembarkThresholdDistance();

		// Token: 0x06001BE9 RID: 7145
		public abstract bool IsTerrainTypeValidForNavigationType(TerrainType terrainType, MobileParty.NavigationType navigationType);

		// Token: 0x06001BEA RID: 7146
		public abstract int[] GetInvalidTerrainTypesForNavigationType(MobileParty.NavigationType navigationType);

		// Token: 0x06001BEB RID: 7147
		public abstract bool HasNavalNavigationCapability(MobileParty mobileParty);
	}
}
