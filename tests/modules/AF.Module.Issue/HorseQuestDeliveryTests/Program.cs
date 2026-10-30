using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

class Program
{
    const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
    static Hero player, giver;
    static MobileParty party;
    static ItemObject mount;
    static ItemRoster roster;
    static int stock, moved, gold, finalized, checks;
    static bool failGold;
    static string resource;
    static Type owner, questType;
    static object configuredRules;
    static bool Rules(ref object __result){__result=configuredRules;return false;}
    static object Raw(Type t)=>FormatterServices.GetUninitializedObject(t);
    static void Set(object o,string field,object value)=>AccessTools.Field(o.GetType(),field).SetValue(o,value);
    static object Call(string name,params object[] args)=>owner.GetMethod(name,All).Invoke(null,args);
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
    static bool MainHero(ref Hero __result){__result=player;return false;}
    static bool MainParty(ref MobileParty __result){__result=party;return false;}
    static bool Roster(ref ItemRoster __result){__result=roster;return false;}
    static bool Count(ref int __result){__result=stock;return false;}
    static bool Enumerate(ref IEnumerator<ItemRosterElement> __result){__result=new List<ItemRosterElement>{new ItemRosterElement(mount,stock)}.GetEnumerator();return false;}
    static bool Give(object[] __args){int n=(int)__args[3];moved+=n;stock-=n;return false;}
    static bool Gold(int amount){if(failGold)throw new InvalidOperationException("fixture failure");gold+=amount;return false;}
    static bool Noop()=>false;
    static bool Complete(QuestBase __instance){AccessTools.DeclaredMethod(questType,"OnCompleteWithSuccess").Invoke(__instance,null);Set(__instance,"_questState",Enum.Parse(AccessTools.Field(typeof(QuestBase),"_questState").FieldType,"Finalized"));finalized++;return false;}
    static bool AddLog(QuestBase __instance,object[] __args,ref JournalLog __result){__result=new JournalLog(default(CampaignTime),(TextObject)__args[0]);((MBList<JournalLog>)AccessTools.Field(typeof(QuestBase),"_journalEntries").GetValue(__instance)).Add(__result);return false;}
    static bool AddDiscrete(QuestBase __instance,object[] __args,ref JournalLog __result){__result=new JournalLog(default(CampaignTime),(TextObject)__args[0],(TextObject)__args[1],(int)__args[2],(int)__args[3],LogType.Discreate);((MBList<JournalLog>)AccessTools.Field(typeof(QuestBase),"_journalEntries").GetValue(__instance)).Add(__result);return false;}
    static bool Properties(string tag,CharacterObject character,TextObject parent,ref TextObject __result){__result=new TextObject(character==giver.CharacterObject?"Alice":"Other");__result.SetTextVariable("NAME",character==giver.CharacterObject?"Alice":"Other");parent?.SetTextVariable(tag,__result);return false;}
    static bool Path(string fileName,ref string __result){__result=System.IO.Path.Combine(resource,fileName);return false;}
    static bool Camel(ref bool __result){__result=false;return false;}
    static QuestBase NewQuest(){var q=(QuestBase)Raw(questType);Set(q,"_questGiver",giver);Set(q,"_journalEntries",new MBList<JournalLog>());Set(q,"RewardGold",100);Set(q,"_numMountsToBeDelivered",5);Set(q,"_mountObjectToBeDelivered",mount);var issue=(IssueBase)Raw(typeof(IssueBase).Assembly.GetType("TaleWorlds.CampaignSystem.Issues.LordNeedsHorsesIssueBehavior+LordNeedsHorsesIssue",true));Set(issue,"_issueOwner",giver);AccessTools.Property(typeof(IssueBase),"IssueQuest").SetValue(issue,q);Set(issue,"_issueState",Enum.Parse(AccessTools.Field(typeof(IssueBase),"_issueState").FieldType,"SolvingWithQuestSolution"));AccessTools.Property(typeof(Hero),"Issue").SetValue(giver,issue);stock=moved=gold=finalized=0;failGold=false;return q;}
    static void Patch(Harmony h,MethodBase method,string prefix){Console.WriteLine("fixture patch "+method+" -> "+prefix);h.Patch(method,prefix:new HarmonyMethod(typeof(Program),prefix));}
    static void Run(string[] args)
    {
        var af=Assembly.LoadFrom(args[1]);resource=args[2];owner=af.GetType("AnimusForge.HorseQuestDeliveryOwner",true);questType=(Type)owner.GetField("QuestType",All).GetValue(null);
        player=(Hero)Raw(typeof(Hero));giver=(Hero)Raw(typeof(Hero));party=(MobileParty)Raw(typeof(MobileParty));roster=new ItemRoster();mount=new ItemObject("fixture_horse");
        Set(giver,"_characterObject",Raw(typeof(CharacterObject)));Set(player,"_characterObject",Raw(typeof(CharacterObject)));
        var h=new Harmony("AF.HorseQuestDeliveryTests");
        Patch(h,AccessTools.PropertyGetter(typeof(Hero),"MainHero"),nameof(MainHero));Patch(h,AccessTools.PropertyGetter(typeof(MobileParty),"MainParty"),nameof(MainParty));Patch(h,AccessTools.PropertyGetter(typeof(MobileParty),"ItemRoster"),nameof(Roster));
        Patch(h,AccessTools.Method(typeof(ItemRoster),"GetItemNumber",new[]{typeof(ItemObject)}),nameof(Count));Patch(h,AccessTools.Method(typeof(ItemRoster),"GetEnumerator"),nameof(Enumerate));Patch(h,AccessTools.Method(questType,"GiveMounts"),nameof(Give));
        Patch(h,AccessTools.Method(typeof(GiveGoldAction),"ApplyBetweenCharacters"),nameof(Gold));Patch(h,AccessTools.Method(typeof(GainRenownAction),"Apply"),nameof(Noop));Patch(h,AccessTools.Method(typeof(QuestBase),"CompleteQuestWithSuccess"),nameof(Complete));
        Patch(h,AccessTools.Method(typeof(QuestBase),"AddLog",new[]{typeof(TextObject),typeof(bool)}),nameof(AddLog));Patch(h,AccessTools.Method(typeof(QuestBase),"AddDiscreteLog"),nameof(AddDiscrete));Patch(h,AccessTools.Method(typeof(StringHelpers),"SetCharacterProperties"),nameof(Properties));Patch(h,AccessTools.Method(questType.DeclaringType,"IsMountCamel"),nameof(Camel));
        Patch(h,af.GetType("AnimusForge.AnimusForgeModulePaths").GetMethod("GetModuleDataFilePath"),nameof(Path));
        h.CreateClassProcessor(af.GetType("AnimusForge.HorseQuestSettlementPatch",true)).Patch();
        h.CreateClassProcessor(af.GetType("AnimusForge.IssueQuestPanelTextBindingPatch",true)).Patch();
        h.CreateClassProcessor(af.GetType("AnimusForge.ArmySuppliesDescriptionBindingPatch",true)).Patch();
        Check(true,"all production Harmony targets admitted");
        var config=af.GetType("AnimusForge.AIConfigHandler",true).GetMethod("GetGuardrailRulePostprocessRules",All);
        var ruleType=config.ReturnType.GetGenericArguments()[0];configuredRules=Activator.CreateInstance(config.ReturnType);
        var rule=Activator.CreateInstance(ruleType);AccessTools.Property(ruleType,"Tag").SetValue(rule,"[ACTION:QUEST_TURN_IN]");AccessTools.Property(ruleType,"Description").SetValue(rule,"Confirm actual task completion.");((System.Collections.IList)configuredRules).Add(rule);
        Patch(h,config,nameof(Rules));
        var q=NewQuest();Call("RecordGiven",giver,"wrong",5);Check((int)Call("Observed",q,"AF_ISSUE_HorseDelivered")==0,"wrong mount not credited");
        Call("RecordGiven",giver,"fixture_horse",2);Call("RecordGiven",giver,"fixture_horse",1);Check((int)Call("Observed",q,"AF_ISSUE_HorseDelivered")==3,"split handover accumulates");Check(!(bool)Call("IsReady",q),"short handover not ready");stock=2;Check((bool)Call("IsReady",q),"delivered plus inventory ready");
        Call("RecordPayment",giver,player,40);Check((int)Call("Observed",q,"AF_ISSUE_HorsePaymentObserved")==40,"actual payment observed");
        var bridge=af.GetType("AnimusForge.VanillaIssueOfferBridge",true);
        var rules=bridge.GetMethod("BuildRuntimePostprocessRulesForExternal",All).Invoke(null,new object[]{giver});
        Check(((System.Collections.IList)rules).Count==1,"ready state produces actual task turn-in rule without conversation manager");
        string description=(string)AccessTools.Property(ruleType,"Description").GetValue(((System.Collections.IList)rules)[0]);
        Check(description.Contains("fixture_horse")&&description.Contains("40"),"actual postprocess rule carries delivery/payment evidence");
        var normalizer=af.GetTypes().Select(t=>t.GetMethod("NormalizeVanillaIssuePostprocessTagsForScene",All)).First(x=>x!=null);
        Check(((string)normalizer.Invoke(null,new[]{(object)"[ACTION:QUEST_TURN_IN]",rules})).Contains("[ACTION:QUEST_TURN_IN]"),"RAW task tag survives actual normalization");
        string evidence=(string)Call("Evidence",q);Check(evidence.Contains("fixture_horse")&&evidence.Contains("40"),"runtime concrete evidence includes item and payment");
        Check((bool)Call("TryComplete",giver),"real native completion reached");Check(moved==2&&gold==60&&finalized==1,"native completion only transfers outstanding mounts and reward");Check((int)Call("Required",q)==5&&q.RewardGold==100,"temporary native fields restored");Check(!(bool)Call("TryComplete",giver)&&gold==60,"repeated completion has no effects");
        q=NewQuest();Call("RecordGiven",giver,"fixture_horse",5);Call("RecordPayment",giver,player,100);Check((bool)Call("TryComplete",giver)&&moved==0&&gold==0,"fully delivered and paid completes without repeat transfer");
        q=NewQuest();Call("RecordGiven",giver,"fixture_horse",2);stock=3;failGold=true;try{Call("TryComplete",giver);}catch(TargetInvocationException){}Check((int)Call("Required",q)==5&&q.RewardGold==100,"native exception restores original fields");
        var text=new TextObject("{=fixture}{QUEST_GIVER.NAME}");af.GetType("AnimusForge.IssueQuestTextBinding",true).GetMethod("Bind",All).Invoke(null,new object[]{text,giver});MBTextManager.SetTextVariable("QUEST_GIVER",new TextObject("Wrong"));Check(text.ToString()=="Alice","local task person survives global variable changes");
        Check(q.JournalEntries.Count(x=>x.TaskName?.GetID()=="AF_ISSUE_HorseDelivered")==1,"one native journal observation per task");
        q=NewQuest();Call("RecordGiven",giver,"fixture_horse",5);Call("RecordPayment",giver,player,40);Check((int)Call("ConfirmedPayment",q)==0,"ordinary observed receipt is not a confirmed quest payment");q.CompleteQuestWithSuccess();Check(gold==100,"native UI completion does not deduct unconfirmed payment");
        q=NewQuest();var other=(Hero)Raw(typeof(Hero));Call("RecordGiven",other,"fixture_horse",5);Check((int)Call("Observed",q,"AF_ISSUE_HorseDelivered")==0,"another recipient cannot credit giver quest");
        Console.WriteLine("PASS assertions="+checks+" candidate="+args[1]+"; actual native settlement body; managed completion/engine fixtures; no live game/save/provider");
    }
    static int Main(string[] args){try{var dirs=args[0].Split('|');AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var file=new AssemblyName(e.Name).Name+".dll";foreach(var dir in dirs)if(File.Exists(System.IO.Path.Combine(dir,file)))return Assembly.LoadFrom(System.IO.Path.Combine(dir,file));return null;};Run(args);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
