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
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// GCCZ-only bridge that raises the town-center civilian population to a prosperity-weighted
/// 100-200 civilian target
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
	private static readonly string[] AdditionalCivilianSpawnTags =
	{
		"sp_merchant",
		"sp_horse_merchant",
		"sp_armorer",
		"sp_weaponsmith",
		"sp_blacksmith",
		"sp_barber",
		"gambler_npc"
	};
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
	private int _prosperityWeightedTargetCivilianCount = -1;
	private float _targetProsperity;
	private int _commonCreatorIndex;
	private int _limitedCreatorIndex;
	private int _beggarCreatorIndex;
	private readonly bool _regularTownMultiplierMode;
	private int _regularTownBaseline = -1;
	private float _nextPopulationCheckAt = -1f;
	private const float RegularTownPopulationMultiplierMin = 2.1f;
	private const float RegularTownPopulationMultiplierMax = 2.8f;
	private const int RegularTownPopulationHardCap = 220;
	private const float PopulationCheckIntervalSeconds = 0.75f;

	public InterventionNativeTownCivilianPopulationMissionBehavior(string settlementId, bool regularTownMultiplierMode = false)
	{
		_settlementId = string.IsNullOrWhiteSpace(settlementId) ? "N/A" : settlementId;
		_regularTownMultiplierMode = regularTownMultiplierMode;
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
		if (_completed || DuelSettings.GetTownAmbientDialogueDensity() <= 0)
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
		// Avoid walking every active Agent once per Mission tick while native
		// location characters are still settling in.
		if (_nextPopulationCheckAt >= 0f && mission.CurrentTime < _nextPopulationCheckAt)
		{
			return;
		}
		_nextPopulationCheckAt = mission.CurrentTime + PopulationCheckIntervalSeconds;
		try
		{
			MissionAgentHandler missionAgentHandler = mission.GetMissionBehavior<MissionAgentHandler>();
			Settlement settlement = PlayerEncounter.LocationEncounter?.Settlement ?? Settlement.CurrentSettlement;
			Location location = CampaignMission.Current?.Location;
			if (missionAgentHandler == null || settlement == null || location == null || (!settlement.IsTown && !settlement.IsVillage))
			{
				CompleteIfRetryWindowExpired(mission, "not_ready", source);
				return;
			}
			if (IsPrivatePopulationLocation(location))
			{
				_completed = true;
				Logger.Log("SiegeAiIntervention", "Native settlement civilian pass blocked for private location. Settlement=" + _settlementId + ", Location=" + (location.StringId ?? "N/A"));
				return;
			}

			int currentCivilianCount = CountCurrentCivilianLikeAgents(mission, location);
			if (_regularTownMultiplierMode && currentCivilianCount < 8 && mission.CurrentTime - _firstAttemptTime < SiegeCivilianAssemblyProfile.NativeTownPopulationRetrySeconds)
			{
				return;
			}
			int targetCivilianCount = GetTargetCivilianCount(mission, settlement, currentCivilianCount);
			if (targetCivilianCount <= currentCivilianCount)
			{
				_completed = true;
				Logger.Log("SiegeAiIntervention", "Native settlement civilian target already satisfied. Settlement=" + _settlementId + ", Type=" + (settlement.IsVillage ? "village" : "town") + ", Current=" + currentCivilianCount + ", Target=" + targetCivilianCount + ", Activity=" + _targetProsperity.ToString("0") + ", Source=" + (source ?? "N/A"));
				return;
			}

			int spawned = SpawnCivilianDeficit(mission, missionAgentHandler, settlement, location, targetCivilianCount - currentCivilianCount);
			int finalCivilianCount = CountCurrentCivilianLikeAgents(mission, location);
			_completed = true;
			Logger.Log("SiegeAiIntervention", "Native settlement civilian pass completed. Settlement=" + _settlementId + ", Type=" + (settlement.IsVillage ? "village" : "town") + ", CurrentBefore=" + currentCivilianCount + ", Baseline=" + _regularTownBaseline + ", Target=" + targetCivilianCount + ", Activity=" + _targetProsperity.ToString("0") + ", Spawned=" + spawned + ", CurrentAfter=" + finalCivilianCount + ", Source=" + (source ?? "N/A") + ", VanillaLocationCharacters=true, VanillaSpawnPoints=true, SiegeDeployment=false");
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "Native town civilian maximum pass failed. Settlement=" + _settlementId + ", Source=" + (source ?? "N/A") + ", Error=" + ex.Message);
			CompleteIfRetryWindowExpired(mission, "exception", source);
		}
	}

	private static bool IsPrivatePopulationLocation(Location location)
	{
		string id = (location?.StringId ?? "").Trim().ToLowerInvariant();
		return id.Contains("lordhall") || id.Contains("lordshall") || id.Contains("lord_hall") || id.Contains("lords_hall") || id.Contains("keep") || id.Contains("tavern") || id.Contains("castle");
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

	private int GetTargetCivilianCount(Mission mission, Settlement settlement, int currentCivilianCount)
	{
		if (_regularTownMultiplierMode)
		{
			if (_regularTownBaseline < 0)
			{
				// A first-frame scene can expose only a few native civilians.  Use a
				// type-specific floor so the configured multiplier remains visible.
				_regularTownBaseline = Math.Max(GetBaselineFloor(settlement), currentCivilianCount);
				_targetProsperity = GetSettlementActivity(settlement);
			}
			float configuredMultiplier = DuelSettings.GetTownAmbientPopulationMultiplier();
			int density = DuelSettings.GetTownAmbientDialogueDensity();
			float activityRatio = GetSettlementActivityRatio(settlement);
			float lowActivityFloor = settlement.IsVillage ? 1.05f : 1.15f;
			// Prosperity controls the curve itself rather than merely nudging a
			// fixed multiplier: a struggling settlement stays close to vanilla,
			// while a prosperous one can approach the configured 2-3x target.
			configuredMultiplier = lowActivityFloor + (configuredMultiplier - lowActivityFloor) * activityRatio;
			if (density <= 1)
			{
				configuredMultiplier = Math.Min(configuredMultiplier, settlement.IsVillage ? 1.35f : 1.50f);
			}
			else if (density >= 3)
			{
				configuredMultiplier = Math.Min(3f, configuredMultiplier + 0.35f);
			}
			float multiplier;
			try
			{
				float variation = 0.10f;
				multiplier = configuredMultiplier - variation + MBRandom.RandomFloat * variation * 2f;
			}
			catch
			{
				multiplier = configuredMultiplier;
			}
			multiplier = Math.Max(1f, Math.Min(3f, multiplier));
			int targetByMultiplier = (int)Math.Round(_regularTownBaseline * multiplier);
			int hardCap = settlement.IsVillage ? SiegeCivilianAssemblyProfile.VillageSceneCap : RegularTownPopulationHardCap;
			int regularTarget = Math.Max(_regularTownBaseline, Math.Min(hardCap, targetByMultiplier));
			int regularActiveHumanCount = mission?.Agents?.Count(agent => agent != null && agent.IsHuman && agent.IsActive()) ?? currentCivilianCount;
			int regularNonCivilianActiveCount = Math.Max(0, regularActiveHumanCount - currentCivilianCount);
			int regularSceneRoomForCivilians = Math.Max(0, SiegeCivilianAssemblyProfile.SceneTotalAgentSoftCap - regularNonCivilianActiveCount);
			return Math.Max(currentCivilianCount, Math.Min(regularTarget, regularSceneRoomForCivilians));
		}
		int prosperityTarget = GetOrCreateProsperityWeightedTarget(settlement);
		int activeHumanCount = mission?.Agents?.Count(agent => agent != null && agent.IsHuman && agent.IsActive()) ?? currentCivilianCount;
		int nonCivilianActiveCount = Math.Max(0, activeHumanCount - currentCivilianCount);
		int sceneRoomForCivilians = Math.Max(0, SiegeCivilianAssemblyProfile.SceneTotalAgentSoftCap - nonCivilianActiveCount);
		int sceneCap = settlement?.IsVillage == true ? SiegeCivilianAssemblyProfile.VillageSceneCap : SiegeCivilianAssemblyProfile.TownSceneCap;
		int target = Math.Min(sceneCap, Math.Min(prosperityTarget, sceneRoomForCivilians));
		return Math.Max(currentCivilianCount, target);
	}

	private static int GetBaselineFloor(Settlement settlement)
	{
		return settlement?.IsVillage == true ? 12 : 40;
	}

	private static float GetSettlementActivity(Settlement settlement)
	{
		try
		{
			if (settlement?.Town != null)
			{
				return Math.Max(0f, settlement.Town.Prosperity);
			}
			if (settlement?.Village != null)
			{
				return Math.Max(0f, settlement.Village.Hearth);
			}
		}
		catch
		{
		}
		return 0f;
	}

	private static float GetSettlementActivityRatio(Settlement settlement)
	{
		float activity = GetSettlementActivity(settlement);
		float max = settlement?.IsVillage == true ? 100f : SiegeCivilianAssemblyProfile.NativeTownPopulationProsperityForMaxCount;
		return Clamp01(activity / Math.Max(1f, max));
	}

	private int GetOrCreateProsperityWeightedTarget(Settlement settlement)
	{
		if (_prosperityWeightedTargetCivilianCount > 0)
		{
			return _prosperityWeightedTargetCivilianCount;
		}
		_targetProsperity = GetTownProsperity(settlement);
		float prosperityRatio = Clamp01(_targetProsperity / Math.Max(1f, SiegeCivilianAssemblyProfile.NativeTownPopulationProsperityForMaxCount));
		int min = settlement?.IsVillage == true ? 20 : SiegeCivilianAssemblyProfile.MinDesiredCivilianCount;
		int max = settlement?.IsVillage == true ? 80 : SiegeCivilianAssemblyProfile.MaxDesiredCivilianCount;
		int band = Math.Max(0, Math.Min(SiegeCivilianAssemblyProfile.NativeTownPopulationRandomBand, max - min));
		int lower = (int)Math.Round(Lerp(min, max - band, prosperityRatio));
		int upper = (int)Math.Round(Lerp(min + band, max, prosperityRatio));
		lower = ClampInt(lower, min, max);
		upper = ClampInt(Math.Max(lower, upper), min, max);
		try
		{
			_prosperityWeightedTargetCivilianCount = ClampInt(MBRandom.RandomInt(lower, upper + 1), min, max);
		}
		catch
		{
			_prosperityWeightedTargetCivilianCount = ClampInt((lower + upper) / 2, min, max);
		}
		return _prosperityWeightedTargetCivilianCount;
	}

	private static float GetTownProsperity(Settlement settlement)
	{
		try
		{
			if (settlement?.Town != null)
			{
				return Math.Max(0f, settlement.Town.Prosperity);
			}
			if (settlement?.Village != null)
			{
				return Math.Max(0f, settlement.Village.Hearth / 100f * SiegeCivilianAssemblyProfile.NativeTownPopulationProsperityForMaxCount);
			}
			SettlementComponent.ProsperityLevel level = settlement?.SettlementComponent?.GetProsperityLevel() ?? SettlementComponent.ProsperityLevel.Mid;
			return ((float)level / 2f) * SiegeCivilianAssemblyProfile.NativeTownPopulationProsperityForMaxCount;
		}
		catch
		{
			return SiegeCivilianAssemblyProfile.NativeTownPopulationProsperityForMaxCount * 0.5f;
		}
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
			ApplySpawnTagOverride(locationCharacter, expectedTag);
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
		// Prefer a specialist creator when a named vanilla profession point is
		// available.  This keeps added agents bound to merchant/horse/forge
		// identities instead of spawning a generic townsman at that point.
		string specialistTag = ChooseAdditionalCivilianSpawnTag(availablePointCounts);
		if (!string.IsNullOrWhiteSpace(specialistTag))
		{
			expectedTag = specialistTag;
			return ChooseSpecialistCreator(specialistTag);
		}
		if (HasAvailable(availablePointCounts, "npc_common"))
		{
			expectedTag = "npc_common";
			return CommonAdultCreators[(_commonCreatorIndex++) % CommonAdultCreators.Length];
		}
		return null;
	}

	private string ChooseSpecialistCreator(string tag)
	{
		if (string.Equals(tag, "sp_merchant", StringComparison.OrdinalIgnoreCase)) return "CreateMerchant";
		if (string.Equals(tag, "sp_horse_merchant", StringComparison.OrdinalIgnoreCase)) return "CreateHorseTrader";
		if (string.Equals(tag, "sp_armorer", StringComparison.OrdinalIgnoreCase)) return "CreateArmorer";
		if (string.Equals(tag, "sp_weaponsmith", StringComparison.OrdinalIgnoreCase)) return "CreateWeaponsmith";
		if (string.Equals(tag, "sp_blacksmith", StringComparison.OrdinalIgnoreCase)) return "CreateBlacksmith";
		return CommonAdultCreators[(_commonCreatorIndex++) % CommonAdultCreators.Length];
	}

	private string ChooseAdditionalCivilianSpawnTag(Dictionary<string, int> availablePointCounts)
	{
		if (availablePointCounts == null || availablePointCounts.Count == 0)
		{
			return null;
		}
		foreach (string tag in AdditionalCivilianSpawnTags)
		{
			if (HasAvailable(availablePointCounts, tag))
			{
				return tag;
			}
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

	private static void ApplySpawnTagOverride(LocationCharacter locationCharacter, string expectedTag)
	{
		try
		{
			if (locationCharacter == null || string.IsNullOrWhiteSpace(expectedTag) || !IsCivilianSpawnTag(expectedTag))
			{
				return;
			}
			locationCharacter.SpecialTargetTag = expectedTag;
			locationCharacter.ForceSpawnInSpecialTargetTag = false;
		}
		catch
		{
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
				// Vanilla citizens often use a location-specific target tag that is
				// not one of our extra spawn tags.  They are still civilians and
				// must count toward the baseline; otherwise the multiplier sees
				// zero residents and never visibly increases a town.
				return locationCharacter.UseCivilianEquipment || IsCivilianSpawnTag(locationCharacter.SpecialTargetTag);
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

	private static float Lerp(float from, float to, float ratio)
	{
		return from + (to - from) * Clamp01(ratio);
	}

	private static float Clamp01(float value)
	{
		if (value < 0f)
		{
			return 0f;
		}
		if (value > 1f)
		{
			return 1f;
		}
		return value;
	}

	private static int ClampInt(int value, int min, int max)
	{
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}
}
