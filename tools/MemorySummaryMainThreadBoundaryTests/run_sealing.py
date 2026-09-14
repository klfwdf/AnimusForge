"""Execute actual synchronous/resumable sealing and maintenance over controlled game data.
Only new sealing fixtures are written; source baseline mode preserves original implementation.
"""
from pathlib import Path
import argparse,hashlib,importlib.util,json,os,re,subprocess,sys
from xml.sax.saxutils import escape
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).resolve().parent
BASELINE='62abfdb3'
MUTATIONS=[
 'abandon-incomplete-same-day','ignore-empty-probe','ignore-stale-queued-job','ignore-owner-binding',
 'ignore-cleanup-identity','unbounded-metadata','unbounded-expensive','ignore-deadline','renew-seal-budget','renew-deferred-deadline','omit-window-restore','drop-deferred-start','ignore-pending-generation','ignore-campaign-scope','unbudgeted-sort','unstable-sort','ordinal-sort','ignore-sort-source','ignore-sort-culture','ignore-sort-final-binding','unbudgeted-owner-normalize','ignore-owner-normalize-key','ignore-owner-normalize-empty','ignore-owner-normalize-kept-empty','skip-owner-normalize-reseal','ignore-owner-normalize-source']
def module(name,path):
 sp=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(sp);sp.loader.exec_module(m);return m
def exact(s,old,new,count=1):
 assert s.count(old)==count,(old,s.count(old),count);return s.replace(old,new)

def apply_product_mutation(product, mutation):
 if not mutation: return product
 if mutation=='ignore-campaign-scope':return exact(product,'_campaignMemoryMaintenanceCycleActive = true;','_campaignMemoryMaintenanceCycleActive = false;')
 if mutation=='renew-deferred-deadline':
  ex=module('budget_mutation_ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py')
  body=ex.declaration(product,'private void ProcessDeferredDailyMaintenance(')
  changed=exact(body,'ResolveDailyMaintenanceBudget(out long startTimestamp, out double budgetMs);','long startTimestamp = Stopwatch.GetTimestamp(); double budgetMs = GetDailyMaintenanceFrameBudgetMs();')
  return exact(product,body,changed)
 if mutation=='omit-window-restore':return exact(product,'_campaignMemoryMaintenanceBudget = previous;','/* fault: expired window retained */')
 if mutation=='drop-deferred-start':return exact(product,'_campaignMemorySummaryStartPending = true;','_campaignMemorySummaryStartPending = false;')
 if mutation=='ignore-pending-generation':return exact(product,'_campaignMemorySummaryStartGeneration != generation','false')
 if mutation=='abandon-incomplete-same-day':
  product=exact(product,'if (!sealActive && !hasQueuedWork && currentDay == _lastMemoryMaintenanceObservedGameDay) return;','if (!hasQueuedWork && currentDay == _lastMemoryMaintenanceObservedGameDay) return;')
  return exact(product,'using (PerfProbe.Scope("MyBehavior.TryRunCampaignMemoryMaintenance.TrySealPastDailyMemoryDrafts"))','_lastMemoryMaintenanceObservedGameDay = observedDay;\n\t\tusing (PerfProbe.Scope("MyBehavior.TryRunCampaignMemoryMaintenance.TrySealPastDailyMemoryDrafts"))')
 return product
