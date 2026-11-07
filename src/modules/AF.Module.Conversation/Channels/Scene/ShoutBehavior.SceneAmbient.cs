using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;
using AnimusForge.XihaiAction;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

// Ambient scene interaction (default off). Runtime contract:
// - Probe: main thread, at most every 5 s from ShoutMissionBehavior.OnMissionTick. Settings,
//   idle, cooldown, budget and busy checks are allocation-free; the peace allowlist is re-read at
//   probe cadence (a siege may start mid-mission); Mission.Agents is scanned only after all cheap
//   checks pass (<=1 scan/5 s).
// - One session at a time. A player shout advances the conversation epoch, which every async
//   stage of the chat checks; the approach state machine is ticked only while a session exists.
// - Named and unnamed characters may take part; native usable seats stay occupied. Camp
//   action-only seated poses are released transactionally by its conversation owner.
// - No postprocess or action tags: chat cannot change game state. Spoken lines use shared
//   personal history; the confirmed witness note is separate from NPC dialogue (no model call).
public partial class ShoutBehavior
{
    private const float AmbientProbeIntervalSeconds = 5f;
    private const float AmbientApproachStopDistance = 2.5f;
    private const float AmbientApproachTimeoutSeconds = 15f;
    private const float AmbientPlayerReplyWindowSeconds = 10f;
    private const float AmbientOpeningTimeoutSeconds = 60f;
    private const float AmbientPresentationTimeoutSeconds = 120f;
    private const float AmbientPlaybackProbeIntervalSeconds = 0.1f;
    private const float AmbientNpcPairMaxDistance = 12f;
    private const float AmbientSeatedTalkDistance = 4f;     // two seated speakers stay put and talk only this close
    private const int AmbientMaxCandidates = 8;

    private enum AmbientStage { None, Approaching, Opening, Presenting, Talking, AwaitPlayer }

    private sealed class AmbientSession
    {
        internal AmbientStage Stage;
        internal bool TargetsPlayer;
        internal Agent Initiator;
        internal Agent Partner;                 // null when the initiator approaches the player
        internal Agent Mover;                   // agent that walks over; null when everyone stays seated
        internal Agent MoveTarget;
        internal float StageDeadline;
        internal long PendingDeadlineTimestamp;
        internal string Topic;
        internal int Epoch;
        internal bool MovementScripted;
        internal bool External;                 // field camp envoy; ignores the ambient MCM switch
        internal Mission Mission;
        internal long RuntimeGeneration;
        internal int HistorySessionId;
        internal FieldCampMissionLogic Camp;
        internal bool InitiatorAcquired;
        internal bool PartnerAcquired;
        internal bool InitiatorWasCampSeated;
        internal Vec3 ApproachStartPosition;
        internal bool ActuallyMoved;
        internal bool Cancelled;
        internal bool OpeningAudioCaptured;
        internal bool Presented;
        internal bool OpeningHasAttachedAudio;
        internal Action OnPresented;
        internal long OpeningRequestId;
        internal ConversationRequestLifetime OpeningLifetime;
        internal SceneSpeechPlaybackInfo OpeningPlayback;
        internal float PresentationObservedAt = -1f;
        internal float VisualCompleteAt = -1f;
        internal float NextPlaybackProbe;
        internal float TransferStartedAt;
        internal float NextTransferProbe;
        internal readonly HashSet<long> OpeningAudioRequestIds = new HashSet<long>();
        internal readonly HashSet<long> AudioBaselineRequestIds = new HashSet<long>();
    }

    private float _nextAmbientProbeMissionTime;
    private float _ambientIdleSinceMissionTime = -1f;
    private float _ambientCooldownUntilMissionTime;
    private int _ambientSessionsThisMission;
    private Mission _ambientMission;
    private AmbientSession _ambientSession;
    private AmbientSession _ambientCampTransfer;
    private readonly HashSet<int> _ambientSpeakingAgentIndices = new HashSet<int>();
    private readonly HashSet<int> _ambientApproachedHeroAgentIndices = new HashSet<int>();
    private readonly Dictionary<string, DateTime> _ambientPairCooldownUntil = new Dictionary<string, DateTime>(StringComparer.Ordinal);

    // Message feed label for ambient lines; ordinary lines keep their label unchanged.
    private string AmbientFeedLabel(int agentIndex, string name)
        => _ambientSpeakingAgentIndices.Count > 0 && _ambientSpeakingAgentIndices.Contains(agentIndex)
            ? ConversationUiLocalization.Text("AF_CONV_UI_Ambient_FeedLabel", "{NAME} (chat)", "NAME", name ?? "")
            : name;

    private void ScheduleSceneSpeechFeed(int agentIndex, string name, string content, SceneSpeechPlaybackInfo playbackInfo)
    {
        ObserveAmbientOpeningPlayback(agentIndex, playbackInfo);
        ScheduleNpcSpeechToMessageFeed(agentIndex, AmbientFeedLabel(agentIndex, name), content, playbackInfo);
    }

    // The existing output port calls this on the game thread for text and each actual TTS
    // segment. Keep the real playback DTO; never infer presentation from request admission.
    private void ObserveAmbientOpeningPlayback(int agentIndex, SceneSpeechPlaybackInfo playbackInfo)
    {
        AmbientSession session = _ambientSession;
        if (session == null || session.Initiator?.Index != agentIndex || playbackInfo == null
            || (session.Stage != AmbientStage.Opening && session.Stage != AmbientStage.Presenting)
            || !IsAmbientSessionCurrent(session)) return;
        session.OpeningPlayback = playbackInfo;
        RefreshAmbientOpeningAudio(session);
        float now = session.Mission.CurrentTime;
        session.PresentationObservedAt = now;
        session.VisualCompleteAt = now + Math.Max(0.75f, playbackInfo.VisualDurationSeconds);
    }

    // Ambient voice switch applies only while an ambient session owns the speech pipeline.
    private bool IsTtsPlaybackEnabledForSceneSpeech()
    {
        if (_ambientSession != null && _ambientSession.Stage == AmbientStage.Talking && _ambientSpeakingAgentIndices.Count > 0
            && DuelSettings.LoadedForSceneRuntime()?.AmbientSceneVoice == false)
        {
            return false;
        }
        return IsTtsPlaybackEnabledForShout();
    }

