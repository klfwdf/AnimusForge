using System;
using System.Linq;
using AnimusForge;
using AnimusForge.DialogueUI.Native;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
static class Program
{
    static int checks;
    static void Check(bool condition,string label) { checks++; if(!condition) throw new Exception(label); }
    static void Reject(Action action,string label) { bool rejected=false; try{action();}catch{rejected=true;} Check(rejected,label); }
    static SceneTradeController Fresh() { Campaign.Current=new(); return Campaign.Current.Behavior._j17SceneTradeController; }
    static void Main()
    {
        var harmony=new Harmony("DialogueUI.ManagedBridgeTests");
        InlineTradeBridge.Install(harmony);
        Check(InlineTradeBridge.Available, "current controller contract installs");
        Check(HarmonyLib.AccessTools.Field(typeof(ShoutBehavior), "_shoutTradeOptions") == null, "legacy host fields are absent in migration fixture");
        foreach(string mode in new[]{"give","show","give_troops","give_prisoners","give_settlements"})
        {
            var host=Fresh(); using var bridge=new InlineTradeBridge(); int before=MBInformationManager.NativeInquiries;
            Check(bridge.Load(mode),mode+" loads"); Check(MBInformationManager.NativeInquiries==before,"no menu popup");
            Check(host.InlineInquiryCapture == null, "capture released after synchronous option load");
            var first=bridge.Options[1]; var second=bridge.Options[0];
            var choices=new[]{new TradeChoice(first,mode=="give_settlements"?1:7),new TradeChoice(second,mode=="give_settlements"?1:15)};
            bridge.Commit(choices);
            Check(host.Commits==1 && host.NativeAmounts==0,mode+" commits once without amount popup");
            Check(host.LastAmounts.SequenceEqual(choices.Select(c=>c.Amount)),"selection order/amounts preserved");
            Check(host.LastIds[0]=="grain","host resource identity preserved");
            Reject(()=>bridge.Commit(choices),"second submission rejected");
        }
        { var host=Fresh(); using var b=new InlineTradeBridge(); b.Load("give"); b.Cancel(); b.Cancel(); Check(host.Cancels==1 && host.Commits==0,"cancel is idempotent and never transfers"); }
        foreach(int amount in new[]{-1,0,101,int.MaxValue})
        { var host=Fresh(); using var b=new InlineTradeBridge(); b.Load("give"); Reject(()=>b.Commit(new[]{new TradeChoice(b.Options[0],amount)}),"invalid amount"); Check(host.Commits==0,"invalid no transfer"); }
        { var host=Fresh(); using var b=new InlineTradeBridge(); b.Load("give"); var c=new TradeChoice(b.Options[0],1); Reject(()=>b.Commit(new[]{c,c}),"duplicate resource"); Check(host.Commits==0,"duplicate no transfer"); }
        { var host=Fresh(); using var b=new InlineTradeBridge(); b.Load("give"); var c=new TradeChoice(b.Options[0],1); host.ReplaceOptions(); Reject(()=>b.Commit(new[]{c}),"stale inventory snapshot"); Check(host.Commits==0,"stale no transfer"); }
        { var host=Fresh(); using var b=new InlineTradeBridge(); b.Load("give"); var c=new TradeChoice(b.Options[0],1); host.SupersedeOwner(); Reject(()=>b.Commit(new[]{c}),"replaced host flow"); b.Cancel(); Check(host.Cancels==0 && host.Commits==0,"old panel cannot cancel newer flow"); }
        { var host=Fresh(); host.CorruptOrder=true; using var b=new InlineTradeBridge(); b.Load("give"); Reject(()=>b.Commit(new[]{new TradeChoice(b.Options[0],2),new TradeChoice(b.Options[1],3)}),"reordered host pending list"); Check(host.Commits==0 && host.Cancels==1,"mapping mismatch cancelled before transfer"); }
        { var host=Fresh(); host.ThrowBeforeCommit=true; using var b=new InlineTradeBridge(); b.Load("give"); Reject(()=>b.Commit(new[]{new TradeChoice(b.Options[0],2)}),"host error surfaces"); Check(host.Cancels==1 && host.Commits==0,"host error releases owned flow"); }
        { var host=Fresh(); host.InjectUnrelated=true; using var b=new InlineTradeBridge(); int before=MBInformationManager.NativeInquiries; b.Load("give"); Check(MBInformationManager.NativeInquiries==before+1,"nested unrelated inquiry remains native"); }
        { var host=Fresh(); host.RefuseCategory=true; using var b=new InlineTradeBridge(); Check(!b.Load("give_troops"),"host eligibility rejection retained"); Check(b.Options.Count==0 && host.Cancels==1,"rejected flow cleared once"); }
        { var host=Fresh(); using var b=new InlineTradeBridge(); b.Load("give"); var choice=new[]{new TradeChoice(b.Options[0],2)}; host.DuringCommit=()=>Reject(()=>b.Commit(choice),"reentrant commit"); b.Commit(choice); Check(host.Commits==1,"reentry cannot transfer twice"); }
        { var host=Fresh(); var b=new InlineTradeBridge(); b.Dispose(); Check(!b.Load("give"),"disposed panel cannot open flow"); }
        { var host=Fresh(); using var b=new InlineTradeBridge(); b.Load("give"); var c=new TradeChoice(b.Options[0],2); host.InvalidateUi(); b.Commit(new[]{c}); Check(host.Commits==0,"captured original callback retains revision guard and cannot transfer stale UI choices"); }
        harmony.UnpatchAll(harmony.Id);
        Console.WriteLine("PASS: "+checks+" managed bridge assertions with real Harmony. Game transfer/rendering still needs live acceptance.");
    }
}
