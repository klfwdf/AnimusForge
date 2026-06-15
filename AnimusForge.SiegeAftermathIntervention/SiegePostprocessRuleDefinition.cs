namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free description of an AF postprocess rule entry.
/// The fused AF adapter maps this to PostprocessRuleEntry.
/// </summary>
public readonly struct SiegePostprocessRuleDefinition
{
    public SiegePostprocessRuleDefinition(string tag, string description)
    {
        Tag = tag ?? string.Empty;
        Description = description ?? string.Empty;
    }

    public string Tag { get; }

    public string Description { get; }
}
