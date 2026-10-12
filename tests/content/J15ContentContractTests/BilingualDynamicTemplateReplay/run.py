"""Replay exact current/before formatting methods with actual 1.3/1.4 TextObject."""
import argparse
import hashlib
import json
import re
from pathlib import Path
import shutil
import subprocess
import sys
from xml.sax.saxutils import escape

sys.stdout.reconfigure(encoding='utf-8')

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
sys.path.insert(0, str(ROOT / 'tools/localization'))
from inventory_untranslated import LEX

parser = argparse.ArgumentParser()
parser.add_argument('--run-root', type=Path)
parser.add_argument('--reference-13', type=Path, default=ROOT / '_deps_auto')
parser.add_argument('--reference-14', type=Path, default=ROOT / '.tmp/build_check/1.4')
parser.add_argument('--before-root', type=Path, default=ROOT / 'artifacts/localization-audit-repair-20261012/before')
parser.add_argument('--native-probe-only', action='store_true')
args = parser.parse_args()
out = new_run_root(ROOT, 'bilingual-dynamic-template', args.run_root)
framework = Path.home() / '.nuget/packages/microsoft.netframework.referenceassemblies.net472/1.0.3/build/.NETFramework/v4.7.2'
assert (framework / 'mscorlib.dll').is_file(), 'Existing net472 reference assemblies required'
inputs = set()
extractions = []

def extract(relative, signature, before=False):
    path = (args.before_root if before else ROOT) / relative
    inputs.add(path.resolve())
    source = path.read_text('utf-8-sig')
    mask = LEX.sub(lambda m: ' ' * len(m[0]), source)
    start = source.index(signature)
    opening = mask.index('{', start)
    depth = 0
    for end in range(opening, len(mask)):
        depth += (mask[end] == '{') - (mask[end] == '}')
        if not depth:
            body = source[start:end + 1]
            extractions.append({'path': str(path), 'signature': signature, 'before': before,
                                'line': source.count('\n', 0, start) + 1,
                                'endLine': source.count('\n', 0, end) + 1,
                                'methodSha256': hashlib.sha256(body.encode('utf-8')).hexdigest()})
            return body
    raise AssertionError(signature)

annex = 'src/modules/AF.Module.Diplomacy/Annexation/KingdomAnnexationBehavior.cs'
ambient = 'src/modules/AF.Module.Conversation/Ambient/Host/TownAmbientDialogueBehavior.cs'
config = 'src/modules/AF.Module.Prompt/Configuration/AIConfigHandler.cs'
clock = 'src/modules/AF.Module.Conversation/Ambient/Host/TownAmbientTime.cs'
bodies = ['using System;using System.Collections.Generic;using System.Text.RegularExpressions;using TaleWorlds.Localization;namespace AnimusForge {']
for before, prefix in ((False, 'Current'), (True, 'Before')):
    bodies.append('internal static class ' + prefix + 'Annexation {')
    bodies.append(extract(annex, 'public static string BuildRuntimeAnnexationInstructionForExternal(', before))
    for sig in ('private static string GetKingdomDisplayName(', 'private static string GetHeroDisplayName('):
        bodies.append(extract(annex, sig, before))
    bodies.append('''
private static bool TryBuildAnnexationRuntimeState(Hero hero, out Kingdom player, out Kingdom target, out Hero speaker, out string reason) {
    Fixture.LastHero = hero; player = Fixture.Player; target = Fixture.Target; speaker = Fixture.Speaker;
    reason = "controlled qualification"; return Fixture.Eligible;
}
private static void LogEvent(string name, Dictionary<string,object> values) { Fixture.LastLog = name; }
private static string DescribeHero(Hero value) => value?.StringId ?? "";
private static string DescribeKingdom(Kingdom value) => value?.StringId ?? "";
}''')
    ambient_methods = [extract(ambient, sig, before) for sig in ('private static string Render(', 'private static string ExtractSpokenText(',
        'private static string LocalizePlayerTitle(', 'private static string LocalizePlayerStatus(', 'private static Town GetContextTown(')]
    if not before:
        ambient_methods.append(extract(ambient, 'private static string LocalizeAmbientTemplate('))
    bodies.append('internal static class ' + prefix + 'Ambient {' + '\n'.join(ambient_methods))
    bodies.append('internal static string Call(string text,NpcDataPacket npc,Settlement settlement,string scene,TownAmbientPlayerContext context=null)=>Render(text,npc,settlement,scene,context);')
    if not before:
        bodies.append('internal static string Localize(string text)=>LocalizeAmbientTemplate(text);')
    bodies.append('}')
    if before:
        leaf = 'spokenText = new TextObject(spokenText).ToString();'
        assert ambient_methods[0].count(leaf) == 1
        controlled = [ambient_methods[0].replace(leaf, 'spokenText = ReplayNativeRawTemplate.Capture(spokenText);')] + ambient_methods[1:]
        bodies.append('internal static class BeforeAmbientRawBoundary {' + '\n'.join(controlled))
        bodies.append('internal static string Call(string text,NpcDataPacket npc,Settlement settlement,string scene,TownAmbientPlayerContext context=null)=>Render(text,npc,settlement,scene,context); }')
    bodies.append('internal static class ' + prefix + 'Runtime {')
    bodies.append(extract(config, 'private static string ApplyRuntimeTemplate(', before))
    pattern = (args.before_root if before else ROOT) / config
    regex_line = next(line for line in pattern.read_text('utf-8-sig').splitlines()
                      if 'private static readonly Regex RpItemIntroductionTemplateVariableRegex =' in line)
    bodies.append(regex_line)
    bodies.append('internal static string Call(string text,Dictionary<string,string> tokens)=>ApplyRuntimeTemplate(text,tokens); }')
