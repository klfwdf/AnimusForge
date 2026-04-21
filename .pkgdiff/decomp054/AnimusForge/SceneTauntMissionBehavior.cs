using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using SandBox;
using SandBox.BoardGames.MissionLogics;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace AnimusForge;

public class SceneTauntMissionBehavior : MissionBehavior
{
	private static readonly FieldInfo PlayerSideOldTeamDataField = AccessTools.Field(typeof(MissionFightHandler), "_playerSideAgentsOldTeamData");

	private static readonly FieldInfo OpponentSideOldTeamDataField = AccessTools.Field(typeof(MissionFightHandler), "_opponentSideAgentsOldTeamData");

	private static readonly FieldInfo OpponentSideAgentsField = AccessTools.Field(typeof(MissionFightHandler), "_opponentSideAgents");

	private static readonly FieldInfo FinishTimerField = AccessTools.Field(typeof(MissionFightHandler), "_finishTimer");

	private const float SceneTauntNativeFightAutoEndDelaySeconds = 3600f;

	private const float ArmedBystanderReactionRefreshIntervalSeconds = 0.5f;

	private const float ArmedBystanderReactionRadiusMeters = 20f;

	private const string FallbackSoldierWeaponId = "iron_spatha_sword_t2";

	private const float SceneTauntCrimeCapBeforeWar = 59f;

	private const float SceneTauntInitialArmedCrimeAmount = 35f;

	private const float SceneTauntPerKnockdownCrimeAmount = 20f;

	private const int SceneTauntPerKnockdownTrustPenalty = 20;

	private MissionFightHandler _fightHandler;

	private readonly HashSet<int> _playerAgentIndices = new HashSet<int>();

	private readonly HashSet<int> _opponentAgentIndices = new HashSet<int>();

	private readonly HashSet<int> _guardAgentIndices = new HashSet<int>();

	private readonly HashSet<int> _blockedAiWeaponAgentIndices = new HashSet<int>();

	private readonly HashSet<int> _armedBystanderWatcherIndices = new HashSet<int>();

	private readonly HashSet<int> _armedEscalationBehaviorFactRolledAgentIndices = new HashSet<int>();

	private readonly HashSet<int> _penalizedArmedKnockdownAgentIndices = new HashSet<int>();

	private readonly Dictionary<int, MissionEquipment> _cachedUnarmedConflictEquipment = new Dictionary<int, MissionEquipment>();

	private readonly Dictionary<Hero, bool> _sceneNotableRecentHitNonLethal = new Dictionary<Hero, bool>();

	private readonly HashSet<Hero> _sceneNotableDeferredBattleDeathCandidates = new HashSet<Hero>();

	private bool _conflictActive;

	private bool _armedConflict;

	private bool _sceneAttackReleaseSuppressed;

	private bool _playerAttackReleasePrimed;

	private ActionStage? _lastMainAgentAttackStage;

	private bool _pendingImmediateUnarmedFightEnd;

	private bool _pendingImmediateUnarmedFightEndPlayerWon;

	private bool _armedCarryoverSceneInitialized;

	private bool _armedCarryoverNoAuthoritySceneNotified;

	private bool _armedCarryoverHandledInThisMission;

	private float _lastArmedCarryoverAttemptAtMissionTime = -1f;

	private float _lastArmedBystanderReactionRefreshAtMissionTime = -1f;

	private bool _pendingPlayerUnarmedPrep;

	private float _pendingPlayerUnarmedPrepAtMissionTime = -1f;

	private bool _pendingPlayerRearmAfterArmedConflictEnd;

	private float _pendingPlayerRearmAfterArmedConflictEndAtMissionTime = -1f;

	private bool _pendingActiveUnarmedTargetFlee;

	private int _pendingActiveUnarmedTargetFleeAgentIndex = -1;

	private float _pendingActiveUnarmedTargetFleeAtMissionTime = -1f;

	private float _lastArmedEscalationAtMissionTime = -1f;

	private readonly Dictionary<int, float> _recentNeutralizedFleeingCivilianUntilMissionTime = new Dictionary<int, float>();

	private bool _armedConflictOccurredThisConflict;

	private bool _armedDefeatOutcomeHandled;

	private bool _baseConsequencesApplied;

	private bool _pendingPlayerBattleDeathAfterMission;

	private bool _pendingPlayerBattleDeathDecisionCaptured;

	private Hero _pendingPlayerBattleDeathKiller;

	private float _appliedCrimeRatingAmount;

	private bool _armedDefeatWasCriminalConflict;

	private string _activeTargetKey = "";

	private string _activeTargetName = "";

	private int _activeTargetAgentIndex = -1;

	private bool _openedAsUnarmedBrawl;

	private bool _openedFromVerbalTaunt;

	private bool _suppressSettlementConsequencesForCurrentConflict;

	private int _lastNativeCriminalConflictTargetAgentIndex = -1;

	private float _lastNativeCriminalConflictMissionTime = -999f;

	public override MissionBehaviorType BehaviorType => (MissionBehaviorType)1;

	internal bool IsConflictActive => _conflictActive;

	internal bool ShouldSuppressNativeMissionConversation()
	{
		return _conflictActive || SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement();
	}

