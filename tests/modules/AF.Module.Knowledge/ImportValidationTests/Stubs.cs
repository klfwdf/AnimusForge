using System; using System.Collections.Generic; using System.Linq; using Newtonsoft.Json;
namespace AnimusForge;
public class KnowledgeLibraryBehavior { public static KnowledgeLibraryBehavior Instance; public class RuleIndexItem { public string Id,Label; } public List<RuleIndexItem> GetRuleIndexItemsForDev(int max)=>new(); public void OpenEditorMenu(Action back){}
 public class LoreRule { public string Id; public List<string> Keywords = new(); }
 public class KnowledgeFile { public List<LoreRule> Rules = new(); }
 public KnowledgeFile Data = new(); public string ExportRulesJson()=>JsonConvert.SerializeObject(Data);
 public List<string> GetRuleIdsForDev(int limit)=>Data.Rules.Select(r=>r.Id).Take(limit).ToList();
}
internal static class KnowledgeImportSupport {
 internal static List<KnowledgeLibraryBehavior.LoreRule> Imported = new();
 internal static List<KnowledgeLibraryBehavior.LoreRule> LoadKnowledgeRulesFromImportDir(string dir)=>Imported;
 internal static IEnumerable<string> GetKnowledgeKeywordsForCompare(KnowledgeLibraryBehavior.LoreRule rule)=>rule.Keywords.Select(KnowledgeImportValidationOwner.NormalizeKeywordForCompare).Where(x=>x.Length>0).Distinct(StringComparer.OrdinalIgnoreCase);
}
