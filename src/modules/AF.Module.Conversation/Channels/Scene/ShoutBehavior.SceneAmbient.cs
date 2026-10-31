using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Modules;
using AnimusForge.XihaiAction;
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
// - Named and unnamed characters may take part; seated ones stay seated (the standing side walks
//   over, or a fully seated pair must already be close).
// - No postprocess or action tags: chat cannot change game state. Persistence is one local
//   summary line per named participant (no extra model call, no AFEF).
public partial class ShoutBehavior
{
    private const float AmbientProbeIntervalSeconds = 5f;
    private const float AmbientApproachStopDistance = 2.5f;
    private const float AmbientApproachTimeoutSeconds = 15f;
    private const float AmbientPlayerReplyWindowSeconds = 10f;
    private const float AmbientNpcPairMaxDistance = 12f;
    private const float AmbientSeatedTalkDistance = 4f;     // two seated speakers stay put and talk only this close
    private const int AmbientMaxCandidates = 8;

    private enum AmbientStage { None, Approaching, Talking, AwaitPlayer }

    private sealed class AmbientSession
    {
        internal AmbientStage Stage;
        internal bool TargetsPlayer;
        internal Agent Initiator;
        internal Agent Partner;                 // null when the initiator approaches the player
        internal Agent Mover;                   // agent that walks over; null when everyone stays seated
        internal Agent MoveTarget;
        internal float StageDeadline;
        internal string Topic;
        internal int Epoch;
        internal bool MovementScripted;
    }

    private float _nextAmbientProbeMissionTime;
    private float _ambientIdleSinceMissionTime = -1f;
    private float _ambientCooldownUntilMissionTime;
    private int _ambientSessionsThisMission;
    private Mission _ambientMission;
    private AmbientSession _ambientSession;
    private readonly HashSet<int> _ambientSpeakingAgentIndices = new HashSet<int>();
    private readonly HashSet<int> _ambientApproachedHeroAgentIndices = new HashSet<int>();
    private readonly Dictionary<string, DateTime> _ambientPairCooldownUntil = new Dictionary<string, DateTime>(StringComparer.Ordinal);

    // Message feed label for ambient lines; ordinary lines keep their label unchanged.
    private string AmbientFeedLabel(int agentIndex, string name)
        => _ambientSpeakingAgentIndices.Count > 0 && _ambientSpeakingAgentIndices.Contains(agentIndex)
            ? ConversationUiLocalization.Text("AF_CONV_UI_Ambient_FeedLabel", "{NAME} (chat)", "NAME", name ?? "")
            : name;

    private void ScheduleSceneSpeechFeed(int agentIndex, string name, string content, SceneSpeechPlaybackInfo playbackInfo)
        => ScheduleNpcSpeechToMessageFeed(agentIndex, AmbientFeedLabel(agentIndex, name), content, playbackInfo);

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
        if (mission == null || Agent.Main == null || !Agent.Main.IsActive())
        {
            return;
        }
        if (!ReferenceEquals(_ambientMission, mission))
        {
            ResetAmbientForMission(mission);
        }
        float now = mission.CurrentTime;
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
        if (now - _ambientIdleSinceMissionTime < settings.AmbientSceneIdleSeconds
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
        _ambientMission = mission;
        _ambientSessionsThisMission = 0;
        _ambientIdleSinceMissionTime = -1f;
        _ambientCooldownUntilMissionTime = 0f;
        _nextAmbientProbeMissionTime = mission.CurrentTime + AmbientProbeIntervalSeconds;
        _ambientApproachedHeroAgentIndices.Clear();
    }

    // Same peace allowlist as scene taunt conflicts (execution scenes excluded), minus prison/port/alley.
    private static bool IsAmbientPeaceScene()
    {
        if (ExecutionSceneConflictBridge.IsSceneControlled(Mission.Current)) return false;
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

    private bool IsAmbientPlayerBusy()
        => _isProcessingShout || _isWaitingForScenePostprocessGate || _shoutHotkeyChargeActive
            || ShoutTextInputPopup.IsOpen || AnimusForgeNativeConversationOverlay.IsOpen || IsScenePresentationActiveForExternal
            || Campaign.Current?.ConversationManager?.IsConversationInProgress == true
            || IsSceneRewriteBusy() || IsMultiNpcSceneConversationActive() || _activeInteractionSessions.Count > 0
            || _sceneConversation.HasActiveImmediateReactions || SceneTauntMissionBehaviorActive();

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
                || _sceneMovement.IsAgentFollowingPlayerBySceneCommand(agent) || IsStaringAgentIndex(agent.Index))
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
        try { return agent.IsUsingGameObject || agent.CurrentlyUsedGameObject != null; }
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
        _ambientPairCooldownUntil[AmbientPairKey(bestInitiator, bestPartner)] = realNow.AddMinutes(settings.AmbientScenePairCooldownMinutes);
        // The standing initiator walks to the partner; two seated speakers stay in place.
        Agent mover = null, moveTarget = null;
        if (!IsAgentUsingObject(bestInitiator.Agent)) { mover = bestInitiator.Agent; moveTarget = bestPartner.Agent; }
        BeginAmbientSession(bestInitiator.Agent, bestPartner.Agent, topic, now, targetsPlayer: false, mover, moveTarget);
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
        _ambientApproachedHeroAgentIndices.Add(best.Agent.Index);
        string topic = BuildAmbientTopic(best.Hero, null, settings, aboutPlayer: true);
        bool seated = IsAgentUsingObject(best.Agent);
        BeginAmbientSession(best.Agent, null, topic, now, targetsPlayer: true, seated ? null : best.Agent, seated ? null : Agent.Main);
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

