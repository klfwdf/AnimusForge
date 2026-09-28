using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json.Linq;

static class Program
{
    static int checks;
    static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException("FAIL " + message); checks++; }
    static bool IsPure(string p) =>
        (p.StartsWith("Refactor/Domain/") || p.StartsWith("Refactor/Persistence/") || p.StartsWith("Refactor/Contracts/")) && Path.GetFileName(p).StartsWith("WorldDiplomacy")
        || p.StartsWith("src/modules/AF.Module.Diplomacy/Application/") || p.StartsWith("src/modules/AF.Module.Diplomacy/Rules/")
        || new[] { "WorldDiplomacyDiscussionEligibilityRules.cs", "WorldDiplomacyProactiveSpeakerEligibilityRules.cs", "WorldDiplomacyProactiveDocumentSelectionRules.cs", "Refactor/Contracts/DiplomacyModulePorts.cs", "Refactor/Contracts/AfTributePowerContext.cs", "Refactor/Contracts/PublishedPolicyArtifactLedgerEntry.cs",
            "src/modules/AF.Module.Diplomacy/World/WorldDiplomacyJobRuntimeCoordinator.cs", "src/modules/AF.Module.Diplomacy/World/WorldDiplomacyRequestLeaseCoordinator.cs" }.Contains(p);
    static readonly HashSet<string> Forbidden = new(StringComparer.Ordinal) {
        "TaleWorlds", "HarmonyLib", "SandBox", "GauntletUI", "MCM", "Hero", "Kingdom", "Clan", "Settlement", "MobileParty", "Agent", "Mission", "Campaign",
        "DuelSettings", "AIConfigHandler", "MyBehavior", "ShoutBehavior", "CourierDeliveryBehavior", "WorldDiplomacyBehavior", "DiplomacyBehavior", "WorldDiplomacyLlmClient",
        "WorldDiplomacyPolicyContext", "DiplomacyModuleServices", "WorldDiplomacyPresentationHost", "WorldMessageTimelineUi", "InformationManager", "Logger",
        "HttpClient", "HttpWebRequest", "WebRequest", "Socket", "TcpClient", "LlmRequestClient", "LlmNonStreamingTransport", "Reflection", "Activator" };
    static List<string> Violations(string path, SyntaxNode root)
    {
        var errors = new List<string>();
        if (path.StartsWith("src/modules/AF.Module.Diplomacy/World/"))
        {
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                if (!new[] { "ProcessAnalyzedDocument", "ProcessAnalyzedMultiActionDocument",
                    "FinalizePublishedDocumentAfterAnalysis", "TryIncludeResultSettlementTarget" }.Contains(method.Identifier.ValueText)) continue;
                var invocations = method.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
                if (method.Body?.Statements.Count != 1 || invocations.Length != 1
                    || invocations[0].Expression.ToString() != "WorldDiplomacyDocumentExecutionApplication." + method.Identifier.ValueText)
                    errors.Add("document predecessor retains orchestration: " + method.Identifier.ValueText);
            }
        }
        if (IsPure(path))
            foreach (SyntaxToken token in root.DescendantTokens().Where(t => t.IsKind(SyntaxKind.IdentifierToken)))
                if (Forbidden.Contains(token.ValueText)) errors.Add("pure dependency " + token.ValueText);
        if (path.StartsWith("src/bridges/Diplomacy/"))
        {
            foreach (var node in root.DescendantNodes())
            {
                if (node is FieldDeclarationSyntax || node is IfStatementSyntax || node is SwitchStatementSyntax
                    || node is ForStatementSyntax || node is ForEachStatementSyntax || node is WhileStatementSyntax
                    || node is TryStatementSyntax || node is LockStatementSyntax || node is AwaitExpressionSyntax || node is ConditionalExpressionSyntax)
                    errors.Add("bridge owns state/control flow: " + node.Kind());
                if (node is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
                    errors.Add("bridge owns protocol/policy literal");
            }
            foreach (var name in root.DescendantNodes().OfType<IdentifierNameSyntax>())
                if (new[] { "Regex", "Random", "Campaign", "WorldDiplomacyStorage", "WorldDiplomacyBehavior", "DiplomacyBehavior" }.Contains(name.Identifier.ValueText))
                    errors.Add("bridge owns implementation: " + name);
        }
        // Legacy concrete owners are accessible only inside their module. Ignore diagnostics/comments/strings.
        if (!path.StartsWith("src/modules/AF.Module.Diplomacy/"))
            foreach (var name in root.DescendantNodes().OfType<IdentifierNameSyntax>())
                if (name.Identifier.ValueText is "WorldDiplomacyBehavior" or "DiplomacyBehavior")
                    errors.Add("external concrete owner dependency: " + name);
        return errors;
    }
    static void Main(string[] args)
    {
        try { Run(args[0]); }
        catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
    }
    static void Run(string manifestPath)
    {
        JObject manifest = JObject.Parse(File.ReadAllText(manifestPath));
        string root = (string)manifest["root"];
        var paths = manifest["paths"].Values<string>().ToArray();
        Check(paths.Distinct(StringComparer.OrdinalIgnoreCase).Count() == paths.Length, "no duplicate Compile members");
        var texts = paths.ToDictionary(p => Path.GetRelativePath(root, p).Replace('\\', '/'), File.ReadAllText);
        var pure = new List<SyntaxTree>();
        var trees = new Dictionary<string, SyntaxTree>();
        foreach (bool api14 in new[] { false, true })
        {
            var options = new CSharpParseOptions(LanguageVersion.Latest, preprocessorSymbols: api14 ? new[] { "BANNERLORD_1_4_OR_GREATER" } : Array.Empty<string>());
            foreach (var (path, text) in texts)
            {
                var tree = CSharpSyntaxTree.ParseText(text, options, path);
                var violations = Violations(path, tree.GetRoot());
                Check(violations.Count == 0, path + ": " + string.Join("; ", violations.Distinct()));
                if (!api14) { trees[path] = tree; if (IsPure(path)) pure.Add(tree); }
            }
        }
        // Compile the complete diplomacy pure layer with framework + JSON references only, no game or host stubs.
        foreach (string p in new[] { "Refactor/Contracts/LlmContracts.cs", "Refactor/Contracts/InteractionContracts.cs" })
            pure.Add(trees[p]);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)
            .Select(p => MetadataReference.CreateFromFile(p)).ToList();
        references.Add(MetadataReference.CreateFromFile(typeof(JObject).Assembly.Location));
        var compilation = CSharpCompilation.Create("DiplomacyPureBoundary", pure, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Check(errors.Length == 0, "pure layer fails independent compilation: " + string.Join("\n", errors.Select(e => e.ToString())));
        Console.WriteLine($"PASS {pure.Count} real pure sources compile without game/host stubs; both preprocessor variants checked across {paths.Length} production files");

        string host = "src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs";
        var retired = manifest["retired"].Values<string>().ToHashSet();
        var prior = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(Path.GetDirectoryName(manifestPath), "prior-host.cs.txt"))).GetRoot();
        var retiredMethods = prior.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => retired.Contains(m.Identifier.ValueText)).ToArray();
        Check(retiredMethods.Length == 17 && retiredMethods.All(m => m.Modifiers.Any(SyntaxKind.PrivateKeyword) && m.AttributeLists.Count == 0), "only private non-callback declarations retired");
        Check(!trees[host].GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Any(m => retired.Contains(m.Identifier.ValueText)), "retired host wrappers absent");
        // All surviving same-name references must be qualified calls to the extracted owner, never a bare/self/owner dispatch.
        foreach (var (path, tree) in trees)
        foreach (var name in tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => retired.Contains(n.Identifier.ValueText)))
        {
            if (!path.StartsWith("src/modules/AF.Module.Diplomacy/World/")) continue;
            Check(name.Parent is MemberAccessExpressionSyntax access && access.Name == name
                && access.Expression.ToString().StartsWith("WorldDiplomacy") && !access.Expression.ToString().StartsWith("WorldDiplomacyBehavior"),
                "surviving reference to retired host member: " + path + " " + name.Parent);
        }
        Check(!trees.Values.SelectMany(t => t.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>())
            .Any(l => l.IsKind(SyntaxKind.StringLiteralExpression) && retired.Contains(l.Token.ValueText)), "no reflection string references retired members");
        Check(!texts.Values.Any(t => t.Contains("WorldDiplomacyJobQueueItem") || t.Contains("SelectNextJobId(")), "retired queue DTO/selector absent from production");
        var hostRoot = trees[host].GetRoot();
        Check(hostRoot.DescendantNodes().OfType<FieldDeclarationSyntax>().Count(f => f.Declaration.Type.ToString() == "WorldDiplomacyStorage") == 1, "one canonical storage field");
        foreach (string path in texts.Keys.Where(p => p.Contains("Diplomacy")))
            if (path != host)
                Check(!trees[path].GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>().Any(f => f.Declaration.Type.ToString() == "WorldDiplomacyStorage"), "second diplomacy state owner " + path);
        Check(texts.Where(x => x.Value.Contains("_af_world_diplomacy_v1")).Select(x => x.Key).SequenceEqual(new[] { "Refactor/Adapters/BannerlordWorldDiplomacyPersistenceAdapter.cs" }), "canonical save key remains adapter-owned");
        var mutations = new[] {
            ("Refactor/Domain/WorldDiplomacyInjectedRules.cs", "class X { object F() => TaleWorlds.CampaignSystem.Hero.MainHero; }"),
            ("Refactor/Contracts/WorldDiplomacyInjected.cs", "using H = TaleWorlds.CampaignSystem.Hero; class X { H H; }"),
            ("src/modules/AF.Module.Diplomacy/Application/Injected.cs", "class X { object F() => new System.Net.Http.HttpClient(); }"),
            ("src/bridges/Diplomacy/Injected.cs", "class X { int state; void F() { if (state > 0) state--; } }"),
            ("src/bridges/Diplomacy/Injected.cs", "class X { string F() => \"[ACTION:DIPLOMACY:DECLARE_WAR]\"; }"),
            ("AIConfigHandler.cs", "class X { object F() => AnimusForge.WorldDiplomacyBehavior.Instance; }") };
        foreach (var (path, text) in mutations)
            Check(Violations(path, CSharpSyntaxTree.ParseText(text).GetRoot()).Count > 0, "dependency mutation must be rejected " + path);
        foreach (string name in new[] { "ProcessAnalyzedDocument", "ProcessAnalyzedMultiActionDocument" })
        {
            string injected = "class X { void " + name + "(object d) { if (d != null) LegacyExecute(d); } }";
            Check(Violations(host, CSharpSyntaxTree.ParseText(injected).GetRoot()).Count > 0, "reject callback-hidden predecessor orchestration: " + name);
        }
        Check(Violations("Refactor/Domain/WorldDiplomacyX.cs", CSharpSyntaxTree.ParseText("// Hero WorldDiplomacyBehavior\nclass X { string F() => \"TaleWorlds\"; }").GetRoot()).Count == 0, "comments/strings are not type references");
        string baseline = (string)manifest["baseline"], candidate = (string)manifest["candidate"];
        if (!string.IsNullOrEmpty(baseline) && !string.IsNullOrEmpty(candidate))
        {
            string[] Surface(string file)
            {
                var reference = MetadataReference.CreateFromFile(Path.GetFullPath(file));
                var c = CSharpCompilation.Create("MetadataAudit", references: references.Append(reference));
                var assembly = (IAssemblySymbol)c.GetAssemblyOrModuleSymbol(reference);
                var lines = new List<string>();
                void Visit(INamespaceSymbol ns)
                {
                    foreach (var child in ns.GetNamespaceMembers()) Visit(child);
                    foreach (var type in ns.GetTypeMembers())
                    {
                        if (type.DeclaredAccessibility != Accessibility.Public || !type.Name.Contains("Diplomacy")) continue;
                        lines.Add(type.ToDisplayString());
                        foreach (var member in type.GetMembers().Where(m => m.DeclaredAccessibility == Accessibility.Public))
                            lines.Add(type.ToDisplayString() + ":" + member.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                    }
                }
                Visit(assembly.GlobalNamespace);return lines.Order().ToArray();
            }
            var before = Surface(baseline);var after = Surface(candidate);
            Check(before.Length > 100 && before.SequenceEqual(after), "public diplomacy ABI differs: " + string.Join("; ", before.Except(after)));
            Console.WriteLine($"PASS {before.Length} public diplomacy type/member signatures preserved against DPL-100 DLL");
        }
        Console.WriteLine($"PASS {checks} architecture/caller assertions, 6 rejected dependency mutations; LIVE/SAVE not tested");
    }
}
