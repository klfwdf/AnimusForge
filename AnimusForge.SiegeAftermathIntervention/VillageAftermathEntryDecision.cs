namespace AnimusForge.SiegeAftermathIntervention;

public sealed class VillageAftermathEntryDecision
{
    public VillageAftermathEntryDecision(bool allowed, VillageAftermathAuthorityKind authorityKind, string reasonCode)
    {
        Allowed = allowed;
        AuthorityKind = authorityKind;
        ReasonCode = reasonCode ?? string.Empty;
    }

    public bool Allowed { get; }

    public VillageAftermathAuthorityKind AuthorityKind { get; }

    public string ReasonCode { get; }
}
