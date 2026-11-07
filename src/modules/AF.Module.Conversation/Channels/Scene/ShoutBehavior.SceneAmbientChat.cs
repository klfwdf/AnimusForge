using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

using static AnimusForge.ShoutBehavior;

namespace AnimusForge;

// Ambient (player-idle) scene chat: NPC-to-NPC lines generated one speaker at a time.
// Runs only after the ShoutBehavior probe admitted the session on the main thread.
// Each line: shared prompt context -> main reply -> speech queue -> history. No
// postprocess, so no game-mechanism tag can execute; a player shout bumps the epoch and
// every stage below observes it and stops.
internal sealed partial class SceneConversationSessionRuntime
{
    internal sealed class AmbientChatRequest
    {
        internal List<NpcDataPacket> Speakers;      // index 0 opens; order of alternation
        internal List<NpcDataPacket> Audience;      // speakers plus nearby listeners
        internal Dictionary<int, Hero> Heroes;
        internal string Topic;
        internal int MaxLines;
        internal int ConversationEpoch;
        internal Func<bool> CanContinue;            // main-thread distance/combat check
        internal Action<int, int> RegisterAddressee;
        internal Action OnLinePresented;
        internal Action<AmbientChatResult> OnFinished;
    }

    internal sealed class AmbientChatResult
    {
        internal readonly List<(NpcDataPacket Speaker, string Line)> Spoken = new List<(NpcDataPacket, string)>();
        internal bool Interrupted;
    }

    internal async Task RunAmbientChatAsync(AmbientChatRequest request)
    {
        AmbientChatResult result = new AmbientChatResult();
        long generation = SaveRuntimeGuard.CaptureGeneration();
        int session = _ports.SceneSessionId();
        int epoch = request.ConversationEpoch;
        var lifetime = _sceneRequestLifetime;
        // Exact epoch match: IsSceneConversationEpochCurrent treats epoch 0 as always current,
        // but an ambient chat must stop on the first player shout of the scene too.
        bool Current() => SaveRuntimeGuard.IsCurrentGeneration(generation) && session == _ports.SceneSessionId()
            && epoch == Volatile.Read(ref _sceneConversationEpoch) && _ports.IsOwnerCurrent();
        try
        {
            using IDisposable worker = lifetime.Enter();
            using IDisposable cancellation = LlmNonStreamingTransport.PushOwnerCancellation(lifetime.Token);
            NpcDataPacket previous = null;
            string previousLine = "";
            int lines = Math.Max(2, request.MaxLines);
            for (int turn = 0; turn < lines && request.Speakers.Count >= 2; turn++)
            {
                NpcDataPacket speaker = request.Speakers[turn % request.Speakers.Count];
                NpcDataPacket addressee = previous ?? request.Speakers[1];
                bool alive = await _dispatcher.RunAsync("ambient_turn_validate", speaker.Name, speaker.AgentIndex,
                    () => Current() && request.CanContinue() && CanAgentParticipateInSceneSpeechExternal(speaker.AgentIndex)
                        && CanAgentParticipateInSceneSpeechExternal(addressee.AgentIndex), false);
                if (!alive) { result.Interrupted = !Current(); break; }
                string instruction = turn == 0
                    ? ConversationRuntimeTexts.FormatPrompt("AF_CONV_Ambient_OpenNpc", ("SELF", GetSceneNpcHistoryNameForPrompt(speaker)),
                        ("TARGET", GetSceneNpcHistoryNameForPrompt(addressee)), ("TOPIC", request.Topic))
                    : ConversationRuntimeTexts.FormatPrompt("AF_CONV_Ambient_ReplyNpc", ("SELF", GetSceneNpcHistoryNameForPrompt(speaker)),
                        ("PREV", GetSceneNpcHistoryNameForPrompt(previous)), ("LINE", previousLine));
                string line = await GenerateAmbientLineAsync(speaker, request, instruction, generation, session, epoch, Current);
                if (!Current()) { result.Interrupted = true; break; }
                if (string.IsNullOrWhiteSpace(line)) break;
                bool published = await PublishAmbientLineAsync(speaker, addressee, line, request, generation, session, epoch, Current);
                if (!published) { result.Interrupted = !Current(); break; }
                result.Spoken.Add((speaker, line));
                previous = speaker;
                previousLine = line;
            }
        }
        catch (OperationCanceledException)
        {
            result.Interrupted = true;
        }
        catch (Exception ex)
        {
            Logger.Log("ShoutBehavior", "[AmbientChat] failed: " + ex.Message);
        }
        finally
        {
            await _dispatcher.RunAsync("ambient_finish", "ambient", -1, () => { request.OnFinished?.Invoke(result); return true; }, false);
        }
    }

