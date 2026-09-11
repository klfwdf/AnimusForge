import argparse,importlib.util,subprocess,os
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent
p=argparse.ArgumentParser();p.add_argument('--original',action='store_true');p.add_argument('--mutate');p.add_argument('--native',action='store_true');a=p.parse_args()
spec=importlib.util.spec_from_file_location('ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
def read(n):return subprocess.check_output(['git','show','659bb998:'+n],cwd=ROOT).decode('utf-8-sig') if a.original else (ROOT/n).read_text(encoding='utf-8-sig')
s=read('MyBehavior.cs');models=['private sealed class DailyMemoryLine','private sealed class DailyMemoryDraft','private sealed class CompressedMemoryBlock','private sealed class WeeklyMemoryMaterialTrigger','private sealed class MemoryRecallCandidate']
methods=['private static string NormalizeMemoryHeroId(','private static string GetMemoryHeroId(','private static bool IsNonHeroMemoryId(','private static string FormatMemoryHourRange(','private static string FormatCompressedMemoryAgeSuffix(','private static string FormatPastAfefLineForPrompt(','private static string StripMemoryTitleDateTime(','private static string BuildMemoryRecallQueryText(','private static void AssignMemoryCandidateDisplayIds(','private bool TryBuildMemoryRecallCandidates(','private bool TrySelectMemoryIdsWithPreprocess(','private string BuildCompressedMemoryContextById(','private string BuildHistoryContextById(']
code=(HERE/'MemoryHarness.cs.txt').read_text(encoding='utf-8-sig').replace('@@MODELS@@','\n'.join(ex.declaration(s,x) for x in models)).replace('@@METHODS@@','private const string NonHeroMemoryIdPrefix="af_nonhero:";\n'+'\n'.join(ex.declaration(s,x) for x in methods))
prior=subprocess.check_output(['git','show','659bb998:MyBehavior.cs'],cwd=ROOT).decode('utf-8-sig')
baseline_names=['FormatCompressedMemoryAgeSuffix','BuildMemoryRecallQueryText','TryBuildMemoryRecallCandidates','TrySelectMemoryIdsWithPreprocess','BuildCompressedMemoryContextById','BuildHistoryContextById']
baseline='\n'.join(ex.declaration(prior,next(x for x in methods if name+'(' in x)) for name in baseline_names)
import re
for name in baseline_names:baseline=re.sub(r'\b'+name+r'\b','Baseline'+name,baseline)
code=code.replace('@@BASELINE_METHODS@@',baseline)
if a.native:
 shout=read('ShoutBehavior.cs');turn=ex.declaration(shout,'private async Task<string> SubmitNativeConversationTextInternalAsync(')
 start=turn.index('\t\tTask<string> persistedHeroHistoryTask = Task.Run(') if a.original else turn.index('\t\tFunc<string> nativeHistoryWork = await')
 end=turn.index('\t\tStopwatch nativePreprocessSw =',start)
 join=turn.index('\t\tstring persistedHeroHistory = ((await persistedHeroHistoryTask)')
 join_end=turn.index('\t\tnativeHistoryJoinSw.Stop();',join)
 fragment=(HERE/'NativeHarness.cs.txt').read_text(encoding='utf-8-sig').replace('@@START@@',turn[start:end]).replace('@@JOIN@@',turn[join:join_end])
 capture_sig='private static string BuildNativeConversationPersistedHistoryContextForPrompt(' if a.original else 'private static Func<string> CaptureNativeConversationPersistedHistoryWork('
 fragment=fragment.replace('@@CAPTURE@@',ex.declaration(shout,capture_sig)).replace('@@RUN@@',ex.declaration(shout,'private Task<T> RunNativeConversationMainThreadFuncAsync<T>(')).replace('@@WAIT@@',ex.declaration(shout,'private static async Task<T> AwaitNativeConversationMainThreadFuncAsync<T>('))
 code=code[:code.index('    public static class Program')]+fragment
out=HERE/'.generated'/((('native-' if a.native else '')+('original' if a.original else a.mutate or 'current')));out.mkdir(parents=True,exist_ok=True)
if not a.original:
 snap=read('MyBehavior.HistoryPromptSnapshot.cs')
 mutations={
  'reuse-owner-blocks':('blocks.Select(CopyHistoryRecallBlock).ToList()','blocks'),
  'reuse-afef-list':('new List<string>(block.AfefLines)','block.AfefLines'),
  'lose-summary':('Summary = block.Summary,','Summary = "",'),
  'ignore-generation':('!ReferenceEquals(Instance, owner) || !SaveRuntimeGuard.IsCurrentGeneration(generation)','!ReferenceEquals(Instance, owner) || false'),
  'ignore-owner':('!ReferenceEquals(Instance, owner) || !SaveRuntimeGuard.IsCurrentGeneration(generation)','false || !SaveRuntimeGuard.IsCurrentGeneration(generation)'),
 }
 if a.mutate in mutations:
  old,new=mutations[a.mutate];assert old in snap;snap=snap.replace(old,new,1)
 if a.mutate=='live-scene':
  old='snapshot?.Scene ?? ResolveCurrentMemorySceneLabel()';assert old in code;code=code.replace(old,'ResolveCurrentMemorySceneLabel()',1)
 if a.mutate=='live-date':
  old='FormatCompressedMemoryAgeSuffix(block, snapshot?.GameDay)';assert old in code;code=code.replace(old,'FormatCompressedMemoryAgeSuffix(block)',1)
 if a.mutate=='live-query':
  old='snapshot?.RecallQuery ?? BuildMemoryRecallQueryText';assert old in code;code=code.replace(old,'BuildMemoryRecallQueryText',1)
 if a.mutate=='drop-capture-guard':
  old='() => IsNativeConversationAdmissionCurrent(admission, out _)\n\t\t\t\t? CaptureNativeConversationPersistedHistoryWork';assert old in code;code=code.replace(old,'() => true\n\t\t\t\t? CaptureNativeConversationPersistedHistoryWork',1)
 if a.mutate=='drop-accept-guard':
  old='() => IsNativeConversationAdmissionCurrent(admission, out _), false)';assert old in code;code=code.replace(old,'() => true, false)',1)
 (out/'Snapshot.cs').write_text(snap,encoding='utf-8')
(out/'Program.cs').write_text(code,encoding='utf-8');(out/'Guard.cs').write_text(read('SaveRuntimeGuard.cs'),encoding='utf-8');(out/'Error.cs').write_text(read('PreprocessFormatException.cs'),encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion>'+('<DefineConstants>ORIGINAL</DefineConstants>' if a.original else '')+'</PropertyGroup></Project>',encoding='utf-8');(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
env=os.environ.copy();env.update(DOTNET_ROOT=r'G:\AFMOD\.dotnet-sdk',DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(ROOT/'.tmp/nuget-packages'),DOTNET_GENERATE_ASPNET_CERTIFICATE='false',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_CLI_UI_LANGUAGE='en')
r=subprocess.run([r'G:\AFMOD\.dotnet-sdk\dotnet.exe','run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180);log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)
