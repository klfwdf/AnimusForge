namespace AnimusForge;
internal sealed class DuelSettings { internal bool EnableTtsSpeech, TtsUseGptSoVits, UsesManagedGptSoVits=true; internal string TtsGptSoVitsManifestPath; internal string TtsGptSoVitsApiUrl="http://127.0.0.1:9880/tts"; }
internal static class AnimusForgeModulePaths { internal static string Root; internal static string GetCurrentModuleRoot()=>Root; }
internal sealed class TtsEngine { internal static readonly TtsEngine Instance=new(); internal int Cancellations; internal void CancelLocalPlayback()=>Interlocked.Increment(ref Cancellations); }
internal static class TtsTestFeedback { internal static void ReportRuntimeStatus(string state,string reason) { } }
internal static class Logger { internal static void Log(string owner,string text)=>System.Console.Error.WriteLine(owner+":"+text); }
