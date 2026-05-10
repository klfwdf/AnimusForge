using System;
using System.Collections.Generic;
using System.Reflection;
using Helpers;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;

namespace AnimusForge;

public static class TroopInspectionBehavior
{
	private const string LogPrefix = "TroopInspection";

	private const string DummyPartyPrefix = "animusforge_troop_inspection_dummy_";

	private static MobileParty _dummyParty;

	private static MapEvent _mapEvent;

	private static string _dummyPartyStringId;

	private static bool _isOpening;

	private static bool _cleanupDone;

	public static void RegisterHarmonyPatches(Harmony harmony)
	{
		if (harmony == null)
		{
			return;
		}
		TryPatchClass(harmony, typeof(TroopInspectionDeathRatePatch));
		TryPatchClass(harmony, typeof(TroopInspectionMeleeDamagePatch));
		TryPatchClass(harmony, typeof(TroopInspectionOrderOfBattlePatch));
	}

	private static void TryPatchClass(Harmony harmony, Type patchType)
	{
		try
		{
			harmony.CreateClassProcessor(patchType).Patch();
		}
		catch (Exception ex)
		{
			Logger.LogTrace("SubModule", ">>> " + patchType.Name + " init failed: " + ex.Message);
		}
	}

	public static void OpenInspectionFromTerminal()
	{
		Log("terminal_open");
		if (_isOpening)
		{
			Display("检阅正在准备中。");
			Log("precheck blocked: already opening");
			return;
		}
		_isOpening = true;
		_cleanupDone = false;
		try
		{
			if (!CanOpenFromCurrentState(out MobileParty mainParty, out string blockedReason))
			{
				Display(blockedReason);
				Log("precheck blocked: " + blockedReason);
				return;
			}
			int healthyInspectableTroops = CountHealthyNonPlayerTroops(PartyBase.MainParty.MemberRoster);
			Log($"precheck wounded={Hero.MainHero.IsWounded} healthy_non_player={healthyInspectableTroops} mission_current={Mission.Current != null} player_encounter={PlayerEncounter.Current != null} player_mapevent={MapEvent.PlayerMapEvent != null}");
			if (Hero.MainHero.IsWounded)
			{
				Display("你受伤了，无法检阅部队。");
				return;
			}
			if (healthyInspectableTroops <= 0)
			{
				Display("没有可检阅的健康士兵。");
				return;
			}
			if (PlayerEncounter.Current != null || MapEvent.PlayerMapEvent != null || mainParty.MapEvent != null)
			{
				Display("当前遭遇状态无法检阅部队。");
				Log("precheck blocked: existing encounter or player map event");
				return;
			}
			PrepareRuntime(mainParty);
			MissionInitializerRecord rec = BuildMissionInitializerRecord(mainParty);
			Log($"open_battle scene={rec.SceneName} terrain={rec.TerrainType}");
			IMission openedMission = CampaignMission.OpenBattleMission(rec);
			Mission mission = openedMission as Mission;
			if (mission == null)
			{
				throw new InvalidOperationException("CampaignMission.OpenBattleMission returned non-Mission.");
			}
			PlayerEncounter.StartAttackMission();
			MapEvent.PlayerMapEvent?.BeginWait();
			Log($"mission_behaviors deployment_handler={HasMissionBehavior(mission, "BattleDeploymentHandler")} deployment_controller={mission.GetMissionBehavior<BattleDeploymentMissionController>() != null} battle_end_logic={mission.GetMissionBehavior<BattleEndLogic>() != null} mode={mission.Mode}");
			TroopInspectionMissionLogic logic = new TroopInspectionMissionLogic(_dummyPartyStringId);
			mission.AddMissionBehavior(logic);
			logic.TryDisableBattleEndLogic("after_open_manual");
			Log("logic_added success");
		}
		catch (Exception ex)
		{
			Log("open failed: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace);
			CleanupRuntime("open_failed");
			Display("打开检阅士兵失败。");
		}
		finally
		{
			_isOpening = false;
		}
	}

	internal static bool IsCurrentInspectionRuntime(string dummyPartyStringId)
	{
		return !string.IsNullOrEmpty(dummyPartyStringId) && string.Equals(dummyPartyStringId, _dummyPartyStringId, StringComparison.Ordinal);
	}

	internal static void CleanupRuntime(string reason)
	{
		if (_cleanupDone)
		{
			Log("cleanup skipped: already done reason=" + reason);
			return;
		}
		_cleanupDone = true;
		Log("cleanup begin reason=" + reason);
		MapEvent mapEvent = _mapEvent;
		MobileParty dummyParty = _dummyParty;
		string dummyId = _dummyPartyStringId;
		_mapEvent = null;
		_dummyParty = null;
		_dummyPartyStringId = null;
		if (mapEvent != null)
		{
			try
			{
				Log($"cleanup_map_event state={mapEvent.State} battle_state={mapEvent.BattleState} finalized={mapEvent.IsFinalized} has_winner={mapEvent.HasWinner}");
				if (!mapEvent.IsFinalized)
				{
					mapEvent.ResetBattleState();
					mapEvent.FinalizeEvent();
					Log("cleanup map_event_finalized");
				}
				else
				{
					Log("cleanup map_event_already_finalized");
				}
			}
			catch (Exception ex)
			{
				Log("cleanup map_event failed: " + ex.GetType().Name + ": " + ex.Message);
			}
		}
		if (dummyParty != null)
		{
			try
			{
				if (IsOwnDummyParty(dummyParty, dummyId) && dummyParty.IsActive)
				{
					DestroyPartyAction.Apply(null, dummyParty);
					Log("cleanup dummy_party_destroyed id=" + dummyParty.StringId);
				}
				else
				{
					Log("cleanup dummy_party_destroy_skipped active=" + dummyParty.IsActive + " id=" + (dummyParty.StringId ?? "null"));
				}
			}
			catch (Exception ex)
			{
				Log("cleanup dummy_party failed: " + ex.GetType().Name + ": " + ex.Message);
			}
		}
		try
		{
			if (PlayerEncounter.Current != null)
			{
				MapEvent currentEncounterMapEvent = GetPrivateField<MapEvent>(PlayerEncounter.Current, "_mapEvent");
				if (currentEncounterMapEvent == mapEvent)
				{
					SetPrivateField<object>(PlayerEncounter.Current, "_campaignBattleResult", null);
					SetPrivateField<MapEvent>(PlayerEncounter.Current, "_mapEvent", null);
					ClearPlayerEncounterProperty();
					Log("cleanup player_encounter_context_cleared");
				}
				else
				{
					Log("cleanup player_encounter_skipped _mapEvent mismatch current=" + (currentEncounterMapEvent != null).ToString() + " ours=" + (mapEvent != null).ToString());
				}
			}
		}
		catch (Exception ex)
		{
			Log("cleanup player_encounter failed: " + ex.GetType().Name + ": " + ex.Message);
		}
		Log("cleanup end");
	}

	private static bool CanOpenFromCurrentState(out MobileParty mainParty, out string blockedReason)
	{
		mainParty = MobileParty.MainParty;
		blockedReason = "";
		try
		{
			if (Campaign.Current == null || mainParty == null || PartyBase.MainParty == null)
			{
				blockedReason = "当前状态无法检阅部队。";
				return false;
			}
			if (Mission.Current != null)
			{
				blockedReason = "当前任务中无法检阅部队。";
				return false;
			}
			if (Campaign.Current.ConversationManager != null && Campaign.Current.ConversationManager.IsConversationInProgress)
			{
				blockedReason = "当前对话中无法检阅部队。";
				return false;
			}
			if (Settlement.CurrentSettlement != null || mainParty.CurrentSettlement != null)
			{
				blockedReason = "当前地点无法检阅部队。";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			Log("precheck exception: " + ex.GetType().Name + ": " + ex.Message);
			blockedReason = "当前状态无法检阅部队。";
			return false;
		}
	}

	private static int CountHealthyNonPlayerTroops(TroopRoster roster)
	{
		int count = 0;
		if (roster == null)
		{
			return 0;
		}
		foreach (TroopRosterElement item in roster.GetTroopRoster())
		{
			CharacterObject character = item.Character;
			if (character == null || character.IsPlayerCharacter)
			{
				continue;
			}
			count += Math.Max(0, item.Number - item.WoundedNumber);
		}
		return count;
	}

	private static void PrepareRuntime(MobileParty mainParty)
	{
		CampaignVec2 mainPosition = mainParty.Position;
		Vec2 direction = ResolveEncounterDirection(mainParty);
		CampaignVec2 dummyPosition = mainPosition - direction * 0.4f;
		_dummyPartyStringId = DummyPartyPrefix + DateTime.UtcNow.Ticks + "_" + MBRandom.RandomInt(1000000);
		_dummyParty = MobileParty.CreateParty(_dummyPartyStringId, new TroopInspectionDummyPartyComponent(dummyPosition, new TextObject("AnimusForge Troop Inspection Dummy"), Hero.MainHero, Clan.PlayerClan));
		if (_dummyParty == null)
		{
			throw new InvalidOperationException("Failed to create dummy party.");
		}
		_dummyParty.IsVisible = false;
		_dummyParty.SetMoveModeHold();
		Log($"dummy_party_create id={_dummyParty.StringId} pos={FormatCampaignVec2(_dummyParty.Position)} members={_dummyParty.Party.NumberOfHealthyMembers}");
		FieldBattleEventComponent component = FieldBattleEventComponent.CreateFieldBattleEvent(PartyBase.MainParty, _dummyParty.Party);
		_mapEvent = component?.MapEvent;
		if (_mapEvent == null)
		{
			throw new InvalidOperationException("Failed to create field battle MapEvent.");
		}
		_mapEvent.ResetBattleState();
		int attackerCount = _mapEvent.AttackerSide.RecalculateMemberCountOfSide();
		int defenderCount = _mapEvent.DefenderSide.RecalculateMemberCountOfSide();
		Log($"mapevent_create attacker_side_count={attackerCount} defender_side_count={defenderCount} player_side={_mapEvent.PlayerSide} is_player_mapevent={_mapEvent.IsPlayerMapEvent}");
		PlayerEncounter.Start();
		PlayerEncounter.Current.SetupFields(PartyBase.MainParty, _dummyParty.Party);
		SetPrivateField<MapEvent>(PlayerEncounter.Current, "_mapEvent", _mapEvent);
		Log($"player_encounter_context battle={PlayerEncounter.Battle != null} is_mapevent={PlayerEncounter.Battle == _mapEvent} player_mapevent={MapEvent.PlayerMapEvent == _mapEvent}");
	}

	private static MissionInitializerRecord BuildMissionInitializerRecord(MobileParty mainParty)
	{
		IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
		MapPatchData patch = mapSceneWrapper.GetMapPatchAtPosition(mainParty.Position);
		string scene = Campaign.Current.Models.SceneModel.GetBattleSceneForMapPatch(patch, false);
		if (string.IsNullOrWhiteSpace(scene))
		{
			throw new InvalidOperationException("Battle scene is empty.");
		}
		MissionInitializerRecord rec = new MissionInitializerRecord(scene);
		TerrainType terrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(mainParty.CurrentNavigationFace);
		rec.TerrainType = (int)terrainType;
		rec.DamageToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		rec.DamageFromPlayerToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		rec.NeedsRandomTerrain = false;
		rec.PlayingInCampaignMode = true;
		rec.RandomTerrainSeed = MBRandom.RandomInt(10000);
		rec.AtmosphereOnCampaign = Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(mainParty.Position);
		rec.SceneHasMapPatch = true;
		rec.DecalAtlasGroup = 2;
		rec.PatchCoordinates = patch.normalizedCoordinates;
		rec.PatchEncounterDir = ResolvePatchEncounterDirection();
		return rec;
	}

	private static Vec2 ResolvePatchEncounterDirection()
	{
		try
		{
			if (_mapEvent?.AttackerSide?.LeaderParty != null && _mapEvent.DefenderSide?.LeaderParty != null)
			{
				Vec2 v = _mapEvent.AttackerSide.LeaderParty.Position.ToVec2() - _mapEvent.DefenderSide.LeaderParty.Position.ToVec2();
				if (v.LengthSquared > 0.0001f)
				{
					return v.Normalized();
				}
			}
		}
		catch
		{
		}
		return ResolveEncounterDirection(MobileParty.MainParty);
	}

	private static Vec2 ResolveEncounterDirection(MobileParty mainParty)
	{
		try
		{
			Vec2 bearing = mainParty?.Bearing ?? Vec2.Zero;
			if (bearing.LengthSquared > 0.0001f)
			{
				return bearing.Normalized();
			}
		}
		catch
		{
		}
		return new Vec2(1f, 0f);
	}

	private static bool IsOwnDummyParty(MobileParty party, string expectedStringId)
	{
		if (party == null || string.IsNullOrEmpty(expectedStringId))
		{
			return false;
		}
		return string.Equals(party.StringId, expectedStringId, StringComparison.Ordinal) && party.StringId.StartsWith(DummyPartyPrefix, StringComparison.Ordinal);
	}

	private static string FormatCampaignVec2(CampaignVec2 position)
	{
		return $"{position.X:0.00},{position.Y:0.00}";
	}

	private static void Display(string message)
	{
		InformationManager.DisplayMessage(new InformationMessage(message));
	}

	private static void SetPrivateField<T>(object target, string fieldName, T value)
	{
		try
		{
			target?.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);
		}
		catch
		{
		}
	}

	private static T GetPrivateField<T>(object target, string fieldName)
	{
		try
		{
			var field = target?.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
			if (field != null)
			{
				return (T)field.GetValue(target);
			}
		}
		catch
		{
		}
		return default;
	}

	private static void ClearPlayerEncounterProperty()
	{
		try
		{
			if (Campaign.Current != null)
			{
				typeof(Campaign).GetProperty("PlayerEncounter", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(Campaign.Current, null);
			}
		}
		catch
		{
		}
	}

	internal static bool HasMissionBehavior(Mission mission, string typeName)
	{
		if (mission == null)
		{
			return false;
		}
		try
		{
			Type handlerType = Type.GetType("TaleWorlds.MountAndBlade." + typeName + ", TaleWorlds.MountAndBlade");
			if (handlerType == null)
			{
				return false;
			}
			var getMethod = typeof(Mission).GetMethod("GetMissionBehavior", Type.EmptyTypes);
			if (getMethod == null)
			{
				return false;
			}
			var generic = getMethod.MakeGenericMethod(handlerType);
			return generic.Invoke(mission, null) != null;
		}
		catch
		{
			return false;
		}
	}

	private static void Log(string message)
	{
		Logger.Log(LogPrefix, "[TroopInspection] " + message);
	}

	private sealed class TroopInspectionDummyPartyComponent : PartyComponent
	{
		private readonly CampaignVec2 _position;

		private readonly TextObject _name;

		private readonly Hero _owner;

		private readonly Clan _clan;

		public TroopInspectionDummyPartyComponent(CampaignVec2 position, TextObject name, Hero owner, Clan clan)
		{
			_position = position;
			_name = name;
			_owner = owner;
			_clan = clan;
		}

		public override Hero PartyOwner => _owner;

		public override TextObject Name => _name;

		public override Settlement HomeSettlement => null;

		public override bool AvoidHostileActions => true;

		public override Banner GetDefaultComponentBanner()
		{
			return _clan?.Banner;
		}

		protected override void OnInitialize()
		{
			MobileParty.ActualClan = _clan;
			MobileParty.InitializeMobilePartyAroundPosition(TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), _position, 0f, 0f, !_position.IsOnLand);
			MobileParty.SetMoveModeHold();
		}
	}
}

internal sealed class TroopInspectionMissionLogic : MissionLogic
{
	private readonly string _dummyPartyStringId;

