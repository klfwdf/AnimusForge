using System;using System.Collections.Generic;using System.IO;using System.Linq;using System.Text;using Newtonsoft.Json;
namespace TaleWorlds.CampaignSystem { public class Campaign {public static Campaign Current;public T GetCampaignBehavior<T>()=>default;}}
namespace AnimusForge.Refactor.Runtime {internal static class SaveRuntimeGuard {internal static long CaptureGeneration()=>1;}}
namespace TaleWorlds.Library { public class InformationMessage {public string Text;public InformationMessage(string text){Text=text;}} public static class InformationManager {public static List<string> Messages=new();public static void DisplayMessage(InformationMessage msg){Messages.Add(msg.Text);}} }
namespace AnimusForge {
internal static class TestState {internal static List<string> Order=new();}
public partial class MyBehavior {internal class NpcPersonaProfile {public string HeroId,HeroName,Personality;}internal class DialogueDay {public int GameDayIndex;public List<string> Lines=new();}internal class EventRecordEntry {public string EventId;}internal class EventImportPayload {public bool HasWorldSummaryFile,HasKingdomSummariesFile,HasEventRecordsFile;public Dictionary<string,string> KingdomSummaries=new();public List<EventRecordEntry> EventRecords=new();}}
internal class CompressedMemoryExportBundle {public string HeroId;public List<object> DailyDrafts=new(),Blocks=new(),SummaryQueue=new(),OverviewQueue=new();public object Overview;}
internal static class Logger {internal static void Log(string channel,string text){}}
internal static class NpcDataFileName {internal static string TryParseHeroId(string path)=>Path.GetFileNameWithoutExtension(path).Split("__")[0];}
internal class RewardSystemBehavior {internal static RewardSystemBehavior Instance=new();internal class DebtExportEntry {public int Amount;}internal Dictionary<string,DebtExportEntry> Snapshot=new();internal Dictionary<string,DebtExportEntry> LastExport,LastCommit;internal Dictionary<string,DebtExportEntry> ExportDebtEntries()=>LastExport=new(Snapshot);internal void ImportDebtEntries(Dictionary<string,DebtExportEntry> entries){TestState.Order.Add("debt");LastCommit=entries;Snapshot=entries;}}
internal static class VoiceMapper {internal static bool Fail;internal static int GetTotalVoiceCount()=>0;internal static string GetFallbackVoice()=>"";internal static bool ImportMappingFromFile(string path,bool overwriteExisting=true){TestState.Order.Add("voice");return !Fail;}internal static bool ImportMappingJson(string text,bool overwriteExisting=true){TestState.Order.Add("voice");return !Fail;}}
internal static class ShoutUtils {internal static bool HasUnnamedPersonaKey(string key)=>false;internal static void ImportUnnamedPersonaFromDir(string path,bool overwriteExisting=true){TestState.Order.Add("unnamed");}}
internal class KingdomStrategicProfileBehavior {internal static KingdomStrategicProfileBehavior Instance=new();internal bool InspectImportDirectory(string dir,out int total,out int duplicate,out int skipped,out string error){total=1;duplicate=skipped=0;error="";return true;}internal bool ImportAllFromDirectory(string dir,bool overwriteExisting,out string detail){TestState.Order.Add("kingdom");detail="fixture failure";return false;}}
internal class KnowledgeLibraryBehavior {internal static KnowledgeLibraryBehavior Instance=new();internal class LoreRule {public string Id;}internal class KnowledgeFile {public List<LoreRule> Rules=new();}internal List<string> GetRuleIdsForDev(int count)=>new();}
internal static class KnowledgeImportSupport {internal static KnowledgeLibraryBehavior.KnowledgeFile TryLoadKnowledgeRulesFromRuleFiles(string dir)=>null;}
internal static class PlayerExportsStore {internal static string ResolveImportFolderPath(string path)=>Path.GetFullPath(path);
	internal static T ReadJson<T>(string path) where T : class
	{
		try
		{
			if (!File.Exists(path))
			{
				return null;
			}
			string value = File.ReadAllText(path, Encoding.UTF8);
			if (string.IsNullOrWhiteSpace(value))
			{
				return null;
			}
			return JsonConvert.DeserializeObject<T>(value);
		}
		catch
		{
			return null;
		}
	}

}
}
