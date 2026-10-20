// Imported from AnimusForge.PromptCache/Core/DynamicBlockDeduplicator.cs. Host adaptations: see import receipt.
using System;
using System.Collections.Generic;
using System.Text;

namespace AnimusForge;

/// <summary>
/// 一个会话“模型已经见过的动态状态行”集合。用于顺序无关的去重与差分。
/// </summary>
internal sealed class DynamicBlockState
{
    public List<string> KnownLines { get; } = new List<string>();
    public HashSet<string> KnownSet { get; } = new HashSet<string>(StringComparer.Ordinal);

    public void Replace(IEnumerable<string> lines)
    {
        KnownLines.Clear();
        KnownSet.Clear();
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (KnownSet.Add(line))
                KnownLines.Add(line);
        }
    }
}

/// <summary>
/// 动态状态块的“按集合去重 + 按行差分”。
///
/// - 比较时按行做集合运算，因此即使 rerank 把同样的知识换了顺序，也能识别为“没变化”；
/// - 发送时保持原顺序，只发出“模型没见过的行”，并附上“本轮失效”的行；
/// - 变化行超过 60%、或差分本身超过字符上限时，退回整块发送；
/// - 截断提示行不会写入已知集合。
///
/// 支持一个会话内的多个独立状态（例如“场景卡”“场景规则”“1 对 1 状态”）。
/// </summary>
internal static class DynamicBlockDeduplicator
{
    private static string DefaultTruncateHint => PromptCacheCoreTexts.Get("Truncated");
    private static string NoChangeText => PromptCacheCoreTexts.Get("NoChange");

    /// <summary>无变化时返回的固定文案，供调用方判断。</summary>
    public static string NoChangeMarker => NoChangeText;

