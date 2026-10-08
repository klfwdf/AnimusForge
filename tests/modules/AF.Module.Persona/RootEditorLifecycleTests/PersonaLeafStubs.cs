using System;using System.Collections.Generic;
namespace AnimusForge.Refactor.Runtime {internal sealed class PersonaEditorFixtureNamespace {}}
namespace TaleWorlds.CampaignSystem {internal sealed class Campaign {internal static Campaign Current;internal T GetCampaignBehavior<T>() where T:class=>null;}}
namespace AnimusForge {
internal static partial class MyBehavior {internal sealed class NpcPersonaProfile {public string Personality="",Background="",VoiceId="";}}
internal static class LlmRetryPrompt {internal static string BuildFailureDetail(string a,string b)=>a;}
internal static class DevTextEditorHelper {internal static Action<string> Confirm;internal static void ShowLongTextEditor(string title,string current,string prompt,string initial,Action<string> confirm,Action cancel){Confirm=confirm;}}
internal sealed class KnowledgeLibraryBehavior {internal static KnowledgeLibraryBehavior Instance;internal sealed class RuleIndexItem {internal string Id,Name,Preview,Label;}internal void OpenEditorMenu(Action close){}internal List<RuleIndexItem> GetRuleIndexItemsForDev(int max)=>new();}
internal static class PlayerExportsStore {internal static string ResolveImportFolderPath(string folder)=>folder;}
internal static class NpcDataFileName {internal static string TryParseHeroId(string path)=>path;}
internal static class VoiceMapper {internal static string[] AllGroupKeys=Array.Empty<string>();internal static List<string> GetVoicesForGroup(string group)=>new();internal static string GetFallbackVoice()=>"";internal static void ReloadConfig(){}internal static void ImportMappingJson(string json,bool overwriteExisting,bool saveToFile){}internal static string GetGroupDisplayName(string key)=>key;internal static bool AddVoiceToGroup(string group,string voice)=>true;internal static bool RemoveVoiceFromGroup(string group,string voice)=>true;internal static void SetFallbackVoice(string voice){}}
internal static class ShoutUtils {internal sealed class UnnamedPersonaIndexItem {internal string Key,Name,Preview,Label;}internal static List<UnnamedPersonaIndexItem> GetUnnamedPersonaIndexItemsForDev(int max)=>new();internal static bool TryGetUnnamedPersonaByKey(string key,out string personality,out string background){personality=background="";return false;}internal static void SaveUnnamedPersonaByKey(string key,string personality,string background){}}
}
