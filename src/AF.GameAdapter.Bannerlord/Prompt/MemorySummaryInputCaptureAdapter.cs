using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using TaleWorlds.CampaignSystem;
using MemorySummaryContextDependencies = AnimusForge.MyBehavior.MemorySummaryContextDependencies;
using MemorySummarySceneDependency = AnimusForge.MyBehavior.MemorySummarySceneDependency;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using TaleWorlds.Library;
using AnimusForge.Refactor.Runtime;
using MemorySummarySourceView = AnimusForge.MyBehavior.MemorySummarySourceView;
using MemorySummaryInput = AnimusForge.MyBehavior.MemorySummaryInput;
namespace AnimusForge.Refactor.Adapters;
internal sealed class MemorySummaryInputCaptureAdapter
{
    private readonly MemoryBusinessStateOwner _state;
    private readonly Func<bool> _instanceOwnerCurrent, _campaignOwnerCurrent;
    internal MemorySummaryInputCaptureAdapter(MemoryBusinessStateOwner state,Func<bool> instanceOwnerCurrent,Func<bool> campaignOwnerCurrent)
    { _state=state ?? throw new ArgumentNullException(nameof(state)); _instanceOwnerCurrent=instanceOwnerCurrent ?? throw new ArgumentNullException(nameof(instanceOwnerCurrent)); _campaignOwnerCurrent=campaignOwnerCurrent ?? throw new ArgumentNullException(nameof(campaignOwnerCurrent)); }
    internal static T CloneMemorySummarySource<T>(T value)
    {
        if (ReferenceEquals(value, null)) return default(T);
        object copy = value switch
        {
            DailyMemoryLine item => MemoryRecordRules.Clone(item),
            DailyMemoryDraft item => MemoryRecordRules.Clone(item),
            CompressedMemoryBlock item => MemoryRecordRules.Clone(item),
            WeeklyMemoryMaterialTrigger item => MemoryRecordRules.Clone(item),
            MemorySummaryJob item => MemoryRecordRules.Clone(item),
            MemoryOverviewJob item => MemoryRecordRules.Clone(item),
            MajorActionSummaryJob item => MemoryRecordRules.Clone(item),
            MemoryOverviewState item => MemoryRecordRules.Clone(item),
            MajorActionSummaryState item => MemoryRecordRules.Clone(item),
            NpcActionEntry item => item.CopyForSummary(),
            List<NpcActionEntry> items => items.Select(x => x?.CopyForSummary()).ToList(),
            List<CompressedMemoryBlock> items => MemoryRecordRules.Clone(items),
            _ => throw new ArgumentException("Unsupported memory summary data model: " + typeof(T).Name)
        };
        return (T)copy;
    }

