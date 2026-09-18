using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class CourierDeliveryBehavior
{
    // A runtime-only reservation begins at the actual Start, before persona/history/network work.
    // Weak keys do not extend a session lifetime or add fields to the saved CourierSession schema.
    private readonly ConditionalWeakTable<CourierSession, CourierPromptRun> _courierPromptRuns =
        new ConditionalWeakTable<CourierSession, CourierPromptRun>();

    private sealed class CourierPromptRun
    {
        internal CourierPromptRun(CourierSession session, long generation)
        { Session = session; Generation = generation; }
        internal CourierSession Session { get; }
        internal long Generation { get; }
    }

    private CourierPromptRun BeginCourierPromptRun(CourierSession session, long generation)
    {
        if (!TWParallel.IsMainThread()) throw new InvalidOperationException("Courier run reservation requires the game thread.");
        var run = new CourierPromptRun(session, generation);
        // All replacements and checks use the original game-thread owner, not a worker lock.
        _courierPromptRuns.Remove(session);
        _courierPromptRuns.Add(session, run);
        return run;
    }

    private bool IsCourierPromptRunCurrent(CourierPromptRun run)
    {
        if (!TWParallel.IsMainThread()) throw new InvalidOperationException("Courier run validation requires the game thread.");
        return run != null && ReferenceEquals(Instance, this) && _pendingOwnerPhases.Accepting
            && SaveRuntimeGuard.IsCurrentGeneration(run.Generation)
            && ReferenceEquals(GetSessionById(run.Session.Id), run.Session)
            && _courierPromptRuns.TryGetValue(run.Session, out CourierPromptRun current) && ReferenceEquals(current, run)
            && run.Session.ReplyGenerationStarted && !run.Session.ReplyGenerated;
    }

    private void CompleteCourierPromptSourceChanged(CourierPromptRun run, CourierPromptInput input)
    {
        // Do not release a new run on the same object, including one still in persona/history work.
        if (!IsCourierPromptRunCurrent(run)
            || !IsCourierHistoryOwnerCurrent(input.SessionId, input.Session, input.Participant, input.Inbound)
            || input.Participant.IsDead) return;
        _courierPromptRuns.Remove(input.Session);
        // No actions were submitted. Use the original failure owner to finish generation and advance
        // the actual transport/wait state machine; never leave Started=true or invent an LLM success.
        if (input.Inbound)
            FailInboundLetterGenerationOnMainThread(input.SessionId, input.Generation,
                input.Session.InboundFallbackLetter, "inbound_prompt_source_changed");
        else
        {
            // A previous preflight/recovery may have left reply tags on the same saved session.
            // Source invalidation must not let the failure tick execute or display those old tags.
            input.Session.ReplyText = string.Empty;
            input.Session.ReplyPostprocessedText = string.Empty;
            input.Session.PostprocessConsumed = true;
            FailCourierReplyGenerationOnMainThread(input.SessionId, input.Generation, "reply_prompt_source_changed");
        }
    }

    // Request-local routing values are captured once. Identity handles remain here solely because
    // the shared rule/lore builder has not yet acquired its own complete game-free snapshot seam.
    // This is not a claim that BuildCourierPreparedPrompt is wholly free of live game reads.
    private sealed class CourierPromptInput
    {
        internal CourierSession Session { get; }
        internal Hero Participant { get; }
        internal CharacterObject Character { get; }
        internal string CultureId { get; }
        internal string SessionId { get; }
        internal string ParticipantId { get; }
        internal bool Inbound { get; }
        internal long Generation { get; }
        internal string LetterText { get; }
        internal string IntentText { get; }
        internal string StoredFallbackLetter { get; }
        internal string FallbackLetter { get; }
        internal string Seed { get; }
        internal string RoutingInput { get; }
        internal CourierPreparedHistory History { get; }

        internal CourierPromptInput(CourierSession session, Hero participant, bool inbound,
            string fallbackLetter, long generation, CourierPreparedHistory history)
        {
            Session = session;
            Participant = participant;
            Character = participant.CharacterObject;
            CultureId = participant.Culture?.StringId ?? "neutral";
            SessionId = session.Id;
            ParticipantId = SafeHeroId(participant);
            Inbound = inbound;
            Generation = generation;
            LetterText = session.LetterText;
            IntentText = session.InboundIntentText;
            StoredFallbackLetter = session.InboundFallbackLetter;
            FallbackLetter = fallbackLetter;
            Seed = string.IsNullOrWhiteSpace(IntentText)
                ? (string.IsNullOrWhiteSpace(LetterText) ? fallbackLetter : LetterText.Trim())
                : IntentText.Trim();
            RoutingInput = inbound ? "[NPC主动写信意图] " + Seed : LetterText;
            History = history;
        }
    }

    private sealed class CourierPreparedPrompt
    {
        internal CourierPreparedPrompt(List<string> ruleHits, MyBehavior.ShoutPromptContext context)
        { RuleHits = ruleHits; Context = context; }
        internal List<string> RuleHits { get; }
        internal MyBehavior.ShoutPromptContext Context { get; }
    }

    private static CourierPromptInput CaptureCourierPromptInput(CourierSession session, Hero participant,
        bool inbound, string fallbackLetter, long generation, CourierPreparedHistory history)
    {
        if (!TWParallel.IsMainThread()) throw new InvalidOperationException("Courier prompt capture requires the game thread.");
        if (history == null) throw new ArgumentNullException(nameof(history));
        return new CourierPromptInput(session, participant, inbound, fallbackLetter, generation, history);
    }

    private bool IsCourierPromptInputCurrent(CourierPromptInput input)
    {
        if (!IsCourierHistoryOwnerCurrent(input.SessionId, input.Session, input.Participant, input.Inbound)
            || input.Participant.IsDead) return false;
        // Same object/generation alone is insufficient if a live letter was edited during routing.
        return string.Equals(input.Session.LetterText, input.LetterText, StringComparison.Ordinal)
            && (!input.Inbound || (string.Equals(input.Session.InboundIntentText, input.IntentText, StringComparison.Ordinal)
                && string.Equals(input.Session.InboundFallbackLetter, input.StoredFallbackLetter, StringComparison.Ordinal)));
    }

    private static CourierPreparedPrompt BuildCourierPreparedPrompt(CourierPromptInput input)
    {
        // Preserve the exact existing two-stage routing and its failure semantics. In the normal
        // async flow this mixed shared builder stays off the game thread; migrating its internal
        // live reads requires a separate shared Native/Scene/Courier snapshot, not Task.Run removal.
        Log(input.Inbound ? "inbound letter llm start session=" + input.SessionId + " sender=" + input.ParticipantId
            : "llm main start session=" + input.SessionId + " recipient=" + input.ParticipantId);
        List<string> preprocessRuleHits = MyBehavior.RunCourierRulePreprocessForExternal(input.Participant,
            input.RoutingInput, input.History.ExtraFact, out var preprocessMentionedEntities,
            input.Character, targetAgentIndex: -1, excludedRuleIds: CourierExcludedRuleIds);
        MyBehavior.ShoutPromptContext ctx = MyBehavior.BuildShoutPromptContextForExternal(input.Participant,
            input.RoutingInput, input.History.ExtraFact, input.CultureId, hasAnyHero: true,
            targetCharacter: input.Character, targetAgentIndex: -1, excludedRuleIds: CourierExcludedRuleIds,
            forcedPreprocessRuleIds: preprocessRuleHits, preprocessMentionedEntities: preprocessMentionedEntities);
        return new CourierPreparedPrompt(preprocessRuleHits, ctx);
    }

    private async Task<T> PrepareCourierPromptRequestAsync<T>(string sessionId, CourierSession session,
        Hero participant, bool inbound, string fallbackLetter, long generation,
        CourierPreparedHistory history, CourierPromptRun promptRun, Func<CourierPromptInput, CourierPreparedPrompt, T> assemble) where T : class
    {
        string source = inbound ? "courier_inbound_prompt" : "courier_reply_prompt";
        try
        {
            CourierPromptInput input = await RunCourierOwnerPhaseAsync(generation, source + "_capture", () =>
            {
                if (!IsCourierPromptRunCurrent(promptRun) || !IsCourierHistoryOwnerCurrent(sessionId, session, participant, inbound) || participant.IsDead) return null;
                return CaptureCourierPromptInput(session, participant, inbound, fallbackLetter, generation, history);
            }, CancellationToken.None).ConfigureAwait(false);
            if (input == null) return null;

            // J04f: the shared prompt build runs as owner phases (game thread) and thread-pool retrieval
            // steps; see CourierDeliveryBehavior.PromptSchedule.cs. The final assembly below still uses the
            // existing live role/assets/history-message builders and therefore belongs to this owner.
            CourierPreparedPrompt prepared = await BuildCourierPreparedPromptScheduledAsync(input, promptRun, generation, source).ConfigureAwait(false);
            if (prepared == null) return null;
            return await RunCourierOwnerPhaseAsync(generation, source + "_assemble", () =>
            {
                if (!IsCourierPromptRunCurrent(promptRun)) return null;
                if (!IsCourierPromptInputCurrent(input))
                {
                    CompleteCourierPromptSourceChanged(promptRun, input);
                    return null;
                }
                return assemble(input, prepared);
            }, CancellationToken.None).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ReferenceEquals(Instance, this)
            || !SaveRuntimeGuard.IsCurrentGeneration(generation))
        {
            // Retired work disappears without a fake failure letter. Same-owner timeouts still
            // reach the established caller's failure handling rather than masquerading as success.
            return null;
        }
    }

    // These synchronous compatibility captures still have real public callers. Their main-thread
    // contract is unchanged; normal asynchronous preparation never uses their blocking preprocess.
    private CourierReplyGenerationRequest BuildCourierReplyGenerationRequestOnMainThread(CourierSession session, Hero recipient, long runtimeGeneration, CourierPreparedHistory preparedHistory)
    {
        CourierPromptInput input = CaptureCourierPromptInput(session, recipient, false, null, runtimeGeneration, preparedHistory);
        return BuildReplyRequestFromPreparedPrompt(input, BuildCourierPreparedPrompt(input));
    }

    private InboundLetterGenerationRequest BuildInboundLetterGenerationRequestOnMainThread(CourierSession session, Hero sender, string fallbackLetter, long runtimeGeneration, CourierPreparedHistory preparedHistory)
    {
        CourierPromptInput input = CaptureCourierPromptInput(session, sender, true, fallbackLetter, runtimeGeneration, preparedHistory);
        return BuildInboundRequestFromPreparedPrompt(input, BuildCourierPreparedPrompt(input));
    }

	private CourierReplyGenerationRequest BuildReplyRequestFromPreparedPrompt(CourierPromptInput input, CourierPreparedPrompt prepared)
	{
		CourierSession session = input.Session;
		Hero recipient = input.Participant;
		long runtimeGeneration = input.Generation;
		string extraFact = input.History.ExtraFact;
		string historyText = input.History.Text;
		List<string> preprocessRuleHits = prepared.RuleHits;
		MyBehavior.ShoutPromptContext ctx = prepared.Context;
		List<string> selectedRuleHits = MergeCourierSelectedRuleIds(preprocessRuleHits, ctx?.PreprocessRuleIds);
		selectedRuleHits = ExcludeCourierSelectedRuleIds(selectedRuleHits, CourierExcludedRuleIds) ?? new List<string>();
		string extras = (ctx?.Extras ?? "").Trim();
		extras = AppendCourierPlayerRecentActions(extras, recipient);
		if (HasPreprocessRuleHit(selectedRuleHits, "worldmap_party_command") || ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "worldmap_party_command"))
		{
			string commandTasks = WorldMapPartyCommandBehavior.BuildCurrentNpcCommandTasksPromptForExternal(recipient, recipient.CharacterObject, -1);
			extras = string.IsNullOrWhiteSpace(extras) ? commandTasks : (extras.TrimEnd() + "\n" + commandTasks);
		}
		List<ConversationMessage> persistentMemoryRoleMessages = MyBehavior.BuildUncompressedMemoryRoleMessagesForExternal(recipient, -1, includeCurrentActiveSceneSession: false);
		string npcRoleContext = ShoutBehavior.BuildHeroStableRoleContextForExternal(recipient);
		List<object> messages = BuildCourierReplyMessages(recipient, session, extras, extraFact, historyText, persistentMemoryRoleMessages, npcRoleContext, ctx?.PreprocessExcludedRuleBlock);
		LogCourierContextAlignment("reply", session.Id, recipient, npcRoleContext, extras, ctx?.EntityPostprocessContext, historyText, persistentMemoryRoleMessages);
		return new CourierReplyGenerationRequest
		{
			SessionId = session.Id,
			RuntimeGeneration = runtimeGeneration,
			RecipientHeroId = SafeHeroId(recipient),
			RecipientName = recipient.Name?.ToString() ?? "NPC",
			LetterText = session.LetterText ?? "",
			ExtraFact = extraFact ?? "",
			HistoryText = historyText,
			Extras = extras ?? "",
			EntityPostprocessContext = ctx?.EntityPostprocessContext ?? "",
			SelectedRuleHits = selectedRuleHits,
			Messages = messages ?? new List<object>()
		};
	}

	private InboundLetterGenerationRequest BuildInboundRequestFromPreparedPrompt(CourierPromptInput input, CourierPreparedPrompt prepared)
	{
		CourierSession session = input.Session;
		Hero sender = input.Participant;
		long runtimeGeneration = input.Generation;
		string extraFact = input.History.ExtraFact;
		string historyText = input.History.Text;
		List<string> preprocessRuleHits = prepared.RuleHits;
		MyBehavior.ShoutPromptContext ctx = prepared.Context;
		string seed = input.Seed;
		string fallbackLetter = input.FallbackLetter;
		List<string> selectedRuleHits = MergeCourierSelectedRuleIds(preprocessRuleHits, ctx?.PreprocessRuleIds);
		selectedRuleHits = ExcludeCourierSelectedRuleIds(selectedRuleHits, CourierExcludedRuleIds) ?? new List<string>();
		string extras = (ctx?.Extras ?? "").Trim();
		extras = AppendCourierPlayerRecentActions(extras, sender);
		if (HasPreprocessRuleHit(selectedRuleHits, "worldmap_party_command") || ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "worldmap_party_command"))
		{
			string commandTasks = WorldMapPartyCommandBehavior.BuildCurrentNpcCommandTasksPromptForExternal(sender, sender.CharacterObject, -1);
			extras = string.IsNullOrWhiteSpace(extras) ? commandTasks : (extras.TrimEnd() + "\n" + commandTasks);
		}
		List<ConversationMessage> persistentMemoryRoleMessages = MyBehavior.BuildUncompressedMemoryRoleMessagesForExternal(sender, -1, includeCurrentActiveSceneSession: false);
		string npcRoleContext = ShoutBehavior.BuildHeroStableRoleContextForExternal(sender);
		List<object> messages = BuildInboundNpcLetterMessages(sender, session, seed, extras, extraFact, historyText, persistentMemoryRoleMessages, npcRoleContext, ctx?.PreprocessExcludedRuleBlock);
		LogCourierContextAlignment("inbound", session.Id, sender, npcRoleContext, extras, ctx?.EntityPostprocessContext, historyText, persistentMemoryRoleMessages);
		return new InboundLetterGenerationRequest
		{
			SessionId = session.Id,
			RuntimeGeneration = runtimeGeneration,
			SenderHeroId = SafeHeroId(sender),
			Seed = seed ?? "",
			FallbackLetter = fallbackLetter ?? "",
			SelectedRuleHits = selectedRuleHits,
			Messages = messages ?? new List<object>()
		};
	}
}
