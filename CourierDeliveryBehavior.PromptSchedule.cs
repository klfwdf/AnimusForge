using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public partial class CourierDeliveryBehavior
{
	/// <summary>
	/// J04f: Courier prepares its prompt as scheduled steps instead of one Task.Run over the whole
	/// mixed builder. Owner phases run on the game thread with generation/owner checks; the two
	/// retrieval steps (Courier preprocess retrieval, shared routing, Knowledge candidates) run on the thread pool over
	/// detached requests. Null means the request was retired or the source changed (legacy semantics).
	/// </summary>
	private async Task<CourierPreparedPrompt> BuildCourierPreparedPromptScheduledAsync(CourierPromptInput input, CourierPromptRun promptRun, long generation, string source)
	{
		MyBehavior owner = MyBehavior.Instance;
		if (owner == null)
		{
			return null;
		}
		Log(input.Inbound ? "inbound letter llm start session=" + input.SessionId + " sender=" + input.ParticipantId
			: "llm main start session=" + input.SessionId + " recipient=" + input.ParticipantId);

		// Step 1 (game thread): Courier preprocess request + shared build request.
		var begin = await RunCourierOwnerPhaseAsync(generation, source + "_prompt_begin", () =>
		{
			if (!IsCourierPromptRunCurrent(promptRun) || !IsCourierPromptInputCurrent(input)) return null;
			MyBehavior.CourierPreprocessRequest preprocess = owner.BeginCourierRulePreprocess(input.Participant, input.RoutingInput, input.History.ExtraFact,
				input.Character, null, -1, CourierExcludedRuleIds);
			return new CourierPromptBeginState { Preprocess = preprocess };
		}, CancellationToken.None).ConfigureAwait(false);
		if (begin == null)
		{
			return null;
		}

		// Step 2 (thread pool): Courier preprocess retrieval → forced ids/mentions for the shared build.
		List<string> preprocessRuleHits = new List<string>();
		MentionedWorldEntities preprocessMentions = new MentionedWorldEntities();
		if (begin.Preprocess != null)
		{
			CourierPreprocessRetrievalResult retrieved = await Task.Run(() =>
			{
				using IDisposable scope = AIConfigHandler.BeginGuardrailRuntimeScope();
				AIConfigHandler.ApplyGuardrailRuntimeTarget(begin.Preprocess.Target, begin.Preprocess.Eligibility);
				try
				{
					List<string> hits = owner.RunCourierRulePreprocessRetrieval(begin.Preprocess, out MentionedWorldEntities mentions);
					return new CourierPreprocessRetrievalResult { RuleHits = hits, Mentions = mentions };
				}
				catch (PreprocessFormatException)
				{
					throw;
				}
				catch (Exception ex)
				{
					try { Logger.Log("CourierDelivery", "[Preprocess] failed: " + ex.Message); } catch { }
					return new CourierPreprocessRetrievalResult { RuleHits = new List<string>(), Mentions = new MentionedWorldEntities() };
				}
				finally
				{
					AIConfigHandler.ClearGuardrailRuntimeTarget();
					AIConfigHandler.SetGuardrailSemanticContext("");
				}
			}).ConfigureAwait(false);
			preprocessRuleHits = retrieved.RuleHits ?? new List<string>();
			preprocessMentions = retrieved.Mentions ?? new MentionedWorldEntities();
		}

		// Step 3 (game thread): shared build request with the preprocess results as forced ids.
		PromptBuildPhases phases = await RunCourierOwnerPhaseAsync(generation, source + "_prompt_capture", () =>
		{
			if (!IsCourierPromptRunCurrent(promptRun) || !IsCourierPromptInputCurrent(input)) return null;
			return owner.BeginSharedPromptBuild(input.Participant, input.RoutingInput, input.History.ExtraFact, input.CultureId, hasAnyHero: true,
				input.Character, null, -1, suppressDynamicRuleAndLore: false, usePrefetchedLoreContext: false, prefetchedLoreContext: null,
				excludedRuleIds: CourierExcludedRuleIds, preprocessExcludedRuleIds: null, forcedPreprocessRuleIds: preprocessRuleHits, preprocessMentionedEntities: preprocessMentions);
		}, CancellationToken.None).ConfigureAwait(false);
		if (phases == null)
		{
			// Empty routing input: the legacy facade returned an empty context rather than aborting.
			return new CourierPreparedPrompt(preprocessRuleHits, MyBehavior.CreateEmptyShoutPromptContext());
		}

		// Step 4 (thread pool): shared routing.
		await Task.Run(() =>
		{
			using IDisposable scope = AIConfigHandler.BeginGuardrailRuntimeScope();
			AIConfigHandler.ApplyGuardrailRuntimeTarget(phases.Request.Target, phases.Request.Eligibility);
			try { owner.RunSharedPromptRouting(phases); }
			finally { AIConfigHandler.ClearGuardrailRuntimeTarget(); }
		}).ConfigureAwait(false);

		// Step 5 (game thread): prepare the Lore index after routing supplied all mentions.
		PromptBuildPhases prepared = await RunCourierOwnerPhaseAsync(generation, source + "_knowledge_capture", () =>
		{
			if (!IsCourierPromptRunCurrent(promptRun) || !IsCourierPromptInputCurrent(input)) return null;
			owner.CaptureSharedKnowledgeSnapshot(phases);
			return phases;
		}, CancellationToken.None).ConfigureAwait(false);
		if (!ReferenceEquals(prepared, phases)) return null;

		// Step 6 (thread pool): candidate retrieval from the prepared, versioned index.
		await Task.Run(() => owner.RunSharedKnowledgeRetrieval(phases)).ConfigureAwait(false);

		// Step 7 (game thread): sections, assembly, appendices. A declined owner phase aborts (null);
		// a null context from the builder itself is a legacy-tolerated result and still assembles.
		CourierPreparedPrompt completed = await RunCourierOwnerPhaseAsync(generation, source + "_prompt_complete", () =>
		{
			if (!IsCourierPromptRunCurrent(promptRun) || !IsCourierPromptInputCurrent(input)) return null;
			using IDisposable scope = AIConfigHandler.BeginGuardrailRuntimeScope();
			AIConfigHandler.ApplyGuardrailRuntimeTarget(phases.Request.Target, phases.Request.Eligibility);
			try { return new CourierPreparedPrompt(preprocessRuleHits, owner.CompleteSharedPromptBuild(phases, input.Participant, input.Character, null)); }
			finally { AIConfigHandler.ClearGuardrailRuntimeTarget(); }
		}, CancellationToken.None).ConfigureAwait(false);
		return completed;
	}

	private sealed class CourierPromptBeginState
	{
		internal MyBehavior.CourierPreprocessRequest Preprocess;
	}

	private sealed class CourierPreprocessRetrievalResult
	{
		internal List<string> RuleHits;
		internal MentionedWorldEntities Mentions;
	}
}
