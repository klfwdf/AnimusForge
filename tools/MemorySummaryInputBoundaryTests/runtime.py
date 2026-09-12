"""Run production capture, worker, parser and queue acceptance with controlled game/provider boundaries."""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import re
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).parent
parser = argparse.ArgumentParser()
parser.add_argument('--framework', choices=['net8.0', 'net472'], default='net8.0')
parser.add_argument('--mutate', choices=['ignore-content', 'ignore-identity', 'reuse-draft', 'skip-retry-guard', 'skip-accept-guard', 'live-name'])
args = parser.parse_args()
spec = importlib.util.spec_from_file_location('extract', ROOT / 'tools/ChannelCutoverBoundaryTests/run.py')
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
source = (ROOT / 'MyBehavior.cs').read_text(encoding='utf-8-sig')
names = ['DailyMemoryLine', 'DailyMemoryDraft', 'CompressedMemoryBlock', 'WeeklyMemoryMaterialTrigger',
         'MemorySummaryJob', 'MemorySummaryExecutionResult', 'MajorActionSummaryJob', 'MajorActionSummaryState',
         'MajorActionSummaryExecutionResult', 'MemoryOverviewJob', 'MemoryOverviewState', 'MemoryOverviewExecutionResult',
         'DailySummaryQueueResult', 'NpcActionEntry']
models = '\n'.join(extract.declaration(source, 'private ' + ('class ' if n == 'NpcActionEntry' else 'sealed class ') + n + '\n') for n in names)
method_names = ['ProcessMemorySummaryQueueAsync', 'RunDailySummaryQueueItemsAsync', 'ExecuteDailySummaryQueueItemAsync',
                'ExecuteMemorySummaryJobAsync', 'ExecuteMajorActionSummaryJobAsync', 'ExecuteMemoryOverviewJobAsync',
                'TryParseMemorySummaryResponse', 'TryParseMajorActionSummaryResponse', 'TryParseMemoryOverviewResponse',
                'FindMemoryDraft', 'NormalizeMemoryHeroId', 'IsNonHeroMemoryId', 'GetMajorActionMaxCursor', 'IsNpcActionAfterSummaryCursor',
                'BuildCompressedMemoryBlockId', 'BuildDailyMemoryLineForPrompt', 'ResolveMemoryLineSceneForPrompt',
                'CountDailyMemorySummarySourceChars', 'BuildMemorySummarySystemPrompt', 'BuildMemorySummaryUserPrompt',
                'BuildCompressionWritingRequirementsPromptSection', 'BuildMemoryOverviewSummarySystemPrompt', 'BuildMemoryOverviewSummaryUserPrompt',
                'BuildMemoryOverviewBlockSourceText', 'BuildMajorActionSummarySystemPrompt', 'BuildMajorActionSummaryUserPrompt',
                'GetMajorActionSummaryTargetChars', 'MarkMemorySummaryFailure', 'MarkMajorActionSummaryFailure', 'MarkMemoryOverviewFailure']
def method(text, name):
    sig = re.search(r'^\s*(?:private|public|internal) [^\n=;]*?\b' + name + r'\(', text, re.M).group().strip()
    return extract.declaration(text, sig)
methods = '\n'.join(method(source, n) for n in method_names)
# Unchanged parser helpers are copied verbatim as a contiguous source section.
methods += source[source.index('\tprivate static string StripMemoryTitleDateTime('):source.index('\tprivate static string BuildDailyMemoryLineForPrompt(')]
notoriety = (ROOT / 'PlayerNotorietyBehavior.cs').read_text(encoding='utf-8-sig')
name_methods = '\n'.join(method(notoriety, n) for n in ['NormalizeMemoryPublicity', 'NormalizePlayerDisplayName',
    'BuildPlayerHistoryDisplayName', 'BuildPlayerHistoryAnonymousAliases', 'StripPlayerInternalMarkers',
    'RenderPlayerActionTextForPrompt', 'RenderPlayerHistoryTextForPrompt', 'RenderPlayerNamedReference', 'NormalizeLine',
    'BuildPlayerHistoryNameForExternal', 'RenderPlayerHistoryMaterialForExternal'])
code = (HERE / 'Harness.cs.txt').read_text(encoding='utf-8-sig').replace('@@MODELS@@', models).replace('@@METHODS@@', methods).replace('@@NAMES@@', name_methods)
process = method(source, 'ProcessMemorySummaryQueueAsync')
accept = '\n'.join(extract.declaration(process, signature) for signature in [
    'foreach (MajorActionSummaryExecutionResult result2 in majorResults)',
    'foreach (MemoryOverviewExecutionResult result3 in overviewResults)'])
