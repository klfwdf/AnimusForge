using System.Diagnostics;
using System.Text.Json;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;

namespace BridgeRuntimeIsolationTests;

internal static class Program
{
    private static readonly string[] WiredIds =
    {
        FeatureBridgeIds.ConversationGateway,
        FeatureBridgeIds.ConversationAction,
        FeatureBridgeIds.ActionMemory,
        FeatureBridgeIds.ActionEconomy,
        FeatureBridgeIds.PolicyWorldDiplomacy,
        FeatureBridgeIds.ConversationSiege,
        FeatureBridgeIds.ConversationCourier,
        FeatureBridgeIds.MemorySocialReports,
        FeatureBridgeIds.GatewayKnowledgeProfile,
        FeatureBridgeIds.UiRuntimeIntegration,
    };

    private static int Main(string[] args)
    {
        if (args.Length > 0 && string.Equals(args[0], "child", StringComparison.Ordinal))
        {
            return Child();
        }

        try
        {
            RunParent();
            Console.WriteLine("PASS BridgeRuntimeIsolationTests scenarios=9");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("FAIL " + exception.Message);
            return 1;
        }
    }

    private static void RunParent()
    {
        AssertScenario("missing", null, expectedValid: true, expectedEnabled: WiredIds);
        AssertScenario("empty", "{\"schemaVersion\":1,\"contractVersion\":1,\"enabled\":[]}", expectedValid: true, expectedEnabled: Array.Empty<string>());

        string[] invalid =
        {
            "{not-json",
            "{\"schemaVersion\":1,\"contractVersion\":1,\"enabled\":[\"conversation-gateway\",\"conversation-gateway\"]}",
            "{\"schemaVersion\":1,\"contractVersion\":1,\"enabled\":[\"unknown\"]}",
            "{\"schemaVersion\":1,\"contractVersion\":1,\"enabled\":[\"Conversation-Gateway\"]}",
            "{\"schemaVersion\":1,\"contractVersion\":99,\"enabled\":[]}",
        };
        foreach (string config in invalid)
        {
            AssertScenario("invalid", config, expectedValid: false, expectedEnabled: Array.Empty<string>());
        }

        // A same-named file in CWD is deliberately not a module boundary.
        AssertScenario("cwd-trap", null, expectedValid: true, expectedEnabled: WiredIds, cwdTrap: true);

        ChildResult disabled = RunChild("empty", "{\"schemaVersion\":1,\"contractVersion\":1,\"enabled\":[]}", false);
        foreach (DecisionResult decision in disabled.Decisions)
        {
            Require(decision.Status == FeatureBridgeDecisionStatus.Disabled.ToString(), "disabled status mismatch for " + decision.Id);
            DefinitionResult definition = disabled.Definitions.Single(item => item.Id == decision.Id);
            Require(decision.Fallback == definition.Fallback, "disabled fallback mismatch for " + decision.Id);
        }

        ChildResult invalidResult = RunChild("invalid", "{not-json", false);
        AssertFallbacksMatchDefinitions(invalidResult);
        Require(FeatureBridgeRuntime.Evaluate("not-a-bridge", 1).ReasonCode == "bridge.unknown", "unknown reason code mismatch");
        Require(FeatureBridgeRuntime.Evaluate(FeatureBridgeIds.ConversationGateway, 999).ReasonCode == "bridge.contract_version_mismatch", "contract mismatch reason code mismatch");
        Require(FeatureBridgeRuntime.Evaluate(FeatureBridgeIds.ConversationGateway, 1, 7, 8).ReasonCode == "bridge.stale_generation", "stale generation reason code mismatch");
        Require(invalidResult.Reason.Contains("configuration rejected", StringComparison.Ordinal), "invalid configuration reason missing");
    }

    private static void AssertScenario(string name, string config, bool expectedValid, IReadOnlyCollection<string> expectedEnabled, bool cwdTrap = false)
    {
        ChildResult result = RunChild(name, config, cwdTrap);
        Require(result.Valid == expectedValid, name + " validity mismatch");
        HashSet<string> enabled = result.Enabled.ToHashSet(StringComparer.Ordinal);
        Require(enabled.SetEquals(expectedEnabled), name + " enabled set mismatch");
        Require(result.Definitions.Count == FeatureBridgeIds.All.Count, name + " definition count mismatch");
        AssertFallbacksMatchDefinitions(result);
        if (!expectedValid)
        {
            Require(result.Enabled.Count == 0, name + " invalid configuration must disable all bridges");
        }
    }

