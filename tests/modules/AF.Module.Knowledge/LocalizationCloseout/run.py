"""Replay production Knowledge text boundaries with actual CN/EN resources.

Game objects and TextObject's native lookup are test doubles; readers, request
assembly, cache lifecycle, XML bodies, parsing and fallback methods are real.
"""
from pathlib import Path
import argparse
import importlib.util
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

parser = argparse.ArgumentParser()
parser.add_argument("--run-root", type=Path)
args = parser.parse_args()
out = new_run_root(ROOT, "knowledge-localization-closeout", args.run_root)
dotnet = resolve_dotnet(ROOT)
resources = ROOT / "content/modules/AF.Module.Knowledge/ModuleData"
source_root = ROOT / "src/modules/AF.Module.Knowledge"
source = (source_root / "Host/KnowledgeLibraryBehavior.cs").read_text(encoding="utf-8-sig")

def unique_pairs(pairs):
    result = {}
    for key, value in pairs:
        assert key not in result, "duplicate JSON key: " + key
        result[key] = value
    return result

def read_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"), object_pairs_hook=unique_pairs)

def variables(text):
    return sorted(re.findall(r"\{([A-Z_]+|[0-9]+)\}", text))

for family in ("KnowledgeRagPromptTexts", "WorldEntityPromptTexts"):
    chinese = read_json(resources / (family + ".json"))
    english = read_json(resources / (family + ".en.json"))
    assert chinese.keys() == english.keys(), family
    for key in chinese:
        assert variables(chinese[key]) == variables(english[key]), (family, key)
        assert re.findall(r"\d+", chinese[key]) == re.findall(r"\d+", english[key]), (family, key)
        assert not re.search(r"[\u3400-\u9fff]", english[key]), (family, key)
    assert not re.search(r"answer in Chinese|respond in Chinese|natural Chinese", "\n".join(english.values()), re.I)

catalogs = {}
for language, relative in (("en", "Languages/afknowledge_strings.xml"), ("zh-CN", "Languages/CNs/afknowledge_strings-zh-CN.xml")):
    path = resources / relative
    elements = ET.parse(path).findall(".//string")
    catalogs[language] = unique_pairs((item.attrib["id"], item.attrib["text"]) for item in elements)
    assert not any("\n" in match.group(1) for match in re.finditer(r'text="([^"]*)"', path.read_text(encoding="utf-8-sig"))), "literal newline in XML attribute"
assert catalogs["en"].keys() == catalogs["zh-CN"].keys()
for key in catalogs["en"]:
    assert variables(catalogs["en"][key]) == variables(catalogs["zh-CN"][key]), key
for path in source_root.rglob("*.cs"):
    text = path.read_text(encoding="utf-8-sig")
    for key in re.findall(r'"(AF_KNOWLEDGE_[A-Za-z0-9_]+)"', text):
        # Four prefixes deliberately construct keys from stable mapping kinds.
        if key.endswith("_"):
            continue
        assert key in catalogs["en"], (path.name, key)
    for key, literal in re.findall(r'(?:Ui|Text)\("(AF_KNOWLEDGE_[A-Za-z0-9_]+)",\s*("(?:\\.|[^"\\])*")', text):
        assert json.loads(literal).strip() == catalogs["en"][key].strip(), (path.name, key)

prompt_values = read_json(resources / "KnowledgeRagPromptTexts.en.json")
calls = re.findall(r'KnowledgePromptTexts\.(?:Get|Format)\("([A-Za-z0-9_]+)",\s*("(?:\\.|[^"\\])*")', source)
assert {key for key, _ in calls} <= prompt_values.keys()
# Retain this pre-existing paired catalog entry although the disabled prototype
# editor no longer consumes it. It is explicitly outside live-call coverage.
assert prompt_values.keys() - {key for key, _ in calls} == {"KN_Prompt_052"}
for key, literal in calls:
    assert json.loads(literal) == prompt_values[key], key
compatibility = read_json(resources / "KnowledgeSourceTexts.json")
compat_calls = set()
for path in source_root.rglob("*.cs"):
    compat_calls.update(re.findall(r'KnowledgeSourceTexts.Get\("([A-Za-z0-9_]+)"\)', path.read_text(encoding="utf-8-sig")))
assert compat_calls == compatibility.keys()

# Catalogs cover every actual enum on both supported reference source lines.
occupation_names = []
for path in (
    ROOT / "原版游戏本体代码1.3.x/TaleWorlds.CampaignSystem/Occupation.cs",
    ROOT / "原版游戏本体代码1.4.5/TaleWorlds.CampaignSystem/TaleWorlds/CampaignSystem/Occupation.cs",
):
    names = set(re.findall(r"^\s*([A-Za-z]+),?\s*$", path.read_text(encoding="utf-8-sig"), re.M)) - {"NumberOfOccupations"}
    occupation_names.append(names)
