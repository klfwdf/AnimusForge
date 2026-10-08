from pathlib import Path
import hashlib, importlib.util, json, re, shutil, subprocess, sys
ROOT=Path(__file__).resolve().parents[4]
HERE=Path(__file__).resolve().parent
import argparse
parser=argparse.ArgumentParser()
parser.add_argument('--run-root',type=Path)
args=parser.parse_args()
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root
OUT=new_run_root(ROOT,'native-memory-continuity',args.run_root)
BASE=OUT/'import-prompt-base'
base_result=subprocess.run([sys.executable,'-X','utf8',str(ROOT/'tests/modules/AF.Module.Memory/ImportedMemoryPromptReplayTests/run.py'),'--run-root',str(BASE)],cwd=ROOT,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180)
(OUT/'import-base.log').write_text(base_result.stdout+base_result.stderr,encoding='utf-8')
if base_result.returncode:
 print(base_result.stdout+base_result.stderr)
 raise SystemExit(base_result.returncode)
print('IMPORT_BASE_PASS; same-run current source prerequisite')
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import resolve_dotnet,minimal_test_environment
spec=importlib.util.spec_from_file_location('extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
extract=importlib.util.module_from_spec(spec);spec.loader.exec_module(extract)
sources={}
def read(path):
    p=ROOT/path;sources[path]=hashlib.sha256(p.read_bytes()).hexdigest();return p.read_text(encoding='utf-8-sig')
def dec(text,marker):return extract.declaration(text,marker)
def write(name,text):(OUT/name).write_text(text,encoding='utf-8')
for p in BASE.glob('*.cs'):shutil.copy2(p,OUT/p.name)
for name in ['Proof.csproj','NuGet.Config']:shutil.copy2(BASE/name,OUT/name)
business=read('src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.cs')
identity=read('src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.IdentityStores.cs')
body=(OUT/'Business.cs').read_text(encoding='utf-8').replace('internal sealed class MemoryBusinessStateOwner {','internal sealed partial class MemoryBusinessStateOwner {')
body=body.replace('internal int _activeNativeConversationMemorySessionId=-1;', '')
body=body.replace('internal int GetCurrentNativeConversationMemorySessionIdForSuppression(Func<bool> active)=>-1;internal string BuildCurrentMemorySessionKey(int scene,int dialogue)=>"";', '')
write('Business.cs',body)
extra='internal int _nativeConversationMemorySessionCounter;internal int _activeNativeConversationMemorySessionId=-1;internal string _memoryRuntimeSessionKey=Guid.NewGuid().ToString("N");'
for marker in ['internal int GetOrStartActiveNativeConversationMemorySessionId(','internal int GetCurrentNativeConversationMemorySessionIdForSuppression(','internal string BuildCurrentMemorySessionKey(']:extra+=dec(identity,marker)
for marker in ['internal void SaveDrafts(','internal bool IsDailyMemoryLinePublished(','internal bool AppendDailyMemoryLineById(','internal void FailDaily(']:extra+=dec(business,marker)
# Weekly triggers do not participate in this pure Native dialogue scenario.
extra+='private void AttachPendingWeeklyTriggers(DailyMemoryDraft d,DailyMemoryLine l,int day){}private static bool Same(string a,string b)=>string.Equals(a,b,StringComparison.Ordinal);'
write('BusinessAppend.cs','using System;using System.Collections.Generic;using System.Linq;namespace AnimusForge;internal sealed partial class MemoryBusinessStateOwner {'+extra+'}'+dec(business,'internal sealed class MemoryDailyAppendCapabilities'))
body=(OUT/'SceneProduction.cs').read_text(encoding='utf-8').replace('internal static int TryGetCurrentSceneHistorySessionIdForHistoryPersistence()=>-1;','')
write('SceneProduction.cs',body)
body=(OUT/'MemoryHarness.cs').read_text(encoding='utf-8')
body=body.replace('=> ReplayChecks.Run(args[0], args[1]);','=> ReentryReplay.Run(args[0]);')
write('MemoryHarness.cs',body)
body=(OUT/'IdentityRules.cs').read_text(encoding='utf-8').replace('IsNonSceneNativeConversationActiveForMemory()=>false','IsNonSceneNativeConversationActiveForMemory()=>ReentryReplay.NativeActive && ReentryReplay.SceneSessionId < 0')
write('IdentityRules.cs',body)
body=(OUT/'UncompressedCapture.cs').read_text(encoding='utf-8').replace('GetCurrentSceneSessionIdForDailyMemorySuppression()=>-1','GetCurrentSceneSessionIdForDailyMemorySuppression()=>ReentryReplay.SceneSessionId')
write('UncompressedCapture.cs',body)
# Add exact source key/history capture methods. Only unavailable game-world leaf readers are stubbed.
scene=read('src/AF.GameAdapter.Bannerlord/Prompt/SceneHistoryPromptCaptureAdapter.cs')
body=(OUT/'SceneCapture.cs').read_text(encoding='utf-8').replace('internal sealed class SceneHistoryPromptCaptureAdapter {','internal sealed partial class SceneHistoryPromptCaptureAdapter {')
write('SceneCapture.cs',body)
methods=''.join(dec(scene,x) for x in ['internal static void AppendNativeConversationSessionHistoryCaptured(','internal static string CaptureNativeConversationHistoryKey(','internal static bool HasNativeConversationSessionHistory(','internal static List<AnimusForgeDialogueHistoryEntry> GetNativeConversationSessionHistorySnapshot(','internal static List<ConversationMessage> BuildNativeConversationSessionHistoryMessages('])
write('NativeCapture.cs','using System;using System.Collections.Generic;using System.Linq;using TaleWorlds.CampaignSystem;using TaleWorlds.MountAndBlade;using AnimusForge;using NpcDataPacket=AnimusForge.ShoutBehavior.NpcDataPacket;namespace AnimusForge.Refactor.Adapters;internal sealed partial class SceneHistoryPromptCaptureAdapter {'+methods+'private static string CaptureNativeConversationNonHeroUnnamedKey(CharacterObject c,string n,int i,Func<int,object> resolve)=>"";internal void AppendNativeConversationSessionLineToSceneHistoryCaptured(Hero h,CharacterObject c,string n,string s,string t,string k,long sequence,int target,NpcDataPacket npc,int playerTarget,string playerName){} }')
body=(OUT/'SceneCapture.cs').read_text(encoding='utf-8').replace('internal static float GetPlayerDistanceToAgentForScenePrompt','internal static object TryResolveWildernessNonHeroMobileParty(int index)=>null;internal static float GetPlayerDistanceToAgentForScenePrompt')
write('SceneCapture.cs',body)
body=(OUT/'MemoryHarness.cs').read_text(encoding='utf-8').replace('public class CharacterObject {','public class CharacterObject {public string StringId="character";')
write('MemoryHarness.cs',body)
body=(OUT/'PromptHarness.cs').read_text(encoding='utf-8').replace('internal sealed class NpcDataPacket { public int AgentIndex; }','internal sealed class NpcDataPacket { public int AgentIndex;public bool IsHero=true;public string UnnamedKey="",TroopId=""; }')
body=body.replace('internal static string StripConversationMetadataPrefix','internal static ShoutBehavior.NpcDataPacket ExtractNpcData(TaleWorlds.MountAndBlade.Agent a)=>null;internal static string StripConversationMetadataPrefix')
write('PromptHarness.cs',body)
# Exact gate and request builder. Surrounding role/rule prompt inputs are controlled leaves.
prompt=read('src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurnPrompt.cs')
capture=dec(prompt,'private void CapturePromptMessages(')
sections=read('src/modules/AF.Module.Prompt/Composition/HistorySectionProjectionOwner.cs')
section_body=(OUT/'Sections.cs').read_text(encoding='utf-8')
section_body='using System.Collections.Generic;using System.Linq;'+section_body[:-1]+dec(sections,'internal static List<ConversationMessage> RemoveNativeMessagesAlreadyInPersistentMemory(')+dec(sections,'internal static string BuildConversationMemoryDedupKey(')+'}'
write('Sections.cs',section_body)
# Link the actual Hero identity projection as well as the already linked owner/assembler/protocol files.
proj=(OUT/'Proof.csproj').read_text(encoding='utf-8')
proj=proj.replace('</ItemGroup>','<Compile Include="'+str(ROOT/'src/modules/AF.Module.Conversation/Internal/History/NativeHistoryIdentityProjectionOwner.cs')+'" /></ItemGroup>')
proj=proj.replace('<PropertyGroup>','<PropertyGroup><EnableDefaultCompileItems>false</EnableDefaultCompileItems>',1).replace('</ItemGroup>','<Compile Include="*.cs" /></ItemGroup>',1)
write('Proof.csproj',proj)
for p in re.findall(r'Compile Include="([^"]+)"',proj):
    if '*' in p: continue
    q=Path(p);sources[q.relative_to(ROOT).as_posix()]=hashlib.sha256(q.read_bytes()).hexdigest()
persona=read('src/AF.GameAdapter.Bannerlord/Prompt/PersonaIdentityPromptCaptureAdapter.cs')
ledger=read('src/modules/AF.Module.Memory/Records/DialogueHistoryLedger.cs')
write('DialogueStorageWrappers.cs','using System;using TaleWorlds.CampaignSystem;namespace AnimusForge.Refactor.Adapters {internal static class PersonaIdentityPromptCaptureAdapter {internal static int GetCurrentMemoryGameHourForExternal()=>11;internal static string BuildPlayerPublicDisplayNameForPrompt(Hero h)=>"Player";'+dec(persona,'internal static string BuildPlayerAddressedInputForName(')+'}}namespace AnimusForge {internal static class SceneConversationHistoryOwner {internal static long NextEventSequence()=>++ReentryReplay.Sequence;}internal static class DialogueHistoryLedger {internal const string SceneShoutPrefix="[场景喊话]";'+dec(ledger,'internal static string NormalizeNpcLine(')+'}}namespace TaleWorlds.CampaignSystem {internal static class CampaignTime {internal readonly struct Time {internal double ToDays=>AnimusForge.Host.Day;public override string ToString()=>"game-day-"+AnimusForge.Host.Day;}internal static Time Now=>new();}}')
runtime=(HERE/'Harness.cs.txt').read_text(encoding='utf-8')
write('ReentryRuntime.cs',runtime.replace('__CAPTURE__',capture))

write('source-manifest.json',json.dumps(sources,indent=2))
dotnet=resolve_dotnet(ROOT);env=minimal_test_environment(dotnet,OUT)
result=subprocess.run([str(dotnet),'run','--project',str(OUT/'Proof.csproj'),'-c','Release','--',str(OUT)],cwd=OUT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
write('run.log',result.stdout+result.stderr);print('\n'.join((result.stdout+result.stderr).splitlines()[-22:]))
raise SystemExit(result.returncode)