    internal static string[] CollectMemorySummaryLookupIds(IEnumerable<string> ids)
    {
        return ids.Select(x => (x ?? "").Trim()).Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.Ordinal).ToArray();
    }

    internal static void DescribeMemorySummaryMajorContext(MemorySummaryInput input, List<NpcActionEntry> added)
    {
        // Only fields actually resolved by BuildNpcActionMetadataNarrativeSuffix.
        // Related*/Actor* IDs remain in the raw digest, but do not trigger extra registry scans.
        input.Context.HeroIds = CollectMemorySummaryLookupIds(added.Select(x => x.TargetHeroId));
        input.Context.ClanIds = CollectMemorySummaryLookupIds(added.SelectMany(x => new[]
            { x.TargetClanId, x.SettlementOwnerClanId, x.PreviousSettlementOwnerClanId }));
        input.Context.KingdomIds = CollectMemorySummaryLookupIds(added.SelectMany(x => new[]
            { x.TargetKingdomId, x.SettlementOwnerKingdomId, x.PreviousSettlementOwnerKingdomId }));
        input.Context.SettlementLookups = CollectMemorySummaryLookupIds(added
            .Where(x => string.IsNullOrWhiteSpace(x.LocationText) && string.IsNullOrWhiteSpace(x.SettlementName))
            .Select(x => x.SettlementId)).Select(x => new NpcActionEntry { SettlementId = x }).ToArray();
    }

    internal static string ComputeMemorySummaryFingerprint(object identity)
    {
        using (var hash = SHA256.Create())
        using (var crypto = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write))
        {
            using (var text = new StreamWriter(crypto, new UTF8Encoding(false), 4096, leaveOpen: true))
            using (var json = new JsonTextWriter(text))
            {
                JsonSerializer.CreateDefault().Serialize(json, identity);
            }
            crypto.FlushFinalBlock();
            return BitConverter.ToString(hash.Hash).Replace("-", "");
        }
    }
    internal MemorySummarySourceView ReadMemorySummarySource(object queueJob, long generation)
    {
        if (!TWParallel.IsMainThread() || !_instanceOwnerCurrent()
            || !SaveRuntimeGuard.IsCurrentGeneration(generation)
            || !_campaignOwnerCurrent()) return null;
        var source = new MemorySummarySourceView { Job = queueJob };
        if (queueJob is MemorySummaryJob daily)
        {
            source.HeroId = MemoryRecordRules.NormalizeMemoryHeroId(daily.HeroId);
            if (_state.DailyQueue == null || !_state.DailyQueue.Contains(daily)
                || _state.HasCompressedMemoryBlock(source.HeroId, daily.GameDayIndex)) return null;
            source.Draft = _state.FindMemoryDraft(daily);
            if (source.Draft == null) return null;
        }
        else if (queueJob is MajorActionSummaryJob major)
        {
            source.HeroId = MemoryRecordRules.NormalizeMemoryHeroId(major.HeroId);
            if (_state.MajorQueue == null || !_state.MajorQueue.Contains(major)
                || MemoryBusinessStateOwner.IsNonHeroMemoryId(source.HeroId) || _state.MajorActions == null
                || !_state.MajorActions.TryGetValue(source.HeroId, out source.Actions) || source.Actions == null) return null;
            source.StatePresent = _state.MajorSummaries != null
                && _state.MajorSummaries.TryGetValue(source.HeroId, out source.MajorState);
        }
        else if (queueJob is MemoryOverviewJob overview)
        {
            source.HeroId = MemoryRecordRules.NormalizeMemoryHeroId(overview.HeroId);
            if (_state.OverviewQueue == null || !_state.OverviewQueue.Contains(overview)
                || _state.Blocks == null
                || !_state.Blocks.TryGetValue(source.HeroId, out source.Blocks) || source.Blocks == null) return null;
            source.StatePresent = _state.Overviews != null
                && _state.Overviews.TryGetValue(source.HeroId, out source.Overview);
        }
        else return null;
        return MemoryEntityIdentityBannerlordAdapter.IsMemoryEntityEligibleForCompressedMemory(source.HeroId) ? source : null;
    }
    internal static void DescribeMemorySummaryDailyContext(MemorySummaryInput input)
    {
        var context = input.Context;
        var lines = input.Draft.Lines ?? new List<DailyMemoryLine>();
        context.DailySourceCharCount = MemorySummaryPlanningRules.CountDailySourceChars(input.Draft);
        context.UnknownTextSceneDays = lines.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Text) && UncompressedMemoryMessageAssemblyOwner.IsUnknownMemorySceneLabel(x.Scene))
            .Select(x => x.GameDayIndex).Distinct().ToArray();
        var scenes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var days = new HashSet<int>();
        var header = new List<MemorySummarySceneDependency>();
        // Original header visits normal lines before AFEF, including blank-text lines.
        // Keep only first scene/day occurrences, which is sufficient to reproduce its
        // ordered Distinct without retaining or rescanning all dialogue text on checks.
        foreach (var line in lines.Where(x => x != null && !x.IsAfef).Concat(lines.Where(x => x != null && x.IsAfef)))
        {
            string scene = (line.Scene ?? "").Trim();
            if (UncompressedMemoryMessageAssemblyOwner.IsUnknownMemorySceneLabel(scene))
            {
                if (days.Add(line.GameDayIndex)) header.Add(new MemorySummarySceneDependency { UnknownDay = line.GameDayIndex });
            }
            else if (scenes.Add(scene)) header.Add(new MemorySummarySceneDependency { Scene = scene });
        }
        context.HasKnownSourceScene = scenes.Count > 0;
        context.SceneHeader = header.ToArray();
    }
    internal static object CaptureMemorySummaryDailySceneContext(MemorySummaryContextDependencies context)
    {
        int day = MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe();
        bool headerUsesToday = context.SceneHeader.Any(x => x.Scene == null && x.UnknownDay == day);
        string scene = headerUsesToday || !context.HasKnownSourceScene ? SceneLocationPromptCaptureAdapter.ResolveCurrentMemorySceneLabel() : null;
        scene = UncompressedMemoryMessageAssemblyOwner.IsUnknownMemorySceneLabel(scene) ? null : scene.Trim();
        var header = context.SceneHeader.Select(x => x.Scene ?? (x.UnknownDay == day ? scene : null))
            .Where(x => x != null).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (header.Count == 0 && scene != null) header.Add(scene);
        bool textUsesToday = scene != null && Array.IndexOf(context.UnknownTextSceneDays, day) >= 0;
        // Compare rendered dependencies, not every movement/day. A blank-text line
        // can contribute a header scene but never a line body. If its contribution
        // duplicates an earlier scene, removing that contribution is immaterial.
        return new
        {
            ActiveUnknownTextDay = textUsesToday ? (int?)day : null,
            TextScene = textUsesToday ? scene : null, HeaderScenes = header
        };
    }
    internal string CaptureMemorySummaryContextFingerprint(MemorySummaryInput input)
    {
        var hero = MemoryEntityIdentityBannerlordAdapter.FindHeroById(input.HeroId);
        int trust = RewardSystemBehavior.Instance?.GetEffectiveTrust(hero) ?? 0;
        object rendering;
        if (input.Job is MemorySummaryJob)
        {
            // This query can mark observer knowledge; keep it on the owner thread,
            // before taking the shared history identity and before original Build calls.
            string observerName = PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForPrompt(hero);
            rendering = new
            {
                TargetChars = Math.Max(80, input.Context.DailySourceCharCount / Math.Max(1, LlmRequestConfigurationCaptureAdapter.GetMemoryCompressionDenominatorFromSettings())),
                Requirements = MemorySummaryRules.WritingRequirements(DuelSettings.GetSettings()?.DailyMemoryCompressionWritingRequirements),
                PlayerName = PlayerNotorietyBehavior.BuildPlayerHistoryNameForExternal(),
                ObserverName = string.IsNullOrWhiteSpace(observerName) ? "玩家" : observerName,
                TrustText = trust.ToString(),
                Scene = CaptureMemorySummaryDailySceneContext(input.Context)
            };
        }
        else if (input.Job is MajorActionSummaryJob)
        {
            rendering = new
            {
                Requirements = MemorySummaryRules.WritingRequirements(DuelSettings.GetSettings()?.MajorActionCompressionWritingRequirements),
                HeroNames = input.Context.HeroIds.Select(MemoryEntityIdentityBannerlordAdapter.ResolveHeroName).ToArray(),
                ClanNames = input.Context.ClanIds.Select(MemoryEntityIdentityBannerlordAdapter.ResolveClanName).ToArray(),
                KingdomNames = input.Context.KingdomIds.Select(MemoryEntityIdentityBannerlordAdapter.ResolveKingdomName).ToArray(),
                SettlementNames = input.Context.SettlementLookups.Select(MemoryEntityIdentityBannerlordAdapter.ResolveDisplayNameBySettlementEntry).ToArray()
            };
        }
        else
        {
            rendering = new
            {
                TargetChars = LlmRequestConfigurationCaptureAdapter.GetMemoryOverviewTargetCharsFromSettings(),
                Requirements = MemorySummaryRules.WritingRequirements(DuelSettings.GetSettings()?.MemoryOverviewCompressionWritingRequirements)
            };
        }
        return ComputeMemorySummaryFingerprint(new
        {
            HeroName = hero?.Name?.ToString(), Trust = trust,
            PlayerHistoryRendering = PlayerNotorietyBehavior.CaptureMemorySummaryHistoryRenderingIdentity(),
            Culture = CultureInfo.CurrentCulture.Name, Rendering = rendering
        });
    }
    internal bool IsMemorySummaryInputCurrent(MemorySummaryInput input)
    {
        if (input == null || (input.Run != null && !input.Run.IsCurrent)) return false;
        var initialSource = ReadMemorySummarySource(input.QueueJob, input.Generation);
        if (initialSource == null || !string.Equals(initialSource.HeroId, input.HeroId, StringComparison.OrdinalIgnoreCase)) return false;
        // Context getters may synchronously publish knowledge. Re-read the live raw
        // view AFTER them so a replacement or edit during a getter cannot pass using
        // an orphaned pre-getter view. There is still just one full raw digest per check.
        if (!string.Equals(CaptureMemorySummaryContextFingerprint(input), input.ContextFingerprint, StringComparison.Ordinal)) return false;
        // Read dynamic eligibility before the final raw view as well: even a
        // settings getter must not publish an edit after the source digest.
        if (input.Job is MemoryOverviewJob && input.OverviewBlockCount < LlmRequestConfigurationCaptureAdapter.GetMemoryOverviewStartBlockCountFromSettings()) return false;
        var source = ReadMemorySummarySource(input.QueueJob, input.Generation);
        if (source == null || !string.Equals(MemorySourceFingerprintRules.Compute(source),
            input.SourceFingerprint, StringComparison.Ordinal)) return false;
        return (input.Run == null || input.Run.IsCurrent) && _instanceOwnerCurrent() && SaveRuntimeGuard.IsCurrentGeneration(input.Generation)
            && _campaignOwnerCurrent();
    }
	internal static string BuildMemorySummarySystemPrompt(DailyMemoryDraft draft)
	{
		int denominator = LlmRequestConfigurationCaptureAdapter.GetMemoryCompressionDenominatorFromSettings();
		int sourceChars = MemorySummaryPlanningRules.CountDailySourceChars(draft);
		string playerHistoryName = PlayerNotorietyBehavior.BuildPlayerHistoryNameForExternal();
		string requirements = DuelSettings.GetSettings()?.DailyMemoryCompressionWritingRequirements;
		return MemorySummaryRules.DailySystemPrompt(sourceChars, denominator, playerHistoryName, requirements);
	}
	internal static string BuildMemorySummaryUserPrompt(Hero hero, DailyMemoryDraft draft)
	{
		string text = string.IsNullOrWhiteSpace(draft.GameDate) ? ("第" + draft.GameDayIndex + "日") : draft.GameDate.Trim();
		List<DailyMemoryLine> normalLines = (draft.Lines ?? new List<DailyMemoryLine>()).Where((DailyMemoryLine x) => x != null && !x.IsAfef).ToList();
		List<DailyMemoryLine> afefLines = (draft.Lines ?? new List<DailyMemoryLine>()).Where((DailyMemoryLine x) => x != null && x.IsAfef).ToList();
		int startHour = normalLines.Concat(afefLines).Select((DailyMemoryLine x) => MBMath.ClampInt(x.GameHour, 0, 23)).DefaultIfEmpty(0).Min();
		int endHour = normalLines.Concat(afefLines).Select((DailyMemoryLine x) => MBMath.ClampInt(x.GameHour, 0, 23)).DefaultIfEmpty(0).Max();
		List<string> scenes = normalLines.Concat(afefLines).Select(MemoryRecallInputCaptureAdapter.ResolveCapturedMemoryLineSceneForPrompt).Where((string x) => !string.IsNullOrWhiteSpace(x) && !UncompressedMemoryMessageAssemblyOwner.IsUnknownMemorySceneLabel(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		if (scenes.Count == 0)
		{
			string fallbackScene = SceneLocationPromptCaptureAdapter.ResolveCurrentMemorySceneLabel();
			if (!UncompressedMemoryMessageAssemblyOwner.IsUnknownMemorySceneLabel(fallbackScene)) scenes.Add(fallbackScene.Trim());
		}
		string hours = MemoryRecallContextOwner.FormatMemoryHourRange(startHour, endHour);
		string heroName = hero?.Name?.ToString() ?? draft.HeroName ?? "NPC";
		int trust = RewardSystemBehavior.Instance?.GetEffectiveTrust(hero) ?? 0;
		string playerDisplayName = PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForPrompt(hero);
		if (string.IsNullOrWhiteSpace(playerDisplayName)) playerDisplayName = "玩家";
		string playerHistoryName = PlayerNotorietyBehavior.BuildPlayerHistoryNameForExternal();
		var normalTexts = new List<string>();
		foreach (DailyMemoryLine line in normalLines) normalTexts.Add(MemorySummaryApplicationAdapter.BuildDailyMemoryLineForPrompt(line, MemoryRecallInputCaptureAdapter.ResolveCapturedMemoryLineSceneForPrompt));
		var afefTexts = new List<string>();
		if (afefLines.Count > 0)
			foreach (DailyMemoryLine line in afefLines) afefTexts.Add(MemorySummaryApplicationAdapter.BuildDailyMemoryLineForPrompt(line, MemoryRecallInputCaptureAdapter.ResolveCapturedMemoryLineSceneForPrompt));
		return MemorySummaryRules.DailyUserPrompt(text, hours, heroName, trust, scenes,
			playerDisplayName, playerHistoryName, normalTexts, afefTexts, afefLines.Count > 0);
	}
	internal static string BuildMajorActionSummarySystemPrompt(int targetChars)
	{
		string requirements = DuelSettings.GetSettings()?.MajorActionCompressionWritingRequirements;
		return MemorySummaryRules.MajorSystemPrompt(targetChars, requirements);
	}
	internal static string BuildMemoryOverviewSummarySystemPrompt(int targetChars)
	{
		string requirements = DuelSettings.GetSettings()?.MemoryOverviewCompressionWritingRequirements;
		return MemorySummaryRules.OverviewSystemPrompt(targetChars, requirements);
	}
    internal MemorySummaryInput CaptureMemorySummaryInput(object queueJob, long generation, string expectedJobFingerprint = null, MemorySummaryRunOwner.Lease run = null)
    {
        if (run != null && !run.IsCurrent) return null;
        var source = ReadMemorySummarySource(queueJob, generation);
        if (source == null) return null;
        // A plan can span ticks. Its job identity and the source are checked in this
        // SAME callback, not through a separate preflight await.
        if (expectedJobFingerprint != null && !string.Equals(expectedJobFingerprint,
            ComputeMemorySummaryFingerprint(queueJob), StringComparison.Ordinal)) return null;
        var input = new MemorySummaryInput
        {
            Generation = generation, Run = run, QueueJob = queueJob, HeroId = source.HeroId,
            SourceFingerprint = MemorySourceFingerprintRules.Compute(source)
        };
        var hero = MemoryEntityIdentityBannerlordAdapter.FindHeroById(input.HeroId);
        // Validate the same pending facts against the copies we actually render.
        // Do not run Has*Pending first: Major/Overview would normalize/copy the
        // entire source again, then discard that work before constructing these inputs.
        if (queueJob is MemorySummaryJob daily)
        {
            if (daily.RetryCount >= 3) return null;
            input.Job = CloneMemorySummarySource(daily);
            input.Draft = CloneMemorySummarySource(source.Draft);
            DescribeMemorySummaryDailyContext(input);
            if (input.Draft.SummaryRetryCount >= 3 || !input.Draft.HasLlmDialogue || input.Context.DailySourceCharCount <= 0) return null;
            input.ContextFingerprint = CaptureMemorySummaryContextFingerprint(input);
            input.SystemPrompt = BuildMemorySummarySystemPrompt(input.Draft);
            input.UserPrompt = BuildMemorySummaryUserPrompt(hero, input.Draft);
        }
        else if (queueJob is MajorActionSummaryJob major)
        {
            if (major.RetryCount >= 3) return null;
            input.Job = CloneMemorySummarySource(major);
            input.Actions = NpcActionLedger.SanitizeNpcActionEntries(CloneMemorySummarySource(source.Actions), false, MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe());
            // Hash RAW state, sanitize only a detached copy for the original pipeline.
            var existing = MemoryRecordRules.SanitizeMajorActionSummaryState(CloneMemorySummarySource(source.MajorState));
            if (existing != null && !string.IsNullOrWhiteSpace(existing.LastError)) return null;
            bool hasSummary = existing != null && !string.IsNullOrWhiteSpace(existing.Summary);
            var added = hasSummary ? input.Actions.Where(x => MemoryBusinessStateOwner.IsNpcActionAfterSummaryCursor(x, existing)).ToList() : input.Actions;
            if (added.Count == 0) return null;
            DescribeMemorySummaryMajorContext(input, added);
            input.ContextFingerprint = CaptureMemorySummaryContextFingerprint(input);
            int target = MemorySummaryApplicationAdapter.GetMajorActionSummaryTargetChars(existing, hero, added);
            input.SystemPrompt = BuildMajorActionSummarySystemPrompt(target);
            input.UserPrompt = BuildMajorActionSummaryUserPrompt(hero, existing, added, target);
        }
        else if (queueJob is MemoryOverviewJob overview)
        {
            if (overview.RetryCount >= 3) return null;
            input.Job = CloneMemorySummarySource(overview);
            var all = MemoryRecordRules.SanitizeCompressedMemoryBlocks(CloneMemorySummarySource(source.Blocks));
            input.Overview = MemoryRecordRules.SanitizeMemoryOverviewState(CloneMemorySummarySource(source.Overview)) ?? new MemoryOverviewState
            { HeroId = input.HeroId, HeroName = (overview.HeroName ?? "").Trim() };
            input.OverviewBlockCount = all.Count;
            if (all.Count < LlmRequestConfigurationCaptureAdapter.GetMemoryOverviewStartBlockCountFromSettings() || !string.IsNullOrWhiteSpace(input.Overview.LastError)) return null;
            bool hasSummary = !string.IsNullOrWhiteSpace(input.Overview.Summary);
            var included = new HashSet<string>(input.Overview.IncludedBlockIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            input.Blocks = hasSummary ? all.Where(x => !MemoryBusinessStateOwner.IsMemoryBlockIncludedInOverview(x, included)).ToList() : all;
            if (input.Blocks.Count == 0) return null;
            input.ContextFingerprint = CaptureMemorySummaryContextFingerprint(input);
            int target = LlmRequestConfigurationCaptureAdapter.GetMemoryOverviewTargetCharsFromSettings();
            input.SystemPrompt = BuildMemoryOverviewSummarySystemPrompt(target);
            input.UserPrompt = BuildMemoryOverviewSummaryUserPrompt(hero, input.Overview, input.Blocks, target);
        }
        else return null;
        // Binding check: original render/getter code must not have changed raw data,
        // effective settings or the owner while these prompts were constructed.
        return IsMemorySummaryInputCurrent(input) ? input : null;
    }
	internal static string BuildMajorActionSummaryUserPrompt(Hero hero, MajorActionSummaryState existingState, List<NpcActionEntry> sourceActions, int targetChars)
	{
		string heroName = (hero?.Name?.ToString() ?? existingState?.HeroName ?? "NPC").Trim();
		if (string.IsNullOrWhiteSpace(heroName)) heroName = "NPC";
		bool hasExisting = existingState != null && !string.IsNullOrWhiteSpace(existingState.Summary);
		string existing = hasExisting ? CampaignBattleRecordCaptureAdapter.StripBattlePlayerMarker(existingState.Summary.Trim()) : null;
		var sourceLines = new List<string>();
		foreach (NpcActionEntry entry in sourceActions ?? new List<NpcActionEntry>())
			sourceLines.Add(MemorySummaryApplicationAdapter.BuildMajorActionSummarySourceLine(hero, entry));
		return MemorySummaryRules.MajorUserPrompt(heroName, hasExisting, existing, sourceLines, targetChars);
	}
	internal static string BuildMemoryOverviewSummaryUserPrompt(Hero hero, MemoryOverviewState existingState, List<CompressedMemoryBlock> sourceBlocks, int targetChars)
	{
		string heroName = (hero?.Name?.ToString() ?? existingState?.HeroName ?? "NPC").Trim();
		if (string.IsNullOrWhiteSpace(heroName)) heroName = "NPC";
		bool hasExisting = existingState != null && !string.IsNullOrWhiteSpace(existingState.Summary);
		string existing = hasExisting ? existingState.Summary.Trim() : null;
		var blockTexts = new List<string>();
		foreach (CompressedMemoryBlock block in sourceBlocks ?? new List<CompressedMemoryBlock>())
			blockTexts.Add(MemorySummaryApplicationAdapter.BuildMemoryOverviewBlockSourceText(block));
		return MemorySummaryRules.OverviewUserPrompt(heroName, hasExisting, existing, blockTexts, targetChars);
	}
}
