"""Compile actual prompt methods and reader; fake only unrelated campaign leaves."""
from pathlib import Path
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

OWNER = "src/modules/AF.Module.WorldMap/Runtime/WorldMapPartyCommandBehavior.cs"
HIDEOUT = "src/modules/AF.Module.WorldMap/Runtime/WorldMapPartyCommandBehavior.HideoutClear.cs"
HELPER = "src/modules/AF.Module.WorldMap/Runtime/WorldMapPromptTexts.cs"
source = (ROOT / OWNER).read_text(encoding="utf-8-sig")
baseline = subprocess.check_output(["git", "show", "97ad1ed14c78b95f8229c9b6cf671b3ed425606c:" + OWNER], cwd=ROOT).decode("utf-8-sig")
hideout = (ROOT / HIDEOUT).read_text(encoding="utf-8-sig")
baseline_hideout = subprocess.check_output(["git", "show", "97ad1ed14c78b95f8229c9b6cf671b3ed425606c:" + HIDEOUT], cwd=ROOT).decode("utf-8-sig")

def method(text, name):
    begin = re.search(r"\t(?:private|public) static " + re.escape(name), text).start()
    end = text.index("\n\t}\n", begin) + len("\n\t}")
    return text[begin:end]

changed_methods = ["string BuildCurrentNpcCommandTasksPromptCore(", "string BuildNaturalCommandSummary(", "string ResolveNaturalCommandTargetName(", "string BuildMergeToPlayerPostprocessDescription("]
unchanged_methods = ["bool IsKind(", "bool IsSettlementTarget(", "string BuildCurrentNpcCommandTasksPromptForExternal(", "List<PostprocessRuleEntry> BuildRuntimePostprocessRulesForExternal(Hero targetHero, CharacterObject", "bool IsMergeToPlayerPostprocessRule(", "PostprocessRuleEntry ClonePostprocessRule("]
# Compatibility calls are allowed only when their exact resource values restore
# the previous algorithm. Unknown keys, changed resource values or other edits fail.
compatibility = json.loads((ROOT / "content/modules/AF.Module.WorldMap/ModuleData/WorldMapCompatibility.json").read_text(encoding="utf-8-sig"))
compatibility_values = {section + "." + key: value for section, entries in compatibility.items() if isinstance(entries, dict) for key, value in entries.items()}
def restore_compatibility(text):
    def literal(match):
        return json.dumps(compatibility_values[match.group(1)], ensure_ascii=False)
    return re.sub(r'WorldMapSourceTexts\.Get\("([^"\n]+)"\)', literal, text)
restored_source = restore_compatibility(source)
# A resource lookup is not a compile-time case constant. Preserve order, null
# handling, Trim and the original default return in the equivalent branches.
preempt_name = "string LocalizePreemptReason("
preempt_current = method(restored_source, preempt_name)
preempt_expected = method(baseline, preempt_name)
expected_branches = preempt_expected.replace('switch ((reason ?? "").Trim())\n\t\t{', 'string normalizedReason = (reason ?? "").Trim();').replace('\n\t\t}', '')
expected_branches = re.sub(r'\t\t\tcase ("[^"]+"): return', r'\t\tif (normalizedReason == \1) return', expected_branches)
expected_branches = expected_branches.replace('\t\t\tdefault: return reason;', '\t\treturn reason;')
assert preempt_current == expected_branches, "Preempt reason branch semantics changed"
restored_source = restored_source.replace(preempt_current, preempt_expected)
assert source.count("WorldMapSourceTexts.Get(") == 68, "Compatibility consumers unexpectedly lost or added"
normalized_current, normalized_baseline = restored_source, baseline
for name in changed_methods:
    normalized_current = normalized_current.replace(method(restored_source, name), "METHOD")
    normalized_baseline = normalized_baseline.replace(method(baseline, name), "METHOD")
assert normalized_current == normalized_baseline, "Algorithm changes outside the four main-owner prompt methods after exact compatibility restoration"
hideout_name = "string BuildKnownHideoutClearTargetsPrompt("
assert hideout.replace(method(hideout, hideout_name), "METHOD") == baseline_hideout.replace(method(baseline_hideout, hideout_name), "METHOD"), "Changes outside hideout prompt method"
helper = (ROOT / HELPER).read_text(encoding="utf-8")
assert "TextObject" not in helper and "ActiveLanguage" not in helper and "WorldMapUiText" not in "".join(method(source, name) for name in changed_methods)

