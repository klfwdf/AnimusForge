import argparse,importlib.util,subprocess,os
from pathlib import Path
import sys
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
import sys
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import current_source_path
from af2_terminal_migration_review import historical_source
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
p=argparse.ArgumentParser();p.add_argument('--original',action='store_true');p.add_argument('--mutate');p.add_argument('--run-root',type=Path);a=p.parse_args()
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
def read(name):return subprocess.check_output(['git','show','5847a195:'+name],cwd=ROOT).decode('utf-8-sig') if a.original else (current_source_path(ROOT, name)).read_text(encoding='utf-8-sig')
s=read('ShoutBehavior.cs');ad=read('ShoutBehavior.NativeAdmission.cs');
# The unchanged boundary is source-projected from verified current phases; NativeTurn executes the new schedule.
import sys
sys.path.insert(0,str(ROOT/'tests/modules/AF.Module.Conversation/NativeConversationAdmissionTests'))
from turn_extraction import projected_source, NEW_SIGNATURE
if NEW_SIGNATURE in s: s=projected_source(s)
body=ex.declaration(s,'private async Task<string> SubmitNativeConversationTextInternalAsync(')
start=body.index('\t\tstring playerName = GetPlayerDisplayNameForShout();') if a.original else body.index('\t\tNativeConversationPendingHistory nativePendingHistory = await')
end=body.index('\t\tbool useSharedDailyMemoryForNpcOpening',start)
values={'PREPARE':body[start:end],'ADMISSION':ex.declaration(ad,'internal sealed class NativeConversationAdmission'),'CHECKS':'\n'.join(ex.declaration(ad,x) for x in ['private bool IsNativeConversationAdmissionCurrent(','private bool IsNativeConversationContextStampCurrent(','private bool IsNativeConversationContextCurrent('])}
selectors=['private static void AppendNativeConversationSessionHistory(','private static void RollbackNativeConversationPendingPlayerHistory(','private static List<AnimusForgeDialogueHistoryEntry> GetNativeConversationSessionHistorySnapshot(','private static List<ConversationMessage> BuildNativeConversationSessionHistoryMessages(','private static AnimusForgeDialogueHistoryEntry CloneNativeConversationHistoryEntry(','private void RemoveNativeConversationSessionHistoryEventFromSceneHistory(','private List<ConversationMessage> ConsumePendingCurrentNativeAfefFactMessagesForPrompt(','private static long NextConversationEventSequence(']
values['HISTORY_METHODS']='\n'.join(ex.declaration(s,x) for x in selectors)
branches=['nativeMainReplyTargetAvailableBeforeDispatch','nativeMainReplyTargetAvailable','nativePostprocessStartTargetAvailable','nativeDirectCommandTargetAvailable','nativePostprocessTargetAvailable']
rejections=[]
for i,name in enumerate(branches):
 if i==0 and not a.original:
  stage=read('src/modules/AF.Module.Conversation/Channels/Native/NativeConversationMainReplyStage.cs')
  branch=ex.declaration(stage,'if (!validation.IsCurrent)')
  # Execute the actual extracted rejection branch and actual captured rollback adapter.
  # Only the test's string-return boundary projects the typed stage result to StopText.
  rejections.append('case 0: { return (await RejectMainReply()).StopText; async Task<NativeConversationMainReplyResult> RejectMainReply() { var validation = new NativeConversationReplyTargetValidation(false, nativeMainReplyTargetUnavailableBeforeDispatchReason); var host = new NativeConversationMainReplyHost(this, admission, nativeTargetLog, nativePendingAfefKey, nativePendingPlayerHistoryEventSequence); '+branch+' throw new Exception("fixture rejection must stop"); } }')
 else: rejections.append('case '+str(i)+': { '+ex.declaration(body,'if (!'+name+')')+' break; }')
values['REJECTIONS']='\n'.join(rejections)
# The fixture returns a capture object instead of visible text; preserve the actual no-capture gate.
if not a.original:
 assert 'if (nativePendingHistory == null) return "";' in values['PREPARE']
 values['PREPARE']=values['PREPARE'].replace('if (nativePendingHistory == null) return "";','if (nativePendingHistory == null) return null;',1)
