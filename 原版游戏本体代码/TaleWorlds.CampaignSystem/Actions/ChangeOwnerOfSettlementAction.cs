using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x0200049A RID: 1178
	public static class ChangeOwnerOfSettlementAction
	{
		// Token: 0x06004962 RID: 18786 RVA: 0x00171760 File Offset: 0x0016F960
		private static void ApplyInternal(Settlement settlement, Hero newOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			Clan ownerClan = settlement.OwnerClan;
			Hero oldOwner = ((ownerClan != null) ? ownerClan.Leader : null);
			if (settlement.Town != null)
			{
				settlement.Town.IsOwnerUnassigned = false;
			}
			if (settlement.IsFortification)
			{
				settlement.Town.OwnerClan = newOwner.Clan;
			}
			if (settlement.IsFortification)
			{
				if (settlement.Town.GarrisonParty == null)
				{
					settlement.AddGarrisonParty();
				}
				ChangeGovernorAction.RemoveGovernorOfIfExists(settlement.Town);
			}
			settlement.Party.SetVisualAsDirty();
			foreach (Village village in settlement.BoundVillages)
			{
				village.Settlement.Party.SetVisualAsDirty();
				if (village.VillagerPartyComponent != null && newOwner != null)
				{
					foreach (MobileParty mobileParty in MobileParty.All)
					{
						if (mobileParty.MapEvent == null && mobileParty != MobileParty.MainParty && mobileParty.ShortTermTargetParty == village.VillagerPartyComponent.MobileParty && !mobileParty.MapFaction.IsAtWarWith(newOwner.MapFaction))
						{
							mobileParty.SetMoveModeHold();
						}
					}
				}
			}
			bool openToClaim = (detail == ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege || detail == ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByClanDestruction || detail == ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByLeaveFaction) && settlement.IsFortification;
			if (newOwner != null)
			{
				IFaction mapFaction = newOwner.MapFaction;
				if (settlement.Party.MapEvent != null && !settlement.Party.MapEvent.AttackerSide.LeaderParty.MapFaction.IsAtWarWith(mapFaction) && settlement.Party.MapEvent.Winner == null)
				{
					settlement.Party.MapEvent.DiplomaticallyFinished = true;
					foreach (WarPartyComponent warPartyComponent in settlement.MapFaction.WarPartyComponents)
					{
						MobileParty mobileParty2 = warPartyComponent.MobileParty;
						if (mobileParty2.DefaultBehavior == AiBehavior.DefendSettlement && mobileParty2.TargetSettlement == settlement && mobileParty2.CurrentSettlement == null)
						{
							mobileParty2.SetMoveModeHold();
						}
					}
					settlement.Party.MapEvent.Update();
				}
				foreach (Clan clan in Clan.NonBanditFactions)
				{
					if (mapFaction == null || (clan.Kingdom == null && !clan.IsAtWarWith(mapFaction)) || (clan.Kingdom != null && !clan.Kingdom.IsAtWarWith(mapFaction)))
					{
						foreach (WarPartyComponent warPartyComponent2 in clan.WarPartyComponents)
						{
							MobileParty mobileParty3 = warPartyComponent2.MobileParty;
							if (mobileParty3.BesiegedSettlement != settlement && (mobileParty3.DefaultBehavior == AiBehavior.RaidSettlement || mobileParty3.DefaultBehavior == AiBehavior.BesiegeSettlement || mobileParty3.DefaultBehavior == AiBehavior.AssaultSettlement) && mobileParty3.TargetSettlement == settlement)
							{
								Army army = mobileParty3.Army;
								if (army != null)
								{
									army.FinishArmyObjective();
								}
								mobileParty3.SetMoveModeHold();
							}
						}
					}
				}
			}
			CampaignEventDispatcher.Instance.OnSettlementOwnerChanged(settlement, openToClaim, newOwner, oldOwner, capturerHero, detail);
		}

		// Token: 0x06004963 RID: 18787 RVA: 0x00171ACC File Offset: 0x0016FCCC
		public static void ApplyByDefault(Hero hero, Settlement settlement)
		{
			ChangeOwnerOfSettlementAction.ApplyInternal(settlement, hero, null, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.Default);
		}

		// Token: 0x06004964 RID: 18788 RVA: 0x00171AD7 File Offset: 0x0016FCD7
		public static void ApplyByKingDecision(Hero hero, Settlement settlement)
		{
			ChangeOwnerOfSettlementAction.ApplyInternal(settlement, hero, null, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByKingDecision);
			if (settlement.Town != null)
			{
				settlement.Town.IsOwnerUnassigned = false;
			}
		}

		// Token: 0x06004965 RID: 18789 RVA: 0x00171AF6 File Offset: 0x0016FCF6
		public static void ApplyBySiege(Hero newOwner, Hero capturerHero, Settlement settlement)
		{
			if (settlement.Town != null)
			{
				settlement.Town.LastCapturedBy = capturerHero.Clan;
			}
			ChangeOwnerOfSettlementAction.ApplyInternal(settlement, newOwner, capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege);
		}

		// Token: 0x06004966 RID: 18790 RVA: 0x00171B1A File Offset: 0x0016FD1A
		public static void ApplyByLeaveFaction(Hero hero, Settlement settlement)
		{
			ChangeOwnerOfSettlementAction.ApplyInternal(settlement, hero, null, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByLeaveFaction);
		}

		// Token: 0x06004967 RID: 18791 RVA: 0x00171B25 File Offset: 0x0016FD25
		public static void ApplyByBarter(Hero hero, Settlement settlement)
		{
			ChangeOwnerOfSettlementAction.ApplyInternal(settlement, hero, null, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByBarter);
		}

		// Token: 0x06004968 RID: 18792 RVA: 0x00171B30 File Offset: 0x0016FD30
		public static void ApplyByRebellion(Hero hero, Settlement settlement)
		{
			ChangeOwnerOfSettlementAction.ApplyInternal(settlement, hero, hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByRebellion);
		}

		// Token: 0x06004969 RID: 18793 RVA: 0x00171B3B File Offset: 0x0016FD3B
		public static void ApplyByDestroyClan(Settlement settlement, Hero newOwner)
		{
			ChangeOwnerOfSettlementAction.ApplyInternal(settlement, newOwner, null, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByClanDestruction);
		}

		// Token: 0x0600496A RID: 18794 RVA: 0x00171B46 File Offset: 0x0016FD46
		public static void ApplyByGift(Settlement settlement, Hero newOwner)
		{
			ChangeOwnerOfSettlementAction.ApplyInternal(settlement, newOwner, null, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByGift);
		}

		// Token: 0x02000881 RID: 2177
		public enum ChangeOwnerOfSettlementDetail
		{
			// Token: 0x040023FE RID: 9214
			Default,
			// Token: 0x040023FF RID: 9215
			BySiege,
			// Token: 0x04002400 RID: 9216
			ByBarter,
			// Token: 0x04002401 RID: 9217
			ByLeaveFaction,
			// Token: 0x04002402 RID: 9218
			ByKingDecision,
			// Token: 0x04002403 RID: 9219
			ByGift,
			// Token: 0x04002404 RID: 9220
			ByRebellion,
			// Token: 0x04002405 RID: 9221
			ByClanDestruction
		}
	}
}
