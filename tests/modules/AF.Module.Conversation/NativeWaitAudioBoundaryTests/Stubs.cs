using System;
using System.Collections.Generic;
namespace TaleWorlds.CampaignSystem {
 public sealed class Campaign { public static Campaign Current; public TaleWorlds.CampaignSystem.Conversation.ConversationManager ConversationManager = new(); }

 public sealed class CharacterObject { public Hero HeroObject; public bool IsFemale; public Text Name = new("NPC"); }
 public sealed class Hero { public Clan Clan; public bool IsLord, IsNotable, IsWanderer, IsFemale; public float Age=30; public CharacterObject CharacterObject; }
 public sealed class Clan { public Text Name = new("Clan"); }
 public sealed class Text { readonly string _value; public Text(string value) { _value = value; } public override string ToString() => _value; }
}
namespace TaleWorlds.Core { }
namespace TaleWorlds.InputSystem { public enum InputKey { Invalid, T, Y } }
namespace TaleWorlds.Library {
 public struct Vec3 { public float X; public Vec3 AsVec2 => this; public float DistanceSquared(Vec3 other) => (X-other.X)*(X-other.X); }
 public struct Color { public Color(float r,float g,float b) { } }
 public sealed class InformationMessage { public InformationMessage(string text, Color color) { } }
 public static class InformationManager { public static int Messages; public static void DisplayMessage(InformationMessage message) { Messages++; } }
}
namespace TaleWorlds.Engine {
 public sealed class Scene { public float TimeSpeed = 1; }
 public sealed class SoundEvent { public bool IsValid = true; public int Stops; public int GetSoundId() => 42; public static SoundEvent CreateEventFromExternalFile(string kind,string file,Scene scene,bool is3d,bool isBlocking) => new(); public void SetPosition(TaleWorlds.Library.Vec3 value) { } public void Play() { } public void SetParameter(string key,float value) { } public void Pause() { } public void Resume() { } public void Stop() { Stops++; } }
}
namespace TaleWorlds.MountAndBlade {
 public sealed class Mission { public static Mission Current; public bool MissionEnded; public float CurrentTime; public string SceneName = "fixture"; public TaleWorlds.Engine.Scene Scene = new(); public List<Agent> Agents = new(); }
 public sealed class Agent { public static Agent Main; public Mission Mission; public int Index; public bool Active = true, IsHuman = true; public float Health = 100; public TaleWorlds.Library.Vec3 Position; public TaleWorlds.CampaignSystem.CharacterObject Character = new(); public TaleWorlds.CampaignSystem.Text Name = new("NPC"); public AgentVisuals AgentVisuals = new(); public bool IsActive() => Active; public enum FacialAnimChannel { Mid, High } public void SetAgentFacialAnimation(FacialAnimChannel channel,string value,bool active) { } }
 public sealed class AgentVisuals { public int Starts, Ends; public void StartRhubarbRecord(string path,int soundId) { if (soundId<0) Ends++; else Starts++; } }
}
namespace AnimusForge {
 public sealed class ScenePresentationHistoryLine { public string Speaker, Text, Kind; }
 public sealed class ScenePresentationTradeOption { public string Name, ValidationName; public int Available; internal bool IsSettlement; }
 public sealed class ScenePresentationParticipantInfo { public int AgentIndex { get; internal set; } public string Name { get; internal set; } public string Role { get; internal set; } public ScenePresentationParticipantState State { get; internal set; } public bool IsAddressee { get; internal set; } public bool IsInRange { get; internal set; } public TaleWorlds.CampaignSystem.CharacterObject Character { get; internal set; } }
 public static class FreezeWatchdog { public static void Mark(string stage,string message,bool immediate) { } }
 public static class Logger { public static void Log(string kind,string message) { } public static void LogVerbose(string category,string key,Func<string> message,double interval) { } }
 public static class BannerlordExceptionSentinel { public static void ReportObservedException(string category,Exception error,string context) { } }
 public static class SaveRuntimeGuard { public static long Generation; public static long CaptureGeneration() => Generation; public static bool IsCurrentGeneration(long generation) => generation==Generation; }
 public static partial class ConversationHelper { internal static object _displayOwner; internal static int Cleared; internal static void Clear(){Cleared++; IsTypewriterWaitingForPlayback=false;} public static int Ended; public static void EndStreaming() { Ended++; } public static void AdjustTypewriterDuration(float duration) { } public static int Starts; public static bool IsTypewriterWaitingForPlayback; public static void StartTypewriterText(string text,float duration,bool waitForPlayback) { IsTypewriterWaitingForPlayback=waitForPlayback; } public static bool StartTypewriterPlaybackIfWaiting(float duration=0) { Starts++; IsTypewriterWaitingForPlayback=false; return true; } }
 public sealed class DuelSettings { public bool TtsSceneUseWinmmAudible = true; public float TtsLipSyncSoundEventVolume; public bool EnableTtsSpeech=true, TtsVolcDedicatedEnabled=true; public static DuelSettings GetSettings() => new(); }
 public sealed class TtsEngine {
  public static TtsEngine Instance = new(); public bool IsReady=true; public void Initialize() { IsReady=true; }
  public sealed class PlaybackRequest { public long RequestId; public int AgentIndex; public bool IsCancellationRequested; }
  public event Action<PlaybackRequest,string,string,float> OnRequestAudioFileReady;
  public event Action<PlaybackRequest> OnRequestPlaybackStarted, OnRequestPlaybackFinished, OnRequestPlaybackCancelled;
  public event Action<PlaybackRequest,string> OnRequestPlaybackFailed;
  public void Ready(PlaybackRequest request) => OnRequestAudioFileReady?.Invoke(request,null,null,1);
  public void Start(PlaybackRequest request) => OnRequestPlaybackStarted?.Invoke(request);
  public void Finish(PlaybackRequest request) => OnRequestPlaybackFinished?.Invoke(request);
  public void Fail(PlaybackRequest request) => OnRequestPlaybackFailed?.Invoke(request,"fixture");
  public void Cancel(PlaybackRequest request) => OnRequestPlaybackCancelled?.Invoke(request);
  public void PausePlayback() { Trace?.Invoke("pause"); } public void ResumePlayback() { Trace?.Invoke("resume"); } public void StopPlayback() { }
  public bool InterruptCurrentPlaybackForAgent(int index,string reason) => false;
  private long _requestSequence=100; public bool SpeakAccepted=true, InvokeAccept=true, ThrowAfterAccept; public Action AcceptSideEffect; public PlaybackRequest LastRequest; public Action<string> Trace;
  public bool SpeakAsync(string text,int speaker=-1,float volume=-1f,int agentIndex=-1,string voiceId="",Action<PlaybackRequest> accepted=null) {
   Trace?.Invoke("tts:"+text+":"+agentIndex+":"+voiceId);LastRequest=new(){RequestId=++_requestSequence,AgentIndex=agentIndex};
   if(InvokeAccept) accepted?.Invoke(LastRequest); AcceptSideEffect?.Invoke(); if(ThrowAfterAccept)throw new Exception("fixture speak error");return SpeakAccepted;
  }
 }
}

