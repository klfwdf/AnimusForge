using System.Collections.ObjectModel;
using AnimusForge.Refactor.Modules;

static class Program
{
    private static int _passed;
    private static int _failed;

    private static InternalModuleDirectory Directory(Func<string, bool> known = null, Func<string, string> gate = null)
        => new(known ?? (id => id == "conversation-siege" || id == "host-runtime"), gate ?? (_ => null));

    private static InternalCapabilityDefinition Cap(string id = "test.read", int version = 1, params string[] bridges)
        => new(id, version, bridges);

    private static InternalModuleDefinition Mod(string id = "test", int version = 1,
        InternalCapabilityDefinition[] capabilities = null, params InternalModuleDependency[] dependencies)
        => new(id, version, capabilities ?? new[] { Cap(id + ".read") }, dependencies);

    private static void Assert(bool condition, string message)
    {
        if (!condition) { throw new InvalidOperationException(message); }
    }

    private static void Register(InternalModuleDirectory directory, InternalModuleDefinition module)
    {
        Assert(directory.TryRegister(module, out string reason), "registration rejected: " + reason);
    }

    private static void State(InternalModuleDirectory directory, string capability, InternalCapabilityState expected, int version = 1)
    {
        InternalCapabilityStatus result = directory.GetCapabilityStatus(capability, version);
        Assert(result.State == expected, $"{capability}: expected {expected}, actual {result.State} ({result.ReasonCode})");
        Assert(result.IsAvailable == (expected == InternalCapabilityState.Available), "availability flag differs from state");
    }

    private static void Ready(InternalModuleDirectory directory, params string[] modules)
    {
        foreach (string module in modules)
        {
            Assert(directory.UpdateRuntimeState(module, InternalModuleRuntimeState.Ready), "ready update rejected: " + module);
        }
    }

    private static void Rejected(string name, InternalModuleDefinition module, string code)
    {
        Case(name, () =>
        {
            InternalModuleDirectory directory = Directory();
            Assert(!directory.TryRegister(module, out string reason) && reason.StartsWith(code, StringComparison.Ordinal), "unexpected rejection: " + reason);
            Assert(directory.GetSnapshot().Count == 0, "rejected definition leaked into catalog");
        });
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }

    private static void Case(string name, Action body)
    {
        try { body(); _passed++; Console.WriteLine("PASS " + name); }
        catch (Exception exception) { _failed++; Console.WriteLine("FAIL " + name + ": " + exception); }
    }

