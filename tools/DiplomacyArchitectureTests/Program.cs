using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json.Linq;

static class Program
{
    static int checks;
    static HashSet<string> ConcreteOwners = new(StringComparer.Ordinal);
    static bool IsImplementation(string path) => path.StartsWith("src/modules/AF.Module.Diplomacy/")
        || (path.StartsWith("Refactor/") && !path.StartsWith("Refactor/Contracts/") && Path.GetFileName(path).Contains("Diplomacy"))
        || path == "DiplomacyPeaceTermsService.cs" || path == "DiplomacyRecentPeaceGuard.cs";
    static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException("FAIL " + message); checks++; }
    static bool IsPure(string p) =>
        (p.StartsWith("Refactor/Domain/") || p.StartsWith("Refactor/Persistence/") || p.StartsWith("Refactor/Contracts/")) && Path.GetFileName(p).StartsWith("WorldDiplomacy")
        || p.StartsWith("src/modules/AF.Module.Diplomacy/Application/") || p.StartsWith("src/modules/AF.Module.Diplomacy/Rules/")
        || new[] { "WorldDiplomacyDiscussionEligibilityRules.cs", "WorldDiplomacyProactiveSpeakerEligibilityRules.cs", "WorldDiplomacyProactiveDocumentSelectionRules.cs", "Refactor/Contracts/DiplomacyModulePorts.cs", "Refactor/Contracts/AfTributePowerContext.cs", "Refactor/Contracts/DiplomacyPeaceEffectReceipt.cs", "Refactor/Domain/DiplomacyRecentPeaceRules.cs", "Refactor/Contracts/PublishedPolicyArtifactLedgerEntry.cs",
            "src/modules/AF.Module.Diplomacy/World/WorldDiplomacyJobRuntimeCoordinator.cs", "src/modules/AF.Module.Diplomacy/World/WorldDiplomacyRequestLeaseCoordinator.cs" }.Contains(p);
    static readonly HashSet<string> Forbidden = new(StringComparer.Ordinal) {
        "TaleWorlds", "HarmonyLib", "SandBox", "GauntletUI", "MCM", "Hero", "Kingdom", "Clan", "Settlement", "MobileParty", "Agent", "Mission", "Campaign",
        "DuelSettings", "AIConfigHandler", "MyBehavior", "ShoutBehavior", "CourierDeliveryBehavior", "WorldDiplomacyBehavior", "DiplomacyBehavior", "WorldDiplomacyLlmClient",
        "WorldDiplomacyPolicyContext", "DiplomacyModuleServices", "WorldDiplomacyPresentationHost", "WorldMessageTimelineUi", "InformationManager", "Logger",
        "HttpClient", "HttpWebRequest", "WebRequest", "Socket", "TcpClient", "LlmRequestClient", "LlmNonStreamingTransport", "Reflection", "Activator" };
    static List<string> Violations(string path, SyntaxNode root)
    {
        var errors = new List<string>();
        if (path.EndsWith("WorldDiplomacyBehavior.PrestigePort.cs") || path.EndsWith("WorldDiplomacyBehavior.ThreatSettlementPort.cs"))
        {
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Where(m => m.Identifier.ValueText is "CaptureCourt" or "CaptureConsequenceSnapshot"))
            {
                if (method.DescendantNodes().OfType<IfStatementSyntax>().Any(statement =>
                    new[] { "IsUnderMercenaryService", "IsClanTypeMercenary", "clan.IsEliminated", "clan.Kingdom", "vassal == ruler" }
                        .Any(statement.Condition.ToString().Contains))
                    || method.ToString().Contains(".Where("))
                    errors.Add("effect snapshot adapter regained clan qualification policy: " + path);
            }
        }
        if (path.Equals("Refactor/Domain/WorldDiplomacyRoundLifecycleRules.cs", StringComparison.OrdinalIgnoreCase)
            && root.DescendantNodes().OfType<MethodDeclarationSyntax>().Any(m => m.Identifier.ValueText is "RegisterOrAdvanceDiplomaticThreat" or "ProcessDiplomaticThreatDocument" or "RebuildPendingJob" or "CommitAnalysis" or "SuppressInvalidDocumentBeforePropagation"))
            errors.Add("Domain must not own threat effect orchestration");
        if (path.Equals("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                string methodName = method.Identifier.ValueText;
                string eventOwner = methodName switch
                {
                    "OnMapEventEnded" => "WorldDiplomacyBattleApplication.Record",
                    "CaptureNativeDiplomacyDecision" => "WorldDiplomacyNativeDecisionApplication.Capture",
                    "RemoveQueuedNativeDiplomacyDecisions" => "WorldDiplomacyNativeDecisionApplication.Sanitize",
                    "GetRoundHardDurationDays" => "WorldDiplomacyEventRules.RoundHardDurationDays",
                    _ => null
                };
                if (eventOwner != null && (method.Body?.Statements.Count != 1 || !method.ToString().Contains(eventOwner)
                    || method.DescendantNodes().Any(n => n is IfStatementSyntax or ForEachStatementSyntax or TryStatementSyntax)))
                    errors.Add("retained event host regained rules/ordering: " + methodName);
                if (methodName == "IsThreatConsequenceClanEligible") errors.Add("clan qualification still owned by Behavior");
                if (new[] { "EnsureKingdomBorderCache", "GetRealmRelationProfile", "ResolveCourtSettlement" }.Contains(methodName))
                {
                    string text = method.ToString();
                    if (!text.Contains("WorldDiplomacyWorldProfileRules.") || new[] { ".OrderBy", ".Take(", "Math.Sqrt", "weightedSum", "nearestDistances" }.Any(text.Contains))
                        errors.Add("world profile policy still owned by Behavior: " + methodName);
                }
                if (new[] { "OnNewGameCreated", "OnGameLoaded", "OnSessionLaunched" }.Contains(methodName)
                    && (method.Body?.Statements.Count != 1 || !method.ToString().Contains("DiplomacyModuleServices.World.OnLifecycle(")))
                    errors.Add("lifecycle recovery ordering still owned by Behavior: " + methodName);
                string routing = method.Identifier.ValueText switch {
                    "StartDocumentPropagation" => "Start",
                    "ReconcileAnalyzedPlayerDeclarationWithReachedCourts" => "ReconcileReachedCourts",
                    "ExecuteImmediateIntent" => "Execute",
                    "TrySettleRelayOffer" => "Settle",
                    _ => null };
                if (routing != null)
                {
                    var calls = method.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
                    string owner = method.Identifier.ValueText == "ExecuteImmediateIntent"
                        ? "WorldDiplomacyImmediateActionApplication"
                        : method.Identifier.ValueText == "TrySettleRelayOffer" ? "WorldDiplomacyOfferApplication"
                        : "WorldDiplomacyPublicationRoutingApplication";
                    if (method.Body?.Statements.Count != 1 || calls.Length != 1
                        || calls[0].Expression.ToString() != owner + "." + routing)
                        errors.Add("publication predecessor retains orchestration: " + method.Identifier.ValueText);
                }
                if (new[] { "TryApplyUltimatumComplianceDomesticPenalty", "TryApplyDiplomaticThreatPolicyConditionCancellation", "TryApplyDiplomaticThreatIssuerRelationReward", "ResolveDiplomaticThreatCompliance", "ApplyDiplomaticThreatReputationPenalty", "RetryDiplomaticThreatDomesticPenalties", "RetryDiplomaticThreatComplianceConsequences", "RetryDiplomaticThreatHistoryResults" }.Contains(method.Identifier.ValueText))
                {
                    var calls = method.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
                    if (method.Body?.Statements.Count != 1 || calls.Length != 1
                        || !calls[0].Expression.ToString().StartsWith("WorldDiplomacyThreatSettlementApplication.")
                        || method.DescendantNodes().Any(n => n is IfStatementSyntax or ForEachStatementSyntax or AssignmentExpressionSyntax))
                        errors.Add("threat predecessor retains orchestration: " + method.Identifier.ValueText);
                }
                if (new[] { "ApplyNationalPrestigeDelta", "SettleInternationalReputationForDocument", "RecoverUnsettledAiInternationalReputation", "ReconcileAllNationalPrestigeVassalRelations", "ReconcileNationalPrestigeVassalRelations", "ApplyZeroPrestigeBreachRelationPenalty", "AnchorInternationalReputationNaturalChangeDays", "ProcessInternationalReputationNaturalChange" }.Contains(method.Identifier.ValueText))
                {
                    var calls = method.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
                    if (method.Body?.Statements.Count != 1 || calls.Length != 1
                        || !calls[0].Expression.ToString().StartsWith("WorldDiplomacyPrestigeApplication."))
                        errors.Add("prestige predecessor retains orchestration: " + method.Identifier.ValueText);
                }
                if (method.Identifier.ValueText is "OnCampaignTick" or "OnDailyTick" or "TryApplyInitialNewGamePeace")
                {
                    var calls = method.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
                    string expected = method.Identifier.ValueText == "TryApplyInitialNewGamePeace"
                        ? "WorldDiplomacyInitialPeaceApplication.Apply" : "DiplomacyModuleServices.World." + method.Identifier.ValueText;
                    int count = method.Identifier.ValueText == "TryApplyInitialNewGamePeace" ? 2 : 1;
                    if (method.Body?.Statements.Count != count || calls.Length != 1 || calls[0].Expression.ToString() != expected
                        || (count == 2 && method.Body.Statements[0] is not LocalDeclarationStatementSyntax))
                        errors.Add("campaign hook retains orchestration: " + method.Identifier.ValueText);
                }
                if (method.Identifier.ValueText is "EnsureCanonicalHistoryInitialized" or "SyncCanonicalHistorySources" or "CaptureCanonicalHistoryForJob"
                    or "RestoreSuspendedExchangeIfAny" or "CompleteExchange")
                {
                    int count = method.Identifier.ValueText is "SyncCanonicalHistorySources" or "CaptureCanonicalHistoryForJob" ? 2 : 1;
                    string owner = method.Identifier.ValueText is "RestoreSuspendedExchangeIfAny" or "CompleteExchange"
                        ? "WorldDiplomacyRoundApplication." : "WorldDiplomacyHistoryCaptureApplication.";
                    if (method.Body?.Statements.Count != count
                        || !method.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(c => c.Expression.ToString().StartsWith(owner))
                        || method.DescendantNodes().Any(n => n is IfStatementSyntax or ForEachStatementSyntax or AssignmentExpressionSyntax))
                        errors.Add("history/exchange predecessor retains orchestration: " + method.Identifier.ValueText);
                }
                if (method.Identifier.ValueText is "IsNonRootAiRelayNoActionAllowed" or "CanUseResultSettlementTarget"
                    or "TryResolvePolicyConditionForThreat" or "RegisterOrAdvanceDiplomaticThreat" or "ProcessDiplomaticThreatDocument")
                {
                    bool admission = method.Identifier.ValueText is "IsNonRootAiRelayNoActionAllowed" or "CanUseResultSettlementTarget";
                    var calls = method.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
                    if (method.Body?.Statements.Count != (admission ? 2 : 1) || calls.Length != 1
                        || !calls[0].Expression.ToString().StartsWith(admission ? "WorldDiplomacyNoActionApplication." : "WorldDiplomacyThreatBindingApplication.")
                        || method.DescendantNodes().Any(n => n is IfStatementSyntax or ForEachStatementSyntax or AssignmentExpressionSyntax))
                        errors.Add("threat/admission predecessor retains orchestration: " + method.Identifier.ValueText);
                }
                if (method.Identifier.ValueText is "PublishPlayerAuthoredDocumentImmediately" or "RefreshPolicyDiplomacySignals")
                {
                    string expected = method.Identifier.ValueText == "PublishPlayerAuthoredDocumentImmediately"
                        ? "WorldDiplomacyDocumentPublicationApplication.PublishPlayerImmediately" : "WorldDiplomacyPolicyRoundApplication.RefreshSignals";
                    var calls = method.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
                    if (method.Body?.Statements.Count != 1 || calls.Length != 1 || calls[0].Expression.ToString() != expected)
                        errors.Add("publication/policy predecessor retains orchestration: " + method.Identifier.ValueText);
                }
                if (new[] { "BuildPotentialDiplomaticActionIntents", "BuildLegalDiplomaticActionIntents", "BuildLegalDiplomaticDeclarationIntents", "GetActionableDiplomaticTargets", "GetRoundPlanActionableParticipants", "GetResultSettlementActionableTargets", "RefreshResultSettlementActionSlots" }.Contains(method.Identifier.ValueText))
                {
                    bool selection = method.Identifier.ValueText.StartsWith("Get");
                    if (method.Body?.Statements.Count != (selection ? 2 : 1)
                        || method.DescendantNodes().Any(n => n is IfStatementSyntax or ForEachStatementSyntax or AssignmentExpressionSyntax)
                        || !method.ToString().Contains(method.Identifier.ValueText == "RefreshResultSettlementActionSlots" ? "WorldDiplomacyDocumentExecutionApplication.RefreshResultSettlementActionSlots" : "WorldDiplomacyActionSelectionApplication"))
                        errors.Add("action selection predecessor retains orchestration: " + method.Identifier.ValueText);
                }
                if (new[] { "EnsureGenerationJobHasKingdomStrategicProfile", "RefreshDiplomaticActionPresentationAndPrompt", "RefreshDiplomaticThreatPresentationAndPrompt", "TryRebuildPendingWorldDiplomacyJob" }.Contains(method.Identifier.ValueText))
                {
                    var calls = method.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
                    if (method.Body?.Statements.Count != 1 || calls.Length != 1 || !calls[0].Expression.ToString().StartsWith("WorldDiplomacyJobPreparationApplication."))
                        errors.Add("job preparation predecessor retains orchestration: " + method.Identifier.ValueText);
                }
                if (new[] { "CommitAnalysis", "SuppressInvalidDocumentBeforePropagation", "PreservePublishedPlayerDocumentAfterRejectedMechanic" }.Contains(method.Identifier.ValueText))
                {
                    var calls = method.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
                    if (method.Body?.Statements.Count != 1 || calls.Length != 1 || !calls[0].Expression.ToString().StartsWith("WorldDiplomacyAnalysisApplication."))
                        errors.Add("analysis predecessor retains orchestration: " + method.Identifier.ValueText);
                }
                if (new[] { "ParseAndValidatePeaceTerms", "AreOfferedPeaceTermsCurrentlyExecutable", "IsCessionCurrentlyAllowed", "BuildCessionCandidates" }.Contains(method.Identifier.ValueText))
                {
                    if (method.Body?.Statements.Count != (method.Identifier.ValueText == "BuildCessionCandidates" ? 2 : 1)
                        || method.DescendantNodes().Any(n => n is IfStatementSyntax or ForEachStatementSyntax or AssignmentExpressionSyntax)
                        || !method.ToString().Contains("WorldDiplomacyPeaceAdmissionApplication."))
                        errors.Add("peace admission predecessor retains policy: " + method.Identifier.ValueText);
                }
                if (method.Identifier.ValueText is "CanDeclareWar" or "CanIssueWarThreat")
                {
                    var calls = method.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
                    if (method.Body?.Statements.Count != 2 || calls.Length != 1
                        || method.Body.Statements[0] is not LocalDeclarationStatementSyntax
                        || method.Body.Statements[1] is not ReturnStatementSyntax
                        || calls[0].Expression.ToString() != "WorldDiplomacyWarAdmissionApplication." + method.Identifier.ValueText)
                        errors.Add("war predecessor retains admission policy: " + method.Identifier.ValueText);
                }
                if (method.Identifier.ValueText is "HandleDisabledState" or "CommitEmbeddedRoundPlan")
                {
                    if (method.Body?.Statements.Count != 1
                        || method.Body.Statements[0] is not ExpressionStatementSyntax statement
                        || statement.Expression is not InvocationExpressionSyntax call
                        || !call.Expression.ToString().StartsWith("WorldDiplomacyRoundApplication.")
                        || method.DescendantNodes().Any(n => n is IfStatementSyntax or ForEachStatementSyntax or AssignmentExpressionSyntax))
                        errors.Add("round predecessor retains orchestration: " + method.Identifier.ValueText);
                }
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
        if (path.EndsWith("WorldDiplomacyBehavior.OfferActionPort.cs", StringComparison.Ordinal))
        {
            foreach (var assignment in root.DescendantNodes().OfType<AssignmentExpressionSyntax>())
                if (assignment.Left.ToString().Contains("_storage") || assignment.Left.ToString().Contains(".PeaceTerms")
                    || assignment.Left.ToString().Contains(".MechanicalResult") || assignment.Left.ToString().Contains(".ChangedDiplomaticState"))
                    errors.Add("offer effect adapter owns application state: " + assignment.Left);
            foreach (var call in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
                if (call.Expression.ToString().Contains("ClearWarPressure") || call.Expression.ToString().Contains("ClonePeaceTerms"))
                    errors.Add("offer effect adapter owns settlement ordering: " + call.Expression);
        }
        if (path.EndsWith("WorldDiplomacyBehavior.ImmediateActionPort.cs") && root.ToString().Contains("HasTradeAgreement("))
            errors.Add("immediate trade receipt collapsed unreadable state into false");
        if (path.EndsWith("WorldDiplomacyBehavior.PublicationPort.cs") || path.EndsWith("WorldDiplomacyBehavior.OrchestrationHost.cs"))
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
                if (new[] { "CaptureDestinations", "CaptureCourtTargets", "CapturePropagationDistances" }.Contains(method.Identifier.ValueText)
                    && (method.Body?.Statements.Count != 1 || !method.ToString().Contains("WorldDiplomacyGeographyApplication.")))
                    errors.Add("geography projection regained host filtering: " + method.Identifier.ValueText);
        if (path.EndsWith("WorldDiplomacyBehavior.ThreatSettlementPort.cs", StringComparison.Ordinal))
        {
            foreach (var assignment in root.DescendantNodes().OfType<AssignmentExpressionSyntax>())
                if (assignment.Left.ToString().Contains("threat.") || assignment.Left.ToString().Contains("_storage")
                    || assignment.Left.ToString().Contains("document."))
                    errors.Add("threat effect adapter writes canonical settlement: " + assignment.Left);
            foreach (var call in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
                if (call.Expression.ToString().Contains("WorldDiplomacyRoundLifecycleRules")
                    || call.Expression.ToString().Contains("RetryDiplomaticThreat")
                    || call.Expression.ToString().Contains("ResolveDiplomaticThreatCompliance"))
                    errors.Add("threat effect adapter hides settlement orchestration: " + call.Expression);
        }
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
        if (!IsImplementation(path))
            foreach (var name in root.DescendantNodes().OfType<IdentifierNameSyntax>())
                if (ConcreteOwners.Contains(name.Identifier.ValueText)
                    && !(path == "src/bridges/Diplomacy/DiplomacyModuleServices.cs" && name.Identifier.ValueText == "DiplomacyModule")
                    && !(path == "src/bridges/Diplomacy/DiplomacyRecentPeaceBridge.cs" && name.Identifier.ValueText == "DiplomacyFactionSnapshot"))
                    errors.Add("external module implementation dependency: " + name);
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
            ConcreteOwners = texts.Where(pair => pair.Key.StartsWith("src/modules/AF.Module.Diplomacy/"))
                .SelectMany(pair => CSharpSyntaxTree.ParseText(pair.Value, options).GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
                .Where(type => !type.Ancestors().OfType<BaseTypeDeclarationSyntax>().Any())
                .Select(type => type.Identifier.ValueText).ToHashSet(StringComparer.Ordinal);
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
        Check(retiredMethods.Length == 29 && retiredMethods.All(m => m.Modifiers.Any(SyntaxKind.PrivateKeyword) && m.AttributeLists.Count == 0), "only private non-callback declarations retired");
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
        Check(hostRoot.DescendantNodes().OfType<FieldDeclarationSyntax>().All(f => f.Declaration.Type.ToString() != "WorldDiplomacyStorage"),
            "the host must not own a canonical storage field");
        Check(hostRoot.DescendantNodes().OfType<FieldDeclarationSyntax>().All(f => f.Declaration.Type.ToString() != "WorldDiplomacyStateStore"),
            "the host must not own the canonical state store itself");
        Check(trees.ContainsKey("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyStateStore.cs")
            && trees["src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyStateStore.cs"].GetRoot()
                .DescendantNodes().OfType<FieldDeclarationSyntax>()
                .Count(f => f.Declaration.Type.ToString() == "WorldDiplomacyStorage") == 1,
            "the application state store must own the single canonical storage field");
        foreach (string path in texts.Keys.Where(p => p.Contains("Diplomacy")))
            if (path != "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyStateStore.cs")
                Check(!trees[path].GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>().Any(f => f.Declaration.Type.ToString() == "WorldDiplomacyStorage"), "second diplomacy state owner " + path);
        Check(texts.Where(x => x.Value.Contains("_af_world_diplomacy_v1")).Select(x => x.Key).SequenceEqual(new[] { "Refactor/Adapters/BannerlordWorldDiplomacyPersistenceAdapter.cs" }), "canonical save key remains adapter-owned");
        var mutations = new[] {
            ("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.ThreatSettlementPort.cs", "class X { void F(dynamic threat) { threat.DomesticPenaltyCompleted = true; } }"),
            ("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.ThreatSettlementPort.cs", "class X { void F() { _owner.RetryDiplomaticThreatHistoryResults(); } }"),
            ("Refactor/Domain/WorldDiplomacyInjectedRules.cs", "class X { object F() => TaleWorlds.CampaignSystem.Hero.MainHero; }"),
            ("Refactor/Contracts/WorldDiplomacyInjected.cs", "using H = TaleWorlds.CampaignSystem.Hero; class X { H H; }"),
            ("src/modules/AF.Module.Diplomacy/Application/Injected.cs", "class X { object F() => new System.Net.Http.HttpClient(); }"),
            ("src/bridges/Diplomacy/Injected.cs", "class X { int state; void F() { if (state > 0) state--; } }"),
            ("src/bridges/Diplomacy/Injected.cs", "class X { string F() => \"[ACTION:DIPLOMACY:DECLARE_WAR]\"; }"),
            ("AIConfigHandler.cs", "class X { object F() => AnimusForge.WorldDiplomacyBehavior.Instance; }") };
        foreach (var (path, text) in mutations)
            Check(Violations(path, CSharpSyntaxTree.ParseText(text).GetRoot()).Count > 0, "dependency mutation must be rejected " + path);
        foreach (string method in new[] { "OnMapEventEnded", "CaptureNativeDiplomacyDecision", "RemoveQueuedNativeDiplomacyDecisions", "GetRoundHardDurationDays" })
            Check(Violations(host, CSharpSyntaxTree.ParseText("class X { void " + method + "() { if (true) OldOrdering(); } }").GetRoot()).Count > 0,
                "event host rule mutation rejected: " + method);
        foreach (string owner in new[] { "DiplomacyCrossDomainActionOwner", "WorldDiplomacyPresentation", "WorldDiplomacyComposePopup", "WorldDiplomacyOrchestration", "DiplomacyPoliticalRewardApplication" })
            Check(Violations("RewardSystemBehavior.cs", CSharpSyntaxTree.ParseText("using Alias = AnimusForge." + owner + "; class X { object F() => typeof(Alias); }").GetRoot()).Count > 0,
                "all concrete module owners reject external alias/typeof bypass: " + owner);
        foreach (string name in new[] { "ProcessAnalyzedDocument", "ProcessAnalyzedMultiActionDocument", "TrySettleRelayOffer", "CanDeclareWar", "CanIssueWarThreat", "HandleDisabledState", "CommitEmbeddedRoundPlan", "TryApplyUltimatumComplianceDomesticPenalty", "TryApplyDiplomaticThreatPolicyConditionCancellation", "TryApplyDiplomaticThreatIssuerRelationReward", "ResolveDiplomaticThreatCompliance", "ApplyDiplomaticThreatReputationPenalty", "RetryDiplomaticThreatDomesticPenalties", "RetryDiplomaticThreatComplianceConsequences", "RetryDiplomaticThreatHistoryResults", "ApplyNationalPrestigeDelta", "SettleInternationalReputationForDocument", "RecoverUnsettledAiInternationalReputation", "ReconcileAllNationalPrestigeVassalRelations", "ReconcileNationalPrestigeVassalRelations", "ApplyZeroPrestigeBreachRelationPenalty", "AnchorInternationalReputationNaturalChangeDays", "ProcessInternationalReputationNaturalChange", "OnDailyTick", "OnCampaignTick", "TryApplyInitialNewGamePeace", "EnsureCanonicalHistoryInitialized", "SyncCanonicalHistorySources", "CaptureCanonicalHistoryForJob", "RestoreSuspendedExchangeIfAny", "CompleteExchange", "IsNonRootAiRelayNoActionAllowed", "CanUseResultSettlementTarget", "TryResolvePolicyConditionForThreat", "RegisterOrAdvanceDiplomaticThreat", "ProcessDiplomaticThreatDocument", "PublishPlayerAuthoredDocumentImmediately", "RefreshPolicyDiplomacySignals", "BuildPotentialDiplomaticActionIntents", "BuildLegalDiplomaticActionIntents", "BuildLegalDiplomaticDeclarationIntents", "GetActionableDiplomaticTargets", "GetRoundPlanActionableParticipants", "GetResultSettlementActionableTargets", "RefreshResultSettlementActionSlots", "EnsureGenerationJobHasKingdomStrategicProfile", "RefreshDiplomaticActionPresentationAndPrompt", "RefreshDiplomaticThreatPresentationAndPrompt", "TryRebuildPendingWorldDiplomacyJob", "CommitAnalysis", "SuppressInvalidDocumentBeforePropagation", "PreservePublishedPlayerDocumentAfterRejectedMechanic", "ParseAndValidatePeaceTerms", "AreOfferedPeaceTermsCurrentlyExecutable", "IsCessionCurrentlyAllowed", "BuildCessionCandidates" })
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
        Console.WriteLine($"PASS {checks} architecture/caller assertions, 8 rejected dependency mutations; LIVE/SAVE not tested");
    }
}