assert occupation_names[0] == occupation_names[1]
world = read_json(resources / "WorldEntityPromptTexts.en.json")
for name in occupation_names[0]:
    assert "Occupation_" + name in world
    assert "AF_KNOWLEDGE_Occupation_" + name in catalogs["en"]

spec = importlib.util.spec_from_file_location("extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
markers = (
    "public class RuleIndexItem", "public class KnowledgeFile", "public class LoreRule",
    "public class LoreVariant", "public class LoreWhen", "public class LoreTextMapping",
    "private void EnsureRuleIndexCache(", "private static bool IsRuleListMatch(",
    "private static string BuildWhenPromptLabel(", "private static string BuildWhenLabel(",
    "private static string BuildRagShortTextGenerationSystemPrompt(",
    "private static string BuildRagShortTextGenerationUserPrompt(",
    "private static List<string> ParseRagShortTextCandidates(",
    "private static string NormalizeRagShortTextCandidate(",
    "private static List<string> BuildDeterministicRagShortTextFallback(",
    "private static string BuildPrototypeGenerationUserPrompt(",
    "private static string NormalizePrototypeText(",
    "private static List<string> BuildDeterministicPrototypeFallback(",
    "private static string BuildPermanentPlayerAppearanceContext(",
    "private static string NormalizePlayerAppearanceForStorage(",
)
declarations = "\n".join(extract.declaration(source, marker) for marker in markers)
for marker in ("private static string BuildRagShortTextGenerationUserPrompt(", "private static string BuildWhenPromptLabel("):
    assert "KnowledgeUiTexts" not in extract.declaration(source, marker), "UI leaked into request"
assert "KnowledgeUiTexts.Occupation(item.Occupation.ToString())" in source
assert 'KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenRoleLabel", "Hero")' not in source
assert 'KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenRoleLabel", "Clan")' not in source
assert "KnowledgeUiTexts.RagFailureDetail(ex.Message)" in source
fields = "\n".join(re.findall(r"^\s*(?:public|private) const int RagShortText[^;]+;", source, re.M))
fields += '\nprivate KnowledgeFile _file = new KnowledgeFile(); private long _ruleIndexCacheVersion = -1; private List<RuleIndexItem> _ruleIndexCache; private string _ruleIndexCacheUiLabelFormat;'
(out / "Production.cs").write_text("using System;using System.Collections.Generic;using System.Linq;using System.Text;using Newtonsoft.Json.Linq;namespace AnimusForge {public partial class KnowledgeLibraryBehavior {" + fields + declarations + "}}", encoding="utf-8")
for name in ("KnowledgeUiTexts", "KnowledgePromptTexts", "KnowledgeSourceTexts"):
    (out / (name + ".cs")).write_bytes((source_root / "Host" / (name + ".cs")).read_bytes())
(out / "Program.cs").write_bytes((HERE / "Harness.cs.txt").read_bytes())
newtonsoft = dotnet.parent / "sdk/8.0.425/Newtonsoft.Json.dll"
(out / "Proof.csproj").write_text(f'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup><EmbeddedResource Include="{(resources / "KnowledgeSourceTexts.json").as_posix()}" LogicalName="AnimusForge.Knowledge.SourceTexts.json"/><Reference Include="Newtonsoft.Json"><HintPath>{newtonsoft.as_posix()}</HintPath></Reference></ItemGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text("<configuration><packageSources><clear/></packageSources></configuration>", encoding="utf-8")
env = minimal_test_environment(dotnet, out)
build = subprocess.run([str(dotnet), "build", str(out / "Proof.csproj"), "-c", "Release", "--nologo", "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
(out / "build.log").write_text(build.stdout + build.stderr, encoding="utf-8")
if build.returncode:
    print(build.stdout + build.stderr)
    raise SystemExit(build.returncode)
for language, filename in (("zh-CN", "KnowledgeRagPromptTexts.json"), ("en", "KnowledgeRagPromptTexts.en.json")):
    package = out / language
    package.mkdir()
    (package / "KnowledgeRagPromptTexts.json").write_bytes((resources / filename).read_bytes())
    for relative in ("Languages/afknowledge_strings.xml", "Languages/CNs/afknowledge_strings-zh-CN.xml"):
        target = package / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes((resources / relative).read_bytes())
    result = subprocess.run([str(dotnet), str(out / "bin/Release/net8.0/Proof.dll"), str(package), language], cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
    (out / (language + ".log")).write_text(result.stdout + result.stderr, encoding="utf-8")
    print(result.stdout + result.stderr, end="")
    if result.returncode:
        raise SystemExit(result.returncode)
print("PASS real resource key/variable/fallback coverage; UI keys=" + str(len(catalogs["en"])) + "; prompt keys=" + str(len(prompt_values)) + "; compatibility keys=" + str(len(compatibility)))
print("Evidence:", out)
