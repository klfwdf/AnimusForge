using System;using System.Collections.Generic;using System.Linq;using TaleWorlds.MountAndBlade;using TaleWorlds.CampaignSystem;
namespace AnimusForge;
internal static class MyBehavior { internal static Func<Hero,string> Voice;internal static string GetNpcVoiceIdForExternal(Hero h)=>Voice(h); }
internal static class VoiceMapper { internal static Action<string> Trace;internal static string ResolveVoiceId(Hero h){Trace?.Invoke("hero-voice");return "mapped-hero";}internal static string ResolveVoiceIdForNonHero(bool female,float age,int index){Trace?.Invoke("unnamed-voice:"+index);return "mapped-unnamed";} }
internal static class MeetingBattleLockMissionBehavior { internal static Action Trace;internal static void ReapplyMeetingLockForAgentIfNeeded(Agent a,bool recaptureAnchor,bool preserveFacing)=>Trace(); }
internal sealed class SpeechScenario {
 internal string Content=" hello ",Badge="badge",Name="NPC",ExternalVoice="external";internal bool Active=true,Hero=true,NullNpc,NullAgent,Hostile,Tts=true,LipSafe=true,Allow=true,Attach=true,Suppress,Accepted=true,AcceptCallback=true,ThrowSpeak,Deactivate,ThrowBadge;internal float Duration=.2f;
}
internal sealed class SpeechFixture {
 internal sealed class Session {internal long InteractionToken=9;}
 internal readonly Dictionary<int,Session> Sessions=new(){[1]=new()};internal readonly HashSet<int> Timeouts=new(){1};internal readonly Dictionary<int,Queue<long>> Tokens=new(){[1]=new()};
 internal readonly List<string> Trace=new();internal readonly SceneSpeechOutputPort Port;internal readonly SceneAudioLipSyncController Audio;internal readonly ScenePresentationController Presentation;
 internal readonly Agent Agent;internal readonly NpcDataPacket Npc;internal readonly SpeechScenario Scenario;internal int Epoch=1,SessionId=1;
 internal SpeechFixture(SpeechScenario c){
 Scenario=c;SaveRuntimeGuard.Generation=100;Mission.Current=new();Agent=new(){Index=1,Active=c.Active,Mission=Mission.Current};Npc=c.NullNpc?null:new(){AgentIndex=2,IsHero=c.Hero,Age=30};
 TtsEngine.Instance=new(){SpeakAccepted=c.Accepted,InvokeAccept=c.AcceptCallback,ThrowAfterAccept=c.ThrowSpeak,Trace=Trace.Add,AcceptSideEffect=c.Deactivate?()=>Agent.Active=false:null};
 MyBehavior.Voice=h=>{Trace.Add("external-voice");return c.ExternalVoice;};VoiceMapper.Trace=Trace.Add;MeetingBattleLockMissionBehavior.Trace=()=>Trace.Add("meeting");
 Audio=new(new SceneAudioLipSyncPort{SceneSessionId=()=>SessionId,ConversationEpoch=()=>Epoch});
 Port=new(){SanitizeUiText=t=>t?.Trim(),SanitizeTtsText=t=>t,BuildPatienceBadge=(n,a)=>c.ThrowBadge?throw new Exception("badge"):c.Badge,NpcDisplayName=n=>c.Name,
 IsHostile=a=>c.Hostile,IsTtsEnabled=()=>c.Tts,CanLipSync=(Agent a,out string reason)=>{reason=c.LipSafe?"safe":"unsafe";return c.LipSafe;},ResolveHero=i=>new(),ExternalHeroVoice=MyBehavior.GetNpcVoiceIdForExternal,
 EstimateTypingDuration=t=>c.Duration,Audio=()=>Audio,RemoveHostileInteraction=i=>{Sessions.Remove(i);Timeouts.Remove(i);Tokens.Remove(i);},CaptureInteractionToken=i=>Sessions.TryGetValue(i,out var s)?s.InteractionToken:0,
 Report=(stage,i,detail)=>Trace.Add(stage+":"+detail),ClearPendingBubble=(i,clear)=>Trace.Add("clear-bubble:"+clear),ClearPendingFeed=i=>Trace.Add("clear-feed"),EnqueueCompletionToken=(i,token)=>Trace.Add("token:"+token),
 EnqueueBubble=(i,a,text,name,duration)=>Trace.Add("queue-bubble:"+i+":"+text+":"+duration),ScheduleFeed=(i,name,text,info)=>Trace.Add("feed:"+i+":"+text+":"+info.TtsAccepted+":"+info.WaitForPlaybackFinished),
 PublishFeedImmediately=(i,name,text,info)=>Trace.Add("feed:"+i+":"+text+":"+info.TtsAccepted+":"+info.WaitForPlaybackFinished),
 ShowBubble=(a,text,duration)=>{Trace.Add("bubble:"+text+":"+duration);return false;},ArmInteractionTimeout=(i,token,duration)=>Trace.Add("timeout:"+token+":"+duration)};
 Presentation=new(a=>a!=null&&a.Active,()=>0,m=>null,()=>10,()=>0,i=>{},()=>{},()=>{});
 }
 internal ShoutBehavior.SceneSpeechPlaybackInfo Show(bool oracle){var live=Scenario.NullAgent?null:Agent;return oracle?new SceneSpeechOutputOracle(Port,a=>a!=null&&a.Active,this).ShowNpcSpeechOutput(Npc,live,Scenario.Content,Scenario.Allow,Scenario.Attach,Scenario.Suppress):Presentation.ShowNpcSpeechOutput(Port,Npc,live,Scenario.Content,Scenario.Allow,Scenario.Attach,Scenario.Suppress);}
 internal string Snapshot(ShoutBehavior.SceneSpeechPlaybackInfo info)=>string.Join("\n",Trace)+"\n"+string.Join("|",info.TtsEnabled,info.TtsAccepted,info.WaitForPlaybackFinished,info.VisualDurationSeconds,Sessions.Count,Timeouts.Count,Tokens.Count,Audio._ttsPlaybackOwners.Count);
}
internal static class SceneSpeechOutputContract {
 internal static void Run(){int n=0;void Check(bool ok,string why){if(!ok)throw new Exception("speech output: "+why);n++;}
 var cases=new SpeechScenario[]{new(),new(){Active=false},new(){NullAgent=true},new(){Content=" "},new(){ThrowBadge=true},new(){Badge="",Name=""},new(){NullNpc=true},new(){Allow=false},new(){Tts=false},new(){Hostile=true},new(){Suppress=true},new(){Attach=false},new(){LipSafe=false},new(){Hero=false},new(){ExternalVoice=""},new(){Accepted=false},new(){AcceptCallback=false},new(){ThrowSpeak=true},new(){Deactivate=true},new(){Duration=5}};
 foreach(var c in cases){var baseline=new SpeechFixture(c);var bi=baseline.Show(true);var immediate=baseline.Snapshot(bi);var br=TtsEngine.Instance.LastRequest;if(br!=null)baseline.Audio.PrepareTtsPlaybackRequest(br);var prepared=baseline.Snapshot(bi);
 var actual=new SpeechFixture(c);var ai=actual.Show(false);Check(actual.Snapshot(ai)==immediate,"original151 immediate oracle case"+Array.IndexOf(cases,c));var ar=TtsEngine.Instance.LastRequest;if(ar!=null)actual.Audio.PrepareTtsPlaybackRequest(ar);Check(actual.Snapshot(ai)==prepared,"original151 prepare oracle case"+Array.IndexOf(cases,c));}
 var fixture=new SpeechFixture(new());var info=fixture.Show(false);var req=TtsEngine.Instance.LastRequest;
 Check(info.TtsAccepted&&info.WaitForPlaybackFinished&&fixture.Audio.IsTtsPlaybackRequestCurrent(req),"accepted exact resource authority consumed");Check(!fixture.Trace.Any(x=>x.StartsWith("queue-bubble")),"attached UI deferred until guarded actual audio prepare");
 fixture.Audio.PrepareTtsPlaybackRequest(req);int queued=fixture.Trace.Count(x=>x.StartsWith("queue-bubble"));fixture.Audio.PrepareTtsPlaybackRequest(req);Check(queued==1&&fixture.Trace.Count(x=>x.StartsWith("queue-bubble"))==1,"prepare duplicate dispatch once");
 Check(fixture.Trace.IndexOf("clear-bubble:True")<fixture.Trace.IndexOf("clear-feed")&&fixture.Trace.IndexOf("clear-feed")<fixture.Trace.IndexOf("token:9")&&fixture.Trace.FindIndex(x=>x.StartsWith("queue-bubble"))<fixture.Trace.FindIndex(x=>x.StartsWith("feed:")),"clear/token/bubble/feed preserved original order");
 Check(fixture.Trace.IndexOf("meeting")<fixture.Trace.FindIndex(x=>x.StartsWith("clear-bubble")),"attached acceptance reapplies meeting lock before deferred presentation");
 var fallback=new SpeechFixture(new(){Allow=false});fallback.Show(false);Check(fallback.Trace.FindIndex(x=>x.StartsWith("bubble:"))<fallback.Trace.FindIndex(x=>x.StartsWith("timeout:"))&&fallback.Trace.FindIndex(x=>x.StartsWith("timeout:"))<fallback.Trace.FindIndex(x=>x.StartsWith("feed:"))&&fallback.Trace.FindIndex(x=>x.StartsWith("feed:"))<fallback.Trace.IndexOf("meeting"),"text fallback bubble/timeout/feed/meeting order");

 foreach(var reason in new[]{"cancel","load","mission","session","epoch","reset"}){fixture=new(new());fixture.Show(false);req=TtsEngine.Instance.LastRequest;switch(reason){case "cancel":req.IsCancellationRequested=true;break;case "load":SaveRuntimeGuard.Generation++;break;case "mission":Mission.Current=new();break;case "session":fixture.SessionId++;break;case "epoch":fixture.Epoch++;break;case "reset":fixture.Audio.ResetRequestOwnership();break;}
 Check(!fixture.Audio.PrepareTtsPlaybackRequest(req)&&!fixture.Trace.Any(x=>x.StartsWith("queue-bubble")),"late "+reason+" cannot present stale UI");}
 fixture=new(new(){Accepted=false});info=fixture.Show(false);req=TtsEngine.Instance.LastRequest;Check(!fixture.Audio.IsTtsPlaybackRequestCurrent(req)&&!info.TtsAccepted&&fixture.Trace.Any(x=>x.StartsWith("timeout:9")),"rejected accepted callback retires identity before text fallback timeout");
 fixture=new(new(){Hostile=true,Allow=false});fixture.Show(false);Check(fixture.Sessions.Count==0&&fixture.Timeouts.Count==0&&fixture.Tokens.Count==0&&!fixture.Trace.Any(x=>x.StartsWith("timeout")),"hostile state removed no timeout capture");
 Console.WriteLine($"PASS: {n} actual ShowNpcSpeechOutput/original151 oracle + unique audio request consumer checks (game/TTS fixtures; no files).");
 }
}
