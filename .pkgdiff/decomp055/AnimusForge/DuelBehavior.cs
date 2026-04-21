using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace AnimusForge;

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

		public override MissionBehaviorType BehaviorType => (MissionBehaviorType)1;

		public ArenaDuelMissionBehavior(Hero target)
		{
			_targetHero = target;
		}

		public override void AfterStart()
		{
			((MissionBehavior)this).AfterStart();
			try
			{
				if (((MissionBehavior)this).Mission != null && !_setupDone)
				{
					((MissionBehavior)this).Mission.SetMissionMode((MissionMode)2, true);
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
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Invalid comparison between Unknown and I4
			try
			{
				if (!_localDuelResultRecorded && (int)agentState != 1)
				{
					Hero val = null;
					try
					{
						BasicCharacterObject obj = ((affectedAgent != null) ? affectedAgent.Character : null);
						CharacterObject val2 = (CharacterObject)(object)((obj is CharacterObject) ? obj : null);
						val = ((val2 != null) ? val2.HeroObject : null);
					}
					catch
					{
					}
					if (val != null && val == _targetHero)
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
			//IL_0477: Unknown result type (might be due to invalid IL or missing references)
			//IL_047c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0486: Expected O, but got Unknown
			//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01df: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0234: Unknown result type (might be due to invalid IL or missing references)
			//IL_0243: Unknown result type (might be due to invalid IL or missing references)
			//IL_0265: Unknown result type (might be due to invalid IL or missing references)
			//IL_0274: Unknown result type (might be due to invalid IL or missing references)
			//IL_0281: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
			if (((MissionBehavior)this).Mission == null)
			{
				return;
			}
			if (((MissionBehavior)this).Mission.Agents != null && ((List<Agent>)(object)((MissionBehavior)this).Mission.Agents).Count > 0 && FindAgentForHero(_targetHero) != null)
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
				uint num = ((mainHero.MapFaction != null) ? mainHero.MapFaction.Color : 4278190335u);
				uint num2 = ((mainHero.MapFaction != null) ? mainHero.MapFaction.Color2 : 4278190208u);
				Banner val = ((mainHero.Clan != null) ? mainHero.Clan.Banner : null);
				uint num3 = ((_targetHero.MapFaction != null) ? _targetHero.MapFaction.Color : 4294901760u);
				uint num4 = ((_targetHero.MapFaction != null) ? _targetHero.MapFaction.Color2 : 4286578688u);
				Banner val2 = ((_targetHero.Clan != null) ? _targetHero.Clan.Banner : null);
				Team val3 = ((MissionBehavior)this).Mission.Teams.Add((BattleSideEnum)1, num, num2, val, true, false, true);
				Team val4 = ((MissionBehavior)this).Mission.Teams.Add((BattleSideEnum)0, num3, num4, val2, false, true, true);
				((MissionBehavior)this).Mission.PlayerTeam = val3;
				Vec3 val5 = default(Vec3);
				((Vec3)(ref val5))._002Ector(156f, 113f, 0f, -1f);
				Vec3 val6 = val5 + new Vec3(-4f, 0f, 0f, -1f);
				Vec3 val7 = val5 + new Vec3(4f, 0f, 0f, -1f);
				Vec2 val8 = default(Vec2);
				((Vec2)(ref val8))._002Ector(1f, 0f);
				Vec2 val9 = default(Vec2);
				((Vec2)(ref val9))._002Ector(-1f, 0f);
				CharacterObject characterObject = mainHero.CharacterObject;
				CharacterObject characterObject2 = _targetHero.CharacterObject;
				Equipment val10 = mainHero.BattleEquipment.Clone(false);
				val10[(EquipmentIndex)10] = EquipmentElement.Invalid;
				val10[(EquipmentIndex)11] = EquipmentElement.Invalid;
				Equipment val11 = _targetHero.BattleEquipment.Clone(false);
				val11[(EquipmentIndex)10] = EquipmentElement.Invalid;
				val11[(EquipmentIndex)11] = EquipmentElement.Invalid;
				AgentBuildData val12 = new AgentBuildData((BasicCharacterObject)(object)characterObject).Team(val3).Equipment(val10).InitialPosition(ref val6)
					.InitialDirection(ref val8);
				AgentBuildData val13 = new AgentBuildData((BasicCharacterObject)(object)characterObject2).Team(val4).Equipment(val11).InitialPosition(ref val7)
					.InitialDirection(ref val9);
				Agent val14 = ((MissionBehavior)this).Mission.SpawnAgent(val12, false);
				if (val14 == null)
				{
					_localAgentsSpawned = false;
					Logger.Log("ArenaDuel", "[Spawn][ERROR] 玩家 Agent 生成失败，将在后续 Tick 重试。");
					return;
				}
				((MissionBehavior)this).Mission.MainAgent = val14;
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
							propertyInfo.SetValue(val14, obj);
						}
					}
				}
				catch
				{
				}
				Agent val15 = ((MissionBehavior)this).Mission.SpawnAgent(val13, false);
				if (val15 == null)
				{
					_localAgentsSpawned = false;
					Logger.Log("ArenaDuel", "[Spawn][ERROR] 敌方 Agent 生成失败，将在后续 Tick 重试。");
					return;
				}
				_localAgentsSpawned = true;
				_hadEnemyAgentEver = true;
				val15.SetTeam(val4, true);
				val4.SetIsEnemyOf(val3, true);
				val3.SetIsEnemyOf(val4, true);
				SetAgentController(val15, "None");
				val14.SetMortalityState((MortalityState)1);
				val15.SetMortalityState((MortalityState)1);
				_localPreFightActive = true;
				float num5 = ((Mission.Current != null) ? Mission.Current.CurrentTime : 0f);
				_localPreFightTimer = num5 + 5f;
				_localPostDuelFreezeActive = false;
				_localDuelResultRecorded = false;
				val15.SetWatchState((WatchState)2);
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
				PropertyInfo propertyInfo = ((object)agent).GetType().GetProperty("Controller") ?? ((object)agent).GetType().GetProperty("ControllerType");
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
			if (hero == null || ((MissionBehavior)this).Mission == null || ((MissionBehavior)this).Mission.Agents == null)
			{
				return null;
			}
			foreach (Agent item in (List<Agent>)(object)((MissionBehavior)this).Mission.Agents)
			{
				BasicCharacterObject character = item.Character;
				CharacterObject val = (CharacterObject)(object)((character is CharacterObject) ? character : null);
				if (val != null && val.HeroObject == hero)
				{
					_hadEnemyAgentEver = true;
					return item;
				}
			}
			return null;
		}

		public override void OnMissionTick(float dt)
		{
			//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f5: Expected O, but got Unknown
			//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f7: Expected O, but got Unknown
			((MissionBehavior)this).OnMissionTick(dt);
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
				float currentTime = ((MissionBehavior)this).Mission.CurrentTime;
				if (currentTime >= _localPreFightTimer)
				{
					_localPreFightActive = false;
					Agent val = FindAgentForHero(_targetHero);
					if (val != null)
					{
						SetAgentController(val, "AI");
						val.SetWatchState((WatchState)2);
					}
					Mission mission = ((MissionBehavior)this).Mission;
					Agent obj = ((mission != null) ? mission.MainAgent : null) ?? Agent.Main;
					if (obj != null)
					{
						obj.SetMortalityState((MortalityState)0);
					}
					if (val != null)
					{
						val.SetMortalityState((MortalityState)0);
					}
					InformationManager.DisplayMessage(new InformationMessage("决斗开始！", Color.FromUint(4294901760u)));
				}
			}
			if (_localPostDuelFreezeActive)
			{
				Agent val2 = FindAgentForHero(_targetHero);
				if (val2 != null)
				{
					val2.SetMovementDirection(ref Vec2.Zero);
					val2.ClearTargetFrame();
				}
				float currentTime2 = ((MissionBehavior)this).Mission.CurrentTime;
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
			if (Input.IsKeyPressed((InputKey)15))
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
					Mission mission2 = ((MissionBehavior)this).Mission;
					int valueOrDefault = ((mission2 == null) ? ((int?)null) : ((List<Agent>)(object)mission2.Agents)?.Count).GetValueOrDefault();
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
					Mission obj2 = Mission.Current ?? ((MissionBehavior)this).Mission;
					num = ((obj2 != null) ? obj2.CurrentTime : 0f);
				}
				catch
				{
				}
				if (!(_arenaMissionLeaveReadyTime > 0f) || !(num < _arenaMissionLeaveReadyTime))
				{
					Logger.Log("ArenaDuel", "[Leave] 决斗结束，ArenaDuelMissionBehavior 收到离场请求，正在执行 EndMission...");
					Mission val3 = Mission.Current ?? ((MissionBehavior)this).Mission;
					if (val3 != null && !val3.IsMissionEnding)
					{
						_arenaMissionLeaveRequested = false;
						_arenaMissionLeaveReadyTime = 0f;
						val3.EndMission();
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
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Invalid comparison between Unknown and I4
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Invalid comparison between Unknown and I4
			//IL_023f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0124: Unknown result type (might be due to invalid IL or missing references)
			//IL_012a: Invalid comparison between Unknown and I4
			//IL_012d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0133: Invalid comparison between Unknown and I4
			//IL_015a: Unknown result type (might be due to invalid IL or missing references)
			if (!_setupDone)
			{
				return;
			}
			Mission mission = ((MissionBehavior)this).Mission;
			Agent val = ((mission != null) ? mission.MainAgent : null) ?? Agent.Main;
			Agent val2 = FindAgentForHero(_targetHero);
			if (val == null)
			{
				return;
			}
			AgentState state = val.State;
			AgentState val3 = state;
			AgentState val4 = val3;
			if ((int)val4 != 3)
			{
				if ((int)val4 != 4 && val.IsActive() && !(val.Health <= 0f))
				{
					if (val2 == null)
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
						return;
					}
					if (!val2.IsActive() || (int)val2.State == 4 || (int)val2.State == 3 || val2.Health <= 0f)
					{
						Logger.Log("ArenaDuel", $"判定: 玩家获胜 (State={val2.State}, Active={val2.IsActive()}, HP={val2.Health:0.0})");
						EndDuelLocal(playerDefeated: false);
						return;
					}
					float healthThreshold = DuelSettings.GetHealthThreshold();
					float num = val.Health / val.HealthLimit;
					float num2 = val2.Health / val2.HealthLimit;
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
				}
				else
				{
					Logger.Log("ArenaDuel", $"判定: 玩家战败 (State={state})");
					EndDuelLocal(playerDefeated: true);
				}
			}
			else
			{
				try
				{
					val.SetMortalityState((MortalityState)0);
				}
				catch
				{
				}
				ForceKillAgentVisual(val, val2);
				ForceKillMainHero(_targetHero);
				Logger.Log("ArenaDuel", "判定: 玩家战败 (Unconscious->Death)");
				EndDuelLocal(playerDefeated: true);
			}
		}

		private void EndDuelLocal(bool playerDefeated)
		{
			//IL_010f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0103: Unknown result type (might be due to invalid IL or missing references)
			//IL_0114: Unknown result type (might be due to invalid IL or missing references)
			//IL_0124: Unknown result type (might be due to invalid IL or missing references)
			//IL_0126: Unknown result type (might be due to invalid IL or missing references)
			//IL_0130: Expected O, but got Unknown
			if (!_localDuelResultRecorded)
			{
				_localDuelResultRecorded = true;
				bool flag = !playerDefeated;
				if (Instance != null)
				{
					Instance._lastDuelResults[((MBObjectBase)_targetHero).StringId] = (flag ? 1 : (-1));
				}
				_localPostDuelFreezeActive = true;
				float currentTime = ((MissionBehavior)this).Mission.CurrentTime;
				_localPostDuelExitTimer = currentTime + 10f;
				Agent val = FindAgentForHero(_targetHero);
				TryPostDuelAiShout(_targetHero, val, flag);
				if (val != null && val.IsActive())
				{
					SetAgentController(val, "None");
					val.SetMortalityState((MortalityState)1);
				}
				if (Agent.Main != null && Agent.Main.IsActive())
				{
					Agent.Main.SetMortalityState((MortalityState)1);
				}
				string text = ApplyDuelStakeSettlementAndBuildResultText(_targetHero, flag);
				string text2 = (flag ? "【决斗结果】你赢了！" : "【决斗结果】你输了！");
				Color val2 = (flag ? Color.FromUint(4281257073u) : Color.FromUint(4293348412u));
				InformationManager.DisplayMessage(new InformationMessage(text2 + text + " 10秒后退出竞技场...", val2));
			}
		}
	}

	private sealed class DuelPlayerDeathAgentStateDeciderLogic : MissionLogic, IAgentStateDecider, IMissionBehavior
	{
		public AgentState GetAgentState(Agent effectedAgent, float deathProbability, out bool usedSurgery)
		{
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
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
						BasicCharacterObject character = effectedAgent.Character;
						CharacterObject val = (CharacterObject)(object)((character is CharacterObject) ? character : null);
						Hero val2 = ((val != null) ? val.HeroObject : null);
						flag2 = val2 != null && instance._targetHero != null && val2 == instance._targetHero;
					}
					catch
					{
						flag2 = false;
					}
					if (flag || flag2)
					{
						return (AgentState)3;
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
			return (AgentState)((MBRandom.RandomFloat <= num) ? 4 : 3);
		}
	}

	private sealed class DuelMainHeroDeathMissionBehavior : MissionBehavior
	{
		public override MissionBehaviorType BehaviorType => (MissionBehaviorType)1;

		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
		{
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Invalid comparison between Unknown and I4
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Invalid comparison between Unknown and I4
			try
			{
				DuelBehavior instance = Instance;
				if (instance != null && instance._isDuelActive && affectedAgent != null && affectedAgent.IsMainAgent && ((int)agentState == 3 || (int)agentState == 4))
				{
					Hero val = null;
					try
					{
						BasicCharacterObject obj = ((affectorAgent != null) ? affectorAgent.Character : null);
						CharacterObject val2 = (CharacterObject)(object)((obj is CharacterObject) ? obj : null);
						val = ((val2 != null) ? val2.HeroObject : null);
					}
					catch
					{
					}
					ForceKillMainHero(instance._targetHero ?? val);
				}
			}
			catch
			{
			}
		}
	}

	private sealed class DuelTargetDeathMissionBehavior : MissionBehavior
	{
		public override MissionBehaviorType BehaviorType => (MissionBehaviorType)1;

		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Invalid comparison between Unknown and I4
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Invalid comparison between Unknown and I4
			try
			{
				DuelBehavior instance = Instance;
				if (instance != null && instance._isDuelActive && !_duelResultRecorded && instance._targetHero != null && ((int)agentState == 3 || (int)agentState == 4))
				{
					Hero val = null;
					try
					{
						BasicCharacterObject obj = ((affectedAgent != null) ? affectedAgent.Character : null);
						CharacterObject val2 = (CharacterObject)(object)((obj is CharacterObject) ? obj : null);
						val = ((val2 != null) ? val2.HeroObject : null);
					}
					catch
					{
					}
					if (val != null && val == instance._targetHero)
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

	private MissionMode _preDuelMode = (MissionMode)2;

	private Team _preDuelTargetTeam;

	private Team _preDuelPlayerTeam;

	private Team _preDuelPlayerMountTeam;

	private Team _preDuelTargetMountTeam;

	private bool _currentDuelIsArena;

	private bool _meetingPreFightActive;

	private float _meetingPreFightEndTime;

	private bool _meetingPendingStart;

	private float _formalDuelSpectatorRefreshTimer;

	private readonly HashSet<int> _formalDuelSpectatorAgentIndices = new HashSet<int>();

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
			dataStore.SyncData<Dictionary<string, float>>("_duelCooldowns", ref _duelCooldowns);
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
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Expected O, but got Unknown
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Expected O, but got Unknown
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
		string text = $"[系统] 双方约定 {delaySeconds:F0} 秒后开始决斗！";
		InformationManager.DisplayMessage(new InformationMessage(text, Color.FromUint(4294901760u)));
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
		string stringId = ((MBObjectBase)hero).StringId;
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
			string stringId = ((MBObjectBase)hero).StringId;
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
			string stringId = ((MBObjectBase)hero).StringId;
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
			if (targetHero == null || string.IsNullOrEmpty(((MBObjectBase)targetHero).StringId))
			{
				return "";
			}
			if (!TryConsumePendingDuelStake(((MBObjectBase)targetHero).StringId, out var stake) || stake == null)
			{
				return "";
			}
			string text = ((targetHero == null) ? null : ((object)targetHero.Name)?.ToString()) ?? "NPC";
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
			string stringId = ((MBObjectBase)hero).StringId;
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
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Expected O, but got Unknown
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
					Mission current = Mission.Current;
					List<MissionBehavior> list = ((current != null) ? current.MissionBehaviors : null);
					if (list != null)
					{
						foreach (MissionBehavior item in list)
						{
							if (item != null)
							{
								string text2 = ((object)item).GetType().FullName ?? "";
								if (text2 == "AnimusForge.ShoutBehavior+ShoutMissionBehavior")
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
				if (targetAgent != null && targetAgent.IsActive() && flag2)
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
				string text3 = ((targetHero == null) ? null : ((object)targetHero.Name)?.ToString()) ?? "NPC";
				try
				{
					InformationManager.DisplayMessage(new InformationMessage("[" + text3 + "] " + text, new Color(1f, 0.8f, 0.2f, 1f)));
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
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Expected O, but got Unknown
		if (Mission.Current == null || target == null)
		{
			return;
		}
		ShowDuelRiskWarning();
		bool flag = false;
		try
		{
			Campaign current = Campaign.Current;
			int num;
			if (current != null)
			{
				ConversationManager conversationManager = current.ConversationManager;
				num = ((conversationManager != null && conversationManager.IsConversationInProgress) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			flag = (byte)num != 0;
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
				Agent main = Agent.Main;
				if (main != null)
				{
					main.SetMortalityState((MortalityState)1);
				}
			}
			catch
			{
			}
			Agent agent = GetAgent(target);
			if (agent != null)
			{
				try
				{
					agent.SetMortalityState((MortalityState)1);
				}
				catch
				{
				}
				TrySetAgentController(agent, "None");
				try
				{
					agent.SetIsAIPaused(true);
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
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Expected O, but got Unknown
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Invalid comparison between Unknown and I4
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Invalid comparison between Unknown and I4
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Invalid comparison between Unknown and I4
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Invalid comparison between Unknown and I4
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
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
				int num;
				if (current != null)
				{
					ConversationManager conversationManager = current.ConversationManager;
					num = ((conversationManager != null && conversationManager.IsConversationInProgress) ? 1 : 0);
				}
				else
				{
					num = 0;
				}
				flag = (byte)num != 0;
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
					main.SetMortalityState((MortalityState)0);
				}
				catch
				{
				}
				try
				{
					agent.SetMortalityState((MortalityState)0);
				}
				catch
				{
				}
				RefreshMeetingDuelParticipantLocks(main, agent, preFight: false);
				float healthThreshold = DuelSettings.GetHealthThreshold();
				Mission current2 = Mission.Current;
				string arg = ((current2 != null) ? current2.SceneName : null) ?? "Unknown";
				string text = $"【决斗开始】当前场景: {arg}。规则：任一方生命值低于 {healthThreshold:P0} 判定为战败。";
				InformationManager.DisplayMessage(new InformationMessage(text, Color.FromUint(4294901760u)));
			}
			if ((int)main.State == 3)
			{
				try
				{
					main.SetMortalityState((MortalityState)0);
				}
				catch
				{
				}
				ForceKillAgentVisual(main, agent);
				ForceKillMainHero(_targetHero);
				Logger.Log("DuelBehavior", "判定: 玩家战败 (Unconscious->Death)");
				EndDuel(playerDefeated: true);
				return;
			}
			if (!main.IsActive() || (int)main.State == 4)
			{
				Logger.Log("DuelBehavior", $"判定: 玩家战败 (State={main.State})");
				EndDuel(playerDefeated: true);
				return;
			}
			if (!agent.IsActive() || (int)agent.State == 4 || (int)agent.State == 3 || agent.Health <= 0f)
			{
				Logger.Log("DuelBehavior", $"判定: 玩家获胜 (State={agent.State}, Active={agent.IsActive()}, HP={agent.Health:0.0})");
				EndDuel(playerDefeated: false);
				return;
			}
			float healthThreshold2 = DuelSettings.GetHealthThreshold();
			float num2 = main.Health / main.HealthLimit;
			float num3 = agent.Health / agent.HealthLimit;
			if (num2 <= healthThreshold2)
			{
				Logger.Log("DuelBehavior", $"判定: 玩家战败 (HP {num2:P0} <= {healthThreshold2:P0})");
				EndDuel(playerDefeated: true);
			}
			else if (num3 <= healthThreshold2)
			{
				Logger.Log("DuelBehavior", $"判定: 玩家获胜 (HP {num3:P0} <= {healthThreshold2:P0})");
				EndDuel(playerDefeated: false);
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
			agent.SetIsAIPaused(false);
		}
		catch
		{
		}
	}

	private void RefreshMeetingDuelParticipantLocks(Agent playerAgent, Agent targetAgent, bool preFight)
	{
		UnlockAgentMovement(playerAgent, unpauseAi: true, clearTargetFrame: true);
		Agent val = null;
		try
		{
			val = ((playerAgent != null) ? playerAgent.MountAgent : null);
		}
		catch
		{
			val = null;
		}
		if (val != null && val.IsActive())
		{
			try
			{
				val.SetIsAIPaused(false);
			}
			catch
			{
			}
			UnlockAgentMovement(val, unpauseAi: true, clearTargetFrame: true);
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
					targetAgent.SetIsAIPaused(true);
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
					targetAgent.SetIsAIPaused(false);
				}
			}
			catch
			{
			}
		}
		UnlockAgentMovement(targetAgent, !preFight, clearTargetFrame: true);
		Agent val2 = null;
		try
		{
			val2 = targetAgent.MountAgent;
		}
		catch
		{
			val2 = null;
		}
		if (val2 != null && val2.IsActive())
		{
			TrySetAgentController(val2, preFight ? "None" : "AI");
			try
			{
				val2.SetIsAIPaused(preFight);
			}
			catch
			{
			}
			UnlockAgentMovement(val2, !preFight, clearTargetFrame: true);
		}
		if (preFight)
		{
			return;
		}
		try
		{
			targetAgent.WieldInitialWeapons((WeaponWieldActionType)2, (InitialWeaponEquipPreference)1);
		}
		catch
		{
		}
		try
		{
			targetAgent.SetWatchState((WatchState)2);
		}
		catch
		{
		}
	}

	private void KeepFormalDuelTargetFocusedOnPlayer(Agent targetAgent)
	{
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Agent main = Agent.Main;
			if (main == null || !main.IsActive() || targetAgent == null || !targetAgent.IsActive())
			{
				return;
			}
			TrySetAgentController(targetAgent, "AI");
			try
			{
				if (targetAgent.IsAIControlled)
				{
					targetAgent.SetIsAIPaused(false);
				}
			}
			catch
			{
			}
			try
			{
				targetAgent.ResetEnemyCaches();
				targetAgent.InvalidateTargetAgent();
				targetAgent.InvalidateAIWeaponSelections();
			}
			catch
			{
			}
			try
			{
				targetAgent.ClearTargetFrame();
			}
			catch
			{
			}
			Vec3 position;
			try
			{
				position = main.Position;
				targetAgent.SetTargetPosition(((Vec3)(ref position)).AsVec2);
			}
			catch
			{
			}
			try
			{
				targetAgent.WieldInitialWeapons((WeaponWieldActionType)2, (InitialWeaponEquipPreference)1);
			}
			catch
			{
			}
			try
			{
				targetAgent.SetWatchState((WatchState)2);
			}
			catch
			{
			}
			try
			{
				Agent mountAgent = targetAgent.MountAgent;
				if (mountAgent != null && mountAgent.IsActive())
				{
					mountAgent.ResetEnemyCaches();
					mountAgent.InvalidateTargetAgent();
					mountAgent.ClearTargetFrame();
					position = main.Position;
					mountAgent.SetTargetPosition(((Vec3)(ref position)).AsVec2);
				}
			}
			catch
			{
			}
		}
		catch
		{
		}
	}

	private void KeepFormalDuelSpectatorsOutOfFight(Agent playerAgent, Agent targetAgent)
	{
		try
		{
			if (_currentDuelIsArena || Mission.Current == null || playerAgent == null || targetAgent == null || Mission.Current.CurrentTime < _formalDuelSpectatorRefreshTimer)
			{
				return;
			}
			_formalDuelSpectatorRefreshTimer = Mission.Current.CurrentTime + 0.2f;
			Team team = playerAgent.Team;
			Team team2 = targetAgent.Team;
			Agent mountAgent = playerAgent.MountAgent;
			Agent mountAgent2 = targetAgent.MountAgent;
			foreach (Agent item in (List<Agent>)(object)Mission.Current.Agents)
			{
				if (item == null || !item.IsActive() || item == playerAgent || item == targetAgent || item == mountAgent || item == mountAgent2 || (item.Team != team && item.Team != team2))
				{
					continue;
				}
				_formalDuelSpectatorAgentIndices.Add(item.Index);
				TrySetAgentController(item, "None");
				try
				{
					if (item.IsAIControlled)
					{
						item.SetIsAIPaused(true);
					}
				}
				catch
				{
				}
				try
				{
					item.ResetEnemyCaches();
					item.InvalidateTargetAgent();
					item.InvalidateAIWeaponSelections();
				}
				catch
				{
				}
				try
				{
					item.ClearTargetFrame();
				}
				catch
				{
				}
				try
				{
					item.SetWatchState((WatchState)0);
				}
				catch
				{
				}
				UnlockAgentMovement(item, unpauseAi: false, clearTargetFrame: true);
				TrySheathWeapons(item);
				try
				{
					Agent mountAgent3 = item.MountAgent;
					if (mountAgent3 != null && mountAgent3.IsActive())
					{
						TrySetAgentController(mountAgent3, "None");
						mountAgent3.SetIsAIPaused(true);
						mountAgent3.ClearTargetFrame();
						UnlockAgentMovement(mountAgent3, unpauseAi: false, clearTargetFrame: true);
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}

	private void RestoreFormalDuelSpectatorControl()
	{
		try
		{
			if (_formalDuelSpectatorAgentIndices.Count == 0 || Mission.Current == null)
			{
				_formalDuelSpectatorRefreshTimer = 0f;
				return;
			}
			foreach (int formalDuelSpectatorAgentIndex in _formalDuelSpectatorAgentIndices.ToList())
			{
				Agent val = ((IEnumerable<Agent>)Mission.Current.Agents).FirstOrDefault((Agent a) => a != null && a.Index == formalDuelSpectatorAgentIndex);
				if (val == null || !val.IsActive())
				{
					continue;
				}
				TrySetAgentController(val, "AI");
				try
				{
					if (val.IsAIControlled)
					{
						val.SetIsAIPaused(false);
					}
				}
				catch
				{
				}
				try
				{
					val.SetWatchState((WatchState)0);
				}
				catch
				{
				}
				UnlockAgentMovement(val, unpauseAi: true, clearTargetFrame: true);
				try
				{
					Agent mountAgent = val.MountAgent;
					if (mountAgent != null && mountAgent.IsActive())
					{
						TrySetAgentController(mountAgent, "AI");
						mountAgent.SetIsAIPaused(false);
						UnlockAgentMovement(mountAgent, unpauseAi: true, clearTargetFrame: true);
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		finally
		{
			_formalDuelSpectatorAgentIndices.Clear();
			_formalDuelSpectatorRefreshTimer = 0f;
		}
	}

	private static void TrySheathWeapons(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		try
		{
			agent.TryToSheathWeaponInHand((HandIndex)0, (WeaponWieldActionType)1);
		}
		catch
		{
		}
		try
		{
			agent.TryToSheathWeaponInHand((HandIndex)1, (WeaponWieldActionType)1);
		}
		catch
		{
		}
	}

	private bool TryInitializeFormalDuelTeams(Mission current, Agent targetAgent)
	{
		if (current == null || Agent.Main == null || targetAgent == null)
		{
			return false;
		}
		if (_currentDuelIsArena)
		{
			_duelPlayerTeam = Agent.Main.Team ?? current.PlayerTeam;
			_duelEnemyTeam = targetAgent.Team;
			return _duelPlayerTeam != null && _duelEnemyTeam != null && _duelPlayerTeam != _duelEnemyTeam;
		}
		Hero mainHero = Hero.MainHero;
		uint? obj;
		if (mainHero == null)
		{
			obj = null;
		}
		else
		{
			IFaction mapFaction = mainHero.MapFaction;
			obj = ((mapFaction != null) ? new uint?(mapFaction.Color) : ((uint?)null));
		}
		uint num = (uint)(((int?)obj) ?? (-16776961));
		Hero mainHero2 = Hero.MainHero;
		uint? obj2;
		if (mainHero2 == null)
		{
			obj2 = null;
		}
		else
		{
			IFaction mapFaction2 = mainHero2.MapFaction;
			obj2 = ((mapFaction2 != null) ? new uint?(mapFaction2.Color2) : ((uint?)null));
		}
		uint num2 = (uint)(((int?)obj2) ?? (-16777088));
		Hero mainHero3 = Hero.MainHero;
		object obj3;
		if (mainHero3 == null)
		{
			obj3 = null;
		}
		else
		{
			Clan clan = mainHero3.Clan;
			obj3 = ((clan != null) ? clan.Banner : null);
		}
		Banner val = (Banner)obj3;
		Hero targetHero = _targetHero;
		uint? obj4;
		if (targetHero == null)
		{
			obj4 = null;
		}
		else
		{
			IFaction mapFaction3 = targetHero.MapFaction;
			obj4 = ((mapFaction3 != null) ? new uint?(mapFaction3.Color) : ((uint?)null));
		}
		uint num3 = (uint)(((int?)obj4) ?? (-65536));
		Hero targetHero2 = _targetHero;
		uint? obj5;
		if (targetHero2 == null)
		{
			obj5 = null;
		}
		else
		{
			IFaction mapFaction4 = targetHero2.MapFaction;
			obj5 = ((mapFaction4 != null) ? new uint?(mapFaction4.Color2) : ((uint?)null));
		}
		uint num4 = (uint)(((int?)obj5) ?? (-8388608));
		Hero targetHero3 = _targetHero;
		object obj6;
		if (targetHero3 == null)
		{
			obj6 = null;
		}
		else
		{
			Clan clan2 = targetHero3.Clan;
			obj6 = ((clan2 != null) ? clan2.Banner : null);
		}
		Banner val2 = (Banner)obj6;
		try
		{
			_duelPlayerTeam = current.Teams.Add((BattleSideEnum)1, num, num2, val, true, false, true);
			_duelEnemyTeam = current.Teams.Add((BattleSideEnum)0, num3, num4, val2, false, true, true);
		}
		catch (Exception ex)
		{
			Logger.Log("DuelBehavior", "[FormalDuel][ERROR] 创建临时决斗队伍失败: " + ex.Message);
			_duelPlayerTeam = null;
			_duelEnemyTeam = null;
			return false;
		}
		if (_duelPlayerTeam == null || _duelEnemyTeam == null || _duelPlayerTeam == _duelEnemyTeam)
		{
			Logger.Log("DuelBehavior", "[FormalDuel][ERROR] 临时决斗队伍无效。");
			return false;
		}
		try
		{
			current.PlayerTeam = _duelPlayerTeam;
		}
		catch
		{
		}
		try
		{
			Agent.Main.SetTeam(_duelPlayerTeam, true);
		}
		catch
		{
		}
		try
		{
			Agent mountAgent = Agent.Main.MountAgent;
			if (mountAgent != null && mountAgent.IsActive())
			{
				mountAgent.SetTeam(_duelPlayerTeam, true);
			}
		}
		catch
		{
		}
		try
		{
			targetAgent.SetTeam(_duelEnemyTeam, true);
		}
		catch
		{
		}
		try
		{
			Agent mountAgent2 = targetAgent.MountAgent;
			if (mountAgent2 != null && mountAgent2.IsActive())
			{
				mountAgent2.SetTeam(_duelEnemyTeam, true);
			}
		}
		catch
		{
		}
		try
		{
			foreach (Team item in (List<Team>)(object)current.Teams)
			{
				if (item != null && item != _duelPlayerTeam && item != _duelEnemyTeam)
				{
					try
					{
						item.SetIsEnemyOf(_duelPlayerTeam, false);
					}
					catch
					{
					}
					try
					{
						_duelPlayerTeam.SetIsEnemyOf(item, false);
					}
					catch
					{
					}
					try
					{
						item.SetIsEnemyOf(_duelEnemyTeam, false);
					}
					catch
					{
					}
					try
					{
						_duelEnemyTeam.SetIsEnemyOf(item, false);
					}
					catch
					{
					}
				}
			}
			_duelEnemyTeam.SetIsEnemyOf(_duelPlayerTeam, true);
			_duelPlayerTeam.SetIsEnemyOf(_duelEnemyTeam, true);
		}
		catch
		{
		}
		Logger.Log("DuelBehavior", "[FormalDuel] 已创建临时决斗队伍，非决斗参与者与双方中立。");
		return true;
	}

	internal static void ShowDuelRiskWarning()
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
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
		//IL_04b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c5: Expected O, but got Unknown
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0559: Unknown result type (might be due to invalid IL or missing references)
		//IL_055e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0568: Expected O, but got Unknown
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Invalid comparison between Unknown and I4
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		if (Hero.MainHero.Clan.Tier < DuelSettings.GetSettings().MinimumClanTier)
		{
			Logger.Log("DuelBehavior", "决斗失败: 玩家家族等级不足");
			return;
		}
		_targetHero = target;
		Mission current = Mission.Current;
		string text = ((current != null) ? current.SceneName : null) ?? "Unknown";
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
			Mission current2 = Mission.Current;
			if (current2 != null)
			{
				_preDuelMode = current2.Mode;
			}
			_duelResultRecorded = false;
			_forcedMainHeroDeath = false;
			EnsureDeathBehaviorsPresent();
			_preDuelTargetTeam = agent.Team;
			Agent main = Agent.Main;
			_preDuelPlayerTeam = ((main != null) ? main.Team : null) ?? current2.PlayerTeam;
			Agent main2 = Agent.Main;
			object preDuelPlayerMountTeam;
			if (main2 == null)
			{
				preDuelPlayerMountTeam = null;
			}
			else
			{
				Agent mountAgent = main2.MountAgent;
				preDuelPlayerMountTeam = ((mountAgent != null) ? mountAgent.Team : null);
			}
			_preDuelPlayerMountTeam = (Team)preDuelPlayerMountTeam;
			Agent mountAgent2 = agent.MountAgent;
			_preDuelTargetMountTeam = ((mountAgent2 != null) ? mountAgent2.Team : null);
			_currentDuelIsArena = flag;
			_meetingPreFightActive = false;
			if (!_arenaMissionActive && flag && current2 != null && ((int)current2.Mode == 1 || Campaign.Current.ConversationManager.OneToOneConversationAgent != null))
			{
				ConversationManager conversationManager = Campaign.Current.ConversationManager;
				if (conversationManager != null)
				{
					conversationManager.EndConversation();
				}
			}
			_isDuelActive = true;
			Dictionary<string, float> duelCooldowns = _duelCooldowns;
			string stringId = ((MBObjectBase)_targetHero).StringId;
			CampaignTime now = CampaignTime.Now;
			duelCooldowns[stringId] = (float)((CampaignTime)(ref now)).ToDays;
			DuelSettings settings = DuelSettings.GetSettings();
			if (current2 != null)
			{
				current2.SetMissionMode((MissionMode)2, true);
				if (!flag)
				{
					Agent main3 = Agent.Main;
					_duelPlayerTeam = ((main3 != null) ? main3.Team : null) ?? current2.PlayerTeam;
					_duelEnemyTeam = agent.Team;
					if (_duelEnemyTeam == null || _duelEnemyTeam == _duelPlayerTeam)
					{
						Team val = null;
						try
						{
							val = current2.PlayerEnemyTeam;
						}
						catch
						{
							val = null;
						}
						if (val != null && val != _duelPlayerTeam)
						{
							_duelEnemyTeam = val;
							try
							{
								agent.SetTeam(_duelEnemyTeam, true);
							}
							catch
							{
							}
							try
							{
								Agent mountAgent3 = agent.MountAgent;
								if (mountAgent3 != null && mountAgent3.IsActive())
								{
									mountAgent3.SetTeam(_duelEnemyTeam, true);
								}
							}
							catch
							{
							}
							Logger.Log("DuelBehavior", "[MeetingDuel] 目标与玩家同队，已切到原生敌方队伍作为决斗对手。");
						}
					}
					if (_duelPlayerTeam == null || _duelEnemyTeam == null || _duelEnemyTeam == _duelPlayerTeam)
					{
						Logger.Log("DuelBehavior", "[MeetingDuel][ERROR] 无法建立稳定的决斗队伍关系，已取消本次决斗以避免异常。");
						_isDuelActive = false;
						return;
					}
					try
					{
						foreach (Team item in (List<Team>)(object)current2.Teams)
						{
							if (item != null && item != _duelPlayerTeam && item != _duelEnemyTeam)
							{
								try
								{
									item.SetIsEnemyOf(_duelPlayerTeam, false);
								}
								catch
								{
								}
								try
								{
									_duelPlayerTeam.SetIsEnemyOf(item, false);
								}
								catch
								{
								}
								try
								{
									item.SetIsEnemyOf(_duelEnemyTeam, false);
								}
								catch
								{
								}
								try
								{
									_duelEnemyTeam.SetIsEnemyOf(item, false);
								}
								catch
								{
								}
							}
						}
						_duelEnemyTeam.SetIsEnemyOf(_duelPlayerTeam, true);
						_duelPlayerTeam.SetIsEnemyOf(_duelEnemyTeam, true);
					}
					catch
					{
					}
					TrySetAgentController(agent, "None");
					try
					{
						agent.SetIsAIPaused(true);
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
						Agent.Main.SetMortalityState((MortalityState)1);
					}
					catch
					{
					}
					try
					{
						agent.SetMortalityState((MortalityState)1);
					}
					catch
					{
					}
					_meetingPreFightActive = true;
					_meetingPreFightEndTime = current2.CurrentTime + 10f;
					RefreshMeetingDuelParticipantLocks(Agent.Main, agent, preFight: true);
					InformationManager.DisplayMessage(new InformationMessage("双方就位！10秒后正式开战（此期间无法互相伤害）", Color.FromUint(4281637083u)));
				}
				else
				{
					Logger.Log("DuelBehavior", "[ArenaInfo] 竞技场 Mission 中保持目标在当前队伍，不再依赖 PlayerEnemyTeam。");
				}
			}
			agent.SetWatchState((WatchState)2);
			if (Agent.Main != null)
			{
				Agent.Main.Health = Agent.Main.HealthLimit;
			}
			agent.Health = agent.HealthLimit;
			Mission current4 = Mission.Current;
			string text2 = ((current4 != null) ? current4.SceneName : null) ?? "Unknown";
			if (flag)
			{
				string text3 = $"【竞技场决斗已开始】当前场景: {text2}。规则：任一方生命值低于 {DuelSettings.GetHealthThreshold():P0} 判定为战败。";
				InformationManager.DisplayMessage(new InformationMessage(text3, Color.FromUint(4281637083u)));
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
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Expected O, but got Unknown
		try
		{
			string text = "arena_vlandia_a";
			Mission current = Mission.Current;
			string text2 = ((current != null) ? current.SceneName : null) ?? "Unknown";
			Hero mainHero = Hero.MainHero;
			object obj;
			if (mainHero == null)
			{
				obj = null;
			}
			else
			{
				Settlement currentSettlement = mainHero.CurrentSettlement;
				obj = ((currentSettlement != null) ? ((MBObjectBase)currentSettlement).StringId : null);
			}
			if (obj == null)
			{
				obj = "";
			}
			string text3 = (string)obj;
			string[] obj2 = new string[8] { "[ArenaTeleport] 尝试通过 MissionState.OpenNew 切换到竞技场。CurrentScene=", text2, ", TargetScene=", text, ", SettlementId=", text3, ", Target=", null };
			Hero obj3 = target;
			obj2[7] = ((obj3 != null) ? ((MBObjectBase)obj3).StringId : null);
			Logger.Log("DuelBehavior", string.Concat(obj2));
			MissionInitializerRecord val = default(MissionInitializerRecord);
			((MissionInitializerRecord)(ref val))._002Ector(text);
			MissionState.OpenNew("AnimusForge_ArenaDuel", val, (InitializeMissionBehaviorsDelegate)((Mission mission) => (IEnumerable<MissionBehavior>)(object)new MissionBehavior[4]
			{
				new ArenaDuelMissionBehavior(target),
				(MissionBehavior)new AgentHumanAILogic(),
				(MissionBehavior)new DuelPlayerDeathAgentStateDeciderLogic(),
				new DuelMainHeroDeathMissionBehavior()
			}), true, true);
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
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Invalid comparison between Unknown and I4
		try
		{
			if (!_leaveSourceMissionRequested || (Mission.Current != null && Mission.Current.CurrentTime < _leaveSourceMissionReadyTime) || (int)Mission.Current.Mode == 1)
			{
				return;
			}
			Campaign current = Campaign.Current;
			object obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				ConversationManager conversationManager = current.ConversationManager;
				obj = ((conversationManager != null) ? conversationManager.OneToOneConversationAgent : null);
			}
			if (obj != null)
			{
				return;
			}
			Mission current2 = Mission.Current;
			if (current2 == null)
			{
				_leaveSourceMissionRequested = false;
				return;
			}
			string text = current2.SceneName ?? string.Empty;
			if (!text.Equals("arena_vlandia_a", StringComparison.OrdinalIgnoreCase))
			{
				_leaveSourceMissionRequested = false;
				_leaveSourceMissionReadyTime = 0f;
				Logger.Log("ArenaDuel", "[Leave] GlobalSourceMissionLeaveTick 10秒等待结束，正在退出原始 Mission。");
				current2.EndMission();
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
				ConversationManager conversationManager = Campaign.Current.ConversationManager;
				flag = ((conversationManager != null) ? conversationManager.OneToOneConversationAgent : null) != null;
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
				Game current = Game.Current;
				object obj2;
				if (current == null)
				{
					obj2 = null;
				}
				else
				{
					GameStateManager gameStateManager = current.GameStateManager;
					obj2 = ((gameStateManager != null) ? gameStateManager.ActiveState : null);
				}
				string text = obj2?.GetType()?.FullName ?? string.Empty;
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
			object obj = Settlement.CurrentSettlement;
			if (obj == null)
			{
				MobileParty mainParty = MobileParty.MainParty;
				obj = ((mainParty != null) ? mainParty.CurrentSettlement : null);
			}
			Settlement val = (Settlement)obj;
			if (val == null)
			{
				_openTownMenuRequested = false;
				return;
			}
			string text = null;
			if (val.IsTown)
			{
				text = "town";
			}
			else if (val.IsCastle)
			{
				text = "castle";
			}
			else if (val.IsVillage)
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
			Logger.Log("ArenaDuel", "[Leave] GlobalTownMenuTick 打开菜单: " + text + ", Settlement=" + ((MBObjectBase)val).StringId);
		}
		catch (Exception ex)
		{
			_openTownMenuRequested = false;
			Logger.Log("ArenaDuel", "[ERROR] GlobalTownMenuTick: " + ex.ToString());
		}
	}

	private void EndDuel(bool playerDefeated)
	{
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Expected O, but got Unknown
		if (_duelResultRecorded)
		{
			return;
		}
		_duelResultRecorded = true;
		bool flag = !playerDefeated;
		if (_targetHero != null && !string.IsNullOrEmpty(((MBObjectBase)_targetHero).StringId))
		{
			_lastDuelResults[((MBObjectBase)_targetHero).StringId] = (flag ? 1 : (-1));
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
					agent.Team.SetIsEnemyOf(main.Team, false);
					main.Team.SetIsEnemyOf(agent.Team, false);
				}
				agent.SetWatchState((WatchState)0);
				agent.ClearTargetFrame();
			}
		}
		string text = ApplyDuelStakeSettlementAndBuildResultText(_targetHero, flag);
		string text2 = (flag ? "【决斗结果】你赢了！" : "【决斗结果】你输了！");
		Color val = (flag ? Color.FromUint(4281257073u) : Color.FromUint(4293348412u));
		string text3 = (_currentDuelIsArena ? " 10秒后退出竞技场..." : "");
		InformationManager.DisplayMessage(new InformationMessage(text2 + text + text3, val));
	}

	private void RestoreState()
	{
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		Agent agent = GetAgent(_targetHero);
		try
		{
			_meetingPreFightActive = false;
			_meetingPreFightEndTime = 0f;
			RestoreFormalDuelSpectatorControl();
		}
		catch
		{
		}
		if (agent != null)
		{
			try
			{
				if (_preDuelTargetTeam != null)
				{
					agent.SetTeam(_preDuelTargetTeam, true);
				}
			}
			catch
			{
			}
			agent.Health = agent.HealthLimit;
			TrySetAgentController(agent, "AI");
			try
			{
				agent.SetMortalityState((MortalityState)0);
			}
			catch
			{
			}
			try
			{
				agent.SetIsAIPaused(false);
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
					try
					{
						if (_preDuelTargetMountTeam != null)
						{
							mountAgent.SetTeam(_preDuelTargetMountTeam, true);
						}
					}
					catch
					{
					}
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
				Agent.Main.SetMortalityState((MortalityState)0);
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
				Mission.Current.SetMissionMode((MissionMode)(flag ? 2 : ((int)_preDuelMode)), true);
			}
			catch
			{
			}
		}
		_preDuelPlayerTeam = null;
		_preDuelTargetTeam = null;
		_preDuelPlayerMountTeam = null;
		_preDuelTargetMountTeam = null;
	}

	private void FinishDuel()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected O, but got Unknown
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
				current.AddMissionBehavior((MissionBehavior)(object)new DuelPlayerDeathAgentStateDeciderLogic());
			}
			if (!flag2)
			{
				current.AddMissionBehavior((MissionBehavior)(object)new DuelMainHeroDeathMissionBehavior());
			}
			if (!flag3)
			{
				current.AddMissionBehavior((MissionBehavior)(object)new DuelTargetDeathMissionBehavior());
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
				KillCharacterAction.ApplyByBattle(mainHero, killerHero, true);
			}
		}
		catch
		{
		}
	}

	private static void ForceKillAgentVisual(Agent victim, Agent killer)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (victim != null && (int)victim.State != 4)
			{
				object obj = killer;
				if (obj == null)
				{
					Mission current = Mission.Current;
					obj = ((current != null) ? current.MainAgent : null) ?? victim;
				}
				Agent val = (Agent)obj;
				if (val != null && val.Monster != null && victim.Monster != null)
				{
					Blow val2 = default(Blow);
					((Blow)(ref val2))._002Ector(val.Index);
					val2.DamageType = (DamageTypes)2;
					val2.BoneIndex = victim.Monster.HeadLookDirectionBoneIndex;
					val2.GlobalPosition = victim.Position;
					val2.GlobalPosition.z = val2.GlobalPosition.z + victim.GetEyeGlobalHeight();
					val2.BaseMagnitude = 2000f;
					((BlowWeaponRecord)(ref val2.WeaponRecord)).FillAsMeleeBlow((ItemObject)null, (WeaponComponentData)null, -1, (sbyte)(-1));
					val2.InflictedDamage = 2000;
					val2.SwingDirection = victim.LookDirection;
					val2.Direction = val2.SwingDirection;
					val2.DamageCalculated = true;
					sbyte mainHandItemBoneIndex = val.Monster.MainHandItemBoneIndex;
					AttackCollisionData attackCollisionDataForDebugPurpose = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(false, false, false, true, false, false, false, false, false, false, false, false, (CombatCollisionResult)1, -1, 0, 2, val2.BoneIndex, (BoneBodyPartType)0, mainHandItemBoneIndex, (UsageDirection)2, -1, (CombatHitResultFlags)0, 0.5f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, Vec3.Up, val2.Direction, val2.GlobalPosition, Vec3.Zero, Vec3.Zero, victim.Velocity, Vec3.Up);
					victim.RegisterBlow(val2, ref attackCollisionDataForDebugPurpose);
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
			PropertyInfo propertyInfo = ((object)agent).GetType().GetProperty("Controller") ?? ((object)agent).GetType().GetProperty("ControllerType");
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
				string[] array = names;
				foreach (string text in array)
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
		Mission current = Mission.Current;
		return (current != null) ? ((IEnumerable<Agent>)current.Agents).FirstOrDefault((Agent a) => (object)a.Character == hero.CharacterObject) : null;
	}
}
