using System.Net;
using System.Net.Sockets;
using System.Text;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static PromptPackage BuildPrompt(string model = "replay-model")
{
    return new PromptPackage(
        new[]
        {
            new PromptMessage("system", "replay system"),
            new PromptMessage("user", "reply with a short answer")
        },
        96,
        model);
}

static LlmGenerateRequest BuildRequest(string endpoint, string model = "replay-model", int timeout = 2000)
{
    return new LlmGenerateRequest(
        new TraceContext("replay-trace", 1, 1, "replay", "1.4"),
        new LlmProviderSnapshot("replay", endpoint, model, timeout, 96),
        BuildPrompt(model));
}

await using (ReplayServer success = await ReplayServer.StartAsync((_, _) =>
    (200, "{\"choices\":[{\"message\":{\"content\":\"replay ok\"}}]}")))
{
    LegacyConfiguredChatGateway gateway = new LegacyConfiguredChatGateway(_ => "secret", disableThinking: true);
    LlmGenerateResult result = await gateway.GenerateAsync(BuildRequest(success.Url), CancellationToken.None);
    AssertTrue(result.Status == LlmResultStatus.Succeeded && result.RawText == "replay ok", "success replay did not extract assistant text");
    AssertTrue(success.Requests.Count == 1, "success replay sent an unexpected request count");
    AssertTrue(success.Requests[0].Authorization == "Bearer secret", "credential did not stay at the send boundary");
    AssertTrue(success.Requests[0].Body.Contains("replay-model", StringComparison.Ordinal), "model was not sent");
}

await using (ReplayServer streaming = await ReplayServer.StartAsync((_, _) =>
    (200, "data: {\"choices\":[{\"delta\":{\"content\":\"stream \"}}]}\n\n"
        + "data: {\"choices\":[{\"delta\":{\"content\":\"reply\"}}]}\n\n"
        + "data: [DONE]\n\n")))
{
    List<string> deltas = new List<string>();
    LegacyConfiguredChatGateway gateway = new LegacyConfiguredChatGateway(_ => "secret", disableThinking: true);
    ConfiguredChatGenerationExchange exchange = await gateway.GenerateExchangeAsync(
        BuildRequest(streaming.Url),
        streamResponse: true,
        onDelta: deltas.Add,
        CancellationToken.None);
    AssertTrue(exchange.Result.Status == LlmResultStatus.Succeeded, "streaming replay did not succeed");
    AssertTrue(exchange.Result.RawText == "stream reply", "streaming replay final text was duplicated or incomplete: " + exchange.Result.RawText);
    AssertTrue(deltas.SequenceEqual(new[] { "stream ", "reply" }), "streaming delta sequence mismatch: " + string.Join("|", deltas));
    AssertTrue(exchange.RawStreamSample.Contains("[DONE]", StringComparison.Ordinal) == false, "stream sample should not include terminal marker");
    AssertTrue(streaming.Requests.Count == 1 && streaming.Requests[0].Body.Contains("\"stream\":true", StringComparison.Ordinal), "streaming request did not set stream=true");
}

await using (ReplayServer thinkingRetry = await ReplayServer.StartAsync((index, _) => index == 1
    ? (400, "thinking unsupported; reject this control")
    : (200, "{\"choices\":[{\"message\":{\"content\":\"plain retry ok\"}}]}")))
{
    LegacyConfiguredChatGateway gateway = new LegacyConfiguredChatGateway(
        _ => "secret",
        disableThinking: false,
        retryWithoutThinkingOnBadRequest: true,
        thinkingEnabled: true,
        reasoningEffort: "high");
    LlmGenerateResult result = await gateway.GenerateAsync(BuildRequest(thinkingRetry.Url), CancellationToken.None);
    AssertTrue(result.Status == LlmResultStatus.Succeeded && result.RawText == "plain retry ok", "thinking plain retry did not recover");
    AssertTrue(thinkingRetry.Requests.Count == 2, "thinking retry did not send exactly two requests");
    AssertTrue(thinkingRetry.Requests[0].Body.Contains("thinking", StringComparison.Ordinal), "first request lacked thinking control");
    AssertTrue(!thinkingRetry.Requests[1].Body.Contains("thinking", StringComparison.Ordinal), "plain retry retained thinking control");
}

