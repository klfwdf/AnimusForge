using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;
namespace AnimusForge;

internal static class NativeWholeConsumer
{
    internal static async Task RunAsync()
    {
        int checks=0;
        void Check(bool ok,string name) { if(!ok) throw new Exception("native-whole: "+name); checks++; }
        foreach(string terminal in new[]{"finish","fail","cancel","fallback","timeout","enqueue-rejected","enqueue-throws","replace","close","mission","save"})
        {
            Console.WriteLine("native-whole scenario "+terminal); Mission.Current=new(); SaveRuntimeGuard.Generation=100; ConversationHelper.Starts=0;
            int nativeAgentIndex = terminal=="cancel" ? 4 : -1;
            Agent.Main = new Agent {Index=0,Mission=Mission.Current};
            if(nativeAgentIndex>=0)Mission.Current.Agents.Add(new Agent{Index=nativeAgentIndex,Mission=Mission.Current});
            var queue=new Queue<Action>(); var delayed=new List<TaskCompletionSource<bool>>();
            var fallbackRegistered=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var fallbackPosted=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<bool> fallbackTimer=null, timeoutTimer=null;
            bool ownerCurrent=true; int clears=0; NativeConversationPlaybackWaitAdapter wait=null;
            SceneAudioLipSyncController audio=null;
            object outputSync = new object();
            var waitPorts=new NativeConversationPlaybackWaitPorts {
                OutputSyncRoot=()=>outputSync, CaptureOwnerCurrent=()=>()=>ownerCurrent,
                PostMainThread=action=>{lock(queue)queue.Enqueue(action);fallbackPosted.TrySetResult(true);}, IsTypewriterWaiting=()=>ConversationHelper.IsTypewriterWaitingForPlayback,
                StartTypewriter=duration=>ConversationHelper.StartTypewriterPlaybackIfWaiting(duration),
                IsPlaybackRequestCurrent=r=>audio.IsTtsPlaybackRequestCurrent(r),
                IsPlaybackRequestCurrentAllowCancelled=r=>audio.IsTtsPlaybackRequestCurrent(r,true),
                IsActivePlaybackRequestAllowCancelled=r=>audio.IsActiveTtsPlaybackRequest(r,true),
                RetirePlaybackRequest=r=>audio.RetireTtsPlaybackRequest(r),
                ClearPendingBubble=i=>clears++, ClearPendingFeed=i=>clears++, CleanupLipSync=i=>audio.CleanupSceneLipSyncAfterPlaybackFinished(i),
                Delay=ms=>{var t=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);lock(delayed) delayed.Add(t);if(ms<30000){fallbackTimer=t;fallbackRegistered.TrySetResult(true);}else timeoutTimer=t;return t.Task;}
            };
            wait=new(waitPorts);
            TtsEngine.Instance=new(); int session=1,epoch=1;
            audio=new(new SceneAudioLipSyncPort { SceneSessionId=()=>session, ConversationEpoch=()=>epoch,
                Dispatch=queue.Enqueue, Cancelled=wait.HandleTtsPlaybackCancelled,
                CompleteNativeWait=(r,why)=>wait.CompleteNativeConversationTtsPlaybackWait(r,why),
                Finished=i=>{}, ClearPending=()=>{}, Report=(s,i,d)=>{},
                FailureOutput=i=>new SceneAudioFailureOutput(), FlushFailedSpeechEffects=(i,d)=>{}, PauseTyping=p=>{}, UnmarkPlaybackStarted=i=>{}, CanLipSyncIndex=(int i,out string why)=>{why="";return true;} });
            NativeConversationSpeechAdapter speech=null;
            var speechPorts=new NativeConversationSpeechPorts { CurrentOwner=()=>ownerCurrent?speech:null,Audio=()=>audio,Wait=wait,
                SetTypingPaused=p=>{},SanitizeUiText=t=>t,SanitizeTtsText=t=>t,
                PreferredHeroVoice=h=>"",MapHeroVoice=h=>"hero",MapNonHeroVoice=(k,f,a,i)=>"npc",
                NonHeroAge=c=>30,NonHeroVoiceKey=(n,h,c,i)=>"voice",CanParticipate=a=>a!=null&&a.IsActive(),IsHostile=a=>false,
                IsValidTargetAgent=(a,h,c)=>false,ResolveTarget=(out TaleWorlds.CampaignSystem.Hero h,out TaleWorlds.CampaignSystem.CharacterObject c,out string n)=>{h=null;c=null;n="";return false;},
                IsInputOpen=()=>true,TypingDuration=t=>1,QueueDiagnostic=i=>default,InteractionDiagnostic=i=>default };
            speech=new(speechPorts);
            if(terminal=="enqueue-rejected")TtsEngine.Instance.SpeakAccepted=false;
            if(terminal=="enqueue-throws")TtsEngine.Instance.ThrowAfterAccept=true;
            // Real accepted callback installs Audio request identity + the production wait before typewriter release.
            TaleWorlds.CampaignSystem.Campaign.Current=new();
            var manager=TaleWorlds.CampaignSystem.Campaign.Current.ConversationManager;
            manager.IsConversationInProgress=true;manager.ActiveToken=7;
            if(nativeAgentIndex>=0)manager.OneToOneConversationAgent=Mission.Current.Agents[0];
            var character=new TaleWorlds.CampaignSystem.CharacterObject();
            var reservation=new NativeConversationAdmissionOwner<ShoutBehavior.NativeConversationAdmission>();
            NativeAdmissionApplicationAdapter admission=null;
            var overlay=new AnimusForgeNativeConversationOverlay();
            admission=new(reservation,()=>true,()=>ownerCurrent,()=>true,queue.Enqueue,1000,
                (out TaleWorlds.CampaignSystem.Hero hero,out TaleWorlds.CampaignSystem.CharacterObject target,out string name)=>{hero=null;target=character;name="NPC";return true;},
                (hero,target)=>nativeAgentIndex,(int index,TaleWorlds.CampaignSystem.Hero hero,TaleWorlds.CampaignSystem.CharacterObject target,out string reason)=>{reason="";return true;},
                async (ticket,text,stream,dialog,postprocess,reply,opening)=>{
                    var result=await NativeConversationMainReplyStage.RunAsync(new WholeNativeReplyHost {Adapter=admission,Admission=ticket},new List<object>(),stream,"NPC","NPC",-1,System.Diagnostics.Stopwatch.StartNew());
                    if(!result.CanContinue)return result.StopText;
                    reply?.Invoke(result.PostprocessReply,null,character);return result.PostprocessReply;
                });
            var lease=admission.CapturePresentation();
            Check(lease!=null,"actual admission captures presentation "+terminal);
            overlay.BindForTest(new ShoutBehavior.NativeConversationPresentationScope(lease));
            int uiGeneration=overlay.GenerationForTest;
            string completed=await admission.SubmitNativeConversationAdmittedAsync("input",null,"",null,
                (reply,hero,target)=>queue.Enqueue(()=>overlay.CallbackForTest(uiGeneration,()=>speech.TrySpeakNativeConversationReplyWithTts(hero,target,new NpcDataPacket(),nativeAgentIndex,reply))),false,lease);
            Check(completed=="reply"&&!admission.IsBusy(),"backend release separate from audio wait "+terminal);
            while(queue.Count>0)queue.Dequeue()();
            var old=TtsEngine.Instance.LastRequest;
            var pending=wait.WaitForNativeConversationTtsPlaybackFinishedForExternalAsync();
            Check(old!=null,"real reply called engine "+terminal);
            if(terminal.StartsWith("enqueue-"))Check(pending.IsCompleted,"rejected enqueue releases only accepted wait "+terminal);
            else Check(!pending.IsCompleted,"actual waiter pending "+terminal);
            if(terminal=="finish")TtsEngine.Instance.Finish(old);
            if(terminal=="fail")TtsEngine.Instance.Fail(old);
            if(terminal=="cancel"){Check(old.AgentIndex==nativeAgentIndex&&audio.PrepareTtsPlaybackRequest(old),"actual Agent cancellation has prepared Audio identity");old.IsCancellationRequested=true;TtsEngine.Instance.Cancel(old);}
            if(terminal=="close")wait.CompleteNativeConversationTtsPlaybackWait(null,"native_conversation_closed",true);
            if(terminal=="timeout")timeoutTimer.TrySetResult(true);
            if(terminal=="fallback") {
                await fallbackRegistered.Task.WaitAsync(TimeSpan.FromSeconds(2));
                fallbackTimer.TrySetResult(true); // Release only fallback; never the unrelated 30s+ native wait timeout.
                await fallbackPosted.Task.WaitAsync(TimeSpan.FromSeconds(2));
            }
            if(terminal is "replace" or "mission" or "save")
            {
                if(terminal=="mission")Mission.Current=new();
                if(terminal=="save")SaveRuntimeGuard.Generation++;
                speech.TrySpeakNativeConversationReplyWithTts(null,null,new NpcDataPacket(),-1,"successor");
                var next=TtsEngine.Instance.LastRequest;var nextWait=wait.WaitForNativeConversationTtsPlaybackFinishedForExternalAsync();
                old.IsCancellationRequested=true;wait.HandleTtsPlaybackCancelled(old);TtsEngine.Instance.Finish(old);
                while(queue.Count>0)queue.Dequeue()();
                await pending.WaitAsync(TimeSpan.FromSeconds(2));
                Check(pending.IsCompleted&&!nextWait.IsCompleted&&wait.IsNativeConversationTtsPlaybackWaitRequest(next),"old waiter cannot complete successor "+terminal);
                Check(clears==0,"old callback cannot clear successor output "+terminal);
                TtsEngine.Instance.Finish(next);while(queue.Count>0)queue.Dequeue()();await nextWait.WaitAsync(TimeSpan.FromSeconds(2));
            }
            while(queue.Count>0)queue.Dequeue()();
            if(terminal=="fallback") {
                Check(ConversationHelper.Starts>0&&!ConversationHelper.IsTypewriterWaitingForPlayback&&!pending.IsCompleted,"real fallback releases typewriter but cannot claim audio finished");
                TtsEngine.Instance.Finish(old);while(queue.Count>0)queue.Dequeue()();
            }
            await pending.WaitAsync(TimeSpan.FromSeconds(2));
            Check(pending.IsCompleted,"actual wait reaches terminal "+terminal);
            if(terminal=="cancel"){Check(clears==2,"accepted Agent cancellation clears bubble and feed exactly once");wait.HandleTtsPlaybackCancelled(old);while(queue.Count>0)queue.Dequeue()();Check(clears==2,"duplicate Agent cancellation cannot clear output twice");}
            if(terminal is "mission" or "save") {
                Check(!overlay.CompleteForTest(uiGeneration)&&!overlay.BusyForTest,"real presentation retires stale context "+terminal);
            } else {
                Check(overlay.CompleteForTest(uiGeneration)&&!overlay.BusyForTest,"real presentation finishes current reply "+terminal);
                Check(!overlay.CompleteForTest(uiGeneration),"duplicate complete cannot end another stream "+terminal);
            }
            ownerCurrent=false;audio.UnsubscribeTtsPlaybackEvents();wait.CompleteNativeConversationTtsPlaybackWait(null,"fixture_done",true);
        }
        Console.WriteLine("PASS production-native-reply/audio/wait "+checks+" checks; actual admission/main-reply/overlay acceptance linked; engine/network/render leaves synthetic; full NativeTurn/host wiring still pending");
    }
}

