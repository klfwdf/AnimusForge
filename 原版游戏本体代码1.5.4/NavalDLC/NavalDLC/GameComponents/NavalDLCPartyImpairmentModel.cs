using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;

namespace NavalDLC.GameComponents;

public class NavalDLCPartyImpairmentModel : PartyImpairmentModel
{
	public override ExplainedNumber GetDisorganizedStateDuration(MobileParty party)
	{
		ExplainedNumber stat = base.BaseModel.GetDisorganizedStateDuration(party);
		PerkHelper.AddPerkBonusForParty(NavalPerks.Shipmaster.Windborne, party, isPrimaryBonus: false, ref stat);
		return stat;
	}

	public override float GetVulnerabilityStateDuration(PartyBase party)
	{
		return base.BaseModel.GetVulnerabilityStateDuration(party);
	}

	public override float GetSiegeExpectedVulnerabilityTime()
	{
		return base.BaseModel.GetSiegeExpectedVulnerabilityTime();
	}

	public override bool CanGetDisorganized(PartyBase partyBase)
	{
		return base.BaseModel.CanGetDisorganized(partyBase);
	}
}
