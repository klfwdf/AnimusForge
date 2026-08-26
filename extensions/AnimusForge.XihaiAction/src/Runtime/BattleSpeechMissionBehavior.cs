using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SceneActions.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    internal sealed partial class BattleSpeechMissionBehavior : MissionBehavior
    {
        private readonly ConcurrentQueue<BattleSpeechCapturedInputV1> _inbound =
            new ConcurrentQueue<BattleSpeechCapturedInputV1>();
        private ActiveBattleSpeechSessionV1 _active;
        private bool _closed = true;
        private bool _mcmDisabled;
        private long _nextSessionGeneration;
        private bool _battlefieldBaselineInitialized;
        private int _battlefieldBaselineFriendlyCount = -1;
        private int _battlefieldBaselineEnemyCount = -1;
        private int _battlefieldFriendlyRemoved;
        private int _battlefieldEnemyRemoved;

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
        internal bool IsSessionActive => !_closed;
        internal bool CanOpenSpeechMenu
        {
            get
            {
                return !_closed && TryResolvePhase(out _);
            }
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            if (!BattleSpeechRuntimeHost.IsInitialized ||
                BattleSpeechRuntimeHost.Settings == null ||
                !BattleSpeechRuntimeHost.ConfigurationValid)
            {
                return;
            }
            _closed = false;
            _mcmDisabled = !BattleSpeechRuntimeHost.IsSpeechEnabled;
            _battlefieldBaselineInitialized = false;
            _battlefieldBaselineFriendlyCount = -1;
            _battlefieldBaselineEnemyCount = -1;
            _battlefieldFriendlyRemoved = 0;
            _battlefieldEnemyRemoved = 0;
            BattleSpeechEnemyProximityCache.Reset(Mission);
            BattleSpeechRuntimeHost.BindSession(this);
            SceneActionsLog.Info("BATTLE_SPEECH", "Mission framework session activated.");
        }

        internal bool TryEnqueue(BattleSpeechCapturedInputV1 input)
        {
            if (_closed || !BattleSpeechRuntimeHost.IsSpeechEnabled ||
                input == null || !ReferenceEquals(input.Mission, Mission))
            {
                return false;
            }
            _inbound.Enqueue(input);
            return true;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (_closed || !ReferenceEquals(Mission.Current, Mission)) return;
            try
            {
                if (!BattleSpeechRuntimeHost.IsSpeechEnabled)
                {
                    DisableFromHost(
                        "BattleSpeech disabled in MCM; pending speech input was discarded.");
                    return;
                }
                if (_mcmDisabled)
                {
                    _mcmDisabled = false;
                    SceneActionsLog.Info(
                        "BATTLE_SPEECH_MCM",
                        "BattleSpeech re-enabled for the active Mission.");
                }
                EnsureBattlefieldBaseline();
                while (_inbound.TryDequeue(out BattleSpeechCapturedInputV1 input))
                {
                    ProcessInput(input);
                }
                ProcessV2ClassifierCompletions();
                ProgressSession();
            }
            catch (Exception ex)
            {
                SceneActionsLog.Error("BATTLE_SPEECH", "Mission tick failed closed.", ex);
                Close("Framework exception.");
            }
        }

        public override void OnAgentHit(
            Agent affectedAgent,
            Agent affectorAgent,
            in MissionWeapon affectorWeapon,
            in Blow blow,
            in AttackCollisionData attackCollisionData)
        {
            base.OnAgentHit(
                affectedAgent,
                affectorAgent,
                in affectorWeapon,
                in blow,
                in attackCollisionData);
            BattleSpeechEnemyProximityCache.Invalidate(
                Mission,
                affectedAgent,
                affectorAgent);
            bool combatStarted = BattleSpeechEnemyProximityCache
                .MarkCombatStartedFromConflict(Mission, affectedAgent, affectorAgent);
            if (combatStarted)
            {
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_STAGE",
                    "Mission combat mode latched by opposing-agent hit.");
            }
            if (_active != null &&
                BattleSpeechEnemyProximityCache.IsCombatStarted(Mission))
            {
                // Any real opposing-agent hit locks this Mission into the
                // text-only combat speech channel, even when the selected
                // speaker is standing behind the active melee.
                EnterCombatSpeechMode(_active);
            }
        }

        public override void OnAgentRemoved(
            Agent affectedAgent,
            Agent affectorAgent,
            AgentState agentState,
            KillingBlow blow)
        {
            bool combatStarted = BattleSpeechEnemyProximityCache
                .MarkCombatStartedFromConflict(Mission, affectedAgent, affectorAgent);
            if (combatStarted)
            {
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_STAGE",
                    "Mission combat mode latched by opposing-agent removal.");
            }
            RecordBattlefieldRemoval(affectedAgent);
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
            BattleSpeechEnemyProximityCache.Invalidate(
                Mission,
                affectedAgent,
                affectorAgent);
            if (_active != null && ReferenceEquals(_active.Speaker, affectedAgent))
            {
                CancelActive("The speaker left the Mission.");
            }
            else if (_active != null &&
                     BattleSpeechEnemyProximityCache.IsCombatStarted(Mission))
            {
                EnterCombatSpeechMode(_active);
            }
        }

        public override void OnAgentTeamChanged(Team previousTeam, Team newTeam, Agent agent)
        {
            base.OnAgentTeamChanged(previousTeam, newTeam, agent);
            BattleSpeechEnemyProximityCache.Invalidate(Mission, agent, null);
            if (_active != null && ReferenceEquals(_active.Speaker, agent))
            {
                CancelActive("The speaker changed team.");
            }
        }

        protected override void OnEndMission()
        {
            Close("OnEndMission");
            base.OnEndMission();
        }

        public override void OnMissionStateFinalized()
        {
            Close("OnMissionStateFinalized");
            base.OnMissionStateFinalized();
        }

        public override void OnRemoveBehavior()
        {
            Close("OnRemoveBehavior");
            base.OnRemoveBehavior();
        }

        internal void CloseFromHost(string reason)
        {
            Close(reason);
        }

        private void ProcessInput(BattleSpeechCapturedInputV1 input)
        {
            if (input == null)
            {
                return;
            }
            if (input.InputKind == BattleSpeechInputKindV1.QueuedNpcReplyCandidate)
            {
                TryAcceptNpcReplyCandidate(input);
                return;
            }
            if (input.InputKind == BattleSpeechInputKindV1.ShownNpcReply)
            {
                TryCompleteNpcSpeech(input);
                return;
            }
            if (input.InputKind == BattleSpeechInputKindV1.GeneratedNpcReply)
            {
                // Background AF completion carries only the frozen AgentIndex.
                // Resolve the live Agent on the Mission thread before any
                // Bannerlord object is inspected or queued for playback.
                if (input.Speaker == null && input.SpeakerAgentIndex >= 0)
                {
                    input.Speaker = Mission.Agents?.FirstOrDefault(agent =>
                        agent != null && agent.Index == input.SpeakerAgentIndex);
                }
                TryAcceptGeneratedNpcReply(input);
                return;
            }
            if (input.InputKind == BattleSpeechInputKindV1.DedicatedNpcSpeechRetry)
            {
                if (_active != null &&
                    _active.SessionId == input.SessionId &&
                    _active.State == BattleSpeechSessionStateV1.AwaitingNpcReply &&
                    _active.Speaker != null &&
                    _active.Speaker.Index == input.SpeakerAgentIndex &&
                    input.ConversationEpoch == _active.ConversationEpoch)
                {
                    StartDedicatedNpcSpeechGeneration(
                        _active,
                        input.RawText,
                        allowDiversityRetry: false);
                }
                return;
            }

            // Every newer player message invalidates an older asynchronous trigger result.
            // Otherwise a slow classifier could replace a session created by a later exact command.
            _triggerGeneration++;

            BattleSpeechTriggerDecisionV2 command = input.DedicatedNpcSpeechEntry
                ? BattleSpeechFrameworkV2.ParseDedicatedNpcSpeechInput()
                : input.DedicatedSpeechEntry
                    ? BattleSpeechFrameworkV2.ParseDedicatedSpeechInput(input.RawText)
                    : BattleSpeechFrameworkV2.ParsePlayerShout(input.RawText);
            if (input.DedicatedNpcSpeechEntry &&
                !AreNpcSpeechTargetsOnPlayerSide(
                    input.Player,
                    input.PrimaryTarget,
                    input.FramedTargets))
            {
                RejectSpeechStart(
                    "NpcSpeechTargetNotAllied",
                    SceneActionsText.BattleSpeechNpcTargetNotAllied());
                return;
            }
            if (command.Kind == BattleSpeechTriggerKindV2.None)
            {
                if (_active != null &&
                    _active.State == BattleSpeechSessionStateV1.AwaitingPlayerSpeech &&
                    ReferenceEquals(_active.Speaker, input.Player) &&
                    input.ConversationEpoch > _active.ConversationEpoch)
                {
                    PrepareSpeech(_active, input.RawText);
                }
                return;
            }
            if (command.Kind == BattleSpeechTriggerKindV2.NeedsClassifier)
            {
                StartTriggerClassification(input);
                return;
            }
            if (!string.IsNullOrWhiteSpace(command.Reason))
            {
                SceneActionsLog.Warning(
                    "BATTLE_SPEECH",
                    "Speech command rejected: " + command.Reason);
                return;
            }
            if (command.Kind == BattleSpeechTriggerKindV2.Cancel)
            {
                CancelActive("Cancelled by player.");
                return;
            }
            if (!TryResolvePhase(out BattleSpeechPhaseV1 phase))
            {
                RejectSpeechStart("UnsupportedPhase", SceneActionsText.BattleSpeechNotReady());
                return;
            }

            Agent player = input.Player ?? Mission.MainAgent ?? Agent.Main;
            if (command.Kind == BattleSpeechTriggerKindV2.ArmPlayerSpeech)
            {
                StartSession(
                    player,
                    BattleSpeechSpeakerKindV1.Player,
                    phase,
                    BattleSpeechSessionStateV1.AwaitingPlayerSpeech,
                    input.ConversationEpoch,
                    input.RawText,
                    BattleSpeechRuntimeHost.Settings.PlayerCaptureSeconds);
                return;
            }
            if (command.Kind == BattleSpeechTriggerKindV2.DeliverPlayerSpeech)
            {
                ActiveBattleSpeechSessionV1 session = StartSession(
                    player,
                    BattleSpeechSpeakerKindV1.Player,
                    phase,
                    BattleSpeechSessionStateV1.AwaitingPlayerSpeech,
                    input.ConversationEpoch,
                    input.RawText,
                    BattleSpeechRuntimeHost.Settings.PlayerCaptureSeconds);
                if (session != null) PrepareSpeech(session, command.SpeechText);
                return;
            }
            if (command.Kind == BattleSpeechTriggerKindV2.RequestNpcSpeech)
            {
                Agent speaker = input.PrimaryTarget;
                ActiveBattleSpeechSessionV1 session = StartSession(
                    speaker,
                    BattleSpeechSpeakerKindV1.Npc,
                    phase,
                    BattleSpeechSessionStateV1.AwaitingNpcReply,
                    input.ConversationEpoch,
                    input.RawText,
                    BattleSpeechRuntimeHost.Settings.NpcReplySeconds,
                    input.DedicatedSpeechEntry);
                // An explicit NPC command freezes the topic/request, not the final
                // wording. AF must generate one fresh troop-facing speech body;
                // the reply claim below captures that one response and prevents
                // the ordinary SceneActions path from playing it a second time.
                if (session != null && input.DedicatedSpeechEntry)
                {
                    StartDedicatedNpcSpeechGeneration(
                        session,
                        command.SpeechText,
                        allowDiversityRetry: true);
                }
            }
        }

        private ActiveBattleSpeechSessionV1 StartSession(
            Agent speaker,
            BattleSpeechSpeakerKindV1 speakerKind,
            BattleSpeechPhaseV1 phase,
            BattleSpeechSessionStateV1 state,
            int conversationEpoch,
            string requestText,
            float timeoutSeconds,
            bool combinedNpcRequest = false)
        {
            if (!BattleSpeechRuntimeHost.RefreshMcmOverrides(out string refreshError) ||
                !BattleSpeechRuntimeHost.IsSpeechEnabled)
            {
                SceneActionsLog.Warning(
                    "BATTLE_SPEECH_MCM",
                    "Speech request rejected after the per-session MCM refresh. " +
                    (refreshError ?? "Battle speech is disabled."));
                return null;
            }
            CancelActive("Replaced by a newer battle speech request.", false);
            if (!IsEligibleSpeaker(speaker))
            {
                RejectSpeechStart("SpeakerUnavailable", SceneActionsText.BattleSpeechSpeakerUnavailable());
                return null;
            }
            BattleSpeechEnemyProximityCache.Invalidate(Mission, speaker, null);
            bool speakerInCombat = IsSpeakerInCombatAction(speaker);
            BattleSpeechBattlefieldFactsV1 battlefieldFacts = speakerKind ==
                                                               BattleSpeechSpeakerKindV1.Npc
                ? CaptureBattlefieldFacts(speaker, phase, speakerInCombat)
                : null;
            bool combatSpeechMode = speakerInCombat ||
                                     BattleSpeechEnemyProximityCache.IsCombatStarted(Mission) ||
                                     (battlefieldFacts?.EnemyNearby ??
                                      HasNearbyEnemyThrottled(speaker));
            if (combatSpeechMode &&
                BattleSpeechEnemyProximityCache.MarkCombatStarted(Mission))
            {
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_STAGE",
                    "Mission combat mode latched when the speech session started.");
            }

            Agent[] audience = FreezeAudience(speaker);
            if (audience.Length < BattleSpeechRuntimeHost.Settings.MinimumAudience)
            {
                Notify(SceneActionsText.BattleSpeechNoAudience(), Colors.Yellow);
                return null;
            }

            double now = Mission.CurrentTime;
            _active = new ActiveBattleSpeechSessionV1
            {
                SessionId = Guid.NewGuid(),
                State = state,
                SpeakerKind = speakerKind,
                Phase = phase,
                Speaker = speaker,
                Audience = audience,
                SpeakerAgentIndex = speaker.Index,
                ConversationEpoch = conversationEpoch,
                RequestText = requestText ?? string.Empty,
                Generation = ++_nextSessionGeneration,
                RequestedAtMissionTime = now,
                ExpiresAtMissionTime = now + timeoutSeconds,
                CombatSpeechMode = combatSpeechMode
            };
            BattleSpeechRuntimeHost.TryCancelPerformanceForNewSpeechRequest(
                Mission,
                _active.SessionId);
            InitializeV2Stage(_active);
            if (speakerKind == BattleSpeechSpeakerKindV1.Npc)
            {
                BattleSpeechRuntimeHost.PublishNpcReplyClaim(
                    _active.SessionId,
                    Mission,
                    speaker,
                    conversationEpoch,
                    _active.RequestText,
                    _active.ExpiresAtMissionTime,
                    combinedNpcRequest,
                    combinedNpcRequest
                        ? BattleSpeechFrameworkV2.ResolveAudienceReplyCount(
                            BattleSpeechRuntimeHost.StageSettings.AudienceRepliesEnabled,
                            BattleSpeechRuntimeHost.StageSettings.AudienceReplyCount,
                            audience.Length)
                        : 0,
                    battlefieldFacts,
                    combatSpeechMode: combatSpeechMode);
            }
            SceneActionsLog.Info(
                "BATTLE_SPEECH",
                "Session=" + _active.SessionId.ToString("N") +
                " State=" + state +
                " Speaker=" + speaker.Index +
                " Audience=" + audience.Length +
                " Phase=" + phase +
                (battlefieldFacts == null
                    ? string.Empty
                    : " FactsFriendly=" + battlefieldFacts.FriendlyActiveHumanCount +
                      " FactsEnemy=" + battlefieldFacts.EnemyActiveHumanCount +
                      " FactsFriendlyRemoved=" + battlefieldFacts.FriendlyRemovedSinceBaseline +
                      " FactsEnemyRemoved=" + battlefieldFacts.EnemyRemovedSinceBaseline +
                      " EnemyNearby=" + battlefieldFacts.EnemyNearby));
            return _active;
        }

        private void TryCompleteNpcSpeech(BattleSpeechCapturedInputV1 input)
        {
            if (_active == null ||
                _active.State != BattleSpeechSessionStateV1.AwaitingNpcReply ||
                !ReferenceEquals(_active.Speaker, input.Speaker) ||
                input.Speaker == null ||
                input.Speaker.Index != _active.SpeakerAgentIndex ||
                input.ConversationEpoch != _active.ConversationEpoch ||
                string.IsNullOrWhiteSpace(_active.PendingNpcReplyText) ||
                !BattleSpeechReplyBindingV1.IsFresh(
                    input.SubmittedAtMissionTime,
                    _active.PendingNpcReplyAtMissionTime) ||
                !BattleSpeechReplyBindingV1.ReplyMatches(
                    _active.PendingNpcReplyText,
                    input.RawText))
            {
                return;
            }
            _active.PendingNpcReplyText = null;
            BeginSpeaking(_active, input.RawText);
        }

        private void TryAcceptNpcReplyCandidate(BattleSpeechCapturedInputV1 input)
        {
            if (_active == null ||
                _active.State != BattleSpeechSessionStateV1.AwaitingNpcReply ||
                !ReferenceEquals(_active.Speaker, input.Speaker) ||
                input.Speaker == null ||
                input.Speaker.Index != _active.SpeakerAgentIndex ||
                input.ConversationEpoch != _active.ConversationEpoch ||
                !BattleSpeechReplyBindingV1.RequestMatches(
                    _active.RequestText,
                    input.PlayerDirectedActionText) ||
                string.IsNullOrWhiteSpace(input.PlayerDirectedNpcReplyText) ||
                !BattleSpeechReplyBindingV1.ReplyMatches(
                    input.PlayerDirectedNpcReplyText,
                    input.RawText) ||
                !BattleSpeechReplyBindingV1.IsFresh(
                    input.SubmittedAtMissionTime,
                    _active.RequestedAtMissionTime))
            {
                return;
            }
            if (!string.IsNullOrWhiteSpace(_active.PendingNpcReplyText))
            {
                return;
            }
            _active.PendingNpcReplyText = input.PlayerDirectedNpcReplyText.Trim();
            _active.PendingNpcReplyAtMissionTime = input.SubmittedAtMissionTime;
            PrepareSpeechPlan(_active, _active.PendingNpcReplyText);
            SceneActionsLog.Info(
                "BATTLE_SPEECH",
                "Session=" + _active.SessionId.ToString("N") +
                " NPC reply candidate bound Agent=" + input.Speaker.Index +
                " Epoch=" + input.ConversationEpoch);
        }

        private void TryAcceptGeneratedNpcReply(BattleSpeechCapturedInputV1 input)
        {
            if (_active == null ||
                _active.State != BattleSpeechSessionStateV1.AwaitingNpcReply ||
                input.SessionId != _active.SessionId ||
                !ReferenceEquals(_active.Speaker, input.Speaker) ||
                input.Speaker == null ||
                input.Speaker.Index != _active.SpeakerAgentIndex ||
                input.ConversationEpoch != _active.ConversationEpoch ||
                string.IsNullOrWhiteSpace(input.RawText) ||
                input.AfBehavior == null ||
                input.AfNpcPacket == null ||
                !BattleSpeechReplyBindingV1.IsFresh(
                    input.SubmittedAtMissionTime,
                    _active.RequestedAtMissionTime))
            {
                return;
            }
            if (!string.IsNullOrWhiteSpace(_active.PendingNpcReplyText))
            {
                return;
            }
            _active.PendingNpcReplyText = input.RawText.Trim();
            _active.PendingNpcReplyAtMissionTime = input.SubmittedAtMissionTime;
            if (!BattleSpeechRuntimeHost.QueueGeneratedNpcReply(
                    _active.SessionId,
                    Mission,
                    input.Speaker,
                    _active.PendingNpcReplyText,
                    input.AfBehavior,
                    input.AfNpcPacket))
            {
                _active.PendingNpcReplyText = null;
                return;
            }
            if (input.CombinedResponse != null)
            {
                _active.ActionProgram = input.CombinedResponse.Plan.ActionProgram;
                _active.Tactic = input.CombinedResponse.Plan.Tactic;
                _active.AudienceReplies = input.CombinedResponse.Plan.AudienceReplies
                    .ToArray();
                _active.TacticDecisionProvided = true;
                _active.PlanClassificationPending = false;
                _active.PlanClassificationCompleted = true;
            }
            else
            {
                PrepareSpeechPlan(_active, _active.PendingNpcReplyText);
            }
            // The deferred AF replay only releases the visual/TTS payload. Keep
            // the same normalized body on the speech session so the next Mission
            // tick can transition AwaitingNpcReply -> Speaking exactly once.
            _active.PendingSpeechText = _active.PendingNpcReplyText;
            SceneActionsLog.Info(
                "BATTLE_SPEECH_INPUT",
                "Dedicated NPC speech body bound. Session=" +
                _active.SessionId.ToString("N") +
                " Agent=" + input.Speaker.Index +
                " Length=" + _active.PendingNpcReplyText.Length);
        }

        private void BeginSpeaking(ActiveBattleSpeechSessionV1 session, string rawText)
        {
            string speech = (rawText ?? string.Empty).Trim();
            if (ReferenceEquals(session, _active) && session.PlanClassificationPending)
            {
                session.PendingSpeechText = speech;
                return;
            }
            if (!ReferenceEquals(session, _active) ||
                !IsEligibleSpeaker(session.Speaker) ||
                speech.Length == 0 ||
                speech.Length > BattleSpeechRuntimeHost.Settings.MaxSpeechChars)
            {
                CancelActive("Speech text or speaker is invalid.");
                return;
            }
            if (!IsActiveSpeechPhaseOpen())
            {
                CancelActive("Battle speech phase changed.");
                return;
            }

            double now = Mission.CurrentTime;
            session.State = BattleSpeechSessionStateV1.Speaking;
            session.SpeechText = speech;
            session.StartedAtMissionTime = now;
            session.EndsAtMissionTime = now +
                BattleSpeechFrameworkV1.EstimateDurationSeconds(
                    speech,
                    BattleSpeechRuntimeHost.Settings);
            BattleSpeechSessionSnapshotV1 snapshot = Snapshot(session);
            BattleSpeechApiV1.PublishStarted(snapshot, RuntimeContext(session, snapshot));
            if (!session.CombatSpeechMode)
            {
                Notify(
                    SceneActionsText.BattleSpeechStarted(
                        session.Speaker.Name,
                        session.Audience.Length),
                    Colors.Green);
            }
            SceneActionsLog.Info(
                "BATTLE_SPEECH",
                "Session=" + session.SessionId.ToString("N") +
                " State=Speaking Duration=" +
                (session.EndsAtMissionTime - now).ToString("F2") +
                " TextLength=" + speech.Length);
        }

        private void ProgressSession()
        {
            if (_active == null) return;
            double now = Mission.CurrentTime;
            if (!IsEligibleSpeaker(_active.Speaker))
            {
                CancelActive("Speaker became unavailable.");
                return;
            }
            if (!_active.CombatSpeechMode &&
                (BattleSpeechEnemyProximityCache.IsCombatStarted(Mission) ||
                 IsSpeakerInCombatAction(_active.Speaker) ||
                 HasNearbyEnemyThrottled(_active.Speaker)))
            {
                BattleSpeechEnemyProximityCache.MarkCombatStarted(Mission);
                EnterCombatSpeechMode(_active);
            }
            if (!IsActiveSpeechPhaseOpen())
            {
                CancelActive("Battle speech phase closed.");
                return;
            }
            if (!ProgressV2Stage(_active, now))
            {
                return;
            }
            if (_active.State == BattleSpeechSessionStateV1.Speaking)
            {
                if (now >= _active.EndsAtMissionTime)
                {
                    CompleteActive();
                }
                return;
            }
            if (now >= _active.ExpiresAtMissionTime)
            {
                CancelActive("Battle speech request timed out.");
            }
        }

        private bool TryResolvePhase(out BattleSpeechPhaseV1 phase)
        {
            phase = BattleSpeechPhaseV1.Deployment;
            BattleSpeechSettingsV1 settings = BattleSpeechRuntimeHost.Settings;
            if (!IsSupportedBattleMission()) return false;

            DeploymentMissionController deployment =
                Mission.GetMissionBehavior<DeploymentMissionController>();
            if (settings.AllowDeployment &&
                Mission.Mode == MissionMode.Deployment &&
                !Mission.IsDeploymentFinished &&
                deployment != null &&
                deployment.TeamSetupOver)
            {
                phase = BattleSpeechPhaseV1.Deployment;
                return true;
            }
            if (settings.AllowPreEngagement &&
                Mission.Mode == MissionMode.Battle &&
                (Mission.IsDeploymentFinished || deployment == null))
            {
                phase = BattleSpeechPhaseV1.PreEngagement;
                return true;
            }
            return false;
        }

        private bool IsSupportedBattleMission()
        {
            return Mission != null &&
                   Mission.CurrentState == Mission.State.Continuing &&
                   Mission.IsLoadingFinished &&
                   !Mission.MissionEnded &&
                   !Mission.MissionIsEnding &&
                   Mission.PlayerTeam != null &&
                   (Mission.IsFieldBattle || Mission.IsSiegeBattle || Mission.IsSallyOutBattle) &&
                   !Mission.IsNavalBattle;
        }

        private bool IsActiveSpeechPhaseOpen()
        {
            return IsSupportedBattleMission() &&
                   (Mission.Mode == MissionMode.Deployment || Mission.Mode == MissionMode.Battle);
        }

        private bool IsEligibleSpeaker(Agent speaker)
        {
            return speaker != null &&
                   ReferenceEquals(speaker.Mission, Mission) &&
                   speaker.IsActive() &&
                   speaker.IsHuman &&
                   speaker.Team != null &&
                   speaker.Team.IsValid &&
                   Mission.PlayerTeam != null &&
                   speaker.Team.Side == Mission.PlayerTeam.Side &&
                   !speaker.IsRunningAway &&
                   !speaker.IsRetreating() &&
                   !speaker.IsUsingGameObject;
        }

        private bool AreNpcSpeechTargetsOnPlayerSide(
            Agent player,
            Agent primaryTarget,
            IReadOnlyList<Agent> framedTargets)
        {
            // The Y menu freezes one actual primary speaker. Other framed
            // agents may include enemies, mounts, or stale selection entries;
            // they must not veto an otherwise valid allied primary target.
            return IsEligibleSpeaker(primaryTarget) &&
                   (player == null || !ReferenceEquals(primaryTarget, player));
        }

        private static bool IsSpeakerInCombatAction(Agent speaker)
        {
            if (speaker == null)
            {
                return true;
            }
            try
            {
                return speaker.IsDoingPassiveAttack ||
                       IsAttackAction(speaker.GetCurrentActionType(0)) ||
                       IsAttackAction(speaker.GetCurrentActionType(1));
            }
            catch
            {
                return true;
            }
        }

        private static bool IsAttackAction(Agent.ActionCodeType actionType)
        {
            switch (actionType)
            {
                case Agent.ActionCodeType.ReadyRanged:
                case Agent.ActionCodeType.ReleaseRanged:
                case Agent.ActionCodeType.ReleaseThrowing:
                case Agent.ActionCodeType.ReadyMelee:
                case Agent.ActionCodeType.ReleaseMelee:
                case Agent.ActionCodeType.Kick:
                case Agent.ActionCodeType.KickContinue:
                case Agent.ActionCodeType.KickHit:
                case Agent.ActionCodeType.WeaponBash:
                case Agent.ActionCodeType.HitObject:
                    return true;
                default:
                    return false;
            }
        }

        private Agent[] FreezeAudience(Agent speaker)
        {
            return Mission.Teams
                .Where(team => team != null && team.IsValid &&
                               (BattleSpeechRuntimeHost.StageSettings.IncludeAlliedAudience
                                   ? team.Side == Mission.PlayerTeam.Side
                                   : ReferenceEquals(team, Mission.PlayerTeam)))
                .SelectMany(team => team.ActiveAgents)
                .Where(agent => agent != null &&
                                !ReferenceEquals(agent, speaker) &&
                                !ReferenceEquals(agent, Mission.MainAgent) &&
                                !ReferenceEquals(agent, Agent.Main) &&
                                ReferenceEquals(agent.Mission, Mission) &&
                                agent.IsActive() && agent.IsHuman &&
                                agent.Team != null &&
                                agent.Team.IsValid &&
                                !agent.IsRunningAway &&
                                !agent.IsRetreating() &&
                                !agent.IsUsingGameObject)
                .Distinct()
                .OrderBy(agent => agent.Position.AsVec2.DistanceSquared(speaker.Position.AsVec2))
                .ThenBy(agent => agent.Index)
                .ToArray();
        }

        private BattleSpeechBattlefieldFactsV1 CaptureBattlefieldFacts(
            Agent speaker,
            BattleSpeechPhaseV1 phase,
            bool speakerInCombat)
        {
            EnsureBattlefieldBaseline();
            CountActiveBattlefieldAgents(
                speaker,
                out int friendlyActive,
                out int enemyActive,
                out bool enemyNearby,
                out string[] enemyFactionNames);

            if (!_battlefieldBaselineInitialized && friendlyActive + enemyActive > 0)
            {
                _battlefieldBaselineInitialized = true;
                _battlefieldBaselineFriendlyCount = friendlyActive;
                _battlefieldBaselineEnemyCount = enemyActive;
            }

            string battleType = Mission.IsSiegeBattle
                ? "攻城战"
                : Mission.IsSallyOutBattle
                    ? "出城战"
                    : Mission.IsFieldBattle
                        ? "野战"
                        : "战斗";
            string phaseText = phase == BattleSpeechPhaseV1.Deployment
                ? "部署阶段"
                : "战斗阶段";
            int friendlyRemoved = _battlefieldBaselineInitialized
                ? Math.Max(
                    _battlefieldFriendlyRemoved,
                    Math.Max(0, _battlefieldBaselineFriendlyCount - friendlyActive))
                : -1;
            int enemyRemoved = _battlefieldBaselineInitialized
                ? Math.Max(
                    _battlefieldEnemyRemoved,
                    Math.Max(0, _battlefieldBaselineEnemyCount - enemyActive))
                : -1;
            string friendlyFactionName = ResolvePlayerPoliticalFactionName();
            string speakerFactionName = ResolveAgentPoliticalFactionName(speaker);
            if (string.IsNullOrWhiteSpace(speakerFactionName) &&
                speaker?.Team != null && Mission.PlayerTeam != null &&
                speaker.Team.Side == Mission.PlayerTeam.Side)
            {
                speakerFactionName = friendlyFactionName;
            }
            string speakerCultureName = ResolveAgentCultureName(speaker);
            return new BattleSpeechBattlefieldFactsV1(
                friendlyActive,
                enemyActive,
                friendlyRemoved,
                enemyRemoved,
                enemyNearby,
                speakerInCombat,
                battleType,
                phaseText,
                friendlyFactionName,
                speakerFactionName,
                speakerCultureName,
                enemyFactionNames);
        }

        private void EnsureBattlefieldBaseline()
        {
            if (_battlefieldBaselineInitialized || Mission?.PlayerTeam == null ||
                !Mission.IsLoadingFinished)
            {
                return;
            }
            CountActiveBattlefieldAgents(
                null,
                out int friendlyActive,
                out int enemyActive,
                out _,
                out _);
            if (friendlyActive + enemyActive <= 0)
            {
                return;
            }
            _battlefieldBaselineInitialized = true;
            _battlefieldBaselineFriendlyCount = friendlyActive;
            _battlefieldBaselineEnemyCount = enemyActive;
        }

        private void CountActiveBattlefieldAgents(
            Agent speaker,
            out int friendlyActive,
            out int enemyActive,
            out bool enemyNearby,
            out string[] enemyFactionNames)
        {
            friendlyActive = 0;
            enemyActive = 0;
            enemyNearby = false;
            HashSet<string> enemyFactions = new HashSet<string>(StringComparer.Ordinal);
            float radius = Math.Max(
                0f,
                BattleSpeechRuntimeHost.Settings?.EnemyInterruptRadiusMeters ?? 10f);
            float radiusSquared = radius * radius;
            bool canCheckNearby = !BattleSpeechEnemyProximityCache.IsCombatStarted(Mission) &&
                                  speaker != null && speaker.IsActive();
            Vec2 speakerPosition = canCheckNearby
                ? speaker.Position.AsVec2
                : default(Vec2);

            foreach (Team team in Mission.Teams)
            {
                if (team == null || !team.IsValid)
                {
                    continue;
                }
                bool sameSide = Mission.PlayerTeam != null &&
                                team.Side == Mission.PlayerTeam.Side;
                foreach (Agent agent in team.ActiveAgents)
                {
                    if (agent == null || !agent.IsActive() || !agent.IsHuman ||
                        agent.Team == null || !agent.Team.IsValid)
                    {
                        continue;
                    }
                    if (sameSide)
                    {
                        friendlyActive++;
                    }
                    else
                    {
                        enemyActive++;
                        if (enemyFactions.Count < 4)
                        {
                            string enemyFactionName = ResolveAgentPoliticalFactionName(agent);
                            if (!string.IsNullOrWhiteSpace(enemyFactionName))
                            {
                                enemyFactions.Add(enemyFactionName);
                            }
                        }
                        if (canCheckNearby && !enemyNearby &&
                            agent.Position.AsVec2.DistanceSquared(speakerPosition) <=
                            radiusSquared)
                        {
                            enemyNearby = true;
                        }
                    }
                }
            }
            enemyFactionNames = enemyFactions
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static string ResolvePlayerPoliticalFactionName()
        {
            try
            {
                IFaction faction = Hero.MainHero?.Clan?.Kingdom ??
                                   MobileParty.MainParty?.MapFaction ??
                                   Hero.MainHero?.MapFaction ??
                                   Clan.PlayerClan?.Kingdom ??
                                   Clan.PlayerClan?.MapFaction;
                return ResolveFactionName(faction);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ResolveAgentPoliticalFactionName(Agent agent)
        {
            try
            {
                PartyBase party = agent?.Origin?.BattleCombatant as PartyBase;
                IFaction faction = party?.MapFaction ??
                                   party?.MobileParty?.MapFaction ??
                                   party?.Owner?.MapFaction;
                return ResolveFactionName(faction);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ResolveAgentCultureName(Agent agent)
        {
            try
            {
                CharacterObject character = agent?.Character as CharacterObject;
                return (character?.Culture?.Name?.ToString() ?? string.Empty)
                    .Replace("\r", " ")
                    .Replace("\n", " ")
                    .Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ResolveFactionName(IFaction faction)
        {
            try
            {
                string name = (faction?.Name?.ToString() ?? string.Empty)
                    .Replace("\r", " ")
                    .Replace("\n", " ")
                    .Trim();
                return string.IsNullOrWhiteSpace(name)
                    ? (faction?.StringId ?? string.Empty).Trim()
                    : name;
            }
            catch
            {
                return string.Empty;
            }
        }

        private void RecordBattlefieldRemoval(Agent affectedAgent)
        {
            if (!_battlefieldBaselineInitialized || affectedAgent == null ||
                Mission?.PlayerTeam == null || affectedAgent.Team == null ||
                !affectedAgent.Team.IsValid || !affectedAgent.IsHuman)
            {
                return;
            }
            if (affectedAgent.Team.Side == Mission.PlayerTeam.Side)
            {
                _battlefieldFriendlyRemoved++;
            }
            else
            {
                _battlefieldEnemyRemoved++;
            }
        }

        private bool HasNearbyEnemyThrottled(Agent speaker)
        {
            return BattleSpeechEnemyProximityCache.HasNearbyEnemy(
                Mission,
                speaker,
                BattleSpeechRuntimeHost.Settings.EnemyInterruptRadiusMeters,
                BattleSpeechRuntimeHost.Settings.EnemyScanIntervalSeconds);
        }

        private void CompleteActive()
        {
            ActiveBattleSpeechSessionV1 session = _active;
            if (session == null) return;
            session.State = BattleSpeechSessionStateV1.Completed;
            BattleSpeechSessionSnapshotV1 snapshot = Snapshot(session);
            BattleSpeechRuntimeContextV1 context = RuntimeContext(session, snapshot);
            _active = null;
            CleanupV2Session(session);
            BattleSpeechApiV1.PublishCompleted(snapshot, context);
            SceneActionsLog.Info(
                "BATTLE_SPEECH",
                "Session=" + session.SessionId.ToString("N") + " State=Completed");
        }

        private void CancelActive(string reason, bool notify = true)
        {
            ActiveBattleSpeechSessionV1 session = _active;
            if (session == null) return;
            session.State = BattleSpeechSessionStateV1.Cancelled;
            BattleSpeechSessionSnapshotV1 snapshot = Snapshot(session);
            BattleSpeechRuntimeContextV1 context = RuntimeContext(session, snapshot);
            _active = null;
            CleanupV2Session(session);
            BattleSpeechApiV1.PublishCancelled(snapshot, context, reason);
            if (notify) Notify(SceneActionsText.BattleSpeechCancelled(reason), Colors.Yellow);
            SceneActionsLog.Info(
                "BATTLE_SPEECH",
                "Session=" + session.SessionId.ToString("N") +
                " State=Cancelled Reason=" + reason);
        }

        private BattleSpeechSessionSnapshotV1 Snapshot(ActiveBattleSpeechSessionV1 session)
        {
            return new BattleSpeechSessionSnapshotV1(
                session.SessionId,
                session.State,
                session.SpeakerKind,
                session.Phase,
                session.Speaker?.Index ?? -1,
                session.Speaker?.Name ?? string.Empty,
                session.Audience
                    .Where(agent => agent != null)
                    .Select(agent => agent.Index)
                    .ToArray(),
                session.SpeechText,
                session.StartedAtMissionTime,
                session.EndsAtMissionTime);
        }

        private BattleSpeechRuntimeContextV1 RuntimeContext(
            ActiveBattleSpeechSessionV1 session,
            BattleSpeechSessionSnapshotV1 snapshot)
        {
            return new BattleSpeechRuntimeContextV1(
                snapshot,
                Mission,
                session.Speaker,
                session.Audience,
                session.ActionProgram,
                session.Tactic,
                session.AudienceReplies,
                session.TacticDecisionProvided,
                session.CombatSpeechMode);
        }

        private void Notify(TaleWorlds.Localization.TextObject text, Color color)
        {
            if (BattleSpeechRuntimeHost.Settings.ScreenNotifications)
            {
                InformationManager.DisplayMessage(new InformationMessage(text.ToString(), color));
            }
        }

        private void RejectSpeechStart(
            string reason,
            TaleWorlds.Localization.TextObject notification)
        {
            SceneActionsLog.Warning(
                "BATTLE_SPEECH",
                "State=Rejected Reason=" + reason);
            Notify(notification, Colors.Yellow);
        }

        private void Close(string reason)
        {
            if (_closed) return;
            CancelActive(reason, false);
            CloseV2Lifetime();
            while (_inbound.TryDequeue(out _)) { }
            _closed = true;
            BattleSpeechRuntimeHost.UnbindSession(this);
            BattleSpeechEnemyProximityCache.Reset(Mission);
            _battlefieldBaselineInitialized = false;
            _battlefieldBaselineFriendlyCount = -1;
            _battlefieldBaselineEnemyCount = -1;
            _battlefieldFriendlyRemoved = 0;
            _battlefieldEnemyRemoved = 0;
            BattleSpeechApiV1.Reset();
            SceneActionsLog.Info("BATTLE_SPEECH", "Mission framework session closed. " + reason);
        }

        /// <summary>
        /// Disables only the BattleSpeech work for the current Mission.  Unlike
        /// Close(), this keeps the behavior and its V2 lifetime token alive so a
        /// later MCM re-enable can accept a new speech request in the same Mission.
        /// </summary>
        internal void DisableFromHost(string reason)
        {
            if (_closed)
            {
                return;
            }
            bool transitioned = !_mcmDisabled;
            _mcmDisabled = true;
            CancelActive(reason ?? "BattleSpeech disabled.", notify: false);
            ResetV2RequestCancellation();
            _triggerGeneration++;
            while (_inbound.TryDequeue(out _))
            {
            }
            while (_triggerCompletions.TryDequeue(out _))
            {
            }
            while (_planCompletions.TryDequeue(out _))
            {
            }
            if (transitioned)
            {
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_MCM",
                    "BattleSpeech session disabled without closing Mission behavior. Reason=" +
                    (reason ?? string.Empty));
            }
        }

        private sealed class ActiveBattleSpeechSessionV1
        {
            public Guid SessionId;
            public BattleSpeechSessionStateV1 State;
            public BattleSpeechSpeakerKindV1 SpeakerKind;
            public BattleSpeechPhaseV1 Phase;
            public Agent Speaker;
            public Agent[] Audience;
            public int SpeakerAgentIndex;
            public int ConversationEpoch;
            public string RequestText;
            public long Generation;
            public string PendingNpcReplyText;
            public double PendingNpcReplyAtMissionTime;
            public string SpeechText;
            public double RequestedAtMissionTime;
            public double ExpiresAtMissionTime;
            public double StartedAtMissionTime;
            public double EndsAtMissionTime;
            public bool ScriptedMovementOwned;
            public bool SpeakerAiPauseCaptured;
            public bool SpeakerWasAiPaused;
            public bool SpeakerAiPauseChanged;
            public Vec3 MovementStartPosition;
            public Vec3 LastMovementProgressPosition;
            public double LastMovementProgressMissionTime;
            public Vec3 SpeechLineCenter;
            public double MovementDeadlineMissionTime;
            public double NextMovementReassertMissionTime;
            public int MovementReassertCount;
            public bool ReachedSpeechLine;
            public bool DeferredReplayRequested;
            public bool PlanClassificationPending;
            public bool PlanClassificationCompleted;
            public double PlanClassificationPlaybackDeadlineMissionTime;
            public string PendingSpeechText;
            public ActionProgramV4 ActionProgram;
            public BattleSpeechTacticV2 Tactic;
            public bool TacticDecisionProvided;
            public bool CombatSpeechMode;
            public string[] AudienceReplies = Array.Empty<string>();
            public System.Threading.CancellationTokenSource ClassificationCancellation;
        }
    }
}
