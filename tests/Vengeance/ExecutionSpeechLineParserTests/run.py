"""Run the existing parser tests with the actual embedded CN sources and EN prompts."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET
ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
parser = argparse.ArgumentParser()
parser.add_argument('--run-root', type=Path)
parser.add_argument('--reference-dir', type=Path, default=ROOT / '_deps_auto')
args = parser.parse_args()
out = new_run_root(ROOT, 'execution-speech-bilingual', args.run_root)
framework = Path.home() / '.nuget/packages/microsoft.netframework.referenceassemblies.net472/1.0.3/build/.NETFramework/v4.7.2'
project = ET.parse(Path(__file__).with_name('ExecutionSpeechLineParserTests.csproj'))
inputs = []
for node in project.getroot().iter():
    if node.tag in ('Compile', 'EmbeddedResource') and 'Include' in node.attrib:
        path = (Path(__file__).parent / node.get('Include')).resolve()
        node.set('Include', str(path))
        inputs.append(path)
    if node.tag == 'HintPath':
        node.text = str(args.reference_dir.resolve() / 'Newtonsoft.Json.dll')
props = project.getroot().find('PropertyGroup')
for key, value in [('AutomaticallyUseReferenceAssemblyPackages', 'false'), ('FrameworkPathOverride', str(framework))]:
    ET.SubElement(props, key).text = value
item = ET.SubElement(project.getroot(), 'ItemGroup')
ref = ET.SubElement(item, 'Reference', Include='netstandard')
ET.SubElement(ref, 'HintPath').text = str(framework / 'Facades/netstandard.dll')
project.write(out / 'Replay.csproj', encoding='utf-8', xml_declaration=True)
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
def hashes():
    return {path.relative_to(ROOT).as_posix(): hashlib.sha256(path.read_bytes()).hexdigest() for path in inputs}
before = hashes()
dotnet = resolve_dotnet(ROOT)
env = minimal_test_environment(dotnet, out)
build = subprocess.run([str(dotnet), 'build', str(out / 'Replay.csproj'), '-c', 'Release', '--nologo'], env=env, cwd=out, capture_output=True, text=True, encoding='utf-8', timeout=90)
(out / 'build.log').write_text(build.stdout + build.stderr, encoding='utf-8')
if build.returncode:
    print(build.stdout + build.stderr)
    raise SystemExit(build.returncode)
result = subprocess.run([str(out / 'bin/Release/net472/Replay.exe')], env=env, cwd=out, capture_output=True, text=True, encoding='utf-8', timeout=30)
(out / 'replay.log').write_text(result.stdout + result.stderr, encoding='utf-8')
print(result.stdout + result.stderr)
if result.returncode:
    raise SystemExit(result.returncode)
assert before == hashes(), 'replay inputs changed concurrently'
(out / 'inputs.json').write_text(json.dumps({'hashes': before, 'inputsUnchanged': True}, indent=2), encoding='utf-8')
