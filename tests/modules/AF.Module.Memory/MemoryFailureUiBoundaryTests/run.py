import argparse,importlib.util,subprocess,os,re,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
import sys
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment, current_source_path
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path);p.add_argument('--original',action='store_true');p.add_argument('--mutate');a=p.parse_args()
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
def read(n):return subprocess.check_output(['git','show','38488ed2:'+n],cwd=ROOT).decode('utf-8-sig') if a.original else (current_source_path(ROOT, n)).read_text(encoding='utf-8-sig')
s=read('MyBehavior.cs')
if a.original:
 calls=[line.strip() for line in s.splitlines() if line.strip().startswith('ShowCompressedMemoryBlockingPopup(')]
 publication_payloads=calls
else:
 # Validate all nine original publication payloads at their current owners; no old host scan claim.
 run=read('src/modules/AF.Module.Memory/Summary/MemorySummaryQueueRunRuntime.cs')
 factory=read('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemorySummaryQueueRun.cs')
 recall=read('src/modules/AF.Module.Memory/Recall/MemoryRecallContextOwner.cs')
 recall_adapter=read('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemoryRecall.cs')
 assert factory.count('BlockingPopup = ShowCompressedMemoryBlockingPopup,')==1
 runtime_calls=re.findall(r'_ports.BlockingPopup\(([^\n]*?)\);',run)
 recall_calls=[line.strip() for line in recall.splitlines() if line.strip().startswith('snapshot.RecordBlockingFailure(')]
 assert len(runtime_calls)==2 and len(recall_calls)==7,'Actual current producer closure changed'
 runtime_calls=['ShowCompressedMemoryBlockingPopup('+x.replace('error.Message','ex.Message').replace(', generation',', runtimeGeneration')+');' for x in runtime_calls]
 publication_payloads=runtime_calls+[x.replace('snapshot.RecordBlockingFailure(','ShowCompressedMemoryBlockingPopup(',1) for x in recall_calls]
 calls=runtime_calls+[x+' PublishMemoryRecallFailure(snapshot);' for x in recall_calls]
assert len(calls)==9, 'Failure publication consumer count changed'
if not a.original:
 # The historical full-process inverse predates J17. Bind only this UI publication boundary.
 accepted=subprocess.check_output(['git','show','f6e2ead7:MyBehavior.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
 assert publication_payloads==[line.strip() for line in accepted.splitlines() if line.strip().startswith('ShowCompressedMemoryBlockingPopup(')],'Unreviewed failure publication call'
 for sig in ['private void ShowCompressedMemoryBlockingPopup(', 'public void OnEngineTick()']:
  assert ex.declaration(s,sig)==ex.declaration(accepted,sig),'Unreviewed failure UI consumer: '+sig
 for sig in ['private void ResetLocalTransientRuntimeForLoadedSave(', 'private void ClearAllDataForCurrentSave(']:
  assert 'ResetMemoryFailureNotices();' in ex.declaration(s,sig),'Missing failure reset consumer'
show=ex.declaration(s,'private void ShowCompressedMemoryBlockingPopup(');tick=ex.declaration(s,'public void OnEngineTick()')
if not a.original:show+='\n'+ex.declaration(recall_adapter,'private void PublishMemoryRecallFailure(')
stubs='\n'.join('private void '+n+'(){OtherTickPhases++;}' for n in re.findall(r'^\s+(\w+)\(\);',tick,re.M) if n!='ProcessPendingMemoryFailureNotice')
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig').replace('string error=',('string failureMessage="fixture failure"; '+('var snapshot=new MemoryRecallRequest { Generation=runtimeGeneration }; ' if not a.original else '')+'string error='),1).replace('@@SHOW@@',show).replace('@@TICK@@',tick).replace('@@TICK_STUBS@@',stubs).replace('@@PRODUCERS@@',' '.join('case '+str(i)+': '+call+' break;' for i,call in enumerate(calls)))
out=new_run_root(ROOT,'MemoryFailureUiBoundaryTests',a.run_root)
if not a.original:
 # Source-extract only the scalar request publication projection; the whole recall/run behavior has separate actual owner fixtures.
 request_fields='internal long Generation; internal string BlockingTitle, BlockingMessage;'
 assert 'internal long Generation;' in recall and 'internal string BlockingTitle, BlockingMessage;' in recall
 code+='\nnamespace AnimusForge { internal sealed class MemoryRecallRequest { '+request_fields+ex.declaration(recall,'internal void RecordBlockingFailure(')+' }}'
 ui=read('MyBehavior.MemoryFailureNotice.cs')
 owner=read('src/modules/AF.Module.Memory/Summary/MemoryFailureNoticeOwner.cs')
 code=code.replace('private bool _memorySummaryFailurePopupActive;', 'private ref bool _memorySummaryFailurePopupActive => ref _memoryFailureNotices.Active;')
 mutations={
  'off-main-ui':('if (!_port.IsMainThread()) return;','if (false) return;'),
  'drop-generation':('&& SaveRuntimeGuard.IsCurrentGeneration(generation)','&& true'),
  'drop-campaign':('return ReferenceEquals(Campaign.Current?.GetCampaignBehavior<MyBehavior>(), this);','return true;'),
  'drop-owner':('ReferenceEquals(Instance, this)','true'),
  'drop-revision':('|| revision != Interlocked.Read(ref _memoryFailurePopupRevision)',''),
  'keep-pending-on-reset':('Interlocked.Exchange(ref _pendingMemoryFailureNotice, null);\n        Interlocked.Increment','// Mutated reset.\n        Interlocked.Increment'),
  'diagnostic-throws':('// Optional diagnostics must not hide an actionable memory failure.\n            return;','// Mutated observer.\n            throw;'),
 }
 if a.mutate:
  old,new=mutations[a.mutate];target=owner if a.mutate in {'off-main-ui','drop-revision','keep-pending-on-reset'} else ui
  assert target.count(old)==1,(a.mutate,target.count(old));target=target.replace(old,new,1)
  if a.mutate in {'off-main-ui','drop-revision','keep-pending-on-reset'}:owner=target
  else:ui=target
 (out/'Notice.cs').write_text(ui,encoding='utf-8')
 (out/'MemoryFailureNoticeOwner.cs').write_text(owner,encoding='utf-8')
 import json
 (out/'source-manifest.json').write_text(json.dumps({'actual_owner_sha256':hashlib.sha256(read('src/modules/AF.Module.Memory/Summary/MemoryFailureNoticeOwner.cs').encode()).hexdigest(),'actual_host_sha256':hashlib.sha256(read('MyBehavior.MemoryFailureNotice.cs').encode()).hexdigest(),'mutation':a.mutate},indent=2),encoding='utf-8')
(out/'Program.cs').write_text(code,encoding='utf-8');(out/'SaveRuntimeGuard.cs').write_text(read('src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs'),encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion>'+('<DefineConstants>ORIGINAL</DefineConstants>' if a.original else '')+'</PropertyGroup></Project>',encoding='utf-8');(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
dotnet=str(resolve_dotnet(ROOT))
env=minimal_test_environment(Path(dotnet),out)
r=subprocess.run([dotnet,'run','--project',str(out/'Proof.csproj'),'-c','Release','-p:RestoreConfigFile='+str(out/'NuGet.Config')],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=150);log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)
