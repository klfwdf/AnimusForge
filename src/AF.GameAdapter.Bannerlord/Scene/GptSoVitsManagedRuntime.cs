using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

// Owns only the selected bundled L/U process. Configuration observation is O(1);
// discovery, hashing, launch and health IO run on the serialized background worker.
internal static class GptSoVitsManagedRuntime
{
    private static readonly object Sync = new object();
    private static readonly SemaphoreSlim Serial = new SemaphoreSlim(1, 1);
    private static readonly HttpClient Client = new HttpClient(new HttpClientHandler { UseProxy = false, AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(3) };
    private static CancellationTokenSource _change = new CancellationTokenSource();
    private static bool _observed, _enabled, _automatic, _managed;
    private static string _url, _state = "Unchecked", _reason = "", _endpoint, _instance;
    private static long _generation;
    private static bool _shutdown;
    private static Process _owned;
    private static string _ownedExecutable;
    private static DateTime _ownedStarted;
    internal static string State { get { lock (Sync) return _state; } }
    internal static string Reason { get { lock (Sync) return _reason; } }

    static GptSoVitsManagedRuntime() { AppDomain.CurrentDomain.ProcessExit += (_, __) => StopOwned(); }

    internal static void Observe(DuelSettings settings)
    {
        if (settings == null) return;
        bool enabled = settings.EnableTtsSpeech && settings.TtsUseGptSoVits;
        bool automatic = settings.UsesManagedGptSoVits;
        string url = automatic ? "" : settings.TtsGptSoVitsApiUrl ?? "";
        bool cancelPrevious;
        lock (Sync)
        {
            if (_shutdown) return;
            if (_observed && _enabled == enabled && _automatic == automatic && _url == url) return;
            cancelPrevious = _managed && _enabled;
            _observed = true; _enabled = enabled; _automatic = automatic; _url = url;
            ScheduleLocked();
        }
        if (cancelPrevious) TtsEngine.Instance.CancelLocalPlayback();
    }

    internal static Task RefreshAsync()
    {
        Task refresh;
        bool cancelPrevious;
        lock (Sync)
        {
            if (_shutdown) return Task.CompletedTask;
            cancelPrevious = _managed && _enabled;
            refresh = ScheduleLocked();
        }
        if (cancelPrevious) TtsEngine.Instance.CancelLocalPlayback();
        return refresh;
    }

    private static Task ScheduleLocked()
    {
        _change.Cancel();
        var cancellation = new CancellationTokenSource();
        var previous = _change; _change = cancellation;
        long generation = ++_generation;
        bool enabled = _enabled, automatic = _automatic;
        _endpoint = null; _instance = null;
        // Before discovery, do not enqueue old lines to be read after cold startup.
        _managed = enabled && automatic;
        _state = automatic ? "Checking" : (_owned != null ? "Stopping" : "Manual"); _reason = "";
        return Task.Run(async () =>
        {
            await Serial.WaitAsync().ConfigureAwait(false);
            try { await ApplyAsync(generation, enabled, automatic, cancellation.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Logger.Log("GptSoVitsRuntime", "startup_failed:" + ex.GetType().Name + ":" + ex.Message); SetState(generation, "Failed", ex is InvalidDataException || ex is InvalidOperationException ? ex.Message : ex.GetType().Name); }
            finally { Serial.Release(); previous.Dispose(); }
        });
    }

    private static async Task ApplyAsync(long generation, bool enabled, bool automatic, CancellationToken cancellation)
    {
        StopOwned();
        cancellation.ThrowIfCancellationRequested();
        if (!automatic) { SetState(generation, "Manual", "manual_connection"); return; }
        string module = AnimusForgeDataPaths.ResolveRoot(AnimusForgeModulePaths.GetCurrentModuleRoot());
        SetState(generation, "Preparing", "verifying_package");
        var package = GptSoVitsRuntimePackage.Discover(module, typeof(TtsEngine).Assembly.GetName().Version, verifyFiles: enabled);
        if (package.State == GptSoVitsPackageState.Missing)
        {
            SetState(generation, "Missing", "package_missing");
            return;
        }
        if (package.State != GptSoVitsPackageState.Installed) { SetState(generation, "Failed", package.Reason); return; }
        if (!enabled) { SetState(generation, "Installed", "installed_not_started"); return; }
        SetState(generation, "Preparing", "verifying_package");
        cancellation.ThrowIfCancellationRequested();
        int port;
        var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start(); port = ((IPEndPoint)reservation.LocalEndpoint).Port; reservation.Stop();
        string instance = Guid.NewGuid().ToString("N");
        string cache = Path.Combine(AnimusForgeDataPaths.GetCacheDirectory(module), "TTS");
        string logs = Path.Combine(AnimusForgeDataPaths.GetLogsDirectory(module), "TTS");
        AnimusForgeDataPaths.ValidatePath(cache); AnimusForgeDataPaths.ValidatePath(logs);
        Directory.CreateDirectory(cache); Directory.CreateDirectory(logs);
        var info = new ProcessStartInfo(package.PythonPath)
        {
            Arguments = Quote(package.EntryPath) + " --port " + port + " --module-root " + Quote(module) + " --parent-pid " + Process.GetCurrentProcess().Id,
            WorkingDirectory = package.Root, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        info.EnvironmentVariables["AF_TTS_INSTANCE"] = instance;
        info.EnvironmentVariables["PYTHONUTF8"] = "1";
        info.EnvironmentVariables["PYTHONDONTWRITEBYTECODE"] = "1";
        info.EnvironmentVariables["HF_HUB_OFFLINE"] = "1";
        info.EnvironmentVariables["HF_HUB_DISABLE_TELEMETRY"] = "1";
        info.EnvironmentVariables["HF_HOME"] = Path.Combine(cache, "huggingface");
        info.EnvironmentVariables["TORCH_HOME"] = Path.Combine(cache, "torch");
        info.EnvironmentVariables["TEMP"] = cache;
        info.EnvironmentVariables["TMP"] = cache;
        info.EnvironmentVariables["AF_TTS_LOG_ROOT"] = logs;
        var process = new Process { StartInfo = info };
        // The packaged server owns its bounded logs. Drain progress bars without
        // copying every token/tqdm update into AF's application log.
        process.OutputDataReceived += (_, __) => { };
        int diagnosticLines = 0;
        process.ErrorDataReceived += (_, output) =>
        {
            string line = output.Data;
            if (string.IsNullOrWhiteSpace(line) ||
                !(line.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase) ||
                  line.StartsWith("WARNING", StringComparison.OrdinalIgnoreCase) ||
                  line.StartsWith("Traceback", StringComparison.Ordinal))) return;
            if (Interlocked.Increment(ref diagnosticLines) <= 16)
                Logger.Log("GptSoVitsRuntime", "worker:" + line.Substring(0, Math.Min(512, line.Length)));
        };
        if (!process.Start()) throw new InvalidOperationException("runtime_start_failed");
        lock (Sync) { _owned = process; _ownedExecutable = package.PythonPath; _ownedStarted = process.StartTime; }
        process.Exited += (_, __) =>
        {
            lock (Sync)
            {
                if (!ReferenceEquals(_owned, process) || generation != _generation || _state != "Ready") return;
                _endpoint = null; _instance = null; _state = "Failed"; _reason = "runtime_exited";
            }
            TtsTestFeedback.ReportRuntimeStatus("Failed", "runtime_exited");
        };
        process.EnableRaisingEvents = true;
        process.BeginOutputReadLine(); process.BeginErrorReadLine();
        string endpoint = "http://127.0.0.1:" + port;
        try
        {
            using (var startup = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
            {
                startup.CancelAfter(TimeSpan.FromSeconds(120));
                try
                {
                    while (true)
                    {
                        startup.Token.ThrowIfCancellationRequested();
                        if (process.HasExited) throw new InvalidOperationException("runtime_exited_" + process.ExitCode);
                        if (await CheckHealthAsync(endpoint, instance, package.Id, package.Engine, startup.Token).ConfigureAwait(false))
                        {
                            lock (Sync)
                            {
                                if (generation != _generation) throw new OperationCanceledException();
                                if (process.HasExited) throw new InvalidOperationException("runtime_exited_" + process.ExitCode);
                                _endpoint = endpoint + "/tts"; _instance = instance; _state = "Ready"; _reason = package.Id;
                            }
                            TtsTestFeedback.ReportRuntimeStatus("Ready", package.Id);
                            return;
                        }
                        await Task.Delay(1000, startup.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (!cancellation.IsCancellationRequested)
                { throw new InvalidOperationException("runtime_start_timeout"); }
            }
        }
        catch { StopOwned(); throw; }
    }

    private static async Task<bool> CheckHealthAsync(string endpoint, string instance, string packageId, string engine, CancellationToken cancellation)
    {
        try
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, endpoint + "/af/health"))
            {
                request.Headers.Add("X-AF-Instance", instance);
                using (var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > 16384) return false;
                    using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var output = new MemoryStream())
                    {
                        var buffer = new byte[2048]; int read;
                        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellation).ConfigureAwait(false)) > 0)
                        { if (output.Length + read > 16384) return false; output.Write(buffer, 0, read); }
                        var value = JObject.Parse(System.Text.Encoding.UTF8.GetString(output.ToArray()));
                        bool identified = (string)value["instance"] == instance && (int?)value["af_runtime_api"] == 1 &&
                            (string)value["package_id"] == packageId &&
                            (string)value["engine"] == engine &&
                            (string)value["capabilities"]?["audio"] == "pcm16_mono_wav" &&
                            (int?)value["capabilities"]?["batch_size"] == 1;
                        if (identified && (string)value["state"] == "failed")
                            throw new InvalidOperationException("runtime_model_load_failed");
                        return identified && (string)value["state"] == "ready";
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { throw; }
        catch (InvalidOperationException) { throw; }
        catch { return false; }
    }

    internal static bool TryCapture(string configured, out string endpoint, out string instance)
    {
        lock (Sync)
        {
            endpoint = configured; instance = null;
            if (!_managed) return true;
            if (_state != "Ready") return false;
            endpoint = _endpoint; instance = _instance; return true;
        }
    }

    internal static void Shutdown()
    {
        lock (Sync) { _shutdown = true; _enabled = false; _automatic = false; ScheduleLocked(); }
    }

    private static void StopOwned()
    {
        Process process; string executable; DateTime started;
        lock (Sync) { process = _owned; _owned = null; executable = _ownedExecutable; started = _ownedStarted; }
        if (process == null) return;
        try
        {
            if (!process.HasExited && process.StartTime == started &&
                string.Equals(process.MainModule.FileName, executable, StringComparison.OrdinalIgnoreCase))
            { process.Kill(); process.WaitForExit(3000); }
        }
        catch (Exception ex) { Logger.Log("GptSoVitsRuntime", "stop_failed:" + ex.GetType().Name); }
        finally { process.Dispose(); }
    }

    private static void SetState(long generation, string state, string reason)
    {
        lock (Sync) if (generation != _generation) return; else { _state = state; _reason = reason; }
        TtsTestFeedback.ReportRuntimeStatus(state, reason);
    }
    private static string Quote(string value)
    { if (value.IndexOf('"') >= 0) throw new ArgumentException("invalid_argument"); return "\"" + value + "\""; }
}
