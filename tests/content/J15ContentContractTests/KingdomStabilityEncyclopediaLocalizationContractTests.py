"""Static display-boundary evidence, not a C# build or live encyclopedia test."""
import json
import re
import sys
import hashlib
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
PATH = "src/AF.GameAdapter.Bannerlord/Kingdom/KingdomStabilityGameAdapter.cs"
source = (ROOT / PATH).read_text(encoding="utf-8-sig")
baseline = subprocess.check_output(["git", "show", "da490e177:" + PATH], cwd=ROOT).decode("utf-8")
assert hashlib.sha256(baseline.encode()).hexdigest() == '3726c436f222518fd55d05d50343fcd7d830c02188e82afbbdc067db01e02b70', 'fixed adapter baseline identity'
manifest = json.loads((ROOT / "docs/handoffs/2026-10-10-kingdom-stability-encyclopedia-ui-keys.json").read_text(encoding="utf-8"))
keys = {entry["id"]: entry for entry in manifest["newKeys"]}
assert len(keys) == len(manifest["newKeys"]) == 7

def method(text, name):
    match = re.search(r"\b(?:internal|private) (?:static )?[^\n]+ " + name + r"\(", text)
    start = match.start()
    masked = LEX.sub(lambda token: ' ' * len(token[0]), text)
    opening = masked.index('{', match.end())
    depth = 1
    for end in range(opening + 1, len(masked)):
        depth += (masked[end] == '{') - (masked[end] == '}')
        if depth == 0:
            return text[start:end + 1]
    raise AssertionError(name + ': unterminated production method')


sys.path.insert(0, str(ROOT / 'tools/localization'))
from inventory_untranslated import LEX, decode
def code_tokens(text):
    result, cursor = [], 0
    for token in LEX.finditer(text):
        result.append(re.sub(r'\s+', '', text[cursor:token.start()]))
        result.append(json.dumps(decode(token[0]), ensure_ascii=False) if token.lastgroup == 'string' else token[0])
        cursor = token.end()
    result.append(re.sub(r'\s+', '', text[cursor:]))
    return ''.join(result)

tier_meanings = [
    ('ExtremelyHigh', '极高', 'Extremely high'), ('High', '高', 'High'),
    ('FairlyHigh', '较高', 'Fairly high'), ('Average', '一般', 'Average'),
    ('Poor', '较差', 'Poor'), ('VeryPoor', '很差', 'Very poor'),
    ('ExtremelyPoor', '极差', 'Extremely poor'),
]
def restore_ui_tiers(text):
    for name, chinese, english in tier_meanings:
        call = 'KingdomUiText.T("AF_KINGDOM_CW_Stability_' + name + '", ' + json.dumps(english) + ')'
        assert text.count(call) == 1, name + ': exact existing UI-tier consumer'
        text = text.replace(call, json.dumps(chinese, ensure_ascii=False), 1)
    return text

display = method(source, "BuildKingdomStabilityEncyclopediaText")
tier = method(source, "GetKingdomStabilityTierDisplayText")
assert not re.search(r"[\u3400-\u9fff]", display + tier)
assert "GetKingdomStabilityTierText(" not in display
assert "GetKingdomStabilityTierDisplayText(stabilityValue)" in display
assert code_tokens(restore_ui_tiers(method(source, "GetKingdomStabilityTierText"))) == code_tokens(method(baseline, "GetKingdomStabilityTierText"))
# Retain the actual calculation inputs; the changed method only projects their output.
prefix = "StringBuilder stringBuilder = new StringBuilder();"
assert code_tokens(display.split(prefix)[0]) == code_tokens(method(baseline, "BuildKingdomStabilityEncyclopediaText").split(prefix)[0])
for name in ("FormatKingdomStabilityRelationOffsetText", "FormatKingdomRebellionChance"):
    assert code_tokens(method(source, name)) == code_tokens(method(baseline, name))

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
new_other = restore_ui_tiers(source)
for name in ("BuildKingdomStabilityEncyclopediaText", "GetKingdomStabilityTierDisplayText", "DisplayText"):
    new_other = new_other.replace(method(new_other, name), "")
