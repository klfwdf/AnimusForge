using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class MyBehavior
{
    // Runtime-only input. No Hero/Agent/Campaign or live collection escapes capture.
    // Full content is checked, not just a day/count or a Save-method-only revision.
    private sealed class MemorySummaryInput
    {
        internal long Generation;
        internal object QueueJob;
        internal object Job;
        internal string HeroId;
        internal string SystemPrompt;
        internal string UserPrompt;
        internal string SourceFingerprint;
        internal string ContextFingerprint;
        internal MemorySummaryContextDependencies Context = new MemorySummaryContextDependencies();
        internal int OverviewBlockCount;
        internal DailyMemoryDraft Draft;
        internal List<NpcActionEntry> Actions;
        internal List<CompressedMemoryBlock> Blocks;
        internal MemoryOverviewState Overview;
    }

    private sealed class CapturedMemorySummaryResult
    {
        internal MemorySummaryInput Source;
        internal object Value;
        internal string Error = "";
        internal bool IsObsolete;
    }

    // Known private persistence-data models only; no game objects or polymorphic types.
    private static T CloneMemorySummarySource<T>(T value)
    {
        if (ReferenceEquals(value, null)) return default(T);
        object copy = value switch
        {
            DailyMemoryLine item => item.CopyForSummary(),
            DailyMemoryDraft item => item.CopyForSummary(),
            CompressedMemoryBlock item => item.CopyForSummary(),
            WeeklyMemoryMaterialTrigger item => item.CopyForSummary(),
            MemorySummaryJob item => item.CopyForSummary(),
            MemoryOverviewJob item => item.CopyForSummary(),
            MajorActionSummaryJob item => item.CopyForSummary(),
            MemoryOverviewState item => item.CopyForSummary(),
            MajorActionSummaryState item => item.CopyForSummary(),
            NpcActionEntry item => item.CopyForSummary(),
            List<NpcActionEntry> items => items.Select(x => x?.CopyForSummary()).ToList(),
            List<CompressedMemoryBlock> items => items.Select(x => x?.CopyForSummary()).ToList(),
            _ => throw new ArgumentException("Unsupported memory summary data model: " + typeof(T).Name)
        };
        return (T)copy;
    }

    // Only dependency descriptors survive completion: never retain full source graphs here.
    // The raw fingerprint catches edits even when sanitization renders the same text.
    private sealed class MemorySummaryContextDependencies
    {
        internal int DailySourceCharCount;
        internal int[] UnknownTextSceneDays = Array.Empty<int>();
        internal MemorySummarySceneDependency[] SceneHeader = Array.Empty<MemorySummarySceneDependency>();
        internal bool HasKnownSourceScene;
        internal string[] HeroIds = Array.Empty<string>();
        internal string[] ClanIds = Array.Empty<string>();
        internal string[] KingdomIds = Array.Empty<string>();
        internal NpcActionEntry[] SettlementLookups = Array.Empty<NpcActionEntry>();
    }

    private sealed class MemorySummarySceneDependency
    {
        internal string Scene;
        internal int UnknownDay;
    }

    private static void DescribeMemorySummaryDailyContext(MemorySummaryInput input)
    {
        var context = input.Context;
        var lines = input.Draft.Lines ?? new List<DailyMemoryLine>();
        context.DailySourceCharCount = CountDailyMemorySummarySourceChars(input.Draft);
        context.UnknownTextSceneDays = lines.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Text) && IsUnknownMemorySceneLabel(x.Scene))
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
            if (IsUnknownMemorySceneLabel(scene))
            {
                if (days.Add(line.GameDayIndex)) header.Add(new MemorySummarySceneDependency { UnknownDay = line.GameDayIndex });
            }
            else if (scenes.Add(scene)) header.Add(new MemorySummarySceneDependency { Scene = scene });
        }
        context.HasKnownSourceScene = scenes.Count > 0;
        context.SceneHeader = header.ToArray();
    }

    // Ephemeral, main-thread-only view. It must never be stored on the async input.
    private sealed class MemorySummarySourceView
    {
        internal string HeroId;
        internal object Identity;
        internal DailyMemoryDraft Draft;
        internal List<NpcActionEntry> Actions;
        internal MajorActionSummaryState MajorState;
        internal List<CompressedMemoryBlock> Blocks;
        internal MemoryOverviewState Overview;
    }

    private MemorySummarySourceView ReadMemorySummarySource(object queueJob, long generation)
    {
        if (!TWParallel.IsMainThread() || !ReferenceEquals(Instance, this)
            || !SaveRuntimeGuard.IsCurrentGeneration(generation)
            || !ReferenceEquals(Campaign.Current?.GetCampaignBehavior<MyBehavior>(), this)) return null;
        var source = new MemorySummarySourceView();
        if (queueJob is MemorySummaryJob daily)
        {
            source.HeroId = NormalizeMemoryHeroId(daily.HeroId);
            if (_memorySummaryQueue == null || !_memorySummaryQueue.Contains(daily)
                || HasCompressedMemoryBlock(source.HeroId, daily.GameDayIndex)) return null;
            source.Draft = FindMemoryDraft(daily);
            if (source.Draft == null) return null;
            source.Identity = new { Job = daily, Draft = source.Draft };
        }
        else if (queueJob is MajorActionSummaryJob major)
        {
            source.HeroId = NormalizeMemoryHeroId(major.HeroId);
            if (_npcMajorActionSummaryQueue == null || !_npcMajorActionSummaryQueue.Contains(major)
                || IsNonHeroMemoryId(source.HeroId) || _npcMajorActions == null
                || !_npcMajorActions.TryGetValue(source.HeroId, out source.Actions) || source.Actions == null) return null;
            bool statePresent = _npcMajorActionSummaries != null
                && _npcMajorActionSummaries.TryGetValue(source.HeroId, out source.MajorState);
            source.Identity = new { Job = major, Actions = source.Actions, State = source.MajorState, StatePresent = statePresent };
        }
        else if (queueJob is MemoryOverviewJob overview)
        {
            source.HeroId = NormalizeMemoryHeroId(overview.HeroId);
            if (_memoryOverviewQueue == null || !_memoryOverviewQueue.Contains(overview)
                || _compressedMemoryBlocks == null
                || !_compressedMemoryBlocks.TryGetValue(source.HeroId, out source.Blocks) || source.Blocks == null) return null;
            bool statePresent = _memoryOverviewStates != null
                && _memoryOverviewStates.TryGetValue(source.HeroId, out source.Overview);
            source.Identity = new { Job = overview, Blocks = source.Blocks, State = source.Overview, StatePresent = statePresent };
        }
        else return null;
        return IsMemoryEntityEligibleForCompressedMemory(source.HeroId) ? source : null;
    }

    private static string[] CollectMemorySummaryLookupIds(IEnumerable<string> ids)
    {
        return ids.Select(x => (x ?? "").Trim()).Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.Ordinal).ToArray();
    }

    private static void DescribeMemorySummaryMajorContext(MemorySummaryInput input, List<NpcActionEntry> added)
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

    private static object CaptureMemorySummaryDailySceneContext(MemorySummaryContextDependencies context)
    {
        int day = GetCurrentGameDayIndexSafe();
        bool headerUsesToday = context.SceneHeader.Any(x => x.Scene == null && x.UnknownDay == day);
        string scene = headerUsesToday || !context.HasKnownSourceScene ? ResolveCurrentMemorySceneLabel() : null;
        scene = IsUnknownMemorySceneLabel(scene) ? null : scene.Trim();
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

    private string CaptureMemorySummaryContextFingerprint(MemorySummaryInput input)
    {
        var hero = FindHeroById(input.HeroId);
        int trust = RewardSystemBehavior.Instance?.GetEffectiveTrust(hero) ?? 0;
        object rendering;
        if (input.Job is MemorySummaryJob)
        {
            // This query can mark observer knowledge; keep it on the owner thread,
            // before taking the shared history identity and before original Build calls.
            string observerName = BuildPlayerPublicDisplayNameForPrompt(hero);
            rendering = new
            {
                TargetChars = Math.Max(80, input.Context.DailySourceCharCount / Math.Max(1, GetMemoryCompressionDenominatorFromSettings())),
                Requirements = BuildCompressionWritingRequirementsPromptSection(DuelSettings.GetSettings()?.DailyMemoryCompressionWritingRequirements),
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
                Requirements = BuildCompressionWritingRequirementsPromptSection(DuelSettings.GetSettings()?.MajorActionCompressionWritingRequirements),
                HeroNames = input.Context.HeroIds.Select(ResolveHeroName).ToArray(),
                ClanNames = input.Context.ClanIds.Select(ResolveClanName).ToArray(),
                KingdomNames = input.Context.KingdomIds.Select(ResolveKingdomName).ToArray(),
                SettlementNames = input.Context.SettlementLookups.Select(ResolveDisplayNameBySettlementEntry).ToArray()
            };
        }
        else
        {
            rendering = new
            {
                TargetChars = GetMemoryOverviewTargetCharsFromSettings(),
                Requirements = BuildCompressionWritingRequirementsPromptSection(DuelSettings.GetSettings()?.MemoryOverviewCompressionWritingRequirements)
            };
        }
        return ComputeMemorySummaryFingerprint(new
        {
            HeroName = hero?.Name?.ToString(), Trust = trust,
            PlayerHistoryRendering = PlayerNotorietyBehavior.CaptureMemorySummaryHistoryRenderingIdentity(),
            Culture = CultureInfo.CurrentCulture.Name, Rendering = rendering
        });
    }

    private MemorySummaryInput CaptureMemorySummaryInput(object queueJob, long generation, string expectedJobFingerprint = null)
    {
        var source = ReadMemorySummarySource(queueJob, generation);
        if (source == null) return null;
        // A plan can span ticks. Its job identity and the source are checked in this
        // SAME callback, not through a separate preflight await.
        if (expectedJobFingerprint != null && !string.Equals(expectedJobFingerprint,
            ComputeMemorySummaryFingerprint(queueJob), StringComparison.Ordinal)) return null;
        var input = new MemorySummaryInput
        {
            Generation = generation, QueueJob = queueJob, HeroId = source.HeroId,
            SourceFingerprint = ComputeMemorySummaryFingerprint(source.Identity)
        };
        var hero = FindHeroById(input.HeroId);
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
            input.Actions = SanitizeNpcActionEntries(CloneMemorySummarySource(source.Actions), keepOnlyRecentWindow: false);
            // Hash RAW state, sanitize only a detached copy for the original pipeline.
            var existing = SanitizeMajorActionSummaryState(CloneMemorySummarySource(source.MajorState));
            if (existing != null && !string.IsNullOrWhiteSpace(existing.LastError)) return null;
            bool hasSummary = existing != null && !string.IsNullOrWhiteSpace(existing.Summary);
            var added = hasSummary ? input.Actions.Where(x => IsNpcActionAfterSummaryCursor(x, existing)).ToList() : input.Actions;
            if (added.Count == 0) return null;
            DescribeMemorySummaryMajorContext(input, added);
            input.ContextFingerprint = CaptureMemorySummaryContextFingerprint(input);
            int target = GetMajorActionSummaryTargetChars(existing, hero, added);
            input.SystemPrompt = BuildMajorActionSummarySystemPrompt(target);
            input.UserPrompt = BuildMajorActionSummaryUserPrompt(hero, existing, added, target);
        }
        else if (queueJob is MemoryOverviewJob overview)
        {
            if (overview.RetryCount >= 3) return null;
            input.Job = CloneMemorySummarySource(overview);
            var all = SanitizeCompressedMemoryBlocks(CloneMemorySummarySource(source.Blocks));
            input.Overview = SanitizeMemoryOverviewState(CloneMemorySummarySource(source.Overview)) ?? new MemoryOverviewState
            { HeroId = input.HeroId, HeroName = (overview.HeroName ?? "").Trim() };
            input.OverviewBlockCount = all.Count;
            if (all.Count < GetMemoryOverviewStartBlockCountFromSettings() || !string.IsNullOrWhiteSpace(input.Overview.LastError)) return null;
            bool hasSummary = !string.IsNullOrWhiteSpace(input.Overview.Summary);
            var included = new HashSet<string>(input.Overview.IncludedBlockIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            input.Blocks = hasSummary ? all.Where(x => !IsMemoryBlockIncludedInOverview(x, included)).ToList() : all;
            if (input.Blocks.Count == 0) return null;
            input.ContextFingerprint = CaptureMemorySummaryContextFingerprint(input);
            int target = GetMemoryOverviewTargetCharsFromSettings();
            input.SystemPrompt = BuildMemoryOverviewSummarySystemPrompt(target);
            input.UserPrompt = BuildMemoryOverviewSummaryUserPrompt(hero, input.Overview, input.Blocks, target);
        }
        else return null;
        // Binding check: original render/getter code must not have changed raw data,
        // effective settings or the owner while these prompts were constructed.
        return IsMemorySummaryInputCurrent(input) ? input : null;
    }

    // Stream a single framed object into the digest. Avoid serializing a source
    // to a giant string only to escape/encode that string again inside identity.
    private static string ComputeMemorySummaryFingerprint(object identity)
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

    private bool IsMemorySummaryInputCurrent(MemorySummaryInput input)
    {
        if (input == null) return false;
        var initialSource = ReadMemorySummarySource(input.QueueJob, input.Generation);
        if (initialSource == null || !string.Equals(initialSource.HeroId, input.HeroId, StringComparison.OrdinalIgnoreCase)) return false;
        // Context getters may synchronously publish knowledge. Re-read the live raw
        // view AFTER them so a replacement or edit during a getter cannot pass using
        // an orphaned pre-getter view. There is still just one full raw digest per check.
        if (!string.Equals(CaptureMemorySummaryContextFingerprint(input), input.ContextFingerprint, StringComparison.Ordinal)) return false;
        // Read dynamic eligibility before the final raw view as well: even a
        // settings getter must not publish an edit after the source digest.
        if (input.Job is MemoryOverviewJob && input.OverviewBlockCount < GetMemoryOverviewStartBlockCountFromSettings()) return false;
        var source = ReadMemorySummarySource(input.QueueJob, input.Generation);
        if (source == null || !string.Equals(ComputeMemorySummaryFingerprint(source.Identity),
            input.SourceFingerprint, StringComparison.Ordinal)) return false;
        return ReferenceEquals(Instance, this) && SaveRuntimeGuard.IsCurrentGeneration(input.Generation)
            && ReferenceEquals(Campaign.Current?.GetCampaignBehavior<MyBehavior>(), this);
    }

    private async Task<CapturedMemorySummaryResult> ExecuteCapturedMemorySummaryJobAsync(object job, int maxAttempts, string expectedJobFingerprint = null)
    {
        var result = new CapturedMemorySummaryResult();
        long generation = SaveRuntimeGuard.CaptureGeneration();
        try
        {
            bool accepted = await RunMemorySummaryMainThreadAsync(generation, delegate
            {
                result.Source = CaptureMemorySummaryInput(job, generation, expectedJobFingerprint);
                return result.Source != null;
            });
            if (!accepted) { result.IsObsolete = true; return result; }
            for (int attempt = 1; attempt <= Math.Max(1, maxAttempts); attempt++)
            {
                if (attempt > 1 && !await RunMemorySummaryMainThreadAsync(generation,
                    () => IsMemorySummaryInputCurrent(result.Source)))
                { result.IsObsolete = true; return result; }
                // The dispatcher continuation may resume after another load/owner swap.
                // Do not renew a retired request merely because its capture used to be valid.
                if (!ReferenceEquals(Instance, this) || !SaveRuntimeGuard.IsCurrentGeneration(generation))
                { result.IsObsolete = true; return result; }
                var input = result.Source;
                string area = job is MemorySummaryJob ? "CompressedMemory" : job is MajorActionSummaryJob ? "NpcMajorSummary" : "MemoryOverview";
                var api = await CallAuxiliaryGatewayDetailed(input.SystemPrompt, input.UserPrompt, area, 0, forceThinkingDisabled: true).ConfigureAwait(false);
                bool parsed = false;
                accepted = await RunMemorySummaryMainThreadAsync(generation, delegate
                {
                    if (!IsMemorySummaryInputCurrent(input)) return false;
                    if (!api.Success) { result.Error = api.ErrorMessage ?? "API请求失败"; return true; }
                    // Keep the authoritative parser (including game-derived rendering) on its owner.
                    // Parse reads detached data; the source check and parse have no intervening await.
                    var hero = FindHeroById(input.HeroId);
                    string error;
                    if (input.Job is MemorySummaryJob)
                    {
                        parsed = TryParseMemorySummaryResponse(api.Content, hero, input.Draft, out var block, out error);
                        result.Value = block;
                        if (!parsed) result.Error = BuildSummaryJsonParseFailureMessage("总结格式解析失败", error, api.Content);
                    }
                    else if (input.Job is MajorActionSummaryJob major)
                    {
                        parsed = TryParseMajorActionSummaryResponse(api.Content, hero, major, input.Actions, out var state, out error);
                        result.Value = state;
                        if (!parsed) result.Error = BuildSummaryJsonParseFailureMessage("重大履历总结格式解析失败", error, api.Content);
                    }
                    else
                    {
                        parsed = TryParseMemoryOverviewResponse(api.Content, hero, (MemoryOverviewJob)input.Job,
                            input.Overview, input.Blocks, out var state, out error);
                        result.Value = state;
                        if (!parsed) result.Error = BuildSummaryJsonParseFailureMessage("记忆总览格式解析失败", error, api.Content);
                    }
                    return true;
                });
                if (!accepted) { result.IsObsolete = true; result.Value = null; return result; }
                if (parsed) return result;
                if (attempt < maxAttempts)
                    await Task.Delay(api.RetryAfterSeconds.HasValue ? Math.Max(1000, api.RetryAfterSeconds.Value * 1000) : 1500).ConfigureAwait(false);
            }
        }
        catch (Exception ex) { result.Error = ex.Message; }
        finally
        {
            // Process may wait for other RPM waves before applying this receipt.
            // Retain only its identity/fingerprint, not every completed job's full
            // draft, rendered prompt and source blocks until the whole queue ends.
            if (result.Source != null)
            {
                result.Source.Draft = null;
                result.Source.Actions = null;
                result.Source.Blocks = null;
                result.Source.Overview = null;
                result.Source.SystemPrompt = null;
                result.Source.UserPrompt = null;
            }
        }
        return result;
    }
}
