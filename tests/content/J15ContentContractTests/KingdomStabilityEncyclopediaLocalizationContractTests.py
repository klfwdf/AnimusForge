"""Static display-boundary evidence, not a C# build or live encyclopedia test."""
import json
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
PATH = "src/AF.GameAdapter.Bannerlord/Kingdom/KingdomStabilityGameAdapter.cs"
source = (ROOT / PATH).read_text(encoding="utf-8-sig")
baseline = subprocess.check_output(["git", "show", "da490e177:" + PATH], cwd=ROOT).decode("utf-8")
manifest = json.loads((ROOT / "docs/handoffs/2026-10-10-kingdom-stability-encyclopedia-ui-keys.json").read_text(encoding="utf-8"))
keys = {entry["id"]: entry for entry in manifest["newKeys"]}
assert len(keys) == len(manifest["newKeys"]) == 7

def method(text, name):
    match = re.search(r"\b(?:internal|private) (?:static )?[^\n]+ " + name + r"\(", text)
    start = match.start()
    end = text.index("\n\t}", start) + len("\n\t}")
    return text[start:end]

display = method(source, "BuildKingdomStabilityEncyclopediaText")
tier = method(source, "GetKingdomStabilityTierDisplayText")
assert not re.search(r"[\u3400-\u9fff]", display + tier)
assert "GetKingdomStabilityTierText(" not in display
assert "GetKingdomStabilityTierDisplayText(stabilityValue)" in display
assert method(source, "GetKingdomStabilityTierText") == method(baseline, "GetKingdomStabilityTierText")
# Retain the actual calculation inputs; the changed method only projects their output.
prefix = "StringBuilder stringBuilder = new StringBuilder();"
assert display.split(prefix)[0] == method(baseline, "BuildKingdomStabilityEncyclopediaText").split(prefix)[0]
for name in ("FormatKingdomStabilityRelationOffsetText", "FormatKingdomRebellionChance"):
    assert method(source, name) == method(baseline, name)

english = {}
chinese = {}
for path in (ROOT / "content/modules").rglob("*.xml"):
    if "Languages" not in path.parts:
        continue
    data = {node.get("id"): node.get("text") for node in ET.parse(path).iter("string")}
    target = chinese if "CNs" in path.parts else english
    target.update(data)

used = set()
for key, fallback in re.findall(r'DisplayText\("([A-Za-z0-9_]+)", "((?:\\.|[^"\\])*)"', display + tier):
    fallback = json.loads('"' + fallback + '"')
    used.add(key)
    if key in keys:
        entry = keys[key]
        assert fallback == entry["en"], key
        assert sorted(re.findall(r"\{([A-Z][A-Z0-9_]*)\}", entry["en"])) == sorted(re.findall(r"\{([A-Z][A-Z0-9_]*)\}", entry["zh"])), key
        # Before merge, all additions are exclusive; after merge, both values must match.
        if key in english or key in chinese:
            assert english.get(key) == entry["en"] and chinese.get(key) == entry["zh"], key
    else:
        assert key in manifest["reusedKeys"], key
        assert english.get(key) == fallback and chinese.get(key), key
assert used == set(keys) | set(manifest["reusedKeys"])

# Everything outside the reviewed display method and new private display helpers is unchanged.
new_other = source
for name in ("BuildKingdomStabilityEncyclopediaText", "GetKingdomStabilityTierDisplayText", "DisplayText"):
    new_other = new_other.replace(method(new_other, name), "")
old_other = baseline.replace(method(baseline, "BuildKingdomStabilityEncyclopediaText"), "")
new_other = new_other.replace("using TaleWorlds.Localization;\n", "")
assert re.sub(r"\s+", "", new_other) == re.sub(r"\s+", "", old_other)
patch = (ROOT / "src/AF.GameAdapter.Bannerlord/UI/Kingdom/EncyclopediaKingdomStabilityPatch.cs").read_text(encoding="utf-8-sig")
assert "ConditionalWeakTable<EncyclopediaFactionPageVM, AppendedBlock>" in patch
assert "RemoveExistingStabilityBlock(__instance.InformationText, previous.Text)" in patch
assert 'previous.Text = "";' in patch and "previous.Text = combinedText.Trim();" in patch
assert "text.EndsWith(previousBlock, StringComparison.Ordinal)" in patch
assert "text[found - 1] == '\\n'" in patch and "text[end] == '\\n'" in patch
assert '{=AF_UI_KingdomStability_EncyclopediaTitle}Kingdom stability' in patch
assert keys["AF_UI_KingdomStability_EncyclopediaTitle"]["zh"] == "【王国稳定度】"
print("PASS kingdom stability encyclopedia: 7 new keys, 9 reused keys, raw/calculation boundaries retained, refresh suffix/title wiring checked; C# and live refresh NOT-RUN")