await using (ReplayServer serverError = await ReplayServer.StartAsync((_, _) =>
    (503, "temporary provider failure")))
{
    LegacyConfiguredChatGateway gateway = new LegacyConfiguredChatGateway(_ => "secret");
    LlmGenerateResult result = await gateway.GenerateAsync(BuildRequest(serverError.Url), CancellationToken.None);
    AssertTrue(result.Status == LlmResultStatus.RetryableFailure && result.ErrorCode == "http_503", "5xx replay was not retryable");
}

await using (ReplayServer boundedMemory = await ReplayServer.StartAsync((_, _) =>
    (400, "thinking unsupported; reject this control")))
{
    LegacyConfiguredChatGateway gateway = new LegacyConfiguredChatGateway(
        _ => "fixture", thinkingEnabled: false, retryWithoutThinkingOnBadRequest: false);
    var request = new LlmGenerateRequest(
        new TraceContext("bounded-town-memory", 1, 1, "replay", "1.4"),
        new LlmProviderSnapshot("replay", boundedMemory.Url, "replay-model", 2000, 384),
        new PromptPackage(new[] { new PromptMessage("user", "fixture") }, 384, "replay-model"));
    LlmGenerateResult result = await gateway.GenerateAsync(request, CancellationToken.None);
    AssertTrue(result.Status != LlmResultStatus.Succeeded && boundedMemory.Requests.Count == 1,
        "bounded town memory unexpectedly retried a failed attempt");
    var payload = Newtonsoft.Json.Linq.JObject.Parse(boundedMemory.Requests[0].Body);
    AssertTrue((int)payload["max_tokens"] == 384 && (bool)payload["thinking"]["enabled"] == false,
        "bounded town memory token/thinking controls changed in HTTP serialization");
}

await using (ReplayServer slow = await ReplayServer.StartAsync(async (_, _) =>
{
    await Task.Delay(5000);
    return (200, "{\"choices\":[{\"message\":{\"content\":\"late\"}}]}");
}))
{
    LegacyConfiguredChatGateway gateway = new LegacyConfiguredChatGateway(_ => "secret");
    using CancellationTokenSource cancellation = new CancellationTokenSource(100);
    LlmGenerateResult result = await gateway.GenerateAsync(BuildRequest(slow.Url, timeout: 5000), cancellation.Token);
    AssertTrue(result.Status == LlmResultStatus.Cancelled && result.ErrorCode == "cancelled", "cancellation replay was not isolated");
}

Console.WriteLine("PASS configuredGatewayReplay success=1 streaming=1 thinkingPlainRetry=1 retryable5xx=1 cancellation=1 credentialBoundary=1");

// The transport tests above remain unchanged. These cases execute the production
// application adapter with only MCM/game/diagnostic leaves replaced by fixtures.
AnimusForge.DuelSettings ConfigureApplication(string endpoint)
{
    var settings = new AnimusForge.DuelSettings { ApiUrl = endpoint, ApiKey = "fixture-only", ModelName = "main-fixture" };
    AnimusForge.DuelSettings.Current = settings;
    AnimusForge.DuelSettings.SettingsReads = 0;
    return settings;
}

AssertTrue(!LlmRequestConfigurationCaptureAdapter.TryResolveUniversalApiConfig(null, ConfiguredChatRoute.Main,
    out _, out _, out _, out string missingRoute, out string missingError)
    && missingRoute == "main" && missingError.Contains("MCM"), "missing configuration result changed");
var routeSettings = ConfigureApplication("http://127.0.0.1/unused");
routeSettings.EventAndRebellionApiKey = "partial-fixture";
AssertTrue(LlmRequestConfigurationCaptureAdapter.TryResolveUniversalApiConfig(routeSettings, ConfiguredChatRoute.EventAndRebellion,
    out _, out _, out _, out string eventRoute, out _) && eventRoute == "event_rebellion_partial_fallback_main",
    "event partial configuration lost main fallback");
AssertTrue(LlmRequestConfigurationCaptureAdapter.ResolveUniversalMaxTokens(routeSettings, eventRoute) == routeSettings.EventTokens
    && LlmRequestConfigurationCaptureAdapter.ResolveUniversalApiTemperature(routeSettings, eventRoute) == routeSettings.MainTemperature,
    "partial event fallback token/temperature distinction changed");
