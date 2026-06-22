using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Arena;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.SaveSystem;

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

		public Dictionary<string, int> Items;

		public int PlayerGold;

		public Dictionary<string, int> PlayerItems;

		public int NpcGold;

		public Dictionary<string, int> NpcItems;

		public long UtcTicks;
	}

	private class PendingDuelDebtTag
	{
		public int Amount;

		public int DueDays;

		public string Note;

		public long UtcTicks;
	}

	private sealed class WildernessDuelBattleRuntime
	{
		public Hero TargetHero;

		public MobileParty OpponentDummyParty;

		public MobileParty TargetOriginalParty;

		public bool TargetWasOriginalLeader;

		public MapEvent MapEvent;

		public int DiagnosticId;

		public bool SettlementDone;

		public bool CleanupDone;

		public bool PlayerDefeated;
	}

	private sealed class WildernessDuelBattleMissionLogic : MissionLogic
	{
		private readonly WildernessDuelBattleRuntime _runtime;

		private BattleEndLogic _battleEndLogic;

		private bool _startedLogged;

		private bool _nonDuelAgentsPruned;

		private float _nextPruneTime = 0.2f;

		private float _leaveTime = -1f;

		public WildernessDuelBattleMissionLogic(WildernessDuelBattleRuntime runtime)
		{
			_runtime = runtime;
		}

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			CacheBattleEndLogic();
			TryDisableBattleEndLogic("OnBehaviorInitialize");
			if (_runtime != null)
			{
				LogWildernessDuelDiagnostic("vanilla_behavior.OnBehaviorInitialize", _runtime.DiagnosticId, _runtime.TargetHero);
			}
		}

		public override void AfterStart()
		{
			base.AfterStart();
			_arenaMissionStartedOnce = true;
			_arenaMissionOpeningGraceUntilUtcTicks = 0L;
			_arenaMissionActive = true;
			_returnToMapAfterIndependentDuel = true;
			if (_battleEndLogic == null)
			{
				CacheBattleEndLogic();
			}
			TryDisableBattleEndLogic("AfterStart");
			try
			{
				if (base.Mission != null && base.Mission.Mode == MissionMode.Deployment)
				{
					base.Mission.SetMissionMode(MissionMode.Battle, atStart: true);
				}
			}
			catch
			{
			}
			if (_runtime != null)
			{
				LogWildernessDuelDiagnostic("vanilla_behavior.AfterStart", _runtime.DiagnosticId, _runtime.TargetHero);
			}
			_startedLogged = true;
		}

		public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
		{
			canPlayerLeave = true;
			return null;
		}

		public override bool MissionEnded(ref MissionResult missionResult)
		{
			return false;
		}

		protected override void OnEndMission()
		{
			try
			{
				if (_runtime != null)
				{
					if (!_runtime.SettlementDone)
					{
						TryResolveResultFromAgents("OnEndMission");
					}
					CleanupWildernessDuelRuntime(_runtime, "OnEndMission");
				}
			}
			catch (Exception ex)
			{
				Logger.Log("DuelBehavior", "[WildernessDuel][ERROR] OnEndMission cleanup: " + ex);
			}
			base.OnEndMission();
		}

		public override void OnMissionTick(float dt)
		{
			base.OnMissionTick(dt);
			if (_runtime == null || base.Mission == null)
			{
				return;
			}
			if (!_startedLogged)
			{
				_startedLogged = true;
				LogWildernessDuelDiagnostic("vanilla_behavior.first_tick", _runtime.DiagnosticId, _runtime.TargetHero);
			}
			if (base.Mission.CurrentTime >= _nextPruneTime)
			{
				_nextPruneTime = base.Mission.CurrentTime + 0.5f;
				PruneNonDuelAgents();
				TryDisableBattleEndLogic("retry_tick");
			}
			if (!_runtime.SettlementDone)
			{
				TryResolveResultFromAgents("tick");
			}
			if (_runtime.SettlementDone && _leaveTime > 0f && base.Mission.CurrentTime >= _leaveTime)
			{
				try
				{
					if (!base.Mission.IsMissionEnding)
					{
						base.Mission.EndMission();
					}
				}
				catch (Exception ex)
				{
					Logger.Log("DuelBehavior", "[WildernessDuel][ERROR] EndMission: " + ex);
				}
				_leaveTime = -1f;
			}
		}

		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
		{
			base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
			if (_runtime != null && !_runtime.SettlementDone && (agentState == AgentState.Killed || agentState == AgentState.Unconscious))
			{
				TryResolveResultFromAgents("OnAgentRemoved");
			}
		}

		private void CacheBattleEndLogic()
		{
			try
			{
				_battleEndLogic = base.Mission?.GetMissionBehavior<BattleEndLogic>();
			}
			catch
			{
				_battleEndLogic = null;
			}
		}

		private void TryDisableBattleEndLogic(string source)
		{
			try
			{
				if (_battleEndLogic == null)
				{
					CacheBattleEndLogic();
				}
				_battleEndLogic?.ChangeCanCheckForEndCondition(true);
			}
			catch (Exception ex)
			{
				Logger.Log("DuelBehavior", "[WildernessDuel][WARN] battle_end_disable " + source + ": " + ex.Message);
			}
		}

		private void PruneNonDuelAgents()
		{
			try
			{
				if (base.Mission?.Agents == null)
				{
					return;
				}
				foreach (Agent agent in base.Mission.Agents.ToList())
				{
					if (agent == null || !agent.IsHuman || !agent.IsActive())
					{
						continue;
					}
					if (IsDuelParticipant(agent))
					{
						continue;
					}
					try
					{
						agent.SetMortalityState(Agent.MortalityState.Invulnerable);
					}
					catch
					{
					}
					try
					{
						agent.FadeOut(hideInstantly: true, hideMount: true);
					}
					catch
					{
					}
					try
					{
						agent.SetTeam(null, sync: false);
					}
					catch
					{
					}
				}
				if (!_nonDuelAgentsPruned)
				{
					_nonDuelAgentsPruned = true;
					LogWildernessDuelDiagnostic("vanilla_behavior.non_duel_agents_pruned", _runtime.DiagnosticId, _runtime.TargetHero);
				}
			}
			catch (Exception ex)
			{
				Logger.Log("DuelBehavior", "[WildernessDuel][WARN] prune agents failed: " + ex.Message);
			}
		}

		private bool IsDuelParticipant(Agent agent)
		{
			try
			{
				if (agent == base.Mission?.MainAgent || agent == Agent.Main || agent.IsMainAgent)
				{
					return true;
				}
				CharacterObject character = agent.Character as CharacterObject;
				return character != null && _runtime?.TargetHero != null && character == _runtime.TargetHero.CharacterObject;
			}
			catch
			{
				return false;
			}
		}

		private Agent FindTargetAgent()
		{
			try
			{
				CharacterObject targetCharacter = _runtime?.TargetHero?.CharacterObject;
				if (targetCharacter == null || base.Mission?.Agents == null)
				{
					return null;
				}
				foreach (Agent agent in base.Mission.Agents)
				{
					if (agent != null && agent.Character == targetCharacter)
					{
						return agent;
					}
				}
			}
			catch
			{
			}
			return null;
		}

		private void TryResolveResultFromAgents(string source)
		{
			try
			{
				Agent player = base.Mission?.MainAgent ?? Agent.Main;
				Agent target = FindTargetAgent();
				if (player == null)
				{
					return;
				}
				bool playerDown = !player.IsActive() || player.State == AgentState.Killed || player.State == AgentState.Unconscious || player.Health <= 0f;
				bool targetDown = target != null && (!target.IsActive() || target.State == AgentState.Killed || target.State == AgentState.Unconscious || target.Health <= 0f);
				if (!playerDown && !targetDown && target != null && player.HealthLimit > 0f && target.HealthLimit > 0f)
				{
					float threshold = DuelSettings.GetHealthThreshold();
					playerDown = (player.Health / player.HealthLimit) <= threshold;
					targetDown = (target.Health / target.HealthLimit) <= threshold;
				}
				if (playerDown)
				{
					SettleWildernessDuelRuntime(_runtime, playerDefeated: true, source);
					_leaveTime = base.Mission.CurrentTime + 2f;
				}
				else if (targetDown)
				{
					SettleWildernessDuelRuntime(_runtime, playerDefeated: false, source);
					_leaveTime = base.Mission.CurrentTime + 2f;
				}
			}
			catch (Exception ex)
			{
				Logger.Log("DuelBehavior", "[WildernessDuel][WARN] result check failed " + source + ": " + ex.Message);
			}
		}
	}

	private class ArenaDuelMissionBehavior : MissionBehavior
	{
		private readonly Hero _targetHero;

		private readonly bool _isWildernessDuel;

		private readonly int _diagnosticId;

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

		public ArenaDuelMissionBehavior(Hero target, bool isWildernessDuel = false, int diagnosticId = 0)
		{
			_targetHero = target;
			_isWildernessDuel = isWildernessDuel;
			_diagnosticId = diagnosticId;
		}

		public override void OnAfterMissionCreated()
		{
			base.OnAfterMissionCreated();
			if (_isWildernessDuel)
			{
				LogWildernessDuelDiagnostic("behavior.OnAfterMissionCreated", _diagnosticId, _targetHero);
			}
		}

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			if (_isWildernessDuel)
			{
				LogWildernessDuelDiagnostic("behavior.OnBehaviorInitialize", _diagnosticId, _targetHero);
			}
		}

		public override void OnCreated()
		{
			base.OnCreated();
			if (_isWildernessDuel)
			{
				LogWildernessDuelDiagnostic("behavior.OnCreated", _diagnosticId, _targetHero);
			}
		}

		public override void EarlyStart()
		{
			base.EarlyStart();
			if (_isWildernessDuel)
			{
				LogWildernessDuelDiagnostic("behavior.EarlyStart", _diagnosticId, _targetHero);
			}
		}

		public override void AfterStart()
		{
			base.AfterStart();
			try
			{
				if (_isWildernessDuel)
				{
					LogWildernessDuelDiagnostic("behavior.AfterStart.enter", _diagnosticId, _targetHero);
				}
				if (base.Mission != null && !_setupDone)
				{
					base.Mission.SetMissionMode(MissionMode.Battle, atStart: true);
					_arenaMissionStartedOnce = true;
					_arenaMissionOpeningGraceUntilUtcTicks = 0L;
					_arenaMissionActive = true;
					SetupArenaDuel();
					_setupDone = _localAgentsSpawned;
					if (_isWildernessDuel)
					{
						LogWildernessDuelDiagnostic("behavior.AfterStart.after_setup spawned=" + _localAgentsSpawned, _diagnosticId, _targetHero);
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log("ArenaDuel", "[ERROR] AfterStart: " + ex.ToString());
				if (_isWildernessDuel)
				{
					LogWildernessDuelDiagnostic("behavior.AfterStart.error " + ex.GetType().Name + ": " + ex.Message, _diagnosticId, _targetHero);
				}
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
				Vec3 position;
				Vec3 position2;
				Vec2 direction;
				Vec2 direction2;
				if (_isWildernessDuel)
				{
					ResolveIndependentDuelSpawnFrames(base.Mission.Scene, out var playerFrame, out var enemyFrame);
					position = playerFrame.origin;
					position2 = enemyFrame.origin;
					direction = new Vec2(playerFrame.rotation.f.x, playerFrame.rotation.f.y);
					direction2 = new Vec2(enemyFrame.rotation.f.x, enemyFrame.rotation.f.y);
				}
				else
				{
					Vec3 vec = new Vec3(156f, 113f);
					position = vec + new Vec3(-4f);
					position2 = vec + new Vec3(4f);
					direction = new Vec2(1f, 0f);
					direction2 = new Vec2(-1f, 0f);
				}
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
				AnimusForgeQuickInfo.Show("双方就位！5秒后开始决斗！(无敌保护中)", _targetHero?.CharacterObject);
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

		private static bool TryGetTaggedFrame(Scene scene, string[] tags, out MatrixFrame frame)
		{
			frame = MatrixFrame.Identity;
			if (scene == null || tags == null)
			{
				return false;
			}
			foreach (string tag in tags)
			{
				if (string.IsNullOrWhiteSpace(tag))
				{
					continue;
				}
				try
				{
					GameEntity gameEntity = scene.FindEntityWithTag(tag);
					if (gameEntity != null)
					{
						frame = gameEntity.GetGlobalFrame();
						frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
						return true;
					}
				}
				catch
				{
				}
			}
			return false;
		}

		private static Vec3 NormalizePlanar(Vec3 vector, Vec3 fallback)
		{
			vector.z = 0f;
			if (vector.LengthSquared < 0.0001f)
			{
				vector = fallback;
				vector.z = 0f;
			}
			if (vector.LengthSquared < 0.0001f)
			{
				vector = new Vec3(1f, 0f, 0f);
			}
			vector.Normalize();
			return vector;
		}

		private static MatrixFrame BuildFacingFrame(Vec3 origin, Vec3 forward)
		{
			forward = NormalizePlanar(forward, new Vec3(1f, 0f, 0f));
			MatrixFrame result = MatrixFrame.Identity;
			result.origin = origin;
			result.rotation.f = forward;
			result.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			return result;
		}

		private static void ResolveSpawnHeight(Scene scene, ref Vec3 position)
		{
			if (scene == null)
			{
				return;
			}
			try
			{
				float height = position.z;
				if (scene.GetHeightAtPoint(position.AsVec2, BodyFlags.CommonCollisionExcludeFlags, ref height))
				{
					position.z = height;
				}
				else
				{
					position.z = scene.GetGroundHeightAtPosition(position);
				}
			}
			catch
			{
			}
		}

		private static bool TryGetDuelSceneCenter(Scene scene, out Vec3 center)
		{
			center = Vec3.Zero;
			if (scene == null)
			{
				return false;
			}
			try
			{
				if (TryGetBoundaryPolygonCenter(scene, out var center2D))
				{
					center = new Vec3(center2D.x, center2D.y);
					ResolveSpawnHeight(scene, ref center);
					return true;
				}
				scene.GetBoundingBox(out var min, out var max);
				if (min == Vec3.Invalid || max == Vec3.Invalid)
				{
					scene.GetSceneLimits(out min, out max);
				}
				if (min == Vec3.Invalid || max == Vec3.Invalid)
				{
					return false;
				}
				center = new Vec3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, (min.z + max.z) * 0.5f);
				ResolveSpawnHeight(scene, ref center);
				return true;
			}
			catch
			{
				center = Vec3.Zero;
				return false;
			}
		}

		private static bool TryGetBoundaryPolygonCenter(Scene scene, out Vec2 center2D)
		{
			center2D = Vec2.Zero;
			if (!TryGetMissionBoundaryPolygon(scene, out var polygon) || polygon.Count < 3)
			{
				return false;
			}
			if (!TryComputePolygonCentroid(polygon, out var centroid))
			{
				return false;
			}
			if (IsPointInsidePolygon(centroid, polygon))
			{
				center2D = centroid;
				return true;
			}
			if (TryFindNearestInsidePoint(polygon, centroid, out var insidePoint))
			{
				center2D = insidePoint;
				return true;
			}
			return false;
		}

		private static bool TryGetMissionBoundaryPolygon(Scene scene, out List<Vec2> polygon)
		{
			polygon = new List<Vec2>();
			if (scene == null)
			{
				return false;
			}
			try
			{
				int count = 0;
				try
				{
					count = scene.GetHardBoundaryVertexCount();
				}
				catch
				{
					count = 0;
				}
				if (count > 2)
				{
					for (int i = 0; i < count; i++)
					{
						try
						{
							polygon.Add(scene.GetHardBoundaryVertex(i));
						}
						catch
						{
						}
					}
				}
				if (polygon.Count < 3)
				{
					polygon.Clear();
					try
					{
						count = scene.GetSoftBoundaryVertexCount();
					}
					catch
					{
						count = 0;
					}
					if (count > 2)
					{
						for (int j = 0; j < count; j++)
						{
							try
							{
								polygon.Add(scene.GetSoftBoundaryVertex(j));
							}
							catch
							{
							}
						}
					}
				}
			}
			catch
			{
				polygon.Clear();
			}
			if (polygon.Count >= 2)
			{
				Vec2 first = polygon[0];
				Vec2 last = polygon[polygon.Count - 1];
				if ((first - last).LengthSquared < 0.0001f)
				{
					polygon.RemoveAt(polygon.Count - 1);
				}
			}
			return polygon.Count >= 3;
		}

		private static bool TryComputePolygonCentroid(List<Vec2> polygon, out Vec2 centroid)
		{
			centroid = Vec2.Zero;
			if (polygon == null || polygon.Count < 3)
			{
				return false;
			}
			float area2 = 0f;
			float x = 0f;
			float y = 0f;
			int count = polygon.Count;
			for (int i = 0; i < count; i++)
			{
				Vec2 current = polygon[i];
				Vec2 next = polygon[(i + 1) % count];
				float cross = current.x * next.y - next.x * current.y;
				area2 += cross;
				x += (current.x + next.x) * cross;
				y += (current.y + next.y) * cross;
			}
			if (Math.Abs(area2) < 0.0001f)
			{
				float avgX = 0f;
				float avgY = 0f;
				for (int j = 0; j < count; j++)
				{
					avgX += polygon[j].x;
					avgY += polygon[j].y;
				}
				centroid = new Vec2(avgX / count, avgY / count);
				return true;
			}
			float scale = 1f / (3f * area2);
			centroid = new Vec2(x * scale, y * scale);
			return true;
		}

		private static bool IsPointInsidePolygon(Vec2 point, List<Vec2> polygon)
		{
			if (polygon == null || polygon.Count < 3)
			{
				return false;
			}
			bool inside = false;
			int count = polygon.Count;
			int previous = count - 1;
			for (int i = 0; i < count; i++)
			{
				Vec2 current = polygon[i];
				Vec2 last = polygon[previous];
				if (current.y > point.y != last.y > point.y && point.x < (last.x - current.x) * (point.y - current.y) / (last.y - current.y + 1E-06f) + current.x)
				{
					inside = !inside;
				}
				previous = i;
			}
			return inside;
		}

		private static bool TryFindNearestInsidePoint(List<Vec2> polygon, Vec2 preferred, out Vec2 insidePoint)
		{
			insidePoint = Vec2.Zero;
			if (polygon == null || polygon.Count < 3)
			{
				return false;
			}
			float minX = float.MaxValue;
			float minY = float.MaxValue;
			float maxX = float.MinValue;
			float maxY = float.MinValue;
			for (int i = 0; i < polygon.Count; i++)
			{
				Vec2 point = polygon[i];
				if (point.x < minX)
				{
					minX = point.x;
				}
				if (point.y < minY)
				{
					minY = point.y;
				}
				if (point.x > maxX)
				{
					maxX = point.x;
				}
				if (point.y > maxY)
				{
					maxY = point.y;
				}
			}
			if (maxX - minX < 0.01f || maxY - minY < 0.01f)
			{
				return false;
			}
			bool found = false;
			float bestDistance = float.MaxValue;
			const int steps = 18;
			for (int x = 0; x <= steps; x++)
			{
				float sampleX = minX + (maxX - minX) * ((float)x / steps);
				for (int y = 0; y <= steps; y++)
				{
					float sampleY = minY + (maxY - minY) * ((float)y / steps);
					Vec2 sample = new Vec2(sampleX, sampleY);
					if (IsPointInsidePolygon(sample, polygon))
					{
						float distance = (sample - preferred).LengthSquared;
						if (!found || distance < bestDistance)
						{
							found = true;
							bestDistance = distance;
							insidePoint = sample;
						}
					}
				}
			}
			return found;
		}

		private static void ClampPointInsideDuelBoundary(Scene scene, ref Vec3 candidate, Vec3 anchor)
		{
			try
			{
				if (scene == null || !TryGetMissionBoundaryPolygon(scene, out var polygon) || polygon.Count < 3)
				{
					return;
				}
				Vec2 candidate2D = candidate.AsVec2;
				if (IsPointInsidePolygon(candidate2D, polygon))
				{
					return;
				}
				Vec2 anchor2D = anchor.AsVec2;
				Vec2 adjusted = anchor2D;
				bool found = false;
				for (int i = 1; i <= 25; i++)
				{
					float t = (float)i / 25f;
					Vec2 sample = candidate2D + (anchor2D - candidate2D) * t;
					if (IsPointInsidePolygon(sample, polygon))
					{
						adjusted = sample;
						found = true;
						break;
					}
				}
				if (!found && !TryFindNearestInsidePoint(polygon, anchor2D, out adjusted))
				{
					return;
				}
				candidate.x = adjusted.x;
				candidate.y = adjusted.y;
				ResolveSpawnHeight(scene, ref candidate);
			}
			catch
			{
			}
		}

		private static void ResolveIndependentDuelSpawnFrames(Scene scene, out MatrixFrame playerFrame, out MatrixFrame enemyFrame)
		{
			MatrixFrame taggedPlayerFrame;
			MatrixFrame taggedEnemyFrame;
			bool hasPlayerFrame = TryGetTaggedFrame(scene, new string[8] { "attacker_mid", "attacker_left", "attacker_right", "player_infantry_spawn", "player_cavalry_spawn", "spawnpoint_player", "spawnpoint_player_outside", "sp_player_conversation" }, out taggedPlayerFrame);
			bool hasEnemyFrame = TryGetTaggedFrame(scene, new string[7] { "defend_mid", "defend_left", "defend_right", "opponent_infantry_spawn", "opponent_cavalry_spawn", "opponent_bodyguard_infantry_spawn", "opponent_bodyguard_cavalry_spawn" }, out taggedEnemyFrame);
			bool hasCenter = TryGetDuelSceneCenter(scene, out var anchor);
			if (!hasCenter)
			{
				anchor = hasPlayerFrame ? taggedPlayerFrame.origin : (hasEnemyFrame ? taggedEnemyFrame.origin : Vec3.Zero);
			}
			Vec3 forward = hasPlayerFrame ? taggedPlayerFrame.rotation.f : new Vec3(1f, 0f, 0f);
			if (hasPlayerFrame && hasEnemyFrame)
			{
				Vec3 towardEnemy = taggedEnemyFrame.origin - taggedPlayerFrame.origin;
				if (towardEnemy.LengthSquared > 0.0001f)
				{
					forward = towardEnemy;
				}
			}
			forward = NormalizePlanar(forward, new Vec3(1f, 0f, 0f));
			Vec3 playerPosition = anchor - forward * 4f;
			Vec3 enemyPosition = anchor + forward * 4f;
			ClampPointInsideDuelBoundary(scene, ref playerPosition, anchor);
			ClampPointInsideDuelBoundary(scene, ref enemyPosition, anchor);
			ResolveSpawnHeight(scene, ref playerPosition);
			ResolveSpawnHeight(scene, ref enemyPosition);
			playerFrame = BuildFacingFrame(playerPosition, forward);
			enemyFrame = BuildFacingFrame(enemyPosition, -forward);
			Logger.Log("ArenaDuel", $"[WildernessSpawn] source={(hasCenter ? "scene_center" : "spawn_tag_fallback")} anchor=({anchor.x:0.0},{anchor.y:0.0},{anchor.z:0.0}) player=({playerPosition.x:0.0},{playerPosition.y:0.0},{playerPosition.z:0.0}) enemy=({enemyPosition.x:0.0},{enemyPosition.y:0.0},{enemyPosition.z:0.0})");
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
					AnimusForgeQuickInfo.Show("决斗开始！", _targetHero?.CharacterObject);
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
					AnimusForgeQuickInfo.Show("正在退出竞技场...");
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
				SetDuelDebtTagGateState(_targetHero, playerDefeated ? -1 : 1);
				MyBehavior.RecordDuelResultForExternal(_targetHero, flag, _isWildernessDuel ? "wilderness" : "arena");
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
				string text3 = _isWildernessDuel ? " 10秒后返回大地图..." : " 10秒后退出竞技场...";
				AnimusForgeQuickInfo.Show(text2 + text + text3, _targetHero?.CharacterObject);
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
				DuelBehavior instance = Instance;
				if (instance != null && instance._isDuelActive && affectedAgent != null && affectedAgent.IsMainAgent && (agentState == AgentState.Unconscious || agentState == AgentState.Killed))
				{
					Hero hero = null;
					try
					{
						hero = ((affectorAgent?.Character is CharacterObject characterObject) ? characterObject.HeroObject : null);
					}
					catch
					{
					}
					ForceKillMainHero(instance._targetHero ?? hero);
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

	private static long _arenaMissionOpeningGraceUntilUtcTicks = 0L;

	private static bool _arenaMissionStartedOnce = false;

	private static int _wildernessDuelDiagnosticSerial = 0;

	private static int _wildernessDuelActiveDiagnosticId = 0;

	private static long _wildernessDuelOpenStartedUtcTicks = 0L;

	private static long _wildernessDuelLastOpeningDiagUtcTicks = 0L;

	private static string _wildernessDuelLastOpenScene = "";

	private const string WildernessDuelDummyPartyPrefix = "animusforge_wilderness_duel_";

	private static WildernessDuelBattleRuntime _wildernessDuelRuntime;

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

	private static bool _queuedWildernessDuel = false;

	private static bool _queuedDuelWaitingForConversationExit = false;

	private static long _queuedDuelReadyUtcTicks = 0L;

	private static int _queuedDuelConversationCloseAttempts = 0;

	private static bool _returnToMapAfterIndependentDuel = false;

	private static Dictionary<string, PendingDuelStake> _pendingDuelStakes = new Dictionary<string, PendingDuelStake>();

	private static Dictionary<string, PendingDuelDebtTag> _pendingDuelDebtTags = new Dictionary<string, PendingDuelDebtTag>(StringComparer.OrdinalIgnoreCase);

	private static Dictionary<string, int> _duelDebtTagGateStates = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

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

	public static void RegisterHarmonyPatches(Harmony harmony)
	{
		if (harmony == null)
		{
			return;
		}
		PatchHarmonyClass(harmony, typeof(WildernessDuelMapEventResultsPatch));
		PatchHarmonyClass(harmony, typeof(WildernessDuelPlayerEncounterResultsPatch));
		PatchHarmonyClass(harmony, typeof(WildernessDuelPlayerEncounterVictoryPatch));
		PatchHarmonyClass(harmony, typeof(WildernessDuelPlayerEncounterDefeatPatch));
		PatchHarmonyClass(harmony, typeof(WildernessDuelPlayerEncounterEndPatch));
		PatchHarmonyClass(harmony, typeof(WildernessDuelBattleRewardsZeroPatch));
	}

	private static void PatchHarmonyClass(Harmony harmony, Type patchType)
	{
		try
		{
			harmony.CreateClassProcessor(patchType).Patch();
			Logger.Log("DuelBehavior", "[WildernessDuel][Harmony] patched " + patchType.Name);
		}
		catch (Exception ex)
		{
			Logger.Log("DuelBehavior", "[WildernessDuel][Harmony][WARN] failed " + patchType.Name + ": " + ex.Message);
		}
	}

	private static void SetDuelDebtTagGateState(Hero hero, int state)
	{
		try
		{
			string text = hero?.StringId;
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			if (_duelDebtTagGateStates == null)
			{
				_duelDebtTagGateStates = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			}
			_duelDebtTagGateStates[text] = state;
		}
		catch
		{
		}
	}

	public static bool TryConsumeDuelDebtTagPermission(Hero hero, out bool allowDebtTag)
	{
		allowDebtTag = true;
		try
		{
			string text = hero?.StringId;
			if (string.IsNullOrWhiteSpace(text) || _duelDebtTagGateStates == null || !_duelDebtTagGateStates.TryGetValue(text, out var value))
			{
				return false;
			}
			if (value == 0)
			{
				allowDebtTag = false;
				return true;
			}
			_duelDebtTagGateStates.Remove(text);
			allowDebtTag = value < 0;
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static void CachePendingDuelDebtTag(Hero hero, int amount, int dueDays, string note)
	{
		try
		{
			string text = hero?.StringId;
			if (string.IsNullOrWhiteSpace(text) || amount <= 0)
			{
				return;
			}
			if (_pendingDuelDebtTags == null)
			{
				_pendingDuelDebtTags = new Dictionary<string, PendingDuelDebtTag>(StringComparer.OrdinalIgnoreCase);
			}
			_pendingDuelDebtTags[text] = new PendingDuelDebtTag
			{
				Amount = Math.Max(1, amount),
				DueDays = Math.Max(0, dueDays),
				Note = (note ?? "").Trim(),
				UtcTicks = DateTime.UtcNow.Ticks
			};
		}
		catch
		{
		}
	}

	public static bool TryConsumePendingDuelDebtTag(Hero hero, out int amount, out int dueDays, out string note)
	{
		amount = 0;
		dueDays = 0;
		note = null;
		try
		{
			string text = hero?.StringId;
			if (string.IsNullOrWhiteSpace(text) || _pendingDuelDebtTags == null || !_pendingDuelDebtTags.TryGetValue(text, out var value) || value == null)
			{
				return false;
			}
			_pendingDuelDebtTags.Remove(text);
			amount = Math.Max(0, value.Amount);
			dueDays = Math.Max(0, value.DueDays);
			note = (value.Note ?? "").Trim();
			return amount > 0;
		}
		catch
		{
			amount = 0;
			dueDays = 0;
			note = null;
			return false;
		}
	}

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

	private static bool IsCampaignConversationActive()
	{
		try
		{
			var conversationManager = Campaign.Current?.ConversationManager;
			if (conversationManager == null)
			{
				return false;
			}
			if (conversationManager.IsConversationInProgress)
			{
				return true;
			}
			if (conversationManager.OneToOneConversationAgent != null)
			{
				return true;
			}
			if (conversationManager.OneToOneConversationCharacter != null)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static void QueueDuelAfterConversationExit(Hero target, float delaySeconds, bool wildernessDuel)
	{
		_queuedArenaDuelTarget = target;
		_queuedArenaDuelDelay = delaySeconds;
		_queuedWildernessDuel = wildernessDuel;
		_queuedDuelWaitingForConversationExit = true;
		_queuedDuelConversationCloseAttempts = 0;
		_queuedDuelReadyUtcTicks = DateTime.UtcNow.AddMilliseconds(250.0).Ticks;
		Logger.Log("DuelBehavior", "[Queue] Duel queued until campaign conversation exits. wilderness=" + wildernessDuel + ", target=" + target?.StringId);
		if (wildernessDuel)
		{
			LogWildernessDuelDiagnostic("queue.until_conversation_exit delay=" + delaySeconds.ToString("0.0"), _wildernessDuelActiveDiagnosticId, target);
		}
	}

	public static void PrepareDuel(Hero target, float delaySeconds)
	{
		if (target == null)
		{
			Logger.Log("DuelBehavior", "[ArenaTeleport] 收到空目标的决斗请求，已忽略。");
			return;
		}
		if (!CanTargetNpcStartDuel(target, out string blockedReason))
		{
			Logger.Log("DuelBehavior", "[DuelHealthGate] " + blockedReason);
			if (!string.IsNullOrWhiteSpace(blockedReason))
			{
				InformationManager.DisplayMessage(new InformationMessage(blockedReason, Color.FromUint(4294901760u)));
			}
			return;
		}
		ShowDuelRiskWarning();
		bool isWildernessDuel = IsWildernessDuelContext(target, out string wildernessBlockedReason);
		if (isWildernessDuel)
		{
			if (Mission.Current != null)
			{
				Logger.Log("DuelBehavior", "[WildernessDuel][Queue] Current mission=" + Mission.Current.SceneName + ", will leave source mission before opening wilderness duel.");
				_queuedArenaDuelTarget = target;
				_queuedArenaDuelDelay = delaySeconds;
				_queuedWildernessDuel = true;
				_queuedDuelWaitingForConversationExit = false;
				_queuedDuelReadyUtcTicks = 0L;
				_queuedDuelConversationCloseAttempts = 0;
				float num = ((Mission.Current != null) ? Mission.Current.CurrentTime : 0f);
				_leaveSourceMissionReadyTime = num + 10f;
				InformationManager.DisplayMessage(new InformationMessage("双方怒目而视，约定 10 秒后前往野外一决胜负！", Color.FromUint(4294901760u)));
				_leaveSourceMissionRequested = true;
				return;
			}
			if (IsCampaignConversationActive())
			{
				Logger.Log("DuelBehavior", "[WildernessDuel][Queue] Campaign conversation is active; deferring wilderness duel until conversation exits.");
				QueueDuelAfterConversationExit(target, delaySeconds, wildernessDuel: true);
				InformationManager.DisplayMessage(new InformationMessage("AnimusForge: wilderness duel will start after the current conversation closes.", Color.FromUint(4294901760u)));
				return;
			}
			if (Instance != null)
			{
				Logger.Log("DuelBehavior", "[WildernessDuel] Starting independent wilderness duel from campaign map.");
				if (Instance.TryOpenWildernessDuelMission(target))
				{
					return;
				}
			}
			Logger.Log("DuelBehavior", "[WildernessDuel][ERROR] failed to open independent wilderness duel; request aborted.");
			InformationManager.DisplayMessage(new InformationMessage("无法打开野外决斗场景，本次决斗已取消。", Color.FromUint(4294901760u)));
			return;
		}
		if (!string.IsNullOrWhiteSpace(wildernessBlockedReason))
		{
			Logger.Log("DuelBehavior", "[WildernessDuel] Not using wilderness duel: " + wildernessBlockedReason);
		}
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
					_queuedWildernessDuel = false;
					_queuedDuelWaitingForConversationExit = false;
					_queuedDuelReadyUtcTicks = 0L;
					_queuedDuelConversationCloseAttempts = 0;
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

	private static bool CanTargetNpcStartDuel(Hero target, out string blockedReason)
	{
		blockedReason = "";
		if (target == null)
		{
			return false;
		}
		Agent agent = Mission.Current?.Agents.FirstOrDefault((Agent a) => a.Character == target.CharacterObject);
		if (agent != null && agent.HealthLimit > 0f)
		{
			float healthRatio = agent.Health / agent.HealthLimit;
			if (healthRatio < 0.999f)
			{
				blockedReason = AIConfigHandler.FormatDuelHealthTemplate(AIConfigHandler.DuelHealthBlockedMessage, target.Name?.ToString(), healthRatio);
				return false;
			}
		}
		if (target.MaxHitPoints > 0 && target.HitPoints < target.MaxHitPoints)
		{
			float healthRatio2 = (float)target.HitPoints / target.MaxHitPoints;
			blockedReason = AIConfigHandler.FormatDuelHealthTemplate(AIConfigHandler.DuelHealthBlockedMessage, target.Name?.ToString(), healthRatio2);
			return false;
		}
		return true;
	}

	private static bool IsWaterOrSeaTerrain(TerrainType terrainType)
	{
		return terrainType == TerrainType.Water || terrainType == TerrainType.River || terrainType == TerrainType.Lake || terrainType == TerrainType.CoastalSea || terrainType == TerrainType.OpenSea || terrainType == TerrainType.NonNavigableRiver || terrainType == TerrainType.SeaRestriction || terrainType == TerrainType.UnderBridge;
	}

	private static bool IsWildernessDuelContext(Hero target, out string blockedReason)
	{
		blockedReason = "";
		try
		{
			if (Campaign.Current == null || MobileParty.MainParty == null || target == null)
			{
				blockedReason = "campaign context is missing";
				return false;
			}
			if (Settlement.CurrentSettlement != null || MobileParty.MainParty.CurrentSettlement != null || Hero.MainHero?.CurrentSettlement != null)
			{
				blockedReason = "current context is a settlement";
				return false;
			}
			if (!MobileParty.MainParty.Position.IsOnLand || MobileParty.MainParty.IsCurrentlyAtSea)
			{
				blockedReason = "main party is not on land";
				return false;
			}
			try
			{
				if (PlayerEncounter.IsNavalEncounter())
				{
					blockedReason = "current encounter is naval";
					return false;
				}
			}
			catch
			{
			}
			IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
			if (mapSceneWrapper == null)
			{
				blockedReason = "map scene wrapper is missing";
				return false;
			}
			TerrainType faceTerrainType = mapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.CurrentNavigationFace);
			if (IsWaterOrSeaTerrain(faceTerrainType))
			{
				blockedReason = "terrain is not a wilderness land terrain: " + faceTerrainType;
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			blockedReason = ex.Message;
			return false;
		}
	}

	private static Vec2 ResolveWildernessDuelEncounterDirection(MobileParty mainParty, Hero target)
	{
		try
		{
			MobileParty mobileParty = target?.PartyBelongedTo ?? PlayerEncounter.EncounteredMobileParty;
			if (mainParty != null && mobileParty != null)
			{
				Vec2 v = mainParty.Position.ToVec2() - mobileParty.Position.ToVec2();
				if (v.LengthSquared > 0.0001f)
				{
					return v.Normalized();
				}
			}
		}
		catch
		{
		}
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

	private static TerrainType ResolveWildernessDuelTerrainType()
	{
		try
		{
			IMapScene mapSceneWrapper = Campaign.Current?.MapSceneWrapper;
			MobileParty mainParty = MobileParty.MainParty;
			if (mapSceneWrapper != null && mainParty != null)
			{
				TerrainType terrainType = mapSceneWrapper.GetFaceTerrainType(mainParty.CurrentNavigationFace);
				if (!IsWaterOrSeaTerrain(terrainType))
				{
					return terrainType;
				}
				try
				{
					mapSceneWrapper.GetEnvironmentTerrainTypesCount(mainParty.Position, out var currentPositionTerrainType);
					if (!IsWaterOrSeaTerrain(currentPositionTerrainType))
					{
						return currentPositionTerrainType;
					}
				}
				catch
				{
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("DuelBehavior", "[WildernessDuel][WARN] resolve terrain failed: " + ex.Message);
		}
		return TerrainType.Plain;
	}

	private static string ResolveWildernessDuelBattleScene(TerrainType terrainType)
	{
		try
		{
			var scenes = GameSceneDataManager.Instance?.SingleplayerBattleScenes;
			if (scenes != null && scenes.Count > 0)
			{
				List<SingleplayerBattleSceneData> terrainMatches = scenes
					.Where((SingleplayerBattleSceneData scene) => !scene.IsNaval && !string.IsNullOrWhiteSpace(scene.SceneID) && (scene.Terrain == terrainType || (scene.TerrainTypes != null && scene.TerrainTypes.Contains(terrainType))))
					.ToList();
				if (terrainMatches.Count > 0)
				{
					return terrainMatches[MBRandom.RandomInt(terrainMatches.Count)].SceneID;
				}
				List<SingleplayerBattleSceneData> plainMatches = scenes
					.Where((SingleplayerBattleSceneData scene) => !scene.IsNaval && !string.IsNullOrWhiteSpace(scene.SceneID) && scene.Terrain == TerrainType.Plain)
					.ToList();
				if (plainMatches.Count > 0)
				{
					return plainMatches[MBRandom.RandomInt(plainMatches.Count)].SceneID;
				}
				List<SingleplayerBattleSceneData> landMatches = scenes
					.Where((SingleplayerBattleSceneData scene) => !scene.IsNaval && !string.IsNullOrWhiteSpace(scene.SceneID) && !IsWaterOrSeaTerrain(scene.Terrain))
					.ToList();
				if (landMatches.Count > 0)
				{
					return landMatches[MBRandom.RandomInt(landMatches.Count)].SceneID;
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("DuelBehavior", "[WildernessDuel][WARN] resolve battle scene failed: " + ex.Message);
		}
		return "battle_terrain_a";
	}

	private static MissionInitializerRecord BuildWildernessDuelMissionInitializerRecord(Hero target)
	{
		MobileParty mainParty = MobileParty.MainParty;
		TerrainType faceTerrainType = ResolveWildernessDuelTerrainType();
		MapPatchData mapPatchAtPosition = default(MapPatchData);
		bool hasMapPatch = false;
		string battleSceneForMapPatch = "";
		try
		{
			IMapScene mapSceneWrapper = Campaign.Current?.MapSceneWrapper;
			if (mapSceneWrapper != null && mainParty != null)
			{
				mapPatchAtPosition = mapSceneWrapper.GetMapPatchAtPosition(mainParty.Position);
				battleSceneForMapPatch = Campaign.Current.Models.SceneModel.GetBattleSceneForMapPatch(mapPatchAtPosition, isNavalEncounter: false);
				hasMapPatch = !string.IsNullOrWhiteSpace(battleSceneForMapPatch);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("DuelBehavior", "[WildernessDuel][WARN] resolve battle map patch failed: " + ex.Message);
		}
		if (string.IsNullOrWhiteSpace(battleSceneForMapPatch))
		{
			battleSceneForMapPatch = ResolveWildernessDuelBattleScene(faceTerrainType);
			hasMapPatch = false;
		}
		if (string.IsNullOrWhiteSpace(battleSceneForMapPatch))
		{
			throw new InvalidOperationException("Battle scene is empty for wilderness duel.");
		}
		MissionInitializerRecord rec = new MissionInitializerRecord(battleSceneForMapPatch);
		rec.TerrainType = (int)faceTerrainType;
		rec.DamageToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		rec.DamageFromPlayerToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		rec.NeedsRandomTerrain = false;
		rec.PlayingInCampaignMode = true;
		rec.DoNotUseLoadingScreen = false;
		rec.RandomTerrainSeed = MBRandom.RandomInt(10000);
		rec.AtmosphereOnCampaign = Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(mainParty.Position);
		rec.DecalAtlasGroup = 2;
		rec.SceneHasMapPatch = hasMapPatch;
		rec.PatchCoordinates = hasMapPatch ? mapPatchAtPosition.normalizedCoordinates : Vec2.Zero;
		rec.PatchEncounterDir = ResolveWildernessDuelEncounterDirection(mainParty, target);
		Logger.Log("DuelBehavior", "[WildernessDuel] initializer scene=" + rec.SceneName + ", terrain=" + faceTerrainType + ", mapPatch=" + hasMapPatch + ", target=" + target?.StringId);
		return rec;
	}

	private static MissionBehavior CreateWildernessDuelBoundaryCrossingHandler()
	{
#if BANNERLORD_1_4_OR_GREATER
		return new MissionBoundaryCrossingHandler();
#else
		return new MissionBoundaryCrossingHandler(10f);
#endif
	}

	private static string BuildWildernessDuelRuntimeDiagnostic()
	{
		string missionPart;
		try
		{
			Mission mission = Mission.Current;
			if (mission == null)
			{
				missionPart = "mission=null";
			}
			else
			{
				int agentCount = -1;
				int teamCount = -1;
				bool hasMainAgent = false;
				try
				{
					agentCount = mission.Agents?.Count ?? -1;
				}
				catch
				{
				}
				try
				{
					teamCount = mission.Teams?.Count ?? -1;
				}
				catch
				{
				}
				try
				{
					hasMainAgent = mission.MainAgent != null;
				}
				catch
				{
				}
				missionPart = "mission=scene:" + (mission.SceneName ?? "null") + ", mode:" + mission.Mode + ", agents:" + agentCount + ", teams:" + teamCount + ", mainAgent:" + hasMainAgent;
			}
		}
		catch (Exception ex)
		{
			missionPart = "mission=error:" + ex.GetType().Name + ":" + ex.Message;
		}
		string conversationPart;
		try
		{
			conversationPart = "conversation=" + (Campaign.Current?.ConversationManager?.IsConversationInProgress == true);
		}
		catch (Exception ex2)
		{
			conversationPart = "conversation=error:" + ex2.GetType().Name + ":" + ex2.Message;
		}
		string encounterPart;
		try
		{
			encounterPart = "encounterCurrent=" + (PlayerEncounter.Current != null) + ", mapEvent=" + (MapEvent.PlayerMapEvent != null);
		}
		catch (Exception ex3)
		{
			encounterPart = "encounter=error:" + ex3.GetType().Name + ":" + ex3.Message;
		}
		return missionPart + "; " + conversationPart + "; " + encounterPart + "; active=" + _arenaMissionActive + "; started=" + _arenaMissionStartedOnce + "; leaveRequested=" + _arenaMissionLeaveRequested + "; returnToMap=" + _returnToMapAfterIndependentDuel;
	}

	private static void LogWildernessDuelDiagnostic(string stage, int diagnosticId, Hero target = null, MissionInitializerRecord? rec = null)
	{
		try
		{
			string recPart = "";
			if (rec.HasValue)
			{
				MissionInitializerRecord value = rec.Value;
				recPart = "; recScene=" + (value.SceneName ?? "null") + "; recTerrain=" + value.TerrainType + "; recDoNotLoad=" + value.DoNotUseLoadingScreen + "; recRandomTerrain=" + value.NeedsRandomTerrain + "; recCampaign=" + value.PlayingInCampaignMode + "; recMapPatch=" + value.SceneHasMapPatch + "; recPatchDir=" + value.PatchEncounterDir;
			}
			Logger.Log("DuelBehavior", "[WildernessDuelDiag] id=" + diagnosticId + " stage=" + stage + "; target=" + (target?.StringId ?? "null") + "; lastScene=" + (_wildernessDuelLastOpenScene ?? "") + recPart + "; " + BuildWildernessDuelRuntimeDiagnostic());
		}
		catch (Exception ex)
		{
			Logger.Log("DuelBehavior", "[WildernessDuelDiag][ERROR] " + ex.GetType().Name + ": " + ex.Message);
		}
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
			FieldInfo field = target?.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
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

	private static void ResetWildernessDuelOpeningState()
	{
		_arenaMissionActive = false;
		_arenaMissionLeaveRequested = false;
		_arenaMissionLeaveReadyTime = 0f;
		_arenaMissionOpeningGraceUntilUtcTicks = 0L;
		_arenaMissionStartedOnce = false;
		_returnToMapAfterIndependentDuel = false;
		_wildernessDuelActiveDiagnosticId = 0;
		_wildernessDuelOpenStartedUtcTicks = 0L;
		_wildernessDuelLastOpeningDiagUtcTicks = 0L;
	}

	private static WildernessDuelBattleRuntime CreateWildernessDuelRuntime(Hero target, int diagnosticId)
	{
		if (target == null || target.CharacterObject == null)
		{
			throw new InvalidOperationException("Wilderness duel target is invalid.");
		}
		MobileParty mainParty = MobileParty.MainParty;
		if (mainParty == null || PartyBase.MainParty == null)
		{
			throw new InvalidOperationException("Main party is missing.");
		}
		Vec2 direction = ResolveWildernessDuelEncounterDirection(mainParty, target);
		if (direction.LengthSquared < 0.0001f)
		{
			direction = new Vec2(1f, 0f);
		}
		CampaignVec2 dummyPosition = mainParty.Position - direction * 0.35f;
		string partyId = WildernessDuelDummyPartyPrefix + DateTime.UtcNow.Ticks + "_" + MBRandom.RandomInt(1000000);
		MobileParty dummyParty = MobileParty.CreateParty(partyId, new WildernessDuelDummyPartyComponent(dummyPosition, new TextObject("AnimusForge Wilderness Duel"), target, target.Clan ?? Clan.PlayerClan));
		if (dummyParty == null)
		{
			throw new InvalidOperationException("Failed to create wilderness duel dummy party.");
		}
		dummyParty.IsVisible = false;
		dummyParty.SetMoveModeHold();
		MobileParty originalParty = target.PartyBelongedTo;
		bool wasOriginalLeader = false;
		try
		{
			wasOriginalLeader = originalParty != null && originalParty.LeaderHero == target;
		}
		catch
		{
			wasOriginalLeader = false;
		}
		try
		{
			dummyParty.MemberRoster.AddToCounts(target.CharacterObject, 1, insertAtFront: true, woundedCount: 0, xpChange: 0, removeDepleted: true, index: -1);
			dummyParty.PartyComponent?.ChangePartyLeader(target);
		}
		catch
		{
		}
		WildernessDuelBattleRuntime runtime = new WildernessDuelBattleRuntime
		{
			TargetHero = target,
			OpponentDummyParty = dummyParty,
			TargetOriginalParty = originalParty,
			TargetWasOriginalLeader = wasOriginalLeader,
			DiagnosticId = diagnosticId
		};
		FieldBattleEventComponent component = FieldBattleEventComponent.CreateFieldBattleEvent(PartyBase.MainParty, dummyParty.Party);
		runtime.MapEvent = component?.MapEvent;
		if (runtime.MapEvent == null)
		{
			CleanupWildernessDuelRuntime(runtime, "create_mapevent_failed");
			throw new InvalidOperationException("Failed to create wilderness duel MapEvent.");
		}
		runtime.MapEvent.ResetBattleState();
		PlayerEncounter.Start();
		PlayerEncounter.Current.SetupFields(PartyBase.MainParty, dummyParty.Party);
		SetPrivateField(PlayerEncounter.Current, "_mapEvent", runtime.MapEvent);
		_wildernessDuelRuntime = runtime;
		return runtime;
	}

	private static void SetPlayerEncounterState(PlayerEncounter encounter, string stateName)
	{
		if (encounter == null || string.IsNullOrWhiteSpace(stateName))
		{
			return;
		}
		try
		{
			Type stateType = Type.GetType("TaleWorlds.CampaignSystem.Encounters.PlayerEncounterState, TaleWorlds.CampaignSystem");
			if (stateType == null)
			{
				return;
			}
			object value = Enum.Parse(stateType, stateName);
			PropertyInfo property = encounter.GetType().GetProperty("EncounterState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null && property.CanWrite)
			{
				property.SetValue(encounter, value);
				return;
			}
			FieldInfo field = encounter.GetType().GetField("_encounterState", BindingFlags.Instance | BindingFlags.NonPublic)
				?? encounter.GetType().GetField("EncounterState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			field?.SetValue(encounter, value);
		}
		catch (Exception ex)
		{
			Logger.Log("DuelBehavior", "[WildernessDuel][WARN] set encounter state failed: " + ex.Message);
		}
	}

	private static bool IsWildernessDuelMapEvent(MapEvent mapEvent)
	{
		if (mapEvent == null)
		{
			return false;
		}
		try
		{
			if (_wildernessDuelRuntime?.MapEvent != null && ReferenceEquals(_wildernessDuelRuntime.MapEvent, mapEvent))
			{
				return true;
			}
		}
		catch
		{
		}
		return MapEventSideHasWildernessDuelDummyParty(mapEvent.AttackerSide) || MapEventSideHasWildernessDuelDummyParty(mapEvent.DefenderSide);
	}

	private static bool MapEventSideHasWildernessDuelDummyParty(MapEventSide side)
	{
		if (side == null)
		{
			return false;
		}
		try
		{
			foreach (MapEventParty party in side.Parties)
			{
				string id = party?.Party?.MobileParty?.StringId ?? "";
				if (id.StartsWith(WildernessDuelDummyPartyPrefix, StringComparison.Ordinal))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	internal static bool ShouldZeroBattleRewardsForWildernessDuel(string source)
	{
		try
		{
			return IsWildernessDuelMapEvent(MapEvent.PlayerMapEvent);
		}
		catch
		{
			return false;
		}
	}

	internal static bool TryHandleWildernessDuelMapEventResults(MapEvent mapEvent, string source)
	{
		if (!IsWildernessDuelMapEvent(mapEvent))
		{
			return false;
		}
		try
		{
			SetPrivateField(mapEvent, "_mapEventResultsApplied", true);
		}
		catch
		{
		}
		Logger.Log("DuelBehavior", "[WildernessDuel] skipped vanilla map event results source=" + source);
		return true;
	}

	internal static bool TryHandleWildernessDuelPlayerEncounterResults(PlayerEncounter encounter, string source)
	{
		MapEvent mapEvent = GetPrivateField<MapEvent>(encounter, "_mapEvent") ?? MapEvent.PlayerMapEvent;
		if (!IsWildernessDuelMapEvent(mapEvent))
		{
			return false;
		}
		try
		{
			if (_wildernessDuelRuntime != null && !_wildernessDuelRuntime.CleanupDone)
			{
				CleanupWildernessDuelRuntime(_wildernessDuelRuntime, source);
			}
			SetPlayerEncounterState(encounter, "End");
			SetPrivateField(encounter, "_stateHandled", true);
		}
		catch (Exception ex)
		{
			Logger.Log("DuelBehavior", "[WildernessDuel][WARN] encounter result cleanup failed: " + ex.Message);
		}
		return true;
	}

	private static void SettleWildernessDuelRuntime(WildernessDuelBattleRuntime runtime, bool playerDefeated, string source)
	{
		if (runtime == null || runtime.SettlementDone)
		{
			return;
		}
		runtime.SettlementDone = true;
		runtime.PlayerDefeated = playerDefeated;
		bool playerWon = !playerDefeated;
		Hero targetHero = runtime.TargetHero;
		try
		{
			if (Instance != null && targetHero != null && !string.IsNullOrEmpty(targetHero.StringId))
			{
				Instance._lastDuelResults[targetHero.StringId] = playerWon ? 1 : -1;
			}
			_duelResultRecorded = true;
			SetDuelDebtTagGateState(targetHero, playerDefeated ? -1 : 1);
			MyBehavior.RecordDuelResultForExternal(targetHero, playerWon, "wilderness");
			TryPostDuelAiShout(targetHero, null, playerWon);
			string text = ApplyDuelStakeSettlementAndBuildResultText(targetHero, playerWon);
			string resultText = playerWon ? "[Duel Result] You won." : "[Duel Result] You lost.";
			AnimusForgeQuickInfo.Show(resultText + text + " Returning to campaign map...", targetHero?.CharacterObject);
			LogWildernessDuelDiagnostic("vanilla_behavior.settled source=" + source + " playerWon=" + playerWon, runtime.DiagnosticId, targetHero);
		}
		catch (Exception ex)
		{
			Logger.Log("DuelBehavior", "[WildernessDuel][ERROR] settle failed: " + ex);
		}
	}

	private static bool RosterContainsCharacter(TroopRoster roster, CharacterObject character)
	{
		if (roster == null || character == null)
		{
			return false;
		}
		try
		{
			foreach (TroopRosterElement element in roster.GetTroopRoster())
			{
				if (element.Character == character && element.Number > 0)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static void CleanupWildernessDuelRuntime(WildernessDuelBattleRuntime runtime, string source)
	{
		if (runtime == null || runtime.CleanupDone)
		{
			return;
		}
		runtime.CleanupDone = true;
		try
		{
			Hero target = runtime.TargetHero;
			MobileParty dummy = runtime.OpponentDummyParty;
			if (target?.CharacterObject != null)
			{
				if (runtime.TargetOriginalParty != null && runtime.TargetOriginalParty.IsActive)
				{
					try
					{
						if (target.PartyBelongedTo != runtime.TargetOriginalParty)
						{
							AddHeroToPartyAction.Apply(target, runtime.TargetOriginalParty, showNotification: false);
							Logger.Log("DuelBehavior", "[WildernessDuel] restored target hero to original party id=" + runtime.TargetOriginalParty.StringId + ", target=" + target.StringId);
						}
						else if (!RosterContainsCharacter(runtime.TargetOriginalParty.MemberRoster, target.CharacterObject))
						{
							runtime.TargetOriginalParty.MemberRoster.AddToCounts(target.CharacterObject, 1, insertAtFront: true, woundedCount: 0, xpChange: 0, removeDepleted: true, index: -1);
							Logger.Log("DuelBehavior", "[WildernessDuel] restored missing target roster entry id=" + runtime.TargetOriginalParty.StringId + ", target=" + target.StringId);
						}
						if (runtime.TargetWasOriginalLeader && runtime.TargetOriginalParty.PartyComponent != null)
						{
							runtime.TargetOriginalParty.PartyComponent.ChangePartyLeader(target);
						}
					}
					catch (Exception ex)
					{
						Logger.Log("DuelBehavior", "[WildernessDuel][WARN] restore target to original party failed: " + ex.Message);
					}
				}
				if (dummy != null)
				{
					try
					{
						dummy.MemberRoster.RemoveTroop(target.CharacterObject, 1);
					}
					catch
					{
					}
				}
			}
			if (dummy != null && dummy.IsActive && (dummy.StringId ?? "").StartsWith(WildernessDuelDummyPartyPrefix, StringComparison.Ordinal))
			{
				try
				{
					DestroyPartyAction.Apply(null, dummy);
				}
				catch (Exception ex)
				{
					Logger.Log("DuelBehavior", "[WildernessDuel][WARN] destroy dummy failed: " + ex.Message);
				}
			}
			if (ReferenceEquals(_wildernessDuelRuntime, runtime))
			{
				_wildernessDuelRuntime = null;
			}
			if (Instance != null)
			{
				Instance._isDuelActive = false;
				Instance._targetHero = null;
			}
			try
			{
				PlayerEncounter current = PlayerEncounter.Current;
				if (current != null && IsWildernessDuelMapEvent(GetPrivateField<MapEvent>(current, "_mapEvent")))
				{
					PlayerEncounter.LeaveEncounter = true;
					current.IsPlayerWaiting = false;
					SetPlayerEncounterState(current, "End");
					SetPrivateField(current, "_stateHandled", true);
				}
			}
			catch
			{
			}
			Logger.Log("DuelBehavior", "[WildernessDuel] cleanup source=" + source);
		}
		catch (Exception ex)
		{
			Logger.Log("DuelBehavior", "[WildernessDuel][ERROR] cleanup failed: " + ex);
		}
	}

	private bool TryOpenWildernessDuelMission(Hero target)
	{
		try
		{
			int diagnosticId = ++_wildernessDuelDiagnosticSerial;
			_wildernessDuelActiveDiagnosticId = diagnosticId;
			_wildernessDuelOpenStartedUtcTicks = DateTime.UtcNow.Ticks;
			_wildernessDuelLastOpeningDiagUtcTicks = 0L;
			LogWildernessDuelDiagnostic("open.enter", diagnosticId, target);
			if (!IsWildernessDuelContext(target, out string blockedReason))
			{
				Logger.Log("DuelBehavior", "[WildernessDuel] blocked: " + blockedReason);
				LogWildernessDuelDiagnostic("open.blocked " + blockedReason, diagnosticId, target);
				return false;
			}
			if (IsCampaignConversationActive())
			{
				Logger.Log("DuelBehavior", "[WildernessDuel][Queue] Open requested while conversation is still active; queued instead of opening synchronously.");
				LogWildernessDuelDiagnostic("open.requeue_conversation_active", diagnosticId, target);
				QueueDuelAfterConversationExit(target, 0f, wildernessDuel: true);
				return true;
			}
			CleanupWildernessDuelRuntime(_wildernessDuelRuntime, "open.new_request_cleanup");
			WildernessDuelBattleRuntime runtime = CreateWildernessDuelRuntime(target, diagnosticId);
			MissionInitializerRecord rec = BuildWildernessDuelMissionInitializerRecord(target);
			_wildernessDuelLastOpenScene = rec.SceneName ?? "";
			LogWildernessDuelDiagnostic("initializer.ready", diagnosticId, target, rec);
			_arenaMissionActive = true;
			_arenaMissionLeaveRequested = false;
			_arenaMissionLeaveReadyTime = 0f;
			_arenaMissionStartedOnce = false;
			_arenaMissionOpeningGraceUntilUtcTicks = DateTime.UtcNow.AddSeconds(120.0).Ticks;
			_returnToMapAfterIndependentDuel = true;
			_duelResultRecorded = false;
			_forcedMainHeroDeath = false;
			_pendingMainHeroDeath = false;
			_pendingMainHeroDeathKiller = null;
			if (Instance != null)
			{
				Instance._targetHero = target;
				Instance._isDuelActive = true;
				Instance._currentDuelIsArena = false;
			}
			Logger.Log("DuelBehavior", "[WildernessDuel] OpenBattleMission vanilla-path scene=" + rec.SceneName + ", terrain=" + rec.TerrainType + ", target=" + target?.StringId);
			LogWildernessDuelDiagnostic("OpenBattleMission.before", diagnosticId, target, rec);
			IMission openedMission = CampaignMission.OpenBattleMission(rec);
			Mission mission = openedMission as Mission;
			if (mission == null)
			{
				throw new InvalidOperationException("CampaignMission.OpenBattleMission returned non-Mission.");
			}
			PlayerEncounter.StartAttackMission();
			MapEvent.PlayerMapEvent?.BeginWait();
			mission.AddMissionBehavior(new WildernessDuelBattleMissionLogic(runtime));
			mission.AddMissionBehavior(new DuelPlayerDeathAgentStateDeciderLogic());
			mission.AddMissionBehavior(new DuelMainHeroDeathMissionBehavior());
			LogWildernessDuelDiagnostic("OpenBattleMission.after returned=" + (mission.SceneName ?? "null"), diagnosticId, target, rec);
			return true;
		}
		catch (Exception ex)
		{
			CleanupWildernessDuelRuntime(_wildernessDuelRuntime, "open.error");
			ResetWildernessDuelOpeningState();
			Logger.Log("DuelBehavior", "[WildernessDuel][ERROR] " + ex.ToString());
			LogWildernessDuelDiagnostic("open.error " + ex.GetType().Name + ": " + ex.Message, _wildernessDuelActiveDiagnosticId, target);
			return false;
		}
	}

	private static void FinishPlayerEncounterForWildernessDuelOpening(int diagnosticId, Hero target, MissionInitializerRecord rec)
	{
		try
		{
			if (IsCampaignConversationActive())
			{
				try
				{
					Campaign.Current?.ConversationManager?.EndConversation();
				}
				catch
				{
				}
			}
			if (PlayerEncounter.Current == null)
			{
				LogWildernessDuelDiagnostic("OpenNew.after_encounter_already_clear", diagnosticId, target, rec);
				return;
			}
			PlayerEncounter.LeaveEncounter = true;
			try
			{
				PlayerEncounter.Current.IsPlayerWaiting = false;
			}
			catch
			{
			}
			PlayerEncounter.Finish(forcePlayerOutFromSettlement: true);
			Logger.Log("DuelBehavior", "[WildernessDuel] PlayerEncounter finished after OpenNew for independent duel.");
			LogWildernessDuelDiagnostic("OpenNew.after_encounter_finished", diagnosticId, target, rec);
		}
		catch (Exception ex)
		{
			Logger.Log("DuelBehavior", "[WildernessDuel][WARN] finish encounter after OpenNew failed: " + ex.Message);
			LogWildernessDuelDiagnostic("OpenNew.after_encounter_finish_error " + ex.GetType().Name + ": " + ex.Message, diagnosticId, target, rec);
		}
	}

	private static void TryReturnToMapAfterIndependentDuel()
	{
		if (!_returnToMapAfterIndependentDuel)
		{
			return;
		}
		_returnToMapAfterIndependentDuel = false;
		if (IsCampaignConversationActive())
		{
			try
			{
				Campaign.Current?.ConversationManager?.EndConversation();
			}
			catch
			{
			}
		}
		try
		{
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.LeaveEncounter = true;
				try
				{
					PlayerEncounter.Current.IsPlayerWaiting = false;
				}
				catch
				{
				}
				PlayerEncounter.Finish(forcePlayerOutFromSettlement: true);
				Logger.Log("DuelBehavior", "[WildernessDuel] PlayerEncounter finished after independent duel.");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("DuelBehavior", "[WildernessDuel][WARN] finish encounter failed: " + ex.Message);
		}
	}

	public static void GlobalDuelStarterTick()
	{
		if (_queuedArenaDuelTarget == null || Mission.Current != null)
		{
			return;
		}
		try
		{
			long nowTicks = DateTime.UtcNow.Ticks;
			if (_queuedDuelReadyUtcTicks > nowTicks)
			{
				return;
			}
			if (_queuedDuelWaitingForConversationExit)
			{
				if (IsCampaignConversationActive())
				{
					_queuedDuelConversationCloseAttempts++;
					try
					{
						Campaign.Current?.ConversationManager?.EndConversation();
						Logger.Log("DuelBehavior", "[Queue] Requested campaign conversation end before queued duel. attempt=" + _queuedDuelConversationCloseAttempts);
					}
					catch (Exception ex)
					{
						Logger.Log("DuelBehavior", "[Queue][WARN] EndConversation before queued duel failed: " + ex.Message);
					}
					if (_queuedDuelConversationCloseAttempts >= 20)
					{
						Logger.Log("DuelBehavior", "[Queue][ERROR] Campaign conversation did not close before queued duel; aborting queued duel.");
						_queuedArenaDuelTarget = null;
						_queuedWildernessDuel = false;
						_queuedDuelWaitingForConversationExit = false;
						_queuedDuelReadyUtcTicks = 0L;
						_queuedDuelConversationCloseAttempts = 0;
						return;
					}
					_queuedDuelReadyUtcTicks = DateTime.UtcNow.AddMilliseconds(500.0).Ticks;
					return;
				}
				_queuedDuelWaitingForConversationExit = false;
				_queuedDuelConversationCloseAttempts = 0;
				_queuedDuelReadyUtcTicks = DateTime.UtcNow.AddMilliseconds(750.0).Ticks;
				Logger.Log("DuelBehavior", "[Queue] Campaign conversation exited; waiting briefly before opening queued duel.");
				return;
			}
			Hero queuedArenaDuelTarget = _queuedArenaDuelTarget;
			_queuedArenaDuelTarget = null;
			bool queuedWildernessDuel = _queuedWildernessDuel;
			_queuedWildernessDuel = false;
			_queuedDuelWaitingForConversationExit = false;
			_queuedDuelReadyUtcTicks = 0L;
			_queuedDuelConversationCloseAttempts = 0;
			Logger.Log("DuelBehavior", $"[Queue] 监测到 Mission 已退出，正在启动排队的竞技场决斗: Target={queuedArenaDuelTarget.Name}");
			if (queuedWildernessDuel)
			{
				LogWildernessDuelDiagnostic("queue.open_ready", _wildernessDuelActiveDiagnosticId, queuedArenaDuelTarget);
			}
			if (Instance != null)
			{
				if (queuedWildernessDuel)
				{
					Instance.TryOpenWildernessDuelMission(queuedArenaDuelTarget);
				}
				else
				{
					Instance.TryTeleportToArenaForDuel(queuedArenaDuelTarget);
				}
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
			Dictionary<string, int> stakeItems = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			int playerStakeGold = 0;
			Dictionary<string, int> playerStakeItems = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			int npcStakeGold = 0;
			Dictionary<string, int> npcStakeItems = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			bool any = false;
			Regex regex = new Regex("\\[ACTION:DUEL_STAKE[_-]GOLD:(\\d+)\\]", RegexOptions.IgnoreCase);
			Regex regex2 = new Regex("\\[ACTION:DUEL_STAKE[_-]ITEM:([^:\\]\\r\\n]+):(\\d+)\\]", RegexOptions.IgnoreCase);
			Regex regex3 = new Regex("\\[ACTION:DUEL_STAKE[_-]PLAYER[_-]GOLD:(\\d+)\\]", RegexOptions.IgnoreCase);
			Regex regex4 = new Regex("\\[ACTION:DUEL_STAKE[_-]PLAYER[_-]ITEM:([^:\\]\\r\\n]+):(\\d+)\\]", RegexOptions.IgnoreCase);
			Regex regex5 = new Regex("\\[ACTION:DUEL_STAKE[_-]NPC[_-]GOLD:(\\d+)\\]", RegexOptions.IgnoreCase);
			Regex regex6 = new Regex("\\[ACTION:DUEL_STAKE[_-]NPC[_-]ITEM:([^:\\]\\r\\n]+):(\\d+)\\]", RegexOptions.IgnoreCase);
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
				string value = (m.Groups[1].Value ?? "").Trim();
				if (int.TryParse(m.Groups[2].Value, out var result2) && result2 > 0 && !string.IsNullOrEmpty(value))
				{
					AddStakeItem(stakeItems, value, result2);
				}
				return string.Empty;
			});
			responseText = regex3.Replace(responseText, delegate(Match m)
			{
				any = true;
				if (int.TryParse(m.Groups[1].Value, out var result2))
				{
					playerStakeGold = Math.Max(playerStakeGold, result2);
				}
				return string.Empty;
			});
			responseText = regex4.Replace(responseText, delegate(Match m)
			{
				any = true;
				string value = (m.Groups[1].Value ?? "").Trim();
				if (int.TryParse(m.Groups[2].Value, out var result2) && result2 > 0 && !string.IsNullOrEmpty(value))
				{
					AddStakeItem(playerStakeItems, value, result2);
				}
				return string.Empty;
			});
			responseText = regex5.Replace(responseText, delegate(Match m)
			{
				any = true;
				if (int.TryParse(m.Groups[1].Value, out var result2))
				{
					npcStakeGold = Math.Max(npcStakeGold, result2);
				}
				return string.Empty;
			});
			responseText = regex6.Replace(responseText, delegate(Match m)
			{
				any = true;
				string value = (m.Groups[1].Value ?? "").Trim();
				if (int.TryParse(m.Groups[2].Value, out var result2) && result2 > 0 && !string.IsNullOrEmpty(value))
				{
					AddStakeItem(npcStakeItems, value, result2);
				}
				return string.Empty;
			});
			if (responseText.IndexOf("[ACTION:DUEL_STAKE", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				Regex regex7 = new Regex("\\[ACTION:DUEL_STAKE[^\\]\\r\\n]*\\]?", RegexOptions.IgnoreCase);
				responseText = regex7.Replace(responseText, string.Empty);
			}
			if (!any && (responseText.Contains("赌") || responseText.Contains("赌注") || responseText.Contains("押") || responseText.Contains("压") || responseText.Contains("筹码")))
			{
				Regex regex8 = new Regex("(?:赌|赌注|押|压|筹码)[^0-9]{0,12}(\\d{1,9})\\s*第纳尔", RegexOptions.IgnoreCase);
				Match match = regex8.Match(responseText);
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
			CachePendingDuelStake(stringId, stakeGold, stakeItems, playerStakeGold, playerStakeItems, npcStakeGold, npcStakeItems);
			responseText = responseText.Trim();
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void CachePendingDuelStake(string heroId, int gold, Dictionary<string, int> items, int playerGold, Dictionary<string, int> playerItems, int npcGold, Dictionary<string, int> npcItems)
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
				value.Gold = Math.Max(value.Gold, gold);
			}
			if (HasStakeItems(items))
			{
				value.Items = CloneStakeItems(items);
			}
			if (playerGold > 0)
			{
				value.PlayerGold = Math.Max(value.PlayerGold, playerGold);
			}
			if (HasStakeItems(playerItems))
			{
				value.PlayerItems = CloneStakeItems(playerItems);
			}
			if (npcGold > 0)
			{
				value.NpcGold = Math.Max(value.NpcGold, npcGold);
			}
			if (HasStakeItems(npcItems))
			{
				value.NpcItems = CloneStakeItems(npcItems);
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

	private static void AddStakeItem(Dictionary<string, int> items, string itemId, int count)
	{
		if (items == null || string.IsNullOrWhiteSpace(itemId) || count <= 0)
		{
			return;
		}
		string text = itemId.Trim();
		if (text.Length == 0)
		{
			return;
		}
		if (!items.ContainsKey(text))
		{
			items[text] = 0;
		}
		items[text] += count;
	}

	private static bool HasStakeItems(Dictionary<string, int> items)
	{
		return items != null && items.Any((KeyValuePair<string, int> x) => !string.IsNullOrWhiteSpace(x.Key) && x.Value > 0);
	}

	private static Dictionary<string, int> CloneStakeItems(Dictionary<string, int> items)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		if (items == null)
		{
			return dictionary;
		}
		foreach (KeyValuePair<string, int> item in items)
		{
			if (!string.IsNullOrWhiteSpace(item.Key) && item.Value > 0)
			{
				dictionary[item.Key.Trim()] = item.Value;
			}
		}
		return dictionary;
	}

	private static Dictionary<string, int> SelectStakeItemsForSettlement(PendingDuelStake stake, bool playerWon)
	{
		if (stake == null)
		{
			return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (playerWon)
		{
			return HasStakeItems(stake.NpcItems) ? CloneStakeItems(stake.NpcItems) : CloneStakeItems(stake.Items);
		}
		return HasStakeItems(stake.PlayerItems) ? CloneStakeItems(stake.PlayerItems) : CloneStakeItems(stake.Items);
	}

	private static string BuildStakeSummaryText(int gold, Dictionary<string, int> items)
	{
		List<string> list = new List<string>();
		if (gold > 0)
		{
			list.Add(gold + " 第纳尔");
		}
		if (items != null)
		{
			foreach (KeyValuePair<string, int> item in items.Where((KeyValuePair<string, int> x) => !string.IsNullOrWhiteSpace(x.Key) && x.Value > 0).OrderBy((KeyValuePair<string, int> x) => x.Key, StringComparer.OrdinalIgnoreCase))
			{
				list.Add(item.Key.Trim() + " x" + item.Value);
			}
		}
		return (list.Count > 0) ? string.Join("，", list) : "（无）";
	}

	private static string ApplyDuelStakeSettlementAndBuildResultText(Hero targetHero, bool playerWon)
	{
		try
		{
			if (targetHero == null || string.IsNullOrEmpty(targetHero.StringId))
			{
				return "";
			}
			bool flag = TryConsumePendingDuelDebtTag(targetHero, out var amount, out var dueDays, out var note) && amount > 0;
			if (!TryConsumePendingDuelStake(targetHero.StringId, out var stake) || stake == null)
			{
				if (!playerWon && flag && RewardSystemBehavior.Instance != null)
				{
					string npcName2 = targetHero?.Name?.ToString() ?? "NPC";
					string playerName2 = Hero.MainHero?.Name?.ToString() ?? "玩家";
					if (RewardSystemBehavior.Instance.RecordDeferredDuelDebtForNpc(targetHero, amount, dueDays, note, out var debtId, out var dueStatusText))
					{
						string text3 = string.IsNullOrWhiteSpace(dueStatusText) ? "" : ("，" + dueStatusText);
						string text4 = string.IsNullOrWhiteSpace(note) ? "" : ("，备注：" + note);
						string text5 = string.IsNullOrWhiteSpace(debtId) ? "" : ("（债务ID:" + debtId + "）");
						MyBehavior.AppendExternalDialogueHistory(targetHero, null, null, $"你在决斗中击败了 {playerName2}，并已记下：{playerName2} 欠你 {amount} 第纳尔{text5}（决斗赌注）{text3}{text4}。");
						MyBehavior.AppendExternalDialogueHistory(Hero.MainHero, null, null, $"你在决斗中输给了 {npcName2}，欠 {npcName2} {amount} 第纳尔{text5}（决斗赌注）{text3}{text4}。");
						return $" 你在决斗中输给了{npcName2}，现在欠{npcName2} {amount} 第纳尔{text5}（决斗赌注）{text3}{text4}。";
					}
				}
				return "";
			}
			string text = targetHero?.Name?.ToString() ?? "NPC";
			string text2 = Hero.MainHero?.Name?.ToString();
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = "主角";
			}
			int num = (playerWon ? ((stake.NpcGold > 0) ? stake.NpcGold : stake.Gold) : ((stake.PlayerGold > 0) ? stake.PlayerGold : stake.Gold));
			Dictionary<string, int> dictionary = SelectStakeItemsForSettlement(stake, playerWon);
			RewardSystemBehavior instance = RewardSystemBehavior.Instance;
			if (num <= 0 && !HasStakeItems(dictionary))
			{
				return "";
			}
			if (playerWon)
			{
				TryConsumePendingDuelDebtTag(targetHero, out var _, out var _, out var _);
				if (instance == null)
				{
					return " " + text + "没有结算赌注。";
				}
				int num2 = 0;
				if (num > 0)
				{
					try
					{
						num2 = instance.TransferGold(targetHero, Hero.MainHero, num, forceComplete: true);
					}
					catch
					{
					}
				}
				List<string> list = new List<string>();
				List<string> list2 = new List<string>();
				if (num2 > 0)
				{
					list.Add(num2 + " 第纳尔");
				}
				if (num > num2)
				{
					list2.Add((num - num2) + " 第纳尔");
				}
				foreach (KeyValuePair<string, int> item in dictionary)
				{
					int num3 = Math.Max(1, item.Value);
					if (string.IsNullOrWhiteSpace(item.Key))
					{
						continue;
					}
					int num4 = 0;
					string itemName = null;
					try
					{
						num4 = instance.TransferItemById(targetHero, Hero.MainHero, item.Key, num3, out itemName, forceComplete: true);
					}
					catch
					{
					}
					string text3 = (string.IsNullOrEmpty(itemName) ? item.Key : itemName);
					if (num4 > 0)
					{
						list.Add(num4 + " 个 " + text3);
					}
					if (num3 > num4)
					{
						list2.Add(text3 + " x" + (num3 - num4));
					}
				}
				if (list.Count > 0)
				{
					string text4 = string.Join("，", list);
					MyBehavior.AppendExternalDialogueHistory(targetHero, null, null, $"你在决斗中输给了 {text2}，并已按赌注交付：{text4}。");
					MyBehavior.AppendExternalDialogueHistory(Hero.MainHero, null, null, $"你在决斗中击败了 {text}，并从 {text} 收到了 {text4}（决斗赌注）。");
					if (list2.Count > 0)
					{
						string text5 = string.Join("，", list2);
						return $" 你在决斗中击败了 {text}，并从 {text} 收到了 {text4}（决斗赌注），但对方无法支付剩余 {text5}。";
					}
					return $" 你在决斗中击败了 {text}，并从 {text} 收到了 {text4}（决斗赌注）。";
				}
				string text6 = string.Join("，", list2);
				return $" 你在决斗中击败了 {text}，但 {text}无法支付赌注 {text6}。";
			}
			if (instance != null && flag)
			{
				if (instance.RecordDeferredDuelDebtForNpc(targetHero, amount, dueDays, note, out var debtId, out var dueStatusText))
				{
					string text7 = string.IsNullOrWhiteSpace(dueStatusText) ? "" : ("，" + dueStatusText);
					string text10 = string.IsNullOrWhiteSpace(note) ? "" : ("，备注：" + note);
					string text11 = string.IsNullOrWhiteSpace(debtId) ? "" : ("（债务ID:" + debtId + "）");
					MyBehavior.AppendExternalDialogueHistory(targetHero, null, null, $"你在决斗中击败了 {text2}，并已记下：{text2} 欠你 {amount} 第纳尔{text11}（决斗赌注）{text7}{text10}。");
					MyBehavior.AppendExternalDialogueHistory(Hero.MainHero, null, null, $"你在决斗中输给了 {text}，欠 {text} {amount} 第纳尔{text11}（决斗赌注）{text7}{text10}。");
					return $" 你在决斗中输给了{text}，现在欠{text} {amount} 第纳尔{text11}（决斗赌注）{text7}{text10}。";
				}
			}
			string text8 = BuildStakeSummaryText(num, dictionary);
			MyBehavior.AppendExternalDialogueHistory(targetHero, null, null, $"你在决斗中击败了 {text2}，并已记下：{text2} 欠你 {text8}（决斗赌注）。");
			MyBehavior.AppendExternalDialogueHistory(Hero.MainHero, null, null, $"你在决斗中输给了 {text}，欠 {text} {text8}（决斗赌注）。");
			return $" 你在决斗中输给了{text}，现在欠{text} {text8}（决斗赌注）。";
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
				try
				{
					ShoutBehavior.RecordNativeConversationNpcLineForExternal(targetHero, targetHero?.CharacterObject, text3, text);
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
			AnimusForgeQuickInfo.Show("已接受决斗。请你手动结束对话；结束后将进入 10 秒准备期，然后正式开战（准备期双方无法互相伤害）。", target?.CharacterObject);
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
				AnimusForgeQuickInfo.Show(information, _targetHero?.CharacterObject);
			}
			if (main.State == AgentState.Unconscious)
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
				return;
			}
			if (!main.IsActive() || main.State == AgentState.Killed)
			{
				Logger.Log("DuelBehavior", $"判定: 玩家战败 (State={main.State})");
				EndDuel(playerDefeated: true);
				return;
			}
			if (!agent.IsActive() || agent.State == AgentState.Killed || agent.State == AgentState.Unconscious || agent.Health <= 0f)
			{
				Logger.Log("DuelBehavior", $"判定: 玩家获胜 (State={agent.State}, Active={agent.IsActive()}, HP={agent.Health:0.0})");
				EndDuel(playerDefeated: false);
				return;
			}
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

	private void KeepFormalDuelTargetFocusedOnPlayer(Agent targetAgent)
	{
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
					targetAgent.SetIsAIPaused(isPaused: false);
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
			try
			{
				targetAgent.SetTargetPosition(main.Position.AsVec2);
			}
			catch
			{
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
			try
			{
				Agent mountAgent = targetAgent.MountAgent;
				if (mountAgent != null && mountAgent.IsActive())
				{
					mountAgent.ResetEnemyCaches();
					mountAgent.InvalidateTargetAgent();
					mountAgent.ClearTargetFrame();
					mountAgent.SetTargetPosition(main.Position.AsVec2);
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
			if (_currentDuelIsArena || Mission.Current == null || playerAgent == null || targetAgent == null)
			{
				return;
			}
			if (Mission.Current.CurrentTime < _formalDuelSpectatorRefreshTimer)
			{
				return;
			}
			_formalDuelSpectatorRefreshTimer = Mission.Current.CurrentTime + 0.2f;
			Team team = playerAgent.Team;
			Team team2 = targetAgent.Team;
			Agent mountAgent = playerAgent.MountAgent;
			Agent mountAgent2 = targetAgent.MountAgent;
			foreach (Agent agent in Mission.Current.Agents)
			{
				if (agent == null || !agent.IsActive() || agent == playerAgent || agent == targetAgent || agent == mountAgent || agent == mountAgent2)
				{
					continue;
				}
				if (agent.Team != team && agent.Team != team2)
				{
					continue;
				}
				_formalDuelSpectatorAgentIndices.Add(agent.Index);
				TrySetAgentController(agent, "None");
				try
				{
					if (agent.IsAIControlled)
					{
						agent.SetIsAIPaused(isPaused: true);
					}
				}
				catch
				{
				}
				try
				{
					agent.ResetEnemyCaches();
					agent.InvalidateTargetAgent();
					agent.InvalidateAIWeaponSelections();
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
					agent.SetWatchState(Agent.WatchState.Patrolling);
				}
				catch
				{
				}
				UnlockAgentMovement(agent, unpauseAi: false, clearTargetFrame: true);
				TrySheathWeapons(agent);
				try
				{
					Agent mountAgent3 = agent.MountAgent;
					if (mountAgent3 != null && mountAgent3.IsActive())
					{
						TrySetAgentController(mountAgent3, "None");
						mountAgent3.SetIsAIPaused(isPaused: true);
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
				Agent agent = Mission.Current.Agents.FirstOrDefault((Agent a) => a != null && a.Index == formalDuelSpectatorAgentIndex);
				if (agent == null || !agent.IsActive())
				{
					continue;
				}
				TrySetAgentController(agent, "AI");
				try
				{
					if (agent.IsAIControlled)
					{
						agent.SetIsAIPaused(isPaused: false);
					}
				}
				catch
				{
				}
				try
				{
					agent.SetWatchState(Agent.WatchState.Patrolling);
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
						mountAgent.SetIsAIPaused(isPaused: false);
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
			agent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
		}
		catch
		{
		}
		try
		{
			agent.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.Instant);
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
		uint color = Hero.MainHero?.MapFaction?.Color ?? 4278190335u;
		uint color2 = Hero.MainHero?.MapFaction?.Color2 ?? 4278190208u;
		Banner banner = Hero.MainHero?.Clan?.Banner;
		uint color3 = (_targetHero?.MapFaction?.Color ?? 4294901760u);
		uint color4 = (_targetHero?.MapFaction?.Color2 ?? 4286578688u);
		Banner banner2 = _targetHero?.Clan?.Banner;
		try
		{
			_duelPlayerTeam = current.Teams.Add(BattleSideEnum.Attacker, color, color2, banner, isPlayerGeneral: true, isPlayerSergeant: false);
			_duelEnemyTeam = current.Teams.Add(BattleSideEnum.Defender, color3, color4, banner2, isPlayerGeneral: false, isPlayerSergeant: true);
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
			Agent.Main.SetTeam(_duelPlayerTeam, sync: true);
		}
		catch
		{
		}
		try
		{
			Agent mainMount = Agent.Main.MountAgent;
			if (mainMount != null && mainMount.IsActive())
			{
				mainMount.SetTeam(_duelPlayerTeam, sync: true);
			}
		}
		catch
		{
		}
		try
		{
			targetAgent.SetTeam(_duelEnemyTeam, sync: true);
		}
		catch
		{
		}
		try
		{
			Agent targetMount = targetAgent.MountAgent;
			if (targetMount != null && targetMount.IsActive())
			{
				targetMount.SetTeam(_duelEnemyTeam, sync: true);
			}
		}
		catch
		{
		}
		try
		{
			foreach (Team team in current.Teams)
			{
				if (team == null || team == _duelPlayerTeam || team == _duelEnemyTeam)
				{
					continue;
				}
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
			_duelEnemyTeam.SetIsEnemyOf(_duelPlayerTeam, isEnemyOf: true);
			_duelPlayerTeam.SetIsEnemyOf(_duelEnemyTeam, isEnemyOf: true);
		}
		catch
		{
		}
		Logger.Log("DuelBehavior", "[FormalDuel] 已创建临时决斗队伍，非决斗参与者与双方中立。");
		return true;
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
				AnimusForgeQuickInfo.Show("【警告!决斗具有较大风险，你有可能在决斗中死亡】");
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
			SetDuelDebtTagGateState(_targetHero, 0);
			_forcedMainHeroDeath = false;
			EnsureDeathBehaviorsPresent();
			_preDuelTargetTeam = agent.Team;
			_preDuelPlayerTeam = Agent.Main?.Team ?? current.PlayerTeam;
			_preDuelPlayerMountTeam = Agent.Main?.MountAgent?.Team;
			_preDuelTargetMountTeam = agent.MountAgent?.Team;
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
							team = null;
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
							try
							{
								Agent mountAgent = agent.MountAgent;
								if (mountAgent != null && mountAgent.IsActive())
								{
									mountAgent.SetTeam(_duelEnemyTeam, sync: true);
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
						foreach (Team item in current.Teams)
						{
							if (item != null && item != _duelPlayerTeam && item != _duelEnemyTeam)
							{
								try
								{
									item.SetIsEnemyOf(_duelPlayerTeam, isEnemyOf: false);
								}
								catch
								{
								}
								try
								{
									_duelPlayerTeam.SetIsEnemyOf(item, isEnemyOf: false);
								}
								catch
								{
								}
								try
								{
									item.SetIsEnemyOf(_duelEnemyTeam, isEnemyOf: false);
								}
								catch
								{
								}
								try
								{
									_duelEnemyTeam.SetIsEnemyOf(item, isEnemyOf: false);
								}
								catch
								{
								}
							}
						}
						_duelEnemyTeam.SetIsEnemyOf(_duelPlayerTeam, isEnemyOf: true);
						_duelPlayerTeam.SetIsEnemyOf(_duelEnemyTeam, isEnemyOf: true);
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
					AnimusForgeQuickInfo.Show("双方就位！10秒后正式开战（此期间无法互相伤害）", target?.CharacterObject);
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
				AnimusForgeQuickInfo.Show(information, _targetHero?.CharacterObject);
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
			MissionState.OpenNew("AnimusForge_ArenaDuel", rec, (Mission mission) => new MissionBehavior[4]
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
				long nowTicks = DateTime.UtcNow.Ticks;
				if (!_arenaMissionStartedOnce)
				{
					if (_arenaMissionOpeningGraceUntilUtcTicks > nowTicks)
					{
						if (_wildernessDuelActiveDiagnosticId > 0 && (nowTicks - _wildernessDuelLastOpeningDiagUtcTicks) > TimeSpan.FromSeconds(5.0).Ticks)
						{
							_wildernessDuelLastOpeningDiagUtcTicks = nowTicks;
							double elapsedSeconds = (_wildernessDuelOpenStartedUtcTicks > 0L) ? TimeSpan.FromTicks(nowTicks - _wildernessDuelOpenStartedUtcTicks).TotalSeconds : 0.0;
							LogWildernessDuelDiagnostic("opening.wait elapsed=" + elapsedSeconds.ToString("0.0") + "s", _wildernessDuelActiveDiagnosticId);
						}
						return;
					}
					LogWildernessDuelDiagnostic("opening.timeout_before_afterstart", _wildernessDuelActiveDiagnosticId);
					CleanupWildernessDuelRuntime(_wildernessDuelRuntime, "opening.timeout_before_afterstart");
					TryReturnToMapAfterIndependentDuel();
					_arenaMissionActive = false;
					_arenaMissionLeaveRequested = false;
					_arenaMissionOpeningGraceUntilUtcTicks = 0L;
					_arenaMissionStartedOnce = false;
					_returnToMapAfterIndependentDuel = false;
					_wildernessDuelActiveDiagnosticId = 0;
					_wildernessDuelOpenStartedUtcTicks = 0L;
					_wildernessDuelLastOpeningDiagUtcTicks = 0L;
					Logger.Log("ArenaDuel", "[Cleanup][WARN] Arena duel mission opening timed out before AfterStart; reset opening state only.");
					return;
				}
				_arenaMissionActive = false;
				_arenaMissionLeaveRequested = false;
				_arenaMissionOpeningGraceUntilUtcTicks = 0L;
				_arenaMissionStartedOnce = false;
				_wildernessDuelActiveDiagnosticId = 0;
				_wildernessDuelOpenStartedUtcTicks = 0L;
				_wildernessDuelLastOpeningDiagUtcTicks = 0L;
				TryReturnToMapAfterIndependentDuel();
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
			if (!_leaveSourceMissionRequested)
			{
				return;
			}
			Mission current = Mission.Current;
			if (current == null)
			{
				_leaveSourceMissionRequested = false;
				return;
			}
			if (current.CurrentTime < _leaveSourceMissionReadyTime || current.Mode == MissionMode.Conversation || IsCampaignConversationActive())
			{
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
		SetDuelDebtTagGateState(_targetHero, playerDefeated ? -1 : 1);
		MyBehavior.RecordDuelResultForExternal(_targetHero, flag, _currentDuelIsArena ? "arena" : "meeting");
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
		AnimusForgeQuickInfo.Show(text2 + text + text3, _targetHero?.CharacterObject);
	}

	private void RestoreState()
	{
		bool flag = false;
		try
		{
			flag = LordEncounterBehavior.IsEncounterMeetingMissionActive || MeetingBattleRuntime.IsMeetingActive;
		}
		catch
		{
			flag = false;
		}
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
					agent.SetTeam(_preDuelTargetTeam, sync: true);
				}
			}
			catch
			{
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
					try
					{
						if (_preDuelTargetMountTeam != null)
						{
							mountAgent.SetTeam(_preDuelTargetMountTeam, sync: true);
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
				Agent mountAgent3 = Agent.Main.MountAgent;
				if (mountAgent3 != null && mountAgent3.IsActive())
				{
					UnlockAgentMovement(mountAgent3, unpauseAi: true, clearTargetFrame: true);
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
			try
			{
				Mission.Current.SetMissionMode(flag ? MissionMode.Battle : _preDuelMode, atStart: true);
			}
			catch
			{
			}
		}
		MeetingBattleLockMissionBehavior.RestoreFormalDuelIsolationForCurrentMeeting("duel_behavior_restore_state");
		_preDuelPlayerTeam = null;
		_preDuelTargetTeam = null;
		_preDuelPlayerMountTeam = null;
		_preDuelTargetMountTeam = null;
	}

	private void FinishDuel()
	{
		_isDuelActive = false;
		Logger.Log("DuelBehavior", "决斗流程彻底结束 (FinishDuel)。");
		AnimusForgeQuickInfo.Show("决斗已结束。");
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

	public sealed class DuelBehaviorSaveableTypeDefiner : SaveableTypeDefiner
	{
		public DuelBehaviorSaveableTypeDefiner()
			: base(711070)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(WildernessDuelDummyPartyComponent), 1);
		}
	}

	private sealed class WildernessDuelDummyPartyComponent : PartyComponent
	{
		private readonly CampaignVec2 _position;

		private readonly TextObject _name;

		private Hero _owner;

		private Clan _clan;

		private Hero _leader;

		public WildernessDuelDummyPartyComponent(CampaignVec2 position, TextObject name, Hero owner, Clan clan)
		{
			_position = position;
			_name = name;
			_owner = owner;
			_clan = clan;
			_leader = owner;
		}

		public override Hero PartyOwner => _owner;

		public override Hero Leader => _leader;

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

		protected override void OnChangePartyLeader(Hero newLeader)
		{
			_leader = newLeader;
			if (newLeader != null)
			{
				_owner = newLeader;
				_clan = newLeader.Clan ?? _clan;
				if (MobileParty != null)
				{
					MobileParty.ActualClan = _clan;
				}
			}
		}
	}
}

[HarmonyPatch(typeof(MapEvent), "CalculateAndCommitMapEventResults")]
public static class WildernessDuelMapEventResultsPatch
{
	public static bool Prefix(MapEvent __instance)
	{
		try
		{
			return !DuelBehavior.TryHandleWildernessDuelMapEventResults(__instance, "MapEvent.CalculateAndCommitMapEventResults");
		}
		catch (Exception ex)
		{
			Logger.Log("DuelBehavior", "[WildernessDuel][PatchError] map_event_results " + ex.GetType().Name + ": " + ex.Message);
			return true;
		}
	}
}

[HarmonyPatch(typeof(PlayerEncounter), "DoApplyMapEventResults")]
public static class WildernessDuelPlayerEncounterResultsPatch
{
	public static bool Prefix(PlayerEncounter __instance)
	{
		try
		{
			return !DuelBehavior.TryHandleWildernessDuelPlayerEncounterResults(__instance, "PlayerEncounter.DoApplyMapEventResults");
		}
		catch (Exception ex)
		{
			Logger.Log("DuelBehavior", "[WildernessDuel][PatchError] encounter_results " + ex.GetType().Name + ": " + ex.Message);
			return true;
		}
	}
}

[HarmonyPatch(typeof(PlayerEncounter), "DoPlayerVictory")]
public static class WildernessDuelPlayerEncounterVictoryPatch
{
	public static bool Prefix(PlayerEncounter __instance)
	{
		try
		{
			return !DuelBehavior.TryHandleWildernessDuelPlayerEncounterResults(__instance, "PlayerEncounter.DoPlayerVictory");
		}
		catch (Exception ex)
		{
			Logger.Log("DuelBehavior", "[WildernessDuel][PatchError] encounter_victory " + ex.GetType().Name + ": " + ex.Message);
			return true;
		}
	}
}

[HarmonyPatch(typeof(PlayerEncounter), "DoPlayerDefeat")]
public static class WildernessDuelPlayerEncounterDefeatPatch
{
	public static bool Prefix(PlayerEncounter __instance)
	{
		try
		{
			return !DuelBehavior.TryHandleWildernessDuelPlayerEncounterResults(__instance, "PlayerEncounter.DoPlayerDefeat");
		}
		catch (Exception ex)
		{
			Logger.Log("DuelBehavior", "[WildernessDuel][PatchError] encounter_defeat " + ex.GetType().Name + ": " + ex.Message);
			return true;
		}
	}
}

[HarmonyPatch(typeof(PlayerEncounter), "DoEnd")]
public static class WildernessDuelPlayerEncounterEndPatch
{
	public static bool Prefix(PlayerEncounter __instance)
	{
		try
		{
			return !DuelBehavior.TryHandleWildernessDuelPlayerEncounterResults(__instance, "PlayerEncounter.DoEnd");
		}
		catch (Exception ex)
		{
			Logger.Log("DuelBehavior", "[WildernessDuel][PatchError] encounter_end " + ex.GetType().Name + ": " + ex.Message);
			return true;
		}
	}
}

[HarmonyPatch(typeof(PlayerEncounter), "GetBattleRewards")]
public static class WildernessDuelBattleRewardsZeroPatch
{
#if BANNERLORD_1_4_OR_GREATER
	public static bool Prefix(
		out ExplainedNumber renownChange,
		out ExplainedNumber influenceChange,
		out ExplainedNumber moraleChange,
		out float playerEarnedLootRate,
		out Figurehead playerEarnedFigurehead)
	{
		renownChange = default;
		influenceChange = default;
		moraleChange = default;
		playerEarnedLootRate = 0f;
		playerEarnedFigurehead = null;
		try
		{
			return !DuelBehavior.ShouldZeroBattleRewardsForWildernessDuel("PlayerEncounter.GetBattleRewards");
		}
		catch (Exception ex)
		{
			Logger.Log("DuelBehavior", "[WildernessDuel][PatchError] rewards_zero " + ex.GetType().Name + ": " + ex.Message);
			return true;
		}
	}
#else
	public static bool Prefix(
		out float renownChange,
		out float influenceChange,
		out float moraleChange,
		out float goldChange,
		out float playerEarnedLootPercentage,
		out Figurehead playerEarnedFigurehead,
		ref ExplainedNumber renownExplainedNumber,
		ref ExplainedNumber influenceExplainedNumber,
		ref ExplainedNumber moraleExplainedNumber)
	{
		renownChange = 0f;
		influenceChange = 0f;
		moraleChange = 0f;
		goldChange = 0f;
		playerEarnedLootPercentage = 0f;
		playerEarnedFigurehead = null;
		try
		{
			if (!DuelBehavior.ShouldZeroBattleRewardsForWildernessDuel("PlayerEncounter.GetBattleRewards"))
			{
				return true;
			}
			renownExplainedNumber = default;
			influenceExplainedNumber = default;
			moraleExplainedNumber = default;
			return false;
		}
		catch (Exception ex)
		{
			Logger.Log("DuelBehavior", "[WildernessDuel][PatchError] rewards_zero " + ex.GetType().Name + ": " + ex.Message);
			return true;
		}
	}
#endif
}
