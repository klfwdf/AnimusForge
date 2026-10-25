using System;
using System.Linq;
using AnimusForge;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
static partial class Program {
 static int checks;static void C(bool value,string msg){checks++;if(!value)throw new Exception(msg);}
 static (ScreenBase Screen,ScreenLayer Parent) Host(string name,int order,bool mouse=true){var screen=new ScreenBase();ScreenManager.TopScreen=screen;var layer=new GauntletLayer(name,order,false){IsFocusLayer=true};screen.AddLayer(layer);layer.InputRestrictions.SetInputRestrictions(mouse,InputUsageMask.All);ScreenManager.FocusedLayer=layer;ScreenManager.FirstHitLayer=layer;return(screen,layer);}
 static GauntletLayer Editor(ScreenBase s)=>s.Layers.OfType<GauntletLayer>().Single(x=>!x.IsFinalized&&x.MovieViewModel is DevHistoryEditPopupVM);
 static DevHistoryEditPopupVM Vm(GauntletLayer l)=>(DevHistoryEditPopupVM)l.MovieViewModel;
 static void Main(){
  foreach(var spec in new[]{("EncyclopediaBar",310),("AnimusForgeTerminalPopup",309),("MCM",4000),("WeeklyReport",4000),("History",1200)}){
   var h=Host(spec.Item1,spec.Item2);int saves=0,cancels=0;string saved=null;DevTextEditorHelper.ShowLongTextEditor("title","subtitle","hint","draft",s=>{saves++;saved=s;C(h.Parent.IsActive,"restore before save callback");},()=>cancels++);
   var child=Editor(h.Screen);C(!h.Parent.IsActive&&h.Parent.InputUsageMask==InputUsageMask.Invalid,"parent releases dispatch: "+spec.Item1);C(child.InputRestrictions.Order>h.Parent.InputRestrictions.Order,"child above parent");var dispatch=ScreenManager.Dispatch();C(dispatch.Mouse==child&&dispatch.Keyboard==child,"mouse and keyboard reach editor");
   Vm(child).EditedText="杈撳叆姹夊瓧";Vm(child).ExecuteSave();Vm(child).ExecuteSave();C(saves==1&&saved=="杈撳叆姹夊瓧"&&cancels==0,"save exactly once");C(h.Parent.IsActive&&h.Parent.InputUsageMask==InputUsageMask.All&&ScreenManager.FocusedLayer==h.Parent,"mask and focus restored");C(!DevHistoryEditPopup.IsOpen&&ScreenManager.PushSubscribers==0,"closed editor releases event subscriptions");
   DevTextEditorHelper.ShowLongTextEditor("title","","hint","",_=>saves++,()=>cancels++);child=Editor(h.Screen);Vm(child).ExecuteCancel();Vm(child).ExecuteCancel();C(cancels==1&&saves==1&&h.Parent.IsActive,"cancel exactly once and restore");
  }
  {
   var h=Host("MCM",4000,false);DevHistoryEditPopup.Show("direct","","draft","draft",_=>{},()=>{});var child=Editor(h.Screen);Vm(child).ExecuteCancel();C(!h.Parent.InputRestrictions.MouseVisibility,"restore original mouse visibility");
  }
  {
   var h=Host("KeyboardOwner",4000);var low=new ScreenLayer("mouse hover",100);h.Screen.AddLayer(low);ScreenManager.FirstHitLayer=low;C(DevHistoryEditPopup.CaptureInputOwner(h.Screen)==h.Parent,"keyboard owner beats lower hover layer");
   var high=new ScreenLayer("clicked overlay",4020);h.Screen.AddLayer(high);ScreenManager.FirstHitLayer=high;C(DevHistoryEditPopup.CaptureInputOwner(h.Screen)==high,"higher mouse owner captured");high.IsFinalized=true;C(DevHistoryEditPopup.CaptureInputOwner(h.Screen)==h.Parent,"stale clicked layer skipped");
   ScreenManager.FirstHitLayer=new ScreenLayer("global inquiry",9999){IsActive=true};C(DevHistoryEditPopup.CaptureInputOwner(h.Screen)==h.Parent,"unowned global layer is not suspended");
  }
  {
   var h=Host("Persona",310);int callbacks=0;DevTextEditorHelper.ShowLongTextEditor("title","","hint","",_=>callbacks++,()=>callbacks++);var child=Editor(h.Screen);ScreenManager.Push(new ScreenBase());C(!DevHistoryEditPopup.IsOpen&&child.IsFinalized&&callbacks==0,"screen change drops ordinary editor without opening business menu");C(!h.Parent.IsActive&&h.Parent.InputUsageMask==InputUsageMask.All,"old screen stays inactive and keeps restrictions");C(ScreenManager.PushSubscribers==0,"screen change unregisters editor");
  }
  {
   var h=Host("owned",4020);int canceled=0;var session=DevTextEditorHelper.ShowOwnedLongTextEditor("owned","","hint","",_=>{},()=>canceled++,h.Parent,()=>true);ScreenManager.Push(new ScreenBase());session.Dispose();C(canceled==1&&!DevHistoryEditPopup.IsOpen,"owned dismissal exactly once");
  }
  {
   var h=Host("Parent",4020);DevHistoryEditPopup.Show("one","","","",_=>{},()=>{});var one=Editor(h.Screen);DevHistoryEditPopup.Show("two","","","",_=>{},()=>{});var two=Editor(h.Screen);C(one.IsFinalized&&ScreenManager.FocusedLayer==two&&!h.Parent.IsActive,"replacement captures restored parent");Vm(two).ExecuteCancel();C(h.Parent.IsActive&&ScreenManager.FocusedLayer==h.Parent,"replacement restores original parent");
  }
  {
   var h=Host("failure",310);GauntletLayer.FailLoad=true;bool opened=DevHistoryEditPopup.Show("title","","","",_=>{},()=>{});GauntletLayer.FailLoad=false;C(!opened&&h.Parent.IsActive&&h.Parent.InputUsageMask==InputUsageMask.All&&ScreenManager.PushSubscribers==0,"failed load restores parent and subscriptions");
  }
  {
   var h=Host("external suspension",4000);using(var lease=new DevPopupInputLease(h.Screen,h.Parent,()=>true)){ScreenManager.SetSuspendLayer(h.Parent,true);}C(!h.Parent.IsActive&&!h.Parent.LastActiveState,"external suspension remains across later screen activation");
  }
  // Native MultiSelection/TextQuery invoke the business callback before CloseQuery.
  // QueryManager is a global layer (19501), then releases focus to the highest screen layer.
  foreach(int baseOrder in new[]{310,4000}){
   var h=Host("native menu host",baseOrder);var global=new ScreenLayer("QueryManager",19501){IsActive=true,IsFocusLayer=true};global.InputRestrictions.SetInputRestrictions();ScreenManager.FocusedLayer=ScreenManager.FirstHitLayer=global;
   int saves=0;DevTextEditorHelper.ShowLongTextEditor("menu edit","","hint","draft",_=>saves++,()=>{});var child=Editor(h.Screen);
   C(global.IsActive&&global.InputUsageMask==InputUsageMask.All&&ScreenManager.FocusedLayer==global,"ordinary editor does not take over global inquiry during native callback");
   global.InputRestrictions.ResetInputRestrictions();ScreenManager.SetSuspendLayer(global,true);global.IsFocusLayer=false;ScreenManager.TryLoseFocus(global);
   var dispatch=ScreenManager.Dispatch();C(dispatch.Mouse==child&&dispatch.Keyboard==child,"native CloseQuery delivers dispatch to new editor, including MCM tie order");
   Vm(child).ExecuteSave();C(saves==1&&ScreenManager.FocusedLayer==h.Parent,"native menu editor save returns to original screen focus");
  }
  foreach(bool autoToggle in new[]{false,true})foreach(bool save in new[]{false,true}){
   var screen=new ScreenBase();ScreenManager.TopScreen=screen;
   var scene=new TaleWorlds.Engine.Screens.SceneLayer(autoToggle);screen.AddLayer(scene);scene.InputRestrictions.SetInputRestrictions(false,InputUsageMask.All);
   var hud=new GauntletLayer("map HUD",100,false);screen.AddLayer(hud);hud.InputRestrictions.SetInputRestrictions();
   ScreenManager.FirstHitLayer=ScreenManager.FocusedLayer=scene;int callbacks=0;
   DevTextEditorHelper.ShowLongTextEditor("appearance","","hint","",_=>callbacks++,()=>callbacks++);var child=Editor(screen);
   C(scene.IsActive&&scene.LastActiveState&&scene.Deactivations==0,"scene stays active when appearance editor opens");
   C(scene.IsRendering&&ScreenManager.IsVisible(child)&&ScreenManager.IsVisible(hud),"map remains behind editor and HUD, including auto-toggle scenes");
   C(scene.InputUsageMask==InputUsageMask.Invalid&&!scene.IsFocusLayer,"scene releases input without changing rendering");
   var dispatch=ScreenManager.Dispatch();C(dispatch.Mouse==child&&dispatch.Keyboard==child,"map editor owns mouse and keyboard");
   if(save)Vm(child).ExecuteSave();else Vm(child).ExecuteCancel();
   C(callbacks==1&&scene.IsActive&&scene.IsRendering&&scene.Deactivations==0,"map save/cancel keeps rendering and invokes callback once");
   C(scene.InputUsageMask==InputUsageMask.All&&!scene.InputRestrictions.MouseVisibility&&scene.IsFocusLayer&&ScreenManager.FocusedLayer==scene,"map input and focus restored exactly");
  }
  {
   var screen=new ScreenBase();ScreenManager.TopScreen=screen;var scene=new TaleWorlds.Engine.Screens.SceneLayer(false);screen.AddLayer(scene);scene.InputRestrictions.SetInputRestrictions(false,InputUsageMask.All);ScreenManager.FirstHitLayer=ScreenManager.FocusedLayer=scene;
   GauntletLayer.FailLoad=true;bool opened=DevHistoryEditPopup.Show("appearance","","","",_=>{},()=>{});GauntletLayer.FailLoad=false;
   C(!opened&&scene.IsActive&&scene.IsRendering&&scene.Deactivations==0&&scene.InputUsageMask==InputUsageMask.All&&ScreenManager.FocusedLayer==scene,"map editor load failure restores input without scene suspension");
   DevHistoryEditPopup.Show("appearance","","","",_=>{},()=>{});var child=Editor(screen);ScreenManager.SetSuspendLayer(scene,true);Vm(child).ExecuteCancel();
   C(!scene.IsActive&&!scene.LastActiveState&&scene.Deactivations==1,"map externally suspended during editor is not reactivated on close");
  }
  C(!Logger.Messages.Any(x=>x.Contains("[ERROR] Save callback failed")),"original callbacks did not hide failed assertions");
  RunLongTextSafetyCases();
  Console.WriteLine("PASS TextEditorInputHandoff assertions="+checks+" production helper/popup/VM/lease/sanitizer; paste methods source-extracted; native dispatch and scene render ordering modeled; live=NOT_RUN");
 }
}