def apply_seal_mutation(seal, mutation):
 if not mutation: return seal
 if mutation=='ignore-sort-source':
  ex=module('queue_sort_mutation',ROOT/'tools/ChannelCutoverBoundaryTests/run.py')
  body=ex.declaration(seal,'private sealed class DailyMemorySealQueueTail<T>')
  changed=exact(body,'!Current(current) || ', '')
  changed=exact(changed,'!Current(readCurrent()) || ', '')
  return exact(seal,body,changed)
 if mutation=='ignore-sort-final-binding':return exact(seal,'if (!BindingsCurrent(same)) return false;', '')
 if mutation=='unbudgeted-owner-normalize':return exact(seal,'if (!budget.Take(source != null)) return false;','')
 if mutation=='ignore-owner-normalize-key':return exact(seal,'if (NormalizeMemoryHeroId(entry.Source.HeroId) != entry.HeroId || entry.Source.GameDayIndex != entry.Day) return false;','')
 if mutation=='ignore-owner-normalize-empty':return exact(seal,'if (entry.EmptyWinner && lines > 0) return false;','')
 if mutation=='ignore-owner-normalize-kept-empty':return exact(seal,'if (entry.Included && lines == 0) return false;','')
 if mutation=='skip-owner-normalize-reseal':return exact(seal,'state.ActiveOwner = null;\n                        state.Normalization = null;','state.Normalization = null;')
 if mutation=='ignore-owner-normalize-source':
  ex=module('owner_normalize_mutation',ROOT/'tools/ChannelCutoverBoundaryTests/run.py')
  body=ex.declaration(seal,'private bool Current(List<DailyMemoryDraft> current)')
  return exact(seal,body,'private bool Current(List<DailyMemoryDraft> current) { return true; }')
 if mutation=='renew-seal-budget':return exact(seal,'var budget = _campaignMemoryMaintenanceBudget;','MemoryMaintenanceWorkBudget budget = null;')
 if mutation=='ignore-empty-probe':
  return exact(seal,'if (!found) { ResetDailyMemoryDraftSealSliceState(); return true; }','if (false) { ResetDailyMemoryDraftSealSliceState(); return true; }')
 if mutation=='ignore-stale-queued-job':
  return exact(seal,'if (!_dailyMemoryDraftSealQueued.Contains(key) || !DailyMemorySealHasCurrentJob(state.DailyIndex, key))','if (!_dailyMemoryDraftSealQueued.Contains(key))')
 if mutation=='ignore-owner-binding':
  return exact(seal,'internal bool Current(List<DailyMemoryDraft> source)\n        {\n            if (!ReferenceEquals(Source, source) || Count != (source?.Count ?? 0)) return false;','internal bool Current(List<DailyMemoryDraft> source)\n        {\n            return true;')
 if mutation=='ignore-cleanup-identity':
  return exact(seal,'if (!same(entry.Value, entry.Frozen)) { Restart(); return false; }','if (false) { Restart(); return false; }')
 if mutation=='unbounded-metadata':
  return exact(seal,'else { if (Metadata <= 0) return false; Metadata--; SealProbe.Hit("metadata-granted"); }','else { SealProbe.Hit("metadata-granted"); }')
 if mutation=='unbounded-expensive':
  return exact(seal,'if (expensive) { if (Expensive <= 0) return false; Expensive--; SealProbe.Hit("expensive-granted"); }','if (expensive) { SealProbe.Hit("expensive-granted"); }')
 if mutation=='ignore-deadline':
  return exact(seal,'if (IsExceeded) return false;' if 'if (IsExceeded) return false;' in seal else 'if (IsDailyMaintenanceBudgetExceeded(Start, Milliseconds)) return false;','')
 return seal
