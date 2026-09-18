namespace AnimusForge;

// A revision owns one complete set of parsed configuration models.
internal sealed class PromptConfigurationSnapshot
{
    internal AIConfigModel Main { get; }
    internal GuardrailConfigModel Guardrail { get; }
    internal ActionPostprocessConfigModel ActionPostprocess { get; }
    internal PreprocessPromptsConfigModel Preprocess { get; }
    internal ProactiveNpcRequestPromptsConfigModel ProactiveRequest { get; }
    internal RpItemIntroductionPromptsConfigModel RpItemIntroduction { get; }
    internal string PreprocessLoadError { get; }

    internal PromptConfigurationSnapshot(AIConfigModel main, GuardrailConfigModel guardrail,
        ActionPostprocessConfigModel actionPostprocess, PreprocessPromptsConfigModel preprocess,
        ProactiveNpcRequestPromptsConfigModel proactiveRequest, RpItemIntroductionPromptsConfigModel rpItemIntroduction,
        string preprocessLoadError)
    {
        Main = main;
        Guardrail = guardrail;
        ActionPostprocess = actionPostprocess;
        Preprocess = preprocess;
        ProactiveRequest = proactiveRequest;
        RpItemIntroduction = rpItemIntroduction;
        PreprocessLoadError = preprocessLoadError ?? "";
    }
}
