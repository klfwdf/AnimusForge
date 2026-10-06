using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RichExecutions.Core;
using RichExecutions.Customization;
using RichExecutions.Diagnostics;
using SandBox.Conversation;
using SandBox.Conversation.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using BannerlordCampaign = TaleWorlds.CampaignSystem.Campaign;

namespace RichExecutions.Scene;

/// <summary>
/// Owns one public-execution mission session. The scene uses only the selected
/// hero's own character and visual equipment as the prisoner. The mission Agent
/// uses a BasicBattleAgentOrigin so a real battlefield death can drive the
/// native corpse pipeline without killing the campaign hero through
/// SimpleAgentOrigin.SetKilled. Campaign execution, costs and consequences stay
/// deferred until the player leaves the mission. No substitute character is
/// permitted.
/// </summary>
public sealed partial class TownExecutionMissionBehavior : MissionLogic, IExecutionSceneHost
{
    private const float InitializationTimeoutSeconds = 8f;
    private const float PlacementRetryIntervalSeconds = 1f;
    private const float CeremonyPreparationSeconds = 0.65f;
    private const float CrowdReactionSeconds = 4.75f;
    private const float PrimaryActionObservationSeconds = 0.35f;
    private const float GenericActionObservationSeconds = 0.65f;
    // Beheading/hanging personal executions use their verified authored roots.
    // Burning and crossbow instead capture MainAgent's trigger position and keep
    // only the facing lock, so the player is never teleported to the NPC mark.
    private const float InitialExecutionerStandbySettleTimeoutSeconds = 2.00f;
    private const float MinimumVisibleActionProgress = 0.015f;
    private const string GenericExecutionAction = "act_kick_right_leg";
    // Keep authored clips on channel 0 so the whole body, weapon and lethal
    // progress stay synchronized. The runtime also pins the Agent root every
    // tick because imported clips can retain a small residual translation and
    // heading even after Blender root cleanup.
    private const string TwoHandedBeheadingExecutionAction = "act_ver_cutscene_executioner_action";
    private const string OneHandedBeheadingExecutionAction = "act_ver_cutscene_executioner_action";
    private const string ExecutionerEntryDefaultIdleAction = "act_ver_cutscene_executioner_idle";
    private const string OneHandedCeremonyIdleAction = "act_ver_cutscene_executioner_idle";
    private const string TwoHandedBeheadingFallbackAction = "act_quick_release_overswing_2h";
    private const string OneHandedBeheadingFallbackAction = "act_quick_release_overswing_1h";
    private const string TwoHandedCeremonyFallbackIdleAction = "act_idle_2h_1";
    private const string OneHandedCeremonyFallbackIdleAction = "act_idle_1h_without_shield_1";
    // Rebinding the ceremony idle costs one channel-0 request per attempt. Give
    // it the same kind of retry window the guards already get instead of
    // cancelling a valid execution on the first read-back miss.
    private const float CeremonyIdleRebindTimeoutSeconds = 2.50f;
    private const float CeremonyIdleRebindIntervalSeconds = 0.20f;
    private const string GuardSpearIdleAction = "act_guard_idle_spear";
    private const string UnarmedExecutionerFallbackIdleAction = "act_idle_unarmed_1";
    private const string HangingVictimStruggleAction = "act_conversation_aggressive_loop";
    private const string BurningVictimIgnitionReactionAction = "act_conversation_aggressive_very_negative";
    private const string BurningVictimSustainedFearAction = "act_conversation_aggressive_loop";
    private const string BurningVictimTorsoReactionAction = "act_conversation_aggressive_very_negative";
    private const string FrozenVictimClosedEyesFacialAction = "idle_sleep";
    private const string HangingReleaseSoundEvent = "event:/mission/siege/siegetower/dooropen";
    private const string VictimFireParticleSystem = "psys_torch_fire_moving";
    private static readonly Vec3[] VictimFireLocalOffsets =
    {
        new(-0.07f, 0.01f, 0.02f),
        new(0.07f, -0.01f, 0.10f)
    };
    private const float HangingStruggleSeconds = 1.25f;
    private const float HangingDeathPoseLeadInSeconds = 0.35f;
    private const float BurningBaseAndLegsSeconds = 0.90f;
    private const float BurningWaistSeconds = 0.90f;
    private const float BurningTorsoSeconds = 1.20f;
    private const float BurningDeathPoseLeadInSeconds = 0.40f;
    private const string ExecutionAxeItemId = "execution_axe";
    private const string ExecutionTorchItemId = "rex_execution_torch";
    private const string BreakingWheelHammerItemId = "peasant_hammer_2_t1";
    private const string StoningStoneItemId = "throwing_stone";
    private const string CrossbowExecutionItemId = "crossbow_a";
    private const string CrossbowBoltItemId = "bolt_a";
    private const float PairedAgentCollisionMargin = 0.08f;
    private const float PlayerExecutionActorDistance = 0.85f;
    private const float NpcExecutionActorDistance = 1.05f;
    private const float BurningExecutionActorDistance = 3.00f;
    private const float BurningSupportCrossSectionScale = 0.68f;
    private const float CrossbowExecutionActorDistance = 10.00f;
    private const float CrossbowVictimRearwardOffset = -10.00f;
    private const float CrossbowExecutionerFiringRightOffset = -1.50f;
    private const float CrossbowBoltSpeed = 70.00f;
    private const float CrossbowHandAnchorMinimumInset = 0.08f;
    private const float CrossbowHandAnchorMaximumHalfSpan = 0.82f;
    private const float CrossbowHandAnchorHeight = 1.48f;
    // Keep scaffold ceremonies to the local left while preserving every
    // authored prisoner/actor separation. Burning remains centered on its
    // ground pyre.
    // The signed value is expressed along the placement Right axis.
    private const float ScaffoldCeremonyLateralShift = -2.25f;
    private const float MinimumPlayerExecutionDistance = 0.55f;
    private const float MaximumPlayerExecutionDistance = 3.50f;
    private const float BurningMinimumPlayerIgnitionDistance = 1.35f;
    private const float BurningMaximumPlayerIgnitionDistance = 3.80f;
    private const float StoningMaximumPlayerThrowDistance = 5.25f;
    private const float CrossbowMinimumPlayerFiringDistance = 7.50f;
    private const float CrossbowMaximumPlayerFiringDistance = 12.50f;
    private const float ExecutionerPreferredStandbyLateralOffset = -2.45f;
    private const float ExecutionerPreferredStandbyForwardOffset = 0.45f;
    // Screenshot-authored stone-throwing mark. Unlike the generic ceremony
    // standby, this is the executioner's actual spawn point: the NPC must be
    // visible in the blue-box position from the first mission frame and must
    // never be moved there only after the player starts the sentence.
    private const float StoningExecutionerStandbyRightOffset = 0.00f;
    private const float StoningExecutionerStandbyForwardOffset = 3.25f;
    private const float StoningPlayerEntryForwardOffset = 4.65f;
    private const float MinimumExecutionerStandbyLeftDistance = 0.75f;
    private const float ExecutionerStandbyClearanceMargin = 0.55f;
    private const float ExecutionerStandbyRingSpacing = 0.7f;
    private const int ExecutionerStandbyRingCount = 3;
    private const int ExecutionerStandbySlotCount = 12;
    private const float ExecutionerRetreatSideStepDistance = 0.65f;
    private const float ExecutionerRetreatMinimumSideStep = 0.20f;
    private const float ExecutionerRetreatMaximumCrowdAdvance = 0.05f;
    private const float GuardStandbyMaintenanceIntervalSeconds = 0.65f;
    private const float DefaultHumanCollisionRadius = 0.35f;
    private const float UsePointCapsuleBottom = 0.12f;
    private const float UsePointCapsuleTop = 1.60f;
    private const float UsePointCapsuleRadius = 0.45f;
    private const float BurningUsePointCapsuleBottom = 0.05f;
    private const float BurningUsePointCapsuleTop = 1.10f;
    private const float BurningUsePointCapsuleRadius = 1.65f;
    private const string UsePointMarkerPrefab = "bd_rope_a";
    private const float MaximumSpawnHeightDelta = 0.85f;
    private const int CrowdOverheadSurfaceRejectionLogLimit = 8;
    private const float AgentBodyClearanceRadius = 0.44f;
    private const float AgentChestHeight = 1.05f;
    private const float SpeedLimitRestoreTolerance = 0.01f;
    private const float ExecutionActionOneShotCompletionProgress = 0.97f;
    private const float ExecutionActionLoopWrapTolerance = 0.20f;
    private const float ExecutionActionRootCorrectionThreshold = 0.01f;
    // Blender inspection and the v0.3.26 runtime read-back agree that the
    // authored execution clip displaces the Agent root about 0.257-0.260m
    // farther toward the prisoner's left as soon as it advances. Bind that one
    // clip from an equal and opposite victim-view offset; the visible root then
    // lands on the already verified -0.850m ceremony mark. Idles and fallbacks
    // deliberately keep the unmodified mark.
    private const float AuthoredExecutionRootVictimRightCompensation = 0.260f;
    // This is the complete native scaffold, not a small plank substituted as
    // a stand-in. Its children contain the deck and the built-in stairs.
    private const string GallowsPrefab = "gallows";
    private const string GallowsDeckPartName = "wooden_platform_c";
    private const string GallowsDeckCoverPartName = "european_castle_wooden_cover_b";
    private const string GallowsStairsPartName = "wooden_platform_stairs_a";
    private const string GallowsStructuralBeamPartName = "wooden_platform_plank_f_column";
    private const string GallowsSmoothRampPrefab = "castle_plank_ramp_a";
    // Realistic hanging visuals: the gallows prefab already contains two
    // decorative rope meshes at deck height. They are suppressed so the
    // ceremony can attach one clean drop rope from the beam to the prisoner's
    // neck and keep a single noose collar at neck height.
    private const string GallowsNativeRopeA = "bd_rope_a";
    private const string GallowsNativeRopeB = "bd_rope_b";
    private const string HangingDropRopeClothPrefab = "bd_cloth_hanging_rope";
    private const string HangingDropRopeReinMesh = "horse_harness_a_new_rein";
    private const float HangingDropRopeLengthSlack = 0.92f;
    private const float GallowsBeamLocalHeight = 5.36f;
    private const float HangingNeckDropFromEye = 0.18f;
    private const float HangingDropRopeMinimumLength = 0.60f;
    private const float HangingDropRopeMaximumLength = 4.50f;
    private const float HangingLiftHeight = 0.55f;
    private const float HangingLiftRampSeconds = 1.10f;
    private const float HangingSuspensionProbeIntervalSeconds = 0.10f;
    private const float GallowsStairCorridorPadding = 0.12f;
    private const float GallowsSmoothRampSurfaceInset = 0.035f;
    private const float GallowsSmoothRampGroundLeadIn = 0.30f;
    private const float GallowsSmoothRampDeckOverlap = 0.20f;
    private const float GallowsSmoothRampWidthMargin = 0.10f;
    private const float GallowsSmoothRampEndSampleInset = 0.12f;
    private const float GallowsSmoothRampAgentHeightTolerance = 0.12f;
    // Retained for the legacy fixed-ray diagnostic below; the active acceptance
    // gate uses Agent-filtered ground height instead.
    private const float GallowsSmoothRampRayHeight = 0.65f;
    private const float GallowsSmoothRampRayDepth = 0.65f;
    private const float GallowsSmoothRampContinuityTolerance = 0.18f;
    private const float GallowsSmoothRampSurfaceMatchTolerance = 0.22f;
    private const float GallowsSmoothRampMinimumRise = 0.45f;
    private const float GallowsSmoothRampMinimumRun = 0.60f;
    private const int GallowsStairSurfaceProbeCount = 7;
    private const float GallowsStairSurfaceProbeSpan = 0.86f;
    private const float GallowsStairSurfaceProbeHeight = 1.60f;
    private const float GallowsStairSurfaceProbeDepth = 1.20f;
    private const float GallowsStairMinimumWidth = 0.40f;
    private const float GallowsSpawnDepth = 8f;
    private const float GallowsDeckSurfaceTolerance = 0.12f;
    private const float GallowsDeckSupportProbeHeight = 0.38f;
    private const float GallowsDeckSupportProbeDepth = 0.52f;
    private const float GallowsDeckSupportHeightTolerance = 0.30f;
    private const float GallowsMinimumDeckWidth = 8f;
    private const float GallowsMinimumDeckDepth = 3f;
    private const float GallowsMaximumDeckDimension = 15f;
    private const float GallowsMinimumInteractionForward = 4.75f;
    private const float GallowsEntryStandOff = 2.10f;
    private const float CrowdFrontRowDistance = 10.75f;
    private const float CrowdRingSpacing = 1.25f;
    private const int MaximumExecutionCrowdCount = 14;
    private const int CrowdHumanLoadDivisor = 12;
    private const int CrowdCandidateBudget = 84;
    private const float MinimumCrowdPlayerClearance = 2.50f;
    private const float MinimumCrowdSpacing = 1.20f;
    private const float CrowdEscapeCorridorHalfWidth = 1.35f;
    private const float CrowdEscapeCorridorStartPadding = 0.35f;
    private const float StoningCrowdRingRadius = 4.80f;    private const float StoningCrowdEntranceHalfAngle = 0.38f;
    private const float StoningFirstThrowDelaySeconds = 0.30f;
    private const float StoningMinimumThrowIntervalSeconds = 0.32f;
    private const float StoningThrowIntervalRangeSeconds = 0.46f;
    private const float StoningAimSeconds = 0.38f;
    private const float StoningProjectileReleaseDelaySeconds = 0.16f;
    private const float StoningProjectileSpeed = 13.50f;
    private const float StoningProjectileSpawnHeight = 1.22f;
    private const float StoningProjectileTargetHeight = 0.82f;
    private const float StoningProjectileLateralSpread = 0.34f;
    private const float CrossbowVolleyAimStartStaggerSeconds = 0.10f;
    private const float CrossbowVolleyReadyToHoldSeconds = 0.65f;