	private BattleEndLogic _battleEndLogic;

	private bool _battleEndDisabled;

	private bool _deploymentWasActive;

	private bool _deploymentEndDetected;

	private bool _inspectionMessageShown;

	private bool _cleanupRequested;

	private bool _agentCountsLogged;

	private bool _enemyAgentWarningLogged;

	private float _continuousRefreshTimer;

	private const float RefreshInterval = 0.12f;

	private const float RefreshRadius = 30f;

	private const bool RefreshAllPlayerAgents = true;

	private bool _conversationStateLogged;

	private bool _firstRefreshLogged;

	private float _nextBattleEndDisableRetryTime = 1f;

	private MissionMode _lastMissionMode;

	private bool _prisonersSpawned;

	private float _prisonerSpawnTimer = 1f;

	private readonly Dictionary<Agent, bool> _prisonerIsLordMap = new Dictionary<Agent, bool>();

	private readonly HashSet<Agent> _civilianPrisonerActionSetApplied = new HashSet<Agent>();

	private readonly Dictionary<Agent, float> _prisonerPoseSuppressedUntil = new Dictionary<Agent, float>();

	private readonly HashSet<Agent> _prisonerPoseApplied = new HashSet<Agent>();

	private ActionIndexCache _lordPrisonerAction;

	private ActionIndexCache _soldierPrisonerAction;