    // One NPC line through the shared prompt phases; prompt section order matches the
    // relay fallback layout so history, persona and knowledge stay consistent.
    private async Task<string> GenerateAmbientLineAsync(NpcDataPacket speaker, AmbientChatRequest request, string instruction,
        long generation, int session, int epoch, Func<bool> current)
    {
        Hero hero = null;
        request.Heroes?.TryGetValue(speaker.AgentIndex, out hero);
        await _ports.EnsurePersonaForCandidatesAsync(new List<NpcDataPacket> { speaker }, request.Heroes);
        if (!current()) return "";
        CharacterObject character = await _dispatcher.RunAsync("ambient_capture_character", speaker.Name, speaker.AgentIndex,
            () => FindAgentByIndex(speaker.AgentIndex)?.Character as CharacterObject, (CharacterObject)null);
        if (!current() || character == null) return "";
        string kingdomId = await _dispatcher.RunAsync("ambient_capture_kingdom", speaker.Name, speaker.AgentIndex,
            () => TryGetKingdomIdOverrideFromAgent(FindAgentByIndex(speaker.AgentIndex)), "");
        // Not player-initiated: no rule preprocess or routing request (empty input returns the empty context).
        MyBehavior.ShoutPromptContext ctx = await BuildModuleScenePromptContextAsync(hero, character, "", null,
            speaker.CultureId ?? "neutral", kingdomId, speaker.AgentIndex, speaker.IsHero, false, null, null, generation, session, epoch);
        if (!current() || ctx == null) return "";
        var memory = await _ports.AwaitPrecomputedPersistedHistoryContextAsync(speaker.AgentIndex, request.Topic, request.Heroes, null, null, "ambient_turn");
        if (!current()) return "";
        SplitPersistedHeroHistorySections(memory?.Context ?? "", out string privateRecent, out string persisted);
        string baseExtras = StripScenePersonaBlocks((ctx.Extras ?? "").Trim());
        string trust = ExtractTrustPromptBlock(baseExtras, out string withoutTrust);
        // Rule sections are dropped: ambient chat must not see action-authorizing rules.
        SplitSceneExtraSections(withoutTrust, out string misc, out _, out string knowledge);
        string present = _ports.BuildScenePresentNpcListBlockForPrompt(request.Audience, speaker, request.Heroes);
        string intro = BuildSceneStablePromptIntroForSingle(speaker, hero, request.Audience, false, false, false, ctx.MentionedEntities);
        string runtime = BuildSceneUserRuntimeContextForSingle(speaker, hero, request.Audience, false, false, false, ctx.MentionedEntities);
        GetSceneReplyLengthLimits(DuelSettings.LoadedForSceneRuntime(), out int minTokens, out int maxTokens);
        string task = BuildSceneSingleNpcTaskSystemBlock(GetSceneNpcHistoryNameForPrompt(speaker), true, minTokens, maxTokens, GetPlayerDisplayNameForShout());
        string currentInstructions = BuildSceneCompositeUserBlock("", BuildSceneCurrentActionForPrompt(speaker, hero), task,
            ConversationRuntimeTexts.Prompt("AF_CONV_Ambient_NoActions"));
        string system = MainPromptMessageAssemblyOwner.BuildSceneSharedSystemPrompt(AppendPlayerCustomPromptRuleToSystemPrompt(""));
        List<object> messages = await _dispatcher.RunAsync("ambient_messages", speaker.Name, speaker.AgentIndex, () =>
            current() ? _ports.BuildStrictSceneMessagesForNpc(speaker.AgentIndex, system,
                MainPromptMessageAssemblyOwner.BuildSceneSingleSpeakerPrefixSections(MainPromptMessageAssemblyOwner.SceneSingleSpeakerLayout.GroupFallback,
                    privateRecent, persisted, runtime, present, "", trust, misc, "", knowledge, ""),
                suffixUserSections: new[] { instruction }, persistentHistoryMessages: memory?.UncompressedMessages,
                stablePersona: intro, currentInstructions: currentInstructions) : null, (List<object>)null);
        if (messages == null || !current()) return "";
        using var cache = ConversationPromptCache.Begin(messages, "scene", generation, session, "ambient:" + speaker.AgentIndex + "|" + speaker.UnnamedKey);
        // No ConfigureAwait(false): the chat runs in the scene main-thread synchronization context.
        string raw = await LegacyShoutNetworkGateway.SendLegacyMessagesAsync(cache.Messages, 5000, promptRetryOnError: false);
        if (!current()) return "";
        raw = LlmVisibleReplyNormalizer.NormalizeComplete(raw);
        if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0679"))
            || raw.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0680")) || raw.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0681"))
            || raw.StartsWith(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0682")))
            return "";
        string line = StripNpcNamePrefixSafely(raw.Replace("\r", "").Trim(), 30);
        line = StripLeakedPromptContentForShout(line);
        line = StripStageDirectionsForPassiveShout(line);
        // Text-only: drop every tag, including relay/end signals and action tags.
        line = PrepareSceneMainReplySpeechText(line, false, false);
        line = StripActionTagsForSceneSpeech(line).Trim();
        cache.Accept(line, current());
        return line;
    }

    private async Task<bool> PublishAmbientLineAsync(NpcDataPacket speaker, NpcDataPacket addressee, string line, AmbientChatRequest request,
        long generation, int session, int epoch, Func<bool> current)
    {
        if (string.IsNullOrWhiteSpace(PrepareSceneHistorySpeechText(line))) return false;
        var spoken = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var presented = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        bool presentationOpen = true;
        bool CanPresent() => presentationOpen && current() && request.CanContinue();
        bool queued = await _dispatcher.RunAsync("ambient_speech", speaker.Name, speaker.AgentIndex, () =>
        {
            if (!current()) return false;
            request.RegisterAddressee?.Invoke(speaker.AgentIndex, addressee.AgentIndex);
            // Empty player text marks an NPC-directed line: no player patience/mood settlement.
            _ports.EnqueueSpeechLineWithOptions(speaker, line, request.Audience, commitHistory: false, suppressStare: false,
                allowPlayerDirectedActions: false, requiredConversationEpoch: epoch, completionSource: spoken,
                canStillPublish: CanPresent,
                playerDirectedActionText: string.Empty, playerDirectedNpcReplyText: line,
                onPresented: (visible, finalSegment) =>
                {
                    // Actual presentation and both history writes share one game-thread turn.
                    if (!CanPresent() || !SaveRuntimeGuard.IsCurrentGeneration(generation) || session != _ports.SceneSessionId() || !_ports.IsOwnerCurrent())
                    { presented.TrySetResult(false); return; }
                    string heard = PrepareSceneHistorySpeechText(visible);
                    bool saved = true;
                    if (!string.IsNullOrWhiteSpace(heard))
                    {
                        _ports.RecordResponseForAllNearbySafe(request.Audience, speaker.AgentIndex, speaker.Name, heard, false,
                            addressee.AgentIndex, addressee.Name);
                        saved = _ports.PersistNpcSpeechToNamedHeroes(speaker.AgentIndex, speaker.Name, heard, request.Audience, false,
                            addressee.AgentIndex, addressee.Name);
                        request.OnLinePresented?.Invoke();
                    }
                    if (finalSegment || !saved) presented.TrySetResult(saved);
                });
            return true;
        }, false);
        if (!queued) return false;
        // Bounded: a cleared queue may drop the item without completing it.
        Task done = await Task.WhenAny(spoken.Task, Task.Delay(NativeConversationMainThreadPreprocessTimeoutMs));
        if (done != spoken.Task || !spoken.Task.Result) { presentationOpen = false; return false; }
        Task shown = await Task.WhenAny(presented.Task, Task.Delay(NativeConversationMainThreadPreprocessTimeoutMs));
        bool completed = shown == presented.Task && presented.Task.Result && current();
        presentationOpen = false;
        return completed;
    }

    private static Agent FindAgentByIndex(int agentIndex)
    {
        var agents = Mission.Current?.Agents;
        if (agents == null || agentIndex < 0) return null;
        for (int i = 0; i < agents.Count; i++)
        {
            Agent agent = agents[i];
            if (agent != null && agent.Index == agentIndex) return agent;
        }
        return null;
    }
}
