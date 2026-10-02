using AnimusForge;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;

static class Program
{
    static int checks;
    static void Check(bool c,string message) { if (!c) throw new Exception("FAIL "+message);checks++; }
    static void Call(string method, params object[] args)
    { Check(Recording.Method==method && Recording.Args.SequenceEqual(args),"exact method/arguments "+method); }
    static void Main()
    {
        try { Run(); }
        catch(Exception ex) { Console.Error.WriteLine(ex);Environment.ExitCode=1; }
    }
    static void Run()
    {
        var lifecycleSteps = new List<string>();
        WorldDiplomacyBehavior.Instance = new WorldDiplomacyBehavior { Orchestration = new RecordingOrchestration(lifecycleSteps) };
        DiplomacyModuleServices.World.OnLifecycle(WorldDiplomacyLifecycleEvent.NewGame);
        Check(lifecycleSteps.SequenceEqual(new[] { "ResetStorageForNewGame", "EnsureScheduleInitialized" }), "module enters new-game Application");
        Call("lifecycle-reset", "new-game");
        lifecycleSteps.Clear();
        DiplomacyModuleServices.World.OnLifecycle(WorldDiplomacyLifecycleEvent.Loaded);
        Check(lifecycleSteps.SequenceEqual(new[] { "RecoverUnsettledAiInternationalReputation", "RecoverPlayerCourtReceiptsFromKnowledge", "EnsureScheduleInitialized", "ReconcileActiveDiplomacyAfterLoad" }), "module enters loaded Application recovery");
        Call("lifecycle-reset", "game-loaded");
        WorldDiplomacyBehavior.Instance = null;
        var h=new Hero{StringId="h"};var other=new Hero{StringId="other"};var ch=new CharacterObject{HeroObject=other};
        Campaign.Current=new Campaign();var index=Campaign.Current.CampaignObjectManager;
        index.Objects["h"]=h;index.Objects["other"]=other;
        var eligibility=new (string,Func<Hero,CharacterObject,bool>)[] {
            ("inject",DiplomacyConversationBridge.CanInjectDiplomacyRuleForExternal),
            ("action",DiplomacyConversationBridge.CanUseDiplomacyActionPostprocessForExternal),
            ("full",DiplomacyConversationBridge.CanUseFullDiplomacyActionPostprocessForExternal),
            ("war",DiplomacyConversationBridge.CanUseNpcSovereignDeclareWarPostprocessForExternal),
            ("peace",DiplomacyConversationBridge.CanUseIndependentClanPeaceForExternal) };
        foreach(var (method,invoke) in eligibility)
        foreach(bool result in new[]{true,false})
        {
            Recording.Result=result;
            var owner=method=="peace"?"peace":"eligibility";
            Recording.PeaceCaptures=0;
            Check(invoke(h,ch)==result,"return "+method);Call(owner,h);
            Check(invoke(null,ch)==result,"character return "+method);Call(owner,other);
            Check(invoke(null,new CharacterObject())==result,"nonhero return "+method);Call(owner,(object)null);
            Check(invoke(null,null)==result,"null return "+method);Call(owner,(object)null);
            if(method=="peace") Check(Recording.PeaceCaptures==(result?16:4),"peace stage short circuit");
        }
        Check(index.Lookups==20,"one indexed lookup per live target; null has no lookup");
        var settlement=new TaleWorlds.CampaignSystem.Settlements.Settlement{StringId="s"};
        index.Objects["s"]=settlement;
        Check(ReferenceEquals(DiplomacyIdentityResolver.Settlement("s"),settlement),"settlement stable ID uses campaign index");
        Check(DiplomacyIdentityResolver.Settlement("")==null,"empty settlement ID skips campaign index");
        index.Throw=true;
        Check(DiplomacyIdentityResolver.Settlement("s")==null,"settlement index failure is isolated");
        index.Throw=false;
        var replacement=new Hero{StringId="h"};index.Objects["h"]=replacement;
        DiplomacyConversationBridge.CanInjectDiplomacyRuleForExternal(h);Call("eligibility",replacement);
        index.Objects.Remove("h");DiplomacyConversationBridge.CanInjectDiplomacyRuleForExternal(h);Call("eligibility",(object)null);
        index.Objects["h"]=h;index.Throw=true;
        DiplomacyConversationBridge.CanInjectDiplomacyRuleForExternal(h);Call("eligibility",(object)null);
        index.Throw=false;Campaign.Current=null;
        string missing="tag";DiplomacyConversationBridge.ProcessDiplomacyTagsDispatch(h,ref missing);
        Check(missing=="tag","missing campaign does not execute stale hero");
        Campaign.Current=new Campaign{CampaignObjectManager=index};
        string postprocessContext=DiplomacyConversationBridge.BuildDiplomacyPostprocessContext(h);
        Check(postprocessContext.Contains("你的王国ID：h（h）")&&postprocessContext.Contains("[ACTION:DIPLOMACY:MAKE_PEACE"),"context result");Call("context",h);
        Check(!postprocessContext.Contains("old = Old"),"eliminated kingdom omitted");
        var independentContext=new ContextReplaySource(1);
        string independentText=DiplomacyPostprocessContextApplication.Build(ref independentContext);
        Check(independentText.Contains("独立家族议和运行时事实")&&independentText.Contains("定居点数：2"),"independent peace context");
        Check(independentContext.KingdomCaptures==0,"independent branch skips kingdom table");
        var warOnlyContext=new ContextReplaySource(2);
        string warOnlyText=DiplomacyPostprocessContextApplication.Build(ref warOnlyContext);
        Check(warOnlyText.Contains("DECLARE_WAR")&&!warOnlyText.Contains("MAKE_PEACE"),"war-only context excludes full tags");
        Check(warOnlyContext.WarCalls==0&&warOnlyContext.TributeCalls==0,"war-only context skips tribute work");
        var fullContext=new ContextReplaySource(3);
        string fullText=DiplomacyPostprocessContextApplication.Build(ref fullContext);
        Check(fullText.Contains("国家吞并约束")&&fullText.Contains("MAKE_PEACE")&&fullText.Contains("auto贡金：n付12/天，p付34/天"),"full context and tribute line");
        Check(fullContext.WarCalls==1&&fullContext.TributeCalls==2,"both tribute directions queried once");
        var blockedContext=new ContextReplaySource(4);
        Check(DiplomacyPostprocessContextApplication.Build(ref blockedContext)==""&&blockedContext.KingdomCaptures==0,"ineligible context does not scan kingdoms");
        foreach(var result in new[]{true,false})
        {
            Recording.Result=result;
            Check(DiplomacyConversationBridge.IsIndependentClanPeacePostprocessTag(" [action:diplomacy:independent_clan_peace] "),"canonical tag");
            Check(!DiplomacyConversationBridge.IsIndependentClanPeacePostprocessTag(" exact "),"reject unrelated tag");
            Check(DiplomacyConversationBridge.CanDiscussWorldDiplomacyForExternal(h)==result,"discussion result");Call("known",h,"kingdom");
            Check(DiplomacyConversationBridge.TryBuildProactiveDiscussionForExternal(h,out var key,out var fact,out var urgency)==result,"proactive result");
            Call("proactive",h);Check(key=="key"&&fact=="fact"&&urgency==0.75f,"all proactive outputs");
        }
        static DiplomacyConversationEligibilitySnapshot Snapshot(bool dead=false,bool same=false,bool playerRuler=true,
            bool npcIsPlayer=false,bool npcEliminated=false) =>
            new(true,npcIsPlayer,dead,true,npcEliminated,true,true,false,!same,playerRuler);
        Check(DiplomacyConversationEligibilityApplication.CanInject(Snapshot()),"eligible speaker");
        Check(DiplomacyConversationEligibilityApplication.CanUseFull(Snapshot()),"full diplomacy eligible");
        Check(!DiplomacyConversationEligibilityApplication.CanUseNpcDeclareWar(Snapshot(dead:true)),"dead speaker blocks war");
        Check(DiplomacyConversationEligibilityApplication.CanUseFull(Snapshot(dead:true)),"full gate preserves historical dead-speaker behavior");
        Check(!DiplomacyConversationEligibilityApplication.CanUseFull(Snapshot(same:true)),"same kingdom blocks full diplomacy");
        Check(DiplomacyConversationEligibilityApplication.CanUseAction(Snapshot(same:true)),"war path remains when full path is blocked");
        Check(!DiplomacyConversationEligibilityApplication.CanUseFull(Snapshot(playerRuler:false)),"nonruler player blocks full diplomacy");
        Check(!DiplomacyConversationEligibilityApplication.CanInject(Snapshot(npcIsPlayer:true)),"player cannot be injected");
        Check(!DiplomacyConversationEligibilityApplication.CanUseAction(Snapshot(npcEliminated:true)),"eliminated speaker kingdom blocks actions");
        Check(!DiplomacyConversationEligibilityApplication.CanUseAction(default),"missing speaker blocks actions");
        Check(DiplomacyIndependentPeaceApplication.IsEligible(new DiplomacyIndependentPeacePlayerSnapshot(true,true,true,false,false,false,true)),"independent player eligible");
        Check(!DiplomacyIndependentPeaceApplication.IsEligible(new DiplomacyIndependentPeacePlayerSnapshot(true,true,true,false,true,false,true)),"kingdom player blocked");
        Check(!DiplomacyIndependentPeaceApplication.IsEligible(new DiplomacyIndependentPeaceSpeakerSnapshot(true,false,true,true,false,false,false,false)),"dead peace speaker blocked");
        Check(!DiplomacyIndependentPeaceApplication.IsEligible(new DiplomacyIndependentPeaceSpeakerSnapshot(true,false,false,true,false,false,true,false)),"bandit peace speaker blocked");
        Check(!DiplomacyIndependentPeaceApplication.IsEligible(new DiplomacyIndependentPeaceTargetSnapshot(true,false)),"ordinary lord blocked");
        Check(DiplomacyIndependentPeaceApplication.IsEligible(new DiplomacyIndependentPeaceWarSnapshot(true,false,false,true,false)),"ordinary war negotiable");
        Check(!DiplomacyIndependentPeaceApplication.IsEligible(new DiplomacyIndependentPeaceWarSnapshot(true,false,false,true,true)),"constant war blocked");
        var quietPeace=new QuietPeaceSource();
        for(int i=0;i<100;i++) Check(DiplomacyIndependentPeaceApplication.CanUse(ref quietPeace),"peace source warmup");
        long peaceBytes=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<10000;i++) DiplomacyIndependentPeaceApplication.CanUse(ref quietPeace);
        Check(GC.GetAllocatedBytesForCurrentThread()==peaceBytes,"10000 independent-peace Application decisions allocate zero bytes");
        string text="[ACTION:DIPLOMACY:MAKE_PEACE]";
        DiplomacyConversationBridge.ProcessDiplomacyTagsDispatch(h,ref text);Call("execute",h,"peace","");Check(text=="confirmed","ref result preserved");
        var failure=new InvalidOperationException("owner error");Recording.Failure=failure;
        text="[ACTION:DIPLOMACY:MAKE_PEACE]";
        DiplomacyConversationBridge.ProcessDiplomacyTagsDispatch(h,ref text);
        Check(text==""&&Recording.LastTagLog.Contains("[Tag Error] action=MAKE_PEACE"),"tag effect failure consumed and logged");
        Recording.Failure=null;
        Recording.TagActions.Clear();
        text="  start [action:diplomacy:make_trade:p:n] middle [ACTION:DIPLOMACY:CANCEL_TRADE:n:p] end  ";
        DiplomacyConversationBridge.ProcessDiplomacyTagsDispatch(h,ref text);
        Check(text=="start confirmed middle confirmed end","multiple tags preserve text order and trim");
        Check(Recording.TagActions.SequenceEqual(new[]{"trade:p:n","cancel-trade:n:p"}),"multiple actions execute in textual order");
        Recording.TagActions.Clear();
        text="plain diplomacy text [ACTION:DIPLOMACY:UNKNOWN]";
        DiplomacyConversationBridge.ProcessDiplomacyTagsDispatch(h,ref text);
        Check(text=="plain diplomacy text"&&Recording.LastTagLog.Contains("Unknown action: UNKNOWN")
              &&Recording.TagActions.Count==0,"unknown tag is removed without effect");
        text="plain text";
        DiplomacyConversationBridge.ProcessDiplomacyTagsDispatch(h,ref text);
        Check(text=="plain text"&&Recording.TagActions.Count==0,"ordinary text avoids tag execution");
        var payer=new Kingdom{StringId="payer"};var receiver=new Kingdom{StringId="receiver"};index.Objects["payer"]=payer;index.Objects["receiver"]=receiver;
        Recording.Result=true;
        Check(DiplomacyConversationBridge.TryBuildTributePowerContext(payer,receiver,out var tribute),"tribute result");Call("tribute",payer,receiver);
        Check(tribute.CalculatedTribute==10&&tribute.ScoreDelta==1&&tribute.AppliedTributeRatio==0.05f,"tribute fields");
        Recording.Result=false;
        Check(!DiplomacyConversationBridge.TryBuildTributePowerContext(payer,receiver,out var missingTribute),"unavailable tribute source");Call("tribute",payer,receiver);
        Check(missingTribute.CalculatedTribute==0&&missingTribute.ScorePayer==0f,"failed tribute clears output");
        Recording.Result=true;
        var highWarDiff=DiplomacyTributePowerApplication.Calculate(new DiplomacyTributePowerSnapshot(1,2,3,4,0,100,1000));
        Check(highWarDiff.WarProgressDifference==100&&highWarDiff.RawTributeRatio==0.2f&&highWarDiff.AppliedTributeRatio==0.10f&&highWarDiff.CalculatedTribute==30,"tribute high-war ratio tier");
        var lowRatio=DiplomacyTributePowerApplication.Calculate(new DiplomacyTributePowerSnapshot(1.7f,2,3,4,0,100,1000));
        Check(lowRatio.AppliedTributeRatio==0f&&lowRatio.CalculatedTribute==0,"tribute low ratio tier");
        var nonpositive=DiplomacyTributePowerApplication.Calculate(new DiplomacyTributePowerSnapshot(1,-2,3,4,0,100,1000));
        Check(nonpositive.RawTributeRatio==1f&&nonpositive.AppliedTributeRatio==0.15f,"nonpositive receiver score uses threshold");
        var thresholdWarDiff=DiplomacyTributePowerApplication.Calculate(new DiplomacyTributePowerSnapshot(1,2,3,4,0,75,1000));
        Check(thresholdWarDiff.AppliedTributeRatio==0.10f,"war difference of 75 enters high-war tier");
        var read=new WorldDiplomacyDocumentReadCommandAdapter();
        Check(read.MarkRead(" ").Status==WorldDiplomacyDocumentReadStatus.InvalidDocumentId,"invalid id");
        WorldDiplomacyBehavior.Available=false;Check(read.MarkRead("diplomacy:x").Status==WorldDiplomacyDocumentReadStatus.Unavailable,"missing owner");
        WorldDiplomacyBehavior.Available=true;WorldDiplomacyBehavior.Applied=false;Check(read.MarkRead(" Diplomacy:x ").Status==WorldDiplomacyDocumentReadStatus.NotFound,"missing record");Call("read","x");
        WorldDiplomacyBehavior.Applied=true;Check(read.MarkRead("x").IsApplied,"applied read");
        var revision=new WorldDiplomacyTimelineRevisionQueryAdapter();Check(!revision.Query().IsAvailable,"unavailable revision");
        var first=new WorldDiplomacyBehavior{Revision=41};WorldDiplomacyBehavior.Instance=first;Check(revision.Query().Revision==41,"live revision");
        var next=new WorldDiplomacyBehavior{Revision=57};WorldDiplomacyBehavior.Instance=next;
        DiplomacyConversationBridge.ApplyExternalPrestigeDelta("kingdom", -10, "civil war");Call("prestige", "kingdom", -10, "civil war");Check(revision.Query().Revision==57,"owner replacement");
        DiplomacyModuleServices.World.OnEngineTick();Check(first.Ticks==0&&next.Ticks==1,"tick resolves current owner");
        for(int i=0;i<100;i++)DiplomacyModuleServices.World.OnEngineTick();
        long bytes=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10000;i++)DiplomacyModuleServices.World.OnEngineTick();
        Check(GC.GetAllocatedBytesForCurrentThread()==bytes,"10000 tick forwards allocate zero bytes");
        WorldDiplomacyBehavior.Instance=null;DiplomacyModuleServices.World.OnEngineTick();Check(next.Ticks==10101,"null owner tick no-op");
        var enabledTick=new TickReplaySource { Owner=true, Enabled=true, Steps=new() };
        WorldDiplomacyTickApplication.Run(ref enabledTick, new RecordingOrchestration(enabledTick.Steps));
        Check(enabledTick.Steps.SequenceEqual(new[]{"popup","clear","completed","compress","start","publish"}),"enabled tick order");
        var disabledTick=new TickReplaySource { Owner=true, Enabled=false, Steps=new() };
        WorldDiplomacyTickApplication.Run(ref disabledTick, new RecordingOrchestration(disabledTick.Steps));
        Check(disabledTick.Steps.SequenceEqual(new[]{"popup","disable","completed"}),"disabled tick drains completion after one cleanup");
        var disabledAgain=new TickReplaySource { Owner=true, Enabled=false, Disabled=true, Steps=new() };
        WorldDiplomacyTickApplication.Run(ref disabledAgain, new RecordingOrchestration(disabledAgain.Steps));
        Check(disabledAgain.Steps.SequenceEqual(new[]{"popup","completed"}),"disabled tick does not repeat cleanup");
        var noOwner=new TickReplaySource { Steps=new() };
        WorldDiplomacyTickApplication.Run(ref noOwner, null);
        Check(noOwner.Steps.Count==0,"missing tick owner has no effects");
        var documents=new WorldDiplomacyTimelineDocumentQueryAdapter();Check(documents.Query(-7).IsAvailable,"document availability");Call("documents",-7);
        var presentation=new Presentation();WorldDiplomacyBehavior.Port=presentation;Check(ReferenceEquals(DiplomacyModuleServices.World.Presentation,presentation),"current presentation owner");
        WorldDiplomacyBehavior.Port=null;Check(DiplomacyModuleServices.World.Presentation==null,"presentation not cached");
        var policy=DiplomacyModuleServices.Policy;
        Check(policy.BuildSnapshot("k")=="snapshot:k","policy snapshot");Call("snapshot","k");
        var signal=new WorldDiplomacyPolicySignalSnapshot("s","p","kingdom","name","summary","i","issuer","t","target","effect",2);
        WorldDiplomacyPolicyContext.Signals.Add(signal);var signals=policy.GetForeignPolicySignals();Call("signals");
        WorldDiplomacyPolicyContext.Signals.Clear();Check(signals.Count==1&&ReferenceEquals(signals[0],signal),"detached snapshot");
        Check(typeof(WorldDiplomacyPolicySignalSnapshot).GetProperties().All(p=>p.SetMethod==null),"immutable snapshot values");
        try { ((IList<WorldDiplomacyPolicySignalSnapshot>)signals).Clear();Check(false,"mutable list"); }catch(NotSupportedException) {checks++;}
        Check(policy.IsForeignPolicySignalActive("p","o","a"),"policy active");Call("active","p","o","a");
        Check(policy.GetPublishedPolicyHistoryLedgerId()=="ledger","ledger id");Call("ledger");
        Check(policy.GetPublishedPolicyHistoryCurrentSequence()==17,"sequence");Call("sequence");
        Check(policy.GetPublishedPolicyHistoryCurrentRevision()==23,"revision");Call("revision");
        Check(ReferenceEquals(policy.GetPublishedPolicyHistoryArtifacts(100,8),WorldDiplomacyPolicyContext.Artifacts),"artifact list preserved");Call("artifacts",100L,8);
        Check(policy.TryAcknowledgePublishedPolicyHistoryThrough(91),"ack result");Call("ack",91L);policy.Clear();Call("clear");
        Recording.Failure=failure;try { policy.BuildSnapshot("k");Check(false,"policy failure swallowed"); }catch(InvalidOperationException ex) {Check(ReferenceEquals(ex,failure),"policy failure keeps existing boundary");}Recording.Failure=null;
        PoliticalBoundaryReplay.Run(Check);
        Console.WriteLine($"PASS {checks} source-linked diplomacy port assertions; current owner lookup, return/ref/out, unavailable lifecycle, immutable policy values and zero-allocation tick routing.");
        Console.WriteLine("NOT TESTED: game engine implementation, actual thread scheduling, LIVE/SAVE acceptance.");
    }
}

