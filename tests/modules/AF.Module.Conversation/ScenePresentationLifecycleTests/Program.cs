using AnimusForge;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Engine;
int assertions = 0;
void Require(bool condition, string name) { if (!condition) throw new Exception("FAIL: "+name); assertions++; }
Mission mission = new(); Mission.Current = mission;
Agent.Main = new Agent { Index=0, Mission=mission };
Agent first = new() { Index=1, Mission=mission };
Agent second = new() { Index=2, Mission=mission };
mission.Agents.AddRange(new[] { Agent.Main, first, second });
int activated=-1, deactivated=0, ended=0; long fingerprint=1;
var panel = new ScenePresentationController(a => a?.IsActive()==true, () => 10, m=>null, ()=>5, ()=>fingerprint, i=>activated=i, ()=>deactivated++, ()=>ended++);
Require(panel.EnsurePresentationSession(new[] { first,second },1), "session begins");
Require(panel.GetParticipants().Count==2 && activated==1, "members and movement wired");
panel.CycleParticipant(2); Require(panel.FindPresentationMember(2).State==ScenePresentationParticipantState.Excluded,"seal excludes");
panel.CycleParticipant(2); second.Position=new Vec3 { X=100 }; panel.TickPresentationSession(.1f);
Require(panel.FindPresentationMember(2).InRange,"locked ignores distance");
Agent.Main.Health=99; panel.TickPresentationSession(.05f); Require(!ScenePresentationController._presentationCollapsed,"not before 10Hz");
panel.TickPresentationSession(.05f); Require(ScenePresentationController._presentationCollapsed,"damage collapses panel");
first.Active=false; panel.TickPresentationSession(.1f); Require(panel._presentationAddresseeIndex==2,"removed addressee falls to locked");
panel.BeginRound(0); Require(panel.IsRoundActive(7,1,true,()=>false),"pending group blocks");
Require(!panel.IsRoundActive(8,1,true,()=>false),"unclaimed line expires");
panel.BeginRound(10); var task=new TaskCompletionSource<bool>(); panel.NoteRoundGroup(task.Task,1);
Require(panel.IsRoundActive(11,1,true,()=>false),"running group blocks"); task.SetResult(true);
Require(panel.IsRoundActive(12,1,false,()=>false),"postprocess gate blocks");
Require(panel.IsRoundActive(13,1,true,()=>true),"speech remains busy");
Require(!panel.IsRoundActive(58,1,true,()=>true),"ambient speech grace bounded");
panel.BeginRound(60); panel.NoteRoundGroup(Task.CompletedTask,1); Require(!panel.IsRoundActive(60,2,true,()=>true),"retired epoch ends completed round");

