"""Run the five bilingual parser regressions against exact production method spans.

True resource readers are compiled. Module paths and the policy branch's two
fields are fixture leaves; this does not run Bannerlord, a model, or gameplay.
"""
from pathlib import Path
import argparse
import hashlib
import json
import subprocess
import sys
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
sys.path.insert(0, str(ROOT / "tools/localization"))
from inventory_untranslated import LEX

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--run-root", type=Path)
parser.add_argument("--source-ref")
parser.add_argument("--dotnet")
parser.add_argument("--newtonsoft", type=Path)
args = parser.parse_args()
out = new_run_root(ROOT, "bilingual-parser-regressions", args.run_root)
sdk = resolve_dotnet(ROOT, args.dotnet)
json_dll = args.newtonsoft or (Path.home() / ".nuget/packages/newtonsoft.json/13.0.3/lib/net6.0/Newtonsoft.Json.dll")
if not json_dll.is_file():
    raise SystemExit("Provide --newtonsoft with the existing net6/netstandard Newtonsoft assembly.")
hashes, spans = {}, []


def read(path):
    data = (subprocess.check_output(["git", "show", args.source_ref + ":" + path], cwd=ROOT)
            if args.source_ref else (ROOT / path).read_bytes())
    hashes[path] = hashlib.sha256(data).hexdigest()
    return data.decode("utf-8-sig")


def extract(path, signature):
    source = read(path)
    start = source.index(signature)
    mask = LEX.sub(lambda m: " " * len(m[0]), source)
    opening = mask.index("{", start)
    depth = 0
    for end in range(opening, len(mask)):
        depth += (mask[end] == "{") - (mask[end] == "}")
        if depth == 0:
            break
    body = source[start:end + 1]
    spans.append({"file": path, "symbol": signature, "startLine": source[:start].count("\n") + 1,
                  "endLine": source[:end + 1].count("\n") + 1,
                  "sha256": hashlib.sha256(body.encode()).hexdigest()})
    return body


headers = "using System;using System.Linq;using System.Collections.Generic;using System.Text;using System.Globalization;using System.Text.RegularExpressions;using Newtonsoft.Json.Linq;using AnimusForge;\n"
domain = "src/modules/AF.Module.Diplomacy/Domain/"
groups = {
    "WorldDiplomacyIntentVocabulary": ["public static string InferTopicCategory(", "public static bool ContainsAny(",
                                      "public static string NormalizeToken(", "public static string NormalizeIntent("],
    "WorldDiplomacyGenerationValidationRules": ["public static bool TryGetPublicPeaceTermsDisclosureViolation(",
                                               "public static bool TryGetRealmIdentityViolation("],
    "WorldDiplomacyDocumentFactRules": ["public static string BuildPeaceKingdomReferencePattern(",
                                       "public static bool ContainsDirectedPeaceTerm("],
    "WorldDiplomacyTextRules": ["public static bool ContainsWholeNumber("],
}
# Optional only for the immutable old-source negative control, never a replacement implementation.
extras = {
    "WorldDiplomacyIntentVocabulary": ["private static class EnglishTopicPatterns"],
    "WorldDiplomacyDocumentFactRules": ["private static bool ContainsEnglishDirectedPeaceTerm(",
                                       "private static string BuildEnglishPeaceKingdomReferencePattern("],
}
for name, signatures in groups.items():
    path = domain + name + ".cs"
    source = read(path)
    signatures = signatures + [s for s in extras.get(name, []) if s in source]
    body = "\n".join(extract(path, sig) for sig in signatures)
    (out / (name + ".cs")).write_text(headers + "namespace AnimusForge.Refactor.Domain {public static class " + name + " {" + body + "}}", encoding="utf-8")

policy = "PolicySystem/Targets/PolicyTargetPlanRouter.cs"
methods = ["private static void ApplyCardinalityFromText(", "private static int ParseRequestedCount(",
           "private static bool HasRequestedCountContext(", "private static bool ContainsAny(",
           "private static string NormalizeText("]
enum = extract("PolicySystem/Targets/PolicyTargetPlan.cs", "internal enum PolicyTargetPlanCardinality")
(out / "Policy.cs").write_text(headers + "namespace AnimusForge.PolicyTargets {" + enum + """
internal sealed class PolicyTargetPlanBranchSaveData {internal PolicyTargetPlanCardinality Cardinality;internal int Limit;}
internal static class PolicyTargetPlanRouter {
""" + "\n".join(extract(policy, sig) for sig in methods) + """
internal static string Replay(string text,PolicyTargetPlanCardinality initial) {
var b=new PolicyTargetPlanBranchSaveData {Cardinality=initial,Limit=1};
ApplyCardinalityFromText(NormalizeText(text),b);return b.Cardinality+":"+b.Limit;}}}
""", encoding="utf-8")