routeSettings.EventAndRebellionApiThinkingEnabled = false;
LlmRequestConfigurationCaptureAdapter.ResolveUniversalThinkingSettings(routeSettings, eventRoute, out bool eventThinking, out _);
AssertTrue(!eventThinking, "partial event fallback thinking did not retain event policy");
routeSettings.AuxiliaryApiKey = "partial-fixture";
AssertTrue(LlmRequestConfigurationCaptureAdapter.TryResolveUniversalApiConfig(routeSettings, ConfiguredChatRoute.Auxiliary,
    out _, out _, out _, out string auxRoute, out _) && auxRoute == "auxiliary_partial_fallback_main"
    && LlmRequestConfigurationCaptureAdapter.ResolveUniversalMaxTokens(routeSettings, auxRoute) == routeSettings.MainTokens,
    "auxiliary partial fallback changed");

await using (ReplayServer appSuccess = await ReplayServer.StartAsync((_, _) =>
    (200, "{\"choices\":[{\"message\":{\"content\":\"<think>hidden</think>**reply**\"}}]}")))
{
    var settings = ConfigureApplication(appSuccess.Url);
    int tokenLogs = AnimusForge.Logger.TokenStats;
    var result = await ConfiguredChatApplicationAdapter.CallUniversalApiDetailed("system", "user", streamResponse: false);
    AssertTrue(result.Success && result.Content == "reply" && result.StatusCode == 200, "application success/clean/result mapping changed");
    AssertTrue(AnimusForge.DuelSettings.SettingsReads == 1 && appSuccess.Requests.Count == 1, "application repeated configuration capture");
    var payload = Newtonsoft.Json.Linq.JObject.Parse(appSuccess.Requests.Single().Body);
    AssertTrue((int)payload["max_tokens"] == settings.MainTokens && (float)payload["temperature"] == settings.MainTemperature,
        "application configured max tokens/temperature changed");
    AssertTrue(AnimusForge.Logger.TokenStats == tokenLogs + 1, "universal successful request lost token accounting");
}

foreach (var fixture in new[]
{
    (Body: "quota exceeded requests per minute", Quota: true, Rpm: false),
    (Body: "requests per minute limit", Quota: false, Rpm: true),
    (Body: "too many requests", Quota: false, Rpm: false)
})
{
    await using var limited = await ReplayServer.StartAsync((_, _) => (429, fixture.Body));
    ConfigureApplication(limited.Url);
    var result = await ConfiguredChatApplicationAdapter.CallUniversalApiDetailed("system", "user", streamResponse: false);
    AssertTrue(!result.Success && result.StatusCode == 429 && result.ResponseBody == fixture.Body
        && result.IsQuotaLimit == fixture.Quota && result.IsRequestsPerMinuteLimit == fixture.Rpm,
        "application quota/RPM precedence or diagnostic body changed");
}

await using (ReplayServer stale = await ReplayServer.StartAsync((_, _) =>
{
    AnimusForge.SaveRuntimeGuard.AdvanceGeneration("application-replay-old-generation");
    return (200, "{\"choices\":[{\"message\":{\"content\":\"late reply\"}}]}");
}))
{
    ConfigureApplication(stale.Url);
    var result = await ConfiguredChatApplicationAdapter.CallWeeklyReportApiDetailed("system", "user");
    AssertTrue(!result.Success && result.ErrorMessage == AnimusForge.SaveRuntimeGuard.BuildStaleRequestErrorText()
        && string.IsNullOrEmpty(result.Content) && stale.Requests.Count == 1,
        "late generation response accepted, or discard mislabeled as network cancellation");
}

await using (ReplayServer fallback = await ReplayServer.StartAsync((index, _) => index == 1
    ? (400, "thinking unsupported; reject this control")
    : (200, "{\"choices\":[{\"message\":{\"content\":\"app fallback\"}}]}")))
{
    ConfigureApplication(fallback.Url);
    var result = await ConfiguredChatApplicationAdapter.CallWeeklyReportApiDetailed("system", "user");
    AssertTrue(result.Success && result.Content == "app fallback" && fallback.Requests.Count == 2
        && AnimusForge.DuelSettings.SettingsReads == 1, "application thinking fallback recaptured configuration or changed attempt count");
}

