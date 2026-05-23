using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001EE RID: 494
	public abstract class PrisonBreakModel : MBGameModel<PrisonBreakModel>
	{
		// Token: 0x06001EED RID: 7917
		public abstract int GetNumberOfGuardsToSpawn(Settlement settlement);

		// Token: 0x06001EEE RID: 7918
		public abstract bool CanPlayerStagePrisonBreak(Settlement settlement);

		// Token: 0x06001EEF RID: 7919
		public abstract int GetPrisonBreakStartCost(Hero prisonerHero);

		// Token: 0x06001EF0 RID: 7920
		public abstract int GetRelationRewardOnPrisonBreak(Hero prisonerHero);

		// Token: 0x06001EF1 RID: 7921
		public abstract float GetRogueryRewardOnPrisonBreak(Hero prisonerHero, bool isSuccess);
	}
}
