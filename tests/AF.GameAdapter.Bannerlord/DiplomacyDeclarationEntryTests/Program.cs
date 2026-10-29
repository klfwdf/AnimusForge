using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml;
using HarmonyLib;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TaleWorlds.TwoDimension;
class Program {
 static readonly BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
 static bool ai=true,ruler=true,compose;
 static int count,opens,hints,ends; static string lastHint;
 static MethodInfo M(Type t,string n)=>t.GetMethod(n,All);
 static void Set(object x,string n,object v)=>x.GetType().GetProperty(n,All).SetValue(x,v);
 static void Field(object x,string n,object v)=>x.GetType().GetField(n,All).SetValue(x,v);
 static object Bare(Type t)=>FormatterServices.GetUninitializedObject(t);
 static void Check(bool b,string n){if(!b)throw new Exception(n);count++;}
 static bool Ai(ref bool __result){__result=ai;return false;}
 static bool Ruler(ref bool __result){__result=ruler;return false;}
 static bool Compose(ref bool __result){__result=compose;return false;}
 static bool Open(ref bool __result){opens++;__result=true;return false;}
 static bool ShowHint(string __0){hints++;lastHint=__0;return false;}
 static bool HideHint(){ends++;return false;}
 static int Main(string[] args){
  AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{
   foreach(string dir in args.Skip(2)){var p=Path.Combine(dir,new AssemblyName(e.Name).Name+".dll");if(File.Exists(p))return Assembly.LoadFrom(p);}return null;
  };
  try{Run(args);Console.WriteLine("PASS "+count+" actual DLL/widget checks; native render/font assets and campaign/provider not started.");return 0;}
  catch(Exception e){Console.WriteLine(e);return 1;}
 }
 [MethodImpl(MethodImplOptions.NoInlining)]
 static void Run(string[] args){
  var af=Assembly.LoadFrom(args[0]);var type=af.GetType("AnimusForge.KingdomDiplomacyDeclarationEntry",true);
  var h=new Harmony("af.declaration.entry.probe");
  if(args.Contains("--proposals-only")){
   var proposalVm=Assembly.Load("TaleWorlds.CampaignSystem.ViewModelCollection").GetType("TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy.KingdomDiplomacyVM",true);
   ProposalTakeover(af,h,proposalVm,args[1]);return;
  }
  M(type,"EnsurePatched").Invoke(null,new object[]{h});
  h.Patch(M(type,"AiEnabled"),prefix:new HarmonyMethod(M(typeof(Program),"Ai")));
  h.Patch(M(type,"PlayerIsRuler"),prefix:new HarmonyMethod(M(typeof(Program),"Ruler")));
  h.Patch(M(af.GetType("AnimusForge.DiplomacyPresentationBridge"),"OpenComposeFromTerminal"),prefix:new HarmonyMethod(M(typeof(Program),"Open")));
  var info=Assembly.Load("TaleWorlds.Core").GetType("TaleWorlds.Core.MBInformationManager",true);
  h.Patch(M(info,"ShowHint"),prefix:new HarmonyMethod(M(typeof(Program),"ShowHint")));
  h.Patch(M(info,"HideInformations"),prefix:new HarmonyMethod(M(typeof(Program),"HideHint")));
  h.Patch(af.GetType("AnimusForge.WorldDiplomacyComposePopup").GetProperty("IsOpen").GetGetMethod(),prefix:new HarmonyMethod(M(typeof(Program),"Compose")));
  var vmType=Assembly.Load("TaleWorlds.CampaignSystem.ViewModelCollection").GetType("TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy.KingdomDiplomacyVM",true);
  var vm=Bare(vmType);
  var context=(UIContext)Bare(typeof(UIContext));
  Set(context,"EventManager",Bare(typeof(EventManager)));
  var font=new Font("Probe");
  var fonts=(FontFactory)Bare(typeof(FontFactory));Field(fonts,"_bitmapFonts",new Dictionary<string,Font>{{"Probe",font}});
  var language=Activator.CreateInstance(typeof(Language),true);Set(language,"DefaultFont",font);
  Set(fonts,"CurrentLanguage",language);Set(context,"FontFactory",fonts);
  var brushes=(BrushFactory)Bare(typeof(BrushFactory));
  var green=new Brush{Name="Popup.Done.Button"};var grey=new Brush{Name="Popup.Cancel.Button"};
  Field(brushes,"_brushes",new Dictionary<string,Brush>{{"Popup.Done.Button",green},{"Popup.Cancel.Button",grey},{"Popup.Button.Text",new Brush{Font=font}},{"DefaultBrush",new Brush{Font=font}}});
  Set(context,"BrushFactory",brushes);
  typeof(WidgetInfo).GetField("_widgetInfos",All).SetValue(null,new[]{typeof(Widget),typeof(ListPanel),typeof(ButtonWidget),typeof(TextWidget),typeof(ImageIdentifierWidget),typeof(HintWidget)}.ToDictionary(t=>t,t=>new WidgetInfo(t)));
  var manager=context.EventManager;
  var containers=typeof(EventManager).GetField("_widgetContainers",All);
  var dict=(System.Collections.IDictionary)Activator.CreateInstance(containers.FieldType);
  var types=containers.FieldType.GetGenericArguments();
  foreach(var key in Enum.GetValues(types[0]))dict[key]=Activator.CreateInstance(types[1],All,null,new object[]{32},null);
  containers.SetValue(manager,dict);
  var root=new Widget(context){Id="FixtureRoot"};
  var panel=new Widget(context){Id="DiplomacyPanel"};root.AddChild(panel);
  var column=new ListPanel(context);panel.AddChild(column);
  var header=new Widget(context);column.AddChild(header);
  var portrait=new ButtonWidget(context){SuggestedWidth=170,SuggestedHeight=130,MarginTop=5};
  portrait.AddChild(new ImageIdentifierWidget(context){MarginLeft=17,MarginRight=18});
  header.AddChild(portrait);header.AddChild(new Widget(context){Id="NameAndClansContainer"});
  var agreements=new Widget(context);column.AddChild(agreements);
  var warLogPanel=new Widget(context);column.AddChild(warLogPanel);
  warLogPanel.AddChild(new Widget(context){Id="WarLogs"});
  var comparison=new Widget(context){MarginRight=42};column.AddChild(comparison);
  var scroll=new Widget(context);comparison.AddChild(scroll);
  var clip=new Widget(context){ClipContents=true};scroll.AddChild(clip);
  clip.AddChild(new Widget(context){Id="StatBars"});
  var warStrategy=new Widget(context){MarginTop=125};panel.AddChild(warStrategy);
  foreach(string lang in new[]{"en","cn"}){
   string label=lang=="en"?"English":"简体中文";
   string path=Path.Combine(args[1],"content/modules/AF.Module.UI/ModuleData/Languages",lang=="en"?"af_ui_strings.xml":"CNs/af_ui_strings-zh-CN.xml");
   var doc=new XmlDocument();doc.LoadXml("<LanguageData id=\""+label+"\" name=\""+label+"\" supported_iso=\""+(lang=="en"?"en":"zh-CN")+"\" under_development=\"false\" text_processor=\"TaleWorlds.Localization.TextProcessor.LanguageProcessors.EnglishTextProcessor\"><LanguageFile xml_path=\""+Path.GetFileName(path)+"\" /></LanguageData>");
   typeof(TextObject).Assembly.GetType("TaleWorlds.Localization.LanguageData").GetMethod("LoadFromXml",All).Invoke(null,new object[]{doc,Path.GetDirectoryName(path)});
   Check(MBTextManager.ChangeLanguage(label),"language");
   M(type,"Inject").Invoke(null,new[]{root,vm});
   var wrapper=root.FindChild("AnimusForgeDiplomacyDeclarationEntry",true);
   var button=wrapper.Children.OfType<ButtonWidget>().Single();
   var text=button.Children.OfType<TextWidget>().Single();
   var hint=wrapper.Children.OfType<HintWidget>().Single();
   if(lang=="en")M(typeof(HintWidget),"OnConnectedToRoot").Invoke(hint,null);
   M(type,"Inject").Invoke(null,new[]{root,vm});
   Check(column.Children.Count(x=>x.Id=="AnimusForgeDiplomacyDeclarationEntry")==1,"no duplicate injection");
   Check(wrapper.SuggestedWidth==250 && wrapper.SuggestedHeight==30 && wrapper.MarginTop==18 && wrapper.MarginBottom==4,"slim button row lowered above statistics");
   Check(wrapper.HorizontalAlignment==HorizontalAlignment.Center && wrapper.MarginRight==comparison.MarginRight,"center aligned with statistics");
   Check(column.Children.ElementAt(2)==wrapper && column.Children.ElementAt(3)==warLogPanel && column.Children.ElementAt(4)==comparison,"row precedes either selected content mode");
   Check(!header.Children.Contains(wrapper) && warStrategy.MarginTop==125,"war strategy and portrait layout unchanged");
   foreach(bool atWar in new[]{false,true}){
    warStrategy.IsVisible=atWar;
    foreach(bool logs in new[]{false,true}){
     warLogPanel.IsVisible=logs;comparison.IsVisible=!logs;
     Check(wrapper.IsVisible && wrapper.ParentWidget==column,"entry outside hidden content or clipped scroller");
    }
   }
   Check(wrapper.Children.Count()==2 && button.Children.Count()==1,"no permanent explanatory text");
   foreach(bool a in new[]{false,true})foreach(bool r in new[]{false,true}){
    ai=a;ruler=r;int before=hints;
    M(type,"PageRefreshed").Invoke(null,new[]{vm});
    Check(hints==before,"page refresh never displays tooltip");
    Check(wrapper.IsVisible && wrapper.IsEnabled,"wrapper visible and hoverable");
    Check(button.IsEnabled==(a&&r),"enabled only for ruler with AI on");
    Check(button.ReadOnlyBrush.Name==(a&&r?"Popup.Done.Button":"Popup.Cancel.Button"),"green or grey brush");
    Check(text.Text==(lang=="en"?"Write declaration":"撰写外交宣言"),"localized button");
    Check(text.ReadOnlyBrush.FontSize==(lang=="en"?20:22),"readable localized label in slim button");
    M(typeof(Widget),"OnHoverBegin").Invoke(wrapper,null);
    Check(hints==before+1,"disabled or enabled parent hover reaches native hint");
    string expected=!a?(lang=="en"?"AI diplomacy is not enabled.":"未开启 AI 外交。"):!r?(lang=="en"?"Only the ruler can write a diplomatic declaration.":"仅统治者可撰写外交宣言。"):(lang=="en"?"Write a diplomatic declaration.":"撰写外交宣言。");
    Check(lastHint==expected,"correct hover reason with disabled AI priority");
    before=ends;M(typeof(Widget),"OnHoverEnd").Invoke(wrapper,null);Check(ends==before+1,"hover end hides hint");
    before=opens;foreach(var click in button.ClickEventHandlers)click(button);Check(opens==before+(a&&r?1:0),"actual click dispatch revalidates eligibility");
   }
   ai=true;ruler=true;M(type,"PageRefreshed").Invoke(null,new[]{vm});ai=false;
   int opened=opens;foreach(var click in button.ClickEventHandlers)click(button);
   Check(opens==opened&&!button.IsEnabled,"stale enabled button blocked when AI changes");
   ai=true;ruler=true;M(type,"PageRefreshed").Invoke(null,new[]{vm});ruler=false;
   foreach(var click in button.ClickEventHandlers)click(button);
   Check(opens==opened&&!button.IsEnabled,"stale enabled button blocked on leadership loss");
  }
  foreach(bool opened in new[]{false,true,false}){compose=opened;Check((bool)M(type,"KingdomInputAllowed").Invoke(null,null)==!opened,"kingdom hotkeys blocked only while editor open");}
  ProposalTakeover(af,h,vmType,args[1]);
 }
 static bool SuppressMessage(){return false;}
 static void ProposalTakeover(Assembly af,Harmony h,Type vmType,string root){
  var owner=af.GetType("AnimusForge.WorldDiplomacyBehavior",true);
  h.Patch(M(owner,"IsWorldDiplomacyEnabled"),prefix:new HarmonyMethod(M(typeof(Program),"Ai")));
  h.Patch(M(owner,"IsPlayerKingdom"),prefix:new HarmonyMethod(M(typeof(Program),"Ruler")));
  h.Patch(typeof(TaleWorlds.Library.InformationManager).GetMethod("DisplayMessage",new[]{typeof(TaleWorlds.Library.InformationMessage)}),
   prefix:new HarmonyMethod(M(typeof(Program),"SuppressMessage")));
  var campaignAssembly=Assembly.Load("TaleWorlds.CampaignSystem");
  var campaignType=campaignAssembly.GetType("TaleWorlds.CampaignSystem.Campaign",true);
  var campaign=Bare(campaignType);campaignType.GetProperty("Current",All).SetValue(null,campaign);
  var clan=Bare(campaignAssembly.GetType("TaleWorlds.CampaignSystem.Clan",true));
  var kingdom=Bare(campaignAssembly.GetType("TaleWorlds.CampaignSystem.Kingdom",true));
  Set(campaign,"PlayerDefaultFaction",clan);
  var actionType=vmType.Assembly.GetType("TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy.KingdomDiplomacyProposalActionItemVM",true);
  h.Patch(M(actionType,"RefreshValues"),postfix:new HarmonyMethod(M(owner,"Patch_DiplomacyProposalActionItem_Constructed_Postfix")));
  h.Patch(M(actionType,"ExecuteAction"),prefix:new HarmonyMethod(M(owner,"Patch_DiplomacyProposalActionItem_Execute_Prefix")));
  foreach(var ctor in actionType.GetConstructors())h.Patch(ctor,postfix:new HarmonyMethod(M(owner,"Patch_DiplomacyProposalActionItem_Constructed_Postfix")));
  foreach(string lang in new[]{"en","cn"}){
   string label=lang=="en"?"English":"简体中文";
   string path=Path.Combine(root,"content/modules/AF.Module.Diplomacy/ModuleData/Languages",lang=="en"?"afdip_ui_strings.xml":"CNs/afdip_ui_strings-zh-CN.xml");
   var doc=new XmlDocument();doc.LoadXml("<LanguageData id=\""+label+"\" name=\""+label+"\" supported_iso=\""+(lang=="en"?"en":"zh-CN")+"\" under_development=\"false\" text_processor=\"TaleWorlds.Localization.TextProcessor.LanguageProcessors.EnglishTextProcessor\"><LanguageFile xml_path=\""+Path.GetFileName(path)+"\" /></LanguageData>");
   typeof(TextObject).Assembly.GetType("TaleWorlds.Localization.LanguageData").GetMethod("LoadFromXml",All).Invoke(null,new object[]{doc,Path.GetDirectoryName(path)});
   Check(MBTextManager.ChangeLanguage(label),"proposal language");
   string expected=new TextObject("{=AF_DIP_UI_WorldDiplomacy_ManagedByAi}This action is managed by AI diplomacy.").ToString();
   foreach(string role in new[]{"ruler","vassal","independent"})foreach(bool enabled in new[]{false,true}){
    ai=enabled;ruler=role=="ruler";Field(clan,"_kingdom",role=="independent"?null:kingdom);
    bool managed=enabled&&role!="independent";int called=0;
    var vm=Bare(vmType);var list=Activator.CreateInstance(vmType.GetProperty("Actions").PropertyType);Set(vm,"Actions",list);
    foreach(string name in new[]{"Alliance","War","Trade","Peace"}){
     var action=Activator.CreateInstance(actionType,new object[]{new TextObject(name),new TextObject("native"),150,true,new TextObject("native hint"),new Action(()=>called++)});
     M(list.GetType(),"Add").Invoke(list,new[]{action});
     Check((bool)actionType.GetProperty("IsEnabled").GetValue(action)==!managed,"constructor takeover "+role+":"+name);
     Set(action,"IsEnabled",true);M(actionType,"RefreshValues").Invoke(action,null);
     Check((bool)actionType.GetProperty("IsEnabled").GetValue(action)==!managed,"refresh takeover "+role+":"+name);
     int before=called;M(actionType,"ExecuteAction").Invoke(action,null);
     Check(called==before+(managed?0:1),"actual native callback blocked only under takeover "+role+":"+name);
    }
    M(owner,"Patch_DiplomacyPage_Refreshed_Postfix").Invoke(null,new[]{vm});
    var items=((System.Collections.IEnumerable)list).Cast<object>().ToArray();
    Check(items.Select(x=>(string)actionType.GetProperty("Explanation").GetValue(x)).SequenceEqual(managed?new[]{"","",expected,""}:new[]{"native","native","native","native"}),"single centered localized explanation "+role);
   }
  }
 }
}
