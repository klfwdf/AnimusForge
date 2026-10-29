"""Replay exact realm host methods with game/library leaves, plus real CN/EN readers.

This is offline source-span evidence, not a Bannerlord or model session.
"""
from pathlib import Path
import argparse
import hashlib
import json
import re
import subprocess
import sys
from xml.sax.saxutils import escape

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
sys.path.insert(0, str(ROOT / "tools/localization"))
from inventory_untranslated import LEX

p = argparse.ArgumentParser(description=__doc__)
p.add_argument("--run-root", type=Path)
a = p.parse_args()
out = new_run_root(ROOT, "diplomacy-realm-identity", a.run_root)
dotnet = resolve_dotnet(ROOT)
hashes, spans = {}, []


def read(path):
    raw = (ROOT / path).read_bytes()
    hashes[path] = hashlib.sha256(raw).hexdigest()
    return raw.decode("utf-8-sig")


def extract(path, signature):
    text = read(path)
    start = text.index(signature)
    mask = LEX.sub(lambda m: " " * len(m[0]), text)
    opening = mask.index("{", start)
    arrow = mask.find("=>", start, opening)
    if arrow >= 0:
        end = mask.index(";", arrow)
    else:
        depth = 0
        for end in range(opening, len(mask)):
            depth += (mask[end] == "{") - (mask[end] == "}")
            if depth == 0:
                break
    spans.append({"path": path, "symbol": signature, "start": text[:start].count("\n") + 1,
                  "end": text[:end + 1].count("\n") + 1})
    return text[start:end + 1]


domain = "src/modules/AF.Module.Diplomacy/Domain/"
groups = {
    "WorldDiplomacyPromptContractRules": [
        "internal static string ContractText(", "private static class CompatibilityContractText",
        "public static string BuildRealmIdentitySourceText(",
        "public static string BuildRealmInstitutionalVoiceText(string kingdomName, string cultureName, string rulerTitle, string governmentHardFact, string lore, Func"],
    "WorldDiplomacyTextRules": ["public static string CompactPromptFact(", "public static string Limit("],
    "WorldDiplomacyRoundLifecycleRules": ["public static string FirstNonEmpty("],
    "WorldDiplomacyGenerationValidationRules": ["public static bool TryGetRealmIdentityViolation("],
}
headers = "using System;using System.Linq;using System.Collections.Generic;using System.Text;using System.Text.RegularExpressions;using Newtonsoft.Json;using AnimusForge;\n"
for name, methods in groups.items():
    (out / (name + ".cs")).write_text(headers + "namespace AnimusForge.Refactor.Domain {public static class " + name + " {" +
        "\n".join(extract(domain + name + ".cs", signature) for signature in methods) + "}}", encoding="utf-8")

host_path = "src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs"
host = read(host_path)
fields = "\n".join(line.strip() for line in host.splitlines() if re.match(
    r"\s*private .* _realmInstitutionalVoice(?:Cache|RuleVersion|Library)[ ;=]", line))
assert len(fields.splitlines()) == 3
methods = ["private string BuildRealmInstitutionalVoiceContext(", "private static string ResolveRealmRulerTitle(",
           "private static string BuildCanonicalRealmGovernmentHardFact("]
(out / "Host.cs").write_text(headers + "using AnimusForge.Refactor.Domain; partial class Host {" + fields + "\n" +
    "\n".join(extract(host_path, signature) for signature in methods) + "}", encoding="utf-8")
for name in ["DiplomacyPromptTexts", "DiplomacyCompatibilityTexts", "DiplomacyMiscPromptTexts"]:
    path = "src/modules/AF.Module.Diplomacy/Presentation/" + name + ".cs"
    (out / (name + ".cs")).write_text(read(path), encoding="utf-8")

# Assert the actual consumers share the source, including retry and oral validation.
orch = read("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOrchestration.cs")
assert "_host.RealmRulerTitle(authorId), _host.RealmRulerDisplayName(authorId)" in orch
assert "TryGetRealmIdentityViolation(item.ActorKingdomId, visible, out reason)" in read(
    "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOrchestration.OralGeneration.cs")
assert "string title = ResolveRealmRulerTitle(kingdom, ruler);" in host
assert "BuildCanonicalRealmGovernmentHardFact(Resolve(author)" in read(
    "src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.PromptContext.cs")
composer = read("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyPromptComposer.cs")
assert "world.RealmInstitutionalVoiceContext(authorId)" in composer
assert "private const int DiplomacyPromptContractVersion = 32;" in host
assert 'CanonicalHistoryCacheAffinityKey = "diplomacy-history:v33"' in read(domain + "WorldDiplomacyPromptContractRules.cs")

base = "content/modules/AF.Module.Diplomacy/ModuleData/"
for language in ["CN", "EN"]:
    package = out / language
    package.mkdir()
    for name in ["DiplomacyPromptTexts", "DiplomacyMiscPromptTexts", "DiplomacyMiscCompatibility"]:
        suffix = ".en" if language == "EN" and name != "DiplomacyMiscCompatibility" else ""
        (package / (name + ".json")).write_text(read(base + name + suffix + ".json"), encoding="utf-8")
    values = json.loads((package / "DiplomacyPromptTexts.json").read_text(encoding="utf-8"))
    for key in ["RealmIdentity_NoTitle", "RealmIdentity_GameTitle", "AF_DIP_Contract_104", "AF_DIP_Contract_105",
                "AF_DIP_Contract_089", "AF_DIP_Contract_090", "AF_DIP_Prompt_Composer_170"]:
        assert values[key]
        if language == "EN":
            assert not re.search(r"[\u4e00-\u9fff]", values[key])
    assert all("RulerVoicePrompt_%03d" % i not in values for i in range(6, 16))
    for filename in [domain + "WorldDiplomacyPromptContractRules.cs",
                     "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyPromptComposer.cs"]:
        for key, literal in re.findall(r'(?:ContractText\(localize,|world.LocalizePrompt\()\s*"([^"]+)",\s*("(?:\\.|[^"\\])*")', read(filename)):
            if language == "EN" and key in values:
                assert json.loads(literal) == values[key], key + ": fallback/resource mismatch"

(out / "Program.cs").write_bytes((HERE / "Program.cs.txt").read_bytes())
json_dll = sorted((dotnet.parent / "sdk").glob("*/Newtonsoft.Json.dll"))[-1]
(out / "Replay.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>' + escape(str(json_dll)) + '</HintPath></Reference></ItemGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding="utf-8")
env = minimal_test_environment(dotnet, out)
commands = [("build", [str(dotnet), "build", str(out / "Replay.csproj"), "--nologo", "-v:q"])] + [
    (language, [str(dotnet), str(out / "bin/Debug/net8.0/Replay.dll"), str(out / language)]) for language in ["CN", "EN"]]
for label, command in commands:
    result = subprocess.run(command, cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120)
    (out / (label + ".log")).write_text(result.stdout + result.stderr, encoding="utf-8")
    print(label + ": " + (result.stdout + result.stderr).strip())
    if result.returncode:
        raise SystemExit(result.returncode)
assert all(hashlib.sha256((ROOT / path).read_bytes()).hexdigest() == digest for path, digest in hashes.items())
(out / "inputs.json").write_text(json.dumps({"hashes": hashes, "spans": spans}, indent=2), encoding="utf-8")
print("PASS source/consumer/resource checks; game and LLM NOT_RUN; output=" + str(out))
