using System;
using System.Collections.Generic;

namespace AnimusForge;

internal sealed class PromptRuleIntentInputBatch
{
    internal readonly List<PromptRuleRecallIntent> Intents = new List<PromptRuleRecallIntent>();
    internal readonly List<string> Texts = new List<string>();

    internal static PromptRuleIntentInputBatch Collect(string userText, string secondaryText, Func<string, float[]> embed)
    {
        var batch = new PromptRuleIntentInputBatch();
        batch.Append(PromptRuleIntentSplitter.Split(userText, IntentQueryOptimizer.MaxIntentCountPerSpeaker),
            IntentQueryOptimizer.MaxIntentCountPerSpeaker, 1f, embed);
        string secondary = Normalize(secondaryText);
        if (secondary.Length > 0 && !string.Equals(secondary, Normalize(userText), StringComparison.Ordinal))
            batch.Append(PromptRuleIntentSplitter.Split(secondary, IntentQueryOptimizer.MaxIntentCountPerSpeaker),
                IntentQueryOptimizer.MaxIntentCountPerSpeaker, 1f, embed);
        return batch;
    }

    private void Append(List<string> intents, int perSourceLimit, float weight, Func<string, float[]> embed)
    {
        if (intents == null || intents.Count == 0 || weight <= 0f || perSourceLimit <= 0) return;
        int accepted = 0;
        foreach (string raw in intents)
        {
            if (Intents.Count >= IntentQueryOptimizer.MaxCombinedIntentCount) break;
            string text = Normalize(raw);
            if (text.Length == 0) continue;
            float[] vector = embed(text);
            if (vector == null || vector.Length == 0) continue;
            accepted++;
            if (accepted > perSourceLimit) break;
            Texts.Add(text);
            Intents.Add(new PromptRuleRecallIntent(text, vector, weight));
        }
    }

    private static string Normalize(string value)
    {
        string text = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(text) ? "" : text.Replace("\r", " ").Replace("\n", " ").Trim();
    }
}
