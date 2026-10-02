namespace AnimusForge;

internal interface IWorldDiplomacyDiscussionSource
{
    bool TryCaptureRepresentative(string heroId, out WorldDiplomacyDiscussionCandidate candidate, out string kingdomId);
    bool HasKnownDocument(string heroId, string kingdomId);
}

internal static class WorldDiplomacyDiscussionApplication
{
    internal static bool CanDiscuss(IWorldDiplomacyDiscussionSource source, string heroId)
    {
        try
        {
            if (!source.TryCaptureRepresentative(heroId, out WorldDiplomacyDiscussionCandidate candidate, out string kingdomId)
                || !WorldDiplomacyDiscussionEligibilityRules.IsEligibleRepresentative(candidate))
            {
                return false;
            }

            return WorldDiplomacyDiscussionEligibilityRules.CanDiscuss(
                candidate, source.HasKnownDocument(heroId, kingdomId));
        }
        catch
        {
            return false;
        }
    }
}