// Only overlay rendering/dispatch leaves are synthetic; the linked Presentation partial owns every gate/retirement branch.
public sealed partial class AnimusForgeNativeConversationOverlay
{
    private int _submitGeneration;
    private bool _isSubmitting, _npcOpeningAutoStarted;
    private readonly TestNativeDataSource _dataSource = new();
    private bool IsSubmitGenerationCurrent(int generation)=>generation == _submitGeneration;
    private void RunOnMainThread(Action action)=>action();
    private void StopWaitingDotsAnimation(int generation=-1){}
    private void ClearPendingPostprocessNotice(){}
    private void FocusInputIfVisible(){}
    internal void BindForTest(ShoutBehavior.NativeConversationPresentationScope scope) { _submitGeneration++;_submitPresentationScope=scope;_isSubmitting=true;_dataSource.SetBusy(true);ConversationHelper._displayOwner=this; }
    internal int GenerationForTest=>_submitGeneration;
    internal void CallbackForTest(int generation,Action action)=>RunNativePresentationCallback(generation,action);
    internal bool CompleteForTest(int generation)=>CompleteNativeSubmissionPresentation(generation);
    internal void TickForTest()=>ValidatePendingSubmissionPresentation();
    internal bool BusyForTest=>_dataSource.Busy;
    private sealed class TestNativeDataSource { internal bool IsCustomAnswerVisible=true,Busy;internal void SetBusy(bool busy){Busy=busy;} }
}
public partial class ShoutBehavior
{
    // Thin ABI projection over the actual admission lease. No second epoch/revision or acceptance policy.
    internal sealed class NativeConversationPresentationScope
    {
        private readonly NativeAdmissionApplicationAdapter.PresentationLease _lease;
        internal NativeConversationPresentationScope(NativeAdmissionApplicationAdapter.PresentationLease lease){_lease=lease;}
        internal bool IsCurrent()=>_lease.IsCurrent();
        internal bool HasCurrentContext()=>_lease.HasCurrentContext();
    }
}
internal sealed class WholeNativeReplyHost : INativeConversationMainReplyHost
{
    internal NativeAdmissionApplicationAdapter Adapter;
    internal ShoutBehavior.NativeConversationAdmission Admission;
    internal string Response="reply";
    public Task<string> GenerateAsync(List<object> messages,Action<string> stream)=>Task.FromResult(Response);
    public bool IsGenerationStale()=>!SaveRuntimeGuard.IsCurrentGeneration(Admission.Generation);
    public string BuildStaleErrorText()=>"stale";
    public Task<NativeConversationReplyTargetValidation> ValidateTargetAsync(){ bool current=Adapter.IsNativeConversationAdmissionCurrent(Admission,out string reason); return Task.FromResult(new NativeConversationReplyTargetValidation(current,reason)); }
    public Task RollbackPendingPlayerHistoryAsync(string reason)=>Task.CompletedTask;
    public string PreparePostprocessReply(string output)=>output;
    public void ReportProviderFailure(string output){}
}
