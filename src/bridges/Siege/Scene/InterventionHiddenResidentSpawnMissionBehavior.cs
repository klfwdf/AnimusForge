using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AnimusForge.SiegeAftermathIntervention;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// GCCZ-only side-effect bridge for bringing a bounded group of hidden residents into a town scene.
/// Spawning occurs only on an eligible gather action and uses vanilla civilian creators and equipment.
/// </summary>
internal sealed class InterventionHiddenResidentSpawnMissionBehavior : MissionLogic
{
	private static readonly Type TownsfolkBehaviorType = typeof(SandBox.CampaignBehaviors.CommonTownsfolkCampaignBehavior);
	private static readonly BindingFlags CreatorBindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
	private static readonly string[] CommonAdultCreators = { "CreateTownsMan", "CreateTownsWoman" };
	private static readonly string[] CommonSpawnTags = { "npc_common", "npc_common_limited" };

	private readonly string _settlementId;
	private readonly TownHiddenResidentSpawnLedger _ledger = new TownHiddenResidentSpawnLedger();
	private readonly HashSet<string> _exhaustedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private int _creatorIndex;

	public InterventionHiddenResidentSpawnMissionBehavior(string settlementId)
	{
		_settlementId = string.IsNullOrWhiteSpace(settlementId) ? "N/A" : settlementId;
	}

	public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