await using (ReplayServer auxiliary = await ReplayServer.StartAsync((_, _) =>
    (200, "{\"choices\":[{\"message\":{\"content\":\"aux reply\"}}]}")))
{
    var settings = ConfigureApplication(auxiliary.Url);
    settings.AuxiliaryApiUrl = auxiliary.Url;
    settings.AuxiliaryApiKey = "aux-fixture";
    settings.AuxiliaryModelName = "aux-model";
    var result = await ConfiguredChatApplicationAdapter.CallAuxiliaryGatewayDetailed("system", "user", "fixture", 73, true);
    var payload = Newtonsoft.Json.Linq.JObject.Parse(auxiliary.Requests.Single().Body);
    AssertTrue(result.Success && result.Content == "aux reply" && (int)payload["max_tokens"] == 73
        && payload["model"].ToString() == "aux-model" && (bool)payload["thinking"]["enabled"] == false
        && payload["thinking"]["effort"].ToString() == "low" && AnimusForge.DuelSettings.SettingsReads == 1,
        "auxiliary explicit token override/force-thinking-disabled changed");
}

using (var retryAfter = new System.Net.Http.HttpResponseMessage(HttpStatusCode.TooManyRequests))
{
    retryAfter.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromMilliseconds(2100));
    AssertTrue(ConfiguredChatApplicationAdapter.TryGetRetryAfterSeconds(retryAfter) == 3, "RetryAfter fractional delta no longer rounds up");
}
AssertTrue(!ConfiguredChatApplicationAdapter.IsQuotaLimitResponseBody("temporary provider failure")
    && ConfiguredChatApplicationAdapter.IsRequestsPerMinuteLimitResponseBody("每分钟请求超过限制"), "limit classifiers changed");
foreach (string malformed in new[] { "{\"choices\":[{\"message\":{\"content\":\"\"}}]}", "{\"choices\":[" })
{
    await using var malformedServer = await ReplayServer.StartAsync((_, _) => (200, malformed));
    ConfigureApplication(malformedServer.Url);
    var result = await ConfiguredChatApplicationAdapter.CallUniversalApiDetailed("system", "user", streamResponse: false);
    bool isEmpty = malformed.EndsWith("}]}", StringComparison.Ordinal);
    AssertTrue(!result.Success && !string.IsNullOrWhiteSpace(result.ErrorMessage)
        && (isEmpty ? result.StatusCode == 200 && result.ResponseBody == malformed
            : result.StatusCode == null && string.IsNullOrEmpty(result.ResponseBody)
                && result.ErrorMessage == "configured_gateway_JsonReaderException"),
        "empty HTTP-result versus truncated gateway-exception mapping changed");
}
int beforeLookupPopup = AnimusForge.LlmRetryPrompt.Popups;
string failedLookup = await ConfiguredChatApplicationAdapter.CallAuxiliaryApiTextForExternal("system", "user", "lookup-failure", () => throw new InvalidOperationException("synthetic lookup failure"));
AssertTrue(failedLookup == "" && AnimusForge.LlmRetryPrompt.Popups == beforeLookupPopup + 1,
    "auxiliary owner lookup exception escaped original guarded popup/empty boundary");
