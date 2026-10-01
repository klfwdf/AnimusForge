using System;
using System.Collections.Generic;
namespace TaleWorlds.CampaignSystem {
 public sealed class Campaign { public static Campaign Current; public ConversationManager ConversationManager = new(); }
 public sealed class ConversationManager { public bool IsConversationInProgress; }
 public sealed class CharacterObject { public Hero HeroObject; public Text Name = new("NPC"); }
 public sealed class Hero { public Clan Clan; public bool IsLord, IsNotable, IsWanderer; }
 public sealed class Clan { public Text Name = new("Clan"); }
 public sealed class Text { readonly string _value; public Text(string value) { _value = value; } public override string ToString() => _value; }
}
namespace TaleWorlds.Core { }
namespace TaleWorlds.InputSystem { public enum InputKey { Invalid, T, Y } }
namespace TaleWorlds.Library {
 public struct Vec3 { public float X; public float DistanceSquared(Vec3 other) => (X-other.X)*(X-other.X); }
 public struct Color { public Color(float r,float g,float b) { } }
 public sealed class InformationMessage { public InformationMessage(string text, Color color) { } }
 public static class InformationManager { public static int Messages; public static void DisplayMessage(InformationMessage message) { Messages++; } }
}
namespace TaleWorlds.Engine {
 public sealed class Scene { public float TimeSpeed = 1; }
 public sealed class SoundEvent { public bool IsValid = true; public int Stops; public int GetSoundId() => 42; public static SoundEvent CreateEventFromExternalFile(string kind,string file,Scene scene,bool is3d,bool isBlocking) => new(); public void SetPosition(TaleWorlds.Library.Vec3 value) { } public void Play() { } public void SetParameter(string key,float value) { } public void Pause() { } public void Resume() { } public void Stop() { Stops++; } }
}
namespace TaleWorlds.MountAndBlade {
 public sealed class Mission { public static Mission Current; public bool MissionEnded; public string SceneName = "fixture"; public TaleWorlds.Engine.Scene Scene = new(); public List<Agent> Agents = new(); }
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
 public static class ConversationHelper { public static void AdjustTypewriterDuration(float duration) { } public static void StartTypewriterPlaybackIfWaiting(float duration=0) { } }
 public sealed class DuelSettings { public bool TtsSceneUseWinmmAudible = true; public float TtsLipSyncSoundEventVolume; public static DuelSettings GetSettings() => new(); }
 public sealed class TtsEngine {
  public static TtsEngine Instance = new();
  public sealed class PlaybackRequest { public long RequestId; public int AgentIndex; public bool IsCancellationRequested; }
  public event Action<PlaybackRequest,string,string,float> OnRequestAudioFileReady;
  public event Action<PlaybackRequest> OnRequestPlaybackStarted, OnRequestPlaybackFinished, OnRequestPlaybackCancelled;
  public event Action<PlaybackRequest,string> OnRequestPlaybackFailed;
  public void Ready(PlaybackRequest request) => OnRequestAudioFileReady?.Invoke(request,null,null,1);
  public void Start(PlaybackRequest request) => OnRequestPlaybackStarted?.Invoke(request);
  public void Finish(PlaybackRequest request) => OnRequestPlaybackFinished?.Invoke(request);
  public void Fail(PlaybackRequest request) => OnRequestPlaybackFailed?.Invoke(request,"fixture");
  public void Cancel(PlaybackRequest request) => OnRequestPlaybackCancelled?.Invoke(request);
  public void PausePlayback() { } public void ResumePlayback() { } public void StopPlayback() { }
  public bool InterruptCurrentPlaybackForAgent(int index,string reason) => false;
 }
}
