using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Voxforge;

public class DuelBehavior : CampaignBehaviorBase
{
	private class DuelAfterLines
	{
		public string WinLine;

		public string LoseLine;

		public long UtcTicks;
	}

	private class PendingDuelStake
	{
		public int Gold;

		public string ItemId;

		public int ItemCount;

		public long UtcTicks;
	}

	private class ArenaDuelMissionBehavior : MissionBehavior
	{
		private readonly Hero _targetHero;

		private bool _loggedFirstTick;

		private bool _setupDone = false;

		private bool _localAgentsSpawned = false;

		private bool _localPreFightActive = false;

		private float _localPreFightTimer = 0f;

		private bool _localPostDuelFreezeActive = false;

		private float _localPostDuelExitTimer = 0f;

		private bool _localDuelResultRecorded = false;

		private bool _hadEnemyAgentEver = false;

		public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

		public ArenaDuelMissionBehavior(Hero target)
		{
			_targetHero = target;
		}

		public override void AfterStart()
		{
			base.AfterStart();
			try
			{
				if (base.Mission != null && !_setupDone)
				{
					base.Mission.SetMissionMode(MissionMode.Battle, atStart: true);
					_arenaMissionActive = true;
					SetupArenaDuel();
					_setupDone = _localAgentsSpawned;
				}
			}
			catch (Exception ex)
			{
				Logger.Log("ArenaDuel", "[ERROR] AfterStart: " + ex.ToString());
			}
		}

		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
		{
			try
			{
				if (!_localDuelResultRecorded && agentState != AgentState.Active)
				{
					Hero hero = null;
					try
					{
						hero = ((affectedAgent?.Character is CharacterObject characterObject) ? characterObject.HeroObject : null);
					}
					catch
					{
					}
					if (hero != null && hero == _targetHero)
					{
						EndDuelLocal(playerDefeated: false);
					}
					else if (affectedAgent != null && affectedAgent.IsMainAgent)
					{
						EndDuelLocal(playerDefeated: true);
					}
				}
			}
			catch
			{
			}
		}

		private void SetupArenaDuel()
		{
			if (base.Mission == null)
			{
				return;
			}
			if (base.Mission.Agents != null && base.Mission.Agents.Count > 0 && FindAgentForHero(_targetHero) != null)
			{
				_localAgentsSpawned = true;
				return;
			}
			try
			{
				Hero mainHero = Hero.MainHero;
				if (mainHero == null || _targetHero == null)
				{
					Logger.Log("ArenaDuel", "[Spawn] Hero.MainHero 或目标 Hero 为空，无法生成决斗双方。");
					return;
				}
				uint color = ((mainHero.MapFaction != null) ? mainHero.MapFaction.Color : 4278190335u);
				uint color2 = ((mainHero.MapFaction != null) ? mainHero.MapFaction.Color2 : 4278190208u);
				Banner banner = ((mainHero.Clan != null) ? mainHero.Clan.Banner : null);
				uint color3 = ((_targetHero.MapFaction != null) ? _targetHero.MapFaction.Color : 4294901760u);
				uint color4 = ((_targetHero.MapFaction != null) ? _targetHero.MapFaction.Color2 : 4286578688u);
				Banner banner2 = ((_targetHero.Clan != null) ? _targetHero.Clan.Banner : null);
				Team team = base.Mission.Teams.Add(BattleSideEnum.Attacker, color, color2, banner);
				Team team2 = base.Mission.Teams.Add(BattleSideEnum.Defender, color3, color4, banner2, isPlayerGeneral: false, isPlayerSergeant: true);
				base.Mission.PlayerTeam = team;
				Vec3 vec = new Vec3(156f, 113f);
				Vec3 position = vec + new Vec3(-4f);
				Vec3 position2 = vec + new Vec3(4f);
				Vec2 direction = new Vec2(1f, 0f);
				Vec2 direction2 = new Vec2(-1f, 0f);
				CharacterObject characterObject = mainHero.CharacterObject;
				CharacterObject characterObject2 = _targetHero.CharacterObject;
				Equipment equipment = mainHero.BattleEquipment.Clone();
				equipment[EquipmentIndex.ArmorItemEndSlot] = EquipmentElement.Invalid;
				equipment[EquipmentIndex.HorseHarness] = EquipmentElement.Invalid;
				Equipment equipment2 = _targetHero.BattleEquipment.Clone();
				equipment2[EquipmentIndex.ArmorItemEndSlot] = EquipmentElement.Invalid;
				equipment2[EquipmentIndex.HorseHarness] = EquipmentElement.Invalid;
				AgentBuildData agentBuildData = new AgentBuildData(characterObject).Team(team).Equipment(equipment).InitialPosition(in position)
					.InitialDirection(in direction);
				AgentBuildData agentBuildData2 = new AgentBuildData(characterObject2).Team(team2).Equipment(equipment2).InitialPosition(in position2)
					.InitialDirection(in direction2);
				Agent agent = base.Mission.SpawnAgent(agentBuildData);
				if (agent == null)
				{
					_localAgentsSpawned = false;
					Logger.Log("ArenaDuel", "[Spawn][ERROR] 玩家 Agent 生成失败，将在后续 Tick 重试。");
					return;
				}
				base.Mission.MainAgent = agent;
				try
				{
					Type typeFromHandle = typeof(Agent);
					PropertyInfo propertyInfo = typeFromHandle.GetProperty("Controller") ?? typeFromHandle.GetProperty("ControllerType");
					if (propertyInfo != null && propertyInfo.CanWrite)
					{
						Type propertyType = propertyInfo.PropertyType;
						object obj = null;
						if (propertyType.IsEnum)
						{
							obj = Enum.Parse(propertyType, "Player");
						}
						if (obj != null)
						{
							propertyInfo.SetValue(agent, obj);
						}
					}
				}
				catch
				{
				}
				Agent agent2 = base.Mission.SpawnAgent(agentBuildData2);
				if (agent2 == null)
				{
					_localAgentsSpawned = false;
					Logger.Log("ArenaDuel", "[Spawn][ERROR] 敌方 Agent 生成失败，将在后续 Tick 重试。");
					return;
				}
				_localAgentsSpawned = true;
				_hadEnemyAgentEver = true;
				agent2.SetTeam(team2, sync: true);
				team2.SetIsEnemyOf(team, isEnemyOf: true);
				team.SetIsEnemyOf(team2, isEnemyOf: true);
				SetAgentController(agent2, "None");
				agent.SetMortalityState(Agent.MortalityState.Invulnerable);
				agent2.SetMortalityState(Agent.MortalityState.Invulnerable);
				_localPreFightActive = true;
				float num = ((Mission.Current != null) ? Mission.Current.CurrentTime : 0f);
				_localPreFightTimer = num + 5f;
				_localPostDuelFreezeActive = false;
				_localDuelResultRecorded = false;
				agent2.SetWatchState(Agent.WatchState.Alarmed);
				InformationManager.DisplayMessage(new InformationMessage("双方就位！5秒后开始决斗！(无敌保护中)", Color.FromUint(4294901760u)));
				Logger.Log("ArenaDuel", $"[Spawn] 已在竞技场生成双方 Agent。Player={mainHero.Name}, Enemy={_targetHero.Name}");
			}
			catch (Exception ex)
			{
				Logger.Log("ArenaDuel", "[ERROR] SetupArenaDuel: " + ex.ToString());
			}
		}

		private void SetAgentController(Agent agent, string controllerType)
		{
			try
			{
				PropertyInfo propertyInfo = agent.GetType().GetProperty("Controller") ?? agent.GetType().GetProperty("ControllerType");
				if (propertyInfo != null && propertyInfo.CanWrite)
				{
					Type propertyType = propertyInfo.PropertyType;
					object value = Enum.Parse(propertyType, controllerType);
					propertyInfo.SetValue(agent, value);
				}
			}
			catch (Exception ex)
			{
				Logger.Log("ArenaDuel", "[Warning] Set Controller to " + controllerType + " failed: " + ex.Message);
			}
		}

		private Agent FindAgentForHero(Hero hero)
		{
			if (hero == null || base.Mission == null || base.Mission.Agents == null)
			{
				return null;
			}
			foreach (Agent agent in base.Mission.Agents)
			{
				if (agent.Character is CharacterObject characterObject && characterObject.HeroObject == hero)
				{
					_hadEnemyAgentEver = true;
					return agent;
				}
			}
			return null;
		}