old_other = baseline.replace(method(baseline, "BuildKingdomStabilityEncyclopediaText"), "")
new_other = new_other.replace("using TaleWorlds.Localization;\n", "")
prompt_root = ROOT / "content/foundation/AF.Foundation.Localization/ModuleData"
prompt_cn = json.loads((prompt_root / "AdapterPromptCapture.zh-CN.json").read_text(encoding="utf-8"))
prompt_en = json.loads((prompt_root / "AdapterPromptCapture.en.json").read_text(encoding="utf-8"))
assert prompt_cn["kingdom_stability_prompt"] == "当前王国稳定度评级："
assert prompt_cn["kingdom_fiefs_more"] == " 等{COUNT}处"
assert prompt_cn["kingdom_clan_fiefs"] == "{CLAN}家族当前掌握的核心封地包括：{FIEFS}。"
for key in ("kingdom_stability_prompt", "kingdom_fiefs_more", "kingdom_clan_fiefs"):
    assert re.findall(r"\{[A-Z_]+\}", prompt_cn[key]) == re.findall(r"\{[A-Z_]+\}", prompt_en[key])
    assert prompt_en[key] and not re.search(r"[\u3400-\u9fff]", prompt_en[key])

prompt_tier = method(source, 'GetKingdomStabilityTierTextForPrompt')
expected_prompt = ['internal static string GetKingdomStabilityTierTextForPrompt(int value) { string key; switch (KingdomStabilityPolicy.GetKingdomStabilityTier(value)) {']
for index, (name, chinese, _) in enumerate(tier_meanings, 1):
    key = 'Project_KingdomStability_' + str(index)
    assert prompt_cn[key] == chinese and prompt_en[key].strip()
    label = 'case KingdomStabilityTier.' + name + ':' if index < 7 else 'default:'
    expected_prompt.append(label + ' key = "' + key + '"; break;')
expected_prompt.append('} return AdapterPromptCaptureTexts.PcapText(key); }')
assert code_tokens(prompt_tier) == code_tokens(' '.join(expected_prompt)), 'seven tier meanings use the fixed package reader and real policy enum'
assert 'KingdomUiText' not in prompt_tier and 'TextObject' not in prompt_tier and 'PcapProjectKnown' not in prompt_tier
weekly = method(source, 'BuildWeeklyReportCurrentKingdomStabilityTierText')
assert 'GetKingdomStabilityTierTextForPrompt(_state().Get(MemoryEntityIdentityBannerlordAdapter.GetKingdomId(kingdom)))' in weekly
assert weekly.count('_state().Get(') == 1 and 'GetKingdomStabilityTierText(' not in weekly and 'PcapProjectKnown' not in weekly
assert '"kingdom", StringComparison.OrdinalIgnoreCase' in weekly and 'if (kingdom == null)' in weekly
assert weekly.count('return "";') == 2, 'null/nonkingdom/unknown identity leaves remain excluded'
assert new_other.count(prompt_tier) == 1
new_other = new_other.replace(prompt_tier, '', 1)
comment = '// Prompt text follows the installed language package, independently of the UI.\n'
assert new_other.count(comment) == 1
new_other = new_other.replace(comment, '', 1)
raw = json.loads((ROOT / 'content/modules/AF.Module.Kingdom/ModuleData/KingdomCivilWarCompatibility.json').read_text(encoding='utf-8-sig'))
new_other = re.sub(r'KingdomCivilWarTexts.Raw\("([^"\n]+)"\)', lambda match: json.dumps(raw[match[1]], ensure_ascii=False), new_other)

# The actual policy calculation remains identical to the fixed old revision.
policy_path = 'src/modules/AF.Module.Kingdom/Stability/KingdomStabilityPolicy.cs'
policy = (ROOT / policy_path).read_text(encoding='utf-8-sig')
old_policy = subprocess.check_output(['git', 'show', 'da490e177:' + policy_path], cwd=ROOT).decode('utf-8-sig')
assert hashlib.sha256(old_policy.encode()).hexdigest() == 'a61105f62ef28e5e72d2ee089553badce4081334f3966b1c4fdffc470c3b72dd', 'fixed policy baseline identity'
assert code_tokens(policy) == code_tokens(old_policy), 'clamp, seven enum tiers, relation/loyalty/rebellion calculations unchanged'
assert 'Math.Max(KingdomStabilityMinValue, Math.Min(KingdomStabilityMaxValue, value))' in policy
assert 'KingdomStabilityMinValue = 0;' in policy and 'KingdomStabilityMaxValue = 100;' in policy

