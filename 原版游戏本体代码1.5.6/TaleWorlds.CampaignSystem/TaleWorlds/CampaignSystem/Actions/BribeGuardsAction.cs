using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Actions;

public static class BribeGuardsAction
{
	private static void ApplyInternal(Settlement settlement, int gold)
	{
		if (gold > 0)
		{
			if (MBRandom.RandomFloat < (float)gold / 1000f)
			{
				SkillLevelingManager.OnBribeGiven(gold);
			}
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, gold);
			settlement.BribePaid += gold;
		}
	}

	public static void Apply(Settlement settlement, int gold)
	{
		ApplyInternal(settlement, gold);
	}
}
