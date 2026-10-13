namespace AnimusForge.Refactor.Contracts;

// Same-DLL, value-only readiness observation; not a public sub-MOD protocol or a live profile.
internal sealed class NpcPersonaReadinessSnapshot
{
    internal NpcPersonaReadinessSnapshot(string heroId, string name, string description,
        bool available, bool needsGeneration, bool active, bool coolingDown)
    {
        HeroId = heroId ?? "";
        Name = string.IsNullOrWhiteSpace(name) ? AnimusForge.PersonaUiTexts.Text("AF_PERSONA_UI_UnknownNpc", "Unknown NPC") : name.Trim();
        Description = description ?? "";
        Available = available;
        NeedsGeneration = needsGeneration;
        Active = active;
        CoolingDown = coolingDown;
    }
    internal string HeroId { get; }
    internal string Name { get; }
    internal string Description { get; }
    internal bool Available { get; }
    internal bool NeedsGeneration { get; }
    internal bool Active { get; }
    internal bool CoolingDown { get; }
}
