using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001C1 RID: 449
	public abstract class SettlementMilitiaModel : MBGameModel<SettlementMilitiaModel>
	{
		// Token: 0x06001D9C RID: 7580
		public abstract int MilitiaToSpawnAfterSiege(Town town);

		// Token: 0x06001D9D RID: 7581
		public abstract ExplainedNumber CalculateMilitiaChange(Settlement settlement, bool includeDescriptions = false);

		// Token: 0x06001D9E RID: 7582
		public abstract ExplainedNumber CalculateVeteranMilitiaSpawnChance(Settlement settlement);

		// Token: 0x06001D9F RID: 7583
		public abstract void CalculateMilitiaSpawnRate(Settlement settlement, out float meleeTroopRate, out float rangedTroopRate);
	}
}
