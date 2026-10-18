using System;

namespace AnimusForge
{
    internal sealed class DuelSettings { internal static readonly DuelSettings Value=new(); internal bool EnableTtsSpeech,TtsUseGptSoVits; internal static DuelSettings GetSettings()=>Value; }
    internal static class GptSoVitsVoiceRuntime { internal static bool IsBattleBlocked; }
    internal static class Logger { internal static void LogTrace(string category, string text) { } }
    internal static class FreezeWatchdog { internal static void Mark(string name, string detail) { } }
    internal static class LlmRetryPrompt { internal static string ProjectKnownErrorForUi(string text) => text; }
    public sealed partial class AnimusForgeNativeConversationOverlay
    {
        private int _displayTextGeneration = -1;
        private string _submissionDisplayText;
        internal int Generation = 1;
        private bool IsSubmitGenerationCurrent(int generation) => generation == Generation;
        internal void Preview(int generation, string text) => SetSubmissionStreamPreview(generation, text);
        internal void Complete(int generation, string plain, string formatted) => SetSubmissionDisplayText(generation, formatted, plain);
    }
}
namespace TaleWorlds.GauntletUI { public sealed class UIContext { } }
namespace TaleWorlds.GauntletUI.BaseTypes
{
    public sealed class ScrollbarWidget { public float MaxValue, ValueFloat; }
    public class ScrollablePanel
    {
        public float MouseScrollSpeed;
        public ScrollbarWidget VerticalScrollbar = new();
        public float NextLayoutMaximum;
        public ScrollablePanel(TaleWorlds.GauntletUI.UIContext context) { }
        protected virtual void OnLateUpdate(float dt) { VerticalScrollbar.MaxValue = NextLayoutMaximum; }
        protected virtual bool OnPreviewMousePressed() => true;
        protected virtual bool OnPreviewMouseReleased() => true;
        protected virtual bool OnPreviewMouseAlternatePressed() => true;
        protected virtual bool OnPreviewMouseAlternateReleased() => true;
    }
}
