using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class MilitaryPowerModel : MBGameModel<MilitaryPowerModel>
{
	public abstract float GetTroopPower(CharacterObject troop, BattleSideEnum side, MapEvent.PowerCalculationContext context, float leaderModifier);

	public abstract float GetPowerOfParty(PartyBase party, BattleSideEnum side, MapEvent.PowerCalculationContext context);

	public abstract float GetContextModifier(CharacterObject troop, BattleSideEnum battleSideEnum, MapEvent.PowerCalculationContext context);

	public abstract float GetContextModifier(Ship ship, BattleSideEnum battleSideEnum, MapEvent.PowerCalculationContext context);

	public abstract MapEvent.PowerCalculationContext GetContextForPosition(CampaignVec2 position);

	public abstract float GetDefaultTroopPower(CharacterObject troop);

	public abstract float GetPowerModifierOfHero(Hero leaderHero);
}
