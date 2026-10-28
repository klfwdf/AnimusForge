using AnimusForge.Refactor.Contracts;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

internal static class DiplomacyEventPreview
{
    private static DiplomacyEventStage? _pending;
    private static long _generation;
    private static ScreenBase _screen;
    internal static void Request(DiplomacyEventStage stage)
    { _pending = stage; _generation = SaveRuntimeGuard.CaptureGeneration(); _screen = ScreenManager.TopScreen; }

    internal static void ProcessPending()
    {
        if (!_pending.HasValue) return;
        DiplomacyEventStage stage = _pending.Value;
        _pending = null;
        ScreenBase screen = _screen; _screen = null;
        if (!SaveRuntimeGuard.IsCurrentGeneration(_generation) || !ReferenceEquals(screen, ScreenManager.TopScreen)) return;
        if (!DiplomacyEventPresentationBridge.TryShow(Create(stage), out _))
        {
            if (DiplomacyEventPopup.IsPreparing) { _pending = stage; _screen = screen; return; }
            InformationManager.DisplayMessage(new InformationMessage(T("PreviewUnavailable", "The event preview could not open. Close other dialogs and try again.")));
        }
    }

    private static string T(string key, string fallback) => DiplomacyEventPopupVM.Text(key, fallback);
    internal static DiplomacyEventPresentation Create(DiplomacyEventStage stage)
    {
        bool start = stage == DiplomacyEventStage.Started;
        return new DiplomacyEventPresentation("terminal-event-preview", SaveRuntimeGuard.CaptureGeneration(), stage,
            start ? T("PreviewStartTitle", "The bloodstained mantle (preview)") : T("PreviewEndTitle", "A weary peace (preview)"),
            start
                ? T("PreviewStartBody", "Rhagaea raises the late emperor's bloodstained mantle once more, accusing the northern Senate of complicity in his death. Lucon denies the charge, claiming the south is exploiting an old crime to seize the throne.\n\nNow the southern eagle standards advance north. The truth remains uncertain, but an old grievance has become a new war.")
                : T("PreviewEndBody", "After a long campaign, neither side has secured a decisive advantage. Exhausted armies return to their camps as envoys agree to suspend the fighting.\n\nThe accusations surrounding the emperor's death remain unresolved. The treaty quiets the border, but does not settle the grievance."),
            new[] { T("South", "Southern Empire"), T("North", "Northern Empire") },
            start ? new[] {
                new DiplomacyEventEffect(T("PreviewMobilization", "Southern Empire · Northern campaign: increased recruitment growth"), DiplomacyEventEffectTone.Beneficial),
                new DiplomacyEventEffect(T("PreviewBorderPressure", "Northern Empire · Border pressure: reduced prosperity growth"), DiplomacyEventEffectTone.Harmful)
            } : new[] {
                new DiplomacyEventEffect(T("PreviewRemoveMobilization", "Southern Empire · Northern campaign: recruitment bonus removed")),
                new DiplomacyEventEffect(T("PreviewRemovePressure", "Northern Empire · Border pressure: prosperity penalty removed"), DiplomacyEventEffectTone.Beneficial),
                new DiplomacyEventEffect(T("PreviewRecovery", "Both empires · Postwar recovery: temporary recovery effects begin"), DiplomacyEventEffectTone.Beneficial)
            },
            start ? DiplomacyEventScene.Muster : DiplomacyEventScene.Negotiation, "empire",
            start ? "12-imperial-plaza-review" : "00-approved-armistice",
            T("PreviewNotice", "Preview only. No event is published and no gameplay effects are applied."));
    }
}
