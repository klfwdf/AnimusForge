namespace AnimusForge.Illustrator.Engine
{
    public static partial class ScreenCaptureHelper
    {
        internal const string ScreenshotMaskReferenceNote =
            "不采集游戏窗口截图；环境只依据独立场景参考和文字事实。";

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
