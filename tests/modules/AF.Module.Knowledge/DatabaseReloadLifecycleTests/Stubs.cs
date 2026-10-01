using System;using System.Collections.Generic;
namespace TaleWorlds.CampaignSystem {public class Hero {public static Hero MainHero;public string StringId;} public class Campaign {public static Campaign Current;public T GetCampaignBehavior<T>()=>default;}}
namespace AnimusForge {
internal static class Logger {internal static void Log(string channel,string text){}}
internal class KingdomDatabaseReloadPlan {internal int CurrentProfilesResetToDefaultCount; internal Dictionary<string,string> SourceProfilesByTargetId=new();}
internal class KnowledgeLibraryBehavior {
 internal static KnowledgeLibraryBehavior Instance;internal class LoreRule {public string Id;public List<string> Keywords=new();} internal class KnowledgeFile {public List<LoreRule> Rules=new();} internal static bool IsPlayerPersonaRuleId(string id)=>id=="player"; internal bool TryValidateDatabaseRulesPreservingPlayer(List<LoreRule> rules,out int a,out int b,out string error){a=b=0;error="";return true;}
 internal bool SnapshotFailure,RejectReplace,RejectRestore;internal string State="old";internal List<string> Calls=new();
 internal List<string> GetRuleIdsForDev(int limit)=>new(); internal string ExportRulesJson(bool pretty=false)=>SnapshotFailure?null:State;
 internal bool ReplaceDatabaseRulesPreservingPlayer(List<LoreRule> source,out int removed,out int imported,out string error){Calls.Add("knowledge-write");removed=1;imported=source.Count;error="rejected";if(RejectReplace)return false;State="new";return true;}
 internal bool ImportRulesJson(string json,bool overwrite){Calls.Add("knowledge-restore");if(RejectRestore)return false;State=json;return true;}
}
internal class KingdomStrategicProfileBehavior {
 internal bool TryBuildDatabaseReloadPlan(string dir,out KingdomDatabaseReloadPlan plan,out string error){plan=new();error="";return true;} internal static KingdomStrategicProfileBehavior Instance;internal bool RejectApply,RejectRestore;internal string State="old";internal List<string> Calls=new();
 internal bool TryCaptureDatabaseReloadRollbackJson(out string json,out string error){json=State;error="";return true;}
 internal bool TryApplyDatabaseReloadPlan(KingdomDatabaseReloadPlan plan,out string detail){Calls.Add("kingdom-write");detail="kingdom";if(RejectApply)return false;State="new";return true;}
 internal bool TryRestoreDatabaseReloadRollbackJson(string json,out string error){Calls.Add("kingdom-restore");error="bad";if(RejectRestore)return false;State=json;return true;}
}
internal static class VoiceMapper {internal static string[] AllGroupKeys=new[]{"male_young"};internal static string State="old";internal static Queue<bool> Results=new();internal static string ExportMappingJson(bool pretty=false)=>State;internal static bool ImportMappingJson(string json,bool overwriteExisting,bool saveToFile){if(Results.Count>0&&!Results.Dequeue())return false;State=json;return true;}}
internal partial class MyBehavior {
 internal class NpcPersonaProfile {public string VoiceId="old",HeroId,HeroName,Personality,Background;}
 internal class EventRecordEntry {public string Id="old";}
 internal class EventImportPayload {public bool HasWorldSummaryFile,HasKingdomSummariesFile;public string WorldSummary;public Dictionary<string,string> KingdomSummaries=new(StringComparer.OrdinalIgnoreCase);}
 internal class DatabaseReloadPlan {public string ImportDirectory="fixture";public List<KnowledgeLibraryBehavior.LoreRule> KnowledgeRules=new();public string VoiceMappingJson="new";public Dictionary<string,string> NpcVoiceIds=new();public EventImportPayload OpeningKnowledge=new();public KingdomDatabaseReloadPlan KingdomProfilePlan=new();public int KnowledgeRuleIdDisambiguationCount,KnowledgeKeywordDeduplicationCount,CurrentKingdomProfilesResetToDefaultCount;}
 internal class DatabaseReloadRollbackSnapshot {public string KnowledgeJson,VoiceMappingJson,NpcPersonaProfilesJson,EventWorldOpeningSummary,EventRecordsJson,KingdomProfilesJson;public Dictionary<string,string> EventKingdomOpeningSummaries=new(StringComparer.OrdinalIgnoreCase);}
}
}
namespace AnimusForge {
internal static class PlayerExportsStore {internal static string ResolveImportFolderPath(string path)=>path;}
internal static class KnowledgeImportSupport {internal static List<KnowledgeLibraryBehavior.LoreRule> LoadKnowledgeRulesFromImportDir(string dir)=>new();internal static IEnumerable<string> GetKnowledgeKeywordsForCompare(KnowledgeLibraryBehavior.LoreRule rule)=>rule.Keywords.Select(KnowledgeImportValidationOwner.NormalizeKeywordForCompare);}
}
