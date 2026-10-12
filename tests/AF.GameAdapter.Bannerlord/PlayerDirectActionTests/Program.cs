using AnimusForge;
using TaleWorlds.Library;

int checks=0;
void Check(bool value, string name) { checks++; if (!value) throw new Exception(name); }
var expected = new Dictionary<PlayerDirectActionKind,string> {
 [PlayerDirectActionKind.Gold]="[ACTION:GIVE_ASSET:GOLD:23]", [PlayerDirectActionKind.Item]="[ACTION:GIVE_ASSET:grain:23]",
 [PlayerDirectActionKind.FixedAsset]="[ACTION:GIVE_ASSET:town_1:1]", [PlayerDirectActionKind.Troops]="[ATT:2:23]",
 [PlayerDirectActionKind.Prisoners]="[ATP:2:23]", [PlayerDirectActionKind.Debt]="[AD:23:0:P:承诺]",
 [PlayerDirectActionKind.ResolveDebt]="[ADP:debt_1]", [PlayerDirectActionKind.Companion]="[A:H_J_P_P_C]",
 [PlayerDirectActionKind.Family]="[A:H_J_P_P_L]", [PlayerDirectActionKind.Follow]="[ACTION:SCENE_FOLLOW_PLAYER]",
 [PlayerDirectActionKind.StopFollow]="[ACTION:SCENE_STOP_FOLLOW]", [PlayerDirectActionKind.LordsHall]="[ACTION:OPEN_LORDS_HALL]",
 [PlayerDirectActionKind.AcceptIssue]="[ACTION:ISSUE_ACCEPT_SELF]", [PlayerDirectActionKind.TurnInQuest]="[ACTION:QUEST_TURN_IN]" };
Check(PlayerDirectActionProtocol.Actions.Count == 14, "all 14 actions");
foreach (var pair in expected) {
 string token=pair.Key switch { PlayerDirectActionKind.Item=>"grain", PlayerDirectActionKind.FixedAsset=>"town_1",
  PlayerDirectActionKind.Troops or PlayerDirectActionKind.Prisoners=>"2", PlayerDirectActionKind.ResolveDebt=>"debt_1", _=>null };
 Check(PlayerDirectActionProtocol.TryBuild(pair.Key, token, "23", 0, "承诺", out var tag) && tag == pair.Value, pair.Key.ToString());
}
foreach (var kind in new[]{PlayerDirectActionKind.Item,PlayerDirectActionKind.Troops,PlayerDirectActionKind.Prisoners}) {
 Check(PlayerDirectActionProtocol.TryBuild(kind,kind==PlayerDirectActionKind.Item?"grain":"2","ALL",0,null,out var tag) && tag.EndsWith(":ALL]"), "single ALL");
 Check(kind==PlayerDirectActionKind.Item ? !PlayerDirectActionProtocol.TryBuild(kind,"ALL","ALL",0,null,out tag)
     : PlayerDirectActionProtocol.TryBuild(kind,"ALL","ALL",0,null,out tag) && tag.Contains(":ALL:ALL]"), "global ALL only supported roster tags");
 Check(!PlayerDirectActionProtocol.TryBuild(kind,"ALL","3",0,null,out _), "no mixed all");
}
foreach (string amount in new[]{"0","-1","2147483648","ALL","1][ADP:x]"})
 Check(!PlayerDirectActionProtocol.TryBuild(PlayerDirectActionKind.Gold,null,amount,0,null,out _), "invalid money");
Check(PlayerDirectActionProtocol.TryBuild(PlayerDirectActionKind.Gold,null,"2147483647",0,null,out _), "no new money cap");
Check(!PlayerDirectActionProtocol.TryBuild(PlayerDirectActionKind.Debt,null,"4",-1,"note",out _),"negative deadline");
Check(!PlayerDirectActionProtocol.TryBuild(PlayerDirectActionKind.Debt,null,"4",0,"x][ACTION:QUEST_TURN_IN]",out _),"note injection");
Check(!PlayerDirectActionProtocol.TryBuild(PlayerDirectActionKind.ResolveDebt,"x][ADP:y]",null,0,null,out _),"id injection");
Check(!PlayerDirectActionProtocol.TryBuild(PlayerDirectActionKind.Item,"grain:1][ACTION:QUEST_TURN_IN]","2",0,null,out _),"candidate injection");

bool current=true, candidateValid=true; int finished=0, executions=0; string executed=null;
var controller=new PlayerDirectActionController();
void Open() => controller.Open("NPC",()=>current,_=>null,
 kind=>new List<PlayerDirectActionChoice>{new("2","candidate"),new("ALL","全部",batchAssetTokens:new[]{"grain","fish"})},
 (kind,choice)=>candidateValid, (kind,choice,tag)=>{Check(finished>0,"release controls before execution");executions++;executed=tag;},()=>finished++);
