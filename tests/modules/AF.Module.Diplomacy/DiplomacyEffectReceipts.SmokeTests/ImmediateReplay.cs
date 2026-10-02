using AnimusForge;
internal static class ImmediateReplay
{
    internal static void Run(Action<bool,string> check)
    {
        foreach (string action in new[]{"declare_war","break_alliance","cancel_trade"})
        foreach (Fault fault in new[]{Fault.None,Fault.NoOp,Fault.Before,Fault.After,Fault.Unreadable,Fault.NoOpUnreadable,Fault.BeforeUnreadable})
        {
            Engine.Reset(fault,allied:action=="break_alliance",trading:action=="cancel_trade");
            var port=WorldDiplomacyBehavior.NewImmediatePort();
            var receipt=action switch { "declare_war"=>port.DeclareWar("player","npc",new()),"break_alliance"=>port.BreakAlliance("player","npc",new()),_=>port.CancelTrade("player","npc",new()) };
            bool known=fault is not (Fault.Unreadable or Fault.NoOpUnreadable or Fault.BeforeUnreadable);
            bool applied=fault is Fault.None or Fault.After;
            check(receipt.Known==known && receipt.Applied==applied && Engine.Calls==1,"real immediate effect is attempted once and measured: "+action+" "+fault);
            if(!known)check(receipt.Message.Contains("无法确认") && !receipt.Message.Contains("未执行"),"unknown immediate outcome must not claim success or confirmed failure");
            Engine.Reset(fault,allied:action=="break_alliance",trading:action=="cancel_trade");
            port=WorldDiplomacyBehavior.NewImmediatePort(); var document=new WorldDiplomacyDocument { IsPlayerAuthored=true };
            WorldDiplomacyImmediateActionApplication.Execute(port,"player","npc",action,document);
            check(document.ChangedDiplomaticState==applied && (action!="declare_war" || port.Storage.LastOffensiveWarDayByKingdom.ContainsKey("player")==applied),"immediate Application credits only confirmed game changes");
        }
    }
}
