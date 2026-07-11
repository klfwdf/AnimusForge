using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AnimusForge.SiegeAftermathIntervention;
using Helpers;
using HarmonyLib;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using SandBox.Objects.AreaMarkers;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace AnimusForge;

public sealed class SettlementEntryTroopSelectionBehavior : CampaignBehaviorBase
{
	private const int OwnSettlementEntryLimit = 100;
	private const int OtherSettlementEntryLimit = 10;
	private const int DefenderReserveWaveSize = 30;
	private const int DefenderReservePhaseCount = 3;
	private const int MaxActiveDefenderReserveWaves = 4;
	private const float DefenderReserveWaveIntervalSeconds = 30f;
	private const float AlliedSpawnBaseDistance = 8f;
	private const float AlliedSpawnRowDistance = 2.8f;
	private const float AlliedSpawnLateralSpacing = 3.2f;
	private const float EnemyDoorSpawnBaseDistance = 4f;
	private const float EnemyDoorSpawnRowDistance = 3.4f;
	private const float EnemyDoorSpawnLateralSpacing = 4f;
	private const int DefenderReserveWorkshopSpawnGroupSize = 10;
	private const int DefenderReserveWorkshopGridColumns = 5;
	private const float DefenderReserveWorkshopGridRowSpacing = 1.5f;
	private const float DefenderReserveWorkshopGridLateralSpacing = 1.5f;
	private const int SpawnGridColumns = 8;
	private const float DefenderReserveStuckNudgeSeconds = 20f;
	private const float DefenderReserveStuckRetrySeconds = 50f;
	private const int EnemyWallPassRescueBudgetPerRefresh = 4;
	private const float EnemyInitialTargetLockSeconds = 1.5f;
	private const float ProtectedFollowerHostilitySuppressionSeconds = 8f;
	private const float ProtectedFollowerFriendlyFireDuplicateWindowSeconds = 0.05f;
	private const float VictoryEndMissionFallbackDelaySeconds = 2f;
	private const string LordHallLocationId = "lordshall";
	private const string TownCenterLocationId = "center";
	private const uint InfoColor = 0xFFDFC16Bu;
	private const uint WarningColor = 0xFFFF6B6Bu;
	private const uint SuccessColor = 0xFF8DDC7Eu;
	private const uint NeutralColor = 0xFF777777u;
	private const uint NeutralColor2 = 0xFF444444u;

	private static TroopRoster _ownSettlementProfile;
	private static TroopRoster _otherSettlementProfile;
	private static PendingProfileSelection _pendingProfileSelection;
	private static PendingMissionEntry _pendingMissionEntry;
	private static PendingSettlementVictoryMenuEntry _pendingVictoryMenuEntry;
	private static Mission _setsActiveUsableProtectionMission;
	private static Mission _setsSelectedFollowerMission;
	private static readonly HashSet<int> SetsActiveUsableProtectionAgentIndexes = new HashSet<int>();
	private static readonly HashSet<int> SetsSelectedFollowerAgentIndexes = new HashSet<int>();
	private static readonly Dictionary<int, float> SetsUsableProtectionLastLogTimes = new Dictionary<int, float>();
	private static bool _setsActiveUsableProtection;
	private static bool _setsEntryMissionActive;
	private static bool _setsOrderControllerPrimed;
	private static float _nextSetsOrderControllerPrimeTime;

	private enum EntryProfileKind
	{
		OwnSettlement,
		OtherSettlement
	}

	public static void RegisterHarmonyPatches(Harmony harmony)
	{
		if (harmony == null)
		{
			return;
		}
		PatchEncounterEntry(harmony, typeof(TownEncounter), nameof(TownEncounter.CreateAndOpenMissionController), nameof(TownCreateAndOpenMissionControllerPrefix), "town-center");
		PatchEncounterEntry(harmony, typeof(CastleEncounter), nameof(CastleEncounter.CreateAndOpenMissionController), nameof(CastleCreateAndOpenMissionControllerPrefix), "castle-center");
		PatchEncounterEntry(harmony, typeof(VillageEncounter), nameof(VillageEncounter.CreateAndOpenMissionController), nameof(VillageCreateAndOpenMissionControllerPrefix), "village-center");
		PatchEndMissionGuard(harmony, typeof(BasicLeaveMissionLogic), nameof(BasicLeaveMissionLogic.OnEndMissionRequest), "basic-leave");
		PatchEndMissionGuard(harmony, typeof(MissionFightHandler), nameof(MissionFightHandler.OnEndMissionRequest), "mission-fight");
		PatchSetsUsableProtection(harmony);
		PatchOwnedOrAttachedTownDamage(harmony);
	}

