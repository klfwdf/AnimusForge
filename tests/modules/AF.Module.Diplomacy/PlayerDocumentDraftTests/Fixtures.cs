using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace TaleWorlds.Library
{
    [AttributeUsage(AttributeTargets.Property)] public sealed class DataSourceProperty : Attribute { }
    public class ViewModel
    {
        protected void OnPropertyChangedWithValue<T>(T value, string name)
        { if (Environment.CurrentManagedThreadId != Test.UiThread) throw new InvalidOperationException("Off-thread UI write: " + name); }
        public virtual void OnFinalize() { }
    }
}

namespace AnimusForge
{
    // Only the game/MCM host and diagnostic sinks are substituted. Tests link the
    // actual VM, application, JSON contract, client, protocol, transport and save guard.
    internal sealed class DuelSettings
    {
        internal static DuelSettings Current = new DuelSettings();
        internal static HttpClient GlobalClient;
        internal string ApiUrl = "https://main.example/v1/chat/completions", ApiKey = "fixture-main", ModelName = "main-model";
        internal string EventAndRebellionApiUrl = "https://event.example/v1/chat/completions", EventAndRebellionApiKey = "fixture-event", EventAndRebellionModelName = "event-model";
        internal string WorldDiplomacyPrompt = "简明冷峻";
        internal int MainApiMaxTokens = 12000, EventAndRebellionApiMaxTokens = 4000;
        internal float MainTemperature = 0.7f, EventTemperature = 0.4f;
        internal int MinChars = 40, MaxChars = 200;
        internal static int Reads;
        internal static bool StrictThread = true;
        internal const string ReasoningEffortHigh = "high";
        internal const int DefaultEventAndRebellionApiMaxTokens = 12000, DefaultGeneralApiMaxTokens = 12000;
        internal static DuelSettings GetSettings()
        {
            if (StrictThread && Environment.CurrentManagedThreadId != Test.UiThread) throw new InvalidOperationException("Off-thread MCM read");
            Reads++;
            return Current;
        }
        internal string GetEffectiveEventAndRebellionModelName() => EventAndRebellionModelName;
        internal string GetEventAndRebellionSelectedModelOption() => EventAndRebellionModelName;
        internal string GetEffectiveMainModelName() => ModelName;
        internal float GetEventAndRebellionApiTemperature() => EventTemperature;
        internal float GetMainApiTemperature() => MainTemperature;
        internal static string GetEffectiveApiUrl(string value) => LlmApiCompat.GetEffectiveChatApiUrl(value);
        internal static int ClampApiMaxTokens(int value, int fallback) => Math.Max(512, Math.Min(64000, value > 0 ? value : fallback));
        internal static float ClampApiTemperature(float value) => Math.Max(0, Math.Min(2, value));
        internal static void ApplyThinkingControls(JObject body, string url, string model, bool thinkingEnabled, string effort, out string mode)
        { body["reasoning_effort"] = "none"; mode = "fixture_disabled"; }
        internal static void RemoveThinkingControls(JObject body) => body.Remove("reasoning_effort");
    }

    internal static class WorldDiplomacyBehavior
    {
        internal static void GetDiplomaticDeclarationCharacterRange(out int minimum, out int maximum)
        {
            var settings = DuelSettings.GetSettings();
            minimum = Math.Max(1, Math.Min(1000, settings.MinChars));
            maximum = Math.Max(minimum, Math.Max(1, Math.Min(1000, settings.MaxChars)));
        }
    }

    internal static class Logger
    {
        internal static void Log(string source, string message) { }
        internal static int EstimateTokensFromMessages(JArray value) => 0;
        internal static int EstimateTokens(string value) => 0;
        internal static void RecordTokenStats(int input, int output, JArray messages, string text, string mode, string body) { }
    }
}