internal struct QuietPeaceSource : IDiplomacyIndependentPeaceSource
{
    public DiplomacyIndependentPeacePlayerSnapshot CapturePlayer() => new(true,true,true,false,false,false,true);
    public DiplomacyIndependentPeaceSpeakerSnapshot CaptureSpeaker() => new(true,false,false,true,false,false,false,false);
    public DiplomacyIndependentPeaceTargetSnapshot CaptureTarget() => new(true,true);
    public DiplomacyIndependentPeaceWarSnapshot CaptureWar() => new(true,false,false,true,false);
}

internal struct TickReplaySource : IWorldDiplomacyTickSource
{
    internal bool Owner;
    internal bool Enabled;
    internal bool Disabled;
    internal List<string> Steps;
    public bool HasOwner => Owner;
    public bool IsEnabled => Enabled;
    public bool DisabledStateApplied => Disabled;
    public void ProcessComposePopup() => Steps.Add("popup");
    public void ClearDisabledState() => Steps.Add("clear");
}

internal sealed class RecordingOrchestration : IWorldDiplomacyOrchestration
{
    public int ApplyNationalPrestigeDelta(string kingdomId, int delta, WorldDiplomacyDocument document, string reason) => delta;
    private readonly List<string> steps;
    internal RecordingOrchestration(List<string> steps) { this.steps = steps; }
    public void ResetStorageForNewGame(bool initialPeacePending) => steps.Add("ResetStorageForNewGame");
    public void EnsureScheduleInitialized() => steps.Add("EnsureScheduleInitialized");
    public void RecoverUnsettledAiInternationalReputation() => steps.Add("RecoverUnsettledAiInternationalReputation");
    public void RecoverPlayerCourtReceiptsFromKnowledge() => steps.Add("RecoverPlayerCourtReceiptsFromKnowledge");
    public void ReconcileActiveDiplomacyAfterLoad() => steps.Add("ReconcileActiveDiplomacyAfterLoad");
    public void HandleDisabledState() => steps.Add("disable");
    public void ProcessCompletedJobs() => steps.Add("completed");
    public void TryScheduleTokenCompression() => steps.Add("compress");
    public void TryStartNextLlmJob() => steps.Add("start");
    public void PollNotifications() => steps.Add("publish");
    public void TryApplyInitialNewGamePeace() { }
    public void NormalizeStorage(bool allowWorldValidation) { }
    public void ReconcileAllNationalPrestigeVassalRelations() { }
    public void RetryDeferredCanonicalHistoryEntries() { }
    public void RetryDiplomaticThreatDomesticPenalties() { }
    public void RetryDiplomaticThreatComplianceConsequences() { }
    public void RetryDiplomaticThreatHistoryResults() { }
    public void RefreshRoundIntervalScheduleIfNeeded() { }
    public void RecalculatePendingPropagationIfNeeded() { }
    public void AnchorInternationalReputationNaturalChangeDays() { }
    public void ProcessInternationalReputationNaturalChange() { }
    public void RefreshPolicyDiplomacySignals() { }
    public void RetryDeferredDocumentPropagation() { }
    public void ProcessPropagationArrivals() { }
    public void ProcessRelayArrivals() { }
    public void RetryDeferredRoundProgress() { }
    public void ProcessRoundLifecycle() { }
    public void TrySchedulePolicyTriggeredRound() { }
    public void TryScheduleNormalRound() { }
    public void EnsureActiveWarLedgers() { }
    public void TrimRecentBattleFacts() { }
    public void DecayWarPressure() { }
}

