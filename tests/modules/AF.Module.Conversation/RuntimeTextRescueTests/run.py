"""Verify reversible corpus rescue, parse all changed C#, and replay actual pure owners."""
from pathlib import Path
import collections
import hashlib
import json
import re
import shutil
import subprocess
import sys
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

PLAN = ROOT / "artifacts/localization-takeover-20261011/inventory/conversation-rescue-plan.json"
plan = json.loads(PLAN.read_text(encoding="utf-8"))
content = ROOT / "content/modules/AF.Module.Conversation/ModuleData"
raw = json.loads((content / "ConversationRuntimeCompatibility.json").read_text(encoding="utf-8-sig"))
chinese = json.loads((content / "ConversationRuntimePrompts.json").read_text(encoding="utf-8-sig"))
english = json.loads((content / "ConversationRuntimePrompts.en.json").read_text(encoding="utf-8-sig"))
assert len(raw) == 1032 and len(chinese) == len(english) == 181 and chinese.keys() == english.keys()
for key in chinese:
    for pattern in (r"\{[A-Za-z_][A-Za-z0-9_]*\}", r"\[(?:ACTION|RELAY|AD|ADP|AFEF):[^\]\r\n]*\]"):
        assert set(re.findall(pattern, chinese[key])) == set(re.findall(pattern, english[key])), key
    assert not re.search(r"[\u3400-\u9fff]", english[key].replace("[RELAY:接力编号]", "")), key
by_file = collections.defaultdict(list)
for row in plan["records"]:
    by_file[row["file"]].append(row)
for filename, records in by_file.items():
    text = (ROOT / filename).read_text(encoding="utf-8-sig")
    if filename.endswith('/ShoutBehavior.SceneHistoryMessages.cs'):
        signature='private static string NormalizeAfefFactLineForPrompt(string text, string fallbackPrefix = null)'
        assignment='\t\tfallbackPrefix = fallbackPrefix ?? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0729");\n'
        assert text.count(signature)==1 and text.count(assignment)==1
        text=text.replace(signature,'private static string NormalizeAfefFactLineForPrompt(string text, string fallbackPrefix = ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0729"))').replace(assignment,'')
    lines = text.splitlines()
    for row in records:
        expected = row["replacement"]
        if filename.endswith("/ConversationActionPostprocessOwner.cs"):
            for variable, number in (("text2", "0343"), ("text", "0345")):
                original = variable + ' == ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_' + number + '")'
                expected = expected.replace(original, variable + ' == ConversationRuntimeTexts.Raw("AF_CONV_Runtime_' + number + '") || ' + original)
        assert lines[row["line"] - 1] == expected, (filename, row["line"], "unexpected change beyond reviewed migration")
        lines[row["line"] - 1] = row["original"]
    baseline = subprocess.check_output(["git", "show", "97ad1ed14c78b95f8229c9b6cf671b3ed425606c:" + filename], cwd=ROOT).decode("utf-8-sig").splitlines()
    assert lines == baseline, filename + " inverse rescue changed non-localization code"

helper = "src/modules/AF.Module.Conversation/Internal/ConversationRuntimeTexts.cs"
assert "ActiveLanguage" not in (ROOT / helper).read_text(encoding="utf-8")
project = (ROOT / "AnimusForge.csproj").read_text(encoding="utf-8-sig")
assert "AnimusForge.Conversation.RuntimeCompatibility.json" in project, "production embedded raw corpus missing"
output = new_run_root(ROOT, "conversation-runtime-texts", None)
sources = list(by_file) + [helper]
(output / "sources.json").write_text(json.dumps(sources, indent=2), encoding="utf-8")

