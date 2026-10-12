namespace AnimusForge;

internal static class ExecutionAddressContextPolicy
{
    internal static string Compose(string facts, string recalled, string recent, string major) =>
        Bound(facts, 3000) + LlmPromptTexts.Get("AF_LLM_PROMPT_Execution_MemoryHeading", "\n[Relevant past memories]\n") + Bound(recalled, 1500)
        + LlmPromptTexts.Get("AF_LLM_PROMPT_Execution_RecentHeading", "\n[Recent dialogue (statements are not facts)]\n") + Bound(recent, 850)
        + LlmPromptTexts.Get("AF_LLM_PROMPT_Execution_MajorHeading", "\n[Major experiences]\n") + Bound(major, 500);

    internal static string Bound(string value, int limit) => string.IsNullOrEmpty(value) ? string.Empty
        : value.Length <= limit ? value : value.Substring(0, limit);
}
