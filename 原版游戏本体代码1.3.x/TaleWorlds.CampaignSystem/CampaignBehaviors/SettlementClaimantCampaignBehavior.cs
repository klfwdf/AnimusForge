using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x0200043E RID: 1086
	public class SettlementClaimantCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06004537 RID: 17719 RVA: 0x00156728 File Offset: 0x00154928
		public override void RegisterEvents()
		{
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
			CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement>(this.DailyTickSettlement));
		}

		// Token: 0x06004538 RID: 17720 RVA: 0x00156758 File Offset: 0x00154958
		private void DailyTickSettlement(Settlement settlement)
		{
			if (settlement.Town != null && settlement.Town.IsOwnerUnassigned && settlement.OwnerClan != null && settlement.OwnerClan.Kingdom != null)
			{
				Kingdom kingdom = settlement.OwnerClan.Kingdom;
				if (kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision x) => x is SettlementClaimantDecision) == null)
				{
					kingdom.AddDecision(new SettlementClaimantDecision(kingdom.RulingClan, settlement, null, null), true);
				}
			}
		}

		// Token: 0x06004539 RID: 17721 RVA: 0x001567DC File Offset: 0x001549DC
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x0600453A RID: 17722 RVA: 0x001567E0 File Offset: 0x001549E0
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
			foreach (Kingdom kingdom in Kingdom.All)
			{
				foreach (KingdomDecision kingdomDecision in kingdom.UnresolvedDecisions.ToList<KingdomDecision>())
				{
					SettlementClaimantDecision settlementClaimantDecision;
					SettlementClaimantPreliminaryDecision settlementClaimantPreliminaryDecision;
					if ((settlementClaimantDecision = kingdomDecision as SettlementClaimantDecision) != null)
					{
						if (settlementClaimantDecision.Settlement == settlement)
						{
							kingdom.RemoveDecision(kingdomDecision);
						}
					}
					else if ((settlementClaimantPreliminaryDecision = kingdomDecision as SettlementClaimantPreliminaryDecision) != null && settlementClaimantPreliminaryDecision.Settlement == settlement && settlementClaimantPreliminaryDecision.Settlement == settlement)
					{
						kingdom.RemoveDecision(kingdomDecision);
					}
				}
			}
			if (oldOwner.Clan == Clan.PlayerClan && (newOwner == null || newOwner.Clan != Clan.PlayerClan))
			{
				foreach (ItemRosterElement itemRosterElement in settlement.Stash)
				{
					settlement.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement.Item, itemRosterElement.Amount);
				}
				settlement.Stash.Clear();
			}
		}
	}
}