def main():
 ap=argparse.ArgumentParser(description=__doc__);g=ap.add_mutually_exclusive_group();g.add_argument('--original',action='store_true');g.add_argument('--source-baseline',choices=['73a6977c','9158132c','40b92e67']);g.add_argument('--mutate',choices=MUTATIONS);a=ap.parse_args();baseline=a.source_baseline or (BASELINE if a.original else None);sys.stdout.reconfigure(encoding='utf-8')
 ex=module('seal_ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');cap=module('seal_capture',HERE/'run_captured.py')
 def read(path):return subprocess.check_output(['git','show',baseline+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n') if baseline and not path.startswith('tools/') else (ROOT/path).read_text(encoding='utf-8-sig')
 source=read('MyBehavior.cs');manifest=[];snippets=[];sealing_path=ROOT/'MyBehavior.MemorySealing.cs';new_sealing=not a.original and sealing_path.exists()
 if a.mutate and a.mutate not in ('abandon-incomplete-same-day',) and not new_sealing: raise ValueError('Sealing mutation requires MyBehavior.MemorySealing.cs')
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
  if name=='TryRunCampaignMemoryMaintenance':
   body=exact(body,'if (!TrySealPastDailyMemoryDrafts(startTimestamp, budgetMs, requirePendingProbe: true)) return;','if (!TrySealPastDailyMemoryDrafts(startTimestamp, budgetMs, requirePendingProbe: true)) return; SealProbe.Hit("campaign-seal-return");') if 'requirePendingProbe: true' in body else body
  if name in ('SanitizeMemorySummaryQueue','SanitizeMajorActionSummaryQueue'):
   body=body.replace('OrderBy((MemorySummaryJob x) => x.GameDayIndex)', 'OrderBy((MemorySummaryJob x) => x.GameDayIndex, SealProbe.SortComparer<int>())').replace('OrderBy((MajorActionSummaryJob x) => x.TriggerGameDayIndex)', 'OrderBy((MajorActionSummaryJob x) => x.TriggerGameDayIndex, SealProbe.SortComparer<int>())')
   body=body.replace('ThenBy((MemorySummaryJob x) => x.HeroName)', 'ThenBy((MemorySummaryJob x) => x.HeroName, SealProbe.SortComparer<string>())').replace('ThenBy((MajorActionSummaryJob x) => x.HeroName)', 'ThenBy((MajorActionSummaryJob x) => x.HeroName, SealProbe.SortComparer<string>())')
  if name=='SanitizeDailyMemoryDraftEntry':
   body=exact(body,'DailyMemoryDraft draft = TWParallel.IsMainThread()', 'if (sourceEntry != null) SealProbe.Hit("owner-normalized-record");\n DailyMemoryDraft draft = TWParallel.IsMainThread()')
  if name=='SanitizeDailyMemoryDrafts' and 'DailyMemoryDraft draft = TWParallel.IsMainThread()' in body:
   body=exact(body,'DailyMemoryDraft draft = TWParallel.IsMainThread()', 'if (sourceEntry != null) SealProbe.Hit("owner-normalized-record");\n DailyMemoryDraft draft = TWParallel.IsMainThread()')
  if name in ('SanitizeDailyMemoryDraftEntry','SanitizeDailyMemoryDrafts') and 'x.GameDayIndex = draft.GameDayIndex;' in body:
   body=exact(body,'x.GameDayIndex = draft.GameDayIndex;','SealProbe.Hit("owner-normalized-line"); x.GameDayIndex = draft.GameDayIndex;')
  snippets.append(body)
 for name in cap.MODELS:add('private sealed class '+name)
 add('private class NpcActionEntry');add('private enum DailyMaintenanceTaskKind');add('private sealed class DailyMaintenanceJob')
 for helper in ['NormalizeMemorySummaryQueue','NormalizeMajorActionSummaryQueue']:
  if re.search(r'private static List<[^>]+> '+helper+r'\(',source):names.append(helper)
 for name in names:
  if name=='SanitizeDailyMemoryDraftEntry' and 'private static DailyMemoryDraft SanitizeDailyMemoryDraftEntry(' not in source:continue
  if name=='HasPastDailyMemoryDrafts' and 'private bool HasPastDailyMemoryDrafts(' not in source:continue
  hit=re.search(r'^\s*private [^\n]*?\b'+name+r'\(',source,re.M);assert hit,name;add(hit.group().strip())
 cycle_signature='private void RunCampaignMemoryMaintenanceCycle('
 if cycle_signature in source:
  owner_tick=ex.declaration(source,'private void OnCampaignTick(')
  assert owner_tick.count('RunCampaignMemoryMaintenanceCycle(processedWeeklyReportCommits);')==1,'Campaign owner must call the shared cycle once'
  assert 'TryRunCampaignMemoryMaintenance();' not in owner_tick and 'ProcessDeferredDailyMaintenance();' not in owner_tick,'Old standalone calls still bypass shared cycle'
  add(cycle_signature)
 else:
  owner_tick=ex.declaration(source,'private void OnCampaignTick(')
  begin=owner_tick.index('using (PerfProbe.Scope("MyBehavior.OnCampaignTick.TryRunCampaignMemoryMaintenance"))')
  end=owner_tick.index('using (PerfProbe.Scope("MyBehavior.OnCampaignTick.CachePlayerClanTier"))',begin)
  body=owner_tick[begin:end]
  snippets.append('private void RunCampaignMemoryMaintenanceCycle(bool processedWeeklyReportCommits) {\n'+body+'\n}')
  manifest.append(dict(file='MyBehavior.cs',signature='OnCampaignTick maintenance callsite',sha256=hashlib.sha256(body.encode()).hexdigest(),derived_wrapper=True))
 for field in re.finditer(r'^\s*private [^\n;]+ _dailyMemoryDraftSeal\w+[^\n;]*;',source,re.M):snippets.append(field.group().strip())
 match=re.search(r'private const int DailyMaintenanceMaxJobsPerTick = [^;]+;',source);assert match;snippets.append(match.group())
 planning=read('MyBehavior.MemorySummaryPlanning.cs')
 for sig in ['private sealed class MemorySummaryPlanEntry','private MemorySummaryPlanEntry DescribeMemorySummaryJob(']:add(sig,'MyBehavior.MemorySummaryPlanning.cs',planning)
 recovery=read('MyBehavior.MemoryRecovery.cs')
 for name in ['IsValidMemoryCommitMarker','IsMemoryRecoveryHexDigest']:
  match=re.search(r'private static bool '+name+r'\([^;]+;',recovery);assert match and '=>' in match.group();snippets.append(match.group())
 oracle=subprocess.check_output(['git','show','40b92e67:MyBehavior.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
 oracle_body=ex.declaration(oracle,'private static List<DailyMemoryDraft> SanitizeDailyMemoryDrafts(')
 snippets.append(oracle_body.replace('SanitizeDailyMemoryDrafts(', 'SanitizeDailyMemoryDraftsOracle(',1))
 manifest.append(dict(file='MyBehavior.cs',signature='SanitizeDailyMemoryDrafts oracle',source_revision='40b92e67',sha256=hashlib.sha256(oracle_body.encode()).hexdigest()))
 product='using System; using System.Diagnostics; using System.Linq; using System.Text; using System.Text.RegularExpressions; using System.Collections.Generic; using System.Threading.Tasks; using Newtonsoft.Json.Linq; using System.Security.Cryptography; using TaleWorlds.CampaignSystem; using TaleWorlds.CampaignSystem.Settlements; using TaleWorlds.Library; namespace AnimusForge { public partial class MyBehavior { private const string NonHeroMemoryIdPrefix="af_nonhero:"; private const int RecentNpcActionWindowDays=30;\n'+'\n\n'.join(snippets)+'\n}}'
 fixture=(HERE/'CapturedHarness.cs.txt').read_text(encoding='utf-8-sig');fixture=fixture[:fixture.index('  static void ThreeKinds() {')]+'\n}}'
 fixture=exact(fixture,'private static double GetDailyMaintenanceFrameBudgetMs() => 1000.0;','private static double GetDailyMaintenanceFrameBudgetMs() { SealProbe.Hit("budget-settings"); return SealProbe.Budget; }')
 fixture,count=re.subn(r'^  bool HasCompressedMemoryBlock\([^\n]+\n','',fixture,flags=re.M);assert count==1
 input_code=read('MyBehavior.MemorySummaryInput.cs');input_code=exact(input_code,'await Task.Delay(api.RetryAfterSeconds.HasValue ? Math.Max(1000, api.RetryAfterSeconds.Value * 1000) : 1500)','await FixtureDelayAsync(api.RetryAfterSeconds.HasValue ? Math.Max(1000, api.RetryAfterSeconds.Value * 1000) : 1500)')
 variant=('original-'+baseline if baseline else (a.mutate or 'current'));out=HERE/'.generated/sealing'/variant;out.mkdir(parents=True,exist_ok=True)
 deps=ROOT/'.tmp/nuget-packages/newtonsoft.json/13.0.3/lib/net6.0/Newtonsoft.Json.dll';assert deps.exists()
 product=apply_product_mutation(product, a.mutate)
 files={'Product.cs':product,'Input.cs':input_code,'Boundary.cs':read('MyBehavior.MemorySummaryMainThread.cs'),'Guard.cs':read('SaveRuntimeGuard.cs'),'Fixture.cs':fixture,'Sealing.cs':(HERE/'SealingHarness.cs.txt').read_text(encoding='utf-8-sig'),'Proof.csproj':'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>'+escape(str(deps))+'</HintPath></Reference></ItemGroup></Project>','NuGet.Config':'<configuration><packageSources><clear/></packageSources></configuration>'}
 if new_sealing:
  seal=read('MyBehavior.MemorySealing.cs');manifest.append(dict(file='MyBehavior.MemorySealing.cs',sha256=hashlib.sha256(seal.encode()).hexdigest(),whole_partial=True))
  seal=exact(seal,'T job = current[index.Cursor++];','T job = current[index.Cursor++]; SealProbe.Hit(typeof(T)==typeof(MemorySummaryJob)?"daily-index":"major-index");')
  seal=exact(seal,'int index = Cursor++;','int index = Cursor++; SealProbe.Hit(typeof(T)==typeof(MemorySummaryJob)?"daily-final-filter":"major-final-filter");')
  shared_budget='MemoryMaintenanceWorkBudget' in seal
  budget_code=read('Refactor/Runtime/MemoryMaintenanceWorkBudget.cs') if shared_budget else seal
  budget_code=exact(budget_code,'Expensive--;','Expensive--; SealProbe.Hit("expensive-granted");')
  budget_code=exact(budget_code,'Metadata--;','Metadata--; SealProbe.Hit("metadata-granted");')
  if shared_budget:
   files['BudgetBinding.cs']=read('MyBehavior.MemoryMaintenanceBudget.cs')
   files['BudgetRuntime.cs']=apply_seal_mutation(budget_code,a.mutate if a.mutate in ('unbounded-metadata','unbounded-expensive','ignore-deadline') else None)
   manifest.extend(dict(file=name,sha256=hashlib.sha256(read(name).encode()).hexdigest(),whole_component=True) for name in ['MyBehavior.MemoryMaintenanceBudget.cs','Refactor/Runtime/MemoryMaintenanceWorkBudget.cs'])
  else:seal=budget_code
  if 'DailyMemoryDraft draft = list[draftIndex];' in seal:seal=exact(seal,'DailyMemoryDraft draft = list[draftIndex];','DailyMemoryDraft draft = list[draftIndex]; SealProbe.Hit("draft-visited");')
  if 'list = SanitizeDailyMemoryDrafts(list);' in seal:seal=exact(seal,'list = SanitizeDailyMemoryDrafts(list);','SealProbe.Hit("owner-sanitize"); if(list.Count>0) SealProbe.Hit("owner-sanitize-nonempty"); list = SanitizeDailyMemoryDrafts(list);')
  seal=exact(seal,'_dailyMemoryDraftSealOwnerKeys.Add(state.OwnerEnumerator.Current.Key);','_dailyMemoryDraftSealOwnerKeys.Add(state.OwnerEnumerator.Current.Key); SealProbe.Hit("owner-index");')
  seal=exact(seal,'foreach (var owner in state.CompletedOwners)\n        {','foreach (var owner in state.CompletedOwners)\n        { SealProbe.Hit("completed-owner-check");')
  files['MemorySealing.cs']=apply_seal_mutation(seal, None if shared_budget and a.mutate in ('unbounded-metadata','unbounded-expensive','ignore-deadline') else a.mutate)
 if 'CooperativeMemoryQueueSort' in files.get('MemorySealing.cs',''):
  path='Refactor/Runtime/CooperativeMemoryQueueSort.cs';sort=read(path)
  manifest.append(dict(file=path,sha256=hashlib.sha256(sort.encode()).hexdigest(),whole_component=True))
  sort=exact(sort,'if (!budget.Take(false)) return false;','if (!budget.Take(false)) return false; SealProbe.Hit(typeof(T).Name=="DailyMemoryDraft" ? "owner-sort-unit" : "queue-sort-unit");',2)
  if a.mutate=='unbudgeted-sort':sort=exact(sort,'if (!budget.Take(false)) return false;', '',2)
  if a.mutate=='unstable-sort':sort=exact(sort,'Compare(_input[_left], _input[_right]) <= 0','Compare(_input[_left], _input[_right]) < 0')
  if a.mutate=='ordinal-sort':sort=exact(sort,'_compareInfo.Compare(a.Name, b.Name, CompareOptions.None)','string.CompareOrdinal(a.Name, b.Name)')
  if a.mutate=='ignore-sort-culture':sort=exact(sort,'_compareInfo.Equals(CultureInfo.CurrentCulture.CompareInfo)','true')
  files['QueueSort.cs']=sort
 if 'ComputeMemorySummarySourceFingerprint(source)' in input_code:
  for name in ['MyBehavior.MemorySourceFingerprint.cs','Refactor/Runtime/MemorySourceFingerprintWriter.cs']:
   files[Path(name).name]=read(name)
   manifest.append(dict(file=name,sha256=hashlib.sha256(read(name).encode()).hexdigest(),whole_component=True))
 files['Proof.csproj']=files['Proof.csproj'].replace('<OutputType>','<EnableDefaultCompileItems>false</EnableDefaultCompileItems><OutputType>',1).replace('</Project>','<ItemGroup>'+''.join('<Compile Include="'+name+'" />' for name in files if name.endswith('.cs'))+'</ItemGroup></Project>')
 for path,text in files.items():(out/path).write_bytes(text.encode())
 meta=dict(source_revision=baseline or 'worktree',mutation=a.mutate,source_sha256=hashlib.sha256(source.encode()).hexdigest(),declarations=manifest,generated_sha256={p:hashlib.sha256(t.encode()).hexdigest() for p,t in files.items()},seams=['Actual Seal/Reset/HasPast/TryRun/sanitizers/pending/major enqueue/cancel execute; game owner identity and summary-start are fixtures','Entry/iteration counters only; controlled entry delay exercises actual Stopwatch budget'],limits=['One owner sanitizer and inner source scan still atomic','No real game/save/provider or overall frame-time acceptance'])
 (out/'manifest.json').write_bytes(json.dumps(meta,ensure_ascii=False,indent=2).encode())
 dotnet=Path(os.environ.get('DOTNET_EXE', r'C:/Program Files/dotnet/dotnet.exe'));env=dict(os.environ,DOTNET_ROOT=str(dotnet.parent),DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(ROOT/'.tmp/nuget-packages'),APPDATA=str(ROOT/'.tmp/appdata'),DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
 build=subprocess.run([str(dotnet),'build',str(out/'Proof.csproj'),'-c','Release','--nologo','-p:RestoreConfigFile='+str(out/'NuGet.Config')],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120);(out/'build.log').write_bytes((build.stdout+build.stderr).encode())
 if build.returncode:print(build.stdout+build.stderr);return 2
 run=subprocess.run([str(dotnet),str(out/'bin/Release/net8.0/Proof.dll')],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120);log=run.stdout+run.stderr;(out/'run.log').write_bytes(log.encode());print('BUILD_PASS sealing='+variant);print(log,end='');return run.returncode if 'SEALING_RESULT' in log else 2
if __name__=='__main__':
 try:result=main()
 except Exception as exc:print('SEALING_TOOL_ERROR '+type(exc).__name__+': '+str(exc));result=2
 raise SystemExit(result)