    private static void AssertFallbacksMatchDefinitions(ChildResult result)
    {
        foreach (DecisionResult decision in result.Decisions)
        {
            DefinitionResult definition = result.Definitions.Single(item => item.Id == decision.Id);
            Require(decision.Fallback == definition.Fallback, "fallback mismatch for " + decision.Id);
        }
    }

    private static ChildResult RunChild(string name, string config, bool cwdTrap)
    {
        string sourceDirectory = AppContext.BaseDirectory;
        string tempRoot = Path.Combine(Path.GetTempPath(), "af-bridge-isolation-" + Guid.NewGuid().ToString("N"));
        string moduleRoot = Path.Combine(tempRoot, "AnimusForge");
        string moduleData = Path.Combine(moduleRoot, "ModuleData");
        string cwd = Path.Combine(tempRoot, "cwd");
        Directory.CreateDirectory(moduleRoot);
        Directory.CreateDirectory(moduleData);
        Directory.CreateDirectory(cwd);
        File.WriteAllText(Path.Combine(moduleRoot, "SubModule.xml"), "<Module />");
        if (config is not null)
        {
            File.WriteAllText(Path.Combine(moduleData, "FeatureBridges.json"), config);
        }
        if (cwdTrap)
        {
            File.WriteAllText(Path.Combine(cwd, "FeatureBridges.json"), "{not-json");
        }
        foreach (string file in Directory.EnumerateFiles(sourceDirectory))
        {
            File.Copy(file, Path.Combine(moduleRoot, Path.GetFileName(file)), true);
        }

        string executable = Environment.ProcessPath ?? throw new InvalidOperationException("process path unavailable");
        string childExecutable = Path.Combine(moduleRoot, Path.GetFileName(executable));
        ProcessStartInfo start = new ProcessStartInfo(childExecutable)
        {
            WorkingDirectory = cwd,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.ArgumentList.Add("child");
        start.Environment["AF_BRIDGE_SCENARIO"] = name;
        // The child assembly is loaded from the module boundary.  Its
        // AppContext.BaseDirectory therefore resolves to AnimusForge, not CWD.
        Process process = Process.Start(start) ?? throw new InvalidOperationException("child process failed to start");
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        try
        {
            Require(process.ExitCode == 0, name + " child failed: " + stderr + stdout);
            ChildResult result = JsonSerializer.Deserialize<ChildResult>(stdout) ?? throw new InvalidOperationException("empty child result");
            return result;
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { }
        }
    }

    private static int Child()
    {
        bool valid = FeatureBridgeRuntime.Initialize(out string reason);
        FeatureBridgeValidationSnapshot snapshot = FeatureBridgeRuntime.GetValidationSnapshot();
        List<string> enabled = FeatureBridgeIds.All.Where(FeatureBridgeRuntime.IsEnabled).ToList();
        Console.WriteLine(JsonSerializer.Serialize(new ChildResult
        {
            Valid = valid,
            Reason = reason,
            Enabled = enabled,
            Definitions = snapshot.Definitions.Select(definition => new DefinitionResult
            {
                Id = definition.Id,
                Fallback = definition.Fallback.ToString(),
            }).ToList(),
            Decisions = snapshot.Definitions.Select(definition =>
            {
                FeatureBridgeDecision decision = FeatureBridgeRuntime.Evaluate(
                    definition.Id,
                    FeatureBridgeIds.ContractVersion);
                return new DecisionResult
                {
                    Id = decision.BridgeId,
                    Status = decision.Status.ToString(),
                    Fallback = decision.Fallback.ToString(),
                    Reason = decision.ReasonCode,
                };
            }).ToList(),
        }));
        return 0;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class ChildResult
    {
        public bool Valid { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<string> Enabled { get; set; } = new();
        public List<DefinitionResult> Definitions { get; set; } = new();
        public List<DecisionResult> Decisions { get; set; } = new();
    }

    private sealed class DefinitionResult
    {
        public string Id { get; set; } = string.Empty;
        public string Fallback { get; set; } = string.Empty;
    }

    private sealed class DecisionResult
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Fallback { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
