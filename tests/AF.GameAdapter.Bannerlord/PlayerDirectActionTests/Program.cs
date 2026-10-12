using AnimusForge;
using TaleWorlds.Library;

int checks=0;
void Check(bool value, string name) { checks++; if (!value) throw new Exception(name); }
var repo = new DirectoryInfo(AppContext.BaseDirectory);
while (repo != null && !File.Exists(Path.Combine(repo.FullName, "AnimusForge.csproj"))) repo = repo.Parent;
Check(repo != null, "repository root for actual displayed UI");
foreach (var entry in new[] { ("AFSceneSessionScroll.xml", "ExecuteDirectActions"), ("AFSceneSessionFolio.xml", "ExecuteDirectActions"), ("AFDialogueNativeOverlay.xml", "OpenDirectActions") })
{
    var prefab = System.Xml.Linq.XDocument.Load(Path.Combine(repo.FullName, "extensions/AnimusForge.DialogueUI/GUI/Prefabs", entry.Item1));
    var buttons = prefab.Descendants("ButtonWidget").Where(x => (string)x.Attribute("Command.Click") == entry.Item2).ToList();
    Check(buttons.Count == 1, "actual displayed panel has exactly one manual-tag button: " + entry.Item1);
    var manual = buttons.Single();
    Check((string)manual.Attribute("IsEnabled") == "@CanOpenDirectActions", "manual-tag button enable binding: " + entry.Item1);
    Check(manual.Attribute("IsVisible") == null || (string)manual.Attribute("IsVisible") == "@IsToolbarVisible", "button not gated by AI/Hero/developer/illustration: " + entry.Item1);
    Check((string)manual.Attribute("DoNotPassEventsToChildren") == "true" && (string)manual.Descendants("TextWidget").Single().Attribute("DoNotAcceptEvents") == "true", "manual-tag button owns clicks: " + entry.Item1);
    string id = (string)manual.Attribute("Id");
    double N(System.Xml.Linq.XElement element, string attr) => double.TryParse((string)element.Attribute(attr), out var value) ? value : 0;
    foreach (var sibling in manual.Parent.Elements().Where(x => x != manual && (x.Name.LocalName is "ButtonWidget" or "TextWidget" or "ListPanel")
        && (string)x.Attribute("HorizontalAlignment") == "Left" && (string)x.Attribute("VerticalAlignment") == "Top"))
    {
        double x=N(manual,"MarginLeft"), y=N(manual,"MarginTop"), w=N(manual,"SuggestedWidth"), h=N(manual,"SuggestedHeight");
        double sx=N(sibling,"MarginLeft"), sy=N(sibling,"MarginTop"), sw=N(sibling,"SuggestedWidth"), sh=N(sibling,"SuggestedHeight");
        Check(Math.Max(x,sx) >= Math.Min(x+w,sx+sw) || Math.Max(y,sy) >= Math.Min(y+h,sy+sh), "manual button does not overlap sibling control/text: " + entry.Item1 + " " + (string)sibling.Attribute("Id") + (string)sibling.Attribute("Text"));
    }
    if (entry.Item1 == "AFSceneSessionFolio.xml")
    {
        var status = manual.Parent.Elements("TextWidget").Single(x => (string)x.Attribute("Text") == "@StatusText");
        var hint = manual.Parent.Elements("TextWidget").Single(x => (string)x.Attribute("Text") == "@InputHintText");
        Check(Math.Max(N(status,"MarginLeft"),N(hint,"MarginLeft")) >= Math.Min(N(status,"MarginLeft")+N(status,"SuggestedWidth"),N(hint,"MarginLeft")+N(hint,"SuggestedWidth"))
            || Math.Max(N(status,"MarginTop"),N(hint,"MarginTop")) >= Math.Min(N(status,"MarginTop")+N(status,"SuggestedHeight"),N(hint,"MarginTop")+N(hint,"SuggestedHeight")), "folio status remains separate from the input hint");
    }
    if (entry.Item2 == "OpenDirectActions")
    {
        foreach (string file in new[] { "Native/NativeUiAdapter.cs", "PresentationRouter.cs" })
            Check(File.ReadAllText(Path.Combine(repo.FullName, "extensions/AnimusForge.DialogueUI/src", file)).Contains("\"" + id + "\""), "native manual button participates in hit protection and styling: " + file);
    }
}
var expected = new Dictionary<PlayerDirectActionKind,string> {
 [PlayerDirectActionKind.Gold]="[ACTION:GIVE_ASSET:GOLD:23]", [PlayerDirectActionKind.Item]="[ACTION:GIVE_ASSET:grain:23]",
 [PlayerDirectActionKind.FixedAsset]="[ACTION:GIVE_ASSET:town_1:1]", [PlayerDirectActionKind.Troops]="[ATT:2:23]",
 [PlayerDirectActionKind.Prisoners]="[ATP:2:23]", [PlayerDirectActionKind.Debt]="[AD:23:0:P:承诺]",
 [PlayerDirectActionKind.ResolveDebt]="[ADP:debt_1]", [PlayerDirectActionKind.Companion]="[A:H_J_P_P_C]",
 [PlayerDirectActionKind.Family]="[A:H_J_P_P_L]", [PlayerDirectActionKind.Follow]="[ACTION:SCENE_FOLLOW_PLAYER]",
 [PlayerDirectActionKind.StopFollow]="[ACTION:SCENE_STOP_FOLLOW]", [PlayerDirectActionKind.LordsHall]="[ACTION:OPEN_LORDS_HALL]",
 [PlayerDirectActionKind.AcceptIssue]="[ACTION:ISSUE_ACCEPT_SELF]", [PlayerDirectActionKind.TurnInQuest]="[ACTION:QUEST_TURN_IN]" };
