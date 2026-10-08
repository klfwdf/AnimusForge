"""Current complete helper/adapter/store + exact SyncData; native store/DTO/runtime leaves only."""
import argparse,hashlib,importlib.util,json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path,required=True);args=p.parse_args()
out=new_run_root(ROOT,'diplomacy-strict-persistence',args.run_root)
s=importlib.util.spec_from_file_location('extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(s);s.loader.exec_module(ex)
paths=['src/AF.Persistence/CampaignSaveChunkHelper.cs','src/modules/AF.Module.Diplomacy/Adapters/BannerlordWorldDiplomacyPersistenceAdapter.cs','src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyStateStore.cs']
for path in paths:(out/Path(path).name).write_bytes((ROOT/path).read_bytes())
orchpath=ROOT/'src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOrchestration.cs';orch=orchpath.read_text(encoding='utf-8-sig')
sync=ex.declaration(orch,'public void SyncData(')
catalog=json.loads((ROOT/'docs/fixtures/phase4-persistence-profile-config/persistence-catalog.json').read_text(encoding='utf-8-sig'))
strict=catalog['optInStrictChunkedStringStorage'];assert set(strict['keys'])=={'_af_world_diplomacy_v1','_af_world_diplomacy_quarantine_v1'}
assert set(strict['keys']).issubset(catalog['chunkedStringStorageKeys'])
current=(ROOT/paths[0]).read_text(encoding='utf-8-sig');before=subprocess.check_output(['git','show','ed92b3b:'+paths[0]],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
for signature in ['public static bool SafeSyncData<T>(', 'public static void SaveChunkedString(', 'public static string LoadChunkedString(', 'public static Dictionary<string, string> FlattenStringDictionary(Dictionary<string, string> source)', 'public static Dictionary<string, string> RestoreStringDictionary(']:
 assert ex.declaration(current,signature)==ex.declaration(before,signature),'legacy helper contract changed: '+signature
code=(HERE/'StrictPersistenceReplay.cs.txt').read_text(encoding='utf8').replace('@@SYNC@@',sync)
(out/'Program.cs').write_text(code,encoding='utf8')
dotnet=resolve_dotnet(ROOT);dll=dotnet.parent/'sdk/8.0.425/Newtonsoft.Json.dll'
if not dll.is_file():raise SystemExit('Pass local SDK with installed Newtonsoft.Json; dependencies are not installed.')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>'+str(dll)+'</HintPath></Reference></ItemGroup></Project>',encoding='utf8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf8')
(out/'source-manifest.json').write_text(json.dumps({'scope':__doc__,'sources':{path:hashlib.sha256((ROOT/path).read_bytes()).hexdigest() for path in paths},'syncSource':str(orchpath),'syncMethodSha256':hashlib.sha256(sync.encode()).hexdigest()},indent=2),encoding='utf8')
r=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf8',errors='replace',timeout=120)
log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf8');print(log,end='');raise SystemExit(r.returncode)