    internal void TickAmbientSceneInteraction()
    {
        Mission mission = Mission.Current;
        if (mission == null || mission.IsMissionEnding || Agent.Main == null || !Agent.Main.IsActive())
        {
            EndAmbientSession("mission_unavailable", applyCooldown: false);
            ReleaseAmbientCampTransfer(force: true);
            return;
        }
        if (!ReferenceEquals(_ambientMission, mission))
        {
            ResetAmbientForMission(mission);
        }
        float now = mission.CurrentTime;
        ReleaseAmbientCampTransfer();
        if (_ambientSession != null)
        {
            TickAmbientSession(now);
            return;
        }
        if (now < _nextAmbientProbeMissionTime)
        {
            return;
        }
        _nextAmbientProbeMissionTime = now + AmbientProbeIntervalSeconds;
        DuelSettings settings = DuelSettings.LoadedForSceneRuntime();
        if (settings == null || !settings.AmbientSceneEnabled)
        {
            return;
        }
        if (IsAmbientPlayerBusy())
        {
            _ambientIdleSinceMissionTime = now;
            return;
        }
        if (_ambientIdleSinceMissionTime < 0f) _ambientIdleSinceMissionTime = now;
        if (now - _ambientIdleSinceMissionTime < settings.AmbientSceneIdleSeconds * AmbientTimeScale()
            || now < _ambientCooldownUntilMissionTime
            || _ambientSessionsThisMission >= settings.AmbientSceneMaxPerScene
            || ProactiveNpcRequestBehavior.TryPeekPendingSceneOpening(out _, out _, out _)
            || !IsAmbientPeaceScene())
        {
            return;
        }
        List<AmbientCandidate> candidates = CollectAmbientCandidates(settings.AmbientScenePlayerRadius);
        if (candidates.Count == 0) return;
        bool preferNpcPair = MBRandom.RandomFloat * 100f < settings.AmbientSceneNpcToNpcWeight;
        bool started = preferNpcPair
            ? TryStartAmbientNpcPair(candidates, settings, now) || TryStartAmbientApproachPlayer(candidates, settings, now)
            : TryStartAmbientApproachPlayer(candidates, settings, now) || TryStartAmbientNpcPair(candidates, settings, now);
        if (started) _ambientSessionsThisMission++;
    }

    private void ResetAmbientForMission(Mission mission)
    {
        EndAmbientSession("mission_changed", applyCooldown: false);
        ReleaseAmbientCampTransfer(force: true);
        _ambientMission = mission;
        _ambientSessionsThisMission = 0;
        _ambientIdleSinceMissionTime = -1f;
        _ambientCooldownUntilMissionTime = 0f;
        _nextAmbientProbeMissionTime = mission.CurrentTime + AmbientProbeIntervalSeconds;
        _ambientApproachedHeroAgentIndices.Clear();
        _lastPresentedFieldCampEnvoy = null;
        _lastPresentedFieldCampEnvoyMission = null;
    }

    // A field camp is a crowd of idle soldiers: ambient idle wait and interval run at half the MCM value there.
    private static float AmbientTimeScale() => FieldCampMissionLogic.Current != null ? 0.5f : 1f;