config = "src/modules/AF.Module.Prompt/Configuration/AIConfigHandler.cs"
methods = ["private static bool IsAuxiliarySceneShoutObserverLine(", "private static string NormalizeSemanticText(",
           "private static int GetAuxiliaryHistorySpeakerSearchStart("]
(out / "Auxiliary.cs").write_text(headers + "namespace AnimusForge {internal static class AIConfigHandler {" +
    "\n".join(extract(config, sig) for sig in methods) +
    "internal static bool Replay(string text)=>IsAuxiliarySceneShoutObserverLine(text);}}", encoding="utf-8")
role = "src/modules/AF.Module.Prompt/Composition/ConversationRoleClassificationOwner.cs"
(out / "Role.cs").write_text(headers + "namespace AnimusForge {internal static class ConversationRoleClassificationOwner {" +
    "\n".join(extract(role, sig) for sig in ["internal static bool TryParseDailyMemorySceneShoutLine(",
                                          "internal static int FindDialogueHistorySpeakerDelimiter("]) + "}}", encoding="utf-8")

readers = ["src/modules/AF.Module.Diplomacy/Presentation/DiplomacyCompatibilityTexts.cs",
           "src/modules/AF.Module.Prompt/Retrieval/PromptRetrievalTexts.cs",
           "src/modules/AF.Module.Prompt/Runtime/PromptComposerTexts.cs",
           "PolicySystem/Core/PolicyCompatibilityTexts.cs"]
for path in readers:
    (out / Path(path).name).write_text(read(path), encoding="utf-8")
resources = {
    "AnimusForge.Prompt.RetrievalCompatibility.json": "content/modules/AF.Module.Prompt/ModuleData/PromptRetrievalCompatibility.json",
    "AnimusForge.Prompt.Compatibility.json": "content/modules/AF.Module.Prompt/ModuleData/PromptCompatibility.json",
}
embedded = []
for name, path in resources.items():
    dest = out / name
    dest.write_text(read(path), encoding="utf-8")
    embedded.append('<EmbeddedResource Include="' + escape(str(dest)) + '" LogicalName="' + name + '" />')
for language in ["CN", "EN"]:
    package = out / language
    package.mkdir()
    for filename, source in {
        "DiplomacyMiscCompatibility.json": "content/modules/AF.Module.Diplomacy/ModuleData/DiplomacyMiscCompatibility.json",
        "PolicyCompatibilityTexts.json": "content/modules/PolicySystem/ModuleData/PolicyCompatibilityTexts.json",
        "PromptRetrievalTexts.json": "content/modules/AF.Module.Prompt/ModuleData/PromptRetrievalTexts" + (".en" if language == "EN" else "") + ".json",
        "PromptComposerTexts.json": "content/modules/AF.Module.Prompt/ModuleData/PromptComposerTexts." + ("en" if language == "EN" else "zh-CN") + ".json",
    }.items():
        (package / filename).write_text(read(source), encoding="utf-8")
(out / "Program.cs").write_text((HERE / "Program.cs.txt").read_text(encoding="utf-8"), encoding="utf-8")
(out / "Replay.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>' + escape(str(json_dll.resolve())) + '</HintPath></Reference>' + "".join(embedded) + '</ItemGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding="utf-8")
env = minimal_test_environment(sdk, out)
build = subprocess.run([str(sdk), "build", str(out / "Replay.csproj"), "--nologo", "-v:q"], cwd=ROOT, env=env,
                       capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120)
(out / "build.log").write_text(build.stdout + build.stderr, encoding="utf-8")
if build.returncode:
    print(build.stdout + build.stderr)
    raise SystemExit(build.returncode)
status = 0
for language in ["CN", "EN"]:
    result = subprocess.run([str(sdk), str(out / "bin/Debug/net8.0/Replay.dll"), str(out / language)],
                            cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=45)
    (out / (language + ".log")).write_text(result.stdout + result.stderr, encoding="utf-8")
    print(language + ": " + (result.stdout + result.stderr).strip())
    status |= result.returncode
if not args.source_ref:
    assert all(hashlib.sha256((ROOT / path).read_bytes()).hexdigest() == digest for path, digest in hashes.items()), "Production inputs changed during replay"
(out / "inputs.json").write_text(json.dumps({"sourceRef": args.source_ref or "working-tree", "hashes": hashes, "spans": spans}, ensure_ascii=False, indent=2), encoding="utf-8")
raise SystemExit(status)
