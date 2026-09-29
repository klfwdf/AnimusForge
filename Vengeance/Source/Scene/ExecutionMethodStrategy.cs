using RichExecutions.Core;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

/// <summary>
/// Host capabilities that an execution-method strategy can use. Implemented by
/// <see cref="TownExecutionMissionBehavior"/>; strategies never own shared
/// ceremony state directly, they only receive host callbacks and expose their
/// own method-specific pipeline (flow, death chain, after-death upkeep).
/// </summary>
internal interface IExecutionSceneHost
{
    ExecutionRequest Request { get; }

    Agent? VictimAgent { get; }

    Agent? ExecutionerAgent { get; }

    Agent? PlayerAgent { get; }

    ExecutionActor Actor { get; }

    IExecutionSceneAct? SceneAct { get; }

    ExecutionScenePlacement? Placement { get; }

    Vec3 VictimPosition { get; set; }

    Vec3 ExecutionActorPosition { get; }

    Vec2 ExecutionActorDirection { get; }

    /// <summary>Position that the hanging strategy lifts the victim to (invalid otherwise).</summary>
    Vec3 HangingLiftPosition { get; set; }

    /// <summary>
    /// Noose decoration spawned by the shared visual profile at its proven-safe
    /// post-stage setup point. Hanging may move it only after initialization.
    /// </summary>
    GameEntity? HangingNooseEntity { get; }

    /// <summary>
    /// Stable gallows entity used as the original prisoner's native parent
    /// while the hanging target frame is driven.
    /// </summary>
    GameEntity? HangingSupportEntity { get; }

    /// <summary>True when the method action has started and is being observed.</summary>
    bool ActionStarted { get; set; }

    /// <summary>True once the lethal frame has been attempted.</summary>
    bool LethalAttempted { get; set; }

    /// <summary>True once the staged method sequence has completed (hanging/burning).</summary>
    bool MethodSequenceCompleted { get; set; }

    /// <summary>Current action-progress observation state.</summary>
    float MaximumExecutionActionProgress { get; set; }

    /// <summary>Elapsed time spent waiting for the method action to advance.</summary>
    float ExecutionActionAttemptElapsed { get; set; }

    /// <summary>Poses an agent at a world position facing a direction.</summary>
    void PoseAgentAt(Agent agent, Vec3 position, Vec2 direction);

    bool TryPlayAction(Agent agent, string actionName, string purpose);

    void LogInfo(string message);

    void LogWarning(string message);

    void LogError(string message, System.Exception? exception = null);

    void RequestLethalFrame();

    /// <summary>Applies the shared single-Blow battlefield death; strategies call this from their own death chain.</summary>
    ExecutionVictimDeathResult ApplySharedBattlefieldDeath(Agent? visualActor);

    /// <summary>Creates the detached head visual; only the beheading strategy should call it.</summary>
    void CreateDetachedHead();

    /// <summary>Restores the head source when the beheading sentence is not committed.</summary>
    void RestoreDetachedHeadSourceIfUncommitted();

    /// <summary>Plays the hanging gallows release sound; only the hanging strategy should call it.</summary>
    void PlayHangingReleaseSound();

    /// <summary>Starts an action clip on channel 0 with fallback/validation.</summary>
    bool TryStartExecutionActionClip(Agent? actionAgent, string actionName, bool isGenericFallback);

    /// <summary>Observes the current method-action progress on channel 0.</summary>
    float ObserveExecutionAction(Agent? actionAgent);

    /// <summary>Reasserts the action agent's root so the ceremony actor stays on its mark.</summary>
    void ReassertExecutionActionRoot(Agent? actionAgent, string phase);

    /// <summary>Applies the shared crowd reaction / post-lethal scene state.</summary>
    void EnterPostLethalSceneState(bool applyCrowdReaction);

    /// <summary>Fires the hanging/burning victim's method sequence completion (calls the shared lethal flow).</summary>
    void CompleteMethodExecutionSequence(string finalPose);

    /// <summary>Starts the staged method execution action (hanging/burning).</summary>
    void StartStagedMethodExecutionAction();

    /// <summary>
    /// Starts the first real stone throw. Player throws use an unlocked overlay
    /// channel so MainAgent keeps movement and camera control.
    /// </summary>
    bool TryStartStoningFirstThrow(Agent thrower, bool preservePlayerControl);

    /// <summary>
    /// Drives the four culture-matched supporting crossbowmen through the same
    /// ready/hold/release clip as the NPC executioner. It is a no-op for player
    /// execution so the personal firing flow remains unchanged.
    /// </summary>
    void DriveCrossbowVolleyAction(string actionName, string purpose);

