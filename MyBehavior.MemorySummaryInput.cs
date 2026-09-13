using System;
using System.Collections.Generic;
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

    private MemorySummaryInput CaptureMemorySummaryInput(object queueJob, long generation)
    {
        if (!TWParallel.IsMainThread() || !ReferenceEquals(Instance, this)
            || !SaveRuntimeGuard.IsCurrentGeneration(generation)
            || !ReferenceEquals(Campaign.Current?.GetCampaignBehavior<MyBehavior>(), this)) return null;

        var input = new MemorySummaryInput { Generation = generation, QueueJob = queueJob };
        object sourceData;
        if (queueJob is MemorySummaryJob daily)
        {
            if (_memorySummaryQueue == null || !_memorySummaryQueue.Contains(daily)
                || !HasMemorySummaryJobStillPending(daily)) return null;
            var source = FindMemoryDraft(daily);
            sourceData = new { Job = daily, Draft = source };
            input.Job = CloneMemorySummarySource(daily);
            input.Draft = CloneMemorySummarySource(source);
            input.HeroId = NormalizeMemoryHeroId(daily.HeroId);
            var hero = FindHeroById(input.HeroId);
            input.SystemPrompt = BuildMemorySummarySystemPrompt(input.Draft);
            input.UserPrompt = BuildMemorySummaryUserPrompt(hero, input.Draft);
        }
        else if (queueJob is MajorActionSummaryJob major)
        {
            if (_npcMajorActionSummaryQueue == null || !_npcMajorActionSummaryQueue.Contains(major)
                || !HasMajorActionSummaryJobStillPending(major)) return null;
            input.HeroId = NormalizeMemoryHeroId(major.HeroId);
            _npcMajorActions.TryGetValue(input.HeroId, out var actions);
            var state = GetMajorActionSummaryState(input.HeroId);
            sourceData = new { Job = major, Actions = actions, State = state };
            input.Job = CloneMemorySummarySource(major);
            // The existing sanitizers may repair their arguments. Only detached copies enter them.
            input.Actions = SanitizeNpcActionEntries(CloneMemorySummarySource(actions), keepOnlyRecentWindow: false);
            var existing = CloneMemorySummarySource(state);
            bool hasSummary = existing != null && !string.IsNullOrWhiteSpace(existing.Summary);
            var added = hasSummary ? input.Actions.Where(x => IsNpcActionAfterSummaryCursor(x, existing)).ToList() : input.Actions;
            if (added.Count == 0) return null;
            var hero = FindHeroById(input.HeroId);
            int target = GetMajorActionSummaryTargetChars(existing, hero, added);
            input.SystemPrompt = BuildMajorActionSummarySystemPrompt(target);
            input.UserPrompt = BuildMajorActionSummaryUserPrompt(hero, existing, added, target);
        }
        else if (queueJob is MemoryOverviewJob overview)
        {
            if (_memoryOverviewQueue == null || !_memoryOverviewQueue.Contains(overview)
                || !HasMemoryOverviewJobStillPending(overview)) return null;
            input.HeroId = NormalizeMemoryHeroId(overview.HeroId);
            _compressedMemoryBlocks.TryGetValue(input.HeroId, out var blocks);
            var state = GetMemoryOverviewState(input.HeroId);
            sourceData = new { Job = overview, Blocks = blocks, State = state };
            input.Job = CloneMemorySummarySource(overview);
            var all = SanitizeCompressedMemoryBlocks(CloneMemorySummarySource(blocks));
            input.Overview = CloneMemorySummarySource(state) ?? new MemoryOverviewState
            { HeroId = input.HeroId, HeroName = (overview.HeroName ?? "").Trim() };
            bool hasSummary = !string.IsNullOrWhiteSpace(input.Overview.Summary);
            var included = new HashSet<string>(input.Overview.IncludedBlockIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            input.Blocks = hasSummary ? all.Where(x => !IsMemoryBlockIncludedInOverview(x, included)).ToList() : all;
            if (input.Blocks.Count == 0) return null;
            int target = GetMemoryOverviewTargetCharsFromSettings();
            input.SystemPrompt = BuildMemoryOverviewSummarySystemPrompt(target);
            input.UserPrompt = BuildMemoryOverviewSummaryUserPrompt(FindHeroById(input.HeroId), input.Overview, input.Blocks, target);
        }
        else return null;

        // Prompt text includes the effective writing requirements, metadata and rendered facts.
        // The extra fields are parse-time dependencies (including otherwise invisible identity changes).
        var currentHero = FindHeroById(input.HeroId);
        object identity = new
        {
            HeroName = currentHero?.Name?.ToString(),
            Trust = RewardSystemBehavior.Instance?.GetEffectiveTrust(currentHero) ?? 0,
            PlayerHistoryRendering = PlayerNotorietyBehavior.CaptureMemorySummaryHistoryRenderingIdentity(),
            input.SystemPrompt, input.UserPrompt, Source = sourceData
        };
        input.SourceFingerprint = ComputeMemorySummaryFingerprint(identity);
        return input;
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
        var current = CaptureMemorySummaryInput(input.QueueJob, input.Generation);
        return current != null && string.Equals(current.SourceFingerprint, input.SourceFingerprint, StringComparison.Ordinal);
    }

    private async Task<CapturedMemorySummaryResult> ExecuteCapturedMemorySummaryJobAsync(object job, int maxAttempts)
    {
        var result = new CapturedMemorySummaryResult();
        long generation = SaveRuntimeGuard.CaptureGeneration();
        try
        {
            bool accepted = await RunMemorySummaryMainThreadAsync(generation, delegate
            {
                result.Source = CaptureMemorySummaryInput(job, generation);
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
