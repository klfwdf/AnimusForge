using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultGenericXpModel : GenericXpModel
{
	public override float GetXpMultiplier(Hero hero)
	{
		float num = 1f;
		if (hero.IsPlayerCompanion && Hero.MainHero.GetPerkValue(DefaultPerks.Charm.NaturalLeader))
		{
			num += 0.2f;
		}
		return num;
	}
}
