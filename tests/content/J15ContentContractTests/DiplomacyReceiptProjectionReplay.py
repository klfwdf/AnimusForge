"""Compile genuine diplomacy receipt sources; isolate output and use both real packages."""
import argparse
import hashlib
import importlib.util
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
parser.add_argument('--mutation', choices=['double-execution', 'append-receipts', 'bypass-cleaner'])
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
    'src/modules/AF.Module.Diplomacy/Presentation/DiplomacyCompatibilityTexts.cs',
    'src/modules/AF.Module.Diplomacy/Presentation/DiplomacyReplyTextRules.cs',
    'src/modules/AF.Module.Conversation/Channels/Courier/CourierVisibleLetterSanitizer.cs',
    'src/modules/AF.Module.Conversation/Internal/History/ConversationSpeechTextRules.cs',
    'src/modules/AF.Module.Conversation/Internal/History/ConversationMessage.cs',
    'src/modules/AF.Module.Prompt/Composition/ConversationRoleClassificationOwner.cs',
    'src/modules/AF.Module.Prompt/Runtime/PromptComposerTexts.cs',
    'src/modules/AF.Module.Conversation/Internal/ConversationRuntimeTexts.cs',
    'src/modules/AF.Module.Llm/Protocol/LlmVisibleReplyNormalizer.cs',
    'src/modules/AF.Module.Economy/Host/GiveAssetTagCodec.cs',
]
evidence = {}
for relative in sources:
    source = ROOT / relative
    shutil.copyfile(source, output / source.name)
    evidence[relative] = hashlib.sha256(source.read_bytes()).hexdigest()
if args.mutation:
    copied = output / ('DiplomacyReplyTextRules.cs' if args.mutation == 'bypass-cleaner' else 'DiplomacyOralTagApplication.cs')
    text = copied.read_text(encoding='utf-8-sig')
    changes = {
        'double-execution': ('string status = ProcessSingle(source, match.Groups[1].Value, match.Groups[2].Value);', 'string status = ProcessSingle(source, match.Groups[1].Value, match.Groups[2].Value); ProcessSingle(source, match.Groups[1].Value, match.Groups[2].Value);'),
        'append-receipts': ('return DiplomacyReplyTextRules.CleanNpcText(body);', 'return body + "\\n" + string.Join("\\n", receipts ?? System.Array.Empty<string>());'),
        'bypass-cleaner': ('if (string.IsNullOrEmpty(text)) return text;', 'return text;'),
    }
    before, after = changes[args.mutation]
    assert text.count(before) == 1, 'mutation target changed; do not bypass the negative guard'
    copied.write_text(text.replace(before, after, 1), encoding='utf-8')
    (output / 'mutation.json').write_text(json.dumps({'mutation': args.mutation, 'compiledCopySha256': hashlib.sha256(copied.read_bytes()).hexdigest()}), encoding='utf-8')
