using System.Collections.Generic;
using System.Linq;
using Helpers;
using NavalDLC.GameComponents;
using StoryMode;
using StoryMode.StoryModePhases;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.SaveSystem;

namespace NavalDLC.CampaignBehaviors;

public class PiratesCampaignBehavior : CampaignBehaviorBase, IPiratePatrolBehavior
{
	private class PatrolZone
	{
		[SaveableField(0)]
		public readonly CampaignVec2 Position;

		[SaveableField(1)]
		public readonly float Radius;

		public int Density { get; set; }

		public PatrolZone(CampaignVec2 position, float radius)
		{
			Position = position;
			Radius = radius;
			Density = 0;
		}
	}

	private class PiratesCampaignBehaviorSaveDefiner : SaveableTypeDefiner
	{
		public PiratesCampaignBehaviorSaveDefiner()
			: base(2277221)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(PatrolZone), 1);
		}

		protected override void DefineContainerDefinitions()
		{
			ConstructContainerDefinition(typeof(Dictionary<MobileParty, PatrolZone>));
		}
	}

	private const float PirateStartGoldPerBandit = 10f;

	private const float PatrollingScore = 5f;

	private const float DefaultPatrolRadius = 20f;

	private const float WeakPirateRemovalStrengthThreshold = 0.7f;

	private Dictionary<Clan, List<PatrolZone>> _patrolZones = new Dictionary<Clan, List<PatrolZone>>();

	private Dictionary<MobileParty, PatrolZone> _assignedZones = new Dictionary<MobileParty, PatrolZone>();

	public override void RegisterEvents()
	{
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUp);
		CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, DailyTickClan);
		CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, DailyTickParty);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
		CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, AiHourlyTick);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, MapEventEnded);
		TutorialPhase instance = TutorialPhase.Instance;
		if (instance != null && !instance.IsCompleted)
		{
			StoryModeEvents.OnStoryModeTutorialEndedEvent.AddNonSerializedListener(this, OnTutorialEnded);
		}
	}

	private void MapEventEnded(MapEvent mapEvent)
	{
		foreach (PartyBase involvedParty in mapEvent.InvolvedParties)
		{
			if (involvedParty.IsMobile && mapEvent.WinningSide == involvedParty.Side && IsPirateParty(involvedParty.MobileParty) && involvedParty.MobileParty.Ships.Count > Campaign.Current.Models.PartyShipLimitModel.GetIdealShipNumber(involvedParty.MobileParty))
			{
				DiscardShips(involvedParty.MobileParty);
			}
		}
	}

	private void DiscardShips(MobileParty pirateParty)
	{
		MBList<Ship> mBList = pirateParty.Ships.OrderByDescending((Ship x) => Campaign.Current.Models.PartyShipLimitModel.GetShipPriority(pirateParty, x, isSelling: false)).ToMBList();
		int num = 0;
		int num2 = 0;
		for (int num3 = 0; num3 < mBList.Count; num3++)
		{
			Ship ship = mBList[num3];
			ShipHull.ShipType type = ship.ShipHull.Type;
			if (num2 < 2 && (type != ShipHull.ShipType.Medium || num2 == 0 || num < 2) && num < Campaign.Current.Models.PartyShipLimitModel.GetIdealShipNumber(pirateParty))
			{
				num++;
				if (type == ShipHull.ShipType.Medium)
				{
					num2++;
				}
			}
			else
			{
				DestroyShipAction.ApplyByDiscard(ship);
			}
		}
	}

	private void OnTutorialEnded()
	{
		if (TutorialPhase.Instance.IsSkipped)
		{
			for (int i = 0; i < 3; i++)
			{
				foreach (Clan banditFaction in Clan.BanditFactions)
				{
					DailyTickClan(banditFaction);
				}
			}
		}
		StoryModeEvents.OnStoryModeTutorialEndedEvent.ClearListeners(this);
	}

	private void OnMobilePartyDestroyed(MobileParty party, PartyBase destroyerParty)
	{
		if (_assignedZones.TryGetValue(party, out var value))
		{
			UnassignPartyFromZone(value, party);
		}
	}

	private void AiHourlyTick(MobileParty mobileParty, PartyThinkParams p)
	{
		if (IsPirateParty(mobileParty))
		{
			PatrolZone assignedZone = GetAssignedZone(mobileParty);
			if (assignedZone != null)
			{
				MobileParty.NavigationType navigationType = MobileParty.NavigationType.Naval;
				AIBehaviorData item = new AIBehaviorData(assignedZone.Position, AiBehavior.PatrolAroundPoint, navigationType, willGatherArmy: false, isFromPort: false, isTargetingPort: false);
				p.AddBehaviorScore((item, 5f));
			}
			else
			{
				Debug.FailedAssert("This should only be possible for cheats & mods.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\CampaignBehaviors\\PiratesCampaignBehavior.cs", "AiHourlyTick", 248);
			}
		}
	}

	private void OnNewGameCreated(CampaignGameStarter starter)
	{
		AssignPatrolZones();
	}

	private void AssignPatrolZones()
	{
		foreach (Clan banditFaction in Clan.BanditFactions)
		{
			if (IsPirateClan(banditFaction))
			{
				AssignPatrolZones(banditFaction);
			}
		}
	}

	private void OnGameLoaded(CampaignGameStarter starter)
	{
		AssignPatrolZones();
		AdjustAssignedPatrolZones();
	}

	private void OnNewGameCreatedPartialFollowUp(CampaignGameStarter starter, int index)
	{
		if (index % 33 != 0)
		{
			return;
		}
		foreach (Clan banditFaction in Clan.BanditFactions)
		{
			DailyTickClan(banditFaction);
		}
	}

	private void DailyTickClan(Clan clan)
	{
		if (IsPirateClan(clan))
		{
			TrySpawnPirateParties(clan);
		}
	}

	private void DailyTickParty(MobileParty mobileParty)
	{
		if (IsPirateParty(mobileParty))
		{
			TryRemoveWeakPirate(mobileParty);
		}
	}

	private void TryRemoveWeakPirate(MobileParty pirateParty)
	{
		if (!pirateParty.HasNavalNavigationCapability || pirateParty.MapEvent != null)
		{
			return;
		}
		bool num = (float)pirateParty.MemberRoster.TotalHealthyCount < (float)pirateParty.ActualClan.DefaultPartyTemplate.GetLowerTroopLimit() * 0.7f || (float)pirateParty.Ships.Count < (float)pirateParty.ActualClan.DefaultPartyTemplate.ShipHulls.SumQ((ShipTemplateStack t) => t.MinValue) * 0.7f;
		float num2 = MobileParty.MainParty.SeeingRange * 2f;
		if (num && pirateParty.Position.DistanceSquared(MobileParty.MainParty.Position) >= num2 * num2)
		{
			DestroyPartyAction.ApplyForDisbanding(pirateParty, Settlement.FindFirst((Settlement t) => t.IsHideout));
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_assignedZones", ref _assignedZones);
	}

	private void AssignPatrolZones(Clan clan)
	{
		_patrolZones[clan] = new List<PatrolZone>();
		foreach (var spawnPoint in NavalDLCManager.Instance.NavalMapSceneWrapper.GetSpawnPoints(clan.StringId))
		{
			if (!FindIdenticalZone(spawnPoint.Item1, spawnPoint.Item2, out var zone))
			{
				zone = new PatrolZone(spawnPoint.Item1, spawnPoint.Item2);
			}
			_patrolZones[clan].Add(zone);
		}
	}

	private bool FindIdenticalZone(CampaignVec2 position, float radius, out PatrolZone zone)
	{
		zone = null;
		foreach (KeyValuePair<MobileParty, PatrolZone> assignedZone in _assignedZones)
		{
			if (assignedZone.Value.Position.Distance(position).ApproximatelyEqualsTo(0f) && assignedZone.Value.Radius.ApproximatelyEqualsTo(radius))
			{
				zone = assignedZone.Value;
				return true;
			}
		}
		return false;
	}

	private PatrolZone GetClosestPatrolZone(MobileParty mobileParty)
	{
		List<PatrolZone> list = _patrolZones[mobileParty.ActualClan];
		PatrolZone result = null;
		float num = float.MaxValue;
		foreach (PatrolZone item in list)
		{
			float num2 = mobileParty.TargetPosition.Distance(item.Position);
			if (num2 < num && CanSpawnPiratePartyInZone(mobileParty.ActualClan, item))
			{
				num = num2;
				result = item;
			}
		}
		return result;
	}

	private void AdjustAssignedPatrolZones()
	{
		foreach (MobileParty allBanditParty in MobileParty.AllBanditParties)
		{
			if (IsPirateParty(allBanditParty) && GetAssignedZone(allBanditParty) == null)
			{
				PatrolZone patrolZone = GetClosestPatrolZone(allBanditParty);
				if (patrolZone == null)
				{
					Debug.FailedAssert("zone != null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\CampaignBehaviors\\PiratesCampaignBehavior.cs", "AdjustAssignedPatrolZones", 391);
					List<PatrolZone> patrolZones = GetPatrolZones(allBanditParty.ActualClan);
					patrolZone = ((patrolZones.Count != 0) ? patrolZones.GetRandomElement() : new PatrolZone(allBanditParty.TargetPosition, 20f));
				}
				AssignPartyToZone(patrolZone, allBanditParty);
				if (allBanditParty.MapEvent == null)
				{
					allBanditParty.SetMovePatrolAroundPoint(patrolZone.Position, MobileParty.NavigationType.Naval);
				}
			}
		}
		foreach (KeyValuePair<MobileParty, PatrolZone> assignedZone in _assignedZones)
		{
			assignedZone.Value.Density++;
		}
	}

	private bool IsPirateParty(MobileParty mobileParty)
	{
		if (mobileParty.ActualClan != null && IsPirateClan(mobileParty.ActualClan) && !mobileParty.IsCurrentlyUsedByAQuest && mobileParty.HasNavalNavigationCapability)
		{
			return mobileParty.IsCurrentlyAtSea;
		}
		return false;
	}

	private List<PatrolZone> GetPatrolZones(Clan clan)
	{
		return _patrolZones[clan];
	}

	private List<PatrolZone> GetAllPatrolZones()
	{
		List<PatrolZone> list = new List<PatrolZone>();
		foreach (KeyValuePair<Clan, List<PatrolZone>> patrolZone in _patrolZones)
		{
			list.AddRange(patrolZone.Value);
		}
		return list;
	}

	private PatrolZone GetAssignedZone(MobileParty party)
	{
		if (_assignedZones.TryGetValue(party, out var value))
		{
			return value;
		}
		return null;
	}

	private void AssignPartyToZone(PatrolZone zone, MobileParty mobileParty)
	{
		if (_assignedZones.TryGetValue(mobileParty, out var value))
		{
			UnassignPartyFromZone(value, mobileParty);
		}
		zone.Density++;
		_assignedZones[mobileParty] = zone;
	}

	private void UnassignPartyFromZone(PatrolZone assignedZone, MobileParty mobileParty)
	{
		assignedZone.Density--;
		_assignedZones.Remove(mobileParty);
	}

	private void TrySpawnPirateParties(Clan clan)
	{
		GetPirateData(clan, out var pirateMemberCount, out var maximumPirateCount);
		int num = MathF.Floor(MathF.Pow(maximumPirateCount - pirateMemberCount, 0.66f));
		if (num <= 0)
		{
			return;
		}
		int num2 = 0;
		List<PatrolZone> patrolZones = GetPatrolZones(clan);
		patrolZones.Shuffle();
		int iter = 0;
		int bestScore = 0;
		for (int i = 0; i < num; i++)
		{
			PatrolZone randomSuitableZone = GetRandomSuitableZone(clan, patrolZones, ref iter, ref bestScore);
			if (randomSuitableZone != null)
			{
				SpawnPirateParty(clan, randomSuitableZone);
				num2++;
				continue;
			}
			break;
		}
	}

	private void GetPirateData(Clan clan, out int pirateMemberCount, out int maximumPirateCount)
	{
		pirateMemberCount = clan.WarPartyComponents.Count;
		maximumPirateCount = Campaign.Current.Models.BanditDensityModel.GetMaxSupportedNumberOfLootersForClan(clan);
	}

	private void SpawnPirateParty(Clan clan, PatrolZone patrolZone)
	{
		Settlement relatedSettlement = SettlementHelper.FindNearestSettlementToPoint(in patrolZone.Position, (Settlement x) => x.IsTown && x.HasPort);
		CampaignVec2 spawnPosition = GetSpawnPosition(patrolZone);
		MobileParty mobileParty = BanditPartyComponent.CreateLooterParty(clan.StringId + "_1", clan, relatedSettlement, isBossParty: false, clan.DefaultPartyTemplate, spawnPosition);
		InitializePirateParty(mobileParty, clan);
		AssignPartyToZone(patrolZone, mobileParty);
		mobileParty.SetMovePatrolAroundPoint(patrolZone.Position, MobileParty.NavigationType.Naval);
	}

	private CampaignVec2 GetSpawnPosition(PatrolZone zone)
	{
		if (Campaign.Current.GameStarted && IsPointVisibleToPlayer(zone))
		{
			float num = float.MaxValue;
			PatrolZone patrolZone = null;
			foreach (PatrolZone allPatrolZone in GetAllPatrolZones())
			{
				float zoneDistance = GetZoneDistance(zone, allPatrolZone);
				if (!IsPointVisibleToPlayer(allPatrolZone) && (zoneDistance < num || patrolZone == null))
				{
					patrolZone = allPatrolZone;
					num = zoneDistance;
				}
			}
			if (patrolZone != null)
			{
				zone = patrolZone;
			}
		}
		int num2 = 0;
		CampaignVec2 campaignVec = NavigationHelper.FindPointAroundPosition(zone.Position, MobileParty.NavigationType.Naval, zone.Radius);
		do
		{
			campaignVec = NavigationHelper.FindPointAroundPosition(zone.Position, MobileParty.NavigationType.Naval, zone.Radius);
			num2++;
		}
		while (num2 < 100 && Campaign.Current.Models.BanditDensityModel.IsPositionInsideNavalSafeZone(campaignVec));
		return campaignVec;
	}

	private float GetZoneDistance(PatrolZone p1, PatrolZone p2)
	{
		int[] invalidTerrainTypesForNavigationType = Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(MobileParty.NavigationType.Naval);
		Campaign.Current.MapSceneWrapper.GetPathDistanceBetweenAIFaces(p1.Position.Face, p2.Position.Face, p1.Position.ToVec2(), p2.Position.ToVec2(), 0.5f, Campaign.PathFindingMaxCostLimit, out var distance, invalidTerrainTypesForNavigationType, Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromLandToSea, Campaign.Current.Models.MapDistanceModel.RegionSwitchCostFromSeaToLand);
		return distance;
	}

	private PatrolZone GetRandomSuitableZone(Clan clan, List<PatrolZone> zones, ref int iter, ref int bestScore)
	{
		int num = iter + zones.Count;
		int num2 = -1;
		PatrolZone result = null;
		while (iter < num)
		{
			PatrolZone patrolZone = zones[iter % zones.Count];
			iter++;
			if (CanSpawnPiratePartyInZone(clan, patrolZone))
			{
				if (bestScore == patrolZone.Density)
				{
					return patrolZone;
				}
				if (num2 < patrolZone.Density)
				{
					num2 = patrolZone.Density;
					result = patrolZone;
				}
			}
		}
		iter = 0;
		bestScore = num2;
		return result;
	}

	private bool CanSpawnPiratePartyInZone(Clan clan, PatrolZone zone)
	{
		if (_patrolZones.TryGetValue(clan, out var value))
		{
			return value.Contains(zone);
		}
		return false;
	}

	private bool IsPirateClan(Clan clan)
	{
		if (!clan.Culture.CanHaveSettlement && clan.HasNavalNavigationCapability)
		{
			return clan.IsBanditFaction;
		}
		return false;
	}

	private void InitializePirateParty(MobileParty pirateParty, Clan faction)
	{
		pirateParty.Party.SetVisualAsDirty();
		pirateParty.ActualClan = faction;
		pirateParty.Aggressiveness = 1f - 0.2f * MBRandom.RandomFloat;
		CreatePartyTrade(pirateParty);
		GiveFoodToBanditParty(pirateParty);
		pirateParty.SetLandNavigationAccess(access: false);
	}

	private bool IsPointVisibleToPlayer(PatrolZone zone)
	{
		float seeingRange = MobileParty.MainParty.SeeingRange;
		return MobileParty.MainParty.Position.DistanceSquared(zone.Position) < (seeingRange + zone.Radius) * (seeingRange + zone.Radius);
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

	public float GetPatrolRadius(MobileParty mobileParty)
	{
		if (_assignedZones.TryGetValue(mobileParty, out var value))
		{
			return value.Radius;
		}
		return 25f;
	}
}
