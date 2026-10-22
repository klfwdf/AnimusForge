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

    // Final targets only govern diagnostics. The shared
    // recent-window / intraday-summary pipeline has already selected this request's history;
    // exceeding a final target must not discard any of those selected turns.
}
