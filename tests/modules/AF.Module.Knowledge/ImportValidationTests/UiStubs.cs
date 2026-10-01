using System; using System.Collections.Generic;
namespace TaleWorlds.CampaignSystem { public class Campaign {public static Campaign Current;public T GetCampaignBehavior<T>()=>default;} public class Hero { public static Hero FindFirst(Func<Hero,bool> match)=>null; public string Name="Hero", StringId="h"; } }
namespace AnimusForge.Refactor.Runtime { internal static class SaveRuntimeGuard { internal static long Generation=1; internal static long CaptureGeneration()=>Generation; internal static bool IsStale(long g,string context)=>g!=Generation; } }
namespace TaleWorlds.Library {
 public class InquiryElement { public object Identifier; public InquiryElement(object id,string title,object image){Identifier=id;} }
 public class InformationMessage { public InformationMessage(string text){} }
 public class InquiryData { public Action Confirm,Cancel; public InquiryData(string title,string text,bool isAffirmativeOptionShown,bool isNegativeOptionShown,string yes,string no,Action confirm,Action cancel){Confirm=confirm;Cancel=cancel;} }
 public class TextInquiryData { public Action<string> Confirm; public Action Cancel; public TextInquiryData(string title,string text,bool isAffirmativeOptionShown,bool isNegativeOptionShown,string yes,string no,Action<string> confirm,Action cancel,bool shouldInputBeObfuscated=false,object validator=null,string initial="",string currentValue=""){Confirm=confirm;Cancel=cancel;} }
 public class MultiSelectionInquiryData { public Action<List<InquiryElement>> Confirm; public Action Cancel; public MultiSelectionInquiryData(string title,string text,List<InquiryElement> options,bool isExitShown,int min,int max,string yes,string no,Action<List<InquiryElement>> confirm,Action cancel,string optional="",bool isSeachAvailable=false){Confirm=confirm;Cancel=cancel;} }
 public static class InformationManager { public static InquiryData Inquiry; public static TextInquiryData Text; public static void DisplayMessage(InformationMessage m){} public static void ShowInquiry(InquiryData d,bool pauseGameActiveState=false,bool prioritize=false){Inquiry=d;} public static void ShowTextInquiry(TextInquiryData d){Text=d;} public static void HideInquiry(){} }
}
namespace TaleWorlds.Core { public static class MBInformationManager { public static TaleWorlds.Library.MultiSelectionInquiryData Selection; public static void ShowMultiSelectionInquiry(TaleWorlds.Library.MultiSelectionInquiryData data,bool pauseGameActiveState=false){Selection=data;} } }
namespace AnimusForge {
 internal static class Logger { internal static void Log(string a,string b){} }
 internal static class DevTextEditorHelper { internal static Action<string> Confirm; internal static Action Cancel; internal static void ShowLongTextEditor(string title,string subtitle,string hint,string initial,Action<string> confirm,Action cancel,string save="save",string back="back"){Confirm=confirm;Cancel=cancel;} }
 internal static class DevLargeSelectionPopup { internal class Option { internal Option(string id,string title,string subtitle=null,string meta=null,bool isPrimary=false,bool isDanger=false){} } }
 internal static class EncyclopediaHeroPersonaPatch { internal static void QueueRefreshForHero(string id){} }
 internal static class LlmRetryPrompt { internal static string BuildFailureDetail(string error,string unused)=>error; }
}

namespace AnimusForge {
 internal static class PlayerExportsStore { internal static string Root; internal static string GetPlayerExportsRootPath()=>Root??throw new InvalidOperationException("fixture unavailable"); internal static string SanitizeFolderName(string name)=>(name??"").Trim(); internal static string ResolveExportFolderName(string name)=>name; internal static string ResolveImportFolderPath(string name)=>null; }
 internal static class NpcDataFileName { internal static string TryParseHeroId(string name)=>null; }
 internal static class ShoutUtils { internal class UnnamedPersonaIndexItem { public string Key,Label; } internal static List<UnnamedPersonaIndexItem> GetUnnamedPersonaIndexItemsForDev(int max)=>new(); internal static bool TryGetUnnamedPersonaByKey(string key,out string personality,out string background){personality=background="";return false;} internal static void SaveUnnamedPersonaByKey(string key,string personality,string background){} }
 internal static class VoiceMapper { internal static string[] AllGroupKeys=Array.Empty<string>(); internal static List<string> GetVoicesForGroup(string key)=>new(); internal static string GetFallbackVoice()=>""; internal static string GetGroupDisplayName(string key)=>key; internal static bool ImportMappingJson(string text,bool overwriteExisting=false,bool saveToFile=true)=>false; internal static void ReloadConfig(){} internal static void AddVoiceToGroup(string key,string value){} internal static void RemoveVoiceFromGroup(string key,string value){} internal static void SetFallbackVoice(string value){} }
}