string content,status;
Require(!panel.PrepareTextSubmission("",()=>true,()=>false,()=>false,out content,out status) && status=="", "blank line not sent");
Require(!panel.PrepareTextSubmission("line",()=>false,()=>false,()=>false,out content,out status), "off-thread submission refused");
Require(!panel.PrepareTextSubmission("line",()=>true,()=>true,()=>true,out content,out status) && status.Contains("打断"), "busy round preserves interrupt hint");
Require(panel.PrepareTextSubmission("  a\r\n ",()=>true,()=>false,()=>false,out content,out status) && content=="a", "valid line normalized without alternate pipeline");
var records = Enumerable.Range(5,8).Select(i => new ScenePresentationHistoryRecord { Exists=true,EventSequence=i,Role="assistant",Content="line"+i,SpeakerName="NPC" }).ToList();
bool NormalizeFact(string text,out string fact) { fact=text.StartsWith("[AFEF") ? text : null; return fact!=null; }
Require(panel.BuildHistory(records.Count,i=>records[i],NormalizeFact,40).Count==6,"history keeps two current and four context lines");
records.Add(new ScenePresentationHistoryRecord { Exists=true,EventSequence=13,Role="system",Content="internal prompt" });
records.Add(new ScenePresentationHistoryRecord { Exists=true,EventSequence=14,Role="system",Content="[AFEF NPC行为补充] done" });
var history=panel.BuildHistory(records.Count,i=>records[i],NormalizeFact,40);
Require(history.Last().Kind=="fact" && !history.Any(x=>x.Text=="internal prompt"),"only confirmed AFEF system lines visible");
Require(panel.BuildHistory(records.Count,i=>records[i],NormalizeFact,1).Count==1,"max display lines bounded");
List<(int,int)> staged=new(); int resets=0;
var options=new[] { new ScenePresentationTradeOption {Name="coin",Available=4},new ScenePresentationTradeOption {Name="town",Available=1,IsSettlement=true} };
Require(!panel.StageTrade(options,new[]{0},new[]{1},"给予","NPC",staged.Clear,(i,n)=>staged.Add((i,n)),()=>{},out status),"trade cannot stage without owned target");
panel.TradeOwnsState=true;
Require(panel.StageTrade(options,new[]{0,0,1,-1},new[]{2,2,99,2},"给予","NPC",staged.Clear,(i,n)=>staged.Add((i,n)),()=>{},out status) && staged.SequenceEqual(new[]{(0,2),(1,1)}),"selection deduplicated and settlement singleton");
Require(panel.TradeStaged && panel.TradeSummary=="给予 NPC：coin ×2、town ×1","staged summary preserves ordered UI names");
Require(!panel.StageTrade(options,new[]{0,1},new[]{5,1},"给予","NPC",staged.Clear,(i,n)=>staged.Add((i,n)),()=>{},out status) && staged.Count==0,"bad amount clears transient selection");
Require(!panel.ValidateStagedTrade(true,false,()=>resets++,out status) && resets==1 && !panel.TradeStaged,"departed staged target resets and refuses line");
int cancels=0,previews=0,merges=0; bool charge=false,released=false,down=true; float heldSeconds=0;
var hotkeys=new ScenePresentationHotkeyPort { IsCharging=()=>charge,IsReleased=()=>released,IsDown=()=>down,HeldSeconds=()=>heldSeconds,
 BeginIfPressed=(a,b)=>{charge=true;return true;},Cancel=r=>{charge=false;cancels++;},CaptureMerge=()=>()=>merges++,DrawPreview=()=>previews++ };
Require(panel.UpdateHotkey(hotkeys,TaleWorlds.InputSystem.InputKey.T,TaleWorlds.InputSystem.InputKey.Y) && panel.MergesHotkeyCharge,"session hotkey starts merge charge");
released=true;heldSeconds=.1f;Require(panel.UpdateHotkey(hotkeys,default,default) && !ScenePresentationController._presentationCollapsed && merges==0,"tap expands without merging");
released=false;panel.UpdateHotkey(hotkeys,default,default);heldSeconds=.3f;panel.UpdateHotkey(hotkeys,default,default);Require(previews==1,"held key previews framed audience");
released=true;panel.UpdateHotkey(hotkeys,default,default);Require(merges==1 && cancels==2,"hold release merges once and cancels charge");