    /// <summary>
    /// Launches the primary real bolt and, for an NPC sentence, one real bolt
    /// from each supporting crossbowman during the same release frame.
    /// </summary>
    bool LaunchCrossbowVolley(Agent primaryShooter, string trigger);

    /// <summary>Equips or restores the execution axe for the beheading strategy.</summary>
    bool TryEnsureExecutionAxeWielded(Agent agent, string purpose);

    /// <summary>Reads the execution action root position for the active actor.</summary>
    Vec3 GetExecutionActorPosition();

    /// <summary>Returns the expected execution actor facing direction.</summary>
    Vec2 GetExecutionActorDirection();

    /// <summary>Warms up a single action on the victim agent (used by prefetch).</summary>
    bool TryPlayRequiredAction(
        Agent? agent,
        string actionName,
        string purpose,
        bool forceFullBody,
        float blendInPeriod = -0.2f);

    /// <summary>True when the shared stage (gallows) has been accepted.</summary>
    bool IsStageReady { get; }

    /// <summary>Mission instance hosting the ceremony.</summary>
    Mission Mission { get; }

    /// <summary>Spawns a native prefab as a collision-free visual entity.</summary>
    GameEntity? SpawnPrefab(string prefabName, Vec3 position, bool createPhysics, bool callScriptCallbacks, float yawDegrees = 0f, float pitchDegrees = 0f, float rollDegrees = 0f, float uniformScale = 1f);

    /// <summary>Removes a spawned visual entity safely.</summary>
    bool RemoveSpawnedEntity(GameEntity? entity);

    /// <summary>Disables an entity's collision tree.</summary>
    bool TryDisableEntityCollision(GameEntity entity, string purpose);

    /// <summary>Gets the execution actor (player or executioner) for the active actor role.</summary>
    Agent? GetActiveExecutionActor();

    /// <summary>Reads the current execution action name used by the ceremony.</summary>
    string GetCeremonyExecutionActionName();

    /// <summary>Returns true when the staged method execution is in progress.</summary>
    bool IsStagedMethodInProgress();

    /// <summary>Returns true when the method sequence has started (hanging/burning).</summary>
    bool HasStartedMethodSequence();

    /// <summary>Clears the execution action channel for the active actor.</summary>
    bool TryClearExecutionActionChannel(Agent actionAgent);

    /// <summary>Reads the current victim channel-0 action.</summary>
    ActionIndexCache GetVictimCurrentAction();

    /// <summary>Reads the victim channel-0 action progress.</summary>
    float GetVictimCurrentActionProgress();

    /// <summary>
    /// Persists the original prisoner's exact appearance and frozen channel-0
    /// pose as a reusable impaled-corpse decoration.
    /// </summary>
    bool SaveImpaledCorpseDisplay(string actionName, float actionProgress);

    /// <summary>Starts the method-specific execution action with the shared fallback chain.</summary>
    void StartMethodExecutionAction();

    /// <summary>Runs the shared method-action observation and dispatch (one tick).</summary>
    void TickMethodExecutionDispatch(float dt);

    /// <summary>Starts a visible fallback action; returns true when one bound.</summary>
    bool TryStartVisibleExecutionFallback(Agent? actionAgent, string failedActionName);

    /// <summary>Logs and continues after an animation failure without ending the mission.</summary>
    void ContinueAfterAnimationFailure(string detail);

    /// <summary>Starts one burning ignition stage (world-space native fire prefabs).</summary>
    bool TryIgniteExecutionEffectRoles(string stage, params string[] roleIds);

    /// <summary>Plays one short-lived native particle burst at a world position.</summary>
    bool TryPlayWorldParticleBurst(string particleSystemName, Vec3 position, string purpose);

    /// <summary>
    /// Clears a completed staged-method action and immediately restores player
    /// movement/equipment without releasing an NPC executioner to hostile AI.
    /// </summary>
    bool ReleaseExecutionActorControl(string purpose);

    /// <summary>Plays the beheading blood burst, detached head and cut sound.</summary>
    void PlayBeheadingLethalEffects();

    /// <summary>Disables entity collision but keeps cloth simulation running.</summary>
    bool DisableCollisionKeepCloth(GameEntity entity, string purpose);

    /// <summary>
    /// Freezes the prisoner in its current pose at a fixed world position for
    /// a fake-death suspension (hanging). The Agent keeps its visuals, AI is
    /// paused, and no native RegisterBlow/corpse pipeline is entered. Returns
    /// false when the agent cannot be frozen.
    /// </summary>
    bool FreezeVictimForSuspension(Vec3 position, string role);

