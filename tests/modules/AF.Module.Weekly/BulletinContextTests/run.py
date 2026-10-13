"""Replay real context policies, capture and request methods with game/provider leaves stubbed."""
import argparse
import hashlib
import json
import re
import sys
from pathlib import Path
import subprocess
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
parser = argparse.ArgumentParser()
parser.add_argument('--target-framework', default='net8.0')
args = parser.parse_args()
out = new_run_root(ROOT, 'bulletin-context', None)
dotnet = resolve_dotnet(ROOT)
env = minimal_test_environment(dotnet, out)
host = (ROOT / 'src/modules/AF.Module.Knowledge/Host/KnowledgeLibraryBehavior.cs').read_text(encoding='utf-8-sig')
def span(source, signature):
    start = source.index(signature)
    begin = source.index('{', start)
    depth = 1
    end = begin + 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[start:end]
(out / 'KnowledgeTypes.cs').write_text('using System; using System.Text; using System.Linq; using System.Collections.Generic; namespace AnimusForge { public partial class KnowledgeLibraryBehavior {' + '\n'.join(span(host, 'public class ' + n + '\n') for n in ['LoreRule', 'LoreVariant', 'LoreWhen', 'LoreTextMapping']) + span(host, 'internal string BuildBulletinLoreSource(') + '}}', encoding='utf-8')
owner = (ROOT / 'src/modules/AF.Module.Weekly/Generation/WorldBulletinStateOwner.cs').read_text(encoding='utf-8-sig')
capture = (ROOT / 'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.WorldBulletin.cs').read_text(encoding='utf-8-sig')
adapter = (ROOT / 'src/AF.GameAdapter.Bannerlord/Weekly/WorldBulletinEventCaptureAdapter.cs').read_text(encoding='utf-8-sig')
policy = (ROOT / 'src/modules/AF.Module.Weekly/Bulletin/WorldBulletinPolicy.cs').read_text(encoding='utf-8-sig')
generated = 'using System; using System.Text; using System.Linq; using System.Collections.Generic; using System.Threading.Tasks; namespace AnimusForge { '
generated += 'internal sealed partial class WorldBulletinStateOwner {' + span(owner, 'internal async Task RunWorldBulletinRequestAsync(') + '}'
generated += span(owner, 'internal sealed class WorldBulletinPromptFacts')
generated += 'internal sealed partial class MyBehavior {' + span(capture, 'private WorldBulletinPromptFacts CaptureWorldBulletinPromptFacts(') + '}'
generated += 'internal sealed partial class WorldBulletinEventCaptureAdapter {' + span(adapter, 'internal WorldBulletinPromptFacts CaptureWorldBulletinPromptFacts(') + '}'
generated += 'internal static partial class WorldBulletinPolicy {' + span(policy, 'public static string BuildSystemPrompt(') + '}}'
(out / 'ProductionSpans.cs').write_text(generated, encoding='utf-8')
(out / 'HostLeaves.cs').write_text('''namespace AnimusForge {
internal static class AnimusForgeModulePaths {
 internal static string GetModuleDataFilePath(string file) => System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("BULLETIN_TEXT_ROOT"), file);
}}
namespace TaleWorlds.Localization { public class TextObject { public TextObject(string text) {} } }
''', encoding='utf-8')
sdk = subprocess.check_output([str(dotnet), '--list-sdks'], env=env, text=True).strip().splitlines()[-1]
newtonsoft = Path(sdk.split('[', 1)[1].rstrip(']')) / sdk.split()[0] / 'Newtonsoft.Json.dll'
sources = [HERE / 'Program.cs', HERE / 'HostLeaves.cs', out / 'KnowledgeTypes.cs', out / 'HostLeaves.cs', out / 'ProductionSpans.cs',
 ROOT / 'src/modules/AF.Module.Weekly/Bulletin/WorldBulletinContextPolicy.cs',
 ROOT / 'src/modules/AF.Module.Knowledge/Lore/BulletinLoreSelectionPolicy.cs',
 ROOT / 'src/modules/AF.Module.Weekly/Bulletin/WorldBulletinPolicyTexts.cs',
 ROOT / 'src/modules/AF.Module.Knowledge/Host/KnowledgePromptTexts.cs']
