using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Actions;

public static class ChangeKingdomAction
{
	public enum ChangeKingdomActionDetail
	{
		JoinAsMercenary,
		JoinKingdom,
		JoinKingdomByDefection,
		LeaveKingdom,
		LeaveWithRebellion,
		LeaveAsMercenary,
		LeaveByClanDestruction,
		CreateKingdom,
		LeaveByKingdomDestruction
	}

	public const float PotentialSettlementsPerNobleEffect = 0.2f;

	public const float NewGainedFiefsValueForKingdomConstant = 0.1f;

	public const float LordsUnitStrengthValue = 20f;

	public const float MercenaryUnitStrengthValue = 5f;

	public const float MinimumNeededGoldForRecruitingMercenaries = 20000f;

	private static void ApplyInternal(Clan clan, Kingdom newKingdom, ChangeKingdomActionDetail detail, CampaignTime shouldStayInKingdomUntil, int awardMultiplier = 0, bool byRebellion = false, bool showNotification = true)
	{
		Kingdom kingdom = clan.Kingdom;
		clan.DebtToKingdom = 0;
		if (detail == ChangeKingdomActionDetail.JoinKingdom || detail == ChangeKingdomActionDetail.JoinAsMercenary || detail == ChangeKingdomActionDetail.JoinKingdomByDefection)
		{
			clan.ShouldStayInKingdomUntil = shouldStayInKingdomUntil;
			FactionHelper.AdjustFactionStancesForClanJoiningKingdom(clan, newKingdom);
		}
		else
		{
			clan.ShouldStayInKingdomUntil = CampaignTime.Zero;
		}
		switch (detail)
		{
		case ChangeKingdomActionDetail.JoinKingdom:
		case ChangeKingdomActionDetail.JoinKingdomByDefection:
		case ChangeKingdomActionDetail.CreateKingdom:
			if (clan.IsUnderMercenaryService)
			{
				EndMercenaryServiceAction.EndByDefault(clan);
			}
			if (kingdom != null)
			{
				clan.ClanLeaveKingdom(!byRebellion);
			}
			if (newKingdom != null && detail == ChangeKingdomActionDetail.CreateKingdom)
			{
				ChangeRulingClanAction.Apply(newKingdom, clan);
			}
			clan.Kingdom = newKingdom;
			break;
		case ChangeKingdomActionDetail.JoinAsMercenary:
			StartMercenaryServiceAction.ApplyByDefault(clan, newKingdom, awardMultiplier);
			break;
		case ChangeKingdomActionDetail.LeaveKingdom:
		case ChangeKingdomActionDetail.LeaveWithRebellion:
		case ChangeKingdomActionDetail.LeaveAsMercenary:
		case ChangeKingdomActionDetail.LeaveByClanDestruction:
		case ChangeKingdomActionDetail.LeaveByKingdomDestruction:
		{
			clan.Kingdom = null;
			bool flag = false;
			if (clan.IsUnderMercenaryService)
			{
				flag = true;
				EndMercenaryServiceAction.EndByLeavingKingdom(clan);
			}
			switch (detail)
			{
			case ChangeKingdomActionDetail.LeaveWithRebellion:
				DeclareWarAction.ApplyByRebellion(kingdom, clan);
				foreach (IFaction item in kingdom.FactionsAtWarWith)
				{
					if (item != clan && !clan.IsAtWarWith(item))
					{
						DeclareWarAction.ApplyByDefault(clan, item);
					}
				}
				break;
			case ChangeKingdomActionDetail.LeaveKingdom:
				foreach (Settlement item2 in new List<Settlement>(clan.Settlements))
				{
					ChangeOwnerOfSettlementAction.ApplyByLeaveFaction(kingdom.Leader, item2);
					foreach (Hero item3 in new List<Hero>(item2.HeroesWithoutParty))
					{
						if (item3.CurrentSettlement != null && item3.Clan == clan)
						{
							if (item3.PartyBelongedTo != null)
							{
								LeaveSettlementAction.ApplyForParty(item3.PartyBelongedTo);
								EnterSettlementAction.ApplyForParty(item3.PartyBelongedTo, clan.Leader.HomeSettlement);
							}
							else
							{
								LeaveSettlementAction.ApplyForCharacterOnly(item3);
								EnterSettlementAction.ApplyForCharacterOnly(item3, clan.Leader.HomeSettlement);
							}
						}
					}
				}
				break;
			case ChangeKingdomActionDetail.LeaveByKingdomDestruction:
				if (flag)
				{
					foreach (IFaction item4 in kingdom.FactionsAtWarWith)
					{
						if (clan != item4 && !Campaign.Current.Models.DiplomacyModel.IsAtConstantWar(clan, item4))
						{
							MakePeaceAction.Apply(clan, item4);
						}
					}
					break;
				}
				foreach (IFaction item5 in kingdom.FactionsAtWarWith)
				{
					if (clan != item5 && !clan.GetStanceWith(item5).IsAtWar)
					{
						DeclareWarAction.ApplyByDefault(clan, item5);
					}
				}
				break;
			}
			break;
		}
		}
		if (detail == ChangeKingdomActionDetail.LeaveAsMercenary || detail == ChangeKingdomActionDetail.LeaveKingdom)
		{
			foreach (IFaction item6 in clan.FactionsAtWarWith.ToList())
			{
				if (clan != item6 && !Campaign.Current.Models.DiplomacyModel.IsAtConstantWar(clan, item6))
				{
					MakePeaceAction.Apply(clan, item6);
					FactionHelper.FinishAllRelatedHostileActionsOfFactionToFaction(clan, item6);
					FactionHelper.FinishAllRelatedHostileActionsOfFactionToFaction(item6, clan);
				}
			}
			CheckIfPartyIconIsDirty(clan, kingdom);
		}
		foreach (WarPartyComponent warPartyComponent in clan.WarPartyComponents)
		{
			if (warPartyComponent.MobileParty.MapEvent == null)
			{
				warPartyComponent.MobileParty.SetMoveModeHold();
			}
		}
		CampaignEventDispatcher.Instance.OnClanChangedKingdom(clan, kingdom, newKingdom, detail, showNotification);
	}

