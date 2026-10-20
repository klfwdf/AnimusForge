using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Adapters;

namespace AnimusForge;

internal static class IntradayConversationMemoryRuntime
{
    private static readonly IntradayConversationMemory Store = new IntradayConversationMemory();
    internal static void Clear() => Store.Clear();

    // Called during the existing main-thread snapshot capture. Nothing live escapes this method.
    internal static Func<string> Capture(string id, string name, List<DailyMemoryDraft> drafts,
        MyBehavior.HistoryPromptSnapshot snapshot, string input, string secondary, int agentIndex)
    {
        ConversationContextOptions options = ConversationContextSettingsCapture.Capture();
        if (!options.Enabled) return null;
        string playerName = PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForPrompt(
            MemoryEntityIdentityBannerlordAdapter.FindHeroById(id));
        var messages = new List<ConversationMessage>();
        foreach (DailyMemoryDraft draft in drafts)
        {
            if (draft?.GameDayIndex != snapshot.GameDay || !string.Equals(draft.HeroId, id, StringComparison.OrdinalIgnoreCase)) continue;
            foreach (DailyMemoryLine line in draft.Lines ?? new List<DailyMemoryLine>())
            {
                if (line == null || line.IsAfef || !line.IsLlmDialogue || string.IsNullOrWhiteSpace(line.Text)) continue;
                messages.Add(UncompressedMemoryMessageAssemblyOwner.BuildUncompressedMemoryConversationMessage(
                    line, name, agentIndex, snapshot.GameDay, snapshot.Scene, playerName));
            }
        }
        DuelSettings settings = DuelSettings.GetSettings();
        string summarySystem = ConversationRuntimeTexts.Prompt("AF_CONV_Intraday_SummarySystem");
        string summaryHeader = ConversationRuntimeTexts.Prompt("AF_CONV_Intraday_SummaryInput");
        string contextHeader = ConversationRuntimeTexts.Prompt("AF_CONV_Intraday_Context");
        // Catalog text is part of the key; CN/EN changes never reuse another edition's summary.
        string key = snapshot.Generation + "|" + id + "|" + snapshot.GameDay + "|"
            + IntradayConversationMemory.Digest(settings?.AuxiliaryApiUrl + "|" + settings?.GetEffectiveAuxiliaryModelName() + "|" + summarySystem);
        return () =>
        {
            if (!snapshot.IsCurrent()) return "";
            bool background = !TaleWorlds.Library.TWParallel.IsMainThread();
            Func<string, string> summarize = background ? text => {
                    if (!snapshot.IsCurrent()) return null;
                    var response = ConfiguredChatApplicationAdapter.CallAuxiliaryGatewayDetailed(summarySystem,
                        summaryHeader + "\n" + text, "intraday_memory_summary", 1200, true).GetAwaiter().GetResult();
                    return response.Success ? response.Content : null;
                } : (Func<string, string>)null;
            var result = Store.Build(key, messages, (secondary + "\n" + input).Trim(), options,
                summarize, snapshot.IsCurrent, background ? text => {
                    if (!snapshot.IsCurrent()) return null;
                    try {
                        var engine = OnnxEmbeddingEngine.Instance;
                        return engine != null && engine.IsAvailable && engine.TryGetEmbedding(text, out var vector) ? vector : null;
                    } catch { return null; }
                } : (Func<string, float[]>)null);
            if (!snapshot.IsCurrent()) return "";
            var history = snapshot.UncompressedMessages ?? new List<ConversationMessage>();
            var known = new HashSet<string>(history.Where(x => !string.IsNullOrEmpty(x.PromptSourceId)).Select(x => x.PromptSourceId), StringComparer.Ordinal);
            var merged = new List<ConversationMessage>();
            foreach (var message in result.Recall)
            {
                if (!string.IsNullOrEmpty(message.PromptSourceId) && known.Contains(message.PromptSourceId)) continue;
                message.PromptIsRecall = true; merged.Add(message);
            }
            merged.AddRange(history);
            foreach (var message in merged) message.PromptContextRevision = result.Revision;
            snapshot.UncompressedMessages = merged;
            Logger.Log("IntradayMemory", "owner=" + id + " day=" + snapshot.GameDay + " source=" + messages.Count
                + " recalled=" + result.Recall.Count + " summaryChars=" + result.Context.Length);
            return result.Context.Length == 0 ? "" : contextHeader + "\n" + result.Context;
        };
    }
}
