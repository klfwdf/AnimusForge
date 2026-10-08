// Only external/game/render/settings boundaries are fakes; production algorithms are linked/extracted.
namespace TaleWorlds.CampaignSystem {
 public interface IDataStore { bool SyncData<T>(string key, ref T data); }
 public class Campaign { public static Campaign Current=new(); public EncyclopediaManager EncyclopediaManager=new(); }
 public class Hero { public string StringId; public bool IsLord; public Clan Clan; public Settlement CurrentSettlement; public Party PartyBelongedTo; }
 public class CharacterObject { public Hero HeroObject; }
 public class Clan { public static Clan PlayerClan; public Kingdom Kingdom; public Hero Leader; }
 public class Kingdom { public Clan RulingClan; public string StringId; }
 public class Settlement { public string StringId; }
 public class Party { public Settlement CurrentSettlement; }
 public class EncyclopediaManager { public int Opens; public string Link; public void GoToLink(string link){Opens++;Link=link;} }
}
namespace TaleWorlds.Library {
 public class InformationMessage { public readonly string Text; public InformationMessage(string text,object color=null){Text=text;} }
 public static class InformationManager { public static readonly System.Collections.Generic.List<string> Messages=new(); public static void DisplayMessage(InformationMessage message){Messages.Add(message.Text);} }
 public static class Colors { public static readonly object Yellow=new(); }
 [System.AttributeUsage(System.AttributeTargets.Property)] public sealed class DataSourcePropertyAttribute:System.Attribute {}
 public class ViewModel { protected void OnPropertyChangedWithValue<T>(T value,string name){} public virtual void OnFinalize(){} }
 public class MBBindingList<T>:System.Collections.Generic.List<T> {}
}
namespace TaleWorlds.Core {
 public class Game { public static Game Current=new(); public GameStateManager GameStateManager=new(); }
 public class GameStateManager { public object ActiveState; }
 public static class MBInformationManager { internal static readonly System.Collections.Generic.List<AnimusForge.WorldDiplomacyMapNotification> Notices=new(); internal static void AddNotice(AnimusForge.WorldDiplomacyMapNotification notice){Notices.Add(notice);} }
}
namespace TaleWorlds.CampaignSystem.GameState { public class MapState {} }
namespace TaleWorlds.MountAndBlade { public class Mission { public static Mission Current; } }
namespace TaleWorlds.InputSystem { public static class Input { public static void SetClipboardText(string text){} } }
namespace AnimusForge {
 internal static class EncyclopediaEntityLinkFormatter {
  internal static string SanitizeUntrustedRichText(string text)=>text;
  internal static DisplaySession CreateDisplaySession()=>new();
  internal sealed class DisplaySession { internal string Format(string text)=>text; }
 }
 internal static class Logger { public static bool IsModLogicEnabled=>false; public static void Log(string tag,string text){} }
 internal static class PolicySystemLog { public static void Failure(string a,string b,string c,string d){} }
 public partial class DuelSettings { public int WeeklyReportPopupBodyFontSize=18; public static DuelSettings GetSettings()=>new(); }
 public partial class MyBehavior {
  public static MyBehavior Instance; public static bool BulletinEnabled=true;
  public static bool IsWorldBulletinEnabled()=>BulletinEnabled;
  public System.Collections.Generic.List<WeeklyReportBrowserCountryData> Countries=new();
  public System.Collections.Generic.List<WeeklyReportBrowserCountryData> GetTerminalWeeklyReportBrowserCountries()=>Countries;
  public System.Collections.Generic.List<WeeklyReportBrowserCountryData> GetTerminalKingdomNewsCountries()=>Countries;
  public System.Func<string,System.Threading.Tasks.Task<bool>> FullReport;
  public System.Threading.Tasks.Task<bool> GenerateWeeklyReportFullByEventIdAsync(string id)=>FullReport?.Invoke(id)??System.Threading.Tasks.Task.FromResult(false);
  public string OpenedId; public int Opens;
  internal bool OpenArchivedWeeklyReport(string id){OpenedId=id;Opens++;return true;}
 }
 public partial class MyBehavior {
  internal readonly WeeklyEventRecordStateOwner _records=new();
  internal System.Collections.Generic.List<EventRecordEntry> _eventRecordEntries {get=>_records.Records;set=>_records.Records=value;}
  internal WorldBulletinSaveState _worldBulletinState;
  private static int GetCurrentGameDayIndexSafe()=>5;
  private static string ResolveWeeklyReportNpcKingdomId(TaleWorlds.CampaignSystem.Hero h,TaleWorlds.CampaignSystem.CharacterObject c,string id)=>id??"A";
  private static string ResolveWeeklyReportSurroundingsKingdomId(TaleWorlds.CampaignSystem.Hero h,TaleWorlds.CampaignSystem.CharacterObject c,string id)=>"B";
  private static System.Collections.Generic.List<string> GetDevEditableKingdoms()=>new(){"A","B"};
  private static bool IsKingdomEligibleForWeeklyReport(string id)=>true;
  private static string GetKingdomId(string id)=>id;
  internal static string ResolveKingdomDisplay(string id)=>id;
  private static System.Collections.Generic.List<string> GetKingdomIdsByPlayerProximity(System.Collections.Generic.List<string> ids)=>ids;
  private static System.Collections.Generic.List<string> SelectWeeklyShortReportKingdomIdsFromSnapshot(string id,bool excludeNpcKingdom,bool eligible,System.Collections.Generic.IEnumerable<string> nearest,System.Collections.Generic.IEnumerable<string> fallback)=>new(excludeNpcKingdom?nearest.Where(x=>x!=id):nearest);
  private EventRecordEntry FindLatestWorldBulletinRecord()=>_eventRecordEntries?.FirstOrDefault();
  internal WeeklyPromptSnapshot ReplayNpcSnapshot(TaleWorlds.CampaignSystem.Hero hero=null,TaleWorlds.CampaignSystem.CharacterObject character=null,string kingdom=null)=>
   AnimusForge.Refactor.Adapters.WorldBulletinNpcPromptCaptureAdapter.CaptureWorldBulletinNpcSnapshot(new AnimusForge.Refactor.Adapters.WeeklyPromptCaptureAdapter.CapturePorts {
    BulletinEvents=()=>_worldBulletinState?.Events, LatestBulletin=FindLatestWorldBulletinRecord,
    NpcKingdom=ResolveWeeklyReportNpcKingdomId, SurroundingsKingdom=ResolveWeeklyReportSurroundingsKingdomId,
    EditableKingdoms=()=>new(){new(){StringId="A"},new(){StringId="B"}}, Eligible=k=>IsKingdomEligibleForWeeklyReport(k.StringId),
    Proximity=GetKingdomIdsByPlayerProximity, SelectSnapshot=SelectWeeklyShortReportKingdomIdsFromSnapshot
   },hero,character,kingdom);
  internal System.Collections.Generic.IReadOnlyList<WorldWeeklyReportHistoryEntry> ReplayDiplomacyHistory(int minWeek=-1)=>_records.GetPublishedWorldWeeklyReportHistoryInternal(minWeek);
 }
 // Only identity lookups, live host availability and current storage are fixtures. Knowledge selection,
 // adapter delegation and MyBehavior's prompt assembly are the actual production method spans.
 internal static class DiplomacyModuleServices { internal static WorldDiplomacyModuleAdapter World=new(); }
 internal sealed partial class WorldDiplomacyModuleAdapter {}
 internal static class DiplomacyIdentityResolver {
  internal static readonly System.Collections.Generic.Dictionary<string,TaleWorlds.CampaignSystem.Hero> Heroes=new();
  internal static TaleWorlds.CampaignSystem.Hero Hero(string id)=>id!=null&&Heroes.TryGetValue(id,out var hero)?hero:null;
 }
 internal sealed class WorldDiplomacyStorage {
  internal System.Collections.Generic.List<WorldDiplomacySettlementKnowledge> SettlementKnowledge=new();
  internal System.Collections.Generic.List<WorldDiplomacyKingdomKnowledge> NobleKnowledge=new(),KingdomKnowledge=new();
 }
 public sealed partial class WorldDiplomacyBehavior {
  internal static WorldDiplomacyBehavior Instance; internal WorldDiplomacyStorage _storage=new();
  internal static readonly System.Collections.Generic.Dictionary<string,TaleWorlds.CampaignSystem.Kingdom> Kingdoms=new();
  internal static int Captures; internal static bool ThrowOnCapture;
  private static WorldDiplomacyBehavior ResolveInstance(){Captures++;if(ThrowOnCapture)throw new System.InvalidOperationException("fixture unavailable");return Instance;}
  private static TaleWorlds.CampaignSystem.Kingdom ResolveKingdom(string id)=>id!=null&&Kingdoms.TryGetValue(id,out var kingdom)?kingdom:null;
  internal static string FormatDateForProactive(int day)=>day.ToString();
  private static bool AreMapNotificationsEnabled()=>true;
  private bool TryEnsureMapNotificationRegistered()=>true;
  private static string KingdomName(TaleWorlds.CampaignSystem.Kingdom kingdom)=>kingdom?.StringId??"";
  private static string FormatCampaignDate(int day)=>day.ToString();
  private static int CurrentDay()=>5;
  private static void Log(string text){}
  internal void ReplayShowNotice(AnimusForge.Refactor.Contracts.WorldDiplomacyNotice notice)=>new NotificationWorld(this).ShowNotice(notice);
 }
 internal sealed class WorldDiplomacyMapNotification {
  internal readonly string DocumentId,Title,Description;
  public WorldDiplomacyMapNotification(string documentId,string title,string description){DocumentId=documentId;Title=title;Description=description;}
 }
 internal sealed class WorldBulletinLayout { public string EventId;public WorldBulletinIllustrationPlan IllustrationPlan;public System.Collections.Generic.List<string> KingdomIds=new(); }
 internal sealed partial class WorldBulletinStateOwner {
  internal bool InFlight;internal string CorruptRaw,LatestEventId;private const string WorldBulletinBulletinIdMarker=":bulletin:";
  internal readonly System.Collections.Concurrent.ConcurrentQueue<System.Action> MainThreadActions=new();
  // Regional aggregation and artwork layout are unchanged fixture boundaries in this publication replay.
  internal System.Collections.Generic.List<MyBehavior.EventRecordEntry> BuildRegionalPublication(WorldBulletinSelection s)=>new();
  private void CommitRegionalPublication(System.Collections.Generic.List<MyBehavior.EventRecordEntry> p){}
  internal void RecordWorldBulletinLayout(string id,WorldBulletinSelection s){}
  internal WorldBulletinLayout FindWorldBulletinLayout(string id)=>null;
 }
 public partial class DuelSettings {
  // Unrelated diplomacy migration, host paths, logging and text-length policy are fixture boundaries.
  private static bool IsWorldDiplomacyPromptPath(string path)=>false;
  private static string MigrateLegacyWorldDiplomacyPromptText(string text)=>text;
  private static void LogPlayerCustomPromptRuleWarning(string text){}
  private static string LimitCustomPromptText(string text,string file)=>text;
  private static bool IsPolicyPromptFileName(string file)=>false;
  private static string GetPolicyPromptTextFilePath(string dir,string file)=>System.IO.Path.Combine(dir,file);
  private static string GetCustomPromptTextFilePath(string dir,string file)=>System.IO.Path.Combine(dir,file);
  internal static bool ReplayReadPrompt(string path,out string text)=>TryReadCustomPromptTextJsonFile(path,NormalizeWorldBulletinWritingRequirementsText,DefaultWorldBulletinWritingRequirements,out text);
  internal static bool ReplayReadLayered(string over,string packaged,out string text)=>TryReadLayeredCustomPromptTextJsonFile(over,packaged,WorldBulletinWritingRequirementsJsonFileName,NormalizeWorldBulletinWritingRequirementsText,DefaultWorldBulletinWritingRequirements,out text);
  internal static string ReplayMigratePrompt(string text)=>MigrateLegacyWorldBulletinWritingRequirements(text);
 }
 internal static class WeeklyGenerationRules {
  // Text processing is not under test here; archive metadata must survive its calls.
  internal static string NeutralizeWeeklyReportScenarioName(string text)=>(text??"").Trim();
  internal static string BuildFallbackWeeklyReportShortSummary(string text)=>(text??"").Trim();
 }
 internal sealed class MemoryStore:TaleWorlds.CampaignSystem.IDataStore {
  public readonly System.Collections.Generic.Dictionary<string,object> Data;
  private readonly bool _loading;
  internal MemoryStore(System.Collections.Generic.Dictionary<string,object> data,bool loading=false){Data=data;_loading=loading;}
  public bool SyncData<T>(string key,ref T value){if(!_loading){Data[key]=value;return true;}if(Data.TryGetValue(key,out var v)){value=(T)v;return true;}return false;}
 }
}
