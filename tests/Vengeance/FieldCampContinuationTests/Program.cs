using System;
using System.Collections.Generic;
using RichExecutions.Core;
using RichExecutions.Scene;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

static class Program
{
    static int count;
    static void Check(bool condition, string name) { if (!condition) throw new Exception(name); count++; }
    static TownExecutionMissionBehavior Setup()
    {
        var host = new TownExecutionMissionBehavior();
        Mission.Current = host.Mission;
        Campaign.Current = new Campaign();
        Campaign.Current.Behavior.Options.Add(new ExecutionStudioPrisonerOption(new Hero()));
        VengeanceIntegration.IsEnabled = true;
        return host;
    }
    static void Main()
    {
        var host = Setup(); var mission = host.Mission; var hero = Campaign.Current!.Behavior.Options[0].Hero;
        host.Choose(hero);
        Check(string.Join(",", mission.Events) == "commit,cleanup,remove-controller,remove-death,begin,inject", "settle and detach before next begin");
        Check(ExecutionSessionCoordinator.LastMission == mission, "reuse mission");
        Check(ExecutionSessionCoordinator.LastPreset == host.Preset, "reuse layout");
        host.Choose(hero); Check(mission.Events.Count == 6, "duplicate callback ignored");
        host = Setup(); mission = host.Mission; host.Choose(null);
        Check(string.Join(",", mission.Events) == "commit,cleanup,remove-controller,remove-death", "leave returns to camp without next begin");
        host = Setup(); hero = Campaign.Current!.Behavior.Options[0].Hero; host.Request.IsFieldCamp = false; host.Choose(hero);
        Check(host.Mission.Events.Count == 0, "town never commits early");
        host = Setup(); hero = Campaign.Current!.Behavior.Options[0].Hero; Mission.Current = new Mission(); host.Choose(hero);
        Check(host.Mission.Events.Count == 0, "stale mission ignored");
        host = Setup(); hero = Campaign.Current!.Behavior.Options[0].Hero; host.Mission.MissionEnded = true; host.Choose(hero);
        Check(host.Mission.Events.Count == 0, "ended mission ignored");
        host = Setup(); hero = Campaign.Current!.Behavior.Options[0].Hero; hero.IsDead = true; host.Choose(hero);
        Check(host.Mission.Events.Count == 0, "dead target rejected");
        host = Setup(); host.Choose(new Hero()); Check(host.Mission.Events.Count == 0, "lost custody rejected");
        host = Setup(); host.CommitOk = false; host.Choose(null);
        Check(string.Join(",", host.Mission.Events) == "commit", "failed commit retains owner");
        host = Setup(); host.CleanupOk = false; host.Choose(null);
        Check(string.Join(",", host.Mission.Events) == "commit,cleanup", "failed cleanup retains owner");
        host = Setup(); host.Open(); var inquiry = MBInformationManager.Last!;
        Check(inquiry.Elements.Count == 2, "candidate plus leave");
        Mission.Current = new Mission(); inquiry.Accept(new List<InquiryElement> { inquiry.Elements[0] });
        Check(host.Mission.Events.Count == 0, "stale inquiry callback ignored");
        host = Setup(); hero = Campaign.Current!.Behavior.Options[0].Hero; host.Service.Valid = false; host.Choose(hero);
        Check(host.Mission.Events.Count == 0, "invalid next request does not settle current");
        host = Setup(); hero = Campaign.Current!.Behavior.Options[0].Hero; host.Service.BeginValid = false; mission = host.Mission; host.Choose(hero);
        Check(!mission.Events.Contains("inject"), "begin rejection never injects");
        host = Setup(); hero = Campaign.Current!.Behavior.Options[0].Hero; mission = host.Mission; ExecutionSessionCoordinator.Fail = true; host.Choose(hero);
        Check(mission.Events.Contains("cancel"), "failed injection cancels new request");
        ExecutionSessionCoordinator.Fail = false;
        host = Setup(); host.Open(); var first = MBInformationManager.Last; host.Open();
        Check(ReferenceEquals(first, MBInformationManager.Last), "only one inquiry at a time");
        Console.WriteLine($"PASS {count} field-camp continuation lifecycle assertions (production orchestration; fake engine/service)");
    }
}
namespace TaleWorlds.Library { public struct Vec3 {} public struct Vec2 {} }
namespace TaleWorlds.Localization { public class TextObject { public TextObject(string s) {} public override string ToString() => "text"; } }
namespace TaleWorlds.CampaignSystem
{
    public class Hero { public bool IsDead; public TaleWorlds.Localization.TextObject Name = new("hero"); }
    public class Campaign { public static Campaign? Current; public RichExecutions.Campaign.RichExecutionCampaignBehavior Behavior = new(); public T? GetCampaignBehavior<T>() where T:class => Behavior as T; }
}
namespace TaleWorlds.Core
{
    public class InformationMessage { public InformationMessage(string s) {} }
    public static class InformationManager { public static void DisplayMessage(InformationMessage m) {} }
    public class InquiryElement { public object? Identifier; public InquiryElement(object? id,string s,object? image) { Identifier=id; } }
    public class MultiSelectionInquiryData
    {
        public List<InquiryElement> Elements; public Action<List<InquiryElement>> Accept;
        public MultiSelectionInquiryData(string title,string desc,List<InquiryElement> elems,bool a,int b,int c,string d,string e,Action<List<InquiryElement>> ok,Action<List<InquiryElement>> cancel) { Elements=elems; Accept=ok; }
    }
}
namespace TaleWorlds.MountAndBlade
{
    public class Mission { public static Mission? Current; public bool MissionEnded; public List<string> Events=new(); public void RemoveMissionBehavior(object o) { Events.Add(o is TownExecutionMissionBehavior ? "remove-controller" : "remove-death"); } }
    public static class MBInformationManager { public static MultiSelectionInquiryData? Last; public static void ShowMultiSelectionInquiry(MultiSelectionInquiryData d,bool pauseGameActiveState) { Last=d; } }
}
namespace RichExecutions.Customization { public class ExecutionSitePreset {} }
namespace RichExecutions.Diagnostics { public static class RexLog { public static void Info(string s) {} public static void Error(string s,Exception e) {} } }
namespace RichExecutions.Core
{
    public enum ExecutionSessionState { Aftermath }
    public enum ExecutionValidationStage { Confirmation }
    public enum ExecutionFailureReason { ScenePlacementFailed }
    public class ExecutionRequest { public bool IsFieldCamp=true; public Hero Victim=new(); public object Charge=new(), Tone=new(); public string SessionId="session"; }
    public class ExecutionStudioPrisonerOption { public Hero Hero; public object Source=new(); public ExecutionStudioPrisonerOption(Hero h) { Hero=h; } }
    public class Validation { public bool IsValid=true; public TaleWorlds.Localization.TextObject Message=new("error"); }
    public class Outcome { public bool DeathCommitted=true; }
    public class ExecutionService
    {
        public Mission Mission; public bool Valid=true,BeginValid=true; public ExecutionService(Mission m) { Mission=m; }
        public Validation Validate(ExecutionRequest r,ExecutionValidationStage s) => new() { IsValid=Valid };
        public Validation Begin(ExecutionRequest r) { Mission.Events.Add("begin"); return new() { IsValid=BeginValid }; }
        public void Cancel(ExecutionRequest r,ExecutionFailureReason f,TaleWorlds.Localization.TextObject t) { Mission.Events.Add("cancel"); }
    }
    public static class VengeanceIntegration { public static bool IsEnabled=true; public static bool IsFieldCampAvailable() => true; }
}
namespace RichExecutions.Campaign
{
    public class RichExecutionCampaignBehavior
    {
        public List<ExecutionStudioPrisonerOption> Options=new();
        public List<ExecutionStudioPrisonerOption> GetDeploymentPrisonerOptions(object? s) => Options;
        public ExecutionRequest BuildDeploymentRequest(Hero h,object src,object method,object charge,object tone,object? s) => new() { Victim=h };
    }
}
namespace RichExecutions.Scene
{
    public static class ExecutionSessionCoordinator
    {
        public static bool Fail; public static Mission? LastMission; public static object? LastPreset;
        public static void InjectChainedSession(Mission m,ExecutionRequest r,ExecutionService s,object p,object o,object f) { m.Events.Add("inject"); if (Fail) throw new Exception("injection"); LastMission=m; LastPreset=p; }
    }
    public sealed partial class TownExecutionMissionBehavior
    {
        public Mission Mission=new(); public ExecutionRequest Request=new(); public ExecutionSessionState State=ExecutionSessionState.Aftermath;
        bool _aftermathSessionReleased=true, _playerExecutionStateRestored=true, _aftermathConversationsRestored=true;
        bool _cleanupComplete, _initializationFailed=false, _endMissionNextTick=false;
        ExecutionService _service; public ExecutionService Service => _service;
        Outcome? _outcome; object _deathLogic=new();
        RichExecutions.Customization.ExecutionSitePreset _customSitePreset=new(); public object Preset => _customSitePreset;
        class Placement { public TaleWorlds.Library.Vec3 Origin; public TaleWorlds.Library.Vec2 Forward; }
        Placement _placement=new(); public object CurrentMethod=new(); public bool CommitOk=true,CleanupOk=true;
        public TownExecutionMissionBehavior() { _service=new(Mission); }
        bool CommitCampaignExecutionOnMissionExit() { Mission.Events.Add("commit"); _outcome=new(); return CommitOk; }
        void FinalizeSession() { Mission.Events.Add("cleanup"); _cleanupComplete=CleanupOk; }
        public void Choose(Hero? h) => ContinueFieldCamp(h);
        public void Open() => ShowFieldCampContinuation();
    }
}
