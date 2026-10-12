from pathlib import Path
import sys, subprocess, re
R=Path(__file__).resolve().parents[4]
sys.path.insert(0,str(R/'tests'))
from output_isolation import resolve_dotnet,minimal_test_environment
out=R/'artifacts/localization/verification-20261011/gccz-castle-bilingual-run'
out.mkdir(parents=True,exist_ok=True)
files=['CompatibilityFlatJson.cs','CastleAftermathCompatibilityTexts.cs','SiegeCastlePostprocessRuleCatalog.cs','SiegeCastlePostprocessRuleFacts.cs','SiegeCastleActionTagCatalog.cs','SiegeCastleActionKind.cs','SiegeCastleActionSpeakerRole.cs','SiegeCastlePrisonerDispositionKind.cs','SiegeCastlePlayerAuthorizationPolicy.cs','SiegeCastlePrisonerTrustProfile.cs','SiegePostprocessRuleDefinition.cs','SiegeCastlePrisonerAllocationProfile.cs','SiegeCastleSoldierReactionProfile.cs']
(out/'Probe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="Program.cs"/>'+''.join('<Compile Include="'+str(R/'AnimusForge.SiegeAftermathIntervention'/p)+'"/>' for p in files)+'</ItemGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
(out/'Program.cs').write_text((Path(__file__).parent/'Program.cs').read_text(encoding='utf-8'),encoding='utf-8')
d=resolve_dotnet(R)
run=subprocess.run([str(d),'run','--project',str(out/'Probe.csproj'),'-c','Release'],cwd=out,env=minimal_test_environment(d,out),capture_output=True,text=True,encoding='utf-8',errors='replace')
(out/'run.log').write_text(run.stdout+run.stderr,encoding='utf-8')
print(run.stdout+run.stderr)
raise SystemExit(run.returncode)
