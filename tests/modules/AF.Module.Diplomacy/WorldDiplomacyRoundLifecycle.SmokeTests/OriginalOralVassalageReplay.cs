using AnimusForge;
using AnimusForge.DiplomacyDialogue;
using Newtonsoft.Json.Linq;

// Real orchestration, routing and speech-review owners; campaign effect ports are fakes.
internal static class OriginalOralVassalageReplay
{
    private sealed class TreatyHost : ConcurrentOralMigrationReplay.Host, IWorldDiplomacyDialogueHost
    {
        internal bool Existing;
        (bool available, bool cycle, bool existing) IWorldDiplomacyDialogueHost.TreatyState(string receiving, string joining)
            => (true, false, Existing);
    }

    internal static void Run()
    {
        foreach (string channel in new[] { "native", "scene", "courier" })
        foreach (string type in new[] { "Tributary", "Garrison", "Vassal" })
        {
            var (host, owner) = ConcurrentOralMigrationReplay.Fixture();
            string payload = $"action={type};move=NewMatter;target=p;receiving=p;joining=a";
            var origin = new DialogueInteractionOrigin(channel, channel + type, "session",
                "Submit to my kingdom and trade for fourteen days", "I agree to submission and trade for fourteen days");
            owner.SubmitOralDiplomaticCommitment("ruler_a", "a", payload, origin, out bool registered);
            Test.True(!registered && owner.CurrentStorage.DialogueArrangements.Count == 0
                && owner.CurrentStorage.Jobs.Count == 0 && host.Effects == 0,
                "COMMIT cannot replace original oral signing: " + channel + type);
            Test.True(owner.CaptureSpeechReview("ruler_a", "a", origin, "[ACTION:DIPLOMACY:REVIEW_REMAINING]" + ("")), "real speech captured without a tag");
            var job = owner.CurrentStorage.Jobs.Single();
            owner.PrepareSharedRequest(job);
            Test.True(job.SystemPrompt.Contains(DiplomacyPromptTexts.Get("SpeechReview_OriginalVassalage", "missing")),
                "actual review request explains original owner");
            var entries = new JArray(
                new JObject { ["payload"] = payload, ["npc_quote"] = origin.FullNpcText,
                    ["player_agreed"] = true, ["player_quote"] = origin.FullPlayerText },
                new JObject { ["payload"] = "action=Trade;move=NewMatter;target=p;days=14",
                    ["npc_quote"] = origin.FullNpcText, ["player_agreed"] = true, ["player_quote"] = origin.FullPlayerText });
            owner.TryCompleteDiplomacyPipeline(job, new LlmJobResult { Success = true,
                Content = new JObject { ["unresolved"] = false, ["commitments"] = entries }.ToString() });
            Test.True(job.Kind == "oral_review_audit", "valid mixed extraction reaches independent audit");
            owner.TryCompleteDiplomacyPipeline(job, new LlmJobResult { Success = true, Content = "{\"approved\":true}" });
            Test.True(owner.CurrentStorage.DialogueArrangements.Count == 0
                && owner.CurrentStorage.SpeechReviews.Count == 0 && host.Effects == 0,
                "original signing and direct bilateral trade cannot be duplicated by formal review: " + channel + type);
        }
        foreach (string type in new[] { "Tributary", "Garrison", "Vassal" })
        {
            foreach (var pair in new[] { (target: "b", receiving: "a", joining: "b"),
                                        (target: "p", receiving: "a", joining: "p") })
            {
                var (host, owner) = ConcurrentOralMigrationReplay.Fixture();
                owner.SubmitOralDiplomaticCommitment("ruler_a", "a",
                    $"action={type};move=NewMatter;target={pair.target};receiving={pair.receiving};joining={pair.joining}",
                    new DialogueInteractionOrigin("native", playerText: "I agree", npcText: "I agree"), out bool registered);
                Test.True(registered && owner.CurrentStorage.DialogueArrangements.Count == 1,
                    "AI-to-AI and player submission still use formal diplomacy: " + type + pair);
            }
            DialogueTagPayload.TryParse($"action={type};move=AcceptProposal;target=p;source_document=original", out var response);
            Test.True(!DialogueRoutingRules.UsesLegacyOralVassalage(response, "a", "p"),
                "explicit source acceptance remains formal: " + type);
            DialogueTagPayload.TryParse($"action={type};move=NewMatter;target=p;receiving=p;joining=a", out var newMatter);
            Test.True(!DialogueRoutingRules.UsesLegacyOralVassalage(newMatter, "a", ""),
                "non-ruler mediation is not claimed by original player-suzerain entry");
        }
        DialogueTagPayload.TryParse("action=Annexation;move=NewMatter;target=p;receiving=p;joining=a", out var annexation);
        Test.True(!DialogueRoutingRules.UsesLegacyOralVassalage(annexation, "a", "p"), "annexation unchanged");
        foreach (string type in new[] { "tributary", "garrison", "vassal" })
        foreach (bool existing in new[] { false, true })
        {
            var host = new TreatyHost { Existing = existing };
            var owner = new WorldDiplomacyOrchestration(host, new WorldDiplomacyRuntimeState());
            host.Owner = owner;
            var source = new WorldDiplomacyDocument {
                DocumentId = "formal_source", AuthorKingdomId = "a", TargetKingdomId = "p",
                Intent = "propose_" + type, IsReadyForPublication = true,
                TreatyTerms = new() { ReceivingKingdomId = "p", JoiningKingdomId = "a" }
            };
            owner.CurrentStorage.Documents.Add(source);
            var offer = new WorldDiplomacyRoundOffer { SourceDocumentId = source.DocumentId,
                ProposerKingdomId = "a", TargetKingdomId = "p", Intent = source.Intent };
            var response = new WorldDiplomacyDocument { DocumentId = "formal_response",
                AuthorKingdomId = "p", TargetKingdomId = "a", Intent = "accept_" + type,
                RespondingToOfferDocumentId = source.DocumentId, IsPlayerAuthored = true };
            var outcome = owner.ExecuteFormalTreatyOffer(source.Intent, offer, source, response);
            Test.True(outcome == (existing ? WorldDiplomacyOfferOutcome.Invalidated : WorldDiplomacyOfferOutcome.Applied)
                && host.Effects == (existing ? 0 : 1) && response.ChangedDiplomaticState == !existing,
                "formal acceptance still executes; an existing oral treaty prevents a second signature: " + type + existing);
        }
    }
}