# The shared core retains raw fact output by default. Reverse only its text
# branch after proving both forwarding entry points and exact CN templates.

wrappers = "internal static string BuildClanFortificationSummary(Clan clan) => BuildClanFortificationSummaryCore(clan, false); internal static string BuildClanFortificationSummaryForPrompt(Clan clan) => BuildClanFortificationSummaryCore(clan, true); private static string BuildClanFortificationSummaryCore(Clan clan, bool forPrompt)"
new_other = code_tokens(new_other)
assert new_other.count(code_tokens(wrappers)) == 1
new_other = new_other.replace(code_tokens(wrappers), code_tokens('internal static string BuildClanFortificationSummary(Clan clan)'), 1)
tail = 'text += forPrompt ? AdapterPromptCaptureTexts.PcapFormat("kingdom_fiefs_more", " and others, {COUNT} total", ("COUNT", list.Count)) : " 等" + list.Count + "处";'
assert new_other.count(code_tokens(tail)) == 1, 'raw/prompt suffix branches retain count and old raw bytes'
new_other = new_other.replace(code_tokens(tail), code_tokens('text = text + " 等" + list.Count + "处";'), 1)
summary = 'return forPrompt ? AdapterPromptCaptureTexts.PcapFormat("kingdom_clan_fiefs", "The {CLAN} clan currently controls these core fiefs: {FIEFS}.", ("CLAN", GetClanDisplayName(clan)), ("FIEFS", text)) : GetClanDisplayName(clan) + "家族当前掌握的核心封地包括：" + text + "。";'
assert new_other.count(code_tokens(summary)) == 1, 'raw/prompt wrappers retain entity inputs and old fact branch'
new_other = new_other.replace(code_tokens(summary), code_tokens('return GetClanDisplayName(clan) + "家族当前掌握的核心封地包括：" + text + "。";'), 1)
new_other = new_other.replace('GetKingdomStabilityTierTextForPrompt(', 'GetKingdomStabilityTierText(')
new_other = new_other.replace(code_tokens('AdapterPromptCaptureTexts.PcapText("kingdom_stability_prompt")'), json.dumps(prompt_cn['kingdom_stability_prompt'], ensure_ascii=False))
assert new_other == code_tokens(old_other), 'whole non-display adapter: original facts, inputs, save/rule/action branches preserved after exact inverse'
# A changed raw branch/threshold/reader cannot be normalized away by the inverse.
assert code_tokens(policy.replace('num >= 90', 'num >= 89', 1)) != code_tokens(old_policy)
assert new_other.replace('Take(4)', 'Take(5)', 1) != code_tokens(old_other)
assert code_tokens(prompt_tier.replace('PcapText(key)', 'KingdomUiText.T(key, key)', 1)) != code_tokens(' '.join(expected_prompt))
patch = (ROOT / "src/AF.GameAdapter.Bannerlord/UI/Kingdom/EncyclopediaKingdomStabilityPatch.cs").read_text(encoding="utf-8-sig")
assert "ConditionalWeakTable<EncyclopediaFactionPageVM, AppendedBlock>" in patch
assert "RemoveExistingStabilityBlock(__instance.InformationText, previous.Text)" in patch
assert 'previous.Text = "";' in patch and "previous.Text = combinedText.Trim();" in patch
assert "text.EndsWith(previousBlock, StringComparison.Ordinal)" in patch
assert "text[found - 1] == '\\n'" in patch and "text[end] == '\\n'" in patch
assert '{=AF_UI_KingdomStability_EncyclopediaTitle}Kingdom stability' in patch
assert keys["AF_UI_KingdomStability_EncyclopediaTitle"]["zh"] == "【王国稳定度】"
print("PASS kingdom stability encyclopedia: 7 new keys, 9 reused keys, raw/calculation boundaries retained, refresh suffix/title wiring checked; C# and live refresh NOT-RUN")
