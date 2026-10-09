using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace TaleWorlds.CampaignSystem {
 public class Campaign { public static Campaign Current; public bool GameStarted=true; public TaleWorlds.SaveSystem.SaveHandler SaveHandler=new(); public T GetCampaignBehavior<T>() where T:class => null; }
}
namespace TaleWorlds.SaveSystem { public class SaveHandler { public bool IsSaving; public int Saves; public void QuickSaveCurrentGame(){Saves++;} } }
namespace TaleWorlds.MountAndBlade { public static class MBGameManager { public static int Exits; public static void EndGame(){Exits++;} } }
namespace TaleWorlds.Library { public class InformationMessage { public string Text; public InformationMessage(string text){Text=text;} } }
namespace TaleWorlds.Core {
 public class InquiryData { public string Title,Text; public Action Accept; public InquiryData(string title,string text,bool isAffirmativeOptionShown,bool isNegativeOptionShown,string yes,string no,Action accept,Action cancel){Title=title;Text=text;Accept=accept;} }
 public static class InformationManager {
  public static int MainThread,Shows; public static bool Busy; public static InquiryData Last; public static List<string> Messages=new();
  public static bool IsAnyInquiryActive()=>Busy;
  static void Check(){if(Environment.CurrentManagedThreadId!=MainThread)throw new Exception("UI accessed off main thread");}
  public static void HideInquiry(){Check();Busy=false;}
  public static void ShowInquiry(InquiryData data,bool pauseGameActiveState){Check();Shows++;Last=data;Busy=true;}
  public static void DisplayMessage(TaleWorlds.Library.InformationMessage m){Check();Messages.Add(m.Text);}
 }
}
namespace AnimusForge {
 internal static class Logger { public static void Log(string a,string b){} }
 internal static class AnimusForgeModulePaths { public static string GetCurrentModuleRoot()=>"captured-model-root"; }
 internal static class AnimusForgeModelStore { public static void ResolveEmbedding(){} }
 public sealed class OnnxEmbeddingEngine {
  public static OnnxEmbeddingEngine Instance=new(); public TaskCompletionSource<bool> Source=new(TaskCreationOptions.RunContinuationsAsynchronously); public int AsyncCalls,SyncCalls; public string Root;
  public bool IsAvailable {get{SyncCalls++;Thread.Sleep(250);return false;}}
  public Task<bool> InitializeAsync(string root){AsyncCalls++;Root=root;return Source.Task;}
 }
 public static class AnimusForgeApiOnboardingPopup { public static bool IsOpen; }
 public static class DevHistoryEditPopup { public static bool IsOpen; }
 public static class DevLargeSelectionPopup { public static bool IsOpen; }
 public class MyBehavior { public enum SaveAndExitStage {None,WaitingForCurrentSave,WaitingForRequestedQuickSave} public enum SaveAndExitReason {None,MissingOnnx,WeeklyReport} public static MyBehavior Instance=new(); public int Queued; public void QueueMissingOnnxGateCheckAfterOnboarding(){Queued++;} }
 public class KnowledgeLibraryBehavior { public static KnowledgeLibraryBehavior Instance=new(); public Action Complete; public bool ThrowOnOpen; public void OpenPlayerPersonaSetup(Action done){if(ThrowOnOpen)throw new Exception("fixture open failure");Complete=done;} }
 public partial class ModOnboardingBehavior {
  public static ModOnboardingBehavior Instance; private bool _welcomeInProgress,_setupDone; private OnboardingUiStage _activeOnboardingStage; public Action PeaceComplete;
  public bool SetupDone=>_setupDone;
  public ModOnboardingBehavior(){Instance=this;}
  private void ResetYjApiSetup(){}
  private void ShowPeaceSceneConflictChoiceAfterPersona(Action onDone){PeaceComplete=onDone;}
  public void Begin(Action returned,bool imported=false)=>CompleteOnboardingAndOpenPlayerPersonaSetup(returned,imported);
 }
}