	private bool _prisonerActionsCached;

	private bool _lordPrisonerActionMissingLogged;

	private bool _soldierPrisonerActionMissingLogged;

	private bool _prisonerActionSetRejectedLogged;

	private float _prisonerPoseRefreshTimer;

	private const float PrisonerPoseRefreshInterval = 0.35f;

	private const float PrisonerPoseStartProgress = 0.35f;

	private const float PrisonerPoseActionSpeed = 0f;

	private string _lastMissionEndedLogState = "";

	private float _nextMissionEndedLogTime;

	public TroopInspectionMissionLogic(string dummyPartyStringId)
	{
		_dummyPartyStringId = dummyPartyStringId;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		CacheBattleEndLogic();
		TryDisableBattleEndLogic("OnBehaviorInitialize");
		_lastMissionMode = base.Mission?.Mode ?? MissionMode.StartUp;
		_deploymentWasActive = _lastMissionMode == MissionMode.Deployment;
		Log($"init deployment_active={_deploymentWasActive} mode={_lastMissionMode} battle_end_logic_cached={_battleEndLogic != null}");
		Log($"mission_behaviors deployment_handler={TroopInspectionBehavior.HasMissionBehavior(base.Mission, "BattleDeploymentHandler")} deployment_controller={base.Mission?.GetMissionBehavior<BattleDeploymentMissionController>() != null} battle_end_logic={_battleEndLogic != null}");
	}

