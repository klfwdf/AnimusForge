using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SceneActions.Core;
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
        private long _nextSessionGeneration;
        private double _nextEnemyScanAtMissionTime;
        private bool _enemyScanDirty = true;
        private bool _cachedNearbyEnemy;

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
            _enemyScanDirty = true;
            _cachedNearbyEnemy = false;
            _nextEnemyScanAtMissionTime = 0d;
            BattleSpeechRuntimeHost.BindSession(this);
            SceneActionsLog.Info("BATTLE_SPEECH", "Mission framework session activated.");
        }

        internal bool TryEnqueue(BattleSpeechCapturedInputV1 input)
        {
            if (_closed || input == null || !ReferenceEquals(input.Mission, Mission))
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
            _enemyScanDirty = true;
            if (_active != null &&
                (ReferenceEquals(_active.Speaker, affectedAgent) ||
                 ReferenceEquals(_active.Speaker, affectorAgent)))
            {
                CancelActive("The speaker entered combat.");
            }
        }

        public override void OnAgentRemoved(
            Agent affectedAgent,
            Agent affectorAgent,
            AgentState agentState,
            KillingBlow blow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
            _enemyScanDirty = true;
            if (_active != null && ReferenceEquals(_active.Speaker, affectedAgent))
            {
                CancelActive("The speaker left the Mission.");
            }
        }

        public override void OnAgentTeamChanged(Team previousTeam, Team newTeam, Agent agent)
        {
            base.OnAgentTeamChanged(previousTeam, newTeam, agent);
            _enemyScanDirty = true;
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
                TryAcceptGeneratedNpcReply(input);
                return;
            }

            // Every newer player message invalidates an older asynchronous trigger result.
            // Otherwise a slow classifier could replace a session created by a later exact command.
            _triggerGeneration++;

            BattleSpeechTriggerDecisionV2 command = input.DedicatedSpeechEntry
                ? BattleSpeechFrameworkV2.ParseDedicatedSpeechInput(input.RawText)
                : BattleSpeechFrameworkV2.ParsePlayerShout(input.RawText);
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
                        input,
                        command.SpeechText);
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
                !BattleSpeechRuntimeHost.Settings.Enabled ||
                !BattleSpeechRuntimeHost.PerformanceSettings.Enabled)
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
            if (IsSpeakerInCombatAction(speaker))
            {
                RejectSpeechStart("SpeakerInCombat", SceneActionsText.BattleSpeechSpeakerInCombat());
                return null;
            }
            _enemyScanDirty = true;
            if (HasNearbyEnemyThrottled(speaker))
            {
                RejectSpeechStart("EnemyNearby", SceneActionsText.BattleSpeechEnemyNearby());
                return null;
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
                ExpiresAtMissionTime = now + timeoutSeconds
            };
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
                        : 0);
            }
            SceneActionsLog.Info(
                "BATTLE_SPEECH",
                "Session=" + _active.SessionId.ToString("N") +
                " State=" + state +
                " Speaker=" + speaker.Index +
                " Audience=" + audience.Length +
                " Phase=" + phase);
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
            Notify(
                SceneActionsText.BattleSpeechStarted(
                    session.Speaker.Name,
                    session.Audience.Length),
                Colors.Green);
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
            if (IsSpeakerInCombatAction(_active.Speaker))
            {
                CancelActive("The speaker entered combat.");
                return;
            }
            if (!IsActiveSpeechPhaseOpen())
            {
                CancelActive("Battle speech phase closed.");
                return;
            }
            if (HasNearbyEnemyThrottled(_active.Speaker))
            {
                CancelActive("Enemy entered the battle speech safety radius.");
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

        private bool HasNearbyEnemyThrottled(Agent speaker)
        {
            if (speaker == null)
            {
                return false;
            }
            double now = Mission?.CurrentTime ?? 0d;
            if (_enemyScanDirty || now >= _nextEnemyScanAtMissionTime)
            {
                _cachedNearbyEnemy = HasNearbyEnemy(speaker);
                _enemyScanDirty = false;
                _nextEnemyScanAtMissionTime = now +
                    BattleSpeechRuntimeHost.Settings.EnemyScanIntervalSeconds;
            }
            return _cachedNearbyEnemy;
        }

        private bool HasNearbyEnemy(Agent speaker)
        {
            float radiusSquared = BattleSpeechRuntimeHost.Settings.EnemyInterruptRadiusMeters *
                                  BattleSpeechRuntimeHost.Settings.EnemyInterruptRadiusMeters;
            foreach (Team team in Mission.Teams)
            {
                if (team == null || !team.IsValid || team.Side == speaker.Team.Side) continue;
                foreach (Agent enemy in team.ActiveAgents)
                {
                    if (enemy != null && enemy.Team != null && enemy.Team.IsValid &&
                        enemy.IsActive() && enemy.IsHuman &&
                        enemy.Position.AsVec2.DistanceSquared(speaker.Position.AsVec2) <= radiusSquared)
                    {
                        return true;
                    }
                }
            }
            return false;
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
                session.TacticDecisionProvided);
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
            BattleSpeechApiV1.Reset();
            SceneActionsLog.Info("BATTLE_SPEECH", "Mission framework session closed. " + reason);
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
            public bool MountFacingUnavailable;
            public bool SpeakerAiPauseCaptured;
            public bool SpeakerWasAiPaused;
            public bool SpeakerAiPauseChanged;
            public Vec3 MovementStartPosition;
            public Vec3 LastMovementProgressPosition;
            public double LastMovementProgressMissionTime;
            public Vec3 SpeechLineCenter;
            public Vec2 SpeechLineDirection;
            public Vec2 AudienceFacingDirection;
            public double MovementDeadlineMissionTime;
            public double NextMovementReassertMissionTime;
            public int MovementReassertCount;
            public bool ReachedSpeechLine;
            public bool SpeechLineFacingAnchored;
            public double NextAudienceFacingRefreshMissionTime;
            public bool AudienceFacingRefreshFailed;
            public Vec2 LastAppliedAudienceFacingDirection;
            public bool HasAppliedAudienceFacing;
            public bool DeferredReplayRequested;
            public bool PlanClassificationPending;
            public bool PlanClassificationCompleted;
            public double PlanClassificationPlaybackDeadlineMissionTime;
            public string PendingSpeechText;
            public ActionProgramV4 ActionProgram;
            public BattleSpeechTacticV2 Tactic;
            public bool TacticDecisionProvided;
            public string[] AudienceReplies = Array.Empty<string>();
            public System.Threading.CancellationTokenSource ClassificationCancellation;
        }
    }
}
