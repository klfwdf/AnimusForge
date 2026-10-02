"""Execute real catalog/host lifecycle code; engine, patches and resource leaves are fakes."""
from pathlib import Path
import argparse, hashlib, importlib.util, json, subprocess, sys
ROOT=Path(__file__).resolve().parents[3]
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
SOURCES=[
 'src/AF.Foundation.Runtime/ModuleDirectory/InternalModuleDirectory.cs',
 'src/AF.Foundation.Runtime/ModuleDirectory/ModuleFrameworkSnapshot.cs',
 'src/AF.Foundation.Runtime/ModuleDirectory/ModuleDirectoryLifecycleOwner.cs',
 'src/AF.Foundation.Runtime/ModuleDirectory/HostedExtensionCatalog.cs',
 'src/AF.GameAdapter.Bannerlord/Composition/ModuleFrameworkRuntime.cs',
 'src/AF.GameAdapter.Bannerlord/Composition/TeamModuleRegistration.cs',
 'src/AF.GameAdapter.Bannerlord/Composition/IntegratedModuleHost.cs',
 'src/AF.Contracts/Internal/FeatureBridgeContracts.cs',
 'src/AF.Contracts/PublicApi/V1/AfApiContracts.cs',
 'src/modules/AF.Module.PublicApi/Internal/AfV1SnapshotProjection.cs',
 'extensions/AnimusForge.DialogueUI/src/SubModule.cs',
 'extensions/AnimusForge.Coup/src/SubModule.cs',
]
ILLUSTRATOR='extensions/AnimusForge.Illustrator/src/SubModule.cs'
VENGEANCE='src/bridges/Vengeance/Host/VengeanceRuntimeBridge.cs'
def main():
 sys.stdout.reconfigure(encoding='utf-8')
 ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--dotnet');ap.add_argument('--run-root',type=Path);ap.add_argument('--defer-coup',action='store_true');args=ap.parse_args()
 out=new_run_root(ROOT,'hosted-extensions',args.run_root);dotnet=resolve_dotnet(ROOT,args.dotnet)
 (out/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 spec=importlib.util.spec_from_file_location('extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
 extract=importlib.util.module_from_spec(spec);spec.loader.exec_module(extract)
 text=(ROOT/ILLUSTRATOR).read_text(encoding='utf-8-sig')
 # Only SubModule, not unrelated campaign-event handlers, is under test.
 (out/'IllustratorSubModule.cs').write_text(text[:text.index('namespace AnimusForge.Illustrator')]+'namespace AnimusForge.Illustrator {\n'+extract.declaration(text,'public sealed class SubModule')+'\n}',encoding='utf-8')
 text=(ROOT/VENGEANCE).read_text(encoding='utf-8-sig')
 methods=[extract.declaration(text,'internal static void '+name+'(') for name in ('Initialize','RegisterCampaign','Shutdown')]
 (out/'VengeanceLifecycle.cs').write_text('using System; using AnimusForge.Refactor.Modules; using RichExecutions.Core; using RichExecutions.Campaign; using RichExecutions.Customization; using RichExecutions.Diagnostics; using RichExecutions.Scene; using TaleWorlds.Core; using TaleWorlds.CampaignSystem; namespace AnimusForge { internal static class VengeanceRuntimeBridge {\n'+'\n'.join(methods)+'\n private static void SubscribeExecutionMemoryFacts() { TestLeaves.Step("vengeance.subscribe"); } private static void UnsubscribeExecutionMemoryFacts() { TestLeaves.Step("vengeance.unsubscribe"); } } }',encoding='utf-8')
 paths=[ROOT/p for p in SOURCES]+[HERE/'Fakes.cs',HERE/'Program.cs',out/'IllustratorSubModule.cs',out/'VengeanceLifecycle.cs']
 from xml.sax.saxutils import escape
 (out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+escape(str(p))+'" />' for p in paths)+'</ItemGroup></Project>')
 env=minimal_test_environment(dotnet,out)
 result=subprocess.run([str(dotnet),'run','--project',str(out/'Tests.csproj'),'-c','Release']+(['--','--defer-coup'] if args.defer_coup else []),cwd=out,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180)
 log=result.stdout+result.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log)
 manifest={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in SOURCES+[ILLUSTRATOR,VENGEANCE]}
 (out/'receipt.json').write_text(json.dumps({'exitCode':result.returncode,'sources':manifest,'boundary':'Real directory, host and lifecycle methods; fake game/patch/resources, no live game or network'},indent=2),encoding='utf-8')
 return result.returncode
if __name__=='__main__':raise SystemExit(main())
