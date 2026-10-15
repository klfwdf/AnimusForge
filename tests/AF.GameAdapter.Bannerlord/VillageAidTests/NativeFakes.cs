// Native UI/rosters and neighboring owner boundaries only. Aid rules, callback, transfer and rewards are production source.
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
namespace TaleWorlds.Core {
    public class HorseComponent { public bool IsLiveStock; }
    public class ItemObject {
        public string StringId, Name; public bool IsFood, IsTradeGood; public HorseComponent HorseComponent;
        public bool HasHorseComponent => HorseComponent != null;
    }
    public readonly record struct EquipmentElement(ItemObject Item, string Modifier = "", int ItemValue = 0) {
        public object ItemModifier => Modifier;
        public bool IsEqualTo(EquipmentElement other) => Item == other.Item && Modifier == other.Modifier;
        public string GetModifiedItemName() => Modifier + Item.Name;
    }
    public static class MBRandom { public static float RandomFloat => .5f; }
    public sealed class InquiryData(string title, string text, bool a, bool b, string yes, string no, Action confirm, Action cancel) {
        public string Title = title, Text = text, Yes = yes, No = no; public bool HasYes = a, HasNo = b; public Action Confirm = confirm, Cancel = cancel;
    }
    public sealed class InformationMessage(string text) { public string Text = text; }
    public static class InformationManager {
        public static InquiryData Last; public static string Message; public static bool ThrowOpen;
        public static void ShowInquiry(InquiryData inquiry, bool pause) { if(ThrowOpen) throw new Exception("UI failed"); Last = inquiry; }
        public static void DisplayMessage(InformationMessage message) => Message = message.Text;
    }
}
namespace TaleWorlds.Library { public class Dummy {} }
namespace TaleWorlds.CampaignSystem {
    public class Hero { public static Hero MainHero; public int Gold, Relation; public bool IsAlive = true; }
    public class Clan { public Hero Leader; }
}
namespace TaleWorlds.CampaignSystem.Roster {
    public readonly record struct ItemRosterElement(EquipmentElement EquipmentElement, int Amount);
    public class ItemRoster : IEnumerable<ItemRosterElement> {
        private readonly List<ItemRosterElement> items = new();
        public bool ThrowNextAfterMutation;
        public void AddToCounts(EquipmentElement item, int delta) {
            int i=items.FindIndex(e=>e.EquipmentElement.IsEqualTo(item)); int before=i<0?0:items[i].Amount;
            if((long)before+delta<0 || (long)before+delta>int.MaxValue) throw new Exception("invalid inventory mutation");
            if(i>=0) items.RemoveAt(i); if(before+delta>0) items.Add(new(item,before+delta));
            if(ThrowNextAfterMutation) {ThrowNextAfterMutation=false;throw new Exception("native hook failed after mutation");}
        }
        public int Count(EquipmentElement item) => items.Where(e=>e.EquipmentElement.IsEqualTo(item)).Sum(e=>e.Amount);
        public IEnumerator<ItemRosterElement> GetEnumerator() => items.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
namespace TaleWorlds.CampaignSystem.Party { public class MobileParty { public static MobileParty MainParty; public ItemRoster ItemRoster = new(); } }
namespace TaleWorlds.CampaignSystem.Settlements {
    public class Village { public Settlement Bound; }
    public class Settlement { public string StringId; public Village Village = new(); public Clan OwnerClan; public List<Hero> Notables = new(); public ItemRoster ItemRoster = new(); }
}
namespace TaleWorlds.CampaignSystem.Actions {
    public static class GiveGoldAction { public static void ApplyBetweenCharacters(Hero from, Hero to, int amount,bool flag) => from.Gold -= amount; }
    public static class ChangeRelationAction { public static void ApplyPlayerRelation(Hero target,int delta,bool a,bool b) => target.Relation += delta; }
}
namespace AnimusForge {
    public static class Logger { public static List<string> Messages=new(); public static void Log(string category,string text)=>Messages.Add(text); }
    public sealed partial class VillageAftermathBehavior {
        private static VillageAftermathBehavior EconomyOwner; private static Settlement Current;
        private static object _activeMission; private static bool Active, Friendly, _giftInquiryActive;
        private static long _giftInquirySequence; private static readonly VillageDepartureDecision Departure = new();
        private Dictionary<string,int> _aidLastDay=new(), _aidDeliveryRemainder=new(), _aidRoundDay=new(), _aidRoundMask=new(), _aidBonuses=new(), _aidBonusExpires=new();
        private readonly Dictionary<string,float> _aidMultiplier=new();
        private readonly Dictionary<string,int> points=new();
        private VillageAssistanceStore Assistance => new(points);
        private const int InitialVillageAssistance=0;
        public static int CurrentDay; public static bool English;
        public static List<string> Facts=new(); public static string Failure;
        private static string VUi(string cn,string en)=>English?en:cn;
        private static string VP(string cn,string en)=>English?en:cn;
        private static Settlement ResolveActiveVillage()=>Current;
        private static bool IsActive()=>Active;
        private static bool CanEnterFriendlyVillage(Settlement village)=>village==Current && Friendly;
        private static void TryDisplayFailureMessage(string text)=>Failure=text;
        private static void RecordForeignFact(Settlement village,string fact,string kind,bool flag)=>Facts.Add(fact);
        public static VillageAftermathBehavior Reset() {
            var owner=new VillageAftermathBehavior();EconomyOwner=owner;_activeMission=new();Active=Friendly=true;_giftInquiryActive=false;
            Current=new(){StringId="village_A",Village=new(){Bound=new(){OwnerClan=new(){Leader=new()}}},Notables=new(){new()}};
            Hero.MainHero=new(){Gold=1000000}; MobileParty.MainParty=new();CurrentDay=0;Facts.Clear();Departure.Reset();
            InformationManager.Last=null;InformationManager.ThrowOpen=false; Failure="";English=false;
            return owner;
        }
        public bool Open(string tag)=>OpenVillageGift(FriendlyVillageActionCatalog.ExtractInvocations(tag).Single());
        public void End()=>_giftInquiryActive=false;
        public void NewMission()=>_activeMission=new();
        public void SetFriendly(bool value)=>Friendly=value;
        public void SetActive(bool value)=>Active=value;
        public int Cooperation=>Assistance.Get(Current.StringId,0);
        public string SaveRemainder()=>System.Text.Json.JsonSerializer.Serialize(_aidDeliveryRemainder);
        public void LoadRemainder(string json)=>_aidDeliveryRemainder=System.Text.Json.JsonSerializer.Deserialize<Dictionary<string,int>>(json);
        public float Multiplier { get {RefreshAidMultipliers();return _aidMultiplier.TryGetValue(Current.StringId,out var f)?f:1;} }
        public int BonusCount=>_aidBonuses.Count;
        public int Expiry=>_aidBonusExpires.Values.Single();
        public int Remainder=>_aidDeliveryRemainder.TryGetValue(Current.StringId,out int r)?r:0;
        public static Settlement Village=>Current;
        public static ItemRoster Inventory=>MobileParty.MainParty.ItemRoster;
        public static bool Pending=>_giftInquiryActive;
        public bool Disposition=>Departure.HasDisposition;
        public void SeedLegacyCooldown(int kind)=>_aidLastDay[Current.StringId+":"+kind]=CurrentDay;
        public void VisitOther()=>Current=new(){StringId="village_B",Village=Current.Village};
    }
}
