using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class SettlementClaimantCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
		CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, DailyTickSettlement);
	}

	private void DailyTickSettlement(Settlement settlement)
	{
		if (settlement.Town != null && settlement.Town.IsOwnerUnassigned && settlement.OwnerClan != null && settlement.OwnerClan.Kingdom != null)
		{
			Kingdom kingdom = settlement.OwnerClan.Kingdom;
			if (kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision x) => x is SettlementClaimantDecision) == null)
			{
				kingdom.AddDecision(new SettlementClaimantDecision(kingdom.RulingClan, settlement, null, null), ignoreInfluenceCost: true);
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	public void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		if (settlement.IsVillage && settlement.Party.MapEvent != null && !FactionManager.IsAtWarAgainstFaction(settlement.Party.MapEvent.AttackerSide.LeaderParty.MapFaction, newOwner.MapFaction))
		{
			settlement.Party.MapEvent.FinalizeEvent();
		}
		if (openToClaim && newOwner.MapFaction.IsKingdomFaction && (newOwner.MapFaction as Kingdom).Clans.Count > 1 && settlement.Town != null)
		{
			settlement.Town.IsOwnerUnassigned = true;
		}
		foreach (Kingdom item in Kingdom.All)
		{
			foreach (KingdomDecision item2 in item.UnresolvedDecisions.ToList())
			{
				if (item2 is SettlementClaimantDecision settlementClaimantDecision)
				{
					if (settlementClaimantDecision.Settlement == settlement)
					{
						item.RemoveDecision(item2);
					}
				}
				else if (item2 is SettlementClaimantPreliminaryDecision settlementClaimantPreliminaryDecision && settlementClaimantPreliminaryDecision.Settlement == settlement && settlementClaimantPreliminaryDecision.Settlement == settlement)
				{
					item.RemoveDecision(item2);
				}
			}
		}
		if (oldOwner.Clan != Clan.PlayerClan || (newOwner != null && newOwner.Clan == Clan.PlayerClan))
		{
			return;
		}
		foreach (ItemRosterElement item3 in settlement.Stash)
		{
			settlement.ItemRoster.AddToCounts(item3.EquipmentElement.Item, item3.Amount);
		}
		settlement.Stash.Clear();
	}
}
