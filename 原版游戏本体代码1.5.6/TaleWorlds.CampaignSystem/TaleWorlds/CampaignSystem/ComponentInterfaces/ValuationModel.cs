using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class ValuationModel : MBGameModel<ValuationModel>
{
	public abstract float GetValueOfTroop(CharacterObject troop);

	public abstract float GetMilitaryValueOfParty(MobileParty party);

	public abstract float GetValueOfHero(Hero hero);
}
