from pathlib import Path
import importlib.util,subprocess,sys
ROOT=Path(__file__).resolve().parents[4]
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
spec=importlib.util.spec_from_file_location('extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
extract=importlib.util.module_from_spec(spec);spec.loader.exec_module(extract)
out=new_run_root(ROOT,'proactive-acquaintance',None)
s=(Path(__file__).with_name('Replay.cs.txt')).read_text(encoding='utf-8')
for token,path,signature in [
 ('FAMILY','src/modules/AF.Module.Social/Host/RomanceSystemBehavior.cs','public static bool IsPlayerCompanionOrFamily('),
 ('SNAPSHOT','src/modules/AF.Module.Social/Proactive/ProactiveCandidateQualification.cs','private static bool TryBuildFriendshipNeedSnapshot('),
 ('GREETING','src/modules/AF.Module.Social/Proactive/ProactiveCandidateQualification.cs','private static bool IsGreetingEligible(')]:
 text=(ROOT/path).read_text(encoding='utf-8-sig')
 s=s.replace('@@'+token+'@@',extract.declaration(text,signature))
(out/'Program.cs').write_text(s,encoding='utf-8')
(out/'Replay.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet=resolve_dotnet(ROOT);env=minimal_test_environment(dotnet,out)
r=subprocess.run([str(dotnet),'run','--project',str(out/'Replay.csproj'),'--configuration','Release','--property:RestoreConfigFile='+str(out/'NuGet.Config')],capture_output=True,text=True,encoding='utf-8',errors='replace',env=env)
(out/'run.log').write_text(r.stdout+r.stderr,encoding='utf-8');print(r.stdout+r.stderr);print(out);sys.exit(r.returncode)
