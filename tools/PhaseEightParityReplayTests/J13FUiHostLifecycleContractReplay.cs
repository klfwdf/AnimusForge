using System;
using System.IO;

internal static class J13FUiHostLifecycleContractReplay
{
    internal static void Run(string repo)
    {
        string Read(string path) => File.ReadAllText(Path.Combine(repo, path));
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("J13f UI host contract: " + name);
        }
        string Slice(string source, string start, string end)
        {
            int first = source.IndexOf(start, StringComparison.Ordinal);
            int last = first < 0 ? -1 : source.IndexOf(end, first + start.Length, StringComparison.Ordinal);
            Check(first >= 0 && last > first, "method boundary " + start);
            return source.Substring(first, last - first);
        }

        string weekly = Read("DevWeeklyReportPopup.cs");
        string onboarding = Read("AnimusForgeApiOnboardingPopup.cs");
        string overlay = Read("AnimusForgeNativeConversationOverlay.cs");
        string vm = Read("AnimusForgeApiOnboardingVM.cs");
        string onboardingHost = Read("ModOnboardingBehavior.cs");
        string weeklyShow = Slice(weekly, "public static bool Show(", "public static void ProcessDeferredCloseIfNeeded()");
        string onboardingShow = Slice(onboarding, "public static bool Show(", "public static void CloseActive(");
        string overlayShow = Slice(overlay, "private static bool Show(ScreenBase screen)", "private void Open()");
        Check(weeklyShow.Contains("devWeeklyReportPopup?.Close(silent: true);", StringComparison.Ordinal)
            && onboardingShow.Contains("popup?.Close(silent: true);", StringComparison.Ordinal)
            && overlayShow.Contains("overlay?.Close(silent: true);", StringComparison.Ordinal),
            "partially opened candidates are not closed");
        Check(Slice(onboarding, "public void Close(bool silent)", "if (!silent)").Contains("ResetInputRestrictions", StringComparison.Ordinal),
            "onboarding input restrictions not reset");
        Check(overlay.Contains("NativeConversationAnswerAreaController.ForceRestoreAll();", StringComparison.Ordinal)
            && overlay.Contains("_screen.RemoveLayer(_layer);", StringComparison.Ordinal)
            && vm.Contains("_uiDispatch.Pump(32);", StringComparison.Ordinal)
            && vm.Contains("_uiDispatch.Close();", StringComparison.Ordinal)
            && vm.Contains("YjKeyMasked = key.Length > 8 ?", StringComparison.Ordinal)
            && !vm.Contains("YjKeyMasked = key.Length > 8 ? (key.Substring(0, 4) + \"...\" + key.Substring(key.Length - 4)) : key;", StringComparison.Ordinal),
            "overlay restoration or onboarding UI callback retirement disconnected");
        foreach (string method in new[] { "private void BeginValidateApiConfigSetAndContinue(",
            "private void BeginValidateBaseUrlAndContinueCore(", "private void BeginFetchAvailableModelsForSetup()",
            "private void BeginValidateMcmApiAndContinueCore(" })
        {
            int start = onboardingHost.IndexOf(method, StringComparison.Ordinal);
            int worker = onboardingHost.IndexOf("Task.Run(async delegate", start, StringComparison.Ordinal);
            int source = onboardingHost.IndexOf("CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();", start, StringComparison.Ordinal);
            Check(start >= 0 && source > start && source < worker, "cancellation source created after worker: " + method);
        }
        Check(!onboardingHost.Contains("_apiValidationVersion", StringComparison.Ordinal)
            && !onboardingHost.Contains("_baseUrlValidationVersion", StringComparison.Ordinal)
            && !onboardingHost.Contains("_modelFetchVersion", StringComparison.Ordinal),
            "host still owns async invalidation versions");
        Console.WriteLine("PASS J13FUiHostLifecycleContractReplay popup/overlay partial-open cleanup, input release and callback retirement; source-wiring-only live=NOT_RUN");
    }
}
