using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge;

internal static class YjProbeCases
{
    private static void Check(bool condition, string label) { if (!condition) throw new Exception("YJ: " + label); }
    private sealed class Handler : HttpMessageHandler
    {
        internal Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Respond;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => Respond(request, token);
    }
    private static HttpResponseMessage Response(HttpStatusCode code, string media = "application/json") =>
        new HttpResponseMessage(code) { Content = new StringContent("{}", System.Text.Encoding.UTF8, media) };
    private static void Pump(AnimusForgeApiOnboardingVM vm, Func<bool> done)
    {
        for (int i = 0; i < 200 && !done(); i++) { vm.OnTick(); Thread.Sleep(5); }
        Check(done(), "UI dispatch completes");
    }
    internal static async Task Run()
    {
        int requests = 0;
        using var handler = new Handler();
        using var client = new HttpClient(handler);
        handler.Respond = async (request, token) => {
            Interlocked.Increment(ref requests);
            Check(request.Method == HttpMethod.Get && request.RequestUri.AbsolutePath == "/v1/models", "models only");
            Check(request.Headers.Authorization == null && !request.Headers.Contains("x-api-key"), "no credential");
            await Task.Delay(request.RequestUri.Host.StartsWith("asia.") ? 5 : 80, token);
            return Response(HttpStatusCode.Unauthorized);
        };
        var result = await YjEndpointProbe.SelectAsync(client, CancellationToken.None);
        Check(result.Url == YjEndpointProbe.AsiaUrl && requests == 6, "fastest of three, two samples each, auth response reachable");
        handler.Respond = async (request, token) => {
            if (request.RequestUri.Host.StartsWith("asia.")) return Response(HttpStatusCode.ServiceUnavailable);
            if (request.RequestUri.Host.StartsWith("www.")) return Response(HttpStatusCode.OK, "text/html");
            await Task.Delay(20, token); return Response(HttpStatusCode.OK);
        };
        Check((await YjEndpointProbe.SelectAsync(client, CancellationToken.None)).Url == YjEndpointProbe.AlternateUrl, "fast errors and HTML excluded");
        handler.Respond = async (request, token) => { await Task.Delay(1000, token); return Response(HttpStatusCode.OK); };
        Check(await YjEndpointProbe.SelectAsync(client, CancellationToken.None, 15) == null, "timeouts return no selection");
        using var cts = new CancellationTokenSource(15);
        bool cancelled = false;
        try { await YjEndpointProbe.SelectAsync(client, cts.Token); } catch (OperationCanceledException) { cancelled = true; }
        Check(cancelled, "caller cancellation propagated");

        DuelSettings.Current = new();
        var vm = new AnimusForgeApiOnboardingVM(false, null, null);
        var pending = new TaskCompletionSource<YjEndpointProbe.Result>();
        int starts = 0;
        vm.ProbeYjEndpoints = _ => { starts++; return pending.Task; };
        vm.ExecuteSelectYj(); vm.ExecuteSelectYj();
        Check(starts == 1 && vm.IsMainViewVisible, "waits before entering menu, duplicate clicks coalesced");
        pending.SetResult(new YjEndpointProbe.Result { Url = YjEndpointProbe.AsiaUrl, Milliseconds = 8 });
        Pump(vm, () => vm.IsYjMenuViewVisible);
        vm.ExecuteYjMultiGroup();
        Check(vm.PrimaryUrl == YjEndpointProbe.AsiaUrl && vm.AuxiliaryUrl == vm.PrimaryUrl && vm.PostprocessUrl == vm.PrimaryUrl && vm.EventUrl == vm.PrimaryUrl, "all multi-group URLs use winner");
        vm.ExecuteBackToYjMenu(); vm.ExecuteYjSingleGroup(); vm.PromptKeyInput = "fixture-key"; vm.ExecuteConfirmPromptKey();
        Check(vm.PrimaryUrl == YjEndpointProbe.AsiaUrl && vm.EventUrl == vm.PrimaryUrl, "single-group URL uses winner");
        vm.OnFinalize();

        foreach (var action in new Action<AnimusForgeApiOnboardingVM>[] { v => v.ExecuteBackToMain(), v => v.ExecuteSelectCustom(), v => v.ExecuteSelectDeepSeekFlash(), v => v.ExecuteOpenSupport(), v => v.ExecuteClose(), v => v.OnFinalize() })
        {
            var late = new TaskCompletionSource<YjEndpointProbe.Result>();
            var other = new AnimusForgeApiOnboardingVM(false, null, null);
            CancellationToken captured = default;
            other.ProbeYjEndpoints = token => { captured = token; return late.Task; };
            other.ExecuteSelectYj(); action(other);
            Check(captured.IsCancellationRequested, "leaving cancels network");
            late.SetResult(new YjEndpointProbe.Result { Url = YjEndpointProbe.AsiaUrl });
            for (int i = 0; i < 20; i++) { other.OnTick(); Thread.Sleep(5); }
            Check(!other.IsYjMenuViewVisible, "late result cannot reopen menu");
            other.OnFinalize();
        }
        var failed = new AnimusForgeApiOnboardingVM(false, null, null);
        string original = failed.PrimaryUrl;
        failed.ProbeYjEndpoints = _ => Task.FromResult<YjEndpointProbe.Result>(null);
        failed.ExecuteSelectYj(); failed.OnTick();
        Check(failed.IsMainViewVisible && failed.PrimaryUrl == original, "all failed leaves configuration unchanged");
        failed.OnFinalize();
        Console.WriteLine("PASS YJ probe: three endpoints, latency, errors, timeout, cancellation, duplicate clicks, both modes, stale UI and failure");
    }
}
