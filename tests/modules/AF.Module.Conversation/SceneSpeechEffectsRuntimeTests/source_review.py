"""Full Publish and audio-completion body inverse; only reviewed scope/once guards differ."""
import importlib.util,re,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
src=ROOT/'src/modules/AF.Module.Conversation/Channels/Scene'
base=subprocess.check_output(['git','show','87e7a0b9:src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SpeechExecution.cs'],cwd=ROOT).decode('utf-8-sig')
old=ex.declaration(base,'private void PublishSceneSpeechQueueItem(')
new=ex.declaration((src/'SceneSpeechEffectController.cs').read_text(encoding='utf-8-sig'),'internal void Publish(')
guard_start=new.index('if (item == null) return;');guard_end=new.index('NpcDataPacket matchedNpc = item.Npc;')
new=new[:guard_start]+new[guard_end:]
new=new.replace('_ports.GetDuelLiteralHit()','_lastShoutDuelLiteralHit').replace('_ports.','')
# Exact reviewed bridge relocation; the original body oracle remains immutable.
bridge=(ROOT/'src/bridges/Diplomacy/DiplomacyConversationBridge.cs').read_text(encoding='utf-8-sig')
assert bridge.count('internal static void ProcessDiplomacyTagsDispatch(')==1
assert 'DiplomacyModuleServices.Conversation.ProcessDiplomacyTags(' in bridge
assert new.count('DiplomacyConversationBridge.ProcessDiplomacyTagsDispatch(')==1
new=new.replace('DiplomacyConversationBridge.ProcessDiplomacyTagsDispatch(', 'DiplomacyBehavior.ProcessDiplomacyTagsDispatch(',1)
def tokens(s):
    pat=r'@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|//[^\n]*|/\*.*?\*/|\w+|[^\s]'
    return [t for t in re.findall(pat,s,re.S) if not t.startswith('//') and not t.startswith('/*')]
def body(s):return s[s.index('{'):]
assert tokens(body(old))==tokens(body(new)), 'Publish rules/effect/history/movement body drift'
# The caller stays a compatibility facade; audio owner remains sole request identity authority.
audio=(src/'ShoutBehavior.SceneAudio.cs').read_text(encoding='utf-8-sig')
assert 'private void HandleSceneTtsPlaybackFinishedOnMainThread(int agentIndex) => _sceneSpeechCompletion.Complete(agentIndex);' in audio
complete=ex.declaration((src/'SceneSpeechCompletionController.cs').read_text(encoding='utf-8-sig'),'internal void Complete(')
complete=complete.replace('_movement.','_sceneMovement.').replace('_ports.MainThreadQueueCount()','_mainThreadActions.Count')
for oldname,newname in [('PrepareInteractionCompletion','PrepareInteractionCompletion'),('RunSceneTtsPlaybackFinishedStep','RunStep'),('FlushPendingSceneDialogueFeedAfterSpeech','FlushDialogueFeed'),('FlushLordsHallMissionEntryAfterSpeech','TryFlushLordsHallEntry'),('FlushMeetingReleaseAfterSpeech','FlushMeetingRelease'),('FlushWorldMapMissionExitAfterSpeech','FlushWorldMapExit'),('FlushSceneAutonomyRestoreAfterSpeech','FlushAutonomyRestore'),('CleanupSceneLipSyncAfterPlaybackFinished','CleanupLipSync')]:complete=complete.replace('_ports.'+newname+'(',oldname+'(')
lifecycle=(src/'SceneInteractionLifecycleController.cs').read_text(encoding='utf-8-sig')
atom=ex.declaration(lifecycle,'internal void PrepareInteractionCompletion(');atom=body(atom)[1:];atom=atom[:atom.rfind('}')]
assert atom.count('_ports.UnmarkPlaybackStarted(agentIndex);')==1
atom=atom.replace('_ports.UnmarkPlaybackStarted(agentIndex);','lock (_ttsBubbleSyncLock) { _ttsPlaybackStartedAgents.Remove(agentIndex); }',1)
assert audio.count('private void PrepareInteractionCompletion(int agentIndex) => _j17SceneInteractionLifecycleController.PrepareInteractionCompletion(agentIndex);')==1
assert complete.count('PrepareInteractionCompletion(agentIndex);')==1
complete=complete.replace('PrepareInteractionCompletion(agentIndex);',atom)
oldaudio=subprocess.check_output(['git','show','87e7a0b9:src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneAudio.cs'],cwd=ROOT).decode('utf-8-sig')
oldcomplete=ex.declaration(oldaudio,'private void HandleSceneTtsPlaybackFinishedOnMainThread(')
assert tokens(body(oldcomplete))==tokens(body(complete)), 'completion interaction/motion/meeting/lipsync order drift'
assert '_owner' not in new and 'ShoutBehavior _' not in new
print('PASS full Publish inverse + full original audio completion inverse + actual producer/runtime/facade consumers')
