using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using AnimusForge.SiegeAftermathIntervention;
using Helpers;
using HarmonyLib;
using SandBox;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.AgentBehaviors;
using SandBox.View.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.View.VisualOrders.Orders;
using TaleWorlds.MountAndBlade.View.VisualOrders.Orders.ToggleOrders;
using TaleWorlds.MountAndBlade.View.VisualOrders.OrderSets;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.FormOrders;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.MovementOrders;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.ToggleOrders;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public class SiegeAiInterventionBehavior : CampaignBehaviorBase
{
	private enum InterventionMode
	{
		None,
		WaitingDecision,
		MercyRelief,
		Plunder,
		Massacre
	}


	private sealed class MarketLootCandidate
	{
		public EquipmentElement EquipmentElement;

		public int Amount;
	}

	private sealed class PlunderInteraction
	{
		public int TargetAgentIndex;

		public int SoldierAgentIndex;

		public float StartedAt;

		public float TalkStartedAt = -1f;
	}

	private sealed class BannerBearerTroopStack
	{
		public CharacterObject Troop;

		public int Available;

		public int SourceOrder;
	}

	private sealed class AmbientReactionRequest
	{
		public SiegeInterventionActionKind Action;

		public bool AlliedSoldier;

		public int AgentIndex;

		public int DirectAgentIndex;

		public int FocusAgentIndex;

		public string FocusName;

		public float NotBeforeTime;
	}

	private sealed class CivilianGatherInteraction
	{
		public int MessengerAgentIndex;

		public int TargetAgentIndex;

		public float StartedAt;

		public float TalkStartedAt = -1f;

		public float TalkSeconds;
	}

	private sealed class InterventionMissionBehavior : MissionLogic, IAgentStateDecider, IMissionBehavior
	{
		public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

		public override void AfterStart()
		{
			base.AfterStart();
			SiegeAiInterventionBehavior.OnInterventionMissionAfterStart(base.Mission);
		}

		public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon attackerWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
		{
			if (affectorAgent == Agent.Main)
			{
				if (!SiegeAiInterventionBehavior.TryHandlePlayerAttackForIntervention(affectedAgent, SiegeLocalAttackProfile.PlayerAgentHitBridgeSource, Math.Max(0, blow.InflictedDamage)))
				{
					SiegeAiInterventionBehavior.TryHandleFriendlyHitOnAlliedSoldier(affectedAgent, SiegeLocalAttackProfile.PlayerAgentHitBridgeSource, 0f);
				}
			}
		}

		public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
		{
			base.OnScoreHit(affectedAgent, affectorAgent, attackerWeapon, isBlocked, isSiegeEngineHit, in blow, in collisionData, damagedHp, hitDistance, shotDifficulty);
			if (damagedHp > 0f && affectorAgent == Agent.Main)
			{
				if (!SiegeAiInterventionBehavior.TryHandlePlayerAttackForIntervention(affectedAgent, SiegeLocalAttackProfile.PlayerScoreHitBridgeSource, damagedHp))
				{
					SiegeAiInterventionBehavior.TryHandleFriendlyHitOnAlliedSoldier(affectedAgent, SiegeLocalAttackProfile.PlayerScoreHitBridgeSource, damagedHp);
				}
			}
		}

		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
		{
			SiegeAiInterventionBehavior.OnInterventionAgentRemoved(affectedAgent, affectorAgent, agentState);
		}

		public AgentState GetAgentState(Agent effectedAgent, float deathProbability, out bool usedSurgery)
		{
			usedSurgery = false;
			try
			{
				if (SiegeAiInterventionBehavior.ShouldForceInterventionNotableUnconscious(effectedAgent))
				{
					return AgentState.Unconscious;
				}
			}
			catch
			{
			}
			float clamped = MathF.Max(0f, MathF.Min(1f, deathProbability));
			return MBRandom.RandomFloat <= clamped ? AgentState.Killed : AgentState.Unconscious;
		}

		public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
		{
			canPlayerLeave = true;
			return null;
		}
	}



	private const int AutoSummonCount = SiegeInterventionEntryProfile.DefaultAutoSummonCount;
	private const int MaxSummonPerAction = SiegeInterventionEntryProfile.MaxSummonPerAction;
	private const int NonHeroPlunderMinGold = SiegeLootAccountingProfile.NonHeroPlunderMinGold;
	private const int NonHeroPlunderMaxGold = SiegeLootAccountingProfile.NonHeroPlunderMaxGold;
	private const int NonHeroMassacreGold = SiegeLootAccountingProfile.NonHeroMassacreGold;
	private const int HeroMassacreFallbackGold = SiegeLootAccountingProfile.HeroMassacreFallbackGold;
	private const int MaxConcurrentPlunderInteractions = SiegePlunderInteractionProfile.MaxConcurrentInteractions;
	private const float PlunderSoldierAssignmentRatio = SiegePlunderInteractionProfile.SoldierAssignmentRatio;
	private const float PlunderApproachDistance = SiegePlunderInteractionProfile.ApproachDistance;
	private const float PlunderTalkSeconds = SiegePlunderInteractionProfile.TalkSeconds;
	private const float MassacreCivilianHideDistance = SiegeMassacreInteractionProfile.CivilianHideDistance;
	private const float MassacreCivilianHideRefreshSeconds = SiegeMassacreInteractionProfile.CivilianHideRefreshSeconds;
	private const float MassacreSoldierFollowRefreshSeconds = SiegeMassacreInteractionProfile.SoldierFollowRefreshSeconds;
	private const float MassacreSoldierTargetRefreshSeconds = SiegeMassacreInteractionProfile.SoldierTargetRefreshSeconds;
	private const int MassacreMaxHuntersPerTarget = SiegeMassacreInteractionProfile.MaxHuntersPerTarget;
	private const float MassacreTargetApproachRadius = SiegeMassacreInteractionProfile.TargetApproachRadius;
	private const float MassacreSoldierStuckReassignSeconds = SiegeMassacreInteractionProfile.SoldierStuckReassignSeconds;
	private const float MassacreSoldierStuckMinMovedDistance = SiegeMassacreInteractionProfile.SoldierStuckMinMovedDistance;
	private const float MassacreSoldierStuckTargetMinDistance = SiegeMassacreInteractionProfile.SoldierStuckTargetMinDistance;
	private const float CivilianSpeechRallySettleTolerance = SiegeCivilianGatherInteractionProfile.SpeechRallySettleTolerance;
	private const float CivilianGatherTalkMinSeconds = SiegeCivilianGatherInteractionProfile.TalkMinSeconds;
	private const float CivilianGatherTalkMaxSeconds = SiegeCivilianGatherInteractionProfile.TalkMaxSeconds;
	private const float CivilianGatherFallbackSeconds = SiegeCivilianGatherInteractionProfile.FallbackSeconds;
	private const float CivilianGatherApproachDistance = SiegeCivilianGatherInteractionProfile.ApproachDistance;
	private const float CivilianGatherFollowRefreshSeconds = SiegeCivilianGatherInteractionProfile.FollowRefreshSeconds;
	private const float CivilianGatherFormationSettleDistance = SiegeCivilianGatherInteractionProfile.FormationSettleDistance;
	private const float CivilianGatherSoldierMessengerRatio = SiegeCivilianGatherInteractionProfile.SoldierMessengerRatio;
	private const float CivilianGatherMessengerMoveSpeedLimit = SiegeCivilianGatherInteractionProfile.MessengerMoveSpeedLimit;
	private const float CivilianFormationControlInitialDelaySeconds = SiegeCivilianGatherInteractionProfile.FormationControlInitialDelaySeconds;
	private const float CivilianFormationControlBatchIntervalSeconds = SiegeCivilianGatherInteractionProfile.FormationControlBatchIntervalSeconds;
	private const int CivilianFormationControlBatchSize = SiegeCivilianGatherInteractionProfile.FormationControlBatchSize;
	private const int CivilianGatherMessengerSpeechMinCount = SiegeCivilianGatherInteractionProfile.MessengerSpeechMinCount;
	private const int CivilianGatherMessengerSpeechMaxCount = SiegeCivilianGatherInteractionProfile.MessengerSpeechMaxCount;
	private const int TownCivilianAssemblySceneCap = SiegeCivilianAssemblyProfile.TownSceneCap;
	private const int SceneTotalAgentSoftCap = SiegeCivilianAssemblyProfile.SceneTotalAgentSoftCap;
	private const int MinimumCivilianAssemblySceneCap = SiegeCivilianAssemblyProfile.MinimumSceneCap;
	private const float CivilianAssemblyForwardDistance = SiegeCivilianAssemblyProfile.ForwardDistance;
	private const float CivilianAssemblyColumnSpacing = SiegeCivilianAssemblyProfile.ColumnSpacing;
	private const float CivilianAssemblyRowSpacing = SiegeCivilianAssemblyProfile.RowSpacing;
	private const int CivilianAssemblyColumns = SiegeCivilianAssemblyProfile.Columns;
	private const float SoldierCordonMinRadius = SiegeSoldierCordonProfile.MinRadius;
	private const float SoldierCordonPadding = SiegeSoldierCordonProfile.Padding;
	private const float SoldierCordonTeleportDistance = SiegeSoldierCordonProfile.TeleportDistance;
	private const float SoldierCordonMoveTolerance = SiegeSoldierCordonProfile.MoveTolerance;
	private const float SoldierCordonSettleTolerance = SiegeSoldierCordonProfile.SettleTolerance;
	private const float SoldierCordonOrderRefreshSeconds = SiegeSoldierCordonProfile.OrderRefreshSeconds;
	private const float SoldierCordonLookRefreshSeconds = SiegeSoldierCordonProfile.LookRefreshSeconds;
	private const int MaxInterventionMemoryEvents = SiegeInterventionMemoryContextBuilder.MaxMemoryEvents;
	private const float AmbientReactionWindowSeconds = SiegeAmbientReactionProfile.WindowSeconds;
	private const int MaxAmbientReactionSpeakersPerAudience = SiegeAmbientReactionProfile.MaxSpeakersPerAudience;
	private const float AmbientReactionRequestSpacingSeconds = SiegeAmbientReactionProfile.RequestSpacingSeconds;

	private static readonly Regex MercyTagRegex = new Regex(SiegeActionTagCatalog.MercyTagPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private static readonly Regex ReliefTagRegex = new Regex(SiegeActionTagCatalog.ReliefTagPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private static readonly Regex InspireTagRegex = new Regex(SiegeActionTagCatalog.InspireTagPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private static readonly Regex RallyOathTagRegex = new Regex(SiegeActionTagCatalog.RallyOathTagPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private static readonly Regex SoldierAppeasementTagRegex = new Regex(SiegeActionTagCatalog.SoldierAppeasementTagPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private static readonly Regex RepopulationTagRegex = new Regex(SiegeActionTagCatalog.CulturalRepopulationTagPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private static readonly Regex GatherCiviliansTagRegex = new Regex(SiegeActionTagCatalog.GatherCiviliansTagPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private static readonly Regex CivilianRobberyTagRegex = new Regex(SiegeActionTagCatalog.CivilianRobberyTagPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private static readonly Regex PlunderTagRegex = new Regex(SiegeActionTagCatalog.PlunderTagPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private static readonly Regex MassacreTagRegex = new Regex(SiegeActionTagCatalog.MassacreTagPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private static readonly Regex AnySiegeTagRegex = new Regex(SiegeActionTagCatalog.AnyActionTagPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static InterventionMode _activeMode = InterventionMode.None;
	private static InterventionMode _pendingMode = InterventionMode.None;
	private static string _activeSettlementId = "";
	private static string _activeSettlementName = "";
	private static bool _alliedTroopsAutoSummoned;
	private static float _nextControlTickTime;
	private static float _nextPlunderTickTime;
	private static bool _playerBattleEquipmentApplied;
	private static bool _plunderStarted;
	private static bool _massacreStarted;
	private static bool _massacreVictoryReached;
	private static bool _civilianSpeechRallyActive;
	private static bool _civilianGatherPropagationActive;
	private static bool _civilianFormationControlPending;
	private static bool _civilianFormationControlComplete;
	private static bool _civilianFormationControlMessageShown;
	private static bool _soldierDefaultFollowOrderIssued;
	private static bool _playerOrderControllerPrimed;
	private static bool _civilianOrderControllerPrimed;
	private static float _civilianGatherStartedAt = -1f;
	private static float _nextCivilianGatherTickTime;
	private static int _civilianGatherMessengerSpeechBudget;
	private static int _civilianGatherMessengerSpeechCount;
	private static float _civilianFormationControlNotBeforeTime = -1f;
	private static float _nextCivilianFormationControlBatchTime;
	private static float _nextPlayerOrderControllerPrimeTime;
	private static bool _culturalRepopulationRequested;
	private static bool _culturalRepopulationApplied;
	private static bool _reliefChoiceApplied;
	private static int _inspirationLevelApplied;
	private static bool _soldierAppeasementCheckDone;
	private static bool _soldierAppeasementRequired;
	private static bool _soldierAppeasementApplied;
	private static bool _soldierAppeasementMoralePenaltyApplied;
	private static float _lastMassacreRealKillMissionTime = -100f;
	private static float _lastDestructiveInquiryMissionTime = -100f;
	private static int _lastDestructiveInquirySourceAgentIndex = -1;
	private static float _lastAmbientCivilianReactionMissionTime = -100f;
	private static float _lastAmbientSoldierReactionMissionTime = -100f;
	private static float _nextAmbientReactionRequestMissionTime = -100f;
	private static readonly List<AmbientReactionRequest> PendingAmbientReactionRequests = new List<AmbientReactionRequest>();
	private static bool _hasPendingAftermath;
	private static SiegeAftermathAction.SiegeAftermath _pendingAftermath;
	private static string _pendingAftermathTrigger = "";
	private static string _pendingAftermathDetail = "";
	private static bool _marketGoodsLootAppliedForPlunder;
	private static bool _marketGoodsLootAppliedForMassacre;
	private static bool _marketGoldLootApplied;
	private static int _lastLootItemTotal;
	private static int _lastLootStackKinds;
	private static int _lastLootValue;
	private static int _lastMarketGoldLoot;
	private static int _lastCivilianGoldLoot;
	private static int _lastCivilianTargetsLooted;
	private static int _civilianRobberyTargetsLooted;
	private static int _civilianRobberyPenaltyLevelApplied;
	private static int _lastSceneCivilianSpawnedCount;
	private static int _lastKilledCivilianUnits;
	private static int _lastKilledNotables;
	private static int _sharedCivilianReliefGold;
	private static int _sharedCivilianReliefFoodUnits;
	private static int _sharedCivilianReliefItemTotal;
	private static long _sharedCivilianReliefItemValue;
	private static int _appliedSharedCivilianReliefGold;
	private static int _appliedSharedCivilianReliefFoodUnits;
	private static long _appliedSharedCivilianReliefItemValue;
	private static bool _sharedCivilianReliefReturned;
	private static readonly Dictionary<string, int> SharedCivilianReliefItems = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, ItemObject> SharedCivilianReliefItemObjects = new Dictionary<string, ItemObject>(StringComparer.OrdinalIgnoreCase);
	private static readonly List<string> InterventionMemoryEvents = new List<string>();
	private static bool _pendingSummarySwitch;
	private static SiegeAftermathAction.SiegeAftermath _pendingSummaryAftermath;
	private static ItemRoster _pendingLootRoster = new ItemRoster();
	private static bool _pendingLootScreen;
	private static bool _pendingLootScreenShown;
	private static bool _afAftermathResolved;
	private static int _interventionMemorySequence;
	private static string _completedSettlementId = "";
	private static string _completedSettlementName = "";
	private static SiegeAftermathAction.SiegeAftermath _completedAftermath;
	private static string _completedSummaryText = "";
	private static bool _pendingEncounterFinish;
	private static SiegeAftermathAction.SiegeAftermath _pendingEncounterFinishAftermath;
	private static int _pendingEncounterFinishDelayTicks;
	private static int _pendingEncounterFinishAttempts;
	private static bool _pendingEncounterFinishMessageShown;
	private static bool _nativeDevastateAftermathFlowActive;
	private static bool _nativeDevastateSummaryContinueHandled;
	private static bool _directMassacreAftermathScriptPending;
	private static bool _directMassacreLootScreenOpened;
	private static bool _directMassacreWaitingForLootClose;
	private static bool _directMassacreScriptMessageShown;
	private static int _directMassacreScriptTicks;
	private static string _directMassacreLastDeferKey = "";
	private static bool _directPlunderAftermathScriptPending;
	private static bool _directPlunderLootScreenOpened;
	private static bool _directPlunderWaitingForLootClose;
	private static bool _directPlunderScriptMessageShown;
	private static int _directPlunderScriptTicks;
	private static string _directPlunderLastDeferKey = "";
	private static readonly SiegeOutcomeMessageDeduplicator OutcomeMessageDeduplicator = new SiegeOutcomeMessageDeduplicator();
	private static bool _civilianAssemblyPointReady;
	private static bool _civilianAssemblyMessageShown;
	private static Vec3 _civilianAssemblyAnchor;
	private static Vec3 _civilianAssemblyForward;
	private static Clan _previousSettlementOwnerClan;
	private static MobileParty _besiegerParty;
	private static Settlement _activeSettlement;
	private static Team _interventionPlayerCommandTeam;
	private static Team _interventionCivilianEnemyTeam;
	private static TroopRoster _selectedInterventionRoster;
	private static Dictionary<MobileParty, float> _partyContributions = new Dictionary<MobileParty, float>();
	private static readonly Dictionary<int, PlunderInteraction> ActivePlunderInteractions = new Dictionary<int, PlunderInteraction>();
	private static readonly Dictionary<int, CivilianGatherInteraction> ActiveCivilianGatherInteractions = new Dictionary<int, CivilianGatherInteraction>();
	private static readonly HashSet<string> LootedTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private static readonly HashSet<string> CivilianRobberyTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private static readonly HashSet<int> AlliedAgentIndexes = new HashSet<int>();
	private static readonly HashSet<int> BannerBearerAgentIndexes = new HashSet<int>();
	private static readonly HashSet<int> CountedMassacreVictims = new HashSet<int>();
	private static readonly HashSet<Hero> PendingInterventionNotableDeaths = new HashSet<Hero>();
	private static readonly HashSet<int> SceneCivilianAgentIndexes = new HashSet<int>();
	private static readonly HashSet<int> VictoryCheerAgentIndexes = new HashSet<int>();
	private static readonly HashSet<int> CordonReadyAgentIndexes = new HashSet<int>();
	private static readonly HashSet<int> CivilianCalmedAgentIndexes = new HashSet<int>();
	private static readonly HashSet<int> CivilianFrightenedActionAgentIndexes = new HashSet<int>();
	private static readonly HashSet<int> CivilianPreMassacrePreparedAgentIndexes = new HashSet<int>();
	private static readonly HashSet<int> LocalPlayerAttackVictimAgentIndexes = new HashSet<int>();
	private static readonly HashSet<int> LocalPlayerAttackDownAgentIndexes = new HashSet<int>();
	private static readonly HashSet<int> LocalSoldierWitnessInquiryVictimAgentIndexes = new HashSet<int>();
	private static readonly HashSet<int> LocalHostileCivilianAgentIndexes = new HashSet<int>();
	private static readonly HashSet<int> LocalFleeingCivilianAgentIndexes = new HashSet<int>();
	private static readonly Dictionary<int, float> LastLocalCivilianWitnessReactionTimes = new Dictionary<int, float>();
	private static bool _localNativeFightStarted;
	private static int _regionalConflictIncidentCount;
	private static readonly List<Vec3> RegionalConflictDebtCenters = new List<Vec3>();
	private static readonly HashSet<int> CivilianGatherMovePreparedAgentIndexes = new HashSet<int>();
	private static readonly HashSet<int> CivilianGatherFollowerAgentIndexes = new HashSet<int>();
	private static readonly HashSet<int> CivilianGatherReadyFormationAgentIndexes = new HashSet<int>();
	private static readonly HashSet<int> CivilianGatherMessengerAgentIndexes = new HashSet<int>();
	private static readonly HashSet<int> CivilianGatherMessengerSpeechAgentIndexes = new HashSet<int>();
	private static readonly HashSet<int> CommandableOriginRuntimeIds = new HashSet<int>();
	private static readonly HashSet<int> MassacreReadySoldierAgentIndexes = new HashSet<int>();
	private static readonly HashSet<int> MassacreCombatPreparedAgentIndexes = new HashSet<int>();
	private static readonly Dictionary<int, int> CivilianSpeechRallySlots = new Dictionary<int, int>();
	private static readonly Dictionary<int, float> LastCordonMoveOrderTimesBySoldier = new Dictionary<int, float>();
	private static readonly Dictionary<int, float> LastCordonLookOrderTimesBySoldier = new Dictionary<int, float>();
	private static readonly Dictionary<int, Vec3> CivilianHideTargets = new Dictionary<int, Vec3>();
	private static readonly Dictionary<int, float> LastCivilianHideOrderTimes = new Dictionary<int, float>();
	private static readonly HashSet<int> CivilianHideSettledAgentIndexes = new HashSet<int>();
	private static readonly List<Vec3> CivilianInteriorHidePointPool = new List<Vec3>();
	private static readonly List<Vec3> CivilianEscapePointPool = new List<Vec3>();
	private static string _civilianRoutPointPoolSceneName = "";
	private static Dictionary<string, int> _repopulationProsperityDebuffUntilDayBySettlement = new Dictionary<string, int>();
	private static Dictionary<string, float> _repopulationProsperityLastObservedBySettlement = new Dictionary<string, float>();
	private static Dictionary<string, int> _civicProsperityBuffUntilDayBySettlement = new Dictionary<string, int>();
	private static Dictionary<string, float> _civicProsperityLastObservedBySettlement = new Dictionary<string, float>();
	private static Dictionary<string, float> _civicProsperityGrowthMultiplierBySettlement = new Dictionary<string, float>();
	private static Dictionary<string, int> _rallyOathLoyaltyLockUntilDayBySettlement = new Dictionary<string, int>();
	private static Dictionary<string, float> _rallyOathLoyaltyLockValueBySettlement = new Dictionary<string, float>();
	private static Dictionary<string, int> _rallyOathRecruitmentBuffUntilDayBySettlement = new Dictionary<string, int>();
	private static Dictionary<string, int> _recruitmentSuppressionUntilDayBySettlement = new Dictionary<string, int>();
	private static int _pendingPositiveNotableRelationDelta;
	private static bool _pendingPositiveNotableRelationIncludesBoundVillages;
	private static string _pendingPositiveNotableRelationReason = "";
	private static int _pendingPositiveNotableTrustDelta;
	private static bool _pendingPositiveNotableTrustIncludesBoundVillages;
	private static string _pendingPositiveNotableTrustReason = "";
	private static readonly Dictionary<int, float> LastMassacreSoldierFollowOrderTimes = new Dictionary<int, float>();
	private static readonly Dictionary<int, float> LastMassacreSoldierTargetOrderTimes = new Dictionary<int, float>();
	private static readonly Dictionary<int, int> MassacreSoldierTargetAgentIndexes = new Dictionary<int, int>();
	private static readonly Dictionary<int, int> MassacreSoldierTargetSlots = new Dictionary<int, int>();
	private static readonly Dictionary<int, Vec3> LastMassacreSoldierProbePositions = new Dictionary<int, Vec3>();
	private static readonly Dictionary<int, float> LastMassacreSoldierProbeTimes = new Dictionary<int, float>();
	private static readonly Dictionary<int, float> LastCivilianGatherFollowOrderTimes = new Dictionary<int, float>();
	private static readonly Dictionary<int, Vec3> LastCivilianGatherFollowTargets = new Dictionary<int, Vec3>();
	private static readonly Dictionary<int, Vec3> LastAgentWallRescueProbePositions = new Dictionary<int, Vec3>();
	private static readonly Dictionary<int, float> LastAgentWallRescueProbeTimes = new Dictionary<int, float>();
	private static readonly Dictionary<int, float> AgentWallRescueUntilTimes = new Dictionary<int, float>();
	private static readonly Dictionary<int, float> LastAgentWallRescueLogTimes = new Dictionary<int, float>();
	private static readonly FieldInfo OrderTroopPlacerOrderControllerField = AccessTools.Field(typeof(OrderTroopPlacer), "_orderController");
	private static readonly FieldInfo SingleVisualOrderOrderTypeField = AccessTools.Field(typeof(SingleVisualOrder), "_orderType");
	private static readonly FieldInfo FollowAgentBehaviorIdleDistanceField = typeof(FollowAgentBehavior).GetField("_idleDistance", BindingFlags.Instance | BindingFlags.NonPublic);

	public static SiegeAiInterventionBehavior Instance { get; private set; }

	public SiegeAiInterventionBehavior()
	{
		Instance = this;
	}

	public override void RegisterEvents()
	{
		SiegeInterventionSceneTauntSuppressionPatch.EnsurePatched();
		SiegeInterventionCommandOriginPatch.EnsurePatched();
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
		CampaignEvents.MissionTickEvent.AddNonSerializedListener(this, OnMissionTick);
		CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
		CampaignEvents.DailyTickTownEvent.AddNonSerializedListener(this, OnDailyTickTown);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_gcczRepopulationProsperityDebuffUntilDayBySettlement_v1", ref _repopulationProsperityDebuffUntilDayBySettlement);
		dataStore.SyncData("_gcczRepopulationProsperityLastObservedBySettlement_v1", ref _repopulationProsperityLastObservedBySettlement);
		dataStore.SyncData("_gcczCivicProsperityBuffUntilDayBySettlement_v1", ref _civicProsperityBuffUntilDayBySettlement);
		dataStore.SyncData("_gcczCivicProsperityLastObservedBySettlement_v1", ref _civicProsperityLastObservedBySettlement);
		dataStore.SyncData("_gcczCivicProsperityGrowthMultiplierBySettlement_v1", ref _civicProsperityGrowthMultiplierBySettlement);
		dataStore.SyncData("_gcczRallyOathLoyaltyLockUntilDayBySettlement_v1", ref _rallyOathLoyaltyLockUntilDayBySettlement);
		dataStore.SyncData("_gcczRallyOathLoyaltyLockValueBySettlement_v1", ref _rallyOathLoyaltyLockValueBySettlement);
		dataStore.SyncData("_gcczRallyOathRecruitmentBuffUntilDayBySettlement_v1", ref _rallyOathRecruitmentBuffUntilDayBySettlement);
		dataStore.SyncData("_gcczRecruitmentSuppressionUntilDayBySettlement_v1", ref _recruitmentSuppressionUntilDayBySettlement);
		_repopulationProsperityDebuffUntilDayBySettlement ??= new Dictionary<string, int>();
		_repopulationProsperityLastObservedBySettlement ??= new Dictionary<string, float>();
		_civicProsperityBuffUntilDayBySettlement ??= new Dictionary<string, int>();
		_civicProsperityLastObservedBySettlement ??= new Dictionary<string, float>();
		_civicProsperityGrowthMultiplierBySettlement ??= new Dictionary<string, float>();
		_rallyOathLoyaltyLockUntilDayBySettlement ??= new Dictionary<string, int>();
		_rallyOathLoyaltyLockValueBySettlement ??= new Dictionary<string, float>();
		_rallyOathRecruitmentBuffUntilDayBySettlement ??= new Dictionary<string, int>();
		_recruitmentSuppressionUntilDayBySettlement ??= new Dictionary<string, int>();
	}

	private void OnDailyTickTown(Town town)
	{
		ApplyRepopulationProsperityGrowthDebuff(town);
		ApplyRecruitmentSuppressionDebuff(town);
		ApplyCivicProsperityGrowthBuff(town);
		ApplyRallyOathLoyaltyLock(town);
		ApplyRallyOathRecruitmentBuff(town);
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		AddGameMenus(starter);
	}

	private void OnNewGameCreated(CampaignGameStarter starter)
	{
		ClearRepopulationProsperityDebuffs();
		ClearRecruitmentSuppressionDebuffs();
		ClearCivicPositiveBuffs();
		ResetAftermathRuntimeGuards(SiegeAftermathTransitionSourceProfile.ResetNewGameCreatedSource);
	}

	private void OnGameLoaded(CampaignGameStarter starter)
	{
		ResetAftermathRuntimeGuards(SiegeAftermathTransitionSourceProfile.ResetGameLoadedSource);
	}

	private void OnGameLoadFinished()
	{
		ResetAftermathRuntimeGuards(SiegeAftermathTransitionSourceProfile.ResetGameLoadFinishedSource);
	}

	private void AddGameMenus(CampaignGameStarter starter)
	{
		if (starter == null)
		{
			return;
		}
		try
		{
			starter.AddGameMenu("AnimusForge_siege_intervention_done", "{=!}{AF_SIEGE_DONE_TEXT}", AfSiegeInterventionDoneOnInit, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			starter.AddGameMenuOption("AnimusForge_siege_intervention_done", "AnimusForge_siege_intervention_done_continue", SiegeInterventionCompletionUiProfile.DoneContinueMenuOptionText, AfSiegeInterventionDoneContinueCondition, AfSiegeInterventionDoneContinueConsequence, isLeave: false, -1);
			foreach (string menuId in SiegeAftermathMenuProfile.EntryMenuIds)
			{
				string optionId = SiegeAftermathMenuProfile.BuildEntryMenuOptionId(menuId);
				starter.AddGameMenuOption(menuId, optionId, SiegeInterventionEntryProfile.EntryMenuOptionText, SiegeInterventionEntryCondition, SiegeInterventionEntryConsequence, isLeave: false, SiegeAftermathMenuProfile.EntryMenuInsertionIndex);
				Logger.Log("SiegeAiIntervention", "Registered entry option. Menu=" + menuId + ", Option=" + optionId + ", Index=" + SiegeAftermathMenuProfile.EntryMenuInsertionIndex);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "AddGameMenus failed: " + ex.Message);
		}
	}

	private static void AfSiegeInterventionDoneOnInit(MenuCallbackArgs args)
	{
		try
		{
			string text = string.IsNullOrWhiteSpace(_completedSummaryText) ? SiegeInterventionCompletionUiProfile.DoneMenuFallbackText : _completedSummaryText;
			MBTextManager.SetTextVariable("AF_SIEGE_DONE_TEXT", text, false);
			args?.MenuContext?.SetBackgroundMeshName("encounter_win");
		}
		catch
		{
		}
	}

	private static bool AfSiegeInterventionDoneContinueCondition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Continue;
		return true;
	}

	private static void AfSiegeInterventionDoneContinueConsequence(MenuCallbackArgs args)
	{
		FinishPlayerEncounterAfterIntervention(_pendingSummaryAftermath);
	}

	private static bool SiegeInterventionEntryCondition(MenuCallbackArgs args)
	{
		try
		{
			Settlement settlement = ResolveCurrentSettlement();
			if (ShouldHideInterventionEntryAfterResolution(settlement))
			{
				args.IsEnabled = false;
				args.optionLeaveType = GameMenuOption.LeaveType.Continue;
				return false;
			}
			bool baseEnabled = settlement != null && settlement.IsTown && PlayerEncounter.LocationEncounter != null && ResolveInterventionLocation(settlement) != null;
			args.IsEnabled = baseEnabled;
			args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
			args.Tooltip = new TextObject(baseEnabled
				? SiegeInterventionEntryProfile.EnabledTooltip
				: SiegeInterventionEntryProfile.MissingSceneTooltip);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool ShouldHideInterventionEntryAfterResolution(Settlement settlement)
	{
		try
		{
			if (settlement == null)
			{
				return false;
			}
			if ((_afAftermathResolved || _pendingEncounterFinish || _pendingSummarySwitch) && DoesCompletedAftermathMatchCurrentSettlement())
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static void SiegeInterventionEntryConsequence(MenuCallbackArgs args)
	{
		EnterIntervention(args);
	}

	private static void EnterIntervention(MenuCallbackArgs args)
	{
		try
		{
			Settlement settlement = ResolveCurrentSettlement();
			Location location = ResolveInterventionLocation(settlement);
			if (settlement == null || PlayerEncounter.LocationEncounter == null || location == null)
			{
				InformationManager.DisplayMessage(new InformationMessage(SiegeInterventionEntryProfile.MissingSceneMessage, Color.FromUint(SiegeInterventionEntryProfile.MissingSceneMessageColor)));
				return;
			}
			_activeMode = InterventionMode.WaitingDecision;
			_pendingMode = InterventionMode.WaitingDecision;
			_activeSettlementId = settlement.StringId ?? "";
			_activeSettlementName = settlement.Name?.ToString() ?? "";
			_activeSettlement = settlement;
			_afAftermathResolved = false;
			_completedSettlementId = "";
			_completedSettlementName = "";
			_completedAftermath = SiegeAftermathAction.SiegeAftermath.ShowMercy;
			_completedSummaryText = "";
			_nativeDevastateAftermathFlowActive = false;
			_nativeDevastateSummaryContinueHandled = false;
			_directMassacreAftermathScriptPending = false;
			_directMassacreLootScreenOpened = false;
			_directMassacreWaitingForLootClose = false;
			_directMassacreScriptMessageShown = false;
			_directMassacreScriptTicks = 0;
			_directMassacreLastDeferKey = "";
			_directPlunderAftermathScriptPending = false;
			_directPlunderLootScreenOpened = false;
			_directPlunderWaitingForLootClose = false;
			_directPlunderScriptMessageShown = false;
			_directPlunderScriptTicks = 0;
			_directPlunderLastDeferKey = "";
			ResetOutcomeMessageDedup();
			_civilianAssemblyPointReady = false;
			_civilianAssemblyMessageShown = false;
			_civilianAssemblyAnchor = Vec3.Zero;
			_civilianAssemblyForward = Vec3.Forward;
			CaptureNativeSiegeContext(settlement);
			ResetSessionCounters();
			SceneTauntBehavior.ClearArmedCarryoverForExternal(SiegeInterventionEntryProfile.SceneEntryCleanupSource);
			SceneTauntBehavior.ClearPendingLocalDungeonCaptivityForExternal(SiegeInterventionEntryProfile.SceneEntryCleanupSource);
			SceneTauntBehavior.ClearPendingForcedPlayerExecutionForExternal(SiegeInterventionEntryProfile.SceneEntryCleanupSource);
			SceneTauntBehavior.ClearPendingMainHeroBattleDeathForExternal(SiegeInterventionEntryProfile.SceneEntryCleanupSource);
			InformationManager.DisplayMessage(new InformationMessage(SiegeInterventionEntryProfile.BuildTroopSelectionInstructionMessage(AutoSummonCount), Color.FromUint(SiegeInterventionEntryProfile.EntryInstructionMessageColor)));
			InformationManager.DisplayMessage(new InformationMessage(SiegeInterventionEntryProfile.DecisionPolicyMessage, Color.FromUint(SiegeInterventionEntryProfile.EntryInstructionMessageColor)));
			if (!TryOpenInterventionTroopSelection(args, location))
			{
				OpenInterventionMissionNow(location, SiegeInterventionEntryProfile.SelectionUnavailableMissionSource);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "EnterIntervention failed: " + ex);
			InformationManager.DisplayMessage(new InformationMessage(SiegeInterventionEntryProfile.EntryFailedMessage, Color.FromUint(SiegeInterventionEntryProfile.MissingSceneMessageColor)));
		}
	}

	private static bool TryOpenInterventionTroopSelection(MenuCallbackArgs args, Location location)
	{
		try
		{
			if (location == null || args?.MenuContext?.Handler == null || MobileParty.MainParty?.MemberRoster == null)
			{
				return false;
			}
			TroopRoster fullRoster = BuildInterventionTroopSelectionFullRoster();
			if (fullRoster == null || fullRoster.TotalManCount <= 0)
			{
				return false;
			}
			TroopRoster initialSelections = BuildDefaultInterventionTroopSelection(fullRoster, AutoSummonCount);
			_selectedInterventionRoster = null;
			Action<TroopRoster> onDone = delegate(TroopRoster selectedRoster)
			{
				StoreSelectedInterventionRoster(selectedRoster, AutoSummonCount);
				int selectedCount = _selectedInterventionRoster?.TotalManCount ?? 0;
				if (selectedCount > 0)
				{
					InformationManager.DisplayMessage(new InformationMessage(SiegeInterventionEntryProfile.BuildSelectionConfirmedMessage(selectedCount), Color.FromUint(SiegeInterventionEntryProfile.SelectionConfirmedMessageColor)));
				}
				else
				{
					InformationManager.DisplayMessage(new InformationMessage(SiegeInterventionEntryProfile.SelectionFallbackMessage, Color.FromUint(SiegeInterventionEntryProfile.SelectionFallbackMessageColor)));
				}
				OpenInterventionMissionNow(location, SiegeInterventionEntryProfile.TroopSelectionDoneMissionSource);
			};
			if (!TryOpenTroopSelectionRuntimeCompat(args.MenuContext, fullRoster, initialSelections, onDone))
			{
				return false;
			}
			Logger.Log("SiegeAiIntervention", "Opened GameMenu troop selection screen. FullRoster=" + fullRoster.TotalManCount + ", Initial=" + (initialSelections?.TotalManCount ?? 0));
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "Open intervention troop selection failed: " + ex.Message);
			return false;
		}
	}

	private static bool TryOpenTroopSelectionRuntimeCompat(MenuContext menuContext, TroopRoster fullRoster, TroopRoster initialSelections, Action<TroopRoster> onDone)
	{
		try
		{
			if (menuContext == null || fullRoster == null || initialSelections == null || onDone == null)
			{
				return false;
			}
			MethodInfo[] methods = menuContext.GetType()
				.GetMethods(BindingFlags.Instance | BindingFlags.Public)
				.Where(x => x.Name == "OpenTroopSelection")
				.OrderBy(x => x.GetParameters().Length == 6 ? 0 : 1)
				.ThenBy(x => x.GetParameters().Length)
				.ToArray();
			foreach (MethodInfo method in methods)
			{
				ParameterInfo[] parameters = method.GetParameters();
				object[] arguments = TryBuildOpenTroopSelectionRuntimeArguments(parameters, fullRoster, initialSelections, onDone);
				if (arguments == null)
				{
					continue;
				}
				try
				{
					method.Invoke(menuContext, arguments);
					Logger.Log("SiegeAiIntervention", "Opened troop selection through runtime-compatible MenuContext bridge. ParameterCount=" + parameters.Length);
					return true;
				}
				catch (TargetInvocationException ex)
				{
					Logger.Log("SiegeAiIntervention", "Runtime-compatible troop selection invocation failed: " + (ex.InnerException?.Message ?? ex.Message));
					return false;
				}
				catch (Exception ex)
				{
					Logger.Log("SiegeAiIntervention", "Skipped incompatible OpenTroopSelection candidate. ParameterCount=" + parameters.Length + ", Error=" + ex.Message);
				}
			}
			Logger.Log("SiegeAiIntervention", "No compatible MenuContext.OpenTroopSelection overload found at runtime.");
			return false;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryOpenTroopSelectionRuntimeCompat failed: " + ex.Message);
			return false;
		}
	}

	private static object[] TryBuildOpenTroopSelectionRuntimeArguments(ParameterInfo[] parameters, TroopRoster fullRoster, TroopRoster initialSelections, Action<TroopRoster> onDone)
	{
		if (parameters == null || parameters.Length < 6)
		{
			return null;
		}
		object[] arguments = new object[parameters.Length];
		int rosterIndex = 0;
		int intIndex = 0;
		bool hasCanChange = false;
		bool hasDone = false;
		for (int i = 0; i < parameters.Length; i++)
		{
			Type parameterType = parameters[i].ParameterType;
			if (typeof(TroopRoster).IsAssignableFrom(parameterType))
			{
				arguments[i] = rosterIndex == 0 ? fullRoster : initialSelections;
				rosterIndex++;
			}
			else if (parameterType == typeof(Func<CharacterObject, bool>))
			{
				arguments[i] = new Func<CharacterObject, bool>(CanChangeInterventionTroopSelectionStatus);
				hasCanChange = true;
			}
			else if (parameterType == typeof(Action<TroopRoster>))
			{
				arguments[i] = onDone;
				hasDone = true;
			}
			else if (parameterType == typeof(int))
			{
				arguments[i] = intIndex == 0 ? AutoSummonCount : 0;
				intIndex++;
			}
			else if (parameterType == typeof(bool))
			{
				arguments[i] = false;
			}
			else if (!parameterType.IsValueType)
			{
				arguments[i] = null;
			}
			else
			{
				return null;
			}
		}
		return rosterIndex >= 2 && intIndex >= 2 && hasCanChange && hasDone ? arguments : null;
	}

	private static TroopRoster BuildInterventionTroopSelectionFullRoster()
	{
		TroopRoster fullRoster = TroopRoster.CreateDummyTroopRoster();
		TroopRoster sourceRoster = MobileParty.MainParty?.MemberRoster;
		if (sourceRoster == null)
		{
			return fullRoster;
		}
		foreach (TroopRosterElement element in sourceRoster.GetTroopRoster())
		{
			CharacterObject character = element.Character;
			if (!IsSelectableInterventionTroop(character) || element.Number <= 0)
			{
				continue;
			}
			int available = character.HeroObject != null ? element.Number : Math.Max(0, element.Number - element.WoundedNumber);
			if (available > 0)
			{
				fullRoster.AddToCounts(character, available, false, 0, 0, true, -1);
			}
		}
		return fullRoster;
	}

	private static TroopRoster BuildDefaultInterventionTroopSelection(TroopRoster fullRoster, int maxCount)
	{
		try
		{
			if (fullRoster == null || maxCount <= 0)
			{
				return TroopRoster.CreateDummyTroopRoster();
			}
			FlattenedTroopRoster flattenedRoster = fullRoster.ToFlattenedRoster();
			flattenedRoster.RemoveIf((FlattenedTroopRosterElement x) => x.IsWounded || x.Troop == CharacterObject.PlayerCharacter);
			return MobilePartyHelper.GetStrongestAndPriorTroops(flattenedRoster, maxCount, includePlayer: false);
		}
		catch
		{
			return TroopRoster.CreateDummyTroopRoster();
		}
	}

	private static bool CanChangeInterventionTroopSelectionStatus(CharacterObject character)
	{
		return IsSelectableInterventionTroop(character);
	}

	private static void StoreSelectedInterventionRoster(TroopRoster sourceRoster, int maxCount)
	{
		TroopRoster selected = TroopRoster.CreateDummyTroopRoster();
		if (sourceRoster != null && maxCount > 0)
		{
			int remaining = maxCount;
			foreach (TroopRosterElement element in sourceRoster.GetTroopRoster())
			{
				CharacterObject character = element.Character;
				if (!IsSelectableInterventionTroop(character) || element.Number <= 0 || remaining <= 0)
				{
					continue;
				}
				int number = Math.Min(remaining, Math.Max(0, element.Number));
				if (number <= 0)
				{
					continue;
				}
				selected.AddToCounts(character, number, false, 0, 0, true, -1);
				remaining -= number;
			}
		}
		_selectedInterventionRoster = selected.TotalManCount > 0 ? selected : null;
		Logger.Log("SiegeAiIntervention", "Stored intervention troop selection. Count=" + (_selectedInterventionRoster?.TotalManCount ?? 0));
	}

	private static void OpenInterventionMissionNow(Location location, string source)
	{
		PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(location, null, null, null);
		Logger.Log("SiegeAiIntervention", "Opened intervention mission through normal location controller. Source=" + (source ?? "N/A") + ", SelectedRoster=" + (_selectedInterventionRoster?.TotalManCount ?? 0));
	}

	private static Location ResolveInterventionLocation(Settlement settlement)
	{
		try
		{
			if (settlement == null || !settlement.IsTown)
			{
				return null;
			}
			LocationComplex complex = settlement.LocationComplex ?? LocationComplex.Current;
			if (complex == null)
			{
				return null;
			}
			return complex.GetLocationWithId("center") ?? complex.FindAll(x => x == "center").FirstOrDefault();
		}
		catch
		{
			return null;
		}
	}

	private static bool IsDestructiveInterventionAllowed()
	{
		return true;
	}

	private static CultureObject ResolveCulturalRepopulationTargetCulture(out string sourceLabel)
	{
		sourceLabel = SiegeCulturalRepopulationProfile.PlayerHeroCultureSourceLabel;
		try
		{
			Hero mainHero = Hero.MainHero;
			Kingdom playerKingdom = mainHero?.Clan?.Kingdom;
			if (playerKingdom?.Culture != null)
			{
				sourceLabel = SiegeCulturalRepopulationProfile.PlayerKingdomCultureSourceLabel;
				return playerKingdom.Culture;
			}
			IFaction mapFaction = mainHero?.MapFaction;
			if (mapFaction != null && !ReferenceEquals(mapFaction, mainHero?.Clan) && mapFaction.Culture != null)
			{
				sourceLabel = SiegeCulturalRepopulationProfile.PlayerKingdomCultureSourceLabel;
				return mapFaction.Culture;
			}
			if (mainHero?.Culture != null)
			{
				sourceLabel = SiegeCulturalRepopulationProfile.PlayerHeroCultureSourceLabel;
				return mainHero.Culture;
			}
			if (mainHero?.Clan?.Culture != null)
			{
				sourceLabel = SiegeCulturalRepopulationProfile.PlayerClanCultureSourceLabel;
				return mainHero.Clan.Culture;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ResolveCulturalRepopulationTargetCulture failed: " + ex.Message);
		}
		sourceLabel = SiegeCulturalRepopulationProfile.PlayerCultureFallbackLabel;
		return null;
	}

	private static string DescribeCultureForMessage(CultureObject culture, string sourceLabel)
	{
		try
		{
			string cultureName = culture?.Name?.ToString();
			if (string.IsNullOrWhiteSpace(cultureName))
			{
				cultureName = culture?.StringId;
			}
			return SiegeCulturalRepopulationProfile.BuildTargetCultureMessageText(cultureName, sourceLabel);
		}
		catch
		{
			return SiegeCulturalRepopulationProfile.NormalizeCultureSourceLabel(sourceLabel);
		}
	}

	private void OnMissionStarted(IMission mission)
	{
		if (_pendingMode == InterventionMode.None)
		{
			return;
		}
		if (!DoesLiveCurrentSettlementMatchActiveIntervention())
		{
			Logger.Log("SiegeAiIntervention", "Ignored pending GCCZ mission start because live settlement did not match. PendingSettlement=" + (_activeSettlementId ?? "N/A"));
			ResetAftermathRuntimeGuards("pending_mission_settlement_mismatch");
			return;
		}
		_activeMode = _pendingMode;
		_pendingMode = InterventionMode.None;
		_nextControlTickTime = 0f;
		_nextPlunderTickTime = 0f;
		AfGcczShoutBridge.ResetPostprocessFrequencyForMissionBoundary(SiegePostprocessFrequencyProfile.MissionStartResetSource);
		try
		{
			if (mission is Mission missionPopulation && missionPopulation.GetMissionBehavior<InterventionNativeTownCivilianPopulationMissionBehavior>() == null)
			{
				missionPopulation.AddMissionBehavior(new InterventionNativeTownCivilianPopulationMissionBehavior(_activeSettlementId));
			}
			if (mission is Mission mission2 && mission2.GetMissionBehavior<InterventionMissionBehavior>() == null)
			{
				mission2.AddMissionBehavior(new InterventionMissionBehavior());
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "Add intervention mission behaviors failed: " + ex.Message);
		}
	}

	private static void OnInterventionMissionAfterStart(Mission mission)
	{
		if (!IsActiveInCurrentMission())
		{
			return;
		}
		EnsureInterventionMissionCombatModeForPlayerDamage(mission);
		EnsureInterventionPlayerCommandTeam(mission);
		ApplyPlayerBattleEquipment();
		RemoveProtectedSceneAgents(mission);
		RemovePlayerCompanionSceneAgents(mission);
		RemoveBackstreetCrimeAgents(mission);
		RemoveUnsafeAssemblyCivilianAgents(mission);
		TrackSceneCivilianAgents(mission);
		MaintainCivilianAssembly(mission, SiegeCivilianAssemblyProfile.MissionAfterStartSource, force: true);
	}

	private void OnMissionTick(float dt)
	{
		if (!IsActiveInCurrentMission())
		{
			return;
		}
		Mission mission = Mission.Current;
		if (mission == null)
		{
			return;
		}
		TryKeepMissionExitImmediatelyAvailable(mission);
		EnsureInterventionMissionCombatModeForPlayerDamage(mission);
		EnsureInterventionPlayerCommandTeam(mission);
		float currentTime = mission.CurrentTime;
		PumpPendingAmbientReactions(mission);
		if (currentTime >= _nextControlTickTime)
		{
			_nextControlTickTime = currentTime + 0.35f;
			ApplyPlayerBattleEquipment();
			RemoveProtectedSceneAgents(mission);
			RemovePlayerCompanionSceneAgents(mission);
			RemoveDefeatedGuardAgents(mission);
			RemoveBackstreetCrimeAgents(mission);
			RemoveUnsafeAssemblyCivilianAgents(mission);
			TrackSceneCivilianAgents(mission);
			MaintainCivilianAssembly(mission, SiegeCivilianAssemblyProfile.ControlTickSource, force: false);
			MaintainCivilianSpeechRally(mission, force: false);
			ApplyFrightenedCivilianIdle(mission);
			MaintainLocalPlayerAttackReactions(mission);
			if (!_alliedTroopsAutoSummoned)
			{
				_alliedTroopsAutoSummoned = true;
				SummonAlliedTroops(AutoSummonCount, SiegeInterventionEntryProfile.AutoEnterSummonSource);
			}
			if (!_massacreVictoryReached)
			{
				KeepAlliedTroopsUseful(mission);
				TryPrimePlayerOrderController(mission, SiegeNativeBridgeSourceProfile.ControlTickOrderControllerSource, force: false);
			}
			if (_massacreStarted)
			{
				if (_massacreVictoryReached)
				{
					KeepAlliedVictoryCheer(mission);
				}
				else
				{
					DriveMassacreCombatState(mission);
					TryCompleteMassacreIfAllTargetsDown(mission);
				}
			}
			TryTriggerOngoingAmbientReactions(mission);
		}
		if (_plunderStarted && !_massacreStarted && currentTime >= _nextPlunderTickTime)
		{
			_nextPlunderTickTime = currentTime + 1.0f;
			TryAutoPlunderOneNearbyCivilian(mission);
		}
		UpdatePendingCivilianFormationControl(mission);
	}

	private static void TryKeepMissionExitImmediatelyAvailable(Mission mission)
	{
		try
		{
			if (mission != null)
			{
				mission.NextCheckTimeEndMission = 0f;
			}
		}
		catch
		{
		}
	}

	private void OnMissionEnded(IMission mission)
	{
		if (_activeMode == InterventionMode.None && _pendingMode == InterventionMode.None)
		{
			return;
		}
		AfGcczShoutBridge.ResetPostprocessFrequencyForMissionBoundary(SiegePostprocessFrequencyProfile.MissionEndResetSource);
		EnsureMissionExitOutcomeBeforeFinalizing();
		if (_plunderStarted && !_massacreStarted)
		{
			AutoLootRemainingVisibleCiviliansForPlunder();
		}
		bool finalized = FinalizePendingAftermath(SiegeAftermathTransitionSourceProfile.MissionEndFinalizeSource);
		if (finalized)
		{
			_pendingSummarySwitch = true;
			if (_massacreStarted && _pendingSummaryAftermath == SiegeAftermathAction.SiegeAftermath.Devastate)
			{
				QueueDirectMassacreAftermathScript(SiegeAftermathTransitionSourceProfile.MissionEndFinalizedSource);
			}
			else if (_plunderStarted && _pendingSummaryAftermath == SiegeAftermathAction.SiegeAftermath.Pillage)
			{
				QueueDirectPlunderAftermathScript(SiegeAftermathTransitionSourceProfile.MissionEndFinalizedSource);
			}
			else
			{
				QueueEncounterFinishAfterIntervention(_pendingSummaryAftermath, SiegeAftermathTransitionSourceProfile.MissionEndFinalizedSource, 2, forceDelay: true);
			}
		}
		else
		{
			_pendingSummarySwitch = true;
			_pendingSummaryAftermath = SiegeAftermathAction.SiegeAftermath.ShowMercy;
			QueueEncounterFinishAfterIntervention(_pendingSummaryAftermath, SiegeAftermathTransitionSourceProfile.MissionEndNoPendingAftermathSource, 2, forceDelay: true);
			ClearActiveState(preserveSummarySwitch: true);
		}
	}

	private static void EnsureMissionExitOutcomeBeforeFinalizing()
	{
		try
		{
			bool needsFallbackPolicy = !_culturalRepopulationRequested && !_massacreStarted && !_plunderStarted && !_hasPendingAftermath;
			SiegeMissionExitOutcomeDecision decision = SiegeMissionExitOutcomeProfile.Resolve(
				_culturalRepopulationRequested,
				_massacreStarted,
				_plunderStarted,
				_hasPendingAftermath,
				needsFallbackPolicy);
			if (!decision.HasDecision)
			{
				return;
			}
			if (decision.ShouldStartPlunder)
			{
				StartPlunder(decision.TriggerSource, decision.TriggerDetail);
				return;
			}
			MarkPendingAftermath(ToNativeAftermathKind(decision.AftermathKind), decision.TriggerSource, decision.TriggerDetail);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "EnsureMissionExitOutcomeBeforeFinalizing failed: " + ex.Message);
		}
	}

	private void OnCampaignTick(float dt)
	{
		if (TryRunDirectMassacreAftermathScript())
		{
			return;
		}
		if (TryRunDirectPlunderAftermathScript())
		{
			return;
		}
		if (TryRouteNativeDevastateAftermathMenu())
		{
			return;
		}
		if (TryFinishResolvedSiegeAftermathMenu())
		{
			return;
		}
		if ((!_pendingSummarySwitch && !_pendingEncounterFinish) || Mission.Current != null)
		{
			return;
		}
		bool keepPending = false;
		try
		{
			if (_nativeDevastateAftermathFlowActive && !_pendingLootScreenShown)
			{
				keepPending = true;
				return;
			}
			if (_hasPendingAftermath)
			{
				keepPending = true;
				return;
			}
			if (_pendingLootScreen && !_pendingLootScreenShown && _pendingLootRoster != null && _pendingLootRoster.Count > 0)
			{
				_pendingLootScreenShown = true;
				keepPending = true;
				InventoryScreenHelper.OpenScreenAsLoot(new Dictionary<PartyBase, ItemRoster>
				{
					{
						PartyBase.MainParty,
						_pendingLootRoster
					}
				});
				return;
			}
			if (_pendingLootScreenShown && Game.Current?.GameStateManager?.ActiveState is InventoryState)
			{
				keepPending = true;
				return;
			}
			SiegeAftermathAction.SiegeAftermath aftermath = _pendingEncounterFinish ? _pendingEncounterFinishAftermath : _pendingSummaryAftermath;
			TrySetNativePlayerEncounterAftermathForSummary(aftermath);
			if (string.IsNullOrWhiteSpace(_completedSummaryText))
			{
				PrepareCompletedInterventionSummary(aftermath);
			}
			if (!_pendingEncounterFinish)
			{
				QueueEncounterFinishAfterIntervention(aftermath, SiegeAftermathTransitionSourceProfile.CampaignTickPostMissionFinishSource, 0, forceDelay: false);
			}
			if (!TryFinishPlayerEncounterAfterInterventionNow(aftermath, SiegeAftermathTransitionSourceProfile.CampaignTickPostMissionFinishSource))
			{
				keepPending = true;
				return;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "Finishing after intervention failed: " + ex.Message);
			keepPending = true;
		}
		finally
		{
			if (!keepPending)
			{
				_pendingSummarySwitch = false;
				_pendingEncounterFinish = false;
				ClearActiveState(preserveSummarySwitch: false);
			}
		}
	}

	private static bool TryFinishResolvedSiegeAftermathMenu()
	{
		try
		{
			if (_nativeDevastateAftermathFlowActive)
			{
				return false;
			}
			if (!_afAftermathResolved || Mission.Current != null)
			{
				return false;
			}
			string menuId = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
			if (!IsNativeSiegeAftermathMenuId(menuId) || !DoesCompletedAftermathMatchCurrentSettlement())
			{
				return false;
			}
			if (string.IsNullOrWhiteSpace(_completedSummaryText))
			{
				PrepareCompletedInterventionSummary(_completedAftermath);
			}
			QueueEncounterFinishAfterIntervention(_completedAftermath, SiegeAftermathTransitionSourceProfile.BuildCampaignTickNativeMenuDetectedSource(menuId), 0, forceDelay: true);
			TryFinishPlayerEncounterAfterInterventionNow(_completedAftermath, SiegeAftermathTransitionSourceProfile.BuildCampaignTickNativeMenuDetectedSource(menuId));
			Logger.Log("SiegeAiIntervention", "Finished resolved AF siege aftermath instead of returning to native three-option menu.");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryFinishResolvedSiegeAftermathMenu failed: " + ex.Message);
			return false;
		}
	}

	private static bool TryRouteNativeDevastateAftermathMenu()
	{
		try
		{
			if (_directMassacreAftermathScriptPending || !_nativeDevastateAftermathFlowActive || Mission.Current != null || !_afAftermathResolved || !DoesCompletedAftermathMatchCurrentSettlement())
			{
				return false;
			}
			string menuId = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
			if (SiegeAftermathMenuProfile.IsNativeSettlementTakenMenuId(menuId))
			{
				TrySetNativePlayerEncounterAftermathForSummary(SiegeAftermathAction.SiegeAftermath.Devastate);
				GameMenu.SwitchToMenu(SiegeAftermathMenuProfile.ContextualSummaryMenuId);
				Logger.Log("SiegeAiIntervention", "Routed AF massacre from native settlement-taken menu to native Devastate summary. CurrentMenu=" + (menuId ?? "N/A"));
				return true;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryRouteNativeDevastateAftermathMenu failed: " + ex.Message);
		}
		return false;
	}

	private static bool IsNativeSiegeAftermathMenuId(string menuId)
	{
		return SiegeAftermathMenuProfile.IsNativeOrContextualSummaryMenuId(menuId);
	}

	private static bool DoesCompletedAftermathMatchCurrentSettlement()
	{
		try
		{
			if (string.IsNullOrWhiteSpace(_completedSettlementId))
			{
				return true;
			}
			Settlement settlement = ResolveCurrentSettlement();
			if (settlement == null)
			{
				return true;
			}
			return string.Equals(settlement.StringId ?? "", _completedSettlementId, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return true;
		}
	}

	private static string BuildCivilianGatherRuntimeContext(Mission mission = null)
	{
		try
		{
			mission ??= Mission.Current;
			if (mission?.Agents == null)
			{
				return "";
			}
			int total = mission.Agents.Count(a => IsEligibleCivilianAgent(a, includeHeroes: true));
			int followers = CivilianGatherFollowerAgentIndexes.Count;
			int ready = CivilianGatherReadyFormationAgentIndexes.Count;
			int messengers = CivilianGatherMessengerAgentIndexes.Count;
			return SiegeCivilianGatherContextBuilder.Build(new SiegeCivilianGatherContextFacts(
				_civilianSpeechRallyActive,
				_civilianGatherPropagationActive,
				_civilianFormationControlPending,
				_civilianFormationControlComplete,
				followers,
				ready,
				messengers,
				total));
		}
		catch
		{
			return "";
		}
	}

	private static void MaybeTriggerSoldierAppeasementNeed(string outcomeName)
	{
		try
		{
			if (_soldierAppeasementCheckDone || _soldierAppeasementRequired || _massacreStarted || _culturalRepopulationRequested)
			{
				return;
			}
			_soldierAppeasementCheckDone = true;
			if (MBRandom.RandomFloat >= 0.5f)
			{
				Logger.Log("SiegeAiIntervention", "Soldier appeasement not required this run. Outcome=" + (outcomeName ?? "N/A"));
				return;
			}
			_soldierAppeasementRequired = true;
			_soldierAppeasementApplied = false;
			_soldierAppeasementMoralePenaltyApplied = false;
			SiegeSoldierAppeasementProfile soldierProfile = new SiegeSoldierAppeasementProfile();
			RecordInterventionMemory(soldierProfile.NeedMemoryTitle, soldierProfile.BuildNeedMemoryText(outcomeName));
			InformationManager.DisplayMessage(new InformationMessage(soldierProfile.NeedMessageText, Color.FromUint(soldierProfile.NeedMessageColor)));
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "MaybeTriggerSoldierAppeasementNeed failed: " + ex.Message);
		}
	}

	private static bool ApplySoldierAppeasementChoice(int targetAgentIndex)
	{
		try
		{
			if (targetAgentIndex < 0 || !AlliedAgentIndexes.Contains(targetAgentIndex))
			{
				InformationManager.DisplayMessage(new InformationMessage(SiegeSoldierAppeasementProfile.TargetValidationMessage, Color.FromUint(SiegeSoldierAppeasementProfile.ValidationMessageColor)));
				return false;
			}
			if (!_soldierAppeasementRequired)
			{
				return false;
			}
			if (_soldierAppeasementApplied)
			{
				return true;
			}
			SiegeSoldierAppeasementProfile soldierProfile = new SiegeSoldierAppeasementProfile();
			_soldierAppeasementApplied = true;
			RecordInterventionMemory(soldierProfile.AppeasementMemoryTitle, soldierProfile.AppeasementMemoryText);
			InformationManager.DisplayMessage(new InformationMessage(soldierProfile.AppeasementMessageText, Color.FromUint(soldierProfile.AppeasementMessageColor)));
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplySoldierAppeasementChoice failed: " + ex.Message);
			return false;
		}
	}

	private static void ApplySoldierAppeasementMoralePenaltyIfNeeded(SiegeAftermathAction.SiegeAftermath aftermath)
	{
		try
		{
			if (aftermath != SiegeAftermathAction.SiegeAftermath.ShowMercy || !_soldierAppeasementRequired || _soldierAppeasementApplied || _soldierAppeasementMoralePenaltyApplied)
			{
				return;
			}
			MobileParty party = MobileParty.MainParty;
			if (party == null)
			{
				return;
			}
			SiegeSoldierAppeasementProfile soldierProfile = new SiegeSoldierAppeasementProfile();
			_soldierAppeasementMoralePenaltyApplied = true;
			party.RecentEventsMorale -= soldierProfile.MoralePenalty;
			RecordInterventionMemory(soldierProfile.PenaltyMemoryTitle, soldierProfile.PenaltyMemoryText);
			InformationManager.DisplayMessage(new InformationMessage(soldierProfile.PenaltyMessageText, Color.FromUint(soldierProfile.PenaltyMessageColor)));
			Logger.Log("SiegeAiIntervention", "Applied soldier appeasement morale penalty -" + soldierProfile.MoralePenalty + ". MoraleNow=" + party.Morale);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplySoldierAppeasementMoralePenaltyIfNeeded failed: " + ex.Message);
		}
	}

	private static void RecordInterventionMemory(string kind, string detail)
	{
		try
		{
			string entry = SiegeInterventionMemoryEventFormatter.FormatEntry(kind, detail);
			if (InterventionMemoryEvents.Count > 0
				&& string.Equals(SiegeInterventionMemoryEventFormatter.StripSequencePrefix(InterventionMemoryEvents[InterventionMemoryEvents.Count - 1]), entry, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
			_interventionMemorySequence++;
			InterventionMemoryEvents.Add(_interventionMemorySequence + "." + entry);
			while (InterventionMemoryEvents.Count > MaxInterventionMemoryEvents)
			{
				InterventionMemoryEvents.RemoveAt(0);
			}
			Logger.Log("SiegeAiIntervention", "Memory: " + entry);
		}
		catch
		{
		}
	}

	private static string BuildInterventionMemoryContext(SiegeInterventionMemoryAudience audience = SiegeInterventionMemoryAudience.General)
	{
		try
		{
			if (InterventionMemoryEvents.Count == 0)
			{
				return "";
			}
			return SiegeInterventionMemoryContextBuilder.Build(InterventionMemoryEvents, audience);
		}
		catch
		{
			return "";
		}
	}

	private static SiegeInterventionMemoryAudience SelectInterventionMemoryAudience(bool alliedSoldier, bool civilian)
	{
		if (alliedSoldier)
		{
			return SiegeInterventionMemoryAudience.AlliedSoldier;
		}
		return civilian ? SiegeInterventionMemoryAudience.Civilian : SiegeInterventionMemoryAudience.General;
	}

	private static string AppendRuntimeContext(string existingContext, string extraContext)
	{
		if (string.IsNullOrWhiteSpace(existingContext))
		{
			return extraContext ?? "";
		}
		if (string.IsNullOrWhiteSpace(extraContext))
		{
			return existingContext ?? "";
		}
		return existingContext + extraContext;
	}

	private static bool IsNpcRuntimeAlliedSoldierFallback(NpcDataPacket npc, CharacterObject character)
	{
		try
		{
			if (npc == null)
			{
				return false;
			}
			if (npc.AgentIndex >= 0 && AlliedAgentIndexes.Contains(npc.AgentIndex))
			{
				return true;
			}
			if (character == null || character == CharacterObject.PlayerCharacter || IsProtectedChildCharacter(character) || IsCivilianForIntervention(character) || IsBackstreetOrCriminalCharacter(character))
			{
				return false;
			}
			string rank = (npc.UnnamedRank ?? "").Trim();
			string role = (npc.RoleDesc ?? "").Trim();
			bool soldierLikeNpc = character.IsSoldier
				|| IsGuardOrSoldier(character)
				|| string.Equals(rank, "soldier", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(role, "士兵", StringComparison.Ordinal)
				|| string.Equals(role, "守卫", StringComparison.Ordinal)
				|| string.Equals(role, "卫兵", StringComparison.Ordinal);
			return soldierLikeNpc && IsMainPartyOrSelectedInterventionTroop(character);
		}
		catch
		{
			return false;
		}
	}

	private static string BuildPlayerCommanderRuntimeContext(bool alliedSoldier, bool civilian)
	{
		try
		{
			return SiegeRuntimePromptProfile.BuildPlayerCommanderContext(ResolvePlayerCharacterNameForContext(), alliedSoldier, civilian);
		}
		catch
		{
			return "";
		}
	}

	private static string ResolvePlayerCharacterNameForContext()
	{
		try
		{
			CharacterObject playerCharacter = CharacterObject.PlayerCharacter ?? Hero.MainHero?.CharacterObject;
			return playerCharacter?.Name?.ToString() ?? Hero.MainHero?.Name?.ToString() ?? "玩家";
		}
		catch
		{
			return "玩家";
		}
	}

	internal static string BuildRuntimePromptForAgent(Hero hero, NpcDataPacket npc, int agentIndex)
	{
		if (!IsActiveInCurrentMission())
		{
			return "";
		}
		string settlementName = string.IsNullOrWhiteSpace(_activeSettlementName) ? "这座刚被攻下的定居点" : _activeSettlementName;
		Agent agent = TryGetAgent(agentIndex);
		CharacterObject character = (agent?.Character as CharacterObject) ?? hero?.CharacterObject;
		bool alliedSoldier = IsRuntimeAlliedSoldierAgent(agent, character, hero);
		if (!alliedSoldier && IsNpcRuntimeAlliedSoldierFallback(npc, character))
		{
			alliedSoldier = true;
		}
		bool guard = IsGuardOrSoldier(character);
		bool civilian = IsCivilianForIntervention(character);
		string gatherContext = BuildCivilianGatherRuntimeContext(Mission.Current);
		string memoryContext = AppendRuntimeContext(
			BuildInterventionMemoryContext(SelectInterventionMemoryAudience(alliedSoldier, civilian)),
			BuildPlayerCommanderRuntimeContext(alliedSoldier, civilian));
		return SiegeRuntimePromptProfile.Build(new SiegeRuntimePromptFacts(
			settlementName,
			alliedSoldier,
			guard,
			civilian,
			_soldierAppeasementRequired,
			_soldierAppeasementApplied,
			gatherContext,
			memoryContext,
			DescribeSharedCivilianReliefPoolForContext(),
			_plunderStarted,
			_massacreStarted));
	}

	internal static string BuildRuntimePromptForPromptContext(Hero hero, CharacterObject character, int agentIndex, string cultureIdOverride = null)
	{
		try
		{
			if (!IsActiveInCurrentMission())
			{
				return "";
			}
			Agent agent = TryGetAgent(agentIndex);
			CharacterObject resolved = character ?? (agent?.Character as CharacterObject) ?? hero?.CharacterObject;
			bool soldierLikeResolved = resolved != null && (resolved.IsSoldier || IsGuardOrSoldier(resolved) || IsMainPartyOrSelectedInterventionTroop(resolved));
			NpcDataPacket packet = new NpcDataPacket
			{
				AgentIndex = agentIndex,
				IsHero = hero != null || resolved?.HeroObject != null,
				CultureId = cultureIdOverride ?? resolved?.Culture?.StringId ?? hero?.Culture?.StringId ?? "neutral",
				Name = agent?.Name?.ToString() ?? hero?.Name?.ToString() ?? resolved?.Name?.ToString() ?? "",
				TroopId = resolved?.StringId ?? "",
				RoleDesc = soldierLikeResolved ? "士兵" : "",
				UnnamedRank = soldierLikeResolved ? "soldier" : "commoner"
			};
			return BuildRuntimePromptForAgent(hero ?? resolved?.HeroObject, packet, agentIndex);
		}
		catch
		{
			return "";
		}
	}

	internal static string BuildImmediateReactionIdentityOverrideForExternal(Hero hero, CharacterObject character, int agentIndex)
	{
		try
		{
			if (!IsActiveInCurrentMission())
			{
				return "";
			}
			Agent agent = TryGetAgent(agentIndex);
			CharacterObject resolved = character ?? (agent?.Character as CharacterObject) ?? hero?.CharacterObject;
			Hero resolvedHero = hero ?? resolved?.HeroObject;
			bool alliedSoldier = IsRuntimeAlliedSoldierAgent(agent, resolved, resolvedHero);
			bool civilian = IsCivilianForIntervention(resolved);
			return SiegeRuntimePromptProfile.BuildImmediateReactionIdentityOverride(ResolvePlayerCharacterNameForContext(), alliedSoldier, civilian);
		}
		catch
		{
			return "";
		}
	}

	internal static bool ShouldRunSiegeInterventionPostprocessForExternal()
	{
		return IsActiveInCurrentMission();
	}

	internal static List<PostprocessRuleEntry> BuildRuntimePostprocessRulesForExternal()
	{
		try
		{
			List<PostprocessRuleEntry> configured = AIConfigHandler.GetGuardrailRulePostprocessRules(SiegePostprocessRuleCatalog.RuleId) ?? new List<PostprocessRuleEntry>();
			List<PostprocessRuleEntry> rules = configured.Count > 0 ? configured : BuildFallbackSiegeInterventionPostprocessRules();
			bool destructiveAllowed = IsDestructiveInterventionAllowed();
			bool destructiveLocked = HasDestructiveOutcomeLocked();
			List<PostprocessRuleEntry> filtered = new List<PostprocessRuleEntry>();
			foreach (PostprocessRuleEntry rule in rules)
			{
				string tag = (rule?.Tag ?? "").Trim();
				if (string.IsNullOrWhiteSpace(tag))
				{
					continue;
				}
				if (!SiegePostprocessRuleFilter.ShouldAllowTag(tag, destructiveAllowed, destructiveLocked, _soldierAppeasementRequired, _soldierAppeasementApplied))
				{
					continue;
				}
				filtered.Add(rule);
			}
			return filtered;
		}
		catch
		{
			return new List<PostprocessRuleEntry>();
		}
	}

	private static List<PostprocessRuleEntry> BuildFallbackSiegeInterventionPostprocessRules()
	{
		return SiegePostprocessRuleCatalog.GetFallbackRules()
			.Select(rule => new PostprocessRuleEntry
			{
				Tag = rule.Tag,
				Description = rule.Description
			})
			.ToList();
	}

	internal static string BuildRuntimePostprocessContextForExternal(int targetAgentIndex, bool replyIsDirectPlayerResponse = false)
	{
		try
		{
			Agent agent = TryGetAgent(targetAgentIndex);
			CharacterObject character = agent?.Character as CharacterObject;
			bool alliedSoldier = IsRuntimeAlliedSoldierAgent(agent, character, character?.HeroObject);
			bool civilian = IsCivilianForIntervention(character);
			bool destructiveAllowed = IsDestructiveInterventionAllowed();
			string currentOutcome = SiegePostprocessOutcomeTextBuilder.Build(BuildPostprocessOutcomeFacts());
			string gatherContext = BuildCivilianGatherRuntimeContext(Mission.Current);
			string memoryContext = AppendRuntimeContext(
				BuildInterventionMemoryContext(SelectInterventionMemoryAudience(alliedSoldier, civilian)),
				BuildPlayerCommanderRuntimeContext(alliedSoldier, civilian));
			var facts = new SiegePostprocessContextFacts(
				settlementName: _activeSettlementName,
				currentOutcome: currentOutcome,
				destructiveAllowed: destructiveAllowed,
				speakerName: agent?.Name?.ToString() ?? character?.Name?.ToString() ?? SiegePostprocessContextBuilder.DefaultSpeakerName,
				speakerIdentity: SiegePostprocessContextBuilder.SelectSpeakerIdentity(alliedSoldier, civilian),
				targetAgentIndex: targetAgentIndex,
				replyIsDirectPlayerResponse: replyIsDirectPlayerResponse,
				sharedReliefPoolDescription: DescribeSharedCivilianReliefPoolForContext(),
				civilianGatherContext: gatherContext,
				interventionMemoryContext: memoryContext);
			return SiegePostprocessContextBuilder.Build(facts);
		}
		catch
		{
			return "";
		}
	}

	private static SiegePostprocessOutcomeFacts BuildPostprocessOutcomeFacts()
	{
		return new SiegePostprocessOutcomeFacts(
			_massacreStarted,
			_plunderStarted,
			_hasPendingAftermath,
			_pendingAftermath.ToString());
	}

	internal static string NormalizeSiegeInterventionPostprocessTagsForExternal(string raw, List<PostprocessRuleEntry> rules)
	{
		try
		{
			List<string> allowed = new List<string>();
			foreach (PostprocessRuleEntry rule in rules ?? new List<PostprocessRuleEntry>())
			{
				string tag = (rule?.Tag ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(tag))
				{
					allowed.Add(tag);
				}
			}
			return SiegePostprocessTagNormalizer.Normalize(raw, allowed);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "NormalizeSiegeInterventionPostprocessTagsForExternal failed: " + ex.Message);
			return "";
		}
	}

	internal static bool TryProcessAiActionTags(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, ref string text, out bool actionHandled, bool replyIsDirectPlayerResponse = false)
	{
		actionHandled = false;
		if (string.IsNullOrWhiteSpace(text) || !AnySiegeTagRegex.IsMatch(text))
		{
			return false;
		}
		try
		{
			if (!IsActiveInCurrentMission())
			{
				text = StripSiegeTags(text);
				return true;
			}
			bool destructiveAllowed = IsDestructiveInterventionAllowed();
			Agent targetAgent = TryGetAgent(targetAgentIndex);
			CharacterObject agentCharacter = targetAgent?.Character as CharacterObject;
			CharacterObject resolvedTargetCharacter = targetCharacter ?? agentCharacter ?? targetHero?.CharacterObject;
			bool targetIsAlliedSoldier = IsRuntimeAlliedSoldierAgent(targetAgent, resolvedTargetCharacter, targetHero ?? resolvedTargetCharacter?.HeroObject);
			bool hasSharedReliefPool = HasSharedCivilianReliefPool();
			bool targetIsCivilian = IsCivilianReliefConversationTarget(targetAgentIndex, resolvedTargetCharacter);
			SiegeActionRoutingDecision actionRouting = SiegeActionRoutingPolicy.Evaluate(new SiegeActionRoutingFacts(
				text,
				HasDestructiveOutcomeLocked(),
				targetIsAlliedSoldier,
				hasSharedReliefPool,
				replyIsDirectPlayerResponse));
			if (SiegeSharedReliefMercyUpgradePolicy.ShouldUpgradeMercyToRelief(text, hasSharedReliefPool, targetIsAlliedSoldier, targetIsCivilian))
			{
				text = MercyTagRegex.Replace(text, SiegePostprocessActionEffectProfile.NormalizedReliefTag);
				actionRouting = SiegeActionRoutingPolicy.Evaluate(new SiegeActionRoutingFacts(
					text,
					HasDestructiveOutcomeLocked(),
					targetIsAlliedSoldier,
					hasSharedReliefPool,
					replyIsDirectPlayerResponse));
			}
			bool containsDestructiveTag = actionRouting.ContainsDestructiveAction;
			bool canApplyMercyTrack = actionRouting.CanApplyMercyTrack;
			bool canApplySoldierMediatedDestructive = actionRouting.CanApplySoldierMediatedDestructiveAction && targetIsAlliedSoldier;
			if (destructiveAllowed && actionRouting.ShouldPromptSoldierDestructiveInquiry)
			{
				TryPromptSoldierDestructiveInquiry(targetAgent, targetAgentIndex, SiegeDestructiveInquiryProfile.InvalidSoldierMediatedTagReason);
				actionHandled = true;
			}
			if (destructiveAllowed && actionRouting.ShouldPromptSoldierForCivilianRobbery)
			{
				TryPromptSoldierDestructiveInquiry(targetAgent, targetAgentIndex, SiegeDestructiveInquiryProfile.CivilianRobberyReason);
				actionHandled = true;
			}
			if (actionRouting.ShouldDowngradeSoldierReliefToMercy)
			{
				text = ReliefTagRegex.Replace(text, SiegePostprocessActionEffectProfile.NormalizedMercyTag);
			}
			bool soldierPositiveCapToRelief = actionRouting.ShouldCapSoldierPositiveToRelief;
			if (!canApplyMercyTrack && !containsDestructiveTag && actionRouting.HasMercyTrackAction)
			{
				actionHandled |= TryBlockMercyTrackAfterDestructive(SiegePostprocessActionEffectProfile.BlockedMercyTrackActionName);
			}
			if (SoldierAppeasementTagRegex.IsMatch(text))
			{
				bool handled = ApplySoldierAppeasementChoice(targetAgentIndex);
				actionHandled |= handled;
				if (handled)
				{
					TryTriggerAmbientReactionsForAction(SiegeInterventionActionKind.AppeaseSoldiers, targetAgentIndex, targetAgentIndex, includeCivilians: false, includeSoldiers: true);
				}
			}
			if (GatherCiviliansTagRegex.IsMatch(text))
			{
				bool handled = GatherCiviliansForSpeech(SiegePostprocessActionEffectProfile.GatherCiviliansSource, targetAgentIndex);
				actionHandled |= handled;
				if (handled)
				{
					TryTriggerAmbientReactionsForAction(SiegeInterventionActionKind.GatherCivilians, targetAgentIndex, targetAgentIndex, includeCivilians: true, includeSoldiers: true);
				}
			}
			if (canApplyMercyTrack && MercyTagRegex.IsMatch(text))
			{
				bool handled = ApplyMercyChoice(SiegePostprocessActionEffectProfile.MercyTriggerSource, SiegePostprocessActionEffectProfile.MercyTriggerDetail);
				actionHandled |= handled;
				if (handled)
				{
					TryTriggerAmbientReactionsForAction(SiegeInterventionActionKind.Mercy, targetAgentIndex, targetAgentIndex, includeCivilians: true, includeSoldiers: true);
				}
			}
			if (canApplyMercyTrack && ReliefTagRegex.IsMatch(text))
			{
				bool handled = targetIsAlliedSoldier
					? ApplySoldierMaterialReliefChoice(targetAgentIndex, SiegePostprocessActionEffectProfile.SoldierMaterialReliefTriggerSource, SiegePostprocessActionEffectProfile.SoldierMaterialReliefTriggerDetail)
					: ApplyCivilianVerbalReliefChoice(SiegePostprocessActionEffectProfile.GetReliefTriggerSource(targetIsCivilian), SiegePostprocessActionEffectProfile.GetReliefTriggerDetail(targetIsCivilian));
				actionHandled |= handled;
				if (handled)
				{
					TryTriggerAmbientReactionsForAction(SiegeInterventionActionKind.Relief, targetAgentIndex, targetAgentIndex, includeCivilians: true, includeSoldiers: true);
				}
			}
			if (canApplyMercyTrack && soldierPositiveCapToRelief)
			{
				bool handled = ApplySoldierMaterialReliefChoice(targetAgentIndex, SiegePostprocessActionEffectProfile.SoldierMaterialReliefTriggerSource, SiegePostprocessActionEffectProfile.SoldierMaterialReliefTriggerDetail);
				actionHandled |= handled;
				if (handled)
				{
					TryTriggerAmbientReactionsForAction(SiegeInterventionActionKind.Relief, targetAgentIndex, targetAgentIndex, includeCivilians: true, includeSoldiers: true);
				}
			}
			if (canApplyMercyTrack && !soldierPositiveCapToRelief && InspireTagRegex.IsMatch(text))
			{
				bool handled = ApplyInspirationChoice(SiegePostprocessActionEffectProfile.InspirationTriggerSource, SiegePostprocessActionEffectProfile.InspirationTriggerDetail);
				actionHandled |= handled;
				if (handled)
				{
					TryTriggerAmbientReactionsForAction(SiegeInterventionActionKind.Inspire, targetAgentIndex, targetAgentIndex, includeCivilians: true, includeSoldiers: true);
				}
			}
			if (canApplyMercyTrack && !soldierPositiveCapToRelief && RallyOathTagRegex.IsMatch(text))
			{
				bool handled = ApplyRallyOathChoice(SiegePostprocessActionEffectProfile.RallyOathTriggerSource, SiegePostprocessActionEffectProfile.RallyOathTriggerDetail);
				actionHandled |= handled;
				if (handled)
				{
					TryTriggerAmbientReactionsForAction(SiegeInterventionActionKind.RallyOath, targetAgentIndex, targetAgentIndex, includeCivilians: true, includeSoldiers: true);
				}
			}
			bool hasPlunderTag = PlunderTagRegex.IsMatch(text);
			if (destructiveAllowed && actionRouting.CanApplyCivilianRobberyAction)
			{
				bool handled = ApplyCivilianRobberyChoice(targetAgentIndex, resolvedTargetCharacter, targetHero, text);
				actionHandled |= handled;
				if (handled)
				{
					TryTriggerAmbientReactionsForAction(SiegeInterventionActionKind.CivilianRobbery, targetAgentIndex, targetAgentIndex, includeCivilians: true, includeSoldiers: false);
				}
				TryPromptSoldierDestructiveInquiry(targetAgent, targetAgentIndex, SiegeDestructiveInquiryProfile.CivilianRobberyReason);
			}
			if (destructiveAllowed && hasPlunderTag && canApplySoldierMediatedDestructive)
			{
				bool handled = StartPlunder(SiegePostprocessActionEffectProfile.PlunderTriggerSource, SiegePostprocessActionEffectProfile.PlunderTriggerDetail);
				actionHandled |= handled;
				if (handled)
				{
					TryTriggerAmbientReactionsForAction(SiegeInterventionActionKind.Plunder, targetAgentIndex, targetAgentIndex, includeCivilians: true, includeSoldiers: true);
				}
			}
			if (destructiveAllowed && MassacreTagRegex.IsMatch(text) && canApplySoldierMediatedDestructive)
			{
				bool handled = StartMassacre(SiegePostprocessActionEffectProfile.MassacreTriggerSource, SiegePostprocessActionEffectProfile.MassacreTriggerDetail);
				actionHandled |= handled;
				if (handled)
				{
					TryTriggerAmbientReactionsForAction(SiegeInterventionActionKind.Massacre, targetAgentIndex, targetAgentIndex, includeCivilians: true, includeSoldiers: true);
				}
			}
			if (destructiveAllowed && RepopulationTagRegex.IsMatch(text) && canApplySoldierMediatedDestructive)
			{
				bool handled = RequestCulturalRepopulation(targetAgentIndex, SiegePostprocessActionEffectProfile.CulturalRepopulationTriggerSource, SiegePostprocessActionEffectProfile.CulturalRepopulationTriggerDetail);
				actionHandled |= handled;
				if (handled)
				{
					TryTriggerAmbientReactionsForAction(SiegeInterventionActionKind.CulturalRepopulation, targetAgentIndex, targetAgentIndex, includeCivilians: true, includeSoldiers: true);
				}
			}
			text = StripSiegeTags(text);
			return actionHandled;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryProcessAiActionTags failed: " + ex.Message);
			text = StripSiegeTags(text);
			return actionHandled;
		}
	}

	private static bool TryPromptSoldierDestructiveInquiry(Agent sourceAgent, int sourceAgentIndex, string reason)
	{
		try
		{
			if (!IsActiveInCurrentMission())
			{
				return false;
			}
			Mission mission = Mission.Current ?? sourceAgent?.Mission;
			if (mission?.Agents == null)
			{
				return false;
			}
			float now = mission.CurrentTime;
			if (now - _lastDestructiveInquiryMissionTime < SiegeDestructiveInquiryProfile.InquiryCooldownSeconds)
			{
				return false;
			}
			if (_lastDestructiveInquirySourceAgentIndex == sourceAgentIndex && now - _lastDestructiveInquiryMissionTime < SiegeDestructiveInquiryProfile.InquiryCooldownSeconds * 2f)
			{
				return false;
			}
			Agent main = Agent.Main ?? mission.MainAgent;
			Vec3 anchor = sourceAgent?.Position ?? main?.Position ?? Vec3.Zero;
			List<Agent> soldiers = mission.Agents
				.Where(a => a != null && a.IsHuman && a.IsActive() && AlliedAgentIndexes.Contains(a.Index) && a.Index != sourceAgentIndex)
				.OrderBy(a => a.Position.DistanceSquared(anchor))
				.ToList();
			if (soldiers.Count == 0)
			{
				return false;
			}
			string sourceName = sourceAgent?.Name?.ToString();
			string factText = SiegeDestructiveInquiryProfile.BuildInquiryFact(sourceName, reason);
			foreach (Agent soldier in soldiers)
			{
				if (!ShoutBehavior.TriggerImmediateSceneBehaviorReactionForExternal(factText, soldier.Index, persistHeroPrivateHistory: true, suppressStare: true, postSpeechLeaveSeconds: -1f))
				{
					continue;
				}
				_lastDestructiveInquiryMissionTime = now;
				_lastDestructiveInquirySourceAgentIndex = sourceAgentIndex;
				Logger.Log("SiegeAiIntervention", "Prompted soldier destructive inquiry. Soldier=" + soldier.Index + ", SourceAgent=" + sourceAgentIndex + ", Reason=" + (reason ?? "N/A"));
				return true;
			}
			return false;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryPromptSoldierDestructiveInquiry failed: " + ex.Message);
			return false;
		}
	}

	private static void TryTriggerOngoingAmbientReactions(Mission mission)
	{
		try
		{
			if (mission == null || !IsActiveInCurrentMission())
			{
				return;
			}
			if (_massacreStarted && !_massacreVictoryReached)
			{
				TryTriggerAmbientReactionsForAction(
					_culturalRepopulationRequested ? SiegeInterventionActionKind.CulturalRepopulation : SiegeInterventionActionKind.Massacre,
					directAgentIndex: -1,
					focusAgentIndex: -1,
					includeCivilians: true,
					includeSoldiers: true);
				return;
			}
			if (_plunderStarted && !_massacreStarted)
			{
				TryTriggerAmbientReactionsForAction(
					SiegeInterventionActionKind.Plunder,
					directAgentIndex: -1,
					focusAgentIndex: -1,
					includeCivilians: true,
					includeSoldiers: true);
				return;
			}
			if (_civilianGatherPropagationActive && !_civilianFormationControlComplete && !_massacreStarted)
			{
				TryTriggerAmbientReactionsForAction(
					SiegeInterventionActionKind.GatherCivilians,
					directAgentIndex: -1,
					focusAgentIndex: -1,
					includeCivilians: true,
					includeSoldiers: true);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryTriggerOngoingAmbientReactions failed: " + ex.Message);
		}
	}

	private static void TryTriggerAmbientReactionsForAction(
		SiegeInterventionActionKind action,
		int directAgentIndex,
		int focusAgentIndex,
		bool includeCivilians,
		bool includeSoldiers)
	{
		try
		{
			if (!IsActiveInCurrentMission())
			{
				return;
			}
			int civilianCount = includeCivilians ? TryTriggerAmbientReactionAudience(action, false, directAgentIndex, focusAgentIndex) : 0;
			int soldierCount = includeSoldiers ? TryTriggerAmbientReactionAudience(action, true, directAgentIndex, focusAgentIndex) : 0;
			if (civilianCount > 0 || soldierCount > 0)
			{
				Logger.Log("SiegeAiIntervention", "Queued staggered ambient reactions. Action=" + action + ", Civilians=" + civilianCount + ", Soldiers=" + soldierCount + ", DirectAgent=" + directAgentIndex);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryTriggerAmbientReactionsForAction failed: " + ex.Message);
		}
	}

	private static int TryTriggerAmbientReactionAudience(
		SiegeInterventionActionKind action,
		bool alliedSoldier,
		int directAgentIndex,
		int focusAgentIndex)
	{
		try
		{
			Mission mission = Mission.Current;
			if (mission?.Agents == null)
			{
				return 0;
			}
			float now = mission.CurrentTime;
			if (!CanStartAmbientReactionAudience(alliedSoldier, now) || HasPendingAmbientReactionAudience(alliedSoldier))
			{
				return 0;
			}
			Agent focus = TryGetAgent(focusAgentIndex);
			Agent direct = TryGetAgent(directAgentIndex);
			Agent anchor = focus ?? direct ?? Agent.Main ?? mission.MainAgent;
			string focusName = focus?.Name?.ToString() ?? direct?.Name?.ToString() ?? SiegeAmbientReactionProfile.DefaultFocusName;
			List<Agent> candidates = mission.Agents
				.Where(a => IsAmbientReactionCandidate(a, action, alliedSoldier, directAgentIndex, focusAgentIndex))
				.OrderBy(a => anchor != null ? a.Position.DistanceSquared(anchor.Position) : a.Index)
				.ThenBy(a => a.Index)
				.Take(MaxAmbientReactionSpeakersPerAudience)
				.ToList();
			if (candidates.Count == 0)
			{
				return 0;
			}
			MarkAmbientReactionAudienceWindowStarted(alliedSoldier, now);
			int queued = 0;
			foreach (Agent agent in candidates)
			{
				if (!QueueAmbientReactionRequest(action, agent, alliedSoldier, directAgentIndex, focusAgentIndex, focusName, now))
				{
					continue;
				}
				queued++;
			}
			if (queued > 0)
			{
				PumpPendingAmbientReactions(mission);
			}
			return queued;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryTriggerAmbientReactionAudience failed: " + ex.Message);
			return 0;
		}
	}

	private static bool HasPendingAmbientReactionAudience(bool alliedSoldier)
	{
		return PendingAmbientReactionRequests.Any(request => request != null && request.AlliedSoldier == alliedSoldier);
	}

	private static bool QueueAmbientReactionRequest(
		SiegeInterventionActionKind action,
		Agent agent,
		bool alliedSoldier,
		int directAgentIndex,
		int focusAgentIndex,
		string focusName,
		float now)
	{
		if (agent == null || agent.Index < 0)
		{
			return false;
		}
		float notBefore = Math.Max(now, _nextAmbientReactionRequestMissionTime);
		PendingAmbientReactionRequests.Add(new AmbientReactionRequest
		{
			Action = action,
			AlliedSoldier = alliedSoldier,
			AgentIndex = agent.Index,
			DirectAgentIndex = directAgentIndex,
			FocusAgentIndex = focusAgentIndex,
			FocusName = focusName,
			NotBeforeTime = notBefore
		});
		_nextAmbientReactionRequestMissionTime = notBefore + AmbientReactionRequestSpacingSeconds;
		return true;
	}

	private static void PumpPendingAmbientReactions(Mission mission)
	{
		try
		{
			if (mission == null || PendingAmbientReactionRequests.Count == 0)
			{
				return;
			}
			float now = mission.CurrentTime;
			AmbientReactionRequest request = PendingAmbientReactionRequests
				.Where(item => item != null && item.NotBeforeTime <= now)
				.OrderBy(item => item.NotBeforeTime)
				.ThenBy(item => item.AgentIndex)
				.FirstOrDefault();
			if (request == null)
			{
				return;
			}
			PendingAmbientReactionRequests.Remove(request);
			Agent agent = TryGetAgent(request.AgentIndex);
			if (!IsAmbientReactionCandidate(agent, request.Action, request.AlliedSoldier, request.DirectAgentIndex, request.FocusAgentIndex))
			{
				return;
			}
			if (TryTriggerAmbientReactionForAgent(request.Action, agent, request.AlliedSoldier, request.FocusName))
			{
				Logger.Log("SiegeAiIntervention", "Started staggered ambient reaction request. Action=" + request.Action + ", Agent=" + request.AgentIndex + ", AlliedSoldier=" + request.AlliedSoldier + ", NextQueue=" + PendingAmbientReactionRequests.Count);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "PumpPendingAmbientReactions failed: " + ex.Message);
		}
	}

	private static bool IsAmbientReactionCandidate(
		Agent agent,
		SiegeInterventionActionKind action,
		bool alliedSoldier,
		int directAgentIndex,
		int focusAgentIndex)
	{
		try
		{
			if (agent == null || !agent.IsHuman || agent == Agent.Main || !agent.IsActive() || agent.State == AgentState.Killed || agent.State == AgentState.Unconscious)
			{
				return false;
			}
			if (agent.Index == directAgentIndex || agent.Index == focusAgentIndex)
			{
				return false;
			}
			if (alliedSoldier)
			{
				return IsInterventionAlliedSoldierForExternal(agent, requireActive: true);
			}
			if (action == SiegeInterventionActionKind.Massacre || action == SiegeInterventionActionKind.CulturalRepopulation)
			{
				return IsMassacreTargetAgent(agent, includeHeroes: true);
			}
			return IsEligibleCivilianAgent(agent, includeHeroes: true);
		}
		catch
		{
			return false;
		}
	}

	private static bool TryTriggerAmbientReactionForAgent(SiegeInterventionActionKind action, Agent agent, bool alliedSoldier, string focusName)
	{
		try
		{
			if (agent == null)
			{
				return false;
			}
			string factText = SiegeAmbientReactionProfile.BuildFact(
				action,
				alliedSoldier,
				DoesAmbientSpeakerCultureMatchSettlement(agent),
				_activeSettlementName,
				focusName);
			return ShoutBehavior.TriggerImmediateSceneBehaviorReactionForExternal(factText, agent.Index, persistHeroPrivateHistory: true, suppressStare: true, postSpeechLeaveSeconds: -1f);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryTriggerAmbientReactionForAgent failed: " + ex.Message);
			return false;
		}
	}

	private static bool CanStartAmbientReactionAudience(bool alliedSoldier, float now)
	{
		float last = alliedSoldier ? _lastAmbientSoldierReactionMissionTime : _lastAmbientCivilianReactionMissionTime;
		return now - last >= AmbientReactionWindowSeconds;
	}

	private static void MarkAmbientReactionAudienceStarted(bool alliedSoldier, float now)
	{
		MarkAmbientReactionAudienceWindowStarted(alliedSoldier, now);
		ReserveAmbientReactionRequestSlot(now);
	}

	private static void MarkAmbientReactionAudienceWindowStarted(bool alliedSoldier, float now)
	{
		if (alliedSoldier)
		{
			_lastAmbientSoldierReactionMissionTime = now;
		}
		else
		{
			_lastAmbientCivilianReactionMissionTime = now;
		}
	}

	private static void ReserveAmbientReactionRequestSlot(float now)
	{
		float next = now + AmbientReactionRequestSpacingSeconds;
		if (_nextAmbientReactionRequestMissionTime < next)
		{
			_nextAmbientReactionRequestMissionTime = next;
		}
	}

	private static bool DoesAmbientSpeakerCultureMatchSettlement(Agent agent)
	{
		try
		{
			CharacterObject character = agent?.Character as CharacterObject;
			CultureObject agentCulture = character?.Culture;
			CultureObject settlementCulture = ResolveCurrentSettlement()?.Culture;
			if (agentCulture == null || settlementCulture == null)
			{
				return false;
			}
			return ReferenceEquals(agentCulture, settlementCulture)
				|| string.Equals(agentCulture.StringId ?? "", settlementCulture.StringId ?? "", StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	private static bool ContainsAny(string text, params string[] needles)
	{
		if (string.IsNullOrWhiteSpace(text) || needles == null)
		{
			return false;
		}
		foreach (string needle in needles)
		{
			if (!string.IsNullOrWhiteSpace(needle) && text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	internal static bool ShouldSuppressEnemyGuardsForSceneConflict()
	{
		return IsActiveInCurrentMission();
	}

	internal static bool IsOccupationSceneActiveForExternal()
	{
		return IsActiveInCurrentMission();
	}

	internal static bool IsInterventionAlliedSoldierForExternal(Agent agent, bool requireActive = false)
	{
		try
		{
			if (!IsActiveInCurrentMission() || agent == null || !agent.IsHuman || agent == Agent.Main)
			{
				return false;
			}
			if (requireActive && (!agent.IsActive() || agent.State == AgentState.Killed || agent.State == AgentState.Unconscious))
			{
				return false;
			}
			return AlliedAgentIndexes.Contains(agent.Index);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsInterventionBannerBearer(Agent agent)
	{
		try
		{
			return agent != null && BannerBearerAgentIndexes.Contains(agent.Index);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsCommandableInterventionSoldier(Agent agent, bool requireActive = false)
	{
		return IsInterventionAlliedSoldierForExternal(agent, requireActive) && !IsInterventionBannerBearer(agent);
	}

	private static void EnsureInterventionMissionCombatModeForPlayerDamage(Mission mission)
	{
		try
		{
			if (mission == null || mission.IsMissionEnding || mission.Mode == MissionMode.Conversation || mission.Mode == MissionMode.Barter)
			{
				return;
			}
			if (_massacreStarted && !_massacreVictoryReached)
			{
				if (mission.Mode != MissionMode.Battle)
				{
					mission.SetMissionMode(MissionMode.Battle, atStart: false);
				}
				return;
			}
			// 非血洗处置阶段保持城镇/村庄原版 StartUp 模式，避免 TAB 变成战斗计分板/撤退界面。
			if (mission.Mode == MissionMode.Battle)
			{
				mission.SetMissionMode(MissionMode.StartUp, atStart: false);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "EnsureInterventionMissionCombatModeForPlayerDamage failed: " + ex.Message);
		}
	}

	private static void EnsureInterventionPlayerCommandTeam(Mission mission)
	{
		try
		{
			Agent main = Agent.Main ?? mission?.MainAgent;
			if (mission == null || mission.IsMissionEnding || main == null || !main.IsActive())
			{
				return;
			}
			Team playerTeam = mission.PlayerTeam;
			if (_interventionPlayerCommandTeam != null)
			{
				playerTeam = _interventionPlayerCommandTeam;
				if (mission.PlayerTeam != playerTeam)
				{
					mission.PlayerTeam = playerTeam;
				}
			}
			if (playerTeam == null || !playerTeam.IsPlayerGeneral)
			{
				uint color = Hero.MainHero?.MapFaction?.Color ?? 0xFF2020FFu;
				uint color2 = Hero.MainHero?.MapFaction?.Color2 ?? 0xFF101080u;
				Banner banner = Hero.MainHero?.Clan?.Banner;
				try
				{
					playerTeam = mission.Teams.Add(BattleSideEnum.Attacker, color, color2, banner, isPlayerGeneral: true, isPlayerSergeant: false);
					_interventionPlayerCommandTeam = playerTeam;
				}
				catch (Exception ex)
				{
					Logger.Log("SiegeAiIntervention", "Creating intervention player command team failed: " + ex.Message);
					playerTeam = mission.PlayerTeam ?? main.Team;
				}
				if (playerTeam != null)
				{
					mission.PlayerTeam = playerTeam;
				}
			}
			else
			{
				_interventionPlayerCommandTeam = playerTeam;
			}
			if (playerTeam == null)
			{
				return;
			}
			if (main.Team != playerTeam)
			{
				main.SetTeam(playerTeam, true);
			}
			try
			{
				Agent mount = main.MountAgent;
				if (mount != null && mount.IsActive() && mount.Team != playerTeam)
				{
					mount.SetTeam(playerTeam, true);
				}
			}
			catch
			{
			}
			try
			{
				foreach (Team team in mission.Teams)
				{
					if (team == null || team == playerTeam)
					{
						continue;
					}
					if (!_massacreStarted)
					{
						team.SetIsEnemyOf(playerTeam, isEnemyOf: false);
						playerTeam.SetIsEnemyOf(team, isEnemyOf: false);
					}
				}
			}
			catch
			{
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "EnsureInterventionPlayerCommandTeam failed: " + ex.Message);
		}
	}

	private static Team EnsureInterventionCivilianEnemyTeam(Mission mission)
	{
		try
		{
			if (mission == null)
			{
				return null;
			}
			Agent main = Agent.Main ?? mission.MainAgent;
			Team playerTeam = mission.PlayerTeam ?? _interventionPlayerCommandTeam ?? main?.Team;
			if (_interventionCivilianEnemyTeam != null && _interventionCivilianEnemyTeam != playerTeam)
			{
				if (playerTeam != null)
				{
					_interventionCivilianEnemyTeam.SetIsEnemyOf(playerTeam, isEnemyOf: true);
					playerTeam.SetIsEnemyOf(_interventionCivilianEnemyTeam, isEnemyOf: true);
				}
				return _interventionCivilianEnemyTeam;
			}
			Team enemyTeam = mission.PlayerEnemyTeam;
			if (enemyTeam == null || enemyTeam == playerTeam)
			{
				try
				{
					enemyTeam = mission.Teams.Add(BattleSideEnum.Defender, 0xFF7A2020u, 0xFF2A0808u, null, isPlayerGeneral: false, isPlayerSergeant: false);
				}
				catch (Exception ex)
				{
					Logger.Log("SiegeAiIntervention", "Creating civilian massacre enemy team failed: " + ex.Message);
					enemyTeam = mission.PlayerEnemyTeam;
				}
			}
			if (enemyTeam != null && playerTeam != null && enemyTeam != playerTeam)
			{
				enemyTeam.SetIsEnemyOf(playerTeam, isEnemyOf: true);
				playerTeam.SetIsEnemyOf(enemyTeam, isEnemyOf: true);
			}
			_interventionCivilianEnemyTeam = enemyTeam;
			return enemyTeam;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "EnsureInterventionCivilianEnemyTeam failed: " + ex.Message);
			return null;
		}
	}

	internal static bool ShouldRedirectResolvedAftermathMenuForExternal(string menuId)
	{
		try
		{
			if (Mission.Current != null || !_afAftermathResolved || _nativeDevastateAftermathFlowActive || string.IsNullOrWhiteSpace(menuId) || !DoesCompletedAftermathMatchCurrentSettlement())
			{
				return false;
			}
			return IsNativeSiegeAftermathMenuId(menuId);
		}
		catch
		{
			return false;
		}
	}

	internal static bool TryHandleNativeAftermathMenuActivationForExternal(string menuId)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(menuId) || !IsNativeSiegeAftermathMenuId(menuId))
			{
				return false;
			}
			string activationSource = SiegeAftermathTransitionSourceProfile.BuildNativeMenuActivationSource(menuId);
			if (TryHandleDirectMassacreAftermathMenuForExternal(menuId, activationSource))
			{
				return true;
			}
			if (TryHandleDirectPlunderAftermathMenuForExternal(menuId, activationSource))
			{
				return true;
			}
			if (!DoesCompletedAftermathMatchCurrentSettlement() && _activeMode == InterventionMode.None && _pendingMode == InterventionMode.None)
			{
				return false;
			}
			if (TryHandleNativeAftermathMenuInitForExternal(activationSource))
			{
				return true;
			}
			if (!ShouldSuppressNativeAftermathMenuDuringInterventionTransition())
			{
				return false;
			}
			if (Mission.Current == null && !_hasPendingAftermath && (_pendingSummarySwitch || _pendingEncounterFinish || _afAftermathResolved))
			{
				SiegeAftermathAction.SiegeAftermath aftermath = _afAftermathResolved
					? _completedAftermath
					: (_pendingEncounterFinish ? _pendingEncounterFinishAftermath : _pendingSummaryAftermath);
				string transitionSource = SiegeAftermathTransitionSourceProfile.BuildNativeMenuActivationTransitionSource(menuId);
				if (string.IsNullOrWhiteSpace(_completedSummaryText))
				{
					PrepareCompletedInterventionSummary(aftermath);
				}
				QueueEncounterFinishAfterIntervention(aftermath, transitionSource, 0, forceDelay: true);
				TryFinishPlayerEncounterAfterInterventionNow(aftermath, transitionSource);
			}
			Logger.Log("SiegeAiIntervention", "Suppressed native siege aftermath menu during GCCZ transition. Menu=" + menuId + ", MissionActive=" + (Mission.Current != null) + ", PendingSummary=" + _pendingSummarySwitch + ", PendingFinish=" + _pendingEncounterFinish + ", PendingAftermath=" + _hasPendingAftermath + ", Resolved=" + _afAftermathResolved);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryHandleNativeAftermathMenuActivationForExternal failed: " + ex.Message);
			return false;
		}
	}

	private static bool ShouldSuppressNativeAftermathMenuDuringInterventionTransition()
	{
		return _activeMode != InterventionMode.None
			|| _pendingMode != InterventionMode.None
			|| _pendingSummarySwitch
			|| _pendingEncounterFinish
			|| _hasPendingAftermath
			|| _afAftermathResolved
			|| _directMassacreAftermathScriptPending
			|| _directPlunderAftermathScriptPending;
	}

	internal static bool TryHandleNativeAftermathMenuInitForExternal(string source)
	{
		try
		{
			if (_directPlunderAftermathScriptPending)
			{
				TryRunDirectPlunderAftermathScript(SiegeAftermathTransitionSourceProfile.BuildNativeMenuInitSource(source));
				return true;
			}
			if (Mission.Current != null || !_afAftermathResolved || !DoesCompletedAftermathMatchCurrentSettlement())
			{
				return false;
			}
			if (string.IsNullOrWhiteSpace(_completedSummaryText))
			{
				PrepareCompletedInterventionSummary(_completedAftermath);
			}
			if (_nativeDevastateAftermathFlowActive && _completedAftermath == SiegeAftermathAction.SiegeAftermath.Devastate)
			{
				TrySetNativePlayerEncounterAftermathForSummary(SiegeAftermathAction.SiegeAftermath.Devastate);
				if ((source ?? "").IndexOf(SiegeAftermathMenuProfile.ContextualSummarySourceMarker, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					Logger.Log("SiegeAiIntervention", "Allowing native Devastate contextual summary init for AF massacre. Source=" + (source ?? "N/A"));
					return false;
				}
				GameMenu.SwitchToMenu(SiegeAftermathMenuProfile.ContextualSummaryMenuId);
				Logger.Log("SiegeAiIntervention", "Auto-routed native siege aftermath menu to Devastate summary for AF massacre. Source=" + (source ?? "N/A"));
				return true;
			}
			QueueEncounterFinishAfterIntervention(_completedAftermath, SiegeAftermathTransitionSourceProfile.BuildNativeMenuInitSource(source), 0, forceDelay: true);
			TryFinishPlayerEncounterAfterInterventionNow(_completedAftermath, SiegeAftermathTransitionSourceProfile.BuildNativeMenuInitSource(source));
			Logger.Log("SiegeAiIntervention", "Suppressed native siege aftermath menu init and requested encounter finish after AF resolved aftermath. Source=" + (source ?? "N/A"));
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryHandleNativeAftermathMenuInitForExternal failed: " + ex.Message);
			return false;
		}
	}

	internal static bool TryHandleNativeAftermathSummaryContinueForExternal(string source)
	{
		try
		{
			if (!_nativeDevastateAftermathFlowActive || !_afAftermathResolved || _completedAftermath != SiegeAftermathAction.SiegeAftermath.Devastate || !DoesCompletedAftermathMatchCurrentSettlement())
			{
				return false;
			}
			string menuId = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
			if (!SiegeAftermathMenuProfile.IsContextualSummaryMenuId(menuId))
			{
				return false;
			}
			if (_nativeDevastateSummaryContinueHandled)
			{
				return true;
			}
			_nativeDevastateSummaryContinueHandled = true;
			try
			{
				GameMenu.ExitToLast();
			}
			catch
			{
			}
			if (_pendingLootRoster != null && _pendingLootRoster.Count > 0)
			{
				_pendingLootScreen = true;
				_pendingLootScreenShown = true;
				_pendingSummarySwitch = true;
				QueueEncounterFinishAfterIntervention(SiegeAftermathAction.SiegeAftermath.Devastate, SiegeAftermathTransitionSourceProfile.NativeDevastateSummaryContinueLootSource, 0, forceDelay: true);
				InventoryScreenHelper.OpenScreenAsLoot(new Dictionary<PartyBase, ItemRoster>
				{
					{
						PartyBase.MainParty,
						_pendingLootRoster
					}
				});
				Logger.Log("SiegeAiIntervention", "Opened native loot screen after AF massacre Devastate summary. Source=" + (source ?? "N/A") + ", LootItems=" + _pendingLootRoster.Count);
				return true;
			}
			QueueEncounterFinishAfterIntervention(SiegeAftermathAction.SiegeAftermath.Devastate, SiegeAftermathTransitionSourceProfile.NativeDevastateSummaryContinueNoLootSource, 0, forceDelay: true);
			TryFinishPlayerEncounterAfterInterventionNow(SiegeAftermathAction.SiegeAftermath.Devastate, SiegeAftermathTransitionSourceProfile.NativeDevastateSummaryContinueNoLootSource);
			Logger.Log("SiegeAiIntervention", "Finished AF massacre Devastate summary without loot. Source=" + (source ?? "N/A"));
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryHandleNativeAftermathSummaryContinueForExternal failed: " + ex.Message);
			return false;
		}
	}

	internal static bool TryHandleDirectMassacreAftermathMenuForExternal(string menuId, string source)
	{
		try
		{
			if (!_directMassacreAftermathScriptPending || string.IsNullOrWhiteSpace(menuId))
			{
				return false;
			}
			if (!IsNativeSiegeAftermathMenuId(menuId))
			{
				return false;
			}
			Logger.Log("SiegeAiIntervention", "Intercepted native siege aftermath menu for direct AF massacre loot script. Menu=" + menuId + ", Source=" + (source ?? "N/A"));
			if (Mission.Current != null)
			{
				LogDirectMassacreLootDeferOnce(SiegeDirectAftermathSourceProfile.BuildNativeMenuMissionCurrentSource(menuId), "Suppressed native siege aftermath menu while Mission.Current is still active; direct AF massacre loot will be pumped after MapState. Menu=" + menuId + ", Source=" + (source ?? "N/A"));
				return true;
			}
			if (!TryOpenDirectMassacreLootScreenNow(source ?? SiegeDirectAftermathSourceProfile.NativeMenuInterceptSource) && IsSafeToOpenDirectMassacreLootScreen(source ?? SiegeDirectAftermathSourceProfile.NativeMenuInterceptNoLootProbeSource))
			{
				QueueEncounterFinishAfterIntervention(SiegeAftermathAction.SiegeAftermath.Devastate, SiegeDirectAftermathSourceProfile.DirectMassacreNativeMenuNoLootSource, 0, forceDelay: true);
				TryFinishPlayerEncounterAfterInterventionNow(SiegeAftermathAction.SiegeAftermath.Devastate, SiegeDirectAftermathSourceProfile.DirectMassacreNativeMenuNoLootSource);
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryHandleDirectMassacreAftermathMenuForExternal failed: " + ex.Message);
			return false;
		}
	}

	internal static bool TryHandleDirectPlunderAftermathMenuForExternal(string menuId, string source)
	{
		try
		{
			if (!_directPlunderAftermathScriptPending || string.IsNullOrWhiteSpace(menuId))
			{
				return false;
			}
			if (!IsNativeSiegeAftermathMenuId(menuId))
			{
				return false;
			}
			Logger.Log("SiegeAiIntervention", "Intercepted native siege aftermath menu for direct AF plunder loot script. Menu=" + menuId + ", Source=" + (source ?? "N/A"));
			if (Mission.Current != null)
			{
				LogDirectPlunderLootDeferOnce(SiegeDirectAftermathSourceProfile.BuildNativeMenuMissionCurrentSource(menuId), "Suppressed native siege aftermath menu while Mission.Current is still active; direct AF plunder loot will be pumped after MapState. Menu=" + menuId + ", Source=" + (source ?? "N/A"));
				return true;
			}
			if (!TryOpenDirectPlunderLootScreenNow(source ?? SiegeDirectAftermathSourceProfile.NativeMenuInterceptSource) && IsSafeToOpenDirectPlunderLootScreen(source ?? SiegeDirectAftermathSourceProfile.NativeMenuInterceptNoLootProbeSource))
			{
				QueueEncounterFinishAfterIntervention(SiegeAftermathAction.SiegeAftermath.Pillage, SiegeDirectAftermathSourceProfile.DirectPlunderNativeMenuNoLootSource, 0, forceDelay: true);
				TryFinishPlayerEncounterAfterInterventionNow(SiegeAftermathAction.SiegeAftermath.Pillage, SiegeDirectAftermathSourceProfile.DirectPlunderNativeMenuNoLootSource);
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryHandleDirectPlunderAftermathMenuForExternal failed: " + ex.Message);
			return false;
		}
	}

	internal static bool TryPumpDirectMassacreAftermathScriptForExternal(string source)
	{
		try
		{
			if (!_directMassacreAftermathScriptPending)
			{
				return false;
			}
			return TryRunDirectMassacreAftermathScript(source ?? SiegeDirectAftermathSourceProfile.ExternalDirectMassacreScriptSource);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryPumpDirectMassacreAftermathScriptForExternal failed. Source=" + (source ?? "N/A") + ", Error=" + ex.Message);
			return true;
		}
	}

	internal static bool TryHandlePlayerAttackForIntervention(Agent affectedAgent, string source, float damagedHp = 0f)
	{
		if (!IsActiveInCurrentMission() || affectedAgent == null || !affectedAgent.IsHuman || affectedAgent == Agent.Main)
		{
			return false;
		}
		if (AlliedAgentIndexes.Contains(affectedAgent.Index))
		{
			return TryHandleFriendlyHitOnAlliedSoldier(affectedAgent, source, damagedHp);
		}
		if (!IsMassacreTargetAgent(affectedAgent, includeHeroes: true))
		{
			return false;
		}
		if (_massacreStarted)
		{
			PrepareCivilianForMassacreCombat(affectedAgent, Mission.Current ?? affectedAgent.Mission);
			return true;
		}
		return HandlePlayerLocalAttackInIntervention(affectedAgent, source, damagedHp);
	}

	private static bool HandlePlayerLocalAttackInIntervention(Agent affectedAgent, string source, float damagedHp)
	{
		try
		{
			Mission mission = Mission.Current ?? affectedAgent?.Mission;
			Agent main = Agent.Main ?? mission?.MainAgent;
			if (mission == null || affectedAgent == null || !affectedAgent.IsActive())
			{
				return false;
			}
			string targetName = affectedAgent.Name?.ToString();
			bool targetWillResist = ShouldCivilianResistMassacre(affectedAgent);
			bool firstHit = LocalPlayerAttackVictimAgentIndexes.Add(affectedAgent.Index);
			if (firstHit)
			{
				TryApplyRegionalConflictTrustPenalty(ResolveCurrentSettlement(), affectedAgent, targetName, victimDown: false, source ?? SiegeLocalAttackProfile.LocalAttackSource);
				InformationManager.DisplayMessage(new InformationMessage(SiegeLocalAttackProfile.BuildPlayerHitMessage(targetName, targetWillResist), Color.FromUint(SiegeLocalAttackProfile.MessageColor)));
				RecordInterventionMemory(SiegeLocalAttackProfile.MemoryTitle, SiegeLocalAttackProfile.BuildPlayerHitMemoryText(targetName, targetWillResist));
			}
			NeutralizeCivilianDailyUsableBehavior(affectedAgent, SiegeLocalAttackProfile.LocalAttackSource);
			affectedAgent.SetMortalityState(Agent.MortalityState.Mortal);
			try
			{
				affectedAgent.SetCrouchMode(false);
				affectedAgent.SetMaximumSpeedLimit(-1f, false);
			}
			catch
			{
			}
			if (targetWillResist)
			{
				PrepareLocalHostileCivilian(affectedAgent, mission, main);
			}
			else
			{
				PrepareLocalFleeingCivilian(affectedAgent, mission, main);
			}
			if (firstHit)
			{
				TriggerLocalCivilianWitnessReactions(mission, affectedAgent, main, victimDown: false, targetName);
			}
			Logger.Log("SiegeAiIntervention", "Handled local player attack without starting massacre. Source=" + (source ?? "N/A") + ", Agent=" + affectedAgent.Index + ", Resist=" + targetWillResist + ", Damage=" + damagedHp.ToString("0.0"));
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "HandlePlayerLocalAttackInIntervention failed: " + ex.Message);
			return false;
		}
	}

	private static bool TryApplyRegionalConflictTrustPenalty(Settlement settlement, Agent victim, string targetName, bool victimDown, string source)
	{
		try
		{
			if (settlement == null || victim == null)
			{
				return false;
			}
			Vec3 center = victim.Position;
			if (!TryReserveRegionalConflictDebtArea(center))
			{
				Logger.Log("SiegeAiIntervention", "Skipped regional conflict trust penalty inside existing debt area. Source=" + (source ?? "N/A")
					+ ", Victim=" + (targetName ?? "N/A")
					+ ", Down=" + victimDown
					+ ", Incidents=" + _regionalConflictIncidentCount);
				return false;
			}
			_regionalConflictIncidentCount++;
			AdjustSettlementPublicTrustOnly(
				settlement,
				SiegeRegionalConflictProfile.SettlementPublicTrustDeltaPerIncident,
				SiegeRegionalConflictProfile.SettlementPublicTrustReason);
			InformationManager.DisplayMessage(new InformationMessage(
				SiegeRegionalConflictProfile.BuildConflictNoticeMessage(targetName, victimDown),
				Color.FromUint(SiegeLocalAttackProfile.MessageColor)));
			Logger.Log("SiegeAiIntervention", "Applied regional conflict trust debt. Source=" + (source ?? "N/A")
				+ ", Settlement=" + (settlement.StringId ?? "N/A")
				+ ", Victim=" + (targetName ?? "N/A")
				+ ", Down=" + victimDown
				+ ", Incidents=" + _regionalConflictIncidentCount
				+ ", AreaDiameter=" + SiegeRegionalConflictProfile.ConflictAreaDiameter.ToString("0.0")
				+ ", SettlementTrust=" + SiegeRegionalConflictProfile.SettlementPublicTrustDeltaPerIncident);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryApplyRegionalConflictTrustPenalty failed: " + ex.Message);
			return false;
		}
	}

	private static bool TryReserveRegionalConflictDebtArea(Vec3 center)
	{
		try
		{
			foreach (Vec3 existingCenter in RegionalConflictDebtCenters)
			{
				if (SiegeRegionalConflictProfile.IsInsideConflictAreaSquared(existingCenter.DistanceSquared(center)))
				{
					return false;
				}
			}
			RegionalConflictDebtCenters.Add(center);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void PrepareLocalHostileCivilian(Agent agent, Mission mission, Agent main, string source = SiegeLocalAttackProfile.LocalHostileSource)
	{
		try
		{
			if (agent == null || mission == null || !agent.IsActive())
			{
				return;
			}
			LocalHostileCivilianAgentIndexes.Add(agent.Index);
			LocalFleeingCivilianAgentIndexes.Remove(agent.Index);
			NeutralizeCivilianDailyUsableBehavior(agent, source ?? SiegeLocalAttackProfile.LocalHostileSource);
			Team playerTeam = mission.PlayerTeam ?? main?.Team;
			Team enemyTeam = EnsureInterventionCivilianEnemyTeam(mission) ?? mission.PlayerEnemyTeam ?? agent.Team;
			if (enemyTeam != null && agent.Team != enemyTeam)
			{
				agent.SetTeam(enemyTeam, true);
			}
			if (agent.Team != null && playerTeam != null && agent.Team != playerTeam)
			{
				agent.Team.SetIsEnemyOf(playerTeam, isEnemyOf: true);
				playerTeam.SetIsEnemyOf(agent.Team, isEnemyOf: true);
			}
			agent.SetWatchState(Agent.WatchState.Alarmed);
			try
			{
				agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.Any);
			}
			catch
			{
			}
			ForceAgentForMassacreFight(agent);
			TryStartOrJoinLocalNativeFight(mission, agent, source ?? SiegeLocalAttackProfile.LocalHostileSource);
			if (main != null && main.IsActive())
			{
				agent.SetLookAgent(main);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "PrepareLocalHostileCivilian failed: " + ex.Message);
		}
	}

	private static void PrepareLocalFleeingCivilian(Agent agent, Mission mission, Agent main, string source = SiegeLocalAttackProfile.LocalFleeSource)
	{
		try
		{
			if (agent == null || mission == null || !agent.IsActive())
			{
				return;
			}
			LocalFleeingCivilianAgentIndexes.Add(agent.Index);
			LocalHostileCivilianAgentIndexes.Remove(agent.Index);
			NeutralizeCivilianDailyUsableBehavior(agent, source ?? SiegeLocalAttackProfile.LocalFleeSource);
			agent.InvalidateTargetAgent();
			ClearAgentLookTarget(agent);
			agent.SetWatchState(Agent.WatchState.Alarmed);
			ActivateNativeLocalFleeBehavior(agent, source ?? SiegeLocalAttackProfile.LocalFleeSource);
			if (main != null && !IsLocalNativeFightActive(mission))
			{
				KeepCivilianHidingFromOccupation(agent, mission, main, force: true);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "PrepareLocalFleeingCivilian failed: " + ex.Message);
		}
	}

	private static void ActivateNativeLocalFleeBehavior(Agent agent, string source)
	{
		try
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive() || !LocalFleeingCivilianAgentIndexes.Contains(agent.Index))
			{
				return;
			}
			ActivateCivilianPanicFleeBehavior(agent, source ?? SiegeLocalCivilianReactionProfile.NativeFleeBridgeSource);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ActivateNativeLocalFleeBehavior failed (" + (source ?? SiegeLocalCivilianReactionProfile.NativeFleeBridgeSource) + "): " + ex.Message);
		}
	}

	private static void ActivateCivilianPanicFleeBehavior(Agent agent, string source)
	{
		try
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive())
			{
				return;
			}
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			AgentNavigator navigator = component?.AgentNavigator ?? component?.CreateAgentNavigator();
			if (navigator == null)
			{
				return;
			}
			AlarmedBehaviorGroup alarmedGroup = navigator.GetBehaviorGroup<AlarmedBehaviorGroup>() ?? navigator.AddBehaviorGroup<AlarmedBehaviorGroup>();
			if (alarmedGroup == null)
			{
				return;
			}
			alarmedGroup.DisableCalmDown = true;
			FleeBehavior fleeBehavior = alarmedGroup.GetBehavior<FleeBehavior>() ?? alarmedGroup.AddBehavior<FleeBehavior>();
			if (fleeBehavior == null)
			{
				return;
			}
			alarmedGroup.SetScriptedBehavior<FleeBehavior>();
			agent.SetWatchState(Agent.WatchState.Alarmed);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ActivateCivilianPanicFleeBehavior failed (" + (source ?? "N/A") + "): " + ex.Message);
		}
	}

	private static bool IsLocalNativeFightActive(Mission mission)
	{
		try
		{
			return _localNativeFightStarted && (mission?.GetMissionBehavior<MissionFightHandler>()?.IsThereActiveFight() ?? false);
		}
		catch
		{
			return false;
		}
	}

	private static void TryStartOrJoinLocalNativeFight(Mission mission, Agent hostile, string source)
	{
		try
		{
			Agent main = Agent.Main ?? mission?.MainAgent;
			if (!IsActiveInCurrentMission() || mission == null || hostile == null || main == null || !hostile.IsActive() || !main.IsActive())
			{
				return;
			}
			MissionFightHandler fightHandler = mission.GetMissionBehavior<MissionFightHandler>();
			if (fightHandler == null)
			{
				return;
			}
			if (fightHandler.IsThereActiveFight())
			{
				if (_localNativeFightStarted)
				{
					fightHandler.AddAgentToSide(hostile, false);
				}
				return;
			}
			fightHandler.StartCustomFight(
				new List<Agent> { main },
				new List<Agent> { hostile },
				false,
				false,
				null,
				0f);
			_localNativeFightStarted = true;
			Logger.Log("SiegeAiIntervention", "Started native local conflict fight for GCCZ regional violence. Source=" + (source ?? SiegeLocalCivilianReactionProfile.NativeLocalFightSource) + ", Hostile=" + hostile.Index);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryStartOrJoinLocalNativeFight failed (" + (source ?? SiegeLocalCivilianReactionProfile.NativeLocalFightSource) + "): " + ex.Message);
		}
	}

	private static void TryEndLocalNativeFight(Mission mission, string source)
	{
		try
		{
			if (!_localNativeFightStarted)
			{
				return;
			}
			MissionFightHandler fightHandler = mission?.GetMissionBehavior<MissionFightHandler>();
			if (fightHandler != null && fightHandler.IsThereActiveFight())
			{
				fightHandler.EndFight(true);
			}
			_localNativeFightStarted = false;
			Logger.Log("SiegeAiIntervention", "Ended native local conflict fight. Source=" + (source ?? "N/A"));
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryEndLocalNativeFight failed (" + (source ?? "N/A") + "): " + ex.Message);
		}
	}

	private static bool IsLocalNativeFleeAllowedForExternal(Agent agent)
	{
		try
		{
			return IsOccupationSceneActiveForExternal()
				&& !_massacreStarted
				&& agent != null
				&& agent.Index >= 0
				&& LocalFleeingCivilianAgentIndexes.Contains(agent.Index)
				&& IsEligibleCivilianAgent(agent, includeHeroes: true, requireActive: false);
		}
		catch
		{
			return false;
		}
	}

	private static void TriggerLocalCivilianWitnessReactions(Mission mission, Agent victim, Agent main, bool victimDown, string targetName)
	{
		try
		{
			if (mission?.Agents == null || victim == null)
			{
				return;
			}
			Agent player = main ?? Agent.Main ?? mission.MainAgent;
			float now = mission.CurrentTime;
			TryTriggerLocalSoldierWitnessInquiry(mission, victim, victimDown, targetName);
			List<Agent> witnesses = mission.Agents
				.Where(agent => IsLocalCivilianWitnessCandidate(agent, victim))
				.OrderBy(agent => agent.Position.DistanceSquared(victim.Position))
				.ThenBy(agent => agent.Index)
				.Take(SiegeLocalCivilianReactionProfile.MaxWitnessesPerIncident)
				.ToList();
			if (witnesses.Count == 0)
			{
				return;
			}
			int resistantEligibleCount = witnesses.Count(ShouldCivilianResistMassacre);
			int maxResisters = SiegeLocalCivilianReactionProfile.CalculateMaxResisters(witnesses.Count, resistantEligibleCount);
			int resistingCount = 0;
			int fleeingCount = 0;
			int speakerCount = 0;
			foreach (Agent witness in witnesses)
			{
				if (!TryReserveLocalCivilianWitnessReaction(witness, now))
				{
					continue;
				}
				bool witnessWillResist = resistingCount < maxResisters && ShouldCivilianResistMassacre(witness);
				if (witnessWillResist)
				{
				PrepareLocalHostileCivilian(witness, mission, player, SiegeLocalCivilianReactionProfile.WitnessResistSource);
					resistingCount++;
				}
				else
				{
					PrepareLocalFleeingCivilian(witness, mission, player, SiegeLocalCivilianReactionProfile.WitnessFleeSource);
					fleeingCount++;
				}
				if (SiegeLocalCivilianReactionProfile.ShouldAssignWitnessSpeech(speakerCount))
				{
					TryTriggerLocalCivilianWitnessSpeech(witness, targetName, victimDown, witnessWillResist);
					speakerCount++;
				}
			}
			if (fleeingCount > 0 || resistingCount > 0)
			{
				RecordInterventionMemory(SiegeLocalCivilianReactionProfile.WitnessMemoryTitle, SiegeLocalCivilianReactionProfile.BuildWitnessMemoryText(targetName, fleeingCount, resistingCount));
				Logger.Log("SiegeAiIntervention", "Triggered local civilian witness reactions. Victim=" + victim.Index + ", Down=" + victimDown + ", Fleeing=" + fleeingCount + ", Resisting=" + resistingCount + ", Speakers=" + speakerCount);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TriggerLocalCivilianWitnessReactions failed: " + ex.Message);
		}
	}

	private static bool IsLocalCivilianWitnessCandidate(Agent agent, Agent victim)
	{
		try
		{
			if (!IsMassacreTargetAgent(agent, includeHeroes: true))
			{
				return false;
			}
			if (victim != null && agent.Index == victim.Index)
			{
				return false;
			}
			if (LocalPlayerAttackVictimAgentIndexes.Contains(agent.Index) || LocalPlayerAttackDownAgentIndexes.Contains(agent.Index))
			{
				return false;
			}
			return SiegeLocalCivilianReactionProfile.IsInsideWitnessRadiusSquared(agent.Position.DistanceSquared(victim.Position));
		}
		catch
		{
			return false;
		}
	}

	private static bool TryReserveLocalCivilianWitnessReaction(Agent witness, float now)
	{
		if (witness == null || witness.Index < 0)
		{
			return false;
		}
		if (LastLocalCivilianWitnessReactionTimes.TryGetValue(witness.Index, out float last) && now - last < SiegeLocalCivilianReactionProfile.WitnessRepeatCooldownSeconds)
		{
			return false;
		}
		LastLocalCivilianWitnessReactionTimes[witness.Index] = now;
		return true;
	}

	private static bool TryTriggerLocalCivilianWitnessSpeech(Agent witness, string targetName, bool victimDown, bool witnessWillResist)
	{
		try
		{
			if (witness == null || !witness.IsActive())
			{
				return false;
			}
			string factText = SiegeLocalCivilianReactionProfile.BuildWitnessFact(targetName, victimDown, witnessWillResist, _activeSettlementName);
			return ShoutBehavior.TriggerImmediateSceneBehaviorReactionForExternal(factText, witness.Index, persistHeroPrivateHistory: true, suppressStare: true, postSpeechLeaveSeconds: -1f);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryTriggerLocalCivilianWitnessSpeech failed: " + ex.Message);
			return false;
		}
	}

	private static bool TryTriggerLocalSoldierWitnessInquiry(Mission mission, Agent victim, bool victimDown, string targetName)
	{
		try
		{
			if (!IsActiveInCurrentMission() || mission?.Agents == null || victim == null || victim.Index < 0)
			{
				return false;
			}
			if (!LocalSoldierWitnessInquiryVictimAgentIndexes.Add(victim.Index))
			{
				return false;
			}
			List<Agent> soldiers = mission.Agents
				.Where(agent => IsLocalSoldierWitnessCandidate(agent, victim))
				.OrderBy(agent => agent.Position.DistanceSquared(victim.Position))
				.ThenBy(agent => agent.Index)
				.ToList();
			if (soldiers.Count == 0)
			{
				LocalSoldierWitnessInquiryVictimAgentIndexes.Remove(victim.Index);
				return false;
			}
			foreach (Agent soldier in soldiers)
			{
				string soldierPersonaText = ResolveLocalSoldierWitnessPersonaText(soldier);
				bool soldierIsBloodthirsty = SiegeLocalCivilianReactionProfile.ResolveSoldierWitnessBloodthirstyFromPersona(soldierPersonaText);
				string factText = SiegeLocalCivilianReactionProfile.BuildSoldierWitnessInquiryFact(targetName, victimDown, _activeSettlementName, soldierIsBloodthirsty, soldierPersonaText);
				if (!ShoutBehavior.TriggerImmediateSceneBehaviorReactionForExternal(factText, soldier.Index, persistHeroPrivateHistory: true, suppressStare: false, postSpeechLeaveSeconds: -1f))
				{
					continue;
				}
				RecordInterventionMemory(SiegeLocalCivilianReactionProfile.SoldierWitnessMemoryTitle, SiegeLocalCivilianReactionProfile.BuildSoldierWitnessMemoryText(targetName, victimDown, soldier.Name?.ToString(), soldierIsBloodthirsty));
				Logger.Log("SiegeAiIntervention", "Triggered local soldier witness inquiry. Source=" + SiegeLocalCivilianReactionProfile.SoldierWitnessInquirySource + ", Soldier=" + soldier.Index + ", Victim=" + victim.Index + ", Down=" + victimDown + ", Bloodthirsty=" + soldierIsBloodthirsty);
				return true;
			}
			Agent fallbackSoldier = soldiers[0];
			string fallbackSoldierPersonaText = ResolveLocalSoldierWitnessPersonaText(fallbackSoldier);
			bool fallbackSoldierIsBloodthirsty = SiegeLocalCivilianReactionProfile.ResolveSoldierWitnessBloodthirstyFromPersona(fallbackSoldierPersonaText);
			InformationManager.DisplayMessage(new InformationMessage(
				SiegeLocalCivilianReactionProfile.BuildSoldierWitnessFallbackMessage(fallbackSoldier.Name?.ToString(), targetName, victimDown, fallbackSoldierIsBloodthirsty),
				Color.FromUint(SiegeLocalCivilianReactionProfile.SoldierWitnessFallbackMessageColor)));
			RecordInterventionMemory(SiegeLocalCivilianReactionProfile.SoldierWitnessMemoryTitle, SiegeLocalCivilianReactionProfile.BuildSoldierWitnessMemoryText(targetName, victimDown, fallbackSoldier.Name?.ToString(), fallbackSoldierIsBloodthirsty));
			Logger.Log("SiegeAiIntervention", "Displayed fallback local soldier witness inquiry. Source=" + SiegeLocalCivilianReactionProfile.SoldierWitnessInquirySource + ", Soldier=" + fallbackSoldier.Index + ", Victim=" + victim.Index + ", Down=" + victimDown + ", Bloodthirsty=" + fallbackSoldierIsBloodthirsty);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryTriggerLocalSoldierWitnessInquiry failed: " + ex.Message);
			return false;
		}
	}

	private static bool IsLocalSoldierWitnessCandidate(Agent agent, Agent victim)
	{
		try
		{
			if (!IsInterventionAlliedSoldierForExternal(agent, requireActive: true) || victim == null)
			{
				return false;
			}
			return SiegeLocalCivilianReactionProfile.IsInsideWitnessRadiusSquared(agent.Position.DistanceSquared(victim.Position));
		}
		catch
		{
			return false;
		}
	}

	private static string ResolveLocalSoldierWitnessPersonaText(Agent soldier)
	{
		try
		{
			CharacterObject character = soldier?.Character as CharacterObject;
			Hero hero = character?.HeroObject;
			if (hero != null)
			{
				MyBehavior.GetNpcPersonaForExternal(hero, out string personality, out string background);
				return JoinPersonaText(personality, background);
			}
			if (ShoutUtils.TryGetUnnamedNpcPersona(soldier, out string unnamedPersonality, out string unnamedBackground))
			{
				return JoinPersonaText(unnamedPersonality, unnamedBackground);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ResolveLocalSoldierWitnessPersonaText failed: " + ex.Message);
		}
		return "";
	}

	private static string JoinPersonaText(string personality, string background)
	{
		string p = (personality ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		string b = (background ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(p))
		{
			return b;
		}
		if (string.IsNullOrWhiteSpace(b))
		{
			return p;
		}
		return p + "；" + b;
	}

	private static void MaintainLocalPlayerAttackReactions(Mission mission)
	{
		try
		{
			if (mission?.Agents == null)
			{
				return;
			}
			if (_massacreStarted)
			{
				ClearLocalPlayerAttackState();
				return;
			}
			Agent main = Agent.Main ?? mission.MainAgent;
			bool localNativeFightActive = IsLocalNativeFightActive(mission);
			foreach (int agentIndex in LocalFleeingCivilianAgentIndexes.ToList())
			{
				Agent agent = mission.Agents.FirstOrDefault(a => a != null && a.Index == agentIndex);
				if (!IsMassacreTargetAgent(agent, includeHeroes: true))
				{
					LocalFleeingCivilianAgentIndexes.Remove(agentIndex);
					LocalPlayerAttackVictimAgentIndexes.Remove(agentIndex);
					continue;
				}
				ActivateNativeLocalFleeBehavior(agent, SiegeLocalCivilianReactionProfile.NativeFleeBridgeSource);
				if (!localNativeFightActive && main != null)
				{
					KeepCivilianHidingFromOccupation(agent, mission, main, force: false);
				}
			}
			foreach (int agentIndex in LocalHostileCivilianAgentIndexes.ToList())
			{
				Agent agent = mission.Agents.FirstOrDefault(a => a != null && a.Index == agentIndex);
				if (!IsMassacreTargetAgent(agent, includeHeroes: true))
				{
					LocalHostileCivilianAgentIndexes.Remove(agentIndex);
					LocalPlayerAttackVictimAgentIndexes.Remove(agentIndex);
					continue;
				}
				ForceAgentForMassacreFight(agent);
				agent.SetWatchState(Agent.WatchState.Alarmed);
				if (main != null && main.IsActive())
				{
					agent.SetLookAgent(main);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "MaintainLocalPlayerAttackReactions failed: " + ex.Message);
		}
	}

	private static void ClearLocalPlayerAttackState()
	{
		LocalPlayerAttackVictimAgentIndexes.Clear();
		LocalPlayerAttackDownAgentIndexes.Clear();
		LocalSoldierWitnessInquiryVictimAgentIndexes.Clear();
		LocalHostileCivilianAgentIndexes.Clear();
		LocalFleeingCivilianAgentIndexes.Clear();
		LastLocalCivilianWitnessReactionTimes.Clear();
		_localNativeFightStarted = false;
	}

	internal static bool TryHandleFriendlyHitOnAlliedSoldier(Agent affectedAgent, string source, float damagedHp = 0f)
	{
		if (!IsActiveInCurrentMission() || affectedAgent == null || !affectedAgent.IsHuman || affectedAgent == Agent.Main || !AlliedAgentIndexes.Contains(affectedAgent.Index))
		{
			return false;
		}
		RestoreAlliedSoldierFriendlyState(affectedAgent, damagedHp, source, forceFollow: !_massacreStarted);
		return true;
	}

	private static void RestoreAlliedSoldierFriendlyState(Agent soldier, float damagedHp, string source, bool forceFollow, bool clearTarget = true)
	{
		try
		{
			if (soldier == null || !AlliedAgentIndexes.Contains(soldier.Index))
			{
				return;
			}
			Mission mission = Mission.Current ?? soldier.Mission;
			Agent main = Agent.Main ?? mission?.MainAgent;
			if (mission == null || main == null)
			{
				return;
			}
			Team playerTeam = mission.PlayerTeam ?? main.Team;
			if (playerTeam != null && soldier.Team != playerTeam)
			{
				soldier.SetTeam(playerTeam, true);
			}
			if (playerTeam != null && soldier.Team != null && soldier.Team != playerTeam)
			{
				soldier.Team.SetIsEnemyOf(playerTeam, isEnemyOf: false);
				playerTeam.SetIsEnemyOf(soldier.Team, isEnemyOf: false);
			}
			if (damagedHp > 0f && soldier.Health > 0f && soldier.HealthLimit > 0f)
			{
				soldier.Health = MathF.Min(soldier.HealthLimit, soldier.Health + damagedHp + 1f);
			}
			soldier.SetMortalityState(Agent.MortalityState.Invulnerable);
			if (clearTarget)
			{
				soldier.InvalidateTargetAgent();
			}
			if (forceFollow)
			{
				soldier.SetWatchState(_plunderStarted ? Agent.WatchState.Alarmed : Agent.WatchState.Patrolling);
				AssignAgentToPlayerFormation(soldier, FormationClass.Infantry);
				TrySetPlayerFormationFollowOrder(FormationClass.Infantry, source);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "RestoreAlliedSoldierFriendlyState failed (" + source + "): " + ex.Message);
		}
	}

	private static bool TryApplyCompanionStyleFollow(Agent soldier, Agent main, string source)
	{
		return TryApplyAgentFollowTarget(soldier, main, source, lookAtTarget: false);
	}

	private static bool TryApplyAgentFollowTarget(Agent soldier, Agent target, string source, bool lookAtTarget)
	{
		try
		{
			if (soldier == null || target == null || !soldier.IsActive() || !target.IsActive())
			{
				return false;
			}
			CampaignAgentComponent component = soldier.GetComponent<CampaignAgentComponent>();
			if (component == null)
			{
				return false;
			}
			AgentNavigator navigator = component.AgentNavigator ?? component.CreateAgentNavigator();
			if (navigator == null)
			{
				return false;
			}
			AlarmedBehaviorGroup alarmedGroup = navigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
			if (alarmedGroup != null)
			{
				alarmedGroup.DisableScriptedBehavior();
				alarmedGroup.IsActive = false;
			}
			DailyBehaviorGroup dailyGroup = navigator.GetBehaviorGroup<DailyBehaviorGroup>() ?? navigator.AddBehaviorGroup<DailyBehaviorGroup>();
			if (dailyGroup == null)
			{
				return false;
			}
			FollowAgentBehavior followBehavior = dailyGroup.GetBehavior<FollowAgentBehavior>() ?? dailyGroup.AddBehavior<FollowAgentBehavior>();
			if (followBehavior == null)
			{
				return false;
			}
			ScriptBehavior scriptBehavior = dailyGroup.GetBehavior<ScriptBehavior>();
			if (scriptBehavior != null)
			{
				scriptBehavior.IsActive = false;
			}
			WalkingBehavior walkingBehavior = dailyGroup.GetBehavior<WalkingBehavior>();
			if (walkingBehavior != null)
			{
				walkingBehavior.IsActive = false;
			}
			dailyGroup.SetScriptedBehavior<FollowAgentBehavior>();
			dailyGroup.IsActive = true;
			followBehavior.IsActive = true;
			followBehavior.SetTargetAgent(target);
			try
			{
				FollowAgentBehaviorIdleDistanceField?.SetValue(followBehavior, 0f);
			}
			catch
			{
			}
			if (lookAtTarget)
			{
				soldier.SetLookAgent(target);
			}
			else
			{
				ClearAgentLookTarget(soldier);
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryApplyAgentFollowTarget failed (" + source + "): " + ex.Message);
			return false;
		}
	}

	private static void DisableCompanionStyleFollow(Agent soldier)
	{
		try
		{
			CampaignAgentComponent component = soldier?.GetComponent<CampaignAgentComponent>();
			AgentNavigator navigator = component?.AgentNavigator;
			DailyBehaviorGroup dailyGroup = navigator?.GetBehaviorGroup<DailyBehaviorGroup>();
			FollowAgentBehavior followBehavior = dailyGroup?.GetBehavior<FollowAgentBehavior>();
			if (followBehavior != null)
			{
				followBehavior.SetTargetAgent(null);
				followBehavior.IsActive = false;
			}
			dailyGroup?.DisableScriptedBehavior();
		}
		catch
		{
		}
	}

	private static void ClearAgentLookTarget(Agent agent)
	{
		try
		{
			if (agent != null && agent.IsActive())
			{
				agent.SetLookAgent(null);
			}
		}
		catch
		{
		}
	}

	private static void SetAgentLookTowardPoint(Agent agent, Vec3 point)
	{
		try
		{
			if (agent == null || !agent.IsActive())
			{
				return;
			}
			Vec3 lookDirection = point - agent.Position;
			lookDirection.z = 0f;
			if (lookDirection.LengthSquared < 0.01f)
			{
				ClearAgentLookTarget(agent);
				return;
			}
			lookDirection.Normalize();
			Vec3 lookPoint = agent.Position + lookDirection * 4f;
			agent.SetLookToPointOfInterest(lookPoint);
			agent.LookDirection = lookDirection;
		}
		catch
		{
		}
	}


	private static bool TrySetInterventionAgentTargetPosition(Agent agent, Vec3 target, string source, Agent.AIScriptedFrameFlags rescueFlags = Agent.AIScriptedFrameFlags.NeverSlowDown)
	{
		try
		{
			Mission mission = Mission.Current ?? agent?.Mission;
			if (agent == null || mission == null || mission.Scene == null || !agent.IsHuman || !agent.IsActive())
			{
				return false;
			}
			Vec3 resolvedTarget = target;
			try
			{
				resolvedTarget.z = mission.Scene.GetGroundHeightAtPosition(resolvedTarget);
			}
			catch
			{
			}
			if (ShouldUseTemporaryWallRescue(agent, mission, resolvedTarget, source))
			{
				try
				{
					WorldPosition scriptedPosition = new WorldPosition(mission.Scene, UIntPtr.Zero, resolvedTarget, false);
					agent.SetScriptedPosition(ref scriptedPosition, false, rescueFlags);
					return true;
				}
				catch (Exception ex)
				{
					Logger.Log("SiegeAiIntervention", "Wall rescue scripted movement failed (" + (source ?? SiegeAgentWallRescueProfile.Source) + "): " + ex.Message);
				}
			}
			agent.SetTargetPosition(resolvedTarget.AsVec2);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TrySetInterventionAgentTargetPosition failed (" + (source ?? "N/A") + "): " + ex.Message);
			return false;
		}
	}

	private static bool ShouldUseTemporaryWallRescue(Agent agent, Mission mission, Vec3 target, string source)
	{
		try
		{
			if (agent == null || mission == null || !agent.IsActive())
			{
				return false;
			}
			float distanceSq = agent.Position.DistanceSquared(target);
			float targetMinDistanceSq = SiegeAgentWallRescueProfile.TargetMinDistance * SiegeAgentWallRescueProfile.TargetMinDistance;
			if (distanceSq <= targetMinDistanceSq)
			{
				AgentWallRescueUntilTimes.Remove(agent.Index);
				LastAgentWallRescueProbePositions[agent.Index] = agent.Position;
				LastAgentWallRescueProbeTimes[agent.Index] = mission.CurrentTime;
				return false;
			}
			float now = mission.CurrentTime;
			if (AgentWallRescueUntilTimes.TryGetValue(agent.Index, out float activeUntil) && now < activeUntil)
			{
				return true;
			}
			if (!LastAgentWallRescueProbeTimes.TryGetValue(agent.Index, out float lastProbeTime))
			{
				LastAgentWallRescueProbePositions[agent.Index] = agent.Position;
				LastAgentWallRescueProbeTimes[agent.Index] = now;
				return false;
			}
			if (now - lastProbeTime < SiegeAgentWallRescueProfile.ProbeSeconds)
			{
				return false;
			}
			Vec3 lastProbePosition = LastAgentWallRescueProbePositions.TryGetValue(agent.Index, out Vec3 value) ? value : agent.Position;
			LastAgentWallRescueProbePositions[agent.Index] = agent.Position;
			LastAgentWallRescueProbeTimes[agent.Index] = now;
			float movedSq = agent.Position.DistanceSquared(lastProbePosition);
			float minMovedSq = SiegeAgentWallRescueProfile.MinMovedDistance * SiegeAgentWallRescueProfile.MinMovedDistance;
			if (movedSq > minMovedSq)
			{
				AgentWallRescueUntilTimes.Remove(agent.Index);
				return false;
			}
			AgentWallRescueUntilTimes[agent.Index] = now + SiegeAgentWallRescueProfile.RescueDurationSeconds;
			if (!LastAgentWallRescueLogTimes.TryGetValue(agent.Index, out float lastLog) || now - lastLog >= SiegeAgentWallRescueProfile.RescueDurationSeconds)
			{
				LastAgentWallRescueLogTimes[agent.Index] = now;
				Logger.Log("SiegeAiIntervention", "Enabled temporary wall rescue movement. Source=" + (source ?? SiegeAgentWallRescueProfile.Source) + ", Agent=" + agent.Index + ", Distance=" + MathF.Sqrt(distanceSq).ToString("0.0"));
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void MoveAlliedSoldierNearMainFallback(Agent soldier, Agent main)
	{
		try
		{
			Mission mission = Mission.Current ?? soldier?.Mission;
			if (mission == null || soldier == null || main == null || !soldier.IsActive())
			{
				return;
			}
			Vec3 forward = main.LookDirection;
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
			int slot = Math.Abs(soldier.Index % AutoSummonCount);
			float back = 2.5f + (slot / 4) * 0.75f;
			float side = ((slot % 2 == 0) ? 1f : -1f) * (0.9f + (slot % 4) * 0.55f);
			Vec3 position = main.Position - forward * back + right * side;
			try
			{
				if (mission.Scene != null)
				{
					position.z = mission.Scene.GetGroundHeightAtPosition(position);
				}
			}
			catch
			{
			}
			float distanceSq = soldier.Position.DistanceSquared(main.Position);
			if (distanceSq > 4.5f * 4.5f)
			{
				TrySetInterventionAgentTargetPosition(soldier, position, SiegeAgentWallRescueProfile.Source + ":allied_follow");
			}
		}
		catch
		{
		}
	}

	private static SiegeInterventionOutcome GetStandaloneOutcome()
	{
		if (_massacreStarted || _culturalRepopulationRequested)
		{
			return SiegeInterventionOutcome.Massacre;
		}
		if (_plunderStarted)
		{
			return SiegeInterventionOutcome.Plunder;
		}
		if (_activeMode == InterventionMode.MercyRelief)
		{
			return SiegeInterventionOutcome.MercyRelief;
		}
		if (_activeMode == InterventionMode.WaitingDecision)
		{
			return SiegeInterventionOutcome.WaitingDecision;
		}
		return SiegeInterventionOutcome.None;
	}

	private static bool HasPendingDevastateAftermath()
	{
		return _hasPendingAftermath && SiegeAftermathSelectionPolicy.IsDevastateOrWorse(ToStandaloneAftermathKind(_pendingAftermath));
	}

	private static bool HasDestructiveOutcomeLocked()
	{
		return SiegeInterventionActionRules.HasDestructiveOutcomeLocked(GetStandaloneOutcome(), _culturalRepopulationRequested, HasPendingDevastateAftermath());
	}

	private static bool TryBlockMercyTrackAfterDestructive(string actionName)
	{
		if (!HasDestructiveOutcomeLocked())
		{
			return false;
		}
		InformationManager.DisplayMessage(new InformationMessage(SiegeMercyTrackTransitionProfile.BuildBlockedAfterDestructiveMessage(actionName), Color.FromUint(SiegeMercyTrackTransitionProfile.BlockedAfterDestructiveMessageColor)));
		return true;
	}

	private static void StopReversiblePlunderForMercyTrack(string reason)
	{
		try
		{
			if (!_plunderStarted || _massacreStarted || _culturalRepopulationRequested)
			{
				return;
			}
			_plunderStarted = false;
			ActivePlunderInteractions.Clear();
			LastMassacreSoldierFollowOrderTimes.Clear();
			LastMassacreSoldierTargetOrderTimes.Clear();
			MassacreSoldierTargetAgentIndexes.Clear();
			MassacreSoldierTargetSlots.Clear();
			LastMassacreSoldierProbePositions.Clear();
			LastMassacreSoldierProbeTimes.Clear();
			InformationManager.DisplayMessage(new InformationMessage(SiegeMercyTrackTransitionProfile.ReversiblePlunderStoppedMessage, Color.FromUint(SiegeMercyTrackTransitionProfile.ReversiblePlunderStoppedMessageColor)));
			Logger.Log("SiegeAiIntervention", "Reversible plunder stopped by mercy track. Reason=" + (reason ?? "N/A"));
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "StopReversiblePlunderForMercyTrack failed: " + ex.Message);
		}
	}

	internal static bool ShouldCapturePlayerGiveForSharedCivilianReliefForExternal()
	{
		try
		{
			return IsActiveInCurrentMission() && !_massacreStarted && !_culturalRepopulationRequested;
		}
		catch
		{
			return false;
		}
	}

	internal static bool RecordSharedCivilianReliefTransferForExternal(int targetAgentIndex, int goldAmount, string itemId, int itemAmount, ItemObject item, int unitValue, string source)
	{
		try
		{
			if (!IsActiveInCurrentMission() || _massacreStarted || _culturalRepopulationRequested)
			{
				return false;
			}
			bool recorded = false;
			if (goldAmount > 0)
			{
				_sharedCivilianReliefGold += goldAmount;
				recorded = true;
			}
			if (!string.IsNullOrWhiteSpace(itemId) && itemAmount > 0)
			{
				string key = itemId.Trim();
				if (SharedCivilianReliefItems.TryGetValue(key, out int oldAmount))
				{
					SharedCivilianReliefItems[key] = oldAmount + itemAmount;
				}
				else
				{
					SharedCivilianReliefItems[key] = itemAmount;
				}
				if (item != null)
				{
					SharedCivilianReliefItemObjects[key] = item;
				}
				_sharedCivilianReliefItemTotal += itemAmount;
				if (item != null && item.IsFood)
				{
					_sharedCivilianReliefFoodUnits += itemAmount;
				}
				else
				{
					int value = unitValue > 0 ? unitValue : Math.Max(1, item?.Value ?? 1);
					_sharedCivilianReliefItemValue += (long)value * itemAmount;
				}
				recorded = true;
			}
			if (recorded)
			{
				_sharedCivilianReliefReturned = false;
				string itemText = SiegeSharedReliefPoolFormatter.BuildItemAmountText(itemAmount, item?.Name?.ToString() ?? itemId);
				string goldText = SiegeSharedReliefPoolFormatter.BuildGoldAmountText(goldAmount);
				string joined = SiegeSharedReliefPoolFormatter.JoinAmountParts(goldText, itemText);
				InformationManager.DisplayMessage(new InformationMessage(SiegeSharedReliefPoolFormatter.BuildCapturedTransferMessage(joined), Color.FromUint(SiegeSharedReliefPoolFormatter.CapturedTransferMessageColor)));
				Logger.Log("SiegeAiIntervention", "Recorded shared civilian relief transfer. Source=" + (source ?? "N/A") + ", TargetAgent=" + targetAgentIndex + ", Gold=" + goldAmount + ", Item=" + (itemId ?? "") + ", Amount=" + itemAmount + ", Pool=" + DescribeSharedCivilianReliefPoolForContext());
			}
			return recorded;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "RecordSharedCivilianReliefTransferForExternal failed (" + (source ?? "N/A") + "): " + ex.Message);
			return false;
		}
	}

	private static string DescribeSharedCivilianReliefPoolForContext()
	{
		try
		{
			return SiegeSharedReliefPoolFormatter.DescribeForContext(BuildSharedCivilianReliefPoolFacts());
		}
		catch
		{
			return SiegeSharedReliefPoolFormatter.UnavailableStatsDescription;
		}
	}

	private static bool HasSharedCivilianReliefPool()
	{
		return SiegeSharedReliefPoolFormatter.HasAnyMaterial(BuildSharedCivilianReliefPoolFacts());
	}

	private static SiegeSharedReliefPoolFacts BuildSharedCivilianReliefPoolFacts()
	{
		return new SiegeSharedReliefPoolFacts(
			_sharedCivilianReliefGold,
			_sharedCivilianReliefFoodUnits,
			_sharedCivilianReliefItemTotal,
			_sharedCivilianReliefItemValue);
	}

	private static bool IsCivilianReliefConversationTarget(int targetAgentIndex, CharacterObject targetCharacter)
	{
		try
		{
			if (targetAgentIndex >= 0)
			{
				Agent agent = TryGetAgent(targetAgentIndex);
				if (agent != null)
				{
					return !AlliedAgentIndexes.Contains(agent.Index) && IsEligibleCivilianAgent(agent, includeHeroes: true, requireActive: false);
				}
			}
			return IsCivilianForIntervention(targetCharacter);
		}
		catch
		{
			return false;
		}
	}

	private static ItemObject ResolveSharedCivilianReliefItem(string itemId)
	{
		try
		{
			string key = (itemId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(key))
			{
				return null;
			}
			if (SharedCivilianReliefItemObjects.TryGetValue(key, out ItemObject cached) && cached != null)
			{
				return cached;
			}
			return Game.Current?.ObjectManager?.GetObject<ItemObject>(key);
		}
		catch
		{
			return null;
		}
	}

	private static bool ReturnSharedCivilianReliefPoolToPlayerForNegativeOutcome(string reason)
	{
		try
		{
			if (!HasSharedCivilianReliefPool() || _sharedCivilianReliefReturned)
			{
				return false;
			}
			int returnedGold = Math.Max(0, _sharedCivilianReliefGold);
			int returnedItems = 0;
			List<string> returnedParts = new List<string>();
			if (returnedGold > 0)
			{
				AwardGoldToPlayer(returnedGold, SiegeSharedReliefPoolFormatter.BuildReturnedGoldSource(reason));
				returnedParts.Add(SiegeSharedReliefPoolFormatter.BuildGoldAmountText(returnedGold));
			}
			MobileParty mainParty = MobileParty.MainParty ?? Hero.MainHero?.PartyBelongedTo;
			ItemRoster itemRoster = mainParty?.ItemRoster;
			foreach (KeyValuePair<string, int> pair in SharedCivilianReliefItems.ToList())
			{
				int amount = Math.Max(0, pair.Value);
				if (amount <= 0 || itemRoster == null)
				{
					continue;
				}
				ItemObject item = ResolveSharedCivilianReliefItem(pair.Key);
				if (item == null)
				{
					Logger.Log("SiegeAiIntervention", "Unable to resolve shared relief item for refund. ItemId=" + (pair.Key ?? ""));
					continue;
				}
				itemRoster.AddToCounts(item, amount);
				returnedItems += amount;
				returnedParts.Add(SiegeSharedReliefPoolFormatter.BuildItemAmountText(amount, item.Name?.ToString() ?? pair.Key));
			}
			if (returnedGold <= 0 && returnedItems <= 0)
			{
				return false;
			}
			_sharedCivilianReliefReturned = true;
			_sharedCivilianReliefGold = 0;
			_sharedCivilianReliefFoodUnits = 0;
			_sharedCivilianReliefItemTotal = 0;
			_sharedCivilianReliefItemValue = 0L;
			_appliedSharedCivilianReliefGold = 0;
			_appliedSharedCivilianReliefFoodUnits = 0;
			_appliedSharedCivilianReliefItemValue = 0L;
			SharedCivilianReliefItems.Clear();
			SharedCivilianReliefItemObjects.Clear();
			string summary = SiegeSharedReliefPoolFormatter.JoinAmountParts(returnedParts.ToArray());
			InformationManager.DisplayMessage(new InformationMessage(SiegeSharedReliefPoolFormatter.BuildReturnedToPlayerMessage(summary), Color.FromUint(SiegeSharedReliefPoolFormatter.ReturnedToPlayerMessageColor)));
			RecordInterventionMemory(SiegeSharedReliefPoolFormatter.ReturnedToPlayerMemoryTitle, SiegeSharedReliefPoolFormatter.BuildReturnedToPlayerMemoryText(summary));
			Logger.Log("SiegeAiIntervention", "Returned shared civilian relief pool to player due to negative outcome. Reason=" + (reason ?? "N/A") + ", Summary=" + summary);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ReturnSharedCivilianReliefPoolToPlayerForNegativeOutcome failed (" + (reason ?? "N/A") + "): " + ex.Message);
			return false;
		}
	}

	private static bool ApplySharedCivilianReliefPoolEffects(Settlement settlement, string reason)
	{
		try
		{
			SiegeSharedReliefPoolEffectDeltas reliefEffect = SiegeSharedReliefPoolEffectCalculator.Calculate(
				new SiegeSharedReliefPoolFacts(_sharedCivilianReliefGold, _sharedCivilianReliefFoodUnits, _sharedCivilianReliefItemTotal, _sharedCivilianReliefItemValue),
				new SiegeSharedReliefPoolFacts(_appliedSharedCivilianReliefGold, _appliedSharedCivilianReliefFoodUnits, 0, _appliedSharedCivilianReliefItemValue));
			if (!reliefEffect.HasNewMaterial)
			{
				return false;
			}
			_appliedSharedCivilianReliefGold = _sharedCivilianReliefGold;
			_appliedSharedCivilianReliefFoodUnits = _sharedCivilianReliefFoodUnits;
			_appliedSharedCivilianReliefItemValue = _sharedCivilianReliefItemValue;
			if (reliefEffect.NewFoodUnits > 0)
			{
				try
				{
					if (settlement?.Town != null)
					{
						settlement.Town.FoodStocks = Math.Min(settlement.Town.FoodStocks + reliefEffect.NewFoodUnits, settlement.Town.FoodStocksUpperLimit());
					}
				}
				catch
				{
				}
			}
			int adjustedPublicTrustDelta = ReducePositiveIntDeltaForRegionalConflict(reliefEffect.PublicTrustDelta, "shared_relief_public_trust");
			float adjustedLoyaltyDelta = ReducePositiveFloatDeltaForRegionalConflict(reliefEffect.LoyaltyDelta, "shared_relief_loyalty");
			float adjustedSecurityDelta = ReducePositiveFloatDeltaForRegionalConflict(reliefEffect.SecurityDelta, "shared_relief_security");
			if (adjustedPublicTrustDelta != 0 || Math.Abs(adjustedLoyaltyDelta) > 0.001f || Math.Abs(adjustedSecurityDelta) > 0.001f)
			{
				AdjustSettlementAfterRelief(settlement, adjustedPublicTrustDelta, adjustedLoyaltyDelta, adjustedSecurityDelta);
			}
			InformationManager.DisplayMessage(new InformationMessage(SiegeSharedReliefPoolFormatter.BuildAppliedEffectMessage(DescribeSharedCivilianReliefPoolForContext()), Color.FromUint(SiegeSharedReliefPoolFormatter.AppliedEffectMessageColor)));
			Logger.Log("SiegeAiIntervention", "Applied shared civilian relief pool effects. Reason=" + (reason ?? "N/A") + ", NewGold=" + reliefEffect.NewGold + ", NewFood=" + reliefEffect.NewFoodUnits + ", NewMaterialValue=" + reliefEffect.NewMaterialValue + ", PublicTrustDelta=" + adjustedPublicTrustDelta + ", LoyaltyDelta=" + adjustedLoyaltyDelta + ", SecurityDelta=" + adjustedSecurityDelta + ", RegionalConflictIncidents=" + _regionalConflictIncidentCount);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplySharedCivilianReliefPoolEffects failed (" + (reason ?? "N/A") + "): " + ex.Message);
			return false;
		}
	}

	private static bool ApplySoldierMaterialReliefChoice(int targetAgentIndex, string triggerSource, string triggerDetail)
	{
		try
		{
			if (targetAgentIndex < 0 || !AlliedAgentIndexes.Contains(targetAgentIndex))
			{
				InformationManager.DisplayMessage(new InformationMessage(SiegeReliefChoiceProfile.SoldierMaterialReliefTargetMessage, Color.FromUint(SiegeReliefChoiceProfile.ValidationMessageColor)));
				return false;
			}
			if (!HasSharedCivilianReliefPool())
			{
				InformationManager.DisplayMessage(new InformationMessage(SiegeReliefChoiceProfile.SoldierMaterialReliefMissingPoolMessage, Color.FromUint(SiegeReliefChoiceProfile.ValidationMessageColor)));
				return false;
			}
			return ApplyReliefChoiceCore(triggerSource, triggerDetail, requireSharedMaterial: true, civilianVerbalOnly: false);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplySoldierMaterialReliefChoice failed: " + ex.Message);
			return false;
		}
	}

	private static bool ApplyCivilianVerbalReliefChoice(string triggerSource, string triggerDetail)
	{
		return ApplyReliefChoiceCore(triggerSource, triggerDetail, requireSharedMaterial: false, civilianVerbalOnly: !HasSharedCivilianReliefPool());
	}

	private static bool ApplyReliefChoiceCore(string triggerSource, string triggerDetail, bool requireSharedMaterial, bool civilianVerbalOnly)
	{
		try
		{
			if (TryBlockMercyTrackAfterDestructive(SiegeReliefChoiceProfile.BlockedAfterDestructiveActionName))
			{
				return false;
			}
			bool hasSharedPool = HasSharedCivilianReliefPool();
			if (requireSharedMaterial && !hasSharedPool)
			{
				InformationManager.DisplayMessage(new InformationMessage(SiegeReliefChoiceProfile.RequiredSharedMaterialMissingMessage, Color.FromUint(SiegeReliefChoiceProfile.ValidationMessageColor)));
				return false;
			}
			SiegeReliefChoiceProfile reliefProfile = SiegeReliefChoiceProfile.Build(hasSharedPool, civilianVerbalOnly, DescribeSharedCivilianReliefPoolForContext());
			StopReversiblePlunderForMercyTrack(SiegeReliefChoiceProfile.StopReversiblePlunderReason);
			if (_reliefChoiceApplied)
			{
				if (reliefProfile.HasSharedPool && !string.IsNullOrWhiteSpace(reliefProfile.RepeatSharedPoolEffectReason))
				{
					ApplySharedCivilianReliefPoolEffects(ResolveCurrentSettlement(), reliefProfile.RepeatSharedPoolEffectReason);
				}
				RecordInterventionMemory(reliefProfile.RepeatMemoryTitle, reliefProfile.RepeatMemoryText);
				return true;
			}
			_reliefChoiceApplied = true;
			_activeMode = InterventionMode.MercyRelief;
			MarkPendingAftermath(SiegeAftermathAction.SiegeAftermath.ShowMercy, triggerSource, triggerDetail);
			MaybeTriggerSoldierAppeasementNeed(reliefProfile.SoldierAppeasementReason);
			Settlement settlement = ResolveCurrentSettlement();
			AdjustSettlementAfterRelief(
				settlement,
				ReducePositiveIntDeltaForRegionalConflict(reliefProfile.PublicTrustDelta, "relief_public_trust"),
				ReducePositiveFloatDeltaForRegionalConflict(reliefProfile.LoyaltyDelta, "relief_loyalty"),
				ReducePositiveFloatDeltaForRegionalConflict(reliefProfile.SecurityDelta, "relief_security"));
			QueuePositiveNotableRelationForFinalAftermath(ReducePositiveIntDeltaForRegionalConflict(reliefProfile.NotableRelationDelta, "relief_notable_relation"), includeBoundVillages: false, SiegeSettlementEffectProfile.ReliefNotableRelationReason);
			QueuePositiveNotableTrustForFinalAftermath(ReducePositiveIntDeltaForRegionalConflict(reliefProfile.NotableTrustDelta, "relief_notable_trust"), includeBoundVillages: false, SiegeSettlementEffectProfile.ReliefNotableTrustReason);
			if (reliefProfile.HasSharedPool && !string.IsNullOrWhiteSpace(reliefProfile.SharedPoolEffectReason))
			{
				ApplySharedCivilianReliefPoolEffects(settlement, reliefProfile.SharedPoolEffectReason);
			}
			ShowOutcomeMessageOnce(reliefProfile.MessageKey, reliefProfile.MessageText, reliefProfile.MessageColor);
			RecordInterventionMemory(reliefProfile.MemoryTitle, reliefProfile.MemoryText);
			Logger.Log("SiegeAiIntervention", "Applied relief choice. Settlement=" + (settlement?.StringId ?? "N/A") + ", QueuedRelationDelta=" + reliefProfile.NotableRelationDelta);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyReliefChoiceCore failed: " + ex.Message);
			return false;
		}
	}

	private static bool ApplyInspirationChoice(string triggerSource, string triggerDetail)
	{
		try
		{
			if (TryBlockMercyTrackAfterDestructive(SiegeCivicChoiceProfile.InspirationBlockedAfterDestructiveActionName))
			{
				return false;
			}
			SiegeCivicChoiceProfile civicProfile = SiegeCivicChoiceProfile.BuildInspiration();
			StopReversiblePlunderForMercyTrack(civicProfile.StopReversiblePlunderReason);
			if (_inspirationLevelApplied >= 1)
			{
				ApplySharedCivilianReliefPoolEffects(ResolveCurrentSettlement(), civicProfile.RepeatSharedPoolEffectReason);
				RecordInterventionMemory(civicProfile.RepeatMemoryTitle, civicProfile.RepeatMemoryText);
				return true;
			}
			Settlement settlement = ResolveCurrentSettlement();
			_activeMode = InterventionMode.MercyRelief;
			MarkPendingAftermath(SiegeAftermathAction.SiegeAftermath.ShowMercy, triggerSource, triggerDetail);
			MaybeTriggerSoldierAppeasementNeed(civicProfile.SoldierAppeasementReason);
			ApplyCivicChoiceSettlementEffects(settlement, civicProfile, SiegeSettlementEffectProfile.InspirationSettlementPublicTrustReason, SiegeSettlementEffectProfile.InspirationBoundVillagePublicTrustReason);
			ApplySharedCivilianReliefPoolEffects(settlement, civicProfile.SharedPoolEffectReason);
			BeginCivicPositiveBuff(settlement, civicProfile);
			_inspirationLevelApplied = civicProfile.ResultingInspirationLevel;
			QueuePositiveNotableRelationForFinalAftermath(ReducePositiveIntDeltaForRegionalConflict(civicProfile.NotableRelationDelta, "inspiration_notable_relation"), includeBoundVillages: true, SiegeSettlementEffectProfile.InspirationNotableRelationReason);
			QueuePositiveNotableTrustForFinalAftermath(ReducePositiveIntDeltaForRegionalConflict(civicProfile.NotableTrustDelta, "inspiration_notable_trust"), includeBoundVillages: true, SiegeSettlementEffectProfile.InspirationNotableTrustReason);
			int powerAdjusted = 0;
			GatherCiviliansForSpeech(civicProfile.GatherSource);
			ShowOutcomeMessageOnce(civicProfile.MessageKey, civicProfile.MessageText, civicProfile.MessageColor);
			RecordInterventionMemory(civicProfile.MemoryTitle, civicProfile.MemoryText);
			Logger.Log("SiegeAiIntervention", "Applied inspiration choice. Settlement=" + (settlement?.StringId ?? "N/A") + ", QueuedRelationDelta=" + civicProfile.NotableRelationDelta + ", PowerAdjusted=" + powerAdjusted);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyInspirationChoice failed: " + ex.Message);
			return false;
		}
	}

	private static bool ApplyRallyOathChoice(string triggerSource, string triggerDetail)
	{
		try
		{
			if (TryBlockMercyTrackAfterDestructive(SiegeCivicChoiceProfile.RallyOathBlockedAfterDestructiveActionName))
			{
				return false;
			}
			SiegeCivicChoiceProfile civicProfile = SiegeCivicChoiceProfile.BuildRallyOath(_inspirationLevelApplied);
			StopReversiblePlunderForMercyTrack(civicProfile.StopReversiblePlunderReason);
			if (_inspirationLevelApplied >= 2)
			{
				ApplySharedCivilianReliefPoolEffects(ResolveCurrentSettlement(), civicProfile.RepeatSharedPoolEffectReason);
				RecordInterventionMemory(civicProfile.RepeatMemoryTitle, civicProfile.RepeatMemoryText);
				return true;
			}
			Settlement settlement = ResolveCurrentSettlement();
			_activeMode = InterventionMode.MercyRelief;
			MarkPendingAftermath(SiegeAftermathAction.SiegeAftermath.ShowMercy, triggerSource, triggerDetail);
			MaybeTriggerSoldierAppeasementNeed(civicProfile.SoldierAppeasementReason);
			ApplyCivicChoiceSettlementEffects(settlement, civicProfile, SiegeSettlementEffectProfile.RallyOathSettlementPublicTrustReason, SiegeSettlementEffectProfile.RallyOathBoundVillagePublicTrustReason);
			ApplySharedCivilianReliefPoolEffects(settlement, civicProfile.SharedPoolEffectReason);
			BeginCivicPositiveBuff(settlement, civicProfile);
			_inspirationLevelApplied = civicProfile.ResultingInspirationLevel;
			QueuePositiveNotableRelationForFinalAftermath(ReducePositiveIntDeltaForRegionalConflict(civicProfile.NotableRelationDelta, "rally_oath_notable_relation"), includeBoundVillages: true, SiegeSettlementEffectProfile.RallyOathNotableRelationReason);
			QueuePositiveNotableTrustForFinalAftermath(ReducePositiveIntDeltaForRegionalConflict(civicProfile.NotableTrustDelta, "rally_oath_notable_trust"), includeBoundVillages: true, SiegeSettlementEffectProfile.RallyOathNotableTrustReason);
			int powerAdjusted = 0;
			GatherCiviliansForSpeech(civicProfile.GatherSource);
			ShowOutcomeMessageOnce(civicProfile.MessageKey, civicProfile.MessageText, civicProfile.MessageColor);
			RecordInterventionMemory(civicProfile.MemoryTitle, civicProfile.MemoryText);
			Logger.Log("SiegeAiIntervention", "Applied rally oath choice. Settlement=" + (settlement?.StringId ?? "N/A") + ", QueuedRelationDelta=" + civicProfile.NotableRelationDelta + ", QueuedTrustDelta=" + civicProfile.NotableTrustDelta + ", PowerAdjusted=" + powerAdjusted);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyRallyOathChoice failed: " + ex.Message);
			return false;
		}
	}

	private static bool ApplyMercyChoice(string triggerSource, string triggerDetail)
	{
		try
		{
			if (TryBlockMercyTrackAfterDestructive(SiegeMercyChoiceProfile.BlockedAfterDestructiveActionName))
			{
				return false;
			}
			SiegeMercyChoiceProfile mercyProfile = new SiegeMercyChoiceProfile();
			StopReversiblePlunderForMercyTrack(mercyProfile.StopPlunderReason);
			_activeMode = InterventionMode.MercyRelief;
			MarkPendingAftermath(SiegeAftermathAction.SiegeAftermath.ShowMercy, triggerSource, triggerDetail);
			MaybeTriggerSoldierAppeasementNeed(mercyProfile.SoldierAppeasementReason);
			ApplySharedCivilianReliefPoolEffects(ResolveCurrentSettlement(), mercyProfile.SharedPoolEffectReason);
			ShowOutcomeMessageOnce(mercyProfile.MessageKey, mercyProfile.MessageText, mercyProfile.MessageColor);
			RecordInterventionMemory(mercyProfile.MemoryTitle, mercyProfile.MemoryText);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyMercyChoice failed: " + ex.Message);
			return false;
		}
	}

	private static void AwardGoldToPlayer(int amount, string source)
	{
		if (amount <= 0 || Hero.MainHero == null)
		{
			return;
		}
		try
		{
			int before = Hero.MainHero.Gold;
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, amount, disableNotification: true);
			if (Hero.MainHero.Gold < before + amount)
			{
				Hero.MainHero.ChangeHeroGold(before + amount - Hero.MainHero.Gold);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "AwardGoldToPlayer failed (" + source + "): " + ex.Message);
			try
			{
				Hero.MainHero.ChangeHeroGold(amount);
			}
			catch
			{
			}
		}
	}

	private static bool StartPlunder(string triggerSource, string triggerDetail)
	{
		if (_massacreStarted)
		{
			return false;
		}
		SiegeDestructiveChoiceProfile plunderProfile = SiegeDestructiveChoiceProfile.BuildPlunder();
		ClearCivicPositiveBuffForSettlement(ResolveCurrentSettlement());
		_activeMode = InterventionMode.Plunder;
		if (!_plunderStarted)
		{
			_plunderStarted = true;
			MarkPendingAftermath(ToNativeAftermathKind(plunderProfile.AftermathKind), triggerSource, triggerDetail);
			EnsureAlliedTroopsSummoned();
			MaintainCivilianAssembly(Mission.Current, plunderProfile.AssemblySource, force: false);
			ShowOutcomeMessageOnce(plunderProfile.MessageKey, plunderProfile.MessageText, plunderProfile.MessageColor);
			RecordInterventionMemory(plunderProfile.MemoryTitle, plunderProfile.FirstMemoryText);
			return true;
		}
		MarkPendingAftermath(ToNativeAftermathKind(plunderProfile.AftermathKind), triggerSource, triggerDetail);
		RecordInterventionMemory(plunderProfile.MemoryTitle, plunderProfile.RepeatMemoryText);
		return false;
	}

	private static bool ApplyCivilianRobberyChoice(int targetAgentIndex, CharacterObject targetCharacter, Hero targetHero, string actionText)
	{
		if (_massacreStarted || _culturalRepopulationRequested)
		{
			return false;
		}
		Agent targetAgent = TryGetAgent(targetAgentIndex);
		CharacterObject agentCharacter = targetAgent?.Character as CharacterObject;
		CharacterObject resolvedCharacter = targetCharacter ?? agentCharacter ?? targetHero?.CharacterObject;
		Hero resolvedHero = targetHero ?? resolvedCharacter?.HeroObject;
		if (targetAgent == null || IsRuntimeAlliedSoldierAgent(targetAgent, resolvedCharacter, resolvedHero))
		{
			return false;
		}
		if (!IsEligibleCivilianAgent(targetAgent, includeHeroes: true))
		{
			return false;
		}
		string targetKey = BuildTargetKey(targetAgent);
		if (!CivilianRobberyTargets.Add(targetKey))
		{
			return false;
		}
		bool lootedGold = TryLootCivilianRobberyGold(targetAgent, targetKey);
		bool wantsGoods = RobberyRequestsGoods(actionText);
		bool lootedGoods = wantsGoods && TryLootCivilianRobberyMarketInventory();
		if (!lootedGold && !lootedGoods)
		{
			CivilianRobberyTargets.Remove(targetKey);
			return false;
		}
		_civilianRobberyTargetsLooted++;
		ApplyCivilianRobberyPenaltyIfNeeded();
		RecordInterventionMemory(
			SiegeCivilianRobberyProfile.MemoryTitle,
			lootedGoods ? SiegeCivilianRobberyProfile.GoodsMemoryText : SiegeCivilianRobberyProfile.GoldMemoryText);
		Logger.Log("SiegeAiIntervention", $"Applied civilian robbery. Target={targetKey}, Gold={lootedGold}, Goods={lootedGoods}, Count={_civilianRobberyTargetsLooted}, PenaltyLevel={_civilianRobberyPenaltyLevelApplied}");
		return true;
	}

	private static bool TryLootCivilianRobberyGold(Agent agent, string targetKey)
	{
		if (!IsEligibleCivilianAgent(agent, includeHeroes: true))
		{
			return false;
		}
		if (!LootedTargets.Add(targetKey))
		{
			return false;
		}
		CharacterObject character = agent.Character as CharacterObject;
		Hero hero = character?.HeroObject;
		int amount = 0;
		if (hero != null && hero != Hero.MainHero)
		{
			amount = hero.Gold > 0
				? RandomPercent(hero.Gold, SiegeCivilianRobberyProfile.HeroGoldMinRatio, SiegeCivilianRobberyProfile.HeroGoldMaxRatio)
				: MBRandom.RandomInt(SiegeCivilianRobberyProfile.HeroFallbackGoldMin, SiegeCivilianRobberyProfile.HeroFallbackGoldMax + 1);
			if (hero.Gold > 0)
			{
				amount = Math.Min(amount, hero.Gold);
				if (amount > 0)
				{
					GiveGoldAction.ApplyBetweenCharacters(hero, Hero.MainHero, amount, disableNotification: true);
				}
			}
			else if (amount > 0)
			{
				AwardGoldToPlayer(amount, SiegeLootAccountingProfile.CivilianHeroFallbackGoldSource);
			}
		}
		else if (IsMerchantRobberyCharacter(character) && TryLootSettlementMarketGoldPortion(SiegeCivilianRobberyProfile.MerchantGoldMinRatio, SiegeCivilianRobberyProfile.MerchantGoldMaxRatio, out amount))
		{
			// Amount already moved from settlement market gold into the player's purse.
		}
		else
		{
			amount = MBRandom.RandomInt(SiegeCivilianRobberyProfile.CommonerMinGold, SiegeCivilianRobberyProfile.CommonerMaxGold + 1);
			AwardGoldToPlayer(amount, SiegeLootAccountingProfile.CivilianFlatGoldSource);
		}
		if (amount <= 0)
		{
			LootedTargets.Remove(targetKey);
			return false;
		}
		_lastCivilianGoldLoot += amount;
		_lastCivilianTargetsLooted++;
		InformationManager.DisplayMessage(new InformationMessage(SiegeLootAccountingProfile.BuildCivilianLootMessage(null, agent.Name?.ToString(), amount), Color.FromUint(SiegeLootAccountingProfile.LootMessageColor)));
		return true;
	}

	private static bool TryLootSettlementMarketGoldPortion(float minRatio, float maxRatio, out int amount)
	{
		amount = 0;
		try
		{
			Settlement settlement = ResolveCurrentSettlement();
			Town town = settlement?.Town;
			if (town == null || town.Gold <= 0)
			{
				return false;
			}
			amount = Math.Max(1, Math.Min(town.Gold, RandomPercent(town.Gold, minRatio, maxRatio)));
			town.ChangeGold(-amount);
			AwardGoldToPlayer(amount, SiegeLootAccountingProfile.MarketGoldSource);
			_lastMarketGoldLoot += amount;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryLootSettlementMarketGoldPortion failed: " + ex.Message);
			amount = 0;
			return false;
		}
	}

	private static bool TryLootCivilianRobberyMarketInventory()
	{
		int itemTotalBefore = _lastLootItemTotal;
		LootSettlementMarketInventory(SiegeCivilianRobberyProfile.MarketInventoryMinRatio, SiegeCivilianRobberyProfile.MarketInventoryMaxRatio, SiegeCivilianRobberyProfile.MarketInventoryLootReason);
		return _lastLootItemTotal > itemTotalBefore;
	}

	private static bool RobberyRequestsGoods(string text)
	{
		return ContainsAny(text, "物资", "货物", "货品", "粮食", "粮草", "库存", "商品", "军需", "辎重", "给货", "交货", "交出货", "交出物", "拿货");
	}

	private static bool IsMerchantRobberyCharacter(CharacterObject character)
	{
		if (character == null)
		{
			return false;
		}
		switch (character.Occupation)
		{
		case Occupation.GoodsTrader:
		case Occupation.Merchant:
		case Occupation.Weaponsmith:
		case Occupation.Armorer:
		case Occupation.HorseTrader:
		case Occupation.ShopWorker:
		case Occupation.Blacksmith:
		case Occupation.Tavernkeeper:
		case Occupation.RansomBroker:
			return true;
		default:
			return false;
		}
	}

	private static void ApplyCivilianRobberyPenaltyIfNeeded()
	{
		try
		{
			Settlement settlement = ResolveCurrentSettlement();
			if (settlement == null)
			{
				return;
			}
			if (_civilianRobberyPenaltyLevelApplied < 1)
			{
				ApplyCivilianRobberyPenaltyDelta(
					settlement,
					SiegeCivilianRobberyProfile.LocalPenaltyKey,
					SiegeCivilianRobberyProfile.LocalSettlementPublicTrustDelta,
					SiegeCivilianRobberyProfile.LocalSettlementPublicTrustReason,
					SiegeCivilianRobberyProfile.LocalBoundVillagePublicTrustDelta,
					SiegeCivilianRobberyProfile.LocalBoundVillagePublicTrustReason,
					SiegeCivilianRobberyProfile.LocalNotableRelationDelta,
					SiegeCivilianRobberyProfile.LocalNotableRelationReason);
				_civilianRobberyPenaltyLevelApplied = 1;
			}
			if (_civilianRobberyPenaltyLevelApplied < 2 && SiegeCivilianRobberyProfile.ShouldEscalateToFullPillagePenalty(_civilianRobberyTargetsLooted))
			{
				ApplyCivilianRobberyPenaltyDelta(
					settlement,
					SiegeCivilianRobberyProfile.FullPillagePenaltyKey,
					SiegeCivilianRobberyProfile.EscalatedSettlementPublicTrustDelta,
					SiegeCivilianRobberyProfile.EscalatedSettlementPublicTrustReason,
					SiegeCivilianRobberyProfile.EscalatedBoundVillagePublicTrustDelta,
					SiegeCivilianRobberyProfile.EscalatedBoundVillagePublicTrustReason,
					SiegeCivilianRobberyProfile.EscalatedNotableRelationDelta,
					SiegeCivilianRobberyProfile.EscalatedNotableRelationReason);
				_civilianRobberyPenaltyLevelApplied = 2;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyCivilianRobberyPenaltyIfNeeded failed: " + ex.Message);
		}
	}

	private static void ApplyCivilianRobberyPenaltyDelta(
		Settlement settlement,
		string key,
		int settlementTrustDelta,
		string settlementTrustReason,
		int boundVillageTrustDelta,
		string boundVillageTrustReason,
		int notableRelationDelta,
		string notableRelationReason)
	{
		AdjustSettlementPublicTrustOnly(settlement, settlementTrustDelta, settlementTrustReason);
		int villageTrustAdjusted = AdjustBoundVillagePublicTrust(settlement, boundVillageTrustDelta, boundVillageTrustReason);
		int notableRelationAdjusted = AdjustSettlementAndBoundVillageNotableRelations(settlement, notableRelationDelta, notableRelationReason);
		Logger.Log("SiegeAiIntervention", $"Applied civilian robbery settlement penalty. Key={key}, Settlement={settlement?.StringId ?? "N/A"}, SettlementTrust={settlementTrustDelta}, VillageTrust={boundVillageTrustDelta}x{villageTrustAdjusted}, NotableRelation={notableRelationDelta}x{notableRelationAdjusted}");
	}

	private static bool StartMassacre(string triggerSource, string triggerDetail)
	{
		SiegeDestructiveChoiceProfile massacreProfile = SiegeDestructiveChoiceProfile.BuildMassacre();
		ClearCivicPositiveBuffForSettlement(ResolveCurrentSettlement());
		_activeMode = InterventionMode.Massacre;
		_civilianGatherPropagationActive = false;
		ActiveCivilianGatherInteractions.Clear();
		TryEndLocalNativeFight(Mission.Current, triggerSource);
		ClearLocalPlayerAttackState();
		bool first = !_massacreStarted;
		_massacreStarted = true;
		if (first)
		{
			_lastMassacreRealKillMissionTime = Mission.Current?.CurrentTime ?? 0f;
			VictoryCheerAgentIndexes.Clear();
		}
		MarkPendingAftermath(ToNativeAftermathKind(massacreProfile.AftermathKind), triggerSource, triggerDetail);
		EnsureAlliedTroopsSummoned();
		EnsureInterventionCivilianEnemyTeam(Mission.Current);
		DriveMassacreCombatState(Mission.Current);
		if (first)
		{
			ShowOutcomeMessageOnce(massacreProfile.MessageKey, massacreProfile.MessageText, massacreProfile.MessageColor);
			RecordInterventionMemory(massacreProfile.MemoryTitle, massacreProfile.BuildMassacreMemoryText(triggerSource));
		}
		return first;
	}

	private static bool RequestCulturalRepopulation(int targetAgentIndex, string triggerSource, string triggerDetail)
	{
		try
		{
			if (!AlliedAgentIndexes.Contains(targetAgentIndex))
			{
				InformationManager.DisplayMessage(new InformationMessage(SiegeCulturalRepopulationProfile.TargetValidationMessage, Color.FromUint(SiegeCulturalRepopulationProfile.ValidationMessageColor)));
				return false;
			}
			bool handled = true;
			SiegeCulturalRepopulationProfile repopulationProfile = new SiegeCulturalRepopulationProfile();
			if (!_massacreStarted)
			{
				handled |= StartMassacre(repopulationProfile.MassacreTriggerSource, repopulationProfile.MassacreTriggerDetail);
			}
			CultureObject targetCulture = ResolveCulturalRepopulationTargetCulture(out string targetCultureSource);
			string targetCultureText = DescribeCultureForMessage(targetCulture, targetCultureSource);
			_culturalRepopulationRequested = true;
			MarkPendingAftermath(ToNativeAftermathKind(repopulationProfile.AftermathKind), triggerSource, triggerDetail);
			RecordInterventionMemory(repopulationProfile.MemoryTitle, repopulationProfile.BuildRequestMemoryText(targetCultureText));
			if (_massacreVictoryReached)
			{
				handled |= ApplyCulturalRepopulationNow(SiegeCulturalRepopulationProfile.VictoryAlreadyReachedApplySource);
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage(repopulationProfile.BuildPendingMessageText(targetCultureText), Color.FromUint(repopulationProfile.PendingMessageColor)));
			}
			return handled;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "RequestCulturalRepopulation failed: " + ex.Message);
			return false;
		}
	}

	private static bool ApplyCulturalRepopulationNow(string source)
	{
		try
		{
			if (_culturalRepopulationApplied)
			{
				return false;
			}
			Settlement settlement = ResolveCurrentSettlement();
			CultureObject targetCulture = ResolveCulturalRepopulationTargetCulture(out string targetCultureSource);
			if (settlement == null || targetCulture == null)
			{
				Logger.Log("SiegeAiIntervention", "ApplyCulturalRepopulationNow skipped. Source=" + (source ?? "N/A") + ", Settlement=" + (settlement?.StringId ?? "null") + ", TargetCulture=" + (targetCulture?.StringId ?? "null") + ", TargetCultureSource=" + (targetCultureSource ?? "N/A"));
				return false;
			}
			CultureObject oldCulture = settlement.Culture;
			settlement.Culture = targetCulture;
			int boundVillagesChanged = 0;
			try
			{
				if (settlement.BoundVillages != null)
				{
					foreach (Village village in settlement.BoundVillages)
					{
						if (village?.Settlement != null)
						{
							village.Settlement.Culture = targetCulture;
							boundVillagesChanged++;
						}
					}
				}
			}
			catch
			{
			}
			int killedNotables = 0;
			int spawnedNotables = 0;
			ReplaceTownNotablesForCulturalRepopulation(settlement, targetCulture, source, out killedNotables, out spawnedNotables);
			_lastKilledNotables += killedNotables;
			_culturalRepopulationApplied = true;
			SiegeCulturalRepopulationProfile repopulationProfile = new SiegeCulturalRepopulationProfile();
			string notableResultText = repopulationProfile.BuildCompletedNotableResultText(settlement.IsTown, killedNotables, spawnedNotables);
			string settlementName = settlement.Name?.ToString();
			string targetCultureText = DescribeCultureForMessage(targetCulture, targetCultureSource);
			InformationManager.DisplayMessage(new InformationMessage(repopulationProfile.BuildCompletedMessageText(settlementName, targetCultureText, notableResultText), Color.FromUint(repopulationProfile.CompletedMessageColor)));
			Logger.Log("SiegeAiIntervention", "Applied purge repopulation. Source=" + (source ?? "N/A") + ", Settlement=" + (settlement.StringId ?? "N/A") + ", OldCulture=" + (oldCulture?.StringId ?? "N/A") + ", NewCulture=" + (targetCulture.StringId ?? "N/A") + ", TargetCultureSource=" + (targetCultureSource ?? "N/A") + ", BoundVillages=" + boundVillagesChanged + ", KilledNotables=" + killedNotables + ", SpawnedNotables=" + spawnedNotables);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyCulturalRepopulationNow failed: " + ex.Message);
			return false;
		}
	}

	private static void ReplaceTownNotablesForCulturalRepopulation(Settlement settlement, CultureObject targetCulture, string source, out int killedNotables, out int spawnedNotables)
	{
		killedNotables = 0;
		spawnedNotables = 0;
		try
		{
			if (settlement == null || targetCulture == null || !settlement.IsTown)
			{
				return;
			}
			List<Hero> oldNotables = settlement.Notables?.Where((Hero notable) => notable != null && notable != Hero.MainHero && notable.IsNotable && notable.IsAlive).ToList() ?? new List<Hero>();
			foreach (Hero notable in oldNotables)
			{
				if (TryKillNotableForCulturalRepopulation(notable, source))
				{
					killedNotables++;
				}
			}
			SpawnReplacementNotablesForCulturalRepopulation(settlement, targetCulture, Occupation.GangLeader, source, ref spawnedNotables);
			SpawnReplacementNotablesForCulturalRepopulation(settlement, targetCulture, Occupation.Artisan, source, ref spawnedNotables);
			SpawnReplacementNotablesForCulturalRepopulation(settlement, targetCulture, Occupation.Merchant, source, ref spawnedNotables);
			Logger.Log("SiegeAiIntervention", "Replaced town notables for purge repopulation. Source=" + (source ?? "N/A") + ", Settlement=" + (settlement.StringId ?? "N/A") + ", TargetCulture=" + (targetCulture.StringId ?? "N/A") + ", Killed=" + killedNotables + ", Spawned=" + spawnedNotables);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ReplaceTownNotablesForCulturalRepopulation failed. Source=" + (source ?? "N/A") + ", Settlement=" + (settlement?.StringId ?? "null") + ": " + ex.Message);
		}
	}

	private static bool TryKillNotableForCulturalRepopulation(Hero notable, string source)
	{
		try
		{
			if (notable == null || notable == Hero.MainHero || !notable.IsNotable || !notable.IsAlive)
			{
				return false;
			}
			try
			{
				float power = notable.Power;
				if (Math.Abs(power) > 0.01f)
				{
					notable.AddPower(-power);
				}
			}
			catch (Exception ex)
			{
				Logger.Log("SiegeAiIntervention", "Failed to zero notable power before purge replacement. Source=" + (source ?? "N/A") + ", Notable=" + (notable.StringId ?? "N/A") + ": " + ex.Message);
			}
			KillCharacterAction.ApplyByMurder(notable, Hero.MainHero, false);
			if (notable.IsAlive)
			{
				KillCharacterAction.ApplyByRemove(notable, false, true);
			}
			if (!notable.IsAlive)
			{
				return true;
			}
			Logger.Log("SiegeAiIntervention", "Purge repopulation could not kill notable. Source=" + (source ?? "N/A") + ", Notable=" + (notable.StringId ?? "N/A"));
			return false;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryKillNotableForCulturalRepopulation failed. Source=" + (source ?? "N/A") + ", Notable=" + (notable?.StringId ?? "null") + ": " + ex.Message);
			return false;
		}
	}

	private static void SpawnReplacementNotablesForCulturalRepopulation(Settlement settlement, CultureObject targetCulture, Occupation occupation, string source, ref int spawnedNotables)
	{
		try
		{
			if (settlement == null || targetCulture == null)
			{
				return;
			}
			int targetCount = Campaign.Current?.Models?.NotableSpawnModel?.GetTargetNotableCountForSettlement(settlement, occupation) ?? 0;
			int currentCount = 0;
			if (settlement.Notables != null)
			{
				currentCount = settlement.Notables.Count((Hero notable) => notable != null && notable.IsAlive && notable.Occupation == occupation);
			}
			for (int i = currentCount; i < targetCount; i++)
			{
				try
				{
					Hero newNotable = HeroCreator.CreateNotable(occupation, settlement);
					if (newNotable == null)
					{
						Logger.Log("SiegeAiIntervention", "Purge repopulation failed to create replacement notable. Source=" + (source ?? "N/A") + ", Settlement=" + (settlement.StringId ?? "N/A") + ", Occupation=" + occupation);
						continue;
					}
					if (newNotable.Culture != targetCulture)
					{
						newNotable.Culture = targetCulture;
					}
					if (newNotable.CurrentSettlement != settlement)
					{
						EnterSettlementAction.ApplyForCharacterOnly(newNotable, settlement);
					}
					spawnedNotables++;
				}
				catch (Exception ex)
				{
					Logger.Log("SiegeAiIntervention", "Purge repopulation replacement notable spawn failed. Source=" + (source ?? "N/A") + ", Settlement=" + (settlement.StringId ?? "N/A") + ", Occupation=" + occupation + ": " + ex.Message);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "SpawnReplacementNotablesForCulturalRepopulation failed. Source=" + (source ?? "N/A") + ", Settlement=" + (settlement?.StringId ?? "null") + ", Occupation=" + occupation + ": " + ex.Message);
		}
	}

	private static void EnsureAlliedTroopsSummoned()
	{
		if (!_alliedTroopsAutoSummoned)
		{
			_alliedTroopsAutoSummoned = true;
			SummonAlliedTroops(AutoSummonCount, SiegeInterventionEntryProfile.EnsureAlliedTroopsSummonSource);
		}
	}

	private static void ApplyPlayerBattleEquipment()
	{
		if (_playerBattleEquipmentApplied)
		{
			return;
		}
		try
		{
			Agent main = Agent.Main ?? Mission.Current?.MainAgent;
			Equipment equipment = Hero.MainHero?.BattleEquipment;
			if (main == null || equipment == null)
			{
				return;
			}
			main.UpdateSpawnEquipmentAndRefreshVisuals(equipment);
			main.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.Any);
			_playerBattleEquipmentApplied = true;
			InformationManager.DisplayMessage(new InformationMessage(SiegeInterventionEntryProfile.BattleEquipmentAppliedMessage, Color.FromUint(SiegeInterventionEntryProfile.BattleEquipmentAppliedMessageColor)));
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyPlayerBattleEquipment failed: " + ex.Message);
		}
	}

	private static void RemoveDefeatedGuardAgents(Mission mission)
	{
		if (mission?.Agents == null)
		{
			return;
		}
		foreach (Agent agent in mission.Agents.ToList())
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive() || agent == Agent.Main || AlliedAgentIndexes.Contains(agent.Index))
			{
				continue;
			}
			if (IsGuardOrSoldier(agent.Character as CharacterObject))
			{
				RemoveGuardAgent(agent);
			}
		}
	}

	private static void RemoveBackstreetCrimeAgents(Mission mission)
	{
		if (mission?.Agents == null)
		{
			return;
		}
		foreach (Agent agent in mission.Agents.ToList())
		{
			try
			{
				if (agent == null || !agent.IsHuman || !agent.IsActive() || agent == Agent.Main || AlliedAgentIndexes.Contains(agent.Index))
				{
					continue;
				}
				if (IsBackstreetOrCriminalCharacter(agent.Character as CharacterObject))
				{
					ShoutBehavior.CancelAgentSpeechForRemovalExternal(agent.Index, SiegeSceneAgentSuppressionProfile.BackstreetCriminalRemovedReason);
					agent.FadeOut(hideInstantly: true, hideMount: true);
				}
			}
			catch
			{
			}
		}
	}

	private static void RemoveUnsafeAssemblyCivilianAgents(Mission mission)
	{
		if (mission?.Agents == null)
		{
			return;
		}
		foreach (Agent agent in mission.Agents.ToList())
		{
			try
			{
				if (agent == null || !agent.IsHuman || !agent.IsActive() || agent == Agent.Main || AlliedAgentIndexes.Contains(agent.Index))
				{
					continue;
				}
				CharacterObject character = agent.Character as CharacterObject;
				if (!IsCivilianForIntervention(character))
				{
					continue;
				}
				if (IsUnsafeAssemblyCivilianTemplate(character))
				{
					ShoutBehavior.CancelAgentSpeechForRemovalExternal(agent.Index, SiegeSceneAgentSuppressionProfile.UnsafeOrNakedCivilianRemovedReason);
					agent.FadeOut(hideInstantly: true, hideMount: true);
					SceneCivilianAgentIndexes.Remove(agent.Index);
					CivilianSpeechRallySlots.Remove(agent.Index);
					CivilianGatherFollowerAgentIndexes.Remove(agent.Index);
					CivilianGatherReadyFormationAgentIndexes.Remove(agent.Index);
					CivilianGatherMovePreparedAgentIndexes.Remove(agent.Index);
					CivilianFrightenedActionAgentIndexes.Remove(agent.Index);
					CivilianPreMassacrePreparedAgentIndexes.Remove(agent.Index);
					Logger.Log("SiegeAiIntervention", "Removed unsafe/naked civilian agent. Agent=" + agent.Index + ", Character=" + (character?.StringId ?? "N/A") + ", Name=" + (character?.Name?.ToString() ?? "N/A"));
				}
			}
			catch (Exception ex)
			{
				Logger.Log("SiegeAiIntervention", "RemoveUnsafeAssemblyCivilianAgents failed: " + ex.Message);
			}
		}
	}

	private static void RemoveProtectedSceneAgents(Mission mission)
	{
		if (mission?.Agents == null)
		{
			return;
		}
		foreach (Agent agent in mission.Agents.ToList())
		{
			try
			{
				if (agent == null || !agent.IsHuman || !agent.IsActive() || agent == Agent.Main || AlliedAgentIndexes.Contains(agent.Index))
				{
					continue;
				}
				if (IsProtectedChildAgent(agent))
				{
					ShoutBehavior.CancelAgentSpeechForRemovalExternal(agent.Index, SiegeSceneAgentSuppressionProfile.ProtectedAgentSuppressedReason);
					agent.FadeOut(hideInstantly: true, hideMount: true);
				}
			}
			catch
			{
			}
		}
	}

	private static void RemovePlayerCompanionSceneAgents(Mission mission)
	{
		if (mission?.Agents == null)
		{
			return;
		}
		foreach (Agent agent in mission.Agents.ToList())
		{
			try
			{
				if (!IsPlayerOwnedCompanionSceneAgent(agent))
				{
					continue;
				}
				ShoutBehavior.CancelAgentSpeechForRemovalExternal(agent.Index, SiegeSceneAgentSuppressionProfile.PlayerCompanionSceneSpawnSuppressedReason);
				agent.FadeOut(hideInstantly: true, hideMount: true);
			}
			catch
			{
			}
		}
	}

	private static bool IsPlayerOwnedCompanionSceneAgent(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive() || agent == Agent.Main || AlliedAgentIndexes.Contains(agent.Index))
			{
				return false;
			}
			CharacterObject character = agent.Character as CharacterObject;
			Hero hero = character?.HeroObject;
			if (hero == null || hero == Hero.MainHero)
			{
				return false;
			}
			if (hero.IsPlayerCompanion)
			{
				return true;
			}
			return hero.Clan == Clan.PlayerClan && hero.PartyBelongedTo == MobileParty.MainParty;
		}
		catch
		{
			return false;
		}
	}

	private static void RemoveGuardAgent(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsActive())
			{
				return;
			}
			ShoutBehavior.CancelAgentSpeechForRemovalExternal(agent.Index, SiegeSceneAgentSuppressionProfile.GuardRemovedReason);
			agent.FadeOut(hideInstantly: true, hideMount: true);
		}
		catch
		{
		}
	}

	private static void TrackSceneCivilianAgents(Mission mission)
	{
		if (mission?.Agents == null)
		{
			return;
		}
		foreach (Agent agent in mission.Agents.ToList())
		{
			if (IsEligibleCivilianAgent(agent, includeHeroes: true))
			{
				SceneCivilianAgentIndexes.Add(agent.Index);
				_lastSceneCivilianSpawnedCount = Math.Max(_lastSceneCivilianSpawnedCount, SceneCivilianAgentIndexes.Count);
			}
		}
	}

	private static void MaintainCivilianAssembly(Mission mission, string source, bool force)
	{
		try
		{
			if (mission?.Agents == null || _massacreStarted || _massacreVictoryReached)
			{
				return;
			}
			int desiredCount = GetDesiredCivilianAssemblyCount(mission);
			EnsureCivilianAssemblyPopulation(mission);
			int total = 0;
			foreach (Agent agent in mission.Agents.ToList().OrderBy(a => a?.Index ?? int.MaxValue))
			{
				if (!IsEligibleCivilianAgent(agent, includeHeroes: true))
				{
					continue;
				}
				total++;
				SceneCivilianAgentIndexes.Add(agent.Index);
				PrepareCivilianForPreMassacreHitDetection(agent, mission);
				ApplyOneTimeFrightenedCivilianAction(agent, allowGathered: true);
			}
			_lastSceneCivilianSpawnedCount = Math.Max(_lastSceneCivilianSpawnedCount, Math.Max(total, SceneCivilianAgentIndexes.Count));
			if (!_civilianAssemblyMessageShown && total > 0)
			{
				_civilianAssemblyMessageShown = true;
				InformationManager.DisplayMessage(new InformationMessage(SiegeCivilianGatherUiProfile.BuildCivilianPreparedMessage(total), Color.FromUint(SiegeCivilianGatherUiProfile.MessageColor)));
				Logger.Log("SiegeAiIntervention", "Civilian town population prepared. Source=" + (source ?? "N/A") + ", Civilians=" + total + ", Desired=" + desiredCount);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "MaintainCivilianAssembly failed (" + (source ?? "N/A") + "): " + ex.Message);
		}
	}

	private static void InitializeCivilianAssemblyPoint(Mission mission, Agent main)
	{
		InitializeCivilianAssemblyPoint(mission, main, CivilianAssemblyForwardDistance);
	}

	private static void InitializeCivilianAssemblyPoint(Mission mission, Agent main, float forwardDistance)
	{
		Vec3 forward = main?.LookDirection ?? Vec3.Forward;
		if (forward.LengthSquared < 0.01f)
		{
			forward = Vec3.Forward;
		}
		forward.Normalize();
		Vec3 anchor = (main?.Position ?? Vec3.Zero) + forward * Math.Max(1f, forwardDistance);
		try
		{
			if (mission?.Scene != null)
			{
				anchor.z = mission.Scene.GetGroundHeightAtPosition(anchor);
			}
		}
		catch
		{
		}
		_civilianAssemblyAnchor = anchor;
		_civilianAssemblyForward = forward;
		_civilianAssemblyPointReady = true;
	}

	private static bool GatherCiviliansForSpeech(string source, int seedAgentIndex = -1)
	{
		try
		{
			Mission mission = Mission.Current;
			Agent main = Agent.Main ?? mission?.MainAgent;
			if (mission?.Agents == null || main == null || !main.IsActive() || _massacreStarted || _massacreVictoryReached)
			{
				return false;
			}
			Agent seed = TryGetAgent(seedAgentIndex);
			bool seedIsSoldier = IsInterventionAlliedSoldierForExternal(seed, requireActive: true);
			if (_civilianFormationControlPending || _civilianFormationControlComplete)
			{
				if (SiegeCivilianGatherInteractionProfile.ShouldReleaseSoldiersForCommandControlRepeat(_civilianFormationControlPending, _civilianFormationControlComplete, seedIsSoldier, source))
				{
					string releaseSource = SiegeCivilianGatherInteractionProfile.BuildGatherSoldierReturnSource(SiegeCivilianGatherInteractionProfile.CommandControlRepeatSoldierReleaseSource);
					int returned = ReturnAlliedGatherSoldiersToFormation(mission, releaseSource);
					Logger.Log("SiegeAiIntervention", "Handled repeated soldier gather during command control. Source=" + (source ?? "N/A") + ", Seed=" + (seed?.Index.ToString() ?? "none") + ", Returned=" + returned);
					return true;
				}
				Logger.Log("SiegeAiIntervention", "Civilian gathering already entering command control; ignored new messenger trigger. Source=" + (source ?? "N/A"));
				return false;
			}
			bool firstStart = !_civilianSpeechRallyActive && !_civilianGatherPropagationActive;
			if (firstStart)
			{
				_civilianSpeechRallyActive = true;
				_civilianGatherPropagationActive = true;
				_civilianFormationControlPending = false;
				_civilianFormationControlComplete = false;
				_civilianFormationControlMessageShown = false;
				_civilianFormationControlNotBeforeTime = -1f;
				_nextCivilianFormationControlBatchTime = 0f;
				_nextCivilianGatherTickTime = 0f;
				_civilianGatherStartedAt = mission.CurrentTime;
				_civilianGatherMessengerSpeechBudget = CivilianGatherMessengerSpeechMinCount + ((CivilianGatherMessengerSpeechMaxCount > CivilianGatherMessengerSpeechMinCount && MBRandom.RandomFloat >= 0.5f) ? 1 : 0);
				_civilianGatherMessengerSpeechCount = 0;
				ActiveCivilianGatherInteractions.Clear();
				CivilianGatherReadyFormationAgentIndexes.Clear();
				CivilianGatherMessengerAgentIndexes.Clear();
				CivilianGatherMessengerSpeechAgentIndexes.Clear();
			}
			EnsureCivilianAssemblyPopulation(mission);
			TrackSceneCivilianAgents(mission);
			bool seedIsCivilian = IsEligibleCivilianAgent(seed, includeHeroes: true);
			int addedMessengers = 0;
			if (seedIsSoldier)
			{
				addedMessengers += EnsureSoldierGatherMessengers(mission, seed);
			}
			else
			{
				if (!seedIsCivilian)
				{
					seed = mission.Agents.Where(a => IsEligibleCivilianAgent(a, includeHeroes: true)).OrderBy(a => a.Position.DistanceSquared(main.Position)).FirstOrDefault();
				}
				if (MarkAgentAsCivilianGatherMessenger(seed, SiegeCivilianGatherInteractionProfile.BuildGatherSeedSource(source)))
				{
					addedMessengers++;
				}
			}
			if (CivilianGatherMessengerAgentIndexes.Count == 0)
			{
				Agent fallback = mission.Agents.Where(a => IsEligibleCivilianAgent(a, includeHeroes: true)).OrderBy(a => a.Position.DistanceSquared(main.Position)).FirstOrDefault();
				if (MarkAgentAsCivilianGatherMessenger(fallback, SiegeCivilianGatherInteractionProfile.BuildGatherFallbackSource(source)))
				{
					addedMessengers++;
				}
			}
			int total = RebuildCivilianSpeechRallySlots(mission);
			MaintainCivilianSpeechRally(mission, force: true);
			if (firstStart)
			{
				InformationManager.DisplayMessage(new InformationMessage(SiegeCivilianGatherUiProfile.PropagationStartedMessage, Color.FromUint(SiegeCivilianGatherUiProfile.MessageColor)));
				RecordInterventionMemory(SiegeCivilianGatherUiProfile.GatherMemoryTitle, SiegeCivilianGatherUiProfile.BuildPropagationStartedMemory(seedIsSoldier));
			}
			else if (addedMessengers > 0)
			{
				InformationManager.DisplayMessage(new InformationMessage(SiegeCivilianGatherUiProfile.MessengerAddedMessage, Color.FromUint(SiegeCivilianGatherUiProfile.MessageColor)));
				RecordInterventionMemory(SiegeCivilianGatherUiProfile.GatherMemoryTitle, SiegeCivilianGatherUiProfile.BuildMessengerAddedMemory(CivilianGatherMessengerAgentIndexes.Count));
			}
			Logger.Log("SiegeAiIntervention", "Updated civilian gathering propagation. Source=" + (source ?? "N/A") + ", FirstStart=" + firstStart + ", Seed=" + (seed?.Index.ToString() ?? "none") + ", SeedSoldier=" + seedIsSoldier + ", AddedMessengers=" + addedMessengers + ", Messengers=" + CivilianGatherMessengerAgentIndexes.Count + ", Civilians=" + total);
			return firstStart || addedMessengers > 0;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "GatherCiviliansForSpeech failed: " + ex.Message);
			return false;
		}
	}

	private static int RebuildCivilianSpeechRallySlots(Mission mission)
	{
		CivilianSpeechRallySlots.Clear();
		if (mission?.Agents == null)
		{
			return 0;
		}
		int slot = 0;
		foreach (Agent agent in mission.Agents.ToList().Where(a => IsEligibleCivilianAgent(a, includeHeroes: true)).OrderBy(a => a?.Index ?? int.MaxValue))
		{
			if (agent == null)
			{
				continue;
			}
			CivilianSpeechRallySlots[agent.Index] = slot++;
			SceneCivilianAgentIndexes.Add(agent.Index);
		}
		return slot;
	}

	private static void MaintainCivilianSpeechRally(Mission mission, bool force)
	{
		try
		{
			if (!_civilianSpeechRallyActive || mission?.Agents == null || _massacreStarted || _massacreVictoryReached)
			{
				return;
			}
			if (_civilianFormationControlPending || _civilianFormationControlComplete)
			{
				return;
			}
			Agent main = Agent.Main ?? mission.MainAgent;
			if (main == null || !main.IsActive())
			{
				return;
			}
			EnsureCivilianAssemblyPopulation(mission);
			TrackSceneCivilianAgents(mission);
			if (CivilianSpeechRallySlots.Count == 0 || force)
			{
				RebuildCivilianSpeechRallySlots(mission);
			}
			float now = mission.CurrentTime;
			if (_civilianGatherPropagationActive && _civilianGatherStartedAt >= 0f && now - _civilianGatherStartedAt >= CivilianGatherFallbackSeconds)
			{
				foreach (Agent agent in mission.Agents.ToList().Where(a => IsEligibleCivilianAgent(a, includeHeroes: true)))
				{
					MarkCivilianAsGatherFollower(agent, SiegeCivilianGatherInteractionProfile.FallbackFollowerSource);
				}
				ActiveCivilianGatherInteractions.Clear();
				QueueCivilianFormationControl(mission, SiegeCivilianGatherInteractionProfile.FallbackElapsedFormationSource);
				return;
			}
			if (_civilianGatherPropagationActive && now >= _nextCivilianGatherTickTime)
			{
				_nextCivilianGatherTickTime = now + 0.5f;
				UpdateCivilianGatherInteractions(mission);
				AssignCivilianGatherInteractions(mission);
			}
			bool allFollowing = AreAllCiviliansGatherFollowing(mission);
			if (allFollowing)
			{
				_civilianGatherPropagationActive = false;
				ActiveCivilianGatherInteractions.Clear();
				MaintainCivilianGatherFollowers(mission, main, force: true);
				if (AreAllCivilianGatherFollowersSettled(mission, main))
				{
					QueueCivilianFormationControl(mission, SiegeCivilianGatherInteractionProfile.AllGatheredAndSettledFormationSource);
					return;
				}
			}
			else
			{
				MaintainCivilianGatherFollowers(mission, main, force);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "MaintainCivilianSpeechRally failed: " + ex.Message);
		}
	}

	private static bool MarkCivilianAsGatherFollower(Agent agent, string reason)
	{
		try
		{
			if (!IsEligibleCivilianAgent(agent, includeHeroes: true))
			{
				return false;
			}
			PrepareCivilianForPreMassacreHitDetection(agent, Mission.Current ?? agent.Mission);
			NeutralizeCivilianDailyUsableBehavior(agent, SiegeCivilianGatherInteractionProfile.BuildGatherMarkSource(reason));
			CivilianGatherFollowerAgentIndexes.Add(agent.Index);
			if (CivilianGatherMessengerAgentIndexes.Contains(agent.Index))
			{
				CivilianGatherMovePreparedAgentIndexes.Add(agent.Index);
			}
			if (!CivilianSpeechRallySlots.ContainsKey(agent.Index))
			{
				CivilianSpeechRallySlots[agent.Index] = CivilianSpeechRallySlots.Count;
			}
			agent.SetWatchState(Agent.WatchState.Patrolling);
			agent.InvalidateTargetAgent();
			ClearAgentLookTarget(agent);
			try
			{
				agent.SetCrouchMode(false);
				agent.SetMaximumSpeedLimit(-1f, false);
			}
			catch
			{
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "MarkCivilianAsGatherFollower failed (" + (reason ?? "N/A") + "): " + ex.Message);
			return false;
		}
	}

	private static bool MarkAgentAsCivilianGatherMessenger(Agent agent, string reason)
	{
		try
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive() || agent == Agent.Main)
			{
				return false;
			}
			if (IsInterventionAlliedSoldierForExternal(agent, requireActive: true))
			{
				if (!CivilianGatherMessengerAgentIndexes.Add(agent.Index))
				{
					return false;
				}
				DisableCompanionStyleFollow(agent);
				AssignAgentToPlayerFormation(agent, FormationClass.Infantry, refreshFormationOrders: false);
				agent.ClearTargetFrame();
				agent.InvalidateTargetAgent();
				ClearAgentLookTarget(agent);
				agent.SetWatchState(Agent.WatchState.Patrolling);
				agent.SetCrouchMode(false);
				agent.SetMaximumSpeedLimit(-1f, false);
				Logger.Log("SiegeAiIntervention", "Added soldier civilian-gather messenger. Reason=" + (reason ?? "N/A") + ", Agent=" + agent.Index);
				return true;
			}
			if (!IsEligibleCivilianAgent(agent, includeHeroes: true))
			{
				return false;
			}
			if (!CivilianGatherMessengerAgentIndexes.Add(agent.Index))
			{
				return false;
			}
			MarkCivilianAsGatherFollower(agent, reason);
			agent.InvalidateTargetAgent();
			ClearAgentLookTarget(agent);
			Logger.Log("SiegeAiIntervention", "Added civilian gather messenger. Reason=" + (reason ?? "N/A") + ", Agent=" + agent.Index);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "MarkAgentAsCivilianGatherMessenger failed (" + (reason ?? "N/A") + "): " + ex.Message);
			return false;
		}
	}

	private static int EnsureSoldierGatherMessengers(Mission mission, Agent seed)
	{
		try
		{
			if (mission?.Agents == null)
			{
				return 0;
			}
			List<Agent> soldiers = mission.Agents
				.Where(a => IsInterventionAlliedSoldierForExternal(a, requireActive: true))
				.OrderBy(a => seed != null ? a.Position.DistanceSquared(seed.Position) : a.Index)
				.ThenBy(a => a.Index)
				.ToList();
			if (soldiers.Count == 0)
			{
				return 0;
			}
			int desired = Math.Max(1, (int)Math.Ceiling(soldiers.Count * CivilianGatherSoldierMessengerRatio));
			int current = soldiers.Count(a => CivilianGatherMessengerAgentIndexes.Contains(a.Index));
			int added = 0;
			if (seed != null && IsInterventionAlliedSoldierForExternal(seed, requireActive: true) && current < desired)
			{
				if (MarkAgentAsCivilianGatherMessenger(seed, SiegeCivilianGatherInteractionProfile.SoldierSeedMessengerSource))
				{
					added++;
					current++;
				}
			}
			foreach (Agent soldier in soldiers)
			{
				if (current >= desired)
				{
					break;
				}
				if (soldier == null || (seed != null && soldier.Index == seed.Index))
				{
					continue;
				}
				if (MarkAgentAsCivilianGatherMessenger(soldier, SiegeCivilianGatherInteractionProfile.SoldierMessengerSource))
				{
					added++;
					current++;
				}
			}
			Logger.Log("SiegeAiIntervention", "Ensured soldier gather messengers. Soldiers=" + soldiers.Count + ", Desired=" + desired + ", Added=" + added + ", Current=" + current + ", Seed=" + (seed?.Index.ToString() ?? "none"));
			return added;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "EnsureSoldierGatherMessengers failed: " + ex.Message);
			return 0;
		}
	}

	private static bool AreAllCiviliansGatherFollowing(Mission mission)
	{
		try
		{
			List<Agent> civilians = mission?.Agents?.Where(a => IsEligibleCivilianAgent(a, includeHeroes: true)).ToList() ?? new List<Agent>();
			return civilians.Count > 0 && civilians.All(a => CivilianGatherFollowerAgentIndexes.Contains(a.Index));
		}
		catch
		{
			return false;
		}
	}

	private static bool AreAllCivilianGatherFollowersSettled(Mission mission, Agent main)
	{
		try
		{
			if (mission?.Agents == null || main == null || !main.IsActive())
			{
				return false;
			}
			List<Agent> civilians = mission.Agents.Where(a => IsEligibleCivilianAgent(a, includeHeroes: true)).ToList();
			if (civilians.Count == 0)
			{
				return false;
			}
			int total = Math.Max(1, CivilianSpeechRallySlots.Count);
			float maxDistanceSq = CivilianGatherFormationSettleDistance * CivilianGatherFormationSettleDistance;
			foreach (Agent agent in civilians)
			{
				if (agent == null || !CivilianGatherFollowerAgentIndexes.Contains(agent.Index))
				{
					return false;
				}
				if (!CivilianSpeechRallySlots.TryGetValue(agent.Index, out int slot))
				{
					return false;
				}
				Vec3 target = GetCivilianFollowerRallyTarget(mission, main, slot, total);
				if (agent.Position.DistanceSquared(target) > maxDistanceSq)
				{
					return false;
				}
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void AssignCivilianGatherInteractions(Mission mission)
	{
		try
		{
			if (mission?.Agents == null)
			{
				return;
			}
			HashSet<int> busyMessengers = new HashSet<int>(ActiveCivilianGatherInteractions.Values.Select(x => x.MessengerAgentIndex));
			HashSet<int> busyTargets = new HashSet<int>(ActiveCivilianGatherInteractions.Keys);
			List<Agent> messengers = mission.Agents
				.Where(a => IsCivilianGatherMessengerAgent(a) && !busyMessengers.Contains(a.Index))
				.OrderBy(a => IsInterventionAlliedSoldierForExternal(a, requireActive: true) ? 0 : 1)
				.ThenBy(a => a.Index)
				.ToList();
			foreach (Agent messenger in messengers)
			{
				Agent target = mission.Agents
					.Where(a => IsEligibleCivilianAgent(a, includeHeroes: true) && !CivilianGatherFollowerAgentIndexes.Contains(a.Index) && !busyTargets.Contains(a.Index))
					.OrderBy(a => a.Position.DistanceSquared(messenger.Position))
					.FirstOrDefault();
				if (target == null)
				{
					break;
				}
				ActiveCivilianGatherInteractions[target.Index] = new CivilianGatherInteraction
				{
					MessengerAgentIndex = messenger.Index,
					TargetAgentIndex = target.Index,
					StartedAt = mission.CurrentTime,
					TalkSeconds = CivilianGatherTalkMinSeconds + MBRandom.RandomFloat * Math.Max(0f, CivilianGatherTalkMaxSeconds - CivilianGatherTalkMinSeconds)
				};
				busyTargets.Add(target.Index);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "AssignCivilianGatherInteractions failed: " + ex.Message);
		}
	}

	private static void UpdateCivilianGatherInteractions(Mission mission)
	{
		try
		{
			if (mission?.Agents == null || ActiveCivilianGatherInteractions.Count == 0)
			{
				return;
			}
			float now = mission.CurrentTime;
			foreach (CivilianGatherInteraction interaction in ActiveCivilianGatherInteractions.Values.ToList())
			{
				Agent messenger = mission.Agents.FirstOrDefault(a => a != null && a.Index == interaction.MessengerAgentIndex);
				Agent target = mission.Agents.FirstOrDefault(a => a != null && a.Index == interaction.TargetAgentIndex);
				if (!IsCivilianGatherMessengerAgent(messenger) || !IsEligibleCivilianAgent(target, includeHeroes: true) || CivilianGatherFollowerAgentIndexes.Contains(target.Index))
				{
					ReleaseGatherMessengerFromCurrentTarget(messenger, SiegeCivilianGatherInteractionProfile.InvalidOrAlreadyFollowerReleaseSource);
					ActiveCivilianGatherInteractions.Remove(interaction.TargetAgentIndex);
					continue;
				}
				NeutralizeCivilianDailyUsableBehavior(target, SiegeCivilianGatherInteractionProfile.TargetWaitSource);
				if (interaction.TalkStartedAt < 0f)
				{
					PrepareGatherMessengerMove(messenger, target);
				}
				float distanceSq = messenger.Position.DistanceSquared(target.Position);
				if (distanceSq <= CivilianGatherApproachDistance * CivilianGatherApproachDistance)
				{
					messenger.SetLookAgent(target);
					target.SetLookAgent(messenger);
					target.SetWatchState(Agent.WatchState.Patrolling);
					if (interaction.TalkStartedAt < 0f)
					{
						interaction.TalkStartedAt = now;
						PauseGatherMessengerForTalk(messenger, target);
						TryTriggerCivilianGatherMessengerSpeech(messenger, target);
					}
					if (now - interaction.TalkStartedAt >= interaction.TalkSeconds)
					{
						MarkCivilianAsGatherFollower(target, SiegeCivilianGatherInteractionProfile.FakeTalkFollowerSource);
						ReleaseGatherMessengerFromCurrentTarget(messenger, SiegeCivilianGatherInteractionProfile.TargetBecameFollowerReleaseSource);
						ActiveCivilianGatherInteractions.Remove(interaction.TargetAgentIndex);
					}
				}
				else if (now - interaction.StartedAt > 18f)
				{
					ReleaseGatherMessengerFromCurrentTarget(messenger, SiegeCivilianGatherInteractionProfile.InteractionTimeoutReleaseSource);
					ActiveCivilianGatherInteractions.Remove(interaction.TargetAgentIndex);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "UpdateCivilianGatherInteractions failed: " + ex.Message);
		}
	}

	private static bool IsCivilianGatherMessengerBusy(int agentIndex)
	{
		return ActiveCivilianGatherInteractions.Values.Any(x => x.MessengerAgentIndex == agentIndex);
	}

	private static bool IsCivilianGatherMessengerAgent(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive() || agent == Agent.Main)
			{
				return false;
			}
			return CivilianGatherMessengerAgentIndexes.Contains(agent.Index)
				&& (IsInterventionAlliedSoldierForExternal(agent, requireActive: true) || IsEligibleCivilianAgent(agent, includeHeroes: true));
		}
		catch
		{
			return false;
		}
	}

	private static void PrepareGatherMessengerMove(Agent messenger, Agent target)
	{
		try
		{
			if (messenger == null || target == null || !messenger.IsActive() || !target.IsActive())
			{
				return;
			}
			if (IsInterventionAlliedSoldierForExternal(messenger, requireActive: true))
			{
				DisableCompanionStyleFollow(messenger);
			}
			else
			{
				NeutralizeCivilianDailyUsableBehavior(messenger, SiegeCivilianGatherInteractionProfile.MessengerMoveSource);
			}
			messenger.SetWatchState(Agent.WatchState.Patrolling);
			messenger.SetMaximumSpeedLimit(CivilianGatherMessengerMoveSpeedLimit, false);
			messenger.InvalidateTargetAgent();
			ClearAgentLookTarget(messenger);
			SetAgentLookTowardPoint(messenger, target.Position);
			if (!TryGuideGatherMessengerToTargetAgent(messenger, target))
			{
				TrySetInterventionAgentTargetPosition(messenger, target.Position, SiegeAgentWallRescueProfile.Source + ":gather_messenger");
			}
		}
		catch
		{
		}
	}

	private static bool TryGuideGatherMessengerToTargetAgent(Agent messenger, Agent target)
	{
		try
		{
			if (messenger == null || target == null || !messenger.IsActive() || !target.IsActive() || messenger == target)
			{
				return false;
			}
			ScriptBehavior.AddAgentTarget(messenger, target);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryGuideGatherMessengerToTargetAgent failed: " + ex.Message);
			return false;
		}
	}

	private static void ReleaseGatherMessengerFromCurrentTarget(Agent messenger, string reason)
	{
		try
		{
			if (messenger == null || !messenger.IsActive())
			{
				return;
			}
			ClearGatherMessengerAgentTargetMovement(messenger);
			messenger.SetMaximumSpeedLimit(-1f, false);
			if (IsInterventionAlliedSoldierForExternal(messenger, requireActive: true))
			{
				AssignAgentToPlayerFormation(messenger, FormationClass.Infantry, refreshFormationOrders: false);
			}
			Logger.Log("SiegeAiIntervention", "Released gather messenger target. Reason=" + (reason ?? "N/A") + ", Agent=" + messenger.Index);
		}
		catch
		{
		}
	}

	private static void ClearGatherMessengerAgentTargetMovement(Agent messenger)
	{
		try
		{
			if (messenger == null || !messenger.IsActive())
			{
				return;
			}
			CampaignAgentComponent component = messenger.GetComponent<CampaignAgentComponent>();
			DailyBehaviorGroup behaviorGroup = component?.AgentNavigator?.GetBehaviorGroup<DailyBehaviorGroup>();
			if (behaviorGroup?.ScriptedBehavior is ScriptBehavior)
			{
				behaviorGroup.DisableScriptedBehavior();
			}
			messenger.DisableScriptedMovement();
			messenger.ClearTargetFrame();
			messenger.InvalidateTargetAgent();
			ClearAgentLookTarget(messenger);
		}
		catch
		{
		}
	}

	private static void PauseGatherMessengerForTalk(Agent messenger, Agent target)
	{
		try
		{
			if (messenger == null || !messenger.IsActive())
			{
				return;
			}
			try
			{
				ClearGatherMessengerAgentTargetMovement(messenger);
			}
			catch
			{
			}
			if (target != null && target.IsActive())
			{
				messenger.SetLookAgent(target);
				target.SetLookAgent(messenger);
				target.SetWatchState(Agent.WatchState.Patrolling);
				try
				{
					target.DisableScriptedMovement();
					target.ClearTargetFrame();
					target.InvalidateTargetAgent();
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

	private static void TryTriggerCivilianGatherMessengerSpeech(Agent messenger, Agent target)
	{
		try
		{
			if (messenger == null || target == null || !messenger.IsActive() || !target.IsActive())
			{
				return;
			}
			if (_civilianGatherMessengerSpeechCount >= _civilianGatherMessengerSpeechBudget)
			{
				return;
			}
			bool messengerIsSoldier = IsInterventionAlliedSoldierForExternal(messenger, requireActive: true);
			float now = (Mission.Current ?? messenger.Mission)?.CurrentTime ?? 0f;
			if (!CanStartAmbientReactionAudience(messengerIsSoldier, now) || HasPendingAmbientReactionAudience(messengerIsSoldier))
			{
				return;
			}
			if (!CivilianGatherMessengerSpeechAgentIndexes.Add(messenger.Index))
			{
				return;
			}
			string messengerName = messenger.Name?.ToString() ?? SiegeCivilianGatherUiProfile.MessengerFallbackName;
			string targetName = target.Name?.ToString() ?? SiegeCivilianGatherUiProfile.CivilianFallbackName;
			string factText = SiegeCivilianGatherUiProfile.BuildMessengerSpeechFactText(targetName);
			if (!ShoutBehavior.TriggerImmediateSceneBehaviorReactionForExternal(factText, messenger.Index, persistHeroPrivateHistory: true, suppressStare: true, postSpeechLeaveSeconds: -1f))
			{
				CivilianGatherMessengerSpeechAgentIndexes.Remove(messenger.Index);
				return;
			}
			_civilianGatherMessengerSpeechCount++;
			MarkAmbientReactionAudienceStarted(messengerIsSoldier, now);
			Logger.Log("SiegeAiIntervention", "Triggered gather messenger speech. Messenger=" + messenger.Index + "/" + messengerName + ", Target=" + target.Index + "/" + targetName + ", Count=" + _civilianGatherMessengerSpeechCount + ", Budget=" + _civilianGatherMessengerSpeechBudget);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryTriggerCivilianGatherMessengerSpeech failed: " + ex.Message);
		}
	}

	private static void MaintainCivilianGatherFollowers(Mission mission, Agent main, bool force)
	{
		try
		{
			if (mission?.Agents == null || main == null)
			{
				return;
			}
			int total = Math.Max(1, CivilianSpeechRallySlots.Count);
			foreach (Agent agent in mission.Agents.ToList().Where(a => IsEligibleCivilianAgent(a, includeHeroes: true) && CivilianGatherFollowerAgentIndexes.Contains(a.Index)))
			{
				if (_civilianGatherPropagationActive && CivilianGatherMessengerAgentIndexes.Contains(agent.Index))
				{
					continue;
				}
				if (IsCivilianGatherMessengerBusy(agent.Index))
				{
					continue;
				}
				if (!CivilianSpeechRallySlots.TryGetValue(agent.Index, out int slot))
				{
					slot = CivilianSpeechRallySlots.Count;
					CivilianSpeechRallySlots[agent.Index] = slot;
					total = Math.Max(total, CivilianSpeechRallySlots.Count);
				}
				MoveCivilianFollowerNearPlayer(agent, main, slot, total, force);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "MaintainCivilianGatherFollowers failed: " + ex.Message);
		}
	}

	private static void MoveCivilianFollowerNearPlayer(Agent agent, Agent main, int slot, int total, bool force)
	{
		try
		{
			if (agent == null || main == null || !agent.IsActive())
			{
				return;
			}
			Mission mission = Mission.Current ?? agent.Mission;
			Vec3 target = GetCivilianFollowerRallyTarget(mission, main, slot, total);
			float now = mission?.CurrentTime ?? 0f;
			bool firstMovePrepare = CivilianGatherMovePreparedAgentIndexes.Add(agent.Index);
			if (firstMovePrepare)
			{
				NeutralizeCivilianDailyUsableBehavior(agent, SiegeCivilianGatherInteractionProfile.FollowPrepareSource);
				ClearCivilianUpperBodyActionForMovement(agent);
			}
			bool targetChanged = !LastCivilianGatherFollowTargets.TryGetValue(agent.Index, out Vec3 lastTarget) || lastTarget.DistanceSquared(target) > 1.44f;
			float distanceToTargetSq = agent.Position.DistanceSquared(target);
			bool nearTarget = distanceToTargetSq <= CivilianSpeechRallySettleTolerance * CivilianSpeechRallySettleTolerance;
			if (!force && !targetChanged && LastCivilianGatherFollowOrderTimes.TryGetValue(agent.Index, out float last) && now - last < CivilianGatherFollowRefreshSeconds)
			{
				if (nearTarget)
				{
					SetCivilianLookTowardAssemblyInterior(agent, main);
				}
				return;
			}
			LastCivilianGatherFollowOrderTimes[agent.Index] = now;
			LastCivilianGatherFollowTargets[agent.Index] = target;
			agent.SetMaximumSpeedLimit(-1f, false);
			ClearCivilianUpperBodyActionForMovement(agent);
			ClearAgentLookTarget(agent);
			SetAgentLookTowardPoint(agent, target);
			TrySetInterventionAgentTargetPosition(agent, target, SiegeAgentWallRescueProfile.Source + ":civilian_gather");
			agent.SetWatchState(Agent.WatchState.Patrolling);
			if (nearTarget)
			{
				SetCivilianLookTowardAssemblyInterior(agent, main);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "MoveCivilianFollowerNearPlayer failed: " + ex.Message);
		}
	}

	private static void ClearCivilianUpperBodyActionForMovement(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive())
			{
				return;
			}
			ActionIndexCache current = agent.GetCurrentAction(1);
			if (current == ActionIndexCache.act_scared_idle_1)
			{
				agent.SetActionChannel(1, ActionIndexCache.act_none, true, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
			}
		}
		catch
		{
		}
	}

	private static Vec3 GetCivilianFollowerRallyTarget(Mission mission, Agent main, int slot, int total)
	{
		Vec3 forward = main?.LookDirection ?? Vec3.Forward;
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
		int columns = Math.Max(6, (int)Math.Ceiling(Math.Sqrt(Math.Max(1, total))));
		int row = Math.Max(0, slot / columns);
		int col = Math.Max(0, slot % columns);
		float side = (col - (columns - 1) * 0.5f) * 0.95f;
		float forwardDistance = 5.5f + row * 0.9f;
		Vec3 target = (main?.Position ?? Vec3.Zero) + forward * forwardDistance + right * side;
		try
		{
			if (mission?.Scene != null)
			{
				target.z = mission.Scene.GetGroundHeightAtPosition(target);
			}
		}
		catch
		{
		}
		return target;
	}

	private static void QueueCivilianFormationControl(Mission mission, string reason)
	{
		try
		{
			if (_civilianFormationControlComplete)
			{
				return;
			}
			StopCivilianGatherScriptFollowForCommandControl(mission, SiegeCivilianGatherInteractionProfile.BuildFormationQueueSource(reason));
			TrySetPlayerFormationFollowOrder(FormationClass.Ranged, SiegeCivilianGatherInteractionProfile.FormationControlBeginSource);
			float now = mission?.CurrentTime ?? 0f;
			if (!_civilianFormationControlPending)
			{
				_civilianFormationControlPending = true;
				_civilianFormationControlNotBeforeTime = now + CivilianFormationControlInitialDelaySeconds;
				_nextCivilianFormationControlBatchTime = _civilianFormationControlNotBeforeTime;
				RecordInterventionMemory(SiegeCivilianGatherUiProfile.AssemblyMemoryTitle, SiegeCivilianGatherUiProfile.BuildFormationQueuedMemory(reason));
				Logger.Log("SiegeAiIntervention", "Queued civilian formation control. Reason=" + (reason ?? "N/A"));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "QueueCivilianFormationControl failed: " + ex.Message);
		}
	}

	private static void StopCivilianGatherScriptFollowForCommandControl(Mission mission, string source)
	{
		try
		{
			_civilianSpeechRallyActive = false;
			_civilianGatherPropagationActive = false;
			ActiveCivilianGatherInteractions.Clear();
			ReturnGatherSoldierMessengersToFormation(mission, source);
			CivilianGatherMessengerAgentIndexes.Clear();
			CivilianGatherMessengerSpeechAgentIndexes.Clear();
			LastCivilianGatherFollowOrderTimes.Clear();
			LastCivilianGatherFollowTargets.Clear();
			foreach (int agentIndex in CivilianGatherFollowerAgentIndexes.ToList())
			{
				Agent agent = mission?.Agents?.FirstOrDefault(a => a != null && a.Index == agentIndex);
				if (!IsEligibleCivilianAgent(agent, includeHeroes: true))
				{
					continue;
				}
				try
				{
					agent.DisableScriptedMovement();
					agent.ClearTargetFrame();
					agent.InvalidateTargetAgent();
					agent.SetMaximumSpeedLimit(-1f, false);
					agent.SetCrouchMode(false);
					agent.SetShouldCatchUpWithFormation(true);
				}
				catch
				{
				}
			}
			Logger.Log("SiegeAiIntervention", "Stopped civilian scripted gather-follow before command control. Source=" + (source ?? "N/A"));
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "StopCivilianGatherScriptFollowForCommandControl failed (" + (source ?? "N/A") + "): " + ex.Message);
		}
	}

	private static int ReturnAlliedGatherSoldiersToFormation(Mission mission, string source)
	{
		try
		{
			if (mission?.Agents == null)
			{
				return 0;
			}
			int returned = 0;
			foreach (Agent soldier in mission.Agents.ToList().Where(a => IsInterventionAlliedSoldierForExternal(a, requireActive: true)))
			{
				try
				{
					RestoreAlliedSoldierFriendlyState(soldier, 0f, source, forceFollow: false, clearTarget: true);
					DisableCompanionStyleFollow(soldier);
					AssignAgentToPlayerFormation(soldier, FormationClass.Infantry, refreshFormationOrders: false);
					soldier.DisableScriptedMovement();
					soldier.ClearTargetFrame();
					soldier.InvalidateTargetAgent();
					soldier.SetMaximumSpeedLimit(-1f, false);
					soldier.SetCrouchMode(false);
					soldier.SetShouldCatchUpWithFormation(true);
					soldier.UpdateFormationOrders();
					soldier.SetWatchState(Agent.WatchState.Patrolling);
					CordonReadyAgentIndexes.Remove(soldier.Index);
					LastCordonMoveOrderTimesBySoldier.Remove(soldier.Index);
					LastCordonLookOrderTimesBySoldier.Remove(soldier.Index);
					returned++;
				}
				catch
				{
				}
			}
			if (returned > 0)
			{
				TrySetPlayerFormationFollowOrder(FormationClass.Infantry, source);
				Logger.Log("SiegeAiIntervention", "Returned gather allied soldiers to formation. Source=" + (source ?? "N/A") + ", Count=" + returned);
			}
			return returned;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ReturnAlliedGatherSoldiersToFormation failed (" + (source ?? "N/A") + "): " + ex.Message);
			return 0;
		}
	}

	private static void ReturnGatherSoldierMessengersToFormation(Mission mission, string source)
	{
		try
		{
			if (mission?.Agents == null || CivilianGatherMessengerAgentIndexes.Count == 0)
			{
				return;
			}
			int returned = 0;
			foreach (int messengerIndex in CivilianGatherMessengerAgentIndexes.ToList())
			{
				Agent messenger = mission.Agents.FirstOrDefault(a => a != null && a.Index == messengerIndex);
				if (messenger == null || !messenger.IsActive() || !IsInterventionAlliedSoldierForExternal(messenger, requireActive: true))
				{
					continue;
				}
				try
				{
					RestoreAlliedSoldierFriendlyState(messenger, 0f, SiegeCivilianGatherInteractionProfile.BuildGatherMessengerReturnSource(source), forceFollow: false, clearTarget: true);
					DisableCompanionStyleFollow(messenger);
					AssignAgentToPlayerFormation(messenger, FormationClass.Infantry, refreshFormationOrders: false);
					messenger.DisableScriptedMovement();
					messenger.ClearTargetFrame();
					messenger.InvalidateTargetAgent();
					messenger.SetMaximumSpeedLimit(-1f, false);
					messenger.SetCrouchMode(false);
					messenger.SetShouldCatchUpWithFormation(true);
					messenger.UpdateFormationOrders();
					messenger.SetWatchState(Agent.WatchState.Patrolling);
					CordonReadyAgentIndexes.Remove(messenger.Index);
					returned++;
				}
				catch
				{
				}
			}
			if (returned > 0)
			{
				Logger.Log("SiegeAiIntervention", "Returned gather soldier messengers to formation. Source=" + (source ?? "N/A") + ", Count=" + returned);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ReturnGatherSoldierMessengersToFormation failed (" + (source ?? "N/A") + "): " + ex.Message);
		}
	}

	private static void UpdatePendingCivilianFormationControl(Mission mission)
	{
		try
		{
			if (!_civilianFormationControlPending || _civilianFormationControlComplete || mission?.Agents == null || _massacreStarted || _massacreVictoryReached)
			{
				return;
			}
			if (mission.Mode == MissionMode.Conversation || mission.Mode == MissionMode.Barter)
			{
				return;
			}
			float now = mission.CurrentTime;
			if (now < _civilianFormationControlNotBeforeTime || now < _nextCivilianFormationControlBatchTime)
			{
				return;
			}
			_nextCivilianFormationControlBatchTime = now + CivilianFormationControlBatchIntervalSeconds;
			List<Agent> pending = mission.Agents
				.ToList()
				.Where(a => IsEligibleCivilianAgent(a, includeHeroes: true) && CivilianGatherFollowerAgentIndexes.Contains(a.Index) && !CivilianGatherReadyFormationAgentIndexes.Contains(a.Index))
				.Take(CivilianFormationControlBatchSize)
				.ToList();
			foreach (Agent agent in pending)
			{
				PrepareCivilianForPreMassacreHitDetection(agent, mission);
				NeutralizeCivilianDailyUsableBehavior(agent, SiegeCivilianGatherInteractionProfile.FormationControlBatchSource);
				AssignAgentToPlayerFormation(agent, FormationClass.Ranged, refreshFormationOrders: false);
				CivilianGatherReadyFormationAgentIndexes.Add(agent.Index);
				try
				{
					agent.DisableScriptedMovement();
					agent.ClearTargetFrame();
					agent.InvalidateTargetAgent();
					agent.SetMaximumSpeedLimit(-1f, false);
					agent.SetCrouchMode(false);
					agent.SetShouldCatchUpWithFormation(true);
				}
				catch
				{
				}
				agent.SetWatchState(Agent.WatchState.Patrolling);
				ApplyOneTimeFrightenedCivilianAction(agent, allowGathered: true);
			}
			bool allAssigned = !mission.Agents.ToList().Any(a => IsEligibleCivilianAgent(a, includeHeroes: true) && CivilianGatherFollowerAgentIndexes.Contains(a.Index) && !CivilianGatherReadyFormationAgentIndexes.Contains(a.Index));
			if (!allAssigned)
			{
				return;
			}
			_civilianFormationControlPending = false;
			_civilianFormationControlComplete = true;
			ApplyCivilianFormationFollowOrder(mission, SiegeCivilianGatherInteractionProfile.FormationReadyFollowSource);
			RecordInterventionMemory(SiegeCivilianGatherUiProfile.AssemblyMemoryTitle, SiegeCivilianGatherUiProfile.FormationCompleteMemory);
			if (!_civilianFormationControlMessageShown)
			{
				_civilianFormationControlMessageShown = true;
				InformationManager.DisplayMessage(new InformationMessage(SiegeCivilianGatherUiProfile.FormationReadyMessage, Color.FromUint(SiegeCivilianGatherUiProfile.MessageColor)));
			}
			if (!_civilianOrderControllerPrimed)
			{
				_civilianOrderControllerPrimed = TryPrimePlayerOrderController(mission, SiegeCivilianGatherInteractionProfile.FormationReadyOrderControllerSource, force: true);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "UpdatePendingCivilianFormationControl failed: " + ex.Message);
		}
	}

	private static bool ApplyCivilianFormationFollowOrder(Mission mission, string source)
	{
		bool orderIssued = TrySetPlayerFormationFollowOrder(FormationClass.Ranged, source);
		try
		{
			if (mission?.Agents == null)
			{
				return orderIssued;
			}
			foreach (int agentIndex in CivilianGatherReadyFormationAgentIndexes.ToList())
			{
				Agent agent = mission.Agents.FirstOrDefault(a => a != null && a.Index == agentIndex);
				if (!IsEligibleCivilianAgent(agent, includeHeroes: true))
				{
					continue;
				}
				try
				{
					AssignAgentToPlayerFormation(agent, FormationClass.Ranged, refreshFormationOrders: false);
					agent.DisableScriptedMovement();
					agent.ClearTargetFrame();
					agent.InvalidateTargetAgent();
					agent.SetMaximumSpeedLimit(-1f, false);
					agent.SetCrouchMode(false);
					agent.SetShouldCatchUpWithFormation(true);
					agent.UpdateFormationOrders();
				}
				catch
				{
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyCivilianFormationFollowOrder failed (" + (source ?? "N/A") + "): " + ex.Message);
		}
		return orderIssued;
	}

	private static void EnsureCivilianAssemblyPopulation(Mission mission)
	{
		try
		{
			if (mission?.Agents == null)
			{
				return;
			}
			int existing = mission.Agents.Count(a => IsEligibleCivilianAgent(a, includeHeroes: true));
			_lastSceneCivilianSpawnedCount = Math.Max(_lastSceneCivilianSpawnedCount, existing);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "EnsureCivilianAssemblyPopulation failed: " + ex.Message);
		}
	}

	private static int GetDesiredCivilianAssemblyCount(Mission mission = null)
	{
		if (mission?.Agents == null)
		{
			return 0;
		}
		int nativeCivilianCount = mission.Agents.Count(a => IsEligibleCivilianAgent(a, includeHeroes: true));
		return Math.Max(0, Math.Min(nativeCivilianCount, GetCivilianSceneCivilianCap(mission)));
	}

	private static int GetCivilianSceneCivilianCap(Mission mission)
	{
		try
		{
			if (mission?.Agents == null)
			{
				return TownCivilianAssemblySceneCap;
			}
			int settlementCap = TownCivilianAssemblySceneCap;
			int nativeCivilianCount = mission.Agents.Count(a => IsEligibleCivilianAgent(a, includeHeroes: true));
			int nonCivilianActiveCount = mission.Agents.Count(a => a != null && a.IsActive() && !IsEligibleCivilianAgent(a, includeHeroes: true));
			int totalAgentRoomCap = Math.Max(MinimumCivilianAssemblySceneCap, SceneTotalAgentSoftCap - nonCivilianActiveCount);
			int cap = Math.Min(settlementCap, Math.Min(nativeCivilianCount, totalAgentRoomCap));
			return Math.Max(0, cap);
		}
		catch
		{
			return TownCivilianAssemblySceneCap;
		}
	}

	private static bool IsUnsafeAssemblyCivilianTemplate(CharacterObject character)
	{
		if (character == null)
		{
			return true;
		}
		if (IsKnownUnsafeAssemblyCivilianTemplateName(character))
		{
			return true;
		}
		return !HasUsableCivilianBodyEquipment(character);
	}

	private static bool IsKnownUnsafeAssemblyCivilianTemplateName(CharacterObject character)
	{
		try
		{
			string text = ((character.StringId ?? "") + " " + (character.Name?.ToString() ?? "")).ToLowerInvariant();
			string compact = text.Replace("_", "").Replace("-", "").Replace(" ", "");
			return ContainsAny(text, "crazy man", "crazy_man", "crazy-man") || compact.Contains("crazyman") || (text.Contains("crazy") && text.Contains("t7"));
		}
		catch
		{
			return false;
		}
	}

	private static bool HasUsableCivilianBodyEquipment(CharacterObject character)
	{
		try
		{
			if (character == null)
			{
				return false;
			}
			IEnumerable<Equipment> equipments = character.CivilianEquipments ?? Enumerable.Empty<Equipment>();
			foreach (Equipment equipment in equipments)
			{
				if (EquipmentHasBodyClothing(equipment))
				{
					return true;
				}
			}
			return EquipmentHasBodyClothing(character.FirstCivilianEquipment) || EquipmentHasBodyClothing(character.RandomCivilianEquipment);
		}
		catch
		{
			return false;
		}
	}

	private static bool EquipmentHasBodyClothing(Equipment equipment)
	{
		try
		{
			return equipment != null && !equipment.IsEmpty() && equipment[EquipmentIndex.Body].Item != null;
		}
		catch
		{
			return false;
		}
	}

	private static bool AssignAgentToPlayerFormation(Agent agent, FormationClass formationClass, bool refreshFormationOrders = true)
	{
		try
		{
			Mission mission = Mission.Current ?? agent?.Mission;
			Agent main = Agent.Main ?? mission?.MainAgent;
			Team playerTeam = mission?.PlayerTeam ?? main?.Team;
			if (agent == null || !agent.IsHuman || !agent.IsActive() || playerTeam == null)
			{
				return false;
			}
			if (agent.Team != playerTeam)
			{
				agent.SetTeam(playerTeam, true);
			}
			EnsureAgentUnderPlayerCommand(agent);
			Formation formation = playerTeam.GetFormation(formationClass);
			MarkFormationPlayerCommandable(formation, main);
			if (formation != null && agent.Formation != formation)
			{
				agent.Formation = formation;
			}
			if (formation != null)
			{
				agent.TryAttachToFormation();
				agent.SetShouldCatchUpWithFormation(true);
				if (refreshFormationOrders)
				{
					agent.UpdateFormationOrders();
				}
			}
			return formation != null;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "AssignAgentToPlayerFormation failed: " + ex.Message);
			return false;
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
			Logger.Log("SiegeAiIntervention", "MarkFormationPlayerCommandable failed: " + ex.Message);
		}
	}

	private static void TrySetFormationProperty(Formation formation, string propertyName, object value)
	{
		try
		{
			PropertyInfo property = formation?.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property == null)
			{
				return;
			}
			MethodInfo setter = property.GetSetMethod(true);
			if (setter != null)
			{
				setter.Invoke(formation, new object[] { value });
			}
		}
		catch
		{
		}
	}

	private static void EnsureAgentUnderPlayerCommand(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsHuman || agent == Agent.Main)
			{
				return;
			}
			MarkAgentOriginUnderPlayerCommand(agent);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "EnsureAgentUnderPlayerCommand failed: " + ex.Message);
		}
	}

	private static void MarkAgentOriginUnderPlayerCommand(Agent agent)
	{
		try
		{
			IAgentOriginBase origin = agent?.Origin;
			if (origin == null)
			{
				return;
			}
			CommandableOriginRuntimeIds.Add(RuntimeHelpers.GetHashCode(origin));
		}
		catch
		{
		}
	}

	internal static bool ShouldForceCommandForOrigin(object origin)
	{
		try
		{
			if (origin == null || !IsActiveInCurrentMission())
			{
				return false;
			}
			return CommandableOriginRuntimeIds.Contains(RuntimeHelpers.GetHashCode(origin));
		}
		catch
		{
			return false;
		}
	}

	private static bool IsRuntimeAlliedSoldierAgent(Agent agent, CharacterObject character = null, Hero hero = null)
	{
		try
		{
			if (!IsActiveInCurrentMission())
			{
				return false;
			}
			character ??= agent?.Character as CharacterObject ?? hero?.CharacterObject;
			hero ??= character?.HeroObject;
			if (character == null || character == CharacterObject.PlayerCharacter || IsProtectedChildCharacter(character) || IsCivilianForIntervention(character) || IsBackstreetOrCriminalCharacter(character))
			{
				return false;
			}
			if (agent != null && AlliedAgentIndexes.Contains(agent.Index))
			{
				return true;
			}
			if (agent?.Origin != null && CommandableOriginRuntimeIds.Contains(RuntimeHelpers.GetHashCode(agent.Origin)))
			{
				return true;
			}
			if (hero != null)
			{
				return hero != Hero.MainHero && hero.PartyBelongedTo == MobileParty.MainParty && !hero.IsPrisoner && !hero.IsWounded;
			}
			Mission mission = Mission.Current;
			Team playerTeam = mission?.PlayerTeam ?? Agent.Main?.Team;
			if (agent != null && playerTeam != null && agent.Team == playerTeam)
			{
				return character.IsSoldier || IsGuardOrSoldier(character);
			}
			return (character.IsSoldier || IsGuardOrSoldier(character)) && IsMainPartyOrSelectedInterventionTroop(character);
		}
		catch
		{
			return false;
		}
	}

	private static bool TrySetPlayerFormationFollowOrder(FormationClass formationClass, string source)
	{
		try
		{
			Mission mission = Mission.Current;
			Agent main = Agent.Main ?? mission?.MainAgent;
			Team playerTeam = mission?.PlayerTeam ?? main?.Team;
			if (mission == null || main == null || !main.IsActive() || playerTeam == null)
			{
				return false;
			}
			Formation formation = playerTeam.GetFormation(formationClass);
			if (formation == null)
			{
				return false;
			}
			MarkFormationPlayerCommandable(formation, main);
			try
			{
				formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
			}
			catch
			{
			}
			formation.SetMovementOrder(MovementOrder.MovementOrderFollow(main));
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TrySetPlayerFormationFollowOrder failed (" + (source ?? "N/A") + "): " + ex.Message);
			return false;
		}
	}

	private static bool IsOffensiveInterventionOrder(OrderType orderType)
	{
		return orderType == OrderType.Charge
			|| orderType == OrderType.ChargeWithTarget
			|| orderType == OrderType.Advance
			|| orderType == OrderType.AdvanceTenPaces
			|| orderType == OrderType.AttackEntity
			|| orderType == OrderType.PointDefence
			|| orderType == OrderType.FireAtWill
			|| orderType == OrderType.AIControlOn;
	}

	internal static MBReadOnlyList<VisualOrderSet> FilterInterventionNativeVisualOrdersForExternal(MBReadOnlyList<VisualOrderSet> originalOrders)
	{
		try
		{
			if (originalOrders == null)
			{
				return originalOrders;
			}
			MBList<VisualOrderSet> filteredSets = new MBList<VisualOrderSet>();
			foreach (VisualOrderSet set in originalOrders)
			{
				if (set == null)
				{
					continue;
				}
				if (set.IsSoloOrder)
				{
					VisualOrder solo = set.SoloOrder ?? set.Orders?.FirstOrDefault();
					if (IsAllowedInterventionVisualOrder(solo))
					{
						filteredSets.Add(set);
					}
					continue;
				}
				foreach (VisualOrder order in (set.Orders?.ToList() ?? new List<VisualOrder>()))
				{
					if (!IsAllowedInterventionVisualOrder(order))
					{
						try
						{
							set.RemoveOrder(order);
						}
						catch
						{
						}
					}
				}
				if ((set.Orders?.Count ?? 0) > 0)
				{
					filteredSets.Add(set);
				}
			}
			return filteredSets;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "FilterInterventionNativeVisualOrdersForExternal failed: " + ex.Message);
			return originalOrders;
		}
	}

	private static bool IsAllowedInterventionVisualOrder(VisualOrder order)
	{
		try
		{
			if (order == null)
			{
				return false;
			}
			string id = order.StringId ?? "";
			if (id.IndexOf("charge", StringComparison.OrdinalIgnoreCase) >= 0
				|| id.IndexOf("advance", StringComparison.OrdinalIgnoreCase) >= 0
				|| id.IndexOf("attack", StringComparison.OrdinalIgnoreCase) >= 0
				|| id.IndexOf("fire", StringComparison.OrdinalIgnoreCase) >= 0
				|| id.IndexOf("ai", StringComparison.OrdinalIgnoreCase) >= 0
				|| id.IndexOf("delegate", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return false;
			}
			if (order is ChargeVisualOrder || order is AdvanceVisualOrder)
			{
				return false;
			}
			if (order is GenericToggleVisualOrder toggle)
			{
				return !IsOffensiveInterventionOrder(toggle.PositiveOrder) && !IsOffensiveInterventionOrder(toggle.NegativeOrder);
			}
			if (SingleVisualOrderOrderTypeField != null && order is SingleVisualOrder)
			{
				object value = SingleVisualOrderOrderTypeField.GetValue(order);
				if (value is OrderType orderType && IsOffensiveInterventionOrder(orderType))
				{
					return false;
				}
			}
			return true;
		}
		catch
		{
			return true;
		}
	}

	private static bool TryPrimePlayerOrderController(Mission mission, string source, bool force, bool preserveSelection = false)
	{
		try
		{
			if (mission == null || mission.Mode == MissionMode.Conversation || mission.Mode == MissionMode.Barter)
			{
				return false;
			}
			float now = mission.CurrentTime;
			if (!force && _playerOrderControllerPrimed)
			{
				return false;
			}
			if (!force && now < _nextPlayerOrderControllerPrimeTime)
			{
				return false;
			}
			_nextPlayerOrderControllerPrimeTime = now + 2.0f;
			Team playerTeam = mission.PlayerTeam ?? _interventionPlayerCommandTeam ?? Agent.Main?.Team;
			if (playerTeam == null)
			{
				return false;
			}
			int commandable = 0;
			foreach (Agent agent in mission.Agents.ToList())
			{
				if (agent != null && agent.IsHuman && agent.IsActive() && agent != Agent.Main && agent.Team == playerTeam && agent.Formation != null)
				{
					commandable++;
				}
			}
			if (commandable <= 0)
			{
				return false;
			}
			Agent main = Agent.Main ?? mission.MainAgent;
			List<Formation> commandFormations = mission.Agents
				.ToList()
				.Where(a => a != null && a.IsHuman && a.IsActive() && a != Agent.Main && a.Team == playerTeam && a.Formation != null)
				.Select(a => a.Formation)
				.Distinct()
				.ToList();
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
					try
					{
						if (playerTeam.PlayerOrderController != null)
						{
							playerTeam.PlayerOrderController.SelectFormation(formation);
						}
					}
					catch
					{
					}
				}
			}
			if (playerTeam.PlayerOrderController == null)
			{
				Logger.Log("SiegeAiIntervention", "Native player order controller not ready yet. Source=" + (source ?? "N/A") + ", Commandable=" + commandable + ", Formations=" + commandFormations.Count);
				return false;
			}
			if (shouldInitializeSelection)
			{
				try
				{
					playerTeam.PlayerOrderController?.SelectAllFormations(false);
				}
				catch
				{
				}
				try
				{
					playerTeam.MasterOrderController?.SelectAllFormations(false);
				}
				catch
				{
				}
			}
			_playerOrderControllerPrimed = true;
			Logger.Log("SiegeAiIntervention", "Primed player order controller. Source=" + (source ?? "N/A") + ", Commandable=" + commandable + ", Formations=" + commandFormations.Count + ", PreserveSelection=" + preserveSelection + ", ExistingSelection=" + hasExistingSelection + ", InitializedSelection=" + shouldInitializeSelection);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryPrimePlayerOrderController failed (" + (source ?? "N/A") + "): " + ex.Message);
			return false;
		}
	}

	internal static Team ResolveInterventionPlayerCommandTeamForExternal(Mission mission, string source = null)
	{
		try
		{
			mission ??= Mission.Current;
			if (mission == null || mission.IsMissionEnding)
			{
				return null;
			}
			EnsureInterventionPlayerCommandTeam(mission);
			Team playerTeam = mission.PlayerTeam ?? _interventionPlayerCommandTeam ?? Agent.Main?.Team ?? mission.MainAgent?.Team;
			if (playerTeam != null)
			{
				_interventionPlayerCommandTeam = playerTeam;
			}
			return playerTeam;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ResolveInterventionPlayerCommandTeamForExternal failed (" + (source ?? "N/A") + "): " + ex.Message);
			return null;
		}
	}

	internal static bool EnsureInterventionCommandUiReadyForExternal(Mission mission, string source)
	{
		try
		{
			mission ??= Mission.Current;
			if (mission == null || mission.IsMissionEnding || mission.Mode == MissionMode.Conversation || mission.Mode == MissionMode.Barter)
			{
				return false;
			}
			Team playerTeam = ResolveInterventionPlayerCommandTeamForExternal(mission, source);
			if (playerTeam == null)
			{
				return false;
			}
			Agent main = Agent.Main ?? mission.MainAgent;
			int commandable = 0;
			foreach (Agent agent in mission.Agents?.ToList() ?? new List<Agent>())
			{
				if (agent == null || !agent.IsHuman || !agent.IsActive() || agent == main || agent.Team != playerTeam || agent.Formation == null)
				{
					continue;
				}
				MarkFormationPlayerCommandable(agent.Formation, main);
				commandable++;
			}
			bool frequentOrderUiPoll = string.Equals(source, SiegeNativeBridgeSourceProfile.MissionOrderVmCheckOpenSource, StringComparison.Ordinal)
				|| string.Equals(source, SiegeNativeBridgeSourceProfile.MissionOrderVmHasTroopsSource, StringComparison.Ordinal)
				|| string.Equals(source, SiegeNativeBridgeSourceProfile.MissionOrderVmControllerSource, StringComparison.Ordinal);
			TryPrimePlayerOrderController(mission, source ?? SiegeNativeBridgeSourceProfile.OrderUiReadySource, force: !frequentOrderUiPoll, preserveSelection: true);
			return commandable > 0 && TryResolveNativeOrderControllerForExternal(mission) != null;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "EnsureInterventionCommandUiReadyForExternal failed (" + (source ?? "N/A") + "): " + ex.Message);
			return false;
		}
	}

	internal static bool InterventionPlayerHasCommandableAgentsForExternal(Mission mission)
	{
		try
		{
			mission ??= Mission.Current;
			Team playerTeam = ResolveInterventionPlayerCommandTeamForExternal(mission, SiegeNativeBridgeSourceProfile.HasCommandableAgentsSource);
			Agent main = Agent.Main ?? mission?.MainAgent;
			return mission?.Agents != null && playerTeam != null && mission.Agents.Any(a => a != null && a.IsHuman && a.IsActive() && a != main && a.Team == playerTeam && a.Formation != null);
		}
		catch
		{
			return false;
		}
	}

	private static void NotifyAgentBuiltForMission(Agent agent, Mission mission)
	{
		try
		{
			if (agent == null || mission == null)
			{
				return;
			}
			BattleAgentLogic battleAgentLogic = mission.GetMissionBehavior<BattleAgentLogic>();
			MethodInfo onAgentBuild = battleAgentLogic?.GetType()
				.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.FirstOrDefault(m => m.Name == "OnAgentBuild" && m.GetParameters().Length == 2);
			onAgentBuild?.Invoke(battleAgentLogic, new object[] { agent, null });
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "NotifyAgentBuiltForMission failed: " + ex.Message);
		}
	}

	internal static bool ShouldInjectInterventionOrderViewsForExternal(Mission mission)
	{
		try
		{
			return mission != null && !mission.IsMissionEnding && (_activeMode != InterventionMode.None || (_pendingMode != InterventionMode.None && DoesLiveCurrentSettlementMatchActiveIntervention()));
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsNativeOrderControllerReadyForExternal(Mission mission)
	{
		try
		{
			if (mission == null || mission.IsMissionEnding || mission.Mode == MissionMode.Conversation || mission.Mode == MissionMode.Barter)
			{
				return false;
			}
			OrderController orderController = TryResolveNativeOrderControllerForExternal(mission);
			if (orderController == null)
			{
				return false;
			}
			Team playerTeam = mission.PlayerTeam ?? _interventionPlayerCommandTeam ?? Agent.Main?.Team ?? mission.MainAgent?.Team;
			return mission.Agents != null && mission.Agents.Any(a => a != null && a.IsHuman && a.IsActive() && a != Agent.Main && a.Team == playerTeam && a.Formation != null);
		}
		catch
		{
			return false;
		}
	}

	internal static OrderController TryResolveNativeOrderControllerForExternal(Mission mission)
	{
		try
		{
			mission ??= Mission.Current;
			if (mission == null || mission.IsMissionEnding || mission.Mode == MissionMode.Conversation || mission.Mode == MissionMode.Barter)
			{
				return null;
			}
			if (mission.PlayerTeam == null || mission.PlayerTeam.PlayerOrderController == null)
			{
				EnsureInterventionPlayerCommandTeam(mission);
			}
			Team playerTeam = ResolveInterventionPlayerCommandTeamForExternal(mission, SiegeNativeBridgeSourceProfile.ResolveOrderControllerSource) ?? mission.PlayerTeam ?? _interventionPlayerCommandTeam ?? Agent.Main?.Team ?? mission.MainAgent?.Team;
			return playerTeam?.PlayerOrderController ?? playerTeam?.MasterOrderController;
		}
		catch
		{
			return null;
		}
	}

	internal static bool TryBindNativeOrderControllerForExternal(OrderTroopPlacer orderTroopPlacer, string source)
	{
		try
		{
			if (orderTroopPlacer == null || !IsOccupationSceneActiveForExternal())
			{
				return false;
			}
			OrderController orderController = TryResolveNativeOrderControllerForExternal(orderTroopPlacer.Mission ?? Mission.Current);
			if (orderController == null || OrderTroopPlacerOrderControllerField == null)
			{
				return false;
			}
			object current = OrderTroopPlacerOrderControllerField.GetValue(orderTroopPlacer);
			if (!ReferenceEquals(current, orderController))
			{
				OrderTroopPlacerOrderControllerField.SetValue(orderTroopPlacer, orderController);
				Logger.Log("SiegeAiIntervention", "Bound native OrderTroopPlacer order controller. Source=" + (source ?? "N/A"));
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryBindNativeOrderControllerForExternal failed (" + (source ?? "N/A") + "): " + ex.Message);
			return false;
		}
	}

	internal static bool NativeOrderControllerHasSelectedFormationsForExternal(Mission mission)
	{
		try
		{
			OrderController orderController = TryResolveNativeOrderControllerForExternal(mission);
			return orderController?.SelectedFormations != null && orderController.SelectedFormations.Count > 0;
		}
		catch
		{
			return false;
		}
	}

	private static void NeutralizeCivilianDailyUsableBehavior(Agent agent, string reason)
	{
		try
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive())
			{
				return;
			}
			try
			{
				agent.InvalidateTargetAgent();
				agent.DisableScriptedMovement();
				agent.ClearTargetFrame();
				agent.SetIsAIPaused(false);
			}
			catch
			{
			}
			TrySetAgentController(agent, "AI");
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			AgentNavigator navigator = component?.AgentNavigator;
			if (navigator == null)
			{
				return;
			}
			try
			{
				navigator.ClearTarget();
			}
			catch
			{
			}
			DailyBehaviorGroup dailyGroup = navigator.GetBehaviorGroup<DailyBehaviorGroup>();
			if (dailyGroup == null)
			{
				return;
			}
			try
			{
				dailyGroup.DisableScriptedBehavior();
			}
			catch
			{
			}
			try
			{
				WalkingBehavior walkingBehavior = dailyGroup.GetBehavior<WalkingBehavior>();
				if (walkingBehavior != null)
				{
					walkingBehavior.IsActive = false;
				}
			}
			catch
			{
			}
			try
			{
				ScriptBehavior scriptBehavior = dailyGroup.GetBehavior<ScriptBehavior>();
				if (scriptBehavior != null)
				{
					scriptBehavior.IsActive = false;
				}
			}
			catch
			{
			}
			try
			{
				FollowAgentBehavior followBehavior = dailyGroup.GetBehavior<FollowAgentBehavior>();
				if (followBehavior != null)
				{
					followBehavior.IsActive = false;
				}
			}
			catch
			{
			}
			try
			{
				dailyGroup.DisableAllBehaviors();
			}
			catch
			{
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "NeutralizeCivilianDailyUsableBehavior failed (" + (reason ?? "N/A") + "): " + ex.Message);
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

	private static void PrepareCivilianForPreMassacreHitDetection(Agent agent, Mission mission)
	{
		try
		{
			if (_massacreStarted || agent == null || mission == null || !IsMassacreTargetAgent(agent, includeHeroes: true))
			{
				return;
			}
			bool firstPrepare = CivilianPreMassacrePreparedAgentIndexes.Add(agent.Index);
			if (firstPrepare)
			{
				agent.SetMortalityState(Agent.MortalityState.Mortal);
				agent.InvalidateTargetAgent();
				agent.SetWatchState(Agent.WatchState.Patrolling);
			}
			Team playerTeam = mission.PlayerTeam ?? mission.MainAgent?.Team ?? Agent.Main?.Team;
			if (playerTeam != null && agent.Team != playerTeam)
			{
				agent.SetTeam(playerTeam, true);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "PrepareCivilianForPreMassacreHitDetection failed: " + ex.Message);
		}
	}

	private static Vec3 GetCivilianAssemblyCenter(Mission mission)
	{
		Vec3 forward = _civilianAssemblyForward;
		if (forward.LengthSquared < 0.01f)
		{
			forward = Vec3.Forward;
		}
		forward.Normalize();
		int totalSlots = Math.Max(1, GetDesiredCivilianAssemblyCount(mission));
		int rows = Math.Max(1, (int)Math.Ceiling(totalSlots / (double)Math.Max(1, CivilianAssemblyColumns)));
		float depth = Math.Max(0f, (rows - 1) * CivilianAssemblyRowSpacing);
		Vec3 center = _civilianAssemblyAnchor + forward * (depth * 0.5f);
		try
		{
			if (mission?.Scene != null)
			{
				center.z = mission.Scene.GetGroundHeightAtPosition(center);
			}
		}
		catch
		{
		}
		return center;
	}

	private static float GetCivilianAssemblyCordonRadius(Mission mission)
	{
		int totalSlots = Math.Max(1, GetDesiredCivilianAssemblyCount(mission));
		int columns = Math.Max(1, CivilianAssemblyColumns);
		int rows = Math.Max(1, (int)Math.Ceiling(totalSlots / (double)columns));
		float halfWidth = Math.Max(0f, (columns - 1) * CivilianAssemblyColumnSpacing * 0.5f);
		float halfDepth = Math.Max(0f, (rows - 1) * CivilianAssemblyRowSpacing * 0.5f);
		return Math.Max(SoldierCordonMinRadius, Math.Max(halfWidth, halfDepth) + SoldierCordonPadding);
	}

	private static Vec3 GetAlliedCordonSlotPosition(Mission mission, int slot, int count)
	{
		Vec3 center = GetCivilianAssemblyCenter(mission);
		Vec3 forward = _civilianAssemblyForward;
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
		int safeCount = Math.Max(1, count);
		float angle = (MathF.PI * 2f * Math.Max(0, slot)) / safeCount;
		float radius = GetCivilianAssemblyCordonRadius(mission);
		Vec3 position = center + right * (MathF.Cos(angle) * radius) + forward * (MathF.Sin(angle) * radius);
		try
		{
			if (mission?.Scene != null)
			{
				position.z = mission.Scene.GetGroundHeightAtPosition(position);
			}
		}
		catch
		{
		}
		return position;
	}

	private static void TryApplyNativeCircleFormationOrders(List<Agent> soldiers, Mission mission, string source)
	{
		try
		{
			if (soldiers == null || soldiers.Count == 0 || mission == null || !_civilianAssemblyPointReady)
			{
				return;
			}
			Vec3 center = GetCivilianAssemblyCenter(mission);
			float diameter = GetCivilianAssemblyCordonRadius(mission) * 2f;
			foreach (Formation formation in soldiers.Select(a => a?.Formation).Where(f => f != null).Distinct().ToList())
			{
				try
				{
					formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderCircle);
					formation.SetFormOrder(FormOrder.FormOrderCustom(diameter), true);
					formation.SetMovementOrder(MovementOrder.MovementOrderStop);
					Vec2 facing = (center.AsVec2 - formation.CurrentPosition);
					if (facing.LengthSquared < 0.01f)
					{
						facing = _civilianAssemblyForward.AsVec2;
					}
					if (facing.LengthSquared < 0.01f)
					{
						facing = Vec2.Forward;
					}
					facing = facing.Normalized();
					formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(facing));
				}
				catch (Exception ex)
				{
					Logger.Log("SiegeAiIntervention", "TryApplyNativeCircleFormationOrders formation failed (" + (source ?? "N/A") + "): " + ex.Message);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryApplyNativeCircleFormationOrders failed (" + (source ?? "N/A") + "): " + ex.Message);
		}
	}

	private static bool MoveAlliedSoldierToCordonSlot(Agent soldier, Mission mission, int slot, int count, bool force)
	{
		try
		{
			if (soldier == null || mission == null || !soldier.IsActive() || !_civilianAssemblyPointReady)
			{
				return false;
			}
			Vec3 target = GetAlliedCordonSlotPosition(mission, slot, count);
			Vec3 center = GetCivilianAssemblyCenter(mission);
			Vec3 lookDirection = center - target;
			lookDirection.z = 0f;
			if (lookDirection.LengthSquared < 0.01f)
			{
				lookDirection = -_civilianAssemblyForward;
			}
			if (lookDirection.LengthSquared < 0.01f)
			{
				lookDirection = -Vec3.Forward;
			}
			lookDirection.Normalize();
			float distSq = soldier.Position.DistanceSquared(target);
			if (force || distSq > SoldierCordonTeleportDistance * SoldierCordonTeleportDistance)
			{
				soldier.TeleportToPosition(target);
				soldier.InvalidateTargetAgent();
				distSq = 0f;
			}
			Vec2 target2 = target.AsVec2;
			float now = mission.CurrentTime;
			bool hasRecentMove = LastCordonMoveOrderTimesBySoldier.TryGetValue(soldier.Index, out float lastMoveOrderTime) && now - lastMoveOrderTime < SoldierCordonOrderRefreshSeconds;
			bool shouldIssueMove = force || distSq > SoldierCordonMoveTolerance * SoldierCordonMoveTolerance || (!hasRecentMove && distSq > SoldierCordonSettleTolerance * SoldierCordonSettleTolerance);
			if (shouldIssueMove && !force)
			{
				ClearAgentLookTarget(soldier);
				TrySetInterventionAgentTargetPosition(soldier, target, SiegeAgentWallRescueProfile.Source + ":cordon");
				LastCordonMoveOrderTimesBySoldier[soldier.Index] = now;
			}
			else if (distSq <= SoldierCordonSettleTolerance * SoldierCordonSettleTolerance)
			{
				try
				{
					soldier.ClearTargetFrame();
					LastCordonMoveOrderTimesBySoldier[soldier.Index] = now;
				}
				catch
				{
				}
			}
			bool hasRecentLook = LastCordonLookOrderTimesBySoldier.TryGetValue(soldier.Index, out float lastLookOrderTime) && now - lastLookOrderTime < SoldierCordonLookRefreshSeconds;
			if (force || (distSq <= SoldierCordonSettleTolerance * SoldierCordonSettleTolerance && !hasRecentLook))
			{
				soldier.SetLookToPointOfInterest(center);
				soldier.LookDirection = lookDirection;
				LastCordonLookOrderTimesBySoldier[soldier.Index] = now;
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "MoveAlliedSoldierToCordonSlot failed: " + ex.Message);
			return false;
		}
	}

	private static void ApplyCivilianSurrenderPose(Agent agent, Agent lookTarget, bool forceAction)
	{
		try
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive() || _massacreStarted)
			{
				return;
			}
			if (!CivilianCalmedAgentIndexes.Add(agent.Index))
			{
				return;
			}
			agent.InvalidateTargetAgent();
			try
			{
				agent.SetCrouchMode(true);
			}
			catch
			{
			}
			try
			{
				agent.SetMaximumSpeedLimit(0f, false);
			}
			catch
			{
			}
			SetCivilianLookTowardAssemblyInterior(agent, lookTarget);
			ApplyOneTimeFrightenedCivilianAction(agent, allowGathered: true);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyCivilianSurrenderPose failed: " + ex.Message);
		}
	}

	private static void SetCivilianLookTowardAssemblyInterior(Agent agent, Agent fallbackTarget)
	{
		try
		{
			if (agent == null || !agent.IsActive())
			{
				return;
			}
			Vec3 point = Vec3.Zero;
			if (fallbackTarget != null && fallbackTarget.IsActive())
			{
				point = fallbackTarget.Position;
			}
			else if (_civilianAssemblyPointReady)
			{
				point = GetCivilianAssemblyCenter(Mission.Current ?? agent.Mission);
			}
			Vec3 lookDirection = point - agent.Position;
			lookDirection.z = 0f;
			if (lookDirection.LengthSquared < 0.01f)
			{
				lookDirection = -_civilianAssemblyForward;
			}
			if (lookDirection.LengthSquared < 0.01f)
			{
				lookDirection = -Vec3.Forward;
			}
			lookDirection.Normalize();
			Vec3 lookPoint = agent.Position + lookDirection * 4f;
			agent.SetLookToPointOfInterest(lookPoint);
			agent.LookDirection = lookDirection;
		}
		catch
		{
		}
	}

	private static void ApplyFrightenedCivilianIdle(Mission mission)
	{
		if (mission?.Agents == null || _massacreStarted)
		{
			return;
		}
		foreach (Agent agent in mission.Agents.ToList())
		{
			if (!IsEligibleCivilianAgent(agent, includeHeroes: true))
			{
				continue;
			}
			if (CivilianFrightenedActionAgentIndexes.Contains(agent.Index) || CivilianGatherFollowerAgentIndexes.Contains(agent.Index) || CivilianGatherMovePreparedAgentIndexes.Contains(agent.Index))
			{
				continue;
			}
			try
			{
				PrepareCivilianForPreMassacreHitDetection(agent, mission);
				ApplyOneTimeFrightenedCivilianAction(agent);
			}
			catch
			{
			}
		}
	}

	private static void ApplyOneTimeFrightenedCivilianAction(Agent agent, bool allowGathered = false)
	{
		try
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive())
			{
				return;
			}
			if (!allowGathered && (CivilianGatherFollowerAgentIndexes.Contains(agent.Index) || CivilianGatherMovePreparedAgentIndexes.Contains(agent.Index)))
			{
				return;
			}
			if (!CivilianFrightenedActionAgentIndexes.Add(agent.Index))
			{
				return;
			}
			agent.SetActionChannel(1, ActionIndexCache.act_scared_idle_1, false, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, MBRandom.RandomFloat, false, -0.2f, 0, true);
		}
		catch
		{
		}
	}

	private static void KeepAlliedTroopsUseful(Mission mission)
	{
		Agent main = Agent.Main ?? mission?.MainAgent;
		if (mission == null || main == null)
		{
			return;
		}
		foreach (Agent agent in mission.Agents.ToList())
		{
			if (agent == null || !agent.IsActive() || !AlliedAgentIndexes.Contains(agent.Index))
			{
				continue;
			}
			try
			{
				if (_plunderStarted && !_massacreStarted && IsSoldierAssignedToPlunder(agent.Index))
				{
					CordonReadyAgentIndexes.Remove(agent.Index);
					RestoreAlliedSoldierFriendlyState(agent, 0f, SiegePlunderInteractionProfile.AlliedAssignmentRestoreSource, forceFollow: false, clearTarget: false);
					agent.SetWatchState(Agent.WatchState.Alarmed);
					continue;
				}
				if (IsInterventionBannerBearer(agent))
				{
					RestoreAlliedSoldierFriendlyState(agent, 0f, SiegeBannerBearerProfile.BannerBearerRestoreSource, forceFollow: false, clearTarget: false);
					AssignAgentToPlayerFormation(agent, GetBannerBearerFormationClass(), refreshFormationOrders: false);
					RestoreBannerBearerMountTeam(agent, main, mission);
					TryWieldBannerBearerBanner(agent);
					agent.SetWatchState(_massacreStarted || _plunderStarted ? Agent.WatchState.Alarmed : Agent.WatchState.Patrolling);
					continue;
				}
				RestoreAlliedSoldierFriendlyState(agent, 0f, SiegeSoldierCordonProfile.AlliedControlTickSource, forceFollow: false, clearTarget: false);
				if (agent.Formation == null || agent.Team != (mission.PlayerTeam ?? main.Team))
				{
					AssignAgentToPlayerFormation(agent, FormationClass.Infantry);
				}
				if (_massacreStarted)
				{
					CordonReadyAgentIndexes.Remove(agent.Index);
					DisableCompanionStyleFollow(agent);
					agent.SetWatchState(Agent.WatchState.Alarmed);
				}
				else if (CivilianGatherMessengerAgentIndexes.Contains(agent.Index))
				{
					DisableCompanionStyleFollow(agent);
					agent.SetWatchState(Agent.WatchState.Patrolling);
					if (!IsCivilianGatherMessengerBusy(agent.Index))
					{
						agent.ClearTargetFrame();
						agent.InvalidateTargetAgent();
						agent.SetMaximumSpeedLimit(-1f, false);
					}
				}
				else if (_plunderStarted)
				{
					KeepPlunderGuardSoldierNearPlayer(agent, main, mission);
				}
				else
				{
					if (CordonReadyAgentIndexes.Add(agent.Index))
					{
						DisableCompanionStyleFollow(agent);
						AssignAgentToPlayerFormation(agent, FormationClass.Infantry);
						if (!_soldierDefaultFollowOrderIssued)
						{
							_soldierDefaultFollowOrderIssued = TrySetPlayerFormationFollowOrder(FormationClass.Infantry, SiegeSoldierCordonProfile.AlliedDefaultFollowSource);
						}
						agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.Any);
					}
					agent.SetWatchState(_plunderStarted ? Agent.WatchState.Alarmed : Agent.WatchState.Patrolling);
				}
			}
			catch
			{
			}
		}
	}

	private static void KeepPlunderGuardSoldierNearPlayer(Agent soldier, Agent main, Mission mission)
	{
		try
		{
			if (soldier == null || main == null || mission == null || !soldier.IsActive())
			{
				return;
			}
			bool firstGuardTick = CordonReadyAgentIndexes.Add(soldier.Index);
			AssignAgentToPlayerFormation(soldier, FormationClass.Infantry, refreshFormationOrders: firstGuardTick);
			if (!_soldierDefaultFollowOrderIssued)
			{
				_soldierDefaultFollowOrderIssued = TrySetPlayerFormationFollowOrder(FormationClass.Infantry, SiegePlunderInteractionProfile.GuardFollowSource);
			}
			if (firstGuardTick)
			{
				soldier.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.Any);
			}
			MoveAlliedSoldierNearMainFallback(soldier, main);
			soldier.InvalidateTargetAgent();
			ClearAgentLookTarget(soldier);
			soldier.SetWatchState(Agent.WatchState.Alarmed);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "KeepPlunderGuardSoldierNearPlayer failed: " + ex.Message);
		}
	}

	private static void TryKeepAlliedSoldierFollowingInOccupation(Agent soldier, Agent main, Mission mission)
	{
		try
		{
			if (soldier == null || main == null || mission == null || !soldier.IsActive())
			{
				return;
			}
			float now = mission.CurrentTime;
			if (!LastMassacreSoldierFollowOrderTimes.TryGetValue(soldier.Index, out float last) || now - last >= MassacreSoldierFollowRefreshSeconds)
			{
				LastMassacreSoldierFollowOrderTimes[soldier.Index] = now;
				if (!TryApplyCompanionStyleFollow(soldier, main, SiegeMassacreInteractionProfile.OccupationFollowSource))
				{
					MoveAlliedSoldierNearMainFallback(soldier, main);
				}
			}
			try
			{
				soldier.InvalidateTargetAgent();
				if (soldier.Position.DistanceSquared(main.Position) <= 3.5f * 3.5f)
				{
					soldier.SetLookAgent(main);
				}
				else
				{
					ClearAgentLookTarget(soldier);
				}
				soldier.SetWatchState(Agent.WatchState.Alarmed);
			}
			catch
			{
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryKeepAlliedSoldierFollowingInOccupation failed: " + ex.Message);
		}
	}

	private static void TryAutoPlunderOneNearbyCivilian(Mission mission)
	{
		try
		{
			UpdatePlunderInteractions(mission);
			if (mission?.Agents == null)
			{
				return;
			}
			int desiredConcurrent = GetDesiredPlunderInteractionLimit(mission);
			if (desiredConcurrent <= 0)
			{
				return;
			}
			int guard = 0;
			while (ActivePlunderInteractions.Count < desiredConcurrent && guard++ < desiredConcurrent * 2)
			{
				HashSet<int> activeTargets = new HashSet<int>(ActivePlunderInteractions.Keys);
				Agent target = mission.Agents
					.Where(a => IsEligibleCivilianAgent(a, includeHeroes: true) && !LootedTargets.Contains(BuildTargetKey(a)) && !activeTargets.Contains(a.Index))
					.OrderBy(a => CountNearbyAssignedPlunderSoldiers(a, mission))
					.ThenBy(a => Agent.Main != null ? a.Position.DistanceSquared(Agent.Main.Position) : 0f)
					.FirstOrDefault();
				if (target == null || !AssignSoldierPlunderInteraction(mission, target))
				{
					break;
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryAutoPlunderOneNearbyCivilian failed: " + ex.Message);
		}
	}

	private static int GetDesiredPlunderInteractionLimit(Mission mission)
	{
		try
		{
			if (mission?.Agents == null)
			{
				return 0;
			}
			int activeSoldiers = mission.Agents.Count(a => IsCommandableInterventionSoldier(a, requireActive: true));
			int remainingTargets = mission.Agents.Count(a => IsEligibleCivilianAgent(a, includeHeroes: true) && !LootedTargets.Contains(BuildTargetKey(a)) && !ActivePlunderInteractions.ContainsKey(a.Index));
			int byRatio = (int)MathF.Ceiling(activeSoldiers * PlunderSoldierAssignmentRatio);
			int desired = Math.Min(MaxConcurrentPlunderInteractions, Math.Max(1, byRatio));
			return Math.Max(0, Math.Min(remainingTargets + ActivePlunderInteractions.Count, desired));
		}
		catch
		{
			return 0;
		}
	}

	private static int CountNearbyAssignedPlunderSoldiers(Agent target, Mission mission)
	{
		try
		{
			if (target == null || mission?.Agents == null)
			{
				return 0;
			}
			int count = 0;
			foreach (PlunderInteraction interaction in ActivePlunderInteractions.Values)
			{
				Agent soldier = mission.Agents.FirstOrDefault(a => a != null && a.Index == interaction.SoldierAgentIndex);
				if (soldier != null && soldier.IsActive() && soldier.Position.DistanceSquared(target.Position) <= 7f * 7f)
				{
					count++;
				}
			}
			return count;
		}
		catch
		{
			return 0;
		}
	}

	private static void UpdatePlunderInteractions(Mission mission)
	{
		if (mission?.Agents == null || ActivePlunderInteractions.Count == 0)
		{
			return;
		}
		float now = mission.CurrentTime;
		foreach (PlunderInteraction interaction in ActivePlunderInteractions.Values.ToList())
		{
			try
			{
				Agent target = mission.Agents.FirstOrDefault(a => a != null && a.Index == interaction.TargetAgentIndex);
				Agent soldier = mission.Agents.FirstOrDefault(a => a != null && a.Index == interaction.SoldierAgentIndex);
				if (target == null || soldier == null || !target.IsActive() || !soldier.IsActive() || LootedTargets.Contains(BuildTargetKey(target)) || !IsEligibleCivilianAgent(target, includeHeroes: true))
				{
					ActivePlunderInteractions.Remove(interaction.TargetAgentIndex);
					continue;
				}
				MoveSoldierTowardPlunderTarget(soldier, target);
				float distanceSq = soldier.Position.DistanceSquared(target.Position);
				if (distanceSq <= PlunderApproachDistance * PlunderApproachDistance)
				{
					soldier.SetLookAgent(target);
					target.SetLookAgent(soldier);
					if (interaction.TalkStartedAt < 0f)
					{
						interaction.TalkStartedAt = now;
						try
						{
							target.SetActionChannel(1, ActionIndexCache.act_scared_idle_1, false, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, MBRandom.RandomFloat, false, -0.2f, 0, true);
						}
						catch
						{
						}
					}
					if (now - interaction.TalkStartedAt >= PlunderTalkSeconds)
					{
						TryLootCivilianAgent(target, massacre: false, force: false, actorName: soldier.Name?.ToString());
						ActivePlunderInteractions.Remove(interaction.TargetAgentIndex);
					}
				}
				else if (now - interaction.StartedAt > 18f)
				{
					ActivePlunderInteractions.Remove(interaction.TargetAgentIndex);
				}
			}
			catch
			{
				ActivePlunderInteractions.Remove(interaction.TargetAgentIndex);
			}
		}
	}

	private static bool AssignSoldierPlunderInteraction(Mission mission, Agent target)
	{
		if (mission?.Agents == null || target == null || ActivePlunderInteractions.ContainsKey(target.Index))
		{
			return false;
		}
		HashSet<int> busySoldiers = new HashSet<int>(ActivePlunderInteractions.Values.Select(x => x.SoldierAgentIndex));
		Agent soldier = mission.Agents.Where(a => IsCommandableInterventionSoldier(a, requireActive: true) && !busySoldiers.Contains(a.Index)).OrderBy(a => a.Position.DistanceSquared(target.Position)).FirstOrDefault();
		if (soldier == null)
		{
			return false;
		}
		ActivePlunderInteractions[target.Index] = new PlunderInteraction
		{
			TargetAgentIndex = target.Index,
			SoldierAgentIndex = soldier.Index,
			StartedAt = mission.CurrentTime
		};
		DisableCompanionStyleFollow(soldier);
		MoveSoldierTowardPlunderTarget(soldier, target);
		return true;
	}

	private static bool IsSoldierAssignedToPlunder(int soldierIndex)
	{
		return ActivePlunderInteractions.Values.Any(x => x.SoldierAgentIndex == soldierIndex);
	}

	private static void MoveSoldierTowardPlunderTarget(Agent soldier, Agent target)
	{
		try
		{
			if (soldier == null || target == null || Mission.Current == null)
			{
				return;
			}
			soldier.SetWatchState(Agent.WatchState.Alarmed);
			TryApplyAgentFollowTarget(soldier, target, SiegePlunderInteractionProfile.TargetFollowSource, lookAtTarget: false);
		}
		catch
		{
		}
	}

	private static void AutoLootRemainingVisibleCiviliansForPlunder()
	{
		try
		{
			Mission mission = Mission.Current;
			if (mission?.Agents == null)
			{
				return;
			}
			TrackSceneCivilianAgents(mission);
			int count = 0;
			int goldBefore = _lastCivilianGoldLoot;
			foreach (int agentIndex in SceneCivilianAgentIndexes.ToList())
			{
				Agent agent = mission.Agents.FirstOrDefault(x => x != null && x.Index == agentIndex);
				if (agent != null && TryLootCivilianAgent(agent, massacre: false, force: false))
				{
					count++;
				}
			}
			if (count > 0)
			{
				int gainedGold = Math.Max(0, _lastCivilianGoldLoot - goldBefore);
				InformationManager.DisplayMessage(new InformationMessage(SiegeLootAccountingProfile.BuildCivilianExitSettlementMessage(SceneCivilianAgentIndexes.Count, count, gainedGold), Color.FromUint(SiegeLootAccountingProfile.LootMessageColor)));
			}
		}
		catch
		{
		}
	}

	private static bool TryLootCivilianAgent(Agent agent, bool massacre, bool force, string actorName = null)
	{
		bool eligibleCivilian = IsEligibleCivilianAgent(agent, includeHeroes: true);
		bool eligibleMassacreTarget = massacre && IsMassacreTargetAgent(agent, includeHeroes: true);
		if (!eligibleCivilian && !eligibleMassacreTarget)
		{
			return false;
		}
		string targetKey = BuildTargetKey(agent);
		if (!force && !LootedTargets.Add(targetKey))
		{
			return false;
		}
		if (force)
		{
			LootedTargets.Add(targetKey);
		}
		CharacterObject character = agent.Character as CharacterObject;
		Hero hero = character?.HeroObject;
		int amount;
		if (hero != null && hero != Hero.MainHero)
		{
			if (massacre)
			{
				int baseAmount = hero.Gold > 0 ? RandomPercent(hero.Gold, 0.90f, 1.00f) : HeroMassacreFallbackGold;
				amount = hero.IsNotable ? Math.Max(baseAmount, Math.Min(HeroMassacreFallbackGold, Math.Max(baseAmount, HeroMassacreFallbackGold))) : baseAmount;
			}
			else
			{
				amount = hero.Gold > 0 ? RandomPercent(hero.Gold, 0.50f, 0.75f) : MBRandom.RandomInt(300, 751);
			}
			if (hero.Gold > 0)
			{
				amount = Math.Min(amount, hero.Gold);
				if (amount > 0)
				{
					GiveGoldAction.ApplyBetweenCharacters(hero, Hero.MainHero, amount, disableNotification: true);
				}
			}
			else if (amount > 0)
			{
				AwardGoldToPlayer(amount, SiegeLootAccountingProfile.CivilianHeroFallbackGoldSource);
			}
		}
		else
		{
			amount = massacre ? NonHeroMassacreGold : MBRandom.RandomInt(NonHeroPlunderMinGold, NonHeroPlunderMaxGold + 1);
			AwardGoldToPlayer(amount, SiegeLootAccountingProfile.CivilianFlatGoldSource);
		}
		if (amount > 0)
		{
			_lastCivilianGoldLoot += amount;
			_lastCivilianTargetsLooted++;
			string targetName = agent.Name?.ToString();
			InformationManager.DisplayMessage(new InformationMessage(SiegeLootAccountingProfile.BuildCivilianLootMessage(actorName, targetName, amount), Color.FromUint(SiegeLootAccountingProfile.LootMessageColor)));
			return true;
		}
		return false;
	}

	private static int RandomPercent(int value, float minRatio, float maxRatio)
	{
		float ratio = minRatio + MBRandom.RandomFloat * Math.Max(0f, maxRatio - minRatio);
		return Math.Max(0, (int)MathF.Round(value * ratio));
	}

	private static void PrepareCivilianForMassacreCombat(Agent agent, Mission mission)
	{
		try
		{
			if (agent == null || mission == null || !agent.IsHuman || AlliedAgentIndexes.Contains(agent.Index))
			{
				return;
			}
			Agent main = Agent.Main ?? mission.MainAgent;
			Team playerTeam = mission.PlayerTeam ?? main?.Team;
			Team enemyTeam = EnsureInterventionCivilianEnemyTeam(mission) ?? mission.PlayerEnemyTeam ?? agent.Team;
			CharacterObject character = agent.Character as CharacterObject;
			bool canResist = ShouldCivilianResistMassacre(agent);
			NeutralizeCivilianDailyUsableBehavior(agent, SiegeMassacreInteractionProfile.CombatPrepareSource);
			agent.SetMortalityState(Agent.MortalityState.Mortal);
			if (enemyTeam != null && agent.Team != enemyTeam)
			{
				agent.SetTeam(enemyTeam, true);
			}
			if (agent.Team != null && playerTeam != null && agent.Team != playerTeam)
			{
				agent.Team.SetIsEnemyOf(playerTeam, isEnemyOf: true);
				playerTeam.SetIsEnemyOf(agent.Team, isEnemyOf: true);
			}
			try
			{
				agent.SetCrouchMode(false);
				agent.SetMaximumSpeedLimit(-1f, false);
			}
			catch
			{
			}
			agent.SetWatchState(Agent.WatchState.Alarmed);
			if (canResist)
			{
				LocalFleeingCivilianAgentIndexes.Remove(agent.Index);
				try
				{
					Formation enemyFormation = enemyTeam?.GetFormation(FormationClass.Infantry);
					if (enemyFormation != null && agent.Formation != enemyFormation)
					{
						agent.Formation = enemyFormation;
						agent.TryAttachToFormation();
						agent.SetShouldCatchUpWithFormation(true);
						agent.UpdateFormationOrders();
					}
				}
				catch
				{
				}
				if (MassacreCombatPreparedAgentIndexes.Add(agent.Index))
				{
					try
					{
						agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.Any);
					}
					catch
					{
					}
				}
				ForceAgentForMassacreFight(agent);
			}
			else
			{
				LocalHostileCivilianAgentIndexes.Remove(agent.Index);
				LocalFleeingCivilianAgentIndexes.Add(agent.Index);
				agent.InvalidateTargetAgent();
				ClearAgentLookTarget(agent);
				try
				{
					agent.DisableScriptedMovement();
					agent.ClearTargetFrame();
					if (agent.Formation != null)
					{
						agent.Formation = null;
					}
					agent.SetShouldCatchUpWithFormation(false);
				}
				catch
				{
				}
				ActivateCivilianPanicFleeBehavior(agent, SiegeMassacreInteractionProfile.CivilianPanicRoutSource);
				KeepCivilianHidingFromOccupation(agent, mission, main, force: false);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "PrepareCivilianForMassacreCombat failed: " + ex.Message);
		}
	}

	private static bool IsAgentUsingAnyRealWeapon(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive())
			{
				return false;
			}
			EquipmentIndex primary = agent.GetPrimaryWieldedItemIndex();
			if (IsRealWeaponWieldedSlot(agent, primary))
			{
				return true;
			}
			EquipmentIndex offhand = agent.GetOffhandWieldedItemIndex();
			return IsRealWeaponWieldedSlot(agent, offhand);
		}
		catch
		{
			return true;
		}
	}

	private static bool IsRealWeaponWieldedSlot(Agent agent, EquipmentIndex equipmentIndex)
	{
		try
		{
			if (agent == null || equipmentIndex == EquipmentIndex.None || equipmentIndex < EquipmentIndex.WeaponItemBeginSlot || equipmentIndex >= EquipmentIndex.NumAllWeaponSlots)
			{
				return false;
			}
			MissionWeapon missionWeapon = agent.Equipment[equipmentIndex];
			if (missionWeapon.IsEmpty)
			{
				return false;
			}
			WeaponComponentData usage = missionWeapon.CurrentUsageItem;
			return usage != null && !usage.IsShield && usage.WeaponClass != WeaponClass.Undefined;
		}
		catch
		{
			return false;
		}
	}

	private static void DriveMassacreCombatState(Mission mission)
	{
		Agent main = Agent.Main ?? mission?.MainAgent;
		if (mission == null || main == null)
		{
			return;
		}
		if (_massacreVictoryReached)
		{
			KeepAlliedVictoryCheer(mission);
			return;
		}
		List<Agent> massacreTargets = mission.Agents.Where(a => IsMassacreTargetAgent(a, includeHeroes: true) && !CountedMassacreVictims.Contains(a.Index)).ToList();
		foreach (Agent target in massacreTargets)
		{
			PrepareCivilianForMassacreCombat(target, mission);
		}
		List<Agent> alliedSoldiers = mission.Agents
			.Where(a => IsCommandableInterventionSoldier(a, requireActive: true))
			.OrderBy(a => MassacreSoldierTargetAgentIndexes.ContainsKey(a.Index) ? 0 : 1)
			.ThenBy(a => a.Index)
			.ToList();
		PruneMassacreHuntAssignments(alliedSoldiers, massacreTargets);
		Dictionary<int, int> targetHunterCounts = new Dictionary<int, int>();
		int activeHunterLimit = SiegeMassacreInteractionProfile.CalculateActiveHunterLimit(alliedSoldiers.Count);
		int activeHunters = 0;
		foreach (Agent allied in alliedSoldiers)
		{
			try
			{
				RestoreAlliedSoldierFriendlyState(allied, 0f, SiegeMassacreInteractionProfile.AlliedCombatDriveSource, forceFollow: false, clearTarget: false);
				DisableCompanionStyleFollow(allied);
				ForceAgentForMassacreFight(allied);
				allied.SetWatchState(Agent.WatchState.Alarmed);
				if (MassacreReadySoldierAgentIndexes.Add(allied.Index))
				{
					try
					{
						allied.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.Any);
					}
					catch
					{
					}
				}
				Agent target = activeHunters < activeHunterLimit
					? SelectMassacreTargetForSoldier(allied, massacreTargets, targetHunterCounts, mission)
					: null;
				if (target != null)
				{
					activeHunters++;
					GuideSoldierTowardMassacreTarget(allied, target, mission);
				}
				else
				{
					KeepMassacreReserveSoldierNearPlayer(allied, main, mission);
				}
			}
			catch (Exception ex)
			{
				Logger.Log("SiegeAiIntervention", "DriveMassacreCombatState soldier failed: " + ex.Message);
			}
		}
	}

	private static Agent SelectMassacreTargetForSoldier(Agent soldier, List<Agent> massacreTargets, Dictionary<int, int> targetHunterCounts, Mission mission)
	{
		try
		{
			if (soldier == null || massacreTargets == null || massacreTargets.Count == 0 || targetHunterCounts == null)
			{
				ClearMassacreHuntAssignment(soldier?.Index ?? -1);
				return null;
			}
			int avoidTargetIndex = -1;
			if (MassacreSoldierTargetAgentIndexes.TryGetValue(soldier.Index, out int assignedTargetIndex))
			{
				Agent assignedTarget = massacreTargets.FirstOrDefault(t => t != null && t.Index == assignedTargetIndex && t.IsActive() && !CountedMassacreVictims.Contains(t.Index));
				int assignedCount = assignedTarget != null && targetHunterCounts.TryGetValue(assignedTarget.Index, out int currentAssignedCount) ? currentAssignedCount : 0;
				if (assignedTarget != null && assignedCount < MassacreMaxHuntersPerTarget && !ShouldReassignMassacreSoldier(soldier, assignedTarget, mission))
				{
					targetHunterCounts[assignedTarget.Index] = assignedCount + 1;
					MassacreSoldierTargetSlots[soldier.Index] = assignedCount;
					return assignedTarget;
				}
				avoidTargetIndex = assignedTargetIndex;
				ClearMassacreHuntAssignment(soldier.Index);
			}
			Agent target = massacreTargets
				.Where(t => t != null && t.IsActive() && !CountedMassacreVictims.Contains(t.Index) && t.Index != avoidTargetIndex)
				.Where(t => !targetHunterCounts.TryGetValue(t.Index, out int count) || count < MassacreMaxHuntersPerTarget)
				.OrderBy(t => t.Position.DistanceSquared(soldier.Position))
				.FirstOrDefault();
			if (target == null)
			{
				return null;
			}
			int slot = targetHunterCounts.TryGetValue(target.Index, out int targetCount) ? targetCount : 0;
			targetHunterCounts[target.Index] = slot + 1;
			MassacreSoldierTargetAgentIndexes[soldier.Index] = target.Index;
			MassacreSoldierTargetSlots[soldier.Index] = slot;
			LastMassacreSoldierProbePositions[soldier.Index] = soldier.Position;
			LastMassacreSoldierProbeTimes[soldier.Index] = mission?.CurrentTime ?? 0f;
			return target;
		}
		catch
		{
			return null;
		}
	}

	private static Agent SelectMassacreTargetForSoldier(Agent soldier, Mission mission)
	{
		try
		{
			if (mission?.Agents == null)
			{
				return null;
			}
			var counts = new Dictionary<int, int>();
			return SelectMassacreTargetForSoldier(soldier, mission.Agents.Where(a => IsMassacreTargetAgent(a, includeHeroes: true) && !CountedMassacreVictims.Contains(a.Index)).ToList(), counts, mission);
		}
		catch
		{
			return null;
		}
	}

	private static void GuideSoldierTowardMassacreTarget(Agent soldier, Agent target, Mission mission)
	{
		try
		{
			if (soldier == null || target == null || mission == null || !soldier.IsActive() || !target.IsActive())
			{
				return;
			}
			float now = mission.CurrentTime;
			if (LastMassacreSoldierTargetOrderTimes.TryGetValue(soldier.Index, out float last) && now - last < MassacreSoldierTargetRefreshSeconds)
			{
				return;
			}
			LastMassacreSoldierTargetOrderTimes[soldier.Index] = now;
			soldier.SetLookAgent(target);
			float closeDistance = MassacreTargetApproachRadius + 0.75f;
			if (soldier.Position.DistanceSquared(target.Position) <= closeDistance * closeDistance)
			{
				return;
			}
			try
			{
				int slot = MassacreSoldierTargetSlots.TryGetValue(soldier.Index, out int assignedSlot) ? assignedSlot : 0;
				Vec3 approachPoint = BuildMassacreTargetApproachPoint(soldier, target, mission, slot);
				SetAgentLookTowardPoint(soldier, approachPoint);
				TrySetInterventionAgentTargetPosition(soldier, approachPoint, SiegeAgentWallRescueProfile.Source + ":massacre_target");
			}
			catch
			{
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "GuideSoldierTowardMassacreTarget failed: " + ex.Message);
		}
	}

	private static void PruneMassacreHuntAssignments(List<Agent> alliedSoldiers, List<Agent> massacreTargets)
	{
		try
		{
			var activeSoldiers = new HashSet<int>((alliedSoldiers ?? new List<Agent>()).Where(a => a != null && a.IsActive()).Select(a => a.Index));
			var activeTargets = new HashSet<int>((massacreTargets ?? new List<Agent>()).Where(a => a != null && a.IsActive() && !CountedMassacreVictims.Contains(a.Index)).Select(a => a.Index));
			foreach (int soldierIndex in MassacreSoldierTargetAgentIndexes.Keys.ToList())
			{
				if (!activeSoldiers.Contains(soldierIndex) || !activeTargets.Contains(MassacreSoldierTargetAgentIndexes[soldierIndex]))
				{
					ClearMassacreHuntAssignment(soldierIndex);
				}
			}
		}
		catch
		{
		}
	}

	private static void ClearMassacreHuntAssignment(int soldierIndex)
	{
		if (soldierIndex < 0)
		{
			return;
		}
		MassacreSoldierTargetAgentIndexes.Remove(soldierIndex);
		MassacreSoldierTargetSlots.Remove(soldierIndex);
		LastMassacreSoldierTargetOrderTimes.Remove(soldierIndex);
		LastMassacreSoldierProbePositions.Remove(soldierIndex);
		LastMassacreSoldierProbeTimes.Remove(soldierIndex);
	}

	private static bool ShouldReassignMassacreSoldier(Agent soldier, Agent target, Mission mission)
	{
		try
		{
			if (soldier == null || target == null || mission == null || !soldier.IsActive() || !target.IsActive())
			{
				return true;
			}
			float targetDistanceSq = soldier.Position.DistanceSquared(target.Position);
			float targetMinDistanceSq = MassacreSoldierStuckTargetMinDistance * MassacreSoldierStuckTargetMinDistance;
			float now = mission.CurrentTime;
			if (targetDistanceSq <= targetMinDistanceSq)
			{
				LastMassacreSoldierProbePositions[soldier.Index] = soldier.Position;
				LastMassacreSoldierProbeTimes[soldier.Index] = now;
				return false;
			}
			if (!LastMassacreSoldierProbeTimes.TryGetValue(soldier.Index, out float lastProbeTime))
			{
				LastMassacreSoldierProbePositions[soldier.Index] = soldier.Position;
				LastMassacreSoldierProbeTimes[soldier.Index] = now;
				return false;
			}
			if (now - lastProbeTime < MassacreSoldierStuckReassignSeconds)
			{
				return false;
			}
			Vec3 lastPosition = LastMassacreSoldierProbePositions.TryGetValue(soldier.Index, out Vec3 storedPosition) ? storedPosition : soldier.Position;
			LastMassacreSoldierProbePositions[soldier.Index] = soldier.Position;
			LastMassacreSoldierProbeTimes[soldier.Index] = now;
			float movedSq = soldier.Position.DistanceSquared(lastPosition);
			float minMovedSq = MassacreSoldierStuckMinMovedDistance * MassacreSoldierStuckMinMovedDistance;
			return movedSq < minMovedSq;
		}
		catch
		{
			return false;
		}
	}

	private static Vec3 BuildMassacreTargetApproachPoint(Agent soldier, Agent target, Mission mission, int slot)
	{
		Vec3 direction = soldier.Position - target.Position;
		direction.z = 0f;
		if (direction.LengthSquared < 0.01f)
		{
			float baseAngle = ((Math.Abs((soldier?.Index ?? 0) + (target?.Index ?? 0)) % 16) / 16f) * MathF.PI * 2f;
			direction = Vec3.Zero;
			direction.x = MathF.Cos(baseAngle);
			direction.y = MathF.Sin(baseAngle);
		}
		if (direction.LengthSquared < 0.01f)
		{
			direction = Vec3.Forward;
		}
		direction.Normalize();
		float spread = (Math.Abs(slot) % 2 == 0 ? -0.6f : 0.6f) + (Math.Abs(slot) / 2) * 0.35f;
		Vec3 approachDirection = RotateFlatDirection(direction, spread);
		Vec3 point = target.Position + approachDirection * MassacreTargetApproachRadius;
		return ProjectCivilianRoutPointToGround(mission, point);
	}

	private static Vec3 RotateFlatDirection(Vec3 direction, float radians)
	{
		float cos = MathF.Cos(radians);
		float sin = MathF.Sin(radians);
		Vec3 rotated = direction;
		rotated.x = direction.x * cos - direction.y * sin;
		rotated.y = direction.x * sin + direction.y * cos;
		rotated.z = 0f;
		if (rotated.LengthSquared < 0.01f)
		{
			return direction;
		}
		rotated.Normalize();
		return rotated;
	}

	private static void KeepMassacreReserveSoldierNearPlayer(Agent soldier, Agent main, Mission mission)
	{
		try
		{
			if (soldier == null || main == null || mission == null || !soldier.IsActive())
			{
				return;
			}
			ClearMassacreHuntAssignment(soldier.Index);
			MoveAlliedSoldierNearMainFallback(soldier, main);
			soldier.InvalidateTargetAgent();
			ClearAgentLookTarget(soldier);
			soldier.SetWatchState(Agent.WatchState.Alarmed);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "KeepMassacreReserveSoldierNearPlayer failed: " + ex.Message);
		}
	}

	private static void ForceAgentForMassacreFight(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive())
			{
				return;
			}
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			AgentNavigator navigator = component?.AgentNavigator ?? component?.CreateAgentNavigator();
			AlarmedBehaviorGroup behaviorGroup = navigator?.GetBehaviorGroup<AlarmedBehaviorGroup>();
			if (behaviorGroup != null)
			{
				behaviorGroup.DisableCalmDown = true;
				behaviorGroup.AddBehavior<FightBehavior>();
				behaviorGroup.SetScriptedBehavior<FightBehavior>();
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ForceAgentForMassacreFight failed: " + ex.Message);
		}
	}

	private static bool DoesAgentCarryRealWeapon(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive())
			{
				return false;
			}
			for (EquipmentIndex slot = EquipmentIndex.WeaponItemBeginSlot; slot < EquipmentIndex.NumAllWeaponSlots; slot++)
			{
				if (IsRealWeaponWieldedSlot(agent, slot))
				{
					return true;
				}
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	private static bool ShouldCivilianResistMassacre(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsHuman || AlliedAgentIndexes.Contains(agent.Index))
			{
				return false;
			}
			if (!IsMassacreTargetAgent(agent, includeHeroes: true))
			{
				return false;
			}
			CharacterObject character = agent.Character as CharacterObject;
			return SiegeMassacreInteractionProfile.ShouldCivilianResist(
				Math.Abs(agent.Index),
				IsInterventionNotableHero(character?.HeroObject),
				DoesAgentCarryRealWeapon(agent),
				IsGuardOrSoldier(character));
		}
		catch
		{
			return false;
		}
	}

	private static Vec3 ResolveCivilianEscapeOrHideTarget(Agent civilian, Mission mission, Agent main)
	{
		Vec3 origin = civilian?.Position ?? Vec3.Zero;
		EnsureCivilianRoutPointPools(mission, main);
		List<Vec3> preferredPool = ShouldCivilianEscapeSettlement(civilian) ? CivilianEscapePointPool : CivilianInteriorHidePointPool;
		List<Vec3> fallbackPool = ShouldCivilianEscapeSettlement(civilian) ? CivilianInteriorHidePointPool : CivilianEscapePointPool;
		List<Vec3> pool = preferredPool.Count > 0 ? preferredPool : fallbackPool;
		if (pool.Count > 0)
		{
			int index = Math.Abs(civilian?.Index ?? 0) % pool.Count;
			return pool[index];
		}
		Vec3 mainPosition = main?.Position ?? origin;
		Vec3 away = origin - mainPosition;
		away.z = 0f;
		if (away.LengthSquared < 0.01f)
		{
			away = -(main?.LookDirection ?? Vec3.Forward);
			away.z = 0f;
		}
		if (away.LengthSquared < 0.01f)
		{
			away = Vec3.Forward;
		}
		away.Normalize();
		return ProjectCivilianRoutPointToGround(mission, origin + away * MassacreCivilianHideDistance);
	}

	private static bool ShouldCivilianEscapeSettlement(Agent civilian)
	{
		try
		{
			return civilian != null && civilian.Index >= 0 && civilian.Index % 3 == 0;
		}
		catch
		{
			return false;
		}
	}

	private static void EnsureCivilianRoutPointPools(Mission mission, Agent main)
	{
		try
		{
			string sceneName = mission?.SceneName ?? "";
			if (_civilianRoutPointPoolSceneName == sceneName && (CivilianInteriorHidePointPool.Count > 0 || CivilianEscapePointPool.Count > 0))
			{
				return;
			}
			CivilianInteriorHidePointPool.Clear();
			CivilianEscapePointPool.Clear();
			_civilianRoutPointPoolSceneName = sceneName;
			Vec3 anchor = main?.Position ?? GetCivilianAssemblyCenter(mission);
			Vec3 forward = main?.LookDirection ?? _civilianAssemblyForward;
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
			AddTaggedCivilianRoutPoints(mission, CivilianInteriorHidePointPool, anchor, outwardDistance: 0f,
				"sp_merchant", "sp_blacksmith", "sp_armorer", "sp_weaponsmith", "sp_horse_merchant", "sp_horse_trader",
				"sp_shop_worker", "sp_workshop_worker", "sp_tavernkeeper", "sp_tavern_wench", "sp_barber",
				"sp_guard", "sp_guard_castle", "sp_prison_guard", "sp_lord_hall_guard", "sp_player_conversation",
				"binary_conversation_point", "center_conversation_point", "sp_notables_parent");
			AddTaggedCivilianRoutPoints(mission, CivilianEscapePointPool, anchor, outwardDistance: 34f,
				"sp_outside_near_town_main_gate", "sp_player_near_town_main_gate", "sp_player_near_town_gate",
				"sp_player_near_castle_gate", "sp_player_near_gate", "sp_castle_gate", "main_gate", "town_gate",
				"castle_gate", "gate", "spawnpoint_player_outside", "spawnpoint_player", "sp_player_outside");
			Vec3[] fixedInteriorDirections =
			{
				forward,
				-forward,
				right,
				-right,
				(forward + right),
				(forward - right),
				(-forward + right),
				(-forward - right)
			};
			for (int i = 0; i < fixedInteriorDirections.Length; i++)
			{
				Vec3 direction = fixedInteriorDirections[i];
				direction.z = 0f;
				if (direction.LengthSquared < 0.01f)
				{
					continue;
				}
				direction.Normalize();
				float distance = SiegeMassacreInteractionProfile.GetInteriorRoutDistance(i);
				AddCivilianRoutPointUnique(CivilianInteriorHidePointPool, ProjectCivilianRoutPointToGround(mission, anchor + direction * distance));
			}
			Vec3[] fixedEscapeDirections =
			{
				forward,
				-forward,
				right,
				-right,
				(forward + right),
				(forward - right)
			};
			for (int i = 0; i < fixedEscapeDirections.Length; i++)
			{
				Vec3 direction = fixedEscapeDirections[i];
				direction.z = 0f;
				if (direction.LengthSquared < 0.01f)
				{
					continue;
				}
				direction.Normalize();
				float distance = SiegeMassacreInteractionProfile.GetEscapeRoutDistance(i);
				AddCivilianRoutPointUnique(CivilianEscapePointPool, ProjectCivilianRoutPointToGround(mission, anchor + direction * distance));
			}
			Logger.Log("SiegeAiIntervention", "Prepared civilian rout point pools. Scene=" + (sceneName ?? "N/A") + ", Interior=" + CivilianInteriorHidePointPool.Count + ", Escape=" + CivilianEscapePointPool.Count);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "EnsureCivilianRoutPointPools failed: " + ex.Message);
		}
	}

	private static void AddTaggedCivilianRoutPoints(Mission mission, List<Vec3> pool, Vec3 anchor, float outwardDistance, params string[] tags)
	{
		if (mission?.Scene == null || pool == null || tags == null)
		{
			return;
		}
		foreach (string tag in tags)
		{
			try
			{
				foreach (GameEntity entity in mission.Scene.FindEntitiesWithTag(tag))
				{
					if (entity == null)
					{
						continue;
					}
					MatrixFrame frame = entity.GetGlobalFrame();
					Vec3 point = frame.origin;
					if (outwardDistance > 0f)
					{
						Vec3 outward = point - anchor;
						outward.z = 0f;
						if (outward.LengthSquared < 0.01f)
						{
							outward = Vec3.Forward;
						}
						outward.Normalize();
						point += outward * outwardDistance;
					}
					AddCivilianRoutPointUnique(pool, ProjectCivilianRoutPointToGround(mission, point));
				}
			}
			catch
			{
			}
		}
	}

	private static void AddCivilianRoutPointUnique(List<Vec3> pool, Vec3 point)
	{
		if (pool == null)
		{
			return;
		}
		if (pool.Any(existing => existing.DistanceSquared(point) <= 4f))
		{
			return;
		}
		pool.Add(point);
	}

	private static Vec3 ProjectCivilianRoutPointToGround(Mission mission, Vec3 point)
	{
		try
		{
			if (mission?.Scene != null)
			{
				point.z = mission.Scene.GetGroundHeightAtPosition(point, BodyFlags.CommonCollisionExcludeFlags);
			}
		}
		catch
		{
			try
			{
				if (mission?.Scene != null)
				{
					point.z = mission.Scene.GetGroundHeightAtPosition(point);
				}
			}
			catch
			{
			}
		}
		return point;
	}

	private static void KeepCivilianHidingFromOccupation(Agent civilian, Mission mission, Agent main, bool force)
	{
		try
		{
			if (civilian == null || mission == null || main == null || !civilian.IsActive())
			{
				return;
			}
			civilian.InvalidateTargetAgent();
			civilian.SetWatchState(Agent.WatchState.Alarmed);
			ApplyOneTimeFrightenedCivilianAction(civilian, allowGathered: true);
			float now = mission.CurrentTime;
			bool hasHideTarget = CivilianHideTargets.TryGetValue(civilian.Index, out Vec3 hideTarget);
			bool hasLastOrder = LastCivilianHideOrderTimes.TryGetValue(civilian.Index, out float lastOrder);
			bool reachedHideTarget = hasHideTarget && civilian.Position.DistanceSquared(hideTarget) <= 4f * 4f;
			bool needNewTarget = force || !hasHideTarget || !hasLastOrder || (!reachedHideTarget && now - lastOrder >= MassacreCivilianHideRefreshSeconds);
			if (needNewTarget)
			{
				CivilianHideSettledAgentIndexes.Remove(civilian.Index);
				hideTarget = ResolveCivilianEscapeOrHideTarget(civilian, mission, main);
				CivilianHideTargets[civilian.Index] = hideTarget;
				LastCivilianHideOrderTimes[civilian.Index] = now;
				ClearAgentLookTarget(civilian);
				civilian.SetMaximumSpeedLimit(-1f, false);
				try
				{
					civilian.SetCrouchMode(false);
				}
				catch
				{
				}
				TrySetInterventionAgentTargetPosition(civilian, hideTarget, SiegeAgentWallRescueProfile.Source + ":civilian_hide");
			}
			else if (reachedHideTarget)
			{
				if (CivilianHideSettledAgentIndexes.Add(civilian.Index))
				{
					try
					{
						civilian.DisableScriptedMovement();
						civilian.ClearTargetFrame();
						civilian.InvalidateTargetAgent();
						civilian.SetMaximumSpeedLimit(0f, false);
						bool escaped = ShouldCivilianEscapeSettlement(civilian);
						civilian.SetCrouchMode(!escaped);
						if (escaped)
						{
							ClearAgentLookTarget(civilian);
						}
						else
						{
							civilian.SetLookToPointOfInterest(main.Position);
						}
					}
					catch
					{
					}
				}
				return;
			}
			try
			{
				civilian.SetMaximumSpeedLimit(-1f, false);
			}
			catch
			{
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "KeepCivilianHidingFromOccupation failed: " + ex.Message);
		}
	}

	private static void TryCompleteMassacreIfAllTargetsDown(Mission mission)
	{
		if (_massacreVictoryReached || mission?.Agents == null)
		{
			return;
		}
		int remaining = mission.Agents.Count(a => IsMassacreTargetAgent(a, includeHeroes: true) && !CountedMassacreVictims.Contains(a.Index));
		if (remaining > 0)
		{
			return;
		}
		CompleteMassacreVictory(mission, SiegeMassacreInteractionProfile.AllTargetsDownVictorySource);
	}

	private static void CompleteMassacreVictory(Mission mission, string reason)
	{
		if (_massacreVictoryReached)
		{
			return;
		}
		_massacreVictoryReached = true;
		TryEndNativeMissionFightHandlerForManualExit(mission);
		KeepAlliedVictoryCheer(mission);
		if (_culturalRepopulationRequested)
		{
			ApplyCulturalRepopulationNow(SiegeCulturalRepopulationProfile.MassacreVictoryApplySource);
		}
		InformationManager.DisplayMessage(new InformationMessage(SiegeInterventionCompletionUiProfile.MassacreVictoryMessage, Color.FromUint(SiegeInterventionCompletionUiProfile.MassacreVictoryMessageColor)));
		ShowMassacreVictoryLootMessages();
		try
		{
			MBInformationManager.AddQuickInformation(new TextObject(SiegeInterventionCompletionUiProfile.MassacreVictoryQuickText), 0, null, null, "event:/ui/mission/arena_victory");
		}
		catch
		{
		}
		Logger.Log("SiegeAiIntervention", "Massacre victory completed. Reason=" + (reason ?? "N/A") + ", SpawnedCivilians=" + _lastSceneCivilianSpawnedCount + ", CountedVictims=" + CountedMassacreVictims.Count);
	}

	private static void TryEndNativeMissionFightHandlerForManualExit(Mission mission)
	{
		try
		{
			if (mission == null)
			{
				return;
			}
			SandBox.Missions.MissionLogics.MissionFightHandler fightHandler = mission.GetMissionBehavior<SandBox.Missions.MissionLogics.MissionFightHandler>();
			if (fightHandler != null && fightHandler.IsThereActiveFight())
			{
				fightHandler.EndFight(true);
				Logger.Log("SiegeAiIntervention", "Ended native MissionFightHandler after massacre victory so TAB can leave normally.");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryEndNativeMissionFightHandlerForManualExit failed: " + ex.Message);
		}
	}

	private static void ShowMassacreVictoryLootMessages()
	{
		try
		{
			if (_lastMarketGoldLoot > 0)
			{
				InformationManager.DisplayMessage(new InformationMessage(SiegeLootAccountingProfile.BuildMarketGoldMessage(SiegeLootAccountingProfile.MassacreActionName, _lastMarketGoldLoot), Color.FromUint(SiegeLootAccountingProfile.LootMessageColor)));
			}
			if (_lastLootItemTotal > 0)
			{
				InformationManager.DisplayMessage(new InformationMessage(SiegeLootAccountingProfile.BuildMarketInventoryMessage(SiegeLootAccountingProfile.MassacreActionName, _lastLootItemTotal, _lastLootStackKinds, _lastLootValue), Color.FromUint(SiegeLootAccountingProfile.LootMessageColor)));
			}
			if (_lastCivilianGoldLoot > 0)
			{
				InformationManager.DisplayMessage(new InformationMessage(SiegeLootAccountingProfile.BuildCivilianSpoilsMessage(_lastCivilianGoldLoot), Color.FromUint(SiegeLootAccountingProfile.LootMessageColor)));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ShowMassacreVictoryLootMessages failed: " + ex.Message);
		}
	}

	private static void KeepAlliedVictoryCheer(Mission mission)
	{
		if (mission?.Agents == null)
		{
			return;
		}
		foreach (Agent allied in mission.Agents.ToList())
		{
			if (allied == null || !allied.IsActive() || !AlliedAgentIndexes.Contains(allied.Index))
			{
				continue;
			}
			if (!VictoryCheerAgentIndexes.Add(allied.Index))
			{
				continue;
			}
			try
			{
				allied.InvalidateTargetAgent();
				allied.DisableScriptedMovement();
				TrySetInterventionAgentTargetPosition(allied, allied.Position, SiegeAgentWallRescueProfile.Source + ":victory_stop");
				allied.Formation?.SetMovementOrder(MovementOrder.MovementOrderStop);
				Agent main = Agent.Main ?? mission.MainAgent;
				if (main != null && main.IsActive())
				{
					allied.SetLookAgent(main);
				}
				allied.SetWatchState(Agent.WatchState.Patrolling);
				allied.SetActionChannel(0, ActionIndexCache.act_cheer_1, true, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, MBRandom.RandomFloat, false, -0.2f, 0, true);
				allied.SetActionChannel(1, ActionIndexCache.act_none, true, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
			}
			catch (Exception ex)
			{
				Logger.Log("SiegeAiIntervention", "Victory cheer failed: " + ex.Message);
			}
		}
	}

	private static void OnInterventionAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState)
	{
		if (!_massacreStarted)
		{
			TryHandleLocalPlayerCivilianDownForIntervention(affectedAgent, affectorAgent, agentState);
			return;
		}
		if (affectedAgent == null || CountedMassacreVictims.Contains(affectedAgent.Index))
		{
			return;
		}
		if (agentState != AgentState.Killed && agentState != AgentState.Unconscious)
		{
			return;
		}
		if (!IsMassacreTargetAgent(affectedAgent, includeHeroes: true, requireActive: false))
		{
			return;
		}
		CountedMassacreVictims.Add(affectedAgent.Index);
		_lastMassacreRealKillMissionTime = (Mission.Current ?? affectedAgent.Mission)?.CurrentTime ?? _lastMassacreRealKillMissionTime;
		CharacterObject character = affectedAgent.Character as CharacterObject;
		Hero hero = character?.HeroObject;
		if (IsInterventionNotableHero(hero))
		{
			PendingInterventionNotableDeaths.Add(hero);
			Logger.Log("SiegeAiIntervention", "Queued intervention notable for settlement-resolution death. Hero=" + (hero.StringId ?? "N/A") + ", Agent=" + affectedAgent.Index + ", State=" + agentState);
		}
		else if (hero == null)
		{
			_lastKilledCivilianUnits++;
		}
		TryLootCivilianAgent(affectedAgent, massacre: true, force: true);
	}

	private static bool TryHandleLocalPlayerCivilianDownForIntervention(Agent affectedAgent, Agent affectorAgent, AgentState agentState)
	{
		try
		{
			if (!IsActiveInCurrentMission() || affectedAgent == null || affectorAgent == null || !affectorAgent.IsMainAgent)
			{
				return false;
			}
			if (agentState != AgentState.Killed && agentState != AgentState.Unconscious)
			{
				return false;
			}
			if (!IsMassacreTargetAgent(affectedAgent, includeHeroes: true, requireActive: false))
			{
				return false;
			}
			Mission mission = Mission.Current ?? affectedAgent.Mission;
			if (mission == null)
			{
				return false;
			}
			string targetName = affectedAgent.Name?.ToString();
			bool firstTrackedVictim = LocalPlayerAttackVictimAgentIndexes.Add(affectedAgent.Index);
			if (LocalPlayerAttackDownAgentIndexes.Add(affectedAgent.Index))
			{
				if (firstTrackedVictim)
				{
					TryApplyRegionalConflictTrustPenalty(ResolveCurrentSettlement(), affectedAgent, targetName, victimDown: true, SiegeLocalCivilianReactionProfile.PlayerDownSource);
				}
				InformationManager.DisplayMessage(new InformationMessage(SiegeLocalCivilianReactionProfile.BuildPlayerDownMessage(targetName), Color.FromUint(SiegeLocalAttackProfile.MessageColor)));
				RecordInterventionMemory(SiegeLocalAttackProfile.MemoryTitle, SiegeLocalCivilianReactionProfile.BuildPlayerDownMemoryText(targetName));
				TriggerLocalCivilianWitnessReactions(mission, affectedAgent, Agent.Main ?? mission.MainAgent, victimDown: true, targetName);
				Logger.Log("SiegeAiIntervention", "Handled local player civilian down without starting massacre. Source=" + SiegeLocalCivilianReactionProfile.PlayerDownSource + ", Agent=" + affectedAgent.Index + ", State=" + agentState);
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryHandleLocalPlayerCivilianDownForIntervention failed: " + ex.Message);
			return false;
		}
	}

	private static bool IsEligibleCivilianAgent(Agent agent, bool includeHeroes, bool requireActive = true)
	{
		if (agent == null || !agent.IsHuman || agent == Agent.Main || AlliedAgentIndexes.Contains(agent.Index))
		{
			return false;
		}
		if (requireActive && (!agent.IsActive() || agent.State == AgentState.Killed || agent.State == AgentState.Unconscious))
		{
			return false;
		}
		CharacterObject character = agent.Character as CharacterObject;
		if (!includeHeroes && character?.HeroObject != null)
		{
			return false;
		}
		return IsCivilianForIntervention(character);
	}

	private static bool IsMassacreTargetAgent(Agent agent, bool includeHeroes, bool requireActive = true)
	{
		if (agent == null || !agent.IsHuman || agent == Agent.Main || AlliedAgentIndexes.Contains(agent.Index))
		{
			return false;
		}
		if (requireActive && (!agent.IsActive() || agent.State == AgentState.Killed || agent.State == AgentState.Unconscious))
		{
			return false;
		}
		CharacterObject character = agent.Character as CharacterObject;
		if (character == null || character == CharacterObject.PlayerCharacter || IsProtectedChildCharacter(character))
		{
			return false;
		}
		Hero hero = character.HeroObject;
		if (hero != null)
		{
			return includeHeroes && IsInterventionNotableHero(hero);
		}
		if (IsBackstreetOrCriminalCharacter(character))
		{
			return false;
		}
		return IsCivilianForIntervention(character);
	}

	private static bool IsProtectedChildAgent(Agent agent)
	{
		return IsProtectedChildCharacter(agent?.Character as CharacterObject);
	}

	private static bool IsProtectedChildCharacter(CharacterObject character)
	{
		return character != null && SceneTauntBehavior.IsChildSceneProtectedTarget(character);
	}

	private static bool IsInterventionNotableHero(Hero hero)
	{
		try
		{
			return hero != null
				&& hero != Hero.MainHero
				&& hero.IsAlive
				&& hero.IsNotable
				&& !IsProtectedChildCharacter(hero.CharacterObject);
		}
		catch
		{
			return false;
		}
	}

	private static bool ShouldForceInterventionNotableUnconscious(Agent agent)
	{
		try
		{
			if (!IsActiveInCurrentMission() || agent == null || !agent.IsHuman || agent == Agent.Main)
			{
				return false;
			}
			CharacterObject character = agent.Character as CharacterObject;
			return SiegeNotableSceneDeathProfile.ShouldForceUnconscious(IsActiveInCurrentMission(), IsInterventionNotableHero(character?.HeroObject));
		}
		catch
		{
			return false;
		}
	}

	private static bool IsGuardOrSoldier(CharacterObject character)
	{
		if (character == null)
		{
			return false;
		}
		return character.Occupation == Occupation.Soldier || character.Occupation == Occupation.Guard || character.Occupation == Occupation.PrisonGuard || character.Occupation == Occupation.BannerBearer || character.Occupation == Occupation.CaravanGuard;
	}

	private static bool IsBackstreetOrCriminalCharacter(CharacterObject character)
	{
		if (character == null)
		{
			return false;
		}
		switch (character.Occupation)
		{
		case Occupation.Gangster:
		case Occupation.GangLeader:
		case Occupation.Bandit:
			return true;
		}
		string id = character.StringId ?? "";
		string name = character.Name?.ToString() ?? "";
		return ContainsAny((id + " " + name).ToLowerInvariant(), "gangster", "gang_leader", "bandit", "looter", "thug", "robber", "backstreet", "alley", "暴徒", "帮派", "后巷", "流氓", "土匪", "劫匪");
	}

	private static bool IsCivilianForIntervention(CharacterObject character)
	{
		if (character == null || character == CharacterObject.PlayerCharacter || IsGuardOrSoldier(character) || IsProtectedChildCharacter(character))
		{
			return false;
		}
		Hero hero = character.HeroObject;
		if (hero != null)
		{
			return IsInterventionNotableHero(hero);
		}
		if (IsBackstreetOrCriminalCharacter(character))
		{
			return false;
		}
		switch (character.Occupation)
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

	private static bool SummonAlliedTroops(int requestedCount, string source)
	{
		Mission mission = Mission.Current;
		Agent main = Agent.Main ?? mission?.MainAgent;
		Team team = mission?.PlayerTeam ?? main?.Team;
		PartyBase party = PartyBase.MainParty;
		if (mission == null || main == null || team == null || party?.MemberRoster == null)
		{
			return false;
		}
		int count = Math.Max(1, Math.Min(requestedCount, MaxSummonPerAction));
		List<CharacterObject> troops = PickInterventionTroops(count);
		if (troops.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(SiegeInterventionEntryProfile.NoHealthyTroopsMessage, Color.FromUint(SiegeInterventionEntryProfile.NoHealthyTroopsMessageColor)));
			return false;
		}
		int spawned = 0;
		Vec3 anchor = main.Position;
		Formation infantryFormation = team.GetFormation(FormationClass.Infantry);
		MarkFormationPlayerCommandable(infantryFormation, main);
		Vec3 forward = main.LookDirection;
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
		bool spawnInCordon = false;
		Vec3 cordonCenter = spawnInCordon ? GetCivilianAssemblyCenter(mission) : anchor;
		for (int i = 0; i < troops.Count; i++)
		{
			CharacterObject troop = troops[i];
			Vec3 position = spawnInCordon
				? GetAlliedCordonSlotPosition(mission, i, troops.Count)
				: (anchor - forward * (2.5f + i / 3f) + right * (((i % 2 == 0) ? 1f : -1f) * (1.5f + (i % 8) * 0.8f)));
			Vec3 spawnDirection = spawnInCordon ? (cordonCenter - position) : forward;
			spawnDirection.z = 0f;
			if (spawnDirection.LengthSquared < 0.01f)
			{
				spawnDirection = forward;
			}
			spawnDirection.Normalize();
			try
			{
				if (mission.Scene != null)
				{
					position.z = mission.Scene.GetGroundHeightAtPosition(position);
				}
			}
			catch
			{
			}
			try
			{
				IAgentOriginBase origin = new PartyAgentOrigin(party, troop);
				CommandableOriginRuntimeIds.Add(RuntimeHelpers.GetHashCode(origin));
				AgentBuildData buildData = new AgentBuildData(troop).TroopOrigin(origin).Monster(TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(troop.Race, "_settlement")).Team(team)
					.InitialPosition(in position)
					.InitialDirection(spawnDirection.AsVec2.Normalized())
					.Controller(AgentControllerType.AI)
					.CivilianEquipment(civilianEquipment: false)
					.NoHorses(noHorses: true);
				if (infantryFormation != null)
				{
					buildData = buildData.Formation(infantryFormation)
						.FormationTroopSpawnCount(troops.Count)
						.FormationTroopSpawnIndex(i)
						.SpawnsIntoOwnFormation(true)
						.SpawnsUsingOwnTroopClass(false);
				}
				Agent spawnedAgent = mission.SpawnAgent(buildData, false);
				if (spawnedAgent != null)
				{
					NotifyAgentBuiltForMission(spawnedAgent, mission);
					spawned++;
					AlliedAgentIndexes.Add(spawnedAgent.Index);
					RestoreAlliedSoldierFriendlyState(spawnedAgent, 0f, SiegeSoldierCordonProfile.SpawnAlliedTroopRestoreSource, forceFollow: false);
					AssignAgentToPlayerFormation(spawnedAgent, FormationClass.Infantry);
					spawnedAgent.SetWatchState(_massacreStarted ? Agent.WatchState.Alarmed : Agent.WatchState.Patrolling);
					if (_massacreStarted)
					{
						DisableCompanionStyleFollow(spawnedAgent);
						ForceAgentForMassacreFight(spawnedAgent);
					}
					else if (spawnInCordon)
					{
						DisableCompanionStyleFollow(spawnedAgent);
						spawnedAgent.SetWatchState(Agent.WatchState.Patrolling);
						if (!_soldierDefaultFollowOrderIssued)
						{
							_soldierDefaultFollowOrderIssued = TrySetPlayerFormationFollowOrder(FormationClass.Infantry, SiegeSoldierCordonProfile.SpawnDefaultFollowSource);
						}
					}
					else
					{
						DisableCompanionStyleFollow(spawnedAgent);
						if (!_soldierDefaultFollowOrderIssued)
						{
							_soldierDefaultFollowOrderIssued = TrySetPlayerFormationFollowOrder(FormationClass.Infantry, SiegeSoldierCordonProfile.SpawnDefaultFollowSource);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log("SiegeAiIntervention", "Spawn allied troop failed: " + ex.Message);
			}
		}
		if (spawned > 0)
		{
			SpawnInterventionBannerBearers(mission, main, team, party, source);
			TrySetPlayerFormationFollowOrder(FormationClass.Infantry, SiegeSoldierCordonProfile.SpawnFollowAfterBatchSource);
			TryPrimePlayerOrderController(mission, SiegeSoldierCordonProfile.SpawnAlliedBatchOrderControllerSource, force: true);
			InformationManager.DisplayMessage(new InformationMessage(SiegeInterventionEntryProfile.BuildSummonedTroopsMessage(spawned), Color.FromUint(SiegeInterventionEntryProfile.SummonedTroopsMessageColor)));
			return true;
		}
		return false;
	}

	private static int SpawnInterventionBannerBearers(Mission mission, Agent main, Team team, PartyBase party, string source)
	{
		try
		{
			if (!SiegeBannerBearerProfile.BannerBearersEnabled || !IsActiveInCurrentMission() || mission == null || main == null || team == null || party?.MemberRoster == null)
			{
				return 0;
			}
			PruneBannerBearerState(mission);
			int missingCount = Math.Max(0, SiegeBannerBearerProfile.BannerBearerCount - BannerBearerAgentIndexes.Count);
			if (missingCount == 0)
			{
				return 0;
			}
			ItemObject bannerItem = ResolveInterventionBannerItem();
			if (!IsUsableBannerItem(bannerItem))
			{
				Logger.Log("SiegeAiIntervention", "Skip GCCZ banner bearers: no usable banner item. Source=" + (source ?? "N/A"));
				return 0;
			}
			bool playerHasMount = ShouldSpawnMountedBannerBearers(main);
			FormationClass bannerFormationClass = GetBannerBearerFormationClass();
			Formation bannerFormation = team.GetFormation(bannerFormationClass);
			MarkFormationPlayerCommandable(bannerFormation, main);
			List<CharacterObject> troops = PickInterventionBannerBearerTroops(missingCount);
			if (troops.Count == 0)
			{
				Logger.Log("SiegeAiIntervention", "Skip GCCZ banner bearers: no available non-hero troop. Source=" + (source ?? "N/A"));
				return 0;
			}
			Banner banner = ResolveInterventionBanner();
			int spawned = 0;
			for (int i = 0; i < missingCount && i < troops.Count; i++)
			{
				CharacterObject troop = troops[i];
				if (troop == null)
				{
					continue;
				}
				Vec3 position = GetBannerBearerInitialSpawnPosition(main, mission, i);
				Vec3 spawnDirection = main.LookDirection;
				bool spawnWithHorse = playerHasMount && CharacterCanSpawnMounted(troop);
				spawnDirection.z = 0f;
				if (spawnDirection.LengthSquared < 0.01f)
				{
					spawnDirection = Vec3.Forward;
				}
				spawnDirection.Normalize();
				try
				{
					IAgentOriginBase origin = new PartyAgentOrigin(party, troop);
					CommandableOriginRuntimeIds.Add(RuntimeHelpers.GetHashCode(origin));
					AgentBuildData buildData = new AgentBuildData(troop).TroopOrigin(origin).Monster(TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(troop.Race, "_settlement")).Team(team)
						.InitialPosition(in position)
						.InitialDirection(spawnDirection.AsVec2.Normalized())
						.Controller(AgentControllerType.AI)
						.CivilianEquipment(civilianEquipment: false)
						.NoHorses(noHorses: !spawnWithHorse)
						.BannerItem(bannerItem);
					if (spawnWithHorse)
					{
						buildData = buildData.MountKey(MountCreationKey.GetRandomMountKeyString(troop.Equipment[EquipmentIndex.ArmorItemEndSlot].Item, troop.GetMountKeySeed()));
					}
					if (banner != null)
					{
						buildData = buildData.Banner(banner);
					}
					ItemObject replacementWeapon = TryGetBannerBearerReplacementWeapon(troop);
					if (replacementWeapon != null)
					{
						buildData = buildData.BannerReplacementWeaponItem(replacementWeapon);
					}
					if (bannerFormation != null)
					{
						buildData = buildData.Formation(bannerFormation)
							.FormationTroopSpawnCount(SiegeBannerBearerProfile.BannerBearerCount)
							.FormationTroopSpawnIndex(i)
							.SpawnsIntoOwnFormation(true)
							.SpawnsUsingOwnTroopClass(false);
					}
					Agent spawnedAgent = mission.SpawnAgent(buildData, false);
					if (spawnedAgent == null)
					{
						continue;
					}
					NotifyAgentBuiltForMission(spawnedAgent, mission);
					AlliedAgentIndexes.Add(spawnedAgent.Index);
					BannerBearerAgentIndexes.Add(spawnedAgent.Index);
					RestoreAlliedSoldierFriendlyState(spawnedAgent, 0f, SiegeBannerBearerProfile.BannerBearerRestoreSource, forceFollow: false, clearTarget: true);
					AssignAgentToPlayerFormation(spawnedAgent, bannerFormationClass, refreshFormationOrders: false);
					RestoreBannerBearerMountTeam(spawnedAgent, main, mission);
					TryWieldBannerBearerBanner(spawnedAgent);
					spawnedAgent.SetWatchState(_massacreStarted || _plunderStarted ? Agent.WatchState.Alarmed : Agent.WatchState.Patrolling);
					spawned++;
				}
				catch (Exception ex)
				{
					Logger.Log("SiegeAiIntervention", "Spawn GCCZ banner bearer failed: " + ex.Message);
				}
			}
			if (spawned > 0)
			{
				TrySetPlayerFormationFollowOrder(bannerFormationClass, SiegeBannerBearerProfile.NativeFormationSource);
				TryPrimePlayerOrderController(mission, SiegeBannerBearerProfile.NativeFormationSource, force: true);
				Logger.Log("SiegeAiIntervention", "Spawned GCCZ banner bearers=" + spawned + ", Formation=" + bannerFormationClass + ", Mounted=" + playerHasMount + ", Source=" + (source ?? SiegeBannerBearerProfile.SpawnSource) + ", BannerItem=" + (bannerItem?.StringId ?? "null"));
			}
			return spawned;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "SpawnInterventionBannerBearers failed: " + ex.Message);
			return 0;
		}
	}

	private static List<CharacterObject> PickInterventionBannerBearerTroops(int count)
	{
		if (count <= 0)
		{
			return new List<CharacterObject>();
		}
		try
		{
			CharacterObject selectedTroop = PickBannerBearerTroopFromRoster(_selectedInterventionRoster);
			if (selectedTroop != null)
			{
				return RepeatBannerBearerTroop(selectedTroop, count);
			}
			CharacterObject cultureTroop = PickHighestTierPlayerCultureBannerBearerTroop();
			if (cultureTroop != null)
			{
				return RepeatBannerBearerTroop(cultureTroop, count);
			}
			CharacterObject mainPartyTroop = PickBannerBearerTroopFromRoster(PartyBase.MainParty?.MemberRoster);
			return RepeatBannerBearerTroop(mainPartyTroop, count);
		}
		catch
		{
			return new List<CharacterObject>();
		}
	}

	private static List<CharacterObject> RepeatBannerBearerTroop(CharacterObject troop, int count)
	{
		List<CharacterObject> result = new List<CharacterObject>();
		if (troop == null || count <= 0)
		{
			return result;
		}
		for (int i = 0; i < count; i++)
		{
			result.Add(troop);
		}
		return result;
	}

	private static CharacterObject PickBannerBearerTroopFromRoster(TroopRoster roster)
	{
		try
		{
			List<BannerBearerTroopStack> stacks = BuildBannerBearerTroopStacks(roster);
			if (stacks.Count == 0)
			{
				return null;
			}
			int maxAvailable = stacks.Max(stack => stack.Available);
			List<BannerBearerTroopStack> tiedStacks = stacks
				.Where(stack => stack.Available == maxAvailable)
				.OrderBy(stack => stack.SourceOrder)
				.ToList();
			if (tiedStacks.Count == 0)
			{
				return null;
			}
			int index = tiedStacks.Count == 1 ? 0 : MBRandom.RandomInt(tiedStacks.Count);
			return tiedStacks[index].Troop;
		}
		catch
		{
			return null;
		}
	}

	private static CharacterObject PickHighestTierPlayerCultureBannerBearerTroop()
	{
		try
		{
			CultureObject playerCulture = Hero.MainHero?.Culture ?? Clan.PlayerClan?.Culture;
			string playerCultureId = playerCulture?.StringId ?? "";
			IEnumerable<CharacterObject> characters = Game.Current?.ObjectManager?.GetObjectTypeList<CharacterObject>();
			if (string.IsNullOrWhiteSpace(playerCultureId) || characters == null)
			{
				return null;
			}
			List<CharacterObject> candidates = characters
				.Where(character => IsValidInterventionBannerBearerTroop(character))
				.Where(character => character.IsSoldier)
				.Where(character => string.Equals(character.Culture?.StringId ?? "", playerCultureId, StringComparison.OrdinalIgnoreCase))
				.ToList();
			if (candidates.Count == 0)
			{
				return null;
			}
			int maxTier = candidates.Max(character => character.Tier);
			List<CharacterObject> maxTierCandidates = candidates.Where(character => character.Tier == maxTier).ToList();
			int maxLevel = maxTierCandidates.Max(character => character.Level);
			List<CharacterObject> tiedCandidates = maxTierCandidates
				.Where(character => character.Level == maxLevel)
				.OrderBy(character => character.StringId ?? "")
				.ToList();
			if (tiedCandidates.Count == 0)
			{
				return null;
			}
			int index = tiedCandidates.Count == 1 ? 0 : MBRandom.RandomInt(tiedCandidates.Count);
			return tiedCandidates[index];
		}
		catch
		{
			return null;
		}
	}

	private static List<BannerBearerTroopStack> BuildBannerBearerTroopStacks(TroopRoster roster)
	{
		List<BannerBearerTroopStack> result = new List<BannerBearerTroopStack>();
		if (roster == null)
		{
			return result;
		}
		try
		{
			Dictionary<string, BannerBearerTroopStack> stacksByTroopId = new Dictionary<string, BannerBearerTroopStack>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < roster.Count; i++)
			{
				TroopRosterElement element = roster.GetElementCopyAtIndex(i);
				CharacterObject troop = element.Character;
				if (!IsValidInterventionBannerBearerTroop(troop) || element.Number <= 0)
				{
					continue;
				}
				int available = Math.Max(0, element.Number - element.WoundedNumber);
				if (available <= 0)
				{
					continue;
				}
				string key = troop.StringId ?? RuntimeHelpers.GetHashCode(troop).ToString();
				if (!stacksByTroopId.TryGetValue(key, out BannerBearerTroopStack stack))
				{
					stack = new BannerBearerTroopStack
					{
						Troop = troop,
						Available = 0,
						SourceOrder = i
					};
					stacksByTroopId[key] = stack;
					result.Add(stack);
				}
				stack.Available += available;
				if (i < stack.SourceOrder)
				{
					stack.SourceOrder = i;
				}
			}
		}
		catch
		{
		}
		return result;
	}

	private static bool IsValidInterventionBannerBearerTroop(CharacterObject troop)
	{
		return troop != null && troop != CharacterObject.PlayerCharacter && !troop.IsHero;
	}

	private static bool CharacterCanSpawnMounted(CharacterObject troop)
	{
		try
		{
			return troop != null && troop.HasMount();
		}
		catch
		{
			return false;
		}
	}

	private static Banner ResolveInterventionBanner()
	{
		try
		{
			return Hero.MainHero?.Clan?.Banner ?? Clan.PlayerClan?.Banner;
		}
		catch
		{
			return null;
		}
	}

	private static ItemObject ResolveInterventionBannerItem()
	{
		try
		{
			ItemObject playerBannerItem = null;
			if (Hero.MainHero != null)
			{
				EquipmentElement playerBannerElement = Hero.MainHero.BannerItem;
				playerBannerItem = playerBannerElement.Item;
			}
			if (IsUsableBannerItem(playerBannerItem))
			{
				return playerBannerItem;
			}
		}
		catch
		{
		}
		try
		{
			IEnumerable<ItemObject> items = Game.Current?.ObjectManager?.GetObjectTypeList<ItemObject>();
			if (items == null)
			{
				return null;
			}
			BasicCultureObject playerCulture = Hero.MainHero?.Culture;
			return items
				.Where(IsUsableBannerItem)
				.OrderByDescending(i => playerCulture != null && i.Culture == playerCulture)
				.ThenBy(i => string.Equals(i.StringId ?? "", "campaign_banner_small", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
				.ThenBy(i => i.StringId ?? "")
				.FirstOrDefault();
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ResolveInterventionBannerItem failed: " + ex.Message);
			return null;
		}
	}

	private static bool IsUsableBannerItem(ItemObject item)
	{
		try
		{
			return item != null && item.IsBannerItem && item.BannerComponent != null;
		}
		catch
		{
			return false;
		}
	}

	private static ItemObject TryGetBannerBearerReplacementWeapon(CharacterObject troop)
	{
		try
		{
			return MissionGameModels.Current?.BattleBannerBearersModel?.GetBannerBearerReplacementWeapon(troop);
		}
		catch
		{
			return null;
		}
	}

	private static void PruneBannerBearerState(Mission mission)
	{
		try
		{
			if (mission?.Agents == null || BannerBearerAgentIndexes.Count == 0)
			{
				return;
			}
			HashSet<int> activeBannerIndexes = new HashSet<int>(mission.Agents.Where(a => a != null && a.IsHuman && a.IsActive() && BannerBearerAgentIndexes.Contains(a.Index)).Select(a => a.Index));
			foreach (int index in BannerBearerAgentIndexes.ToList())
			{
				if (activeBannerIndexes.Contains(index))
				{
					continue;
				}
				BannerBearerAgentIndexes.Remove(index);
				CordonReadyAgentIndexes.Remove(index);
				MassacreReadySoldierAgentIndexes.Remove(index);
				ClearMassacreHuntAssignment(index);
				AlliedAgentIndexes.Remove(index);
			}
		}
		catch
		{
		}
	}

	private static Vec3 GetBannerBearerInitialSpawnPosition(Agent main, Mission mission, int index)
	{
		Vec3 position = main?.Position ?? Vec3.Zero;
		try
		{
			Vec3 forward = main?.LookDirection ?? Vec3.Forward;
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
			float centered = index - (Math.Max(1, SiegeBannerBearerProfile.BannerBearerCount) - 1) * 0.5f;
			position = main.Position - forward * SiegeBannerBearerProfile.InitialBackOffsetMeters + right * centered * SiegeBannerBearerProfile.InitialSideSpacingMeters;
			try
			{
				if (mission?.Scene != null)
				{
					position.z = mission.Scene.GetGroundHeightAtPosition(position);
				}
			}
			catch
			{
			}
		}
		catch
		{
		}
		return position;
	}

	private static FormationClass GetBannerBearerFormationClass()
	{
		try
		{
			FormationClass formationClass = (FormationClass)SiegeBannerBearerProfile.NativeFormationClassIndex;
			return formationClass >= FormationClass.Infantry && formationClass < FormationClass.NumberOfRegularFormations
				? formationClass
				: FormationClass.Cavalry;
		}
		catch
		{
			return FormationClass.Cavalry;
		}
	}

	private static bool IsAgentMounted(Agent agent)
	{
		try
		{
			return agent?.MountAgent != null && agent.MountAgent.IsActive();
		}
		catch
		{
			return false;
		}
	}

	private static bool ShouldSpawnMountedBannerBearers(Agent main)
	{
		try
		{
			return IsAgentMounted(main) || DoesPlayerHaveBattleMountEquipment();
		}
		catch
		{
			return false;
		}
	}

	private static bool DoesPlayerHaveBattleMountEquipment()
	{
		try
		{
			if (CharacterObject.PlayerCharacter?.HasMount() == true)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			Equipment battleEquipment = Hero.MainHero?.BattleEquipment;
			return battleEquipment != null && battleEquipment[EquipmentIndex.Horse].Item?.HorseComponent != null;
		}
		catch
		{
			return false;
		}
	}

	private static void RestoreBannerBearerMountTeam(Agent bearer, Agent main, Mission mission)
	{
		try
		{
			Agent mount = bearer?.MountAgent;
			if (mount == null || !mount.IsActive())
			{
				return;
			}
			Team playerTeam = mission?.PlayerTeam ?? main?.Team;
			if (playerTeam != null && mount.Team != playerTeam)
			{
				mount.SetTeam(playerTeam, true);
			}
			mount.SetMortalityState(Agent.MortalityState.Invulnerable);
		}
		catch
		{
		}
	}

	private static void TryWieldBannerBearerBanner(Agent bearer)
	{
		try
		{
			if (bearer == null || !bearer.IsActive())
			{
				return;
			}
			ItemObject item = bearer.Equipment[EquipmentIndex.ExtraWeaponSlot].Item;
			if (!IsUsableBannerItem(item))
			{
				return;
			}
			if (bearer.GetPrimaryWieldedItemIndex() == EquipmentIndex.ExtraWeaponSlot || bearer.GetOffhandWieldedItemIndex() == EquipmentIndex.ExtraWeaponSlot)
			{
				return;
			}
			bearer.TryToWieldWeaponInSlot(EquipmentIndex.ExtraWeaponSlot, Agent.WeaponWieldActionType.InstantAfterPickUp, isWieldedOnSpawn: false);
		}
		catch
		{
		}
	}

	private static bool IsMainPartyOrSelectedInterventionTroop(CharacterObject character)
	{
		try
		{
			if (character == null || character == CharacterObject.PlayerCharacter || character.IsHero)
			{
				return false;
			}
			return RosterContainsAvailableTroop(_selectedInterventionRoster, character)
				|| RosterContainsAvailableTroop(PartyBase.MainParty?.MemberRoster, character);
		}
		catch
		{
			return false;
		}
	}

	private static bool RosterContainsAvailableTroop(TroopRoster roster, CharacterObject character)
	{
		if (roster == null || character == null)
		{
			return false;
		}
		string characterId = character.StringId ?? "";
		for (int i = 0; i < roster.Count; i++)
		{
			TroopRosterElement element = roster.GetElementCopyAtIndex(i);
			CharacterObject rosterCharacter = element.Character;
			if (rosterCharacter == null)
			{
				continue;
			}
			bool sameTroop = rosterCharacter == character
				|| (!string.IsNullOrWhiteSpace(characterId) && string.Equals(rosterCharacter.StringId ?? "", characterId, StringComparison.OrdinalIgnoreCase));
			if (!sameTroop)
			{
				continue;
			}
			int available = Math.Max(0, element.Number - element.WoundedNumber);
			if (available > 0)
			{
				return true;
			}
		}
		return false;
	}

	private static List<CharacterObject> PickInterventionTroops(int count)
	{
		List<CharacterObject> selectedTroops = ExpandSelectedInterventionRoster(count);
		if (selectedTroops.Count > 0)
		{
			return selectedTroops;
		}
		return PickTroopsFromMainParty(count);
	}

	private static List<CharacterObject> ExpandSelectedInterventionRoster(int count)
	{
		List<CharacterObject> result = new List<CharacterObject>();
		TroopRoster roster = _selectedInterventionRoster;
		if (roster == null || count <= 0)
		{
			return result;
		}
		for (int i = 0; i < roster.Count && result.Count < count; i++)
		{
			TroopRosterElement element = roster.GetElementCopyAtIndex(i);
			CharacterObject character = element.Character;
			if (!IsSelectableInterventionTroop(character) || element.Number <= 0)
			{
				continue;
			}
			int available = character.HeroObject != null ? element.Number : Math.Max(0, element.Number - element.WoundedNumber);
			for (int j = 0; j < available && result.Count < count; j++)
			{
				result.Add(character);
			}
		}
		return result;
	}

	private static List<CharacterObject> PickTroopsFromMainParty(int count)
	{
		List<CharacterObject> result = new List<CharacterObject>();
		TroopRoster roster = PartyBase.MainParty?.MemberRoster;
		if (roster == null)
		{
			return result;
		}
		for (int i = 0; i < roster.Count && result.Count < count; i++)
		{
			TroopRosterElement element = roster.GetElementCopyAtIndex(i);
			CharacterObject character = element.Character;
			if (character == null || character.IsHero || character == CharacterObject.PlayerCharacter || element.Number <= 0)
			{
				continue;
			}
			int available = Math.Max(0, element.Number - element.WoundedNumber);
			for (int j = 0; j < available && result.Count < count; j++)
			{
				result.Add(character);
			}
		}
		return result;
	}

	private static bool IsSelectableInterventionTroop(CharacterObject character)
	{
		try
		{
			if (character == null || character == CharacterObject.PlayerCharacter)
			{
				return false;
			}
			Hero hero = character.HeroObject;
			if (hero != null)
			{
				return hero != Hero.MainHero && hero.PartyBelongedTo == MobileParty.MainParty && !hero.IsPrisoner && !hero.IsWounded;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void LootSettlementMarketGold(string reason, bool showMessage = true)
	{
		try
		{
			Settlement settlement = ResolveCurrentSettlement();
			Town town = settlement?.Town;
			if (town == null || town.Gold <= 0)
			{
				return;
			}
			int amount = town.Gold;
			town.ChangeGold(-amount);
			AwardGoldToPlayer(amount, SiegeLootAccountingProfile.MarketGoldSource);
			_lastMarketGoldLoot += amount;
			if (showMessage)
			{
				InformationManager.DisplayMessage(new InformationMessage(SiegeLootAccountingProfile.BuildMarketGoldMessage(reason, amount), Color.FromUint(SiegeLootAccountingProfile.LootMessageColor)));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "LootSettlementMarketGold failed: " + ex.Message);
		}
	}

	private static void LootSettlementMarketInventory(float minRatio, float maxRatio, string reason, bool showMessage = true)
	{
		try
		{
			Settlement settlement = ResolveCurrentSettlement();
			ItemRoster sourceRoster = settlement?.ItemRoster;
			if (sourceRoster == null || MobileParty.MainParty == null || sourceRoster.Count <= 0)
			{
				return;
			}
			if (_pendingLootRoster == null)
			{
				_pendingLootRoster = new ItemRoster();
			}
			List<MarketLootCandidate> stacks = new List<MarketLootCandidate>();
			int totalAmount = 0;
			for (int i = 0; i < sourceRoster.Count; i++)
			{
				ItemRosterElement element = sourceRoster.GetElementCopyAtIndex(i);
				if (element.EquipmentElement.Item != null && element.Amount > 0)
				{
					stacks.Add(new MarketLootCandidate
					{
						EquipmentElement = element.EquipmentElement,
						Amount = element.Amount
					});
					totalAmount += element.Amount;
				}
			}
			if (totalAmount <= 0 || stacks.Count == 0)
			{
				return;
			}
			float ratio = minRatio + MBRandom.RandomFloat * Math.Max(0f, maxRatio - minRatio);
			int targetAmount = Math.Max(1, Math.Min(totalAmount, (int)MathF.Round(totalAmount * ratio)));
			List<MarketLootCandidate> shuffled = stacks.OrderBy(_ => MBRandom.RandomFloat).ToList();
			int remaining = targetAmount;
			int movedTotal = 0;
			int movedValue = 0;
			HashSet<string> movedKindKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			while (remaining > 0 && shuffled.Any(x => x.Amount > 0))
			{
				foreach (MarketLootCandidate element in shuffled)
				{
					if (remaining <= 0)
					{
						break;
					}
					int available = Math.Min(element.Amount, remaining);
					if (available <= 0)
					{
						continue;
					}
					int move = Math.Max(1, MBRandom.RandomInt(1, available + 1));
					sourceRoster.AddToCounts(element.EquipmentElement, -move);
					_pendingLootRoster.AddToCounts(element.EquipmentElement, move);
					_pendingLootScreen = true;
					element.Amount -= move;
					remaining -= move;
					movedTotal += move;
					movedKindKeys.Add(element.EquipmentElement.Item?.StringId ?? "item");
					movedValue += Math.Max(0, RewardSystemBehavior.Instance?.GetInventoryActualItemUnitValueForExternal(element.EquipmentElement) ?? element.EquipmentElement.Item?.Value ?? 0) * move;
				}
			}
			_lastLootItemTotal += movedTotal;
			_lastLootStackKinds += movedKindKeys.Count;
			_lastLootValue += movedValue;
			if (showMessage && movedTotal > 0)
			{
				InformationManager.DisplayMessage(new InformationMessage(SiegeLootAccountingProfile.BuildMarketInventoryMessage(reason, movedTotal, movedKindKeys.Count, movedValue), Color.FromUint(SiegeLootAccountingProfile.LootMessageColor)));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "LootSettlementMarketInventory failed: " + ex.Message);
		}
	}

	private static void AdjustSettlementAfterRelief(Settlement settlement, int publicTrustDelta, float loyaltyDelta, float securityDelta)
	{
		try
		{
			if (settlement != null && publicTrustDelta != 0 && RewardSystemBehavior.Instance != null)
			{
				RewardSystemBehavior.Instance.AdjustSettlementLocalPublicTrustForExternal(settlement, publicTrustDelta, SiegeSettlementEffectProfile.PositivePublicTrustReason);
			}
			if (settlement?.Town != null)
			{
				settlement.Town.Loyalty += loyaltyDelta;
				settlement.Town.Security += securityDelta;
			}
		}
		catch
		{
		}
	}

	private static int ReducePositiveIntDeltaForRegionalConflict(int delta, string effectName)
	{
		int adjusted = SiegeRegionalConflictProfile.ReducePositiveIntDelta(delta, _regionalConflictIncidentCount);
		if (adjusted != delta && delta > 0)
		{
			Logger.Log("SiegeAiIntervention", "Reduced positive integer effect by regional conflict debt. Effect=" + (effectName ?? "N/A")
				+ ", Original=" + delta
				+ ", Adjusted=" + adjusted
				+ ", Incidents=" + _regionalConflictIncidentCount);
		}
		return adjusted;
	}

	private static float ReducePositiveFloatDeltaForRegionalConflict(float delta, string effectName)
	{
		float adjusted = SiegeRegionalConflictProfile.ReducePositiveFloatDelta(delta, _regionalConflictIncidentCount);
		if (Math.Abs(adjusted - delta) > 0.001f && delta > 0f)
		{
			Logger.Log("SiegeAiIntervention", "Reduced positive float effect by regional conflict debt. Effect=" + (effectName ?? "N/A")
				+ ", Original=" + delta.ToString("0.##")
				+ ", Adjusted=" + adjusted.ToString("0.##")
				+ ", Incidents=" + _regionalConflictIncidentCount);
		}
		return adjusted;
	}

	private static void ApplyCivicChoiceSettlementEffects(Settlement settlement, SiegeCivicChoiceProfile profile, string settlementTrustReason, string boundVillageTrustReason)
	{
		try
		{
			if (settlement == null || profile == null)
			{
				return;
			}
			AdjustSettlementPublicTrustOnly(settlement, ReducePositiveIntDeltaForRegionalConflict(profile.SettlementPublicTrustDelta, "civic_settlement_public_trust"), settlementTrustReason);
			AdjustBoundVillagePublicTrust(settlement, ReducePositiveIntDeltaForRegionalConflict(profile.BoundVillagePublicTrustDelta, "civic_bound_village_public_trust"), boundVillageTrustReason);
			if (settlement.Town != null)
			{
				if (profile.LocksLoyalty)
				{
					float adjustedLockValue = ReducePositiveFloatDeltaForRegionalConflict(profile.LoyaltyLockValue, "civic_loyalty_lock");
					settlement.Town.Loyalty = MathF.Max(settlement.Town.Loyalty, adjustedLockValue);
				}
				else
				{
					settlement.Town.Loyalty += ReducePositiveFloatDeltaForRegionalConflict(profile.LoyaltyDelta, "civic_loyalty");
				}
				settlement.Town.Security += ReducePositiveFloatDeltaForRegionalConflict(profile.SecurityDelta, "civic_security");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyCivicChoiceSettlementEffects failed: " + ex.Message);
		}
	}

	private static int AdjustSettlementNotableRelations(Settlement settlement, int relationDelta, string reason)
	{
		int adjusted = 0;
		try
		{
			if (settlement?.Notables == null || relationDelta == 0)
			{
				return 0;
			}
			HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (Hero notable in settlement.Notables.ToList())
			{
				string key = notable?.StringId;
				if (notable == null || notable == Hero.MainHero || !notable.IsAlive || string.IsNullOrWhiteSpace(key) || !seen.Add(key))
				{
					continue;
				}
				try
				{
					ChangeRelationAction.ApplyPlayerRelation(notable, relationDelta, true, true);
					adjusted++;
				}
				catch (Exception ex)
				{
					Logger.Log("SiegeAiIntervention", "Settlement notable relation adjustment failed. Reason=" + (reason ?? "N/A") + ", Notable=" + key + ": " + ex.Message);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "AdjustSettlementNotableRelations failed: " + ex.Message);
		}
		return adjusted;
	}

	private static void QueuePositiveNotableRelationForFinalAftermath(int relationDelta, bool includeBoundVillages, string reason)
	{
		try
		{
			if (relationDelta <= 0)
			{
				return;
			}
			if (relationDelta > _pendingPositiveNotableRelationDelta || (relationDelta == _pendingPositiveNotableRelationDelta && includeBoundVillages && !_pendingPositiveNotableRelationIncludesBoundVillages))
			{
				_pendingPositiveNotableRelationDelta = relationDelta;
				_pendingPositiveNotableRelationIncludesBoundVillages = includeBoundVillages;
				_pendingPositiveNotableRelationReason = string.IsNullOrWhiteSpace(reason) ? "siege_ai_positive_notables" : reason.Trim();
			}
		}
		catch
		{
		}
	}

	private static void QueuePositiveNotableTrustForFinalAftermath(int trustDelta, bool includeBoundVillages, string reason)
	{
		try
		{
			if (trustDelta <= 0)
			{
				return;
			}
			if (trustDelta > _pendingPositiveNotableTrustDelta || (trustDelta == _pendingPositiveNotableTrustDelta && includeBoundVillages && !_pendingPositiveNotableTrustIncludesBoundVillages))
			{
				_pendingPositiveNotableTrustDelta = trustDelta;
				_pendingPositiveNotableTrustIncludesBoundVillages = includeBoundVillages;
				_pendingPositiveNotableTrustReason = string.IsNullOrWhiteSpace(reason) ? "siege_ai_positive_notable_trust" : reason.Trim();
			}
		}
		catch
		{
		}
	}

	private static int ApplyPendingPositiveNotableRelationsForFinalAftermath(Settlement settlement, SiegeAftermathAction.SiegeAftermath aftermath)
	{
		try
		{
			int relationDelta = _pendingPositiveNotableRelationDelta;
			bool includeBoundVillages = _pendingPositiveNotableRelationIncludesBoundVillages;
			string reason = _pendingPositiveNotableRelationReason;
			_pendingPositiveNotableRelationDelta = 0;
			_pendingPositiveNotableRelationIncludesBoundVillages = false;
			_pendingPositiveNotableRelationReason = "";
			if (relationDelta <= 0)
			{
				return 0;
			}
			if (!SiegePositiveRelationTimingProfile.ShouldApplyQueuedPositiveRelations(ToStandaloneAftermathKind(aftermath)))
			{
				Logger.Log("SiegeAiIntervention", "Skipped queued positive notable relations because final aftermath is not mercy. Aftermath=" + aftermath + ", Delta=" + relationDelta);
				return 0;
			}
			int adjusted = includeBoundVillages
				? AdjustSettlementAndBoundVillageNotableRelations(settlement, relationDelta, reason)
				: AdjustSettlementNotableRelations(settlement, relationDelta, reason);
			Logger.Log("SiegeAiIntervention", "Applied queued positive notable relations after final mercy aftermath. Delta=" + relationDelta + ", IncludeBoundVillages=" + includeBoundVillages + ", Adjusted=" + adjusted + ", Reason=" + (reason ?? "N/A"));
			return adjusted;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyPendingPositiveNotableRelationsForFinalAftermath failed: " + ex.Message);
			return 0;
		}
	}

	private static int ApplyPendingPositiveNotableTrustForFinalAftermath(Settlement settlement, SiegeAftermathAction.SiegeAftermath aftermath)
	{
		try
		{
			int trustDelta = _pendingPositiveNotableTrustDelta;
			bool includeBoundVillages = _pendingPositiveNotableTrustIncludesBoundVillages;
			string reason = _pendingPositiveNotableTrustReason;
			_pendingPositiveNotableTrustDelta = 0;
			_pendingPositiveNotableTrustIncludesBoundVillages = false;
			_pendingPositiveNotableTrustReason = "";
			if (trustDelta <= 0)
			{
				return 0;
			}
			if (!SiegePositiveRelationTimingProfile.ShouldApplyQueuedPositiveRelations(ToStandaloneAftermathKind(aftermath)))
			{
				Logger.Log("SiegeAiIntervention", "Skipped queued positive notable trust because final aftermath is not mercy. Aftermath=" + aftermath + ", Delta=" + trustDelta);
				return 0;
			}
			int adjusted = includeBoundVillages
				? AdjustSettlementAndBoundVillageNotableTrust(settlement, trustDelta, reason)
				: AdjustSettlementNotableTrust(settlement, trustDelta, reason);
			Logger.Log("SiegeAiIntervention", "Applied queued positive notable trust after final mercy aftermath. Delta=" + trustDelta + ", IncludeBoundVillages=" + includeBoundVillages + ", Adjusted=" + adjusted + ", Reason=" + (reason ?? "N/A"));
			return adjusted;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyPendingPositiveNotableTrustForFinalAftermath failed: " + ex.Message);
			return 0;
		}
	}

	private static void AdjustSettlementPublicTrustOnly(Settlement settlement, int publicTrustDelta, string reason)
	{
		try
		{
			if (settlement != null && RewardSystemBehavior.Instance != null)
			{
				RewardSystemBehavior.Instance.AdjustSettlementLocalPublicTrustForExternal(settlement, publicTrustDelta, reason);
			}
		}
		catch
		{
		}
	}

	private static void MarkPendingAftermath(SiegeAftermathAction.SiegeAftermath aftermath, string triggerSource, string triggerDetail)
	{
		SiegeAftermathResolutionKind requestedAftermath = ToStandaloneAftermathKind(aftermath);
		if (SiegeAftermathSelectionPolicy.ShouldReturnSharedReliefPool(requestedAftermath))
		{
			ReturnSharedCivilianReliefPoolToPlayerForNegativeOutcome(triggerSource ?? aftermath.ToString());
		}
		if (SiegeAftermathSelectionPolicy.ShouldReplacePendingAftermath(requestedAftermath, ToStandaloneAftermathKind(_pendingAftermath), _hasPendingAftermath, _massacreStarted, _culturalRepopulationRequested))
		{
			ResetOutcomeMessageDedupForTrack(aftermath.ToString());
			_pendingAftermath = aftermath;
			_pendingAftermathTrigger = (triggerSource ?? "").Trim();
			_pendingAftermathDetail = (triggerDetail ?? "").Trim();
		}
		_hasPendingAftermath = true;
	}

	private static void ResetOutcomeMessageDedup()
	{
		OutcomeMessageDeduplicator.Reset();
	}

	private static void ResetOutcomeMessageDedupForTrack(string track)
	{
		OutcomeMessageDeduplicator.ResetForTrack(track);
	}

	private static void ShowOutcomeMessageOnce(string key, string message, uint color)
	{
		try
		{
			if (!OutcomeMessageDeduplicator.ShouldShow(key, message))
			{
				return;
			}
			InformationManager.DisplayMessage(new InformationMessage(message, Color.FromUint(color)));
		}
		catch
		{
		}
	}

	private static int GetAftermathSeverity(SiegeAftermathAction.SiegeAftermath aftermath)
	{
		return SiegeAftermathSelectionPolicy.GetSeverity(ToStandaloneAftermathKind(aftermath));
	}

	private static SiegeAftermathResolutionKind ToStandaloneAftermathKind(SiegeAftermathAction.SiegeAftermath aftermath)
	{
		return aftermath switch
		{
			SiegeAftermathAction.SiegeAftermath.Devastate => SiegeAftermathResolutionKind.Devastate,
			SiegeAftermathAction.SiegeAftermath.Pillage => SiegeAftermathResolutionKind.Pillage,
			SiegeAftermathAction.SiegeAftermath.ShowMercy => SiegeAftermathResolutionKind.ShowMercy,
			_ => SiegeAftermathResolutionKind.Unknown
		};
	}

	private static SiegeAftermathAction.SiegeAftermath ToNativeAftermathKind(SiegeAftermathResolutionKind aftermath)
	{
		return aftermath switch
		{
			SiegeAftermathResolutionKind.Devastate => SiegeAftermathAction.SiegeAftermath.Devastate,
			SiegeAftermathResolutionKind.Pillage => SiegeAftermathAction.SiegeAftermath.Pillage,
			SiegeAftermathResolutionKind.ShowMercy => SiegeAftermathAction.SiegeAftermath.ShowMercy,
			_ => SiegeAftermathAction.SiegeAftermath.ShowMercy
		};
	}

	private static void ApplyPendingMarketLootForFinalAftermath(SiegeAftermathAction.SiegeAftermath aftermath)
	{
		try
		{
			if (aftermath == SiegeAftermathAction.SiegeAftermath.Pillage)
			{
				if (!_marketGoldLootApplied)
				{
					_marketGoldLootApplied = true;
					LootSettlementMarketGold(SiegeLootAccountingProfile.PlunderSettlementLootReason);
				}
				if (!_marketGoodsLootAppliedForPlunder)
				{
					_marketGoodsLootAppliedForPlunder = true;
					LootSettlementMarketInventory(SiegeLootAccountingProfile.PlunderMarketInventoryMinRatio, SiegeLootAccountingProfile.PlunderMarketInventoryMaxRatio, SiegeLootAccountingProfile.PlunderSettlementLootReason);
				}
			}
			else if (aftermath == SiegeAftermathAction.SiegeAftermath.Devastate)
			{
				if (!_marketGoldLootApplied)
				{
					_marketGoldLootApplied = true;
					LootSettlementMarketGold(SiegeLootAccountingProfile.MassacreSettlementLootReason);
				}
				if (!_marketGoodsLootAppliedForMassacre)
				{
					_marketGoodsLootAppliedForMassacre = true;
					LootSettlementMarketInventory(SiegeLootAccountingProfile.MassacreMarketInventoryMinRatio, SiegeLootAccountingProfile.MassacreMarketInventoryMaxRatio, SiegeLootAccountingProfile.MassacreSettlementLootReason);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyPendingMarketLootForFinalAftermath failed: " + ex.Message);
		}
	}

	private static bool FinalizePendingAftermath(string reason)
	{
		if (!_hasPendingAftermath)
		{
			return false;
		}
		Settlement settlement = ResolveCurrentSettlement();
		if (settlement == null || settlement.Town == null)
		{
			return false;
		}
		try
		{
			CaptureNativeSiegeContext(settlement);
			MobileParty attackerParty = _besiegerParty ?? MobileParty.MainParty;
			Clan previousOwner = _previousSettlementOwnerClan ?? settlement.OwnerClan;
			if (previousOwner?.Leader == null)
			{
				previousOwner = (settlement.OwnerClan?.Leader != null) ? settlement.OwnerClan : attackerParty?.ActualClan;
			}
			Dictionary<MobileParty, float> contributions = BuildSafePartyContributions(attackerParty);
			SiegeAftermathAction.SiegeAftermath aftermath = _pendingAftermath;
			if (aftermath == SiegeAftermathAction.SiegeAftermath.Pillage || aftermath == SiegeAftermathAction.SiegeAftermath.Devastate)
			{
				ReturnSharedCivilianReliefPoolToPlayerForNegativeOutcome(reason ?? aftermath.ToString());
			}
			float prosperityBefore = settlement.Town.Prosperity;
			SiegeAftermathAction.ApplyAftermath(attackerParty, settlement, aftermath, previousOwner, contributions);
			ApplySoldierAppeasementMoralePenaltyIfNeeded(aftermath);
			if (aftermath == SiegeAftermathAction.SiegeAftermath.Devastate)
			{
				if (_culturalRepopulationRequested)
				{
					ApplyFinalizedSettlementOutcomeEffects(settlement, SiegeSettlementOutcomeProfile.BuildCulturalRepopulation(), prosperityBefore);
					ApplyCulturalRepopulationNow(SiegeCulturalRepopulationProfile.FinalizeAftermathApplySource);
				}
				else if (_massacreStarted)
				{
					ApplyFinalizedSettlementOutcomeEffects(settlement, SiegeSettlementOutcomeProfile.BuildMassacre(), prosperityBefore);
				}
				_nativeDevastateAftermathFlowActive = true;
				_nativeDevastateSummaryContinueHandled = false;
			}
			if (aftermath == SiegeAftermathAction.SiegeAftermath.Pillage && _plunderStarted)
			{
				ApplyFinalizedSettlementOutcomeEffects(settlement, SiegeSettlementOutcomeProfile.BuildPlunder(), prosperityBefore);
			}
			ApplyPendingPositiveNotableRelationsForFinalAftermath(settlement, aftermath);
			ApplyPendingPositiveNotableTrustForFinalAftermath(settlement, aftermath);
			CommitPendingInterventionNotableDeaths(SiegeNotableSceneDeathProfile.SettlementResolutionKillReason);
			ApplyPendingMarketLootForFinalAftermath(aftermath);
			TrySetNativePlayerEncounterAftermathForSummary(aftermath);
			MyBehavior.Instance?.RecordAnimusForgeSiegeInterventionForExternal(attackerParty, settlement, aftermath, previousOwner, _pendingAftermathTrigger, _pendingAftermathDetail, Math.Min(AutoSummonCount, CountHealthyMainPartySoldiers()), _lastLootItemTotal, _lastLootStackKinds, _lastLootValue, _lastMarketGoldLoot, _lastCivilianGoldLoot, _lastCivilianTargetsLooted, _lastKilledCivilianUnits, _lastKilledNotables, _plunderStarted, _massacreStarted);
			_pendingSummaryAftermath = aftermath;
			MarkAftermathResolvedForCompletion(settlement, aftermath);
			PrepareCompletedInterventionSummary(aftermath);
			_hasPendingAftermath = false;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "FinalizePendingAftermath failed: " + ex);
			return false;
		}
	}

	private static Dictionary<MobileParty, float> BuildSafePartyContributions(MobileParty attackerParty)
	{
		Dictionary<MobileParty, float> result = new Dictionary<MobileParty, float>();
		try
		{
			if (_partyContributions != null)
			{
				foreach (KeyValuePair<MobileParty, float> item in _partyContributions)
				{
					if (item.Key != null && item.Value > 0f && !result.ContainsKey(item.Key))
					{
						result.Add(item.Key, item.Value);
					}
				}
			}
			if (result.Count == 0 && attackerParty != null)
			{
				result[attackerParty] = 100f;
			}
		}
		catch
		{
		}
		return result;
	}

	private static void ApplyFinalizedSettlementOutcomeEffects(Settlement settlement, SiegeSettlementOutcomeProfile profile, float prosperityBeforeNativeAftermath)
	{
		try
		{
			if (settlement == null || profile == null)
			{
				return;
			}
			AdjustSettlementPublicTrustOnly(settlement, profile.SettlementPublicTrustDelta, profile.SettlementPublicTrustReason);
			int villageTrustAdjusted = AdjustBoundVillagePublicTrust(settlement, profile.BoundVillagePublicTrustDelta, profile.BoundVillagePublicTrustReason);
			int notableRelationAdjusted = AdjustSettlementAndBoundVillageNotableRelations(settlement, profile.NotableRelationDelta, profile.NotableRelationReason);
			int notableTrustAdjusted = AdjustSettlementAndBoundVillageNotableTrust(settlement, profile.NotableTrustDelta, profile.NotableTrustReason);
			float extraProsperityDelta = 0f;
			if (profile.DoublesNativeDevastateProsperityPenalty)
			{
				extraProsperityDelta = ApplyExtraNativeDevastateProsperityPenalty(settlement, prosperityBeforeNativeAftermath, SiegeSettlementOutcomeProfile.CulturalRepopulationNativeDevastateProsperityMultiplier);
			}
			if (profile.ResetsLoyaltyToInitial && settlement.Town != null)
			{
				settlement.Town.Loyalty = SiegeSettlementOutcomeProfile.CulturalRepopulationInitialLoyalty;
			}
			if (profile.AppliesProsperityGrowthDebuff)
			{
				BeginRepopulationProsperityGrowthDebuff(settlement);
			}
			if (profile.SuppressesRecruitment)
			{
				BeginRecruitmentSuppressionDebuff(settlement, profile);
			}
			Logger.Log("SiegeAiIntervention", $"Applied finalized GCCZ settlement outcome. Key={profile.Key}, Settlement={settlement.StringId}, SettlementTrust={profile.SettlementPublicTrustDelta}, VillageTrust={profile.BoundVillagePublicTrustDelta}x{villageTrustAdjusted}, NotableRelation={profile.NotableRelationDelta}x{notableRelationAdjusted}, NotableTrust={profile.NotableTrustDelta}x{notableTrustAdjusted}, ExtraProsperityDelta={extraProsperityDelta:0.##}");
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyFinalizedSettlementOutcomeEffects failed: " + ex.Message);
		}
	}

	private static int AdjustBoundVillagePublicTrust(Settlement settlement, int publicTrustDelta, string reason)
	{
		int adjusted = 0;
		try
		{
			if (settlement?.BoundVillages == null || publicTrustDelta == 0 || RewardSystemBehavior.Instance == null)
			{
				return 0;
			}
			HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (Village village in settlement.BoundVillages)
			{
				Settlement villageSettlement = village?.Settlement;
				string key = villageSettlement?.StringId;
				if (villageSettlement == null || string.IsNullOrWhiteSpace(key) || !seen.Add(key))
				{
					continue;
				}
				RewardSystemBehavior.Instance.AdjustSettlementLocalPublicTrustForExternal(villageSettlement, publicTrustDelta, reason ?? "siege_ai_bound_village");
				adjusted++;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "AdjustBoundVillagePublicTrust failed: " + ex.Message);
		}
		return adjusted;
	}

	private static int AdjustSettlementAndBoundVillageNotableRelations(Settlement settlement, int relationDelta, string reason)
	{
		int adjusted = 0;
		try
		{
			if (settlement == null || relationDelta == 0)
			{
				return 0;
			}
			HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (Hero notable in EnumerateSettlementAndBoundVillageNotables(settlement))
			{
				string key = notable?.StringId;
				if (notable == null || notable == Hero.MainHero || !notable.IsAlive || string.IsNullOrWhiteSpace(key) || !seen.Add(key))
				{
					continue;
				}
				try
				{
					ChangeRelationAction.ApplyPlayerRelation(notable, relationDelta, true, true);
					adjusted++;
				}
				catch (Exception ex)
				{
					Logger.Log("SiegeAiIntervention", "Notable relation adjustment failed. Reason=" + (reason ?? "N/A") + ", Notable=" + (key ?? "N/A") + ": " + ex.Message);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "AdjustSettlementAndBoundVillageNotableRelations failed: " + ex.Message);
		}
		return adjusted;
	}

	private static int AdjustSettlementNotableTrust(Settlement settlement, int trustDelta, string reason)
	{
		int adjusted = 0;
		try
		{
			if (settlement?.Notables == null || trustDelta == 0 || RewardSystemBehavior.Instance == null)
			{
				return 0;
			}
			HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (Hero notable in settlement.Notables.ToList())
			{
				string key = notable?.StringId;
				if (notable == null || notable == Hero.MainHero || !notable.IsAlive || string.IsNullOrWhiteSpace(key) || !seen.Add(key))
				{
					continue;
				}
				try
				{
					RewardSystemBehavior.Instance.AdjustPersonalTrustWholeDeltaForExternal(notable, trustDelta, reason ?? "siege_ai_notable_trust");
					adjusted++;
				}
				catch (Exception ex)
				{
					Logger.Log("SiegeAiIntervention", "Settlement notable trust adjustment failed. Reason=" + (reason ?? "N/A") + ", Notable=" + key + ": " + ex.Message);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "AdjustSettlementNotableTrust failed: " + ex.Message);
		}
		return adjusted;
	}

	private static int AdjustSettlementAndBoundVillageNotableTrust(Settlement settlement, int trustDelta, string reason)
	{
		int adjusted = 0;
		try
		{
			if (settlement == null || trustDelta == 0 || RewardSystemBehavior.Instance == null)
			{
				return 0;
			}
			HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (Hero notable in EnumerateSettlementAndBoundVillageNotables(settlement))
			{
				string key = notable?.StringId;
				if (notable == null || notable == Hero.MainHero || !notable.IsAlive || string.IsNullOrWhiteSpace(key) || !seen.Add(key))
				{
					continue;
				}
				try
				{
					RewardSystemBehavior.Instance.AdjustPersonalTrustWholeDeltaForExternal(notable, trustDelta, reason ?? "siege_ai_notable_trust");
					adjusted++;
				}
				catch (Exception ex)
				{
					Logger.Log("SiegeAiIntervention", "Notable trust adjustment failed. Reason=" + (reason ?? "N/A") + ", Notable=" + key + ": " + ex.Message);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "AdjustSettlementAndBoundVillageNotableTrust failed: " + ex.Message);
		}
		return adjusted;
	}

	private static IEnumerable<Hero> EnumerateSettlementAndBoundVillageNotables(Settlement settlement)
	{
		if (settlement?.Notables != null)
		{
			foreach (Hero notable in settlement.Notables)
			{
				yield return notable;
			}
		}
		if (settlement?.BoundVillages == null)
		{
			yield break;
		}
		foreach (Village village in settlement.BoundVillages)
		{
			if (village?.Settlement?.Notables == null)
			{
				continue;
			}
			foreach (Hero notable in village.Settlement.Notables)
			{
				yield return notable;
			}
		}
	}

	private static float ApplyExtraNativeDevastateProsperityPenalty(Settlement settlement, float prosperityBeforeNativeAftermath, float totalMultiplier)
	{
		try
		{
			if (settlement?.Town == null || totalMultiplier <= 1f)
			{
				return 0f;
			}
			float nativeDelta = settlement.Town.Prosperity - prosperityBeforeNativeAftermath;
			if (nativeDelta >= 0f)
			{
				return 0f;
			}
			float extraDelta = nativeDelta * (totalMultiplier - 1f);
			settlement.Town.Prosperity = MathF.Max(0f, settlement.Town.Prosperity + extraDelta);
			return extraDelta;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyExtraNativeDevastateProsperityPenalty failed: " + ex.Message);
			return 0f;
		}
	}

	private static void BeginRepopulationProsperityGrowthDebuff(Settlement settlement)
	{
		try
		{
			string key = settlement?.StringId;
			if (string.IsNullOrWhiteSpace(key) || settlement?.Town == null)
			{
				return;
			}
			int untilDay = GetCurrentCampaignDay() + Math.Max(1, CampaignTime.DaysInYear * SiegeSettlementOutcomeProfile.CulturalRepopulationProsperityGrowthDebuffYears);
			_repopulationProsperityDebuffUntilDayBySettlement[key] = untilDay;
			_repopulationProsperityLastObservedBySettlement[key] = settlement.Town.Prosperity;
			Logger.Log("SiegeAiIntervention", $"Applied repopulation prosperity growth debuff. Settlement={key}, UntilDay={untilDay}, Reduction={SiegeSettlementOutcomeProfile.CulturalRepopulationProsperityGrowthReductionRatio:P0}");
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "BeginRepopulationProsperityGrowthDebuff failed: " + ex.Message);
		}
	}

	private static void ApplyRepopulationProsperityGrowthDebuff(Town town)
	{
		try
		{
			Settlement settlement = town?.Settlement;
			string key = settlement?.StringId;
			if (string.IsNullOrWhiteSpace(key))
			{
				return;
			}
			int today = GetCurrentCampaignDay();
			if (!_repopulationProsperityDebuffUntilDayBySettlement.TryGetValue(key, out int untilDay) || today > untilDay)
			{
				_repopulationProsperityDebuffUntilDayBySettlement.Remove(key);
				_repopulationProsperityLastObservedBySettlement.Remove(key);
				return;
			}
			float current = town.Prosperity;
			if (!_repopulationProsperityLastObservedBySettlement.TryGetValue(key, out float last))
			{
				_repopulationProsperityLastObservedBySettlement[key] = current;
				return;
			}
			float growth = current - last;
			if (growth > 0.01f)
			{
				float reduction = growth * SiegeSettlementOutcomeProfile.CulturalRepopulationProsperityGrowthReductionRatio;
				town.Prosperity = MathF.Max(0f, current - reduction);
				current = town.Prosperity;
				Logger.Log("SiegeAiIntervention", $"Repopulation prosperity growth debuff applied. Settlement={key}, Growth={growth:0.##}, Reduction={reduction:0.##}, UntilDay={untilDay}");
			}
			_repopulationProsperityLastObservedBySettlement[key] = current;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyRepopulationProsperityGrowthDebuff failed: " + ex.Message);
		}
	}

	private static void ClearRepopulationProsperityDebuffs()
	{
		_repopulationProsperityDebuffUntilDayBySettlement.Clear();
		_repopulationProsperityLastObservedBySettlement.Clear();
	}

	private static void BeginRecruitmentSuppressionDebuff(Settlement settlement, SiegeSettlementOutcomeProfile profile)
	{
		try
		{
			string key = settlement?.StringId;
			if (string.IsNullOrWhiteSpace(key) || settlement?.Town == null || profile == null || !profile.SuppressesRecruitment)
			{
				return;
			}
			int untilDay = GetCurrentCampaignDay() + Math.Max(1, CampaignTime.DaysInYear * profile.RecruitmentSuppressionYears);
			_recruitmentSuppressionUntilDayBySettlement[key] = untilDay;
			int cleared = ClearVolunteerSlotsForSettlementAndBoundVillages(settlement);
			Logger.Log("SiegeAiIntervention", $"Applied recruitment suppression debuff. Settlement={key}, UntilDay={untilDay}, ClearedSlots={cleared}, Reason={profile.RecruitmentSuppressionReason ?? "N/A"}");
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "BeginRecruitmentSuppressionDebuff failed: " + ex.Message);
		}
	}

	private static void ApplyRecruitmentSuppressionDebuff(Town town)
	{
		try
		{
			Settlement settlement = town?.Settlement;
			string key = settlement?.StringId;
			if (string.IsNullOrWhiteSpace(key))
			{
				return;
			}
			int today = GetCurrentCampaignDay();
			if (!_recruitmentSuppressionUntilDayBySettlement.TryGetValue(key, out int untilDay) || today > untilDay)
			{
				_recruitmentSuppressionUntilDayBySettlement.Remove(key);
				return;
			}
			int cleared = ClearVolunteerSlotsForSettlementAndBoundVillages(settlement);
			if (cleared > 0)
			{
				Logger.Log("SiegeAiIntervention", $"Recruitment suppression debuff applied. Settlement={key}, ClearedSlots={cleared}, UntilDay={untilDay}");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyRecruitmentSuppressionDebuff failed: " + ex.Message);
		}
	}

	private static int ClearVolunteerSlotsForSettlementAndBoundVillages(Settlement settlement)
	{
		int cleared = ClearVolunteerSlotsForSettlement(settlement);
		try
		{
			if (settlement?.BoundVillages != null)
			{
				foreach (Village village in settlement.BoundVillages)
				{
					cleared += ClearVolunteerSlotsForSettlement(village?.Settlement);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ClearVolunteerSlotsForSettlementAndBoundVillages failed: " + ex.Message);
		}
		return cleared;
	}

	private static int ClearVolunteerSlotsForSettlement(Settlement settlement)
	{
		int cleared = 0;
		try
		{
			if (settlement?.Notables == null)
			{
				return 0;
			}
			foreach (Hero notable in settlement.Notables.ToList())
			{
				CharacterObject[] volunteerTypes = notable?.VolunteerTypes;
				if (notable == null || !notable.IsAlive || volunteerTypes == null)
				{
					continue;
				}
				for (int i = 0; i < volunteerTypes.Length; i++)
				{
					if (volunteerTypes[i] == null)
					{
						continue;
					}
					volunteerTypes[i] = null;
					cleared++;
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ClearVolunteerSlotsForSettlement failed. Settlement=" + (settlement?.StringId ?? "N/A") + ": " + ex.Message);
		}
		return cleared;
	}

	private static void ClearRecruitmentSuppressionDebuffs()
	{
		_recruitmentSuppressionUntilDayBySettlement.Clear();
	}


	private static void BeginCivicPositiveBuff(Settlement settlement, SiegeCivicChoiceProfile profile)
	{
		try
		{
			string key = settlement?.StringId;
			if (string.IsNullOrWhiteSpace(key) || settlement?.Town == null || profile == null || profile.EffectYears <= 0)
			{
				return;
			}
			int untilDay = GetCurrentCampaignDay() + Math.Max(1, CampaignTime.DaysInYear * profile.EffectYears);
			if (profile.HasProsperityGrowthBuff)
			{
				_civicProsperityBuffUntilDayBySettlement[key] = untilDay;
				_civicProsperityLastObservedBySettlement[key] = settlement.Town.Prosperity;
				_civicProsperityGrowthMultiplierBySettlement[key] = MathF.Max(profile.ProsperityGrowthMultiplier, ResolveExistingCivicProsperityMultiplier(key));
			}
			if (profile.LocksLoyalty)
			{
				float adjustedLockValue = ReducePositiveFloatDeltaForRegionalConflict(profile.LoyaltyLockValue, "civic_buff_loyalty_lock");
				_rallyOathLoyaltyLockUntilDayBySettlement[key] = untilDay;
				_rallyOathLoyaltyLockValueBySettlement[key] = adjustedLockValue;
				settlement.Town.Loyalty = MathF.Max(settlement.Town.Loyalty, adjustedLockValue);
			}
			if (profile.HasRecruitmentSpeedBuff)
			{
				_rallyOathRecruitmentBuffUntilDayBySettlement[key] = untilDay;
				int changed = ApplyExtraVolunteerProductionForSettlementAndBoundVillages(settlement);
				if (changed > 0)
				{
					Logger.Log("SiegeAiIntervention", $"Applied immediate rally oath recruitment speed buff. Settlement={key}, ChangedSlots={changed}, UntilDay={untilDay}");
				}
			}
			Logger.Log("SiegeAiIntervention", $"Applied civic positive buff. Settlement={key}, UntilDay={untilDay}, ProsperityMultiplier={profile.ProsperityGrowthMultiplier:0.##}, RecruitmentMultiplier={profile.RecruitmentSpeedMultiplier:0.##}, LoyaltyLock={profile.LocksLoyalty}, RegionalConflictIncidents={_regionalConflictIncidentCount}");
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "BeginCivicPositiveBuff failed: " + ex.Message);
		}
	}

	private static float ResolveExistingCivicProsperityMultiplier(string key)
	{
		try
		{
			return !string.IsNullOrWhiteSpace(key) && _civicProsperityGrowthMultiplierBySettlement.TryGetValue(key, out float multiplier)
				? multiplier
				: 1f;
		}
		catch
		{
			return 1f;
		}
	}

	private static void ApplyCivicProsperityGrowthBuff(Town town)
	{
		try
		{
			Settlement settlement = town?.Settlement;
			string key = settlement?.StringId;
			if (string.IsNullOrWhiteSpace(key))
			{
				return;
			}
			int today = GetCurrentCampaignDay();
			if (!_civicProsperityBuffUntilDayBySettlement.TryGetValue(key, out int untilDay) || today > untilDay)
			{
				_civicProsperityBuffUntilDayBySettlement.Remove(key);
				_civicProsperityLastObservedBySettlement.Remove(key);
				_civicProsperityGrowthMultiplierBySettlement.Remove(key);
				return;
			}
			float current = town.Prosperity;
			if (!_civicProsperityLastObservedBySettlement.TryGetValue(key, out float last))
			{
				_civicProsperityLastObservedBySettlement[key] = current;
				return;
			}
			float growth = current - last;
			float multiplier = MathF.Max(1f, ResolveExistingCivicProsperityMultiplier(key));
			if (growth > 0.01f && multiplier > 1.001f)
			{
				float extra = growth * (multiplier - 1f);
				town.Prosperity = MathF.Max(0f, current + extra);
				current = town.Prosperity;
				Logger.Log("SiegeAiIntervention", $"Civic prosperity growth buff applied. Settlement={key}, Growth={growth:0.##}, Extra={extra:0.##}, Multiplier={multiplier:0.##}, UntilDay={untilDay}");
			}
			_civicProsperityLastObservedBySettlement[key] = current;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyCivicProsperityGrowthBuff failed: " + ex.Message);
		}
	}

	private static void ApplyRallyOathLoyaltyLock(Town town)
	{
		try
		{
			Settlement settlement = town?.Settlement;
			string key = settlement?.StringId;
			if (string.IsNullOrWhiteSpace(key))
			{
				return;
			}
			int today = GetCurrentCampaignDay();
			if (!_rallyOathLoyaltyLockUntilDayBySettlement.TryGetValue(key, out int untilDay) || today > untilDay)
			{
				_rallyOathLoyaltyLockUntilDayBySettlement.Remove(key);
				_rallyOathLoyaltyLockValueBySettlement.Remove(key);
				return;
			}
			float lockValue = _rallyOathLoyaltyLockValueBySettlement.TryGetValue(key, out float savedLockValue)
				? savedLockValue
				: SiegeCivicChoiceProfile.RallyOathLoyaltyValue;
			town.Loyalty = MathF.Max(town.Loyalty, lockValue);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyRallyOathLoyaltyLock failed: " + ex.Message);
		}
	}

	private static void ApplyRallyOathRecruitmentBuff(Town town)
	{
		try
		{
			Settlement settlement = town?.Settlement;
			string key = settlement?.StringId;
			if (string.IsNullOrWhiteSpace(key))
			{
				return;
			}
			int today = GetCurrentCampaignDay();
			if (!_rallyOathRecruitmentBuffUntilDayBySettlement.TryGetValue(key, out int untilDay) || today > untilDay)
			{
				_rallyOathRecruitmentBuffUntilDayBySettlement.Remove(key);
				return;
			}
			int changed = ApplyExtraVolunteerProductionForSettlementAndBoundVillages(settlement);
			if (changed > 0)
			{
				Logger.Log("SiegeAiIntervention", $"Rally oath recruitment speed buff applied. Settlement={key}, ChangedSlots={changed}, UntilDay={untilDay}");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyRallyOathRecruitmentBuff failed: " + ex.Message);
		}
	}

	private static int ApplyExtraVolunteerProductionForSettlementAndBoundVillages(Settlement settlement)
	{
		int changed = ApplyExtraVolunteerProductionForSettlement(settlement);
		try
		{
			if (settlement?.BoundVillages != null)
			{
				foreach (Village village in settlement.BoundVillages)
				{
					changed += ApplyExtraVolunteerProductionForSettlement(village?.Settlement);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyExtraVolunteerProductionForSettlementAndBoundVillages failed. Settlement=" + (settlement?.StringId ?? "N/A") + ": " + ex.Message);
		}
		return changed;
	}

	private static int ApplyExtraVolunteerProductionForSettlement(Settlement settlement)
	{
		int changed = 0;
		try
		{
			if (settlement?.Notables == null || Campaign.Current?.Models?.VolunteerModel == null)
			{
				return 0;
			}
			if (settlement.IsTown && settlement.Town?.InRebelliousState == true)
			{
				return 0;
			}
			if (settlement.IsVillage && settlement.Village?.Bound?.Town?.InRebelliousState == true)
			{
				return 0;
			}
			foreach (Hero hero in settlement.Notables.ToList())
			{
				changed += ApplyExtraVolunteerProductionForNotable(hero, settlement);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyExtraVolunteerProductionForSettlement failed. Settlement=" + (settlement?.StringId ?? "N/A") + ": " + ex.Message);
		}
		return changed;
	}

	private static int ApplyExtraVolunteerProductionForNotable(Hero notable, Settlement settlement)
	{
		int changed = 0;
		try
		{
			if (notable == null || !notable.IsAlive || !notable.CanHaveRecruits || notable.VolunteerTypes == null || settlement == null)
			{
				return 0;
			}
			CharacterObject basicVolunteer = Campaign.Current.Models.VolunteerModel.GetBasicVolunteer(notable);
			int slots = Math.Min(6, notable.VolunteerTypes.Length);
			for (int i = 0; i < slots; i++)
			{
				if (MBRandom.RandomFloat >= Campaign.Current.Models.VolunteerModel.GetDailyVolunteerProductionProbability(notable, i, settlement))
				{
					continue;
				}
				CharacterObject current = notable.VolunteerTypes[i];
				if (current == null)
				{
					notable.VolunteerTypes[i] = basicVolunteer;
					changed++;
				}
				else if (current.UpgradeTargets != null && current.UpgradeTargets.Length != 0 && current.Tier < Campaign.Current.Models.VolunteerModel.MaxVolunteerTier)
				{
					float upgradeProbability = MathF.Log(MathF.Max(1f, notable.Power) / MathF.Max(1f, (float)current.Tier), 2f) * 0.01f;
					if (MBRandom.RandomFloat < upgradeProbability)
					{
						notable.VolunteerTypes[i] = current.UpgradeTargets[MBRandom.RandomInt(current.UpgradeTargets.Length)];
						changed++;
					}
				}
			}
			if (changed > 0)
			{
				SortVolunteerSlots(notable.VolunteerTypes, slots);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyExtraVolunteerProductionForNotable failed. Notable=" + (notable?.StringId ?? "N/A") + ": " + ex.Message);
		}
		return changed;
	}

	private static void SortVolunteerSlots(CharacterObject[] volunteerTypes, int slots)
	{
		try
		{
			for (int j = 1; j < slots; j++)
			{
				CharacterObject character = volunteerTypes[j];
				if (character == null)
				{
					continue;
				}
				int emptySlots = 0;
				int previousIndex = j - 1;
				CharacterObject previous = volunteerTypes[previousIndex];
				while (previousIndex >= 0 && (previous == null || GetVolunteerSortValue(character) < GetVolunteerSortValue(previous)))
				{
					if (previous == null)
					{
						previousIndex--;
						emptySlots++;
						if (previousIndex >= 0)
						{
							previous = volunteerTypes[previousIndex];
						}
					}
					else
					{
						volunteerTypes[previousIndex + 1 + emptySlots] = previous;
						previousIndex--;
						emptySlots = 0;
						if (previousIndex >= 0)
						{
							previous = volunteerTypes[previousIndex];
						}
					}
				}
				volunteerTypes[previousIndex + 1 + emptySlots] = character;
			}
		}
		catch
		{
		}
	}

	private static float GetVolunteerSortValue(CharacterObject character)
	{
		return character == null ? float.MaxValue : character.Level + (character.IsMounted ? 0.5f : 0f);
	}

	private static void ClearCivicPositiveBuffForSettlement(Settlement settlement)
	{
		try
		{
			string key = settlement?.StringId;
			if (string.IsNullOrWhiteSpace(key))
			{
				return;
			}
			_civicProsperityBuffUntilDayBySettlement.Remove(key);
			_civicProsperityLastObservedBySettlement.Remove(key);
			_civicProsperityGrowthMultiplierBySettlement.Remove(key);
			_rallyOathLoyaltyLockUntilDayBySettlement.Remove(key);
			_rallyOathLoyaltyLockValueBySettlement.Remove(key);
			_rallyOathRecruitmentBuffUntilDayBySettlement.Remove(key);
		}
		catch
		{
		}
	}

	private static void ClearCivicPositiveBuffs()
	{
		_civicProsperityBuffUntilDayBySettlement.Clear();
		_civicProsperityLastObservedBySettlement.Clear();
		_civicProsperityGrowthMultiplierBySettlement.Clear();
		_rallyOathLoyaltyLockUntilDayBySettlement.Clear();
		_rallyOathLoyaltyLockValueBySettlement.Clear();
		_rallyOathRecruitmentBuffUntilDayBySettlement.Clear();
	}

	private static int CommitPendingInterventionNotableDeaths(string reason)
	{
		int killed = 0;
		try
		{
			if (PendingInterventionNotableDeaths.Count == 0)
			{
				return 0;
			}
			foreach (Hero notable in PendingInterventionNotableDeaths.ToList())
			{
				if (!SiegeNotableSceneDeathProfile.ShouldKillAtSettlementResolution(activeGcczStage: true, wasKnockedDownInScene: notable != null))
				{
					continue;
				}
				if (TryKillInterventionNotableAtSettlementResolution(notable, reason))
				{
					killed++;
				}
			}
			PendingInterventionNotableDeaths.Clear();
			_lastKilledNotables += killed;
			if (killed > 0)
			{
				Logger.Log("SiegeAiIntervention", "Committed pending GCCZ notable deaths at settlement resolution. Killed=" + killed + ", Reason=" + (reason ?? "N/A"));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "CommitPendingInterventionNotableDeaths failed: " + ex.Message);
		}
		return killed;
	}

	private static bool TryKillInterventionNotableAtSettlementResolution(Hero notable, string reason)
	{
		try
		{
			if (!IsInterventionNotableHero(notable))
			{
				return false;
			}
			try
			{
				KillCharacterAction.ApplyByBattle(notable, Hero.MainHero, true);
			}
			catch
			{
				KillCharacterAction.ApplyByMurder(notable, Hero.MainHero, false);
			}
			if (notable.IsAlive)
			{
				KillCharacterAction.ApplyByRemove(notable, false, true);
			}
			bool killed = !notable.IsAlive;
			Logger.Log("SiegeAiIntervention", "Resolved GCCZ notable knockdown death. Hero=" + (notable.StringId ?? "N/A") + ", Killed=" + killed + ", Reason=" + (reason ?? "N/A"));
			return killed;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryKillInterventionNotableAtSettlementResolution failed. Notable=" + (notable?.StringId ?? "N/A") + ": " + ex.Message);
			return false;
		}
	}


	private static int GetCurrentCampaignDay()
	{
		try
		{
			return Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
		}
		catch
		{
			return 0;
		}
	}

	private static void CaptureNativeSiegeContext(Settlement settlement)
	{
		try
		{
			SiegeAftermathCampaignBehavior behavior = Campaign.Current?.GetCampaignBehavior<SiegeAftermathCampaignBehavior>();
			if (behavior == null)
			{
				_besiegerParty = _besiegerParty ?? MobileParty.MainParty;
				_previousSettlementOwnerClan = _previousSettlementOwnerClan ?? settlement?.OwnerClan;
				return;
			}
			Type type = typeof(SiegeAftermathCampaignBehavior);
			_besiegerParty = ReadPrivateField<MobileParty>(behavior, type, "_besiegerParty") ?? _besiegerParty ?? MobileParty.MainParty;
			_previousSettlementOwnerClan = ReadPrivateField<Clan>(behavior, type, "_prevSettlementOwnerClan") ?? _previousSettlementOwnerClan ?? settlement?.OwnerClan;
			Dictionary<MobileParty, float> contributions = ReadPrivateField<Dictionary<MobileParty, float>>(behavior, type, "_siegeEventPartyContributions");
			if (contributions != null && contributions.Count > 0)
			{
				_partyContributions = new Dictionary<MobileParty, float>(contributions);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "CaptureNativeSiegeContext failed: " + ex.Message);
			_besiegerParty = _besiegerParty ?? MobileParty.MainParty;
			_previousSettlementOwnerClan = _previousSettlementOwnerClan ?? settlement?.OwnerClan;
		}
	}

	private static T ReadPrivateField<T>(object instance, Type type, string fieldName) where T : class
	{
		try
		{
			return type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance) as T;
		}
		catch
		{
			return null;
		}
	}

	private static void TrySetNativePlayerEncounterAftermathForSummary(SiegeAftermathAction.SiegeAftermath aftermath)
	{
		try
		{
			SiegeAftermathCampaignBehavior behavior = Campaign.Current?.GetCampaignBehavior<SiegeAftermathCampaignBehavior>();
			if (behavior == null)
			{
				return;
			}
			typeof(SiegeAftermathCampaignBehavior).GetField("_playerEncounterAftermath", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(behavior, aftermath);
			typeof(SiegeAftermathCampaignBehavior).GetField("_besiegerParty", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(behavior, _besiegerParty ?? MobileParty.MainParty);
			typeof(SiegeAftermathCampaignBehavior).GetField("_prevSettlementOwnerClan", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(behavior, _previousSettlementOwnerClan ?? ResolveCurrentSettlement()?.OwnerClan);
			typeof(SiegeAftermathCampaignBehavior).GetField("_siegeEventPartyContributions", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(behavior, BuildSafePartyContributions(_besiegerParty ?? MobileParty.MainParty));
		}
		catch
		{
		}
	}

	private static void MarkAftermathResolvedForCompletion(Settlement settlement, SiegeAftermathAction.SiegeAftermath aftermath)
	{
		try
		{
			_afAftermathResolved = true;
			_completedAftermath = aftermath;
			_completedSettlementId = settlement?.StringId ?? _activeSettlementId ?? "";
			_completedSettlementName = settlement?.Name?.ToString() ?? _activeSettlementName ?? "";
			Logger.Log("SiegeAiIntervention", $"Marked AF siege aftermath resolved. Settlement={_completedSettlementId}, Aftermath={aftermath}");
		}
		catch
		{
			_afAftermathResolved = true;
			_completedAftermath = aftermath;
		}
	}

	private static void PrepareCompletedInterventionSummary(SiegeAftermathAction.SiegeAftermath aftermath)
	{
		try
		{
			string settlementName = string.IsNullOrWhiteSpace(_completedSettlementName) ? _activeSettlementName : _completedSettlementName;
			if (string.IsNullOrWhiteSpace(settlementName))
			{
				settlementName = ResolveCurrentSettlement()?.Name?.ToString() ?? "这座定居点";
			}
			bool culturalRepopulationApplied = _culturalRepopulationRequested || _culturalRepopulationApplied;
			string targetCultureText = "";
			if (culturalRepopulationApplied)
			{
				CultureObject targetCulture = ResolveCulturalRepopulationTargetCulture(out string targetCultureSource);
				targetCultureText = DescribeCultureForMessage(targetCulture, targetCultureSource);
			}
			_completedSummaryText = SiegeCompletedInterventionSummaryBuilder.Build(new SiegeCompletedInterventionSummaryFacts(
				settlementName,
				ToStandaloneAftermathKind(aftermath),
				culturalRepopulationApplied,
				_massacreStarted,
				_plunderStarted,
				targetCultureText,
				_lastLootItemTotal,
				_lastLootStackKinds,
				_lastLootValue,
				_lastMarketGoldLoot,
				_lastCivilianGoldLoot,
				_lastCivilianTargetsLooted));
		}
		catch
		{
			_completedSummaryText = SiegeInterventionCompletionUiProfile.CompletedSummaryFallbackText;
		}
	}

	private static void FinishPlayerEncounterAfterIntervention(SiegeAftermathAction.SiegeAftermath aftermath)
	{
		try
		{
			QueueEncounterFinishAfterIntervention(aftermath, SiegeAftermathTransitionSourceProfile.DoneMenuContinueFinishSource, 0, forceDelay: true);
			TryFinishPlayerEncounterAfterInterventionNow(aftermath, SiegeAftermathTransitionSourceProfile.DoneMenuContinueFinishSource);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "FinishPlayerEncounterAfterIntervention failed: " + ex.Message);
		}
	}

	private static void QueueEncounterFinishAfterIntervention(SiegeAftermathAction.SiegeAftermath aftermath, string reason, int delayTicks, bool forceDelay)
	{
		try
		{
			if (!_pendingEncounterFinish)
			{
				_pendingEncounterFinish = true;
				_pendingEncounterFinishAttempts = 0;
				_pendingEncounterFinishMessageShown = false;
			}
			_pendingEncounterFinishAftermath = aftermath;
			if (forceDelay || _pendingEncounterFinishDelayTicks <= 0)
			{
				_pendingEncounterFinishDelayTicks = Math.Max(0, delayTicks);
			}
			Logger.Log("SiegeAiIntervention", "Queued AF siege encounter finish. Reason=" + (reason ?? "N/A") + ", Aftermath=" + aftermath + ", DelayTicks=" + _pendingEncounterFinishDelayTicks);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "QueueEncounterFinishAfterIntervention failed: " + ex.Message);
		}
	}

	private static bool TryFinishPlayerEncounterAfterInterventionNow(SiegeAftermathAction.SiegeAftermath aftermath, string source)
	{
		try
		{
			if (!_pendingEncounterFinish)
			{
				QueueEncounterFinishAfterIntervention(aftermath, source, 0, forceDelay: true);
			}
			if (_pendingEncounterFinishDelayTicks > 0)
			{
				_pendingEncounterFinishDelayTicks--;
				return false;
			}
			ShowEncounterFinishMessagesOnce(aftermath);
			_pendingEncounterFinishAttempts++;
			try
			{
				PlayerEncounter.LeaveEncounter = true;
			}
			catch
			{
			}
			try
			{
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounter.Current.IsPlayerWaiting = false;
				}
			}
			catch
			{
			}
			try
			{
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounter.Finish(true);
					Logger.Log("SiegeAiIntervention", "Requested PlayerEncounter.Finish(true) after AF intervention. Source=" + (source ?? "N/A") + ", Attempt=" + _pendingEncounterFinishAttempts);
				}
			}
			catch (Exception ex)
			{
				Logger.Log("SiegeAiIntervention", "PlayerEncounter.Finish after AF intervention failed. Source=" + (source ?? "N/A") + ", Error=" + ex.Message);
			}
			if (PlayerEncounter.Current == null)
			{
				Logger.Log("SiegeAiIntervention", "AF siege encounter finish completed. Source=" + (source ?? "N/A"));
				return true;
			}
			if (_pendingEncounterFinishAttempts >= 3)
			{
				try
				{
					GameMenu.ExitToLast();
					Logger.Log("SiegeAiIntervention", "Fallback GameMenu.ExitToLast after AF intervention finish attempts. Source=" + (source ?? "N/A") + ", Attempt=" + _pendingEncounterFinishAttempts);
				}
				catch
				{
				}
			}
			return false;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryFinishPlayerEncounterAfterInterventionNow failed. Source=" + (source ?? "N/A") + ", Error=" + ex.Message);
			return false;
		}
	}

	private static void QueueDirectMassacreAftermathScript(string reason)
	{
		try
		{
			_directMassacreAftermathScriptPending = true;
			_directMassacreLootScreenOpened = false;
			_directMassacreWaitingForLootClose = false;
			_directMassacreScriptMessageShown = false;
			_directMassacreScriptTicks = 0;
			_directMassacreLastDeferKey = "";
			_pendingSummarySwitch = true;
			_pendingEncounterFinish = false;
			_pendingEncounterFinishDelayTicks = 0;
			_pendingEncounterFinishAttempts = 0;
			_nativeDevastateAftermathFlowActive = false;
			_nativeDevastateSummaryContinueHandled = true;
			Logger.Log("SiegeAiIntervention", "Queued direct AF massacre aftermath script. Reason=" + (reason ?? "N/A") + ", LootItems=" + (_pendingLootRoster?.Count ?? 0) + ", MarketGold=" + _lastMarketGoldLoot + ", CivilianGold=" + _lastCivilianGoldLoot);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "QueueDirectMassacreAftermathScript failed: " + ex.Message);
		}
	}

	private static bool TryRunDirectMassacreAftermathScript(string source = SiegeDirectAftermathSourceProfile.CampaignTickDirectMassacreScriptSource)
	{
		if (!_directMassacreAftermathScriptPending || Mission.Current != null)
		{
			return false;
		}
		try
		{
			_directMassacreScriptTicks++;
			if (_hasPendingAftermath)
			{
				FinalizePendingAftermath(SiegeDirectAftermathSourceProfile.DirectMassacrePendingAftermathSource);
			}
			if (!_afAftermathResolved)
			{
				return true;
			}
			string pumpSource = source ?? SiegeDirectAftermathSourceProfile.DirectMassacreFallbackPumpSource;
			if (!IsSafeToOpenDirectMassacreLootScreen(pumpSource))
			{
				return true;
			}
			_nativeDevastateAftermathFlowActive = false;
			_nativeDevastateSummaryContinueHandled = true;
			try
			{
				if (Campaign.Current?.CurrentMenuContext != null)
				{
					GameMenu.ExitToLast();
				}
			}
			catch
			{
			}
			if (_directMassacreWaitingForLootClose)
			{
				if (Game.Current?.GameStateManager?.ActiveState is InventoryState)
				{
					return true;
				}
				_directMassacreWaitingForLootClose = false;
				QueueEncounterFinishAfterIntervention(SiegeAftermathAction.SiegeAftermath.Devastate, SiegeDirectAftermathSourceProfile.DirectMassacreAfterLootSource, 0, forceDelay: true);
				if (!TryFinishPlayerEncounterAfterInterventionNow(SiegeAftermathAction.SiegeAftermath.Devastate, SiegeDirectAftermathSourceProfile.DirectMassacreAfterLootSource))
				{
					return true;
				}
				_directMassacreAftermathScriptPending = false;
				_pendingSummarySwitch = false;
				ClearActiveState(preserveSummarySwitch: false);
				Logger.Log("SiegeAiIntervention", "Direct AF massacre aftermath script completed after loot screen.");
				return true;
			}
			if (!_directMassacreLootScreenOpened && _pendingLootRoster != null && _pendingLootRoster.Count > 0)
			{
				TryOpenDirectMassacreLootScreenNow(pumpSource);
				return true;
			}
			ShowDirectMassacreLootMessage();
			QueueEncounterFinishAfterIntervention(SiegeAftermathAction.SiegeAftermath.Devastate, SiegeDirectAftermathSourceProfile.DirectMassacreNoLootSource, 0, forceDelay: true);
			if (!TryFinishPlayerEncounterAfterInterventionNow(SiegeAftermathAction.SiegeAftermath.Devastate, SiegeDirectAftermathSourceProfile.DirectMassacreNoLootSource))
			{
				return true;
			}
			_directMassacreAftermathScriptPending = false;
			_pendingSummarySwitch = false;
			ClearActiveState(preserveSummarySwitch: false);
			Logger.Log("SiegeAiIntervention", "Direct AF massacre aftermath script completed without loot screen.");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryRunDirectMassacreAftermathScript failed: " + ex.Message);
			return true;
		}
	}

	private static void QueueDirectPlunderAftermathScript(string reason)
	{
		try
		{
			_directPlunderAftermathScriptPending = true;
			_directPlunderLootScreenOpened = false;
			_directPlunderWaitingForLootClose = false;
			_directPlunderScriptMessageShown = false;
			_directPlunderScriptTicks = 0;
			_directPlunderLastDeferKey = "";
			_pendingSummarySwitch = true;
			_pendingEncounterFinish = false;
			_pendingEncounterFinishDelayTicks = 0;
			_pendingEncounterFinishAttempts = 0;
			Logger.Log("SiegeAiIntervention", "Queued direct AF plunder aftermath loot script. Reason=" + (reason ?? "N/A") + ", LootItems=" + (_pendingLootRoster?.Count ?? 0) + ", MarketGold=" + _lastMarketGoldLoot + ", CivilianGold=" + _lastCivilianGoldLoot);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "QueueDirectPlunderAftermathScript failed: " + ex.Message);
		}
	}

	private static bool TryRunDirectPlunderAftermathScript(string source = SiegeDirectAftermathSourceProfile.CampaignTickDirectPlunderScriptSource)
	{
		if (!_directPlunderAftermathScriptPending || Mission.Current != null)
		{
			return false;
		}
		try
		{
			_directPlunderScriptTicks++;
			if (_hasPendingAftermath)
			{
				FinalizePendingAftermath(SiegeDirectAftermathSourceProfile.DirectPlunderPendingAftermathSource);
			}
			if (!_afAftermathResolved)
			{
				return true;
			}
			string pumpSource = source ?? SiegeDirectAftermathSourceProfile.DirectPlunderFallbackPumpSource;
			if (!IsSafeToOpenDirectPlunderLootScreen(pumpSource))
			{
				return true;
			}
			try
			{
				if (Campaign.Current?.CurrentMenuContext != null)
				{
					GameMenu.ExitToLast();
				}
			}
			catch
			{
			}
			if (_directPlunderWaitingForLootClose)
			{
				if (Game.Current?.GameStateManager?.ActiveState is InventoryState)
				{
					return true;
				}
				_directPlunderWaitingForLootClose = false;
				QueueEncounterFinishAfterIntervention(SiegeAftermathAction.SiegeAftermath.Pillage, SiegeDirectAftermathSourceProfile.DirectPlunderAfterLootSource, 0, forceDelay: true);
				if (!TryFinishPlayerEncounterAfterInterventionNow(SiegeAftermathAction.SiegeAftermath.Pillage, SiegeDirectAftermathSourceProfile.DirectPlunderAfterLootSource))
				{
					return true;
				}
				_directPlunderAftermathScriptPending = false;
				_pendingSummarySwitch = false;
				ClearActiveState(preserveSummarySwitch: false);
				Logger.Log("SiegeAiIntervention", "Direct AF plunder aftermath loot script completed after loot screen.");
				return true;
			}
			if (!_directPlunderLootScreenOpened && _pendingLootRoster != null && _pendingLootRoster.Count > 0)
			{
				TryOpenDirectPlunderLootScreenNow(pumpSource);
				return true;
			}
			ShowDirectPlunderLootMessage();
			QueueEncounterFinishAfterIntervention(SiegeAftermathAction.SiegeAftermath.Pillage, SiegeDirectAftermathSourceProfile.DirectPlunderNoLootSource, 0, forceDelay: true);
			if (!TryFinishPlayerEncounterAfterInterventionNow(SiegeAftermathAction.SiegeAftermath.Pillage, SiegeDirectAftermathSourceProfile.DirectPlunderNoLootSource))
			{
				return true;
			}
			_directPlunderAftermathScriptPending = false;
			_pendingSummarySwitch = false;
			ClearActiveState(preserveSummarySwitch: false);
			Logger.Log("SiegeAiIntervention", "Direct AF plunder aftermath loot script completed without loot screen.");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryRunDirectPlunderAftermathScript failed: " + ex.Message);
			return true;
		}
	}

	internal static bool TryPumpDirectPlunderAftermathScriptForExternal(string source)
	{
		try
		{
			if (!_directPlunderAftermathScriptPending)
			{
				return false;
			}
			return TryRunDirectPlunderAftermathScript(source ?? SiegeDirectAftermathSourceProfile.ExternalDirectPlunderScriptSource);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryPumpDirectPlunderAftermathScriptForExternal failed. Source=" + (source ?? "N/A") + ", Error=" + ex.Message);
			return true;
		}
	}

	private static bool TryOpenDirectMassacreLootScreenNow(string source)
	{
		try
		{
			if (!_directMassacreAftermathScriptPending || _directMassacreLootScreenOpened || _pendingLootRoster == null || _pendingLootRoster.Count <= 0)
			{
				return false;
			}
			if (!IsSafeToOpenDirectMassacreLootScreen(source))
			{
				return false;
			}
			_directMassacreLootScreenOpened = true;
			_directMassacreWaitingForLootClose = true;
			_pendingLootScreen = true;
			_pendingLootScreenShown = true;
			_directMassacreLastDeferKey = "";
			_nativeDevastateAftermathFlowActive = false;
			_nativeDevastateSummaryContinueHandled = true;
			ShowDirectMassacreLootMessage();
			try
			{
				if (Campaign.Current?.CurrentMenuContext != null)
				{
					GameMenu.ExitToLast();
				}
			}
			catch
			{
			}
			InventoryScreenHelper.OpenScreenAsLoot(new Dictionary<PartyBase, ItemRoster>
			{
				{
					PartyBase.MainParty,
					_pendingLootRoster
				}
			});
			Logger.Log("SiegeAiIntervention", "Direct AF massacre script opened loot screen immediately. Source=" + (source ?? "N/A") + ", LootItems=" + _pendingLootRoster.Count + ", MarketGold=" + _lastMarketGoldLoot + ", CivilianGold=" + _lastCivilianGoldLoot);
			return true;
		}
		catch (Exception ex)
		{
			_directMassacreLootScreenOpened = false;
			_directMassacreWaitingForLootClose = false;
			_pendingLootScreenShown = false;
			Logger.Log("SiegeAiIntervention", "TryOpenDirectMassacreLootScreenNow failed. Source=" + (source ?? "N/A") + ", Error=" + ex.Message);
			return false;
		}
	}

	private static bool TryOpenDirectPlunderLootScreenNow(string source)
	{
		try
		{
			if (!_directPlunderAftermathScriptPending || _directPlunderLootScreenOpened || _pendingLootRoster == null || _pendingLootRoster.Count <= 0)
			{
				return false;
			}
			if (!IsSafeToOpenDirectPlunderLootScreen(source))
			{
				return false;
			}
			_directPlunderLootScreenOpened = true;
			_directPlunderWaitingForLootClose = true;
			_pendingLootScreen = true;
			_pendingLootScreenShown = true;
			_directPlunderLastDeferKey = "";
			ShowDirectPlunderLootMessage();
			try
			{
				if (Campaign.Current?.CurrentMenuContext != null)
				{
					GameMenu.ExitToLast();
				}
			}
			catch
			{
			}
			InventoryScreenHelper.OpenScreenAsLoot(new Dictionary<PartyBase, ItemRoster>
			{
				{
					PartyBase.MainParty,
					_pendingLootRoster
				}
			});
			Logger.Log("SiegeAiIntervention", "Direct AF plunder script opened loot screen immediately. Source=" + (source ?? "N/A") + ", LootItems=" + _pendingLootRoster.Count + ", MarketGold=" + _lastMarketGoldLoot + ", CivilianGold=" + _lastCivilianGoldLoot);
			return true;
		}
		catch (Exception ex)
		{
			_directPlunderLootScreenOpened = false;
			_directPlunderWaitingForLootClose = false;
			_pendingLootScreenShown = false;
			Logger.Log("SiegeAiIntervention", "TryOpenDirectPlunderLootScreenNow failed. Source=" + (source ?? "N/A") + ", Error=" + ex.Message);
			return false;
		}
	}

	private static bool IsSafeToOpenDirectMassacreLootScreen(string source)
	{
		try
		{
			if (Mission.Current != null)
			{
				LogDirectMassacreLootDeferOnce(SiegeDirectAftermathSourceProfile.MissionCurrentLootDeferSource, "Direct massacre loot screen deferred because Mission.Current is still active. Source=" + (source ?? "N/A"));
				return false;
			}
			object activeState = Game.Current?.GameStateManager?.ActiveState;
			if (activeState == null)
			{
				LogDirectMassacreLootDeferOnce(SiegeDirectAftermathSourceProfile.NullStateLootDeferSource, "Direct massacre loot screen deferred because active game state is null. Source=" + (source ?? "N/A"));
				return false;
			}
			if (activeState is InventoryState)
			{
				return false;
			}
			if (activeState is MapState)
			{
				return true;
			}
			string stateName = activeState.GetType().FullName ?? activeState.GetType().Name;
			LogDirectMassacreLootDeferOnce(SiegeDirectAftermathSourceProfile.BuildActiveStateLootDeferSource(stateName), "Direct massacre loot screen deferred until MapState. Source=" + (source ?? "N/A") + ", ActiveState=" + stateName);
			return false;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "IsSafeToOpenDirectMassacreLootScreen failed. Source=" + (source ?? "N/A") + ", Error=" + ex.Message);
			return false;
		}
	}

	private static bool IsSafeToOpenDirectPlunderLootScreen(string source)
	{
		try
		{
			if (Mission.Current != null)
			{
				LogDirectPlunderLootDeferOnce(SiegeDirectAftermathSourceProfile.MissionCurrentLootDeferSource, "Direct plunder loot screen deferred because Mission.Current is still active. Source=" + (source ?? "N/A"));
				return false;
			}
			object activeState = Game.Current?.GameStateManager?.ActiveState;
			if (activeState == null)
			{
				LogDirectPlunderLootDeferOnce(SiegeDirectAftermathSourceProfile.NullStateLootDeferSource, "Direct plunder loot screen deferred because active game state is null. Source=" + (source ?? "N/A"));
				return false;
			}
			if (activeState is InventoryState)
			{
				return false;
			}
			if (activeState is MapState)
			{
				return true;
			}
			string stateName = activeState.GetType().FullName ?? activeState.GetType().Name;
			LogDirectPlunderLootDeferOnce(SiegeDirectAftermathSourceProfile.BuildActiveStateLootDeferSource(stateName), "Direct plunder loot screen deferred until MapState. Source=" + (source ?? "N/A") + ", ActiveState=" + stateName);
			return false;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "IsSafeToOpenDirectPlunderLootScreen failed. Source=" + (source ?? "N/A") + ", Error=" + ex.Message);
			return false;
		}
	}

	private static void LogDirectMassacreLootDeferOnce(string key, string message)
	{
		try
		{
			if (string.Equals(_directMassacreLastDeferKey, key ?? "", StringComparison.Ordinal))
			{
				return;
			}
			_directMassacreLastDeferKey = key ?? "";
			Logger.Log("SiegeAiIntervention", message);
		}
		catch
		{
		}
	}

	private static void LogDirectPlunderLootDeferOnce(string key, string message)
	{
		try
		{
			if (string.Equals(_directPlunderLastDeferKey, key ?? "", StringComparison.Ordinal))
			{
				return;
			}
			_directPlunderLastDeferKey = key ?? "";
			Logger.Log("SiegeAiIntervention", message);
		}
		catch
		{
		}
	}

	private static void ShowDirectMassacreLootMessage()
	{
		if (_directMassacreScriptMessageShown)
		{
			return;
		}
		if (_culturalRepopulationRequested && !_culturalRepopulationApplied)
		{
			ApplyCulturalRepopulationNow(SiegeCulturalRepopulationProfile.DirectMassacreLootMessageApplySource);
		}
		_directMassacreScriptMessageShown = true;
		try
		{
			string action = _culturalRepopulationRequested || _culturalRepopulationApplied ? SiegeLootAccountingProfile.CulturalRepopulationActionName : SiegeLootAccountingProfile.MassacreActionName;
			InformationManager.DisplayMessage(new InformationMessage(SiegeLootAccountingProfile.BuildDirectDevastateSettlementMessage(action), Color.FromUint(SiegeLootAccountingProfile.DirectDevastateSettlementMessageColor)));
			InformationManager.DisplayMessage(new InformationMessage(SiegeLootAccountingProfile.BuildLootCreditedSummaryMessage(_lastMarketGoldLoot, _lastCivilianGoldLoot, _lastLootItemTotal, _lastLootStackKinds), Color.FromUint(SiegeLootAccountingProfile.LootMessageColor)));
		}
		catch
		{
		}
	}

	private static void ShowDirectPlunderLootMessage()
	{
		if (_directPlunderScriptMessageShown)
		{
			return;
		}
		_directPlunderScriptMessageShown = true;
		try
		{
			InformationManager.DisplayMessage(new InformationMessage(SiegeLootAccountingProfile.BuildDirectPlunderSettlementMessage(), Color.FromUint(SiegeLootAccountingProfile.DirectPlunderSettlementMessageColor)));
			InformationManager.DisplayMessage(new InformationMessage(SiegeLootAccountingProfile.BuildLootCreditedSummaryMessage(_lastMarketGoldLoot, _lastCivilianGoldLoot, _lastLootItemTotal, _lastLootStackKinds), Color.FromUint(SiegeLootAccountingProfile.LootMessageColor)));
		}
		catch
		{
		}
	}

	private static void ShowEncounterFinishMessagesOnce(SiegeAftermathAction.SiegeAftermath aftermath)
	{
		if (_pendingEncounterFinishMessageShown)
		{
			return;
		}
		_pendingEncounterFinishMessageShown = true;
		try
		{
			InformationManager.DisplayMessage(new InformationMessage(SiegeInterventionCompletionUiProfile.BuildCompletedEncounterMessage(ToStandaloneAftermathKind(aftermath), _culturalRepopulationRequested || _culturalRepopulationApplied), Color.FromUint(SiegeInterventionCompletionUiProfile.CompletionMessageColor)));
			if (_lastLootItemTotal > 0 || _lastMarketGoldLoot > 0 || _lastCivilianGoldLoot > 0)
			{
				InformationManager.DisplayMessage(new InformationMessage(SiegeInterventionCompletionUiProfile.BuildLootSettlementSummaryMessage(_lastLootItemTotal, _lastLootStackKinds, _lastMarketGoldLoot, _lastCivilianGoldLoot), Color.FromUint(SiegeInterventionCompletionUiProfile.CompletionMessageColor)));
			}
		}
		catch
		{
		}
		try
		{
			MBInformationManager.AddQuickInformation(new TextObject(SiegeInterventionCompletionUiProfile.LeaveEncounterQuickText), 0, null, null, "event:/ui/mission/arena_victory");
		}
		catch
		{
		}
	}

	private static Settlement ResolveCurrentSettlement()
	{
		return ResolveLiveCurrentSettlement() ?? _activeSettlement;
	}

	private static Settlement ResolveLiveCurrentSettlement()
	{
		try
		{
			if (Settlement.CurrentSettlement != null)
			{
				return Settlement.CurrentSettlement;
			}
		}
		catch
		{
		}
		try
		{
			if (PlayerEncounter.EncounterSettlement != null)
			{
				return PlayerEncounter.EncounterSettlement;
			}
		}
		catch
		{
		}
		try
		{
			if (PlayerEncounter.LocationEncounter?.Settlement != null)
			{
				return PlayerEncounter.LocationEncounter.Settlement;
			}
		}
		catch
		{
		}
		return null;
	}

	private static bool DoesLiveCurrentSettlementMatchActiveIntervention()
	{
		try
		{
			if (string.IsNullOrWhiteSpace(_activeSettlementId))
			{
				return false;
			}
			Settlement settlement = ResolveLiveCurrentSettlement();
			return settlement != null && string.Equals(settlement.StringId, _activeSettlementId, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	private static Agent TryGetAgent(int agentIndex)
	{
		try
		{
			if (agentIndex < 0 || Mission.Current?.Agents == null)
			{
				return null;
			}
			return Mission.Current.Agents.FirstOrDefault(a => a != null && a.Index == agentIndex);
		}
		catch
		{
			return null;
		}
	}

	private static string StripSiegeTags(string text)
	{
		return AnySiegeTagRegex.Replace(text ?? "", "").Trim();
	}

	private static string BuildTargetKey(Agent agent)
	{
		CharacterObject character = agent?.Character as CharacterObject;
		Hero hero = character?.HeroObject;
		string id = hero?.StringId ?? character?.StringId ?? "agent";
		return (_activeSettlementId ?? "") + ":" + id + ":" + (agent?.Index ?? -1);
	}

	private static int CountHealthyMainPartySoldiers()
	{
		try
		{
			TroopRoster roster = PartyBase.MainParty?.MemberRoster;
			if (roster == null)
			{
				return 0;
			}
			int total = 0;
			for (int i = 0; i < roster.Count; i++)
			{
				TroopRosterElement element = roster.GetElementCopyAtIndex(i);
				CharacterObject character = element.Character;
				if (character != null && !character.IsHero && character != CharacterObject.PlayerCharacter)
				{
					total += Math.Max(0, element.Number - element.WoundedNumber);
				}
			}
			return total;
		}
		catch
		{
			return 0;
		}
	}

	private static bool IsActiveInCurrentMission()
	{
		if (_activeMode == InterventionMode.None || Mission.Current == null)
		{
			return false;
		}
		Settlement settlement = ResolveCurrentSettlement();
		if (!string.IsNullOrWhiteSpace(_activeSettlementId) && settlement != null && !string.Equals(settlement.StringId, _activeSettlementId, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		return true;
	}

	private static bool ShouldForceDirectPlayerMountedSpawn()
	{
		try
		{
			Mission mission = Mission.Current;
			if (mission == null || mission.IsMissionEnding)
			{
				return false;
			}
			if (IsActiveInCurrentMission())
			{
				return true;
			}
			if (_pendingMode == InterventionMode.None)
			{
				return false;
			}
			return DoesLiveCurrentSettlementMatchActiveIntervention();
		}
		catch
		{
			return false;
		}
	}

	private static void ResetSessionCounters()
	{
		_alliedTroopsAutoSummoned = false;
		_nextControlTickTime = 0f;
		_nextPlunderTickTime = 0f;
		_playerBattleEquipmentApplied = false;
		_plunderStarted = false;
		_massacreStarted = false;
		_massacreVictoryReached = false;
		_civilianSpeechRallyActive = false;
		_civilianGatherPropagationActive = false;
		_civilianFormationControlPending = false;
		_civilianFormationControlComplete = false;
		_civilianFormationControlMessageShown = false;
		_soldierDefaultFollowOrderIssued = false;
		_playerOrderControllerPrimed = false;
		_civilianOrderControllerPrimed = false;
		_selectedInterventionRoster = null;
		_civilianGatherStartedAt = -1f;
		_nextCivilianGatherTickTime = 0f;
		_civilianGatherMessengerSpeechBudget = 0;
		_civilianGatherMessengerSpeechCount = 0;
		_civilianFormationControlNotBeforeTime = -1f;
		_nextCivilianFormationControlBatchTime = 0f;
		_nextPlayerOrderControllerPrimeTime = 0f;
		_culturalRepopulationRequested = false;
		_culturalRepopulationApplied = false;
		_reliefChoiceApplied = false;
		_inspirationLevelApplied = 0;
		_soldierAppeasementCheckDone = false;
		_soldierAppeasementRequired = false;
		_soldierAppeasementApplied = false;
		_soldierAppeasementMoralePenaltyApplied = false;
		_pendingPositiveNotableRelationDelta = 0;
		_pendingPositiveNotableRelationIncludesBoundVillages = false;
		_pendingPositiveNotableRelationReason = "";
		_pendingPositiveNotableTrustDelta = 0;
		_pendingPositiveNotableTrustIncludesBoundVillages = false;
		_pendingPositiveNotableTrustReason = "";
		_regionalConflictIncidentCount = 0;
		RegionalConflictDebtCenters.Clear();
		_lastMassacreRealKillMissionTime = -100f;
		_lastDestructiveInquiryMissionTime = -100f;
		_lastDestructiveInquirySourceAgentIndex = -1;
		_lastAmbientCivilianReactionMissionTime = -100f;
		_lastAmbientSoldierReactionMissionTime = -100f;
		_nextAmbientReactionRequestMissionTime = -100f;
		PendingAmbientReactionRequests.Clear();
		_hasPendingAftermath = false;
		_pendingAftermathTrigger = "";
		_pendingAftermathDetail = "";
		_marketGoodsLootAppliedForPlunder = false;
		_marketGoodsLootAppliedForMassacre = false;
		_marketGoldLootApplied = false;
		_lastLootItemTotal = 0;
		_lastLootStackKinds = 0;
		_lastLootValue = 0;
		_lastMarketGoldLoot = 0;
		_lastCivilianGoldLoot = 0;
		_lastCivilianTargetsLooted = 0;
		_civilianRobberyTargetsLooted = 0;
		_civilianRobberyPenaltyLevelApplied = 0;
		_lastSceneCivilianSpawnedCount = 0;
		_lastKilledCivilianUnits = 0;
		_lastKilledNotables = 0;
		_sharedCivilianReliefGold = 0;
		_sharedCivilianReliefFoodUnits = 0;
		_sharedCivilianReliefItemTotal = 0;
		_sharedCivilianReliefItemValue = 0L;
		_appliedSharedCivilianReliefGold = 0;
		_appliedSharedCivilianReliefFoodUnits = 0;
		_appliedSharedCivilianReliefItemValue = 0L;
		_sharedCivilianReliefReturned = false;
		SharedCivilianReliefItems.Clear();
		SharedCivilianReliefItemObjects.Clear();
		InterventionMemoryEvents.Clear();
		_interventionMemorySequence = 0;
		_pendingLootRoster = new ItemRoster();
		_pendingLootScreen = false;
		_pendingLootScreenShown = false;
		_directPlunderAftermathScriptPending = false;
		_directPlunderLootScreenOpened = false;
		_directPlunderWaitingForLootClose = false;
		_directPlunderScriptMessageShown = false;
		_directPlunderScriptTicks = 0;
		_directPlunderLastDeferKey = "";
		ResetOutcomeMessageDedup();
		ActivePlunderInteractions.Clear();
		ActiveCivilianGatherInteractions.Clear();
		LootedTargets.Clear();
		CivilianRobberyTargets.Clear();
		AlliedAgentIndexes.Clear();
		BannerBearerAgentIndexes.Clear();
		CountedMassacreVictims.Clear();
		PendingInterventionNotableDeaths.Clear();
		SceneCivilianAgentIndexes.Clear();
		VictoryCheerAgentIndexes.Clear();
		CordonReadyAgentIndexes.Clear();
		CivilianCalmedAgentIndexes.Clear();
		CivilianFrightenedActionAgentIndexes.Clear();
		CivilianPreMassacrePreparedAgentIndexes.Clear();
		ClearLocalPlayerAttackState();
		CivilianGatherMovePreparedAgentIndexes.Clear();
		CivilianGatherFollowerAgentIndexes.Clear();
		CivilianGatherReadyFormationAgentIndexes.Clear();
		CivilianGatherMessengerAgentIndexes.Clear();
		CivilianGatherMessengerSpeechAgentIndexes.Clear();
		CommandableOriginRuntimeIds.Clear();
		MassacreReadySoldierAgentIndexes.Clear();
		MassacreCombatPreparedAgentIndexes.Clear();
		CivilianSpeechRallySlots.Clear();
		LastCordonMoveOrderTimesBySoldier.Clear();
		LastCordonLookOrderTimesBySoldier.Clear();
		CivilianHideTargets.Clear();
		LastCivilianHideOrderTimes.Clear();
		CivilianHideSettledAgentIndexes.Clear();
		CivilianInteriorHidePointPool.Clear();
		CivilianEscapePointPool.Clear();
		_civilianRoutPointPoolSceneName = "";
		LastMassacreSoldierFollowOrderTimes.Clear();
		LastMassacreSoldierTargetOrderTimes.Clear();
		MassacreSoldierTargetAgentIndexes.Clear();
		MassacreSoldierTargetSlots.Clear();
		LastMassacreSoldierProbePositions.Clear();
		LastMassacreSoldierProbeTimes.Clear();
		LastCivilianGatherFollowOrderTimes.Clear();
		LastCivilianGatherFollowTargets.Clear();
		LastAgentWallRescueProbePositions.Clear();
		LastAgentWallRescueProbeTimes.Clear();
		AgentWallRescueUntilTimes.Clear();
		LastAgentWallRescueLogTimes.Clear();
		_civilianAssemblyPointReady = false;
		_civilianAssemblyMessageShown = false;
		_civilianAssemblyAnchor = Vec3.Zero;
		_civilianAssemblyForward = Vec3.Forward;
		_interventionCivilianEnemyTeam = null;
	}

	private static void ResetAftermathRuntimeGuards(string reason)
	{
		try
		{
			_activeMode = InterventionMode.None;
			_pendingMode = InterventionMode.None;
			_activeSettlementId = "";
			_activeSettlementName = "";
			_activeSettlement = null;
			_previousSettlementOwnerClan = null;
			_besiegerParty = null;
			_interventionPlayerCommandTeam = null;
			_interventionCivilianEnemyTeam = null;
			_partyContributions = new Dictionary<MobileParty, float>();
			_afAftermathResolved = false;
			_completedSettlementId = "";
			_completedSettlementName = "";
			_completedAftermath = SiegeAftermathAction.SiegeAftermath.ShowMercy;
			_completedSummaryText = "";
			_pendingSummarySwitch = false;
			_pendingEncounterFinish = false;
			_pendingEncounterFinishAftermath = SiegeAftermathAction.SiegeAftermath.ShowMercy;
			_pendingEncounterFinishDelayTicks = 0;
			_pendingEncounterFinishAttempts = 0;
			_pendingEncounterFinishMessageShown = false;
			_nativeDevastateAftermathFlowActive = false;
			_nativeDevastateSummaryContinueHandled = false;
			_directMassacreAftermathScriptPending = false;
			_directMassacreLootScreenOpened = false;
			_directMassacreWaitingForLootClose = false;
			_directMassacreScriptMessageShown = false;
			_directMassacreScriptTicks = 0;
			_directMassacreLastDeferKey = "";
			_directPlunderAftermathScriptPending = false;
			_directPlunderLootScreenOpened = false;
			_directPlunderWaitingForLootClose = false;
			_directPlunderScriptMessageShown = false;
			_directPlunderScriptTicks = 0;
			_directPlunderLastDeferKey = "";
			ResetOutcomeMessageDedup();
			ResetSessionCounters();
			Logger.Log("SiegeAiIntervention", "Reset AF siege aftermath runtime guards. Reason=" + (reason ?? "N/A"));
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ResetAftermathRuntimeGuards failed. Reason=" + (reason ?? "N/A") + ", Error=" + ex.Message);
		}
	}

	private static void ClearActiveState(bool preserveSummarySwitch)
	{
		_activeMode = InterventionMode.None;
		_pendingMode = InterventionMode.None;
		_activeSettlementId = "";
		_activeSettlementName = "";
		_previousSettlementOwnerClan = null;
		_besiegerParty = null;
		_activeSettlement = null;
		_interventionPlayerCommandTeam = null;
		_interventionCivilianEnemyTeam = null;
		_partyContributions = new Dictionary<MobileParty, float>();
		if (!preserveSummarySwitch)
		{
			_pendingSummarySwitch = false;
		}
		_pendingEncounterFinish = false;
		_pendingEncounterFinishDelayTicks = 0;
		_pendingEncounterFinishAttempts = 0;
		_pendingEncounterFinishMessageShown = false;
		_nativeDevastateAftermathFlowActive = false;
		_nativeDevastateSummaryContinueHandled = false;
		_directMassacreAftermathScriptPending = false;
		_directMassacreLootScreenOpened = false;
		_directMassacreWaitingForLootClose = false;
		_directMassacreScriptMessageShown = false;
		_directMassacreScriptTicks = 0;
		_directMassacreLastDeferKey = "";
		_directPlunderAftermathScriptPending = false;
		_directPlunderLootScreenOpened = false;
		_directPlunderWaitingForLootClose = false;
		_directPlunderScriptMessageShown = false;
		_directPlunderScriptTicks = 0;
		_directPlunderLastDeferKey = "";
		ResetOutcomeMessageDedup();
		ResetSessionCounters();
	}

	private static class SiegeInterventionCommandOriginPatch
	{
		private static bool _patched;

		internal static void EnsurePatched()
		{
			if (_patched)
			{
				return;
			}
			try
			{
				Harmony harmony = new Harmony("com.AnimusForge.siege_intervention.command_origin");
				MethodInfo partyPrefix = typeof(SiegeInterventionCommandOriginPatch).GetMethod(nameof(SafePartyAgentOriginUnderPlayerCommandPrefix), BindingFlags.Static | BindingFlags.NonPublic);
				MethodInfo forceMarkedPrefix = typeof(SiegeInterventionCommandOriginPatch).GetMethod(nameof(ForceMarkedOriginUnderPlayerCommandPrefix), BindingFlags.Static | BindingFlags.NonPublic);
				PatchGetter(harmony, typeof(PartyAgentOrigin), new HarmonyMethod(partyPrefix));
				PatchGetter(harmony, typeof(SimpleAgentOrigin), new HarmonyMethod(forceMarkedPrefix));
				PatchGetter(harmony, typeof(PartyGroupAgentOrigin), new HarmonyMethod(forceMarkedPrefix));
				_patched = true;
			}
			catch (Exception ex)
			{
				Logger.Log("SiegeAiIntervention", "SiegeInterventionCommandOriginPatch failed: " + ex.Message);
			}
		}

		private static void PatchGetter(Harmony harmony, Type originType, HarmonyMethod prefix)
		{
			try
			{
				MethodInfo getter = AccessTools.PropertyGetter(originType, nameof(IAgentOriginBase.IsUnderPlayersCommand)) ?? originType.GetProperty(nameof(IAgentOriginBase.IsUnderPlayersCommand), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetGetMethod(true);
				if (getter != null)
				{
					harmony.Patch(getter, prefix: prefix);
				}
			}
			catch (Exception ex)
			{
				Logger.Log("SiegeAiIntervention", "Patch IsUnderPlayersCommand getter failed for " + originType?.FullName + ": " + ex.Message);
			}
		}

		private static bool SafePartyAgentOriginUnderPlayerCommandPrefix(PartyAgentOrigin __instance, ref bool __result)
		{
			try
			{
				if (SiegeAiInterventionBehavior.ShouldForceCommandForOrigin(__instance))
				{
					__result = true;
					return false;
				}
				if (__instance == null)
				{
					__result = false;
					return false;
				}
				PartyBase party = __instance.Party;
				if (party == null)
				{
					__result = false;
					return false;
				}
				__result = party == PartyBase.MainParty || party.Owner == Hero.MainHero || (party.MapFaction != null && party.MapFaction.Leader == Hero.MainHero);
				return false;
			}
			catch
			{
				__result = false;
				return false;
			}
		}

		private static bool ForceMarkedOriginUnderPlayerCommandPrefix(object __instance, ref bool __result)
		{
			if (SiegeAiInterventionBehavior.ShouldForceCommandForOrigin(__instance))
			{
				__result = true;
				return false;
			}
			return true;
		}
	}

	private static class SiegeInterventionSceneTauntSuppressionPatch
	{
		private static bool _patched;

		internal static void EnsurePatched()
		{
			if (_patched)
			{
				return;
			}
			try
			{
				Harmony harmony = new Harmony("com.AnimusForge.siege_intervention.scene_taunt_suppression");
				PatchMovementOrderConstructors(harmony);
				PatchInterventionPlayerSpawn(harmony);
				MethodInfo prefix = typeof(SiegeInterventionSceneTauntSuppressionPatch).GetMethod(nameof(SuppressSceneTauntMissionBehaviorPrefix), BindingFlags.Static | BindingFlags.NonPublic);
				PatchSceneTauntMissionMethod(harmony, prefix, nameof(SceneTauntMissionBehavior.OnMissionTick));
				PatchSceneTauntMissionMethod(harmony, prefix, nameof(SceneTauntMissionBehavior.OnAgentHit));
				PatchSceneTauntMissionMethod(harmony, prefix, nameof(SceneTauntMissionBehavior.OnScoreHit));
				PatchSceneTauntMissionMethod(harmony, prefix, nameof(SceneTauntMissionBehavior.OnAgentRemoved));
				MethodInfo sceneTauntCrimePrefix = typeof(SiegeInterventionSceneTauntSuppressionPatch).GetMethod(nameof(SuppressSceneTauntCrimePrefix), BindingFlags.Static | BindingFlags.NonPublic);
				PatchSceneTauntStaticMethod(harmony, sceneTauntCrimePrefix, nameof(SceneTauntBehavior.QueueDeferredCrimeForExternal));
				PatchSceneTauntStaticMethod(harmony, sceneTauntCrimePrefix, nameof(SceneTauntBehavior.AddCrimeRefillReserveForExternal));
				PatchSceneTauntStaticMethod(harmony, sceneTauntCrimePrefix, nameof(SceneTauntBehavior.TryShowTrackedCrimeTotalMessageForExternal));
				MethodInfo crimePrefix = typeof(SiegeInterventionSceneTauntSuppressionPatch).GetMethod(nameof(SuppressCrimeRatingDuringInterventionPrefix), BindingFlags.Static | BindingFlags.NonPublic);
				MethodInfo crimeTarget = AccessTools.Method(typeof(ChangeCrimeRatingAction), "ApplyInternal");
				if (crimeTarget != null && crimePrefix != null)
				{
					harmony.Patch(crimeTarget, prefix: new HarmonyMethod(crimePrefix));
				}
				MethodInfo usableTargetPrefix = typeof(SiegeInterventionSceneTauntSuppressionPatch).GetMethod(nameof(SuppressCivilianUsableTargetPrefix), BindingFlags.Static | BindingFlags.NonPublic);
				MethodInfo usableTarget = AccessTools.Method(typeof(AgentNavigator), nameof(AgentNavigator.SetTarget), new Type[] { typeof(UsableMachine), typeof(bool), typeof(Agent.AIScriptedFrameFlags) });
				if (usableTarget != null && usableTargetPrefix != null)
				{
					harmony.Patch(usableTarget, prefix: new HarmonyMethod(usableTargetPrefix));
				}
				MethodInfo usableMovePrefix = typeof(SiegeInterventionSceneTauntSuppressionPatch).GetMethod(nameof(SuppressCivilianUsableMissionObjectPrefix), BindingFlags.Static | BindingFlags.NonPublic);
				MethodInfo usableMoveTarget = AccessTools.Method(typeof(UsableMissionObject), "OnAIMoveToUse", new Type[] { typeof(Agent), typeof(IDetachment) });
				if (usableMoveTarget != null && usableMovePrefix != null)
				{
					harmony.Patch(usableMoveTarget, prefix: new HarmonyMethod(usableMovePrefix));
				}
				MethodInfo orderViewsPostfix = typeof(SiegeInterventionSceneTauntSuppressionPatch).GetMethod(nameof(InjectInterventionOrderViewsPostfix), BindingFlags.Static | BindingFlags.NonPublic);
				PatchOrderViewsMethod(harmony, orderViewsPostfix, nameof(SandBoxMissionViews.OpenTownCenterMission));
				PatchOrderViewsMethod(harmony, orderViewsPostfix, nameof(SandBoxMissionViews.OpenTavernMission));
				PatchOrderViewsMethod(harmony, orderViewsPostfix, nameof(SandBoxMissionViews.OpenVillageMission));
				PatchOrderViewsMethod(harmony, orderViewsPostfix, nameof(SandBoxMissionViews.OpenTownMerchantMission));
				PatchOrderViewsMethod(harmony, orderViewsPostfix, nameof(SandBoxMissionViews.OpenAlleyMission));
				PatchOrderViewsMethod(harmony, orderViewsPostfix, nameof(SandBoxMissionViews.OpenBattleMissionWhileEnteringSettlement));
				MethodInfo orderControllerPrefix = typeof(SiegeInterventionSceneTauntSuppressionPatch).GetMethod(nameof(ProvideOrderTroopPlacerOrderControllerPrefix), BindingFlags.Static | BindingFlags.NonPublic);
				MethodInfo orderControllerGetter = AccessTools.PropertyGetter(typeof(OrderTroopPlacer), "OrderController");
				if (orderControllerGetter != null && orderControllerPrefix != null)
				{
					harmony.Patch(orderControllerGetter, prefix: new HarmonyMethod(orderControllerPrefix));
				}
				MethodInfo hasSelectedPrefix = typeof(SiegeInterventionSceneTauntSuppressionPatch).GetMethod(nameof(SafeOrderTroopPlacerHasSelectedFormationsPrefix), BindingFlags.Static | BindingFlags.NonPublic);
				MethodInfo hasSelectedTarget = AccessTools.Method(typeof(OrderTroopPlacer), "HasSelectedFormations");
				if (hasSelectedTarget != null && hasSelectedPrefix != null)
				{
					harmony.Patch(hasSelectedTarget, prefix: new HarmonyMethod(hasSelectedPrefix));
				}
				MethodInfo canUpdatePrefix = typeof(SiegeInterventionSceneTauntSuppressionPatch).GetMethod(nameof(SafeOrderTroopPlacerCanUpdatePrefix), BindingFlags.Static | BindingFlags.NonPublic);
				MethodInfo canUpdateTarget = AccessTools.Method(typeof(OrderTroopPlacer), "CanUpdate");
				if (canUpdateTarget != null && canUpdatePrefix != null)
				{
					harmony.Patch(canUpdateTarget, prefix: new HarmonyMethod(canUpdatePrefix));
				}
				MethodInfo afterStartPostfix = typeof(SiegeInterventionSceneTauntSuppressionPatch).GetMethod(nameof(BindOrderTroopPlacerAfterStartPostfix), BindingFlags.Static | BindingFlags.NonPublic);
				MethodInfo afterStartTarget = AccessTools.Method(typeof(OrderTroopPlacer), "AfterStart");
				if (afterStartTarget != null && afterStartPostfix != null)
				{
					harmony.Patch(afterStartTarget, postfix: new HarmonyMethod(afterStartPostfix));
				}
				PatchMissionOrderVmAndUiMethods(harmony);
				PatchInterventionHideoutVisualOrderProvider(harmony);
				PatchInterventionVisualOrderFactory(harmony);
				PatchInterventionOrderControllerMethods(harmony);
				MethodInfo sameTeamDamagePrefix = typeof(SiegeInterventionSceneTauntSuppressionPatch).GetMethod(nameof(AllowPlayerCivilianDamagePrefix), BindingFlags.Static | BindingFlags.NonPublic);
				MethodInfo sameTeamDamageTarget = AccessTools.Method(typeof(Mission), "CancelsDamageAndBlocksAttackBecauseOfNonEnemyCase");
				if (sameTeamDamageTarget != null && sameTeamDamagePrefix != null)
				{
					harmony.Patch(sameTeamDamageTarget, prefix: new HarmonyMethod(sameTeamDamagePrefix));
				}
				MethodInfo fleeTickPrefix = typeof(SiegeInterventionSceneTauntSuppressionPatch).GetMethod(nameof(SuppressCivilianNativeFleeTickPrefix), BindingFlags.Static | BindingFlags.NonPublic);
				MethodInfo fleeTickTarget = AccessTools.Method(typeof(FleeBehavior), nameof(FleeBehavior.Tick), new Type[] { typeof(float), typeof(bool) });
				if (fleeTickTarget != null && fleeTickPrefix != null)
				{
					harmony.Patch(fleeTickTarget, prefix: new HarmonyMethod(fleeTickPrefix));
				}
				MethodInfo fleeAvailabilityPrefix = typeof(SiegeInterventionSceneTauntSuppressionPatch).GetMethod(nameof(SuppressCivilianNativeFleeAvailabilityPrefix), BindingFlags.Static | BindingFlags.NonPublic);
				MethodInfo fleeAvailabilityTarget = AccessTools.Method(typeof(FleeBehavior), nameof(FleeBehavior.GetAvailability), new Type[] { typeof(bool) });
				if (fleeAvailabilityTarget != null && fleeAvailabilityPrefix != null)
				{
					harmony.Patch(fleeAvailabilityTarget, prefix: new HarmonyMethod(fleeAvailabilityPrefix));
				}
				_patched = true;
			}
			catch (Exception ex)
			{
				Logger.Log("SiegeAiIntervention", "Scene taunt suppression patch failed: " + ex.Message);
			}
		}

		private static void PatchMovementOrderConstructors(Harmony harmony)
		{
			try
			{
				MethodInfo transpiler = typeof(SiegeInterventionSceneTauntSuppressionPatch).GetMethod(nameof(SafeMovementOrderMissionTimeTranspiler), BindingFlags.Static | BindingFlags.NonPublic);
				if (harmony == null || transpiler == null)
				{
					return;
				}
				foreach (ConstructorInfo ctor in typeof(MovementOrder).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				{
					try
					{
						harmony.Patch(ctor, transpiler: new HarmonyMethod(transpiler));
					}
					catch (Exception ex)
					{
						Logger.Log("SiegeAiIntervention", "MovementOrder ctor safety patch failed on " + ctor + ": " + ex.Message);
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log("SiegeAiIntervention", "PatchMovementOrderConstructors failed: " + ex.Message);
			}
		}

		private static void PatchInterventionPlayerSpawn(Harmony harmony)
		{
			try
			{
				MethodInfo prefix = typeof(SiegeInterventionSceneTauntSuppressionPatch).GetMethod(nameof(ForceInterventionPlayerMountedSpawnPrefix), BindingFlags.Static | BindingFlags.NonPublic);
				if (prefix == null)
				{
					return;
				}
				MethodInfo taggedSpawn = AccessTools.Method(typeof(SandBoxHelpers.MissionHelper), nameof(SandBoxHelpers.MissionHelper.SpawnPlayer), new Type[] { typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(string) });
				if (taggedSpawn != null)
				{
					harmony.Patch(taggedSpawn, prefix: new HarmonyMethod(prefix));
				}
				MethodInfo entitySpawn = AccessTools.Method(typeof(SandBoxHelpers.MissionHelper), nameof(SandBoxHelpers.MissionHelper.SpawnPlayer), new Type[] { typeof(GameEntity), typeof(bool), typeof(bool), typeof(bool), typeof(bool) });
				if (entitySpawn != null)
				{
					harmony.Patch(entitySpawn, prefix: new HarmonyMethod(prefix));
				}
			}
			catch (Exception ex)
			{
				Logger.Log("SiegeAiIntervention", "PatchInterventionPlayerSpawn failed: " + ex.Message);
			}
		}

		private static void ForceInterventionPlayerMountedSpawnPrefix(ref bool civilianEquipment, ref bool noHorses)
		{
			try
			{
				Mission mission = Mission.Current;
				if (mission == null || mission.IsMissionEnding || !SiegeAiInterventionBehavior.ShouldForceDirectPlayerMountedSpawn())
				{
					return;
				}
				civilianEquipment = false;
				noHorses = false;
				Logger.Log("SiegeAiIntervention", "Forced GCCZ direct player battle/mounted spawn. Source=" + SiegeBannerBearerProfile.DirectPlayerMountedSpawnSource);
			}
			catch (Exception ex)
			{
				Logger.Log("SiegeAiIntervention", "ForceInterventionPlayerMountedSpawnPrefix failed: " + ex.Message);
			}
		}

		private static IEnumerable<CodeInstruction> SafeMovementOrderMissionTimeTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> list = (instructions ?? Enumerable.Empty<CodeInstruction>()).ToList();
			MethodInfo getCurrent = AccessTools.PropertyGetter(typeof(Mission), nameof(Mission.Current));
			MethodInfo getCurrentTime = AccessTools.PropertyGetter(typeof(Mission), nameof(Mission.CurrentTime));
			MethodInfo safeTime = typeof(SiegeInterventionSceneTauntSuppressionPatch).GetMethod(nameof(GetSafeMovementOrderMissionTime), BindingFlags.Static | BindingFlags.NonPublic);
			if (getCurrent == null || getCurrentTime == null || safeTime == null)
			{
				return list;
			}
			for (int i = 0; i < list.Count - 1; i++)
			{
				if (Equals(list[i].operand, getCurrent) && Equals(list[i + 1].operand, getCurrentTime))
				{
					CodeInstruction replacement = new CodeInstruction(OpCodes.Call, safeTime);
					replacement.labels.AddRange(list[i].labels);
					replacement.labels.AddRange(list[i + 1].labels);
					replacement.blocks.AddRange(list[i].blocks);
					replacement.blocks.AddRange(list[i + 1].blocks);
					list[i] = replacement;
					list.RemoveAt(i + 1);
				}
			}
			return list;
		}

		private static float GetSafeMovementOrderMissionTime()
		{
			try
			{
				return Mission.Current?.CurrentTime ?? 0f;
			}
			catch
			{
				return 0f;
			}
		}

		private static void PatchSceneTauntMissionMethod(Harmony harmony, MethodInfo prefix, string methodName)
		{
			try
			{
				MethodInfo target = AccessTools.Method(typeof(SceneTauntMissionBehavior), methodName);
				if (harmony == null || prefix == null || target == null)
				{
					Logger.Log("SiegeAiIntervention", "Scene taunt suppression target missing: " + methodName);
					return;
				}
				harmony.Patch(target, prefix: new HarmonyMethod(prefix));
			}
			catch (Exception ex)
			{
				Logger.Log("SiegeAiIntervention", "Scene taunt suppression method patch failed (" + methodName + "): " + ex.Message);
			}
		}

		private static void PatchSceneTauntStaticMethod(Harmony harmony, MethodInfo prefix, string methodName)
		{
			try
			{
				MethodInfo target = AccessTools.Method(typeof(SceneTauntBehavior), methodName);
				if (harmony == null || prefix == null || target == null)
				{
					Logger.Log("SiegeAiIntervention", "Scene taunt static suppression target missing: " + methodName);
					return;
				}
				harmony.Patch(target, prefix: new HarmonyMethod(prefix));
			}
			catch (Exception ex)
			{
				Logger.Log("SiegeAiIntervention", "Scene taunt static suppression method patch failed (" + methodName + "): " + ex.Message);
			}
		}

		private static void PatchOrderViewsMethod(Harmony harmony, MethodInfo postfix, string methodName)
		{
			try
			{
				MethodInfo target = AccessTools.Method(typeof(SandBoxMissionViews), methodName, new Type[] { typeof(Mission) });
				if (harmony == null || postfix == null || target == null)
				{
					Logger.Log("SiegeAiIntervention", "Order view injection target missing: " + methodName);
					return;
				}
				harmony.Patch(target, postfix: new HarmonyMethod(postfix));
			}
			catch (Exception ex)
			{
				Logger.Log("SiegeAiIntervention", "Order view injection patch failed (" + methodName + "): " + ex.Message);
			}
		}

		private static void PatchMissionOrderVmAndUiMethods(Harmony harmony)
		{
			try
			{
				if (harmony == null)
				{
					return;
				}
				Type orderUiHandlerType = AccessTools.TypeByName("TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer.MissionGauntletSingleplayerOrderUIHandler");
				MethodInfo initTarget = orderUiHandlerType == null ? null : AccessTools.Method(orderUiHandlerType, "OnMissionScreenInitialize");
				MethodInfo initPrefix = typeof(SiegeInterventionSceneTauntSuppressionPatch).GetMethod(nameof(PrepareOrderUiInitializePrefix), BindingFlags.Static | BindingFlags.NonPublic);
				if (initTarget != null && initPrefix != null)
				{
					harmony.Patch(initTarget, prefix: new HarmonyMethod(initPrefix));
					Logger.Log("SiegeAiIntervention", "Patched MissionGauntletSingleplayerOrderUIHandler.OnMissionScreenInitialize for intervention order UI.");
				}
				Type vmType = AccessTools.TypeByName("TaleWorlds.MountAndBlade.ViewModelCollection.Order.MissionOrderVM");
				MethodInfo teamGetter = vmType == null ? null : AccessTools.PropertyGetter(vmType, "Team");
				MethodInfo teamPrefix = typeof(SiegeInterventionSceneTauntSuppressionPatch).GetMethod(nameof(ProvideMissionOrderVmTeamPrefix), BindingFlags.Static | BindingFlags.NonPublic);
				if (teamGetter != null && teamPrefix != null)
				{
					harmony.Patch(teamGetter, prefix: new HarmonyMethod(teamPrefix));
				}
				MethodInfo controllerGetter = vmType == null ? null : AccessTools.PropertyGetter(vmType, "OrderController");
				MethodInfo controllerPrefix = typeof(SiegeInterventionSceneTauntSuppressionPatch).GetMethod(nameof(ProvideMissionOrderVmOrderControllerPrefix), BindingFlags.Static | BindingFlags.NonPublic);
				if (controllerGetter != null && controllerPrefix != null)
				{
					harmony.Patch(controllerGetter, prefix: new HarmonyMethod(controllerPrefix));
				}
				MethodInfo hasTroopsGetter = vmType == null ? null : AccessTools.PropertyGetter(vmType, "PlayerHasAnyTroopUnderThem");
				MethodInfo hasTroopsPrefix = typeof(SiegeInterventionSceneTauntSuppressionPatch).GetMethod(nameof(ProvideMissionOrderVmPlayerHasTroopsPrefix), BindingFlags.Static | BindingFlags.NonPublic);
				if (hasTroopsGetter != null && hasTroopsPrefix != null)
				{
					harmony.Patch(hasTroopsGetter, prefix: new HarmonyMethod(hasTroopsPrefix));
				}
				MethodInfo checkCanOpenTarget = vmType == null ? null : AccessTools.Method(vmType, "CheckCanBeOpened");
				MethodInfo checkCanOpenPrefix = typeof(SiegeInterventionSceneTauntSuppressionPatch).GetMethod(nameof(ProvideMissionOrderVmCheckCanBeOpenedPrefix), BindingFlags.Static | BindingFlags.NonPublic);
				if (checkCanOpenTarget != null && checkCanOpenPrefix != null)
				{
					harmony.Patch(checkCanOpenTarget, prefix: new HarmonyMethod(checkCanOpenPrefix));
				}
			}
			catch (Exception ex)
			{
				Logger.Log("SiegeAiIntervention", "Mission order VM/UI patch failed: " + ex.Message);
			}
		}

		private static void PatchInterventionHideoutVisualOrderProvider(Harmony harmony)
		{
			try
			{
				if (harmony == null)
				{
					return;
				}
				Type providerType = AccessTools.TypeByName("SandBox.View.OrderProviders.HideoutVisualOrderProvider");
				MethodInfo target = providerType == null ? null : AccessTools.Method(providerType, "IsAvailable", Type.EmptyTypes);
				MethodInfo prefix = typeof(SiegeInterventionSceneTauntSuppressionPatch).GetMethod(nameof(AllowHideoutVisualOrderProviderDuringInterventionPrefix), BindingFlags.Static | BindingFlags.NonPublic);
				if (target == null || prefix == null)
				{
					Logger.Log("SiegeAiIntervention", "Hideout visual order provider patch target missing.");
					return;
				}
				harmony.Patch(target, prefix: new HarmonyMethod(prefix));
				Logger.Log("SiegeAiIntervention", "Patched HideoutVisualOrderProvider.IsAvailable for intervention native order UI.");
			}
			catch (Exception ex)
			{
				Logger.Log("SiegeAiIntervention", "PatchInterventionHideoutVisualOrderProvider failed: " + ex.Message);
			}
		}

		private static bool AllowHideoutVisualOrderProviderDuringInterventionPrefix(ref bool __result)
		{
			try
			{
				if (!SiegeAiInterventionBehavior.IsOccupationSceneActiveForExternal())
				{
					return true;
				}
				Mission mission = Mission.Current;
				if (mission == null || mission.IsMissionEnding || mission.Mode == MissionMode.Conversation || mission.Mode == MissionMode.Barter)
				{
					return true;
				}
				__result = true;
				return false;
			}
			catch
			{
				return true;
			}
		}

		private static void PatchInterventionVisualOrderFactory(Harmony harmony)
		{
			try
			{
				MethodInfo target = AccessTools.Method(typeof(VisualOrderFactory), nameof(VisualOrderFactory.GetOrders), Type.EmptyTypes);
				MethodInfo postfix = typeof(SiegeInterventionSceneTauntSuppressionPatch).GetMethod(nameof(FilterInterventionVisualOrdersPostfix), BindingFlags.Static | BindingFlags.NonPublic);
				if (harmony != null && target != null && postfix != null)
				{
					harmony.Patch(target, postfix: new HarmonyMethod(postfix));
					Logger.Log("SiegeAiIntervention", "Patched VisualOrderFactory.GetOrders to filter offensive intervention orders.");
				}
			}
			catch (Exception ex)
			{
				Logger.Log("SiegeAiIntervention", "PatchInterventionVisualOrderFactory failed: " + ex.Message);
			}
		}

		private static void PatchInterventionOrderControllerMethods(Harmony harmony)
		{
			try
			{
				MethodInfo prefix = typeof(SiegeInterventionSceneTauntSuppressionPatch).GetMethod(nameof(BlockOffensiveNativeOrderPrefix), BindingFlags.Static | BindingFlags.NonPublic);
				if (harmony == null || prefix == null)
				{
					return;
				}
				foreach (MethodInfo method in typeof(OrderController).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				{
					ParameterInfo[] parameters = method.GetParameters();
					if (!method.Name.StartsWith("SetOrder", StringComparison.Ordinal) || parameters.Length == 0 || parameters[0].ParameterType != typeof(OrderType))
					{
						continue;
					}
					try
					{
						harmony.Patch(method, prefix: new HarmonyMethod(prefix));
						Logger.Log("SiegeAiIntervention", "Patched offensive order guard on OrderController." + method.Name + ".");
					}
					catch (Exception ex)
					{
						Logger.Log("SiegeAiIntervention", "OrderController offensive guard patch failed on " + method.Name + ": " + ex.Message);
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log("SiegeAiIntervention", "PatchInterventionOrderControllerMethods failed: " + ex.Message);
			}
		}

		private static void FilterInterventionVisualOrdersPostfix(ref MBReadOnlyList<VisualOrderSet> __result)
		{
			try
			{
				if (!SiegeAiInterventionBehavior.IsOccupationSceneActiveForExternal())
				{
					return;
				}
				__result = SiegeAiInterventionBehavior.FilterInterventionNativeVisualOrdersForExternal(__result);
			}
			catch (Exception ex)
			{
				Logger.Log("SiegeAiIntervention", "FilterInterventionVisualOrdersPostfix failed: " + ex.Message);
			}
		}

		private static bool BlockOffensiveNativeOrderPrefix(object[] __args)
		{
			try
			{
				if (!SiegeAiInterventionBehavior.IsOccupationSceneActiveForExternal())
				{
					return true;
				}
				if (__args == null || __args.Length == 0 || !(__args[0] is OrderType orderType))
				{
					return true;
				}
				if (!SiegeAiInterventionBehavior.IsOffensiveInterventionOrder(orderType))
				{
					return true;
				}
				Logger.Log("SiegeAiIntervention", "Blocked offensive native order during intervention: " + orderType);
				return false;
			}
			catch
			{
				return true;
			}
		}

		private static bool SuppressSceneTauntMissionBehaviorPrefix()
		{
			return !SiegeAiInterventionBehavior.IsOccupationSceneActiveForExternal();
		}

		private static bool SuppressSceneTauntCrimePrefix()
		{
			return !SiegeAiInterventionBehavior.IsOccupationSceneActiveForExternal();
		}

		private static bool SuppressCrimeRatingDuringInterventionPrefix()
		{
			return !SiegeAiInterventionBehavior.IsOccupationSceneActiveForExternal();
		}

		private static bool SuppressCivilianUsableTargetPrefix(AgentNavigator __instance, UsableMachine usableMachine)
		{
			try
			{
				if (usableMachine == null || !SiegeAiInterventionBehavior.IsOccupationSceneActiveForExternal())
				{
					return true;
				}
				Agent agent = __instance?.OwnerAgent;
				if (!SiegeAiInterventionBehavior.IsEligibleCivilianAgent(agent, includeHeroes: true, requireActive: false))
				{
					return true;
				}
				SiegeAiInterventionBehavior.NeutralizeCivilianDailyUsableBehavior(agent, SiegeNativeBridgeSourceProfile.UsableTargetPrefixSource);
				return false;
			}
			catch
			{
				return true;
			}
		}

		private static bool SuppressCivilianUsableMissionObjectPrefix(Agent userAgent)
		{
			try
			{
				if (SiegeAiInterventionBehavior.IsOccupationSceneActiveForExternal() && SiegeAiInterventionBehavior.IsEligibleCivilianAgent(userAgent, includeHeroes: true, requireActive: false))
				{
					return false;
				}
			}
			catch
			{
			}
			return true;
		}

		private static bool SuppressCivilianNativeFleeTickPrefix(FleeBehavior __instance)
		{
			try
			{
				Agent agent = __instance?.OwnerAgent;
				if (SiegeAiInterventionBehavior.IsOccupationSceneActiveForExternal() && SiegeAiInterventionBehavior.IsEligibleCivilianAgent(agent, includeHeroes: true, requireActive: false))
				{
					if (SiegeAiInterventionBehavior.IsLocalNativeFleeAllowedForExternal(agent))
					{
						return true;
					}
					if (__instance != null)
					{
						__instance.IsActive = false;
					}
					if (agent != null && agent.IsActive())
					{
						SiegeAiInterventionBehavior.NeutralizeCivilianDailyUsableBehavior(agent, SiegeNativeBridgeSourceProfile.NativeFleeTickPrefixSource);
					}
					return false;
				}
			}
			catch
			{
			}
			return true;
		}

		private static bool SuppressCivilianNativeFleeAvailabilityPrefix(FleeBehavior __instance, ref float __result)
		{
			try
			{
				Agent agent = __instance?.OwnerAgent;
				if (SiegeAiInterventionBehavior.IsOccupationSceneActiveForExternal() && SiegeAiInterventionBehavior.IsEligibleCivilianAgent(agent, includeHeroes: true, requireActive: false))
				{
					if (SiegeAiInterventionBehavior.IsLocalNativeFleeAllowedForExternal(agent))
					{
						return true;
					}
					__result = 0f;
					return false;
				}
			}
			catch
			{
			}
			return true;
		}

		private static void PrepareOrderUiInitializePrefix()
		{
			try
			{
				if (!SiegeAiInterventionBehavior.IsOccupationSceneActiveForExternal())
				{
					return;
				}
				SiegeAiInterventionBehavior.EnsureInterventionCommandUiReadyForExternal(Mission.Current, SiegeNativeBridgeSourceProfile.OrderUiInitializeSource);
			}
			catch
			{
			}
		}

		private static bool ProvideMissionOrderVmTeamPrefix(ref Team __result)
		{
			try
			{
				if (!SiegeAiInterventionBehavior.IsOccupationSceneActiveForExternal())
				{
					return true;
				}
				__result = SiegeAiInterventionBehavior.ResolveInterventionPlayerCommandTeamForExternal(Mission.Current, SiegeNativeBridgeSourceProfile.MissionOrderVmTeamSource);
				return __result == null;
			}
			catch
			{
				return true;
			}
		}

		private static bool ProvideMissionOrderVmOrderControllerPrefix(ref OrderController __result)
		{
			try
			{
				if (!SiegeAiInterventionBehavior.IsOccupationSceneActiveForExternal())
				{
					return true;
				}
				SiegeAiInterventionBehavior.EnsureInterventionCommandUiReadyForExternal(Mission.Current, SiegeNativeBridgeSourceProfile.MissionOrderVmControllerSource);
				__result = SiegeAiInterventionBehavior.TryResolveNativeOrderControllerForExternal(Mission.Current);
				return __result == null;
			}
			catch
			{
				__result = null;
				return false;
			}
		}

		private static bool ProvideMissionOrderVmPlayerHasTroopsPrefix(ref bool __result)
		{
			try
			{
				if (!SiegeAiInterventionBehavior.IsOccupationSceneActiveForExternal())
				{
					return true;
				}
				SiegeAiInterventionBehavior.EnsureInterventionCommandUiReadyForExternal(Mission.Current, SiegeNativeBridgeSourceProfile.MissionOrderVmHasTroopsSource);
				__result = SiegeAiInterventionBehavior.InterventionPlayerHasCommandableAgentsForExternal(Mission.Current);
				return false;
			}
			catch
			{
				__result = false;
				return false;
			}
		}

		private static bool ProvideMissionOrderVmCheckCanBeOpenedPrefix(ref bool __result)
		{
			try
			{
				if (!SiegeAiInterventionBehavior.IsOccupationSceneActiveForExternal())
				{
					return true;
				}
				Mission mission = Mission.Current;
				if (mission == null || mission.IsMissionEnding || mission.Mode == MissionMode.Conversation || mission.Mode == MissionMode.Barter)
				{
					return true;
				}
				__result = SiegeAiInterventionBehavior.EnsureInterventionCommandUiReadyForExternal(mission, SiegeNativeBridgeSourceProfile.MissionOrderVmCheckOpenSource) && SiegeAiInterventionBehavior.InterventionPlayerHasCommandableAgentsForExternal(mission);
				return false;
			}
			catch
			{
				__result = false;
				return false;
			}
		}

		private static bool ProvideOrderTroopPlacerOrderControllerPrefix(OrderTroopPlacer __instance, ref OrderController __result)
		{
			try
			{
				if (!SiegeAiInterventionBehavior.IsOccupationSceneActiveForExternal())
				{
					return true;
				}
				OrderController orderController = SiegeAiInterventionBehavior.TryResolveNativeOrderControllerForExternal(__instance?.Mission ?? Mission.Current);
				if (orderController == null)
				{
					__result = null;
					return false;
				}
				SiegeAiInterventionBehavior.TryBindNativeOrderControllerForExternal(__instance, SiegeNativeBridgeSourceProfile.OrderControllerGetterSource);
				__result = orderController;
				return false;
			}
			catch
			{
				__result = null;
				return false;
			}
		}

		private static bool SafeOrderTroopPlacerHasSelectedFormationsPrefix(OrderTroopPlacer __instance, ref bool __result)
		{
			try
			{
				if (!SiegeAiInterventionBehavior.IsOccupationSceneActiveForExternal())
				{
					return true;
				}
				__result = SiegeAiInterventionBehavior.NativeOrderControllerHasSelectedFormationsForExternal(__instance?.Mission ?? Mission.Current);
				return false;
			}
			catch
			{
				__result = false;
				return false;
			}
		}

		private static bool SafeOrderTroopPlacerCanUpdatePrefix(OrderTroopPlacer __instance, ref bool __result)
		{
			try
			{
				if (!SiegeAiInterventionBehavior.IsOccupationSceneActiveForExternal())
				{
					return true;
				}
				__result = SiegeAiInterventionBehavior.NativeOrderControllerHasSelectedFormationsForExternal(__instance?.Mission ?? Mission.Current);
				return false;
			}
			catch
			{
				__result = false;
				return false;
			}
		}

		private static void BindOrderTroopPlacerAfterStartPostfix(OrderTroopPlacer __instance)
		{
			SiegeAiInterventionBehavior.TryBindNativeOrderControllerForExternal(__instance, SiegeNativeBridgeSourceProfile.OrderPlacerAfterStartSource);
		}


		private static void InjectInterventionOrderViewsPostfix(Mission mission, ref MissionView[] __result, MethodBase __originalMethod)
		{
			try
			{
				if (!SiegeAiInterventionBehavior.ShouldInjectInterventionOrderViewsForExternal(mission))
				{
					return;
				}
				int oldCount = __result?.Length ?? 0;
				SiegeAiInterventionBehavior.EnsureInterventionCommandUiReadyForExternal(mission, SiegeNativeBridgeSourceProfile.InjectNativeOrderViewsSource);
				__result = BuildInterventionNativeOrderViews(mission, __result);
				Logger.Log("SiegeAiIntervention", "Extended intervention mission views with native order UI while preserving settlement leave view. ViewMethod=" + (__originalMethod?.Name ?? "N/A") + ", OldViews=" + oldCount + ", NewViews=" + (__result?.Length ?? 0));
			}
			catch (Exception ex)
			{
				Logger.Log("SiegeAiIntervention", "InjectInterventionOrderViewsPostfix failed: " + ex.Message);
			}
		}

		private static MissionView[] BuildInterventionNativeOrderViews(Mission mission, MissionView[] originalViews)
		{
			List<MissionView> views = (originalViews ?? Array.Empty<MissionView>())
				.Where(view => view != null && !IsBattleScoreMissionView(view))
				.ToList();
			void AddIfMissing(MissionView view)
			{
				if (view == null)
				{
					return;
				}
				Type type = view.GetType();
				if (views.Any(existing => existing != null && existing.GetType() == type))
				{
					return;
				}
				views.Add(view);
			}
			AddIfMissing(ViewCreator.CreateMissionLeaveView());
			AddIfMissing(ViewCreator.CreateMissionOrderUIHandler(null));
			AddIfMissing(new OrderTroopPlacer(null));
			AddIfMissing(ViewCreator.CreateMissionFormationMarkerUIHandler(mission));
			AddIfMissing(new MissionFormationTargetSelectionHandler());
			return views.ToArray();
		}

		private static bool IsBattleScoreMissionView(MissionView view)
		{
			string name = view?.GetType()?.Name ?? "";
			return name.IndexOf("BattleScore", StringComparison.OrdinalIgnoreCase) >= 0
				|| name.IndexOf("Scoreboard", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static bool AllowPlayerCivilianDamagePrefix(Mission __instance, Agent attacker, Agent victim, ref bool __result)
		{
			try
			{
				if (!SiegeAiInterventionBehavior.IsOccupationSceneActiveForExternal() || attacker == null || victim == null || !attacker.IsMainAgent || !victim.IsHuman)
				{
					return true;
				}
				if (SiegeAiInterventionBehavior.IsInterventionAlliedSoldierForExternal(victim, requireActive: false))
				{
					SiegeAiInterventionBehavior.TryHandleFriendlyHitOnAlliedSoldier(victim, SiegeLocalAttackProfile.NonEnemyDamagePrefixSource, 0f);
					__result = true;
					return false;
				}
				if (SiegeAiInterventionBehavior.IsEligibleCivilianAgent(victim, includeHeroes: true, requireActive: false))
				{
					__result = false;
					return false;
				}
			}
			catch
			{
			}
			return true;
		}
	}
}