// Request options freeze once; changing the live settings cannot change an in-flight request.
foreach (bool detailed in new[] { false, true })
foreach (bool preserve in new[] { false, true })
{
    AnimusForge.DuelSettings.Current = new AnimusForge.DuelSettings { UseDetailedSceneSpeechPrompt=detailed, PreserveSceneAsteriskActions=preserve };
    AnimusForge.DuelSettings.SettingsReads=0;
    var options=LlmRequestConfigurationCaptureAdapter.CaptureSceneSpeechTextOptions();
    AssertTrue(options.Detailed==detailed && options.PreserveAsterisk==preserve && AnimusForge.DuelSettings.SettingsReads==1, "scene option boolean capture changed");
    AnimusForge.DuelSettings.Current.UseDetailedSceneSpeechPrompt=!detailed;
    AnimusForge.DuelSettings.Current.PreserveSceneAsteriskActions=!preserve;
    AssertTrue(options.Detailed==detailed && options.PreserveAsterisk==preserve, "in-flight options followed mutable settings");
    var next=LlmRequestConfigurationCaptureAdapter.CaptureSceneSpeechTextOptions();
    AssertTrue(next.Detailed!=detailed && next.PreserveAsterisk!=preserve, "next request lost setting toggle");
}
AnimusForge.DuelSettings.SettingsNull=true;
var nullOptions=LlmRequestConfigurationCaptureAdapter.CaptureSceneSpeechTextOptions();
AssertTrue(!nullOptions.Detailed && !nullOptions.PreserveAsterisk,"null scene settings changed defaults");
AnimusForge.DuelSettings.SettingsNull=false;
AnimusForge.DuelSettings.SettingsThrow=true;
var failedOptions=LlmRequestConfigurationCaptureAdapter.CaptureSceneSpeechTextOptions();
AssertTrue(!failedOptions.Detailed && !failedOptions.PreserveAsterisk,"throwing scene settings changed defaults");
AnimusForge.DuelSettings.SettingsThrow=false;
AnimusForge.DuelSettings.SettingsNull=false; AnimusForge.DuelSettings.SettingsThrow=false;
AnimusForge.DuelSettings.Current=new AnimusForge.DuelSettings { MemoryCompressionDenominator=-10, MemoryOverviewStartBlockCount=99, MemoryOverviewTargetChars=2 };
AssertTrue(LlmRequestConfigurationCaptureAdapter.GetMemoryCompressionDenominatorFromSettings()==3
    && LlmRequestConfigurationCaptureAdapter.GetMemoryOverviewStartBlockCountFromSettings()==10
    && LlmRequestConfigurationCaptureAdapter.GetMemoryOverviewTargetCharsFromSettings()==100,"summary config clamp boundary changed");
AnimusForge.DuelSettings.Current.MemoryCompressionDenominator=40;
AnimusForge.DuelSettings.Current.MemoryOverviewStartBlockCount=0;
AnimusForge.DuelSettings.Current.MemoryOverviewTargetChars=9999;
AssertTrue(LlmRequestConfigurationCaptureAdapter.GetMemoryCompressionDenominatorFromSettings()==10
    && LlmRequestConfigurationCaptureAdapter.GetMemoryOverviewStartBlockCountFromSettings()==3
    && LlmRequestConfigurationCaptureAdapter.GetMemoryOverviewTargetCharsFromSettings()==1000,"summary inverse clamp boundary changed");
foreach(bool throws in new[]{false,true}) {
 AnimusForge.DuelSettings.SettingsNull=!throws; AnimusForge.DuelSettings.SettingsThrow=throws;
 int readCount=AnimusForge.DuelSettings.SettingsReads;
 AssertTrue(LlmRequestConfigurationCaptureAdapter.GetMemoryCompressionDenominatorFromSettings()==5
    && LlmRequestConfigurationCaptureAdapter.GetMemoryOverviewStartBlockCountFromSettings()==5
    && LlmRequestConfigurationCaptureAdapter.GetMemoryOverviewTargetCharsFromSettings()==200,"summary null/throw defaults changed");
 AssertTrue(AnimusForge.DuelSettings.SettingsReads==readCount+3,"summary scalar config repeated read");
}
AnimusForge.DuelSettings.SettingsNull=false; AnimusForge.DuelSettings.SettingsThrow=false;
Console.WriteLine("PASS summaryConfiguration clampCases=6 nullDefaults=3 throwDefaults=3 captureOncePerScalar=1");
Console.WriteLine("PASS sceneRequestOptions boolCases=4 null=1 throw=1 toggleFrozen=4");
Console.WriteLine("PASS configuredApplicationReplay partialRoutes=2 success=1 quotaRpm=3 stale=1 thinkingFallback=1 auxiliary=1 retryAfter=1 captureOnce=1");


// Whole current Native application over exact gateway wrappers and controlled lower-network callbacks.
// Timeouts use the real unchanged 180000ms production timeout; no source rewrite or fake timer.
var nativeMessages = new List<object> { new { role = "user", content = "native replay" } };
var nativeOptions = new AnimusForge.ConversationSpeechTextOptions(false, false);
AnimusForge.ShoutNetwork.NonStream = (_, token) => Task.Delay(Timeout.Infinite, token).ContinueWith<string>(
    _ => throw new OperationCanceledException(token), TaskScheduler.Default);