	public override void AfterStart()
	{
		base.AfterStart();
		if (_battleEndLogic == null)
		{
			CacheBattleEndLogic();
		}
		TryDisableBattleEndLogic("AfterStart");
		_lastMissionMode = base.Mission?.Mode ?? MissionMode.StartUp;
		_deploymentWasActive = _lastMissionMode == MissionMode.Deployment;
		Log($"after_start deployment_active={_deploymentWasActive} mode={_lastMissionMode} battle_end_disabled={_battleEndDisabled}");
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		RetryBattleEndDisableIfNeeded();
		if (_deploymentWasActive && !_prisonersSpawned)
		{
			_prisonerSpawnTimer -= dt;
			if (_prisonerSpawnTimer <= 0f)
			{
				SpawnPrisoners();
			}
		}
		DetectDeploymentEnd();
		TryLogAgentCounts();
		RefreshPrisonerPoses(dt);
		ContinuousAgentRefresh(dt);
		if (!_inspectionMessageShown && _deploymentEndDetected && base.Mission != null && base.Mission.CurrentTime > 2f)
		{
			_inspectionMessageShown = true;
			InformationManager.DisplayMessage(new InformationMessage("检阅模式：可自由指挥部队进行检阅。按TAB撤退结束检阅。", Colors.Green));
			Log("inspection_message_shown");
		}
	}

	public override void OnRemoveBehavior()
	{
		RequestCleanup("OnRemoveBehavior");
		base.OnRemoveBehavior();
	}

	protected override void OnEndMission()
	{
		RequestCleanup("OnEndMission");
		base.OnEndMission();
	}

	public override bool MissionEnded(ref MissionResult missionResult)
	{
		string missionResultText = missionResult?.ToString() ?? "null";
		string state = missionResultText + "|" + _battleEndDisabled + "|" + _deploymentEndDetected;
		float currentTime = base.Mission?.CurrentTime ?? 0f;
		if (!string.Equals(_lastMissionEndedLogState, state, StringComparison.Ordinal) || currentTime >= _nextMissionEndedLogTime)
		{
			_lastMissionEndedLogState = state;
			_nextMissionEndedLogTime = currentTime + 5f;
			Log($"mission_ended_check mission_result={missionResultText} battle_end_disabled={_battleEndDisabled} deployment_detected={_deploymentEndDetected}");
		}
		return false;
	}

