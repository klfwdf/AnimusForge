"""Actual bounded postprocess-history and template methods with real CN/EN readers.
Only game-dependent name/context/settings and module-path ports are fixture-local.
No game, model, executor or fact persistence is exercised.
"""
from pathlib import Path
import argparse, hashlib, json, shutil, subprocess, sys
from xml.sax.saxutils import escape
ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
sys.path.insert(0, str(ROOT / "tools/localization"))
from inventory_untranslated import LEX
parser = argparse.ArgumentParser()
parser.add_argument("--run-root", type=Path)
parser.add_argument("--dotnet")
parser.add_argument("--target-framework", default="net8.0")
parser.add_argument("--captured-trace", type=Path)
args = parser.parse_args()
out = new_run_root(ROOT, "postprocess-history", args.run_root)
sdk = resolve_dotnet(ROOT, args.dotnet)
hashes, spans = {}, []

def read(path):
    raw = (ROOT / path).read_bytes()
    hashes[path] = hashlib.sha256(raw).hexdigest()
    return raw.decode("utf-8-sig")

def extract(path, signature):
    text = read(path)
    mask = LEX.sub(lambda m: " " * len(m.group()), text)
    start = text.index(signature)
    opening = mask.index("{", start)
    depth = 0
    for end in range(opening, len(mask)):
        depth += (mask[end] == "{") - (mask[end] == "}")
        if not depth:
            break
    body = text[start:end + 1]
    spans.append({"path": path, "symbol": signature, "startLine": text[:start].count("\n") + 1,
                  "endLine": text[:end + 1].count("\n") + 1,
                  "sha256": hashlib.sha256(body.encode()).hexdigest()})
    return body

config = "src/modules/AF.Module.Prompt/Configuration/AIConfigHandler.cs"
signatures = ['private sealed class ActionPostprocessHistoryEntry', 'private static string PrepareActionPostprocessHistoryText(string historyText, int maxEntries, string latestReplyBlock)', 'private static void SelectActionPostprocessHistoryEntries(', 'private static List<ActionPostprocessHistoryEntry> BuildActionPostprocessHistoryEntries(', 'private static bool TryParseActionPostprocessAfefHeaderLine(', 'private static bool TryParseActionPostprocessRoleMarker(', 'private static void FlushActionPostprocessRoleHistoryEntry(', 'private static string BuildActionPostprocessRoleHistoryEntry(', 'private static string ExtractActionPostprocessPlayerTextFromRoleContent(', 'private static int FindLastActionPostprocessDirectSpeechLabelEnd(', 'private static string ExtractActionPostprocessAssistantTextFromRoleContent(', 'private static bool IsActionPostprocessRecallBlockStart(', 'private static bool IsActionPostprocessRecallBlockEnd(', 'private static bool ShouldSkipActionPostprocessHistoryLine(', 'private static string NormalizeActionPostprocessHistoryLine(', 'private static string NormalizeActionPostprocessAfefHistoryLine(', 'private static bool IsActionPostprocessHistoryEntryStart(', 'private static HashSet<string> BuildActionPostprocessLatestReplyEntryKeys(', 'private static string BuildActionPostprocessHistoryEntryKey(', 'private static string NormalizeActionPostprocessHistoryContent(', 'private static string NormalizeActionPostprocessDialogueText(', 'private static string ExtractLatestPlayerUtteranceForActionPostprocess(']
signatures += ["public static string BuildActionPostprocessUserPrompt(",
               "private static string NormalizeActionPostprocessOptionalValue(",
               "private static string ReplaceActionPostprocessOptionalSection("]
methods = "\n".join(extract(config, s) for s in signatures)
ports = """
internal static string Replay(string history,int cap,string latest=null)=>PrepareActionPostprocessHistoryText(history,cap,latest);
private static string NormalizeActionPostprocessNameReferences(string s,string name)=>s;
private static string AppendPersistentActionPostprocessRuntimeContext(string s)=>s;
}
internal static class DuelSettings { internal static int Cap=100; internal static int GetActionPostprocessHistoryEntryLimitForExternal()=>Cap; }
internal static class AnimusForgeModulePaths { internal static string Directory; internal static string GetModuleDataFilePath(string name)=>Path.Combine(Directory,name); }
"""
(out / "ActualHistory.cs").write_text("using System;using System.IO;using System.Collections.Generic;using System.Linq;using System.Text;using System.Text.RegularExpressions;namespace AnimusForge {internal static class AIConfigHandler {" + methods + ports + "}", encoding="utf-8")
for name, path, signature in [
    ("ShoutUtils", "src/modules/AF.Module.Conversation/Channels/Scene/ShoutUtils.cs", "public static string StripConversationMetadataPrefix("),
    ("ConversationSpeechTextRules", "src/modules/AF.Module.Conversation/Internal/History/ConversationSpeechTextRules.cs", "internal static string StripAfefPromptScopeLabel(")]:
    (out / (name + ".cs")).write_text("using System;using System.Text.RegularExpressions;namespace AnimusForge {internal static class " + name + " {" + extract(path, signature) + "}}", encoding="utf-8")
