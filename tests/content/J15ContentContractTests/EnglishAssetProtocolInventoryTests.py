"""Verify exact protocol identities without suppressing untranslated prose."""
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tools/localization'))
from inventory_untranslated import english_prose_has_chinese, json_strings

paths = (
    'content/foundation/AF.Foundation.Localization/ModuleData/AdapterSceneTexts.en.json',
    'content/foundation/AF.Foundation.Localization/ModuleData/McmPromptDefaults.en.json',
    'content/modules/AF.Module.Diplomacy/ModuleData/DiplomacyMiscPromptTexts.en.json',
    'content/modules/AF.Module.Prompt/CustomPrompts/PlayerCustomPromptRule.en.json',
    'content/modules/AF.Module.Prompt/ModuleData/PromptComposerTexts.en.json',
)
checks = 0
for relative in paths:
    for pointer, value in json_strings(json.loads((ROOT / relative).read_text('utf-8-sig'))):
        assert not english_prose_has_chinese(value), (relative, pointer, value)
        checks += 1
for value in (
    '[AFEF NPC行为补充] 已送达。',
    '[AFEF玩家行为补充] English body with 汉字.',
    '【AFEF NPC行为补充】 未翻译正文',
    '[AFEF NPC行为补充未翻译] English body',
    '[AFEF 未知行为] English body',
    '[ACTION:SCENE_ACT:动作程序错误]',
    '[RELAY:接力编号错误]',
):
    assert english_prose_has_chinese(value), value
    checks += 1
print(f'PASS exact protocol inventory: {checks} actual asset leaves and untranslated-body/tag negative controls')
