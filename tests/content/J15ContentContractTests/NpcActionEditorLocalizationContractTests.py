"""Static NPC action editor localization and fact-projection migration evidence.

No C# compiler or game runtime is invoked. Actual lifecycle replay is NOT-RUN.
"""
import json
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
UI = ROOT / "src/AF.GameAdapter.Bannerlord/UI/Editors/NpcActionEditorProjection.cs"
FACT = ROOT / "src/AF.GameAdapter.Bannerlord/Records/NpcActionMaterialPreviewProjection.cs"
PREFIX = "AF_NPCACTION_EDITOR_"
BASELINE = "33a9ba44f"


def method(source, start, end):
    return source[source.index(start):source.index(end, source.index(start))].strip().replace("\r\n", "\n")


def strings(path):
    root = ET.parse(path).getroot()
    assert root.tag == "base" and root.get("type") == "string", f"invalid native resource root: {path}"
    rows = root.findall("strings/string")
    result = {row.attrib["id"]: row.attrib["text"] for row in rows}
    assert len(result) == len(rows), f"duplicate IDs: {path}"
    assert all(value for value in result.values()), f"empty text: {path}"
    return result


ui = UI.read_text(encoding="utf-8-sig")
fact = FACT.read_text(encoding="utf-8-sig")
assert not re.search(r"[\u3400-\u9fff]", ui), "UI projection still contains hardcoded Chinese"
calls = re.findall(r'\bT\("([^"]+)",\s*("(?:\\.|[^"\\])*")', ui)
fallbacks = {PREFIX + key: json.loads(raw) for key, raw in calls}
assert len(fallbacks) == 110, f"unexpected key count: {len(fallbacks)}"
base = ROOT / "content/modules/AF.Module.UI/ModuleData/Languages"
en = strings(base / "afnpcaction_editor_strings.xml")
zh = strings(base / "CNs/afnpcaction_editor_strings-zh-CN.xml")
assert en == fallbacks, "English key/fallback drift or unused resource key"
assert en.keys() == zh.keys(), "asymmetric language keys"
for key in en:
    assert sorted(re.findall(r"\{[A-Z_]+\}", en[key])) == sorted(re.findall(r"\{[A-Z_]+\}", zh[key])), key

baseline = subprocess.check_output(["git", "show", f"{BASELINE}:{UI.relative_to(ROOT).as_posix()}"], cwd=ROOT).decode("utf-8-sig")
old_method = method(baseline, "internal static string BuildDevNpcActionPreviewText(", "internal static string BuildDevNpcActionNarrative(")
new_method = fact[fact.index("internal static string Build("):fact.rfind("}")].strip().replace("\r\n", "\n")
prompt_root = ROOT / "content/foundation/AF.Foundation.Localization/ModuleData"
prompt_cn = json.loads((prompt_root / "AdapterRecordPromptTexts.json").read_text(encoding="utf-8"))
prompt_en = json.loads((prompt_root / "AdapterRecordPromptTexts.en.json").read_text(encoding="utf-8"))
assert prompt_cn.keys() == prompt_en.keys()
action_fact_additions = {
    "ActionFactCompanionJoin", "ActionFactCompanionLeave", "ActionFactGovernorRemoved",
    "ActionFactGovernorAppointed", "ActionFactDuelWin", "ActionFactDuelLoss", "ActionFactQuest",
    "ActionFactVote", "ActionFactVoteOption", "ActionFactCompanionFired",
    "ActionFactQuestResult_0001", "ActionFactQuestResult_0002", "ActionFactQuestResult_0003",
    "ActionFactQuestResult_0004", "ActionFactQuestResult_0005", "ActionFactPrisonRescue",
    "ActionFactPrisonRescueLocation",
}
assert {key for key in prompt_cn if key.startswith("ActionFact")} == action_fact_additions
assert len(prompt_cn) == 82 + len(action_fact_additions), "unexpected package resource additions"
for key in prompt_cn:
    cn_variables = re.findall(r"\{[A-Z_]+\}", prompt_cn[key])
    en_variables = re.findall(r"\{[A-Z_]+\}", prompt_en[key])
    # Named expansion allows package grammar to reorder only these reviewed additions.
    assert (sorted(cn_variables) == sorted(en_variables) if key in action_fact_additions
            else cn_variables == en_variables), key
    assert not re.search(r"[\u3400-\u9fff]", prompt_en[key]), key