items = []
for path in ["src/modules/AF.Module.Prompt/Runtime/PromptComposerTexts.cs",
             "src/modules/AF.Module.Prompt/Retrieval/PromptRetrievalTexts.cs",
             "src/modules/AF.Module.Conversation/Internal/ConversationRuntimeTexts.cs"]:
    (out / Path(path).name).write_text(read(path), encoding="utf-8")
for path, logical in [
    ("content/modules/AF.Module.Prompt/ModuleData/PromptCompatibility.json", "AnimusForge.Prompt.Compatibility.json"),
    ("content/modules/AF.Module.Prompt/ModuleData/PromptRetrievalCompatibility.json", "AnimusForge.Prompt.RetrievalCompatibility.json"),
    ("content/modules/AF.Module.Conversation/ModuleData/ConversationRuntimeCompatibility.json", "AnimusForge.Conversation.RuntimeCompatibility.json")]:
    read(path)
    items.append('<EmbeddedResource Include="' + escape(str(ROOT / path)) + '" LogicalName="' + logical + '" />')
for lang, suffix in [("CN", "zh-CN"), ("EN", "en")]:
    pack = out / lang
    pack.mkdir()
    path = "content/modules/AF.Module.Prompt/ModuleData/PromptComposerTexts." + suffix + ".json"
    read(path)
    shutil.copyfile(ROOT / path, pack / "PromptComposerTexts.json")
    path = "content/modules/AF.Module.Prompt/ModuleData/PromptRetrievalTexts" + (".en" if lang == "EN" else "") + ".json"
    read(path)
    shutil.copyfile(ROOT / path, pack / "PromptRetrievalTexts.json")
shutil.copyfile(HERE / "Program.cs.txt", out / "Program.cs")
major = args.target_framework.removeprefix("net").split(".")[0]
json_candidates = sorted((sdk.parent / "sdk").glob(major + ".*/Newtonsoft.Json.dll"))
assert json_candidates, "Missing existing SDK Newtonsoft.Json.dll"
newtonsoft = json_candidates[-1]
hashes[str(newtonsoft)] = hashlib.sha256(newtonsoft.read_bytes()).hexdigest()
items.append('<Reference Include="Newtonsoft.Json"><HintPath>' + escape(str(newtonsoft)) + '</HintPath></Reference>')
(out / "Tests.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>' + escape(args.target_framework) + '</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup><ItemGroup>' + "".join(items) + '</ItemGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding="utf-8")
env = minimal_test_environment(sdk, out)
build = subprocess.run([str(sdk), "build", str(out / "Tests.csproj"), "-c", "Release", "--nologo", "-v:q"], cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120)
(out / "build.log").write_text(build.stdout + build.stderr, encoding="utf-8")
if build.returncode:
    print(build.stdout + build.stderr)
    raise SystemExit(build.returncode)
runs = []
for lang in ["CN", "EN"]:
    command = [str(sdk), str(out / ("bin/Release/" + args.target_framework + "/Tests.dll")), lang, str(out), str(out / (lang + "-results.json"))]
    if args.captured_trace:
        trace = args.captured_trace.resolve(strict=True)
        if not trace.is_relative_to(ROOT):
            raise SystemExit("Captured trace must be repository-local")
        hashes[str(trace)] = hashlib.sha256(trace.read_bytes()).hexdigest()
        command.append(str(trace))
    result = subprocess.run(command, cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=30)
    (out / (lang + "-run.log")).write_text(result.stdout + result.stderr, encoding="utf-8")
    print(result.stdout + result.stderr)
    runs.append({"language": lang, "exitCode": result.returncode})
(out / "receipt.json").write_text(json.dumps({"sourceHashes": hashes, "extractedUnmodifiedSpans": spans, "buildExitCode": build.returncode, "runs": runs, "boundary": __doc__}, indent=2), encoding="utf-8")
print("OUTPUT", out)
raise SystemExit(1 if any(r["exitCode"] for r in runs) else 0)
