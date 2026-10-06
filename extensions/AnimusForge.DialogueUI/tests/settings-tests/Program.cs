using AnimusForge.DialogueUI;
using MCM.Abstractions.Attributes.v2;
using TaleWorlds.GauntletUI;
using System.Xml.Linq;
int checks=0;
void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
DialogueUiSettings.Instance=null;
Check(DialogueUiOptions.SkinEnabled && DialogueUiOptions.PanelStyle==ShoutPanelStyle.Scroll && DialogueUiOptions.BodyFontSize==24,"missing settings use new UI, scroll and 24");
var settings=new DialogueUiSettings();DialogueUiSettings.Instance=settings;
Check(settings.InterfaceStyleDropdown.SelectedIndex==1 && settings.ShoutPanelStyleDropdown.SelectedIndex==1,"fresh MCM defaults");
Check(settings.InterfaceStyleDropdown.Values.SequenceEqual(new[]{"原版","新 UI"}),"two explicit UI options");
settings.InterfaceStyleDropdown.SelectedIndex=0;
Check(!DialogueUiOptions.SkinEnabled && !DialogueUiOptions.UsesSceneSession,"original mode disables replacements and scene session");
settings.InterfaceStyleDropdown.SelectedIndex=1;
Check(DialogueUiOptions.SkinEnabled && DialogueUiOptions.UsesSceneSession,"new mode enables default scroll session");
settings.ShoutPanelStyleDropdown.SelectedIndex=0;Check(!DialogueUiOptions.UsesSceneSession,"explicit original shout style preserved");
settings.ShoutPanelStyleDropdown.SelectedIndex=2;Check(DialogueUiOptions.PanelStyle==ShoutPanelStyle.SideFolio,"explicit folio style preserved");
settings.ShoutPanelStyleDropdown.SelectedIndex=99;Check(DialogueUiOptions.PanelStyle==ShoutPanelStyle.Scroll,"invalid style uses scroll");
settings.EnableSkin=false;Check(!DialogueUiOptions.SkinEnabled,"legacy integration setter maps to original selector");
settings.EnableSkin=true;Check(DialogueUiOptions.SkinEnabled,"legacy integration setter maps to new selector");
Check(!typeof(DialogueUiSettings).GetProperty("EnableSkin").GetCustomAttributes(false).Any(a=>a is SettingPropertyDropdownAttribute),"legacy switch no longer visible in MCM");
foreach(int size in new[]{-1,14,24,36,100})
{
 settings.BodyFontSize=size;int want=Math.Clamp(size,14,36);
 var text=new AFDialogueBodyTextWidget(new UIContext());text.Brush.FontSize=7;text.Frame();
 var rich=new AFDialogueBodyRichTextWidget(new UIContext());rich.Brush.FontSize=7;rich.Frame();
 var edit=new AFDialogueBodyEditorWidget(new UIContext());edit.EditorFontSize=7;edit.Frame();
 Check(text.Brush.FontSize==want && rich.Brush.FontSize==want && edit.EditorFontSize==want && edit.Brush.FontSize==want,"all body widgets capture bounded size "+size);
 settings.BodyFontSize=20;text.Frame();rich.Frame();edit.Frame();
 Check(text.Brush.FontSize==want && rich.Brush.FontSize==want && edit.EditorFontSize==want,"existing widgets retain captured size "+size);
 var reopened=new AFDialogueBodyTextWidget(new UIContext());reopened.Frame();Check(reopened.Brush.FontSize==20,"reopened widget takes new size "+size);
}
var root=Path.GetFullPath(args[0]);int bodyCount=0;
var bindings=new HashSet<string>{"@DialogText","@ItemText","@ChatText","@HistoryText","@Text","@InputText","@EditText"};
foreach(string file in Directory.GetFiles(Path.Combine(root,"extensions/AnimusForge.DialogueUI/GUI/Prefabs"),"*.xml"))
{
 foreach(var e in XDocument.Load(file).Descendants())
 {
  string value=(string)e.Attribute("Text")??(string)e.Attribute("RealText");
  if(!bindings.Contains(value??""))continue;
  Check(e.Name.LocalName.StartsWith("AFDialogueBody"),Path.GetFileName(file)+" body binding "+value);
  bodyCount++;
 }
}
Check(bodyCount==12,"all 12 intended body bindings covered");
Console.WriteLine($"{checks}/{checks} production settings/widget capture and XML checks passed; MCM/Gauntlet native rendering NOT_RUN");
