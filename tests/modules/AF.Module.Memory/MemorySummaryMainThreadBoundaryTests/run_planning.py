"""Run the production resumable queue scanner/planner; game/pending predicates are controlled seams.
The counters instrument actual slot reads, including null slots, never a substitute planner.
"""
from __future__ import annotations
import argparse,hashlib,importlib.util,json,os,re,subprocess,sys
import sys as _legacy_sys
from pathlib import Path as _LegacyPath
_legacy_sys.path.insert(0, str(_LegacyPath(__file__).resolve().parents[4] / "tests"))
from af2_terminal_migration_review import historical_source
AF2_FIXTURE_METADATA = {"sourceClass": "legacy-oracle-extraction", "currentOwnerReplayProjected": False}
from pathlib import Path
from xml.sax.saxutils import escape
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root, current_source_path, minimal_test_environment
MUTATIONS=['unbounded-slices','ignore-structure','compact-rechecks-predicate','restart-on-write','omit-unavailable-cleanup','ignore-overview-exclusion','read-live-sort-keys','cleanup-collects-plan','ignore-slice-time','fresh-slice-budget']
MODELS=['MemorySummaryJob','MajorActionSummaryJob','MemoryOverviewJob','MajorActionSummaryState','MemoryOverviewState']
METHODS=['private static List<MemorySummaryJob> NormalizeMemorySummaryQueue(','private static List<MajorActionSummaryJob> NormalizeMajorActionSummaryQueue(','private static string NormalizeMemoryHeroId(','private static bool IsNonHeroMemoryId(','private static List<MemorySummaryJob> SanitizeMemorySummaryQueue(','private static List<MajorActionSummaryJob> SanitizeMajorActionSummaryQueue(','private static List<MemoryOverviewJob> SanitizeMemoryOverviewQueue(','private bool CancelUnavailableHeroCompressionWorkById(','private static bool IsDailyMaintenanceBudgetExceeded(']
def main():
 ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--mutate',choices=MUTATIONS);ap.add_argument('--run-root',type=Path);a=ap.parse_args();sys.stdout.reconfigure(encoding='utf-8')
 spec=importlib.util.spec_from_file_location('plan_ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
 support=__import__('business_owner_fixture_support');support.enable_expression_declarations(ex)
 source=historical_source('MyBehavior.cs');manifest=[];blocks=[]
 for sig in METHODS:
  original_body=ex.declaration(source,sig);body=support.statement_body(original_body);manifest.append(dict(file='MyBehavior.cs',signature=sig,line=source[:source.index(original_body)].count('\n')+1,sha256=hashlib.sha256(body.encode()).hexdigest()));blocks.append(body)
 recovery=(current_source_path(ROOT, 'MyBehavior.MemoryRecovery.cs')).read_text(encoding='utf-8-sig')
 for name in ('IsValidMemoryCommitMarker','IsMemoryRecoveryHexDigest'):
  match=re.search(r'internal static bool '+name+r'\([^;]+;',recovery);assert match,name;blocks.append(match.group())
 for name in ['NonHeroMemoryIdPrefix','DailyMaintenanceMaxJobsPerTick']:
  m=re.search(r'private const (?:string|int) '+name+r' = [^;]+;',source);assert m,name;blocks.append(m.group())
 inp=(current_source_path(ROOT, 'MyBehavior.MemorySummaryInput.cs')).read_text(encoding='utf-8-sig');body=ex.declaration(inp,'private static string ComputeMemorySummaryFingerprint(');blocks.append(body)
 manifest.append(dict(file='MyBehavior.MemorySummaryInput.cs',signature='private static string ComputeMemorySummaryFingerprint(',sha256=hashlib.sha256(body.encode()).hexdigest()))
 facade=(current_source_path(ROOT, 'MyBehavior.MemorySummaryPlanning.cs')).read_text(encoding='utf-8-sig');planning=(ROOT/'src/modules/AF.Module.Memory/Summary/MemorySummaryPlanningOwner.cs').read_text(encoding='utf-8-sig');production_planning=planning
 def change(old,new,count=1):
  nonlocal planning
  assert planning.count(old)==count,(old,planning.count(old));planning=planning.replace(old,new)
 change('var entry = new MemorySummaryPlanEntry { Job = job, Ordinal = ordinal };','PlanningProbe.Described++; var entry = new MemorySummaryPlanEntry { Job = job, Ordinal = ordinal };')
 change('T job = source[index];','T job = source[index]; PlanningProbe.Visit("filter");')
 change('T item = source[cursor++];','T item = source[cursor++]; PlanningProbe.Visit("compact");')
 change('long started = Stopwatch.GetTimestamp();','PlanningProbe.BeginSlice(); long started = Stopwatch.GetTimestamp();',2)
 change('// Pure frozen metadata only. Never inspect entry.Job on this worker.','PlanningProbe.WaitSortGate(); // Pure frozen metadata only. Never inspect entry.Job on this worker.')
 if a.mutate=='unbounded-slices':change('visited < _port.MaxJobs','visited < int.MaxValue',2)
 elif a.mutate=='ignore-structure':change('if (!IsMemorySummaryQueueStructureCurrent(source, getQueue(), ref probe))','if (false)',2)
 elif a.mutate=='compact-rechecks-predicate':change('if (item != null) compact.Add(item);','if (item != null && isPending(item)) compact.Add(item);')
 elif a.mutate=='restart-on-write':change('{ deferred = true; return true; }','{ source = getQueue(); limit = source?.Count ?? 0; cursor = 0; probe = source.GetEnumerator(); return true; }',2)
 elif a.mutate=='omit-unavailable-cleanup':change('_port.CancelUnavailable(id, "queue_execute");','PlanningProbe.MissingCleanup++;')
 elif a.mutate=='ignore-overview-exclusion':change('!(excludedOverviewIds?.Contains(x.HeroId) ?? false)','true')
 elif a.mutate=='fresh-slice-budget':change('_port.GetBudgetMilliseconds()\n                    - _port.ElapsedTicks() * 1000.0 / Stopwatch.Frequency','_port.GetBudgetMilliseconds()',2)
 elif a.mutate=='ignore-slice-time':change('(visited == 0 || !MemorySummaryPlanningRules.IsBudgetExceeded(started, budget))','true',2)
 elif a.mutate=='cleanup-collects-plan':change('unavailableOwners, !cleanupOnly, run);','unavailableOwners, true, run);',3)
 elif a.mutate=='read-live-sort-keys':change('.ThenBy(x => x.Name, StringComparer.Create(culture, false))','.ThenBy(x => (x.Job is MemorySummaryJob daily ? daily.HeroName : x.Job is MajorActionSummaryJob major ? major.HeroName : ((MemoryOverviewJob)x.Job).HeroName), StringComparer.Create(culture, false))')
 prefix='using System; using TaleWorlds.Library; using System.IO; using System.Text; using System.Security.Cryptography; using System.Diagnostics; using System.Collections.Generic; using System.Linq; using Newtonsoft.Json; namespace AnimusForge {public partial class MyBehavior {'
 out=new_run_root(ROOT,'memory-b1a-planning',a.run_root)
 # Same resolution as run_terminal.py/run_business.py: tests/run_all.py passes the SDK-bundled DLL via AF_NEWTONSOFT.
 deps=Path(os.environ.get('AF_NEWTONSOFT') or os.environ.get('NEWTONSOFT_JSON_PATH') or str(ROOT/'.tmp/nuget-packages/newtonsoft.json/13.0.3/lib/net6.0/Newtonsoft.Json.dll'))
 if not deps.is_file():raise ValueError('Existing Newtonsoft DLL missing; no downloads allowed: '+str(deps))
 files={'Product.cs':prefix+'\n'+'\n'.join(blocks)+'\n}}','Planning.cs':facade,'MemorySummaryPlanningOwner.cs':planning.replace('private async Task<List<MemorySummaryPlanEntry>> ScanMemorySummaryQueueAsync<T>','internal async Task<List<MemorySummaryPlanEntry>> ScanMemorySummaryQueueAsync<T>'),'Boundary.cs':(current_source_path(ROOT, 'MyBehavior.MemorySummaryMainThread.cs')).read_text(encoding='utf-8-sig'),'Guard.cs':(ROOT/'src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs').read_text(encoding='utf-8-sig'),'Program.cs':(HERE/'PlanningHarness.cs.txt').read_text(encoding='utf-8-sig'),'Proof.csproj':'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649;CS0162</NoWarn></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>'+escape(str(deps))+'</HintPath></Reference></ItemGroup></Project>','NuGet.Config':'<configuration><packageSources><clear/></packageSources></configuration>'}
 files['MemoryPersistenceModels.cs']=(ROOT/'src/modules/AF.Module.Memory/Records/MemoryPersistenceModels.cs').read_text(encoding='utf-8-sig')
 files['NpcActionEntry.cs']=(ROOT/'src/modules/AF.Module.Memory/Records/NpcActionEntry.cs').read_text(encoding='utf-8-sig')
 if 'MemorySummaryDispatcher' in files.get('Boundary.cs', ''):
     for relative in ['src/modules/AF.Module.Memory/Summary/IMemorySummaryDispatchHost.cs','src/modules/AF.Module.Memory/Summary/MemorySummaryDispatcher.cs']:
         files[Path(relative).name]=(current_source_path(ROOT, relative)).read_text(encoding='utf-8-sig')
 run_scope_spec=importlib.util.spec_from_file_location('memory_run_fixture',ROOT/'tests/modules/AF.Module.Memory/MemorySummaryRunOwnerTests/fixture_support.py');run_scope=importlib.util.module_from_spec(run_scope_spec);run_scope_spec.loader.exec_module(run_scope)
 run_scope.include(files, original=False)
 files['Program.cs']=files['Program.cs'].replace(';List<',';\n List<').replace(';Dictionary<',';\n Dictionary<')
 files['Program.cs']=files['Program.cs'].replace('HashSet<string> _dirtyMemoryOverviewIds=new(),_pendingMemoryOverviewCandidateScanIdSet=new(),_dailyMemoryDraftSealQueued=new(),_dailyMemoryDraftSealQueuedMajor=new();','HashSet<string> _dirtyMemoryOverviewIds=new();\n HashSet<string> _pendingMemoryOverviewCandidateScanIdSet=new();\n HashSet<string> _dailyMemoryDraftSealQueued {get=>_memoryBusinessState.Sealing.Queued;set=>_memoryBusinessState.Sealing.Queued=value;}\n HashSet<string> _dailyMemoryDraftSealQueuedMajor {get=>_memoryBusinessState.Sealing.QueuedMajor;set=>_memoryBusinessState.Sealing.QueuedMajor=value;}')
 support.include(ROOT,files,manifest,ex)
 files.pop("MemorySourceFingerprintRules.cs",None);files.pop("MemorySourceFingerprintWriter.cs",None)
 # The private scanner is made test-visible in generated source only; source algorithm/counters unchanged.
 files['Program.cs']=files['Program.cs'].replace('Instance=b;Campaign.Owner=b;', 'b._memoryBusinessState.Sealing.Queued=new();b._memoryBusinessState.Sealing.QueuedMajor=new();Instance=b;Campaign.Owner=b;')
 files['Program.cs']=files['Program.cs'].replace('Scan()=>ScanMemorySummaryQueueAsync(', 'Scan()=>CreateMemorySummaryPlanningOwner().ScanMemorySummaryQueueAsync(')
 # Declare each old fixture field individually, then project the exact production containers.
 import re as rx
 for key in ['Program.cs','Product.cs']:
  for field in support.FIELDS:
   prop=rx.search(r'^\s*private [^\n]+ '+field+r' \{ get => _memoryBusinessState[^\n]+',(current_source_path(ROOT,'MyBehavior.cs')).read_text(encoding='utf-8-sig'),rx.M)
   if not prop:continue
   # Support fixture declarations sharing the same source line.
   files[key]=rx.sub(r'(?:List<[^;=]+>|Dictionary<[^;=]+>|Queue<[^;=]+>) '+field+r'=new\(\);',prop.group().strip(),files[key])
 # HashSet fixture line used a shared type; preserve sealing-owned set projections explicitly.
 old='HashSet<string> _dirtyMemoryOverviewIds=new(),_pendingMemoryOverviewCandidateScanIdSet=new(),_dailyMemoryDraftSealQueued=new(),_dailyMemoryDraftSealQueuedMajor=new();'
 fields=['_dirtyMemoryOverviewIds','_pendingMemoryOverviewCandidateScanIdSet']
 replacement=''.join(rx.search(r'^\s*private [^\n]+ '+field+r' \{ get => _memoryBusinessState[^\n]+',(current_source_path(ROOT,'MyBehavior.cs')).read_text(encoding='utf-8-sig'),rx.M).group().strip() for field in fields)
 replacement+='HashSet<string> _dailyMemoryDraftSealQueued {get=>_memoryBusinessState.Sealing.Queued;set=>_memoryBusinessState.Sealing.Queued=value;} HashSet<string> _dailyMemoryDraftSealQueuedMajor {get=>_memoryBusinessState.Sealing.QueuedMajor;set=>_memoryBusinessState.Sealing.QueuedMajor=value;}'
 files['Program.cs']=files['Program.cs'].replace(old,replacement)
 # remove projections already introduced into Program from StateProjections.
 for field in support.FIELDS:
  if (' '+field+' {') in files['Program.cs']:
   files['StateProjections.cs']=rx.sub(r'private [^\n]+ '+field+r' \{ get => _memoryBusinessState[^\n]+','',files['StateProjections.cs'])
 files['StateProjections.cs']=files['StateProjections.cs'].replace('CurrentDay = GetCurrentGameDayIndexSafe','CurrentDay = () => 10').replace('OverviewStartCount = GetMemoryOverviewStartBlockCountFromSettings','OverviewStartCount = () => 1')
 for name,text in files.items():(out/name).write_bytes(text.encode())
 meta=dict(mutation=a.mutate,declarations=manifest,planning_sha256=hashlib.sha256(production_planning.encode()).hexdigest(),generated_sha256={k:hashlib.sha256(v.encode()).hexdigest() for k,v in files.items()},seams=['Actual filter/compact slot read and slice-entry counters','Pending/game eligibility and Campaign boundary are fixtures','Actual invalid-owner cancellation modifies queue/state/candidate collections'],limits=['Source record work inside one pending predicate is not bounded by queue-slot budget','No live-game frame time/provider/save proof','Does not substitute for dispatcher expected-job-fingerprint integration tests'])
 (out/'manifest.json').write_bytes(json.dumps(meta,ensure_ascii=False,indent=2).encode())
 dotnet=Path(os.environ.get('DOTNET_EXE',str(ROOT/'local/dotnet/8.0.425/dotnet.exe')));env=minimal_test_environment(dotnet,out)
 build=subprocess.run([str(dotnet),'build',str(out/'Proof.csproj'),'-c','Release','--nologo','-p:RestoreConfigFile='+str(out/'NuGet.Config')],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120);(out/'build.log').write_bytes((build.stdout+build.stderr).encode())
 if build.returncode:print(build.stdout+build.stderr);return 2
 run=subprocess.run([str(dotnet),str(out/'bin/Release/net8.0/Proof.dll')],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120);log=run.stdout+run.stderr;(out/'run.log').write_bytes(log.encode());print('BUILD_PASS planning='+str(a.mutate or 'current'));print(log,end='');return run.returncode if 'PLANNING_RESULT' in log else 2
if __name__=='__main__':
 try:result=main()
 except Exception as exc:print('PLANNING_TOOL_ERROR '+type(exc).__name__+': '+str(exc));result=2
 raise SystemExit(result)