internal struct ContextReplaySource : IDiplomacyPostprocessContextSource
{
    private readonly int mode;
    internal int KingdomCaptures;
    internal int WarCalls;
    internal int TributeCalls;
    internal ContextReplaySource(int mode) { this.mode=mode;KingdomCaptures=0;WarCalls=0;TributeCalls=0; }
    public bool HasSpeaker => true;
    public bool TryCaptureIndependentPeace(out DiplomacyIndependentPeaceContextSnapshot snapshot)
    { snapshot=new("Clan",2,"Target");return mode==1; }
    public DiplomacyConversationEligibilitySnapshot CaptureEligibility() => mode==4 ? default :
        new(true,false,false,true,false,true,mode==3,false,true,mode==3);
    public DiplomacyPostprocessKingdomSnapshot CaptureKingdoms()
    {
        KingdomCaptures++;
        return new(true,"n","Npc",mode==3,false,"p","Player",mode==3,true,
            new[]{new DiplomacyKingdomSummary("n","Npc",false)});
    }
    public string GetAnnexationHint() => "constraint";
    public bool ArePlayerAndNpcAtWar() { WarCalls++;return true; }
    public int CalculateDailyTribute(bool npcPays)
    { TributeCalls++;return npcPays?12:34; }
    public void LogFailure(string message) => throw new Exception("context failure: "+message);
}