code = code.replace('@@ACCEPT@@', accept)
inputs = (ROOT / 'MyBehavior.MemorySummaryInputs.cs').read_text(encoding='utf-8-sig')
renderer = (ROOT / 'PlayerNotorietyBehavior.MemorySummarySnapshot.cs').read_text(encoding='utf-8-sig')
mutations = {
    'ignore-content': ('inputs', 'return string.Equals(_content, SerializeMemorySummarySource(current), StringComparison.Ordinal);', 'return true;'),
    'ignore-identity': ('inputs', 'if (!ReferenceEquals(_source, current)) return false;', 'if (false) return false;'),
    'reuse-draft': ('inputs', 'DailyMemoryDraft draft = CopyMemorySummarySource(source);', 'DailyMemoryDraft draft = source;'),
    'skip-retry-guard': ('code', 'if (!await RunMemorySummaryMainThreadAsync(runtimeGeneration, () => IsMemorySummaryInputCurrent(input)))', 'if (false)'),
    'skip-accept-guard': ('code', ' || !IsMemorySummaryInputCurrent(', ' || false && !IsMemorySummaryInputCurrent('),
    'live-name': ('code', 'input.RenderPlayerHistory(playerHistoryMaterial)', 'PlayerNotorietyBehavior.RenderPlayerHistoryMaterialForExternal(playerHistoryMaterial)'),
}
if args.mutate:
    target, old, new = mutations[args.mutate]
    value = locals()[target]
    assert old in value
    locals()[target] = value.replace(old, new)
out = HERE / '.generated' / (args.framework + '-' + (args.mutate or 'current'))
out.mkdir(parents=True, exist_ok=True)
for filename, content in [('Program.cs', code), ('Inputs.cs', inputs), ('Renderer.cs', renderer),
                          ('MainThread.cs', (ROOT / 'MyBehavior.MemorySummaryMainThread.cs').read_text(encoding='utf-8-sig')),
                          ('Guard.cs', (ROOT / 'SaveRuntimeGuard.cs').read_text(encoding='utf-8-sig'))]:
    (out / filename).write_text(content, encoding='utf-8')
newtonsoft = Path(os.environ.get('NEWTONSOFT_TEST_DLL', r'C:\Program Files\dotnet\sdk\8.0.421\Newtonsoft.Json.dll'))
if args.framework == 'net472':
    newtonsoft = ROOT / '_deps_auto/Newtonsoft.Json.dll'
shutil.copyfile(newtonsoft, out / 'Newtonsoft.Json.dll')
framework_properties = ''
if args.framework == 'net472':
    assets = json.loads((ROOT / 'obj/project.assets.json').read_text(encoding='utf-8'))
    package_roots = [Path(path) for path in assets['packageFolders']]
    refpath = next(path / 'microsoft.netframework.referenceassemblies.net472/1.0.3/build/.NETFramework/v4.7.2'
        for path in package_roots if (path / 'microsoft.netframework.referenceassemblies.net472/1.0.3').exists())
    from xml.sax.saxutils import escape
    framework_properties = '<AutomaticallyUseReferenceAssemblyPackages>false</AutomaticallyUseReferenceAssemblyPackages><FrameworkPathOverride>' + escape(str(refpath)) + '</FrameworkPathOverride>'
(out / 'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
    '<TargetFramework>' + args.framework + '</TargetFramework><LangVersion>latest</LangVersion><NoWarn>0162;0169;0649;0414</NoWarn>'
    + framework_properties + '</PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>Newtonsoft.Json.dll</HintPath>'
    '</Reference></ItemGroup></Project>', encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
dotnet = os.environ.get('DOTNET_EXE', r'C:\Program Files\dotnet\dotnet.exe')
env = os.environ.copy()
env.update(DOTNET_CLI_HOME=str(ROOT / '.tmp/dotnet-cli'), NUGET_PACKAGES=str(ROOT / '.tmp/nuget-packages'),
           DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1', DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_CLI_UI_LANGUAGE='en',
           DOTNET_GENERATE_ASPNET_CERTIFICATE='false', APPDATA=str(ROOT / '.tmp/appdata'))
result = subprocess.run([dotnet, 'run', '--project', str(out / 'Proof.csproj'), '-c', 'Release',
    '-p:RestoreConfigFile=' + str(out / 'NuGet.Config')], cwd=ROOT, env=env, capture_output=True,
    text=True, encoding='utf-8', errors='replace', timeout=180)
log = result.stdout + result.stderr
(out / 'run.log').write_text(log, encoding='utf-8')
print(log)
raise SystemExit(result.returncode)
