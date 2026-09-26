using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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

string artifactDirectory = Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory,
    "..", "..", "..", "..", "..",
    "bin", "Debug", "single_module_artifacts"));
string projectRoot = Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory,
    "..", "..", "..", "..", ".."));
string referenceDirectory = Path.Combine(projectRoot, ".tmp", "build_check", "1.4");
string implementationPath = Path.Combine(artifactDirectory, "versions", "1.4", "AnimusForge.dll");
AssertTrue(File.Exists(implementationPath), "project-local 1.4 AnimusForge.dll is missing");

AppDomain.CurrentDomain.AssemblyResolve += (_, arguments) =>
{
    string name = new AssemblyName(arguments.Name).Name;
    foreach (string root in new[] { AppContext.BaseDirectory, artifactDirectory, referenceDirectory,
        Path.Combine(projectRoot, "..", "..", ".dotnet-sdk", "sdk", "8.0.422") })
    {
        if (!Directory.Exists(root))
        {
            continue;
        }
        foreach (string candidate in Directory.GetFiles(root, name + ".dll", SearchOption.AllDirectories))
        {
            try
            {
                return Assembly.LoadFrom(candidate);
            }
            catch
            {
            }
        }
    }
    return null;
};

Assembly animusForge = Assembly.LoadFrom(implementationPath);
Type settingsType = animusForge.GetType("AnimusForge.DuelSettings", true);
Type gatewayType = animusForge.GetType("AnimusForge.Refactor.Contracts.LegacyShoutNetworkGateway", true);
object settings = settingsType.GetMethod("GetSettings", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
settingsType.GetProperty("ApiUrl").SetValue(settings, "http://replay.invalid/v1/chat/completions", null);
settingsType.GetProperty("ApiKey").SetValue(settings, "primary-replay-secret", null);
settingsType.GetProperty("ModelName").SetValue(settings, "deepseek-replay", null);
settingsType.GetProperty("MainApiThinkingEnabled").SetValue(settings, true, null);

MethodInfo sendMethod = gatewayType.GetMethod("SendLegacyMessagesAsync", BindingFlags.Public | BindingFlags.Static);
MethodInfo streamMethod = gatewayType.GetMethod("SendLegacyMessagesStreamAsync", BindingFlags.Public | BindingFlags.Static);
MethodInfo pushMethod = animusForge.GetType("AnimusForge.ShoutNetwork", true)
    .GetMethod("PushNonStreamingTransportOverrideForExternal", BindingFlags.Public | BindingFlags.Static);
MethodInfo pushStreamMethod = animusForge.GetType("AnimusForge.ShoutNetwork", true)
    .GetMethod("PushStreamingTransportOverrideForExternal", BindingFlags.Public | BindingFlags.Static);
AssertTrue(sendMethod != null && streamMethod != null && pushMethod != null && pushStreamMethod != null,
    "production primary Gateway replay seam is missing");

List<string> requestBodies = new List<string>();
int requestCount = 0;
Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sender = (request, token) =>
{
    string body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty;
    lock (requestBodies)
    {
        requestBodies.Add(body);
    }
    int index = Interlocked.Increment(ref requestCount);
    if (index == 1)
    {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("thinking unsupported", Encoding.UTF8, "application/json")
        });
    }
    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent(
            "{\"choices\":[{\"message\":{\"content\":\"主回复回放\"}}]}",
            Encoding.UTF8,
            "application/json")
    });
};

using (IDisposable scope = (IDisposable)pushMethod.Invoke(null, new object[] { sender }))
{
    List<object> messages = new List<object>
    {
        new Dictionary<string, object> { ["role"] = "system", ["content"] = "replay system" },
        new Dictionary<string, object> { ["role"] = "user", ["content"] = "replay user" }
    };
    Task task = (Task)sendMethod.Invoke(null, new object[]
    {
        messages,
        5000,
        true,
        null,
        false,
        false,
        CancellationToken.None,
        null
    });
    await task.ConfigureAwait(false);
    string result = (string)task.GetType().GetProperty("Result").GetValue(task, null);
    AssertTrue(result == "主回复回放", "primary Gateway non-stream final text mismatch");
}

AssertTrue(requestCount == 2, "primary Gateway did not perform thinking plain retry");
AssertTrue(requestBodies.Count == 2, "primary Gateway request body capture count mismatch");
AssertTrue(requestBodies[0].Contains("thinking", StringComparison.OrdinalIgnoreCase), "primary first request lacked thinking controls");
AssertTrue(!requestBodies[1].Contains("thinking", StringComparison.OrdinalIgnoreCase), "primary plain retry retained thinking controls");
AssertTrue(requestBodies.All(body => !body.Contains("primary-replay-secret", StringComparison.Ordinal)), "primary credential leaked into request body");

using (IDisposable cancellationScope = (IDisposable)pushMethod.Invoke(null, new object[]
{
    new Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>(async (_, token) =>
    {
        await Task.Delay(5000, token).ConfigureAwait(false);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"choices\":[{\"message\":{\"content\":\"late\"}}]}")
        };
    })
}))
{
    using CancellationTokenSource cancellation = new CancellationTokenSource(100);
    List<object> messages = new List<object>
    {
        new Dictionary<string, object> { ["role"] = "user", ["content"] = "cancel me" }
    };
    Task task = (Task)sendMethod.Invoke(null, new object[]
    {
        messages,
        5000,
        false,
        null,
        false,
        false,
        cancellation.Token,
        null
    });
    bool cancelled = false;
    try
    {
        await task.ConfigureAwait(false);
    }
    catch (OperationCanceledException)
    {
        cancelled = true;
    }
    AssertTrue(cancelled, "primary Gateway caller cancellation was not propagated");
}

