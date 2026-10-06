using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NavalDLC.CampaignBehaviors;

public class NavalKingdomPolicyCampaignBehaviour : CampaignBehaviorBase
{
	private const float KingsPardonForPiratesSearchRadius = 50f;

	private const float KingsPardonForPiratesArriveDistance = 5f;

	private const float KingsPardonDailyCheckChance = 0.05f;

	private const float KingsPardonRecruitPercentage = 0.25f;

	private Dictionary<MobileParty, Settlement> _settlementToSurrenderByParty = new Dictionary<MobileParty, Settlement>();

	public override void RegisterEvents()
	{
		CampaignEvents.OnShipOwnerChangedEvent.AddNonSerializedListener(this, OnShipOwnerChanged);
		CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, OnRaidCompleted);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
		CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, OnHourlyTickParty);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
		CampaignEvents.OnBarterAcceptedEvent.AddNonSerializedListener(this, OnBarterAccepted);
	}

	private void OnBarterAccepted(Hero offererHero, Hero otherHero, List<Barterable> barters)
	{
		Clan clan = offererHero.Clan;
		Kingdom kingdom = clan?.Kingdom;
		if (kingdom == null || kingdom.RulingClan == clan || !kingdom.HasPolicy(NavalPolicies.RoyalRansomClaim))
		{
			return;
		}
		IEnumerable<SetPrisonerFreeBarterable> enumerable = barters.Where((Barterable x) => x is SetPrisonerFreeBarterable).Cast<SetPrisonerFreeBarterable>();
		float num = 0f;
		foreach (SetPrisonerFreeBarterable item in enumerable)
		{
			num = item.GetUnitValueForFaction(otherHero.MapFaction);
		}
		int amount = TaleWorlds.Library.MathF.Round(num * 0.15f);
		GiveGoldAction.ApplyBetweenCharacters(offererHero, kingdom.Leader, amount);
	}

	private void OnDailyTick()
	{
		foreach (Kingdom item in Kingdom.All)
		{
			if (!item.HasPolicy(NavalPolicies.KingsPardonForPirates))
			{
				continue;
			}
			foreach (Settlement settlement in item.Settlements)
			{
				if (MBRandom.RandomFloat <= 0.05f && settlement.IsTown && settlement.HasPort)
				{
					MobileParty availableNearbyPirateParty = GetAvailableNearbyPirateParty(settlement);
					if (availableNearbyPirateParty != null)
					{
						availableNearbyPirateParty.SetMoveGoToPoint(settlement.PortPosition, MobileParty.NavigationType.Naval);
						availableNearbyPirateParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
						_settlementToSurrenderByParty.Add(availableNearbyPirateParty, settlement);
					}
				}
			}
		}
	}

	private void OnHourlyTickParty(MobileParty party)
	{
		if (!_settlementToSurrenderByParty.TryGetValue(party, out var value) || !(Campaign.Current.Models.MapDistanceModel.GetDistance(party, value, isTargetingPort: true, MobileParty.NavigationType.Naval, out var _) <= 5f))
		{
			return;
		}
		MobileParty garrisonParty = value.Town.GarrisonParty;
		if (garrisonParty != null)
		{
			foreach (TroopRosterElement item in party.MemberRoster.GetTroopRoster())
			{
				int num = Math.Min(garrisonParty.GetAvailableWageBudget() / Campaign.Current.Models.PartyWageModel.GetCharacterWage(item.Character), TaleWorlds.Library.MathF.Round((float)item.Number * 0.25f));
				if (num > 0)
				{
					garrisonParty.MemberRoster.AddToCounts(item.Character, num);
				}
			}
		}
		value.Town.Security -= 5f;
		for (int num2 = party.Ships.Count - 1; num2 >= 0; num2--)
		{
			ChangeShipOwnerAction.ApplyByTransferring(value.Party, party.Ships[num2]);
		}
		if (party.MapEvent != null)
		{
			party.MapEventSide = null;
		}
		DestroyPartyAction.Apply(null, party);
	}

	private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
	{
		_settlementToSurrenderByParty.Remove(mobileParty);
	}

	private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		KeyValuePair<MobileParty, Settlement> keyValuePair = _settlementToSurrenderByParty.FirstOrDefault((KeyValuePair<MobileParty, Settlement> x) => x.Value == settlement);
		Kingdom kingdom = newOwner.Clan?.Kingdom;
		MobileParty key = keyValuePair.Key;
		if (key != null && (kingdom == null || !kingdom.HasPolicy(NavalPolicies.KingsPardonForPirates)))
		{
			if (key.MapEvent != null)
			{
				key.MapEventSide = null;
			}
			DestroyPartyAction.Apply(null, key);
		}
	}

	private void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEvent)
	{
		if (winnerSide != BattleSideEnum.Attacker)
		{
			return;
		}
		Kingdom kingdom = (raidEvent.AttackerSide.LeaderParty.Owner?.Clan)?.Kingdom;
		if (kingdom == null || !kingdom.HasPolicy(NavalPolicies.RaidersSpoils))
		{
			return;
		}
		foreach (MapEventParty party in raidEvent.AttackerSide.Parties)
		{
			if (party.Party.LeaderHero != null && party.Party.LeaderHero.MapFaction == kingdom)
			{
				GainKingdomInfluenceAction.ApplyForDefault(party.Party.LeaderHero, 5f);
			}
		}
	}

	private void OnShipOwnerChanged(Ship ship, PartyBase oldOwner, ChangeShipOwnerAction.ShipOwnerChangeDetail details)
	{
		if (details == ChangeShipOwnerAction.ShipOwnerChangeDetail.ApplyByTrade && oldOwner.IsSettlement)
		{
			Clan ownerClan = oldOwner.Settlement.OwnerClan;
			Kingdom kingdom = ownerClan?.Kingdom;
			if (kingdom != null && kingdom.RulingClan != ownerClan && kingdom.HasPolicy(NavalPolicies.KingsTitheOnKeels))
			{
				int amount = TaleWorlds.Library.MathF.Round(Campaign.Current.Models.ShipCostModel.GetShipTradeValue(ship, oldOwner, ship.Owner) * 0.15f);
				GiveGoldAction.ApplyForPartyToCharacter(oldOwner, kingdom.Leader, amount);
			}
		}
	}

	private MobileParty GetAvailableNearbyPirateParty(Settlement settlement)
	{
		LocatableSearchData<MobileParty> data = MobileParty.StartFindingLocatablesAroundPosition(settlement.PortPosition.ToVec2(), 50f);
		for (MobileParty mobileParty = MobileParty.FindNextLocatable(ref data); mobileParty != null; mobileParty = MobileParty.FindNextLocatable(ref data))
		{
			if (mobileParty.IsBandit && mobileParty.MapEvent == null && mobileParty.HasNavalNavigationCapability && !_settlementToSurrenderByParty.ContainsKey(mobileParty))
			{
				return mobileParty;
			}
		}
		return null;
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_settlementsToSurrenderByParties", ref _settlementToSurrenderByParty);
	}
}
