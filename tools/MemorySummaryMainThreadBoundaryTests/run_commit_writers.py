"""Run actual ordinary dialogue Commit -> Daily/Recent writes and exact readback.
Reuse only hash-verified real terminal extraction artifacts; refresh their generating
suite when the source snapshot changed. No game deployment, real save, or network.
"""
from __future__ import annotations
import argparse, hashlib, importlib.util, json, os, re, subprocess, sys
from pathlib import Path
from xml.sax.saxutils import escape
ROOT=Path(__file__).resolve().parents[2]
HERE=Path(__file__).resolve().parent
MUTATIONS=['ignore-main-thread','fake-commit-success','ignore-daily-readback','ignore-recent-readback','swap-daily-order','omit-recent-save','ignore-editor-lifetime','ignore-editor-reference','ignore-editor-fingerprint','bypass-import-scope']

def main():
    ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--mutate',choices=MUTATIONS);a=ap.parse_args();sys.stdout.reconfigure(encoding='utf-8')
    base=HERE/'.generated/terminal/current'
    def sha(text):return hashlib.sha256(text.encode('utf-8')).hexdigest()
    def current_base():
        try:
            manifest=json.loads((base/'manifest.json').read_text(encoding='utf-8'))
            if manifest.get('mutation') is not None:return None
            for entry in manifest['extraction']:
                if 'signature' not in entry and sha((ROOT/entry['file']).read_text(encoding='utf-8-sig'))!=entry['sha256']:return None
            for name,digest in manifest['generated_sha256'].items():
                if hashlib.sha256((base/name).read_bytes()).hexdigest()!=digest:return None
            result=re.search(r'^TERMINAL_RESULT scenarios=(\d+) failures=0\b',(base/'run.log').read_text(encoding='utf-8'),re.M)
            if result is None or int(result.group(1))<47:return None
            return manifest
        except (OSError,KeyError,ValueError):return None
    manifest=current_base()
    if manifest is None:
        refresh=subprocess.run([sys.executable,'-X','utf8','-B',str(HERE/'run_terminal.py')],cwd=ROOT,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180)
        print('BASE_TERMINAL_REFRESH exit='+str(refresh.returncode))
        if refresh.returncode:print(refresh.stdout+refresh.stderr);return 2
        manifest=current_base()
        if manifest is None:raise ValueError('Terminal extraction changed while refreshing; retry from one stable source snapshot')
    files={name:(base/name).read_text(encoding='utf-8-sig') for name in manifest['generated_sha256'] if name.endswith('.cs')}
    inventory=[]
    def read(name):
        data=(ROOT/name).read_text(encoding='utf-8-sig');inventory.append(dict(file=name,sha256=sha(data)));return data
    spec=importlib.util.spec_from_file_location('commit_extractor',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
    def replace(data,old,new,count=1):
        if data.count(old)!=count:raise ValueError('Commit anchor drift '+old)
        return data.replace(old,new)
    source=read('MyBehavior.cs');snippets=[]
    for name in ['AppendDialogueHistory','AppendDialogueHistoryById','IsDialogueHistoryPublished','BuildPlayerAddressedInputForName','GetMemoryHeroId','FindHeroById','OpenDevDailyMemoryLineTextEditor','ApplyDevDailyMemoryLineMutation','SaveDevDailyMemoryDraftsAfterEdit','SyncDialogueHistoryForDailyMemoryDraftEdit','CloneDevDailyMemoryLines','NormalizeDevDailyMemoryDraftForSave','FindDevDailyMemoryDraft','FindDevDailyMemoryLine','NormalizeDevCompressedMemoryMultilineInput','BuildDevDailyMemoryLineSubtitle','LoadDailyMemoryDrafts','SaveDailyMemoryDrafts','LoadDialogueHistory','SaveDialogueHistory','BuildDailyMemorySyncLineCounts','BuildDailyMemorySyncCountDelta','RemoveDialogueHistoryLinesByCounts','BuildDailyMemorySyncAddedDialogueLines','BuildDialogueHistoryLineForDailyMemorySync','NormalizeDialogueHistoryLineForDailyMemorySync','ResolveDailyMemorySyncGameDate','ImportDialogueHistoryData','ImportSingleNpcDialogueHistoryData','ApplyCompressedMemoryExportBundle','HasCompressedMemoryDataForHero','ShowDuplicateImportInquiry','TryParseHeroIdFromNpcFileName','TryParseNpcFileNameParts']:
        match=re.search(r'^\s*private [^\n]*?\b'+name+r'\(',source,re.M)
        if not match:raise ValueError('Missing actual ordinary commit method '+name)
        body=ex.declaration(source,match.group().strip());inventory.append(dict(file='MyBehavior.cs',signature=name,line=source[:source.index(body)].count('\n')+1,sha256=sha(body)))
        if name in ['ImportDialogueHistoryData','ImportSingleNpcDialogueHistoryData']:
            if a.mutate=='bypass-import-scope':body=replace(body,'IsMemorySourceEditorCurrent(importGeneration)','true',4)
            body=body.replace('Directory.Exists(', 'CommitImportFileBoundary.DirectoryExists(').replace('Directory.GetFiles(', 'CommitImportFileBoundary.GetFiles(').replace('File.Exists(', 'CommitImportFileBoundary.FileExists(')
        if name=='ApplyCompressedMemoryExportBundle':
            opening=body.index('{')+1;body=body[:opening]+'\n CommitImportProbe.Applies++;'+body[opening:]
        if name=='OpenDevDailyMemoryLineTextEditor':
            if a.mutate=='ignore-editor-reference':body=replace(body,'|| !ReferenceEquals(line, FindDevDailyMemoryLine(LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))','',2)
            if a.mutate=='ignore-editor-fingerprint':body=replace(body,'|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)','',2)
        if name=='AppendDialogueHistoryById':
            if a.mutate=='ignore-daily-readback':body=replace(body,'return dailyAccepted && IsDialogueHistoryPublished(normalizedMemoryId, list3);','return IsDialogueHistoryPublished(normalizedMemoryId, list3);')
            if a.mutate=='ignore-recent-readback':body=replace(body,'return dailyAccepted && IsDialogueHistoryPublished(normalizedMemoryId, list3);','return dailyAccepted;')
            if a.mutate=='swap-daily-order':
                first=body.index('if (!string.IsNullOrWhiteSpace(playerText))');second=body.index('if (!string.IsNullOrWhiteSpace(extraFact))',first);third=body.index('if (!string.IsNullOrWhiteSpace(aiText))',second);body=body[:first]+body[second:third]+body[first:second]+body[third:]
        snippets.append(body)
    for signature in ['private sealed class CompressedMemoryExportBundle','private static T ReadJson<T>(']:
        body=ex.declaration(source,signature);inventory.append(dict(file='MyBehavior.cs',signature=signature,line=source[:source.index(body)].count('\n')+1,sha256=sha(body)))
        if 'ReadJson<T>' in signature:body=body.replace('File.Exists(', 'CommitImportFileBoundary.FileExists(').replace('File.ReadAllText(', 'CommitImportFileBoundary.ReadAllText(')
        snippets.append(body)
    editor_windows=['OpenDevDailyMemoryLineEditor','OpenDevDailyMemoryLineTextEditor','OpenDevDailyMemoryLineSpeakerEditor','OpenDevDailyMemoryLineSceneEditor','OpenDevDailyMemoryLineHourEditor','OpenDevAddDailyMemoryLine','ConfirmDevDeleteDailyMemoryLine','ConfirmDevDeleteDailyMemoryDraft']
    for name in editor_windows:
        body=ex.declaration(source,'private void '+name+'(')
        checks={'capture_once':body.count('SaveRuntimeGuard.CaptureGeneration()')==1,'open_and_two_callbacks_guarded':body.count('IsMemorySourceEditorCurrent(editorGeneration)')==3,'two_identity_checks':body.count('!ReferenceEquals(')==2,'two_source_checks':body.count('!string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(')==2,'guard_before_first_source_read':body.index('IsMemorySourceEditorCurrent(editorGeneration)')<body.index('LoadDailyMemoryDrafts(')}
        if not all(checks.values()):raise ValueError('Editor window guard structure drift '+name+': '+str(checks))
        inventory.append(dict(file='MyBehavior.cs',signature=name,line=source[:source.index(body)].count('\n')+1,sha256=sha(body),structure_only=checks))
    import_windows=['ImportSingleNpcDialogueHistoryData','ImportDialogueHistoryData','ImportHeroNpcAllData','ImportAllData']
    for name in import_windows:
        body=ex.declaration(source,'private void '+name+'(')
        checks={'capture_once':body.count('SaveRuntimeGuard.CaptureGeneration()')==1,'entry_and_callbacks_and_catch_guarded':body.count('IsMemorySourceEditorCurrent(importGeneration)')==4,'entry_guard_before_try':body.index('IsMemorySourceEditorCurrent(importGeneration)')<body.index('try'),'overwrite_callback_first_guard':re.search(r'Action action = delegate\s*\{\s*if \(!IsMemorySourceEditorCurrent\(importGeneration\)\) return;',body) is not None,'merge_callback_first_guard':re.search(r'Action onSkipDuplicates = delegate\s*\{\s*if \(!IsMemorySourceEditorCurrent\(importGeneration\)\) return;',body) is not None,'catch_guard_before_notice':re.search(r'catch \(Exception ex\)\s*\{\s*if \(!IsMemorySourceEditorCurrent\(importGeneration\)\) return;',body) is not None}
        if not all(checks.values()):raise ValueError('Import guard structure drift '+name+': '+str(checks))
        inventory.append(dict(file='MyBehavior.cs',signature=name,line=source[:source.index(body)].count('\n')+1,sha256=sha(body),structure_only=checks))
    editor_source=read('MyBehavior.MemorySourceWrites.cs');guard=ex.declaration(editor_source,'private bool IsMemorySourceEditorCurrent(')
    inventory.append(dict(file='MyBehavior.MemorySourceWrites.cs',signature='IsMemorySourceEditorCurrent',line=editor_source[:editor_source.index(guard)].count('\n')+1,sha256=sha(guard)))
    if a.mutate=='ignore-editor-lifetime':guard='private bool IsMemorySourceEditorCurrent(long generation) { return true; }'
    files['EditorGuard.cs']='using System; using TaleWorlds.Library; using TaleWorlds.CampaignSystem; namespace AnimusForge { public partial class MyBehavior { '+guard+' }}'
    files['CommitBodies.cs']='using System; using System.IO; using System.Text; using System.Collections.Generic; using System.Linq; using Newtonsoft.Json; using TaleWorlds.Core; using TaleWorlds.CampaignSystem; using TaleWorlds.Library; namespace AnimusForge { public partial class MyBehavior {\n'+'\n\n'.join(snippets)+'\n}}'
    commit=read('MyBehavior.DialogueHistoryCommit.cs')
    if a.mutate=='ignore-main-thread':commit=replace(commit,'if (!TWParallel.IsMainThread())','if (false)')
    if a.mutate=='fake-commit-success':commit=replace(commit,'return accepted\n','return true\n')
    files['CommitEntry.cs']=commit
    files['DevTextEditorHelper.cs']=read('DevTextEditorHelper.cs')
    files['TextInputSanitizer.cs']=read('AnimusForgeTextInputSanitizer.cs')
    files['DialogueHistoryEntry.cs']=read('AnimusForgeDialogueHistoryEntry.cs')
    # Replace only the game registry fixture; FindHeroById itself now executes actual code.
    files['Fixture.cs'],count=re.subn(r'^  Hero FindHeroById\([^\n]+\n','',files['Fixture.cs'],flags=re.M)
    if count!=1:raise ValueError('Base game hero lookup fixture drift')
    files['Fixture.cs']=replace(files['Fixture.cs'],'public sealed class Hero {','public sealed class Hero { public static Hero Find(string id){AnimusForge.Probe.Live("hero-find");return AnimusForge.MyBehavior.CommitFixtureHeroes.FirstOrDefault(h=>h.Id==id);} public static Hero FindFirst(Func<Hero,bool> predicate){AnimusForge.Probe.Live("hero-find-first");return AnimusForge.MyBehavior.CommitFixtureHeroes.FirstOrDefault(predicate);}')
    files['Terminal.cs']=replace(files['Terminal.cs'],'public static void NoteConversationLineForExternal(string id)=>TerminalProbe.Event("note:"+id);','public static void NoteConversationLineForExternal(string id){TerminalProbe.Event("note:"+id);MyBehavior.CommitFixtureAfterNote(id);}')
    files['Terminal.cs']=replace(files['Terminal.cs'],'public static class InformationManager {','public static partial class InformationManager {')
    files['Terminal.cs']=replace(files['Terminal.cs'],'public static class ShoutBehavior {','public static partial class ShoutBehavior {')
    product=files['Product.cs']
    product=replace(product,'BeforeTerminalSave(memoryId);','BeforeTerminalSave(memoryId); CommitFixtureBeforeDailySave(memoryId);')
    product=replace(product,'BeforeRecentTerminalSave(memoryId);','BeforeRecentTerminalSave(memoryId); CommitFixtureBeforeRecentSave(memoryId); if (CommitWriterProbe.DropRecentSave) return;')
    if a.mutate=='omit-recent-save':product=replace(product,'_dialogueHistory[stringId] = records;','/* fault: missing Recent publication */')
    files['Product.cs']=product
    files['CommitWritersHarness.cs']=read('tools/MemorySummaryMainThreadBoundaryTests/CommitWritersHarness.cs.txt')
    deps=ROOT/'.tmp/nuget-packages/newtonsoft.json/13.0.3/lib/net6.0/Newtonsoft.Json.dll'
    files['Proof.csproj']='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn><EnableDefaultCompileItems>false</EnableDefaultCompileItems><StartupObject>AnimusForge.CommitWritersProgram</StartupObject></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+escape(name)+'" />' for name in files if name.endswith('.cs'))+'<Reference Include="Newtonsoft.Json"><HintPath>'+escape(str(deps))+'</HintPath></Reference></ItemGroup></Project>'
    files['NuGet.Config']='<configuration><packageSources><clear/></packageSources></configuration>'
    out=HERE/'.generated/commit_writers'/(a.mutate or 'current');out.mkdir(parents=True,exist_ok=True)
    for name,data in files.items():(out/name).write_bytes(data.encode('utf-8'))
    metadata=dict(mutation=a.mutate,editor_guard_windows=editor_windows,import_guard_windows=import_windows,base_terminal_manifest_sha256=hashlib.sha256((base/'manifest.json').read_bytes()).hexdigest(),base_extraction=manifest['extraction'],added_extraction=inventory,generated_sha256={n:sha(v) for n,v in files.items()},seams=['Reuse current-hash-verified terminal actual Daily/Recent storage/sanitizers and source/Process chain','Actual Commit, AppendDialogueHistory, ById order and exact publication readback execute','Game Hero registry/time/rendering and Notoriety downstream effects are fixtures','Daily/Recent Save entry can explicitly throw; Recent Save can drop publication only when fault flag is set','Notoriety external side-effect can remove hero eligibility to exercise partial Daily acceptance','Full import/duplicate choice/Apply and JSON decoding execute; virtual Directory/File and popup delivery are fixtures, with no actual filesystem import'],limits=['No game/provider/disk save validation','Ordinary commit is nontransactional and does not itself promise idempotency','One actual text-editor save closure and Daily/Recent edit delta are executed; popup delivery is a fixture and Native short-history display projection is stubbed','Other editor and aggregate import entrypoints, Single-NPC directory matching and game delivery of late callbacks remain outside this suite'])
    (out/'manifest.json').write_bytes(json.dumps(metadata,ensure_ascii=False,indent=2).encode('utf-8'))
    dotnet=Path(os.environ.get('DOTNET_EXE',str(ROOT.parent/'.dotnet-sdk/dotnet.exe')));env=dict(os.environ,DOTNET_ROOT=str(dotnet.parent),DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(ROOT/'.tmp/nuget-packages'),APPDATA=str(ROOT/'.tmp/appdata'),DOTNET_GENERATE_ASPNET_CERTIFICATE='false',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
    build=subprocess.run([str(dotnet),'build',str(out/'Proof.csproj'),'-c','Release','--nologo','-p:RestoreConfigFile='+str(out/'NuGet.Config')],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120);(out/'build.log').write_bytes((build.stdout+build.stderr).encode())
    if build.returncode:print(build.stdout+build.stderr);return 2
    run=subprocess.run([str(dotnet),str(out/'bin/Release/net8.0/Proof.dll')],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120);(out/'run.log').write_bytes((run.stdout+run.stderr).encode());print('EDITOR_GUARD_STRUCTURE windows=8 (source only; runtime cases cover text save/cancel)');print('IMPORT_GUARD_STRUCTURE windows=4 (runtime: single explicit file and memory batch; aggregate imports source only)');print('BUILD_PASS commit_writers='+str(a.mutate or 'current'));print(run.stdout+run.stderr,end='');return run.returncode if 'COMMIT_WRITERS_RESULT' in run.stdout else 2
if __name__=='__main__':
    try:code=main()
    except Exception as exc:print('COMMIT_WRITERS_TOOL_ERROR '+type(exc).__name__+': '+str(exc));code=2
    raise SystemExit(code)
