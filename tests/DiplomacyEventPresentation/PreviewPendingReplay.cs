using AnimusForge;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.Library;

internal static class PreviewPendingReplay
{
    internal static void Run(Action<bool, string> check)
    {
        DiplomacyEventPopup.IsPreparing = true;
        DiplomacyEventPreview.Request(DiplomacyEventStage.Started);
        DiplomacyEventPreview.ProcessPending();
        check(DiplomacyEventPresentationBridge.Calls == 1 && InformationManager.Messages == 0, "Preparing preview must not report an error.");
        DiplomacyEventPresentationBridge.Succeed = true;
        DiplomacyEventPopup.IsPreparing = false;
        DiplomacyEventPreview.ProcessPending();
        DiplomacyEventPreview.ProcessPending();
        check(DiplomacyEventPresentationBridge.Calls == 2, "Pending preview must retry once and stop after opening.");
        DiplomacyEventPreview.Request(DiplomacyEventStage.Started);
        SaveRuntimeGuard.Generation++;
        DiplomacyEventPreview.ProcessPending();
        check(DiplomacyEventPresentationBridge.Calls == 2, "Retired campaign preview must not retry.");
        DiplomacyEventPresentationBridge.Succeed = false;
        DiplomacyEventPreview.Request(DiplomacyEventStage.Ended);
        DiplomacyEventPreview.ProcessPending();
        DiplomacyEventPreview.ProcessPending();
        check(DiplomacyEventPresentationBridge.Calls == 3 && InformationManager.Messages == 1, "A genuine modal refusal must report once, not retry forever.");
    }
}
namespace AnimusForge
{
    internal static class DiplomacyEventPopup
    {
        internal static bool IsPreparing;
        internal static bool TryShowPreview(DiplomacyEventPresentation data, out string image)
            => DiplomacyEventPresentationBridge.TryShow(data, out image);
    }
    internal static class SaveRuntimeGuard
    {
        internal static long Generation = 1;
        internal static long CaptureGeneration() => Generation;
        internal static bool IsCurrentGeneration(long generation) => generation == Generation;
    }
    internal static class DiplomacyEventPopupVM { internal static string Text(string key, string fallback) => fallback; }
    internal static class DiplomacyEventPresentationBridge
    {
        internal static int Calls; internal static bool Succeed;
        internal static bool TryShow(DiplomacyEventPresentation data, out string image) { Calls++; image = ""; return Succeed; }
    }
}
namespace TaleWorlds.Core { internal class PreviewCoreStub { } }
namespace TaleWorlds.Library
{
    internal class InformationMessage { internal InformationMessage(string message) { } }
    internal static class InformationManager { internal static int Messages; internal static void DisplayMessage(InformationMessage message) { Messages++; } }
}
namespace TaleWorlds.ScreenSystem
{
    internal class ScreenBase { }
    internal static class ScreenManager { internal static ScreenBase TopScreen = new(); }
}
