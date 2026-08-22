namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Host-neutral request prepared for the AF auxiliary language-model call.
/// </summary>
public sealed class SettlementRuleMemoryGenerationPrompt
{
    public SettlementRuleMemoryGenerationPrompt(string systemPrompt, string userPrompt)
    {
        SystemPrompt = systemPrompt ?? string.Empty;
        UserPrompt = userPrompt ?? string.Empty;
    }

    public string SystemPrompt { get; }

    public string UserPrompt { get; }
}
