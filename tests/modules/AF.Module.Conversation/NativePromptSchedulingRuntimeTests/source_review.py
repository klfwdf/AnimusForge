"""Full scheduled body and authoritative slot inverse; explicit reviewed capture atoms."""
from pathlib import Path
import re,importlib.util,subprocess
ROOT=Path(__file__).resolve().parents[4];F=ROOT/'src/modules/AF.Module.Conversation/Channels/Native'
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
def old(path):return subprocess.check_output(['git','show','84f428cd:'+path],cwd=ROOT).decode('utf-8-sig')
def tokens_body(s):return re.findall(r'\S+',s[s.index('{'):])
before=ex.declaration(old('src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativePromptBuild.cs'),'private async Task<MyBehavior.ShoutPromptContext> BuildNativePromptContextScheduledAsync(')
current=(F/'ShoutBehavior.NativePromptBuild.cs').read_text(encoding='utf-8-sig')
after=ex.declaration(current,'private async Task<MyBehavior.ShoutPromptContext> BuildNativePromptContextScheduledAsync(')
new_owner='bool ownerAvailable = await _ports.PromptDispatcher.RunAsync("prompt_build_owner", targetLog, targetAgentIndex, _ports.IsPromptOwnerAvailable, false).ConfigureAwait(false);\n\t\tif (!ownerAvailable)'
assert after.count(new_owner)==1
after=after.replace(new_owner,'MyBehavior owner = MyBehavior.Instance;\n\t\tif (owner == null)')
after=after.replace('_ports.PromptDispatcher.RunAsync(', 'RunNativeConversationMainThreadFuncAsync(').replace('_ports.IsNativeConversationAdmissionCurrent(', 'IsNativeConversationAdmissionCurrent(')
after=after.replace('SharedPromptRoutingWork routingWork = await', 'PromptBuildPhases phases = await').replace('if (routingWork == null)', 'if (phases == null)').replace('PromptBuildPhases phases = routingWork.Phases;', '').replace('(SharedPromptRoutingWork)null)', '(PromptBuildPhases)null)').replace('SharedPromptRoutingRuntime.Run(routingWork);','owner.RunSharedPromptRouting(phases);')
after=after.replace('_ports.CapturePromptRoutingWork(new NativePromptCaptureRequest(targetHero, targetCharacter, routingInput, extraFact, cultureId, hasAnyHero, targetAgentIndex, preprocessExcludedRuleIds))','owner.BeginSharedPromptBuild(targetHero, routingInput, extraFact, cultureId, hasAnyHero, targetCharacter, null, targetAgentIndex, suppressDynamicRuleAndLore: false, usePrefetchedLoreContext: false, prefetchedLoreContext: null, excludedRuleIds: null, preprocessExcludedRuleIds: preprocessExcludedRuleIds, forcedPreprocessRuleIds: null, preprocessMentionedEntities: null)')
after=after.replace('return _ports.CapturePromptKnowledge(phases, targetHero ?? targetCharacter?.HeroObject);', 'owner.CaptureSharedKnowledgeSnapshot(phases, targetHero ?? targetCharacter?.HeroObject); return MyBehavior.CreateSharedKnowledgeWorkInput(phases);').replace('_ports.CompletePromptCapture(phases, targetHero, targetCharacter, weeklyPromptSnapshot)', 'owner.CompleteSharedPromptBuild(phases, targetHero, targetCharacter, weeklyPromptSnapshot)')
assert tokens_body(before)==tokens_body(after),'scheduled body/ordering drift'
main=old('src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs');scheduler=(F/'NativePromptWorkScheduler.cs').read_text(encoding='utf-8-sig')
for name in ['TryAcquireNativeConversationBackgroundPreprocessSlot','RunNativeConversationBackgroundPreprocessAsync','AwaitNativeConversationBackgroundPreprocessAsync','ObserveNativeConversationBackgroundPreprocessLateCompletion']:
 sig=re.search(r'(?:private|internal) (?:static )?(?:async )?(?:bool|void|Task<MyBehavior.ShoutPromptContext>) '+name+r'\(',main).group()
 newsig=re.search(r'(?:private|internal) (?:static )?(?:async )?(?:bool|void|Task<MyBehavior.ShoutPromptContext>) '+name+r'\(',scheduler).group()
 assert tokens_body(ex.declaration(main,sig))==tokens_body(ex.declaration(scheduler,newsig)), 'slot scheduler body drift '+name
for name in ['Sequence','ActiveRequestId','ActiveGeneration','TimeoutCount']:
 assert scheduler.count('private static long _nativeConversationBackgroundPreprocess'+name+';')==1
assert 'ShoutBehavior _' not in current and 'MyBehavior owner' not in current and 'RunSharedPromptRouting(' not in current
ports=(F/'NativeConversationTurnPorts.cs').read_text();assert 'BuildNativePromptContextScheduledAsyncCapability' not in ports
factory=(F/'ShoutBehavior.NativeTurn.cs').read_text().split('private NativeConversationTurnPorts CreateNativeConversationTurnPorts()',1)[1]
expected_capture="""CapturePromptRoutingWork = static request =>
        {
            MyBehavior owner = MyBehavior.Instance;
            if (owner == null) return null;
            PromptBuildPhases phases = owner.BeginSharedPromptBuild(request.Hero, request.Input, request.ExtraFact, request.CultureId, request.HasAnyHero, request.Character, null, request.AgentIndex,
                suppressDynamicRuleAndLore: false, usePrefetchedLoreContext: false, prefetchedLoreContext: null,
                excludedRuleIds: null, preprocessExcludedRuleIds: request.ExcludedRules, forcedPreprocessRuleIds: null, preprocessMentionedEntities: null);
            return owner.CaptureSharedPromptRoutingWork(phases);
        },"""
actual=factory[factory.index('CapturePromptRoutingWork ='):factory.index('CapturePromptKnowledge =')].strip()
assert re.findall(r'\S+',actual)==re.findall(r'\S+',expected_capture),'game capture argument/binding drift'
print('PASS full five-stage body + four slot bodies inverse; no whole scheduled callback/host survives')
