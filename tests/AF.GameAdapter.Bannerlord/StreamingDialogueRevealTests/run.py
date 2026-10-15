"""Real reveal/helper/scroll code; native VM and layout are explicit doubles."""
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).parent
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

out = new_run_root(ROOT, 'streaming-dialogue-reveal', None)
inputs = [
    'src/AF.GameAdapter.Bannerlord/UI/Conversation/StreamingDialogueTextReveal.cs',
    'src/AF.GameAdapter.Bannerlord/UI/Conversation/ConversationHelper.cs',
    'extensions/AnimusForge.DialogueUI/src/Native/AFDialogueClickThroughScrollPanel.cs',
]
for relative in inputs:
    (out / Path(relative).name).write_bytes((ROOT / relative).read_bytes())
for name in ['Program.cs', 'Stubs.cs']:
    (out / name).write_bytes((HERE / name).read_bytes())
# Keep the actual sanitizer at the production UI boundary in this replay.
spec = importlib.util.spec_from_file_location('extractor', ROOT / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
ex = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ex)
formatter = ROOT / 'src/AF.GameAdapter.Bannerlord/UI/Common/EncyclopediaEntityLinkFormatter.cs'
sanitizer = ex.declaration(formatter.read_text(encoding='utf-8-sig'), 'internal static string SanitizeUntrustedRichText(')
interruption = ROOT / 'src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeNativeConversationOverlay.Interruption.cs'
source = interruption.read_text(encoding='utf-8-sig')
methods = '\n'.join(ex.declaration(source, sig) for sig in [
    'private void SetSubmissionDisplayText(', 'private void SetSubmissionStreamPreview('])
(out / 'OverlayMethods.cs').write_text('using System; namespace AnimusForge { internal static class EncyclopediaEntityLinkFormatter {' + sanitizer + '} public sealed partial class AnimusForgeNativeConversationOverlay {' + methods + '}}', encoding='utf-8')
prefab = ROOT / 'extensions/AnimusForge.DialogueUI/GUI/Prefabs/AFDialogueConversation.xml'
xml = ET.parse(prefab).getroot()
scroll = xml.find('.//*[@Id="AFDialogueTextScroll"]')
assert scroll.attrib['FollowStreamingText'] == 'true'
assert scroll.attrib['InnerPanel'] == 'AFDialogueTextClip\\AFDialogueTextContent'
clip = xml.find('.//*[@Id="AFDialogueTextClip"]')
assert clip.attrib['ClipContents'] == 'true'
body = clip.find('.//AFDialogueBodyRichTextWidget')
assert body.attrib['WidthSizePolicy'] == 'StretchToParent' and body.attrib['HeightSizePolicy'] == 'CoverChildren'
assert body.attrib['Text'] == '@DialogText'
assert 'FollowStreamingText' not in xml.find('.//*[@Id="AFDialogueAnswerScroll"]').attrib
main_path = ROOT / 'src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeNativeConversationOverlay.cs'
main = main_path.read_text(encoding='utf-8-sig')
assert main.count('ConversationHelper.BeginStreaming(this, DuelSettings.GetSettings()?.MainApiStreamingEnabled == true);') == 2
assert main.count('SetSubmissionStreamPreview(generation, partial);') == 2
assert main.count('streamingText: mainReplyBeforePostprocess') == 2
assert main.count('streamingText: completedReply') == 2
map_path = ROOT / 'extensions/AnimusForge.DialogueUI/GUI/Prefabs/AFDialogueMapConversation.xml'
map_xml = ET.parse(map_path).getroot()
assert map_xml.find('.//AFDialogueConversation').attrib['DataSource'] == '{DialogController}'
(out / 'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>', encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
dotnet = resolve_dotnet(ROOT)
result = subprocess.run([str(dotnet), 'run', '--project', str(out / 'Proof.csproj')], cwd=out,
    env=minimal_test_environment(dotnet, out), capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=120)
(out / 'run.log').write_text(result.stdout + result.stderr, encoding='utf-8')
(out / 'source-manifest.json').write_text(json.dumps({str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest()
    for p in [*(ROOT / item for item in inputs), formatter, interruption, prefab, main_path, map_path]}, indent=2), encoding='utf-8')
print(result.stdout + result.stderr)
print('Output:', out)
raise SystemExit(result.returncode)