namespace SandBox { }
namespace TaleWorlds.CampaignSystem {
 public interface ICampaignMission { void OnConversationPlay(string a,string b,string c,string d,string path); }
 public static class CampaignMission { public static ICampaignMission Current; }
}
namespace AnimusForge {
 public static class EncyclopediaEntityLinkFormatter { public static string SanitizeUntrustedRichText(string text)=>text; }
}

namespace TaleWorlds.CampaignSystem.Conversation { public sealed class ConversationManager { public bool IsConversationInProgress; public int ActiveToken; public object OneToOneConversationAgent; } }
namespace AnimusForge.Refactor.Modules { internal sealed class CoreDialogueOperation { internal void MarkOwnerAdmitted() {} } }
namespace AnimusForge {
 internal static class PlayerEncounterCompat { internal static bool IsInPostBattleResultFlow()=>false; }
 internal static class NpcInitiatedOpeningRouter { internal static bool TryConsumePendingNativeOpening(TaleWorlds.CampaignSystem.Hero hero,out string fact,out string prompt,out string source) { fact=prompt=source="";return false; } }
 internal static class LlmNonStreamingTransport { internal static System.IDisposable PushOwnerCancellation(System.Threading.CancellationToken token)=>new EmptyScope(); private sealed class EmptyScope:System.IDisposable {public void Dispose(){}} }
}
