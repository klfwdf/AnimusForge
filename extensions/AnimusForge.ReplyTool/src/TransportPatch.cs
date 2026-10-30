using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;

namespace AnimusForge.ReplyTool;

public static class ReplyToolInstaller
{
    public const string Owner = "AnimusForge.ReplyTool.v0_1";
    private static readonly Harmony Patcher = new Harmony(Owner);
    private static ReplyPrompt _prompt;
    private static Action<string> _log;
    public static bool Install(Assembly af, string promptJson, Action<string> log)
    {
        var prompt = new ReplyPrompt(promptJson);
        string[] types = { "AnimusForge.LlmNonStreamingTransport", "AnimusForge.LlmStreamingTransport" };
        var methods = types.Select(t => af.GetType(t, false)?.GetMethod("SendAsync", BindingFlags.Static | BindingFlags.NonPublic)).ToArray();
        if (methods.Any(m => m == null || !Matches(m))) { log?.Invoke("unsupported_transport_shape;patches=0"); return false; }
        if (methods.All(m => Harmony.GetPatchInfo(m)?.Owners.Contains(Owner) == true)) return true;
        _prompt = prompt; _log = log;
        try
        {
            foreach (var method in methods)
                if (Harmony.GetPatchInfo(method)?.Owners.Contains(Owner) != true)
                    Patcher.Patch(method, prefix: new HarmonyMethod(typeof(ReplyToolInstaller), nameof(Prefix)));
            log?.Invoke("installed;transports=2;mode=gemini_reply_tool;input_wrapper=enabled;version=0.1.1;retries=0");
            return true;
        }
        catch (Exception error)
        {
            Uninstall(); log?.Invoke("installation_rejected:" + error.GetType().Name); return false;
        }
    }
    private static bool Matches(MethodInfo method)
    {
        var p = method.GetParameters();
        return p.Length >= 5 && p[0].ParameterType == typeof(string) && p[1].ParameterType == typeof(string)
            && p[2].ParameterType == typeof(string) && p[3].Name == "sender"
            && p[3].ParameterType == typeof(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>)
            && p[4].ParameterType == typeof(CancellationToken)
            && method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);
    }
    public static void Uninstall() => Patcher.UnpatchAll(Owner);
    private static void Prefix(ref Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sender)
    {
        var original = sender; var prompt = _prompt;
        sender = (request, token) => SendAsync(original, request, token, prompt);
    }
    internal static async Task<HttpResponseMessage> SendAsync(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sender,
        HttpRequestMessage request, CancellationToken token, ReplyPrompt prompt)
    {
        token.ThrowIfCancellationRequested();
        if (request.Method != HttpMethod.Post || request.Content == null) return await sender(request, token).ConfigureAwait(false);
        string json = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        if (!ReplyProtocol.TryPrepare(request.RequestUri, json, prompt, out string prepared, out bool streaming, out bool inputWrapped))
            return await sender(request, token).ConfigureAwait(false);
        var previous = request.Content;
        var replacement = new StringContent(prepared, Encoding.UTF8, "application/json");
        foreach (var header in previous.Headers)
            if (!header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)
                && !header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                replacement.Headers.TryAddWithoutValidation(header.Key, header.Value);
        request.Content = replacement; previous.Dispose();
        try { _log?.Invoke("request_prepared;input_wrapped=" + inputWrapped + ";stream=" + streaming); }
        catch (Exception) { /* Diagnostics cannot decide whether a request is sent. */ }
        // AF's original sender still owns authentication, endpoint and exactly one attempt.
        HttpResponseMessage response = await sender(request, token).ConfigureAwait(false);
        try
        {
            token.ThrowIfCancellationRequested();
            if (response.IsSuccessStatusCode && response.Content != null)
                response.Content = new ReplyHttpContent(response.Content, streaming, token);
            return response;
        }
        catch { response.Dispose(); throw; }
    }
}
