using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using MCM.Common;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using AnimusForge;
namespace AnimusForge {
 public class DuelSettings {
  public Dropdown<string> MainModelDropdown{get;set;}=new Dropdown<string>(new[]{"manual","main-chosen","main-other"},1);
  public Dropdown<string> AuxiliaryModelDropdown{get;set;}=new Dropdown<string>(new[]{"manual","aux-chosen"},1);
  public Dropdown<string> ActionPostprocessModelDropdown{get;set;}=new Dropdown<string>(new[]{"manual","post-chosen"},1);
  public Dropdown<string> EventAndRebellionModelDropdown{get;set;}=new Dropdown<string>(new[]{"manual","event-chosen"},1);
  public Dropdown<string> TownAmbientAiModelDropdown{get;set;}=new Dropdown<string>(new[]{"manual","ambient-chosen"},1);
 }
 internal static class McmPromptDefaults {internal static string Raw(string key)=>"manual";}
 internal static class Logger {public static void Log(string area,string text){} }
}
namespace AnimusForge.Illustrator {public class IllustratorSettings {public Dropdown<string> ModelDropdown{get;set;}=new Dropdown<string>(new[]{"manual","image-chosen"},1);} }
public class ReplayPropertyVm:ViewModel {
 public bool IsDropdown=>true;public PropertyRef PropertyReference{get;set;}public object DropdownValue{get;set;}public int Edits;
 public void Attach(){var v=(ViewModel)DropdownValue;v.PropertyChanged+=DropdownValue_PropertyChanged;v.PropertyChangedWithValue+=DropdownValue_PropertyChangedWithValue;}
 private void DropdownValue_PropertyChanged(object sender,PropertyChangedEventArgs args){if(args.PropertyName=="SelectedIndex")Apply(sender);}
 private void DropdownValue_PropertyChangedWithValue(object sender,PropertyChangedWithValueEventArgs args){if(args.PropertyName=="SelectedIndex")Apply(sender);}
 private void Apply(object selector){Edits++;var action=Program.Ui.GetType("MCM.UI.Actions.SetSelectedIndexAction",true);var a=Activator.CreateInstance(action,new object[]{PropertyReference,selector});action.GetMethod("DoAction").Invoke(a,null);}
}
public class BrokenSelector:ViewModel {
 public IList ItemList{get;set;}public int SelectedIndex{get;set;}public bool HasSingleItem{get;set;}private int _selectedIndex;
 public BrokenSelector(IEnumerable<string> values,int index){throw new MissingMethodException("selector fixture constructor");}
}
public static class Program {
 public static Assembly Ui;static int checks;
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
 static MethodInfo Refresh=typeof(McmDropdownRuntimeRefresh).GetMethod("TryRefreshDropdown",BindingFlags.Static|BindingFlags.NonPublic);
 static object Selector(){var item=Ui.GetType("MCM.UI.Dropdown.MCMSelectorItemVM`1").MakeGenericType(typeof(TextObject));var selector=Ui.GetType("MCM.UI.Dropdown.MCMSelectorVM`2").MakeGenericType(item,typeof(TextObject));return Activator.CreateInstance(selector,new object[]{new List<TextObject>{new TextObject("old")},0});}
 static ReplayPropertyVm Vm(object owner,string property){return new ReplayPropertyVm{PropertyReference=new PropertyRef(owner.GetType().GetProperty(property),owner),DropdownValue=Selector()};}
 public static int Main(string[] args){try{
  Ui=Assembly.LoadFrom(args[0]);
  Check(AnimusForgeMcmVisibility.IsVisibleProperty("AnimusForge_global_settings","EnableTownAmbientAi","{=AF_MCM_TownAmbient_Group}Settlement life/{=AF_MCM_TownAmbient_AiGroup}Ambient AI interface"),"requested ambient AI switch visible through real frontend filter");
  Check(!AnimusForgeMcmVisibility.IsVisibleProperty("AnimusForge_global_settings","EnableTownAmbientDialogue","{=AF_MCM_TownAmbient_Group}Settlement life"),"other gameplay switches remain terminal controls");
  var owner=new DuelSettings();var image=new AnimusForge.Illustrator.IllustratorSettings();
  var main=Vm(owner,"MainModelDropdown");var ambient=Vm(owner,"TownAmbientAiModelDropdown");var photo=Vm(image,"ModelDropdown");
  // Actual selector + actual MCM edit action reproduce the write-back omitted by old fake-VM tests.
  main.Attach();Refresh.Invoke(null,new object[]{main});
  Check(main.Edits==0&&owner.MainModelDropdown.SelectedIndex==1,"refresh is display-only with actual MCM action listener");
  ambient.Attach();photo.Attach();
  for(int tick=0;tick<3;tick++){Refresh.Invoke(null,new object[]{main});Refresh.Invoke(null,new object[]{ambient});Refresh.Invoke(null,new object[]{photo});}
  Check(main.Edits==0&&ambient.Edits==0&&photo.Edits==0,"all API refreshes produce zero automatic configuration edits");
  Check(owner.MainModelDropdown[owner.MainModelDropdown.SelectedIndex]=="main-chosen"&&image.ModelDropdown[image.ModelDropdown.SelectedIndex]=="image-chosen","cross API selections survive");
  foreach(var vm in new[]{main,ambient,photo})Check(((IList)vm.DropdownValue.GetType().GetProperty("ItemList").GetValue(vm.DropdownValue)).Count>=2,"actual MCM list not empty");
  main.DropdownValue.GetType().GetProperty("SelectedIndex").SetValue(main.DropdownValue,2);
  Check(main.Edits>0&&owner.MainModelDropdown.SelectedIndex==2&&owner.TownAmbientAiModelDropdown.SelectedIndex==1,"user selection works after refresh listeners restored");
  var broken=(BrokenSelector)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(BrokenSelector));broken.ItemList=new ArrayList{"retained"};broken.SelectedIndex=0;
  var bad=Vm(owner,"MainModelDropdown");bad.DropdownValue=broken;
  bool failed=false;try{Refresh.Invoke(null,new object[]{bad});}catch(TargetInvocationException){failed=true;}
  Check(failed&&broken.ItemList.Count==1&&(string)broken.ItemList[0]=="retained","failed staged construction leaves live list untouched");
  Console.WriteLine("PASS "+checks+" actual MCM selector/action checks; fake property host, no game/config writes");return 0;
 }catch(Exception ex){Console.WriteLine(ex);return 1;}}
}
