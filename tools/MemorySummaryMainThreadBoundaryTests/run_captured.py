"""Execute actual captured summary queue/provider/parser flow with controlled external seams.
No network or game deployment. Return 2 for extraction/build failure, 1 for runtime assertion failure.
"""
from __future__ import annotations
import argparse, hashlib, importlib.util, json, os, re, subprocess, sys
from pathlib import Path
from xml.sax.saxutils import escape
ROOT=Path(__file__).resolve().parents[2]
HERE=Path(__file__).resolve().parent
MODELS=['DailyMemoryLine','DailyMemoryDraft','CompressedMemoryBlock','WeeklyMemoryMaterialTrigger','MemorySummaryJob','MemorySummaryExecutionResult','MemoryOverviewState','MemoryOverviewJob','MemoryOverviewExecutionResult','MajorActionSummaryState','MajorActionSummaryJob','MajorActionSummaryExecutionResult','DailySummaryQueueResult']
NAMES='''RunDailySummaryQueueItemsAsync ExecuteDailySummaryQueueItemAsync ExecuteMemorySummaryJobAsync ExecuteMajorActionSummaryJobAsync ExecuteMemoryOverviewJobAsync FindMemoryDraft HasMemorySummaryJobStillPending HasMajorActionSummaryJobStillPending HasMemoryOverviewJobStillPending HasMajorActionsNeedingSummary HasMemoryOverviewPendingBlocks GetMemoryOverviewState GetMajorActionSummaryState SanitizeMemoryOverviewState SanitizeMajorActionSummaryState GetMajorActionMaxCursor IsNpcActionAfterSummaryCursor IsMemoryBlockIncludedInOverview BuildCompressedMemoryBlockId NormalizeMemoryHeroId IsNonHeroMemoryId CountDailyMemorySummarySourceChars BuildMemorySummarySystemPrompt BuildMemorySummaryUserPrompt BuildMajorActionSummarySystemPrompt BuildMajorActionSummaryUserPrompt GetMajorActionSummaryTargetChars BuildMajorActionSummarySourceLine BuildMemoryOverviewSummarySystemPrompt BuildMemoryOverviewSummaryUserPrompt BuildMemoryOverviewBlockSourceText BuildCompressionWritingRequirementsPromptSection TryParseMemorySummaryResponse TryParseMajorActionSummaryResponse TryParseMemoryOverviewResponse TryParseBestSummaryJsonObject TryParseTaggedSummaryObject AddTaggedSummaryProperty TryExtractTaggedBlock TryParseLooseSummaryJsonObject AddLooseJsonStringProperties TryExtractLooseJsonStringProperty SkipJsonWhitespace TryReadLooseJsonStringValue BuildRequiredJsonFieldDescription BuildRequiredJsonFieldGroupDescription HasAnyNonWhiteSpaceJsonProperty IsEmptySummaryMarker GetJsonStringIgnoreCase GetJsonPropertyIgnoreCase BuildSummaryJsonParseFailureMessage StripJsonResponseEnvelope ExtractJsonObjectPayloads StripMemoryTitleDateTime FormatMemoryHourRange BuildDailyMemoryLineForPrompt ResolveMemoryLineSceneForPrompt SanitizeDailyMemoryDrafts SanitizeCompressedMemoryBlocks SanitizeNpcActionEntries SanitizeWeeklyMemoryMaterialTriggers NormalizeWeeklyMemoryMaterialTags ExtractWeeklyMemoryMaterialTags NormalizeWeeklyMemoryMaterialTagText BuildWeeklyMemoryMaterialTriggerStableKey ComputeWeeklyMemoryMaterialHash CopyFactIds AddUniqueId'''.split()
NAMES += ['SanitizeDailyMemoryDraftEntry','SanitizeDailyMemoryDraftLine','BindDailyMemoryDraftWeeklyTrigger']
NAMES += '''GetMemoryCompressionDenominatorFromSettings GetMemoryOverviewStartBlockCountFromSettings GetMemoryOverviewTargetCharsFromSettings StripBattlePlayerMarker RenderNpcActionPromptText RewriteNpcActionSecondPersonPronouns BuildNpcActionMetadataNarrativeSuffix TranslateNpcActionKindForPrompt ResolveHeroName ResolveClanName ResolveKingdomName ResolveDisplayNameBySettlementEntry'''.split()
MUTATIONS=['retain-payload','reuse-source','ignore-fingerprint','worker-parse','skip-retry-source','drop-afef','drop-overview-ids','drop-major-cursor','background-sanitize-alias','main-sanitize-detach','drop-source-receipt','ignore-wave-lifetime','drop-nested-copy','drop-scalar-copy','omit-stream-source','drop-plan-expected','skip-planned-source-check','ignore-context','rebuild-on-check','clone-on-check','stale-pre-getter-view','skip-initial-binding','skip-retarget-guard','skip-overview-threshold','raw-denominator-context','raw-scene-context','ignore-initial-retry','omit-state-presence','omit-raw-line-field','omit-raw-nested-tags','unframed-raw-strings','truncate-raw-long','repeat-raw-list-head','raw-low-code-unit-only','ignore-full-raw-buffer']
def main():
    ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--mutate',choices=MUTATIONS);ap.add_argument('--source-baseline',choices=['8bcde78b']);ap.add_argument('--observe-rebuilds',action='store_true');a=ap.parse_args()
    sys.stdout.reconfigure(encoding='utf-8')
    spec=importlib.util.spec_from_file_location('capture_ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
    def read(path):return subprocess.check_output(['git','show',a.source_baseline+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n') if a.source_baseline else (ROOT/path).read_text(encoding='utf-8-sig')
    if a.source_baseline and a.mutate:raise ValueError('Baseline and mutation are exclusive')
    source=read('MyBehavior.cs');snippets=[];manifest=[]
    def add(sig):
        body=ex.declaration(source,sig);line=source[:source.index(body)].count('\n')+1
        manifest.append(dict(signature=sig,line=line,sha256=hashlib.sha256(body.encode()).hexdigest()))
        if 'RunDailySummaryQueueItemsAsync(' in sig:
            assert body.count('await Task.Delay(60000);')==1
            body=body.replace('await Task.Delay(60000);','await FixtureDelayAsync(60000);')
        if 'ExecuteDailySummaryQueueItemAsync(' in sig:
            opening=body.index('{')+1
            body=body[:opening]+'\n System.Threading.Interlocked.Increment(ref Probe.QueueDispatches);'+body[opening:]
        if any(name+'(' in sig for name in ['BuildMemorySummarySystemPrompt','BuildMemorySummaryUserPrompt','BuildMajorActionSummarySystemPrompt','BuildMajorActionSummaryUserPrompt','BuildMemoryOverviewSummarySystemPrompt','BuildMemoryOverviewSummaryUserPrompt']):
            name=re.search(r'(\w+)\($',sig)[1];opening=body.index('{')+1
            body=body[:opening]+'\n Probe.Call("'+name+'");'+body[opening:]
        if any(name+'(' in sig for name in ['HasMemorySummaryJobStillPending','HasMajorActionSummaryJobStillPending','HasMemoryOverviewJobStillPending']):
            opening=body.index('{')+1;body=body[:opening]+'\n Probe.Call("HasPending");'+body[opening:]
        snippets.append(body)
    for name in MODELS:add('private sealed class '+name)
    add('private class NpcActionEntry')
    for name in NAMES:
        if name=='SanitizeDailyMemoryDraftEntry' and a.source_baseline and 'private static DailyMemoryDraft SanitizeDailyMemoryDraftEntry(' not in source:continue
        match=re.search(r'^\s*private [^\n]*?\b'+name+r'\(',source,re.M)
        if not match:raise ValueError('Missing declaration '+name)
        add(match.group().strip())
    planning=(ROOT/'MyBehavior.MemorySummaryPlanning.cs').read_text(encoding='utf-8-sig')
    for sig in ['private sealed class MemorySummaryPlanEntry','private MemorySummaryPlanEntry DescribeMemorySummaryJob(']:
        body=ex.declaration(planning,sig);snippets.append(body)
        manifest.append(dict(file='MyBehavior.MemorySummaryPlanning.cs',signature=sig,line=planning[:planning.index(body)].count('\n')+1,sha256=hashlib.sha256(body.encode()).hexdigest()))
    recovery=(ROOT/'MyBehavior.MemoryRecovery.cs').read_text(encoding='utf-8-sig')
    for name in ['IsValidMemoryCommitMarker','IsMemoryRecoveryHexDigest']:
        match=re.search(r'private static bool '+name+r'\([^;]+;',recovery)
        if not match or '=>' not in match.group():raise ValueError('Missing expression-bodied recovery guard '+name)
        body=match.group();snippets.append(body)
        manifest.append(dict(file='MyBehavior.MemoryRecovery.cs',signature=name,line=recovery[:match.start()].count('\n')+1,sha256=hashlib.sha256(body.encode()).hexdigest()))
    product='using System; using System.Linq; using System.Text; using System.Text.RegularExpressions; using System.Collections.Generic; using System.Threading.Tasks; using Newtonsoft.Json.Linq; using System.Security.Cryptography; using TaleWorlds.CampaignSystem; using TaleWorlds.CampaignSystem.Settlements; using TaleWorlds.Library; namespace AnimusForge { public partial class MyBehavior {\nprivate const string NonHeroMemoryIdPrefix="af_nonhero:"; private const int RecentNpcActionWindowDays=30;\n'+'\n\n'.join(snippets)+'\n}}'
    capture=read('MyBehavior.MemorySummaryInput.cs');uses_typed_source='ComputeMemorySummarySourceFingerprint(source)' in capture
    for sig,label in [('private MemorySummaryInput CaptureMemorySummaryInput(','Capture'),('private bool IsMemorySummaryInputCurrent(','Check'),('private static T CloneMemorySummarySource<T>(', 'Clone'),('private static string ComputeMemorySummaryFingerprint(', 'Fingerprint')]:
        body=ex.declaration(capture,sig);opening=body.index('{')+1
        capture=capture.replace(body,body[:opening]+'\n Probe.Call("'+label+'");'+body[opening:],1)
    old='await Task.Delay(api.RetryAfterSeconds.HasValue ? Math.Max(1000, api.RetryAfterSeconds.Value * 1000) : 1500)'
    assert capture.count(old)==1
    capture=capture.replace(old,'await FixtureDelayAsync(api.RetryAfterSeconds.HasValue ? Math.Max(1000, api.RetryAfterSeconds.Value * 1000) : 1500)')
    def mutation(text,old,new):
        assert text.count(old)==1,old
        return text.replace(old,new)
    if a.mutate=='omit-state-presence':
        pass  # Applied to the source encoder below; do not skip reading the actual state.
    elif a.mutate=='ignore-context':capture=mutation(capture,'if (!string.Equals(CaptureMemorySummaryContextFingerprint(input), input.ContextFingerprint, StringComparison.Ordinal)) return false;','/* fault: context ignored */')
    elif a.mutate=='rebuild-on-check':
        capture=mutation(capture,'return IsMemorySummaryInputCurrent(input) ? input : null;','return input;')
        old_check=ex.declaration(capture,'private bool IsMemorySummaryInputCurrent(')
        capture=mutation(capture,old_check,'private bool IsMemorySummaryInputCurrent(MemorySummaryInput input) { Probe.Call("Check"); if(input==null)return false; var current=CaptureMemorySummaryInput(input.QueueJob,input.Generation); return current!=null && string.Equals(current.SourceFingerprint,input.SourceFingerprint,StringComparison.Ordinal) && string.Equals(current.ContextFingerprint,input.ContextFingerprint,StringComparison.Ordinal); }')
    elif a.mutate=='clone-on-check':capture=mutation(capture,'var initialSource = ReadMemorySummarySource(input.QueueJob, input.Generation);','CloneMemorySummarySource(input.Job); var initialSource = ReadMemorySummarySource(input.QueueJob, input.Generation);')
    elif a.mutate=='stale-pre-getter-view':capture=mutation(capture,'var source = ReadMemorySummarySource(input.QueueJob, input.Generation);','var source = initialSource;')
    elif a.mutate=='skip-initial-binding':capture=mutation(capture,'return IsMemorySummaryInputCurrent(input) ? input : null;','return input;')
    elif a.mutate=='skip-retarget-guard':capture=mutation(capture,'!string.Equals(initialSource.HeroId, input.HeroId, StringComparison.OrdinalIgnoreCase)','false')
    elif a.mutate=='skip-overview-threshold':capture=mutation(capture,'if (input.Job is MemoryOverviewJob && input.OverviewBlockCount < GetMemoryOverviewStartBlockCountFromSettings()) return false;','/* fault: dynamic threshold ignored */')
    elif a.mutate=='raw-denominator-context':capture=mutation(capture,'Math.Max(80, input.Context.DailySourceCharCount / Math.Max(1, GetMemoryCompressionDenominatorFromSettings()))','GetMemoryCompressionDenominatorFromSettings()')
    elif a.mutate=='raw-scene-context':
        original=ex.declaration(capture,'private static object CaptureMemorySummaryDailySceneContext(')
        capture=mutation(capture,original,'private static object CaptureMemorySummaryDailySceneContext(MemorySummaryContextDependencies context) { return new {Day=GetCurrentGameDayIndexSafe(),Scene=ResolveCurrentMemorySceneLabel()}; }')
    elif a.mutate=='ignore-initial-retry':
        for kind in ['daily','major','overview']:capture=mutation(capture,'if ('+kind+'.RetryCount >= 3) return null;','/* fault: initial retry gate omitted */')
    elif a.mutate=='retain-payload':
        for field in ['Draft','Actions','Blocks','Overview','SystemPrompt','UserPrompt']:
            capture=mutation(capture,'result.Source.'+field+' = null;','/* fault: retained request payload */')
    elif a.mutate=='reuse-source':capture=mutation(capture,'return (T)copy;','return value;')
    elif a.mutate=='drop-nested-copy':product=mutation(product,'copy.Tags = Tags?.ToList();','copy.Tags = Tags;')
    elif a.mutate=='drop-scalar-copy':product=mutation(product,'var copy = (DailyMemoryLine)MemberwiseClone();','var copy = (DailyMemoryLine)MemberwiseClone(); copy.MemoryCommitOriginGameDate = "";')
    elif a.mutate=='omit-stream-source':
        capture=mutation(capture,'SourceFingerprint = ComputeMemorySummarySourceFingerprint(source)','SourceFingerprint = ComputeMemorySummaryFingerprint((object)null)')
        capture=mutation(capture,'string.Equals(ComputeMemorySummarySourceFingerprint(source),','string.Equals(ComputeMemorySummaryFingerprint((object)null),')
    elif a.mutate=='drop-plan-expected':product=mutation(product,'expectedJobFingerprint = planned.JobFingerprint;','expectedJobFingerprint = null;')
    elif a.mutate=='skip-planned-source-check':capture=mutation(capture,'if (expectedJobFingerprint != null && !string.Equals(expectedJobFingerprint,','if (false && !string.Equals(expectedJobFingerprint,')
    elif a.mutate=='ignore-fingerprint':capture=mutation(capture,'if (source == null || !string.Equals(ComputeMemorySummarySourceFingerprint(source),\n            input.SourceFingerprint, StringComparison.Ordinal)) return false;','if (source == null) return false;')
    elif a.mutate=='worker-parse':capture=mutation(capture,'\n                accepted = await RunMemorySummaryRunCaptureAsync(run, generation, delegate','\n                accepted = await Task.Run(delegate')
    elif a.mutate=='skip-retry-source':capture=mutation(capture,'attempt > 1 && !await RunMemorySummaryRunCaptureAsync','false && !await RunMemorySummaryRunCaptureAsync')
    elif a.mutate=='drop-afef':product=mutation(product,'AfefLines = afefLines,','AfefLines = new List<string>(),')
    elif a.mutate=='drop-overview-ids':product=mutation(product,'IncludedBlockIds = includedBlockIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),','IncludedBlockIds = new List<string>(),')
    elif a.mutate=='drop-major-cursor':product=mutation(product,'LastSummarizedSequence = sequence,','LastSummarizedSequence = 0,')
    elif a.mutate=='drop-source-receipt':
        assert product.count('Source = captured.Source,')==3
        product=product.replace('Source = captured.Source,','Source = null,')
    elif a.mutate=='ignore-wave-lifetime':
        product=mutation(product,'if ((run != null && !run.IsCurrent) || !ReferenceEquals(Instance, this) || SaveRuntimeGuard.IsStale(runtimeGeneration, "memory_summary_queue_wave")) return;','/* fault: ignore wave owner/generation lifetime */')
    elif a.mutate in ['background-sanitize-alias','main-sanitize-detach']:
        anchor='TWParallel.IsMainThread() ? sourceEntry : CloneMemorySummarySource(sourceEntry)'
        assert product.count(anchor)==3
        product=product.replace(anchor,'sourceEntry' if a.mutate=='background-sanitize-alias' else 'CloneMemorySummarySource(sourceEntry)')
    digest='ComputeMemorySummarySourceFingerprint(source)' if uses_typed_source else 'ComputeMemorySummaryFingerprint(source.Identity)'
    product += '\nnamespace AnimusForge { public partial class MyBehavior { private static string RawDigestUnderTest(MemorySummarySourceView source) => '+digest+'; } }'
    deps=ROOT/'.tmp/nuget-packages/newtonsoft.json/13.0.3/lib/net6.0/Newtonsoft.Json.dll'
    if not deps.is_file():raise ValueError('Existing Newtonsoft DLL missing; no downloads allowed')
    out=HERE/'.generated/captured'/(a.mutate or ('original-'+a.source_baseline if a.source_baseline else ('observe-rebuilds' if a.observe_rebuilds else 'current')));out.mkdir(parents=True,exist_ok=True)
    files={'Product.cs':product,'Input.cs':capture,'Boundary.cs':(ROOT/'MyBehavior.MemorySummaryMainThread.cs').read_text(encoding='utf-8-sig'),'Guard.cs':(ROOT/'src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs').read_text(encoding='utf-8-sig'),'Program.cs':(HERE/'CapturedHarness.cs.txt').read_text(encoding='utf-8-sig'),'Proof.csproj':'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>'+escape(str(deps))+'</HintPath></Reference></ItemGroup></Project>','NuGet.Config':'<configuration><packageSources><clear/></packageSources></configuration>'}
    if uses_typed_source:
        for name in ['MyBehavior.MemorySourceFingerprint.cs','Refactor/Runtime/MemorySourceFingerprintWriter.cs']:
            files[Path(name).name]=read(name)
            manifest.append(dict(file=name,sha256=hashlib.sha256(read(name).encode()).hexdigest(),whole_component=True))
    if 'MyBehavior.MemorySourceFingerprint.cs' in files:
        mapper=files['MyBehavior.MemorySourceFingerprint.cs'];runtime=files['MemorySourceFingerprintWriter.cs']
        body=ex.declaration(mapper,'private static string ComputeMemorySummarySourceFingerprint(')
        pos=body.index('{')+1;mapper=mutation(mapper,body,body[:pos]+'\n Probe.Call("RawFingerprint");'+body[pos:])
        if a.mutate=='omit-state-presence':
            assert mapper.count('writer.Write(source.StatePresent);')==2
            mapper=mapper.replace('writer.Write(source.StatePresent);','writer.Write(false);')
        if a.mutate=='omit-raw-line-field':mapper=mutation(mapper,'writer.Write(value.MemoryCommitOriginGameDate);','')
        if a.mutate=='omit-raw-nested-tags':mapper=mutation(mapper,'writer.WriteList(value.Tags, WriteMemorySourceString);','')
        if a.mutate=='unframed-raw-strings':runtime=mutation(runtime,'Write(value == null ? -1 : value.Length);','')
        if a.mutate=='truncate-raw-long':runtime=mutation(runtime,'Write(unchecked((int)(value >> 32)));','Write(0);')
        if a.mutate=='repeat-raw-list-head':runtime=mutation(runtime,'foreach (T value in values) write(this, value);','foreach (T value in values) write(this, values[0]);')
        if a.mutate=='raw-low-code-unit-only':
            runtime=mutation(runtime,'WriteByte((byte)(character >> 8));','')
            runtime=mutation(runtime,'_buffer[destination++] = (byte)(character >> 8);','')
        if a.mutate=='ignore-full-raw-buffer':runtime=mutation(runtime,'_hash.TransformBlock(_buffer, 0, _count, _buffer, 0);','')
        files['MyBehavior.MemorySourceFingerprint.cs']=mapper;files['MemorySourceFingerprintWriter.cs']=runtime
    if 'MemorySummaryDispatcher' in files.get('Boundary.cs', ''):
        for relative in ['Refactor/Contracts/IMemorySummaryDispatchHost.cs','Refactor/Runtime/MemorySummaryDispatcher.cs']:
            files[Path(relative).name]=(ROOT/relative).read_text(encoding='utf-8-sig')
    run_scope_spec=importlib.util.spec_from_file_location('memory_run_fixture',ROOT/'tools/MemorySummaryRunOwnerTests/fixture_support.py');run_scope=importlib.util.module_from_spec(run_scope_spec);run_scope_spec.loader.exec_module(run_scope)
    run_scope.include(files, original=bool(a.source_baseline))
    files['Proof.csproj']=files['Proof.csproj'].replace('<OutputType>','<EnableDefaultCompileItems>false</EnableDefaultCompileItems><OutputType>',1).replace('</Project>','<ItemGroup>'+''.join('<Compile Include="'+name+'" />' for name in files if name.endswith('.cs'))+'</ItemGroup></Project>')
    for name,data in files.items():(out/name).write_bytes(data.encode())
    metadata=dict(source_baseline=a.source_baseline,mutation=a.mutate,observe_rebuilds=a.observe_rebuilds,declarations=manifest,generated_sha256={n:hashlib.sha256(v.encode()).hexdigest() for n,v in files.items()},production_hash_normalization="utf8-no-bom-lf",production_sha256={n:hashlib.sha256(read(n).encode()).hexdigest() for n in ['MyBehavior.cs','MyBehavior.MemorySummaryInput.cs','MyBehavior.MemorySummaryMainThread.cs','MyBehavior.MemorySummaryPlanning.cs']},seams=['Capture/check/clone/hash and six Build entry call counters without changed business conditions','Queue dispatcher entry count probe without changed conditions','Gateway HTTP boundary scripted TCS','Task.Delay -> controlled clock','TaleWorlds/game rendering/settings lookups are instrumented fixtures','legacy action repair/suppression and public material normalization are fixtures'],limits=['No live provider/game/save or hard frame-time/record budget proof','Does not execute Apply/Mark/final queue Process (separate business suite)'])
    (out/'manifest.json').write_bytes(json.dumps(metadata,ensure_ascii=False,indent=2).encode())
    dotnet=Path(os.environ.get('DOTNET_EXE',str(ROOT.parent/'.dotnet-sdk/dotnet.exe')))
    env=dict(os.environ,DOTNET_ROOT=str(dotnet.parent),DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(ROOT/'.tmp/nuget-packages'),APPDATA=str(ROOT/'.tmp/appdata'),DOTNET_GENERATE_ASPNET_CERTIFICATE='false',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
    build=subprocess.run([str(dotnet),'build',str(out/'Proof.csproj'),'-c','Release','--nologo','-p:RestoreConfigFile='+str(out/'NuGet.Config')],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
    (out/'build.log').write_bytes((build.stdout+build.stderr).encode())
    if build.returncode:print(build.stdout+build.stderr);return 2
    run=subprocess.run([str(dotnet),str(out/'bin/Release/net8.0/Proof.dll')]+(['--observe-rebuilds'] if a.observe_rebuilds else []),cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
    (out/'run.log').write_bytes((run.stdout+run.stderr).encode());print('BUILD_PASS captured='+out.name);print(run.stdout+run.stderr,end='')
    return run.returncode if 'CAPTURED_RESULT' in run.stdout else 2
if __name__=='__main__':
    try: code=main()
    except Exception as exc:
        print('CAPTURED_TOOL_ERROR '+type(exc).__name__+': '+str(exc));code=2
    raise SystemExit(code)
