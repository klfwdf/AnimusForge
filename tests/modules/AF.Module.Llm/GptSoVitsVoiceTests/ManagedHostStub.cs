namespace AnimusForge;
internal static class GptSoVitsManagedRuntime
{
    internal static void Observe(DuelSettings settings) { }
    internal static bool TryCapture(string configured, out string endpoint, out string instance)
    { endpoint = configured; instance = null; return true; }
}

internal static class GptSoVitsUiText { internal static string Text(string key, string fallback) => fallback; }
