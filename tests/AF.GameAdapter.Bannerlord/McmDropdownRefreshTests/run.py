from pathlib import Path
import subprocess,sys,shutil,json,hashlib,argparse
from xml.sax.saxutils import escape
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
parser=argparse.ArgumentParser();parser.add_argument('--baseline');parser.add_argument('--mcm-ui',type=Path);parser.add_argument('--framework',type=Path);args=parser.parse_args()
out=new_run_root(ROOT,'mcm-dropdown-refresh',None);dotnet=resolve_dotnet(ROOT)
ref=args.framework or Path('C:/Users/29310/.nuget/packages/microsoft.netframework.referenceassemblies.net472/1.0.3/build/.NETFramework/v4.7.2')
deps=[ROOT/'.tmp/build_check/1.4/TaleWorlds.Library.dll',ROOT/'.tmp/build_check/1.4/TaleWorlds.Localization.dll']+[ROOT/'.tmp/build_check/1.4/MCMv5.dll',ROOT/'.tmp/build_check/1.4/0Harmony.dll',ref/'Facades/netstandard.dll']
ui=(args.mcm_ui or Path('F:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/Bannerlord.MBOptionScreen/bin/Win64_Shipping_Client/Bannerlord.MBOptionScreen.v1.4.0.dll')).resolve();# UI is loaded at runtime; avoid unrelated game metadata at compile time.
items=''.join('<Reference Include="'+x.stem+'"><HintPath>'+escape(str(x))+'</HintPath></Reference>' for x in deps if x.exists())
source=ROOT/'src/AF.GameAdapter.Bannerlord/UI/Common/McmDropdownRuntimeRefresh.cs'
if args.baseline:
 (out/'Production.cs').write_bytes(subprocess.check_output(['git','show',args.baseline+':'+str(source.relative_to(ROOT)).replace('\\','/')],cwd=ROOT));source=out/'Production.cs'
source_hash=hashlib.sha256(source.read_bytes()).hexdigest()
items+='<Compile Include="'+str(source)+'" />'
items+='<Compile Include="'+str(ROOT/'src/AF.GameAdapter.Bannerlord/Configuration/Mcm/AnimusForgeMcmVisibility.cs')+'" />'
(out/'Program.cs').write_bytes((Path(__file__).parent/'Program.cs').read_bytes())
(out/'Probe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net472</TargetFramework><LangVersion>latest</LangVersion><AutomaticallyUseReferenceAssemblyPackages>false</AutomaticallyUseReferenceAssemblyPackages><FrameworkPathOverride>'+str(ref)+'</FrameworkPathOverride><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup>'+items+'</ItemGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
env=minimal_test_environment(dotnet,out)
build=subprocess.run([str(dotnet),'build',str(out/'Probe.csproj'),'--nologo','-p:RestoreConfigFile='+str(out/'NuGet.Config'),'-p:UseSharedCompilation=false'],capture_output=True,text=True,encoding='utf-8',errors='replace',env=env)
(out/'build.log').write_text(build.stdout+build.stderr,encoding='utf-8')
if build.returncode: print(build.stdout[-6000:]);sys.exit(build.returncode)
for f in ui.parent.glob('*.dll'):shutil.copy2(f,out/'bin/Debug/net472'/f.name)
run=subprocess.run([str(out/'bin/Debug/net472/Probe.exe'),ui.name],cwd=out/'bin/Debug/net472',capture_output=True,text=True,encoding='utf-8',errors='replace',env=env)
(out/'receipt.json').write_text(json.dumps({'sourceSha256':source_hash,'mcmSha256':hashlib.sha256(ui.read_bytes()).hexdigest(),'baseline':args.baseline,'exit':run.returncode},indent=2))
(out/'run.log').write_text(run.stdout+run.stderr,encoding='utf-8');print(run.stdout+run.stderr);print(out);sys.exit(run.returncode)