Mission.Current = new Mission(); panel.TickPresentationSession(.1f);
Require(!panel.IsPresentationSessionLive() && panel.GetParticipants().Count==0 && deactivated==1,"mission retirement clears state");
int version=ScenePresentationController._presentationVersion; panel.EndPresentationSession("repeat"); Require(ScenePresentationController._presentationVersion==version && deactivated==1,"repeat end idempotent");
Mission.Current=mission; first.Active=true;
int session=1,epoch=1,finished=0,failed=0,cancelled=0,timeoutArms=0;
SceneAudioFailureOutput failureOutput=new(); Queue<Action> dispatched=new(); HashSet<int> started=new();
var port = new SceneAudioLipSyncPort {
 SceneSessionId=()=>session, ConversationEpoch=()=>epoch, Dispatch=a=>dispatched.Enqueue(a),
 Finished=i=>finished++, Cancelled=r=>cancelled++, CompleteNativeWait=(r,e)=>false,
 IsNativeWait=r=>false, QueueMapPlayback=(r,w,x,d)=>false, UseMapPlayback=()=>false,
 CanParticipate=i=>true, CanLipSync=(Agent a,out string r)=>{r="";return true;}, CanLipSyncIndex=(int i,out string r)=>{r="";return true;},
 FailureOutput=i=>failureOutput, UnmarkPlaybackStarted=i=>started.Remove(i), ConvertDialogueFeed=(i,d)=>{}, FlushFailedSpeechEffects=(i,d)=>{}, ShowBubble=(a,t,d)=>failed++, ScheduleInteractionTimeout=(i,t,d)=>timeoutArms++, EstimateTypingDuration=t=>1,
 AudioDuration=(i,d)=>{}, DispatchBubble=(i,f)=>true, ClearOrphanDuration=i=>{}, ScheduleBubbleFallback=r=>{},
 HasPlaybackStarted=i=>started.Contains(i), MarkPlaybackStarted=i=>started.Add(i), ClearAgentPending=i=>{}, ClearAgentInteraction=i=>{},
 PauseTyping=p=>{}, StopTyping=()=>{}, ClearPending=()=>{}, SpeechBusy=()=>false, ClearSpeechQueue=()=>{}, ClearMeetingControl=()=>{}, SuppressMeetingControl=()=>false, Report=(s,i,e)=>{}
};
var audio = new SceneAudioLipSyncController(port); audio.SubscribeTtsPlaybackEvents();
void Drain() { while(dispatched.Count>0) dispatched.Dequeue()(); }
var old=new TtsEngine.PlaybackRequest { RequestId=1, AgentIndex=1 }; var next=new TtsEngine.PlaybackRequest { RequestId=2, AgentIndex=1 };
audio.TrackTtsPlaybackRequest(old); TtsEngine.Instance.Start(old); Drain(); Require(audio.IsActiveTtsPlaybackRequest(old),"first request active");
audio.TrackTtsPlaybackRequest(next); TtsEngine.Instance.Start(next); Drain();
SoundEvent held=new(); audio._agentSoundEvents[1]=held;
TtsEngine.Instance.Finish(old); Drain(); Require(finished==0 && audio._agentSoundEvents[1]==held,"old finish cannot clean new audio");
TtsEngine.Instance.Finish(next); Drain(); Require(finished==1 && !audio.IsTtsPlaybackRequestCurrent(next),"natural finish retires exactly once");
TtsEngine.Instance.Finish(next); Drain(); Require(finished==1,"duplicate finish ignored");
var stale=new TtsEngine.PlaybackRequest { RequestId=3, AgentIndex=1 };audio.TrackTtsPlaybackRequest(stale);SaveRuntimeGuard.Generation++;
TtsEngine.Instance.Start(stale);Drain();Require(!audio.IsTtsPlaybackRequestCurrent(stale),"load generation rejects late callback");
audio.ResetRequestOwnership();Require(audio._ttsPlaybackOwners.Count==0,"load clears resource request identities");
var active=new TtsEngine.PlaybackRequest { RequestId=4, AgentIndex=1 }; audio.TrackTtsPlaybackRequest(active);epoch++;Require(!audio.IsTtsPlaybackRequestCurrent(active),"retired epoch rejected");
epoch--;session++;Require(!audio.IsTtsPlaybackRequestCurrent(active),"new history session rejects old callback");
failureOutput=new SceneAudioFailureOutput { HasBubble=true,Agent=second,UiContent="fallback",TypingDuration=2,HasInteractionToken=true,InteractionToken=9 };
var failure=new TtsEngine.PlaybackRequest { RequestId=6,AgentIndex=2 };audio.TrackTtsPlaybackRequest(failure);
TtsEngine.Instance.Fail(failure);Drain();TtsEngine.Instance.Fail(failure);Drain();
Require(failed==1 && timeoutArms==1 && !audio.IsTtsPlaybackRequestCurrent(failure),"failure produces one text fallback and retires ownership");
var queued=new TtsEngine.PlaybackRequest { RequestId=5, AgentIndex=1 };audio.TrackTtsPlaybackRequest(queued);TtsEngine.Instance.Start(queued);
int startedBefore=started.Count; audio.UnsubscribeTtsPlaybackEvents();Drain();Require(started.Count==startedBefore && !audio.IsTtsPlaybackRequestCurrent(queued),"queued completion retired on unsubscribe");
TtsEngine.Instance.Cancel(active);Require(cancelled==0,"unsubscribe releases all listeners");
audio.CleanupSceneLipSyncAfterPlaybackFinished(1);int deferred=audio._deferredCleanupQueue.Count;audio.CleanupSceneLipSyncAfterPlaybackFinished(1);Require(audio._deferredCleanupQueue.Count==deferred,"duplicate resource finish queues once");
Require(first.AgentVisuals.Ends==1,"native detach once");
audio.StopAllLipSyncPlaybackAndCleanup();Require(audio._agentSoundEvents.Count==0 && audio._deferredCleanupQueue.Count==0,"stop all clears held resources");
Console.WriteLine($"PASS: {assertions} production presentation/audio lifecycle assertions (stubbed game/native, no files).");

SceneSpeechOutputContract.Run();

SceneAudienceToggleCases.Run();
