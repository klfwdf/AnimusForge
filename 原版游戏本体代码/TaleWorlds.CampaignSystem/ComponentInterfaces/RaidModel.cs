using System;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x02000197 RID: 407
	public abstract class RaidModel : MBGameModel<RaidModel>
	{
		// Token: 0x06001C2E RID: 7214
		public abstract MBReadOnlyList<ValueTuple<ItemObject, float>> GetCommonLootItemScores();

		// Token: 0x1700070A RID: 1802
		// (get) Token: 0x06001C2F RID: 7215
		public abstract int GoldRewardForEachLostHearth { get; }

		// Token: 0x06001C30 RID: 7216
		public abstract ExplainedNumber CalculateHitDamage(MapEventSide attackerSide, float settlementHitPoints);
	}
}
