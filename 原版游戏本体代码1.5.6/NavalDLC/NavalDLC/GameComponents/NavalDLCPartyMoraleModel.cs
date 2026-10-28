using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;

namespace NavalDLC.GameComponents;

public class NavalDLCPartyMoraleModel : PartyMoraleModel
{
	public override float HighMoraleValue => base.BaseModel.HighMoraleValue;

	public override int GetDailyStarvationMoralePenalty(PartyBase party)
	{
		return base.BaseModel.GetDailyStarvationMoralePenalty(party);
	}

	public override int GetDailyNoWageMoralePenalty(MobileParty party)
	{
		return base.BaseModel.GetDailyNoWageMoralePenalty(party);
	}

	public override float GetStandardBaseMorale(PartyBase party)
	{
		return base.BaseModel.GetStandardBaseMorale(party);
	}

	public override float GetVictoryMoraleChange(PartyBase party)
	{
		return base.BaseModel.GetVictoryMoraleChange(party);
	}

	public override float GetDefeatMoraleChange(PartyBase party)
	{
		return base.BaseModel.GetDefeatMoraleChange(party);
	}

	public override ExplainedNumber GetEffectivePartyMorale(MobileParty party, bool includeDescription = false)
	{
		ExplainedNumber stat = base.BaseModel.GetEffectivePartyMorale(party, includeDescription);
		if (party.Anchor != null && party.CurrentSettlement != null && party.CurrentSettlement.HasPort && party.Anchor.IsAtSettlement(party.CurrentSettlement))
		{
			PerkHelper.AddPerkBonusForParty(NavalPerks.Boatswain.EfficientCaptain, party, isPrimaryBonus: false, ref stat);
		}
		return stat;
	}
}