if not a.original:
 prior=subprocess.check_output(['git','show','5847a195:ShoutBehavior.cs'],cwd=ROOT).decode('utf-8-sig')
 for signature in ['private static void AppendNativeConversationSessionHistory(','private static List<AnimusForgeDialogueHistoryEntry> GetNativeConversationSessionHistorySnapshot(','private static List<ConversationMessage> BuildNativeConversationSessionHistoryMessages(']:
  current=ex.declaration(s,signature);old=ex.declaration(prior,signature)
  inverse=current.replace(current.splitlines()[0],old.splitlines()[0],1).replace('capturedHistoryKey ?? BuildNativeConversationHistoryKey','BuildNativeConversationHistoryKey').replace('maxLines, npc, capturedHistoryKey);','maxLines, npc);')
  assert inverse==old, 'Default history-helper behavior changed: '+signature
# Retain the historical exact renderer inverse above. The current runtime uses the
# migrated preparation/rollback authority, not those historical helper bodies.
if not a.original:
 current_shout=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_text(encoding='utf-8-sig')
 values['HISTORY_METHODS']=ex.declaration(current_shout,'private static void RollbackNativeConversationPendingPlayerHistory(')
 values['HISTORY_METHODS']+='\nprivate static long NextConversationEventSequence()=>SceneConversationHistoryOwner.NextEventSequence();'
if not a.original:
 values['ADMISSION']+='\n'+ex.declaration(ad,'internal sealed class NativeConversationAdmissionException')
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig')

for k,v in values.items():code=code.replace('@@'+k+'@@',v)
if not a.original:
 # This prefix belongs to the fixed old pending-history rejection oracle, not current MainReply runtime.
 host_source=historical_source('ShoutBehavior.NativeMainReply.cs')
 host=ex.declaration(host_source,'private sealed class NativeConversationMainReplyHost')
 prefix=host.split('public Task<string> GenerateAsync(',1)[0].replace(' : INativeConversationMainReplyHost','')
 start=host_source.index('public Task RollbackPendingPlayerHistoryAsync(');end=host_source.index(';',start)+1;rollback=host_source[start:end]
 # Use the exact expression-bodied production rollback port without unused provider members.
 code=code.replace('public partial class ShoutBehavior{','public partial class ShoutBehavior{\n'+prefix+rollback+'\n}\n',1)