List<string> streamBodies = new List<string>();
int streamRequests = 0;
Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> streamSender = async (request, token) =>
{
    streamBodies.Add(await request.Content.ReadAsStringAsync().ConfigureAwait(false));
    int index = Interlocked.Increment(ref streamRequests);
    if (index == 1)
    {
        return new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("thinking unsupported", Encoding.UTF8, "application/json")
        };
    }
    string sse = "data: {\"choices\":[{\"delta\":{\"reasoning_content\":\"理由\"}}]}\n\n"
        + "data: {\"choices\":[{\"delta\":{\"content\":\"流式🙂\"}}]}\n\n"
        + "data: {\"choices\":[{\"delta\":{\"content\":\"回复\"}}]}\n\n"
        + "data: [DONE]\n\n";
    return new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent(sse, Encoding.UTF8, "text/event-stream")
    };
};
using (IDisposable streamScope = (IDisposable)pushStreamMethod.Invoke(null, new object[] { streamSender }))
{
    List<string> deltas = new List<string>();
    string completed = null;
    string failure = null;
    Task task = (Task)streamMethod.Invoke(null, new object[]
    {
        new List<object> { new Dictionary<string, object> { ["role"] = "user", ["content"] = "stream" } },
        5000,
        new Action<string>(deltas.Add),
        new Action<string>(text => completed = text),
        new Action<string>(text => failure = text),
        CancellationToken.None,
        false
    });
    await task.ConfigureAwait(false);
    AssertTrue(failure == null, "primary stream unexpectedly failed: " + failure);
    AssertTrue(completed == "流式🙂回复", "primary stream final text mismatch: " + completed);
    AssertTrue(string.Concat(deltas) == completed, "primary stream deltas duplicated or lost Unicode content");
}
AssertTrue(streamRequests == 2, "primary stream thinking fallback did not send exactly two attempts");
AssertTrue(streamBodies[0].Contains("thinking", StringComparison.OrdinalIgnoreCase), "stream first attempt lacked thinking controls");
AssertTrue(!streamBodies[1].Contains("thinking", StringComparison.OrdinalIgnoreCase), "stream fallback retained thinking controls");

using (IDisposable partialScope = (IDisposable)pushStreamMethod.Invoke(null, new object[]
{
    new Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>((_, _) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new FaultingSseContent("data: {\"choices\":[{\"delta\":{\"content\":\"部分\"}}]}\n\n")
        }))
}))
{
    string completed = null;
    string failure = null;
    Task task = (Task)streamMethod.Invoke(null, new object[]
    {
        new List<object> { new Dictionary<string, object> { ["role"] = "user", ["content"] = "partial" } },
        5000, new Action<string>(_ => { }), new Action<string>(text => completed = text),
        new Action<string>(text => failure = text), CancellationToken.None, false
    });
    await task.ConfigureAwait(false);
    AssertTrue(completed == "部分" && failure == null,
        "published partial stream was replayed or discarded completed=" + completed + " failure=" + failure);
}

using (IDisposable streamCancelScope = (IDisposable)pushStreamMethod.Invoke(null, new object[]
{
    new Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>(async (_, token) =>
    {
        await Task.Delay(5000, token).ConfigureAwait(false);
        return new HttpResponseMessage(HttpStatusCode.OK);
    })
}))
{
    using CancellationTokenSource cancellation = new CancellationTokenSource(100);
    string completed = null;
    string failure = null;
    Task task = (Task)streamMethod.Invoke(null, new object[]
    {
        new List<object> { new Dictionary<string, object> { ["role"] = "user", ["content"] = "cancel stream" } },
        5000, new Action<string>(_ => { }), new Action<string>(text => completed = text),
        new Action<string>(text => failure = text), cancellation.Token, false
    });
    await task.ConfigureAwait(false);
    AssertTrue(completed == string.Empty && failure == null,
        "primary stream cancellation changed its existing empty partial completion contract");
}

Console.WriteLine("PASS primaryLlmGatewayReplay nonStream=1 stream=1 thinkingFallbacks=2 Unicode=1 partialNoReplay=1 credentialBoundary=1 cancellations=2");

internal sealed class FaultingSseContent : HttpContent
{
    private readonly byte[] _prefix;
    internal FaultingSseContent(string prefix)
    {
        _prefix = Encoding.UTF8.GetBytes(prefix);
        Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");
    }
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) => throw new NotSupportedException();
    protected override bool TryComputeLength(out long length) { length = -1; return false; }
    protected override Task<Stream> CreateContentReadStreamAsync()
        => Task.FromResult<Stream>(new FaultAfterPrefixStream(_prefix));
}

internal sealed class FaultAfterPrefixStream : MemoryStream
{
    private bool _failed;
    internal FaultAfterPrefixStream(byte[] prefix) : base(prefix) { }
    public override int Read(byte[] buffer, int offset, int count)
    {
        int read = base.Read(buffer, offset, count);
        if (read > 0) return read;
        if (!_failed) { _failed = true; throw new IOException("fixture stream failure"); }
        return 0;
    }
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await Task.Yield();
        return Read(buffer, offset, count);
    }
}
