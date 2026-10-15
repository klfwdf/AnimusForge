using TaleWorlds.Localization;

namespace AnimusForge;

internal static class GptSoVitsUiText
{
    internal static string Text(string key, string englishFallback)
        => new TextObject("{=AF_GPT_UI_" + key + "}" + englishFallback).ToString();
}

internal static class VillageDispositionIntegrationText
{
    internal static string VictoryReady => GptSoVitsUiText.Text("VictoryReady",
        "Battle rewards have been handled. Enter GCCZ to continue, or leave.");
}