assert '@@' not in code
out=new_run_root(ROOT,'NativePendingHistoryBoundaryTests',a.run_root)
if not a.original: (out/'MainReplyContracts.cs').write_text(read('src/modules/AF.Module.Conversation/Channels/Native/NativeConversationMainReplyContracts.cs'),encoding='utf-8')
(out/'Program.cs').write_text(code,encoding='utf-8')
for name in ['AnimusForgeDialogueHistoryEntry.cs','ConversationMessage.cs']:(out/name).write_text(read(name),encoding='utf-8')
if not a.original:
 pending=read('ShoutBehavior.NativePendingHistory.cs')
 app_source=(ROOT/'src/AF.GameAdapter.Bannerlord/Prompt/NativePendingHistoryApplicationAdapter.cs').read_text(encoding='utf-8-sig')
 app_fields=app_source[app_source.index('    private readonly Func<bool>'):app_source.index('    internal NativePendingHistoryApplicationAdapter(')]
 app_signatures=['internal NativePendingHistoryApplicationAdapter(', 'internal Task<T> RunNativePendingHistoryOnMainThreadAsync<T>(', 'internal static void ObserveNativePendingHistory(', 'internal Task<NativeConversationPendingHistory> PrepareNativeConversationPendingHistoryAsync(', 'internal Task RollbackNativeConversationPendingPlayerHistoryAsync(', 'internal void RollbackNativeConversationPendingPlayerHistory(']
 app='\n'.join(ex.declaration(app_source,x) for x in app_signatures)
 capture_source=(ROOT/'src/AF.GameAdapter.Bannerlord/Prompt/SceneHistoryPromptCaptureAdapter.cs').read_text(encoding='utf-8-sig')
 capture_signatures=['internal static string CaptureNativeConversationHistoryKey(', 'internal static ConversationMessage StampConversationMessageWithCurrentMemoryContext(', 'internal void AppendNativeConversationSessionLineToSceneHistoryCaptured(', 'internal static void AppendNativeConversationSessionHistoryCaptured(', 'internal static List<AnimusForgeDialogueHistoryEntry> GetNativeConversationSessionHistorySnapshot(', 'internal static List<ConversationMessage> BuildNativeConversationSessionHistoryMessages(', 'internal List<ConversationMessage> ConsumePendingCurrentNativeAfefFactMessagesForPrompt(', 'internal void RemoveNativeConversationSessionHistoryEventFromSceneHistoryCaptured(']
 capture={x:ex.declaration(capture_source,x) for x in capture_signatures}
 native_source=(ROOT/'src/modules/AF.Module.Conversation/Channels/Native/NativeConversationSessionOwner.cs').read_text(encoding='utf-8-sig')
 native_fields=native_source[native_source.index('    private readonly object _gate'):native_source.index('    internal NativeConversationSessionOwner(')]
 native_signatures=['internal NativeConversationSessionOwner(', 'internal void ClearAll(', 'internal void Append(', 'internal void RollbackPlayerEvent(', 'internal List<AnimusForgeDialogueHistoryEntry> GetTail(', 'internal List<AnimusForgeDialogueHistoryEntry> Snapshot(', 'internal void QueueFact(', 'internal List<ConversationMessage> ConsumeFacts(', 'private static ConversationMessage CloneMessage(', 'private AnimusForgeDialogueHistoryEntry CloneNativeConversationHistoryEntry(', 'private void TrimNativeConversationSessionHistory(', 'internal static int ResolveHistoryLineLimit(', 'internal static List<ConversationMessage> ProjectHistoryMessages(']
 native={x:ex.declaration(native_source,x) for x in native_signatures}
 scene_source=(ROOT/'src/modules/AF.Module.Conversation/Internal/History/SceneConversationHistoryOwner.cs').read_text(encoding='utf-8-sig')
 scene_fields=scene_source[scene_source.index(' internal static int SessionId'):scene_source.index(' internal SceneConversationHistoryOwner(')]
 scene_signatures=['internal SceneConversationHistoryOwner(', 'internal void AppendPublic(', 'internal void AppendNpc(', 'internal void RollbackPlayerEvent(']
 scene={x:ex.declaration(scene_source,x) for x in scene_signatures}
 def mutate_one(text,before,after,count=1):
  assert text.count(before)==count,(a.mutate,before,text.count(before))
  return text.replace(before,after,1)
 scheduler={
  'drop-prepare-guard':('if (!_admissions.IsNativeConversationAdmissionCurrent(admission, out _))','if (false)'),
  'drop-claim':('if (Interlocked.CompareExchange(ref state, 1, 0) != 0) return;','if (false) return;'),
  'keep-expired-live':('winner != completion.Task && Interlocked.CompareExchange(ref state, 2, 0) == 0','winner != completion.Task && Volatile.Read(ref state) == 0'),
  'expire-started':('winner != completion.Task && Interlocked.CompareExchange(ref state, 2, 0) == 0','winner != completion.Task && Interlocked.Exchange(ref state, 2) != 2'),
  'keep-failed-publication':('if (Interlocked.CompareExchange(ref state, 2, 0) == 0) completion.TrySetException(ex);','completion.TrySetException(ex);'),
  'allow-diagnostic-failure':('// Optional diagnostics cannot alter queue ownership or report a fake completion.\n            return;','// Mutated observer.\n            throw;'),
  'drop-rollback-context':('|| !_admissions.IsNativeConversationContextStampCurrent(admission)\n            || !_presentationCurrent(admission.PresentationRevision)','|| false'),
  'recompute-rollback-key':('_sessions.RollbackPlayerEvent(historyKey, eventSequence);','historyKey = SceneHistoryPromptCaptureAdapter.CaptureNativeConversationHistoryKey(admission.Hero, admission.Character, admission.NpcName, admission.AgentIndex);\n        _sessions.RollbackPlayerEvent(historyKey, eventSequence);'),
 }
 if a.mutate in scheduler:app=mutate_one(app,*scheduler[a.mutate])
 if a.mutate in ['ignore-append-key','ignore-read-key']:
  sig=capture_signatures[3 if a.mutate=='ignore-append-key' else 4]
  capture[sig]=mutate_one(capture[sig],'capturedHistoryKey ?? CaptureNativeConversationHistoryKey','CaptureNativeConversationHistoryKey')
 if a.mutate=='remove-player-filter':
  sig='internal void RollbackPlayerEvent(';native[sig]=mutate_one(native[sig],'&& string.Equals(entry.Kind,"player",StringComparison.OrdinalIgnoreCase)','')
 if a.mutate=='remove-user-filter':
  sig='internal void RollbackPlayerEvent(';before='&&string.Equals(m.Role,"user",StringComparison.OrdinalIgnoreCase)';assert scene[sig].count(before)==2;scene[sig]=scene[sig].replace(before,'')
 aliases='using NpcDataPacket=AnimusForge.ShoutBehavior.NpcDataPacket; using NativeConversationAdmission=AnimusForge.ShoutBehavior.NativeConversationAdmission; using NativeConversationPendingHistory=AnimusForge.ShoutBehavior.NativeConversationPendingHistory;'
 imports='using System; using System.Collections.Generic; using System.Linq; using System.Threading; using System.Threading.Tasks; using TaleWorlds.CampaignSystem; using TaleWorlds.MountAndBlade;'
 (out/'PendingHistory.cs').write_text(pending,encoding='utf-8')
 (out/'PendingApplication.cs').write_text(imports+aliases+'namespace AnimusForge.Refactor.Adapters { internal sealed class NativePendingHistoryApplicationAdapter {'+app_fields+app+'} }',encoding='utf-8')
 (out/'NativeSessions.cs').write_text(imports+'namespace AnimusForge { internal sealed class NativeConversationSessionOwner {'+native_fields+'\n'.join(native.values())+'} }',encoding='utf-8')
 (out/'SceneHistory.cs').write_text(imports+'namespace AnimusForge { internal sealed class SceneConversationHistoryOwner {'+scene_fields+'\n'.join(scene.values())+'} }',encoding='utf-8')
 capture_init='private readonly NativeConversationSessionOwner _nativeSessions; private readonly Func<SceneConversationHistoryOwner> _history; internal SceneHistoryPromptCaptureAdapter(NativeConversationSessionOwner sessions,Func<SceneConversationHistoryOwner> history){_nativeSessions=sessions;_history=history;} internal static string CaptureNativeConversationNonHeroUnnamedKey(CharacterObject c,string name,int index,Func<int,MobileParty> resolve)=>throw new NotSupportedException("Hero-only fixture does not support unnamed party capture");'
 (out/'PromptCapture.cs').write_text(imports+aliases+'using TaleWorlds.CampaignSystem.Party; namespace AnimusForge.Refactor.Adapters { internal sealed class SceneHistoryPromptCaptureAdapter {'+capture_init+'\n'.join(capture.values())+'} }',encoding='utf-8')
 # Keep the current full speech rules' pure dependency closure, even for unused members.
 for source in ['src/modules/AF.Module.Llm/Protocol/LlmVisibleReplyNormalizer.cs','src/modules/AF.Module.Economy/Host/GiveAssetTagCodec.cs']:
  (out/Path(source).name).write_text((ROOT/source).read_text(encoding='utf-8-sig'),encoding='utf-8')
 postprocess=(ROOT/'src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs').read_text(encoding='utf-8-sig')
 (out/'ActionSpeechProjection.cs').write_text('using System;using System.Text.RegularExpressions;namespace AnimusForge {internal static class ConversationActionPostprocessOwner {'+ex.declaration(postprocess,'internal static string StripActionTagsForSceneSpeech(')+'}}',encoding='utf-8')
 for name in ['NativeHistoryIdentityProjectionOwner','ConversationSpeechTextRules']:
  (out/(name+'.cs')).write_text((ROOT/('src/modules/AF.Module.Conversation/Internal/History/'+name+'.cs')).read_text(encoding='utf-8-sig'),encoding='utf-8')
 projection=(ROOT/'src/modules/AF.Module.Conversation/Internal/History/SceneHistoryProjectionOwner.cs').read_text(encoding='utf-8-sig')
 (out/'SceneBridgeProjection.cs').write_text(imports+'namespace AnimusForge { internal static class SceneHistoryProjectionOwner {'+ex.declaration(projection,'internal static ConversationMessage BuildNativeSceneBridgeMessage(')+'} }',encoding='utf-8')
 # Fixture setup/inspection aliases the real owners' containers. No second history/fact store.
 # Reflection is cold test setup/assertion only, never part of the extracted production path.
 fixture_root=r"""
 private static readonly NativeConversationSessionOwner _nativeSessionOwner=new(NextConversationEventSequence,260);
 private readonly SceneConversationHistoryOwner _sceneHistoryOwner=new(new object());
 private AnimusForge.Refactor.Adapters.SceneHistoryPromptCaptureAdapter _capture;
 private AnimusForge.Refactor.Adapters.SceneHistoryPromptCaptureAdapter SceneHistoryPromptCapture=>_capture??=new(_nativeSessionOwner,()=>_sceneHistoryOwner);
 private static AnimusForge.Refactor.Adapters.SceneHistoryPromptCaptureAdapter CurrentSceneHistoryPromptCapture()=>CurrentInstance?.SceneHistoryPromptCapture;
 private static T FixtureField<T>(object owner,string name)=>(T)owner.GetType().GetField(name,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(owner);
 private readonly NativeAdmissionApplicationAdapter NativeAdmissions;
 """
 root_anchor='public partial class ShoutBehavior{';assert code.count(root_anchor)==1;code=code.replace(root_anchor,root_anchor+fixture_root,1)
 replacements={
 'private static readonly object _nativeConversationSessionHistoryLock=new object();':'private static object _nativeConversationSessionHistoryLock=>FixtureField<object>(_nativeSessionOwner,"_gate");',
 'private static readonly Dictionary<string,List<AnimusForgeDialogueHistoryEntry>> _nativeConversationSessionHistory=new Dictionary<string,List<AnimusForgeDialogueHistoryEntry>>();':'private static Dictionary<string,List<AnimusForgeDialogueHistoryEntry>> _nativeConversationSessionHistory=>FixtureField<Dictionary<string,List<AnimusForgeDialogueHistoryEntry>>>(_nativeSessionOwner,"_history");',
 'private readonly List<ConversationMessage> _publicConversationHistory=new List<ConversationMessage>();':'private List<ConversationMessage> _publicConversationHistory=>FixtureField<List<ConversationMessage>>(_sceneHistoryOwner,"_publicHistory");',
 'private readonly Dictionary<int,List<ConversationMessage>> _npcConversationHistory=new Dictionary<int,List<ConversationMessage>>();':'private Dictionary<int,List<ConversationMessage>> _npcConversationHistory=>FixtureField<Dictionary<int,List<ConversationMessage>>>(_sceneHistoryOwner,"_npcHistory");',
 'private readonly Dictionary<string,List<ConversationMessage>> _pendingCurrentNativeAfefFactsByKey=new Dictionary<string,List<ConversationMessage>>();':'private Dictionary<string,List<ConversationMessage>> _pendingCurrentNativeAfefFactsByKey=>FixtureField<Dictionary<string,List<ConversationMessage>>>(_nativeSessionOwner,"_pendingFacts");',
 'private static long _currentConversationEventSequence;':'private static long _currentConversationEventSequence {get=>SceneConversationHistoryOwner.CurrentEventSequence;set=>SceneConversationHistoryOwner.CurrentEventSequence=value;}',
 'private class NpcDataPacket{public string Name="NPC";public int AgentIndex=1;}':'internal class NpcDataPacket{public string Name="NPC",UnnamedKey="";public int AgentIndex=1;public bool IsHero=true;}',
 'static class SaveRuntimeGuard{internal static long Generation;internal static bool IsCurrentGeneration(long g)=>g==Generation;}':'static class SaveRuntimeGuard{internal static long Generation;internal static long CaptureGeneration()=>Generation;internal static bool IsCurrentGeneration(long g)=>g==Generation;}',
 'public class Hero{public string StringId="hero";}':'public class Hero{public string Name="Hero";public string StringId{get{AnimusForge.Host.Game();AnimusForge.Host.KeyReads++;return AnimusForge.Host.Key;}}}',
 'public class CharacterObject{public string StringId="npc";}':'public class CharacterObject{public string StringId="npc",Name="NPC";public Hero HeroObject;}',
 'public class Mission{public static Mission Current=new Mission();}':'public class Mission{public static Mission Current=new Mission();public List<Agent> Agents=new();} public class Agent{public int Index;public object Character;public bool IsActive()=>true;}',
 }
 for before,after in replacements.items():assert code.count(before)==1,before;code=code.replace(before,after,1)
 before='internal ShoutBehavior(){CurrentInstance=this;';assert code.count(before)==1
 binding='NativeAdmissions=new NativeAdmissionApplicationAdapter(_nativeAdmissionOwner,IsBannerlordMainThreadForNativeActions,()=>ReferenceEquals(CurrentInstance,this),()=>true,_mainThreadActions.Enqueue,NativeConversationMainThreadPreprocessTimeoutMs,TryResolveNativeConversationTarget,TryResolveNativeConversationAgentIndex,IsNativeConversationResponseTargetAvailableForActionDispatch,(a,t,s,d,p,r,o)=>Task.FromException<string>(new NotSupportedException("Generation is not a pending-history fixture capability")));'
 code=code.replace(before,before+binding+'_nativeSessionOwner.ClearAll();',1)
 (out/'NativeAdmissionApplicationAdapter.cs').write_text((ROOT/'src/modules/AF.Module.Conversation/Channels/Native/NativeAdmissionApplicationAdapter.cs').read_text(encoding='utf-8-sig'),encoding='utf-8')
 (out/'PromptLeaves.cs').write_text(("""using System;using System.Collections.Generic;using TaleWorlds.CampaignSystem;using TaleWorlds.CampaignSystem.Party;using TaleWorlds.MountAndBlade;using NpcDataPacket=AnimusForge.ShoutBehavior.NpcDataPacket;
 namespace TaleWorlds.CampaignSystem.Party {public class MobileParty{}}
 namespace AnimusForge {internal static class NpcInitiatedOpeningRouter{internal static bool TryConsumePendingNativeOpening(Hero h,out string fact,out string prompt,out string source){throw new NotSupportedException("Opening capture is not requested");}} internal static class DuelSettings{internal const int DailyConversationHistoryLineLimitMax=260;internal static int GetDailyConversationHistoryLineLimitForExternal()=>2;} internal static class ShoutUtils{internal static NpcDataPacket ExtractNpcData(Agent a)=>throw new NotSupportedException("Agent recapture is not requested"); internal static string StripNamePrefixedLineSafely(string text,int maxPrefixLength=30)=>text;internal static string StripConversationMetadataPrefix(string text)=>text;}}
 namespace AnimusForge.Refactor.Adapters {internal static class SceneTradeBannerlordAdapter{internal static string GetPlayerDisplayNameForShout(){AnimusForge.Host.Game();AnimusForge.Host.OnPlayerName?.Invoke();AnimusForge.Host.OnPlayerName=null;return AnimusForge.Host.PlayerName;}} internal static class PersonaIdentityPromptCaptureAdapter{internal static int GetCurrentMemoryGameHourForExternal()=>MyBehavior.GetCurrentMemoryGameHourForExternal();}internal static class SceneLocationPromptCaptureAdapter{internal static string ResolveCurrentMemorySceneLabel()=>MyBehavior.ResolveCurrentMemorySceneLabelForExternal();}internal static class LlmRequestConfigurationCaptureAdapter{internal static ConversationSpeechTextOptions CaptureSceneSpeechTextOptions()=>default;}
 internal static class SceneAgentIdentityPromptCaptureAdapter{internal static int TryResolveNativeConversationAgentIndex(Hero h,CharacterObject c)=>1;internal static string GetSceneNpcHistoryNameForPrompt(NpcDataPacket n)=>n.Name;internal static float GetPlayerDistanceToAgentForScenePrompt(int i){Host.Game();return 12f;}internal static void FillSceneMessageHeroIdentity(ConversationMessage m){}internal static bool TryResolveWildernessNonHeroMemory(NpcDataPacket n,Hero h,CharacterObject c,int i,out string id,out string name){throw new NotSupportedException("Hero-only fixture does not resolve wilderness memory");}internal static MobileParty TryResolveWildernessNonHeroMobileParty(int i)=>throw new NotSupportedException("Hero-only fixture does not resolve parties");}}
 """),encoding='utf-8')
spec_core=importlib.util.spec_from_file_location('native_core_fixture',ROOT/'tests/modules/AF.Module.Conversation/NativeModuleSubmissionTests/fixture_support.py');core_fixture=importlib.util.module_from_spec(spec_core);spec_core.loader.exec_module(core_fixture);core_fixture.include_operation_sources(out)
if not a.original:
 core_fixture.include_admission_owner(out);code=core_fixture.migrate_admission_fixture(code);(out/'Program.cs').write_text(code,encoding='utf-8')
reference=''
if not a.original:
 from xml.sax.saxutils import escape
 newtonsoft=ROOT/'local/bannerlord-refs/1.4.7.117484/Newtonsoft.Json.dll';assert newtonsoft.is_file(),newtonsoft
 reference='<ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>'+escape(str(newtonsoft))+'</HintPath></Reference></ItemGroup>'
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion>'+('<DefineConstants>ORIGINAL</DefineConstants>' if a.original else '')+'</PropertyGroup>'+reference+'</Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
dotnet=resolve_dotnet(ROOT);env=minimal_test_environment(dotnet,out)
r=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=150)
log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)