bodies.append(extract(ambient, 'internal sealed class TownAmbientPlayerContext'))
bodies.append(extract(clock, 'internal enum TownAmbientTimeBand'))
bodies.append('internal static class TownAmbientTime {')
for sig in ('public static TownAmbientTimeBand GetBand(', 'public static string GetLocalizedDisplayName(',
            'public static string GetLocalizedGreeting(', 'private static int NormalizeHour('):
    bodies.append(extract(clock, sig))
bodies.append('}}')
(out / 'ActualMethods.cs').write_text('\n'.join(bodies), encoding='utf-8')
shutil.copyfile(Path(__file__).with_name('Program.cs'), out / 'Program.cs')

links = [
    'src/modules/AF.Module.Prompt/Runtime/PromptComposerTexts.cs',
    'src/modules/AF.Module.Diplomacy/Presentation/DiplomacyMiscPromptTexts.cs',
    'src/modules/AF.Module.Conversation/Internal/ConversationRuntimeTexts.cs',
    'src/modules/AF.Module.Conversation/Channels/Scene/NpcDataPacket.cs',
]
embedded = [
    ('content/modules/AF.Module.Prompt/ModuleData/PromptCompatibility.json', 'AnimusForge.Prompt.Compatibility.json'),
    ('content/modules/AF.Module.Conversation/ModuleData/ConversationRuntimeCompatibility.json', 'AnimusForge.Conversation.RuntimeCompatibility.json'),
]
resources = []
for lang, suffix in (('cn', 'zh-CN'), ('en', 'en')):
    package = out / lang
    package.mkdir()
    for relative, name in [
        ('content/modules/AF.Module.Prompt/ModuleData/PromptComposerTexts.' + suffix + '.json', 'PromptComposerTexts.json'),
        ('content/modules/AF.Module.Diplomacy/ModuleData/DiplomacyMiscPromptTexts' + ('.en' if lang == 'en' else '') + '.json', 'DiplomacyMiscPromptTexts.json'),
        ('content/modules/AF.Module.Conversation/ModuleData/ConversationRuntimePrompts' + ('.en' if lang == 'en' else '') + '.json', 'ConversationRuntimePrompts.json'),
        ('content/modules/AF.Module.Conversation/ModuleData/TownAmbientDialogue.' + suffix + '.json', 'TownAmbientDialogue.json'),
        ('content/modules/AF.Module.Prompt/ModuleData/RuleBehaviorPrompts' + ('.en' if lang == 'en' else '') + '.json', 'RuleBehaviorPrompts.json'),
    ]:
        resources.append(relative)
        shutil.copyfile(ROOT / relative, package / name)
for relative in ('content/modules/AF.Module.Conversation/ModuleData/Languages/afconv_ambient_strings.xml',
                 'content/modules/AF.Module.Conversation/ModuleData/Languages/CNs/afconv_ambient_strings-zh-CN.xml'):
    resources.append(relative)
inputs.update((ROOT / p).resolve() for p in links + resources + [p for p, _ in embedded])
inputs.update(Path(__file__).parent.glob('*.*'))

