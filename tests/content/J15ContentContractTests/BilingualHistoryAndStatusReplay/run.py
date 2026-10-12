"""Replay whole production history/status owners with actual framework DLLs."""
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
out = new_run_root(ROOT, 'bilingual-history-status', args.run_root)
refs = args.reference_dir.resolve()
framework = Path.home() / '.nuget/packages/microsoft.netframework.referenceassemblies.net472/1.0.3/build/.NETFramework/v4.7.2'
assert (framework / 'mscorlib.dll').is_file(), 'Existing net472 reference assemblies required'
source = ROOT / 'src/modules/AF.Module.Prompt/Composition/SceneHistoryMessageAssemblyOwner.cs'
text = source.read_text('utf-8-sig')
mask = LEX.sub(lambda m: ' ' * len(m[0]), text)
start = text.index('internal static string FormatScenePlayerDirectSpeechLabel(')
opening = mask.index('{', start)
depth = 0
for end in range(opening, len(mask)):
    depth += (mask[end] == '{') - (mask[end] == '}')
    if not depth:
        break
label = text[start:end + 1]
(out / 'SceneLabel.cs').write_text('using System; namespace AnimusForge { internal static class SceneHistoryMessageAssemblyOwner {' + label + '''
internal static string NormalizeSceneHistoryPromptLineContent(string text) => text;
internal static bool TryNormalizeAfefFactLineForPrompt(string text, out string result) { result = text; return false; }
internal static string BuildScopedAfefFactLineForPrompt(string text, bool isCurrent) => text;
}}''', encoding='utf-8')
shutil.copyfile(Path(__file__).with_name('Program.cs'), out / 'Program.cs')
paths = [
    'src/modules/AF.Module.Prompt/Composition/HistorySectionProjectionOwner.cs',
    'src/modules/AF.Module.Conversation/Internal/History/ConversationMessage.cs',
    'src/modules/AF.Module.Prompt/Runtime/PromptComposerTexts.cs',
    'src/modules/AF.Module.Diplomacy/Presentation/DiplomacyDialogueStatusUi.cs',
    'src/modules/AF.Module.Diplomacy/Presentation/DiplomacyUiLocalization.cs',
    'src/modules/AF.Module.Diplomacy/Presentation/DiplomacyMiscPromptTexts.cs',
]
items = ''.join('<Compile Include="' + escape(str(ROOT / p)) + '" />' for p in paths)
items += '<EmbeddedResource Include="' + escape(str(ROOT / 'content/modules/AF.Module.Prompt/ModuleData/PromptCompatibility.json')) + '" LogicalName="AnimusForge.Prompt.Compatibility.json" />'
for name in ('TaleWorlds.Library', 'TaleWorlds.Localization', 'Newtonsoft.Json'):
    items += '<Reference Include="' + name + '"><HintPath>' + escape(str(refs / (name + '.dll'))) + '</HintPath></Reference>'
items += '<Reference Include="netstandard"><HintPath>' + escape(str(framework / 'Facades/netstandard.dll')) + '</HintPath></Reference>'
(out / 'Replay.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net472</TargetFramework><LangVersion>latest</LangVersion><AutomaticallyUseReferenceAssemblyPackages>false</AutomaticallyUseReferenceAssemblyPackages><FrameworkPathOverride>' + escape(str(framework)) + '</FrameworkPathOverride></PropertyGroup><ItemGroup>' + items + '</ItemGroup></Project>', encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
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
    shutil.copyfile(ROOT / ('content/modules/AF.Module.Prompt/ModuleData/PromptComposerTexts.' + suffix + '.json'), package / 'PromptComposerTexts.json')
    shutil.copyfile(ROOT / ('content/modules/AF.Module.Diplomacy/ModuleData/DiplomacyMiscPromptTexts' + ('.en' if lang == 'en' else '') + '.json'), package / 'DiplomacyMiscPromptTexts.json')
    result = subprocess.run([str(out / 'bin/Release/net472/Replay.exe'), str(package), str(ROOT), lang], env=env, cwd=out, capture_output=True, text=True, encoding='utf-8', timeout=30)
    (out / (lang + '.log')).write_text(result.stdout + result.stderr, encoding='utf-8')
    print(result.stdout + result.stderr)
    if result.returncode:
        raise SystemExit(result.returncode)
hashes = {p: hashlib.sha256((ROOT / p).read_bytes()).hexdigest() for p in paths}
hashes[source.relative_to(ROOT).as_posix()] = hashlib.sha256(source.read_bytes()).hexdigest()
(out / 'inputs.json').write_text(json.dumps({'referenceDir': str(refs), 'hashes': hashes}, indent=2) + '\n', encoding='utf-8')
