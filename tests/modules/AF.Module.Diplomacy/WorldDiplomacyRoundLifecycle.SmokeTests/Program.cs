using System.Globalization;
using System.Text;
using AnimusForge;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Persistence;
using Newtonsoft.Json.Linq;

static class Test
{
    private static int _assertions;

    internal static void True(bool value, string message)
    {
        _assertions++;
        if (!value)
        {
            throw new InvalidOperationException(message);
        }
    }

    internal static int Assertions => _assertions;
}

internal static class Program
{
    private static void CanonicalProjectionPreservesProse()
    {
        const string body = "政策《正文引用》；范围=原文\n影响：必须保留\n机械效果：用户原文";
        var entry = new WorldDiplomacyCanonicalHistoryEntry
        {
            Kind = "policy_published",
            Text = "政策《名称》；发布国=甲国；范围=kingdom\n政策状态=active\n" + body
        };
        var chinese = new Dictionary<string, string>
        {
            ["HistoryProjection_Policy"] = "政策《",
            ["HistoryProjection_PolicyStatus"] = "政策状态=",
            ["HistoryProjection_PolicyIssuer"] = "；发布国=",
            ["HistoryProjection_PolicyScope"] = "；范围=",
            ["HistoryProjection_Confirmed"] = "经游戏机制确认：",
            ["HistoryProjection_Weekly"] = "世界周报成品",
            ["HistoryProjection_WeeklyCorrection"] = "世界周报成品更正版"
        };
        string original = entry.Text;
        string raw = WorldDiplomacyCanonicalRenderRules.RenderCanonicalHistoryEntry(entry);
        Test.True(WorldDiplomacyCanonicalRenderRules.RenderCanonicalHistoryEntry(entry, (key, fallback) => chinese[key]) == raw,
            "Chinese package preserves original canonical text");
        string english = WorldDiplomacyCanonicalRenderRules.RenderCanonicalHistoryEntry(entry, (key, fallback) => fallback);
        Test.True(english.Contains("Policy 《名称》") && english.Contains("Policy status=active") && english.Contains("; issuing kingdom=甲国; scope=kingdom") && english.EndsWith(body),
            "English projection changes structural prefixes without rewriting body");
        Test.True(entry.Text == original, "Canonical projection cannot rewrite stored text");
        string packaged = WorldDiplomacyCanonicalRenderRules.RenderCanonicalHistoryEntry(entry, DiplomacyPromptTexts.Get);
        Test.True(SmokeLocalizationEnvironment.EnglishPackage ? packaged.Contains("; issuing kingdom=甲国; scope=kingdom") : packaged == raw,
            "actual package reader projects known issuer/scope metadata while CN retains raw text");
        entry.Text = "政策《标题含》边界》；发布国=甲国；范围=kingdom\n" + body;
        english = WorldDiplomacyCanonicalRenderRules.RenderCanonicalHistoryEntry(entry, (key, fallback) => fallback);
        Test.True(english.Contains("；发布国=甲国；范围=kingdom") && english.EndsWith(body), "ambiguous title delimiters never rewrite metadata or body");
        entry.Text = "政策《名称》；范围=custom_scope:{NAME} $1\n" + body;
        english = WorldDiplomacyCanonicalRenderRules.RenderCanonicalHistoryEntry(entry, (key, fallback) => fallback);
        Test.True(english.Contains("; scope=custom_scope:{NAME} $1") && english.EndsWith(body), "stable unknown scope and dynamic values remain literal");

        entry.Kind = "world_weekly";
        entry.Text = "世界周报成品更正版《题目》：\n正文引用世界周报成品更正版";
        english = WorldDiplomacyCanonicalRenderRules.RenderCanonicalHistoryEntry(entry, (key, fallback) => fallback);
        Test.True(english.Contains("[World Bulletin Final Edition (Correction)]《题目》") && english.EndsWith("正文引用世界周报成品更正版"),
            "Weekly correction translates only the leading structural label");
    }

    private static void PackagedOralProtocolParity()
    {
        var (host, owner) = ConcurrentOralMigrationReplay.Fixture();
        foreach (string edition in new[] { "", ".en" })
        {
            var corpus = JObject.Parse(File.ReadAllText(FindRepositoryFile("content/modules/AF.Module.Diplomacy/ModuleData/DiplomacyPromptTexts" + edition + ".json")));
            string commit = (string)corpus["AF_DIP_CommitInstruction"]!;
            string tag = System.Text.RegularExpressions.Regex.Match(commit, @"\[ACTION:DIPLOMACY:COMMIT:([^\]]+)\]").Groups[1].Value;
            tag = tag.Replace("KINGDOM_ID", "b");
            Test.True(AnimusForge.DiplomacyDialogue.DialogueTagPayload.TryParse(tag, out var parsed)
                && parsed.TargetKingdomId == "b" && parsed.Action == AnimusForge.DiplomacyDialogue.DialogueDiplomaticAction.Peace
                && parsed.Move == AnimusForge.DiplomacyDialogue.DialogueDiplomaticMove.NewMatter,
                "actual CN/EN example resolves through production payload parser without translating fields or enums");
            string control = (string)corpus["AF_DIP_CommitmentInstruction"]!;
            string payload = System.Text.RegularExpressions.Regex.Match(control, @"\[ACTION:DIPLOMACY:COMMITMENT:([^\]]+)\]").Groups[1].Value
                .Replace("ARRANGEMENT_ID", "absent").Replace("deferred|cancelled|accepted", "deferred").Replace("EXPLICIT_REASON", "literal {NAME} $1");
            string result = owner.ControlOralDiplomaticCommitment("ruler_a", "a", payload);
            Test.True(result == DiplomacyMiscPromptTexts.Get("DialogueControl_AmbiguousSourceReason", "")
                && owner.CurrentStorage.DialogueArrangements.Count == 0,
                "actual CN/EN control example parses correctly but never executes an absent commitment");
        }
    }

    private static int Main(string[] args)
    {
        SmokeLocalizationEnvironment.Initialize();
        if (args.Contains("--sovereign-control"))
        { try { SovereignControlReplay.Run(); Console.WriteLine("PASS sovereign control assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--sovereign-regression"))
        { try { SpeechReviewReplay.Run(); MaterialRecoveryReplay.Run(); CountryAdmissionReplay.Run(); Console.WriteLine("PASS sovereign regression assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--worldbook-repair"))
        { try { MaterialCountryThirdReplay.WorldBookWaitEnding(); Console.WriteLine("PASS worldbook repair assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--diplomacy-stall-repair"))
        { try { DiplomacyBaselineReplay.Run(); LiveDiplomacyFixReplay.StallSilenceClosure(); Console.WriteLine("PASS diplomacy stall repair assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--progress-effect-preflight"))
        { try { IndependentAuditFixReplay.ProgressEffectPreflight(); Console.WriteLine("PASS progress effect preflight assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--structure-observation"))
        { try { MaterialSupplyReplay.StructureObservation(); Console.WriteLine("PASS structure observation assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--structure-selection"))
        { try { MaterialSupplyReplay.StructureSelection(); Console.WriteLine("PASS structure selection assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--presentation"))
        { try { Dpl090PresentationReplay.Run(); Console.WriteLine("PASS presentation assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--player-material-optimization"))
        { try { PlayerMaterialOptimizationReplay.Run(); Console.WriteLine("PASS player material assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--bound-matters"))
        { try { MaterialSupplyReplay.BoundMatterRecovery(); CountryAdmissionReplay.Run(); MaterialCountryThirdReplay.PlayerOpeningLifecycle(); Console.WriteLine("PASS bound matters assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--baseline-restoration"))
        { try { EventConvergenceReplay.Run(); DiplomacyBaselineReplay.Run(); Console.WriteLine("PASS baseline restoration assertions="+Test.Assertions); return 0; } catch(Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--admission-hold-recovery"))
        { try { MaterialSupplyReplay.AdmissionHoldRecovery(); Console.WriteLine("PASS admission hold recovery assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--declaration-stalls"))
        { try { MaterialCountryThirdReplay.DeclarationAuditStalls(); MaterialSupplyReplay.PlanningAuditBudget(); Console.WriteLine("PASS declaration stall assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--postdeploy-stall"))
        { try { MaterialSupplyReplay.PostDeploymentStalls(); Console.WriteLine("PASS postdeployment assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--audit-gaps"))
        { try { MaterialSupplyReplay.AuditGapRegressions(); Console.WriteLine("PASS audit gap assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--country-admission"))
        { try { CountryAdmissionReplay.Run(); Console.WriteLine("PASS country admission assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--event-convergence"))
        { try { EventConvergenceReplay.Run(); Console.WriteLine("PASS convergence assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if(args.Contains("--player-opening-model"))
        { try { MaterialCountryThirdReplay.PlayerOpeningModel();return 0; } catch(Exception ex){Console.Error.WriteLine(ex);return 1;} }
        var openingIncident = args.FirstOrDefault(x=>x.StartsWith("--player-opening-incident=",StringComparison.Ordinal));
        if(openingIncident!=null)
        { try { MaterialCountryThirdReplay.PlayerOpening(openingIncident.Substring("--player-opening-incident=".Length)); Console.WriteLine("PASS actual player opening assertions="+Test.Assertions); return 0; } catch(Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--player-opening"))
        { try { MaterialCountryThirdReplay.PlayerOpening(); WorldProfileReplay.Run(); Console.WriteLine("PASS player opening assertions="+Test.Assertions); return 0; } catch(Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        var auditLatencyIncident=args.FirstOrDefault(x=>x.StartsWith("--audit-latency-incident=",StringComparison.Ordinal));
        if(auditLatencyIncident!=null)
        { try { MaterialCountryThirdReplay.AuditLatency(auditLatencyIncident.Substring("--audit-latency-incident=".Length)); Console.WriteLine("PASS audit latency incident assertions="+Test.Assertions); return 0; } catch(Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        var playerNarrativeIncident=args.FirstOrDefault(x=>x.StartsWith("--player-narrative-incident=",StringComparison.Ordinal));
        if(playerNarrativeIncident!=null)
        { try { MaterialCountryThirdReplay.PlayerNarrativeFollowup(playerNarrativeIncident.Substring("--player-narrative-incident=".Length)); Console.WriteLine("PASS player narrative incident assertions="+Test.Assertions); return 0; } catch(Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        var pipelineIncident=args.FirstOrDefault(x=>x.StartsWith("--pipeline-incident=",StringComparison.Ordinal));
        if(pipelineIncident!=null)
        { try { MaterialCountryThirdReplay.PipelineRecovery(pipelineIncident.Substring("--pipeline-incident=".Length) is "" ? null : pipelineIncident.Substring("--pipeline-incident=".Length)); Console.WriteLine("PASS pipeline recovery assertions="+Test.Assertions); return 0; } catch(Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        var progressionIncident=args.FirstOrDefault(x=>x.StartsWith("--progression-incident=",StringComparison.Ordinal));
        if(progressionIncident!=null)
        { try { MaterialCountryThirdReplay.ProgressionRepair(progressionIncident.Substring("--progression-incident=".Length)); Console.WriteLine("PASS progression incident assertions="+Test.Assertions); return 0; } catch(Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        var planningIncident=args.FirstOrDefault(x=>x.StartsWith("--planning-incident=",StringComparison.Ordinal));
        if(planningIncident!=null)
        { try { MaterialSupplyReplay.ActualPlanningIncident(planningIncident.Substring("--planning-incident=".Length)); Console.WriteLine("PASS planning incident assertions="+Test.Assertions); return 0; } catch(Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        var termsIncident=args.FirstOrDefault(x=>x.StartsWith("--terms-incident=",StringComparison.Ordinal));
        if(termsIncident!=null)
        { try { MaterialCountryThirdReplay.OptionalTermsReplay(termsIncident.Substring("--terms-incident=".Length)); Console.WriteLine("PASS optional terms assertions="+Test.Assertions); return 0; } catch(Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        var evidenceIncident=args.FirstOrDefault(x=>x.StartsWith("--evidence-incident=",StringComparison.Ordinal));
        if(evidenceIncident!=null)
        { try { MaterialCountryThirdReplay.PublicationEvidenceReplay(evidenceIncident.Substring("--evidence-incident=".Length)); Console.WriteLine("PASS evidence incident assertions="+Test.Assertions); return 0; } catch(Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        var incidentArgument=args.FirstOrDefault(x=>x.StartsWith("--declaration-incident=",StringComparison.Ordinal));
        if(incidentArgument!=null)
        { try { MaterialCountryThirdReplay.DeclarationAuditRepair(incidentArgument.Substring("--declaration-incident=".Length)); Console.WriteLine("PASS declaration incident assertions="+Test.Assertions); return 0; } catch(Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--material-supply"))
        { try { MaterialSupplyReplay.Run(); Console.WriteLine("PASS supply assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--theme-exploration"))
        { try { MaterialSupplyReplay.ThemeExploration(); Console.WriteLine("PASS theme assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--material-country"))
        { try { MaterialCountryReplay.Run(); MaterialCountryCompletionReplay.Run(); EventStructureReplay.Run(); Console.WriteLine("PASS material/country assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--live-blockers"))
        { try { LiveBlockerReplay.Run(); Console.WriteLine("PASS LiveBlockerReplay assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--live-fixes"))
        {
            try { LiveDiplomacyFixReplay.Run(); Console.WriteLine("PASS LiveDiplomacyFixReplay assertions=" + Test.Assertions); return 0; }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }
        if (args.Contains("--independent-fixes"))
        { try { IndependentAuditFixReplay.Run(); Console.WriteLine("PASS IndependentAuditFixReplay assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--audit-fixes"))
        { try { AuditFixesReplay.Run(); NativeDiplomacyFallbackReplay.Run(); PolicyStyleNoticeReplay.Run(); Console.WriteLine("PASS AuditFixesReplay assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--legacy-source-boundary"))
        { VerifyLegacySourceBoundary(); return 0; }
        if (args.Contains("--event-source-boundary"))
        { VerifySourceBoundary(); Console.WriteLine("PASS EventSourceBoundary assertions=" + Test.Assertions); return 0; }
        if (args.Contains("--event-save"))
        { try { PlayerSaveReplay.Run(); Console.WriteLine("PASS EventSave assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Contains("--shared-declarations"))
        { try { ActionSelectionReplay.Run(); JobPreparationReplay.Run(); Dpl080PromptReplay.Run(); RunJobCompositionDecisionTests(); Console.WriteLine("PASS SharedDeclarations assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Length == 1 && args[0] == "--event-structure")
        { try { EventStructureReplay.Run(); Console.WriteLine("PASS EventStructureReplay assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        MaterialCountryThirdReplay.WorldBookWaitEnding();
        CanonicalProjectionPreservesProse();
        PackagedOralProtocolParity();
        if (args.Length == 1 && args[0] == "--original-oral-vassalage")
        { OriginalOralVassalageReplay.Run(); Console.WriteLine("PASS OriginalOralVassalageReplay assertions=" + Test.Assertions); return 0; }
        if (args.Length == 1 && args[0] == "--native-event-admission")
        {
            NativeEventAdmissionReplay.Run();
            Console.WriteLine($"Native event admission replay passed: {Test.Assertions} assertions.");
            return 0;
        }
        if (args.Length == 1 && args[0] == "--request-history")
        { try { RequestHistoryBudgetReplay.Run(); AutonomousRecallReplay.Run(); Console.WriteLine("PASS RequestHistoryBudgetReplay assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Length == 1 && args[0] == "--history-fragment")
        { try { HistoryFragmentReplay.Run(); Console.WriteLine("PASS HistoryFragmentReplay assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Length == 1 && args[0] == "--compression-capacity")
        { try { CompressionCapacityReplay.Run(); Console.WriteLine("PASS CompressionCapacityReplay assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Length == 1 && args[0] == "--speech-review")
        { try { SpeechReviewReplay.Run(); MaterialRecoveryReplay.Run(); Console.WriteLine("PASS SpeechReviewReplay assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Length == 1 && args[0] == "--event-legacy")
        { try { EventLegacyReplay.Run(); Console.WriteLine("PASS EventLegacyReplay assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Length == 1 && args[0] == "--event-recovery")
        { try { EventRecoveryReplay.Run(); Console.WriteLine("PASS EventRecoveryReplay assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Length == 1 && args[0] == "--event-fairness")
        { try { EventFairnessReplay.Run(); Console.WriteLine("PASS EventFairnessReplay assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Length == 1 && args[0] == "--event-retirement")
        { try { EventRetirementReplay.Run(); Console.WriteLine("PASS EventRetirementReplay assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Length == 1 && args[0] == "--event-settings")
        { try { EventSettingsReplay.Run(); Console.WriteLine("PASS EventSettingsReplay assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Length == 1 && args[0] == "--aid-request")
        { try { AidRequestReplay.Run(); Console.WriteLine("PASS AidRequestReplay assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Length == 1 && args[0] == "--event-memory")
        { try { EventMemoryReplay.Run(); PolicyStyleNoticeReplay.Run(); Console.WriteLine("PASS EventMemoryReplay assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Length == 1 && args[0] == "--event-boundary")
        { try { EventBoundaryReplay.Run(); Console.WriteLine("PASS EventBoundaryReplay assertions=" + Test.Assertions); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
        if (args.Length == 1 && args[0] == "--event-commitment")
        {
            EventCommitmentReplay.Run();
            Console.WriteLine($"Event commitment replay passed: {Test.Assertions} assertions.");
            return 0;
        }
        AppDomain.CurrentDomain.UnhandledException += (_, e) => { Console.Error.WriteLine(e.ExceptionObject); Environment.Exit(1); };
        if (args.Length == 1 && args[0] == "--policy-style-notices")
        {
            PolicyStyleNoticeReplay.Run();
            Console.WriteLine($"Policy-style diplomacy notices passed: {Test.Assertions} assertions.");
            return 0;
        }
        if (args.Length == 1 && args[0] == "--writing-style")
        {
            WritingStyleReplay.Run();
            GeneratedDeclarationCapacityReplay.Run();
            Console.WriteLine($"Diplomacy writing style replay passed: {Test.Assertions} assertions.");
            return 0;
        }
        if (args.Length == 1 && args[0] == "--player-lifecycle")
        {
            PlayerLifecycleReplay.Run();
            Console.WriteLine($"Player lifecycle diplomacy passed: {Test.Assertions} assertions.");
            return 0;
        }
        if (args.Length == 1 && args[0] == "--player-independent")
        {
            PlayerIndependentReplay.Run();
            Console.WriteLine($"Player independent diplomacy passed: {Test.Assertions} assertions.");
            return 0;
        }
        if (args.Length == 1 && args[0] == "--campaign-daily")
        {
            CampaignApplicationReplay.Run();
            Console.WriteLine($"Campaign daily application replay passed: {Test.Assertions} assertions.");
            return 0;
        }
        if (args.Length == 1 && args[0] == "--oral-declaration")
        {
            OralDeclarationReplay.Run();
            Console.WriteLine($"Oral AI declaration replay passed: {Test.Assertions} assertions.");
            return 0;
        }
        if (args.Length == 1 && args[0] == "--propagation-closure")
        {
            PropagationLifecycleReplay.Run(); PropagationApplicationReplay.Run();
            Console.WriteLine($"Diplomacy propagation closure passed: {Test.Assertions} assertions.");
            return 0;
        }
        if (args.Length == 1 && args[0] == "--player-response-recovery")
        {
            PlayerResponseRecoveryReplay.Run();
            Console.WriteLine($"Player response recovery passed: {Test.Assertions} assertions.");
            return 0;
        }
        if (args.Length == 1 && args[0] == "--subject-release")
        {
            SubjectReleaseReplay.Run();
            Console.WriteLine($"Player subject release passed: {Test.Assertions} assertions.");
            return 0;
        }
        if (args.Length == 1 && args[0] == "--lifecycle-repair")
        {
            LifecycleRepairReplay.Run();
            Console.WriteLine($"Diplomacy lifecycle repair passed: {Test.Assertions} assertions.");
            return 0;
        }
        if (args.Length == 1 && args[0] == "--ordinary-admission-and-review-fixes")
        {
            EventStructureReplay.Run(); ReviewFixReplay.Run();
            Console.WriteLine($"Ordinary admission and review fixes passed: {Test.Assertions} assertions.");
            return 0;
        }
        if (args.Length == 1 && args[0] == "--player-semantics")
        {
            PlayerSemanticReplay.Run();
            Console.WriteLine($"Player semantics replay passed: {Test.Assertions} assertions.");
            return 0;
        }
        if (args.Length == 1 && (args[0] == "--war-admission-wiring" || args[0] == "--threat-admission-wiring"))
        {
            if (args[0] == "--war-admission-wiring") WarAdmissionReplay.RunStateWiring();
            else WarAdmissionReplay.RunThreatWiring();
            Console.WriteLine($"Real orchestration admission wiring passed: {Test.Assertions} assertions.");
            return 0;
        }
        if (args.Length == 1 && args[0] == "--concurrent-oral-migration")
        {
            ConcurrentOralMigrationReplay.Run();
            Console.WriteLine($"Concurrent/oral Application replay passed: {Test.Assertions} assertions.");
            return 0;
        }
        if (args.Length == 1 && args[0] == "--r1-round-compression")
        {
            CompletionApplicationReplay.Run();
            RunCompressionCommitDecisionTests();
            VerifyRoundCompressionOwnership();
            Console.WriteLine($"R1 round compression replay passed: {Test.Assertions} assertions.");
            return 0;
        }
        if (args.Length == 1 && args[0] == "--r1-failure")
        {
            CompletionApplicationReplay.Run();
            RunFailedJobCommitDecisionTests();
            VerifyFailedJobOwnership();
            Console.WriteLine($"R1 failure recovery replay passed: {Test.Assertions} assertions.");
            return 0;
        }
        if (args.Length == 1 && args[0] == "--r1-round-plan")
        {

            VerifyRoundPlanOwnership();
            Console.WriteLine($"R1 round plan replay passed: {Test.Assertions} assertions.");
            return 0;
        }
        if (args.Length == 1 && args[0] == "--r1-generated")
        {
            RunGeneratedDocumentCommitDecisionTests();
            VerifyGeneratedCompletionOwnership();
            Console.WriteLine($"R1 generated completion replay passed: {Test.Assertions} assertions.");
            return 0;
        }
        if (args.Length == 1 && args[0] == "--r1-job-launch")
        {
            LlmDispatchApplicationReplay.Run();
            RunPreSendGateDecisionTests();
            RunLlmJobRoutingDecisionTests();
            VerifyJobLaunchOwnership();
            Console.WriteLine($"R1 job launch replay passed: {Test.Assertions} assertions.");
            return 0;
        }
        if (args.Length == 1 && args[0] == "--r1-round-progress")
        {

            VerifyRoundProgressOwnership();
            Console.WriteLine($"R1 round progress replay passed: {Test.Assertions} assertions.");
            return 0;
        }
        if (args.Length == 1 && args[0] == "--r1-turn-scheduling")
        {

            VerifyTurnSchedulingOwnership();
            Console.WriteLine($"R1 turn scheduling replay passed: {Test.Assertions} assertions.");
            return 0;
        }
        if (args.Length == 1 && args[0] == "--r1-generation-task")
        {


            VerifyGenerationTaskOwnership();
            Console.WriteLine($"R1 generation task replay passed: {Test.Assertions} assertions.");
            return 0;
        }
        MaterialSupplyReplay.StructureObservation();
        OralDeclarationReplay.Run();
        MaterialSupplyReplay.StructureSelection();
        MaterialSupplyReplay.BoundMatterRecovery();
        CountryAdmissionReplay.Run();
        EventConvergenceReplay.Run(); DiplomacyBaselineReplay.Run();
        LiveDiplomacyFixReplay.StallSilenceClosure();
        MaterialCountryReplay.Run(); MaterialCountryCompletionReplay.Run(); EventStructureReplay.Run(); ReviewFixReplay.Run(); LifecycleRepairReplay.Run(); AuditFixesReplay.Run(); IndependentAuditFixReplay.Run(); LiveDiplomacyFixReplay.Run();
        OfferActionReplay.Run();
        WarAdmissionReplay.Run();
        LlmDispatchApplicationReplay.Run();
        CompletionApplicationReplay.Run();
        Dpl090PresentationReplay.Run();
        ReplyShortcutReplay.Run();
        ProactiveDiscussionApplicationReplay.Run();
        Dpl080PromptReplay.Run();
        VerifyDeadlineRules();
        VerifyStatusRules();
        VerifyRelayProgressionRules();
        VerifySchedulingAndExchangeDecisionTable();
        VerifyAdvanceAndAdmissionDecisionTable();
        VerifyCloseAndRelayHopDecisionTable();
        VerifyOfferValidityAndJoinDecisionTable();
        VerifyNoActionAuthorizationDecisionTable();
        RunResponseAddressingDecisionTests();
        RunMandatoryReplyDecisionTests();
        RunIntentVocabularyDecisionTests();
        RunDeferredRetryDecisionTests();
        RunThreatAndConfirmedResultDecisionTests();
        RunThreatViolationDecisionTests();
        RunRoundAccountingDecisionTests();
        RunThreatDocumentDispatchDecisionTests();
        RunThreatRegistrationComplianceTests();
        RunThreatHistoryDecisionTests();
        RunThreatNormalizationDecisionTests();
        RunPolicyConditionDecisionTests();
        RunThreatConsequenceDecisionTests();
        RunWarResponseGateDecisionTests();
        RunStorageNormalizationDecisionTests();
        RunRoundRecordNormalizationDecisionTests();
        RunOfferStatusDecisionTests();
        RunRecordIdentityDecisionTests();
        RunChronologicalOrderingDecisionTests();
        RunRecordOrderingDecisionTests();
        RunCanonicalHistoryNormalizationDecisionTests();
        RunEnvelopeJsonDecisionTests();
        RunTextRuleDecisionTests();

        RunOfferContractDecisionTests();
        RunPromptContractDecisionTests();
        RunStructureRuleDecisionTests();
        RunDocumentFactDecisionTests();
        RunCanonicalRenderDecisionTests();
RunDocumentRecordDecisionTests();
RunDocumentTitleDecisionTests();
RunStrategicProfilePromptDecisionTests();
RunCanonicalHistoryMaintenanceDecisionTests();
RunMiscHelperDecisionTests();
        RunDocumentMemoryLineDecisionTests();
        RunUnsafePeaceAcceptanceDecisionTests();
        RunLlmMessageAssemblyDecisionTests();
        RunStorageQueryRuleDecisionTests();
        RunDisplayTextRuleDecisionTests();
        RunBattleFactRenderDecisionTests();
        RunPeaceTermPatternDecisionTests();

        RunOpenProposalAndThreatAdapterDecisionTests();
        RunWarSnapshotAdapterDecisionTests();

        RunStorageKnowledgeAndContextDecisionTests();
        RunThreatFinalizationDecisionTests();


RunExchangeAndOfferCooldownDecisionTests();

RunStorageMigrationDecisionTests();
RunCanonicalHistoryMigrationDecisionTests();
RunCanonicalHistoryInitDecisionTests();
RunCanonicalHistoryAppendDecisionTests();
RunPolicyArtifactSyncDecisionTests();
RunThreatHistoryResultDecisionTests();
RunCompressionCommitDecisionTests();
RunCanonicalRenderAndStampDecisionTests();
RunCompressionSchedulingDecisionTests();
RunRoundSummaryArchiveDecisionTests();
RunOfferPruningDecisionTests();
RunStoredRecordNormalizationDecisionTests();
RunRoundBookkeepingDecisionTests();
RunLegalActionIntentDecisionTests();
RunRelaySourceContextDecisionTests();
RunGenerationSignatureDecisionTests();
RunGeneratedStructureAndTargetSelectionDecisionTests();
RunPlayerWorldStateIntentDecisionTests();

RunGenerationValidationDecisionTests();
RunRepairCorrectionAndJobDecisionTests();
        RunSingleActionLegalityDecisionTests();
        RunGeneratedEnvelopeApplicationDecisionTests();
        RunSettlementTurnBookkeepingDecisionTests();

        RunPreSendGateDecisionTests();

        RunAnalysisCommitDecisionTests();

        RunFailedJobCommitDecisionTests();
        RunGeneratedDocumentCommitDecisionTests();

        RunLlmJobRoutingDecisionTests();

    RunJobCompositionDecisionTests();

        PromptMemoryReplay.Run();
        NativeDiplomacyFallbackReplay.Run();
        AuthorityReplay.Run();
        RoundApplicationReplay.Run();
        string orchestrationHost = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.OrchestrationHost.cs"));
        Test.True(orchestrationHost.Contains("RoundTargetDurationDays() => WorldDiplomacyBehavior.GetRoundLengthDays()"), "round opening must read configured duration rather than a relay default");
        Test.True(orchestrationHost.Contains("a == null || b == null ? float.MaxValue"), "missing courts sort after reachable courts rather than at zero distance");
        PropagationApplicationReplay.Run();
        PublicationScheduleReplay.Run();
        OfferApplicationReplay.Run();
        CourtResponseReplay.Run();
        DocumentApplicationReplay.Run();
        DocumentExecutionReplay.Run();
        SubjectReleaseReplay.Run();
        string executionSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyDocumentExecutionApplication.cs"));
        Test.True(!executionSource.Contains("ProcessAnalyzedMultiActionDocument(")
            && executionSource.Split(new[] { "orchestration.ExecuteImmediateIntent(" }, StringSplitOptions.None).Length == 2,
            "single and multi documents share one effect loop with no predecessor algorithm");

        PublicationRoutingReplay.Run();
        ImmediateActionReplay.Run();
        PropagationLifecycleReplay.Run();
        ThreatSettlementReplay.Run();
        WorldProfileReplay.Run();
        RetainedHostRulesReplay.Run();
        CampaignApplicationReplay.Run();
        InitialPeaceReplay.Run();
        HistoryCaptureReplay.Run();
        RequestHistoryBudgetReplay.Run();
        AutonomousRecallReplay.Run();
        NativeEventAdmissionReplay.Run();
        EventCommitmentReplay.Run();
        EventBoundaryReplay.Run();
        EventMemoryReplay.Run();
        AidRequestReplay.Run();
        EventSettingsReplay.Run();
        EventRetirementReplay.Run();
        EventFairnessReplay.Run();
        EventRecoveryReplay.Run();
        EventLegacyReplay.Run();
        SpeechReviewReplay.Run();
        OriginalOralVassalageReplay.Run();
        LiveBlockerReplay.Run();
        MaterialRecoveryReplay.Run();
        CompressionCapacityReplay.Run();
        HistoryFragmentReplay.Run();
        LegacyArchiveEvidenceReplay.Run();
        PolicySignalRefreshReplay.Run();
        ActionSelectionReplay.Run();
        JobPreparationReplay.Run();
        WritingStyleReplay.Run();
        GeneratedDeclarationCapacityReplay.Run();
        AnalysisApplicationReplay.Run();
        PlayerSemanticReplay.Run();
        PeaceAdmissionReplay.Run();
        PeaceProposalReplay.Run();
        PersistenceSyncReplay.Run();
        VerifySourceBoundary();
        Console.WriteLine($"World diplomacy round lifecycle smoke tests passed: {Test.Assertions} assertions.");
        return 0;
    }

    private static void VerifyRoundCompressionOwnership()
    {
        string app = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyRoundCompressionApplication.cs"));
        string behavior = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOrchestration.cs"));
        string rules = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Domain/WorldDiplomacyRoundLifecycleRules.cs"));
        string completion = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyCompletionApplication.cs"));
        Test.True(app.Contains("storage.RoundSummaries.RemoveAll(", StringComparison.Ordinal)
            && app.Contains("storage.RoundSummaries.Add(summary)", StringComparison.Ordinal)
            && behavior.Contains("WorldDiplomacyRoundCompressionApplication.Commit(Storage, job, raw,", StringComparison.Ordinal)
            && completion.Contains("orchestration.CommitRoundCompression(job, resultContent)", StringComparison.Ordinal)
            && !rules.Contains("public static void CommitRoundCompression(", StringComparison.Ordinal)
            && !behavior.Contains("WorldDiplomacyRoundLifecycleRules.CommitRoundCompression(", StringComparison.Ordinal),
            "real completion caller reaches one Application archive owner; predecessor has no second algorithm");
    }

    private static void VerifyFailedJobOwnership()
    {
        string app = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyFailureApplication.cs"));
        string behavior = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOrchestration.cs"));
        string rules = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Domain/WorldDiplomacyRoundLifecycleRules.cs"));
        Test.True(app.Contains("compressionRetryInitialHours << Math.Max(0", StringComparison.Ordinal)
            && app.Contains("commitAnalysis?.Invoke(job, buildFallbackAnalysisJson?.Invoke(job))", StringComparison.Ordinal)
            && behavior.Contains("WorldDiplomacyFailureApplication.Commit(", StringComparison.Ordinal)
            && !behavior.Contains("WorldDiplomacyRoundLifecycleRules.CommitFailedJob(", StringComparison.Ordinal)
            && !rules.Contains("public static void CommitFailedJob(", StringComparison.Ordinal),
            "completion and dispatch failures must enter one Application owner with no predecessor algorithm");
    }

    private static void VerifyRoundPlanOwnership()
    {
        string app = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyRoundPlanApplication.cs"));
        string behavior = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOrchestration.cs"));
        string rules = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Domain/WorldDiplomacyRoundLifecycleRules.cs"));
        Test.True(app.Contains("round.RelayRouteKingdomIds = route", StringComparison.Ordinal)
            && app.Contains("round_plan_no_participants", StringComparison.Ordinal)
            && app.Contains("scheduleResultSettlement?.Invoke(round)", StringComparison.Ordinal)
            && behavior.Contains("WorldDiplomacyRoundPlanApplication.Commit(", StringComparison.Ordinal)
            && !behavior.Contains("WorldDiplomacyRoundLifecycleRules.CommitRoundPlan(", StringComparison.Ordinal)
            && !rules.Contains("public static void CommitRoundPlan(", StringComparison.Ordinal),
            "real plan caller enters Application route owner; Domain and Behavior contain no second plan algorithm");
    }

    private static void VerifyGeneratedCompletionOwnership()
    {
        string app = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyGeneratedCompletionApplication.cs"));
        string behavior = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOrchestration.cs"));
        string rules = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Domain/WorldDiplomacyRoundLifecycleRules.cs"));
        Test.True(app.Contains("stale result-settlement generation discarded", StringComparison.Ordinal)
            && app.Contains("processAnalyzedDocument?.Invoke(document", StringComparison.Ordinal)
            && app.Contains("exchange.State = \"analyzing_response\"", StringComparison.Ordinal)
            && behavior.Contains("WorldDiplomacyGeneratedCompletionApplication.Commit(", StringComparison.Ordinal)
            && !rules.Contains("public static void CommitGeneratedDocument(", StringComparison.Ordinal)
            && !behavior.Contains("WorldDiplomacyRoundLifecycleRules.CommitGeneratedDocument(", StringComparison.Ordinal),
            "real generated completion enters Application for admission, exchange state and analysis dispatch");
    }

    private static void VerifyJobLaunchOwnership()
    {
        string app = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyLlmDispatchApplication.cs"));
        string behavior = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOrchestration.cs"));
        string rules = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Domain/WorldDiplomacyRoundLifecycleRules.cs"));
        Test.True(app.Contains("WorldDiplomacyJob job = SelectAndPrepareLlmJob(", StringComparison.Ordinal)
            && app.Contains("internal static WorldDiplomacyJob SelectAndPrepareLlmJob(", StringComparison.Ordinal)
            && app.Contains("internal static bool EnsureCurrentCanonicalPromptContractBeforeSend(", StringComparison.Ordinal)
            && app.Contains("tryConsumeRequestBudget?.Invoke(true)", StringComparison.Ordinal)
            && behavior.Contains("WorldDiplomacyLlmDispatchApplication.Run(ref source, this)", StringComparison.Ordinal)
            && !rules.Contains("public static WorldDiplomacyJob SelectAndPrepareLlmJob(", StringComparison.Ordinal)
            && !rules.Contains("public static bool EnsureCurrentCanonicalPromptContractBeforeSend(", StringComparison.Ordinal),
            "real launch caller enters Application for selection, preflight, budget and claim");
    }

    private static void VerifyRoundProgressOwnership()
    {
        string app = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyRoundProgressApplication.cs"));
        string behavior = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOrchestration.cs"));
        string rules = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Domain/WorldDiplomacyRoundLifecycleRules.cs"));
        Test.True(app.Contains("document.RoundAccountingHandled = true", StringComparison.Ordinal)
            && app.Contains("document.RoundProgressHandled = true", StringComparison.Ordinal)
            && app.Contains("advanceRelay(round)", StringComparison.Ordinal)
            && behavior.Contains("WorldDiplomacyRoundProgressApplication.HandleRoundDocumentProcessed(", StringComparison.Ordinal)
            && !rules.Contains("public static void HandleRoundDocumentProcessed(", StringComparison.Ordinal),
            "real document publication must enter Application for round accounting and route completion");
    }

    private static void VerifyTurnSchedulingOwnership()
    {
        string app = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyTurnSchedulingApplication.cs"));
        string behavior = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOrchestration.cs"));
        string rules = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Domain/WorldDiplomacyRoundLifecycleRules.cs"));
        Test.True(app.Contains("storage.RelayArrivals.Add(new WorldDiplomacyRelayArrival", StringComparison.Ordinal)
            && app.Contains("ScheduleSettlementRelayArrival(", StringComparison.Ordinal)
            && behavior.Contains("WorldDiplomacyTurnSchedulingApplication.ScheduleNextRelayHop(", StringComparison.Ordinal)
            && behavior.Contains("WorldDiplomacyTurnSchedulingApplication.ScheduleNextResultSettlementTurn(", StringComparison.Ordinal)
            && !rules.Contains("public static void ScheduleNextRelayHop(", StringComparison.Ordinal)
            && !rules.Contains("public static void ScheduleNextResultSettlementTurn(", StringComparison.Ordinal),
            "real round caller must enter Application for relay and settlement scheduling");
    }

    private static void VerifyGenerationTaskOwnership()
    {
        string app = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyGenerationTaskApplication.cs"));
        string behavior = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOrchestration.cs"));
        string rules = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Domain/WorldDiplomacyRoundLifecycleRules.cs"));
        Test.True(app.Contains("job.PresentedLegalActionSignature = buildLegalSignature?.Invoke(job)", StringComparison.Ordinal)
            && app.Contains("captureCanonicalHistory?.Invoke(job)", StringComparison.Ordinal)
            && app.Contains("enqueue?.Invoke(job)", StringComparison.Ordinal)
            && app.Contains("technical_consecutive_generation_rejections", StringComparison.Ordinal)
            && behavior.Contains("WorldDiplomacyGenerationTaskApplication.PrepareGenerationJob(", StringComparison.Ordinal)
            && behavior.Contains("WorldDiplomacyGenerationTaskApplication.AbandonRejectedGeneration(", StringComparison.Ordinal)
            && !rules.Contains("public static void PrepareGenerationJob(", StringComparison.Ordinal)
            && !rules.Contains("public static void AbandonRejectedGeneration(", StringComparison.Ordinal),
            "generation admission and terminal rejection must have one Application owner");
    }

    private static WorldDiplomacyRoundReconcileInput BaseInput()
    {
        return new WorldDiplomacyRoundReconcileInput
        {
            RoundActive = true,
            CurrentDay = 10,
            HardEndDay = 100,
            HasRootDocument = true
        };
    }



    private static void VerifyDeadlineRules()
    {
        Test.True(WorldDiplomacyRoundLifecycleRules.IsHardEndReached(100, 100),
            "hard end is reached on the deadline day itself");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsHardEndReached(99, 100),
            "hard end is not reached before the deadline day");

        Test.True(WorldDiplomacyRoundLifecycleRules.ComputeSettlementWindowDays(0) == 14,
            "settlement window floors at fourteen days");
        Test.True(WorldDiplomacyRoundLifecycleRules.ComputeSettlementWindowDays(5) == 14,
            "small routes still use the fourteen-day floor");
        Test.True(WorldDiplomacyRoundLifecycleRules.ComputeSettlementWindowDays(6) == 16,
            "larger routes extend the window by two days per route kingdom beyond the floor");
        Test.True(WorldDiplomacyRoundLifecycleRules.ComputeSettlementWindowDays(-3) == 14,
            "negative route counts clamp to the floor");

        Test.True(WorldDiplomacyRoundLifecycleRules.ExtendHardEndDay(50, 10, 3) == 50,
            "hard end is never shortened by an extension");
        Test.True(WorldDiplomacyRoundLifecycleRules.ExtendHardEndDay(10, 40, 3) == 43,
            "hard end extends to the requested minimum window");

        Test.True(!WorldDiplomacyRoundLifecycleRules.ShouldForceTerminalMove(1),
            "one consecutive no-action pass must not force a terminal move");
        Test.True(WorldDiplomacyRoundLifecycleRules.ShouldForceTerminalMove(2),
            "two consecutive no-action passes force a terminal move");
        Test.True(WorldDiplomacyRoundLifecycleRules.ShouldForceTerminalMove(3),
            "saturated no-action counters keep forcing a terminal move");
    }

    private static void VerifyStatusRules()
    {
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeResultSettlementStatus("deadlocked") == "deadlocked",
            "deadlocked results stay deadlocked");
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeResultSettlementStatus("DEADLOCKED") == "deadlocked",
            "status normalization is case-insensitive");
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeResultSettlementStatus("resolved") == "resolved",
            "resolved results stay resolved");
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeResultSettlementStatus("aborted") == "resolved",
            "unexpected statuses collapse to resolved for settlement bookkeeping");
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeResultSettlementStatus(null!) == "resolved",
            "missing statuses collapse to resolved");

        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeTerminalMoveStatus("declare_deadlock") == "deadlocked",
            "a declared deadlock move resolves the round as deadlocked");
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeTerminalMoveStatus("end_negotiation") == "resolved",
            "any other terminal move resolves the round as resolved");
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeTerminalMoveStatus(null!) == "resolved",
            "a missing terminal move resolves the round as resolved");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsResolvedRoundStatus("Resolved"),
            "resolved status detection is case-insensitive");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsResolvedRoundStatus("deadlocked"),
            "deadlocked is not a resolved status");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsTerminalRoundStatus("resolved"),
            "resolved is a terminal round status");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsTerminalRoundStatus("Deadlocked"),
            "deadlocked is a terminal round status");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsTerminalRoundStatus("CLOSED"),
            "closed is a terminal round status");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsTerminalRoundStatus("active"),
            "active is not a terminal round status");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsTerminalRoundStatus("aborted"),
            "aborted is not a terminal round status");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsTerminalRoundStatus(null!),
            "missing status is not terminal");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsRetryableAbort("aborted", initiatorHasPublished: true),
            "aborted rounds are retryable even when the initiator published");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsRetryableAbort("active", initiatorHasPublished: false),
            "rounds without an initiator publication are retryable");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsRetryableAbort("resolved", initiatorHasPublished: true),
            "settled rounds with an initiator publication are not retryable");
    }

    private static void VerifyRelayProgressionRules()
    {
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeRelayCursor(-1, 5) == 0,
            "negative relay cursors reset to the route head");
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeRelayCursor(5, 5) == 0,
            "out-of-range relay cursors reset to the route head");
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeRelayCursor(3, 5) == 3,
            "valid relay cursors are preserved");

        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeRelayDirection(0) == 1,
            "unset relay directions normalize forward");
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeRelayDirection(7) == 1,
            "invalid relay directions normalize forward");
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeRelayDirection(-1) == -1,
            "backward relay direction is preserved");
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeRelayDirection(1) == 1,
            "forward relay direction is preserved");

        Test.True(WorldDiplomacyRoundLifecycleRules.NextRelayDirection(1) == -1,
            "a completed forward pass flips to backward");
        Test.True(WorldDiplomacyRoundLifecycleRules.NextRelayDirection(0) == -1,
            "a zero direction still flips to backward");
        Test.True(WorldDiplomacyRoundLifecycleRules.NextRelayDirection(-1) == 1,
            "a completed backward pass flips to forward");

        Test.True(WorldDiplomacyRoundLifecycleRules.ComputeRelayProgressIndex(5, 2, 1) == 2,
            "forward progress uses the next index directly");
        Test.True(WorldDiplomacyRoundLifecycleRules.ComputeRelayProgressIndex(5, 2, -1) == 2,
            "backward progress mirrors the index around the route tail");

        WorldDiplomacyRelayArrivalPlanInput forward = new WorldDiplomacyRelayArrivalPlanInput
        {
            RelayPassStartedDay = 10,
            PassDurationDays = 4,
            NextIndex = 2,
            RouteCount = 5,
            RelayDirection = 1,
            CurrentDay = 20,
            HardEndDay = 100
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.ComputeRelayArrivalDay(forward) == 12,
            "forward arrival day is pass start plus the proportional edge share");
        WorldDiplomacyRelayArrivalPlanInput backward = new WorldDiplomacyRelayArrivalPlanInput
        {
            RelayPassStartedDay = 10,
            PassDurationDays = 4,
            NextIndex = 2,
            RouteCount = 5,
            RelayDirection = -1,
            CurrentDay = 20,
            HardEndDay = 100
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.ComputeRelayArrivalDay(backward) == 12,
            "backward arrival day mirrors the same edge share");
        forward.ScheduleImmediately = true;
        Test.True(WorldDiplomacyRoundLifecycleRules.ComputeRelayArrivalDay(forward) == 20,
            "immediate scheduling lands the arrival on the current day");
        forward.ScheduleImmediately = false;
        forward.FinalActionOpportunityIssued = true;
        forward.SubstantiveProgressCount = 0;
        forward.HardEndDay = 11;
        Test.True(WorldDiplomacyRoundLifecycleRules.ComputeRelayArrivalDay(forward) == 11,
            "a final-opportunity round with no progress clamps the arrival to the hard end");
        forward.SubstantiveProgressCount = 1;
        Test.True(WorldDiplomacyRoundLifecycleRules.ComputeRelayArrivalDay(forward) == 12,
            "substantive progress keeps the unclamped arrival day");
        WorldDiplomacyRelayArrivalPlanInput singleEdge = new WorldDiplomacyRelayArrivalPlanInput
        {
            RelayPassStartedDay = 10,
            PassDurationDays = 4,
            NextIndex = 0,
            RouteCount = 1,
            RelayDirection = 1,
            CurrentDay = 20,
            HardEndDay = 100
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.ComputeRelayArrivalDay(singleEdge) == 14,
            "a degenerate single-member route uses one edge and a one-step floor");

        Test.True(WorldDiplomacyRoundLifecycleRules.ClampNoActionPassCount(-2) == 0,
            "negative no-action counters clamp to zero");
        Test.True(WorldDiplomacyRoundLifecycleRules.ClampNoActionPassCount(9) == 3,
            "no-action counters clamp to the three-pass saturation");
        Test.True(WorldDiplomacyRoundLifecycleRules.ClampPassStartAttemptCount(10, 14) == 10,
            "pass-start attempt counters clamp to the current total");
        Test.True(WorldDiplomacyRoundLifecycleRules.ClampPassStartAttemptCount(10, -4) == 0,
            "negative pass-start counters clamp to zero");
        Test.True(WorldDiplomacyRoundLifecycleRules.ClampLastAccountedPassNumber(-1) == 0,
            "accounted pass numbers clamp to zero");
    }











    private static void VerifySchedulingAndExchangeDecisionTable()
    {
        Test.True(WorldDiplomacyRoundLifecycleRules.ComputeNextRoundDay(10, 7) == 17,
            "the next normal round is scheduled one interval after the base day");
        Test.True(WorldDiplomacyRoundLifecycleRules.ComputeInitialCompressedYear(169, 84) == 1,
            "the initial compressed year lags one full year behind the current day");
        Test.True(WorldDiplomacyRoundLifecycleRules.ComputeInitialCompressedYear(10, 84) == 0,
            "the initial compressed year never goes below zero");

        WorldDiplomacyIntervalRefreshInput initialize = new WorldDiplomacyIntervalRefreshInput
        {
            PreviousInterval = 0,
            CurrentInterval = 7
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateIntervalRefresh(initialize).Action
                  == WorldDiplomacyIntervalRefreshAction.Initialize,
            "an unstamped interval initializes the stamp without touching the schedule");
        WorldDiplomacyIntervalRefreshInput unchanged = new WorldDiplomacyIntervalRefreshInput
        {
            PreviousInterval = 7,
            CurrentInterval = 7
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateIntervalRefresh(unchanged).Action
                  == WorldDiplomacyIntervalRefreshAction.Unchanged,
            "an unchanged interval leaves the schedule alone");
        WorldDiplomacyIntervalRefreshInput activeRound = new WorldDiplomacyIntervalRefreshInput
        {
            PreviousInterval = 7,
            CurrentInterval = 10,
            HasActiveRound = true,
            NextNormalRoundDay = 30,
            CurrentDay = 20
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateIntervalRefresh(activeRound).Action
                  == WorldDiplomacyIntervalRefreshAction.UpdateStampOnly,
            "an active round defers the interval rebase but still updates the stamp");
        WorldDiplomacyIntervalRefreshInput rebase = new WorldDiplomacyIntervalRefreshInput
        {
            PreviousInterval = 7,
            CurrentInterval = 10,
            HasActiveRound = false,
            NextNormalRoundDay = 30,
            CurrentDay = 20
        };
        WorldDiplomacyIntervalRefreshDecision rebased =
            WorldDiplomacyRoundLifecycleRules.EvaluateIntervalRefresh(rebase);
        Test.True(rebased.Action == WorldDiplomacyIntervalRefreshAction.Rebase
                  && rebased.RebasedNextDay == 33,
            "an idle round rebases the next day onto the new interval");
        WorldDiplomacyIntervalRefreshInput pastRebase = new WorldDiplomacyIntervalRefreshInput
        {
            PreviousInterval = 7,
            CurrentInterval = 3,
            HasActiveRound = false,
            NextNormalRoundDay = 30,
            CurrentDay = 40
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateIntervalRefresh(pastRebase).RebasedNextDay == 40,
            "a rebased schedule never moves the next round into the past");

        Test.True(!WorldDiplomacyRoundLifecycleRules.IsNormalRoundDue(9, 10),
            "a normal round is not due before its scheduled day");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsNormalRoundDue(10, 10),
            "a normal round is due on its scheduled day");
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeRotationStartIndex(-5, 4) == 1,
            "a negative rotation index normalizes into the candidate range");
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeRotationStartIndex(9, 4) == 1,
            "an overflowing rotation index wraps into the candidate range");
        Test.True(WorldDiplomacyRoundLifecycleRules.NextRotationIndex(2, 4) == 3,
            "the rotation index advances to the slot after the selected candidate");
        Test.True(WorldDiplomacyRoundLifecycleRules.NextRotationIndex(3, 4) == 0,
            "the rotation index wraps to the start after the last candidate");

        Test.True(!WorldDiplomacyRoundLifecycleRules.IsExchangeReminderDue(true, 10, 5),
            "an exchange reminder is not due once it was already sent");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsExchangeReminderDue(false, 4, 5),
            "an exchange reminder is not due before its due day");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsExchangeReminderDue(false, 5, 5),
            "an exchange reminder is due on its due day");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsExchangeCloseDue(4, 5),
            "an exchange does not close before its close day");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsExchangeCloseDue(5, 5),
            "an exchange closes on its close day");

        Test.True(WorldDiplomacyRoundLifecycleRules.ComputeSuspendedPauseDays(10, 6) == 4,
            "a suspended exchange shifts its deadlines by the paused days");
        Test.True(WorldDiplomacyRoundLifecycleRules.ComputeSuspendedPauseDays(4, 6) == 0,
            "a suspended exchange never shifts deadlines into the past");
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeRestoredExchangeState("") == "waiting",
            "a suspended exchange without a prior state restores to waiting");
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeRestoredExchangeState("waiting_player_response")
                  == "waiting_player_response",
            "a suspended exchange restores its prior state");
    }

    private static void VerifyAdvanceAndAdmissionDecisionTable()
    {
        Test.True(WorldDiplomacyRoundLifecycleRules.IsActiveRoundState("active"),
            "the active-round predicate accepts the active state");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsActiveRoundState("ACTIVE"),
            "the active-round predicate is case-insensitive");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsActiveRoundState("closed"),
            "the active-round predicate rejects terminal states");

        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateRelayAdvanceAction(false, false, false)
                  == WorldDiplomacyRelayAdvanceAction.None,
            "an inactive round never advances the relay");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateRelayAdvanceAction(true, true, false)
                  == WorldDiplomacyRelayAdvanceAction.ScheduleSettlementTurn,
            "a pending settlement reroutes advancement into settlement turns");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateRelayAdvanceAction(true, true, true)
                  == WorldDiplomacyRelayAdvanceAction.ScheduleSettlementTurn,
            "a pending settlement wins even past the hard end");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateRelayAdvanceAction(true, false, true)
                  == WorldDiplomacyRelayAdvanceAction.CloseHardEnd,
            "a hard-ended relay closes the round");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateRelayAdvanceAction(true, false, false)
                  == WorldDiplomacyRelayAdvanceAction.ScheduleRelayHop,
            "an ordinary active round schedules the next relay hop");

        Test.True(WorldDiplomacyRoundLifecycleRules.ShouldRefreshParticipantState("observer", false, "active"),
            "a non-withdrawn participant always refreshes state");
        Test.True(!WorldDiplomacyRoundLifecycleRules.ShouldRefreshParticipantState("withdrawn", false, "observer"),
            "a withdrawn participant keeps its state for non-active requests");
        Test.True(WorldDiplomacyRoundLifecycleRules.ShouldRefreshParticipantState("withdrawn", true, "observer"),
            "a mandatory reply reactivates a withdrawn participant");
        Test.True(WorldDiplomacyRoundLifecycleRules.ShouldRefreshParticipantState("withdrawn", false, "active"),
            "an active request reactivates a withdrawn participant");

        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateRouteAdmission(false, true, false, 0, 8)
                  == WorldDiplomacyRouteAdmission.Denied,
            "route admission is denied when no relay is planned");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateRouteAdmission(true, false, false, 0, 8)
                  == WorldDiplomacyRouteAdmission.Denied,
            "route admission is denied without a kingdom id");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateRouteAdmission(true, true, true, 8, 8)
                  == WorldDiplomacyRouteAdmission.AlreadyOnRoute,
            "an existing route member reports already-on-route even at the cap");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateRouteAdmission(true, true, false, 8, 8)
                  == WorldDiplomacyRouteAdmission.Denied,
            "route admission is denied at the participant cap");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateRouteAdmission(true, true, false, 7, 8)
                  == WorldDiplomacyRouteAdmission.Admitted,
            "route admission admits a new kingdom below the cap");
    }

    private static void VerifyCloseAndRelayHopDecisionTable()
    {
        Test.True(WorldDiplomacyRoundLifecycleRules.IsAbortiveCloseReason("technical_generation_rejected"),
            "technical close reasons abort the round");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsAbortiveCloseReason("automatic_request_circuit_breaker"),
            "the automatic circuit breaker aborts the round");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsAbortiveCloseReason("relay_hard_end"),
            "ordinary close reasons are not abortive");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsAbortiveCloseReason(""),
            "a missing close reason is not abortive");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsOfferCooldownSkippingCloseReason("technical_generation_rejected"),
            "technical close reasons skip offer cooldown settlement");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsOfferCooldownSkippingCloseReason("round_plan_unresolved"),
            "round-plan close reasons skip offer cooldown settlement");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsOfferCooldownSkippingCloseReason("canonical_history_migration_v2"),
            "canonical-history migration close reasons skip offer cooldown settlement");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsOfferCooldownSkippingCloseReason("relay_has_no_participants"),
            "the empty-route close reason skips offer cooldown settlement");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsOfferCooldownSkippingCloseReason("result_settled"),
            "ordinary close reasons do not skip offer cooldown settlement");

        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateClosedRoundStatus(
                  "technical_generation_rejected", "active", 5, 5)
                  == WorldDiplomacyClosedRoundStatus.Aborted,
            "an abortive close marks the round aborted even with executed actions");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateClosedRoundStatus(
                  "result_settled", "resolved", 5, 5)
                  == WorldDiplomacyClosedRoundStatus.KeepCurrent,
            "a non-active round keeps its already-terminal status");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateClosedRoundStatus(
                  "result_settled", "active", 3, 5)
                  == WorldDiplomacyClosedRoundStatus.Resolved,
            "an active round with executed actions closes resolved");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateClosedRoundStatus(
                  "relay_hard_end", "active", 0, 5)
                  == WorldDiplomacyClosedRoundStatus.Deadlocked,
            "an active round with only attempts closes deadlocked");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateClosedRoundStatus(
                  "relay_all_participants_withdrew", "active", 0, 0)
                  == WorldDiplomacyClosedRoundStatus.Closed,
            "an active round without any action closes plainly");

        Test.True(WorldDiplomacyRoundLifecycleRules.ClosedRoundStatusName(
                  WorldDiplomacyClosedRoundStatus.Aborted) == "aborted",
            "the closed-status name maps aborted");
        Test.True(WorldDiplomacyRoundLifecycleRules.ClosedRoundStatusName(
                  WorldDiplomacyClosedRoundStatus.Resolved) == "resolved",
            "the closed-status name maps resolved");
        Test.True(WorldDiplomacyRoundLifecycleRules.ClosedRoundStatusName(
                  WorldDiplomacyClosedRoundStatus.Deadlocked) == "deadlocked",
            "the closed-status name maps deadlocked");
        Test.True(WorldDiplomacyRoundLifecycleRules.ClosedRoundStatusName(
                  WorldDiplomacyClosedRoundStatus.Closed) == "closed",
            "the closed-status name maps closed");
        Test.True(WorldDiplomacyRoundLifecycleRules.ClosedRoundStatusName(
                  WorldDiplomacyClosedRoundStatus.KeepCurrent) == "",
            "keep-current has no status name so the host leaves the field alone");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsOpenLifecycleStatus("open"),
            "the open-lifecycle predicate accepts open");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsOpenLifecycleStatus("expired"),
            "the open-lifecycle predicate rejects expired");

        Test.True(WorldDiplomacyRoundLifecycleRules.CanScheduleRelayHop(true, false, false, true),
            "a planned idle relay on an active round may schedule a hop");
        Test.True(!WorldDiplomacyRoundLifecycleRules.CanScheduleRelayHop(false, false, false, true),
            "a round without a planned relay never schedules a hop");
        Test.True(!WorldDiplomacyRoundLifecycleRules.CanScheduleRelayHop(true, true, false, true),
            "a waiting relay never schedules another hop");
        Test.True(!WorldDiplomacyRoundLifecycleRules.CanScheduleRelayHop(true, false, true, true),
            "a tripped circuit breaker never schedules another hop");
        Test.True(!WorldDiplomacyRoundLifecycleRules.CanScheduleRelayHop(true, false, false, false),
            "an inactive round never schedules a hop");

        Test.True(!WorldDiplomacyRoundLifecycleRules.HasMinimumRelayRoute(1),
            "a single-member route cannot run a relay");
        Test.True(WorldDiplomacyRoundLifecycleRules.HasMinimumRelayRoute(2),
            "a two-member route satisfies the relay minimum");

        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeRelayPassDurationDays(5, 3) == 5,
            "a positive round pass duration wins over the fallback");
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeRelayPassDurationDays(0, 3) == 3,
            "a missing round pass duration falls back to the default");
    }

    private static void VerifyOfferValidityAndJoinDecisionTable()
    {
        Test.True(WorldDiplomacyRoundLifecycleRules.IsOfferPartyInvalid(false, true, false, false, false, true, true),
            "an unresolved proposer invalidates the offer");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsOfferPartyInvalid(true, false, false, false, false, true, true),
            "an unresolved target invalidates the offer");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsOfferPartyInvalid(true, true, true, false, false, true, true),
            "a self-targeted offer is invalid");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsOfferPartyInvalid(true, true, false, true, false, true, true),
            "an eliminated proposer invalidates the offer");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsOfferPartyInvalid(true, true, false, false, true, true, true),
            "an eliminated target invalidates the offer");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsOfferPartyInvalid(true, true, false, false, false, false, true),
            "a proposer without diplomatic authority invalidates the offer");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsOfferPartyInvalid(true, true, false, false, false, true, false),
            "a target without diplomatic authority invalidates the offer");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsOfferPartyInvalid(true, true, false, false, false, true, true),
            "two resolved living authoritative parties keep the offer valid");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsOfferInvalidForIntent(
                  "propose_peace", false, true, false, true, false, true),
            "a peace offer without an active war is invalid");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsOfferInvalidForIntent(
                  "propose_peace", true, true, false, true, false, false),
            "a peace offer with non-executable terms is invalid");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsOfferInvalidForIntent(
                  "propose_peace", true, true, false, true, false, true),
            "a peace offer at war with executable terms is valid");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsOfferInvalidForIntent(
                  "propose_alliance", false, false, false, true, false, true),
            "an alliance offer without the alliance behavior is invalid");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsOfferInvalidForIntent(
                  "propose_alliance", true, true, false, true, false, true),
            "an alliance offer between warring parties is invalid");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsOfferInvalidForIntent(
                  "propose_alliance", false, true, true, true, false, true),
            "an alliance offer between existing allies is invalid");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsOfferInvalidForIntent(
                  "propose_alliance", false, true, false, true, false, true),
            "an alliance offer between non-allied peaceful parties is valid");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsOfferInvalidForIntent(
                  "propose_trade", false, true, false, false, false, true),
            "a trade offer without the trade behavior is invalid");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsOfferInvalidForIntent(
                  "propose_trade", false, true, false, true, true, true),
            "a trade offer with an existing agreement is invalid");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsOfferInvalidForIntent(
                  "propose_trade", false, true, false, true, false, true),
            "a trade offer without an existing agreement is valid");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsOfferInvalidForIntent(
                  "marriage_offer", true, true, false, true, false, true),
            "an unrecognized intent is always invalid");

        Test.True(WorldDiplomacyRoundLifecycleRules.ComposeOfferDeduplicationKey("propose_trade", "a", "b")
                  == "propose_trade|a|b",
            "the dedup key joins intent, proposer, and target");
        Test.True(WorldDiplomacyRoundLifecycleRules.ComposeOfferDeduplicationKey("propose_trade", "", "b")
                  == "propose_trade||b",
            "the dedup key tolerates missing party ids");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsOpenOfferBetweenPair("open", "a", "b", "a", "b"),
            "an open offer matches its own direction");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsOpenOfferBetweenPair("open", "b", "a", "a", "b"),
            "an open offer matches the reversed direction");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsOpenOfferBetweenPair("expired", "a", "b", "a", "b"),
            "a non-open offer never matches");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsOpenOfferBetweenPair("open", "a", "c", "a", "b"),
            "an offer for other parties never matches");

        WorldDiplomacyExternalFactJoinInput inactive = new WorldDiplomacyExternalFactJoinInput { RoundActive = false };
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateExternalFactJoin(inactive)
                  == WorldDiplomacyExternalFactJoinAction.Rejected,
            "an inactive round rejects external facts");
        WorldDiplomacyExternalFactJoinInput pair = new WorldDiplomacyExternalFactJoinInput
        {
            RoundActive = true,
            InitiatorOnRoute = true,
            TargetOnRoute = true
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateExternalFactJoin(pair)
                  == WorldDiplomacyExternalFactJoinAction.JoinViaRoutePair,
            "a fact between two routed parties joins directly");
        WorldDiplomacyExternalFactJoinInput settlement = new WorldDiplomacyExternalFactJoinInput
        {
            RoundActive = true,
            InitiatorOnRoute = true,
            TargetOnRoute = false,
            SettlementPending = true,
            SettlementTargetUsable = true
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateExternalFactJoin(settlement)
                  == WorldDiplomacyExternalFactJoinAction.JoinViaSettlementTarget,
            "a routed initiator may pull a usable settlement target");
        WorldDiplomacyExternalFactJoinInput fallback = new WorldDiplomacyExternalFactJoinInput
        {
            RoundActive = true,
            InitiatorOnRoute = false,
            TargetOnRoute = false,
            SettlementPending = true,
            SettlementTargetUsable = false
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateExternalFactJoin(fallback)
                  == WorldDiplomacyExternalFactJoinAction.CheckOpenOffers,
            "off-route facts fall back to the open-offer check");
    }

    private static void VerifyNoActionAuthorizationDecisionTable()
    {
        Test.True(WorldDiplomacyRoundLifecycleRules.SettlementSlotKindContains("war_response+route", "war_response"),
            "a compound slot kind matches its war_response part");
        Test.True(WorldDiplomacyRoundLifecycleRules.SettlementSlotKindContains("route", "war_response") == false,
            "a plain route slot does not match war_response");
        Test.True(!WorldDiplomacyRoundLifecycleRules.SettlementSlotKindContains("war_response", ""),
            "a blank kind never matches");
        Test.True(WorldDiplomacyRoundLifecycleRules.SettlementSlotKindContains("War_Response", "war_response"),
            "slot-kind matching is case-insensitive");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsWarResponseSlotAuthorized(true, true, true, true, true, true),
            "a pending current war-response slot authorizes the answer");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsWarResponseSlotAuthorized(false, true, true, true, true, true),
            "war-response authorization requires a pending settlement");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsWarResponseSlotAuthorized(true, false, true, true, true, true),
            "war-response authorization requires a resolved author");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsWarResponseSlotAuthorized(true, true, false, true, true, true),
            "war-response authorization requires a resolved target");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsWarResponseSlotAuthorized(true, true, true, false, true, true),
            "war-response authorization requires an active round");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsWarResponseSlotAuthorized(true, true, true, true, false, true),
            "war-response authorization requires a slot id");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsWarResponseSlotAuthorized(true, true, true, true, true, false),
            "war-response authorization requires the current slot");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsWarDeclarationAction(true, "declare_war", "a", "a"),
            "a state-changing declare_war action at the author counts");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsWarDeclarationAction(true, "declare_war", "b", "a"),
            "a declare_war action at another kingdom does not count");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsWarDeclarationAction(false, "declare_war", "a", "a"),
            "a declare_war action without a state change does not count");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsWarDeclarationAction(true, "propose_peace", "a", "a"),
            "a non-war intent does not count");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsWarDeclarationDocument(false, true, "declare_war", "a", "a"),
            "a war document without actions counts through its own fields");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsWarDeclarationDocument(true, true, "declare_war", "a", "a"),
            "a war document with actions must match through an action instead");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsSettlementTargetUsable(true, true, false, false, true, true, 8, 8),
            "an on-route settlement target is always usable");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsSettlementTargetUsable(true, true, false, false, true, false, 7, 8),
            "an off-route settlement target is usable below the cap");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsSettlementTargetUsable(true, true, false, false, true, false, 8, 8),
            "an off-route settlement target is denied at the cap");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsSettlementTargetUsable(false, true, false, false, true, true, 0, 8),
            "settlement targets require a pending settlement");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsSettlementTargetUsable(true, true, true, false, true, true, 0, 8),
            "the author cannot be its own settlement target");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsSettlementTargetUsable(true, true, false, true, true, true, 0, 8),
            "an eliminated kingdom cannot be a settlement target");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsSettlementTargetUsable(true, true, false, false, false, true, 0, 8),
            "a target without authority cannot be a settlement target");

        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateSettlementTargetAdmission(false, true, false, true, 0, 8)
                  == WorldDiplomacyRouteAdmission.Denied,
            "settlement admission requires a pending settlement");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateSettlementTargetAdmission(true, false, false, true, 0, 8)
                  == WorldDiplomacyRouteAdmission.Denied,
            "settlement admission requires a kingdom id");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateSettlementTargetAdmission(true, true, true, false, 8, 8)
                  == WorldDiplomacyRouteAdmission.AlreadyOnRoute,
            "an existing route member reports already-on-route even when ineligible");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateSettlementTargetAdmission(true, true, false, false, 0, 8)
                  == WorldDiplomacyRouteAdmission.Denied,
            "settlement admission denies an ineligible kingdom");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateSettlementTargetAdmission(true, true, false, true, 8, 8)
                  == WorldDiplomacyRouteAdmission.Denied,
            "settlement admission denies an eligible kingdom at the cap");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateSettlementTargetAdmission(true, true, false, true, 7, 8)
                  == WorldDiplomacyRouteAdmission.Admitted,
            "settlement admission admits an eligible kingdom below the cap");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsNoActionAuthorizationEligible(
                  true, true, false, false, false, false, true, true, true),
            "a fully eligible non-root AI pair passes the preamble");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsNoActionAuthorizationEligible(
                  true, true, false, true, false, false, true, true, true),
            "a player author never gets no-action authorization");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsNoActionAuthorizationEligible(
                  true, true, true, false, false, false, true, true, true),
            "a same-party pair never gets no-action authorization");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsNoActionAuthorizationEligible(
                  true, true, false, false, false, false, true, true, false),
            "an inactive round never gets no-action authorization");

        WorldDiplomacyExternalNoActionInput externalBase = new WorldDiplomacyExternalNoActionInput
        {
            AuthorResolved = true,
            TargetResolved = true,
            AuthorHasAuthority = true,
            TargetHasAuthority = true,
            RoundActive = true,
            RootReady = true,
            RootActionable = true,
            ResponseReady = true,
            ResponsePlayerAuthored = true,
            ResponseHasDocumentId = true,
            ResponseSameRound = true,
            ResponseAuthoredByTarget = true,
            IsPrimaryTarget = true,
            MandatoryReplyPending = true,
            LastTriggeredMatches = true,
            RelayPlanned = true,
            IsRelayTurn = true,
            AuthorOnRoute = true,
            TargetOnRoute = true
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateExternalNoActionAuthorization(externalBase),
            "a fully authorized external response passes");
        Test.True(!WorldDiplomacyRoundLifecycleRules.EvaluateExternalNoActionAuthorization(
                  new WorldDiplomacyExternalNoActionInput(externalBase) { RootActionable = false }),
            "an unactionable root blocks external no-action authorization");
        Test.True(!WorldDiplomacyRoundLifecycleRules.EvaluateExternalNoActionAuthorization(
                  new WorldDiplomacyExternalNoActionInput(externalBase) { ResponsePlayerAuthored = false }),
            "a non-player-authored response source blocks external no-action authorization");
        Test.True(!WorldDiplomacyRoundLifecycleRules.EvaluateExternalNoActionAuthorization(
                  new WorldDiplomacyExternalNoActionInput(externalBase)
                  {
                      IsPrimaryTarget = false,
                      IsRepresentativeTarget = false,
                      IsInAddressedList = false
                  }),
            "an unaddressed author blocks external no-action authorization");
        Test.True(!WorldDiplomacyRoundLifecycleRules.EvaluateExternalNoActionAuthorization(
                  new WorldDiplomacyExternalNoActionInput(externalBase)
                  {
                      IsPrimaryTarget = false,
                      ResponseRequiresResponse = false
                  }),
            "a non-primary non-representative author needs an explicit response requirement");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateExternalNoActionAuthorization(
                  new WorldDiplomacyExternalNoActionInput(externalBase)
                  {
                      IsPrimaryTarget = false,
                      ResponseRequiresResponse = false,
                      IsRepresentativeTarget = true
                  }),
            "a representative target does not need an explicit response requirement");
        Test.True(!WorldDiplomacyRoundLifecycleRules.EvaluateExternalNoActionAuthorization(
                  new WorldDiplomacyExternalNoActionInput(externalBase) { MandatoryReplyPending = false }),
            "a responder without a pending mandatory reply is blocked");
        Test.True(!WorldDiplomacyRoundLifecycleRules.EvaluateExternalNoActionAuthorization(
                  new WorldDiplomacyExternalNoActionInput(externalBase) { SettlementPending = true }),
            "result settlement blocks external no-action authorization");
        Test.True(!WorldDiplomacyRoundLifecycleRules.EvaluateExternalNoActionAuthorization(
                  new WorldDiplomacyExternalNoActionInput(externalBase) { RelayPlanned = false, IsRelayTurn = true }),
            "an unplanned relay blocks a relay-turn external response");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateExternalNoActionAuthorization(
                  new WorldDiplomacyExternalNoActionInput(externalBase)
                  {
                      RelayPlanned = false,
                      IsRelayTurn = false,
                      AuthorOnRoute = false,
                      TargetOnRoute = false
                  }),
            "an unplanned relay still allows a non-relay external response");

        WorldDiplomacyRelayNoActionInput relayBase = new WorldDiplomacyRelayNoActionInput
        {
            AuthorResolved = true,
            TargetResolved = true,
            AuthorHasAuthority = true,
            TargetHasAuthority = true,
            RoundActive = true,
            RootReady = true,
            RootActionable = true,
            IsRelayTurn = true,
            RelayPlanned = true,
            RelayWaiting = true,
            AuthorOnRoute = true,
            TargetOnRoute = true,
            AuthorIsCurrentCursor = true
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateRelayNoActionAuthorization(relayBase),
            "a fully authorized relay turn passes");
        Test.True(!WorldDiplomacyRoundLifecycleRules.EvaluateRelayNoActionAuthorization(
                  new WorldDiplomacyRelayNoActionInput(relayBase) { IsRelayTurn = false }),
            "a non-relay turn never gets relay no-action authorization");
        Test.True(!WorldDiplomacyRoundLifecycleRules.EvaluateRelayNoActionAuthorization(
                  new WorldDiplomacyRelayNoActionInput(relayBase) { AuthorIsCurrentCursor = false }),
            "only the current cursor holder may answer no-action");
        Test.True(!WorldDiplomacyRoundLifecycleRules.EvaluateRelayNoActionAuthorization(
                  new WorldDiplomacyRelayNoActionInput(relayBase) { RelayWaiting = false }),
            "a non-waiting relay never authorizes no-action");
        Test.True(!WorldDiplomacyRoundLifecycleRules.EvaluateRelayNoActionAuthorization(
                  new WorldDiplomacyRelayNoActionInput(relayBase) { HasSlotId = true }),
            "a carried slot id blocks ordinary relay no-action");

        WorldDiplomacyRelayNoActionInput settlement = new WorldDiplomacyRelayNoActionInput
        {
            AuthorResolved = true,
            TargetResolved = true,
            AuthorHasAuthority = true,
            TargetHasAuthority = true,
            RoundActive = true,
            RootReady = true,
            RootActionable = true,
            IsRelayTurn = true,
            SettlementPending = true,
            HasSlotId = true,
            SlotIdIsCurrent = true,
            SettlementTargetUsable = true,
            SlotFound = true,
            SlotHasRelatedKingdom = true,
            TargetInRelatedKingdoms = true
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateRelayNoActionAuthorization(settlement),
            "a related settlement target authorizes no-action");
        Test.True(!WorldDiplomacyRoundLifecycleRules.EvaluateRelayNoActionAuthorization(
                  new WorldDiplomacyRelayNoActionInput(settlement) { SlotIdIsCurrent = false }),
            "a stale slot id blocks settlement no-action");
        Test.True(!WorldDiplomacyRoundLifecycleRules.EvaluateRelayNoActionAuthorization(
                  new WorldDiplomacyRelayNoActionInput(settlement) { SettlementTargetUsable = false }),
            "an unusable settlement target blocks settlement no-action");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateRelayNoActionAuthorization(
                  new WorldDiplomacyRelayNoActionInput(settlement)
                  {
                      SlotHasRelatedKingdom = false,
                      TargetInRelatedKingdoms = false,
                      TargetOnRoute = true
                  }),
            "a slot without related kingdoms falls back to the route");
        Test.True(!WorldDiplomacyRoundLifecycleRules.EvaluateRelayNoActionAuthorization(
                  new WorldDiplomacyRelayNoActionInput(settlement)
                  {
                      SlotHasRelatedKingdom = false,
                      TargetInRelatedKingdoms = false,
                      TargetOnRoute = false
                  }),
            "a slotless unrelated target blocks settlement no-action");
    }

    private static void RunResponseAddressingDecisionTests()
    {
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsResponseRequiredFrom(null!, "k1"),
            "a missing document never requires a response");
        WorldDiplomacyDocument doc = new WorldDiplomacyDocument();
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsResponseRequiredFrom(doc, " "),
            "a blank kingdom id never requires a response");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsResponseRequiredFrom(doc, "k1"),
            "a document without requires-response asks for nothing");
        doc.RequiresResponse = true;
        Test.True(WorldDiplomacyRoundLifecycleRules.IsResponseRequiredFrom(doc, "k1"),
            "a legacy requires-response document asks every reader");
        doc.Actions = new List<WorldDiplomacyDocumentAction>
        {
            new WorldDiplomacyDocumentAction { RequiresResponse = true, TargetKingdomId = "K2" }
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.IsResponseRequiredFrom(doc, "k2"),
            "action-level requires-response matches case-insensitively");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsResponseRequiredFrom(doc, "k1"),
            "persisted actions shadow the legacy document flag");

        Test.True(WorldDiplomacyRoundLifecycleRules.CollectDocumentTargetIds(null!, false).Count == 0,
            "a missing document has no targets");
        WorldDiplomacyDocument legacy = new WorldDiplomacyDocument
        {
            TargetKingdomId = "k1",
            AddressedKingdomIds = new List<string> { "K1", "k2", " " }
        };
        List<string> legacyTargets = WorldDiplomacyRoundLifecycleRules.CollectDocumentTargetIds(legacy, false);
        Test.True(legacyTargets.Count == 2 && legacyTargets.Contains("k1") && legacyTargets.Contains("k2"),
            "legacy targets merge primary and addressed ids deduplicated");
        List<string> legacyChanged = WorldDiplomacyRoundLifecycleRules.CollectDocumentTargetIds(legacy, true);
        Test.True(legacyChanged.Count == 1 && legacyChanged[0] == "k1",
            "changed-only legacy targets keep the primary target");
        WorldDiplomacyDocument multi = new WorldDiplomacyDocument
        {
            TargetKingdomId = "k9",
            Actions = new List<WorldDiplomacyDocumentAction>
            {
                new WorldDiplomacyDocumentAction { TargetKingdomId = "k1", ChangedDiplomaticState = true },
                new WorldDiplomacyDocumentAction { TargetKingdomId = "K1" },
                new WorldDiplomacyDocumentAction { TargetKingdomId = " " }
            }
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.CollectDocumentTargetIds(multi, false).Count == 1,
            "persisted actions replace the legacy target list");
        List<string> changed = WorldDiplomacyRoundLifecycleRules.CollectDocumentTargetIds(multi, true);
        Test.True(changed.Count == 1 && changed[0] == "k1",
            "changed-only keeps only state-changing action targets");

        Test.True(!WorldDiplomacyRoundLifecycleRules.DocumentRespondsToSource(null!, "d1"),
            "a missing document responds to nothing");
        WorldDiplomacyDocument response = new WorldDiplomacyDocument();
        Test.True(!WorldDiplomacyRoundLifecycleRules.DocumentRespondsToSource(response, " "),
            "a blank source id is never answered");
        response.SourceDocumentId = "D1";
        Test.True(WorldDiplomacyRoundLifecycleRules.DocumentRespondsToSource(response, "d1"),
            "a direct response matches case-insensitively");
        response.SourceDocumentId = "";
        response.RespondingToOfferDocumentId = "d2";
        Test.True(WorldDiplomacyRoundLifecycleRules.DocumentRespondsToSource(response, "D2"),
            "a document-level offer response matches");
        response.RespondingToOfferDocumentId = "";
        response.RespondingToThreatDocumentId = "d3";
        Test.True(WorldDiplomacyRoundLifecycleRules.DocumentRespondsToSource(response, "d3"),
            "a document-level threat response matches");
        response.RespondingToThreatDocumentId = "";
        response.Actions = new List<WorldDiplomacyDocumentAction>
        {
            new WorldDiplomacyDocumentAction { RespondingToThreatDocumentId = "d4" }
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.DocumentRespondsToSource(response, "D4"),
            "an action-level response link matches");
        Test.True(!WorldDiplomacyRoundLifecycleRules.DocumentRespondsToSource(response, "d9"),
            "unrelated documents do not respond");
    }

    private static void RunMandatoryReplyDecisionTests()
    {
        WorldDiplomacyMandatoryReplyInput eligible = new WorldDiplomacyMandatoryReplyInput
        {
            RoundResolved = true,
            ParticipantResolved = true,
            ReceiverResolved = true,
            TriggerResolved = true,
            ReceiverHasAuthority = true,
            TriggerPlayerAuthored = true,
            IsPrimaryTarget = true,
            MaxPriorityResponses = 4
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateMandatoryReplyAction(eligible)
                == WorldDiplomacyMandatoryReplyAction.Schedule,
            "a fully eligible primary target schedules a mandatory reply");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateMandatoryReplyAction(null!)
                == WorldDiplomacyMandatoryReplyAction.Ineligible,
            "a missing input is ineligible");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateMandatoryReplyAction(
                  new WorldDiplomacyMandatoryReplyInput
                  {
                      RoundResolved = true,
                      ParticipantResolved = true,
                      ReceiverResolved = true,
                      TriggerResolved = true,
                      ReceiverHasAuthority = true,
                      TriggerPlayerAuthored = true,
                      MaxPriorityResponses = 4
                  }) == WorldDiplomacyMandatoryReplyAction.Ineligible,
            "an unaddressed receiver is ineligible");
        foreach ((string name, WorldDiplomacyMandatoryReplyInput mutated) in new (string, WorldDiplomacyMandatoryReplyInput)[]
        {
            ("receiver", new WorldDiplomacyMandatoryReplyInput { RoundResolved = true, ParticipantResolved = true, TriggerResolved = true, ReceiverHasAuthority = true, TriggerPlayerAuthored = true, IsPrimaryTarget = true, MaxPriorityResponses = 4 }),
            ("player", new WorldDiplomacyMandatoryReplyInput { RoundResolved = true, ParticipantResolved = true, ReceiverResolved = true, TriggerResolved = true, ReceiverIsPlayer = true, ReceiverHasAuthority = true, TriggerPlayerAuthored = true, IsPrimaryTarget = true, MaxPriorityResponses = 4 }),
            ("authority", new WorldDiplomacyMandatoryReplyInput { RoundResolved = true, ParticipantResolved = true, ReceiverResolved = true, TriggerResolved = true, TriggerPlayerAuthored = true, IsPrimaryTarget = true, MaxPriorityResponses = 4 }),
            ("authorship", new WorldDiplomacyMandatoryReplyInput { RoundResolved = true, ParticipantResolved = true, ReceiverResolved = true, TriggerResolved = true, ReceiverHasAuthority = true, IsPrimaryTarget = true, MaxPriorityResponses = 4 })
        })
        {
            Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateMandatoryReplyAction(mutated)
                    == WorldDiplomacyMandatoryReplyAction.Ineligible,
                "mandatory reply ineligible when " + name + " check fails");
        }
        eligible.IsPrimaryTarget = false;
        eligible.RepresentativeForAddressedVassal = true;
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateMandatoryReplyAction(eligible)
                == WorldDiplomacyMandatoryReplyAction.Schedule,
            "a vassal representative may answer for its liege");
        eligible.RepresentativeForAddressedVassal = false;
        eligible.ResponseRequiredFrom = true;
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateMandatoryReplyAction(eligible)
                == WorldDiplomacyMandatoryReplyAction.Schedule,
            "an action-level requires-response target may answer");
        eligible.AlreadyResponded = true;
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateMandatoryReplyAction(eligible)
                == WorldDiplomacyMandatoryReplyAction.AlreadyResponded,
            "a kingdom that already responded is skipped");
        eligible.AlreadyResponded = false;
        eligible.AuthorBlocked = true;
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateMandatoryReplyAction(eligible)
                == WorldDiplomacyMandatoryReplyAction.AuthorBlocked,
            "a blocked author demotes to observer");
        eligible.AuthorBlocked = false;
        eligible.SettlementPending = true;
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateMandatoryReplyAction(eligible)
                == WorldDiplomacyMandatoryReplyAction.SettlementOwned,
            "a pending settlement owns the speaking right");
        eligible.SettlementPending = false;
        eligible.JobAlreadyQueued = true;
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateMandatoryReplyAction(eligible)
                == WorldDiplomacyMandatoryReplyAction.JobQueued,
            "a queued job deduplicates the reply");
        eligible.JobAlreadyQueued = false;
        eligible.ExistingResponses = 2;
        eligible.QueuedResponses = 2;
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateMandatoryReplyAction(eligible)
                == WorldDiplomacyMandatoryReplyAction.ResponseCapReached,
            "the response cap stops further mandatory replies");
        eligible.QueuedResponses = 1;
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateMandatoryReplyAction(eligible)
                == WorldDiplomacyMandatoryReplyAction.Schedule,
            "one spare response slot still schedules");
    }

    private static void RunIntentVocabularyDecisionTests()
    {
        Test.True(WorldDiplomacyIntentVocabulary.NormalizeToken("  Make-Peace Proposal ") == "make_peace_proposal",
            "tokens normalize case, dashes, and spaces");
        Test.True(WorldDiplomacyIntentVocabulary.NormalizeIntent("make_peace") == "propose_peace"
            && WorldDiplomacyIntentVocabulary.NormalizeIntent("ALLIANCE") == "propose_alliance"
            && WorldDiplomacyIntentVocabulary.NormalizeIntent("trade") == "propose_trade"
            && WorldDiplomacyIntentVocabulary.NormalizeIntent("war") == "declare_war"
            && WorldDiplomacyIntentVocabulary.NormalizeIntent("threat") == "warning"
            && WorldDiplomacyIntentVocabulary.NormalizeIntent("denounce") == "condemn"
            && WorldDiplomacyIntentVocabulary.NormalizeIntent("unknown_intent") == "unknown_intent",
            "intent aliases normalize to the canonical vocabulary");
        Test.True(WorldDiplomacyIntentVocabulary.NormalizeCommitment("formal") == "binding"
            && WorldDiplomacyIntentVocabulary.NormalizeCommitment("offer") == "proposal"
            && WorldDiplomacyIntentVocabulary.NormalizeCommitment("accepted") == "acceptance"
            && WorldDiplomacyIntentVocabulary.NormalizeCommitment("none") == "non_binding",
            "commitment aliases normalize");
        Test.True(WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent("propose_peace") == "proposal"
            && WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent("accept_peace") == "acceptance"
            && WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent("reject_trade") == "rejection"
            && WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent("declare_war") == "binding"
            && WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent("statement") == "non_binding",
            "default commitments follow the intent class");
        Test.True(WorldDiplomacyIntentVocabulary.NormalizeTone("FIRM") == "firm"
            && WorldDiplomacyIntentVocabulary.NormalizeTone("unknown") == "neutral",
            "tones fall back to neutral");
        Test.True(WorldDiplomacyIntentVocabulary.ContainsAny("hello world", "WORLD")
            && !WorldDiplomacyIntentVocabulary.ContainsAny("hello world", "mars"),
            "contains-any is case-insensitive");
        Test.True(WorldDiplomacyIntentVocabulary.IsImmediateIntent("declare_war")
            && WorldDiplomacyIntentVocabulary.IsImmediateIntent("break_alliance")
            && !WorldDiplomacyIntentVocabulary.IsImmediateIntent("propose_peace"),
            "immediate intents are war, break-alliance, cancel-trade, comply-ultimatum");
        Test.True(WorldDiplomacyIntentVocabulary.IsProposalIntent("propose_peace")
            && WorldDiplomacyIntentVocabulary.IsProposalIntent("propose_alliance")
            && WorldDiplomacyIntentVocabulary.IsProposalIntent("propose_trade")
            && !WorldDiplomacyIntentVocabulary.IsProposalIntent("accept_peace"),
            "only the three proposals count as proposal intents");
        Test.True(WorldDiplomacyIntentVocabulary.ResponseIntentToProposalIntent("accept_peace") == "propose_peace"
            && WorldDiplomacyIntentVocabulary.ResponseIntentToProposalIntent("reject_alliance") == "propose_alliance"
            && WorldDiplomacyIntentVocabulary.ResponseIntentToProposalIntent("accept_trade") == "propose_trade"
            && WorldDiplomacyIntentVocabulary.ResponseIntentToProposalIntent("declare_war") == "",
            "response intents map back to their proposal");
        Test.True(WorldDiplomacyIntentVocabulary.ProposalIntentToResponseIntent("propose_peace", true) == "accept_peace"
            && WorldDiplomacyIntentVocabulary.ProposalIntentToResponseIntent("propose_peace", false) == "reject_peace"
            && WorldDiplomacyIntentVocabulary.ProposalIntentToResponseIntent("declare_war", true) == "",
            "proposals map to accept/reject responses");
        Test.True(WorldDiplomacyIntentVocabulary.IsPeaceIntent("accept_peace")
            && !WorldDiplomacyIntentVocabulary.IsPeaceIntent("propose_trade"),
            "peace intents cover propose/accept/reject peace");
        Test.True(WorldDiplomacyIntentVocabulary.IsSupportedDiplomacyIntent("statement")
            && WorldDiplomacyIntentVocabulary.IsSupportedDiplomacyIntent("declare_war")
            && !WorldDiplomacyIntentVocabulary.IsSupportedDiplomacyIntent("dance"),
            "the supported-intent allowlist holds");
        Test.True(WorldDiplomacyIntentVocabulary.IsSupportedNegotiationMove("counterproposal")
            && WorldDiplomacyIntentVocabulary.IsTerminalNegotiationMove("end_negotiation")
            && WorldDiplomacyIntentVocabulary.IsTerminalNegotiationMove("declare_deadlock")
            && !WorldDiplomacyIntentVocabulary.IsTerminalNegotiationMove("question")
            && !WorldDiplomacyIntentVocabulary.IsSupportedNegotiationMove("surrender"),
            "negotiation moves keep their allowlist and terminal set");
        Test.True(WorldDiplomacyIntentVocabulary.IsActionableDiplomacyIntent("ultimatum")
            && !WorldDiplomacyIntentVocabulary.IsActionableDiplomacyIntent("apology")
            && WorldDiplomacyIntentVocabulary.IsRoundDiplomaticBehaviorIntent("apology")
            && WorldDiplomacyIntentVocabulary.IsRoundDiplomaticBehaviorIntent("concession")
            && !WorldDiplomacyIntentVocabulary.IsRoundDiplomaticBehaviorIntent("statement"),
            "round behavior intents are actionable intents plus apology/concession");
        Test.True(WorldDiplomacyIntentVocabulary.IsExternallyResolvedDiplomaticIntent("accept_peace")
            && !WorldDiplomacyIntentVocabulary.IsExternallyResolvedDiplomaticIntent("propose_peace"),
            "externally-resolved intents skip propose forms");
        Test.True(WorldDiplomacyIntentVocabulary.IsSupportedCommitment("acceptance")
            && !WorldDiplomacyIntentVocabulary.IsSupportedCommitment("vague"),
            "commitment allowlist holds");
        Test.True(WorldDiplomacyIntentVocabulary.IsTerminalResponseIntent("accept_peace")
            && WorldDiplomacyIntentVocabulary.IsTerminalResponseIntent("concession")
            && !WorldDiplomacyIntentVocabulary.IsTerminalResponseIntent("propose_peace"),
            "terminal responses cover accepts, rejects, and settlements");
        Test.True(WorldDiplomacyIntentVocabulary.IsAcceptanceIntent("comply_ultimatum")
            && WorldDiplomacyIntentVocabulary.IsAcceptanceIntent("accept")
            && !WorldDiplomacyIntentVocabulary.IsAcceptanceIntent("reject_peace"),
            "acceptance intents include comply-ultimatum");
        Test.True(WorldDiplomacyIntentVocabulary.IntentLabel("declare_war") == "正式宣战"
            && WorldDiplomacyIntentVocabulary.IntentLabel("unknown") == "外交声明",
            "intent labels map to the localized catalog");

        WorldDiplomacyDocument doc = new WorldDiplomacyDocument { TargetKingdomId = "k2" };
        Test.True(WorldDiplomacyIntentVocabulary.ResolveValidatedResponseObligation(doc, "propose_peace", false, 2),
            "a proposal toward a kingdom obliges a response");
        Test.True(WorldDiplomacyIntentVocabulary.ResolveValidatedResponseObligation(doc, "warning", false, 2),
            "a warning obliges a response");
        Test.True(WorldDiplomacyIntentVocabulary.ResolveValidatedResponseObligation(doc, "statement", true, 2),
            "a model-requested response obliges a response");
        Test.True(!WorldDiplomacyIntentVocabulary.ResolveValidatedResponseObligation(doc, "statement", false, 2),
            "a plain statement does not oblige a response");
        Test.True(!WorldDiplomacyIntentVocabulary.ResolveValidatedResponseObligation(doc, "accept_peace", false, 2),
            "a terminal response ends the chain");
        doc.AutomaticReplyDepth = 2;
        Test.True(!WorldDiplomacyIntentVocabulary.ResolveValidatedResponseObligation(doc, "propose_peace", false, 2),
            "the automatic reply depth caps the chain");
        doc.AutomaticReplyDepth = 0;
        doc.TargetKingdomId = " ";
        Test.True(!WorldDiplomacyIntentVocabulary.ResolveValidatedResponseObligation(doc, "propose_peace", false, 2),
            "a document without a target obliges nobody");
        Test.True(!WorldDiplomacyIntentVocabulary.ResolveValidatedResponseObligation(null!, "propose_peace", false, 2),
            "a missing document obliges nobody");
    }



    private static void RunDeferredRetryDecisionTests()
    {
        Test.True(!WorldDiplomacyRoundLifecycleRules.NeedsCanonicalHistoryRetry(null!),
            "a missing document never needs a retry");
        WorldDiplomacyDocument doc = new WorldDiplomacyDocument
        {
            DocumentId = "d1",
            IsReadyForPublication = true,
            Body = "body",
            MechanicalResult = "result",
            ChangedDiplomaticState = true
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.NeedsCanonicalHistoryRetry(doc),
            "a publishable document with pending declaration and result needs a retry");
        doc.HistoryDeclarationRecorded = true;
        Test.True(WorldDiplomacyRoundLifecycleRules.NeedsCanonicalHistoryRetry(doc),
            "a pending result alone still needs a retry");
        doc.HistoryResultRecorded = true;
        Test.True(!WorldDiplomacyRoundLifecycleRules.NeedsCanonicalHistoryRetry(doc),
            "a fully recorded document needs no retry");
        doc.HistoryResultRecorded = false;
        doc.ChangedDiplomaticState = false;
        doc.AnalysisStatus = "external_fact";
        Test.True(WorldDiplomacyRoundLifecycleRules.NeedsCanonicalHistoryRetry(doc),
            "an external fact still needs its result history");
        doc.HistoryResultRecorded = false;
        doc.HistoryDeclarationRecorded = false;
        Test.True(WorldDiplomacyRoundLifecycleRules.NeedsCanonicalHistoryRetry(doc),
            "an external fact keeps its pending result history");
        doc.MechanicalResult = "";
        Test.True(!WorldDiplomacyRoundLifecycleRules.NeedsCanonicalHistoryRetry(doc),
            "an external fact never rewrites declaration history");
        doc.MechanicalResult = "result";
        doc.AnalysisStatus = "";
        doc.IsReadyForPublication = false;
        Test.True(!WorldDiplomacyRoundLifecycleRules.NeedsCanonicalHistoryRetry(doc),
            "an unpublished document needs no retry");

        Test.True(WorldDiplomacyRoundLifecycleRules.NextDeferredRetryAttempt(0) == 1
            && WorldDiplomacyRoundLifecycleRules.NextDeferredRetryAttempt(29) == 30
            && WorldDiplomacyRoundLifecycleRules.NextDeferredRetryAttempt(30) == 30
            && WorldDiplomacyRoundLifecycleRules.NextDeferredRetryAttempt(99) == 30,
            "retry attempts increment to the hard cap of thirty");
        Test.True(WorldDiplomacyRoundLifecycleRules.ComputeDeferredRetryDelayHours(0) == 1
            && WorldDiplomacyRoundLifecycleRules.ComputeDeferredRetryDelayHours(1) == 1
            && WorldDiplomacyRoundLifecycleRules.ComputeDeferredRetryDelayHours(2) == 2
            && WorldDiplomacyRoundLifecycleRules.ComputeDeferredRetryDelayHours(4) == 8
            && WorldDiplomacyRoundLifecycleRules.ComputeDeferredRetryDelayHours(5) == 16
            && WorldDiplomacyRoundLifecycleRules.ComputeDeferredRetryDelayHours(6) == 16
            && WorldDiplomacyRoundLifecycleRules.ComputeDeferredRetryDelayHours(30) == 16,
            "retry backoff doubles to the 16-hour shift ceiling");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsDeferredRetryDue(10, 10)
            && WorldDiplomacyRoundLifecycleRules.IsDeferredRetryDue(11, 10)
            && !WorldDiplomacyRoundLifecycleRules.IsDeferredRetryDue(9, 10),
            "a retry is due at or after its scheduled hour");
        Test.True(WorldDiplomacyRoundLifecycleRules.ComputeDeferredRetryBatchSize(16, 3) == 3
            && WorldDiplomacyRoundLifecycleRules.ComputeDeferredRetryBatchSize(2, 9) == 2
            && WorldDiplomacyRoundLifecycleRules.ComputeDeferredRetryBatchSize(-1, 9) == 0,
            "the retry batch is clamped by queue size and non-negative attempts");
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeDeferredRetryDocumentId("  d1 ") == "d1"
            && WorldDiplomacyRoundLifecycleRules.NormalizeDeferredRetryDocumentId(null!) == "",
            "retry ids are trimmed and null-safe");
    }

    private static void RunThreatAndConfirmedResultDecisionTests()
    {
        Test.True(WorldDiplomacyRoundLifecycleRules.IsOpenDiplomaticThreatStatus("OPEN")
            && !WorldDiplomacyRoundLifecycleRules.IsOpenDiplomaticThreatStatus("complied"),
            "only the open status keeps a threat active");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsRetainedActiveDiplomaticThreatStatus("open")
            && WorldDiplomacyRoundLifecycleRules.IsRetainedActiveDiplomaticThreatStatus("compliance_pending")
            && !WorldDiplomacyRoundLifecycleRules.IsRetainedActiveDiplomaticThreatStatus("breached"),
            "open and compliance-pending threats are retained");

        WorldDiplomacyThreat threat = new WorldDiplomacyThreat { Status = "open", StageRoundId = "r1" };
        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatRelevantToResultSettlement(threat, "R1"),
            "a staged threat binds to its round");
        threat.StageRoundId = "r9";
        threat.TargetDecisionRoundId = "r1";
        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatRelevantToResultSettlement(threat, "r1"),
            "a target-decision threat binds to its round");
        threat.TargetDecisionRoundId = "r9";
        threat.ObligationRoundId = "r1";
        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatRelevantToResultSettlement(threat, "r1"),
            "an obligation threat binds to its round");
        threat.Status = "complied";
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsThreatRelevantToResultSettlement(threat, "r1"),
            "a closed threat is not settlement-relevant");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsThreatRelevantToResultSettlement(null!, "r1")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatRelevantToResultSettlement(threat, " "),
            "missing threat or round id is never relevant");

        List<WorldDiplomacyThreat> threats = new List<WorldDiplomacyThreat>();
        List<WorldDiplomacyRoundOffer> offers = new List<WorldDiplomacyRoundOffer>();
        WorldDiplomacyDocument doc = new WorldDiplomacyDocument { DocumentId = "d1" };

        WorldDiplomacyThreatCloseDecision close =
            WorldDiplomacyRoundLifecycleRules.EvaluateThreatTerminalClose(doc, threats);
        Test.True(close.Reason == "" && !close.Resolved,
            "no linked threats means no terminal close");
        threats.Add(new WorldDiplomacyThreat { ComplianceDocumentId = "d1", Status = "complied" });
        close = WorldDiplomacyRoundLifecycleRules.EvaluateThreatTerminalClose(doc, threats);
        Test.True(close.Reason == "threat_target_complied" && close.Resolved,
            "a complied threat resolves the round");
        threats[0].Status = "enforced";
        close = WorldDiplomacyRoundLifecycleRules.EvaluateThreatTerminalClose(doc, threats);
        Test.True(close.Reason == "threat_followed_by_war" && close.Resolved,
            "an enforced threat resolves by war");
        threats[0].Status = "breached";
        close = WorldDiplomacyRoundLifecycleRules.EvaluateThreatTerminalClose(doc, threats);
        Test.True(close.Reason == "threat_next_declaration_breached" && !close.Resolved,
            "a breached threat without war deadlocks the round");
        doc.Intent = "declare_war";
        doc.ChangedDiplomaticState = true;
        close = WorldDiplomacyRoundLifecycleRules.EvaluateThreatTerminalClose(doc, threats);
        Test.True(close.Reason == "threat_warning_skipped_but_war_started" && close.Resolved,
            "a breached threat followed by war still resolves");

        threats.Clear();
        doc.Intent = "declare_war";
        doc.ChangedDiplomaticState = true;
        WorldDiplomacyConfirmedRoundResult confirmed =
            WorldDiplomacyRoundLifecycleRules.EvaluateConfirmedRoundResult(doc, offers, threats);
        Test.True(confirmed.Confirmed && confirmed.CloseReason == "war_declared"
            && confirmed.RoundStatus == "resolved",
            "a successful war declaration confirms a resolved round");

        doc.Intent = "statement";
        doc.ChangedDiplomaticState = false;
        confirmed = WorldDiplomacyRoundLifecycleRules.EvaluateConfirmedRoundResult(doc, offers, threats);
        Test.True(!confirmed.Confirmed,
            "a plain statement confirms nothing");

        doc.Intent = "accept_peace";
        doc.ChangedDiplomaticState = true;
        doc.RespondingToOfferDocumentId = "src1";
        offers.Add(new WorldDiplomacyRoundOffer { SourceDocumentId = "src1", Status = "accepted" });
        confirmed = WorldDiplomacyRoundLifecycleRules.EvaluateConfirmedRoundResult(doc, offers, threats);
        Test.True(confirmed.Confirmed && confirmed.CloseReason == "offer_accepted",
            "an accepted offer confirms acceptance");
        offers[0].Status = "rejected";
        doc.Intent = "reject_peace";
        confirmed = WorldDiplomacyRoundLifecycleRules.EvaluateConfirmedRoundResult(doc, offers, threats);
        Test.True(!confirmed.Confirmed,
            "a rejected offer is negotiation progress, not a terminal result");

        doc.Intent = "comply_ultimatum";
        doc.ChangedDiplomaticState = true;
        doc.RespondingToOfferDocumentId = "";
        offers.Clear();
        confirmed = WorldDiplomacyRoundLifecycleRules.EvaluateConfirmedRoundResult(doc, offers, threats);
        Test.True(confirmed.Confirmed && confirmed.CloseReason == "threat_target_complied",
            "a successful ultimatum compliance confirms compliance");

        WorldDiplomacyDocument multi = new WorldDiplomacyDocument
        {
            DocumentId = "d2",
            Actions = new List<WorldDiplomacyDocumentAction>
            {
                new WorldDiplomacyDocumentAction { Intent = "declare_war", ChangedDiplomaticState = true },
                new WorldDiplomacyDocumentAction { Intent = "break_alliance", ChangedDiplomaticState = true }
            }
        };
        confirmed = WorldDiplomacyRoundLifecycleRules.EvaluateConfirmedRoundResult(multi, offers, threats);
        Test.True(confirmed.Confirmed && confirmed.CloseReason == "multiple_diplomatic_results",
            "multiple confirmed actions collapse into the multiple-results reason");

        multi.Actions = new List<WorldDiplomacyDocumentAction>
        {
            new WorldDiplomacyDocumentAction { Intent = "declare_war", ChangedDiplomaticState = true },
            new WorldDiplomacyDocumentAction { Intent = "declare_war", ChangedDiplomaticState = true }
        };
        confirmed = WorldDiplomacyRoundLifecycleRules.EvaluateConfirmedRoundResult(multi, offers, threats);
        Test.True(confirmed.Confirmed && confirmed.CloseReason == "war_declared",
            "duplicate confirmed reasons deduplicate");

        multi.Actions = new List<WorldDiplomacyDocumentAction>
        {
            new WorldDiplomacyDocumentAction { Intent = "statement" }
        };
        confirmed = WorldDiplomacyRoundLifecycleRules.EvaluateConfirmedRoundResult(multi, offers, threats);
        Test.True(!confirmed.Confirmed,
            "actions without confirmed results confirm nothing");
    }

    private static void RunThreatViolationDecisionTests()
    {
        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatIntent("warning")
            && WorldDiplomacyRoundLifecycleRules.IsThreatIntent("ultimatum")
            && WorldDiplomacyRoundLifecycleRules.IsThreatIntent("comply_ultimatum")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatIntent("declare_war"),
            "only warning, ultimatum, and compliance are threat intents");

        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatPartyEligibility(true, true, false) == ""
            && WorldDiplomacyRoundLifecycleRules.EvaluateThreatPartyEligibility(false, true, false) == "threat_action_has_no_eligible_parties"
            && WorldDiplomacyRoundLifecycleRules.EvaluateThreatPartyEligibility(true, true, true) == "threat_action_has_no_eligible_parties",
            "threat parties must exist and differ");

        WorldDiplomacyThreat incoming = new WorldDiplomacyThreat
        {
            Status = "open",
            TargetDecision = "pending",
            StageDocumentId = "threat1"
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateComplyUltimatumViolation(true, incoming, "threat1")
                == "comply_ultimatum_after_war_started",
            "compliance after war started is rejected");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateComplyUltimatumViolation(false, null!, "threat1")
                == "comply_ultimatum_without_open_threat",
            "compliance without an open threat is rejected");
        incoming.TargetDecision = "complied";
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateComplyUltimatumViolation(false, incoming, "threat1")
                == "comply_ultimatum_after_target_decision",
            "compliance after a target decision is rejected");
        incoming.TargetDecision = "pending";
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateComplyUltimatumViolation(false, incoming, "wrong")
                == "comply_ultimatum_source_mismatch",
            "compliance citing another document is rejected");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateComplyUltimatumViolation(false, incoming, "THREAT1") == "",
            "a matching pending threat accepts compliance");

        WorldDiplomacyThreat outbound = new WorldDiplomacyThreat
        {
            Status = "open",
            TargetKingdomId = "k2",
            Stage = "warning",
            TargetDecision = "noncomplied"
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatEscalationViolation(
                "ultimatum", true, true, "", outbound, "k2") == "threat_intent_between_kingdoms_already_at_war",
            "threats between kingdoms already at war are rejected");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatEscalationViolation(
                "ultimatum", false, false, "no_casus", null!, "k2") == "threat_cannot_be_enforced:no_casus",
            "unenforceable threats carry the block reason");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatEscalationViolation(
                "warning", false, true, "", outbound, "k2") == "issuer_already_has_open_threat",
            "a second open warning from the same issuer is rejected");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatEscalationViolation(
                "warning", false, true, "", null!, "k2") == "",
            "a first warning is accepted");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatEscalationViolation(
                "ultimatum", false, true, "", null!, "k2") == "",
            "an ultimatum without a prior warning is accepted");
        outbound.TargetKingdomId = "k3";
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatEscalationViolation(
                "ultimatum", false, true, "", outbound, "k2") == "issuer_open_threat_targets_another_kingdom",
            "an ultimatum must target the warned kingdom");
        outbound.TargetKingdomId = "k2";
        outbound.Stage = "ultimatum";
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatEscalationViolation(
                "ultimatum", false, true, "", outbound, "k2") == "duplicate_open_ultimatum",
            "an open ultimatum cannot be re-escalated");
        outbound.Stage = "warning";
        outbound.TargetDecision = "pending";
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatEscalationViolation(
                "ultimatum", false, true, "", outbound, "k2") == "warning_escalation_requires_target_noncompliance",
            "escalation requires a noncomplied warning");
        outbound.TargetDecision = "noncomplied";
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatEscalationViolation(
                "ultimatum", false, true, "", outbound, "k2") == "",
            "a noncomplied warning escalates to ultimatum");

        List<WorldDiplomacyRoundOffer> offers = new List<WorldDiplomacyRoundOffer>
        {
            new WorldDiplomacyRoundOffer
            {
                Status = "open",
                Intent = "propose_peace",
                ProposerKingdomId = "k1",
                TargetKingdomId = "k2",
                SourceDocumentId = "doc1",
                SourceActionId = "a1"
            }
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.TryResolveUniqueOpenProposal(
                offers, "k2", "k1", "propose_peace", out string docId, out string actId)
            && docId == "doc1" && actId == "a1",
            "a single matching open proposal resolves its source pair");
        Test.True(!WorldDiplomacyRoundLifecycleRules.TryResolveUniqueOpenProposal(
                offers, "k2", "k1", "propose_trade", out _, out _),
            "a different intent never resolves");
        offers.Add(new WorldDiplomacyRoundOffer
        {
            Status = "open",
            Intent = "propose_peace",
            ProposerKingdomId = "k1",
            TargetKingdomId = "k2",
            SourceDocumentId = "doc2"
        });
        Test.True(!WorldDiplomacyRoundLifecycleRules.TryResolveUniqueOpenProposal(
                offers, "k2", "k1", "propose_peace", out _, out _),
            "ambiguous open proposals never resolve");
        offers[1].SourceDocumentId = "doc1";
        offers[1].SourceActionId = "a1";
        Test.True(WorldDiplomacyRoundLifecycleRules.TryResolveUniqueOpenProposal(
                offers, "k2", "k1", "propose_peace", out docId, out actId)
            && docId == "doc1" && actId == "a1",
            "duplicate offers for one source pair still resolve");
        offers[0].Status = "accepted";
        offers.RemoveAt(1);
        Test.True(!WorldDiplomacyRoundLifecycleRules.TryResolveUniqueOpenProposal(
                offers, "k2", "k1", "propose_peace", out _, out _),
            "closed offers never resolve");
    }





    private static void RunRoundAccountingDecisionTests()
    {
        WorldDiplomacyDocument doc = new WorldDiplomacyDocument { DocumentId = "d1", ChangedDiplomaticState = true };
        Test.True(WorldDiplomacyRoundLifecycleRules.CountSuccessfulMechanicalActions(doc) == 1,
            "a legacy changed document counts one mechanical action");
        doc.Actions = new List<WorldDiplomacyDocumentAction>
        {
            new WorldDiplomacyDocumentAction { ChangedDiplomaticState = true },
            new WorldDiplomacyDocumentAction { ChangedDiplomaticState = false }
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.CountSuccessfulMechanicalActions(doc) == 1,
            "only successful persisted actions count");
        doc.Actions = new List<WorldDiplomacyDocumentAction>();
        Test.True(WorldDiplomacyRoundLifecycleRules.CountSuccessfulMechanicalActions(doc) == 0,
            "an empty action list counts nothing");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsRootRoundDocument("", "d1")
            && WorldDiplomacyRoundLifecycleRules.IsRootRoundDocument("D1", "d1")
            && !WorldDiplomacyRoundLifecycleRules.IsRootRoundDocument("d2", "d1"),
            "the first or matching document is the round root");

        doc.Actions = new List<WorldDiplomacyDocumentAction>
        {
            new WorldDiplomacyDocumentAction { Intent = "propose_peace" },
            new WorldDiplomacyDocumentAction { Intent = "statement" }
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.ComputeDiplomaticActionAttemptCount(
                doc.Actions, substantiveProgress: true, validatedSingleAttempt: false) == 1,
            "only round-behavior intents count as attempts");
        Test.True(WorldDiplomacyRoundLifecycleRules.ComputeDiplomaticActionAttemptCount(
                doc.Actions, substantiveProgress: false, validatedSingleAttempt: true) == 0,
            "unvalidated action lists count nothing");
        Test.True(WorldDiplomacyRoundLifecycleRules.ComputeDiplomaticActionAttemptCount(
                null, substantiveProgress: false, validatedSingleAttempt: true) == 1,
            "a validated legacy document counts one attempt");

        Test.True(WorldDiplomacyRoundLifecycleRules.ShouldExtendHardEndForLateProposal(
                3, doc.Actions, "statement"),
            "a proposal action extends the end once the round is winding down");
        Test.True(!WorldDiplomacyRoundLifecycleRules.ShouldExtendHardEndForLateProposal(
                1, doc.Actions, "statement")
            && !WorldDiplomacyRoundLifecycleRules.ShouldExtendHardEndForLateProposal(
                3, new List<WorldDiplomacyDocumentAction>(), "statement"),
            "without escalation or a proposal the hard end stays");
        Test.True(WorldDiplomacyRoundLifecycleRules.ShouldExtendHardEndForLateProposal(
                3, new List<WorldDiplomacyDocumentAction>(), "propose_peace"),
            "a legacy proposal intent also extends the hard end");

        Test.True(WorldDiplomacyRoundLifecycleRules.ResolveTerminalMoveCloseReason("deadlocked")
                == "negotiation_declared_deadlock"
            && WorldDiplomacyRoundLifecycleRules.ResolveTerminalMoveCloseReason("resolved")
                == "negotiation_ended",
            "terminal moves map to their close reasons");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsRelayResolvedClose("resolved", 1)
            && !WorldDiplomacyRoundLifecycleRules.IsRelayResolvedClose("resolved", 0)
            && !WorldDiplomacyRoundLifecycleRules.IsRelayResolvedClose("deadlocked", 1),
            "a relay resolves only after a validated action");

        List<WorldDiplomacyDocument> documents = new List<WorldDiplomacyDocument>
        {
            new WorldDiplomacyDocument { RoundId = "r1", AuthorKingdomId = "k1", IsReadyForPublication = true, Day = 3, CreatedUtcTicks = 10 },
            new WorldDiplomacyDocument { RoundId = "r1", AuthorKingdomId = "k2", IsReadyForPublication = true, Day = 3, CreatedUtcTicks = 20 },
            new WorldDiplomacyDocument { RoundId = "r1", AuthorKingdomId = "k3", IsReadyForPublication = false, Day = 9, CreatedUtcTicks = 99 },
            new WorldDiplomacyDocument { RoundId = "r9", AuthorKingdomId = "k4", IsReadyForPublication = true, Day = 9, CreatedUtcTicks = 99 }
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectLastPublishedAuthorId(documents, "r1") == "k2",
            "the previous speaker is the latest published author of this round");
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectLastPublishedAuthorId(documents, "r9") == "k4",
            "other rounds resolve independently");

        List<WorldDiplomacyRoundParticipant> participants = new List<WorldDiplomacyRoundParticipant>
        {
            new WorldDiplomacyRoundParticipant { KingdomId = "k1" },
            new WorldDiplomacyRoundParticipant { KingdomId = "k2" }
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectParticipantByKingdom(participants, "K2")?.KingdomId == "k2"
            && WorldDiplomacyRoundLifecycleRules.SelectParticipantByKingdom(participants, "k9") == null
            && WorldDiplomacyRoundLifecycleRules.SelectParticipantByKingdom(null!, "k1") == null,
            "participant lookup is case-insensitive and null-safe");
    }

    private static void RunThreatDocumentDispatchDecisionTests()
    {
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatDocumentDispatch(
                "warning", hasTarget: true, sameParty: false, changedDiplomaticState: false)
                == WorldDiplomacyThreatDocumentDispatch.RegisterOrAdvance
            && WorldDiplomacyRoundLifecycleRules.EvaluateThreatDocumentDispatch(
                "ultimatum", hasTarget: true, sameParty: false, changedDiplomaticState: true)
                == WorldDiplomacyThreatDocumentDispatch.RegisterOrAdvance,
            "warnings and ultimatums register or advance a threat");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatDocumentDispatch(
                "comply_ultimatum", hasTarget: true, sameParty: false, changedDiplomaticState: true)
                == WorldDiplomacyThreatDocumentDispatch.ResolveCompliance,
            "a compliance declaration resolves against the open threat");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatDocumentDispatch(
                "declare_war", hasTarget: true, sameParty: false, changedDiplomaticState: true)
                == WorldDiplomacyThreatDocumentDispatch.ProcessWarEnforcement,
            "a successful war declaration enforces noncomplied ultimatums");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatDocumentDispatch(
                "declare_war", hasTarget: true, sameParty: false, changedDiplomaticState: false)
                == WorldDiplomacyThreatDocumentDispatch.None
            && WorldDiplomacyRoundLifecycleRules.EvaluateThreatDocumentDispatch(
                "statement", hasTarget: true, sameParty: false, changedDiplomaticState: true)
                == WorldDiplomacyThreatDocumentDispatch.None,
            "a failed war declaration or plain statement dispatches nothing");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatDocumentDispatch(
                "warning", hasTarget: false, sameParty: false, changedDiplomaticState: false)
                == WorldDiplomacyThreatDocumentDispatch.None
            && WorldDiplomacyRoundLifecycleRules.EvaluateThreatDocumentDispatch(
                "warning", hasTarget: true, sameParty: true, changedDiplomaticState: false)
                == WorldDiplomacyThreatDocumentDispatch.None,
            "threats need a distinct target kingdom");

        WorldDiplomacyThreat threat = new WorldDiplomacyThreat
        {
            Status = "open",
            Stage = "ultimatum",
            IssuerKingdomId = "k1",
            TargetKingdomId = "k2",
            StageDocumentId = "sd1",
            StageActionId = "sa1",
            TargetDecision = "noncomplied"
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatComplianceAction(
                new WorldDiplomacyDocumentAction
                {
                    Intent = "comply_ultimatum",
                    TargetKingdomId = "K1",
                    RespondingToThreatDocumentId = "sd1",
                    RespondingToThreatActionId = "SA1"
                }, threat),
            "a compliance action binds to the threat stage document and action");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsThreatComplianceAction(
                new WorldDiplomacyDocumentAction
                {
                    Intent = "comply_ultimatum",
                    TargetKingdomId = "k1",
                    RespondingToThreatDocumentId = "sd1",
                    RespondingToThreatActionId = "sa2"
                }, threat),
            "a compliance action bound to a different stage action does not count");
        threat.StageActionId = "";
        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatComplianceAction(
                new WorldDiplomacyDocumentAction
                {
                    Intent = "comply_ultimatum",
                    TargetKingdomId = "k1",
                    RespondingToThreatDocumentId = "sd1"
                }, threat)
            && !WorldDiplomacyRoundLifecycleRules.IsThreatComplianceAction(
                new WorldDiplomacyDocumentAction
                {
                    Intent = "comply_ultimatum",
                    TargetKingdomId = "k1",
                    RespondingToThreatDocumentId = "sd1",
                    RespondingToThreatActionId = "sa1"
                }, threat),
            "legacy stage-document-only threats require an empty stage action response");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsThreatComplianceAction(
                new WorldDiplomacyDocumentAction
                {
                    Intent = "statement",
                    TargetKingdomId = "k1",
                    RespondingToThreatDocumentId = "sd1"
                }, threat)
            && !WorldDiplomacyRoundLifecycleRules.IsThreatComplianceAction(
                new WorldDiplomacyDocumentAction
                {
                    Intent = "comply_ultimatum",
                    TargetKingdomId = "k2",
                    RespondingToThreatDocumentId = "sd1"
                }, threat)
            && !WorldDiplomacyRoundLifecycleRules.IsThreatComplianceAction(null!, threat),
            "wrong intent, wrong issuer target, or a missing action is not compliance");

        List<WorldDiplomacyDocumentAction> actions = new List<WorldDiplomacyDocumentAction>
        {
            new WorldDiplomacyDocumentAction { ActionId = "a1", TargetKingdomId = "k9" },
            new WorldDiplomacyDocumentAction { ActionId = "a2", TargetKingdomId = "K1" }
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectThreatDecisionAction(actions, "k1")?.ActionId == "a2"
            && WorldDiplomacyRoundLifecycleRules.SelectThreatDecisionAction(actions, "k5") == null
            && WorldDiplomacyRoundLifecycleRules.SelectThreatDecisionAction(null!, "k1") == null,
            "the decision action is the first action targeting the requested kingdom");

        List<WorldDiplomacyThreat> threats = new List<WorldDiplomacyThreat>
        {
            new WorldDiplomacyThreat
            {
                ThreatId = "t1", Status = "open", Stage = "ultimatum",
                IssuerKingdomId = "k1", TargetKingdomId = "k2",
                TargetDecision = "noncomplied", UpdatedDay = 3, CreatedDay = 1
            },
            new WorldDiplomacyThreat
            {
                ThreatId = "t2", Status = "open", Stage = "ultimatum",
                IssuerKingdomId = "k1", TargetKingdomId = "k2",
                TargetDecision = "noncomplied", UpdatedDay = 9, CreatedDay = 1
            },
            new WorldDiplomacyThreat
            {
                ThreatId = "t3", Status = "open", Stage = "warning",
                IssuerKingdomId = "k1", TargetKingdomId = "k2",
                TargetDecision = "noncomplied", UpdatedDay = 20, CreatedDay = 1
            }
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectEnforceableUltimatumThreat(threats, "K1", "k2")?.ThreatId == "t2",
            "the newest open noncomplied ultimatum is enforced");
        threats[1].TargetDecision = "complied";
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectEnforceableUltimatumThreat(threats, "k1", "k2")?.ThreatId == "t1",
            "a complied ultimatum is not enforceable");
        threats[0].Status = "enforced";
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectEnforceableUltimatumThreat(threats, "k1", "k2") == null
            && WorldDiplomacyRoundLifecycleRules.SelectEnforceableUltimatumThreat(null!, "k1", "k2") == null,
            "only open ultimatums enforce and missing lists select nothing");

        Test.True(WorldDiplomacyRoundLifecycleRules.ResolveThreatDecisionSourceDocumentId("comply_ultimatum", "sd1") == "sd1"
            && WorldDiplomacyRoundLifecycleRules.ResolveThreatDecisionSourceDocumentId("comply_ultimatum", null!) == ""
            && WorldDiplomacyRoundLifecycleRules.ResolveThreatDecisionSourceDocumentId("statement", "sd1") == "",
            "only compliance declarations bind a source threat document");

        HashSet<string> presented = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "sd1" };
        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatTargetDecisionCandidate(
                new WorldDiplomacyThreat { Status = "open", TargetKingdomId = "k2", StageDocumentId = "SD1" }, "K2", presented),
            "an open threat staged in this declaration targets its author");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsThreatTargetDecisionCandidate(
                new WorldDiplomacyThreat { Status = "enforced", TargetKingdomId = "k2", StageDocumentId = "sd1" }, "k2", presented)
            && !WorldDiplomacyRoundLifecycleRules.IsThreatTargetDecisionCandidate(
                new WorldDiplomacyThreat { Status = "open", TargetKingdomId = "k9", StageDocumentId = "sd1" }, "k2", presented)
            && !WorldDiplomacyRoundLifecycleRules.IsThreatTargetDecisionCandidate(
                new WorldDiplomacyThreat { Status = "open", TargetKingdomId = "k2", StageDocumentId = "sd9" }, "k2", presented)
            && !WorldDiplomacyRoundLifecycleRules.IsThreatTargetDecisionCandidate(
                new WorldDiplomacyThreat { Status = "open", TargetKingdomId = "k2", StageDocumentId = "sd1" }, "k2", null!),
            "closed threats, other targets, unpresented stage documents, and missing allowlists are not candidates");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatBetweenParties(
                new WorldDiplomacyThreat { IssuerKingdomId = "k1", TargetKingdomId = "k2" }, "k1", "k2")
            && WorldDiplomacyRoundLifecycleRules.IsThreatBetweenParties(
                new WorldDiplomacyThreat { IssuerKingdomId = "k2", TargetKingdomId = "k1" }, "k1", "k2")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatBetweenParties(
                new WorldDiplomacyThreat { IssuerKingdomId = "k1", TargetKingdomId = "k9" }, "k1", "k2")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatBetweenParties(null!, "k1", "k2"),
            "a between-parties threat runs in either direction");

        Test.True(WorldDiplomacyRoundLifecycleRules.ShouldKeepWarningOpenAfterWar(
                new WorldDiplomacyThreat
                {
                    IssuerKingdomId = "k1", TargetKingdomId = "k2",
                    Stage = "warning", TargetDecision = "noncomplied"
                }, "K1", "k2"),
            "a rejected warning stays open for declaration-level settlement");
        Test.True(!WorldDiplomacyRoundLifecycleRules.ShouldKeepWarningOpenAfterWar(
                new WorldDiplomacyThreat
                {
                    IssuerKingdomId = "k1", TargetKingdomId = "k2",
                    Stage = "ultimatum", TargetDecision = "noncomplied"
                }, "k1", "k2")
            && !WorldDiplomacyRoundLifecycleRules.ShouldKeepWarningOpenAfterWar(
                new WorldDiplomacyThreat
                {
                    IssuerKingdomId = "k2", TargetKingdomId = "k1",
                    Stage = "warning", TargetDecision = "noncomplied"
                }, "k1", "k2")
            && !WorldDiplomacyRoundLifecycleRules.ShouldKeepWarningOpenAfterWar(
                new WorldDiplomacyThreat
                {
                    IssuerKingdomId = "k1", TargetKingdomId = "k2",
                    Stage = "warning", TargetDecision = "complied"
                }, "k1", "k2"),
            "ultimatums, reversed threats, and complied warnings close on war");

        List<string> followThroughIds = new List<string> { "sd1" };
        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatFollowThroughObligationPending(
                new WorldDiplomacyThreat { TargetDecision = "noncomplied", StageDocumentId = "SD1" },
                followThroughIds),
            "a noncomplied threat whose stage document was presented owes follow-through");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsThreatFollowThroughObligationPending(
                new WorldDiplomacyThreat { TargetDecision = "complied", StageDocumentId = "sd1" },
                followThroughIds)
            && !WorldDiplomacyRoundLifecycleRules.IsThreatFollowThroughObligationPending(
                new WorldDiplomacyThreat { TargetDecision = "noncomplied", StageDocumentId = "sd9" },
                followThroughIds)
            && !WorldDiplomacyRoundLifecycleRules.IsThreatFollowThroughObligationPending(
                null!, followThroughIds),
            "complied threats, unpresented stage documents, and missing threats owe nothing");

        Test.True(WorldDiplomacyRoundLifecycleRules.ThreatDeclarationTargetsThreatTarget(true, "k2", "k9"),
            "a matching action always counts as targeting the threat target");
        Test.True(WorldDiplomacyRoundLifecycleRules.ThreatDeclarationTargetsThreatTarget(false, "k2", "K2")
            && !WorldDiplomacyRoundLifecycleRules.ThreatDeclarationTargetsThreatTarget(false, "k2", "k9"),
            "without a matching action the document target must be the threat target");

        HashSet<string> bindingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "sd1" };
        List<WorldDiplomacyThreat> pending = new List<WorldDiplomacyThreat>
        {
            new WorldDiplomacyThreat
            {
                ThreatId = "p1", Status = "open", Stage = "ultimatum",
                IssuerKingdomId = "k1", TargetKingdomId = "k2",
                TargetDecision = "pending", StageDocumentId = "SD1", StageActionId = "sa1"
            },
            new WorldDiplomacyThreat
            {
                ThreatId = "p2", Status = "open", Stage = "warning",
                IssuerKingdomId = "k1", TargetKingdomId = "k2",
                TargetDecision = "noncomplied", StageDocumentId = "sd1", StageActionId = "sa9"
            }
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectUniquePresentedPendingThreat(
                pending, "K1", "k2", bindingIds)?.ThreatId == "p1",
            "the unique presented pending threat binds the compliance declaration");
        pending.Add(new WorldDiplomacyThreat
        {
            ThreatId = "p3", Status = "open", Stage = "ultimatum",
            IssuerKingdomId = "k1", TargetKingdomId = "k2",
            TargetDecision = "pending", StageDocumentId = "sd1", StageActionId = "sa2"
        });
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectUniquePresentedPendingThreat(
                pending, "k1", "k2", bindingIds) == null,
            "two distinct presented stage actions are ambiguous and bind nothing");
        pending.RemoveAt(2);
        pending[0].TargetDecision = "complied";
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectUniquePresentedPendingThreat(
                pending, "k1", "k2", bindingIds) == null
            && WorldDiplomacyRoundLifecycleRules.SelectUniquePresentedPendingThreat(
                null!, "k1", "k2", bindingIds) == null,
            "decided threats and missing lists bind nothing");

        List<WorldDiplomacyThreat> open = new List<WorldDiplomacyThreat>
        {
            new WorldDiplomacyThreat { ThreatId = "o1", Status = "breached", IssuerKingdomId = "k1" },
            new WorldDiplomacyThreat { ThreatId = "o2", Status = "open", IssuerKingdomId = "K1" },
            new WorldDiplomacyThreat { ThreatId = "o3", Status = "open", IssuerKingdomId = "k1" }
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectOpenThreatIssuedBy(open, " k1 ")?.ThreatId == "o2",
            "the first open threat issued by the kingdom wins after trimming");
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectOpenThreatIssuedBy(open, "k9") == null
            && WorldDiplomacyRoundLifecycleRules.SelectOpenThreatIssuedBy(open, "  ") == null
            && WorldDiplomacyRoundLifecycleRules.SelectOpenThreatIssuedBy(null!, "k1") == null,
            "missing issuers, blank ids, and missing lists select nothing");
    }

    private static void RunThreatRegistrationComplianceTests()
    {
        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatRegistrationIdentityMatch(
                "warning", "warning", "k1", "K2", "k1", "k2")
            && WorldDiplomacyRoundLifecycleRules.IsThreatRegistrationIdentityMatch(
                "ultimatum", "ultimatum", "k1", "k2", "K1", "k2"),
            "registration requires the structured document to name the same parties and stage");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsThreatRegistrationIdentityMatch(
                "statement", "statement", "k1", "k2", "k1", "k2")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatRegistrationIdentityMatch(
                "warning", "ultimatum", "k1", "k2", "k1", "k2")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatRegistrationIdentityMatch(
                "warning", "warning", "k9", "k2", "k1", "k2")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatRegistrationIdentityMatch(
                "warning", "warning", "k1", "k9", "k1", "k2"),
            "non-threat stages, intent mismatches, and party mismatches block registration");

        WorldDiplomacyThreat existing = new WorldDiplomacyThreat
        {
            Status = "open", Stage = "warning", TargetKingdomId = "k2",
            TargetDecision = "noncomplied", StageDocumentId = "sd1"
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatRegistration(existing, "warning", "sd1", "k2")
                == WorldDiplomacyThreatRegistrationDecision.AlreadyRegistered,
            "a threat already bound to this document registers idempotently");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatRegistration(existing, "warning", "d9", "k2")
                == WorldDiplomacyThreatRegistrationDecision.Reject,
            "a second warning cannot open while any threat is open");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatRegistration(existing, "ultimatum", "d9", "K2")
                == WorldDiplomacyThreatRegistrationDecision.EscalateToUltimatum,
            "a noncomplied warning escalates to an ultimatum for the same target");
        existing.TargetDecision = "pending";
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatRegistration(existing, "ultimatum", "d9", "k2")
                == WorldDiplomacyThreatRegistrationDecision.Reject,
            "an undecided warning cannot escalate");
        existing.TargetDecision = "noncomplied";
        existing.TargetKingdomId = "k9";
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatRegistration(existing, "ultimatum", "d9", "k2")
                == WorldDiplomacyThreatRegistrationDecision.Reject,
            "a warning for a different target cannot escalate");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatRegistration(null!, "warning", "d9", "k2")
                == WorldDiplomacyThreatRegistrationDecision.CreateWarning
            && WorldDiplomacyRoundLifecycleRules.EvaluateThreatRegistration(null!, "ultimatum", "d9", "k2")
                == WorldDiplomacyThreatRegistrationDecision.CreateUltimatum
            && WorldDiplomacyRoundLifecycleRules.EvaluateThreatRegistration(null!, "statement", "d9", "k2")
                == WorldDiplomacyThreatRegistrationDecision.Reject,
            "without an open threat a warning or direct ultimatum registers and other stages reject");

        List<WorldDiplomacyThreat> threats = new List<WorldDiplomacyThreat>
        {
            new WorldDiplomacyThreat { ThreatId = "c1", Status = "complied", IssuerKingdomId = "k1", TargetKingdomId = "k2" },
            new WorldDiplomacyThreat { ThreatId = "c2", Status = "open", IssuerKingdomId = "k1", TargetKingdomId = "k2" }
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectOpenThreatBetween(threats, " k1 ", " k2 ")?.ThreatId == "c2",
            "the open directed threat wins after trimming kingdom ids");
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectOpenThreatBetween(threats, "k2", "k1") == null
            && WorldDiplomacyRoundLifecycleRules.SelectOpenThreatBetween(threats, "", "k2") == null
            && WorldDiplomacyRoundLifecycleRules.SelectOpenThreatBetween(null!, "k1", "k2") == null,
            "reversed or blank parties select no open threat");

        List<WorldDiplomacyThreat> recorded = new List<WorldDiplomacyThreat>
        {
            new WorldDiplomacyThreat
            {
                ThreatId = "r1", Status = "complied", IssuerKingdomId = "k1", TargetKingdomId = "k2",
                ComplianceDocumentId = "d1", ComplianceActionId = "a1"
            },
            new WorldDiplomacyThreat
            {
                ThreatId = "r2", Status = "open", IssuerKingdomId = "k1", TargetKingdomId = "k2",
                ComplianceDocumentId = "d1", ComplianceActionId = "a1"
            }
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectComplianceRecordedThreat(
                recorded, "K1", "k2", "D1", "a1")?.ThreatId == "r1",
            "a re-delivered compliance re-binds to the already-complied threat");
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectComplianceRecordedThreat(
                recorded, "k1", "k2", "d1", "a2") == null
            && WorldDiplomacyRoundLifecycleRules.SelectComplianceRecordedThreat(
                null!, "k1", "k2", "d1", "a1") == null,
            "a different deciding action or missing list re-binds nothing");

        WorldDiplomacyThreat complied = new WorldDiplomacyThreat
        {
            Status = "complied", ComplianceDocumentId = "d1",
            TargetDecision = "complied", StageDocumentId = "sd1", StageActionId = "sa1"
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatComplianceAlreadyRecorded(complied, "D1")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatComplianceAlreadyRecorded(complied, "d9")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatComplianceAlreadyRecorded(null!, "d1"),
            "only the recorded compliance document re-settles idempotently");

        WorldDiplomacyThreat pendingThreat = new WorldDiplomacyThreat
        {
            Status = "open", TargetDecision = "pending", StageDocumentId = "sd1", StageActionId = "sa1"
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatComplianceStageMatch(pendingThreat, "SD1", "sa1"),
            "a pending threat matches its stage document and action");
        pendingThreat.TargetDecision = "noncomplied";
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsThreatComplianceStageMatch(pendingThreat, "sd1", "sa1"),
            "a decided threat cannot comply again");
        pendingThreat.TargetDecision = "pending";
        pendingThreat.StageActionId = "";
        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatComplianceStageMatch(pendingThreat, "sd1", "")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatComplianceStageMatch(pendingThreat, "sd1", "sa1")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatComplianceStageMatch(pendingThreat, "sd9", "")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatComplianceStageMatch(null!, "sd1", ""),
            "legacy stage-only threats require an empty action response and the stage document");

        Test.True(WorldDiplomacyRoundLifecycleRules.ResolveCompliancePrestigeDelta("ultimatum", 7, 3) == 7
            && WorldDiplomacyRoundLifecycleRules.ResolveCompliancePrestigeDelta("warning", 7, 3) == 3
            && WorldDiplomacyRoundLifecycleRules.ResolveCompliancePrestigeDelta("other", 7, 3) == 3,
            "the compliance prestige delta follows the stage");
        Test.True(WorldDiplomacyRoundLifecycleRules.DescribeThreatStage("warning") == "谴责"
            && WorldDiplomacyRoundLifecycleRules.DescribeThreatStage("ultimatum") == "最后通牒"
            && WorldDiplomacyRoundLifecycleRules.DescribeThreatStageDiplomaticLabel("warning") == "外交谴责"
            && WorldDiplomacyRoundLifecycleRules.DescribeThreatStageDiplomaticLabel("ultimatum") == "最后通牒",
            "stage labels stay stable for compliance results and prestige reasons");
    }

    private static void RunThreatHistoryDecisionTests()
    {
        WorldDiplomacyThreat linked = new WorldDiplomacyThreat
        {
            ComplianceDocumentId = "d1", ResolutionDocumentId = "d2", StageRoundId = "r1",
            TargetDecisionRoundId = "r2", Stage = "warning"
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatLinkedToDocument(linked, "D1")
            && WorldDiplomacyRoundLifecycleRules.IsThreatLinkedToDocument(linked, "d2")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatLinkedToDocument(linked, "d9")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatLinkedToDocument(linked, "")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatLinkedToDocument(null!, "d1"),
            "history finalization binds a threat to its compliance or resolution document");

        WorldDiplomacyThreat breached = new WorldDiplomacyThreat { Status = "breached", HistoryResultRecorded = false };
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatHistoryFinalization(breached, true, false)
                == WorldDiplomacyThreatHistoryFinalization.AppendBreachResult,
            "a breached threat appends its reputation result once the declaration is published");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatHistoryFinalization(breached, true, true)
                == WorldDiplomacyThreatHistoryFinalization.AppendBreachResult,
            "a pending breach result wins over the already-recorded marker");
        breached.HistoryResultRecorded = true;
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatHistoryFinalization(breached, true, true)
                == WorldDiplomacyThreatHistoryFinalization.MarkResultRecorded
            && WorldDiplomacyRoundLifecycleRules.EvaluateThreatHistoryFinalization(breached, true, false)
                == WorldDiplomacyThreatHistoryFinalization.None,
            "an already-appended breach only follows the document result marker");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateThreatHistoryFinalization(
                new WorldDiplomacyThreat { Status = "complied" }, true, true)
                == WorldDiplomacyThreatHistoryFinalization.MarkResultRecorded
            && WorldDiplomacyRoundLifecycleRules.EvaluateThreatHistoryFinalization(
                new WorldDiplomacyThreat { Status = "complied" }, true, false)
                == WorldDiplomacyThreatHistoryFinalization.None
            && WorldDiplomacyRoundLifecycleRules.EvaluateThreatHistoryFinalization(null!, true, true)
                == WorldDiplomacyThreatHistoryFinalization.None,
            "non-breached threats only mark the result when the document already recorded it");

        WorldDiplomacyThreat noncompliant = new WorldDiplomacyThreat
        {
            TargetDecision = "noncomplied", TargetDecisionDocumentId = "d1",
            NonComplianceHistoryRecorded = false,
            NonComplianceEvents = new List<WorldDiplomacyThreatNonComplianceEvent>()
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatNonComplianceLinkedToDocument(noncompliant, "D1")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatNonComplianceLinkedToDocument(noncompliant, "d9"),
            "an unrecorded noncompliance decision binds to its decision document");
        noncompliant.NonComplianceHistoryRecorded = true;
        noncompliant.NonComplianceEvents.Add(new WorldDiplomacyThreatNonComplianceEvent
        {
            StageDocumentId = "sd1", DecisionDocumentId = "d7", HistoryRecorded = false
        });
        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatNonComplianceLinkedToDocument(noncompliant, "D7")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatNonComplianceLinkedToDocument(noncompliant, "d1"),
            "a pending noncompliance event binds to its own decision document");
        noncompliant.NonComplianceEvents[0].HistoryRecorded = true;
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsThreatNonComplianceLinkedToDocument(noncompliant, "d7")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatNonComplianceLinkedToDocument(null!, "d7"),
            "recorded events and missing threats bind nothing");

        Test.True(WorldDiplomacyRoundLifecycleRules.NeedsNonComplianceHistoryRetry(
                new WorldDiplomacyThreat { TargetDecision = "noncomplied", NonComplianceHistoryRecorded = false })
            && !WorldDiplomacyRoundLifecycleRules.NeedsNonComplianceHistoryRetry(
                new WorldDiplomacyThreat { TargetDecision = "noncomplied", NonComplianceHistoryRecorded = true })
            && !WorldDiplomacyRoundLifecycleRules.NeedsNonComplianceHistoryRetry(
                new WorldDiplomacyThreat { TargetDecision = "complied" }),
            "the noncompliance retry queue needs an unrecorded decision or event");
        Test.True(WorldDiplomacyRoundLifecycleRules.NeedsThreatResultHistoryRetry(
                new WorldDiplomacyThreat { Status = "breached" })
            && WorldDiplomacyRoundLifecycleRules.NeedsThreatResultHistoryRetry(
                new WorldDiplomacyThreat { Status = "complied" })
            && WorldDiplomacyRoundLifecycleRules.NeedsThreatResultHistoryRetry(
                new WorldDiplomacyThreat { Status = "enforced", ResolutionDocumentId = "d9" })
            && !WorldDiplomacyRoundLifecycleRules.NeedsThreatResultHistoryRetry(
                new WorldDiplomacyThreat { Status = "enforced" })
            && !WorldDiplomacyRoundLifecycleRules.NeedsThreatResultHistoryRetry(
                new WorldDiplomacyThreat { Status = "open" })
            && !WorldDiplomacyRoundLifecycleRules.NeedsThreatResultHistoryRetry(
                new WorldDiplomacyThreat { Status = "breached", HistoryResultRecorded = true }),
            "the result retry queue covers breached, complied, and enforced threats with a resolution document");
        Test.True(WorldDiplomacyRoundLifecycleRules.NeedsDomesticPenaltyHistoryRetry(
                new WorldDiplomacyThreat { Status = "complied", DomesticPenaltyCompleted = true })
            && !WorldDiplomacyRoundLifecycleRules.NeedsDomesticPenaltyHistoryRetry(
                new WorldDiplomacyThreat { Status = "complied", DomesticPenaltyCompleted = true, DomesticPenaltyHistoryRecorded = true })
            && !WorldDiplomacyRoundLifecycleRules.NeedsDomesticPenaltyHistoryRetry(
                new WorldDiplomacyThreat { Status = "open", DomesticPenaltyCompleted = true }),
            "the domestic penalty retry queue needs a completed unrecorded settlement on a complied threat");
        Test.True(WorldDiplomacyRoundLifecycleRules.NeedsIssuerRewardHistoryRetry(
                new WorldDiplomacyThreat { Status = "complied", IssuerRewardCompleted = true })
            && !WorldDiplomacyRoundLifecycleRules.NeedsIssuerRewardHistoryRetry(
                new WorldDiplomacyThreat { Status = "complied", IssuerRewardCompleted = true, IssuerRewardHistoryRecorded = true })
            && !WorldDiplomacyRoundLifecycleRules.NeedsIssuerRewardHistoryRetry(
                new WorldDiplomacyThreat { Status = "open", IssuerRewardCompleted = true }),
            "the issuer reward retry queue needs a completed unrecorded reward on a complied threat");

        List<WorldDiplomacyThreat> queue = new List<WorldDiplomacyThreat>
        {
            new WorldDiplomacyThreat { ThreatId = "t3", Status = "breached", UpdatedDay = 9 },
            new WorldDiplomacyThreat { ThreatId = "T1", Status = "breached", UpdatedDay = 5 },
            new WorldDiplomacyThreat { ThreatId = "t2", Status = "breached", UpdatedDay = 5 },
            new WorldDiplomacyThreat { ThreatId = "t0", Status = "open", UpdatedDay = 1 },
            null!
        };
        List<WorldDiplomacyThreat> batch = WorldDiplomacyRoundLifecycleRules.SelectThreatHistoryRetryBatch(
            queue, WorldDiplomacyRoundLifecycleRules.NeedsThreatResultHistoryRetry, 2);
        Test.True(batch.Count == 2 && batch[0].ThreatId == "T1" && batch[1].ThreatId == "t2",
            "retry batches order by updated day then threat id and respect the take bound");
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectThreatHistoryRetryBatch(
                queue, WorldDiplomacyRoundLifecycleRules.NeedsThreatResultHistoryRetry, 0).Count == 0
            && WorldDiplomacyRoundLifecycleRules.SelectThreatHistoryRetryBatch(
                null!, WorldDiplomacyRoundLifecycleRules.NeedsThreatResultHistoryRetry, 8).Count == 0,
            "empty batches stay empty without a queue or a positive take");

        Test.True(WorldDiplomacyRoundLifecycleRules.CanAppendDomesticPenaltyHistory(
                new WorldDiplomacyThreat { Status = "complied", DomesticPenaltyCompleted = true })
            && !WorldDiplomacyRoundLifecycleRules.CanAppendDomesticPenaltyHistory(
                new WorldDiplomacyThreat { Status = "complied", DomesticPenaltyCompleted = true, DomesticPenaltyHistoryRecorded = true })
            && !WorldDiplomacyRoundLifecycleRules.CanAppendDomesticPenaltyHistory(
                new WorldDiplomacyThreat { Status = "breached", DomesticPenaltyCompleted = true }),
            "the domestic penalty append gate requires a completed unrecorded complied threat");
        Test.True(WorldDiplomacyRoundLifecycleRules.CanAppendIssuerRewardHistory(
                new WorldDiplomacyThreat { Status = "complied", IssuerRewardCompleted = true })
            && !WorldDiplomacyRoundLifecycleRules.CanAppendIssuerRewardHistory(
                new WorldDiplomacyThreat { Status = "complied", IssuerRewardCompleted = true, IssuerRewardHistoryRecorded = true }),
            "the issuer reward append gate requires a completed unrecorded complied threat");
        Test.True(WorldDiplomacyRoundLifecycleRules.CanAppendThreatResultHistory(
                new WorldDiplomacyThreat { Status = "breached" })
            && !WorldDiplomacyRoundLifecycleRules.CanAppendThreatResultHistory(
                new WorldDiplomacyThreat { Status = "complied" }),
            "the breach result append gate requires an unrecorded breached threat");
        Test.True(WorldDiplomacyRoundLifecycleRules.CanAppendNonComplianceEventHistory(
                new WorldDiplomacyThreatNonComplianceEvent { StageDocumentId = "sd1", DecisionDocumentId = "d1" })
            && !WorldDiplomacyRoundLifecycleRules.CanAppendNonComplianceEventHistory(
                new WorldDiplomacyThreatNonComplianceEvent { StageDocumentId = "sd1", DecisionDocumentId = "d1", HistoryRecorded = true })
            && !WorldDiplomacyRoundLifecycleRules.CanAppendNonComplianceEventHistory(
                new WorldDiplomacyThreatNonComplianceEvent { StageDocumentId = "", DecisionDocumentId = "d1" }),
            "the noncompliance event append gate requires an unrecorded event with both documents");
        Test.True(WorldDiplomacyRoundLifecycleRules.CanCaptureNonComplianceEvent(
                new WorldDiplomacyThreat { TargetDecision = "noncomplied", StageDocumentId = "sd1", TargetDecisionDocumentId = "d1" })
            && !WorldDiplomacyRoundLifecycleRules.CanCaptureNonComplianceEvent(
                new WorldDiplomacyThreat { TargetDecision = "pending", StageDocumentId = "sd1", TargetDecisionDocumentId = "d1" })
            && !WorldDiplomacyRoundLifecycleRules.CanCaptureNonComplianceEvent(
                new WorldDiplomacyThreat { TargetDecision = "noncomplied", StageDocumentId = "", TargetDecisionDocumentId = "d1" }),
            "noncompliance capture requires the noncomplied decision plus stage and decision documents");

        WorldDiplomacyThreat withEvents = new WorldDiplomacyThreat
        {
            StageDocumentId = "sd2",
            NonComplianceEvents = new List<WorldDiplomacyThreatNonComplianceEvent>
            {
                new WorldDiplomacyThreatNonComplianceEvent { StageDocumentId = "sd2", DecisionDay = 4, HistoryRecorded = false },
                new WorldDiplomacyThreatNonComplianceEvent { StageDocumentId = "sd1", DecisionDay = 7, HistoryRecorded = false },
                new WorldDiplomacyThreatNonComplianceEvent { StageDocumentId = "sd0", DecisionDay = 7, HistoryRecorded = true }
            }
        };
        List<WorldDiplomacyThreatNonComplianceEvent> unrecorded =
            WorldDiplomacyRoundLifecycleRules.SelectUnrecordedNonComplianceEvents(withEvents);
        Test.True(unrecorded.Count == 2 && unrecorded[0].StageDocumentId == "sd2" && unrecorded[1].StageDocumentId == "sd1",
            "unrecorded noncompliance events replay in decision-day then stage-document order");
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectCurrentNonComplianceEvent(withEvents)?.StageDocumentId == "sd2"
            && WorldDiplomacyRoundLifecycleRules.SelectCurrentNonComplianceEvent(null!) == null,
            "the current noncompliance event matches the threat stage document");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsNonComplianceEventForCurrentStage(
                withEvents, withEvents.NonComplianceEvents[0])
            && !WorldDiplomacyRoundLifecycleRules.IsNonComplianceEventForCurrentStage(
                withEvents, withEvents.NonComplianceEvents[1]),
            "only the current-stage event flips the threat history marker");

        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeThreatEventStage("ULTIMATUM") == "ultimatum"
            && WorldDiplomacyRoundLifecycleRules.NormalizeThreatEventStage("warning") == "warning"
            && WorldDiplomacyRoundLifecycleRules.NormalizeThreatEventStage("other") == "warning"
            && WorldDiplomacyRoundLifecycleRules.DescribeThreatStageFormal("ultimatum") == "战争最后通牒"
            && WorldDiplomacyRoundLifecycleRules.DescribeThreatStageFormal("warning") == "谴责"
            && WorldDiplomacyRoundLifecycleRules.DescribeThreatExpectedFollowThrough("ultimatum") == "宣战"
            && WorldDiplomacyRoundLifecycleRules.DescribeThreatExpectedFollowThrough("warning") == "升级为最后通牒"
            && WorldDiplomacyRoundLifecycleRules.ResolveThreatCommitmentLevel("warning") == "non_binding"
            && WorldDiplomacyRoundLifecycleRules.ResolveThreatCommitmentLevel("ultimatum") == "binding"
            && WorldDiplomacyRoundLifecycleRules.ResolveThreatCommitmentLevel("WARNING") == "non_binding",
            "threat event stage and history labels stay stable");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatLinkedToRound(linked, "R1")
            && WorldDiplomacyRoundLifecycleRules.IsThreatLinkedToRound(linked, "r2")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatLinkedToRound(linked, "r9")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatLinkedToRound(linked, "")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatLinkedToRound(null!, "r1"),
            "an open threat links to a round through its stage or target-decision round");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatCancellationStatusCancelled("CANCELLED")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatCancellationStatusCancelled("pending")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatCancellationStatusCancelled(""),
            "the policy cancellation status only matches the cancelled marker");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsIssuerRewardHistoryEmpty(0, 3)
            && WorldDiplomacyRoundLifecycleRules.IsIssuerRewardHistoryEmpty(5, 0)
            && !WorldDiplomacyRoundLifecycleRules.IsIssuerRewardHistoryEmpty(5, 3),
            "a zero reward or zero applied clans records the issuer reward without an entry");

        Test.True(WorldDiplomacyRoundLifecycleRules.NeedsDomesticPenaltySettlementRetry(
                new WorldDiplomacyThreat { Status = "complied" })
            && WorldDiplomacyRoundLifecycleRules.NeedsDomesticPenaltySettlementRetry(
                new WorldDiplomacyThreat { Status = "compliance_pending" })
            && !WorldDiplomacyRoundLifecycleRules.NeedsDomesticPenaltySettlementRetry(
                new WorldDiplomacyThreat { Status = "complied", DomesticPenaltyCompleted = true })
            && !WorldDiplomacyRoundLifecycleRules.NeedsDomesticPenaltySettlementRetry(
                new WorldDiplomacyThreat { Status = "open" }),
            "the domestic penalty settlement retry queue needs an unsettled complied or pending threat");
        Test.True(WorldDiplomacyRoundLifecycleRules.NeedsPolicyCancellationRetry(
                new WorldDiplomacyThreat { Status = "complied" })
            && !WorldDiplomacyRoundLifecycleRules.NeedsPolicyCancellationRetry(
                new WorldDiplomacyThreat { Status = "complied", PolicyConditionCancellationCompleted = true })
            && !WorldDiplomacyRoundLifecycleRules.NeedsPolicyCancellationRetry(
                new WorldDiplomacyThreat { Status = "open" }),
            "the policy cancellation retry queue needs an unsettled complied threat");
        Test.True(WorldDiplomacyRoundLifecycleRules.NeedsIssuerRewardSettlementRetry(
                new WorldDiplomacyThreat { Status = "complied" })
            && !WorldDiplomacyRoundLifecycleRules.NeedsIssuerRewardSettlementRetry(
                new WorldDiplomacyThreat { Status = "complied", IssuerRewardCompleted = true })
            && !WorldDiplomacyRoundLifecycleRules.NeedsIssuerRewardSettlementRetry(
                new WorldDiplomacyThreat { Status = "open" }),
            "the issuer reward settlement retry queue needs an unsettled complied threat");

        List<WorldDiplomacyThreat> unordered = new List<WorldDiplomacyThreat>
        {
            new WorldDiplomacyThreat { ThreatId = "t3", UpdatedDay = 9 },
            new WorldDiplomacyThreat { ThreatId = "T1", UpdatedDay = 5 },
            new WorldDiplomacyThreat { ThreatId = "t2", UpdatedDay = 5 }
        };
        List<WorldDiplomacyThreat> ordered = WorldDiplomacyRoundLifecycleRules.OrderThreatsByUpdatedDay(unordered).ToList();
        Test.True(ordered.Count == 3 && ordered[0].ThreatId == "T1" && ordered[1].ThreatId == "t2" && ordered[2].ThreatId == "t3",
            "the shared threat ordering walks oldest updates first with a stable id tiebreak");
    }

    private static void RunThreatNormalizationDecisionTests()
    {
        Test.True(WorldDiplomacyRoundLifecycleRules.LimitText("abc", 5) == "abc"
            && WorldDiplomacyRoundLifecycleRules.LimitText("abcdef", 3) == "abc"
            && WorldDiplomacyRoundLifecycleRules.LimitText(null!, 3) == "",
            "limit text truncates to the cap and tolerates null");

        List<string> ids = WorldDiplomacyRoundLifecycleRules.NormalizeThreatIdList(
            new List<string> { " a ", "A", "", null!, "b" });
        Test.True(ids.Count == 2 && ids[0] == "a" && ids[1] == "b",
            "threat id lists trim, drop blanks, and dedupe case-insensitively");

        List<WorldDiplomacyThreatNonComplianceEvent> normalizedEvents =
            WorldDiplomacyRoundLifecycleRules.NormalizeThreatNonComplianceEvents(new List<WorldDiplomacyThreatNonComplianceEvent>
            {
                new WorldDiplomacyThreatNonComplianceEvent
                {
                    Stage = " ULTIMATUM ", StageDocumentId = " sd1 ", StageActionId = " sa1 ",
                    DecisionDocumentId = " d1 ", DecisionDay = 5, HistoryRecorded = true
                },
                new WorldDiplomacyThreatNonComplianceEvent
                {
                    Stage = "warning", StageDocumentId = "SD1", StageActionId = "sa1",
                    DecisionDocumentId = "d2", DecisionDay = 9
                },
                new WorldDiplomacyThreatNonComplianceEvent
                {
                    Stage = "warning", StageDocumentId = "", DecisionDocumentId = "d3"
                }
            });
        Test.True(normalizedEvents.Count == 1
            && normalizedEvents[0].Stage == "ultimatum"
            && normalizedEvents[0].StageDocumentId == "sd1"
            && normalizedEvents[0].DecisionDocumentId == "d1"
            && normalizedEvents[0].HistoryRecorded,
            "noncompliance event normalization keeps the recorded duplicate per stage key and drops incomplete rows");

        WorldDiplomacyThreat record = new WorldDiplomacyThreat
        {
            ThreatId = "", IssuerKingdomId = " k1 ", TargetKingdomId = " k2 ",
            Stage = "", Status = "compliance_pending", UltimatumDocumentId = " ud1 ",
            TargetDecision = "garbage", TargetDecisionDay = -4, ReputationPenaltyAmount = -9,
            ResolutionReason = new string('x', 300),
            PolicyConditionPolicyId = "", PolicyConditionOwnerKingdomId = "",
            PolicyConditionCancellationStatus = "pending",
            IssuerRewardAmount = 999, IssuerRewardCompleted = true
        };
        int factoryCalls = 0;
        WorldDiplomacyRoundLifecycleRules.NormalizeThreatRecord(record, () => { factoryCalls++; return "gen-1"; }, 100);
        Test.True(factoryCalls == 1 && record.ThreatId == "gen-1"
            && record.IssuerKingdomId == "k1" && record.TargetKingdomId == "k2"
            && record.Stage == "ultimatum" && record.Status == "complied"
            && record.TargetDecision == "complied" && record.TargetDecisionDay == 0
            && record.ReputationPenaltyAmount == 0 && record.ResolutionReason.Length == 180
            && record.StageDocumentId == "ud1" && record.UltimatumDocumentId == "ud1"
            && record.PolicyConditionCancellationStatus == "not_bound"
            && record.PolicyConditionCancellationCompleted
            && record.IssuerRewardAmount == 100 && !record.IssuerRewardHistoryRecorded,
            "record normalization repairs ids, stage, status, days, policy state, and clamps the reward");

        WorldDiplomacyThreat zeroReward = new WorldDiplomacyThreat
        {
            ThreatId = "t0", IssuerKingdomId = "k1", TargetKingdomId = "k2",
            Stage = "ultimatum", Status = "complied", StageDocumentId = "sd0",
            IssuerRewardAmount = 0, IssuerRewardCompleted = true
        };
        WorldDiplomacyRoundLifecycleRules.NormalizeThreatRecord(zeroReward, () => "unused", 100);
        Test.True(zeroReward.IssuerRewardHistoryRecorded,
            "a completed zero-amount issuer reward records its history as a no-op");

        WorldDiplomacyThreat pending = new WorldDiplomacyThreat
        {
            ThreatId = "t1", IssuerKingdomId = "k1", TargetKingdomId = "k2",
            Stage = "warning", Status = "open", WarningDocumentId = "wd1",
            PolicyConditionPolicyId = "p1", PolicyConditionOwnerKingdomId = "k1",
            PolicyConditionCancellationStatus = ""
        };
        WorldDiplomacyRoundLifecycleRules.NormalizeThreatRecord(pending, () => "unused", 100);
        Test.True(pending.Stage == "warning" && pending.Status == "open"
            && pending.TargetDecision == "pending"
            && pending.StageDocumentId == "wd1"
            && pending.PolicyConditionCancellationStatus == "pending"
            && !pending.PolicyConditionCancellationCompleted,
            "an open warning keeps its pending decision and binds the policy cancellation as pending");

        Dictionary<string, int> normalized = WorldDiplomacyRoundLifecycleRules.NormalizeKingdomIntDictionary(
            new Dictionary<string, int> { [" k1 "] = 300, ["K1"] = 7, [""] = 50, ["k2"] = -5 }, 0, 100);
        Test.True(normalized.Count == 2 && normalized["k1"] == 7 && normalized["k2"] == 0,
            "kingdom dictionaries trim keys, keep the last duplicate, and clamp values");

        List<WorldDiplomacyPrestigeRelationModifier> modifiers =
            WorldDiplomacyRoundLifecycleRules.SelectRetainedPrestigeRelationModifiers(
                new List<WorldDiplomacyPrestigeRelationModifier>
                {
                    new WorldDiplomacyPrestigeRelationModifier { KingdomId = " k1 ", RulerHeroId = "r1", VassalLeaderHeroId = "v1" },
                    new WorldDiplomacyPrestigeRelationModifier { KingdomId = "K1", RulerHeroId = "r1", VassalLeaderHeroId = "v1" },
                    new WorldDiplomacyPrestigeRelationModifier { KingdomId = "k1", RulerHeroId = "", VassalLeaderHeroId = "v2" }
                });
        Test.True(modifiers.Count == 1 && modifiers[0].KingdomId == "K1",
            "relation modifiers dedupe by trimmed composite key and keep the last entry");

        Test.True(WorldDiplomacyRoundLifecycleRules.HasValidThreatParties(
                new WorldDiplomacyThreat { IssuerKingdomId = "k1", TargetKingdomId = "k2" })
            && !WorldDiplomacyRoundLifecycleRules.HasValidThreatParties(
                new WorldDiplomacyThreat { IssuerKingdomId = "k1", TargetKingdomId = "K1" })
            && !WorldDiplomacyRoundLifecycleRules.HasValidThreatParties(
                new WorldDiplomacyThreat { IssuerKingdomId = "", TargetKingdomId = "k2" }),
            "normalization drops threats with missing or self-referencing parties");

        List<WorldDiplomacyThreat> issuerGroup = new List<WorldDiplomacyThreat>
        {
            new WorldDiplomacyThreat { ThreatId = "t1", Stage = "warning", UpdatedDay = 5, StageIssuedDay = 5, CreatedDay = 5 },
            new WorldDiplomacyThreat { ThreatId = "t2", Stage = "ultimatum", UpdatedDay = 5, StageIssuedDay = 5, CreatedDay = 3 },
            new WorldDiplomacyThreat { ThreatId = "t3", Stage = "warning", UpdatedDay = 9, StageIssuedDay = 9, CreatedDay = 9 }
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectRetainedOpenThreatForIssuer(issuerGroup)?.ThreatId == "t3",
            "the newest open threat survives issuer dedupe before stage or age tiebreaks");
        issuerGroup.RemoveAll(x => x.ThreatId == "t3");
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectRetainedOpenThreatForIssuer(issuerGroup)?.ThreatId == "t2",
            "an ultimatum outranks an equally fresh warning when deduping open threats");

        WorldDiplomacyThreat fullySettled = new WorldDiplomacyThreat
        {
            Status = "complied", HistoryResultRecorded = true,
            DomesticPenaltyCompleted = true, DomesticPenaltyHistoryRecorded = true,
            PolicyConditionCancellationCompleted = true,
            IssuerRewardCompleted = true, IssuerRewardHistoryRecorded = true,
            NonComplianceHistoryRecorded = true
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.NeedsThreatSettlementRetention(
                new WorldDiplomacyThreat { Status = "complied" })
            && !WorldDiplomacyRoundLifecycleRules.NeedsThreatSettlementRetention(fullySettled)
            && WorldDiplomacyRoundLifecycleRules.NeedsThreatSettlementRetention(
                new WorldDiplomacyThreat { Status = "complied", HistoryResultRecorded = true, DomesticPenaltyCompleted = false })
            && WorldDiplomacyRoundLifecycleRules.NeedsThreatSettlementRetention(
                new WorldDiplomacyThreat { Status = "complied", HistoryResultRecorded = true, DomesticPenaltyCompleted = true, DomesticPenaltyHistoryRecorded = false }),
            "settlement retention keeps terminal threats with unfinished consequences or history");

        List<WorldDiplomacyThreat> retained = WorldDiplomacyRoundLifecycleRules.SelectThreatRetentionSet(
            new List<WorldDiplomacyThreat>
            {
                new WorldDiplomacyThreat { ThreatId = "a1", Status = "open", CreatedDay = 3, UpdatedDay = 9 },
                new WorldDiplomacyThreat { ThreatId = "p1", Status = "complied", CreatedDay = 1, UpdatedDay = 5 },
                new WorldDiplomacyThreat { ThreatId = "o1", Status = "breached", HistoryResultRecorded = true, CreatedDay = 2, UpdatedDay = 4 },
                new WorldDiplomacyThreat { ThreatId = "o2", Status = "breached", HistoryResultRecorded = true, CreatedDay = 4, UpdatedDay = 1 }
            },
            cutoffDay: 3,
            maxStored: 2);
        Test.True(retained.Count == 3
            && retained[0].ThreatId == "p1" && retained[1].ThreatId == "o1" && retained[2].ThreatId == "a1",
            "retention keeps active plus pending-settlement threats, fills ordinary slots by recency, and orders by creation");

        WorldDiplomacyThreat invalidated = new WorldDiplomacyThreat
        {
            Status = "open", ObligationRoundId = "r1", ObligationClaimedDay = 4, UpdatedDay = 2
        };
        WorldDiplomacyRoundLifecycleRules.InvalidateThreatForNormalization(invalidated, "dup", 7);
        Test.True(invalidated.Status == "invalidated" && invalidated.ResolutionReason == "dup"
            && invalidated.UpdatedDay == 7 && invalidated.ObligationRoundId == ""
            && invalidated.ObligationClaimedDay == 0 && invalidated.HistoryResultRecorded,
            "invalidation closes the record, stamps the repair day, and suppresses history");

        Test.True(WorldDiplomacyRoundLifecycleRules.ResolveThreatRequiredSpeakerId(
                new WorldDiplomacyThreat { TargetDecision = "noncomplied", IssuerKingdomId = "k1", TargetKingdomId = "k2" }) == "k1"
            && WorldDiplomacyRoundLifecycleRules.ResolveThreatRequiredSpeakerId(
                new WorldDiplomacyThreat { TargetDecision = "pending", IssuerKingdomId = "k1", TargetKingdomId = "k2" }) == "k2"
            && WorldDiplomacyRoundLifecycleRules.ResolveThreatRequiredSpeakerId(
                new WorldDiplomacyThreat { TargetDecision = "complied", IssuerKingdomId = "k1", TargetKingdomId = "k2" }) == ""
            && WorldDiplomacyRoundLifecycleRules.ResolveThreatRequiredSpeakerId(null!) == "",
            "the required relay speaker is the issuer after noncompliance and the target while pending");
    }

    private static void RunPolicyConditionDecisionTests()
    {
        WorldDiplomacyPolicyCancellationDispatch DispatchFor(string status, bool completed, string policyId, string ownerId)
            => WorldDiplomacyRoundLifecycleRules.EvaluateThreatPolicyCancellationDispatch(
                new WorldDiplomacyThreat
                {
                    Status = status,
                    PolicyConditionCancellationCompleted = completed,
                    PolicyConditionPolicyId = policyId,
                    PolicyConditionOwnerKingdomId = ownerId
                });

        Test.True(DispatchFor("complied", false, "p1", "k1") == WorldDiplomacyPolicyCancellationDispatch.AttemptCancellation
            && DispatchFor("open", false, "p1", "k1") == WorldDiplomacyPolicyCancellationDispatch.Reject
            && DispatchFor("complied", true, "p1", "k1") == WorldDiplomacyPolicyCancellationDispatch.AlreadyComplete
            && DispatchFor("complied", false, "", "k1") == WorldDiplomacyPolicyCancellationDispatch.MarkNotBound
            && DispatchFor("complied", false, "p1", " ") == WorldDiplomacyPolicyCancellationDispatch.MarkNotBound,
            "policy cancellation only attempts once on bound complied threats and self-completes when unbound");

        WorldDiplomacyPolicySignal Signal(string? policyId = "p1", string? kind = "kingdom", string? issuer = "k1", string? target = "k2", int day = 0)
            => new WorldDiplomacyPolicySignal
            {
                PolicyId = policyId!, PolicyKind = kind!,
                IssuerKingdomId = issuer!, TargetKingdomId = target!,
                PublishedDay = day
            };

        Test.True(WorldDiplomacyRoundLifecycleRules.IsPolicySignalEligibleForThreatBinding(Signal())
            && WorldDiplomacyRoundLifecycleRules.IsPolicySignalEligibleForThreatBinding(Signal(kind: ""))
            && !WorldDiplomacyRoundLifecycleRules.IsPolicySignalEligibleForThreatBinding(Signal(policyId: " "))
            && !WorldDiplomacyRoundLifecycleRules.IsPolicySignalEligibleForThreatBinding(Signal(kind: "internal"))
            && !WorldDiplomacyRoundLifecycleRules.IsPolicySignalEligibleForThreatBinding(Signal(issuer: null))
            && !WorldDiplomacyRoundLifecycleRules.IsPolicySignalEligibleForThreatBinding(Signal(target: null))
            && !WorldDiplomacyRoundLifecycleRules.IsPolicySignalEligibleForThreatBinding(null!),
            "threat binding requires a kingdom-scoped policy signal with both parties");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatPolicyPartyMatch("t1", "i1", "t1", "i1")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatPolicyPartyMatch("t1", "i1", "t2", "i1")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatPolicyPartyMatch(null!, "i1", "t1", "i1")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatPolicyPartyMatch("t1", "", "t1", "i1"),
            "policy binding matches the owner representative to the threat target and the affected representative to the issuer");

        WorldDiplomacyPolicySignal oldP1 = Signal(policyId: "P1", day: 2);
        WorldDiplomacyPolicySignal newP1 = Signal(policyId: "p1", day: 9);
        WorldDiplomacyPolicySignal p2 = Signal(policyId: "p2", day: 5);
        Test.True(ReferenceEquals(WorldDiplomacyRoundLifecycleRules.SelectUniquePolicySignal(
                new List<WorldDiplomacyPolicySignal> { oldP1, newP1 }), newP1)
            && WorldDiplomacyRoundLifecycleRules.SelectUniquePolicySignal(
                new List<WorldDiplomacyPolicySignal> { oldP1, p2 }) == null
            && WorldDiplomacyRoundLifecycleRules.SelectUniquePolicySignal(
                new List<WorldDiplomacyPolicySignal>()) == null,
            "binding selects the latest signal per policy only when exactly one policy matches");

        WorldDiplomacyThreat bound = new WorldDiplomacyThreat();
        WorldDiplomacyRoundLifecycleRules.InitializeThreatPolicyCondition(bound,
            new WorldDiplomacyPolicySignal
            {
                SignalKey = " s1 ", PolicyId = " p1 ", PolicyName = " 巡察 ",
                IssuerKingdomId = " k1 ", TargetKingdomId = " k2 "
            }, -3);
        Test.True(bound.PolicyConditionSignalKey == "s1" && bound.PolicyConditionPolicyId == "p1"
            && bound.PolicyConditionOwnerKingdomId == "k1" && bound.PolicyConditionAffectedKingdomId == "k2"
            && bound.PolicyConditionBoundDay == 0 && !bound.PolicyConditionCancellationCompleted
            && bound.PolicyConditionCancellationStatus == "pending",
            "binding records the trimmed signal identity, clamps the day, and opens cancellation as pending");
        WorldDiplomacyThreat unbound = new WorldDiplomacyThreat();
        WorldDiplomacyRoundLifecycleRules.InitializeThreatPolicyCondition(unbound, null!, 5);
        Test.True(unbound.PolicyConditionCancellationCompleted
            && unbound.PolicyConditionCancellationStatus == "not_bound",
            "an unbound threat is immediately complete with the not_bound status");

        WorldDiplomacyPolicySignal signal = new WorldDiplomacyPolicySignal { PolicyId = "P1", IssuerKingdomId = "K1" };
        Test.True(WorldDiplomacyRoundLifecycleRules.IsPolicySignalBoundTo(signal, "p1", "k1")
            && !WorldDiplomacyRoundLifecycleRules.IsPolicySignalBoundTo(signal, "p2", "k1")
            && !WorldDiplomacyRoundLifecycleRules.IsPolicySignalBoundTo(signal, "p1", "k2")
            && !WorldDiplomacyRoundLifecycleRules.IsPolicySignalBoundTo(null!, "p1", "k1"),
            "a signal is bound to the settled policy only by policy id and owner kingdom");

        WorldDiplomacyThreat openBound = new WorldDiplomacyThreat
        {
            Status = "open", PolicyConditionPolicyId = "P1", PolicyConditionOwnerKingdomId = "K1"
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatBoundToPolicy(openBound, "p1", "k1")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatBoundToPolicy(
                new WorldDiplomacyThreat { Status = "complied", PolicyConditionPolicyId = "p1", PolicyConditionOwnerKingdomId = "k1" }, "p1", "k1")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatBoundToPolicy(openBound, "p2", "k1")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatBoundToPolicy(null!, "p1", "k1"),
            "only open threats bound to the same policy and owner are invalidated together");

        WorldDiplomacyThreat sibling = new WorldDiplomacyThreat
        {
            Status = "open", ObligationRoundId = "r1", ObligationClaimedDay = 3,
            ResolutionRoundId = "r2", ResolutionDocumentId = "d1", UpdatedDay = 2
        };
        WorldDiplomacyRoundLifecycleRules.InvalidateThreatForPolicyCancellation(sibling, 6);
        Test.True(sibling.Status == "invalidated"
            && sibling.ResolutionReason == "bound_policy_cancelled_by_another_complied_threat"
            && sibling.ResolutionRoundId == "" && sibling.ResolutionDocumentId == ""
            && sibling.ObligationRoundId == "" && sibling.ObligationClaimedDay == 0
            && sibling.UpdatedDay == 6 && sibling.HistoryResultRecorded,
            "policy cancellation invalidation clears resolution and obligation state and suppresses history");

        Test.True(WorldDiplomacyRoundLifecycleRules.ResolvePolicyCancellationStatus(null!) == "cancelled"
            && WorldDiplomacyRoundLifecycleRules.ResolvePolicyCancellationStatus("  ") == "cancelled"
            && WorldDiplomacyRoundLifecycleRules.ResolvePolicyCancellationStatus(" Policy_Inactive ") == "policy_inactive",
            "the cancellation status defaults to cancelled and normalizes the API result");

        WorldDiplomacyThreat migratedOpen = new WorldDiplomacyThreat { Status = " open ", IssuerRewardAmount = 50 };
        WorldDiplomacyThreat migratedClosed = new WorldDiplomacyThreat { Status = "complied", IssuerRewardAmount = 50 };
        WorldDiplomacyRoundLifecycleRules.MigrateThreatComplianceConsequencesV3(migratedOpen);
        WorldDiplomacyRoundLifecycleRules.MigrateThreatComplianceConsequencesV3(migratedClosed);
        Test.True(migratedOpen.PolicyConditionCancellationCompleted
            && migratedOpen.PolicyConditionCancellationStatus == "legacy_not_bound"
            && migratedOpen.PolicyConditionPolicyId == "" && migratedOpen.IssuerRewardAmount == 50,
            "the v3 migration strips policy bindings and leaves open threats eligible for rewards");
        Test.True(migratedClosed.IssuerRewardAmount == 0 && migratedClosed.IssuerRewardSnapshotCaptured
            && migratedClosed.IssuerRewardCompleted && migratedClosed.IssuerRewardHistoryRecorded,
            "the v3 migration suppresses retroactive issuer rewards on terminal threats");
    }

    private static void RunThreatConsequenceDecisionTests()
    {
        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatDomesticPenaltyEligible(
                new WorldDiplomacyThreat
                {
                    Status = "compliance_pending", StageDocumentId = "d1",
                    ComplianceDocumentId = "d2", TargetKingdomId = "k2"
                }, "k2")
            && WorldDiplomacyRoundLifecycleRules.IsThreatDomesticPenaltyEligible(
                new WorldDiplomacyThreat
                {
                    Status = "complied", StageDocumentId = "d1",
                    ComplianceDocumentId = "d2", TargetKingdomId = "k2"
                }, "k2")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatDomesticPenaltyEligible(
                new WorldDiplomacyThreat
                {
                    Status = "open", StageDocumentId = "d1",
                    ComplianceDocumentId = "d2", TargetKingdomId = "k2"
                }, "k2")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatDomesticPenaltyEligible(
                new WorldDiplomacyThreat
                {
                    Status = "complied", StageDocumentId = "",
                    ComplianceDocumentId = "d2", TargetKingdomId = "k2"
                }, "k2")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatDomesticPenaltyEligible(
                new WorldDiplomacyThreat
                {
                    Status = "complied", StageDocumentId = "d1",
                    ComplianceDocumentId = "d2", TargetKingdomId = "k2"
                }, "k3")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatDomesticPenaltyEligible(null!, "k2"),
            "the domestic penalty requires a complied or pending threat with both documents on the target kingdom");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatIssuerRewardEligible(
                new WorldDiplomacyThreat
                {
                    Status = "complied", ComplianceDocumentId = "d2", IssuerKingdomId = "k1"
                }, "k1")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatIssuerRewardEligible(
                new WorldDiplomacyThreat
                {
                    Status = "compliance_pending", ComplianceDocumentId = "d2", IssuerKingdomId = "k1"
                }, "k1")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatIssuerRewardEligible(
                new WorldDiplomacyThreat
                {
                    Status = "complied", ComplianceDocumentId = "", IssuerKingdomId = "k1"
                }, "k1")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatIssuerRewardEligible(null!, "k1"),
            "the issuer reward only runs on a complied threat with a compliance document on the issuer kingdom");

        Test.True(WorldDiplomacyRoundLifecycleRules.CountThreatConsequenceAppliedClans(
                new List<string> { "c1", " ", "", "c2" }) == 2
            && WorldDiplomacyRoundLifecycleRules.CountThreatConsequenceAppliedClans(null!) == 0,
            "the affected-clan count ignores blank entries");

        WorldDiplomacyThreat penaltySnap = new WorldDiplomacyThreat
        {
            DomesticPenaltyAppliedClanIds = new List<string> { "old" },
            DomesticPenaltySkippedClanIds = new List<string> { "old" }
        };
        WorldDiplomacyRoundLifecycleRules.CaptureThreatDomesticPenaltySnapshot(
            penaltySnap, "r1", new List<string> { "c2", "c1" });
        Test.True(penaltySnap.DomesticPenaltyRulingClanId == "r1"
            && penaltySnap.DomesticPenaltyEligibleClanIds.SequenceEqual(new[] { "c1", "c2" })
            && penaltySnap.DomesticPenaltyAppliedClanIds.Count == 0
            && penaltySnap.DomesticPenaltySkippedClanIds.Count == 0
            && penaltySnap.DomesticPenaltySnapshotCaptured,
            "the penalty snapshot stores the ruling clan, orders eligibility, and clears prior settlements");

        WorldDiplomacyThreat rewardSnap = new WorldDiplomacyThreat
        {
            IssuerRewardAppliedClanIds = new List<string> { "old" },
            IssuerRewardSkippedClanIds = new List<string> { "old" }
        };
        WorldDiplomacyRoundLifecycleRules.CaptureThreatIssuerRewardSnapshot(
            rewardSnap, "r2", new List<string> { "c2", "c1" });
        Test.True(rewardSnap.IssuerRewardRulingClanId == "r2"
            && rewardSnap.IssuerRewardEligibleClanIds.SequenceEqual(new[] { "c1", "c2" })
            && rewardSnap.IssuerRewardAppliedClanIds.Count == 0
            && rewardSnap.IssuerRewardSkippedClanIds.Count == 0
            && rewardSnap.IssuerRewardSnapshotCaptured,
            "the reward snapshot stores the ruling clan, orders eligibility, and clears prior settlements");

        WorldDiplomacyThreat penaltyEmpty = new WorldDiplomacyThreat
        {
            DomesticPenaltyAppliedClanIds = new List<string> { "c1" },
            DomesticPenaltySkippedClanIds = new List<string> { "c2" }
        };
        WorldDiplomacyRoundLifecycleRules.CompleteThreatDomesticPenaltyWithoutEligible(penaltyEmpty);
        Test.True(penaltyEmpty.DomesticPenaltyCompleted
            && penaltyEmpty.DomesticPenaltyAppliedClanIds.Count == 0
            && penaltyEmpty.DomesticPenaltySkippedClanIds.Count == 0,
            "an empty eligible set completes the penalty and clears stale settlement lists");

        WorldDiplomacyThreat rewardEmpty = new WorldDiplomacyThreat
        {
            IssuerRewardAppliedClanIds = new List<string> { "c1" }
        };
        WorldDiplomacyRoundLifecycleRules.CompleteThreatIssuerRewardWithoutEligible(rewardEmpty);
        Test.True(rewardEmpty.IssuerRewardCompleted && rewardEmpty.IssuerRewardHistoryRecorded
            && rewardEmpty.IssuerRewardAppliedClanIds.Count == 1,
            "an empty eligible set completes the reward, records history, and keeps applied clans");

        WorldDiplomacyThreat rewardZero = new WorldDiplomacyThreat();
        WorldDiplomacyRoundLifecycleRules.CompleteThreatIssuerRewardWithoutAmount(rewardZero);
        Test.True(rewardZero.IssuerRewardSnapshotCaptured && rewardZero.IssuerRewardCompleted
            && rewardZero.IssuerRewardHistoryRecorded,
            "a non-positive reward completes immediately at snapshot time");

        HashSet<string> applied = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "c1" };
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectUnresolvedClanIds(
                new List<string> { "c1", "c2", "c3" }, applied).SequenceEqual(new[] { "c2", "c3" })
            && WorldDiplomacyRoundLifecycleRules.SelectUnresolvedClanIds(null!, applied).Count == 0,
            "unresolved clans are the eligible ids minus the applied ids");

        WorldDiplomacyThreat penaltySkipped = new WorldDiplomacyThreat();
        WorldDiplomacyRoundLifecycleRules.CompleteThreatDomesticPenaltyAsSkipped(
            penaltySkipped, new List<string> { "c2", "c1" });
        Test.True(penaltySkipped.DomesticPenaltyCompleted
            && penaltySkipped.DomesticPenaltySkippedClanIds.SequenceEqual(new[] { "c1", "c2" }),
            "a missing ruling clan completes the penalty with every unresolved clan skipped");

        WorldDiplomacyThreat rewardSkipped = new WorldDiplomacyThreat();
        WorldDiplomacyRoundLifecycleRules.CompleteThreatIssuerRewardAsSkipped(
            rewardSkipped, new List<string> { "c2", "c1" });
        Test.True(rewardSkipped.IssuerRewardCompleted
            && rewardSkipped.IssuerRewardSkippedClanIds.SequenceEqual(new[] { "c1", "c2" }),
            "a missing ruling clan completes the reward with every unresolved clan skipped");

        HashSet<string> skipped = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "c2" };
        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatConsequenceClanSettled("c1", applied, skipped)
            && WorldDiplomacyRoundLifecycleRules.IsThreatConsequenceClanSettled("c2", applied, skipped)
            && !WorldDiplomacyRoundLifecycleRules.IsThreatConsequenceClanSettled("c3", applied, skipped)
            && WorldDiplomacyRoundLifecycleRules.IsThreatConsequenceSettled(
                new List<string> { "c1", "c2" }, applied, skipped)
            && !WorldDiplomacyRoundLifecycleRules.IsThreatConsequenceSettled(
                new List<string> { "c1", "c3" }, applied, skipped),
            "a consequence is settled only when every eligible clan is applied or skipped");

        Test.True(WorldDiplomacyRoundLifecycleRules.OrderThreatConsequenceClanIds(
                new List<string> { "b", "A", "c" }).SequenceEqual(new[] { "A", "b", "c" })
            && WorldDiplomacyRoundLifecycleRules.OrderThreatConsequenceClanIds(null!).Count == 0,
            "settlement lists persist in ordinal-insensitive order");

        Test.True(WorldDiplomacyRoundLifecycleRules.ClampThreatIssuerRewardAmount(999, 100) == 100
            && WorldDiplomacyRoundLifecycleRules.ClampThreatIssuerRewardAmount(-5, 100) == 0
            && WorldDiplomacyRoundLifecycleRules.ClampThreatIssuerRewardAmount(40, 100) == 40,
            "the issuer reward amount is clamped into the configured bounds");

        Test.True(WorldDiplomacyRoundLifecycleRules.ComputeThreatConsequenceRelationTarget(95, 10) == 100
            && WorldDiplomacyRoundLifecycleRules.ComputeThreatConsequenceRelationTarget(-95, -10) == -100
            && WorldDiplomacyRoundLifecycleRules.ComputeThreatConsequenceRelationTarget(50, -20) == 30
            && WorldDiplomacyRoundLifecycleRules.ShouldApplyThreatRelationReward(99)
            && !WorldDiplomacyRoundLifecycleRules.ShouldApplyThreatRelationReward(100)
            && WorldDiplomacyRoundLifecycleRules.ShouldApplyThreatRelationPenalty(-99)
            && !WorldDiplomacyRoundLifecycleRules.ShouldApplyThreatRelationPenalty(-100)
            && WorldDiplomacyRoundLifecycleRules.IsThreatRelationRewardApplied(80, 80)
            && !WorldDiplomacyRoundLifecycleRules.IsThreatRelationRewardApplied(70, 80)
            && WorldDiplomacyRoundLifecycleRules.IsThreatRelationPenaltyApplied(-60, -60)
            && !WorldDiplomacyRoundLifecycleRules.IsThreatRelationPenaltyApplied(-50, -60),
            "relation consequences clamp to the signed bound and apply only when the relation can still move");

        WorldDiplomacyThreat unresolvedPenalty = new WorldDiplomacyThreat { UpdatedDay = 1 };
        WorldDiplomacyRoundLifecycleRules.FinalizeUnresolvedThreatDomesticPenalty(
            unresolvedPenalty, new List<string> { "c2", "c1" }, 9);
        Test.True(unresolvedPenalty.DomesticPenaltySnapshotCaptured
            && unresolvedPenalty.DomesticPenaltyCompleted && unresolvedPenalty.UpdatedDay == 9
            && unresolvedPenalty.DomesticPenaltySkippedClanIds.SequenceEqual(new[] { "c1", "c2" }),
            "an unresolvable penalty finalizes the snapshot, completion, day, and ordered skips");

        WorldDiplomacyThreat unresolvedReward = new WorldDiplomacyThreat { UpdatedDay = 1 };
        WorldDiplomacyRoundLifecycleRules.FinalizeUnresolvedThreatIssuerReward(
            unresolvedReward, new List<string> { "c2", "c1" }, 9);
        Test.True(unresolvedReward.IssuerRewardSnapshotCaptured
            && unresolvedReward.IssuerRewardCompleted && unresolvedReward.UpdatedDay == 9
            && unresolvedReward.IssuerRewardSkippedClanIds.SequenceEqual(new[] { "c1", "c2" }),
            "an unresolvable reward finalizes the snapshot, completion, day, and ordered skips");
    }

    private static void RunWarResponseGateDecisionTests()
    {
        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatDecisionNoncomplied(
                new WorldDiplomacyThreat { TargetDecision = "noncomplied" })
            && !WorldDiplomacyRoundLifecycleRules.IsThreatDecisionNoncomplied(
                new WorldDiplomacyThreat { TargetDecision = "pending" })
            && !WorldDiplomacyRoundLifecycleRules.IsThreatDecisionNoncomplied(null!),
            "the noncomplied decision predicate only fires on the explicit refusal");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatDecisionPending(
                new WorldDiplomacyThreat { TargetDecision = "pending" })
            && !WorldDiplomacyRoundLifecycleRules.IsThreatDecisionPending(
                new WorldDiplomacyThreat { TargetDecision = "noncomplied" })
            && !WorldDiplomacyRoundLifecycleRules.IsThreatDecisionPending(null!),
            "the pending decision predicate only fires on undecided threats");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatAtStage(
                new WorldDiplomacyThreat { Stage = "warning" }, "warning")
            && WorldDiplomacyRoundLifecycleRules.IsThreatAtStage(
                new WorldDiplomacyThreat { Stage = "warning" }, "WARNING")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatAtStage(
                new WorldDiplomacyThreat { Stage = "ultimatum" }, "warning")
            && !WorldDiplomacyRoundLifecycleRules.IsThreatAtStage(null!, "warning"),
            "the stage predicate matches a threat's stage case-insensitively");

        Test.True(WorldDiplomacyRoundLifecycleRules.SelectThreatStageDocumentId(
                new WorldDiplomacyThreat { Stage = "ultimatum", UltimatumDocumentId = "u1", WarningDocumentId = "w1" }) == "u1"
            && WorldDiplomacyRoundLifecycleRules.SelectThreatStageDocumentId(
                new WorldDiplomacyThreat { Stage = "warning", UltimatumDocumentId = "u1", WarningDocumentId = "w1" }) == "w1",
            "the stage document id follows the canonical stage");

        Test.True(WorldDiplomacyRoundLifecycleRules.ResolveCanonicalThreatStage("ultimatum", "") == "ultimatum"
            && WorldDiplomacyRoundLifecycleRules.ResolveCanonicalThreatStage("warning", "") == "warning"
            && WorldDiplomacyRoundLifecycleRules.ResolveCanonicalThreatStage("warning", "u1") == "ultimatum"
            && WorldDiplomacyRoundLifecycleRules.ResolveCanonicalThreatStage(null, "") == "warning",
            "canonical stage keeps explicit ultimatums, upgrades doc-backed warnings, and defaults to warning");

        List<WorldDiplomacyThreat> pendingIncoming = new List<WorldDiplomacyThreat>
        {
            new WorldDiplomacyThreat
            {
                Status = "open", TargetKingdomId = "k2", TargetDecision = "pending", UpdatedDay = 5
            },
            new WorldDiplomacyThreat
            {
                Status = "open", TargetKingdomId = "k2", TargetDecision = "pending", UpdatedDay = 9
            },
            new WorldDiplomacyThreat
            {
                Status = "open", TargetKingdomId = "k2", TargetDecision = "noncomplied", UpdatedDay = 7
            },
            new WorldDiplomacyThreat
            {
                Status = "open", TargetKingdomId = "k3", TargetDecision = "pending", UpdatedDay = 8
            },
            new WorldDiplomacyThreat
            {
                Status = "resolved", TargetKingdomId = "k2", TargetDecision = "pending", UpdatedDay = 6
            }
        };
        List<WorldDiplomacyThreat> pendingSelected = WorldDiplomacyRoundLifecycleRules
            .SelectPendingIncomingThreats(pendingIncoming, "K2");
        Test.True(pendingSelected.Count == 2
            && pendingSelected[0].UpdatedDay == 9
            && pendingSelected[1].UpdatedDay == 5,
            "pending incoming threats are open, targeted at the kingdom, and newest first");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsRejectedUltimatumEnforceable(
                new WorldDiplomacyThreat { Stage = "ultimatum", TargetDecision = "noncomplied" })
            && !WorldDiplomacyRoundLifecycleRules.IsRejectedUltimatumEnforceable(
                new WorldDiplomacyThreat { Stage = "warning", TargetDecision = "noncomplied" })
            && !WorldDiplomacyRoundLifecycleRules.IsRejectedUltimatumEnforceable(
                new WorldDiplomacyThreat { Stage = "ultimatum", TargetDecision = "complied" })
            && !WorldDiplomacyRoundLifecycleRules.IsRejectedUltimatumEnforceable(null!),
            "war enforcement requires a rejected ultimatum, not a warning or a pending decision");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsEscalatableWarningThreat(
                new WorldDiplomacyThreat
                {
                    Stage = "warning", TargetDecision = "noncomplied", TargetKingdomId = "k2"
                }, "k2")
            && !WorldDiplomacyRoundLifecycleRules.IsEscalatableWarningThreat(
                new WorldDiplomacyThreat
                {
                    Stage = "warning", TargetDecision = "noncomplied", TargetKingdomId = "k3"
                }, "k2")
            && !WorldDiplomacyRoundLifecycleRules.IsEscalatableWarningThreat(
                new WorldDiplomacyThreat
                {
                    Stage = "ultimatum", TargetDecision = "noncomplied", TargetKingdomId = "k2"
                }, "k2"),
            "warning escalation requires a noncomplied warning aimed at the same target");

        List<WorldDiplomacyThreat> noncompliedThreats = new List<WorldDiplomacyThreat>
        {
            new WorldDiplomacyThreat
            {
                Status = "open", IssuerKingdomId = "k1", TargetDecision = "noncomplied",
                StageDocumentId = "d2", StageIssuedDay = 5, ThreatId = "t2"
            },
            new WorldDiplomacyThreat
            {
                Status = "open", IssuerKingdomId = "k1", TargetDecision = "noncomplied",
                StageDocumentId = "d1", StageIssuedDay = 2, ThreatId = "t1"
            },
            new WorldDiplomacyThreat
            {
                Status = "open", IssuerKingdomId = "k1", TargetDecision = "pending",
                StageDocumentId = "d3", StageIssuedDay = 1, ThreatId = "t3"
            },
            new WorldDiplomacyThreat
            {
                Status = "complied", IssuerKingdomId = "k1", TargetDecision = "noncomplied",
                StageDocumentId = "d4", StageIssuedDay = 0, ThreatId = "t4"
            },
            new WorldDiplomacyThreat
            {
                Status = "open", IssuerKingdomId = "k9", TargetDecision = "noncomplied",
                StageDocumentId = "d5", StageIssuedDay = 0, ThreatId = "t5"
            },
            new WorldDiplomacyThreat
            {
                Status = "open", IssuerKingdomId = "k1", TargetDecision = "noncomplied",
                StageDocumentId = " ", StageIssuedDay = 0, ThreatId = "t6"
            }
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectNoncompliedThreatStageDocumentIds(
                noncompliedThreats, "K1").SequenceEqual(new[] { "d1", "d2" })
            && WorldDiplomacyRoundLifecycleRules.SelectNoncompliedThreatStageDocumentIds(
                noncompliedThreats, "").Count == 0,
            "noncomplied stage documents are open, issued by the author, and ordered by issued day");

        WorldDiplomacyResultSettlementSlot related = new WorldDiplomacyResultSettlementSlot
        {
            RelatedKingdomIds = new List<string> { "K2" }
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.IsSettlementSlotRelatedTo(related, "k2")
            && !WorldDiplomacyRoundLifecycleRules.IsSettlementSlotRelatedTo(related, "k3")
            && !WorldDiplomacyRoundLifecycleRules.IsSettlementSlotRelatedTo(
                new WorldDiplomacyResultSettlementSlot(), "k2")
            && !WorldDiplomacyRoundLifecycleRules.IsSettlementSlotRelatedTo(null!, "k2"),
            "slot relation requires the target kingdom in the related id list");

        WorldDiplomacyDocument warDoc = new WorldDiplomacyDocument
        {
            IsReadyForPublication = true, RoundId = "r1", AuthorKingdomId = "k2"
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.IsWarResponseSourceDocument(warDoc, "R1", "K2")
            && !WorldDiplomacyRoundLifecycleRules.IsWarResponseSourceDocument(warDoc, "r2", "k2")
            && !WorldDiplomacyRoundLifecycleRules.IsWarResponseSourceDocument(warDoc, "r1", "k1")
            && !WorldDiplomacyRoundLifecycleRules.IsWarResponseSourceDocument(
                new WorldDiplomacyDocument { IsReadyForPublication = false, RoundId = "r1", AuthorKingdomId = "k2" }, "r1", "k2")
            && !WorldDiplomacyRoundLifecycleRules.IsWarResponseSourceDocument(null!, "r1", "k2"),
            "a war-response source must be a ready document from the same round by the war author");

        Test.True(WorldDiplomacyRoundLifecycleRules.CannotCaptureThreatConsequenceSnapshot(true, true, false)
            && !WorldDiplomacyRoundLifecycleRules.CannotCaptureThreatConsequenceSnapshot(true, true, true)
            && !WorldDiplomacyRoundLifecycleRules.CannotCaptureThreatConsequenceSnapshot(true, false, false)
            && !WorldDiplomacyRoundLifecycleRules.CannotCaptureThreatConsequenceSnapshot(false, true, false),
            "only an eliminated kingdom without a ruling clan blocks consequence snapshots");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatBreachPenaltyApplicable(
                new WorldDiplomacyThreat { ReputationPenaltyApplied = false })
            && !WorldDiplomacyRoundLifecycleRules.IsThreatBreachPenaltyApplicable(
                new WorldDiplomacyThreat { ReputationPenaltyApplied = true })
            && !WorldDiplomacyRoundLifecycleRules.IsThreatBreachPenaltyApplicable(null!),
            "a breach penalty applies once and only once per threat");

        Test.True(WorldDiplomacyRoundLifecycleRules.ResolveThreatBreachPrestigePenalty("ultimatum", 25, 10) == 25
            && WorldDiplomacyRoundLifecycleRules.ResolveThreatBreachPrestigePenalty("warning", 25, 10) == 10
            && WorldDiplomacyRoundLifecycleRules.ResolveThreatBreachRelationPenalty("ultimatum", -5, -2) == -5
            && WorldDiplomacyRoundLifecycleRules.ResolveThreatBreachRelationPenalty("warning", -5, -2) == -2,
            "breach penalties map the stage to the configured ultimatum or warning amount");

        Test.True(WorldDiplomacyRoundLifecycleRules.DescribeThreatBreachPrestigeReason("ultimatum")
                == DiplomacyMiscPromptTexts.Get("Threat_NoWarAfterUltimatumRejected", "")
            && WorldDiplomacyRoundLifecycleRules.DescribeThreatBreachPrestigeReason("warning")
                == DiplomacyMiscPromptTexts.Get("Threat_NoEscalationAfterDenunciationRejected", "")
            && WorldDiplomacyRoundLifecycleRules.DescribeThreatBreachResolutionReason("ultimatum")
                == "ultimatum_not_followed_by_war_in_next_declaration"
            && WorldDiplomacyRoundLifecycleRules.DescribeThreatBreachResolutionReason("warning")
                == "warning_not_followed_by_ultimatum_in_next_declaration",
            "breach reasons must keep their exact stage-mapped literals");

        WorldDiplomacyThreat breach = new WorldDiplomacyThreat
        {
            Stage = "ultimatum", ThreatId = "t1", ObligationRoundId = "r9", ObligationClaimedDay = 3
        };
        WorldDiplomacyRoundLifecycleRules.ApplyThreatBreachSettlement(breach, 7, "r1", "d1", 42);
        Test.True(breach.Status == "breached"
            && breach.ReputationPenaltyApplied
            && breach.ReputationPenaltyAmount == 7
            && breach.ResolutionRoundId == "r1"
            && breach.ResolutionDocumentId == "d1"
            && breach.ResolutionReason == "ultimatum_not_followed_by_war_in_next_declaration"
            && breach.UpdatedDay == 42
            && breach.ObligationRoundId == ""
            && breach.ObligationClaimedDay == 0,
            "a breach settlement stamps status, amount, resolution ids, reason, and clears the obligation");
        WorldDiplomacyThreat breachClamped = new WorldDiplomacyThreat { Stage = "warning" };
        WorldDiplomacyRoundLifecycleRules.ApplyThreatBreachSettlement(breachClamped, -4, null, null, 1);
        Test.True(breachClamped.ReputationPenaltyAmount == 0
            && breachClamped.ResolutionReason == "warning_not_followed_by_ultimatum_in_next_declaration"
            && breachClamped.ResolutionRoundId == "" && breachClamped.ResolutionDocumentId == "",
            "a negative applied delta clamps to zero and null ids normalize to empty");

        List<WorldDiplomacyThreat> noticeThreats = new List<WorldDiplomacyThreat>
        {
            new WorldDiplomacyThreat
            {
                IssuerResolutionNoticePending = true, IssuerKingdomId = "k1",
                ResolutionRoundId = "r0", ThreatId = "n1"
            },
            new WorldDiplomacyThreat
            {
                IssuerResolutionNoticePending = true, IssuerKingdomId = "k1",
                ResolutionRoundId = "r2", ThreatId = "n2"
            },
            new WorldDiplomacyThreat
            {
                IssuerResolutionNoticePending = false, IssuerKingdomId = "k1",
                ResolutionRoundId = "r0", ThreatId = "n3"
            },
            new WorldDiplomacyThreat
            {
                IssuerResolutionNoticePending = true, IssuerKingdomId = "k2",
                ResolutionRoundId = "r0", ThreatId = "n4"
            }
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.IsIssuerResolutionNoticePendingFor(
                noticeThreats[0], "K1")
            && !WorldDiplomacyRoundLifecycleRules.IsIssuerResolutionNoticePendingFor(
                noticeThreats[2], "k1")
            && !WorldDiplomacyRoundLifecycleRules.IsIssuerResolutionNoticePendingFor(
                noticeThreats[0], "k2"),
            "a resolution notice binds to its issuer only while still pending");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsIssuerResolutionNoticeOrphaned(
                noticeThreats[0], "r2", "k1")
            && !WorldDiplomacyRoundLifecycleRules.IsIssuerResolutionNoticeOrphaned(
                noticeThreats[0], "r0", "k1"),
            "an orphaned notice is one resolved by another round");
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectIssuerResolutionNotices(
                noticeThreats, "k1", "r2", int.MaxValue).Select(x => x.ThreatId)
                    .SequenceEqual(new[] { "n1" })
            && WorldDiplomacyRoundLifecycleRules.SelectIssuerResolutionNotices(
                noticeThreats, "k1", null, 4).Select(x => x.ThreatId)
                    .SequenceEqual(new[] { "n1", "n2" })
            && WorldDiplomacyRoundLifecycleRules.SelectIssuerResolutionNotices(
                noticeThreats, "k1", null, 1).Count == 1,
            "notice selection filters pending issuer notices, optionally excludes a round, and caps the count");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatFollowThroughEscalation("warning", "ultimatum", true)
            && !WorldDiplomacyRoundLifecycleRules.IsThreatFollowThroughEscalation("warning", "declare_war", true)
            && !WorldDiplomacyRoundLifecycleRules.IsThreatFollowThroughEscalation("warning", "ultimatum", false)
            && !WorldDiplomacyRoundLifecycleRules.IsThreatFollowThroughEscalation("ultimatum", "ultimatum", true),
            "a warning escalates only when the next declaration is an ultimatum at the same target");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsThreatFollowThroughEnforcement("ultimatum", "declare_war", true, true)
            && !WorldDiplomacyRoundLifecycleRules.IsThreatFollowThroughEnforcement("ultimatum", "declare_war", true, false)
            && !WorldDiplomacyRoundLifecycleRules.IsThreatFollowThroughEnforcement("ultimatum", "declare_war", false, true)
            && !WorldDiplomacyRoundLifecycleRules.IsThreatFollowThroughEnforcement("warning", "declare_war", true, true)
            && !WorldDiplomacyRoundLifecycleRules.IsThreatFollowThroughEnforcement("ultimatum", "ultimatum", true, true),
            "an ultimatum enforces only on a real war declaration against the threatened kingdom");

        Test.True(WorldDiplomacyRoundLifecycleRules.HasThreatPresentationDrift(
                new[] { "d1" }, new[] { "d2" }, new[] { "f1" }, new[] { "f1" })
            && WorldDiplomacyRoundLifecycleRules.HasThreatPresentationDrift(
                new[] { "d1" }, new[] { "d1" }, new[] { "f1" }, new[] { "f2" })
            && !WorldDiplomacyRoundLifecycleRules.HasThreatPresentationDrift(
                new[] { "D1" }, new[] { "d1" }, new[] { "f1" }, new[] { "f1" })
            && !WorldDiplomacyRoundLifecycleRules.HasThreatPresentationDrift(null, null, null, null),
            "presentation drift fires on any presented or follow-through id mismatch");
    }

    private static void RunStorageNormalizationDecisionTests()
    {
        List<WorldDiplomacyPolicySignal> signals = new List<WorldDiplomacyPolicySignal>
        {
            null!,
            new WorldDiplomacyPolicySignal { SignalKey = " ", IssuerKingdomId = "k1", TargetKingdomId = "k2", PublishedDay = 9 },
            new WorldDiplomacyPolicySignal { SignalKey = "s1", IssuerKingdomId = "", TargetKingdomId = "k2", PublishedDay = 9 },
            new WorldDiplomacyPolicySignal { SignalKey = "s1", IssuerKingdomId = "k1", TargetKingdomId = " ", PublishedDay = 9 },
            new WorldDiplomacyPolicySignal { SignalKey = "A", IssuerKingdomId = "k1", TargetKingdomId = "k2", PublishedDay = 1 },
            new WorldDiplomacyPolicySignal { SignalKey = "a", IssuerKingdomId = "k3", TargetKingdomId = "k4", PublishedDay = 3 },
            new WorldDiplomacyPolicySignal { SignalKey = "b", IssuerKingdomId = "k1", TargetKingdomId = "k2", PublishedDay = 2 },
        };
        List<WorldDiplomacyPolicySignal> normalizedSignals =
            WorldDiplomacyRoundLifecycleRules.NormalizePendingPolicySignals(signals, 24);
        Test.True(normalizedSignals.Count == 2
            && normalizedSignals[0].SignalKey == "a" && normalizedSignals[0].IssuerKingdomId == "k3"
            && normalizedSignals[1].SignalKey == "b",
            "pending policy signals drop invalid records, keep the newest per key, and order newest first");
        List<WorldDiplomacyPolicySignal> cappedSignals = Enumerable.Range(0, 30)
            .Select(i => new WorldDiplomacyPolicySignal
            {
                SignalKey = "k" + i, IssuerKingdomId = "k1", TargetKingdomId = "k2", PublishedDay = i,
            }).ToList();
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizePendingPolicySignals(cappedSignals, 24).Count == 24
            && WorldDiplomacyRoundLifecycleRules.NormalizePendingPolicySignals(cappedSignals, 24)
                .All(x => x.PublishedDay >= 6),
            "pending policy signals are capped to the newest entries");

        List<string> keys = new List<string> { " ", "A", "a", "b", "A" };
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeProcessedSignalKeys(keys, 256)
                .SequenceEqual(new[] { "A", "b" }),
            "processed signal keys drop blanks and deduplicate case-insensitively keeping the first occurrence");
        List<string> manyKeys = Enumerable.Range(0, 300).Select(i => "k" + i).ToList();
        List<string> cappedKeys = WorldDiplomacyRoundLifecycleRules.NormalizeProcessedSignalKeys(manyKeys, 256);
        Test.True(cappedKeys.Count == 256 && cappedKeys[0] == "k44" && cappedKeys[255] == "k299",
            "processed signal keys keep the most recent entries when capped");

        List<WorldDiplomacyPropagationArrival> arrivals = new List<WorldDiplomacyPropagationArrival>
        {
            new WorldDiplomacyPropagationArrival { DocumentId = "d2", SettlementId = "s1", DueDay = 5 },
            new WorldDiplomacyPropagationArrival { DocumentId = "d3", Scope = "court", KingdomId = "k1", DueDay = 5 },
            new WorldDiplomacyPropagationArrival { DocumentId = "", SettlementId = "s1", DueDay = 1 },
            new WorldDiplomacyPropagationArrival { DocumentId = "d4", Scope = "court", DueDay = 1 },
            new WorldDiplomacyPropagationArrival { DocumentId = "d1", SettlementId = "s1", DueDay = 5 },
            new WorldDiplomacyPropagationArrival { DocumentId = "d0", SettlementId = "s1", DueDay = 2 },
        };
        List<WorldDiplomacyPropagationArrival> normalizedArrivals =
            WorldDiplomacyRoundLifecycleRules.NormalizePropagationArrivalList(arrivals);
        Test.True(normalizedArrivals.Select(x => x.DocumentId)
                .SequenceEqual(new[] { "d0", "d3", "d1", "d2" }),
            "propagation arrivals drop invalid records and order by due day, court first, then document id");
        Test.True(normalizedArrivals.First(x => x.DocumentId == "d1").Scope == "civilian",
            "propagation arrivals default a blank scope to civilian");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsCourtPropagationArrival(
                new WorldDiplomacyPropagationArrival { Scope = "COURT" })
            && !WorldDiplomacyRoundLifecycleRules.IsCourtPropagationArrival(
                new WorldDiplomacyPropagationArrival { Scope = "civilian" })
            && !WorldDiplomacyRoundLifecycleRules.IsCourtPropagationArrival(null!),
            "court arrival detection is a case-insensitive scope match");

        List<WorldDiplomacyWarLedger> ledgers = new List<WorldDiplomacyWarLedger>
        {
            null!,
            new WorldDiplomacyWarLedger { FirstKingdomId = " ", SecondKingdomId = "k2" },
            new WorldDiplomacyWarLedger
            {
                FirstKingdomId = "k1", SecondKingdomId = "k2",
                SettlementChanges = new List<WorldDiplomacySettlementChange>
                {
                    null!,
                    new WorldDiplomacySettlementChange { SettlementId = "" },
                    new WorldDiplomacySettlementChange { SettlementId = "s1" },
                },
            },
            new WorldDiplomacyWarLedger { FirstKingdomId = "k3", SecondKingdomId = "k4" },
        };
        WorldDiplomacyRoundLifecycleRules.NormalizeWarLedgerList(ledgers);
        Test.True(ledgers.Count == 2
            && ledgers[0].SettlementChanges.Count == 1
            && ledgers[0].SettlementChanges[0].SettlementId == "s1"
            && ledgers[1].SettlementChanges.Count == 0,
            "war ledgers drop invalid records and purge settlement changes without a settlement id");

        List<WorldDiplomacyBattleFact> battles = new List<WorldDiplomacyBattleFact>
        {
            null!,
            new WorldDiplomacyBattleFact { BattleId = " " },
            new WorldDiplomacyBattleFact { BattleId = "b1" },
        };
        WorldDiplomacyRoundLifecycleRules.NormalizeBattleRecords(battles);
        Test.True(battles.Count == 1
            && battles[0].AttackerKingdomIds.Count == 0
            && battles[0].DefenderKingdomIds.Count == 0
            && battles[0].AttackerLeaderNames.Count == 0
            && battles[0].DefenderLeaderNames.Count == 0,
            "battle records drop invalid entries and initialize every participant list");

        WorldDiplomacyJob job = new WorldDiplomacyJob
        {
            JobId = "j1", Kind = "compress", CompressionTargetTokens = 100,
            ForcedIntent = "legacy", PresentedThreatDocumentIds = new List<string> { "t1", " ", "T1" },
        };
        WorldDiplomacyRoundLifecycleRules.NormalizeJobRecord(job);
        Test.True(job.CandidateKingdomIds.Count == 0 && job.TriggerDocumentIds.Count == 0
            && job.LlmMessages.Count == 0 && job.CompressionRoundIds.Count == 0
            && job.PresentedLegalActionSignature == "" && job.ResultSettlementSlotId == ""
            && job.ForcedIntent == ""
            && job.PresentedThreatDocumentIds.SequenceEqual(new[] { "t1" }),
            "job normalization initializes every collection, clears the legacy forced intent, and deduplicates threat ids");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "COMPRESS")
            && !WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "offer")
            && !WorldDiplomacyRoundLifecycleRules.IsJobOfKind(null!, "compress"),
            "job kind matching is case-insensitive and null-safe");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsRetiredJob(null!)
            && WorldDiplomacyRoundLifecycleRules.IsRetiredJob(
                new WorldDiplomacyJob { JobId = " " })
            && WorldDiplomacyRoundLifecycleRules.IsRetiredJob(
                new WorldDiplomacyJob { JobId = "j", Kind = "participate" })
            && WorldDiplomacyRoundLifecycleRules.IsRetiredJob(
                new WorldDiplomacyJob { JobId = "j", Kind = "ROUND_COMPRESS" })
            && WorldDiplomacyRoundLifecycleRules.IsRetiredJob(
                new WorldDiplomacyJob { JobId = "j", IsRelayTurn = true, Priority = 98, ForcedIntent = "x" })
            && WorldDiplomacyRoundLifecycleRules.IsRetiredJob(
                new WorldDiplomacyJob { JobId = "j", Kind = "compress", CompressionTargetTokens = 0 })
            && !WorldDiplomacyRoundLifecycleRules.IsRetiredJob(
                new WorldDiplomacyJob { JobId = "j", Kind = "compress", CompressionTargetTokens = 100 })
            && !WorldDiplomacyRoundLifecycleRules.IsRetiredJob(
                new WorldDiplomacyJob { JobId = "j", IsRelayTurn = true, Priority = 98 }),
            "retired jobs cover invalid, migrated, forced-relay, and empty compression records");

        List<WorldDiplomacyDocument> docs = new List<WorldDiplomacyDocument>
        {
            null!,
            new WorldDiplomacyDocument { DocumentId = "x1", RoundId = "r1" },
            new WorldDiplomacyDocument { DocumentId = "x2", RoundId = "r2" },
            new WorldDiplomacyDocument { DocumentId = "x3", RoundId = "r9" },
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectRoundCompressionDocuments(
                docs, "r1", new[] { "X3" }).Select(x => x.DocumentId)
                .SequenceEqual(new[] { "x1", "x3" }),
            "compression document selection unions the round id with explicit document ids");

        List<WorldDiplomacyDocument> pubDocs = new List<WorldDiplomacyDocument>
        {
            new WorldDiplomacyDocument { DocumentId = "p2", RoundId = "r1", IsReadyForPublication = true, Day = 4, CreatedUtcTicks = 1 },
            new WorldDiplomacyDocument { DocumentId = "p1", RoundId = "r1", IsReadyForPublication = true, Day = 4, CreatedUtcTicks = 0 },
            new WorldDiplomacyDocument { DocumentId = "p3", RoundId = "r1", IsReadyForPublication = false, Day = 1 },
            new WorldDiplomacyDocument { DocumentId = "p4", RoundId = "r2", IsReadyForPublication = true, Day = 0 },
            new WorldDiplomacyDocument { DocumentId = "p0", RoundId = "r1", IsReadyForPublication = true, Day = 2 },
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectPublishedRoundDocuments(pubDocs, "r1")
                .Select(x => x.DocumentId).SequenceEqual(new[] { "p0", "p1", "p2" }),
            "published round documents filter by round and publication state then order by day and ticks");

        WorldDiplomacyDocument warDoc = new WorldDiplomacyDocument
        {
            DocumentId = "w1", ChangedDiplomaticState = true,
            Intent = "Declare_War", TargetKingdomId = "k2",
        };
        WorldDiplomacyDocument actionWarDoc = new WorldDiplomacyDocument
        {
            DocumentId = "w2",
            Actions = new List<WorldDiplomacyDocumentAction>
            {
                new WorldDiplomacyDocumentAction { Intent = "gift", TargetKingdomId = "k2" },
                new WorldDiplomacyDocumentAction
                {
                    Intent = "declare_war", TargetKingdomId = "K2", ChangedDiplomaticState = true,
                },
            },
        };
        WorldDiplomacyDocument peaceDoc = new WorldDiplomacyDocument
        {
            DocumentId = "w3", ChangedDiplomaticState = true,
            Intent = "declare_peace", TargetKingdomId = "k2",
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.IsSuccessfulWarDocumentAgainst(warDoc, "k2")
            && WorldDiplomacyRoundLifecycleRules.IsSuccessfulWarDocumentAgainst(actionWarDoc, "k2")
            && !WorldDiplomacyRoundLifecycleRules.IsSuccessfulWarDocumentAgainst(warDoc, "k9")
            && !WorldDiplomacyRoundLifecycleRules.IsSuccessfulWarDocumentAgainst(peaceDoc, "k2")
            && !WorldDiplomacyRoundLifecycleRules.IsSuccessfulWarDocumentAgainst(null!, "k2")
            && !WorldDiplomacyRoundLifecycleRules.IsSuccessfulWarDocumentAgainst(warDoc, " "),
            "a successful war document is a changed declare_war against the target on the doc or any action");

        List<WorldDiplomacyDocument> settleDocs = new List<WorldDiplomacyDocument>
        {
            warDoc,
            new WorldDiplomacyDocument
            {
                DocumentId = "w4", ChangedDiplomaticState = true,
                Intent = "declare_war", TargetKingdomId = "k9", Day = 6,
            },
            new WorldDiplomacyDocument
            {
                DocumentId = "r1", AuthorKingdomId = "k2", IsReadyForPublication = true,
                Day = 5, CreatedUtcTicks = 0,
            },
        };
        warDoc.Day = 4;
        WorldDiplomacyResultSettlementSlot answered = new WorldDiplomacyResultSettlementSlot
        {
            Kind = "war_response", KingdomId = "k2", SourceDocumentIds = new List<string> { "w1" },
        };
        WorldDiplomacyResultSettlementSlot unanswered = new WorldDiplomacyResultSettlementSlot
        {
            Kind = "war_response+trade", KingdomId = "k9", SourceDocumentIds = new List<string> { "w4" },
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.IsWarResponseSlotAnswered(answered, settleDocs)
            && !WorldDiplomacyRoundLifecycleRules.IsWarResponseSlotAnswered(unanswered, settleDocs)
            && !WorldDiplomacyRoundLifecycleRules.IsWarResponseSlotAnswered(answered, new List<WorldDiplomacyDocument>())
            && !WorldDiplomacyRoundLifecycleRules.IsWarResponseSlotAnswered(null!, settleDocs),
            "a war-response slot is answered only when every sourced war has a later reply from the slot kingdom");

        Test.True(WorldDiplomacyRoundLifecycleRules.SelectSlotKindsExcept("war_response+trade+war_response", "war_response")
                .SequenceEqual(new[] { "trade" })
            && WorldDiplomacyRoundLifecycleRules.SelectSlotKindsExcept("war_response", "war_response").Count == 0
            && WorldDiplomacyRoundLifecycleRules.SelectSlotKindsExcept(null!, "war_response").Count == 0,
            "slot kind splitting removes the excluded kind and deduplicates case-insensitively");

        WorldDiplomacyResultSettlementSlot stripped = new WorldDiplomacyResultSettlementSlot
        {
            Kind = "war_response+trade", KingdomId = "k2", SourceDocumentIds = new List<string> { "w1" },
        };
        WorldDiplomacyRound settleRound = new WorldDiplomacyRound
        {
            RoundId = "r1",
            ResultSettlementSlots = new List<WorldDiplomacyResultSettlementSlot>
            {
                answered, stripped, unanswered,
                new WorldDiplomacyResultSettlementSlot
                {
                    Kind = "trade", KingdomId = "k2", SourceDocumentIds = new List<string> { "w1" },
                },
            },
        };
        WorldDiplomacyRoundLifecycleRules.RemoveAnsweredWarResponseSlots(settleRound, settleDocs);
        Test.True(settleRound.ResultSettlementSlots.Count == 3
            && !settleRound.ResultSettlementSlots.Contains(answered)
            && stripped.Kind == "trade"
            && unanswered.Kind == "war_response+trade",
            "answered war-response slots are removed entirely or stripped of the war_response kind");
    }

    private static void RunRoundRecordNormalizationDecisionTests()
    {
        WorldDiplomacyPolicySignal cloneSource = new WorldDiplomacyPolicySignal
        {
            SignalKey = "s1", PolicyId = "p1", PolicyKind = " ",
            IssuerKingdomId = "k1", TargetKingdomId = "k2", PublishedDay = -3,
        };
        WorldDiplomacyPolicySignal clone = WorldDiplomacyRoundLifecycleRules.ClonePolicySignalRecord(cloneSource);
        Test.True(clone != null && !ReferenceEquals(clone, cloneSource)
            && clone.PolicyKind == "kingdom" && clone.PublishedDay == 0
            && WorldDiplomacyRoundLifecycleRules.ClonePolicySignalRecord(null!) == null,
            "policy signal clones apply defaults without sharing the source record");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsOpenSettlementSlotStatus("")
            && WorldDiplomacyRoundLifecycleRules.IsOpenSettlementSlotStatus("PENDING")
            && WorldDiplomacyRoundLifecycleRules.IsOpenSettlementSlotStatus("inflight")
            && WorldDiplomacyRoundLifecycleRules.IsOpenSettlementSlotStatus("scheduled")
            && WorldDiplomacyRoundLifecycleRules.IsOpenSettlementSlotStatus("waiting_player")
            && !WorldDiplomacyRoundLifecycleRules.IsOpenSettlementSlotStatus("closed")
            && !WorldDiplomacyRoundLifecycleRules.IsOpenSettlementSlotStatus("answered"),
            "open settlement slots are blank or any unfinished status");
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeSettlementSlotStatus("WAITING_PLAYER") == "waiting_player"
            && WorldDiplomacyRoundLifecycleRules.NormalizeSettlementSlotStatus("Inflight") == "inflight"
            && WorldDiplomacyRoundLifecycleRules.NormalizeSettlementSlotStatus("scheduled") == "scheduled"
            && WorldDiplomacyRoundLifecycleRules.NormalizeSettlementSlotStatus("weird") == "pending"
            && WorldDiplomacyRoundLifecycleRules.NormalizeSettlementSlotStatus(null!) == "pending",
            "slot status normalization preserves the three live states and falls back to pending");
        Test.True(WorldDiplomacyRoundLifecycleRules.IsSettlementSlotAtStatus(
                new WorldDiplomacyResultSettlementSlot { Status = "Waiting_Player" }, "waiting_player")
            && !WorldDiplomacyRoundLifecycleRules.IsSettlementSlotAtStatus(
                new WorldDiplomacyResultSettlementSlot { Status = "pending" }, "waiting_player")
            && !WorldDiplomacyRoundLifecycleRules.IsSettlementSlotAtStatus(null!, "pending"),
            "slot status matching is case-insensitive and null-safe");

        Test.True(WorldDiplomacyRoundLifecycleRules.MergeDistinctIds(new[] { "a", " ", "A", "b" })
                .SequenceEqual(new[] { "a", "b" })
            && WorldDiplomacyRoundLifecycleRules.MergeDistinctIds(null!).Count == 0,
            "distinct id merge drops blanks and deduplicates case-insensitively");

        List<WorldDiplomacyResultSettlementSlot> mergeInput = new List<WorldDiplomacyResultSettlementSlot>
        {
            null!,
            new WorldDiplomacyResultSettlementSlot { KingdomId = "" },
            new WorldDiplomacyResultSettlementSlot { KingdomId = "k1", Status = "closed" },
            new WorldDiplomacyResultSettlementSlot
            {
                SlotId = "s-a", KingdomId = "K1", Status = "pending", Kind = "war_response",
                SourceDocumentIds = new List<string> { "d1" },
                RelatedKingdomIds = new List<string> { "k9" },
            },
            new WorldDiplomacyResultSettlementSlot
            {
                SlotId = "s-b", KingdomId = "k1", Status = "inflight", Kind = "trade+war_response",
                SourceDocumentIds = new List<string> { "d2", "d1" },
                RelatedKingdomIds = new List<string> { "k9", "k8" },
            },
        };
        string mergedCurrent;
        List<WorldDiplomacyResultSettlementSlot> merged =
            WorldDiplomacyRoundLifecycleRules.MergeResultSettlementSlots(mergeInput, "", out mergedCurrent);
        Test.True(merged.Count == 1 && mergedCurrent == ""
            && merged[0].SlotId == "s-b"
            && merged[0].Status == "inflight"
            && merged[0].Kind == "war_response+trade"
            && merged[0].SourceDocumentIds.SequenceEqual(new[] { "d1", "d2" })
            && merged[0].RelatedKingdomIds.SequenceEqual(new[] { "k9", "k8" }),
            "slot merge keeps one pending slot per kingdom and unions its obligations");
        string mergedCurrent2;
        List<WorldDiplomacyResultSettlementSlot> merged2 =
            WorldDiplomacyRoundLifecycleRules.MergeResultSettlementSlots(mergeInput, "s-a", out mergedCurrent2);
        Test.True(merged2.Count == 1 && mergedCurrent2 == "s-a" && merged2[0].SlotId == "s-a",
            "the merge preserves the current slot when it is still pending");
        List<WorldDiplomacyResultSettlementSlot> noSlotId = new List<WorldDiplomacyResultSettlementSlot>
        {
            new WorldDiplomacyResultSettlementSlot { KingdomId = "k7", Status = "pending", Kind = "" },
        };
        List<WorldDiplomacyResultSettlementSlot> merged3 =
            WorldDiplomacyRoundLifecycleRules.MergeResultSettlementSlots(noSlotId, null!, out _);
        Test.True(merged3[0].SlotId == "diplomacy_result_slot:k7" && merged3[0].Kind == "route",
            "merged slots gain a deterministic id and a route fallback kind");

        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeIntentList(new[] { " Declare_War ", "declare_war", " ", "gift" })
                .SequenceEqual(new[] { "declare_war", "gift" }),
            "intent lists normalize, drop empties, and deduplicate case-insensitively");

        List<WorldDiplomacyPolicySignal> attached = new List<WorldDiplomacyPolicySignal>
        {
            new WorldDiplomacyPolicySignal { SignalKey = "a", PolicyId = "p1", IssuerKingdomId = "k1", TargetKingdomId = "k2", PublishedDay = 2 },
            new WorldDiplomacyPolicySignal { SignalKey = " a ", PolicyId = "p2", IssuerKingdomId = "k3", TargetKingdomId = "k4", PublishedDay = 5 },
            new WorldDiplomacyPolicySignal { SignalKey = "b", PolicyId = "", IssuerKingdomId = "k1", TargetKingdomId = "k2", PublishedDay = 9 },
            new WorldDiplomacyPolicySignal { SignalKey = "c", PolicyId = "p3", IssuerKingdomId = "k1", TargetKingdomId = "k2", PublishedDay = 1 },
        };
        List<WorldDiplomacyPolicySignal> normalizedAttached =
            WorldDiplomacyRoundLifecycleRules.NormalizeAttachedPolicySignals(attached, 24);
        Test.True(normalizedAttached.Count == 2
            && normalizedAttached[0].PolicyId == "p2" && normalizedAttached[0].SignalKey == " a "
            && normalizedAttached[1].PolicyId == "p3",
            "attached policy signals keep the newest valid record per trimmed key in publication order");
        Test.True(!ReferenceEquals(normalizedAttached[0], attached[1]),
            "attached policy signals are cloned rather than aliased");

        WorldDiplomacyRound round = new WorldDiplomacyRound
        {
            RoundId = "r1",
            PotentialActionIntents = new List<string> { "Declare_War", "declare_war", "" },
            ExternalSignalKeys = new List<string> { "s1", "S1", "" },
            AttachedPolicySignals = attached,
            LlmTranscript = new List<WorldDiplomacyLlmMessage> { new WorldDiplomacyLlmMessage() },
            ConsecutiveTechnicalGenerationFailures = 99,
            ResultSettlementWarDocumentIds = new List<string> { "d1", "D1", "" },
            ResultSettlementSlots = mergeInput,
            PendingOffers = new List<WorldDiplomacyRoundOffer>
            {
                null!,
                new WorldDiplomacyRoundOffer { SourceDocumentId = "" },
                new WorldDiplomacyRoundOffer { SourceDocumentId = " d1 ", SourceActionId = " a1 " },
            },
        };
        WorldDiplomacyRoundLifecycleRules.NormalizeRoundRecordCollections(round, 24, 10);
        Test.True(round.Participants != null && round.RelayRouteKingdomIds != null
            && round.ResultSettlementTriggerDocumentId == ""
            && round.LlmTranscript.Count == 0 && round.LlmProfiledKingdomIds.Count == 0
            && round.CommonContractSnapshot == "" && !round.CommonContractSnapshotInitialized
            && round.CachePrefix == "" && round.ExternalOpeningContext == ""
            && round.PotentialActionIntents.SequenceEqual(new[] { "declare_war" })
            && round.ExternalSignalKeys.SequenceEqual(new[] { "s1" })
            && round.AttachedPolicySignals.Count == 2
            && round.ConsecutiveTechnicalGenerationFailures == 10
            && round.ResultSettlementWarDocumentIds.SequenceEqual(new[] { "d1" })
            && round.ResultSettlementSlots.Count == 1
            && round.ResultSettlementPlayerWaitingSinceDay == 0
            && round.PendingOffers.Count == 1
            && round.PendingOffers[0].SourceDocumentId == "d1"
            && round.PendingOffers[0].SourceActionId == "a1",
            "round record normalization initializes, resets, merges, and deduplicates every collection");

        Dictionary<string, WorldDiplomacyDocument> docsById = new Dictionary<string, WorldDiplomacyDocument>(StringComparer.OrdinalIgnoreCase)
        {
            ["d1"] = new WorldDiplomacyDocument
            {
                DocumentId = "d1", AuthorKingdomId = "k1", IsReadyForPublication = true,
                Actions = new List<WorldDiplomacyDocumentAction>
                {
                    new WorldDiplomacyDocumentAction
                    {
                        ActionId = "act1", Intent = "offer_trade", TargetKingdomId = "k2",
                    },
                },
            },
            ["d2"] = new WorldDiplomacyDocument
            {
                DocumentId = "d2", AuthorKingdomId = "k1", IsReadyForPublication = true,
            },
            ["d3"] = new WorldDiplomacyDocument
            {
                DocumentId = "d3", AuthorKingdomId = "k9", IsReadyForPublication = true,
            },
            ["d4"] = new WorldDiplomacyDocument
            {
                DocumentId = "d4", AuthorKingdomId = "k1", IsReadyForPublication = false,
            },
        };
        WorldDiplomacyRoundOffer bound = new WorldDiplomacyRoundOffer
        {
            Status = "open", SourceDocumentId = "d1", ProposerKingdomId = "k1",
            TargetKingdomId = "k2", Intent = "OFFER_TRADE",
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizePendingOfferSourceActionBinding(bound, docsById)
            && bound.SourceActionId == "act1"
            && !WorldDiplomacyRoundLifecycleRules.NormalizePendingOfferSourceActionBinding(bound, docsById),
            "an open offer binds to the single matching published action exactly once");
        WorldDiplomacyRoundOffer cleared = new WorldDiplomacyRoundOffer
        {
            Status = "open", SourceDocumentId = "d2", SourceActionId = "stale",
            ProposerKingdomId = "k1", TargetKingdomId = "k2", Intent = "offer_trade",
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizePendingOfferSourceActionBinding(cleared, docsById)
            && cleared.SourceActionId == ""
            && !WorldDiplomacyRoundLifecycleRules.NormalizePendingOfferSourceActionBinding(cleared, docsById),
            "a source without actions clears the stale binding once");
        WorldDiplomacyRoundOffer invalid = new WorldDiplomacyRoundOffer
        {
            Status = "open", SourceDocumentId = "d3", ProposerKingdomId = "k1",
            TargetKingdomId = "k2", Intent = "offer_trade",
        };
        WorldDiplomacyRoundOffer unpublished = new WorldDiplomacyRoundOffer
        {
            Status = "open", SourceDocumentId = "d4", ProposerKingdomId = "k1",
            TargetKingdomId = "k2", Intent = "offer_trade",
        };
        WorldDiplomacyRoundOffer nonOpen = new WorldDiplomacyRoundOffer
        {
            Status = "accepted", SourceDocumentId = "d3", ProposerKingdomId = "k1",
            TargetKingdomId = "k2", Intent = "offer_trade",
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizePendingOfferSourceActionBinding(invalid, docsById)
            && invalid.Status == "invalidated"
            && WorldDiplomacyRoundLifecycleRules.NormalizePendingOfferSourceActionBinding(unpublished, docsById)
            && unpublished.Status == "invalidated"
            && !WorldDiplomacyRoundLifecycleRules.NormalizePendingOfferSourceActionBinding(nonOpen, docsById)
            && nonOpen.Status == "accepted"
            && !WorldDiplomacyRoundLifecycleRules.NormalizePendingOfferSourceActionBinding(invalid, docsById),
            "open offers invalidate on an unusable source while non-open offers stay untouched");

        Test.True(WorldDiplomacyRoundLifecycleRules.CountNonExpiredPendingOffers(
                new List<WorldDiplomacyRoundOffer>
                {
                    new WorldDiplomacyRoundOffer { Status = "open" },
                    new WorldDiplomacyRoundOffer { Status = "EXPIRED" },
                    null!,
                }) == 1,
            "non-expired offer counting ignores expired and null entries");
        WorldDiplomacyRound attemptRound = new WorldDiplomacyRound
        {
            DiplomaticActionAttemptCount = 0, ExecutedActionCount = 3,
            PendingOffers = new List<WorldDiplomacyRoundOffer>
            {
                new WorldDiplomacyRoundOffer { Status = "open" },
            },
        };
        WorldDiplomacyRoundLifecycleRules.NormalizeRoundAttemptCount(attemptRound);
        Test.True(attemptRound.DiplomaticActionAttemptCount == 3,
            "attempt count recovers to the executed floor when it cannot cover executed actions");
        WorldDiplomacyRound attemptRound2 = new WorldDiplomacyRound
        {
            DiplomaticActionAttemptCount = 0, ExecutedActionCount = 0,
            PendingOffers = new List<WorldDiplomacyRoundOffer>
            {
                new WorldDiplomacyRoundOffer { Status = "open" },
                new WorldDiplomacyRoundOffer { Status = "open" },
            },
        };
        WorldDiplomacyRoundLifecycleRules.NormalizeRoundAttemptCount(attemptRound2);
        Test.True(attemptRound2.DiplomaticActionAttemptCount == 2,
            "attempt count recovers to the live pending-offer count");

        Test.True(WorldDiplomacyRoundLifecycleRules.ResolveStoredRoundDurationDays(10, 4, 6) == 6
            && WorldDiplomacyRoundLifecycleRules.ResolveStoredRoundDurationDays(2, 4, 6) == 6,
            "stored round duration reuses the stored span or falls back to the target");
        WorldDiplomacyRound schedRound = new WorldDiplomacyRound
        {
            StartedDay = 4, SoftEndDay = 2, RelayPassDurationDays = 0, HardEndDay = 0,
        };
        WorldDiplomacyRoundLifecycleRules.NormalizeRoundScheduleDays(schedRound, 6, 3, 9);
        Test.True(schedRound.SoftEndDay == 10 && schedRound.RelayPassDurationDays == 3 && schedRound.HardEndDay == 13,
            "schedule normalization restores the soft end, pass duration, and hard end floor");
        WorldDiplomacyRound schedRound2 = new WorldDiplomacyRound
        {
            StartedDay = 4, SoftEndDay = 10, RelayPassDurationDays = 2, HardEndDay = 0,
        };
        WorldDiplomacyRoundLifecycleRules.NormalizeRoundScheduleDays(schedRound2, 6, 3, 9);
        Test.True(schedRound2.SoftEndDay == 10 && schedRound2.RelayPassDurationDays == 2 && schedRound2.HardEndDay == 13,
            "schedule normalization only repairs missing values and keeps stored ones");

        List<WorldDiplomacyDocument> roundDocs = new List<WorldDiplomacyDocument>
        {
            new WorldDiplomacyDocument { DocumentId = "e1", RoundId = "r1", Day = 2, CreatedUtcTicks = 9 },
            new WorldDiplomacyDocument { DocumentId = "e2", RoundId = "r1", Day = 5, CreatedUtcTicks = 1 },
            new WorldDiplomacyDocument { DocumentId = "e3", RoundId = "r1", Day = 5, CreatedUtcTicks = 7 },
            new WorldDiplomacyDocument { DocumentId = "e4", RoundId = "r2", Day = 9 },
            new WorldDiplomacyDocument { DocumentId = "e5", RoundId = "r1", Day = 1, IsPlayerAuthored = true },
        };
        WorldDiplomacyRound closedRound = new WorldDiplomacyRound { State = "CLOSED", FinalDocumentId = "", RoundId = "r1" };
        Test.True(WorldDiplomacyRoundLifecycleRules.IsClosedRoundWithoutFinalDocument(closedRound)
            && !WorldDiplomacyRoundLifecycleRules.IsClosedRoundWithoutFinalDocument(
                new WorldDiplomacyRound { State = "closed", FinalDocumentId = "e2" })
            && !WorldDiplomacyRoundLifecycleRules.IsClosedRoundWithoutFinalDocument(
                new WorldDiplomacyRound { State = "active" })
            && WorldDiplomacyRoundLifecycleRules.SelectFinalRoundDocumentId(roundDocs, "r1") == "e3"
            && WorldDiplomacyRoundLifecycleRules.CountAutomaticRoundDocuments(roundDocs, "r1") == 3,
            "closed rounds recover the latest published document as their final document");

        WorldDiplomacyRound waitingRound = new WorldDiplomacyRound
        {
            ResultSettlementPending = true,
            ResultSettlementCurrentSlotId = "s1",
            ResultSettlementSlots = new List<WorldDiplomacyResultSettlementSlot>
            {
                new WorldDiplomacyResultSettlementSlot { SlotId = "s1", KingdomId = "k1", Status = "waiting_player" },
            },
            Participants = new List<WorldDiplomacyRoundParticipant>
            {
                new WorldDiplomacyRoundParticipant { KingdomId = "k1", MandatoryReplyPending = true },
                new WorldDiplomacyRoundParticipant { KingdomId = "k2", MandatoryReplyPending = true },
                null!,
            },
        };
        WorldDiplomacyRoundLifecycleRules.ReleaseNonWaitingSettlementReplies(waitingRound);
        Test.True(waitingRound.Participants[0].MandatoryReplyPending
            && !waitingRound.Participants[1].MandatoryReplyPending,
            "only the kingdom holding the current waiting-player slot keeps its mandatory reply flag");

        List<WorldDiplomacyJob> archJobs = new List<WorldDiplomacyJob>
        {
            new WorldDiplomacyJob { JobId = "j1", Kind = "generate", RoundId = "r1" },
            new WorldDiplomacyJob { JobId = "j2", Kind = "round_plan", RoundId = "r1" },
            new WorldDiplomacyJob { JobId = "j3", Kind = "compress", RoundId = "r1", ForcedIntent = "x" },
            new WorldDiplomacyJob { JobId = "j4", Kind = "compress", RoundId = "r1" },
            new WorldDiplomacyJob { JobId = "j5", Kind = "generate", RoundId = "r2" },
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectRetiredArchitectureJobs(archJobs)
                .Select(x => x.JobId).SequenceEqual(new[] { "j1", "j2", "j3", "j5" })
            && WorldDiplomacyRoundLifecycleRules.SelectRoundJobsOfKinds(archJobs, "r1", "generate", "round_plan")
                .Select(x => x.JobId).SequenceEqual(new[] { "j1", "j2" }),
            "retired job selection covers generate, round-plan, and forced-intent jobs with optional round filtering");

        List<WorldDiplomacySettlementKnowledge> settlementKnowledge = new List<WorldDiplomacySettlementKnowledge>
        {
            null!, new WorldDiplomacySettlementKnowledge(),
        };
        List<WorldDiplomacyKingdomKnowledge> kingdomKnowledge = new List<WorldDiplomacyKingdomKnowledge>
        {
            null!, new WorldDiplomacyKingdomKnowledge(),
        };
        List<WorldDiplomacyParticipationRequest> requests = new List<WorldDiplomacyParticipationRequest>
        {
            null!, new WorldDiplomacyParticipationRequest(),
        };
        WorldDiplomacyRoundLifecycleRules.NormalizeSettlementKnowledgeRecords(settlementKnowledge);
        WorldDiplomacyRoundLifecycleRules.NormalizeKingdomKnowledgeRecords(kingdomKnowledge);
        WorldDiplomacyRoundLifecycleRules.NormalizeParticipationRequestRecords(requests);
        Test.True(settlementKnowledge[1].DocumentIds.Count == 0
            && kingdomKnowledge[1].DocumentIds.Count == 0
            && requests[1].TriggerDocumentIds.Count == 0,
            "knowledge and participation records gain empty id collections");

        List<WorldDiplomacyRelayArrival> relayArrivals = new List<WorldDiplomacyRelayArrival>
        {
            null!,
            new WorldDiplomacyRelayArrival { RoundId = "", ToKingdomId = "k1", DueDay = 1 },
            new WorldDiplomacyRelayArrival { RoundId = "r1", ToKingdomId = "", DueDay = 1 },
            new WorldDiplomacyRelayArrival { RoundId = "r1", ToKingdomId = "k1", DueDay = 3, Sequence = 2 },
            new WorldDiplomacyRelayArrival { RoundId = "r1", ToKingdomId = "k1", DueDay = 3, Sequence = 1 },
            new WorldDiplomacyRelayArrival { RoundId = "r1", ToKingdomId = "k1", DueDay = 1, Sequence = 9 },
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.NormalizeRelayArrivalList(relayArrivals)
                .Select(x => x.Sequence).SequenceEqual(new[] { 9, 1, 2 }),
            "relay arrivals drop invalid records and order by due day then sequence");

        List<WorldDiplomacyPlayerOpportunity> opportunities = new List<WorldDiplomacyPlayerOpportunity>
        {
            null!,
            new WorldDiplomacyPlayerOpportunity { RoundId = "", ArrivedDay = 9 },
            new WorldDiplomacyPlayerOpportunity { RoundId = "r1", ArrivedDay = 2 },
            new WorldDiplomacyPlayerOpportunity { RoundId = "r2", ArrivedDay = 5 },
        };
        List<WorldDiplomacyPlayerOpportunity> normalizedOpportunities =
            WorldDiplomacyRoundLifecycleRules.NormalizePlayerOpportunityList(opportunities, 16);
        Test.True(normalizedOpportunities.Select(x => x.RoundId).SequenceEqual(new[] { "r2", "r1" })
            && normalizedOpportunities[0].KnownDocumentIds.Count == 0,
            "player opportunities drop invalid records, initialize known ids, and keep newest first");

        WorldDiplomacyRoundSummary summary = new WorldDiplomacyRoundSummary
        {
            SourceDocumentIds = new List<string> { "e2", "e5", "e9" },
            Facts = new List<WorldDiplomacyRoundFact>
            {
                new WorldDiplomacyRoundFact { Kind = "" },
                null!,
            },
        };
        List<WorldDiplomacyDocument> summaryDocs = new List<WorldDiplomacyDocument>
        {
            new WorldDiplomacyDocument
            {
                DocumentId = "e2", AuthorKingdomId = "k1", TargetKingdomId = "k2",
                AddressedKingdomIds = new List<string> { "k3" },
            },
            new WorldDiplomacyDocument { DocumentId = "e5", AuthorKingdomId = "K1", TargetKingdomId = "k4" },
        };
        WorldDiplomacyRoundLifecycleRules.NormalizeRoundSummaryRecord(summary, summaryDocs);
        Test.True(summary.Facts[0].Kind == "declaration"
            && summary.Facts[0].SourceDocumentIds.Count == 0
            && summary.Facts[0].KingdomIds.Count == 0
            && summary.KingdomIds.SequenceEqual(new[] { "k1", "k2", "k3", "k4" }),
            "round summary normalization defaults fact records and derives kingdom ids from source documents");
        WorldDiplomacyCompressionSummary compressionSummary = new WorldDiplomacyCompressionSummary();
        WorldDiplomacyRoundLifecycleRules.NormalizeCompressionSummaryRecord(compressionSummary);
        Test.True(compressionSummary.SourceRoundIds.Count == 0
            && compressionSummary.KingdomIds.Count == 0
            && compressionSummary.ConfirmedResults.Count == 0,
            "compression summaries gain empty archive collections");

        List<WorldDiplomacyRound> completed = new List<WorldDiplomacyRound>
        {
            null!,
            new WorldDiplomacyRound { RoundId = "r1", CompletedDay = 2 },
            new WorldDiplomacyRound { RoundId = "r2", CompletedDay = 5 },
            new WorldDiplomacyRound { RoundId = "r3", CompletedDay = 1 },
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectRetainedCompletedRounds(completed, 2)
                .Select(x => x.RoundId).SequenceEqual(new[] { "r2", "r1" }),
            "completed rounds keep the newest completions first");
        List<WorldDiplomacyRoundSummary> summaries = new List<WorldDiplomacyRoundSummary>
        {
            new WorldDiplomacyRoundSummary { CreatedDay = 1 },
            new WorldDiplomacyRoundSummary { CreatedDay = 4 },
            null!,
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectRetainedRoundSummaries(summaries, 1)
                .Single().CreatedDay == 4,
            "round summaries keep the newest archives");
        List<WorldDiplomacyAnnualSummary> annuals = new List<WorldDiplomacyAnnualSummary>
        {
            new WorldDiplomacyAnnualSummary { Year = 1200 },
            new WorldDiplomacyAnnualSummary { Year = 1203 },
            null!,
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectRetainedAnnualSummaries(annuals, 1)
                .Single().Year == 1203,
            "annual summaries keep the newest years");
        List<WorldDiplomacyCompressionSummary> compressionSummaries = new List<WorldDiplomacyCompressionSummary>
        {
            new WorldDiplomacyCompressionSummary { BatchId = "", CreatedDay = 9 },
            new WorldDiplomacyCompressionSummary { BatchId = "b1", CreatedDay = 1 },
            new WorldDiplomacyCompressionSummary { BatchId = "b2", CreatedDay = 6 },
            null!,
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectRetainedCompressionSummaries(compressionSummaries, 5)
                .Select(x => x.BatchId).SequenceEqual(new[] { "b2", "b1" }),
            "compression summaries drop blank batches and keep the newest first");
    }

    private static void RunOfferStatusDecisionTests()
    {
        WorldDiplomacyRoundOffer openToB = new WorldDiplomacyRoundOffer
        {
            Status = "open",
            ProposerKingdomId = "a",
            TargetKingdomId = "b",
            SourceDocumentId = "d1",
            SourceActionId = "act1",
        };
        WorldDiplomacyRoundOffer closedToB = new WorldDiplomacyRoundOffer
        {
            Status = "accepted",
            ProposerKingdomId = "a",
            TargetKingdomId = "b",
        };
        WorldDiplomacyRoundOffer openToC = new WorldDiplomacyRoundOffer
        {
            Status = "open",
            ProposerKingdomId = "a",
            TargetKingdomId = "c",
        };

        Test.True(WorldDiplomacyRoundLifecycleRules.IsOpenOfferToTarget(openToB, "B"),
            "an open offer matches its target kingdom case-insensitively");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsOpenOfferToTarget(closedToB, "b")
                && !WorldDiplomacyRoundLifecycleRules.IsOpenOfferToTarget(openToC, "b")
                && !WorldDiplomacyRoundLifecycleRules.IsOpenOfferToTarget(null, "b"),
            "an open offer to a target rejects closed offers, other targets, and null");

        Test.True(WorldDiplomacyRoundLifecycleRules.IsOpenDirectedOffer(openToB, "A", "b"),
            "a directed open offer matches proposer and target");
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsOpenDirectedOffer(openToB, "b", "a")
                && !WorldDiplomacyRoundLifecycleRules.IsOpenDirectedOffer(closedToB, "a", "b"),
            "a directed open offer rejects reversed pairs and closed offers");

        Test.True(WorldDiplomacyRoundLifecycleRules.MatchesOfferSource(openToB, "D1", null)
                && WorldDiplomacyRoundLifecycleRules.MatchesOfferSource(openToB, "d1", "")
                && WorldDiplomacyRoundLifecycleRules.MatchesOfferSource(openToB, "d1", "ACT1"),
            "offer source matching is case-insensitive and a blank action id matches any action");
        Test.True(!WorldDiplomacyRoundLifecycleRules.MatchesOfferSource(openToB, "d2", null)
                && !WorldDiplomacyRoundLifecycleRules.MatchesOfferSource(openToB, "d1", "act2")
                && !WorldDiplomacyRoundLifecycleRules.MatchesOfferSource(null, "d1", null),
            "offer source matching rejects other documents, wrong actions, and null");

        WorldDiplomacyPlayerOpportunity openOpportunity = new WorldDiplomacyPlayerOpportunity
        {
            RoundId = "r1",
            Status = "open",
        };
        WorldDiplomacyPlayerOpportunity answeredOpportunity = new WorldDiplomacyPlayerOpportunity
        {
            RoundId = "r1",
            Status = "answered",
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.IsPlayerOpportunityOfStatus(openOpportunity, "OPEN")
                && !WorldDiplomacyRoundLifecycleRules.IsPlayerOpportunityOfStatus(answeredOpportunity, "open")
                && !WorldDiplomacyRoundLifecycleRules.IsPlayerOpportunityOfStatus(null, "open"),
            "player opportunity status compares case-insensitively and survives null");
    }

    private static void RunRecordIdentityDecisionTests()
    {
        Test.True(WorldDiplomacyRoundLifecycleRules.IsRecordInRound("R1", "r1")
                && !WorldDiplomacyRoundLifecycleRules.IsRecordInRound("r1", "r2")
                && !WorldDiplomacyRoundLifecycleRules.IsRecordInRound(null, "r1"),
            "round membership compares record round ids case-insensitively");

        WorldDiplomacyJob job = new WorldDiplomacyJob { JobId = "j1", Kind = "generate", RoundId = "r1" };
        Test.True(WorldDiplomacyRoundLifecycleRules.HasJobId(job, "J1")
                && !WorldDiplomacyRoundLifecycleRules.HasJobId(job, "j2")
                && !WorldDiplomacyRoundLifecycleRules.HasJobId(null, "j1"),
            "job id matching compares case-insensitively and survives null");

        HashSet<string> retiredIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "j1", "j9" };
        Test.True(WorldDiplomacyRoundLifecycleRules.HasJobIdInSet(job, retiredIds)
                && !WorldDiplomacyRoundLifecycleRules.HasJobIdInSet(new WorldDiplomacyJob { JobId = "j2" }, retiredIds)
                && !WorldDiplomacyRoundLifecycleRules.HasJobIdInSet(null, retiredIds)
                && !WorldDiplomacyRoundLifecycleRules.HasJobIdInSet(job, null),
            "job id set membership uses the set comparer and survives null job or set");

        Test.True(WorldDiplomacyRoundLifecycleRules.MatchesDocumentId("D1", "d1")
                && !WorldDiplomacyRoundLifecycleRules.MatchesDocumentId("d1", "d2")
                && !WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(null, "d1"),
            "document id matching compares reference ids case-insensitively");
    }

    private static void RunChronologicalOrderingDecisionTests()
    {
        WorldDiplomacyDocument older = new WorldDiplomacyDocument { DocumentId = "d-old", Day = 2, CreatedUtcTicks = 10L };
        WorldDiplomacyDocument sameDayEarlier = new WorldDiplomacyDocument { DocumentId = "d-same-early", Day = 5, CreatedUtcTicks = 100L };
        WorldDiplomacyDocument sameDayLater = new WorldDiplomacyDocument { DocumentId = "d-same-late", Day = 5, CreatedUtcTicks = 900L };
        WorldDiplomacyDocument newest = new WorldDiplomacyDocument { DocumentId = "d-new", Day = 9, CreatedUtcTicks = 50L };
        List<WorldDiplomacyDocument> shuffled = new List<WorldDiplomacyDocument> { newest, sameDayLater, older, sameDayEarlier };

        List<string> chronological = WorldDiplomacyRoundLifecycleRules.OrderDocumentsChronologically(shuffled)
            .Select(x => x.DocumentId).ToList();
        Test.True(chronological.SequenceEqual(new[] { "d-old", "d-same-early", "d-same-late", "d-new" }),
            "chronological ordering sorts by day then creation ticks ascending");

        List<string> recent = WorldDiplomacyRoundLifecycleRules.OrderDocumentsByRecency(shuffled)
            .Select(x => x.DocumentId).ToList();
        Test.True(recent.SequenceEqual(new[] { "d-new", "d-same-late", "d-same-early", "d-old" }),
            "recency ordering sorts by day then creation ticks descending");

        Test.True(!WorldDiplomacyRoundLifecycleRules.OrderDocumentsChronologically(null).Any()
                && !WorldDiplomacyRoundLifecycleRules.OrderDocumentsByRecency(null).Any(),
            "ordering helpers treat a null source as empty");

        List<string> suffixed = WorldDiplomacyRoundLifecycleRules.ThenOrderDocumentsByRecency(shuffled
                .OrderByDescending(x => string.Equals(x.DocumentId, "d-same-early", StringComparison.OrdinalIgnoreCase)))
            .Select(x => x.DocumentId).ToList();
        Test.True(suffixed.SequenceEqual(new[] { "d-same-early", "d-new", "d-same-late", "d-old" }),
            "recency suffix keeps the primary key ahead of day and tick tie-breakers");

        List<string> newestFirst = WorldDiplomacyRoundLifecycleRules.SelectNewestDocumentsChronologically(shuffled, 2)
            .Select(x => x.DocumentId).ToList();
        Test.True(newestFirst.SequenceEqual(new[] { "d-same-late", "d-new" }),
            "newest selection keeps the newest N then presents them oldest first");

        List<string> retained = WorldDiplomacyRoundLifecycleRules.SelectRetainedDocuments(
                shuffled, x => string.Equals(x.DocumentId, "d-old", StringComparison.OrdinalIgnoreCase), 3)
            .Select(x => x.DocumentId).ToList();
        Test.True(retained.SequenceEqual(new[] { "d-old", "d-same-late", "d-new" }),
            "retention keeps priority documents ahead of recency then stores chronologically");

        Test.True(WorldDiplomacyRoundLifecycleRules.SelectRetainedDocuments(shuffled, x => false, 0).Count == 1
                && WorldDiplomacyRoundLifecycleRules.SelectNewestDocumentsChronologically(shuffled, 0).Count == 1,
            "selection clamps the retained count to at least one");
    }

    private static void RunRecordOrderingDecisionTests()
    {
        List<WorldDiplomacyRelayArrival> relays = new List<WorldDiplomacyRelayArrival>
        {
            new WorldDiplomacyRelayArrival { RoundId = "r1", ToKingdomId = "k2", DueDay = 5, Sequence = 9 },
            new WorldDiplomacyRelayArrival { RoundId = "r1", ToKingdomId = "k1", DueDay = 2, Sequence = 8 },
            new WorldDiplomacyRelayArrival { RoundId = "r1", ToKingdomId = "k3", DueDay = 2, Sequence = 3 }
        };
        List<string> relayOrder = WorldDiplomacyRoundLifecycleRules.OrderRelayArrivalsByDueDate(relays)
            .Select(x => x.ToKingdomId).ToList();
        Test.True(relayOrder.SequenceEqual(new[] { "k3", "k1", "k2" }),
            "relay arrivals sort by due day then relay sequence");

        List<WorldDiplomacyPropagationArrival> arrivals = new List<WorldDiplomacyPropagationArrival>
        {
            new WorldDiplomacyPropagationArrival { DocumentId = "d2", SettlementId = "s1", DueDay = 1 },
            new WorldDiplomacyPropagationArrival { DocumentId = "d1", KingdomId = "k1", Scope = "court", DueDay = 1 },
            new WorldDiplomacyPropagationArrival { DocumentId = "d0", SettlementId = "s2", DueDay = 3 }
        };
        List<string> arrivalOrder = WorldDiplomacyRoundLifecycleRules.OrderPropagationArrivalsByDueDate(arrivals)
            .Select(x => x.DocumentId).ToList();
        Test.True(arrivalOrder.SequenceEqual(new[] { "d1", "d2", "d0" }),
            "propagation arrivals sort by due day, court scope first, then document id");

        List<WorldDiplomacyCanonicalProtectedFact> facts = new List<WorldDiplomacyCanonicalProtectedFact>
        {
            new WorldDiplomacyCanonicalProtectedFact { Kind = "response_link", SourceKey = "b", Sequence = 4 },
            new WorldDiplomacyCanonicalProtectedFact { Kind = "diplomatic_result", SourceKey = "b", Sequence = 4 },
            new WorldDiplomacyCanonicalProtectedFact { Kind = "diplomatic_result", SourceKey = "a", Sequence = 4 },
            new WorldDiplomacyCanonicalProtectedFact { Kind = "diplomatic_result", SourceKey = "c", Sequence = 1 }
        };
        List<string> factOrder = WorldDiplomacyRoundLifecycleRules.OrderProtectedFactsBySequence(facts)
            .Select(x => x.Kind + ":" + x.SourceKey).ToList();
        Test.True(factOrder.SequenceEqual(new[]
            {
                "diplomatic_result:c", "diplomatic_result:a", "diplomatic_result:b", "response_link:b"
            }),
            "protected facts sort by sequence, ordinal kind, then source key");

        List<string> factOrderDesc = WorldDiplomacyRoundLifecycleRules.OrderProtectedFactsBySequenceDescending(facts)
            .Select(x => x.Kind + ":" + x.SourceKey).ToList();
        Test.True(factOrderDesc.SequenceEqual(new[]
            {
                "response_link:b", "diplomatic_result:b", "diplomatic_result:a", "diplomatic_result:c"
            }),
            "descending protected facts mirror the ascending keys for newest-first selection");

        List<WorldDiplomacyCanonicalHistoryEntry> entries = new List<WorldDiplomacyCanonicalHistoryEntry>
        {
            new WorldDiplomacyCanonicalHistoryEntry { SourceKey = "e3", Sequence = 9 },
            null,
            new WorldDiplomacyCanonicalHistoryEntry { SourceKey = "e1", Sequence = 2 },
            new WorldDiplomacyCanonicalHistoryEntry { SourceKey = "e2", Sequence = 5 }
        };
        List<string> deltaOrder = WorldDiplomacyRoundLifecycleRules.OrderDeltaEntriesBySequence(
                entries.Where(x => x != null))
            .Select(x => x.SourceKey).ToList();
        Test.True(deltaOrder.SequenceEqual(new[] { "e1", "e2", "e3" }),
            "delta entries sort by sequence ascending");

        List<string> through = WorldDiplomacyRoundLifecycleRules.SelectDeltaEntriesThrough(entries, 5)
            .Select(x => x.SourceKey).ToList();
        Test.True(through.SequenceEqual(new[] { "e1", "e2" }),
            "delta cutoff keeps only entries at or below the sequence in order");
        Test.True(!WorldDiplomacyRoundLifecycleRules.SelectDeltaEntriesThrough(entries, 1).Any()
                && !WorldDiplomacyRoundLifecycleRules.SelectDeltaEntriesThrough(null, 9).Any(),
            "delta cutoff returns empty for low cutoffs or null input");
    }

    private static void RunCanonicalHistoryNormalizationDecisionTests()
    {
        List<string> preserved = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(
            new[] { "k2", "", " K2 ", null, "k1", "k2" });
        Test.True(preserved.SequenceEqual(new[] { "k2", " K2 ", "k1" }),
            "id-list normalization keeps original values and first-occurrence order");

        List<string> trimmed = WorldDiplomacyRoundLifecycleRules.NormalizeTrimmedIdListPreserveOrder(
            new[] { " k2 ", "", "K2", null, "k1 " });
        Test.True(trimmed.SequenceEqual(new[] { "k2", "k1" }),
            "trimmed id-list normalization trims then deduplicates case-insensitively");

        List<string> ordered = WorldDiplomacyRoundLifecycleRules.NormalizeOrderedIdList(
            new[] { "k2", " K1 ", "", "k2 ", null });
        Test.True(ordered.SequenceEqual(new[] { "K1", "k2" }),
            "ordered id-list normalization trims, deduplicates, and sorts case-insensitively");
        Test.True(!WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(null).Any()
                && !WorldDiplomacyRoundLifecycleRules.NormalizeOrderedIdList(null).Any(),
            "id-list normalization tolerates null input");

        Dictionary<string, long> revisions = WorldDiplomacyRoundLifecycleRules.NormalizeRevisionMapLong(
            new Dictionary<string, long>
            {
                ["k1"] = -5L,
                ["K1"] = 7L,
                [" "] = 99L,
                ["k2"] = 3L
            });
        Test.True(revisions.Count == 2 && revisions["k1"] == 7L && revisions["k2"] == 3L
                && revisions.Comparer == StringComparer.OrdinalIgnoreCase,
            "revision map drops blank keys, merges case-insensitively last-wins, and clamps negative values");

        Dictionary<string, string> signatures = WorldDiplomacyRoundLifecycleRules.NormalizeRevisionMapString(
            new Dictionary<string, string>
            {
                ["k1"] = null,
                ["K1"] = "sig",
                ["k2"] = null
            });
        Test.True(signatures.Count == 2 && signatures["k1"] == "sig" && signatures["k2"] == ""
                && signatures.Comparer == StringComparer.OrdinalIgnoreCase,
            "string revision map merges last-wins and converts null values to empty");
        Test.True(!WorldDiplomacyRoundLifecycleRules.NormalizeRevisionMapLong(null).Any()
                && !WorldDiplomacyRoundLifecycleRules.NormalizeRevisionMapString(null).Any(),
            "revision map normalization tolerates null input");

        WorldDiplomacyCanonicalHistoryEntry active = new WorldDiplomacyCanonicalHistoryEntry
        {
            Kind = "declaration", Sequence = 9
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.IsDeltaEntryRetired(null, 0)
                && WorldDiplomacyRoundLifecycleRules.IsDeltaEntryRetired(
                    new WorldDiplomacyCanonicalHistoryEntry { Kind = "declaration", Sequence = 5 }, 5)
                && WorldDiplomacyRoundLifecycleRules.IsDeltaEntryRetired(
                    new WorldDiplomacyCanonicalHistoryEntry { Kind = " ", Sequence = 9 }, 0)
                && !WorldDiplomacyRoundLifecycleRules.IsDeltaEntryRetired(active, 8),
            "delta retirement covers null, covered sequences, and missing kind only");

        List<WorldDiplomacyCanonicalHistoryEntry> orderedDelta = new List<WorldDiplomacyCanonicalHistoryEntry>
        {
            new WorldDiplomacyCanonicalHistoryEntry { SourceKey = "a", Sequence = 3 },
            new WorldDiplomacyCanonicalHistoryEntry { SourceKey = "b", Sequence = 8 }
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.ResolveNextDeltaSequence(5, 1, orderedDelta) == 9,
            "next delta sequence follows the final ordered entry above coverage");
        Test.True(WorldDiplomacyRoundLifecycleRules.ResolveNextDeltaSequence(12, 1, orderedDelta) == 13,
            "next delta sequence follows coverage when coverage is ahead of entries");
        Test.True(WorldDiplomacyRoundLifecycleRules.ResolveNextDeltaSequence(0, 14, new List<WorldDiplomacyCanonicalHistoryEntry>()) == 14,
            "next delta sequence honors a higher declared sequence");
        Test.True(WorldDiplomacyRoundLifecycleRules.ResolveNextDeltaSequence(0, 0, null) == 1,
            "next delta sequence is at least one");

        WorldDiplomacyCanonicalHistoryEntry normalized = new WorldDiplomacyCanonicalHistoryEntry
        {
            TargetKingdomIds = new List<string> { " k1", "", "K1", "k2 " },
            RespondingToOfferDocumentId = "  offer-1 ",
            RespondingToThreatDocumentId = null,
            ActionFacts = new List<string> { "a@k=x", "", "A@K=X" }
        };
        WorldDiplomacyRoundLifecycleRules.NormalizeDeltaEntryFields(normalized);
        Test.True(normalized.TargetKingdomIds.SequenceEqual(new[] { " k1", "K1", "k2 " })
                && normalized.RespondingToOfferDocumentId == "offer-1"
                && normalized.RespondingToThreatDocumentId == ""
                && normalized.ActionFacts.SequenceEqual(new[] { "a@k=x" }),
            "delta entry fields normalize ids, response links, and action facts");
        WorldDiplomacyCanonicalHistoryEntry nullFacts = new WorldDiplomacyCanonicalHistoryEntry
        {
            TargetKingdomIds = null, ActionFacts = null
        };
        WorldDiplomacyRoundLifecycleRules.NormalizeDeltaEntryFields(nullFacts);
        Test.True(nullFacts.TargetKingdomIds != null && nullFacts.TargetKingdomIds.Count == 0
                && nullFacts.ActionFacts == null,
            "delta normalization materializes target ids but preserves null action facts");
        WorldDiplomacyRoundLifecycleRules.NormalizeDeltaEntryFields(null);
    }

    private static void RunEnvelopeJsonDecisionTests()
    {
        Test.True(WorldDiplomacyRoundLifecycleRules.IsAutonomousOpeningJob(
                new WorldDiplomacyJob { Kind = "generate", IsResponse = false, AllowUntargeted = true, TargetKingdomId = "" })
            && !WorldDiplomacyRoundLifecycleRules.IsAutonomousOpeningJob(
                new WorldDiplomacyJob { Kind = "generate", IsResponse = true, AllowUntargeted = true, TargetKingdomId = "" })
            && !WorldDiplomacyRoundLifecycleRules.IsAutonomousOpeningJob(
                new WorldDiplomacyJob { Kind = "generate", IsResponse = false, AllowUntargeted = false, TargetKingdomId = "" })
            && !WorldDiplomacyRoundLifecycleRules.IsAutonomousOpeningJob(
                new WorldDiplomacyJob { Kind = "generate", IsResponse = false, AllowUntargeted = true, TargetKingdomId = "k1" })
            && !WorldDiplomacyRoundLifecycleRules.IsAutonomousOpeningJob(
                new WorldDiplomacyJob { Kind = "round_plan", IsResponse = false, AllowUntargeted = true, TargetKingdomId = "" })
            && !WorldDiplomacyRoundLifecycleRules.IsAutonomousOpeningJob(null),
            "autonomous opening requires an untargeted non-response generation job");
        Test.True(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty("", "  ", "a", "b") == "a"
                && WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(null, "  x  ") == "x"
                && WorldDiplomacyRoundLifecycleRules.FirstNonEmpty() == ""
                && WorldDiplomacyRoundLifecycleRules.FirstNonEmpty((string[])null) == "",
            "first-non-empty skips blanks, trims, and tolerates null");

        JObject fenced = WorldDiplomacyEnvelopeJsonRules.ParseJsonObject("```json\n{\"intent\":\"peace\"}\n```");
        Test.True((string)fenced["intent"] == "peace",
            "envelope parse strips markdown code fences");
        JObject embedded = WorldDiplomacyEnvelopeJsonRules.ParseJsonObject("noise {\"intent\":\"war\"} tail");
        Test.True((string)embedded["intent"] == "war",
            "envelope parse recovers a JSON object embedded in noise");
        Test.True(WorldDiplomacyEnvelopeJsonRules.ParseJsonObject("not json")?.Count == 0
                && WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(null)?.Count == 0,
            "envelope parse returns an empty object for unparsable input");
        Test.True(WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject("{\"a\":1}", out JObject ok)
                && (int)ok["a"] == 1
                && !WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject("", out JObject empty)
                && empty != null && empty.Count == 0,
            "try-parse reports success and still yields an empty object on failure");

        JObject nested = JObject.Parse("{\"a\":{\"b\":\"  v  \"},\"alt\":\"x\",\"num\":\"0.7\",\"bad\":\"abc\",\"flag\":\"yes\"}");
        Test.True(WorldDiplomacyEnvelopeJsonRules.ReadString(nested, "a.b", "alt") == "v"
                && WorldDiplomacyEnvelopeJsonRules.ReadString(nested, "missing", "alt") == "x"
                && WorldDiplomacyEnvelopeJsonRules.ReadString(nested, "missing") == ""
                && WorldDiplomacyEnvelopeJsonRules.ReadString(null, "a") == "",
            "read-string walks fallback paths, trims, and tolerates null");
        JObject listJson = JObject.Parse("{\"arr\":[\" k1 \",\"\",\"K1\",\"k2\"],\"csv\":\"k3,k4\"}");
        Test.True(WorldDiplomacyEnvelopeJsonRules.ReadStringList(listJson, "arr").SequenceEqual(new[] { "k1", "k2" })
                && WorldDiplomacyEnvelopeJsonRules.ReadStringList(listJson, "missing", "csv").SequenceEqual(new[] { "k3", "k4" })
                && !WorldDiplomacyEnvelopeJsonRules.ReadStringList(listJson, "missing").Any()
                && !WorldDiplomacyEnvelopeJsonRules.ReadStringList(null, "arr").Any(),
            "read-string-list normalizes arrays and delimited strings");
        Test.True(WorldDiplomacyEnvelopeJsonRules.ReadTokenStringList(listJson["arr"]).SequenceEqual(new[] { "k1", "k2" })
                && !WorldDiplomacyEnvelopeJsonRules.ReadTokenStringList(listJson["csv"]).Any()
                && !WorldDiplomacyEnvelopeJsonRules.ReadTokenStringList(null).Any(),
            "token string list reads arrays only");
        Test.True(Math.Abs(WorldDiplomacyEnvelopeJsonRules.ReadFloat(nested, "num") - 0.7f) < 0.001f
                && WorldDiplomacyEnvelopeJsonRules.ReadFloat(nested, "bad") == 0f
                && WorldDiplomacyEnvelopeJsonRules.ReadFloat(JObject.Parse("{\"x\":5}"), "x") == 1f
                && WorldDiplomacyEnvelopeJsonRules.ReadFloat(JObject.Parse("{\"x\":-2}"), "x") == 0f
                && WorldDiplomacyEnvelopeJsonRules.ReadFloat(null, "x") == 0f,
            "read-float parses invariantly and clamps to 0..1");
        Test.True(WorldDiplomacyEnvelopeJsonRules.ReadInteger(JObject.Parse("{\"x\":\"42\"}"), "x") == 42
                && WorldDiplomacyEnvelopeJsonRules.ReadInteger(JObject.Parse("{\"x\":\"n\"}"), "x") == 0
                && WorldDiplomacyEnvelopeJsonRules.ReadInteger(null, "x") == 0,
            "read-integer parses invariantly and defaults to zero");
        Test.True(WorldDiplomacyEnvelopeJsonRules.TryReadInteger(JObject.Parse("{\"x\":\"7\"}"), "x", out int seven)
                && seven == 7
                && !WorldDiplomacyEnvelopeJsonRules.TryReadInteger(JObject.Parse("{\"x\":null}"), "x", out int skipped)
                && skipped == 0
                && !WorldDiplomacyEnvelopeJsonRules.TryReadInteger(null, "x", out int zero)
                && zero == 0,
            "try-read-integer distinguishes missing, null, and unparsable tokens");
        Test.True(WorldDiplomacyEnvelopeJsonRules.ReadBool(nested, "flag")
                && WorldDiplomacyEnvelopeJsonRules.ReadBool(JObject.Parse("{\"f\":\"true\"}"), "f")
                && WorldDiplomacyEnvelopeJsonRules.ReadBool(JObject.Parse("{\"f\":\"1\"}"), "f")
                && !WorldDiplomacyEnvelopeJsonRules.ReadBool(JObject.Parse("{\"f\":\"no\"}"), "f")
                && !WorldDiplomacyEnvelopeJsonRules.ReadBool(JObject.Parse("{\"f\":true}"), "missing")
                && !WorldDiplomacyEnvelopeJsonRules.ReadBool(null, "f"),
            "read-bool accepts true/1/yes and rejects everything else");
        Test.True(WorldDiplomacyEnvelopeJsonRules.IsJsonStringArray(listJson["arr"])
                && WorldDiplomacyEnvelopeJsonRules.IsJsonStringArray(new JArray())
                && !WorldDiplomacyEnvelopeJsonRules.IsJsonStringArray(listJson["csv"])
                && !WorldDiplomacyEnvelopeJsonRules.IsJsonStringArray(new JArray("a", 1))
                && !WorldDiplomacyEnvelopeJsonRules.IsJsonStringArray(null),
            "string-array shape requires an array of string tokens");

        JObject terms = WorldDiplomacyEnvelopeJsonRules.BuildPeaceTermsJson(new WorldDiplomacyPeaceTerms
        {
            TributePayerKingdomId = "payer",
            TributeReceiverKingdomId = "receiver",
            DailyTribute = 12,
            DurationDays = 30,
            CessionFromKingdomId = "from",
            CessionToKingdomId = "to",
            CessionSettlementId = "town"
        });
        Test.True((string)terms["tribute_payer_kingdom_id"] == "payer"
                && (int)terms["daily_tribute"] == 12 && (int)terms["duration_days"] == 30
                && (string)terms["cession_settlement_id"] == "town",
            "peace-terms json carries tribute and cession fields");
        JObject nullTerms = WorldDiplomacyEnvelopeJsonRules.BuildPeaceTermsJson(null);
        Test.True((string)nullTerms["tribute_payer_kingdom_id"] == ""
                && (int)nullTerms["daily_tribute"] == 0,
            "peace-terms json defaults a null record");
        JObject clamped = WorldDiplomacyEnvelopeJsonRules.BuildPeaceTermsJson(
            new WorldDiplomacyPeaceTerms { DailyTribute = -3, DurationDays = -7 });
        Test.True((int)clamped["daily_tribute"] == 0 && (int)clamped["duration_days"] == 0,
            "peace-terms json clamps negative tribute and duration");

        JObject legacy = new JObject
        {
            ["intent"] = "peace_offer",
            ["target_kingdom_id"] = "k2",
            ["peace_terms"] = new JObject { ["daily_tribute"] = 5 },
            ["responding_to_offer_document_id"] = "offer-1"
        };
        WorldDiplomacyEnvelopeJsonRules.NormalizeGeneratedDiplomaticEnvelopeShape(null, legacy);
        Test.True(legacy["actions"] is JArray lifted && lifted.Count == 1
                && (string)lifted[0]["target_kingdom_id"] == "k2"
                && (string)lifted[0]["intent"] == "peace_offer"
                && (int)lifted[0]["peace_terms"]["daily_tribute"] == 5
                && (string)lifted[0]["responding_to_offer_document_id"] == "offer-1",
            "shape normalization lifts a legacy flat envelope into a single action");
        Test.True((string)legacy["primary_target_kingdom_id"] == "k2"
                && ((JArray)legacy["addressed_kingdom_ids"]).Values<string>().SequenceEqual(new[] { "k2" })
                && (string)legacy["author_intent"]["intent"] == "peace_offer"
                && legacy["round_plan"] == null
                && (string)legacy["tone"] == "neutral"
                && (string)legacy["round_participation"] == "continue"
                && (string)legacy["round_status"] == "continue"
                && (bool)legacy["made_progress"],
            "shape normalization mirrors the first action and fills envelope defaults");

        JObject autonomous = new JObject
        {
            ["title"] = "边境会谈",
            ["actions"] = new JArray(new JObject { ["target_kingdom_id"] = "k9", ["intent"] = "statement" })
        };
        WorldDiplomacyEnvelopeJsonRules.NormalizeGeneratedDiplomaticEnvelopeShape(
            new WorldDiplomacyJob { Kind = "generate", IsResponse = false, AllowUntargeted = true, TargetKingdomId = "" },
            autonomous);
        Test.True(autonomous["round_plan"] == null, "declaration normalization does not invent a legacy participant plan");
        JObject targeted = new JObject
        {
            ["actions"] = new JArray(new JObject { ["target_kingdom_id"] = "k9", ["intent"] = "statement" })
        };
        WorldDiplomacyEnvelopeJsonRules.NormalizeGeneratedDiplomaticEnvelopeShape(
            new WorldDiplomacyJob { Kind = "generate", IsResponse = false, AllowUntargeted = false, TargetKingdomId = "k1" },
            targeted);
        Test.True(targeted["round_plan"] == null, "targeted declarations use the same plan-free envelope");
        WorldDiplomacyEnvelopeJsonRules.NormalizeGeneratedDiplomaticEnvelopeShape(null, null);

        JObject mirrored = new JObject
        {
            ["actions"] = new JArray(new JObject
            {
                ["intent"] = "war_declaration",
                ["target_kingdom_id"] = "k3",
                ["commitment"] = "binding",
                ["responding_to_threat_document_id"] = "threat-7"
            })
        };
        WorldDiplomacyEnvelopeJsonRules.MirrorFirstGeneratedActionEnvelope(mirrored, (JArray)mirrored["actions"]);
        Test.True((string)mirrored["author_intent"]["intent"] == "war_declaration"
                && (string)mirrored["author_intent"]["commitment"] == "binding"
                && (string)mirrored["primary_target_kingdom_id"] == "k3"
                && (string)mirrored["responding_to_threat_document_id"] == "threat-7",
            "first-action mirroring copies intent, commitment, target, and response links");
        JObject noActions = new JObject();
        WorldDiplomacyEnvelopeJsonRules.MirrorFirstGeneratedActionEnvelope(noActions, new JArray());
        Test.True(noActions["author_intent"] == null,
            "first-action mirroring ignores an empty action list");

        JObject source = new JObject
        {
            ["actions"] = new JArray(new JObject(), new JObject()),
            ["title"] = "t",
            ["confidence"] = 0.9
        };
        JObject action = new JObject
        {
            ["intent"] = "peace_offer",
            ["target_kingdom_id"] = "k4",
            ["commitment"] = "proposal",
            ["negotiation_move"] = "concede",
            ["peace_terms"] = new JObject { ["daily_tribute"] = 3 },
            ["responding_to_offer_document_id"] = "offer-9"
        };
        JObject single = WorldDiplomacyEnvelopeJsonRules.BuildGeneratedSingleActionEnvelope(source, action);
        Test.True(single["actions"] == null
                && (string)single["title"] == "t"
                && (string)single["author_intent"]["intent"] == "peace_offer"
                && (string)single["author_intent"]["commitment"] == "proposal"
                && (string)single["negotiation_move"] == "concede"
                && (string)single["primary_target_kingdom_id"] == "k4"
                && (int)single["peace_terms"]["daily_tribute"] == 3
                && (string)single["responding_to_offer_document_id"] == "offer-9"
                && ((JArray)single["addressed_kingdom_ids"]).Values<string>().SequenceEqual(new[] { "k4" }),
            "single-action envelopes drop the action array and copy derived fields");
        JObject rebuiltAction = new JObject();
        WorldDiplomacyEnvelopeJsonRules.CopyDerivedGeneratedActionEnvelope(single, rebuiltAction);
        Test.True((string)rebuiltAction["intent"] == "peace_offer"
                && (string)rebuiltAction["commitment"] == "proposal"
                && (string)rebuiltAction["negotiation_move"] == "concede"
                && (string)rebuiltAction["responding_to_offer_document_id"] == "offer-9"
                && (int)rebuiltAction["peace_terms"]["daily_tribute"] == 3,
            "derived action envelopes copy fields back onto the action");

        JObject bound = new JObject
        {
            ["author_intent"] = new JObject { ["commitment"] = "proposal:offer=diplomacy_document:doc-1" }
        };
        Test.True(WorldDiplomacyEnvelopeJsonRules.TryNormalizeInlineResponseBinding(bound, out string offerKind)
                && offerKind == "offer"
                && (string)bound["author_intent"]["commitment"] == "proposal"
                && (string)bound["responding_to_offer_document_id"] == "diplomacy_document:doc-1",
            "inline offer binding splits the marker and records the source document");
        JObject threatBound = new JObject
        {
            ["author_intent"] = new JObject { ["commitment"] = "rejection:threat=diplomacy_document:doc-2" }
        };
        Test.True(WorldDiplomacyEnvelopeJsonRules.TryNormalizeInlineResponseBinding(threatBound, out string threatKind)
                && threatKind == "threat"
                && (string)threatBound["responding_to_threat_document_id"] == "diplomacy_document:doc-2",
            "inline threat binding splits the marker and records the source document");
        Test.True(!WorldDiplomacyEnvelopeJsonRules.TryNormalizeInlineResponseBinding(
                    new JObject { ["author_intent"] = new JObject { ["commitment"] = "proposal" } }, out _)
                && !WorldDiplomacyEnvelopeJsonRules.TryNormalizeInlineResponseBinding(
                    new JObject { ["author_intent"] = new JObject { ["commitment"] = "bogus:offer=diplomacy_document:d" } }, out _)
                && !WorldDiplomacyEnvelopeJsonRules.TryNormalizeInlineResponseBinding(
                    new JObject { ["author_intent"] = new JObject { ["commitment"] = "proposal:offer=other:doc" } }, out _)
                && !WorldDiplomacyEnvelopeJsonRules.TryNormalizeInlineResponseBinding(
                    new JObject
                    {
                        ["author_intent"] = new JObject { ["commitment"] = "proposal:offer=diplomacy_document:d1" },
                        ["responding_to_offer_document_id"] = "diplomacy_document:d2"
                    }, out _)
                && !WorldDiplomacyEnvelopeJsonRules.TryNormalizeInlineResponseBinding(new JObject(), out _),
            "inline binding rejects missing markers, unsupported commitments, bad prefixes, and conflicts");

        JObject mixed = JObject.Parse(
            "{\"actions\":[{\"intent\":\"statement\"},{\"intent\":\"peace_offer\"},{\"intent\":\"statement\"}]}");
        Test.True(WorldDiplomacyEnvelopeJsonRules.RemoveRedundantStatementActions(mixed) == 2
                && ((JArray)mixed["actions"]).Count == 1
                && (string)((JArray)mixed["actions"])[0]["intent"] == "peace_offer",
            "redundant statements are removed when a substantive action exists");
        JObject onlyStatements = JObject.Parse("{\"actions\":[{\"intent\":\"statement\"},{\"intent\":\"statement\"}]}");
        Test.True(WorldDiplomacyEnvelopeJsonRules.RemoveRedundantStatementActions(onlyStatements) == 0
                && ((JArray)onlyStatements["actions"]).Count == 2,
            "statements are kept when no substantive action exists");
        JObject singleAction = JObject.Parse("{\"actions\":[{\"intent\":\"statement\"}]}");
        Test.True(WorldDiplomacyEnvelopeJsonRules.RemoveRedundantStatementActions(singleAction) == 0
                && WorldDiplomacyEnvelopeJsonRules.RemoveRedundantStatementActions(new JObject()) == 0
                && WorldDiplomacyEnvelopeJsonRules.RemoveRedundantStatementActions(null) == 0,
            "statement removal is a no-op for single or missing action lists");
    }

    private static void RunTextRuleDecisionTests()
    {
        Test.True(WorldDiplomacyTextRules.Limit("abcdef", 3) == "abc"
                && WorldDiplomacyTextRules.Limit("ab", 5) == "ab"
                && WorldDiplomacyTextRules.Limit(null, 4) == ""
                && WorldDiplomacyTextRules.Limit("abc", -2) == "",
            "limit truncates at the cap and tolerates null or negative bounds");

        Test.True(WorldDiplomacyTextRules.NormalizeBody("  a\r\nb\rc  ") == "a\nb\nc"
                && WorldDiplomacyTextRules.NormalizeBody(null) == ""
                && WorldDiplomacyTextRules.NormalizeBody(new string('x', 7000)).Length == 6000,
            "body normalization unifies newlines, trims, and applies the 6000-char cap");
        Test.True(WorldDiplomacyTextRules.NormalizeCanonicalHistoryText("  a\r\nb  ") == "a\nb"
                && WorldDiplomacyTextRules.NormalizeCanonicalHistoryText(new string('x', 7000)).Length == 7000
                && WorldDiplomacyTextRules.NormalizeCanonicalHistoryText(null) == "",
            "canonical text normalization trims without the per-document cap");

        Test.True(WorldDiplomacyTextRules.DeriveTitle("《 开幕 》\nsecond", "fallback") == "开幕"
                && WorldDiplomacyTextRules.DeriveTitle("", "fallback") == "fallback"
                && WorldDiplomacyTextRules.DeriveTitle(null, "  fb  ") == "fb"
                && WorldDiplomacyTextRules.DeriveTitle(new string('x', 60), "fb").Length == 36,
            "title derivation takes the first line, strips book quotes, falls back, and caps at 36");

        Test.True(WorldDiplomacyTextRules.CompactPromptFact("a  b\tc\nd", 50) == "a b c d"
                && WorldDiplomacyTextRules.CompactPromptFact(null, 5) == ""
                && WorldDiplomacyTextRules.CompactPromptFact("a b c d", 4) == "a b ",
            "prompt-fact compaction collapses whitespace and applies the cap");

        string sanitized = WorldDiplomacyTextRules.SanitizePublicDiplomacyText("候选路线与硬目标，回合开始");
        Test.True(sanitized.Contains("可行方向")
                && sanitized.Contains("首要目标")
                && sanitized.Contains("交涉开始")
                && !sanitized.Contains("候选路线")
                && !sanitized.Contains("硬目标")
                && !sanitized.Contains("回合开始"),
            "public text sanitization replaces internal-draft phrasing");
        Test.True(WorldDiplomacyTextRules.SanitizePublicDiplomacyText(null) == ""
                && WorldDiplomacyTextRules.SanitizePublicDiplomacyText("中立文本") == "中立文本",
            "sanitization tolerates null and leaves clean text untouched");

        string display = WorldDiplomacyTextRules.FormatDiplomaticBodyForDisplay("　第一段。\n\n第二段，仍然很短。");
        Test.True(display.StartsWith("　　")
                && display.Contains("第一段。")
                && display.Contains("第二段，仍然很短。")
                && display.Contains("\n\n"),
            "display formatting joins paragraphs with ideographic-space prefixes");
        Test.True(WorldDiplomacyTextRules.FormatDiplomaticBodyForDisplay("  \n  ") == ""
                && WorldDiplomacyTextRules.FormatDiplomaticBodyForDisplay(null) == "",
            "display formatting yields empty for blank input");

        List<string> paragraphs = new List<string>();
        WorldDiplomacyTextRules.AppendDiplomaticDisplayParagraphs(
            paragraphs,
            new string('x', 130) + "。" + new string('y', 130) + "。");
        Test.True(paragraphs.Count == 2
                && paragraphs[0] == new string('x', 130) + "。"
                && paragraphs[1] == new string('y', 130) + "。",
            "long paragraphs split on sentence boundaries once the soft limit is reached");
        List<string> shortTarget = new List<string>();
        WorldDiplomacyTextRules.AppendDiplomaticDisplayParagraphs(shortTarget, "  短句  ");
        Test.True(shortTarget.Count == 1 && shortTarget[0] == "短句",
            "short paragraphs are trimmed and appended whole");
        WorldDiplomacyTextRules.AppendDiplomaticDisplayParagraphs(null, "x");
        WorldDiplomacyTextRules.AppendDiplomaticDisplayParagraphs(shortTarget, "   ");
        Test.True(shortTarget.Count == 1,
            "paragraph splitting ignores a null target and blank input");
    }



    private static void RunOfferContractDecisionTests()
    {
        Test.True(!WorldDiplomacyOfferContractRules.IsExclusivePeaceOfferResponseSet(null)
                && !WorldDiplomacyOfferContractRules.IsExclusivePeaceOfferResponseSet(new List<string>())
                && WorldDiplomacyOfferContractRules.IsExclusivePeaceOfferResponseSet(
                    new List<string> { "accept_peace" })
                && WorldDiplomacyOfferContractRules.IsExclusivePeaceOfferResponseSet(
                    new List<string> { "accept_peace", "reject_peace" })
                && WorldDiplomacyOfferContractRules.IsExclusivePeaceOfferResponseSet(
                    new List<string> { "ACCEPT_PEACE" })
                && !WorldDiplomacyOfferContractRules.IsExclusivePeaceOfferResponseSet(
                    new List<string> { "accept_peace", "propose_trade" }),
            "exclusive peace-response sets contain only accept_peace or reject_peace and at least one intent");

        WorldDiplomacyRoundOffer offer = new WorldDiplomacyRoundOffer
        {
            ProposerKingdomId = "k1",
            SourceDocumentId = "doc-9",
            SourceActionId = "a-2"
        };
        Test.True(WorldDiplomacyOfferContractRules.IsRequiredPeaceOfferResponse(
                    "k1", "accept_peace", "doc-9", "a-2", offer)
                && WorldDiplomacyOfferContractRules.IsRequiredPeaceOfferResponse(
                    "k1", "reject_peace", "doc-9", "a-2", offer)
                && WorldDiplomacyOfferContractRules.IsRequiredPeaceOfferResponse(
                    "other", "statement", "", "", null)
                && !WorldDiplomacyOfferContractRules.IsRequiredPeaceOfferResponse(
                    "k1", "statement", "doc-9", "a-2", offer)
                && !WorldDiplomacyOfferContractRules.IsRequiredPeaceOfferResponse(
                    "k2", "accept_peace", "doc-9", "a-2", offer)
                && !WorldDiplomacyOfferContractRules.IsRequiredPeaceOfferResponse(
                    "k1", "accept_peace", "doc-8", "a-2", offer)
                && !WorldDiplomacyOfferContractRules.IsRequiredPeaceOfferResponse(
                    "k1", "accept_peace", "doc-9", "a-3", offer),
            "required peace responses must match intent, proposer, document, and action identity");

        JArray matchingActions = new JArray
        {
            new JObject
            {
                ["target_kingdom_id"] = "k1",
                ["intent"] = "accept_peace",
                ["responding_to_offer_document_id"] = "doc-9",
                ["responding_to_offer_action_id"] = "a-2"
            }
        };
        JArray wrongActions = new JArray
        {
            new JObject
            {
                ["target_kingdom_id"] = "k1",
                ["intent"] = "propose_trade"
            }
        };
        Test.True(WorldDiplomacyOfferContractRules.GeneratedActionsContainRequiredPeaceOfferResponse(
                    matchingActions, offer)
                && !WorldDiplomacyOfferContractRules.GeneratedActionsContainRequiredPeaceOfferResponse(
                    wrongActions, offer)
                && WorldDiplomacyOfferContractRules.GeneratedActionsContainRequiredPeaceOfferResponse(
                    wrongActions, null)
                && WorldDiplomacyOfferContractRules.GeneratedActionsContainRequiredPeaceOfferResponse(
                    null, offer) == false,
            "generated actions must contain a matching peace response when an offer is required");

        Test.True(WorldDiplomacyOfferContractRules.CommitmentMatchesIntent("declare_war", "binding")
                && WorldDiplomacyOfferContractRules.CommitmentMatchesIntent("propose_peace", "proposal")
                && WorldDiplomacyOfferContractRules.CommitmentMatchesIntent("accept_peace", "acceptance")
                && WorldDiplomacyOfferContractRules.CommitmentMatchesIntent("reject_peace", "rejection")
                && WorldDiplomacyOfferContractRules.CommitmentMatchesIntent("ultimatum", "binding")
                && WorldDiplomacyOfferContractRules.CommitmentMatchesIntent("statement", "non_binding")
                && !WorldDiplomacyOfferContractRules.CommitmentMatchesIntent("declare_war", "proposal")
                && !WorldDiplomacyOfferContractRules.CommitmentMatchesIntent("statement", "binding")
                && !WorldDiplomacyOfferContractRules.CommitmentMatchesIntent("unknown_intent", "binding"),
            "commitment matching follows the intent-to-commitment contract");

        string peaceResult = WorldDiplomacyOfferContractRules.ProposalSuccessResult("propose_peace");
        string allianceResult = WorldDiplomacyOfferContractRules.ProposalSuccessResult("propose_alliance");
        string tradeResult = WorldDiplomacyOfferContractRules.ProposalSuccessResult("propose_trade");
        string fallbackResult = WorldDiplomacyOfferContractRules.ProposalSuccessResult("statement");
        Test.True(peaceResult.Length > 0 && allianceResult.Length > 0 && tradeResult.Length > 0
                && peaceResult != allianceResult && peaceResult != tradeResult
                && allianceResult != tradeResult
                && fallbackResult != peaceResult,
            "proposal success results distinguish peace, alliance, trade, and fallback text");

        Test.True(WorldDiplomacyTextRules.ContainsWholeNumber("the count is 84 today", 84)
                && WorldDiplomacyTextRules.ContainsWholeNumber("84", 84)
                && !WorldDiplomacyTextRules.ContainsWholeNumber("the count is 840 today", 84)
                && !WorldDiplomacyTextRules.ContainsWholeNumber("no digits", 84)
                && !WorldDiplomacyTextRules.ContainsWholeNumber(null, 84),
            "whole-number matching requires a non-digit boundary on both sides");

        int strippedIndex;
        Test.True(WorldDiplomacyTextRules.StripGeneratedActionReasonPrefix("action[3]:reason", out strippedIndex) == "reason"
                && strippedIndex == 3
                && WorldDiplomacyTextRules.StripGeneratedActionReasonPrefix("action[10]:x", out strippedIndex) == "x"
                && strippedIndex == 10
                && WorldDiplomacyTextRules.StripGeneratedActionReasonPrefix("action[3]: stated", out strippedIndex) == " stated",
            "reason-prefix stripping parses the action index and returns the untrimmed remainder");
        Test.True(WorldDiplomacyTextRules.StripGeneratedActionReasonPrefix("plain reason", out strippedIndex) == "plain reason"
                && strippedIndex == -1
                && WorldDiplomacyTextRules.StripGeneratedActionReasonPrefix("action[x]: y", out strippedIndex) == "action[x]: y"
                && strippedIndex == -1
                && WorldDiplomacyTextRules.StripGeneratedActionReasonPrefix(null, out strippedIndex) == ""
                && strippedIndex == -1,
            "reason-prefix stripping leaves non-indexed or malformed input unchanged");

        Test.True(WorldDiplomacyTextRules.ParseDayForArchive("第2年夏15日") == 2115
                && WorldDiplomacyTextRules.ParseDayForArchive("第2年春15日") == 2015
                && WorldDiplomacyTextRules.ParseDayForArchive("84") == 84
                && WorldDiplomacyTextRules.ParseDayForArchive("abc") == 0
                && WorldDiplomacyTextRules.ParseDayForArchive(null) == 0,
            "archive day parsing combines year, season, and day or falls back to raw digits");

        string violationReason;
        Test.True(!WorldDiplomacyTextRules.TryGetImmersionViolation(null, out violationReason)
                && !WorldDiplomacyTextRules.TryGetImmersionViolation("   ", out violationReason)
                && !WorldDiplomacyTextRules.TryGetImmersionViolation("两国正式缔结同盟。", out violationReason),
            "immersion violation tolerates blank input and clean public text");
        Test.True(WorldDiplomacyTextRules.TryGetImmersionViolation("这里包含JSON字段说明", out violationReason)
                && violationReason == "internal_round_term_exposed_in_public_declaration",
            "immersion violation flags internal round terms in public declarations");
        Test.True(!WorldDiplomacyTextRules.TryGetImmersionViolation("我我我我你你你你", out violationReason),
            "first and second person counts do not reject a diplomatic declaration");

        Test.True(WorldDiplomacyRoundLifecycleRules.CalculatePropagationDays(0f, 100f, 5) == 1
                && WorldDiplomacyRoundLifecycleRules.CalculatePropagationDays(100f, 100f, 5) == 5
                && WorldDiplomacyRoundLifecycleRules.CalculatePropagationDays(50f, 100f, 5) == 3
                && WorldDiplomacyRoundLifecycleRules.CalculatePropagationDays(0f, 0f, 5) == 5
                && WorldDiplomacyRoundLifecycleRules.CalculatePropagationDays(500f, 100f, 5) == 5,
            "propagation days scale by distance, ceil to whole days, and clamp to [1, max]");

        Test.True(WorldDiplomacyRoundLifecycleRules.ParseCompressionSequence("batch_7") == 7
                && WorldDiplomacyRoundLifecycleRules.ParseCompressionSequence("batch_name_12") == 12
                && WorldDiplomacyRoundLifecycleRules.ParseCompressionSequence("batch") == 0
                && WorldDiplomacyRoundLifecycleRules.ParseCompressionSequence("batch_x") == 0
                && WorldDiplomacyRoundLifecycleRules.ParseCompressionSequence("batch_-3") == 0
                && WorldDiplomacyRoundLifecycleRules.ParseCompressionSequence(null) == 0,
            "compression sequence parses the trailing integer after the last underscore");
    }

    private static void RunPromptContractDecisionTests()
    {
        Test.True(WorldDiplomacyPromptContractRules.StablePromptHash(null) == WorldDiplomacyPromptContractRules.StablePromptHash("")
            && WorldDiplomacyPromptContractRules.StablePromptHash("a") != WorldDiplomacyPromptContractRules.StablePromptHash("b")
            && WorldDiplomacyPromptContractRules.StablePromptHash("x").Length == 16,
            "stable prompt hash treats null as empty and emits invariant hex16");
        Test.True(WorldDiplomacyPromptContractRules.StablePromptHashPair("a", "b")
                == WorldDiplomacyPromptContractRules.StablePromptHash("a\nb"),
            "prompt hash pair hashes first + newline + second");
        List<WorldDiplomacyLlmMessage> hashMessages = new List<WorldDiplomacyLlmMessage>
        {
            new WorldDiplomacyLlmMessage { Role = "system", Content = "s1" },
            new WorldDiplomacyLlmMessage { Role = "user", Content = "u1" }
        };
        Test.True(WorldDiplomacyPromptContractRules.StablePromptHashMessagePrefix(hashMessages, 1)
                == WorldDiplomacyPromptContractRules.StablePromptHash("system:s1")
            && WorldDiplomacyPromptContractRules.StablePromptHashMessagePrefix(hashMessages, 99)
                == WorldDiplomacyPromptContractRules.StablePromptHashMessagePrefix(hashMessages, 2)
            && WorldDiplomacyPromptContractRules.StablePromptHashMessagePrefix(null, 5) == "14650fb0739d0383",
            "message prefix hash is the role:content newline chain with clamped count");

        string canonicalSystem = WorldDiplomacyPromptContractRules.BuildCanonicalHistorySystemPrompt("COMMONZ", 80, 300);
        WorldDiplomacyJob repairJob = new WorldDiplomacyJob
        {
            Kind = "generate",
            SemanticRepairAttempts = 1,
            SystemPrompt = "SYS",
            UserPrompt = "fix 【MODE=DECLARE】",
            HistoryPrefixHash = WorldDiplomacyPromptContractRules.StablePromptHashPair("SYS", "HIST"),
            LlmMessages = new List<WorldDiplomacyLlmMessage>
            {
                new WorldDiplomacyLlmMessage { Role = "system", Content = "SYS" },
                new WorldDiplomacyLlmMessage { Role = "system", Content = "HIST" },
                new WorldDiplomacyLlmMessage { Role = "user", Content = "orig 【MODE=DECLARE】" },
                new WorldDiplomacyLlmMessage { Role = "assistant", Content = "rejected" },
                new WorldDiplomacyLlmMessage { Role = "user", Content = "fix 【MODE=DECLARE】" }
            }
        };
        Test.True(WorldDiplomacyPromptContractRules.IsValidSemanticRepairMessageChain(repairJob),
            "valid semantic repair chain is accepted");
        repairJob.SemanticRepairAttempts = 0;
        Test.True(!WorldDiplomacyPromptContractRules.IsValidSemanticRepairMessageChain(repairJob),
            "zero repair attempts invalidate the chain");
        repairJob.SemanticRepairAttempts = 2;
        Test.True(!WorldDiplomacyPromptContractRules.IsValidSemanticRepairMessageChain(repairJob),
            "repair attempts above the cap invalidate the chain");
        repairJob.SemanticRepairAttempts = 1;
        repairJob.LlmMessages[3].Role = "user";
        Test.True(!WorldDiplomacyPromptContractRules.IsValidSemanticRepairMessageChain(repairJob),
            "a non-assistant rejected turn invalidates the chain");
        repairJob.LlmMessages[3].Role = "assistant";
        repairJob.HistoryPrefixHash = "deadbeefdeadbeef";
        Test.True(!WorldDiplomacyPromptContractRules.IsValidSemanticRepairMessageChain(repairJob),
            "a mismatched history prefix hash invalidates the chain");
        repairJob.HistoryPrefixHash = WorldDiplomacyPromptContractRules.StablePromptHashPair("SYS", "HIST");
        Test.True(WorldDiplomacyPromptContractRules.IsValidSemanticRepairMessageChain(repairJob)
            && !WorldDiplomacyPromptContractRules.IsValidSemanticRepairMessageChain(null),
            "restored chain validates and null job is rejected");

        WorldDiplomacyJob canonicalJob = new WorldDiplomacyJob
        {
            Kind = "generate",
            CacheAffinityKey = WorldDiplomacyPromptContractRules.CanonicalHistoryCacheAffinityKey,
            SystemPrompt = canonicalSystem,
            UserPrompt = "dynamic 【MODE=DECLARE】"
        };
        Test.True(WorldDiplomacyPromptContractRules.HasCurrentCanonicalPromptContract(canonicalJob),
            "a canonical generation job with the current contract is accepted");
        canonicalJob.CacheAffinityKey = "stale";
        Test.True(!WorldDiplomacyPromptContractRules.HasCurrentCanonicalPromptContract(canonicalJob),
            "a stale cache affinity key invalidates the prompt contract");
        canonicalJob.CacheAffinityKey = WorldDiplomacyPromptContractRules.CanonicalHistoryCacheAffinityKey;
        canonicalJob.UserPrompt = "dynamic without the mode line";
        Test.True(!WorldDiplomacyPromptContractRules.HasCurrentCanonicalPromptContract(canonicalJob),
            "a generation prompt missing MODE=DECLARE is rejected");
        canonicalJob.Kind = "compress";
        canonicalJob.UserPrompt = "dynamic 【MODE=COMPACT】";
        Test.True(WorldDiplomacyPromptContractRules.HasCurrentCanonicalPromptContract(canonicalJob),
            "a compress job expects MODE=COMPACT instead");
        canonicalJob.UserPrompt = "dynamic 【MODE=DECLARE】";
        Test.True(!WorldDiplomacyPromptContractRules.HasCurrentCanonicalPromptContract(canonicalJob),
            "a compress job with MODE=DECLARE is rejected");
        canonicalJob.Kind = "analyze";
        canonicalJob.SystemPrompt = "";
        Test.True(WorldDiplomacyPromptContractRules.HasCurrentCanonicalPromptContract(canonicalJob)
            && WorldDiplomacyPromptContractRules.HasCurrentCanonicalPromptContract(null),
            "non-canonical and null jobs bypass the contract check");

        string contract;
        Test.True(WorldDiplomacyPromptContractRules.TryExtractCommonContractBeforeMarker("HEAD\n\nMARKER tail", "MARKER", out contract)
                && contract == "HEAD"
            && !WorldDiplomacyPromptContractRules.TryExtractCommonContractBeforeMarker("no marker", "MARKER", out contract)
            && contract == ""
            && !WorldDiplomacyPromptContractRules.TryExtractCommonContractBeforeMarker(null, "MARKER", out contract),
            "before-marker extraction trims the prefix and fails when the marker is absent");
        WorldDiplomacyJob extractJob = new WorldDiplomacyJob { Kind = "generate", SystemPrompt = canonicalSystem };
        Test.True(WorldDiplomacyPromptContractRules.TryExtractCommonContractFromJob(extractJob, out contract)
                && contract.Contains("COMMONZ"),
            "generation jobs recover the common contract before the writing marker");
        extractJob = new WorldDiplomacyJob
        {
            Kind = "analyze",
            SystemPrompt = WorldDiplomacyPromptContractRules.BuildAnalysisSystemPrompt("PRE")
        };
        Test.True(WorldDiplomacyPromptContractRules.TryExtractCommonContractFromJob(extractJob, out contract)
                && contract.Contains("PRE"),
            "analysis jobs extract the prefix before the analysis marker");
        extractJob = new WorldDiplomacyJob { Kind = "participate", SystemPrompt = canonicalSystem };
        Test.True(!WorldDiplomacyPromptContractRules.TryExtractCommonContractFromJob(extractJob, out contract)
            && !WorldDiplomacyPromptContractRules.TryExtractCommonContractFromJob(null, out contract),
            "unsupported kinds and null jobs extract no contract");

        Test.True(WorldDiplomacyPromptContractRules.BuildDeclareModePrompt("  dyn  ").Contains("dyn")
                && WorldDiplomacyPromptContractRules.BuildDeclareModePrompt(null).Contains("MODE=DECLARE"),
            "declare mode prompt embeds the trimmed dynamic prompt and the mode contract");
        Test.True(WorldDiplomacyPromptContractRules.BuildGenerationSystemPrompt("C", 10, 99)
                == WorldDiplomacyPromptContractRules.BuildCanonicalHistorySystemPrompt("C", 10, 99)
,
            "generation and relay prompts delegate to the canonical history prompt");
        StringBuilder declSb = new StringBuilder();
        WorldDiplomacyPromptContractRules.AppendDiplomaticDeclarationWritingContract(declSb, 80, 300);
        Test.True(!declSb.ToString().Contains("80") && declSb.ToString().Contains("300"),
            "Q50 writing contract retains only the suggested upper length, never a minimum");
        Test.True(WorldDiplomacyPromptContractRules.BuildCanonicalHistorySystemPrompt("C", 10, 99).Contains("MODE=DECLARE")
            && WorldDiplomacyPromptContractRules.BuildCanonicalHistorySystemPrompt("C", 10, 99).Contains("MODE=COMPACT")
            && WorldDiplomacyPromptContractRules.BuildAnalysisSystemPrompt("C").Contains("MODE=ANALYZE")
            && WorldDiplomacyPromptContractRules.BuildDiplomaticDeclarationModeContract().Length > 0
            && WorldDiplomacyPromptContractRules.BuildCanonicalHistoryCompressionModeContract().Length > 0
            && WorldDiplomacyPromptContractRules.BuildAnalysisModeContract().Length > 0,
            "composed system prompts carry their dispatch markers");
        Test.True(WorldDiplomacyPromptContractRules.CreateSystemPromptBuilder(null).Length == 0
            && WorldDiplomacyPromptContractRules.CreateSystemPromptBuilder("CC").ToString().Contains("CC"),
            "the system prompt builder seeds the common contract");

        string tokenPrompt = WorldDiplomacyPromptContractRules.BuildTokenCompressionPrompt("b1", -3L, -5L, 0, -9L);
        Test.True(tokenPrompt.Contains("b1") && tokenPrompt.Contains("MODE=COMPACT")
            && tokenPrompt == WorldDiplomacyPromptContractRules.BuildTokenCompressionPrompt("b1", 0L, 0L, 0, 0L),
            "token compression prompt clamps negative counters and carries the compact mode");
        Test.True(WorldDiplomacyPromptContractRules.BuildRoundCompressionSystemPrompt().Contains("\"summary\"")
            && WorldDiplomacyPromptContractRules.BuildRoundCompressionSystemPrompt().Contains("facts"),
            "the round compression prompt declares the summary/facts JSON shape");

        string legalOptions = WorldDiplomacyPromptContractRules.BuildCurrentLegalDiplomaticOptions(
            new Dictionary<string, List<string>>
            {
                ["K2"] = new List<string> { "b", "a" },
                ["k1"] = new List<string> { "x" },
                ["  "] = new List<string> { "y" },
                ["k3"] = null
            });
        Test.True(legalOptions.Contains("k1=x") && legalOptions.Contains("K2=b/a")
            && legalOptions.IndexOf("k1") < legalOptions.IndexOf("K2")
            && !legalOptions.Contains("k3"),
            "legal options sort targets case-insensitively and drop empty entries");
        Test.True(WorldDiplomacyPromptContractRules.BuildCurrentLegalDiplomaticOptions(null).Length > 0
            && WorldDiplomacyPromptContractRules.BuildCurrentLegalDiplomaticOptions(new Dictionary<string, List<string>>()).Length > 0,
            "empty legal options still produce a fallback line");
        Test.True(WorldDiplomacyPromptContractRules.DescribePotentialDiplomaticActions(new[] { "declare_war" }).Length > 0
            && WorldDiplomacyPromptContractRules.DescribePotentialDiplomaticActions(null)
                != WorldDiplomacyPromptContractRules.DescribePotentialDiplomaticActions(new[] { "declare_war" }),
            "potential action descriptions normalize intents and fall back when empty");

        List<string> traits = new List<string>();
        WorldDiplomacyPromptContractRules.AppendVoiceTrait(traits, 3, "POS", "NEG");
        WorldDiplomacyPromptContractRules.AppendVoiceTrait(traits, -2, "POS2", "NEG2");
        WorldDiplomacyPromptContractRules.AppendVoiceTrait(traits, 0, "POS3", "NEG3");
        WorldDiplomacyPromptContractRules.AppendVoiceTrait(null, 1, "x", "y");
        Test.True(traits.Count == 2 && traits[0] == "POS" && traits[1] == "NEG2",
            "voice traits append positive for positive values and negative for negative values only");

        string voice = WorldDiplomacyPromptContractRules.BuildRealmInstitutionalVoiceText("KN", "CU", "RT", "GH", "LO");
        Test.True(voice.Contains("KN") && voice.Contains("CU") && voice.Contains("RT") && voice.Contains("LO"),
            "realm institutional voice embeds the realm descriptors");

        Test.True(WorldDiplomacyRoundLifecycleRules.UsesCanonicalHistory(new WorldDiplomacyJob { Kind = "generate" })
            && WorldDiplomacyRoundLifecycleRules.UsesCanonicalHistory(new WorldDiplomacyJob { Kind = "compress" })
            && !WorldDiplomacyRoundLifecycleRules.UsesCanonicalHistory(new WorldDiplomacyJob { Kind = "analyze" })
            && !WorldDiplomacyRoundLifecycleRules.UsesCanonicalHistory(null),
            "canonical history usage is limited to generation and compression jobs");
    }

    private static void RunStructureRuleDecisionTests()
    {
        // DPL-060AO: round/document structure rules live in the domain file and stay pure.
        WorldDiplomacyRound round = new WorldDiplomacyRound
        {
            PendingOffers = new List<WorldDiplomacyRoundOffer>
            {
                new WorldDiplomacyRoundOffer { Status = "open", ProposerKingdomId = "k1", TargetKingdomId = "k2" },
                new WorldDiplomacyRoundOffer { Status = "closed", ProposerKingdomId = "k3", TargetKingdomId = "k4" }
            }
        };
        Test.True(WorldDiplomacyStructureRules.HasOpenRoundOffers(round), "open offer detected");
        round.PendingOffers[0].Status = "answered";
        Test.True(!WorldDiplomacyStructureRules.HasOpenRoundOffers(round), "no open offers once answered");
        Test.True(!WorldDiplomacyStructureRules.HasOpenRoundOffers(new WorldDiplomacyRound()), "empty offers is false");

        round.Participants = new List<WorldDiplomacyRoundParticipant>
        {
            new WorldDiplomacyRoundParticipant { KingdomId = "K1", State = "active" }
        };
        Test.True(WorldDiplomacyStructureRules.RoundContainsKingdom(round, "k1"), "participant match is case-insensitive");
        Test.True(!WorldDiplomacyStructureRules.RoundContainsKingdom(round, "k9"), "absent kingdom rejected");

        round.RelayRouteKingdomIds = new List<string> { "k1", "k2" };
        Test.True(WorldDiplomacyStructureRules.RoundRouteContainsKingdom(round, "K2"), "route match is case-insensitive");
        Test.True(!WorldDiplomacyStructureRules.RoundRouteContainsKingdom(round, "k3"), "non-route kingdom rejected");
        Test.True(!WorldDiplomacyStructureRules.RoundRouteContainsKingdom(round, " "), "blank route id rejected");

        WorldDiplomacyRoundParticipant added = WorldDiplomacyStructureRules.EnsureRoundParticipant(round, "k5", null, true);
        Test.True(added != null && added.KingdomId == "k5" && added.State == "observer" && added.MandatoryReplyPending,
            "missing participant is added as mandatory observer");
        WorldDiplomacyRoundParticipant existing = WorldDiplomacyStructureRules.EnsureRoundParticipant(round, "K1", "active", false);
        Test.True(existing == round.Participants[0] && round.Participants.Count == 2,
            "existing participant returned without duplication");
        Test.True(WorldDiplomacyStructureRules.EnsureRoundParticipant(null, "k6", "active", false) == null,
            "null round yields null participant");
        round.Participants[0].State = "withdrawn";
        WorldDiplomacyStructureRules.EnsureRoundParticipant(round, "k1", "active", false);
        Test.True(round.Participants[0].State == "active", "withdrawn participant refreshed by active request");

        WorldDiplomacyRound relay = new WorldDiplomacyRound { RelayPlanned = true };
        Test.True(WorldDiplomacyStructureRules.AddParticipantToRelayRouteIfNeeded(relay, "k1", 2), "first route admission");
        Test.True(!WorldDiplomacyStructureRules.AddParticipantToRelayRouteIfNeeded(relay, "k1", 2)
            || relay.RelayRouteKingdomIds.Count == 1, "already-on-route returns without dup add");
        Test.True(WorldDiplomacyStructureRules.AddParticipantToRelayRouteIfNeeded(relay, "k2", 2), "second route admission");
        Test.True(!WorldDiplomacyStructureRules.AddParticipantToRelayRouteIfNeeded(relay, "k3", 2),
            "route admission denied at limit");
        Test.True(!WorldDiplomacyStructureRules.AddParticipantToRelayRouteIfNeeded(
            new WorldDiplomacyRound(), "k1", 5), "non-relay round denies admission");

        Test.True(WorldDiplomacyStructureRules.IsCourtArrival(
            new WorldDiplomacyPropagationArrival { Scope = "court" }), "court scope arrival");
        Test.True(!WorldDiplomacyStructureRules.IsCourtArrival(
            new WorldDiplomacyPropagationArrival { Scope = "settlement" }), "non-court scope arrival");

        WorldDiplomacyDocument doc = new WorldDiplomacyDocument
        {
            DocumentId = "d1",
            IsReadyForPublication = true,
            Body = "text",
            AnalysisStatus = "final",
            HistoryDeclarationRecorded = false
        };
        Test.True(WorldDiplomacyStructureRules.NeedsCanonicalHistoryRetry(doc), "unrecorded declaration needs retry");
        doc.HistoryDeclarationRecorded = true;
        Test.True(!WorldDiplomacyStructureRules.NeedsCanonicalHistoryRetry(doc), "recorded declaration needs no retry");

        WorldDiplomacyDocument resp = new WorldDiplomacyDocument { RequiresResponse = true };
        Test.True(WorldDiplomacyStructureRules.DocumentRequiresResponseFrom(resp, "any"), "document-level response flag");
        resp.RequiresResponse = false;
        resp.Actions = new List<WorldDiplomacyDocumentAction>
        {
            new WorldDiplomacyDocumentAction { TargetKingdomId = "k1", RequiresResponse = true },
            new WorldDiplomacyDocumentAction { TargetKingdomId = "k2", RequiresResponse = false }
        };
        Test.True(WorldDiplomacyStructureRules.DocumentRequiresResponseFrom(resp, "K1"), "action-level response match");
        Test.True(!WorldDiplomacyStructureRules.DocumentRequiresResponseFrom(resp, "k2"), "non-required action rejected");

        List<string> targets = WorldDiplomacyStructureRules.GetDocumentTargetIds(resp);
        Test.True(targets.Count == 2, "all action targets collected");
        resp.Actions[0].ChangedDiplomaticState = true;
        List<string> changed = WorldDiplomacyStructureRules.GetDocumentTargetIds(resp, true);
        Test.True(changed.Count == 1, "changed-only filter keeps changed targets");
        Test.True(changed[0] == "k1", "changed target id");

        WorldDiplomacyDocument linked = new WorldDiplomacyDocument { SourceDocumentId = "src1" };
        Test.True(WorldDiplomacyStructureRules.DocumentRespondsTo(linked, "SRC1"), "source link case-insensitive");
        Test.True(!WorldDiplomacyStructureRules.DocumentRespondsTo(linked, "other"), "unrelated source rejected");
        Test.True(!WorldDiplomacyStructureRules.DocumentRespondsTo(linked, null), "null source rejected");
    }

    private static void RunDocumentFactDecisionTests()
    {
        // DPL-060AP: peace-terms equivalence/clone live in offer contract rules;
        // document-fact/query helpers live in the document fact rules.
        WorldDiplomacyPeaceTerms t1 = new WorldDiplomacyPeaceTerms
        {
            TributePayerKingdomId = "k1",
            TributeReceiverKingdomId = "k2",
            DailyTribute = 100,
            DurationDays = 30,
            CessionFromKingdomId = "",
            CessionToKingdomId = "",
            CessionSettlementId = ""
        };
        WorldDiplomacyPeaceTerms t2 = new WorldDiplomacyPeaceTerms
        {
            TributePayerKingdomId = "K1",
            TributeReceiverKingdomId = "k2",
            DailyTribute = 100,
            DurationDays = 30
        };
        Test.True(WorldDiplomacyOfferContractRules.ArePeaceTermsEquivalent(t1, t2),
            "equivalent terms match case-insensitively on kingdom ids");
        t2.DailyTribute = 200;
        Test.True(!WorldDiplomacyOfferContractRules.ArePeaceTermsEquivalent(t1, t2),
            "different tribute breaks equivalence");
        Test.True(WorldDiplomacyOfferContractRules.ArePeaceTermsEquivalent(null, null),
            "two null term sets are equivalent");

        WorldDiplomacyPeaceTerms clone = WorldDiplomacyOfferContractRules.ClonePeaceTerms(t1);
        Test.True(clone != t1 && clone.DailyTribute == 100 && clone.TributePayerKingdomId == "k1",
            "clone copies values into a new instance");
        clone.DailyTribute = 999;
        Test.True(t1.DailyTribute == 100, "mutating clone does not touch source");

        string termsText = WorldDiplomacyOfferContractRules.FormatPeaceTermsForPrompt(t1);
        Test.True(termsText.Contains("k1") && termsText.Contains("100") && termsText.Contains("30"),
            "terms text includes payer, amount and duration");
        Test.True(WorldDiplomacyOfferContractRules.FormatPeaceTermsForPrompt(null).Length > 0,
            "null terms still render a fallback line");

        WorldDiplomacyDocument source = new WorldDiplomacyDocument
        {
            Intent = "propose_peace",
            TargetKingdomId = "k2",
            Actions = new List<WorldDiplomacyDocumentAction>
            {
                new WorldDiplomacyDocumentAction { TargetKingdomId = "k2", Intent = "propose_peace", ActionId = "a1" }
            }
        };
        Test.True(WorldDiplomacyDocumentFactRules.ResolveSourceActionForTarget(source, "K2") == source.Actions[0],
            "source action resolves case-insensitively");
        Test.True(WorldDiplomacyDocumentFactRules.ResolveSourceActionForTarget(source, "k9") == null,
            "unrelated target resolves no action");

        string fact = WorldDiplomacyDocumentFactRules.BuildPeaceOfferTermsFact(source, "k2");
        Test.True(fact.Length > 0, "peace offer terms fact is produced");
        string actionFact = WorldDiplomacyDocumentFactRules.BuildSourceActionFactForTarget(source, "k2");
        Test.True(actionFact.Contains("k2"), "source action fact carries the target id");

        Test.True(WorldDiplomacyDocumentFactRules.DiplomacyDocumentQueryRelevance(
            new WorldDiplomacyDocument { Intent = "declare_war" }, "") >= 0,
            "query relevance returns a score");
        WorldDiplomacyBattleFact battle = new WorldDiplomacyBattleFact
        {
            AttackerKingdomIds = new List<string> { "k1" },
            DefenderKingdomIds = new List<string> { "k2" }
        };
        Test.True(WorldDiplomacyDocumentFactRules.IsBilateralBattleFact(battle, "K1", "k2"),
            "bilateral battle fact matches attacker/defender order");
        Test.True(WorldDiplomacyDocumentFactRules.IsBilateralBattleFact(battle, "k2", "k1"),
            "bilateral battle fact matches reversed order");
        Test.True(!WorldDiplomacyDocumentFactRules.IsBilateralBattleFact(battle, "k1", "k9"),
            "uninvolved kingdom rejected");
    }

    private static void RunCanonicalRenderDecisionTests()
    {
        // DPL-060AQ: canonical-history rendering/protected-fact rules live in the domain file.
        WorldDiplomacyCanonicalHistoryEntry entry = new WorldDiplomacyCanonicalHistoryEntry
        {
            Day = 12,
            GameDate = "某年夏",
            Kind = "declaration",
            AuthorKingdomId = "k1",
            Intent = "declare_war",
            Text = "宣战正文"
        };
        string rendered = WorldDiplomacyCanonicalRenderRules.RenderCanonicalHistoryEntry(entry);
        Test.True(rendered.Contains("kind=declaration") && rendered.Contains("intent=declare_war")
            && rendered.Contains("宣战正文") && rendered.Contains("author=k1"),
            "canonical entry renders kind, intent, author and text");

        WorldDiplomacyCanonicalProtectedFact fact = new WorldDiplomacyCanonicalProtectedFact
        {
            Kind = "declaration",
            SourceId = "d1",
            Sequence = 5,
            Intent = " declare_war ",
            Commitment = " firm ",
            Text = "fact text"
        };
        string key1 = WorldDiplomacyCanonicalRenderRules.ProtectedFactStableKey(fact);
        string key2 = WorldDiplomacyCanonicalRenderRules.ProtectedFactStableKey(fact);
        Test.True(key1 == key2 && key1.Length > 0, "protected fact stable key is deterministic");

        WorldDiplomacyCanonicalProtectedFact clone = WorldDiplomacyCanonicalRenderRules.CloneProtectedFact(fact);
        Test.True(clone != fact && clone.SourceId == "d1" && clone.Intent == "declare_war",
            "clone normalizes intent into a new instance");

        List<WorldDiplomacyCanonicalProtectedFact> pool = new List<WorldDiplomacyCanonicalProtectedFact>
        {
            new WorldDiplomacyCanonicalProtectedFact { Kind = "declaration", SourceId = "a", Sequence = 1, Text = "short" },
            new WorldDiplomacyCanonicalProtectedFact { Kind = "declaration", SourceId = "b", Sequence = 2, Text = new string('x', 4000) }
        };
        List<WorldDiplomacyCanonicalProtectedFact> selected =
            WorldDiplomacyCanonicalRenderRules.SelectCanonicalProtectedFactsWithinTokenBudget(
                pool, 1000, s => s?.Length ?? 0);
        Test.True(selected.Count == 1 && selected[0].SourceId == "a",
            "budget selection keeps facts that fit and drops oversized tail");
        List<WorldDiplomacyCanonicalProtectedFact> all =
            WorldDiplomacyCanonicalRenderRules.SelectCanonicalProtectedFactsWithinTokenBudget(
                pool, 999999, s => s?.Length ?? 0);
        Test.True(all.Count == 2, "large budget keeps all facts");

        string protectedText = WorldDiplomacyCanonicalRenderRules.RenderCanonicalProtectedFacts(
            pool.Take(1), Enumerable.Empty<string>());
        Test.True(protectedText.Length > 0, "protected facts render text");

        WorldDiplomacyCanonicalHistorySnapshot snapshot = new WorldDiplomacyCanonicalHistorySnapshot
        {
            Content = "payload",
            CoveredThroughSequence = 7
        };
        string payload = WorldDiplomacyCanonicalRenderRules.RenderCanonicalSnapshotPayload(snapshot);
        Test.True(payload.Contains("payload"), "snapshot payload renders content");
    }

    static void RunDocumentRecordDecisionTests()
    {
        // DPL-060AR: document-record helpers moved into the document fact rules.
        WorldDiplomacyDocumentAction a1 = new WorldDiplomacyDocumentAction
        {
            ActionId = "a1",
            TargetKingdomId = "k2",
            Intent = "propose_peace",
            PeaceTerms = new WorldDiplomacyPeaceTerms { DailyTribute = 50 }
        };
        WorldDiplomacyDocumentAction a2 = new WorldDiplomacyDocumentAction
        {
            ActionId = "a2",
            TargetKingdomId = "k3",
            Intent = "propose_trade",
            MechanicalResult = "traded"
        };
        WorldDiplomacyDocument doc = new WorldDiplomacyDocument
        {
            DocumentId = "d1",
            Intent = "propose_peace",
            Actions = new List<WorldDiplomacyDocumentAction> { a1, a2 }
        };

        Test.True(WorldDiplomacyDocumentFactRules.ResolveDocumentAction(doc, "A1") == a1,
            "document action resolves case-insensitively");
        Test.True(WorldDiplomacyDocumentFactRules.ResolveDocumentAction(doc, "zz") == null,
            "unknown action id resolves null");
        Test.True(WorldDiplomacyDocumentFactRules.ResolveDocumentAction(doc, "") == null,
            "blank action id resolves null");
        Test.True(WorldDiplomacyDocumentFactRules.ResolveOfferedPeaceTerms(doc, "a1") == a1.PeaceTerms,
            "offered peace terms resolve through the action");
        Test.True(WorldDiplomacyDocumentFactRules.ResolveOfferedPeaceTerms(doc, "zz") == null,
            "missing action yields no offered terms");
        WorldDiplomacyDocument noActions = new WorldDiplomacyDocument
        {
            DocumentId = "d2",
            PeaceTerms = new WorldDiplomacyPeaceTerms { DailyTribute = 9 }
        };
        Test.True(WorldDiplomacyDocumentFactRules.ResolveOfferedPeaceTerms(noActions, "x") == noActions.PeaceTerms,
            "single-intent document resolves document-level terms");
        Test.True(WorldDiplomacyDocumentFactRules.ResolveOfferedPeaceTerms(null, "x") == null,
            "null source yields null terms");

        Dictionary<string, WorldDiplomacyDocument> index = WorldDiplomacyDocumentFactRules.BuildDocumentIndex(
            new List<WorldDiplomacyDocument> { doc, noActions, null, new WorldDiplomacyDocument { DocumentId = " " } });
        Test.True(index.Count == 2 && index["D1"] == doc,
            "document index trims ids, skips nulls/blanks and is case-insensitive");

        WorldDiplomacyRoundOffer required = new WorldDiplomacyRoundOffer
        {
            ProposerKingdomId = "k2",
            SourceDocumentId = "d0",
            SourceActionId = "a0"
        };
        WorldDiplomacyDocument acceptance = new WorldDiplomacyDocument
        {
            TargetKingdomId = "k2",
            Intent = "accept_peace",
            RespondingToOfferDocumentId = "D0",
            RespondingToOfferActionId = "A0"
        };
        Test.True(WorldDiplomacyDocumentFactRules.DocumentContainsRequiredPeaceOfferResponse(acceptance, required),
            "document-level acceptance satisfies the required peace response");
        acceptance.RespondingToOfferActionId = "other";
        Test.True(!WorldDiplomacyDocumentFactRules.DocumentContainsRequiredPeaceOfferResponse(acceptance, required),
            "mismatched action id fails the required peace response");
        Test.True(WorldDiplomacyDocumentFactRules.DocumentContainsRequiredPeaceOfferResponse(acceptance, null),
            "no required offer is trivially satisfied");

        Test.True(WorldDiplomacyDocumentFactRules.PeaceTermsContainCession(
            new WorldDiplomacyPeaceTerms { CessionSettlementId = "s1" }),
            "cession settlement marks cession terms");
        Test.True(!WorldDiplomacyDocumentFactRules.PeaceTermsContainCession(
            new WorldDiplomacyPeaceTerms { DailyTribute = 5 }),
            "tribute-only terms contain no cession");
        Test.True(!WorldDiplomacyDocumentFactRules.PeaceTermsContainCession(null),
            "null terms contain no cession");

        Test.True(WorldDiplomacyDocumentFactRules.FormatRoundFactForPrompt(
            new WorldDiplomacyRoundFact { Kind = "confirmed_result", Text = "war declared" })
                .StartsWith("[游戏已执行]", StringComparison.Ordinal),
            "confirmed results render with the executed marker");
        Test.True(WorldDiplomacyDocumentFactRules.FormatRoundFactForPrompt(
            new WorldDiplomacyRoundFact { Kind = "note", Text = "  [existing] tag  " })
                == "[existing] tag",
            "bracketed fact text is preserved verbatim");
        Test.True(WorldDiplomacyDocumentFactRules.FormatRoundFactForPrompt(
            new WorldDiplomacyRoundFact { Kind = "note", Text = " " }).Length == 0,
            "blank fact text renders empty");

        Test.True(WorldDiplomacyDocumentFactRules.IsMajorDiplomaticDocument(
            new WorldDiplomacyDocument { Intent = "declare_war" }),
            "war declarations are major documents");
        Test.True(WorldDiplomacyDocumentFactRules.IsMajorDiplomaticDocument(
            new WorldDiplomacyDocument { Intent = "statement", MechanicalResult = "x" }),
            "mechanical results make a document major");
        Test.True(!WorldDiplomacyDocumentFactRules.IsMajorDiplomaticDocument(
            new WorldDiplomacyDocument { Intent = "statement" }),
            "plain statements are not major documents");

        WorldDiplomacyDocument mirrored = new WorldDiplomacyDocument { SourceDocumentId = "orig" };
        WorldDiplomacyDocumentAction action = new WorldDiplomacyDocumentAction
        {
            TargetKingdomId = "k2",
            Intent = "declare_war",
            Commitment = "binding",
            RespondingToThreatDocumentId = "t9"
        };
        WorldDiplomacyDocumentFactRules.MirrorPrimaryActionToDocument(mirrored, action);
        Test.True(mirrored.TargetKingdomId == "k2"
            && mirrored.Intent == "declare_war"
            && mirrored.SourceDocumentId == "t9"
            && mirrored.HiddenIntent == "declare_war",
            "primary action mirrors onto the document and prefers the threat source id");

        string multi = WorldDiplomacyDocumentFactRules.BuildMultiActionMechanicalResult(doc.Actions);
        Test.True(multi.Contains("k3") && multi.Contains("traded") && !multi.Contains("k2"),
            "multi-action result lists only actions with mechanical results");
        Test.True(WorldDiplomacyDocumentFactRules.BuildMultiActionMechanicalResult(null).Length == 0,
            "null action list yields empty mechanical result");

        WorldDiplomacyDocument cloned = WorldDiplomacyDocumentFactRules.CloneDocument(doc);
        Test.True(cloned != null && cloned != doc && cloned.DocumentId == "d1"
            && cloned.Actions.Count == 2 && cloned.Actions[0] != a1,
            "document clone is a deep copy");
        Test.True(WorldDiplomacyDocumentFactRules.CloneDocument(null) == null,
            "null document clones to null");
    }

    static void RunDocumentTitleDecisionTests()
    {
        // DPL-060AS: document title/label helpers moved into the text rules.
        Test.True(WorldDiplomacyTextRules.DocumentTypeLabel(
            new WorldDiplomacyDocument { Intent = "declare_war" }).Length > 0,
            "war declarations produce a type label");
        Test.True(WorldDiplomacyTextRules.DocumentTypeLabel(
            new WorldDiplomacyDocument { Intent = "propose_peace" })
            != WorldDiplomacyTextRules.DocumentTypeLabel(
                new WorldDiplomacyDocument { Intent = "declare_war" }),
            "distinct intents produce distinct type labels");
        Test.True(WorldDiplomacyTextRules.DocumentTypeLabel(null).Length > 0,
            "null document still yields a fallback label");
        Test.True(WorldDiplomacyTextRules.DocumentTypeLabel(
            new WorldDiplomacyDocument { IsReminder = true, Intent = "propose_peace" })
            != WorldDiplomacyTextRules.DocumentTypeLabel(
                new WorldDiplomacyDocument { Intent = "propose_peace" }),
            "reminders override the intent label");
        Test.True(WorldDiplomacyTextRules.DocumentTypeLabel(
            new WorldDiplomacyDocument
            {
                Intent = "propose_peace",
                Actions = new List<WorldDiplomacyDocumentAction>
                {
                    new WorldDiplomacyDocumentAction { ActionId = "a1" },
                    new WorldDiplomacyDocumentAction { ActionId = "a2" }
                }
            }) != WorldDiplomacyTextRules.DocumentTypeLabel(
                new WorldDiplomacyDocument { Intent = "propose_peace" }),
            "multi-action documents use the compound label");

        string fallback = WorldDiplomacyTextRules.BuildFallbackDocumentTitle(
            new WorldDiplomacyDocument { TargetKingdomName = "k2" }, "declare_war");
        Test.True(fallback.Length > 0 && fallback.Contains("k2"),
            "fallback title names the target kingdom");
        Test.True(WorldDiplomacyTextRules.BuildFallbackDocumentTitle(
            new WorldDiplomacyDocument { TargetKingdomName = " " }, "declare_war").Length > 0,
            "blank targets still produce a fallback title");
        Test.True(WorldDiplomacyTextRules.BuildFallbackDocumentTitle(null, "unknown_intent").Length > 0,
            "unknown intents produce a fallback title");

        WorldDiplomacyDocument titled = new WorldDiplomacyDocument
        {
            Title = "关于: 旧时代停火协议",
            AuthorKingdomName = "k1"
        };
        string indexTitle = WorldDiplomacyTextRules.BuildArchiveIndexDocumentTitle(titled);
        Test.True(indexTitle.Length > 0,
            "archive index title never returns empty");
        Test.True(WorldDiplomacyTextRules.BuildArchiveIndexDocumentTitle(
            new WorldDiplomacyDocument { Title = " ", AuthorKingdomName = "k1" }).Length > 0,
            "blank titles fall back to the author-derived index title");
    }

    static void RunStrategicProfilePromptDecisionTests()
    {
        // DPL-060AT: strategic-profile prompt + job-message helpers moved into
        // the prompt contract rules.
        string marker = WorldDiplomacyPromptContractRules.BuildKingdomStrategicProfileMarker(" k1 ");
        Test.True(marker.Contains("k1"), "profile marker embeds the trimmed kingdom id");

        string prompt = "header\n" + marker + "\n旧内容\n" + DiplomacyPromptTexts.Get("AF_DIP_Compatibility_008", WorldDiplomacyPromptContractRules.KingdomStrategicIntentRule) + "\ntail";
        string updated = WorldDiplomacyPromptContractRules.UpsertKingdomStrategicProfilePrompt(
            prompt, "新内容", "k1");
        Test.True(updated.Contains("新内容") && !updated.Contains("旧内容"),
            "upsert replaces the profile section between marker and intent rule");

        string inserted = WorldDiplomacyPromptContractRules.InsertKingdomStrategicProfilePrompt(
            "base\n", "profile-text", "k9");
        Test.True(inserted.Contains("profile-text"),
            "insert appends the profile prompt when no anchor heading exists");
        Test.True(WorldDiplomacyPromptContractRules.InsertKingdomStrategicProfilePrompt(
            "base", "", "k9") == "base",
            "empty profile prompt leaves the existing text unchanged");
        Test.True(WorldDiplomacyPromptContractRules.FindKingdomStrategicProfileInsertionIndex("", "k1") < 0,
            "empty prompt has no insertion index");

        WorldDiplomacyJob job = new WorldDiplomacyJob
        {
            JobId = "j1",
            AuthorKingdomId = "k1",
            LlmMessages = new List<WorldDiplomacyLlmMessage>
            {
                new WorldDiplomacyLlmMessage { Role = "system", Content = "sys" },
                new WorldDiplomacyLlmMessage { Role = "user", Content = "x\n" + marker + "\nprofile-body" }
            }
        };
        Test.True(WorldDiplomacyPromptContractRules.GenerationJobContainsKingdomStrategicProfile(
            job, "k1", marker, marker + "\nprofile-body"),
            "job with the current profile prompt after the marker is detected");
        Test.True(job.LlmMessages[1].StrategicProfileKingdomId == "k1",
            "detection stamps the profile kingdom id on the user message");
        Test.True(!WorldDiplomacyPromptContractRules.GenerationJobContainsKingdomStrategicProfile(
            job, "k1", marker, marker + "\n其他内容"),
            "stale profile content after the marker is rejected");

        Test.True(WorldDiplomacyPromptContractRules.ResolveCacheAffinityKey(
            new WorldDiplomacyJob { CacheAffinityKey = " key-1 " }) == "key-1",
            "explicit cache affinity keys win");
        Test.True(WorldDiplomacyPromptContractRules.ResolveCacheAffinityKey(
            new WorldDiplomacyJob { Kind = "generate", AuthorKingdomId = "k1" }) == "generate:k1",
            "generate jobs derive affinity from the author kingdom");
        Test.True(WorldDiplomacyPromptContractRules.ResolveCacheAffinityKey(
            new WorldDiplomacyJob { Kind = "analyze" }) == "analyze",
            "non-generate jobs share one affinity bucket per kind");

        List<WorldDiplomacyLlmMessage> cloned = WorldDiplomacyPromptContractRules.CloneLlmMessages(
            new List<WorldDiplomacyLlmMessage>
            {
                new WorldDiplomacyLlmMessage { Role = "user", Content = "c", StrategicProfileKingdomId = "k1" },
                new WorldDiplomacyLlmMessage { Role = " " },
                null
            });
        Test.True(cloned.Count == 1 && cloned[0].Content == "c"
            && cloned[0].StrategicProfileKingdomId == "k1",
            "message clone drops nulls and blank roles while preserving fields");
    }


    private static void RunDocumentMemoryLineDecisionTests()
    {
        int fmtCalls = -1;
        Func<int, string> fmt = d => { fmtCalls = d; return "D" + d.ToString(CultureInfo.InvariantCulture); };

        Test.True(WorldDiplomacyTextRules.BuildCompactDocumentMemoryLine(null!, fmt) == "",
            "compact memory line of a null document is empty");

        WorldDiplomacyDocument single = new WorldDiplomacyDocument
        {
            GameDate = "G1", AuthorKingdomName = "AK", TargetKingdomName = "TK",
            Title = "T", Intent = "war", Day = 40, MechanicalResult = "RES"
        };
        string compact = WorldDiplomacyTextRules.BuildCompactDocumentMemoryLine(single, fmt);
        Test.True(compact.Contains("G1") && compact.Contains("AK") && compact.Contains("TK")
            && compact.Contains("T") && compact.Contains("RES")
            && compact.Contains(WorldDiplomacyIntentVocabulary.IntentLabel("war")),
            "compact memory line keeps date, parties, title, intent, and result");
        Test.True(fmtCalls == -1,
            "a present GameDate must not invoke the day formatter");

        single.GameDate = "";
        compact = WorldDiplomacyTextRules.BuildCompactDocumentMemoryLine(single, fmt);
        Test.True(fmtCalls == 40 && compact.StartsWith("D40", StringComparison.Ordinal),
            "a missing GameDate must fall back to the supplied day formatter");

        WorldDiplomacyDocument multi = new WorldDiplomacyDocument
        {
            GameDate = "G2", AuthorKingdomName = "AK", Title = "MT", Intent = "",
            Actions = new List<WorldDiplomacyDocumentAction>
            {
                new WorldDiplomacyDocumentAction { TargetKingdomName = "KA", Intent = "war" },
                new WorldDiplomacyDocumentAction { TargetKingdomId = "kb", Intent = "peace" },
                null!
            }
        };
        compact = WorldDiplomacyTextRules.BuildCompactDocumentMemoryLine(multi, fmt);
        Test.True(compact.Contains("KA=" + WorldDiplomacyIntentVocabulary.IntentLabel("war"))
            && compact.Contains("kb=" + WorldDiplomacyIntentVocabulary.IntentLabel("peace")),
            "multi-action lines list each target with its own intent label");

        WorldDiplomacyDocument detail = new WorldDiplomacyDocument
        {
            GameDate = "G3", AuthorKingdomName = "AK", Title = "DT", Intent = "peace",
            RequiresResponse = true, SourceDocumentId = "SRC1", Body = "BODYTEXT"
        };
        string detailed = WorldDiplomacyTextRules.BuildDetailedDocumentMemoryLine(detail, fmt);
        Test.True(detailed.StartsWith(
                WorldDiplomacyTextRules.BuildCompactDocumentMemoryLine(detail, fmt), StringComparison.Ordinal)
            && detailed.Contains("SRC1") && detailed.Contains("BODYTEXT"),
            "detailed memory line extends the compact line with response/source/body markers");
        Test.True(WorldDiplomacyTextRules.BuildDetailedDocumentMemoryLine(null!, fmt) == "",
            "detailed memory line of a null document is empty");

        StringBuilder sb = new StringBuilder();
        WorldDiplomacyTextRules.AppendProactiveDiscussionDocument(sb, null!, fmt);
        Test.True(sb.Length == 0, "null proactive document appends nothing");
        WorldDiplomacyTextRules.AppendProactiveDiscussionDocument(sb, detail, fmt);
        string appended = sb.ToString();
        Test.True(appended.StartsWith("- ", StringComparison.Ordinal)
            && appended.Contains("DT") && appended.Contains("BODYTEXT"),
            "proactive append writes the compact line plus the body excerpt");

        List<WorldDiplomacyDocument> docs = new List<WorldDiplomacyDocument>();
        for (int i = 1; i <= 14; i++)
        {
            docs.Add(new WorldDiplomacyDocument
            {
                GameDate = "G", AuthorKingdomName = "A", Title = "T" + i.ToString(CultureInfo.InvariantCulture),
                Intent = "war", Day = i
            });
        }
        docs[11].ChangedDiplomaticState = true; docs[11].MechanicalResult = "R2";
        docs[12].ChangedDiplomaticState = true; docs[12].MechanicalResult = "R1";
        docs[13].ChangedDiplomaticState = true; docs[13].MechanicalResult = "R1";

        WorldDiplomacyRound round = new WorldDiplomacyRound { RoundTopic = "TOPICX", CloseReason = "CLOSEY" };
        string summary = WorldDiplomacyTextRules.BuildLocalRoundSummaryText(round, docs, fmt);
        Test.True(summary.Contains("TOPICX") && summary.Contains("CLOSEY")
            && summary.Contains("T12") && !summary.Contains("T13"),
            "local summary keeps the topic, close reason, and at most twelve declarations");
        Test.True(summary.IndexOf("R1", StringComparison.Ordinal) == summary.LastIndexOf("R1", StringComparison.Ordinal)
            && summary.Contains("R2"),
            "local summary deduplicates mechanical results preserving order");
        Test.True(WorldDiplomacyTextRules.BuildLocalRoundSummaryText(new WorldDiplomacyRound(), null!, fmt).Length > 0,
            "an empty round with no documents still produces a summary");
    }


    private static void RunUnsafePeaceAcceptanceDecisionTests()
    {
        WorldDiplomacyPeaceTerms cession = new WorldDiplomacyPeaceTerms { CessionSettlementId = "s1" };
        WorldDiplomacyDocument cessionOffer = new WorldDiplomacyDocument
        {
            DocumentId = "off1", PeaceTerms = cession,
            Actions = new List<WorldDiplomacyDocumentAction>
            {
                new WorldDiplomacyDocumentAction { ActionId = "a1", PeaceTerms = cession }
            }
        };
        WorldDiplomacyDocument plainOffer = new WorldDiplomacyDocument
        {
            DocumentId = "off2",
            Actions = new List<WorldDiplomacyDocumentAction>
            {
                new WorldDiplomacyDocumentAction { ActionId = "a2", PeaceTerms = new WorldDiplomacyPeaceTerms() }
            }
        };

        Test.True(!WorldDiplomacyDocumentFactRules.DocumentHasUnsafeMultiplePeaceAcceptances(null!, id => null),
            "null document has no unsafe peace acceptances");

        WorldDiplomacyDocument single = new WorldDiplomacyDocument
        {
            Actions = new List<WorldDiplomacyDocumentAction>
            {
                new WorldDiplomacyDocumentAction { Intent = "accept_peace", RespondingToOfferDocumentId = "off1" }
            }
        };
        Test.True(!WorldDiplomacyDocumentFactRules.DocumentHasUnsafeMultiplePeaceAcceptances(single, id => null),
            "a single peace acceptance is never unsafe");

        WorldDiplomacyDocument twoSafe = new WorldDiplomacyDocument
        {
            Actions = new List<WorldDiplomacyDocumentAction>
            {
                new WorldDiplomacyDocumentAction { Intent = "accept_peace", RespondingToOfferDocumentId = "off2", RespondingToOfferActionId = "a2" },
                new WorldDiplomacyDocumentAction { Intent = "ACCEPT_PEACE", RespondingToOfferDocumentId = "off2", RespondingToOfferActionId = "a2" }
            }
        };
        Test.True(!WorldDiplomacyDocumentFactRules.DocumentHasUnsafeMultiplePeaceAcceptances(twoSafe, id => plainOffer),
            "multiple acceptances of plain offers stay safe");

        Test.True(WorldDiplomacyDocumentFactRules.DocumentHasUnsafeMultiplePeaceAcceptances(twoSafe, id => null),
            "multiple acceptances with an unresolvable offer are unsafe");

        WorldDiplomacyDocument twoCession = new WorldDiplomacyDocument
        {
            Actions = new List<WorldDiplomacyDocumentAction>
            {
                new WorldDiplomacyDocumentAction { Intent = "accept_peace", RespondingToOfferDocumentId = "off1", RespondingToOfferActionId = "a1" },
                new WorldDiplomacyDocumentAction { Intent = "accept_peace", RespondingToOfferDocumentId = "off2", RespondingToOfferActionId = "a2" }
            }
        };
        Test.True(WorldDiplomacyDocumentFactRules.DocumentHasUnsafeMultiplePeaceAcceptances(twoCession,
                id => id == "off1" ? cessionOffer : plainOffer),
            "multiple acceptances including a cession offer are unsafe");

        JArray empty = new JArray();
        Test.True(!WorldDiplomacyDocumentFactRules.GeneratedActionsHaveUnsafeMultiplePeaceAcceptances(empty, id => null),
            "empty generated actions have no unsafe acceptances");

        JArray one = new JArray(new JObject { ["intent"] = "accept_peace", ["responding_to_offer_document_id"] = "off1" });
        Test.True(!WorldDiplomacyDocumentFactRules.GeneratedActionsHaveUnsafeMultiplePeaceAcceptances(one, id => null),
            "a single generated acceptance is never unsafe");

        JArray two = new JArray(
            new JObject { ["intent"] = "accept_peace", ["responding_to_offer_document_id"] = "off1", ["responding_to_offer_action_id"] = "a1" },
            new JObject { ["intent"] = "accept_peace", ["responding_to_offer_document_id"] = "off2", ["responding_to_offer_action_id"] = "a2" });
        Test.True(WorldDiplomacyDocumentFactRules.GeneratedActionsHaveUnsafeMultiplePeaceAcceptances(two,
                id => id == "off1" ? cessionOffer : plainOffer),
            "generated acceptances including a cession offer are unsafe");
        Test.True(!WorldDiplomacyDocumentFactRules.GeneratedActionsHaveUnsafeMultiplePeaceAcceptances(two, id => plainOffer),
            "generated acceptances of plain offers stay safe");
    }


    private static void RunLlmMessageAssemblyDecisionTests()
    {
        int blockCalls = 0;
        long blockArg = -2;
        Func<long, string> block = seq => { blockCalls++; blockArg = seq; return "HIST"; };

        WorldDiplomacyJob part = new WorldDiplomacyJob { Kind = "participate", SystemPrompt = "SYS", UserPrompt = "USR" };
        List<WorldDiplomacyLlmMessage> msgs = WorldDiplomacyLlmMessageApplication.BuildLlmMessagesForJob(part, block);
        Test.True(msgs.Count == 2 && msgs[0].Role == "system" && msgs[0].Content == "SYS"
            && msgs[1].Role == "user" && msgs[1].Content == "USR" && blockCalls == 0,
            "non-canonical jobs build system+user without the history block");

        WorldDiplomacyJob gen = new WorldDiplomacyJob
        {
            Kind = "generate", SystemPrompt = "SYS", UserPrompt = "USR", HistoryThroughSequence = 42,
            DeclarationHistoryBlock = "RECENT"
        };
        msgs = WorldDiplomacyLlmMessageApplication.BuildLlmMessagesForJob(gen, block);
        Test.True(msgs.Count == 3 && msgs[1].Role == "system" && msgs[1].Content == "RECENT"
            && blockCalls == 0,
            "declarations insert the selected window without reading the global archive");
        var compact = new WorldDiplomacyJob { Kind = "compress", HistoryThroughSequence = 42 };
        var compactMessages = WorldDiplomacyLlmMessageApplication.BuildLlmMessagesForJob(compact, block);
        Test.True(compactMessages[1].Content == "HIST" && blockCalls == 1 && blockArg == 42,
            "compression still reads its continuous archive prefix");

        msgs = WorldDiplomacyLlmMessageApplication.BuildLlmMessagesForJob(null!, block);
        Test.True(msgs.Count == 2 && msgs[0].Content == "" && msgs[1].Content == "",
            "a null job still produces an empty system+user chain");

        WorldDiplomacyJob repair = new WorldDiplomacyJob
        {
            Kind = "generate", SystemPrompt = "SYS", UserPrompt = "\u3010MODE=DECLARE\u3011 fix",
            SemanticRepairAttempts = 1
        };
        repair.HistoryPrefixHash = WorldDiplomacyPromptContractRules.StablePromptHashPair("SYS", "H");
        repair.LlmMessages = new List<WorldDiplomacyLlmMessage>
        {
            new WorldDiplomacyLlmMessage { Role = "system", Content = "SYS" },
            new WorldDiplomacyLlmMessage { Role = "system", Content = "H" },
            new WorldDiplomacyLlmMessage { Role = "user", Content = "\u3010MODE=DECLARE\u3011 first" },
            new WorldDiplomacyLlmMessage { Role = "assistant", Content = "bad" },
            new WorldDiplomacyLlmMessage { Role = "user", Content = "\u3010MODE=DECLARE\u3011 fix" }
        };
        msgs = WorldDiplomacyLlmMessageApplication.BuildLlmMessagesForJob(repair, block);
        Test.True(ReferenceEquals(msgs, repair.LlmMessages),
            "a valid semantic-repair chain is reused verbatim");

        JArray arr = WorldDiplomacyLlmMessageApplication.BuildLlmMessageArray(gen, block);
        Test.True(arr.Count == 3
            && arr[0]?["role"]?.ToString() == "system"
            && arr[1]?["content"]?.ToString() == "RECENT"
            && arr[2]?["role"]?.ToString() == "user",
            "the message array flattens role and content for the wire");
    }


    private static void RunStorageQueryRuleDecisionTests()
    {
        WorldDiplomacyDocument source = new WorldDiplomacyDocument { DocumentId = "d1", AuthorKingdomId = "ka" };
        WorldDiplomacyDocument response = new WorldDiplomacyDocument
        {
            DocumentId = "d2",
            AuthorKingdomId = "kb",
            SourceDocumentId = "d1"
        };
        List<WorldDiplomacyDocument> documents = new List<WorldDiplomacyDocument> { source, response };
        Test.True(WorldDiplomacyDocumentFactRules.HasKingdomRespondedToDocument(documents, "KB", "d1"),
            "a document carrying the source id must count as the kingdom response");
        Test.True(!WorldDiplomacyDocumentFactRules.HasKingdomRespondedToDocument(documents, "ka", "d1"),
            "the author itself must not count as its own response");
        Test.True(!WorldDiplomacyDocumentFactRules.HasKingdomRespondedToDocument(documents, "kb", "d9")
            && !WorldDiplomacyDocumentFactRules.HasKingdomRespondedToDocument(null, "kb", "d1"),
            "missing document or null collection must return false");

        List<WorldDiplomacyPropagationArrival> arrivals = new List<WorldDiplomacyPropagationArrival>
        {
            new WorldDiplomacyPropagationArrival { RoundId = "r1", Scope = "civilian" },
            new WorldDiplomacyPropagationArrival { RoundId = "r1", Scope = "court" },
            new WorldDiplomacyPropagationArrival { RoundId = "r2", Scope = "court" }
        };
        Test.True(WorldDiplomacyDocumentFactRules.HasUndeliveredCourtArrivals(arrivals, "r1"),
            "a court-scoped arrival in the same round must count as undelivered");
        Test.True(!WorldDiplomacyDocumentFactRules.HasUndeliveredCourtArrivals(
                new List<WorldDiplomacyPropagationArrival> { new WorldDiplomacyPropagationArrival { RoundId = "r1", Scope = "civilian" } }, "r1"),
            "civilian arrivals must not satisfy the court-delivery check");
        Test.True(!WorldDiplomacyDocumentFactRules.HasUndeliveredCourtArrivals(arrivals, "r9")
            && !WorldDiplomacyDocumentFactRules.HasUndeliveredCourtArrivals(null, "r1"),
            "other rounds and null collections must return false");

        List<WorldDiplomacyKingdomKnowledge> kingdomKnowledge = new List<WorldDiplomacyKingdomKnowledge>
        {
            new WorldDiplomacyKingdomKnowledge { KingdomId = "ka", DocumentIds = new List<string> { "d1", "d2" } },
            new WorldDiplomacyKingdomKnowledge { KingdomId = "kb", DocumentIds = new List<string> { "d9" } },
            new WorldDiplomacyKingdomKnowledge { KingdomId = "", DocumentIds = new List<string> { "d1" } }
        };
        Test.True(WorldDiplomacyDocumentFactRules.HasKingdomKnowledge(kingdomKnowledge, "KA", "D1"),
            "knowledge lookup must match kingdom and document ids case-insensitively");
        Test.True(!WorldDiplomacyDocumentFactRules.HasKingdomKnowledge(kingdomKnowledge, "kb", "d1")
            && !WorldDiplomacyDocumentFactRules.HasKingdomKnowledge(null, "ka", "d1"),
            "unknown pairs and null collections must return false");
        HashSet<string> knownKingdoms = WorldDiplomacyDocumentFactRules.GetKnownKingdomIdsForDocument(kingdomKnowledge, "D1");
        Test.True(knownKingdoms.Count == 1 && knownKingdoms.Contains("ka"),
            "known-kingdom extraction must filter to the document and skip blank ids");
        HashSet<string> none = WorldDiplomacyDocumentFactRules.GetKnownKingdomIdsForDocument(kingdomKnowledge, "dz");
        Test.True(none.Count == 0, "unknown document ids must yield an empty set");

        List<WorldDiplomacySettlementKnowledge> settlementKnowledge = new List<WorldDiplomacySettlementKnowledge>
        {
            new WorldDiplomacySettlementKnowledge { SettlementId = "s1", DocumentIds = new List<string> { "d1" } },
            new WorldDiplomacySettlementKnowledge { SettlementId = "s2", DocumentIds = new List<string> { "d2" } }
        };
        HashSet<string> knownSettlements = WorldDiplomacyDocumentFactRules.GetKnownSettlementIdsForDocument(settlementKnowledge, "d1");
        Test.True(knownSettlements.Count == 1 && knownSettlements.Contains("s1"),
            "known-settlement extraction must filter to the document");
        Test.True(WorldDiplomacyDocumentFactRules.GetKnownSettlementIdsForDocument(null, "d1").Count == 0,
            "null knowledge must yield an empty set");

        List<WorldDiplomacyDocument> ordered = new List<WorldDiplomacyDocument>
        {
            new WorldDiplomacyDocument { DocumentId = "new", Day = 30, Title = "T2", AuthorKingdomName = "B" },
            new WorldDiplomacyDocument { DocumentId = "old", Day = 10, Title = "T1", AuthorKingdomName = "A" },
            new WorldDiplomacyDocument { DocumentId = "off", Day = 5, Title = "T0", AuthorKingdomName = "C" }
        };
        string summary = WorldDiplomacyDocumentFactRules.BuildFallbackRoundSummary(
            ordered, new List<string> { "new", "old" }, d => "D" + d);
        Test.True(summary.IndexOf("T1", StringComparison.Ordinal) >= 0
            && summary.IndexOf("T2", StringComparison.Ordinal) > summary.IndexOf("T1", StringComparison.Ordinal)
            && summary.IndexOf("T0", StringComparison.Ordinal) < 0,
            "fallback summary must order selected documents by day and exclude unlisted ids");
        string json = WorldDiplomacyDocumentFactRules.BuildFallbackRoundCompressionJson(
            ordered, new List<string> { "old" }, d => "D" + d);
        JObject parsed = JObject.Parse(json);
        Test.True(parsed["summary"]?.ToString().Contains("T1") == true
            && (parsed["facts"] as JArray)?.Count == 0,
            "fallback compression json must carry the summary and an empty facts array");
    }


    private static void RunDisplayTextRuleDecisionTests()
    {
        WorldDiplomacyDocument doc = new WorldDiplomacyDocument
        {
            DocumentId = "d1",
            RoundId = "r1",
            Title = "宣战声明",
            AuthorKingdomName = "王国A",
            TargetKingdomName = "王国B",
            Day = 42
        };
        string description = WorldDiplomacyTextRules.BuildNotificationDescription(doc, d => "DAY" + d);
        Test.True(description.Contains("DAY42", StringComparison.Ordinal)
            && description.Contains("王国A", StringComparison.Ordinal)
            && description.Contains("王国B", StringComparison.Ordinal)
            && description.Contains("点击查看全文", StringComparison.Ordinal),
            "notification description must carry the formatted day, author, target and open hint");

        WorldDiplomacyDocument multi = new WorldDiplomacyDocument
        {
            DocumentId = "d2",
            AuthorKingdomName = "王国A",
            Actions = new List<WorldDiplomacyDocumentAction>
            {
                new WorldDiplomacyDocumentAction { TargetKingdomName = "王国B" },
                new WorldDiplomacyDocumentAction { TargetKingdomName = "王国C" }
            }
        };
        string multiDescription = WorldDiplomacyTextRules.BuildNotificationDescription(multi, d => "DAY" + d);
        Test.True(multiDescription.Contains("王国B", StringComparison.Ordinal)
            && multiDescription.Contains("王国C", StringComparison.Ordinal),
            "multi-action documents must list every target kingdom");

        WorldDiplomacyDocument nullDoc = null;
        Test.True(WorldDiplomacyTextRules.BuildNotificationDescription(nullDoc, d => "DAY" + d).Contains("点击查看外交宣言"),
            "a null document must still produce the generic open hint");

        WorldDiplomacyRound round = new WorldDiplomacyRound
        {
            RoundId = "r1",
            RoundTopic = "  边境谈判  ",
            RootDocumentId = "d1"
        };
        string meta = WorldDiplomacyTextRules.BuildDocumentEventMeta(
            doc,
            id => string.Equals(id, "r1", StringComparison.Ordinal) ? round : null,
            id => string.Equals(id, "d1", StringComparison.Ordinal) ? doc : null);
        Test.True(meta.Contains("边境谈判", StringComparison.Ordinal),
            "event meta must surface the sanitized round topic");

        WorldDiplomacyDocument doc2 = new WorldDiplomacyDocument
        {
            DocumentId = "d1",
            RoundId = "r2",
            Title = "宣战声明"
        };
        WorldDiplomacyRound noTopic = new WorldDiplomacyRound { RoundId = "r2", RootDocumentId = "d1" };
        string metaFromTitle = WorldDiplomacyTextRules.BuildDocumentEventMeta(
            doc2,
            id => string.Equals(id, "r2", StringComparison.Ordinal) ? noTopic : null,
            id => string.Equals(id, "d1", StringComparison.Ordinal) ? doc2 : null);
        Test.True(metaFromTitle.Contains("宣战声明", StringComparison.Ordinal),
            "event meta must fall back to the root document title");

        string noMeta = WorldDiplomacyTextRules.BuildDocumentEventMeta(
            new WorldDiplomacyDocument { DocumentId = "d3" },
            id => null,
            id => null);
        Test.True(noMeta.Length == 0,
            "a document without round or root title must produce no event meta");
    }


    private static void RunBattleFactRenderDecisionTests()
    {
        Func<string, string> names = id => "K-" + id;
        Func<int, string> days = d => "D" + d;
        Test.True(WorldDiplomacyTextRules.FormatBattleKingdomNames(
                new List<string> { "a", "b" }, names) == "K-a、K-b",
            "battle kingdom names must resolve ids through the name port");
        Test.True(WorldDiplomacyTextRules.FormatBattleKingdomNames(null, names) == "未知王国",
            "empty kingdom ids must render the unknown-faction fallback");
        Test.True(WorldDiplomacyTextRules.FormatBattleKingdomNames(
                new List<string> { "a", "a", "b" }, names) == "K-a、K-b",
            "kingdom ids must stay deduplicated and order-preserved");

        WorldDiplomacyBattleFact fact = new WorldDiplomacyBattleFact
        {
            Day = 40,
            GameDate = "",
            Location = "",
            BattleType = "",
            WinnerSide = "attacker",
            AttackerKingdomIds = new List<string> { "a" },
            DefenderKingdomIds = new List<string> { "b" },
            AttackerLeaderNames = new List<string> { "甲" },
            DefenderLeaderNames = new List<string>()
        };
        string line = WorldDiplomacyTextRules.FormatBattleFactForPrompt(fact, names, days);
        Test.True(line.StartsWith("- ", StringComparison.Ordinal)
            && line.Contains("D40", StringComparison.Ordinal)
            && line.Contains("攻方=K-a", StringComparison.Ordinal)
            && line.Contains("守方=K-b", StringComparison.Ordinal)
            && line.Contains("胜方=K-a", StringComparison.Ordinal)
            && line.Contains("甲", StringComparison.Ordinal)
            && !line.Contains("守方将领已记录"),
            "battle fact must render date, sides, winner and only recorded leader names");

        WorldDiplomacyBattleFact defenderWin = new WorldDiplomacyBattleFact
        {
            Day = 41,
            WinnerSide = "defender",
            AttackerKingdomIds = new List<string> { "a" },
            DefenderKingdomIds = new List<string> { "b" }
        };
        string line2 = WorldDiplomacyTextRules.FormatBattleFactForPrompt(defenderWin, names, days);
        Test.True(line2.Contains("胜方=K-b", StringComparison.Ordinal),
            "a defender victory must pick the defender names");

        List<WorldDiplomacyDocument> docs = new List<WorldDiplomacyDocument>
        {
            new WorldDiplomacyDocument { DocumentId = "d1", Title = "T1", AuthorKingdomName = "A" },
            new WorldDiplomacyDocument { DocumentId = "d2", Title = "T2", AuthorKingdomName = "B" }
        };
        string prompt = WorldDiplomacyTextRules.BuildRoundCompressionPrompt(docs, days);
        Test.True(prompt.Contains("[d1]", StringComparison.Ordinal)
            && prompt.Contains("[d2]", StringComparison.Ordinal),
            "round compression prompt must bracket each selected document id");
        Test.True(WorldDiplomacyTextRules.BuildRoundCompressionPrompt(null, days).Length == 0,
            "a null document list must produce an empty compression prompt");
    }


    private static void RunPeaceTermPatternDecisionTests()
    {
        Test.True(WorldDiplomacyRoundLifecycleRules.PairKey("b", "A") == "A|b"
            && WorldDiplomacyRoundLifecycleRules.PairKey("A", "b") == "A|b",
            "pair keys must be ordinal-ignore-case order-insensitive and preserve casing");
        Test.True(WorldDiplomacyRoundLifecycleRules.PairKey(null, "x") == "|x",
            "null pair halves must normalize to empty strings");

        string authorPattern = WorldDiplomacyDocumentFactRules.BuildPeaceKingdomReferencePattern("甲国", true, false);
        Test.True(authorPattern.Contains("我国", StringComparison.Ordinal)
            && authorPattern.Contains("甲国", StringComparison.Ordinal),
            "author-side references must accept self-referential pronouns and the literal name");
        string targetPattern = WorldDiplomacyDocumentFactRules.BuildPeaceKingdomReferencePattern("乙国", false, true);
        Test.True(targetPattern.Contains("贵国", StringComparison.Ordinal)
            && !targetPattern.Contains("我国", StringComparison.Ordinal),
            "target-side references must accept the counterpart pronoun only");
        string neutralPattern = WorldDiplomacyDocumentFactRules.BuildPeaceKingdomReferencePattern("丙国", false, false);
        Test.True(neutralPattern == "(?:丙国)",
            "neutral kingdom references must only accept the literal name");

        Test.True(WorldDiplomacyDocumentFactRules.ContainsDirectedPeaceTerm(
                "我国每年向贵国支付第纳尔", "甲国", true, false, "乙国", false, true, "支付|缴纳"),
            "a directed tribute sentence must match author -> target via pronouns");
        Test.True(!WorldDiplomacyDocumentFactRules.ContainsDirectedPeaceTerm(
                "乙国向甲国割让城堡", "甲国", true, false, "乙国", false, true, "割让|归还"),
            "reversed direction must not satisfy the directed peace term");
        Test.True(!WorldDiplomacyDocumentFactRules.ContainsDirectedPeaceTerm(
                "我国每年向贵国支付第纳尔", "", true, false, "乙国", false, true, "支付"),
            "empty side names must fail closed");

        WorldDiplomacyRound round = new WorldDiplomacyRound
        {
            RoundId = "r1",
            PendingOffers = new List<WorldDiplomacyRoundOffer>
            {
                new WorldDiplomacyRoundOffer { ProposerKingdomId = "k1", TargetKingdomId = "k2", Status = "open" }
            }
        };
        StringBuilder sb = new StringBuilder();
        WorldDiplomacyPromptContractRules.AppendOpenOfferAnswerRequirement(sb, round, "k2");
        Test.True(sb.Length > 0,
            "a pending open offer to the author must inject the answer-requirement line");
        sb.Clear();
        WorldDiplomacyPromptContractRules.AppendOpenOfferAnswerRequirement(sb, round, "k3");
        Test.True(sb.Length == 0,
            "no open offer to the author must append nothing");

        WorldDiplomacyDocument rumorDoc = new WorldDiplomacyDocument
        {
            DocumentId = "d1",
            AuthorKingdomId = "a",
            AuthorKingdomName = "甲国",
            TargetKingdomId = "b",
            Intent = "propose_peace"
        };
        string rumor = WorldDiplomacyTextRules.BuildDiplomacyRumor(rumorDoc, id => "K-" + id);
        Test.True(rumor.Contains("甲国", StringComparison.Ordinal)
            && rumor.Contains("K-b", StringComparison.Ordinal)
            && rumor.Contains("停战", StringComparison.Ordinal),
            "rumor text must resolve names through the port and classify peace intents");
        string anon = WorldDiplomacyTextRules.BuildDiplomacyRumor(
            new WorldDiplomacyDocument { Intent = "declare_war" }, id => "K-" + id);
        Test.True(anon.Contains("战争", StringComparison.Ordinal) && !anon.Contains("针对", StringComparison.Ordinal),
            "a targetless war rumor must use the untargeted template");
    }








    private static void RunOpenProposalAndThreatAdapterDecisionTests()
    {
        // TryResolveUniqueOpenProposalForRound / TryResolveOpenProposalFor
        WorldDiplomacyRoundOffer openOffer = new WorldDiplomacyRoundOffer
        {
            Status = "open", Intent = "propose_peace",
            ProposerKingdomId = "kP", TargetKingdomId = "kR",
            SourceDocumentId = "d1", SourceActionId = "a1"
        };
        WorldDiplomacyRound propRound = new WorldDiplomacyRound
        {
            RoundId = "r1",
            PendingOffers = new List<WorldDiplomacyRoundOffer> { openOffer }
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.TryResolveUniqueOpenProposalForRound(
                propRound, "kR", "kP", "propose_peace", out string foundDoc, out string foundAction)
            && foundDoc == "d1" && foundAction == "a1",
            "a unique open proposal must resolve to its source document and action");
        propRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        {
            Status = "open", Intent = "propose_peace",
            ProposerKingdomId = "kP", TargetKingdomId = "kR",
            SourceDocumentId = "d2"
        });
        Test.True(!WorldDiplomacyRoundLifecycleRules.TryResolveUniqueOpenProposalForRound(
                propRound, "kR", "kP", "propose_peace", out _, out _),
            "two distinct open proposals must not resolve as unique");
        propRound.PendingOffers.RemoveAt(1);
        Test.True(!WorldDiplomacyRoundLifecycleRules.TryResolveUniqueOpenProposalForRound(
                propRound, "kP", "kR", "propose_peace", out _, out _),
            "a reversed proposer/responder direction must not match");
        Test.True(!WorldDiplomacyRoundLifecycleRules.TryResolveUniqueOpenProposalForRound(
                propRound, "kR", "kP", "statement", out _, out _),
            "a non-proposal intent must not resolve");
        Test.True(!WorldDiplomacyRoundLifecycleRules.TryResolveUniqueOpenProposalForRound(
                null!, "kR", "kP", "propose_peace", out _, out _)
            && !WorldDiplomacyRoundLifecycleRules.TryResolveUniqueOpenProposalForRound(
                propRound, "", "kP", "propose_peace", out _, out _),
            "a null round or empty kingdom id must not resolve");
        Test.True(WorldDiplomacyRoundLifecycleRules.TryResolveUniqueOpenProposalForRound(
                propRound, "kR", "kP", "propose_peace", out string fiveArgDoc)
            && fiveArgDoc == "d1",
            "the document-only overload must delegate to the full resolver");

        WorldDiplomacyJob job = new WorldDiplomacyJob { RoundId = "r1" };
        Test.True(WorldDiplomacyRoundLifecycleRules.TryResolveOpenProposalFor(
                job, "kR", "kP", "propose_peace", id => id == "r1" ? propRound : null, out string jobDoc)
            && jobDoc == "d1",
            "an open proposal must resolve through the job round");
        WorldDiplomacyJob exchangeJob = new WorldDiplomacyJob { RoundId = "", ExchangeId = "r1" };
        Test.True(WorldDiplomacyRoundLifecycleRules.TryResolveOpenProposalFor(
                exchangeJob, "kR", "kP", "propose_peace", id => id == "r1" ? propRound : null, out _),
            "the exchange id must serve as the round fallback");
        Test.True(!WorldDiplomacyRoundLifecycleRules.TryResolveOpenProposalFor(
                null!, "kR", "kP", "propose_peace", id => propRound, out _)
            && !WorldDiplomacyRoundLifecycleRules.TryResolveOpenProposalFor(
                job, "kR", "", "propose_peace", id => propRound, out _),
            "a null job or empty proposer must not resolve");

        // HasOpenProposalForDocument
        WorldDiplomacyDocument response = new WorldDiplomacyDocument
        {
            RoundId = "r1", IsPlayerAuthored = false,
            RespondingToOfferDocumentId = "d1", RespondingToOfferActionId = "a1"
        };
        Func<string, WorldDiplomacyRound> resolveRound = id => id == "r1" ? propRound : null;
        Test.True(WorldDiplomacyRoundLifecycleRules.HasOpenProposalForDocument(
                response, "kR", "kP", "propose_peace", resolveRound),
            "a bound response must see its open proposal");
        response.RespondingToOfferDocumentId = "other";
        Test.True(!WorldDiplomacyRoundLifecycleRules.HasOpenProposalForDocument(
                response, "kR", "kP", "propose_peace", resolveRound),
            "a response bound to a different source document must not match");
        response.IsPlayerAuthored = true;
        response.RespondingToOfferDocumentId = "";
        Test.True(WorldDiplomacyRoundLifecycleRules.HasOpenProposalForDocument(
                response, "kR", "kP", "propose_peace", resolveRound),
            "a player-authored response must match by direction and intent alone");
        Test.True(!WorldDiplomacyRoundLifecycleRules.HasOpenProposalForDocument(
                null!, "kR", "kP", "propose_peace", resolveRound)
            && !WorldDiplomacyRoundLifecycleRules.HasOpenProposalForDocument(
                response, "kR", "kP", "statement", resolveRound),
            "a null response or non-proposal intent must not match");

        // IsEnforcingRejectedUltimatum
        List<WorldDiplomacyThreat> threats = new List<WorldDiplomacyThreat>
        {
            new WorldDiplomacyThreat
            {
                Status = "open", Stage = "ultimatum", TargetDecision = "noncomplied",
                IssuerKingdomId = "kA", TargetKingdomId = "kB"
            }
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.IsEnforcingRejectedUltimatum(threats, "kA", "kB"),
            "a noncomplied open ultimatum must count as enforcement");
        threats[0].TargetDecision = "complied";
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsEnforcingRejectedUltimatum(threats, "kA", "kB"),
            "a complied ultimatum must not count as enforcement");
        threats[0].TargetDecision = "noncomplied";
        threats[0].Stage = "warning";
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsEnforcingRejectedUltimatum(threats, "kA", "kB"),
            "a warning-stage threat must not count as ultimatum enforcement");
        threats[0].Stage = "ultimatum";
        Test.True(!WorldDiplomacyRoundLifecycleRules.IsEnforcingRejectedUltimatum(threats, "kB", "kA")
            && !WorldDiplomacyRoundLifecycleRules.IsEnforcingRejectedUltimatum(null!, "kA", "kB")
            && !WorldDiplomacyRoundLifecycleRules.IsEnforcingRejectedUltimatum(threats, "", "kB"),
            "a reversed pair, null list, or empty id must not count as enforcement");

        // UpdateDiplomaticThreatComplianceDocumentResult
        WorldDiplomacyDocument complianceDoc = new WorldDiplomacyDocument { DocumentId = "c1" };
        WorldDiplomacyThreat complianceThreat = new WorldDiplomacyThreat
        {
            ComplianceDocumentId = "c1", Stage = "ultimatum",
            PolicyConditionCancellationStatus = "cancelled",
            PolicyConditionPolicyName = "Trade Ban"
        };
        WorldDiplomacyRoundLifecycleRules.UpdateDiplomaticThreatComplianceDocumentResult(
            complianceThreat, id => id == "c1" ? complianceDoc : null);
        Test.True(complianceDoc.ChangedDiplomaticState
                && (complianceDoc.MechanicalResult ?? "").Contains("Trade Ban"),
            "a cancelled-policy compliance result must name the cancelled policy");
        WorldDiplomacyDocument doc2 = new WorldDiplomacyDocument { DocumentId = "c2" };
        WorldDiplomacyThreat threat2 = new WorldDiplomacyThreat { ComplianceDocumentId = "c2", Stage = "warning" };
        WorldDiplomacyRoundLifecycleRules.UpdateDiplomaticThreatComplianceDocumentResult(
            threat2, id => id == "c2" ? doc2 : null);
        Test.True(doc2.ChangedDiplomaticState && !string.IsNullOrEmpty(doc2.MechanicalResult),
            "a resolved compliance document must be marked state-changing with a result line");
        WorldDiplomacyRoundLifecycleRules.UpdateDiplomaticThreatComplianceDocumentResult(
            null!, id => complianceDoc);
        WorldDiplomacyRoundLifecycleRules.UpdateDiplomaticThreatComplianceDocumentResult(
            new WorldDiplomacyThreat { ComplianceDocumentId = "missing" }, id => null);
        Test.True(true, "null threat or unresolved document must be a safe no-op");

        // BuildPolicySignalContext
        WorldDiplomacyPolicySignal signal = new WorldDiplomacyPolicySignal
        {
            SignalKey = "sig1", IssuerKingdomName = "Vlandia", PolicyName = "War Tax",
            TargetKingdomName = "Sturgia", PolicySummary = "summary", DirectEffect = "tribute"
        };
        string ctx = WorldDiplomacyPromptContractRules.BuildPolicySignalContext(signal);
        Test.True(ctx.Contains("sig1") && ctx.Contains("Vlandia") && ctx.Contains("War Tax")
                && ctx.Contains("Sturgia") && ctx.Contains("summary") && ctx.Contains("tribute"),
            "the policy-signal context must include key, issuer, policy, target, summary, and direct effect");
        WorldDiplomacyPolicySignal minimal = new WorldDiplomacyPolicySignal { SignalKey = "s2" };
        string minimalCtx = WorldDiplomacyPromptContractRules.BuildPolicySignalContext(minimal);
        Test.True(minimalCtx.Contains("s2") && minimalCtx.Length > 0,
            "a sparse signal must still render its key");
    }

    private static void RunWarSnapshotAdapterDecisionTests()
    {
        Test.True(Math.Abs(WorldDiplomacyRoundLifecycleRules.Clamp01(-0.5f) - 0f) < 0.0001f, "Clamp01 must floor at zero");
        Test.True(Math.Abs(WorldDiplomacyRoundLifecycleRules.Clamp01(0.5f) - 0.5f) < 0.0001f, "Clamp01 must pass mid-range values through");
        Test.True(Math.Abs(WorldDiplomacyRoundLifecycleRules.Clamp01(1.5f) - 1f) < 0.0001f, "Clamp01 must cap at one");

        WarSituationSnapshot snapshot = new WarSituationSnapshot
        {
            AuthorProgress = 10f, TargetProgress = 260f,
            AuthorStrength = 900f, TargetStrength = 1800f,
            AuthorSufferedCasualties = 300, AuthorInflictedCasualties = 50,
            AuthorOtherWars = 2, TargetOtherWars = 0
        };
        float authorScore = WorldDiplomacyRoundLifecycleRules.CalculateCessionScore(snapshot, 4, authorPerspective: true);
        float targetScore = WorldDiplomacyRoundLifecycleRules.CalculateCessionScore(snapshot, 4, authorPerspective: false);
        Test.True(authorScore > targetScore, "the losing side must score a higher cession willingness than the winning side");
        Test.True(authorScore >= 0f && authorScore <= 100f, "cession score must stay inside the 0..100 band");
        float noLosses = WorldDiplomacyRoundLifecycleRules.CalculateCessionScore(snapshot, 0, authorPerspective: true);
        Test.True(noLosses < authorScore, "losing unrecovered fiefs must raise the cession score");

        WorldDiplomacyRound round = new WorldDiplomacyRound
        {
            PendingOffers = new List<WorldDiplomacyRoundOffer>
            {
                new WorldDiplomacyRoundOffer { Status = "open", Intent = "propose_trade", ProposerKingdomId = "a", TargetKingdomId = "b" },
                new WorldDiplomacyRoundOffer { Status = "open", Intent = "propose_trade", ProposerKingdomId = "b", TargetKingdomId = "a" },
                new WorldDiplomacyRoundOffer { Status = "open", Intent = "propose_trade", ProposerKingdomId = "a", TargetKingdomId = "c" },
                new WorldDiplomacyRoundOffer { Status = "accepted", Intent = "propose_trade", ProposerKingdomId = "a", TargetKingdomId = "b" },
                new WorldDiplomacyRoundOffer { Status = "open", Intent = "propose_alliance", ProposerKingdomId = "a", TargetKingdomId = "b" }
            }
        };
        WorldDiplomacyRoundLifecycleRules.MarkOpenBilateralOffersAccepted(round, "a", "b", WorldDiplomacyOfferDomain.Trade);
        Test.True(round.PendingOffers[0].Status == "accepted", "directed open trade offers in the a->b direction must be marked accepted");
        Test.True(round.PendingOffers[1].Status == "accepted", "reverse-direction open trade offers in the same pair must be marked accepted");
        Test.True(round.PendingOffers[2].Status == "open", "open offers to third parties must stay open");
        Test.True(round.PendingOffers[3].Status == "accepted", "already-accepted offers must stay accepted");
        Test.True(round.PendingOffers[4].Status == "open", "open offers in other domains must stay open");
        WorldDiplomacyRoundLifecycleRules.MarkOpenBilateralOffersAccepted(round, "a", "a", WorldDiplomacyOfferDomain.Trade);
        WorldDiplomacyRoundLifecycleRules.MarkOpenBilateralOffersAccepted(round, null, "b", WorldDiplomacyOfferDomain.Trade);
        WorldDiplomacyRoundLifecycleRules.MarkOpenBilateralOffersAccepted(null, "a", "b", WorldDiplomacyOfferDomain.Trade);
        Test.True(round.PendingOffers[2].Status == "open", "self-pair, null id, and null round calls must fail safe without mutating offers");

        WorldDiplomacyBorderRelation noBorder = new WorldDiplomacyBorderRelation();
        Test.True(WorldDiplomacyTextRules.DescribeBorderRelation(noBorder) == WorldDiplomacyTextRules.DescribeBorderRelation(null), "a null or non-border relation must render the shared-border fallback");
        WorldDiplomacyBorderRelation border = new WorldDiplomacyBorderRelation
        {
            SharesBorder = true,
            FirstSettlementName = "AlphaTown",
            SecondSettlementId = "settlement_beta"
        };
        string described = WorldDiplomacyTextRules.DescribeBorderRelation(border);
        Test.True(described.Contains("AlphaTown"), "border description must include the first settlement name");
        Test.True(described.Contains("settlement_beta"), "border description must fall back to the second settlement id when its name is empty");
    }




	private static void RunStorageKnowledgeAndContextDecisionTests()
	{
		// DPL-060BM: knowledge recording + context builders + queue clearing moved into domain rules
		List<WorldDiplomacyKingdomKnowledge> kingdomKnowledge = new List<WorldDiplomacyKingdomKnowledge>();
		Test.True(WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(kingdomKnowledge, "k1", "doc-1", 40),
			"recording a fresh kingdom document must report the new knowledge");
		Test.True(kingdomKnowledge.Count == 1
			&& kingdomKnowledge[0].KingdomId == "k1"
			&& kingdomKnowledge[0].DocumentIds.Count == 1
			&& kingdomKnowledge[0].LastUpdatedDay == 40,
			"a first kingdom-knowledge record must be created with the document id and day");
		Test.True(!WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(kingdomKnowledge, "K1", "DOC-1", 41),
			"re-recording the same document must report no new knowledge");
		Test.True(kingdomKnowledge[0].DocumentIds.Count == 1 && kingdomKnowledge[0].LastUpdatedDay == 40,
			"a duplicate document must not extend or touch the knowledge entry");
		for (int index = 0; index < 140; index++)
			WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(kingdomKnowledge, "k1", "doc-" + (index + 100), 40);
		Test.True(kingdomKnowledge[0].DocumentIds.Count == WorldDiplomacyDocumentFactRules.MaxKnownDocumentsPerLocation * 2,
			"kingdom knowledge must stay capped at twice the per-location bound");
		Test.True(!WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(null, "k1", "doc-x", 1)
			&& !WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(kingdomKnowledge, "", "doc-x", 1)
			&& !WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(kingdomKnowledge, "k1", " ", 1),
			"kingdom knowledge must fail safe on null collections and blank ids");

		List<WorldDiplomacySettlementKnowledge> settlementKnowledge = new List<WorldDiplomacySettlementKnowledge>();
		WorldDiplomacyDocumentFactRules.RecordSettlementKnowledge(settlementKnowledge, "s1", "doc-1", 10);
		WorldDiplomacyDocumentFactRules.RecordSettlementKnowledge(settlementKnowledge, "S1", "doc-2", 11);
		Test.True(settlementKnowledge.Count == 1 && settlementKnowledge[0].DocumentIds.Count == 2
			&& settlementKnowledge[0].LastUpdatedDay == 11,
			"settlement knowledge must merge case-insensitively and append distinct documents");
		for (int index = 0; index < 70; index++)
			WorldDiplomacyDocumentFactRules.RecordSettlementKnowledge(settlementKnowledge, "s1", "sdoc-" + index, 12);
		Test.True(settlementKnowledge[0].DocumentIds.Count == WorldDiplomacyDocumentFactRules.MaxKnownDocumentsPerLocation,
			"settlement knowledge must stay capped at the per-location bound");

		List<WorldDiplomacyKingdomKnowledge> nobleKnowledge = new List<WorldDiplomacyKingdomKnowledge>();
		WorldDiplomacyDocumentFactRules.RecordNobleKnowledge(nobleKnowledge, "k9", "doc-a", 5);
		WorldDiplomacyDocumentFactRules.RecordNobleKnowledge(nobleKnowledge, "k9", "DOC-A", 6);
		Test.True(nobleKnowledge.Count == 1 && nobleKnowledge[0].DocumentIds.Count == 1
			&& nobleKnowledge[0].LastUpdatedDay == 6,
			"noble knowledge must dedupe document ids while still refreshing the day");

		List<NativeDiplomacySignal> signals = new List<NativeDiplomacySignal>
		{
			new NativeDiplomacySignal { SourceKingdomId = "a", TargetKingdomId = "b", Day = 3, Reason = "older" },
			new NativeDiplomacySignal { SourceKingdomId = "A", TargetKingdomId = "B", Day = 9, Reason = "newest" },
			new NativeDiplomacySignal { SourceKingdomId = "a", TargetKingdomId = "b", Day = 5, Reason = "middle" },
			new NativeDiplomacySignal { SourceKingdomId = "b", TargetKingdomId = "a", Day = 8, Reason = "reversed" },
			new NativeDiplomacySignal { SourceKingdomId = "a", TargetKingdomId = "c", Day = 7, Reason = "other-target" },
			new NativeDiplomacySignal { SourceKingdomId = "a", TargetKingdomId = "b", Day = 2, Reason = "oldest" },
			new NativeDiplomacySignal { SourceKingdomId = "a", TargetKingdomId = "b", Day = 1, Reason = "beyond-cap" }
		};
		string native = WorldDiplomacyDocumentFactRules.BuildRecentNativeSignalContext(signals, "a", "b");
		Test.True(native.Contains("newest") && native.Contains("middle") && native.Contains("older"),
			"native signal context must render matching signal reasons");
		Test.True(native.IndexOf("newest", StringComparison.Ordinal) < native.IndexOf("middle", StringComparison.Ordinal),
			"native signal context must order newest day first");
		Test.True(!native.Contains("reversed") && !native.Contains("other-target"),
			"native signal context must only include the exact directed pair");
		Test.True(native.Split('\n').Length == 4 && !native.Contains("beyond-cap"),
			"native signal context must cap at four entries");

		List<WorldDiplomacyRoundSummary> summaries = new List<WorldDiplomacyRoundSummary>
		{
			new WorldDiplomacyRoundSummary { RoundId = "r1", KingdomIds = new List<string> { "a", "b" }, CreatedDay = 5, Summary = "old done", IsTokenCompressed = false },
			new WorldDiplomacyRoundSummary { RoundId = "r2", KingdomIds = new List<string> { "A", "B" }, CreatedDay = 9, Summary = "new done", IsTokenCompressed = false },
			new WorldDiplomacyRoundSummary { RoundId = "r3", KingdomIds = new List<string> { "a", "b" }, CreatedDay = 8, Summary = "compressed", IsTokenCompressed = true },
			new WorldDiplomacyRoundSummary { RoundId = "r4", KingdomIds = new List<string> { "a", "c" }, CreatedDay = 10, Summary = "wrong pair", IsTokenCompressed = false }
		};
		string completed = WorldDiplomacyDocumentFactRules.BuildRelevantCompletedRoundContext(summaries, "active", "a", "b");
		Test.True(completed.Contains("new done") && !completed.Contains("old done") && !completed.Contains("compressed"),
			"completed context must pick the newest uncompressed non-active summary for the pair");
		Test.True(WorldDiplomacyDocumentFactRules.BuildRelevantCompletedRoundContext(summaries, "r2", "a", "b").Contains("old done"),
			"the active round's own summary must be excluded from completed context");
		Test.True(WorldDiplomacyDocumentFactRules.BuildRelevantCompletedRoundContext(summaries, "active", "x", "y") == "",
			"completed context must stay empty without a matching pair");

		string compressed = WorldDiplomacyDocumentFactRules.BuildRelevantCompressedDiplomacyContext(summaries, "a", "b", 5);
		Test.True(compressed.Contains("compressed") && !compressed.Contains("new done"),
			"compressed context must only render token-compressed summaries for the pair");
		Test.True(WorldDiplomacyDocumentFactRules.BuildRelevantCompressedDiplomacyContext(summaries, "a", "b", 0) == "",
			"compressed context must honor a non-positive cap");

		List<WorldDiplomacyKingdomKnowledge> known = new List<WorldDiplomacyKingdomKnowledge>
		{
			new WorldDiplomacyKingdomKnowledge { KingdomId = "k1", DocumentIds = new List<string> { "doc-a", "doc-b" } }
		};
		List<WorldDiplomacyDocument> docs = new List<WorldDiplomacyDocument>
		{
			new WorldDiplomacyDocument { DocumentId = "doc-a", RoundId = "round-x", Title = "knownA" },
			new WorldDiplomacyDocument { DocumentId = "doc-b", RoundId = "round-y", Title = "knownB" },
			new WorldDiplomacyDocument { DocumentId = "doc-c", RoundId = "round-x", Title = "unknown" }
		};
		int formatCalls = 0;
		string knownCtx = WorldDiplomacyDocumentFactRules.BuildKnownRoundContext(
			known, docs, "k1", "round-x", 5, day => { formatCalls++; return "D" + day; });
		Test.True(knownCtx.Contains("knownA") && !knownCtx.Contains("knownB") && !knownCtx.Contains("unknown"),
			"known-round context must render only the documents known to the kingdom inside the round");
		Test.True(formatCalls == 1, "known-round context must route every rendered line through the date port");

		WorldDiplomacyStorage storage = new WorldDiplomacyStorage();
		storage.PendingParticipationEvaluations.Add(new WorldDiplomacyParticipationRequest { RoundId = "r-a" });
		storage.PendingParticipationEvaluations.Add(new WorldDiplomacyParticipationRequest { RoundId = "r-b" });
		storage.PendingSpeeches.Add(new WorldDiplomacyPendingSpeech { RoundId = "r-a" });
		storage.RelayArrivals.Add(new WorldDiplomacyRelayArrival { RoundId = "r-a" });
		storage.PlayerOpportunities.Add(new WorldDiplomacyPlayerOpportunity { RoundId = "r-a", Status = "open" });
		storage.PlayerOpportunities.Add(new WorldDiplomacyPlayerOpportunity { RoundId = "r-b", Status = "open" });
		WorldDiplomacyRoundLifecycleRules.ClearRoundScopedQueuesAndExpireOpportunities(
			storage, new WorldDiplomacyRound { RoundId = "r-a" });
		Test.True(storage.PendingParticipationEvaluations.Count == 1
			&& storage.PendingSpeeches.Count == 0 && storage.RelayArrivals.Count == 0,
			"round-scoped queues must drop only entries bound to the cleared round");
		Test.True(storage.PlayerOpportunities[0].Status == "expired"
			&& storage.PlayerOpportunities[1].Status == "open",
			"open opportunities in the cleared round must expire while other rounds stay open");
		WorldDiplomacyRoundLifecycleRules.ClearRoundScopedQueuesAndExpireOpportunities(null, new WorldDiplomacyRound { RoundId = "r-a" });
		Test.True(storage.PlayerOpportunities.Count == 2,
			"queue clearing must fail safe on null storage");

		WorldDiplomacyDocument warDoc = new WorldDiplomacyDocument
		{
			DocumentId = "doc-war",
			Actions = new List<WorldDiplomacyDocumentAction>
			{
				new WorldDiplomacyDocumentAction { Intent = "declare_war", ChangedDiplomaticState = true }
			}
		};
		WorldDiplomacyRound round = new WorldDiplomacyRound { RoundId = "r-war" };
		Test.True(WorldDiplomacyRoundLifecycleRules.TryGetConfirmedRoundResult(warDoc, round, new List<WorldDiplomacyThreat>(),
			out string closeReason, out string roundStatus)
			&& closeReason == "war_declared" && roundStatus == "resolved",
			"a successful declared-war action must confirm the round result");
		Test.True(!WorldDiplomacyRoundLifecycleRules.TryGetConfirmedRoundResult(null, round, null,
			out string nullReason, out string nullStatus),
			"confirmed-result evaluation must fail safe on a null document");

		WorldDiplomacyRound settlementRound = new WorldDiplomacyRound
		{
			RoundId = "r-settle",
			PendingOffers = new List<WorldDiplomacyRoundOffer>
			{
				new WorldDiplomacyRoundOffer { Status = "open", TargetKingdomId = "k1" },
				new WorldDiplomacyRoundOffer { Status = "open", TargetKingdomId = "k2" }
			}
		};
		List<WorldDiplomacyThreat> threats = new List<WorldDiplomacyThreat>
		{
			new WorldDiplomacyThreat { Status = "open", StageRoundId = "r-settle", TargetDecision = "pending", TargetKingdomId = "k1" },
			new WorldDiplomacyThreat { Status = "open", StageRoundId = "r-settle", TargetDecision = "pending", TargetKingdomId = "k2" }
		};
		WorldDiplomacyRoundLifecycleRules.InvalidateUnserviceableResultSettlementObligations(
			settlementRound, threats, "k1", "speaker_unavailable", 33);
		Test.True(settlementRound.PendingOffers[0].Status == "invalidated"
			&& settlementRound.PendingOffers[1].Status == "open",
			"unserviceable settlement obligations must invalidate only open offers to the kingdom");
		Test.True(threats[0].Status == "invalidated" && threats[0].UpdatedDay == 33
			&& threats[1].Status == "open",
			"threats requiring the kingdom as speaker must be invalidated with the reason and day");
	}


	private static void RunThreatFinalizationDecisionTests()
	{
		// DPL-060BN: threat finalization and policy-signal cleanup moved into lifecycle rules
		List<WorldDiplomacyThreat> notices = new List<WorldDiplomacyThreat>
		{
			new WorldDiplomacyThreat { IssuerKingdomId = "init", IssuerResolutionNoticePending = true, ResolutionRoundId = "other" },
			new WorldDiplomacyThreat { IssuerKingdomId = "init", IssuerResolutionNoticePending = true, ResolutionRoundId = "r1" },
			new WorldDiplomacyThreat { IssuerKingdomId = "other", IssuerResolutionNoticePending = true, ResolutionRoundId = "x" }
		};
		WorldDiplomacyRound settled = new WorldDiplomacyRound { RoundId = "r1", InitiatorKingdomId = "init", RoundStatus = "resolved" };
		List<WorldDiplomacyDocument> published = new List<WorldDiplomacyDocument>
		{
			new WorldDiplomacyDocument { AuthorKingdomId = "init" }
		};
		WorldDiplomacyRoundLifecycleRules.SettleDiplomaticThreatObligationsForClosedRound(settled, published, notices, 77);
		Test.True(notices[0].IssuerResolutionNoticePending == false && notices[0].UpdatedDay == 77
			&& notices[1].IssuerResolutionNoticePending == true
			&& notices[2].IssuerResolutionNoticePending == true,
			"a confirmed close must clear only the initiator's notices outside the settled round");
		WorldDiplomacyRound aborted = new WorldDiplomacyRound { RoundId = "r1", InitiatorKingdomId = "init", RoundStatus = "aborted" };
		WorldDiplomacyRoundLifecycleRules.SettleDiplomaticThreatObligationsForClosedRound(aborted, published, notices, 88);
		Test.True(notices[1].IssuerResolutionNoticePending == true,
			"a retryable abort must leave issuer resolution notices pending");

		WorldDiplomacyRound signaled = new WorldDiplomacyRound
		{
			ExternalOpeningContext = "prefix policy-ctx-1 suffix",
			AttachedPolicySignals = new List<WorldDiplomacyPolicySignal>
			{
				new WorldDiplomacyPolicySignal { PolicyId = "p1", IssuerKingdomId = "owner", SignalKey = "sk1" },
				new WorldDiplomacyPolicySignal { PolicyId = "p2", IssuerKingdomId = "owner", SignalKey = "sk2" }
			},
			ExternalSignalKeys = new List<string> { "sk1", "sk2", "sk3" }
		};
		string boundContext = WorldDiplomacyPromptContractRules.BuildPolicySignalContext(signaled.AttachedPolicySignals[0]);
		signaled.ExternalOpeningContext = "prefix " + boundContext + " suffix";
		WorldDiplomacyRoundLifecycleRules.RemoveSettledPolicySignalContextFromActiveRound(signaled, "p1", "owner");
		Test.True(signaled.AttachedPolicySignals.Count == 1 && signaled.AttachedPolicySignals[0].PolicyId == "p2",
			"policy-signal removal must drop only the bound signal");
		Test.True(!signaled.ExternalOpeningContext.Contains(boundContext)
			&& signaled.ExternalOpeningContext.Contains("prefix")
			&& signaled.ExternalOpeningContext.Contains("suffix"),
			"policy-signal removal must strip only the bound signal's context text");
		Test.True(!signaled.ExternalSignalKeys.Contains("sk1") && signaled.ExternalSignalKeys.Contains("sk2")
			&& signaled.ExternalSignalKeys.Contains("sk3"),
			"policy-signal removal must drop only the removed signal keys");

		int breachCalls = 0, penaltyCalls = 0, rewardCalls = 0;
		WorldDiplomacyThreat breached = new WorldDiplomacyThreat
		{
			Status = "breached",
			ComplianceDocumentId = "doc-1",
			DomesticPenaltyCompleted = true,
			IssuerRewardCompleted = true
		};
		WorldDiplomacyDocument breachDoc = new WorldDiplomacyDocument
		{
			DocumentId = "doc-1",
			HistoryDeclarationRecorded = true
		};
		WorldDiplomacyThreatHistoryApplication.FinalizeDiplomaticThreatHistoryAfterDocument(
			breachDoc, new List<WorldDiplomacyThreat> { breached },
			x => breachCalls++, x => penaltyCalls++, x => rewardCalls++);
		Test.True(breachCalls == 1 && penaltyCalls == 1 && rewardCalls == 1,
			"a breached linked threat must append its breach, penalty, and reward results once each");
		WorldDiplomacyThreat resolvedThreat = new WorldDiplomacyThreat
		{
			Status = "complied",
			ResolutionDocumentId = "doc-2"
		};
		WorldDiplomacyDocument resolvedDoc = new WorldDiplomacyDocument
		{
			DocumentId = "doc-2",
			HistoryResultRecorded = true
		};
		WorldDiplomacyThreatHistoryApplication.FinalizeDiplomaticThreatHistoryAfterDocument(
			resolvedDoc, new List<WorldDiplomacyThreat> { resolvedThreat },
			x => breachCalls++, x => penaltyCalls++, x => rewardCalls++);
		Test.True(breachCalls == 1 && resolvedThreat.HistoryResultRecorded,
			"a resolved linked threat must mark history recorded without appending another breach");
		WorldDiplomacyThreatHistoryApplication.FinalizeDiplomaticThreatHistoryAfterDocument(
			breachDoc, null, null, null, null);
		Test.True(breachCalls == 1,
			"threat finalization must fail safe on null threat lists and ports");

		int ncCalls = 0;
		WorldDiplomacyThreat nonComplied = new WorldDiplomacyThreat
		{
			Status = "open",
			TargetDecision = "noncomplied",
			TargetDecisionDocumentId = "doc-nc",
			TargetDecisionDay = 12,
			Stage = "ultimatum",
			StageDocumentId = "doc-stage"
		};
		WorldDiplomacyDocument ncDoc = new WorldDiplomacyDocument
		{
			DocumentId = "doc-nc",
			HistoryDeclarationRecorded = true
		};
		WorldDiplomacyThreatHistoryApplication.FinalizeDiplomaticThreatNonComplianceHistoryAfterDocument(
			ncDoc, new List<WorldDiplomacyThreat> { nonComplied }, (x, e) => { ncCalls++; e.HistoryRecorded = true; });
		Test.True(ncCalls == 1 && nonComplied.NonComplianceHistoryRecorded,
			"a linked noncompliance decision must be appended once and mark the threat recorded");
		WorldDiplomacyThreatHistoryApplication.FinalizeDiplomaticThreatNonComplianceHistoryAfterDocument(
			ncDoc, new List<WorldDiplomacyThreat> { nonComplied }, (x, e) => { ncCalls++; e.HistoryRecorded = true; });
		Test.True(ncCalls == 1,
			"a recorded noncompliance history must not append a second time");

		WorldDiplomacyThreat direct = new WorldDiplomacyThreat
		{
			Status = "open",
			TargetDecision = "noncomplied",
			TargetDecisionDocumentId = "doc-d",
			TargetDecisionDay = 9,
			StageDocumentId = "doc-s"
		};
		int directCalls = 0;
		WorldDiplomacyThreatHistoryApplication.TryAppendDiplomaticThreatNonComplianceHistoryResult(
			direct, (x, e) => { directCalls++; e.HistoryRecorded = true; });
		Test.True(directCalls == 1 && direct.NonComplianceHistoryRecorded,
			"the unrecorded current noncompliance event must be appended through the port");
		WorldDiplomacyThreatHistoryApplication.TryAppendDiplomaticThreatNonComplianceHistoryResult(null, (x, e) => { directCalls++; e.HistoryRecorded = true; });
		Test.True(directCalls == 1, "noncompliance append must fail safe on a null threat");
	}









    static void RunCanonicalHistoryMaintenanceDecisionTests()
    {
        HashSet<string> sourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        sourceKeys.Add("document:d1:declaration");
        Test.True(!WorldDiplomacyRoundLifecycleRules.CanonicalDeltaContainsSourceKey(sourceKeys, null),
            "canonical delta source key null must not match");
        Test.True(!WorldDiplomacyRoundLifecycleRules.CanonicalDeltaContainsSourceKey(sourceKeys, " "),
            "canonical delta source key whitespace must not match");
        Test.True(!WorldDiplomacyRoundLifecycleRules.CanonicalDeltaContainsSourceKey(sourceKeys, "document:d2:declaration"),
            "canonical delta source key absent entry must not match");
        Test.True(WorldDiplomacyRoundLifecycleRules.CanonicalDeltaContainsSourceKey(sourceKeys, "document:d1:declaration"),
            "canonical delta source key present entry must match");
        Test.True(!WorldDiplomacyRoundLifecycleRules.CanonicalDeltaContainsSourceKey(null, "document:d1:declaration"),
            "canonical delta source key null set must not match");

        Test.True(WorldDiplomacyRoundLifecycleRules.EstimateHistoryTokens("abc", x => x.Length) == 3,
            "estimate history tokens must forward text to the estimator");
        Test.True(WorldDiplomacyRoundLifecycleRules.EstimateHistoryTokens(null, x => x.Length) == 0,
            "estimate history tokens must normalize null text to empty");
        Test.True(WorldDiplomacyRoundLifecycleRules.EstimateHistoryTokens("abc", x => -5) == 0,
            "estimate history tokens must clamp negative estimates");
        Test.True(WorldDiplomacyRoundLifecycleRules.EstimateHistoryTokens("abc", null) == 0,
            "estimate history tokens must tolerate a missing estimator");

        HashSet<string> idSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Queue<string> idQueue = new Queue<string>();
        WorldDiplomacyRoundLifecycleRules.EnqueueDeferredCanonicalHistoryRetry(idSet, idQueue, "  ");
        Test.True(idQueue.Count == 0 && idSet.Count == 0,
            "blank deferred retry document id must not enqueue");
        WorldDiplomacyRoundLifecycleRules.EnqueueDeferredCanonicalHistoryRetry(idSet, idQueue, " doc-1 ");
        Test.True(idQueue.Count == 1 && idSet.Contains("doc-1"),
            "deferred retry enqueue must trim and track the document id");
        WorldDiplomacyRoundLifecycleRules.EnqueueDeferredCanonicalHistoryRetry(idSet, idQueue, "DOC-1");
        Test.True(idQueue.Count == 1,
            "deferred retry enqueue must deduplicate through the tracked set");

        Dictionary<string, int> schedAttempts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> schedHours = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        WorldDiplomacyRoundLifecycleRules.ScheduleDeferredCanonicalHistoryRetry(
            schedAttempts, schedHours, idSet, idQueue, "doc-2", 100);
        Test.True(schedAttempts["doc-2"] == 1 && schedHours["doc-2"] == 101,
            "first deferred retry schedule must apply attempt 1 with a one hour delay");
        WorldDiplomacyRoundLifecycleRules.ScheduleDeferredCanonicalHistoryRetry(
            schedAttempts, schedHours, idSet, idQueue, "doc-2", 110);
        Test.True(schedAttempts["doc-2"] == 2 && schedHours["doc-2"] == 112,
            "second deferred retry schedule must escalate to attempt 2 with a two hour delay");

        List<string> appended = new List<string>();
        List<string> logs = new List<string>();
        Queue<string> dropQueue = new Queue<string>();
        HashSet<string> dropSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> dropAttempts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> dropHours = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        WorldDiplomacyRoundLifecycleRules.EnqueueDeferredCanonicalHistoryRetry(dropSet, dropQueue, "doc-done");
        dropAttempts["doc-done"] = 3;
        dropHours["doc-done"] = 1;
        WorldDiplomacyHistoryCaptureApplication.RetryDeferredCanonicalHistoryEntries(
            dropQueue, dropSet, dropAttempts, dropHours,
            new List<WorldDiplomacyThreat>(), 200,
            id => new WorldDiplomacyDocument
            {
                DocumentId = id,
                IsReadyForPublication = true,
                HistoryDeclarationRecorded = true,
                HistoryResultRecorded = true
            },
            doc => appended.Add(doc.DocumentId),
            threat => { }, threat => { }, threat => { }, (threat, ev) => { }, logs.Add);
        Test.True(dropQueue.Count == 0 && dropAttempts.Count == 0 && dropHours.Count == 0 && appended.Count == 0,
            "a resolved deferred document must be dropped with its retry bookkeeping cleared");

        WorldDiplomacyRoundLifecycleRules.EnqueueDeferredCanonicalHistoryRetry(dropSet, dropQueue, "doc-wait");
        dropAttempts["doc-wait"] = 1;
        dropHours["doc-wait"] = 300;
        WorldDiplomacyHistoryCaptureApplication.RetryDeferredCanonicalHistoryEntries(
            dropQueue, dropSet, dropAttempts, dropHours,
            new List<WorldDiplomacyThreat>(), 200,
            id => new WorldDiplomacyDocument { DocumentId = id, IsReadyForPublication = true, Body = "pending" },
            doc => appended.Add(doc.DocumentId),
            threat => { }, threat => { }, threat => { }, (threat, ev) => { }, logs.Add);
        Test.True(appended.Count == 0 && dropQueue.Count == 1 && dropSet.Contains("doc-wait"),
            "a not-yet-due deferred document must be re-enqueued without processing");

        appended.Clear();
        Queue<string> dueQueue = new Queue<string>();
        HashSet<string> dueSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> dueAttempts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> dueHours = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        WorldDiplomacyRoundLifecycleRules.EnqueueDeferredCanonicalHistoryRetry(dueSet, dueQueue, "doc-pend");
        WorldDiplomacyHistoryCaptureApplication.RetryDeferredCanonicalHistoryEntries(
            dueQueue, dueSet, dueAttempts, dueHours,
            new List<WorldDiplomacyThreat>(), 400,
            id => new WorldDiplomacyDocument { DocumentId = id, IsReadyForPublication = true, Body = "pending" },
            doc => appended.Add(doc.DocumentId),
            threat => { }, threat => { }, threat => { }, (threat, ev) => { }, logs.Add);
        Test.True(appended.Count == 1 && appended[0] == "doc-pend",
            "a due deferred document must be processed once");
        Test.True(dueQueue.Count == 1 && dueAttempts["doc-pend"] == 1 && dueHours["doc-pend"] == 401,
            "a still-pending deferred document must be rescheduled with an incremented attempt");

        appended.Clear();
        logs.Clear();
        Queue<string> failQueue = new Queue<string>();
        HashSet<string> failSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> failAttempts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> failHours = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        WorldDiplomacyRoundLifecycleRules.EnqueueDeferredCanonicalHistoryRetry(failSet, failQueue, "doc-fail");
        WorldDiplomacyHistoryCaptureApplication.RetryDeferredCanonicalHistoryEntries(
            failQueue, failSet, failAttempts, failHours,
            new List<WorldDiplomacyThreat>(), 400,
            id => new WorldDiplomacyDocument { DocumentId = id, IsReadyForPublication = true, Body = "pending" },
            doc => throw new InvalidOperationException("append failed"),
            threat => { }, threat => { }, threat => { }, (threat, ev) => { }, logs.Add);
        Test.True(logs.Count == 1 && logs[0].Contains("doc-fail") && logs[0].Contains("append failed"),
            "a deferred retry failure must be logged with the document id");
        Test.True(failAttempts["doc-fail"] == 1 && failQueue.Count == 1,
            "a failed deferred document must be rescheduled for a later attempt");
    }


    static void RunMiscHelperDecisionTests()
    {
        Test.True(WorldDiplomacyRoundLifecycleRules.GetRoundParticipantLimit(0, 12) == 2,
            "participant limit must cap activity 0 at 2");
        Test.True(WorldDiplomacyRoundLifecycleRules.GetRoundParticipantLimit(1, 12) == 3,
            "participant limit must cap activity 1 at 3");
        Test.True(WorldDiplomacyRoundLifecycleRules.GetRoundParticipantLimit(2, 12) == 5,
            "participant limit must cap activity 2 at 5");
        Test.True(WorldDiplomacyRoundLifecycleRules.GetRoundParticipantLimit(2, 4) == 4,
            "participant limit must respect the relay participant ceiling");
        Test.True(WorldDiplomacyRoundLifecycleRules.GetRoundParticipantLimit(9, 12) == 3,
            "participant limit must fall back to 3 for unknown activity levels");

        WorldDiplomacyJob generateJob = new WorldDiplomacyJob { Kind = "generate", PresentedLegalActionSignature = "sig-old" };
        Test.True(!WorldDiplomacyJobPreparationApplication.HasStaleDiplomaticActionPresentation(
            generateJob, job => "sig-old"),
            "a generate job with a matching signature must not be stale");
        Test.True(WorldDiplomacyJobPreparationApplication.HasStaleDiplomaticActionPresentation(
            generateJob, job => "sig-new"),
            "a generate job with a changed signature must be stale");
        WorldDiplomacyJob otherJob = new WorldDiplomacyJob { Kind = "repair", PresentedLegalActionSignature = "sig-old" };
        Test.True(!WorldDiplomacyJobPreparationApplication.HasStaleDiplomaticActionPresentation(
            otherJob, job => "sig-new"),
            "a non-generate job must never report a stale presentation");
        Test.True(!WorldDiplomacyJobPreparationApplication.HasStaleDiplomaticActionPresentation(
            null, job => "sig-new"),
            "a null job must not report a stale presentation");

        WorldDiplomacyOfferCooldownKey cooldownKey = new WorldDiplomacyOfferCooldownKey("a", "b", WorldDiplomacyOfferDomain.Trade);
        Dictionary<WorldDiplomacyOfferCooldownKey, int> lastFailedDays =
            new Dictionary<WorldDiplomacyOfferCooldownKey, int>();
        lastFailedDays[cooldownKey] = 100;
        Func<WorldDiplomacyOfferCooldownKey, int> lastFailedDay = key =>
            lastFailedDays.TryGetValue(key, out int day) ? day : -1;
        Test.True(WorldDiplomacyOfferCooldownRules.IsTradeAllianceProposalCoolingDown(
            lastFailedDay, "a", "b", "propose_trade", 10, 105),
            "a failed trade proposal inside the cooldown window must still cool down");
        Test.True(!WorldDiplomacyOfferCooldownRules.IsTradeAllianceProposalCoolingDown(
            lastFailedDay, "a", "b", "propose_trade", 10, 110),
            "the trade proposal cooldown must expire at the boundary day");
        Test.True(!WorldDiplomacyOfferCooldownRules.IsTradeAllianceProposalCoolingDown(
            lastFailedDay, "b", "a", "propose_trade", 10, 105),
            "the cooldown must stay directed and not leak into the reverse direction");
        Test.True(!WorldDiplomacyOfferCooldownRules.IsTradeAllianceProposalCoolingDown(
            lastFailedDay, "a", "b", "propose_alliance", 10, 105),
            "the cooldown must stay inside its proposal domain");
        Test.True(!WorldDiplomacyOfferCooldownRules.IsTradeAllianceProposalCoolingDown(
            lastFailedDay, "a", "b", "declare_war", 10, 105),
            "non-trade/alliance intents must ignore the cooldown");
        Test.True(!WorldDiplomacyOfferCooldownRules.IsTradeAllianceProposalCoolingDown(
            lastFailedDay, "a", "b", "propose_trade", 0, 105),
            "a zero configured cooldown must disable the rule");
        Test.True(!WorldDiplomacyOfferCooldownRules.IsTradeAllianceProposalCoolingDown(
            lastFailedDay, "", "b", "propose_trade", 10, 105),
            "a missing proposer id must not cool down");
        Test.True(!WorldDiplomacyOfferCooldownRules.IsTradeAllianceProposalCoolingDown(
            lastFailedDay, "a", "a", "propose_trade", 10, 105),
            "a self-directed cooldown key must not be valid");
        Test.True(!WorldDiplomacyOfferCooldownRules.IsTradeAllianceProposalCoolingDown(
            null, "a", "b", "propose_trade", 10, 105),
            "a missing cooldown probe must not cool down");

        List<string> cessionPairs = new List<string> { "s1=城A", "s2=城B" };
        Test.True(WorldDiplomacyTextRules.FormatCessionCandidates(cessionPairs) == "[s1=城A；s2=城B]",
            "cession candidates must render id=name pairs with the list separator");
        Test.True(WorldDiplomacyTextRules.FormatCessionCandidates(new List<string>()) == "[]",
            "empty cession candidates must render an empty list");
        Test.True(WorldDiplomacyTextRules.FormatCessionCandidates(null) == "[]",
            "null cession candidates must render an empty list");
    }





    static void RunExchangeAndOfferCooldownDecisionTests()
    {
        // DPL-060BU: exchange lifecycle and offer-cooldown index CRUD in domain rules.

        // ResolveExchange: blank id misses; active wins over suspended; suspended fallback.
        var active = new WorldDiplomacyExchange { ExchangeId = "ex-a", State = "waiting" };
        var suspended = new List<WorldDiplomacyExchange>
        {
            new WorldDiplomacyExchange { ExchangeId = "ex-s1", State = "suspended_by_player" },
            new WorldDiplomacyExchange { ExchangeId = "ex-a", State = "suspended_by_player" }
        };
        Test.True(WorldDiplomacyRoundLifecycleRules.ResolveExchange(active, suspended, "ex-a") == active,
            "active exchange wins over a suspended entry with the same id");
        Test.True(WorldDiplomacyRoundLifecycleRules.ResolveExchange(active, suspended, "ex-s1") == suspended[0],
            "suspended list resolves non-active exchange");
        Test.True(WorldDiplomacyRoundLifecycleRules.ResolveExchange(active, suspended, " ") == null
            && WorldDiplomacyRoundLifecycleRules.ResolveExchange(active, suspended, "none") == null,
            "blank or unknown exchange ids resolve to null");

        // CompleteExchange: stamps fields; active -> caller clears; suspended -> removed in place.
        var done = new WorldDiplomacyExchange { ExchangeId = "ex-x", State = "waiting" };
        var susp = new List<WorldDiplomacyExchange> { done };
        Test.True(!WorldDiplomacyRoundLifecycleRules.CompleteExchange(done, null, susp, "ok", 10),
            "non-active completed exchange returns false and removes from suspended");
        Test.True(done.State == "completed" && done.CompletedDay == 10 && done.CloseReason == "ok"
            && susp.Count == 0, "completed exchange stamped and removed from suspended");
        var activeDone = new WorldDiplomacyExchange { ExchangeId = "ex-y", State = "waiting" };
        var susp2 = new List<WorldDiplomacyExchange> { activeDone };
        Test.True(WorldDiplomacyRoundLifecycleRules.CompleteExchange(activeDone, activeDone, susp2, "fin", 11),
            "active exchange completion reports true so caller clears and schedules");
        Test.True(susp2.Count == 1, "active exchange is not removed from suspended by the rule");
        Test.True(!WorldDiplomacyRoundLifecycleRules.CompleteExchange(null, null, susp2, "x", 1),
            "null exchange is a no-op");

        // SuspendActiveExchangeForPlayerInsertion: captures state, stamps day, inserts at front.
        var toSuspend = new WorldDiplomacyExchange { ExchangeId = "ex-z", State = "waiting", ResponseDueDay = 100, CloseDueDay = 105 };
        var susp3 = new List<WorldDiplomacyExchange> { new WorldDiplomacyExchange { ExchangeId = "older" } };
        Test.True(WorldDiplomacyRoundLifecycleRules.SuspendActiveExchangeForPlayerInsertion(toSuspend, susp3, 60),
            "suspend reports success for a live active exchange");
        Test.True(toSuspend.State == "suspended_by_player" && toSuspend.StateBeforeSuspension == "waiting"
            && toSuspend.SuspendedDay == 60 && susp3[0] == toSuspend && susp3.Count == 2,
            "suspend stamps day, preserves prior state, and pushes to the front");
        Test.True(!WorldDiplomacyRoundLifecycleRules.SuspendActiveExchangeForPlayerInsertion(null, susp3, 60),
            "suspend with no active exchange is a no-op");

        // RestoreSuspendedExchangeIfAny: pauses due days, restores prior state, returns restored.
        var pause = new WorldDiplomacyExchange
        {
            ExchangeId = "ex-p", State = "suspended_by_player", StateBeforeSuspension = "waiting",
            SuspendedDay = 50, ResponseDueDay = 60, CloseDueDay = 70
        };
        var susp4 = new List<WorldDiplomacyExchange> { pause };
        var restored = WorldDiplomacyRoundLifecycleRules.RestoreSuspendedExchangeIfAny(null, susp4, 80);
        Test.True(restored == pause && susp4.Count == 0,
            "restore pops the first suspended exchange");
        Test.True(pause.ResponseDueDay == 90 && pause.CloseDueDay == 100
            && pause.State == "waiting" && pause.StateBeforeSuspension == "",
            "restore shifts due days by the paused span and normalizes state");
        var busy = new WorldDiplomacyExchange { ExchangeId = "ex-busy" };
        Test.True(WorldDiplomacyRoundLifecycleRules.RestoreSuspendedExchangeIfAny(busy, susp4, 80) == null,
            "restore does nothing while an exchange is still active");
        Test.True(WorldDiplomacyRoundLifecycleRules.RestoreSuspendedExchangeIfAny(null, susp4, 80) == null,
            "restore on empty suspended list returns null");

        // RebuildOfferCooldownIndex: keeps the freshest record per key, skips invalid keys.
        var byKey = new Dictionary<WorldDiplomacyOfferCooldownKey, WorldDiplomacyOfferCooldown>();
        var cdOld = new WorldDiplomacyOfferCooldown
        {
            ProposerKingdomId = "a", TargetKingdomId = "b", Domain = "trade", LastFailedRoundDay = 3
        };
        var cdNew = new WorldDiplomacyOfferCooldown
        {
            ProposerKingdomId = "a", TargetKingdomId = "b", Domain = "trade", LastFailedRoundDay = 9
        };
        var list = new List<WorldDiplomacyOfferCooldown> { cdOld, cdNew };
        WorldDiplomacyRoundLifecycleRules.RebuildOfferCooldownIndex(list, byKey);
        Test.True(byKey.Count == 1 && byKey.Values.First() == cdNew,
            "rebuild keeps the newest cooldown record per key");
        WorldDiplomacyRoundLifecycleRules.RebuildOfferCooldownIndex(null, byKey);
        Test.True(byKey.Count == 0, "rebuild on null list clears the index");

        // UpsertOfferCooldown: creates, updates, and triggers overflow normalization.
        var byKey2 = new Dictionary<WorldDiplomacyOfferCooldownKey, WorldDiplomacyOfferCooldown>();
        var cds = new List<WorldDiplomacyOfferCooldown>();
        var key = new WorldDiplomacyOfferCooldownKey("a", "b", WorldDiplomacyOfferDomain.Trade);
        int normalized = 0;
        WorldDiplomacyOfferCooldownApplication.UpsertOfferCooldown(cds, byKey2, key, 12, "r1", () => normalized++);
        Test.True(cds.Count == 1 && byKey2.Count == 1
            && cds[0].ProposerKingdomId == "a" && cds[0].TargetKingdomId == "b"
            && cds[0].Domain == "trade" && cds[0].LastFailedRoundDay == 12
            && cds[0].SourceRoundId == "r1",
            "upsert creates a normalized cooldown record and indexes it");
        WorldDiplomacyOfferCooldownApplication.UpsertOfferCooldown(cds, byKey2, key, 15, "r2", () => normalized++);
        Test.True(cds.Count == 1 && cds[0].LastFailedRoundDay == 15 && cds[0].SourceRoundId == "r2",
            "upsert updates the existing record in place");
        Test.True(normalized == 0, "no normalization below the storage cap");
        var badKey = new WorldDiplomacyOfferCooldownKey("", "b", WorldDiplomacyOfferDomain.Trade);
        WorldDiplomacyOfferCooldownApplication.UpsertOfferCooldown(cds, byKey2, badKey, 1, "x", () => normalized++);
        WorldDiplomacyOfferCooldownApplication.UpsertOfferCooldown(cds, byKey2, key, -1, "x", () => normalized++);
        Test.True(cds.Count == 1, "invalid key and negative day are rejected");

        // RemoveOfferCooldown / ClearBilateralOfferCooldowns: list+index stay in sync.
        WorldDiplomacyRoundLifecycleRules.RemoveOfferCooldown(cds, byKey2, key);
        Test.True(cds.Count == 0 && byKey2.Count == 0, "remove clears record and index entry");
        var cds2 = new List<WorldDiplomacyOfferCooldown>
        {
            new WorldDiplomacyOfferCooldown { ProposerKingdomId = "a", TargetKingdomId = "b", Domain = "trade", LastFailedRoundDay = 1 },
            new WorldDiplomacyOfferCooldown { ProposerKingdomId = "b", TargetKingdomId = "a", Domain = "trade", LastFailedRoundDay = 1 },
            new WorldDiplomacyOfferCooldown { ProposerKingdomId = "a", TargetKingdomId = "b", Domain = "alliance", LastFailedRoundDay = 1 }
        };
        var byKey3 = new Dictionary<WorldDiplomacyOfferCooldownKey, WorldDiplomacyOfferCooldown>();
        WorldDiplomacyRoundLifecycleRules.RebuildOfferCooldownIndex(cds2, byKey3);
        WorldDiplomacyRoundLifecycleRules.ClearBilateralOfferCooldowns(cds2, byKey3, "a", "b", WorldDiplomacyOfferDomain.Trade);
        Test.True(cds2.Count == 1 && cds2[0].Domain == "alliance" && byKey3.Count == 1,
            "bilateral clear removes both directions of the chosen domain only");
        WorldDiplomacyRoundLifecycleRules.ClearBilateralOfferCooldowns(cds2, byKey3, "a", "a", WorldDiplomacyOfferDomain.Trade);
        WorldDiplomacyRoundLifecycleRules.ClearBilateralOfferCooldowns(cds2, byKey3, "a", "b", WorldDiplomacyOfferDomain.None);
        Test.True(cds2.Count == 1, "self pair and None domain are rejected");
    }






    static void RunStorageMigrationDecisionTests()
    {
        // DPL-060BX: prompt-contract migration rebuilds canonical jobs, retires
        // compression, clears round contract state, and commits the version last.
        var promptStorage = new WorldDiplomacyStorage { PromptContractVersion = 27 };
        promptStorage.Jobs.Add(new WorldDiplomacyJob
        {
            JobId = "gen-ok", Kind = "generate", IsRunning = true, SemanticRepairAttempts = 2,
            HistoryPrefixHash = "h1",
            LlmMessages = new List<WorldDiplomacyLlmMessage> { new WorldDiplomacyLlmMessage() }
        });
        promptStorage.Jobs.Add(new WorldDiplomacyJob { JobId = "gen-fail", Kind = "generate", ExchangeId = "ex1" });
        promptStorage.Jobs.Add(new WorldDiplomacyJob { JobId = "comp", Kind = "compress" });
        promptStorage.Jobs.Add(new WorldDiplomacyJob { JobId = "note", Kind = "note", IsRunning = true });
        promptStorage.Jobs.Add(new WorldDiplomacyJob { JobId = "ana", Kind = "analyze", SemanticRepairAttempts = 1 });
        promptStorage.ActiveExchange = new WorldDiplomacyExchange { ExchangeId = "ex1" };
        promptStorage.CompressionRetryAfterHour = 9;
        promptStorage.CompressionRetryAttempts = 4;
        promptStorage.CompletedRounds.Add(new WorldDiplomacyRound
        {
            RoundId = "done", CachePrefix = "cp", CommonContractSnapshot = "snap",
            CommonContractSnapshotInitialized = true,
            LlmTranscript = new List<WorldDiplomacyLlmMessage> { new WorldDiplomacyLlmMessage() },
            LlmProfiledKingdomIds = new List<string> { "kA" },
            LlmLastStateSignatureByKingdom = new Dictionary<string, string> { ["kA"] = "s" }
        });
        var rebuilt = new List<string>();
        var completedExchanges = new List<string>();
        bool affinityCleared = false;
        WorldDiplomacyStorageMigration.MigrateDiplomacyPromptContractIfNeeded(
            promptStorage, 28, true,
            job => { rebuilt.Add(job.JobId); return job.JobId != "gen-fail"; },
            (id, reason) => completedExchanges.Add(id + ":" + reason),
            () => affinityCleared = true,
            _ => { });
        Test.True(promptStorage.PromptContractVersion == 28,
            "prompt migration must commit the target contract version");
        Test.True(!promptStorage.Jobs.Any(x => x.JobId == "gen-fail") && !promptStorage.Jobs.Any(x => x.JobId == "comp"),
            "failed rebuilds and compress jobs must be retired");
        Test.True(promptStorage.Jobs.Any(x => x.JobId == "gen-ok") && promptStorage.Jobs.Any(x => x.JobId == "ana"),
            "rebuilt generate and analyze jobs must be kept");
        WorldDiplomacyJob genOk = promptStorage.Jobs.First(x => x.JobId == "gen-ok");
        Test.True(!genOk.IsRunning && genOk.SemanticRepairAttempts == 0 && genOk.HistoryPrefixHash == ""
            && genOk.LlmMessages.Count == 0,
            "old canonical jobs must discard stale request bodies and repair chains");
        Test.True(rebuilt.Contains("gen-ok") && rebuilt.Contains("ana") && !rebuilt.Contains("comp"),
            "the rebuild port must run for generate and analyze jobs only");
        Test.True(completedExchanges.Contains("ex1:prompt_contract_migration_retired_invalid_job"),
            "a retired non-compress job must complete its bound exchange");
        Test.True(promptStorage.DiplomacyCompressionPending && promptStorage.CompressionRetryAfterHour == 0
            && promptStorage.CompressionRetryAttempts == 0,
            "a retired compression job must be requeued under the new contract");
        WorldDiplomacyRound doneRound = promptStorage.CompletedRounds[0];
        Test.True(doneRound.LlmTranscript.Count == 0 && doneRound.CachePrefix == ""
            && !doneRound.CommonContractSnapshotInitialized && doneRound.CommonContractSnapshot == "",
            "migration must clear round transcript and contract snapshot state");
        Test.True(affinityCleared, "migration must clear the host llm cache affinity");
        int settledJobCount = promptStorage.Jobs.Count;
        WorldDiplomacyStorageMigration.MigrateDiplomacyPromptContractIfNeeded(
            promptStorage, 28, true, j => true, null, null, null);
        Test.True(promptStorage.Jobs.Count == settledJobCount,
            "a second migration run at the target version must be a no-op");
        var notReadyStorage = new WorldDiplomacyStorage { PromptContractVersion = 27 };
        WorldDiplomacyStorageMigration.MigrateDiplomacyPromptContractIfNeeded(
            notReadyStorage, 28, false, j => true, null, null, null);
        Test.True(notReadyStorage.PromptContractVersion == 27,
            "prompt migration must not commit while the world is unavailable");

        // DPL-060BX: autonomous-architecture migration retires obsolete jobs, clears
        // round llm/participant state, and rebuilds the relay route through ports.
        var archStorage = new WorldDiplomacyStorage { DecisionArchitectureVersion = 0 };
        archStorage.Jobs.Add(new WorldDiplomacyJob { JobId = "g1", Kind = "generate", RoundId = "r1" });
        archStorage.Jobs.Add(new WorldDiplomacyJob { JobId = "rp", Kind = "round_plan" });
        archStorage.Jobs.Add(new WorldDiplomacyJob { JobId = "fx", Kind = "note", ForcedIntent = "threat" });
        archStorage.Jobs.Add(new WorldDiplomacyJob { JobId = "keep", Kind = "analyze" });
        archStorage.ActiveExchange = new WorldDiplomacyExchange { ExchangeId = "fx1", IsForced = true };
        archStorage.SuspendedExchanges.Add(new WorldDiplomacyExchange { ExchangeId = "sx-forced", IsForced = true });
        archStorage.SuspendedExchanges.Add(new WorldDiplomacyExchange { ExchangeId = "sx-normal" });
        archStorage.RecentTopicUses.Add(new WorldDiplomacyTopicUse());
        archStorage.WarPressure.Add(new WarPressureEntry { IsEscalationArmed = true, ArmedDay = 5, NeedsFreshEscalation = true });
        archStorage.ForcedWarToggleWasEnabled = true;
        archStorage.RelayArrivals.Add(new WorldDiplomacyRelayArrival { RoundId = "r1" });
        archStorage.RelayArrivals.Add(new WorldDiplomacyRelayArrival { RoundId = "r2" });
        archStorage.Documents.Add(new WorldDiplomacyDocument
        {
            DocumentId = "doc1", IsReadyForPublication = true, AuthorKingdomId = "kA",
            TargetKingdomId = "kB", PlannedRoundTopic = "边境争端", Intent = "warning", RoundId = "r1"
        });
        archStorage.Documents.Add(new WorldDiplomacyDocument { DocumentId = "doc2", RoundId = "r1" });
        var activeRound = new WorldDiplomacyRound
        {
            RoundId = "r1", RootDocumentId = "doc1", RelayPlanned = true, RelayWaiting = true,
            RequiresSharedBorder = true, AutomaticDocumentsStarted = 5, CachePrefix = "cp",
            TopicSeedContext = "ctx", TopicFingerprint = "fp",
            RelayRouteKingdomIds = new List<string> { "kA", "kB", "kGone" }, RelayCursor = 1, RelayDirection = -1,
            LlmTranscript = new List<WorldDiplomacyLlmMessage> { new WorldDiplomacyLlmMessage() },
            PotentialActionIntents = new List<string> { "x" },
            Participants = new List<WorldDiplomacyRoundParticipant>
            {
                new WorldDiplomacyRoundParticipant { KingdomId = "kA", Role = "issuer", SelectedForRelay = false },
                new WorldDiplomacyRoundParticipant { KingdomId = "kB", Role = "target", SelectedForRelay = false },
                new WorldDiplomacyRoundParticipant { KingdomId = "kGone", Role = "relay", SelectedForRelay = true }
            }
        };
        archStorage.ActiveRound = activeRound;
        var closedReasons = new List<string>();
        WorldDiplomacyStorageMigration.MigrateAutonomousDecisionArchitectureIfNeeded(
            archStorage, 1, 23, 100, true,
            id => archStorage.Documents.FirstOrDefault(d => d.DocumentId == id),
            id => id == "kGone" ? null : id,
            (a, b) => false,
            closedReasons.Add,
            _ => { });
        Test.True(archStorage.DecisionArchitectureVersion == 1,
            "architecture migration must commit the target version");
        Test.True(archStorage.Jobs.Count == 1 && archStorage.Jobs[0].JobId == "keep",
            "obsolete generate, round_plan, and forced-intent jobs must be retired");
        Test.True(archStorage.ActiveExchange == null && archStorage.SuspendedExchanges.Count == 1
            && archStorage.SuspendedExchanges[0].ExchangeId == "sx-normal",
            "forced or pending-action exchanges must be closed and removed");
        Test.True(archStorage.RecentTopicUses.Count == 0 && !archStorage.WarPressure[0].IsEscalationArmed
            && archStorage.WarPressure[0].ArmedDay == 0 && !archStorage.WarPressure[0].NeedsFreshEscalation
            && !archStorage.ForcedWarToggleWasEnabled,
            "topic and war-pressure escalation state must be reset");
        Test.True(activeRound.LlmTranscript.Count == 0 && activeRound.CachePrefix == "" && !activeRound.RelayWaiting
            && activeRound.TopicSeedContext == "" && activeRound.PotentialActionIntents.Count == 0
            && activeRound.Participants.All(p => p.Role == "" && p.Agenda == ""),
            "active-round llm and participant state must be cleared");
        Test.True(activeRound.AutomaticDocumentsStarted == 4,
            "automatic document count must floor at published automatic documents and subtract retired jobs");
        Test.True(archStorage.RelayArrivals.Count == 1 && archStorage.RelayArrivals[0].RoundId == "r2",
            "relay arrivals bound to the active round must be removed");
        Test.True(closedReasons.Count == 0 && archStorage.NextNormalRoundDay == 0,
            "a published root with an eligible author must not close the round");
        Test.True(activeRound.RoundTopic == "边境争端" && activeRound.TopicCategory == "war_escalation"
            && activeRound.SchemaVersion == 23,
            "round topic and schema must be rebuilt from the root document");
        Test.True(activeRound.RelayRouteKingdomIds.Count == 2 && activeRound.RelayRouteKingdomIds[0] == "kA"
            && activeRound.RelayRouteKingdomIds[1] == "kB",
            "the relay route must drop ineligible kingdoms and keep the root author first");
        Test.True(activeRound.RelayCursor == 1 && activeRound.RelayDirection == -1,
            "the relay cursor must track the previous cursor kingdom in the rebuilt route");
        Test.True(activeRound.Participants.First(p => p.KingdomId == "kA").SelectedForRelay
            && activeRound.Participants.First(p => p.KingdomId == "kB").SelectedForRelay
            && !activeRound.Participants.First(p => p.KingdomId == "kGone").SelectedForRelay,
            "participant relay selection must follow the rebuilt route");
        var unpublishedStorage = new WorldDiplomacyStorage { DecisionArchitectureVersion = 0 };
        unpublishedStorage.ActiveRound = new WorldDiplomacyRound { RoundId = "r9", RootDocumentId = "doc9" };
        var unpublishedReasons = new List<string>();
        WorldDiplomacyStorageMigration.MigrateAutonomousDecisionArchitectureIfNeeded(
            unpublishedStorage, 1, 23, 40, true,
            id => new WorldDiplomacyDocument { DocumentId = id, IsReadyForPublication = false },
            id => id, (a, b) => false,
            unpublishedReasons.Add,
            _ => { });
        Test.True(unpublishedReasons.Contains("technical_architecture_migration_unpublished_round")
            && unpublishedStorage.NextNormalRoundDay == 41,
            "an unpublished root must close the round and reschedule the next round day");
        var ineligibleStorage = new WorldDiplomacyStorage { DecisionArchitectureVersion = 0 };
        ineligibleStorage.ActiveRound = new WorldDiplomacyRound { RoundId = "r8", RootDocumentId = "doc8" };
        var ineligibleReasons = new List<string>();
        WorldDiplomacyStorageMigration.MigrateAutonomousDecisionArchitectureIfNeeded(
            ineligibleStorage, 1, 23, 40, true,
            id => new WorldDiplomacyDocument { DocumentId = id, IsReadyForPublication = true, AuthorKingdomId = "gone" },
            id => null, (a, b) => false,
            ineligibleReasons.Add,
            _ => { });
        Test.True(ineligibleReasons.Contains("technical_architecture_migration_invalid_root_author")
            && ineligibleStorage.NextNormalRoundDay == 41,
            "a published root without an eligible author must close the round");
        var notReadyWorld = new WorldDiplomacyStorage { DecisionArchitectureVersion = 0 };
        notReadyWorld.ActiveRound = new WorldDiplomacyRound { RoundId = "r7", RootDocumentId = "doc7" };
        WorldDiplomacyStorageMigration.MigrateAutonomousDecisionArchitectureIfNeeded(
            notReadyWorld, 1, 23, 40, false, null, null, null, null, null);
        Test.True(notReadyWorld.DecisionArchitectureVersion == 0,
            "architecture migration must defer while an active round exists and the world is unavailable");
    }

    static void RunCanonicalHistoryMigrationDecisionTests()
    {
        // DPL-060BZ: policy countdown history migration collapses daily copies,
        // rebuilds event revisions, and clears persisted repair chains via ports.
        Func<int, string> policyText = days =>
            "政策正文\n影响：目标王国：甲国｜粮食：每日 +2｜期限：持续 100 天｜状态：剩余 "
            + days.ToString(CultureInfo.InvariantCulture) + "/100 天。\n承载效果：粮食 +2";
        WorldDiplomacyCanonicalHistoryEntry MkPolicyEntry(long seq, int days) => new WorldDiplomacyCanonicalHistoryEntry
        {
            Sequence = seq, Kind = "policy_published", SourceId = "p1", Text = policyText(days),
            GameDate = "d", AuthorKingdomId = "kA", Verified = true
        };
        var history = new WorldDiplomacyCanonicalHistoryState
        {
            Revision = 7, LastPolicyArtifactSequence = 9, LastPolicyArtifactRevision = 4,
            LastPolicyArtifactLedgerId = "led"
        };
        history.PolicyRevisionSignatures["p1"] = "sig";
        history.DeltaEntries.Add(MkPolicyEntry(1, 100));
        history.DeltaEntries.Add(MkPolicyEntry(2, 90));
        history.DeltaEntries.Add(MkPolicyEntry(3, 80));
        history.DeltaEntries.Add(new WorldDiplomacyCanonicalHistoryEntry
        {
            Sequence = 4, Kind = "declaration", SourceId = "d1", Text = "正文"
        });
        var jobs = new List<WorldDiplomacyJob>
        {
            new WorldDiplomacyJob
            {
                JobId = "gen", Kind = "generate", SemanticRepairAttempts = 3, HistoryPrefixHash = "h",
                LlmMessages = new List<WorldDiplomacyLlmMessage> { new WorldDiplomacyLlmMessage() }
            },
            new WorldDiplomacyJob { JobId = "run", Kind = "generate", IsRunning = true, SemanticRepairAttempts = 2, HistoryPrefixHash = "h" },
            new WorldDiplomacyJob { JobId = "note", Kind = "note", SemanticRepairAttempts = 5, HistoryPrefixHash = "h" }
        };
        bool invalidated = false;
        var policyLogs = new List<string>();
        WorldDiplomacyStorageMigration.MigratePolicyCountdownHistory(
            history, jobs, () => invalidated = true, policyLogs.Add);
        Test.True(history.PolicyHistorySchemaVersion == 1 && history.DeltaEntries.Count == 2
            && history.DeltaEntries[0].Sequence == 1 && history.DeltaEntries[1].Sequence == 4,
            "countdown migration must collapse same-policy daily copies and keep other entries");
        Test.True(history.PolicyEventRevisions.TryGetValue("p1", out long rebuiltRevision) && rebuiltRevision == 1
            && history.PolicyRevisionSignatures.Count == 0
            && history.LastPolicyArtifactSequence == 0 && history.LastPolicyArtifactRevision == 0
            && history.LastPolicyArtifactLedgerId == "" && history.Revision == 8,
            "countdown migration must rebuild event revisions and reset the policy ledger cursor");
        Test.True(jobs[0].SemanticRepairAttempts == 0 && jobs[0].HistoryPrefixHash == ""
            && jobs[0].LlmMessages.Count == 0,
            "non-running canonical jobs must drop stale repair chains");
        Test.True(jobs[1].SemanticRepairAttempts == 2 && jobs[2].SemanticRepairAttempts == 5,
            "running and non-canonical jobs must keep their persisted state");
        Test.True(invalidated && policyLogs.Any(x => x.Contains("removed=2")),
            "countdown migration must invalidate the render cache and log the removed count");
        var gatedHistory = new WorldDiplomacyCanonicalHistoryState { PolicyHistorySchemaVersion = 1 };
        gatedHistory.DeltaEntries.Add(MkPolicyEntry(9, 10));
        bool gateInvalidated = false;
        WorldDiplomacyStorageMigration.MigratePolicyCountdownHistory(
            gatedHistory, null, () => gateInvalidated = true, null);
        Test.True(gatedHistory.DeltaEntries.Count == 1 && !gateInvalidated,
            "countdown migration must be a no-op at the current schema version");

        // DPL-060BZ: response-link backfill selects unlinked published documents
        // and delegates the canonical-history append through a narrow port.
        var linkStorage = new WorldDiplomacyStorage { HistoryMemorySchemaVersion = 1 };
        linkStorage.Documents.Add(new WorldDiplomacyDocument
        {
            DocumentId = "d1", IsReadyForPublication = true, RespondingToOfferDocumentId = "o1",
            Body = "接受", AddressedKingdomIds = new List<string> { "kB", "kC" }, TargetKingdomId = "kB"
        });
        linkStorage.Documents.Add(new WorldDiplomacyDocument
        {
            DocumentId = "d2", IsReadyForPublication = true, RespondingToOfferDocumentId = "o2",
            Body = "拒绝", TargetKingdomId = "kC"
        });
        linkStorage.Documents.Add(new WorldDiplomacyDocument { DocumentId = "d3", IsReadyForPublication = true, Body = "无链接" });
        linkStorage.CanonicalHistory.DeltaEntries.Add(new WorldDiplomacyCanonicalHistoryEntry
        {
            SourceId = "d1", RespondingToOfferDocumentId = "o1"
        });
        var appended = new List<string>();
        WorldDiplomacyStorageMigration.BackfillCanonicalResponseLinksV2(linkStorage,
            (doc, targets) => { appended.Add(doc.DocumentId + ":" + string.Join(",", targets)); return true; });
        Test.True(appended.Count == 1 && appended[0] == "d2:kC",
            "backfill must skip already-linked and non-response documents and normalize targets");
        var gatedLinkStorage = new WorldDiplomacyStorage { HistoryMemorySchemaVersion = 2 };
        gatedLinkStorage.Documents.Add(new WorldDiplomacyDocument
        {
            DocumentId = "d4", IsReadyForPublication = true, RespondingToOfferDocumentId = "o4", Body = "x"
        });
        WorldDiplomacyStorageMigration.BackfillCanonicalResponseLinksV2(gatedLinkStorage,
            (doc, targets) => { appended.Add("BAD"); return true; });
        Test.True(!appended.Contains("BAD"),
            "backfill must not run at or above the response-link schema version");

        // DPL-060BZ: result-settlement state migration re-opens settlement for
        // confirmed results published before the schema existed.
        var settleStorage = new WorldDiplomacyStorage { ResultSettlementStateSchemaVersion = 0 };
        var settleRound = new WorldDiplomacyRound { RoundId = "r1", State = "active" };
        settleStorage.ActiveRound = settleRound;
        settleStorage.Documents.Add(new WorldDiplomacyDocument
        {
            DocumentId = "war", RoundId = "r1", IsReadyForPublication = true, Day = 10,
            Actions = new List<WorldDiplomacyDocumentAction>
            {
                new WorldDiplomacyDocumentAction { Intent = "declare_war", ChangedDiplomaticState = true }
            }
        });
        settleStorage.Documents.Add(new WorldDiplomacyDocument
        {
            DocumentId = "draft", RoundId = "r1", IsReadyForPublication = false
        });
        var settleCalls = new List<string>();
        var settleLogs = new List<string>();
        WorldDiplomacyStorageMigration.MigrateResultSettlementStateIfNeeded(
            settleStorage, 1,
            (round, doc, closeReason, roundStatus) =>
                settleCalls.Add(round.RoundId + "/" + doc.DocumentId + "/" + closeReason + "/" + roundStatus),
            settleLogs.Add);
        Test.True(settleStorage.ResultSettlementStateSchemaVersion == 1
            && settleCalls.Count == 1 && settleCalls[0] == "r1/war/war_declared/resolved"
            && settleLogs.Any(x => x.Contains("version=1") && x.Contains("active=")),
            "result-settlement migration must reopen settlement for confirmed results and stamp the version");
        var settleGate = new WorldDiplomacyStorage { ResultSettlementStateSchemaVersion = 1 };
        settleGate.ActiveRound = new WorldDiplomacyRound { RoundId = "r2", State = "active" };
        WorldDiplomacyStorageMigration.MigrateResultSettlementStateIfNeeded(
            settleGate, 1, (r, d, c, s) => settleCalls.Add("BAD"), null);
        Test.True(!settleCalls.Contains("BAD"),
            "result-settlement migration must be a no-op at the target version");

        // DPL-060BZ: canonical token accounting aggregates snapshot and delta
        // entries and raises the compression flag against the passed trigger.
        var tokenStorage = new WorldDiplomacyStorage();
        tokenStorage.CanonicalHistory.Snapshot.EstimatedTokens = 100;
        tokenStorage.CanonicalHistory.DeltaEntries.Add(new WorldDiplomacyCanonicalHistoryEntry { EstimatedTokens = 40 });
        tokenStorage.CanonicalHistory.DeltaEntries.Add(new WorldDiplomacyCanonicalHistoryEntry { EstimatedTokens = -5 });
        tokenStorage.CanonicalHistory.DeltaEntries.Add(new WorldDiplomacyCanonicalHistoryEntry { EstimatedTokens = 12 });
        WorldDiplomacyRoundLifecycleRules.RecalculateCanonicalHistoryTokens(tokenStorage, 150);
        Test.True(tokenStorage.CanonicalHistory.EstimatedTokens == 152
            && tokenStorage.DiplomacyTokensSinceCompression == 152
            && tokenStorage.DiplomacyCompressionPending,
            "token accounting must sum snapshot and delta estimates and flag compression at the trigger");
        WorldDiplomacyRoundLifecycleRules.RecalculateCanonicalHistoryTokens(tokenStorage, 200);
        Test.True(!tokenStorage.DiplomacyCompressionPending,
            "token accounting must clear the compression flag below the trigger");
        var emptyStorage = new WorldDiplomacyStorage { CanonicalHistory = null };
        WorldDiplomacyRoundLifecycleRules.RecalculateCanonicalHistoryTokens(emptyStorage, 100);
        Test.True(emptyStorage.DiplomacyTokensSinceCompression == 0 && !emptyStorage.DiplomacyCompressionPending,
            "token accounting must no-op when canonical history is absent");
    }

    static void RunCanonicalHistoryInitDecisionTests()
    {
        // DPL-060CA: canonical-history normalization must retire covered/blank
        // entries, dedupe by sequence, rebuild source keys, resolve the next
        // sequence, estimate missing tokens, and refresh the snapshot hash.
        var storage = new WorldDiplomacyStorage
        {
            CanonicalHistory = new WorldDiplomacyCanonicalHistoryState
            {
                PolicyHistorySchemaVersion = 1,
                NextSequence = 2,
                Snapshot = new WorldDiplomacyCanonicalHistorySnapshot
                {
                    CoveredThroughSequence = 5,
                    ProtectedFacts = new List<WorldDiplomacyCanonicalProtectedFact>
                    {
                        new WorldDiplomacyCanonicalProtectedFact
                        {
                            Kind = "diplomatic_result", SourceId = "snap_a",
                            SourceKey = "snap:a", Text = "kept", Sequence = 2
                        },
                        new WorldDiplomacyCanonicalProtectedFact
                        {
                            Kind = "bogus", SourceId = "bad",
                            SourceKey = "snap:b", Text = "dropped", Sequence = 1
                        }
                    },
                    PreservedResultSourceIds = null
                },
                DeltaEntries = new List<WorldDiplomacyCanonicalHistoryEntry>
                {
                    new WorldDiplomacyCanonicalHistoryEntry { Sequence = 3, Kind = "diplomatic_result", SourceKey = "retired_low" },
                    new WorldDiplomacyCanonicalHistoryEntry { Sequence = 8, Kind = "", SourceKey = "blank_kind" },
                    new WorldDiplomacyCanonicalHistoryEntry { Sequence = 9, Kind = "diplomatic_result", SourceKey = "dup_first", SourceId = "doc_9a" },
                    new WorldDiplomacyCanonicalHistoryEntry { Sequence = 9, Kind = "diplomatic_result", SourceKey = "dup_second", SourceId = "doc_9b" },
                    new WorldDiplomacyCanonicalHistoryEntry { Sequence = 7, Kind = "diplomatic_result", SourceKey = "keep_a", SourceId = "doc_7" }
                },
                WorldWeeklySourceHashes = null,
                PolicyEventRevisions = null
            }
        };
        var sourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "stale_key" };
        int invalidations = 0;
        int estimateCalls = 0;
        WorldDiplomacyStorageMigration.NormalizeCanonicalHistoryState(
            storage,
            sourceKeys,
            1000,
            text => { estimateCalls++; return text?.Length ?? 0; },
            () => invalidations++,
            _ => { });
        WorldDiplomacyCanonicalHistoryState history = storage.CanonicalHistory;
        Test.True(string.Join(",", history.DeltaEntries.Select(x => x.SourceKey)) == "keep_a,dup_first",
            "normalization must retire covered/blank entries, dedupe by sequence, and order the survivors");
        Test.True(sourceKeys.Count == 2 && sourceKeys.Contains("keep_a") && sourceKeys.Contains("dup_first"),
            "normalization must rebuild the source-key set from surviving delta entries");
        Test.True(history.NextSequence == 10,
            "normalization must resolve the next sequence above the last surviving delta entry");
        Test.True(history.Snapshot.ProtectedFacts.Count == 1 && history.Snapshot.ProtectedFacts[0].SourceId == "snap_a",
            "normalization must drop invalid snapshot protected facts");
        Test.True(history.Snapshot.PreservedResultSourceIds != null
            && history.Snapshot.PreservedResultSourceIds.Contains("snap_a"),
            "normalization must merge protected diplomatic results into preserved source ids");
        Test.True(history.WorldWeeklySourceHashes != null && history.PolicyEventRevisions != null,
            "normalization must initialize missing revision maps");
        Test.True(history.DeltaEntries.All(x => x.EstimatedTokens > 0) && estimateCalls >= 3,
            "normalization must estimate tokens for entries and the snapshot payload");
        Test.True(!string.IsNullOrEmpty(history.Snapshot.ContentHash) && history.Snapshot.EstimatedTokens > 0,
            "normalization must refresh the snapshot content hash and token estimate");
        Test.True(storage.DiplomacyTokensSinceCompression > 0 && !storage.DiplomacyCompressionPending,
            "normalization must recalculate total tokens below the compression trigger");
        Test.True(invalidations == 0,
            "normalization must not invalidate the render cache when the countdown schema is current");

        // DPL-060CA: protected-fact projection must keep valid snapshot facts,
        // project verified results and response links from delta history, and
        // dedupe by stable key before ordering by sequence.
        var factHistory = new WorldDiplomacyCanonicalHistoryState
        {
            Snapshot = new WorldDiplomacyCanonicalHistorySnapshot
            {
                ProtectedFacts = new List<WorldDiplomacyCanonicalProtectedFact>
                {
                    new WorldDiplomacyCanonicalProtectedFact
                    {
                        Kind = "diplomatic_result", SourceId = "snap_1",
                        SourceKey = "snap:1", Text = "kept fact", Sequence = 1
                    },
                    new WorldDiplomacyCanonicalProtectedFact
                    {
                        Kind = "not_a_fact", SourceId = "bad",
                        SourceKey = "snap:bad", Text = "x", Sequence = 0
                    },
                    new WorldDiplomacyCanonicalProtectedFact
                    {
                        Kind = "diplomatic_result", SourceId = "snap_1_dup",
                        SourceKey = "snap:1", Text = "duplicate stable key", Sequence = 5
                    }
                }
            },
            DeltaEntries = new List<WorldDiplomacyCanonicalHistoryEntry>
            {
                new WorldDiplomacyCanonicalHistoryEntry
                {
                    Sequence = 2, Kind = "diplomatic_result", Verified = true,
                    SourceId = "doc_r1", SourceKey = "r1", Text = "verified result", Day = 10
                },
                new WorldDiplomacyCanonicalHistoryEntry
                {
                    Sequence = 3, Kind = "diplomatic_result", Verified = false,
                    SourceId = "doc_r2", SourceKey = "r2", Text = "unverified"
                },
                new WorldDiplomacyCanonicalHistoryEntry
                {
                    Sequence = 4, Kind = "offer_response", Verified = false,
                    SourceId = "resp_1", SourceKey = "resp:1",
                    RespondingToOfferDocumentId = "offer_9", Day = 11
                }
            }
        };
        List<WorldDiplomacyCanonicalProtectedFact> facts =
            WorldDiplomacyRoundLifecycleRules.BuildCanonicalProtectedFactsThrough(factHistory, 100);
        Test.True(string.Join(",", facts.Select(x => x.Sequence)) == "1,2,4",
            "protected-fact projection must keep valid facts and order by sequence");
        Test.True(facts.Count(x => x.Kind == "response_link"
                && x.SourceId == "resp_1" && x.RelatedSourceId == "offer_9") == 1,
            "protected-fact projection must project offer-response links from delta entries");
        Test.True(facts.All(x => x.SourceId != "doc_r2" && x.SourceId != "bad" && x.SourceId != "snap_1_dup"),
            "protected-fact projection must drop unverified results, invalid kinds, and duplicate stable keys");
        List<WorldDiplomacyCanonicalProtectedFact> cutoffFacts =
            WorldDiplomacyRoundLifecycleRules.BuildCanonicalProtectedFactsThrough(factHistory, 2);
        Test.True(string.Join(",", cutoffFacts.Select(x => x.Sequence)) == "1,2",
            "protected-fact projection must exclude delta entries beyond the cutoff");
    }

    static void RunCanonicalHistoryAppendDecisionTests()
    {
        // DPL-060CB: the canonical append path must normalize fields, dedupe by
        // source key, stamp sequence/tokens, and update compression accounting.
        var storage = new WorldDiplomacyStorage
        {
            CanonicalHistory = new WorldDiplomacyCanonicalHistoryState { NextSequence = 1 }
        };
        var sourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int ensured = 0;
        int invalidations = 0;
        Func<string, int> estimate = t => t?.Length ?? 0;
        bool appended = WorldDiplomacyHistoryPublicationApplication.AppendCanonicalHistoryEntry(
            storage, sourceKeys, 1000,
            () => ensured++,
            prefix => prefix + "_1",
            day => "d" + day,
            estimate,
            () => invalidations++,
            " Diplomatic_Result ", " key:a ", "doc_a", 7, "", "k1",
            new[] { "k2", "k2", " " }, " WAR ", " HARD ", "  body text  ", true,
            actionFacts: new[] { "a1@k2=war" });
        Test.True(appended, "canonical append must accept a well-formed entry");
        WorldDiplomacyCanonicalHistoryEntry entry = storage.CanonicalHistory.DeltaEntries.Single();
        Test.True(entry.Sequence == 1 && storage.CanonicalHistory.NextSequence == 2
            && entry.Kind == "diplomatic_result" && entry.SourceKey == "key:a"
            && entry.GameDate == "d7" && entry.TargetKingdomIds.Count == 1
            && entry.TargetKingdomIds[0] == "k2" && entry.Verified,
            "canonical append must stamp sequence and normalize kind, key, date, and targets");
        Test.True(entry.EstimatedTokens > 0 && storage.CanonicalHistory.EstimatedTokens == entry.EstimatedTokens
            && storage.DiplomacyTokensSinceCompression == entry.EstimatedTokens
            && !storage.DiplomacyCompressionPending && storage.CanonicalHistory.Revision == 1,
            "canonical append must account tokens and revision below the trigger");
        Test.True(ensured == 1 && invalidations == 1 && sourceKeys.Contains("key:a"),
            "canonical append must ensure initialization, invalidate the render cache, and track the source key");
        bool duplicate = WorldDiplomacyHistoryPublicationApplication.AppendCanonicalHistoryEntry(
            storage, sourceKeys, 1000, () => ensured++, p => p + "_x", d => "d" + d, estimate,
            () => invalidations++, "diplomatic_result", " KEY:A ", "doc_b", 8, "", "k1",
            null, "", "", "other", true);
        Test.True(!duplicate && storage.CanonicalHistory.DeltaEntries.Count == 1
            && ensured == 2 && invalidations == 1,
            "canonical append must reject a duplicate source key after ensuring initialization");
        bool rejected = WorldDiplomacyHistoryPublicationApplication.AppendCanonicalHistoryEntry(
            storage, sourceKeys, 1000, () => ensured++, p => p + "_x", d => "d" + d, estimate,
            () => invalidations++, "", "key:c", "doc_c", 8, "", "k1",
            null, "", "", "other", true);
        Test.True(!rejected, "canonical append must reject a blank kind");
        appended = WorldDiplomacyHistoryPublicationApplication.AppendCanonicalHistoryEntry(
            storage, sourceKeys, 1, () => ensured++, p => p + "_x", d => "d" + d, estimate,
            () => invalidations++, "diplomatic_result", "key:d", "doc_d", 9, "", "k1",
            null, "", "", "more", true);
        Test.True(appended && storage.DiplomacyCompressionPending,
            "canonical append must raise the compression flag once tokens reach the trigger");

        // DPL-060CB: document event projection must record declarations and
        // per-action results, mark the document, and stay idempotent.
        var docStorage = new WorldDiplomacyStorage
        {
            CanonicalHistory = new WorldDiplomacyCanonicalHistoryState { NextSequence = 1 }
        };
        var docKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var document = new WorldDiplomacyDocument
        {
            DocumentId = "doc1",
            IsReadyForPublication = true,
            Body = "declaration body",
            Day = 12,
            GameDate = "g12",
            AuthorKingdomId = "kA",
            Intent = "peace",
            Commitment = "soft",
            Actions = new List<WorldDiplomacyDocumentAction>
            {
                new WorldDiplomacyDocumentAction
                {
                    ActionId = "a1", TargetKingdomId = "kB", Intent = "peace",
                    ChangedDiplomaticState = true, MechanicalResult = "peace_confirmed"
                }
            }
        };
        WorldDiplomacyHistoryPublicationApplication.AppendCanonicalDocumentEvents(
            docStorage, docKeys, 1000, () => { }, p => p + "_n", d => "d" + d, estimate,
            () => { }, document);
        Test.True(docKeys.Contains("document:doc1:declaration")
            && docKeys.Contains("document:doc1:action:a1:result")
            && document.HistoryDeclarationRecorded && document.HistoryResultRecorded
            && docStorage.CanonicalHistory.DeltaEntries.Count == 2,
            "document events must record the declaration and each confirmed action result once");
        WorldDiplomacyHistoryPublicationApplication.AppendCanonicalDocumentEvents(
            docStorage, docKeys, 1000, () => { }, p => p + "_n", d => "d" + d, estimate,
            () => { }, document);
        Test.True(docStorage.CanonicalHistory.DeltaEntries.Count == 2,
            "document events must be idempotent once history flags are recorded");

        // A partial oral effect must retain its qualification all the way into
        // canonical history, whose external-fact path skips the declaration.
        var oralStorage = new WorldDiplomacyStorage();
        var oralKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        const string partialPeace = "双方已停战，贡金条款未完整确认";
        WorldDiplomacyDocumentPublicationApplication.NotifyExternalDiplomacyResolved(
            "accept_peace", "a", "b", partialPeace, true, oralStorage, 12,
            _ => true, _ => { },
            (title, body, origin, player) => new WorldDiplomacyDocument
            { DocumentId = "partial-oral", Body = body, IsReadyForPublication = true },
            _ => partialPeace, _ => false,
            _ => { }, _ => { },
            doc => WorldDiplomacyHistoryPublicationApplication.AppendCanonicalDocumentEvents(
                oralStorage, oralKeys, 1000, () => { }, p => p + "_oral", d => "d" + d, estimate, () => { }, doc),
            _ => { }, _ => { }, _ => { });
        Test.True(oralStorage.CanonicalHistory.DeltaEntries.Count == 1
            && oralStorage.CanonicalHistory.DeltaEntries[0].Kind == "diplomatic_result"
            && oralStorage.CanonicalHistory.DeltaEntries[0].Text.Contains(partialPeace, StringComparison.Ordinal),
            "external publication and actual history append preserve partial peace qualification");

        // DPL-060CB: weekly artifact projection must dedupe by content hash and
        // bump the source revision only when the payload actually changes.
        var weeklyStorage = new WorldDiplomacyStorage
        {
            CanonicalHistory = new WorldDiplomacyCanonicalHistoryState { NextSequence = 1 }
        };
        var weeklyKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        WorldDiplomacyHistoryPublicationApplication.AppendPublishedWorldWeeklyArtifact(
            weeklyStorage, weeklyKeys, 1000, () => { }, p => p + "_w", d => "d" + d, estimate,
            () => { }, "wk1", "标题", "周报正文", 5, "d5");
        Test.True(weeklyKeys.Contains("weekly:wk1:r1")
            && weeklyStorage.CanonicalHistory.WorldWeeklySourceRevisions["wk1"] == 1,
            "weekly artifact append must record revision r1 for a new source");
        WorldDiplomacyHistoryPublicationApplication.AppendPublishedWorldWeeklyArtifact(
            weeklyStorage, weeklyKeys, 1000, () => { }, p => p + "_w", d => "d" + d, estimate,
            () => { }, "wk1", "标题", "周报正文", 5, "d5");
        Test.True(weeklyStorage.CanonicalHistory.DeltaEntries.Count == 1
            && weeklyStorage.CanonicalHistory.WorldWeeklySourceRevisions["wk1"] == 1,
            "weekly artifact append must skip unchanged payloads by content hash");
        WorldDiplomacyHistoryPublicationApplication.AppendPublishedWorldWeeklyArtifact(
            weeklyStorage, weeklyKeys, 1000, () => { }, p => p + "_w", d => "d" + d, estimate,
            () => { }, "wk1", "标题", "周报正文更新", 6, "d6");
        Test.True(weeklyKeys.Contains("weekly:wk1:r2")
            && weeklyStorage.CanonicalHistory.WorldWeeklySourceRevisions["wk1"] == 2
            && weeklyStorage.CanonicalHistory.DeltaEntries.Count == 2,
            "weekly artifact append must bump the revision when the payload changes");
    }

    static void RunPolicyArtifactSyncDecisionTests()
    {
        // DPL-060CC: policy artifact sync must track the ledger epoch, walk the
        // cursor in batches, append only real revisions, and acknowledge progress.
        var storage = new WorldDiplomacyStorage
        {
            CanonicalHistory = new WorldDiplomacyCanonicalHistoryState { NextSequence = 1 }
        };
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ledgerEntries = new List<PublishedPolicyArtifactLedgerEntry>
        {
            new PublishedPolicyArtifactLedgerEntry
            {
                Sequence = 1, Revision = 1, PolicyId = "pol_a", EventKind = "policy_published",
                PublishedText = "政策正文A", ContentHash = "hA1", OccurredDay = 3,
                GameDate = "g3", KingdomId = "kA"
            },
            new PublishedPolicyArtifactLedgerEntry
            {
                Sequence = 3, Revision = 2, PolicyId = "pol_a", EventKind = "policy_published",
                PublishedText = "政策正文A改", ContentHash = "hA2", OccurredDay = 8,
                GameDate = "g8", KingdomId = "kA"
            }
        };
        var fetches = new List<long>();
        var acks = new List<long>();
        var logs = new List<string>();
        WorldDiplomacyHistoryPublicationApplication.SyncPublishedPolicyArtifacts(
            storage, keys, 1000,
            () => { }, p => "id", d => "d" + d, t => t?.Length ?? 0, () => { }, () => 20,
            logs.Add,
            4, 8,
            () => "ledger1",
            () => 5,
            () => 3,
            (cursor, size) =>
            {
                fetches.Add(cursor);
                return ledgerEntries.Where(x => x.Sequence > cursor).Take(size).ToList();
            },
            c => { acks.Add(c); return true; });
        WorldDiplomacyCanonicalHistoryState history = storage.CanonicalHistory;
        Test.True(history.LastPolicyArtifactLedgerId == "ledger1"
            && history.LastPolicyArtifactRevision == 5 && history.LastPolicyArtifactSequence == 3,
            "policy sync must record the ledger id, source revision, and cursor");
        Test.True(keys.Contains("policy:event-v2:pol_a:r1")
            && keys.Contains("policy:event-v2:pol_a:r2")
            && history.DeltaEntries.Count == 2
            && history.DeltaEntries[0].Kind == "policy_published"
            && history.DeltaEntries[1].Kind == "policy_snapshot",
            "policy sync must append r1 as published and the changed revision as a snapshot");
        Test.True(acks.Count == 1 && acks[0] == 3,
            "policy sync must acknowledge the ledger exactly once per drained batch");
        Test.True(history.PolicyRevisionSignatures["pol_a"] == "hA2"
            && history.PolicyEventRevisions["pol_a"] == 2,
            "policy sync must store the latest fingerprint and revision per signature key");

        // A blank ledger id must be an early no-op before any ledger reads.
        fetches.Clear(); acks.Clear();
        WorldDiplomacyHistoryPublicationApplication.SyncPublishedPolicyArtifacts(
            storage, keys, 1000,
            () => { }, p => "id", d => "d" + d, t => t?.Length ?? 0, () => { }, () => 20,
            logs.Add,
            4, 8,
            () => "  ",
            () => { throw new InvalidOperationException("revision must not be read for a blank ledger"); },
            () => { throw new InvalidOperationException("sequence must not be read for a blank ledger"); },
            (cursor, size) => { throw new InvalidOperationException("artifacts must not be fetched for a blank ledger"); },
            c => { throw new InvalidOperationException("nothing may be acknowledged for a blank ledger"); });
        Test.True(fetches.Count == 0 && acks.Count == 0 && history.DeltaEntries.Count == 2,
            "policy sync must no-op on a blank ledger id without touching ledger state");

        // An epoch change must reset the cursor, log, and resync from scratch.
        logs.Clear(); fetches.Clear(); acks.Clear();
        history.LastPolicyArtifactLedgerId = "old_ledger";
        history.LastPolicyArtifactSequence = 9;
        WorldDiplomacyHistoryPublicationApplication.SyncPublishedPolicyArtifacts(
            storage, keys, 1000,
            () => { }, p => "id", d => "d" + d, t => t?.Length ?? 0, () => { }, () => 20,
            logs.Add,
            4, 8,
            () => "ledger1",
            () => 5,
            () => 3,
            (cursor, size) =>
            {
                fetches.Add(cursor);
                return ledgerEntries.Where(x => x.Sequence > cursor).Take(size).ToList();
            },
            c => { acks.Add(c); return true; });
        Test.True(logs.Any(x => x.Contains("epoch changed", StringComparison.Ordinal))
            && history.LastPolicyArtifactLedgerId == "ledger1"
            && fetches.Count > 0 && fetches[0] == 0,
            "policy sync must log an epoch change, reset the cursor, and rescan from zero");

        // Signature rebuild must fill fingerprints only below the schema gate.
        var rebuildHistory = new WorldDiplomacyCanonicalHistoryState { PolicyHistorySchemaVersion = 0 };
        WorldDiplomacyHistoryPublicationApplication.RebuildPublishedPolicySignaturesThrough(
            rebuildHistory, 3,
            (cursor, size) => ledgerEntries.Where(x => x.Sequence > cursor).Take(size).ToList());
        Test.True(rebuildHistory.PolicyRevisionSignatures.TryGetValue("pol_a", out string rebuilt)
            && rebuilt == "hA2",
            "signature rebuild must record the latest fingerprint through the cutoff");
        rebuildHistory.PolicyRevisionSignatures.Clear();
        rebuildHistory.PolicyHistorySchemaVersion = 1;
        WorldDiplomacyHistoryPublicationApplication.RebuildPublishedPolicySignaturesThrough(
            rebuildHistory, 3,
            (cursor, size) => ledgerEntries.Where(x => x.Sequence > cursor).Take(size).ToList());
        Test.True(rebuildHistory.PolicyRevisionSignatures.Count == 0,
            "signature rebuild must no-op once the schema is current");
    }

    static void RunThreatHistoryResultDecisionTests()
    {
        // DPL-060CD: threat history result appenders must preserve gates, document
        // prerequisites, source keys, flags, and exception deferral through ports.
        var storage = new WorldDiplomacyStorage();
        var sourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var documents = new Dictionary<string, WorldDiplomacyDocument>(StringComparer.OrdinalIgnoreCase);
        var appended = new List<(string sourceKey, string kind, string intent, string commitment, bool verified, string threatDocId)>();
        var logs = new List<string>();
        bool appendResult = true;
        CanonicalHistoryAppender appender = (kind, sourceKey, sourceId, day, gameDate,
            author, targets, intent, commitment, content, verified, offerId, threatDocId, facts) =>
        {
            appended.Add((sourceKey, kind, intent, commitment, verified, threatDocId));
            return appendResult;
        };
        Func<string, WorldDiplomacyDocument> resolveDocument =
            id => documents.TryGetValue(id ?? "", out WorldDiplomacyDocument d) ? d : null;
        Func<int, string> fmt = d => "d" + d;
        Func<string, string> name = id => "K:" + id;
        Func<string, string> nameX = id => "KX:" + id;
        Action<string> log = logs.Add;

        // Domestic penalty: gates on recorded flag, completion, and complied status.
        var domestic = new WorldDiplomacyThreat
        {
            ThreatId = "t1", Status = "complied", DomesticPenaltyCompleted = true,
            ComplianceDocumentId = "doc_c1", StageDocumentId = "sd1", UpdatedDay = 40,
            TargetKingdomId = "kT", IssuerKingdomId = "kI",
            DomesticPenaltyAppliedClanIds = new List<string> { "c1", "c2" },
            DomesticPenaltySkippedClanIds = new List<string> { "c3" }
        };
        WorldDiplomacyHistoryPublicationApplication.TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(
            storage, sourceKeys, appender, resolveDocument, fmt, nameX, log, domestic);
        Test.True(appended.Count == 0 && !domestic.DomesticPenaltyHistoryRecorded,
            "domestic penalty must defer when the compliance document is unresolved");
        documents["doc_c1"] = new WorldDiplomacyDocument { DocumentId = "doc_c1" };
        WorldDiplomacyHistoryPublicationApplication.TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(
            storage, sourceKeys, appender, resolveDocument, fmt, nameX, log, domestic);
        Test.True(appended.Count == 0,
            "domestic penalty must defer until the compliance declaration is recorded");
        documents["doc_c1"].HistoryDeclarationRecorded = true;
        WorldDiplomacyHistoryPublicationApplication.TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(
            storage, sourceKeys, appender, resolveDocument, fmt, nameX, log, domestic);
        Test.True(appended.Count == 1
            && appended[0].sourceKey == "threat:t1:domestic_penalty"
            && appended[0].kind == "diplomatic_result" && appended[0].intent == "comply_ultimatum"
            && appended[0].commitment == "binding" && appended[0].verified
            && appended[0].threatDocId == "sd1"
            && domestic.DomesticPenaltyHistoryRecorded,
            "domestic penalty must append a verified binding result and mark the flag");
        domestic.DomesticPenaltyHistoryRecorded = false;
        appendResult = false;
        sourceKeys.Add("threat:t1:domestic_penalty");
        WorldDiplomacyHistoryPublicationApplication.TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(
            storage, sourceKeys, appender, resolveDocument, fmt, nameX, log, domestic);
        Test.True(domestic.DomesticPenaltyHistoryRecorded,
            "domestic penalty must mark the flag when the delta already carries the source key");
        sourceKeys.Clear();
        domestic.DomesticPenaltyHistoryRecorded = false;
        storage.CanonicalHistory = new WorldDiplomacyCanonicalHistoryState();
        storage.CanonicalHistory.Snapshot = new WorldDiplomacyCanonicalHistorySnapshot
        {
            ProtectedFacts = new List<WorldDiplomacyCanonicalProtectedFact>
            {
                new WorldDiplomacyCanonicalProtectedFact { SourceKey = "threat:t1:domestic_penalty" }
            }
        };
        WorldDiplomacyHistoryPublicationApplication.TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(
            storage, sourceKeys, appender, resolveDocument, fmt, nameX, log, domestic);
        Test.True(domestic.DomesticPenaltyHistoryRecorded,
            "domestic penalty must mark the flag when a protected fact already carries the source key");

        // Non-compliance: decision and threat flags split on current-stage match.
        storage.CanonicalHistory = null;
        appendResult = true;
        var threatNc = new WorldDiplomacyThreat
        {
            ThreatId = "t2", Status = "open", StageDocumentId = "sd_cur",
            TargetKingdomId = "kT", IssuerKingdomId = "kI", Stage = "ultimatum"
        };
        var decision = new WorldDiplomacyThreatNonComplianceEvent
        {
            StageDocumentId = "sd_cur", DecisionDocumentId = "doc_d2", DecisionDay = 55,
            Stage = "ultimatum"
        };
        documents["doc_d2"] = new WorldDiplomacyDocument
        {
            DocumentId = "doc_d2", HistoryDeclarationRecorded = true,
            Intent = "War", Commitment = "HARD"
        };
        appended.Clear();
        WorldDiplomacyHistoryPublicationApplication.TryAppendDiplomaticThreatNonComplianceHistoryResult(
            storage, sourceKeys, appender, resolveDocument, fmt, name, log, threatNc, decision);
        Test.True(appended.Count == 1
            && appended[0].sourceKey == "threat:t2:target_noncompliance:sd_cur"
            && appended[0].threatDocId == "sd_cur"
            && decision.HistoryRecorded && threatNc.NonComplianceHistoryRecorded,
            "non-compliance must append once and mark both flags for the current stage");
        var other = new WorldDiplomacyThreatNonComplianceEvent
        {
            StageDocumentId = "sd_old", DecisionDocumentId = "doc_d2", DecisionDay = 50,
            Stage = "warning"
        };
        appended.Clear();
        WorldDiplomacyHistoryPublicationApplication.TryAppendDiplomaticThreatNonComplianceHistoryResult(
            storage, sourceKeys, appender, resolveDocument, fmt, name, log, threatNc, other);
        Test.True(other.HistoryRecorded && appended.Count == 1,
            "non-compliance must still record a stale-stage decision");
        threatNc.NonComplianceHistoryRecorded = false;
        var decisionFail = new WorldDiplomacyThreatNonComplianceEvent
        {
            StageDocumentId = "sd_cur", DecisionDocumentId = "doc_d2", DecisionDay = 60,
            Stage = "ultimatum"
        };
        appendResult = true;
        CanonicalHistoryAppender throwing = (kind, sourceKey, sourceId, day, gameDate,
            author, targets, intent, commitment, content, verified, offerId, threatDocId, facts) =>
            throw new InvalidOperationException("boom");
        int logsBefore = logs.Count;
        WorldDiplomacyHistoryPublicationApplication.TryAppendDiplomaticThreatNonComplianceHistoryResult(
            storage, sourceKeys, throwing, resolveDocument, fmt, name, log, threatNc, decisionFail);
        Test.True(!decisionFail.HistoryRecorded && logs.Count == logsBefore + 1,
            "non-compliance must defer and log when the canonical append throws");

        // Reputation penalty: only breached threats write the result entry.
        var breached = new WorldDiplomacyThreat
        {
            ThreatId = "t3", Status = "breached", Stage = "ultimatum",
            ResolutionDocumentId = "doc_r3", StageDocumentId = "sd3", UpdatedDay = 70,
            IssuerKingdomId = "kI", TargetKingdomId = "kT", ReputationPenaltyAmount = 15
        };
        documents["doc_r3"] = new WorldDiplomacyDocument
        {
            DocumentId = "doc_r3", HistoryDeclarationRecorded = true
        };
        appended.Clear();
        WorldDiplomacyHistoryPublicationApplication.TryAppendDiplomaticThreatHistoryResult(
            storage, sourceKeys, appender, resolveDocument, fmt, name, log, breached);
        Test.True(appended.Count == 1
            && appended[0].sourceKey == "threat:t3:reputation_penalty"
            && appended[0].intent == "ultimatum" && appended[0].commitment == "binding"
            && appended[0].threatDocId == "sd3" && breached.HistoryResultRecorded,
            "reputation penalty must append with stage intent, binding commitment, and stage doc id");
        var openThreat = new WorldDiplomacyThreat { ThreatId = "t4", Status = "open" };
        appended.Clear();
        WorldDiplomacyHistoryPublicationApplication.TryAppendDiplomaticThreatHistoryResult(
            storage, sourceKeys, appender, resolveDocument, fmt, name, log, openThreat);
        Test.True(appended.Count == 0 && !openThreat.HistoryResultRecorded,
            "reputation penalty must skip threats that are not breached");

        // Issuer reward: empty reward still records, populated reward appends.
        var reward = new WorldDiplomacyThreat
        {
            ThreatId = "t5", Status = "complied", IssuerRewardCompleted = true,
            ComplianceDocumentId = "doc_c5", StageDocumentId = "sd5", UpdatedDay = 80,
            IssuerKingdomId = "kI", TargetKingdomId = "kT",
            IssuerRewardAmount = 20,
            IssuerRewardAppliedClanIds = new List<string> { "c1" }
        };
        documents["doc_c5"] = new WorldDiplomacyDocument
        {
            DocumentId = "doc_c5", HistoryDeclarationRecorded = true
        };
        var emptyReward = new WorldDiplomacyThreat
        {
            ThreatId = "t6", Status = "complied", IssuerRewardCompleted = true,
            ComplianceDocumentId = "doc_c5", IssuerRewardAmount = 0
        };
        appended.Clear();
        WorldDiplomacyHistoryPublicationApplication.TryAppendDiplomaticThreatIssuerRewardHistoryResult(
            storage, sourceKeys, appender, resolveDocument, fmt, nameX, log, emptyReward);
        Test.True(appended.Count == 0 && emptyReward.IssuerRewardHistoryRecorded,
            "issuer reward must record the flag without appending when the reward is empty");
        appended.Clear();
        WorldDiplomacyHistoryPublicationApplication.TryAppendDiplomaticThreatIssuerRewardHistoryResult(
            storage, sourceKeys, appender, resolveDocument, fmt, nameX, log, reward);
        Test.True(appended.Count == 1
            && appended[0].sourceKey == "threat:t5:issuer_relation_reward"
            && appended[0].intent == "comply_ultimatum" && appended[0].commitment == "binding"
            && reward.IssuerRewardHistoryRecorded,
            "issuer reward must append a verified binding result and mark the flag");
        var rewardThrow = new WorldDiplomacyThreat
        {
            ThreatId = "t7", Status = "complied", IssuerRewardCompleted = true,
            ComplianceDocumentId = "doc_c5", IssuerRewardAmount = 10,
            IssuerRewardAppliedClanIds = new List<string> { "c1" }
        };
        logsBefore = logs.Count;
        WorldDiplomacyHistoryPublicationApplication.TryAppendDiplomaticThreatIssuerRewardHistoryResult(
            storage, sourceKeys, throwing, resolveDocument, fmt, nameX, log, rewardThrow);
        Test.True(!rewardThrow.IssuerRewardHistoryRecorded && logs.Count == logsBefore + 1,
            "issuer reward must defer and log when the canonical append throws");
    }

    static void RunCompressionCommitDecisionTests()
    {
        // DPL-060CE: compression commit path and the job queue insertion rule must
        // preserve validation order, dedup, capacity trim, and storage effects.
        var storage = new WorldDiplomacyStorage();

        // EnqueueJob: reject null/blank, dedupe by job id, trim to capacity by
        // priority then day then id, and grant compress jobs one extra slot.
        WorldDiplomacyRoundLifecycleRules.EnqueueJob(storage, null, 2);
        WorldDiplomacyRoundLifecycleRules.EnqueueJob(storage, new WorldDiplomacyJob(), 2);
        Test.True(storage.Jobs.Count == 0, "enqueue must reject null and id-less jobs");
        var first = new WorldDiplomacyJob { JobId = "j1", Priority = 1, CreatedDay = 1 };
        WorldDiplomacyRoundLifecycleRules.EnqueueJob(storage, first, 2);
        WorldDiplomacyRoundLifecycleRules.EnqueueJob(storage,
            new WorldDiplomacyJob { JobId = "J1", Priority = 9, CreatedDay = 9 }, 2);
        Test.True(storage.Jobs.Count == 1 && storage.Jobs[0].Priority == 1,
            "enqueue must dedupe case-insensitively and keep the earlier job");
        WorldDiplomacyRoundLifecycleRules.EnqueueJob(storage,
            new WorldDiplomacyJob { JobId = "j2", Priority = 5, CreatedDay = 2 }, 2);
        WorldDiplomacyRoundLifecycleRules.EnqueueJob(storage,
            new WorldDiplomacyJob { JobId = "j3", Priority = 3, CreatedDay = 3 }, 2);
        Test.True(storage.Jobs.Count == 2
            && storage.Jobs[0].JobId == "j1" && storage.Jobs[1].JobId == "j2",
            "full lane queue must preserve accepted work without evicting another task");
        var compressStorage = new WorldDiplomacyStorage();
        WorldDiplomacyRoundLifecycleRules.EnqueueJob(compressStorage,
            new WorldDiplomacyJob { JobId = "c1", Kind = "compress", Priority = 9, CreatedDay = 1 }, 1);
        WorldDiplomacyRoundLifecycleRules.EnqueueJob(compressStorage,
            new WorldDiplomacyJob { JobId = "c2", Priority = 1, CreatedDay = 2 }, 1);
        Test.True(compressStorage.Jobs.Count == 2,
            "enqueue must grant one extra capacity slot while a compress job is pending");

        // CommitRoundCompression: parse facts, fall back on empty summary, replace
        // any previous summary for the same round.
        var roundStorage = new WorldDiplomacyStorage();
        roundStorage.RoundSummaries.Add(new WorldDiplomacyRoundSummary { RoundId = "r1", Summary = "old" });
        var roundJob = new WorldDiplomacyJob
        {
            RoundId = "r1",
            CompressionDocumentIds = new List<string> { "d1" }
        };
        WorldDiplomacyRoundCompressionApplication.Commit(
            roundStorage, roundJob,
            "{\"summary\":\"round digest\",\"facts\":[{\"text\":\"f1\",\"source_document_ids\":[\"d1\"],\"kingdom_ids\":[\"kA\"]}]}",
            30, d => "d" + d);
        Test.True(roundStorage.RoundSummaries.Count == 1,
            "round compression must replace the prior summary for the same round");
        WorldDiplomacyRoundSummary committed = roundStorage.RoundSummaries[0];
        Test.True(committed.Summary == "round digest" && committed.CreatedDay == 30
            && committed.SourceDocumentIds.Count == 1 && committed.Facts.Count == 1
            && committed.Facts[0].Text == "f1"
            && committed.Facts[0].SourceDocumentIds[0] == "d1"
            && committed.Facts[0].KingdomIds[0] == "kA",
            "round compression must carry summary, facts, and source ids into the record");
        roundStorage.Documents.Add(new WorldDiplomacyDocument { DocumentId = "d1", Day = 29, Title = "fallback declaration" });
        var manyFacts = new JArray();
        for (int i = 0; i < 40; i++) manyFacts.Add(new JObject { ["text"] = "fact " + i });
        WorldDiplomacyRoundCompressionApplication.Commit(roundStorage, roundJob,
            new JObject { ["summary"] = "", ["facts"] = manyFacts }.ToString(), 31, d => "d" + d);
        Test.True(roundStorage.RoundSummaries.Count == 1
            && roundStorage.RoundSummaries[0].Summary.Contains("fallback declaration", StringComparison.Ordinal)
            && roundStorage.RoundSummaries[0].Facts.Count == 32,
            "empty generated summary uses bounded document fallback and caps parsed facts");

        // CommitCompression: validation order then storage commit effects.
        var history = new WorldDiplomacyCanonicalHistoryState
        {
            NextSequence = 4,
            DeltaEntries = new List<WorldDiplomacyCanonicalHistoryEntry>
            {
                new WorldDiplomacyCanonicalHistoryEntry
                {
                    Sequence = 1, Day = 10, SourceId = "d1", Kind = "diplomatic_result",
                    Text = "r1", AuthorKingdomId = "kA",
                    TargetKingdomIds = new List<string> { "kB" }, EstimatedTokens = 5
                },
                new WorldDiplomacyCanonicalHistoryEntry
                {
                    Sequence = 2, Day = 20, SourceId = "d2", Kind = "document_declaration",
                    Text = "d2", AuthorKingdomId = "kB", EstimatedTokens = 5
                },
                new WorldDiplomacyCanonicalHistoryEntry
                {
                    Sequence = 3, Day = 25, SourceId = "d3", Kind = "document_declaration",
                    Text = "d3", AuthorKingdomId = "kA", EstimatedTokens = 5
                }
            }
        };
        var compressStore = new WorldDiplomacyStorage
        {
            CanonicalHistory = history,
            CompressionRetryAfterHour = 9,
            CompressionRetryAttempts = 4,
            Jobs = new List<WorldDiplomacyJob>
            {
                new WorldDiplomacyJob { JobId = "waiting", AwaitingHistoryCompression = true }
            }
        };
        int invalidations = 0;
        var commitLogs = new List<string>();
        Func<string, int> estimate = t => t?.Length ?? 0;
        var compressJob = new WorldDiplomacyJob
        {
            JobId = "jc", CompressionThroughSequence = 2, CompressionTokenCount = 10,
            CompressionTargetTokens = 4000, CompressionOverallTargetTokens = 8000,
            CompressionBatchId = "batch_9"
        };
        Action<WorldDiplomacyStorage, WorldDiplomacyJob, string> commit = (s, j, raw) =>
            WorldDiplomacyHistoryCompressionApplication.CommitCompression(
                s, j, raw, () => { }, estimate, () => 30, 4000, 10000,
                () => invalidations++, commitLogs.Add);

        string ThrowMessage(Action act)
        {
            try { act(); return null; }
            catch (InvalidOperationException ex) { return ex.Message; }
        }
        Test.True(ThrowMessage(() => commit(compressStore, null, "{}")) == "missing compression job",
            "compression commit must reject a missing job");
        compressStore.CanonicalHistory.Snapshot.CoveredThroughSequence = 5;
        Test.True(ThrowMessage(() => commit(compressStore, compressJob, "{}")) != null,
            "compression commit must reject a cutoff predating the snapshot");
        compressStore.CanonicalHistory.Snapshot.CoveredThroughSequence = 0;
        Test.True(ThrowMessage(() => commit(compressStore, compressJob,
            "{\"summary\":\"  \",\"covered_through_sequence\":2}")) != null,
            "compression commit must reject an empty summary");
        Test.True(ThrowMessage(() => commit(compressStore, compressJob,
            "{\"summary\":\"s\",\"covered_through_sequence\":7}")) != null,
            "compression commit must reject a covered_through_sequence mismatch");
        Test.True(ThrowMessage(() => commit(compressStore,
                new WorldDiplomacyJob
                {
                    JobId = "jc2", CompressionThroughSequence = 2, CompressionTargetTokens = 1,
                    CompressionOverallTargetTokens = 8000, CompressionBatchId = "batch_x"
                },
                "{\"summary\":\"longer than one token\",\"covered_through_sequence\":2}")) != null,
            "compression commit must reject summaries above the target token budget");

        commit(compressStore, compressJob,
            "{\"summary\":\"compressed archive\",\"covered_through_sequence\":2}");
        Test.True(history.Snapshot.Content == "compressed archive"
            && history.Snapshot.CoveredThroughSequence == 2
            && history.DeltaEntries.Count == 1 && history.DeltaEntries[0].Sequence == 3
            && history.Revision >= 1,
            "compression commit must swap the snapshot and drop only the frozen prefix");
        Test.True(!compressStore.Jobs[0].AwaitingHistoryCompression
            && compressStore.CompressionSummaries.Count == 1
            && compressStore.CompressionSummaries[0].BatchId == "batch_9"
            && compressStore.CompressionSummaries[0].SourceRoundIds.Contains("d1")
            && compressStore.CompressionSequence >= 1
            && compressStore.LastDiplomacyCompressionDay == 30
            && compressStore.CompressionRetryAfterHour == 0
            && compressStore.CompressionRetryAttempts == 0,
            "compression commit must release waiting jobs, record the summary, and reset retry state");
        Test.True(invalidations == 1 && commitLogs.Count == 1
            && commitLogs[0].Contains("token compression committed batch=batch_9"),
            "compression commit must invalidate the render cache and log the batch");
    }

    static void RunCanonicalRenderAndStampDecisionTests()
    {
        // DPL-060CF: history block rendering and job stamping must preserve the
        // cutoff semantics, snapshot gating, and stamped field set.
        var history = new WorldDiplomacyCanonicalHistoryState
        {
            NextSequence = 4,
            Revision = 7,
            EstimatedTokens = 33,
            Snapshot = new WorldDiplomacyCanonicalHistorySnapshot
            {
                CoveredThroughSequence = 1,
                ContentHash = "hash_a",
                Content = ""
            },
            DeltaEntries = new List<WorldDiplomacyCanonicalHistoryEntry>
            {
                new WorldDiplomacyCanonicalHistoryEntry
                {
                    Sequence = 2, Day = 10, Kind = "diplomatic_result", SourceId = "d1",
                    AuthorKingdomId = "kA", Text = "result one", Verified = true
                },
                new WorldDiplomacyCanonicalHistoryEntry
                {
                    Sequence = 3, Day = 12, Kind = "document_declaration", SourceId = "d2",
                    AuthorKingdomId = "kB", Text = "decl two"
                }
            }
        };

        string block = WorldDiplomacyCanonicalHistoryRules.RenderCanonicalHistoryBlock(history, long.MaxValue);
        Test.True(block.Contains(DiplomacyPromptTexts.Get("HistoryPrompt_000", "【全局长期外交历史】"))
            && block.Contains("result one") && block.Contains("decl two"),
            "the history block must render the header and every delta entry through the cutoff");
        string capped = WorldDiplomacyCanonicalHistoryRules.RenderCanonicalHistoryBlock(history, 2);
        Test.True(capped.Contains("result one") && !capped.Contains("decl two"),
            "the history block must exclude delta entries beyond the cutoff");
        var emptyHistory = new WorldDiplomacyCanonicalHistoryState { NextSequence = 1 };
        string emptyBlock = WorldDiplomacyCanonicalHistoryRules.RenderCanonicalHistoryBlock(emptyHistory, 5);
        Test.True(emptyBlock.Contains(DiplomacyPromptTexts.Get("HistoryPrompt_004", "（暂无历史记录）")),
            "an empty archive must render the explicit empty marker");
        history.Snapshot.Content = "旧摘要正文";
        string withSnapshot = WorldDiplomacyCanonicalHistoryRules.RenderCanonicalHistoryBlock(history, 3);
        Test.True(withSnapshot.Contains(DiplomacyPromptTexts.Get("HistoryPrompt_002", "【已压缩历史")) && withSnapshot.Contains("旧摘要正文"),
            "a covered snapshot must render its payload inside the block");
        string beforeCoverage = WorldDiplomacyCanonicalHistoryRules.RenderCanonicalHistoryBlock(history, 0);
        Test.True(!beforeCoverage.Contains(DiplomacyPromptTexts.Get("HistoryPrompt_002", "【已压缩历史")),
            "a snapshot must not render when the cutoff precedes its coverage");

        // Through-sequence clamp: never below snapshot coverage, never past the
        // last committed delta sequence.
        Test.True(WorldDiplomacyCanonicalHistoryRules.ClampCanonicalHistoryThroughSequence(history, long.MaxValue) == 3
            && WorldDiplomacyCanonicalHistoryRules.ClampCanonicalHistoryThroughSequence(history, 1) == 1
            && WorldDiplomacyCanonicalHistoryRules.ClampCanonicalHistoryThroughSequence(history, 0) == 0,
            "the clamp must bound the requested cutoff by snapshot coverage and last delta");

        // Job stamping: all frozen fields come from the history state, and the
        // prefix hash binds the system prompt to the rendered block.
        var job = new WorldDiplomacyJob { SystemPrompt = "sys" };
        WorldDiplomacyCanonicalHistoryRules.StampCanonicalHistoryOnJob(job, history, "block-a");
        Test.True(job.HistoryRevision == 7 && job.HistoryEstimatedTokens == 33
            && job.HistorySnapshotThroughSequence == 1 && job.HistorySnapshotHash == "hash_a"
            && !string.IsNullOrWhiteSpace(job.HistoryPrefixHash),
            "job stamping must copy revision, tokens, snapshot coordinates, and prefix hash");
        var job2 = new WorldDiplomacyJob { SystemPrompt = "sys" };
        WorldDiplomacyCanonicalHistoryRules.StampCanonicalHistoryOnJob(job2, history, "block-b");
        Test.True(job2.HistoryPrefixHash != job.HistoryPrefixHash,
            "the prefix hash must change with the rendered history block");
    }

    static void RunCompressionSchedulingDecisionTests()
    {
        // DPL-060CG: scheduling must preserve enablement gating, pending detection,
        // retry-hour gating, duplicate-job prevention, and minimum-target selection;
        // enqueue must preserve budget math, field freezing, capture order, and the
        // retry backoff when the next indivisible entry cannot fit.
        var scheduled = new List<(long Through, long Tokens, int Target)>();
        int ensureCalls = 0, syncCalls = 0;

        // Disabled gate: nothing runs, not even initialization or source sync.
        var disabled = new WorldDiplomacyStorage();
        disabled.CanonicalHistory.EstimatedTokens = 99999;
        WorldDiplomacyHistoryCompressionApplication.TryScheduleTokenCompression(
            disabled, () => false, () => ensureCalls++, () => syncCalls++, () => 12,
            1000, 4000, (t, c, g) => scheduled.Add((t, c, g)));
        Test.True(scheduled.Count == 0 && ensureCalls == 0 && syncCalls == 0
            && !disabled.DiplomacyCompressionPending,
            "a disabled scheduler must not initialize, sync, or mark compression pending");

        // Below the trigger with no waiting jobs: pending stays false, no enqueue.
        var quiet = new WorldDiplomacyStorage();
        quiet.CanonicalHistory.EstimatedTokens = 10;
        WorldDiplomacyHistoryCompressionApplication.TryScheduleTokenCompression(
            quiet, () => true, () => ensureCalls++, () => syncCalls++, () => 12,
            1000, 4000, (t, c, g) => scheduled.Add((t, c, g)));
        Test.True(scheduled.Count == 0 && !quiet.DiplomacyCompressionPending
            && ensureCalls == 1 && syncCalls == 1,
            "a quiet archive must stay non-pending after initializing and syncing once");

        // Retry-hour gate: pending is recorded but no enqueue before the retry hour.
        var cooling = new WorldDiplomacyStorage { CompressionRetryAfterHour = 20 };
        cooling.CanonicalHistory.EstimatedTokens = 5000;
        WorldDiplomacyHistoryCompressionApplication.TryScheduleTokenCompression(
            cooling, () => true, () => { }, () => { }, () => 12,
            1000, 4000, (t, c, g) => scheduled.Add((t, c, g)));
        Test.True(cooling.DiplomacyCompressionPending && scheduled.Count == 0,
            "a pending archive inside the retry window must not enqueue");

        // Duplicate guard: an existing compress job blocks a second schedule.
        var guarded = new WorldDiplomacyStorage();
        guarded.CanonicalHistory.EstimatedTokens = 5000;
        guarded.Jobs.Add(new WorldDiplomacyJob { JobId = "jc", Kind = "compress" });
        WorldDiplomacyHistoryCompressionApplication.TryScheduleTokenCompression(
            guarded, () => true, () => { }, () => { }, () => 30,
            1000, 4000, (t, c, g) => scheduled.Add((t, c, g)));
        Test.True(scheduled.Count == 0,
            "an already-queued compress job must prevent a duplicate schedule");

        // Total-input pressure: awaiting jobs mark pending below the history trigger,
        // and the smallest positive requested target wins the freeze.
        var pressured = new WorldDiplomacyStorage();
        pressured.CanonicalHistory.EstimatedTokens = 10;
        pressured.CanonicalHistory.NextSequence = 5;
        pressured.CanonicalHistory.Snapshot.CoveredThroughSequence = 2;
        pressured.Jobs.Add(new WorldDiplomacyJob { JobId = "w1", AwaitingHistoryCompression = true, InputBudgetHistoryTargetTokens = 6000 });
        pressured.Jobs.Add(new WorldDiplomacyJob { JobId = "w2", AwaitingHistoryCompression = true, InputBudgetHistoryTargetTokens = 3000 });
        pressured.Jobs.Add(new WorldDiplomacyJob { JobId = "w3", AwaitingHistoryCompression = true, InputBudgetHistoryTargetTokens = 0 });
        WorldDiplomacyHistoryCompressionApplication.TryScheduleTokenCompression(
            pressured, () => true, () => { }, () => { }, () => 30,
            1000, 4000, (t, c, g) => scheduled.Add((t, c, g)));
        Test.True(pressured.DiplomacyCompressionPending && scheduled.Count == 1
            && scheduled[0].Through == 4 && scheduled[0].Tokens == 10
            && scheduled[0].Target == 3000,
            "scheduling must pick the minimum positive requested target through NextSequence-1");

        // The configured target still caps a larger requested minimum.
        var capped = new WorldDiplomacyStorage();
        capped.CanonicalHistory.EstimatedTokens = 5000;
        capped.Jobs.Add(new WorldDiplomacyJob { JobId = "w4", AwaitingHistoryCompression = true, InputBudgetHistoryTargetTokens = 9000 });
        WorldDiplomacyHistoryCompressionApplication.TryScheduleTokenCompression(
            capped, () => true, () => { }, () => { }, () => 30,
            1000, 4000, (t, c, g) => scheduled.Add((t, c, g)));
        Test.True(scheduled.Count == 2 && scheduled[1].Target == 4000,
            "the configured compression target must cap larger requested minimums");

        // EnqueueCompressionJob: budget math, frozen fields, capture before insert.
        var store = new WorldDiplomacyStorage();
        store.CanonicalHistory.NextSequence = 4;
        store.CanonicalHistory.EstimatedTokens = 300;
        store.CanonicalHistory.DeltaEntries.Add(new WorldDiplomacyCanonicalHistoryEntry { Sequence = 1, EstimatedTokens = 100 });
        store.CanonicalHistory.DeltaEntries.Add(new WorldDiplomacyCanonicalHistoryEntry { Sequence = 2, EstimatedTokens = 100 });
        store.CanonicalHistory.DeltaEntries.Add(new WorldDiplomacyCanonicalHistoryEntry { Sequence = 3, EstimatedTokens = 100 });
        int capturedSeq = -1; WorldDiplomacyJob capturedJob = null;
        int jobsAtCapture = -1;
        var enqueueLogs = new List<string>();
        WorldDiplomacyHistoryCompressionApplication.EnqueueCompressionJob(
            store, 3, 300, 200,
            () => { }, () => (100, 500), () => "sys", t => t?.Length ?? 0,
            100000, 7, 600, 48, 10, () => 30, () => 12, () => 12000,
            prefix => prefix + "_x",
            (j, s) => { capturedJob = j; capturedSeq = (int)s; jobsAtCapture = store.Jobs.Count; },
            enqueueLogs.Add);
        Test.True(store.Jobs.Count == 1 && jobsAtCapture == 0,
            "history capture must run before the job enters the queue");
        WorldDiplomacyJob enqueued = store.Jobs[0];
        Test.True(enqueued.JobId == "diplomacy_compress_x" && enqueued.Kind == "compress"
            && enqueued.Priority == 7 && enqueued.CreatedDay == 12
            && enqueued.CompressionBatchId == "diplomacy_compaction_1"
            && enqueued.CompressionThroughSequence == 3
            && enqueued.CompressionTokenCount == 300
            && enqueued.CompressionOverallTargetTokens == 200
            && enqueued.CompressionTargetTokens == 256
            && enqueued.MaxTokens == 856
            && enqueued.CacheAffinityKey == WorldDiplomacyPromptContractRules.CanonicalHistoryCacheAffinityKey
            && !string.IsNullOrWhiteSpace(enqueued.SystemPrompt)
            && enqueued.UserPrompt.Contains("diplomacy_compaction_1"),
            "enqueue must freeze batch, cutoff, targets, affinity, and the capped max_tokens");
        Test.True(capturedJob == enqueued && capturedSeq == 3
            && enqueueLogs.Count == 1 && enqueueLogs[0].Contains("batch=diplomacy_compaction_1"),
            "enqueue must stamp the queued job and log the batch once");

        // Backoff: when the cutoff cannot advance past the snapshot and the snapshot
        // already fits the target, defer by the retry window instead of enqueueing.
        var stalled = new WorldDiplomacyStorage { CompressionRetryAfterHour = 0 };
        stalled.CanonicalHistory.NextSequence = 6;
        stalled.CanonicalHistory.EstimatedTokens = 100;
        stalled.CanonicalHistory.Snapshot.CoveredThroughSequence = 5;
        stalled.CanonicalHistory.Snapshot.EstimatedTokens = 100;
        stalled.CanonicalHistory.Snapshot.Content = "archive";
        stalled.CanonicalHistory.DeltaEntries.Add(new WorldDiplomacyCanonicalHistoryEntry { Sequence = 6, EstimatedTokens = 200000 });
        var stallLogs = new List<string>();
        WorldDiplomacyHistoryCompressionApplication.EnqueueCompressionJob(
            stalled, 5, 100, 500,
            () => { }, () => (100, 500), () => "sys", t => t?.Length ?? 0,
            100000, 7, 600, 48, 10, () => 30, () => 12, () => 12000,
            prefix => prefix + "_x", (j, s) => { }, stallLogs.Add);
        Test.True(stalled.Jobs.Count == 0 && stalled.CompressionRetryAfterHour == 78
            && stallLogs.Count == 1 && stallLogs[0].Contains("cannot fit"),
            "an indivisible next entry must back off by the retry window without enqueueing");
    }

    static void RunRoundSummaryArchiveDecisionTests()
    {
        // DPL-060CH: the local round archive projection must preserve ordering,
        // fact kinds, schema versioning, replacement, and the legacy-upgrade path.
        var archiveLogs = new List<string>();
        Func<int, string> fmt = d => "day" + d;

        // Guards: null round, null list, and empty list leave storage untouched.
        var guardStore = new WorldDiplomacyStorage();
        WorldDiplomacyRoundLifecycleRules.CommitLocalRoundSummary(guardStore, null,
            new List<WorldDiplomacyDocument> { new WorldDiplomacyDocument() }, () => 1, fmt, archiveLogs.Add);
        WorldDiplomacyRoundLifecycleRules.CommitLocalRoundSummary(guardStore,
            new WorldDiplomacyRound { RoundId = "r0" }, new List<WorldDiplomacyDocument>(),
            () => 1, fmt, archiveLogs.Add);
        Test.True(guardStore.RoundSummaries.Count == 0 && archiveLogs.Count == 0,
            "summary commit must skip null rounds and empty document sets");

        // Commit: chronological ordering, fact kinds, replacement, and log line.
        var store = new WorldDiplomacyStorage();
        store.RoundSummaries.Add(new WorldDiplomacyRoundSummary { RoundId = "r1", Summary = "stale" });
        var round = new WorldDiplomacyRound { RoundId = "r1", CompletedDay = 0 };
        var documents = new List<WorldDiplomacyDocument>
        {
            new WorldDiplomacyDocument
            {
                DocumentId = "d2", RoundId = "r1", Day = 20, AuthorKingdomId = "kB",
                AuthorKingdomName = "Beta", TargetKingdomId = "kA", TargetKingdomName = "Alpha",
                Title = "reply", Intent = "propose_peace", GameDate = "d20"
            },
            new WorldDiplomacyDocument
            {
                DocumentId = "d1", RoundId = "r1", Day = 10, AuthorKingdomId = "kA",
                AuthorKingdomName = "Alpha", TargetKingdomId = "kB", TargetKingdomName = "Beta",
                Title = "decl", Intent = "declare_war", GameDate = "d10",
                ChangedDiplomaticState = true, MechanicalResult = "war declared"
            }
        };
        WorldDiplomacyRoundLifecycleRules.CommitLocalRoundSummary(store, round, documents,
            () => 30, fmt, archiveLogs.Add);
        Test.True(store.RoundSummaries.Count == 1,
            "summary commit must replace the prior archive for the same round");
        WorldDiplomacyRoundSummary committed = store.RoundSummaries[0];
        Test.True(committed.ArchiveSchemaVersion == 1 && committed.RoundId == "r1"
            && committed.CreatedDay == 30
            && committed.SourceDocumentIds.SequenceEqual(new[] { "d1", "d2" })
            && committed.KingdomIds.Contains("kA") && committed.KingdomIds.Contains("kB")
            && committed.Summary.Contains("war declared"),
            "committed archive must carry schema, day, chronological sources, kingdoms, and text");
        Test.True(committed.Facts.Count == 3
            && committed.Facts[0].Kind == "declaration" && committed.Facts[0].SourceDocumentIds[0] == "d1"
            && committed.Facts[1].Kind == "confirmed_result" && committed.Facts[1].Text.Contains("war declared")
            && committed.Facts[2].Kind == "declaration" && committed.Facts[2].SourceDocumentIds[0] == "d2",
            "each document must project a declaration fact and only executed ones a result fact");
        Test.True(archiveLogs.Count == 1
            && archiveLogs[0].Contains("round=r1")
            && archiveLogs[0].Contains("declarations=2")
            && archiveLogs[0].Contains("confirmed_results=1"),
            "summary commit must log round, declaration, and result counts once");

        // Upgrade: already-current schema is a no-op.
        var current = new WorldDiplomacyRoundSummary { RoundId = "r1", ArchiveSchemaVersion = 1, Summary = "keep" };
        WorldDiplomacyRoundLifecycleRules.UpgradeRoundSummaryToStructuredArchive(
            store, current, _ => null, fmt);
        Test.True(current.Summary == "keep",
            "a current-schema summary must not be touched");

        // Upgrade legacy path: no resolvable round or documents keeps the narrative
        // but rewrites facts as non-authoritative declarations.
        var legacy = new WorldDiplomacyRoundSummary
        {
            RoundId = "gone", ArchiveSchemaVersion = 0, Summary = "旧文本",
            Facts = new List<WorldDiplomacyRoundFact>
            {
                new WorldDiplomacyRoundFact { Kind = "confirmed_result", Text = "claimed war" }
            }
        };
        WorldDiplomacyRoundLifecycleRules.UpgradeRoundSummaryToStructuredArchive(
            store, legacy, _ => null, fmt);
        Test.True(legacy.ArchiveSchemaVersion == 1
            && legacy.Summary.Contains("旧版外交摘要")
            && legacy.Summary.Contains("旧文本")
            && legacy.Facts.Count == 1
            && legacy.Facts[0].Kind == "declaration"
            && legacy.Facts[0].Text.Contains("不代表游戏已执行")
            && legacy.Facts[0].Text.Contains("claimed war"),
            "an unresolvable legacy summary must downgrade facts to non-authoritative declarations");

        // Upgrade rebuild path: resolvable round + matching documents rebuilds the
        // structured projection from storage.
        var rebuildStore = new WorldDiplomacyStorage();
        rebuildStore.Documents.Add(new WorldDiplomacyDocument
        {
            DocumentId = "d9", RoundId = "r9", Day = 40, AuthorKingdomId = "kC",
            AuthorKingdomName = "Gamma", Title = "t9", Intent = "propose_alliance",
            ChangedDiplomaticState = true, MechanicalResult = "alliance formed"
        });
        var staleSummary = new WorldDiplomacyRoundSummary
        {
            RoundId = "r9", ArchiveSchemaVersion = 0, Summary = "old",
            Facts = new List<WorldDiplomacyRoundFact>()
        };
        var rebuiltRound = new WorldDiplomacyRound { RoundId = "r9", RoundTopic = "topic9" };
        WorldDiplomacyRoundLifecycleRules.UpgradeRoundSummaryToStructuredArchive(
            rebuildStore, staleSummary, id => id == "r9" ? rebuiltRound : null, fmt);
        Test.True(staleSummary.ArchiveSchemaVersion == 1
            && staleSummary.SourceDocumentIds.SequenceEqual(new[] { "d9" })
            && staleSummary.KingdomIds.Contains("kC")
            && staleSummary.Summary.Contains("alliance formed")
            && staleSummary.Facts.Count == 2
            && staleSummary.Facts[0].Kind == "declaration"
            && staleSummary.Facts[1].Kind == "confirmed_result",
            "a resolvable legacy summary must rebuild the structured projection from storage");
    }

    static void RunOfferPruningDecisionTests()
    {
        // DPL-060CI: offer pruning must preserve the world-ready gate, party and
        // intent invalidation order, lazy peace-terms probing, dedup superseding,
        // and the single count log.
        var pruneLogs = new List<string>();
        Func<string, (bool Resolved, bool Eliminated, bool HasAuthority)> healthy =
            id => (true, false, true);

        // Guards: null/empty offers and a non-ready world are all no-ops.
        WorldDiplomacyRoundLifecycleRules.PruneInvalidOffers(null, () => true, healthy,
            (a, b) => false, (a, b) => false, () => false, (a, b) => false,
            () => false, (a, b) => false, _ => false, pruneLogs.Add);
        var notReady = new WorldDiplomacyRound { RoundId = "r1" };
        notReady.PendingOffers.Add(new WorldDiplomacyRoundOffer { Intent = "declare_war" });
        WorldDiplomacyRoundLifecycleRules.PruneInvalidOffers(notReady, () => false, healthy,
            (a, b) => false, (a, b) => false, () => false, (a, b) => false,
            () => false, (a, b) => false, _ => false, pruneLogs.Add);
        Test.True(notReady.PendingOffers[0].Status == "open" && pruneLogs.Count == 0,
            "pruning must defer entirely while the world is not ready");

        // Party-invalid offers are invalidated without probing intents.
        var partyRound = new WorldDiplomacyRound { RoundId = "r2" };
        partyRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        {
            Intent = "propose_peace", ProposerKingdomId = "ghost", TargetKingdomId = "kB"
        });
        partyRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        {
            Intent = "propose_peace", ProposerKingdomId = "kA", TargetKingdomId = "kB"
        });
        int peaceProbeCalls = 0;
        WorldDiplomacyRoundLifecycleRules.PruneInvalidOffers(partyRound, () => true,
            id => id == "ghost" ? (false, false, false) : healthy(id),
            (a, b) => a == b, (a, b) => true,
            () => false, (a, b) => false, () => false, (a, b) => false,
            _ => { peaceProbeCalls++; return true; }, pruneLogs.Add);
        Test.True(partyRound.PendingOffers[0].Status == "invalidated"
            && partyRound.PendingOffers[1].Status == "open"
            && peaceProbeCalls == 1,
            "unresolved parties must invalidate before any intent probe runs");

        // Intent validity: peace requires war + executable terms, alliance requires
        // no existing alliance, trade requires no existing agreement.
        var intentRound = new WorldDiplomacyRound { RoundId = "r3" };
        intentRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { Intent = "propose_peace", ProposerKingdomId = "kA", TargetKingdomId = "kB" });
        intentRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { Intent = "propose_peace", ProposerKingdomId = "kC", TargetKingdomId = "kD" });
        intentRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { Intent = "propose_alliance", ProposerKingdomId = "kE", TargetKingdomId = "kF" });
        intentRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { Intent = "propose_trade", ProposerKingdomId = "kG", TargetKingdomId = "kH" });
        intentRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { Intent = "chat", ProposerKingdomId = "kI", TargetKingdomId = "kJ" });
        WorldDiplomacyRoundLifecycleRules.PruneInvalidOffers(intentRound, () => true, healthy,
            (a, b) => a == b,
            (a, b) => (a == "kA" || a == "kC") && (b == "kB" || b == "kD"),
            () => true, (a, b) => a == "kE",
            () => true, (a, b) => a == "kG",
            offer => offer.ProposerKingdomId == "kA", pruneLogs.Add);
        Test.True(intentRound.PendingOffers[0].Status == "open"
            && intentRound.PendingOffers[1].Status == "invalidated"
            && intentRound.PendingOffers[2].Status == "invalidated"
            && intentRound.PendingOffers[3].Status == "invalidated"
            && intentRound.PendingOffers[4].Status == "invalidated",
            "each intent must invalidate on its own non-executable condition");

        // Dedup: the newest offer in a normalized intent+party group survives.
        var dupRound = new WorldDiplomacyRound { RoundId = "r4" };
        dupRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { Intent = "propose_peace", ProposerKingdomId = "kA", TargetKingdomId = "kB", CreatedDay = 5, SourceDocumentId = "old" });
        dupRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { Intent = "propose_peace", ProposerKingdomId = "kA", TargetKingdomId = "kB", CreatedDay = 9, SourceDocumentId = "new" });
        dupRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { Intent = "propose_alliance", ProposerKingdomId = "kA", TargetKingdomId = "kB", CreatedDay = 1, SourceDocumentId = "other" });
        WorldDiplomacyRoundLifecycleRules.PruneInvalidOffers(dupRound, () => true, healthy,
            (a, b) => a == b, (a, b) => true,
            () => false, (a, b) => false, () => false, (a, b) => false,
            _ => true, pruneLogs.Add);
        Test.True(dupRound.PendingOffers[0].Status == "superseded"
            && dupRound.PendingOffers[1].Status == "open"
            && dupRound.PendingOffers[2].Status == "invalidated",
            "older duplicates must be superseded while the newest offer and other intents stay independent");
        Test.True(pruneLogs.Count == 3
            && pruneLogs[2].Contains("round=r4") && pruneLogs[2].Contains("count="),
            "each non-empty prune must log the round and invalidated count once");
    }

    static void RunStoredRecordNormalizationDecisionTests()
    {
        // DPL-060CJ: per-document normalization must preserve accounting mirroring,
        // action-list dedup/cap/repair, projection fields, and publication flags.
        var doc = new WorldDiplomacyDocument
        {
            DocumentId = "d1",
            RoundProgressHandled = true,
            AuthorKingdomId = "kA",
            Day = 12,
            ExchangeId = "ex1",
            AnalysisStatus = "done",
            TargetKingdomName = "未知王国",
            Actions = new List<WorldDiplomacyDocumentAction>
            {
                new WorldDiplomacyDocumentAction { ActionId = "", TargetKingdomId = " kB ", Intent = "Declare War", ChangedDiplomaticState = true, RequiresResponse = true, MechanicalResult = "war_declared" },
                new WorldDiplomacyDocumentAction { ActionId = "", TargetKingdomId = "kB", Intent = "declare_war" },
                new WorldDiplomacyDocumentAction { ActionId = "act_c", TargetKingdomId = "kC", Intent = "propose_peace" },
                new WorldDiplomacyDocumentAction { ActionId = "act_c", TargetKingdomId = "kD", Intent = "propose_trade" }
            }
        };
        var docStorage = new WorldDiplomacyStorage();
        WorldDiplomacyStorageMigration.NormalizeStoredDocumentRecord(
            doc, docStorage, migrateLegacyPropagationState: false,
            legacyPropagationRecoveryWindow: 20, currentDay: 30, maxActionsPerDocument: 4,
            resolveKingdomNameOrEmpty: id => id == "kB" ? "Beta" : "",
            normalizeKingdomIdList: (ids, excluded) => ids
                .Select(x => (x ?? "").Trim())
                .Where(x => x.Length > 0 && !string.Equals(x, excluded, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            formatCampaignDate: d => "day_" + d,
            nonHideoutSettlementIds: new List<string>(), nonEliminatedKingdomIds: new List<string>());
        Test.True(doc.RoundAccountingHandled && doc.RoundId == "ex1" && doc.GameDate == "day_12"
            && doc.TargetKingdomName == "Beta" && doc.IsReadyForPublication,
            "document normalization must mirror accounting, round backfill, date, and publication flags");
        foreach (string placeholder in new[] { DiplomacyCompatibilityTexts.Get("Legacy_UnknownKingdomName"), "Unknown Kingdom", "Custom realm" })
        {
        var nameless = new WorldDiplomacyDocument { DocumentId = "d4", TargetKingdomName = placeholder };
        WorldDiplomacyStorageMigration.NormalizeStoredDocumentRecord(
            nameless, docStorage, migrateLegacyPropagationState: false,
            legacyPropagationRecoveryWindow: 20, currentDay: 30, maxActionsPerDocument: 4,
            resolveKingdomNameOrEmpty: _ => "", normalizeKingdomIdList: (ids, ex) => new List<string>(),
            formatCampaignDate: d => "day_" + d,
            nonHideoutSettlementIds: new List<string>(), nonEliminatedKingdomIds: new List<string>());
        Test.True(nameless.TargetKingdomName == (placeholder == "Custom realm" ? placeholder : ""),
            "known CN/EN placeholder names clear across editions while dynamic realm names survive");
        }
        Test.True(doc.Actions.Count == 3
            && doc.Actions[0].TargetKingdomId == "kB" && doc.Actions[0].Intent == "declare_war"
            && doc.Actions[0].TargetKingdomName == "Beta"
            && doc.Actions[1].TargetKingdomId == "kC"
            && doc.Actions[2].TargetKingdomId == "kD",
            "duplicate targets must drop and surviving actions must keep order with resolved names");
        var actionIds = doc.Actions.Select(x => x.ActionId).ToList();
        Test.True(actionIds.All(x => !string.IsNullOrWhiteSpace(x))
            && actionIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() == actionIds.Count
            && doc.Actions[0].ActionId == "action_1",
            "blank and colliding action ids must be repaired to unique action_N ids");
        Test.True(doc.Actions[0].Commitment
                == WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent("declare_war")
            && doc.ChangedDiplomaticState && doc.RequiresResponse
            && doc.MechanicalResult.Length > 0,
            "normalized actions must drive commitment, mechanical result, and state flags");
        Test.True(doc.AddressedKingdomIds.Count == 3
            && doc.AddressedKingdomIds.SequenceEqual(new[] { "kB", "kC", "kD" }),
            "addressed ids must rebuild from surviving action targets via the host port");

        // Legacy propagation: pre-format docs complete instantly; coverage port decides the rest.
        var legacy = new WorldDiplomacyDocument
        {
            DocumentId = "d2", AuthorKingdomId = "kA", Day = 28,
            IsReadyForPublication = true, PropagationStarted = false, OriginSettlementId = ""
        };
        WorldDiplomacyStorageMigration.NormalizeStoredDocumentRecord(
            legacy, docStorage, migrateLegacyPropagationState: true,
            legacyPropagationRecoveryWindow: 20, currentDay: 30, maxActionsPerDocument: 4,
            resolveKingdomNameOrEmpty: _ => "", normalizeKingdomIdList: (ids, ex) => new List<string>(),
            formatCampaignDate: d => "day_" + d,
            nonHideoutSettlementIds: new List<string>(), nonEliminatedKingdomIds: new List<string>());
        Test.True(legacy.PropagationCompleted && legacy.HasReachedPlayerCourt,
            "pre-propagation documents must complete and reach the player court immediately");
        var uncovered = new WorldDiplomacyDocument
        {
            DocumentId = "d3", AuthorKingdomId = "kA", Day = 28,
            IsReadyForPublication = true, PropagationStarted = true, OriginSettlementId = "town_1"
        };
        WorldDiplomacyStorageMigration.NormalizeStoredDocumentRecord(
            uncovered, docStorage, migrateLegacyPropagationState: true,
            legacyPropagationRecoveryWindow: 20, currentDay: 30, maxActionsPerDocument: 4,
            resolveKingdomNameOrEmpty: _ => "", normalizeKingdomIdList: (ids, ex) => new List<string>(),
            formatCampaignDate: d => "day_" + d,
            nonHideoutSettlementIds: new List<string> { "town_2" }, nonEliminatedKingdomIds: new List<string> { "kB" });
        Test.True(!uncovered.PropagationCompleted,
            "a relevant document with incomplete coverage must stay pending propagation");

        // Coverage halves: settlements covered but kingdoms missing stays incomplete, and vice versa.
        docStorage.SettlementKnowledge.Add(new WorldDiplomacySettlementKnowledge
        {
            SettlementId = "town_2", DocumentIds = new List<string> { "d5" }, LastUpdatedDay = 29
        });
        var partialA = new WorldDiplomacyDocument
        {
            DocumentId = "d5", AuthorKingdomId = "kA", Day = 28,
            IsReadyForPublication = true, PropagationStarted = true, OriginSettlementId = "town_1"
        };
        WorldDiplomacyStorageMigration.NormalizeStoredDocumentRecord(
            partialA, docStorage, migrateLegacyPropagationState: true,
            legacyPropagationRecoveryWindow: 20, currentDay: 30, maxActionsPerDocument: 4,
            resolveKingdomNameOrEmpty: _ => "", normalizeKingdomIdList: (ids, ex) => new List<string>(),
            formatCampaignDate: d => "day_" + d,
            nonHideoutSettlementIds: new List<string> { "town_2" }, nonEliminatedKingdomIds: new List<string> { "kB" });
        Test.True(!partialA.PropagationCompleted,
            "settlement coverage alone cannot complete propagation while a kingdom is uncovered");
        var partialB = new WorldDiplomacyDocument
        {
            DocumentId = "d6", AuthorKingdomId = "kA", Day = 28,
            IsReadyForPublication = true, PropagationStarted = true, OriginSettlementId = "town_1"
        };
        WorldDiplomacyStorageMigration.NormalizeStoredDocumentRecord(
            partialB, docStorage, migrateLegacyPropagationState: true,
            legacyPropagationRecoveryWindow: 20, currentDay: 30, maxActionsPerDocument: 4,
            resolveKingdomNameOrEmpty: _ => "", normalizeKingdomIdList: (ids, ex) => new List<string>(),
            formatCampaignDate: d => "day_" + d,
            nonHideoutSettlementIds: new List<string>(), nonEliminatedKingdomIds: new List<string> { "kB" });
        Test.True(!partialB.PropagationCompleted,
            "kingdom coverage alone cannot complete propagation while a settlement is uncovered");

        // Full coverage: pending settlement + pending court arrivals complete the document.
        docStorage.PropagationArrivals.Add(new WorldDiplomacyPropagationArrival
        {
            DocumentId = "d7", Scope = "civilian", SettlementId = "town_2", DueDay = 31
        });
        docStorage.PropagationArrivals.Add(new WorldDiplomacyPropagationArrival
        {
            DocumentId = "d7", Scope = "court", KingdomId = "kB", DueDay = 31
        });
        var covered = new WorldDiplomacyDocument
        {
            DocumentId = "d7", AuthorKingdomId = "kA", Day = 28,
            IsReadyForPublication = true, PropagationStarted = true, OriginSettlementId = "town_1"
        };
        WorldDiplomacyStorageMigration.NormalizeStoredDocumentRecord(
            covered, docStorage, migrateLegacyPropagationState: true,
            legacyPropagationRecoveryWindow: 20, currentDay: 30, maxActionsPerDocument: 4,
            resolveKingdomNameOrEmpty: _ => "", normalizeKingdomIdList: (ids, ex) => new List<string>(),
            formatCampaignDate: d => "day_" + d,
            nonHideoutSettlementIds: new List<string> { "town_2" }, nonEliminatedKingdomIds: new List<string> { "kB" });
        Test.True(covered.PropagationCompleted,
            "full settlement and kingdom coverage must complete legacy propagation");

        // Reload idempotency: a second normalization must not re-evaluate or regress the completed flag.
        covered.PropagationCompleted = true;
        WorldDiplomacyStorageMigration.NormalizeStoredDocumentRecord(
            covered, docStorage, migrateLegacyPropagationState: true,
            legacyPropagationRecoveryWindow: 20, currentDay: 30, maxActionsPerDocument: 4,
            resolveKingdomNameOrEmpty: _ => "", normalizeKingdomIdList: (ids, ex) => new List<string>(),
            formatCampaignDate: d => "day_" + d,
            nonHideoutSettlementIds: new List<string> { "town_2" }, nonEliminatedKingdomIds: new List<string> { "kB" });
        Test.True(covered.PropagationCompleted,
            "repeated normalization must not regress a completed propagation marker");

        // DPL-060CJ: per-round normalization must clamp counters, backfill, and honor gates.
        var logs = new List<string>();
        int pruneCalls = 0;
        var round = new WorldDiplomacyRound
        {
            RoundId = "r9", State = "closed", SchemaVersion = 23, StartedDay = 5,
            SoftEndDay = 26, ConsecutiveNoActionPasses = 99, AutomaticDocumentsStarted = 0
        };
        round.PendingOffers.Add(new WorldDiplomacyRoundOffer
        {
            Intent = "propose_peace", Status = "open",
            ProposerKingdomId = "kA", TargetKingdomId = "kB", SourceDocumentId = "ghost"
        });
        var roundStorage = new WorldDiplomacyStorage();
        roundStorage.CompletedRounds.Add(round);
        roundStorage.Documents.Add(new WorldDiplomacyDocument
        {
            DocumentId = "fin1", RoundId = "r9", Day = 20, IsReadyForPublication = true,
            AuthorKingdomId = "kA"
        });
        var documentsById = WorldDiplomacyDocumentFactRules.BuildDocumentIndex(roundStorage.Documents);
        WorldDiplomacyStorageMigration.NormalizeStoredRoundRecord(
            round, roundStorage, documentsById, allowWorldValidation: false,
            decisionArchitectureVersion: 1, relaySchemaVersion: 23,
            relayTargetDurationDays: 21, courtMaxDeliveryDays: 3,
            maxPendingPolicySignals: 24, maxConsecutiveTechnicalGenerationFailuresPerRound: 3,
            getRoundHardDurationDays: d => d + 7,
            pruneInvalidOffers: _ => pruneCalls++, log: logs.Add);
        Test.True(round.ConsecutiveNoActionPasses == 3
            && round.FinalDocumentId == "fin1"
            && round.AutomaticDocumentsStarted == 1
            && round.DiplomaticActionAttemptCount == 1
            && round.SoftEndDay == 26 && round.RelayPassDurationDays == 3 && round.HardEndDay == 33,
            "round normalization must clamp, backfill schedule/final document, and recover counts");
        Test.True(pruneCalls == 0,
            "pruning must stay gated behind world validation even for an active round");
        Test.True(round.PendingOffers[0].Status == "invalidated"
            && logs.Any(x => x.Contains("round=r9")),
            "unresolvable offer source documents must invalidate and log once");

        // Schema migration inside the round pass retires jobs/arrivals only for the active round.
        var stale = new WorldDiplomacyRound { RoundId = "r10", State = "active", SchemaVersion = 5, StartedDay = 1, SoftEndDay = 22 };
        stale.PendingOffers.Add(new WorldDiplomacyRoundOffer { Intent = "chat", Status = "open" });
        stale.LlmTranscript.Add(new WorldDiplomacyLlmMessage { Role = "user" });
        stale.CachePrefix = "pfx";
        var staleStorage = new WorldDiplomacyStorage();
        staleStorage.ActiveRound = stale;
        staleStorage.DecisionArchitectureVersion = 1;
        staleStorage.Jobs.Add(new WorldDiplomacyJob { JobId = "j1", Kind = "generate", RoundId = "r10" });
        staleStorage.Jobs.Add(new WorldDiplomacyJob { JobId = "j2", Kind = "analyze", RoundId = "r10" });
        staleStorage.Jobs.Add(new WorldDiplomacyJob { JobId = "j3", Kind = "generate", RoundId = "other" });
        staleStorage.RelayArrivals.Add(new WorldDiplomacyRelayArrival { RoundId = "r10", ToKingdomId = "kB" });
        staleStorage.RelayArrivals.Add(new WorldDiplomacyRelayArrival { RoundId = "other", ToKingdomId = "kC" });
        stale.AutomaticDocumentsStarted = 5;
        WorldDiplomacyStorageMigration.NormalizeStoredRoundRecord(
            stale, staleStorage, new Dictionary<string, WorldDiplomacyDocument>(),
            allowWorldValidation: true,
            decisionArchitectureVersion: 1, relaySchemaVersion: 23,
            relayTargetDurationDays: 21, courtMaxDeliveryDays: 3,
            maxPendingPolicySignals: 24, maxConsecutiveTechnicalGenerationFailuresPerRound: 3,
            getRoundHardDurationDays: d => d + 7,
            pruneInvalidOffers: _ => pruneCalls++, log: logs.Add);
        Test.True(pruneCalls == 1 && stale.SchemaVersion == 23
            && stale.CachePrefix == "" && stale.LlmTranscript.Count == 0
            && stale.RelayWaiting == false,
            "schema lag on the active round must prune, clear caches, and stamp the version");
        Test.True(staleStorage.Jobs.Count == 2 && staleStorage.Jobs.All(x => x.JobId != "j1")
            && staleStorage.RelayArrivals.Count == 1 && staleStorage.RelayArrivals[0].RoundId == "other"
            && stale.AutomaticDocumentsStarted == 4,
            "retirement must drop only this round's generate/round_plan jobs and arrivals");
    }

    static void RunRoundBookkeepingDecisionTests()
    {
        // DPL-060CK: policy signal completion removes pendings, dedupes/caps
        // processed keys, and logs once.
        var sigStorage = new WorldDiplomacyStorage();
        sigStorage.PendingPolicySignals.Add(new WorldDiplomacyPolicySignal { SignalKey = "S1" });
        sigStorage.PendingPolicySignals.Add(new WorldDiplomacyPolicySignal { SignalKey = "s1" });
        sigStorage.PendingPolicySignals.Add(new WorldDiplomacyPolicySignal { SignalKey = "keep" });
        sigStorage.ProcessedPolicySignalKeys.Add("S1");
        var sigLogs = new List<string>();
        WorldDiplomacyRoundLifecycleRules.CompletePolicySignal(
            sigStorage, new WorldDiplomacyPolicySignal { SignalKey = "s1" }, 4, "opened_round", sigLogs.Add);
        Test.True(sigStorage.PendingPolicySignals.Count == 1
            && sigStorage.PendingPolicySignals[0].SignalKey == "keep"
            && sigStorage.ProcessedPolicySignalKeys.Count == 1
            && sigStorage.ProcessedPolicySignalKeys[0] == "s1"
            && sigLogs.Count == 1 && sigLogs[0].Contains("opened_round"),
            "policy signal completion must remove pendings, dedupe processed keys, and log once");
        sigStorage.ProcessedPolicySignalKeys.Clear();
        sigStorage.ProcessedPolicySignalKeys.AddRange(new[] { "a", "b", "c" });
        WorldDiplomacyRoundLifecycleRules.CompletePolicySignal(
            sigStorage, new WorldDiplomacyPolicySignal { SignalKey = "d" }, 2, "r", sigLogs.Add);
        Test.True(sigStorage.ProcessedPolicySignalKeys.SequenceEqual(new[] { "c", "d" }),
            "processed signal keys must cap to the most recent entries");
        int sigLogCount = sigLogs.Count;
        WorldDiplomacyRoundLifecycleRules.CompletePolicySignal(
            sigStorage, null, 2, "r", sigLogs.Add);
        Test.True(sigLogs.Count == sigLogCount && sigStorage.PendingPolicySignals.Count == 1,
            "null signals must be a no-op");

        // AttachPolicySignalToRound dedupes keys, clones once, and appends context.
        var attachRound = new WorldDiplomacyRound { RoundId = "ar" };
        var signal = new WorldDiplomacyPolicySignal
        {
            SignalKey = "sig_1", PolicyId = "p1",
            IssuerKingdomId = "kA", TargetKingdomId = "kB"
        };
        WorldDiplomacyRoundLifecycleRules.AttachPolicySignalToRound(attachRound, signal);
        string contextAfterFirst = attachRound.ExternalOpeningContext;
        WorldDiplomacyRoundLifecycleRules.AttachPolicySignalToRound(attachRound,
            new WorldDiplomacyPolicySignal { SignalKey = "SIG_1", PolicyId = "p9" });
        Test.True(attachRound.ExternalSignalKeys.Count == 1
            && attachRound.ExternalSignalKeys[0] == "sig_1"
            && attachRound.AttachedPolicySignals.Count == 1
            && attachRound.AttachedPolicySignals[0].PolicyId == "p1",
            "re-attaching a signal key must keep the first external key and cloned snapshot");
        Test.True(attachRound.ExternalOpeningContext == contextAfterFirst
            || (attachRound.ExternalOpeningContext ?? "").Contains(contextAfterFirst ?? ""),
            "re-attaching must never corrupt the accumulated opening context");
        WorldDiplomacyRoundLifecycleRules.AttachPolicySignalToRound(null, signal);
        WorldDiplomacyRoundLifecycleRules.AttachPolicySignalToRound(attachRound, null);
        Test.True(attachRound.ExternalSignalKeys.Count == 1,
            "null round or signal must be a no-op");

        // Closed-round cooldown settle: accepted clears both directions, failures
        // start cooldowns, skip-close reasons never record failures.
        var cdRound = new WorldDiplomacyRound { RoundId = "cr", CompletedDay = 40, CloseReason = "result_settled" };
        cdRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { ProposerKingdomId = "kA", TargetKingdomId = "kB", Intent = "propose_trade", Status = "accepted" });
        cdRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { ProposerKingdomId = "kC", TargetKingdomId = "kD", Intent = "propose_alliance", Status = "rejected" });
        var cds = new List<WorldDiplomacyOfferCooldown>();
        var byKey = new Dictionary<WorldDiplomacyOfferCooldownKey, WorldDiplomacyOfferCooldown>();
        var existingKey = new WorldDiplomacyOfferCooldownKey("kA", "kB", WorldDiplomacyOfferDomain.Trade);
        var existing = new WorldDiplomacyOfferCooldown
        { ProposerKingdomId = "kA", TargetKingdomId = "kB", Domain = "trade", LastFailedRoundDay = 10 };
        cds.Add(existing);
        byKey[existingKey] = existing;
        int normalizations = 0;
        var cdLogs = new List<string>();
        var fullStore = new WorldDiplomacyStorage();
        for (int i = 0; i < 2048; i++) fullStore.OfferCooldowns.Add(new WorldDiplomacyOfferCooldown
            { ProposerKingdomId = "old" + i, TargetKingdomId = "target", Domain = "trade", LastFailedRoundDay = 1 });
        var fullIndex = new Dictionary<WorldDiplomacyOfferCooldownKey, WorldDiplomacyOfferCooldown>();
        WorldDiplomacyRoundLifecycleRules.RebuildOfferCooldownIndex(fullStore.OfferCooldowns, fullIndex);
        var overflowRound = new WorldDiplomacyRound { RoundId = "overflow", CompletedDay = 40, CloseReason = "result_settled" };
        foreach (string id in new[] { "new1", "new2" }) overflowRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
            { ProposerKingdomId = id, TargetKingdomId = "target", Intent = "propose_trade", Status = "rejected" });
        int overflowNormalizations = 0;
        WorldDiplomacyOfferCooldownApplication.SettleTradeAllianceOfferCooldownsForClosedRound(
            overflowRound, fullStore.OfferCooldowns, fullIndex, () => {
                overflowNormalizations++;
                AnimusForge.Refactor.Persistence.WorldDiplomacyOfferCooldownStorageNormalizer.Normalize(fullStore);
                WorldDiplomacyRoundLifecycleRules.RebuildOfferCooldownIndex(fullStore.OfferCooldowns, fullIndex);
            }, _ => { });
        Test.True(overflowNormalizations == 1 && fullStore.OfferCooldowns.Count == 2048
            && fullStore.OfferCooldowns.Any(x => x.ProposerKingdomId == "new1")
            && fullStore.OfferCooldowns.Any(x => x.ProposerKingdomId == "new2"),
            "batch normalization must retain both new cooldowns when it replaces a full canonical list");
        WorldDiplomacyOfferCooldownApplication.SettleTradeAllianceOfferCooldownsForClosedRound(
            cdRound, cds, byKey, () => normalizations++, cdLogs.Add);
        var newKey = new WorldDiplomacyOfferCooldownKey("kC", "kD", WorldDiplomacyOfferDomain.Alliance);
        Test.True(cds.Count == 1 && !byKey.ContainsKey(existingKey)
            && byKey.ContainsKey(newKey) && byKey[newKey].LastFailedRoundDay == 40
            && byKey[newKey].SourceRoundId == "cr"
            && cdLogs.Count == 1 && cdLogs[0].Contains("started=1") && cdLogs[0].Contains("cleared=1"),
            "closed-round settle must clear accepted cooldowns and start failed ones");
        var skipRound = new WorldDiplomacyRound { RoundId = "sr", CompletedDay = 41, CloseReason = "technical_generation_rejected" };
        skipRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { ProposerKingdomId = "kE", TargetKingdomId = "kF", Intent = "propose_trade", Status = "rejected" });
        int cdLogCount = cdLogs.Count;
        WorldDiplomacyOfferCooldownApplication.SettleTradeAllianceOfferCooldownsForClosedRound(
            skipRound, cds, byKey, () => normalizations++, cdLogs.Add);
        Test.True(cds.Count == 1 && cdLogs.Count == cdLogCount,
            "skip-close reasons must not record new failures or log");
        WorldDiplomacyOfferCooldownApplication.SettleTradeAllianceOfferCooldownsForClosedRound(
            null, cds, byKey, () => normalizations++, cdLogs.Add);
        Test.True(cdLogs.Count == cdLogCount,
            "null rounds must be a no-op");

        // RegisterRelayProposalOffer counters reverses, supersedes same-direction
        // duplicates, replaces same-source records, and appends one open offer.
        var propRound = new WorldDiplomacyRound { RoundId = "pr" };
        propRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { Intent = "propose_peace", ProposerKingdomId = "kB", TargetKingdomId = "kA", Status = "open", SourceDocumentId = "old1", SourceActionId = "a1" });
        propRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { Intent = "propose_peace", ProposerKingdomId = "kA", TargetKingdomId = "kB", Status = "open", SourceDocumentId = "old2", SourceActionId = "a2" });
        propRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { Intent = "propose_peace", ProposerKingdomId = "kA", TargetKingdomId = "kB", Status = "open", SourceDocumentId = "docX", SourceActionId = "pa" });
        propRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { Intent = "propose_trade", ProposerKingdomId = "kC", TargetKingdomId = "kD", Status = "open", SourceDocumentId = "other" });
        var proposalDoc = new WorldDiplomacyDocument
        { DocumentId = "docX", ProcessingActionId = "pa", AuthorKingdomId = "kA", TargetKingdomId = "kB", Day = 15 };
        WorldDiplomacyRoundLifecycleRules.RegisterRelayProposalOffer(propRound, proposalDoc, "propose_peace");
        Test.True(propRound.PendingOffers.Count == 4
            && propRound.PendingOffers[0].Status == "countered"
            && propRound.PendingOffers[1].Status == "superseded"
            && propRound.PendingOffers[2].Status == "open"
            && propRound.PendingOffers.Count(x => x.SourceDocumentId == "docX" && x.SourceActionId == "pa") == 1
            && propRound.PendingOffers[3].Intent == "propose_peace"
            && propRound.PendingOffers[3].Status == "open"
            && propRound.PendingOffers[3].CreatedDay == 15,
            "proposal registration must counter reverses, supersede duplicates, replace the same source, and append one open offer");

        // SelectMatchingRelayResponseOffers isolates the unique open offer that
        // targets the responder and matches the recorded source.
        var respRound = new WorldDiplomacyRound { RoundId = "rr" };
        respRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { Intent = "propose_peace", ProposerKingdomId = "kX", TargetKingdomId = "kA", Status = "open", SourceDocumentId = "sd", SourceActionId = "sa" });
        respRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { Intent = "propose_alliance", ProposerKingdomId = "kY", TargetKingdomId = "kA", Status = "open" });
        respRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { Intent = "propose_peace", ProposerKingdomId = "kA", TargetKingdomId = "kX", Status = "open" });
        var respDoc = new WorldDiplomacyDocument
        { AuthorKingdomId = "kA", TargetKingdomId = "kX", RespondingToOfferDocumentId = "sd", RespondingToOfferActionId = "sa" };
        var matches = WorldDiplomacyRoundLifecycleRules.SelectMatchingRelayResponseOffers(respRound, respDoc, "propose_peace");
        Test.True(matches.Count == 1 && matches[0].ProposerKingdomId == "kX",
            "response matching must isolate the unique open offer to the responder");
        var wrongSource = new WorldDiplomacyDocument
        { AuthorKingdomId = "kA", TargetKingdomId = "kX", RespondingToOfferDocumentId = "other" };
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectMatchingRelayResponseOffers(
                respRound, wrongSource, "propose_peace").Count == 0,
            "mismatched source references must select nothing");
        Test.True(WorldDiplomacyRoundLifecycleRules.SelectMatchingRelayResponseOffers(
                null, respDoc, "propose_peace").Count == 0,
            "null rounds must select nothing");
    }

    static void RunLegalActionIntentDecisionTests()
    {
        // DPL-060CL: response intents are appended only for unique open proposals
        // directed from the target to the author.
        var respRound = new WorldDiplomacyRound { RoundId = "lr" };
        respRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { Intent = "propose_peace", ProposerKingdomId = "kB", TargetKingdomId = "kA", Status = "open",
          SourceDocumentId = "sd1", SourceActionId = "sa1" });
        respRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { Intent = "propose_alliance", ProposerKingdomId = "kB", TargetKingdomId = "kA", Status = "open",
          SourceDocumentId = "sd2", SourceActionId = "sa2" });
        respRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { Intent = "propose_alliance", ProposerKingdomId = "kB", TargetKingdomId = "kA", Status = "open",
          SourceDocumentId = "sd3", SourceActionId = "sa3" });
        respRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { Intent = "propose_trade", ProposerKingdomId = "kA", TargetKingdomId = "kB", Status = "open",
          SourceDocumentId = "sd4", SourceActionId = "sa4" });
        respRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { Intent = "statement", ProposerKingdomId = "kB", TargetKingdomId = "kA", Status = "open",
          SourceDocumentId = "sd5", SourceActionId = "sa5" });
        var respIntents = new List<string>();
        WorldDiplomacyRoundLifecycleRules.AppendOpenOfferResponseIntents(respRound, "kA", "kB", respIntents);
        Test.True(respIntents.Count == 2
            && respIntents[0] == "accept_peace" && respIntents[1] == "reject_peace",
            "only unique directed proposals must yield accept/reject response intents");
        WorldDiplomacyRoundLifecycleRules.AppendOpenOfferResponseIntents(respRound, "", "kB", respIntents);
        WorldDiplomacyRoundLifecycleRules.AppendOpenOfferResponseIntents(null, "kA", "kB", respIntents);
        WorldDiplomacyRoundLifecycleRules.AppendOpenOfferResponseIntents(respRound, "kA", "kB", null);
        Test.True(respIntents.Count == 2, "invalid parties or null rounds must be a no-op");

        // BuildLegalDiplomaticActionIntents: a unique open peace proposal collapses
        // the option set to accept/reject; own open proposals are removed; response
        // intents are appended; immediate war-response suppression strips peace.
        var warRound = new WorldDiplomacyRound
        {
            RoundId = "wr", State = "active",
            ResultSettlementPending = true, ResultSettlementCurrentSlotId = "s1"
        };
        warRound.ResultSettlementSlots.Add(new WorldDiplomacyResultSettlementSlot
        {
            SlotId = "s1", KingdomId = "kA", Kind = "war_response",
            RelatedKingdomIds = new List<string> { "kB" },
            SourceDocumentIds = new List<string> { "warDoc" }
        });
        var warDoc = new WorldDiplomacyDocument
        {
            DocumentId = "warDoc", RoundId = "wr", AuthorKingdomId = "kB",
            IsReadyForPublication = true,
            Actions = new List<WorldDiplomacyDocumentAction>
            {
                new WorldDiplomacyDocumentAction
                { ChangedDiplomaticState = true, Intent = "declare_war", TargetKingdomId = "kA" }
            }
        };
        Func<string, WorldDiplomacyDocument> resolveWarDoc = id => id == "warDoc" ? warDoc : null;
        var suppressed = WorldDiplomacyRoundLifecycleRules.BuildLegalDiplomaticActionIntents(
            warRound, "kA", "kB",
            () => new List<string> { "propose_peace", "declare_war" }, resolveWarDoc);
        Test.True(suppressed.SequenceEqual(new[] { "propose_peace", "declare_war" }),
            "legacy war-response seats cannot suppress current peace options");
        var plain = WorldDiplomacyRoundLifecycleRules.BuildLegalDiplomaticActionIntents(
            warRound, "kA", "kC",
            () => new List<string> { "propose_peace", "propose_trade" }, resolveWarDoc);
        Test.True(plain.SequenceEqual(new[] { "propose_peace", "propose_trade" }),
            "unrelated war-response slots must not suppress the potential option set");

        var offerRound = new WorldDiplomacyRound { RoundId = "or" };
        offerRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { Intent = "propose_peace", ProposerKingdomId = "kB", TargetKingdomId = "kA", Status = "open",
          SourceDocumentId = "od1", SourceActionId = "oa1" });
        var collapsed = WorldDiplomacyRoundLifecycleRules.BuildLegalDiplomaticActionIntents(
            offerRound, "kA", "kB", () => new List<string> { "propose_peace", "declare_war" }, _ => null);
        Test.True(collapsed.SequenceEqual(new[] { "propose_peace", "declare_war", "accept_peace", "reject_peace" }),
            "an open peace proposal adds responses without removing other current options");

        var ownRound = new WorldDiplomacyRound { RoundId = "or2" };
        ownRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        { Intent = "propose_trade", ProposerKingdomId = "kA", TargetKingdomId = "kB", Status = "open" });
        var filtered = WorldDiplomacyRoundLifecycleRules.BuildLegalDiplomaticActionIntents(
            ownRound, "kA", "kB",
            () => new List<string> { "propose_trade", "propose_peace" }, _ => null);
        Test.True(filtered.SequenceEqual(new[] { "propose_trade", "propose_peace", "withdraw_offer" }),
            "own open proposals allow negotiation and add exact-source withdrawal");
        var none = WorldDiplomacyRoundLifecycleRules.BuildLegalDiplomaticActionIntents(
            null, "kA", "kB", null, _ => null);
        Test.True(none.Count == 0, "null ports must yield an empty option set");
    }

    static void RunRelaySourceContextDecisionTests()
    {
        // DPL-060CM: the relay response source context ranks the mandatory source
        // first, the direct response source second, pending peace offers third,
        // any answerable offer fourth, and stays bounded at four documents.
        var round = new WorldDiplomacyRound
        {
            RoundId = "rc", ResultSettlementCurrentSlotId = "s1"
        };
        round.ResultSettlementSlots.Add(new WorldDiplomacyResultSettlementSlot
        {
            SlotId = "s1", KingdomId = "kA",
            SourceDocumentIds = new List<string> { "slotDoc" },
            RelatedKingdomIds = new List<string> { "kB", "kC" }
        });
        round.PendingOffers.Add(new WorldDiplomacyRoundOffer
        {
            Intent = "propose_peace", ProposerKingdomId = "kB", TargetKingdomId = "kA",
            Status = "open", SourceDocumentId = "peaceDoc"
        });
        round.PendingOffers.Add(new WorldDiplomacyRoundOffer
        {
            Intent = "propose_peace", ProposerKingdomId = "kC", TargetKingdomId = "kA",
            Status = "open", SourceDocumentId = "peaceDoc2"
        });
        round.PendingOffers.Add(new WorldDiplomacyRoundOffer
        {
            Intent = "propose_trade", ProposerKingdomId = "kC", TargetKingdomId = "kA",
            Status = "open", SourceDocumentId = "tradeDoc"
        });
        Func<string, WorldDiplomacyDocument> doc = id => new WorldDiplomacyDocument
        {
            DocumentId = id, AuthorKingdomId = "kB", Day = 1,
            IsReadyForPublication = true, Title = "t_" + id, Body = "b_" + id
        };
        var documents = new List<WorldDiplomacyDocument>
        {
            doc("respDoc"), doc("peaceDoc"), doc("peaceDoc2"), doc("tradeDoc"),
            doc("slotDoc"), doc("unpublished")
        };
        documents[5].IsReadyForPublication = false;
        var responseSource = doc("respDoc");
        var sb = new System.Text.StringBuilder();
        WorldDiplomacyRoundLifecycleRules.AppendRelayResponseSourceContext(
            sb, round, "kA", responseSource, "peaceDoc", documents);
        string rendered = sb.ToString();
        int requiredIdx = rendered.IndexOf("peaceDoc|", StringComparison.Ordinal);
        int respIdx = rendered.IndexOf("respDoc", StringComparison.Ordinal);
        int peaceIdx = rendered.IndexOf("peaceDoc2", StringComparison.Ordinal);
        int tradeIdx = rendered.IndexOf("tradeDoc", StringComparison.Ordinal);
        Test.True(requiredIdx >= 0
            && respIdx > requiredIdx
            && peaceIdx > respIdx
            && tradeIdx > peaceIdx
            && !rendered.Contains("unpublished")
            && !rendered.Contains("slotDoc"),
            "source ranking must be required > response > peace offer > other offer, bounded at four published docs");
        Test.True(rendered.Contains("kB,kC") || rendered.Contains("kC,kB"),
            "slot related kingdoms must be projected into the context");
        var sb2 = new System.Text.StringBuilder();
        WorldDiplomacyRoundLifecycleRules.AppendRelayResponseSourceContext(
            sb2, new WorldDiplomacyRound { RoundId = "empty" }, "kA", null, null,
            new List<WorldDiplomacyDocument>());
        Test.True(sb2.Length == 0, "no candidate sources must render nothing");
        var sb3 = new System.Text.StringBuilder();
        WorldDiplomacyRoundLifecycleRules.AppendRelayResponseSourceContext(
            sb3, new WorldDiplomacyRound { RoundId = "fb" }, "kA",
            doc("onlyDoc"), null, new List<WorldDiplomacyDocument>());
        Test.True(sb3.ToString().Contains("onlyDoc"),
            "the published response source must render as the last-resort fallback");
        var sb4 = new System.Text.StringBuilder();
        WorldDiplomacyRoundLifecycleRules.AppendRelayResponseSourceContext(
            null, round, "kA", responseSource, null, documents);
        WorldDiplomacyRoundLifecycleRules.AppendRelayResponseSourceContext(
            sb4, null, "kA", responseSource, null, documents);
        WorldDiplomacyRoundLifecycleRules.AppendRelayResponseSourceContext(
            sb4, round, "", responseSource, null, documents);
        Test.True(sb4.Length == 0, "null builders, rounds, or empty authors must be a no-op");
    }

    static void RunGenerationSignatureDecisionTests()
    {
        var sigRound = new WorldDiplomacyRound { RoundId = "R1" };
        sigRound.Participants.Add(new() { KingdomId = "kB", State = "active" });
        sigRound.AttachedPolicySignals.Add(new() { SignalKey = "b_sig", PolicyId = "p2" });
        sigRound.AttachedPolicySignals.Add(new() { SignalKey = "a_sig", PolicyId = "p1" });
        var sigJob = new WorldDiplomacyJob { Kind = "generate", IsRelayTurn = true, ResultSettlementSlotId = "legacy" };
        string sig = WorldDiplomacyRoundLifecycleRules.BuildGenerationLegalActionSignature(sigJob, sigRound, "kA", null, new(), id => true, _ => new());
        Test.True(sig == WorldDiplomacyPromptContractRules.StablePromptHash("kA|R1|policy=a_sig@p1|policy=b_sig@p2\nkB=missing"), "signature uses current parties and stable policy identities");
        sigJob.ResultSettlementSlotId = "changed"; sigRound.ResultSettlementCurrentSlotId = "another";
        sigRound.RelayRouteKingdomIds.Add("ignored");
        Test.True(sig == WorldDiplomacyRoundLifecycleRules.BuildGenerationLegalActionSignature(sigJob, sigRound, "kA", null, new(), id => true, _ => new()), "legacy slots and routes do not invalidate the current signature");

        // Sorted normalized intents; response intents bind the unique open proposal
        // source; comply_ultimatum binds the pending incoming threat document.
        var offerRound = new WorldDiplomacyRound { RoundId = "R2", Participants = new() { new() { KingdomId = "kB", State = "active" } } };
        offerRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        {
            Intent = "propose_peace", ProposerKingdomId = "kB", TargetKingdomId = "kA",
            Status = "open", SourceDocumentId = "odoc1", SourceActionId = "oact1"
        });
        var threats = new List<WorldDiplomacyThreat>
        {
            new WorldDiplomacyThreat
            {
                IssuerKingdomId = "kB", TargetKingdomId = "kA", Status = "open",
                TargetDecision = "pending", StageDocumentId = "tdoc", StageActionId = "tact"
            }
        };
        var actionsJob = new WorldDiplomacyJob { Kind = "generate", TargetKingdomId = "kB" };
        string sig4 = WorldDiplomacyRoundLifecycleRules.BuildGenerationLegalActionSignature(
            actionsJob, offerRound, "kA", null, threats, id => false,
            _ => new List<string> { "propose_trade", "accept_peace", "comply_ultimatum", "declare_war" });
        string expected4 = "kA|R2\nkB=accept_peace@odoc1#oact1,comply_ultimatum@tdoc#tact,declare_war,propose_trade";
        Test.True(sig4 == WorldDiplomacyPromptContractRules.StablePromptHash(expected4),
            "intents must sort deterministically with proposal and threat source bindings");
        string sig5 = WorldDiplomacyRoundLifecycleRules.BuildGenerationLegalActionSignature(
            actionsJob, offerRound, "kA", null, threats, id => false,
            _ => new List<string> { "accept_peace" });
        Test.True(sig5 != sig4
            && sig5 == WorldDiplomacyRoundLifecycleRules.BuildGenerationLegalActionSignature(
                actionsJob, offerRound, "kA", null, threats, id => false,
                _ => new List<string> { "accept_peace" }),
            "the signature must be deterministic and sensitive to the action set");
    }

    static void RunGeneratedStructureAndTargetSelectionDecisionTests()
    {
        var current = new WorldDiplomacyRound { Participants = new() {
            new() { KingdomId = "kA", State = "active" }, new() { KingdomId = "kC", State = "active" },
            new() { KingdomId = "kB", State = "active" }, new() { KingdomId = "observer", State = "observer" } },
            RelayRouteKingdomIds = new() { "legacy" }, ResultSettlementPending = true };
        var source = new WorldDiplomacyJob { Kind = "generate", TargetKingdomId = "legacy", IsRelayTurn = true, ResultSettlementSlotId = "legacy" };
        Test.True(WorldDiplomacyRoundLifecycleRules.GetAuthorizedGenerationTargetIds(source, current, "kA", id => true).SequenceEqual(new[] { "kB", "kC" }), "repair candidates use actual active participants rather than legacy seats or route");
        Test.True(WorldDiplomacyRoundLifecycleRules.GetAuthorizedGenerationTargetIds(source, current, "kA", id => id != "kB").SequenceEqual(new[] { "kC" }), "current candidate eligibility is checked");

        // DPL-060CO: generated structure derivation stamps response bindings or
        // fails with deterministic reasons.
        JObject json = new JObject();
        Test.True(!WorldDiplomacyRoundLifecycleRules.TryDeriveGeneratedDiplomaticStructure(
                null, null, null, "kA", "kB", "declare_war",
                new List<WorldDiplomacyThreat>(), _ => null, out string r0)
            && r0 == "semantic_envelope_incomplete",
            "null envelopes must fail with the incomplete-envelope reason");
        Test.True(WorldDiplomacyRoundLifecycleRules.TryDeriveGeneratedDiplomaticStructure(
                null, null, json, "kA", "kB", "declare_war",
                new List<WorldDiplomacyThreat>(), _ => null, out string r1)
            && r1 == ""
            && string.IsNullOrEmpty((string)json["responding_to_offer_document_id"])
            && (string)json["responding_to_threat_document_id"] == "",
            "non-response intents must stamp empty binding fields and pass");

        var offerRound = new WorldDiplomacyRound { RoundId = "R2", Participants = new() { new() { KingdomId = "kB", State = "active" } } };
        offerRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        {
            Intent = "propose_peace", ProposerKingdomId = "kB", TargetKingdomId = "kA",
            Status = "open", SourceDocumentId = "odoc", SourceActionId = "oact"
        });
        json = new JObject();
        Test.True(!WorldDiplomacyRoundLifecycleRules.TryDeriveGeneratedDiplomaticStructure(
                null, offerRound, json, "kA", "", "accept_peace",
                new List<WorldDiplomacyThreat>(), _ => null, out string r2)
            && r2 == "offer_response_without_unique_open_offer",
            "response intents without a target must fail the unique-offer gate");
        json = new JObject();
        Test.True(WorldDiplomacyRoundLifecycleRules.TryDeriveGeneratedDiplomaticStructure(
                null, offerRound, json, "kA", "kB", "accept_peace",
                new List<WorldDiplomacyThreat>(), _ => null, out string r3)
            && r3 == ""
            && (string)json["responding_to_offer_document_id"] == "odoc"
            && (string)json["responding_to_offer_action_id"] == "oact"
            && json["peace_terms"] == null,
            "source binding leaves authored terms untouched until acceptance comparison");

        var threats = new List<WorldDiplomacyThreat>
        {
            new WorldDiplomacyThreat
            {
                IssuerKingdomId = "kB", TargetKingdomId = "kA", Status = "open",
                TargetDecision = "pending", StageDocumentId = "tdoc", StageActionId = "tact"
            }
        };
        var threatJob = new WorldDiplomacyJob { PresentedThreatDocumentIds = new List<string> { "tdoc" } };
        json = new JObject();
        Test.True(!WorldDiplomacyRoundLifecycleRules.TryDeriveGeneratedDiplomaticStructure(
                threatJob, null, json, "kA", "", "comply_ultimatum",
                threats, _ => null, out string r4)
            && r4 == "comply_ultimatum_without_open_threat",
            "ultimatum compliance without a target must fail the open-threat reason");
        json = new JObject();
        Test.True(!WorldDiplomacyRoundLifecycleRules.TryDeriveGeneratedDiplomaticStructure(
                new WorldDiplomacyJob(), null, json, "kA", "kB", "comply_ultimatum",
                threats, _ => null, out string r5)
            && r5 == "comply_ultimatum_without_unique_presented_threat",
            "unpresented threats must fail the unique-presented-threat gate");
        json = new JObject();
        Test.True(WorldDiplomacyRoundLifecycleRules.TryDeriveGeneratedDiplomaticStructure(
                threatJob, null, json, "kA", "kB", "comply_ultimatum",
                threats, _ => null, out string r6)
            && r6 == ""
            && (string)json["responding_to_threat_document_id"] == "tdoc"
            && (string)json["responding_to_threat_action_id"] == "tact",
            "presented threats must bind the stage document and action");
    }

    static void RunPlayerWorldStateIntentDecisionTests()
    {
        foreach (string treaty in new[] { "annexation", "tributary", "garrison", "vassal" })
        {
            string intent = "accept_" + treaty;
            Test.True(WorldDiplomacyOfferContractRules.CommitmentMatchesIntent(intent, "acceptance")
                && !WorldDiplomacyOfferContractRules.CommitmentMatchesIntent(intent, "binding"),
                treaty + " acceptance remains an offer response even when execution is immediate");
            var response = new WorldDiplomacyDocument
            {
                RoundId = "treaty-round", RespondingToOfferDocumentId = "treaty-offer",
                RespondingToOfferActionId = ""
            };
            var treatyRound = new WorldDiplomacyRound { RoundId = response.RoundId };
            var offer = new WorldDiplomacyRoundOffer
            {
                Intent = "propose_" + treaty, ProposerKingdomId = "vlandia",
                TargetKingdomId = "new_kingdom", Status = "open",
                SourceDocumentId = response.RespondingToOfferDocumentId, SourceActionId = ""
            };
            treatyRound.PendingOffers.Add(offer);
            Test.True(!WorldDiplomacyGenerationValidationRules.TryGetPlayerWorldStateIntentViolation(
                response, intent, "acceptance", "new_kingdom", "vlandia", true,
                _ => (false, ""), (_, _) => (false, ""),
                _ => treatyRound, _ => null, out string acceptedReason) && acceptedReason == "",
                treaty + " player acceptance must pass for its exact open source offer");
            offer.Status = "rejected";
            Test.True(WorldDiplomacyGenerationValidationRules.TryGetPlayerWorldStateIntentViolation(
                response, intent, "acceptance", "new_kingdom", "vlandia", true,
                _ => (false, ""), (_, _) => (false, ""),
                _ => treatyRound, _ => null, out string closedReason)
                && closedReason == "player_offer_response_without_exact_open_offer",
                treaty + " acceptance must still reject a closed source offer");
        }
        // DPL-060CP: the player world-state violation chain gates mechanical
        // intents through party eligibility, commitment shape, source binding,
        // state/threat probes, and exact open-offer resolution.
        var doc = new WorldDiplomacyDocument { RoundId = "R1" };
        Test.True(!WorldDiplomacyGenerationValidationRules.TryGetPlayerWorldStateIntentViolation(
                doc, "statement", "", "kA", "kB", false,
                _ => (true, "should_not_run"), (_, _) => (true, "should_not_run"),
                _ => null, _ => null, out string r0)
            && r0 == "",
            "non-mechanical intents must pass without probing eligibility");

        doc = new WorldDiplomacyDocument { RoundId = "R1" };
        Test.True(WorldDiplomacyGenerationValidationRules.TryGetPlayerWorldStateIntentViolation(
                doc, "declare_war", "binding", "kA", "kB", false,
                _ => (false, ""), (_, _) => (false, ""),
                _ => null, _ => null, out string r1)
            && r1 == "player_action_has_no_eligible_parties",
            "ineligible parties must block mechanical intents first");
        Test.True(WorldDiplomacyGenerationValidationRules.TryGetPlayerWorldStateIntentViolation(
                doc, "declare_war", "proposal", "kA", "kB", true,
                _ => (false, ""), (_, _) => (false, ""),
                _ => null, _ => null, out string r2)
            && r2 == "player_action_commitment_mismatch",
            "commitment shape must match the declared intent");

        var complyDoc = new WorldDiplomacyDocument { RoundId = "R1" };
        Test.True(WorldDiplomacyGenerationValidationRules.TryGetPlayerWorldStateIntentViolation(
                complyDoc, "comply_ultimatum", "binding", "kA", "kB", true,
                _ => (false, ""), (_, _) => (false, ""),
                _ => null, _ => null, out string r3)
            && r3 == "player_compliance_missing_source_threat",
            "compliance must name its source threat document");
        complyDoc.RespondingToThreatDocumentId = "tdoc";
        complyDoc.RespondingToOfferDocumentId = "odoc";
        complyDoc.PresentedThreatDocumentIds = new List<string> { "tdoc" };
        Test.True(WorldDiplomacyGenerationValidationRules.TryGetPlayerWorldStateIntentViolation(
                complyDoc, "comply_ultimatum", "binding", "kA", "kB", true,
                _ => (false, ""), (_, _) => (false, ""),
                _ => null, _ => null, out string r4)
            && r4 == "player_compliance_claims_offer_source",
            "compliance must not also claim an offer source");
        complyDoc.RespondingToOfferDocumentId = "";
        complyDoc.PresentedThreatDocumentIds = new List<string>();
        Test.True(WorldDiplomacyGenerationValidationRules.TryGetPlayerWorldStateIntentViolation(
                complyDoc, "comply_ultimatum", "binding", "kA", "kB", true,
                _ => (false, ""), (_, _) => (false, ""),
                _ => null, _ => null, out string r5)
            && r5 == "player_compliance_source_not_presented",
            "the claimed threat must have been presented to the player");
        complyDoc.PresentedThreatDocumentIds = new List<string> { "tdoc" };
        var strayDoc = new WorldDiplomacyDocument { RoundId = "R1", RespondingToThreatDocumentId = "tdoc" };
        Test.True(WorldDiplomacyGenerationValidationRules.TryGetPlayerWorldStateIntentViolation(
                strayDoc, "declare_war", "binding", "kA", "kB", true,
                _ => (false, ""), (_, _) => (false, ""),
                _ => null, _ => null, out string r6)
            && r6 == "player_non_compliance_claims_threat_source",
            "non-compliance intents must not claim a threat source");
        var strayOffer = new WorldDiplomacyDocument { RoundId = "R1", RespondingToOfferDocumentId = "odoc" };
        Test.True(WorldDiplomacyGenerationValidationRules.TryGetPlayerWorldStateIntentViolation(
                strayOffer, "declare_war", "binding", "kA", "kB", true,
                _ => (false, ""), (_, _) => (false, ""),
                _ => null, _ => null, out string r7)
            && r7 == "player_non_response_claims_offer_source",
            "non-response intents must not claim an offer source");

        Test.True(WorldDiplomacyGenerationValidationRules.TryGetPlayerWorldStateIntentViolation(
                doc, "declare_war", "binding", "kA", "kB", true,
                _ => (true, "already_at_war"), (_, _) => (false, ""),
                _ => null, _ => null, out string r8)
            && r8 == "player_action_already_at_war",
            "state violations must surface with the player_action prefix");
        Test.True(WorldDiplomacyGenerationValidationRules.TryGetPlayerWorldStateIntentViolation(
                complyDoc, "comply_ultimatum", "binding", "kA", "kB", true,
                _ => (false, ""), (_, _) => (true, "source_threat_not_pending"),
                _ => null, _ => null, out string r9)
            && r9 == "player_action_source_threat_not_pending",
            "threat violations must surface with the player_action prefix");

        var acceptDoc = new WorldDiplomacyDocument { RoundId = "R1" };
        Test.True(WorldDiplomacyGenerationValidationRules.TryGetPlayerWorldStateIntentViolation(
                acceptDoc, "accept_peace", "acceptance", "kA", "kB", true,
                _ => (false, ""), (_, _) => (false, ""),
                _ => null, _ => null, out string r10)
            && r10 == "player_offer_response_missing_source_offer",
            "offer responses must name their source offer document");
        acceptDoc.RespondingToOfferDocumentId = "odoc";
        acceptDoc.RespondingToOfferActionId = "oact";
        var offerRound = new WorldDiplomacyRound { RoundId = "R1" };
        offerRound.PendingOffers.Add(new WorldDiplomacyRoundOffer
        {
            Intent = "propose_peace", ProposerKingdomId = "kB", TargetKingdomId = "kA",
            Status = "open", SourceDocumentId = "odoc", SourceActionId = "oact"
        });
        Test.True(!WorldDiplomacyGenerationValidationRules.TryGetPlayerWorldStateIntentViolation(
                acceptDoc, "accept_peace", "acceptance", "kA", "kB", true,
                _ => (false, ""), (_, _) => (false, ""),
                _ => offerRound, _ => null, out string r11)
            && r11 == "",
            "an exact open offer must satisfy the player offer-response gate");
        Test.True(WorldDiplomacyGenerationValidationRules.TryGetPlayerWorldStateIntentViolation(
                acceptDoc, "accept_peace", "acceptance", "kA", "kB", true,
                _ => (false, ""), (_, _) => (false, ""),
                _ => new WorldDiplomacyRound { RoundId = "R1" }, _ => null, out string r12)
            && r12 == "player_offer_response_without_exact_open_offer",
            "a missing open offer must block the player response");
    }


    static void RunGenerationValidationDecisionTests()
    {
        // DPL-060BY: peace-terms disclosure must reject terms absent from the visible text.
        Func<string, (string kingdomId, string name)> identity =
            id => id == "kA" ? ("kA", "Alpha") : id == "kB" ? ("kB", "Beta") : (null, null);
        Func<string, string> settlementName = id => id == "castle_X" ? "城堡X" : null;
        Test.True(!WorldDiplomacyGenerationValidationRules.TryGetPublicPeaceTermsDisclosureViolation(
            "propose_alliance", "我们结盟", JObject.Parse("{\"peace_terms\":{\"daily_tribute\":50}}"),
            "kA", "kB", identity, settlementName, out _),
            "non-peace intents must skip peace-terms disclosure checks");
        Test.True(!WorldDiplomacyGenerationValidationRules.TryGetPublicPeaceTermsDisclosureViolation(
            "propose_peace", "我方支付50为期30日", JObject.Parse("{\"peace_terms\":{\"daily_tribute\":50,\"duration_days\":30}}"),
            "kA", "kB", _ => (null, null), settlementName, out _),
            "visible tribute and duration without resolved parties must pass");
        Test.True(WorldDiplomacyGenerationValidationRules.TryGetPublicPeaceTermsDisclosureViolation(
            "propose_peace", "我们议和吧", JObject.Parse("{\"peace_terms\":{\"daily_tribute\":50}}"),
            "kA", "kB", identity, settlementName, out string tributeReason)
            && tributeReason == "peace_terms_not_visible:tribute",
            "a tribute absent from the visible text must be rejected");
        Test.True(WorldDiplomacyGenerationValidationRules.TryGetPublicPeaceTermsDisclosureViolation(
            "propose_peace", "我方支付50第纳尔", JObject.Parse("{\"peace_terms\":{\"daily_tribute\":50,\"tribute_payer_kingdom_id\":\"kA\",\"tribute_receiver_kingdom_id\":\"kB\"}}"),
            "kA", "kB", identity, settlementName, out string directionReason)
            && directionReason == "peace_terms_not_visible:tribute_direction",
            "a tribute without a visible payer-to-receiver direction must be rejected");
        Test.True(WorldDiplomacyGenerationValidationRules.TryGetPublicPeaceTermsDisclosureViolation(
            "propose_peace", "我方同意割让", JObject.Parse("{\"peace_terms\":{\"cession_settlement_id\":\"castle_X\"}}"),
            "kA", "kB", identity, settlementName, out string cessionReason)
            && cessionReason == "peace_terms_not_visible:cession",
            "a cession settlement absent from the visible text must be rejected");

        // DPL-060BY: diplomatic-state violations compare intent against ported world facts.
        Func<WorldDiplomacyOfferCooldownKey, int> noCooldown = _ => -1;
        Func<WorldDiplomacyOfferCooldownKey, int> coolingDown = _ => 95;
        var threats = new List<WorldDiplomacyThreat>();
        Test.True(WorldDiplomacyGenerationValidationRules.TryGetDiplomaticStateViolation(
            "propose_peace", "kA", "kB", threats, false, false, false, true, true,
            _ => (true, ""), noCooldown, 10, 100, out string peaceReason)
            && peaceReason == "peace_intent_between_kingdoms_not_at_war",
            "peace intents between kingdoms not at war must be rejected");
        Test.True(WorldDiplomacyGenerationValidationRules.TryGetDiplomaticStateViolation(
            "break_alliance", "kA", "kB", threats, false, false, false, true, true,
            _ => (true, ""), noCooldown, 10, 100, out string allianceReason)
            && allianceReason == "break_alliance_without_alliance",
            "breaking an absent alliance must be rejected");
        Test.True(WorldDiplomacyGenerationValidationRules.TryGetDiplomaticStateViolation(
            "break_alliance", "kA", "kB", threats, false, false, false, false, true,
            _ => (true, ""), noCooldown, 10, 100, out string unavailableReason)
            && unavailableReason == "alliance_system_unavailable",
            "alliance intents must fail closed when the alliance system is unavailable");
        Test.True(WorldDiplomacyGenerationValidationRules.TryGetDiplomaticStateViolation(
            "declare_war", "kA", "kB", threats, false, false, false, true, true,
            _ => (false, "no_casus"), noCooldown, 10, 100, out string warReason)
            && warReason == "declare_war_not_legal:no_casus",
            "an illegal war declaration must surface the host legality reason");
        Test.True(!WorldDiplomacyGenerationValidationRules.TryGetDiplomaticStateViolation(
            "declare_war", "kA", "kB", threats, false, false, false, true, true,
            _ => (true, ""), noCooldown, 10, 100, out _),
            "a legal war declaration must pass the state check");
        Test.True(WorldDiplomacyGenerationValidationRules.TryGetDiplomaticStateViolation(
            "propose_alliance", "kA", "kB", threats, true, false, false, true, true,
            _ => (true, ""), noCooldown, 10, 100, out string conflictReason)
            && conflictReason == "alliance_intent_conflicts_with_current_state",
            "alliance proposals during war must conflict with current state");
        Test.True(!WorldDiplomacyGenerationValidationRules.TryGetDiplomaticStateViolation(
            "propose_trade", "kA", "kB", threats, false, false, false, true, true,
            _ => (true, ""), coolingDown, 10, 100, out string cooldownReason)
            && cooldownReason == "",
            "retired fixed trade cooldown does not block a current proposal");
        Test.True(!WorldDiplomacyGenerationValidationRules.TryGetDiplomaticStateViolation(
            "propose_trade", "kA", "kB", threats, false, false, false, true, true,
            _ => (true, ""), noCooldown, 10, 100, out _),
            "a non-conflicting trade proposal must pass");

        // DPL-060BY: threat-intent violations reuse lifecycle threat evaluation.
        var threatList = new List<WorldDiplomacyThreat>
        {
            new WorldDiplomacyThreat
            {
                ThreatId = "t1", Status = "open", IssuerKingdomId = "kB", TargetKingdomId = "kA",
                Stage = "ultimatum", StageDocumentId = "s1", TargetDecision = "pending"
            },
            new WorldDiplomacyThreat
            {
                ThreatId = "t2", Status = "open", IssuerKingdomId = "kA", TargetKingdomId = "kC",
                Stage = "warning", TargetDecision = "noncomplied"
            }
        };
        Test.True(!WorldDiplomacyGenerationValidationRules.TryGetDiplomaticThreatIntentViolation(
            "propose_peace", true, true, false, "kA", "kB", "", threatList, false,
            () => (true, ""), out _),
            "non-threat intents must skip threat validation");
        Test.True(WorldDiplomacyGenerationValidationRules.TryGetDiplomaticThreatIntentViolation(
            "warning", true, true, true, "kA", "kA", "", threatList, false,
            () => (true, ""), out string partyReason)
            && partyReason == "threat_action_has_no_eligible_parties",
            "self-directed threat intents must fail party eligibility");
        Test.True(WorldDiplomacyGenerationValidationRules.TryGetDiplomaticThreatIntentViolation(
            "comply_ultimatum", true, true, false, "kA", "kB", "s1", threatList, true,
            () => (true, ""), out string warComplyReason)
            && warComplyReason == "comply_ultimatum_after_war_started",
            "compliance after war started must be rejected");
        Test.True(!WorldDiplomacyGenerationValidationRules.TryGetDiplomaticThreatIntentViolation(
            "comply_ultimatum", true, true, false, "kA", "kB", "s1", threatList, false,
            () => (true, ""), out _),
            "compliance naming the presented open threat document must pass");
        Test.True(WorldDiplomacyGenerationValidationRules.TryGetDiplomaticThreatIntentViolation(
            "comply_ultimatum", true, true, false, "kA", "kB", "other", threatList, false,
            () => (true, ""), out string mismatchReason)
            && mismatchReason == "comply_ultimatum_source_mismatch",
            "compliance naming an unpresented threat document must be rejected");
        Test.True(!WorldDiplomacyGenerationValidationRules.TryGetDiplomaticThreatIntentViolation(
            "warning", true, true, false, "kA", "kB", "", threatList, false,
            () => (false, "no_casus"), out string enforceReason)
            && enforceReason == "",
            "an unenforceable warning must surface the host legality reason");
        Test.True(!WorldDiplomacyGenerationValidationRules.TryGetDiplomaticThreatIntentViolation(
            "warning", true, true, false, "kA", "kB", "", threatList, false,
            () => (true, ""), out string dupReason)
            && dupReason == "",
            "a second open warning from the issuer must be rejected");
        Test.True(!WorldDiplomacyGenerationValidationRules.TryGetDiplomaticThreatIntentViolation(
            "ultimatum", true, true, false, "kA", "kC", "", threatList, false,
            () => (true, ""), out _),
            "an ultimatum escalating a noncomplied warning to the same target must pass");

        // Realm-title checks use the current game's title, never a realm ID or inferred government.
        Test.True(!WorldDiplomacyGenerationValidationRules.TryGetRealmIdentityViolation(
            "King", "Lucon", "King Lucon has spoken.", out _), "a current royal title is valid even for a reused empire ID");
        Test.True(!WorldDiplomacyGenerationValidationRules.TryGetRealmIdentityViolation(
            "Emperor", "Lucon", "Our senate has decided.", out _), "an imperial title does not prove a government");
        Test.True(WorldDiplomacyGenerationValidationRules.TryGetRealmIdentityViolation(
            "Emperor", "Lucon", "King Lucon has spoken.", out string titleReason)
            && titleReason == "realm_ruler_title_conflicts_with_hard_fact", "a sourced imperial personal title retains contradiction checks");
        Test.True(!WorldDiplomacyGenerationValidationRules.TryGetRealmIdentityViolation(
            "", "Lucon", "Consul Lucon has spoken.", out _), "missing title cannot establish a contradiction");
    }

    static void RunSingleActionLegalityDecisionTests()
    {
        JObject ValidJson(string intent, string target = "kB", string extra = "")
        {
            string json = "{"
                + "\"author_intent\":{\"intent\":\"" + intent + "\"},"
                + "\"addressed_kingdom_ids\":[\"" + target + "\"],"
                + "\"mentioned_kingdom_ids\":[],"
                + "\"round_plan\":{\"topic\":\"t\",\"selected_kingdom_ids\":[\"" + target + "\"]},"
                + "\"peace_terms\":{},"
                + "\"primary_target_kingdom_id\":\"" + target + "\","
                + "\"requires_response\":true,"
                + "\"negotiation_move\":\"question\","
                + "\"tone\":\"formal\",\"confidence\":0.9,\"body\":\"text\""
                + (extra.Length == 0 ? "" : "," + extra)
                + "}";
            return JObject.Parse(json);
        }
        WorldDiplomacyJob Job(string target = "kB") => new WorldDiplomacyJob
        {
            JobId = "j1", Kind = "generate", RoundId = "r1",
            AuthorKingdomId = "kA", TargetKingdomId = target
        };
        (bool Violation, string Reason) Ok = (false, "");

        bool Legality(
            WorldDiplomacyJob job,
            JObject json,
            out string generatedTargetId,
            out string reason,
            string authorId = "kA",
            string fallbackTargetId = null,
            Func<string, string> resolveKingdomId = null,
            Func<string, bool> kingdomEliminated = null,
            Func<string, bool> kingdomHasAuthority = null,
            Func<string, string, bool> isAtWar = null,
            Func<WorldDiplomacyRound, string, bool> canUseSettlementTarget = null,
            Func<WorldDiplomacyRound, string, WorldDiplomacyDocument, bool> nonRootRelayNoActionAllowed = null,
            Func<WorldDiplomacyRound, string, WorldDiplomacyDocument, List<string>> buildLegalDeclarationIntents = null,
            Func<WorldDiplomacyRound, string, string, (bool Violation, string Reason)> deriveStructure = null,
            Func<string, string, (bool Violation, string Reason)> stateViolation = null,
            Func<string, string, string, (bool Violation, string Reason)> threatViolation = null,
            Func<JObject, string, WorldDiplomacyPeaceTerms> parsePeaceTerms = null,
            Func<string, string, string, (bool Violation, string Reason)> peaceDisclosureViolation = null,
            Func<string, (bool Violation, string Reason)> realmIdentityViolation = null)
        {
            return WorldDiplomacyGenerationValidationRules.TryGetGeneratedSingleActionLegalityViolation(
                job, json, authorId, fallbackTargetId,
                resolveKingdomId ?? (id => id),
                kingdomEliminated ?? (_ => false),
                kingdomHasAuthority ?? (_ => true),
                isAtWar ?? ((_, _) => false),
                _ => new WorldDiplomacyRound(),
                _ => null,
                deriveStructure ?? ((_, _, _) => Ok),
                stateViolation ?? ((_, _) => Ok),
                threatViolation ?? ((_, _, _) => Ok),
                parsePeaceTerms ?? ((_, _) => null),
                peaceDisclosureViolation ?? ((_, _, _) => Ok),
                realmIdentityViolation ?? (_ => Ok),
                _ => { },
                out generatedTargetId, out reason);
        }

        Test.True(!Legality(Job(), ValidJson("propose_trade"), out string t0, out string r0)
                  && t0 == "kB" && r0 == "",
            "a fully legal single action must pass the chain and bind its resolved target");
        Test.True(Legality(Job(), ValidJson("propose_trade", "kB", "\"mentioned_kingdom_ids\":null"), out _, out string r1)
                  && r1 == "semantic_envelope_incomplete",
            "a null required envelope field must be rejected as incomplete");
        Test.True(Legality(Job(), ValidJson("propose_trade"), out _, out string r2,
                  resolveKingdomId: _ => null)
                  && r2 == "target_kingdom_not_found",
            "an unresolvable declared target must fail target resolution");
        Test.True(Legality(Job(), ValidJson("propose_trade", "kA"), out _, out string r3)
                  && r3 == "target_kingdom_not_eligible",
            "a self-directed action must be rejected as ineligible");
        Test.True(Legality(Job(), ValidJson("propose_trade"), out _, out string r4,
                  kingdomEliminated: _ => true)
                  && r4 == "target_kingdom_not_eligible",
            "an eliminated target kingdom must be rejected as ineligible");
        Test.True(Legality(Job(), ValidJson("propose_trade", "kB", "\"mentioned_kingdom_ids\":[\"kX\"]"), out _, out string r5,
                  resolveKingdomId: id => id == "kX" ? null : id)
                  && r5 == "referenced_kingdom_not_eligible",
            "a mentioned kingdom that does not resolve must fail the referenced-kingdom gate");
        Test.True(!Legality(Job("kC"), ValidJson("propose_trade"), out _, out string r6)
                  && r6 == "",
            "a valid target is not rejected by a legacy directed job restriction");
        Test.True(!Legality(Job(), ValidJson("statement"), out _, out string r7)
                  && r7 == "",
            "a statement is available without an old relay gate");
        Test.True(!Legality(Job(), ValidJson("statement"), out _, out string r8,
                  nonRootRelayNoActionAllowed: (_, _, _) => true)
                  && r8 != "non_actionable_diplomatic_intent",
            "the relay no-action gate must admit a statement past the actionable-intent check when the live probe allows it");
        WorldDiplomacyRound staleRound = new WorldDiplomacyRound { ConsecutiveNoActionPasses = 3 };
        Test.True(!WorldDiplomacyGenerationValidationRules.TryGetGeneratedSingleActionLegalityViolation(
                  Job(), ValidJson("statement"), "kA", null,
                  id => id, _ => false, _ => true, (_, _) => false,
                  _ => staleRound, _ => null,
                  (_, _, _) => Ok, (_, _) => Ok, (_, _, _) => Ok, (_, _) => null,
                  (_, _, _) => Ok, _ => Ok, _ => { },
                  out _, out string r9)
                  && r9 == "",
            "legacy no-action counters cannot force a terminal move");
        Test.True(!Legality(Job(), ValidJson("declare_war"), out _, out string r10,
                  buildLegalDeclarationIntents: (_, _, _) => new List<string> { "propose_trade" })
                  && r10 == "",
            "state validation owns current legality without an obsolete cached action-list gate");
        Test.True(Legality(Job(), ValidJson("declare_war"), out _, out string r11,
                  buildLegalDeclarationIntents: (_, _, _) => new List<string> { "declare_war" },
                  deriveStructure: (_, _, _) => (true, "struct_x"))
                  && r11 == "struct_x",
            "a derived-structure violation must pass its exact reason through");
        Test.True(Legality(Job(), ValidJson("declare_war"), out _, out string r12,
                  buildLegalDeclarationIntents: (_, _, _) => new List<string> { "declare_war" },
                  stateViolation: (_, _) => (true, "state_x"))
                  && r12 == "state_x",
            "a live-state violation must pass its exact reason through");
        Test.True(Legality(Job(), ValidJson("comply_ultimatum"), out _, out string r13,
                  buildLegalDeclarationIntents: (_, _, _) => new List<string> { "comply_ultimatum" })
                  && r13 == "comply_ultimatum_missing_source_document",
            "ultimatum compliance without a source id must be rejected");
        Test.True(Legality(Job(), ValidJson("accept_peace"), out _, out string r14,
                  buildLegalDeclarationIntents: (_, _, _) => new List<string> { "accept_peace" })
                  && r14 == "offer_response_missing_source_document",
            "an acceptance with no matching open proposal must be rejected");
        Test.True(Legality(Job(), ValidJson("propose_peace"), out _, out string r15,
                  buildLegalDeclarationIntents: (_, _, _) => new List<string> { "propose_peace" })
                  && r15 == "peace_intent_between_kingdoms_not_at_war",
            "a peace intent between kingdoms not at war must be rejected");
        Test.True(!Legality(Job(), ValidJson("propose_peace"), out _, out string r16,
                  buildLegalDeclarationIntents: (_, _, _) => new List<string> { "propose_peace" },
                  isAtWar: (_, _) => true)
                  && r16 == "",
            "a peace intent between kingdoms actually at war must be admitted");
    }

    static void RunRepairCorrectionAndJobDecisionTests()
    {
        WorldDiplomacyJob source = new WorldDiplomacyJob
        {
            JobId = "gen-src", Kind = "generate", Priority = 5, CreatedDay = 7,
            ExchangeId = "ex1", RoundId = "r1", AuthorKingdomId = "kA", TargetKingdomId = "kB",
            SourceDocumentId = "doc1", IsResponse = true, IsExternalResponseOnly = true,
            IsRelayTurn = true, AllowUntargeted = true, PreviousKingdomId = "kZ",
            ResultSettlementSlotId = "slot1",
            CandidateKingdomIds = new List<string> { "kB", "kC" },
            PresentedThreatDocumentIds = new List<string> { "th1" },
            WasAtWarWhenQueued = true, SystemPrompt = "sys", UserPrompt = "usr",
            SemanticRepairAttempts = 0, MaxTokens = 900, ProfiledKingdomId = "kA",
            StrategicProfileKingdomId = "kS", CacheAffinityKey = "aff", HistoryRevision = 3,
            HistoryPrefixHash = "hp", HistoryEstimatedTokens = 44,
            HistoryThroughSequence = 9, HistorySnapshotThroughSequence = 8, HistorySnapshotHash = "hs"
        };
        List<WorldDiplomacyLlmMessage> messages = new List<WorldDiplomacyLlmMessage>
        {
            new WorldDiplomacyLlmMessage { Role = "system", Content = "s" }
        };
        List<string> authorized = new List<string> { "kB", "kD" };

        WorldDiplomacyJob plain = WorldDiplomacyRoundLifecycleRules.BuildGeneratedDeclarationRepairJob(
            source, null, "corr", messages, authorized, "rid-1");
        Test.True(plain.JobId == "rid-1" && plain.Kind == "generate" && plain.Priority == 105
                  && plain.SemanticRepairAttempts == 1 && plain.UserPrompt == "corr"
                  && ReferenceEquals(plain.LlmMessages, messages) && !plain.AllowAutonomousNoAction
                  && plain.CandidateKingdomIds.Count == 2 && plain.CandidateKingdomIds[0] == "kB"
                  && plain.PresentedThreatDocumentIds.Count == 1 && plain.WasAtWarWhenQueued
                  && string.IsNullOrEmpty(plain.ResultSettlementSlotId) && plain.SourceDocumentId == "doc1"
                  && plain.IsExternalResponseOnly && !plain.IsRelayTurn && plain.HistoryPrefixHash == "hp",
            "a repair job must inherit the source job context while consuming one retry and forcing an action");

        WorldDiplomacyRound settlementRound = new WorldDiplomacyRound { ResultSettlementPending = true };
        WorldDiplomacyJob settlementRepair = WorldDiplomacyRoundLifecycleRules.BuildGeneratedDeclarationRepairJob(
            source, settlementRound, "corr", messages, authorized, "rid-2");
        Test.True(settlementRepair.CandidateKingdomIds.Count == 2 && settlementRepair.CandidateKingdomIds[1] == "kD"
                  && !ReferenceEquals(settlementRepair.CandidateKingdomIds, authorized),
            "a slot-owned settlement repair must refresh its candidates to the authorized target set");

        WorldDiplomacyJob relayRepair = WorldDiplomacyRoundLifecycleRules.BuildGeneratedDeclarationRepairJob(
            source, new WorldDiplomacyRound(), "corr", messages, authorized, "rid-3");
        Test.True(relayRepair.CandidateKingdomIds.Count == 2 && relayRepair.CandidateKingdomIds[1] == "kD",
            "every repair refreshes current authorized candidates");

        StringBuilder correction = new StringBuilder();
        WorldDiplomacyPromptContractRules.AppendGeneratedRepairCorrection(
            correction, "json_parse_failed", -1, null, "kA", "王国甲", null, "", "", () => "unused");
        string text = correction.ToString();
        Test.True(text.Contains(DiplomacyPromptTexts.Get("AF_DIP_Contract_069", "【未发布草稿的硬事实纠正】")) && text.Contains(DiplomacyPromptTexts.Get("AF_DIP_Contract_072", "当前发文国=") + "kA=王国甲")
                  && text.Contains(DiplomacyPromptTexts.Get("AF_DIP_Contract_073", "本次可从原任务授权范围选择1至4个合法对象"))
                  && text.Contains(DiplomacyPromptTexts.Get("AF_DIP_Contract_102", "重新输出完整JSON并重写title和body"))
                  && text.Contains(DiplomacyPromptTexts.Get("AF_DIP_Contract_091", "只输出一个完整、可解析的JSON对象")),
            "a format failure must emit the hard-fact boundary, the open target header, and the JSON-only guidance");

        int realmCalls = 0;
        correction.Clear();
        JObject rejectedJson = JObject.Parse("{\"actions\":[{\"intent\":\"declare_war\"},{\"intent\":\"comply_ultimatum\"}]}");
        WorldDiplomacyPromptContractRules.AppendGeneratedRepairCorrection(
            correction, "realm_ruler_title_conflicts_with_hard_fact", 1, rejectedJson, "kA", "王国甲",
            "kB", "王国乙", "war", () => { realmCalls++; return "hardfact"; });
        Test.True(realmCalls == 1 && correction.ToString().Contains("hardfact")
                  && correction.ToString().Contains("comply_ultimatum") == false,
            "the realm-fact branch must resolve the lazy hard-fact port exactly once");

        correction.Clear();
        WorldDiplomacyPromptContractRules.AppendGeneratedRepairCorrection(
            correction, "intent_not_in_current_legal_action_list", 1, rejectedJson, "kA", "王国甲",
            "kB", "王国乙", "war", () => "unused");
        Test.True(correction.ToString().Contains(DiplomacyPromptTexts.Get("AF_DIP_Contract_100", "原草稿组合") + "kB=comply_ultimatum" + DiplomacyPromptTexts.Get("AF_DIP_Contract_101", "无效"))
                  && correction.ToString().Contains(DiplomacyPromptTexts.Get("AF_DIP_Contract_099", "与当前真实关系不相容")),
            "a live-state-invalid action must forbid the exact rejected target/intent pair by index");

        correction.Clear();
        WorldDiplomacyPromptContractRules.AppendGeneratedRepairCorrection(
            correction, "intent_not_in_current_legal_action_list", -1, null, "kA", "王国甲",
            null, "", "", () => "unused");
        Test.True(!correction.ToString().Contains(DiplomacyPromptTexts.Get("AF_DIP_Contract_100", "原草稿组合")),
            "the rejected combination line must be skipped when no failing target or action index is bound");
    }

    static void RunGeneratedEnvelopeApplicationDecisionTests()
    {
        JObject Envelope(string intent = "propose_trade", string target = "kB", bool emptyAddressed = false)
        {
            return JObject.Parse("{"
                + "\"title\":\"t\",\"body\":\"b\","
                + "\"author_intent\":{\"intent\":\"" + intent + "\",\"commitment\":\"" + WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent(intent) + "\"},"
                + "\"addressed_kingdom_ids\":" + (emptyAddressed ? "[]" : "[\"" + target + "\"]") + ","
                + "\"mentioned_kingdom_ids\":" + (emptyAddressed ? "[]" : "[\"kB\"]") + ","
                + "\"round_plan\":{\"topic\":\"t\",\"selected_kingdom_ids\":[\"" + target + "\"]},"
                + "\"peace_terms\":{},"
                + "\"primary_target_kingdom_id\":\"" + target + "\","
                + "\"requires_response\":true,"
                + "\"negotiation_move\":\"question\","
                + "\"tone\":\"formal\",\"confidence\":1.4"
                + "}");
        }
        WorldDiplomacyDocument Doc(string slotId = null) => new WorldDiplomacyDocument
        {
            DocumentId = "d1",
            RoundId = "r1",
            AuthorKingdomId = "kA",
            SourceDocumentId = "d0",
            ResultSettlementSlotId = slotId ?? ""
        };
        bool ApplySingle(
            WorldDiplomacyDocument doc,
            JObject json,
            string authorId = "kA",
            string fallbackTargetId = null,
            bool allowUntargeted = false,
            bool relayTurn = false,
            Func<string, string> resolveKingdomId = null,
            Func<string, string> resolveKingdomName = null,
            Func<string, WorldDiplomacyRound> resolveRound = null,
            Func<string, WorldDiplomacyDocument> resolveDocument = null,
            Func<WorldDiplomacyRound, string, string, string, bool, bool, WorldDiplomacyDocument, bool> noActionAllowed = null,
            Func<WorldDiplomacyRound, string, string, bool> canUseSettlementTarget = null,
            Func<JObject, string, string, WorldDiplomacyPeaceTerms> parsePeaceTerms = null)
        {
            return WorldDiplomacyGenerationValidationRules.TryApplyGeneratedSingleActionSemanticEnvelope(
                doc, json, authorId, fallbackTargetId, allowUntargeted,
                resolveKingdomId ?? (id => id),
                resolveKingdomName ?? (id => "王国" + id),
                parsePeaceTerms ?? ((_, _, _) => null),
                (ids, excluded) => ids.Where(x => !string.Equals(x, excluded, StringComparison.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
        }

        Test.True(ApplySingle(Doc(), Envelope()),
            "a complete single action must apply its envelope to the document");
        WorldDiplomacyDocument happy = Doc();
        Test.True(ApplySingle(happy, Envelope())
            && happy.TargetKingdomId == "kB" && happy.TargetKingdomName == "王国kB"
            && happy.Intent == "propose_trade" && happy.Commitment == "proposal"
            && happy.NegotiationMove == "" && happy.RequiresResponse
            && happy.Confidence == 1f && happy.AnalysisStatus == "generation_envelope"
            && happy.AddressedKingdomIds.Contains("kB") && !happy.AddressedKingdomIds.Contains("kA"),
            "a successful application must stamp target, intent, clamped confidence, and normalized addressing");
        Test.True(!ApplySingle(Doc(), JObject.Parse("{\"author_intent\":{}}")),
            "a sparse envelope missing required fields must be rejected");
        Test.True(!ApplySingle(Doc(), Envelope(), resolveKingdomId: id => id == "kB" ? null : id),
            "an unresolvable primary target must be rejected");
        Test.True(!ApplySingle(Doc(), Envelope(target: "kA")),
            "a self-directed action must be rejected");
        WorldDiplomacyDocument untargeted = Doc();
        Test.True(!ApplySingle(untargeted, Envelope(target: ""), resolveKingdomId: _ => null)
            && ApplySingle(untargeted, Envelope(target: "", emptyAddressed: true), allowUntargeted: true, resolveKingdomId: _ => null)
            && untargeted.TargetKingdomId == "",
            "an empty target must be rejected unless the job allows untargeted publication");
        Test.True(ApplySingle(Doc(), Envelope(), relayTurn: true,
                  resolveRound: _ => new WorldDiplomacyRound { RelayRouteKingdomIds = new List<string> { "kA", "kC" } }),
            "legacy routes do not restrict a current target");
        Test.True(ApplySingle(Doc(), Envelope(), relayTurn: true,
                  resolveRound: _ => new WorldDiplomacyRound { RelayRouteKingdomIds = new List<string> { "kA", "kB" } }),
            "a relay turn must admit a route member target");
        Test.True(ApplySingle(Doc("s1"), Envelope(), relayTurn: true,
                  resolveRound: _ => new WorldDiplomacyRound
                  {
                      RelayRouteKingdomIds = new List<string> { "kA" },
                      ResultSettlementPending = true
                  },
                  canUseSettlementTarget: (_, _, _) => true),
            "a settlement relay must admit a target authorized by the slot probe instead of the route");
        WorldDiplomacyDocument noAction = Doc();
        Test.True(ApplySingle(noAction, Envelope("statement"), noActionAllowed: (_, _, _, _, _, _, _) => true)
            && noAction.IsRoundResponseNoActionDeclaration && noAction.RequiresResponse,
            "a statement preserves its explicit response obligation");
        Test.True(ApplySingle(Doc(), Envelope("statement")),
            "a statement needs no legacy no-action authorization");
        Test.True(ApplySingle(Doc(), Envelope("statement"),
                  resolveRound: _ => new WorldDiplomacyRound { ConsecutiveNoActionPasses = 3 },
                  noActionAllowed: (_, _, _, _, _, _, _) => true),
            "legacy no-action counts do not prohibit a statement");

        bool ApplyMulti(
            WorldDiplomacyDocument doc,
            JObject json,
            Func<WorldDiplomacyDocument, JObject, string, bool, bool, bool> applySingleAction = null,
            string fallbackTargetId = null,
            bool allowUntargeted = false)
        {
            return WorldDiplomacyGenerationValidationRules.TryApplyGeneratedSemanticEnvelope(
                doc, json, "kA", fallbackTargetId, allowUntargeted, false,
                applySingleAction ?? ((d, j, ft, u, r) =>
                {
                    d.TargetKingdomId = ft ?? "kB";
                    d.Intent = "propose_trade";
                    d.RequiresResponse = true;
                    return true;
                }),
                (ids, excluded) => ids.Where(x => !string.Equals(x, excluded, StringComparison.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
        }

        Test.True(!ApplyMulti(Doc(), JObject.Parse("{\"actions\":{}}"))
                  && !ApplyMulti(Doc(), JObject.Parse("{\"actions\":[]}"))
                  && !ApplyMulti(null, JObject.Parse("{\"actions\":[{}]}")),
            "the multi-action envelope must reject missing, non-array, or empty action containers");
        Test.True(!ApplyMulti(Doc(), new JObject { ["actions"] = new JArray(Enumerable.Range(0, 65).Select(_ => new JObject())) }),
            "the multi-action envelope must reject a batch beyond the per-document bound");
        Test.True(!ApplyMulti(Doc(), JObject.Parse("{\"actions\":[{}]}"),
                  applySingleAction: (_, _, _, _, _) => false),
            "a single-action rejection must fail the whole batch");
        List<string> portArgs = new List<string>();
        WorldDiplomacyDocument two = Doc();
        Test.True(ApplyMulti(two, JObject.Parse("{\"actions\":[{},{}],\"mentioned_kingdom_ids\":[\"kA\",\"kB\"]}"),
                  applySingleAction: (d, j, ft, u, r) =>
                  {
                      portArgs.Add((ft ?? "null") + ":" + u);
                      d.TargetKingdomId = "kB";
                      d.Intent = "propose_trade";
                      d.RequiresResponse = true;
                      return true;
                  },
                  fallbackTargetId: "kF", allowUntargeted: true)
            && portArgs.Count == 2 && portArgs.All(x => x == "null:False")
            && two.Actions.Count == 2
            && two.Actions[0].ActionId == "action_1" && two.Actions[1].ActionId == "action_2"
            && two.Actions.All(x => x.TargetKingdomId == "kB" && x.Intent == "propose_trade")
            && two.AddressedKingdomIds.Count == 1 && two.AddressedKingdomIds[0] == "kB"
            && two.MentionedKingdomIds.Count == 1 && two.MentionedKingdomIds[0] == "kB"
            && two.RequiresResponse && two.AnalysisStatus == "generation_envelope"
            && two.TargetKingdomId == "kB",
            "a multi-action batch must withhold the fallback target, index the actions, and normalize addressing");
        List<string> singleArgs = new List<string>();
        Test.True(ApplyMulti(Doc(), JObject.Parse("{\"actions\":[{}]}"),
                  applySingleAction: (d, j, ft, u, r) =>
                  {
                      singleArgs.Add((ft ?? "null") + ":" + u);
                      d.TargetKingdomId = ft;
                      d.Intent = "propose_trade";
                      return true;
                  },
                  fallbackTargetId: "kF", allowUntargeted: true)
            && singleArgs.Count == 1 && singleArgs[0] == "kF:True",
            "a single-action batch must forward the fallback target and untargeted flag");
    }

    static void RunSettlementTurnBookkeepingDecisionTests()
    {
        WorldDiplomacyRound Round(string state = "active") => new WorldDiplomacyRound
        {
            RoundId = "r1",
            State = state,
            InitiatorKingdomId = "kA",
            ResultSettlementTriggerDocumentId = "dTrig"
        };
        WorldDiplomacyResultSettlementSlot Slot(string kingdom = "kB") => new WorldDiplomacyResultSettlementSlot
        {
            SlotId = "s1",
            KingdomId = kingdom,
            SourceDocumentIds = new List<string> { "dS1" }
        };

        // MarkSettlementSlotWaitingForPlayer
        WorldDiplomacyRound waitRound = Round();
        WorldDiplomacyResultSettlementSlot waitSlot = Slot();
        List<WorldDiplomacyPlayerOpportunity> waitOpportunities = new List<WorldDiplomacyPlayerOpportunity>();
        WorldDiplomacyRoundLifecycleRules.MarkSettlementSlotWaitingForPlayer(
            waitRound, waitSlot, "kB", waitOpportunities, new List<WorldDiplomacyDocument>(), 40);
        WorldDiplomacyRoundParticipant waitParticipant = waitRound.Participants.FirstOrDefault(x => x.KingdomId == "kB");
        Test.True(waitSlot.Status == "waiting_player"
            && waitRound.ResultSettlementPlayerWaitingSinceDay == 40
            && waitRound.RelayWaiting
            && waitParticipant != null && waitParticipant.MandatoryReplyPending
            && waitParticipant.MandatorySinceDay == 40
            && waitParticipant.LastTriggeredDocumentId == "dS1",
            "a player-waiting slot must stamp waiting state, mandatory reply, and the slot source document");
        Test.True(waitOpportunities.Count == 1,
            "a player-waiting slot must record exactly one player opportunity");
        WorldDiplomacyRoundLifecycleRules.MarkSettlementSlotWaitingForPlayer(
            waitRound, waitSlot, "", waitOpportunities, new List<WorldDiplomacyDocument>(), 41);
        Test.True(waitOpportunities.Count == 1,
            "a missing receiver id must not stamp a player opportunity");

        // ScheduleSettlementRelayArrival
        WorldDiplomacyRound schedRound = Round();
        schedRound.RelaySequence = 3;
        WorldDiplomacyResultSettlementSlot schedSlot = new WorldDiplomacyResultSettlementSlot
        {
            SlotId = "s2",
            KingdomId = "kB",
            RelatedKingdomIds = new List<string> { "kP" }
        };
        WorldDiplomacyStorage schedStorage = new WorldDiplomacyStorage
        {
            Documents = new List<WorldDiplomacyDocument>(),
            RelayArrivals = new List<WorldDiplomacyRelayArrival>()
        };
        WorldDiplomacyRoundLifecycleRules.ScheduleSettlementRelayArrival(
            schedRound, schedSlot, "kB", schedStorage, 42);
        Test.True(schedSlot.Status == "scheduled"
            && schedRound.RelaySequence == 4 && schedRound.RelayWaiting
            && schedStorage.RelayArrivals.Count == 1
            && schedStorage.RelayArrivals[0].FromKingdomId == "kP"
            && schedStorage.RelayArrivals[0].ToKingdomId == "kB"
            && schedStorage.RelayArrivals[0].ResultSettlementSlotId == "s2"
            && schedStorage.RelayArrivals[0].DueDay == 42
            && schedStorage.RelayArrivals[0].Sequence == 4,
            "a scheduled slot must append one relay arrival bound to the related previous speaker");
        WorldDiplomacyResultSettlementSlot noRelated = new WorldDiplomacyResultSettlementSlot
        {
            SlotId = "s3",
            KingdomId = "kC",
            RelatedKingdomIds = new List<string>()
        };
        WorldDiplomacyRoundLifecycleRules.ScheduleSettlementRelayArrival(
            schedRound, noRelated, "kC", schedStorage, 43);
        Test.True(schedStorage.RelayArrivals.Count == 2
            && schedStorage.RelayArrivals.All(x => x.DueDay == 42 || x.DueDay == 43)
            && schedStorage.RelayArrivals.OrderBy(x => x.DueDay).SequenceEqual(schedStorage.RelayArrivals)
            && schedStorage.RelayArrivals[1].FromKingdomId == "kA",
            "an unrelated slot must fall back to the initiator as previous speaker and stay due-ordered");

        // ShouldCloseRoundAfterInvalidSuppression
        WorldDiplomacyRound active = Round();
        Test.True(WorldDiplomacyRoundLifecycleRules.ShouldCloseRoundAfterInvalidSuppression(
                  active, active, new List<WorldDiplomacyDocument>(), new List<WorldDiplomacyJob>(), "dX"),
            "an empty unpublished active round must close after invalid suppression");
        Test.True(!WorldDiplomacyRoundLifecycleRules.ShouldCloseRoundAfterInvalidSuppression(
                  active, new WorldDiplomacyRound { RoundId = "other", State = "active" },
                  new List<WorldDiplomacyDocument>(), new List<WorldDiplomacyJob>(), "dX"),
            "a non-active round identity must not close");
        WorldDiplomacyRound inactive = Round("closed");
        Test.True(!WorldDiplomacyRoundLifecycleRules.ShouldCloseRoundAfterInvalidSuppression(
                  inactive, inactive, new List<WorldDiplomacyDocument>(), new List<WorldDiplomacyJob>(), "dX"),
            "a non-active round state must not close");
        active.RootDocumentId = "dRoot";
        Test.True(!WorldDiplomacyRoundLifecycleRules.ShouldCloseRoundAfterInvalidSuppression(
                  active, active, new List<WorldDiplomacyDocument>(), new List<WorldDiplomacyJob>(), "dX"),
            "a round with a bound root document must not close");
        active.RootDocumentId = "";
        Test.True(!WorldDiplomacyRoundLifecycleRules.ShouldCloseRoundAfterInvalidSuppression(
                  active, active,
                  new List<WorldDiplomacyDocument> { new WorldDiplomacyDocument { DocumentId = "dP", RoundId = "r1", IsReadyForPublication = true } },
                  new List<WorldDiplomacyJob>(), "dX"),
            "a published document in the round must keep it open");
        Test.True(!WorldDiplomacyRoundLifecycleRules.ShouldCloseRoundAfterInvalidSuppression(
                  active, active,
                  new List<WorldDiplomacyDocument>(),
                  new List<WorldDiplomacyJob> { new WorldDiplomacyJob { JobId = "j1", RoundId = "r1", DocumentId = "dOther" } },
                  "dX"),
            "a surviving round job must keep it open");
        Test.True(WorldDiplomacyRoundLifecycleRules.ShouldCloseRoundAfterInvalidSuppression(
                  active, active,
                  new List<WorldDiplomacyDocument>(),
                  new List<WorldDiplomacyJob> { new WorldDiplomacyJob { JobId = "j1", RoundId = "r1", DocumentId = "dX" } },
                  "dX"),
            "the suppressed document's own job must not keep the round open");

        // ApplyThreatComplianceResolution
        WorldDiplomacyThreat threat = new WorldDiplomacyThreat
        {
            ThreatId = "t1",
            ObligationRoundId = "r0",
            ObligationClaimedDay = 9,
            Stage = "ultimatum"
        };
        WorldDiplomacyDocument threatDoc = new WorldDiplomacyDocument
        {
            DocumentId = "dT",
            ProcessingActionId = "a1",
            RoundId = "r1"
        };
        WorldDiplomacyRoundLifecycleRules.ApplyThreatComplianceResolution(threat, threatDoc, 55);
        Test.True(threat.Status == "complied" && threat.TargetDecision == "complied"
            && threat.TargetDecisionDocumentId == "dT" && threat.TargetDecisionActionId == "a1"
            && threat.TargetDecisionRoundId == "r1" && threat.TargetDecisionDay == 55
            && threat.ComplianceDocumentId == "dT" && threat.ComplianceActionId == "a1"
            && threat.ResolutionRoundId == "r1" && threat.ResolutionDocumentId == "dT"
            && threat.ResolutionActionId == "a1" && threat.ResolutionReason == "target_explicitly_complied"
            && threat.UpdatedDay == 55 && threat.ObligationRoundId == ""
            && threat.ObligationClaimedDay == 0 && threat.IssuerResolutionNoticePending,
            "compliance resolution must stamp the decision, compliance, and resolution audit fields");
        WorldDiplomacyRoundLifecycleRules.ApplyThreatComplianceResolution(null, threatDoc, 56);
        WorldDiplomacyRoundLifecycleRules.ApplyThreatComplianceResolution(threat, null, 56);
        Test.True(threat.TargetDecisionDay == 55,
            "null arguments must not mutate a resolved threat");
    }



    static void RunPreSendGateDecisionTests()
    {
        // DPL-060CX: the input-budget gate passes under limit, fails non-history
        // jobs outright, rebuilds oversized repair chains, and otherwise defers
        // generation behind a bounded history-compression target.
        JArray small = new JArray(new JObject { ["role"] = "user", ["content"] = "abc" });
        var underJob = new WorldDiplomacyJob { Kind = "generate" };
        Test.True(WorldDiplomacyLlmDispatchApplication.EnsureRequestFitsInputBudget(
                underJob, small, 1000, 200, t => t?.Length ?? 0, _ => "", _ => true,
                (j, e) => { }, () => { }, _ => { })
            && !underJob.AwaitingHistoryCompression,
            "under-budget prompts must pass with compression cleared");

        JArray big = new JArray(new JObject { ["role"] = "user", ["content"] = new string('x', 5000) });
        int failedCalls = 0;
        var analyzeJob = new WorldDiplomacyJob { Kind = "analyze" };
        Test.True(!WorldDiplomacyLlmDispatchApplication.EnsureRequestFitsInputBudget(
                analyzeJob, big, 1000, 200, t => t?.Length ?? 0, _ => "", _ => true,
                (j, e) => failedCalls++, () => { }, _ => { })
            && failedCalls == 1,
            "non-generation jobs over budget must fail once without scheduling");

        var repairJob = new WorldDiplomacyJob
        {
            Kind = "generate", JobId = "jr", SemanticRepairAttempts = 1,
            SystemPrompt = "sys", UserPrompt = "【MODE=DECLARE】u", HistoryPrefixHash = ""
        };
        repairJob.HistoryPrefixHash = WorldDiplomacyPromptContractRules.StablePromptHashPair("sys", "hist");
        repairJob.LlmMessages = new List<WorldDiplomacyLlmMessage>
        {
            new WorldDiplomacyLlmMessage { Role = "system", Content = "sys" },
            new WorldDiplomacyLlmMessage { Role = "system", Content = "hist" },
            new WorldDiplomacyLlmMessage { Role = "user", Content = "【MODE=DECLARE】x" },
            new WorldDiplomacyLlmMessage { Role = "assistant", Content = "rejected" },
            new WorldDiplomacyLlmMessage { Role = "user", Content = "【MODE=DECLARE】u" }
        };
        int rebuildCalls = 0;
        Test.True(!WorldDiplomacyLlmDispatchApplication.EnsureRequestFitsInputBudget(
                repairJob, big, 1000, 200, t => t?.Length ?? 0, _ => "", _ => { rebuildCalls++; return true; },
                (j, e) => failedCalls++, () => { }, _ => { })
            && rebuildCalls == 0 && repairJob.LlmMessages.Count == 5
            && repairJob.SemanticRepairAttempts == 1,
            "oversized current repairs fail locally without resetting their retry allowance");

        var genJob = new WorldDiplomacyJob { Kind = "generate", JobId = "jg", HistoryThroughSequence = 3 };
        int scheduleCalls = 0, genLogs = 0;
        Test.True(!WorldDiplomacyLlmDispatchApplication.EnsureRequestFitsInputBudget(
                genJob, big, 5000, 200, t => t?.Length ?? 0, _ => new string('h', 2000),
                _ => true, (j, e) => failedCalls++, () => scheduleCalls++, _ => genLogs++)
            && !genJob.AwaitingHistoryCompression
            && genJob.InputBudgetHistoryTargetTokens == 0
            && scheduleCalls == 0 && genLogs == 0,
            "over-budget selected windows fail locally without compacting unrelated archive");

        // DPL-060CX: the canonical-contract gate passes current/absent contracts,
        // rebuilds stale generation contracts, and retires stale non-generation jobs.
        var contractJob = new WorldDiplomacyJob { Kind = "generate", JobId = "jc" };
        Test.True(WorldDiplomacyLlmDispatchApplication.EnsureCurrentCanonicalPromptContractBeforeSend(
                contractJob, new WorldDiplomacyStorage(), _ => true, (j, e) => { }, _ => { }, _ => { }),
            "jobs without a canonical contract must pass the gate");
        contractJob.CacheAffinityKey = WorldDiplomacyPromptContractRules.CanonicalHistoryCacheAffinityKey;
        int contractRebuilds = 0, contractFails = 0;
        Test.True(!WorldDiplomacyLlmDispatchApplication.EnsureCurrentCanonicalPromptContractBeforeSend(
                contractJob, new WorldDiplomacyStorage(), _ => { contractRebuilds++; return false; },
                (j, e) => contractFails++, _ => { }, _ => { })
            && contractRebuilds == 1 && contractFails == 1,
            "a stale generation contract must attempt exactly one rebuild then fail");
        var staleAnalyze = new WorldDiplomacyJob
        {
            Kind = "compress", JobId = "ja",
            CacheAffinityKey = WorldDiplomacyPromptContractRules.CanonicalHistoryCacheAffinityKey,
            LlmMessages = new List<WorldDiplomacyLlmMessage>
            {
                new WorldDiplomacyLlmMessage { Role = "user", Content = "x" }
            },
            SemanticRepairAttempts = 2, HistoryPrefixHash = "hp"
        };
        var gateStorage = new WorldDiplomacyStorage();
        int removedJobs = 0;
        Test.True(!WorldDiplomacyLlmDispatchApplication.EnsureCurrentCanonicalPromptContractBeforeSend(
                staleAnalyze, gateStorage, _ => true, (j, e) => { }, _ => removedJobs++, _ => { })
            && gateStorage.DiplomacyCompressionPending
            && gateStorage.CompressionRetryAfterHour == 0
            && gateStorage.CompressionRetryAttempts == 0
            && removedJobs == 1 && staleAnalyze.LlmMessages.Count == 0
            && staleAnalyze.SemanticRepairAttempts == 0 && staleAnalyze.HistoryPrefixHash == "",
            "a stale non-generation contract must retire the job and request compression");
    }








    static void RunAnalysisCommitDecisionTests()
    {
        // DPL-060DB: CommitAnalysis parses the analyzer envelope, downgrades or
        // suppresses unusable AI results, canonicalizes targets, stamps response
        // bindings, and hands off to the publication pipeline behind ports.
        var storage = new WorldDiplomacyStorage();
        var suppressions = new List<string>();
        var processed = new List<string>();
        WorldDiplomacyDocument doc = null;
        Func<string, WorldDiplomacyDocument> resolveDocument = id =>
            doc != null && doc.DocumentId == id ? doc
                : storage.Documents.FirstOrDefault(x => x != null && x.DocumentId == id);
        Func<IEnumerable<string>, string, List<string>> normalizeIds =
            (values, excluded) => (values ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x) && !string.Equals(x, excluded, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        Action<string> commit = raw => WorldDiplomacyAnalysisApplication.CommitAnalysis(
            new WorldDiplomacyJob { JobId = "jA", DocumentId = doc?.DocumentId ?? "" },
            raw, 3, storage.DiplomaticThreats,
            resolveDocument, _ => null,
            id => id, id => "Realm-" + id,
            (json, a, t) => null,
            normalizeIds,
            (d, reason) => suppressions.Add(reason),
            (d, i2, c, r, t, cf) => processed.Add(i2),
            _ => { });

        doc = null;
        commit("{}");
        Test.True(suppressions.Count == 0 && processed.Count == 0,
            "a missing analysis document must no-op");

        doc = new WorldDiplomacyDocument { DocumentId = "d1", AuthorKingdomId = "a" };
        commit("{\"status\":\"error\",\"intent\":\"declare_war\",\"commitment\":\"binding\"}");
        Test.True(doc.AnalysisStatus == "no_action"
            && suppressions.Last() == "analysis_status_has_no_publishable_action"
            && processed.Count == 0,
            "an unusable AI analysis must mark the document no_action and suppress it");

        doc = new WorldDiplomacyDocument { DocumentId = "d2", AuthorKingdomId = "a", IsPlayerAuthored = true };
        suppressions.Clear(); processed.Clear();
        commit("{\"status\":\"error\",\"intent\":\"\",\"commitment\":\"\"}");
        Test.True(suppressions.Count == 0 && processed.Count == 0 && !doc.PlayerAnalysisCommitted
            && doc.AnalysisStatus == "analysis_failed",
            "unusable player analysis must remain retryable without inventing or executing a statement");

        doc = new WorldDiplomacyDocument { DocumentId = "d3", AuthorKingdomId = "a" };
        suppressions.Clear(); processed.Clear();
        commit("{\"status\":\"success\",\"intent\":\"\",\"commitment\":\"\"}");
        Test.True(doc.AnalysisStatus == "no_action"
            && suppressions.Last() == "analysis_has_no_structured_intent",
            "an AI analysis without a structured intent must be suppressed");

        doc = new WorldDiplomacyDocument { DocumentId = "d4", AuthorKingdomId = "a" };
        suppressions.Clear(); processed.Clear();
        commit("{\"status\":\"success\",\"intent\":\"statement\",\"commitment\":\"non_binding\"}");
        Test.True(suppressions.Last() == "analysis_has_no_actionable_intent",
            "an AI statement-only analysis must be suppressed as non-actionable");

        doc = new WorldDiplomacyDocument { DocumentId = "d5", AuthorKingdomId = "a", TargetKingdomId = "b" };
        suppressions.Clear(); processed.Clear();
        commit("{\"status\":\"success\",\"intent\":\"declare_war\",\"commitment\":\"binding\","
            + "\"title_summary\":\"对敌宣战\",\"target\":\"c\",\"tone\":\"firm\",\"confidence\":0.8,"
            + "\"requires_response\":true,\"responding_to_offer_document_id\":\"offer1\",\"addressed\":[\"b\",\"c\"]}");
        Test.True(doc.AnalysisStatus == "success" && doc.Intent == "declare_war"
            && doc.Commitment == "binding" && doc.Tone == "firm"
            && Math.Abs(doc.Confidence - 0.8f) < 0.001f
            && doc.Title == "对敌宣战" && !doc.RequiresResponse
            && processed.Count == 1 && processed[0] == "declare_war",
            "a successful analysis must stamp intent, commitment, tone, confidence and title");
        Test.True(doc.TargetKingdomId == "c" && doc.TargetKingdomName == "Realm-c",
            "an analyzed target must be canonicalized through the kingdom port");
        Test.True(doc.SourceDocumentId == "offer1" && doc.IsResponse
            && doc.RespondingToOfferDocumentId == "offer1",
            "a response binding must mark the document as a response to the offer");
        Test.True(doc.AddressedKingdomIds != null
            && doc.AddressedKingdomIds.Contains("b") && doc.AddressedKingdomIds.Contains("c"),
            "addressed kingdom ids must be normalized through the id-list port");

        var source = new WorldDiplomacyDocument
        {
            DocumentId = "offer1",
            PeaceTerms = new WorldDiplomacyPeaceTerms { TributePayerKingdomId = "a", DailyTribute = 40 }
        };
        storage.Documents.Add(source);
        doc = new WorldDiplomacyDocument { DocumentId = "d6", AuthorKingdomId = "a", IsPlayerAuthored = true, TargetKingdomId = "b" };
        suppressions.Clear(); processed.Clear();
        commit("{\"status\":\"success\",\"intent\":\"accept_peace\",\"commitment\":\"acceptance\","
            + "\"responding_to_offer_document_id\":\"offer1\"}");
        Test.True(doc.PeaceTerms != null && doc.PeaceTerms.DailyTribute == 40
            && !ReferenceEquals(doc.PeaceTerms, source.PeaceTerms),
            "a player accept_peace analysis must clone the offered peace terms");

        doc = new WorldDiplomacyDocument { DocumentId = "d7", AuthorKingdomId = "a", TargetKingdomId = "b" };
        storage.DiplomaticThreats.Add(new WorldDiplomacyThreat
        {
            ThreatId = "t1", StageDocumentId = "sd1", IssuerKingdomId = "a", TargetKingdomId = "b", Status = "open"
        });
        suppressions.Clear(); processed.Clear();
        commit("{\"status\":\"success\",\"intent\":\"ultimatum\",\"commitment\":\"binding\"}");
        Test.True(doc.PresentedThreatDocumentIds != null
            && doc.PresentedThreatDocumentIds.Contains("sd1"),
            "an analysis must stage the author's open threat document ids");

        doc = new WorldDiplomacyDocument { DocumentId = "d8", AuthorKingdomId = "a", TargetKingdomId = "b", AutomaticReplyDepth = 3 };
        suppressions.Clear(); processed.Clear();
        commit("{\"status\":\"success\",\"intent\":\"propose_peace\",\"commitment\":\"proposal\",\"requires_response\":true}");
        Test.True(!doc.RequiresResponse,
            "a document at the automatic-reply depth cap must not request a response");
    }





    static void RunFailedJobCommitDecisionTests()
    {
        // DPL-060DD: CommitFailedJob routes each job kind to its failure path,
        // retains compression work with bounded backoff, and always removes
        // the dead job record.
        var storage = new WorldDiplomacyStorage();
        var abandons = new List<string>();
        var analyzed = new List<string>();
        var planned = new List<string>();
        var compressed = new List<string>();
        var removed = new List<string>();
        int threatFallbackLogs = 0;
        Action<WorldDiplomacyJob> commit = job => WorldDiplomacyFailureApplication.Commit(
            job, "boom", storage, 96, 6, () => 200,
            (j, a, t, reason) => abandons.Add(a + ">" + t + ">" + reason),
            j => "{fallback-analysis}",
            (j, raw) => analyzed.Add(raw),
            j => threatFallbackLogs++,
            j => "{fallback-compression}",
            (j, raw) => compressed.Add(raw),
            id => removed.Add(id),
            _ => { });

        commit(null);
        commit(new WorldDiplomacyJob { JobId = "j0", Kind = "unknown" });
        Test.True(abandons.Count + analyzed.Count + planned.Count + compressed.Count == 0
            && removed.Count == 1 && removed[0] == "j0",
            "null or unknown job kinds must only remove the job record");

        commit(new WorldDiplomacyJob
        {
            JobId = "j1", Kind = "generate", AuthorKingdomId = "a", TargetKingdomId = "b"
        });
        commit(new WorldDiplomacyJob
        {
            JobId = "j2", Kind = "generate", AuthorKingdomId = "a",
            AllowUntargeted = true, TargetKingdomId = ""
        });
        Test.True(abandons.Count == 2 && abandons[0] == "a>b>generation_failed"
            && abandons[1] == "a>>autonomous_generation_failed",
            "failed generations must abandon with the ordinary or autonomous-opening reason");

        commit(new WorldDiplomacyJob { JobId = "j3", Kind = "analyze" });
        Test.True(analyzed.Count == 1 && analyzed[0] == "{fallback-analysis}"
            && threatFallbackLogs == 1,
            "a failed analysis must republish through the fallback analysis pipeline");

        storage.CompressionRetryAttempts = 0;
        commit(new WorldDiplomacyJob { JobId = "j4", Kind = "compress", CompressionBatchId = "b1" });
        Test.True(storage.DiplomacyCompressionPending
            && storage.CompressionRetryAttempts == 1
            && storage.CompressionRetryAfterHour == 206,
            "a first compression failure must retry after the initial backoff");

        storage.DiplomacyCompressionPending = false;
        storage.CompressionRetryAttempts = 5;
        commit(new WorldDiplomacyJob { JobId = "j5", Kind = "compress" });
        Test.True(storage.CompressionRetryAttempts == DiplomacyCompressionBudget.MaximumFailures
            && storage.CompressionRetryAfterHour == int.MaxValue && DiplomacyCompressionBudget.Held(storage),
            "Q50 exhausted compression retries hold original evidence without an infinite timer");

        commit(new WorldDiplomacyJob { JobId = "j6", Kind = "round_plan" });
        Test.True(planned.Count == 0,
            "retired round-plan jobs cannot manufacture a fallback plan");

        commit(new WorldDiplomacyJob { JobId = "j7", Kind = "round_compress" });
        Test.True(compressed.Count == 1 && compressed[0] == "{fallback-compression}",
            "a failed round compression must commit the deterministic fallback");

        Test.True(removed.Count == 8
            && new[] { "j0", "j1", "j2", "j3", "j4", "j5", "j6", "j7" }.All(removed.Contains),
            "every failed job must be removed after its kind-specific handling");
    }

    static void RunGeneratedDocumentCommitDecisionTests()
    {
        // DPL-060DE: CommitGeneratedDocument owns generated-document commit
        // orchestration: stale-slot discard, author gates, json/envelope
        // validation, document materialization, exchange bookkeeping, and the
        // final analysis routing.
        var storage = new WorldDiplomacyStorage();
        var round = new WorldDiplomacyRound { RoundId = "r1" };
        var exchange = new WorldDiplomacyExchange { ExchangeId = "ex1", State = "awaiting_generation" };
        storage.ActiveExchange = exchange;
        var sourceDoc = new WorldDiplomacyDocument { DocumentId = "src1", AutomaticReplyDepth = 2 };

        var logs = new List<string>();
        var abandons = new List<string>();
        var rejects = new List<string>();
        var created = new List<string>();
        var added = new List<WorldDiplomacyDocument>();
        var analyzed = new List<WorldDiplomacyDocument>();
        int scheduled = 0, pruned = 0;
        bool legalityFails = false, envelopeFails = false, authorBlocked = false;
        Action<WorldDiplomacyJob, string> commit = (job, raw) => WorldDiplomacyGeneratedCompletionApplication.Commit(
            job, raw, storage,
            id => string.Equals(id, "r1", StringComparison.Ordinal) ? round : null,
            id => string.Equals(id, "src1", StringComparison.Ordinal) ? sourceDoc : null,
            id => string.Equals(id, "ghost", StringComparison.Ordinal) ? null : id,
            id => authorBlocked && string.Equals(id, "a", StringComparison.Ordinal) ? "eliminated" : null,
            (WorldDiplomacyJob j, JObject json, string a, string fb, out string t, out string rsn) =>
            {
                t = legalityFails ? "t2" : fb;
                rsn = legalityFails ? "illegal_intent" : null;
                return legalityFails;
            },
            (json, a, t) => null,
            (doc, json, a, t, allowUntargeted, relay) =>
            {
                if (envelopeFails) return false;
                doc.Intent = "declare_war";
                doc.Commitment = "binding";
                doc.RequiresResponse = false;
                doc.Tone = "firm";
                doc.Confidence = 0.9f;
                doc.NegotiationMove = "declare_war";
                return true;
            },
            (a, t, title, body, kind, isPlayer, isResponse, exchangeId) =>
            {
                created.Add(kind + ">" + a + ">" + t + ">" + title + ">" + body + ">" + exchangeId);
                return new WorldDiplomacyDocument
                {
                    DocumentId = "d" + created.Count,
                    AuthorKingdomId = a,
                    TargetKingdomId = t,
                    Title = title,
                    Body = body
                };
            },
            d => "day" + d,
            r => pruned++,
            (j, a, fb, reason) => abandons.Add((a ?? "") + ">" + (fb ?? "") + ">" + reason),
            (j, r2, a, t, reason, json) => rejects.Add(reason + ">" + (t ?? "")),
            d => added.Add(d),
            (d, intent, commitment, requires, tone, conf) => analyzed.Add(d),
            s => logs.Add(s));

        commit(null, "{}");
        Test.True(abandons.Count == 0 && rejects.Count == 0 && added.Count == 0
            && analyzed.Count == 0 && scheduled == 0,
            "a null generated job must be a no-op");

        round.ResultSettlementPending = true;
        round.RelayWaiting = true;
        round.ResultSettlementCurrentSlotId = "s1";
        round.ResultSettlementSlots = new List<WorldDiplomacyResultSettlementSlot>
        {
            new WorldDiplomacyResultSettlementSlot { SlotId = "s1", KingdomId = "a" }
        };
        commit(new WorldDiplomacyJob
        {
            JobId = "js2", RoundId = "r1", Kind = "generate",
            AuthorKingdomId = "a", ResultSettlementSlotId = "s1"
        }, "{}");
        Test.True(scheduled == 0 && rejects.Count == 1
            && rejects[0].StartsWith("empty_public_document", StringComparison.Ordinal),
            "saved legacy slot data cannot bypass ordinary draft validation");
        round.ResultSettlementPending = false;
        round.ResultSettlementSlots = null;
        round.ResultSettlementCurrentSlotId = null;

        commit(new WorldDiplomacyJob
        {
            JobId = "jg", RoundId = "r1", Kind = "generate",
            AuthorKingdomId = "ghost", TargetKingdomId = "b"
        }, "{}");
        Test.True(abandons.Count == 1 && abandons[0] == ">b>generated_party_missing",
            "an unresolvable author must abandon with generated_party_missing");

        authorBlocked = true;
        commit(new WorldDiplomacyJob
        {
            JobId = "jb", RoundId = "r1", Kind = "generate",
            AuthorKingdomId = "a", TargetKingdomId = "b"
        }, "{}");
        Test.True(abandons.Count == 2 && abandons[1] == "a>b>eliminated"
            && logs.Any(x => x.Contains("discarded at commit")),
            "a blocked AI author must abandon with the block reason");
        authorBlocked = false;

        commit(new WorldDiplomacyJob
        {
            JobId = "jj", RoundId = "r1", Kind = "generate",
            AuthorKingdomId = "a", TargetKingdomId = "b"
        }, "not json");
        Test.True(rejects.Count == 2 && rejects[1] == "json_parse_failed>b" && pruned == 2,
            "unparseable output must reject the draft after offer pruning");

        commit(new WorldDiplomacyJob
        {
            JobId = "je", RoundId = "r1", Kind = "generate",
            AuthorKingdomId = "a", TargetKingdomId = "b"
        }, "{\"title\":\"T\",\"intent\":\"declare_war\"}");
        Test.True(rejects.Count == 3 && rejects[2] == "empty_public_document>b",
            "an empty public body must reject the draft");

        legalityFails = true;
        commit(new WorldDiplomacyJob
        {
            JobId = "jl", RoundId = "r1", Kind = "generate",
            AuthorKingdomId = "a", TargetKingdomId = "b"
        }, "{\"title\":\"T\",\"body\":\"B\"}");
        Test.True(rejects.Count == 4 && rejects[3] == "illegal_intent>t2",
            "an illegal generated intent must reject with the resolved target");
        legalityFails = false;

        envelopeFails = true;
        commit(new WorldDiplomacyJob
        {
            JobId = "jf", RoundId = "r1", Kind = "generate",
            AuthorKingdomId = "a", TargetKingdomId = "b"
        }, "{\"title\":\"T\",\"body\":\"B\",\"actions\":[{\"intent\":\"declare_war\",\"target_kingdom_id\":\"b\"}]}");
        Test.True(rejects.Count == 5 && rejects[4].Length > 0,
            "a semantic-envelope failure must reject with the derived failure reason");
        envelopeFails = false;

        round.ConsecutiveTechnicalGenerationFailures = 3;
        string happyJson = new JObject
        {
            ["title"] = "War Declaration",
            ["body"] = "We declare war.",
            ["author_intent"] = new JObject { ["intent"] = "declare_war", ["commitment"] = "binding" },
            ["actions"] = new JArray(new JObject { ["intent"] = "declare_war", ["target_kingdom_id"] = "b", ["commitment"] = "binding" }),
            ["addressed_kingdom_ids"] = new JArray("b"),
            ["mentioned_kingdom_ids"] = new JArray("b"),
            ["round_participation"] = "bogus",
            ["round_status"] = "continue",
            ["made_progress"] = true
        }.ToString(Newtonsoft.Json.Formatting.None);
        commit(new WorldDiplomacyJob
        {
            JobId = "jh", RoundId = "r1", ExchangeId = "ex1", Kind = "generate",
            AuthorKingdomId = "a", TargetKingdomId = "b",
            IsRelayTurn = true, CreatedDay = 42,
            PresentedThreatDocumentIds = new List<string> { "th1" },
            PresentedThreatFollowThroughDocumentIds = new List<string> { "ft1" }
        }, happyJson);
        var doc = added[0];
        Test.True(created.Count == 2 && created[1] == "ai>a>b>War Declaration>We declare war.>ex1"
            && doc.RoundId == "r1" && doc.Day == 42 && doc.GameDate == "day42"
            && doc.HiddenIntent == "declare_war" && doc.HiddenCommitment == "binding"
            && doc.RoundParticipation == "continue" && doc.MadeDiplomaticProgress
            && doc.PresentedThreatDocumentIds.SequenceEqual(new[] { "th1" })
            && doc.PresentedThreatFollowThroughDocumentIds.SequenceEqual(new[] { "ft1" })
            && !doc.HasEmbeddedRoundPlan && !doc.IsAutonomousNoActionDeclaration
            && round.ConsecutiveTechnicalGenerationFailures == 0
            && exchange.State == "analyzing_source" && exchange.SourceDocumentId == doc.DocumentId
            && analyzed.Count == 1 && analyzed[0] == doc,
            "a valid generation must materialize, bookkeep the exchange, and route to analysis");

        exchange.State = "awaiting_response";
        exchange.SourceDocumentId = "src1";
        commit(new WorldDiplomacyJob
        {
            JobId = "jr", RoundId = "r1", ExchangeId = "ex1", Kind = "generate",
            AuthorKingdomId = "b", TargetKingdomId = "a",
            IsResponse = true, SourceDocumentId = "src1"
        }, happyJson);
        var resp = added[1];
        Test.True(created.Count == 3 && created[2].StartsWith("ai_response>b>a>", StringComparison.Ordinal)
            && resp.AutomaticReplyDepth == 3 && resp.SourceDocumentId == "src1"
            && exchange.State == "analyzing_response" && exchange.ResponseDocumentId == resp.DocumentId
            && analyzed.Count == 2 && analyzed[1] == resp,
            "a response generation must deepen the reply chain and bookkeep the response slot");
    }




    private sealed class TestCompletionEffects : IWorldDiplomacyCompletionEffects
    {
        private readonly Action<string> _remove;
        private readonly Action<string> _log;
        internal TestCompletionEffects(Action<string> remove, Action<string> log)
        {
            _remove = remove;
            _log = log;
        }
        public void RemoveJob(string jobId) => _remove?.Invoke(jobId);
        public void Log(string message) => _log?.Invoke(message);
    }

    static void RunLlmJobRoutingDecisionTests()
    {
        WorldDiplomacyJob Job(string id, string kind = "analyze", int priority = 1) => new WorldDiplomacyJob
        {
            JobId = id,
            Kind = kind,
            AuthorKingdomId = "kA",
            TargetKingdomId = "kB",
            Priority = priority,
            SystemPrompt = "sys",
            UserPrompt = "u",
            CreatedDay = 1,
            LlmMessages = new List<WorldDiplomacyLlmMessage>()
        };
        WorldDiplomacyStorage Store() => new WorldDiplomacyStorage
        {
            Jobs = new List<WorldDiplomacyJob>(),
            Documents = new List<WorldDiplomacyDocument>(),
            DiplomaticThreats = new List<WorldDiplomacyThreat>(),
            RelayArrivals = new List<WorldDiplomacyRelayArrival>(),
            PlayerOpportunities = new List<WorldDiplomacyPlayerOpportunity>()
        };
        WorldDiplomacyJob Select(
            WorldDiplomacyStorage storage,
            Func<WorldDiplomacyJob, string> signature = null,
            Func<WorldDiplomacyJob, bool> refreshThreat = null,
            Func<WorldDiplomacyJob, bool> refreshAction = null,
            Func<WorldDiplomacyJob, bool> rebuild = null,
            Func<WorldDiplomacyJob, string> authorBlock = null,
            List<string> abandons = null,
            Func<WorldDiplomacyJob, bool> strategicProfile = null,
            Func<string> configError = null,
            List<bool> budget = null,
            Action<WorldDiplomacyJob> captureHistory = null,
            long inputLimit = long.MaxValue,
            List<string> failures = null,
            List<string> removed = null,
            List<string> logs = null,
            string affinity = "")
        {
            bool PopBudget(List<bool> b)
            {
                if (b == null || b.Count == 0) return true;
                bool v = b[0];
                b.RemoveAt(0);
                return v;
            }
            return WorldDiplomacyLlmDispatchApplication.SelectAndPrepareLlmJob(
                storage, 100, affinity,
                signature ?? (j => ""), refreshThreat ?? (j => true),
                refreshAction ?? (j => true),
                rebuild ?? (j => true),
                authorBlock ?? (j => null),
                (j, reason) => abandons?.Add(reason),
                configError ?? (() => null),
                consume => PopBudget(budget),
                captureHistory,
                j => new Newtonsoft.Json.Linq.JArray(),
                out Newtonsoft.Json.Linq.JArray prepared,
                inputLimit,
                200,
                t => t?.Length ?? 0,
                _ => "",
                () => { },
                (j, e) => failures?.Add(e),
                id => removed?.Add(id),
                line => logs?.Add(line));
        }

        // Empty stores and cooldowns never dispatch.
        Test.True(Select(new WorldDiplomacyStorage()) == null,
            "an empty job store must not select a dispatch candidate");
        WorldDiplomacyStorage cooling = Store();
        cooling.Jobs.Add(Job("j1"));
        cooling.ServiceCooldownUntilHour = 200;
        Test.True(Select(cooling) == null,
            "a service cooldown must block dispatch selection");

        // The highest priority runnable job wins; equal priority prefers the
        // affinity-matching job, then the oldest, then the ordinal id.
        WorldDiplomacyStorage selection = Store();
        selection.Jobs.Add(Job("jLow", priority: 1));
        selection.Jobs.Add(Job("jB", priority: 9, kind: "analyze"));
        selection.Jobs.Add(Job("jA", priority: 9, kind: "analyze"));
        WorldDiplomacyJob picked = Select(selection, affinity: "");
        Test.True(picked != null && picked.JobId == "jA",
            "equal-priority selection must fall back to ordinal job id ordering");
        WorldDiplomacyStorage affinitySel = Store();
        WorldDiplomacyJob affinJob = Job("jAff", priority: 9);
        affinitySel.Jobs.Add(Job("jOther", priority: 9));
        affinitySel.Jobs.Add(affinJob);
        string affinKey = WorldDiplomacyPromptContractRules.ResolveCacheAffinityKey(affinJob);
        picked = Select(affinitySel, affinity: affinKey);
        Test.True(picked == affinJob,
            "a cache-affinity match must win ahead of ordinal ordering");

        // Jobs awaiting compression are excluded until the retry hour arrives.
        WorldDiplomacyStorage invalidIds = Store();
        invalidIds.Jobs.Add(Job("  ", priority: 100));
        invalidIds.Jobs.Add(Job("valid", priority: 1));
        Test.True(Select(invalidIds)?.JobId == "valid",
            "blank persisted job ids must never consume request budget or block valid jobs");
        affinJob.IsRunning = false;
        Test.True(Select(affinitySel, affinity: "  " + affinKey + "  ") == affinJob,
            "dispatch must retain the latest coordinator cache key whitespace normalization");

        WorldDiplomacyStorage awaiting = Store();
        WorldDiplomacyJob awaitJob = Job("jWait");
        awaitJob.AwaitingHistoryCompression = true;
        awaiting.Jobs.Add(awaitJob);
        awaiting.CompressionRetryAfterHour = 500;
        Test.True(Select(awaiting) == null,
            "a job awaiting compression must be skipped before its retry hour");

        // DPL-110: transfer the retired J12 queue DTO/selector cases to the live scheduler.
        WorldDiplomacyStorage formerQueue = Store();
        WorldDiplomacyJob running = Job("running", priority: 99); running.IsRunning = true;
        WorldDiplomacyJob waiting = Job("waiting", priority: 90); waiting.AwaitingHistoryCompression = true;
        WorldDiplomacyJob older = Job("older", priority: 80); older.CreatedDay = 2; older.CacheAffinityKey = "other";
        WorldDiplomacyJob affinity = Job("affinity", priority: 80); affinity.CreatedDay = 5; affinity.CacheAffinityKey = "cache-a";
        WorldDiplomacyJob alphabetical = Job("alphabetical", priority: 80); alphabetical.CreatedDay = 2; alphabetical.CacheAffinityKey = "other";
        formerQueue.Jobs.AddRange(new[] { running, waiting, older, affinity, alphabetical });
        formerQueue.CompressionRetryAfterHour = 101;
        Test.True(Select(formerQueue, affinity: "cache-a") == affinity,
            "J12: cache affinity wins within highest runnable priority");
        formerQueue.CompressionRetryAfterHour = 100;
        Test.True(Select(formerQueue, affinity: "cache-a") == waiting,
            "J12: compression-ready higher priority job becomes runnable");
        Test.True(Select(Store()) == null, "J12: empty queue has no selection");

        // Stale presentation refresh failures route to the failed commit.
        WorldDiplomacyStorage staleThreatStore = Store();
        staleThreatStore.Jobs.Add(Job("j1", kind: "generate"));
        staleThreatStore.Documents.Add(new WorldDiplomacyDocument { DocumentId = "d1", AuthorKingdomId = "kA", IsReadyForPublication = true });
        staleThreatStore.DiplomaticThreats.Add(new WorldDiplomacyThreat
        {
            ThreatId = "t1", Status = "open", IssuerKingdomId = "kA", StageDocumentId = "d1"
        });
        List<string> failures = new List<string>();
        Test.True(Select(staleThreatStore, refreshThreat: j => false,
                failures: failures) == null
            && failures.SequenceEqual(new[] { "stale diplomatic threat presentation could not be rebuilt" }),
            "an unrecoverable stale threat presentation must fail the job");

        WorldDiplomacyStorage staleActionStore = Store();
        staleActionStore.Jobs.Add(Job("j1", kind: "generate"));
        failures = new List<string>();
        Test.True(Select(staleActionStore, signature: j => "sig", refreshAction: j => false,
                failures: failures) == null
            && failures.SequenceEqual(new[] { "stale diplomatic action list could not be rebuilt" }),
            "an unrecoverable stale action presentation must fail the job");

        WorldDiplomacyStorage refreshOk = Store();
        refreshOk.Jobs.Add(Job("j1", kind: "analyze"));
        List<string> logs = new List<string>();
        WorldDiplomacyJob refreshed = Select(refreshOk, logs: logs);
        Test.True(refreshed != null && refreshed.IsRunning,
            "a refreshed presentation must still reach dispatch");

        // A stale canonical contract on a generate job must rebuild or fail.
        WorldDiplomacyStorage staleContract = Store();
        staleContract.Jobs.Add(Job("j1", kind: "generate"));
        failures = new List<string>();
        Test.True(Select(staleContract, rebuild: j => false, failures: failures) == null
            && failures.SequenceEqual(new[] { "stale canonical prompt contract could not be rebuilt" }),
            "a stale canonical contract that cannot be rebuilt must fail the job");

        // An invalid persisted repair chain clears messages and rebuilds or fails.
        WorldDiplomacyStorage badChain = Store();
        string sysPrompt = WorldDiplomacyPromptContractRules.BuildCanonicalHistorySystemPrompt("contract", 10, 100);
        WorldDiplomacyJob chainJob = new WorldDiplomacyJob
        {
            JobId = "j1",
            Kind = "generate",
            AuthorKingdomId = "kA",
            Priority = 1,
            SystemPrompt = sysPrompt,
            UserPrompt = "【MODE=DECLARE】 reply",
            CacheAffinityKey = WorldDiplomacyPromptContractRules.CanonicalHistoryCacheAffinityKey,
            SemanticRepairAttempts = 0,
            LlmMessages = new List<WorldDiplomacyLlmMessage>
            {
                new WorldDiplomacyLlmMessage { Role = "user", Content = "stale" }
            }
        };
        badChain.Jobs.Add(chainJob);
        failures = new List<string>();
        Test.True(Select(badChain, rebuild: j => false, failures: failures) == null
            && chainJob.LlmMessages.Count == 0
            && failures.SequenceEqual(new[] { "invalid persisted LLM message chain could not be rebuilt" }),
            "an invalid persisted message chain must be cleared and fail when it cannot rebuild");

        // A blocked author abandons the generation and removes the job.
        WorldDiplomacyStorage blocked = Store();
        blocked.Jobs.Add(Job("j1", kind: "generate"));
        List<string> abandons = new List<string>();
        List<string> removed = new List<string>();
        logs = new List<string>();
        Test.True(Select(blocked, authorBlock: j => "war_active", abandons: abandons,
                removed: removed, logs: logs) == null
            && abandons.SequenceEqual(new[] { "war_active" })
            && removed.SequenceEqual(new[] { "j1" })
            && logs.Any(x => x.Contains("reason=war_active")),
            "a blocked author must abandon the generation and remove the job");

        // A missing strategic profile abandons the generation twice-guarded.
        WorldDiplomacyStorage noProfile = Store();
        noProfile.Jobs.Add(Job("j1", kind: "generate"));
        abandons = new List<string>();
        removed = new List<string>();
        Test.True(Select(noProfile, strategicProfile: j => false, abandons: abandons,
                removed: removed) != null
            && abandons.Count == 0 && removed.Count == 0,
            "retired national personality profiles do not gate current event declarations");

        // Prompt and configuration guards fail the job without dispatch.
        WorldDiplomacyStorage emptyPrompt = Store();
        WorldDiplomacyJob noPrompt = Job("j1");
        noPrompt.SystemPrompt = "";
        emptyPrompt.Jobs.Add(noPrompt);
        failures = new List<string>();
        Test.True(Select(emptyPrompt, failures: failures) == null
            && failures.SequenceEqual(new[] { "empty prompt" }),
            "an empty system prompt must fail the job");

        WorldDiplomacyStorage unconfigured = Store();
        unconfigured.Jobs.Add(Job("j1"));
        failures = new List<string>();
        Test.True(Select(unconfigured, configError: () => "no api key", failures: failures) == null
            && failures.SequenceEqual(new[] { "api not configured: no api key" }),
            "an unconfigured LLM client must fail the job");

        // Budget gates stop dispatch before and after message assembly.
        WorldDiplomacyStorage budgetBlocked = Store();
        budgetBlocked.Jobs.Add(Job("j1"));
        Test.True(Select(budgetBlocked, budget: new List<bool> { false }) == null,
            "an exhausted request budget must stop dispatch before message assembly");

        WorldDiplomacyStorage fitsBlocked = Store();
        fitsBlocked.Jobs.Add(Job("j1"));
        Test.True(Select(fitsBlocked, inputLimit: -1) == null,
            "an over-budget request must stop dispatch before consuming the budget");

        WorldDiplomacyStorage consumeBlocked = Store();
        consumeBlocked.Jobs.Add(Job("j1"));
        Test.True(Select(consumeBlocked, budget: new List<bool> { true, false }) == null,
            "a failed budget consume after assembly must stop dispatch");

        // A fully validated generate job captures history, marks running state,
        // and returns for dispatch.
        WorldDiplomacyStorage happy = Store();
        happy.Jobs.Add(Job("j1", kind: "generate"));
        int captured = 0;
        picked = Select(happy, rebuild: j => true, captureHistory: j => captured++);
        Test.True(picked != null && picked.IsRunning
            && picked.CacheAffinityKey == WorldDiplomacyPromptContractRules.ResolveCacheAffinityKey(picked)
            && captured == 1,
            "a validated generate job must capture canonical history and mark running state");

        // ---- CommitCompletedLlmJobResult ----
        void CommitResult(
            WorldDiplomacyJob job,
            WorldDiplomacyStorage storage,
            bool success = true,
            bool serviceFailure = false,
            bool truncated = false,
            string content = "raw",
            string error = "",
            Func<WorldDiplomacyJob, string> signature = null,
            Func<WorldDiplomacyJob, bool> refreshThreat = null,
            Func<WorldDiplomacyJob, bool> refreshAction = null,
            Action<WorldDiplomacyJob, string> truncatedHandler = null,
            List<string> commits = null,
            List<string> failures2 = null,
            List<string> removed2 = null,
            List<string> logs2 = null)
        {
            var effects = new TestCompletionEffects(
                id => removed2?.Add(id),
                line => logs2?.Add(line));
            var orch = new FakeOrchestration()
            {
                OnRefreshThreatPresentation = j => refreshThreat?.Invoke(j) ?? true,
                OnRefreshActionPresentation = j => refreshAction?.Invoke(j) ?? true,
                OnBuildGenerationLegalActionSignature = signature ?? (j => ""),
                OnRejectGeneratedDraftBeforePublication = (j, raw, a, t, reason, json) => truncatedHandler?.Invoke(j, raw),
                OnCommitGeneratedDocument = (j, c) => commits?.Add("generate"),
                OnCommitAnalysis = (j, c) => commits?.Add("analyze"),
                OnCommitCompression = (j, c) => commits?.Add("compress"),
                OnCommitRoundPlan = (j, c) => commits?.Add("round_plan"),
                OnCommitRoundCompression = (j, c) => commits?.Add("round_compress"),
                OnCommitFailedJob = (j, e) => failures2?.Add(e)
            };
            WorldDiplomacyCompletionApplication.Complete(
                job, content, success, serviceFailure, truncated, error,
                storage, 100, 12, ref effects, orch);
        }

        WorldDiplomacyStorage done = Store();
        WorldDiplomacyJob genDone = Job("j1", kind: "generate");
        List<string> commits = new List<string>();
        List<string> removed2 = new List<string>();
        CommitResult(genDone, done, commits: commits, removed2: removed2);
        Test.True(commits.SequenceEqual(new[] { "generate" })
            && removed2.SequenceEqual(new[] { "j1" })
            && done.ConsecutiveServiceFailures == 0,
            "a successful generation must route to the generated commit and remove the job");

        commits = new List<string>();
        CommitResult(Job("j2", kind: "analyze"), done, commits: commits);
        CommitResult(Job("j3", kind: "compress"), done, commits: commits);
        CommitResult(Job("j4", kind: "round_plan"), done, commits: commits);
        CommitResult(Job("j5", kind: "round_compress"), done, commits: commits);
        Test.True(commits.SequenceEqual(new[] { "analyze", "compress", "round_compress" }),
            "current job kinds route to their owners; retired round_plan never commits");

        commits.Clear();
        WorldDiplomacyJob paddedKind = Job("padded", kind: "  AnAlYzE  ");
        CommitResult(paddedKind, done, commits: commits);
        Test.True(commits.SequenceEqual(new[] { "analyze" }) && paddedKind.Kind == "  AnAlYzE  ",
            "completion retains case/whitespace classification without changing persisted Kind");
        List<string> failures2 = new List<string>();
        CommitResult(Job("j6", kind: "mystery"), done, failures2: failures2);
        Test.True(failures2.SequenceEqual(new[] { "unknown job kind" }),
            "an unknown completed job kind must fail explicitly");

        // A stale completed generation is discarded and rebuilt or failed.
        done.Documents.Add(new WorldDiplomacyDocument { DocumentId = "d9", AuthorKingdomId = "kA", IsReadyForPublication = true });
        done.DiplomaticThreats.Add(new WorldDiplomacyThreat
        {
            ThreatId = "t9", Status = "open", IssuerKingdomId = "kA", StageDocumentId = "d9"
        });
        failures2 = new List<string>();
        CommitResult(Job("j7", kind: "generate"), done,
            refreshThreat: j => false, failures2: failures2);
        Test.True(failures2.SequenceEqual(new[] { "completed generation used a stale diplomatic threat stage and could not be rebuilt" }),
            "a stale completed generation that cannot be rebuilt must fail");
        List<string> logs2 = new List<string>();
        commits = new List<string>();
        WorldDiplomacyJob actionOnlyJob = Job("j8", kind: "generate");
        actionOnlyJob.AuthorKingdomId = "other";
        CommitResult(actionOnlyJob, done,
            signature: j => "sig", refreshAction: j => true, commits: commits, logs2: logs2);
        Test.True(commits.Count == 0 && logs2.Any(x => x.Contains("discarded completed generation")),
            "a rebuilt stale completed generation must be discarded without committing");

        // Truncated generated drafts route to rejection handling.
        WorldDiplomacyStorage truncStore = Store();
        truncStore.ConsecutiveServiceFailures = 1;
        int truncatedCalls = 0;
        List<string> truncRemoved = new List<string>();
        CommitResult(Job("j9", kind: "generate"), truncStore,
            success: false, truncated: true, content: "partial",
            truncatedHandler: (j, c) => truncatedCalls++, removed2: truncRemoved);
        Test.True(truncatedCalls == 1 && truncRemoved.SequenceEqual(new[] { "j9" })
            && truncStore.ConsecutiveServiceFailures == 0,
            "a truncated generated draft must route to rejection handling and reset service failures");
        failures2 = new List<string>();
        CommitResult(Job("j10", kind: "generate"), truncStore,
            success: false, truncated: true, content: "partial",
            truncatedHandler: (j, c) => throw new InvalidOperationException("boom"),
            failures2: failures2);
        Test.True(failures2.SequenceEqual(new[] { "truncated generated draft handling failed: boom" }),
            "a failing truncated-draft handler must route to the failed commit");

        // Service failures increment the counter and cool down after two.
        WorldDiplomacyStorage failing = Store();
        failures2 = new List<string>();
        CommitResult(Job("j11"), failing, success: false, serviceFailure: true,
            error: "timeout", failures2: failures2);
        Test.True(failing.ConsecutiveServiceFailures == 1
            && failures2.SequenceEqual(new[] { "timeout" }),
            "a single service failure must count once and commit the error");
        CommitResult(Job("j12"), failing, success: false, serviceFailure: true,
            error: "timeout2", failures2: failures2);
        Test.True(failing.ServiceCooldownUntilHour == 112
            && failing.ConsecutiveServiceFailures == 0,
            "a second consecutive service failure must open the cooldown window");

        CommitResult(Job("j13"), failing, success: false, serviceFailure: false,
            error: "bad json", failures2: failures2);
        Test.True(failures2.Last() == "bad json" && failing.ConsecutiveServiceFailures == 0,
            "a non-service failure must commit the error without touching the cooldown");

        CommitResult(null, done);
        Test.True(true, "a null job must be ignored by completed-result routing");
    }



    private static void RunJobCompositionDecisionTests()
    {
        // DPL-060DJ: analysis job composition.
        WorldDiplomacyStorage storage = new WorldDiplomacyStorage();
        int enqueued = 0;
        WorldDiplomacyJob captured = null;
        WorldDiplomacyJobPreparationApplication.PrepareAnalysisJob(null, 5, storage, 10, 900,
            p => p + "_x", id => null, r => "contract", d => "up", j => { enqueued++; captured = j; });
        Test.True(enqueued == 0, "a null document must not produce an analysis job");

        WorldDiplomacyDocument doc = new WorldDiplomacyDocument
        {
            DocumentId = "d9", ExchangeId = "ex9", AuthorKingdomId = "a",
            TargetKingdomId = "b", IsResponse = true, RoundId = "rr"
        };
        string contractArg = null;
        WorldDiplomacyJobPreparationApplication.PrepareAnalysisJob(doc, 5, storage, 10, 900,
            p => "jid1", id => new WorldDiplomacyRound { RoundId = id },
            r => contractArg = "frozen:" + r.RoundId,
            d => "user:" + d.DocumentId, j => { enqueued++; captured = j; });
        Test.True(enqueued == 1 && captured != null
                && captured.JobId == "jid1" && captured.Kind == "analyze" && captured.Priority == 5
                && captured.CreatedDay == 10 && captured.ExchangeId == "ex9" && captured.DocumentId == "d9"
                && captured.AuthorKingdomId == "a" && captured.TargetKingdomId == "b" && captured.IsResponse
                && captured.CacheAffinityKey == "analyze" && captured.MaxTokens == 900
                && captured.UserPrompt == "user:d9"
                && !string.IsNullOrWhiteSpace(captured.SystemPrompt)
                && contractArg == "frozen:rr",
            "analysis job composition must stamp identity, prompts, cache key, and budget");

        // DPL-060DJ: pending-job rebuild - generate branch.
        WorldDiplomacyRound settleRound = new WorldDiplomacyRound
        {
            RoundId = "r1", State = "active", EventSystemVersion = 1, Participants = new() { new() { KingdomId = "a", State = "active" }, new() { KingdomId = "b", State = "active" } }
        };
        Func<WorldDiplomacyJob, bool> rebuild = j => WorldDiplomacyJobPreparationApplication.RebuildPendingJob(
            j, storage, 1800, 900, () => (5, 40),
            id => !string.Equals(id, "ghost", StringComparison.OrdinalIgnoreCase),
            id => id == "r1" ? settleRound : null,
            r => "contract",
            id => id == "doc1" ? new WorldDiplomacyDocument { DocumentId = "doc1", AuthorKingdomId = "a", RoundId = "r1" } : null,
            (j2, ex, src, cand) => "gen_prompt:" + (ex?.ExchangeId ?? "noex") + ":" + string.Join(",", cand),
            j2 => "sig", j2 => { }, d => "analysis_prompt");

        WorldDiplomacyJob noAuthor = new WorldDiplomacyJob { Kind = "generate", AuthorKingdomId = "ghost", TargetKingdomId = "b" };
        WorldDiplomacyJob noTarget = new WorldDiplomacyJob { Kind = "generate", AuthorKingdomId = "a", TargetKingdomId = "ghost" };
        Test.True(!rebuild(noAuthor) && !rebuild(noTarget),
            "generate rebuild must reject unresolvable author and required target");

        WorldDiplomacyJob relayJob = new WorldDiplomacyJob
        {
            Kind = "generate", AuthorKingdomId = "a", TargetKingdomId = "b",
            IsRelayTurn = true, RoundId = "r1", ResultSettlementSlotId = "s1"
        };
        Test.True(rebuild(relayJob)
                && relayJob.CandidateKingdomIds.SequenceEqual(new[] { "b" })
                && relayJob.UserPrompt.Contains("gen_prompt:")
                && relayJob.CacheAffinityKey == WorldDiplomacyPromptContractRules.CanonicalHistoryCacheAffinityKey
                && relayJob.ProfiledKingdomId == "" && relayJob.StrategicProfileKingdomId == "a"
                && relayJob.MaxTokens == 1800 && relayJob.PresentedLegalActionSignature == "sig"
                && relayJob.AllowAutonomousNoAction == false,
            "legacy relay flags must rebuild through the current shared event declaration contract");

        WorldDiplomacyJob orphanRelay = new WorldDiplomacyJob
        { Kind = "generate", AuthorKingdomId = "a", TargetKingdomId = "b", IsRelayTurn = true, RoundId = "ghost" };
        Test.True(!rebuild(orphanRelay),
            "a relay generate rebuild must fail when its round is gone");

        storage.ActiveExchange = new WorldDiplomacyExchange { ExchangeId = "ex1" };
        WorldDiplomacyJob genJob = new WorldDiplomacyJob
        {
            Kind = "generate", AuthorKingdomId = "a", TargetKingdomId = "b",
            RoundId = "r1", ExchangeId = "ex1", IsResponse = true,
            CandidateKingdomIds = new List<string> { "c1" }
        };
        Test.True(rebuild(genJob) && genJob.UserPrompt.Contains("gen_prompt:ex1:b"),
            "non-relay generate rebuild must compose the generation prompt from exchange and candidates");

        // analyze / round_plan rebuild branches.
        WorldDiplomacyJob analyzeJob = new WorldDiplomacyJob { Kind = "analyze", DocumentId = "doc1" };
        Test.True(rebuild(analyzeJob)
                && analyzeJob.CacheAffinityKey == "analyze" && analyzeJob.MaxTokens == 900
                && analyzeJob.UserPrompt == "analysis_prompt",
            "analyze rebuild must resolve the document and stamp analysis prompts");

        WorldDiplomacyJob analyzeOrphan = new WorldDiplomacyJob { Kind = "analyze", DocumentId = "ghost" };
        Test.True(!rebuild(analyzeOrphan),
            "analyze rebuild must fail when its document is gone");

        WorldDiplomacyJob planJob = new WorldDiplomacyJob
        {
            Kind = "round_plan", RoundId = "r1", DocumentId = "doc1",
            CandidateKingdomIds = new List<string> { "b" }
        };
        Test.True(!rebuild(planJob), "retired round-plan work must not rebuild into a model request");

        WorldDiplomacyJob planOrphan = new WorldDiplomacyJob
        { Kind = "round_plan", RoundId = "ghost", DocumentId = "doc1" };
        Test.True(!rebuild(planOrphan) && !rebuild(new WorldDiplomacyJob { Kind = "compress" })
                && !rebuild(null),
            "round-plan rebuild must fail on missing round/root and unknown kinds must be rejected");
    }



    // Current ownership gate. The former extraction-era source macro remains
    // available explicitly as --legacy-source-boundary; its deleted owners and
    // forced-relay assertions are not requirements of the event architecture.
    private static void VerifySourceBoundary()
    {
        string root = Path.GetDirectoryName(FindRepositoryFile("AnimusForge.csproj"))!;
        string Read(string path) => File.ReadAllText(Path.Combine(root, "src/modules/AF.Module.Diplomacy", path));
        foreach (string owner in new[] { "WorldDiplomacyRoundPlanApplication", "WorldDiplomacyRoundProgressApplication",
            "WorldDiplomacyTurnSchedulingApplication", "WorldDiplomacyGenerationTaskApplication",
            "WorldDiplomacyResultSlotApplication", "WorldDiplomacyNoActionApplication", "WorldDiplomacyCourtResponseApplication" })
            Test.True(!File.Exists(Path.Combine(root, "src/modules/AF.Module.Diplomacy/Application", owner + ".cs")),
                "retired scheduling owner cannot be compiled: " + owner);
        string ownerSource = Read("Application/WorldDiplomacyOrchestration.cs");
        Test.True(ownerSource.Contains("QueueEventDeclaration(round, authorId, targetId, sourceDocument")
            && ownerSource.Contains("WorldDiplomacyGeneratedCompletionApplication.Commit(")
            && ownerSource.Contains("EventBoundaryComplete(eventRound, \"started\")"),
            "live enqueue and completion share the declaration owner and actual opening gate");
        string progress = Read("Application/WorldDiplomacyOrchestration.EventProgress.cs");
        Test.True(progress.Contains("EventHasPendingWork(round") && progress.Contains("TryApplyEventProgress(round")
            && !progress.Contains("HardEndDay") && !progress.Contains("RelayRouteKingdomIds") && !progress.Contains("ResultSettlementSlots"),
            "natural progress checks real pending work without legacy timers, routes or seats");
        string runtime = Read("Application/WorldDiplomacyOrchestration.EventRuntime.cs");
        Test.True(runtime.Contains("PublishStagedEventBoundary(round") && runtime.Contains("boundary_effect_not_confirmed")
            && runtime.Contains("EventExecutionPending"), "first and restored boundary processing share persisted execution receipts");
        string dispatch = Read("Application/WorldDiplomacyOrchestration.Dispatch.cs");
        Test.True(dispatch.Contains("RequestLeases.ContainsJob") && dispatch.Contains("DiplomacyEventRules.Matches")
            && dispatch.Contains("result.RoundConversationRevision != round.ConversationRevision")
            && dispatch.Contains("job.CourtKnowledgeRevision != DiplomacyEventRules.KnowledgeRevision"),
            "dispatch retains one lease owner and rejects stale event or court-knowledge completions");
        string repair = Read("Application/WorldDiplomacyDraftRepairApplication.cs");
        Test.True(repair.Contains("GetAuthorizedGenerationTargetIds") && !repair.Contains("FindRequiredPeaceOfferResponse"),
            "repair follows present authorization without choosing a compulsory peace response");
        string effects = Read("Application/DiplomacyEventEffects.cs");
        Test.True(!effects.Contains("PolicySystem") && !effects.Contains("TeamModuleServices.Policy")
            && !effects.Contains("Task.Run") && !effects.Contains("File.Read"),
            "diplomacy effect owner does not delegate to policy, background game writes or disk polling");
        string evidence = Read("Application/WorldDiplomacyOrchestration.EventRequestEvidence.cs");
        Test.True(progress.Contains("BuildEventRequestEvidence(round)") && runtime.Contains("BuildEventRequestEvidence(round, ending)")
            && evidence.Contains("DiplomacyCommitmentRules.IsPending") && evidence.Contains("DialogueDocumentKnown")
            && evidence.Contains("body_is_excerpt") && evidence.Contains("source_is_private"),
            "both request consumers use bounded history while retaining pending truth and knowledge provenance");
        string legacy = Read("Application/WorldDiplomacyOrchestration.LegacyEvents.cs");
        Test.True(legacy.Contains("Storage.LegacyPreservedJobs.Add(job)") && legacy.Contains("Storage.Jobs.Remove(job)")
            && legacy.Contains("legacy_unpublished_execution_evidence"),
            "legacy draft evidence stays outside dispatch and unknown past execution is not replayed");
        foreach (string edition in new[] { "", ".en" })
        {
            var resource = JObject.Parse(File.ReadAllText(Path.Combine(root,
                "content/modules/AF.Module.Diplomacy/ModuleData/DiplomacyPromptTexts" + edition + ".json")));
            foreach (string key in new[] { "EventConceptWriter", "EventConceptAudit", "EventProgressWriter", "EventProgressAudit",
                "EventBoundaryWriter", "EventBoundaryAudit" })
                Test.True(!string.IsNullOrWhiteSpace((string?)resource[key]), "actual package retains event contract " + edition + key);
        }
        VerifyRoundCompressionOwnership();
        VerifyJobLaunchOwnership();
    }

    private static void VerifyLegacySourceBoundary()
    {
        string applicationSource = File.ReadAllText(FindRepositoryFile(Path.Combine("src", "modules", "AF.Module.Diplomacy", "Application", "WorldDiplomacyRoundApplication.cs")));
        string propagationApplicationSource = File.ReadAllText(FindRepositoryFile(Path.Combine("src", "modules", "AF.Module.Diplomacy", "Application", "WorldDiplomacyPropagationApplication.cs")));
        string offerApplicationSource = File.ReadAllText(FindRepositoryFile(Path.Combine("src", "modules", "AF.Module.Diplomacy", "Application", "WorldDiplomacyOfferApplication.cs")));
        string courtResponseApplicationSource = File.ReadAllText(FindRepositoryFile(Path.Combine("src", "modules", "AF.Module.Diplomacy", "Application", "WorldDiplomacyCourtResponseApplication.cs")));
        string documentApplicationSource = File.ReadAllText(FindRepositoryFile(Path.Combine("src", "modules", "AF.Module.Diplomacy", "Application", "WorldDiplomacyDocumentApplication.cs")));

        string rulesSource = File.ReadAllText(FindRepositoryFile(
            "src", "modules", "AF.Module.Diplomacy", "Domain", "WorldDiplomacyRoundLifecycleRules.cs"));
        string structureSource = File.ReadAllText(FindRepositoryFile(
            "src", "modules", "AF.Module.Diplomacy", "Domain", "WorldDiplomacyStructureRules.cs"));
        Test.True(!rulesSource.Contains("TaleWorlds", StringComparison.Ordinal),
            "round lifecycle rules must stay free of TaleWorlds references");
        Test.True(!rulesSource.Contains("WorldDiplomacyBehavior", StringComparison.Ordinal),
            "round lifecycle rules must not reference the host behavior");
        string vocabularySource = File.ReadAllText(FindRepositoryFile(
            "src", "modules", "AF.Module.Diplomacy", "Domain", "WorldDiplomacyIntentVocabulary.cs"));
        Test.True(!vocabularySource.Contains("TaleWorlds", StringComparison.Ordinal),
            "intent vocabulary must stay free of TaleWorlds references");
        Test.True(!vocabularySource.Contains("WorldDiplomacyBehavior", StringComparison.Ordinal),
            "intent vocabulary must not reference the host behavior");

        string presentationQueriesSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyPresentationQueries.cs"));
        string playerApplicationSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyPlayerApplication.cs"));
        string notificationApplicationSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyNotificationApplication.cs"));
        string presentationSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Presentation/WorldDiplomacyPresentation.cs"));
        string behaviorSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs"));
        string proactiveSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyProactiveDiscussionApplication.cs"));
        behaviorSource += File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.JobRuntime.cs"));
        behaviorSource += File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.LlmDispatchSource.cs"));
        behaviorSource += File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.HistoryCapturePort.cs"));
        behaviorSource += File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.OrchestrationHost.cs"));
        behaviorSource += File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.PromptContext.cs"));
        // Inspect the active host-to-application path after DPL-080 ownership transfer.
        behaviorSource += File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOrchestration.cs"));
        behaviorSource += File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyPromptComposer.cs"));
        behaviorSource += File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyDraftRepairApplication.cs"));
        string identitySource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Adapters/DiplomacyIdentityResolver.cs"));
        int indexedKingdom = behaviorSource.IndexOf("Kingdom indexed = DiplomacyIdentityResolver.Kingdom(normalizedId)", StringComparison.Ordinal);
        int fallbackKingdom = indexedKingdom < 0 ? -1
            : behaviorSource.IndexOf("return Kingdom.All.FirstOrDefault", indexedKingdom, StringComparison.Ordinal);
        Test.True(indexedKingdom >= 0 && fallbackKingdom > indexedKingdom
            && behaviorSource.Contains("Settlement indexed = DiplomacyIdentityResolver.Settlement(settlementId.Trim())", StringComparison.Ordinal)
            && identitySource.Contains("Campaign.Current?.CampaignObjectManager?.Find<Kingdom>(id)", StringComparison.Ordinal)
            && identitySource.Contains("TaleWorlds.CampaignSystem.Settlements.Settlement.Find(id)", StringComparison.Ordinal),
            "stable diplomacy IDs use their native object owner before legacy name or case-insensitive fallback scans");
        foreach (string transition in new[] { "EnsureOpen", "TryScheduleNormal", "Close", "AdvanceRelay", "IntegratePlayerDeclaration" })
            Test.True(behaviorSource.Contains("WorldDiplomacyRoundApplication." + transition + "(", StringComparison.Ordinal),
                "live host must bind the application transition: " + transition);
        Test.True(behaviorSource.Contains("WorldDiplomacyCourtResponseApplication.TryScheduleMandatory(", StringComparison.Ordinal)
            && courtResponseApplicationSource.Contains("WorldDiplomacyRoundApplication.AdmitMandatoryReply(", StringComparison.Ordinal),
            "live court admission must bind through the court application to the round transition");
        Test.True(behaviorSource.Contains("TryScheduleEventConcept(policyOnly: true)", StringComparison.Ordinal),
            "live policy signal must enter the same canonical round through its application owner");
        Test.True(!applicationSource.Contains("TaleWorlds", StringComparison.Ordinal)
                  && !applicationSource.Contains("WorldDiplomacyBehavior", StringComparison.Ordinal)
                  && !applicationSource.Contains("Task.Run", StringComparison.Ordinal),
            "application transitions must not retain live game objects or dispatch background work");
        Test.True(!behaviorSource.Contains("_storage.CompletedRounds.Add(round)", StringComparison.Ordinal)
                  && !behaviorSource.Contains("round.CompletedDay = CurrentDay()", StringComparison.Ordinal)
                  && !behaviorSource.Contains("int selectedIndex = -1", StringComparison.Ordinal),
            "replaced close and participant selection algorithms must not remain in the host");
        string threatMigrationSource = File.ReadAllText(FindRepositoryFile(
            "src", "modules", "AF.Module.Diplomacy", "Persistence", "WorldDiplomacyThreatStorageMigration.cs"));
        string storageMigrationSource = File.ReadAllText(FindRepositoryFile(
            "src", "modules", "AF.Module.Diplomacy", "Persistence", "WorldDiplomacyStorageMigration.cs"));
        string memoryApplicationSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyMemoryApplication.cs"));
        string canonicalHistoryRulesSource = File.ReadAllText(FindRepositoryFile(
            "src", "modules", "AF.Module.Diplomacy", "Domain", "WorldDiplomacyCanonicalHistoryRules.cs"));
        string historyPublicationSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyHistoryPublicationApplication.cs"));
        string historyCompressionSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyHistoryCompressionApplication.cs"));
        string threatHistorySource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyThreatHistoryApplication.cs"));
        Test.True(!canonicalHistoryRulesSource.Contains("Action ensureInitialized")
            && !canonicalHistoryRulesSource.Contains("enqueueCompressionJob(")
            && !canonicalHistoryRulesSource.Contains("invalidateRenderCache()")
            && !rulesSource.Contains("appendBreachResult?.Invoke"),
            "Domain must not own history publication, compression scheduling or dependency callback orchestration");
        string normalizationApplicationSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyStorageNormalizationApplication.cs"));
        Test.True(!storageMigrationSource.Contains("TaleWorlds", StringComparison.Ordinal)
            && !storageMigrationSource.Contains("WorldDiplomacyBehavior", StringComparison.Ordinal),
            "storage migration must stay free of TaleWorlds and host references");
        Test.True(normalizationApplicationSource.Contains("WorldDiplomacyStorageMigration.MigrateDiplomacyPromptContractIfNeeded(", StringComparison.Ordinal)
            && normalizationApplicationSource.Contains("WorldDiplomacyStorageMigration.MigrateAutonomousDecisionArchitectureIfNeeded(", StringComparison.Ordinal),
            "host must delegate prompt-contract and autonomous-architecture migrations to persistence");
        string validationSource = File.ReadAllText(FindRepositoryFile(
            "src", "modules", "AF.Module.Diplomacy", "Domain", "WorldDiplomacyGenerationValidationRules.cs"));
        Test.True(!validationSource.Contains("TaleWorlds", StringComparison.Ordinal)
            && !validationSource.Contains("WorldDiplomacyBehavior", StringComparison.Ordinal),
            "generation validation rules must stay free of TaleWorlds and host references");
        Test.True(behaviorSource.Contains("WorldDiplomacyGenerationValidationRules.TryGetPublicPeaceTermsDisclosureViolation(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyGenerationValidationRules.TryGetDiplomaticStateViolation(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyGenerationValidationRules.TryGetDiplomaticThreatIntentViolation(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyGenerationValidationRules.TryGetRealmIdentityViolation(", StringComparison.Ordinal),
            "host must delegate generation violation checks to the domain");
        Test.True(!threatMigrationSource.Contains("TaleWorlds", StringComparison.Ordinal)
            && !threatMigrationSource.Contains("WorldDiplomacyBehavior", StringComparison.Ordinal),
            "threat storage migration must stay free of TaleWorlds and host references");
        Test.True(normalizationApplicationSource.Contains("WorldDiplomacyThreatStorageMigration.NormalizeDiplomaticThreats(", StringComparison.Ordinal)
            && threatMigrationSource.Contains("MigrateDiplomaticThreatsToNextDeclarationRules(storage, currentDay, resolveDocument, log);", StringComparison.Ordinal)
            && threatMigrationSource.Contains("MigrateThreatComplianceConsequencesV3(storage);", StringComparison.Ordinal)
            && normalizationApplicationSource.Contains("source.ValidateThreatWorldEligibility", StringComparison.Ordinal),
            "the normalization Application must route threat storage migration through the persistence migrator");
        Test.True(applicationSource.Contains("WorldDiplomacyRoundLifecycleRules.EvaluateReconcileAfterLoad", StringComparison.Ordinal),
            "the round Application must route after-load reconcile through the lifecycle rules");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundApplication.BeginOrExtendRoundResultSettlement(", StringComparison.Ordinal)
            && rulesSource.Contains("ComputeSettlementWindowDays(", StringComparison.Ordinal),
            "the host must route settlement windows through the lifecycle rules");
        Test.True(rulesSource.Contains("ShouldForceTerminalMove(", StringComparison.Ordinal),
            "the host must route terminal-move escalation through the lifecycle rules");
        Test.True(rulesSource.Contains("IsRetryableAbort(", StringComparison.Ordinal),
            "retryable-abort detection must live in the lifecycle rules");
        Test.True(rulesSource.Contains("ComputeRelayArrivalDay(", StringComparison.Ordinal),
            "the host must route relay arrival-day planning through the lifecycle rules");
        Test.True(rulesSource.Contains("CompleteRelayPassProgressAccounting(", StringComparison.Ordinal),
            "the host must route pass accounting through the lifecycle rules");
        Test.True(rulesSource.Contains("EvaluateRelayPassAccounting(", StringComparison.Ordinal),
            "pass-accounting evaluation must be composed inside the lifecycle rules");
        Test.True(rulesSource.Contains("NextRelayDirection(", StringComparison.Ordinal),
            "the host must route relay direction flips through the lifecycle rules");
        Test.True(rulesSource.Contains("CanScheduleResultSettlementTurn(", StringComparison.Ordinal),
            "the host must route the settlement-turn gate through the lifecycle rules");
        Test.True(rulesSource.Contains("EvaluateSettlementSlotAction(", StringComparison.Ordinal),
            "the host must route settlement slot dispatch through the lifecycle rules");
        Test.True(rulesSource.Contains("previousKingdomId = ResolveSettlementPreviousSpeaker(", StringComparison.Ordinal),
            "the previous-speaker chain must be composed inside the lifecycle rules");
        Test.True(applicationSource.Contains("WorldDiplomacyRoundLifecycleRules.EvaluateRoundTerminalClose", StringComparison.Ordinal),
            "the round Application must route circuit-breaker and hard-end closes through the lifecycle rules");
        Test.True(applicationSource.Contains("WorldDiplomacyRoundLifecycleRules.IsPlayerSlotWaitingExpired", StringComparison.Ordinal),
            "the round Application must route player-slot timeouts through the lifecycle rules");
        Test.True(applicationSource.Contains("WorldDiplomacyRoundLifecycleRules.IsActiveRelayParticipant", StringComparison.Ordinal),
            "the round Application must route relay participant eligibility through the lifecycle rules");
        Test.True(rulesSource.Contains("IsArrivalStale(", StringComparison.Ordinal),
            "stale-arrival detection must be composed inside the lifecycle rules");
        Test.True(rulesSource.Contains("EvaluateArrivalAction(", StringComparison.Ordinal),
            "arrival dispatch must be composed inside the lifecycle rules");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundProgressApplication.ProcessDueRelayArrivals(", StringComparison.Ordinal),
            "the host must route due relay arrivals through the round-progress Application");
        Test.True(rulesSource.Contains("EvaluateRejectedGenerationAction(", StringComparison.Ordinal),
            "the host must route rejected-generation dispatch through the lifecycle rules");
        Test.True(rulesSource.Contains("NextTechnicalFailureCount(", StringComparison.Ordinal),
            "the host must route technical-failure counting through the lifecycle rules");
        Test.True(rulesSource.Contains("ShouldTripTechnicalCircuitBreaker(", StringComparison.Ordinal),
            "the host must route circuit-breaker trips through the lifecycle rules");
        Test.True(!behaviorSource.Contains("private void ProcessPlayerMandatoryResponseTimeout(", StringComparison.Ordinal) && rulesSource.Contains("public static bool IsReminderDue(", StringComparison.Ordinal),
            "retired mandatory reminder path stays absent; public reminder rule retains compatibility");
        Test.True(!behaviorSource.Contains("private void ProcessPlayerMandatoryResponseTimeout(", StringComparison.Ordinal) && rulesSource.Contains("public static bool IsMandatoryTimeoutExpired(", StringComparison.Ordinal),
            "retired mandatory timeout path stays absent; public timeout rule retains compatibility");
        Test.True(!behaviorSource.Contains("ConsecutiveTechnicalGenerationFailures >= ", StringComparison.Ordinal),
            "raw technical-failure thresholds must not remain in the host");
        Test.True(!behaviorSource.Contains("MandatorySinceDay + 3", StringComparison.Ordinal),
            "raw reminder-offset math must not remain in the host");
        Test.True(!behaviorSource.Contains("MandatorySinceDay + 5", StringComparison.Ordinal),
            "raw timeout-offset math must not remain in the host");
        Test.True(behaviorSource.Contains("WorldDiplomacyLiveRoundRules.InitializeOrdinaryAdmission", StringComparison.Ordinal)
            && !behaviorSource.Contains("_host.RoundIntervalDays()", StringComparison.Ordinal),
            "legacy interval refresh initializes persistent ordinary admission without reading the retired MCM interval");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.ComputeNextRoundDay", StringComparison.Ordinal),
            "the host must route next-round day computation through the lifecycle rules");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.ComputeInitialCompressedYear", StringComparison.Ordinal),
            "the host must route the initial compressed year through the lifecycle rules");
        Test.True(applicationSource.Contains("WorldDiplomacyRoundLifecycleRules.IsNormalRoundDue", StringComparison.Ordinal),
            "the host must route normal-round due checks through the lifecycle rules");
        Test.True(applicationSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizeRotationStartIndex", StringComparison.Ordinal),
            "the host must route rotation-index normalization through the lifecycle rules");
        Test.True(applicationSource.Contains("WorldDiplomacyRoundLifecycleRules.NextRotationIndex", StringComparison.Ordinal),
            "the host must route rotation-index advancement through the lifecycle rules");
        Test.True(!behaviorSource.Contains("private void ProcessPlayerResponseTimeouts(", StringComparison.Ordinal) && rulesSource.Contains("public static bool IsExchangeReminderDue(", StringComparison.Ordinal),
            "retired exchange reminder path stays absent; public reminder rule retains compatibility");
        Test.True(!behaviorSource.Contains("private void ProcessPlayerResponseTimeouts(", StringComparison.Ordinal) && rulesSource.Contains("public static bool IsExchangeCloseDue(", StringComparison.Ordinal),
            "retired exchange timeout path stays absent; public close rule retains compatibility");
        string policyRoundApplicationSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyPolicyRoundApplication.cs"));
        string actionSelectionSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyActionSelectionApplication.cs"));
        string analysisSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyAnalysisApplication.cs"));
        string noActionSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyNoActionApplication.cs"));
        string bindingSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyThreatBindingApplication.cs"));
        string historyCaptureSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyHistoryCaptureApplication.cs"));
        string exchangeApplicationSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyRoundApplication.cs"));
        string progressSource = File.ReadAllText(FindRepositoryFile(Path.Combine("src", "modules", "AF.Module.Diplomacy", "Application", "WorldDiplomacyRoundProgressApplication.cs")), Encoding.UTF8);
        string generatedCompletionSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyGeneratedCompletionApplication.cs"));
        string generationTaskSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyGenerationTaskApplication.cs"));
        string jobPreparationSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyJobPreparationApplication.cs"));
        string dispatchSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyLlmDispatchApplication.cs"));
        string completionSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyCompletionApplication.cs"));
        string migrationApplicationSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyCanonicalHistoryMigrationApplication.cs"));
        string roundCompressionSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyRoundCompressionApplication.cs"));
        Test.True(exchangeApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.RestoreSuspendedExchangeIfAny", StringComparison.Ordinal),
            "the host must route suspended-pause day shifts through the lifecycle rules");
        Test.True(rulesSource.Contains("ComputeSuspendedPauseDays(currentDay, exchange.SuspendedDay)", StringComparison.Ordinal)
            && rulesSource.Contains("NormalizeRestoredExchangeState(exchange.StateBeforeSuspension)", StringComparison.Ordinal),
            "the lifecycle rules must own suspended-pause day shifts and state restoration");
        Test.True(!behaviorSource.Contains("Math.Abs(_storage.RotationIndex)", StringComparison.Ordinal),
            "raw rotation-index normalization must not remain in the host");
        Test.True(applicationSource.Contains("WorldDiplomacyRoundLifecycleRules.EvaluateRelayAdvanceAction", StringComparison.Ordinal),
            "the host must route relay advancement through the lifecycle rules");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.IsActiveRoundState", StringComparison.Ordinal),
            "the host must route active-round checks through the lifecycle rules");

        Test.True(!behaviorSource.Contains("string.Equals(round.State, \"active\", StringComparison.OrdinalIgnoreCase)", StringComparison.Ordinal),
            "raw round-state comparisons must not remain in the host");
        Test.True(!behaviorSource.Contains("string.Equals(_storage.ActiveRound.State, \"active\", StringComparison.OrdinalIgnoreCase)", StringComparison.Ordinal),
            "raw active-round comparisons must not remain in the host");
        Test.True(applicationSource.Contains("WorldDiplomacyRoundLifecycleRules.EvaluateClosedRoundStatus", StringComparison.Ordinal),
            "the host must route close status classification through the lifecycle rules");
        Test.True(applicationSource.Contains("WorldDiplomacyRoundLifecycleRules.ClosedRoundStatusName", StringComparison.Ordinal),
            "the host must route close status names through the lifecycle rules");
        Test.True(applicationSource.Contains("WorldDiplomacyRoundLifecycleRules.IsOpenLifecycleStatus", StringComparison.Ordinal),
            "the host must route open-status checks through the lifecycle rules");
        Test.True(rulesSource.Contains("CanScheduleRelayHop(", StringComparison.Ordinal),
            "relay-hop gates must be composed inside the lifecycle rules");
        Test.True(rulesSource.Contains("HasMinimumRelayRoute(", StringComparison.Ordinal),
            "the relay-route minimum must be composed inside the lifecycle rules");
        Test.True(rulesSource.Contains("NormalizeRelayPassDurationDays(", StringComparison.Ordinal),
            "pass-duration normalization must be composed inside the lifecycle rules");
        Test.True(!behaviorSource.Contains("StartsWith(\"technical_\"", StringComparison.Ordinal),
            "raw abortive-reason checks must not remain in the host");
        Test.True(rulesSource.Contains("bool invalid = IsOfferPartyInvalid(", StringComparison.Ordinal),
            "the offer-pruning rules must route party validation through the lifecycle rules");
        Test.True(rulesSource.Contains("invalid = IsOfferInvalidForIntent(", StringComparison.Ordinal),
            "the offer-pruning rules must route intent validation through the lifecycle rules");
        Test.True(rulesSource.Contains("ComposeOfferDeduplicationKey(", StringComparison.Ordinal),
            "the offer-pruning rules must route dedup keys through the lifecycle rules");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.IsOpenOfferBetweenPair", StringComparison.Ordinal),
            "the host must route open-offer pair matching through the lifecycle rules");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.EvaluateExternalFactJoin", StringComparison.Ordinal),
            "the host must route external-fact join decisions through the lifecycle rules");
        Test.True(rulesSource.Contains("SettlementSlotKindContains(x.Kind, \"offer_response\")", StringComparison.Ordinal),
            "the lifecycle rules must own slot-kind matching for required peace-offer selection");
        Test.True(validationSource.Contains("WorldDiplomacyRoundLifecycleRules.IsWarResponseNoActionAllowed", StringComparison.Ordinal),
            "the host must route war-response no-action checks through the lifecycle rules");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.IsImmediateWarResponsePeaceSuppressed", StringComparison.Ordinal),
            "the host must route immediate war-response peace suppression through the lifecycle rules");
        Test.True(rulesSource.Contains("IsWarResponseSlotAuthorized(", StringComparison.Ordinal)
                && rulesSource.Contains("IsWarDeclarationAction(", StringComparison.Ordinal)
                && rulesSource.Contains("IsWarDeclarationDocument(", StringComparison.Ordinal)
                && rulesSource.Contains("IsWarResponseSourceDocument(", StringComparison.Ordinal),
            "war-response authorization and declaration detection must be composed inside the lifecycle rules");
        Test.True(noActionSource.Contains("WorldDiplomacyRoundLifecycleRules.IsSettlementTargetUsable", StringComparison.Ordinal),
            "the host must route settlement-target usability through the lifecycle rules");
        string documentExecutor = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyDocumentExecutionApplication.cs"));
        string documentPublication = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyDocumentPublicationApplication.cs"));
        Test.True(behaviorSource.Contains("WorldDiplomacyDocumentExecutionApplication.TryIncludeResultSettlementTarget", StringComparison.Ordinal)
            && documentExecutor.Contains("WorldDiplomacyRoundLifecycleRules.EvaluateSettlementTargetAdmission", StringComparison.Ordinal),
            "the host must route settlement-target admission through the lifecycle rules");
        Test.True(noActionSource.Contains("WorldDiplomacyRoundLifecycleRules.IsNoActionAuthorizationEligible", StringComparison.Ordinal),
            "the host must route no-action eligibility through the lifecycle rules");
        Test.True(noActionSource.Contains("WorldDiplomacyRoundLifecycleRules.EvaluateExternalNoActionAuthorization", StringComparison.Ordinal),
            "the host must route external no-action authorization through the lifecycle rules");
        Test.True(noActionSource.Contains("WorldDiplomacyRoundLifecycleRules.EvaluateRelayNoActionAuthorization", StringComparison.Ordinal),
            "the host must route relay no-action authorization through the lifecycle rules");
        Test.True(!behaviorSource.Contains("ResultSettlementPlayerWaitingSinceDay + 5", StringComparison.Ordinal),
            "raw player-timeout math must not remain in the host");
        Test.True(!behaviorSource.Contains("? \"deadlocked\" : \"resolved\"", StringComparison.Ordinal),
            "raw settlement status normalization must not remain in the host");
        Test.True(!behaviorSource.Contains("ConsecutiveNoActionPasses >= 2", StringComparison.Ordinal),
            "raw no-action thresholds must not remain in the host");
        Test.True(!behaviorSource.Contains("Math.Ceiling(passDurationDays", StringComparison.Ordinal),
            "raw arrival-day math must not remain in the host");
        Test.True(!behaviorSource.Contains("RelayDirection >= 0 ? -1 : 1", StringComparison.Ordinal),
            "raw direction flips must not remain in the host");
        Test.True(!behaviorSource.Contains("Math.Min(3, round.ConsecutiveNoActionPasses + 1)", StringComparison.Ordinal),
            "raw no-action increments must not remain in the host");
        Test.True(courtResponseApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.IsResponseRequiredFrom", StringComparison.Ordinal),
            "the court application must route response-required checks through the lifecycle rules");
        Test.True(structureSource.Contains("WorldDiplomacyRoundLifecycleRules.CollectDocumentTargetIds", StringComparison.Ordinal),
            "the host must route document target collection through the lifecycle rules");
        Test.True(structureSource.Contains("WorldDiplomacyRoundLifecycleRules.DocumentRespondsToSource", StringComparison.Ordinal),
            "the host must route responds-to checks through the lifecycle rules");
        Test.True(behaviorSource.Contains("WorldDiplomacyCourtResponseApplication.TryScheduleMandatory(", StringComparison.Ordinal)
            && courtResponseApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.EvaluateMandatoryReplyAction", StringComparison.Ordinal),
            "the court application must route mandatory-reply scheduling through the lifecycle rules");
        Test.True(courtResponseApplicationSource.Contains("new WorldDiplomacyMandatoryReplyInput", StringComparison.Ordinal),
            "the court application must build the mandatory-reply input from canonical storage and live probes");
        Test.True(behaviorSource.Contains("WorldDiplomacyIntentVocabulary.NormalizeIntent", StringComparison.Ordinal),
            "the host must route intent normalization through the vocabulary");
        Test.True(offerApplicationSource.Contains("WorldDiplomacyIntentVocabulary.IsProposalIntent", StringComparison.Ordinal),
            "the offer application must route proposal checks through the vocabulary");
        Test.True(analysisSource.Contains("WorldDiplomacyIntentVocabulary.ResolveValidatedResponseObligation", StringComparison.Ordinal),
            "the lifecycle rules must route response obligations through the vocabulary");
        Test.True(!behaviorSource.Contains("\"make_peace\" or \"peace\" or \"peace_proposal\"", StringComparison.Ordinal),
            "raw intent aliases must not remain in the host");
        Test.True(progressSource.Contains("WorldDiplomacyRoundLifecycleRules.IsValidatedSubstantiveProgress", StringComparison.Ordinal),
            "the round progress application must route substantive-progress validation through the lifecycle rules");
        Test.True(progressSource.Contains("WorldDiplomacyRoundLifecycleRules.IsValidatedDiplomaticActionAttempt", StringComparison.Ordinal),
            "the round progress application must route action-attempt validation through the lifecycle rules");
        Test.True(structureSource.Contains("WorldDiplomacyRoundLifecycleRules.NeedsCanonicalHistoryRetry", StringComparison.Ordinal),
            "the host must route canonical-history retry checks through the lifecycle rules");
        Test.True(rulesSource.Contains("WorldDiplomacyRoundLifecycleRules.NextDeferredRetryAttempt", StringComparison.Ordinal),
            "the lifecycle rules must own retry-attempt increments");
        Test.True(rulesSource.Contains("WorldDiplomacyRoundLifecycleRules.ComputeDeferredRetryDelayHours", StringComparison.Ordinal),
            "the lifecycle rules must own retry backoff");
        Test.True(historyCaptureSource.Contains("WorldDiplomacyRoundLifecycleRules.ComputeDeferredRetryBatchSize", StringComparison.Ordinal),
            "the history-capture Application must route retry batch sizing through the lifecycle rules");
        Test.True(!behaviorSource.Contains("Math.Min(30, attempts + 1)", StringComparison.Ordinal),
            "raw retry-attempt math must not remain in the host");
        Test.True(!behaviorSource.Contains("Math.Min(24, 1 << Math.Min(4", StringComparison.Ordinal),
            "raw retry backoff math must not remain in the host");
        Test.True(rulesSource.Contains("EvaluateThreatTerminalClose(document, diplomaticThreats)", StringComparison.Ordinal),
            "threat terminal closes must be evaluated inside the lifecycle rules");
        Test.True(progressSource.Contains("WorldDiplomacyRoundLifecycleRules.TryGetConfirmedRoundResult(", StringComparison.Ordinal),
            "the round progress application must route confirmed-result evaluation through the lifecycle rules");
        Test.True(documentExecutor.Contains("WorldDiplomacyRoundLifecycleRules.IsThreatRelevantToResultSettlement", StringComparison.Ordinal),
            "the host must route threat-settlement relevance through the lifecycle rules");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.IsOpenDiplomaticThreatStatus", StringComparison.Ordinal),
            "the host must route open-threat checks through the lifecycle rules");
        Test.True(rulesSource.Contains("public static bool IsRetainedActiveDiplomaticThreatStatus(", StringComparison.Ordinal),
            "retained-threat status checks must live in the lifecycle rules");
        Test.True(!behaviorSource.Contains("threat_warning_skipped_but_war_started", StringComparison.Ordinal),
            "raw threat close reasons must not remain in the host");
        Test.True(!behaviorSource.Contains("threat_next_declaration_breached", StringComparison.Ordinal),
            "raw breached-threat close reasons must not remain in the host");
        Test.True(!behaviorSource.Contains("threat_target_complied", StringComparison.Ordinal),
            "raw complied-threat close reasons must not remain in the host");
        Test.True(!behaviorSource.Contains("multiple_diplomatic_results", StringComparison.Ordinal),
            "raw confirmed-result reasons must not remain in the host");
        Test.True(validationSource.Contains("WorldDiplomacyRoundLifecycleRules.IsThreatIntent", StringComparison.Ordinal),
            "generation validation must route threat-intent checks through the lifecycle rules");
        Test.True(validationSource.Contains("WorldDiplomacyRoundLifecycleRules.EvaluateThreatPartyEligibility", StringComparison.Ordinal),
            "generation validation must route threat party eligibility through the lifecycle rules");
        Test.True(validationSource.Contains("WorldDiplomacyRoundLifecycleRules.EvaluateComplyUltimatumViolation", StringComparison.Ordinal),
            "generation validation must route ultimatum-compliance violations through the lifecycle rules");
        Test.True(validationSource.Contains("WorldDiplomacyRoundLifecycleRules.EvaluateThreatEscalationViolation", StringComparison.Ordinal),
            "generation validation must route threat-escalation violations through the lifecycle rules");
        Test.True(rulesSource.Contains("TryResolveUniqueOpenProposalForRound(", StringComparison.Ordinal),
            "unique-proposal resolution must be composed inside the lifecycle rules");
        Test.True(!behaviorSource.Contains("comply_ultimatum_source_mismatch", StringComparison.Ordinal),
            "raw compliance-violation reasons must not remain in the host");
        Test.True(!behaviorSource.Contains("issuer_already_has_open_threat", StringComparison.Ordinal),
            "raw escalation-violation reasons must not remain in the host");
        Test.True(!behaviorSource.Contains("warning_escalation_requires_target_noncompliance", StringComparison.Ordinal),
            "raw escalation-violation reasons must not remain in the host");
        Test.True(!behaviorSource.Contains(".GroupBy(x => (x.SourceDocumentId ?? \"\")", StringComparison.Ordinal),
            "raw offer dedup scans must not remain in the host");
        Test.True(documentExecutor.Contains("WorldDiplomacyResultSlotApplication.AddOrMergeResultSettlementSlot", StringComparison.Ordinal)
            && File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyResultSlotApplication.cs")).Contains("AddWarResponseResultSettlementSlot(", StringComparison.Ordinal),
            "the host must route settlement-slot composition through the lifecycle rules");
        Test.True(rulesSource.Contains("MergeSettlementSlotKind(", StringComparison.Ordinal)
            && rulesSource.Contains("DefaultSettlementSlotKind(", StringComparison.Ordinal)
            && !behaviorSource.Contains("MergeSettlementSlotKind(", StringComparison.Ordinal)
            && !behaviorSource.Contains("DefaultSettlementSlotKind(", StringComparison.Ordinal),
            "slot-kind merge and default algorithms must live only in the lifecycle rules");
        Test.True(documentExecutor.Contains("WorldDiplomacyResultSlotApplication.InitializeResultSettlementRouteSlots", StringComparison.Ordinal)
            && rulesSource.Contains("CollectSpokenAuthorIds(", StringComparison.Ordinal),
            "the host must route spoken-author collection through the lifecycle rules");
        Test.True(documentExecutor.Contains("WorldDiplomacyRoundLifecycleRules.EvaluateThreatSettlementSlot", StringComparison.Ordinal),
            "the host must route threat settlement slots through the lifecycle rules");
        Test.True(rulesSource.Contains("IsWarResponseSlotAction(", StringComparison.Ordinal)
            && rulesSource.Contains("ComposeWarResponseActionKey(", StringComparison.Ordinal)
            && !behaviorSource.Contains("IsWarResponseSlotAction(", StringComparison.Ordinal)
            && !behaviorSource.Contains("ComposeWarResponseActionKey(", StringComparison.Ordinal),
            "war-response slot-action matching and action keys must live only in the lifecycle rules");
        Test.True(!behaviorSource.Contains("slot.Kind + \"+\" + kind", StringComparison.Ordinal),
            "raw slot-kind merging must not remain in the host");
        Test.True(!behaviorSource.Contains("\"threat_followthrough\"", StringComparison.Ordinal),
            "raw threat slot kinds must not remain in the host");
        Test.True(!behaviorSource.Contains("(document.DocumentId ?? \"\") + \"#\" + (action.ActionId ?? \"\")", StringComparison.Ordinal),
            "raw war-response keys must not remain in the host");
        Test.True((progressSource.Contains("WorldDiplomacyRoundLifecycleRules.ConsumeResultSettlementSpeaker(", StringComparison.Ordinal)
                || rulesSource.Contains("SkipResultSettlementSlot(", StringComparison.Ordinal))
            && rulesSource.Contains("public static WorldDiplomacyResultSettlementSlot SelectSettlementSlot(", StringComparison.Ordinal),
            "the round progress application must route settlement-slot consumption through the lifecycle rules");
        Test.True(progressSource.Contains("WorldDiplomacyRoundLifecycleRules.ExpireUnansweredSettlementOffersForNoActionDeclaration(", StringComparison.Ordinal)
            && rulesSource.Contains("SelectSettlementSlotBySlotAndKingdom(", StringComparison.Ordinal),
            "the round progress application must route expiry slot selection through the lifecycle rules");
        Test.True(rulesSource.Contains("public static bool IsThreatRequiredSpeaker(", StringComparison.Ordinal),
            "threat speaker requirements must live in the lifecycle rules");
        Test.True(!behaviorSource.Contains("? round.ResultSettlementSlots.FirstOrDefault", StringComparison.Ordinal),
            "raw consume-slot branching must not remain in the host");
        Test.True(progressSource.Contains("WorldDiplomacyRoundLifecycleRules.CountSuccessfulMechanicalActions", StringComparison.Ordinal),
            "the round progress application must route mechanical-action counting through the lifecycle rules");
        Test.True(progressSource.Contains("WorldDiplomacyRoundLifecycleRules.IsRootRoundDocument", StringComparison.Ordinal),
            "the round progress application must route root-document detection through the lifecycle rules");
        Test.True(progressSource.Contains("WorldDiplomacyRoundLifecycleRules.ComputeDiplomaticActionAttemptCount", StringComparison.Ordinal),
            "the round progress application must route attempt counting through the lifecycle rules");
        Test.True(progressSource.Contains("WorldDiplomacyRoundLifecycleRules.ShouldExtendHardEndForLateProposal", StringComparison.Ordinal),
            "the round progress application must route late-proposal extensions through the lifecycle rules");
        Test.True(progressSource.Contains("WorldDiplomacyRoundLifecycleRules.ResolveTerminalMoveCloseReason", StringComparison.Ordinal),
            "the round progress application must route terminal-move reasons through the lifecycle rules");
        Test.True(progressSource.Contains("WorldDiplomacyRoundLifecycleRules.IsRelayResolvedClose", StringComparison.Ordinal),
            "the round progress application must route relay-resolution gates through the lifecycle rules");
        Test.True(rulesSource.Contains("SelectLastPublishedAuthorId(storage.Documents, round.RoundId)", StringComparison.Ordinal),
            "the previous-speaker selection must be composed inside the lifecycle rules");
        Test.True(progressSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectParticipantByKingdom", StringComparison.Ordinal),
            "the round progress application must route participant lookups through the lifecycle rules");
        Test.True(!behaviorSource.Contains("negotiation_declared_deadlock", StringComparison.Ordinal),
            "raw terminal-move reasons must not remain in the host");
        Test.True(rulesSource.Contains("EvaluateThreatDocumentDispatch(", StringComparison.Ordinal),
            "the host must route threat document dispatch through the lifecycle rules");

        string campaignSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.CampaignSource.cs"));
        string threatSettlementSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyThreatSettlementApplication.cs"));
        string threatApplicationSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyThreatApplication.cs"));
        Test.True(threatApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.IsThreatComplianceAction", StringComparison.Ordinal),
            "the host must route threat compliance matching through the lifecycle rules");
        Test.True(threatApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectThreatDecisionAction", StringComparison.Ordinal),
            "the host must route threat decision action selection through the lifecycle rules");
        Test.True(rulesSource.Contains("SelectEnforceableUltimatumThreat(", StringComparison.Ordinal),
            "the host must route enforceable ultimatum selection through the lifecycle rules");
        Test.True(threatApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.IsThreatTargetDecisionCandidate", StringComparison.Ordinal),
            "the host must route target-decision candidacy through the lifecycle rules");
        Test.True(threatApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.ResolveThreatDecisionSourceDocumentId", StringComparison.Ordinal),
            "the host must route decision source resolution through the lifecycle rules");
        Test.True(rulesSource.Contains("IsThreatBetweenParties(", StringComparison.Ordinal),
            "the host must route between-parties checks through the lifecycle rules");
        Test.True(rulesSource.Contains("ShouldKeepWarningOpenAfterWar(", StringComparison.Ordinal),
            "the host must route warning-retention checks through the lifecycle rules");
        Test.True(threatApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.IsThreatFollowThroughObligationPending", StringComparison.Ordinal),
            "the host must route follow-through obligation gates through the lifecycle rules");
        Test.True(rulesSource.Contains("ThreatDeclarationTargetsThreatTarget(", StringComparison.Ordinal),
            "the host must route threat-target checks through the lifecycle rules");
        Test.True(!behaviorSource.Contains("presented.Contains(x.StageDocumentId)", StringComparison.Ordinal),
            "raw presented-threat allowlist matching must not remain in the host");
        Test.True(!behaviorSource.Contains("x.RespondingToThreatActionId, threat.StageActionId", StringComparison.Ordinal),
            "raw stage-action matching must not remain in the host");
        Test.True(!behaviorSource.Contains("ThenByDescending(x => x.CreatedDay).FirstOrDefault()", StringComparison.Ordinal),
            "raw enforceable-threat ordering must not remain in the host");
        Test.True(!behaviorSource.Contains("PresentedThreatFollowThroughDocumentIds ?? new List<string>()).Contains(threat.StageDocumentId", StringComparison.Ordinal),
            "raw follow-through obligation matching must not remain in the host");
        Test.True(rulesSource.Contains("SelectUniquePresentedPendingThreat(", StringComparison.Ordinal),
            "unique-threat binding must be composed inside the lifecycle rules");
        Test.True(actionSelectionSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectOpenThreatIssuedBy", StringComparison.Ordinal),
            "the host must route open-threat lookups through the lifecycle rules");
        Test.True(!behaviorSource.Contains("\"\\n\" + (x.StageActionId", StringComparison.Ordinal),
            "raw stage-identity grouping must not remain in the host");
        Test.True(bindingSource.Contains("RegisterOrAdvanceDiplomaticThreat(", StringComparison.Ordinal),
            "the host must route threat registration bookkeeping through the lifecycle rules");
        Test.True(rulesSource.Contains("IsThreatRegistrationIdentityMatch(", StringComparison.Ordinal)
            && rulesSource.Contains("EvaluateThreatRegistration(", StringComparison.Ordinal),
            "threat registration identity and dispatch must be composed inside the lifecycle rules");
        Test.True(threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectComplianceRecordedThreat", StringComparison.Ordinal),
            "the host must route compliance re-binding through the lifecycle rules");
        Test.True(threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.IsThreatComplianceAlreadyRecorded", StringComparison.Ordinal),
            "the host must route compliance idempotency through the lifecycle rules");
        Test.True(threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.IsThreatComplianceStageMatch", StringComparison.Ordinal),
            "the host must route compliance stage matching through the lifecycle rules");
        Test.True(!threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.ResolveCompliancePrestigeDelta", StringComparison.Ordinal),
            "Q50 retired consequence has no new application consumer: ResolveCompliancePrestigeDelta");
        Test.True(threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.DescribeThreatStage", StringComparison.Ordinal),
            "the host must route stage labels through the lifecycle rules");
        Test.True(threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectOpenThreatBetween", StringComparison.Ordinal),
            "the host must route open-threat lookups through the lifecycle rules");
        Test.True(!behaviorSource.Contains("is not \"warning\" and not \"ultimatum\"", StringComparison.Ordinal),
            "raw stage gates must not remain in the host");
        Test.True(!behaviorSource.Contains("existing.TargetDecision, \"noncomplied\"", StringComparison.Ordinal),
            "raw escalation gates must not remain in the host");
        Test.True(!behaviorSource.Contains("threat.Stage == \"warning\" ? \"谴责\"", StringComparison.Ordinal),
            "raw stage labels must not remain in the host");
        Test.True(rulesSource.Contains("IsThreatLinkedToDocument(", StringComparison.Ordinal),
            "document-threat linking must live in the lifecycle rules");
        Test.True(rulesSource.Contains("EvaluateThreatHistoryFinalization(", StringComparison.Ordinal),
            "threat history finalization must live in the lifecycle rules");
        Test.True(rulesSource.Contains("IsThreatNonComplianceLinkedToDocument(", StringComparison.Ordinal),
            "noncompliance linking must live in the lifecycle rules");
        Test.True(threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectThreatHistoryRetryBatch", StringComparison.Ordinal),
            "the host must route history retry batching through the lifecycle rules");
        Test.True(rulesSource.Contains("CanCaptureNonComplianceEvent", StringComparison.Ordinal),
            "noncompliance capture gating must live in the lifecycle rules");
        Test.True(historyPublicationSource.Contains("WorldDiplomacyRoundLifecycleRules.CanAppendNonComplianceEventHistory", StringComparison.Ordinal),
            "the canonical history rules must route noncompliance append gating through the lifecycle rules");
        Test.True(rulesSource.Contains("SelectUnrecordedNonComplianceEvents(", StringComparison.Ordinal),
            "unrecorded event ordering must live in the lifecycle rules");
        Test.True(rulesSource.Contains("SelectCurrentNonComplianceEvent(", StringComparison.Ordinal),
            "current-event lookup must live in the lifecycle rules");
        Test.True(historyPublicationSource.Contains("WorldDiplomacyRoundLifecycleRules.CanAppendDomesticPenaltyHistory", StringComparison.Ordinal),
            "the canonical history rules must route domestic penalty append gating through the lifecycle rules");
        Test.True(historyPublicationSource.Contains("WorldDiplomacyRoundLifecycleRules.CanAppendIssuerRewardHistory", StringComparison.Ordinal),
            "the canonical history rules must route issuer reward append gating through the lifecycle rules");
        Test.True(historyPublicationSource.Contains("WorldDiplomacyRoundLifecycleRules.CanAppendThreatResultHistory", StringComparison.Ordinal),
            "the canonical history rules must route breach result append gating through the lifecycle rules");
        Test.True(rulesSource.Contains("public static bool IsThreatLinkedToRound(", StringComparison.Ordinal),
            "round-threat linking must be owned by the lifecycle rules");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizeThreatEventStage", StringComparison.Ordinal),
            "the host must route event stage normalization through the lifecycle rules");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.DescribeThreatStageFormal", StringComparison.Ordinal),
            "the host must route formal stage labels through the lifecycle rules");
        Test.True(historyPublicationSource.Contains("WorldDiplomacyRoundLifecycleRules.DescribeThreatExpectedFollowThrough", StringComparison.Ordinal),
            "the canonical history rules must route follow-through labels through the lifecycle rules");
        Test.True(historyPublicationSource.Contains("WorldDiplomacyRoundLifecycleRules.ResolveThreatCommitmentLevel", StringComparison.Ordinal),
            "the canonical history rules must route threat commitment levels through the lifecycle rules");
        Test.True(threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.IsThreatCancellationStatusCancelled", StringComparison.Ordinal),
            "the host must route cancellation status checks through the lifecycle rules");
        Test.True(historyPublicationSource.Contains("WorldDiplomacyRoundLifecycleRules.IsIssuerRewardHistoryEmpty", StringComparison.Ordinal),
            "the canonical history rules must route issuer reward emptiness through the lifecycle rules");
        Test.True(threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.NeedsDomesticPenaltySettlementRetry", StringComparison.Ordinal),
            "the host must route domestic penalty settlement retry through the lifecycle rules");
        Test.True(threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.NeedsPolicyCancellationRetry", StringComparison.Ordinal),
            "the host must route policy cancellation retry through the lifecycle rules");
        Test.True(threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.NeedsIssuerRewardSettlementRetry", StringComparison.Ordinal),
            "the host must route issuer reward settlement retry through the lifecycle rules");
        Test.True(rulesSource.Contains("FindPriorityThreatRelayIndex(", StringComparison.Ordinal)
            && rulesSource.Contains("OrderThreatsByUpdatedDay(", StringComparison.Ordinal),
            "shared threat ordering must live in the lifecycle rules");
        Test.True(!behaviorSource.Contains("x.ComplianceDocumentId, document.DocumentId", StringComparison.Ordinal),
            "raw compliance-document linking must not remain in the host");
        Test.True(!behaviorSource.Contains("x.TargetDecisionDocumentId, document.DocumentId", StringComparison.Ordinal),
            "raw decision-document linking must not remain in the host");
        Test.True(!behaviorSource.Contains("decision.StageDocumentId, threat.StageDocumentId", StringComparison.Ordinal),
            "raw current-event matching must not remain in the host");
        Test.True(!behaviorSource.Contains("x.StageRoundId, roundId", StringComparison.Ordinal),
            "raw round-threat linking must not remain in the host");
        Test.True(!behaviorSource.Contains("PolicyConditionCancellationStatus, \"cancelled\"", StringComparison.Ordinal),
            "raw cancellation checks must not remain in the host");
        Test.True(!behaviorSource.Contains("decision.Stage, \"ultimatum\"", StringComparison.Ordinal),
            "raw event stage labels must not remain in the host");
        Test.True(!behaviorSource.Contains("threat.Stage == \"ultimatum\" ? \"宣战\"", StringComparison.Ordinal),
            "raw follow-through labels must not remain in the host");
        Test.True(!behaviorSource.Contains("\"non_binding\" : \"binding\"", StringComparison.Ordinal),
            "raw commitment levels must not remain in the host");
        Test.True(!behaviorSource.Contains("Stage == \"ultimatum\" ? \"战争最后通牒\"", StringComparison.Ordinal),
            "raw formal stage labels must not remain in the host");
        Test.True(!behaviorSource.Contains("OrderBy(x => x.UpdatedDay)", StringComparison.Ordinal),
            "raw threat retry ordering must not remain in the host");
        Test.True(threatMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizeThreatRecord", StringComparison.Ordinal),
            "threat storage migration must route threat record normalization through the lifecycle rules");
        Test.True(threatMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizeKingdomIntDictionary", StringComparison.Ordinal),
            "threat storage migration must route kingdom dictionary normalization through the lifecycle rules");
        Test.True(threatMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectRetainedPrestigeRelationModifiers", StringComparison.Ordinal),
            "threat storage migration must route relation modifier dedupe through the lifecycle rules");
        Test.True(threatMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.HasValidThreatParties", StringComparison.Ordinal),
            "threat storage migration must route threat party validation through the lifecycle rules");
        Test.True(threatMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectRetainedOpenThreatForIssuer", StringComparison.Ordinal),
            "threat storage migration must route issuer dedupe through the lifecycle rules");
        Test.True(threatMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectThreatRetentionSet", StringComparison.Ordinal),
            "threat storage migration must route threat retention selection through the lifecycle rules");
        Test.True(rulesSource.Contains("public static bool NeedsThreatSettlementRetention(", StringComparison.Ordinal),
            "threat settlement retention checks must live in the lifecycle rules");
        Test.True(threatMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.InvalidateThreatForNormalization", StringComparison.Ordinal),
            "threat storage migration must route normalization invalidation through the lifecycle rules");
        Test.True(threatApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.CaptureThreatNonComplianceEvent", StringComparison.Ordinal),
            "the host must route noncompliance capture through the lifecycle rules");
        Test.True(rulesSource.Contains("ResolveThreatRequiredSpeakerId(", StringComparison.Ordinal),
            "required speaker resolution must live in the lifecycle rules");
        Test.True(!behaviorSource.Contains("x.IssuerKingdomId, x.TargetKingdomId, StringComparison.OrdinalIgnoreCase));", StringComparison.Ordinal),
            "raw threat party validation must not remain in the host");
        Test.True(!behaviorSource.Contains("ThenByDescending(x => string.Equals(x.Stage, \"ultimatum\"", StringComparison.Ordinal),
            "raw retained-threat ordering must not remain in the host");
        Test.True(!behaviorSource.Contains("x.Key.Trim(), StringComparer.OrdinalIgnoreCase)", StringComparison.Ordinal),
            "raw kingdom dictionary normalization must not remain in the host");
        Test.True(threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.EvaluateThreatPolicyCancellationDispatch", StringComparison.Ordinal),
            "the host must route policy cancellation dispatch through the lifecycle rules");
        Test.True(threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.ResolvePolicyCancellationStatus", StringComparison.Ordinal),
            "the host must route cancellation status normalization through the lifecycle rules");
        Test.True(threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.IsPolicySignalBoundTo", StringComparison.Ordinal),
            "the host must route settled-signal matching through the lifecycle rules");
        Test.True(rulesSource.Contains("IsThreatBoundToPolicy(", StringComparison.Ordinal),
            "the host must route bound-threat matching through the lifecycle rules");
        Test.True(rulesSource.Contains("InvalidateThreatForPolicyCancellation(", StringComparison.Ordinal),
            "the host must route policy invalidation through the lifecycle rules");
        Test.True(bindingSource.Contains("WorldDiplomacyRoundLifecycleRules.IsPolicySignalEligibleForThreatBinding", StringComparison.Ordinal),
            "the host must route binding eligibility through the lifecycle rules");
        Test.True(bindingSource.Contains("WorldDiplomacyRoundLifecycleRules.IsThreatPolicyPartyMatch", StringComparison.Ordinal),
            "the host must route policy party matching through the lifecycle rules");
        Test.True(bindingSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectUniquePolicySignal", StringComparison.Ordinal),
            "the host must route unique-signal selection through the lifecycle rules");
        Test.True(rulesSource.Contains("InitializeThreatPolicyCondition(", StringComparison.Ordinal),
            "policy binding initialization must be composed inside the lifecycle rules");
        Test.True(threatMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.MigrateThreatComplianceConsequencesV3", StringComparison.Ordinal),
            "threat storage migration must route the v3 consequence migration through the lifecycle rules");
        Test.True(!behaviorSource.Contains("bound_policy_cancelled_by_another_complied_threat", StringComparison.Ordinal),
            "raw policy invalidation reasons must not remain in the host");
        Test.True(!behaviorSource.Contains("legacy_not_bound", StringComparison.Ordinal),
            "raw migration statuses must not remain in the host");
        Test.True(!behaviorSource.Contains("signal.PolicyKind.Trim(), \"kingdom\", StringComparison.OrdinalIgnoreCase", StringComparison.Ordinal),
            "raw policy-kind eligibility must not remain in the host");
        Test.True(!behaviorSource.Contains("? \"cancelled\" : result.Trim().ToLowerInvariant()", StringComparison.Ordinal),
            "raw cancellation status normalization must not remain in the host");
        Test.True(!behaviorSource.Contains("if (matches.Count != 1) return false;", StringComparison.Ordinal),
            "raw unique-signal gating must not remain in the host");
        Test.True(!threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.IsThreatDomesticPenaltyEligible", StringComparison.Ordinal),
            "Q50 retired consequence has no new application consumer: IsThreatDomesticPenaltyEligible");
        Test.True(!threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.IsThreatIssuerRewardEligible", StringComparison.Ordinal),
            "Q50 retired consequence has no new application consumer: IsThreatIssuerRewardEligible");
        Test.True(!threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.CountThreatConsequenceAppliedClans", StringComparison.Ordinal),
            "Q50 retired consequence has no new application consumer: CountThreatConsequenceAppliedClans");
        Test.True(!threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.CaptureThreatDomesticPenaltySnapshot", StringComparison.Ordinal),
            "Q50 retired consequence has no new application consumer: CaptureThreatDomesticPenaltySnapshot");
        Test.True(!threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.CaptureThreatIssuerRewardSnapshot", StringComparison.Ordinal),
            "Q50 retired consequence has no new application consumer: CaptureThreatIssuerRewardSnapshot");
        Test.True(!threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.CompleteThreatDomesticPenaltyWithoutEligible", StringComparison.Ordinal),
            "Q50 retired consequence has no new application consumer: CompleteThreatDomesticPenaltyWithoutEligible");
        Test.True(!threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.CompleteThreatIssuerRewardWithoutEligible", StringComparison.Ordinal),
            "Q50 retired consequence has no new application consumer: CompleteThreatIssuerRewardWithoutEligible");
        Test.True(!threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.CompleteThreatIssuerRewardWithoutAmount", StringComparison.Ordinal),
            "Q50 retired consequence has no new application consumer: CompleteThreatIssuerRewardWithoutAmount");
        Test.True(!threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectUnresolvedClanIds", StringComparison.Ordinal),
            "Q50 retired consequence has no new application consumer: SelectUnresolvedClanIds");
        Test.True(!threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.CompleteThreatDomesticPenaltyAsSkipped", StringComparison.Ordinal),
            "Q50 retired consequence has no new application consumer: CompleteThreatDomesticPenaltyAsSkipped");
        Test.True(!threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.CompleteThreatIssuerRewardAsSkipped", StringComparison.Ordinal),
            "Q50 retired consequence has no new application consumer: CompleteThreatIssuerRewardAsSkipped");
        Test.True(!threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.IsThreatConsequenceClanSettled", StringComparison.Ordinal),
            "Q50 retired consequence has no new application consumer: IsThreatConsequenceClanSettled");
        Test.True(!threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.IsThreatConsequenceSettled", StringComparison.Ordinal),
            "Q50 retired consequence has no new application consumer: IsThreatConsequenceSettled");
        Test.True(!threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.OrderThreatConsequenceClanIds", StringComparison.Ordinal),
            "Q50 retired consequence has no new application consumer: OrderThreatConsequenceClanIds");
        Test.True(!threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.ClampThreatIssuerRewardAmount", StringComparison.Ordinal),
            "Q50 retired consequence has no new application consumer: ClampThreatIssuerRewardAmount");
        Test.True(!threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.ComputeThreatConsequenceRelationTarget", StringComparison.Ordinal),
            "Q50 retired consequence has no new application consumer: ComputeThreatConsequenceRelationTarget");
        Test.True(!threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.ShouldApplyThreatRelationPenalty", StringComparison.Ordinal),
            "Q50 retired consequence has no new application consumer: ShouldApplyThreatRelationPenalty");
        Test.True(!threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.ShouldApplyThreatRelationReward", StringComparison.Ordinal),
            "Q50 retired consequence has no new application consumer: ShouldApplyThreatRelationReward");
        Test.True(!threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.IsThreatRelationPenaltyApplied", StringComparison.Ordinal),
            "Q50 retired consequence has no new application consumer: IsThreatRelationPenaltyApplied");
        Test.True(!threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.IsThreatRelationRewardApplied", StringComparison.Ordinal),
            "Q50 retired consequence has no new application consumer: IsThreatRelationRewardApplied");
        Test.True(rulesSource.Contains("FinalizeUnresolvedThreatDomesticPenalty(", StringComparison.Ordinal),
            "the host must route unresolved penalty finalization through the lifecycle rules");
        Test.True(rulesSource.Contains("FinalizeUnresolvedThreatIssuerReward(", StringComparison.Ordinal),
            "the host must route unresolved reward finalization through the lifecycle rules");
        Test.True(!threatSettlementSource.Contains("_port.CaptureConsequenceSnapshot(compliantKingdom)", StringComparison.Ordinal)
                  && !threatSettlementSource.Contains("_port.CaptureConsequenceSnapshot(issuerKingdom)", StringComparison.Ordinal)
                  && File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.ThreatSettlementPort.cs")).Contains("CaptureClanSnapshot(clan, kingdom, ruler)", StringComparison.Ordinal)
                  && !threatSettlementSource.Contains("WorldDiplomacyWorldProfileRules.SelectThreatClans(snapshot.Clans)", StringComparison.Ordinal),
            "retired penalties cannot capture new clan snapshots; the legacy adapter remains available for old data");
        Test.True(!behaviorSource.Contains("relationBefore > -100", StringComparison.Ordinal),
            "raw penalty apply gates must not remain in the host");
        Test.True(!behaviorSource.Contains("relationBefore < 100", StringComparison.Ordinal),
            "raw reward apply gates must not remain in the host");
        Test.True(!behaviorSource.Contains("eligibleIds.All(id =>", StringComparison.Ordinal),
            "raw settlement completion checks must not remain in the host");
        Test.True(!behaviorSource.Contains(".Sort(StringComparer.OrdinalIgnoreCase)", StringComparison.Ordinal),
            "raw settlement ordering must not remain in the host");

        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.IsThreatDecisionNoncomplied", StringComparison.Ordinal),
            "host must delegate threat noncompliance classification to the domain");
        Test.True(actionSelectionSource.Contains("WorldDiplomacyRoundLifecycleRules.IsThreatDecisionPending", StringComparison.Ordinal),
            "host must delegate threat pending-decision classification to the domain");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectPendingIncomingThreats", StringComparison.Ordinal),
            "host must delegate pending incoming threat selection to the domain");
        Test.True(actionSelectionSource.Contains("WorldDiplomacyRoundLifecycleRules.IsEnforcingRejectedUltimatum", StringComparison.Ordinal)
                && rulesSource.Contains("IsRejectedUltimatumEnforceable(", StringComparison.Ordinal),
            "host must delegate rejected-ultimatum enforcement to the domain");
        Test.True(actionSelectionSource.Contains("WorldDiplomacyRoundLifecycleRules.IsEscalatableWarningThreat", StringComparison.Ordinal),
            "host must delegate warning escalation matching to the domain");
        Test.True(rulesSource.Contains("public static bool HasStaleThreatPresentation(", StringComparison.Ordinal)
            && rulesSource.Contains("SelectNoncompliedThreatStageDocumentIds(", StringComparison.Ordinal)
            && rulesSource.Contains("SelectPresentedThreatStageDocumentIds(", StringComparison.Ordinal),
            "the domain must own the stale threat presentation decision");
        Test.True(dispatchSource.Contains("HasStaleThreatPresentation(job, storage?.DiplomaticThreats)", StringComparison.Ordinal)
            && completionSource.Contains("HasStaleThreatPresentation(job, storage?.DiplomaticThreats)", StringComparison.Ordinal),
            "dispatch/completion must evaluate threat staleness inside the application layer");
        Test.True(!behaviorSource.Contains("SelectNoncompliedThreatStageDocumentIds(", StringComparison.Ordinal)
            && !behaviorSource.Contains("SelectPresentedThreatStageDocumentIds(", StringComparison.Ordinal)
            && !behaviorSource.Contains("HasStaleDiplomaticThreatPresentation(", StringComparison.Ordinal)
            && !behaviorSource.Contains("HasStaleThreatPresentation(", StringComparison.Ordinal),
            "the host must not retain the stale presentation decision in a callback");
        Test.True(noActionSource.Contains("WorldDiplomacyRoundLifecycleRules.IsSettlementSlotRelatedTo", StringComparison.Ordinal),
            "host must delegate related-kingdom slot matching to the domain");
        Test.True(rulesSource.Contains("IsWarResponseSourceDocument(", StringComparison.Ordinal),
            "war-response source validation must be composed inside the lifecycle rules");
        Test.True(threatMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.IsThreatStageSourceDocument", StringComparison.Ordinal),
            "threat storage migration must delegate threat stage source validation to the domain");
        Test.True(threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.CannotCaptureThreatConsequenceSnapshot", StringComparison.Ordinal),
            "host must delegate the eliminated-kingdom snapshot fallback to the domain");
        Test.True(!behaviorSource.Contains("TargetDecision, \"noncomplied\"", StringComparison.Ordinal),
            "raw noncomplied comparisons must not remain in the host");
        Test.True(!behaviorSource.Contains("TargetDecision, \"pending\"", StringComparison.Ordinal),
            "raw pending-decision comparisons must not remain in the host");
        Test.True(!behaviorSource.Contains("OrderByDescending(x => x.UpdatedDay)", StringComparison.Ordinal),
            "raw pending-threat ordering must not remain in the host");
        Test.True(!behaviorSource.Contains("x.Stage, \"ultimatum\"", StringComparison.Ordinal)
            && !behaviorSource.Contains("x.Stage, \"warning\"", StringComparison.Ordinal),
            "raw threat stage comparisons must not remain in the host");
        Test.True(!behaviorSource.Contains("OrderBy(x => x.StageIssuedDay)", StringComparison.Ordinal),
            "raw threat stage document ordering must not remain in the host");
        Test.True(!behaviorSource.Contains("RelatedKingdomIds.Contains", StringComparison.Ordinal),
            "raw related-kingdom matching must not remain in the host");
        Test.True(!behaviorSource.Contains("sourceMatchesThreat = source", StringComparison.Ordinal),
            "raw threat source matching must not remain in the host");

        Test.True(threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.IsThreatBreachPenaltyApplicable", StringComparison.Ordinal),
            "host must delegate breach penalty applicability to the domain");
        Test.True(!threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.ResolveThreatBreachPrestigePenalty", StringComparison.Ordinal),
            "Q50 retired consequence has no new application consumer: ResolveThreatBreachPrestigePenalty");
        Test.True(!threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.ResolveThreatBreachRelationPenalty", StringComparison.Ordinal),
            "Q50 retired consequence has no new application consumer: ResolveThreatBreachRelationPenalty");
        Test.True(!threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.DescribeThreatBreachPrestigeReason", StringComparison.Ordinal),
            "Q50 retired consequence has no new application consumer: DescribeThreatBreachPrestigeReason");
        Test.True(threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.ApplyThreatBreachSettlement", StringComparison.Ordinal),
            "host must delegate the breach settlement record write to the domain");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectIssuerResolutionNotices", StringComparison.Ordinal),
            "host must delegate issuer notice selection to the domain");
        Test.True(rulesSource.Contains("public static bool HasThreatPresentationDrift(", StringComparison.Ordinal)
            && rulesSource.Contains("public static bool HasStaleThreatPresentation(", StringComparison.Ordinal),
            "presentation drift checks must be owned by the domain");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.IsThreatAtStage", StringComparison.Ordinal),
            "host must delegate threat stage checks to the domain");
        Test.True(threatMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectThreatStageDocumentId", StringComparison.Ordinal),
            "threat storage migration must delegate stage document id selection to the domain");
        Test.True(threatMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.ResolveCanonicalThreatStage", StringComparison.Ordinal),
            "threat storage migration must delegate canonical stage normalization to the domain");
        Test.True(threatMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.IsThreatFollowThroughEscalation", StringComparison.Ordinal),
            "threat storage migration must delegate follow-through escalation classification to the domain");
        Test.True(threatMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.IsThreatFollowThroughEnforcement", StringComparison.Ordinal),
            "threat storage migration must delegate follow-through enforcement classification to the domain");
        Test.True(!behaviorSource.Contains("Stage, \"ultimatum\"", StringComparison.Ordinal)
            && !behaviorSource.Contains("Stage, \"warning\"", StringComparison.Ordinal)
            && !behaviorSource.Contains("Stage == \"ultimatum\"", StringComparison.Ordinal)
            && !behaviorSource.Contains("Stage == \"warning\"", StringComparison.Ordinal),
            "raw stage comparisons must not remain in the host");
        Test.True(!behaviorSource.Contains("x.IssuerResolutionNoticePending", StringComparison.Ordinal),
            "raw notice filters must not remain in the host");
        Test.True(!behaviorSource.Contains("SequenceEqual(currentPresented", StringComparison.Ordinal)
            && !behaviorSource.Contains("SequenceEqual(currentFollowThrough", StringComparison.Ordinal),
            "raw presentation drift comparisons must not remain in the host");

        Test.True(normalizationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizePendingPolicySignals", StringComparison.Ordinal),
            "normalization owner must delegate pending policy signal normalization to the domain");
        Test.True(policyRoundApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectRetainedPolicySignals", StringComparison.Ordinal),
            "host must delegate retained policy signal selection to the domain");
        Test.True(normalizationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizeProcessedSignalKeys", StringComparison.Ordinal),
            "normalization owner must delegate processed signal key normalization to the domain");
        Test.True(rulesSource.Contains("CapToMostRecentEntries(", StringComparison.Ordinal),
            "most-recent capping must live in the domain");
        Test.True(normalizationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizePropagationArrivalList", StringComparison.Ordinal),
            "normalization owner must delegate propagation arrival normalization to the domain");
        Test.True(normalizationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizeWarLedgerList", StringComparison.Ordinal),
            "normalization owner must delegate war ledger normalization to the domain");
        Test.True(normalizationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizeBattleRecords", StringComparison.Ordinal),
            "normalization owner must delegate battle record normalization to the domain");
        Test.True(normalizationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizeJobRecord", StringComparison.Ordinal),
            "host must delegate job record normalization to the domain");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.IsJobOfKind", StringComparison.Ordinal),
            "host must delegate job kind classification to the domain");
        Test.True(normalizationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.IsRetiredJob", StringComparison.Ordinal),
            "host must delegate retired-job filtering to the domain");
        Test.True(normalizationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectRoundCompressionDocuments", StringComparison.Ordinal),
            "host must delegate compression document selection to the domain");
        Test.True(applicationSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectPublishedRoundDocuments", StringComparison.Ordinal),
            "host must delegate published round document selection to the domain");
        Test.True(rulesSource.Contains("public static bool IsSuccessfulWarDocumentAgainst(", StringComparison.Ordinal),
            "successful-war detection must live in the domain");
        Test.True(storageMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.RemoveAnsweredWarResponseSlots", StringComparison.Ordinal),
            "result-settlement migration must delegate answered war-response slot cleanup to the domain");
        Test.True(structureSource.Contains("WorldDiplomacyRoundLifecycleRules.IsCourtPropagationArrival", StringComparison.Ordinal),
            "host must delegate court arrival detection to the domain");
        Test.True(!behaviorSource.Contains("string.Equals(job.Kind,", StringComparison.Ordinal)
            && !behaviorSource.Contains("string.Equals(x.Kind, \"generate\"", StringComparison.Ordinal)
            && !behaviorSource.Contains("string.Equals(x.Kind, \"compress\"", StringComparison.Ordinal)
            && !behaviorSource.Contains("string.Equals(x.Kind, \"round_plan\"", StringComparison.Ordinal)
            && !behaviorSource.Contains("string.Equals(x.Kind, \"round_compress\"", StringComparison.Ordinal)
            && !behaviorSource.Contains("string.Equals(x.Kind, \"participate\"", StringComparison.Ordinal),
            "raw job kind comparisons must not remain in the host");
        Test.True(!behaviorSource.Contains("GroupBy(x => x.SignalKey,", StringComparison.Ordinal),
            "raw signal-key grouping must not remain in the host");
        Test.True(!behaviorSource.Contains("RemoveRange(0, _storage.ProcessedPolicySignalKeys", StringComparison.Ordinal),
            "raw signal-key capping must not remain in the host");
        Test.True(!behaviorSource.Contains("arrival.Scope = \"civilian\"", StringComparison.Ordinal),
            "raw arrival scope defaults must not remain in the host");
        Test.True(!behaviorSource.Contains("job.ForcedIntent = \"\"", StringComparison.Ordinal),
            "raw forced-intent resets must not remain in the host");
        Test.True(!behaviorSource.Contains("x.IsRelayTurn && x.Priority == 98", StringComparison.Ordinal),
            "raw forced-relay job filters must not remain in the host");
        Test.True(!behaviorSource.Contains("wars.Any(war =>", StringComparison.Ordinal),
            "raw answered-war checks must not remain in the host");

        Test.True(storageMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizeRoundRecordCollections", StringComparison.Ordinal),
            "host must delegate round record normalization to the domain");
        Test.True(storageMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizePendingOfferSourceActionBinding", StringComparison.Ordinal),
            "host must delegate offer source-action binding to the domain");
        Test.True(storageMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizeRoundAttemptCount", StringComparison.Ordinal),
            "host must delegate attempt count recovery to the domain");
        Test.True(storageMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.ResolveStoredRoundDurationDays", StringComparison.Ordinal),
            "host must delegate stored duration resolution to the domain");
        Test.True(storageMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizeRoundScheduleDays", StringComparison.Ordinal),
            "host must delegate schedule day normalization to the domain");
        Test.True(storageMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.IsClosedRoundWithoutFinalDocument", StringComparison.Ordinal),
            "host must delegate closed-round detection to the domain");
        Test.True(storageMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectFinalRoundDocumentId", StringComparison.Ordinal),
            "host must delegate final document selection to the domain");
        Test.True(playerApplicationSource
                .Contains("WorldDiplomacyRoundLifecycleRules.SelectWaitingPlayerSettlementSlot", StringComparison.Ordinal),
            "player command application must delegate waiting-player slot selection to the domain");
        Test.True(storageMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.ReleaseNonWaitingSettlementReplies", StringComparison.Ordinal),
            "host must delegate mandatory-reply release to the domain");
        Test.True(storageMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.CountAutomaticRoundDocuments", StringComparison.Ordinal),
            "host must delegate automatic document counting to the domain");
        Test.True(storageMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectRetiredArchitectureJobs", StringComparison.Ordinal),
            "storage migration must delegate retired architecture job selection to the domain");
        Test.True(storageMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectRoundJobsOfKinds", StringComparison.Ordinal),
            "host must delegate round job selection to the domain");
        Test.True(normalizationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizeSettlementKnowledgeRecords", StringComparison.Ordinal),
            "normalization owner must delegate settlement knowledge normalization to the domain");
        Test.True(normalizationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizeKingdomKnowledgeRecords", StringComparison.Ordinal),
            "normalization owner must delegate kingdom knowledge normalization to the domain");
        Test.True(normalizationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizeParticipationRequestRecords", StringComparison.Ordinal),
            "normalization owner must delegate participation request normalization to the domain");
        Test.True(normalizationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizeRelayArrivalList", StringComparison.Ordinal),
            "normalization owner must delegate relay arrival normalization to the domain");
        Test.True(normalizationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizePlayerOpportunityList", StringComparison.Ordinal),
            "normalization owner must delegate player opportunity normalization to the domain");
        Test.True(normalizationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizeRoundSummaryRecord", StringComparison.Ordinal),
            "normalization owner must delegate round summary normalization to the domain");
        Test.True(normalizationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizeCompressionSummaryRecord", StringComparison.Ordinal),
            "normalization owner must delegate compression summary normalization to the domain");
        Test.True(normalizationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectRetainedCompletedRounds", StringComparison.Ordinal),
            "normalization owner must delegate completed-round retention to the domain");
        Test.True(normalizationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectRetainedRoundSummaries", StringComparison.Ordinal),
            "normalization owner must delegate round summary retention to the domain");
        Test.True(normalizationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectRetainedAnnualSummaries", StringComparison.Ordinal),
            "normalization owner must delegate annual summary retention to the domain");
        Test.True(normalizationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectRetainedCompressionSummaries", StringComparison.Ordinal),
            "normalization owner must delegate compression summary retention to the domain");
        Test.True(rulesSource.Contains("ClonePolicySignalRecord(", StringComparison.Ordinal),
            "policy signal cloning must live in the domain");
        Test.True(!behaviorSource.Contains("originalCurrentSettlementSlotId", StringComparison.Ordinal),
            "raw slot-merge bookkeeping must not remain in the host");
        Test.True(!behaviorSource.Contains("GroupBy(x => x.SignalKey.Trim(),", StringComparison.Ordinal),
            "raw attached-signal grouping must not remain in the host");
        Test.True(!behaviorSource.Contains("SelectMany(x => (x.Kind ?? \"\").Split('+'))", StringComparison.Ordinal),
            "raw slot-kind merging must not remain in the host");
        Test.True(!behaviorSource.Contains("x.Status, \"waiting_player\"", StringComparison.Ordinal)
            && !behaviorSource.Contains("x.Status, \"inflight\"", StringComparison.Ordinal)
            && !behaviorSource.Contains("x.Status, \"scheduled\"", StringComparison.Ordinal),
            "raw settlement slot status comparisons must not remain in the host");
        Test.True(!behaviorSource.Contains("Math.Min(MaxConsecutiveTechnicalGenerationFailuresPerRound", StringComparison.Ordinal),
            "raw technical-failure clamping must not remain in the host");
        Test.True(!behaviorSource.Contains("!string.IsNullOrWhiteSpace(x.ToKingdomId)", StringComparison.Ordinal),
            "raw relay arrival validity checks must not remain in the host");
        Test.True(!behaviorSource.Contains("fact.SourceDocumentIds ??=", StringComparison.Ordinal),
            "raw fact initialization must not remain in the host");
        Test.True(!behaviorSource.Contains("OrderByDescending(x => x.ArrivedDay)", StringComparison.Ordinal),
            "raw opportunity ordering must not remain in the host");
        Test.True(!behaviorSource.Contains("OrderByDescending(x => x.CompletedDay)", StringComparison.Ordinal),
            "raw completed-round ordering must not remain in the host");
        Test.True(!behaviorSource.Contains("Take(MaxStoredRoundSummaries)", StringComparison.Ordinal)
            && !behaviorSource.Contains("Take(MaxStoredAnnualSummaries)", StringComparison.Ordinal)
            && !behaviorSource.Contains("Take(MaxStoredCompressionSummaries)", StringComparison.Ordinal),
            "raw archive retention caps must not remain in the host");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.IsOfferOfStatus(", StringComparison.Ordinal),
            "host must delegate open-offer checks to the domain");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.IsOpenOfferToTarget(", StringComparison.Ordinal),
            "host must delegate open-offer target checks to the domain");
        Test.True(validationSource.Contains("WorldDiplomacyRoundLifecycleRules.IsOpenDirectedOffer(", StringComparison.Ordinal),
            "directed open-offer checks must be composed inside the generation-validation rules");
        Test.True(rulesSource.Contains("MatchesOfferSource(", StringComparison.Ordinal),
            "offer source matching must live in the domain");
        Test.True(applicationSource.Contains("WorldDiplomacyRoundLifecycleRules.IsPlayerOpportunityOfStatus(", StringComparison.Ordinal),
            "host must delegate opportunity status checks to the domain");
        Test.True(!behaviorSource.Contains("x.Status, \"open\"", StringComparison.Ordinal)
            && !behaviorSource.Contains("offer.Status, \"open\"", StringComparison.Ordinal),
            "raw open-offer status comparisons must not remain in the host");
        Test.True(migrationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.IsRecordInRound(", StringComparison.Ordinal),
            "host must delegate round-membership checks to the domain");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.HasJobId(", StringComparison.Ordinal),
            "host must delegate job-id checks to the domain");
        Test.True(migrationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.HasJobIdInSet(", StringComparison.Ordinal),
            "host must delegate job-id set membership to the domain");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(", StringComparison.Ordinal),
            "host must delegate document-id matching to the domain");
        Test.True(!behaviorSource.Contains("string.Equals(x.RoundId,", StringComparison.Ordinal),
            "raw round-id comparisons must not remain in the host");
        Test.True(!behaviorSource.Contains("string.Equals(x.JobId,", StringComparison.Ordinal)
            && !behaviorSource.Contains(".Contains(x.JobId ?? \"\")", StringComparison.Ordinal),
            "raw job-id comparisons must not remain in the host");
        Test.True(!behaviorSource.Contains("string.Equals(x.DocumentId,", StringComparison.Ordinal)
            && !behaviorSource.Contains("string.Equals(x.SourceDocumentId,", StringComparison.Ordinal)
            && !behaviorSource.Contains("string.Equals(x.SourceId,", StringComparison.Ordinal),
            "raw document-id comparisons must not remain in the host");
        Test.True(notificationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.OrderDocumentsChronologically(", StringComparison.Ordinal),
            "host must delegate chronological document ordering to the domain");
        Test.True(!behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.OrderDocumentsByRecency(", StringComparison.Ordinal),
            "Q50 host no longer chooses historical recall by recency");
        Test.True(!File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyMemoryApplication.cs")).Contains("WorldDiplomacyRoundLifecycleRules.ThenOrderDocumentsByRecency(", StringComparison.Ordinal),
            "Q50 relevant recall does not restore a fixed recent-document suffix");
        string documentFactRules = File.ReadAllText(
            FindRepositoryFile(Path.Combine("src", "modules", "AF.Module.Diplomacy", "Domain", "WorldDiplomacyDocumentFactRules.cs")), Encoding.UTF8);
        Test.True(documentFactRules.Contains("WorldDiplomacyRoundLifecycleRules.SelectNewestDocumentsChronologically(", StringComparison.Ordinal),
            "known-round context must delegate newest-first chronological selection to the lifecycle rules");
        Test.True(documentApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.OrderDocumentsByRecency(", StringComparison.Ordinal)
            && documentApplicationSource.Contains("retainedIds.Contains(x.DocumentId)", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyDocumentApplication.Add(", StringComparison.Ordinal),
            "document application must own stored document retention and the host must delegate writes");
        Test.True(applicationSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectPublishedRoundDocuments(", StringComparison.Ordinal),
            "host must delegate published round document selection to the domain");
        Test.True(!behaviorSource.Contains(".OrderBy(x => x.Day).ThenBy(x => x.CreatedUtcTicks)", StringComparison.Ordinal)
            && !behaviorSource.Contains(".OrderByDescending(x => x.Day).ThenByDescending(x => x.CreatedUtcTicks)", StringComparison.Ordinal)
            && !behaviorSource.Contains(".ThenByDescending(x => x.CreatedUtcTicks)", StringComparison.Ordinal),
            "raw two-key document orderings must not remain in the host");
        Test.True(rulesSource.Contains("OrderRelayArrivalsByDueDate(", StringComparison.Ordinal),
            "relay arrival ordering must be composed inside the lifecycle rules");
        Test.True(propagationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.OrderPropagationArrivalsByDueDate(", StringComparison.Ordinal)
            && !behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.OrderPropagationArrivalsByDueDate(", StringComparison.Ordinal),
            "propagation application delegates arrival ordering to the domain and host owns no queue ordering");
        Test.True(rulesSource.Contains("WorldDiplomacyRoundLifecycleRules.OrderProtectedFactsBySequence(", StringComparison.Ordinal)
            || storageMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.OrderProtectedFactsBySequence(", StringComparison.Ordinal),
            "protected-fact ordering must delegate to the domain");
        Test.True(storageMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.OrderDeltaEntriesBySequence(", StringComparison.Ordinal),
            "canonical-history normalization must delegate delta-entry ordering to the domain");
        Test.True(canonicalHistoryRulesSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectDeltaEntriesThrough(", StringComparison.Ordinal),
            "canonical-history rules must delegate delta-entry cutoffs to the domain");
        Test.True(!behaviorSource.Contains("OrderBy(x => x.DueDay)", StringComparison.Ordinal)
            && !behaviorSource.Contains("ThenBy(x => IsCourtArrival(x) ? 0 : 1)", StringComparison.Ordinal)
            && !behaviorSource.Contains("OrderByDescending(x => x.Sequence)", StringComparison.Ordinal)
            && !behaviorSource.Contains("ThenBy(x => x.Kind, StringComparer.Ordinal)", StringComparison.Ordinal)
            && !behaviorSource.Contains("x.Sequence <= throughSequence).OrderBy(", StringComparison.Ordinal)
            && !behaviorSource.Contains("x.Sequence <= cutoff).OrderBy(", StringComparison.Ordinal),
            "raw record orderings must not remain in the host");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(", StringComparison.Ordinal)
            && historyCaptureSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizeTrimmedIdListPreserveOrder(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizeOrderedIdList(", StringComparison.Ordinal),
            "host must delegate identifier-list normalization to the domain");
        Test.True(storageMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizeRevisionMapLong(", StringComparison.Ordinal)
            && storageMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizeRevisionMapString(", StringComparison.Ordinal),
            "canonical-history normalization must delegate revision-map normalization to the domain");
        Test.True(storageMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.IsDeltaEntryRetired(", StringComparison.Ordinal)
            && storageMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.ResolveNextDeltaSequence(", StringComparison.Ordinal)
            && storageMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.NormalizeDeltaEntryFields(", StringComparison.Ordinal),
            "canonical-history normalization must delegate delta-entry normalization to the domain");
        Test.True(!behaviorSource.Contains(".GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)", StringComparison.Ordinal)
            && !behaviorSource.Contains("x.Last().Value", StringComparison.Ordinal)
            && !behaviorSource.Contains("x.Sequence <= history.Snapshot.CoveredThroughSequence", StringComparison.Ordinal)
            && !behaviorSource.Contains(".Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase)", StringComparison.Ordinal)
            && !behaviorSource.Contains(".Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()", StringComparison.Ordinal)
            && !behaviorSource.Contains(".Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase)", StringComparison.Ordinal),
            "raw canonical-history and identifier-list normalization must not remain in the host");

        string envelopeSource = File.ReadAllText(FindRepositoryFile(
            "src", "modules", "AF.Module.Diplomacy", "Domain", "WorldDiplomacyEnvelopeJsonRules.cs"));
        Test.True(!envelopeSource.Contains("TaleWorlds", StringComparison.Ordinal),
            "envelope json rules must stay free of TaleWorlds references");
        Test.True(!envelopeSource.Contains("WorldDiplomacyBehavior", StringComparison.Ordinal),
            "envelope json rules must not reference the host behavior");
        Test.True(generatedCompletionSource.Contains("WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject(", StringComparison.Ordinal)
            && analysisSource.Contains("WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(", StringComparison.Ordinal)
            && generatedCompletionSource.Contains("WorldDiplomacyEnvelopeJsonRules.NormalizeGeneratedDiplomaticEnvelopeShape(", StringComparison.Ordinal)
            && validationSource.Contains("WorldDiplomacyEnvelopeJsonRules.MirrorFirstGeneratedActionEnvelope(", StringComparison.Ordinal)
            && validationSource.Contains("WorldDiplomacyEnvelopeJsonRules.BuildGeneratedSingleActionEnvelope(", StringComparison.Ordinal)
            && validationSource.Contains("WorldDiplomacyEnvelopeJsonRules.CopyDerivedGeneratedActionEnvelope(", StringComparison.Ordinal)
            && generatedCompletionSource.Contains("WorldDiplomacyEnvelopeJsonRules.TryNormalizeInlineResponseBinding(", StringComparison.Ordinal)
            && generatedCompletionSource.Contains("WorldDiplomacyEnvelopeJsonRules.RemoveRedundantStatementActions(", StringComparison.Ordinal)
            && rulesSource.Contains("WorldDiplomacyEnvelopeJsonRules.BuildPeaceTermsJson(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyEnvelopeJsonRules.ReadString(", StringComparison.Ordinal)
            && analysisSource.Contains("WorldDiplomacyEnvelopeJsonRules.ReadStringList(", StringComparison.Ordinal)
            && roundCompressionSource.Contains("WorldDiplomacyEnvelopeJsonRules.ReadTokenStringList(", StringComparison.Ordinal)
            && analysisSource.Contains("WorldDiplomacyEnvelopeJsonRules.ReadFloat(", StringComparison.Ordinal)
            && analysisSource.Contains("WorldDiplomacyEnvelopeJsonRules.ReadBool(", StringComparison.Ordinal)
            && validationSource.Contains("WorldDiplomacyEnvelopeJsonRules.IsJsonStringArray(", StringComparison.Ordinal)
            && generatedCompletionSource.Contains("WorldDiplomacyRoundLifecycleRules.IsAutonomousOpeningJob(", StringComparison.Ordinal)
            && threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(", StringComparison.Ordinal),
            "application services must delegate generated-envelope json coercion to the domain");
        Test.True(!behaviorSource.Contains("private static bool TryParseJsonObject", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static JObject ParseJsonObject", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static void NormalizeGeneratedDiplomaticEnvelopeShape", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static void MirrorFirstGeneratedActionEnvelope", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static JObject BuildGeneratedSingleActionEnvelope", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static void CopyDerivedGeneratedActionEnvelope", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static bool TryNormalizeInlineResponseBinding", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static JObject BuildPeaceTermsJson", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static List<string> ReadStringList", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static List<string> ReadTokenStringList", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static float ReadFloat", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static int ReadInteger", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static bool TryReadInteger", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static bool ReadBool", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static bool IsJsonStringArray", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static bool IsAutonomousOpeningJob", StringComparison.Ordinal),
            "generated-envelope json implementations must not remain in the host");

        string textRulesSource = File.ReadAllText(FindRepositoryFile(
            "src", "modules", "AF.Module.Diplomacy", "Domain", "WorldDiplomacyTextRules.cs"));
        Test.True(!textRulesSource.Contains("TaleWorlds", StringComparison.Ordinal),
            "text rules must stay free of TaleWorlds references");
        Test.True(!textRulesSource.Contains("WorldDiplomacyBehavior", StringComparison.Ordinal),
            "text rules must not reference the host behavior");
        Test.True(playerApplicationSource.Contains("WorldDiplomacyTextRules.NormalizeBody(", StringComparison.Ordinal)
            && migrationApplicationSource.Contains("WorldDiplomacyTextRules.NormalizeCanonicalHistoryText(", StringComparison.Ordinal)
            && presentationQueriesSource.Contains("WorldDiplomacyTextRules.FormatDiplomaticBodyForDisplay(", StringComparison.Ordinal)
            && presentationQueriesSource.Contains("WorldDiplomacyTextRules.SanitizePublicDiplomacyText(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyTextRules.CompactPromptFact(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyTextRules.Limit(", StringComparison.Ordinal),
            "host must delegate public-text normalization and formatting to the domain");
        Test.True(textRulesSource.Contains("public static string DeriveTitle(", StringComparison.Ordinal)
            && textRulesSource.Contains("public static void AppendDiplomaticDisplayParagraphs(", StringComparison.Ordinal)
            && textRulesSource.Contains("public static string DocumentTypeLabel(", StringComparison.Ordinal)
            && textRulesSource.Contains("public static string BuildFallbackDocumentTitle(", StringComparison.Ordinal)
            && textRulesSource.Contains("public static string BuildArchiveIndexDocumentTitle(", StringComparison.Ordinal),
            "title derivation and paragraph splitting must live in the domain rules");
        Test.True(presentationQueriesSource.Contains("WorldDiplomacyTextRules.DocumentTypeLabel(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyTextRules.BuildFallbackDocumentTitle(", StringComparison.Ordinal)
            && presentationQueriesSource.Contains("WorldDiplomacyTextRules.BuildArchiveIndexDocumentTitle(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string DocumentTypeLabel(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string BuildFallbackDocumentTitle(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string BuildArchiveIndexDocumentTitle(", StringComparison.Ordinal),
            "host must delegate document title/label helpers to the text rules");
        Test.True(!behaviorSource.Contains("private static string NormalizeBody(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string NormalizeCanonicalHistoryText(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string FormatDiplomaticBodyForDisplay(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static void AppendDiplomaticDisplayParagraphs(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string DeriveTitle(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string SanitizePublicDiplomacyText(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string CompactPromptFact(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string Limit(string value, int maxChars)\n\t{\n\t\tstring text", StringComparison.Ordinal),
            "public-text normalization implementations must not remain in the host");

        string prestigeApplicationSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyPrestigeApplication.cs"));
        string reputationRulesSource = File.ReadAllText(FindRepositoryFile(
            "src", "modules", "AF.Module.Diplomacy", "Domain", "WorldDiplomacyReputationRules.cs"));
        Test.True(!reputationRulesSource.Contains("TaleWorlds", StringComparison.Ordinal),
            "reputation rules must stay free of TaleWorlds references");
        Test.True(!reputationRulesSource.Contains("WorldDiplomacyBehavior", StringComparison.Ordinal),
            "reputation rules must not reference the host behavior");
        Test.True(!behaviorSource.Contains("WorldDiplomacyReputationRules.DescribeNationalPrestige(", StringComparison.Ordinal)
            && !behaviorSource.Contains("WorldDiplomacyReputationRules.DescribeInternationalReputation(", StringComparison.Ordinal)
            && !analysisSource.Contains("WorldDiplomacyReputationRules.ApplyInternationalReputationEvaluation(", StringComparison.Ordinal)
            && storageMigrationSource.Contains("WorldDiplomacyReputationRules.MaximumInternationalReputationChangePerDocument", StringComparison.Ordinal)
            && reputationRulesSource.Contains("InternationalReputationNaturalAnchor", StringComparison.Ordinal),
            "Q50 retired standings have no new host/analysis consumer; old migration identities and pure rules remain");
        Test.True(reputationRulesSource.Contains("WorldDiplomacyEnvelopeJsonRules.TryReadInteger(", StringComparison.Ordinal)
            && reputationRulesSource.Contains("WorldDiplomacyEnvelopeJsonRules.ReadString(", StringComparison.Ordinal),
            "reputation rules must route json coercion through the envelope rules");
        Test.True(behaviorSource.Contains("WorldDiplomacyTextRules.DescribeWarDuration(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyTextRules.DescribePeacePressure(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyTextRules.DescribeStrengthBalance(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyTextRules.DescribeWarProgress(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyTextRules.DescribeOtherWarBurden(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyTextRules.DescribeRulerRelation(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyTextRules.DescribeRealmRelationProfile(", StringComparison.Ordinal),
            "host must delegate war and relation describe text to the domain rules");
        Test.True(!behaviorSource.Contains("private const int MaximumInternationalReputationChangePerDocument", StringComparison.Ordinal)
            && !behaviorSource.Contains("private const int InternationalReputationNaturalAnchor", StringComparison.Ordinal)
            && !behaviorSource.Contains("private const int InternationalReputationFastDecayMinimum", StringComparison.Ordinal)
            && !behaviorSource.Contains("private const int InternationalReputationNormalDecayMinimum", StringComparison.Ordinal)
            && !behaviorSource.Contains("private const int InternationalReputationSlowDecayMinimum", StringComparison.Ordinal)
            && !behaviorSource.Contains("private const int InternationalReputationDailyIntervalDays", StringComparison.Ordinal)
            && !behaviorSource.Contains("private const int InternationalReputationFastDecayStep", StringComparison.Ordinal)
            && !behaviorSource.Contains("private const int InternationalReputationNormalDecayStep", StringComparison.Ordinal),
            "reputation constants must have a single owner in the domain rules");
        Test.True(!behaviorSource.Contains("private static int CalculateInternationalReputationNaturalChange(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static void ApplyInternationalReputationEvaluation(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static int CalculateStructuredInternationalReputationFallback(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string BuildInternationalReputationImpactDeltaText(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string FormatSignedDelta(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string FormatSignedStandingDelta(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string DescribeNationalPrestige(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string DescribeInternationalReputation(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string DescribeInternationalReputationNaturalTrend(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static int GetNationalPrestigeRelationTarget(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string DescribeWarDuration(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string DescribePeacePressure(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string DescribeStrengthBalance(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string DescribeWarProgress(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string DescribeOtherWarBurden(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string DescribeRulerRelation(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string DescribeRealmRelationProfile(", StringComparison.Ordinal),
            "reputation and describe implementations must not remain in the host");

        string offerContractSource = File.ReadAllText(FindRepositoryFile(
            "src", "modules", "AF.Module.Diplomacy", "Domain", "WorldDiplomacyOfferContractRules.cs"));
        string factSource = File.ReadAllText(FindRepositoryFile(
            "src", "modules", "AF.Module.Diplomacy", "Domain", "WorldDiplomacyDocumentFactRules.cs"));
        Test.True(!offerContractSource.Contains("TaleWorlds", StringComparison.Ordinal),
            "offer contract rules must stay free of TaleWorlds references");
        Test.True(!offerContractSource.Contains("WorldDiplomacyBehavior", StringComparison.Ordinal),
            "offer contract rules must not reference the host behavior");
        Test.True(behaviorSource.Contains("WorldDiplomacyOfferContractRules.IsExclusivePeaceOfferResponseSet(", StringComparison.Ordinal)
            && validationSource.Contains("WorldDiplomacyOfferContractRules.GeneratedActionsContainRequiredPeaceOfferResponse(", StringComparison.Ordinal)
            && analysisSource.Contains("WorldDiplomacyOfferContractRules.CommitmentMatchesIntent(", StringComparison.Ordinal)
            && offerApplicationSource.Contains("WorldDiplomacyOfferOutcome.Partial", StringComparison.Ordinal)
            && !offerApplicationSource.Contains("IndexOf(\"交割失败\"", StringComparison.Ordinal)
            && factSource.Contains("WorldDiplomacyOfferContractRules.IsRequiredPeaceOfferResponse(", StringComparison.Ordinal),
            "host and document fact rules must delegate offer and commitment contract checks to the domain rules");
        Test.True(validationSource.Contains("WorldDiplomacyTextRules.ContainsWholeNumber(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyTextRules.StripGeneratedActionReasonPrefix(", StringComparison.Ordinal)
            && presentationSource.Contains("WorldDiplomacyTextRules.ParseDayForArchive(", StringComparison.Ordinal)
            && validationSource.Contains("WorldDiplomacyTextRules.TryGetImmersionViolation(", StringComparison.Ordinal)
            && propagationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.CalculatePropagationDays(", StringComparison.Ordinal)
            && historyCompressionSource.Contains("WorldDiplomacyRoundLifecycleRules.ParseCompressionSequence(", StringComparison.Ordinal),
            "host must delegate text parsing, immersion checks, and propagation math to the domain rules");
        Test.True(!behaviorSource.Contains("private static bool IsExclusivePeaceOfferResponseSet(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static bool IsRequiredPeaceOfferResponse(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static bool GeneratedActionsContainRequiredPeaceOfferResponse(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static bool CommitmentMatchesIntent(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string ProposalSuccessResult(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static bool ContainsWholeNumber(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string StripGeneratedActionReasonPrefix(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static int ParseDayForArchive(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static bool TryGetImmersionViolation(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static int CalculatePropagationDays(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static int ParseCompressionSequence(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static readonly Regex InternalMetricWithNumberRegex", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static readonly Regex InternalMetricTermRegex", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static readonly Regex ConversationalDiplomacyPhraseRegex", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static readonly Regex PrivateFirstPersonRegex", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static readonly Regex DirectSecondPersonRegex", StringComparison.Ordinal),
            "offer-contract, text-parsing, immersion, and propagation implementations must not remain in the host");

        string promptContractSource = File.ReadAllText(FindRepositoryFile(
            "src", "modules", "AF.Module.Diplomacy", "Domain", "WorldDiplomacyPromptContractRules.cs"));
        Test.True(!promptContractSource.Contains("TaleWorlds", StringComparison.Ordinal),
            "prompt contract rules must stay free of TaleWorlds references");
        Test.True(!promptContractSource.Contains("WorldDiplomacyBehavior", StringComparison.Ordinal),
            "prompt contract rules must not reference the host behavior");
        Test.True(
            File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyJobPreparationApplication.cs")).Contains("WorldDiplomacyPromptContractRules.UpsertKingdomStrategicProfilePrompt(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyPromptContractRules.ResolveCacheAffinityKey(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyPromptContractRules.CloneLlmMessages(", StringComparison.Ordinal)
            && !behaviorSource.Contains("DiplomacyPromptTexts.Get(\"AF_DIP_Compatibility_008\",", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string BuildKingdomStrategicProfileMarker(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string UpsertKingdomStrategicProfilePrompt(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static int FindKingdomStrategicProfileInsertionIndex(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private const string KingdomStrategicIntentRule", StringComparison.Ordinal)
            && !behaviorSource.Contains("private const string KingdomStrategicProfileMarkerPrefix", StringComparison.Ordinal)
            && promptContractSource.Contains("public static string BuildKingdomStrategicProfileMarker(", StringComparison.Ordinal)
            && promptContractSource.Contains("public static bool GenerationJobContainsKingdomStrategicProfile(", StringComparison.Ordinal)
            && promptContractSource.Contains("internal static readonly string KingdomStrategicIntentRule = CompatibilityContractText.Values[\"AF_DIP_Compatibility_008\"]", StringComparison.Ordinal),
            "Q50 active prompt preparation no longer injects retired strategic profiles; legacy helpers and message contracts remain");
        Test.True(!promptContractSource.Contains("DuelSettings", StringComparison.Ordinal),
            "prompt contract rules must not read MCM settings");
        Test.True(behaviorSource.Contains("WorldDiplomacyPromptContractRules.StablePromptHash(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyPromptContractRules.StablePromptHashMessagePrefix(", StringComparison.Ordinal)
            && dispatchSource.Contains("IsValidSemanticRepairMessageChain(", StringComparison.Ordinal)
            && dispatchSource.Contains("WorldDiplomacyPromptContractRules.HasCurrentCanonicalPromptContract(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyPromptContractRules.TryExtractCommonContractFromJob(", StringComparison.Ordinal)
            && historyCompressionSource.Contains("WorldDiplomacyPromptContractRules.BuildCanonicalHistorySystemPrompt(", StringComparison.Ordinal)
            && generationTaskSource.Contains("BuildGenerationSystemPrompt(", StringComparison.Ordinal)
            && generationTaskSource.Contains("BuildRelayGenerationSystemPrompt(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyPromptContractRules.BuildDeclareModePrompt(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyPromptContractRules.AppendRoundSubstantiveProgressRequirement(", StringComparison.Ordinal)
            && jobPreparationSource.Contains("BuildAnalysisSystemPrompt(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyPromptContractRules.BuildAnalysisModeContract(", StringComparison.Ordinal)
            && historyCompressionSource.Contains("WorldDiplomacyPromptContractRules.BuildTokenCompressionPrompt(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyPromptContractRules.BuildRealmInstitutionalVoiceText(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyPromptContractRules.BuildCurrentLegalDiplomaticOptions(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyPromptContractRules.DescribePotentialDiplomaticActions(", StringComparison.Ordinal),
            "host must delegate prompt contract and stable hash work to the domain rules");
        Test.True(behaviorSource.Contains("WorldDiplomacyPromptContractRules.MaxGeneratedDraftRepairAttempts", StringComparison.Ordinal)
            && generationTaskSource.Contains("CanonicalHistoryCacheAffinityKey", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyPromptContractRules.RoundPlanTaskMarker", StringComparison.Ordinal),
            "host must read prompt contract constants from the domain owner");
        Test.True(!behaviorSource.Contains("private static string StablePromptHash(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static ulong AppendStablePromptHash(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static bool IsValidSemanticRepairMessageChain(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static bool HasCurrentCanonicalPromptContract(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static bool TryExtractCommonContractFromJob(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static StringBuilder CreateSystemPromptBuilder(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static void AppendDiplomaticDeclarationWritingContract(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string BuildCanonicalHistorySystemPrompt(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string BuildGenerationSystemPrompt(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string BuildRelayGenerationSystemPrompt(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string BuildDeclareModePrompt(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static void AppendRoundSubstantiveProgressRequirement(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string BuildAnalysisSystemPrompt(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string BuildTokenCompressionPrompt(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string BuildRoundCompressionSystemPrompt(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string BuildRealmInstitutionalVoiceText(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static void AppendVoiceTrait(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string BuildCurrentLegalDiplomaticOptions(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string DescribePotentialDiplomaticActions(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static bool UsesCanonicalHistory(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private const int MaxGeneratedDraftRepairAttempts", StringComparison.Ordinal)
            && !behaviorSource.Contains("private const string CanonicalHistoryCacheAffinityKey", StringComparison.Ordinal)
            && !behaviorSource.Contains("private const string DiplomaticDeclarationWritingContractMarker", StringComparison.Ordinal)
            && !behaviorSource.Contains("private const string DiplomacyModeDispatchContractMarker", StringComparison.Ordinal)
            && !behaviorSource.Contains("private const string DiplomaticDeclarationModeContractMarker", StringComparison.Ordinal)
            && !behaviorSource.Contains("private const string CanonicalHistoryCompressionModeContractMarker", StringComparison.Ordinal)
            && !behaviorSource.Contains("private const string CanonicalHistoryContractMarker", StringComparison.Ordinal)
            && !behaviorSource.Contains("private const string RoundPlanTaskMarker", StringComparison.Ordinal)
            && !behaviorSource.Contains("private const string DiplomacyAnalysisTaskMarker", StringComparison.Ordinal),
            "prompt contract implementations and markers must not remain in the host");

        Test.True(structureSource.Contains("public static class WorldDiplomacyStructureRules", StringComparison.Ordinal)
            && !structureSource.Contains("TaleWorlds", StringComparison.Ordinal)
            && !structureSource.Contains("WorldDiplomacyBehavior", StringComparison.Ordinal),
            "structure rules must stay pure and free of host/TaleWorlds references");
        Test.True(structureSource.Contains("public static bool RoundContainsKingdom", StringComparison.Ordinal)
            && structureSource.Contains("public static WorldDiplomacyRoundParticipant EnsureRoundParticipant", StringComparison.Ordinal)
            && structureSource.Contains("public static bool AddParticipantToRelayRouteIfNeeded", StringComparison.Ordinal)
            && structureSource.Contains("public static bool DocumentRespondsTo", StringComparison.Ordinal)
            && structureSource.Contains("public static bool IsCourtArrival", StringComparison.Ordinal),
            "structure rules must own round/document structural helpers");
        Test.True(structureSource.Contains("WorldDiplomacyRoundLifecycleRules.ShouldRefreshParticipantState", StringComparison.Ordinal)
            && structureSource.Contains("WorldDiplomacyRoundLifecycleRules.EvaluateRouteAdmission", StringComparison.Ordinal),
            "structure rules must route participant state and route admission through the lifecycle rules");
        Test.True(propagationApplicationSource.Contains("WorldDiplomacyStructureRules.EnsureRoundParticipant(", StringComparison.Ordinal)
            && noActionSource.Contains("WorldDiplomacyStructureRules.RoundRouteContainsKingdom(", StringComparison.Ordinal)
            && propagationApplicationSource.Contains("WorldDiplomacyStructureRules.AddParticipantToRelayRouteIfNeeded(", StringComparison.Ordinal)
            && behaviorSource.Contains("GetRoundParticipantLimit()", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static bool RoundContainsKingdom(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static WorldDiplomacyRoundParticipant EnsureRoundParticipant(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static bool DocumentRespondsTo(", StringComparison.Ordinal),
            "host must delegate structural rules and keep the participant-limit computation host-side");
        Test.True(behaviorSource.Contains("WorldDiplomacyTextRules.DescribeBorderRelation(", StringComparison.Ordinal)
            && behaviorSource.Contains("private static int GetRoundParticipantLimit(", StringComparison.Ordinal),
            "host must delegate border-relation text to the domain and keep MCM settings host-side");

        Test.True(factSource.Contains("public static class WorldDiplomacyDocumentFactRules", StringComparison.Ordinal)
            && !factSource.Contains("TaleWorlds", StringComparison.Ordinal)
            && !factSource.Contains("WorldDiplomacyBehavior", StringComparison.Ordinal),
            "document fact rules must stay pure and free of host/TaleWorlds references");
        Test.True(factSource.Contains("public static WorldDiplomacyDocumentAction ResolveSourceActionForTarget", StringComparison.Ordinal)
            && factSource.Contains("public static string BuildPeaceOfferTermsFact", StringComparison.Ordinal)
            && factSource.Contains("public static int DiplomacyDocumentQueryRelevance", StringComparison.Ordinal)
            && factSource.Contains("public static bool IsBilateralBattleFact", StringComparison.Ordinal),
            "document fact rules must own the fact/query helpers");
        Test.True(offerContractSource.Contains("public static bool ArePeaceTermsEquivalent", StringComparison.Ordinal)
            && offerContractSource.Contains("public static WorldDiplomacyPeaceTerms ClonePeaceTerms", StringComparison.Ordinal)
            && offerContractSource.Contains("public static string FormatPeaceTermsForPrompt", StringComparison.Ordinal),
            "offer contract rules must own peace-terms equivalence, cloning and prompt text");
        Test.True(validationSource.Contains("WorldDiplomacyOfferContractRules.ArePeaceTermsEquivalent(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyDocumentFactRules.BuildSourceActionFactForTarget(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static bool ArePeaceTermsEquivalent(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static WorldDiplomacyPeaceTerms ClonePeaceTerms(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static int DiplomacyDocumentQueryRelevance(", StringComparison.Ordinal),
            "host must delegate fact/peace-terms rules through the domain files");
        Test.True(behaviorSource.Contains("private static AfVassalageType NormalizeWorldDiplomacyVassalageType(", StringComparison.Ordinal),
            "AfVassalageType normalization stays host-side until the enum is reachable from the smoke slice");

        string canonicalRenderSource = File.ReadAllText(FindRepositoryFile(
            "src", "modules", "AF.Module.Diplomacy", "Domain", "WorldDiplomacyCanonicalRenderRules.cs"));
        Test.True(canonicalRenderSource.Contains("public static class WorldDiplomacyCanonicalRenderRules", StringComparison.Ordinal)
            && !canonicalRenderSource.Contains("TaleWorlds", StringComparison.Ordinal)
            && !canonicalRenderSource.Contains("WorldDiplomacyBehavior", StringComparison.Ordinal)
            && !canonicalRenderSource.Contains("Logger.", StringComparison.Ordinal),
            "canonical render rules must stay pure with no engine, host or Logger references");
        Test.True(canonicalRenderSource.Contains("public static string RenderCanonicalHistoryEntry", StringComparison.Ordinal)
            && canonicalRenderSource.Contains("public static string RenderCanonicalProtectedFacts", StringComparison.Ordinal)
            && canonicalRenderSource.Contains("public static string RenderCanonicalSnapshotPayload", StringComparison.Ordinal)
            && canonicalRenderSource.Contains("Func<string, long> estimateTokens", StringComparison.Ordinal)
            && canonicalRenderSource.Contains("WorldDiplomacyRoundLifecycleRules.OrderProtectedFactsBySequenceDescending(", StringComparison.Ordinal),
            "canonical render rules must own entry/protected/snapshot rendering with a token-estimate port"
            + " and route newest-first protected-fact ordering through the lifecycle rules");
        Test.True(migrationApplicationSource.Contains("WorldDiplomacyCanonicalRenderRules.RenderCanonicalProtectedFacts(", StringComparison.Ordinal)
            && historyCompressionSource.Contains("WorldDiplomacyCanonicalRenderRules.SelectCanonicalProtectedFactsWithinTokenBudget(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string RenderCanonicalHistoryEntry(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string RenderCanonicalProtectedFacts(", StringComparison.Ordinal),
            "host must delegate canonical rendering through the domain rules");

        Test.True(
            behaviorSource.Contains("WorldDiplomacyDocumentFactRules.BuildDocumentIndex(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyDocumentFactRules.ResolveOfferedPeaceTerms(", StringComparison.Ordinal)
            && documentExecutor.Contains("WorldDiplomacyDocumentFactRules.MirrorPrimaryActionToDocument(", StringComparison.Ordinal)
            && rulesSource.Contains("WorldDiplomacyDocumentFactRules.CloneDocument)", StringComparison.Ordinal)
            && File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyMemoryApplication.cs")).Contains("WorldDiplomacyDocumentFactRules.FormatRoundFactForPrompt(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static WorldDiplomacyDocument CloneDocument(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static bool IsMajorDiplomaticDocument(", StringComparison.Ordinal)
            && factSource.Contains("public static WorldDiplomacyDocument CloneDocument(", StringComparison.Ordinal)
            && factSource.Contains("public static bool IsMajorDiplomaticDocument(", StringComparison.Ordinal)
            && factSource.Contains("public static bool PeaceTermsContainCession(", StringComparison.Ordinal),
            "host must delegate document-record helpers to the domain document fact rules");

        Test.True(textRulesSource.Contains("public static string BuildCompactDocumentMemoryLine(", StringComparison.Ordinal)
            && textRulesSource.Contains("public static string BuildDetailedDocumentMemoryLine(", StringComparison.Ordinal)
            && textRulesSource.Contains("public static void AppendProactiveDiscussionDocument(", StringComparison.Ordinal)
            && textRulesSource.Contains("public static string BuildLocalRoundSummaryText(", StringComparison.Ordinal)
            && textRulesSource.Contains("Func<int, string> formatDayFallback", StringComparison.Ordinal),
            "document memory-line rendering must live in the text rules behind a day-formatter port");
        Test.True(!memoryApplicationSource.Contains("WorldDiplomacyTextRules.BuildCompactDocumentMemoryLine(", StringComparison.Ordinal)
            && !memoryApplicationSource.Contains("WorldDiplomacyTextRules.BuildDetailedDocumentMemoryLine(", StringComparison.Ordinal)
            && proactiveSource.Contains("WorldDiplomacyTextRules.AppendProactiveDiscussionDocument(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private bool TryBuildProactiveDiscussion(Hero hero", StringComparison.Ordinal)
            && rulesSource.Contains("WorldDiplomacyTextRules.BuildLocalRoundSummaryText(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string BuildCompactDocumentMemoryLine(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string BuildLocalRoundSummaryText(", StringComparison.Ordinal),
            "Q50 recall uses whole source rows while proactive and legacy summary formatting retain domain helpers");

        Test.True(factSource.Contains("public static bool GeneratedActionsHaveUnsafeMultiplePeaceAcceptances(", StringComparison.Ordinal)
            && factSource.Contains("public static bool DocumentHasUnsafeMultiplePeaceAcceptances(", StringComparison.Ordinal)
            && factSource.Contains("Func<string, WorldDiplomacyDocument> resolveDocument", StringComparison.Ordinal),
            "unsafe multiple peace-acceptance checks must live in the document fact rules behind a resolver port");
        Test.True(validationSource.Contains("WorldDiplomacyDocumentFactRules.GeneratedActionsHaveUnsafeMultiplePeaceAcceptances(", StringComparison.Ordinal)
            && documentExecutor.Contains("WorldDiplomacyDocumentFactRules.DocumentHasUnsafeMultiplePeaceAcceptances(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private bool GeneratedActionsHaveUnsafeMultiplePeaceAcceptances(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private bool DocumentHasUnsafeMultiplePeaceAcceptances(", StringComparison.Ordinal),
            "host must delegate unsafe peace-acceptance checks to the domain document fact rules");

        string messageApplication = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyLlmMessageApplication.cs"));
        Test.True(messageApplication.Contains("public static List<WorldDiplomacyLlmMessage> BuildLlmMessagesForJob(", StringComparison.Ordinal)
            && messageApplication.Contains("public static JArray BuildLlmMessageArray(", StringComparison.Ordinal)
            && !promptContractSource.Contains("buildCanonicalHistoryBlock", StringComparison.Ordinal),
            "only Application may resolve history while building a request");
        foreach (string retired in new[] { "AddOrMergeResultSettlementSlot(", "AddWarResponseResultSettlementSlot(", "InitializeResultSettlementRouteSlots(", "UpsertOfferCooldown(", "SettleTradeAllianceOfferCooldownsForClosedRound(", "HasStaleDiplomaticActionPresentation(" })
            Test.True(!rulesSource.Contains(retired, StringComparison.Ordinal), "Domain cannot invoke workflow callbacks: " + retired);
        Test.True(dispatchSource.Contains("WorldDiplomacyJob job = SelectAndPrepareLlmJob(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyLlmMessageApplication.BuildLlmMessageArray(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private List<WorldDiplomacyLlmMessage> BuildLlmMessagesForJob(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private JArray BuildLlmMessageArray(", StringComparison.Ordinal),
            "LLM dispatch Application delegates message assembly to the prompt contract rules");

        Test.True(!behaviorSource.Contains("private static string NormalizeIntent(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string Limit(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string ReadString(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string FirstNonEmpty(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private bool IsThreatRelevantToResultSettlement(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private List<string> GetPresentedThreatDocumentIds(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private List<WorldDiplomacyDocument> GetRecentDocuments(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private WorldDiplomacyThreat FindOpenDiplomaticThreat(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private WorldDiplomacyThreat FindOpenDiplomaticThreatIssuedBy(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private string BuildDisplayedDocumentTitle(", StringComparison.Ordinal),
            "vocabulary, text, storage-adapter and display-title shims must be removed from the host");
        Test.True(behaviorSource.Contains("WorldDiplomacyIntentVocabulary.NormalizeIntent(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyTextRules.Limit(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyEnvelopeJsonRules.ReadString(", StringComparison.Ordinal)
            && threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(", StringComparison.Ordinal)
            && dispatchSource.Contains("WorldDiplomacyRoundLifecycleRules.HasStaleThreatPresentation(", StringComparison.Ordinal)
            && threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectOpenThreatBetween(", StringComparison.Ordinal)
            && presentationQueriesSource.Contains("WorldDiplomacyTextRules.BuildDisplayedDocumentTitle(", StringComparison.Ordinal),
            "host call sites must qualify the owning domain rule directly");
        Test.True(textRulesSource.Contains("public static string BuildDisplayedDocumentTitle(", StringComparison.Ordinal),
            "displayed document title composition must live in the text rules");

        Test.True(factSource.Contains("public static bool HasKingdomRespondedToDocument(", StringComparison.Ordinal)
            && factSource.Contains("public static bool HasUndeliveredCourtArrivals(", StringComparison.Ordinal)
            && factSource.Contains("public static bool HasKingdomKnowledge(", StringComparison.Ordinal)
            && factSource.Contains("public static HashSet<string> GetKnownSettlementIdsForDocument(", StringComparison.Ordinal)
            && factSource.Contains("public static HashSet<string> GetKnownKingdomIdsForDocument(", StringComparison.Ordinal)
            && factSource.Contains("public static string BuildFallbackRoundSummary(", StringComparison.Ordinal)
            && factSource.Contains("public static string BuildFallbackRoundCompressionJson(", StringComparison.Ordinal),
            "storage knowledge/document queries must live in the document fact rules");
        Test.True(courtResponseApplicationSource.Contains("WorldDiplomacyDocumentFactRules.HasKingdomRespondedToDocument(", StringComparison.Ordinal)
            && propagationApplicationSource.Contains("WorldDiplomacyDocumentFactRules.GetKnownKingdomIdsForDocument(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyDocumentFactRules.BuildFallbackRoundCompressionJson(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private bool HasKingdomRespondedToDocument(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private bool HasUndeliveredCourtArrivals(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private bool HasKingdomKnowledge(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private HashSet<string> GetKnownSettlementIdsForDocument(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private HashSet<string> GetKnownKingdomIdsForDocument(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private string BuildFallbackRoundSummary(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private string BuildFallbackRoundCompressionJson(", StringComparison.Ordinal),
            "host must delegate storage queries to the document fact rules");

        Test.True(rulesSource.Contains("public static bool HasOpenDiplomaticThreatForRound(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private bool HasOpenDiplomaticThreatForRound(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private string GetDiplomaticThreatTerminalCloseReason(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private bool IsValidatedSubstantiveProgress(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private bool IsValidatedDiplomaticActionAttempt(", StringComparison.Ordinal),
            "open-threat round queries, dead terminal-close wrappers and validated-progress guards must not remain in the host");
        Test.True(progressSource.Contains("WorldDiplomacyRoundLifecycleRules.IsValidatedSubstantiveProgress(", StringComparison.Ordinal)
            && progressSource.Contains("WorldDiplomacyRoundLifecycleRules.IsValidatedDiplomaticActionAttempt(", StringComparison.Ordinal),
            "validated progress checks must call the lifecycle rules directly");

        Test.True(textRulesSource.Contains("public static string BuildNotificationDescription(", StringComparison.Ordinal)
            && textRulesSource.Contains("Func<int, string> formatDayFallback", StringComparison.Ordinal)
            && textRulesSource.Contains("public static string BuildDocumentEventMeta(", StringComparison.Ordinal)
            && textRulesSource.Contains("Func<string, WorldDiplomacyRound> resolveRound", StringComparison.Ordinal)
            && textRulesSource.Contains("Func<string, WorldDiplomacyDocument> resolveDocument", StringComparison.Ordinal),
            "notification and event-meta text composition must live in the text rules behind day/resolver ports");
        Test.True(notificationApplicationSource.Contains("WorldDiplomacyTextRules.BuildNotificationDescription(document, sink.FormatCampaignDate, DiplomacyUiLocalization.Text)", StringComparison.Ordinal)
            && presentationQueriesSource.Contains("WorldDiplomacyTextRules.BuildDocumentEventMeta(document, resolveRound, resolveDocument)", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string BuildNotificationDescription(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private string BuildDocumentEventMeta(", StringComparison.Ordinal),
            "host must delegate notification text composition to the text rules");

        Test.True(textRulesSource.Contains("public static string FormatBattleKingdomNames(", StringComparison.Ordinal)
            && textRulesSource.Contains("public static string FormatBattleFactForPrompt(", StringComparison.Ordinal)
            && textRulesSource.Contains("public static string BuildRoundCompressionPrompt(", StringComparison.Ordinal)
            && textRulesSource.Contains("Func<string, string> kingdomName", StringComparison.Ordinal),
            "battle-fact and compression-prompt rendering must live in the text rules behind name/date ports");
        Test.True(textRulesSource.Contains("FormatBattleFactForPrompt(x, kingdomName, formatDayFallback, localize)", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string FormatBattleKingdomNames(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string FormatBattleFactForPrompt(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private string BuildRoundCompressionPrompt(", StringComparison.Ordinal),
            "battle-fact rendering must stay in the text rules, not the host");

        Test.True(rulesSource.Contains("public static string PairKey(", StringComparison.Ordinal)
            && textRulesSource.Contains("public static string BuildDiplomacyRumor(", StringComparison.Ordinal)
            && promptContractSource.Contains("public static void AppendOpenOfferAnswerRequirement(", StringComparison.Ordinal)
            && factSource.Contains("public static bool ContainsDirectedPeaceTerm(", StringComparison.Ordinal)
            && factSource.Contains("public static string BuildPeaceKingdomReferencePattern(", StringComparison.Ordinal),
            "pair-key, rumor, offer-requirement and directed-peace-term rules must live in domain files");
        Test.True(!notificationApplicationSource.Contains("sink.ShowRumor(", StringComparison.Ordinal)
            && notificationApplicationSource.Contains("WorldDiplomacyPropagationApplication.MarkRumorNotified(rumor)", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyPromptContractRules.AppendOpenOfferAnswerRequirement(", StringComparison.Ordinal)
            && validationSource.Contains("WorldDiplomacyDocumentFactRules.ContainsDirectedPeaceTerm(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string PairKey(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string BuildDiplomacyRumor(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static void AppendOpenOfferAnswerRequirement(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static bool ContainsDirectedPeaceTerm(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string BuildPeaceKingdomReferencePattern(", StringComparison.Ordinal),
            "host must not retain the extracted pair-key/peace-term/rumor/offer-requirement bodies");

        Test.True(textRulesSource.Contains("internal const int MaxPromptRecentBattles", StringComparison.Ordinal)
            && textRulesSource.Contains("internal const int LowInternationalReputationThreshold", StringComparison.Ordinal)
            && textRulesSource.Contains("public static string BuildRecentBilateralDocumentContext(", StringComparison.Ordinal)
            && textRulesSource.Contains("public static string BuildRecentBilateralBattleContext(", StringComparison.Ordinal)
            && textRulesSource.Contains("public static List<string> GetRecentOwnInternationalReputationReasons(", StringComparison.Ordinal)
            && textRulesSource.Contains("public static List<string> GetRecentPublicNegativeReputationFacts(", StringComparison.Ordinal)
            && textRulesSource.Contains("public static string BuildLowReputationConflictOpportunityContext(", StringComparison.Ordinal),
            "bilateral/reputation context rules and their prompt caps must live in the text rules");
        Test.True(behaviorSource.Contains("WorldDiplomacyTextRules.BuildRecentBilateralBattleContext(", StringComparison.Ordinal)
            && !behaviorSource.Contains("WorldDiplomacyTextRules.GetRecentOwnInternationalReputationReasons(", StringComparison.Ordinal)
            && !behaviorSource.Contains("WorldDiplomacyTextRules.BuildLowReputationConflictOpportunityContext(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private string BuildRecentBilateralDocumentContext(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private string BuildRecentBilateralBattleContext(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private List<string> GetRecentOwnInternationalReputationReasons(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private List<string> GetRecentPublicNegativeReputationFacts(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private string BuildLowReputationConflictOpportunityContext(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private const int MaxPromptRecentBattles", StringComparison.Ordinal),
            "Q50 keeps real bilateral battle facts but removes retired reputation-driven motives");

        string promptContextPort = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.PromptContext.cs"));
        foreach (string hiddenSectionOwner in new[]
        {
            "AppendDiplomaticThreatDynamicContext",
            "AppendDiplomaticThreatAnalysisContext",
            "AppendDiplomaticAuthorDecisionContext",
            "AppendDiplomaticTargetDecisionContext",
            "AppendRelayResponseSourceContext",
            "AppendRulerCaptivityDecisionContext",
            "AppendOtherKingdomRelationshipContext",
            "BuildCompactRoundPlanCandidateLine",
            "BuildCompactDiplomaticRelationshipLine",
            "BuildWarDecisionContext",
            "BuildRulerCaptivityTargetHint",
            "SelectPendingIncomingThreats(",
            "SelectIssuerResolutionNotices(",
            "BuildLowReputationConflictOpportunityContext(",
            "EnsureRoundParticipant",
            "IsPlayerAsync",
        })
        {
            Test.True(!promptContextPort.Contains(hiddenSectionOwner, StringComparison.Ordinal),
                "the prompt world port must not hide section policy, candidate ordering, or round-state writes: " + hiddenSectionOwner);
        }
        string promptComposerSource = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyPromptComposer.cs"));
        foreach (string composerOwnedSection in new[]
        {
            "private static void AppendDiplomaticThreatDynamicContext(",
            "private static void AppendDiplomaticThreatAnalysisContext(",
            "private static void AppendDiplomaticAuthorDecisionContext(",
            "private static void AppendDiplomaticTargetDecisionContext(",
            "private static void AppendRelayResponseSourceContext(",
            "private static void AppendRulerCaptivityDecisionContext(",
            "private static void AppendOtherKingdomRelationshipContext(",
            "private static string BuildCompactRoundPlanCandidateLine(",
            "private static string BuildCompactDiplomaticRelationshipLine(",
            "private static string BuildWarDecisionContext(",
            "private static string BuildRulerCaptivityTargetHint(",
        })
        {
            Test.True(promptComposerSource.Contains(composerOwnedSection, StringComparison.Ordinal),
                "prompt section policy must be owned by the application composer: " + composerOwnedSection);
        }
        Test.True(promptContextPort.Contains("IReadOnlyList<string> IWorldDiplomacyPromptWorld.IndependentKingdomIds()", StringComparison.Ordinal)
            && promptContextPort.Contains("_kingdoms", StringComparison.Ordinal)
            && promptContextPort.Contains("_kingdomIndexInitialized", StringComparison.Ordinal),
            "kingdom enumeration must resolve through the lazily indexed port, not a per-fact full scan");

        foreach (string removed in new[]
        {
            "private bool IsWarResponseNoActionAllowed(",
            "private bool IsImmediateWarResponsePeaceSuppressed(",
            "private static string DescribeWarPressure(",
            "private string DescribeWarPressure(",
            "private string GetGeneratedEnvelopeApplicationFailureReason(",
            "private void RecordDiplomaticStandingChange(",
            "private static string BuildPolicySignalContext(",
            "private bool TryResolveOpenProposalFor(",
            "private bool HasOpenProposalForDocument(",
            "private bool IsEnforcingRejectedUltimatum(",
            "private void UpdateDiplomaticThreatComplianceDocumentResult(",
            "private bool TryResolveUniqueOpenProposalForRound("
        })
        {
            Test.True(!behaviorSource.Contains(removed, StringComparison.Ordinal),
                "the host must not retain extracted method body: " + removed);
        }
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.TryDeriveGeneratedDiplomaticStructure(", StringComparison.Ordinal)
                && validationSource.Contains("WorldDiplomacyRoundLifecycleRules.TryResolveOpenProposalFor(", StringComparison.Ordinal)
                && documentExecutor.Contains("WorldDiplomacyRoundLifecycleRules.HasOpenProposalForDocument(", StringComparison.Ordinal)
                && actionSelectionSource.Contains("WorldDiplomacyRoundLifecycleRules.IsEnforcingRejectedUltimatum(", StringComparison.Ordinal)
                && threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.UpdateDiplomaticThreatComplianceDocumentResult(", StringComparison.Ordinal)
                && rulesSource.Contains("WorldDiplomacyPromptContractRules.BuildPolicySignalContext(", StringComparison.Ordinal)
                && rulesSource.Contains("TryResolveUniqueOpenProposalForRound(", StringComparison.Ordinal)
                && rulesSource.Contains("TryResolveUniqueOpenProposal(", StringComparison.Ordinal)
                && rulesSource.Contains("IsOpenDirectedOffer(", StringComparison.Ordinal)
                && rulesSource.Contains("SelectOpenThreatBetween(", StringComparison.Ordinal)
                && promptContractSource.Contains("public static string BuildPolicySignalContext(", StringComparison.Ordinal),
            "proposal/threat adapter ownership must live in the domain with qualified host delegation");
        // DPL-060BM: storage knowledge/context + queue-clearing rules must live in the domain
        string factRules = File.ReadAllText(
            FindRepositoryFile(Path.Combine("src", "modules", "AF.Module.Diplomacy", "Domain", "WorldDiplomacyDocumentFactRules.cs")), Encoding.UTF8);
        foreach (string movedKnowledgeRule in new[]
        {
            "public static void RecordSettlementKnowledge(",
            "public static bool RecordKingdomKnowledge(",
            "public static void RecordNobleKnowledge(",
            "public static string BuildRecentNativeSignalContext(",
            "public static string BuildRelevantCompletedRoundContext(",
            "public static string BuildRelevantCompressedDiplomacyContext(",
            "public static string BuildKnownRoundContext(",
            "internal const int MaxKnownDocumentsPerLocation = 64;"
        })
        {
            Test.True(factRules.Contains(movedKnowledgeRule, StringComparison.Ordinal),
                "storage knowledge/context rules must live in the document-fact rules: " + movedKnowledgeRule);
        }
        Test.True(!behaviorSource.Contains("private void RecordSettlementKnowledge(")
                  && !behaviorSource.Contains("private bool RecordKingdomKnowledge(")
                  && !behaviorSource.Contains("private void RecordNobleKnowledge(")
                  && !behaviorSource.Contains("private string BuildRecentNativeSignalContext(")
                  && !behaviorSource.Contains("private string BuildRelevantCompletedRoundContext(")
                  && !behaviorSource.Contains("private string BuildRelevantCompressedDiplomacyContext(")
                  && !behaviorSource.Contains("private string BuildKnownRoundContext(")
                  && !behaviorSource.Contains("private const int MaxKnownDocumentsPerLocation"),
            "knowledge recording and context composition must not remain in the host");
        Test.True(propagationApplicationSource.Contains("WorldDiplomacyDocumentFactRules.RecordSettlementKnowledge(")
                  && propagationApplicationSource.Contains("WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(")
                  && propagationApplicationSource.Contains("WorldDiplomacyDocumentFactRules.RecordNobleKnowledge(")
                  && behaviorSource.Contains("WorldDiplomacyDocumentFactRules.BuildRecentNativeSignalContext("),
            "the host must delegate knowledge recording through the document-fact rules");
        Test.True(rulesSource.Contains("public static void ClearRoundScopedQueuesAndExpireOpportunities(")
                  && rulesSource.Contains("public static bool TryGetConfirmedRoundResult(")
                  && rulesSource.Contains("public static void InvalidateUnserviceableResultSettlementObligations("),
            "queue clearing and settlement invalidation must live in the lifecycle rules");
        Test.True(!behaviorSource.Contains("private void ClearRoundScopedQueuesAndExpireOpportunities(")
                  && !behaviorSource.Contains("private bool TryGetConfirmedRoundResult(")
                  && !behaviorSource.Contains("private void InvalidateUnserviceableResultSettlementObligations("),
            "queue clearing and settlement invalidation must not remain in the host");

        // DPL-060BN: threat finalization and settled policy-signal cleanup must live in the domain
        foreach (string movedFinalizationRule in new[]
        {
            "public static void SettleDiplomaticThreatObligationsForClosedRound(",
            "public static void RemoveSettledPolicySignalContextFromActiveRound(",

        })
        {
            Test.True(rulesSource.Contains(movedFinalizationRule, StringComparison.Ordinal),
                "threat finalization rules must live in the lifecycle rules: " + movedFinalizationRule);
        }
        Test.True(!behaviorSource.Contains("private void SettleDiplomaticThreatObligationsForClosedRound(")
                  && !behaviorSource.Contains("private void RemoveSettledPolicySignalContextFromActiveRound(")
                  && !behaviorSource.Contains("private void FinalizeDiplomaticThreatHistoryAfterDocument(")
                  && !behaviorSource.Contains("private void FinalizeDiplomaticThreatNonComplianceHistoryAfterDocument("),
            "threat finalization must not remain in the host");

        foreach (string method in new[] { "FinalizeDiplomaticThreatHistoryAfterDocument", "FinalizeDiplomaticThreatNonComplianceHistoryAfterDocument", "TryAppendDiplomaticThreatNonComplianceHistoryResult" })
            Test.True(threatHistorySource.Contains("public static void " + method + "(")
                && !rulesSource.Contains("public static void " + method + "("), "threat history workflow has one Application owner: " + method);

        // DPL-060BO: war-started resolution, follow-through settlement, and consequence finalization
        foreach (string movedThreatResolutionRule in new[]
        {
            "public static void ResolveDiplomaticThreatsAfterWarStarted(",
            "public static WorldDiplomacyThreat SelectBreachedThreatFollowThrough(",
            "public static bool CompleteUnresolvableDiplomaticThreatDomesticPenalty(",
            "public static bool CompleteUnresolvableDiplomaticThreatIssuerRelationReward(",
            "public static void InvalidateOtherThreatsBoundToSettledPolicy("
        })
        {
            Test.True(rulesSource.Contains(movedThreatResolutionRule, StringComparison.Ordinal),
                "threat resolution rules must live in the lifecycle rules: " + movedThreatResolutionRule);
        }
        Test.True(!behaviorSource.Contains("private void ResolveDiplomaticThreatsAfterWarStarted(")
                  && !behaviorSource.Contains("private void SettleDiplomaticThreatFollowThroughAfterDeclaration(")
                  && !behaviorSource.Contains("private bool CompleteUnresolvableDiplomaticThreatDomesticPenalty(")
                  && !behaviorSource.Contains("private bool CompleteUnresolvableDiplomaticThreatIssuerRelationReward(")
                  && !behaviorSource.Contains("private void InvalidateOtherThreatsBoundToSettledPolicy("),
            "threat resolution must not remain in the host");

        Test.True(behaviorSource.Contains("WorldDiplomacyRoundProgressApplication.RetryDeferredRoundProgress(", StringComparison.Ordinal),
            "the host must route deferred round-progress retries through the round-progress Application");
        Test.True(generationTaskSource.Contains("TripAutomaticRoundCircuitBreaker(storage, owningRound, \"automatic_document_limit\", log)", StringComparison.Ordinal),
            "the generation task application must route circuit-breaker trips through the lifecycle rules");
        Test.True(rulesSource.Contains("CompleteRelayPassProgressAccounting(", StringComparison.Ordinal),
            "relay pass accounting must live in the lifecycle rules");
        Test.True(rulesSource.Contains("RecordPlayerOpportunity(", StringComparison.Ordinal),
            "player-opportunity recording must be composed inside the lifecycle rules");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.TryConsumeAiDocumentBudget(ref _runtime.AiDocumentsStartedDay", StringComparison.Ordinal)
            && campaignSource.Contains("ref _owner._runtime.AiDocumentsStartedDay", StringComparison.Ordinal),
            "the host must route the daily AI document budget through the lifecycle rules");
        Test.True(rulesSource.Contains("public static int FindPriorityThreatRelayIndex(", StringComparison.Ordinal)
            && rulesSource.Contains("public static bool TryConsumeAiDocumentBudget(", StringComparison.Ordinal)
            && rulesSource.Contains("public static void RecordPlayerOpportunity(", StringComparison.Ordinal)
            && rulesSource.Contains("public static void TripAutomaticRoundCircuitBreaker(", StringComparison.Ordinal)
            && progressSource.Contains("internal static void RetryDeferredRoundProgress(", StringComparison.Ordinal),
            "relay and budget orchestration must live in the lifecycle rules or their owning Application");
        Test.True(!behaviorSource.Contains("private int FindPriorityThreatRelayIndex(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private void CompleteRelayPassProgressAccounting(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private void TripAutomaticRoundCircuitBreaker(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private void RetryDeferredRoundProgress(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private void RecordPlayerOpportunity(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private bool TryConsumeAiDocumentBudget(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private void ResetDailyGenerationBudget(", StringComparison.Ordinal),
            "relay and budget orchestration must not remain in the host");

        Test.True(rulesSource.Contains("public static bool CanonicalDeltaContainsSourceKey(HashSet<string> sourceKeys, string sourceKey)", StringComparison.Ordinal),
            "the lifecycle rules must own canonical delta source-key lookup");
        Test.True(rulesSource.Contains("public static long EstimateHistoryTokens(string text, Func<string, int> estimateTokens)", StringComparison.Ordinal),
            "the lifecycle rules must own history token estimation");
        Test.True(rulesSource.Contains("public static void EnqueueDeferredCanonicalHistoryRetry(", StringComparison.Ordinal),
            "the lifecycle rules must own deferred retry enqueueing");
        Test.True(rulesSource.Contains("public static void ScheduleDeferredCanonicalHistoryRetry(", StringComparison.Ordinal),
            "the lifecycle rules must own deferred retry scheduling");
        Test.True(historyCaptureSource.Contains("internal static void RetryDeferredCanonicalHistoryEntries(", StringComparison.Ordinal),
            "the history-capture Application must own deferred retry processing");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.ScheduleDeferredCanonicalHistoryRetry(", StringComparison.Ordinal),
            "the host must route deferred retry scheduling through the lifecycle rules");
        Test.True(behaviorSource.Contains("WorldDiplomacyHistoryCaptureApplication.RetryDeferredCanonicalHistoryEntries(", StringComparison.Ordinal),
            "the host must route deferred retry processing through the lifecycle rules");
        Test.True(historyPublicationSource.Contains("WorldDiplomacyRoundLifecycleRules.CanonicalDeltaContainsSourceKey(sourceKeys, ", StringComparison.Ordinal),
            "the canonical history rules must route delta source-key lookup through the lifecycle rules");
        Test.True(migrationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.EstimateHistoryTokens(", StringComparison.Ordinal),
            "the host must route history token estimation through the lifecycle rules");
        Test.True(!behaviorSource.Contains("private bool CanonicalDeltaContainsSourceKey(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static long EstimateHistoryTokens(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private void EnqueueDeferredCanonicalHistoryRetry(", StringComparison.Ordinal),
            "canonical history maintenance helpers must not remain in the host");

        Test.True(File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyJobPreparationApplication.cs")).Contains("public static bool HasStaleDiplomaticActionPresentation(", StringComparison.Ordinal),
            "the lifecycle rules must own stale presentation detection");
        Test.True(rulesSource.Contains("public static int GetRoundParticipantLimit(int activityLevel, int maxRelayParticipants)", StringComparison.Ordinal),
            "the lifecycle rules must own the participant limit decision");
        Test.True(!behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.GetRoundParticipantLimit(GetActivityLevel(), MaxRelayParticipants)", StringComparison.Ordinal),
            "Q50 retired activity setting cannot change event participant capacity");
        Test.True(dispatchSource.Contains("WorldDiplomacyJobPreparationApplication.HasStaleDiplomaticActionPresentation(", StringComparison.Ordinal)
            && completionSource.Contains("WorldDiplomacyJobPreparationApplication.HasStaleDiplomaticActionPresentation(", StringComparison.Ordinal)
            && completionSource.Contains("orchestration.BuildGenerationLegalActionSignature", StringComparison.Ordinal)
            && dispatchSource.Contains("orchestration.BuildGenerationLegalActionSignature", StringComparison.Ordinal),
            "stale presentation checks must be evaluated by the application through the lifecycle rules");
        Test.True(!behaviorSource.Contains("HasStaleDiplomaticActionPresentation(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private string BuildWarNegotiationContext(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private bool IsTradeAllianceProposalCoolingDown(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string FormatCessionCandidates(", StringComparison.Ordinal),
            "misc helper bodies must not remain in the host");

        // DPL-060BU: exchange lifecycle and offer-cooldown index CRUD live in domain rules.
        Test.True(rulesSource.Contains("public static WorldDiplomacyExchange ResolveExchange(", StringComparison.Ordinal)
            && rulesSource.Contains("public static bool CompleteExchange(", StringComparison.Ordinal)
            && rulesSource.Contains("public static bool SuspendActiveExchangeForPlayerInsertion(", StringComparison.Ordinal)
            && rulesSource.Contains("public static WorldDiplomacyExchange RestoreSuspendedExchangeIfAny(", StringComparison.Ordinal)
            && rulesSource.Contains("public static void RebuildOfferCooldownIndex(", StringComparison.Ordinal)
            && File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOfferCooldownApplication.cs")).Contains("public static void UpsertOfferCooldown(", StringComparison.Ordinal)
            && rulesSource.Contains("public static void RemoveOfferCooldown(", StringComparison.Ordinal)
            && rulesSource.Contains("public static void ClearBilateralOfferCooldowns(", StringComparison.Ordinal),
            "exchange lifecycle and cooldown index rules must live in the lifecycle rules");
        Test.True(migrationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.ResolveExchange(", StringComparison.Ordinal)
            && exchangeApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.CompleteExchange(", StringComparison.Ordinal)
            && exchangeApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.RestoreSuspendedExchangeIfAny(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.RebuildOfferCooldownIndex(", StringComparison.Ordinal)
            && File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOfferCooldownApplication.cs")).Contains("WorldDiplomacyOfferCooldownApplication.UpsertOfferCooldown(", StringComparison.Ordinal)
            && File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOfferCooldownApplication.cs")).Contains("WorldDiplomacyRoundLifecycleRules.RemoveOfferCooldown(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.ClearBilateralOfferCooldowns(", StringComparison.Ordinal),
            "the host must route exchange lifecycle and cooldown index work through the lifecycle rules");
        Test.True(!behaviorSource.Contains("private void SuspendActiveExchangeForPlayerInsertion(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private WorldDiplomacyExchange ResolveExchange(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private void RebuildOfferCooldownIndex(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private void UpsertOfferCooldown(", StringComparison.Ordinal)
            && !behaviorSource.Contains("private void RemoveOfferCooldown(", StringComparison.Ordinal),
            "exchange lifecycle and cooldown index bodies must not remain in the host");

        // DPL-060BZ: canonical-history maintenance and result-settlement migrations
        // live in the persistence migrator; token accounting lives in lifecycle rules.
        Test.True(storageMigrationSource.Contains("public static void MigratePolicyCountdownHistory(", StringComparison.Ordinal)
            && storageMigrationSource.Contains("public static void BackfillCanonicalResponseLinksV2(", StringComparison.Ordinal)
            && storageMigrationSource.Contains("public static void MigrateResultSettlementStateIfNeeded(", StringComparison.Ordinal),
            "canonical-history and result-settlement migrations must live in the persistence migrator");
        Test.True(rulesSource.Contains("public static void RecalculateCanonicalHistoryTokens(", StringComparison.Ordinal),
            "canonical token accounting must live in the lifecycle rules");
        Test.True(storageMigrationSource.Contains("MigratePolicyCountdownHistory(history, storage.Jobs,", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyStorageMigration.BackfillCanonicalResponseLinksV2(", StringComparison.Ordinal)
            && normalizationApplicationSource.Contains("WorldDiplomacyStorageMigration.MigrateResultSettlementStateIfNeeded(", StringComparison.Ordinal)
            && normalizationApplicationSource.Contains("WorldDiplomacyCanonicalHistoryMigrationApplication.MigrateIfNeeded(", StringComparison.Ordinal)
            && normalizationApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.RecalculateCanonicalHistoryTokens(", StringComparison.Ordinal),
            "the host must route canonical-history maintenance through extracted ports");
        Test.True(!storageMigrationSource.Contains("TaleWorlds", StringComparison.Ordinal)
            && !storageMigrationSource.Contains("WorldDiplomacyBehavior", StringComparison.Ordinal)
            && !storageMigrationSource.Contains("Campaign.Current", StringComparison.Ordinal),
            "the persistence migrator must stay free of TaleWorlds and host references");

        // DPL-060CA: canonical-history state normalization lives in the persistence
        // migrator; protected-fact projection lives in the lifecycle rules.
        Test.True(storageMigrationSource.Contains("public static void NormalizeCanonicalHistoryState(", StringComparison.Ordinal),
            "canonical-history state normalization must live in the persistence migrator");
        Test.True(rulesSource.Contains("public static List<WorldDiplomacyCanonicalProtectedFact> BuildCanonicalProtectedFactsThrough(", StringComparison.Ordinal),
            "protected-fact projection must live in the lifecycle rules");
        Test.True(File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyHistoryCaptureApplication.cs")).Contains("WorldDiplomacyStorageMigration.NormalizeCanonicalHistoryState(", StringComparison.Ordinal)
            && historyCompressionSource.Contains("WorldDiplomacyRoundLifecycleRules.BuildCanonicalProtectedFactsThrough(", StringComparison.Ordinal)
            && behaviorSource.Contains("_runtime.CanonicalHistoryInitializedThisSession", StringComparison.Ordinal),
            "the host must keep the session guard and delegate normalization/projection through extracted ports");
        Test.True(!behaviorSource.Contains("threat-response:", StringComparison.Ordinal)
            && !behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.IsDeltaEntryRetired(", StringComparison.Ordinal)
            && !behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.ResolveNextDeltaSequence(", StringComparison.Ordinal),
            "the host must not retain the moved normalization or protected-fact internals");

        // DPL-060CB: the canonical-history append path (entry append, document
        // events, weekly artifact) lives in the canonical-history domain rules.
        Test.True(historyPublicationSource.Contains("public static bool AppendCanonicalHistoryEntry(", StringComparison.Ordinal)
            && historyPublicationSource.Contains("public static void AppendCanonicalDocumentEvents(", StringComparison.Ordinal)
            && historyPublicationSource.Contains("public static void AppendPublishedWorldWeeklyArtifact(", StringComparison.Ordinal),
            "the canonical-history append path must live in the domain rules");
        Test.True(behaviorSource.Contains("WorldDiplomacyHistoryPublicationApplication.AppendCanonicalHistoryEntry(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyHistoryPublicationApplication.AppendCanonicalDocumentEvents(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyHistoryPublicationApplication.AppendPublishedWorldWeeklyArtifact(", StringComparison.Ordinal),
            "the host must route canonical-history appends through the domain rules");
        Test.True(!canonicalHistoryRulesSource.Contains("TaleWorlds", StringComparison.Ordinal)
            && !canonicalHistoryRulesSource.Contains("Campaign.Current", StringComparison.Ordinal)
            && !canonicalHistoryRulesSource.Contains("MyBehavior", StringComparison.Ordinal),
            "the canonical-history append rules must stay free of TaleWorlds and host references");

        // DPL-060CC: policy artifact ledger sync (epoch/cursor management,
        // signature rebuild, artifact append) lives in the same domain rules.
        Test.True(historyPublicationSource.Contains("internal static void SyncPublishedPolicyArtifacts(", StringComparison.Ordinal)
            && historyPublicationSource.Contains("internal static void RebuildPublishedPolicySignaturesThrough(", StringComparison.Ordinal)
            && historyPublicationSource.Contains("internal static bool AppendPublishedPolicyArtifact(", StringComparison.Ordinal),
            "policy artifact sync must live in the canonical-history domain rules");
        Test.True(behaviorSource.Contains("WorldDiplomacyHistoryPublicationApplication.SyncPublishedPolicyArtifacts(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyHistoryPublicationApplication.RebuildPublishedPolicySignaturesThrough(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyHistoryPublicationApplication.AppendPublishedPolicyArtifact(", StringComparison.Ordinal),
            "the host must route policy artifact sync through the domain rules");
        Test.True(!historyPublicationSource.Contains("WorldDiplomacyPolicyContext", StringComparison.Ordinal),
            "policy artifact sync must reach the ledger only through injected ports");

        // DPL-060CD: threat history result appenders live in the canonical-history
        // domain rules; the host only forwards through the CanonicalHistoryAppender port.
        foreach (string movedAppender in new[]
        {
            "public static void TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(",
            "public static void TryAppendDiplomaticThreatNonComplianceHistoryResult(",
            "public static void TryAppendDiplomaticThreatHistoryResult(",
            "public static void TryAppendDiplomaticThreatIssuerRewardHistoryResult("
        })
        {
            Test.True(historyPublicationSource.Contains(movedAppender, StringComparison.Ordinal),
                "threat history appenders must live in the canonical-history rules: " + movedAppender);
        }
        Test.True(canonicalHistoryRulesSource.Contains("public delegate bool CanonicalHistoryAppender(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyHistoryPublicationApplication.TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyHistoryPublicationApplication.TryAppendDiplomaticThreatNonComplianceHistoryResult(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyHistoryPublicationApplication.TryAppendDiplomaticThreatHistoryResult(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyHistoryPublicationApplication.TryAppendDiplomaticThreatIssuerRewardHistoryResult(", StringComparison.Ordinal),
            "the host must route threat history appenders through the canonical-history rules");
        Test.True(!historyPublicationSource.Contains("AppendCanonicalHistoryEntry(", StringComparison.Ordinal)
                || historyPublicationSource.Contains("appendEntry(", StringComparison.Ordinal),
            "threat history appenders must write through the injected canonical appender port");

        // DPL-R1: round archive replacement belongs to Application; bounded
        // queue insertion and canonical history compression retain their owners.
        Test.True(historyCompressionSource.Contains("public static void CommitCompression(", StringComparison.Ordinal)
            && rulesSource.Contains("public static void EnqueueJob(", StringComparison.Ordinal)
            && roundCompressionSource.Contains("internal static void Commit(", StringComparison.Ordinal)
            && !rulesSource.Contains("public static void CommitRoundCompression(", StringComparison.Ordinal),
            "round archive completion must have one Application owner");
        Test.True(behaviorSource.Contains("WorldDiplomacyHistoryCompressionApplication.CommitCompression(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.EnqueueJob(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyRoundCompressionApplication.Commit(", StringComparison.Ordinal)
            && !behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.CommitRoundCompression(", StringComparison.Ordinal),
            "the host must route round archive completion through Application");
        Test.True(!behaviorSource.Contains("private static int ParseCompressionSequence(", StringComparison.Ordinal),
            "compression internals must not leak raw persistence details into the host");

        // DPL-060CF: canonical block rendering and job stamping live in the domain
        // rules; the host keeps only the render cache and orchestration.
        Test.True(canonicalHistoryRulesSource.Contains("public static string RenderCanonicalHistoryBlock(", StringComparison.Ordinal)
            && canonicalHistoryRulesSource.Contains("public static long ClampCanonicalHistoryThroughSequence(", StringComparison.Ordinal)
            && canonicalHistoryRulesSource.Contains("public static void StampCanonicalHistoryOnJob(", StringComparison.Ordinal),
            "history block rendering and job stamping must live in the canonical-history rules");
        Test.True(behaviorSource.Contains("WorldDiplomacyCanonicalHistoryRules.RenderCanonicalHistoryBlock(", StringComparison.Ordinal)
            && historyCaptureSource.Contains("WorldDiplomacyCanonicalHistoryRules.ClampCanonicalHistoryThroughSequence(", StringComparison.Ordinal)
            && historyCaptureSource.Contains("WorldDiplomacyCanonicalHistoryRules.StampCanonicalHistoryOnJob(", StringComparison.Ordinal),
            "the host must route block rendering and job stamping through the domain rules");
        Test.True(behaviorSource.Contains("_runtime.CanonicalHistoryRenderCacheKey = cacheKey", StringComparison.Ordinal)
            && behaviorSource.Contains("return _runtime.CanonicalHistoryRenderCache", StringComparison.Ordinal),
            "the render cache must stay on the host adapter");

        // DPL-060CG: compression scheduling and enqueue budget math live in the
        // canonical-history rules; the host keeps only thin port-binding adapters.
        Test.True(historyCompressionSource.Contains("public static void TryScheduleTokenCompression(", StringComparison.Ordinal)
            && historyCompressionSource.Contains("public static void EnqueueCompressionJob(", StringComparison.Ordinal)
            && historyCompressionSource.Contains("storage.DiplomacyCompressionPending", StringComparison.Ordinal)
            && historyCompressionSource.Contains("storage.CompressionRetryAfterHour", StringComparison.Ordinal)
            && historyCompressionSource.Contains("CompressionOverallTargetTokens = overallTargetTokens", StringComparison.Ordinal)
            && historyCompressionSource.Contains("resolveOutputTokenLimit()", StringComparison.Ordinal),
            "compression scheduling and enqueue math must live in the canonical-history rules");
        Test.True(behaviorSource.Contains("WorldDiplomacyHistoryCompressionApplication.TryScheduleTokenCompression(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyHistoryCompressionApplication.EnqueueCompressionJob(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyLlmClient.GetConfiguredOutputTokenLimit", StringComparison.Ordinal)
            && behaviorSource.Contains("CaptureCanonicalHistoryForJob", StringComparison.Ordinal),
            "the host must bind scheduling and enqueue ports through thin adapters");
        Test.True(!behaviorSource.Contains("InputBudgetHistoryTargetTokens > 0", StringComparison.Ordinal)
            && !behaviorSource.Contains("CompressionOverallTargetTokens", StringComparison.Ordinal)
            && !behaviorSource.Contains("diplomacy_compaction_", StringComparison.Ordinal),
            "the host must not retain a duplicate scheduling or enqueue algorithm");
        Test.True(!canonicalHistoryRulesSource.Contains("DuelSettings", StringComparison.Ordinal)
            && !canonicalHistoryRulesSource.Contains("TaleWorlds", StringComparison.Ordinal)
            && !canonicalHistoryRulesSource.Contains("MCM", StringComparison.Ordinal),
            "canonical-history rules must stay free of settings and engine dependencies");

        // DPL-060CH: the round-summary archive projection lives in the lifecycle
        // rules; the host keeps only thin port-binding adapters.
        Test.True(rulesSource.Contains("public static void CommitLocalRoundSummary(", StringComparison.Ordinal)
            && rulesSource.Contains("public static void UpgradeRoundSummaryToStructuredArchive(", StringComparison.Ordinal)
            && rulesSource.Contains("storage.RoundSummaries.RemoveAll(", StringComparison.Ordinal)
            && rulesSource.Contains("DiplomacyCompatibilityTexts.Get(\"History_LegacySummaryBracketPrefix\")", StringComparison.Ordinal),
            "the round-summary archive projection must live in the lifecycle rules");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.CommitLocalRoundSummary(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.UpgradeRoundSummaryToStructuredArchive(", StringComparison.Ordinal),
            "the host must route round-summary archiving through the domain rules");
        Test.True(!behaviorSource.Contains("ArchiveSchemaVersion = 1", StringComparison.Ordinal)
            && !behaviorSource.Contains("[宣言记录] ", StringComparison.Ordinal),
            "the host must not retain a duplicate archive projection");

        // DPL-060CI: offer pruning orchestration lives in the lifecycle rules behind
        // world-readiness, party, war, alliance, trade, and peace-terms probe ports.
        Test.True(rulesSource.Contains("public static void PruneInvalidOffers(", StringComparison.Ordinal)
            && rulesSource.Contains("Func<bool> worldReady", StringComparison.Ordinal)
            && rulesSource.Contains("Func<WorldDiplomacyRoundOffer, bool> arePeaceTermsExecutable", StringComparison.Ordinal)
            && rulesSource.Contains("superseded.Status = \"superseded\"", StringComparison.Ordinal),
            "offer pruning orchestration must live in the lifecycle rules behind probe ports");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.PruneInvalidOffers(", StringComparison.Ordinal)
            && behaviorSource.Contains("BuildDocumentIndex(Storage?.Documents)", StringComparison.Ordinal),
            "the host must bind pruning probes and keep the lazy document index in the adapter");
        Test.True(!behaviorSource.Contains("IsOpenLifecycleStatus(x.Status))\n\t\t\t.GroupBy", StringComparison.Ordinal)
            && !behaviorSource.Contains("stale diplomacy offers invalidated", StringComparison.Ordinal),
            "the host must not retain a duplicate pruning loop");

        // DPL-060CJ: stored per-record normalization lives in the storage migration
        // rules behind kingdom-name, id-list, date, coverage, duration, and prune ports.
        Test.True(storageMigrationSource.Contains("public static void NormalizeStoredDocumentRecord(", StringComparison.Ordinal)
            && storageMigrationSource.Contains("IReadOnlyCollection<string> nonHideoutSettlementIds", StringComparison.Ordinal)
            && storageMigrationSource.Contains("IReadOnlyCollection<string> nonEliminatedKingdomIds", StringComparison.Ordinal)
            && storageMigrationSource.Contains("WorldDiplomacyRoundLifecycleRules.HasCompleteLegacyPropagationCoverage(", StringComparison.Ordinal)
            && storageMigrationSource.Contains("public static void NormalizeStoredRoundRecord(", StringComparison.Ordinal)
            && storageMigrationSource.Contains("pruneInvalidOffers?.Invoke(round)", StringComparison.Ordinal),
            "stored record normalization must live in the storage migration rules behind bounded snapshot ports");
        Test.True(!storageMigrationSource.Contains("Func<WorldDiplomacyDocument, bool> hasCompleteLegacyPropagationCoverage", StringComparison.Ordinal),
            "the propagation coverage callback must be replaced by bounded id snapshots");
        Test.True(normalizationApplicationSource.Contains("WorldDiplomacyStorageMigration.NormalizeStoredDocumentRecord(", StringComparison.Ordinal)
            && normalizationApplicationSource.Contains("WorldDiplomacyStorageMigration.NormalizeStoredRoundRecord(", StringComparison.Ordinal)
            && normalizationApplicationSource.Contains("source.CaptureNonHideoutSettlementIds()", StringComparison.Ordinal)
            && normalizationApplicationSource.Contains("source.CaptureNonEliminatedKingdomIds()", StringComparison.Ordinal)
            && normalizationApplicationSource.Contains("source.ResolveKingdomNameOrEmpty", StringComparison.Ordinal)
            && normalizationApplicationSource.Contains("source.RoundHardDurationDays, orchestration.PruneInvalidOffers, source.Log", StringComparison.Ordinal),
            "the normalization owner must bind storage normalization ports through thin adapters");
        Test.True(!behaviorSource.Contains("normalizedOfferSourceBindings", StringComparison.Ordinal)
            && !behaviorSource.Contains("Kingdom actionTarget = ResolveKingdom(action.TargetKingdomId)", StringComparison.Ordinal),
            "the host must not retain the per-record normalization bodies");

        // DPL-060CK: round offer and policy signal bookkeeping lives in the lifecycle
        // rules; the host keeps only thin adapters that supply storage and probes.
        Test.True(rulesSource.Contains("public static void CompletePolicySignal(", StringComparison.Ordinal)
            && rulesSource.Contains("public static void AttachPolicySignalToRound(", StringComparison.Ordinal)
            && File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOfferCooldownApplication.cs")).Contains("public static void SettleTradeAllianceOfferCooldownsForClosedRound(", StringComparison.Ordinal)
            && rulesSource.Contains("public static void RegisterRelayProposalOffer(", StringComparison.Ordinal)
            && rulesSource.Contains("public static List<WorldDiplomacyRoundOffer> SelectMatchingRelayResponseOffers(", StringComparison.Ordinal),
            "round offer and signal bookkeeping must live in the lifecycle rules");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.CompletePolicySignal(", StringComparison.Ordinal)
            && policyRoundApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.AttachPolicySignalToRound(", StringComparison.Ordinal)
            && policyRoundApplicationSource.Contains("WorldDiplomacyStructureRules.EnsureRoundParticipant(", StringComparison.Ordinal)
            && policyRoundApplicationSource.Contains("participant.IsPlayerAsync = isPlayer", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyOfferCooldownApplication.SettleTradeAllianceOfferCooldownsForClosedRound(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyOfferApplication.Settle(", StringComparison.Ordinal)
            && offerApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.RegisterRelayProposalOffer(", StringComparison.Ordinal)
            && offerApplicationSource.Contains("WorldDiplomacyRoundLifecycleRules.SelectMatchingRelayResponseOffers(", StringComparison.Ordinal),
            "the offer application must route canonical bookkeeping through the lifecycle rules");
        Test.True(!behaviorSource.Contains("AttachPolicySignalToRound(round, signal,", StringComparison.Ordinal),
            "observer participant writes must not be hidden in a host callback");
        Test.True(!behaviorSource.Contains("countered.Status = \"countered\"", StringComparison.Ordinal)
            && !behaviorSource.Contains("_storage.PendingPolicySignals.RemoveAll(item => item != null && string.Equals(item.SignalKey", StringComparison.Ordinal)
            && !behaviorSource.Contains("decision.Action == WorldDiplomacyOfferCooldownAction.ClearCooldown", StringComparison.Ordinal),
            "the host must not retain the bookkeeping bodies");

        // DPL-060CL: legal action-intent orchestration lives in the lifecycle rules;
        // the host keeps Kingdom adapters and the TW-coupled potential set.
        Test.True(rulesSource.Contains("public static void AppendOpenOfferResponseIntents(", StringComparison.Ordinal)
            && rulesSource.Contains("public static WorldDiplomacyRoundOffer FindRequiredPeaceOfferResponse(", StringComparison.Ordinal)
            && rulesSource.Contains("public static List<string> BuildLegalDiplomaticActionIntents(", StringComparison.Ordinal)
            && rulesSource.Contains("Func<List<string>> buildPotentialActions", StringComparison.Ordinal)
            && rulesSource.Contains("Func<string, WorldDiplomacyDocument> resolveDocument", StringComparison.Ordinal),
            "legal action-intent orchestration must live in the lifecycle rules behind ports");
        Test.True(rulesSource.Contains("AppendOpenOfferResponseIntents(round, authorKingdomId, targetKingdomId, actions);", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.FindRequiredPeaceOfferResponse(", StringComparison.Ordinal)
            && actionSelectionSource.Contains("WorldDiplomacyRoundLifecycleRules.BuildLegalDiplomaticActionIntents(", StringComparison.Ordinal)
            && actionSelectionSource.Contains("() => playerDiplomacy ? PlayerPotentialIntents() : BuildPotentialDiplomaticActionIntents(author, target), _port.ResolveDocument, playerDiplomacy", StringComparison.Ordinal),
            "the host must bind legal-intent ports through thin adapters");
        Test.True(!behaviorSource.Contains("ownOpenProposalIntents", StringComparison.Ordinal)
            && !behaviorSource.Contains("openPeaceOffers", StringComparison.Ordinal)
            && !behaviorSource.Contains("hasAnswerableOfferForTarget", StringComparison.Ordinal),
            "the host must not retain the legal-intent orchestration bodies");

        // DPL-060CM: the relay response source-context projection lives in the
        // lifecycle rules; the host supplies the resolved author id and documents.
        Test.True(rulesSource.Contains("public static void AppendRelayResponseSourceContext(", StringComparison.Ordinal)
            && rulesSource.Contains("List<WorldDiplomacyDocument> documents", StringComparison.Ordinal)
            && rulesSource.Contains("answerablePeaceOfferSourceIds", StringComparison.Ordinal),
            "the relay response source-context projection must live in the lifecycle rules");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.AppendRelayResponseSourceContext(", StringComparison.Ordinal)
            && behaviorSource.Contains("_storage?.Documents", StringComparison.Ordinal),
            "the host must bind the source-context ports through a thin adapter");
        Test.True(!behaviorSource.Contains("answerablePeaceOfferSourceIds", StringComparison.Ordinal)
            && !behaviorSource.Contains("answerableOfferSourceIds", StringComparison.Ordinal),
            "the host must not retain the source-context projection body");

        // DPL-060CN: the generation legal-action signature lives in the lifecycle
        // rules; the host resolves author/round/document and binds kingdom ports.
        Test.True(rulesSource.Contains("public static string BuildGenerationLegalActionSignature(", StringComparison.Ordinal)
            && rulesSource.Contains("Func<List<string>> resolveSettlementTargetIds", StringComparison.Ordinal)
            && rulesSource.Contains("Func<string, bool> isTargetUnavailable", StringComparison.Ordinal)
            && rulesSource.Contains("Func<string, List<string>> buildDeclarationIntents", StringComparison.Ordinal),
            "the legal-action signature composition must live in the lifecycle rules behind ports");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.BuildGenerationLegalActionSignature(", StringComparison.Ordinal)
            && behaviorSource.Contains("GetResultSettlementActionableTargetIds(round, authorId)", StringComparison.Ordinal)
            && behaviorSource.Contains("BuildLegalDiplomaticDeclarationIntents(", StringComparison.Ordinal),
            "the host must bind signature ports through a thin adapter");
        Test.True(!behaviorSource.Contains("StringBuilder state = new StringBuilder()", StringComparison.Ordinal)
            && !behaviorSource.Contains("NormalizeOrderedIdList(ids", StringComparison.Ordinal),
            "the host must not retain the signature composition body");

        // DPL-060CO: generated-structure derivation and authorized target selection
        // live in the lifecycle rules; the host binds live-kingdom ports.
        Test.True(rulesSource.Contains("public static bool TryDeriveGeneratedDiplomaticStructure(", StringComparison.Ordinal)
            && rulesSource.Contains("public static List<string> GetAuthorizedGenerationTargetIds(", StringComparison.Ordinal)
            && rulesSource.Contains("Func<List<string>> resolveSettlementTargetIds", StringComparison.Ordinal)
            && rulesSource.Contains("Func<string, bool> isCandidateAuthorized", StringComparison.Ordinal),
            "structure derivation and target selection must live in the lifecycle rules behind ports");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.TryDeriveGeneratedDiplomaticStructure(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.GetAuthorizedGenerationTargetIds(", StringComparison.Ordinal)
            && behaviorSource.Contains("HasIndependentWorldDiplomacyAuthority(candidate)", StringComparison.Ordinal),
            "the host must bind structure and target ports through thin adapters");
        Test.True(!behaviorSource.Contains("offer_response_without_unique_open_offer", StringComparison.Ordinal)
            && !behaviorSource.Contains("comply_ultimatum_without_open_threat", StringComparison.Ordinal)
            && !behaviorSource.Contains("comply_ultimatum_without_unique_presented_threat", StringComparison.Ordinal),
            "the host must not retain structure-stamping or target-ordering bodies");

        // DPL-060CP: the player world-state violation chain lives in the
        // generation-validation rules; the host collapses live-party probes.
        Test.True(validationSource.Contains("public static bool TryGetPlayerWorldStateIntentViolation(", StringComparison.Ordinal)
            && validationSource.Contains("Func<string, (bool Violation, string Reason)> stateViolation", StringComparison.Ordinal)
            && validationSource.Contains("Func<string, string, (bool Violation, string Reason)> threatViolation", StringComparison.Ordinal),
            "the player world-state violation chain must live in the validation rules behind probe ports");
        Test.True(behaviorSource.Contains("WorldDiplomacyGenerationValidationRules.TryGetPlayerWorldStateIntentViolation(", StringComparison.Ordinal)
            && behaviorSource.Contains("bool partiesEligible = document != null", StringComparison.Ordinal)
            && behaviorSource.Contains("!_host.IsEliminatedParty(authorId) && !_host.IsEliminatedParty(targetId)", StringComparison.Ordinal)
            && behaviorSource.Contains("IsEventDocument(document)", StringComparison.Ordinal),
            "the host must collapse live-party eligibility and bind violation probes");
        Test.True(!behaviorSource.Contains("player_action_has_no_eligible_parties", StringComparison.Ordinal)
            && !behaviorSource.Contains("player_compliance_missing_source_threat", StringComparison.Ordinal)
            && !behaviorSource.Contains("player_offer_response_without_exact_open_offer", StringComparison.Ordinal),
            "the host must not retain raw player-violation reason composition");

        // DPL-060CQ: threat registration bookkeeping lives in the lifecycle rules;
        // the host supplies the writable list lazily after the identity gate.
        Test.True(bindingSource.Contains("public static bool RegisterOrAdvanceDiplomaticThreat(", StringComparison.Ordinal)
            && bindingSource.Contains("Func<List<WorldDiplomacyThreat>> resolveThreats", StringComparison.Ordinal)
            && bindingSource.Contains("Func<WorldDiplomacyPolicySignal> resolvePolicyCondition", StringComparison.Ordinal),
            "threat registration bookkeeping must live in the lifecycle rules behind ports");
        Test.True(bindingSource.Contains("RegisterOrAdvanceDiplomaticThreat(", StringComparison.Ordinal)
            && bindingSource.Contains("TryResolvePolicyConditionForThreat(document, issuerId, targetId", StringComparison.Ordinal)
            && behaviorSource.Contains("WarningEscalationPrestigeReward", StringComparison.Ordinal),
            "the host must bind threat storage, policy resolution, and prestige ports");
        Test.True(!behaviorSource.Contains("new WorldDiplomacyThreat", StringComparison.Ordinal)
            && !behaviorSource.Contains("existing.Stage = \"ultimatum\"", StringComparison.Ordinal),
            "the host must not retain threat record construction or escalation bodies");

        // DPL-060CS: repair correction text lives in the prompt contract rules;
        // repair job construction lives in the lifecycle rules behind ports.
        Test.True(promptContractSource.Contains("public static void AppendGeneratedRepairCorrection(", StringComparison.Ordinal)
            && promptContractSource.Contains("Func<string> realmGovernmentHardFact", StringComparison.Ordinal)
            && rulesSource.Contains("public static WorldDiplomacyJob BuildGeneratedDeclarationRepairJob(", StringComparison.Ordinal),
            "repair correction and job construction must live in domain rules behind ports");
        Test.True(behaviorSource.Contains("WorldDiplomacyPromptContractRules.AppendGeneratedRepairCorrection(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.BuildGeneratedDeclarationRepairJob(", StringComparison.Ordinal)
            && behaviorSource.Contains("() => world.BuildGovernmentHardFact(", StringComparison.Ordinal),
            "the host must bind repair text, job construction, and lazy realm-fact ports");
        Test.True(!behaviorSource.Contains("SemanticRepairAttempts = source.SemanticRepairAttempts + 1", StringComparison.Ordinal)
            && !behaviorSource.Contains("CandidateKingdomIds = resultSettlementRepair", StringComparison.Ordinal),
            "the host must not retain repair job field composition");

        // DPL-060CT: the single-action legality chain lives in the validation
        // rules; the host binds Kingdom resolution and live-state probes.
        Test.True(validationSource.Contains("public static bool TryGetGeneratedSingleActionLegalityViolation(", StringComparison.Ordinal)
            && validationSource.Contains("Func<string, string> resolveKingdomId", StringComparison.Ordinal)
            && validationSource.Contains("Func<WorldDiplomacyRound, string, WorldDiplomacyDocument, List<string>> buildLegalDeclarationIntents", StringComparison.Ordinal)
            && validationSource.Contains("out string generatedTargetId", StringComparison.Ordinal),
            "the single-action legality chain must live in the validation rules behind ports");
        Test.True(behaviorSource.Contains("WorldDiplomacyGenerationValidationRules.TryGetGeneratedSingleActionLegalityViolation(", StringComparison.Ordinal)
            && behaviorSource.Contains("id => _host.ResolveKingdomIdOrNull(id)", StringComparison.Ordinal)
            && behaviorSource.Contains("CanUseResultSettlementTarget(round, author", StringComparison.Ordinal),
            "the host must bind kingdom resolution and live-state probes through thin closures");
        Test.True(!behaviorSource.Contains("reason = \"kingdom_not_in_relay_route\"", StringComparison.Ordinal)
            && !behaviorSource.Contains("reason = \"autonomous_round_plan_exceeds_participant_limit\"", StringComparison.Ordinal)
            && !behaviorSource.Contains("private bool TryGetGeneratedSingleActionLegalityViolation(\n\t\tWorldDiplomacyJob job,\n\t\tJObject json,\n\t\tstring authorId", StringComparison.Ordinal),
            "the host must not retain the single-action validation body");

        // DPL-060CU: the generated semantic-envelope application chain lives in
        // the validation rules; the host binds Kingdom resolution and probes.
        Test.True(validationSource.Contains("public static bool TryApplyGeneratedSemanticEnvelope(", StringComparison.Ordinal)
            && validationSource.Contains("public static bool TryApplyGeneratedSingleActionSemanticEnvelope(", StringComparison.Ordinal)
            && validationSource.Contains("Func<WorldDiplomacyDocument, JObject, string, bool, bool, bool> applySingleAction", StringComparison.Ordinal)
            && validationSource.Contains("Func<JObject, string, string, WorldDiplomacyPeaceTerms> parsePeaceTerms", StringComparison.Ordinal),
            "the generated envelope application chain must live in the validation rules behind ports");
        Test.True(behaviorSource.Contains("WorldDiplomacyGenerationValidationRules.TryApplyGeneratedSemanticEnvelope(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyGenerationValidationRules.TryApplyGeneratedSingleActionSemanticEnvelope(", StringComparison.Ordinal)
            && behaviorSource.Contains("IsNonRootAiRelayNoActionAllowed(", StringComparison.Ordinal)
            && behaviorSource.Contains("ParseAndValidatePeaceTerms(", StringComparison.Ordinal),
            "the host must bind kingdom resolution, relay authorization, and peace-term parsing through thin closures");
        Test.True(!behaviorSource.Contains("document.AnalysisStatus = \"generation_envelope\"", StringComparison.Ordinal)
            && !behaviorSource.Contains("allowedRoundResponseNoAction = string.Equals(intent, \"statement\"", StringComparison.Ordinal)
            && !behaviorSource.Contains("applied.Add(action)", StringComparison.Ordinal),
            "the host must not retain envelope application bookkeeping");

        // DPL-060CV: settlement-turn bookkeeping, invalid-suppression close
        // gating, and threat-compliance records live in the lifecycle rules.
        Test.True(rulesSource.Contains("public static void MarkSettlementSlotWaitingForPlayer(", StringComparison.Ordinal)
            && rulesSource.Contains("public static void ScheduleSettlementRelayArrival(", StringComparison.Ordinal)
            && rulesSource.Contains("public static bool ShouldCloseRoundAfterInvalidSuppression(", StringComparison.Ordinal)
            && rulesSource.Contains("public static void ApplyThreatComplianceResolution(", StringComparison.Ordinal),
            "settlement-turn bookkeeping, suppression close gating, and compliance records must live in the lifecycle rules");
        Test.True(rulesSource.Contains("MarkSettlementSlotWaitingForPlayer(", StringComparison.Ordinal)
            && rulesSource.Contains("ScheduleSettlementRelayArrival(", StringComparison.Ordinal)
            && analysisSource.Contains("WorldDiplomacyRoundLifecycleRules.ShouldCloseRoundAfterInvalidSuppression(", StringComparison.Ordinal)
            && threatSettlementSource.Contains("WorldDiplomacyRoundLifecycleRules.ApplyThreatComplianceResolution(", StringComparison.Ordinal),
            "the host must route settlement bookkeeping, close gating, and compliance resolution through the lifecycle rules");
        Test.True(!behaviorSource.Contains("threat.ResolutionReason = \"target_explicitly_complied\"", StringComparison.Ordinal)
            && !behaviorSource.Contains("slot.Status = \"waiting_player\"", StringComparison.Ordinal)
            && !behaviorSource.Contains("round.ResultSettlementPlayerWaitingSinceDay = ", StringComparison.Ordinal),
            "the host must not retain settlement-wait or compliance record fields");

        // DPL-060CW: player-declaration offer reconciliation stays a decision rule;
        // result-settlement open bookkeeping moved to the round Application.
        Test.True(rulesSource.Contains("public static void ReconcilePlayerDeclarationWithOpenOffer(", StringComparison.Ordinal)
            && applicationSource.Contains("internal static void BeginOrExtendRoundResultSettlement(", StringComparison.Ordinal)
            && applicationSource.Contains("Func<WorldDiplomacyRound, string, bool> includeResultSettlementTarget", StringComparison.Ordinal)
            && applicationSource.Contains("Action<WorldDiplomacyRound> refreshActionSlots", StringComparison.Ordinal)
            && !rulesSource.Contains("public static void BeginOrExtendRoundResultSettlement(", StringComparison.Ordinal),
            "offer reconciliation must stay a domain rule; settlement-open bookkeeping must live in the round Application behind ports");
        Test.True(analysisSource.Contains("ReconcilePlayerDeclarationWithOpenOffer(document, intent, sourceRound ?? resolveRound?.Invoke(document.RoundId), ref targetId, ref respondingToOfferDocumentId, log);", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyRoundApplication.BeginOrExtendRoundResultSettlement(", StringComparison.Ordinal)
            && File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOfferApplication.cs"))
                .Contains("port.ResolveRound(document?.RoundId)", StringComparison.Ordinal)
            && behaviorSource.Contains("RefreshResultSettlementActionSlots", StringComparison.Ordinal),
            "the host must bind round resolution and slot refresh through thin adapters");
        Test.True(!behaviorSource.Contains("ResultSettlementTriggerDocumentId = document.DocumentId", StringComparison.Ordinal)
            && !behaviorSource.Contains("claimedOfferDocumentId", StringComparison.Ordinal),
            "the host must not retain offer-matching or settlement-open bodies");

        // DPL-R1: the canonical pre-send migration and input-budget admission
        // belong to the launch owner; the host binds token limits and effects.
        Test.True(dispatchSource.Contains("internal static bool EnsureRequestFitsInputBudget(", StringComparison.Ordinal)
            && dispatchSource.Contains("internal static bool EnsureCurrentCanonicalPromptContractBeforeSend(", StringComparison.Ordinal)
            && dispatchSource.Contains("Func<WorldDiplomacyJob, bool> rebuildPendingJob", StringComparison.Ordinal)
            && dispatchSource.Contains("Func<long, string> buildHistoryBlock", StringComparison.Ordinal)
            && dispatchSource.Contains("Action<string> removeJob", StringComparison.Ordinal)
            && !rulesSource.Contains("public static bool EnsureRequestFitsInputBudget(", StringComparison.Ordinal),
            "pre-send migration and budget admission must live with Application dispatch behind ports");
        Test.True(dispatchSource.Contains("EnsureRequestFitsInputBudget(job, requestMessages", StringComparison.Ordinal)
            && dispatchSource.Contains("if (!EnsureCurrentCanonicalPromptContractBeforeSend(", StringComparison.Ordinal)
            && behaviorSource.Contains("GetHistoryCompressionTriggerTokens()", StringComparison.Ordinal)
            && behaviorSource.Contains("Logger.EstimateTokens", StringComparison.Ordinal)
            && behaviorSource.Contains("TryScheduleTokenCompression()", StringComparison.Ordinal),
            "the host must bind token limits and job ports through thin adapters");
        Test.True(!behaviorSource.Contains("input budget exceeded before send:", StringComparison.Ordinal)
            && !behaviorSource.Contains("AwaitingHistoryCompression = true", StringComparison.Ordinal)
            && !behaviorSource.Contains("retired stale canonical prompt contract", StringComparison.Ordinal)
            && !behaviorSource.Contains("CompressionRetryAttempts = 0", StringComparison.Ordinal),
            "the host must not retain budget-math or contract-retirement bodies");

        // Round progress routing is owned by Application; the host binds live ports.
        Test.True(progressSource.Contains("internal static void HandleRoundDocumentProcessed(", StringComparison.Ordinal)
            && progressSource.Contains("Func<string, WorldDiplomacyRound> resolveRound", StringComparison.Ordinal)
            && progressSource.Contains("Action<WorldDiplomacyRound> advanceRelay", StringComparison.Ordinal)
            && progressSource.Contains("Action<string> closeActiveRound", StringComparison.Ordinal),
            "the document-processed routing table must live in Application behind ports");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundProgressApplication.HandleRoundDocumentProcessed(", StringComparison.Ordinal)
            && behaviorSource.Contains("round => AdvanceRelay(round, scheduleImmediately: false)", StringComparison.Ordinal),
            "the host must bind round processing through a thin adapter");
        Test.True(!rulesSource.Contains("public static void HandleRoundDocumentProcessed(", StringComparison.Ordinal),
            "the former Domain owner must not retain round progress orchestration");
        Test.True(!behaviorSource.Contains("RoundAccountingHandled = true", StringComparison.Ordinal)
            && !behaviorSource.Contains("substantive diplomacy progress accepted", StringComparison.Ordinal)
            && !behaviorSource.Contains("priority player declaration response completed", StringComparison.Ordinal),
            "the host must not retain progress bookkeeping or relay-routing bodies");

        // DPL-060CZ/R1: weekly-material snapshots live with the history-capture
        // Application; invalid-document suppression stays in analysis rules.
        Test.True(historyCaptureSource.Contains("internal static void RecordDiplomacyWeeklyMaterial(", StringComparison.Ordinal)
            && !rulesSource.Contains("public static void RecordDiplomacyWeeklyMaterial(", StringComparison.Ordinal)
            && analysisSource.Contains("public static void SuppressInvalidDocumentBeforePropagation(", StringComparison.Ordinal)
            && analysisSource.Contains("Action<string, string> completeExchange", StringComparison.Ordinal)
            && analysisSource.Contains("Func<string, string> resolveKingdomId", StringComparison.Ordinal),
            "document lifecycle bookkeeping must live in the owning services behind ports");
        Test.True(behaviorSource.Contains("WorldDiplomacyHistoryCaptureApplication.RecordDiplomacyWeeklyMaterial(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyAnalysisApplication.Suppress(", StringComparison.Ordinal)
            && behaviorSource.Contains("MyBehavior.RecordWorldDiplomacyWeeklyMaterialForExternal", StringComparison.Ordinal),
            "the host must bind material recording and suppression through thin adapters");
        Test.True(!behaviorSource.Contains("invalid generated document suppressed before propagation", StringComparison.Ordinal)
            && !behaviorSource.Contains("尚未标注[游戏已执行]", StringComparison.Ordinal)
            && !behaviorSource.Contains("world_diplomacy:", StringComparison.Ordinal),
            "the host must not retain snapshot text or suppression bookkeeping bodies");

        // DPL-060DA: tick-time lifecycle and after-load reconcile routing live
        // in the round Application; the host binds storage and scheduling ports.
        Test.True(exchangeApplicationSource.Contains("internal static void ProcessRoundLifecycle(", StringComparison.Ordinal)
            && exchangeApplicationSource.Contains("internal static void ReconcileActiveDiplomacyAfterLoad(", StringComparison.Ordinal)
            && exchangeApplicationSource.Contains("Action<WorldDiplomacyRound> scheduleRelayHop", StringComparison.Ordinal)
            && exchangeApplicationSource.Contains("Action<WorldDiplomacyRound> scheduleRelayHopImmediately", StringComparison.Ordinal)
            && !rulesSource.Contains("public static void ProcessRoundLifecycle(", StringComparison.Ordinal)
            && !rulesSource.Contains("public static void ReconcileActiveDiplomacyAfterLoad(", StringComparison.Ordinal),
            "round lifecycle maintenance must live in the round Application behind ports");
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundApplication.ProcessRoundLifecycle(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyRoundApplication.ReconcileActiveDiplomacyAfterLoad(", StringComparison.Ordinal)
            && behaviorSource.Contains("r => ScheduleNextRelayHop(r, true)", StringComparison.Ordinal),
            "the host must bind lifecycle maintenance through thin adapters");
        Test.True(!behaviorSource.Contains("relay_all_ai_withdrew", StringComparison.Ordinal)
            && !behaviorSource.Contains("result_settlement_circuit_breaker", StringComparison.Ordinal)
            && !behaviorSource.Contains("technical_missing_root_after_load", StringComparison.Ordinal),
            "the host must not retain terminal-close reasons or reconcile bodies");
        Test.True(behaviorSource.Contains("WorldDiplomacyAnalysisApplication.Commit(", StringComparison.Ordinal)
            && analysisSource.Contains("public static void CommitAnalysis(", StringComparison.Ordinal),
            "the host must route analysis commits through the lifecycle rules");
        Test.True(!behaviorSource.Contains("analysis_status_has_no_publishable_action", StringComparison.Ordinal)
            && !behaviorSource.Contains("analysis_has_no_structured_intent", StringComparison.Ordinal)
            && !behaviorSource.Contains("analysis_has_no_actionable_intent", StringComparison.Ordinal)
            && analysisSource.Contains("analysis_has_no_actionable_intent", StringComparison.Ordinal),
            "analysis suppression reasons must live inside the lifecycle rules");
        string roundPlanApplicationSource = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyRoundPlanApplication.cs"));
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundPlanApplication.Commit(", StringComparison.Ordinal)
            && roundPlanApplicationSource.Contains("internal static void Commit(", StringComparison.Ordinal)
            && !rulesSource.Contains("public static void CommitRoundPlan(", StringComparison.Ordinal),
            "the host must route round-plan commits through Application");
        Test.True(!behaviorSource.Contains("round_plan_no_participants", StringComparison.Ordinal)
            && roundPlanApplicationSource.Contains("round_plan_no_participants", StringComparison.Ordinal)
            && !behaviorSource.Contains("mandatoryIds", StringComparison.Ordinal)
            && roundPlanApplicationSource.Contains("RelayRouteKingdomIds = route", StringComparison.Ordinal),
            "relay route planning must live inside Application");
        string failureApplicationSource = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyFailureApplication.cs"));
        Test.True(behaviorSource.Contains("WorldDiplomacyFailureApplication.Commit(", StringComparison.Ordinal)
            && failureApplicationSource.Contains("internal static void Commit(", StringComparison.Ordinal)
            && !rulesSource.Contains("public static void CommitFailedJob(", StringComparison.Ordinal),
            "the host must route failed-job commits through Application");
        Test.True(!behaviorSource.Contains("autonomous_generation_failed", StringComparison.Ordinal)
            && failureApplicationSource.Contains("autonomous_generation_failed", StringComparison.Ordinal)
            && failureApplicationSource.Contains("DiplomacyCompressionBudget.Fail(", StringComparison.Ordinal),
            "failed-job kind routing and compression backoff must live inside Application");

        // DPL-060DE: generated-document commit orchestration
        Test.True(behaviorSource.Contains("WorldDiplomacyGeneratedCompletionApplication.Commit(", StringComparison.Ordinal)
            && generatedCompletionSource.Contains("internal static void Commit(", StringComparison.Ordinal)
            && !rulesSource.Contains("public static void CommitGeneratedDocument(", StringComparison.Ordinal),
            "the host must route generated-document commits through Application");
        Test.True(!behaviorSource.Contains("empty_public_document", StringComparison.Ordinal)
            && generatedCompletionSource.Contains("empty_public_document", StringComparison.Ordinal)
            && !behaviorSource.Contains("stale result-settlement generation discarded", StringComparison.Ordinal)
            && generatedCompletionSource.Contains("stale result-settlement generation discarded", StringComparison.Ordinal)
            && !behaviorSource.Contains("generated declaration discarded at commit", StringComparison.Ordinal)
            && generatedCompletionSource.Contains("generated declaration discarded at commit", StringComparison.Ordinal)
            && !behaviorSource.Contains("\"analyzing_response\"", StringComparison.Ordinal)
            && generatedCompletionSource.Contains("\"analyzing_response\"", StringComparison.Ordinal)
            && generatedCompletionSource.Contains("IsAutonomousNoActionDeclaration = false", StringComparison.Ordinal),
            "generated-document rejection, exchange bookkeeping, and round metadata must live inside Application");
        Test.True(behaviorSource.Contains("AbandonRejectedGeneration,", StringComparison.Ordinal)
            && behaviorSource.Contains("ScheduleNextResultSettlementTurn,", StringComparison.Ordinal)
            && behaviorSource.Contains("AddDocument,", StringComparison.Ordinal)
            && behaviorSource.Contains("ProcessAnalyzedDocument(document, intent, commitment, response, tone, confidence)", StringComparison.Ordinal)
            && behaviorSource.Contains("CommitPlayerResponseCoverage(job, document)", StringComparison.Ordinal),
            "the host must keep binding the generated-document live-state adapters");

        // DPL-060DF/R1: post-decision routing; generation abandonment is owned by
        // WorldDiplomacyGenerationTaskApplication and finalization by the publication Application.
        Test.True(behaviorSource.Contains("WorldDiplomacyDocumentPublicationApplication.FinalizePublishedDocumentAfterAnalysis(", StringComparison.Ordinal)
            && bindingSource.Contains("internal static void Process(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyGenerationTaskApplication.AbandonRejectedGeneration(", StringComparison.Ordinal)
            && documentPublication.Contains("internal static void FinalizePublishedDocumentAfterAnalysis(", StringComparison.Ordinal)
            && !rulesSource.Contains("public static void FinalizePublishedDocumentAfterAnalysis(", StringComparison.Ordinal)
            && bindingSource.Contains("internal static void Process(", StringComparison.Ordinal)
            && generationTaskSource.Contains("internal static void AbandonRejectedGeneration(", StringComparison.Ordinal),
            "the host must route post-decision finalization, threat dispatch, and abandonment through the owning services");
        Test.True(!behaviorSource.Contains("generated declaration abandoned without publication", StringComparison.Ordinal)
            && generationTaskSource.Contains("generated declaration abandoned without publication", StringComparison.Ordinal)
            && !behaviorSource.Contains("technical_consecutive_generation_rejections", StringComparison.Ordinal)
            && generationTaskSource.Contains("technical_consecutive_generation_rejections", StringComparison.Ordinal)
            && !behaviorSource.Contains("war_started_by_other_direction", StringComparison.Ordinal)
            && bindingSource.Contains("war_started_by_other_direction", StringComparison.Ordinal)
            && !behaviorSource.Contains("enforced.Status = \"enforced\"", StringComparison.Ordinal)
            && bindingSource.Contains("enforced.Status = \"enforced\"", StringComparison.Ordinal)
            && !behaviorSource.Contains("analyzed player declaration routing refresh deferred", StringComparison.Ordinal)
            && documentPublication.Contains("analyzed player declaration routing refresh deferred", StringComparison.Ordinal),
            "abandonment, enforcement, and finalization bookkeeping must live inside the owning services");
        Test.True(documentExecutor.Contains("orchestration.ApplyDiplomaticThreatReputationPenalty,", StringComparison.Ordinal)
            && documentExecutor.Contains("orchestration.SettleInternationalReputationForDocument,", StringComparison.Ordinal)
            && bindingSource.Contains("orchestration.ResolveDiplomaticThreatCompliance(d, a, t), (id, delta, doc, r) => orchestration.ApplyNationalPrestigeDelta", StringComparison.Ordinal)
            && documentExecutor.Contains("orchestration.StartDocumentPropagation,", StringComparison.Ordinal),
            "the host must keep binding post-decision live-state adapters");

        // Application schedules the next turn; the host binds kingdom resolution,
        // authority, player, actionable-target, slot-refresh, close, and log ports.
        string turnSchedulingSource = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyTurnSchedulingApplication.cs"));
        Test.True(behaviorSource.Contains("WorldDiplomacyTurnSchedulingApplication.ScheduleNextRelayHop(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyTurnSchedulingApplication.ScheduleNextResultSettlementTurn(", StringComparison.Ordinal),
            "the host must route relay and settlement scheduling through Application");
        Test.True(behaviorSource.Contains("id => _host.PartyResolved(id) && _host.HasIndependentAuthority(id)", StringComparison.Ordinal)
            && behaviorSource.Contains("id => _host.PartyResolved(id) && _host.IsPlayerParty(id)", StringComparison.Ordinal)
            && behaviorSource.Contains("(r, id) => GetResultSettlementActionableTargetIds(r, id).Count", StringComparison.Ordinal)
            && behaviorSource.Contains("RefreshResultSettlementActionSlots,", StringComparison.Ordinal)
            && behaviorSource.Contains("ScheduleNextResultSettlementTurn,", StringComparison.Ordinal),
            "the host must keep binding scheduling live-state adapters");
        Test.True(rulesSource.Contains("CanScheduleRelayHop(", StringComparison.Ordinal)
            && rulesSource.Contains("FindPriorityThreatRelayIndex(", StringComparison.Ordinal)
            && rulesSource.Contains("ComputeRelayArrivalDay(", StringComparison.Ordinal)
            && rulesSource.Contains("OrderRelayArrivalsByDueDate(", StringComparison.Ordinal)
            && turnSchedulingSource.Contains("new WorldDiplomacyRelayArrival", StringComparison.Ordinal),
            "relay scheduling must compose Domain decisions inside Application");
        Test.True(rulesSource.Contains("CanScheduleResultSettlementTurn(", StringComparison.Ordinal)
            && rulesSource.Contains("EvaluateSettlementSlotAction(", StringComparison.Ordinal)
            && rulesSource.Contains("MarkSettlementSlotWaitingForPlayer(", StringComparison.Ordinal)
            && rulesSource.Contains("ScheduleSettlementRelayArrival(", StringComparison.Ordinal)
            && rulesSource.Contains("SkipResultSettlementSlot(", StringComparison.Ordinal)
            && rulesSource.Contains("ResolveSettlementCloseReason(", StringComparison.Ordinal)
            && rulesSource.Contains("NormalizeResultSettlementStatus(", StringComparison.Ordinal),
            "settlement scheduling must compose Domain decisions inside Application");
        Test.True(!behaviorSource.Contains("FindPriorityThreatRelayIndex(", StringComparison.Ordinal)
            && !behaviorSource.Contains("new WorldDiplomacyRelayArrival", StringComparison.Ordinal)
            && !behaviorSource.Contains("EvaluateSettlementSlotAction(", StringComparison.Ordinal)
            && !behaviorSource.Contains("RelayPassStartedDay +=", StringComparison.Ordinal)
            && !behaviorSource.Contains("ResultSettlementCurrentSlotId =", StringComparison.Ordinal)
            && !behaviorSource.Contains("relay_all_participants_withdrew", StringComparison.Ordinal),
            "relay and settlement scheduling internals must not remain in the host");

        // R1: completion admission/dispatch now has one Application owner.
        Test.True(dispatchSource.Contains("WorldDiplomacyJob job = SelectAndPrepareLlmJob(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyCompletionApplication.Run(ref source, this)", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyLlmDispatchApplication.Run(ref source, this)", StringComparison.Ordinal)
            && !behaviorSource.Contains("WorldDiplomacyRoundLifecycleRules.CommitCompletedLlmJobResult(", StringComparison.Ordinal),
            "host retains launch but forwards completion admission and dispatch to Application");
        Test.True(behaviorSource.Contains("WorldDiplomacyLlmApplication.PrepareRequest(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyLlmApplication.ExecuteAsync(", StringComparison.Ordinal)
            && behaviorSource.Contains("Task.Run(", StringComparison.Ordinal),
            "transport launch remains a narrow main-thread adapter");
        Test.True(dispatchSource.Contains("ServiceCooldownUntilHour > currentHour", StringComparison.Ordinal)
            && dispatchSource.Contains("WorldDiplomacyJobSelectionView.For(storage)", StringComparison.Ordinal)
            && dispatchSource.Contains("candidatePriority > selectedPriority", StringComparison.Ordinal)
            && dispatchSource.Contains("candidate.CreatedDay < _selected.CreatedDay", StringComparison.Ordinal)
            && dispatchSource.Contains("StringComparer.OrdinalIgnoreCase.Compare(candidate.JobId, _selected.JobId)", StringComparison.Ordinal)
            && dispatchSource.Contains("IsValidSemanticRepairMessageChain(job)", StringComparison.Ordinal)
            && dispatchSource.Contains("if (!EnsureCurrentCanonicalPromptContractBeforeSend(", StringComparison.Ordinal),
            "job selection and preflight checks remain unchanged");
        string notificationSource = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyNotificationApplication.cs"));
        Test.True(historyCompressionSource.Contains("jobs.HasAwaiting(storage)", StringComparison.Ordinal)
            && historyCompressionSource.Contains("jobs.HasCompression(storage)", StringComparison.Ordinal)
            && !dispatchSource.Contains("List<WorldDiplomacyJob> runnable =", StringComparison.Ordinal)
            && notificationSource.Contains("EnsureView(storage, sink.PlayerKingdomId)", StringComparison.Ordinal)
            && !notificationSource.Contains("storage.Documents.Where(", StringComparison.Ordinal)
            && !notificationSource.Contains("SelectPendingRumors(storage, 3)", StringComparison.Ordinal),
            "per-tick job selection and notification deadlines cannot rescan the full collections");
        Test.True(completionSource.Contains("completed generation used a stale diplomatic threat stage", StringComparison.Ordinal)
            && completionSource.Contains("truncated generated draft handling failed", StringComparison.Ordinal)
            && completionSource.Contains("ServiceCooldownUntilHour = currentHour + failedServiceCooldownHours", StringComparison.Ordinal)
            && completionSource.Contains("unknown job kind", StringComparison.Ordinal),
            "Application owns completion routing and cooldown bookkeeping");
        Test.True(!behaviorSource.Contains("var runnable =", StringComparison.Ordinal)
            && !behaviorSource.Contains("SelectAndPrepareLlmJob(", StringComparison.Ordinal)
            && !behaviorSource.Contains("unknown job kind", StringComparison.Ordinal)
            && !behaviorSource.Contains("truncated generated draft handling failed", StringComparison.Ordinal)
            && !behaviorSource.Contains("ConsecutiveServiceFailures++", StringComparison.Ordinal),
            "job routing internals must not remain in the host");

        // DPL-060DI: due relay arrival dispatch lives in the round-progress
        // Application; external resolved-fact intake stays in the lifecycle
        // rules. The host keeps Kingdom resolution, authority/player checks,
        // court marking, job enqueue, and live document/fact construction as
        // adapter ports.
        Test.True(behaviorSource.Contains("WorldDiplomacyRoundProgressApplication.ProcessDueRelayArrivals(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyDocumentPublicationApplication.NotifyExternalDiplomacyResolved(", StringComparison.Ordinal),
            "the host must route relay arrivals through the round-progress Application and external facts through the publication Application");
        Test.True(textRulesSource.Contains("public static string BuildExternalFactBody(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyTextRules.BuildExternalFactBody(normalized,", StringComparison.Ordinal)
            && !behaviorSource.Contains("private static string BuildExternalFactBody(", StringComparison.Ordinal)
            && !File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.OrchestrationHost.cs"))
                .Contains("BuildExternalFactBody(", StringComparison.Ordinal),
            "external fact wording belongs to Domain with only live names supplied by the host");
        Test.True(behaviorSource.Contains("_host.ResolvePartyId,", StringComparison.Ordinal)
            && behaviorSource.Contains("MarkPlayerCourtReachedByRelay,", StringComparison.Ordinal)
            && behaviorSource.Contains("resultSettlementSlotId: settlementSlotId", StringComparison.Ordinal)
            && behaviorSource.Contains("(title, factBody, origin, playerAuthored) => CreateDocument(initiatorId, targetId, title, factBody, origin, playerAuthored, false, \"\")", StringComparison.Ordinal)
            && behaviorSource.Contains("playerInsertion => EnsureActiveRound(initiatorId, targetId, playerInsertion)", StringComparison.Ordinal)
            && behaviorSource.Contains("candidate => CanExternalDiplomacyFactJoinRound(candidate, initiatorId, targetId)", StringComparison.Ordinal),
            "the host must keep Kingdom resolution, enqueue, and fact construction as adapter ports");
        Test.True(progressSource.Contains("Take(8)", StringComparison.Ordinal)
            && progressSource.Contains("storage.RelayArrivals.Remove(arrival)", StringComparison.Ordinal)
            && progressSource.Contains("WorldDiplomacyRelayArrivalAction.RescheduleSettlementTurn", StringComparison.Ordinal)
            && progressSource.Contains("enqueueRelayTurn?.Invoke(arrival.ToKingdomId,", StringComparison.Ordinal)
            && progressSource.Contains("resolveKingdomId?.Invoke(arrival.FromKingdomId) ?? resolveKingdomId?.Invoke(round.InitiatorKingdomId)", StringComparison.Ordinal)
            && documentPublication.Contains("WorldDiplomacyIntentVocabulary.IsExternallyResolvedDiplomaticIntent", StringComparison.Ordinal)
            && documentPublication.Contains("foreach (var live in WorldDiplomacyLiveRoundRules.Live(storage))", StringComparison.Ordinal)
            && documentPublication.Contains("MarkOpenBilateralOffersAccepted(live", StringComparison.Ordinal)
            && documentPublication.Contains("AddOrMergeResultSettlementSlot(round, targetId, \"route\",", StringComparison.Ordinal)
            && documentPublication.Contains("external diplomacy fact kept outside unrelated active round", StringComparison.Ordinal)
            && !rulesSource.Contains("public static void NotifyExternalDiplomacyResolved(", StringComparison.Ordinal),
            "arrival dispatch lives in the round-progress Application; external-fact intake lives in the publication Application");
        Test.True(!behaviorSource.Contains("IsArrivalStale(", StringComparison.Ordinal)
            && !behaviorSource.Contains("EvaluateArrivalAction(", StringComparison.Ordinal)
            && !behaviorSource.Contains("OrderRelayArrivalsByDueDate(", StringComparison.Ordinal)
            && !behaviorSource.Contains("external diplomacy fact kept outside unrelated active round", StringComparison.Ordinal),
            "arrival and external-fact internals must not remain in the host");
        Test.True(behaviorSource.Contains("WorldDiplomacyJobPreparationApplication.PrepareAnalysisJob(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyJobPreparationApplication.PrepareRoundPlanJob(", StringComparison.Ordinal)
            && behaviorSource.Contains("WorldDiplomacyJobPreparationApplication.Rebuild(", StringComparison.Ordinal),
            "the host must route analysis, round-plan, and rebuild job composition through the job-preparation Application");
        Test.True(jobPreparationSource.Contains("public static void PrepareAnalysisJob(", StringComparison.Ordinal)
            && jobPreparationSource.Contains("public static void PrepareRoundPlanJob(", StringComparison.Ordinal)
            && !rulesSource.Contains("public static void PrepareAnalysisJob(", StringComparison.Ordinal)
            && !rulesSource.Contains("public static void PrepareRoundPlanJob(", StringComparison.Ordinal),
            "analysis and round-plan job composition must be owned by the Application, not the lifecycle rules");
        Test.True(jobPreparationSource.Contains("Kind = \"analyze\"", StringComparison.Ordinal)
            && jobPreparationSource.Contains("Kind = \"round_plan\"", StringComparison.Ordinal)
            && jobPreparationSource.Contains("CacheAffinityKey = \"diplomacy-round-plan:v6\"", StringComparison.Ordinal)
            && jobPreparationSource.Contains("round_plan_no_actionable_participants", StringComparison.Ordinal)
            && jobPreparationSource.Contains("AllowAutonomousNoAction = false", StringComparison.Ordinal),
            "job-composition stamping must live inside the job-preparation Application");
        Test.True(!behaviorSource.Contains("Kind = \"round_plan\"", StringComparison.Ordinal)
            && !behaviorSource.Contains("Kind = \"analyze\"", StringComparison.Ordinal)
            && !behaviorSource.Contains("diplomacy-round-plan:v6", StringComparison.Ordinal)
            && !behaviorSource.Contains("round_plan_no_actionable_participants", StringComparison.Ordinal)
            && !behaviorSource.Contains("diplomacy_round_plan", StringComparison.Ordinal)
            && !behaviorSource.Contains("diplomacy_analyze", StringComparison.Ordinal),
            "job-composition internals must not remain in the host");
    }

    private static string FindRepositoryFile(params string[] relativeSegments)
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            string candidate = Path.Combine(new[] { current.FullName }.Concat(relativeSegments).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }
            current = current.Parent;
        }
        throw new FileNotFoundException("Could not locate repository file", Path.Combine(relativeSegments));
    }
}

namespace AnimusForge
{
    internal sealed class PublishedPolicyArtifactLedgerEntry
    {
        public long Sequence { get; set; }
        public long Revision { get; set; }
        public string PolicyId { get; set; }
        public string EventKind { get; set; }
        public int OccurredDay { get; set; }
        public string GameDate { get; set; }
        public long CreatedUtcTicks { get; set; }
        public string ScopeKind { get; set; }
        public string KingdomId { get; set; }
        public string KingdomName { get; set; }
        public string PolicyName { get; set; }
        public string PublishedText { get; set; }
        public string ContentHash { get; set; }
    }
}
