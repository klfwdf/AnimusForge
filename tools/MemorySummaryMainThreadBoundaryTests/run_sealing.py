"""Execute actual synchronous/resumable sealing and maintenance over controlled game data.
Only new sealing fixtures are written; source baseline mode preserves original implementation.
"""
from pathlib import Path
import argparse,hashlib,importlib.util,json,os,re,subprocess,sys
from xml.sax.saxutils import escape
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).resolve().parent
BASELINE='62abfdb3'
def module(name,path):
 sp=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(sp);sp.loader.exec_module(m);return m
def exact(s,old,new,count=1):
 assert s.count(old)==count,(old,s.count(old),count);return s.replace(old,new)
def main():
 ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--original',action='store_true');a=ap.parse_args();sys.stdout.reconfigure(encoding='utf-8')
 ex=module('seal_ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');cap=module('seal_capture',HERE/'run_captured.py')
 def read(path):return subprocess.check_output(['git','show',BASELINE+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n') if a.original and not path.startswith('tools/') else (ROOT/path).read_text(encoding='utf-8-sig')
 source=read('MyBehavior.cs');manifest=[];snippets=[];sealing_path=ROOT/'MyBehavior.MemorySealing.cs';new_sealing=not a.original and sealing_path.exists()
 names=list(dict.fromkeys(cap.NAMES+'''TrySealPastDailyMemoryDrafts ResetDailyMemoryDraftSealSliceState HasPastDailyMemoryDrafts TryRunCampaignMemoryMaintenance HasCompressedMemoryBlock TryEnqueueMajorActionSummaryForDraft HasDailyMemoryDraftAfefLines SanitizeMemorySummaryQueue SanitizeMajorActionSummaryQueue CancelUnavailableHeroCompressionWorkById IsDailyMaintenanceBudgetExceeded HasPendingDeferredDailyMaintenanceWork ProcessDeferredDailyMaintenance ExecuteDailyMaintenanceJob EnqueueDailyMaintenanceJob BuildDailyMaintenanceJobKey'''.split()))
 def add(sig,path='MyBehavior.cs',text=None):
  data=source if text is None else text;body=ex.declaration(data,sig);name=sig.rstrip('(').split()[-1]
  manifest.append(dict(file=path,signature=sig,line=data[:data.index(body)].count('\n')+1,sha256=hashlib.sha256(body.encode()).hexdigest()))
  if name=='RunDailySummaryQueueItemsAsync':body=exact(body,'await Task.Delay(60000);','await FixtureDelayAsync(60000);')
  if name in ['TrySealPastDailyMemoryDrafts','HasPastDailyMemoryDrafts','TryRunCampaignMemoryMaintenance','SanitizeDailyMemoryDrafts','HasMemorySummaryJobStillPending','HasMajorActionSummaryJobStillPending']:
   pos=body.index('{')+1;body=body[:pos]+'\n SealProbe.Hit("'+name+'");'+body[pos:]
  if name=='TrySealPastDailyMemoryDrafts' and '_dailyMemoryDrafts.Keys.ToList()' in body:
   body=exact(body,'_dailyMemoryDrafts.Keys.ToList()','SealProbe.Visit(_dailyMemoryDrafts.Keys,"owner-index").ToList()')
   body=exact(body,'_memorySummaryQueue.Where((MemorySummaryJob x) => x != null)','SealProbe.Visit(_memorySummaryQueue,"daily-index").Where((MemorySummaryJob x) => x != null)')
   body=exact(body,'_npcMajorActionSummaryQueue.Where((MajorActionSummaryJob x) => x != null)','SealProbe.Visit(_npcMajorActionSummaryQueue,"major-index").Where((MajorActionSummaryJob x) => x != null)')
   body=exact(body,'DailyMemoryDraft draft = list[draftIndex];','DailyMemoryDraft draft = list[draftIndex]; SealProbe.Hit("draft-visited");')
   body=exact(body,'list = SanitizeDailyMemoryDrafts(list);','SealProbe.Hit("owner-sanitize"); if(list.Count>0) SealProbe.Hit("owner-sanitize-nonempty"); list = SanitizeDailyMemoryDrafts(list);')
   body=exact(body,'_memorySummaryQueue.Where(HasMemorySummaryJobStillPending)','SealProbe.Visit(_memorySummaryQueue,"daily-final-filter").Where(HasMemorySummaryJobStillPending)')
   body=exact(body,'_npcMajorActionSummaryQueue.Where(HasMajorActionSummaryJobStillPending)','SealProbe.Visit(_npcMajorActionSummaryQueue,"major-final-filter").Where(HasMajorActionSummaryJobStillPending)')
  snippets.append(body)
 for name in cap.MODELS:add('private sealed class '+name)
 add('private class NpcActionEntry');add('private enum DailyMaintenanceTaskKind');add('private sealed class DailyMaintenanceJob')
 for name in names:
  if name=='HasPastDailyMemoryDrafts' and 'private bool HasPastDailyMemoryDrafts(' not in source:continue
  hit=re.search(r'^\s*private [^\n]*?\b'+name+r'\(',source,re.M);assert hit,name;add(hit.group().strip())
 for field in re.finditer(r'^\s*private [^\n;]+ _dailyMemoryDraftSeal\w+[^\n;]*;',source,re.M):snippets.append(field.group().strip())
 match=re.search(r'private const int DailyMaintenanceMaxJobsPerTick = [^;]+;',source);assert match;snippets.append(match.group())
 planning=read('MyBehavior.MemorySummaryPlanning.cs')
 for sig in ['private sealed class MemorySummaryPlanEntry','private MemorySummaryPlanEntry DescribeMemorySummaryJob(']:add(sig,'MyBehavior.MemorySummaryPlanning.cs',planning)
 recovery=read('MyBehavior.MemoryRecovery.cs')
 for name in ['IsValidMemoryCommitMarker','IsMemoryRecoveryHexDigest']:
  match=re.search(r'private static bool '+name+r'\([^;]+;',recovery);assert match and '=>' in match.group();snippets.append(match.group())
 product='using System; using System.Diagnostics; using System.Linq; using System.Text; using System.Text.RegularExpressions; using System.Collections.Generic; using System.Threading.Tasks; using Newtonsoft.Json.Linq; using System.Security.Cryptography; using TaleWorlds.CampaignSystem; using TaleWorlds.CampaignSystem.Settlements; using TaleWorlds.Library; namespace AnimusForge { public partial class MyBehavior { private const string NonHeroMemoryIdPrefix="af_nonhero:"; private const int RecentNpcActionWindowDays=30;\n'+'\n\n'.join(snippets)+'\n}}'
 fixture=(HERE/'CapturedHarness.cs.txt').read_text(encoding='utf-8-sig');fixture=fixture[:fixture.index('  static void ThreeKinds() {')]+'\n}}'
 fixture=exact(fixture,'private static double GetDailyMaintenanceFrameBudgetMs() => 1000.0;','private static double GetDailyMaintenanceFrameBudgetMs() => SealProbe.Budget;')
 fixture,count=re.subn(r'^  bool HasCompressedMemoryBlock\([^\n]+\n','',fixture,flags=re.M);assert count==1
 input_code=read('MyBehavior.MemorySummaryInput.cs');input_code=exact(input_code,'await Task.Delay(api.RetryAfterSeconds.HasValue ? Math.Max(1000, api.RetryAfterSeconds.Value * 1000) : 1500)','await FixtureDelayAsync(api.RetryAfterSeconds.HasValue ? Math.Max(1000, api.RetryAfterSeconds.Value * 1000) : 1500)')
 out=HERE/'.generated/sealing'/('original-'+BASELINE if a.original else 'current');out.mkdir(parents=True,exist_ok=True)
 deps=ROOT/'.tmp/nuget-packages/newtonsoft.json/13.0.3/lib/net6.0/Newtonsoft.Json.dll';assert deps.exists()
 files={'Product.cs':product,'Input.cs':input_code,'Boundary.cs':read('MyBehavior.MemorySummaryMainThread.cs'),'Guard.cs':read('SaveRuntimeGuard.cs'),'Fixture.cs':fixture,'Sealing.cs':(HERE/'SealingHarness.cs.txt').read_text(encoding='utf-8-sig'),'Proof.csproj':'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>'+escape(str(deps))+'</HintPath></Reference></ItemGroup></Project>','NuGet.Config':'<configuration><packageSources><clear/></packageSources></configuration>'}
 if new_sealing:
  seal=read('MyBehavior.MemorySealing.cs');manifest.append(dict(file='MyBehavior.MemorySealing.cs',sha256=hashlib.sha256(seal.encode()).hexdigest(),whole_partial=True))
  seal=exact(seal,'T job = current[index.Cursor++];','T job = current[index.Cursor++]; SealProbe.Hit(typeof(T)==typeof(MemorySummaryJob)?"daily-index":"major-index");')
  seal=exact(seal,'int index = Cursor++;','int index = Cursor++; SealProbe.Hit(typeof(T)==typeof(MemorySummaryJob)?"daily-final-filter":"major-final-filter");')
  seal=exact(seal,'Expensive--;','Expensive--; SealProbe.Hit("expensive-granted");')
  seal=exact(seal,'Metadata--;','Metadata--; SealProbe.Hit("metadata-granted");')
  if 'DailyMemoryDraft draft = list[draftIndex];' in seal:seal=exact(seal,'DailyMemoryDraft draft = list[draftIndex];','DailyMemoryDraft draft = list[draftIndex]; SealProbe.Hit("draft-visited");')
  if 'list = SanitizeDailyMemoryDrafts(list);' in seal:seal=exact(seal,'list = SanitizeDailyMemoryDrafts(list);','SealProbe.Hit("owner-sanitize"); if(list.Count>0) SealProbe.Hit("owner-sanitize-nonempty"); list = SanitizeDailyMemoryDrafts(list);')
  seal=exact(seal,'_dailyMemoryDraftSealOwnerKeys.Add(state.OwnerEnumerator.Current.Key);','_dailyMemoryDraftSealOwnerKeys.Add(state.OwnerEnumerator.Current.Key); SealProbe.Hit("owner-index");')
  seal=exact(seal,'foreach (var owner in state.CompletedOwners)\n        {','foreach (var owner in state.CompletedOwners)\n        { SealProbe.Hit("completed-owner-check");')
  files['MemorySealing.cs']=seal
 for path,text in files.items():(out/path).write_bytes(text.encode())
 meta=dict(source_revision=BASELINE if a.original else 'worktree',source_sha256=hashlib.sha256(source.encode()).hexdigest(),declarations=manifest,generated_sha256={p:hashlib.sha256(t.encode()).hexdigest() for p,t in files.items()},seams=['Actual Seal/Reset/HasPast/TryRun/sanitizers/pending/major enqueue/cancel execute; game owner identity and summary-start are fixtures','Entry/iteration counters only; controlled entry delay exercises actual Stopwatch budget'],limits=['One owner sanitizer and inner source scan still atomic','No real game/save/provider or overall frame-time acceptance'])
 (out/'manifest.json').write_bytes(json.dumps(meta,ensure_ascii=False,indent=2).encode())
 dotnet=ROOT.parent/'.dotnet-sdk/dotnet.exe';env=dict(os.environ,DOTNET_ROOT=str(dotnet.parent),DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(ROOT/'.tmp/nuget-packages'),APPDATA=str(ROOT/'.tmp/appdata'),DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
 build=subprocess.run([str(dotnet),'build',str(out/'Proof.csproj'),'-c','Release','--nologo','-p:RestoreConfigFile='+str(out/'NuGet.Config')],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120);(out/'build.log').write_bytes((build.stdout+build.stderr).encode())
 if build.returncode:print(build.stdout+build.stderr);return 2
 run=subprocess.run([str(dotnet),str(out/'bin/Release/net8.0/Proof.dll')],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120);log=run.stdout+run.stderr;(out/'run.log').write_bytes(log.encode());print('BUILD_PASS sealing='+('original-'+BASELINE if a.original else 'current'));print(log,end='');return run.returncode if 'SEALING_RESULT' in log else 2
if __name__=='__main__':
 try:result=main()
 except Exception as exc:print('SEALING_TOOL_ERROR '+type(exc).__name__+': '+str(exc));result=2
 raise SystemExit(result)
