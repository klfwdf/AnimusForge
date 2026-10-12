namespace AnimusForge.Illustrator.Engine
{
    public static partial class ScreenCaptureHelper
    {
        internal static string ScreenshotMaskReferenceNote => IllustratorText.Get("Ref_PassiveScreenshotMask");

        internal static string CaptureUnobstructedConversationSceneBase64()
            => CaptureUnobstructedConversationSceneBase64(out _);

        internal static string CaptureUnobstructedConversationSceneBase64(out string reason)
        {
            // Compatibility entry point: no screen pixels or UI are read.
            reason = "disabled_ui_screenshot_reference";
            return null;
        }
    }
}
