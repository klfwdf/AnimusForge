namespace AnimusForge.Refactor.Contracts;

// Same-DLL, value-only readiness observation; not a public sub-MOD protocol or a live profile.
internal sealed class NpcPersonaReadinessSnapshot
{
    internal NpcPersonaReadinessSnapshot(string heroId, string name, string personality, string background,
        bool available, bool needsGeneration, bool active, bool coolingDown)
    {
        HeroId = heroId ?? "";
        Name = string.IsNullOrWhiteSpace(name) ? "该NPC" : name.Trim();
        Personality = personality ?? "";
        Background = background ?? "";
        Available = available;
        NeedsGeneration = needsGeneration;
        Active = active;
        CoolingDown = coolingDown;
    }
    internal string HeroId { get; }
    internal string Name { get; }
    internal string Personality { get; }
    internal string Background { get; }
    internal bool Available { get; }
    internal bool NeedsGeneration { get; }
    internal bool Active { get; }
    internal bool CoolingDown { get; }
}
