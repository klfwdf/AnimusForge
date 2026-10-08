"""Full original 287-line system speech inverse; no generated artifact baseline."""
from pathlib import Path
import importlib.util,re,subprocess
ROOT=Path(__file__).resolve().parents[4]
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
main_path='src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs'
old_source=subprocess.check_output(['git','show','ab1d3e48:'+main_path],cwd=ROOT).decode('utf-8-sig')
old=ex.declaration(old_source,'private void EnqueueSystemNpcShout(')
runtime=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/SceneSystemNpcShoutRuntime.cs').read_text(encoding='utf-8-sig')
new=ex.declaration(runtime,'private Task<bool> CaptureAndQueue(')
new=new.replace('private Task<bool> CaptureAndQueue(','private void EnqueueSystemNpcShout(',1)
assert new.count('return Task.FromResult(false);')==3
new=new.replace('return Task.FromResult(false);','return;',2)
new=new.replace('        return Task.FromResult(false);\n','',1)
capture='''Mission sourceMission = Mission.Current;
            long generation = SaveRuntimeGuard.CaptureGeneration();
            int epoch = _conversationEpoch();
            int sessionId = _sceneSessionId();
            return _dispatcher.RunAsync("scene_system_effects", "scene", speakerData.AgentIndex, () =>
            {
                if (!_isOwnerCurrent() || !SaveRuntimeGuard.IsCurrentGeneration(generation)
                    || !ReferenceEquals(Mission.Current, sourceMission)
                    || epoch != _conversationEpoch() || sessionId != _sceneSessionId())
                    return false;'''
assert new.count(capture)==1
new=new.replace(capture,'_mainThreadActions.Enqueue(delegate\n            {',1)
agent_line='Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == speakerData.AgentIndex);'
assert new.count(agent_line)==1
new=new.replace('\t\t\t\t'+agent_line+'\n                if (!speakerAgent.IsActive() || !ReferenceEquals(agent, speakerAgent)) return false;\n','',1)
anchor='\t\t\t\ttry\n\t\t\t\t{'
assert new.count(anchor)==1;new=new.replace(anchor,anchor+'\n\t\t\t\t\t'+agent_line,1)
end='return true;\n\t\t\t}, false, forceQueue: true);'
assert new.count(end)==1;new=new.replace(end,'});',1)
assert new.count('return false;')==2;new=new.replace('return false;','return;')
new=new.replace('_ports.GetDuelLiteralHit()','_lastShoutDuelLiteralHit').replace('_ports.','')
# Exact reviewed bridge relocation; the original body oracle remains immutable.
bridge=(ROOT/'src/bridges/Diplomacy/DiplomacyConversationBridge.cs').read_text(encoding='utf-8-sig')
assert bridge.count('internal static void ProcessDiplomacyTagsDispatch(')==1
assert 'DiplomacyModuleServices.Conversation.ProcessDiplomacyTags(' in bridge
assert new.count('DiplomacyConversationBridge.ProcessDiplomacyTagsDispatch(')==1
new=new.replace('DiplomacyConversationBridge.ProcessDiplomacyTagsDispatch(', 'DiplomacyBehavior.ProcessDiplomacyTagsDispatch(',1)
tokens=lambda text:re.findall(r'@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\w+|[^\s]',text)
assert tokens(old)==tokens(new),'unreviewed system effects/history/TTS/notification/exit rule drift'
entry=ex.declaration(runtime,'internal async Task<bool> Enqueue(')
assert '() => CaptureAndQueue(speakerAgent, content)' in entry and '_dispatcher.RunAsync("scene_system_capture"' in entry
assert 'return queued != null && await queued.ConfigureAwait(false);' in entry
assert not re.search(r'\bShoutBehavior\s+_\w+',runtime)
assert 'PendingOperationRegistry' not in runtime and 'TaskCompletionSource' not in runtime
composition=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SpeechExecution.cs').read_text(encoding='utf-8-sig')
assert '_sceneSpeechEffects.Ports, _conversationGameThreadDispatcher' in composition
current=(ROOT/main_path).read_text(encoding='utf-8-sig')
facade='private void EnqueueSystemNpcShout(Agent speakerAgent, string content) => _ = _systemNpcShout.Enqueue(speakerAgent, content);'
assert current.count(facade)==1,'actual original system speech consumer is not connected'
assert current.count('?.EnqueueSystemNpcShout(speakerAgent, content);')==1
print('PASS complete original287 system speech inverse; same atomic ports/registry; actual producer→facade→owner consumer')
