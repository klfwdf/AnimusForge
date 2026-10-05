using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
namespace AnimusForge;
public sealed class TestVm { public string DialogText { get; set; } = "..."; public string CurrentCharacterNameLbl { get; set; } }
public sealed class ScreenBase { public bool Temporary; public void RemoveLayer(FakeLayer layer) {} }
public sealed class FakeRestriction { public int Claimed; public void SetInputRestrictions(bool v, InputUsageMask mask) { Claimed++; } public void ResetInputRestrictions() {} }
public sealed class FakeLayer { public FakeRestriction InputRestrictions=new(); public bool IsFocusLayer; public FakeView TwoDimensionView=new(); }
public sealed class FakeView { public void SetEnable(bool v) {} }
public sealed class FakeMovie { public FakeMovie Movie=>this; public FakeMovie RootWidget=>this; public void Show() {} public void Hide() {} }
public enum InputUsageMask { All }
public static class ScreenManager { public static ScreenBase TopScreen; public static int FocusClaims; public static void TrySetFocus(FakeLayer l) { FocusClaims++; } public static void TryLoseFocus(FakeLayer l) {} }
public static class InformationManager { public static bool Inquiry; public static bool IsAnyInquiryActive()=>Inquiry; }
public static class ShoutTextInputPopup { public static bool IsOpen; }
public static class AnimusForgeConversationHistoryLogPopup { public static bool IsOpen; }
public static class NativeConversationAnswerAreaController { public static void SetSuppressed(bool v) {} public static void ForceRestoreAll() {} }
public static class FreezeWatchdog { public static void Mark(string s,string d="",bool immediate=false) {} public static IDisposable Scope(string s)=>new ScopeStub(); private sealed class ScopeStub:IDisposable {public void Dispose() {}} }
public static class Logger { public static void Log(string c,string s) {} public static void LogTrace(string c,string s) {} }
public static class ShoutBehavior {
 public static bool CanSubmitNativeConversationForExternal()=>true;
 public static void OpenNativeConversationInputSilentlyForExternal() {} public static void CloseNativeConversationInputForExternal() {}
 public sealed class NativeConversationPresentationScope { public bool Valid=true; public bool IsCurrent()=>Valid; public bool HasCurrentContext()=>Valid; public bool HasCurrentConversationContext()=>Valid; }
}
public sealed class ReplyWait { public void Stop() {} }
public sealed class ModeText { public void Reset() {} }
public sealed class DataSource { public bool IsCustomAnswerVisible=true; public bool Busy=true; public int Focuses; public void RequestInputFocus(){Focuses++;} public void SetBusy(bool v){Busy=v;} public void OnFinalize(){} }
public sealed partial class AnimusForgeNativeConversationOverlay {
 private static AnimusForgeNativeConversationOverlay _activeOverlay; private static int _mainThreadId;
 private readonly ScreenBase _screen=new(); private readonly FakeLayer _layer=new(); private readonly FakeMovie _movieIdentifier=new();
 private readonly DataSource _dataSource=new(); private readonly ConcurrentQueue<Action> _mainThreadActions=new();
 private bool _isClosed,_isSubmitting=true,_npcOpeningAutoStarted,_temporarySystemUiActive,_isHiddenForTemporarySystemUi,_waitingDotsActive=true;
 private int _submitGeneration=1,_waitingDotsGeneration=1,_postRestoreForceRestoreTicks; private long _nextWaitingDotsUpdateUtcTicks;
 private readonly ReplyWait _replyWait=new(); private readonly ModeText _modeText=new();
 private ShoutBehavior.NativeConversationPresentationScope _modeTextScope=new();
 private void Tick(){ProcessMainThreadActions();ValidatePendingSubmissionPresentation();ProcessPostRestoreNativeAnswerRestore();}
 private static bool Show(ScreenBase s)=>false;
 private static bool IsKnownTemporarySystemScreen(ScreenBase s)=>s?.Temporary==true;
 private static void CloseActive(){_activeOverlay?.Close(true);}
 private void ClearPendingPostprocessNotice(){}
 private void SetLayerForButtonsOnly(){}
 private void RestoreNativeConversationInputAfterOrdinaryMode(bool forceAnswerRestore){}
 public static int Checks;
 private static void Check(bool ok,string name){if(!ok)throw new Exception("FAIL "+name);Checks++;Console.WriteLine("PASS "+name);}
 private static AnimusForgeNativeConversationOverlay New(){
   ConversationHelper.Clear();ScreenManager.FocusClaims=0;InformationManager.Inquiry=false;
   var o=new AnimusForgeNativeConversationOverlay();o._submitPresentationScope=o._modeTextScope;
   _activeOverlay=o;ScreenManager.TopScreen=o._screen;ConversationHelper.SetCurrentVM(new TestVm());
   var ownerMethod=typeof(ConversationHelper).GetMethod("BeginStreaming",BindingFlags.NonPublic|BindingFlags.Static,null,new[]{typeof(object)},null);
   if(ownerMethod!=null)ownerMethod.Invoke(null,new object[]{o});else ConversationHelper.BeginStreaming();
   o._screen.Temporary=true;OnApplicationTick();return o;
 }
 private void PostReply(string text,bool finish){int gen=_submitGeneration;Task.Run(()=>RunNativePresentationCallback(gen,()=>{
   StopWaitingDotsAnimation(gen);SetSubmissionDisplayText(gen,text);
   if(finish&&CompleteNativeSubmissionPresentation(gen))RunVisibleUiAction(gen,()=>{_dataSource.Focuses+=10;FocusInputIfVisible();});
 })).GetAwaiter().GetResult();}
 private void Resume(){_screen.Temporary=false;OnApplicationTick();ConversationHelper.Tick();}
 public static void Run(){
   var o=New();o.PostReply("completed while unfocused",true);
   Check(o._mainThreadActions.Count==1&&o._isSubmitting,"background completion only enqueues, no game/UI write on worker");
   OnApplicationTick();ConversationHelper.Tick();
   Check(!o._isSubmitting&&!o._dataSource.Busy&&!o._waitingDotsActive,"paused application tick completes reply and clears busy/dots");
   Check(ConversationHelper.GetCurrentDialogText()=="completed while unfocused","completed reply delivered while pause layer owns input");
   Check(ScreenManager.FocusClaims==0&&o._dataSource.Focuses==0,"no focus or ready interaction through native pause");
   Check(o._deferredVisibleUiActions.Count==1,"ready/focus interaction deferred until visible");
   ConversationHelper.SetCurrentVM(new TestVm());
   o.Resume();
   Check(ConversationHelper.GetCurrentDialogText()=="completed while unfocused","resume replays cached final after VM rebind and EndStreaming");
   Check(o._deferredVisibleUiActions.Count==0&&o._dataSource.Focuses>=10,"deferred ready runs when pause closes");
   int focuses=o._dataSource.Focuses;OnApplicationTick();Check(o._dataSource.Focuses==focuses,"normal following tick does not replay ready action");
   ConversationHelper.SetCurrentVM(new TestVm());OnApplicationTick();ConversationHelper.Tick();
   Check(ConversationHelper.GetCurrentDialogText()=="completed while unfocused","late native VM rebind after resume is repainted within existing restore window");
   for(int i=0;i<3;i++){o._screen.Temporary=true;OnApplicationTick();ConversationHelper.SetCurrentVM(new TestVm());o.Resume();Check(ConversationHelper.GetCurrentDialogText()=="completed while unfocused","repeated focus transition keeps final body");}
   o=New();o.PostReply("partial",false);OnApplicationTick();ConversationHelper.Tick();
   Check(o._isSubmitting&&ConversationHelper.GetCurrentDialogText()=="partial","partial stream retained without fake completion");
   o.Resume();o.PostReply("full after resume",true);OnApplicationTick();ConversationHelper.Tick();
   Check(!o._isSubmitting&&ConversationHelper.GetCurrentDialogText()=="full after resume","stream continues after resume and completes once");
   o=New();o.PostReply("stale NPC reply",true);o._modeTextScope.Valid=false;OnApplicationTick();ConversationHelper.Tick();
   Check(!o._isSubmitting&&o._submissionDisplayText==null,"changed NPC/token/save context retires cached presentation");
   o.Resume();Check(ConversationHelper.GetCurrentDialogText()!="stale NPC reply","old context cannot repaint on resume");
   o=New();o.PostReply("closed reply",true);o.Close(true);o.ProcessInterruptedPresentation();ConversationHelper.Tick();
   Check(o._deferredVisibleUiActions.Count==0&&o._submissionDisplayText==null,"true close discards deferred interactions and text");
   Check(ScreenManager.FocusClaims==0,"closed callback cannot steal focus");
   o=New();o.PostReply("old generation",true);o._submitGeneration++;OnApplicationTick();ConversationHelper.Tick();o.Resume();
   Check(ConversationHelper.GetCurrentDialogText()!="old generation","late generation callback ignored");
   o=New();o.PostReply("first generation body",true);OnApplicationTick();ConversationHelper.Tick();o._submitGeneration++;
   ConversationHelper.SetCurrentVM(new TestVm{DialogText="new request"});o.Resume();
   Check(ConversationHelper.GetCurrentDialogText()=="new request","previous generation cache cannot overwrite new request");
   o=New();o.PostReply("ordinary-mode hidden body",true);OnApplicationTick();ConversationHelper.Tick();o._dataSource.IsCustomAnswerVisible=false;
   ConversationHelper.SetCurrentVM(new TestVm{DialogText="native options"});o.Resume();Check(ConversationHelper.GetCurrentDialogText()=="native options","ordinary mode never repaints AI cache");
   o=New();o.PostReply("cached pre-audio reply",true);OnApplicationTick();ConversationHelper.Tick();
   ConversationHelper.StartTypewriterText("audio-owned typewriter reply",5f,waitForPlayback:true);o.Resume();
   Check(ConversationHelper.IsTypewriterWaitingForPlayback,"resume cache does not replace active TTS typewriter");
   o=New();o.FocusInputIfVisible();Check(ScreenManager.FocusClaims==0,"direct focus request blocked while paused");
   o._temporarySystemUiActive=false;InformationManager.Inquiry=true;o.FocusInputIfVisible();Check(ScreenManager.FocusClaims==0,"active inquiry retains focus");
   InformationManager.Inquiry=false;o.FocusInputIfVisible();Check(ScreenManager.FocusClaims==1,"normal visible input still focuses");
   o=New();int work=0;for(int i=0;i<129;i++)o._mainThreadActions.Enqueue(()=>work++);OnApplicationTick();
   Check(work==128&&o._mainThreadActions.Count==1,"paused queue retains existing 128 callback budget");OnApplicationTick();Check(work==129,"remaining paused callbacks advance next tick");
   o=New();int errors=0;o.RunVisibleUiAction(o._submitGeneration,()=>errors++);Check(errors==0,"failure/retry inquiry does not pop through pause");o.Resume();Check(errors==1,"deferred failure/retry remains available on resume");
   o=New();errors=0;o.RunVisibleUiAction(o._submitGeneration,()=>errors++);o._submitGeneration++;o.Resume();Check(errors==0,"stale deferred inquiry never opens for next request");
   Console.WriteLine("PASS native focus-pause production replay assertions="+Checks);
 }
}
public static class Program {public static void Main()=>AnimusForgeNativeConversationOverlay.Run();}