	public static void ApplyByJoinToKingdom(Clan clan, Kingdom newKingdom, CampaignTime shouldStayInKingdomUntil = default(CampaignTime), bool showNotification = true)
	{
		ApplyInternal(clan, newKingdom, ChangeKingdomActionDetail.JoinKingdom, shouldStayInKingdomUntil, 0, byRebellion: false, showNotification);
	}

	public static void ApplyByJoinToKingdomByDefection(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, CampaignTime shouldStayInKingdomUntil = default(CampaignTime), bool showNotification = true)
	{
		ApplyInternal(clan, newKingdom, ChangeKingdomActionDetail.JoinKingdomByDefection, shouldStayInKingdomUntil, 0, byRebellion: false, showNotification);
		CampaignEventDispatcher.Instance.OnClanDefected(clan, oldKingdom, newKingdom);
	}

	public static void ApplyByCreateKingdom(Clan clan, Kingdom newKingdom, bool showNotification = true)
	{
		ApplyInternal(clan, newKingdom, ChangeKingdomActionDetail.CreateKingdom, CampaignTime.Zero, 0, byRebellion: false, showNotification);
	}

	public static void ApplyByLeaveByKingdomDestruction(Clan clan, bool showNotification = true)
	{
		ApplyInternal(clan, null, ChangeKingdomActionDetail.LeaveByKingdomDestruction, CampaignTime.Zero, 0, byRebellion: false, showNotification);
	}

	public static void ApplyByLeaveKingdom(Clan clan, bool showNotification = true)
	{
		ApplyInternal(clan, null, ChangeKingdomActionDetail.LeaveKingdom, CampaignTime.Zero, 0, byRebellion: false, showNotification);
	}

	public static void ApplyByLeaveWithRebellionAgainstKingdom(Clan clan, bool showNotification = true)
	{
		ApplyInternal(clan, null, ChangeKingdomActionDetail.LeaveWithRebellion, CampaignTime.Zero, 0, byRebellion: false, showNotification);
	}

	public static void ApplyByJoinFactionAsMercenary(Clan clan, Kingdom newKingdom, CampaignTime shouldStayInKingdomUntil = default(CampaignTime), int awardMultiplier = 50, bool showNotification = true)
	{
		ApplyInternal(clan, newKingdom, ChangeKingdomActionDetail.JoinAsMercenary, shouldStayInKingdomUntil, awardMultiplier, byRebellion: false, showNotification);
	}

	public static void ApplyByLeaveKingdomAsMercenary(Clan mercenaryClan, bool showNotification = true)
	{
		ApplyInternal(mercenaryClan, null, ChangeKingdomActionDetail.LeaveAsMercenary, CampaignTime.Zero, 0, byRebellion: false, showNotification);
	}

	public static void ApplyByLeaveKingdomByClanDestruction(Clan clan, bool showNotification = true)
	{
		ApplyInternal(clan, null, ChangeKingdomActionDetail.LeaveByClanDestruction, CampaignTime.Zero, 0, byRebellion: false, showNotification);
	}

	private static void CheckIfPartyIconIsDirty(Clan clan, Kingdom oldKingdom)
	{
		IFaction faction2;
		IFaction faction;
		if (clan.Kingdom == null)
		{
			faction = clan;
			faction2 = faction;
		}
		else
		{
			faction = clan.Kingdom;
			faction2 = faction;
		}
		IFaction faction3 = faction2;
		faction = oldKingdom;
		IFaction faction4 = faction ?? clan;
		foreach (MobileParty item in MobileParty.All)
		{
			if (item.IsVisible && ((item.Party.Owner != null && item.Party.Owner.Clan == clan) || (clan == Clan.PlayerClan && ((!FactionManager.IsAtWarAgainstFaction(item.MapFaction, faction3) && FactionManager.IsAtWarAgainstFaction(item.MapFaction, faction4)) || (FactionManager.IsAtWarAgainstFaction(item.MapFaction, faction3) && !FactionManager.IsAtWarAgainstFaction(item.MapFaction, faction4))))))
			{
				item.Party.SetVisualAsDirty();
			}
		}
		foreach (Settlement settlement in clan.Settlements)
		{
			settlement.Party.SetVisualAsDirty();
		}
	}
}