Check(PlayerDirectActionProtocol.Actions.Count == 20, "14 existing plus six world-map actions");
foreach (var action in PlayerDirectActionProtocol.Actions)
    Check(PlayerDirectActionProtocol.TagTemplate(action.Key).StartsWith("[") && PlayerDirectActionProtocol.TagTemplate(action.Key).EndsWith("]"), "all choices expose their existing tag template: " + action.Key);
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
WorldMapActions.Run(Check);
WorldMapOwner.Run(Check);
WorldMapIntegration.Run(Check);
var root=new DirectoryInfo(AppContext.BaseDirectory);
while(root!=null && !File.Exists(Path.Combine(root.FullName,"AnimusForge.csproj")))root=root.Parent;
Check(root!=null,"repository root");
var languageRoot = Path.Combine(root.FullName, "content/modules/AF.Module.Conversation/ModuleData/Languages");
Dictionary<string,string> ReadLanguage(string name) => System.Xml.Linq.XDocument.Load(Path.Combine(languageRoot, name))
 .Descendants("string").ToDictionary(x=>(string)x.Attribute("id"),x=>(string)x.Attribute("text"));
TaleWorlds.Localization.TextObject.English=ReadLanguage("afconv_ui_strings.xml");
TaleWorlds.Localization.TextObject.Chinese=ReadLanguage("CNs/afconv_ui_strings-zh-CN.xml");
foreach(var pair in TaleWorlds.Localization.TextObject.English.Where(x=>x.Key.StartsWith("AF_CONV_UI_Direct_"))) {
 Check(TaleWorlds.Localization.TextObject.Chinese.ContainsKey(pair.Key),"matching Chinese direct-action key "+pair.Key);
 Check(System.Text.RegularExpressions.Regex.Matches(pair.Value,@"\{[A-Z_]+\}").Select(x=>x.Value).Order().SequenceEqual(
    System.Text.RegularExpressions.Regex.Matches(TaleWorlds.Localization.TextObject.Chinese[pair.Key],@"\{[A-Z_]+\}").Select(x=>x.Value).Order()),"CN/EN placeholder parity "+pair.Key);
 foreach(var language in new[]{"English","简体中文"}) {
  TaleWorlds.Localization.TextObject.Language=language;
  Check(ConversationUiLocalization.Text(pair.Key,pair.Value)==(language=="English"?pair.Value:TaleWorlds.Localization.TextObject.Chinese[pair.Key]),"actual reader resolves "+language+" "+pair.Key);
 }
}
foreach(var language in new[]{"English","简体中文"}) {
 TaleWorlds.Localization.TextObject.Language=language;
 Open(); var localizedMenu=(MultiSelectionInquiryData)InformationManager.Popup;
 Check(localizedMenu.Title.EndsWith("NPC") && localizedMenu.Elements.Count==20,"localized action inquiry retains target and count");
 Check(localizedMenu.Elements[0].Name==(language=="English"?"NPC gives me gold":"NPC 给我钱"),"actual controller uses current UI language");
 Check(localizedMenu.Elements.Single(x=>Equals(x.Identifier,PlayerDirectActionKind.GoToSettlement)).Name==(language=="English"?"Go to a location":"前往指定地点"),"map actions use the same live language reader");
 localizedMenu.NegativeAction(null);
}
TaleWorlds.Localization.TextObject.Language="English";
foreach(var (index,file) in new[]{("language_data.xml","afconv_ui_strings.xml"),("CNs/language_data.xml","CNs/afconv_ui_strings-zh-CN.xml")})
 Check(System.Xml.Linq.XDocument.Load(Path.Combine(root.FullName,"content/foundation/AF.Foundation.Localization/ModuleData/Languages",index)).Descendants("LanguageFile").Any(x=>(string)x.Attribute("xml_path")==file),"actual language registration "+file);
using(var map=System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName,"content/content-map.json"))))
 foreach(var file in new[]{"afconv_ui_strings.xml","CNs/afconv_ui_strings-zh-CN.xml"})
  Check(map.RootElement.GetProperty("entries").EnumerateArray().Any(x=>x.GetProperty("source").GetString()=="content/modules/AF.Module.Conversation/ModuleData/Languages/"+file
    &&x.GetProperty("target").GetString()=="ModuleData/Languages/"+file),"unchanged standard resource map "+file);
var xml=System.Xml.Linq.XDocument.Load(Path.Combine(root.FullName,"content/modules/AF.Module.Conversation/GUI/Prefabs/AnimusForgeNativeConversationOverlay.xml"));
var button=xml.Descendants("ButtonWidget").Single(x=>(string)x.Attribute("Command.Click")=="OpenDirectActions");
Check((string)button.Attribute("MarginTop")=="30" && (string)button.Attribute("IsEnabled")=="@CanOpenDirectActions","native top-right binding");
Check((string)button.Descendants("TextWidget").Single().Attribute("DoNotAcceptEvents")=="true","label does not steal clicks");
Check((string)button.Descendants("TextWidget").Single().Attribute("Text")=="@DirectActionsButtonText","native direct-action button binds localized datasource");
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
