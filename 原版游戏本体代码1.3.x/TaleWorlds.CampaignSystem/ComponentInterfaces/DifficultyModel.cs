using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001ED RID: 493
	public abstract class DifficultyModel : MBGameModel<DifficultyModel>
	{
		// Token: 0x06001EE4 RID: 7908
		public abstract float GetPlayerTroopsReceivedDamageMultiplier();

		// Token: 0x06001EE5 RID: 7909
		public abstract int GetPlayerRecruitSlotBonus();

		// Token: 0x06001EE6 RID: 7910
		public abstract float GetPlayerMapMovementSpeedBonusMultiplier();

		// Token: 0x06001EE7 RID: 7911
		public abstract float GetCombatAIDifficultyMultiplier();

		// Token: 0x06001EE8 RID: 7912
		public abstract float GetPersuasionBonusChance();

		// Token: 0x06001EE9 RID: 7913
		public abstract float GetClanMemberDeathChanceMultiplier();

		// Token: 0x06001EEA RID: 7914
		public abstract float GetStealthDifficultyMultiplier();

		// Token: 0x06001EEB RID: 7915
		public abstract float GetDisguiseDifficultyMultiplier();
	}
}
