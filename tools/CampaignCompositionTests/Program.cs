using System;
using System.Linq;
using System.Reflection;
using AnimusForge;
using AnimusForge.Api.V1;
using AnimusForge.PolicyEffects;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;

internal static class Program
{
    private static int checks;
    private static void Check(bool ok, string name)
    {
        if (!ok) throw new Exception("FAIL " + name);
        checks++;
    }
    private static string Capture(StubSubModule module, Action<CampaignGameStarter> setup = null)
    {
        HostProbe.Reset();
        var starter = new CampaignGameStarter();
        setup?.Invoke(starter);
        string error = "none";
        try { module.Run(starter); }
        catch (InvalidOperationException ex) { error = ex.Message; }
        return string.Join("|", HostProbe.Events) + "|error:" + error
            + "|behaviors:" + string.Join(",", starter.Behaviors.Select(x => x.GetType().Name))
            + "|models:" + string.Join(",", starter.ModelList.Select(x => x.GetType().Name));
    }
    private static void Compare(string name, Action<CampaignGameStarter> setup = null)
        => Check(Capture(new OriginalSubModule(), setup) == Capture(new CurrentSubModule(), setup), name);
    private static GameModel[] SeedCustomModels(CampaignGameStarter starter)
    {
        GameModel[] last = { new CustomMobilePartyFoodConsumptionModel(), new CustomMobilePartyAIModel(),
            new CustomSettlementAccessModel(), new CustomSettlementLoyaltyModel() };
        starter.ModelList.AddRange(new GameModel[] { new DefaultMobilePartyFoodConsumptionModel(),
            new DefaultMobilePartyAIModel(), new DefaultSettlementAccessModel(), new DefaultSettlementLoyaltyModel() });
        starter.ModelList.AddRange(last);
        starter.ModelList.AddRange(new GameModel[] { new CourierFoodConsumptionModel(new DefaultMobilePartyFoodConsumptionModel()),
            new CourierMobilePartyAIModel(new DefaultMobilePartyAIModel()),
            new AnimusForgeSettlementAccessModel(new DefaultSettlementAccessModel()),
            new AnimusForgeSettlementLoyaltyModel(new DefaultSettlementLoyaltyModel()) });
        return last;
    }
    private static void Main()
    {
        try { Run(); }
        catch (Exception ex) { Console.Error.WriteLine(ex); Environment.ExitCode = 1; }
    }
    private static void Run()
    {
        Compare("pre-init Campaign parity");
        HostProbe.Reset(); var host = new CurrentSubModule();
        host.Run(null); host.Run(new OtherStarter());
        CampaignModelComposition.Register(null);
        Check(HostProbe.Events.Count == 0, "null/non-Campaign no side effects");
        Check(AfApi.GetSnapshot().State == AfFrameworkState.NotInitialized, "registration does not initialize catalog or claim save ready");
        var first = new CampaignGameStarter(); host.Run(first);
        Check(first.Behaviors.Select(x=>x.GetType().Name).SequenceEqual(Expected.Behaviors), "all 36 behaviors in pinned order");
        Check(first.ModelList.Count==4 && HostProbe.Events.Take(8).All(x=>!x.StartsWith("construct:")), "4 models before behavior constructors");
        var second = new CampaignGameStarter(); host.Run(second);
        Check(first.Behaviors.Zip(second.Behaviors).All(pair=>!ReferenceEquals(pair.First,pair.Second)), "separate Campaign owners");
        Check(first.ModelList.Zip(second.ModelList).All(pair=>!ReferenceEquals(pair.First,pair.Second)), "separate Campaign wrappers");
        host.Run(first);
        Check(first.Behaviors.Count==72 && first.ModelList.Count==8, "no invented callback deduplication");
        Check(first.Behaviors.Take(36).Zip(first.Behaviors.Skip(36)).All(pair=>!ReferenceEquals(pair.First,pair.Second)), "repeated callback never caches behavior");
        var custom = new CampaignGameStarter(); var last = SeedCustomModels(custom); host.Run(custom);
        Check(custom.ModelList.Skip(12).Cast<IWrapped>().Select(x=>x.Inner).SequenceEqual(last), "last non-AF inners retained by reference");
        Compare("custom/AF-wrapper exclusion parity", s=>SeedCustomModels(s));
        Compare("model enumeration failure continues", s=>s.ThrowModelRead=true);
        for(int i=0;i<4;i++) { int slot=i; Compare("model Add failure "+i, s=>s.FailModel=slot); }
        foreach(string wrapper in new[]{"CourierFoodConsumptionModel","CourierMobilePartyAIModel","AnimusForgeSettlementAccessModel","AnimusForgeSettlementLoyaltyModel"})
            Compare("wrapper constructor failure "+wrapper, s=>HostProbe.FailWrapper=wrapper);
        foreach(int slot in new[]{0,1,3,20,35})
        {
            Compare("behavior constructor failure "+slot, s=>HostProbe.FailConstructor=slot);
            Compare("behavior Add failure "+slot, s=>s.FailBehavior=slot);
        }
        // Directory state must never become a new Campaign initialization gate.
        Check(ModuleFrameworkRuntime.Initialize(out _), "directory ready"); Compare("ready parity");
        ModuleFrameworkRuntime.Shutdown(); Compare("stopped parity");
        TeamModuleServices.Policy=null;
        Check(!ModuleFrameworkRuntime.Initialize(out _), "directory degraded"); Compare("degraded parity");
        TeamModuleServices.Policy=new object(); ModuleFrameworkRuntime.Shutdown();
        Check(ModuleFrameworkRuntime.Initialize(out _), "directory reload"); Compare("reload parity");
        int constructors=HostProbe.ConstructorCalls; ModuleFrameworkRuntime.Shutdown();
        Check(HostProbe.ConstructorCalls==constructors, "shutdown does not recreate Campaign behaviors");
        foreach(var type in new[]{typeof(CampaignComposition),typeof(CampaignModelComposition)})
        {
            Check(!type.IsPublic, "composition remains internal");
            Check(type.GetFields(BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Length==0, "no retained Campaign state");
        }
        Console.WriteLine($"PASS {checks} Campaign composition assertions; real source + pinned old methods; stub engine/model/behavior constructors.");
        Console.WriteLine("NOT TESTED: actual game constructor side effects, Campaign save load, Mission cleanup, player interactions.");
    }
}
