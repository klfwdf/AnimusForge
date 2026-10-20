using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json.Linq;

internal static class EnvelopeAliasReviewReplay
{
    internal static void Run()
    {
        foreach (string body in new[]
        {
            "We refuse peace and will continue fighting.",
            "我们拒绝和平与结盟，继续作战。",
            "We formally reject the old trade agreement and declaration of war."
        })
        {
            foreach (string intent in new[] { "statement", "", "reject_trade" })
            {
                var json = new JObject
                {
                    ["body"] = body,
                    ["actions"] = new JArray(new JObject { ["target_kingdom_id"] = "k2", ["intent"] = intent })
                };
                var job = new WorldDiplomacyJob
                {
                    PresentedLegalActionSignature = "k1|r1\nk2=propose_peace,propose_trade,warning"
                };
                WorldDiplomacyEnvelopeJsonRules.NormalizeGeneratedDiplomaticEnvelopeShape(job, json);
                Test.True((string)json["actions"][0]["intent"] == intent,
                    "prose and available actions must never invent or override an action intent");
            }
        }

        var multi = JObject.Parse("""
            {"intent":"warning","target_kingdom_id":"old","actions":[
              {"target_id":"k2","action_type":"propose_trade"},
              {"target_country_id":"k3","intent":"propose_alliance"}],"body":"Original text"}
            """);
        WorldDiplomacyEnvelopeJsonRules.NormalizeGeneratedDiplomaticEnvelopeShape(null, multi);
        foreach (JObject action in (JArray)multi["actions"])
        {
            JObject single = WorldDiplomacyEnvelopeJsonRules.BuildGeneratedSingleActionEnvelope(multi, action);
            Test.True(WorldDiplomacyEnvelopeJsonRules.ReadIntent(single) == (string)action["intent"],
                "a split action must read its own intent rather than the inherited root intent");
            Test.True(WorldDiplomacyEnvelopeJsonRules.ReadTargetKingdomId(single) == (string)action["target_kingdom_id"],
                "a split action must read its own target rather than the inherited root target");
        }
        var missing = JObject.Parse("{\"actions\":[{\"target_id\":\"k2\"}],\"intent\":\"declare_war\"}");
        WorldDiplomacyEnvelopeJsonRules.NormalizeGeneratedDiplomaticEnvelopeShape(null, missing);
        Test.True((string)missing["actions"][0]["intent"] == "",
            "an action missing its intent must stay invalid instead of borrowing the root action");

        var nested = JObject.Parse("{\"content\":{\"title\":\"Title\",\"text\":\"Actual body 正文\"}}");
        Test.True(WorldDiplomacyEnvelopeJsonRules.ReadBody(nested) == "Actual body 正文",
            "nested content must return its text rather than serialized JSON");
        Test.True(WorldDiplomacyEnvelopeJsonRules.ReadTitle(nested) == "Title", "nested content title is preserved");
        var malformed = JObject.Parse("{\"body\":{},\"content\":[\"not public text\"]}");
        Test.True(WorldDiplomacyEnvelopeJsonRules.ReadBody(malformed) == "", "objects and arrays are not public prose");
        Test.True(WorldDiplomacyEnvelopeJsonRules.ReadTargetKingdomId(
            JObject.Parse("{\"targets\":[\"k2\",\"k3\"]}")) == "", "multiple targets cannot collapse silently to the first target");

        var legacy = JObject.Parse("""
            {"action_type":"propose_trade","target_country_id":"k2","content":"Trade proposal 正文"}
            """);
        WorldDiplomacyEnvelopeJsonRules.NormalizeGeneratedDiplomaticEnvelopeShape(null, legacy);
        Test.True((string)legacy["actions"][0]["intent"] == "propose_trade"
            && (string)legacy["actions"][0]["target_kingdom_id"] == "k2"
            && (string)legacy["body"] == "Trade proposal 正文", "explicit legacy aliases still normalize without semantic guessing");

        var declaration = JObject.Parse("""
            {"round_plan":["k2"],"declarations":[{"target_id":"k2","action_type":"propose_trade",
            "title":"Declaration","content":{"text":"Preserved declaration"}}]}
            """);
        WorldDiplomacyEnvelopeJsonRules.NormalizeGeneratedDiplomaticEnvelopeShape(null, declaration);
        Test.True((string)declaration["actions"][0]["intent"] == "propose_trade"
            && (string)declaration["body"] == "Preserved declaration"
            && (string)declaration["round_plan"]["selected_kingdom_ids"][0] == "k2",
            "declarations, nested content and array round plans remain supported");
    }
}
