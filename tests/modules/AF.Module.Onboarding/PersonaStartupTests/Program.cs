using System;
using System.Diagnostics;
using System.Reflection;
using AnimusForge;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
class Program {
 static int checks,failures;
 static void Check(bool condition,string label){checks++;if(!condition){failures++;Console.WriteLine("FAIL "+label);}}
 static CampaignSaveExitController Reset(){Campaign.Current=new();SaveRuntimeGuard.AdvanceGeneration("test");InformationManager.MainThread=Environment.CurrentManagedThreadId;InformationManager.Shows=0;InformationManager.Busy=false;InformationManager.Last=null;InformationManager.Messages.Clear();ModOnboardingBehavior.Instance=null;KnowledgeLibraryBehavior.Instance=new();MyBehavior.Instance=new();OnnxEmbeddingEngine.Instance=new();DevHistoryEditPopup.IsOpen=DevLargeSelectionPopup.IsOpen=AnimusForgeApiOnboardingPopup.IsOpen=false;var c=new CampaignSaveExitController(()=>{});c.QueueMissingOnnxGateCheck(TimeSpan.Zero);return c;}
 static void Start(CampaignSaveExitController c){c.ProcessPendingMissingOnnxGateCheck();}
 static void GateTests(){
  var c=Reset();var watch=Stopwatch.StartNew();Start(c);watch.Stop();
  Check(OnnxEmbeddingEngine.Instance.SyncCalls==0,"gate never initializes synchronously");Check(watch.ElapsedMilliseconds<200,"pending native work does not block tick");
  Check(OnnxEmbeddingEngine.Instance.AsyncCalls==1,"starts one async check");Check(OnnxEmbeddingEngine.Instance.Root=="captured-model-root","passes detached path");
  for(int i=0;i<20;i++)Start(c);Check(OnnxEmbeddingEngine.Instance.AsyncCalls==1,"does not launch per tick");Check(c._missingOnnxGateActive,"AF admission closed while loading");
  c.ProcessMissingOnnxGateUiResume();c.ShowMissingOnnxGatePopup();Check(InformationManager.Shows==0,"pending load not reported as missing");
  OnnxEmbeddingEngine.Instance.Source.SetResult(true);Start(c);Check(!c._missingOnnxGateActive,"successful completion opens admission");Check(InformationManager.Messages.Contains("本地知识模型已就绪。"),"completion feedback");
  for(int kind=0;kind<3;kind++){c=Reset();if(kind==0)InformationManager.Busy=true;if(kind==1)DevHistoryEditPopup.IsOpen=true;if(kind==2)DevLargeSelectionPopup.IsOpen=true;Start(c);Check(OnnxEmbeddingEngine.Instance.AsyncCalls==0,"defers for UI "+kind);Check(c._pendingMissingOnnxGateCheck,"retains pending for UI "+kind);}
  c=Reset();Start(c);OnnxEmbeddingEngine.Instance.Source.SetResult(false);Start(c);DevLargeSelectionPopup.IsOpen=true;c.ProcessMissingOnnxGateUiResume();Check(InformationManager.Shows==0,"failure does not cover active custom menu");DevLargeSelectionPopup.IsOpen=false;c.ProcessMissingOnnxGateUiResume();Check(InformationManager.Shows==1&&c._missingOnnxGateActive,"unavailable model remains blocked with feedback");
  c=Reset();Start(c);OnnxEmbeddingEngine.Instance.Source.SetException(new Exception("fixture failure"));Start(c);c.ProcessMissingOnnxGateUiResume();Check(c._missingOnnxGateActive&&InformationManager.Shows==1,"fault becomes recoverable error UI");
  c=Reset();Start(c);OnnxEmbeddingEngine.Instance.Source.SetCanceled();Start(c);Check(c._missingOnnxGateActive,"cancelled task not success");
  c=Reset();Start(c);var started=typeof(CampaignSaveExitController).GetField("_onnxStartedAtUtcTicks",BindingFlags.Instance|BindingFlags.NonPublic);
  if(started==null)Check(false,"bounded wait exists");else{started.SetValue(c,DateTime.UtcNow.AddSeconds(-61).Ticks);Start(c);c.ProcessMissingOnnxGateUiResume();Check(InformationManager.Last?.Text.Contains("60 秒")==true,"timeout gives precise feedback");OnnxEmbeddingEngine.Instance.Source.SetResult(true);Start(c);Check(c._missingOnnxGateActive,"late success does not silently override timeout");}
  c=Reset();Start(c);SaveRuntimeGuard.AdvanceGeneration("load");OnnxEmbeddingEngine.Instance.Source.SetResult(false);Start(c);c.ProcessMissingOnnxGateUiResume();Check(!c._missingOnnxGateActive&&InformationManager.Shows==0,"old generation cannot show failure");
  c=Reset();Start(c);Campaign.Current=new();OnnxEmbeddingEngine.Instance.Source.SetResult(false);Start(c);Check(!c._missingOnnxGateActive,"old campaign cannot change new campaign");
  c=Reset();Start(c);c.ExitCurrentGameBecauseOnnxMissing();OnnxEmbeddingEngine.Instance.Source.SetResult(false);Start(c);Check(!c._missingOnnxGateActive,"exit detaches result");Check(Campaign.Current.SaveHandler.Saves==1,"save before exit preserved");
 }
 static void PersonaTests(){
  foreach(bool imported in new[]{false,true}){var c=Reset();var onboarding=new ModOnboardingBehavior();int returned=0;onboarding.Begin(()=>returned++,imported);Check(!onboarding.SetupDone&&onboarding.IsSetupUiActive,"persona retains setup stage imported="+imported);Start(c);Check(OnnxEmbeddingEngine.Instance.AsyncCalls==0,"gate waits for persona");KnowledgeLibraryBehavior.Instance.Complete();Check(!onboarding.SetupDone&&onboarding.IsSetupUiActive,"waits for peace choice too");onboarding.PeaceComplete();onboarding.PeaceComplete();Check(onboarding.SetupDone&&!onboarding.IsSetupUiActive,"stage ends after final choice");Check(returned==1&&MyBehavior.Instance.Queued==1,"finish and queue once");}
  Reset();var old=new ModOnboardingBehavior();old.Begin(()=>throw new Exception("stale callback ran"));SaveRuntimeGuard.AdvanceGeneration("new save");KnowledgeLibraryBehavior.Instance.Complete();Check(old.PeaceComplete==null,"stale persona callback discarded");
  Reset();old=new ModOnboardingBehavior();old.Begin(()=>throw new Exception("replaced owner callback ran"));var replacement=new ModOnboardingBehavior();KnowledgeLibraryBehavior.Instance.Complete();Check(old.PeaceComplete==null&&!replacement.SetupDone,"replaced owner unchanged");
  Reset();old=new ModOnboardingBehavior();KnowledgeLibraryBehavior.Instance.ThrowOnOpen=true;int called=0;old.Begin(()=>called++);Check(called==1&&old.SetupDone&&!old.IsSetupUiActive,"editor open failure releases stage");
  Reset();old=new ModOnboardingBehavior();KnowledgeLibraryBehavior.Instance=null;called=0;old.Begin(()=>called++);Check(called==1&&!old.IsSetupUiActive,"missing knowledge owner releases stage");
 }
 static int Main(){GateTests();PersonaTests();Console.WriteLine($"Checks={checks} Failures={failures}; source-linked controller + exact onboarding callback slice, fake UI/Campaign/model boundary");return failures==0?0:1;}
}
