using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;

internal static class LegacyArchiveEvidenceReplay
{
    internal static void Run()
    {
        string summary = new string('史', 5000) + " summary tail";
        string fact = new string('约', 2400) + " conditional fact tail";
        foreach (bool rebuild in new[] { false, true })
        {
            var originalFact = new WorldDiplomacyRoundFact
            {
                Kind = "confirmed_result", Text = fact,
                SourceDocumentIds = new() { "source" }, KingdomIds = new() { "a" }
            };
            var archive = new WorldDiplomacyRoundSummary
            {
                RoundId = "legacy", Summary = summary, Facts = new() { originalFact }
            };
            var storage = new WorldDiplomacyStorage();
            if (rebuild) storage.Documents.Add(new WorldDiplomacyDocument
            {
                DocumentId = "source", RoundId = "legacy", AuthorKingdomId = "a",
                Body = "Original declaration", Title = "title"
            });
            WorldDiplomacyRoundLifecycleRules.UpgradeRoundSummaryToStructuredArchive(storage, archive,
                _ => rebuild ? new WorldDiplomacyRound { RoundId = "legacy" } : null, day => "day " + day);
            Test.True(archive.LegacySummaryOriginal == summary && archive.LegacyFactsOriginal.Single().Text == fact,
                "legacy upgrade preserves complete original evidence with or without surviving documents");
            Test.True(archive.LegacyFactsOriginal[0].Kind == "confirmed_result"
                && archive.Facts.All(x => x.Kind == "declaration"),
                "original claimed result remains evidence while current projection does not invent execution");
            if (!rebuild) Test.True(archive.Summary.EndsWith(summary) && archive.Facts[0].Text.EndsWith(fact),
                "unresolvable legacy evidence is not clipped at former 1200/360 character limits");
            originalFact.SourceDocumentIds.Clear(); originalFact.KingdomIds.Clear(); originalFact.Text = "mutated";
            Test.True(archive.LegacyFactsOriginal[0].SourceDocumentIds.SequenceEqual(new[] { "source" })
                && archive.LegacyFactsOriginal[0].KingdomIds.SequenceEqual(new[] { "a" })
                && archive.LegacyFactsOriginal[0].Text == fact, "original evidence owns detached identity lists");
            var restored = JsonConvert.DeserializeObject<WorldDiplomacyRoundSummary>(JsonConvert.SerializeObject(archive))!;
            WorldDiplomacyRoundLifecycleRules.UpgradeRoundSummaryToStructuredArchive(storage, restored, _ => null, day => "day " + day);
            Test.True(restored.LegacySummaryOriginal == summary && restored.LegacyFactsOriginal[0].Text == fact,
                "save/load and repeated normalization retain exact original text without duplicate prefixes");
        }
    }
}