    // Same peace allowlist as scene taunt conflicts (execution scenes excluded), minus prison/port/alley.
    private static bool IsAmbientPeaceScene()
    {
        Mission mission = Mission.Current;
        if (mission == null || mission.IsMissionEnding || ExecutionSceneConflictBridge.IsSceneControlled(mission)) return false;
        // The exact camp behavior is the allowlist. It has no settlement Location;
        // execution still wins above, and camp acquisition arbitrates duel/slaughter/crowd.
        if (FieldCampMissionLogic.Current != null) return true;
        if (IsAmbientBattleSettlementScene(mission)) return false;
        try
        {
            if (!SceneTauntMissionBehavior.TryCaptureScenePeaceConflictFacts(Settlement.CurrentSettlement, out ScenePeaceConflictContext facts)
                || !ScenePeaceConflictContextOwner.CanInitialize(in facts))
            {
                return false;
            }
            string location = facts.LocationId ?? "";
            return !string.Equals(location, "prison", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(location, "port", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(location, "alley", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    // Armed coup street/hall run on peace locations (center/lordshall) without native battle markers,
    // so the location allowlist alone admits them. One mission-behavior lookup per probe/session tick.
    private static bool IsAmbientBattleSettlementScene(Mission mission)
        => SettlementEntryTroopSelectionBehavior.TryGetArmedCoupSceneForExternal(mission, out _);

    private bool IsAmbientPlayerBusy()
        => _ambientCampTransfer != null || _isProcessingShout || _isWaitingForScenePostprocessGate || _shoutHotkeyChargeActive
            || ShoutTextInputPopup.IsOpen || AnimusForgeNativeConversationOverlay.IsOpen || IsScenePresentationActiveForExternal
            || Campaign.Current?.ConversationManager?.IsConversationInProgress == true
            || IsSceneRewriteBusy() || IsMultiNpcSceneConversationActive() || _activeInteractionSessions.Count > 0
            || _sceneConversation.HasActiveImmediateReactions || IsSpeechPipelineBusy() || SceneTauntMissionBehaviorActive();

    private static bool SceneTauntMissionBehaviorActive()
        => Mission.Current?.GetMissionBehavior<SceneTauntMissionBehavior>()?.IsConflictActive == true;

    private readonly struct AmbientCandidate
    {
        internal AmbientCandidate(Agent agent, Hero hero, float playerDistance) { Agent = agent; Hero = hero; PlayerDistance = playerDistance; }
        internal Agent Agent { get; }
        internal Hero Hero { get; }
        internal float PlayerDistance { get; }
    }

    // One bounded scan; keeps the nearest few eligible named characters.
    private List<AmbientCandidate> CollectAmbientCandidates(float radius)
    {
        var result = new List<AmbientCandidate>(AmbientMaxCandidates);
        var agents = Mission.Current?.Agents;
        Agent main = Agent.Main;
        if (agents == null || main == null) return result;
        Vec3 playerPosition = main.Position;
        float radiusSquared = radius * radius;
        FieldCampMissionLogic camp = FieldCampMissionLogic.Current;
        for (int i = 0; i < agents.Count; i++)
        {
            Agent agent = agents[i];
            if (agent == null || agent == main || !CanAgentParticipateInSceneSpeech(agent)) continue;
            float distanceSquared = agent.Position.DistanceSquared(playerPosition);
            if (distanceSquared > radiusSquared) continue;
            if (!(agent.Character is CharacterObject character)) continue;
            Hero hero = character.HeroObject;
            if (hero == Hero.MainHero || hero?.IsChild == true || hero?.IsPrisoner == true || (hero == null && IsAmbientUnnamedChild(agent, character))
                || IsAgentHostileToMainAgent(agent)
                || RichExecutions.Core.VengeanceIntegration.IsExecutedVictim(agent)
                || BattleSpeechRuntimeHost.IsClaimedSpeechSpeaker(agent)
                || _sceneMovement.IsAgentBusyWithSceneSummonErrand(agent.Index) || _sceneMovement.IsAgentBusyWithSceneGuideErrand(agent.Index)
                || _sceneMovement.IsAgentFollowingPlayerBySceneCommand(agent) || IsStaringAgentIndex(agent.Index)
                || camp?.IsCampAgentBusyForConversation(agent) == true)
            {
                continue;
            }
            result.Add(new AmbientCandidate(agent, hero, (float)Math.Sqrt(distanceSquared)));
        }
        result.Sort((a, b) => a.PlayerDistance.CompareTo(b.PlayerDistance));
        if (result.Count > AmbientMaxCandidates) result.RemoveRange(AmbientMaxCandidates, result.Count - AmbientMaxCandidates);
        return result;
    }

    private bool IsStaringAgentIndex(int index) => _j17SceneAttentionController.IsStaringAgentIndex(index);

    // Unnamed agents carry no Hero; a reported age in (0, 18) marks a child.
    private static bool IsAmbientUnnamedChild(Agent agent, CharacterObject character)
    {
        try
        {
            float age = agent.Age;
            if (age > 0f && age < 18f) return true;
            age = character.Age;
            return age > 0f && age < 18f;
        }
        catch
        {
            return true;
        }
    }

    private static string AmbientIdentityKey(AmbientCandidate candidate)
        => candidate.Hero != null ? "hero:" + candidate.Hero.StringId : "agent:" + candidate.Agent.Index;

    private static bool IsAgentUsingObject(Agent agent)
    {
        try { return FieldCampMissionLogic.Current?.IsCampAgentSeated(agent) == true
            || agent.IsUsingGameObject || agent.CurrentlyUsedGameObject != null; }
        catch { return true; }
    }

    // Relationship > same faction > named > distance. A standing participant walks over; two seated
    // participants talk in place only when already close.
    private bool TryStartAmbientNpcPair(List<AmbientCandidate> candidates, DuelSettings settings, float now)
    {
        if (candidates.Count < 2) return false;
        DateTime realNow = DateTime.UtcNow;
        int bestScore = int.MinValue;
        AmbientCandidate bestInitiator = default, bestPartner = default;
        for (int i = 0; i < candidates.Count; i++)
        {
            AmbientCandidate initiator = candidates[i];
            bool initiatorSeated = IsAgentUsingObject(initiator.Agent);
            for (int j = 0; j < candidates.Count; j++)
            {
                if (i == j) continue;
                AmbientCandidate partner = candidates[j];
                float pairDistance = initiator.Agent.Position.Distance(partner.Agent.Position);
                if (pairDistance > AmbientNpcPairMaxDistance) continue;
                bool partnerSeated = IsAgentUsingObject(partner.Agent);
                if (initiatorSeated && partnerSeated && pairDistance > AmbientSeatedTalkDistance) continue;
                // Only a standing initiator may open; a seated one would talk across the room.
                if (initiatorSeated && !partnerSeated) continue;
                if (_ambientPairCooldownUntil.TryGetValue(AmbientPairKey(initiator, partner), out DateTime until) && until > realNow) continue;
                bool bothNamed = initiator.Hero != null && partner.Hero != null;
                int relation = 0;
                if (bothNamed) { try { relation = initiator.Hero.GetRelation(partner.Hero); } catch { relation = 0; } }
                bool sameFaction = bothNamed && initiator.Hero.MapFaction != null && initiator.Hero.MapFaction == partner.Hero.MapFaction;
                int named = (initiator.Hero != null ? 15 : 0) + (partner.Hero != null ? 10 : 0);
                int score = Math.Abs(relation) * 4 + (sameFaction ? 40 : 0) + (bothNamed && IsAmbientKin(initiator.Hero, partner.Hero) ? 80 : 0) + named - (int)pairDistance;
                if (score > bestScore) { bestScore = score; bestInitiator = initiator; bestPartner = partner; }
            }
        }
        if (bestInitiator.Agent == null) return false;
        string topic = BuildAmbientTopic(bestInitiator.Hero, bestPartner.Hero, settings, aboutPlayer: false);
        if (_ambientPairCooldownUntil.Count > 64)
        {
            foreach (string expired in _ambientPairCooldownUntil.Where(pair => pair.Value <= realNow).Select(pair => pair.Key).ToList())
                _ambientPairCooldownUntil.Remove(expired);
        }
        // Native usable seats stay occupied. Camp action-only seats are released by acquisition.
        Agent mover = null, moveTarget = null;
        if (!IsAgentUsingObject(bestInitiator.Agent)) { mover = bestInitiator.Agent; moveTarget = bestPartner.Agent; }
        if (!BeginAmbientSession(bestInitiator.Agent, bestPartner.Agent, topic, now, targetsPlayer: false, mover, moveTarget)) return false;
        _ambientPairCooldownUntil[AmbientPairKey(bestInitiator, bestPartner)] = realNow.AddMinutes(settings.AmbientScenePairCooldownMinutes);
        return true;
    }

    private bool TryStartAmbientApproachPlayer(List<AmbientCandidate> candidates, DuelSettings settings, float now)
    {
        AmbientCandidate best = default;
        int bestScore = int.MinValue;
        for (int i = 0; i < candidates.Count; i++)
        {
            AmbientCandidate candidate = candidates[i];
            if (_ambientApproachedHeroAgentIndices.Contains(candidate.Agent.Index) || !IsCooldownReady(candidate.Agent)) continue;
            // Seated characters speak from their seat, so they must already be close.
            if (IsAgentUsingObject(candidate.Agent) && candidate.PlayerDistance > AmbientSeatedTalkDistance) continue;
            int relation = 0;
            bool sameFaction = false;
            if (candidate.Hero != null)
            {
                try { relation = (int)candidate.Hero.GetRelationWithPlayer(); } catch { relation = 0; }
                sameFaction = candidate.Hero.MapFaction != null && candidate.Hero.MapFaction == Hero.MainHero?.MapFaction;
            }
            // A non-zero relation stands in for "has history with the player" (no API-line-specific member).
            int score = (relation != 0 ? 60 : 0) + Math.Abs(relation) * 3 + (sameFaction ? 30 : 0) + (candidate.Hero != null ? 20 : 0) - (int)candidate.PlayerDistance;
            if (score > bestScore) { bestScore = score; best = candidate; }
        }
        if (best.Agent == null) return false;
        string topic = BuildAmbientTopic(best.Hero, null, settings, aboutPlayer: true);
        bool seated = IsAgentUsingObject(best.Agent);
        if (!BeginAmbientSession(best.Agent, null, topic, now, targetsPlayer: true, seated ? null : best.Agent, seated ? null : Agent.Main)) return false;
        _ambientApproachedHeroAgentIndices.Add(best.Agent.Index);
        return true;
    }

    private static string AmbientPairKey(AmbientCandidate a, AmbientCandidate b)
    {
        string x = AmbientIdentityKey(a), y = AmbientIdentityKey(b);
        return string.CompareOrdinal(x, y) <= 0 ? x + "|" + y : y + "|" + x;
    }

    private static bool IsAmbientKin(Hero a, Hero b)
    {
        try
        {
            return a.Spouse == b || a.Father == b || a.Mother == b || b.Father == a || b.Mother == a
                || (a.Clan != null && a.Clan == b.Clan) || a.Siblings.Contains(b);
        }
        catch
        {
            return false;
        }
    }

    // One topic seed per chat, drawn at random from the enabled seed kinds that apply.
    private static string BuildAmbientTopic(Hero speaker, Hero partner, DuelSettings settings, bool aboutPlayer)
    {
        var seeds = new List<string>(4);
        try
        {
            if (settings.AmbientSceneTopicRelation && speaker != null && partner != null)
            {
                int relation = speaker.GetRelation(partner);
                if (IsAmbientKin(speaker, partner)) seeds.Add(ConversationRuntimeTexts.Prompt("AF_CONV_Ambient_Topic_Kin"));
                else if (relation >= 20) seeds.Add(ConversationRuntimeTexts.Prompt("AF_CONV_Ambient_Topic_Friend"));
                else if (relation <= -20) seeds.Add(ConversationRuntimeTexts.Prompt("AF_CONV_Ambient_Topic_Rival"));
                else if (speaker.MapFaction != null && speaker.MapFaction == partner.MapFaction)
                    seeds.Add(ConversationRuntimeTexts.FormatPrompt("AF_CONV_Ambient_Topic_SameFaction", ("FACTION", speaker.MapFaction.Name?.ToString() ?? "")));
            }
            if (settings.AmbientSceneTopicWorld && speaker?.MapFaction is Kingdom kingdom)
            {
                string enemies = string.Join(", ", Kingdom.All.Where(k => k != null && k != kingdom && !k.IsEliminated && kingdom.IsAtWarWith(k))
                    .Take(2).Select(k => k.Name?.ToString() ?? ""));
                if (!string.IsNullOrWhiteSpace(enemies))
                    seeds.Add(ConversationRuntimeTexts.FormatPrompt("AF_CONV_Ambient_Topic_War", ("FACTION", kingdom.Name?.ToString() ?? ""), ("ENEMIES", enemies)));
            }
            Settlement settlement = Settlement.CurrentSettlement;
            if (settings.AmbientSceneTopicPlace && settlement != null)
                seeds.Add(ConversationRuntimeTexts.FormatPrompt("AF_CONV_Ambient_Topic_Place", ("PLACE", settlement.Name?.ToString() ?? ""),
                    ("OWNER", settlement.OwnerClan?.Name?.ToString() ?? "")));
            if (settings.AmbientSceneTopicPlayer && (aboutPlayer || MBRandom.RandomFloat < 0.25f))
                seeds.Add(ConversationRuntimeTexts.FormatPrompt("AF_CONV_Ambient_Topic_Player", ("PLAYER", GetPlayerDisplayNameForShout())));
        }
        catch
        {
            // Seeds are optional flavour; an unavailable campaign fact only narrows the choice.
        }
        return seeds.Count == 0 ? ConversationRuntimeTexts.Prompt("AF_CONV_Ambient_Topic_Open") : seeds[MBRandom.RandomInt(seeds.Count)];
    }

    private bool BeginAmbientSession(Agent initiator, Agent partner, string topic, float now, bool targetsPlayer, Agent mover, Agent moveTarget, bool external = false)
    {
        Mission mission = Mission.Current;
        if (_ambientSession != null || mission == null || mission.IsMissionEnding || !CanAgentParticipateInSceneSpeech(initiator)
            || (partner != null && !CanAgentParticipateInSceneSpeech(partner)) || IsAmbientPlayerBusy()
            || ExecutionSceneConflictBridge.IsSceneControlled(mission)) return false;
        var session = new AmbientSession
        {
            Stage = AmbientStage.Approaching,
            TargetsPlayer = targetsPlayer,
            Initiator = initiator,
            Partner = partner,
            Mover = mover,
            MoveTarget = moveTarget,
            Topic = topic,
            Epoch = _sceneConversationEpoch,
            Mission = mission,
            RuntimeGeneration = SaveRuntimeGuard.CaptureGeneration(),
            HistorySessionId = Volatile.Read(ref SceneConversationHistoryOwner.SessionId),
            Camp = FieldCampMissionLogic.Current,
            External = external,
            ApproachStartPosition = (mover ?? initiator).Position,
            StageDeadline = now + AmbientApproachTimeoutSeconds,
        };
        if (session.Camp != null)
        {
            // Acquisition prepares the navigator but retains ground sitting/sleeping cycles.
            // Both camp ground rests and native usable seats keep their never-force-stand rule.
            session.InitiatorWasCampSeated = session.Camp.IsCampAgentSeated(initiator);
            if (!session.Camp.TryAcquireSceneConversation(initiator)) return false;
            session.InitiatorAcquired = true;
            if (partner != null)
            {
                if (!session.Camp.TryAcquireSceneConversation(partner))
                {
                    session.Camp.ReleaseSceneConversation(initiator);
                    session.InitiatorAcquired = false;
                    return false;
                }
                session.PartnerAcquired = true;
            }
        }
        _ambientSession = session;
        Logger.Log("ShoutBehavior", "[Ambient] start initiator=" + initiator.Index + " partner=" + (partner?.Index ?? -1) + " player=" + targetsPlayer
            + " mover=" + (mover?.Index ?? -1) + " external=" + external + " campSeated=" + session.InitiatorWasCampSeated);
        return true;
    }

    private bool IsAmbientSessionCurrent(AmbientSession session)
        => session != null && !session.Cancelled && ReferenceEquals(_ambientSession, session)
            && ReferenceEquals(CurrentInstance, this) && ReferenceEquals(Mission.Current, session.Mission)
            && !session.Mission.IsMissionEnding && SaveRuntimeGuard.IsCurrentGeneration(session.RuntimeGeneration)
            && session.HistorySessionId == Volatile.Read(ref SceneConversationHistoryOwner.SessionId)
            && session.Epoch == _sceneConversationEpoch;

    private static bool HasAmbientCampOwnership(AmbientSession session)
        => session.Camp == null || (ReferenceEquals(FieldCampMissionLogic.Current, session.Camp)
            && (!session.InitiatorAcquired || session.Camp.IsSceneConversationHeld(session.Initiator))
            && (!session.PartnerAcquired || session.Camp.IsSceneConversationHeld(session.Partner)));

    private void TickAmbientSession(float now)
    {
        AmbientSession session = _ambientSession;
        DuelSettings settings = DuelSettings.LoadedForSceneRuntime();
        if (settings == null || (!settings.AmbientSceneEnabled && !session.External))
        {
            EndAmbientSession("disabled", applyCooldown: false);
            return;
        }
        if (session.Epoch != _sceneConversationEpoch)
        {
            // A player shout took over; cancel the old opening, but never restore native
            // autonomy over the relay. Camp release also rechecks its new owner.
            EndAmbientSession(session.Stage == AmbientStage.AwaitPlayer ? "player_answered" : "player_interrupt",
                applyCooldown: true, releaseAgents: false);
            return;
        }
        if (!IsAmbientSessionCurrent(session) || !HasAmbientCampOwnership(session))
        {
            EndAmbientSession("owner_lost", applyCooldown: true, releaseAgents: false);
            return;
        }
        if (ExecutionSceneConflictBridge.IsSceneControlled(session.Mission))
        {
            EndAmbientSession("ceremony", applyCooldown: true);
            return;
        }
        if (session.Camp == null && IsAmbientBattleSettlementScene(session.Mission))
        {
            EndAmbientSession("settlement_battle", applyCooldown: true);
            return;
        }
        Agent target = session.TargetsPlayer ? Agent.Main : session.Partner;
        if (!CanAgentParticipateInSceneSpeech(session.Initiator) || !CanAgentParticipateInSceneSpeech(target)
            || IsAgentHostileToMainAgent(session.Initiator) || !IsAmbientWithinAbortRange(settings, session)
            || Campaign.Current?.ConversationManager?.IsConversationInProgress == true || SceneTauntMissionBehaviorActive())
        {
            EndAmbientSession("participant_lost", applyCooldown: true,
                releaseAgents: session.Camp == null || Campaign.Current?.ConversationManager?.IsConversationInProgress != true);
            return;
        }
        switch (session.Stage)
        {
            case AmbientStage.Approaching:
                TickAmbientApproach(session, target, now, settings);
                break;
            case AmbientStage.Opening:
                MaintainAmbientOpeningHold(session);
                if (IsAmbientPendingExpired(session, now)) EndAmbientSession("opening_timeout", applyCooldown: true);
                break;
            case AmbientStage.Presenting:
                MaintainAmbientOpeningHold(session);
                TickAmbientOpeningPresentation(session, now);
                break;
            case AmbientStage.AwaitPlayer:
                MaintainAmbientOpeningHold(session);
                if (now >= session.StageDeadline) EndAmbientSession("player_ignored", applyCooldown: true);
                break;
        }
    }

    private bool IsAmbientWithinAbortRange(DuelSettings settings, AmbientSession session)
    {
        float limit = settings.AmbientSceneAbortRadius;
        Vec3 player = Agent.Main.Position;
        return session.Initiator.Position.Distance(player) <= limit
            && (session.Partner == null || session.Partner.Position.Distance(player) <= limit);
    }

    // The standing participant walks to the other; when everyone is seated nobody moves and the chat
    // starts in place (selection already required them to be close).
    private void TickAmbientApproach(AmbientSession session, Agent target, float now, DuelSettings settings)
    {
        Agent mover = session.Mover;
        Agent moveTarget = session.MoveTarget ?? target;
        if (mover != null && session.MovementScripted && mover.Position.DistanceSquared(session.ApproachStartPosition) >= 0.25f)
            session.ActuallyMoved = true;
        bool arrived = mover == null
            ? session.Initiator.Position.Distance(target.Position) <= AmbientSeatedTalkDistance
            : mover.Position.Distance(moveTarget.Position) <= AmbientApproachStopDistance;
        if (arrived)
        {
            StopAmbientApproachMovement(session);
            if (session.TargetsPlayer) StartAmbientPlayerOpening(session, now);
            else StartAmbientNpcChat(session, settings);
            return;
        }
        if (now >= session.StageDeadline)
        {
            EndAmbientSession("approach_timeout", applyCooldown: true);
            return;
        }
        if (!session.MovementScripted)
        {
            try
            {
                _sceneMovement.PrepareAgentForSceneSummonMovement(mover);
                if (session.Camp != null)
                {
                    // Camp acquisition already released its action pose. Do not let an old
                    // home frame win over the Agent target (never synthesize a coordinate target).
                    mover.DisableScriptedMovement();
                    mover.SetIsAIPaused(false);
                    mover.SetMaximumSpeedLimit(-1f, false);
                    if (mover.GetComponent<CampaignAgentComponent>()?.AgentNavigator?.GetBehaviorGroup<DailyBehaviorGroup>() == null)
                        throw new InvalidOperationException("Camp conversation navigator unavailable");
                }
                ScriptBehavior.AddAgentTarget(mover, moveTarget);
                _sceneMovement.TrackSceneSummonScriptedAgent(mover);
                session.MovementScripted = true;
                if (!session.TargetsPlayer)
                {
                    // Hold the waiting participant in place, facing the walker, without interrupting what it uses.
                    _j17SceneAttentionController.AddAgentToStareList(moveTarget, interruptCurrentUse: false);
                    _j17SceneAttentionController.ApplyRelaySpeechGaze(moveTarget, mover, null);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("ShoutBehavior", "[Ambient] approach failed: " + ex.Message);
                EndAmbientSession("approach_failed", applyCooldown: true);
            }
        }
    }

    private void StopAmbientApproachMovement(AmbientSession session)
    {
        if (!session.MovementScripted) return;
        session.MovementScripted = false;
        if (session.Mover != null)
        {
            _sceneMovement.ClearSceneSummonScriptedBehavior(session.Mover);
            if (session.Camp != null && session.Mover.IsActive() && !IsAgentUsingObject(session.Mover))
            {
                try
                {
                    session.Mover.DisableScriptedMovement();
                    session.Mover.ClearTargetFrame();
                }
                catch (Exception ex)
                {
                    Logger.Log("ShoutBehavior", "[Ambient] movement cleanup failed agent=" + session.Mover.Index + " error=" + ex.Message);
                }
            }
        }
    }

    private void StartAmbientPlayerOpening(AmbientSession session, float now)
    {
        session.Stage = AmbientStage.Opening;
        session.OpeningLifetime = new ConversationRequestLifetime();
        session.StageDeadline = now + AmbientOpeningTimeoutSeconds;
        session.PendingDeadlineTimestamp = Stopwatch.GetTimestamp() + (long)(AmbientOpeningTimeoutSeconds * Stopwatch.Frequency);
        _ambientSpeakingAgentIndices.Add(session.Initiator.Index);
        RefreshAmbientOpeningAudio(session, captureBaseline: true);
        string fact = BuildProactiveSceneOpeningFactText("", ConversationRuntimeTexts.FormatPrompt("AF_CONV_Ambient_OpenPlayer",
            ("SELF", session.Initiator.Name?.ToString() ?? ""), ("TOPIC", session.Topic)));
        // Stay in Opening until the queue confirms actual presentation. All callbacks run on
        // the game thread and are bound to this session, mission, save, history and epoch.
        bool started = _sceneConversation.TriggerImmediateSceneBehaviorReaction(fact, session.Initiator.Index, persistHeroPrivateHistory: false,
            suppressStare: false, postSpeechLeaveSeconds: AmbientPlayerReplyWindowSeconds, skipSceneFactRecord: true,
            canStillPublish: () => CanPublishAmbientOpening(session),
            onCompleted: presented => CompleteAmbientPlayerOpening(session, presented),
            ownerCancellationToken: session.OpeningLifetime.Token,
            onRequestRegistered: requestId => session.OpeningRequestId = requestId,
            onPresented: () => MarkAmbientOpeningPresented(session));
        ApplyInteractionGraceAndGroupCooldown(0f, ScenePassiveInteractionController.ACTIVE_CHAT_COOLDOWN, new[] { session.Initiator });
        if (!started && ReferenceEquals(_ambientSession, session)) EndAmbientSession("opening_failed", applyCooldown: true);
    }

    private bool CanPublishAmbientOpening(AmbientSession session)
    {
        if (!IsAmbientSessionCurrent(session) || !HasAmbientCampOwnership(session)
            || (session.Stage != AmbientStage.Opening && session.Stage != AmbientStage.Presenting)
            || IsAmbientPendingExpired(session, session.Mission.CurrentTime)
            || ExecutionSceneConflictBridge.IsSceneControlled(session.Mission)
            || Campaign.Current?.ConversationManager?.IsConversationInProgress == true
            || SceneTauntMissionBehaviorActive() || !CanAgentParticipateInSceneSpeech(session.Initiator)
            || !CanAgentParticipateInSceneSpeech(Agent.Main) || IsAgentHostileToMainAgent(session.Initiator)) return false;
        DuelSettings settings = DuelSettings.LoadedForSceneRuntime();
        return settings != null && (session.External || settings.AmbientSceneEnabled) && IsAmbientWithinAbortRange(settings, session);
    }

    private void CompleteAmbientPlayerOpening(AmbientSession session, bool presented)
    {
        if (!IsAmbientSessionCurrent(session)) return;
        session.OpeningRequestId = 0L;
        if (!presented || !CanPublishAmbientOpening(session))
        {
            EndAmbientSession("opening_failed", applyCooldown: true);
            return;
        }
        session.Stage = AmbientStage.Presenting;
        float now = session.Mission.CurrentTime;
        session.StageDeadline = now + AmbientPresentationTimeoutSeconds;
        session.PendingDeadlineTimestamp = Stopwatch.GetTimestamp() + (long)(AmbientPresentationTimeoutSeconds * Stopwatch.Frequency);
        RefreshAmbientOpeningAudio(session);
        session.OpeningAudioCaptured = true;
        if (!session.OpeningHasAttachedAudio)
            MarkAmbientOpeningPresented(session);
        // Text-only output is immediate and may bypass ScheduleFeed. Use its real DTO when
        // available; otherwise the authoritative queue completion proves text was displayed.
        if (session.PresentationObservedAt < 0f && !session.OpeningHasAttachedAudio)
        {
            session.PresentationObservedAt = now;
            string visible = GetLatestSceneNpcUtterance(session.Initiator.Index);
            session.VisualCompleteAt = now + Math.Max(0.75f, EstimateBubbleTypingDurationSeconds(visible ?? ""));
        }
        Logger.Log("ShoutBehavior", "[Ambient] opening_presenting agent=" + session.Initiator.Index + " moved=" + session.ActuallyMoved);
        TickAmbientOpeningPresentation(session, now);
    }

    private void MarkAmbientOpeningPresented(AmbientSession session)
    {
        if (session.Presented || !IsAmbientSessionCurrent(session)) return;
        session.Presented = true;
        if (session.OpeningPlayback != null)
            session.VisualCompleteAt = Math.Max(session.VisualCompleteAt,
                session.Mission.CurrentTime + Math.Max(0.75f, session.OpeningPlayback.VisualDurationSeconds));
        if (session.External)
        {
            _lastPresentedFieldCampEnvoy = session.Initiator;
            _lastPresentedFieldCampEnvoyMission = session.Mission;
            _lastPresentedFieldCampEnvoyGeneration = session.RuntimeGeneration;
        }
        Action onPresented = session.OnPresented;
        session.OnPresented = null;
        try { onPresented?.Invoke(); }
        catch (Exception ex) { Logger.Log("ShoutBehavior", "[Ambient] presented callback failed: " + ex.Message); }
        if (!IsAmbientSessionCurrent(session)) return;
        string key = session.ActuallyMoved ? "AF_CONV_UI_Ambient_Approach" : "AF_CONV_UI_Ambient_InPlace";
        string fallback = session.ActuallyMoved ? "{NAME} comes over to talk to you." : "{NAME} would like to talk to you.";
        InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text(key, fallback,
            "NAME", session.Initiator.Name?.ToString() ?? ""), new Color(0.75f, 0.75f, 0.75f)));
        Logger.Log("ShoutBehavior", "[Ambient] opening_shown agent=" + session.Initiator.Index);
    }

    // Only the active opening inspects audio ownership, at <=10 Hz. No agent/world scan,
    // reflection, new polling task or per-probe allocation; IDs are reused and request-scoped.
    private bool RefreshAmbientOpeningAudio(AmbientSession session, bool captureBaseline = false)
    {
        bool activeAudio = false;
        bool canDiscover = !session.OpeningAudioCaptured && session.Epoch == _sceneConversationEpoch
            && !ExecutionSceneConflictBridge.IsSceneControlled(session.Mission);
        lock (SceneAudio._requestLock)
        {
            foreach (var pair in SceneAudio._ttsPlaybackOwners)
            {
                var owner = pair.Value;
                TtsEngine.PlaybackRequest request = owner?.Request;
                if (request == null || request.IsCancellationRequested || !ReferenceEquals(owner.Mission, session.Mission)
                    || owner.RuntimeGeneration != session.RuntimeGeneration || owner.SceneSessionId != session.HistorySessionId
                    || owner.ConversationEpoch != session.Epoch
                    || (request.AgentIndex != session.Initiator.Index && request.AgentIndex != -1)) continue;
                if (captureBaseline) { session.AudioBaselineRequestIds.Add(pair.Key); continue; }
                if (session.AudioBaselineRequestIds.Contains(pair.Key)
                    || (!canDiscover && !session.OpeningAudioRequestIds.Contains(pair.Key))) continue;
                session.OpeningAudioRequestIds.Add(pair.Key);
                activeAudio = true;
                if (request.AgentIndex == session.Initiator.Index) session.OpeningHasAttachedAudio = true;
            }
        }
        return activeAudio || SceneAudio.IsSpeaking(session.Initiator.Index);
    }

    private static bool IsAmbientPendingExpired(AmbientSession session, float now)
        => now >= session.StageDeadline || (session.PendingDeadlineTimestamp > 0L
            && Stopwatch.GetTimestamp() >= session.PendingDeadlineTimestamp);

    private void TickAmbientOpeningPresentation(AmbientSession session, float now)
    {
        if (IsAmbientPendingExpired(session, now))
        {
            EndAmbientSession("presentation_timeout", applyCooldown: true);
            return;
        }
        if (now < session.NextPlaybackProbe) return;
        session.NextPlaybackProbe = now + AmbientPlaybackProbeIntervalSeconds;
        bool activeAudio = RefreshAmbientOpeningAudio(session);
        if (!session.Presented && session.OpeningHasAttachedAudio)
        {
            // PrepareSceneOutput runs before audio/bubble dispatch; only the actual playback
            // start flag proves an attached TTS line has begun. Failure retires the request
            // after publishing its text fallback, which is also a real presentation.
            bool started;
            lock (_ttsBubbleSyncLock) started = _ttsPlaybackStartedAgents.Contains(session.Initiator.Index);
            if (started || (!activeAudio && session.PresentationObservedAt >= 0f)) MarkAmbientOpeningPresented(session);
        }
        if (!IsAmbientSessionCurrent(session)) return;
        if (activeAudio) return;
        if (session.OpeningHasAttachedAudio && session.OpeningPlayback != null
            && session.OpeningPlayback.TtsAccepted && session.OpeningPlayback.WaitForPlaybackFinished)
        {
            // Finishing audio may reveal the last streamed subtitle (or text fallback)
            // in this very frame; honor that DTO's last observed duration as well.
            session.VisualCompleteAt = Math.Max(session.VisualCompleteAt, session.PresentationObservedAt
                + Math.Max(0.75f, session.OpeningPlayback.VisualDurationSeconds));
        }
        if (!session.Presented)
        {
            EndAmbientSession("opening_not_presented", applyCooldown: true);
            return;
        }
        if (now < session.VisualCompleteAt) return;
        session.Stage = AmbientStage.AwaitPlayer;
        session.StageDeadline = now + AmbientPlayerReplyWindowSeconds;
        Logger.Log("ShoutBehavior", "[Ambient] await_player agent=" + session.Initiator.Index + " deadline=" + session.StageDeadline);
    }

    private void MaintainAmbientOpeningHold(AmbientSession session)
    {
        // Suppress the generic interaction timer until our reply window starts/ends. It
        // otherwise releases the envoy while generation or synthesis is still pending.
        int index = session.Initiator.Index;
        if (!IsStaringAgentIndex(index))
            _j17SceneAttentionController.AddAgentToStareList(session.Initiator, interruptCurrentUse: false);
        _stopStaringTime = Math.Max(_stopStaringTime, session.Mission.CurrentTime + 1f);
        _pendingInteractionTimeoutArms.Remove(index);
        if (_activeInteractionSessions.TryGetValue(index, out var interaction) && interaction != null)
            interaction.TimeoutArmed = false;
    }

    private void StartAmbientNpcChat(AmbientSession session, DuelSettings settings)
    {
        session.Stage = AmbientStage.Talking;
        Agent initiator = session.Initiator, partner = session.Partner;
        NpcDataPacket first = ShoutUtils.ExtractNpcData(initiator);
        NpcDataPacket second = ShoutUtils.ExtractNpcData(partner);
        if (first == null || second == null)
        {
            EndAmbientSession("snapshot_failed", applyCooldown: true);
            return;
        }
        var speakers = new List<NpcDataPacket> { first, second };
        ApplySceneLocalDisambiguatedNames(speakers);
        var heroes = new Dictionary<int, Hero>();
        foreach (Agent agent in new[] { initiator, partner })
        {
            if (agent.Character is CharacterObject character && character.HeroObject != null) heroes[agent.Index] = character.HeroObject;
        }
        _ambientSpeakingAgentIndices.Clear();
        _ambientSpeakingAgentIndices.Add(initiator.Index);
        _ambientSpeakingAgentIndices.Add(partner.Index);
        // Seated speakers keep their seat: the stare hold only turns their gaze (see
        // SceneAttentionController.HoldAgentInPlaceUnlessSeated).
        _j17SceneAttentionController.AddAgentToStareList(initiator, interruptCurrentUse: false);
        _j17SceneAttentionController.AddAgentToStareList(partner, interruptCurrentUse: false);
        _j17SceneAttentionController.ApplyRelaySpeechGaze(initiator, partner, new[] { partner.Index });
        AmbientSession owner = session;
        string place = FieldCampMissionLogic.Current != null ? FieldCampText.MenuTitle().ToString()
            : Settlement.CurrentSettlement?.Name?.ToString() ?? "";
        float playerRadius = settings.AmbientScenePlayerRadius;
        var audience = new List<NpcDataPacket>(speakers);
        foreach (Agent listener in ShoutUtils.GetNearbyNPCAgents() ?? new List<Agent>())
        {
            if (!CanAgentParticipateInSceneSpeech(listener) || listener == initiator || listener == partner
                || listener.Position.DistanceSquared(initiator.Position) > playerRadius * playerRadius) continue;
            NpcDataPacket datum = ShoutUtils.ExtractNpcData(listener);
            if (datum != null) audience.Add(datum);
        }
        ApplySceneLocalDisambiguatedNames(audience);
        bool playerWitnessRecorded = false;
        var request = new SceneConversationSessionRuntime.AmbientChatRequest
        {
            Speakers = speakers,
            Audience = audience,
            Heroes = heroes,
            Topic = session.Topic,
            MaxLines = settings.AmbientSceneMaxLines,
            ConversationEpoch = session.Epoch,
            CanContinue = () => IsAmbientSessionCurrent(owner) && HasAmbientCampOwnership(owner) && Agent.Main != null
                && !ExecutionSceneConflictBridge.IsSceneControlled(owner.Mission)
                && IsAmbientWithinAbortRange(DuelSettings.LoadedForSceneRuntime() ?? settings, owner),
            RegisterAddressee = RegisterRelaySpeechAddressee,
            OnLinePresented = () =>
            {
                if (!playerWitnessRecorded && Agent.Main != null && initiator.Position.Distance(Agent.Main.Position) <= playerRadius)
                {
                    playerWitnessRecorded = true;
                    PersistAmbientWitness(speakers, heroes, place);
                }
            },
            OnFinished = result =>
            {
                if (ReferenceEquals(_ambientSession, owner))
                {
                    EndAmbientSession(result.Interrupted ? "interrupted" : "finished", applyCooldown: true, releaseAgents: !result.Interrupted);
                }
            },
        };
        _ = _sceneConversation.RunSceneGroupOnMainThreadAsync(() => _sceneConversation.RunAmbientChatAsync(request));
    }

    // Speech is already persisted per line. This note only retains the player-as-witness context.
    private static void PersistAmbientWitness(List<NpcDataPacket> speakers, Dictionary<int, Hero> heroes, string place)
    {
        int session = Volatile.Read(ref SceneConversationHistoryOwner.SessionId);
        foreach (NpcDataPacket speaker in speakers)
        {
            if (speaker == null || heroes == null || !heroes.TryGetValue(speaker.AgentIndex, out Hero hero) || hero == null) continue;
            if (!AfGcczShoutBridge.ShouldUsePersistentPersonalMemory(hero, hero.CharacterObject, speaker.AgentIndex)) continue;
            string others = string.Join(", ", speakers.Where(s => s != null && s.AgentIndex != speaker.AgentIndex).Select(GetSceneNpcHistoryNameForPrompt));
            string witness = ConversationRuntimeTexts.FormatPrompt("AF_CONV_Ambient_SummaryOverheard", ("PLACE", place), ("OTHERS", others),
                ("PLAYER", GetPlayerDisplayNameForShout()));
            MyBehavior.AppendExternalSceneDialogueHistory(hero, null, null, witness, session);
        }
    }

    private static void ReleaseAmbientCampParticipants(AmbientSession session)
    {
        try
        {
            if (session.InitiatorAcquired)
            {
                session.InitiatorAcquired = false;
                session.Camp.ReleaseSceneConversation(session.Initiator);
            }
        }
        finally
        {
            if (session.PartnerAcquired)
            {
                session.PartnerAcquired = false;
                session.Camp.ReleaseSceneConversation(session.Partner);
            }
        }
    }

    // At most two already-owned agents, checked at the camp upkeep cadence. This consumes
    // the existing attention/interaction release state; it never acquires a new agent or
    // polls a provider, and does not keep a cancelled opening alive.
    private void ReleaseAmbientCampTransfer(bool force = false)
    {
        AmbientSession transfer = _ambientCampTransfer;
        if (transfer == null) return;
        bool current = ReferenceEquals(Mission.Current, transfer.Mission) && !transfer.Mission.IsMissionEnding
            && SaveRuntimeGuard.IsCurrentGeneration(transfer.RuntimeGeneration)
            && transfer.HistorySessionId == Volatile.Read(ref SceneConversationHistoryOwner.SessionId)
            && ReferenceEquals(FieldCampMissionLogic.Current, transfer.Camp);
        if (!force && current)
        {
            float now = transfer.Mission.CurrentTime;
            if (now < transfer.NextTransferProbe) return;
            transfer.NextTransferProbe = now + 0.5f;
            if (_isProcessingShout || _isWaitingForScenePostprocessGate || IsSceneRewriteBusy()
                || IsSpeechPipelineBusy() || _sceneConversation.HasActiveImmediateReactions
                || ShoutTextInputPopup.IsOpen || IsScenePresentationActiveForExternal
                || Campaign.Current?.ConversationManager?.IsConversationInProgress == true) return;
            // Existing interactions are normally armed by actual speech completion. If a
            // failed new round left an unarmed orphan, arm the original idle timeout only
            // after all generation/playback owners are idle; never release a live response.
            if (now - transfer.TransferStartedAt >= AmbientOpeningTimeoutSeconds)
            {
                ArmTransferredAmbientOrphan(transfer.Initiator, now);
                ArmTransferredAmbientOrphan(transfer.Partner, now);
            }
            if ((transfer.InitiatorAcquired && IsSceneConversationAgentHeldForExternal(transfer.Initiator))
                || (transfer.PartnerAcquired && IsSceneConversationAgentHeldForExternal(transfer.Partner))) return;
        }
        _ambientCampTransfer = null;
        if (current) StopAmbientApproachMovement(transfer);
        ReleaseAmbientCampParticipants(transfer);
    }

    private void ArmTransferredAmbientOrphan(Agent agent, float now)
    {
        if (agent == null || _pendingInteractionTimeoutArms.ContainsKey(agent.Index)
            || !_activeInteractionSessions.TryGetValue(agent.Index, out var interaction)
            || interaction == null || interaction.TimeoutArmed) return;
        interaction.LastActivityTime = now;
        interaction.TimeoutArmed = true;
    }

    private void EndAmbientSession(string reason, bool applyCooldown, bool releaseAgents = true)
    {
        AmbientSession session = _ambientSession;
        _ambientSession = null;
        _ambientSpeakingAgentIndices.Clear();
        if (session == null)
        {
            if (string.Equals(reason, "mission_end", StringComparison.Ordinal)) ReleaseAmbientCampTransfer(force: true);
            return;
        }
        // Retire publication authority before invoking cancellation/completion callbacks.
        session.Cancelled = true;
        session.OnPresented = null;
        if (session.Stage == AmbientStage.Opening || session.Stage == AmbientStage.Presenting)
            RefreshAmbientOpeningAudio(session);
        session.OpeningLifetime?.Retire();
        if (session.OpeningRequestId > 0L) _sceneConversation.CancelImmediateSceneReaction(session.OpeningRequestId);
        if (session.OpeningAudioRequestIds.Count > 0)
            TtsEngine.Instance?.CancelRequests(session.OpeningAudioRequestIds, keepCurrent: false);
        bool sameMission = ReferenceEquals(Mission.Current, session.Mission);
        if (sameMission && releaseAgents) StopAmbientApproachMovement(session);
        if (releaseAgents && sameMission)
        {
            if (session.TargetsPlayer)
            {
                _pendingInteractionTimeoutArms.Remove(session.Initiator.Index);
                _activeInteractionSessions.Remove(session.Initiator.Index);
            }
            var indices = new List<int>(2);
            if (session.Initiator != null) indices.Add(session.Initiator.Index);
            if (session.Partner != null) indices.Add(session.Partner.Index);
            _j17SceneAttentionController.ReleaseSceneConversationAttention(indices, fullyRestoreAutonomy: session.Camp == null);
            ClearRelaySpeechPresentation();
        }
        bool transferToSceneOwner = !releaseAgents && sameMission && !session.Mission.IsMissionEnding
            && SaveRuntimeGuard.IsCurrentGeneration(session.RuntimeGeneration)
            && session.HistorySessionId == Volatile.Read(ref SceneConversationHistoryOwner.SessionId)
            && (session.Epoch != _sceneConversationEpoch || Campaign.Current?.ConversationManager?.IsConversationInProgress == true);
        if (session.Camp != null && transferToSceneOwner && (session.InitiatorAcquired || session.PartnerAcquired))
        {
            // Keep only the camp occupancy, never the cancelled opening's publication owner.
            // Existing relay/native attention and interaction releases decide when to return.
            _ambientCampTransfer = session;
            session.TransferStartedAt = session.Mission.CurrentTime;
            session.NextTransferProbe = session.TransferStartedAt;
        }
        else ReleaseAmbientCampParticipants(session);
        if (string.Equals(reason, "mission_end", StringComparison.Ordinal)) ReleaseAmbientCampTransfer(force: true);
        Mission mission = Mission.Current;
        if (applyCooldown && mission != null)
        {
            DuelSettings settings = DuelSettings.LoadedForSceneRuntime();
            _ambientCooldownUntilMissionTime = mission.CurrentTime + (settings?.AmbientSceneCooldownSeconds ?? 120f) * AmbientTimeScale();
            _ambientIdleSinceMissionTime = mission.CurrentTime;
        }
        Logger.Log("ShoutBehavior", "[Ambient] end reason=" + (reason ?? ""));
    }
}
