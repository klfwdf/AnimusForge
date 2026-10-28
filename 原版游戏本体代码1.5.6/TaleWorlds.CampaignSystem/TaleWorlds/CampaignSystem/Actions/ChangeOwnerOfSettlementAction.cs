using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Actions;

public static class ChangeOwnerOfSettlementAction
{
	public enum ChangeOwnerOfSettlementDetail
	{
		Default,
		BySiege,
		ByBarter,
		ByLeaveFaction,
		ByKingDecision,
		ByGift,
		ByRebellion,
		ByClanDestruction
	}

	private static void ApplyInternal(Settlement settlement, Hero newOwner, Hero capturerHero, ChangeOwnerOfSettlementDetail detail)
	{
		Hero oldOwner = settlement.OwnerClan?.Leader;
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
			if (detail == ChangeOwnerOfSettlementDetail.BySiege && settlement.Town.GarrisonParty != null)
			{
				DestroyPartyAction.Apply(capturerHero.PartyBelongedTo.Party, settlement.Town.GarrisonParty);
			}
			if (settlement.Town.GarrisonParty == null)
			{
				settlement.AddGarrisonParty();
			}
			ChangeGovernorAction.RemoveGovernorOfIfExists(settlement.Town);
		}
		settlement.Party.SetVisualAsDirty();
		foreach (Village boundVillage in settlement.BoundVillages)
		{
			boundVillage.Settlement.Party.SetVisualAsDirty();
			if (boundVillage.VillagerPartyComponent == null || newOwner == null)
			{
				continue;
			}
			foreach (MobileParty item in MobileParty.All)
			{
				if (item.MapEvent == null && item != MobileParty.MainParty && item.ShortTermTargetParty == boundVillage.VillagerPartyComponent.MobileParty && !item.MapFaction.IsAtWarWith(newOwner.MapFaction))
				{
					item.SetMoveModeHold();
				}
			}
		}
		bool openToClaim = (detail == ChangeOwnerOfSettlementDetail.BySiege || detail == ChangeOwnerOfSettlementDetail.ByClanDestruction || detail == ChangeOwnerOfSettlementDetail.ByLeaveFaction) && settlement.IsFortification;
		if (newOwner != null)
		{
			IFaction mapFaction = newOwner.MapFaction;
			if (settlement.Party.MapEvent != null && !settlement.Party.MapEvent.AttackerSide.LeaderParty.MapFaction.IsAtWarWith(mapFaction) && settlement.Party.MapEvent.Winner == null)
			{
				settlement.Party.MapEvent.DiplomaticallyFinished = true;
				foreach (WarPartyComponent warPartyComponent in settlement.MapFaction.WarPartyComponents)
				{
					MobileParty mobileParty = warPartyComponent.MobileParty;
					if (mobileParty.DefaultBehavior == AiBehavior.DefendSettlement && mobileParty.TargetSettlement == settlement && mobileParty.CurrentSettlement == null)
					{
						mobileParty.SetMoveModeHold();
					}
				}
				settlement.Party.MapEvent.Update();
			}
			foreach (Clan nonBanditFaction in Clan.NonBanditFactions)
			{
				if (mapFaction != null && (nonBanditFaction.Kingdom != null || nonBanditFaction.IsAtWarWith(mapFaction)) && (nonBanditFaction.Kingdom == null || nonBanditFaction.Kingdom.IsAtWarWith(mapFaction)))
				{
					continue;
				}
				foreach (WarPartyComponent warPartyComponent2 in nonBanditFaction.WarPartyComponents)
				{
					MobileParty mobileParty2 = warPartyComponent2.MobileParty;
					if (mobileParty2.BesiegedSettlement != settlement && (mobileParty2.DefaultBehavior == AiBehavior.RaidSettlement || mobileParty2.DefaultBehavior == AiBehavior.BesiegeSettlement || mobileParty2.DefaultBehavior == AiBehavior.AssaultSettlement) && mobileParty2.TargetSettlement == settlement)
					{
						mobileParty2.Army?.FinishArmyObjective();
						mobileParty2.SetMoveModeHold();
					}
				}
			}
		}
		CampaignEventDispatcher.Instance.OnSettlementOwnerChanged(settlement, openToClaim, newOwner, oldOwner, capturerHero, detail);
	}

	public static void ApplyByDefault(Hero hero, Settlement settlement)
	{
		ApplyInternal(settlement, hero, null, ChangeOwnerOfSettlementDetail.Default);
	}

	public static void ApplyByKingDecision(Hero hero, Settlement settlement)
	{
		ApplyInternal(settlement, hero, null, ChangeOwnerOfSettlementDetail.ByKingDecision);
		if (settlement.Town != null)
		{
			settlement.Town.IsOwnerUnassigned = false;
		}
	}

	public static void ApplyBySiege(Hero newOwner, Hero capturerHero, Settlement settlement)
	{
		if (settlement.Town != null)
		{
			settlement.Town.LastCapturedBy = capturerHero.Clan;
		}
		ApplyInternal(settlement, newOwner, capturerHero, ChangeOwnerOfSettlementDetail.BySiege);
	}

	public static void ApplyByLeaveFaction(Hero hero, Settlement settlement)
	{
		ApplyInternal(settlement, hero, null, ChangeOwnerOfSettlementDetail.ByLeaveFaction);
	}

	public static void ApplyByBarter(Hero hero, Settlement settlement)
	{
		ApplyInternal(settlement, hero, null, ChangeOwnerOfSettlementDetail.ByBarter);
	}

	public static void ApplyByRebellion(Hero hero, Settlement settlement)
	{
		ApplyInternal(settlement, hero, hero, ChangeOwnerOfSettlementDetail.ByRebellion);
	}

	public static void ApplyByDestroyClan(Settlement settlement, Hero newOwner)
	{
		ApplyInternal(settlement, newOwner, null, ChangeOwnerOfSettlementDetail.ByClanDestruction);
	}

	public static void ApplyByGift(Settlement settlement, Hero newOwner)
	{
		ApplyInternal(settlement, newOwner, null, ChangeOwnerOfSettlementDetail.ByGift);
	}
}
