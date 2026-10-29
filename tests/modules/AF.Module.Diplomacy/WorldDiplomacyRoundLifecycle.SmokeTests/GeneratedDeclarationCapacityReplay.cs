using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Runs the real generated-document completion owner and peace-disclosure check.
// Campaign identity, document storage and final execution are observed fakes.
internal static class GeneratedDeclarationCapacityReplay
{
    internal static void Run()
    {
        foreach (string bodyKey in new[] { "body", "public_document", "document" })
        foreach (int length in new[] { 6000, 6001 })
            VerifyBoundary(bodyKey, length);
    }

    private static void VerifyBoundary(string bodyKey, int length)
    {
        // The last character is part of a substantive amount: truncating one
        // character would publish 123 while the action still promises 1234.
        const string termsText = "Alpha支付Beta，期限30天，每日贡金1234";
        string body = new string('x', length - termsText.Length) + termsText;
        var terms = new JObject
        {
            ["tribute_payer_kingdom_id"] = "a",
            ["tribute_receiver_kingdom_id"] = "b",
            ["daily_tribute"] = 1234,
            ["duration_days"] = 30
        };
        var action = new JObject
        {
            ["target_kingdom_id"] = "b",
            ["intent"] = "propose_peace",
            ["commitment"] = "proposal",
            ["peace_terms"] = terms.DeepClone()
        };
        var json = new JObject
        {
            ["title"] = "Peace proposal",
            [bodyKey] = body,
            ["actions"] = new JArray(action.DeepClone()),
            ["international_reputation_delta"] = 1,
            ["international_reputation_reason"] = "Concrete peace terms."
        };
        string raw = json.ToString(Formatting.None);
        var storage = new WorldDiplomacyStorage
        {
            ActiveExchange = new WorldDiplomacyExchange { ExchangeId = "exchange", State = "awaiting_generation" }
        };
        var round = new WorldDiplomacyRound { RoundId = "round", ConsecutiveTechnicalGenerationFailures = 2 };
        var job = new WorldDiplomacyJob
        {
            JobId = "capacity", Kind = "generate", AuthorKingdomId = "a", TargetKingdomId = "b",
            RoundId = "round", ExchangeId = "exchange", IsResponse = true
        };
        int created = 0, applied = 0, published = 0, rejected = 0, legalityChecks = 0;
        WorldDiplomacyDocument stored = null;
        string rejection = "", rejectedRaw = "";
        JObject rejectedJson = null;
        WorldDiplomacyGeneratedCompletionApplication.Commit(
            job, raw, storage, _ => round, _ => null, id => id, _ => null,
            (WorldDiplomacyJob current, JObject parsed, string author, string fallback, out string target, out string reason) =>
            {
                legalityChecks++;
                target = fallback;
                JObject parsedAction = (JObject)((JArray)parsed["actions"])[0];
                Test.True(JToken.DeepEquals(parsedAction, action), "capacity check preserves the original action before admission");
                return WorldDiplomacyGenerationValidationRules.TryGetPublicPeaceTermsDisclosureViolation(
                    "propose_peace", WorldDiplomacyEnvelopeJsonRules.ReadString(parsed, "body", "public_document", "document"),
                    parsedAction, author, target, id => id == "a" ? ("a", "Alpha") : ("b", "Beta"), _ => null, out reason);
            },
            (_, _, _) => new WorldDiplomacyPeaceTerms
            {
                TributePayerKingdomId = "a", TributeReceiverKingdomId = "b", DailyTribute = 1234, DurationDays = 30
            },
            (document, parsed, _, _, _, _) =>
            {
                applied++;
                Test.True(JToken.DeepEquals(parsed["actions"][0], action), "admitted action retains all original peace fields");
                document.Intent = "propose_peace";
                document.Commitment = "proposal";
                document.RequiresResponse = true;
                return true;
            },
            (author, target, title, text, _, _, _, _) =>
            {
                created++;
                return new WorldDiplomacyDocument
                {
                    DocumentId = "created", AuthorKingdomId = author, TargetKingdomId = target, Title = title, Body = text
                };
            },
            day => day.ToString(), _ => { },
            (_, _, _, reason) => throw new InvalidOperationException("Unexpected abandon: " + reason),
            (_, original, _, _, reason, parsed) =>
            {
                rejected++;
                rejectedRaw = original;
                rejection = reason;
                rejectedJson = parsed;
            },
            document => { stored = document; storage.Documents.Add(document); },
            (_, _, _, _, _, _) => published++, _ => { });

        Test.True(legalityChecks == 1, "capacity guard retains the existing legality and disclosure check");
        if (length == 6000)
        {
            Test.True(rejected == 0 && created == 1 && applied == 1 && published == 1
                && storage.Documents.Count == 1 && stored.Body == body && stored.Body.EndsWith("1234")
                && stored.PeaceTerms.DailyTribute == 1234 && stored.PeaceTerms.DurationDays == 30,
                "6000-character declaration is retained in full with unchanged substantive terms: " + bodyKey);
        }
        else
        {
            Test.True(rejected == 1 && rejection == "public_document_capacity_exceeded"
                && created == 0 && applied == 0 && published == 0 && storage.Documents.Count == 0
                && storage.ActiveExchange.State == "awaiting_generation"
                && round.ConsecutiveTechnicalGenerationFailures == 2,
                "6001-character declaration cannot create, publish or execute a truncated document: " + bodyKey);
            Test.True(rejectedRaw == raw
                && WorldDiplomacyEnvelopeJsonRules.ReadString(rejectedJson, "body", "public_document", "document") == body
                && JToken.DeepEquals(rejectedJson["actions"][0], action),
                "existing repair callback receives complete source text and unchanged actions/terms: " + bodyKey);
        }
    }
}
