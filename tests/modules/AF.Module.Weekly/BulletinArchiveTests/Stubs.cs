// Only external/game/render/settings boundaries are fakes; production algorithms are linked/extracted.
namespace TaleWorlds.CampaignSystem { public interface IDataStore { bool SyncData<T>(string key, ref T data); } }
namespace TaleWorlds.Library {
 [System.AttributeUsage(System.AttributeTargets.Property)] public sealed class DataSourcePropertyAttribute:System.Attribute {}
 public class ViewModel { protected void OnPropertyChangedWithValue<T>(T value,string name){} public virtual void OnFinalize(){} }
 public class MBBindingList<T>:System.Collections.Generic.List<T> {}
}
namespace AnimusForge {
 internal static class Logger { public static bool IsModLogicEnabled=>false; public static void Log(string tag,string text){} }
 internal static class PolicySystemLog { public static void Failure(string a,string b,string c,string d){} }
 public class DuelSettings { public int WeeklyReportPopupBodyFontSize=18; public static DuelSettings GetSettings()=>new(); }
 public partial class MyBehavior {
  public static MyBehavior Instance; public static bool BulletinEnabled=true;
  public static bool IsWorldBulletinEnabled()=>BulletinEnabled;
  public System.Collections.Generic.List<WeeklyReportBrowserCountryData> Countries=new();
  public System.Collections.Generic.List<WeeklyReportBrowserCountryData> GetTerminalWeeklyReportBrowserCountries()=>Countries;
  public System.Threading.Tasks.Task<bool> GenerateWeeklyReportFullByEventIdAsync(string id)=>System.Threading.Tasks.Task.FromResult(false);
  public string OpenedId; public int Opens;
  internal bool OpenArchivedWeeklyReport(string id){OpenedId=id;Opens++;return true;}
 }
 internal sealed class WorldBulletinLayout { public string EventId;public System.Collections.Generic.List<string> KingdomIds=new(); }
 internal sealed class WorldBulletinPort {
  internal System.Func<System.Collections.Generic.List<MyBehavior.EventRecordEntry>> Records;
  internal System.Func<string,MyBehavior.EventRecordEntry> FindRecord;
  internal System.Func<MyBehavior.EventRecordEntry,string> ProductState;
  internal System.Func<string> CurrentDate;
  internal System.Action<string,MyBehavior.EventRecordEntry> NotifyProductChanged;
  internal System.Action NotifyTimeline;
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