    /// <summary>Maintains the original prisoner on an arbitrary moving apparatus frame.</summary>
    bool SetVictimPresentationFrame(
        Vec3 position,
        Vec2 direction,
        Vec3 up,
        bool freezeAction,
        bool closeEyes,
        string role,
        Vec3? facing = null);

    /// <summary>Returns a profile prop captured by its stable role ID.</summary>
    GameEntity? GetProfileEntity(string roleId);

    /// <summary>Converts a method-local right/forward/up offset into a world point.</summary>
    Vec3 GetMethodWorldPoint(float right, float forward, float up = 0f);

    /// <summary>Moves and configures the persistent multi-stage F-key proxy.</summary>
    bool UpdateMultiStageUsePoint(
        Vec3 position,
        TextObject description,
        TextObject action,
        bool enabled);

    /// <summary>Spawns one unarmed, invulnerable local-culture helper on the execution side.</summary>
    Agent? SpawnMethodHelper(Vec3 position, Vec2 direction, string role, int cultureSlot);

    /// <summary>Starts native scripted walking toward a stage point without teleporting.</summary>
    bool MoveAgentToStagePoint(Agent agent, Vec3 position, Vec2 direction, string purpose);

    /// <summary>Captures the player's current stage root and locks only facing until released.</summary>
    bool BeginPlayerStageControl(Vec2 direction, string purpose);

    /// <summary>Clears the current stage action and restores player movement/look locking.</summary>
    bool EndPlayerStageControl(string purpose);

    /// <summary>Returns a world-space bone position from the original prisoner.</summary>
    bool TryGetVictimBoneWorldPosition(sbyte boneIndex, out Vec3 position);

    /// <summary>Triggers a bounded limb blood burst and optional world particle.</summary>
    void PlayVictimStageBlood(sbyte boneIndex, float intensity, int worldBurstCount, string purpose);

    /// <summary>Requests one native prisoner voice.</summary>
    void PlayVictimVoice(SkinVoiceManager.SkinVoiceType voice, string purpose);

    /// <summary>Stretches a collision-free entity between two world points along its longest mesh axis.</summary>
    bool SetEntityBetweenPoints(GameEntity entity, Vec3 start, Vec3 end, string purpose);
}

/// <summary>
/// Optional capability implemented only by execution methods that keep one
/// persistent F-key proxy and advance through several idempotent stages.
/// </summary>
internal interface IMultiStageExecutionStrategy
{
    bool IsWaitingForPlayerInput { get; }

    bool IsStageBusy { get; }

    bool IsPresentationComplete { get; }

    bool TryAdvancePlayerStage();
}

/// <summary>Receives the fully spawned profile props and persistent F host.</summary>
internal interface IMethodSceneReadyStrategy
{
    void OnMethodSceneReady();
}

/// <summary>
/// One fully isolated execution-method pipeline. Each strategy owns its own
/// scene visuals, flow playback, lethal-frame effects and the complete death
/// chain (including after-death upkeep such as hanging suspension). Strategies
/// must not depend on each other; only the shared host is used.
/// </summary>
internal interface IExecutionMethodStrategy
{
    string MethodId { get; }

    bool UsesStagedExecution { get; }

    bool RequiresExecutionAxe { get; }

    /// <summary>
    /// True when the visible mission Agent remains alive in a frozen execution
    /// pose and the real character death is committed only on mission exit.
    /// </summary>
    bool DefersVictimDeathToMissionExit { get; }

    void AttachHost(IExecutionSceneHost host);

    /// <summary>Spawns method-specific visual props on top of the shared stage.</summary>
    void SetupSceneVisuals();

    /// <summary>
    /// Maintains method-specific scene visuals once per initialized mission
    /// tick, including while the ceremony is still waiting for player input.
    /// </summary>
    void TickSceneVisuals(float dt);

    /// <summary>Warms up actions/particles used by this method before the ceremony.</summary>
    void PrefetchActions();

    void StartExecutionAction();

    void TickExecution(float dt);

    void BeginMethodSequence(string trigger);

    void TickMethodSequence(float dt);

    void ApplyLethalFrameEffects();

    /// <summary>Applies this method's death chain and returns whether death was applied.</summary>
    bool ApplyDeath();

    /// <summary>Runs every tick after the lethal frame until the session ends.</summary>
    void TickAfterDeath(float dt);

    void Cleanup();
}
