using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;

namespace AnimusForge.ReplyTool;

internal static class ImageTransportPatch
{
    private static readonly MethodInfo OriginalSend = typeof(HttpClient).GetMethod(nameof(HttpClient.SendAsync),
        new[] { typeof(HttpRequestMessage), typeof(HttpCompletionOption), typeof(CancellationToken) });
    private static ReplyPrompt _prompt;
    private static Action<string> _log;

    internal static bool Install(Assembly af, ReplyPrompt prompt, Action<string> log)
    {
        var method = af.GetType("AnimusForge.Illustrator.Core.UniversalOpenAiImageClient", false)?
            .GetMethod("AttemptGenerateOnceAsync", BindingFlags.Static | BindingFlags.NonPublic);
        var stateMachine = method?.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
        var moveNext = stateMachine?.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (moveNext == null) { log?.Invoke("image_transport_unsupported;patches=0"); return false; }
        if (Harmony.GetPatchInfo(moveNext)?.Owners.Contains(ReplyToolInstaller.Owner) == true) return true;
        _prompt = prompt; _log = log;
        try
        {
            new Harmony(ReplyToolInstaller.Owner).Patch(moveNext,
                transpiler: new HarmonyMethod(typeof(ImageTransportPatch), nameof(Transpile)));
            log?.Invoke("image_transport_installed;sites=1;input_wrapper=enabled;output=native_image;retries=0");
            return true;
        }
        catch (Exception error)
        {
            new Harmony(ReplyToolInstaller.Owner).Unpatch(moveNext, HarmonyPatchType.All, ReplyToolInstaller.Owner);
            log?.Invoke("image_transport_rejected:" + error.GetType().Name);
            return false;
        }
    }

    private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        var sites = code.Where(i => i.Calls(OriginalSend)).ToArray();
        if (sites.Length != 1) throw new InvalidDataException("reply_tool_image_send_shape");
        sites[0].opcode = OpCodes.Call;
        sites[0].operand = typeof(ImageTransportPatch).GetMethod(nameof(SendAsync), BindingFlags.Static | BindingFlags.NonPublic);
        return code;
    }

    internal static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpRequestMessage request,
        HttpCompletionOption completion, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (request.Method == HttpMethod.Post && request.Content != null
            && request.RequestUri.AbsolutePath.TrimEnd('/').EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            string json = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            if (ImageRequestProtocol.TryPrepare(request.RequestUri, json, _prompt, out string prepared))
            {
                var previous = request.Content;
                var replacement = new StringContent(prepared, Encoding.UTF8, "application/json");
                foreach (var header in previous.Headers)
                    if (!header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)
                        && !header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                        replacement.Headers.TryAddWithoutValidation(header.Key, header.Value);
                request.Content = replacement; previous.Dispose();
                try { _log?.Invoke("image_request_prepared;input_wrapped=True;output=native_image;retries=0"); }
                catch (Exception) { /* Diagnostics cannot decide whether to send. */ }
            }
        }
        // AF retains bounded image reads, base64 decoding, cancellation, errors,
        // downloads, persistence and owner/generation acceptance of late images.
        return await client.SendAsync(request, completion, token).ConfigureAwait(false);
    }
}
