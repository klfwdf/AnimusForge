import argparse,importlib.util,subprocess,os,re,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent
p=argparse.ArgumentParser();p.add_argument('--original',action='store_true');p.add_argument('--mutate');a=p.parse_args()
spec=importlib.util.spec_from_file_location('ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
def read(n):return subprocess.check_output(['git','show','38488ed2:'+n],cwd=ROOT).decode('utf-8-sig') if a.original else (ROOT/n).read_text(encoding='utf-8-sig')
s=read('MyBehavior.cs')
if not a.original:
 snapshot_spec=importlib.util.spec_from_file_location('snapshot_parity',ROOT/'tools/NativeHistorySnapshotTests/source_parity.py');snapshot_parity=importlib.util.module_from_spec(snapshot_spec);snapshot_spec.loader.exec_module(snapshot_parity)
 s=snapshot_parity.restore_snapshot_source('MyBehavior.cs',s)
calls=[line.strip() for line in s.splitlines() if line.strip().startswith('ShowCompressedMemoryBlockingPopup(')]
assert len(calls)==9
if not a.original:
 old=subprocess.check_output(['git','show','38488ed2:MyBehavior.cs'],cwd=ROOT).decode('utf-8-sig');signatures=['private async Task ProcessMemorySummaryQueueAsync()', 'private bool TryBuildMemoryRecallCandidates(', 'private bool TrySelectMemoryIdsWithPreprocess(', 'private string BuildCompressedMemoryContextById(', 'private void ShowCompressedMemoryBlockingPopup(', 'public void OnEngineTick()', 'private void ResetLocalTransientRuntimeForLoadedSave(', 'private void ClearAllDataForCurrentSave()'];restored=s;rows=[]
 for sig in signatures:
  current=ex.declaration(s,sig);prior=ex.declaration(old,sig);inverse=current
  if sig.startswith('private async') or sig.startswith('private bool'):
   lines=inverse.splitlines()
   for i,line in enumerate(lines):
    if line.strip().startswith('ShowCompressedMemoryBlockingPopup('):
     assert line.endswith(', runtimeGeneration);');lines[i]=line[:-len(', runtimeGeneration);')]+');'
   inverse='\n'.join(lines)
   if sig.startswith('private bool'):inverse=inverse.replace(', long runtimeGeneration)',')',1)
  elif sig.startswith('private string'):
   inverse=inverse.replace('\n\t\tlong runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();','',1).replace('out candidates, out var error, runtimeGeneration))','out candidates, out var error))',1).replace('out var selectedIds, out var error2, runtimeGeneration))','out var selectedIds, out var error2))',1)
  elif sig=='public void OnEngineTick()':inverse=inverse.replace('\t\t\tProcessPendingMemoryFailureNotice();\n','',1)
  elif sig in signatures[-2:]:inverse=inverse.replace('ResetMemoryFailureNotices();','_memorySummaryFailurePopupActive = false;',1)
  if not sig.startswith('private void Show'):assert inverse==prior,'Business-body change: '+sig
  assert 'TeamModuleServices.' not in current
  restored=restored.replace(current,prior,1);rows.append(dict(path='MyBehavior.cs',signature=sig,sha256=hashlib.sha256(current.encode()).hexdigest()))
 assert restored==old,'Other owner changes'
show=ex.declaration(s,'private void ShowCompressedMemoryBlockingPopup(');tick=ex.declaration(s,'public void OnEngineTick()')
stubs='\n'.join('private void '+n+'(){OtherTickPhases++;}' for n in re.findall(r'^\s+(\w+)\(\);',tick,re.M) if n!='ProcessPendingMemoryFailureNotice')
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig').replace('@@SHOW@@',show).replace('@@TICK@@',tick).replace('@@TICK_STUBS@@',stubs).replace('@@PRODUCERS@@',' '.join('case '+str(i)+': '+call+' break;' for i,call in enumerate(calls)))
out=HERE/'.generated'/('original' if a.original else a.mutate or 'current');out.mkdir(parents=True,exist_ok=True)
if not a.original:
 ui=read('MyBehavior.MemoryFailureNotice.cs')
 mutations={
  'off-main-ui':('if (!TWParallel.IsMainThread()) return;','if (false) return;'),
  'drop-generation':('&& SaveRuntimeGuard.IsCurrentGeneration(generation)','&& true'),
  'drop-campaign':('return ReferenceEquals(Campaign.Current?.GetCampaignBehavior<MyBehavior>(), this);','return true;'),
  'drop-owner':('ReferenceEquals(Instance, this)','true'),
  'drop-revision':('|| revision != Interlocked.Read(ref _memoryFailurePopupRevision)',''),
  'keep-pending-on-reset':('Interlocked.Exchange(ref _pendingMemoryFailureNotice, null);\n        Interlocked.Increment','// Mutated reset.\n        Interlocked.Increment'),
  'diagnostic-throws':('// Optional diagnostics must not hide an actionable memory failure.\n            return;','// Mutated observer.\n            throw;'),
 }
 if a.mutate:
  old,new=mutations[a.mutate];assert old in ui;ui=ui.replace(old,new,1)
 (out/'Notice.cs').write_text(ui,encoding='utf-8')
(out/'Program.cs').write_text(code,encoding='utf-8');(out/'SaveRuntimeGuard.cs').write_text(read('SaveRuntimeGuard.cs'),encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion>'+('<DefineConstants>ORIGINAL</DefineConstants>' if a.original else '')+'</PropertyGroup></Project>',encoding='utf-8');(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
dotnet=os.environ.get('DOTNET_EXE',r'C:\Program Files\dotnet\dotnet.exe')
env=os.environ.copy();env.update(DOTNET_ROOT=str(Path(dotnet).parent),DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(ROOT/'.tmp/nuget-packages'),DOTNET_GENERATE_ASPNET_CERTIFICATE='false',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1',APPDATA=str(ROOT/'.tmp/appdata'))
r=subprocess.run([dotnet,'run','--project',str(out/'Proof.csproj'),'-c','Release','-p:RestoreConfigFile='+str(out/'NuGet.Config')],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=150);log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)
