using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class EventRetirementReplay
{
    internal static void Run()
    {
        var h=new FakeOrchestrationHost();
        var o=new WorldDiplomacyOrchestration(h,new WorldDiplomacyRuntimeState());
        var s=o.CurrentStorage;
        s.NationalPrestigeByKingdom["a"]=12;
        s.InternationalReputationByKingdom["a"]=23;
        var d=new WorldDiplomacyDocument { DocumentId="old", InternationalReputationEvaluationDelta=7 };
        s.Documents.Add(d);
        string before=JsonConvert.SerializeObject(s);
        var restored=JsonConvert.DeserializeObject<WorldDiplomacyStorage>(before)!;
        Test.True(restored.NationalPrestigeByKingdom["a"]==12 && restored.InternationalReputationByKingdom["a"]==23,
            "legacy numeric fields remain readable history");
        Test.True(typeof(WorldDiplomacyOrchestration).GetMethod("ApplyNationalPrestigeDelta") != null, "national prestige restored while unrelated retired mechanics stay removed");
        foreach (var removed in new[]{"SettleInternationalReputationForDocument","RecordDiplomaticThreatTargetDecisions","DeferUnresolvedRequiredThreatAction","IsNonRootAiRelayNoActionAllowed","CanUseResultSettlementTarget"})
            Test.True(typeof(WorldDiplomacyOrchestration).GetMethod(removed)==null,"retired business interface physically removed: "+removed);
        Test.True(WorldDiplomacyPresentationQueries.Standing(s,"a")=="" && WorldDiplomacyPresentationQueries.BuildImpactText(d)=="", "retired scores are not displayed as current effects");
        d.MechanicalResult="actual treaty receipt";
        Test.True(WorldDiplomacyPresentationQueries.BuildImpactText(d).Contains(d.MechanicalResult),"actual diplomatic outcomes remain visible");
        var w=new PromptWorldFixture { RejectRetiredScores=true, EnglishPrompts=SmokeLocalizationEnvironment.EnglishPackage };
        var po=new PromptOrch(w);
        string prompt=WorldDiplomacyPromptComposer.BuildGenerationPrompt(w,po,"a","b",null,false,null,false,"r",false,new(){"b","c"},false);
        Test.True(prompt.Contains("voice-a") && prompt.Contains("- b="),"real generation keeps ruler voice and world targets without reading global scores");
        string contract=WorldDiplomacyPromptContractRules.BuildDiplomaticDeclarationModeContract(DiplomacyPromptTexts.Get);
        string analysis=WorldDiplomacyPromptContractRules.BuildAnalysisModeContract(DiplomacyPromptTexts.Get);
        Test.True(!contract.Contains("\"international_reputation_delta\":") && !analysis.Contains("\"international_reputation_delta\":"),"real generation and semantic JSON examples no longer request numeric score output");
        Test.True(JObject.Parse(WorldDiplomacyPromptComposer.BuildFallbackAnalysisJson(null,"b"))["international_reputation_delta"]==null,"fallback does not manufacture an evaluation");
        Test.True(WorldDiplomacyPromptContractRules.BuildGenerationSystemPrompt("core",999,300).Contains("300"),"compatibility reader also includes upper-only declaration contract");
    }
}
