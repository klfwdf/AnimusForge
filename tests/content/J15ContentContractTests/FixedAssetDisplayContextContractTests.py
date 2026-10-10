"""Fixed asset display must not split arbitrary names on prose delimiters."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
host = (ROOT / 'src/modules/AF.Module.Economy/Host/RewardSystemBehavior.cs').read_text(encoding='utf-8-sig')
ui = (ROOT / 'src/modules/AF.Module.Economy/Host/EconomyUiTexts.cs').read_text(encoding='utf-8-sig')
section = ui[ui.index('internal static string FixedAssetStatusForDisplay('):ui.index('public static string KingdomTransferStatusForDisplay(')]
checks = 0

def check(value, message):
    global checks
    checks += 1
    if not value:
        raise AssertionError(message)

branches = re.findall(r'context.Matches\(value, "([^"]*)", "([^"]*)", "([^"]*)"\)\)\s*\{\s*return Text\("([^"]+)"', section)
check(len(branches) == 4 and '(.+)' not in section, 'all four ambiguous regexes replaced')
for language, filename in [('en', 'afeconomy_strings.xml'), ('zh', 'CNs/afeconomy_strings-zh-CN.xml')]:
    resource = ROOT / 'content/modules/AF.Module.Economy/ModuleData/Languages' / filename
    values = {n.get('id'): n.get('text') for n in ET.parse(resource).getroot().iter('string')}
    for asset, giver in [('Workshop', 'Owner'), ('Forge 转交给 A', 'B 转交给 C'),
                         ('Estate 不属于 A', 'B 不属于 C'), ('Estate\nSecond line', 'Owner\nSecond line'),
                         ('Asset（literal）', 'Owner 的家族'), ('', '')]:
        for prefix, separator, suffix, key in branches:
            raw = prefix + asset + separator + giver + suffix
            matched = [(a, b, c, k) for a, b, c, k in branches if raw == a + asset + b + giver + c]
            check(len(matched) == 1 and matched[0][3] == key, 'exact typed-context branch')
            display = values[key].replace('{ASSET}', asset).replace('{GIVER}', giver)
            check(bool(display) and (not asset or asset in display) and (not giver or giver in display), language + ' names survive unchanged')
            check(raw + 'unexpected' != prefix + asset + separator + giver + suffix, 'unknown extra text is not accepted')
check(section.count('("ASSET", context.AssetName), ("GIVER", context.GiverName)') == 4, 'translation variables use captured names')
check('value, prefix + AssetName + separator + GiverName + suffix, StringComparison.Ordinal' in ui, 'match is ordinal and exact')
start = host.index('out EconomyUiTexts.FixedAssetDisplayContext displayContext, EconomyMutationObservation mutationObservation)')
end = host.index('private bool TryApplySettlementFixedAssetTransfer', start)
entry = host[start:end]
check(entry.index('new EconomyUiTexts.FixedAssetDisplayContext') < entry.index('switch (entry.AssetKind)'), 'capture precedes every ownership mutation')
for method in ('Settlement', 'Workshop', 'Caravan'):
    check(entry.count('return TryApply' + method + 'FixedAssetTransfer(') == 1, method + ' still executes once')
check('catch\n\t\t\t{\n\t\t\t\tdisplayContext = default;' in entry, 'display lookup failure cannot reject the transfer')
check('FixedAssetStatusForDisplay(statusText, displayContext)' in host, 'HUD uses captured context')
check('displayContext: out displayContext' in host and 'out statusText, out displayContext, mutationObservation: null' in host, 'context crosses both private entry layers')
check('giverFacts.Add(statusText);' in host and 'receiverFacts.Add(statusText);' in host, 'raw fact paths remain')
replay = (ROOT / 'src/modules/AF.Module.Economy/Execution/Hero/RewardSystemBehavior.EconomyReplay.cs').read_text(encoding='utf-8-sig')
check('TryApplySettlementTransferAction(' in replay and 'mutationObservation' in replay, 'replay retains old observation-aware entry')
print(f'PASS fixed asset display context: {checks} static/simulation checks; compiled transfer/replay NOT RUN')
