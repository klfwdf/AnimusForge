"""Replay production entity-context formatting with real package text resources."""
from pathlib import Path
import argparse
import importlib.util
import json
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

spec = importlib.util.spec_from_file_location("extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
parser = argparse.ArgumentParser()
parser.add_argument("--run-root", type=Path)
parser.add_argument("--line-source-file", type=Path, help="Use a historical visible-party line formatter as a behavioral negative control")
args = parser.parse_args()
out = new_run_root(ROOT, "knowledge-world-entity-prompts", args.run_root)
dotnet = resolve_dotnet(ROOT)
source = (ROOT / "src/modules/AF.Module.Knowledge/Entities/WorldEntityRetrievalService.cs").read_text(encoding="utf-8-sig")
resources_root = ROOT / "content/modules/AF.Module.Knowledge/ModuleData"
chinese = json.loads((resources_root / "WorldEntityPromptTexts.json").read_text(encoding="utf-8"))
english = json.loads((resources_root / "WorldEntityPromptTexts.en.json").read_text(encoding="utf-8"))
assert set(chinese) == set(english)
library_source = (ROOT / "src/modules/AF.Module.Knowledge/Host/KnowledgeLibraryBehavior.cs").read_text(encoding="utf-8-sig")
calls = re.findall(r'WorldEntityPromptTexts.Get\("([A-Za-z0-9_]+)", ("(?:\\.|[^"\\])*")\)', source + library_source)
dynamic = {key for key in english if key.startswith(("Occupation_", "VillageState_"))}
assert set(key for key, _ in calls) | dynamic == set(english)
for key, fallback in calls:
    assert json.loads(fallback) == english[key], key
    assert not re.search(r'[\u3400-\u9fff]', english[key]), key
    assert re.findall(r'\d+', chinese[key]) == re.findall(r'\d+', english[key]), key
markers = (
    "internal sealed class VisiblePartyCandidate",
    "private static string StripEntityIdsFromMainPromptBlock(",
    "private static string BuildVisiblePartyPromptLine(",
    "private static string BuildVisiblePartyArmyInfo(",
    "private static string BuildVisiblePartyRelationPromptSegment(",
    "private static string FormatPostprocessEntityId(",
    "private static string FormatRelationBand(",
    "private static string FormatSettlementPopulation(",
    "private static string FormatDirection(",
    "private static string FormatDistance(",
    "private static string SafeName(",
    "private static string SafePromptName(",
    "private static string FormatAge(",
    "private static string FormatGender(",
    "private static string FormatBool(",
    "private static string FormatHeroOccupation(",
    "private static string FormatSettlementStatus(",
    "private static string FormatEliminatedStatus(",
    "private static string FormatFloat(",
    "private static string FormatInt(",
    "private static string FormatScore(",
)
line_source = args.line_source_file.read_text(encoding="utf-8-sig") if args.line_source_file else source
methods = "\n".join(extract.declaration(line_source if marker == "private static string BuildVisiblePartyPromptLine(" else source, marker) for marker in markers)
capture = extract.declaration(source, "private static List<VisiblePartyCandidate> BuildVisiblePartyCandidates(")
assert "Dictionary<Army, string> armyInfoCache = new Dictionary<Army, string>()" in capture
assert "ArmyInfo = BuildVisiblePartyArmyInfo(party, armyInfoCache)" in capture
(out / "Production.cs").write_text("using System;using System.Collections.Generic;using System.Globalization;using System.Text.RegularExpressions;using System.Linq;namespace AnimusForge {internal static partial class WorldEntityRetrievalService {" + methods + "}}", encoding="utf-8")
(out / "KnowledgeSourceTexts.cs").write_bytes((ROOT / "src/modules/AF.Module.Knowledge/Host/KnowledgeSourceTexts.cs").read_bytes())
(out / "WorldEntityPromptTexts.cs").write_bytes((ROOT / "src/modules/AF.Module.Knowledge/Entities/WorldEntityPromptTexts.cs").read_bytes())
(out / "Program.cs").write_bytes((HERE / "Harness.cs.txt").read_bytes())
newtonsoft = dotnet.parent / "sdk/8.0.425/Newtonsoft.Json.dll"
(out / "Proof.csproj").write_text(f'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup><EmbeddedResource Include="{(resources_root / "KnowledgeSourceTexts.json").as_posix()}" LogicalName="AnimusForge.Knowledge.SourceTexts.json"/><Reference Include="Newtonsoft.Json"><HintPath>{newtonsoft.as_posix()}</HintPath></Reference></ItemGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text("<configuration><packageSources><clear/></packageSources></configuration>", encoding="utf-8")
environment = minimal_test_environment(dotnet, out)
build = subprocess.run([str(dotnet), "build", str(out / "Proof.csproj"), "-c", "Release", "--nologo", "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], cwd=out, env=environment, capture_output=True, text=True, encoding="utf-8", errors="replace")
(out / "build.log").write_text(build.stdout + build.stderr, encoding="utf-8")
if build.returncode:
    print(build.stdout + build.stderr)
    raise SystemExit(build.returncode)
for language, filename in (("zh-CN", "WorldEntityPromptTexts.json"), ("en", "WorldEntityPromptTexts.en.json")):
    package = out / language
    package.mkdir()
    (package / "WorldEntityPromptTexts.json").write_bytes((ROOT / "content/modules/AF.Module.Knowledge/ModuleData" / filename).read_bytes())
    result = subprocess.run([str(dotnet), str(out / "bin/Release/net8.0/Proof.dll"), str(package), language], cwd=out, env=environment, capture_output=True, text=True, encoding="utf-8", errors="replace")
    (out / (language + ".log")).write_text(result.stdout + result.stderr, encoding="utf-8")
    print(result.stdout + result.stderr, end="")
    if result.returncode:
        raise SystemExit(result.returncode)
print("Evidence:", out)