	internal string BuildFrightenedCivilianShoutExtraFact(Agent targetAgent)
	{
		try
		{
			if (targetAgent == null || !targetAgent.IsHuman || !targetAgent.IsActive())
			{
				return "";
			}
			if ((!_conflictActive || !_armedConflict) && !SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement())
			{
				return "";
			}
			if (_playerAgentIndices.Contains(targetAgent.Index))
			{
				return "";
			}
			if (!ShouldFleeWhenArmedVictim(targetAgent))
			{
				return "";
			}
			string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "玩家";
			}
			return "[AFEF玩家行为补充] " + text + "在定居点内乱砍人，你被吓的半死。";
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Building frightened civilian shout extra fact failed: " + ex.Message);
			return "";
		}
	}

	public override void OnBehaviorInitialize()
	{
		Mission current = Mission.Current;
		_fightHandler = ((current != null) ? current.GetMissionBehavior<MissionFightHandler>() : null);
	}

	public override void OnMissionTick(float dt)
	{
		TryActivateSettlementArmedCarryover();
		TryResolveCompletedUnarmedConflictBeforeEscalation();
		TryCommitPendingImmediateUnarmedFightEnd();
		TryCommitPendingPlayerUnarmedPrep();
		TryCommitPendingPlayerRearmAfterArmedConflictEnd();
		TryCommitPendingActiveUnarmedTargetFlee();
		TryForceActiveUnarmedTargetFleeFallback();
		TryMaintainRecentlyNeutralizedFleeingCivilians();
		TryMaintainHostileUnarmedOpponentsFleeing();
		TryMaintainMainAgentArmedPresence();
		TryMaintainArmedBystanderReactions();
		TryAppendNearbyArmedEscalationBehaviorFacts();
		if (IsPlayerInteractionInputSuppressed())
		{
			_sceneAttackReleaseSuppressed = false;
			_playerAttackReleasePrimed = false;
		}
		else if (Input.IsKeyDown((InputKey)224) && Input.IsKeyDown((InputKey)225))
		{
			_sceneAttackReleaseSuppressed = true;
		}
		if (!IsPlayerInteractionInputSuppressed() && ShouldTriggerPlayerAttackRelease())
		{
			object[] array = new object[6];
			Mission current = Mission.Current;
			array[0] = ((current != null) ? new float?(current.CurrentTime) : ((float?)null));
			ICampaignMission current2 = CampaignMission.Current;
			object obj;
			if (current2 == null)
			{
				obj = null;
			}
			else
			{
				Location location = current2.Location;
				obj = ((location != null) ? location.StringId : null);
			}
			if (obj == null)
			{
				obj = "";
			}
			array[1] = ((string)obj).Trim().ToLowerInvariant();
			Settlement currentSettlement = Settlement.CurrentSettlement;
			array[2] = ((currentSettlement != null) ? ((MBObjectBase)currentSettlement).StringId : null);
			array[3] = IsAgentUsingRealWeapon(Agent.Main);
			array[4] = _conflictActive;
			array[5] = _armedConflict;
			Logger.Log("SceneTaunt", string.Format("[AttackTiming] release_triggered time={0:0.###} location={1} settlement={2} weapon={3} conflict={4} armed={5}", array));
			if (!_sceneAttackReleaseSuppressed)
			{
				if (!_conflictActive)
				{
					TryStartConflictFromFacingAttackInput();
				}
				else if (!_armedConflict)
				{
					TryHandleFacingAttackDuringUnarmedConflict();
				}
				else if (_armedConflict)
				{
					TryTauntFacingAgentDuringArmedConflict();
				}
			}
			_sceneAttackReleaseSuppressed = false;
		}
		if (_conflictActive && !_armedConflict && IsPlayerAttemptingWeaponDrawInput(Agent.Main))
		{
			EscalateToArmedConflict("player_requested_weapon_draw");
		}
		if (_conflictActive && !_armedConflict && !IsMainAgentSeated() && IsAgentUsingRealWeapon(Agent.Main))
		{
			EscalateToArmedConflict("player_drew_weapon");
		}
		UpdateMainAgentAttackReleaseTracking();
	}

	private void UpdateMainAgentAttackReleaseTracking()
	{
		_lastMainAgentAttackStage = GetMainAgentAttackStage();
	}

	private bool ShouldTriggerPlayerAttackRelease()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Invalid comparison between Unknown and I4
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Invalid comparison between Unknown and I4
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Invalid comparison between Unknown and I4
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Invalid comparison between Unknown and I4
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Invalid comparison between Unknown and I4
		ActionStage? mainAgentAttackStage = GetMainAgentAttackStage();
		if (mainAgentAttackStage == (ActionStage?)0 || (int)mainAgentAttackStage.GetValueOrDefault() == 1)
		{
			_playerAttackReleasePrimed = true;
			return false;
		}
		if ((int)mainAgentAttackStage.GetValueOrDefault() == 2 && (int)_lastMainAgentAttackStage.GetValueOrDefault() != 2 && (_playerAttackReleasePrimed || IsAgentUsingRealWeapon(Agent.Main)))
		{
			_playerAttackReleasePrimed = false;
			return true;
		}
		if ((int)mainAgentAttackStage.GetValueOrDefault() != 2 && mainAgentAttackStage != (ActionStage?)0 && (int)mainAgentAttackStage.GetValueOrDefault() != 1 && !Input.IsKeyDown((InputKey)224))
		{
			_playerAttackReleasePrimed = false;
		}
		return false;
	}

	private static ActionStage? GetMainAgentAttackStage()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (Agent.Main == null || !Agent.Main.IsActive())
			{
				return null;
			}
			return Agent.Main.GetCurrentActionStage(1);
		}
		catch
		{
			return null;
		}
	}

	private static bool IsPlayerInteractionInputSuppressed()
	{
		return IsBoardGameInteractionActive() || IsMainAgentSeated() || ShoutBehavior.IsSceneShoutInputActiveForExternal();
	}

	private static bool IsBoardGameInteractionActive()
	{
		try
		{
			Mission current = Mission.Current;
			bool? obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				MissionBoardGameLogic missionBehavior = current.GetMissionBehavior<MissionBoardGameLogic>();
				obj = ((missionBehavior != null) ? new bool?(missionBehavior.IsGameInProgress) : ((bool?)null));
			}
			bool? flag = obj;
			return flag == true;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsMainAgentSeated()
	{
		try
		{
			return Agent.Main != null && Agent.Main.IsActive() && Agent.Main.IsSitting();
		}
		catch
		{
			return false;
		}
	}

	private void TryResolveCompletedUnarmedConflictBeforeEscalation()
	{
		try
		{
			if (_conflictActive && !_armedConflict && _fightHandler != null && _fightHandler.IsThereActiveFight())
			{
				if (IsIndexedSideDefeated(_opponentAgentIndices))
				{
					_pendingImmediateUnarmedFightEnd = false;
					ClearMissionFightHandlerPendingFinishTimer();
					_fightHandler.EndFight(true);
				}
				else if (IsIndexedSideDefeated(_playerAgentIndices))
				{
					_pendingImmediateUnarmedFightEnd = false;
					ClearMissionFightHandlerPendingFinishTimer();
					_fightHandler.EndFight(false);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Resolving completed unarmed conflict before escalation failed: " + ex.Message);
		}
	}

	private void TryStartConflictFromFacingAttackInput()
	{
		try
		{
			if (_conflictActive || Mission.Current == null || Agent.Main == null || !Agent.Main.IsActive())
			{
				return;
			}
			Campaign current = Campaign.Current;
			bool? obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				ConversationManager conversationManager = current.ConversationManager;
				obj = ((conversationManager != null) ? new bool?(conversationManager.IsConversationInProgress) : ((bool?)null));
			}
			bool? flag = obj;
			if (flag != true)
			{
				List<Agent> nearbyNPCAgents = ShoutUtils.GetNearbyNPCAgents();
				Agent val = FindFacingCriminalAttackTarget(nearbyNPCAgents) ?? FindFacingPhysicalAttackTarget() ?? FindClosestEligiblePhysicalAttackTarget();
				object[] array = new object[6];
				Mission current2 = Mission.Current;
				array[0] = ((current2 != null) ? new float?(current2.CurrentTime) : ((float?)null));
				ICampaignMission current3 = CampaignMission.Current;
				object obj2;
				if (current3 == null)
				{
					obj2 = null;
				}
				else
				{
					Location location = current3.Location;
					obj2 = ((location != null) ? location.StringId : null);
				}
				if (obj2 == null)
				{
					obj2 = "";
				}
				array[1] = ((string)obj2).Trim().ToLowerInvariant();
				Settlement currentSettlement = Settlement.CurrentSettlement;
				array[2] = ((currentSettlement != null) ? ((MBObjectBase)currentSettlement).StringId : null);
				array[3] = nearbyNPCAgents?.Count ?? 0;
				array[4] = ((val == null) ? null : val.Name?.ToString()) ?? "null";
				array[5] = ((val != null) ? val.Index : (-1));
				Logger.Log("SceneTaunt", string.Format("[AttackTiming] facing_attack_scan time={0:0.###} location={1} settlement={2} nearbyCount={3} target={4} targetIndex={5}", array));
				if (val != null && val.IsActive())
				{
					TryStartConflictFromPhysicalAttack(val, IsAgentUsingRealWeapon(Agent.Main), "player_attack_release_targeting");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Starting conflict from facing attack input failed: " + ex.Message);
		}
	}

	private static Agent FindFacingPhysicalAttackTarget()
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (Mission.Current == null || Agent.Main == null || !Agent.Main.IsActive())
			{
				return null;
			}
			Vec3 position = Agent.Main.Position;
			Vec3 lookDirection = Agent.Main.LookDirection;
			Agent result = null;
			float num = -1f;
			foreach (Agent item in (List<Agent>)(object)Mission.Current.Agents)
			{
				if (item == null || item == Agent.Main || !item.IsHuman || !item.IsActive())
				{
					continue;
				}
				BasicCharacterObject character = item.Character;
				CharacterObject val = (CharacterObject)(object)((character is CharacterObject) ? character : null);
				Hero targetHero = ((val != null) ? val.HeroObject : null);
				if (!IsEligiblePhysicalAttackTarget(targetHero, val) || SceneTauntBehavior.IsChildSceneProtectedTarget(val))
				{
					continue;
				}
				Vec3 val2 = item.Position - position;
				float length = ((Vec3)(ref val2)).Length;
				if (length > 4.5f)
				{
					continue;
				}
				((Vec3)(ref val2)).Normalize();
				float num2 = Vec3.DotProduct(lookDirection, val2);
				if (!(num2 < 0.55f))
				{
					float num3 = num2 / Math.Max(0.35f, length);
					if (num3 > num)
					{
						num = num3;
						result = item;
					}
				}
			}
			return result;
		}
		catch
		{
			return null;
		}
	}

	private static Agent FindClosestEligiblePhysicalAttackTarget()
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (Mission.Current == null || Agent.Main == null || !Agent.Main.IsActive())
			{
				return null;
			}
			Vec3 position = Agent.Main.Position;
			Vec3 lookDirection = Agent.Main.LookDirection;
			Agent result = null;
			float num = float.MaxValue;
			foreach (Agent item in (List<Agent>)(object)Mission.Current.Agents)
			{
				if (item == null || item == Agent.Main || !item.IsHuman || !item.IsActive())
				{
					continue;
				}
				BasicCharacterObject character = item.Character;
				CharacterObject val = (CharacterObject)(object)((character is CharacterObject) ? character : null);
				Hero targetHero = ((val != null) ? val.HeroObject : null);
				if (!IsEligiblePhysicalAttackTarget(targetHero, val) || SceneTauntBehavior.IsChildSceneProtectedTarget(val))
				{
					continue;
				}
				Vec3 val2 = item.Position - position;
				float length = ((Vec3)(ref val2)).Length;
				if (!(length > 2.2f))
				{
					((Vec3)(ref val2)).Normalize();
					if (!(Vec3.DotProduct(lookDirection, val2) < 0.2f) && length < num)
					{
						num = length;
						result = item;
					}
				}
			}
			return result;
		}
		catch
		{
			return null;
		}
	}

	private static Agent FindFacingCriminalAttackTarget(List<Agent> nearbyAgents)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (Agent.Main == null || nearbyAgents == null || nearbyAgents.Count == 0)
			{
				return null;
			}
			Vec3 position = Agent.Main.Position;
			Vec3 lookDirection = Agent.Main.LookDirection;
			Agent result = null;
			float num = -1f;
			foreach (Agent nearbyAgent in nearbyAgents)
			{
				if (nearbyAgent == null || !nearbyAgent.IsHuman || !nearbyAgent.IsActive())
				{
					continue;
				}
				BasicCharacterObject character = nearbyAgent.Character;
				CharacterObject val = (CharacterObject)(object)((character is CharacterObject) ? character : null);
				if (!IsSettlementCriminalConflictTarget((val != null) ? val.HeroObject : null, val))
				{
					continue;
				}
				Vec3 val2 = nearbyAgent.Position - position;
				float length = ((Vec3)(ref val2)).Length;
				if (length > 3.2f)
				{
					continue;
				}
				((Vec3)(ref val2)).Normalize();
				float num2 = Vec3.DotProduct(lookDirection, val2);
				if (!(num2 < 0.9f))
				{
					float num3 = num2 / Math.Max(0.25f, length);
					if (num3 > num)
					{
						num = num3;
						result = nearbyAgent;
					}
				}
			}
			return result;
		}
		catch
		{
			return null;
		}
	}

	private void TryTauntFacingAgentDuringArmedConflict()
	{
		try
		{
			if (!_conflictActive || !_armedConflict || Mission.Current == null || Agent.Main == null || !Agent.Main.IsActive())
			{
				return;
			}
			Campaign current = Campaign.Current;
			bool? obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				ConversationManager conversationManager = current.ConversationManager;
				obj = ((conversationManager != null) ? new bool?(conversationManager.IsConversationInProgress) : ((bool?)null));
			}
			bool? flag = obj;
			if (flag == true)
			{
				return;
			}
			List<Agent> nearbyNPCAgents = ShoutUtils.GetNearbyNPCAgents();
			if (nearbyNPCAgents != null && nearbyNPCAgents.Count != 0)
			{
				Agent facingAgent = ShoutUtils.GetFacingAgent(nearbyNPCAgents);
				if (facingAgent != null && facingAgent.IsActive())
				{
					TryAddFacingAgentToArmedConflict(facingAgent, "player_attack_release_targeting_existing_armed_conflict");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Taunting facing agent during armed conflict failed: " + ex.Message);
		}
	}

	private void TryHandleFacingAttackDuringUnarmedConflict()
	{
		try
		{
			if (_conflictActive && !_armedConflict && Mission.Current != null && Agent.Main != null && Agent.Main.IsActive())
			{
				Campaign current = Campaign.Current;
				bool? obj;
				if (current == null)
				{
					obj = null;
				}
				else
				{
					ConversationManager conversationManager = current.ConversationManager;
					obj = ((conversationManager != null) ? new bool?(conversationManager.IsConversationInProgress) : ((bool?)null));
				}
				bool? flag = obj;
				if (flag != true)
				{
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Handling facing attack during unarmed conflict failed: " + ex.Message);
		}
	}

	public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon attackerWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
	{
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		if (affectedAgent != null && affectedAgent.IsHuman && affectedAgent != Agent.Main)
		{
			ShoutBehavior.InterruptAgentSpeechForCombatExternal(affectedAgent.Index, (affectorAgent == Agent.Main) ? "scene_taunt_agent_hit" : "scene_agent_hit_any_source");
		}
		if (affectorAgent != Agent.Main || affectedAgent == null || !affectedAgent.IsHuman || affectedAgent == Agent.Main)
		{
			return;
		}
		object[] array = new object[8];
		Mission current = Mission.Current;
		array[0] = ((current != null) ? new float?(current.CurrentTime) : ((float?)null));
		ICampaignMission current2 = CampaignMission.Current;
		object obj;
		if (current2 == null)
		{
			obj = null;
		}
		else
		{
			Location location = current2.Location;
			obj = ((location != null) ? location.StringId : null);
		}
		if (obj == null)
		{
			obj = "";
		}
		array[1] = ((string)obj).Trim().ToLowerInvariant();
		Settlement currentSettlement = Settlement.CurrentSettlement;
		array[2] = ((currentSettlement != null) ? ((MBObjectBase)currentSettlement).StringId : null);
		array[3] = affectedAgent.Name;
		array[4] = affectedAgent.Index;
		array[5] = IsMissionWeaponRealWeapon(attackerWeapon);
		array[6] = _conflictActive;
		array[7] = _armedConflict;
		Logger.Log("SceneTaunt", string.Format("[AttackTiming] on_agent_hit time={0:0.###} location={1} settlement={2} target={3} targetIndex={4} weapon={5} conflict={6} armed={7}", array));
		if (!_conflictActive)
		{
			TryStartConflictFromPhysicalAttack(affectedAgent, IsMissionWeaponRealWeapon(attackerWeapon), "player_physical_hit");
		}
		else if (!_armedConflict)
		{
			if (IsMissionWeaponRealWeapon(attackerWeapon))
			{
				EscalateToArmedConflict("player_attacked_with_weapon");
			}
			else
			{
				TryAddFacingAgentToUnarmedConflict(affectedAgent, "player_physical_hit_existing_unarmed_conflict");
			}
		}
	}

	public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
	{
		((MissionBehavior)this).OnScoreHit(affectedAgent, affectorAgent, attackerWeapon, isBlocked, isSiegeEngineHit, ref blow, ref collisionData, damagedHp, hitDistance, shotDifficulty);
		RememberSceneNotableHitLethality(affectedAgent, attackerWeapon, in blow, damagedHp);
		if (affectedAgent != null && affectedAgent.IsHuman && affectedAgent != Agent.Main && damagedHp > 0f)
		{
			ShoutBehavior.InterruptAgentSpeechForCombatExternal(affectedAgent.Index, (affectorAgent == Agent.Main) ? "scene_taunt_score_hit" : "scene_score_hit_any_source");
		}
		if (!(damagedHp <= 0f) && affectorAgent == Agent.Main && affectedAgent != null && affectedAgent.IsHuman && affectedAgent != Agent.Main)
		{
			object[] array = new object[10];
			Mission current = Mission.Current;
			array[0] = ((current != null) ? new float?(current.CurrentTime) : ((float?)null));
			ICampaignMission current2 = CampaignMission.Current;
			object obj;
			if (current2 == null)
			{
				obj = null;
			}
			else
			{
				Location location = current2.Location;
				obj = ((location != null) ? location.StringId : null);
			}
			if (obj == null)
			{
				obj = "";
			}
			array[1] = ((string)obj).Trim().ToLowerInvariant();
			Settlement currentSettlement = Settlement.CurrentSettlement;
			array[2] = ((currentSettlement != null) ? ((MBObjectBase)currentSettlement).StringId : null);
			array[3] = affectedAgent.Name;
			array[4] = affectedAgent.Index;
			array[5] = IsWeaponComponentRealWeapon(attackerWeapon);
			array[6] = damagedHp;
			array[7] = isBlocked;
			array[8] = _conflictActive;
			array[9] = _armedConflict;
			Logger.Log("SceneTaunt", string.Format("[AttackTiming] on_score_hit time={0:0.###} location={1} settlement={2} target={3} targetIndex={4} weapon={5} damage={6:0.##} blocked={7} conflict={8} armed={9}", array));
			if (!_conflictActive)
			{
				TryStartConflictFromPhysicalAttack(affectedAgent, IsWeaponComponentRealWeapon(attackerWeapon), "player_physical_score_hit");
			}
			else if (!_armedConflict && IsWeaponComponentRealWeapon(attackerWeapon))
			{
				EscalateToArmedConflict("player_dealt_weapon_damage");
			}
			else if (!_armedConflict)
			{
				TryAddFacingAgentToUnarmedConflict(affectedAgent, "player_physical_score_hit_existing_unarmed_conflict");
			}
		}
	}

	public unsafe override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Invalid comparison between Unknown and I4
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Invalid comparison between Unknown and I4
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Invalid comparison between Unknown and I4
		try
		{
			if (affectedAgent != null && affectedAgent.IsHuman)
			{
				ShoutBehavior.CancelAgentSpeechForRemovalExternal(affectedAgent.Index, "scene_taunt_agent_removed_" + ((object)(*(AgentState*)(&agentState))/*cast due to .constrained prefix*/).ToString());
			}
			TryQueuePendingPlayerBattleDeathOutcome(affectedAgent, affectorAgent, agentState);
			TryApplyNativeAlleyNpcKnockdownConsequences(affectedAgent, affectorAgent, agentState);
			if (!_conflictActive || affectedAgent == null || !affectedAgent.IsHuman)
			{
				return;
			}
			TryApplyArmedNpcKnockdownConsequences(affectedAgent, affectorAgent, agentState);
			BasicCharacterObject character = affectedAgent.Character;
			CharacterObject val = (CharacterObject)(object)((character is CharacterObject) ? character : null);
			Hero val2 = ((val != null) ? val.HeroObject : null);
			if (!SceneTauntBehavior.IsSceneNotableTauntTarget(val2))
			{
				return;
			}
			if (((int)agentState == 4 || (int)agentState == 3) && _sceneNotableDeferredBattleDeathCandidates.Contains(val2))
			{
				BasicCharacterObject obj = ((affectorAgent != null) ? affectorAgent.Character : null);
				BasicCharacterObject obj2 = ((obj is CharacterObject) ? obj : null);
				Hero val3 = ((obj2 != null) ? ((CharacterObject)obj2).HeroObject : null);
				if (val3 == null && affectorAgent == Agent.Main)
				{
					val3 = Hero.MainHero;
				}
				SceneTauntBehavior.MarkPendingSceneNotableBattleDeathForExternal(val2, val3, ((int)agentState == 4) ? "scene_taunt_location_kill" : "scene_taunt_location_unconscious_deathmark");
			}
			_sceneNotableDeferredBattleDeathCandidates.Remove(val2);
			_sceneNotableRecentHitNonLethal.Remove(val2);
			TryQueueImmediateUnarmedFightEndAfterAgentRemoval(affectedAgent, agentState);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Handling scene notable removal failed: " + ex.Message);
		}
	}

	private void TryApplyNativeAlleyNpcKnockdownConsequences(Agent affectedAgent, Agent affectorAgent, AgentState agentState)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (IsNativeAlleyFightKnockdownContext(affectedAgent, affectorAgent, agentState) && !_playerAgentIndices.Contains(affectedAgent.Index) && _penalizedArmedKnockdownAgentIndices.Add(affectedAgent.Index))
			{
				BasicCharacterObject character = affectedAgent.Character;
				CharacterObject victimCharacter = (CharacterObject)(object)((character is CharacterObject) ? character : null);
				ApplyPerNpcKnockdownConsequences(affectedAgent, victimCharacter, affectedAgent.Name?.ToString());
				Logger.Log("SceneTaunt", "Applied native alley criminal knockdown consequences. Victim=" + affectedAgent.Name + ", Affector=" + ((affectorAgent != null) ? affectorAgent.Name : null));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Applying native alley knockdown consequences failed: " + ex.Message);
		}
	}

	private bool IsNativeAlleyFightKnockdownContext(Agent affectedAgent, Agent affectorAgent, AgentState agentState)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Invalid comparison between Unknown and I4
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Invalid comparison between Unknown and I4
		if (_conflictActive || affectedAgent == null || !affectedAgent.IsHuman)
		{
			return false;
		}
		if ((int)agentState != 4 && (int)agentState != 3)
		{
			return false;
		}
		BasicCharacterObject character = affectedAgent.Character;
		CharacterObject val = (CharacterObject)(object)((character is CharacterObject) ? character : null);
		if (!IsSettlementCriminalConflictTarget((val != null) ? val.HeroObject : null, val))
		{
			return false;
		}
		CampaignAgentComponent component = affectedAgent.GetComponent<CampaignAgentComponent>();
		object obj;
		if (component == null)
		{
			obj = null;
		}
		else
		{
			AgentNavigator agentNavigator = component.AgentNavigator;
			obj = ((agentNavigator != null) ? agentNavigator.MemberOfAlley : null);
		}
		if (obj == null)
		{
			return false;
		}
		object obj2 = _fightHandler;
		if (obj2 == null)
		{
			Mission current = Mission.Current;
			obj2 = ((current != null) ? current.GetMissionBehavior<MissionFightHandler>() : null);
		}
		_fightHandler = (MissionFightHandler)obj2;
		if (_fightHandler == null || !_fightHandler.IsThereActiveFight())
		{
			return false;
		}
		return IsNativeAlleyPlayerSideAgent(affectorAgent);
	}

	private static bool IsNativeAlleyPlayerSideAgent(Agent agent)
	{
		if (agent == null || !agent.IsHuman)
		{
			return false;
		}
		if (agent == Agent.Main)
		{
			return true;
		}
		Agent main = Agent.Main;
		if (main == null || agent.Team == null || main.Team == null || agent.Team != main.Team)
		{
			return false;
		}
		BasicCharacterObject character = agent.Character;
		CharacterObject val = (CharacterObject)(object)((character is CharacterObject) ? character : null);
		Hero val2 = ((val != null) ? val.HeroObject : null);
		return val2 != null && !IsSettlementCriminalConflictTarget(val2, val);
	}

	protected override void OnEndMission()
	{
		ClearRuntimeState();
	}

	internal bool CanStartConflict(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		object obj = _fightHandler;
		if (obj == null)
		{
			Mission current = Mission.Current;
			obj = ((current != null) ? current.GetMissionBehavior<MissionFightHandler>() : null);
		}
		_fightHandler = (MissionFightHandler)obj;
		if (_conflictActive || Mission.Current == null || _fightHandler == null || Settlement.CurrentSettlement == null)
		{
			return false;
		}
		if (_fightHandler.IsThereActiveFight())
		{
			return false;
		}
		Agent val = ResolveTargetAgent(targetHero, targetCharacter, targetAgentIndex);
		return val != null && val.IsHuman && val.IsActive();
	}

	internal bool TryStartConflict(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string targetKey, bool fromVerbalTaunt = false, bool playerUsedWeaponOverride = false)
	{
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fd: Expected O, but got Unknown
		try
		{
			object obj = _fightHandler;
			if (obj == null)
			{
				Mission current = Mission.Current;
				obj = ((current != null) ? current.GetMissionBehavior<MissionFightHandler>() : null);
			}
			_fightHandler = (MissionFightHandler)obj;
			if (!CanStartConflict(targetHero, targetCharacter, targetAgentIndex))
			{
				return false;
			}
			Agent val = ResolveTargetAgent(targetHero, targetCharacter, targetAgentIndex);
			if (val == null)
			{
				return false;
			}
			bool flag = playerUsedWeaponOverride || IsAgentUsingRealWeapon(Agent.Main);
			bool flag2 = SceneTauntBehavior.IsSoldierSceneTauntTarget(targetCharacter);
			bool flag3 = SceneTauntBehavior.IsSceneLordTauntTarget(targetHero);
			bool flag4 = IsSettlementCriminalConflictTarget(targetHero, targetCharacter);
			if (flag4)
			{
				return TryStartNativeCriminalConflict(val, fromVerbalTaunt ? "scene_taunt_verbal_criminal_conflict" : "scene_taunt_physical_criminal_conflict");
			}
			List<Agent> list = CollectPlayerSideAgents();
			List<Agent> list2 = CollectOpponentSideAgents(val);
			List<Agent> list3 = (flag4 ? new List<Agent>() : CollectGuardAgents(list, list2));
			if (flag)
			{
				foreach (Agent item in list3)
				{
					AddUniqueAgent(list2, item);
				}
			}
			_conflictActive = true;
			_armedConflict = false;
			_armedConflictOccurredThisConflict = false;
			_armedDefeatOutcomeHandled = false;
			_baseConsequencesApplied = false;
			_appliedCrimeRatingAmount = 0f;
			_activeTargetKey = (targetKey ?? "").Trim();
			_activeTargetName = val.Name?.ToString() ?? ((targetHero == null) ? null : ((object)targetHero.Name)?.ToString()) ?? ((targetCharacter == null) ? null : ((object)((BasicCharacterObject)targetCharacter).Name)?.ToString()) ?? "NPC";
			_activeTargetAgentIndex = val.Index;
			_openedAsUnarmedBrawl = false;
			_openedFromVerbalTaunt = fromVerbalTaunt;
			_suppressSettlementConsequencesForCurrentConflict = flag4;
			_armedDefeatWasCriminalConflict = flag4;
			_playerAgentIndices.Clear();
			_opponentAgentIndices.Clear();
			_guardAgentIndices.Clear();
			_blockedAiWeaponAgentIndices.Clear();
			foreach (Agent item2 in list)
			{
				_playerAgentIndices.Add(item2.Index);
			}
			foreach (Agent item3 in list2)
			{
				_opponentAgentIndices.Add(item3.Index);
			}
			foreach (Agent item4 in list3)
			{
				_guardAgentIndices.Add(item4.Index);
			}
			_fightHandler.StartCustomFight(list, list2, false, false, new OnFightEndDelegate(OnConflictFinished), float.Epsilon);
			ApplyBaseConsequences(targetCharacter, (flag || flag2 || flag3) ? 35f : 5f);
			bool suppressAnnouncement = SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement() && _armedCarryoverHandledInThisMission;
			if (flag3)
			{
				ApplyLordSceneFightConsequences(targetHero);
			}
			if (flag3)
			{
				EscalateToArmedConflict("taunted_lord_scene", suppressAnnouncement);
			}
			else if (flag2)
			{
				EscalateToArmedConflict("taunted_soldier", suppressAnnouncement);
			}
			else if (flag)
			{
				EscalateToArmedConflict("player_already_wielding", suppressAnnouncement);
			}
			else
			{
				PrepareUnarmedConflict();
				if (fromVerbalTaunt)
				{
					TryAppendNpcBehaviorFactForVerbalConflict(targetAgentIndex);
				}
				else
				{
					TryAppendPlayerBehaviorFactForOpenedBrawl(targetHero, targetCharacter, targetAgentIndex);
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "TryStartConflict failed: " + ex.Message);
			ClearRuntimeState();
			return false;
		}
	}

	private bool TryStartConflictFromPhysicalAttack(Agent targetAgent, bool playerUsedWeapon, string reason)
	{
		try
		{
			object[] array = new object[9];
			Mission current = Mission.Current;
			array[0] = ((current != null) ? new float?(current.CurrentTime) : ((float?)null));
			ICampaignMission current2 = CampaignMission.Current;
			object obj;
			if (current2 == null)
			{
				obj = null;
			}
			else
			{
				Location location = current2.Location;
				obj = ((location != null) ? location.StringId : null);
			}
			if (obj == null)
			{
				obj = "";
			}
			array[1] = ((string)obj).Trim().ToLowerInvariant();
			Settlement currentSettlement = Settlement.CurrentSettlement;
			array[2] = ((currentSettlement != null) ? ((MBObjectBase)currentSettlement).StringId : null);
			array[3] = reason;
			array[4] = ((targetAgent == null) ? null : targetAgent.Name?.ToString()) ?? "null";
			array[5] = ((targetAgent != null) ? targetAgent.Index : (-1));
			array[6] = playerUsedWeapon;
			array[7] = _conflictActive;
			array[8] = _armedConflict;
			Logger.Log("SceneTaunt", string.Format("[AttackTiming] try_start_conflict_from_physical_attack time={0:0.###} location={1} settlement={2} reason={3} target={4} targetIndex={5} playerUsedWeapon={6} conflict={7} armed={8}", array));
			if (_conflictActive || targetAgent == null || !targetAgent.IsHuman || !targetAgent.IsActive())
			{
				return false;
			}
			BasicCharacterObject character = targetAgent.Character;
			CharacterObject val = (CharacterObject)(object)((character is CharacterObject) ? character : null);
			Hero targetHero = ((val != null) ? val.HeroObject : null);
			if (!IsEligiblePhysicalAttackTarget(targetHero, val))
			{
				return false;
			}
			if (IsSettlementCriminalConflictTarget(targetHero, val))
			{
				if (ShouldSuppressDuplicateNativeCriminalConflict(targetAgent))
				{
					Logger.Log("SceneTaunt", $"Skipped duplicate native criminal conflict redirect. Reason={reason}, Target={targetAgent.Name}, AgentIndex={targetAgent.Index}");
					return true;
				}
				try
				{
					Campaign current3 = Campaign.Current;
					if (current3 != null)
					{
						ConversationManager conversationManager = current3.ConversationManager;
						if (conversationManager != null)
						{
							conversationManager.EndConversation();
						}
					}
				}
				catch
				{
				}
				bool flag = TryStartNativeCriminalConflict(targetAgent, reason + "_native_alley");
				if (flag)
				{
					RememberNativeCriminalConflictTarget(targetAgent);
					Logger.Log("SceneTaunt", $"Physical attack bypassed custom scene conflict and redirected to native criminal conflict. Reason={reason}, Target={targetAgent.Name}, UsedWeapon={playerUsedWeapon}");
				}
				return flag;
			}
			try
			{
				Campaign current4 = Campaign.Current;
				if (current4 != null)
				{
					ConversationManager conversationManager2 = current4.ConversationManager;
					if (conversationManager2 != null)
					{
						conversationManager2.EndConversation();
					}
				}
			}
			catch
			{
			}
			string targetKey = SceneTauntBehavior.BuildSceneTauntTargetKey(targetHero, val, targetAgent.Index);
			bool flag2 = TryStartConflict(targetHero, val, targetAgent.Index, targetKey, fromVerbalTaunt: false, playerUsedWeapon);
			object[] array2 = new object[6];
			Mission current5 = Mission.Current;
			array2[0] = ((current5 != null) ? new float?(current5.CurrentTime) : ((float?)null));
			array2[1] = reason;
			array2[2] = ((targetAgent == null) ? null : targetAgent.Name?.ToString()) ?? "null";
			array2[3] = flag2;
			array2[4] = _conflictActive;
			array2[5] = _armedConflict;
			Logger.Log("SceneTaunt", string.Format("[AttackTiming] try_start_conflict_result time={0:0.###} reason={1} target={2} started={3} conflict={4} armed={5}", array2));
			if (!flag2)
			{
				return false;
			}
			if (IsSettlementCriminalConflictTarget(targetHero, val))
			{
				Logger.Log("SceneTaunt", $"Physical attack redirected to native criminal conflict. Reason={reason}, Target={targetAgent.Name}, UsedWeapon={playerUsedWeapon}");
				return true;
			}
			bool flag3 = IsAuthorityPhysicalAttackTarget(targetHero, val);
			if ((playerUsedWeapon || flag3) && !_armedConflict)
			{
				EscalateToArmedConflict(flag3 ? "player_attacked_authority_in_peace_scene" : "player_started_scene_fight_with_weapon");
			}
			Logger.Log("SceneTaunt", $"Physical attack triggered scene conflict. Reason={reason}, Target={targetAgent.Name}, UsedWeapon={playerUsedWeapon}, AuthorityTarget={flag3}");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Starting scene conflict from physical attack failed: " + ex.Message);
			return false;
		}
	}

	private bool ShouldSuppressDuplicateNativeCriminalConflict(Agent targetAgent)
	{
		if (targetAgent == null || targetAgent.Index < 0 || Mission.Current == null)
		{
			return false;
		}
		return _lastNativeCriminalConflictTargetAgentIndex == targetAgent.Index && Mission.Current.CurrentTime - _lastNativeCriminalConflictMissionTime <= 0.45f;
	}

	private void RememberNativeCriminalConflictTarget(Agent targetAgent)
	{
		if (targetAgent != null && targetAgent.Index >= 0 && Mission.Current != null)
		{
			_lastNativeCriminalConflictTargetAgentIndex = targetAgent.Index;
			_lastNativeCriminalConflictMissionTime = Mission.Current.CurrentTime;
		}
	}

	private bool TryAddFacingAgentToArmedConflict(Agent targetAgent, string reason)
	{
		try
		{
			if (!_conflictActive || !_armedConflict || targetAgent == null || !targetAgent.IsHuman || !targetAgent.IsActive())
			{
				return false;
			}
			if (targetAgent == Agent.Main || _playerAgentIndices.Contains(targetAgent.Index) || _guardAgentIndices.Contains(targetAgent.Index))
			{
				return false;
			}
			BasicCharacterObject character = targetAgent.Character;
			CharacterObject val = (CharacterObject)(object)((character is CharacterObject) ? character : null);
			Hero val2 = ((val != null) ? val.HeroObject : null);
			if (IsAuthorityPhysicalAttackTarget(val2, val))
			{
				_activeTargetKey = SceneTauntBehavior.BuildSceneTauntTargetKey(val2, val, targetAgent.Index);
				_activeTargetName = targetAgent.Name?.ToString() ?? ((val2 == null) ? null : ((object)val2.Name)?.ToString()) ?? ((val == null) ? null : ((object)((BasicCharacterObject)val).Name)?.ToString()) ?? _activeTargetName;
				_activeTargetAgentIndex = targetAgent.Index;
				EnableSettlementConsequencesForCurrentConflict(val, val2, 35f, "authority_targeted_during_armed_conflict");
			}
			BasicCharacterObject character2 = targetAgent.Character;
			if (SceneTauntBehavior.IsChildSceneProtectedTarget((CharacterObject)(object)((character2 is CharacterObject) ? character2 : null)))
			{
				return false;
			}
			bool flag = ShouldFleeWhenArmedVictim(targetAgent);
			if (_opponentAgentIndices.Contains(targetAgent.Index))
			{
				if (_armedBystanderWatcherIndices.Contains(targetAgent.Index))
				{
					ReleaseArmedBystanderWatcher(targetAgent);
					if (flag)
					{
						TryForceUnarmedBystanderToFlee(targetAgent);
						Logger.Log("SceneTaunt", "Released frozen fleeing civilian into active armed conflict while preserving flee. Reason=" + reason + ", Target=" + targetAgent.Name);
					}
					else
					{
						Logger.Log("SceneTaunt", "Released frozen armed bystander into active combat. Reason=" + reason + ", Target=" + targetAgent.Name);
					}
					return true;
				}
				if (flag)
				{
					TryForceUnarmedBystanderToFlee(targetAgent);
					Logger.Log("SceneTaunt", "Refreshed fleeing hostile civilian during armed conflict. Reason=" + reason + ", Target=" + targetAgent.Name);
					return true;
				}
				return false;
			}
			AddAgentToFightSide(targetAgent, isPlayerSide: false);
			TryForceAgentMortal(targetAgent);
			TryAlarmAgent(targetAgent);
			foreach (Agent item in CollectEscortedFollowers(targetAgent))
			{
				AddAgentToFightSide(item, isPlayerSide: false);
				TryForceAgentMortal(item);
				TryAlarmAgent(item);
			}
			if (flag)
			{
				TryForceUnarmedBystanderToFlee(targetAgent);
				Logger.Log("SceneTaunt", $"Added fleeing civilian to armed scene conflict while preserving flee. Reason={reason}, Target={targetAgent.Name}, Opponents={_opponentAgentIndices.Count}");
			}
			else
			{
				Logger.Log("SceneTaunt", $"Added facing civilian to armed scene conflict. Reason={reason}, Target={targetAgent.Name}, Opponents={_opponentAgentIndices.Count}");
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Adding facing agent to armed conflict failed: " + ex.Message);
			return false;
		}
	}

	private bool TryAddFacingAgentToUnarmedConflict(Agent targetAgent, string reason)
	{
		try
		{
			if (!_conflictActive || _armedConflict || targetAgent == null || !targetAgent.IsHuman || !targetAgent.IsActive())
			{
				return false;
			}
			if (targetAgent == Agent.Main || _playerAgentIndices.Contains(targetAgent.Index))
			{
				return false;
			}
			BasicCharacterObject character = targetAgent.Character;
			CharacterObject val = (CharacterObject)(object)((character is CharacterObject) ? character : null);
			Hero val2 = ((val != null) ? val.HeroObject : null);
			if (!IsEligiblePhysicalAttackTarget(val2, val) || SceneTauntBehavior.IsChildSceneProtectedTarget(val))
			{
				return false;
			}
			if (IsAuthorityPhysicalAttackTarget(val2, val))
			{
				_activeTargetKey = SceneTauntBehavior.BuildSceneTauntTargetKey(val2, val, targetAgent.Index);
				_activeTargetName = targetAgent.Name?.ToString() ?? ((val2 == null) ? null : ((object)val2.Name)?.ToString()) ?? ((val == null) ? null : ((object)((BasicCharacterObject)val).Name)?.ToString()) ?? _activeTargetName;
				_activeTargetAgentIndex = targetAgent.Index;
				EnableSettlementConsequencesForCurrentConflict(val, val2, 35f, "authority_targeted_during_unarmed_conflict");
				if (!_opponentAgentIndices.Contains(targetAgent.Index) && !_guardAgentIndices.Contains(targetAgent.Index))
				{
					AddAgentToFightSide(targetAgent, isPlayerSide: false);
					foreach (Agent item in CollectEscortedFollowers(targetAgent))
					{
						AddAgentToFightSide(item, isPlayerSide: false);
					}
				}
				ClearMissionFightHandlerPendingFinishTimer();
				EscalateToArmedConflict("player_attacked_authority_during_unarmed_conflict");
				Logger.Log("SceneTaunt", $"Escalated existing unarmed conflict after attacking authority. Reason={reason}, Target={targetAgent.Name}, TargetIsLord={SceneTauntBehavior.IsSceneLordTauntTarget(val2)}");
				return true;
			}
			if (_opponentAgentIndices.Contains(targetAgent.Index) || _guardAgentIndices.Contains(targetAgent.Index))
			{
				return false;
			}
			ClearMissionFightHandlerPendingFinishTimer();
			AddAgentToFightSide(targetAgent, isPlayerSide: false);
			TryStripWeaponsForUnarmedConflict(targetAgent);
			TryAlarmAgent(targetAgent);
			foreach (Agent item2 in CollectEscortedFollowers(targetAgent))
			{
				AddAgentToFightSide(item2, isPlayerSide: false);
				TryStripWeaponsForUnarmedConflict(item2);
				TryAlarmAgent(item2);
			}
			TryAppendPlayerBehaviorFactForOpenedBrawl(val2, val, targetAgent.Index);
			Logger.Log("SceneTaunt", $"Added facing agent to existing unarmed scene conflict. Reason={reason}, Target={targetAgent.Name}, Opponents={_opponentAgentIndices.Count}");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Adding facing agent to unarmed conflict failed: " + ex.Message);
			return false;
		}
	}

	private static void TryAppendPlayerBehaviorFactForOpenedBrawl(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		try
		{
			string factText = BuildDirectBrawlImmediateReactionFactText();
			ShoutBehavior.TriggerImmediateSceneBehaviorReactionForExternal(factText, targetAgentIndex, persistHeroPrivateHistory: true, suppressStare: true);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Appending player behavior fact for opened brawl failed: " + ex.Message);
		}
	}

	private static string BuildDirectBrawlImmediateReactionFactText()
	{
		string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "玩家";
		}
		return "[AFEF NPC行为补充] " + text + "一拳打到了你的身上，你也开始拿拳头反击。";
	}

	private static string BuildDirectArmedImmediateReactionFactText(Agent targetAgent)
	{
		string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "玩家";
		}
		string text2 = TryGetActiveWeaponDisplayName(targetAgent);
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "武器";
		}
		return "[AFEF NPC行为补充] ，" + text + "一刀看向了你，而你现在也拔出了" + text2 + "与" + text + "肉搏。";
	}

	private static string TryGetActiveWeaponDisplayName(Agent agent)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Invalid comparison between Unknown and I4
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (agent == null)
			{
				return "";
			}
			EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
			if (TryGetRealWeaponDisplayName(agent, primaryWieldedItemIndex, out var weaponName))
			{
				return weaponName;
			}
			EquipmentIndex offhandWieldedItemIndex = agent.GetOffhandWieldedItemIndex();
			if (TryGetRealWeaponDisplayName(agent, offhandWieldedItemIndex, out weaponName))
			{
				return weaponName;
			}
			for (EquipmentIndex val = (EquipmentIndex)0; (int)val < 5; val = (EquipmentIndex)(val + 1))
			{
				if (IsMissionWeaponRealWeapon(agent.Equipment[val]))
				{
					MissionWeapon val2 = agent.Equipment[val];
					ItemObject item = ((MissionWeapon)(ref val2)).Item;
					string text = ((item == null) ? null : ((object)item.Name)?.ToString());
					if (!string.IsNullOrWhiteSpace(text))
					{
						return text.Trim();
					}
				}
			}
		}
		catch
		{
		}
		return "";
	}

	private static bool TryGetRealWeaponDisplayName(Agent agent, EquipmentIndex equipmentIndex, out string weaponName)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		weaponName = "";
		try
		{
			if (!IsRealWeaponWieldedSlot(agent, equipmentIndex))
			{
				return false;
			}
			MissionWeapon val = agent.Equipment[equipmentIndex];
			ItemObject item = ((MissionWeapon)(ref val)).Item;
			string text = ((item == null) ? null : ((object)item.Name)?.ToString());
			if (string.IsNullOrWhiteSpace(text))
			{
				return false;
			}
			weaponName = text.Trim();
			return weaponName.Length > 0;
		}
		catch
		{
			return false;
		}
	}

	private static void TryAppendNpcBehaviorFactForVerbalConflict(int targetAgentIndex)
	{
		try
		{
			string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "玩家";
			}
			string factText = "经过交流，你和" + text + "发生了冲突";
			ShoutBehavior.AppendExternalTargetedSceneNpcFactForExternal(factText, targetAgentIndex);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Appending NPC behavior fact for verbal conflict failed: " + ex.Message);
		}
	}

	private void TryAppendNpcBehaviorFactForVerbalArmedEscalation()
	{
		try
		{
			if (_activeTargetAgentIndex >= 0)
			{
				string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
				if (string.IsNullOrWhiteSpace(text))
				{
					text = "玩家";
				}
				Mission current = Mission.Current;
				Agent val = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == _activeTargetAgentIndex));
				BasicCharacterObject obj = ((val != null) ? val.Character : null);
				CharacterObject val2 = (CharacterObject)(object)((obj is CharacterObject) ? obj : null);
				Hero targetHero = ((val2 != null) ? val2.HeroObject : null);
				bool flag = _guardAgentIndices.Count > 0;
				bool flag2 = SceneTauntBehavior.IsSoldierSceneTauntTarget(val2);
				bool flag3 = SceneTauntBehavior.IsSceneLordTauntTarget(targetHero);
				bool flag4 = IsAgentCarryingRealWeapon(val);
				bool flag5 = IsSettlementCriminalConflictTarget(targetHero, val2);
				string factText = (flag3 ? ("经过交流，你和" + text + "彻底撕破了脸，你身边的士兵立刻拔出武器开始围剿他") : (flag2 ? ("经过交流，你和" + text + "发生了冲突，周围的士兵立刻拔出武器开始围剿他") : (flag5 ? (flag4 ? ("经过交流，你和" + text + "彻底闹翻了，他直接亮出了武器，你也开始和他械斗") : ("经过交流，你和" + text + "彻底闹翻了，他突然亮出了武器，你被吓得开始逃跑")) : ((!flag4) ? (flag ? ("经过交流，你和" + text + "发生了冲突，他随即亮出了武器，周围的守卫立刻开始围剿他") : ("经过交流，你和" + text + "发生了冲突，他随即亮出了武器，你被吓得开始逃跑")) : (flag ? ("经过交流，你和" + text + "发生了冲突，你也拿出武器开始和他械斗，周围的守卫也开始帮助你") : ("经过交流，你和" + text + "发生了冲突，你也拿出武器开始和他械斗"))))));
				ShoutBehavior.AppendExternalTargetedSceneNpcFactForExternal(factText, _activeTargetAgentIndex);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Appending NPC behavior fact for verbal armed escalation failed: " + ex.Message);
		}
	}

	private void TryAppendPlayerBehaviorFactForArmedEscalation(string reason)
	{
		try
		{
			if (_activeTargetAgentIndex < 0)
			{
				return;
			}
			if (_openedFromVerbalTaunt || string.Equals(reason, "taunted_lord_scene", StringComparison.Ordinal) || string.Equals(reason, "taunted_soldier", StringComparison.Ordinal))
			{
				TryAppendNpcBehaviorFactForVerbalArmedEscalation();
				return;
			}
			Mission current = Mission.Current;
			Agent targetAgent = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == _activeTargetAgentIndex));
			string factText = BuildDirectArmedImmediateReactionFactText(targetAgent);
			ShoutBehavior.TriggerImmediateSceneBehaviorReactionForExternal(factText, _activeTargetAgentIndex, persistHeroPrivateHistory: true, suppressStare: true);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Appending player behavior fact for armed escalation failed: " + ex.Message);
		}
	}

	private string BuildGuardReactionFactText()
	{
		string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "玩家";
		}
		Mission current = Mission.Current;
		Agent val = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == _activeTargetAgentIndex));
		BasicCharacterObject obj = ((val != null) ? val.Character : null);
		CharacterObject val2 = (CharacterObject)(object)((obj is CharacterObject) ? obj : null);
		Hero targetHero = ((val2 != null) ? val2.HeroObject : null);
		bool flag = IsAgentUsingRealWeapon(Agent.Main);
		bool flag2 = SceneTauntBehavior.IsSceneLordTauntTarget(targetHero);
		bool flag3 = SceneTauntBehavior.IsSoldierSceneTauntTarget(val2);
		bool flag4 = IsSettlementCriminalConflictTarget(targetHero, val2);
		if (_openedFromVerbalTaunt)
		{
			if (flag2 || flag3)
			{
				return text + "在定居点内与你爆发了冲突，你和其他守卫开始向他发动攻击";
			}
			if (flag4)
			{
				return text + (flag ? "在定居点内和暴徒爆发了冲突，并亮出了武器，周围的人立刻开始躲避" : "在定居点内和暴徒爆发了冲突，周围的人立刻开始躲避");
			}
			return text + (flag ? "在定居点内和你爆发了冲突，并亮出了武器，你和其他守卫开始向他发动攻击" : "在定居点内和你爆发了冲突，你和其他守卫开始向他发动攻击");
		}
		if (flag4)
		{
			return text + (flag ? "在定居点内和暴徒爆发了械斗，周围的人立刻开始四散躲避" : "在定居点内和暴徒爆发了冲突，周围的人立刻开始四散躲避");
		}
		if (!flag && (_openedAsUnarmedBrawl || flag2 || flag3))
		{
			return text + "在定居点内殴打了平民，你和其他守卫开始向他发动攻击";
		}
		return text + "在定居点内拿武器乱砍人，你和其他守卫开始向他发动攻击";
	}

	private void TryAppendGuardBehaviorFactsForArmedEscalation()
	{
		try
		{
			Agent main = Agent.Main;
			if (main != null && main.IsActive() && _guardAgentIndices.Count != 0)
			{
				string value = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
				if (string.IsNullOrWhiteSpace(value))
				{
					value = "玩家";
				}
				string factText = BuildGuardReactionFactText();
				TryRollArmedEscalationBehaviorFacts(main, factText);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Appending guard behavior facts for armed escalation failed: " + ex.Message);
		}
	}

	private void TryAppendNearbyArmedEscalationBehaviorFacts()
	{
		try
		{
			if (!_conflictActive || !_armedConflict)
			{
				return;
			}
			Agent main = Agent.Main;
			if (main != null && main.IsActive())
			{
				string value = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
				if (string.IsNullOrWhiteSpace(value))
				{
					value = "玩家";
				}
				string factText = BuildGuardReactionFactText();
				TryRollArmedEscalationBehaviorFacts(main, factText);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Appending nearby armed escalation behavior facts failed: " + ex.Message);
		}
	}

	private void TryRollArmedEscalationBehaviorFacts(Agent main, string factText)
	{
		if (main == null || string.IsNullOrWhiteSpace(factText))
		{
			return;
		}
		foreach (int item in _guardAgentIndices)
		{
			Mission current = Mission.Current;
			Agent val = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == item && a.IsActive()));
			if (val != null && IsAgentWithinArmedBystanderReactionRadius(val, main) && _armedEscalationBehaviorFactRolledAgentIndices.Add(item) && MBRandom.RandomFloat <= 0.3334f)
			{
				ShoutBehavior.TriggerImmediateSceneBehaviorReactionForExternal(factText, item, persistHeroPrivateHistory: true, suppressStare: true);
			}
		}
		HashSet<int> hashSet = new HashSet<int>(_playerAgentIndices);
		hashSet.UnionWith(_guardAgentIndices);
		Mission current2 = Mission.Current;
		IEnumerable<Agent> enumerable = (IEnumerable<Agent>)((current2 != null) ? current2.Agents : null);
		foreach (Agent item2 in enumerable ?? Enumerable.Empty<Agent>())
		{
			if (item2 != null && item2.IsActive() && item2.IsHuman && !hashSet.Contains(item2.Index) && IsAgentWithinArmedBystanderReactionRadius(item2, main) && IsAgentCarryingRealWeapon(item2) && !SceneTauntBehavior.IsChildSceneProtectedTarget((CharacterObject)/*isinst with value type is only supported in some contexts*/) && _armedEscalationBehaviorFactRolledAgentIndices.Add(item2.Index) && MBRandom.RandomFloat <= 0.5f)
			{
				ShoutBehavior.TriggerImmediateSceneBehaviorReactionForExternal(factText, item2.Index, persistHeroPrivateHistory: true, suppressStare: true);
			}
		}
	}

	internal bool ShouldBlockAgentWeaponWield(Agent agent)
	{
		return _conflictActive && !_armedConflict && agent != null && _blockedAiWeaponAgentIndices.Contains(agent.Index);
	}

	internal bool ShouldUseFullCombatDamage(Agent victimAgent, Agent attackerAgent)
	{
		if (!_conflictActive || victimAgent == null || attackerAgent == null)
		{
			return false;
		}
		bool flag = _playerAgentIndices.Contains(attackerAgent.Index);
		bool flag2 = _opponentAgentIndices.Contains(attackerAgent.Index);
		bool flag3 = _playerAgentIndices.Contains(victimAgent.Index);
		bool flag4 = _opponentAgentIndices.Contains(victimAgent.Index);
		if ((flag && flag4) || (flag2 && flag3))
		{
			return true;
		}
		if (!_armedConflict)
		{
			return false;
		}
		if (flag && IsArmedConflictCollateralVictim(victimAgent))
		{
			return true;
		}
		return flag2 && flag3;
	}

	private bool IsArmedConflictCollateralVictim(Agent victimAgent)
	{
		try
		{
			if (victimAgent == null || !victimAgent.IsHuman || !victimAgent.IsActive() || victimAgent.IsMainAgent || _playerAgentIndices.Contains(victimAgent.Index))
			{
				return false;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	internal bool ShouldBlockSceneExit()
	{
		return _conflictActive || SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement();
	}

	internal bool ShouldDelayNativeFightAutoEndLong()
	{
		return SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement();
	}

	internal bool ShouldSendPlayerToLocalDungeonOnDefeat()
	{
		if (_armedDefeatOutcomeHandled || !_armedConflictOccurredThisConflict || ShouldCommitPlayerBattleDeathAfterMission())
		{
			return false;
		}
		try
		{
			return Agent.Main == null || !Agent.Main.IsActive();
		}
		catch
		{
			return false;
		}
	}

	internal void MarkPlayerDefeatOutcomeHandled()
	{
		_armedDefeatOutcomeHandled = true;
	}

	internal bool TryUseSafeMainHeroDefeatState(Agent effectedAgent, float deathProbability, out AgentState result)
	{
		result = (AgentState)3;
		try
		{
			if (!_conflictActive || (!_armedConflict && !_armedConflictOccurredThisConflict) || effectedAgent == null || !effectedAgent.IsMainAgent)
			{
				return false;
			}
			if (!_pendingPlayerBattleDeathDecisionCaptured)
			{
				_pendingPlayerBattleDeathAfterMission = RollDeferredPlayerBattleDeath(deathProbability);
				_pendingPlayerBattleDeathDecisionCaptured = true;
				Logger.Log("SceneTaunt", $"Deferred main hero defeat state inside mission. PendingBattleDeath={_pendingPlayerBattleDeathAfterMission}, DeathProbability={MathF.Max(0f, MathF.Min(1f, deathProbability)):0.###}");
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Deferring main hero defeat state failed: " + ex.Message);
			return false;
		}
	}

	internal bool ShouldCommitPlayerBattleDeathAfterMission()
	{
		return _pendingPlayerBattleDeathAfterMission && _armedConflictOccurredThisConflict;
	}

	internal void EnsurePendingPlayerBattleDeathQueued(string reason)
	{
		if (ShouldCommitPlayerBattleDeathAfterMission())
		{
			SceneTauntBehavior.QueuePendingMainHeroBattleDeathForExternal(_pendingPlayerBattleDeathKiller, reason);
		}
	}

	internal bool WasLastArmedDefeatCriminalConflict()
	{
		return _armedDefeatWasCriminalConflict;
	}

	internal static bool ShouldBlockAgentWeaponWieldExternal(Agent agent)
	{
		try
		{
			bool? obj;
			if (agent == null)
			{
				obj = null;
			}
			else
			{
				Mission mission = agent.Mission;
				obj = ((mission == null) ? ((bool?)null) : mission.GetMissionBehavior<SceneTauntMissionBehavior>()?.ShouldBlockAgentWeaponWield(agent));
			}
			bool? flag = obj;
			return flag == true;
		}
		catch
		{
			return false;
		}
	}

	internal static bool ShouldUseFullCombatDamageExternal(Agent victimAgent, Agent attackerAgent)
	{
		try
		{
			Mission val = ((victimAgent != null) ? victimAgent.Mission : null) ?? ((attackerAgent != null) ? attackerAgent.Mission : null);
			return ((val == null) ? ((bool?)null) : val.GetMissionBehavior<SceneTauntMissionBehavior>()?.ShouldUseFullCombatDamage(victimAgent, attackerAgent)) == true;
		}
		catch
		{
			return false;
		}
	}

	internal static bool ShouldDelayNativeFightAutoEndLongExternal(Mission mission)
	{
		try
		{
			return ((mission != null) ? mission.GetMissionBehavior<SceneTauntMissionBehavior>() : null)?.ShouldDelayNativeFightAutoEndLong() ?? false;
		}
		catch
		{
			return false;
		}
	}

	internal static bool ShouldSuppressSceneNotableDeathExternal(Hero hero)
	{
		try
		{
			Mission current = Mission.Current;
			return ((current == null) ? ((bool?)null) : current.GetMissionBehavior<SceneTauntMissionBehavior>()?.ShouldSuppressSceneNotableDeath(hero)) == true;
		}
		catch
		{
			return false;
		}
	}

	internal static bool ShouldDeferSceneNotableBattleDeathExternal(Hero hero)
	{
		try
		{
			Mission current = Mission.Current;
			return ((current == null) ? ((bool?)null) : current.GetMissionBehavior<SceneTauntMissionBehavior>()?.ShouldDeferSceneNotableBattleDeath(hero)) == true;
		}
		catch
		{
			return false;
		}
	}

	internal static bool ShouldSuppressNativeMissionConversationExternal(Mission mission)
	{
		try
		{
			if (((mission == null) ? ((bool?)null) : mission.GetMissionBehavior<SceneTauntMissionBehavior>()?.ShouldSuppressNativeMissionConversation()) == true)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			return SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement();
		}
		catch
		{
			return false;
		}
	}

	internal static string BuildFrightenedCivilianShoutExtraFactExternal(Agent targetAgent)
	{
		try
		{
			Mission current = Mission.Current;
			return ((current == null) ? null : current.GetMissionBehavior<SceneTauntMissionBehavior>()?.BuildFrightenedCivilianShoutExtraFact(targetAgent)) ?? "";
		}
		catch
		{
			return "";
		}
	}

	internal static bool ShouldBlockSceneExitExternal(Mission mission)
	{
		try
		{
			if (((mission == null) ? ((bool?)null) : mission.GetMissionBehavior<SceneTauntMissionBehavior>()?.ShouldBlockSceneExit()) == true)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			return SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement();
		}
		catch
		{
			return false;
		}
	}

	private Agent ResolveTargetAgent(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		try
		{
			if (targetAgentIndex >= 0)
			{
				Mission current = Mission.Current;
				Agent val = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent x) => x != null && x.Index == targetAgentIndex));
				if (val != null)
				{
					return val;
				}
			}
		}
		catch
		{
		}
		try
		{
			Campaign current2 = Campaign.Current;
			object obj2;
			if (current2 == null)
			{
				obj2 = null;
			}
			else
			{
				ConversationManager conversationManager = current2.ConversationManager;
				obj2 = ((conversationManager != null) ? conversationManager.OneToOneConversationAgent : null);
			}
			Agent val2 = (Agent)((obj2 is Agent) ? obj2 : null);
			BasicCharacterObject obj3 = ((val2 != null) ? val2.Character : null);
			CharacterObject val3 = (CharacterObject)(object)((obj3 is CharacterObject) ? obj3 : null);
			if (val2 != null && ((object)val2.Character == targetCharacter || ((val3 != null) ? val3.HeroObject : null) == targetHero))
			{
				return val2;
			}
		}
		catch
		{
		}
		try
		{
			if (targetHero != null)
			{
				Mission current3 = Mission.Current;
				Agent val4 = ((current3 == null) ? null : ((IEnumerable<Agent>)current3.Agents)?.FirstOrDefault(delegate(Agent x)
				{
					int result;
					if (x != null && x.IsHuman)
					{
						BasicCharacterObject character = x.Character;
						BasicCharacterObject obj7 = ((character is CharacterObject) ? character : null);
						result = ((((obj7 != null) ? ((CharacterObject)obj7).HeroObject : null) == targetHero) ? 1 : 0);
					}
					else
					{
						result = 0;
					}
					return (byte)result != 0;
				}));
				if (val4 != null)
				{
					return val4;
				}
			}
		}
		catch
		{
		}
		try
		{
			Mission current4 = Mission.Current;
			return (current4 == null) ? null : ((IEnumerable<Agent>)current4.Agents)?.FirstOrDefault((Agent x) => x != null && x.IsHuman && (object)x.Character == targetCharacter);
		}
		catch
		{
			return null;
		}
	}

	private static bool IsEligiblePhysicalAttackTarget(Hero targetHero, CharacterObject targetCharacter)
	{
		return IsAuthorityPhysicalAttackTarget(targetHero, targetCharacter) || IsSettlementCriminalConflictTarget(targetHero, targetCharacter) || SceneTauntBehavior.IsSceneNotableTauntTarget(targetHero) || SceneTauntBehavior.IsEligibleSceneTauntCharacter(targetCharacter);
	}

	private static bool IsAuthorityPhysicalAttackTarget(Hero targetHero, CharacterObject targetCharacter)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Invalid comparison between Unknown and I4
		if (SceneTauntBehavior.IsSceneLordTauntTarget(targetHero))
		{
			return true;
		}
		if (targetCharacter == null || ((BasicCharacterObject)targetCharacter).IsHero)
		{
			return false;
		}
		Occupation occupation = targetCharacter.Occupation;
		Occupation val = occupation;
		if ((int)val == 7 || val - 23 <= 1)
		{
			return true;
		}
		return false;
	}

	private static bool IsSettlementCriminalConflictTarget(Hero targetHero, CharacterObject targetCharacter)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Invalid comparison between Unknown and I4
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Invalid comparison between Unknown and I4
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Invalid comparison between Unknown and I4
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Invalid comparison between Unknown and I4
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Invalid comparison between Unknown and I4
		Hero val = targetHero ?? ((targetCharacter != null) ? targetCharacter.HeroObject : null);
		if (val != null)
		{
			Occupation occupation = val.Occupation;
			Occupation val2 = occupation;
			if ((int)val2 == 15 || (int)val2 == 21)
			{
				return true;
			}
		}
		if (targetCharacter == null)
		{
			return false;
		}
		Occupation occupation2 = targetCharacter.Occupation;
		Occupation val3 = occupation2;
		if ((int)val3 == 15 || (int)val3 == 21 || (int)val3 == 27)
		{
			return true;
		}
		return false;
	}

	internal static bool IsSettlementCriminalConflictTargetExternal(Hero targetHero, CharacterObject targetCharacter)
	{
		return IsSettlementCriminalConflictTarget(targetHero, targetCharacter);
	}

	private bool IsActiveTargetSettlementCriminalConflict()
	{
		try
		{
			Mission current = Mission.Current;
			Agent val = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == _activeTargetAgentIndex));
			BasicCharacterObject obj = ((val != null) ? val.Character : null);
			CharacterObject val2 = (CharacterObject)(object)((obj is CharacterObject) ? obj : null);
			Hero targetHero = ((val2 != null) ? val2.HeroObject : null);
			return IsSettlementCriminalConflictTarget(targetHero, val2);
		}
		catch
		{
			return false;
		}
	}

	private void TryRewardSettlementTrustForCriminalKnockdown(Settlement settlement, string victimName)
	{
		SceneTauntBehavior.TryRewardSettlementTrustForCriminalKnockdownForExternal(settlement, victimName);
	}

	private static Hero TryResolveCriminalOwnerHeroFromAgent(Agent victimAgent)
	{
		try
		{
			BasicCharacterObject obj = ((victimAgent != null) ? victimAgent.Character : null);
			CharacterObject val = (CharacterObject)(object)((obj is CharacterObject) ? obj : null);
			Hero val2 = ((val != null) ? val.HeroObject : null);
			if (val2 != null && val2.IsGangLeader)
			{
				return val2;
			}
			CampaignAgentComponent val3 = ((victimAgent != null) ? victimAgent.GetComponent<CampaignAgentComponent>() : null);
			object obj2;
			if (val3 == null)
			{
				obj2 = null;
			}
			else
			{
				AgentNavigator agentNavigator = val3.AgentNavigator;
				if (agentNavigator == null)
				{
					obj2 = null;
				}
				else
				{
					Alley memberOfAlley = agentNavigator.MemberOfAlley;
					obj2 = ((memberOfAlley != null) ? ((SettlementArea)memberOfAlley).Owner : null);
				}
			}
			Hero val4 = (Hero)obj2;
			if (val4 != null && val4 != Hero.MainHero)
			{
				return val4;
			}
		}
		catch
		{
		}
		return null;
	}

	private bool TryStartNativeCriminalConflict(Agent targetAgent, string reason)
	{
		try
		{
			object obj;
			if (targetAgent == null)
			{
				obj = null;
			}
			else
			{
				CampaignAgentComponent component = targetAgent.GetComponent<CampaignAgentComponent>();
				if (component == null)
				{
					obj = null;
				}
				else
				{
					AgentNavigator agentNavigator = component.AgentNavigator;
					obj = ((agentNavigator != null) ? agentNavigator.MemberOfAlley : null);
				}
			}
			Alley val = (Alley)obj;
			if (val == null || Mission.Current == null)
			{
				Logger.Log("SceneTaunt", "Native criminal conflict start skipped because alley context is unavailable.");
				return false;
			}
			Type type = AccessTools.TypeByName("SandBox.Missions.MissionLogics.MissionAlleyHandler");
			MethodInfo methodInfo = typeof(Mission).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault((MethodInfo m) => m.Name == "GetMissionBehavior" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
			MethodInfo methodInfo2 = AccessTools.Method(type, "StartCommonAreaBattle");
			if (type == null || methodInfo == null || methodInfo2 == null)
			{
				Logger.Log("SceneTaunt", "Native criminal conflict start skipped because alley handler reflection failed.");
				return false;
			}
			object obj2 = methodInfo.MakeGenericMethod(type).Invoke(Mission.Current, null);
			if (obj2 == null)
			{
				Logger.Log("SceneTaunt", "Native criminal conflict start skipped because MissionAlleyHandler was not found.");
				return false;
			}
			try
			{
				Campaign current = Campaign.Current;
				if (current != null)
				{
					ConversationManager conversationManager = current.ConversationManager;
					if (conversationManager != null)
					{
						conversationManager.EndConversation();
					}
				}
			}
			catch
			{
			}
			TryTriggerNativeCriminalConflictReaction(targetAgent, reason);
			methodInfo2.Invoke(obj2, new object[1] { val });
			Logger.Log("SceneTaunt", $"Redirected criminal conflict to native alley flow. Reason={reason}, Target={((targetAgent != null) ? targetAgent.Name : null)}, Alley={((val != null) ? ((SettlementArea)val).Name : null)}");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Starting native criminal conflict failed: " + ex.Message);
			return false;
		}
	}

	private void TryTriggerNativeCriminalConflictReaction(Agent targetAgent, string reason)
	{
		try
		{
			if (targetAgent != null && targetAgent.IsHuman && targetAgent.IsActive())
			{
				string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
				if (string.IsNullOrWhiteSpace(text))
				{
					text = "玩家";
				}
				string factText = (((reason ?? "").IndexOf("verbal", StringComparison.OrdinalIgnoreCase) >= 0) ? ("经过交流，" + text + "把你彻底激怒了，你立刻招呼同伙扑上去，要狠狠干他一顿") : ("经过交流，" + text + "竟敢直接对你动手，你一边破口大骂，一边立刻招呼同伙围上去狠狠干他一顿"));
				ShoutBehavior.TriggerImmediateSceneBehaviorReactionForExternal(factText, targetAgent.Index, persistHeroPrivateHistory: true, suppressStare: true);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Triggering native criminal conflict reaction failed: " + ex.Message);
		}
	}

	private void TryApplyCriminalOwnerPenalty(Hero ownerHero, string victimName)
	{
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Expected O, but got Unknown
		try
		{
			if (ownerHero != null && Hero.MainHero != null)
			{
				RewardSystemBehavior.Instance?.AdjustTrustForExternal(ownerHero, -1, 0, "scene_taunt_criminal_owner_knockdown");
				RomanceSystemBehavior.Instance?.AdjustPrivateLove(ownerHero, -1, "scene_taunt_criminal_owner_knockdown");
				string text = (string.IsNullOrWhiteSpace(victimName) ? "匪类" : victimName);
				InformationManager.DisplayMessage(new InformationMessage($"击倒 {text}：{ownerHero.Name} 的个人信任 -1，私人关系 -1。", new Color(1f, 0.72f, 0.2f, 1f)));
				Logger.Log("SceneTaunt", $"Applied criminal owner penalty after knockdown. Owner={ownerHero.Name}, Victim={text}, PersonalTrustDelta=-1, PrivateLoveDelta=-1");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Applying criminal owner penalty failed: " + ex.Message);
		}
	}

	private List<Agent> CollectPlayerSideAgents()
	{
		List<Agent> list = new List<Agent>();
		try
		{
			foreach (Agent item in (List<Agent>)(object)Mission.Current.Agents)
			{
				if (item == null || !item.IsHuman || !item.IsActive())
				{
					continue;
				}
				if (item == Agent.Main)
				{
					AddUniqueAgent(list, item);
					continue;
				}
				try
				{
					LocationComplex current2 = LocationComplex.Current;
					LocationCharacter val = ((current2 != null) ? current2.FindCharacter((IAgent)(object)item) : null);
					LocationEncounter locationEncounter = PlayerEncounter.LocationEncounter;
					AccompanyingCharacter val2 = ((locationEncounter != null) ? locationEncounter.GetAccompanyingCharacter(val) : null);
					if (val2 != null && val2.IsFollowingPlayerAtMissionStart)
					{
						AddUniqueAgent(list, item);
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
		if (!list.Contains(Agent.Main))
		{
			AddUniqueAgent(list, Agent.Main);
		}
		return list;
	}

	private List<Agent> CollectOpponentSideAgents(Agent targetAgent)
	{
		List<Agent> list = new List<Agent>();
		AddUniqueAgent(list, targetAgent);
		foreach (Agent item in CollectEscortedFollowers(targetAgent))
		{
			AddUniqueAgent(list, item);
		}
		return list;
	}

	private List<Agent> CollectEscortedFollowers(Agent targetAgent)
	{
		List<Agent> list = new List<Agent>();
		if (targetAgent == null)
		{
			return list;
		}
		try
		{
			foreach (Agent item in (List<Agent>)(object)Mission.Current.Agents)
			{
				if (item != null && item != targetAgent && item.IsHuman && item.IsActive() && EscortAgentBehavior.CheckIfAgentIsEscortedBy(item, targetAgent))
				{
					AddUniqueAgent(list, item);
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private List<Agent> CollectGuardAgents(List<Agent> playerSideAgents, List<Agent> opponentSideAgents)
	{
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Invalid comparison between Unknown and I4
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Invalid comparison between Unknown and I4
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Invalid comparison between Unknown and I4
		HashSet<int> hashSet = new HashSet<int>(from x in playerSideAgents
			where x != null
			select x.Index);
		foreach (Agent opponentSideAgent in opponentSideAgents)
		{
			if (opponentSideAgent != null)
			{
				hashSet.Add(opponentSideAgent.Index);
			}
		}
		List<Agent> list = new List<Agent>();
		try
		{
			foreach (Agent item in (List<Agent>)(object)Mission.Current.Agents)
			{
				if (item != null && item.IsHuman && item.IsActive() && !hashSet.Contains(item.Index))
				{
					BasicCharacterObject character = item.Character;
					CharacterObject val = (CharacterObject)(object)((character is CharacterObject) ? character : null);
					if (val != null && ((int)val.Occupation == 24 || (int)val.Occupation == 23 || (int)val.Occupation == 7))
					{
						list.Add(item);
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private List<Agent> CollectAuthorityCarryoverOpponentAgents(List<Agent> playerSideAgents, out List<Agent> guardAgents)
	{
		HashSet<int> hashSet = new HashSet<int>(from x in playerSideAgents
			where x != null
			select x.Index);
		List<Agent> list = new List<Agent>();
		guardAgents = new List<Agent>();
		try
		{
			foreach (Agent item in (List<Agent>)(object)Mission.Current.Agents)
			{
				if (item == null || !item.IsHuman || !item.IsActive() || hashSet.Contains(item.Index))
				{
					continue;
				}
				BasicCharacterObject character = item.Character;
				CharacterObject val = (CharacterObject)(object)((character is CharacterObject) ? character : null);
				Hero targetHero = ((val != null) ? val.HeroObject : null);
				if (IsCarryoverAuthorityOpponent(targetHero, val))
				{
					AddUniqueAgent(list, item);
					if (IsGuardLikeCharacter(val))
					{
						AddUniqueAgent(guardAgents, item);
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private static bool IsCarryoverAuthorityOpponent(Hero targetHero, CharacterObject targetCharacter)
	{
		if (SceneTauntBehavior.IsSceneLordTauntTarget(targetHero))
		{
			return true;
		}
		return IsGuardLikeCharacter(targetCharacter);
	}

	private static bool IsGuardLikeCharacter(CharacterObject targetCharacter)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Invalid comparison between Unknown and I4
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Invalid comparison between Unknown and I4
		if (targetCharacter == null || ((BasicCharacterObject)targetCharacter).IsHero)
		{
			return false;
		}
		Occupation occupation = targetCharacter.Occupation;
		Occupation val = occupation;
		if ((int)val == 7 || val - 23 <= 1)
		{
			return true;
		}
		return false;
	}

	private void TryActivateSettlementArmedCarryover()
	{
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Expected O, but got Unknown
		//IL_0354: Unknown result type (might be due to invalid IL or missing references)
		//IL_0363: Expected O, but got Unknown
		//IL_0414: Unknown result type (might be due to invalid IL or missing references)
		//IL_0419: Unknown result type (might be due to invalid IL or missing references)
		//IL_0423: Expected O, but got Unknown
		if (_conflictActive || _armedCarryoverSceneInitialized || _armedCarryoverHandledInThisMission || !SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement() || Mission.Current == null || Agent.Main == null || !Agent.Main.IsActive() || Settlement.CurrentSettlement == null || _fightHandler == null)
		{
			return;
		}
		ICampaignMission current = CampaignMission.Current;
		if (((current != null) ? current.Location : null) == null || PlayerEncounter.LocationEncounter == null)
		{
			return;
		}
		Campaign current2 = Campaign.Current;
		bool? obj;
		if (current2 == null)
		{
			obj = null;
		}
		else
		{
			ConversationManager conversationManager = current2.ConversationManager;
			obj = ((conversationManager != null) ? new bool?(conversationManager.IsConversationInProgress) : ((bool?)null));
		}
		bool? flag = obj;
		if (flag == true || _fightHandler.IsThereActiveFight())
		{
			return;
		}
		float currentTime = Mission.Current.CurrentTime;
		if (_lastArmedCarryoverAttemptAtMissionTime >= 0f && currentTime - _lastArmedCarryoverAttemptAtMissionTime < 0.25f)
		{
			return;
		}
		_lastArmedCarryoverAttemptAtMissionTime = currentTime;
		List<Agent> list = CollectPlayerSideAgents();
		List<Agent> guardAgents;
		List<Agent> list2 = CollectAuthorityCarryoverOpponentAgents(list, out guardAgents);
		if (list2.Count == 0)
		{
			if (!_armedCarryoverNoAuthoritySceneNotified && !SceneTauntBehavior.HasShownCarryoverNoAuthorityAlertForCurrentLocationExternal())
			{
				AlarmNearbyBystanders();
				InformationManager.DisplayMessage(new InformationMessage("持械冲突的警报蔓延到了这个场景，周围的人立刻紧张起来。", new Color(1f, 0.45f, 0.2f, 1f)));
				_armedCarryoverNoAuthoritySceneNotified = true;
				_armedCarryoverHandledInThisMission = true;
				SceneTauntBehavior.MarkCarryoverNoAuthorityAlertShownForCurrentLocationExternal();
				Settlement currentSettlement = Settlement.CurrentSettlement;
				Logger.Log("SceneTaunt", $"Armed carryover reached scene without authority opponents. Settlement={((currentSettlement != null) ? currentSettlement.Name : null)}, Source={SceneTauntBehavior.GetArmedCarryoverSourceForCurrentSettlement()}");
			}
			return;
		}
		try
		{
			_conflictActive = true;
			_armedConflict = true;
			_armedConflictOccurredThisConflict = true;
			_armedDefeatOutcomeHandled = false;
			_baseConsequencesApplied = true;
			_appliedCrimeRatingAmount = 35f;
			_activeTargetKey = "armed_settlement_carryover";
			Settlement currentSettlement2 = Settlement.CurrentSettlement;
			_activeTargetName = ((currentSettlement2 == null) ? null : ((object)currentSettlement2.Name)?.ToString()) ?? "当前场景";
			_playerAgentIndices.Clear();
			_opponentAgentIndices.Clear();
			_guardAgentIndices.Clear();
			_blockedAiWeaponAgentIndices.Clear();
			foreach (Agent item in list)
			{
				_playerAgentIndices.Add(item.Index);
			}
			foreach (Agent item2 in list2)
			{
				_opponentAgentIndices.Add(item2.Index);
			}
			foreach (Agent item3 in guardAgents)
			{
				_guardAgentIndices.Add(item3.Index);
			}
			_fightHandler.StartCustomFight(list, list2, false, false, new OnFightEndDelegate(OnConflictFinished), float.Epsilon);
			foreach (Agent item4 in EnumerateConflictAgents(includeGuards: true))
			{
				if (item4 != null && item4.IsActive())
				{
					TryRestoreWeaponsAfterUnarmedConflict(item4);
					TryAlarmAgent(item4);
					if (item4 != Agent.Main)
					{
						TryArmAgent(item4);
					}
				}
			}
			ForceAllNonPlayerSceneAgentsMortal();
			AlarmNearbyBystanders();
			_armedCarryoverSceneInitialized = true;
			_armedCarryoverHandledInThisMission = true;
			InformationManager.DisplayMessage(new InformationMessage("你的持械冲突已经蔓延到这个场景，守卫和武装平民立刻开始围堵你。", new Color(1f, 0.35f, 0.2f, 1f)));
			object[] array = new object[4];
			Settlement currentSettlement3 = Settlement.CurrentSettlement;
			array[0] = ((currentSettlement3 != null) ? currentSettlement3.Name : null);
			array[1] = list2.Count;
			array[2] = guardAgents.Count;
			array[3] = SceneTauntBehavior.GetArmedCarryoverSourceForCurrentSettlement();
			Logger.Log("SceneTaunt", string.Format("Activated armed settlement carryover in scene. Settlement={0}, Opponents={1}, Guards={2}, Source={3}", array));
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Activating armed settlement carryover failed: " + ex.Message);
			ClearRuntimeState();
		}
	}

	private void PrepareUnarmedConflict()
	{
		_openedAsUnarmedBrawl = true;
		foreach (Agent item in EnumerateConflictAgents(includeGuards: false))
		{
			if (item != null && item.IsActive())
			{
				if (item == Agent.Main)
				{
					QueuePendingPlayerUnarmedPrep();
				}
				else
				{
					TryStripWeaponsForUnarmedConflict(item);
					TrySheathWeapons(item);
				}
				TryAlarmAgent(item);
				if (item != Agent.Main && item.IsAIControlled)
				{
					_blockedAiWeaponAgentIndices.Add(item.Index);
				}
			}
		}
		Logger.Log("SceneTaunt", $"Started unarmed scene conflict. Target={_activeTargetName}, PlayerSide={_playerAgentIndices.Count}, OpponentSide={_opponentAgentIndices.Count}");
	}

	private void TryQueueImmediateUnarmedFightEndAfterAgentRemoval(Agent affectedAgent, AgentState agentState)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Invalid comparison between Unknown and I4
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Invalid comparison between Unknown and I4
		try
		{
			if (_conflictActive && !_armedConflict && affectedAgent != null && ((int)agentState == 4 || (int)agentState == 3) && (_playerAgentIndices.Contains(affectedAgent.Index) || _opponentAgentIndices.Contains(affectedAgent.Index)))
			{
				if (IsIndexedSideDefeated(_opponentAgentIndices))
				{
					_pendingImmediateUnarmedFightEnd = true;
					_pendingImmediateUnarmedFightEndPlayerWon = true;
				}
				else if (IsIndexedSideDefeated(_playerAgentIndices))
				{
					_pendingImmediateUnarmedFightEnd = true;
					_pendingImmediateUnarmedFightEndPlayerWon = false;
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Queueing immediate unarmed fight end failed: " + ex.Message);
		}
	}

	private bool IsIndexedSideDefeated(HashSet<int> indices)
	{
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Invalid comparison between Unknown and I4
		try
		{
			if (indices != null && indices.Count != 0)
			{
				Mission current = Mission.Current;
				if (((current != null) ? current.Agents : null) != null)
				{
					foreach (int index in indices)
					{
						Agent val = ((IEnumerable<Agent>)Mission.Current.Agents).FirstOrDefault((Agent a) => a != null && a.Index == index);
						if (val != null && val.IsHuman && (int)val.State == 1)
						{
							return false;
						}
					}
					return true;
				}
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private void TryCommitPendingImmediateUnarmedFightEnd()
	{
		try
		{
			if (_pendingImmediateUnarmedFightEnd && !_armedConflict && _conflictActive)
			{
				if (_fightHandler == null || !_fightHandler.IsThereActiveFight())
				{
					_pendingImmediateUnarmedFightEnd = false;
					return;
				}
				bool pendingImmediateUnarmedFightEndPlayerWon = _pendingImmediateUnarmedFightEndPlayerWon;
				_pendingImmediateUnarmedFightEnd = false;
				ClearMissionFightHandlerPendingFinishTimer();
				_fightHandler.EndFight(pendingImmediateUnarmedFightEndPlayerWon);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Committing immediate unarmed fight end failed: " + ex.Message);
		}
	}

	private void ClearMissionFightHandlerPendingFinishTimer()
	{
		try
		{
			if (_fightHandler != null && FinishTimerField != null)
			{
				FinishTimerField.SetValue(_fightHandler, null);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Clearing MissionFightHandler finish timer failed: " + ex.Message);
		}
	}

	private void QueuePendingPlayerUnarmedPrep()
	{
		_pendingPlayerUnarmedPrep = true;
		Mission current = Mission.Current;
		_pendingPlayerUnarmedPrepAtMissionTime = ((current != null) ? current.CurrentTime : 0f) + 0.14f;
	}

	private void TryCommitPendingPlayerUnarmedPrep()
	{
		try
		{
			if (!_pendingPlayerUnarmedPrep || Mission.Current == null || _armedConflict)
			{
				if (_armedConflict)
				{
					ClearPendingPlayerUnarmedPrep();
				}
			}
			else if (!(Mission.Current.CurrentTime < _pendingPlayerUnarmedPrepAtMissionTime) && Agent.Main != null && Agent.Main.IsActive())
			{
				TryStripWeaponsForUnarmedConflict(Agent.Main);
				TrySheathWeapons(Agent.Main);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Applying delayed player unarmed prep failed: " + ex.Message);
		}
		finally
		{
			ClearPendingPlayerUnarmedPrep();
		}
	}

	private void ClearPendingPlayerUnarmedPrep()
	{
		_pendingPlayerUnarmedPrep = false;
		_pendingPlayerUnarmedPrepAtMissionTime = -1f;
	}

	private void QueuePendingPlayerRearmAfterArmedConflictEnd()
	{
		_pendingPlayerRearmAfterArmedConflictEnd = true;
		Mission current = Mission.Current;
		_pendingPlayerRearmAfterArmedConflictEndAtMissionTime = ((current != null) ? current.CurrentTime : 0f) + 0.2f;
	}

	private void QueuePendingActiveUnarmedTargetFleeIfNeeded()
	{
		try
		{
			_pendingActiveUnarmedTargetFlee = false;
			_pendingActiveUnarmedTargetFleeAgentIndex = -1;
			_pendingActiveUnarmedTargetFleeAtMissionTime = -1f;
			if (_armedConflict && _activeTargetAgentIndex >= 0 && Mission.Current != null)
			{
				Agent val = ((IEnumerable<Agent>)Mission.Current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == _activeTargetAgentIndex);
				if (val != null && val.IsActive() && ShouldFleeWhenArmedVictim(val))
				{
					_pendingActiveUnarmedTargetFlee = true;
					_pendingActiveUnarmedTargetFleeAgentIndex = val.Index;
					_pendingActiveUnarmedTargetFleeAtMissionTime = Mission.Current.CurrentTime + 0.12f;
					TryForceUnarmedBystanderToFlee(val);
					Logger.Log("SceneTaunt", $"Queued active unarmed target to flee after armed escalation. Agent={val.Name}, AgentIndex={val.Index}, ExecuteAt={_pendingActiveUnarmedTargetFleeAtMissionTime:0.###}");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Queueing active unarmed target flee failed: " + ex.Message);
		}
	}

	private void TryCommitPendingActiveUnarmedTargetFlee()
	{
		try
		{
			if (_pendingActiveUnarmedTargetFlee && Mission.Current != null && !(Mission.Current.CurrentTime < _pendingActiveUnarmedTargetFleeAtMissionTime))
			{
				Agent val = ((IEnumerable<Agent>)Mission.Current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == _pendingActiveUnarmedTargetFleeAgentIndex);
				bool flag = val != null && val.IsActive();
				bool flag2 = flag && ShouldFleeWhenArmedVictim(val);
				bool flag3 = false;
				if (flag2)
				{
					flag3 = TryRemoveAgentFromOpponentFightSide(val);
				}
				if (flag3)
				{
					TryForceUnarmedBystanderToFlee(val);
					Logger.Log("SceneTaunt", "Converted active unarmed civilian target to fleeing bystander after armed escalation delay. Agent=" + val.Name);
					return;
				}
				Logger.Log("SceneTaunt", string.Format("Skipped converting active unarmed target after delay. Agent={0}, AgentIndex={1}, Active={2}, ShouldFlee={3}, Removed={4}", ((val == null) ? null : val.Name?.ToString()) ?? "null", _pendingActiveUnarmedTargetFleeAgentIndex, flag, flag2, flag3));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Committing active unarmed target flee failed: " + ex.Message);
		}
		finally
		{
			_pendingActiveUnarmedTargetFlee = false;
			_pendingActiveUnarmedTargetFleeAgentIndex = -1;
			_pendingActiveUnarmedTargetFleeAtMissionTime = -1f;
		}
	}

	private void ClearPendingActiveUnarmedTargetFlee()
	{
		_pendingActiveUnarmedTargetFlee = false;
		_pendingActiveUnarmedTargetFleeAgentIndex = -1;
		_pendingActiveUnarmedTargetFleeAtMissionTime = -1f;
	}

	private void TryForceActiveUnarmedTargetFleeFallback()
	{
		try
		{
			if (_armedConflict && _pendingActiveUnarmedTargetFlee && Mission.Current != null && _pendingActiveUnarmedTargetFleeAgentIndex >= 0 && !(Mission.Current.CurrentTime < _pendingActiveUnarmedTargetFleeAtMissionTime))
			{
				Agent val = ((IEnumerable<Agent>)Mission.Current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == _pendingActiveUnarmedTargetFleeAgentIndex);
				if (val == null || !val.IsActive())
				{
					ClearPendingActiveUnarmedTargetFlee();
				}
				else if (!ShouldFleeWhenArmedVictim(val))
				{
					ClearPendingActiveUnarmedTargetFlee();
				}
				else if (!_opponentAgentIndices.Contains(val.Index))
				{
					TryForceUnarmedBystanderToFlee(val);
					ClearPendingActiveUnarmedTargetFlee();
				}
				else if (TryRemoveAgentFromOpponentFightSide(val))
				{
					TryForceUnarmedBystanderToFlee(val);
					ClearPendingActiveUnarmedTargetFlee();
					Logger.Log("SceneTaunt", $"Fallback-converted active unarmed civilian target to fleeing bystander after armed escalation. Agent={val.Name}, AgentIndex={val.Index}");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Fallback-converting active unarmed target after armed escalation failed: " + ex.Message);
		}
	}

	private void TryMaintainRecentlyNeutralizedFleeingCivilians()
	{
		try
		{
			if (Mission.Current == null || _recentNeutralizedFleeingCivilianUntilMissionTime.Count == 0)
			{
				return;
			}
			float currentTime = Mission.Current.CurrentTime;
			foreach (int item in _recentNeutralizedFleeingCivilianUntilMissionTime.Keys.ToList())
			{
				if (!_recentNeutralizedFleeingCivilianUntilMissionTime.TryGetValue(item, out var value) || currentTime > value)
				{
					_recentNeutralizedFleeingCivilianUntilMissionTime.Remove(item);
					continue;
				}
				Agent val = ((IEnumerable<Agent>)Mission.Current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == item);
				if (val == null || !val.IsActive() || _opponentAgentIndices.Contains(item) || !ShouldFleeWhenArmedVictim(val))
				{
					_recentNeutralizedFleeingCivilianUntilMissionTime.Remove(item);
				}
				else
				{
					TryForceUnarmedBystanderToFlee(val);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Maintaining recently neutralized fleeing civilians failed: " + ex.Message);
		}
	}

	private void TryMaintainHostileUnarmedOpponentsFleeing()
	{
		try
		{
			if (!_conflictActive || !_armedConflict)
			{
				return;
			}
			Mission current = Mission.Current;
			if (((current != null) ? current.Agents : null) == null || _opponentAgentIndices.Count == 0)
			{
				return;
			}
			foreach (int item in _opponentAgentIndices.ToList())
			{
				Agent val = ((IEnumerable<Agent>)Mission.Current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == item);
				if (val != null && val.IsActive() && ShouldFleeWhenArmedVictim(val))
				{
					TryForceUnarmedBystanderToFlee(val);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Maintaining hostile unarmed opponents fleeing failed: " + ex.Message);
		}
	}

	private void TryCommitPendingPlayerRearmAfterArmedConflictEnd()
	{
		try
		{
			if (_pendingPlayerRearmAfterArmedConflictEnd && Mission.Current != null && !(Mission.Current.CurrentTime < _pendingPlayerRearmAfterArmedConflictEndAtMissionTime) && SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement())
			{
				Agent main = Agent.Main;
				if (main != null && main.IsActive() && !IsAgentUsingRealWeapon(main))
				{
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Re-arming player after armed conflict end failed: " + ex.Message);
		}
		finally
		{
			ClearPendingPlayerRearmAfterArmedConflictEnd();
		}
	}

	private void ClearPendingPlayerRearmAfterArmedConflictEnd()
	{
		_pendingPlayerRearmAfterArmedConflictEnd = false;
		_pendingPlayerRearmAfterArmedConflictEndAtMissionTime = -1f;
	}

	private void TryMaintainMainAgentArmedPresence()
	{
		try
		{
			if (Mission.Current == null || !SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement())
			{
				return;
			}
			Campaign current = Campaign.Current;
			bool? obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				ConversationManager conversationManager = current.ConversationManager;
				obj = ((conversationManager != null) ? new bool?(conversationManager.IsConversationInProgress) : ((bool?)null));
			}
			bool? flag = obj;
			if (flag != true)
			{
				Agent main = Agent.Main;
				if (main != null && main.IsActive() && !IsAgentUsingRealWeapon(main) && IsAgentCarryingRealWeapon(main))
				{
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Maintaining main agent armed presence failed: " + ex.Message);
		}
	}

	private void TryStripWeaponsForUnarmedConflict(Agent agent)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Invalid comparison between Unknown and I4
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (agent == null || !agent.IsActive() || _cachedUnarmedConflictEquipment.ContainsKey(agent.Index))
			{
				return;
			}
			MissionEquipment val = new MissionEquipment();
			val.FillFrom(agent.Equipment);
			_cachedUnarmedConflictEquipment[agent.Index] = val;
			for (EquipmentIndex val2 = (EquipmentIndex)0; (int)val2 < 5; val2 = (EquipmentIndex)(val2 + 1))
			{
				try
				{
					agent.RemoveEquippedWeapon(val2);
				}
				catch
				{
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Stripping weapons for unarmed conflict failed: " + ex.Message);
		}
	}

	private void TryRestoreWeaponsAfterUnarmedConflict(Agent agent)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Invalid comparison between Unknown and I4
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (agent == null || !_cachedUnarmedConflictEquipment.TryGetValue(agent.Index, out var value))
			{
				return;
			}
			for (EquipmentIndex val = (EquipmentIndex)0; (int)val < 5; val = (EquipmentIndex)(val + 1))
			{
				try
				{
					MissionWeapon val2 = value[val];
					agent.EquipWeaponWithNewEntity(val, ref val2);
				}
				catch
				{
				}
			}
			_cachedUnarmedConflictEquipment.Remove(agent.Index);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Restoring weapons after unarmed conflict failed: " + ex.Message);
		}
	}

	private bool IsPlayerAttemptingWeaponDrawInput(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsActive() || agent.IsSitting() || !HasAvailableRealWeaponForEscalation(agent))
			{
				return false;
			}
			return Input.IsKeyPressed((InputKey)2) || Input.IsKeyPressed((InputKey)3) || Input.IsKeyPressed((InputKey)4) || Input.IsKeyPressed((InputKey)5) || Input.IsKeyPressed((InputKey)79) || Input.IsKeyPressed((InputKey)80) || Input.IsKeyPressed((InputKey)81) || Input.IsKeyPressed((InputKey)75) || Input.IsKeyPressed((InputKey)229) || Input.IsKeyPressed((InputKey)230);
		}
		catch
		{
			return false;
		}
	}

	private bool HasAvailableRealWeaponForEscalation(Agent agent)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Invalid comparison between Unknown and I4
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (agent == null || !agent.IsActive())
			{
				return false;
			}
			if (IsAgentCarryingRealWeapon(agent))
			{
				return true;
			}
			if (!_cachedUnarmedConflictEquipment.TryGetValue(agent.Index, out var value))
			{
				return false;
			}
			for (EquipmentIndex val = (EquipmentIndex)0; (int)val < 5; val = (EquipmentIndex)(val + 1))
			{
				if (IsMissionWeaponRealWeapon(value[val]))
				{
					return true;
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Checking player real weapon availability for escalation failed: " + ex.Message);
		}
		return false;
	}

	private void RestoreAllCachedWeapons()
	{
		foreach (int item in _cachedUnarmedConflictEquipment.Keys.ToList())
		{
			try
			{
				Mission current = Mission.Current;
				Agent val = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent x) => x != null && x.Index == item));
				if (val != null)
				{
					TryRestoreWeaponsAfterUnarmedConflict(val);
				}
			}
			catch
			{
			}
		}
		_cachedUnarmedConflictEquipment.Clear();
	}

	private void ApplyLordSceneFightConsequences(Hero targetHero)
	{
		try
		{
			PartyBase val = null;
			try
			{
				object obj;
				if (targetHero == null)
				{
					obj = null;
				}
				else
				{
					MobileParty partyBelongedTo = targetHero.PartyBelongedTo;
					obj = ((partyBelongedTo != null) ? partyBelongedTo.Party : null);
				}
				val = (PartyBase)obj;
			}
			catch
			{
				val = null;
			}
			if (val == null)
			{
				Settlement currentSettlement = Settlement.CurrentSettlement;
				val = ((currentSettlement != null) ? currentSettlement.Party : null);
			}
			LordEncounterBehavior.ApplyHostileEscalationDiplomaticConsequences(val, targetHero, "scene_taunt_lord_scene", "SceneTaunt");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Applying lord scene fight consequences failed: " + ex.Message);
		}
	}

	private void EscalateToArmedConflict(string reason, bool suppressAnnouncement = false)
	{
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Expected O, but got Unknown
		if (!_conflictActive || _armedConflict)
		{
			return;
		}
		ClearMissionFightHandlerPendingFinishTimer();
		_armedConflict = true;
		_armedConflictOccurredThisConflict = true;
		Mission current = Mission.Current;
		_lastArmedEscalationAtMissionTime = ((current != null) ? current.CurrentTime : (-1f));
		_armedCarryoverHandledInThisMission = true;
		SceneTauntBehavior.MarkArmedCarryoverForCurrentSettlement(reason);
		_blockedAiWeaponAgentIndices.Clear();
		foreach (int guardAgentIndex in _guardAgentIndices.ToList())
		{
			Mission current2 = Mission.Current;
			Agent val = ((current2 == null) ? null : ((IEnumerable<Agent>)current2.Agents)?.FirstOrDefault((Agent x) => x != null && x.Index == guardAgentIndex));
			if (val != null && val.IsActive())
			{
				AddAgentToFightSide(val, isPlayerSide: false);
			}
		}
		foreach (Agent item in EnumerateConflictAgents(includeGuards: true))
		{
			if (item != null && item.IsActive())
			{
				TryRestoreWeaponsAfterUnarmedConflict(item);
				TryAlarmAgent(item);
				TryArmAgent(item);
			}
		}
		TryConvertUnarmedCivilianOpponentsToFleeingBystanders();
		QueuePendingActiveUnarmedTargetFleeIfNeeded();
		ForceAllNonPlayerSceneAgentsMortal();
		EnsureCrimeRatingAtLeast(35f);
		AlarmNearbyBystanders();
		TryAppendPlayerBehaviorFactForArmedEscalation(reason);
		TryAppendGuardBehaviorFactsForArmedEscalation();
		_openedAsUnarmedBrawl = false;
		if (!suppressAnnouncement)
		{
			InformationManager.DisplayMessage(new InformationMessage("持械冲突爆发，守卫开始敌视你和你的同伴。", new Color(1f, 0.35f, 0.2f, 1f)));
		}
		Logger.Log("SceneTaunt", $"Escalated scene conflict to armed combat. Reason={reason}, Target={_activeTargetName}, Guards={_guardAgentIndices.Count}");
	}

	private void TryConvertUnarmedCivilianOpponentsToFleeingBystanders()
	{
		try
		{
			foreach (int item in _opponentAgentIndices.ToList())
			{
				Mission current = Mission.Current;
				Agent val = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent x) => x != null && x.Index == item));
				if ((val == null || val.Index != _activeTargetAgentIndex) && val != null && ShouldFleeWhenArmedVictim(val) && TryRemoveAgentFromOpponentFightSide(val))
				{
					TryForceUnarmedBystanderToFlee(val);
					Logger.Log("SceneTaunt", "Converted unarmed civilian opponent to fleeing bystander during armed escalation. Agent=" + val.Name);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Converting unarmed civilian opponents to fleeing bystanders failed: " + ex.Message);
		}
	}

	private bool TryRemoveAgentFromOpponentFightSide(Agent agent)
	{
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Expected O, but got Unknown
		try
		{
			if (agent == null || _fightHandler == null)
			{
				return false;
			}
			List<Agent> list = OpponentSideAgentsField?.GetValue(_fightHandler) as List<Agent>;
			Dictionary<Agent, Team> dictionary = OpponentSideOldTeamDataField?.GetValue(_fightHandler) as Dictionary<Agent, Team>;
			if (list == null || !list.Remove(agent))
			{
				return false;
			}
			_opponentAgentIndices.Remove(agent.Index);
			ReleaseArmedBystanderWatcher(agent);
			Team value = null;
			if (dictionary != null && dictionary.TryGetValue(agent, out value))
			{
				dictionary.Remove(agent);
			}
			try
			{
				CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
				object obj;
				if (component == null)
				{
					obj = null;
				}
				else
				{
					AgentNavigator agentNavigator = component.AgentNavigator;
					obj = ((agentNavigator != null) ? agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>() : null);
				}
				AlarmedBehaviorGroup val = (AlarmedBehaviorGroup)obj;
				if (val != null)
				{
					((AgentBehaviorGroup)val).DisableScriptedBehavior();
				}
			}
			catch
			{
			}
			try
			{
				if (value != null)
				{
					agent.SetTeam(new Team(value.MBTeam, (BattleSideEnum)(-1), Mission.Current, uint.MaxValue, uint.MaxValue, (Banner)null), true);
				}
			}
			catch
			{
			}
			try
			{
				if (agent.IsAIControlled)
				{
					agent.ResetEnemyCaches();
					agent.InvalidateTargetAgent();
					agent.InvalidateAIWeaponSelections();
					agent.SetWatchState((WatchState)2);
				}
			}
			catch
			{
			}
			if (Mission.Current != null && ShouldFleeWhenArmedVictim(agent))
			{
				_recentNeutralizedFleeingCivilianUntilMissionTime[agent.Index] = Mission.Current.CurrentTime + 6f;
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Removing agent from opponent fight side failed: " + ex.Message);
			return false;
		}
	}

	private static bool ShouldFleeWhenArmedVictim(Agent agent)
	{
		try
		{
			BasicCharacterObject obj = ((agent != null) ? agent.Character : null);
			CharacterObject val = (CharacterObject)(object)((obj is CharacterObject) ? obj : null);
			if (agent == null || val == null || !agent.IsHuman || !agent.IsActive() || ((BasicCharacterObject)val).IsHero)
			{
				return false;
			}
			if (IsAuthorityPhysicalAttackTarget(null, val))
			{
				return false;
			}
			return !IsAgentCarryingRealWeapon(agent);
		}
		catch
		{
			return false;
		}
	}

	private void EnsureCrimeRatingAtLeast(float targetCrimeAmount)
	{
		try
		{
			if (_suppressSettlementConsequencesForCurrentConflict)
			{
				return;
			}
			Settlement currentSettlement = Settlement.CurrentSettlement;
			IFaction val = ((currentSettlement != null) ? currentSettlement.MapFaction : null);
			if (val != null && !(targetCrimeAmount <= _appliedCrimeRatingAmount))
			{
				float num = targetCrimeAmount - _appliedCrimeRatingAmount;
				if (!(num <= 0f))
				{
					ApplySceneTauntCrimeWithDeferredCap(val, num, "scene_taunt_armed_escalation");
					_appliedCrimeRatingAmount += num;
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Ensuring crime rating for armed conflict failed: " + ex.Message);
		}
	}

	private void ForceAllNonPlayerSceneAgentsMortal()
	{
		try
		{
			foreach (Agent item in (List<Agent>)(object)Mission.Current.Agents)
			{
				if (item != null && item.IsHuman && item.IsActive() && !_playerAgentIndices.Contains(item.Index))
				{
					TryForceAgentMortal(item);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Forcing scene agents mortal failed: " + ex.Message);
		}
	}

	private static void TryForceAgentMortal(Agent agent)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		try
		{
			if (agent != null && agent.IsActive() && (int)agent.CurrentMortalityState > 0)
			{
				agent.SetMortalityState((MortalityState)0);
				Logger.Log("SceneTaunt", "Forced agent to mortal state during armed conflict. Agent=" + agent.Name);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Forcing agent mortal failed: " + ex.Message);
		}
	}

	private void AddAgentToFightSide(Agent agent, bool isPlayerSide)
	{
		try
		{
			if (agent != null && agent.IsActive() && _fightHandler != null)
			{
				ReleaseArmedBystanderWatcher(agent);
				Team team = agent.Team;
				_fightHandler.AddAgentToSide(agent, isPlayerSide);
				FixMissionFightHandlerStoredTeam(agent, isPlayerSide, team);
				_recentNeutralizedFleeingCivilianUntilMissionTime.Remove(agent.Index);
				if (isPlayerSide)
				{
					_playerAgentIndices.Add(agent.Index);
				}
				else
				{
					_opponentAgentIndices.Add(agent.Index);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "AddAgentToFightSide failed: " + ex.Message);
		}
	}

	private void FixMissionFightHandlerStoredTeam(Agent agent, bool isPlayerSide, Team originalTeam)
	{
		try
		{
			if ((isPlayerSide ? PlayerSideOldTeamDataField : OpponentSideOldTeamDataField)?.GetValue(_fightHandler) is Dictionary<Agent, Team> dictionary && agent != null && originalTeam != null)
			{
				dictionary[agent] = originalTeam;
			}
		}
		catch
		{
		}
	}

	private IEnumerable<Agent> EnumerateConflictAgents(bool includeGuards)
	{
		HashSet<int> hashSet = new HashSet<int>(_playerAgentIndices);
		hashSet.UnionWith(_opponentAgentIndices);
		if (includeGuards)
		{
			hashSet.UnionWith(_guardAgentIndices);
		}
		foreach (int item in hashSet)
		{
			Mission current = Mission.Current;
			Agent agent = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent x) => x != null && x.Index == item));
			if (agent != null)
			{
				yield return agent;
			}
		}
	}

	private void ApplyBaseConsequences(CharacterObject targetCharacter, float crimeRatingAmount)
	{
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Expected O, but got Unknown
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Expected O, but got Unknown
		if (_baseConsequencesApplied)
		{
			return;
		}
		Settlement currentSettlement = Settlement.CurrentSettlement;
		if (currentSettlement == null)
		{
			return;
		}
		if (IsSettlementCriminalConflictTarget((targetCharacter != null) ? targetCharacter.HeroObject : null, targetCharacter))
		{
			Logger.Log("SceneTaunt", $"Skipped settlement trust/crime consequences for criminal target conflict. Target={((targetCharacter != null) ? ((BasicCharacterObject)targetCharacter).Name : null)}");
			return;
		}
		_baseConsequencesApplied = true;
		try
		{
			if (RewardSystemBehavior.Instance != null)
			{
				if (targetCharacter != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(targetCharacter, out var kind))
				{
					RewardSystemBehavior.Instance.AdjustSettlementMerchantTrustForExternal(currentSettlement, kind, -10, "scene_taunt_brawl");
					InformationManager.DisplayMessage(new InformationMessage($"{currentSettlement.Name} 的{GetMerchantTrustLabel(kind)}信任 -10。", new Color(1f, 0.7f, 0.2f, 1f)));
				}
				else
				{
					RewardSystemBehavior.Instance.AdjustSettlementLocalPublicTrustForExternal(currentSettlement, -10, "scene_taunt_brawl");
					InformationManager.DisplayMessage(new InformationMessage($"{currentSettlement.Name} 的公共信任 -10。", new Color(1f, 0.7f, 0.2f, 1f)));
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Applying trust consequence failed: " + ex.Message);
		}
		try
		{
			if (currentSettlement.MapFaction != null)
			{
				float num = MathF.Max(0f, crimeRatingAmount);
				if (num > 0f)
				{
					ApplySceneTauntCrimeWithDeferredCap(currentSettlement.MapFaction, num, "scene_taunt_conflict_started");
					_appliedCrimeRatingAmount = num;
				}
			}
		}
		catch (Exception ex2)
		{
			Logger.Log("SceneTaunt", "Applying crime consequence failed: " + ex2.Message);
		}
	}

	private void EnableSettlementConsequencesForCurrentConflict(CharacterObject targetCharacter, Hero targetHero, float crimeRatingAmount, string reason)
	{
		if (_suppressSettlementConsequencesForCurrentConflict)
		{
			_suppressSettlementConsequencesForCurrentConflict = false;
			ApplyBaseConsequences(targetCharacter, crimeRatingAmount);
			if (SceneTauntBehavior.IsSceneLordTauntTarget(targetHero))
			{
				ApplyLordSceneFightConsequences(targetHero);
			}
			Logger.Log("SceneTaunt", "Settlement crime/trust consequences were enabled for current conflict. Reason=" + reason + ", Target=" + (((targetCharacter == null) ? null : ((object)((BasicCharacterObject)targetCharacter).Name)?.ToString()) ?? ((targetHero == null) ? null : ((object)targetHero.Name)?.ToString()) ?? "N/A"));
		}
	}

	private static float ApplySceneTauntCrimeWithCap(IFaction faction, float requestedAmount)
	{
		try
		{
			if (faction == null)
			{
				return 0f;
			}
			float num = MathF.Max(0f, requestedAmount);
			if (num <= 0f)
			{
				return 0f;
			}
			float num2 = MathF.Max(0f, faction.MainHeroCrimeRating);
			float num3 = MathF.Max(0f, 59f - num2);
			float num4 = MathF.Min(num, num3);
			if (num4 <= 0f)
			{
				Logger.Log("SceneTaunt", $"Crime increase skipped because faction crime is already at cap. Faction={faction.Name}, Current={num2:0.##}, Cap={59f:0.##}");
				return 0f;
			}
			ChangeCrimeRatingAction.Apply(faction, num4, true);
			Logger.Log("SceneTaunt", $"Applied capped scene-taunt crime. Faction={faction.Name}, Requested={num:0.##}, Applied={num4:0.##}, Result={MathF.Min(59f, num2 + num4):0.##}");
			return num4;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Applying capped scene-taunt crime failed: " + ex.Message);
			return 0f;
		}
	}

	private void ApplySceneTauntCrimeWithDeferredCap(IFaction faction, float requestedAmount, string reason)
	{
		try
		{
			float num = MathF.Max(0f, requestedAmount);
			if (faction != null && !(num <= 0f))
			{
				float num2 = ApplySceneTauntCrimeWithCap(faction, num);
				float num3 = MathF.Max(0f, num - num2);
				if (num3 > 0f)
				{
					QueueDeferredCrimeForFaction(faction, num3, reason);
				}
				if (num2 > 0f || num3 > 0f)
				{
					SceneTauntBehavior.TryShowTrackedCrimeTotalMessageForExternal(faction);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Applying deferred-cap scene-taunt crime failed: " + ex.Message);
		}
	}

	private void QueueDeferredCrimeForFaction(IFaction faction, float amount, string reason)
	{
		SceneTauntBehavior.QueueDeferredCrimeForExternal(faction, amount, reason);
	}

	private void TryApplyArmedNpcKnockdownConsequences(Agent affectedAgent, Agent affectorAgent, AgentState agentState)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Invalid comparison between Unknown and I4
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Invalid comparison between Unknown and I4
		try
		{
			if (_conflictActive && _armedConflict && affectedAgent != null && affectedAgent.IsHuman && ((int)agentState == 4 || (int)agentState == 3) && !_playerAgentIndices.Contains(affectedAgent.Index) && _penalizedArmedKnockdownAgentIndices.Add(affectedAgent.Index) && (affectorAgent == Agent.Main || (affectorAgent != null && _playerAgentIndices.Contains(affectorAgent.Index))))
			{
				BasicCharacterObject character = affectedAgent.Character;
				CharacterObject victimCharacter = (CharacterObject)(object)((character is CharacterObject) ? character : null);
				ApplyPerNpcKnockdownConsequences(affectedAgent, victimCharacter, affectedAgent.Name?.ToString());
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Applying per-NPC armed knockdown consequences failed: " + ex.Message);
		}
	}

	private void ApplyPerNpcKnockdownConsequences(Agent victimAgent, CharacterObject victimCharacter, string victimName)
	{
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Expected O, but got Unknown
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Expected O, but got Unknown
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Expected O, but got Unknown
		Settlement currentSettlement = Settlement.CurrentSettlement;
		if (currentSettlement == null)
		{
			return;
		}
		string text = victimName ?? ((victimCharacter == null) ? null : ((object)((BasicCharacterObject)victimCharacter).Name)?.ToString()) ?? "目标";
		if (IsSettlementCriminalConflictTarget((victimCharacter != null) ? victimCharacter.HeroObject : null, victimCharacter))
		{
			TryRewardSettlementTrustForCriminalKnockdown(currentSettlement, text);
			Hero val = TryResolveCriminalOwnerHeroFromAgent(victimAgent);
			if (val != null)
			{
				TryApplyCriminalOwnerPenalty(val, text);
			}
			Logger.Log("SceneTaunt", "Handled criminal target knockdown consequences. Victim=" + text + ", Owner=" + (((val == null) ? null : ((object)val.Name)?.ToString()) ?? "N/A"));
			return;
		}
		try
		{
			if (RewardSystemBehavior.Instance != null)
			{
				if (victimCharacter != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(victimCharacter, out var kind))
				{
					RewardSystemBehavior.Instance.AdjustSettlementMerchantTrustForExternal(currentSettlement, kind, -20, "scene_taunt_armed_knockdown");
					InformationManager.DisplayMessage(new InformationMessage($"击倒 {text}：{currentSettlement.Name} 的{GetMerchantTrustLabel(kind)}信任 -{20}。", new Color(1f, 0.7f, 0.2f, 1f)));
				}
				else
				{
					RewardSystemBehavior.Instance.AdjustSettlementLocalPublicTrustForExternal(currentSettlement, -20, "scene_taunt_armed_knockdown");
					InformationManager.DisplayMessage(new InformationMessage($"击倒 {text}：{currentSettlement.Name} 的公共信任 -{20}。", new Color(1f, 0.7f, 0.2f, 1f)));
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Applying per-NPC knockdown trust consequence failed: " + ex.Message);
		}
		try
		{
			if (currentSettlement.MapFaction != null)
			{
				ApplySceneTauntCrimeWithDeferredCap(currentSettlement.MapFaction, 20f, "scene_taunt_armed_knockdown");
				_appliedCrimeRatingAmount += 20f;
				InformationManager.DisplayMessage(new InformationMessage($"击倒 {text}：累计犯罪度 +{20f:0.#}。超出 59 的部分会在离开定居点后再结算。", new Color(1f, 0.45f, 0.2f, 1f)));
			}
		}
		catch (Exception ex2)
		{
			Logger.Log("SceneTaunt", "Applying per-NPC knockdown crime consequence failed: " + ex2.Message);
		}
	}

	private void AlarmNearbyBystanders()
	{
		HashSet<int> hashSet = new HashSet<int>(_playerAgentIndices);
		hashSet.UnionWith(_opponentAgentIndices);
		hashSet.UnionWith(_guardAgentIndices);
		Agent main = Agent.Main;
		try
		{
			foreach (Agent item in (List<Agent>)(object)Mission.Current.Agents)
			{
				if (item != null && item.IsHuman && item.IsActive() && !hashSet.Contains(item.Index) && IsAgentWithinArmedBystanderReactionRadius(item, main))
				{
					TryAlarmAgent(item);
					if (!TryJoinArmedBystanderToConflict(item))
					{
						TryForceUnarmedBystanderToFlee(item);
					}
				}
			}
		}
		catch
		{
		}
	}

	private void TryMaintainArmedBystanderReactions()
	{
		try
		{
			if (!_conflictActive || !_armedConflict)
			{
				return;
			}
			Mission current = Mission.Current;
			if (((current != null) ? current.Agents : null) == null)
			{
				return;
			}
			float currentTime = Mission.Current.CurrentTime;
			if (_lastArmedBystanderReactionRefreshAtMissionTime >= 0f && currentTime - _lastArmedBystanderReactionRefreshAtMissionTime < 0.5f)
			{
				return;
			}
			_lastArmedBystanderReactionRefreshAtMissionTime = currentTime;
			HashSet<int> hashSet = new HashSet<int>(_playerAgentIndices);
			hashSet.UnionWith(_opponentAgentIndices);
			hashSet.UnionWith(_guardAgentIndices);
			Agent main = Agent.Main;
			foreach (Agent item in (List<Agent>)(object)Mission.Current.Agents)
			{
				if (item != null && item.IsHuman && item.IsActive() && !hashSet.Contains(item.Index) && IsAgentWithinArmedBystanderReactionRadius(item, main) && !TryJoinArmedBystanderToConflict(item))
				{
					TryForceUnarmedBystanderToFlee(item);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Maintaining armed bystander reactions failed: " + ex.Message);
		}
	}

	private static bool IsAgentWithinArmedBystanderReactionRadius(Agent agent, Agent main)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (agent == null || main == null || !main.IsActive())
			{
				return false;
			}
			float num = 400f;
			Vec3 position = agent.Position;
			Vec2 asVec = ((Vec3)(ref position)).AsVec2;
			position = main.Position;
			return ((Vec2)(ref asVec)).DistanceSquared(((Vec3)(ref position)).AsVec2) <= num;
		}
		catch
		{
			return false;
		}
	}

	private bool TryJoinArmedBystanderToConflict(Agent agent)
	{
		try
		{
			if (!ShouldJoinArmedBystanderToConflict(agent))
			{
				return false;
			}
			ReleaseArmedBystanderWatcher(agent);
			if (!_opponentAgentIndices.Contains(agent.Index))
			{
				AddAgentToFightSide(agent, isPlayerSide: false);
			}
			TryForceAgentMortal(agent);
			TryAlarmAgent(agent);
			TryArmAgent(agent);
			TryWakeArmedBystanderCombatAi(agent);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Joining armed bystander to conflict failed: " + ex.Message);
			return false;
		}
	}

	private static bool ShouldJoinArmedBystanderToConflict(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive() || agent.IsMainAgent || !agent.IsAIControlled)
			{
				return false;
			}
			BasicCharacterObject character = agent.Character;
			if (SceneTauntBehavior.IsChildSceneProtectedTarget((CharacterObject)(object)((character is CharacterObject) ? character : null)))
			{
				return false;
			}
			return IsAgentCarryingRealWeapon(agent);
		}
		catch
		{
			return false;
		}
	}

	private static void TryForceUnarmedBystanderToFlee(Agent agent)
	{
		try
		{
			if (!ShouldForceUnarmedBystanderToFlee(agent))
			{
				return;
			}
			agent.SetLookAgent((Agent)null);
			agent.SetMaximumSpeedLimit(-1f, false);
			agent.DisableScriptedMovement();
			if (TryForceUnarmedBystanderDirectRetreat(agent))
			{
				return;
			}
			AlarmedBehaviorGroup val = EnsureAlarmedBehaviorGroup(agent);
			if (val != null)
			{
				FleeBehavior val2 = ((AgentBehaviorGroup)val).GetBehavior<FleeBehavior>();
				if (val2 == null)
				{
					val2 = ((AgentBehaviorGroup)val).AddBehavior<FleeBehavior>();
				}
				if ((object)((AgentBehaviorGroup)val).ScriptedBehavior != val2)
				{
					((AgentBehaviorGroup)val).SetScriptedBehavior<FleeBehavior>();
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Forcing unarmed bystander to flee failed: " + ex.Message);
		}
	}

	private static bool TryForceUnarmedBystanderDirectRetreat(Agent agent)
	{
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Agent main = Agent.Main;
			Mission current = Mission.Current;
			CampaignAgentComponent val = ((agent != null) ? agent.GetComponent<CampaignAgentComponent>() : null);
			AgentNavigator val2 = ((val != null) ? val.AgentNavigator : null) ?? ((val != null) ? val.CreateAgentNavigator() : null);
			if (agent == null || main == null || !main.IsActive() || (NativeObject)(object)((current != null) ? current.Scene : null) == (NativeObject)null || val2 == null)
			{
				return false;
			}
			Vec3 position = agent.Position;
			Vec2 asVec = ((Vec3)(ref position)).AsVec2;
			position = main.Position;
			Vec2 asVec2 = ((Vec3)(ref position)).AsVec2;
			Vec2 val3 = asVec - asVec2;
			if (((Vec2)(ref val3)).LengthSquared < 0.04f)
			{
				MatrixFrame frame = agent.Frame;
				val3 = ((Vec3)(ref frame.rotation.f)).AsVec2;
			}
			if (((Vec2)(ref val3)).LengthSquared < 0.04f)
			{
				((Vec2)(ref val3))._002Ector(1f, 0f);
			}
			((Vec2)(ref val3)).Normalize();
			WorldPosition val4 = WorldPosition.Invalid;
			float num = float.MinValue;
			WorldPosition val5 = default(WorldPosition);
			for (int i = 0; i < 16; i++)
			{
				bool flag = i % 2 == 0;
				Vec3 randomPositionAroundPoint = current.GetRandomPositionAroundPoint(agent.Position, 4f, 14f, flag);
				((WorldPosition)(ref val5))._002Ector(current.Scene, randomPositionAroundPoint);
				if (((WorldPosition)(ref val5)).GetNearestNavMesh() == UIntPtr.Zero)
				{
					continue;
				}
				Vec2 val6 = ((WorldPosition)(ref val5)).AsVec2 - asVec;
				if (((Vec2)(ref val6)).LengthSquared < 0.25f)
				{
					continue;
				}
				((Vec2)(ref val6)).Normalize();
				float num2 = Vec2.DotProduct(val6, val3);
				if (!(num2 < 0.2f))
				{
					Vec2 asVec3 = ((WorldPosition)(ref val5)).AsVec2;
					float num3 = ((Vec2)(ref asVec3)).DistanceSquared(asVec2) + num2 * 25f;
					if (num3 > num)
					{
						val4 = val5;
						num = num3;
					}
				}
			}
			if (num <= 0f)
			{
				return false;
			}
			Vec2 val7 = ((WorldPosition)(ref val4)).AsVec2 - asVec;
			float num4 = ((((Vec2)(ref val7)).LengthSquared > 0.04f) ? ((Vec2)(ref val7)).RotationInRadians : ((Vec2)(ref val3)).RotationInRadians);
			val2.SetTargetFrame(val4, num4, 0.6f, -10f, (AIScriptedFrameFlags)10, false);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Forcing direct retreat target failed: " + ex.Message);
			return false;
		}
	}

	private static bool ShouldForceUnarmedBystanderToFlee(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive() || agent.IsMainAgent || !agent.IsAIControlled)
			{
				return false;
			}
			return !IsAgentCarryingRealWeapon(agent);
		}
		catch
		{
			return false;
		}
	}

	private static AlarmedBehaviorGroup EnsureAlarmedBehaviorGroup(Agent agent)
	{
		try
		{
			CampaignAgentComponent val = ((agent != null) ? agent.GetComponent<CampaignAgentComponent>() : null);
			if (val == null)
			{
				return null;
			}
			AgentNavigator val2 = val.AgentNavigator ?? val.CreateAgentNavigator();
			if (val2 == null)
			{
				return null;
			}
			AlarmedBehaviorGroup behaviorGroup = val2.GetBehaviorGroup<AlarmedBehaviorGroup>();
			if (behaviorGroup == null)
			{
				try
				{
					val2.AddBehaviorGroup<DailyBehaviorGroup>();
				}
				catch
				{
				}
				try
				{
					val2.AddBehaviorGroup<InterruptingBehaviorGroup>();
				}
				catch
				{
				}
				val2.AddBehaviorGroup<AlarmedBehaviorGroup>();
				behaviorGroup = val2.GetBehaviorGroup<AlarmedBehaviorGroup>();
			}
			if (behaviorGroup != null && !((AgentBehaviorGroup)behaviorGroup).HasBehavior<FleeBehavior>())
			{
				((AgentBehaviorGroup)behaviorGroup).AddBehavior<FleeBehavior>();
			}
			return behaviorGroup;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Ensuring alarmed behavior group failed: " + ex.Message);
			return null;
		}
	}

	private static void TryWakeArmedBystanderCombatAi(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsActive() || !agent.IsAIControlled)
			{
				return;
			}
			agent.DisableScriptedMovement();
		}
		catch
		{
		}
		try
		{
			agent.ResetEnemyCaches();
			agent.InvalidateTargetAgent();
			agent.InvalidateAIWeaponSelections();
			agent.SetWatchState((WatchState)2);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Waking armed bystander combat AI failed: " + ex.Message);
		}
	}

	private static bool IsAgentCarryingRealWeapon(Agent agent)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Invalid comparison between Unknown and I4
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		if (agent == null || !agent.IsActive())
		{
			return false;
		}
		try
		{
			for (EquipmentIndex val = (EquipmentIndex)0; (int)val < 5; val = (EquipmentIndex)(val + 1))
			{
				if (IsMissionWeaponRealWeapon(agent.Equipment[val]))
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

	private static bool TryForceArmedCivilianBystanderToFlee(Agent agent)
	{
		try
		{
			if (!ShouldForceArmedCivilianBystanderToFlee(agent))
			{
				return false;
			}
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			AgentNavigator val = ((component != null) ? component.AgentNavigator : null);
			if (val == null)
			{
				return false;
			}
			AlarmedBehaviorGroup behaviorGroup = val.GetBehaviorGroup<AlarmedBehaviorGroup>();
			if (behaviorGroup == null)
			{
				return false;
			}
			if (!((AgentBehaviorGroup)behaviorGroup).HasBehavior<FleeBehavior>())
			{
				((AgentBehaviorGroup)behaviorGroup).AddBehavior<FleeBehavior>();
			}
			((AgentBehaviorGroup)behaviorGroup).SetScriptedBehavior<FleeBehavior>();
			Logger.Log("SceneTaunt", "Forced armed civilian bystander to flee. Agent=" + agent.Name);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Forcing armed civilian bystander to flee failed: " + ex.Message);
			return false;
		}
	}

	private static bool ShouldForceArmedCivilianBystanderToFlee(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive() || agent.IsMainAgent || !agent.IsAIControlled || !IsAgentUsingRealWeapon(agent))
			{
				return false;
			}
			BasicCharacterObject character = agent.Character;
			CharacterObject val = (CharacterObject)(object)((character is CharacterObject) ? character : null);
			if (val == null || IsGuardLikeCharacter(val) || SceneTauntBehavior.IsSceneLordTauntTarget(val.HeroObject))
			{
				return false;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private void TryForceArmedBystanderToWatchPlayer(Agent agent)
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (ShouldForceArmedBystanderToWatchPlayer(agent))
			{
				if (!_opponentAgentIndices.Contains(agent.Index))
				{
					AddAgentToFightSide(agent, isPlayerSide: false);
					TryForceAgentMortal(agent);
					TryAlarmAgent(agent);
				}
				agent.SetWatchState((WatchState)2);
				agent.SetMaximumSpeedLimit(0f, false);
				WorldPosition worldPosition = agent.GetWorldPosition();
				agent.SetScriptedPosition(ref worldPosition, false, (AIScriptedFrameFlags)0);
				_armedBystanderWatcherIndices.Add(agent.Index);
				Logger.Log("SceneTaunt", "Frozen armed bystander inside player conflict. Agent=" + agent.Name);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Freezing armed bystander inside player conflict failed: " + ex.Message);
		}
	}

	private static bool ShouldForceArmedBystanderToWatchPlayer(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive() || agent.IsMainAgent || !agent.IsAIControlled || !IsAgentUsingRealWeapon(agent))
			{
				return false;
			}
			return !ShouldForceArmedCivilianBystanderToFlee(agent);
		}
		catch
		{
			return false;
		}
	}

	private void ReleaseArmedBystanderWatcher(Agent agent)
	{
		try
		{
			if (agent != null && _armedBystanderWatcherIndices.Remove(agent.Index) && agent.IsActive())
			{
				agent.SetLookAgent((Agent)null);
				agent.SetMaximumSpeedLimit(-1f, false);
				agent.DisableScriptedMovement();
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Releasing armed bystander watcher failed: " + ex.Message);
		}
	}

	private void ReleaseAllArmedBystanderWatchers()
	{
		foreach (int item in _armedBystanderWatcherIndices.ToList())
		{
			try
			{
				Mission current = Mission.Current;
				Agent agent = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent x) => x != null && x.Index == item));
				ReleaseArmedBystanderWatcher(agent);
			}
			catch
			{
			}
		}
		_armedBystanderWatcherIndices.Clear();
	}

	private static void TryAlarmAgent(Agent agent)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		if (agent == null)
		{
			return;
		}
		try
		{
			if (!agent.IsMainAgent)
			{
				AgentFlag agentFlags = agent.GetAgentFlags();
				agent.SetAgentFlags((AgentFlag)(agentFlags | 0x10000));
			}
		}
		catch
		{
		}
		try
		{
			AlarmedBehaviorGroup.AlarmAgent(agent);
		}
		catch
		{
		}
		try
		{
			agent.SetAlarmState((AIStateFlag)3);
		}
		catch
		{
		}
	}

	private static void TrySheathWeapons(Agent agent)
	{
		try
		{
			if (agent != null)
			{
				agent.TryToSheathWeaponInHand((HandIndex)0, (WeaponWieldActionType)1);
			}
		}
		catch
		{
		}
		try
		{
			if (agent != null)
			{
				agent.TryToSheathWeaponInHand((HandIndex)1, (WeaponWieldActionType)1);
			}
		}
		catch
		{
		}
	}

	private static void TryArmAgent(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		try
		{
			agent.WieldInitialWeapons((WeaponWieldActionType)2, (InitialWeaponEquipPreference)0);
		}
		catch
		{
		}
		if (!IsAgentUsingRealWeapon(agent))
		{
			TryGiveFallbackSoldierWeapon(agent);
		}
		try
		{
			agent.SetWatchState((WatchState)2);
		}
		catch
		{
		}
	}

	private static void TryGiveFallbackSoldierWeapon(Agent agent)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Invalid comparison between Unknown and I4
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		if (!ShouldReceiveFallbackSoldierWeapon(agent))
		{
			return;
		}
		try
		{
			Game current = Game.Current;
			object obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				MBObjectManager objectManager = current.ObjectManager;
				obj = ((objectManager != null) ? objectManager.GetObject<ItemObject>("iron_spatha_sword_t2") : null);
			}
			ItemObject val = (ItemObject)obj;
			if (val != null)
			{
				EquipmentIndex val2 = FindFallbackWeaponSlot(agent);
				if ((int)val2 != -1)
				{
					IAgentOriginBase origin = agent.Origin;
					MissionWeapon val3 = default(MissionWeapon);
					((MissionWeapon)(ref val3))._002Ector(val, (ItemModifier)null, (origin != null) ? origin.Banner : null);
					agent.EquipWeaponWithNewEntity(val2, ref val3);
					agent.TryToWieldWeaponInSlot(val2, (WeaponWieldActionType)1, false);
					Logger.Log("SceneTaunt", $"Granted fallback sword to scene soldier. Agent={agent.Name}, Slot={val2}");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Granting fallback soldier weapon failed: " + ex.Message);
		}
	}

	private static bool ShouldReceiveFallbackSoldierWeapon(Agent agent)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Invalid comparison between Unknown and I4
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Invalid comparison between Unknown and I4
		try
		{
			if (agent == null || !agent.IsActive() || agent.IsMount)
			{
				return false;
			}
			BasicCharacterObject character = agent.Character;
			CharacterObject val = (CharacterObject)(object)((character is CharacterObject) ? character : null);
			if (val == null)
			{
				return false;
			}
			Occupation occupation = val.Occupation;
			Occupation val2 = occupation;
			if ((int)val2 == 7 || val2 - 23 <= 1)
			{
				return true;
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	private static EquipmentIndex FindFallbackWeaponSlot(Agent agent)
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Invalid comparison between Unknown and I4
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Invalid comparison between Unknown and I4
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			for (EquipmentIndex val = (EquipmentIndex)0; (int)val < 5; val = (EquipmentIndex)(val + 1))
			{
				MissionWeapon val2 = agent.Equipment[val];
				if (((MissionWeapon)(ref val2)).IsEmpty)
				{
					return val;
				}
			}
			for (EquipmentIndex val3 = (EquipmentIndex)0; (int)val3 < 5; val3 = (EquipmentIndex)(val3 + 1))
			{
				if (!IsMissionWeaponRealWeapon(agent.Equipment[val3]))
				{
					return val3;
				}
			}
			return (EquipmentIndex)3;
		}
		catch
		{
			return (EquipmentIndex)(-1);
		}
	}

	private static bool IsAgentUsingRealWeapon(Agent agent)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		if (agent == null || !agent.IsActive())
		{
			return false;
		}
		try
		{
			EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
			if (IsRealWeaponWieldedSlot(agent, primaryWieldedItemIndex))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			EquipmentIndex offhandWieldedItemIndex = agent.GetOffhandWieldedItemIndex();
			return IsRealWeaponWieldedSlot(agent, offhandWieldedItemIndex);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsRealWeaponWieldedSlot(Agent agent, EquipmentIndex equipmentIndex)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Invalid comparison between Unknown and I4
		try
		{
			if (agent == null || (int)equipmentIndex == -1 || (int)equipmentIndex < 0 || (int)equipmentIndex >= 5)
			{
				return false;
			}
			return IsMissionWeaponRealWeapon(agent.Equipment[equipmentIndex]);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsMissionWeaponRealWeapon(MissionWeapon missionWeapon)
	{
		try
		{
			WeaponComponentData currentUsageItem = ((MissionWeapon)(ref missionWeapon)).CurrentUsageItem;
			return currentUsageItem != null && !currentUsageItem.IsShield;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsWeaponComponentRealWeapon(WeaponComponentData attackerWeapon)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
		try
		{
			return attackerWeapon != null && !attackerWeapon.IsShield && (int)attackerWeapon.WeaponClass > 0;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsMissionWeaponRealWeapon(EquipmentElement equipmentElement)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Invalid comparison between Unknown and I4
		try
		{
			ItemObject item = ((EquipmentElement)(ref equipmentElement)).Item;
			if (item == null)
			{
				return false;
			}
			WeaponComponentData primaryWeapon = item.PrimaryWeapon;
			return primaryWeapon != null && !primaryWeapon.IsShield && (int)item.Type != 8;
		}
		catch
		{
			return false;
		}
	}

	private static void AddUniqueAgent(List<Agent> agents, Agent agent)
	{
		if (agent != null && !agents.Contains(agent))
		{
			agents.Add(agent);
		}
	}

	private static string GetMerchantTrustLabel(RewardSystemBehavior.SettlementMerchantKind kind)
	{
		if (1 == 0)
		{
		}
		string result = kind switch
		{
			RewardSystemBehavior.SettlementMerchantKind.Weapon => "武器市场", 
			RewardSystemBehavior.SettlementMerchantKind.Armor => "盔甲市场", 
			RewardSystemBehavior.SettlementMerchantKind.Horse => "马匹市场", 
			RewardSystemBehavior.SettlementMerchantKind.Goods => "杂货市场", 
			_ => "市场", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private void OnConflictFinished(bool playerWon)
	{
		Logger.Log("SceneTaunt", $"Scene taunt conflict ended. PlayerWon={playerWon}, Armed={_armedConflict}, Target={_activeTargetName}, Key={_activeTargetKey}");
		bool flag = false;
		bool flag2 = false;
		try
		{
			flag = _armedConflictOccurredThisConflict && (Agent.Main == null || !Agent.Main.IsActive());
			flag2 = _armedConflict && !flag && SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement() && Agent.Main != null && Agent.Main.IsActive();
		}
		catch
		{
			flag = false;
			flag2 = false;
		}
		ClearRuntimeState(flag);
		if (flag2)
		{
			QueuePendingPlayerRearmAfterArmedConflictEnd();
		}
	}

	private void ClearRuntimeState(bool preserveArmedDefeatState = false)
	{
		ReleaseAllArmedBystanderWatchers();
		_armedEscalationBehaviorFactRolledAgentIndices.Clear();
		RestoreAllCachedWeapons();
		_conflictActive = false;
		_armedConflict = false;
		_baseConsequencesApplied = false;
		_appliedCrimeRatingAmount = 0f;
		_activeTargetKey = "";
		_activeTargetName = "";
		_activeTargetAgentIndex = -1;
		_openedAsUnarmedBrawl = false;
		_openedFromVerbalTaunt = false;
		_suppressSettlementConsequencesForCurrentConflict = false;
		_sceneAttackReleaseSuppressed = false;
		_pendingImmediateUnarmedFightEnd = false;
		_pendingImmediateUnarmedFightEndPlayerWon = false;
		_armedCarryoverSceneInitialized = false;
		_armedCarryoverNoAuthoritySceneNotified = false;
		_lastArmedCarryoverAttemptAtMissionTime = -1f;
		_pendingActiveUnarmedTargetFlee = false;
		_pendingActiveUnarmedTargetFleeAgentIndex = -1;
		_pendingActiveUnarmedTargetFleeAtMissionTime = -1f;
		ClearPendingPlayerUnarmedPrep();
		ClearPendingPlayerRearmAfterArmedConflictEnd();
		_sceneNotableRecentHitNonLethal.Clear();
		_sceneNotableDeferredBattleDeathCandidates.Clear();
		_playerAgentIndices.Clear();
		_opponentAgentIndices.Clear();
		_guardAgentIndices.Clear();
		_blockedAiWeaponAgentIndices.Clear();
		_penalizedArmedKnockdownAgentIndices.Clear();
		if (!preserveArmedDefeatState)
		{
			_pendingPlayerBattleDeathAfterMission = false;
			_pendingPlayerBattleDeathDecisionCaptured = false;
			_pendingPlayerBattleDeathKiller = null;
			_armedConflictOccurredThisConflict = false;
			_armedDefeatOutcomeHandled = false;
			_armedDefeatWasCriminalConflict = false;
		}
	}

	private void RememberSceneNotableHitLethality(Agent affectedAgent, WeaponComponentData attackerWeapon, in Blow blow, float damagedHp)
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Invalid comparison between Unknown and I4
		try
		{
			if (!_conflictActive || damagedHp <= 0f || affectedAgent == null || !affectedAgent.IsHuman)
			{
				return;
			}
			BasicCharacterObject character = affectedAgent.Character;
			CharacterObject val = (CharacterObject)(object)((character is CharacterObject) ? character : null);
			Hero val2 = ((val != null) ? val.HeroObject : null);
			if (SceneTauntBehavior.IsSceneNotableTauntTarget(val2))
			{
				bool flag = (int)blow.DamageType == 2 || !IsWeaponComponentRealWeapon(attackerWeapon);
				_sceneNotableRecentHitNonLethal[val2] = flag;
				if (flag)
				{
					_sceneNotableDeferredBattleDeathCandidates.Remove(val2);
				}
				else
				{
					_sceneNotableDeferredBattleDeathCandidates.Add(val2);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Recording scene notable hit lethality failed: " + ex.Message);
		}
	}

	private bool ShouldSuppressSceneNotableDeath(Hero hero)
	{
		try
		{
			bool value = default(bool);
			return hero != null && _conflictActive && _sceneNotableRecentHitNonLethal.TryGetValue(hero, out value) && value;
		}
		catch
		{
			return false;
		}
	}

	private bool ShouldDeferSceneNotableBattleDeath(Hero hero)
	{
		try
		{
			return hero != null && _conflictActive && _sceneNotableDeferredBattleDeathCandidates.Contains(hero);
		}
		catch
		{
			return false;
		}
	}

	private static bool RollDeferredPlayerBattleDeath(float deathProbability)
	{
		float num = deathProbability;
		if (num < 0f)
		{
			num = 0f;
		}
		if (num > 1f)
		{
			num = 1f;
		}
		return MBRandom.RandomFloat <= num;
	}

	private void TryQueuePendingPlayerBattleDeathOutcome(Agent affectedAgent, Agent affectorAgent, AgentState agentState)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Invalid comparison between Unknown and I4
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Invalid comparison between Unknown and I4
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Invalid comparison between Unknown and I4
		try
		{
			if (_conflictActive && affectedAgent != null && affectedAgent.IsMainAgent && ((int)agentState == 4 || (int)agentState == 3))
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
				if (val == null && affectorAgent == Agent.Main)
				{
					val = Hero.MainHero;
				}
				if (val != null)
				{
					_pendingPlayerBattleDeathKiller = val;
				}
				if (_armedConflictOccurredThisConflict && _pendingPlayerBattleDeathAfterMission)
				{
					SceneTauntBehavior.QueuePendingMainHeroBattleDeathForExternal(_pendingPlayerBattleDeathKiller, ((int)agentState == 4) ? "scene_taunt_player_killed" : "scene_taunt_player_unconscious_deathmark");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Queueing pending player battle death outcome failed: " + ex.Message);
		}
	}
}
