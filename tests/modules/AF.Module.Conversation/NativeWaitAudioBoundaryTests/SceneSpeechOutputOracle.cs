using System;using System.Collections.Generic;using TaleWorlds.CampaignSystem;using TaleWorlds.MountAndBlade;
using SceneSpeechPlaybackInfo=AnimusForge.ShoutBehavior.SceneSpeechPlaybackInfo;
namespace AnimusForge;
public partial class ShoutBehavior { internal sealed class SceneSpeechPlaybackInfo { public bool TtsEnabled,TtsAccepted,WaitForPlaybackFinished;public float VisualDurationSeconds; } }
internal sealed class SceneSpeechOutputOracle {
 private readonly SceneSpeechOutputPort port;private readonly Func<Agent,bool> can;
 private readonly Dictionary<int,SpeechFixture.Session> _activeInteractionSessions;
 private readonly HashSet<int> _pendingInteractionTimeoutArms;
 private readonly Dictionary<int,Queue<long>> _pendingSpeechCompletionTokenQueues;
 private readonly object _ttsBubbleSyncLock=new();
 internal SceneSpeechOutputOracle(SceneSpeechOutputPort p,Func<Agent,bool> c,SpeechFixture f){port=p;can=c;_activeInteractionSessions=f.Sessions;_pendingInteractionTimeoutArms=f.Timeouts;_pendingSpeechCompletionTokenQueues=f.Tokens;}
 private bool CanAgentParticipateInSceneSpeech(Agent a)=>can(a);
 private string SanitizeSceneSpeechText(string text)=>port.SanitizeUiText(text);
 private string BuildPatienceBadgeForNpc(NpcDataPacket n,Agent a)=>port.BuildPatienceBadge(n,a);
 private string GetSceneNpcHistoryNameForPrompt(NpcDataPacket n)=>port.NpcDisplayName(n);
 private bool IsAgentHostileToMainAgent(Agent a)=>port.IsHostile(a);
 private bool IsTtsPlaybackEnabledForShout()=>port.IsTtsEnabled();
 private bool CanAgentUseSceneLipSync(Agent a,out string reason)=>port.CanLipSync(a,out reason);
 private void LogTtsReport(string stage,int i,string detail)=>port.Report(stage,i,detail);
 private string SanitizeSceneSpeechTextForTts(string text)=>port.SanitizeTtsText(text);
 private Hero ResolveHeroFromAgentIndex(int i)=>port.ResolveHero(i);
 private float EstimateBubbleTypingDurationSeconds(string t)=>port.EstimateTypingDuration(t);
 private void TrackTtsPlaybackRequest(TtsEngine.PlaybackRequest r,Action prepare)=>port.Audio().TrackTtsPlaybackRequest(r,prepare);
 private void RetireTtsPlaybackRequest(TtsEngine.PlaybackRequest r)=>port.Audio().RetireTtsPlaybackRequest(r);
 private void ClearPendingTtsBubbleSyncForAgent(int i,bool clearInteractionToken)=>port.ClearPendingBubble(i,clearInteractionToken);
 private void ClearPendingSceneDialogueFeedForAgent(int i)=>port.ClearPendingFeed(i);
 private void EnqueuePendingSpeechCompletionToken(int i,long token)=>port.EnqueueCompletionToken(i,token);
 private void EnqueuePendingNpcBubble(int i,Agent a,string text,string name,float duration)=>port.EnqueueBubble(i,a,text,name,duration);
 private void ScheduleNpcSpeechToMessageFeed(int i,string name,string text,SceneSpeechPlaybackInfo info)=>port.ScheduleFeed(i,name,text,info);
 private bool TryShowNpcBubble(Agent a,string text,float duration)=>port.ShowBubble(a,text,duration);
 private void ScheduleInteractionTimeoutArm(int i,long token,float duration)=>port.ArmInteractionTimeout(i,token,duration);
	internal SceneSpeechPlaybackInfo ShowNpcSpeechOutput(NpcDataPacket npc, Agent liveAgent, string content, bool allowTts = true, bool attachTtsToSceneAgent = true, bool suppressInteractionTimeoutArm = false)
	{
		SceneSpeechPlaybackInfo sceneSpeechPlaybackInfo = new SceneSpeechPlaybackInfo();
		if (!CanAgentParticipateInSceneSpeech(liveAgent))
		{
			return sceneSpeechPlaybackInfo;
		}
		string text = SanitizeSceneSpeechText(content);
		if (string.IsNullOrWhiteSpace(text))
		{
			return sceneSpeechPlaybackInfo;
		}
		try
		{
			string text2 = BuildPatienceBadgeForNpc(npc, liveAgent);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				text = "【" + text2 + "】" + text;
			}
		}
		catch
		{
		}
		int packetAgentIndex = npc?.AgentIndex ?? (-1);
		int num = (liveAgent != null) ? liveAgent.Index : packetAgentIndex;
		if (packetAgentIndex >= 0 && num >= 0 && packetAgentIndex != num)
		{
			LogTtsReport("ShowNpcSpeechOutput.AgentIndexMismatch", num, $"packetAgentIndex={packetAgentIndex};liveAgentIndex={num}");
		}
		string npcDisplayName = GetSceneNpcHistoryNameForPrompt(npc);
		if (string.IsNullOrWhiteSpace(npcDisplayName))
		{
			npcDisplayName = "NPC";
		}
		bool flagHostileSpeech = IsAgentHostileToMainAgent(liveAgent);
		if (flagHostileSpeech && num >= 0)
		{
			_activeInteractionSessions.Remove(num);
			_pendingInteractionTimeoutArms.Remove(num);
			lock (_ttsBubbleSyncLock)
			{
				_pendingSpeechCompletionTokenQueues.Remove(num);
			}
		}
		long interactionToken = 0L;
		if (!flagHostileSpeech && !suppressInteractionTimeoutArm && num >= 0 && _activeInteractionSessions.TryGetValue(num, out var value) && value != null)
		{
			interactionToken = value.InteractionToken;
		}
		bool flag = false;
		TtsEngine.PlaybackRequest acceptedRequest = null;
		bool flag2 = allowTts && IsTtsPlaybackEnabledForShout();
		sceneSpeechPlaybackInfo.TtsEnabled = flag2;
		string text3 = "scene_lipsync_not_requested";
		bool flag3 = flag2 && attachTtsToSceneAgent && num >= 0 && CanAgentParticipateInSceneSpeech(liveAgent) && CanAgentUseSceneLipSync(liveAgent, out text3);
		int num2 = (flag3 ? num : (-1));
		LogTtsReport("ShowNpcSpeechOutput.Enter", num, $"allowTts={allowTts};attachToSceneAgent={attachTtsToSceneAgent};suppressTimeoutArm={suppressInteractionTimeoutArm};effectiveAgentIndex={num2};contentLen={(text ?? string.Empty).Length};hostileSpeech={flagHostileSpeech};lipSyncSafe={flag3};lipSyncReason={text3}");
		if (!allowTts)
		{
			try
			{
				Logger.Log("LipSync", "[SAFEGUARD] Skip TTS for current speech. agentIndex=" + num);
			}
			catch
			{
			}
		}
		else if (flag2 && attachTtsToSceneAgent && num >= 0 && num2 < 0)
		{
			try
			{
				Logger.Log("LipSync", "[SAFEGUARD] Use detached TTS without scene lipsync. agentIndex=" + num + ", reason=" + text3);
			}
			catch
			{
			}
		}
		if (flag2)
		{
			string text4 = "";
			string text5 = SanitizeSceneSpeechTextForTts(text);
			try
			{
				if (npc != null && npc.IsHero)
				{
					Hero hero = ResolveHeroFromAgentIndex(num);
					if (hero != null)
					{
						text4 = MyBehavior.GetNpcVoiceIdForExternal(hero);
						if (string.IsNullOrWhiteSpace(text4))
						{
							text4 = VoiceMapper.ResolveVoiceId(hero);
						}
					}
				}
				if (string.IsNullOrWhiteSpace(text4) && npc != null)
				{
					text4 = VoiceMapper.ResolveVoiceIdForNonHero(npc.IsFemale, npc.Age, num);
				}
				if (!string.IsNullOrWhiteSpace(text5))
				{
					flag = TtsEngine.Instance.SpeakAsync(text5, -1, -1f, num2, text4, request =>
					{
						acceptedRequest = request;
						sceneSpeechPlaybackInfo.TtsAccepted = true;
						sceneSpeechPlaybackInfo.WaitForPlaybackFinished = num2 >= 0;
						sceneSpeechPlaybackInfo.VisualDurationSeconds = Math.Max(0.75f, EstimateBubbleTypingDurationSeconds(text));
						TrackTtsPlaybackRequest(request, delegate
						{
							if (num2 < 0) { return; }
						sceneSpeechPlaybackInfo.VisualDurationSeconds = Math.Max(0.75f, EstimateBubbleTypingDurationSeconds(text));
						ClearPendingTtsBubbleSyncForAgent(num, clearInteractionToken: true);
						ClearPendingSceneDialogueFeedForAgent(num);
						if (interactionToken != 0L)
						{
							EnqueuePendingSpeechCompletionToken(num, interactionToken);
						}
						EnqueuePendingNpcBubble(num, liveAgent, text, npcDisplayName, sceneSpeechPlaybackInfo.VisualDurationSeconds);
						ScheduleNpcSpeechToMessageFeed(num, npcDisplayName, text, sceneSpeechPlaybackInfo);
						});
					});
				}
			}
			catch
			{
			}
			if (!flag && acceptedRequest != null) { RetireTtsPlaybackRequest(acceptedRequest); }
			sceneSpeechPlaybackInfo.TtsAccepted = flag;
			sceneSpeechPlaybackInfo.WaitForPlaybackFinished = flag && num2 >= 0;
			LogTtsReport("ShowNpcSpeechOutput.SpeakAttempt", num, $"effectiveAgentIndex={num2};speakAccepted={flag};voiceId={text4};lipSyncSafe={flag3};lipSyncReason={text3};ttsLen={(text5 ?? string.Empty).Length};uiLen={(text ?? string.Empty).Length}");
		}
		if (flag && num2 >= 0 && CanAgentParticipateInSceneSpeech(liveAgent))
		{
			MeetingBattleLockMissionBehavior.ReapplyMeetingLockForAgentIfNeeded(liveAgent, recaptureAnchor: false, preserveFacing: true);
			return sceneSpeechPlaybackInfo;
		}
		float num3 = EstimateBubbleTypingDurationSeconds(text);
		sceneSpeechPlaybackInfo.VisualDurationSeconds = num3;
		if (!TryShowNpcBubble(liveAgent, text, num3))
		{
			Logger.Log("FloatingText", "[Fallback] bubble unavailable, use message: npc=" + npcDisplayName);
		}
		if (interactionToken != 0L)
		{
			ScheduleInteractionTimeoutArm(num, interactionToken, num3);
		}
		ScheduleNpcSpeechToMessageFeed(num, npcDisplayName, text, sceneSpeechPlaybackInfo);
		MeetingBattleLockMissionBehavior.ReapplyMeetingLockForAgentIfNeeded(liveAgent, recaptureAnchor: false, preserveFacing: true);
		LogTtsReport("ShowNpcSpeechOutput.BubbleFallback", num, $"interactionToken={interactionToken};typingDuration={num3:F2};ttsAccepted={flag};ttsEnabled={flag2}");
		return sceneSpeechPlaybackInfo;
	}

}
