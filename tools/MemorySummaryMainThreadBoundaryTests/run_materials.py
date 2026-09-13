"""Run real event-material publication/index/rebuild against controlled host boundaries.
Historical mode extracts the real 62abfdb3 implementation, not a rewritten oracle.
"""
from __future__ import annotations
import argparse, hashlib, importlib.util, json, os, re, subprocess, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).resolve().parent
SIGNATURES=['private sealed class EventSourceMaterialEntry','private void RecordEventSourceMaterial(','private static bool IsPlayerWeeklySourceMaterial(','private static string NormalizeNpcActionStableKey(','private static string BuildEventSourceMaterialIndexKey(','private void RebuildEventSourceMaterialIndex(','private static List<EventSourceMaterialEntry> SanitizeEventSourceMaterials(']
MUTATIONS=['ignore-structure','ignore-map-binding','ignore-source-binding','publish-partial','blank-last-wins','omit-append-bind','restore-fallback']

def main():
    ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--source-baseline',choices=['62abfdb3']);ap.add_argument('--mutate',choices=MUTATIONS);a=ap.parse_args();sys.stdout.reconfigure(encoding='utf-8')
    if a.source_baseline and a.mutate:raise ValueError('Use either historical real source or one current mutation')
    spec=importlib.util.spec_from_file_location('material_ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
    def read(name):return (ROOT/name).read_text(encoding='utf-8-sig')
    def digest(text):return hashlib.sha256(text.encode()).hexdigest()
    def change(text,old,new,count=1):
        if text.count(old)!=count:raise ValueError('Materials mutation anchor drift: '+old)
        return text.replace(old,new)
    source=read('MyBehavior.cs')
    if a.source_baseline:source=subprocess.run(['git','show',a.source_baseline+':MyBehavior.cs'],cwd=ROOT,capture_output=True,text=True,encoding='utf-8',check=True).stdout
    manifest=[];snippets=[]
    for signature in SIGNATURES:
        body=ex.declaration(source,signature)
        manifest.append(dict(file='MyBehavior.cs',signature=signature,line=source[:source.index(body)].count('\n')+1,sha256=digest(body),source_revision=a.source_baseline or 'working-tree'))
        if signature=='private void RebuildEventSourceMaterialIndex(':
            if a.mutate=='publish-partial':body=change(body,'foreach (EventSourceMaterialEntry item in source','_eventSourceMaterialIndex = rebuilt;\n foreach (EventSourceMaterialEntry item in source')
            if a.mutate=='blank-last-wins':body=change(body,'else if (item.Day >= 0 && !rebuilt.ContainsKey(key)) rebuilt.Add(key, item);','else if (item.Day >= 0) rebuilt[key] = item;')
        if signature=='private void RecordEventSourceMaterial(':
            if a.mutate=='omit-append-bind':body=change(body,'BindEventSourceMaterialIndex(_eventSourceMaterials, _eventSourceMaterialIndex);','/* fault: successful append leaves obsolete binding */')
            if a.mutate=='restore-fallback':body=change(body,'_eventSourceMaterialIndex.TryGetValue(indexKey, out var eventSourceMaterialEntry);','_eventSourceMaterialIndex.TryGetValue(indexKey, out var eventSourceMaterialEntry);\n if(eventSourceMaterialEntry == null) eventSourceMaterialEntry = _eventSourceMaterials.FirstOrDefault((EventSourceMaterialEntry x) => x != null && x.Day == currentGameDayIndexSafe && string.Equals((x.StableKey ?? "").Trim(), text2, StringComparison.OrdinalIgnoreCase));')
        if signature=='private void RebuildEventSourceMaterialIndex(':
            pos=body.index('{')+1;body=body[:pos]+'\n MaterialsProbe.Rebuilds++; MaterialsProbe.InRebuild=true; try {\n'+body[pos:-1]+'\n} finally { MaterialsProbe.InRebuild=false; } }'
        if signature=='private static string BuildEventSourceMaterialIndexKey(':
            pos=body.index('{')+1;body=body[:pos]+'\n MaterialsProbe.Key();'+body[pos:]
        if signature=='private void RecordEventSourceMaterial(':
            # Count the actual legacy fallback predicate visits, not list.Count estimates.
            anchor='_eventSourceMaterials.FirstOrDefault((EventSourceMaterialEntry x) => x != null'
            if body.count(anchor)>1:raise ValueError('Ambiguous material fallback instrumentation')
            body=body.replace(anchor,'_eventSourceMaterials.FirstOrDefault((EventSourceMaterialEntry x) => MaterialsProbe.FallbackVisit() && x != null')
            body=change(body,'_eventSourceMaterials.Add(newEntry);','_eventSourceMaterials.Add(newEntry); MaterialsProbe.AfterAppend();')
        snippets.append(body)
    prefix='using System; using System.Linq; using System.Collections.Generic; using TaleWorlds.CampaignSystem; namespace AnimusForge { public partial class MyBehavior {\n'
    files={'Product.cs':prefix+'\n\n'.join(snippets)+'\n}}','Fixture.cs':read('tools/MemorySummaryMainThreadBoundaryTests/MaterialsHarness.cs.txt')}
    partial=ROOT/'MyBehavior.EventSourceMaterialIndex.cs'
    if not a.source_baseline:
        if not partial.is_file():raise ValueError('Current production index partial has not landed')
        files['Index.cs']=partial.read_text(encoding='utf-8-sig');manifest.append(dict(file=partial.name,sha256=digest(files['Index.cs']),source_revision='working-tree'))
        if a.mutate=='ignore-structure':files['Index.cs']=change(files['Index.cs'],'_eventSourceMaterialStructureProbe.MoveNext();','/* fault: ignore same-count mutation */')
        if a.mutate=='ignore-map-binding':files['Index.cs']=change(files['Index.cs'],'|| !ReferenceEquals(_eventSourceMaterialIndexedMap, _eventSourceMaterialIndex)','')
        if a.mutate=='ignore-source-binding':files['Index.cs']=change(files['Index.cs'],'|| !ReferenceEquals(_eventSourceMaterialIndexedSource, _eventSourceMaterials)','')
    files['Proof.csproj']='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn></PropertyGroup></Project>'
    files['NuGet.Config']='<configuration><packageSources><clear/></packageSources></configuration>'
    variant=('source-baseline-'+a.source_baseline) if a.source_baseline else (a.mutate or 'current');out=HERE/'.generated/materials'/variant;out.mkdir(parents=True,exist_ok=True)
    for name,data in files.items():(out/name).write_bytes(data.encode())
    metadata=dict(source_baseline=a.source_baseline,mutation=a.mutate,declarations=manifest,generated_sha256={n:digest(v) for n,v in files.items()},seams=['NameRenderer, calendar, Hero identity are explicit deterministic boundaries; real classification/normalization/storage/index/sanitizer execute','Counters wrap actual Rebuild and actual BuildIndexKey calls; a controlled second-key exception tests failed rebuild publication','An explicit after-Add/before-index hook tests failed insertion without undoing the real appended source','Legacy fallback counter executes inside the real original predicate, never replaces its decision'],limits=['Memory-only production method extraction, not Bannerlord/SyncData/UI or real persistence acceptance','No event business algorithm, public notoriety side effect, or entire weekly-report lifecycle exercised','Runtime structural index work is observed; not a live frame-time or arbitrary deep-record budget claim'])
    (out/'manifest.json').write_bytes(json.dumps(metadata,ensure_ascii=False,indent=2).encode())
    dotnet=Path(os.environ.get('DOTNET_EXE',str(ROOT.parent/'.dotnet-sdk/dotnet.exe')));env=dict(os.environ,DOTNET_ROOT=str(dotnet.parent),DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(ROOT/'.tmp/nuget-packages'),APPDATA=str(ROOT/'.tmp/appdata'),DOTNET_GENERATE_ASPNET_CERTIFICATE='false',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
    build=subprocess.run([str(dotnet),'build',str(out/'Proof.csproj'),'-c','Release','--nologo','-p:RestoreConfigFile='+str(out/'NuGet.Config')],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
    (out/'build.log').write_bytes((build.stdout+build.stderr).encode())
    if build.returncode:print(build.stdout+build.stderr);return 2
    run=subprocess.run([str(dotnet),str(out/'bin/Release/net8.0/Proof.dll')],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120);log=run.stdout+run.stderr
    (out/'run.log').write_bytes(log.encode());print('BUILD_PASS materials='+variant);print(log,end='');return run.returncode if 'MATERIALS_RESULT' in log else 2

if __name__=='__main__':
    try:result=main()
    except Exception as exc:print('MATERIALS_TOOL_ERROR '+type(exc).__name__+': '+str(exc));result=2
    raise SystemExit(result)