    private static readonly System.Text.RegularExpressions.Regex SectionHeaderRegex =
        new System.Text.RegularExpressions.Regex(@"(?m)^(?=(?:【[^】]{2,80}】|\[[^\]\n]{2,100}\]))", System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    /// 按【段标题】把动态块拆开、每段独立去重。
    /// 这样“家族事实/可婚配名单/周报/履历”等段各自只在变化时发送，
    /// 不会因为某一段大改就整包 30k 全部重发。
    /// </summary>
    public static string ApplySections(ConversationSessionCache session, string block, string tagPrefix)
    {
        if (session == null)
            return block;

        string trimmedBlock = (block ?? "").Replace("\r", "").Trim();
        string[] sections = SectionHeaderRegex.Split(trimmedBlock);
        bool blockEmpty = string.IsNullOrWhiteSpace(trimmedBlock);
        if (sections.Length <= 1)
            return Apply(session, block, 0, tagPrefix, emitRemovals: !blockEmpty);

        // Failure/expiry of an owned dynamic block must still emit removals and
        // clear state; otherwise the model keeps stale lines it can no longer see.
        if (blockEmpty)
            return ExpireAll(session, tagPrefix);

        List<string> parts = new List<string>();
        int changed = 0;
        int index = 0;

        // 段键必须稳定：用“标题 + 该标题第几次出现”，而不是全局序号。
        // 否则前面某段消失时，后面所有段的序号都会平移，导致它们被当成新段整段重发。
        Dictionary<string, int> occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
        HashSet<string> seenTags = new HashSet<string>(StringComparer.Ordinal);
        int deltaChars = 0;

        foreach (string rawSection in sections)
        {
            string section = (rawSection ?? "").Trim();
            if (section.Length == 0)
                continue;

            index++;
            string headerLine = section.Split('\n')[0];
            bool hasHeader = (headerLine.StartsWith("【", StringComparison.Ordinal) || headerLine.StartsWith("[", StringComparison.Ordinal));
            string keyHeader = hasHeader
                ? headerLine
                : "preamble";

            occurrences.TryGetValue(keyHeader, out int occurrence);
            occurrences[keyHeader] = occurrence + 1;

            string tag = tagPrefix + ":" + occurrence + ":" + keyHeader;
            seenTags.Add(tag);

            string delta = Apply(session, session.GetSectionState(tag), section, 0, tag, emitRemovals: true);
            if (delta == NoChangeText)
                continue;

            parts.Add(delta);
            deltaChars += delta.Length;
            changed++;
        }

        // Sections owned by this tagPrefix that disappeared this round: emit the
        // previously sent lines as removed and clear their state.
        List<string> vanished = CollectVanishedSections(session, tagPrefix, seenTags);
        if (vanished.Count > 0)
        {
            StringBuilder removals = new StringBuilder();
            removals.Append(PromptCacheCoreTexts.Get("Removed"));
            foreach (string line in vanished)
                removals.Append(line).Append('\n');
            parts.Add(removals.ToString().Trim());
            deltaChars += removals.Length;
            changed++;
        }

        if (parts.Count == 0)
            return NoChangeText;

        string result = string.Join("\n", parts);
        int originalLength = block.Length;
        PromptCacheLogger.Log(
            PromptCacheCoreTexts.Get("SectionSummary")
                .Replace("{0}", index.ToString())
                .Replace("{1}", changed.ToString())
                .Replace("{2}", originalLength.ToString())
                .Replace("{3}", result.Length.ToString()));

        return result;
    }

    /// <summary>
    /// Empty owned block: every previously sent line under this tagPrefix is
    /// reported removed and all section states are cleared.
    /// </summary>
    public static string ExpireAll(ConversationSessionCache session, string tagPrefix)
    {
        List<string> vanished = CollectVanishedSections(session, tagPrefix, null);
        List<string> globalRemoved = new List<string>(session.DynamicState.KnownLines);
        if (globalRemoved.Count > 0)
        {
            UpdateState(session, session.DynamicState, new List<string>());
            vanished.AddRange(globalRemoved);
        }

        if (vanished.Count == 0)
            return NoChangeText;

        StringBuilder removals = new StringBuilder();
        removals.Append(PromptCacheCoreTexts.Get("Removed"));
        foreach (string line in vanished)
            removals.Append(line).Append('\n');
        return removals.ToString().Trim();
    }

    private static List<string> CollectVanishedSections(
        ConversationSessionCache session, string tagPrefix, HashSet<string> seenTags)
    {
        List<string> removed = new List<string>();
        List<string> expiredKeys = new List<string>();
        lock (session.Sync)
        {
            foreach (KeyValuePair<string, DynamicBlockState> pair in session.EnumerateSectionStatesSnapshot())
            {
                if (!pair.Key.StartsWith(tagPrefix + ":", StringComparison.Ordinal))
                    continue;
                if (seenTags != null && seenTags.Contains(pair.Key))
                    continue;
                if (pair.Value.KnownLines.Count == 0)
                    continue;
                removed.AddRange(pair.Value.KnownLines);
                expiredKeys.Add(pair.Key);
            }

            foreach (string key in expiredKeys)
                session.GetSectionState(key).Replace(Array.Empty<string>());
        }

        return removed;
    }

    public static string Apply(
        ConversationSessionCache session,
        string block,
        int maxChars,
        string tag,
        string truncateHint = null,
        bool emitRemovals = true)
    {
        return Apply(session, session?.DynamicState, block, maxChars, tag, truncateHint, emitRemovals);
    }

    public static string Apply(
        ConversationSessionCache session,
        DynamicBlockState state,
        string block,
        int maxChars,
        string tag,
        string truncateHint = null,
        bool emitRemovals = true)
    {
        if (state == null || string.IsNullOrWhiteSpace(block))
            return block;

        truncateHint = string.IsNullOrWhiteSpace(truncateHint) ? DefaultTruncateHint : truncateHint;

        // 诊断：按需转储未经截断的完整动态块
        int dumpLeft = PromptCacheSettings.Instance.DumpRawDynamicBlockCount;
        if (dumpLeft > 0)
        {
            PromptCacheSettings.Instance.DumpRawDynamicBlockCount = dumpLeft - 1;
            CacheStatsLogger.DumpRawBlock(tag, block);
            PromptCacheLogger.Log($"[Dump] Raw dynamic block saved: tag={tag}, chars={block.Length}, remaining={dumpLeft - 1}.");
        }

        int originalLength = block.Length;
        List<string> lines = SplitLines(block);

        if (!PromptCacheSettings.Instance.EnableDynamicBlockDedup || state.KnownLines.Count == 0)
        {
            string sent = TruncateByLines(lines, maxChars, truncateHint, out List<string> stored);
            UpdateState(session, state, stored);
            PromptCacheLogger.LogVerbose($"[Dedup] {tag} initial or disabled: original={originalLength}, sent={sent.Length} chars.");
            return sent;
        }

        HashSet<string> newSet = new HashSet<string>(lines, StringComparer.Ordinal);

        List<string> added = new List<string>();
        HashSet<string> addedSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (string line in lines)
        {
            if (!state.KnownSet.Contains(line) && addedSet.Add(line))
                added.Add(line);
        }

        // 追加型知识：不标记“失效”，只追加模型没见过的条目。
        // 未检索到 ≠ 这条知识变假了；标记失效会让模型忘掉本来正确的背景。
        if (!emitRemovals)
        {
            if (added.Count == 0)
            {
                PromptCacheLogger.LogVerbose($"[Dedup] {tag} no additions: original={originalLength} chars; nothing appended.");
                return NoChangeText;
            }

            StringBuilder addOnly = new StringBuilder();
            addOnly.Append(PromptCacheCoreTexts.Get("NewKnowledge"));
            foreach (string line in added)
                addOnly.Append(line).Append('\n');

            string addText = addOnly.ToString().Trim();
            if (maxChars > 0 && addText.Length > maxChars)
            {
                string fullBlock = TruncateByLines(lines, maxChars, truncateHint, out List<string> storedFull);
                UpdateState(session, state, storedFull);
                PromptCacheLogger.Log($"[Dedup] {tag} additions={addText.Length} exceed limit={maxChars}; sending full block {fullBlock.Length}/{originalLength} chars.");
                return fullBlock;
            }

            // 已知集合只增不减：模型已经见过的知识条目，后面再检索到也不再重复发送
            List<string> union = new List<string>(state.KnownLines);
            HashSet<string> unionSet = new HashSet<string>(state.KnownSet, StringComparer.Ordinal);
            foreach (string line in added)
            {
                if (unionSet.Add(line))
                    union.Add(line);
            }

            UpdateState(session, state, union);
            PromptCacheLogger.Log($"[Dedup] {tag} original={originalLength}, additions={addText.Length} chars ({added.Count} entries).");
            return addText;
        }

        List<string> removed = new List<string>();
        foreach (string line in state.KnownLines)
        {
            if (!newSet.Contains(line))
                removed.Add(line);
        }

        if (added.Count == 0 && removed.Count == 0)
        {
            PromptCacheLogger.LogVerbose($"[Dedup] {tag} unchanged: original={originalLength} chars; nothing appended.");
            return NoChangeText;
        }

        double changeRatio = (double)(added.Count + removed.Count) / Math.Max(1, lines.Count);

        StringBuilder diff = new StringBuilder();
        if (added.Count > 0)
        {
            diff.Append(PromptCacheCoreTexts.Get("Added"));
            foreach (string line in added)
                diff.Append(line).Append('\n');
        }

        if (removed.Count > 0)
        {
            diff.Append(PromptCacheCoreTexts.Get("Removed"));
            foreach (string line in removed)
                diff.Append(line).Append('\n');
        }

        string diffText = diff.ToString().Trim();

        if (changeRatio > 0.6 || (maxChars > 0 && diffText.Length > maxChars))
        {
            string full = TruncateByLines(lines, maxChars, truncateHint, out List<string> stored);
            UpdateState(session, state, stored);

            string reason = changeRatio > 0.6
                ? $"change ratio {changeRatio:P0} exceeds 60%"
                : $"delta {diffText.Length} chars exceeds limit {maxChars}";

            PromptCacheLogger.Log($"[Dedup] {tag} {reason}; sending full block {full.Length}/{originalLength} chars.");
            return full;
        }

        UpdateState(session, state, lines);
        PromptCacheLogger.Log(
            $"[Dedup] {tag} original={originalLength}, delta={diffText.Length} chars (added={added.Count}, removed={removed.Count} lines).");

        return diffText;
    }

    private static void UpdateState(ConversationSessionCache session, DynamicBlockState state, List<string> lines)
    {
        if (session == null)
        {
            state.Replace(lines);
            return;
        }

        lock (session.Sync)
        {
            state.Replace(lines);
        }
    }

    /// <summary>
    /// 按行拆分并去重（保留首次出现顺序）。
    /// </summary>
    public static List<string> SplitLines(string text)
    {
        List<string> lines = new List<string>();
        if (string.IsNullOrEmpty(text))
            return lines;

        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        foreach (string raw in normalized.Split('\n'))
        {
            string trimmed = raw.Trim();
            if (trimmed.Length > 0 && seen.Add(trimmed))
                lines.Add(trimmed);
        }

        return lines;
    }

    /// <summary>
    /// 按行截断，stored 只包含实际发出的完整行。
    /// </summary>
    public static string TruncateByLines(List<string> lines, int maxChars, string truncateHint, out List<string> stored)
    {
        stored = new List<string>();

        if (lines == null || lines.Count == 0)
            return string.Empty;

        if (maxChars <= 0)
        {
            stored.AddRange(lines);
            return string.Join("\n", lines);
        }

        string hint = string.IsNullOrWhiteSpace(truncateHint) ? DefaultTruncateHint : truncateHint;
        StringBuilder sb = new StringBuilder();
        foreach (string line in lines)
        {
            if (sb.Length + line.Length + 1 > maxChars)
            {
                if (sb.Length == 0)
                {
                    int keep = maxChars - hint.Length - 1;
                    if (keep > 0)
                    {
                        sb.Append(line.Substring(0, Math.Min(keep, line.Length)));
                        stored.Add(sb.ToString());
                    }
                }

                if (sb.Length > 0)
                    sb.Append('\n');

                sb.Append(hint);
                return sb.ToString();
            }

            if (sb.Length > 0)
                sb.Append('\n');

            sb.Append(line);
            stored.Add(line);
        }

        return sb.ToString();
    }
}
