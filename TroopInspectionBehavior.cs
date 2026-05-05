using System;
using System.Reflection;
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
		DetectDeploymentEnd();
		TryLogAgentCounts();
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
		Log($"mission_ended_check mission_result={missionResult?.ToString() ?? "null"} battle_end_disabled={_battleEndDisabled} deployment_detected={_deploymentEndDetected}");
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
	}

	private static void Log(string message)
	{
		Logger.Log("TroopInspection", "[TroopInspection] " + message);
	}
}
