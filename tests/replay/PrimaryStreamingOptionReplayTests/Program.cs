using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
string candidate = ReplayCandidateInput.Read(args);
AppDomain.CurrentDomain.AssemblyResolve += (_, eventArgs) =>
{
    string name = new AssemblyName(eventArgs.Name).Name;
    foreach (string file in Directory.GetFiles(AppContext.BaseDirectory, name + ".dll", SearchOption.AllDirectories))
        try { return Assembly.LoadFrom(file); } catch { }
    return null;
};
Assembly implementation = Assembly.LoadFrom(candidate);
Type network = implementation.GetType("AnimusForge.ShoutNetwork", true);
MethodInfo pushNonStream = network.GetMethod("PushNonStreamingTransportOverrideForExternal");
MethodInfo pushStream = network.GetMethod("PushStreamingTransportOverrideForExternal");
Check(pushNonStream != null && pushStream != null, "Debug-only synthetic seams required; no real-network fallback");
Type settingsType = implementation.GetType("AnimusForge.DuelSettings", true);
object settings = settingsType.GetMethod("GetSettings").Invoke(null, null);
PropertyInfo option = settingsType.GetProperty("MainApiStreamingEnabled");
Check(option != null && !(bool)option.GetValue(settings), "option must exist and default off");
Check(option.GetCustomAttributesData().Any(attribute => attribute.AttributeType.Name == "SettingPropertyBoolAttribute"), "MCM bool attribute missing");
settingsType.GetProperty("ApiUrl").SetValue(settings, "https://fixture.invalid/v1");
settingsType.GetProperty("ApiKey").SetValue(settings, "stream-option-fixture-key");
settingsType.GetProperty("ModelName").SetValue(settings, "deepseek-fixture");
settingsType.GetProperty("MainApiThinkingEnabled").SetValue(settings, true);
Type registry = implementation.GetType("AnimusForge.TerminalSettingsRegistry", true);
object definition = registry.GetMethod("GetById").Invoke(null, new object[] { "MainApiStreamingEnabled" });
Check(definition != null, "terminal option missing");
Delegate setter = (Delegate)definition.GetType().GetProperty("Setter").GetValue(definition);
Delegate getter = (Delegate)definition.GetType().GetProperty("Getter").GetValue(definition);
setter.DynamicInvoke(settings, true);
Check((bool)option.GetValue(settings) && (bool)getter.DynamicInvoke(settings), "terminal and MCM do not share option");
setter.DynamicInvoke(settings, false);

MethodInfo send = network.GetMethod("CallApiWithMessages");
MethodInfo stream = network.GetMethod("CallApiWithMessagesStream");
List<object> Messages() => new List<object> {
    new Dictionary<string, object> { ["role"] = "user", ["content"] = "synthetic prompt" }
};
async Task<string> SendAsync(int? maxTokens = null, bool disableThinking = false, float? temperature = null, CancellationToken token = default)
{
    var task = (Task<string>)send.Invoke(null, new object[] { Messages(), 5000, false, maxTokens, disableThinking, false, token, temperature });
    return await task;
}
Task StreamAsync(Action<string> chunk, Action<string> completed, Action<string> error, CancellationToken token = default)
    => (Task)stream.Invoke(null, new object[] { Messages(), 5000, chunk, completed, error, token, false });
