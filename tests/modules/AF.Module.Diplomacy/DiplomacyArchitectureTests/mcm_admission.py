"""Seven diplomacy entries, retained persistence and paired real UI resources."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[4]
def read(path):
    return (root / path).read_text(encoding="utf-8-sig")
mcm = read("src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.cs")
terminal = read("src/AF.GameAdapter.Bannerlord/UI/Terminal/TerminalSettingsRegistry.cs")
coverage = read("src/AF.GameAdapter.Bannerlord/UI/Terminal/TerminalSettingsCoverage.cs")
localization = read("src/AF.GameAdapter.Bannerlord/UI/Terminal/TerminalSettingsLocalization.cs")
expected = {"EnableWorldDiplomacy", "WorldDiplomacyStartNewGameAtPeace", "WorldDiplomacyRoundIntervalDays", "WorldDiplomacyCourtMaxDeliveryDays", "WorldDiplomacyWarTendency", "WorldDiplomacyDeclarationMaxCharacters", "EditWorldDiplomacyPrompt"}
rows = re.findall(r'new TerminalSettingDef\("([^"]+)", TerminalCategories\.(\w+), "([^"]+)"', terminal)
actual = {key for key, category, group in rows if category == "Diplomacy" and group.startswith("17.")}
assert actual == expected, (actual, expected)
assert sum(key == "EnableWorldDiplomacyMapNotifications" and category == "Ui" for key, category, group in rows) == 1
retired = set(re.findall(r'"(\w+)"', coverage.split("RetiredDiplomacySettings =")[1].split("};")[0]))
assert len(retired) == 12
for key in retired:
    assert re.search(r'public [^\n]+ '+key+r'\s*\{', mcm), key
    assert key not in actual
assert "if (RetiredDiplomacySettings.Contains(id)) return false;" in coverage
assert 'WorldDiplomacyStartNewGameAtPeace { get; set; } = true;' in mcm
assert 'WorldDiplomacyRoundIntervalDays { get; set; } = 3;' in mcm
assert 'WorldDiplomacyWarTendency { get; set; } = 50;' in mcm
for relative in ["af_ui_strings.xml", "CNs/af_ui_strings-zh-CN.xml"]:
    elements = list(ET.parse(root / "content/modules/AF.Module.UI/ModuleData/Languages" / relative).iter("string"))
    strings = {e.attrib["id"]: e.attrib["text"] for e in elements}
    assert len(strings) == len(elements), "duplicate localization key"
    for key in expected:
        for part in ["Title", "Hint", "Group"]:
            assert strings.get("AF_TERMINAL_UI_"+key+"_"+part), (relative,key,part)
        assert '"'+key+'",' in localization, key
popup = ET.parse(root / "content/modules/AF.Module.UI/GUI/Prefabs/DevHistoryEditPopup.xml")
assert any(e.attrib.get("Command.Click")=="ExecuteRestoreDefaults" and e.attrib.get("IsVisible")=="@CanRestoreDefaults" for e in popup.iter())
assert '() => DefaultWorldDiplomacyPreference' in mcm
print("Diplomacy settings: PASS (7 entries, UI notice, 12 retired fields, CN/EN consumers, editor restore)")
