"""Static gallery/inventory wiring and resource-driven suffix regression checks."""
from pathlib import Path
import json
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
gallery = (ROOT / "extensions/AnimusForge.Illustrator/src/UI/Gallery/GalleryRpItemConverter.cs").read_text(encoding="utf-8-sig")
economy = (ROOT / "src/modules/AF.Module.Economy/Host/RewardSystemBehavior.cs").read_text(encoding="utf-8-sig")
checks = 0
def check(condition, label):
    global checks
    checks += 1
    if not condition:
        raise AssertionError(label)

check('return DisplayItemName(item);' in gallery, "real inventory name comes from bilingual projection")
check('preserveExistingDisplayName: true' in gallery, "gallery requests persisted-name preservation")
check('name = item.Name?.ToString() ?? name;' in gallery, "roster uses actual created/restored name")
check('item.StringId, name, null, item.Id.InternalValue, 1,' in gallery, "roster keeps id, object id and one-item quantity")
check('GalleryConversionAlreadyOwned(name)' in gallery and 'GalleryConversionDelivered(name)' in gallery,
      "success notifications agree with the real inventory name")
check('"illustrator_gallery:" + DiskImageCacheManager.SanitizeKey(item.CampaignKey) + ":" + item.Key' in gallery,
      "identity stays independent of translated display text")
check('string logSource = null)\n\t\t=> TryCreateGeneratedInventoryItemForExternal(displayName, identityKey, out generatedItem, false, templateItemId, logSource);' in economy,
      "original public signature forwards with original rename behavior")
start = economy.index('public static bool TryCreateGeneratedInventoryItemForExternal(string displayName, string identityKey, out ItemObject generatedItem, bool')
end = economy.index('private static string NormalizeGeneratedInventoryDisplayName', start)
create = economy[start:end]
check(create.count('BuildGeneratedRewardItemStringId(identity, templateItem.StringId ?? "")') == 1,
      "same identity hash and template input")
check(create.count('TryGetOrCreateGeneratedRewardItem(') == 1, "one creation path")
check('name = existingRecord.DisplayName;' in create and 'name = existingName;' in create,
      "stored record wins; registered object is the fallback")
check(create.index('if (preserveExistingDisplayName)') < create.index('TryGetOrCreateGeneratedRewardItem('),
      "preservation happens before existing item mutation")
check('foreach' not in create and 'Task.' not in create, "no new item scan or background game access")

# Both templates must continue through the same miscellaneous-item selector. Read the
# real equipment/food suffix tables, including English word-boundary requirements.
rules = []
for m in re.finditer(r'new GeneratedRp(?:Equipment|Food)SuffixRule\([^\n]+', economy):
    boundary = ', true,' in m[0]
    for text in re.findall(r'"(?:\\.|[^"\\])*"', m[0]):
        rules.append((json.loads(text), boundary))
check(len(rules) > 100, "production suffix rules extracted")
for lang, relative in (("en", "illustrator_strings.xml"), ("zh", "CNs/illustrator_strings-zh-CN.xml")):
    path = ROOT / "content/modules/AnimusForge.Illustrator/ModuleData/Languages" / relative
    values = {n.get("id"): n.get("text", "") for n in ET.parse(path).getroot().iter("string")}
    template = values['AF_ILL_UI_GALLERY_CONVERSION_ITEM_NAME']
    default = values['AF_ILL_UI_GALLERY_CONVERSION_DEFAULT_TITLE']
    check(set(re.findall(r'\{\w+\}', template)) == {'{TITLE}'}, lang + " title placeholder retained")
    for title in (default, 'Sword', 'Bread', 'Royal Portrait', '剑', '面包', '{SPECIAL} & title'):
        name = template.replace('{TITLE}', title)
        matches = []
        for suffix, boundary in rules:
            if not name.casefold().endswith(suffix.casefold()):
                continue
            preceding = name[:-len(suffix)]
            if boundary and preceding and preceding[-1].isascii() and preceding[-1].isalnum():
                continue
            matches.append(suffix)
        check(not matches, lang + " scroll is not reclassified as equipment/food: " + repr(matches))
print(f"PASS gallery inventory name static/suffix contract: {checks} checks; compiled/game replay NOT RUN")