assert sorted(re.findall(r"\{[A-Z_]+\}", prompt_cn["ActionFactQuest"])) == ["{ACTOR}", "{FIRST}", "{SECOND}"]
# Exact CN reconstruction protects all original branch conditions, order,
# names and saved-history text, while allowing the actual package grammar.
restored = new_method.replace('AdapterRecordTexts.PromptActionKind(entry.ActionKind, port.TranslateNpcActionKindForPrompt(entry.ActionKind))', 'port.TranslateNpcActionKindForPrompt(entry.ActionKind)')
assert prompt_cn["Result"] == "结果是{OUTCOME}" and prompt_cn["Victory"] == "获胜" and prompt_cn["Defeat"] == "失利"
restored = restored.replace('AdapterRecordTexts.PromptFormat("Result", ("OUTCOME", AdapterRecordTexts.Prompt(entry.Won.Value ? "Victory" : "Defeat")))', '"结果是" + (entry.Won.Value ? "获胜" : "失利")')
def original_expression(match):
    template = prompt_cn[match[1]]
    variables = dict(re.findall(r'\("([A-Z_]+)", (text\d+)\)', match[2]))
    parts = []
    for piece in re.split(r'(\{[A-Z_]+\})', template):
        if not piece: continue
        parts.append(variables[piece[1:-1]] if piece.startswith('{') else json.dumps(piece, ensure_ascii=False))
    return ' + '.join(parts)
restored = re.sub(r'AdapterRecordTexts.PromptFormat\("([^"\n]+)", ((?:\("[A-Z_]+", text\d+\)(?:, )?)+)\)', original_expression, restored)
restored = re.sub(r'AdapterRecordTexts.Prompt\("([^"\n]+)"\)', lambda match: json.dumps(prompt_cn[match[1]], ensure_ascii=False), restored)
assert restored.replace("internal static string Build(", "internal static string BuildDevNpcActionPreviewText(", 1) == old_method, "fact algorithm changed after exact CN resource reconstruction"
reader = (ROOT / "src/AF.GameAdapter.Bannerlord/Records/AdapterRecordTexts.cs").read_text(encoding="utf-8-sig")
assert 'GetModuleDataFilePath("AdapterRecordPromptTexts.json")' in reader and 'Placeholder.Replace(Prompt(id), match =>' in reader
assert 'PromptValues.Value.TryGetValue("ActionKind_" + (actionKind ?? "").Trim().ToLowerInvariant()' in reader
assert '? value : original;' in reader, "unknown action kind text must remain unchanged"
host = (ROOT / "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs").read_text(encoding="utf-8-sig")
capture = next(line for line in host.splitlines() if "_campaignCharacterRecordCapture = new CampaignCharacterRecordCaptureAdapter(" in line)
assert "entry => NpcActionMaterialPreviewProjection.Build(NpcActionEditorDisplay, entry)" in capture
assert "NpcActionEditorProjection.BuildDevNpcActionPreviewText" not in capture, "localized UI pollutes saved Prompt materials"

prompt_kinds = ROOT / "src/modules/AF.Module.Weekly/Materials/WeeklyAggregateEventLineOwner.cs"
prompt_source = prompt_kinds.read_text(encoding="utf-8-sig")
original_kind = method(prompt_source, "internal static string TranslateNpcActionKindForPrompt(", "private static string ParseWeeklyPromptSettlementChangeDetail(")
ui_kind = method(ui, "private static string GetNpcActionKindForDisplay(", "internal static string BuildDevNpcActionItemLabel(")
assert set(re.findall(r'case "([^"]+)"', original_kind)) == set(re.findall(r'case "([^"]+)"', ui_kind)), "missing or invented action kinds"
raw_weekly = json.loads((ROOT / "content/modules/AF.Module.Weekly/ModuleData/WeeklyMaterialCompatibility.json").read_text(encoding="utf-8"))
kind_pairs = re.findall(r'case "([^"\n]+)":\s*return WeeklyMaterialTexts.Raw\("([^"\n]+)"\)', original_kind)
assert {"ActionKind_" + kind for kind, _ in kind_pairs} == {key for key in prompt_cn if key.startswith("ActionKind_")}
for kind, raw_key in kind_pairs:
    assert prompt_cn["ActionKind_" + kind] == raw_weekly[raw_key], kind + ": original meaning changed"
assert "port.TranslateNpcActionKindForPrompt(" not in ui and "port.BuildNpcActionActorNarrativeText(" not in ui, "UI invokes Prompt/fact text producer"
assert "port.BuildNpcActionActorNarrativeForDisplay?.Invoke(entry)" in ui, "optional actor display port missing"
assert '(entry.Text ?? "")' in ui and "entry.GameDate.Trim()" in ui and "(entry.LocationText ??" in ui, "raw fields were hidden"
assert not re.search(r"\bentry\.\w+\s*=(?!=)", ui), "display projection mutates records"
assert not re.search(r"\b(?:OnTick|GetType|GetMethods|GetFields)\b", ui), "unexpected hot path or reflection"
project = ET.parse(ROOT / "tests/modules/AF.Module.Persona/RootEditorLifecycleTests/RootEditorLifecycleTests.csproj")
for row in project.findall(".//Compile"):
    target = (ROOT / "tests/modules/AF.Module.Persona/RootEditorLifecycleTests" / row.attrib["Include"]).resolve()
    assert target.is_file(), f"fixture closure missing: {target}"
print("PASS NPC action editor static contract: keys=110 kinds=37 UI Chinese=0 CN fact method=baseline-identical after resource reconstruction, paired package grammar; C# compile/runtime NOT-RUN")
