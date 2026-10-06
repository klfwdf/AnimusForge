using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class InfluenceGainCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnPrisonerDonatedToSettlementEvent.AddNonSerializedListener(this, OnPrisonerDonatedToSettlement);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnPrisonerDonatedToSettlement(MobileParty donatingParty, FlattenedTroopRoster donatedPrisoners, Settlement donatedSettlement)
	{
		if (donatedSettlement.OwnerClan == Clan.PlayerClan && donatingParty.ActualClan == Clan.PlayerClan)
		{
			return;
		}
		float num = 0f;
		foreach (FlattenedTroopRosterElement donatedPrisoner in donatedPrisoners)
		{
			num += Campaign.Current.Models.PrisonerDonationModel.CalculateInfluenceGainAfterPrisonerDonation(donatingParty.Party, donatedPrisoner.Troop, donatedSettlement);
		}
		GainKingdomInfluenceAction.ApplyForDonatePrisoners(donatingParty, num);
	}
}