	internal TownHiddenResidentSpawnOutcome TryBringOutHiddenResidents(
		bool operationSnapshotLocked,
		bool destructiveCombatActive,
		string source)
	{
		Mission mission = base.Mission;
		try
		{
			int visibleCivilianCount = CountCurrentCivilianLikeAgents(mission);
			int activeHumanCount = mission?.Agents?.Count(agent => agent != null && agent.IsHuman && agent.IsActive()) ?? 0;
			TownHiddenResidentSpawnPlan plan = _ledger.Plan(
				visibleCivilianCount,
				activeHumanCount,
				SiegeCivilianAssemblyProfile.SceneTotalAgentSoftCap,
				operationSnapshotLocked,
				destructiveCombatActive);
			if (!plan.CanSpawn)
			{
				return new TownHiddenResidentSpawnOutcome(plan.Status, plan.RequestedCount, 0);
			}

			MissionAgentHandler missionAgentHandler = mission?.GetMissionBehavior<MissionAgentHandler>();
			Settlement settlement = PlayerEncounter.LocationEncounter?.Settlement ?? Settlement.CurrentSettlement;
			Location location = CampaignMission.Current?.Location;
			Agent main = Agent.Main ?? mission?.MainAgent;
			if (missionAgentHandler == null
				|| settlement?.Town == null
				|| location == null
				|| main == null
				|| !main.IsActive()
				|| !settlement.IsTown
				|| !string.Equals(location.StringId, SiegeInterventionEntryProfile.SettlementCenterLocationId, StringComparison.OrdinalIgnoreCase))
			{
				return new TownHiddenResidentSpawnOutcome(TownHiddenResidentSpawnStatus.RuntimeUnavailable, plan.RequestedCount, 0);
			}

			if (!TryResolveSafeCorner(mission, main, out Vec3 safeAnchor))
			{
				return new TownHiddenResidentSpawnOutcome(TownHiddenResidentSpawnStatus.NoSafeCorner, plan.RequestedCount, 0);
			}

			int spawned = SpawnResidentGroup(
				mission,
				missionAgentHandler,
				settlement,
				location,
				main,
				safeAnchor,
				plan.RequestedCount);
			TownHiddenResidentSpawnOutcome outcome = _ledger.Record(plan, spawned);
			Logger.Log(
				"SiegeAiIntervention",
				"Hidden resident bring-out completed. Settlement=" + _settlementId
				+ ", Status=" + outcome.Status
				+ ", Requested=" + outcome.RequestedCount
				+ ", Spawned=" + outcome.SpawnedCount
				+ ", SceneSpawnedTotal=" + _ledger.SpawnedCount
				+ ", VisibleBefore=" + visibleCivilianCount
				+ ", Source=" + (source ?? "N/A"));
			return outcome;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "Hidden resident bring-out failed. Settlement=" + _settlementId + ", Source=" + (source ?? "N/A") + ", Error=" + ex.Message);
			return new TownHiddenResidentSpawnOutcome(TownHiddenResidentSpawnStatus.SpawnFailed, 0, 0);
		}
	}

	private int SpawnResidentGroup(
		Mission mission,
		MissionAgentHandler missionAgentHandler,
		Settlement settlement,
		Location location,
		Agent main,
		Vec3 safeAnchor,
		int requestedCount)
	{
		int spawned = 0;
		for (int attempt = 0; attempt < requestedCount; attempt++)
		{
			string spawnTag = ChooseCommonSpawnTag(missionAgentHandler);
			if (string.IsNullOrWhiteSpace(spawnTag))
			{
				break;
			}

			string creatorName = CommonAdultCreators[(_creatorIndex++) % CommonAdultCreators.Length];
			LocationCharacter locationCharacter = CreateNativeTownCivilian(creatorName, settlement.Culture);
			if (locationCharacter == null)
			{
				_exhaustedTags.Add(spawnTag);
				continue;
			}

			ApplySpawnTagOverride(locationCharacter, spawnTag);
			location.AddCharacter(locationCharacter);
			Agent agent = null;
			try
			{
				agent = missionAgentHandler.SpawnDefaultLocationCharacter(locationCharacter, simulateAgentAfterSpawn: true);
			}
			catch (Exception ex)
			{
				Logger.Log("SiegeAiIntervention", "Hidden resident vanilla spawn failed. Settlement=" + _settlementId + ", Creator=" + creatorName + ", Tag=" + spawnTag + ", Error=" + ex.Message);
			}

			if (agent == null)
			{
				location.RemoveLocationCharacter(locationCharacter);
				_exhaustedTags.Add(spawnTag);
				continue;
			}

			Vec3 slot = ResolveGroupSlot(mission, main, safeAnchor, spawned);
			try
			{
				agent.TeleportToPosition(slot);
				agent.ClearTargetFrame();
				agent.SetTargetPosition(slot.AsVec2);
				agent.SetWatchState(Agent.WatchState.Alarmed);
			}
			catch (Exception ex)
			{
				Logger.Log("SiegeAiIntervention", "Hidden resident corner placement failed. Agent=" + agent.Index + ", Error=" + ex.Message);
			}
			spawned++;
		}
		return spawned;
	}

	private static bool TryResolveSafeCorner(Mission mission, Agent main, out Vec3 safeAnchor)
	{
		safeAnchor = Vec3.Zero;
		if (mission?.Scene == null || main == null)
		{
			return false;
		}

		Vec3 forward = main.LookDirection;
		forward.z = 0f;
		if (forward.LengthSquared < 0.01f)
		{
			forward = Vec3.Forward;
		}
		forward.Normalize();
		Vec3 right = Vec3.CrossProduct(forward, Vec3.Up);
		if (right.LengthSquared < 0.01f)
		{
			right = Vec3.Side;
		}
		right.Normalize();

		foreach (TownHiddenResidentSpawnOffset offset in TownHiddenResidentSpawnPositionPolicy.GetCandidateOffsets())
		{
			Vec3 candidate = main.Position + forward * offset.ForwardMeters + right * offset.RightMeters;
			if (TryResolveNavigableCorner(mission, main, forward, candidate, out Vec3 resolved))
			{
				safeAnchor = resolved;
				return true;
			}
		}
		return false;
	}

	private static bool TryResolveNavigableCorner(
		Mission mission,
		Agent main,
		Vec3 playerForward,
		Vec3 candidate,
		out Vec3 resolved)
	{
		resolved = Ground(mission, candidate);
		if (IsSafeNavigablePosition(mission, main, playerForward, resolved))
		{
			return true;
		}

		for (int attempt = 0; attempt < TownHiddenResidentSpawnPositionPolicy.NavmeshSampleAttempts; attempt++)
		{
			Vec3 sample = mission.GetRandomPositionAroundPoint(
				resolved,
				TownHiddenResidentSpawnPositionPolicy.NavmeshSampleMinRadius,
				TownHiddenResidentSpawnPositionPolicy.NavmeshSampleMaxRadius,
				true);
			sample = Ground(mission, sample);
			if (IsSafeNavigablePosition(mission, main, playerForward, sample))
			{
				resolved = sample;
				return true;
			}
		}
		return false;
	}

	private static bool IsSafeNavigablePosition(Mission mission, Agent main, Vec3 playerForward, Vec3 candidate)
	{
		Vec3 offset = candidate - main.Position;
		float minimumDistance = TownHiddenResidentSpawnPositionPolicy.MinimumPlayerDistance;
		if (offset.LengthSquared < minimumDistance * minimumDistance)
		{
			return false;
		}
		float forwardProjection = offset.x * playerForward.x + offset.y * playerForward.y;
		if (forwardProjection >= 0f)
		{
			return false;
		}

		WorldPosition worldPosition = new WorldPosition(mission.Scene, candidate);
		if (worldPosition.GetNearestNavMesh() == UIntPtr.Zero)
		{
			return false;
		}

		float clearance = TownHiddenResidentSpawnPositionPolicy.MinimumExistingAgentDistance;
		float clearanceSquared = clearance * clearance;
		return mission.Agents == null || !mission.Agents.Any(agent =>
			agent != null
			&& agent.IsHuman
			&& agent.IsActive()
			&& agent.Position.DistanceSquared(candidate) < clearanceSquared);
	}

	private static Vec3 ResolveGroupSlot(Mission mission, Agent main, Vec3 anchor, int index)
	{
		Vec3 forward = main.LookDirection;
		forward.z = 0f;
		if (forward.LengthSquared < 0.01f)
		{
			forward = Vec3.Forward;
		}
		forward.Normalize();
		Vec3 right = Vec3.CrossProduct(forward, Vec3.Up);
		if (right.LengthSquared < 0.01f)
		{
			right = Vec3.Side;
		}
		right.Normalize();

		TownHiddenResidentSpawnOffset slot = TownHiddenResidentSpawnPositionPolicy.GetGroupSlotOffset(index);
		Vec3 candidate = Ground(mission, anchor + forward * slot.ForwardMeters + right * slot.RightMeters);
		WorldPosition worldPosition = new WorldPosition(mission.Scene, candidate);
		return worldPosition.GetNearestNavMesh() == UIntPtr.Zero ? anchor : candidate;
	}

	private string ChooseCommonSpawnTag(MissionAgentHandler missionAgentHandler)
	{
		Dictionary<string, int> availablePointCounts;
		try
		{
			availablePointCounts = missionAgentHandler?.FindUnusedUsablePointCount() ?? new Dictionary<string, int>();
		}
		catch
		{
			availablePointCounts = new Dictionary<string, int>();
		}

		foreach (string tag in CommonSpawnTags)
		{
			if (!_exhaustedTags.Contains(tag)
				&& availablePointCounts.TryGetValue(tag, out int count)
				&& count > 0)
			{
				return tag;
			}
		}
		return null;
	}

	private static void ApplySpawnTagOverride(LocationCharacter locationCharacter, string expectedTag)
	{
		if (locationCharacter == null || string.IsNullOrWhiteSpace(expectedTag))
		{
			return;
		}
		locationCharacter.SpecialTargetTag = expectedTag;
		locationCharacter.ForceSpawnInSpecialTargetTag = false;
	}

	private static LocationCharacter CreateNativeTownCivilian(string creatorName, CultureObject culture)
	{
		try
		{
			MethodInfo creator = TownsfolkBehaviorType.GetMethod(creatorName, CreatorBindingFlags);
			return creator?.Invoke(null, new object[] { culture, LocationCharacter.CharacterRelations.Neutral }) as LocationCharacter;
		}
		catch
		{
			return null;
		}
	}

	private static int CountCurrentCivilianLikeAgents(Mission mission)
	{
		return mission?.Agents?.Count(agent =>
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive() || agent == Agent.Main)
			{
				return false;
			}
			CharacterObject character = agent.Character as CharacterObject;
			return character != null
				&& character != CharacterObject.PlayerCharacter
				&& !character.IsHero
				&& !character.IsSoldier
				&& IsCivilianOccupation(character.Occupation);
		}) ?? 0;
	}

	private static bool IsCivilianOccupation(Occupation occupation)
	{
		switch (occupation)
		{
		case Occupation.Townsfolk:
		case Occupation.Villager:
		case Occupation.GoodsTrader:
		case Occupation.Artisan:
		case Occupation.Merchant:
		case Occupation.Weaponsmith:
		case Occupation.Armorer:
		case Occupation.HorseTrader:
		case Occupation.ShopWorker:
		case Occupation.Blacksmith:
		case Occupation.Tavernkeeper:
		case Occupation.TavernWench:
		case Occupation.TavernGameHost:
		case Occupation.Musician:
		case Occupation.Preacher:
		case Occupation.RansomBroker:
		case Occupation.ShipWright:
		case Occupation.NotAssigned:
			return true;
		default:
			return false;
		}
	}

	private static Vec3 Ground(Mission mission, Vec3 position)
	{
		try
		{
			position.z = mission.Scene.GetGroundHeightAtPosition(position);
		}
		catch
		{
		}
		return position;
	}
}
