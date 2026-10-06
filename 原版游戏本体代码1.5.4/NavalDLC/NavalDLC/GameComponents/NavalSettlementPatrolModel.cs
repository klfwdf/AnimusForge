using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace NavalDLC.GameComponents;

public class NavalSettlementPatrolModel : SettlementPatrolModel
{
	public override bool CanSettlementHavePatrolParties(Settlement settlement, bool naval)
	{
		if (naval)
		{
			if (settlement.OwnerClan != null && !settlement.OwnerClan.IsRebelClan && settlement.IsTown && settlement.HasPort && settlement.OwnerClan.Kingdom != null)
			{
				return HasCoastalEdict(settlement.OwnerClan.Kingdom);
			}
			return false;
		}
		return base.BaseModel.CanSettlementHavePatrolParties(settlement, naval);
	}

	public override PartyTemplateObject GetPartyTemplateForPatrolParty(Settlement settlement, bool naval)
	{
		if (naval)
		{
			return settlement.OwnerClan.Culture.SettlementPatrolPartyTemplateNaval;
		}
		return base.BaseModel.GetPartyTemplateForPatrolParty(settlement, naval);
	}

	public override CampaignTime GetPatrolPartySpawnDuration(Settlement settlement, bool naval)
	{
		if (naval)
		{
			return CampaignTime.Days(settlement.RandomFloatWithSeed((uint)CampaignTime.Now.ElapsedMillisecondsUntilNow, 5f, 7f));
		}
		return base.BaseModel.GetPatrolPartySpawnDuration(settlement, naval);
	}

	private bool HasCoastalEdict(Kingdom kingdom)
	{
		return kingdom.HasPolicy(NavalPolicies.CoastalGuardEdict);
	}
}
