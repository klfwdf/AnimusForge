namespace AnimusForge
{
    internal static class DiplomacyIdentityResolver
    {
        internal static int Lookups;
        internal static TaleWorlds.CampaignSystem.Hero Hero(string id)
        { Lookups++; return TaleWorlds.CampaignSystem.Hero.Find(id); }
    }
    internal sealed class VassalageBehavior
    {
        internal static VassalageBehavior Instance { get; } = new();
        internal bool TryApplyVassalageAction(object giver,string action,string type,string target,out string status) { status="applied"; return true; }
    }
    internal sealed class KingdomAnnexationBehavior
    {
        internal static KingdomAnnexationBehavior Instance { get; } = new();
        internal bool TryApplyKingdomAnnexation(object giver,string target,out string status) { status="applied"; return true; }
    }
    internal static class VassalageDiagnosticLog
    { internal static object DescribeHero(object hero)=>hero; internal static void Event(string name,Dictionary<string,object> fields) { } }
    internal static class KingdomAnnexationDiagnosticLog
    { internal static object DescribeHero(object hero)=>hero; internal static void Event(string name,Dictionary<string,object> fields) { } }
    internal static class Logger
    { internal static void Log(string category,string text) { } internal static void Obs(string category,string name,Dictionary<string,object> fields) { } }
}
namespace TaleWorlds.Core { }
namespace TaleWorlds.Library
{
    internal readonly struct Color { internal static Color FromUint(uint n)=>new(); }
    internal sealed class InformationMessage { internal InformationMessage(string text,Color color) { } }
    internal static class InformationManager { internal static void DisplayMessage(InformationMessage message) { } }
}
internal static class PoliticalEngineReplay
{
    internal static void Run(Action<bool,string> check)
    {
        Engine.Reset(Fault.None); AnimusForge.DiplomacyIdentityResolver.Lookups=0;
        var port=new AnimusForge.DiplomacyPoliticalRewardPort("speaker","ruler");
        string text="ordinary reply";
        AnimusForge.DiplomacyPoliticalRewardApplication.Apply(port,AnimusForge.DiplomacyPoliticalRewardKind.Vassalage,ref text,new(),new());
        check(AnimusForge.DiplomacyIdentityResolver.Lookups==0,"real political adapter performs no actor lookup for ordinary replies");
        text="[ACTION:VASSALAGE:SUBMIT:VASSAL:k][ACTION:VASSALAGE:SUBMIT:MILITARY:k]";
        AnimusForge.DiplomacyPoliticalRewardApplication.Apply(port,AnimusForge.DiplomacyPoliticalRewardKind.Vassalage,ref text,new(),new());
        check(AnimusForge.DiplomacyIdentityResolver.Lookups==2,"real political adapter resolves each actor once across multiple matched tags");
        TaleWorlds.CampaignSystem.Hero.MainHero=null!;
        var missing=new AnimusForge.DiplomacyPoliticalRewardPort("speaker","missing");
        check(!missing.GiverIsPlayer && !missing.ReceiverIsPlayer,"null player and failed receiver resolution cannot grant player qualification");
    }
}
