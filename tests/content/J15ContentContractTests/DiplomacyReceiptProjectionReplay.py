"""Compile genuine diplomacy receipt sources; isolate output and use both real packages."""
import argparse
import hashlib
import json
import shutil
import subprocess
import sys
from pathlib import Path
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

parser = argparse.ArgumentParser()
parser.add_argument('--run-root', type=Path, required=True)
parser.add_argument('--mutation', choices=['double-execution', 'project-body'])
args = parser.parse_args()
output = new_run_root(ROOT, 'diplomacy-receipt-projection', args.run_root)
sources = [
    'src/modules/AF.Module.Diplomacy/Application/DiplomacyOralTagApplication.cs',
    'src/modules/AF.Module.Diplomacy/Application/IDiplomacyCommitmentTagSource.cs',
    'src/modules/AF.Module.Diplomacy/Application/DiplomacyNativeCommitmentApplication.cs',
    'src/modules/AF.Module.Diplomacy/Domain/Dialogue/DialogueTagPayload.cs',
    'src/modules/AF.Module.Diplomacy/Domain/Dialogue/DialogueContracts.cs',
    'src/modules/AF.Module.Diplomacy/Domain/Dialogue/DialogueInteractionOrigin.cs',
    'src/modules/AF.Module.Diplomacy/Presentation/DiplomacyMiscPromptTexts.cs',
    'src/modules/AF.Module.Diplomacy/Presentation/DiplomacyDialogueStatusUi.cs',
]
evidence = {}
for relative in sources:
    source = ROOT / relative
    shutil.copyfile(source, output / source.name)
    evidence[relative] = hashlib.sha256(source.read_bytes()).hexdigest()
if args.mutation:
    copied = output / 'DiplomacyOralTagApplication.cs'
    text = copied.read_text(encoding='utf-8-sig')
    changes = {
        'double-execution': ('string status = ProcessSingle(source, match.Groups[1].Value, match.Groups[2].Value);', 'string status = ProcessSingle(source, match.Groups[1].Value, match.Groups[2].Value); ProcessSingle(source, match.Groups[1].Value, match.Groups[2].Value);'),
        'project-body': ('result.Append(project == null ? status : project(status));', 'result.Append(project == null ? status : project(body));'),
    }
    before, after = changes[args.mutation]
    assert text.count(before) == 1, 'mutation target changed; do not bypass the negative guard'
    copied.write_text(text.replace(before, after, 1), encoding='utf-8')
    (output / 'mutation.json').write_text(json.dumps({'mutation': args.mutation, 'compiledCopySha256': hashlib.sha256(copied.read_bytes()).hexdigest()}), encoding='utf-8')
shutil.copyfile(Path(__file__).with_suffix('.cs.txt'), output / 'Program.cs')
newtonsoft = Path.home() / '.nuget/packages/newtonsoft.json/13.0.3/lib/net6.0/Newtonsoft.Json.dll'
assert newtonsoft.is_file(), 'actual Newtonsoft dependency missing; do not replace the production reader'
(output / 'Proof.csproj').write_text(
    '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
    '<TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings>'
    '<LangVersion>latest</LangVersion></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json">'
    '<HintPath>' + escape(str(newtonsoft)) + '</HintPath></Reference></ItemGroup></Project>', encoding='utf-8')
(output / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
(output / 'source-hashes.json').write_text(json.dumps(evidence, indent=2), encoding='utf-8')
dotnet = resolve_dotnet(ROOT)
environment = minimal_test_environment(dotnet, output)
base = ROOT / 'content/modules/AF.Module.Diplomacy/ModuleData'
for package in ('cn', 'en'):
    folder = output / package
    folder.mkdir()
    source = base / ('DiplomacyMiscPromptTexts.json' if package == 'cn' else 'DiplomacyMiscPromptTexts.en.json')
    shutil.copyfile(source, folder / 'DiplomacyMiscPromptTexts.json')
    command = [str(dotnet), 'run', '--project', str(output / 'Proof.csproj'), '-c', 'Release']
    if package == 'en': command += ['--no-build']
    command += ['--', str(folder), str(base / 'Languages/afdip_misc_strings.xml'), str(base / 'Languages/CNs/afdip_misc_strings-zh-CN.xml')]
    result = subprocess.run(command, cwd=ROOT, env=environment, capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=90)
    (output / (package + '-run.log')).write_text(result.stdout + result.stderr, encoding='utf-8')
    print(result.stdout + result.stderr)
    if result.returncode: raise SystemExit(result.returncode)