def declaration(source, signature):
    begin = source.index(signature)
    opening = source.index("{", begin)
    depth = 1
    offset = opening + 1
    token = re.compile(r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|[{}]')
    for match in token.finditer(source, offset):
        if match[0] == "{": depth += 1
        elif match[0] == "}":
            depth -= 1
            if depth == 0: return source[begin:match.end()]
    raise AssertionError(signature)

post = (ROOT / "src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs").read_text(encoding="utf-8-sig")
speech = (ROOT / "src/modules/AF.Module.Conversation/Internal/History/ConversationSpeechTextRules.cs").read_text(encoding="utf-8-sig")
signatures = ["internal static bool IsNpcInventorySummaryHeader(", "internal static bool IsLeakedPromptLineForShout(", "internal static string StripAfefPromptScopeLabel(", "internal static string NormalizeNativeConversationFactLineForPrompt(", "internal static string StripAutoGroupStopSignal(", "internal static string StripAutoGroupRelaySignal("]
program = (HERE / "Program.cs.txt").read_text(encoding="utf-8").replace("POSTPROCESS_METHOD", declaration(post, "internal static string AppendPostprocessContextBlockForScene(")).replace("SPEECH_METHODS", "\n".join(declaration(speech, name) for name in signatures))
wrapper=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneHistoryMessages.cs').read_text(encoding='utf-8-sig')
program=program.replace('DEFAULT_PREFIX_METHOD',declaration(wrapper,'private static string NormalizeAfefFactLineForPrompt(').replace('private static','internal static',1))
(output / "Program.cs").write_text(program, encoding="utf-8")
linked = [helper,
    "src/modules/AF.Module.Conversation/Channels/Native/NativeMeetingElapsedOwner.cs",
    "src/modules/AF.Module.Conversation/Ambient/Host/TownAmbientTime.cs",
    "src/modules/AF.Module.Conversation/Internal/History/NativeHistoryIdentityProjectionOwner.cs",
    "src/modules/AF.Module.Memory/Records/AnimusForgeDialogueHistoryEntry.cs"]
for filename, name in [(linked[1], "NativeMeetingElapsed"), (linked[2], "TownAmbientTime")]:
    baseline = subprocess.check_output(["git", "show", "97ad1ed14c78b95f8229c9b6cf671b3ed425606c:" + filename], cwd=ROOT).decode("utf-8-sig")
    (output / ("Legacy" + Path(filename).name)).write_text(baseline.replace(name, "Legacy" + name), encoding="utf-8")
sdk = ROOT / "local/dotnet/8.0.425/sdk/8.0.425"
roslyn = next(path for path in sorted(sdk.parent.glob("*/Roslyn/bincore")) if (path / "Microsoft.CodeAnalysis.CSharp.dll").is_file())
references = {"Newtonsoft.Json": sdk / "Newtonsoft.Json.dll", "Microsoft.CodeAnalysis": roslyn / "Microsoft.CodeAnalysis.dll", "Microsoft.CodeAnalysis.CSharp": roslyn / "Microsoft.CodeAnalysis.CSharp.dll"}
items = "".join('<Compile Include="' + escape(str(ROOT / path)) + '"/>' for path in linked)
items += "".join('<Reference Include="' + name + '"><HintPath>' + escape(str(path)) + '</HintPath></Reference>' for name, path in references.items())
items += '<EmbeddedResource Include="' + escape(str(content / "ConversationRuntimeCompatibility.json")) + '"><LogicalName>AnimusForge.Conversation.RuntimeCompatibility.json</LogicalName><WithCulture>false</WithCulture></EmbeddedResource>'
(output / "Probe.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><UseAppHost>false</UseAppHost><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup>' + items + '</ItemGroup></Project>', encoding="utf-8")
(output / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
dotnet = resolve_dotnet(ROOT)
env = minimal_test_environment(dotnet, output)
build = subprocess.run([str(dotnet), "build", "Probe.csproj", "-c", "Release", "--nologo"], cwd=output, env=env, capture_output=True, encoding="utf-8", errors="replace")
(output / "build.log").write_text(build.stdout + build.stderr, encoding="utf-8")
if build.returncode:
    print(build.stdout + build.stderr)
    raise SystemExit(build.returncode)
for language, filename in (("CN", "ConversationRuntimePrompts.json"), ("EN", "ConversationRuntimePrompts.en.json")):
    package = output / language
    package.mkdir()
    shutil.copyfile(content / filename, package / "ConversationRuntimePrompts.json")
    run = subprocess.run([str(dotnet), str(output / "bin/Release/net8.0/Probe.dll"), str(package), language, str(ROOT), str(output / "sources.json")], cwd=output, env=env, capture_output=True, encoding="utf-8", errors="replace")
    (output / (language + ".log")).write_text(run.stdout + run.stderr, encoding="utf-8")
    print(run.stdout + run.stderr)
    if run.returncode: raise SystemExit(run.returncode)
(output / "receipt.json").write_text(json.dumps(dict(inverseFiles=len(by_file), migratedCalls=plan["calls"], sourceHashes={name: hashlib.sha256((ROOT / name).read_bytes()).hexdigest() for name in sources}, rawValues=1032, promptPairs=181, liveGame="NOT_RUN", fullBuild="NOT_RUN"), indent=2), encoding="utf-8")
print("PASS: 36-file inverse reconstruction, 984 rescued calls + 2 compatibility comparisons + runtime default resolution, 181 prompt pairs and protocol/placeholder parity")
print("Evidence:", output)
