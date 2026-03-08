using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200010D RID: 269
	public class DefaultDifficultyModel : DifficultyModel
	{
		// Token: 0x0600173D RID: 5949 RVA: 0x0006C420 File Offset: 0x0006A620
		public override float GetPlayerTroopsReceivedDamageMultiplier()
		{
			switch (CampaignOptions.PlayerTroopsReceivedDamage)
			{
			case CampaignOptions.Difficulty.VeryEasy:
				return 0.5f;
			case CampaignOptions.Difficulty.Easy:
				return 0.75f;
			case CampaignOptions.Difficulty.Realistic:
				return 1f;
			default:
				return 1f;
			}
		}

		// Token: 0x0600173E RID: 5950 RVA: 0x0006C460 File Offset: 0x0006A660
		public override int GetPlayerRecruitSlotBonus()
		{
			switch (CampaignOptions.RecruitmentDifficulty)
			{
			case CampaignOptions.Difficulty.VeryEasy:
				return 2;
			case CampaignOptions.Difficulty.Easy:
				return 1;
			case CampaignOptions.Difficulty.Realistic:
				return 0;
			default:
				return 0;
			}
		}

		// Token: 0x0600173F RID: 5951 RVA: 0x0006C490 File Offset: 0x0006A690
		public override float GetPlayerMapMovementSpeedBonusMultiplier()
		{
			switch (CampaignOptions.PlayerMapMovementSpeed)
			{
			case CampaignOptions.Difficulty.VeryEasy:
				return 0.1f;
			case CampaignOptions.Difficulty.Easy:
				return 0.05f;
			case CampaignOptions.Difficulty.Realistic:
				return 0f;
			default:
				return 0f;
			}
		}

		// Token: 0x06001740 RID: 5952 RVA: 0x0006C4D0 File Offset: 0x0006A6D0
		public override float GetStealthDifficultyMultiplier()
		{
			switch (CampaignOptions.StealthAndDisguiseDifficulty)
			{
			case CampaignOptions.Difficulty.VeryEasy:
				return 0.35f;
			case CampaignOptions.Difficulty.Easy:
				return 0.55f;
			case CampaignOptions.Difficulty.Realistic:
				return 1f;
			default:
				return 0f;
			}
		}

		// Token: 0x06001741 RID: 5953 RVA: 0x0006C510 File Offset: 0x0006A710
		public override float GetDisguiseDifficultyMultiplier()
		{
			switch (CampaignOptions.StealthAndDisguiseDifficulty)
			{
			case CampaignOptions.Difficulty.VeryEasy:
				return 0.4f;
			case CampaignOptions.Difficulty.Easy:
				return 1f;
			case CampaignOptions.Difficulty.Realistic:
				return 1.2f;
			default:
				return 0f;
			}
		}

		// Token: 0x06001742 RID: 5954 RVA: 0x0006C550 File Offset: 0x0006A750
		public override float GetCombatAIDifficultyMultiplier()
		{
			switch (CampaignOptions.CombatAIDifficulty)
			{
			case CampaignOptions.Difficulty.VeryEasy:
				return 0f;
			case CampaignOptions.Difficulty.Easy:
				return 0.5f;
			}
			return 1f;
		}

		// Token: 0x06001743 RID: 5955 RVA: 0x0006C588 File Offset: 0x0006A788
		public override float GetPersuasionBonusChance()
		{
			switch (CampaignOptions.PersuasionSuccessChance)
			{
			case CampaignOptions.Difficulty.VeryEasy:
				return 0.1f;
			case CampaignOptions.Difficulty.Easy:
				return 0.05f;
			case CampaignOptions.Difficulty.Realistic:
				return 0f;
			default:
				return 0f;
			}
		}

		// Token: 0x06001744 RID: 5956 RVA: 0x0006C5C8 File Offset: 0x0006A7C8
		public override float GetClanMemberDeathChanceMultiplier()
		{
			switch (CampaignOptions.ClanMemberDeathChance)
			{
			case CampaignOptions.Difficulty.VeryEasy:
				return -1f;
			case CampaignOptions.Difficulty.Easy:
				return -0.5f;
			case CampaignOptions.Difficulty.Realistic:
				return 0f;
			default:
				return 0f;
			}
		}
	}
}
