using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

internal static class ConversationRequestBudget
{
    // Explicit estimate, NOT a provider tokenizer or a promise about a model's capacity.
    internal static long Estimate(IEnumerable<PromptCacheMessage> messages)
    {
        long units = 0, count = 0;
        foreach (var message in messages)
        {
            count++;
            foreach (char c in message.Content) units += c <= 127 ? 1 : 6;
        }
        return (units + 3) / 4 + count * 8 + 256;
    }
    internal static long Characters(IEnumerable<PromptCacheMessage> messages) => messages.Sum(x => (long)x.Content.Length);
    internal static bool Fits(IEnumerable<PromptCacheMessage> messages, int chars, int totalTokens, int outputTokens)
        => Characters(messages) <= chars && Estimate(messages) + Math.Max(0, outputTokens) <= totalTokens;

    internal static List<PromptCacheMessage> Prepare(List<PromptCacheMessage> original, int chars, int totalTokens, int outputTokens)
    {
        var result = new List<PromptCacheMessage>(original);
        // Only whole optional historical turns can leave a request. Fixed rules, current facts,
        // and the current input are never trimmed, rewritten, or promoted to executed facts.
        // Targets do not reject required-only requests: the caller preserves that floor and
        // continues, while the bounded cache can fall back to this freshly prepared request.
        foreach (int group in result.Where(x => x.HistoryGroup > 0).Select(x => x.HistoryGroup).Distinct().ToArray())
        {
            if (Fits(result, chars, totalTokens, outputTokens)) break;
            result.RemoveAll(x => x.HistoryGroup == group);
        }
        return result;
    }
}
