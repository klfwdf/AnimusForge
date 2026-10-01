from pathlib import Path
import argparse,re,subprocess,sys
ROOT=Path(__file__).resolve().parents[3];HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import minimal_test_environment
parser=argparse.ArgumentParser();parser.add_argument('--run-root',required=True);args=parser.parse_args()
out=Path(args.run_root).resolve();out.relative_to(ROOT);out.mkdir(parents=True,exist_ok=True)
fields=[]
for p in (ROOT/'src/modules/AF.Module.Memory/Summary').glob('MemoryBusinessStateOwner*.cs'):
 for line in p.read_text(encoding='utf-8-sig').splitlines():
  if re.match(r'^\s*internal (?:Dictionary<.*>|List<.*>) \w+\s*=',line):fields.append(line)
source=(ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_text(encoding='utf-8-sig')
a=source.index('\tinternal class DialogueDay');z=source.index('\n\t}',a)+4
harness=(HERE/'Harness.cs.txt').read_text(encoding='utf-8').replace('@@STATE_FIELDS@@','\n'.join(fields)).replace('@@DIALOGUE_DAY@@',source[a:z])
(out/'Program.cs').write_text(harness,encoding='utf-8')
paths=['src/AF.GameAdapter.Bannerlord/Persistence/CampaignMemoryPersistenceAdapter.cs','src/AF.Persistence/CampaignSaveChunkHelper.cs','src/AF.Persistence/OwnerJsonStorageCodec.cs','src/modules/AF.Module.Memory/Records/MemoryPersistenceModels.cs','src/modules/AF.Module.Memory/Records/NpcActionEntry.cs']
reference=ROOT/'local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll'
(out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+str(ROOT/p)+'"/>' for p in paths)+'<Reference Include="Newtonsoft.Json"><HintPath>'+str(reference)+'</HintPath></Reference></ItemGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet=ROOT/'local/dotnet/8.0.425/dotnet.exe';result=subprocess.run([str(dotnet),'run','--project',str(out/'Tests.csproj'),'-c','Release'],cwd=out,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90)
(out/'run.log').write_text(result.stdout+result.stderr,encoding='utf-8');print(result.stdout+result.stderr,end='');raise SystemExit(result.returncode)
