using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
string projectRoot = Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory,
    "..", "..", "..", "..", ".."));
string referenceDirectory = Path.Combine(projectRoot, ".tmp", "build_check", "1.4");
string implementationPath = Path.Combine(stageDirectory, "versions", "1.4", "AnimusForge.dll");
AssertTrue(File.Exists(implementationPath), "project-local 1.4 AnimusForge.dll is missing");

AppDomain.CurrentDomain.AssemblyResolve += (_, arguments) =>
{
    string name = new AssemblyName(arguments.Name).Name;
    foreach (string root in new[] { AppContext.BaseDirectory, stageDirectory, referenceDirectory })
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
MethodInfo pushMethod = animusForge.GetType("AnimusForge.ShoutNetwork", true)
    .GetMethod("PushNonStreamingTransportOverrideForExternal", BindingFlags.Public | BindingFlags.Static);
AssertTrue(sendMethod != null && pushMethod != null, "production primary Gateway replay seam is missing");

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

Console.WriteLine("PASS primaryLlmGatewayReplay success=1 thinkingPlainRetry=1 credentialBoundary=1 cancellation=1");
