using Newtonsoft.Json;

namespace AnimusForge;

// A revision owns detached models. Public-facing reads cannot mutate the published graph.
internal sealed class PromptConfigurationSnapshot
{
    private readonly AIConfigModel _main;
    private readonly GuardrailConfigModel _guardrail;
    private readonly ActionPostprocessConfigModel _actionPostprocess;
    private readonly PreprocessPromptsConfigModel _preprocess;
    private readonly ProactiveNpcRequestPromptsConfigModel _proactiveRequest;
    private readonly RpItemIntroductionPromptsConfigModel _rpItemIntroduction;

    internal AIConfigModel Main => Clone(_main);
    internal GuardrailConfigModel Guardrail => Clone(_guardrail);
    internal ActionPostprocessConfigModel ActionPostprocess => Clone(_actionPostprocess);
    internal PreprocessPromptsConfigModel Preprocess => Clone(_preprocess);
    internal ProactiveNpcRequestPromptsConfigModel ProactiveRequest => Clone(_proactiveRequest);
    internal RpItemIntroductionPromptsConfigModel RpItemIntroduction => Clone(_rpItemIntroduction);
    internal string PreprocessLoadError { get; }

    // Only AIConfigHandler uses these read-only borrowed references on the prompt hot path.
    internal AIConfigModel ReadMainForOwner() => _main;
    internal GuardrailConfigModel ReadGuardrailForOwner() => _guardrail;
    internal ActionPostprocessConfigModel ReadActionPostprocessForOwner() => _actionPostprocess;
    internal PreprocessPromptsConfigModel ReadPreprocessForOwner() => _preprocess;
    internal ProactiveNpcRequestPromptsConfigModel ReadProactiveRequestForOwner() => _proactiveRequest;
    internal RpItemIntroductionPromptsConfigModel ReadRpItemIntroductionForOwner() => _rpItemIntroduction;

    internal PromptConfigurationSnapshot(AIConfigModel main, GuardrailConfigModel guardrail,
        ActionPostprocessConfigModel actionPostprocess, PreprocessPromptsConfigModel preprocess,
        ProactiveNpcRequestPromptsConfigModel proactiveRequest, RpItemIntroductionPromptsConfigModel rpItemIntroduction,
        string preprocessLoadError)
    {
        _main = Clone(main);
        _guardrail = Clone(guardrail);
        _actionPostprocess = Clone(actionPostprocess);
        _preprocess = Clone(preprocess);
        _proactiveRequest = Clone(proactiveRequest);
        _rpItemIntroduction = Clone(rpItemIntroduction);
        PreprocessLoadError = preprocessLoadError ?? "";
    }

    private static T Clone<T>(T value) where T : class => value == null
        ? null
        : JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
}