    private static readonly float[][] CrossbowVolleyRightOffsetCandidates =
    {
        new[] { -4.50f, -4.85f, -4.15f },
        new[] { -3.00f, -3.35f, -2.70f },
        new[] { 1.50f, 1.85f, 1.20f },
        new[] { 3.00f, 3.35f, 2.70f }
    };

    // The authored venue axis is mirrored by the firing-line presentation in
    // the current scene. Keep the spawned marks unchanged, but drive their
    // phases in the visual left-to-right order the player sees.
    private static readonly int[] CrossbowVolleyLeftToRightOrder = { 3, 2, 1, 0 };

    private static readonly string[] StoningCrowdIdleActionNames =
    {
        "act_idle_unarmed_1",
        "act_idle_unarmed_2",
        "act_idle_unarmed_3",
        "act_idle_unarmed_4",
        "act_idle_unarmed_5",
        "act_idle_unarmed_1_left_stance",
        "act_idle_unarmed_2_left_stance",
        "act_idle_unarmed_3_left_stance",
        "act_idle_unarmed_4_left_stance",
        "act_idle_unarmed_5_left_stance"
    };

    // Spectators used to stand in the encyclopedia "inventory display" stance,
    // which is a stiff authored tableau pose and reads as a shop mannequin
    // rather than a crowd. These are the vanilla town-resident ambient
    // animations instead: the ordinary stand idles, the crowd reactions the
    // town scenes already use (listen / talk / applaud / cheer) and a few
    // characterful resident poses. Picked at random per agent, so the ring of
    // spectators no longer moves in lockstep.
    //
    // Every name below was verified to exist in
    // Modules/Native/ModuleData/action_types.xml for the installed build.
    private static readonly string[] CrowdAmbientIdleActionNames =
    {
        // Ordinary town-resident standing idles.
        "act_stand_1",
        "act_stand_2",
        "act_stand_3",
        "act_stand_4",
        "act_stand_6",
        "act_stand_8",
        "act_stand_9",
        "act_stand_10",
        // Watching / listening to the address.
        "act_listen_1",
        "act_listen_2",
        "act_listen_3",
        "act_listen_5",
        "act_listen_7",
        // Neighbours muttering to each other.
        "act_conversation_talk_1",
        "act_conversation_talk_commenting",
        "act_conversation_talk_explain",
        "act_conversation_talk_negative",
        // Crowd reactions.
        "act_applaud_1",
        "act_applaud_3",
        "act_applaud_5",
        "act_cheer_1",
        "act_cheer_2",
        "act_cheer_3",
        "act_cheering_low_01",
        "act_cheering_low_04",
        "act_taunt_cheer_1",
        "act_taunt_cheer_2",
        // Characterful resident poses so the crowd is not uniform.
        "act_beggar_idle",
        "act_drunk_idle",
        "act_musician_idle_stand_cheerful"
    };