		public override void OnMissionTick(float dt)
		{
			base.OnMissionTick(dt);
			if (!_setupDone)
			{
				SetupArenaDuel();
				_setupDone = _localAgentsSpawned;
				if (!_setupDone)
				{
					return;
				}
			}
			if (_localPreFightActive)
			{
				float currentTime = base.Mission.CurrentTime;
				if (currentTime >= _localPreFightTimer)
				{
					_localPreFightActive = false;
					Agent agent = FindAgentForHero(_targetHero);
					if (agent != null)
					{
						SetAgentController(agent, "AI");
						agent.SetWatchState(Agent.WatchState.Alarmed);
					}
					(base.Mission?.MainAgent ?? Agent.Main)?.SetMortalityState(Agent.MortalityState.Mortal);
					agent?.SetMortalityState(Agent.MortalityState.Mortal);
					InformationManager.DisplayMessage(new InformationMessage("决斗开始！", Color.FromUint(4294901760u)));
				}
			}
			if (_localPostDuelFreezeActive)
			{
				Agent agent2 = FindAgentForHero(_targetHero);
				if (agent2 != null)
				{
					agent2.SetMovementDirection(in Vec2.Zero);
					agent2.ClearTargetFrame();
				}
				float currentTime2 = base.Mission.CurrentTime;
				if (currentTime2 >= _localPostDuelExitTimer)
				{
					_localPostDuelFreezeActive = false;
					if (IsArenaMissionActive)
					{
						_arenaMissionLeaveRequested = true;
					}
					if (Instance != null)
					{
						Instance.FinishDuel();
					}
				}
			}
			if (!_localPreFightActive && !_localPostDuelFreezeActive && !_localDuelResultRecorded)
			{
				CheckDuelResult();
			}
			if (Input.IsKeyPressed(InputKey.Tab))
			{
				try
				{
					Logger.Log("ArenaDuel", "[Input] 用户按下了 TAB 键，请求退出。");
					InformationManager.DisplayMessage(new InformationMessage("正在退出竞技场...", Color.FromUint(4294901760u)));
					_arenaMissionLeaveRequested = true;
					if (Instance != null)
					{
						Instance.FinishDuel();
					}
				}
				catch (Exception ex)
				{
					Logger.Log("ArenaDuel", "[ERROR] TAB Key Handle: " + ex.ToString());
				}
			}
			if (!_loggedFirstTick)
			{
				_loggedFirstTick = true;
				try
				{
					int valueOrDefault = (base.Mission?.Agents?.Count).GetValueOrDefault();
					bool flag = Agent.Main != null;
					Logger.Log("ArenaDuel", $"[OnMissionTick] Agents={valueOrDefault}, HasMain={flag}");
				}
				catch (Exception ex2)
				{
					Logger.Log("ArenaDuel", "[ERROR] OnMissionTick: " + ex2.ToString());
				}
			}
			if (!_arenaMissionLeaveRequested || !_arenaMissionActive)
			{
				return;
			}
			try
			{
				float num = 0f;
				try
				{
					num = (Mission.Current ?? base.Mission)?.CurrentTime ?? 0f;
				}
				catch
				{
				}
				if (!(_arenaMissionLeaveReadyTime > 0f) || !(num < _arenaMissionLeaveReadyTime))
				{
					Logger.Log("ArenaDuel", "[Leave] 决斗结束，ArenaDuelMissionBehavior 收到离场请求，正在执行 EndMission...");
					Mission mission = Mission.Current ?? base.Mission;
					if (mission != null && !mission.IsMissionEnding)
					{
						_arenaMissionLeaveRequested = false;
						_arenaMissionLeaveReadyTime = 0f;
						mission.EndMission();
						_arenaMissionActive = false;
					}
				}
			}
			catch (Exception ex3)
			{
				Logger.Log("ArenaDuel", "[ERROR] OnMissionTick EndMission: " + ex3.ToString());
			}
		}

		private void CheckDuelResult()
		{
			if (!_setupDone)
			{
				return;
			}
			Agent agent = base.Mission?.MainAgent ?? Agent.Main;
			Agent agent2 = FindAgentForHero(_targetHero);
			if (agent == null)
			{
				return;
			}
			AgentState state = agent.State;
			switch (state)
			{
			case AgentState.Unconscious:
				try
				{
					agent.SetMortalityState(Agent.MortalityState.Mortal);
				}
				catch
				{
				}
				ForceKillAgentVisual(agent, agent2);
				ForceKillMainHero(_targetHero);
				Logger.Log("ArenaDuel", "判定: 玩家战败 (Unconscious->Death)");
				EndDuelLocal(playerDefeated: true);
				break;
			default:
				if (agent.IsActive() && !(agent.Health <= 0f))
				{
					if (agent2 == null)
					{
						if (_hadEnemyAgentEver)
						{
							Logger.Log("ArenaDuel", "判定: 玩家获胜 (敌方Agent已消失/被移除)");
							EndDuelLocal(playerDefeated: false);
						}
						else
						{
							Logger.Log("ArenaDuel", "[Spawn] 敌方Agent为null，跳过胜负判定，等待后续Tick。");
						}
						break;
					}
					if (!agent2.IsActive() || agent2.State == AgentState.Killed || agent2.State == AgentState.Unconscious || agent2.Health <= 0f)
					{
						Logger.Log("ArenaDuel", $"判定: 玩家获胜 (State={agent2.State}, Active={agent2.IsActive()}, HP={agent2.Health:0.0})");
						EndDuelLocal(playerDefeated: false);
						break;
					}
					float healthThreshold = DuelSettings.GetHealthThreshold();
					float num = agent.Health / agent.HealthLimit;
					float num2 = agent2.Health / agent2.HealthLimit;
					if (num <= healthThreshold)
					{
						Logger.Log("ArenaDuel", $"判定: 玩家战败 (HP {num:P0} <= {healthThreshold:P0})");
						EndDuelLocal(playerDefeated: true);
					}
					else if (num2 <= healthThreshold)
					{
						Logger.Log("ArenaDuel", $"判定: 玩家获胜 (HP {num2:P0} <= {healthThreshold:P0})");
						EndDuelLocal(playerDefeated: false);
					}
					break;
				}
				goto case AgentState.Killed;
			case AgentState.Killed:
				Logger.Log("ArenaDuel", $"判定: 玩家战败 (State={state})");
				EndDuelLocal(playerDefeated: true);
				break;
			}
		}

		private void EndDuelLocal(bool playerDefeated)
		{
			if (!_localDuelResultRecorded)
			{
				_localDuelResultRecorded = true;
				bool flag = !playerDefeated;
				if (Instance != null)
				{
					Instance._lastDuelResults[_targetHero.StringId] = (flag ? 1 : (-1));
				}
				_localPostDuelFreezeActive = true;
				float currentTime = base.Mission.CurrentTime;
				_localPostDuelExitTimer = currentTime + 10f;
				Agent agent = FindAgentForHero(_targetHero);
				TryPostDuelAiShout(_targetHero, agent, flag);
				if (agent != null && agent.IsActive())
				{
					SetAgentController(agent, "None");
					agent.SetMortalityState(Agent.MortalityState.Invulnerable);
				}
				if (Agent.Main != null && Agent.Main.IsActive())
				{
					Agent.Main.SetMortalityState(Agent.MortalityState.Invulnerable);
				}
				string text = ApplyDuelStakeSettlementAndBuildResultText(_targetHero, flag);
				string text2 = (flag ? "【决斗结果】你赢了！" : "【决斗结果】你输了！");
				Color color = (flag ? Color.FromUint(4281257073u) : Color.FromUint(4293348412u));
				InformationManager.DisplayMessage(new InformationMessage(text2 + text + " 10秒后退出竞技场...", color));
			}
		}
	}

	private sealed class DuelPlayerDeathAgentStateDeciderLogic : MissionLogic, IAgentStateDecider, IMissionBehavior
	{
		public AgentState GetAgentState(Agent effectedAgent, float deathProbability, out bool usedSurgery)
		{
			usedSurgery = false;
			try
			{
				DuelBehavior instance = Instance;
				if (instance != null && instance._isDuelActive && effectedAgent != null)
				{
					bool flag = false;
					try
					{
						flag = effectedAgent.IsMainAgent;
					}
					catch
					{
						flag = false;
					}
					bool flag2 = false;
					try
					{
						Hero hero = ((effectedAgent.Character is CharacterObject characterObject) ? characterObject.HeroObject : null);
						flag2 = hero != null && instance._targetHero != null && hero == instance._targetHero;
					}
					catch
					{
						flag2 = false;
					}
					if (flag || flag2)
					{
						return AgentState.Unconscious;
					}
				}
			}
			catch
			{
			}
			float num = deathProbability;
			if (num < 0f)
			{
				num = 0f;
			}
			if (num > 1f)
			{
				num = 1f;
			}
			return (MBRandom.RandomFloat <= num) ? AgentState.Killed : AgentState.Unconscious;
		}
	}