shutil.copyfile(Path(__file__).with_suffix('.cs.txt'), output / 'Program.cs')
spec = importlib.util.spec_from_file_location('prose_extract', ROOT / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
def spans(path, signatures):
    source = (ROOT / path).read_text('utf-8-sig')
    evidence[path] = hashlib.sha256((ROOT / path).read_bytes()).hexdigest()
    return '\n'.join(extract.declaration(source, signature) for signature in signatures)
helpers = 'using System; using System.Collections.Generic; using System.Text; using System.Text.RegularExpressions; namespace AnimusForge;\n'
helpers += 'internal static class ShoutUtils { ' + spans('src/modules/AF.Module.Conversation/Channels/Scene/ShoutUtils.cs', [
    'public static bool TrySplitNamePrefixedLineSafely(', 'public static string StripNamePrefixedLineSafely(', 'public static string StripConversationMetadataPrefix(']) + ' }\n'
helpers += 'internal static class ConversationActionPostprocessOwner { ' + spans('src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs', ['internal static string StripActionTagsForSceneSpeech(']) + ' }\n'
helpers += 'internal static class MainPromptMessageAssemblyOwner { ' + spans('src/modules/AF.Module.Prompt/Composition/MainPromptMessageAssemblyOwner.cs', [
    'internal static object CreateCourierChatMessage(', 'internal static bool TryConvertCourierMemoryMessageToChatMessage(',
    'internal static string BuildCourierMemoryMetadataPrefix(', 'internal static string StripCourierPromptScopeLabel(',
    'internal static string StripCourierSpeakerPrefix(']) + ' }\n'
helpers += 'internal static class WorldDiplomacyPresentationQueries { ' + spans('src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyPresentationQueries.cs', ['internal static string ProjectDocumentBody(']) + '''
}\ninternal sealed class WorldDiplomacyDocument { internal bool IsPlayerAuthored; internal string Body; }
'''
helpers += 'internal static class HistorySectionProjectionOwner { ' + spans('src/modules/AF.Module.Prompt/Composition/HistorySectionProjectionOwner.cs', ['internal static bool TryRenderSceneHistoryLine(']) + '''
internal static string NormalizeScenePlayerHistoryLine(string text, string target, bool heard, float distance, string player) => text;
}\n'''
helpers += 'internal static class SceneHistoryMessageAssemblyOwner { ' + spans('src/modules/AF.Module.Prompt/Composition/SceneHistoryMessageAssemblyOwner.cs', ['internal static string NormalizeSceneHistoryPromptLineContent(']) + '''
internal static bool TryNormalizeAfefFactLineForPrompt(string text, out string result) { result = text; return text.StartsWith("[AFEF"); }
internal static string BuildScopedAfefFactLineForPrompt(string text, bool isCurrent) => text;
}\ninternal static class MemorySystemFactProjection { internal static string ProjectNpcFact(string text) => text; }
'''
(output / 'SharedTextDependencies.cs').write_text(helpers, encoding='utf-8')
newtonsoft = Path.home() / '.nuget/packages/newtonsoft.json/13.0.3/lib/net6.0/Newtonsoft.Json.dll'
assert newtonsoft.is_file(), 'actual Newtonsoft dependency missing; do not replace the production reader'
base = ROOT / 'content/modules/AF.Module.Diplomacy/ModuleData'
embedded = ''
for lang, filename, xml in [('cn', 'DiplomacyMiscPromptTexts.json', 'Languages/CNs/afdip_misc_strings-zh-CN.xml'), ('en', 'DiplomacyMiscPromptTexts.en.json', 'Languages/afdip_misc_strings.xml')]:
    embedded += '<EmbeddedResource Include="' + escape(str(base / filename)) + '" LogicalName="AnimusForge.Diplomacy.ReceiptTexts.' + lang + '.json" WithCulture="false" />'
    embedded += '<EmbeddedResource Include="' + escape(str(base / xml)) + '" LogicalName="AnimusForge.Diplomacy.ReceiptUi.' + lang + '.xml" WithCulture="false" />'
for path, name in [('content/modules/AF.Module.Prompt/ModuleData/PromptCompatibility.json', 'AnimusForge.Prompt.Compatibility.json'), ('content/modules/AF.Module.Conversation/ModuleData/ConversationRuntimeCompatibility.json', 'AnimusForge.Conversation.RuntimeCompatibility.json')]:
    embedded += '<EmbeddedResource Include="' + escape(str(ROOT / path)) + '" LogicalName="' + name + '" />'
(output / 'Proof.csproj').write_text(
    '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
    '<TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings>'
    '<LangVersion>latest</LangVersion><UseSharedCompilation>false</UseSharedCompilation></PropertyGroup><ItemGroup>' + embedded + '<Reference Include="Newtonsoft.Json">'
    '<HintPath>' + escape(str(newtonsoft)) + '</HintPath></Reference></ItemGroup></Project>', encoding='utf-8')
(output / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
(output / 'source-hashes.json').write_text(json.dumps(evidence, indent=2), encoding='utf-8')
dotnet = resolve_dotnet(ROOT)
environment = minimal_test_environment(dotnet, output)
for package in ('cn', 'en'):
    folder = output / package
    folder.mkdir()
    source = base / ('DiplomacyMiscPromptTexts.json' if package == 'cn' else 'DiplomacyMiscPromptTexts.en.json')
    shutil.copyfile(source, folder / 'DiplomacyMiscPromptTexts.json')
    shutil.copyfile(base / 'DiplomacyMiscCompatibility.json', folder / 'DiplomacyMiscCompatibility.json')
    shutil.copyfile(ROOT / ('content/modules/AF.Module.Prompt/ModuleData/PromptComposerTexts.' + ('zh-CN' if package == 'cn' else 'en') + '.json'), folder / 'PromptComposerTexts.json')
    shutil.copyfile(ROOT / ('content/modules/AF.Module.Conversation/ModuleData/ConversationRuntimePrompts' + ('.en' if package == 'en' else '') + '.json'), folder / 'ConversationRuntimePrompts.json')
    command = [str(dotnet), 'run', '--project', str(output / 'Proof.csproj'), '-c', 'Release']
    if package == 'en': command += ['--no-build']
    command += ['--', str(folder), str(base / 'Languages/afdip_misc_strings.xml'), str(base / 'Languages/CNs/afdip_misc_strings-zh-CN.xml')]
    result = subprocess.run(command, cwd=ROOT, env=environment, capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=90)
    (output / (package + '-run.log')).write_text(result.stdout + result.stderr, encoding='utf-8')
    print(result.stdout + result.stderr)
    if result.returncode: raise SystemExit(result.returncode)
