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
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// GCCZ-only bridge that raises the town-center civilian population to the normal town maximum
/// while still using vanilla LocationCharacter creators, culture equipment, wanderer behaviors,
/// and MissionAgentHandler spawn points. It intentionally does not create raw agent builds
/// and does not switch the mission to siege AI/deployment.
/// </summary>
internal sealed class InterventionNativeTownCivilianPopulationMissionBehavior : MissionLogic
{
	private static readonly Type TownsfolkBehaviorType = typeof(SandBox.CampaignBehaviors.CommonTownsfolkCampaignBehavior);
	private static readonly BindingFlags CreatorBindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
	private static readonly string[] CommonAdultCreators = { "CreateTownsMan", "CreateTownsWoman" };
	private static readonly string[] LimitedAdultCreators = { "CreateTownsManCarryingStuff", "CreateTownsWomanCarryingStuff" };
	private static readonly string[] BeggarCreators = { "CreateMaleBeggar", "CreateFemaleBeggar" };
	private static readonly HashSet<string> CivilianSpawnTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"npc_common",
		"npc_common_limited",
		"spawnpoint_cleaner",
		"npc_dancer",
		"npc_beggar",
		"sp_merchant",
		"sp_horse_merchant",
		"sp_armorer",
		"sp_weaponsmith",
		"sp_blacksmith",
		"sp_barber",
		"gambler_npc"
	};

	private readonly string _settlementId;
	private readonly HashSet<string> _exhaustedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private bool _completed;
	private bool _loggedCreatorFailure;
	private float _firstAttemptTime = -1f;
	private int _commonCreatorIndex;
	private int _limitedCreatorIndex;
	private int _beggarCreatorIndex;

	public InterventionNativeTownCivilianPopulationMissionBehavior(string settlementId)
	{
		_settlementId = string.IsNullOrWhiteSpace(settlementId) ? "N/A" : settlementId;
	}

	public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

	public override void AfterStart()
	{
		base.AfterStart();
		TryEnsureNativeTownCivilianMaximum(SiegeCivilianAssemblyProfile.NativeTownMaxPopulationSource + ":after_start");
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (_completed)
		{
			return;
		}
		TryEnsureNativeTownCivilianMaximum(SiegeCivilianAssemblyProfile.NativeTownMaxPopulationSource + ":tick");
	}

	private void TryEnsureNativeTownCivilianMaximum(string source)
	{
		if (_completed)
		{
			return;
		}
		Mission mission = base.Mission;
		if (mission == null)
		{
			return;
		}
		if (_firstAttemptTime < 0f)
		{
			_firstAttemptTime = mission.CurrentTime;
		}
		try
		{
			MissionAgentHandler missionAgentHandler = mission.GetMissionBehavior<MissionAgentHandler>();
			Settlement settlement = PlayerEncounter.LocationEncounter?.Settlement ?? Settlement.CurrentSettlement;
			Location location = CampaignMission.Current?.Location;
			if (missionAgentHandler == null || settlement?.Town == null || location == null || !settlement.IsTown || !string.Equals(location.StringId, "center", StringComparison.OrdinalIgnoreCase))
			{
				CompleteIfRetryWindowExpired(mission, "not_ready", source);
				return;
			}

			int currentCivilianCount = CountCurrentCivilianLikeAgents(mission, location);
			int targetCivilianCount = GetTargetCivilianCount(mission, currentCivilianCount);
			if (targetCivilianCount <= currentCivilianCount)
			{
				_completed = true;
				Logger.Log("SiegeAiIntervention", "Native town civilian maximum already satisfied. Settlement=" + _settlementId + ", Current=" + currentCivilianCount + ", Target=" + targetCivilianCount + ", Source=" + (source ?? "N/A"));
				return;
			}

			int spawned = SpawnCivilianDeficit(mission, missionAgentHandler, settlement, location, targetCivilianCount - currentCivilianCount);
			int finalCivilianCount = CountCurrentCivilianLikeAgents(mission, location);
			_completed = true;
			Logger.Log("SiegeAiIntervention", "Native town civilian maximum pass completed. Settlement=" + _settlementId + ", CurrentBefore=" + currentCivilianCount + ", Target=" + targetCivilianCount + ", Spawned=" + spawned + ", CurrentAfter=" + finalCivilianCount + ", Source=" + (source ?? "N/A") + ", VanillaLocationCharacters=true, VanillaSpawnPoints=true, SiegeDeployment=false");
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "Native town civilian maximum pass failed. Settlement=" + _settlementId + ", Source=" + (source ?? "N/A") + ", Error=" + ex.Message);
			CompleteIfRetryWindowExpired(mission, "exception", source);
		}
	}

	private void CompleteIfRetryWindowExpired(Mission mission, string reason, string source)
	{
		if (mission == null || _firstAttemptTime < 0f)
		{
			return;
		}
		if (mission.CurrentTime - _firstAttemptTime >= SiegeCivilianAssemblyProfile.NativeTownPopulationRetrySeconds)
		{
			_completed = true;
			Logger.Log("SiegeAiIntervention", "Native town civilian maximum pass skipped after retry window. Settlement=" + _settlementId + ", Reason=" + (reason ?? "N/A") + ", Source=" + (source ?? "N/A"));
		}
	}

	private static int GetTargetCivilianCount(Mission mission, int currentCivilianCount)
	{
		int normalConfigMax = Math.Max(0, (int)Math.Round(BannerlordConfig.CivilianAgentCount));
		if (normalConfigMax <= 0)
		{
			return currentCivilianCount;
		}
		int activeHumanCount = mission?.Agents?.Count(agent => agent != null && agent.IsHuman && agent.IsActive()) ?? currentCivilianCount;
		int nonCivilianActiveCount = Math.Max(0, activeHumanCount - currentCivilianCount);
		int sceneRoomForCivilians = Math.Max(0, SiegeCivilianAssemblyProfile.SceneTotalAgentSoftCap - nonCivilianActiveCount);
		int target = Math.Min(SiegeCivilianAssemblyProfile.TownSceneCap, Math.Min(normalConfigMax, sceneRoomForCivilians));
		return Math.Max(currentCivilianCount, target);
	}

	private int SpawnCivilianDeficit(Mission mission, MissionAgentHandler missionAgentHandler, Settlement settlement, Location location, int deficit)
	{
		if (deficit <= 0)
		{
			return 0;
		}
		int spawned = 0;
		int attempts = 0;
		while (spawned < deficit && attempts < SiegeCivilianAssemblyProfile.NativeTownPopulationMaxSpawnAttempts)
		{
			attempts++;
			string expectedTag;
			string creatorName = ChooseCreator(missionAgentHandler, out expectedTag);
			if (string.IsNullOrWhiteSpace(creatorName) || string.IsNullOrWhiteSpace(expectedTag))
			{
				break;
			}
			LocationCharacter locationCharacter = CreateNativeTownCivilian(creatorName, settlement.Culture);
			if (locationCharacter == null)
			{
				_exhaustedTags.Add(expectedTag);
				continue;
			}
			location.AddCharacter(locationCharacter);
			Agent agent = null;
			try
			{
				agent = missionAgentHandler.SpawnDefaultLocationCharacter(locationCharacter, simulateAgentAfterSpawn: true);
			}
			catch (Exception ex)
			{
				Logger.Log("SiegeAiIntervention", "Native town civilian vanilla spawn failed. Settlement=" + _settlementId + ", Creator=" + creatorName + ", Tag=" + expectedTag + ", Error=" + ex.Message);
			}
			if (agent == null)
			{
				location.RemoveLocationCharacter(locationCharacter);
				_exhaustedTags.Add(expectedTag);
				continue;
			}
			spawned++;
		}
		return spawned;
	}

	private string ChooseCreator(MissionAgentHandler missionAgentHandler, out string expectedTag)
	{
		expectedTag = null;
		Dictionary<string, int> availablePointCounts = GetAvailableUsablePointCounts(missionAgentHandler);
		if (HasAvailable(availablePointCounts, "spawnpoint_cleaner"))
		{
			expectedTag = "spawnpoint_cleaner";
			return "CreateBroomsWoman";
		}
		if (HasAvailable(availablePointCounts, "npc_dancer"))
		{
			expectedTag = "npc_dancer";
			return "CreateDancer";
		}
		if (HasAvailable(availablePointCounts, "npc_beggar"))
		{
			expectedTag = "npc_beggar";
			return BeggarCreators[(_beggarCreatorIndex++) % BeggarCreators.Length];
		}
		if (HasAvailable(availablePointCounts, "npc_common_limited"))
		{
			expectedTag = "npc_common_limited";
			return LimitedAdultCreators[(_limitedCreatorIndex++) % LimitedAdultCreators.Length];
		}
		if (HasAvailable(availablePointCounts, "npc_common"))
		{
			expectedTag = "npc_common";
			return CommonAdultCreators[(_commonCreatorIndex++) % CommonAdultCreators.Length];
		}
		return null;
	}

	private bool HasAvailable(Dictionary<string, int> availablePointCounts, string tag)
	{
		return !_exhaustedTags.Contains(tag) && availablePointCounts != null && availablePointCounts.TryGetValue(tag, out int count) && count > 0;
	}

	private static Dictionary<string, int> GetAvailableUsablePointCounts(MissionAgentHandler missionAgentHandler)
	{
		try
		{
			return missionAgentHandler?.FindUnusedUsablePointCount() ?? new Dictionary<string, int>();
		}
		catch
		{
			return new Dictionary<string, int>();
		}
	}

	private LocationCharacter CreateNativeTownCivilian(string creatorName, CultureObject culture)
	{
		try
		{
			MethodInfo creator = TownsfolkBehaviorType.GetMethod(creatorName, CreatorBindingFlags);
			if (creator == null)
			{
				LogCreatorFailureOnce("missing creator " + creatorName);
				return null;
			}
			return creator.Invoke(null, new object[] { culture, LocationCharacter.CharacterRelations.Neutral }) as LocationCharacter;
		}
		catch (Exception ex)
		{
			LogCreatorFailureOnce(creatorName + " failed: " + ex.Message);
			return null;
		}
	}

	private void LogCreatorFailureOnce(string detail)
	{
		if (_loggedCreatorFailure)
		{
			return;
		}
		_loggedCreatorFailure = true;
		Logger.Log("SiegeAiIntervention", "Native town civilian creator reflection unavailable. Settlement=" + _settlementId + ", Detail=" + (detail ?? "N/A"));
	}

	private static int CountCurrentCivilianLikeAgents(Mission mission, Location location)
	{
		if (mission?.Agents == null)
		{
			return 0;
		}
		int count = 0;
		foreach (Agent agent in mission.Agents.ToList())
		{
			if (!IsCivilianLikeTownAgent(agent, location))
			{
				continue;
			}
			count++;
		}
		return count;
	}

	private static bool IsCivilianLikeTownAgent(Agent agent, Location location)
	{
		try
		{
			if (agent == null || !agent.IsHuman || agent == Agent.Main || !agent.IsActive() || agent.State == AgentState.Killed || agent.State == AgentState.Unconscious)
			{
				return false;
			}
			CharacterObject character = agent.Character as CharacterObject;
			if (character == null || character == CharacterObject.PlayerCharacter || character.IsHero || character.IsSoldier)
			{
				return false;
			}
			LocationCharacter locationCharacter = null;
			try
			{
				locationCharacter = location?.GetLocationCharacter(agent.Origin);
			}
			catch
			{
			}
			if (locationCharacter != null)
			{
				return locationCharacter.UseCivilianEquipment && IsCivilianSpawnTag(locationCharacter.SpecialTargetTag);
			}
			return IsCivilianOccupation(character.Occupation);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsCivilianSpawnTag(string tag)
	{
		return !string.IsNullOrWhiteSpace(tag) && CivilianSpawnTags.Contains(tag);
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
}