	public override void RegisterEvents()
	{
		EnsureProfileRosters();
		CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
		CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnSetsMissionEnded);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnSetsGameMenuOpened);
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_setsOwnSettlementEntryProfile_v1", ref _ownSettlementProfile);
		dataStore.SyncData("_setsOtherSettlementEntryProfile_v1", ref _otherSettlementProfile);
		EnsureProfileRosters();
	}

	public static void OpenConfigFromTerminal()
	{
		EnsureProfileRosters();
		try
		{
			if (Mission.Current != null || Campaign.Current == null || MobileParty.MainParty?.MemberRoster == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("【SETS】只能在战役地图上配置进城随行。", Color.FromUint(WarningColor)));
				return;
			}
			List<InquiryElement> choices = new List<InquiryElement>
			{
				new InquiryElement(EntryProfileKind.OwnSettlement, "自有定居点随行（100人）", null, isEnabled: true, BuildProfileHint(EntryProfileKind.OwnSettlement)),
				new InquiryElement(EntryProfileKind.OtherSettlement, "他方定居点随行（10人）", null, isEnabled: true, BuildProfileHint(EntryProfileKind.OtherSettlement))
			};
			MultiSelectionInquiryData data = new MultiSelectionInquiryData("【SETS】进城随行配置", "选择要配置的进城/城堡/村庄随行名单：", choices, isExitShown: true, 1, 1, "配置", "关闭", delegate(List<InquiryElement> selected)
			{
				if (selected == null || selected.Count == 0 || selected[0].Identifier is not EntryProfileKind kind)
				{
					return;
				}
				OpenProfileSelection(kind);
			}, null, "", isSeachAvailable: false);
			MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
		}
		catch (Exception ex)
		{
			SettlementEntryTroopSelectionLog.Log("Open config from terminal failed. error=" + ex);
			InformationManager.DisplayMessage(new InformationMessage("【SETS】打开进城随行配置失败。", Color.FromUint(WarningColor)));
		}
	}

	internal static void QueueSettlementTakenMenuAfterTownVictory(string settlementId, TroopRoster survivingRoster, string source, bool skipOwnershipTransfer = false, bool setsOwnedIncident = false, bool setsTownRiotKilledNotable = false)
	{
		if (string.IsNullOrWhiteSpace(settlementId))
		{
			return;
		}
		Settlement settlement = Settlement.Find(settlementId);
		if (settlement != null && !settlement.IsTown)
		{
			SettlementEntryTroopSelectionLog.Log("Ignored SETS victory menu queue for non-town settlement. settlement=" + SafeSettlementId(settlement) + ", source=" + (source ?? ""));
			return;
		}
		_pendingVictoryMenuEntry = new PendingSettlementVictoryMenuEntry
		{
			SettlementId = settlementId,
			SurvivingRoster = CloneRoster(survivingRoster, int.MaxValue),
			Source = string.IsNullOrWhiteSpace(source) ? "SETS_town_victory" : source,
			SkipOwnershipTransfer = skipOwnershipTransfer,
			SetsOwnedIncident = setsOwnedIncident,
			SetsTownRiotKilledNotable = setsTownRiotKilledNotable
		};
		SettlementEntryTroopSelectionLog.Log("Queued native settlement-taken menu after SETS town victory. settlement=" + settlementId + ", survivors=" + (_pendingVictoryMenuEntry.SurvivingRoster?.TotalManCount ?? 0) + ", source=" + _pendingVictoryMenuEntry.Source + ", skipOwnershipTransfer=" + skipOwnershipTransfer + ", ownedIncident=" + setsOwnedIncident + ", killedNotable=" + setsTownRiotKilledNotable);
	}

	private void OnNewGameCreated(CampaignGameStarter starter)
	{
		ClearRuntime("new_game");
		EnsureProfileRosters();
	}

	private void OnGameLoaded(CampaignGameStarter starter)
	{
		ClearRuntime("game_loaded");
		EnsureProfileRosters();
	}

	private static void ClearRuntime(string source)
	{
		_pendingProfileSelection = null;
		_pendingMissionEntry = null;
		_pendingVictoryMenuEntry = null;
		ClearSetsUsableProtectionState(source);
		ClearSetsSelectedFollowerState(source);
		SettlementEntryTroopSelectionLog.Log("Runtime cleared. source=" + source);
	}

	internal static bool IsSetsSelectedFollowerAgentForExternal(Agent agent)
	{
		return agent != null && IsSetsSelectedFollowerAgentForExternal(agent.Index);
	}

	internal static bool IsSetsSelectedFollowerAgentForExternal(int agentIndex)
	{
		try
		{
			if (agentIndex < 0 || !_setsEntryMissionActive || _setsSelectedFollowerMission == null || Mission.Current != _setsSelectedFollowerMission)
			{
				return false;
			}
			return SetsSelectedFollowerAgentIndexes.Contains(agentIndex);
		}
		catch
		{
			return false;
		}
	}

	internal static bool ShouldBypassSceneTauntExitBlockForExternal(Mission mission)
	{
		try
		{
			SettlementEntryTroopSelectionMissionLogic logic = mission?.GetMissionBehavior<SettlementEntryTroopSelectionMissionLogic>();
			return logic != null && logic.ShouldBypassNativeEndMissionGuards();
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsOwnedOrAttachedTownEntryActiveForExternal(Mission mission)
	{
		try
		{
			SettlementEntryTroopSelectionMissionLogic logic = mission?.GetMissionBehavior<SettlementEntryTroopSelectionMissionLogic>();
			return logic != null && logic.IsOwnedOrAttachedTownEntryActive();
		}
		catch
		{
			return false;
		}
	}

	internal static bool TryGatherOwnedOrAttachedTownSceneAgentsForExternal(int seedAgentIndex, string source)
	{
		try
		{
			SettlementEntryTroopSelectionMissionLogic logic = Mission.Current?.GetMissionBehavior<SettlementEntryTroopSelectionMissionLogic>();
			return logic != null && logic.TryGatherOwnedOrAttachedTownSceneAgents(seedAgentIndex, source);
		}
		catch (Exception ex)
		{
			SettlementEntryTroopSelectionLog.Log("TryGatherOwnedOrAttachedTownSceneAgentsForExternal failed. source=" + (source ?? "N/A") + ", error=" + ex.Message);
			return false;
		}
	}

	internal static bool ShouldInjectSetsOrderViewsForExternal(Mission mission)
	{
		try
		{
			mission ??= Mission.Current;
			return IsSetsCommandMissionCandidate(mission);
		}
		catch
		{
			return false;
		}
	}

	internal static Team ResolveSetsPlayerCommandTeamForExternal(Mission mission, string source = null)
	{
		try
		{
			mission ??= Mission.Current;
			if (!IsSetsCommandMissionCandidate(mission))
			{
				return null;
			}
			Agent main = Agent.Main ?? mission?.MainAgent;
			Team playerTeam = mission?.PlayerTeam ?? main?.Team;
			if (mission == null || main == null || !main.IsActive())
			{
				return playerTeam;
			}
			if (playerTeam == null || !playerTeam.IsPlayerGeneral)
			{
				try
				{
					uint color = Hero.MainHero?.MapFaction?.Color ?? 0xFF2020FFu;
					uint color2 = Hero.MainHero?.MapFaction?.Color2 ?? 0xFF101080u;
					playerTeam = mission.Teams.Add(BattleSideEnum.Attacker, color, color2, Hero.MainHero?.Clan?.Banner, isPlayerGeneral: true, isPlayerSergeant: false);
					mission.PlayerTeam = playerTeam;
				}
				catch
				{
					playerTeam = mission.PlayerTeam ?? main.Team;
				}
			}
			else
			{
				mission.PlayerTeam = playerTeam;
			}
			if (playerTeam != null && main.Team != playerTeam)
			{
				main.SetTeam(playerTeam, true);
			}
			return playerTeam;
		}
		catch (Exception ex)
		{
			SettlementEntryTroopSelectionLog.Log("ResolveSetsPlayerCommandTeamForExternal failed. source=" + (source ?? "N/A") + ", error=" + ex.Message);
			return null;
		}
	}

	internal static bool EnsureSetsCommandUiReadyForExternal(Mission mission, string source, bool force = false, bool preserveSelection = true)
	{
		try
		{
			mission ??= Mission.Current;
			if (!IsSetsCommandMissionCandidate(mission) || mission.Mode == MissionMode.Conversation || mission.Mode == MissionMode.Barter)
			{
				return false;
			}
			float now = mission.CurrentTime;
			if (!force && _setsOrderControllerPrimed)
			{
				return SetsPlayerHasCommandableAgentsForExternal(mission) && TryResolveSetsNativeOrderControllerForExternal(mission) != null;
			}
			if (!force && now < _nextSetsOrderControllerPrimeTime)
			{
				return SetsPlayerHasCommandableAgentsForExternal(mission) && TryResolveSetsNativeOrderControllerForExternal(mission) != null;
			}
			_nextSetsOrderControllerPrimeTime = now + 2f;
			Team playerTeam = ResolveSetsPlayerCommandTeamForExternal(mission, source);
			Agent main = Agent.Main ?? mission.MainAgent;
			if (playerTeam == null || main == null)
			{
				return false;
			}
			int commandable = 0;
			HashSet<Formation> commandFormations = new HashSet<Formation>();
			foreach (Agent agent in mission.Agents?.ToList() ?? new List<Agent>())
			{
				if (!IsSetsSelectedFollowerAgentForExternal(agent) || agent == main)
				{
					continue;
				}
				if (agent.Team != playerTeam)
				{
					agent.SetTeam(playerTeam, true);
				}
				AssignSetsAgentToPlayerFormation(agent, playerTeam, FormationClass.Infantry);
				if (agent.Formation != null)
				{
					MarkFormationPlayerCommandable(agent.Formation, main);
					commandFormations.Add(agent.Formation);
					commandable++;
				}
			}
			if (commandable <= 0)
			{
				return false;
			}
			bool hasExistingSelection = false;
			try
			{
				hasExistingSelection = playerTeam.PlayerOrderController?.SelectedFormations != null && playerTeam.PlayerOrderController.SelectedFormations.Count > 0;
			}
			catch
			{
			}
			bool shouldInitializeSelection = !preserveSelection && !hasExistingSelection;
			foreach (Formation formation in commandFormations)
			{
				MarkFormationPlayerCommandable(formation, main);
				if (shouldInitializeSelection)
				{
					try { playerTeam.PlayerOrderController?.SelectFormation(formation); } catch { }
				}
			}
			if (shouldInitializeSelection)
			{
				try { playerTeam.PlayerOrderController?.SelectAllFormations(false); } catch { }
				try { playerTeam.MasterOrderController?.SelectAllFormations(false); } catch { }
			}
			_setsOrderControllerPrimed = true;
			SettlementEntryTroopSelectionLog.Log("Primed SETS player order controller. source=" + (source ?? "N/A") + ", commandable=" + commandable + ", formations=" + commandFormations.Count + ", preserveSelection=" + preserveSelection + ", existingSelection=" + hasExistingSelection);
			return TryResolveSetsNativeOrderControllerForExternal(mission) != null;
		}
		catch (Exception ex)
		{
			SettlementEntryTroopSelectionLog.Log("EnsureSetsCommandUiReadyForExternal failed. source=" + (source ?? "N/A") + ", error=" + ex.Message);
			return false;
		}
	}

	internal static bool SetsPlayerHasCommandableAgentsForExternal(Mission mission)
	{
		try
		{
			mission ??= Mission.Current;
			Team playerTeam = ResolveSetsPlayerCommandTeamForExternal(mission, "has_commandable_agents");
			Agent main = Agent.Main ?? mission?.MainAgent;
			return mission?.Agents != null && playerTeam != null && mission.Agents.Any(a => IsSetsSelectedFollowerAgentForExternal(a) && a != main && a.Team == playerTeam && a.Formation != null);
		}
		catch
		{
			return false;
		}
	}

	internal static bool NativeOrderControllerHasSelectedFormationsForSetsExternal(Mission mission)
	{
		try
		{
			OrderController orderController = TryResolveSetsNativeOrderControllerForExternal(mission);
			return orderController != null && SetsPlayerHasCommandableAgentsForExternal(mission);
		}
		catch
		{
			return false;
		}
	}

	internal static OrderController TryResolveSetsNativeOrderControllerForExternal(Mission mission)
	{
		try
		{
			mission ??= Mission.Current;
			if (!IsSetsCommandMissionCandidate(mission) || mission.Mode == MissionMode.Conversation || mission.Mode == MissionMode.Barter)
			{
				return null;
			}
			Team playerTeam = ResolveSetsPlayerCommandTeamForExternal(mission, "resolve_order_controller") ?? mission.PlayerTeam ?? Agent.Main?.Team ?? mission.MainAgent?.Team;
			return playerTeam?.PlayerOrderController ?? playerTeam?.MasterOrderController;
		}
		catch
		{
			return null;
		}
	}

	private static bool IsSetsCommandMissionCandidate(Mission mission)
	{
		try
		{
			if (mission == null || mission.IsMissionEnding)
			{
				return false;
			}
			if (_setsEntryMissionActive && ReferenceEquals(_setsSelectedFollowerMission, mission))
			{
				return true;
			}
			if (mission.GetMissionBehavior<SettlementEntryTroopSelectionMissionLogic>() != null)
			{
				return true;
			}
			return _pendingMissionEntry != null;
		}
		catch
		{
			return false;
		}
	}

	private static void AssignSetsAgentToPlayerFormation(Agent agent, Team team, FormationClass formationClass)
	{
		try
		{
			Formation formation = team?.GetFormation(formationClass);
			if (agent == null || formation == null || !agent.IsHuman || !agent.IsActive())
			{
				return;
			}
			if (agent.Team != team)
			{
				agent.SetTeam(team, true);
			}
			agent.Formation = formation;
			MarkFormationPlayerCommandable(formation, Agent.Main ?? agent.Mission?.MainAgent);
			agent.TryAttachToFormation();
			agent.SetShouldCatchUpWithFormation(true);
			agent.UpdateFormationOrders();
		}
		catch (Exception ex)
		{
			SettlementEntryTroopSelectionLog.Log("AssignSetsAgentToPlayerFormation failed. error=" + ex.Message);
		}
	}

	private static void MarkFormationPlayerCommandable(Formation formation, Agent playerOwner)
	{
		try
		{
			if (formation == null)
			{
				return;
			}
			try
			{
				formation.SetControlledByAI(false, false);
			}
			catch
			{
				TrySetFormationProperty(formation, nameof(Formation.IsAIControlled), false);
			}
			TrySetFormationProperty(formation, nameof(Formation.HasPlayerControlledTroop), true);
			if (playerOwner != null && playerOwner.IsActive())
			{
				TrySetFormationProperty(formation, nameof(Formation.PlayerOwner), playerOwner);
			}
		}
		catch (Exception ex)
		{
			SettlementEntryTroopSelectionLog.Log("MarkFormationPlayerCommandable failed. error=" + ex.Message);
		}
	}

	private static void TrySetFormationProperty(Formation formation, string propertyName, object value)
	{
		try
		{
			PropertyInfo property = formation?.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			MethodInfo setter = property?.GetSetMethod(true);
			setter?.Invoke(formation, new object[] { value });
		}
		catch
		{
		}
	}

	private static void SetSetsSelectedFollowerState(Mission mission, bool active, string source)
	{
		try
		{
			if (mission == null || !ReferenceEquals(_setsSelectedFollowerMission, mission))
			{
				SetsSelectedFollowerAgentIndexes.Clear();
				_setsSelectedFollowerMission = mission;
			}
			_setsEntryMissionActive = active && mission != null;
			if (!active)
			{
				_setsOrderControllerPrimed = false;
				_nextSetsOrderControllerPrimeTime = 0f;
			}
			SettlementEntryTroopSelectionLog.Log("SETS selected follower state updated. source=" + source + ", active=" + _setsEntryMissionActive + ", tracked=" + SetsSelectedFollowerAgentIndexes.Count);
		}
		catch
		{
		}
	}

	private static void RegisterSetsSelectedFollowerAgent(Agent agent, string source)
	{
		try
		{
			if (agent == null || agent.Index < 0)
			{
				return;
			}
			SetSetsSelectedFollowerState(Mission.Current, active: true, source);
			if (SetsSelectedFollowerAgentIndexes.Add(agent.Index))
			{
				SettlementEntryTroopSelectionLog.Log("Registered SETS selected follower agent. source=" + source + ", agent=" + agent.Index + ", troop=" + SafeCharacterId(agent.Character as CharacterObject));
			}
		}
		catch
		{
		}
	}

	private static void ClearSetsSelectedFollowerState(string source)
	{
		try
		{
			SetsSelectedFollowerAgentIndexes.Clear();
			_setsSelectedFollowerMission = null;
			_setsEntryMissionActive = false;
			_setsOrderControllerPrimed = false;
			_nextSetsOrderControllerPrimeTime = 0f;
			SettlementEntryTroopSelectionLog.Log("Cleared SETS selected follower state. source=" + source);
		}
		catch
		{
		}
	}

	private static void PatchEncounterEntry(Harmony harmony, Type encounterType, string methodName, string prefixName, string label)
	{
		MethodInfo target = AccessTools.Method(encounterType, methodName, new[]
		{
			typeof(Location),
			typeof(Location),
			typeof(CharacterObject),
			typeof(string)
		});
		if (target == null)
		{
			SettlementEntryTroopSelectionLog.Log(label + " entry patch target not found.");
			return;
		}
		harmony.Patch(target, prefix: new HarmonyMethod(typeof(SettlementEntryTroopSelectionBehavior), prefixName));
		SettlementEntryTroopSelectionLog.Log("Harmony patch registered for " + label + " entry.");
	}

	private static void PatchEndMissionGuard(Harmony harmony, Type logicType, string methodName, string label)
	{
		MethodInfo target = AccessTools.Method(logicType, methodName);
		if (target == null)
		{
			SettlementEntryTroopSelectionLog.Log(label + " end-mission guard patch target not found.");
			return;
		}
		harmony.Patch(target, prefix: new HarmonyMethod(typeof(SettlementEntryTroopSelectionBehavior), nameof(AllowSetsVictoryEndMissionGuardPrefix)));
		SettlementEntryTroopSelectionLog.Log("Harmony patch registered for " + label + " end-mission guard.");
	}

	private static void PatchSetsUsableProtection(Harmony harmony)
	{
		try
		{
			MethodInfo targetPrefix = typeof(SettlementEntryTroopSelectionBehavior).GetMethod(nameof(SetsAgentNavigatorSetTargetPrefix), BindingFlags.Static | BindingFlags.NonPublic);
			MethodInfo targetMethod = AccessTools.Method(typeof(AgentNavigator), nameof(AgentNavigator.SetTarget), new[] { typeof(UsableMachine), typeof(bool), typeof(Agent.AIScriptedFrameFlags) });
			if (targetMethod != null && targetPrefix != null)
			{
				harmony.Patch(targetMethod, prefix: new HarmonyMethod(targetPrefix));
				SettlementEntryTroopSelectionLog.Log("Harmony patch registered for SETS usable target protection.");
			}
			MethodInfo movePrefix = typeof(SettlementEntryTroopSelectionBehavior).GetMethod(nameof(SetsUsableMissionObjectMoveToUsePrefix), BindingFlags.Static | BindingFlags.NonPublic);
			MethodInfo moveMethod = AccessTools.Method(typeof(UsableMissionObject), "OnAIMoveToUse", new[] { typeof(Agent), typeof(IDetachment) });
			if (moveMethod != null && movePrefix != null)
			{
				harmony.Patch(moveMethod, prefix: new HarmonyMethod(movePrefix));
				SettlementEntryTroopSelectionLog.Log("Harmony patch registered for SETS usable move protection.");
			}
		}
		catch (Exception ex)
		{
			SettlementEntryTroopSelectionLog.Log("PatchSetsUsableProtection failed. error=" + ex.Message);
		}
	}

	private static void PatchOwnedOrAttachedTownDamage(Harmony harmony)
	{
		try
		{
			MethodInfo target = AccessTools.Method(typeof(Mission), "CancelsDamageAndBlocksAttackBecauseOfNonEnemyCase");
			MethodInfo prefix = typeof(SettlementEntryTroopSelectionBehavior).GetMethod(nameof(AllowOwnedOrAttachedTownPlayerDamagePrefix), BindingFlags.Static | BindingFlags.NonPublic);
			if (target == null || prefix == null)
			{
				SettlementEntryTroopSelectionLog.Log("SETS owned/attached town damage patch target not found.");
				return;
			}
			harmony.Patch(target, prefix: new HarmonyMethod(prefix));
			SettlementEntryTroopSelectionLog.Log("Harmony patch registered for SETS owned/attached town player damage.");
		}
		catch (Exception ex)
		{
			SettlementEntryTroopSelectionLog.Log("PatchOwnedOrAttachedTownDamage failed. error=" + ex.Message);
		}
	}

	private static bool AllowOwnedOrAttachedTownPlayerDamagePrefix(Mission __instance, Agent attacker, Agent victim, ref bool __result)
	{
		try
		{
			if (__instance == null
				|| !IsOwnedOrAttachedTownEntryActiveForExternal(__instance)
				|| !SceneTauntBehavior.IsPeaceSceneConflictEnabled()
				|| attacker == null
				|| victim == null
				|| !attacker.IsMainAgent
				|| !victim.IsHuman
				|| victim.IsMainAgent)
			{
				return true;
			}
			if (IsSetsSelectedFollowerAgentForExternal(victim))
			{
				__result = true;
				return false;
			}
			__result = false;
			return false;
		}
		catch
		{
			return true;
		}
	}

	private static bool SetsAgentNavigatorSetTargetPrefix(AgentNavigator __instance, UsableMachine usableMachine)
	{
		try
		{
			Agent agent = __instance?.OwnerAgent;
			if (!IsSetsUsableProtectionAgent(agent))
			{
				return true;
			}
			if (usableMachine == null)
			{
				return true;
			}
			TryStopSetsUsableNavigation(agent);
			LogSetsUsableProtectionSuppression(agent, "SetTarget");
			return false;
		}
		catch
		{
			return true;
		}
	}

	private static bool SetsUsableMissionObjectMoveToUsePrefix(Agent userAgent)
	{
		try
		{
			if (!IsSetsUsableProtectionAgent(userAgent))
			{
				return true;
			}
			LogSetsUsableProtectionSuppression(userAgent, "OnAIMoveToUse");
			return false;
		}
		catch
		{
			return true;
		}
	}

	private static bool IsSetsUsableProtectionAgent(Agent agent)
	{
		try
		{
			return _setsActiveUsableProtection
				&& agent != null
				&& agent.IsHuman
				&& agent.IsActive()
				&& Mission.Current != null
				&& Mission.Current == _setsActiveUsableProtectionMission
				&& SetsActiveUsableProtectionAgentIndexes.Contains(agent.Index);
		}
		catch
		{
			return false;
		}
	}

	private static void SetSetsUsableProtectionState(Mission mission, bool active, IEnumerable<int> alliedAgentIndexes, IEnumerable<int> enemyAgentIndexes, string source)
	{
		try
		{
			if (!active || mission == null)
			{
				ClearSetsUsableProtectionState(source);
				return;
			}
			_setsActiveUsableProtection = true;
			_setsActiveUsableProtectionMission = mission;
			SetsActiveUsableProtectionAgentIndexes.Clear();
			AddSetsUsableProtectionIndexes(alliedAgentIndexes);
			AddSetsUsableProtectionIndexes(enemyAgentIndexes);
		}
		catch (Exception ex)
		{
			SettlementEntryTroopSelectionLog.Log("SetSetsUsableProtectionState failed. source=" + source + ", error=" + ex.Message);
		}
	}

	private static void AddSetsUsableProtectionIndexes(IEnumerable<int> indexes)
	{
		if (indexes == null)
		{
			return;
		}
		foreach (int index in indexes)
		{
			SetsActiveUsableProtectionAgentIndexes.Add(index);
		}
	}

	private static void ClearSetsUsableProtectionState(string source)
	{
		if (!_setsActiveUsableProtection && SetsActiveUsableProtectionAgentIndexes.Count <= 0)
		{
			return;
		}
		_setsActiveUsableProtection = false;
		_setsActiveUsableProtectionMission = null;
		SetsActiveUsableProtectionAgentIndexes.Clear();
		SetsUsableProtectionLastLogTimes.Clear();
		SettlementEntryTroopSelectionLog.Log("Cleared SETS usable protection. source=" + source);
	}

	private static void TryStopSetsUsableNavigation(Agent agent)
	{
		try
		{
			if (agent == null)
			{
				return;
			}
			agent.DisableScriptedMovement();
			agent.ClearTargetFrame();
		}
		catch
		{
		}
	}

	private static void LogSetsUsableProtectionSuppression(Agent agent, string source)
	{
		try
		{
			float now = Mission.Current?.CurrentTime ?? 0f;
			int index = agent?.Index ?? -1;
			if (index >= 0 && SetsUsableProtectionLastLogTimes.TryGetValue(index, out float last) && now - last < 5f)
			{
				return;
			}
			if (index >= 0)
			{
				SetsUsableProtectionLastLogTimes[index] = now;
			}
			SettlementEntryTroopSelectionLog.Log("Suppressed SETS combat agent usable target. source=" + source + ", agent=" + index + ", troop=" + SafeCharacterId(agent?.Character as CharacterObject));
		}
		catch
		{
		}
	}

	private static bool AllowSetsVictoryEndMissionGuardPrefix(ref InquiryData __result, out bool canPlayerLeave)
	{
		canPlayerLeave = true;
		if (!ShouldBypassEndMissionGuardsForSetsVictory())
		{
			return true;
		}
		__result = null;
		SettlementEntryTroopSelectionLog.Log("Bypassed native end-mission guard for SETS victory.");
		return false;
	}

	private static bool ShouldBypassEndMissionGuardsForSetsVictory()
	{
		try
		{
			SettlementEntryTroopSelectionMissionLogic logic = Mission.Current?.GetMissionBehavior<SettlementEntryTroopSelectionMissionLogic>();
			return logic != null && logic.ShouldBypassNativeEndMissionGuards();
		}
		catch
		{
			return false;
		}
	}

	private static bool TownCreateAndOpenMissionControllerPrefix(TownEncounter __instance, Location nextLocation, Location previousLocation, CharacterObject talkToChar, string playerSpecialSpawnTag)
	{
		TryPrepareSettlementEntryMission(__instance?.Settlement, nextLocation, TownCenterLocationId, "town");
		return true;
	}

	private static bool CastleCreateAndOpenMissionControllerPrefix(CastleEncounter __instance, Location nextLocation, Location previousLocation, CharacterObject talkToChar, string playerSpecialSpawnTag)
	{
		// SETS castle entry/conflict is intentionally blocked until the castle aftermath bridge is fixed.
		return true;
	}

	private static bool VillageCreateAndOpenMissionControllerPrefix(VillageEncounter __instance, Location nextLocation, Location previousLocation, CharacterObject talkToChar, string playerSpecialSpawnTag)
	{
		// SETS village entry/conflict is intentionally blocked until the village aftermath bridge is fixed.
		return true;
	}

	private static void TryPrepareSettlementEntryMission(Settlement settlement, Location nextLocation, string expectedLocationId, string sceneKind)
	{
		try
		{
			if (!ShouldPrepareSettlementEntry(settlement, nextLocation, expectedLocationId))
			{
				return;
			}
			EnsureProfileRosters();
			EntryProfileKind profileKind = IsOwnEntrySettlement(settlement) ? EntryProfileKind.OwnSettlement : EntryProfileKind.OtherSettlement;
			int limit = GetProfileLimit(profileKind);
			TroopRoster profile = GetProfileRoster(profileKind);
			TroopRoster selected = ResolveProfileRosterForEntry(profile, MobileParty.MainParty?.MemberRoster, limit, out int configuredCount, out int unavailableCount);
			_pendingMissionEntry = new PendingMissionEntry
			{
				SettlementId = settlement?.StringId ?? "",
				SelectedRoster = selected,
				Limit = limit,
				IsOwnSettlement = profileKind == EntryProfileKind.OwnSettlement,
				SceneKind = sceneKind ?? "settlement"
			};
			ShowEntryReminder(settlement, profileKind, limit, selected.TotalManCount, configuredCount, unavailableCount);
			SettlementEntryTroopSelectionLog.Log("Prepared settlement entry followers. settlement=" + SafeSettlementId(settlement) + ", scene=" + _pendingMissionEntry.SceneKind + ", profile=" + profileKind + ", selected=" + selected.TotalManCount + ", configured=" + configuredCount + ", unavailable=" + unavailableCount);
		}
		catch (Exception ex)
		{
			SettlementEntryTroopSelectionLog.Log("Prepare settlement entry failed; falling through vanilla. settlement=" + SafeSettlementId(settlement) + ", error=" + ex);
			_pendingMissionEntry = null;
		}
	}

	private static bool ShouldPrepareSettlementEntry(Settlement settlement, Location nextLocation, string expectedLocationId)
	{
		try
		{
			if (settlement == null || nextLocation == null || nextLocation.StringId != expectedLocationId)
			{
				return false;
			}
			if (!settlement.IsTown)
			{
				return false;
			}
			if (settlement.IsUnderSiege || settlement.Party?.MapEvent != null || MobileParty.MainParty?.MapEvent != null)
			{
				return false;
			}
			if (Mission.Current != null || Campaign.Current == null || MobileParty.MainParty?.MemberRoster == null)
			{
				return false;
			}
			if (Campaign.Current.IsMainHeroDisguised)
			{
				return false;
			}
			if (_pendingMissionEntry != null || _pendingVictoryMenuEntry != null)
			{
				return false;
			}
			if (SiegeAiInterventionBehavior.IsInterventionMissionOpenOrPendingForExternal())
			{
				SettlementEntryTroopSelectionLog.Log("Skipped settlement entry followers; GCCZ intervention is open or pending. settlement=" + SafeSettlementId(settlement) + ", location=" + (nextLocation?.StringId ?? "null"));
				return false;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void OpenProfileSelection(EntryProfileKind profileKind)
	{
		EnsureProfileRosters();
		try
		{
			TroopRoster mainRoster = MobileParty.MainParty?.MemberRoster;
			if (mainRoster == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("【SETS】玩家部队不可用，无法配置随行。", Color.FromUint(WarningColor)));
				return;
			}
			int limit = GetProfileLimit(profileKind);
			TroopRoster current = ResolveProfileRosterForEntry(GetProfileRoster(profileKind), mainRoster, limit, out _, out _);
			TroopRoster leftMembers = BuildConfigSelectableRoster(mainRoster);
			SubtractRoster(leftMembers, current);
			TroopRoster emptyPrisoners = TroopRoster.CreateDummyTroopRoster();
			_pendingProfileSelection = new PendingProfileSelection
			{
				ProfileKind = profileKind,
				Limit = limit
			};
			TextObject rightName = new TextObject(GetProfileTitle(profileKind) + "（上限 {LIMIT}）");
			rightName.SetTextVariable("LIMIT", limit);
			PartyScreenLogic logic = new PartyScreenLogic();
			PartyScreenLogicInitializationData data = new PartyScreenLogicInitializationData
			{
				LeftOwnerParty = null,
				RightOwnerParty = MobileParty.MainParty?.Party,
				LeftMemberRoster = leftMembers,
				LeftPrisonerRoster = emptyPrisoners,
				RightMemberRoster = current,
				RightPrisonerRoster = TroopRoster.CreateDummyTroopRoster(),
				LeftLeaderHero = null,
				RightLeaderHero = PartyBase.MainParty?.LeaderHero,
				LeftPartyMembersSizeLimit = Math.Max(leftMembers.TotalManCount + current.TotalManCount, limit),
				LeftPartyPrisonersSizeLimit = 0,
				RightPartyMembersSizeLimit = Math.Max(1, limit),
				RightPartyPrisonersSizeLimit = 0,
				LeftPartyName = new TextObject("可选健康成年人"),
				RightPartyName = rightName,
				TroopTransferableDelegate = EntryProfileTroopTransferableDelegate,
				CanTalkToTroopDelegate = null,
				PartyPresentationDoneButtonDelegate = EntryProfileDoneHandler,
				PartyPresentationDoneButtonConditionDelegate = EntryProfileDoneCondition,
				PartyPresentationCancelButtonActivateDelegate = null,
				PartyPresentationCancelButtonDelegate = null,
				PartyScreenClosedDelegate = OnEntryProfileScreenClosed,
				IsDismissMode = true,
				IsTroopUpgradesDisabled = true,
				Header = new TextObject("配置 " + GetProfileTitle(profileKind)),
				TransferHealthiesGetWoundedsFirst = true,
				ShowProgressBar = false,
				MemberTransferState = PartyScreenLogic.TransferState.Transferable,
				PrisonerTransferState = PartyScreenLogic.TransferState.NotTransferable,
				AccompanyingTransferState = PartyScreenLogic.TransferState.Transferable,
				PartyScreenMode = PartyScreenHelper.PartyScreenMode.Normal
			};
			logic.Initialize(data);
			PartyState state = Game.Current.GameStateManager.CreateState<PartyState>();
			state.PartyScreenLogic = logic;
			state.IsDonating = false;
			state.PartyScreenMode = PartyScreenHelper.PartyScreenMode.Normal;
			Game.Current.GameStateManager.PushState((GameState)(object)state, 0);
			InformationManager.DisplayMessage(new InformationMessage("【SETS】正在配置" + GetProfileTitle(profileKind) + "，只列出健康成年同伴和士兵。", Color.FromUint(InfoColor)));
			SettlementEntryTroopSelectionLog.Log("Profile selection opened. profile=" + profileKind + ", limit=" + limit + ", current=" + current.TotalManCount + ", selectable=" + leftMembers.TotalManCount);
		}
		catch (Exception ex)
		{
			_pendingProfileSelection = null;
			SettlementEntryTroopSelectionLog.Log("Open profile selection failed. error=" + ex);
			InformationManager.DisplayMessage(new InformationMessage("【SETS】打开随行配置界面失败。", Color.FromUint(WarningColor)));
		}
	}

	private static bool EntryProfileTroopTransferableDelegate(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty)
	{
		return IsConfigurableEntryCharacter(character);
	}

	private static bool EntryProfileDoneHandler(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty = null, PartyBase rightParty = null)
	{
		return true;
	}

	private static Tuple<bool, TextObject> EntryProfileDoneCondition(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum)
	{
		int selected = Math.Max(0, rightMemberRoster?.TotalManCount ?? 0);
		int limit = Math.Max(0, _pendingProfileSelection?.Limit ?? OtherSettlementEntryLimit);
		if (selected > limit)
		{
			TextObject text = new TextObject("随行配置不能超过 {LIMIT} 人。当前：{COUNT}");
			text.SetTextVariable("LIMIT", limit);
			text.SetTextVariable("COUNT", selected);
			return new Tuple<bool, TextObject>(false, text);
		}
		return new Tuple<bool, TextObject>(true, TextObject.GetEmpty());
	}

	private static void OnEntryProfileScreenClosed(PartyBase leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, bool fromCancel)
	{
		try
		{
			PendingProfileSelection selection = _pendingProfileSelection;
			_pendingProfileSelection = null;
			if (selection == null)
			{
				return;
			}
			if (fromCancel)
			{
				InformationManager.DisplayMessage(new InformationMessage("【SETS】已取消进城随行配置。", Color.FromUint(WarningColor)));
				return;
			}
			TroopRoster saved = ResolveProfileRosterForEntry(rightMemberRoster, MobileParty.MainParty?.MemberRoster, selection.Limit, out _, out _);
			SetProfileRoster(selection.ProfileKind, saved);
			int count = saved.TotalManCount;
			int free = Math.Max(0, selection.Limit - count);
			string message = "【SETS】" + GetProfileTitle(selection.ProfileKind) + "已保存 " + count + "/" + selection.Limit + "。";
			if (free > 0)
			{
				message += " 仍可带入 " + free + " 人，可按 U 重新配置城镇/城堡/村庄随行。";
			}
			InformationManager.DisplayMessage(new InformationMessage(message, Color.FromUint(free > 0 ? InfoColor : SuccessColor)));
			SettlementEntryTroopSelectionLog.Log("Profile saved. profile=" + selection.ProfileKind + ", count=" + count + ", limit=" + selection.Limit);
		}
		catch (Exception ex)
		{
			_pendingProfileSelection = null;
			SettlementEntryTroopSelectionLog.Log("Profile close failed. error=" + ex);
			InformationManager.DisplayMessage(new InformationMessage("【SETS】保存进城随行配置失败。", Color.FromUint(WarningColor)));
		}
	}

	private void OnMissionStarted(IMission mission)
	{
		try
		{
			PendingMissionEntry entry = _pendingMissionEntry;
			if (entry == null || mission is not Mission concreteMission)
			{
				return;
			}
			Settlement current = Settlement.CurrentSettlement ?? PlayerEncounter.LocationEncounter?.Settlement;
			if (!string.IsNullOrWhiteSpace(entry.SettlementId) && current?.StringId != entry.SettlementId)
			{
				SettlementEntryTroopSelectionLog.Log("Ignored mission start; settlement mismatch. expected=" + entry.SettlementId + ", live=" + SafeSettlementId(current));
				return;
			}
			if (concreteMission.GetMissionBehavior<SettlementEntryTroopSelectionMissionLogic>() == null)
			{
				concreteMission.AddMissionBehavior(new SettlementEntryTroopSelectionMissionLogic(entry));
				SettlementEntryTroopSelectionLog.Log("Added mission logic. settlement=" + entry.SettlementId + ", selected=" + (entry.SelectedRoster?.TotalManCount ?? 0));
			}
			_pendingMissionEntry = null;
		}
		catch (Exception ex)
		{
			SettlementEntryTroopSelectionLog.Log("OnMissionStarted failed. error=" + ex);
		}
	}

	private void OnCampaignTick(float dt)
	{
		PumpPendingPostMissionFlow("campaign_tick");
	}

	private void OnSetsMissionEnded(IMission mission)
	{
		PumpPendingPostMissionFlow("mission_ended");
	}

	private void OnSetsGameMenuOpened(MenuCallbackArgs args)
	{
		PumpPendingPostMissionFlow("game_menu_opened_" + SafeGameMenuId(args));
	}

	private static void PumpPendingPostMissionFlow(string source)
	{
		try
		{
			TryPumpPendingSettlementTakenMenu(source);
		}
		catch (Exception ex)
		{
			SettlementEntryTroopSelectionLog.Log("PumpPendingPostMissionFlow failed. source=" + (source ?? "") + ", error=" + ex);
		}
	}

	private static bool TryPumpPendingSettlementTakenMenu(string source)
	{
		if (_pendingVictoryMenuEntry == null)
		{
			return false;
		}
		if (Mission.Current != null)
		{
			return true;
		}
		Settlement settlement = Settlement.Find(_pendingVictoryMenuEntry.SettlementId);
		if (settlement == null)
		{
			SettlementEntryTroopSelectionLog.Log("Dropping pending SETS victory menu; settlement missing. settlement=" + (_pendingVictoryMenuEntry.SettlementId ?? "") + ", source=" + (source ?? ""));
			_pendingVictoryMenuEntry = null;
			return false;
		}
		if (!settlement.IsTown)
		{
			SettlementEntryTroopSelectionLog.Log("Dropping pending SETS victory menu; non-town SETS is blocked. settlement=" + SafeSettlementId(settlement) + ", source=" + (source ?? ""));
			_pendingVictoryMenuEntry = null;
			return false;
		}
		PendingSettlementVictoryMenuEntry pending = _pendingVictoryMenuEntry;
		_pendingVictoryMenuEntry = null;
		string bridgeSource = string.IsNullOrWhiteSpace(pending.Source) ? source : pending.Source;
		bool opened = SiegeAiInterventionBehavior.TryOpenSettlementEntryVictoryMenu(settlement, pending.SurvivingRoster, bridgeSource, !pending.SkipOwnershipTransfer, pending.SetsOwnedIncident, pending.SetsTownRiotKilledNotable);
		if (!opened)
		{
			_pendingVictoryMenuEntry = pending;
			SettlementEntryTroopSelectionLog.Log("Native settlement-taken menu bridge not ready; will retry. settlement=" + SafeSettlementId(settlement) + ", source=" + (source ?? "") + ", hasLocationEncounter=" + (PlayerEncounter.LocationEncounter != null));
			return true;
		}
		SettlementEntryTroopSelectionLog.Log("Opened native settlement-taken menu after SETS town victory. settlement=" + SafeSettlementId(settlement) + ", source=" + (source ?? ""));
		return true;
	}

	private static string SafeGameMenuId(MenuCallbackArgs args)
	{
		try
		{
			return args?.MenuContext?.GameMenu?.StringId ?? "null";
		}
		catch
		{
			return "unknown";
		}
	}

	private static void EnsureProfileRosters()
	{
		_ownSettlementProfile ??= TroopRoster.CreateDummyTroopRoster();
		_otherSettlementProfile ??= TroopRoster.CreateDummyTroopRoster();
	}

	private static int GetProfileLimit(EntryProfileKind profileKind)
	{
		return profileKind == EntryProfileKind.OwnSettlement ? OwnSettlementEntryLimit : OtherSettlementEntryLimit;
	}

	private static TroopRoster GetProfileRoster(EntryProfileKind profileKind)
	{
		EnsureProfileRosters();
		return profileKind == EntryProfileKind.OwnSettlement ? _ownSettlementProfile : _otherSettlementProfile;
	}

	private static void SetProfileRoster(EntryProfileKind profileKind, TroopRoster roster)
	{
		TroopRoster saved = CloneRoster(roster, GetProfileLimit(profileKind));
		if (profileKind == EntryProfileKind.OwnSettlement)
		{
			_ownSettlementProfile = saved;
		}
		else
		{
			_otherSettlementProfile = saved;
		}
	}

	private static string GetProfileTitle(EntryProfileKind profileKind)
	{
		return profileKind == EntryProfileKind.OwnSettlement ? "自有定居点随行" : "他方定居点随行";
	}

	private static string BuildProfileHint(EntryProfileKind profileKind)
	{
		try
		{
			int limit = GetProfileLimit(profileKind);
			TroopRoster live = ResolveProfileRosterForEntry(GetProfileRoster(profileKind), MobileParty.MainParty?.MemberRoster, limit, out int configured, out int unavailable);
			string relationText = profileKind == EntryProfileKind.OwnSettlement
				? "玩家直属领地；玩家为统治者时，同国附属领主领地也走这套。"
				: "非自有领地；玩家不是统治者时，同国领主城镇也走这套。";
			string hint = "已配置 " + configured + "/" + limit + "，当前健康可带入 " + (live?.TotalManCount ?? 0) + "/" + limit + "。" + relationText;
			if (unavailable > 0)
			{
				hint += " 有 " + unavailable + " 个配置名额因受伤/离队/未成年不可用。";
			}
			return hint;
		}
		catch
		{
			return "配置进城/城堡/村庄自动带入的同伴和士兵。";
		}
	}

	private static void ShowEntryReminder(Settlement settlement, EntryProfileKind profileKind, int limit, int selectedCount, int configuredCount, int unavailableCount)
	{
		try
		{
			if (selectedCount >= limit)
			{
				return;
			}
			string name = settlement?.Name?.ToString() ?? "定居点";
			int free = Math.Max(0, limit - selectedCount);
			string message;
			if (configuredCount <= 0)
			{
				message = "【SETS】" + GetProfileTitle(profileKind) + "尚未配置，本次进入 " + name + " 不会带随行。可按 U 进入进城随行配置，为城镇/城堡/村庄设置随行人员。";
			}
			else
			{
				message = "【SETS】本次进入 " + name + " 随行 " + selectedCount + "/" + limit + "，仍可带入 " + free + " 人。";
				if (unavailableCount > 0)
				{
					message += " 有 " + unavailableCount + " 个配置名额因受伤/离队/未成年空缺。";
				}
				message += " 可按 U 重新配置城镇/城堡/村庄随行。";
			}
			InformationManager.DisplayMessage(new InformationMessage(message, Color.FromUint(InfoColor)));
		}
		catch
		{
		}
	}

	private static TroopRoster BuildConfigSelectableRoster(TroopRoster sourceRoster)
	{
		TroopRoster roster = TroopRoster.CreateDummyTroopRoster();
		if (sourceRoster == null)
		{
			return roster;
		}
		for (int i = 0; i < sourceRoster.Count; i++)
		{
			TroopRosterElement item = sourceRoster.GetElementCopyAtIndex(i);
			CharacterObject character = item.Character;
			if (!IsConfigurableEntryCharacter(character) || item.Number <= 0)
			{
				continue;
			}
			int healthy = character.IsHero ? 1 : Math.Max(0, item.Number - item.WoundedNumber);
			if (healthy <= 0)
			{
				continue;
			}
			int xp = character.IsHero ? 0 : CalculateRosterXpToMove(item, healthy);
			roster.AddToCounts(character, healthy, false, 0, xp, true, -1);
		}
		return roster;
	}

	private static TroopRoster ResolveProfileRosterForEntry(TroopRoster profileRoster, TroopRoster liveRoster, int limit, out int configuredCount, out int unavailableCount)
	{
		TroopRoster resolved = TroopRoster.CreateDummyTroopRoster();
		configuredCount = 0;
		unavailableCount = 0;
		if (profileRoster == null || liveRoster == null || limit <= 0)
		{
			return resolved;
		}
		int remaining = limit;
		for (int i = 0; i < profileRoster.Count && remaining > 0; i++)
		{
			TroopRosterElement requested = profileRoster.GetElementCopyAtIndex(i);
			CharacterObject requestedCharacter = requested.Character;
			if (requestedCharacter == null || requested.Number <= 0)
			{
				continue;
			}
			int requestedNumber = Math.Min(Math.Max(0, requested.Number), remaining);
			configuredCount += requestedNumber;
			if (!TryFindRosterElement(liveRoster, requestedCharacter, out TroopRosterElement liveElement))
			{
				unavailableCount += requestedNumber;
				remaining -= requestedNumber;
				continue;
			}
			CharacterObject liveCharacter = liveElement.Character;
			if (!IsConfigurableEntryCharacter(liveCharacter))
			{
				unavailableCount += requestedNumber;
				remaining -= requestedNumber;
				continue;
			}
			int healthy = liveCharacter.IsHero ? 1 : Math.Max(0, liveElement.Number - liveElement.WoundedNumber);
			int move = Math.Min(requestedNumber, healthy);
			if (move > 0)
			{
				int xp = liveCharacter.IsHero ? 0 : CalculateRosterXpToMove(liveElement, move);
				resolved.AddToCounts(liveCharacter, move, false, 0, xp, true, -1);
				remaining -= move;
			}
			if (move < requestedNumber)
			{
				unavailableCount += requestedNumber - move;
				remaining -= requestedNumber - move;
			}
		}
		return resolved;
	}

	private static bool IsConfigurableEntryCharacter(CharacterObject character)
	{
		try
		{
			if (character == null || character.IsPlayerCharacter || character.IsNotTransferableInPartyScreen)
			{
				return false;
			}
			Hero hero = character.HeroObject;
			if (hero != null)
			{
				return hero != Hero.MainHero
					&& hero.PartyBelongedTo == MobileParty.MainParty
					&& !hero.IsPrisoner
					&& !hero.IsDead
					&& !hero.IsWounded
					&& hero.Age >= 18f;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsOwnEntrySettlement(Settlement settlement)
	{
		try
		{
			Clan playerClan = Clan.PlayerClan;
			Clan ownerClan = settlement?.OwnerClan;
			if (playerClan == null || ownerClan == null)
			{
				return false;
			}
			if (ownerClan == playerClan)
			{
				return true;
			}
			Kingdom playerKingdom = playerClan.Kingdom ?? Hero.MainHero?.Clan?.Kingdom;
			if (playerKingdom == null)
			{
				return false;
			}
			bool playerIsRuler = playerKingdom.RulingClan == playerClan || playerKingdom.Leader == Hero.MainHero;
			return playerIsRuler && ownerClan.Kingdom == playerKingdom;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryFindRosterElement(TroopRoster roster, CharacterObject character, out TroopRosterElement element)
	{
		if (roster != null && character != null)
		{
			for (int i = 0; i < roster.Count; i++)
			{
				TroopRosterElement current = roster.GetElementCopyAtIndex(i);
				if (CharactersMatch(current.Character, character))
				{
					element = current;
					return true;
				}
			}
		}
		element = default;
		return false;
	}

	private static bool CharactersMatch(CharacterObject left, CharacterObject right)
	{
		if (left == null || right == null)
		{
			return false;
		}
		if (left == right)
		{
			return true;
		}
		return !string.IsNullOrWhiteSpace(left.StringId) && string.Equals(left.StringId, right.StringId, StringComparison.OrdinalIgnoreCase);
	}

	private static void SubtractRoster(TroopRoster targetRoster, TroopRoster subtractRoster)
	{
		if (targetRoster == null || subtractRoster == null)
		{
			return;
		}
		for (int i = 0; i < subtractRoster.Count; i++)
		{
			TroopRosterElement item = subtractRoster.GetElementCopyAtIndex(i);
			if (item.Character != null && item.Number > 0)
			{
				TryRemoveFromRoster(targetRoster, item.Character, item.Number);
			}
		}
	}

	private static TroopRoster CloneRoster(TroopRoster sourceRoster, int maxCount)
	{
		TroopRoster clone = TroopRoster.CreateDummyTroopRoster();
		if (sourceRoster == null || maxCount <= 0)
		{
			return clone;
		}
		int remaining = maxCount;
		for (int i = 0; i < sourceRoster.Count && remaining > 0; i++)
		{
			TroopRosterElement item = sourceRoster.GetElementCopyAtIndex(i);
			CharacterObject character = item.Character;
			if (character == null || item.Number <= 0)
			{
				continue;
			}
			int number = Math.Min(remaining, Math.Max(0, item.Number));
			if (number <= 0)
			{
				continue;
			}
			int wounded = character.IsHero ? 0 : Math.Min(number, Math.Max(0, item.WoundedNumber));
			int xp = character.IsHero ? 0 : CalculateRosterXpToMove(item, number);
			clone.AddToCounts(character, number, false, wounded, xp, true, -1);
			remaining -= number;
		}
		return clone;
	}

	private static int CalculateRosterXpToMove(TroopRosterElement sourceElement, int numberToMove)
	{
		try
		{
			int number = Math.Max(0, sourceElement.Number);
			int xp = Math.Max(0, sourceElement.Xp);
			numberToMove = Math.Max(0, numberToMove);
			if (number <= 0 || xp <= 0 || numberToMove <= 0)
			{
				return 0;
			}
			if (numberToMove >= number)
			{
				return xp;
			}
			int result = (int)Math.Round((double)xp * numberToMove / number, MidpointRounding.AwayFromZero);
			return Math.Max(0, Math.Min(xp, result));
		}
		catch
		{
			return 0;
		}
	}

	private static string SafeSettlementId(Settlement settlement)
	{
		return settlement?.StringId ?? "null";
	}

	private sealed class PendingProfileSelection
	{
		public EntryProfileKind ProfileKind;
		public int Limit;
	}

	internal sealed class PendingMissionEntry
	{
		public string SettlementId;
		public TroopRoster SelectedRoster;
		public int Limit;
		public bool IsOwnSettlement;
		public string SceneKind;
	}

	private sealed class PendingSettlementVictoryMenuEntry
	{
		public string SettlementId;
		public TroopRoster SurvivingRoster;
		public string Source;
		public bool SkipOwnershipTransfer;
		public bool SetsOwnedIncident;
		public bool SetsTownRiotKilledNotable;
	}

	private sealed class DefenderReserveEntry
	{
		public CharacterObject Character;
		public TroopRoster SourceRoster;
		public PartyBase SourceParty;
		public string SourceKind;
	}

	private sealed class SettlementEntryTroopSelectionMissionLogic : MissionLogic
	{
		private readonly string _settlementId;
		private readonly int _limit;
		private readonly bool _isOwnSettlement;
		private readonly string _sceneKind;
		private readonly bool _conflictFeaturesEnabled;
		private readonly bool _defenderConflictEnabled;
		private readonly TroopRoster _selectedRoster;
		private readonly TroopRoster _survivingRoster;
		private readonly List<DefenderReserveEntry> _remainingDefenderReserve;
		private readonly HashSet<int> _alliedAgentIndexes = new HashSet<int>();
		private readonly HashSet<int> _enemyAgentIndexes = new HashSet<int>();
		private readonly HashSet<int> _ownedSettlementFleeingCivilianAgentIndexes = new HashSet<int>();
		private readonly HashSet<int> _victoryObjectiveEnemyAgentIndexes = new HashSet<int>();
		private readonly HashSet<int> _spawnedDefenderReserveAgentIndexes = new HashSet<int>();
		private readonly HashSet<int> _settledCasualtyAgentIndexes = new HashSet<int>();
		private readonly HashSet<int> _settledDefenderReserveAgentIndexes = new HashSet<int>();
		private readonly Dictionary<int, TroopRoster> _defenderReserveAgentSourceRosters = new Dictionary<int, TroopRoster>();
		private readonly Dictionary<int, int> _defenderReserveAgentWaveNumbers = new Dictionary<int, int>();
		private readonly Dictionary<int, float> _lastProtectedFollowerHealth = new Dictionary<int, float>();
		private readonly Dictionary<int, ProtectedFollowerFriendlyFireHitRecord> _recentProtectedFollowerFriendlyFireHits = new Dictionary<int, ProtectedFollowerFriendlyFireHitRecord>();
		private readonly Dictionary<int, int> _defenderReserveRecoveryCursorByWorkshopFrame = new Dictionary<int, int>();
		private readonly Dictionary<int, float> _enemyInitialTargetReleaseTimes = new Dictionary<int, float>();
		private readonly Dictionary<int, Vec3> _enemyWallPassProbePositions = new Dictionary<int, Vec3>();
		private readonly Dictionary<int, float> _enemyWallPassProbeTimes = new Dictionary<int, float>();
		private readonly Dictionary<int, float> _enemyWallPassLastStepTimes = new Dictionary<int, float>();
		private static readonly MethodInfo AgentSetTargetAgentMethod = AccessTools.Method(typeof(Agent), "SetTargetAgent", new[] { typeof(Agent) });
		private static readonly MethodInfo AgentSetAutomaticTargetSelectionMethod = AccessTools.Method(typeof(Agent), "SetAutomaticTargetSelection", new[] { typeof(bool) });
		private bool _spawnedAllies;
		private bool _enemyFormationChargeOrderIssued;
		private bool _conflictActive;
		private bool _ownedSettlementIncidentTriggered;
		private bool _townRiotKilledNotable;
		private bool _victoryReached;
		private bool _victoryQueued;
		private bool _victoryEndMissionRequested;
		private bool _politicalConsequenceApplied;
		private Team _playerTeam;
		private Team _enemyTeam;
		private Team _neutralTeam;
		private float _nextEnemyCheckTime;
		private float _protectedFollowerHostilitySuppressionUntil;
		private float _nextDefenderReserveWaveTime;
		private float _lastDefenderReserveProgressTime;
		private float _nextOwnedSettlementPanicTickTime;
		private float _victoryReachedTime = -1f;
		private int _lastDefenderReserveLiveEnemyCount = -1;
		private int _enemyWallPassRescueBudget;
		private bool _defenderReserveStuckNudged;
		private int _defenderReservePhaseIndex;
		private int _defenderReserveWaveIndex;

		private struct ProtectedFollowerFriendlyFireHitRecord
		{
			public int AffectorIndex;
			public float MissionTime;
			public float Damage;
		}

		public SettlementEntryTroopSelectionMissionLogic(PendingMissionEntry entry)
		{
			_settlementId = entry?.SettlementId ?? "";
			_limit = Math.Max(0, entry?.Limit ?? OtherSettlementEntryLimit);
			_isOwnSettlement = entry?.IsOwnSettlement ?? false;
			_sceneKind = string.IsNullOrWhiteSpace(entry?.SceneKind) ? "settlement" : entry.SceneKind;
			_conflictFeaturesEnabled = string.Equals(_sceneKind, "town", StringComparison.OrdinalIgnoreCase);
			_defenderConflictEnabled = _conflictFeaturesEnabled && !_isOwnSettlement;
			_selectedRoster = CloneRoster(entry?.SelectedRoster, _limit);
			_survivingRoster = CloneRoster(_selectedRoster, int.MaxValue);
			_remainingDefenderReserve = _defenderConflictEnabled ? BuildCurrentDefenderReserve(_settlementId) : new List<DefenderReserveEntry>();
		}

		public override void AfterStart()
		{
			base.AfterStart();
			SetSetsSelectedFollowerState(base.Mission, active: true, "after_start");
			TrySpawnSelectedAllies("AfterStart");
		}

		public override void OnMissionTick(float dt)
		{
			base.OnMissionTick(dt);
			if (!_spawnedAllies)
			{
				TrySpawnSelectedAllies("TickFallback");
			}
			MaintainProtectedFollowersFriendlyState();
			EnsureSetsCommandUiReadyForExternal(base.Mission, _conflictActive ? "tick_conflict" : "tick", force: false, preserveSelection: true);
			if (_conflictFeaturesEnabled && _ownedSettlementIncidentTriggered && base.Mission != null && base.Mission.CurrentTime >= _nextOwnedSettlementPanicTickTime)
			{
				_nextOwnedSettlementPanicTickTime = base.Mission.CurrentTime + 1f;
				MaintainOwnedSettlementIncidentPanic(force: false);
			}
			if (_defenderConflictEnabled && _victoryReached)
			{
				TryForceVictoryMissionEnd("tick");
			}
			if (!_defenderConflictEnabled || !_conflictActive || _victoryReached || base.Mission == null || base.Mission.CurrentTime < _nextEnemyCheckTime)
			{
				return;
			}
			_nextEnemyCheckTime = base.Mission.CurrentTime + 1f;
			MaintainConflictTeams();
			PruneNonObjectiveVictoryTracking("tick");
			RefreshEnemyNativeCombatOrders();
			int liveEnemyCount = CountLiveTrackedEnemies();
			ObserveDefenderReserveProgress(liveEnemyCount);
			if (TryRecoverStalledDefenderReserve(liveEnemyCount))
			{
				liveEnemyCount = CountLiveTrackedEnemies();
			}
			if (HasRemainingDefenderReserve())
			{
				TrySpawnTimedDefenderReserveWave();
				return;
			}
			if (liveEnemyCount <= 0)
			{
				ReachVictory("all_defenders_defeated");
				return;
			}
		}

		public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon attackerWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
		{
			base.OnAgentHit(affectedAgent, affectorAgent, in attackerWeapon, in blow, in attackCollisionData);
			if (IsProtectedFollowerFriendlyFire(affectedAgent, affectorAgent))
			{
				ProtectFollowerFromFriendlyFire(affectedAgent, affectorAgent, in blow);
				return;
			}
			CacheProtectedFollowerHealth(affectedAgent);
			if (_conflictFeaturesEnabled && _isOwnSettlement)
			{
				if (!_ownedSettlementIncidentTriggered && IsPlayerSideAgent(affectorAgent) && !IsPlayerSideAgent(affectedAgent) && IsOwnedSettlementIncidentTarget(affectedAgent))
				{
					StartOwnedSettlementIncident("player_side_hit_owned_settlement", affectedAgent);
				}
				else if (_ownedSettlementIncidentTriggered)
				{
					MaintainOwnedSettlementIncidentPanic(force: true);
				}
				return;
			}
			if (!_defenderConflictEnabled || _conflictActive || _victoryReached)
			{
				return;
			}
			bool affectorIsPlayerSide = IsPlayerSideAgent(affectorAgent);
			bool affectedIsPlayerSide = IsPlayerSideAgent(affectedAgent);
			if (affectorIsPlayerSide && !affectedIsPlayerSide && IsVictoryObjectiveSceneAgent(affectedAgent))
			{
				StartConflict("player_side_hit_guard", affectedAgent);
			}
			else if (affectedIsPlayerSide && !affectorIsPlayerSide && IsVictoryObjectiveSceneAgent(affectorAgent))
			{
				StartConflict("guard_hit_player_side", affectorAgent);
			}
		}

		public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
		{
			base.OnScoreHit(affectedAgent, affectorAgent, attackerWeapon, isBlocked, isSiegeEngineHit, in blow, in collisionData, damagedHp, hitDistance, shotDifficulty);
			if (IsProtectedFollowerFriendlyFire(affectedAgent, affectorAgent))
			{
				ProtectFollowerFromFriendlyFire(affectedAgent, affectorAgent, in blow);
				return;
			}
			if (damagedHp <= 0f || !_conflictFeaturesEnabled || !_isOwnSettlement || _ownedSettlementIncidentTriggered)
			{
				return;
			}
			if (IsPlayerSideAgent(affectorAgent) && !IsPlayerSideAgent(affectedAgent) && IsOwnedSettlementIncidentTarget(affectedAgent))
			{
				StartOwnedSettlementIncident("player_side_score_hit_owned_settlement", affectedAgent);
			}
		}

		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
		{
			base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, killingBlow);
			if (affectedAgent == null)
			{
				return;
			}
			_lastProtectedFollowerHealth.Remove(affectedAgent.Index);
			_recentProtectedFollowerFriendlyFireHits.Remove(affectedAgent.Index);
			_enemyInitialTargetReleaseTimes.Remove(affectedAgent.Index);
			ClearEnemyWallPassTracking(affectedAgent.Index);
			if (_conflictFeaturesEnabled && (_ownedSettlementIncidentTriggered || _conflictActive) && agentState == AgentState.Killed && IsPlayerSideAgent(affectorAgent) && !IsPlayerSideAgent(affectedAgent) && IsOwnedSettlementIncidentNotable(affectedAgent))
			{
				_townRiotKilledNotable = true;
				SettlementEntryTroopSelectionLog.Log("SETS town riot notable killed. settlement=" + _settlementId + ", troop=" + SafeCharacterId(affectedAgent.Character as CharacterObject));
			}
			if (_alliedAgentIndexes.Contains(affectedAgent.Index) && agentState == AgentState.Killed)
			{
				if (IsPlayerSideAgent(affectorAgent))
				{
					SettlementEntryTroopSelectionLog.Log("Suppressed SETS follower roster casualty caused by player-side friendly fire. troop=" + SafeCharacterId(affectedAgent.Character as CharacterObject));
					return;
				}
				SettleAlliedCasualty(affectedAgent);
			}
			if (_defenderConflictEnabled && _enemyAgentIndexes.Contains(affectedAgent.Index) && (agentState == AgentState.Killed || agentState == AgentState.Unconscious))
			{
				if (_spawnedDefenderReserveAgentIndexes.Contains(affectedAgent.Index))
				{
					SettleDefenderReserveDefeat(affectedAgent, "agent_removed_" + agentState);
				}
				_enemyAgentIndexes.Remove(affectedAgent.Index);
				_victoryObjectiveEnemyAgentIndexes.Remove(affectedAgent.Index);
				_spawnedDefenderReserveAgentIndexes.Remove(affectedAgent.Index);
				_defenderReserveAgentSourceRosters.Remove(affectedAgent.Index);
				_defenderReserveAgentWaveNumbers.Remove(affectedAgent.Index);
				RefreshSetsUsableProtectionState("agent_removed");
			}
		}

		protected override void OnEndMission()
		{
			if (_conflictFeaturesEnabled && (_victoryReached || _ownedSettlementIncidentTriggered))
			{
				QueueVictoryPostMissionFlow(_ownedSettlementIncidentTriggered ? "SETS_owned_or_attached_town_exit" : "SETS_town_victory_endmission_fallback");
			}
			ClearSetsUsableProtectionState("sets_mission_end");
			ClearSetsSelectedFollowerState("sets_mission_end");
			base.OnEndMission();
		}

		private void TrySpawnSelectedAllies(string source)
		{
			try
			{
				Mission mission = base.Mission;
				Agent main = Agent.Main ?? mission?.MainAgent;
				if (_spawnedAllies || mission == null || main == null || !main.IsActive())
				{
					return;
				}
				EnsurePlayerTeam(mission, main, requireCommandTeam: true);
				if (_playerTeam == null)
				{
					return;
				}
				List<CharacterObject> troops = ExpandRoster(_selectedRoster, _limit);
				SetSetsSelectedFollowerState(mission, active: true, "spawn_selected_allies");
				int spawned = SpawnAgentsNearPlayer(troops, _playerTeam, asEnemy: false, source);
				_spawnedAllies = true;
				if (spawned > 0)
				{
					TrySetFollowerFormationFollowOrder(mission, main);
					EnsureSetsCommandUiReadyForExternal(mission, "spawn_selected_allies", force: true, preserveSelection: false);
					string message = "【SETS】随行人员已进入场景，可按原版指挥调整。";
					if (_defenderConflictEnabled)
					{
						message += " 冲突爆发时他们会协助你。";
					}
					InformationManager.DisplayMessage(new InformationMessage(message, Color.FromUint(InfoColor)));
				}
				SettlementEntryTroopSelectionLog.Log("Spawned selected allies. settlement=" + _settlementId + ", scene=" + _sceneKind + ", source=" + source + ", selected=" + troops.Count + ", spawned=" + spawned + ", ownSettlement=" + _isOwnSettlement);
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("TrySpawnSelectedAllies failed. error=" + ex);
			}
		}

		internal bool IsOwnedOrAttachedTownEntryActive()
		{
			return _conflictFeaturesEnabled && _isOwnSettlement;
		}

		private void StartConflict(string source, Agent initialEnemy)
		{
			try
			{
				Mission mission = base.Mission;
				Agent main = Agent.Main ?? mission?.MainAgent;
				if (!_defenderConflictEnabled || mission == null || main == null || !main.IsActive())
				{
					return;
				}
				_conflictActive = true;
				ApplyTownRiotPoliticalConsequences(source);
				EnsurePlayerTeam(mission, main, requireCommandTeam: true);
				KeepPlayerEntryFollowersCommandable(refreshFormation: true);
				EnsureSetsCommandUiReadyForExternal(mission, "conflict_start", force: true, preserveSelection: true);
				EnsureEnemyTeam(mission);
				if (mission.Mode != MissionMode.Battle && mission.Mode != MissionMode.Conversation && mission.Mode != MissionMode.Barter)
				{
					mission.SetMissionMode(MissionMode.Battle, atStart: false);
				}
				MarkCurrentSceneGuardsEnemy(initialEnemy);
				MaintainConflictTeams();
				RefreshSetsUsableProtectionState("conflict_start");
				SpawnInitialDefenderReserveWave();
				InformationManager.DisplayMessage(new InformationMessage("【SETS内部暴乱】城镇守卫已进入敌对状态（第 0 波），随行士兵等待你的指挥。", Color.FromUint(WarningColor)));
				SettlementEntryTroopSelectionLog.Log("Conflict started. settlement=" + _settlementId + ", source=" + source + ", enemies=" + _enemyAgentIndexes.Count + ", followersCommandable=true, autoCharge=false");
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("StartConflict failed. error=" + ex);
			}
		}

		private void SpawnInitialDefenderReserveWave()
		{
			try
			{
				if (_defenderReserveWaveIndex > 0 || !HasRemainingDefenderReserve())
				{
					return;
				}
				SpawnDefenderReserveWave();
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("SpawnInitialDefenderReserveWave failed. error=" + ex);
			}
		}

		private void ApplyTownRiotPoliticalConsequences(string source)
		{
			if (_politicalConsequenceApplied)
			{
				return;
			}
			_politicalConsequenceApplied = true;
			try
			{
				Settlement settlement = Settlement.Find(_settlementId);
				Clan playerClan = Clan.PlayerClan;
				Clan ownerClan = settlement?.OwnerClan;
				if (settlement == null || playerClan == null || ownerClan == null || ownerClan == playerClan)
				{
					return;
				}
				Kingdom playerKingdom = playerClan.Kingdom ?? Hero.MainHero?.Clan?.Kingdom;
				if (playerKingdom != null && ownerClan.Kingdom == playerKingdom)
				{
					bool playerIsRuler = playerKingdom.RulingClan == playerClan || playerKingdom.Leader == Hero.MainHero;
					if (playerIsRuler)
					{
						SettlementEntryTroopSelectionLog.Log("Skipped hostility for ruler attacking an attached town; outcome relation penalty is deferred. settlement=" + _settlementId + ", ownerClan=" + (ownerClan.StringId ?? "null") + ", source=" + source);
						return;
					}
					ChangeKingdomAction.ApplyByLeaveWithRebellionAgainstKingdom(playerClan, true);
					InformationManager.DisplayMessage(new InformationMessage("【SETS内部暴乱】你攻击了本国城镇，已按保留土地财产的叛国处理。", Color.FromUint(WarningColor)));
					SettlementEntryTroopSelectionLog.Log("Applied vassal riot rebellion. settlement=" + _settlementId + ", oldKingdom=" + (playerKingdom.StringId ?? "null") + ", source=" + source);
					return;
				}
				IFaction playerFaction = Hero.MainHero?.MapFaction ?? playerClan;
				IFaction ownerFaction = ownerClan.Kingdom ?? (IFaction)ownerClan;
				if (playerFaction != null && ownerFaction != null && playerFaction != ownerFaction && !FactionManager.IsAtWarAgainstFaction(playerFaction, ownerFaction))
				{
					DeclareWarAction.ApplyByPlayerHostility(playerFaction, ownerFaction);
					InformationManager.DisplayMessage(new InformationMessage("【SETS内部暴乱】你袭击了该城镇，双方已进入敌对状态。", Color.FromUint(WarningColor)));
					SettlementEntryTroopSelectionLog.Log("Applied SETS town riot hostility. settlement=" + _settlementId + ", playerFaction=" + (playerFaction.StringId ?? "null") + ", ownerFaction=" + (ownerFaction.StringId ?? "null") + ", source=" + source);
				}
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("ApplyTownRiotPoliticalConsequences failed. settlement=" + _settlementId + ", source=" + source + ", error=" + ex);
			}
		}

		private void EnsurePlayerTeam(Mission mission, Agent main, bool requireCommandTeam)
		{
			_playerTeam = mission.PlayerTeam ?? main.Team;
			if (_playerTeam == null || (requireCommandTeam && !_playerTeam.IsPlayerGeneral))
			{
				try
				{
					uint color = Hero.MainHero?.MapFaction?.Color ?? 0xFF2020FFu;
					uint color2 = Hero.MainHero?.MapFaction?.Color2 ?? 0xFF101080u;
					_playerTeam = mission.Teams.Add(BattleSideEnum.Attacker, color, color2, Hero.MainHero?.Clan?.Banner, isPlayerGeneral: true, isPlayerSergeant: false);
					mission.PlayerTeam = _playerTeam;
				}
				catch
				{
					_playerTeam = mission.PlayerTeam ?? main.Team;
				}
			}
			else
			{
				mission.PlayerTeam = _playerTeam;
			}
			if (_playerTeam != null && main.Team != _playerTeam)
			{
				main.SetTeam(_playerTeam, true);
			}
		}

		private void EnsureEnemyTeam(Mission mission)
		{
			if (_enemyTeam == null || _enemyTeam == _playerTeam)
			{
				try
				{
					_enemyTeam = mission.Teams.Add(BattleSideEnum.Defender, 0xFF8B1A1Au, 0xFF3A0808u, Settlement.Find(_settlementId)?.OwnerClan?.Banner, isPlayerGeneral: false, isPlayerSergeant: false);
				}
				catch
				{
					_enemyTeam = mission.PlayerEnemyTeam;
				}
			}
			if (_enemyTeam != null && _playerTeam != null)
			{
				_enemyTeam.SetIsEnemyOf(_playerTeam, true);
				_playerTeam.SetIsEnemyOf(_enemyTeam, true);
			}
		}

		private void MarkCurrentSceneGuardsEnemy(Agent initialEnemy)
		{
			try
			{
				if (initialEnemy != null && !IsPlayerSideAgent(initialEnemy) && IsVictoryObjectiveSceneAgent(initialEnemy))
				{
					MarkEnemyAgent(initialEnemy);
				}
				foreach (Agent agent in base.Mission.Agents)
				{
					if (agent == null || !agent.IsHuman || !agent.IsActive())
					{
						continue;
					}
					if (IsPlayerSideAgent(agent))
					{
						continue;
					}
					if (IsVictoryObjectiveSceneAgent(agent))
					{
						MarkEnemyAgent(agent);
					}
					else
					{
						agent.SetWatchState(Agent.WatchState.Alarmed);
					}
				}
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("MarkCurrentSceneGuardsEnemy failed. error=" + ex.Message);
			}
		}

		private void MarkEnemyAgent(Agent agent)
		{
			MarkEnemyAgent(agent, victoryObjective: true);
		}

		private void MarkEnemyAgent(Agent agent, bool victoryObjective)
		{
			if (agent == null || _enemyTeam == null || IsPlayerSideAgent(agent))
			{
				return;
			}
			if (agent.Team != _enemyTeam)
			{
				agent.SetTeam(_enemyTeam, true);
			}
			agent.SetWatchState(Agent.WatchState.Alarmed);
			_enemyAgentIndexes.Add(agent.Index);
			if (victoryObjective)
			{
				_victoryObjectiveEnemyAgentIndexes.Add(agent.Index);
			}
			AssignEnemyAgentCombatTarget(agent, agent.Index);
		}

		private void MaintainConflictTeams()
		{
			try
			{
				if (_playerTeam != null && _enemyTeam != null)
				{
					_playerTeam.SetIsEnemyOf(_enemyTeam, true);
					_enemyTeam.SetIsEnemyOf(_playerTeam, true);
				}
				foreach (Agent agent in base.Mission.Agents)
				{
					if (agent == null || !agent.IsHuman || !agent.IsActive())
					{
						continue;
					}
					if (IsPlayerSideAgent(agent))
					{
						if (_playerTeam != null && agent.Team != _playerTeam)
						{
							agent.SetTeam(_playerTeam, true);
						}
						continue;
					}
					if (_enemyAgentIndexes.Contains(agent.Index) && _enemyTeam != null && agent.Team != _enemyTeam)
					{
						agent.SetTeam(_enemyTeam, true);
					}
				}
				EnsureEnemyFormationEngagesPlayer();
			}
			catch
			{
			}
		}

		private int CountLiveTrackedEnemies()
		{
			int count = 0;
			try
			{
				foreach (Agent agent in base.Mission.Agents)
				{
					if (IsLiveTrackedEnemy(agent))
					{
						count++;
					}
				}
			}
			catch
			{
			}
			return count;
		}

		private bool IsLiveTrackedEnemy(Agent agent)
		{
			return IsLiveTrackedCombatEnemy(agent)
				&& _victoryObjectiveEnemyAgentIndexes.Contains(agent.Index);
		}

		private bool IsLiveTrackedCombatEnemy(Agent agent)
		{
			return agent != null
				&& agent.IsHuman
				&& agent.IsActive()
				&& !IsPlayerSideAgent(agent)
				&& _enemyAgentIndexes.Contains(agent.Index)
				&& agent.State != AgentState.Killed
				&& agent.State != AgentState.Unconscious;
		}

		private int PruneNonObjectiveVictoryTracking(string reason)
		{
			try
			{
				Mission mission = base.Mission;
				if (mission == null || _enemyAgentIndexes.Count == 0)
				{
					return 0;
				}
				List<int> nonObjectiveTrackedEnemies = new List<int>();
				foreach (Agent agent in mission.Agents)
				{
					if (agent == null
						|| !agent.IsHuman
						|| !agent.IsActive()
						|| agent.State == AgentState.Killed
						|| agent.State == AgentState.Unconscious
						|| !_enemyAgentIndexes.Contains(agent.Index)
						|| _victoryObjectiveEnemyAgentIndexes.Contains(agent.Index)
						|| _spawnedDefenderReserveAgentIndexes.Contains(agent.Index)
						|| IsVictoryObjectiveSceneAgent(agent))
					{
						continue;
					}
					nonObjectiveTrackedEnemies.Add(agent.Index);
				}
				for (int i = 0; i < nonObjectiveTrackedEnemies.Count; i++)
				{
					int agentIndex = nonObjectiveTrackedEnemies[i];
					_enemyAgentIndexes.Remove(agentIndex);
					_victoryObjectiveEnemyAgentIndexes.Remove(agentIndex);
					_spawnedDefenderReserveAgentIndexes.Remove(agentIndex);
					_defenderReserveAgentSourceRosters.Remove(agentIndex);
					_defenderReserveAgentWaveNumbers.Remove(agentIndex);
					ClearEnemyWallPassTracking(agentIndex);
				}
				if (nonObjectiveTrackedEnemies.Count > 0)
				{
					SettlementEntryTroopSelectionLog.Log("Pruned non-objective SETS victory tracking only. settlement=" + _settlementId + ", reason=" + reason + ", count=" + nonObjectiveTrackedEnemies.Count);
					RefreshSetsUsableProtectionState("prune_non_objective_victory_tracking");
				}
				return nonObjectiveTrackedEnemies.Count;
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("PruneNonObjectiveVictoryTracking failed. reason=" + reason + ", error=" + ex.Message);
				return 0;
			}
		}

		private void StartOwnedSettlementIncident(string source, Agent initialTarget)
		{
			try
			{
				Mission mission = base.Mission;
				Agent main = Agent.Main ?? mission?.MainAgent;
				if (!_conflictFeaturesEnabled || !_isOwnSettlement || mission == null || main == null || !main.IsActive())
				{
					return;
				}
				_ownedSettlementIncidentTriggered = true;
				_conflictActive = false;
				_victoryReached = false;
				ApplyOwnedSettlementIncidentConsequences(source);
				EnsurePlayerTeam(mission, main, requireCommandTeam: true);
				KeepPlayerEntryFollowersCommandable(refreshFormation: true);
				EnsureSetsCommandUiReadyForExternal(mission, "owned_settlement_incident", force: true, preserveSelection: true);
				if (mission.Mode != MissionMode.Battle && mission.Mode != MissionMode.Conversation && mission.Mode != MissionMode.Barter)
				{
					mission.SetMissionMode(MissionMode.Battle, atStart: false);
				}
				MaintainOwnedSettlementIncidentPanic(force: true);
				ClearSetsUsableProtectionState("owned_settlement_incident");
				InformationManager.DisplayMessage(new InformationMessage("【SETS】自有/附属城镇事件已触发，平民正在逃散。可随时按 TAB 退出后进入 SETS 专用处置菜单。", Color.FromUint(WarningColor)));
				SettlementEntryTroopSelectionLog.Log("Owned/attached settlement incident started. settlement=" + _settlementId + ", source=" + (source ?? "") + ", target=" + SafeCharacterId(initialTarget?.Character as CharacterObject));
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("StartOwnedSettlementIncident failed. settlement=" + _settlementId + ", source=" + (source ?? "") + ", error=" + ex);
			}
		}

		private void ApplyOwnedSettlementIncidentConsequences(string source)
		{
			if (_politicalConsequenceApplied)
			{
				return;
			}
			_politicalConsequenceApplied = true;
			SettlementEntryTroopSelectionLog.Log("Deferred owned/attached settlement incident relation penalty to post-mission SETS/GCCZ outcome. settlement=" + _settlementId + ", source=" + (source ?? ""));
		}

		private void MaintainOwnedSettlementIncidentPanic(bool force)
		{
			try
			{
				Mission mission = base.Mission;
				Agent main = Agent.Main ?? mission?.MainAgent;
				if (mission == null || main == null || !main.IsActive())
				{
					return;
				}
				Team neutralTeam = EnsureNeutralTeam(mission);
				int fleeing = 0;
				int neutralized = 0;
				foreach (Agent agent in mission.Agents)
				{
					if (agent == null || !agent.IsHuman || !agent.IsActive() || IsPlayerSideAgent(agent))
					{
						continue;
					}
					NeutralizeOwnedSettlementNonPlayerAgent(agent, neutralTeam);
					neutralized++;
					if (IsOwnedSettlementCivilian(agent))
					{
						ForceOwnedSettlementCivilianFlee(agent, mission, main, force || _ownedSettlementFleeingCivilianAgentIndexes.Add(agent.Index));
						fleeing++;
					}
				}
				if (force)
				{
					SettlementEntryTroopSelectionLog.Log("Maintained owned/attached settlement panic. settlement=" + _settlementId + ", fleeing=" + fleeing + ", neutralized=" + neutralized);
				}
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("MaintainOwnedSettlementIncidentPanic failed. settlement=" + _settlementId + ", error=" + ex.Message);
			}
		}

		private void NeutralizeOwnedSettlementNonPlayerAgent(Agent agent, Team neutralTeam)
		{
			try
			{
				if (agent == null || !agent.IsHuman || !agent.IsActive() || IsPlayerSideAgent(agent))
				{
					return;
				}
				agent.ResetEnemyCaches();
				agent.InvalidateTargetAgent();
				AgentSetAutomaticTargetSelectionMethod?.Invoke(agent, new object[] { false });
				agent.SetWatchState(IsOwnedSettlementCivilian(agent) ? Agent.WatchState.Alarmed : Agent.WatchState.Patrolling);
				if (neutralTeam != null && agent.Team != neutralTeam)
				{
					agent.SetTeam(neutralTeam, true);
				}
			}
			catch
			{
			}
		}

		private void ForceOwnedSettlementCivilianFlee(Agent agent, Mission mission, Agent main, bool force)
		{
			try
			{
				if (!force && !_ownedSettlementFleeingCivilianAgentIndexes.Contains(agent.Index))
				{
					return;
				}
				agent.SetLookAgent(null);
				agent.SetMaximumSpeedLimit(-1f, isMultiplier: false);
				agent.SetCrouchMode(false);
				agent.InvalidateTargetAgent();
				agent.SetWatchState(Agent.WatchState.Alarmed);
				if (!TryForceOwnedSettlementCivilianDirectRetreat(agent, mission, main))
				{
					ActivateOwnedSettlementCivilianFleeBehavior(agent);
				}
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("ForceOwnedSettlementCivilianFlee failed. agent=" + agent?.Index + ", error=" + ex.Message);
			}
		}

		private bool TryForceOwnedSettlementCivilianDirectRetreat(Agent agent, Mission mission, Agent main)
		{
			try
			{
				CampaignAgentComponent component = agent?.GetComponent<CampaignAgentComponent>();
				AgentNavigator navigator = component?.AgentNavigator ?? component?.CreateAgentNavigator();
				if (agent == null || mission?.Scene == null || main == null || !main.IsActive() || navigator == null)
				{
					return false;
				}
				Vec2 origin = agent.Position.AsVec2;
				Vec2 away = origin - main.Position.AsVec2;
				if (away.LengthSquared < 0.04f)
				{
					away = agent.Frame.rotation.f.AsVec2;
				}
				if (away.LengthSquared < 0.04f)
				{
					away = new Vec2(1f, 0f);
				}
				away.Normalize();
				WorldPosition best = WorldPosition.Invalid;
				float bestScore = float.MinValue;
				for (int i = 0; i < 16; i++)
				{
					Vec3 candidate = mission.GetRandomPositionAroundPoint(agent.Position, 6f, 24f, i % 2 == 0);
					WorldPosition world = new WorldPosition(mission.Scene, candidate);
					if (world.GetNearestNavMesh() == UIntPtr.Zero)
					{
						continue;
					}
					Vec2 delta = world.AsVec2 - origin;
					if (delta.LengthSquared < 0.25f)
					{
						continue;
					}
					delta.Normalize();
					float directionScore = Vec2.DotProduct(delta, away);
					if (directionScore < 0.1f)
					{
						continue;
					}
					float score = world.AsVec2.DistanceSquared(main.Position.AsVec2) + directionScore * 25f;
					if (score > bestScore)
					{
						best = world;
						bestScore = score;
					}
				}
				if (bestScore <= 0f)
				{
					return false;
				}
				Vec2 bestDelta = best.AsVec2 - origin;
				float rotation = bestDelta.LengthSquared > 0.04f ? bestDelta.RotationInRadians : away.RotationInRadians;
				navigator.SetTargetFrame(best, rotation, 0.6f, -10f, Agent.AIScriptedFrameFlags.NoAttack | Agent.AIScriptedFrameFlags.NeverSlowDown, false);
				return true;
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("TryForceOwnedSettlementCivilianDirectRetreat failed. agent=" + agent?.Index + ", error=" + ex.Message);
				return false;
			}
		}

		private void ActivateOwnedSettlementCivilianFleeBehavior(Agent agent)
		{
			try
			{
				CampaignAgentComponent component = agent?.GetComponent<CampaignAgentComponent>();
				AgentNavigator navigator = component?.AgentNavigator ?? component?.CreateAgentNavigator();
				if (agent == null || navigator == null)
				{
					return;
				}
				if (navigator.GetBehaviorGroup<DailyBehaviorGroup>() == null)
				{
					try
					{
						navigator.AddBehaviorGroup<DailyBehaviorGroup>();
					}
					catch
					{
					}
				}
				if (navigator.GetBehaviorGroup<InterruptingBehaviorGroup>() == null)
				{
					try
					{
						navigator.AddBehaviorGroup<InterruptingBehaviorGroup>();
					}
					catch
					{
					}
				}
				AlarmedBehaviorGroup alarmedGroup = navigator.GetBehaviorGroup<AlarmedBehaviorGroup>() ?? navigator.AddBehaviorGroup<AlarmedBehaviorGroup>();
				if (alarmedGroup == null)
				{
					return;
				}
				alarmedGroup.DisableCalmDown = true;
				FleeBehavior fleeBehavior = alarmedGroup.GetBehavior<FleeBehavior>() ?? alarmedGroup.AddBehavior<FleeBehavior>();
				if (fleeBehavior != null)
				{
					alarmedGroup.SetScriptedBehavior<FleeBehavior>();
				}
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("ActivateOwnedSettlementCivilianFleeBehavior failed. agent=" + agent?.Index + ", error=" + ex.Message);
			}
		}

		private void RefreshEnemyNativeCombatOrders()
		{
			try
			{
				_enemyWallPassRescueBudget = EnemyWallPassRescueBudgetPerRefresh;
				foreach (Agent agent in base.Mission.Agents)
				{
					if (IsLiveTrackedEnemy(agent))
					{
						MaintainEnemyAgentNativeCombat(agent);
					}
				}
				EnsureEnemyFormationEngagesPlayer();
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("RefreshEnemyNativeCombatOrders failed. error=" + ex.Message);
			}
		}

		private void AssignEnemyAgentCombatTarget(Agent agent, int seed)
		{
			try
			{
				if (!IsLiveTrackedEnemy(agent))
				{
					return;
				}
				if (_enemyTeam != null && agent.Team != _enemyTeam)
				{
					agent.SetTeam(_enemyTeam, true);
				}
				agent.SetWatchState(Agent.WatchState.Alarmed);
				AssignAgentToFormation(agent, _enemyTeam, FormationClass.Infantry);
				Agent target = SelectPlayerSideTarget(seed);
				if (target != null && AgentSetTargetAgentMethod != null)
				{
					agent.ResetEnemyCaches();
					agent.InvalidateTargetAgent();
					AgentSetAutomaticTargetSelectionMethod?.Invoke(agent, new object[] { false });
					AgentSetTargetAgentMethod.Invoke(agent, new object[] { target });
					_enemyInitialTargetReleaseTimes[agent.Index] = (base.Mission?.CurrentTime ?? 0f) + EnemyInitialTargetLockSeconds;
				}
				else
				{
					_enemyInitialTargetReleaseTimes.Remove(agent.Index);
					agent.InvalidateTargetAgent();
					agent.ClearTargetFrame();
					AgentSetTargetAgentMethod?.Invoke(agent, new object[] { null });
					AgentSetAutomaticTargetSelectionMethod?.Invoke(agent, new object[] { true });
				}
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("AssignEnemyAgentCombatTarget failed. agent=" + agent?.Index + ", error=" + ex.Message);
			}
		}

		private void MaintainEnemyAgentNativeCombat(Agent agent)
		{
			try
			{
				if (!IsLiveTrackedEnemy(agent))
				{
					return;
				}
				if (_enemyTeam != null && agent.Team != _enemyTeam)
				{
					agent.SetTeam(_enemyTeam, true);
				}
				agent.SetWatchState(Agent.WatchState.Alarmed);
				Formation enemyFormation = _enemyTeam?.GetFormation(FormationClass.Infantry);
				if (enemyFormation != null && agent.Formation != enemyFormation)
				{
					AssignAgentToFormation(agent, _enemyTeam, FormationClass.Infantry);
				}
				TryMaintainEnemyWallPassRescue(agent);
				if (!_enemyInitialTargetReleaseTimes.TryGetValue(agent.Index, out float releaseTime))
				{
					return;
				}
				if ((base.Mission?.CurrentTime ?? 0f) < releaseTime)
				{
					return;
				}
				agent.ResetEnemyCaches();
				agent.InvalidateTargetAgent();
				agent.ClearTargetFrame();
				AgentSetTargetAgentMethod?.Invoke(agent, new object[] { null });
				AgentSetAutomaticTargetSelectionMethod?.Invoke(agent, new object[] { true });
				_enemyInitialTargetReleaseTimes.Remove(agent.Index);
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("MaintainEnemyAgentNativeCombat failed. agent=" + agent?.Index + ", error=" + ex.Message);
			}
		}

		private void TryMaintainEnemyWallPassRescue(Agent agent)
		{
			try
			{
				Mission mission = base.Mission;
				Agent target = FindNearestPlayerSideTarget(agent);
				if (!IsLiveTrackedEnemy(agent) || mission?.Scene == null || target == null || !target.IsActive())
				{
					ClearEnemyWallPassTracking(agent?.Index ?? -1);
					return;
				}
				float distanceSq = agent.Position.DistanceSquared(target.Position);
				float minTargetDistanceSq = SiegeAgentWallRescueProfile.WallPassTeleportMinDistance * SiegeAgentWallRescueProfile.WallPassTeleportMinDistance;
				if (distanceSq <= minTargetDistanceSq)
				{
					ClearEnemyWallPassTracking(agent.Index);
					return;
				}
				float now = mission.CurrentTime;
				if (!_enemyWallPassProbeTimes.TryGetValue(agent.Index, out float lastProbeTime))
				{
					_enemyWallPassProbePositions[agent.Index] = agent.Position;
					_enemyWallPassProbeTimes[agent.Index] = now;
					return;
				}
				if (now - lastProbeTime < SiegeAgentWallRescueProfile.ProbeSeconds)
				{
					return;
				}
				Vec3 lastPosition = _enemyWallPassProbePositions.TryGetValue(agent.Index, out Vec3 probePosition) ? probePosition : agent.Position;
				_enemyWallPassProbePositions[agent.Index] = agent.Position;
				_enemyWallPassProbeTimes[agent.Index] = now;
				float minMovedSq = SiegeAgentWallRescueProfile.MinMovedDistance * SiegeAgentWallRescueProfile.MinMovedDistance;
				if (agent.Position.DistanceSquared(lastPosition) >= minMovedSq)
				{
					return;
				}
				if (_enemyWallPassLastStepTimes.TryGetValue(agent.Index, out float lastStepTime)
					&& now - lastStepTime < SiegeAgentWallRescueProfile.WallPassTeleportCooldownSeconds)
				{
					return;
				}
				if (_enemyWallPassRescueBudget <= 0)
				{
					return;
				}
				_enemyWallPassRescueBudget--;
				if (TryApplyEnemyWallPassStep(agent, target, out Vec3 rescuePosition))
				{
					_enemyWallPassLastStepTimes[agent.Index] = now;
					_enemyWallPassProbePositions[agent.Index] = rescuePosition;
					_enemyWallPassProbeTimes[agent.Index] = now;
					AssignEnemyAgentCombatTarget(agent, agent.Index);
					SettlementEntryTroopSelectionLog.Log("Applied SETS enemy wall-pass rescue. settlement=" + _settlementId + ", agent=" + agent.Index + ", target=" + target.Index + ", position=" + rescuePosition);
				}
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("SETS enemy wall-pass rescue failed. agent=" + agent?.Index + ", error=" + ex.Message);
			}
		}

		private Agent FindNearestPlayerSideTarget(Agent source)
		{
			Agent nearest = null;
			float nearestDistanceSq = float.MaxValue;
			try
			{
				if (source == null || base.Mission?.Agents == null)
				{
					return Agent.Main ?? base.Mission?.MainAgent;
				}
				foreach (Agent candidate in base.Mission.Agents)
				{
					if (candidate == null || !candidate.IsHuman || !candidate.IsActive() || !IsPlayerSideAgent(candidate))
					{
						continue;
					}
					float distanceSq = source.Position.DistanceSquared(candidate.Position);
					if (distanceSq < nearestDistanceSq)
					{
						nearestDistanceSq = distanceSq;
						nearest = candidate;
					}
				}
			}
			catch
			{
			}
			return nearest ?? Agent.Main ?? base.Mission?.MainAgent;
		}

		private bool TryApplyEnemyWallPassStep(Agent agent, Agent target, out Vec3 rescuePosition)
		{
			rescuePosition = agent?.Position ?? Vec3.Zero;
			try
			{
				Mission mission = base.Mission;
				Scene scene = mission?.Scene;
				if (agent == null || target == null || scene == null)
				{
					return false;
				}
				Vec3 direction = target.Position - agent.Position;
				direction.z = 0f;
				float distance = direction.Normalize();
				if (distance <= SiegeAgentWallRescueProfile.WallPassTeleportMinDistance)
				{
					return false;
				}
				float maxStep = Math.Min(SiegeAgentWallRescueProfile.NativeTargetFrameSampleMaxRadius, distance - 1f);
				float maxStepSq = maxStep * maxStep;
				Vec3 stepCenter = agent.Position + direction * maxStep;
				Vec3 bestPosition = agent.Position;
				float currentTargetDistanceSq = agent.Position.DistanceSquared(target.Position);
				float bestScore = float.MinValue;
				for (int i = 0; i < SiegeAgentWallRescueProfile.NativeTargetFrameSampleCount; i++)
				{
					Vec3 candidate = i == 0
						? stepCenter
						: mission.GetRandomPositionAroundPoint(stepCenter, 0.35f, 2.25f, i % 2 == 0);
					candidate.z = scene.GetGroundHeightAtPosition(candidate, BodyFlags.CommonCollisionExcludeFlags);
					WorldPosition candidateWorld = new WorldPosition(scene, candidate);
					if (candidateWorld.GetNearestNavMesh() == UIntPtr.Zero)
					{
						continue;
					}
					candidate = candidateWorld.GetNavMeshVec3();
					float movedSq = candidate.DistanceSquared(agent.Position);
					float candidateTargetDistanceSq = candidate.DistanceSquared(target.Position);
					if (movedSq < 0.25f || movedSq > maxStepSq || candidateTargetDistanceSq >= currentTargetDistanceSq - 0.25f)
					{
						continue;
					}
					Vec3 candidateDirection = candidate - agent.Position;
					candidateDirection.z = 0f;
					float directionScore = candidateDirection.LengthSquared > 0.01f ? Vec2.DotProduct(candidateDirection.AsVec2.Normalized(), direction.AsVec2) : -1f;
					float score = currentTargetDistanceSq - candidateTargetDistanceSq + directionScore * 4f;
					if (score > bestScore)
					{
						bestScore = score;
						bestPosition = candidate;
					}
				}
				if (bestScore == float.MinValue)
				{
					return false;
				}
				agent.DisableScriptedMovement();
				agent.ClearTargetFrame();
				agent.InvalidateTargetAgent();
				agent.TeleportToPosition(bestPosition);
				rescuePosition = bestPosition;
				return true;
			}
			catch
			{
				return false;
			}
		}

		private void ClearEnemyWallPassTracking(int agentIndex)
		{
			if (agentIndex < 0)
			{
				return;
			}
			_enemyWallPassProbePositions.Remove(agentIndex);
			_enemyWallPassProbeTimes.Remove(agentIndex);
			_enemyWallPassLastStepTimes.Remove(agentIndex);
		}

		private Agent SelectPlayerSideTarget(int seed)
		{
			List<Agent> targets = new List<Agent>();
			try
			{
				foreach (Agent agent in base.Mission.Agents)
				{
					if (agent != null && agent.IsHuman && agent.IsActive() && IsPlayerSideAgent(agent) && agent.State != AgentState.Killed && agent.State != AgentState.Unconscious)
					{
						targets.Add(agent);
					}
				}
			}
			catch
			{
			}
			if (targets.Count <= 0)
			{
				return Agent.Main ?? base.Mission?.MainAgent;
			}
			int index = Math.Abs(seed % targets.Count);
			return targets[index];
		}

		private void EnsureEnemyFormationEngagesPlayer()
		{
			try
			{
				if (_enemyFormationChargeOrderIssued)
				{
					return;
				}
				Formation enemyFormation = _enemyTeam?.GetFormation(FormationClass.Infantry);
				if (enemyFormation == null)
				{
					return;
				}
				enemyFormation.SetMovementOrder(MovementOrder.MovementOrderCharge);
				enemyFormation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
				_enemyFormationChargeOrderIssued = true;
			}
			catch
			{
			}
		}

		private void ObserveDefenderReserveProgress(int liveEnemyCount)
		{
			if (!_conflictFeaturesEnabled || !_conflictActive || _victoryReached || _defenderReserveWaveIndex <= 0 || base.Mission == null)
			{
				return;
			}
			float now = base.Mission.CurrentTime;
			if (_lastDefenderReserveLiveEnemyCount < 0 || _lastDefenderReserveProgressTime <= 0f)
			{
				ResetDefenderReserveProgress(liveEnemyCount, "observe_init");
				return;
			}
			if (liveEnemyCount != _lastDefenderReserveLiveEnemyCount)
			{
				ResetDefenderReserveProgress(liveEnemyCount, "live_count_changed");
			}
		}

		private void ResetDefenderReserveProgress(int liveEnemyCount, string source)
		{
			_lastDefenderReserveLiveEnemyCount = liveEnemyCount;
			_lastDefenderReserveProgressTime = base.Mission?.CurrentTime ?? 0f;
			_defenderReserveStuckNudged = false;
			SettlementEntryTroopSelectionLog.Log("Defender reserve progress reset. settlement=" + _settlementId + ", source=" + source + ", wave=" + _defenderReserveWaveIndex + ", activeWaves=" + CountActiveDefenderReserveWaves() + ", liveEnemies=" + liveEnemyCount + ", time=" + _lastDefenderReserveProgressTime.ToString("0.0"));
		}

		private bool TryRecoverStalledDefenderReserve(int liveEnemyCount)
		{
			try
			{
				Mission mission = base.Mission;
				if (!_conflictFeaturesEnabled || !_conflictActive || _victoryReached || _defenderReserveWaveIndex <= 0 || mission == null || liveEnemyCount <= 0 || _lastDefenderReserveProgressTime <= 0f)
				{
					return false;
				}
				float noProgressSeconds = mission.CurrentTime - _lastDefenderReserveProgressTime;
				if (!_defenderReserveStuckNudged && noProgressSeconds >= DefenderReserveStuckNudgeSeconds)
				{
					_defenderReserveStuckNudged = true;
					RefreshEnemyNativeCombatOrders();
					SettlementEntryTroopSelectionLog.Log("Defender reserve native charge nudge applied. settlement=" + _settlementId + ", wave=" + _defenderReserveWaveIndex + ", activeWaves=" + CountActiveDefenderReserveWaves() + ", liveEnemies=" + liveEnemyCount + ", noProgressSeconds=" + noProgressSeconds.ToString("0.0"));
					return false;
				}
				if (noProgressSeconds >= DefenderReserveStuckRetrySeconds)
				{
					int repositioned = RepositionLiveTrackedEnemiesAtReserveSpawns("SETS_wave_stuck_retry");
					RefreshEnemyNativeCombatOrders();
					ResetDefenderReserveProgress(CountLiveTrackedEnemies(), "stuck_retry");
					SettlementEntryTroopSelectionLog.Log("Defender reserve wave spawn retry applied. settlement=" + _settlementId + ", wave=" + _defenderReserveWaveIndex + ", activeWaves=" + CountActiveDefenderReserveWaves() + ", liveEnemies=" + liveEnemyCount + ", repositioned=" + repositioned + ", noProgressSeconds=" + noProgressSeconds.ToString("0.0"));
					return false;
				}
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("TryRecoverStalledDefenderReserve failed. error=" + ex.Message);
			}
			return false;
		}

		private int RepositionLiveTrackedEnemiesAtReserveSpawns(string reason)
		{
			try
			{
				Mission mission = base.Mission;
				Agent main = Agent.Main ?? mission?.MainAgent;
				if (mission == null)
				{
					return 0;
				}
				if (!TryGetEnemyReserveSpawnFrames(out List<MatrixFrame> spawnFrames, out string spawnSource))
				{
					SettlementEntryTroopSelectionLog.Log("Skipped defender reserve spawn retry; no workshop or fallback spawn. settlement=" + _settlementId + ", reason=" + reason);
					return 0;
				}
				List<Agent> enemies = new List<Agent>();
				foreach (Agent agent in mission.Agents)
				{
					if (IsLiveTrackedEnemy(agent))
					{
						enemies.Add(agent);
					}
				}
				if (enemies.Count <= 0)
				{
					return 0;
				}
				bool workshopSpawn = string.Equals(spawnSource, "workshop", StringComparison.OrdinalIgnoreCase);
				List<KeyValuePair<Agent, int>> recoveryTargets = new List<KeyValuePair<Agent, int>>();
				if (workshopSpawn)
				{
					for (int frameIndex = 0; frameIndex < spawnFrames.Count; frameIndex++)
					{
						List<Agent> frameEnemies = new List<Agent>();
						for (int enemyIndex = 0; enemyIndex < enemies.Count; enemyIndex++)
						{
							if (SelectEnemyReserveSpawnFrameIndex(enemyIndex, spawnFrames.Count, spawnSource) == frameIndex)
							{
								frameEnemies.Add(enemies[enemyIndex]);
							}
						}
						if (frameEnemies.Count <= 0)
						{
							continue;
						}
						_defenderReserveRecoveryCursorByWorkshopFrame.TryGetValue(frameIndex, out int cursor);
						recoveryTargets.Add(new KeyValuePair<Agent, int>(frameEnemies[cursor % frameEnemies.Count], frameIndex));
						_defenderReserveRecoveryCursorByWorkshopFrame[frameIndex] = cursor + 1;
					}
				}
				else
				{
					for (int i = 0; i < enemies.Count; i++)
					{
						recoveryTargets.Add(new KeyValuePair<Agent, int>(enemies[i], SelectEnemyReserveSpawnFrameIndex(i, spawnFrames.Count, spawnSource)));
					}
				}
				int moved = 0;
				for (int i = 0; i < recoveryTargets.Count; i++)
				{
					Agent agent = recoveryTargets[i].Key;
					MatrixFrame spawnFrame = spawnFrames[recoveryTargets[i].Value];
					Vec3 position = ResolveEnemyReserveSpawnPosition(spawnFrame, i, spawnSource);
					if (mission.Scene != null)
					{
						position.z = mission.Scene.GetGroundHeightAtPosition(position);
					}
					agent.TeleportToPosition(position);
					Vec3 fallbackForward = spawnFrame.rotation.f;
					fallbackForward.z = 0f;
					if (fallbackForward.LengthSquared < 0.01f)
					{
						fallbackForward = Vec3.Forward;
					}
					fallbackForward.Normalize();
					Vec3 direction = main != null && main.IsActive() ? main.Position - position : fallbackForward * -1f;
					direction.z = 0f;
					if (direction.LengthSquared < 0.01f)
					{
						direction = fallbackForward * -1f;
					}
					direction.Normalize();
					Vec2 moveDirection = direction.AsVec2;
					agent.SetMovementDirection(in moveDirection);
					AssignEnemyAgentCombatTarget(agent, agent.Index + i);
					moved++;
				}
				SettlementEntryTroopSelectionLog.Log("Repositioned live tracked enemies at reserve spawns. settlement=" + _settlementId + ", source=" + spawnSource + ", reason=" + reason + ", moved=" + moved + ", deferred=" + Math.Max(0, enemies.Count - moved));
				return moved;
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("RepositionLiveTrackedEnemiesAtReserveSpawns failed. reason=" + reason + ", error=" + ex.Message);
				return 0;
			}
		}

		private void NeutralizeLiveTrackedEnemies(string reason)
		{
			try
			{
				Mission mission = base.Mission;
				if (mission == null)
				{
					return;
				}
				List<Agent> enemies = new List<Agent>();
				foreach (Agent agent in mission.Agents)
				{
					if (IsLiveTrackedCombatEnemy(agent))
					{
						enemies.Add(agent);
					}
				}
				Team neutralTeam = EnsureNeutralTeam(mission);
				for (int i = 0; i < enemies.Count; i++)
				{
					Agent agent = enemies[i];
					if (_spawnedDefenderReserveAgentIndexes.Contains(agent.Index))
					{
						SettleDefenderReserveDefeat(agent, reason);
					}
					NeutralizeEnemyAgent(agent, neutralTeam);
					_enemyAgentIndexes.Remove(agent.Index);
					_victoryObjectiveEnemyAgentIndexes.Remove(agent.Index);
					_spawnedDefenderReserveAgentIndexes.Remove(agent.Index);
					_defenderReserveAgentSourceRosters.Remove(agent.Index);
					_defenderReserveAgentWaveNumbers.Remove(agent.Index);
					ClearEnemyWallPassTracking(agent.Index);
				}
				SettlementEntryTroopSelectionLog.Log("Neutralized live tracked enemies. reason=" + reason + ", count=" + enemies.Count);
				RefreshSetsUsableProtectionState("neutralize_enemies");
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("NeutralizeLiveTrackedEnemies failed. reason=" + reason + ", error=" + ex.Message);
			}
		}

		private Team EnsureNeutralTeam(Mission mission)
		{
			if (_neutralTeam != null)
			{
				return _neutralTeam;
			}
			try
			{
				_neutralTeam = mission.Teams.Add(BattleSideEnum.None, NeutralColor, NeutralColor2, null, isPlayerGeneral: false, isPlayerSergeant: false);
				if (_playerTeam != null)
				{
					_neutralTeam.SetIsEnemyOf(_playerTeam, false);
					_playerTeam.SetIsEnemyOf(_neutralTeam, false);
				}
				if (_enemyTeam != null)
				{
					_neutralTeam.SetIsEnemyOf(_enemyTeam, false);
					_enemyTeam.SetIsEnemyOf(_neutralTeam, false);
				}
			}
			catch
			{
				_neutralTeam = null;
			}
			return _neutralTeam;
		}

		private static void NeutralizeEnemyAgent(Agent agent, Team neutralTeam)
		{
			try
			{
				if (agent == null || !agent.IsActive())
				{
					return;
				}
				agent.ResetEnemyCaches();
				agent.InvalidateTargetAgent();
				AgentSetAutomaticTargetSelectionMethod?.Invoke(agent, new object[] { true });
				agent.SetWatchState(Agent.WatchState.Patrolling);
				if (neutralTeam != null && agent.Team != neutralTeam)
				{
					agent.SetTeam(neutralTeam, true);
				}
			}
			catch
			{
			}
		}

		internal bool TryGatherOwnedOrAttachedTownSceneAgents(int seedAgentIndex, string source)
		{
			try
			{
				Mission mission = base.Mission;
				Agent main = Agent.Main ?? mission?.MainAgent;
				if (!IsOwnedOrAttachedTownEntryActive() || mission?.Agents == null || main == null || !main.IsActive())
				{
					return false;
				}
				EnsurePlayerTeam(mission, main, requireCommandTeam: true);
				KeepPlayerEntryFollowersCommandable(refreshFormation: true);
				int gathered = 0;
				foreach (Agent agent in mission.Agents.ToList())
				{
					if (agent == null || !agent.IsHuman || !agent.IsActive() || agent == main || agent.IsMainAgent)
					{
						continue;
					}
					if (IsPlayerSideAgent(agent))
					{
						AssignAgentToFormation(agent, _playerTeam, FormationClass.Infantry);
						gathered++;
						continue;
					}
					if (ShoutBehavior.TryForceSceneFollowPlayerForExternal(agent.Index, transient: true, reason: "sets_owned_attached_gather"))
					{
						gathered++;
					}
				}
				TrySetFollowerFormationFollowOrder(mission, main);
				EnsureSetsCommandUiReadyForExternal(mission, "owned_attached_gather", force: true, preserveSelection: true);
				SettlementEntryTroopSelectionLog.Log("SETS owned/attached town gather executed. settlement=" + _settlementId + ", source=" + (source ?? "") + ", seed=" + seedAgentIndex + ", gathered=" + gathered);
				return gathered > 0;
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("TryGatherOwnedOrAttachedTownSceneAgents failed. settlement=" + _settlementId + ", source=" + (source ?? "") + ", error=" + ex.Message);
				return false;
			}
		}

		private void KeepPlayerEntryFollowersCommandable(bool refreshFormation)
		{
			try
			{
				if (base.Mission == null || _playerTeam == null)
				{
					return;
				}
				foreach (Agent agent in base.Mission.Agents)
				{
					if (agent == null || !agent.IsHuman || !agent.IsActive() || !IsPlayerSideAgent(agent))
					{
						continue;
					}
					if (agent.Team != _playerTeam)
					{
						agent.SetTeam(_playerTeam, true);
					}
					if (refreshFormation && agent != Agent.Main && _alliedAgentIndexes.Contains(agent.Index))
					{
						AssignAgentToFormation(agent, _playerTeam, FormationClass.Infantry);
					}
				}
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("Keep player entry followers commandable failed. error=" + ex.Message);
			}
		}

		private void RefreshSetsUsableProtectionState(string source)
		{
			SetSetsUsableProtectionState(base.Mission, _defenderConflictEnabled && _conflictActive && !_victoryReached, _alliedAgentIndexes, _enemyAgentIndexes, source);
		}

		private bool HasRemainingDefenderReserve()
		{
			return _remainingDefenderReserve != null
				&& GetCurrentDefenderReservePhaseKind() != null;
		}

		private void TrySpawnTimedDefenderReserveWave()
		{
			try
			{
				if (!HasRemainingDefenderReserve() || base.Mission == null)
				{
					return;
				}
				if (base.Mission.CurrentTime < _nextDefenderReserveWaveTime)
				{
					return;
				}
				int activeWaveCount = CountActiveDefenderReserveWaves();
				if (activeWaveCount >= MaxActiveDefenderReserveWaves)
				{
					return;
				}
				SpawnDefenderReserveWave();
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("TrySpawnTimedDefenderReserveWave failed. error=" + ex.Message);
			}
		}

		private int CountActiveDefenderReserveWaves()
		{
			try
			{
				if (base.Mission == null || _defenderReserveAgentWaveNumbers.Count == 0)
				{
					return 0;
				}
				HashSet<int> activeWaves = new HashSet<int>();
				foreach (Agent agent in base.Mission.Agents)
				{
					if (IsLiveTrackedEnemy(agent) && _defenderReserveAgentWaveNumbers.TryGetValue(agent.Index, out int waveNumber) && waveNumber > 0)
					{
						activeWaves.Add(waveNumber);
					}
				}
				return activeWaves.Count;
			}
			catch
			{
				return 0;
			}
		}

		private void SpawnDefenderReserveWave()
		{
			try
			{
				EnsureEnemyTeam(base.Mission);
				string phaseKind = GetCurrentDefenderReservePhaseKind();
				if (phaseKind == null)
				{
					return;
				}
				List<DefenderReserveEntry> defenders = PeekDefenderReserve(DefenderReserveWaveSize, phaseKind);
				List<CharacterObject> troops = ExtractCharacters(defenders);
				int waveNumber = _defenderReserveWaveIndex + 1;
				int spawned = SpawnAgentsNearPlayer(troops, _enemyTeam, asEnemy: true, "defender_reserve_wave_" + waveNumber + "_" + phaseKind, null, defenders, waveNumber);
				RemoveDefenderReserveEntries(defenders);
				_defenderReserveWaveIndex++;
				_nextDefenderReserveWaveTime = (base.Mission?.CurrentTime ?? 0f) + DefenderReserveWaveIntervalSeconds;
				RefreshSetsUsableProtectionState("defender_reserve_wave");
				ResetDefenderReserveProgress(CountLiveTrackedEnemies(), "defender_reserve_wave_" + waveNumber);
				RefreshEnemyNativeCombatOrders();
				if (spawned > 0)
				{
					InformationManager.DisplayMessage(new InformationMessage("【SETS内部暴乱】" + GetDefenderReservePhaseDisplayName(phaseKind) + "从城镇工坊区加入镇压（第 " + waveNumber + " 波，场上最多 " + MaxActiveDefenderReserveWaves + " 波）。", Color.FromUint(WarningColor)));
				}
				SettlementEntryTroopSelectionLog.Log("Spawned defender reserve wave. settlement=" + _settlementId + ", wave=" + waveNumber + ", activeWaves=" + CountActiveDefenderReserveWaves() + "/" + MaxActiveDefenderReserveWaves + ", phase=" + phaseKind + ", requested=" + troops.Count + ", spawned=" + spawned + ", skipped=" + Math.Max(0, troops.Count - spawned) + ", remainingTotal=" + (_remainingDefenderReserve?.Count ?? 0) + ", nextWaveTime=" + _nextDefenderReserveWaveTime.ToString("0.0"));
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("SpawnDefenderReserveWave failed. error=" + ex);
			}
		}

		private string GetCurrentDefenderReservePhaseKind()
		{
			while (_defenderReservePhaseIndex < DefenderReservePhaseCount)
			{
				string phaseKind = GetDefenderReservePhaseKind(_defenderReservePhaseIndex);
				if (HasDefenderReservePhaseEntries(phaseKind))
				{
					return phaseKind;
				}
				_defenderReservePhaseIndex++;
			}
			return null;
		}

		private bool HasDefenderReservePhaseEntries(string phaseKind)
		{
			if (_remainingDefenderReserve == null || string.IsNullOrWhiteSpace(phaseKind))
			{
				return false;
			}
			for (int i = 0; i < _remainingDefenderReserve.Count; i++)
			{
				if (IsDefenderReservePhaseEntry(_remainingDefenderReserve[i], phaseKind))
				{
					return true;
				}
			}
			return false;
		}

		private List<DefenderReserveEntry> PeekDefenderReserve(int maxCount, string phaseKind)
		{
			List<DefenderReserveEntry> entries = new List<DefenderReserveEntry>();
			if (_remainingDefenderReserve == null || maxCount <= 0 || string.IsNullOrWhiteSpace(phaseKind))
			{
				return entries;
			}
			for (int i = 0; i < _remainingDefenderReserve.Count && entries.Count < maxCount; i++)
			{
				if (_remainingDefenderReserve[i]?.Character != null && IsDefenderReservePhaseEntry(_remainingDefenderReserve[i], phaseKind))
				{
					entries.Add(_remainingDefenderReserve[i]);
				}
			}
			return entries;
		}

		private static string GetDefenderReservePhaseKind(int phaseIndex)
		{
			switch (phaseIndex)
			{
				case 0:
					return "garrison";
				case 1:
					return "militia";
				case 2:
					return "lord_party";
				default:
					return null;
			}
		}

		private static bool IsDefenderReservePhaseEntry(DefenderReserveEntry entry, string phaseKind)
		{
			if (entry == null || string.IsNullOrWhiteSpace(phaseKind))
			{
				return false;
			}
			if (string.Equals(phaseKind, "lord_party", StringComparison.OrdinalIgnoreCase))
			{
				return string.Equals(entry.SourceKind, "lord_party", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(entry.SourceKind, "owner_hero", StringComparison.OrdinalIgnoreCase);
			}
			return string.Equals(entry.SourceKind, phaseKind, StringComparison.OrdinalIgnoreCase);
		}

		private static string GetDefenderReservePhaseDisplayName(string phaseKind)
		{
			if (string.Equals(phaseKind, "garrison", StringComparison.OrdinalIgnoreCase))
			{
				return "城镇驻军";
			}
			if (string.Equals(phaseKind, "militia", StringComparison.OrdinalIgnoreCase))
			{
				return "城镇民兵";
			}
			return "敌对领主部队";
		}

		private void RemoveDefenderReserveEntries(List<DefenderReserveEntry> entries)
		{
			if (_remainingDefenderReserve == null || entries == null || entries.Count <= 0)
			{
				return;
			}
			for (int i = 0; i < entries.Count; i++)
			{
				_remainingDefenderReserve.Remove(entries[i]);
			}
		}

		private static List<CharacterObject> ExtractCharacters(List<DefenderReserveEntry> entries)
		{
			List<CharacterObject> troops = new List<CharacterObject>();
			if (entries == null)
			{
				return troops;
			}
			for (int i = 0; i < entries.Count; i++)
			{
				if (entries[i]?.Character != null)
				{
					troops.Add(entries[i].Character);
				}
			}
			return troops;
		}

		private bool TryGetEnemyReserveSpawnFrames(out List<MatrixFrame> frames, out string spawnSource)
		{
			if (TryGetTownWorkshopSpawnFrames(out frames))
			{
				spawnSource = "workshop";
				return true;
			}
			if (TryGetLordHallDoorSpawnFrame(out MatrixFrame fallbackFrame))
			{
				frames = new List<MatrixFrame> { fallbackFrame };
				spawnSource = "lord_hall_fallback";
				return true;
			}
			frames = null;
			spawnSource = "none";
			return false;
		}

		private bool TryGetTownWorkshopSpawnFrames(out List<MatrixFrame> frames)
		{
			frames = new List<MatrixFrame>();
			try
			{
				Mission mission = base.Mission;
				Settlement settlement = Settlement.Find(_settlementId);
				if (mission == null || settlement?.IsTown != true)
				{
					return false;
				}
				List<WorkshopAreaMarker> markers = mission.ActiveMissionObjects?
					.FindAllWithType<WorkshopAreaMarker>()?
					.Where(marker => marker != null && marker.AreaIndex > 0 && marker.GameEntity != null)
					.OrderBy(marker => marker.AreaIndex)
					.ToList();
				if (markers == null || markers.Count == 0)
				{
					return false;
				}
				for (int i = 0; i < markers.Count; i++)
				{
					WorkshopAreaMarker marker = markers[i];
					if (marker.GameEntity.HasTag("workshop_area_marker") == false)
					{
						continue;
					}
					try
					{
						if (marker.GetWorkshop()?.WorkshopType?.IsHidden == true)
						{
							continue;
						}
					}
					catch
					{
					}
					MatrixFrame frame = marker.GameEntity.GetGlobalFrame();
					frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
					if (!TryResolveReachableWorkshopSpawnAnchor(mission, frame.origin, out Vec3 workshopAnchor))
					{
						SettlementEntryTroopSelectionLog.Log("Skipped town workshop spawn marker without a clear path to player. settlement=" + _settlementId + ", area=" + marker.AreaIndex);
						continue;
					}
					frame.origin = workshopAnchor;
					if (frame.origin.LengthSquared > 0.01f)
					{
						frames.Add(frame);
					}
				}
				if (frames.Count > 0)
				{
					SettlementEntryTroopSelectionLog.Log("Resolved town workshop spawn frames. settlement=" + _settlementId + ", count=" + frames.Count);
					return true;
				}
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("Resolve town workshop spawn frames failed. settlement=" + _settlementId + ", error=" + ex.Message);
			}
			return false;
		}

		private static bool TryResolveReachableWorkshopSpawnAnchor(Mission mission, Vec3 markerOrigin, out Vec3 anchor)
		{
			anchor = markerOrigin;
			try
			{
				Scene scene = mission?.Scene;
				Agent main = Agent.Main ?? mission?.MainAgent;
				if (scene == null || main == null || !main.IsActive())
				{
					return false;
				}
				markerOrigin.z = scene.GetGroundHeightAtPosition(markerOrigin);
				List<Vec3> candidates = new List<Vec3>();
				PathFaceRecord markerFace = PathFaceRecord.NullFaceRecord;
				scene.GetNavMeshFaceIndex(ref markerFace, markerOrigin, true);
				if (markerFace.IsValid())
				{
					Vec3 faceCenter = markerOrigin;
					scene.GetNavMeshCenterPosition(markerFace.FaceIndex, ref faceCenter);
					if (faceCenter.DistanceSquared(markerOrigin) <= 64f)
					{
						candidates.Add(faceCenter);
					}
				}
				candidates.Add(markerOrigin);
				for (int i = 0; i < 8; i++)
				{
					candidates.Add(mission.GetRandomPositionAroundPoint(markerOrigin, 0.8f, 4f, true));
				}
				WorldPosition playerWorld = new WorldPosition(scene, main.Position);
				if (playerWorld.GetNearestNavMesh() == UIntPtr.Zero)
				{
					return false;
				}
				float bestScore = float.MinValue;
				Vec3 bestAnchor = markerOrigin;
				for (int i = 0; i < candidates.Count; i++)
				{
					Vec3 candidate = candidates[i];
					candidate.z = scene.GetGroundHeightAtPosition(candidate);
					WorldPosition candidateWorld = new WorldPosition(scene, candidate);
					if (candidateWorld.GetNearestNavMesh() == UIntPtr.Zero
						|| !scene.GetPathDistanceBetweenPositions(ref candidateWorld, ref playerWorld, 0.45f, out float pathDistance))
					{
						continue;
					}
					candidate = candidateWorld.GetNavMeshVec3();
					int clearance = CountWorkshopAnchorClearDirections(scene, candidateWorld, candidate);
					if (clearance < 3)
					{
						continue;
					}
					float score = clearance * 100f - pathDistance * 0.01f - candidate.Distance(markerOrigin) * 0.1f;
					if (score > bestScore)
					{
						bestScore = score;
						bestAnchor = candidate;
					}
				}
				if (bestScore == float.MinValue)
				{
					return false;
				}
				anchor = bestAnchor;
				return true;
			}
			catch
			{
				return false;
			}
		}

		private static int CountWorkshopAnchorClearDirections(Scene scene, WorldPosition anchorWorld, Vec3 anchor)
		{
			int clearDirections = 0;
			Vec2[] directions =
			{
				new Vec2(1f, 0f),
				new Vec2(-1f, 0f),
				new Vec2(0f, 1f),
				new Vec2(0f, -1f)
			};
			for (int i = 0; i < directions.Length; i++)
			{
				Vec3 probe = anchor + new Vec3(directions[i] * 1.2f);
				probe.z = scene.GetGroundHeightAtPosition(probe);
				WorldPosition probeWorld = new WorldPosition(scene, probe);
				if (probeWorld.GetNearestNavMesh() != UIntPtr.Zero && scene.IsLineToPointClear(ref anchorWorld, ref probeWorld, 0.45f))
				{
					clearDirections++;
				}
			}
			return clearDirections;
		}

		private static int SelectEnemyReserveSpawnFrameIndex(int troopIndex, int frameCount, string spawnSource)
		{
			if (frameCount <= 1)
			{
				return 0;
			}
			if (string.Equals(spawnSource, "workshop", StringComparison.OrdinalIgnoreCase))
			{
				return Math.Min(frameCount - 1, Math.Max(0, troopIndex / DefenderReserveWorkshopSpawnGroupSize));
			}
			return 0;
		}

		private Vec3 ResolveEnemyReserveSpawnPosition(MatrixFrame spawnFrame, int troopIndex, string spawnSource)
		{
			if (string.Equals(spawnSource, "workshop", StringComparison.OrdinalIgnoreCase))
			{
				Vec3 workshopForward = spawnFrame.rotation.f;
				workshopForward.z = 0f;
				if (workshopForward.LengthSquared < 0.01f)
				{
					workshopForward = Vec3.Forward;
				}
				workshopForward.Normalize();
				Vec3 workshopRight = Vec3.CrossProduct(workshopForward, Vec3.Up);
				if (workshopRight.LengthSquared < 0.01f)
				{
					workshopRight = Vec3.Side;
				}
				workshopRight.Normalize();
				int workshopLocalIndex = Math.Abs(troopIndex % DefenderReserveWorkshopSpawnGroupSize);
				int workshopRow = workshopLocalIndex / DefenderReserveWorkshopGridColumns;
				int workshopColumn = workshopLocalIndex % DefenderReserveWorkshopGridColumns;
				float workshopForwardOffset = (workshopRow - 0.5f) * DefenderReserveWorkshopGridRowSpacing;
				float workshopLateralOffset = (workshopColumn - (DefenderReserveWorkshopGridColumns - 1) * 0.5f) * DefenderReserveWorkshopGridLateralSpacing;
				Vec3 gridOffset = workshopForward * workshopForwardOffset + workshopRight * workshopLateralOffset;
				for (int i = 0; i < 3; i++)
				{
					float projectionScale = i == 0 ? 1f : (i == 1 ? 0.7f : 0.45f);
					if (TryProjectWorkshopSpawnPosition(spawnFrame.origin, spawnFrame.origin + gridOffset * projectionScale, out Vec3 projectedPosition))
					{
						return projectedPosition;
					}
				}
				Mission mission = base.Mission;
				for (int i = 0; i < 3 && mission != null; i++)
				{
					Vec3 fallbackCandidate = mission.GetRandomPositionAroundPoint(spawnFrame.origin, 0.45f, 1.4f, true);
					if (TryProjectWorkshopSpawnPosition(spawnFrame.origin, fallbackCandidate, out Vec3 projectedPosition))
					{
						return projectedPosition;
					}
				}
				for (int i = 0; i < 6 && mission?.Scene != null; i++)
				{
					Vec3 navMeshFallback = mission.GetRandomPositionAroundPoint(spawnFrame.origin, 0.45f, 2.4f, true);
					WorldPosition fallbackWorld = new WorldPosition(mission.Scene, navMeshFallback);
					if (fallbackWorld.GetNearestNavMesh() != UIntPtr.Zero && navMeshFallback.DistanceSquared(spawnFrame.origin) > 0.04f)
					{
						return fallbackWorld.GetNavMeshVec3();
					}
				}
				return spawnFrame.origin;
			}
			Vec3 forward = spawnFrame.rotation.f;
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
			int columns = SpawnGridColumns;
			int localIndex = troopIndex;
			int row = localIndex / columns;
			int column = localIndex % columns;
			float lateralIndex = column - (columns - 1) * 0.5f;
			float forwardDistance = EnemyDoorSpawnBaseDistance + row * EnemyDoorSpawnRowDistance;
			float lateralDistance = lateralIndex * EnemyDoorSpawnLateralSpacing;
			return spawnFrame.origin + forward * forwardDistance + right * lateralDistance;
		}

		private bool TryProjectWorkshopSpawnPosition(Vec3 anchor, Vec3 candidate, out Vec3 projectedPosition)
		{
			projectedPosition = candidate;
			try
			{
				Scene scene = base.Mission?.Scene;
				if (scene == null)
				{
					return false;
				}
				anchor.z = scene.GetGroundHeightAtPosition(anchor);
				candidate.z = scene.GetGroundHeightAtPosition(candidate);
				WorldPosition anchorWorld = new WorldPosition(scene, anchor);
				WorldPosition candidateWorld = new WorldPosition(scene, candidate);
				if (anchorWorld.GetNearestNavMesh() == UIntPtr.Zero || candidateWorld.GetNearestNavMesh() == UIntPtr.Zero)
				{
					return false;
				}
				if (!scene.IsLineToPointClear(ref anchorWorld, ref candidateWorld, 0.4f))
				{
					return false;
				}
				projectedPosition = candidateWorld.GetNavMeshVec3();
				return true;
			}
			catch
			{
				return false;
			}
		}

		private bool TryGetLordHallDoorSpawnFrame(out MatrixFrame frame)
		{
			frame = MatrixFrame.Identity;
			try
			{
				Mission mission = base.Mission;
				Location lordHall = LocationComplex.Current?.GetLocationWithId(LordHallLocationId);
				MissionLocationLogic locationLogic = mission?.GetMissionBehavior<MissionLocationLogic>();
				if (lordHall != null && locationLogic != null)
				{
					frame = locationLogic.GetSpawnFrameOfPassage(lordHall);
					if (frame.origin.LengthSquared > 0.01f)
					{
						return true;
					}
				}
				MissionAgentHandler agentHandler = mission?.GetMissionBehavior<MissionAgentHandler>();
				if (TryGetLordHallDoorSpawnFrameFromPassages(agentHandler?.TownPassageProps, out frame)
					|| TryGetLordHallDoorSpawnFrameFromPassages(agentHandler?.DisabledPassages, out frame))
				{
					return true;
				}
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("Resolve lord hall door spawn failed. settlement=" + _settlementId + ", error=" + ex.Message);
			}
			return false;
		}

		private bool TryGetLordHallDoorSpawnFrameFromPassages(List<UsableMachine> passages, out MatrixFrame frame)
		{
			frame = MatrixFrame.Identity;
			if (passages == null)
			{
				return false;
			}
			for (int i = 0; i < passages.Count; i++)
			{
				Passage passage = passages[i] as Passage;
				if (passage == null)
				{
					continue;
				}
				Location toLocation = passage.ToLocation;
				if (toLocation == null || !string.Equals(toLocation.StringId, LordHallLocationId, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				try
				{
					frame = passage.PilotStandingPoint.GameEntity.GetGlobalFrame();
					frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
					if (base.Mission?.Scene != null)
					{
						frame.origin.z = base.Mission.Scene.GetGroundHeightAtPosition(frame.origin);
					}
					frame.rotation.RotateAboutUp((float)Math.PI);
					return frame.origin.LengthSquared > 0.01f;
				}
				catch
				{
					return false;
				}
			}
			return false;
		}

		private int SpawnAgentsNearPlayer(List<CharacterObject> troops, Team team, bool asEnemy, string source, List<CharacterObject> spawnedCharacters = null, List<DefenderReserveEntry> defenderEntries = null, int defenderReserveWaveNumber = -1)
		{
			Mission mission = base.Mission;
			Agent main = Agent.Main ?? mission?.MainAgent;
			PartyBase fallbackOriginParty = asEnemy ? Settlement.Find(_settlementId)?.Town?.GarrisonParty?.Party : PartyBase.MainParty;
			int entryCount = defenderEntries != null ? defenderEntries.Count : (troops?.Count ?? 0);
			if (mission == null || main == null || team == null || entryCount == 0)
			{
				return 0;
			}
			int spawned = 0;
			Vec3 anchor = main.Position;
			Vec3 forward = main.LookDirection;
			List<MatrixFrame> enemyReserveSpawnFrames = null;
			string enemyReserveSpawnSource = null;
			bool useEnemyReserveSpawnFrames = asEnemy && TryGetEnemyReserveSpawnFrames(out enemyReserveSpawnFrames, out enemyReserveSpawnSource);
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
			Formation formation = team.GetFormation(FormationClass.Infantry);
			for (int i = 0; i < entryCount; i++)
			{
				DefenderReserveEntry defenderEntry = defenderEntries != null && i < defenderEntries.Count ? defenderEntries[i] : null;
				CharacterObject troop = defenderEntry?.Character ?? troops[i];
				if (troop == null)
				{
					continue;
				}
				PartyBase originParty = defenderEntry?.SourceParty ?? fallbackOriginParty;
				if (!asEnemy && TryAdoptExistingSelectedHeroAgent(troop, team, source, out Agent adoptedAgent))
				{
					spawned++;
					spawnedCharacters?.Add(troop);
					continue;
				}
				int row = i / SpawnGridColumns;
				int column = i % SpawnGridColumns;
				float lateralIndex = column - (SpawnGridColumns - 1) * 0.5f;
				float enemyBaseDistance = EnemyDoorSpawnBaseDistance;
				float enemyRowDistance = EnemyDoorSpawnRowDistance;
				float enemyLateralSpacing = EnemyDoorSpawnLateralSpacing;
				float forwardDistance = (asEnemy ? enemyBaseDistance : AlliedSpawnBaseDistance) + row * (asEnemy ? enemyRowDistance : AlliedSpawnRowDistance);
				float lateralDistance = lateralIndex * (asEnemy ? enemyLateralSpacing : AlliedSpawnLateralSpacing);
				Vec3 position = asEnemy
					? anchor + forward * forwardDistance + right * lateralDistance
					: anchor - forward * forwardDistance + right * lateralDistance;
				if (useEnemyReserveSpawnFrames)
				{
					MatrixFrame spawnFrame = enemyReserveSpawnFrames[SelectEnemyReserveSpawnFrameIndex(i, enemyReserveSpawnFrames.Count, enemyReserveSpawnSource)];
					position = ResolveEnemyReserveSpawnPosition(spawnFrame, i, enemyReserveSpawnSource);
				}
				Vec3 direction = asEnemy ? (main.Position - position) : forward;
				direction.z = 0f;
				if (direction.LengthSquared < 0.01f)
				{
					direction = asEnemy ? forward * -1f : forward;
				}
				direction.Normalize();
				try
				{
					if (mission.Scene != null)
					{
						position.z = mission.Scene.GetGroundHeightAtPosition(position);
					}
					IAgentOriginBase origin = originParty != null ? new PartyAgentOrigin(originParty, troop) : null;
					AgentBuildData buildData = new AgentBuildData(troop)
						.Team(team)
						.Monster(TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(troop.Race, "_settlement"))
						.InitialPosition(in position)
						.InitialDirection(direction.AsVec2.Normalized())
						.Controller(AgentControllerType.AI)
						.CivilianEquipment(false)
						.NoHorses(true);
					if (origin != null)
					{
						buildData = buildData.TroopOrigin(origin);
					}
					if (formation != null)
					{
						buildData = buildData.Formation(formation)
							.FormationTroopSpawnCount(entryCount)
							.FormationTroopSpawnIndex(i)
							.SpawnsIntoOwnFormation(true)
							.SpawnsUsingOwnTroopClass(false);
					}
					Agent spawnedAgent = mission.SpawnAgent(buildData, false);
					if (spawnedAgent == null)
					{
						continue;
					}
					spawned++;
					spawnedCharacters?.Add(troop);
					spawnedAgent.SetWatchState(asEnemy ? Agent.WatchState.Alarmed : Agent.WatchState.Patrolling);
					if (asEnemy)
					{
						_enemyAgentIndexes.Add(spawnedAgent.Index);
						_victoryObjectiveEnemyAgentIndexes.Add(spawnedAgent.Index);
						_spawnedDefenderReserveAgentIndexes.Add(spawnedAgent.Index);
						if (defenderEntry?.SourceRoster != null)
						{
							_defenderReserveAgentSourceRosters[spawnedAgent.Index] = defenderEntry.SourceRoster;
						}
						if (defenderEntry != null && defenderReserveWaveNumber > 0)
						{
							_defenderReserveAgentWaveNumbers[spawnedAgent.Index] = defenderReserveWaveNumber;
						}
						AssignEnemyAgentCombatTarget(spawnedAgent, spawnedAgent.Index + i);
					}
					else
					{
						_alliedAgentIndexes.Add(spawnedAgent.Index);
						RegisterSetsSelectedFollowerAgent(spawnedAgent, "spawn_allied_agent");
						CacheProtectedFollowerHealth(spawnedAgent);
						AssignAgentToFormation(spawnedAgent, team, FormationClass.Infantry);
					}
				}
				catch (Exception ex)
				{
					SettlementEntryTroopSelectionLog.Log("Spawn agent failed. source=" + source + ", enemy=" + asEnemy + ", reserveKind=" + (defenderEntry?.SourceKind ?? "none") + ", troop=" + SafeCharacterId(troop) + ", error=" + ex.Message);
				}
			}
			return spawned;
		}

		private bool TryAdoptExistingSelectedHeroAgent(CharacterObject troop, Team team, string source, out Agent adoptedAgent)
		{
			adoptedAgent = null;
			try
			{
				Hero selectedHero = troop?.HeroObject;
				Mission mission = base.Mission;
				if (selectedHero == null || mission?.Agents == null || team == null)
				{
					return false;
				}
				foreach (Agent candidate in mission.Agents.ToList())
				{
					if (candidate == null || !candidate.IsHuman || !candidate.IsActive() || candidate == Agent.Main || candidate.IsMainAgent)
					{
						continue;
					}
					CharacterObject candidateCharacter = candidate.Character as CharacterObject;
					Hero candidateHero = candidateCharacter?.HeroObject;
					if (candidateHero != selectedHero && !CharactersMatch(candidateCharacter, troop))
					{
						continue;
					}
					adoptedAgent = candidate;
					break;
				}
				if (adoptedAgent == null)
				{
					return false;
				}
				if (adoptedAgent.Team != team)
				{
					adoptedAgent.SetTeam(team, true);
				}
				adoptedAgent.SetWatchState(Agent.WatchState.Patrolling);
				_alliedAgentIndexes.Add(adoptedAgent.Index);
				RegisterSetsSelectedFollowerAgent(adoptedAgent, "adopt_existing_selected_hero");
				CacheProtectedFollowerHealth(adoptedAgent);
				AssignAgentToFormation(adoptedAgent, team, FormationClass.Infantry);
				SettlementEntryTroopSelectionLog.Log("Adopted existing selected hero scene agent instead of spawning duplicate. settlement=" + _settlementId + ", source=" + (source ?? "") + ", agent=" + adoptedAgent.Index + ", hero=" + (selectedHero.StringId ?? SafeCharacterId(troop)));
				return true;
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("TryAdoptExistingSelectedHeroAgent failed. troop=" + SafeCharacterId(troop) + ", error=" + ex.Message);
				return false;
			}
		}

		private static void AssignAgentToFormation(Agent agent, Team team, FormationClass formationClass)
		{
			try
			{
				Formation formation = team?.GetFormation(formationClass);
				if (agent == null || formation == null || !agent.IsActive())
				{
					return;
				}
				agent.Formation = formation;
				if (team?.IsPlayerGeneral == true)
				{
					MarkFormationPlayerCommandable(formation, Agent.Main ?? agent.Mission?.MainAgent);
				}
				agent.TryAttachToFormation();
				agent.SetShouldCatchUpWithFormation(true);
				agent.UpdateFormationOrders();
			}
			catch
			{
			}
		}

		private static void MarkFormationPlayerCommandable(Formation formation, Agent playerOwner)
		{
			try
			{
				if (formation == null)
				{
					return;
				}
				try
				{
					formation.SetControlledByAI(false, false);
				}
				catch
				{
					TrySetFormationProperty(formation, nameof(Formation.IsAIControlled), false);
				}
				TrySetFormationProperty(formation, nameof(Formation.HasPlayerControlledTroop), true);
				if (playerOwner != null && playerOwner.IsActive())
				{
					TrySetFormationProperty(formation, nameof(Formation.PlayerOwner), playerOwner);
				}
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("MarkFormationPlayerCommandable failed. error=" + ex.Message);
			}
		}

		private static void TrySetFormationProperty(Formation formation, string propertyName, object value)
		{
			try
			{
				PropertyInfo property = formation?.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				MethodInfo setter = property?.GetSetMethod(true);
				setter?.Invoke(formation, new object[] { value });
			}
			catch
			{
			}
		}

		private void TrySetFollowerFormationFollowOrder(Mission mission, Agent main)
		{
			try
			{
				Formation formation = _playerTeam?.GetFormation(FormationClass.Infantry);
				if (mission == null || main == null || formation == null)
				{
					return;
				}
				MarkFormationPlayerCommandable(formation, main);
				formation.SetMovementOrder(MovementOrder.MovementOrderFollow(main));
				formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("Set follower formation follow order failed. error=" + ex.Message);
			}
		}

		private bool IsProtectedFollowerFriendlyFire(Agent affectedAgent, Agent affectorAgent)
		{
			return affectedAgent != null
				&& affectorAgent != null
				&& affectedAgent != affectorAgent
				&& IsProtectedFollowerAgent(affectedAgent)
				&& IsPlayerSideAgent(affectorAgent);
		}

		private void ProtectFollowerFromFriendlyFire(Agent affectedAgent, Agent affectorAgent, in Blow blow)
		{
			try
			{
				if (affectedAgent == null || !affectedAgent.IsActive())
				{
					return;
				}
				float damage = Math.Max(0f, blow.InflictedDamage);
				int affectorIndex = affectorAgent?.Index ?? -1;
				float missionTime = base.Mission?.CurrentTime ?? 0f;
				bool duplicateHit = IsDuplicateProtectedFollowerFriendlyFire(affectedAgent.Index, affectorIndex, missionTime, damage);
				if (!duplicateHit)
				{
					float cachedHealth = 0f;
					bool hasCachedHealth = _lastProtectedFollowerHealth.TryGetValue(affectedAgent.Index, out cachedHealth);
					float restoredHealth = affectedAgent.Health + damage;
					if (hasCachedHealth)
					{
						restoredHealth = Math.Max(restoredHealth, cachedHealth);
					}
					restoredHealth = ClampProtectedFollowerHealth(restoredHealth, affectedAgent.HealthLimit);
					if (affectedAgent.Health < restoredHealth)
					{
						affectedAgent.Health = restoredHealth;
					}
					RememberProtectedFollowerFriendlyFire(affectedAgent.Index, affectorIndex, missionTime, damage);
				}
				ExtendProtectedFollowerHostilitySuppression();
				ForceProtectedFollowerFriendlyState(affectedAgent);
				ClearProtectedFollowersHostilityFromPlayerSide("friendly_fire_hit");
				if (affectorAgent != null)
				{
					ClearAgentCombatTarget(affectorAgent);
				}
				CacheProtectedFollowerHealth(affectedAgent);
				SettlementEntryTroopSelectionLog.Log("Protected SETS follower from player-side friendly fire. troop=" + SafeCharacterId(affectedAgent.Character as CharacterObject) + ", affector=" + SafeCharacterId(affectorAgent.Character as CharacterObject) + ", health=" + affectedAgent.Health.ToString("0.0") + ", duplicateHit=" + duplicateHit);
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("ProtectFollowerFromFriendlyFire failed. error=" + ex.Message);
			}
		}

		private bool IsDuplicateProtectedFollowerFriendlyFire(int affectedIndex, int affectorIndex, float missionTime, float damage)
		{
			try
			{
				if (!_recentProtectedFollowerFriendlyFireHits.TryGetValue(affectedIndex, out ProtectedFollowerFriendlyFireHitRecord record))
				{
					return false;
				}
				return record.AffectorIndex == affectorIndex
					&& MathF.Abs(record.MissionTime - missionTime) <= ProtectedFollowerFriendlyFireDuplicateWindowSeconds
					&& MathF.Abs(record.Damage - damage) <= 0.5f;
			}
			catch
			{
				return false;
			}
		}

		private void RememberProtectedFollowerFriendlyFire(int affectedIndex, int affectorIndex, float missionTime, float damage)
		{
			_recentProtectedFollowerFriendlyFireHits[affectedIndex] = new ProtectedFollowerFriendlyFireHitRecord
			{
				AffectorIndex = affectorIndex,
				MissionTime = missionTime,
				Damage = damage
			};
		}

		private void MaintainProtectedFollowersFriendlyState()
		{
			try
			{
				Mission mission = base.Mission;
				if (mission == null || _alliedAgentIndexes.Count <= 0)
				{
					return;
				}
				bool suppressHostility = mission.CurrentTime <= _protectedFollowerHostilitySuppressionUntil;
				foreach (Agent agent in mission.Agents)
				{
					if (agent == null || !agent.IsHuman || !agent.IsActive() || !IsProtectedFollowerAgent(agent))
					{
						continue;
					}
					if (suppressHostility || (_playerTeam != null && agent.Team != _playerTeam) || _enemyAgentIndexes.Contains(agent.Index) || _spawnedDefenderReserveAgentIndexes.Contains(agent.Index))
					{
						ForceProtectedFollowerFriendlyState(agent);
					}
					CacheProtectedFollowerHealth(agent);
				}
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("MaintainProtectedFollowersFriendlyState failed. error=" + ex.Message);
			}
		}

		private void ForceProtectedFollowerFriendlyState(Agent agent)
		{
			try
			{
				if (agent == null || !agent.IsHuman || !agent.IsActive() || !_alliedAgentIndexes.Contains(agent.Index))
				{
					return;
				}
				if (_playerTeam != null && agent.Team != _playerTeam)
				{
					agent.SetTeam(_playerTeam, true);
				}
				_enemyAgentIndexes.Remove(agent.Index);
				_victoryObjectiveEnemyAgentIndexes.Remove(agent.Index);
				_spawnedDefenderReserveAgentIndexes.Remove(agent.Index);
				_defenderReserveAgentSourceRosters.Remove(agent.Index);
				_defenderReserveAgentWaveNumbers.Remove(agent.Index);
				ClearAgentCombatTarget(agent);
				agent.SetWatchState(_conflictActive ? Agent.WatchState.Alarmed : Agent.WatchState.Patrolling);
				AssignAgentToFormation(agent, _playerTeam, FormationClass.Infantry);
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("ForceProtectedFollowerFriendlyState failed. agent=" + agent?.Index + ", error=" + ex.Message);
			}
		}

		private void ClearProtectedFollowersHostilityFromPlayerSide(string reason)
		{
			try
			{
				Mission mission = base.Mission;
				if (mission == null)
				{
					return;
				}
				int cleared = 0;
				foreach (Agent agent in mission.Agents)
				{
					if (agent == null || !agent.IsHuman || !agent.IsActive() || !IsProtectedFollowerAgent(agent))
					{
						continue;
					}
					ForceProtectedFollowerFriendlyState(agent);
					cleared++;
				}
				SettlementEntryTroopSelectionLog.Log("Cleared SETS follower hostility toward player side. reason=" + reason + ", count=" + cleared);
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("ClearProtectedFollowersHostilityFromPlayerSide failed. reason=" + reason + ", error=" + ex.Message);
			}
		}

		private void ExtendProtectedFollowerHostilitySuppression()
		{
			try
			{
				float currentTime = base.Mission?.CurrentTime ?? 0f;
				_protectedFollowerHostilitySuppressionUntil = Math.Max(_protectedFollowerHostilitySuppressionUntil, currentTime + ProtectedFollowerHostilitySuppressionSeconds);
			}
			catch
			{
			}
		}

		private void CacheProtectedFollowerHealth(Agent agent)
		{
			try
			{
				if (agent != null && _alliedAgentIndexes.Contains(agent.Index) && agent.IsActive())
				{
					_lastProtectedFollowerHealth[agent.Index] = ClampProtectedFollowerHealth(agent.Health, agent.HealthLimit);
				}
			}
			catch
			{
			}
		}

		private bool IsProtectedFollowerAgent(Agent agent)
		{
			if (agent == null || !agent.IsHuman)
			{
				return false;
			}
			if (_alliedAgentIndexes.Contains(agent.Index))
			{
				return true;
			}
			return false;
		}

		private static void ClearAgentCombatTarget(Agent agent)
		{
			try
			{
				if (agent == null || !agent.IsActive())
				{
					return;
				}
				agent.ResetEnemyCaches();
				agent.InvalidateTargetAgent();
				AgentSetTargetAgentMethod?.Invoke(agent, new object[] { null });
				AgentSetAutomaticTargetSelectionMethod?.Invoke(agent, new object[] { true });
			}
			catch
			{
			}
		}

		private static float ClampProtectedFollowerHealth(float health, float healthLimit)
		{
			float upper = Math.Max(1f, healthLimit);
			if (float.IsNaN(health))
			{
				return 1f;
			}
			return Math.Min(Math.Max(health, 1f), upper);
		}

		private void SettleAlliedCasualty(Agent affectedAgent)
		{
			if (!_settledCasualtyAgentIndexes.Add(affectedAgent.Index))
			{
				return;
			}
			CharacterObject character = affectedAgent.Character as CharacterObject;
			if (character == null || character.IsHero)
			{
				SettlementEntryTroopSelectionLog.Log("Allied hero casualty observed; no roster removal. troop=" + SafeCharacterId(character));
				return;
			}
			TryRemoveFromRoster(MobileParty.MainParty?.MemberRoster, character, 1);
			TryRemoveFromRoster(_survivingRoster, character, 1);
			SettlementEntryTroopSelectionLog.Log("Allied casualty removed from main party. troop=" + SafeCharacterId(character));
		}

		private void SettleDefenderReserveDefeat(Agent affectedAgent, string source)
		{
			if (!_conflictFeaturesEnabled)
			{
				return;
			}
			if (!_settledDefenderReserveAgentIndexes.Add(affectedAgent.Index))
			{
				return;
			}
			CharacterObject character = affectedAgent.Character as CharacterObject;
			if (character == null || character.IsHero)
			{
				_defenderReserveAgentSourceRosters.Remove(affectedAgent.Index);
				return;
			}
			if (!_defenderReserveAgentSourceRosters.TryGetValue(affectedAgent.Index, out TroopRoster sourceRoster))
			{
				sourceRoster = Settlement.Find(_settlementId)?.Town?.GarrisonParty?.MemberRoster;
			}
			TryRemoveFromRoster(sourceRoster, character, 1);
			_defenderReserveAgentSourceRosters.Remove(affectedAgent.Index);
			SettlementEntryTroopSelectionLog.Log("Defender reserve defeat removed. troop=" + SafeCharacterId(character) + ", source=" + source);
		}

		public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
		{
			if (_defenderConflictEnabled && _conflictActive && !_victoryReached)
			{
				canPlayerLeave = false;
				return new InquiryData("【SETS内部暴乱】", "内部暴乱尚未结束。击溃现场守卫、民兵、驻军与驻城领主部队后才能退出。", isAffirmativeOptionShown: true, isNegativeOptionShown: false, "确定", "", null, null);
			}
			canPlayerLeave = true;
			return null;
		}

		internal bool ShouldBypassNativeEndMissionGuards()
		{
			return _conflictFeaturesEnabled && (_victoryReached || _ownedSettlementIncidentTriggered);
		}

		private void ReachVictory(string source)
		{
			if (_victoryReached)
			{
				return;
			}
			_victoryReached = true;
			_victoryReachedTime = base.Mission?.CurrentTime ?? 0f;
			_conflictActive = false;
			ClearSetsUsableProtectionState("sets_victory");
			PrepareVictoryExit(source);
			QueueVictoryPostMissionFlow(source);
			InformationManager.DisplayMessage(new InformationMessage("【SETS内部暴乱】城镇守卫、民兵、驻军与驻城领主部队已被击溃。按 TAB 退出后进入原版围城战胜利处置菜单，可选择原版处置或 GCCZ 攻城处置。", Color.FromUint(SuccessColor)));
			SettlementEntryTroopSelectionLog.Log("Victory reached. settlement=" + _settlementId + ", survivors=" + (_survivingRoster?.TotalManCount ?? 0) + ", source=" + source);
		}

		private void TryForceVictoryMissionEnd(string source)
		{
			if (_victoryEndMissionRequested)
			{
				return;
			}
			try
			{
				Mission mission = base.Mission;
				if (mission == null || mission.IsMissionEnding)
				{
					return;
				}
				float victoryReachedTime = _victoryReachedTime < 0f ? mission.CurrentTime : _victoryReachedTime;
				if (mission.CurrentTime - victoryReachedTime < VictoryEndMissionFallbackDelaySeconds)
				{
					return;
				}
				_victoryEndMissionRequested = true;
				PrepareVictoryExit(source + "_force_end");
				mission.NextCheckTimeEndMission = 0f;
				mission.EndMission();
				SettlementEntryTroopSelectionLog.Log("Forced SETS victory mission end. settlement=" + _settlementId + ", source=" + source);
			}
			catch (Exception ex)
			{
				_victoryEndMissionRequested = false;
				SettlementEntryTroopSelectionLog.Log("Forced SETS victory mission end failed. settlement=" + _settlementId + ", error=" + ex.Message);
			}
		}

		private void QueueVictoryPostMissionFlow(string source)
		{
			if (!_conflictFeaturesEnabled || _victoryQueued)
			{
				return;
			}
			_victoryQueued = true;
			string queueSource = string.IsNullOrWhiteSpace(source) ? "SETS_town_victory" : source;
			QueueSettlementTakenMenuAfterTownVictory(_settlementId, _survivingRoster, queueSource, skipOwnershipTransfer: _isOwnSettlement || _ownedSettlementIncidentTriggered, setsOwnedIncident: _ownedSettlementIncidentTriggered, setsTownRiotKilledNotable: _townRiotKilledNotable);
		}

		private void PrepareVictoryExit(string source)
		{
			try
			{
				Mission mission = base.Mission;
				if (mission == null)
				{
					return;
				}
				KeepPlayerEntryFollowersCommandable(refreshFormation: true);
				if (_playerTeam != null && _enemyTeam != null)
				{
					_playerTeam.SetIsEnemyOf(_enemyTeam, false);
					_enemyTeam.SetIsEnemyOf(_playerTeam, false);
				}
				if (_neutralTeam != null && _playerTeam != null)
				{
					_neutralTeam.SetIsEnemyOf(_playerTeam, false);
					_playerTeam.SetIsEnemyOf(_neutralTeam, false);
				}
				NeutralizeLiveTrackedEnemies(string.IsNullOrWhiteSpace(source) ? "SETS_victory_exit" : source + "_victory_exit");
				mission.NextCheckTimeEndMission = 0f;
				if (mission.Mode == MissionMode.Battle)
				{
					mission.SetMissionMode(MissionMode.StartUp, atStart: false);
				}
			}
			catch (Exception ex)
			{
				SettlementEntryTroopSelectionLog.Log("Prepare victory exit failed. error=" + ex.Message);
			}
		}

		private bool IsPlayerSideAgent(Agent agent)
		{
			return agent != null && (agent == Agent.Main || _alliedAgentIndexes.Contains(agent.Index));
		}

		private bool IsOwnedSettlementIncidentTarget(Agent agent)
		{
			return agent != null
				&& agent.IsHuman
				&& agent.IsActive()
				&& !agent.IsMainAgent
				&& !IsPlayerSideAgent(agent);
		}

		private bool IsOwnedSettlementCivilian(Agent agent)
		{
			CharacterObject character = agent?.Character as CharacterObject;
			return IsOwnedSettlementIncidentTarget(agent)
				&& !IsGuardOrSoldier(character)
				&& !IsLordCombatant(character);
		}

		private static bool IsOwnedSettlementIncidentNotable(Agent agent)
		{
			Hero hero = (agent?.Character as CharacterObject)?.HeroObject;
			return hero != null && hero.IsNotable;
		}

		private static bool IsVictoryObjectiveSceneAgent(Agent agent)
		{
			return agent != null && agent.IsHuman && agent.IsActive() && (IsGuardOrSoldier(agent.Character as CharacterObject) || IsLordCombatant(agent.Character as CharacterObject));
		}

		private static bool IsGuardOrSoldier(CharacterObject character)
		{
			return character != null && (character.Occupation == Occupation.Soldier
				|| character.Occupation == Occupation.Guard
				|| character.Occupation == Occupation.PrisonGuard
				|| character.Occupation == Occupation.BannerBearer
				|| character.Occupation == Occupation.CaravanGuard);
		}

		private static bool IsLordCombatant(CharacterObject character)
		{
			Hero hero = character?.HeroObject;
			return hero != null && hero != Hero.MainHero && (hero.IsLord || hero.Occupation == Occupation.Lord || character.Occupation == Occupation.Lord);
		}
	}

	private static List<CharacterObject> ExpandRoster(TroopRoster roster, int maxCount)
	{
		List<CharacterObject> troops = new List<CharacterObject>();
		if (roster == null || maxCount <= 0)
		{
			return troops;
		}
		for (int i = 0; i < roster.Count && troops.Count < maxCount; i++)
		{
			TroopRosterElement item = roster.GetElementCopyAtIndex(i);
			for (int j = 0; j < item.Number && troops.Count < maxCount; j++)
			{
				if (item.Character != null)
				{
					troops.Add(item.Character);
				}
			}
		}
		return troops;
	}

	private static List<DefenderReserveEntry> BuildCurrentDefenderReserve(string settlementId)
	{
		List<DefenderReserveEntry> entries = new List<DefenderReserveEntry>();
		Settlement settlement = string.IsNullOrWhiteSpace(settlementId) ? null : Settlement.Find(settlementId);
		if (settlement == null)
		{
			return entries;
		}
		HashSet<Hero> addedHeroes = new HashSet<Hero>();
		int garrisonStart = entries.Count;
		MobileParty garrisonParty = settlement.Town?.GarrisonParty;
		AppendDefenderReserveFromRoster(entries, garrisonParty?.MemberRoster, garrisonParty?.Party, "garrison", addedHeroes);
		int militiaStart = entries.Count;
		MobileParty militiaParty = settlement.MilitiaPartyComponent?.MobileParty;
		AppendDefenderReserveFromRoster(entries, militiaParty?.MemberRoster, militiaParty?.Party, "militia", addedHeroes);
		int lordPartyStart = entries.Count;
		for (int i = 0; i < settlement.Parties.Count; i++)
		{
			MobileParty lordParty = settlement.Parties[i];
			if (IsSameKingdomDefenderLordParty(settlement, lordParty))
			{
				AppendDefenderReserveFromRoster(entries, lordParty.MemberRoster, lordParty.Party, "lord_party", addedHeroes);
			}
		}
		int ownerHeroStart = entries.Count;
		AppendOwnerHeroIfPresent(entries, settlement, addedHeroes);
		int ownerHeroEnd = entries.Count;
		SettlementEntryTroopSelectionLog.Log("Built defender reserve. settlement=" + settlementId
			+ ", garrison=" + (militiaStart - garrisonStart)
			+ ", militia=" + (lordPartyStart - militiaStart)
			+ ", lordParties=" + (ownerHeroStart - lordPartyStart)
			+ ", ownerHero=" + (ownerHeroEnd - ownerHeroStart)
			+ ", total=" + entries.Count);
		return entries;
	}

	private static void AppendDefenderReserveFromRoster(List<DefenderReserveEntry> entries, TroopRoster roster, PartyBase sourceParty, string sourceKind, HashSet<Hero> addedHeroes)
	{
		if (entries == null || roster == null)
		{
			return;
		}
		for (int i = roster.Count - 1; i >= 0; i--)
		{
			TroopRosterElement item = roster.GetElementCopyAtIndex(i);
			CharacterObject character = item.Character;
			if (character == null || item.Number <= 0)
			{
				continue;
			}
			if (character.IsHero)
			{
				Hero hero = character.HeroObject;
				if (!ShouldUseDefenderReserveHero(hero) || (addedHeroes != null && !addedHeroes.Add(hero)))
				{
					continue;
				}
				entries.Add(new DefenderReserveEntry
				{
					Character = character,
					SourceRoster = roster,
					SourceParty = sourceParty,
					SourceKind = sourceKind
				});
				continue;
			}
			int healthyCount = Math.Max(0, item.Number - item.WoundedNumber);
			for (int j = 0; j < healthyCount; j++)
			{
				entries.Add(new DefenderReserveEntry
				{
					Character = character,
					SourceRoster = roster,
					SourceParty = sourceParty,
					SourceKind = sourceKind
				});
			}
		}
	}

	private static void AppendOwnerHeroIfPresent(List<DefenderReserveEntry> entries, Settlement settlement, HashSet<Hero> addedHeroes)
	{
		Hero ownerHero = settlement?.OwnerClan?.Leader;
		if (!ShouldUseDefenderReserveHero(ownerHero) || ownerHero.CurrentSettlement != settlement || (addedHeroes != null && !addedHeroes.Add(ownerHero)))
		{
			return;
		}
		entries.Add(new DefenderReserveEntry
		{
			Character = ownerHero.CharacterObject,
			SourceRoster = null,
			SourceParty = settlement.Party,
			SourceKind = "owner_hero"
		});
	}

	private static bool IsSameKingdomDefenderLordParty(Settlement settlement, MobileParty mobileParty)
	{
		if (settlement == null || mobileParty == null || mobileParty == MobileParty.MainParty || !mobileParty.IsActive || !mobileParty.IsLordParty || mobileParty.CurrentSettlement != settlement)
		{
			return false;
		}
		Clan ownerClan = settlement.OwnerClan;
		Kingdom ownerKingdom = ownerClan?.Kingdom;
		if (ownerKingdom == null)
		{
			return mobileParty.MapFaction == settlement.MapFaction;
		}
		if (mobileParty.ActualClan?.Kingdom == ownerKingdom || mobileParty.LeaderHero?.Clan?.Kingdom == ownerKingdom)
		{
			return true;
		}
		return mobileParty.MapFaction == settlement.MapFaction;
	}

	private static bool ShouldUseDefenderReserveHero(Hero hero)
	{
		return hero != null
			&& hero != Hero.MainHero
			&& hero.IsAlive
			&& !hero.IsPrisoner
			&& !hero.IsWounded
			&& hero.Age >= 18f
			&& hero.CharacterObject != null;
	}

	private static void TryRemoveFromRoster(TroopRoster roster, CharacterObject character, int count)
	{
		try
		{
			if (roster == null || character == null || count <= 0)
			{
				return;
			}
			roster.AddToCounts(character, -count, false, 0, 0, true, -1);
		}
		catch (Exception ex)
		{
			SettlementEntryTroopSelectionLog.Log("TryRemoveFromRoster failed. troop=" + SafeCharacterId(character) + ", count=" + count + ", error=" + ex.Message);
		}
	}

	private static string SafeCharacterId(CharacterObject character)
	{
		return character?.StringId ?? "null";
	}
}