    private static readonly string[] GuardOneHandedIdleActionNames =
    {
        "act_idle_1h_without_shield_1",
        "act_run_idle_1h"
    };

    private static readonly string[] GuardTwoHandedIdleActionNames =
    {
        "act_idle_2h_1",
        "act_run_idle_2h"
    };

    private static readonly string[] GuardPolearmIdleActionNames =
    {
        "act_idle_spear_1",
        "act_run_idle_polearm"
    };

    private enum MethodExecutionPhase
    {
        None = 0,
        HangingStruggle = 1,
        HangingDeathPose = 2,
        BurningBaseAndLegs = 3,
        BurningWaist = 4,
        BurningTorso = 5,
        BurningDeathPose = 6,
        Completed = 7
    }

    private readonly ExecutionService _service;
    private readonly ExecutionSceneActCatalog _sceneActs;
    private readonly ExecutionVictimDeathMissionLogic _deathLogic;
    private IExecutionMethodStrategy _strategy;
    private ExecutionMethodDefinition? _runtimeMethod;
    private readonly ExecutionStateMachine _stateMachine = new();
    private readonly List<GameEntity> _spawnedEntities = new();
    private readonly List<GameEntity> _pendingEntityRemovals = new();
    private readonly List<GameEntity> _gallowsRampEntities = new();
    private readonly List<Agent> _spawnedAgents = new();
    private readonly ExecutionSceneLocationCharacters _sceneLocationCharacters = new();
    private readonly List<Agent> _frontGuardAgents = new();
    private readonly List<Agent> _crowdAgents = new();
    private int _crowdOverheadSurfaceRejections;
    private readonly List<Agent> _crossbowVolleyAgents = new();
    private readonly List<Vec3> _crossbowVolleyPositions = new();
    private readonly List<bool> _crossbowVolleyReadyStarted = new();
    private readonly List<bool> _crossbowVolleyHoldStarted = new();
    private readonly List<bool> _crossbowVolleyReleased = new();
    private readonly List<Agent> _methodHelperAgents = new();
    private readonly Dictionary<string, GameEntity> _profileEntities =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<GameEntity, float> _entityBaseLengths = new();
    private readonly List<GameEntity> _burningSupportEntities = new();
    private readonly List<MatrixFrame> _burningSupportBaseFrames = new();
    private readonly List<GuardStandbyBinding> _guardStandbyBindings = new();
    private readonly List<Vec3> _occupiedSpawnPositions = new();
    private readonly HashSet<string> _ignitionRolesAttempted =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _ignitionRolesSucceeded =
        new(StringComparer.OrdinalIgnoreCase);

