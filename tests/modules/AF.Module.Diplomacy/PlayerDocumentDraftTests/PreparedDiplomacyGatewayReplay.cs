using System.Net;
using AnimusForge;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json.Linq;

internal static class PreparedDiplomacyGatewayReplay
{
    private static LlmGenerateRequest Request() => new(
        new TraceContext("prepared-diplomacy-test", SaveRuntimeGuard.CaptureGeneration(), 0, "single-player", "shared"),
        new LlmProviderSnapshot("world-diplomacy", "legacy://world-diplomacy", "world-diplomacy", 90000, 2000),
        LegacyWorldDiplomacyLlmGateway.BuildPromptPackage(new JArray(
            new JObject { ["role"] = "system", ["content"] = "Frozen diplomacy rules" },
            new JObject { ["role"] = "user", ["content"] = "Original source" }), 2000), InteractionStage.MainReply);

    private static ControlledHandler Handler()
    {
        DuelSettings.Current = new DuelSettings();
        DuelSettings.StrictThread = true;
        var handler = new ControlledHandler();
        DuelSettings.GlobalClient = new HttpClient(handler);
        return handler;
    }

    internal static void Run(bool legacyWorkerNegativeControl = false)
    {
        var handler = Handler();
        var request = Request();
        int sends = 0;
        Func<bool> admit = () => { Interlocked.Increment(ref sends); return true; };
        var gateway = legacyWorkerNegativeControl ? new LegacyWorldDiplomacyLlmGateway(admit)
            : new LegacyWorldDiplomacyLlmGateway(request, admit);
        int reads = DuelSettings.Reads;
        var task = Task.Run(() => gateway.GenerateAsync(request, CancellationToken.None));
        Test.Until(() => handler.Get(0) != null || task.IsCompleted, "prepared worker starts");
        Test.That(handler.Get(0) != null, "main-thread prepared gateway must reach HTTP with strict MCM thread checks enabled");
        DuelSettings.Current.EventAndRebellionApiUrl = "https://changed.example/v1/chat/completions";
        DuelSettings.Current.EventAndRebellionApiKey = "changed-fixture";
        DuelSettings.Current.EventAndRebellionModelName = "changed-model";
        DuelSettings.Current.EventAndRebellionApiMaxTokens = 512;
        DuelSettings.Current.EventTemperature = 1.9f;
        handler.Finish(0, "retry", status: HttpStatusCode.InternalServerError);
        Test.Until(() => handler.Get(1) != null || task.IsCompleted, "retry after asynchronous provider failure");
        Test.That(handler.Get(1) != null, "original retry policy survives an asynchronous failure");
        Test.That(handler.Get(1).Url == handler.Get(0).Url && handler.Get(1).Auth == handler.Get(0).Auth
            && JToken.DeepEquals(handler.Get(1).Body, handler.Get(0).Body),
            "retry keeps original address, credential, model, temperature, tokens and messages after settings change");
        handler.Finish(1, "final-output");
        var result = task.GetAwaiter().GetResult();
        Test.That(result.Status == LlmResultStatus.Succeeded && result.RawText == "final-output", "prepared gateway returns actual provider output");
        Test.That(DuelSettings.Reads == reads && sends == 2, "worker and retry do not read MCM; each HTTP attempt is budgeted");
        CheckFallbackBudget();
        CheckCancellation();
        CheckPreparationFailure();
    }

    private static void CheckFallbackBudget()
    {
        var handler = Handler();
        var request = Request();
        int admissions = 0;
        var gateway = new LegacyWorldDiplomacyLlmGateway(request, () => Interlocked.Increment(ref admissions) == 1);
        var task = Task.Run(() => gateway.GenerateAsync(request, CancellationToken.None));
        Test.Until(() => handler.Get(0) != null, "budgeted thinking request");
        handler.Get(0).Reply.SetResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            { Content = new StringContent("unsupported reasoning_effort") });
        var result = task.GetAwaiter().GetResult();
        Test.That(result.ErrorCode == "world_diplomacy_request_budget_deferred" && admissions == 2 && handler.Get(1) == null,
            "thinking fallback cannot bypass the real-send budget or trigger an outer retry after deferral");
    }

    private static void CheckCancellation()
    {
        var handler = Handler();
        var request = Request();
        using var cancellation = new CancellationTokenSource();
        var gateway = new LegacyWorldDiplomacyLlmGateway(request, () => true);
        var task = Task.Run(() => gateway.GenerateAsync(request, cancellation.Token));
        Test.Until(() => handler.Get(0) != null, "cancellable prepared request");
        cancellation.Cancel();
        handler.Finish(0, "late-output");
        Test.That(task.GetAwaiter().GetResult().Status == LlmResultStatus.Cancelled && handler.Get(1) == null,
            "cancelled late provider output cannot succeed or start another request");
    }

    private static void CheckPreparationFailure()
    {
        var handler = Handler();
        DuelSettings.Current.ApiUrl = DuelSettings.Current.EventAndRebellionApiUrl = "";
        DuelSettings.Current.ApiKey = DuelSettings.Current.EventAndRebellionApiKey = "";
        var request = Request();
        var gateway = new LegacyWorldDiplomacyLlmGateway(request, () => true);
        int reads = DuelSettings.Reads;
        var result = Task.Run(() => gateway.GenerateAsync(request, CancellationToken.None)).GetAwaiter().GetResult();
        Test.That(result.Status != LlmResultStatus.Succeeded && handler.Get(0) == null && DuelSettings.Reads == reads,
            "missing configuration becomes a failed completion without a worker settings read or HTTP request");
    }
}