	internal void TryDisableBattleEndLogic(string source)
	{
		try
		{
			if (base.Mission == null)
			{
				Log("battle_end_disable skipped source=" + source + " mission=null");
				return;
			}
			if (_battleEndLogic == null)
			{
				CacheBattleEndLogic();
			}
			if (_battleEndLogic == null)
			{
				Log("battle_end_disable failed source=" + source + " BattleEndLogic=null");
				return;
			}
			_battleEndLogic.ChangeCanCheckForEndCondition(false);
			_battleEndDisabled = true;
			Log("battle_end_disable success source=" + source);
		}
		catch (Exception ex)
		{
			Log("battle_end_disable exception source=" + source + " " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void CacheBattleEndLogic()
	{
		try
		{
			_battleEndLogic = base.Mission?.GetMissionBehavior<BattleEndLogic>();
		}
		catch (Exception ex)
		{
			Log("cache_battle_end_logic failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void RetryBattleEndDisableIfNeeded()
	{
		if (_battleEndDisabled || base.Mission == null || base.Mission.CurrentTime < _nextBattleEndDisableRetryTime)
		{
			return;
		}
		_nextBattleEndDisableRetryTime = base.Mission.CurrentTime + 1f;
		TryDisableBattleEndLogic("retry_tick");
	}

	private void DetectDeploymentEnd()
	{
		if (_deploymentEndDetected || base.Mission == null)
		{
			return;
		}
		try
		{
			MissionMode currentMode = base.Mission.Mode;
			if (_lastMissionMode != currentMode)
			{
				Log($"mission_mode_changed {_lastMissionMode} -> {currentMode}");
			}
			if (_deploymentWasActive && currentMode != MissionMode.Deployment)
			{
				_deploymentEndDetected = true;
				FreezePrisoners();
				Log("deployment_ended detection");
				TryDisableBattleEndLogic("deployment_ended");
			}
			_deploymentWasActive = currentMode == MissionMode.Deployment;
			_lastMissionMode = currentMode;
		}
		catch (Exception ex)
		{
			Log("detect_deployment_end failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void FreezePrisoners()
	{
		try
		{
			if (base.Mission == null)
			{
				return;
			}
			foreach (Agent agent in base.Mission.Agents)
			{
				if (agent == null || !agent.IsActive() || !(agent.Origin is PrisonerAgentOrigin))
				{
					continue;
				}
				agent.Formation = null;
				bool isLord;
				if (_prisonerIsLordMap.TryGetValue(agent, out isLord))
				{
					ApplyPrisonerPose(agent, isLord, afterDeployment: true);
				}
			}
			Log("freeze_prisoners done");
		}
		catch (Exception ex)
		{
			Log("freeze_prisoners failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void SpawnPrisoners()
	{
		if (_prisonersSpawned || base.Mission == null)
		{
			return;
		}
		_prisonersSpawned = true;
		TroopRoster prisonRoster = PartyBase.MainParty?.PrisonRoster;
		if (prisonRoster == null)
		{
			Logger.LogEvent("TroopInspection", "spawn_prisoners skipped: PrisonRoster null");
			InformationManager.DisplayMessage(new InformationMessage("阅兵：无法访问囚犯名册。", Colors.Red));
			return;
		}
		int totalCount = 0;
		int heroCount = 0;
		int regularCount = 0;
		foreach (TroopRosterElement item in prisonRoster.GetTroopRoster())
		{
			if (item.Character == null)
			{
				continue;
			}
			totalCount += item.Number;
			if (item.Character.IsHero)
			{
				heroCount += item.Number;
			}
			else
			{
				regularCount += item.Number;
			}
		}
		if (totalCount <= 0)
		{
			Logger.LogEvent("TroopInspection", "spawn_prisoners skipped: no prisoners at all");
			InformationManager.DisplayMessage(new InformationMessage("阅兵：没有囚犯可参加阅兵。", Colors.Yellow));
			return;
		}
		Team playerTeam = base.Mission.PlayerTeam;
		if (playerTeam == null)
		{
			Logger.LogEvent("TroopInspection", "spawn_prisoners skipped: PlayerTeam null");
			return;
		}
		if (heroCount > 0)
		{
			playerTeam.GetFormation(FormationClass.HeavyCavalry);
		}
		if (regularCount > 0)
		{
			playerTeam.GetFormation(FormationClass.LightCavalry);
		}
		int spawnedHeroes = 0;
		int spawnedRegulars = 0;
		int totalErrors = 0;
		string lastError = "";
		int heroIdx = 0;
		foreach (TroopRosterElement item in prisonRoster.GetTroopRoster())
		{
			CharacterObject character = item.Character;
			if (character == null || !character.IsHero)
			{
				continue;
			}
			for (int i = 0; i < item.Number; i++)
			{
				try
				{
					PrisonerAgentOrigin origin = new PrisonerAgentOrigin(character);
					Agent agent = base.Mission.SpawnTroop(origin, isPlayerSide: true, hasFormation: true, spawnWithHorse: false, isReinforcement: false, formationTroopCount: heroCount, formationTroopIndex: heroIdx, isAlarmed: false, wieldInitialWeapons: false, forceDismounted: true, null, null, null, null, FormationClass.HeavyCavalry, false);
					if (agent != null)
					{
						agent.SetIsAIPaused(isPaused: true);
						agent.DisableScriptedMovement();
						_prisonerIsLordMap[agent] = true;
						ApplyPrisonerPose(agent, isLord: true, afterDeployment: false);
						Logger.LogEvent("TroopInspection", $"spawn_prisoner_hero ok troop={character.StringId} team={agent.Team?.Side.ToString() ?? "null"} formation={agent.Formation?.FormationIndex.ToString() ?? "null"} pos={agent.Position}");
						spawnedHeroes++;
					}
					else
					{
						Logger.LogEvent("TroopInspection", $"spawn_prisoner_hero returned null troop={character.StringId} formation={FormationClass.HeavyCavalry}");
					}
					heroIdx++;
				}
				catch (Exception ex)
				{
					totalErrors++;
					lastError = ex.GetType().Name + ": " + ex.Message;
					Logger.LogEvent("TroopInspection", "spawn_prisoner_hero failed: " + lastError);
				}
			}
		}
		int regIdx = 0;
		foreach (TroopRosterElement item in prisonRoster.GetTroopRoster())
		{
			CharacterObject character = item.Character;
			if (character == null || character.IsHero)
			{
				continue;
			}
			for (int i = 0; i < item.Number; i++)
			{
				try
				{
					PrisonerAgentOrigin origin = new PrisonerAgentOrigin(character);
					Agent agent = base.Mission.SpawnTroop(origin, isPlayerSide: true, hasFormation: true, spawnWithHorse: false, isReinforcement: false, formationTroopCount: regularCount, formationTroopIndex: regIdx, isAlarmed: false, wieldInitialWeapons: false, forceDismounted: true, null, null, null, null, FormationClass.LightCavalry, false);
					if (agent != null)
					{
						agent.SetIsAIPaused(isPaused: true);
						agent.DisableScriptedMovement();
						_prisonerIsLordMap[agent] = false;
						ApplyPrisonerPose(agent, isLord: false, afterDeployment: false);
						Logger.LogEvent("TroopInspection", $"spawn_prisoner_regular ok troop={character.StringId} team={agent.Team?.Side.ToString() ?? "null"} formation={agent.Formation?.FormationIndex.ToString() ?? "null"} pos={agent.Position}");
						spawnedRegulars++;
					}
					else
					{
						Logger.LogEvent("TroopInspection", $"spawn_prisoner_regular returned null troop={character.StringId} formation={FormationClass.LightCavalry}");
					}
					regIdx++;
				}
				catch (Exception ex)
				{
					totalErrors++;
					lastError = ex.GetType().Name + ": " + ex.Message;
					Logger.LogEvent("TroopInspection", "spawn_prisoner_regular failed: " + lastError);
				}
			}
		}
		int spawned = spawnedHeroes + spawnedRegulars;
		Logger.LogEvent("TroopInspection", "spawn_prisoners result: total=" + totalCount + " heroes=" + heroCount + " spawned_heroes=" + spawnedHeroes + " regulars=" + regularCount + " spawned_regulars=" + spawnedRegulars + " errors=" + totalErrors);
		if (spawned > 0)
		{
			string msg = "阅兵：";
			if (spawnedHeroes > 0)
			{
				msg += spawnedHeroes + " 名领主俘虏（8号领主编队），";
			}
			if (spawnedRegulars > 0)
			{
				msg += spawnedRegulars + " 名士兵俘虏（7号俘虏编队）";
			}
			InformationManager.DisplayMessage(new InformationMessage(msg, Colors.Green));
		}
		else if (totalErrors > 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("阅兵：囚犯生成失败(" + totalErrors + "/" + totalCount + ") 错误: " + lastError, Colors.Red));
		}
		else
		{
			InformationManager.DisplayMessage(new InformationMessage("阅兵：囚犯生成失败(" + totalCount + "名尝试，0名成功)。", Colors.Red));
		}
	}

	private void TryLogAgentCounts()
	{
		if (base.Mission == null || (!_deploymentEndDetected && base.Mission.CurrentTime < 3f))
		{
			return;
		}
		try
		{
			BattleSideEnum playerSide = base.Mission.PlayerTeam?.Side ?? PartyBase.MainParty.Side;
			BattleSideEnum enemySide = playerSide.GetOppositeSide();
			int playerAgents = 0;
			int enemyAgents = 0;
			int neutralAgents = 0;
			foreach (Agent agent in base.Mission.Agents)
			{
				if (agent == null || !agent.IsHuman || !agent.IsActive())
				{
					continue;
				}
				Team team = agent.Team;
				if (team == null)
				{
					neutralAgents++;
				}
				else if (team.Side == playerSide)
				{
					playerAgents++;
				}
				else if (team.Side == enemySide)
				{
					enemyAgents++;
				}
				else
				{
					neutralAgents++;
				}
			}
			if (!_agentCountsLogged)
			{
				_agentCountsLogged = true;
				Log($"agent_counts player_side={playerSide} enemy_side={enemySide} player_agents={playerAgents} enemy_agents={enemyAgents} neutral_agents={neutralAgents}");
			}
			if (!_enemyAgentWarningLogged && enemyAgents > 0)
			{
				_enemyAgentWarningLogged = true;
				Log($"enemy_agents_detected count={enemyAgents}");
			}
		}
		catch (Exception ex)
		{
			Log("agent_count_log failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void ContinuousAgentRefresh(float dt)
	{
		_continuousRefreshTimer += dt;
		if (_continuousRefreshTimer < RefreshInterval)
		{
			return;
		}
		_continuousRefreshTimer = 0f;
		try
		{
			bool isInConversation = Campaign.Current?.ConversationManager != null && Campaign.Current.ConversationManager.IsConversationInProgress && Campaign.Current.ConversationManager.OneToOneConversationAgent != null;
			if (isInConversation && !_conversationStateLogged)
			{
				_conversationStateLogged = true;
				Log("conversation_state_changed in_conversation=true");
			}
			if (!isInConversation)
			{
				_conversationStateLogged = false;
			}
			Agent mainAgent = base.Mission?.MainAgent;
			Team playerTeam = base.Mission?.PlayerTeam;
			if (mainAgent == null || playerTeam == null)
			{
				return;
			}
			Vec3 mainPos = mainAgent.Position;
			int refreshed = 0;
			foreach (Agent agent in base.Mission.Agents)
			{
				if (agent == null || !agent.IsHuman || !agent.IsActive())
				{
					continue;
				}
				if (agent == mainAgent)
				{
					continue;
				}
				if (agent.Team != playerTeam)
				{
					continue;
				}
				if (agent.Origin is PrisonerAgentOrigin)
				{
					continue;
				}
				if (!RefreshAllPlayerAgents && agent.Position.Distance(mainPos) > RefreshRadius)
				{
					continue;
				}
				RefreshSingleAgent(agent);
				refreshed++;
			}
			if (!_firstRefreshLogged)
			{
				_firstRefreshLogged = true;
				Log($"continuous_refresh_started agents_refreshed={refreshed} interval={RefreshInterval} radius={RefreshRadius} refresh_all={RefreshAllPlayerAgents}");
			}
		}
		catch (Exception ex)
		{
			Log("continuous_refresh error: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void RefreshSingleAgent(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		try
		{
			agent.DisableScriptedMovement();
		}
		catch
		{
		}
		try
		{
			agent.ClearTargetFrame();
		}
		catch
		{
		}
		try
		{
			agent.SetIsAIPaused(isPaused: false);
		}
		catch
		{
		}
		TrySetAgentController(agent, "AI");
		try
		{
			Agent mountAgent = agent.MountAgent;
			if (mountAgent != null && mountAgent.IsActive())
			{
				mountAgent.DisableScriptedMovement();
				mountAgent.ClearTargetFrame();
				mountAgent.SetIsAIPaused(isPaused: false);
				TrySetAgentController(mountAgent, "AI");
			}
		}
		catch
		{
		}
	}

	private void RefreshPrisonerPoses(float dt)
	{
		_prisonerPoseRefreshTimer -= dt;
		if (_prisonerPoseRefreshTimer > 0f)
		{
			return;
		}
		_prisonerPoseRefreshTimer = PrisonerPoseRefreshInterval;
		try
		{
			if (base.Mission == null)
			{
				return;
			}
			foreach (Agent agent in base.Mission.Agents)
			{
				if (agent == null || !agent.IsActive() || !(agent.Origin is PrisonerAgentOrigin))
				{
					continue;
				}
				bool isLord;
				if (TryGetPrisonerIsLord(agent, out isLord))
				{
					ApplyPrisonerPose(agent, isLord, _deploymentEndDetected);
				}
			}
		}
		catch (Exception ex)
		{
			Log("refresh_prisoner_poses failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private bool TryGetPrisonerIsLord(Agent agent, out bool isLord)
	{
		isLord = false;
		if (agent == null)
		{
			return false;
		}
		if (_prisonerIsLordMap.TryGetValue(agent, out isLord))
		{
			return true;
		}
		PrisonerAgentOrigin origin = agent.Origin as PrisonerAgentOrigin;
		CharacterObject character = origin?.Troop as CharacterObject;
		if (character == null)
		{
			return false;
		}
		isLord = character.IsHero;
		_prisonerIsLordMap[agent] = isLord;
		return true;
	}

	private void CachePrisonerActions()
	{
		if (_prisonerActionsCached)
		{
			return;
		}
		_prisonerActionsCached = true;
		_lordPrisonerAction = ActionIndexCache.act_scared_idle_1;
		_soldierPrisonerAction = ActionIndexCache.act_scared_idle_1;
		Log("prisoner_actions_cached lord=act_scared_idle_1 soldier=act_scared_idle_1 static_speed=0 progress=0.35");
	}

	private void ApplyPrisonerPose(Agent agent, bool isLord, bool afterDeployment)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		CachePrisonerActions();
		try
		{
			agent.SetIsAIPaused(isPaused: true);
		}
		catch
		{
		}
		try
		{
			agent.DisableScriptedMovement();
		}
		catch
		{
		}
		if (afterDeployment)
		{
			try
			{
				agent.SetMaximumSpeedLimit(0f, false);
			}
			catch
			{
			}
		}
		try
		{
			AgentFlag agentFlags = agent.GetAgentFlags();
			agent.SetAgentFlags(agentFlags & ~AgentFlag.CanGetAlarmed);
		}
		catch
		{
		}
		StripPrisonerWeapons(agent);
		try
		{
			agent.SetCrouchMode(false);
		}
		catch
		{
		}
		if (!afterDeployment)
		{
			return;
		}
		if (IsPrisonerPoseTemporarilySuppressed(agent))
		{
			return;
		}
		TrySetCivilianPrisonerActionSet(agent);
		TrySetPrisonerAction(agent, isLord);
	}

	private static void StripPrisonerWeapons(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		try
		{
			agent.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.Instant);
			agent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
		}
		catch
		{
		}
		for (int i = (int)EquipmentIndex.WeaponItemBeginSlot; i < (int)EquipmentIndex.NumAllWeaponSlots; i++)
		{
			try
			{
				agent.RemoveEquippedWeapon((EquipmentIndex)i);
			}
			catch
			{
			}
		}
		try
		{
			agent.InvalidateAIWeaponSelections();
			agent.UpdateWeapons();
		}
		catch
		{
		}
	}

	private void TrySetPrisonerAction(Agent agent, bool isLord)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		ActionIndexCache action = isLord ? _lordPrisonerAction : _soldierPrisonerAction;
		string actionName = "act_scared_idle_1";
		int channelNo = 0;
		try
		{
			if (!MBActionSet.CheckActionAnimationClipExists(agent.ActionSet, action))
			{
				if (isLord && !_lordPrisonerActionMissingLogged)
				{
					_lordPrisonerActionMissingLogged = true;
					Log("prisoner_action_missing action=" + actionName);
				}
				if (!isLord && !_soldierPrisonerActionMissingLogged)
				{
					_soldierPrisonerActionMissingLogged = true;
					Log("prisoner_action_missing action=" + actionName);
				}
				return;
			}
			ActionIndexCache currentAction = agent.GetCurrentAction(channelNo);
			if (currentAction == action && _prisonerPoseApplied.Contains(agent))
			{
				return;
			}
			AnimFlags poseFlags = AnimFlags.anf_disable_alternative_randomization | AnimFlags.anf_disable_auto_increment_progress | AnimFlags.anf_enforce_all;
			bool actionSet = agent.SetActionChannel(channelNo, action, true, poseFlags, 0f, PrisonerPoseActionSpeed, -0.2f, 0.4f, PrisonerPoseStartProgress, false, -0.2f, 0, true);
			if (actionSet)
			{
				try
				{
					agent.SetCurrentActionProgress(channelNo, PrisonerPoseStartProgress);
				}
				catch
				{
				}
				_prisonerPoseApplied.Add(agent);
			}
			else if (!_prisonerActionSetRejectedLogged)
			{
				_prisonerActionSetRejectedLogged = true;
				Log("set_prisoner_action rejected action=" + actionName);
			}
		}
		catch (Exception ex)
		{
			Log("set_prisoner_action failed action=" + actionName + " " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void TrySetCivilianPrisonerActionSet(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsActive() || _civilianPrisonerActionSetApplied.Contains(agent))
			{
				return;
			}
			Monster monster = agent.Monster;
			if (monster == null)
			{
				return;
			}
			string actionSetCode = agent.IsFemale ? "as_human_female_villager" : "as_human_villager";
			AnimationSystemData animationSystemData = monster.FillAnimationSystemData(MBActionSet.GetActionSet(actionSetCode), 1f, false);
			agent.SetActionSet(ref animationSystemData);
			_civilianPrisonerActionSetApplied.Add(agent);
			Log("set_civilian_prisoner_action_set action_set=" + actionSetCode);
		}
		catch (Exception ex)
		{
			Log("set_civilian_prisoner_action_set failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private bool IsPrisonerPoseTemporarilySuppressed(Agent agent)
	{
		try
		{
			if (agent == null || base.Mission == null)
			{
				return false;
			}
			float suppressUntil;
			if (!_prisonerPoseSuppressedUntil.TryGetValue(agent, out suppressUntil))
			{
				return false;
			}
			return base.Mission.CurrentTime < suppressUntil;
		}
		catch
		{
			return false;
		}
	}

	public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon attackerWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
	{
		base.OnAgentHit(affectedAgent, affectorAgent, in attackerWeapon, in blow, in attackCollisionData);
		try
		{
			if (affectedAgent == null || base.Mission == null || !(affectedAgent.Origin is PrisonerAgentOrigin))
			{
				return;
			}
			_prisonerPoseSuppressedUntil[affectedAgent] = base.Mission.CurrentTime + 0.9f;
			_prisonerPoseApplied.Remove(affectedAgent);
		}
		catch
		{
		}
	}

	private static void TrySetAgentController(Agent agent, string controllerType)
	{
		try
		{
			if (agent == null || string.IsNullOrWhiteSpace(controllerType))
			{
				return;
			}
			PropertyInfo propertyInfo = agent.GetType().GetProperty("Controller") ?? agent.GetType().GetProperty("ControllerType");
			if (propertyInfo == null || !propertyInfo.CanWrite)
			{
				return;
			}
			object value = Enum.Parse(propertyInfo.PropertyType, controllerType, ignoreCase: true);
			if (value != null)
			{
				propertyInfo.SetValue(agent, value);
			}
		}
		catch
		{
		}
	}

	private void RequestCleanup(string reason)
	{
		if (_cleanupRequested)
		{
			return;
		}
		_cleanupRequested = true;
		try
		{
			Log($"request_cleanup reason={reason} battle_end_disabled={_battleEndDisabled} mission_result={base.Mission?.MissionResult?.ToString() ?? "null"}");
		}
		catch
		{
		}
		if (TroopInspectionBehavior.IsCurrentInspectionRuntime(_dummyPartyStringId))
		{
			TroopInspectionBehavior.CleanupRuntime(reason);
		}
		_prisonerIsLordMap.Clear();
		_civilianPrisonerActionSetApplied.Clear();
		_prisonerPoseSuppressedUntil.Clear();
		_prisonerPoseApplied.Clear();
	}

	private static void Log(string message)
	{
		Logger.Log("TroopInspection", "[TroopInspection] " + message);
	}
}

public class PrisonerAgentOrigin : IAgentOriginBase
{
	private static readonly uint PrisonerFactionColor = new Color(1f, 0f, 0f).ToUnsignedInteger();

	private static readonly uint PrisonerFactionColor2 = new Color(0.6f, 0f, 0f).ToUnsignedInteger();

	private readonly CharacterObject _troop;

	private Banner _banner;

	private bool _isRemoved;

	private bool _hasThrownWeapon;

	private bool _hasHeavyArmor;

	private bool _hasShield;

	private bool _hasSpear;

	public BasicCharacterObject Troop => _troop;

	bool IAgentOriginBase.HasThrownWeapon => _hasThrownWeapon;

	bool IAgentOriginBase.HasHeavyArmor => _hasHeavyArmor;

	bool IAgentOriginBase.HasShield => _hasShield;

	bool IAgentOriginBase.HasSpear => _hasSpear;

	public bool IsUnderPlayersCommand => true;

	public uint FactionColor => PrisonerFactionColor;

	public uint FactionColor2 => PrisonerFactionColor2;

	public IBattleCombatant BattleCombatant => PartyBase.MainParty;

	public int UniqueSeed => MBRandom.RandomInt(1000000);

	public int Seed => CharacterHelper.GetDefaultFaceSeed(_troop, 0);

	public Banner Banner => _banner;

	public PrisonerAgentOrigin(CharacterObject troop)
	{
		_troop = troop;
		_banner = Clan.PlayerClan?.Banner;
		AgentOriginUtilities.GetDefaultTroopTraits(_troop, out _hasThrownWeapon, out _hasSpear, out _hasShield, out _hasHeavyArmor);
	}

	public void SetWounded()
	{
		if (_isRemoved)
		{
			return;
		}
		_isRemoved = true;
		if (_troop.IsHero)
		{
			_troop.HeroObject.MakeWounded();
		}
		else
		{
			PartyBase.MainParty.PrisonRoster.WoundTroop(_troop, 1, default(UniqueTroopDescriptor));
		}
	}

	public void SetKilled()
	{
		if (_isRemoved)
		{
			return;
		}
		_isRemoved = true;
		if (_troop.IsHero)
		{
			KillCharacterAction.ApplyByBattle(_troop.HeroObject, null, showNotification: true);
		}
		else
		{
			PartyBase.MainParty.PrisonRoster.AddToCounts(_troop, -1, false, 0, 0, true, -1);
		}
	}

	public void SetRouted(bool isOrderRetreat)
	{
	}

	public void OnAgentRemoved(float agentHealth)
	{
		if (_troop.IsHero)
		{
			_troop.HeroObject.HitPoints = MathF.Max(1, MathF.Round(agentHealth));
		}
	}

	void IAgentOriginBase.OnScoreHit(BasicCharacterObject victim, BasicCharacterObject formationCaptain, int damage, bool isFatal, bool isTeamKill, WeaponComponentData attackerWeapon)
	{
	}

	public void SetBanner(Banner banner)
	{
		_banner = banner;
	}

	TroopTraitsMask IAgentOriginBase.GetTraitsMask()
	{
		return AgentOriginUtilities.GetDefaultTraitsMask(this);
	}
}

[HarmonyPatch(typeof(SandBox.GameComponents.SandboxAgentDecideKilledOrUnconsciousModel), "GetAgentStateProbability")]
public static class TroopInspectionDeathRatePatch
{
	public static void Postfix(Agent affectorAgent, Agent effectedAgent, DamageTypes damageType, ref float __result)
	{
		if (Mission.Current?.GetMissionBehavior<TroopInspectionMissionLogic>() == null)
		{
			return;
		}
		if (effectedAgent == null || !effectedAgent.IsHuman)
		{
			return;
		}
		if (damageType == DamageTypes.Pierce || damageType == DamageTypes.Cut)
		{
			__result = 1f;
		}
	}
}

[HarmonyPatch(typeof(Mission), "CancelsDamageAndBlocksAttackBecauseOfNonEnemyCase")]
public static class TroopInspectionMeleeDamagePatch
{
	public static bool Prefix(Mission __instance, Agent attacker, Agent victim, ref bool __result)
	{
		if (attacker == null || victim == null || !attacker.IsMainAgent || !victim.IsHuman || !attacker.IsFriendOf(victim))
		{
			return true;
		}
		if (__instance?.GetMissionBehavior<TroopInspectionMissionLogic>() == null)
		{
			return true;
		}
		__result = false;
		return false;
	}
}

[HarmonyPatch]
public static class TroopInspectionOrderOfBattlePatch
{
	private const int RegularPrisonerFormationIndex = 6;

	private const int LordPrisonerFormationIndex = 7;

	private static readonly FieldInfo AllFormationsField = AccessTools.Field(typeof(OrderOfBattleVM), "_allFormations");

	[HarmonyPatch(typeof(OrderOfBattleFormationItemVM), nameof(OrderOfBattleFormationItemVM.RefreshFormation), new Type[] { typeof(Formation), typeof(DeploymentFormationClass), typeof(bool) })]
	[HarmonyPrefix]
	private static void RefreshFormationPrefix(Formation formation, ref DeploymentFormationClass overriddenClass, ref bool mustExist)
	{
		if (!IsTroopInspectionRuntime() || formation == null)
		{
			return;
		}
		switch (formation.Index)
		{
		case RegularPrisonerFormationIndex:
		case LordPrisonerFormationIndex:
			overriddenClass = DeploymentFormationClass.Cavalry;
			mustExist = true;
			break;
		}
	}

	[HarmonyPatch(typeof(OrderOfBattleVM), "EnsureAllFormationTypesAreSet")]
	[HarmonyPrefix]
	private static bool EnsureAllFormationTypesAreSetPrefix(OrderOfBattleFormationItemVM f)
	{
		if (!IsTroopInspectionRuntime() || f?.Formation == null)
		{
			return true;
		}
		int index = f.Formation.Index;
		return index != LordPrisonerFormationIndex && index != RegularPrisonerFormationIndex;
	}

	[HarmonyPatch(typeof(OrderOfBattleVM), nameof(OrderOfBattleVM.Tick))]
	[HarmonyPostfix]
	private static void TickPostfix(OrderOfBattleVM __instance)
	{
		if (!IsTroopInspectionRuntime() || __instance == null)
		{
			return;
		}
		try
		{
			List<OrderOfBattleFormationItemVM> allFormations = AllFormationsField?.GetValue(__instance) as List<OrderOfBattleFormationItemVM>;
			if (allFormations == null)
			{
				return;
			}
			RefreshPrisonerFormationItem(allFormations, RegularPrisonerFormationIndex, DeploymentFormationClass.Cavalry);
			RefreshPrisonerFormationItem(allFormations, LordPrisonerFormationIndex, DeploymentFormationClass.Cavalry);
		}
		catch
		{
		}
	}

	private static void RefreshPrisonerFormationItem(List<OrderOfBattleFormationItemVM> allFormations, int formationIndex, DeploymentFormationClass deploymentClass)
	{
		for (int i = 0; i < allFormations.Count; i++)
		{
			OrderOfBattleFormationItemVM item = allFormations[i];
			Formation formation = item?.Formation;
			if (formation == null || formation.Index != formationIndex)
			{
				continue;
			}
			int actualCount = formation.CountOfUnits;
			if (actualCount <= 0)
			{
				return;
			}
			if (item.OrderOfBattleFormationClassInt == 0 || item.TroopCount != actualCount || !item.IsSelectable)
			{
				item.RefreshFormation(formation, deploymentClass, mustExist: true);
				item.OnSizeChanged();
			}
			return;
		}
	}

	private static bool IsTroopInspectionRuntime()
	{
		try
		{
			return Mission.Current?.GetMissionBehavior<TroopInspectionMissionLogic>() != null;
		}
		catch
		{
			return false;
		}
	}
}
