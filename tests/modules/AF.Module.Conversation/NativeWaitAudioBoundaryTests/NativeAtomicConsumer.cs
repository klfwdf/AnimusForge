using System;using System.Collections.Generic;using System.Threading.Tasks;using TaleWorlds.Engine;using TaleWorlds.MountAndBlade;
namespace AnimusForge;
public partial class ShoutBehavior {
 private static readonly object _nativeConversationTtsPlaybackWaitLock=new();
 private static TaskCompletionSource<bool> _nativeConversationTtsPlaybackWaitTcs;
 private static TtsEngine.PlaybackRequest _nativeConversationTtsPlaybackRequest;
 private static long _nativeConversationTtsPlaybackWaitToken;
 private static int _nativeConversationTtsPlaybackWaitAgentIndex=int.MinValue,_nativeConversationTtsPlaybackWaitTimeoutMs;
 private const int NativeConversationTtsPlaybackWaitMinTimeoutMs=30000,NativeConversationTtsPlaybackWaitMaxTimeoutMs=300000;
 private readonly Queue<Action> _mainThreadActions=new();
 private SceneAudioLipSyncController SceneAudio;
 private TypingSink _floatingTextView=new();
 private bool IsTtsPlaybackRequestCurrent(TtsEngine.PlaybackRequest r,bool allowCancelled=false)=>SceneAudio.IsTtsPlaybackRequestCurrent(r,allowCancelled);
 private bool IsActiveTtsPlaybackRequest(TtsEngine.PlaybackRequest r,bool allowCancelled=false)=>SceneAudio.IsActiveTtsPlaybackRequest(r,allowCancelled);
 private void RetireTtsPlaybackRequest(TtsEngine.PlaybackRequest r)=>SceneAudio.RetireTtsPlaybackRequest(r);
 private void CleanupSceneLipSyncAfterPlaybackFinished(int i)=>SceneAudio.CleanupSceneLipSyncAfterPlaybackFinished(i);
 private void ClearPendingTtsBubbleSyncForAgent(int i,bool clearInteractionToken){ }
 private void ClearPendingSceneDialogueFeedForAgent(int i){ }
 private sealed class TypingSink {internal void SetTypingPaused(bool p){Trace.Add("typing:"+p);}}
 private static List<string> Trace=new();
	private static long RegisterNativeConversationTtsPlaybackWait(TtsEngine.PlaybackRequest request, float estimatedDurationSeconds, int textLength)
	{
		if (request == null || request.IsCancellationRequested) { return 0L; }
		int agentIndex = request.AgentIndex;
		TaskCompletionSource<bool> oldTcs = null;
		int timeoutMs = ResolveNativeConversationTtsPlaybackWaitTimeoutMs(estimatedDurationSeconds, textLength);
		TaskCompletionSource<bool> newTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		long token = 0L;
		lock (_nativeConversationTtsPlaybackWaitLock)
		{
			if (request.IsCancellationRequested) { return 0L; }
			oldTcs = _nativeConversationTtsPlaybackWaitTcs;
			_nativeConversationTtsPlaybackRequest = request;
			_nativeConversationTtsPlaybackWaitTcs = newTcs;
			_nativeConversationTtsPlaybackWaitAgentIndex = agentIndex;
			_nativeConversationTtsPlaybackWaitTimeoutMs = timeoutMs;
			_nativeConversationTtsPlaybackWaitToken++;
			token = _nativeConversationTtsPlaybackWaitToken;
		}
		try
		{
			oldTcs?.TrySetResult(true);
		}
		catch
		{
		}
		Logger.Log("NativeConversation", "[TTS] registered native playback wait. agentIndex=" + agentIndex + ", timeoutMs=" + timeoutMs + ", token=" + token);
		return token;
	}
	private static int ResolveNativeConversationTtsPlaybackWaitTimeoutMs(float estimatedDurationSeconds, int textLength)
	{
		double seconds = 0.0;
		if (estimatedDurationSeconds > 0f && !float.IsNaN(estimatedDurationSeconds) && !float.IsInfinity(estimatedDurationSeconds))
		{
			seconds = estimatedDurationSeconds;
		}
		else
		{
			seconds = Math.Max(1.0, Math.Max(0, textLength) * 0.05);
		}
		seconds += 30.0;
		int timeoutMs = (int)Math.Round(seconds * 1000.0);
		return Math.Max(NativeConversationTtsPlaybackWaitMinTimeoutMs, Math.Min(NativeConversationTtsPlaybackWaitMaxTimeoutMs, timeoutMs));
	}
	private static bool CompleteNativeConversationTtsPlaybackWait(TtsEngine.PlaybackRequest request, string reason, bool force = false)
	{
		int agentIndex = request?.AgentIndex ?? int.MinValue;
		TaskCompletionSource<bool> tcs = null;
		int expectedAgentIndex = int.MinValue;
		long token = 0L;
		lock (_nativeConversationTtsPlaybackWaitLock)
		{
			if (_nativeConversationTtsPlaybackWaitTcs == null)
			{
				return false;
			}
			expectedAgentIndex = _nativeConversationTtsPlaybackWaitAgentIndex;
			bool matches = force || (request != null && ReferenceEquals(_nativeConversationTtsPlaybackRequest, request));
			if (!matches)
			{
				return false;
			}
			tcs = _nativeConversationTtsPlaybackWaitTcs;
			token = _nativeConversationTtsPlaybackWaitToken;
			_nativeConversationTtsPlaybackWaitTcs = null;
			_nativeConversationTtsPlaybackRequest = null;
			_nativeConversationTtsPlaybackWaitAgentIndex = int.MinValue;
			_nativeConversationTtsPlaybackWaitTimeoutMs = 0;
		}
		try
		{
			tcs.TrySetResult(true);
		}
		catch
		{
		}
		Logger.Log("NativeConversation", "[TTS] completed native playback wait. reason=" + (reason ?? "") + ", agentIndex=" + agentIndex + ", expectedAgentIndex=" + expectedAgentIndex + ", token=" + token);
		return true;
	}
	private static void CompleteNativeConversationTtsPlaybackWaitByToken(long token, string reason)
	{
		TaskCompletionSource<bool> tcs = null;
		int expectedAgentIndex = int.MinValue;
		lock (_nativeConversationTtsPlaybackWaitLock)
		{
			if (_nativeConversationTtsPlaybackWaitTcs == null || _nativeConversationTtsPlaybackWaitToken != token)
			{
				return;
			}
			tcs = _nativeConversationTtsPlaybackWaitTcs;
			expectedAgentIndex = _nativeConversationTtsPlaybackWaitAgentIndex;
			_nativeConversationTtsPlaybackWaitTcs = null;
			_nativeConversationTtsPlaybackRequest = null;
			_nativeConversationTtsPlaybackWaitAgentIndex = int.MinValue;
			_nativeConversationTtsPlaybackWaitTimeoutMs = 0;
		}
		try
		{
			tcs.TrySetResult(true);
		}
		catch
		{
		}
		Logger.Log("NativeConversation", "[TTS] completed native playback wait by token. reason=" + (reason ?? "") + ", expectedAgentIndex=" + expectedAgentIndex + ", token=" + token);
	}
	private static bool IsNativeConversationTtsPlaybackWaitRequest(TtsEngine.PlaybackRequest request)
	{
		lock (_nativeConversationTtsPlaybackWaitLock)
		{
			if (_nativeConversationTtsPlaybackWaitTcs == null)
			{
				return false;
			}
			return request != null && !request.IsCancellationRequested && ReferenceEquals(_nativeConversationTtsPlaybackRequest, request);
		}
	}
	private void ResumeTtsForNativeConversationReply()
	{
		try
		{
			bool wasPaused = SceneAudio.ClearPauseForNativeReply();
			try
			{
				_floatingTextView?.SetTypingPaused(false);
			}
			catch
			{
			}
			TtsEngine.Instance?.ResumePlayback();
			if (wasPaused)
			{
				Logger.Log("NativeConversation", "[TTS] resumed paused TTS state before native conversation reply playback");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversation", "[TTS] resume before reply playback failed: " + ex.Message);
		}
	}
	private void HandleTtsPlaybackCancelled(TtsEngine.PlaybackRequest request)
	{
		if (request == null) { return; }
		long completedWaitRevision;
		lock (_nativeConversationTtsPlaybackWaitLock) { completedWaitRevision = _nativeConversationTtsPlaybackWaitToken; }
		bool releaseNativeTypewriter = CompleteNativeConversationTtsPlaybackWait(request, "playback_cancelled");
		_mainThreadActions.Enqueue(delegate
		{
			try
			{
				if (!IsTtsPlaybackRequestCurrent(request, allowCancelled: true)) { return; }
				if (releaseNativeTypewriter)
				{
					lock (_nativeConversationTtsPlaybackWaitLock)
					{
						if (_nativeConversationTtsPlaybackWaitToken == completedWaitRevision && _nativeConversationTtsPlaybackWaitTcs == null)
						{
							ConversationHelper.StartTypewriterPlaybackIfWaiting();
						}
					}
				}
				if (request.AgentIndex >= 0 && IsActiveTtsPlaybackRequest(request, allowCancelled: true))
				{
					ClearPendingTtsBubbleSyncForAgent(request.AgentIndex, clearInteractionToken: true);
					ClearPendingSceneDialogueFeedForAgent(request.AgentIndex);
					CleanupSceneLipSyncAfterPlaybackFinished(request.AgentIndex);
				}
			}
			finally { RetireTtsPlaybackRequest(request); }
		});
	}

 internal static void RunNativeAudioBoundary(){
 int checks=0;void Check(bool ok,string name){if(!ok)throw new Exception("native-audio: "+name);checks++;}
 foreach(string mode in new[]{"replacement","cancel","exit","mission","load"}){
 Trace=new();TtsEngine.Instance=new(){Trace=Trace.Add};Mission.Current=new();SaveRuntimeGuard.Generation=10;ConversationHelper.Starts=0;
 var host=new ShoutBehavior();int session=1,epoch=1;host.SceneAudio=new(new SceneAudioLipSyncPort{SceneSessionId=()=>session,ConversationEpoch=()=>epoch,ClearPending=()=>{},Dispatch=a=>host._mainThreadActions.Enqueue(a),Cancelled=host.HandleTtsPlaybackCancelled,CompleteNativeWait=(r,why)=>CompleteNativeConversationTtsPlaybackWait(r,why),Finished=i=>host.CleanupSceneLipSyncAfterPlaybackFinished(i),Report=(s,i,d)=>{},PauseTyping=p=>Trace.Add("typing:"+p),CanLipSyncIndex=(int i,out string why)=>{why="";return true;},UnmarkPlaybackStarted=i=>{}});
 var audio=host.SceneAudio;audio.SubscribeTtsPlaybackEvents();var old=new TtsEngine.PlaybackRequest{RequestId=1,AgentIndex=1};audio.TrackTtsPlaybackRequest(old);audio.PrepareTtsPlaybackRequest(old);
 long oldToken=RegisterNativeConversationTtsPlaybackWait(old,1,1);var oldWait=_nativeConversationTtsPlaybackWaitTcs.Task;
 // Queue the actual cancel adapter before installing the new request; no task-pump model replaces its guarded closure.
 old.IsCancellationRequested=true;host.HandleTtsPlaybackCancelled(old);
 if(mode=="exit")CompleteNativeConversationTtsPlaybackWait(null,"native_conversation_closed",true);
 if(mode=="mission")Mission.Current=new();if(mode=="load")SaveRuntimeGuard.Generation++;
 var next=new TtsEngine.PlaybackRequest{RequestId=2,AgentIndex=1};audio.TrackTtsPlaybackRequest(next);audio.PrepareTtsPlaybackRequest(next);
 long newToken=RegisterNativeConversationTtsPlaybackWait(next,1,1);var newWait=_nativeConversationTtsPlaybackWaitTcs.Task;
 SoundEvent held=new();audio._agentSoundEvents[1]=held;audio.PauseTtsForShoutUi();Trace.Clear();
 while(host._mainThreadActions.Count>0)host._mainThreadActions.Dequeue()();
 CompleteNativeConversationTtsPlaybackWaitByToken(oldToken,"timeout");TtsEngine.Instance.Finish(old);while(host._mainThreadActions.Count>0)host._mainThreadActions.Dequeue()();
 Check(oldWait.IsCompleted&&!newWait.IsCompleted&&ReferenceEquals(_nativeConversationTtsPlaybackRequest,next)&&newToken!=oldToken,mode+" late waiter cannot clear new wait");
 Check(audio._ttsPausedByShoutUi&&ReferenceEquals(audio._agentSoundEvents[1],held)&&held.Stops==0&&!Trace.Contains("resume")&&ConversationHelper.Starts==0,mode+" late cancellation/finish leaves new pause/resources/typewriter");
 // Only the explicit new-reply atom resumes; its original clear->typing->engine ordering is retained.
 host.ResumeTtsForNativeConversationReply();Check(!audio._ttsPausedByShoutUi&&Trace.SequenceEqual(new[]{"typing:False","resume"}),mode+" explicit resume ordering");
 TtsEngine.Instance.Finish(next);while(host._mainThreadActions.Count>0)host._mainThreadActions.Dequeue()();Check(newWait.IsCompleted&&_nativeConversationTtsPlaybackWaitTcs==null,mode+" current natural finish completes native wait");
 var newest=new TtsEngine.PlaybackRequest{RequestId=3,AgentIndex=1};audio.TrackTtsPlaybackRequest(newest);audio.PrepareTtsPlaybackRequest(newest);RegisterNativeConversationTtsPlaybackWait(newest,1,1);var newestWait=_nativeConversationTtsPlaybackWaitTcs.Task;held=new();audio._agentSoundEvents[1]=held;audio.PauseTtsForShoutUi();Trace.Clear();
 TtsEngine.Instance.Finish(next);host.HandleTtsPlaybackCancelled(next);CompleteNativeConversationTtsPlaybackWaitByToken(newToken,"timeout");while(host._mainThreadActions.Count>0)host._mainThreadActions.Dequeue()();
 Check(!newestWait.IsCompleted&&audio._ttsPausedByShoutUi&&ReferenceEquals(audio._agentSoundEvents[1],held)&&held.Stops==0&&!Trace.Contains("resume"),mode+" duplicate finish/cancel after current natural completion cannot change successor");
 audio.UnsubscribeTtsPlaybackEvents();CompleteNativeConversationTtsPlaybackWait(null,"fixture_done",true);
 }
 Console.WriteLine("PASS native-historical-atoms/actual-whole-audio "+checks+" checks; engine/typing/sound are isolated sinks; no real audio/game/io");
 }
}