	private sealed class DuelMainHeroDeathMissionBehavior : MissionBehavior
	{
		public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
		{
			try
			{
				if (affectedAgent != null && affectedAgent.IsMainAgent && (agentState == AgentState.Unconscious || agentState == AgentState.Killed))
				{
					Hero hero = null;
					try
					{
						hero = ((affectorAgent?.Character is CharacterObject characterObject) ? characterObject.HeroObject : null);
					}
					catch
					{
					}
					ForceKillMainHero(Instance?._targetHero ?? hero);
				}
			}
			catch
			{
			}
		}
	}

	private sealed class DuelTargetDeathMissionBehavior : MissionBehavior
	{
		public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
		{
			try
			{
				DuelBehavior instance = Instance;
				if (instance != null && instance._isDuelActive && !_duelResultRecorded && instance._targetHero != null && (agentState == AgentState.Unconscious || agentState == AgentState.Killed))
				{
					Hero hero = null;
					try
					{
						hero = ((affectedAgent?.Character is CharacterObject characterObject) ? characterObject.HeroObject : null);
					}
					catch
					{
					}
					if (hero != null && hero == instance._targetHero)
					{
						instance.EndDuel(playerDefeated: false);
					}
				}
			}
			catch
			{
			}
		}
	}

	private const string VlandiaArenaSceneName = "arena_vlandia_a";

	private bool _isDuelActive = false;

	private Hero _targetHero = null;

	private Dictionary<string, float> _duelCooldowns = new Dictionary<string, float>();

	private Dictionary<string, int> _lastDuelResults = new Dictionary<string, int>();

	private Dictionary<string, DuelAfterLines> _lastDuelAfterLines = new Dictionary<string, DuelAfterLines>();

	private MissionMode _preDuelMode = MissionMode.Battle;

	private Team _preDuelTargetTeam;

	private bool _hasPreDuelTargetState;

	private Team _preDuelPlayerTeam;

	private bool _hasPreDuelPlayerState;

	private bool _currentDuelIsArena;

	private bool _meetingPreFightActive;

	private float _meetingPreFightEndTime;

	private bool _meetingPendingStart;

	private Team _duelPlayerTeam;

	private Team _duelEnemyTeam;

	private static Hero _pendingDuelTarget = null;

	private static float _preDuelTimer = 0f;

	private static bool _arenaMissionActive = false;

	private static bool _arenaMissionLeaveRequested = false;

	private static float _arenaMissionLeaveReadyTime = 0f;

	private static bool _duelResultRecorded = false;

	private static bool _forcedMainHeroDeath = false;

	private static bool _pendingMainHeroDeath = false;

	private static Hero _pendingMainHeroDeathKiller = null;

	private static long _pendingMainHeroDeathRequestUtcTicks;

	private static long _lastDuelRiskWarnUtcTicks;

	private static bool _nextDuelRiskWarningEnabled = true;

	private static bool _leaveSourceMissionRequested = false;

	private static float _leaveSourceMissionReadyTime = 0f;

	private static bool _openTownMenuRequested = false;

	private static Hero _queuedArenaDuelTarget = null;

	private static float _queuedArenaDuelDelay = 0f;

	private static Dictionary<string, PendingDuelStake> _pendingDuelStakes = new Dictionary<string, PendingDuelStake>();

	public static DuelBehavior Instance { get; private set; }

	public static bool IsArenaMissionActive => _arenaMissionActive;

	public static bool IsFormalDuelActive
	{
		get
		{
			try
			{
				return Instance != null && Instance._isDuelActive;
			}
			catch
			{
				return false;
			}
		}
	}

	public static bool IsFormalDuelPreFightActive
	{
		get
		{
			try
			{
				return Instance != null && Instance._isDuelActive && !Instance._currentDuelIsArena && Instance._meetingPreFightActive;
			}
			catch
			{
				return false;
			}
		}
	}

	public static bool IsDuelEnded => _duelResultRecorded;

	public override void RegisterEvents()
	{
		Instance = this;
	}

	public override void SyncData(IDataStore dataStore)
	{
		try
		{
			dataStore.SyncData("_duelCooldowns", ref _duelCooldowns);
		}
		catch (Exception ex)
		{
			Logger.Log("DuelBehavior", "[ERROR] SyncData failed, cooldowns cleared to protect save: " + ex.Message);
			_duelCooldowns = new Dictionary<string, float>();
		}
		if (_duelCooldowns == null)
		{
			_duelCooldowns = new Dictionary<string, float>();
		}
	}

	public static void PrepareDuel(Hero target, float delaySeconds)
	{
		if (target == null)
		{
			Logger.Log("DuelBehavior", "[ArenaTeleport] 收到空目标的决斗请求，已忽略。");
			return;
		}
		ShowDuelRiskWarning();
		bool flag = false;
		try
		{
			flag = LordEncounterBehavior.IsEncounterMeetingMissionActive;
		}
		catch
		{
		}
		bool flag2 = !flag;
		if (!flag2)
		{
			if (Instance != null && Mission.Current != null)
			{
				Logger.Log("DuelBehavior", "[ArenaTeleport] 检测到会面场景，禁用竞技场传送，改为原地决斗。");
				Instance.StartDuelViaAI(target);
				return;
			}
			flag2 = true;
		}
		if (flag2)
		{
			if (Mission.Current != null && Mission.Current.SceneName != null && Mission.Current.SceneName.Equals("arena_vlandia_a", StringComparison.OrdinalIgnoreCase))
			{
				Logger.Log("DuelBehavior", "[ArenaTeleport] 当前已在竞技场，直接准备决斗。");
			}
			else
			{
				if (Mission.Current != null)
				{
					Logger.Log("DuelBehavior", "[Queue] 当前在场景 " + Mission.Current.SceneName + "，将在 10 秒后退出并前往竞技场。");
					_queuedArenaDuelTarget = target;
					_queuedArenaDuelDelay = delaySeconds;
					float num = ((Mission.Current != null) ? Mission.Current.CurrentTime : 0f);
					_leaveSourceMissionReadyTime = num + 10f;
					InformationManager.DisplayMessage(new InformationMessage("双方怒目而视，约定 10 秒后前往竞技场一决胜负！", Color.FromUint(4294901760u)));
					_leaveSourceMissionRequested = true;
					return;
				}
				if (Instance != null)
				{
					Logger.Log("DuelBehavior", "[ArenaTeleport] 当前无 Active Mission，直接启动竞技场。");
					Instance.TryTeleportToArenaForDuel(target);
					return;
				}
			}
		}
		_pendingDuelTarget = target;
		_preDuelTimer = delaySeconds;
		string information = $"[系统] 双方约定 {delaySeconds:F0} 秒后开始决斗！";
		InformationManager.DisplayMessage(new InformationMessage(information, Color.FromUint(4294901760u)));
	}