AnimusForge.ShoutNetwork.Stream = async (_, chunk, complete, error, token) =>
{
    try { await Task.Delay(Timeout.Infinite, token); } catch (OperationCanceledException) { }
};
Task<string> nonStreamTimeout = NativeConversationLlmApplicationAdapter.CallNativeConversationApiAsync(nativeMessages, null, nativeOptions);
Task<string> streamTimeout = NativeConversationLlmApplicationAdapter.CallNativeConversationApiAsync(nativeMessages, _ => throw new InvalidOperationException("Timeout produced preview"), nativeOptions);
await using (ReplayServer nativeHttp = await ReplayServer.StartAsync((_, _) => (200, "{\"choices\":[{\"message\":{\"content\":\"native nonstream ok\"}}]}")))
{
    var gateway = new LegacyConfiguredChatGateway(_ => "synthetic-native-secret", disableThinking:true);
    AnimusForge.ShoutNetwork.NonStream = async (_, token) => (await gateway.GenerateAsync(BuildRequest(nativeHttp.Url), token)).RawText;
    string raw = await NativeConversationLlmApplicationAdapter.CallNativeConversationApiAsync(nativeMessages, null, nativeOptions);
    AssertTrue(raw == "native nonstream ok" && nativeHttp.Requests.Count == 1, "Native nonstream real lower HTTP failed");
}
await using (ReplayServer nativeSse = await ReplayServer.StartAsync((_, _) => (200,
    "data: {\"choices\":[{\"delta\":{\"content\":\"NPC: hello \"}}]}\n\n" +
    "data: {\"choices\":[{\"delta\":{\"content\":\"there\"}}]}\n\n" + "data: [DONE]\n\n")))
{
    var gateway = new LegacyConfiguredChatGateway(_ => "synthetic-native-secret", disableThinking:true);
    AnimusForge.ShoutNetwork.Stream = async (_, chunk, complete, error, token) =>
    {
        var result = await gateway.GenerateExchangeAsync(BuildRequest(nativeSse.Url), true, chunk, token);
        complete(result.Result.RawText);
    };
    var previews = new List<string>();
    string raw = await NativeConversationLlmApplicationAdapter.CallNativeConversationApiAsync(nativeMessages, previews.Add, nativeOptions);
    AssertTrue(raw == "NPC: hello there", "Native stream normalized result mismatch: " + raw);
    AssertTrue(previews.Count >= 1 && previews.Last() == "hello there" && previews.All(x => !x.StartsWith("NPC:")), "Native preview filter leaked/duplicated text");
}
foreach (var test in new[] { (partial:"partial visible", completed:"", error:"transport error", expected:"partial visible"),
    (partial:"partial", completed:"complete wins", error:"transport error", expected:"complete wins"),
    (partial:"", completed:"", error:"quota limited", expected:"quota limited"),
    (partial:"", completed:"", error:"", expected:"") })
{
    AnimusForge.ShoutNetwork.Stream = (_, chunk, complete, error, token) =>
    {
        if (test.partial.Length > 0) chunk(test.partial);
        if (test.completed.Length > 0) complete(test.completed);
        if (test.error.Length > 0) error(test.error);
        return Task.CompletedTask;
    };
    string result = await NativeConversationLlmApplicationAdapter.CallNativeConversationApiAsync(nativeMessages, _ => {}, nativeOptions);
    AssertTrue(result == test.expected, "Native completed/partial/error/empty precedence changed");
}
foreach (bool streaming in new[] { false, true })
{
    using var cancel = new CancellationTokenSource();
    AnimusForge.ShoutNetwork.NonStream = (_, token) => { cancel.Cancel(); return Task.FromResult("late nonstream"); };
    var latePreviews = new List<string>();
    AnimusForge.ShoutNetwork.Stream = (_, chunk, complete, error, token) =>
    {
        cancel.Cancel(); chunk("late delta"); complete("late completed"); return Task.CompletedTask;
    };
    bool canceled = false;
    try { await NativeConversationLlmApplicationAdapter.CallNativeConversationApiAsync(nativeMessages, streaming ? latePreviews.Add : null, nativeOptions, cancel.Token); }
    catch (OperationCanceledException) { canceled = true; }
    AssertTrue(canceled && latePreviews.Count == 0, "Native cancellation/late callback boundary changed");
}
string timeoutExpected = "（API请求失败: 原生对话正文生成超时 180000ms）";
AssertTrue(await nonStreamTimeout == timeoutExpected, "Native nonstream real timeout mapping failed");
AssertTrue(await streamTimeout == timeoutExpected, "Native stream real timeout mapping failed");
Console.WriteLine("PASS nativeApplicationReplay nonstreamHttp=1 streamSse=1 completionPartialErrorEmpty=4 callerCancelLate=2 actualTimeout180000Ms=2 frozenSpeechOptions=1");

