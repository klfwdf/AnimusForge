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
        string lengthTemplate = DiplomacyMiscPromptTexts.GetRequired("PlayerDraft_LengthRule");
        string lengthRule = lengthTemplate.Replace("{MIN}", input.MinimumCharacters.ToString(CultureInfo.InvariantCulture))
            .Replace("{MAX}", input.MaximumCharacters.ToString(CultureInfo.InvariantCulture));
        JArray messages = new JArray(new JObject { ["role"] = "system", ["content"] = Contract + "\n" + lengthRule });
        if (!string.IsNullOrWhiteSpace(preference))
        {
            string prefHeader = DiplomacyMiscPromptTexts.GetRequired("PlayerDraft_PreferenceHeader");
            messages.Add(new JObject { ["role"] = "system", ["content"] = prefHeader + preference });
        }
        string userHeader = DiplomacyMiscPromptTexts.GetRequired("PlayerDraft_UserHeader");
        JObject finalSystem = (JObject)messages[messages.Count - 1];
        finalSystem["content"] = PromptComposerTexts.ApplyOutputLanguage((string)finalSystem["content"]);
        messages.Add(new JObject { ["role"] = "user", ["content"] = userHeader + input.OriginalBody });
        return messages;
    }

    internal static WorldDiplomacyPlayerDraftResult Parse(string raw, WorldDiplomacyPlayerDraftInput input)
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
                return WorldDiplomacyPlayerDraftResult.Failed(DiplomacyUiLocalization.Text("AF_DIP_UI_Compose_DraftTooLong", "The draft was too long; original text was kept. Please adjust the diplomacy character limit and try again."));
            body = AnimusForgeTextInputSanitizer.SanitizeMultiline(body, 6000).Trim();
            int count = CountVisibleCharacters(body);
            if (count < input.MinimumCharacters || count > input.MaximumCharacters)
                return WorldDiplomacyPlayerDraftResult.Failed(DiplomacyUiLocalization.Format("AF_DIP_UI_Compose_DraftLengthMismatch", "The draft did not meet the diplomacy character length settings ({MIN}-{MAX} characters); original text was kept. Please try again or adjust settings.", ("MIN", input.MinimumCharacters), ("MAX", input.MaximumCharacters)));
            return WorldDiplomacyPlayerDraftResult.Written(body);
        }
        catch (JsonException)
        {
            return WorldDiplomacyPlayerDraftResult.Failed(DiplomacyUiLocalization.Text("AF_DIP_UI_Compose_DraftFormatError", "The draft returned by the scribe was in an invalid format; original text was kept. Please try again."));
        }
    }

    internal static int CountVisibleCharacters(string body)
    {
        int count = 0;
        for (int i = 0; i < body.Length; i++)
        {
            if (char.IsWhiteSpace(body[i])) continue;
            if (char.IsHighSurrogate(body[i]) && i + 1 < body.Length && char.IsLowSurrogate(body[i + 1])) i++;
            count++;
        }
        return count;
    }
}
