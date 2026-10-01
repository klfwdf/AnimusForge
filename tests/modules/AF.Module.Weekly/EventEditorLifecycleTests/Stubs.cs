using TaleWorlds.CampaignSystem;
using System; using System.Collections.Generic;
namespace TaleWorlds.CampaignSystem { public class Kingdom { public static List<Kingdom> All=new();public string StringId,Name;public Hero Leader;public Clan RulingClan; } public class Clan {public string Name;} public class Campaign {public static Campaign Current;public T GetCampaignBehavior<T>()=>default;} public class Hero { public static Hero FindFirst(Func<Hero,bool> match)=>null; public string Name="Hero", StringId="h"; } }
namespace AnimusForge.Refactor.Runtime { internal static class SaveRuntimeGuard { internal static long Generation=1; internal static long CaptureGeneration()=>Generation; internal static bool IsStale(long g,string context)=>g!=Generation; } }
namespace TaleWorlds.Library {
 public class InquiryElement { public object Identifier; public string Title;public InquiryElement(object id,string title,object image){Identifier=id;Title=title;} }
 public class InformationMessage { public InformationMessage(string text){} }
 public class InquiryData { public Action Confirm,Cancel; public InquiryData(string title,string text,bool isAffirmativeOptionShown,bool isNegativeOptionShown,string yes,string no,Action confirm,Action cancel){Confirm=confirm;Cancel=cancel;} }
 public class TextInquiryData { public Action<string> Confirm; public Action Cancel; public TextInquiryData(string title,string text,bool isAffirmativeOptionShown,bool isNegativeOptionShown,string yes,string no,Action<string> confirm,Action cancel,bool shouldInputBeObfuscated=false,object validator=null,string initial="",string currentValue=""){Confirm=confirm;Cancel=cancel;} }
 public class MultiSelectionInquiryData { public List<InquiryElement> Options;public string Description;public Action<List<InquiryElement>> Confirm; public Action Cancel; public MultiSelectionInquiryData(string title,string text,List<InquiryElement> options,bool isExitShown,int min,int max,string yes,string no,Action<List<InquiryElement>> confirm,Action cancel,string sound="",bool isSeachAvailable=false){Options=options;Description=text;Confirm=confirm;Cancel=cancel;} }
 public static class InformationManager { public static InquiryData Inquiry; public static TextInquiryData Text; public static void DisplayMessage(InformationMessage m){} public static void ShowInquiry(InquiryData d,bool pauseGameActiveState=false){Inquiry=d;} public static void ShowTextInquiry(TextInquiryData d){Text=d;} public static void HideInquiry(){} }
}
namespace TaleWorlds.Core { public static class MBInformationManager { public static TaleWorlds.Library.MultiSelectionInquiryData Last; public static void ShowMultiSelectionInquiry(TaleWorlds.Library.MultiSelectionInquiryData data){Last=data;} } }
namespace AnimusForge {
 internal static class Logger {internal static void Log(string a,string b){} }
 internal sealed class NpcActionEntry {}
 internal sealed class WorldBulletinPanelData { internal string EventId; }
 internal static class DevWeeklyReportPopup { internal static Action Reading; internal static bool Result=true; internal static bool ShowWorldBulletin(WorldBulletinPanelData data,double seconds,Action read){Reading=read;return Result;} }
}

namespace AnimusForge {internal static class PlayerKingdomRebellionImmunity {internal static bool Protected;internal static bool ShouldProtectKingdom(TaleWorlds.CampaignSystem.Kingdom kingdom)=>Protected;} internal static class DevTextEditorHelper {internal static Action<string> Confirm;internal static Action Cancel;internal static void ShowLongTextEditor(string title,string subtitle,string hint,string initial,Action<string> save,Action cancel){Confirm=save;Cancel=cancel;}} public partial class MyBehavior {internal enum ExportImportScope {EventData} internal sealed class DevKingdomSummaryMenuItem {public string KingdomId,DisplayName;} }}
namespace AnimusForge { public partial class MyBehavior {
	internal sealed class KingdomRebellionCandidateInfo
	{
		public Clan Clan;

		public string ClanId;

		public string ClanName;

		public int RelationToKing;

		public int TownCount;

		public int CastleCount;

		public int TotalFortificationCount;

		public int ClanTier;

		public float Score;

		public bool Eligible;

		public string Note;

		public List<string> PreviewFollowerClanNames = new List<string>();
	}

	internal sealed class KingdomRebellionFollowerInfo
	{
		public Clan Clan;

		public string ClanId;

		public string ClanName;

		public int RelationToKing;

		public int RelationToLeader;

		public int TownCount;

		public int CastleCount;

		public int ClanTier;

		public float Score;

		public bool Eligible;

		public string Note;
	}

	internal sealed class KingdomRebellionResolutionResult
	{
		public Kingdom Kingdom;

		public int WeekIndex;

		public bool Forced;

		public int StabilityValue;

		public string StabilityTierText;

		public float TriggerChance;

		public float? Roll;

		public bool PassedChanceGate;

		public Clan SelectedClan;

		public List<Clan> SelectedFollowerClans = new List<Clan>();

		public bool Executed;

		public string Message;

		public List<KingdomRebellionCandidateInfo> Candidates = new List<KingdomRebellionCandidateInfo>();

		public List<KingdomRebellionFollowerInfo> FollowerCandidates = new List<KingdomRebellionFollowerInfo>();
	}


} }
