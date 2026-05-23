using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class DesertersCampaignBehavior : CampaignBehaviorBase
{
	public const int MinimumDeserterPartyCount = 15;

	public const int MaximumDeserterPartyCount = 40;

	private const int MaxDeserterPartyCountAfterBattle = 3;

	private const int MaxDeserterPartyCountAfterArmyBattle = 5;

	private Clan _deserterClan;

	public static int MergePartiesMaxSize => 120;

	private float DesertersSpawnRadiusAroundVillages => 0.2f * Campaign.Current.EstimatedAverageBanditPartySpeed * (float)CampaignTime.HoursInDay;

	private Clan DeserterClan
	{
		get
		{
			if (_deserterClan == null)
			{
				_deserterClan = Clan.FindFirst((Clan x) => x.StringId == "deserters");
			}
			return _deserterClan;
		}
	}

	public override void RegisterEvents()
	{
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, MapEventEnded);
		CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, HourlyTickParty);
	}

	private void HourlyTickParty(MobileParty party)
	{
		if (!IsDeserterParty(party) || !CanPartyMerge(party) || party.MemberRoster.TotalRegulars >= MergePartiesMaxSize)
		{
			return;
		}
		LocatableSearchData<MobileParty> data = MobileParty.StartFindingLocatablesAroundPosition(party.Position.ToVec2(), GetMergeDistance(party));
		for (MobileParty mobileParty = MobileParty.FindNextLocatable(ref data); mobileParty != null; mobileParty = MobileParty.FindNextLocatable(ref data))
		{
			if (IsDeserterParty(mobileParty) && mobileParty != party && CanPartyMerge(mobileParty) && mobileParty.MemberRoster.TotalRegulars + party.MemberRoster.TotalRegulars <= MergePartiesMaxSize && MBRandom.RandomFloat < 0.05f)
			{
				MergeParties(party, mobileParty);
				break;
			}
		}
	}

	private bool CanPartyMerge(MobileParty mobileParty)
	{
		if (mobileParty.IsActive && mobileParty.MapEvent == null && !mobileParty.IsCurrentlyUsedByAQuest && !mobileParty.IsCurrentlyEngagingParty)
		{
			return !mobileParty.IsFleeing();
		}
		return false;
	}

	private void MergeParties(MobileParty party, MobileParty nearbyParty)
	{
		Debug.Print($"Deserter parties {party.StringId} of {party.MemberRoster.TotalManCount} and {nearbyParty.StringId} of {nearbyParty.MemberRoster.TotalManCount} merged.");
		party.MemberRoster.Add(nearbyParty.MemberRoster);
		foreach (TroopRosterElement item in nearbyParty.PrisonRoster.GetTroopRoster())
		{
			if (item.Character.HeroObject != null)
			{
				TransferPrisonerAction.Apply(item.Character, nearbyParty.Party, party.Party);
			}
		}
		if (party.PrisonRoster.Count > 0)
		{
			party.PrisonRoster.Add(nearbyParty.PrisonRoster);
		}
		party.PartyTradeGold += nearbyParty.PartyTradeGold;
		party.ItemRoster.Add(nearbyParty.ItemRoster);
		DestroyPartyAction.Apply(null, nearbyParty);
		PartyBaseHelper.SortRoster(party);
	}

	private void MapEventEnded(MapEvent mapEvent)
	{
		if (mapEvent.IsNavalMapEvent || (!mapEvent.IsFieldBattle && !mapEvent.IsSiegeAssault && !mapEvent.IsSiegeOutside && !mapEvent.IsSallyOut) || !mapEvent.HasWinner || DeserterClan == null || DeserterClan.WarPartyComponents.Count >= Campaign.Current.Models.BanditDensityModel.GetMaxSupportedNumberOfLootersForClan(DeserterClan))
		{
			return;
		}
		MapEventSide mapEventSide = mapEvent.GetMapEventSide(mapEvent.DefeatedSide);
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		foreach (MapEventParty party in mapEventSide.Parties)
		{
			if (CanPartyGenerateDeserters(party))
			{
				troopRoster.Add(party.RoutedInBattle);
				troopRoster.Add(party.DiedInBattle);
			}
		}
		if (MBRandom.RandomFloat < 0.9f)
		{
			troopRoster.RemoveIf((TroopRosterElement x) => x.Character.IsHero);
			if (troopRoster.TotalManCount >= 15)
			{
				TrySpawnDeserters(mapEvent, troopRoster);
			}
		}
	}

	private bool CanPartyGenerateDeserters(MapEventParty mapEventParty)
	{
		if (mapEventParty.Party.IsMobile && mapEventParty.Party.MobileParty.IsLordParty && mapEventParty.Party.MobileParty.ActualClan != null)
		{
			return !mapEventParty.Party.MobileParty.ActualClan.IsMinorFaction;
		}
		return false;
	}

	private void TrySpawnDeserters(MapEvent mapEvent, TroopRoster routedTroops)
	{
		int maxDeserterPartyCountForMapEvent = GetMaxDeserterPartyCountForMapEvent(mapEvent);
		List<TroopRoster> rostersSuitableForDeserters = GetRostersSuitableForDeserters(routedTroops, maxDeserterPartyCountForMapEvent);
		List<Settlement> list = SelectRandomSettlementsForDeserters(mapEvent, rostersSuitableForDeserters.Count);
		for (int i = 0; i < rostersSuitableForDeserters.Count; i++)
		{
			SpawnDesertersParty(mapEvent, rostersSuitableForDeserters[i], list[i]);
		}
	}

	private int GetMaxDeserterPartyCountForMapEvent(MapEvent mapEvent)
	{
		bool num = mapEvent.AttackerSide.Parties.Any((MapEventParty x) => CanPartyGenerateDeserters(x) && x.Party.MobileParty.Army != null && (x.Party.MobileParty.AttachedTo != null || x.Party.MobileParty.Army.LeaderParty == x.Party.MobileParty));
		bool flag = mapEvent.DefenderSide.Parties.Any((MapEventParty x) => CanPartyGenerateDeserters(x) && x.Party.MobileParty.Army != null && (x.Party.MobileParty.AttachedTo != null || x.Party.MobileParty.Army.LeaderParty == x.Party.MobileParty));
		if (num && flag)
		{
			return 5;
		}
		return 3;
	}

	private List<TroopRoster> GetRostersSuitableForDeserters(TroopRoster routedTroops, int maxPartyCount)
	{
		int totalManCount = routedTroops.TotalManCount;
		int maxSupportedNumberOfLootersForClan = Campaign.Current.Models.BanditDensityModel.GetMaxSupportedNumberOfLootersForClan(DeserterClan);
		int val = Math.Min(maxPartyCount, maxSupportedNumberOfLootersForClan - DeserterClan.WarPartyComponents.Count);
		int val2 = totalManCount / 15;
		int num = Math.Min(val, val2);
		List<TroopRoster> list = new List<TroopRoster>();
		for (int i = 0; i < num; i++)
		{
			list.Add(routedTroops.RemoveNumberOfNonHeroTroopsRandomly(Math.Min(routedTroops.TotalManCount / (num - i), 40)));
		}
		return list;
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void SpawnDesertersParty(MapEvent mapEvent, TroopRoster troops, Settlement settlement)
	{
		CampaignVec2 deserterSpawnPosition = GetDeserterSpawnPosition(settlement);
		MobileParty mobileParty = BanditPartyComponent.CreateLooterParty(DeserterClan.StringId + "_1", DeserterClan, settlement, isBossParty: false, null, deserterSpawnPosition);
		mobileParty.MemberRoster.Add(troops);
		InitializeDeserterParty(mobileParty);
		mobileParty.SetMovePatrolAroundPoint(mobileParty.Position, MobileParty.NavigationType.Default);
		PartyBaseHelper.SortRoster(mobileParty);
	}

	private List<Settlement> SelectRandomSettlementsForDeserters(MapEvent mapEvent, int count)
	{
		List<Settlement> list = FindSettlementsAroundPoint(mapEvent.Position, (Settlement x) => x.IsVillage, MobileParty.NavigationType.Default, GetMaxVillageDistance());
		if (list.Count > count)
		{
			list.Shuffle();
			return list.Take(count).ToList();
		}
		if (list.Count == 0)
		{
			list.Add(SettlementHelper.FindNearestSettlementToPoint(mapEvent.Position, (Settlement x) => x.IsVillage));
		}
		int count2 = list.Count;
		for (int num = 0; num < count - count2; num++)
		{
			list.Add(list[MBRandom.RandomInt(0, count2 - 1)]);
		}
		return list;
	}

	private static List<Settlement> FindSettlementsAroundPoint(in CampaignVec2 point, Func<Settlement, bool> condition, MobileParty.NavigationType navCapabilities, float maxDistance)
	{
		List<Settlement> list = new List<Settlement>();
		foreach (Settlement item in Settlement.All)
		{
			if ((condition == null || condition(item)) && item.Position.Distance(point) < maxDistance)
			{
				list.Add(item);
			}
		}
		return list;
	}

	private float GetMaxVillageDistance()
	{
		return Campaign.Current.EstimatedAverageBanditPartySpeed * (float)CampaignTime.HoursInDay / 2f;
	}

	private CampaignVec2 GetDeserterSpawnPosition(Settlement settlement)
	{
		CampaignVec2 campaignVec = NavigationHelper.FindPointAroundPosition(settlement.GatePosition, MobileParty.NavigationType.Default, DesertersSpawnRadiusAroundVillages);
		float num = MobileParty.MainParty.SeeingRange * MobileParty.MainParty.SeeingRange;
		if (campaignVec.DistanceSquared(MobileParty.MainParty.Position) < num)
		{
			for (int i = 0; i < 15; i++)
			{
				CampaignVec2 campaignVec2 = NavigationHelper.FindReachablePointAroundPosition(campaignVec, MobileParty.NavigationType.Default, DesertersSpawnRadiusAroundVillages);
				if (NavigationHelper.IsPositionValidForNavigationType(campaignVec2, MobileParty.NavigationType.Default))
				{
					float landRatio;
					float num2 = DistanceHelper.FindClosestDistanceFromMobilePartyToPoint(MobileParty.MainParty, campaignVec2, MobileParty.NavigationType.Default, out landRatio);
					if (num2 * num2 > num)
					{
						campaignVec = campaignVec2;
						break;
					}
				}
			}
		}
		return campaignVec;
	}

	private void InitializeDeserterParty(MobileParty banditParty)
	{
		banditParty.Party.SetVisualAsDirty();
		banditParty.ActualClan = DeserterClan;
		banditParty.Aggressiveness = 1f - 0.2f * MBRandom.RandomFloat;
		CreatePartyTrade(banditParty);
		GiveFoodToBanditParty(banditParty);
	}

	private static void CreatePartyTrade(MobileParty banditParty)
	{
		int initialGold = (int)(10f * (float)banditParty.Party.MemberRoster.TotalManCount * (0.5f + 1f * MBRandom.RandomFloat));
		banditParty.InitializePartyTrade(initialGold);
	}

	private void GiveFoodToBanditParty(MobileParty banditParty)
	{
		foreach (ItemObject item in Items.All)
		{
			if (item.IsFood)
			{
				int num = MBRandom.RoundRandomized((float)banditParty.MemberRoster.TotalManCount * (1f / (float)item.Value) * 8f * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat);
				if (num > 0)
				{
					banditParty.ItemRoster.AddToCounts(item, num);
				}
			}
		}
	}

	private float GetMergeDistance(MobileParty mobileParty)
	{
		return mobileParty._lastCalculatedSpeed * 2f;
	}

	private bool IsDeserterParty(MobileParty mobileParty)
	{
		if (mobileParty.ActualClan != null)
		{
			return mobileParty.ActualClan == DeserterClan;
		}
		return false;
	}
}
