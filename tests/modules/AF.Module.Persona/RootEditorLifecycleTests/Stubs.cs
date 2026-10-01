using System;using System.Collections.Generic;
namespace TaleWorlds.CampaignSystem {public class Hero {public static Hero MainHero;public static List<Hero> AllAliveHeroes=new();public string Name,StringId;public static Hero FindFirst(Func<Hero,bool> test)=>AllAliveHeroes.Find(h=>test(h));}}
namespace TaleWorlds.Library {
public class InquiryElement {public object Identifier;public string Title;public InquiryElement(object id,string title,object image){Identifier=id;Title=title;}}
public class InformationMessage {public InformationMessage(string text){}}
public class InquiryData {public Action Confirm,Cancel;public InquiryData(string title,string text,bool isAffirmativeOptionShown,bool isNegativeOptionShown,string yes,string no,Action confirm,Action cancel){Confirm=confirm;Cancel=cancel;}}
public class TextInquiryData {public Action<string> Confirm;public Action Cancel;public TextInquiryData(string title,string text,bool isAffirmativeOptionShown,bool isNegativeOptionShown,string yes,string no,Action<string> confirm,Action cancel,bool shouldInputBeObfuscated=false,object validator=null,string initial=""){Confirm=confirm;Cancel=cancel;}}
public class MultiSelectionInquiryData {public List<InquiryElement> Options;public Action<List<InquiryElement>> Confirm;public Action Cancel;public MultiSelectionInquiryData(string title,string text,List<InquiryElement> options,bool isExitShown,int min,int max,string yes,string no,Action<List<InquiryElement>> confirm,Action cancel){Options=options;Confirm=confirm;Cancel=cancel;}}
public static class InformationManager {public static InquiryData Inquiry;public static TextInquiryData Text;public static void DisplayMessage(InformationMessage m){}public static void ShowInquiry(InquiryData data,bool pauseGameActiveState=false){Inquiry=data;}public static void ShowTextInquiry(TextInquiryData data){Text=data;}}
}
namespace TaleWorlds.Core {public static class MBInformationManager {public static TaleWorlds.Library.MultiSelectionInquiryData Selection;public static void ShowMultiSelectionInquiry(TaleWorlds.Library.MultiSelectionInquiryData data){Selection=data;}}}
namespace AnimusForge {
internal static class MyBehavior {internal enum ExportImportScope {All,HeroNpcAll,PersonalityBackground,UnnamedPersona,DialogueHistory,Debt,EventData,Knowledge,VoiceMapping}internal static bool IsValidMemoryCommitMarker(string a,string b,string c)=>false;internal static bool IsMemoryRecoveryHexDigest(string value)=>false;}
internal static class Logger {internal static void Log(string channel,string message){}}
internal static class EncyclopediaHeroPersonaPatch {internal static void QueueRefreshForHero(string hero){}}
internal class RewardSystemBehavior {internal static RewardSystemBehavior Instance=new();internal int Writes;internal void GetDebtSnapshot(TaleWorlds.CampaignSystem.Hero hero,out int gold,out Dictionary<string,int> items){gold=3;items=new(){{"item",2}};}internal void SetDebt(TaleWorlds.CampaignSystem.Hero hero,int gold,Dictionary<string,int> items){Writes++;}internal string BuildDebtEditorSummary(TaleWorlds.CampaignSystem.Hero hero,int max)=>"debt";internal List<string> GetAllDebtorHeroIds()=>new();}
internal class KingdomStrategicProfileBehavior {internal static KingdomStrategicProfileBehavior Instance=new();internal int GetProfileCountForDev()=>1;internal int GetPlayerOverrideCountForDev()=>0;}
internal static class DevLargeSelectionPopup {internal class Option {internal string Id,TitleText,DetailText;internal Option(string id,string title,string detail=null,bool isDanger=false,bool isPrimary=false){Id=id;TitleText=title;DetailText=detail;}}internal static bool ShowText(string a,string b,string c,Action close,string caption)=>false;internal static bool Show(string a,string b,string c,List<Option> options,Action<string> select,Action cancel,string caption)=>false;}
internal static class DevWeeklyReportPopup {internal static bool Show(string title,string subtitle,string text,Action close,string caption)=>false;}
}

namespace TaleWorlds.Library {internal static class MBMath {internal static int ClampInt(int v,int min,int max)=>Math.Clamp(v,min,max);}internal static class TWParallel {internal static bool IsMainThread()=>true;}}
