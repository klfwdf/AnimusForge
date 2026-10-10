"""Static UI boundary/resource evidence. Current C# and in-game refresh are NOT-RUN."""
import importlib.util
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
BASELINE = "97075a230"
spec = importlib.util.spec_from_file_location("inventory", ROOT / "tools/localization/inventory_untranslated.py")
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)


def read(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")


def original(path):
    return subprocess.check_output(["git", "show", BASELINE + ":" + path], cwd=ROOT).decode("utf-8-sig").replace("\r\n", "\n")


def method(source, signature):
    start = source.index(signature)
    code = audit.tokenize(source)[0]
    opening, depth = code.index("{", start), 1
    for end in range(opening + 1, len(code)):
        depth += (code[end] == "{") - (code[end] == "}")
        if depth == 0:
            return source[start:end + 1]
    raise AssertionError(signature)


host_path = "src/modules/AF.Module.Social/Host/PlayerNotorietyBehavior.cs"
host, baseline = read(host_path), original(host_path)
restored = host
signatures = (
    "private string BuildPlayerNotorietyDisplayText(",
    "private string BuildPlayerNotorietyHistoryText(",
    "private static string GetLevelText(",
    "private static string ResolveCultureDisplayName(CultureObject",
    "private static string ResolveCultureDisplayName(string",
)
for signature in signatures:
    restored = restored.replace(method(host, signature), method(baseline, signature), 1)
new_helper = method(host, "private static string BuildNotorietyActionLineForDisplay(")
restored = restored.replace(new_helper + "\n\n\t", "", 1)
array = r"private static readonly string\[\] NotorietyLevelTexts = new string\[11\]\s*\{[\s\S]*?\};"
old_array, new_array = re.search(array, baseline)[0], re.search(array, host)[0]
restored = restored.replace(new_array, old_array, 1)
assert restored == baseline, "A non-display producer/Prompt/save/rule changed"
assert host.count("BuildPlayerNotorietyDisplayText(") == 2  # Declaration + sole encyclopedia caller.
assert "BuildPlayerNotorietyPopupData" in host and "HistoryText = BuildPlayerNotorietyHistoryText" in host
assert ".Take(20)" in method(host, signatures[0]) and ".Take(30)" in method(host, signatures[0])
assert "gameDate.Trim()" in new_helper and '("TEXT", text)' in new_helper
level = method(host, signatures[2])
assert "Math.Floor(ClampPercentDouble(score) / 10.0)" in level and "index = 10" in level

en_path = ROOT / "content/modules/AF.Module.Social/ModuleData/Languages/afsocial_ui_strings.xml"
en = {n.get("id"): n.get("text") for n in ET.parse(en_path).iter("string")}
cn = {n.get("id"): n.get("text") for n in ET.parse(en_path.parent / "CNs/afsocial_ui_strings-zh-CN.xml").iter("string")}
keys = {key for key in en if key.startswith("AF_SOCIAL_UI_Notoriety_")}
assert len(keys) == 24 and keys <= cn.keys(), keys
for match in re.finditer(r'(?:UiText|SocialUiLocalization\.Format)\("(AF_SOCIAL_UI_Notoriety_[^"]+)",\s*("(?:[^"\\]|\\.)*")', host):
    assert en[match[1]] == audit.decode(match[2]), match[1]
levels = re.findall(r'"\{=(AF_SOCIAL_UI_Notoriety_Level\d+)\}([^"]+)"', new_array)
assert len(levels) == 11
assert [cn[key] for key, _ in levels] == [audit.decode(m[0]) for m, _ in audit.tokenize(old_array)[1]]
assert all(en[key] == fallback for key, fallback in levels)
for signature in signatures[:2]:
    assert not any(audit.HAN.search(audit.decode(m[0])) for m, _ in audit.tokenize(method(host, signature))[1]), signature

patch_path = "src/AF.GameAdapter.Bannerlord/UI/Persona/EncyclopediaHeroPersonaPatch.cs"
patch = read(patch_path)
assert "ConditionalWeakTable<EncyclopediaHeroPageVM, NotorietyBlock>" in patch
apply = method(patch, "private static void ApplyPlayerNotorietyText(")
assert "RemovePreviousNotorietyBlock(vm.InformationText, previous.Text)" in apply
assert apply.index("RemovePreviousNotorietyBlock") < apply.index("if (string.IsNullOrWhiteSpace(notorietyText))")
assert 'previous.Text = "";' in apply and "previous.Text = notorietyText;" in apply
remove = method(patch, "private static string RemovePreviousNotorietyBlock(")
assert "EndsWith(previousBlock, StringComparison.Ordinal)" in remove
find = method(patch, "private static int FindNotorietyHeading(")
assert "text[found - 1] == '\\n'" in find and "text[end] == '\\n'" in find
before_patch = original(patch_path)
for signature in ("public static bool EncyclopediaDataOnTickPrefix(", "public static void OnApplicationTick("):
    # The existing input guard and update loop must stay intact; no new Tick translation.
    assert method(patch, signature) == method(before_patch, signature), signature
assert set(audit.TRACED_UI_METHODS[host_path]) >= {"BuildPlayerNotorietyDisplayText", "BuildPlayerNotorietyHistoryText"}
print("PASS static notoriety UI: 24 paired keys, 11 unchanged tier meanings, raw producers/Prompt/save/rules identical, encyclopedia/popup wiring and refresh suffix/input/Tick guards; C# and game NOT-RUN")
