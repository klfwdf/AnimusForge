"""Replay actual fact projection and request consumers without rewriting saved data."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
sys.path.insert(0, str(ROOT / 'tools/localization'))
from inventory_untranslated import LEX

parser = argparse.ArgumentParser()
parser.add_argument('--run-root', type=Path)
parser.add_argument('--reference-dir', type=Path, default=ROOT / '_deps_auto')
args = parser.parse_args()
out = new_run_root(ROOT, 'bilingual-fact-recall', args.run_root)
refs = args.reference_dir.resolve()
framework = Path.home() / '.nuget/packages/microsoft.netframework.referenceassemblies.net472/1.0.3/build/.NETFramework/v4.7.2'
inputs = []
def method(path, signature):
    inputs.append(path)
    source = (ROOT / path).read_text('utf-8-sig')
    mask = LEX.sub(lambda match: ' ' * len(match[0]), source)
    start = source.index(signature)
    opening = mask.index('{', start)
    depth = 0
    for end in range(opening, len(mask)):
        depth += (mask[end] == '{') - (mask[end] == '}')
        if not depth:
            return source[start:end + 1]
    raise AssertionError(signature)

recall = method('src/modules/AF.Module.Memory/Recall/MemoryRecallContextOwner.cs', 'internal static string FormatPastAfefLineForPrompt(')
daily = method('src/AF.GameAdapter.Bannerlord/Memory/MemorySummaryApplicationAdapter.cs', 'internal static string BuildDailyMemoryLineForPrompt(')
overview = method('src/AF.GameAdapter.Bannerlord/Memory/MemorySummaryApplicationAdapter.cs', 'internal static string BuildMemoryOverviewBlockSourceText(')
(out / 'ActualMethods.cs').write_text('using System;using System.Linq;using System.Text;using TaleWorlds.Library;namespace AnimusForge {internal static class MemoryRecallContextOwner {' + recall + '\ninternal static string FormatMemoryHourRange(int start,int end)=>start+"-"+end; } internal static class MemorySummaryApplicationAdapter {' + daily + '\n' + overview + '}}', encoding='utf-8')
inputs.append('src/AF.GameAdapter.Bannerlord/Prompt/MemorySummaryInputCaptureAdapter.cs')
capture = (ROOT / inputs[-1]).read_text('utf-8-sig')
assert 'afefTexts.Add(MemorySystemFactProjection.ProjectRecordedFactForPrompt(MemorySummaryApplicationAdapter.BuildDailyMemoryLineForPrompt(' in capture
assert 'normalTexts.Add(MemorySystemFactProjection.' not in capture, 'ordinary conversation must remain raw'
persist = (ROOT / 'src/AF.GameAdapter.Bannerlord/Memory/MemorySummaryApplicationAdapter.cs').read_text('utf-8-sig')
assert 'line => BuildDailyMemoryLineForPrompt(line, _resolveScene)' in persist, 'save callback retains unprojected renderer'
assert '.Select(renderDailyLine)' in persist
shutil.copyfile(Path(__file__).with_name('Program.cs'), out / 'Program.cs')
paths = [
    'src/modules/AF.Module.Prompt/Runtime/PromptComposerTexts.cs',
    'src/modules/AF.Module.Prompt/Composition/MemorySystemFactProjection.cs',
    'src/modules/AF.Module.Memory/Runtime/MemoryTextResources.cs',
    'src/AF.GameAdapter.Bannerlord/Prompt/AdapterPromptCaptureTexts.cs',
    'src/modules/AF.Module.WorldMap/Runtime/WorldMapFactPromptProjection.cs',
    'src/modules/AF.Module.WorldMap/Runtime/WorldMapSourceTexts.cs',
]
inputs.extend(paths)
inputs.append('content/modules/AF.Module.WorldMap/ModuleData/WorldMapCompatibility.json')
items = ''.join('<Compile Include="' + escape(str(ROOT / path)) + '" />' for path in paths)
for path, logical in [
    ('content/modules/AF.Module.Prompt/ModuleData/PromptCompatibility.json', 'AnimusForge.Prompt.Compatibility.json'),
    ('content/modules/AF.Module.Memory/ModuleData/MemorySourceTexts.json', 'AnimusForge.Memory.SourceTexts.json'),
    ('content/foundation/AF.Foundation.Localization/ModuleData/AdapterPromptCaptureCompatibility.json', 'AnimusForge.AdapterPromptCapture.Compatibility.json')
]:
    inputs.append(path)
    items += '<EmbeddedResource Include="' + escape(str(ROOT / path)) + '" LogicalName="' + logical + '" />'
for name in ('TaleWorlds.Library', 'Newtonsoft.Json'):
    items += '<Reference Include="' + name + '"><HintPath>' + escape(str(refs / (name + '.dll'))) + '</HintPath></Reference>'
items += '<Reference Include="netstandard"><HintPath>' + escape(str(framework / 'Facades/netstandard.dll')) + '</HintPath></Reference>'
(out / 'Replay.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net472</TargetFramework><LangVersion>latest</LangVersion><NoWarn>0649</NoWarn><AutomaticallyUseReferenceAssemblyPackages>false</AutomaticallyUseReferenceAssemblyPackages><FrameworkPathOverride>' + escape(str(framework)) + '</FrameworkPathOverride></PropertyGroup><ItemGroup>' + items + '</ItemGroup></Project>', encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
for suffix in ('zh-CN', 'en'):
    inputs += ['content/modules/AF.Module.Prompt/ModuleData/PromptComposerTexts.' + suffix + '.json', 'content/foundation/AF.Foundation.Localization/ModuleData/AdapterPromptCapture.' + suffix + '.json']
def hashes():
    return {path: hashlib.sha256((ROOT / path).read_bytes()).hexdigest() for path in sorted(set(inputs))}
before = hashes()
dotnet = resolve_dotnet(ROOT)
env = minimal_test_environment(dotnet, out)
build = subprocess.run([str(dotnet), 'build', str(out / 'Replay.csproj'), '-c', 'Release', '--nologo'], cwd=out, env=env, capture_output=True, text=True, encoding='utf-8', timeout=90)
(out / 'build.log').write_text(build.stdout + build.stderr, encoding='utf-8')
if build.returncode:
    print(build.stdout + build.stderr)
    raise SystemExit(build.returncode)
for lang, suffix in (('cn', 'zh-CN'), ('en', 'en')):
    package = out / lang
    package.mkdir()
    for relative, name in [('content/modules/AF.Module.Prompt/ModuleData/PromptComposerTexts.' + suffix + '.json', 'PromptComposerTexts.json'), ('content/foundation/AF.Foundation.Localization/ModuleData/AdapterPromptCapture.' + suffix + '.json', 'AdapterPromptCapture.json'), ('content/modules/AF.Module.WorldMap/ModuleData/WorldMapCompatibility.json', 'WorldMapCompatibility.json')]:
        shutil.copyfile(ROOT / relative, package / name)
    result = subprocess.run([str(out / 'bin/Release/net472/Replay.exe'), str(package), lang], env=env, cwd=out, capture_output=True, text=True, encoding='utf-8', timeout=30)
    (out / (lang + '.log')).write_text(result.stdout + result.stderr, encoding='utf-8')
    print(result.stdout + result.stderr)
    if result.returncode:
        raise SystemExit(result.returncode)
after = hashes()
assert before == after, 'concurrent inputs changed during replay'
(out / 'inputs.json').write_text(json.dumps({'referenceDir': str(refs), 'hashes': after, 'inputsUnchanged': True, 'uncovered': ['gathering projection', 'record-title rendering', 'live Campaign and actual sav']}, indent=2) + '\n', encoding='utf-8')
