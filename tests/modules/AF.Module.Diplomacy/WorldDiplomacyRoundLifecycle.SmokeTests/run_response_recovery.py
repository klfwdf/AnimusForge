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
p.add_argument('--source-file', action='append')
p.add_argument('--package', choices=['cn', 'en'])
p.add_argument('--game-bin', type=Path)
a = p.parse_args()
out = new_run_root(ROOT, 'diplomacy-response-recovery', a.run_root)
dotnet = resolve_dotnet(ROOT, a.dotnet)
original = ET.parse(HERE / 'WorldDiplomacyRoundLifecycle.SmokeTests.csproj').getroot()
sources = {(HERE / node.attrib['Include']).resolve() for node in original.findall('./ItemGroup/Compile') if 'Include' in node.attrib}
sources.update(path.resolve() for path in HERE.glob('*.cs') if path.name != 'PersistenceRecordStubs.cs')
if a.source_ref:
    # Negative control changes only the old production orchestration lane. The new
    # additive DTO defaults remain available to the same behavioral oracle.
    for name in a.source_file or ['WorldDiplomacyOrchestration.cs', 'WorldDiplomacyOrchestration.Scheduling.cs', 'WorldDiplomacyOrchestration.Dispatch.cs']:
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
for source in original.findall('./ItemGroup/EmbeddedResource'):
    attrs = dict(source.attrib)
    attrs['Include'] = str((HERE / attrs['Include']).resolve())
    ET.SubElement(items, 'EmbeddedResource', **attrs)
references = sorted((dotnet.parent / 'sdk').glob('8.*/Newtonsoft.Json.dll'), key=lambda p: tuple(map(int, p.parent.name.split('.'))))
if not references: raise SystemExit('Existing .NET 8 SDK Newtonsoft reference is required')
newtonsoft = references[-1]
reference = ET.SubElement(items, 'Reference', Include='Newtonsoft.Json')
ET.SubElement(reference, 'HintPath').text = str(newtonsoft)
if a.game_bin:
    for source in original.findall('./ItemGroup/Reference'):
        name = source.attrib['Include']
        path = a.game_bin / (name + '.dll')
        if not path.is_file(): raise SystemExit('Missing test reference: ' + str(path))
        reference = ET.SubElement(items, 'Reference', Include=name)
        ET.SubElement(reference, 'HintPath').text = str(path.resolve())
ET.ElementTree(project).write(out / 'Recovery.csproj', encoding='utf-8', xml_declaration=True)
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
(out / 'sources.json').write_text(json.dumps({str(path):hashlib.sha256(path.read_bytes()).hexdigest() for path in sorted(sources)},indent=2),encoding='utf-8')
env = minimal_test_environment(dotnet, out)
if a.package:
    import shutil
    package = out / 'package'
    package.mkdir()
    for source in (ROOT / 'content/modules/AF.Module.Diplomacy/ModuleData').glob('*.json'):
        if source.stem.endswith('.en'): continue
        selected = source.with_name(source.stem + '.en.json') if a.package == 'en' else source
        if not selected.is_file(): selected = source
        shutil.copy2(selected, package / source.name)
    composer = ROOT / 'content/modules/AF.Module.Prompt/ModuleData' / ('PromptComposerTexts.en.json' if a.package == 'en' else 'PromptComposerTexts.zh-CN.json')
    shutil.copy2(composer, package / 'PromptComposerTexts.json')
    env.update(AF_SMOKE_PACKAGE_ROOT=str(package), AF_SMOKE_PACKAGE_LANGUAGE=a.package, AF_SMOKE_UI_LANGUAGE="English" if a.package == "en" else "Chinese")

for command, log in [([str(dotnet),'build',str(out/'Recovery.csproj'),'-c','Release','--nologo','-p:RestoreConfigFile='+str(out/'NuGet.Config')],'build.log'),
                     ([str(dotnet),str(out/'bin/Release/net8.0/Recovery.dll'),a.mode] if a.mode else [str(dotnet),str(out/'bin/Release/net8.0/Recovery.dll')],'run.log')]:
    if log == 'run.log' and a.package:
        shutil.copytree(package, out / 'bin/Release/net8.0/ModuleData', dirs_exist_ok=True)
    result = subprocess.run(command,cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180)
    (out/log).write_text(result.stdout+result.stderr,encoding='utf-8')
    print(result.stdout+result.stderr)
    if result.returncode: raise SystemExit(result.returncode)
