using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

string stageDirectory = Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory,
    "..", "..", "..", "..", "..",
    "bin", "Debug", "single_module_stage", "AnimusForge",
    "bin", "Win64_Shipping_Client"));
string implementationPath = Path.Combine(stageDirectory, "versions", "1.4", "AnimusForge.dll");
AssertTrue(File.Exists(implementationPath), "project-local 1.4 AnimusForge.dll is missing");
AppDomain.CurrentDomain.AssemblyResolve += (_, arguments) =>
{
    string name = new AssemblyName(arguments.Name).Name;
    foreach (string root in new[] { AppContext.BaseDirectory, stageDirectory })
    {
        if (!Directory.Exists(root)) continue;
        foreach (string candidate in Directory.GetFiles(root, name + ".dll", SearchOption.AllDirectories))
        {
            try { return Assembly.LoadFrom(candidate); } catch { }
        }
    }
    return null;
};

Assembly animusForge = Assembly.LoadFrom(implementationPath);
Type requestType = animusForge.GetType("AnimusForge.Refactor.Contracts.TtsSynthesisRequest", true);
Type gatewayType = animusForge.GetType("AnimusForge.Refactor.Adapters.LegacyVolcTtsGateway", true);

object BuildRequest(string endpoint, string extra = "{\"enable\":true}")
{
    return Activator.CreateInstance(
        requestType,
        endpoint,
        "app-replay",
        "resource-replay",
        "voice-replay",
        "你好，世界",
        "wav",
        24000,
        1.1f,
        0.9f,
        extra);
}

using (ReplayServer success = ReplayServer.Start("AQID", 0))
{
    using HttpClient client = new HttpClient();
    object gateway = Activator.CreateInstance(gatewayType, client);
    MethodInfo method = gatewayType.GetMethod("SynthesizeAsync");
    object taskObject = method.Invoke(gateway, new[] { BuildRequest(success.Url), "tts-replay-secret", CancellationToken.None });
    Task task = (Task)taskObject;
    await task.ConfigureAwait(false);
    object result = task.GetType().GetProperty("Result").GetValue(task, null);
    Type resultType = result.GetType();
    AssertTrue((bool)resultType.GetProperty("Success").GetValue(result, null), "TTS code=3000 response was not accepted");
    AssertTrue(((byte[])resultType.GetProperty("AudioBytes").GetValue(result, null)).SequenceEqual(new byte[] { 1, 2, 3 }), "TTS base64 audio was decoded incorrectly");
    string requestText = success.RequestText;
    AssertTrue(requestText.Contains("Bearer;tts-replay-secret", StringComparison.Ordinal), "TTS bearer header was not preserved");
    AssertTrue(requestText.Contains("X-Api-App-Id: app-replay", StringComparison.Ordinal), "TTS app header was not preserved");
    AssertTrue(requestText.Contains("X-Api-Resource-Id: resource-replay", StringComparison.Ordinal), "TTS resource header was not preserved");
    AssertTrue(requestText.Contains("\"token\":\"token\"", StringComparison.Ordinal), "TTS legacy payload token literal changed");
    int bodyStart = requestText.IndexOf("\r\n\r\n", StringComparison.Ordinal);
    AssertTrue(bodyStart >= 0 && !requestText.Substring(bodyStart + 4).Contains("tts-replay-secret", StringComparison.Ordinal), "TTS credential leaked into request body");
}

using (ReplayServer providerError = ReplayServer.Start("AQID", 0, 500, "provider-error"))
{
    using HttpClient client = new HttpClient();
    object gateway = Activator.CreateInstance(gatewayType, client);
    Task task = (Task)gatewayType.GetMethod("SynthesizeAsync").Invoke(gateway, new[] { BuildRequest(providerError.Url), "tts-replay-secret", CancellationToken.None });
    await task.ConfigureAwait(false);
    object result = task.GetType().GetProperty("Result").GetValue(task, null);
    AssertTrue(!(bool)result.GetType().GetProperty("Success").GetValue(result, null), "TTS provider HTTP error was accepted");
    AssertTrue((string)result.GetType().GetProperty("ErrorCode").GetValue(result, null) == "tts_http_500", "TTS provider HTTP error mapping changed");
}

using (ReplayServer invalidAudio = ReplayServer.Start("not-base64", 0))
{
    using HttpClient client = new HttpClient();
    object gateway = Activator.CreateInstance(gatewayType, client);
    Task task = (Task)gatewayType.GetMethod("SynthesizeAsync").Invoke(gateway, new[] { BuildRequest(invalidAudio.Url), "tts-replay-secret", CancellationToken.None });
    await task.ConfigureAwait(false);
    object result = task.GetType().GetProperty("Result").GetValue(task, null);
    AssertTrue((string)result.GetType().GetProperty("ErrorCode").GetValue(result, null) == "tts_audio_base64_invalid", "TTS invalid base64 mapping changed");
}

