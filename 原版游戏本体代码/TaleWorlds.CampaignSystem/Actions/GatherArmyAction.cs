using System;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004B2 RID: 1202
	public static class GatherArmyAction
	{
		// Token: 0x060049D3 RID: 18899 RVA: 0x00173390 File Offset: 0x00171590
		private static void ApplyInternal(MobileParty leaderParty, IMapPoint gatheringPoint, float playerInvolvement = 0f)
		{
			Army army = leaderParty.Army;
			CampaignEventDispatcher.Instance.OnArmyGathered(army, gatheringPoint);
		}

		// Token: 0x060049D4 RID: 18900 RVA: 0x001733B0 File Offset: 0x001715B0
		public static void Apply(MobileParty leaderParty, IMapPoint gatheringPoint)
		{
			GatherArmyAction.ApplyInternal(leaderParty, gatheringPoint, (leaderParty == MobileParty.MainParty) ? 1f : 0f);
		}
	}
}
