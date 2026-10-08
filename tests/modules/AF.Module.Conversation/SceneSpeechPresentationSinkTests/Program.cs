using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Reflection;
using AnimusForge;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using Playback = AnimusForge.ShoutBehavior.SceneSpeechPlaybackInfo;

internal sealed class SinkFixture {
 internal int Epoch=1,Session=1,Bubbles;
 internal bool BubbleReady=true;
 internal readonly Agent Agent;
 internal readonly NpcDataPacket Npc;
 internal readonly ScenePresentationController Presentation;
 internal readonly SceneSpeechOutputQueueController Output;
 internal readonly SceneAudioLipSyncController Audio;
 internal readonly SceneAudioLipSyncPort AudioPorts;
 internal readonly ConcurrentQueue<Action> Actions=new();
 internal readonly SceneSpeechOutputPort Port;
 internal readonly List<float> Timeouts=new();
 internal SinkFixture(bool tts,bool accepted,bool attached,bool bubbleReady=true) {
  InformationManager.Captured.Clear();InformationManager.Messages=0;
  SaveRuntimeGuard.Generation=100;Mission.Current=new(){CurrentTime=32};
  Agent=new(){Index=23,Mission=Mission.Current};Agent.Main=new(){Index=0,Mission=Mission.Current};Mission.Current.Agents.Add(Agent);
  Npc=new(){AgentIndex=23,Name="synthetic-speaker",Age=30};BubbleReady=bubbleReady;
  TtsEngine.Instance=new(){SpeakAccepted=accepted};VoiceMapper.Trace=null;MeetingBattleLockMissionBehavior.Trace=()=>{};
  AudioPorts=new SceneAudioLipSyncPort {SceneSessionId=()=>Session,ConversationEpoch=()=>Epoch};
  Audio=new SceneAudioLipSyncController(AudioPorts);
  var queue=new ConcurrentQueue<Action>();
  var outputPorts=new SceneSpeechOutputQueueControllerPorts {
   Get_mainThreadActions=()=>queue,Set_mainThreadActions=value=>{},Get_sceneMovement=()=>new SceneMovementController(),
   IsSceneConversationEpochCurrent_L223=epoch=>epoch==Epoch,ClearInteractionTimeoutArms=()=>{},
   ResetSceneAudioRequestOwnership_L67=Audio.ResetRequestOwnership,IsTtsPlaybackRequestCurrent_L73=Audio.IsTtsPlaybackRequestCurrent,
   LogTtsReport_L208=(s,i,e)=>{},TryShowNpcBubble_L2127=ShowBubble
  };
  typeof(SceneSpeechOutputQueueControllerPorts).GetField("CaptureConversationEpoch",BindingFlags.Instance|BindingFlags.NonPublic)?.SetValue(outputPorts,(Func<int>)(()=>Epoch));
  Output=new SceneSpeechOutputQueueController(outputPorts);
  Port=new SceneSpeechOutputPort {
   SanitizeUiText=text=>text,SanitizeTtsText=text=>text,BuildPatienceBadge=(npc,agent)=>"",NpcDisplayName=npc=>npc.Name,
   IsHostile=agent=>false,IsTtsEnabled=()=>tts,CanLipSync=(Agent agent,out string reason)=>{reason=attached?"safe":"scene_lipsync_not_requested";return attached;},
   ResolveHero=i=>null,ExternalHeroVoice=hero=>null,EstimateTypingDuration=SceneSpeechOutputQueueController.EstimateBubbleTypingDurationSeconds,Audio=()=>Audio,
   RemoveHostileInteraction=i=>{},CaptureInteractionToken=i=>9,Report=(s,i,e)=>{},
   ClearPendingBubble=Output.ClearPendingTtsBubbleSyncForAgent,ClearPendingFeed=Output.ClearPendingSceneDialogueFeedForAgent,
   EnqueueCompletionToken=Output.EnqueuePendingSpeechCompletionToken,EnqueueBubble=Output.EnqueuePendingNpcBubble,
   ScheduleFeed=Output.ScheduleNpcSpeechToMessageFeed,ShowBubble=ShowBubble,ArmInteractionTimeout=(i,token,duration)=>Timeouts.Add(duration)
  };
  var immediate=typeof(SceneSpeechOutputQueueController).GetMethod("PublishNpcSpeechToMessageFeedImmediately",BindingFlags.Instance|BindingFlags.NonPublic);
  var publish=typeof(SceneSpeechOutputPort).GetField("PublishFeedImmediately",BindingFlags.Instance|BindingFlags.NonPublic);
  if(immediate!=null&&publish!=null)publish.SetValue(Port,Delegate.CreateDelegate(publish.FieldType,Output,immediate));
  Presentation=new(agent=>agent?.Active==true,()=>0,mission=>null,()=>10,()=>0,i=>{},()=>{},()=>{});
 }
 private bool ShowBubble(Agent agent,string text,float duration){if(agent?.Index!=23)throw new Exception("bubble target drift");Bubbles++;return BubbleReady;}
 internal Playback Show(int length,char letter)=>Presentation.ShowNpcSpeechOutput(Port,Npc,Agent,new string(letter,length));
 internal int Messages=>InformationManager.Captured.Count;
}
internal static class Program {
 static int checks;
 static void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
 static void Main(string[] args) {
  if(Array.IndexOf(args,"--baseline")>=0) {
   var old=new SinkFixture(false,false,false);var first=old.Show(797,'甲');var second=old.Show(1103,'乙');
   Check(old.Messages==0&&old.Bubbles==2,"baseline should reproduce delayed feedback");
   Check(Math.Abs(first.VisualDurationSeconds-39.85f)<.01&&Math.Abs(second.VisualDurationSeconds-55.15f)<.01,"baseline real lengths/timing");
   Console.WriteLine("REPRO baseline actual controller->queue->sink: TTSoff, agent23, 797/1103 characters, bubbles2, visible messages0 at fixed mission time32, delayed39.85/55.15 seconds");return;
  }
  foreach(string mode in new[]{"disabled","rejected","disabled-bubble-failed","rejected-bubble-failed"}) {
   bool rejected=mode.StartsWith("rejected");var f=new SinkFixture(rejected,false,false,!mode.EndsWith("failed"));
   var first=f.Show(797,'甲');Check(f.Messages==1,mode+" first reply visible before mission clock moves");
   var second=f.Show(1103,'乙');Check(f.Messages==2&&f.Bubbles==2,mode+" consecutive replies once each");
   Check(!first.TtsAccepted&&!first.WaitForPlaybackFinished&&!second.WaitForPlaybackFinished,mode+" text doesn't wait for audio");
   Check(Math.Abs(first.VisualDurationSeconds-39.85f)<.01&&Math.Abs(second.VisualDurationSeconds-55.15f)<.01,mode+" bubble visual timing preserved");
   Check(f.Timeouts.Count==2&&f.Timeouts[0]==first.VisualDurationSeconds&&f.Timeouts[1]==second.VisualDurationSeconds,mode+" interaction timing preserved");
   f.Output.UpdatePendingSceneDialogueFeeds();f.Output.FlushPendingSceneDialogueFeedAfterSpeech(23);Mission.Current.CurrentTime=1000;f.Output.UpdatePendingSceneDialogueFeeds();
   Check(f.Messages==2,mode+" no delayed duplicate");
  }
  foreach(bool bubble in new[]{true,false}) {
   var f=new SinkFixture(true,true,true,bubble);
   for(int round=0;round<2;round++) {
    var info=f.Show(797+round*306,round==0?'甲':'乙');var request=TtsEngine.Instance.LastRequest;
    Check(f.Messages==round&&info.TtsAccepted&&info.WaitForPlaybackFinished,"attached accepted retains audio timing");
    Check(f.Audio.PrepareTtsPlaybackRequest(request),"attached guarded preparation");
    f.Output.TryDispatchPendingNpcBubbleForTts(23, true);Check(f.Messages==round+(bubble?0:1),"bubble failure publishes pending message immediately");
    f.Output.FlushPendingSceneDialogueFeedAfterSpeech(23);Check(f.Messages==round+1,"attached completion message exactly once");
    f.Output.FlushPendingSceneDialogueFeedAfterSpeech(23);f.Output.UpdatePendingSceneDialogueFeeds();Check(f.Messages==round+1,"attached duplicate completion inert");
   }
  }
  foreach(bool bubble in new[]{true,false}) {
   var f=new SinkFixture(true,true,false,bubble);var info=f.Show(1103,'甲');
   Check(info.TtsAccepted&&!info.WaitForPlaybackFinished,"detached accepted stays detached");
   Check(f.Messages==(bubble?0:1),"detached accepted timing preserved or bubble failure immediate");
   Mission.Current.CurrentTime=1000;f.Output.UpdatePendingSceneDialogueFeeds();Check(f.Messages==1,"detached feed once");
   f.Output.UpdatePendingSceneDialogueFeeds();Check(f.Messages==1,"detached duplicate clock tick inert");
  }
  foreach(string drift in new[]{"mission","generation","epoch","reset"}) {
   var f=new SinkFixture(true,true,false);f.Show(797,'甲');Check(f.Messages==0,"old delayed feed exists before "+drift);
   switch(drift){case "mission":Mission.Current=new(){CurrentTime=1000};break;case "generation":SaveRuntimeGuard.Generation++;break;case "epoch":f.Epoch++;break;case "reset":f.Output.ClearPendingTtsBubbleSyncQueues();break;}
   if(Mission.Current!=null)Mission.Current.CurrentTime=1000;
   f.Output.UpdatePendingSceneDialogueFeeds();f.Output.FlushPendingSceneDialogueFeedAfterSpeech(23);Check(f.Messages==0,"old delayed feed cannot publish after "+drift);
  }
  {
   var f=new SinkFixture(true,true,true);
   f.Output.EnqueuePendingSceneDialogueFeed(23,"old","old-pending",new Color(),true);
   f.Epoch++;
   f.Output.EnqueuePendingSceneDialogueFeed(23,"new","new-pending",new Color(),true);
   f.Output.FlushPendingSceneDialogueFeedAfterSpeech(23);
   Check(f.Messages==0&&f.Output._pendingSceneDialogueFeedQueues[23].Count==1,"reject stale head without advancing into new successor");
   f.Output.FlushPendingSceneDialogueFeedAfterSpeech(23);Check(f.Messages==1,"current completion may publish current successor");
  }
  {
   var f=new SinkFixture(true,true,true);
   f.AudioPorts.Dispatch=action=>f.Actions.Enqueue(action);
   f.AudioPorts.ClearPending=()=>{};
   f.AudioPorts.Report=(stage,index,extra)=>{};
   f.AudioPorts.CompleteNativeWait=(request,reason)=>false;
   f.AudioPorts.Finished=index=>f.Output.FlushPendingSceneDialogueFeedAfterSpeech(index);
   f.Audio.SubscribeTtsPlaybackEvents();
   Check(f.Audio.IsSubscribed,"actual audio event consumer subscribed");
   f.Show(797,'甲');var oldRequest=TtsEngine.Instance.LastRequest;f.Audio.PrepareTtsPlaybackRequest(oldRequest);
   // Queue old finish on the real production audio event owner, then advance the
   // epoch before its game-thread callback can claim the new reply's queue.
   TtsEngine.Instance.Finish(oldRequest);f.Epoch++;
   f.Show(1103,'乙');var newRequest=TtsEngine.Instance.LastRequest;f.Audio.PrepareTtsPlaybackRequest(newRequest);
   while(f.Actions.TryDequeue(out var action))action();
   Check(f.Messages==0,"late old audio finish does not consume new reply");
   Check(f.Output._pendingSceneDialogueFeedQueues[23].Count==1,"new guarded audio preparation replaces old pending feed; late finish leaves it untouched");
   f.Output.UpdatePendingSceneDialogueFeeds();
   Check(f.Messages==0&&f.Output._pendingSceneDialogueFeedQueues[23].Count==1,"tick retires one stale head without consuming new waiting reply");
   TtsEngine.Instance.Finish(newRequest);while(f.Actions.TryDequeue(out var action))action();
   Check(f.Messages==1&&InformationManager.Captured[0].Contains("乙"),"new audio finish publishes only its current reply once");
   TtsEngine.Instance.Finish(oldRequest);while(f.Actions.TryDequeue(out var action))action();
   Check(f.Messages==1,"late retired finish stays inert");f.Audio.UnsubscribeTtsPlaybackEvents();
  }
  Console.WriteLine("PASS SceneSpeechPresentationSink checks="+checks+"; actual full presentation/audio/queue controllers -> visible message leaf; TTSoff+valid23+lipsyncnotrequested, consecutive rounds, timing, bubblefail, dedupe, context retirement; no live bubble-render claim");
 }
}