void Select(object id) { var p=(MultiSelectionInquiryData)InformationManager.Popup; p.AffirmativeAction(new(){p.Elements.Single(e=>Equals(e.Identifier,id))}); }
void Input(string value) => ((TextInquiryData)InformationManager.Popup).AffirmativeAction(value);
foreach (var kind in expected.Keys) {
 finished=0; Open(); Select(kind);
 switch(kind) {
 case PlayerDirectActionKind.Gold: Input("23"); break;
 case PlayerDirectActionKind.Item: case PlayerDirectActionKind.Troops: case PlayerDirectActionKind.Prisoners: Select("2"); Select("quantity"); Input("23"); break;
 case PlayerDirectActionKind.FixedAsset: Select("2"); ((InquiryData)InformationManager.Popup).AffirmativeAction(); break;
 case PlayerDirectActionKind.ResolveDebt: Select("2"); break;
 case PlayerDirectActionKind.Debt: Input("23"); Input("0"); Input("承诺"); break;
 case PlayerDirectActionKind.Companion: case PlayerDirectActionKind.Family: ((InquiryData)InformationManager.Popup).AffirmativeAction(); break;
 }
 Check(finished==1,"finish once " + kind);
}
Open(); Select(PlayerDirectActionKind.Gold); var money=(TextInquiryData)InformationManager.Popup; int before=executions;
money.AffirmativeAction("7"); money.AffirmativeAction("7"); money.NegativeAction(); Check(executions==before+1,"double affirmative consumed once");
Open(); var menu=(MultiSelectionInquiryData)InformationManager.Popup; before=executions;
menu.NegativeAction(null); menu.AffirmativeAction(new(){menu.Elements[0]}); Check(executions==before,"cancel never executes");
Open(); Select(PlayerDirectActionKind.Gold); money=(TextInquiryData)InformationManager.Popup; current=false; before=executions;
money.AffirmativeAction("7"); Check(executions==before,"stale target rejects"); current=true;
Open(); Select(PlayerDirectActionKind.Item); Select("2"); Select("quantity"); candidateValid=false; before=executions; Input("7");
Check(executions==before,"stale candidate rejects before owner fallback"); candidateValid=true;
Open(); Select(PlayerDirectActionKind.Item); Select("ALL"); Check(executed=="[ACTION:GIVE_ASSET:grain:ALL] [ACTION:GIVE_ASSET:fish:ALL]","ordinary items batch uses supported original tags");
Open(); Select(PlayerDirectActionKind.Gold); money=(TextInquiryData)InformationManager.Popup;
Open(); before=executions; money.AffirmativeAction("8"); Check(executions==before,"superseded popup rejects");
((MultiSelectionInquiryData)InformationManager.Popup).NegativeAction(null);
int finishedBefore=finished;
controller.Open("NPC",()=>true,_=>null,_=>throw new InvalidOperationException("candidate read failed"),
    (kind,choice)=>true,(kind,choice,tag)=>executions++,()=>finished++);
before=executions;Select(PlayerDirectActionKind.Item);
Check(finished==finishedBefore+1 && executions==before,"candidate exception restores controls without retry or effects");
Integration.Run(Check);
var root=new DirectoryInfo(AppContext.BaseDirectory);
while(root!=null && !File.Exists(Path.Combine(root.FullName,"AnimusForge.csproj")))root=root.Parent;
Check(root!=null,"repository root");
var xml=System.Xml.Linq.XDocument.Load(Path.Combine(root.FullName,"content/modules/AF.Module.Conversation/GUI/Prefabs/AnimusForgeNativeConversationOverlay.xml"));
var button=xml.Descendants("ButtonWidget").Single(x=>(string)x.Attribute("Command.Click")=="OpenDirectActions");
Check((string)button.Attribute("MarginTop")=="30" && (string)button.Attribute("IsEnabled")=="@CanOpenDirectActions","native top-right binding");
Check((string)button.Descendants("TextWidget").Single().Attribute("DoNotAcceptEvents")=="true","label does not steal clicks");
Check(xml.Descendants("ButtonWidget").Count()==7,"all six original buttons retained");
var scene=File.ReadAllText(Path.Combine(root.FullName,"src/AF.GameAdapter.Bannerlord/Scene/SceneShoutInputController.cs"));
var menuStart=scene.IndexOf("internal void TriggerShout()",StringComparison.Ordinal);
var directBranch=scene.IndexOf("if (text2 == \"direct_actions\")",menuStart,StringComparison.Ordinal);
Check(scene.IndexOf("requireSetupReady: false",menuStart,StringComparison.Ordinal)<directBranch,"Y menu available without API setup");
Check(scene.IndexOf("EnsureSetupReady()",directBranch,StringComparison.Ordinal)>scene.IndexOf("OpenScenePlayerDirectActions",directBranch,StringComparison.Ordinal),"direct branch returns before original setup gate");
var host=File.ReadAllText(Path.Combine(root.FullName,"src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.PlayerDirectActions.cs"));
foreach(var forbidden in new[]{"DeveloperActions","EconomyReplay","GenerateAsync","CallApi","EnqueueSpeechLine","MOOD:"})
 Check(!host.Contains(forbidden,StringComparison.Ordinal),"no forbidden alternate execution / AI / speech: "+forbidden);
Console.WriteLine($"PASS {checks} player-direct protocol, popup lifecycle and production admission/routing checks");
