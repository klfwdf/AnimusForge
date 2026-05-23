using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001D5 RID: 469
	public abstract class BuildingConstructionModel : MBGameModel<BuildingConstructionModel>
	{
		// Token: 0x1700078A RID: 1930
		// (get) Token: 0x06001E37 RID: 7735
		public abstract int TownBoostCost { get; }

		// Token: 0x1700078B RID: 1931
		// (get) Token: 0x06001E38 RID: 7736
		public abstract int TownBoostBonus { get; }

		// Token: 0x1700078C RID: 1932
		// (get) Token: 0x06001E39 RID: 7737
		public abstract int CastleBoostCost { get; }

		// Token: 0x1700078D RID: 1933
		// (get) Token: 0x06001E3A RID: 7738
		public abstract int CastleBoostBonus { get; }

		// Token: 0x06001E3B RID: 7739
		public abstract ExplainedNumber CalculateDailyConstructionPower(Town town, bool includeDescriptions = false);

		// Token: 0x06001E3C RID: 7740
		public abstract int CalculateDailyConstructionPowerWithoutBoost(Town town);

		// Token: 0x06001E3D RID: 7741
		public abstract int GetBoostCost(Town town);

		// Token: 0x06001E3E RID: 7742
		public abstract int GetBoostAmount(Town town);
	}
}
