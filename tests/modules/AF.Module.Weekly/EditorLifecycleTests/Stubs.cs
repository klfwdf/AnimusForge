using System; using System.Collections.Generic;
namespace TaleWorlds.CampaignSystem { public class Kingdom { public string StringId; } public class Campaign {public static Campaign Current;public T GetCampaignBehavior<T>()=>default;} public class Hero { public static Hero FindFirst(Func<Hero,bool> match)=>null; public string Name="Hero", StringId="h"; } }
namespace AnimusForge.Refactor.Runtime { internal static class SaveRuntimeGuard { internal static long Generation=1; internal static long CaptureGeneration()=>Generation; internal static bool IsStale(long g,string context)=>g!=Generation; } }
namespace TaleWorlds.Library {
 public class InquiryElement { public object Identifier; public InquiryElement(object id,string title,object image){Identifier=id;} }
 public class InformationMessage { public InformationMessage(string text){} }
 public class InquiryData { public Action Confirm,Cancel; public InquiryData(string title,string text,bool isAffirmativeOptionShown,bool isNegativeOptionShown,string yes,string no,Action confirm,Action cancel){Confirm=confirm;Cancel=cancel;} }
 public class TextInquiryData { public Action<string> Confirm; public Action Cancel; public TextInquiryData(string title,string text,bool isAffirmativeOptionShown,bool isNegativeOptionShown,string yes,string no,Action<string> confirm,Action cancel,bool shouldInputBeObfuscated=false,object validator=null,string initial="",string currentValue=""){Confirm=confirm;Cancel=cancel;} }
 public class MultiSelectionInquiryData { public Action<List<InquiryElement>> Confirm; public Action Cancel; public MultiSelectionInquiryData(string title,string text,List<InquiryElement> options,bool isExitShown,int min,int max,string yes,string no,Action<List<InquiryElement>> confirm,Action cancel){Confirm=confirm;Cancel=cancel;} }
 public static class InformationManager { public static InquiryData Inquiry; public static TextInquiryData Text; public static void DisplayMessage(InformationMessage m){} public static void ShowInquiry(InquiryData d,bool pauseGameActiveState=false){Inquiry=d;} public static void ShowTextInquiry(TextInquiryData d){Text=d;} public static void HideInquiry(){} }
}
namespace TaleWorlds.Core { public static class MBInformationManager { public static TaleWorlds.Library.MultiSelectionInquiryData Last; public static void ShowMultiSelectionInquiry(TaleWorlds.Library.MultiSelectionInquiryData data){Last=data;} } }
namespace AnimusForge {
 internal static class Logger {internal static void Log(string a,string b){} }
 internal sealed class NpcActionEntry {}
 internal sealed class WorldBulletinPanelData { internal string EventId; }
 internal static class DevWeeklyReportPopup { internal static Action Reading; internal static bool Result=true; internal static bool ShowWorldBulletin(WorldBulletinPanelData data,double seconds,Action read){Reading=read;return Result;} }
}