project = f'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>{args.target_framework}</TargetFramework><LangVersion>latest</LangVersion><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup>'
project += ''.join(f'<Compile Include="{escape(str(p))}" />' for p in sources)
project += f'<Reference Include="Newtonsoft.Json"><HintPath>{escape(str(newtonsoft))}</HintPath></Reference></ItemGroup></Project>'
(out / 'Replay.csproj').write_text(project, encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding='utf-8')
originals = [p for p in sources if not p.is_relative_to(out)] + [ROOT / p for p in [
 'src/modules/AF.Module.Knowledge/Host/KnowledgeLibraryBehavior.cs',
 'src/modules/AF.Module.Weekly/Generation/WorldBulletinStateOwner.cs',
 'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.WorldBulletin.cs',
 'src/AF.GameAdapter.Bannerlord/Weekly/WorldBulletinEventCaptureAdapter.cs',
 'src/modules/AF.Module.Weekly/Bulletin/WorldBulletinPolicy.cs']]
for module, filename in [('Weekly', 'WorldBulletinPromptTexts'), ('Knowledge', 'KnowledgeRagPromptTexts')]:
    base = ROOT / f'content/modules/AF.Module.{module}/ModuleData'
    cn_path, en_path = base / (filename + '.json'), base / (filename + '.en.json')
    originals.extend([cn_path, en_path])
    cn, en = (json.loads(p.read_text(encoding='utf-8-sig')) for p in [cn_path, en_path])
    keys = [k for k in cn if k.startswith('AF_WBLT_Prompt_') and k in ('AF_WBLT_Prompt_PrivacyReview', 'AF_WBLT_Prompt_ContextSafety', 'AF_WBLT_Prompt_LoreContextHeader', 'AF_WBLT_Prompt_PublicHistoryHeader', 'AF_WBLT_Prompt_NoPublicHistory', 'AF_WBLT_Prompt_PublicHistoryEntry')] if module == 'Weekly' else ['BulletinLoreEntry']
    assert len(keys) == (6 if module == 'Weekly' else 1)
    for key in keys:
        assert cn[key] and en[key] and re.findall(r'\{[A-Z]+\}', cn[key]) == re.findall(r'\{[A-Z]+\}', en[key]), key
    mapped = json.loads((ROOT / 'content/content-map.json').read_text(encoding='utf-8-sig'))['entries']
    assert any(row['source'] == cn_path.relative_to(ROOT).as_posix() and row['target'] == 'ModuleData/' + filename + '.json' for row in mapped)
originals += [HERE / 'run.py', ROOT / 'content/content-map.json']
(out / 'source-manifest.json').write_text(json.dumps({
    'revision': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip(),
    'sources': {p.relative_to(ROOT).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in originals},
    'game': 'STUBBED', 'provider': 'STUBBED', 'targetFramework': args.target_framework}, indent=2), encoding='utf-8')
result = subprocess.run([str(dotnet), 'build', str(out / 'Replay.csproj'), '--nologo'], env=env, capture_output=True, text=True, encoding='utf-8', errors='replace')
(out / 'build.log').write_text(result.stdout + result.stderr, encoding='utf-8')
print(out)
if result.returncode:
    print((result.stdout + result.stderr)[-5000:])
    raise SystemExit(result.returncode)
for language in ['CN', 'EN']:
    texts = out / language
    texts.mkdir()
    source = ROOT / 'content/modules/AF.Module.Weekly/ModuleData' / ('WorldBulletinPromptTexts.en.json' if language == 'EN' else 'WorldBulletinPromptTexts.json')
    (texts / 'WorldBulletinPromptTexts.json').write_bytes(source.read_bytes())
    lore = ROOT / 'content/modules/AF.Module.Knowledge/ModuleData' / ('KnowledgeRagPromptTexts.en.json' if language == 'EN' else 'KnowledgeRagPromptTexts.json')
    (texts / 'KnowledgeRagPromptTexts.json').write_bytes(lore.read_bytes())
    env['BULLETIN_TEXT_ROOT'] = str(texts)
    result = subprocess.run([str(dotnet), str(out / f'bin/Debug/{args.target_framework}/Replay.dll')], env=env, capture_output=True, text=True, encoding='utf-8', errors='replace')
    (out / (language + '.log')).write_text(result.stdout + result.stderr, encoding='utf-8')
    print(language, (result.stdout + result.stderr)[-3000:])
    if result.returncode: raise SystemExit(result.returncode)