    private void BeginAmbientSession(Agent initiator, Agent partner, string topic, float now, bool targetsPlayer, Agent mover, Agent moveTarget)
    {
        _ambientSession = new AmbientSession
        {
            Stage = AmbientStage.Approaching,
            TargetsPlayer = targetsPlayer,
            Initiator = initiator,
            Partner = partner,
            Mover = mover,
            MoveTarget = moveTarget,
            Topic = topic,
            Epoch = _sceneConversationEpoch,
            StageDeadline = now + AmbientApproachTimeoutSeconds,
        };
        Logger.Log("ShoutBehavior", "[Ambient] start initiator=" + initiator.Index + " partner=" + (partner?.Index ?? -1) + " player=" + targetsPlayer
            + " mover=" + (mover?.Index ?? -1));
    }

    private void TickAmbientSession(float now)
    {
        AmbientSession session = _ambientSession;
        DuelSettings settings = DuelSettings.LoadedForSceneRuntime();
        if (settings == null || !settings.AmbientSceneEnabled)
        {
            EndAmbientSession("disabled", applyCooldown: false);
            return;
        }
        if (session.Epoch != _sceneConversationEpoch && session.Stage != AmbientStage.AwaitPlayer)
        {
            // A player shout took over; the relay owns the participants now.
            EndAmbientSession("player_interrupt", applyCooldown: true, releaseAgents: false);
            return;
        }
        Agent target = session.TargetsPlayer ? Agent.Main : session.Partner;
        if (!CanAgentParticipateInSceneSpeech(session.Initiator) || !CanAgentParticipateInSceneSpeech(target)
            || IsAgentHostileToMainAgent(session.Initiator) || !IsAmbientWithinAbortRange(settings, session)
            || Campaign.Current?.ConversationManager?.IsConversationInProgress == true || SceneTauntMissionBehaviorActive())
        {
            EndAmbientSession("participant_lost", applyCooldown: true);
            return;
        }
        switch (session.Stage)
        {
            case AmbientStage.Approaching:
                TickAmbientApproach(session, target, now, settings);
                break;
            case AmbientStage.AwaitPlayer:
                if (session.Epoch != _sceneConversationEpoch || now >= session.StageDeadline)
                {
                    // The player answered (relay took over) or ignored the approach.
                    EndAmbientSession(session.Epoch != _sceneConversationEpoch ? "player_answered" : "player_ignored",
                        applyCooldown: true, releaseAgents: session.Epoch == _sceneConversationEpoch);
                }
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
        if (session.Mover != null) _sceneMovement.ClearSceneSummonScriptedBehavior(session.Mover);
    }

    private void StartAmbientPlayerOpening(AmbientSession session, float now)
    {
        session.Stage = AmbientStage.AwaitPlayer;
        session.StageDeadline = now + AmbientPlayerReplyWindowSeconds;
        InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Ambient_Approach",
            "{NAME} comes over to talk to you.", "NAME", session.Initiator.Name?.ToString() ?? ""), new Color(0.75f, 0.75f, 0.75f)));
        string fact = BuildProactiveSceneOpeningFactText("", ConversationRuntimeTexts.FormatPrompt("AF_CONV_Ambient_OpenPlayer",
            ("SELF", session.Initiator.Name?.ToString() ?? ""), ("TOPIC", session.Topic)));
        // The opening line reuses the immediate-reaction pipeline (NPC-initiated fact, player-directed).
        bool started = TriggerImmediateSceneBehaviorReaction(fact, session.Initiator.Index, persistHeroPrivateHistory: false,
            suppressStare: false, postSpeechLeaveSeconds: AmbientPlayerReplyWindowSeconds, skipSceneFactRecord: true);
        ApplyInteractionGraceAndGroupCooldown(0f, ScenePassiveInteractionController.ACTIVE_CHAT_COOLDOWN, new[] { session.Initiator });
        if (!started) EndAmbientSession("opening_failed", applyCooldown: true);
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
        string place = Settlement.CurrentSettlement?.Name?.ToString() ?? "";
        float playerRadius = settings.AmbientScenePlayerRadius;
        var request = new SceneConversationSessionRuntime.AmbientChatRequest
        {
            Speakers = speakers,
            Audience = speakers,
            Heroes = heroes,
            Topic = session.Topic,
            MaxLines = settings.AmbientSceneMaxLines,
            ConversationEpoch = session.Epoch,
            CanContinue = () => ReferenceEquals(_ambientSession, owner) && Agent.Main != null
                && IsAmbientWithinAbortRange(DuelSettings.LoadedForSceneRuntime() ?? settings, owner),
            RegisterAddressee = RegisterRelaySpeechAddressee,
            OnFinished = result =>
            {
                bool overheard = Agent.Main != null && result.Spoken.Count > 0
                    && initiator.Position.Distance(Agent.Main.Position) <= playerRadius;
                PersistAmbientSummaries(speakers, heroes, result, place, session.Topic, overheard);
                if (ReferenceEquals(_ambientSession, owner))
                {
                    EndAmbientSession(result.Interrupted ? "interrupted" : "finished", applyCooldown: true, releaseAgents: !result.Interrupted);
                }
            },
        };
        _ = _sceneConversation.RunSceneGroupOnMainThreadAsync(() => _sceneConversation.RunAmbientChatAsync(request));
    }

    // One local summary line per named participant; the overheard line records the player as witness.
    private static void PersistAmbientSummaries(List<NpcDataPacket> speakers, Dictionary<int, Hero> heroes,
        SceneConversationSessionRuntime.AmbientChatResult result, string place, string topic, bool overheard)
    {
        if (result == null || result.Spoken.Count == 0) return;
        string lastLine = result.Spoken[result.Spoken.Count - 1].Line;
        if (lastLine.Length > 80) lastLine = lastLine.Substring(0, 80) + "…";
        int session = System.Threading.Volatile.Read(ref SceneConversationHistoryOwner.SessionId);
        foreach (NpcDataPacket speaker in speakers)
        {
            if (speaker == null || heroes == null || !heroes.TryGetValue(speaker.AgentIndex, out Hero hero) || hero == null) continue;
            if (!AfGcczShoutBridge.ShouldUsePersistentPersonalMemory(hero, hero.CharacterObject, speaker.AgentIndex)) continue;
            string others = string.Join(", ", speakers.Where(s => s != null && s.AgentIndex != speaker.AgentIndex).Select(GetSceneNpcHistoryNameForPrompt));
            string summary = ConversationRuntimeTexts.FormatPrompt("AF_CONV_Ambient_SummaryNpc", ("PLACE", place), ("OTHERS", others), ("LINE", lastLine));
            if (overheard)
            {
                summary += " " + ConversationRuntimeTexts.FormatPrompt("AF_CONV_Ambient_SummaryOverheard", ("PLACE", place), ("OTHERS", others),
                    ("PLAYER", GetPlayerDisplayNameForShout()));
            }
            MyBehavior.AppendExternalSceneDialogueHistory(hero, null, summary, null, session);
        }
    }

    private void EndAmbientSession(string reason, bool applyCooldown, bool releaseAgents = true)
    {
        AmbientSession session = _ambientSession;
        _ambientSession = null;
        _ambientSpeakingAgentIndices.Clear();
        if (session == null) return;
        StopAmbientApproachMovement(session);
        if (releaseAgents)
        {
            var indices = new List<int>(2);
            if (session.Initiator != null) indices.Add(session.Initiator.Index);
            if (session.Partner != null) indices.Add(session.Partner.Index);
            _j17SceneAttentionController.ReleaseSceneConversationAttention(indices, fullyRestoreAutonomy: true);
            ClearRelaySpeechPresentation();
        }
        Mission mission = Mission.Current;
        if (applyCooldown && mission != null)
        {
            DuelSettings settings = DuelSettings.LoadedForSceneRuntime();
            _ambientCooldownUntilMissionTime = mission.CurrentTime + (settings?.AmbientSceneCooldownSeconds ?? 120f);
            _ambientIdleSinceMissionTime = mission.CurrentTime;
        }
        Logger.Log("ShoutBehavior", "[Ambient] end reason=" + (reason ?? ""));
    }
}