HttpResponseMessage JsonReply() => new HttpResponseMessage(HttpStatusCode.OK) {
    Content = new StringContent("{\"choices\":[{\"message\":{\"content\":\"whole reply\"}}]}", Encoding.UTF8, "application/json")
};
HttpResponseMessage SseReply(bool done = true, bool finishReason = false, bool malformed = false)
{
    string data = "data: {\"choices\":[{\"delta\":{\"content\":\"whole \"}}]}\n\n"
        + (malformed ? "data: {bad-json}\n\n" : "")
        + "data: {\"choices\":[{\"delta\":{\"content\":\"reply\"}" + (finishReason ? ",\"finish_reason\":\"stop\"" : "") + "}]}\n\n"
        + (done ? "data: [DONE]\n\n" : "");
    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(data, Encoding.UTF8, "text/event-stream") };
}
int nonCount = 0, streamCount = 0;
JObject lastBody = null;
Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> streamResponse = (_, _) => Task.FromResult(SseReply());
Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> nonResponse = (_, _) => Task.FromResult(JsonReply());
void Inspect(HttpRequestMessage request, bool expectedStream)
{
    Check(request.Method == HttpMethod.Post && request.RequestUri.AbsoluteUri == "https://fixture.invalid/v1/chat/completions", "endpoint/method changed");
    Check(request.Headers.Authorization?.Parameter == "stream-option-fixture-key", "authentication mismatch");
    Check(request.Content.Headers.ContentType.MediaType == "application/json", "content type mismatch");
    string body = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    Check(!body.Contains("stream-option-fixture-key"), "credential in request payload");
    lastBody = JObject.Parse(body);
    Check((lastBody["stream"]?.Value<bool>() ?? false) == expectedStream, "transport and payload stream mode differ");
}
using IDisposable nonScope = (IDisposable)pushNonStream.Invoke(null, new object[] {
    new Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>((request, token) => {
        nonCount++; Inspect(request, false); return nonResponse(request, token);
    })
});
using IDisposable streamScope = (IDisposable)pushStream.Invoke(null, new object[] {
    new Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>((request, token) => {
        streamCount++; Inspect(request, true); return streamResponse(request, token);
    })
});
int cases = 0;
async Task Case(string name, Func<Task> test)
{
    nonCount = streamCount = 0;
    await test(); cases++; Console.WriteLine("PASS " + name);
}
await Case("off: ordinary reply uses JSON only", async () => {
    option.SetValue(settings, false);
    Check(await SendAsync() == "whole reply" && nonCount == 1 && streamCount == 0, "off routing failed");
});
await Case("off: preview-capable entry receives one final reply, no chunks", async () => {
    int chunks = 0, completions = 0, errors = 0;
    await StreamAsync(_ => chunks++, reply => { Check(reply == "whole reply", "final mismatch"); completions++; }, _ => errors++);
    Check(chunks == 0 && completions == 1 && errors == 0 && nonCount == 1 && streamCount == 0, "off callback contract failed");
});
await Case("on: ordinary channel accumulates SSE before returning", async () => {
    option.SetValue(settings, true);
    Check(await SendAsync() == "whole reply" && streamCount == 1 && nonCount == 0, "SSE accumulator routing failed");
});
await Case("on: explicit request overrides preserved", async () => {
    Check(await SendAsync(768, true, 0.25f) == "whole reply", "override reply mismatch");
    Check(lastBody["max_tokens"].Value<int>() == 768 && Math.Abs(lastBody["temperature"].Value<double>() - 0.25) < 0.001, "max_tokens/temperature lost");
    Check((string)lastBody.SelectToken("thinking.type") == "disabled", "forced thinking disable lost");
});
await Case("on: preview chunks and single complete callback", async () => {
    var chunks = new StringBuilder(); int completed = 0, errors = 0;
    await StreamAsync(delta => chunks.Append(delta), reply => { Check(reply == "whole reply", "final mismatch"); completed++; }, _ => errors++);
    Check(chunks.ToString() == "whole reply" && completed == 1 && errors == 0, "preview/final duplication");
});
await Case("on: clean finish_reason without DONE is accepted", async () => {
    streamResponse = (_, _) => Task.FromResult(SseReply(done: false, finishReason: true));
    Check(await SendAsync() == "whole reply" && streamCount == 1 && nonCount == 0, "finish marker ignored");
});
await Case("on: silent EOF with partial reply never becomes business success", async () => {
    streamResponse = (_, _) => Task.FromResult(SseReply(done: false));
    string result = await SendAsync();
    Check(result.StartsWith("（API请求失败:") && streamCount == 1 && nonCount == 0, "partial EOF accepted or replayed");
});
await Case("on: malformed chunk never becomes complete reply", async () => {
    streamResponse = (_, _) => Task.FromResult(SseReply(malformed: true));
    Check((await SendAsync()).StartsWith("（API请求失败:") && streamCount == 1 && nonCount == 0, "malformed reply accepted");
});
await Case("on: partial preview failure does not invoke completion", async () => {
    streamResponse = (_, _) => Task.FromResult(SseReply(done: false));
    int completed = 0, errors = 0;
    await StreamAsync(_ => {}, _ => completed++, _ => errors++);
    Check(completed == 0 && errors == 1 && streamCount == 1 && nonCount == 0, "partial preview triggered downstream completion");
});
await Case("on: cancellation after chunk never completes", async () => {
    streamResponse = (_, _) => Task.FromResult(SseReply());
    using var cancel = new CancellationTokenSource(); int completed = 0;
    bool cancelled = false;
    try { await StreamAsync(_ => cancel.Cancel(), _ => completed++, _ => {}, cancel.Token); }
    catch (OperationCanceledException) { cancelled = true; }
    Check(cancelled && completed == 0 && streamCount == 1 && nonCount == 0, "cancelled partial accepted");
});
await Case("on: changing setting mid-send does not reroute request", async () => {
    option.SetValue(settings, true);
    streamResponse = (_, _) => { option.SetValue(settings, false); return Task.FromResult(SseReply()); };
    Check(await SendAsync() == "whole reply" && streamCount == 1 && nonCount == 0, "mid-send setting rerouted request");
});
await Case("off: HTTP failure does not invoke completion", async () => {
    nonResponse = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StringContent("synthetic denied") });
    int completed = 0, errors = 0;
    await StreamAsync(_ => {}, _ => completed++, _ => errors++);
    Check(completed == 0 && errors == 1 && nonCount == 1 && streamCount == 0, "HTTP error published as reply");
});
await Case("on: thinking rejection retries plain SSE without losing overrides", async () => {
    option.SetValue(settings, true);
    streamResponse = (_, _) => Task.FromResult(streamCount == 1
        ? new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("thinking unsupported") }
        : SseReply());
    Check(await SendAsync(768, false, 0.25f) == "whole reply" && streamCount == 2 && nonCount == 0, "plain SSE retry failed");
    Check(lastBody["thinking"] == null && lastBody["max_tokens"].Value<int>() == 768
        && Math.Abs(lastBody["temperature"].Value<double>() - 0.25) < 0.001, "plain retry lost controls");
});
await Case("on: failure before any content retains one bounded non-stream fallback", async () => {
    streamResponse = (_, _) => throw new IOException("synthetic stream unavailable");
    nonResponse = (_, _) => Task.FromResult(JsonReply());
    Check(await SendAsync(768, true, 0.25f) == "whole reply" && streamCount == 2 && nonCount == 1, "fallback recursed or was lost");
    Check(lastBody["max_tokens"].Value<int>() == 768 && Math.Abs(lastBody["temperature"].Value<double>() - 0.25) < 0.001, "fallback overrides lost");
    option.SetValue(settings, false);
});
await Case("off: stale response does not invoke completion", async () => {
    Type guard = implementation.GetType("AnimusForge.SaveRuntimeGuard", true);
    nonResponse = (_, _) => {
        guard.GetMethod("AdvanceGeneration").Invoke(null, new object[] { "stream-option-fixture" });
        return Task.FromResult(JsonReply());
    };
    int completed = 0, errors = 0;
    await StreamAsync(_ => {}, _ => completed++, _ => errors++);
    Check(completed == 0 && errors == 0 && nonCount == 1 && streamCount == 0, "stale reply published");
});
Console.WriteLine("PASS PrimaryStreamingOptionReplayTests cases=" + cases + " realCandidate=" + candidate + " liveNetwork=0");
