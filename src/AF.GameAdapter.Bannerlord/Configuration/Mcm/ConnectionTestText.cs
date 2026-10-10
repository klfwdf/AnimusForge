using System;

namespace AnimusForge;

// Captured once on the settings button's UI thread. Background requests only concatenate
// these immutable strings; they do not query localization or inspect game objects.
internal sealed class ConnectionTestText
{
    internal string Stage { get; }
    internal string Started { get; }
    internal string SuccessPrefix { get; }
    internal string ExceptionPrefix { get; }
    internal string InvalidFormatPrefix { get; }
    internal string ReasoningRetryPrefix { get; }
    internal string RetrySucceededPrefix { get; }
    internal string FirstResponseHeader { get; }
    internal string RetryResponseHeader { get; }
    internal string PromptSystem { get; }
    internal string PromptUser { get; }
    private readonly string _failureStatus;
    private readonly string _failureError;

    private static string Text(string suffix, string fallback)
        => DuelSettingsUiLocalization.Text("AF_RUNTIME_ApiTest_" + suffix, fallback);

    internal static ConnectionTestText Capture(string stage) => new ConnectionTestText(stage);

    private ConnectionTestText(string stage)
    {
        string label = stage switch
        {
            "Main" => "Main API",
            "Preprocess" => "Preprocessing API",
            "Postprocess" => "Postprocessing API",
            "Event" => "Event/rebellion API",
            "Ambient" => "Ambient AI API",
            _ => throw new ArgumentOutOfRangeException(nameof(stage))
        };
        Stage = Text("Stage_" + stage, label);
        Started = Stage + Text("Started", ": connection test requested.");
        SuccessPrefix = Stage + Text("Success", ": connection test succeeded. Reply: ");
        ExceptionPrefix = Stage + Text("Exception", ": connection test error: ");
        InvalidFormatPrefix = Stage + Text("InvalidFormat", ": reachable, but the preprocessing response format is invalid: ");
        ReasoningRetryPrefix = Text("ReasoningRetry", "Postprocessing test used its token budget on reasoning only; retrying with reasoning disabled: completion_tokens=");
        RetrySucceededPrefix = Text("RetrySucceeded", "Postprocessing test succeeded after disabling reasoning: control_mode=");
        FirstResponseHeader = Text("FirstResponse", "[First response]\n");
        RetryResponseHeader = Text("RetryResponse", "\n\n[Retry response with reasoning disabled]\n");
        _failureStatus = Text("FailureStatus", ": connection test failed. Status: ");
        _failureError = Text("FailureError", " | Error: ");
        PromptSystem = "";
        PromptUser = "";
        if (stage == "Main")
            PromptUser = Text("MainPrompt", "I am an adventurer. Hello! (Play a cheerful child named Habo and give a warm reply of roughly 20 words.)");
        else if (stage == "Postprocess")
        {
            PromptSystem = Text("PostprocessSystem", "You are an action-tag emitter. Output only tags.");
            PromptUser = Text("PostprocessPrompt", "Output only [ACTION:MOOD:NEUTRAL]");
        }
        else if (stage == "Event")
        {
            PromptSystem = Text("EventSystem", "You are a test echo assistant. Output only one short reply.");
            PromptUser = Text("EventPrompt", "Please reply: Event and rebellion endpoint connected");
        }
    }

    internal string Failure(object status, string error)
        => Stage + _failureStatus + status + _failureError + error;
}
