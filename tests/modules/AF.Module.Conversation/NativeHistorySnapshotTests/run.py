import argparse,importlib.util,subprocess,os,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
import sys
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import current_source_path
import sys
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root, minimal_test_environment
p=argparse.ArgumentParser();p.add_argument('--original',action='store_true');p.add_argument('--mutate');p.add_argument('--native',action='store_true');p.add_argument('--run-root',type=Path);a=p.parse_args()
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
def read(n):return subprocess.check_output(['git','show','659bb998:'+n],cwd=ROOT).decode('utf-8-sig') if a.original else (current_source_path(ROOT, n)).read_text(encoding='utf-8-sig')
s=read('MyBehavior.cs')
if not a.original:
 s += '\n' + read('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemoryRecall.cs')
models=(['private sealed class DailyMemoryLine','private sealed class DailyMemoryDraft','private sealed class CompressedMemoryBlock','private sealed class WeeklyMemoryMaterialTrigger','private sealed class MemoryRecallCandidate'] if a.original else [])
methods=['private static string NormalizeMemoryHeroId(','private static string GetMemoryHeroId(','private static bool IsNonHeroMemoryId(','private static string FormatMemoryHourRange(','private static string FormatCompressedMemoryAgeSuffix(','private static string FormatPastAfefLineForPrompt(','private static string StripMemoryTitleDateTime(','private static string BuildMemoryRecallQueryText(','private static void AssignMemoryCandidateDisplayIds(','private bool TryBuildMemoryRecallCandidates(','private bool TrySelectMemoryIdsWithPreprocess(','private string BuildCompressedMemoryContextById(','private string BuildHistoryContextById(']
recovery=(ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemoryRecovery.cs').read_text(encoding='utf-8-sig') if not a.original else ''
marker_helpers=[]
for signature in (['internal static bool IsValidMemoryCommitMarker(', 'internal static bool IsMemoryRecoveryHexDigest('] if not a.original else []):
 helper=ex.declaration(recovery,signature)
 marker_helpers.append(helper[:helper.index(';')+1])
code=(HERE/'MemoryHarness.cs.txt').read_text(encoding='utf-8-sig').replace('@@MODELS@@','\n'.join(ex.declaration(s,x) for x in models)).replace('@@METHODS@@','private const string NonHeroMemoryIdPrefix="af_nonhero:";\n'+'\n'.join(ex.declaration(s,x) for x in methods)+ '\n'+'\n'.join(marker_helpers))
if not a.original:
 extras = '\n'.join(ex.declaration(s,x) for x in ('private MemoryRecallRequest CaptureMemoryRecallRequest(', 'private void PublishMemoryRecallFailure('))
 code = code.replace('@@BASELINE_METHODS@@', extras+'\n@@BASELINE_METHODS@@')
prior=subprocess.check_output(['git','show','659bb998:MyBehavior.cs'],cwd=ROOT).decode('utf-8-sig')
baseline_names=['FormatCompressedMemoryAgeSuffix','BuildMemoryRecallQueryText','TryBuildMemoryRecallCandidates','TrySelectMemoryIdsWithPreprocess','BuildCompressedMemoryContextById','BuildHistoryContextById']
baseline='\n'.join(ex.declaration(prior,next(x for x in methods if name+'(' in x)) for name in baseline_names)
import re
for name in baseline_names:baseline=re.sub(r'\b'+name+r'\b','Baseline'+name,baseline)
code=code.replace('@@BASELINE_METHODS@@',baseline)
if a.native:
 shout=read('ShoutBehavior.cs')
 import sys
 sys.path.insert(0,str(ROOT/'tests/modules/AF.Module.Conversation/NativeConversationAdmissionTests'))
 from turn_extraction import projected_source, NEW_SIGNATURE
 if NEW_SIGNATURE in shout: shout=projected_source(shout)
 turn=ex.declaration(shout,'private async Task<string> SubmitNativeConversationTextInternalAsync(')
 start=turn.index('\t\tTask<string> persistedHeroHistoryTask = Task.Run(') if a.original else turn.index('\t\tFunc<string> nativeHistoryWork = await')
 end=turn.index('\t\tStopwatch nativePreprocessSw =',start)
 join=turn.index('\t\tstring persistedHeroHistory = ((await persistedHeroHistoryTask)')
 join_end=turn.index('\t\tnativeHistoryJoinSw.Stop();',join)
 fragment=(HERE/'NativeHarness.cs.txt').read_text(encoding='utf-8-sig').replace('@@START@@',turn[start:end]).replace('@@JOIN@@',turn[join:join_end])
 capture_sig='private static string BuildNativeConversationPersistedHistoryContextForPrompt(' if a.original else 'private static Func<string> CaptureNativeConversationPersistedHistoryWork('
 fragment=fragment.replace('@@CAPTURE@@',ex.declaration(shout,capture_sig)).replace('@@RUN@@',ex.declaration(shout,'private Task<T> RunNativeConversationMainThreadFuncAsync<T>(')).replace('@@WAIT@@',ex.declaration(shout,'private static async Task<T> AwaitNativeConversationMainThreadFuncAsync<T>('))
 code=code[:code.index('    public static class Program')]+fragment
out=new_run_root(ROOT,'native-history-snapshot',a.run_root)
if not a.original:
 snaproot=read('MyBehavior.HistoryPromptSnapshot.cs')
 shared=read('src/AF.GameAdapter.Bannerlord/Prompt/SharedPromptCaptureBannerlordAdapter.cs')
 snap='using System;using System.Collections.Generic;using TaleWorlds.CampaignSystem;using AnimusForge.Refactor.Adapters; namespace AnimusForge {public partial class MyBehavior { '+ '\n'.join(ex.declaration(snaproot,x) for x in ['internal sealed class HistoryPromptSnapshot','internal static Func<string> CaptureHistoryContextWorkForHero(','internal static Func<string> CaptureHistoryContextWorkById('])+' } }'
 shared_methods='\n'.join(ex.declaration(shared,x) for x in ['internal sealed class HistoryWorkCapturePorts','internal static Func<string> CaptureHistoryContextWorkById(','internal static Func<string> CaptureHistoryContextWorkForHero(','internal static CompressedMemoryBlock CopyHistoryRecallBlock('])
 recall=read('src/AF.GameAdapter.Bannerlord/Prompt/MemoryRecallInputCaptureAdapter.cs')
 recall_methods='\n'.join(ex.declaration(recall,x) for x in ['internal sealed class CapturePorts','internal static string BuildMemoryRecallQueryText(','internal static bool TryBuildMemoryRecallCandidates(','internal static bool TrySelectMemoryIdsWithPreprocess(','internal static string BuildCompressedMemoryContextById(','internal static MemoryRecallRequest CaptureMemoryRecallRequest(','internal static void PublishMemoryRecallFailure('])
 state=read('src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.cs')
 state_methods=ex.declaration(state,'internal string BuildHistoryContextById(')
 adapters='using System;using System.Linq;using System.Collections.Generic;using TaleWorlds.CampaignSystem;using TaleWorlds.Library;using HistoryPromptSnapshot=AnimusForge.MyBehavior.HistoryPromptSnapshot; namespace AnimusForge.Refactor.Adapters {internal static class SharedPromptCaptureBannerlordAdapter {'+shared_methods+'} internal static class MemoryRecallInputCaptureAdapter {'+recall_methods+'} }'
 (out/'CaptureAdapters.cs').write_text(adapters,encoding='utf-8')
 state_code='using System;using System.Collections.Generic;using System.Diagnostics;using System.Text; namespace AnimusForge { '+ex.declaration(state,'internal sealed class MemoryHistoryContextReadCapabilities')+' internal sealed class MemoryBusinessStateOwner {internal Func<string,List<CompressedMemoryBlock>> LoadBlocks;internal Func<string,List<DailyMemoryDraft>> LoadDrafts;internal static int GetMemoryFinalInjectCountFromSettings()=>MyBehavior.ReadFinal();internal static int GetMemoryCandidateLimitFromSettings()=>MyBehavior.ReadLimit();internal static int GetMemoryPreprocessModeFromSettings()=>MyBehavior.ReadMode();private const string NonHeroMemoryIdPrefix=\"af_nonhero:\";'+ex.declaration(state,'internal static bool IsNonHeroMemoryId(')+state_methods+' } }'
 (out/'StateRead.cs').write_text(state_code,encoding='utf-8')
 mutations={
  'reuse-owner-blocks':('blocks.Select(CopyHistoryRecallBlock).ToList()','blocks'),
  'reuse-afef-list':('new List<string>(block.AfefLines)','block.AfefLines'),
  'lose-summary':('Summary = block.Summary,','Summary = "",'),
  'ignore-generation':('!owner.IsCurrentOwner() || !SaveRuntimeGuard.IsCurrentGeneration(generation)','!owner.IsCurrentOwner() || false'),
  'ignore-owner':('!owner.IsCurrentOwner() || !SaveRuntimeGuard.IsCurrentGeneration(generation)','false || !SaveRuntimeGuard.IsCurrentGeneration(generation)'),
 }
 if a.mutate in mutations:
  old,new=mutations[a.mutate];assert old in adapters;adapters=adapters.replace(old,new,1)
 if a.mutate=='live-scene':
  old='snapshot?.Scene ?? SceneLocationPromptCaptureAdapter.ResolveCurrentMemorySceneLabel()';assert old in adapters;adapters=adapters.replace(old,'SceneLocationPromptCaptureAdapter.ResolveCurrentMemorySceneLabel()',1)
 if a.mutate=='live-date':
  old='snapshot?.GameDay ?? MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe()';assert old in adapters;adapters=adapters.replace(old,'MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe()',1)
 if a.mutate=='live-query':
  old='snapshot?.RecallQuery ?? (blocks.Count';assert old in adapters;adapters=adapters.replace(old,'(blocks.Count',1)
 if a.mutate=='drop-capture-guard':
  old='() => IsNativeConversationAdmissionCurrent(admission, out _)\n\t\t\t\t? CaptureNativeConversationPersistedHistoryWork';assert old in code;code=code.replace(old,'() => true\n\t\t\t\t? CaptureNativeConversationPersistedHistoryWork',1)
 if a.mutate=='drop-accept-guard':
  old='() => IsNativeConversationAdmissionCurrent(admission, out _), false)';assert old in code;code=code.replace(old,'() => true, false)',1)
 (out/'CaptureAdapters.cs').write_text(adapters,encoding='utf-8')
 (out/'Snapshot.cs').write_text(snap,encoding='utf-8')
if a.native:(out/'PendingOperationRegistry.cs').write_text((ROOT/'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs').read_text(encoding='utf-8-sig'),encoding='utf-8')
if not a.original:
 # Keep the real recovery adapter wrappers; link their two state-free owner dependencies.
 owner=read('src/modules/AF.Module.Memory/Summary/MemoryRecoveryStateOwner.cs')
 pure=[]
 for signature in ['internal static bool IsValidMemoryCommitMarker(', 'internal static bool IsMemoryRecoveryHexDigest(']:
  start=owner.index(signature);end=owner.index(';',start)+1;pure.append(owner[start:end])
 (out/'RecoveryMarkerOwner.cs').write_text('using System.Linq; namespace AnimusForge; internal sealed class MemoryRecoveryStateOwner { '+'\n'.join(pure)+' }',encoding='utf-8')
 (out/'recovery-marker-source-sha256.txt').write_text('\n'.join(hashlib.sha256(span.encode()).hexdigest() for span in pure),encoding='utf-8')
if not a.original:
 (out/'Models.cs').write_text(read('src/modules/AF.Module.Memory/Records/MemoryPersistenceModels.cs'),encoding='utf-8')
 (out/'NpcActionEntry.cs').write_text(read('src/modules/AF.Module.Memory/Records/NpcActionEntry.cs'),encoding='utf-8')
 (out/'MemoryRecallOwner.cs').write_text(read('src/modules/AF.Module.Memory/Recall/MemoryRecallContextOwner.cs'),encoding='utf-8')
 (out/'MemoryRecallCandidate.cs').write_text(read('src/modules/AF.Module.Memory/Records/MemoryRecallCandidate.cs'),encoding='utf-8')
(out/'Program.cs').write_text(code,encoding='utf-8');(out/'Guard.cs').write_text(read('SaveRuntimeGuard.cs' if a.original else 'src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs'),encoding='utf-8');(out/'Error.cs').write_text(read('PreprocessFormatException.cs'),encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion>'+('<DefineConstants>ORIGINAL</DefineConstants>' if a.original else '')+'</PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>'+str(ROOT/'local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll')+'</HintPath></Reference></ItemGroup></Project>',encoding='utf-8');(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
dotnet=os.environ.get('DOTNET_EXE',str(ROOT/'local/dotnet/8.0.425/dotnet.exe'))
env=minimal_test_environment(Path(dotnet),out)
build=subprocess.run([dotnet,'build',str(out/'Proof.csproj'),'-c','Release','-p:UseAppHost=false','-p:RestoreConfigFile='+str(out/'NuGet.Config')],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180)
run=subprocess.run([dotnet,str(out/'bin/Release/net8.0/Proof.dll')],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180) if build.returncode==0 else None
log=build.stdout+build.stderr+((run.stdout+run.stderr) if run else '');(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(build.returncode or (run.returncode if run else 0))
