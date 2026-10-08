"""Compile the existing actual-owner suite with explicit isolated source membership."""
from pathlib import Path
import argparse, hashlib, json, subprocess, sys
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

p = argparse.ArgumentParser()
p.add_argument('--run-root', type=Path)
p.add_argument('--dotnet')
p.add_argument('--mode', default='--player-response-recovery')
p.add_argument('--source-ref')
a = p.parse_args()
out = new_run_root(ROOT, 'diplomacy-response-recovery', a.run_root)
dotnet = resolve_dotnet(ROOT, a.dotnet)
original = ET.parse(HERE / 'WorldDiplomacyRoundLifecycle.SmokeTests.csproj').getroot()
sources = {(HERE / node.attrib['Include']).resolve() for node in original.findall('./ItemGroup/Compile') if 'Include' in node.attrib}
sources.update(path.resolve() for path in HERE.glob('*.cs') if path.name != 'PersistenceRecordStubs.cs')
if a.source_ref:
    # Negative control changes only the old production orchestration lane. The new
    # additive DTO defaults remain available to the same behavioral oracle.
    for name in ['WorldDiplomacyOrchestration.cs', 'WorldDiplomacyOrchestration.Scheduling.cs', 'WorldDiplomacyOrchestration.Dispatch.cs']:
        production = ROOT / 'src/modules/AF.Module.Diplomacy/Application' / name
        target = out / name
        target.write_bytes(subprocess.check_output(['git', 'show', a.source_ref + ':' + production.relative_to(ROOT).as_posix()], cwd=ROOT))
        sources.remove(production.resolve()); sources.add(target)
project = ET.Element('Project', Sdk='Microsoft.NET.Sdk')
props = ET.SubElement(project, 'PropertyGroup')
for k, v in {'OutputType':'Exe','TargetFramework':'net8.0','ImplicitUsings':'enable','Nullable':'enable','EnableDefaultCompileItems':'false','WarningLevel':'0'}.items():
    ET.SubElement(props, k).text = v
items = ET.SubElement(project, 'ItemGroup')
for path in sorted(sources): ET.SubElement(items, 'Compile', Include=str(path))
newtonsoft = dotnet.parent / 'sdk/8.0.425/Newtonsoft.Json.dll'
if not newtonsoft.is_file(): raise SystemExit('Existing SDK Newtonsoft reference is required')
reference = ET.SubElement(items, 'Reference', Include='Newtonsoft.Json')
ET.SubElement(reference, 'HintPath').text = str(newtonsoft)
ET.ElementTree(project).write(out / 'Recovery.csproj', encoding='utf-8', xml_declaration=True)
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
(out / 'sources.json').write_text(json.dumps({str(path):hashlib.sha256(path.read_bytes()).hexdigest() for path in sorted(sources)},indent=2),encoding='utf-8')
env = minimal_test_environment(dotnet, out)
for command, log in [([str(dotnet),'build',str(out/'Recovery.csproj'),'-c','Release','--nologo','-p:RestoreConfigFile='+str(out/'NuGet.Config')],'build.log'),
                     ([str(dotnet),str(out/'bin/Release/net8.0/Recovery.dll'),a.mode] if a.mode else [str(dotnet),str(out/'bin/Release/net8.0/Recovery.dll')],'run.log')]:
    result = subprocess.run(command,cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180)
    (out/log).write_text(result.stdout+result.stderr,encoding='utf-8')
    print(result.stdout+result.stderr)
    if result.returncode: raise SystemExit(result.returncode)
