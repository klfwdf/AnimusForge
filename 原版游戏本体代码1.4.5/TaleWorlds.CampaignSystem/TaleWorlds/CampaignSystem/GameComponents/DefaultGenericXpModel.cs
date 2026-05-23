using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultGenericXpModel : GenericXpModel
{
	public override float GetXpMultiplier(Hero hero)
	{
		if (hero.IsPlayerCompanion && Hero.MainHero.GetPerkValue(DefaultPerks.Charm.NaturalLeader))
		{
			return 1.2f;
		}
		return 1f;
	}
}
