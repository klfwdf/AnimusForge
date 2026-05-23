using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x02000494 RID: 1172
	public static class BribeGuardsAction
	{
		// Token: 0x06004949 RID: 18761 RVA: 0x00170D9C File Offset: 0x0016EF9C
		private static void ApplyInternal(Settlement settlement, int gold)
		{
			if (gold > 0)
			{
				if (MBRandom.RandomFloat < (float)gold / 1000f)
				{
					SkillLevelingManager.OnBribeGiven(gold);
				}
				GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, gold, false);
				settlement.BribePaid += gold;
			}
		}

		// Token: 0x0600494A RID: 18762 RVA: 0x00170DD2 File Offset: 0x0016EFD2
		public static void Apply(Settlement settlement, int gold)
		{
			BribeGuardsAction.ApplyInternal(settlement, gold);
		}
	}
}
