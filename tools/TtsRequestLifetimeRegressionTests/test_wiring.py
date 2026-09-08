"""Focused source wiring checks, complementing the executable request-lifetime suite."""
from pathlib import Path
import importlib.util,os,re,unittest
ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
SOURCE=Path(os.environ.get('TTS_CONSUMER_SOURCE',str(ROOT/'ShoutBehavior.cs')))
class Wiring(unittest.TestCase):
 @classmethod
 def setUpClass(cls):cls.s=SOURCE.read_text(encoding='utf-8-sig');cls.e=(ROOT/'TtsEngine.cs').read_text(encoding='utf-8-sig')
 def m(self,name):return ex.declaration(self.s,name)
 def test_typed_subscription_and_unsubscription(self):
  sub=self.m('private void SubscribeTtsPlaybackEvents(');unsub=self.m('private void UnsubscribeTtsPlaybackEventsInternal(')
  for n in ['AudioFileReady','PlaybackStarted','PlaybackFinished','PlaybackFailed','PlaybackCancelled']:
   self.assertIn('.OnRequest'+n+' +=',sub);self.assertIn('.OnRequest'+n+' -=',unsub)
  self.assertNotRegex(sub,r'\.On(?:AudioFileReady|PlaybackStarted|PlaybackFinished|PlaybackFailed) \+=')
 def test_every_subscription_main_queue_callback_has_current_guard(self):
  sub=self.m('private void SubscribeTtsPlaybackEvents(')
  callbacks=re.findall(r'_mainThreadActions\.Enqueue\(delegate\s*\{([\s\S]{0,180})',sub)
  self.assertEqual(len(callbacks),4)
  self.assertTrue(all('if (!IsTtsPlaybackRequestCurrent(request))' in body for body in callbacks))
  self.assertIn('RunTtsMainThreadEventStep(delegate',sub)
  self.assertEqual(sub.count('finally { RetireTtsPlaybackRequest(request); }'),2)
 def test_native_wait_registered_inside_acceptance_callback(self):
  m=self.m('private void TrySpeakNativeConversationReplyWithTts(')
  start=m.index('accepted = TtsEngine.Instance.SpeakAsync(');end=m.index('});',start)
  cb=m[start:end]
  self.assertIn('voiceId, request =>',cb);self.assertIn('RegisterNativeConversationTtsPlaybackWait(request,',cb);self.assertIn('StartTypewriterText(',cb)
  self.assertEqual(m.count('RegisterNativeConversationTtsPlaybackWait('),1)
 def test_scene_waits_registered_inside_acceptance_callback_once(self):
  m=self.m('private SceneSpeechPlaybackInfo ShowNpcSpeechOutput(')
  self.assertEqual(m.count('.SpeakAsync('),1)
  start=m.index('flag = TtsEngine.Instance.SpeakAsync(');end=m.index('});',start);cb=m[start:end]
  for token in ['TrackTtsPlaybackRequest(request, delegate','EnqueuePendingSpeechCompletionToken(','EnqueuePendingNpcBubble(','ScheduleNpcSpeechToMessageFeed(']:self.assertIn(token,cb)
  self.assertIn('clearInteractionToken: true',cb)
 def test_delayed_bubble_and_tableau_check_request(self):
  for name in ['private void SchedulePendingNpcBubbleFallbackDispatch(','private static bool TryQueueNativeMapConversationTableauTtsPlayback(']:
   m=self.m(name);self.assertIn('TtsEngine.PlaybackRequest request',m);self.assertGreaterEqual(m.count('IsTtsPlaybackRequestCurrent(request)'),2)
 def test_stale_audio_gets_cleanup_not_playback(self):
  m=self.m('private void SubscribeTtsPlaybackEvents(')
  self.assertIn('"stale_tts_audio"',m);self.assertIn('"stale_tts_audio_main"',m)
 def test_global_cancel_and_bypass_state_removed(self):
  self.assertNotIn('_cancelCurrent',self.e);self.assertNotIn('_bypassEnabledCheck',self.e);self.assertNotIn('_currentAgentIndex',self.e)
  self.assertIn('!flag2 && !job.BypassEnabledCheck',self.e)
 def test_real_gateway_uses_request_token(self):
  m=ex.declaration(self.e,'private byte[] CallVolcV1Api(')
  self.assertIn('.SynthesizeAsync(request, token, cancellationToken)',m);self.assertNotIn('CancellationToken.None',m)
if __name__=='__main__':unittest.main()