    private IExecutionSceneAct? _sceneAct;
    private ExecutionSceneVisualProfile? _visualProfile;
    private ExecutionScenePlacement? _placement;
    private ExecutionSiteBuilderController? _customSiteBuilder;
    private ExecutionSitePreset? _customSitePreset;
    private Dictionary<string, ExecutionSiteMarker>? _customSiteMarkers;
    private bool _customSiteLayoutConfirmed;
    private MissionConversationLogic? _conversationLogic;
    private Agent? _playerAgent;
    private Agent? _victimAgent;
    private Agent? _executionerAgent;
    private Team? _executionTeam;
    private Team? _victimTeam;
    private DetachedHeadVisual? _detachedHeadVisual;
    private ExecutionUsePoint? _usePoint;
    private GameEntity? _usePointHost;
    private GameEntity? _gallowsEntity;
    private GameEntity? _profileHangingNoose;
    private GameEntity? _hangingDropRope;
    private GameEntity? _burningStakeEntity;
    private GameEntity? _burningCrossbeamEntity;
    private MatrixFrame _burningStakeBaseFrame;
    private MatrixFrame _burningCrossbeamBaseFrame;
    private Vec3 _burningCrucifixReferenceVictimPosition = Vec3.Invalid;
    private bool _burningStakeFrameCaptured;
    private bool _burningCrossbeamFrameCaptured;
    private bool _burningCrucifixSyncLogged;
    private bool _burningCrucifixSyncFailureLogged;
    private Vec3 _hangingRopeBeamAnchor = Vec3.Invalid;
    private float _hangingRopeBaseLength;
    private float _hangingLiftElapsed;
    private Vec3 _hangingLiftPosition = Vec3.Invalid;
    private bool _hangingSuspensionActive;
    private float _hangingSuspensionProbeElapsed;
    private bool _frozenVictimFacialSuppressionLogged;
    private bool _frozenVictimFacialSuppressionFailureLogged;
    private bool _frozenVictimPlayerTeamTransferLogged;
    private bool _frozenVictimPlayerTeamTransferFailureLogged;
    private bool _ambientIdleUnavailableLogged;
    private ExecutionActor _actor = ExecutionActor.Undecided;
    private ExecutionOutcome? _outcome;
    private Vec3 _executionerPosition = Vec3.Invalid;
    private Vec3 _executionerStandbyPosition = Vec3.Invalid;
    private Vec2 _executionerStandbyDirection = Vec2.Forward;
    private Vec3 _executionerRetreatPosition = Vec3.Invalid;
    private Vec2 _executionerRetreatDirection = Vec2.Forward;
    private Vec3 _playerExecutionActorPosition = Vec3.Invalid;
    private Vec3 _npcExecutionActorPosition = Vec3.Invalid;
    private Vec3 _victimPosition = Vec3.Invalid;
    private Vec3 _gallowsDeckBoundsMin = Vec3.Invalid;
    private Vec3 _gallowsDeckBoundsMax = Vec3.Invalid;
    private float _stageSurfaceHeight;
    private float _stageSideExtent;
    private float _stageFrontExtent;
    private float _frontInteractionOffset = GallowsMinimumInteractionForward;
    private float _initializationElapsed;
    private float _nextPlacementAttemptElapsed;
    private float _stateElapsed;
    private float _executionElapsed;
    private float _executionActionAttemptElapsed;
    private float _standbyAxeReadbackElapsed;
    private float _guardStandbyMaintenanceElapsed;
    private bool _ceremonyAlliesReleasedToPlayerFormation;
    private float _stoningNextCrowdThrowElapsed;
    private int _stoningCrowdThrowCount;
    private Agent? _stoningPendingProjectileThrower;
    private float _stoningPendingProjectileElapsed;
    private int _stoningPendingProjectileNumber;
    private bool _stoningPendingReleaseStarted;
    private bool _stoningPendingPreservePlayerControl;
    private float _crossbowVolleyAimElapsed;
    private float _crossbowVolleyReleaseElapsed;
    private bool _crossbowVolleyAimActive;
    private bool _crossbowVolleyHoldRequested;
    private bool _crossbowVolleyReleaseActive;
    private Agent.MortalityState _stoningVictimOriginalMortality;
    private float _stoningVictimOriginalHealth;
    private float _stoningVictimOriginalHealthLimit;
    private bool _stoningVictimProtectionCaptured;
    private bool _stoningVictimProtectionReleased;
    private bool _stoningVictimRestraintFailureLogged;
    private bool _crossbowVictimRestraintFailureLogged;
    private bool _crossbowHandAnchorSourceLogged;
    private float _executionerInitialStandbySettleElapsed;
    private float _maximumExecutionActionProgress;
    private float _ceremonyIdleRebindElapsed;
    private float _ceremonyIdleRebindAttemptElapsed;
    private float _methodExecutionPhaseElapsed;
    private MethodExecutionPhase _methodExecutionPhase;
    private ActionIndexCache _executionAction = ActionIndexCache.act_none;
    private ActionIndexCache _victimIdleAction = ActionIndexCache.act_none;
    private ActionIndexCache _executionerIdleAction = ActionIndexCache.act_none;
    private ActionIndexCache _crossbowVolleyIdleAction = ActionIndexCache.act_none;
    private string _executionActionName = string.Empty;
    // Both actors use the same native two-handed execution axe and authored
    // channel-0 clip. The player's original five slots are restored afterwards.
    private string _ceremonyExecutionActionName = TwoHandedBeheadingExecutionAction;
    private string _ceremonyIdleActionName = ExecutionerEntryDefaultIdleAction;
    private string _ceremonyFallbackExecutionActionName = TwoHandedBeheadingFallbackAction;
    private string _ceremonyFallbackIdleActionName = TwoHandedCeremonyFallbackIdleAction;
    private bool _initialized;
    private bool _initializationFailed;
    private bool _ceremonyPrepared;
    private bool _executionerRetreatStarted;
    private bool _executionerRetreatCompleted;
    private bool _groundRetreatDiagnosticLogged;
    private bool _burningCeremonyClearanceBypassLogged;
    private bool _customCeremonyClearanceBypassLogged;
    private bool _executionerStandbyRestored;
    private bool _executionerInitialStandbySettlePending;
    private bool _actionStarted;
    private bool _executionActionPlaying;
    private bool _executionActionAdvanced;
    private bool _executionActionRootCorrectionLogged;
    private bool _postLethalExecutionActionPending;
    private bool _usingGenericExecutionAction;
    private bool _lethalAttempted;
    internal bool HasReachedLethalFrame => _lethalAttempted;
    private bool _missionDeathApplied;
    private bool _campaignCommitPending;
    private bool _campaignCommitAttempted;
    private bool _crossbowDeathEnvironmentCaptured;
    private bool _crossbowOriginalDisableDying;
    private Agent.MortalityState _crossbowVictimOriginalMortality;
    private float _crossbowVictimOriginalHealth;
    private bool _crossbowDeathOverrideApplied;
    private bool _crossbowCeremonialSuspensionArmed;
    private bool _projectileExternalDeathStaged;
    private bool _playerExecutionStateRestored;
    private bool _focusCallbackRegistered;
    private bool _cleanupComplete;
    private bool _strategyCleanupComplete;
    // Pre-execution address (executioner -> prisoner -> crowd), streamed once
    // per ceremony as speech bubbles. The director owns the line plan; the
    // mission view owns the overlay.
    private readonly ExecutionSpeechDirector _speechDirector = new();
    private ExecutionSpeechBubbleMissionView? _speechView;
    private bool _speechPlanned;
    private bool _speechFinishedLogged;
    private float _speechOverlayWaitElapsed;
    private bool _endMissionNextTick;
    private bool _ignitionSucceeded;
    private bool _hangingReleaseSoundAttempted;
    private bool _playerWieldStateCaptured;
    private bool _playerExecutionActionClearedForRestore;
    private bool _playerWeaponSlotsModifiedForCeremony;
    private bool _playerLookDirectionStateCaptured;
    private bool _playerExecutionFacingLockActive;
    private bool _playerOriginalLookDirectionLocked;
    private Vec3 _playerOriginalLookDirection = Vec3.Zero;
    private Vec2 _playerExecutionFacingDirection = Vec2.Forward;
    private float _playerMinimumExecutionFacingDot = 1f;
    private int _playerExecutionFacingWriteCount;
    private bool _playerExecutionRootRotationFlagChecked;
    private TextObject? _aftermathMessageOverride;
    private bool _aftermathMessageDisplayed;
    private bool _aftermathConversationsRestored;
    // The native conversation logic exposes only a mission-wide switch.  Once
    // the sentence is valid, add the original victim Agent to its private
    // per-Agent exclusion set so ordinary town conversations can be restored
    // without making the executed prisoner interactable again.
    private bool _victimConversationBlockAttempted;
    private bool _victimConversationBlockApplied;
    private bool _aftermathSessionReleased;
    private bool _aftermathSpeechPlayed;
    private EquipmentIndex _playerPrimaryWieldedBeforeCeremony = EquipmentIndex.None;
    private EquipmentIndex _playerOffhandWieldedBeforeCeremony = EquipmentIndex.None;
    private readonly MissionWeapon[] _playerWeaponsBeforeCeremony =
        new MissionWeapon[(int)EquipmentIndex.NumAllWeaponSlots];
    private bool _playerPrimaryWasTwoHanded;

    public TownExecutionMissionBehavior(
        ExecutionRequest request,
        ExecutionService service,
        ExecutionSceneActCatalog sceneActs)
        : this(
            request,
            service,
            sceneActs,
            new ExecutionVictimDeathMissionLogic(
                request ?? throw new ArgumentNullException(nameof(request)),
                downstreamDecider: null))
    {
    }