    private static int Main()
    {
        Console.OutputEncoding = new System.Text.UTF8Encoding(false);
        Case("constructor rejects missing gate adapters", () =>
        {
            Throws<ArgumentNullException>(() => new InternalModuleDirectory(null, _ => null));
            Throws<ArgumentNullException>(() => new InternalModuleDirectory(_ => true, null));
        });
        Case("open catalog does not advertise availability", () =>
        {
            var directory = Directory(); Register(directory, Mod());
            State(directory, "test.read", InternalCapabilityState.RegistrationOpen);
            var module = directory.GetSnapshot().Single();
            Assert(module.State == InternalModuleRuntimeState.NotInitialized && !module.IsRegistrationComplete && !module.IsDefinitionValid, "registration was mistaken for initialized routing");
            Assert(!directory.UpdateRuntimeState("test", InternalModuleRuntimeState.Ready), "state changed before registration completed");
        });
        Case("empty directory can freeze without inventing capabilities", () =>
        {
            var directory = Directory(); Assert(directory.CompleteRegistration().IsValid, "empty catalog invalid");
            Assert(directory.GetSnapshot().Count == 0, "invented module");
            State(directory, "unknown", InternalCapabilityState.UnknownCapability);
        });
        Case("definitions deeply protect mutable input collections", () =>
        {
            var bridgeIds = new List<string> { "host-runtime" };
            var cap = new InternalCapabilityDefinition("test.read", 1, bridgeIds);
            var capabilities = new List<InternalCapabilityDefinition> { cap };
            var dependencies = new List<InternalModuleDependency> { new("base", 1) };
            var module = new InternalModuleDefinition("test", 1, capabilities, dependencies);
            bridgeIds.Clear(); capabilities.Clear(); dependencies.Clear();
            Assert(module.Capabilities.Count == 1 && cap.RequiredFeatureBridgeIds.Count == 1 && module.RequiredModules.Count == 1, "source mutation affected immutable definition");
            Throws<NotSupportedException>(() => ((IList<string>)cap.RequiredFeatureBridgeIds).Add("injected"));
            Throws<NotSupportedException>(() => ((IList<InternalCapabilityDefinition>)module.Capabilities).Clear());
            Throws<NotSupportedException>(() => ((IList<InternalModuleDependency>)module.RequiredModules).Clear());
        });
        Rejected("null module", null, "module.definition_missing");
        Rejected("blank module ID", Mod(" "), "module.id_invalid");
        Rejected("embedded whitespace ID", Mod("invalid id"), "module.id_invalid");
        Rejected("non-ASCII stable ID", Mod("模块"), "module.id_invalid");
        Rejected("oversized ID", Mod(new string('x', 129)), "module.id_invalid");
        Rejected("zero module version", Mod(version: 0), "module.version_invalid");
        Rejected("negative module version", Mod(version: -1), "module.version_invalid");
        Rejected("null capability", Mod(capabilities: new InternalCapabilityDefinition[] { null }), "capability.id_invalid");
        Rejected("blank capability", Mod(capabilities: new[] { Cap(" ") }), "capability.id_invalid");
        Rejected("invalid capability version", Mod(capabilities: new[] { Cap(version: 0) }), "capability.version_invalid");
        Rejected("duplicate local capability", Mod(capabilities: new[] { Cap(), Cap() }), "capability.duplicate");
        Rejected("null dependency", Mod(dependencies: new InternalModuleDependency[] { null }), "module.dependency_invalid");
        Rejected("blank dependency", Mod(dependencies: new[] { new InternalModuleDependency(" ", 1) }), "module.dependency_invalid");
        Rejected("invalid dependency version", Mod(dependencies: new[] { new InternalModuleDependency("base", 0) }), "module.dependency_version_invalid");
        Rejected("duplicate dependency", Mod(dependencies: new[] { new InternalModuleDependency("base", 1), new InternalModuleDependency("base", 2) }), "module.dependency_duplicate");
        Rejected("unknown FeatureBridge", Mod(capabilities: new[] { Cap("test.read", 1, "invented-bridge") }), "capability.bridge_unknown");
        Rejected("blank FeatureBridge", Mod(capabilities: new[] { Cap("test.read", 1, " ") }), "capability.bridge_invalid");
        Rejected("duplicate FeatureBridge", Mod(capabilities: new[] { Cap("test.read", 1, "host-runtime", "host-runtime") }), "capability.bridge_duplicate");
        Case("bridge discovery failure rejects declaration closed", () =>
        {
            var directory = Directory(_ => throw new InvalidOperationException("fixture"));
            Assert(!directory.TryRegister(Mod(capabilities: new[] { Cap("test.read", 1, "host-runtime") }), out var reason), "gate exception accepted");
            Assert(reason == "capability.bridge_validation_failed:host-runtime" && directory.GetSnapshot().Count == 0, "unbounded error or partial registration");
        });
        Case("duplicate module cannot replace accepted definition", () =>
        {
            var directory = Directory(); var first = Mod(); Register(directory, first);
            Assert(!directory.TryRegister(Mod(version: 2), out var reason) && reason == "module.duplicate:test", "duplicate accepted");
            Assert(ReferenceEquals(directory.GetSnapshot().Single().Definition, first), "original overwritten");
            Assert(directory.CompleteRegistration().IsValid, "rejected declaration poisoned accepted catalog");
        });
        Case("conflicting capability rejects complete module atomically", () =>
        {
            var directory = Directory(); Register(directory, Mod("first", capabilities: new[] { Cap("shared") }));
            Assert(!directory.TryRegister(Mod("second", capabilities: new[] { Cap("new.read"), Cap("shared") }), out var reason) && reason == "capability.duplicate:shared", "conflict accepted");
            directory.CompleteRegistration(); Ready(directory, "first");
            Assert(directory.GetSnapshot().Count == 1, "partial module added");
            State(directory, "new.read", InternalCapabilityState.UnknownCapability);
            Assert(directory.GetCapabilityStatus("shared", 1).ModuleId == "first", "provider overwritten");
        });
        Case("registration freeze is idempotent and final", () =>
        {
            var directory = Directory(); Register(directory, Mod()); var result = directory.CompleteRegistration();
            Assert(ReferenceEquals(result, directory.CompleteRegistration()), "freeze recomputed result");
            Assert(!directory.TryRegister(Mod("late"), out var reason) && reason == "module.registration_closed", "late registration accepted");
            State(directory, "test.read", InternalCapabilityState.ModuleNotInitialized);
        });
        Case("state reporting is explicit and preserves snapshot history", () =>
        {
            var directory = Directory(); Register(directory, Mod()); directory.CompleteRegistration();
            var before = directory.GetSnapshot(); Assert(ReferenceEquals(before, directory.GetSnapshot()), "stable snapshot not cached");
            Ready(directory, "test"); var after = directory.GetSnapshot();
            Assert(before.Single().State == InternalModuleRuntimeState.NotInitialized && after.Single().State == InternalModuleRuntimeState.Ready, "snapshot mutated in place");
            Assert(after.Single().ReasonCode == "module.ready" && after.Single().IsDefinitionValid, "snapshot metadata incorrect");
            Throws<NotSupportedException>(() => ((IList<InternalModuleStatus>)after).Clear());
            State(directory, "test.read", InternalCapabilityState.Available);
        });
        Case("runtime unavailable failed and recovery remain distinct", () =>
        {
            var directory = Directory(); Register(directory, Mod()); directory.CompleteRegistration();
            directory.UpdateRuntimeState("test", InternalModuleRuntimeState.Unavailable, "fixture.unavailable");
            State(directory, "test.read", InternalCapabilityState.ModuleUnavailable);
            Assert(directory.GetSnapshot()[0].ReasonCode == "fixture.unavailable", "state reason lost");
            directory.UpdateRuntimeState("test", InternalModuleRuntimeState.Failed); State(directory, "test.read", InternalCapabilityState.ModuleFailed);
            Ready(directory, "test"); State(directory, "test.read", InternalCapabilityState.Available);
            directory.UpdateRuntimeState("test", InternalModuleRuntimeState.NotInitialized); State(directory, "test.read", InternalCapabilityState.ModuleNotInitialized);
        });
        Case("unknown module and invalid state cannot change directory", () =>
        {
            var directory = Directory(); Register(directory, Mod()); directory.CompleteRegistration();
            Assert(!directory.UpdateRuntimeState("unknown", InternalModuleRuntimeState.Ready), "unknown module accepted");
            Assert(!directory.UpdateRuntimeState(null, InternalModuleRuntimeState.Ready), "null module accepted");
            Assert(!directory.UpdateRuntimeState("test", (InternalModuleRuntimeState)99), "invalid enum accepted");
            Assert(!directory.UpdateRuntimeState("test", InternalModuleRuntimeState.Ready, new string('x', 257)), "oversized reason accepted");
            State(directory, "test.read", InternalCapabilityState.ModuleNotInitialized);
        });
        Case("capability version and missing ID are concrete failures", () =>
        {
            var directory = Directory(); Register(directory, Mod(capabilities: new[] { Cap("test.read", 3) })); directory.CompleteRegistration(); Ready(directory, "test");
            State(directory, "test.read", InternalCapabilityState.VersionMismatch, 1); State(directory, "test.read", InternalCapabilityState.VersionMismatch, 0);
            State(directory, "test.read", InternalCapabilityState.VersionMismatch, -1); State(directory, "test.read", InternalCapabilityState.Available, 3);
            var mismatch = directory.GetCapabilityStatus("test.read", 2);
            Assert(mismatch.ContractVersion == 3 && mismatch.RequestedContractVersion == 2 && mismatch.ModuleId == "test", "version identity lost");
            State(directory, null, InternalCapabilityState.UnknownCapability); State(directory, "missing", InternalCapabilityState.UnknownCapability);
        });
        Case("missing dependencies block affected consumers only", () =>
        {
            var directory = Directory(); Register(directory, Mod("broken", dependencies: new[] { new InternalModuleDependency("missing", 1) }));
            Register(directory, Mod("dependent", dependencies: new[] { new InternalModuleDependency("broken", 1) })); Register(directory, Mod("unrelated"));
            Assert(!directory.CompleteRegistration().IsValid, "missing dependency marked valid"); Ready(directory, "broken", "dependent", "unrelated");
            State(directory, "broken.read", InternalCapabilityState.Blocked); State(directory, "dependent.read", InternalCapabilityState.Blocked);
            State(directory, "unrelated.read", InternalCapabilityState.Available);
        });
        Case("dependency version is exact and propagates to consumers", () =>
        {
            var directory = Directory(); Register(directory, Mod("base", 2));
            Register(directory, Mod("wrong", dependencies: new[] { new InternalModuleDependency("base", 1) }));
            Register(directory, Mod("right", dependencies: new[] { new InternalModuleDependency("base", 2) }));
            directory.CompleteRegistration(); Ready(directory, "base", "wrong", "right");
            State(directory, "wrong.read", InternalCapabilityState.Blocked); State(directory, "right.read", InternalCapabilityState.Available);
            Assert(directory.GetCapabilityStatus("wrong.read", 1).ReasonCode == "module.dependency_version_mismatch:base", "wrong mismatch reason");
        });
        Case("self cycle is rejected at freeze", () =>
        {
            var directory = Directory(); Register(directory, Mod("self", dependencies: new[] { new InternalModuleDependency("self", 1) }));
            Assert(!directory.CompleteRegistration().IsValid, "self cycle valid"); Ready(directory, "self"); State(directory, "self.read", InternalCapabilityState.Blocked);
        });
        Case("cycle and dependent are blocked without poisoning independent module", () =>
        {
            var directory = Directory(); Register(directory, Mod("a", dependencies: new[] { new InternalModuleDependency("b", 1) }));
            Register(directory, Mod("b", dependencies: new[] { new InternalModuleDependency("a", 1) }));
            Register(directory, Mod("consumer", dependencies: new[] { new InternalModuleDependency("a", 1) })); Register(directory, Mod("independent"));
            var result = directory.CompleteRegistration(); Assert(!result.IsValid && result.Issues.Count == 3, "cycle diagnostics incomplete");
            Ready(directory, "a", "b", "consumer", "independent");
            foreach (string id in new[] { "a", "b", "consumer" }) { State(directory, id + ".read", InternalCapabilityState.Blocked); }
            State(directory, "independent.read", InternalCapabilityState.Available);
            Throws<NotSupportedException>(() => ((IList<string>)result.Issues).Add("inject"));
        });
        Case("transitive runtime readiness is separate from valid dependency graph", () =>
        {
            var directory = Directory(); Register(directory, Mod("a"));
            Register(directory, Mod("b", dependencies: new[] { new InternalModuleDependency("a", 1) }));
            Register(directory, Mod("c", dependencies: new[] { new InternalModuleDependency("b", 1) }));
            Assert(directory.CompleteRegistration().IsValid, "valid chain rejected"); Ready(directory, "b", "c");
            State(directory, "c.read", InternalCapabilityState.DependencyUnavailable);
            Ready(directory, "a"); State(directory, "c.read", InternalCapabilityState.Available);
            directory.UpdateRuntimeState("a", InternalModuleRuntimeState.Failed); State(directory, "b.read", InternalCapabilityState.DependencyUnavailable); State(directory, "c.read", InternalCapabilityState.DependencyUnavailable);
        });
        Case("diamond graph is not mistaken for cycle", () =>
        {
            var directory = Directory(); Register(directory, Mod("a"));
            Register(directory, Mod("b", dependencies: new[] { new InternalModuleDependency("a", 1) }));
            Register(directory, Mod("c", dependencies: new[] { new InternalModuleDependency("a", 1) }));
            Register(directory, Mod("d", dependencies: new[] { new InternalModuleDependency("b", 1), new InternalModuleDependency("c", 1) }));
            Assert(directory.CompleteRegistration().IsValid, "diamond rejected"); Ready(directory, "a", "b", "c", "d"); State(directory, "d.read", InternalCapabilityState.Available);
        });
        Case("Ready never overrides a rejected existing FeatureBridge", () =>
        {
            string rejection = "bridge.disabled"; var directory = Directory(gate: _ => rejection);
            Register(directory, Mod(capabilities: new[] { Cap("test.read", 1, "conversation-siege") })); directory.CompleteRegistration(); Ready(directory, "test");
            State(directory, "test.read", InternalCapabilityState.BridgeRejected);
            Assert(directory.GetCapabilityStatus("test.read", 1).ReasonCode == "bridge.disabled", "gate reason lost");
            rejection = null; State(directory, "test.read", InternalCapabilityState.Available);
            rejection = "bridge.disabled"; State(directory, "test.read", InternalCapabilityState.BridgeRejected);
        });
        Case("all required bridges must allow", () =>
        {
            var directory = Directory(gate: id => id == "host-runtime" ? null : "bridge.disabled");
            Register(directory, Mod(capabilities: new[] { Cap("test.read", 1, "host-runtime", "conversation-siege") })); directory.CompleteRegistration(); Ready(directory, "test");
            State(directory, "test.read", InternalCapabilityState.BridgeRejected);
        });
        Case("empty and throwing gate rejection fail closed", () =>
        {
            var directory = Directory(gate: _ => ""); Register(directory, Mod(capabilities: new[] { Cap("test.read", 1, "host-runtime") })); directory.CompleteRegistration(); Ready(directory, "test");
            State(directory, "test.read", InternalCapabilityState.BridgeRejected);
            directory = Directory(gate: _ => throw new Exception("private details")); Register(directory, Mod(capabilities: new[] { Cap("test.read", 1, "host-runtime") })); directory.CompleteRegistration(); Ready(directory, "test");
            Assert(directory.GetCapabilityStatus("test.read", 1).ReasonCode == "bridge.evaluation_failed:host-runtime", "exception leaked or granted");
        });
        Case("bridge callbacks run outside lock and changed state is rechecked", () =>
        {
            InternalModuleDirectory directory = null;
            directory = Directory(known: _ => Task.Run(() => directory.GetSnapshot()).Wait(2000), gate: _ =>
            {
                Task<bool> update = Task.Run(() => directory.UpdateRuntimeState("test", InternalModuleRuntimeState.Failed));
                Assert(update.Wait(2000) && update.Result, "gate callback ran under lock or state update rejected");
                return null;
            });
            Register(directory, Mod(capabilities: new[] { Cap("test.read", 1, "host-runtime") })); directory.CompleteRegistration(); Ready(directory, "test");
            State(directory, "test.read", InternalCapabilityState.ModuleFailed);
        });
        Case("queries do not rediscover bridges or inspect unrelated capabilities", () =>
        {
            int discovered = 0; var evaluated = new List<string>();
            var directory = Directory(_ => { discovered++; return true; }, id => { evaluated.Add(id); return null; });
            Register(directory, Mod("one", capabilities: new[] { Cap("one.read", 1, "host-runtime") }));
            Register(directory, Mod("two", capabilities: new[] { Cap("two.read", 1, "conversation-siege") })); directory.CompleteRegistration();
            State(directory, "one.read", InternalCapabilityState.ModuleNotInitialized); Assert(evaluated.Count == 0, "unready capability queried gate");
            Ready(directory, "one", "two"); for (int i = 0; i < 100; i++) { State(directory, "one.read", InternalCapabilityState.Available); }
            Assert(discovered == 2 && evaluated.Count == 100 && evaluated.All(id => id == "host-runtime"), "query rediscovered metadata or scanned unrelated gates");
        });
        Case("module and capability counts are bounded", () =>
        {
            var directory = Directory(); for (int i = 0; i < 64; i++) { Register(directory, Mod("module" + i)); }
            Assert(!directory.TryRegister(Mod("overflow"), out var reason) && reason == "module.catalog_capacity", "module bound missing");
            directory = Directory(); Register(directory, Mod("full", capabilities: Enumerable.Range(0, 256).Select(i => Cap("cap" + i)).ToArray()));
            Assert(!directory.TryRegister(Mod("overflow"), out reason) && reason == "module.catalog_capacity", "capability bound missing");
        });
        Case("concurrent duplicate attempts never overwrite or partially register", () =>
        {
            var directory = Directory(); int accepted = 0;
            Parallel.For(0, 16, i => { if (directory.TryRegister(Mod("same", capabilities: new[] { Cap("same.read") }), out _)) { Interlocked.Increment(ref accepted); } });
            Assert(accepted == 1 && directory.GetSnapshot().Count == 1, "duplicate concurrent provider");
            Assert(directory.CompleteRegistration().IsValid, "accepted provider invalid"); Ready(directory, "same"); State(directory, "same.read", InternalCapabilityState.Available);
        });
        Case("snapshot order is deterministic and unchanged update keeps cache", () =>
        {
            var directory = Directory(); Register(directory, Mod("z")); Register(directory, Mod("a")); directory.CompleteRegistration(); Ready(directory, "z", "a");
            var snapshot = directory.GetSnapshot(); Assert(snapshot[0].Definition.Id == "a" && snapshot[1].Definition.Id == "z", "snapshot unstable");
            Ready(directory, "a"); Assert(ReferenceEquals(snapshot, directory.GetSnapshot()), "unchanged state invalidated cache");
        });
        Console.WriteLine($"InternalModuleDirectory PASS={_passed} FAIL={_failed} LIVE=NOT_RUN");
        return _failed == 0 ? 0 : 1;
    }
}
