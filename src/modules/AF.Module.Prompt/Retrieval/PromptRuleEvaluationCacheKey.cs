namespace AnimusForge;

internal static class PromptRuleEvaluationCacheKey
{
    internal static string Build(string inputKey, bool auxiliary, string excludeKey, long revision,
        bool autoExclude, bool retrievalEnabled, bool semanticFirst, int semanticTopK, int returnCap,
        string eligibilityKey, string targetKey)
    {
        return inputKey + (auxiliary ? "|aux" : "|rag") + excludeKey + "|revision=" + revision
            + "|autoExclude=" + autoExclude
            + "|options=" + retrievalEnabled + ":" + semanticFirst + ":" + semanticTopK + ":" + returnCap
            + "|eligible=" + eligibilityKey + "|target=" + targetKey;
    }
}