using (ReplayServer cancelled = ReplayServer.Start("AQID", 5000))
{
    using HttpClient client = new HttpClient();
    object gateway = Activator.CreateInstance(gatewayType, client);
    using CancellationTokenSource cancellation = new CancellationTokenSource(150);
    Task task = (Task)gatewayType.GetMethod("SynthesizeAsync").Invoke(gateway, new[] { BuildRequest(cancelled.Url), "tts-replay-secret", cancellation.Token });
    await task.ConfigureAwait(false);
    object result = task.GetType().GetProperty("Result").GetValue(task, null);
    AssertTrue((string)result.GetType().GetProperty("ErrorCode").GetValue(result, null) == "tts_cancelled", "TTS caller cancellation was not isolated");
}

using (ReplayServer malformed = ReplayServer.Start("AQID", 0))
{
    using HttpClient client = new HttpClient();
    object gateway = Activator.CreateInstance(gatewayType, client);
    Task task = (Task)gatewayType.GetMethod("SynthesizeAsync").Invoke(gateway, new[] { BuildRequest(malformed.Url, "not-json"), "tts-replay-secret", CancellationToken.None });
    await task.ConfigureAwait(false);
    object result = task.GetType().GetProperty("Result").GetValue(task, null);
    AssertTrue((string)result.GetType().GetProperty("ErrorCode").GetValue(result, null) == "tts_extra_parameters_invalid" && malformed.RequestCount == 0, "TTS malformed extra parameters reached provider");
}

Console.WriteLine("PASS ttsGatewayReplay success=1 headers=1 credentialBoundary=1 providerError=1 invalidAudio=1 cancellation=1 malformedExtra=1");

internal sealed class ReplayServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly string _responseBody;
    private readonly int _delayMilliseconds;
    private readonly int _statusCode;
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _acceptLoop;
    private int _requestCount;
    private string _requestText = string.Empty;

    private ReplayServer(TcpListener listener, int delayMilliseconds, int statusCode, string body)
    {
        _listener = listener;
        _delayMilliseconds = delayMilliseconds;
        _statusCode = statusCode;
        _responseBody = body;
        Url = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/tts";
        _acceptLoop = AcceptLoopAsync();
    }

    public string Url { get; }
    public int RequestCount => Volatile.Read(ref _requestCount);
    public string RequestText => Volatile.Read(ref _requestText);

    public static ReplayServer Start(string audioToken, int delayMilliseconds, int statusCode = 200, string body = null)
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        string response = body ?? "{\"code\":3000,\"data\":\"" + audioToken + "\"}";
        return new ReplayServer(listener, delayMilliseconds, statusCode, response);
    }

    private async Task AcceptLoopAsync()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                _ = HandleAsync(client);
            }
        }
        catch (ObjectDisposedException) { }
        catch (SocketException) { }
    }

    private async Task HandleAsync(TcpClient client)
    {
        using (client)
        {
            Interlocked.Increment(ref _requestCount);
            using NetworkStream stream = client.GetStream();
            using MemoryStream request = new MemoryStream();
            byte[] buffer = new byte[4096];
            int read;
            int headerLength;
            do
            {
                read = await stream.ReadAsync(buffer).ConfigureAwait(false);
                if (read > 0) request.Write(buffer, 0, read);
                headerLength = FindHeaderEnd(request);
            }
            while (read > 0 && headerLength < 0);

            if (headerLength >= 0)
            {
                string headers = Encoding.ASCII.GetString(request.GetBuffer(), 0, headerLength);
                int contentLength = GetContentLength(headers);
                int bodyBytesRead = (int)request.Length - headerLength - 4;
                while (bodyBytesRead < contentLength)
                {
                    read = await stream.ReadAsync(buffer).ConfigureAwait(false);
                    if (read <= 0) break;
                    request.Write(buffer, 0, read);
                    bodyBytesRead += read;
                }
            }

            Volatile.Write(ref _requestText, Encoding.UTF8.GetString(request.ToArray()));
            if (_delayMilliseconds > 0)
            {
                try { await Task.Delay(_delayMilliseconds, _stop.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) { return; }
            }
            byte[] body = Encoding.UTF8.GetBytes(_responseBody);
            string responseHeaders = "HTTP/1.1 " + _statusCode + " Test\r\nContent-Type: application/json\r\nContent-Length: " + body.Length + "\r\nConnection: close\r\n\r\n";
            await stream.WriteAsync(Encoding.ASCII.GetBytes(responseHeaders)).ConfigureAwait(false);
            await stream.WriteAsync(body).ConfigureAwait(false);
        }
    }

    private static int FindHeaderEnd(MemoryStream request)
    {
        byte[] bytes = request.GetBuffer();
        int length = (int)request.Length;
        for (int i = 3; i < length; i++)
        {
            if (bytes[i - 3] == '\r' && bytes[i - 2] == '\n' && bytes[i - 1] == '\r' && bytes[i] == '\n')
            {
                return i - 3;
            }
        }

        return -1;
    }

    private static int GetContentLength(string headers)
    {
        foreach (string line in headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
        {
            const string prefix = "Content-Length:";
            if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(line.Substring(prefix.Length).Trim(), out int length))
            {
                return length;
            }
        }

        return 0;
    }

    public void Dispose()
    {
        _stop.Cancel();
        _listener.Stop();
        try { _acceptLoop.GetAwaiter().GetResult(); } catch { }
    }
}
