using TaleWorlds.Localization;

namespace AnimusForge;

internal static class GptSoVitsUiText
{
    internal static string TestPhrase(string configured)
    {
        var language = GptSoVitsLanguage.Parse(configured);
        switch (language.Code)
        {
            case "zh": return Text("LanguageTestZh", "旅人，你好。我们先把事情说清楚，再决定下一步怎么做。");
            case "yue": return Text("LanguageTestYue", "你好呀，我哋先讲清楚件事，再决定下一步点做。");
            case "en": return Text("LanguageTestEn", "Hello, traveler. Let us discuss this carefully before deciding what to do next.");
            case "ja": return Text("LanguageTestJa", "旅人さん、こんにちは。まずは落ち着いて話してから、次にどうするか決めましょう。");
            case "auto": return Text("TestPhrase", "At your service, traveler!");
            default: throw new System.ArgumentException(TtsErrorUiText.Describe("tts_local_language_invalid"));
        }
    }
    internal static string Text(string key, string englishFallback)
        => new TextObject("{=AF_GPT_UI_" + key + "}" + englishFallback).ToString();
}

internal static class VillageDispositionIntegrationText
{
    internal static string VictoryReady => GptSoVitsUiText.Text("VictoryReady",
        "Battle rewards have been handled. Enter GCCZ to continue, or leave.");
}
