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
        VengeanceIntegration.SetEnabled(true);
        VengeanceIntegration.FieldCampMissionProbe = m => m != null;
        VengeanceIntegration.FieldCampCeremonyAdmissionProbe = m => m?.CeremonyReady == true;
        MBInformationManager.Last = null;
        MBInformationManager.OnShow = null;
        return host;
    }
    static void Main()
    {
        var host = Setup(); var mission = host.Mission; var hero = Campaign.Current!.Behavior.Options[0].Hero;
        host.Choose(hero);
        Check(string.Join(",", mission.Events) == "commit,begin,cleanup,remove-controller,remove-death,inject", "authorize next request before detaching previous entry");
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
        Check(string.Join(",", mission.Events) == "commit,begin", "begin rejection preserves executioner and controller");
        Check(host.CanChoose, "F entry remains available after begin rejection");
        host.Service.BeginValid = true; host.Choose(hero);
        Check(string.Join(",", mission.Events) == "commit,begin,begin,cleanup,remove-controller,remove-death,inject", "retry succeeds without repeating campaign commit or changing mission");
        host = Setup(); hero = Campaign.Current!.Behavior.Options[0].Hero; mission = host.Mission; ExecutionSessionCoordinator.Fail = true; host.Choose(hero);
        Check(mission.Events.Contains("cancel"), "failed injection cancels new request");
        ExecutionSessionCoordinator.Fail = false;
        host = Setup(); host.Open(); var first = MBInformationManager.Last; host.Open();
        Check(ReferenceEquals(first, MBInformationManager.Last), "only one inquiry at a time");
        host = Setup(); hero = Campaign.Current!.Behavior.Options[0].Hero; host.Mission.CeremonyReady = false; host.Choose(hero);
        Check(host.Mission.Events.Count == 0, "busy camp cannot commit or start continuation");
        host.Open(); Check(!host.ChoiceOpen, "busy camp cannot open inquiry");
        host = Setup(); host.Open(); inquiry = MBInformationManager.Last!; host.Mission.CeremonyReady = false;
        inquiry.Accept(new List<InquiryElement> { inquiry.Elements[0] });
        Check(host.Mission.Events.Count == 0, "inquiry callback rechecks camp admission");
        host = Setup(); hero = Campaign.Current!.Behavior.Options[0].Hero; mission = host.Mission;
        host.Service.OnBegin = () => mission.CeremonyReady = false; host.Choose(hero);
        Check(string.Join(",", mission.Events) == "commit,begin,cancel", "begin callback becoming busy cancels reservation before cleanup");
        mission.CeremonyReady = true; host.Service.OnBegin = null; Check(host.CanChoose, "busy callback does not destroy retry entry");
        host.Choose(hero); Check(mission.Events.Contains("inject"), "retry after busy activity finishes");
        host = Setup(); hero = Campaign.Current!.Behavior.Options[0].Hero; mission = host.Mission;
        host.Service.OnBegin = () => host.Choose(hero); host.Choose(hero);
        Check(string.Join(",", mission.Events) == "commit,begin,cleanup,remove-controller,remove-death,inject", "begin callback cannot recursively hand off");
        host = Setup(); hero = Campaign.Current!.Behavior.Options[0].Hero; mission = host.Mission;
        host.Service.OnBegin = () => mission.MissionEnded = true; host.Choose(hero);
        Check(string.Join(",", mission.Events) == "commit,begin,cancel", "mission ending during begin cancels without detaching previous owner");
        host = Setup(); hero = Campaign.Current!.Behavior.Options[0].Hero; mission = host.Mission;
        host.Service.OnBegin = () => throw new Exception("begin failure"); host.Choose(hero);
        Check(!mission.Events.Contains("cleanup") && host.CanChoose, "begin exception preserves retry entry and releases transition guard");
        host = Setup(); VengeanceIntegration.SetEnabled(false); host.Choose(null);
        Check(host.Mission.Events.Count == 0, "disabled execution rejects continuation");
        host = Setup(); VengeanceIntegration.FieldCampCeremonyAdmissionProbe = null; host.Choose(null);
        Check(host.Mission.Events.Count == 0, "missing admission probe fails closed");
        VengeanceIntegration.FieldCampCeremonyAdmissionProbe = _ => throw new Exception("owner failure");
        host.Choose(null); Check(host.Mission.Events.Count == 0, "admission probe exception fails closed");
        CheckInquiryCallbacks();
        Console.WriteLine($"PASS {count} field-camp continuation lifecycle assertions (production orchestration; fake engine/service)");
    }

    static void CheckInquiryCallbacks()
    {
        var host = Setup(); var mission = host.Mission; host.Open(); var first = MBInformationManager.Last!;
        Check(first.CancelText == "{=REX_FieldCamp_Continue_Cancel}Cancel", "camp cancel uses its own localization key and fallback");
        first.Cancel(new List<InquiryElement> { first.Elements[1] });
        Check(!host.ChoiceOpen && host.CanChoose, "real cancel dismisses the inquiry and restores F");
        Check(mission.Events.Count == 0, "cancel never commits, begins, finalizes, removes or injects even with a Leave payload");
        host.Open(); var current = MBInformationManager.Last!;
        Check(!ReferenceEquals(first, current) && host.ChoiceOpen, "F opens a fresh inquiry after cancel");
        first.Cancel(new List<InquiryElement>());
        Check(host.ChoiceOpen && ReferenceEquals(current, MBInformationManager.Last), "old cancel cannot close the replacement inquiry");
        first.Accept(new List<InquiryElement> { first.Elements[0] });
        Check(host.ChoiceOpen && mission.Events.Count == 0, "old prisoner confirm cannot close or execute through the replacement inquiry");
        first.Accept(new List<InquiryElement> { first.Elements[1] });
        Check(host.ChoiceOpen && mission.Events.Count == 0, "old Leave confirm cannot finalize the replacement inquiry");
        host.Open();
        Check(ReferenceEquals(current, MBInformationManager.Last), "old callbacks cannot reopen the F gate while a replacement is open");
        current.Cancel(new List<InquiryElement>());
        first.Accept(new List<InquiryElement> { first.Elements[1] });
        current.Accept(new List<InquiryElement> { current.Elements[0] });
        Check(host.CanChoose && mission.Events.Count == 0, "consumed confirms remain inert even when no inquiry is open");
        host.Open(); current = MBInformationManager.Last!;
        current.Accept(new List<InquiryElement> { current.Elements[0] });
        Check(string.Join(",", mission.Events) == "commit,begin,cleanup,remove-controller,remove-death,inject", "cancel then F and confirm still begins before detaching the old entry");
        current.Cancel(new List<InquiryElement>());
        current.Accept(new List<InquiryElement> { current.Elements[1] });
        Check(mission.Events.Count == 6, "completed inquiry callbacks cannot repeat settlement or teardown");

        host = Setup(); host.Open(); current = MBInformationManager.Last!;
        current.Accept(new List<InquiryElement> { current.Elements[1] });
        Check(string.Join(",", host.Mission.Events) == "commit,cleanup,remove-controller,remove-death", "explicit listed Leave still ends the ceremony without beginning another");

        host = Setup(); host.Open(); current = MBInformationManager.Last!;
        current.Accept(new List<InquiryElement>());
        Check(host.CanChoose && host.Mission.Events.Count == 0, "empty confirmation is not Leave");
        host = Setup(); host.Open(); current = MBInformationManager.Last!;
        current.Accept(null!);
        Check(host.CanChoose && host.Mission.Events.Count == 0, "null confirmation is not Leave");
        host = Setup(); host.Open(); current = MBInformationManager.Last!;
        current.Accept(new List<InquiryElement> { null! });
        Check(host.CanChoose && host.Mission.Events.Count == 0, "null inquiry element is not Leave");
        host = Setup(); host.Open(); current = MBInformationManager.Last!;
        current.Accept(new List<InquiryElement> { new InquiryElement(null, "foreign Leave", null) });
        Check(host.CanChoose && host.Mission.Events.Count == 0, "foreign null-identifier element cannot impersonate listed Leave");
        host = Setup(); host.Open(); current = MBInformationManager.Last!;
        current.Accept(new List<InquiryElement> { new InquiryElement(current.Elements[0].Identifier, "foreign hero", null) });
        Check(host.CanChoose && host.Mission.Events.Count == 0, "foreign element cannot confirm even the same prisoner");
        host = Setup(); host.Open(); current = MBInformationManager.Last!;
        current.Accept(new List<InquiryElement> { current.Elements[1], current.Elements[0] });
        Check(host.CanChoose && host.Mission.Events.Count == 0, "ambiguous multiple selections cannot silently become Leave");

        host = Setup(); mission = host.Mission; host.Service.Valid = false; host.Open(); first = MBInformationManager.Last!;
        first.Accept(new List<InquiryElement> { first.Elements[0] });
        Check(host.CanChoose && mission.Events.Count == 0, "validation failure through inquiry retains the uncommitted ceremony");
        host.Open(); current = MBInformationManager.Last!;
        first.Accept(new List<InquiryElement> { first.Elements[1] });
        first.Cancel(new List<InquiryElement>());
        Check(host.ChoiceOpen && mission.Events.Count == 0, "failed confirmation is one-shot and cannot later consume Leave");
        current.Accept(new List<InquiryElement> { current.Elements[1] });
        Check(string.Join(",", mission.Events) == "commit,cleanup,remove-controller,remove-death", "validation failure still allows an explicit Leave from a fresh inquiry");

        host = Setup(); mission = host.Mission; host.Service.BeginValid = false; host.Open(); first = MBInformationManager.Last!;
        first.Accept(new List<InquiryElement> { first.Elements[0] });
        Check(host.CanChoose && string.Join(",", mission.Events) == "commit,begin", "Begin rejection through inquiry preserves the old executioner and F entry");
        host.Open(); current = MBInformationManager.Last!;
        first.Cancel(new List<InquiryElement>());
        first.Accept(new List<InquiryElement> { first.Elements[1] });
        Check(host.ChoiceOpen && string.Join(",", mission.Events) == "commit,begin", "rejected Begin callbacks cannot consume a later choice");
        current.Cancel(new List<InquiryElement>());
        Check(host.CanChoose && string.Join(",", mission.Events) == "commit,begin", "cancel after rejected Begin does not finalize or commit again");
        host.Open(); current = MBInformationManager.Last!;
        current.Accept(new List<InquiryElement> { current.Elements[1] });
        Check(string.Join(",", mission.Events) == "commit,begin,cleanup,remove-controller,remove-death", "Leave remains available after rejected Begin without duplicate campaign commit");

        host = Setup(); mission = host.Mission; host.Open(); first = MBInformationManager.Last!;
        Campaign.Current!.Behavior.Options.Clear();
        first.Accept(new List<InquiryElement> { first.Elements[0] });
        Check(host.CanChoose && mission.Events.Count == 0, "confirmation rechecks custody without consuming the ceremony");
        host.Open(); current = MBInformationManager.Last!;
        Check(current.Elements.Count == 1, "no-prisoner replacement still offers explicit Leave");
        current.Accept(new List<InquiryElement> { current.Elements[0] });
        Check(string.Join(",", mission.Events) == "commit,cleanup,remove-controller,remove-death", "lost custody does not consume the later explicit Leave");

        host = Setup(); mission = host.Mission; host.Open(); first = MBInformationManager.Last!;
        mission.CeremonyReady = false;
        first.Accept(new List<InquiryElement> { first.Elements[1] });
        Check(!host.ChoiceOpen && mission.Events.Count == 0, "busy camp rejects even explicit Leave at callback time");
        mission.CeremonyReady = true; host.Open(); current = MBInformationManager.Last!;
        first.Accept(new List<InquiryElement> { first.Elements[1] });
        Check(host.ChoiceOpen && mission.Events.Count == 0, "late Leave after admission recovers cannot close the fresh inquiry");
        current.Accept(new List<InquiryElement> { current.Elements[1] });
        Check(string.Join(",", mission.Events) == "commit,cleanup,remove-controller,remove-death", "admission rejection does not consume the later authorized Leave");

        host = Setup(); mission = host.Mission; host.Service.BeginValid = false; host.Open(); first = MBInformationManager.Last!;
        var reentryBlocked = false;
        host.Service.OnBegin = () =>
        {
            first.Cancel(new List<InquiryElement>());
            first.Accept(new List<InquiryElement> { first.Elements[1] });
            host.Open();
            reentryBlocked = !host.ChoiceOpen && ReferenceEquals(first, MBInformationManager.Last);
        };
        first.Accept(new List<InquiryElement> { first.Elements[0] });
        Check(reentryBlocked, "Begin callback cannot reenter the inquiry or reuse its consumed identity");
        Check(host.CanChoose && string.Join(",", mission.Events) == "commit,begin", "reentrant old callbacks leave a rejected Begin retryable");
        host.Service.OnBegin = null; host.Service.BeginValid = true; host.Open(); current = MBInformationManager.Last!;
        first.Accept(new List<InquiryElement> { first.Elements[1] });
        current.Accept(new List<InquiryElement> { current.Elements[0] });
        Check(string.Join(",", mission.Events) == "commit,begin,begin,cleanup,remove-controller,remove-death,inject", "fresh inquiry retries Begin without repeating the old campaign commit");

        host = Setup(); mission = host.Mission;
        var showFailure = new InvalidOperationException("show failure");
        MBInformationManager.OnShow = _ => throw showFailure;
        var threw = false;
        try { host.Open(); } catch (InvalidOperationException exception) when (ReferenceEquals(exception, showFailure)) { threw = true; }
        first = MBInformationManager.Last!;
        Check(threw && host.CanChoose && mission.Events.Count == 0, "show failure releases only its own inquiry without touching the ceremony");
        MBInformationManager.OnShow = null; host.Open(); current = MBInformationManager.Last!;
        first.Cancel(new List<InquiryElement>());
        first.Accept(new List<InquiryElement> { first.Elements[1] });
        Check(host.ChoiceOpen && mission.Events.Count == 0, "callbacks retained by failed Show cannot close the replacement");
        current.Cancel(new List<InquiryElement>());
        Check(host.CanChoose && mission.Events.Count == 0, "replacement remains cancellable after Show failure");

        host = Setup(); mission = host.Mission;
        MultiSelectionInquiryData? failedInquiry = null, replacement = null;
        MBInformationManager.OnShow = shown =>
        {
            MBInformationManager.OnShow = null;
            failedInquiry = shown;
            shown.Cancel(new List<InquiryElement>());
            host.Open(); replacement = MBInformationManager.Last;
            throw showFailure;
        };
        threw = false;
        try { host.Open(); } catch (InvalidOperationException exception) when (ReferenceEquals(exception, showFailure)) { threw = true; }
        Check(threw && replacement != null && !ReferenceEquals(failedInquiry, replacement) && host.ChoiceOpen,
            "old Show catch cannot clear an inquiry opened reentrantly after cancel");
        failedInquiry!.Cancel(new List<InquiryElement>());
        failedInquiry.Accept(new List<InquiryElement> { failedInquiry.Elements[1] });
        Check(host.ChoiceOpen && ReferenceEquals(replacement, MBInformationManager.Last) && mission.Events.Count == 0,
            "reentrant replacement resists both old callbacks after Show failure");
        replacement!.Cancel(new List<InquiryElement>());
        Check(host.CanChoose && mission.Events.Count == 0, "reentrant replacement cancel keeps ceremony and F intact");
    }
}
namespace TaleWorlds.Library { public struct Vec3 {} public struct Vec2 {} }
namespace TaleWorlds.Localization { public class TextObject { readonly string _source; public TextObject(string s) { _source=s; } public override string ToString() => _source; } }
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
        public List<InquiryElement> Elements; public Action<List<InquiryElement>> Accept, Cancel; public string CancelText;
        public MultiSelectionInquiryData(string title,string desc,List<InquiryElement> elems,bool a,int b,int c,string d,string e,Action<List<InquiryElement>> ok,Action<List<InquiryElement>> cancel) { Elements=elems; Accept=ok; Cancel=cancel; CancelText=e; }
    }
}
namespace TaleWorlds.MountAndBlade
{
    public class Mission { public static Mission? Current; public bool CeremonyReady=true; public bool MissionEnded; public List<string> Events=new(); public void RemoveMissionBehavior(object o) { Events.Add(o is TownExecutionMissionBehavior ? "remove-controller" : "remove-death"); } }
    public class Agent { public int Index; }
    public static class MBInformationManager { public static MultiSelectionInquiryData? Last; public static Action<MultiSelectionInquiryData>? OnShow; public static void ShowMultiSelectionInquiry(MultiSelectionInquiryData d,bool pauseGameActiveState) { Last=d; OnShow?.Invoke(d); } }
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
        public Action? OnBegin; public Mission Mission; public bool Valid=true,BeginValid=true; public ExecutionService(Mission m) { Mission=m; }
        public Validation Validate(ExecutionRequest r,ExecutionValidationStage s) => new() { IsValid=Valid };
        public Validation Begin(ExecutionRequest r) { Mission.Events.Add("begin"); OnBegin?.Invoke(); return new() { IsValid=BeginValid }; }
        public void Cancel(ExecutionRequest r,ExecutionFailureReason f,TaleWorlds.Localization.TextObject t) { Mission.Events.Add("cancel"); }
    }
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
        bool CommitCampaignExecutionOnMissionExit() { if (_outcome?.DeathCommitted == true) return true; Mission.Events.Add("commit"); if (CommitOk) _outcome=new(); return CommitOk; }
        void FinalizeSession() { Mission.Events.Add("cleanup"); _cleanupComplete=CleanupOk; }
        public bool CanChoose => CanContinueFieldCamp;
        public bool ChoiceOpen => _continuationChoiceOpen;
        public void Choose(Hero? h) => ContinueFieldCamp(h);
        public void Open() => ShowFieldCampContinuation();
    }
}