internal sealed class ReplayServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly Func<int, string, ValueTask<(int StatusCode, string Body)>> _handler;
    private readonly CancellationTokenSource _stop = new CancellationTokenSource();
    private readonly Task _acceptLoop;
    private int _requestIndex;

    private ReplayServer(
        TcpListener listener,
        Func<int, string, ValueTask<(int StatusCode, string Body)>> handler)
    {
        _listener = listener;
        _handler = handler;
        Url = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/v1/chat/completions";
        _acceptLoop = AcceptLoopAsync();
    }

    public string Url { get; }
    public List<ReplayRequest> Requests { get; } = new List<ReplayRequest>();

    public static async Task<ReplayServer> StartAsync(
        Func<int, string, (int StatusCode, string Body)> handler)
    {
        return await StartAsync((index, body) => new ValueTask<(int, string)>(handler(index, body)));
    }

    public static async Task<ReplayServer> StartAsync(
        Func<int, string, Task<(int StatusCode, string Body)>> handler)
    {
        return await StartAsync((index, body) => new ValueTask<(int, string)>(handler(index, body)));
    }

    private static Task<ReplayServer> StartAsync(
        Func<int, string, ValueTask<(int StatusCode, string Body)>> handler)
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return Task.FromResult(new ReplayServer(listener, handler));
    }

    private async Task AcceptLoopAsync()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(_stop.Token);
                _ = HandleAsync(client);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private async Task HandleAsync(TcpClient client)
    {
        using (client)
        using (NetworkStream stream = client.GetStream())
        {
            byte[] headerBuffer = new byte[16384];
            int count = 0;
            int headerEnd = -1;
            while (count < headerBuffer.Length && headerEnd < 0)
            {
                int read = await stream.ReadAsync(headerBuffer.AsMemory(count, headerBuffer.Length - count));
                if (read == 0)
                {
                    return;
                }
                count += read;
                headerEnd = FindHeaderEnd(headerBuffer, count);
            }
            if (headerEnd < 0)
            {
                return;
            }
            string headers = Encoding.ASCII.GetString(headerBuffer, 0, headerEnd);
            int contentLength = ReadContentLength(headers);
            int bodyStart = headerEnd + 4;
            while (count - bodyStart < contentLength)
            {
                int read = await stream.ReadAsync(headerBuffer.AsMemory(count, headerBuffer.Length - count));
                if (read == 0)
                {
                    return;
                }
                count += read;
            }
            string body = Encoding.UTF8.GetString(headerBuffer, bodyStart, contentLength);
            string authorization = headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(line => line.StartsWith("Authorization:", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
            ReplayRequest request = new ReplayRequest(authorization.Substring(authorization.IndexOf(':') + 1).Trim(), body);
            lock (Requests)
            {
                Requests.Add(request);
            }
            int index = Interlocked.Increment(ref _requestIndex);
            (int statusCode, string responseBody) = await _handler(index, body);
            byte[] payload = Encoding.UTF8.GetBytes(responseBody ?? string.Empty);
            byte[] prefix = Encoding.UTF8.GetBytes(
                "HTTP/1.1 " + statusCode + " " + (statusCode >= 200 && statusCode < 300 ? "OK" : "Error") + "\r\n"
                + "Content-Type: application/json\r\nContent-Length: " + payload.Length + "\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(prefix);
            await stream.WriteAsync(payload);
        }
    }

    private static int FindHeaderEnd(byte[] buffer, int count)
    {
        for (int i = 3; i < count; i++)
        {
            if (buffer[i - 3] == 13 && buffer[i - 2] == 10 && buffer[i - 1] == 13 && buffer[i] == 10)
            {
                return i - 3;
            }
        }
        return -1;
    }

    private static int ReadContentLength(string headers)
    {
        string line = headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(value => value.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase));
        return line == null ? 0 : int.Parse(line.Substring(line.IndexOf(':') + 1).Trim());
    }

    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        _listener.Stop();
        try
        {
            await _acceptLoop;
        }
        catch
        {
        }
    }
}

internal sealed record ReplayRequest(string Authorization, string Body);
