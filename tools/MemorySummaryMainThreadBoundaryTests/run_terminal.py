"""Execute real memory terminal writers, captured requests, and final Apply/Mark together.
Existing captured fixture supplies game/provider seams; no business writers are stubbed.
"""
from __future__ import annotations
import argparse, hashlib, importlib.util, json, os, re, subprocess, sys
from pathlib import Path
from xml.sax.saxutils import escape
ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
MUTATIONS = ['ignore-parse-source','ignore-final-source','omit-daily-save','omit-line-add','omit-pending-consume','drop-afef','reuse-recovery-source','drop-recovery-marker','drop-recent-marker','omit-recent-save','weekly-false-success','weekly-drop-provenance','swallow-completion-failure','omit-major-entry','omit-block-publish']
ADMISSION_MUTATIONS = ['old-predicate','skip-title-pass','trim-seen-id','seen-after-content','require-same-owner']

def module(name,path):
    spec=importlib.util.spec_from_file_location(name,path); value=importlib.util.module_from_spec(spec);spec.loader.exec_module(value);return value

def main():
    ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--mutate',choices=MUTATIONS);ap.add_argument('--source-baseline',choices=['e77602f9']);ap.add_argument('--admission-only',action='store_true');ap.add_argument('--admission-mutate',choices=ADMISSION_MUTATIONS);a=ap.parse_args();sys.stdout.reconfigure(encoding='utf-8')
    if a.admission_mutate and not a.admission_only:raise ValueError('Admission mutations require --admission-only')
    if a.admission_only and (a.mutate or a.source_baseline):raise ValueError('Admission observation is independent of source-check mutations and baselines')
    capture=module('terminal_capture_inventory',HERE/'run_captured.py');business=module('terminal_business_inventory',HERE/'run_business.py');ex=module('terminal_extractor',ROOT/'tools/ChannelCutoverBoundaryTests/run.py')
    manifest=[]
    def read(name):
        data=(ROOT/name).read_text(encoding='utf-8-sig');manifest.append(dict(file=name,sha256=hashlib.sha256(data.encode()).hexdigest()));return data
    source=read('MyBehavior.cs');snippets=[]
    names=list(dict.fromkeys(capture.NAMES+[re.search(r'(\w+)\($',s).group(1) for s in business.METHODS]+'''AppendDailyMemoryLineById LoadDailyMemoryDraftsById SaveDailyMemoryDraftsById IsDailyMemoryLinePublished AttachPendingWeeklyMemoryMaterialTriggers AddWeeklyMemoryMaterialTriggerToDraft PrunePendingWeeklyMemoryMaterialTriggers LoadCompressedMemoryBlocksById SaveCompressedMemoryBlocksById MarkMemoryOverviewDirty CountDailyMemoryDraftLines HasCompressedMemoryBlock LoadDialogueHistoryById SaveDialogueHistoryById TagSceneSessionHistoryLine RemoveExpiredSingleUseNpcFactLines IsSingleUseNpcFactLine IsFirstMeetingNpcFactBody IsMeaningfulDirectConversationLine IsMeaningfulConversationLine IsSystemFactLine IsLoreInjectionHistoryLine TryStripSceneSessionHistoryMarker CountDialogueHistoryLines RecordNpcMajorAction RecordNpcActionInternal CreateNpcActionEntry GetNpcActionHeroKey NormalizeNpcActionStableKey RemoveInvalidNpcActionEntries ContainsNpcActionStableKey ContainsNpcActionForDay GetNextNpcActionOrder CompareNpcActionTimeline'''.split()))
    # Admission/maintenance scans have their own real business suite, not this terminal scenario.
    names=[name for name in names if name not in {'TryStartMemorySummaryQueue','ShouldScanMemoryOverviewCandidates','TryRunCampaignMemoryMaintenance','QueueAllMemoryOverviewCandidatesForDeferredScan'}]
    if a.admission_only:names=list(dict.fromkeys(names+['TryEnqueueMemoryOverviewForMemoryId']))
    def replace(data,old,new,count=1):
        if data.count(old)!=count:raise ValueError('Terminal extraction anchor drift: '+old)
        return data.replace(old,new)
    def add(sig,name=''):
        body=ex.declaration(source,sig);manifest.append(dict(file='MyBehavior.cs',signature=sig,line=source[:source.index(body)].count('\n')+1,sha256=hashlib.sha256(body.encode()).hexdigest()))
        if a.admission_only and name=='SanitizeCompressedMemoryBlocks':
            pos=body.index('{')+1;body=body[:pos]+'\n AdmissionProbe.Sanitizers++;'+body[pos:]
            body=replace(body,'CompressedMemoryBlock block = TWParallel.IsMainThread()', 'AdmissionProbe.SanitizedRecords++;\n CompressedMemoryBlock block = TWParallel.IsMainThread()')
        if a.admission_only and name=='HasMemoryOverviewPendingBlocks':
            if a.admission_mutate=='old-predicate':body=body[:body.index('{')+1]+' return HasMemoryOverviewPendingBlocksOracle(heroId, blocks); }'
            if a.admission_mutate=='skip-title-pass':body=replace(body,'StripMemoryTitleDateTime((block.RichTitle ?? "").Trim())','(block.RichTitle ?? "").Trim()')
            if a.admission_mutate=='trim-seen-id':body=replace(body,'seen.Add(blockId)','seen.Add(blockId.Trim())')
            if a.admission_mutate=='seen-after-content':
                body=replace(body,'if (!seen.Add(blockId)) continue;','/* fault: invalid first block no longer reserves ID */')
                body=replace(body,'if (hasContent) blockIds.Add(blockId.Trim());','if (hasContent && seen.Add(blockId)) blockIds.Add(blockId.Trim());')
            if a.admission_mutate=='require-same-owner':body=replace(body,'string.IsNullOrWhiteSpace(ownerId) || block.GameDayIndex < 0','string.IsNullOrWhiteSpace(ownerId) || ownerId != heroId || block.GameDayIndex < 0')
        if name in ['RunDailySummaryQueueItemsAsync','ProcessMemorySummaryQueueAsync']:
            body=replace(body,'await Task.Delay(60000);','await FixtureDelayAsync(60000);')
        if name=='ExecuteDailySummaryQueueItemAsync':
            for method,field,queue in [('ExecuteMemorySummaryJobAsync','MemoryResult','terminalCompleted'),('ExecuteMajorActionSummaryJobAsync','MajorActionResult','terminalMajorCompleted'),('ExecuteMemoryOverviewJobAsync','MemoryOverviewResult','terminalOverviewCompleted')]:
                matches=list(re.finditer(r'result\.'+field+r' = await '+method+r'\([^;]+;',body))
                if len(matches)!=1:raise ValueError('Dispatcher receipt probe drift '+field)
                anchor=matches[0].group();body=replace(body,anchor,anchor+' '+queue+'.Enqueue(result.'+field+');')
        if name.startswith(('Apply','MarkMemorySummaryFailure','MarkMajorActionSummaryFailure','MarkMemoryOverviewFailure','TryParseMemorySummaryResponse')):
            pos=body.index('{')+1;body=body[:pos]+'\n TerminalEvent("'+name+'");'+body[pos:]
        if name=='SaveDailyMemoryDraftsById':
            pos=body.index('{')+1;body=body[:pos]+'\n BeforeTerminalSave(memoryId);'+body[pos:]
            if a.mutate=='omit-daily-save':body=body[:body.index('{')+1]+' return; }'
        if name=='SaveDialogueHistoryById':
            pos=body.index('{')+1;body=body[:pos]+'\n BeforeRecentTerminalSave(memoryId);'+body[pos:]
            if a.mutate=='omit-recent-save':body=replace(body,'_dialogueHistory[stringId] = records;','/* fault: no Recent publish */')
        if name=='ProcessMemorySummaryQueueAsync' and a.mutate=='swallow-completion-failure':body=replace(body,'RunMemorySummaryCompletionAsync(runtimeGeneration, delegate','RunMemorySummaryMainThreadAsync(runtimeGeneration, delegate',6)
        if name=='RecordNpcActionInternal' and a.mutate=='omit-major-entry':body=replace(body,'value.Add(npcActionEntry);','/* fault: lost major entry */')
        if name=='SaveCompressedMemoryBlocksById' and a.mutate=='omit-block-publish':body=replace(body,'_compressedMemoryBlocks[text] = list;','/* fault: lost block publication */')
        if name=='ProcessMemorySummaryQueueAsync' and a.mutate=='ignore-final-source':body=replace(body,' || !IsMemorySummaryInputCurrent(result.Source)','',3)
        if name=='AppendDailyMemoryLineById':
            if a.mutate=='omit-line-add':body=replace(body,'dailyMemoryDraft.Lines.Add(dailyMemoryLine);','/* fault: missing actual line */')
            if a.mutate=='drop-afef':body=replace(body,'IsAfef = isAfef,','IsAfef = false,')
        if name=='AttachPendingWeeklyMemoryMaterialTriggers' and a.mutate=='omit-pending-consume':
            anchor='_pendingWeeklyMemoryMaterialTriggers.RemoveAll((WeeklyMemoryMaterialTrigger x) => x == null || matchedKeys.Contains((x.StableKey ?? "").Trim()));';body=replace(body,anchor,'/* fault: pending trigger never consumed */')
        snippets.append(body)
    for name in capture.MODELS:add('private sealed class '+name)
    add('private class NpcActionEntry');add('private class DialogueDay');add('private sealed class NpcActionFacts')
    for name in names:
        match=re.search(r'^\s*private [^\n]*?\b'+name+r'\(',source,re.M)
        if not match:raise ValueError('Missing '+name)
        add(match.group().strip(),name)
    if a.admission_only:
        old=subprocess.run(['git','show','e77602f9:MyBehavior.cs'],cwd=ROOT,capture_output=True,text=True,encoding='utf-8',check=True).stdout
        oracle=ex.declaration(old,'private bool HasMemoryOverviewPendingBlocks(')
        manifest.append(dict(file='MyBehavior.cs',signature='private bool HasMemoryOverviewPendingBlocks(',source_revision='e77602f9',oracle_only=True,sha256=hashlib.sha256(oracle.encode()).hexdigest()))
        snippets.append(replace(oracle,'HasMemoryOverviewPendingBlocks(','HasMemoryOverviewPendingBlocksOracle('))
    recovery=read('MyBehavior.MemoryRecovery.cs')
    for name in ['IsValidMemoryCommitMarker','IsMemoryRecoveryHexDigest','BuildMemoryCommitMarkerKey']:
        match=re.search(r'private static (?:bool|string) '+name+r'\([^;]+;',recovery)
        if not match or '=>' not in match.group():raise ValueError('Missing recovery guard '+name)
        snippets.append(match.group());manifest.append(dict(file='MyBehavior.MemoryRecovery.cs',signature=name,line=recovery[:match.start()].count('\n')+1,sha256=hashlib.sha256(match.group().encode()).hexdigest()))
    for signature in ['private bool PublishDailyInteractionMemoryComponent(', 'private bool TryApplyInteractionMemoryRecoveryWork(', 'private void RegisterInteractionMemoryRecoveryRetryOrQuarantine(', 'private bool HasDailyInteractionMemoryMarker(', 'private static T CloneForMemoryRecovery<T>(', 'private InteractionMemoryRecoveryLedger EnsureInteractionMemoryRecoveryLedger(', 'private sealed class InteractionMemoryRecoveryPermanentException', 'private bool PublishRecentInteractionMemoryComponent(', 'private bool HasRecentInteractionMemoryMarker(', 'private static List<DialogueDay> TrimDialogueHistoryForMemoryRecovery(', 'private static void CopyMemoryCommitMarkers(', 'private static Dictionary<string, string> SanitizeMemoryCommitMarkers(', 'private static bool TryParseMemoryCommitMarkerKey(']:
        body=ex.declaration(recovery,signature);manifest.append(dict(file='MyBehavior.MemoryRecovery.cs',signature=signature,line=recovery[:recovery.index(body)].count('\n')+1,sha256=hashlib.sha256(body.encode()).hexdigest()))
        if 'CloneForMemoryRecovery<T>' in signature and a.mutate=='reuse-recovery-source':body=replace(body,'return JsonConvert.DeserializeObject<T>(json);','return value;')
        if 'PublishDailyInteractionMemoryComponent(' in signature and a.mutate=='drop-recovery-marker':body=replace(body,'MemoryCommitId = work.RecoveryId,','MemoryCommitId = "",')
        if 'PublishRecentInteractionMemoryComponent(' in signature and a.mutate=='drop-recent-marker':body=replace(body,'day.MemoryCommitMarkers[markerKey] = work.PayloadHash;','/* fault: missing Recent marker */')
        snippets.append(body)
    match=re.search(r'private bool HasMatchingInteractionMemoryMarker\([^;]+;',recovery)
    if not match or '=>' not in match.group():raise ValueError('Missing matching recovery marker forwarding')
    snippets.append(match.group());manifest.append(dict(file='MyBehavior.MemoryRecovery.cs',signature='HasMatchingInteractionMemoryMarker',line=recovery[:match.start()].count('\n')+1,sha256=hashlib.sha256(match.group().encode()).hexdigest()))
    weekly=read('MyBehavior.WeeklyActionOutcomeReceipts.cs')
    for signature in ['private WeeklyMemoryMaterialOutcomeOperationStatus TryPublishWeeklyActionOutcome(', 'private static bool HasExactWeeklyActionOutcomeTrigger(', 'private WeeklyMemoryMaterialOutcomeLedger EnsureWeeklyActionOutcomeLedger(', 'private void RefreshWeeklyActionOutcomeWorkFlag(', 'private void ScheduleWeeklyActionOutcomeRetry(', 'internal static WeeklyMemoryMaterialOutcomeOperationStatus PublishWeeklyActionOutcomeForExternal(']:
        body=ex.declaration(weekly,signature);manifest.append(dict(file='MyBehavior.WeeklyActionOutcomeReceipts.cs',signature=signature,line=weekly[:weekly.index(body)].count('\n')+1,sha256=hashlib.sha256(body.encode()).hexdigest()))
        if a.mutate=='weekly-false-success' and 'TryPublishWeeklyActionOutcome(' in signature:
            # Same return type; real ledger/markers must expose the false acknowledgement.
            body=body[:body.index('{')+1]+' return WeeklyMemoryMaterialOutcomeOperationStatus.Accepted; }'
        if a.mutate=='weekly-drop-provenance' and 'TryPublishWeeklyActionOutcome(' in signature:
            body=replace(body,'OutcomePayloadHash = receipt.PayloadHash,','OutcomePayloadHash = "",')
        snippets.append(body)
    match=re.search(r'private bool IsWeeklyActionOutcomeOwnerActive\(\)[^;]+;',weekly)
    if not match or '=>' not in match.group():raise ValueError('Missing weekly owner readiness guard')
    snippets.append(match.group());manifest.append(dict(file='MyBehavior.WeeklyActionOutcomeReceipts.cs',signature='IsWeeklyActionOutcomeOwnerActive',line=weekly[:match.start()].count('\n')+1,sha256=hashlib.sha256(match.group().encode()).hexdigest()))
    for data,name in [(recovery,'MaximumPersistedMemoryCommitMarkers'),(weekly,'WeeklyActionOutcomeRetryDelayTicks'),(source,'SceneHistorySessionMarkerPrefix'),(source,'MaxMajorNpcActionEntriesPerHero'),(source,'DailyMaintenanceMaxJobsPerTick')]:
        match=re.search(r'private const [^;]+\b'+name+r'\s*=[^;]+;',data)
        if not match:raise ValueError('Missing actual constant '+name)
        snippets.append(match.group())
    product='using System; using System.Diagnostics; using System.Linq; using System.Text; using System.Text.RegularExpressions; using System.Collections.Generic; using System.Threading; using System.Threading.Tasks; using Newtonsoft.Json; using Newtonsoft.Json.Linq; using AnimusForge.Refactor.Runtime; using System.Security.Cryptography; using TaleWorlds.CampaignSystem; using TaleWorlds.CampaignSystem.Settlements; using TaleWorlds.Library; namespace AnimusForge { public partial class MyBehavior {\nprivate const string NonHeroMemoryIdPrefix="af_nonhero:"; private const int RecentNpcActionWindowDays=30;\n'+'\n\n'.join(snippets)+'\n}}'
    input_code=read('MyBehavior.MemorySummaryInput.cs')
    if a.admission_only:
        body=ex.declaration(input_code,'private static T CloneMemorySummarySource<T>(');pos=body.index('{')+1
        input_code=replace(input_code,body,body[:pos]+'\n AdmissionProbe.Clones++; if(value is List<CompressedMemoryBlock> admissionBlocks) AdmissionProbe.ClonedBlocks+=admissionBlocks.Count;'+body[pos:])
    if a.source_baseline:
        if a.mutate:raise ValueError('Select either a mutation or the historical source-check implementation')
        old=subprocess.run(['git','show',a.source_baseline+':MyBehavior.MemorySummaryInput.cs'],cwd=ROOT,capture_output=True,text=True,encoding='utf-8',check=True).stdout
        manifest.append(dict(file='MyBehavior.MemorySummaryInput.cs',source_revision=a.source_baseline,sha256=hashlib.sha256(old.encode()).hexdigest(),historical_input_only=True));input_code=old
    input_code=replace(input_code,'await Task.Delay(api.RetryAfterSeconds.HasValue ? Math.Max(1000, api.RetryAfterSeconds.Value * 1000) : 1500)','await FixtureDelayAsync(api.RetryAfterSeconds.HasValue ? Math.Max(1000, api.RetryAfterSeconds.Value * 1000) : 1500)')
    if a.mutate=='ignore-parse-source':input_code=replace(input_code,'if (!IsMemorySummaryInputCurrent(input)) return false;','/* fault: old provider payload may parse */')
    fixture=read('tools/MemorySummaryMainThreadBoundaryTests/CapturedHarness.cs.txt');fixture=fixture[:fixture.index('  static void ThreeKinds() {')]+'\n}}'
    fixture=replace(fixture,'public sealed class Hero {','public sealed class Hero { public static Hero MainHero; public object CharacterObject=new(); public TaleWorlds.CampaignSystem.Settlements.Settlement CurrentSettlement;')
    fixture,count=re.subn(r'^  bool HasCompressedMemoryBlock\([^\n]+\n','',fixture,flags=re.M)
    if count!=1:raise ValueError('Shared fixture block lookup anchor drift')
    fixture,count=re.subn(r'^  static string NormalizeNpcActionStableKey\([^\n]+\n','',fixture,flags=re.M)
    if count!=1:raise ValueError('Shared fixture stable-key lookup anchor drift')
    fixture=replace(fixture,'public static class PlayerNotorietyBehavior {','public static partial class PlayerNotorietyBehavior {')
    # The shared helper uses its own isolated reset. Keep terminal instrumentation separate.
    files={'Product.cs':product,'Input.cs':input_code,'Boundary.cs':read('MyBehavior.MemorySummaryMainThread.cs'),'Guard.cs':read('SaveRuntimeGuard.cs'),'Fixture.cs':fixture,'Terminal.cs':read('tools/MemorySummaryMainThreadBoundaryTests/TerminalHarness.cs.txt')}
    if a.admission_only:
        files['Terminal.cs']=replace(files['Terminal.cs'],'  void TryEnqueueMemoryOverviewForMemoryId(string id,string name,List<CompressedMemoryBlock> blocks)=>TerminalEvent("overview-after:"+id);\n','')
    files['RecoveryLedger.cs']=read('Refactor/Runtime/InteractionMemoryRecoveryLedger.cs')
    for name in ['Refactor/Runtime/WeeklyMemoryMaterialOutcomeReceipt.cs','Refactor/Contracts/InteractionContracts.cs','Refactor/Contracts/LlmContracts.cs','Refactor/Contracts/EconomyRewardDebtContracts.cs']:
        files[Path(name).name]=read(name)
    for extra in ['MyBehavior.MemorySummaryData.cs','MyBehavior.MemorySummaryFingerprint.cs','MyBehavior.MemorySummaryPlanning.cs']:
        if (ROOT/extra).exists():files[Path(extra).name]=read(extra)
    deps=ROOT/'.tmp/nuget-packages/newtonsoft.json/13.0.3/lib/net6.0/Newtonsoft.Json.dll'
    if not deps.is_file():raise ValueError('Existing Newtonsoft dependency missing')
    files['Proof.csproj']='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>'+escape(str(deps))+'</HintPath></Reference></ItemGroup></Project>'
    if a.admission_only:files['Proof.csproj']=files['Proof.csproj'].replace('<NoWarn>','<DefineConstants>ADMISSION_PROOF</DefineConstants><NoWarn>')
    if 'ComputeMemorySummarySourceFingerprint(source)' in input_code:
        for name in ['MyBehavior.MemorySourceFingerprint.cs','Refactor/Runtime/MemorySourceFingerprintWriter.cs']:
            files[Path(name).name]=read(name)
    if 'MemorySummaryDispatcher' in files.get('Boundary.cs', ''):
        for relative in ['Refactor/Contracts/IMemorySummaryDispatchHost.cs','Refactor/Runtime/MemorySummaryDispatcher.cs']:
            files[Path(relative).name]=(ROOT/relative).read_text(encoding='utf-8-sig')
    files['Proof.csproj']=files['Proof.csproj'].replace('<OutputType>','<EnableDefaultCompileItems>false</EnableDefaultCompileItems><OutputType>',1).replace('</Project>','<ItemGroup>'+''.join('<Compile Include="'+name+'" />' for name in files if name.endswith('.cs'))+'</ItemGroup></Project>')
    files['NuGet.Config']='<configuration><packageSources><clear/></packageSources></configuration>'
    variant=('admission-'+(a.admission_mutate or 'current')) if a.admission_only else (('source-baseline-'+a.source_baseline) if a.source_baseline else (a.mutate or 'current'))
    out=HERE/'.generated/terminal'/variant;out.mkdir(parents=True,exist_ok=True)
    for name,data in files.items():(out/name).write_bytes(data.encode())
    metadata=dict(mutation=a.mutate,source_baseline=a.source_baseline,extraction=manifest,generated_sha256={n:hashlib.sha256(v.encode()).hexdigest() for n,v in files.items()},seams=['existing captured game/settings/rendering and provider TCS fixtures; its maintenance budget is not a performance acceptance claim','overview admission/whole-owner candidate scans are explicit non-executing seams; real QueueDirty only enqueues candidate IDs, never fake overview jobs','Task.Delay replaced by controlled clock','actual Save entry has explicit pre-save fault hook (normally no-op)','method-entry witness and completed-result witness only; no business branch replacement except named mutants','downstream weekly publication/overview planning and native-history/Notoriety effects are witness-only; the error notice publish seam is a thread-safe queue, not actual UI display','Major writer uses supplied detached NPC facts, real Record/Create/dedupe/order/storage, with external hero eligibility and unused player/recent branches guarded as fixtures','Recovery uses actual Daily/Recent writer/marker/ledger; retention cleanup/scheduling effects are isolated witnesses','Weekly outcome uses actual wrapper/owner guard/publish/readback/ledger; feature enablement and already-confirmed Economy candidate payload are fixtures'],limits=['Daily legacy, Recovery Daily/Recent, and Weekly outcome terminal execute; not complete external action execution or whole recovery load/retention lifecycle','No real provider/game/save validation; ordinary DialogueHistoryCommit/editor/import caller thread ownership remains outside this suite','Actual source validation, Append, Save, pending trigger attach, publication readback, Process, all Apply/Mark, parsers and sanitizers execute'])
    if a.admission_only:
        metadata['admission_only']=True
        metadata['admission_mutation']=a.admission_mutate
        metadata['seams']=['Actual TryEnqueueMemoryOverviewForMemoryId, HasMemoryOverviewPendingBlocks, sanitizers/getter/settings/queue publication; controlled game identity. Only method-entry and per-source iteration/copy counters added.']
        metadata['limits']=['Admission-only functional and operation-count observation; no Process/Apply/provider execution or live frame-time acceptance in this mode. Ordinary terminal mode retains its explicitly separate overview-admission seam.']
    (out/'manifest.json').write_bytes(json.dumps(metadata,ensure_ascii=False,indent=2).encode())
    dotnet=Path(os.environ.get('DOTNET_EXE',str(ROOT.parent/'.dotnet-sdk/dotnet.exe')))
    env=dict(os.environ,DOTNET_ROOT=str(dotnet.parent),DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(ROOT/'.tmp/nuget-packages'),APPDATA=str(ROOT/'.tmp/appdata'),DOTNET_GENERATE_ASPNET_CERTIFICATE='false',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
    build=subprocess.run([str(dotnet),'build',str(out/'Proof.csproj'),'-c','Release','--nologo','-p:RestoreConfigFile='+str(out/'NuGet.Config')],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
    (out/'build.log').write_bytes((build.stdout+build.stderr).encode())
    if build.returncode:print(build.stdout+build.stderr);return 2
    run=subprocess.run([str(dotnet),str(out/'bin/Release/net8.0/Proof.dll')]+(['--admission-only'] if a.admission_only else []),cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
    (out/'run.log').write_bytes((run.stdout+run.stderr).encode());print('BUILD_PASS terminal='+variant);print(run.stdout+run.stderr,end='')
    return run.returncode if 'TERMINAL_RESULT' in run.stdout else 2
if __name__=='__main__':
    try:code=main()
    except Exception as exc:print('TERMINAL_TOOL_ERROR '+type(exc).__name__+': '+str(exc));code=2
    raise SystemExit(code)
