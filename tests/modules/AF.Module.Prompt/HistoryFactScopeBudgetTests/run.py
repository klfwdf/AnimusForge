"""Actual request assembly and history-budget methods; no game/network side effects."""
from pathlib import Path
import argparse, importlib.util, subprocess, sys, shutil, os
from xml.sax.saxutils import escape
ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).parent
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
parser = argparse.ArgumentParser()
parser.add_argument('--out', type=Path, required=True)
parser.add_argument('--target-framework', default='net8.0')
args = parser.parse_args()
out = new_run_root(ROOT, 'history-fact-scope', args.out)
spec = importlib.util.spec_from_file_location('extract', ROOT / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
extract = importlib.util.module_from_spec(spec); spec.loader.exec_module(extract)
source = (ROOT / 'src/modules/AF.Module.Conversation/Internal/History/SceneHistoryProjectionOwner.cs').read_text('utf-8-sig')
markers = ['internal static List<ConversationMessage> CapturePromptMessages', 'internal static void AppendConversationMessages',
           'internal static List<ConversationMessage> SortConversationMessagesByEventSequence',
           'internal static List<ConversationMessage> KeepAfefFactsAndRecentConversationMessages',
           'internal static List<string> KeepAfefFactsAndRecentHistoryLines', 'internal static int HistoricalFactLimit']
import re
constants = '\n'.join(re.findall(r'internal const int MaxHistoricalPrompt\w+ = \d+;', source))
(out / 'Projection.cs').write_text('using System;using System.Linq;using System.Collections.Generic;namespace AnimusForge;internal static class SceneHistoryProjectionOwner{'
    + constants + '\n' + '\n'.join(extract.declaration(source, m) for m in markers) + '}', encoding='utf-8')
shutil.copyfile(HERE / 'Program.cs.txt', out / 'Program.cs')
mode_source = (ROOT / 'src/modules/AF.Module.Weekly/Generation/WorldBulletinStateOwner.cs').read_text('utf-8-sig')
(out / 'NewsMode.cs').write_text('namespace AnimusForge;internal static class NewsMode {' +
    extract.declaration(mode_source, 'internal static bool IsWorldBulletinEnabled(') +
    extract.declaration(mode_source, 'internal static bool IsWorldBulletinPublishingEnabled(') +
    '} internal class DuelSettings {internal static DuelSettings Settings=new();internal bool UseWorldBulletin,AutoGenerateWorldBulletins;internal static DuelSettings GetSettings()=>Settings;}', encoding='utf-8')

paths = ['src/modules/AF.Module.Prompt/Composition/SceneHistoryMessageAssemblyOwner.cs',
         'src/modules/AF.Module.Conversation/Internal/History/ConversationMessage.cs',
         'src/modules/AF.Module.Conversation/Internal/ConversationRuntimeTexts.cs',
         'src/modules/AF.Module.Diplomacy/Presentation/DiplomacyReplyTextRules.cs']
resources = '<EmbeddedResource Include="' + escape(str(ROOT / 'content/modules/AF.Module.Conversation/ModuleData/ConversationRuntimeCompatibility.json')) + '" LogicalName="AnimusForge.Conversation.RuntimeCompatibility.json"/>'
base = ROOT / 'content/modules/AF.Module.Diplomacy/ModuleData'
resources += '<EmbeddedResource Include="' + escape(str(base / 'DiplomacyReceiptCompatibility.json')) + '" LogicalName="AnimusForge.Diplomacy.ReceiptCompatibility.json"/>'
newtonsoft = Path(os.environ.get('AF_NEWTONSOFT') or str(ROOT / '.tmp/build_check/1.4/Newtonsoft.Json.dll'))
assert newtonsoft.is_file(), newtonsoft
(out / 'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>' + escape(args.target_framework) + '</TargetFramework><ImplicitUsings>enable</ImplicitUsings><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup>'
    + ''.join('<Compile Include="' + escape(str(ROOT / p)) + '"/>' for p in paths) + resources
    + '<Reference Include="Newtonsoft.Json"><HintPath>' + escape(str(newtonsoft)) + '</HintPath></Reference></ItemGroup></Project>', encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
sdk = resolve_dotnet(ROOT); env = minimal_test_environment(sdk, out)
command = [str(sdk), 'build', str(out / 'Test.csproj'), '--nologo']
run = subprocess.run(command, cwd=out, env=env, capture_output=True, text=True, encoding='utf-8', errors='replace')
(out / 'build.log').write_text(run.stdout + run.stderr, encoding='utf-8')
if run.returncode: print(run.stdout + run.stderr); raise SystemExit(run.returncode)
for lang in ['zh-CN', 'en']:
    run = subprocess.run([str(sdk), str(out / ('bin/Debug/' + args.target_framework + '/Test.dll')), str(ROOT), lang], cwd=out, env=env,
                         capture_output=True, text=True, encoding='utf-8', errors='replace')
    (out / (lang + '.log')).write_text(run.stdout + run.stderr, encoding='utf-8')
    print(run.stdout + run.stderr)
    if run.returncode: raise SystemExit(run.returncode)
