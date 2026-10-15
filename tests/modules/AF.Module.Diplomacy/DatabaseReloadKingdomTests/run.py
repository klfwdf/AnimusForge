"""Replay intact production reload planning/apply/matching/clone methods; world/JSON input boundaries are fixtures."""
import argparse, importlib.util, json, hashlib, subprocess, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
spec=importlib.util.spec_from_file_location('extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
p=argparse.ArgumentParser();p.add_argument('--source-file',type=Path);p.add_argument('--run-root',type=Path);a=p.parse_args()
source=a.source_file or ROOT/'src/modules/AF.Module.Diplomacy/Profiles/KingdomStrategicProfileBehavior.cs'
s=source.read_text(encoding='utf-8-sig')
methods=[
 'internal bool TryBuildDatabaseReloadPlan(', 'internal bool TryApplyDatabaseReloadPlan(',
 'private KingdomStrategicProfileStorage BuildDatabaseReloadCandidateStorage(',
 'private KingdomImportTargetIndex BuildImportTargetIndex(',
 'private static bool TryResolveImportTarget(', 'private static KingdomStrategicProfileRecord CloneForExport(',
 'private static KingdomStrategicProfileStorage CloneStorage(',
 'private static void ResetProfileToDefaultForDatabaseReload(', 'private static void ApplyDatabaseSourceProfile(',
 'private static string NormalizeId(', 'private static string NormalizeName(', 'private static bool IsDynamicKingdomId(',
 'private sealed class KingdomImportTargetIndex']
types=['internal sealed class KingdomDatabaseReloadPlan','internal sealed class KingdomStrategicProfileStorage','internal sealed class KingdomStrategicProfileRecord']
out=new_run_root(ROOT,'database-reload-kingdoms',a.run_root)
template=Path(__file__).with_name('Harness.cs.txt').read_text(encoding='utf-8')
(out/'Program.cs').write_text(template.replace('@@METHODS@@','\n'.join(ex.declaration(s,x) for x in methods)).replace('@@TYPES@@','\n'.join(ex.declaration(s,x) for x in types)),encoding='utf-8')
dep=next(path for path in (ROOT/'local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll',ROOT/'.tmp/build_check/1.4/Newtonsoft.Json.dll') if path.is_file())
(out/'Probe.csproj').write_text(f'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>{dep.as_posix()}</HintPath></Reference></ItemGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>',encoding='utf-8')
dotnet=resolve_dotnet(ROOT)
r=subprocess.run([str(dotnet),'run','--project',str(out/'Probe.csproj'),'-c','Release'],cwd=out,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace')
(out/'run.log').write_text(r.stdout+r.stderr,encoding='utf-8')
(out/'receipt.json').write_text(json.dumps({'source':str(source),'sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'declarations':methods+types,'exit':r.returncode,'scope':__doc__},indent=2),encoding='utf-8')
print(r.stdout+r.stderr);print(out);sys.exit(r.returncode)
