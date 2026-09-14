"""Run the production resumable queue scanner/planner; game/pending predicates are controlled seams.
The counters instrument actual slot reads, including null slots, never a substitute planner.
"""
from __future__ import annotations
import argparse,hashlib,importlib.util,json,os,re,subprocess,sys
from pathlib import Path
from xml.sax.saxutils import escape
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).resolve().parent
MUTATIONS=['unbounded-slices','ignore-structure','compact-rechecks-predicate','restart-on-write','omit-unavailable-cleanup','ignore-overview-exclusion','read-live-sort-keys','cleanup-collects-plan','ignore-slice-time','fresh-slice-budget']
MODELS=['MemorySummaryJob','MajorActionSummaryJob','MemoryOverviewJob','MajorActionSummaryState','MemoryOverviewState']
METHODS=['private static List<MemorySummaryJob> NormalizeMemorySummaryQueue(','private static List<MajorActionSummaryJob> NormalizeMajorActionSummaryQueue(','private static string NormalizeMemoryHeroId(','private static bool IsNonHeroMemoryId(','private static List<MemorySummaryJob> SanitizeMemorySummaryQueue(','private static List<MajorActionSummaryJob> SanitizeMajorActionSummaryQueue(','private static List<MemoryOverviewJob> SanitizeMemoryOverviewQueue(','private bool CancelUnavailableHeroCompressionWorkById(','private static bool IsDailyMaintenanceBudgetExceeded(']
def main():
 ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--mutate',choices=MUTATIONS);a=ap.parse_args();sys.stdout.reconfigure(encoding='utf-8')
 spec=importlib.util.spec_from_file_location('plan_ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
 source=(ROOT/'MyBehavior.cs').read_text(encoding='utf-8-sig');manifest=[];blocks=[]
 for sig in ['private sealed class '+x for x in MODELS]+METHODS:
  body=ex.declaration(source,sig);manifest.append(dict(file='MyBehavior.cs',signature=sig,line=source[:source.index(body)].count('\n')+1,sha256=hashlib.sha256(body.encode()).hexdigest()));blocks.append(body)
 for name in ['NonHeroMemoryIdPrefix','DailyMaintenanceMaxJobsPerTick']:
  m=re.search(r'private const (?:string|int) '+name+r' = [^;]+;',source);assert m,name;blocks.append(m.group())
 inp=(ROOT/'MyBehavior.MemorySummaryInput.cs').read_text(encoding='utf-8-sig');body=ex.declaration(inp,'private static string ComputeMemorySummaryFingerprint(');blocks.append(body)
 manifest.append(dict(file='MyBehavior.MemorySummaryInput.cs',signature='private static string ComputeMemorySummaryFingerprint(',sha256=hashlib.sha256(body.encode()).hexdigest()))
 planning=(ROOT/'MyBehavior.MemorySummaryPlanning.cs').read_text(encoding='utf-8-sig');production_planning=planning
 def change(old,new,count=1):
  nonlocal planning
  assert planning.count(old)==count,(old,planning.count(old));planning=planning.replace(old,new)
 change('var entry = new MemorySummaryPlanEntry { Job = job, Ordinal = ordinal };','PlanningProbe.Described++; var entry = new MemorySummaryPlanEntry { Job = job, Ordinal = ordinal };')
 change('T job = source[index];','T job = source[index]; PlanningProbe.Visit("filter");')
 change('T item = source[cursor++];','T item = source[cursor++]; PlanningProbe.Visit("compact");')
 change('long started = Stopwatch.GetTimestamp();','PlanningProbe.BeginSlice(); long started = Stopwatch.GetTimestamp();',2)
 change('// Pure frozen metadata only. Never inspect entry.Job on this worker.','PlanningProbe.WaitSortGate(); // Pure frozen metadata only. Never inspect entry.Job on this worker.')
 if a.mutate=='unbounded-slices':change('visited < DailyMaintenanceMaxJobsPerTick','visited < int.MaxValue',2)
 elif a.mutate=='ignore-structure':change('if (!IsMemorySummaryQueueStructureCurrent(source, getQueue(), ref probe))','if (false)',2)
 elif a.mutate=='compact-rechecks-predicate':change('if (item != null) compact.Add(item);','if (item != null && isPending(item)) compact.Add(item);')
 elif a.mutate=='restart-on-write':change('{ deferred = true; return true; }','{ source = getQueue(); limit = source?.Count ?? 0; cursor = 0; probe = source.GetEnumerator(); return true; }',2)
 elif a.mutate=='omit-unavailable-cleanup':change('CancelUnavailableHeroCompressionWorkById(id, "queue_execute");','PlanningProbe.MissingCleanup++;')
 elif a.mutate=='ignore-overview-exclusion':change('!(excludedOverviewIds?.Contains(x.HeroId) ?? false)','true')
 elif a.mutate=='fresh-slice-budget':change('GetDailyMaintenanceFrameBudgetMs()\n                    - MemorySummaryDispatchElapsedTicks * 1000.0 / Stopwatch.Frequency','GetDailyMaintenanceFrameBudgetMs()',2)
 elif a.mutate=='ignore-slice-time':change('(visited == 0 || !IsDailyMaintenanceBudgetExceeded(started, budget))','true',2)
 elif a.mutate=='cleanup-collects-plan':change('unavailableOwners, !cleanupOnly);','unavailableOwners, true);',3)
 elif a.mutate=='read-live-sort-keys':change('.ThenBy(x => x.Name, StringComparer.Create(culture, false))','.ThenBy(x => (x.Job is MemorySummaryJob daily ? daily.HeroName : x.Job is MajorActionSummaryJob major ? major.HeroName : ((MemoryOverviewJob)x.Job).HeroName), StringComparer.Create(culture, false))')
 prefix='using System; using TaleWorlds.Library; using System.IO; using System.Text; using System.Security.Cryptography; using System.Diagnostics; using System.Collections.Generic; using System.Linq; using Newtonsoft.Json; namespace AnimusForge {public partial class MyBehavior {'
 out=HERE/'.generated/planning'/(a.mutate or 'current');out.mkdir(parents=True,exist_ok=True)
 deps=ROOT/'.tmp/nuget-packages/newtonsoft.json/13.0.3/lib/net6.0/Newtonsoft.Json.dll';assert deps.is_file()
 files={'Product.cs':prefix+'\n'+'\n'.join(blocks)+'\n}}','Planning.cs':planning,'Boundary.cs':(ROOT/'MyBehavior.MemorySummaryMainThread.cs').read_text(encoding='utf-8-sig'),'Guard.cs':(ROOT/'SaveRuntimeGuard.cs').read_text(encoding='utf-8-sig'),'Program.cs':(HERE/'PlanningHarness.cs.txt').read_text(encoding='utf-8-sig'),'Proof.csproj':'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649;CS0162</NoWarn></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>'+escape(str(deps))+'</HintPath></Reference></ItemGroup></Project>','NuGet.Config':'<configuration><packageSources><clear/></packageSources></configuration>'}
 if 'MemorySummaryDispatcher' in files.get('Boundary.cs', ''):
     for relative in ['Refactor/Contracts/IMemorySummaryDispatchHost.cs','Refactor/Runtime/MemorySummaryDispatcher.cs']:
         files[Path(relative).name]=(ROOT/relative).read_text(encoding='utf-8-sig')
 for name,text in files.items():(out/name).write_bytes(text.encode())
 meta=dict(mutation=a.mutate,declarations=manifest,planning_sha256=hashlib.sha256(production_planning.encode()).hexdigest(),generated_sha256={k:hashlib.sha256(v.encode()).hexdigest() for k,v in files.items()},seams=['Actual filter/compact slot read and slice-entry counters','Pending/game eligibility and Campaign boundary are fixtures','Actual invalid-owner cancellation modifies queue/state/candidate collections'],limits=['Source record work inside one pending predicate is not bounded by queue-slot budget','No live-game frame time/provider/save proof','Does not substitute for dispatcher expected-job-fingerprint integration tests'])
 (out/'manifest.json').write_bytes(json.dumps(meta,ensure_ascii=False,indent=2).encode())
 dotnet=Path(os.environ.get('DOTNET_EXE',str(ROOT.parent/'.dotnet-sdk/dotnet.exe')));env=dict(os.environ,DOTNET_ROOT=str(dotnet.parent),DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(ROOT/'.tmp/nuget-packages'),APPDATA=str(ROOT/'.tmp/appdata'),DOTNET_GENERATE_ASPNET_CERTIFICATE='false',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
 build=subprocess.run([str(dotnet),'build',str(out/'Proof.csproj'),'-c','Release','--nologo','-p:RestoreConfigFile='+str(out/'NuGet.Config')],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120);(out/'build.log').write_bytes((build.stdout+build.stderr).encode())
 if build.returncode:print(build.stdout+build.stderr);return 2
 run=subprocess.run([str(dotnet),str(out/'bin/Release/net8.0/Proof.dll')],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120);log=run.stdout+run.stderr;(out/'run.log').write_bytes(log.encode());print('BUILD_PASS planning='+str(a.mutate or 'current'));print(log,end='');return run.returncode if 'PLANNING_RESULT' in log else 2
if __name__=='__main__':
 try:result=main()
 except Exception as exc:print('PLANNING_TOOL_ERROR '+type(exc).__name__+': '+str(exc));result=2
 raise SystemExit(result)