content = ROOT / "content/modules/AF.Module.WorldMap/ModuleData"
chinese = json.loads((content / "WorldMapCommandPromptTexts.json").read_text(encoding="utf-8"))
english = json.loads((content / "WorldMapCommandPromptTexts.en.json").read_text(encoding="utf-8"))
assert chinese.keys() == english.keys() and len(chinese) == 23
for key in chinese:
    assert set(re.findall(r"\{[A-Za-z_]+\}", chinese[key])) == set(re.findall(r"\{[A-Za-z_]+\}", english[key])), key
assert not re.search(r"[\u3400-\u9fff]", "".join(english.values()))
entries = json.loads((ROOT / "content/content-map.json").read_text(encoding="utf-8"))["entries"]
assert any(entry["source"] == "content/modules/AF.Module.WorldMap/ModuleData/WorldMapCommandPromptTexts.json" and entry["target"] == "ModuleData/WorldMapCommandPromptTexts.json" for entry in entries)
assert any(entry["source"] == "content/modules/AF.Module.WorldMap/ModuleData/WorldMapCompatibility.json" and entry["target"] == "ModuleData/WorldMapCompatibility.json" and entry.get("localizationRole") == "compatibility" for entry in entries), "Stable source values must remain packaged as compatibility content"

output = new_run_root(ROOT, "worldmap-command-prompt", None)
fixture = (HERE / "Fixture.cs.txt").read_text(encoding="utf-8")
for prefix, text in [("", source), ("Legacy", baseline)]:
    methods = "\n".join(method(text, name) for name in changed_methods + unchanged_methods)
    methods += "\n" + method(baseline_hideout if prefix else hideout, hideout_name)
    body = fixture.replace("OWNER_METHODS", methods).replace("OWNER_CLASS", prefix + "WorldMapPartyCommandBehavior")
    if prefix:
        body = body.replace("WorldMapPartyCommandBehavior behavior", "LegacyWorldMapPartyCommandBehavior behavior").replace("GetCampaignBehavior<WorldMapPartyCommandBehavior>", "GetCampaignBehavior<LegacyWorldMapPartyCommandBehavior>")
    (output / (prefix + "Owner.cs")).write_text(body, encoding="utf-8")
(output / "Program.cs").write_text((HERE / "Program.cs.txt").read_text(encoding="utf-8"), encoding="utf-8")
newtonsoft = ROOT / "local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll"
(output / "Probe.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>CS0649</NoWarn><UseAppHost>false</UseAppHost></PropertyGroup><ItemGroup><Compile Include="' + escape(str(ROOT / HELPER)) + '"/><Reference Include="Newtonsoft.Json"><HintPath>' + escape(str(newtonsoft)) + '</HintPath></Reference></ItemGroup></Project>', encoding="utf-8")
(output / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
dotnet = resolve_dotnet(ROOT)
env = minimal_test_environment(dotnet, output)
build = subprocess.run([str(dotnet), "build", "Probe.csproj", "-c", "Release", "--nologo"], cwd=output, env=env, capture_output=True, encoding="utf-8", errors="replace")
(output / "build.log").write_text(build.stdout + build.stderr, encoding="utf-8")
if build.returncode:
    print(build.stdout + build.stderr)
    raise SystemExit(build.returncode)
for language, filename in [("CN", "WorldMapCommandPromptTexts.json"), ("EN", "WorldMapCommandPromptTexts.en.json")]:
    package = output / language
    package.mkdir()
    shutil.copyfile(content / filename, package / "WorldMapCommandPromptTexts.json")
    run = subprocess.run([str(dotnet), str(output / "bin/Release/net8.0/Probe.dll"), str(package), language], cwd=output, env=env, capture_output=True, encoding="utf-8", errors="replace")
    (output / (language + ".log")).write_text(run.stdout + run.stderr, encoding="utf-8")
    print(run.stdout + run.stderr)
    if run.returncode:
        raise SystemExit(run.returncode)
print("PASS: 23 paired keys, placeholder parity, standard package mapping, no UI-language dependency, outside-method algorithm unchanged after exact compatibility restoration")
print("Evidence:", output)

