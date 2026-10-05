"""Check the two configuration surfaces against each other and the approved defaults."""
from pathlib import Path
from collections import Counter
import re

root = Path(__file__).resolve().parents[4]
mcm = (root / "src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.cs").read_text(encoding="utf-8-sig")
terminal = (root / "src/AF.GameAdapter.Bannerlord/UI/Terminal/TerminalSettingsRegistry.cs").read_text(encoding="utf-8-sig")
settings = {}
for match in re.finditer(r'\[SettingProperty(?:Bool|Integer|Dropdown|Button)\("([^"\n]+)"[^\n]*\]\s*\[SettingPropertyGroup\("(17\.[^"\n]+)"[^\n]*\]\s*public [^\n]+? (\w+)\s*\{', mcm):
    title, group, key = match.groups()
    hint = re.search(r'HintText = "([^"]*)"', match[0])
    settings[key] = (title, group, hint[1])
definitions = {}
for match in re.finditer(r'new TerminalSettingDef\("([^"]+)", "[^"]+", "(17\.[^"]+)", "([^"]+)", "([^"]*)"', terminal):
    key, group, title, hint = match.groups()
    assert key not in definitions, key
    definitions[key] = (title, group, hint)
assert len(settings) == 18, len(settings)
assert settings == definitions, "MCM and terminal must have the same keys, groups, titles and hints"
assert sorted(Counter(x[1] for x in settings.values()).values()) == [2, 2, 3, 3, 4, 4]
assert "WorldDiplomacyRoundIntervalDays" not in settings
assert re.search(r'public int WorldDiplomacyRoundIntervalDays \{ get; set; \} = 3;', mcm)
for key, value in [("DefaultWorldDiplomacyMaxConcurrentOrdinaryRounds", 3),
                   ("DefaultWorldDiplomacyDeclarationMinCharacters", 100),
                   ("DefaultWorldDiplomacyDeclarationMaxCharacters", 500),
                   ("WorldDiplomacyDeclarationCharactersMin", 1),
                   ("WorldDiplomacyDeclarationCharactersMax", 1000)]:
    assert re.search(rf'public const int {key} = {value};', mcm), key
assert 'TerminalSettingType.Integer, 1f, 12f' in next(line for line in terminal.splitlines() if 'new TerminalSettingDef("WorldDiplomacyMaxConcurrentOrdinaryRounds"' in line)
assert settings["WorldDiplomacyMaxConcurrentOrdinaryRounds"][0] == "同时进行的普通外交事件上限"
print("Diplomacy MCM/terminal: PASS (18 consistent settings, 6 groups, defaults and legacy key)")