references = {}
for api, directory in (('1.3', args.reference_13.resolve()), ('1.4', args.reference_14.resolve())):
    versions = set(re.findall(r'v\d+\.\d+\.\d+\.\d+', (directory / 'TaleWorlds.Library.dll').read_bytes().decode('utf-16le', errors='ignore')))
    assert len(versions) == 1 and next(iter(versions)).startswith('v' + api + '.'), (api, directory, versions)
    references[api] = {'directory': str(directory), 'version': next(iter(versions))}
    for name in ('TaleWorlds.Library', 'TaleWorlds.Localization', 'Newtonsoft.Json'):
        inputs.add(directory / (name + '.dll'))

def hashes():
    return {str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(inputs)}

before_hash = hashes()
dotnet = resolve_dotnet(ROOT)
env = minimal_test_environment(dotnet, out)
summary = []
for api, data in references.items():
    directory = Path(data['directory'])
    build_root = out / api
    build_root.mkdir()
    items = '<Compile Include="' + escape(str(out / 'ActualMethods.cs')) + '"/><Compile Include="' + escape(str(out / 'Program.cs')) + '"/>'
    items += ''.join('<Compile Include="' + escape(str(ROOT / p)) + '"/>' for p in links)
    items += ''.join('<EmbeddedResource Include="' + escape(str(ROOT / p)) + '" LogicalName="' + name + '"/>' for p, name in embedded)
    for name in ('TaleWorlds.Library', 'TaleWorlds.Localization', 'Newtonsoft.Json'):
        items += '<Reference Include="' + name + '"><HintPath>' + escape(str(directory / (name + '.dll'))) + '</HintPath></Reference>'
    items += '<Reference Include="netstandard"><HintPath>' + escape(str(framework / 'Facades/netstandard.dll')) + '</HintPath></Reference>'
    project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net472</TargetFramework><LangVersion>latest</LangVersion><NoWarn>0649</NoWarn><AutomaticallyUseReferenceAssemblyPackages>false</AutomaticallyUseReferenceAssemblyPackages><FrameworkPathOverride>' + escape(str(framework)) + '</FrameworkPathOverride><DefineConstants>' + ('BANNERLORD_1_4_OR_GREATER' if api == '1.4' else 'BANNERLORD_1_3') + '</DefineConstants></PropertyGroup><ItemGroup>' + items + '</ItemGroup></Project>'
    (build_root / 'Replay.csproj').write_text(project, encoding='utf-8')
    (build_root / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
    build = subprocess.run([str(dotnet), 'build', str(build_root / 'Replay.csproj'), '-c', 'Release', '--nologo'], cwd=build_root, env=env, capture_output=True, text=True, encoding='utf-8', timeout=90)
    (build_root / 'build.log').write_text(build.stdout + build.stderr, encoding='utf-8')
    assert build.returncode == 0, build.stdout + build.stderr
    for lang in ('cn', 'en'):
        command = [str(build_root / 'bin/Release/net472/Replay.exe'), str(out / lang), str(ROOT), lang, api]
        if args.native_probe_only:
            command.append('native-only')
        run = subprocess.run(command, cwd=build_root, env=env, capture_output=True, text=True, encoding='utf-8', timeout=60)
        (build_root / (lang + '.log')).write_text(run.stdout + run.stderr, encoding='utf-8')
        print(run.stdout + run.stderr)
        assert run.returncode == 0, (api, lang, run.returncode)
        summary.append(json.loads((build_root / (lang + '-result.json')).read_text('utf-8-sig')))
after_hash = hashes()
assert before_hash == after_hash, 'Source/resource/reference inputs changed during replay'
(out / 'inputs.json').write_text(json.dumps({'references': references, 'before': before_hash, 'after': after_hash, 'inputsUnchanged': True, 'extractions': extractions,
    'controlledBeforeAmbientLeaf': {'original': 'spokenText = new TextObject(spokenText).ToString();', 'replacement': 'spokenText = ReplayNativeRawTemplate.Capture(spokenText);', 'purpose': 'Isolate old sequential formatter from preexisting native variable deletion. Unmodified before Render is also replayed.'}}, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
(out / 'summary.json').write_text(json.dumps({'passed': True, 'nativeProbeOnly': args.native_probe_only, 'runs': summary, 'assertions': sum(x['assertions'] for x in summary), 'negativeControls': sum(len(x['negativeControls']) for x in summary), 'uncovered': ['real Campaign eligibility/settlement capture', 'rule retrieval and dispatch', 'live game', 'whole repository builds', 'all English sentence semantics']}, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print('OUTPUT', out)
