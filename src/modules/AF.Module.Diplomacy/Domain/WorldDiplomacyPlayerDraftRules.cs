using System;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal sealed class WorldDiplomacyPlayerDraftInput
{
    internal readonly string OriginalBody;
    internal readonly int MinimumCharacters;
    internal readonly int MaximumCharacters;
    internal readonly long Generation;
    internal WorldDiplomacyPlayerDraftInput(string body, int minimum, int maximum, long generation)
    {
        OriginalBody = body ?? "";
        MinimumCharacters = Math.Max(1, Math.Min(1000, minimum));
        MaximumCharacters = Math.Max(MinimumCharacters, Math.Max(1, Math.Min(1000, maximum)));
        Generation = generation;
    }
}

internal sealed class WorldDiplomacyPlayerDraftResult
{
    internal readonly string Body;
    internal readonly string Error;
    internal bool Success => !string.IsNullOrEmpty(Body) && string.IsNullOrEmpty(Error);
    private WorldDiplomacyPlayerDraftResult(string body, string error) { Body = body; Error = error; }
    internal static WorldDiplomacyPlayerDraftResult Written(string body) => new WorldDiplomacyPlayerDraftResult(body, "");
    internal static WorldDiplomacyPlayerDraftResult Failed(string error) => new WorldDiplomacyPlayerDraftResult("", error);
}

internal static class WorldDiplomacyPlayerDraftRules
{
    internal static string Contract => DiplomacyMiscPromptTexts.GetRequired("PlayerDraft_Contract");

    internal static JArray BuildMessages(WorldDiplomacyPlayerDraftInput input, string preference)
    {
        int target = input.MinimumCharacters + (input.MaximumCharacters - input.MinimumCharacters) / 2;
        string lengthRule = PromptComposerTexts.FormatTemplate(DiplomacyMiscPromptTexts.GetRequired("PlayerDraft_LengthRule"),
            ("MIN", input.MinimumCharacters.ToString(CultureInfo.InvariantCulture)), ("MAX", input.MaximumCharacters.ToString(CultureInfo.InvariantCulture)), ("TARGET", target.ToString(CultureInfo.InvariantCulture)));
        string system = Contract;
        if (!string.IsNullOrWhiteSpace(preference))
        {
            string prefHeader = DiplomacyMiscPromptTexts.GetRequired("PlayerDraft_PreferenceHeader");
            system += "\n" + prefHeader + preference;
        }
        string userHeader = DiplomacyMiscPromptTexts.GetRequired("PlayerDraft_UserHeader");
        system += "\n" + DiplomacyMiscPromptTexts.GetRequired("PlayerDraft_LengthHeader") + "\n" + lengthRule;
        return new JArray(new JObject { ["role"] = "system", ["content"] = PromptComposerTexts.ApplyOutputLanguage(system) },
            new JObject { ["role"] = "user", ["content"] = userHeader + input.OriginalBody });
    }

    internal static int GetOutputTokenBudget(WorldDiplomacyPlayerDraftInput input)
    {
        // CJK tokenization varies by provider. Leave room for the full requested
        // range and JSON overhead; max_tokens is capacity, not a required spend.
        return input.MaximumCharacters * 4 + 256;
    }

    internal static WorldDiplomacyPlayerDraftResult Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Length > 24000)
            return WorldDiplomacyPlayerDraftResult.Failed(DiplomacyUiLocalization.Text("AF_DIP_UI_Compose_DraftIncomplete", "The scribe failed to return a complete draft; original text was kept. Please try again."));
        try
        {
            JObject json = JObject.Parse(raw, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (json.Properties().Count() != 1 || json["body"]?.Type != JTokenType.String)
                return WorldDiplomacyPlayerDraftResult.Failed(DiplomacyUiLocalization.Text("AF_DIP_UI_Compose_DraftFormatError", "The draft returned by the scribe was in an invalid format; original text was kept. Please try again."));
            string body = (string)json["body"];
            if (body.Length > 6000)
                return WorldDiplomacyPlayerDraftResult.Failed(DiplomacyUiLocalization.Text("AF_DIP_UI_Compose_DraftTooLong", "The scribe returned a draft exceeding the editor capacity; original text was kept. Please try again."));
            body = DiplomacyReplyTextRules.CleanNpcText(AnimusForgeTextInputSanitizer.SanitizeMultiline(body, 6000).Trim());
            if (string.IsNullOrWhiteSpace(body))
                return WorldDiplomacyPlayerDraftResult.Failed(DiplomacyUiLocalization.Text("AF_DIP_UI_Compose_DraftIncomplete", "The scribe failed to return a complete draft; original text was kept. Please try again."));
            return WorldDiplomacyPlayerDraftResult.Written(body);
        }
        catch (JsonException)
        {
            return WorldDiplomacyPlayerDraftResult.Failed(DiplomacyUiLocalization.Text("AF_DIP_UI_Compose_DraftFormatError", "The draft returned by the scribe was in an invalid format; original text was kept. Please try again."));
        }
    }

}