    internal TownExecutionMissionBehavior(
        ExecutionRequest request,
        ExecutionService service,
        ExecutionSceneActCatalog sceneActs,
        ExecutionVictimDeathMissionLogic deathLogic)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _sceneActs = sceneActs ?? throw new ArgumentNullException(nameof(sceneActs));
        _deathLogic = deathLogic ?? throw new ArgumentNullException(nameof(deathLogic));
        _strategy = ExecutionMethodStrategyFactory.Create(CurrentMethod.StringId);
        _strategy.AttachHost(this);
    }

    public ExecutionRequest Request { get; private set; }
    private ExecutionMethodDefinition CurrentMethod => _runtimeMethod ?? Request.Method;
    public ExecutionSessionState State => _stateMachine.State;
    internal bool IsCustomSiteBuilderActive =>
        Request.SceneMode == ExecutionSceneMode.CustomPreset &&
        !_customSiteLayoutConfirmed &&
        !_initialized &&
        !_initializationFailed;

    internal void AttachCustomSiteBuilderView(ExecutionSiteBuilderMissionView view)
    {
        if (!IsCustomSiteBuilderActive)
        {
            return;
        }

        EnsureCustomSiteBuilderCreated();
        _customSiteBuilder?.AttachView(view);
    }

    internal void DetachCustomSiteBuilderView(ExecutionSiteBuilderMissionView view) =>
        _customSiteBuilder?.DetachView(view);

    internal void SelectCustomBuilderWheelOption(string optionId) =>
        _customSiteBuilder?.SelectWheelOption(optionId);

    internal void ConfirmCustomBuilderWheelOption(string optionId) =>
        _customSiteBuilder?.ConfirmWheelOption(optionId);

    internal void SelectCustomBuilderPanelItem(string itemId) =>
        _customSiteBuilder?.SelectPanelItem(itemId);

    internal void FinishCustomBuilderDeployment() =>
        _customSiteBuilder?.FinishCustomBuilderDeployment();

    internal void ReturnToCustomBuilderMainPanel() =>
        _customSiteBuilder?.ReturnToMainPanel();

    internal bool TryValidateCustomSiteLayout(
        ExecutionMethodDefinition method,
        ExecutionSitePreset preset,
        Vec3 origin,
        Vec2 forward,
        out TextObject? error)
    {
        error = null;
        if (Request.SceneMode != ExecutionSceneMode.CustomPreset ||
            _customSiteLayoutConfirmed ||
            _initialized ||
            _initializationFailed ||
            method is null ||
            preset is null ||
            !origin.IsValid ||
            !forward.IsNonZero() ||
            !Mission.IsPositionInsideBoundaries(origin.AsVec2))
        {
            error = new TextObject(
                "{=REX_Builder_Confirm_Failed}The layout could not be initialized; the builder remains open.");
            return false;
        }

        if (!RichExecutionApi.Methods.TryGet(method.StringId, out var registeredMethod) ||
            !ReferenceEquals(registeredMethod, method))
        {
            error = new TextObject(
                "{=REX_Builder_Method_Invalid}The selected execution method is no longer available.");
            return false;
        }

        if (!_sceneActs.TryGet(method.SceneActId, out _))
        {
            error = new TextObject(
                "{=REX_Error_Scene_Act_Missing}The selected punishment has no registered scene act. Nothing was spent and the prisoner lives.");
            return false;
        }

        var validationError = ExecutionSitePresetStore.Validate(preset, method.StringId);
        if (validationError is not null)
        {
            error = new TextObject("{=REX_Builder_Preset_Invalid}Preset validation failed: {ERROR}");
            error.SetTextVariable("ERROR", validationError);
            return false;
        }

        var missingPieces = ExecutionSitePresetFactory.GetMissingRequiredPieceRoles(preset);
        if (missingPieces.Count > 0)
        {
            error = new TextObject("{=REX_Builder_Core_Pieces_Missing}This preset is missing fixed core apparatus: {ROLES}. Choose another valid preset.");
            error.SetTextVariable("ROLES", string.Join(", ", missingPieces));
            return false;
        }

        var missingMarkers = ExecutionSitePresetFactory.GetMissingRequiredMarkerRoles(preset);
        if (missingMarkers.Count > 0)
        {
            error = new TextObject("{=REX_Builder_Core_Markers_Missing}This preset is missing fixed actor or interaction markers: {MARKERS}. Choose another valid preset.");
            error.SetTextVariable("MARKERS", string.Join(", ", missingMarkers));
            return false;
        }

        foreach (var piece in preset.Pieces.Where(piece => !string.IsNullOrWhiteSpace(piece.RoleId)))
        {
            if (ExecutionSitePresetStore.IsObsoletePiece(piece))
            {
                // Presets saved before v0.5.5 still carry the removed
                // verjianchi01 bottom Mesh; it is no longer part of the
                // apparatus, so it must not disable the whole preset.
                continue;
            }

            var available = piece.ResourceType == ExecutionSiteResourceType.Prefab
                ? GameEntity.PrefabExists(piece.ResourceKey)
                : IsCustomSiteMeshAvailable(piece.ResourceKey);
            if (available)
            {
                continue;
            }

            error = new TextObject(
                "{=REX_Builder_Missing_Core_Resource}Required functional resource is unavailable: {RESOURCE}");
            error.SetTextVariable("RESOURCE", piece.ResourceKey);
            return false;
        }

        try
        {
            _ = ExecutionMethodStrategyFactory.Create(method.StringId);
        }
        catch (Exception exception)
        {
            RexLog.Warning($"Custom method '{method.StringId}' has no usable strategy: {exception.Message}");
            error = new TextObject("{=REX_Builder_Method_Invalid}The selected execution method is not supported in this scene.");
            return false;
        }

        if (!ExecutionSceneVisualProfiles.TryGet(method.StringId, out _))
        {
            error = new TextObject("{=REX_Builder_Method_Invalid}The selected execution method has no usable visual profile.");
            return false;
        }

        return true;
    }

    private static bool IsCustomSiteMeshAvailable(string resourceKey)
    {
        try
        {
            var mesh = MetaMesh.GetCopy(resourceKey, showErrors: false, mayReturnNull: true);
            return mesh is not null && mesh.IsValid;
        }
        catch
        {
            return false;
        }
    }

    internal bool TryAcceptCustomSiteLayout(
        ExecutionMethodDefinition method,
        ExecutionSitePreset preset,
        Vec3 origin,
        Vec2 forward,
        out TextObject? error)
    {
        if (!TryValidateCustomSiteLayout(method, preset, origin, forward, out error))
        {
            return false;
        }

        _runtimeMethod = method;
        _strategy = ExecutionMethodStrategyFactory.Create(method.StringId);
        _strategy.AttachHost(this);
        _customSitePreset = preset;
        _customSiteMarkers = preset.Markers
            .GroupBy(marker => marker.RoleId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);
        _placement = new ExecutionScenePlacement(
            origin,
            forward,
            supportsFullStage: true,
            ExecutionPlacementSource.MissionEntryFallback,
            isCompatibilityFallback: true);
        _customSiteLayoutConfirmed = true;
        _initializationElapsed = 0f;
        _nextPlacementAttemptElapsed = 0f;
        RexLog.Info(
            $"Session {Request.SessionId} accepted custom method '{method.StringId}', preset {preset.Id:D} '{preset.Name}' " +
            $"at ({origin.x:0.000}, {origin.y:0.000}, {origin.z:0.000}) with " +
            $"{preset.Pieces.Count} pieces and {preset.Markers.Count} markers.");
        return true;
    }

    internal bool TryAcceptCustomSiteLayout(
        ExecutionSitePreset preset,
        Vec3 origin,
        Vec2 forward,
        out TextObject? error) =>
        TryAcceptCustomSiteLayout(Request.Method, preset, origin, forward, out error);

    // The scene opened on a provisional request so the studio could choose the
    // prisoner in-scene. This swaps in the committed request built from the final
    // selection. It is only legal while the deployment is still provisional: the
    // old request was never begun, so nothing was spent and nobody was harmed.
    internal bool TryCommitDeploymentRequest(ExecutionRequest request, out TextObject? error)
    {
        error = null;
        if (request is null ||
            Request.SceneMode != ExecutionSceneMode.CustomPreset ||
            !_customSiteLayoutConfirmed ||
            _initialized ||
            _initializationFailed)
        {
            error = new TextObject(
                "{=REX_Builder_Commit_Failed}The deployment could not be committed; nothing was spent and the prisoner lives.");
            return false;
        }

        var previous = Request;
        if (!_deathLogic.TryRebindProvisionalRequest(request))
        {
            error = new TextObject(
                "{=REX_Builder_Commit_Failed}The deployment could not be committed; nothing was spent and the prisoner lives.");
            return false;
        }

        try
        {
            Request = request;
            // The mission was claimed under the provisional session ID. Once the
            // final request is installed, remove only that stale pending ID; keep
            // this controller registered under the committed ID for mission end.
            ExecutionSessionCoordinator.Release(previous.SessionId);
            RexLog.Info(
                $"Committed deployment request {request.SessionId} for prisoner '{request.Victim.StringId}' " +
                $"with charge '{request.Charge.StringId}' (provisional session {previous.SessionId} was never begun).");
            return true;
        }
        catch (Exception exception)
        {
            Request = previous;
            _ = _deathLogic.TryRebindProvisionalRequest(previous);
            RexLog.Error("Could not install the committed deployment request; provisional request restored.", exception);
            error = new TextObject(
                "{=REX_Builder_Commit_Failed}The deployment could not be committed; nothing was spent and the prisoner lives.");
            return false;
        }
    }

    internal void RollbackCustomSiteLayout()
    {
        if (Request.SceneMode != ExecutionSceneMode.CustomPreset || _initialized)
        {
            return;
        }

        _customSitePreset = null;
        _customSiteMarkers = null;
        _placement = null;
        _customSiteLayoutConfirmed = false;
        _runtimeMethod = null;
        _strategy = ExecutionMethodStrategyFactory.Create(Request.Method.StringId);
        _strategy.AttachHost(this);
        _initializationElapsed = 0f;
        _nextPlacementAttemptElapsed = 0f;
        RexLog.Info(
            $"Rolled back provisional custom-site layout for session {Request.SessionId}; " +
            "the builder can retry without an active campaign execution.");
    }

    internal ExecutionService DeploymentService => _service;
    public bool CanPlayerUseExecutionPoint =>
        _initialized &&
        ((State == ExecutionSessionState.WaitingForPlayer && !_lethalAttempted) ||
         (State is ExecutionSessionState.Execution or ExecutionSessionState.CrowdReaction &&
          _strategy is IMultiStageExecutionStrategy multiStage &&
          multiStage.IsWaitingForPlayerInput &&
          !multiStage.IsStageBusy)) &&
        !_executionerInitialStandbySettlePending;

    internal bool IsBoundPlayer(Agent? agent) =>
        agent is not null && ReferenceEquals(agent, _playerAgent);

    // ------------------------------------------------------------------

    private ExecutionVictimDeathResult ApplySharedBattlefieldDeath(Agent? visualActor)
    {
        if (visualActor is null || !_deathLogicRegistered())
        {
            return ExecutionVictimDeathResult.InvalidBinding;
        }

        return _deathLogic.ApplyBattlefieldDeath(visualActor);
    }

    private bool _deathLogicRegistered() =>
        Mission.MissionBehaviors.Contains(_deathLogic);

    private bool EnsureBoundPlayerRemainsMainAgent()
    {
        if (_playerAgent is null || !_playerAgent.IsActive())
        {
            return false;
        }

        if (ReferenceEquals(Mission.MainAgent, _playerAgent))
        {
            return true;
        }

        try
        {
            var displacedIndex = Mission.MainAgent?.Index ?? -1;
            Mission.MainAgent = _playerAgent;
            var restored = ReferenceEquals(Mission.MainAgent, _playerAgent);
            RexLog.Warning(
                $"Session {Request.SessionId} restored the bound player as MainAgent " +
                $"(player={_playerAgent.Index}, displaced={displacedIndex}, restored={restored}).");
            return restored;
        }
        catch (Exception exception)
        {
            RexLog.Error("The bound town-entry player could not be restored as MainAgent.", exception);
            return false;
        }
    }

    internal bool IsCeremonyExecutioner(Agent agent) =>
        agent is not null && ReferenceEquals(agent, _executionerAgent);

    internal bool IsCeremonyVictim(Agent agent) =>
        agent is not null && ReferenceEquals(agent, _victimAgent);

    internal bool IsCeremonyGuard(Agent agent) =>
        agent is not null && _guardStandbyBindings.Any(binding => ReferenceEquals(binding.Guard, agent));

    internal bool IsCeremonyCrowd(Agent agent) =>
        agent is not null && _crowdAgents.Contains(agent);

    internal bool TryGetEscortHoldPosition(int index, int count, out Vec3 position, out Vec2 facing)
    {
        position = Vec3.Zero;
        facing = Vec2.Forward;
        if (_placement is null || count <= 0 || !_initialized) return false;
        var column = index % 6;
        var row = index / 6;
        var raw = _placement.Offset(6.5f + row * 1.3f, 8.5f + (column - 2.5f) * 1.15f);
        if (!TrySnapNavigable(raw, out position, requireDirectLineFromPlacement: false)) return false;
        facing = -_placement.Right;
        return true;
    }

    internal bool TryGetPrisonerHoldPosition(int index, int count, out Vec3 position, out Vec2 facing)
    {
        position = Vec3.Zero;
        facing = Vec2.Forward;
        if (_placement is null || count <= 0 || !_initialized || index < 0) return false;
        var column = index % 6;
        var row = index / 6;
        var raw = _placement.Offset(-6.5f - row * 1.3f, 8.5f + (column - 2.5f) * 1.15f);
        if (!TrySnapNavigable(raw, out position, requireDirectLineFromPlacement: false)) return false;
        facing = _placement.Right;
        return true;
    }

    internal bool IsCeremonyInProgress =>
        State is ExecutionSessionState.CeremonyPreparation or
            ExecutionSessionState.Execution or
            ExecutionSessionState.CrowdReaction;

    public override void OnCreated()
    {
        base.OnCreated();
        EnsureLegacyConstructorDeathLogicRegistration();
    }

    private void EnsureLegacyConstructorDeathLogicRegistration()
    {
        if (Mission.MissionBehaviors.Contains(_deathLogic))
        {
            return;
        }

        try
        {
            // The normal SubModule path injects both behaviors atomically. Keep
            // the existing public three-argument constructor behavior-compatible
            // for external callers by completing the same registration here.
            var downstreamDecider = Mission.MissionBehaviors
                .OfType<IAgentStateDecider>()
                .FirstOrDefault();
            if (!_deathLogic.TryAttachDownstreamDecider(downstreamDecider))
            {
                throw new InvalidOperationException(
                    "The legacy controller could not attach its downstream Agent-state decider.");
            }

            Mission.AddMissionBehavior(_deathLogic);
            var deathLogicIndex = Mission.MissionBehaviors.IndexOf(_deathLogic);
            var controllerIndex = Mission.MissionBehaviors.IndexOf(this);
            var targetIndex = downstreamDecider is MissionBehavior downstreamBehavior
                ? Mission.MissionBehaviors.IndexOf(downstreamBehavior)
                : controllerIndex;
            if (deathLogicIndex < 0 || targetIndex < 0)
            {
                throw new InvalidOperationException(
                    "The legacy death logic or controller disappeared during registration.");
            }

            if (deathLogicIndex > targetIndex)
            {
                Mission.MissionBehaviors.RemoveAt(deathLogicIndex);
                Mission.MissionBehaviors.Insert(targetIndex, _deathLogic);
            }

            var firstDecider = Mission.MissionBehaviors
                .OfType<IAgentStateDecider>()
                .FirstOrDefault();
            if (!ReferenceEquals(firstDecider, _deathLogic))
            {
                throw new InvalidOperationException(
                    "The legacy death logic was not installed as the first Agent-state decider.");
            }

            RexLog.Warning(
                $"Session {Request.SessionId} used the public three-argument controller constructor; " +
                "its dedicated death logic was automatically registered at deathDeciderIndex=0.");
        }
        catch (Exception exception)
        {
            RexLog.Error(
                $"DEATH_INVARIANT_FAILED session={Request.SessionId}: the public controller constructor " +
                "could not complete dedicated death-logic registration. The mission remains active.",
                exception);
        }
    }

    public override void AfterStart()
    {
        base.AfterStart();
        ExecutionSessionCoordinator.RegisterController(this);
        if (Request.SceneMode == ExecutionSceneMode.CustomPreset)
        {
            if (TryBindPlayerForCustomBuilder())
            {
                EnsureCustomSiteBuilderCreated();
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject(
                        "{=REX_Builder_Enter}Choose a place in town, then press B to open the custom execution-site deployment studio.").ToString()));
            }

            return;
        }

        TryInitializeScene();
    }

    private bool TryBindPlayerForCustomBuilder()
    {
        var currentPlayer = _playerAgent ?? Mission.MainAgent ?? Agent.Main;
        if (currentPlayer is null || !currentPlayer.IsActive())
        {
            return false;
        }

        if (_playerAgent is null)
        {
            _playerAgent = currentPlayer;
            RexLog.Info(
                $"Session {Request.SessionId} bound town-entry player {_playerAgent.Index} for custom-site building without moving the Agent.");
        }

        return EnsureBoundPlayerRemainsMainAgent();
    }

    private void EnsureCustomSiteBuilderCreated()
    {
        if (_customSiteBuilder is null && IsCustomSiteBuilderActive)
        {
            _customSiteBuilder = new ExecutionSiteBuilderController(this, Mission);
            RexLog.Info(
                $"Session {Request.SessionId} entered custom-site state WaitingForBuilder; no prisoner, apparatus or ceremony personnel were spawned.");
        }
    }

    public override void OnMissionTick(float dt)
    {
        base.OnMissionTick(dt);
        // Cancellation cleanup may remove the victim. Honor the pending exit
        // before observing that removal or running another scene update.
        if (_endMissionNextTick)
        {
            _endMissionNextTick = false;
            Mission.EndMission();
            return;
        }

        if (_initialized)
        {
            TryStageObservedProjectileVictimDeath("mission tick");
            if (!_lethalAttempted && _deathLogic.HasUnexpectedVictimRemoval)
            {
                CancelSceneAndReturn(
                    ExecutionFailureReason.VictimUnavailable,
                    new TextObject("{=REX_Error_Victim_Unavailable}The selected prisoner is no longer in the required custody."));
                return;
            }
        }

        if (_playerAgent is not null && !EnsureBoundPlayerRemainsMainAgent())
        {
            if (!_lethalAttempted)
            {
                CancelSceneAndReturn(
                    ExecutionFailureReason.PlayerUnavailable,
                    new TextObject(
                        "{=REX_Error_Player_Unavailable}The player character is no longer available. Nothing was spent and the prisoner lives."));
            }

            return;
        }

        if (Request.SceneMode == ExecutionSceneMode.CustomPreset && !_customSiteLayoutConfirmed)
        {
            if (!TryBindPlayerForCustomBuilder())
            {
                return;
            }

            EnsureCustomSiteBuilderCreated();
            _customSiteBuilder?.Tick(dt);
            return;
        }

        if (!_initialized)
        {
            if (_initializationFailed)
            {
                return;
            }

            _initializationElapsed += MathF.Max(0f, dt);
            if (!TryInitializeScene() && _initializationElapsed >= InitializationTimeoutSeconds)
            {
                CancelSceneAndReturn(
                    ExecutionFailureReason.ScenePlacementFailed,
                    new TextObject(
                        "{=REX_Error_No_Safe_Position}No safe navigable execution site could be found in this town center. Nothing was spent and the prisoner lives."));
            }

            return;
        }

        RetryPendingEntityRemovals();
        TickImpaledCorpseDisplays(dt);
        MaintainGuardStandbyStates(dt);
        MaintainPlayerExecutionFacingLock();
        MaintainFrozenVictimFacialSuppression();
        MaintainStoningVictimRestraint();
        MaintainCrossbowVictimRestraint();
        MaintainCrossbowVolleyStandby();
        TickCrossbowVolleyStagger(dt);
        if (_usePointHost is not null &&
            State != ExecutionSessionState.WaitingForPlayer &&
            _strategy is not IMultiStageExecutionStrategy)
        {
            // A transient native Remove failure must not turn the consumed
            // interaction marker into a persistent collision body. Retry from
            // every later state, including execution and aftermath.
            RemoveConsumedUsePointMarker();
        }

        _stateElapsed += MathF.Max(0f, dt);
        switch (State)
        {
            case ExecutionSessionState.WaitingForPlayer:
                TickWaitingForPlayer(dt);
                break;
            case ExecutionSessionState.CeremonyPreparation:
                TickCeremonyPreparation();
                break;
            case ExecutionSessionState.Execution:
                TickExecution(dt);
                break;
            case ExecutionSessionState.CrowdReaction:
                _strategy.TickAfterDeath(dt);
                TickPostLethalExecutionAction();
                if (_stateElapsed >= CrowdReactionSeconds &&
                    (_strategy is not IMultiStageExecutionStrategy multiStage ||
                     multiStage.IsPresentationComplete))
                {
                    FinishAftermath();
                }

                break;
            case ExecutionSessionState.Aftermath:
                _strategy.TickAfterDeath(dt);
                FinishAftermath();
                TickCrowdDispersal(dt);
                break;
        }

        // Speech continues after the player gives the order. The waiting state is
        // only where the first request starts; execution and aftermath still
        // have lines from that same reply.
        if (_initialized)
        {
            PumpCeremonySpeech(dt);
        }

        // Run method-owned visual tracking exactly once after state-specific
        // movement has completed. Hanging uses this while waiting for input as
        // well as after lifting, so the beam rope is attached to the current
        // neck bone from the first visible mission tick onward.
        _strategy.TickSceneVisuals(dt);
        SynchronizeBurningCrucifixToVictim();
    }

    private void PumpCeremonySpeech(float dt)
    {
        ExecutionAddressLlmBridge.Tick(Request.SessionId);
        if (!_speechPlanned && State == ExecutionSessionState.WaitingForPlayer)
        {
            var overlay = Mission.GetMissionBehavior<ExecutionSpeechBubbleMissionView>();
            if (ExecutionSpeechBubbleBridge.IsReady || overlay?.IsBubbleReady() == true)
            {
                TryPrepareCeremonySpeech();
            }
            else
            {
                _speechOverlayWaitElapsed += MathF.Max(0f, dt);
                if (_speechOverlayWaitElapsed >= 8f)
                {
                    RexLog.Warning(
                        $"Session {Request.SessionId} did not get a speech overlay; using the local address.");
                    TryPrepareCeremonySpeech();
                }
            }
        }

        TickCeremonySpeech(dt);
    }

    private void TickWaitingForPlayer(float dt)
    {
        if (_executionerInitialStandbySettlePending)
        {
            _executionerInitialStandbySettleElapsed += MathF.Max(0f, dt);
            if (!TryRestoreExecutionerEntryStandby())
            {
                if (_executionerInitialStandbySettleElapsed >=
                    InitialExecutionerStandbySettleTimeoutSeconds)
                {
                    if (UsesConfirmedCustomSite)
                    {
                        _executionerInitialStandbySettlePending = false;
                        _executionerInitialStandbySettleElapsed = 0f;
                        RexLog.Error(
                            $"Session {Request.SessionId} could not retain the executioner standby read-back " +
                            "for the confirmed custom site; keeping the current root and leaving the mission active.");
                    }
                    else
                    {
                        CancelSceneAndReturn(
                            ExecutionFailureReason.ScenePlacementFailed,
                            new TextObject(
                                "{=REX_Error_No_Safe_Position}No safe navigable execution site could be found in this town center. Nothing was spent and the prisoner lives."));
                    }
                }

                return;
            }

            _executionerInitialStandbySettlePending = false;
            _executionerInitialStandbySettleElapsed = 0f;
            RexLog.Info(
                $"Session {Request.SessionId} confirmed the executioner standby mark on the first mission tick.");
            var settledExecutioner = _executionerAgent;
            if (RequiresCeremonyWeapon() &&
                (settledExecutioner is null ||
                 !TryEnsureCeremonyWeaponWielded(
                     settledExecutioner,
                     "initial executioner standby after first-tick settle")))
            {
                if (UsesConfirmedCustomSite)
                {
                    RexLog.Error(
                        $"Session {Request.SessionId} could not retain the ceremony weapon at the " +
                        "confirmed custom standby; later execution preparation will retry without ending the mission.");
                }
                else
                {
                    CancelSceneAndReturn(
                        ExecutionFailureReason.UnexpectedError,
                        new TextObject(
                            "{=REX_Error_Animation_Playback}The execution animation could not be played safely. Nothing was spent and the prisoner lives."));
                }

                return;
            }
        }

        if (!RequiresCeremonyWeapon() || _executionerAgent is null)
        {
            return;
        }

        _standbyAxeReadbackElapsed += MathF.Max(0f, dt);
        if (_standbyAxeReadbackElapsed < 0.75f)
        {
            return;
        }

        _standbyAxeReadbackElapsed = 0f;
        if (IsCeremonyWeaponStillWielded(_executionerAgent, "waiting executioner"))
        {
            return;
        }

        RexLog.Warning(
            $"Session {Request.SessionId} detected a sheathed or replaced ceremony weapon; re-wielding it.");
        if (!TryEnsureCeremonyWeaponWielded(_executionerAgent, "waiting executioner repair"))
        {
            if (UsesConfirmedCustomSite)
            {
                RexLog.Error(
                    $"Session {Request.SessionId} could not repair the custom-site standby weapon; " +
                    "the scene remains active and execution preparation will retry.");
            }
            else
            {
                CancelSceneAndReturn(
                    ExecutionFailureReason.UnexpectedError,
                    new TextObject(
                        "{=REX_Error_Animation_Playback}The execution animation could not be played safely. Nothing was spent and the prisoner lives."));
            }
        }
    }

    // ------------------------------------------------------------------
    // Pre-execution address. The ceremony speaks once: the executioner
    // announces the judgement; personality and verified facts shape replies.
    // Pauses and zero-to-two residents replace the fixed roll call. Speech is purely presentational
    // and never gates the player, so every failure path degrades to silence
    // instead of cancelling a valid execution.
    // ------------------------------------------------------------------
    /// <summary>
    /// Resolves the address plan once the ceremony is ready. Called from the
    /// single scene-ready point so the bubble can never start twice.
    /// </summary>
    private void TryPrepareCeremonySpeech()
    {
        if (_speechPlanned)
        {
            return;
        }

        _speechPlanned = true;
        try
        {
            if (_executionerAgent is not null &&
                ExecutionSpeechDirector.AddressOverride?.Invoke(
                    Request, _executionerAgent, _victimAgent, _crowdAgents, _speechDirector) == true)
            {
                RexLog.Info($"Session {Request.SessionId} handed the pre-execution address to its host override.");
                return;
            }

            if (!_speechDirector.TryPrepare(
                    Request,
                    _executionerAgent,
                    _victimAgent,
                    _crowdAgents,
                    out var failureDetail))
            {
                RexLog.Warning(
                    $"Session {Request.SessionId} will not play a pre-execution address: {failureDetail}.");
                _speechDirector.Abort();
                return;
            }

            RexLog.Info(
                $"Session {Request.SessionId} queued the {Request.Tone} pre-execution address " +
                "(fact-bound judgement, individual response and optional crowd voices).");
        }
        catch (Exception exception)
        {
            // A missing line must never break the ceremony.
            RexLog.Error(
                "Preparing the pre-execution address failed; the ceremony continues without it.",
                exception);
            _speechDirector.Abort();
        }
    }

    private void TickCeremonySpeech(float dt)
    {
        if (!_speechPlanned)
        {
            return;
        }

        // A prepared plan reports IsBusy from its very first (Idle) tick, so the
        // director is guaranteed one Tick that starts the first line. Only a
        // finished or aborted plan reaches this early return.
        if (!_speechDirector.IsBusy)
        {
            if (!_speechFinishedLogged)
            {
                _speechFinishedLogged = true;
                _speechView ??= Mission.GetMissionBehavior<ExecutionSpeechBubbleMissionView>();
                _speechView?.ClearLine();
            }

            return;
        }

        _speechFinishedLogged = false;

        try
        {
            _speechView ??= Mission.GetMissionBehavior<ExecutionSpeechBubbleMissionView>();
            _speechDirector.Tick(dt, _speechView);
            if (!_speechDirector.IsBusy && !_speechFinishedLogged)
            {
                _speechFinishedLogged = true;
                _speechView?.ClearLine();
            }
        }
        catch (Exception exception)
        {
            RexLog.Error(
                "The pre-execution address escaped its tick boundary; ending it immediately.",
                exception);
            EndCeremonySpeech();
        }
    }

    /// <summary>
    /// Stops and clears the address. Called whenever the player takes control
    /// of the ceremony or the mission starts tearing down.
    /// </summary>
    private void EndCeremonySpeech(bool keepLaterPhases = false)
    {
        try
        {
            if (keepLaterPhases)
            {
                _speechDirector.FinishCurrentPhase();
                _speechFinishedLogged = false;
                return;
            }
            ExecutionAddressLlmBridge.Cancel(Request.SessionId);
            _speechDirector.Abort();
            _speechView ??= Mission.GetMissionBehavior<ExecutionSpeechBubbleMissionView>();
            _speechView?.AbortLines();
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not clear the pre-execution address overlay.", exception);
        }
    }
    private sealed class GuardStandbyBinding
    {
        internal GuardStandbyBinding(
            Agent guard,
            string actionName,
            EquipmentIndex meleeSlot)
        {
            Guard = guard;
            ActionName = actionName;
            MeleeSlot = meleeSlot;
        }

        internal Agent Guard { get; }

        internal string ActionName { get; }

        internal EquipmentIndex MeleeSlot { get; }

        internal bool RepairFailureLogged { get; set; }
    }

}
