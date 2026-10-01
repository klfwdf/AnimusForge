using System; using System.Collections.Generic; using System.Linq; using Newtonsoft.Json;
namespace AnimusForge;
public class KnowledgeLibraryBehavior { public const int RagShortTextMaxLength=100;public Func<string,bool,bool> BulkImport,SingleImport;public bool ImportRulesJson(string json,bool overwrite)=>BulkImport?.Invoke(json,overwrite)??false;public bool ImportSingleRuleJson(string json,bool overwrite)=>SingleImport?.Invoke(json,overwrite)??false; public static bool IsPlayerPersonaRuleId(string id)=>id=="player"; public static KnowledgeLibraryBehavior Instance; public class RuleIndexItem { public string Id,Label; } public List<RuleIndexItem> GetRuleIndexItemsForDev(int max)=>new(); public void OpenEditorMenu(Action back){}
 public class LoreRule { public string Id;public List<string> RagShortTexts=new(); public List<string> Keywords = new(); }
 public class KnowledgeFile { public List<LoreRule> Rules = new(); }
 public KnowledgeFile Data = new(); public string ExportRulesJson()=>JsonConvert.SerializeObject(Data);
 public List<string> GetRuleIdsForDev(int limit)=>Data.Rules.Select(r=>r.Id).Take(limit).ToList();
}
internal static class KnowledgeImportSupport { internal static bool DuplicateConditions;internal static bool TryFindDuplicateKnowledgeVariantCondition(KnowledgeLibraryBehavior.LoreRule rule,out int first,out int second){first=0;second=1;return DuplicateConditions;}
 internal static List<KnowledgeLibraryBehavior.LoreRule> Imported = new();
 internal static List<KnowledgeLibraryBehavior.LoreRule> LoadKnowledgeRulesFromImportDir(string dir)=>Imported;
 internal static IEnumerable<string> GetKnowledgeKeywordsForCompare(KnowledgeLibraryBehavior.LoreRule rule)=>rule.Keywords.Select(KnowledgeImportValidationOwner.NormalizeKeywordForCompare).Where(x=>x.Length>0).Distinct(StringComparer.OrdinalIgnoreCase);
}