	public static void GlobalDuelStarterTick()
	{
		if (_queuedArenaDuelTarget == null || Mission.Current != null)
		{
			return;
		}
		try
		{
			Hero queuedArenaDuelTarget = _queuedArenaDuelTarget;
			_queuedArenaDuelTarget = null;
			Logger.Log("DuelBehavior", $"[Queue] 监测到 Mission 已退出，正在启动排队的竞技场决斗: Target={queuedArenaDuelTarget.Name}");
			if (Instance != null)
			{
				Instance.TryTeleportToArenaForDuel(queuedArenaDuelTarget);
			}
			else
			{
				Logger.Log("DuelBehavior", "[Queue] [ERROR] Instance 为空，无法启动决斗。");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("DuelBehavior", "[Queue] [ERROR] 启动排队决斗失败: " + ex.ToString());
		}
	}

	public static bool TryConsumeLastDuelResult(Hero hero, out bool playerWon)
	{
		playerWon = false;
		if (Instance == null || hero == null)
		{
			return false;
		}
		string stringId = hero.StringId;
		if (string.IsNullOrEmpty(stringId) || Instance._lastDuelResults == null)
		{
			return false;
		}
		if (Instance._lastDuelResults.TryGetValue(stringId, out var value))
		{
			playerWon = value > 0;
			Instance._lastDuelResults.Remove(stringId);
			Logger.Log("DuelBehavior", $"对话系统消费决斗结果: Hero={hero.Name}, PlayerWon={playerWon}");
			return true;
		}
		return false;
	}

	public static bool TryCacheDuelAfterLinesFromText(Hero hero, ref string responseText)
	{
		try
		{
			if (Instance == null || hero == null || string.IsNullOrEmpty(responseText))
			{
				return false;
			}
			string stringId = hero.StringId;
			if (string.IsNullOrEmpty(stringId))
			{
				return false;
			}
			if (Instance._lastDuelAfterLines == null)
			{
				Instance._lastDuelAfterLines = new Dictionary<string, DuelAfterLines>();
			}
			string winLine = null;
			string loseLine = null;
			bool any = false;
			Regex regex = new Regex("\\[ACTION:DUEL_LINE_WIN:([^\\]]+)\\]", RegexOptions.IgnoreCase);
			Regex regex2 = new Regex("\\[ACTION:DUEL_LINE_LOSE:([^\\]]+)\\]", RegexOptions.IgnoreCase);
			responseText = regex.Replace(responseText, delegate(Match m)
			{
				any = true;
				winLine = ((m.Groups.Count > 1) ? m.Groups[1].Value : "")?.Trim();
				return string.Empty;
			});
			responseText = regex2.Replace(responseText, delegate(Match m)
			{
				any = true;
				loseLine = ((m.Groups.Count > 1) ? m.Groups[1].Value : "")?.Trim();
				return string.Empty;
			});
			if (!any)
			{
				return false;
			}
			if (!Instance._lastDuelAfterLines.TryGetValue(stringId, out var value) || value == null)
			{
				value = new DuelAfterLines();
				Instance._lastDuelAfterLines[stringId] = value;
			}
			if (!string.IsNullOrWhiteSpace(winLine))
			{
				value.WinLine = winLine;
			}
			if (!string.IsNullOrWhiteSpace(loseLine))
			{
				value.LoseLine = loseLine;
			}
			value.UtcTicks = DateTime.UtcNow.Ticks;
			responseText = responseText.Trim();
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool TryCacheDuelStakeFromText(Hero hero, ref string responseText)
	{
		try
		{
			if (!AIConfigHandler.DuelStakeEnabled)
			{
				return false;
			}
			if (hero == null || string.IsNullOrEmpty(responseText))
			{
				return false;
			}
			string stringId = hero.StringId;
			if (string.IsNullOrEmpty(stringId))
			{
				return false;
			}
			int stakeGold = 0;
			string stakeItemId = null;
			int stakeItemCount = 0;
			bool any = false;
			Regex regex = new Regex("\\[ACTION:DUEL_STAKE[_-]GOLD:(\\d+)\\]", RegexOptions.IgnoreCase);
			Regex regex2 = new Regex("\\[ACTION:DUEL_STAKE[_-]ITEM:([a-zA-Z0-9_]+):(\\d+)\\]", RegexOptions.IgnoreCase);
			responseText = regex.Replace(responseText, delegate(Match m)
			{
				any = true;
				if (int.TryParse(m.Groups[1].Value, out var result2))
				{
					stakeGold = Math.Max(stakeGold, result2);
				}
				return string.Empty;
			});
			responseText = regex2.Replace(responseText, delegate(Match m)
			{
				any = true;
				string value = m.Groups[1].Value;
				if (int.TryParse(m.Groups[2].Value, out var result2) && result2 > 0 && !string.IsNullOrEmpty(value))
				{
					stakeItemId = value;
					stakeItemCount = result2;
				}
				return string.Empty;
			});
			if (responseText.IndexOf("[ACTION:DUEL_STAKE", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				Regex regex3 = new Regex("\\[ACTION:DUEL_STAKE[^\\]\\r\\n]*\\]?", RegexOptions.IgnoreCase);
				responseText = regex3.Replace(responseText, string.Empty);
			}
			if (!any && (responseText.Contains("赌") || responseText.Contains("赌注") || responseText.Contains("押") || responseText.Contains("压") || responseText.Contains("筹码")))
			{
				Regex regex4 = new Regex("(?:赌|赌注|押|压|筹码)[^0-9]{0,12}(\\d{1,9})\\s*第纳尔", RegexOptions.IgnoreCase);
				Match match = regex4.Match(responseText);
				if (match.Success && int.TryParse(match.Groups[1].Value, out var result))
				{
					stakeGold = Math.Max(stakeGold, result);
					any = true;
				}
			}
			if (!any)
			{
				responseText = responseText.Trim();
				return false;
			}
			CachePendingDuelStake(stringId, stakeGold, stakeItemId, stakeItemCount);
			responseText = responseText.Trim();
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void CachePendingDuelStake(string heroId, int gold, string itemId, int itemCount)
	{
		if (!string.IsNullOrEmpty(heroId))
		{
			if (_pendingDuelStakes == null)
			{
				_pendingDuelStakes = new Dictionary<string, PendingDuelStake>();
			}
			if (!_pendingDuelStakes.TryGetValue(heroId, out var value) || value == null)
			{
				value = new PendingDuelStake();
				_pendingDuelStakes[heroId] = value;
			}
			if (gold > 0)
			{
				value.Gold = gold;
			}
			if (!string.IsNullOrEmpty(itemId) && itemCount > 0)
			{
				value.ItemId = itemId;
				value.ItemCount = itemCount;
			}
			value.UtcTicks = DateTime.UtcNow.Ticks;
		}
	}

	private static bool TryConsumePendingDuelStake(string heroId, out PendingDuelStake stake)
	{
		stake = null;
		try
		{
			if (string.IsNullOrEmpty(heroId) || _pendingDuelStakes == null)
			{
				return false;
			}
			if (!_pendingDuelStakes.TryGetValue(heroId, out stake) || stake == null)
			{
				return false;
			}
			_pendingDuelStakes.Remove(heroId);
			return true;
		}
		catch
		{
			stake = null;
			return false;
		}
	}

	private static string ApplyDuelStakeSettlementAndBuildResultText(Hero targetHero, bool playerWon)
	{
		try
		{
			if (targetHero == null || string.IsNullOrEmpty(targetHero.StringId))
			{
				return "";
			}
			if (!TryConsumePendingDuelStake(targetHero.StringId, out var stake) || stake == null)
			{
				return "";
			}
			string text = targetHero?.Name?.ToString() ?? "NPC";
			RewardSystemBehavior instance = RewardSystemBehavior.Instance;
			if (playerWon)
			{
				if (instance == null)
				{
					return " " + text + "没有结算赌注。";
				}
				if (stake.Gold > 0)
				{
					int num = 0;
					try
					{
						num = instance.TransferGold(targetHero, Hero.MainHero, stake.Gold);
					}
					catch
					{
					}
					if (num > 0)
					{
						MyBehavior.AppendExternalDialogueHistory(targetHero, null, null, $"你已经将 {num} 第纳尔交给玩家（决斗赌注）。");
						MyBehavior.AppendExternalDialogueHistory(Hero.MainHero, null, null, $"你从 {text} 收到了 {num} 第纳尔（决斗赌注）。");
					}
					if (num >= stake.Gold)
					{
						return $" 你从 {text} 收到了 {num} 第纳尔（决斗赌注）。";
					}
					if (num > 0)
					{
						return $" 你从 {text} 收到了 {num} 第纳尔（决斗赌注），但对方无法支付剩余 {stake.Gold - num}。";
					}
					return $" 但 {text}无法支付赌注 {stake.Gold} 第纳尔。";
				}
				if (!string.IsNullOrEmpty(stake.ItemId) && stake.ItemCount > 0)
				{
					int num2 = 0;
					string itemName = null;
					try
					{
						num2 = instance.TransferItemById(targetHero, Hero.MainHero, stake.ItemId, stake.ItemCount, out itemName);
					}
					catch
					{
					}
					string text2 = (string.IsNullOrEmpty(itemName) ? stake.ItemId : itemName);
					if (num2 > 0)
					{
						MyBehavior.AppendExternalDialogueHistory(targetHero, null, null, $"你已经将 {num2} 个 {text2} 交给玩家（决斗赌注）。");
						MyBehavior.AppendExternalDialogueHistory(Hero.MainHero, null, null, $"你从 {text} 收到了 {num2} 个 {text2}（决斗赌注）。");
					}
					if (num2 >= stake.ItemCount)
					{
						return $" 你从 {text} 收到了 {num2} 个 {text2}（决斗赌注）。";
					}
					if (num2 > 0)
					{
						return $" 你从 {text} 收到了 {num2} 个 {text2}（决斗赌注），但对方无法支付剩余 {stake.ItemCount - num2} 个。";
					}
					return $" 但 {text}无法支付赌注 {text2} x{stake.ItemCount}。";
				}
				return "";
			}
			if (instance != null)
			{
				instance.GetDebtSnapshot(targetHero, out var owedGold, out var owedItems);
				if (stake.Gold > 0)
				{
					owedGold += stake.Gold;
				}
				if (!string.IsNullOrEmpty(stake.ItemId) && stake.ItemCount > 0)
				{
					if (owedItems == null)
					{
						owedItems = new Dictionary<string, int>();
					}
					if (!owedItems.ContainsKey(stake.ItemId))
					{
						owedItems[stake.ItemId] = 0;
					}
					owedItems[stake.ItemId] += stake.ItemCount;
				}
				instance.SetDebt(targetHero, owedGold, owedItems);
			}
			if (stake.Gold > 0)
			{
				MyBehavior.AppendExternalDialogueHistory(targetHero, null, null, $"你已经记下：玩家欠你 {stake.Gold} 第纳尔（决斗赌注）。");
				MyBehavior.AppendExternalDialogueHistory(Hero.MainHero, null, null, $"你欠 {text} {stake.Gold} 第纳尔（决斗赌注）。");
				return $" 你现在欠{text} {stake.Gold} 第纳尔（决斗赌注）。";
			}
			if (!string.IsNullOrEmpty(stake.ItemId) && stake.ItemCount > 0)
			{
				MyBehavior.AppendExternalDialogueHistory(targetHero, null, null, $"你已经记下：玩家欠你 {stake.ItemId} x{stake.ItemCount}（决斗赌注）。");
				MyBehavior.AppendExternalDialogueHistory(Hero.MainHero, null, null, $"你欠 {text} {stake.ItemId} x{stake.ItemCount}（决斗赌注）。");
				return $" 你现在欠{text} {stake.ItemId} x{stake.ItemCount}（决斗赌注）。";
			}
			return "";
		}
		catch
		{
			return "";
		}
	}

	private bool TryConsumeDuelAfterLines(Hero hero, out DuelAfterLines lines)
	{
		lines = null;
		try
		{
			if (hero == null)
			{
				return false;
			}
			string stringId = hero.StringId;
			if (string.IsNullOrEmpty(stringId) || _lastDuelAfterLines == null)
			{
				return false;
			}
			if (!_lastDuelAfterLines.TryGetValue(stringId, out lines) || lines == null)
			{
				return false;
			}
			_lastDuelAfterLines.Remove(stringId);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void TryPostDuelAiShout(Hero targetHero, Agent targetAgent, bool playerWon)
	{
		try
		{
			if (Instance == null || targetHero == null)
			{
				return;
			}
			string text = null;
			if (Instance.TryConsumeDuelAfterLines(targetHero, out var lines) && lines != null)
			{
				text = (playerWon ? lines.LoseLine : lines.WinLine);
				if (string.IsNullOrWhiteSpace(text))
				{
					text = (playerWon ? lines.WinLine : lines.LoseLine);
				}
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				text = (playerWon ? "……算你厉害。" : "哼，不过如此。");
			}
			bool flag = false;
			try
			{
				bool flag2 = false;
				try
				{
					List<MissionBehavior> list = Mission.Current?.MissionBehaviors;
					if (list != null)
					{
						foreach (MissionBehavior item in list)
						{
							if (item != null)
							{
								string text2 = item.GetType().FullName ?? "";
								if (text2 == "Voxforge.ShoutBehavior+ShoutMissionBehavior")
								{
									flag2 = true;
									break;
								}
							}
						}
					}
				}
				catch
				{
				}
				if ((targetAgent?.IsActive() ?? false) && flag2)
				{
					try
					{
						ShoutBehavior.TrySystemNpcShout(targetAgent, text);
						flag = true;
					}
					catch
					{
						flag = false;
					}
				}
			}
			catch
			{
			}
			if (!flag)
			{
				string text3 = targetHero?.Name?.ToString() ?? "NPC";
				try
				{
					InformationManager.DisplayMessage(new InformationMessage("[" + text3 + "] " + text, new Color(1f, 0.8f, 0.2f)));
				}
				catch
				{
				}
			}
			try
			{
				MyBehavior.AppendExternalDialogueHistory(targetHero, null, text, null);
			}
			catch
			{
			}
		}
		catch
		{
		}
	}

	public void StartDuelViaAI(Hero target)
	{
		if (Mission.Current == null || target == null)
		{
			return;
		}
		ShowDuelRiskWarning();
		bool flag = false;
		try
		{
			Campaign current = Campaign.Current;
			flag = current != null && current.ConversationManager?.IsConversationInProgress == true;
		}
		catch
		{
		}
		if (flag)
		{
			_targetHero = target;
			_meetingPendingStart = true;
			try
			{
				Agent.Main?.SetMortalityState(Agent.MortalityState.Invulnerable);
			}
			catch
			{
			}
			Agent agent = GetAgent(target);
			if (agent != null)
			{
				try
				{
					agent.SetMortalityState(Agent.MortalityState.Invulnerable);
				}
				catch
				{
				}
				TrySetAgentController(agent, "None");
				try
				{
					agent.SetIsAIPaused(isPaused: true);
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
			}
			InformationManager.DisplayMessage(new InformationMessage("已接受决斗。请你手动结束对话；结束后将进入 10 秒准备期，然后正式开战（准备期双方无法互相伤害）。", Color.FromUint(4281637083u)));
		}
		else
		{
			StartDuelInternal(target);
		}
	}

	public void OnEngineTick()
	{
		GlobalDuelStarterTick();
		GlobalSourceMissionLeaveTick();
		GlobalArenaLeaveTick();
		GlobalTownMenuTick();
		if (Mission.Current == null)
		{
			return;
		}
		if (_meetingPendingStart && _targetHero != null)
		{
			bool flag = false;
			try
			{
				Campaign current = Campaign.Current;
				flag = current != null && current.ConversationManager?.IsConversationInProgress == true;
			}
			catch
			{
			}
			if (!flag)
			{
				_meetingPendingStart = false;
				StartDuelInternal(_targetHero);
			}
		}
		else
		{
			if (!_isDuelActive || _targetHero == null)
			{
				return;
			}
			Agent agent = GetAgent(_targetHero);
			Agent main = Agent.Main;
			if (main == null || agent == null)
			{
				return;
			}
			if (!_currentDuelIsArena && _meetingPreFightActive)
			{
				float currentTime = Mission.Current.CurrentTime;
				if (currentTime < _meetingPreFightEndTime)
				{
					RefreshMeetingDuelParticipantLocks(main, agent, preFight: true);
					return;
				}
				_meetingPreFightActive = false;
				try
				{
					if (_duelPlayerTeam != null && _duelEnemyTeam != null)
					{
						foreach (Team team in Mission.Current.Teams)
						{
							if (team != null && team != _duelPlayerTeam && team != _duelEnemyTeam)
							{
								try
								{
									team.SetIsEnemyOf(_duelPlayerTeam, isEnemyOf: false);
								}
								catch
								{
								}
								try
								{
									_duelPlayerTeam.SetIsEnemyOf(team, isEnemyOf: false);
								}
								catch
								{
								}
								try
								{
									team.SetIsEnemyOf(_duelEnemyTeam, isEnemyOf: false);
								}
								catch
								{
								}
								try
								{
									_duelEnemyTeam.SetIsEnemyOf(team, isEnemyOf: false);
								}
								catch
								{
								}
							}
						}
						try
						{
							_duelEnemyTeam.SetIsEnemyOf(_duelPlayerTeam, isEnemyOf: true);
						}
						catch
						{
						}
						try
						{
							_duelPlayerTeam.SetIsEnemyOf(_duelEnemyTeam, isEnemyOf: true);
						}
						catch
						{
						}
					}
				}
				catch
				{
				}
				try
				{
					main.SetMortalityState(Agent.MortalityState.Mortal);
				}
				catch
				{
				}
				try
				{
					agent.SetMortalityState(Agent.MortalityState.Mortal);
				}
				catch
				{
				}
				RefreshMeetingDuelParticipantLocks(main, agent, preFight: false);
				float healthThreshold = DuelSettings.GetHealthThreshold();
				string arg = Mission.Current?.SceneName ?? "Unknown";
				string information = $"【决斗开始】当前场景: {arg}。规则：任一方生命值低于 {healthThreshold:P0} 判定为战败。";
				InformationManager.DisplayMessage(new InformationMessage(information, Color.FromUint(4294901760u)));
			}
			else if (main.State == AgentState.Unconscious)
			{
				try
				{
					main.SetMortalityState(Agent.MortalityState.Mortal);
				}
				catch
				{
				}
				ForceKillAgentVisual(main, agent);
				ForceKillMainHero(_targetHero);
				Logger.Log("DuelBehavior", "判定: 玩家战败 (Unconscious->Death)");
				EndDuel(playerDefeated: true);
			}
			else if (!main.IsActive() || main.State == AgentState.Killed)
			{
				Logger.Log("DuelBehavior", $"判定: 玩家战败 (State={main.State})");
				EndDuel(playerDefeated: true);
			}
			else if (!agent.IsActive() || agent.State == AgentState.Killed || agent.State == AgentState.Unconscious || agent.Health <= 0f)
			{
				Logger.Log("DuelBehavior", $"判定: 玩家获胜 (State={agent.State}, Active={agent.IsActive()}, HP={agent.Health:0.0})");
				EndDuel(playerDefeated: false);
			}
			else
			{
				float healthThreshold2 = DuelSettings.GetHealthThreshold();
				float num = main.Health / main.HealthLimit;
				float num2 = agent.Health / agent.HealthLimit;
				if (num <= healthThreshold2)
				{
					Logger.Log("DuelBehavior", $"判定: 玩家战败 (HP {num:P0} <= {healthThreshold2:P0})");
					EndDuel(playerDefeated: true);
				}
				else if (num2 <= healthThreshold2)
				{
					Logger.Log("DuelBehavior", $"判定: 玩家获胜 (HP {num2:P0} <= {healthThreshold2:P0})");
					EndDuel(playerDefeated: false);
				}
			}
		}
	}

	private static void UnlockAgentMovement(Agent agent, bool unpauseAi, bool clearTargetFrame)
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
		if (clearTargetFrame)
		{
			try
			{
				agent.ClearTargetFrame();
			}
			catch
			{
			}
		}
		if (!unpauseAi)
		{
			return;
		}
		try
		{
			agent.SetIsAIPaused(isPaused: false);
		}
		catch
		{
		}
	}

	private void RefreshMeetingDuelParticipantLocks(Agent playerAgent, Agent targetAgent, bool preFight)
	{
		UnlockAgentMovement(playerAgent, unpauseAi: true, clearTargetFrame: true);
		Agent agent = null;
		try
		{
			agent = playerAgent?.MountAgent;
		}
		catch
		{
			agent = null;
		}
		if (agent != null && agent.IsActive())
		{
			try
			{
				agent.SetIsAIPaused(isPaused: false);
			}
			catch
			{
			}
			UnlockAgentMovement(agent, unpauseAi: true, clearTargetFrame: true);
		}
		if (targetAgent == null || !targetAgent.IsActive())
		{
			return;
		}
		if (preFight)
		{
			TrySetAgentController(targetAgent, "None");
			try
			{
				if (targetAgent.IsAIControlled)
				{
					targetAgent.SetIsAIPaused(isPaused: true);
				}
			}
			catch
			{
			}
		}
		else
		{
			TrySetAgentController(targetAgent, "AI");
			try
			{
				if (targetAgent.IsAIControlled)
				{
					targetAgent.SetIsAIPaused(isPaused: false);
				}
			}
			catch
			{
			}
		}
		UnlockAgentMovement(targetAgent, !preFight, clearTargetFrame: true);
		Agent agent2 = null;
		try
		{
			agent2 = targetAgent.MountAgent;
		}
		catch
		{
			agent2 = null;
		}
		if (agent2 != null && agent2.IsActive())
		{
			TrySetAgentController(agent2, preFight ? "None" : "AI");
			try
			{
				agent2.SetIsAIPaused(preFight);
			}
			catch
			{
			}
			UnlockAgentMovement(agent2, !preFight, clearTargetFrame: true);
		}
		if (preFight)
		{
			return;
		}
		try
		{
			targetAgent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.MeleeForMainHand);
		}
		catch
		{
		}
		try
		{
			targetAgent.SetWatchState(Agent.WatchState.Alarmed);
		}
		catch
		{
		}
	}

	internal static void ShowDuelRiskWarning()
	{
		try
		{
			if (!_nextDuelRiskWarningEnabled)
			{
				_nextDuelRiskWarningEnabled = true;
				return;
			}
			_nextDuelRiskWarningEnabled = true;
			long ticks = DateTime.UtcNow.Ticks;
			if (_lastDuelRiskWarnUtcTicks == 0L || ticks - _lastDuelRiskWarnUtcTicks >= TimeSpan.FromSeconds(10.0).Ticks)
			{
				_lastDuelRiskWarnUtcTicks = ticks;
				InformationManager.DisplayMessage(new InformationMessage("【警告!决斗具有较大风险，你有可能在决斗中死亡】", Color.FromUint(4294936576u)));
			}
		}
		catch
		{
		}
	}

	public static void SetNextDuelRiskWarningEnabled(bool enabled)
	{
		_nextDuelRiskWarningEnabled = enabled;
	}

	private void StartDuelInternal(Hero target)
	{
		if (Hero.MainHero.Clan.Tier < DuelSettings.GetSettings().MinimumClanTier)
		{
			Logger.Log("DuelBehavior", "决斗失败: 玩家家族等级不足");
			return;
		}
		_targetHero = target;
		string text = Mission.Current?.SceneName ?? "Unknown";
		bool flag = text.Equals("arena_vlandia_a", StringComparison.OrdinalIgnoreCase);
		if (flag)
		{
			Logger.Log("DuelBehavior", "[ArenaInfo] 当前已在瓦兰迪亚竞技场场景 (arena_vlandia_a) 内发起决斗。");
		}
		else
		{
			Logger.Log("DuelBehavior", "[ArenaInfo] 当前场景 " + text + " 非瓦兰迪亚竞技场 (arena_vlandia_a)，暂时仍在原地决斗。");
		}
		Agent agent = GetAgent(target);
		if (agent != null)
		{
			Mission current = Mission.Current;
			if (current != null)
			{
				_preDuelMode = current.Mode;
			}
			_duelResultRecorded = false;
			_forcedMainHeroDeath = false;
			EnsureDeathBehaviorsPresent();
			_preDuelTargetTeam = agent.Team;
			_hasPreDuelTargetState = true;
			_preDuelPlayerTeam = Agent.Main?.Team ?? current.PlayerTeam;
			_hasPreDuelPlayerState = _preDuelPlayerTeam != null;
			_currentDuelIsArena = flag;
			_meetingPreFightActive = false;
			if (!_arenaMissionActive && flag && current != null && (current.Mode == MissionMode.Conversation || Campaign.Current.ConversationManager.OneToOneConversationAgent != null))
			{
				Campaign.Current.ConversationManager?.EndConversation();
			}
			_isDuelActive = true;
			_duelCooldowns[_targetHero.StringId] = (float)CampaignTime.Now.ToDays;
			DuelSettings settings = DuelSettings.GetSettings();
			if (current != null)
			{
				current.SetMissionMode(MissionMode.Battle, atStart: true);
				if (!flag)
				{
					_duelPlayerTeam = Agent.Main?.Team ?? current.PlayerTeam;
					_duelEnemyTeam = agent.Team;
					if (_duelEnemyTeam == null || _duelEnemyTeam == _duelPlayerTeam)
					{
						Team team = null;
						try
						{
							team = current.PlayerEnemyTeam;
						}
						catch
						{
						}
						if (team != null && team != _duelPlayerTeam)
						{
							_duelEnemyTeam = team;
							try
							{
								agent.SetTeam(_duelEnemyTeam, sync: true);
							}
							catch
							{
							}
							Logger.Log("DuelBehavior", "[MeetingDuel] 目标与玩家同队，已切到原生敌方队伍作为决斗对手。");
						}
					}
					if (_duelPlayerTeam == null || _duelEnemyTeam == null || _duelEnemyTeam == _duelPlayerTeam)
					{
						Logger.Log("DuelBehavior", "[MeetingDuel][ERROR] 无法建立稳定的决斗队伍关系，已取消本次决斗以避免闪退。");
						_isDuelActive = false;
						return;
					}
					try
					{
						foreach (Team team2 in current.Teams)
						{
							if (team2 != null && team2 != _duelPlayerTeam && team2 != _duelEnemyTeam)
							{
								try
								{
									team2.SetIsEnemyOf(_duelPlayerTeam, isEnemyOf: false);
								}
								catch
								{
								}
								try
								{
									_duelPlayerTeam.SetIsEnemyOf(team2, isEnemyOf: false);
								}
								catch
								{
								}
								try
								{
									team2.SetIsEnemyOf(_duelEnemyTeam, isEnemyOf: false);
								}
								catch
								{
								}
								try
								{
									_duelEnemyTeam.SetIsEnemyOf(team2, isEnemyOf: false);
								}
								catch
								{
								}
							}
						}
						if (_duelPlayerTeam != null && _duelEnemyTeam != null)
						{
							try
							{
								_duelEnemyTeam.SetIsEnemyOf(_duelPlayerTeam, isEnemyOf: true);
							}
							catch
							{
							}
							try
							{
								_duelPlayerTeam.SetIsEnemyOf(_duelEnemyTeam, isEnemyOf: true);
							}
							catch
							{
							}
						}
					}
					catch
					{
					}
					TrySetAgentController(agent, "None");
					try
					{
						agent.SetIsAIPaused(isPaused: true);
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
						Agent.Main.SetMortalityState(Agent.MortalityState.Invulnerable);
					}
					catch
					{
					}
					try
					{
						agent.SetMortalityState(Agent.MortalityState.Invulnerable);
					}
					catch
					{
					}
					_meetingPreFightActive = true;
					_meetingPreFightEndTime = current.CurrentTime + 10f;
					RefreshMeetingDuelParticipantLocks(Agent.Main, agent, preFight: true);
					InformationManager.DisplayMessage(new InformationMessage("双方就位！10秒后正式开战（此期间无法互相伤害）", Color.FromUint(4281637083u)));
				}
				else
				{
					Logger.Log("DuelBehavior", "[ArenaInfo] 竞技场 Mission 中保持目标在当前队伍，不再依赖 PlayerEnemyTeam。");
				}
			}
			agent.SetWatchState(Agent.WatchState.Alarmed);
			if (Agent.Main != null)
			{
				Agent.Main.Health = Agent.Main.HealthLimit;
			}
			agent.Health = agent.HealthLimit;
			string text2 = Mission.Current?.SceneName ?? "Unknown";
			if (flag)
			{
				string information = $"【竞技场决斗已开始】当前场景: {text2}。规则：任一方生命值低于 {DuelSettings.GetHealthThreshold():P0} 判定为战败。";
				InformationManager.DisplayMessage(new InformationMessage(information, Color.FromUint(4281637083u)));
			}
			Logger.Log("DuelBehavior", $"决斗已启动/初始化! 目标: {_targetHero.Name}, 场景: {text2}, 模式已切换为 Duel");
		}
		else
		{
			Logger.Log("DuelBehavior", "决斗启动失败: 找不到目标的 Agent 实体");
		}
	}

	private bool TryTeleportToArenaForDuel(Hero target)
	{
		try
		{
			string text = "arena_vlandia_a";
			string text2 = Mission.Current?.SceneName ?? "Unknown";
			string text3 = Hero.MainHero?.CurrentSettlement?.StringId ?? "";
			Logger.Log("DuelBehavior", "[ArenaTeleport] 尝试通过 MissionState.OpenNew 切换到竞技场。CurrentScene=" + text2 + ", TargetScene=" + text + ", SettlementId=" + text3 + ", Target=" + target?.StringId);
			MissionInitializerRecord rec = new MissionInitializerRecord(text);
			MissionState.OpenNew("Voxforge_ArenaDuel", rec, (Mission mission) => new MissionBehavior[4]
			{
				new ArenaDuelMissionBehavior(target),
				new AgentHumanAILogic(),
				new DuelPlayerDeathAgentStateDeciderLogic(),
				new DuelMainHeroDeathMissionBehavior()
			});
			Logger.Log("DuelBehavior", "[ArenaTeleport] MissionState.OpenNew 调用已返回，等待新 Mission 初始化。");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("DuelBehavior", "[ArenaTeleport][ERROR] 打开竞技场 Mission 失败: " + ex.ToString());
			return false;
		}
	}

	public static void GlobalArenaLeaveTick()
	{
		try
		{
			if (_arenaMissionActive && Mission.Current == null)
			{
				_arenaMissionActive = false;
				_arenaMissionLeaveRequested = false;
				Logger.Log("ArenaDuel", "[Cleanup] GlobalArenaLeaveTick 检测到 Mission 已结束，重置竞技场状态。");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ArenaDuel", "[ERROR] GlobalArenaLeaveTick: " + ex.ToString());
		}
	}

	public static void GlobalSourceMissionLeaveTick()
	{
		try
		{
			if (!_leaveSourceMissionRequested || (Mission.Current != null && Mission.Current.CurrentTime < _leaveSourceMissionReadyTime) || Mission.Current.Mode == MissionMode.Conversation || Campaign.Current?.ConversationManager?.OneToOneConversationAgent != null)
			{
				return;
			}
			Mission current = Mission.Current;
			if (current == null)
			{
				_leaveSourceMissionRequested = false;
				return;
			}
			string text = current.SceneName ?? string.Empty;
			if (!text.Equals("arena_vlandia_a", StringComparison.OrdinalIgnoreCase))
			{
				_leaveSourceMissionRequested = false;
				_leaveSourceMissionReadyTime = 0f;
				Logger.Log("ArenaDuel", "[Leave] GlobalSourceMissionLeaveTick 10秒等待结束，正在退出原始 Mission。");
				current.EndMission();
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ArenaDuel", "[ERROR] GlobalSourceMissionLeaveTick: " + ex.ToString());
		}
	}

	public static void GlobalPendingMainHeroDeathTick()
	{
		try
		{
			if (!_pendingMainHeroDeath || Mission.Current != null || Campaign.Current == null)
			{
				return;
			}
			bool flag = false;
			try
			{
				flag = Campaign.Current.ConversationManager?.OneToOneConversationAgent != null;
			}
			catch
			{
			}
			if (flag)
			{
				return;
			}
			bool flag2 = false;
			try
			{
				string text = (Game.Current?.GameStateManager?.ActiveState)?.GetType()?.FullName ?? string.Empty;
				if (!string.IsNullOrEmpty(text) && text.IndexOf("MapState", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					flag2 = true;
				}
			}
			catch
			{
			}
			if (flag2)
			{
				ApplyMainHeroDeathNow(_pendingMainHeroDeathKiller);
				_pendingMainHeroDeath = false;
				_pendingMainHeroDeathKiller = null;
				_pendingMainHeroDeathRequestUtcTicks = 0L;
			}
		}
		catch
		{
		}
	}

	public static void GlobalTownMenuTick()
	{
		try
		{
			if (!_openTownMenuRequested || Mission.Current != null || Campaign.Current == null)
			{
				return;
			}
			Settlement settlement = Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
			if (settlement == null)
			{
				_openTownMenuRequested = false;
				return;
			}
			string text = null;
			if (settlement.IsTown)
			{
				text = "town";
			}
			else if (settlement.IsCastle)
			{
				text = "castle";
			}
			else if (settlement.IsVillage)
			{
				text = "village";
			}
			if (string.IsNullOrEmpty(text))
			{
				_openTownMenuRequested = false;
				return;
			}
			_openTownMenuRequested = false;
			GameMenu.SwitchToMenu(text);
			Logger.Log("ArenaDuel", "[Leave] GlobalTownMenuTick 打开菜单: " + text + ", Settlement=" + settlement.StringId);
		}
		catch (Exception ex)
		{
			_openTownMenuRequested = false;
			Logger.Log("ArenaDuel", "[ERROR] GlobalTownMenuTick: " + ex.ToString());
		}
	}

	private void EndDuel(bool playerDefeated)
	{
		if (_duelResultRecorded)
		{
			return;
		}
		_duelResultRecorded = true;
		bool flag = !playerDefeated;
		if (_targetHero != null && !string.IsNullOrEmpty(_targetHero.StringId))
		{
			_lastDuelResults[_targetHero.StringId] = (flag ? 1 : (-1));
		}
		Agent agent = GetAgent(_targetHero);
		TryPostDuelAiShout(_targetHero, agent, flag);
		if (!_currentDuelIsArena)
		{
			try
			{
				RestoreState();
			}
			catch
			{
			}
		}
		FinishDuel();
		if (_currentDuelIsArena)
		{
			Agent main = Agent.Main;
			if (agent != null && main != null)
			{
				if (agent.Team != null && main.Team != null)
				{
					agent.Team.SetIsEnemyOf(main.Team, isEnemyOf: false);
					main.Team.SetIsEnemyOf(agent.Team, isEnemyOf: false);
				}
				agent.SetWatchState(Agent.WatchState.Patrolling);
				agent.ClearTargetFrame();
			}
		}
		string text = ApplyDuelStakeSettlementAndBuildResultText(_targetHero, flag);
		string text2 = (flag ? "【决斗结果】你赢了！" : "【决斗结果】你输了！");
		Color color = (flag ? Color.FromUint(4281257073u) : Color.FromUint(4293348412u));
		string text3 = (_currentDuelIsArena ? " 10秒后退出竞技场..." : "");
		InformationManager.DisplayMessage(new InformationMessage(text2 + text + text3, color));
	}

	private void RestoreState()
	{
		try
		{
			_meetingPreFightActive = false;
			_meetingPreFightEndTime = 0f;
			_duelPlayerTeam = null;
			_duelEnemyTeam = null;
		}
		catch
		{
		}
		Agent agent = GetAgent(_targetHero);
		if (agent != null)
		{
			if (_hasPreDuelTargetState && _preDuelTargetTeam != null)
			{
				agent.SetTeam(_preDuelTargetTeam, sync: true);
			}
			else if (Mission.Current != null)
			{
				agent.SetTeam(Mission.Current.PlayerTeam, sync: true);
			}
			agent.Health = agent.HealthLimit;
			TrySetAgentController(agent, "AI");
			try
			{
				agent.SetMortalityState(Agent.MortalityState.Mortal);
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
			UnlockAgentMovement(agent, unpauseAi: true, clearTargetFrame: true);
			try
			{
				Agent mountAgent = agent.MountAgent;
				if (mountAgent != null && mountAgent.IsActive())
				{
					TrySetAgentController(mountAgent, "AI");
					UnlockAgentMovement(mountAgent, unpauseAi: true, clearTargetFrame: true);
				}
			}
			catch
			{
			}
		}
		if (Agent.Main != null)
		{
			try
			{
				if (_hasPreDuelPlayerState && _preDuelPlayerTeam != null)
				{
					Agent.Main.SetTeam(_preDuelPlayerTeam, sync: true);
				}
			}
			catch
			{
			}
			TrySetAgentController(Agent.Main, "Player");
			UnlockAgentMovement(Agent.Main, unpauseAi: true, clearTargetFrame: true);
			try
			{
				Agent mountAgent2 = Agent.Main.MountAgent;
				if (mountAgent2 != null && mountAgent2.IsActive())
				{
					UnlockAgentMovement(mountAgent2, unpauseAi: true, clearTargetFrame: true);
				}
			}
			catch
			{
			}
			try
			{
				Agent.Main.SetMortalityState(Agent.MortalityState.Mortal);
			}
			catch
			{
			}
			Agent.Main.Health = Agent.Main.HealthLimit;
		}
		if (Mission.Current != null)
		{
			bool flag = false;
			try
			{
				flag = LordEncounterBehavior.IsEncounterMeetingMissionActive;
			}
			catch
			{
				flag = false;
			}
			try
			{
				Mission.Current.SetMissionMode(flag ? MissionMode.Battle : _preDuelMode, atStart: true);
			}
			catch
			{
			}
		}
		_hasPreDuelTargetState = false;
		_hasPreDuelPlayerState = false;
	}

	private void FinishDuel()
	{
		_isDuelActive = false;
		Logger.Log("DuelBehavior", "决斗流程彻底结束 (FinishDuel)。");
		InformationManager.DisplayMessage(new InformationMessage("决斗已结束。", Color.FromUint(4294155282u)));
	}

	private void EnsureDeathBehaviorsPresent()
	{
		try
		{
			Mission current = Mission.Current;
			if (current == null)
			{
				return;
			}
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			foreach (MissionBehavior missionBehavior in current.MissionBehaviors)
			{
				if (missionBehavior != null)
				{
					if (!flag && missionBehavior is DuelPlayerDeathAgentStateDeciderLogic)
					{
						flag = true;
					}
					if (!flag2 && missionBehavior is DuelMainHeroDeathMissionBehavior)
					{
						flag2 = true;
					}
					if (!flag3 && missionBehavior is DuelTargetDeathMissionBehavior)
					{
						flag3 = true;
					}
					if (flag && flag2 && flag3)
					{
						break;
					}
				}
			}
			if (!flag)
			{
				current.AddMissionBehavior(new DuelPlayerDeathAgentStateDeciderLogic());
			}
			if (!flag2)
			{
				current.AddMissionBehavior(new DuelMainHeroDeathMissionBehavior());
			}
			if (!flag3)
			{
				current.AddMissionBehavior(new DuelTargetDeathMissionBehavior());
			}
		}
		catch
		{
		}
	}

	private static void ForceKillMainHero(Hero killerHero)
	{
		if (_forcedMainHeroDeath)
		{
			return;
		}
		_forcedMainHeroDeath = true;
		try
		{
			if (Mission.Current != null)
			{
				_pendingMainHeroDeath = true;
				_pendingMainHeroDeathKiller = killerHero;
				_pendingMainHeroDeathRequestUtcTicks = DateTime.UtcNow.Ticks;
				if (IsArenaMissionActive)
				{
					_arenaMissionLeaveRequested = true;
					try
					{
						_arenaMissionLeaveReadyTime = Mission.Current.CurrentTime + 2f;
						return;
					}
					catch
					{
						_arenaMissionLeaveReadyTime = 0f;
						return;
					}
				}
				_leaveSourceMissionRequested = true;
				float num = 0f;
				try
				{
					num = Mission.Current.CurrentTime;
				}
				catch
				{
				}
				_leaveSourceMissionReadyTime = num + 2f;
				return;
			}
		}
		catch
		{
		}
		ApplyMainHeroDeathNow(killerHero);
	}

	private static void ApplyMainHeroDeathNow(Hero killerHero)
	{
		try
		{
			Hero mainHero = Hero.MainHero;
			if (mainHero != null)
			{
				KillCharacterAction.ApplyByBattle(mainHero, killerHero);
			}
		}
		catch
		{
		}
	}

	private static void ForceKillAgentVisual(Agent victim, Agent killer)
	{
		try
		{
			if (victim != null && victim.State != AgentState.Killed)
			{
				Agent agent = killer ?? Mission.Current?.MainAgent ?? victim;
				if (agent != null && agent.Monster != null && victim.Monster != null)
				{
					Blow blow = new Blow(agent.Index);
					blow.DamageType = DamageTypes.Blunt;
					blow.BoneIndex = victim.Monster.HeadLookDirectionBoneIndex;
					blow.GlobalPosition = victim.Position;
					blow.GlobalPosition.z = blow.GlobalPosition.z + victim.GetEyeGlobalHeight();
					blow.BaseMagnitude = 2000f;
					blow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
					blow.InflictedDamage = 2000;
					blow.SwingDirection = victim.LookDirection;
					blow.Direction = blow.SwingDirection;
					blow.DamageCalculated = true;
					sbyte mainHandItemBoneIndex = agent.Monster.MainHandItemBoneIndex;
					AttackCollisionData collisionData = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(_attackBlockedWithShield: false, _correctSideShieldBlock: false, _isAlternativeAttack: false, _isColliderAgent: true, _collidedWithShieldOnBack: false, _isMissile: false, _isMissileBlockedWithWeapon: false, _missileHasPhysics: false, _entityExists: false, _thrustTipHit: false, _missileGoneUnderWater: false, _missileGoneOutOfBorder: false, CombatCollisionResult.StrikeAgent, -1, 0, 2, blow.BoneIndex, BoneBodyPartType.Head, mainHandItemBoneIndex, Agent.UsageDirection.AttackLeft, -1, CombatHitResultFlags.NormalHit, 0.5f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, Vec3.Up, blow.Direction, blow.GlobalPosition, Vec3.Zero, Vec3.Zero, victim.Velocity, Vec3.Up);
					victim.RegisterBlow(blow, in collisionData);
				}
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
			if (agent == null)
			{
				return;
			}
			PropertyInfo propertyInfo = agent.GetType().GetProperty("Controller") ?? agent.GetType().GetProperty("ControllerType");
			if (!(propertyInfo != null) || !propertyInfo.CanWrite)
			{
				return;
			}
			Type propertyType = propertyInfo.PropertyType;
			object obj = null;
			try
			{
				obj = Enum.Parse(propertyType, controllerType, ignoreCase: true);
			}
			catch
			{
			}
			if (obj == null)
			{
				string[] names = Enum.GetNames(propertyType);
				foreach (string text in names)
				{
					if (text.Equals(controllerType, StringComparison.OrdinalIgnoreCase))
					{
						obj = Enum.Parse(propertyType, text, ignoreCase: true);
						break;
					}
					if (controllerType.Equals("AI", StringComparison.OrdinalIgnoreCase) && text.IndexOf("AI", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						obj = Enum.Parse(propertyType, text, ignoreCase: true);
						break;
					}
					if (controllerType.Equals("None", StringComparison.OrdinalIgnoreCase) && text.IndexOf("None", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						obj = Enum.Parse(propertyType, text, ignoreCase: true);
						break;
					}
				}
			}
			if (obj != null)
			{
				propertyInfo.SetValue(agent, obj);
			}
		}
		catch
		{
		}
	}

	private Agent GetAgent(Hero hero)
	{
		return Mission.Current?.Agents.FirstOrDefault((Agent a) => a.Character == hero.CharacterObject);
	}
}
