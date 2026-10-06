using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class PartyMoraleModel : MBGameModel<PartyMoraleModel>
{
	public abstract float HighMoraleValue { get; }

	public abstract int GetDailyStarvationMoralePenalty(PartyBase party);

	public abstract int GetDailyNoWageMoralePenalty(MobileParty party);

	public abstract float GetStandardBaseMorale(PartyBase party);

	public abstract float GetVictoryMoraleChange(PartyBase party);

	public abstract float GetDefeatMoraleChange(PartyBase party);

	public abstract ExplainedNumber GetEffectivePartyMorale(MobileParty party, bool includeDescription = false);
}
