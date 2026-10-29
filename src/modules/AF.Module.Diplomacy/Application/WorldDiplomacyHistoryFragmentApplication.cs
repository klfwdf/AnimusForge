using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal static class WorldDiplomacyHistoryFragmentApplication
{
    private const int MaximumPasses = 3, MaximumPieces = 256;
    private sealed class HashMemo { internal string Value; }
    private static readonly ConditionalWeakTable<string, HashMemo> Hashes = new();
    private static string Hash(string text) => Hashes.GetValue(text ?? "", x => new HashMemo {
        Value = WorldDiplomacyPromptContractRules.StablePromptHash(x) }).Value;
    private static void Hold(WorldDiplomacyStorage storage, string reason)
    {
        storage.CompressionRetryAttempts = DiplomacyCompressionBudget.MaximumFailures;
        storage.CompressionRetryAfterHour = int.MaxValue;
        storage.CompressionHoldReason = reason;
        storage.DiplomacyCompressionPending = true;
    }
    private static string Original(WorldDiplomacyStorage storage, DiplomacyHistoryFragmentWork work)
        => work.Snapshot ? (storage.CanonicalHistory.Snapshot.CoveredThroughSequence == work.Sequence ? storage.CanonicalHistory.Snapshot.Content : null)
            : storage.CanonicalHistory.DeltaEntries.FirstOrDefault(x => x.EntryId == work.EntryId && x.Sequence == work.Sequence)?.Text;

    // True means a giant-source workflow owns this scheduling attempt (including a retained hold).
    internal static bool TryQueue(WorldDiplomacyStorage storage, string system, long inputBudget, long throughSequence,
        Func<string, int> estimate, Func<string, string> newId, int day, int priority, int maximumJobs, int outputLimit,
        Func<string, string, string> localize)
    {
        var history = storage.CanonicalHistory;
        int Count(string text) => Math.Max(0, estimate(text ?? ""));
        var work = storage.HistoryFragmentWork;
        if (work == null)
        {
            bool snapshot = history.Snapshot.EstimatedTokens > inputBudget && Count(history.Snapshot.Content) > inputBudget / 2;
            var entry = history.DeltaEntries.FirstOrDefault(x => x != null && x.Sequence > history.Snapshot.CoveredThroughSequence && x.Sequence <= throughSequence);
            bool oversizedEntry = entry != null && entry.EstimatedTokens > inputBudget - history.Snapshot.EstimatedTokens
                && Count(entry.Text) > inputBudget / 2;
            if (!snapshot && !oversizedEntry) return false;
            string original = snapshot ? history.Snapshot.Content : entry.Text;
            work = storage.HistoryFragmentWork = new DiplomacyHistoryFragmentWork {
                Id = newId("history_fragment"), EntryId = snapshot ? "" : entry.EntryId,
                Sequence = snapshot ? history.Snapshot.CoveredThroughSequence : entry.Sequence,
                Snapshot = snapshot, OriginalHash = Hash(original), SourceText = original, SourceHash = Hash(original),
                SourceKind = snapshot ? "previous_summary" : entry.Kind, SourceVerified = !snapshot && entry.Verified,
                TargetTokens = (int)Math.Max(256, Math.Min(12000, inputBudget / 3)) };
        }
        if (Original(storage, work) == null || Hash(Original(storage, work)) != work.OriginalHash)
        { Hold(storage, "fragment_original_source_changed"); return true; }
        if (Hash(work.SourceText) != work.SourceHash || work.Offset < 0 || work.Offset >= work.SourceText.Length
            || work.PendingLength < 0 || work.PendingLength > work.SourceText.Length - work.Offset)
        { Hold(storage, "fragment_progress_invalid"); return true; }
        work.Pieces ??= new();
        if (work.Pieces.Count >= MaximumPieces || work.Pass >= MaximumPasses)
        { Hold(storage, "fragment_work_limit"); return true; }
        bool audit = !string.IsNullOrWhiteSpace(work.PendingSummary);
        string rule = WorldDiplomacyPromptContractRules.ContractText(localize, audit ? "HistoryFragment_Audit" : "HistoryFragment_Write", "");
        int available = (int)Math.Max(0, Math.Min(int.MaxValue, inputBudget - Count(rule) - 2048));
        if (available < 256) { Hold(storage, "fragment_contract_cannot_fit"); return true; }
        work.TargetTokens = Math.Min(work.TargetTokens, Math.Max(256, available / 2));
        // A provider-capacity reduction can invalidate a pending large audit, but never its original source.
        if (audit && Count(work.SourceText.Substring(work.Offset, work.PendingLength)) + Count(work.PendingSummary) > available)
        {
            work.PendingSummary = ""; work.PendingLength = 0; audit = false;
            rule = WorldDiplomacyPromptContractRules.ContractText(localize, "HistoryFragment_Write", "");
            available = (int)Math.Max(0, Math.Min(int.MaxValue, inputBudget - Count(rule) - 2048));
        }
        if (!audit)
        {
            int low = 1, high = (int)Math.Min(work.SourceText.Length - work.Offset, (long)available * 8), length = 0;
            while (low <= high)
            {
                int middle = low + (high - low) / 2;
                if (Count(work.SourceText.Substring(work.Offset, middle)) <= available) { length = middle; low = middle + 1; }
                else high = middle - 1;
            }
            if (length > 0 && work.Offset + length < work.SourceText.Length && char.IsHighSurrogate(work.SourceText[work.Offset + length - 1])) length--;
            if (length <= 0) { Hold(storage, "fragment_cannot_advance"); return true; }
            int newline = work.SourceText.LastIndexOf('\n', work.Offset + length - 1, length);
            if (newline > work.Offset + length * 3 / 4) length = newline + 1 - work.Offset;
            work.PendingLength = length;
        }
        int beforeStart = Math.Max(0, work.Offset - 128), afterStart = work.Offset + work.PendingLength;
        int target = Math.Max(64, Math.Min(512, Math.Min(available / 8, Math.Max(64, outputLimit - 512))));
        var job = new WorldDiplomacyJob {
            JobId = newId("diplomacy_fragment"), Kind = "compress", Priority = priority, CreatedDay = day,
            CompressionBatchId = work.Id, CompressionFragmentId = work.Id, CompressionFragmentHash = work.SourceHash,
            CompressionFragmentOffset = work.Offset, CompressionFragmentLength = work.PendingLength, CompressionFragmentAudit = audit, CompressionFragmentDraftHash = audit ? Hash(work.PendingSummary) : "",
            CompressionTargetTokens = target, CompressionConfigurationFingerprint = storage.CompressionConfigurationFingerprint,
            SystemPrompt = system, MaxTokens = Math.Min(outputLimit, audit ? 512 : target + 512),
            CacheAffinityKey = WorldDiplomacyPromptContractRules.CanonicalHistoryCacheAffinityKey,
            UserPrompt = "【MODE=COMPACT】\n" + rule + "\n" + JsonConvert.SerializeObject(new {
                source_hash = work.SourceHash, offset = work.Offset, length = work.PendingLength,
                source_kind = work.SourceKind, source_record_verified = work.SourceVerified,
                is_last = afterStart == work.SourceText.Length, summary_token_limit = target,
                context_before = work.SourceText.Substring(beforeStart, work.Offset - beforeStart),
                fragment = work.SourceText.Substring(work.Offset, work.PendingLength),
                context_after = work.SourceText.Substring(afterStart, Math.Min(128, work.SourceText.Length - afterStart)),
                proposed_summary = audit ? work.PendingSummary : "" }) };
        WorldDiplomacyRoundLifecycleRules.EnqueueJob(storage, job, maximumJobs);
        storage.DiplomacyCompressionPending = true;
        return true;
    }

    internal static bool TryCommit(WorldDiplomacyStorage storage, WorldDiplomacyJob job, string raw,
        Func<string, int> estimate, long trigger, Action invalidate)
    {
        if (string.IsNullOrEmpty(job.CompressionFragmentId)) return false;
        var work = storage.HistoryFragmentWork;
        if (work == null || work.Id != job.CompressionFragmentId) return true;
        if (work.SourceHash != job.CompressionFragmentHash || job.CompressionFragmentOffset < work.Offset) return true;
        if (Hash(work.SourceText) != work.SourceHash || work.Offset < 0 || work.PendingLength <= 0
            || work.PendingLength > work.SourceText.Length - work.Offset)
        { Hold(storage, "fragment_progress_invalid"); return true; }
        string original = Original(storage, work);
        if (original == null || Hash(original) != work.OriginalHash) { Hold(storage, "fragment_original_source_changed"); return true; }
        JObject json = WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(raw);
        if (json.Value<string>("source_hash") != work.SourceHash || json.Value<int?>("offset") != work.Offset
            || json.Value<int?>("length") != work.PendingLength || work.Offset != job.CompressionFragmentOffset
            || work.PendingLength != job.CompressionFragmentLength) throw new InvalidOperationException("fragment_source_range_mismatch");
        if (!job.CompressionFragmentAudit)
        {
            string summary = json.Value<string>("summary")?.Trim();
            if (string.IsNullOrEmpty(summary) || estimate(summary) > job.CompressionTargetTokens) throw new InvalidOperationException("fragment_summary_invalid");
            work.PendingSummary = summary;
            return true;
        }
        if (Hash(work.PendingSummary) != job.CompressionFragmentDraftHash) throw new InvalidOperationException("fragment_audit_draft_changed");
        if (string.IsNullOrWhiteSpace(work.PendingSummary)) throw new InvalidOperationException("fragment_audit_missing_draft");
        if (json["approved"]?.Type != JTokenType.Boolean || !json["approved"].Value<bool>())
        { work.PendingSummary = ""; throw new InvalidOperationException("fragment_audit_rejected"); }
        work.Pieces.Add(new() { Offset = work.Offset, Length = work.PendingLength, Summary = work.PendingSummary });
        work.Offset += work.PendingLength; work.PendingLength = 0; work.PendingSummary = "";
        DiplomacyCompressionBudget.Resume(storage, false);
        if (work.Offset < work.SourceText.Length) return true;
        int covered = 0;
        foreach (var part in work.Pieces)
        {
            if (part.Offset != covered || part.Length <= 0 || string.IsNullOrWhiteSpace(part.Summary)) { Hold(storage, "fragment_incomplete_coverage"); return true; }
            covered += part.Length;
        }
        if (covered != work.SourceText.Length) { Hold(storage, "fragment_incomplete_coverage"); return true; }
        string joined = string.Join("\n", work.Pieces.Select(x => x.Summary));
        if (estimate(joined) >= estimate(work.SourceText))
        {
            work.Offset = 0; work.Pieces.Clear(); Hold(storage, "fragment_did_not_shrink"); return true;
        }
        if (estimate(joined) > work.TargetTokens)
        {
            work.SourceText = joined; work.SourceHash = Hash(joined); work.Offset = 0; work.Pass++; work.Pieces.Clear();
            if (work.Pass >= MaximumPasses) Hold(storage, "fragment_merge_limit");
            return true;
        }
        if (work.Snapshot)
        {
            var snapshot = storage.CanonicalHistory.Snapshot;
            snapshot.Content = joined;
            string payload = WorldDiplomacyCanonicalRenderRules.RenderCanonicalSnapshotPayload(snapshot);
            snapshot.ContentHash = Hash(payload); snapshot.EstimatedTokens = estimate(payload);
        }
        else
        {
            var entry = storage.CanonicalHistory.DeltaEntries.Single(x => x.EntryId == work.EntryId && x.Sequence == work.Sequence);
            entry.Text = joined; entry.EstimatedTokens = estimate(WorldDiplomacyCanonicalRenderRules.RenderCanonicalHistoryEntry(entry));
        }
        storage.HistoryFragmentWork = null;
        storage.CanonicalHistory.Revision++;
        WorldDiplomacyRoundLifecycleRules.RecalculateCanonicalHistoryTokens(storage, trigger);
        invalidate();
        return true;
    }
}
