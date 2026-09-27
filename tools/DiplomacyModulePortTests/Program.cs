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
            Check(invoke(h,ch)==result,"return "+method);Call(method,h);
            Check(invoke(null,ch)==result,"character return "+method);Call(method,other);
            Check(invoke(null,new CharacterObject())==result,"nonhero return "+method);Call(method,(object)null);
            Check(invoke(null,null)==result,"null return "+method);Call(method,(object)null);
        }
        Check(index.Lookups==20,"one indexed lookup per live target; null has no lookup");
        var replacement=new Hero{StringId="h"};index.Objects["h"]=replacement;
        DiplomacyConversationBridge.CanInjectDiplomacyRuleForExternal(h);Call("inject",replacement);
        index.Objects.Remove("h");DiplomacyConversationBridge.CanInjectDiplomacyRuleForExternal(h);Call("inject",(object)null);
        index.Objects["h"]=h;index.Throw=true;
        DiplomacyConversationBridge.CanInjectDiplomacyRuleForExternal(h);Call("inject",(object)null);
        index.Throw=false;Campaign.Current=null;
        string missing="tag";DiplomacyConversationBridge.ProcessDiplomacyTagsDispatch(h,ref missing);
        Call("execute",null,"tag");Check(missing=="tag","missing campaign does not execute stale hero");
        Campaign.Current=new Campaign{CampaignObjectManager=index};
        Check(DiplomacyConversationBridge.BuildDiplomacyPostprocessContext(h)=="h","context result");Call("context",h);
        foreach(var result in new[]{true,false})
        {
            Recording.Result=result;
            Check(DiplomacyConversationBridge.IsIndependentClanPeacePostprocessTag(" exact ")==result,"tag result");Call("tag"," exact ");
            Check(DiplomacyConversationBridge.CanDiscussWorldDiplomacyForExternal(h)==result,"discussion result");Call("known",h,"kingdom");
            Check(DiplomacyConversationBridge.TryBuildProactiveDiscussionForExternal(h,out var key,out var fact,out var urgency)==result,"proactive result");
            Call("proactive",h);Check(key=="key"&&fact=="fact"&&urgency==0.75f,"all proactive outputs");
        }
        string text="[ACTION:DIPLOMACY:MAKE_PEACE]";
        DiplomacyConversationBridge.ProcessDiplomacyTagsDispatch(h,ref text);Call("execute",h,"[ACTION:DIPLOMACY:MAKE_PEACE]");Check(text=="confirmed:[ACTION:DIPLOMACY:MAKE_PEACE]","ref result preserved");
        var failure=new InvalidOperationException("owner error");Recording.Failure=failure;
        try { DiplomacyConversationBridge.ProcessDiplomacyTagsDispatch(h,ref text);Check(false,"owner exception must propagate"); }
        catch(InvalidOperationException ex) { Check(ReferenceEquals(ex,failure),"original exception instance"); }
        Recording.Failure=null;
        var payer=new Kingdom{StringId="payer"};var receiver=new Kingdom{StringId="receiver"};index.Objects["payer"]=payer;index.Objects["receiver"]=receiver;
        Recording.Result=true;
        Check(DiplomacyConversationBridge.TryBuildTributePowerContext(payer,receiver,out var tribute),"tribute result");Call("tribute",payer,receiver);
        Check(tribute.CalculatedTribute==11&&tribute.ScoreDelta==1&&tribute.AppliedTributeRatio==9,"tribute fields");
        var read=new WorldDiplomacyDocumentReadCommandAdapter();
        Check(read.MarkRead(" ").Status==WorldDiplomacyDocumentReadStatus.InvalidDocumentId,"invalid id");
        WorldDiplomacyBehavior.Available=false;Check(read.MarkRead("diplomacy:x").Status==WorldDiplomacyDocumentReadStatus.Unavailable,"missing owner");
        WorldDiplomacyBehavior.Available=true;WorldDiplomacyBehavior.Applied=false;Check(read.MarkRead(" Diplomacy:x ").Status==WorldDiplomacyDocumentReadStatus.NotFound,"missing record");Call("read","x");
        WorldDiplomacyBehavior.Applied=true;Check(read.MarkRead("x").IsApplied,"applied read");
        var revision=new WorldDiplomacyTimelineRevisionQueryAdapter();Check(!revision.Query().IsAvailable,"unavailable revision");
        var first=new WorldDiplomacyBehavior{Revision=41};WorldDiplomacyBehavior.Instance=first;Check(revision.Query().Revision==41,"live revision");
        var next=new WorldDiplomacyBehavior{Revision=57};WorldDiplomacyBehavior.Instance=next;Check(revision.Query().Revision==57,"owner replacement");
        DiplomacyModuleServices.World.OnEngineTick();Check(first.Ticks==0&&next.Ticks==1,"tick resolves current owner");
        for(int i=0;i<100;i++)DiplomacyModuleServices.World.OnEngineTick();
        long bytes=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10000;i++)DiplomacyModuleServices.World.OnEngineTick();
        Check(GC.GetAllocatedBytesForCurrentThread()==bytes,"10000 tick forwards allocate zero bytes");
        WorldDiplomacyBehavior.Instance=null;DiplomacyModuleServices.World.OnEngineTick();Check(next.Ticks==10101,"null owner tick no-op");
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
        Console.WriteLine($"PASS {checks} source-linked diplomacy port assertions; current owner lookup, return/ref/out, unavailable lifecycle, immutable policy values and zero-allocation tick routing.");
        Console.WriteLine("NOT TESTED: game engine implementation, actual thread scheduling, LIVE/SAVE acceptance.");
    }
}
