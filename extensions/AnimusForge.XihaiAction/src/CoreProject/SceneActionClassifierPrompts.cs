namespace AnimusForge.SceneActions.Core
{
    // Immutable, language-explicit snapshot. It has no game/thread dependencies.
    public sealed class SceneActionClassifierPrompts
    {
        public string SystemPromptPreamble { get; }
        public string ConsentSystemPrompt { get; }
        public string BattleSpeechTriggerSystemPrompt { get; }
        public string BattleSpeechPlanSystemPromptPreamble { get; }

        public SceneActionClassifierPrompts(bool useChinese)
        {
            SystemPromptPreamble = SceneActionCatalogText.Prompt(nameof(SystemPromptPreamble), useChinese);
            ConsentSystemPrompt = SceneActionCatalogText.Prompt(nameof(ConsentSystemPrompt), useChinese);
            BattleSpeechTriggerSystemPrompt = SceneActionCatalogText.Prompt(nameof(BattleSpeechTriggerSystemPrompt), useChinese);
            BattleSpeechPlanSystemPromptPreamble = SceneActionCatalogText.Prompt(nameof(BattleSpeechPlanSystemPromptPreamble), useChinese);
        }
    }
}
