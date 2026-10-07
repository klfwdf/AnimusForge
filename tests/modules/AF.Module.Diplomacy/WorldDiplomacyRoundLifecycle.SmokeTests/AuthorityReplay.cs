using AnimusForge;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;

internal static class AuthorityReplay
{
    internal static void Run()
    {
        foreach (bool exists in new[] { false, true })
        foreach (bool eliminated in new[] { false, true })
        foreach (bool controlled in new[] { false, true })
        foreach (bool player in new[] { false, true })
        foreach (bool rulerAlive in new[] { false, true })
        {
            var party = new WorldDiplomacyAuthoritySnapshot("a", exists, eliminated, controlled, "s", player, rulerAlive);
            Test.True(WorldDiplomacyAuthorityRules.HasIndependentAuthority(party) == (exists && !eliminated && !controlled), "authority truth table");
            bool allowed = WorldDiplomacyAuthorityRules.CanAiAuthor(party, out string reason);
            Test.True(allowed == (exists && !eliminated && !player && rulerAlive), "AI authorship uses captured facts");
            Test.True(reason == (!exists || eliminated ? "author_kingdom_missing" : player ? "player_controlled_realm_requires_player_authorization" : !rulerAlive ? "ruler_unavailable" : ""), "authorship rejection priority preserved");
            Test.True(WorldDiplomacyAuthorityRules.Representative(party) == (controlled ? "s" : "a"), "representative selection");
        }
        var doc = new WorldDiplomacyDocument { DocumentId = "d", RoundId = "r", AuthorKingdomId = "b", IsReadyForPublication = true };
        var round = new WorldDiplomacyRound { RoundId = "r" };
        round.Participants.Add(new WorldDiplomacyRoundParticipant { KingdomId = "a", MandatoryReplyPending = true });
        foreach (bool ruler in new[] { false, true })
        foreach (bool independent in new[] { false, true })
        {
            var result = WorldDiplomacyPresentationQueries.Detail(doc, round,
                new WorldDiplomacyPlayerContext(2, "a", ruler, independent, ""), _ => "");
            Test.True(result.CanReply == (ruler && independent), "query combines ruler, authority and outstanding reply");
        }
        round.Participants[0].MandatoryReplyPending = false;
        Test.True(!WorldDiplomacyPresentationQueries.Detail(doc, round, new WorldDiplomacyPlayerContext(2, "a", true, true, ""), _ => "").CanReply, "completed obligation removes reply eligibility");
        Test.True(WorldDiplomacyWarPressureRules.CalculatePeacePressure(7, 0, 0, 1000, 1000, 0, 0, 0, 0) == 0, "peace pressure neutral baseline");
        Test.True(WorldDiplomacyWarPressureRules.CalculatePeacePressure(119, 0, 500, 1000, 2500, 2000, 0, 2, 2) == 300, "peace pressure preserves all seven terms and ceiling");
    }
}
