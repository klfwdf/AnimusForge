using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultValuationModel : ValuationModel
{
	public override float GetMilitaryValueOfParty(MobileParty party)
	{
		return party.Party.CalculateCurrentStrength() * 15f;
	}

	public override float GetValueOfTroop(CharacterObject troop)
	{
		return troop.GetPower() * 15f;
	}

	public override float GetValueOfHero(Hero hero)
	{
		if (hero.Clan != null)
		{
			return ((float)hero.Clan.Gold * 0.15f + (float)((1 + hero.Clan.Tier * hero.Clan.Tier) * 500)) * ((hero.Clan.Leader == hero) ? 4f : 1f);
		}
		return 500f;
	}
}
