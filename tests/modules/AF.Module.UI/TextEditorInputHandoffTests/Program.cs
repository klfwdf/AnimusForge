using System;
using System.Linq;
using AnimusForge;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
static class Program {
 static int checks;static void C(bool value,string msg){checks++;if(!value)throw new Exception(msg);}
 static (ScreenBase Screen,ScreenLayer Parent) Host(string name,int order,bool mouse=true){var screen=new ScreenBase();ScreenManager.TopScreen=screen;var layer=new ScreenLayer(name,order){IsFocusLayer=true};screen.AddLayer(layer);layer.InputRestrictions.SetInputRestrictions(mouse,InputUsageMask.All);ScreenManager.FocusedLayer=layer;ScreenManager.FirstHitLayer=layer;return(screen,layer);}
 static GauntletLayer Editor(ScreenBase s)=>s.Layers.OfType<GauntletLayer>().Single(x=>!x.IsFinalized);
 static DevHistoryEditPopupVM Vm(GauntletLayer l)=>(DevHistoryEditPopupVM)l.MovieViewModel;
 static void Main(){
  foreach(var spec in new[]{("EncyclopediaBar",310),("AnimusForgeTerminalPopup",309),("MCM",4000),("WeeklyReport",4000),("History",1200)}){
   var h=Host(spec.Item1,spec.Item2);int saves=0,cancels=0;string saved=null;DevTextEditorHelper.ShowLongTextEditor("title","subtitle","hint","draft",s=>{saves++;saved=s;C(h.Parent.IsActive,"restore before save callback");},()=>cancels++);
   var child=Editor(h.Screen);C(!h.Parent.IsActive&&h.Parent.InputUsageMask==InputUsageMask.Invalid,"parent releases dispatch: "+spec.Item1);C(child.InputRestrictions.Order>h.Parent.InputRestrictions.Order,"child above parent");var dispatch=ScreenManager.Dispatch();C(dispatch.Mouse==child&&dispatch.Keyboard==child,"mouse and keyboard reach editor");
   Vm(child).EditedText="输入汉字";Vm(child).ExecuteSave();Vm(child).ExecuteSave();C(saves==1&&saved=="输入汉字"&&cancels==0,"save exactly once");C(h.Parent.IsActive&&h.Parent.InputUsageMask==InputUsageMask.All&&ScreenManager.FocusedLayer==h.Parent,"mask and focus restored");C(!DevHistoryEditPopup.IsOpen&&ScreenManager.PushSubscribers==0,"closed editor releases event subscriptions");
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
  Console.WriteLine("PASS TextEditorInputHandoff assertions="+checks+" production helper/popup/VM/lease; native dispatch modeled; live=NOT_RUN");
 }
}
