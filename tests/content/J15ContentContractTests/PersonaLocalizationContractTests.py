"""Static/simulation contract for the Persona runtime localization slice."""
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
BASELINE = "8a29ce5f49a76230006cb508fd511d00a135bd8a"
PROMPT_RULES = "src/modules/AF.Module.Persona/Generation/NpcPersonaTextRules.cs"
IMPORT_RULES = "src/modules/AF.Module.Persona/Import/UnnamedPersonaImportValidationOwner.cs"
READINESS = "src/modules/AF.Module.Persona/Generation/NpcPersonaReadinessSnapshot.cs"
PROMPT_LOADER = "src/modules/AF.Module.Persona/Generation/PersonaPromptTexts.cs"
UI_LOADER = "src/modules/AF.Module.Persona/Import/PersonaUiTexts.cs"
PROMPT_EN = ROOT / "content/modules/AF.Module.Persona/ModuleData/PersonaPromptTexts.en.json"
PROMPT_ZH = ROOT / "content/modules/AF.Module.Persona/ModuleData/PersonaPromptTexts.json"
UI_EN = ROOT / "content/modules/AF.Module.Persona/ModuleData/Languages/afpersona_ui_strings.xml"
UI_ZH = ROOT / "content/modules/AF.Module.Persona/ModuleData/Languages/CNs/afpersona_ui_strings-zh-CN.xml"
PROMPT_KEYS = {
    "AF_PERSONA_PROMPT_CustomRequirementsHeading",
    "AF_PERSONA_PROMPT_NativeSystem",
    "AF_PERSONA_PROMPT_NativeUserIntro",
    "AF_PERSONA_PROMPT_TraitCatalog",
    "AF_PERSONA_PROMPT_RegenerateTask",
    "AF_PERSONA_PROMPT_OldPersonality",
    "AF_PERSONA_PROMPT_OldBackground",
    "AF_PERSONA_PROMPT_EmptyValue",
    "AF_PERSONA_PROMPT_PromotedSystem",
    "AF_PERSONA_PROMPT_PromotedUserIntro",
    "AF_PERSONA_PROMPT_PersonalNameLabel",
    "AF_PERSONA_PROMPT_FullNameLabel",
    "AF_PERSONA_PROMPT_OriginalTroopLabel",
    "AF_PERSONA_PROMPT_SkillOriginalTroopLabel",
    "AF_PERSONA_PROMPT_CultureLabel",
    "AF_PERSONA_PROMPT_CurrentSceneLabel",
    "AF_PERSONA_PROMPT_JoinEventLabel",
    "AF_PERSONA_PROMPT_PromotedFactsHeading",
    "AF_PERSONA_PROMPT_JoinHistoryHeading",
    "AF_PERSONA_PROMPT_SkillSystem",
    "AF_PERSONA_PROMPT_SkillUserIntro",
    "AF_PERSONA_PROMPT_AvailableSkillsLabel",
    "AF_PERSONA_PROMPT_EquipmentLabel",
    "AF_PERSONA_PROMPT_SkillSourceLabel",
    "AF_PERSONA_PROMPT_PersonaSummaryLabel",
    "AF_PERSONA_PROMPT_FallbackPersonality",
    "AF_PERSONA_PROMPT_FallbackBackground",
}
UI_KEYS = {
    "AF_PERSONA_UI_CONTEXT_Fears", "AF_PERSONA_UI_CONTEXT_Motivations", "AF_PERSONA_UI_CONTEXT_SpeechStyle", "AF_PERSONA_UI_CONTEXT_Traits", "AF_PERSONA_UI_CONTEXT_Values",
    "AF_PERSONA_UI_ImportMissingDirectory",
    "AF_PERSONA_UI_ImportDuplicateFolderKey",
    "AF_PERSONA_UI_ImportExistingKeyConflict",
    "AF_PERSONA_UI_ImportValidationException",
    "AF_PERSONA_UI_UnknownNpc",
} | {"AF_PERSONA_UI_TRAIT_" + key[len("AF_PERSONA_TRAIT_"):] for key in json.loads(PROMPT_EN.read_text(encoding="utf-8")).keys() if key.startswith("AF_PERSONA_TRAIT_")}
NAMED = re.compile(r"\{([A-Z][A-Z0-9_]*)\}")
NUMERIC = re.compile(r"\{\d+\}")

sys.path.insert(0, str(ROOT / "tools/localization"))
from inventory_untranslated import ESCAPE, HAN, LEX, decode


def has_cjk_or_fullwidth(value):
    return any(
        0x3000 <= ord(char) <= 0x303F
        or 0x3400 <= ord(char) <= 0x9FFF
        or 0xF900 <= ord(char) <= 0xFAFF
        or 0xFE30 <= ord(char) <= 0xFE4F
        or 0xFF00 <= ord(char) <= 0xFFEF
        or 0x20000 <= ord(char) <= 0x2FA1F
        for char in value
    )


def csharp_literals(source):
    values = []
    for match in LEX.finditer(source):
        if match.lastgroup == "string":
            values.append(decode(match[0]))
        elif match.lastgroup == "char":
            raw = match[0][1:-1]
            controls = {"n": "\n", "r": "\r", "t": "\t", "0": "\0"}
            values.append(ESCAPE.sub(lambda m: chr(int(next(x for x in m.groups()[:3] if x), 16))
                                     if any(m.groups()[:3]) else controls.get(m[4], m[4]), raw))
    return values


def read(relative):
    return (ROOT / relative).read_text(encoding="utf-8-sig")


def git_source(relative):
    return subprocess.check_output(["git", "show", f"{BASELINE}:{relative}"], cwd=ROOT).decode("utf-8")


def strings(path):
    root = ET.parse(path).getroot()
    assert root.tag == "base" and root.get("type") == "string", path
    nodes = list(root.findall("./strings/string"))
    result = {node.get("id"): node.get("text", "") for node in nodes}
    assert len(nodes) == len(result), path
    return result


def render(template, variables):
    return NAMED.sub(lambda match: variables.get(match.group(1), match.group(0)), template)


def cjk_values(values):
    return [value for value in values if has_cjk_or_fullwidth(value)]


def between(source, start, end):
    return source.split(start, 1)[1].split(end, 1)[0].replace("\r\n", "\n")


old_prompt_source = git_source(PROMPT_RULES)
old_import_source = git_source(IMPORT_RULES)
old_readiness_source = git_source(READINESS)
old_prompt_literals = cjk_values(csharp_literals(old_prompt_source))
old_import_literals = cjk_values(csharp_literals(old_import_source))
old_readiness_literals = cjk_values(csharp_literals(old_readiness_source))
assert len([value for value in old_prompt_literals if HAN.search(value)]) == 33
assert len([value for value in old_import_literals if HAN.search(value)]) == 5
assert len([value for value in old_readiness_literals if HAN.search(value)]) == 1
assert sum(map(len, (old_prompt_literals, old_import_literals, old_readiness_literals))) >= 39

prompt_en = json.loads(PROMPT_EN.read_text(encoding="utf-8"))
prompt_zh = json.loads(PROMPT_ZH.read_text(encoding="utf-8"))
ui_en = strings(UI_EN)
ui_zh = strings(UI_ZH)
record_prompt_keys = {"AF_PERSONA_PROMPT_PromotedSkillNone"}
assert (PROMPT_KEYS | record_prompt_keys) <= set(prompt_en) == set(prompt_zh)
record_source = read("src/AF.GameAdapter.Bannerlord/Records/CampaignCharacterRecordCaptureAdapter.cs")
assert record_source.count('PersonaPromptTexts.Get("AF_PERSONA_PROMPT_PromotedSkillNone", "(none)")') == 2
assert prompt_zh["AF_PERSONA_PROMPT_PromotedSkillNone"] == "（无）"
assert prompt_en["AF_PERSONA_PROMPT_PromotedSkillNone"] == "(none)"
assert set(ui_en) == set(ui_zh) == UI_KEYS
for path in (PROMPT_EN, PROMPT_ZH, UI_EN, UI_ZH):
    raw = path.read_text(encoding="utf-8")
    assert not re.search(r"\\u[0-9a-fA-F]{4}", raw), path
for key in PROMPT_KEYS:
    assert prompt_en[key] and prompt_zh[key]
    assert not has_cjk_or_fullwidth(prompt_en[key]), key
    assert not NUMERIC.search(prompt_en[key] + prompt_zh[key]), key
    assert Counter(NAMED.findall(prompt_en[key])) == Counter(NAMED.findall(prompt_zh[key])), key
for key in UI_KEYS:
    assert ui_en[key] and ui_zh[key]
    assert not has_cjk_or_fullwidth(ui_en[key]), key
    assert Counter(NAMED.findall(ui_en[key])) == Counter(NAMED.findall(ui_zh[key])), key

prompt_corpus_zh = "\n".join(prompt_zh.values())
ui_corpus_zh = "\n".join(ui_zh.values())
# The generation system/user contract intentionally changed in schema v2; fixed
# labels are checked through the keyed CN/EN resources below rather than by the
# old whole-system fallback sentence.
assert all(value in ui_corpus_zh for value in old_import_literals)
assert all(value in ui_zh["AF_PERSONA_UI_UnknownNpc"] for value in old_readiness_literals)

prompt_rules = read(PROMPT_RULES)
assert "NpcPersonaGeneratedData" in prompt_rules and "ParseTraits" in prompt_rules
assert "internal static string NormalizePromotedSkillKey" in prompt_rules and "internal static NpcPersonaPrompt BuildNative" in prompt_rules
assert "internal static bool TryParseSkills" in prompt_rules
called_prompt_keys = {key for key in PROMPT_KEYS if any(f'PersonaPromptTexts.{method}("{key}"' in prompt_rules for method in ("Get", "Format"))}
assert called_prompt_keys == PROMPT_KEYS
prompt_code_literals = csharp_literals(prompt_rules)
for key in PROMPT_KEYS:
    assert prompt_en[key] in prompt_code_literals, key
assert "AppendNpcPersonaGenerationRequirementsToSystemPrompt" in prompt_rules
assert "TryParsePersonaJson" in prompt_rules and "TryParseSkills" in prompt_rules
assert "personality" in prompt_en["AF_PERSONA_PROMPT_NativeSystem"]
assert "background" in prompt_en["AF_PERSONA_PROMPT_NativeSystem"]
assert '"skills"' in prompt_en["AF_PERSONA_PROMPT_SkillSystem"]
skill_ids = ["OneHanded", "TwoHanded", "Polearm", "Bow", "Crossbow", "Throwing", "Riding", "Athletics", "Crafting", "Scouting", "Tactics", "Roguery", "Charm", "Leadership", "Trade", "Steward", "Medicine", "Engineering"]
for skill in skill_ids:
    assert skill in prompt_en["AF_PERSONA_PROMPT_AvailableSkillsLabel"]
    assert skill in prompt_zh["AF_PERSONA_PROMPT_AvailableSkillsLabel"]
for key in ("AF_PERSONA_PROMPT_NativeSystem", "AF_PERSONA_PROMPT_PromotedSystem", "AF_PERSONA_PROMPT_SkillSystem"):
    assert not re.search(r"(?i)(respond|reply|answer)\s+in\s+english|必须用英文|必须用中文", prompt_en[key])

sample = {
    "NAME": "NAME-测试",
    "TROOP_NAME": "TROOP-旧名",
    "FULL_NAME": "FULL-原始称呼",
    "SCENE": "SCENE-地点",
    "JOIN_FACT": "JOIN-原始事实 {kept}",
    "HISTORY": "HISTORY-原始历史 {kept}",
}
for language in (prompt_en, prompt_zh):
    personality = render(language["AF_PERSONA_PROMPT_FallbackPersonality"], {"NAME": sample["NAME"], "TROOP_NAME": sample["TROOP_NAME"]})
    background = render(language["AF_PERSONA_PROMPT_FallbackBackground"], {key: sample[key] for key in ("NAME", "FULL_NAME", "SCENE", "JOIN_FACT", "HISTORY")})
    assert sample["NAME"] in personality and sample["TROOP_NAME"] in personality
    for value in (sample["NAME"], sample["FULL_NAME"], sample["SCENE"], sample["JOIN_FACT"], sample["HISTORY"]):
        assert value in background, value
expected_personality_zh = sample["NAME"] + "曾是" + sample["TROOP_NAME"] + "，保留着原本职业养成的警觉、纪律和生存本能；面对玩家时，他会把加入前的承诺与利害放在心里，既愿意服从队伍安排，也会在危险或失信时变得谨慎。"
expected_background_zh = sample["JOIN_FACT"] + "他原本以“" + sample["FULL_NAME"] + "”的身份在" + sample["SCENE"] + "活动，加入后只以个人名“" + sample["NAME"] + "”示人。此前对话中可用的经历会成为他追随玩家的理由：" + sample["HISTORY"]
assert render(prompt_zh["AF_PERSONA_PROMPT_FallbackPersonality"], {"NAME": sample["NAME"], "TROOP_NAME": sample["TROOP_NAME"]}) == expected_personality_zh
assert render(prompt_zh["AF_PERSONA_PROMPT_FallbackBackground"], {key: sample[key] for key in ("NAME", "FULL_NAME", "SCENE", "JOIN_FACT", "HISTORY")}) == expected_background_zh

ui_sample = {
    "KEY": "hero__原始Key{KEEP}",
    "ERROR": "Provider detail {KEEP}: 未知尾部 AF_PERSONA_TEXT_VAR_0_END",
}
for language in (ui_en, ui_zh):
    duplicate = render(language["AF_PERSONA_UI_ImportDuplicateFolderKey"], {"KEY": ui_sample["KEY"]})
    conflict = render(language["AF_PERSONA_UI_ImportExistingKeyConflict"], {"KEY": ui_sample["KEY"]})
    detail = render(language["AF_PERSONA_UI_ImportValidationException"], {"ERROR": ui_sample["ERROR"]})
    assert ui_sample["KEY"] in duplicate and ui_sample["KEY"] in conflict
    assert ui_sample["ERROR"] in detail
    empty_detail = render(language["AF_PERSONA_UI_ImportValidationException"], {"ERROR": ""})
    assert empty_detail == language["AF_PERSONA_UI_ImportValidationException"].replace("{ERROR}", "")

current_sources = [read(path) for path in (PROMPT_RULES, IMPORT_RULES, READINESS, PROMPT_LOADER, UI_LOADER)]
assert all(not cjk_values(csharp_literals(source)) for source in current_sources)
prompt_loader = read(PROMPT_LOADER)
ui_loader = read(UI_LOADER)
assert "Lazy<Dictionary<string, string>>" in prompt_loader
assert 'GetModuleDataFilePath("PersonaPromptTexts.json")' in prompt_loader
assert all(value not in prompt_loader for value in ("TextObject", "CurrentLanguage", "ConversationUiLocalization"))
assert "TextObject" in ui_loader and "SetTextVariable" in ui_loader
assert "AF_PERSONA_TEXT_VAR_" in ui_loader and "string.Compare(resolved, offset" in ui_loader
snapshot = read(READINESS)
assert 'PersonaUiTexts.Text("AF_PERSONA_UI_UnknownNpc"' in snapshot
import_owner = read(IMPORT_RULES)
assert "Directory.Exists" in import_owner and "hashSet.Add" in import_owner and "hasExistingKey(text)" in import_owner
assert "ex.Message" in import_owner
ui_source = snapshot + import_owner
called_ui_keys = {key for key in UI_KEYS if any(f'PersonaUiTexts.{method}("{key}"' in ui_source for method in ("Text", "Format"))}
assert called_ui_keys == {key for key in UI_KEYS if not key.startswith(("AF_PERSONA_UI_CONTEXT_", "AF_PERSONA_UI_TRAIT_"))}
ui_code_literals = csharp_literals(ui_source)
for key in called_ui_keys:
    assert ui_en[key] in ui_code_literals, key
assert 'bool forUi)' in prompt_rules
assert 'PersonaUiTexts.Text(key.Replace("AF_PERSONA_", "AF_PERSONA_UI_"), fallback)' in prompt_rules
assert 'GetNpcPersonaContextForExternal(hero, forUi: true)' in read('src/AF.GameAdapter.Bannerlord/UI/Persona/EncyclopediaHeroPersonaPatch.cs')
for key in UI_KEYS - called_ui_keys:
    assert ui_en[key] == prompt_en[key.replace("AF_PERSONA_UI_", "AF_PERSONA_")]
    assert ui_zh[key] == prompt_zh[key.replace("AF_PERSONA_UI_", "AF_PERSONA_")]

adapter = read("src/AF.GameAdapter.Bannerlord/Prompt/NpcPersonaGenerationApplicationAdapter.cs")
assert "NpcPersonaTextRules.BuildNative" in adapter and "CallAuxiliaryGatewayDetailed" in adapter
assert "_dispatch(generation" in adapter
promoted = read("src/AF.GameAdapter.Bannerlord/Prompt/PromotedPersonaGenerationApplicationAdapter.cs")
assert "NpcPersonaTextRules.BuildPromoted" in promoted and "NpcPersonaTextRules.BuildSkills" in promoted
readiness_adapter = read("src/AF.GameAdapter.Bannerlord/Prompt/ScenePersonaPreparationAdapter.cs")
assert "TWParallel.IsMainThread()" in readiness_adapter and "NpcPersonaReadinessSnapshot" in readiness_adapter
import_adapter = read("src/AF.GameAdapter.Bannerlord/ImportExport/PersonaProfileImportExportAdapter.cs")
assert "ValidateUnnamedPersonaKeysForImport" in import_adapter and "InformationManager.DisplayMessage(new InformationMessage(error))" in import_adapter

content_map = json.loads(read("content/content-map.json"))
entries = content_map["entries"]
base_prompt_source = "content/modules/AF.Module.Persona/ModuleData/PersonaPromptTexts.json"
en_prompt_source = "content/modules/AF.Module.Persona/ModuleData/PersonaPromptTexts.en.json"
assert not any(entry.get("source") == en_prompt_source for entry in entries)
prompt_wired = any(entry.get("source") == base_prompt_source and entry.get("target") == "ModuleData/PersonaPromptTexts.json" for entry in entries)
ui_base = "content/modules/AF.Module.Persona/ModuleData/Languages/"
ui_en_target = "ModuleData/Languages/afpersona_ui_strings.xml"
ui_zh_source = "content/modules/AF.Module.Persona/ModuleData/Languages/CNs/afpersona_ui_strings-zh-CN.xml"
ui_zh_target = "ModuleData/Languages/CNs/afpersona_ui_strings-zh-CN.xml"
ui_en_wired = any(entry.get("source") == ui_base + "afpersona_ui_strings.xml" and entry.get("target") == ui_en_target for entry in entries)
ui_zh_wired = any(entry.get("source") == ui_zh_source and entry.get("target") == ui_zh_target for entry in entries)
lang_en = read("content/foundation/AF.Foundation.Localization/ModuleData/Languages/language_data.xml")
lang_zh = read("content/foundation/AF.Foundation.Localization/ModuleData/Languages/CNs/language_data.xml")
lang_en_wired = 'xml_path="afpersona_ui_strings.xml"' in lang_en
lang_zh_wired = 'xml_path="CNs/afpersona_ui_strings-zh-CN.xml"' in lang_zh
registration = "WIRED" if all((prompt_wired, ui_en_wired, ui_zh_wired, lang_en_wired, lang_zh_wired)) else "NOT-WIRED"
assert registration == "WIRED", "Persona UI and Prompt resources must reach the native/package loaders"
print(f"PASS Persona localization: source baseline Han literals=39 (33 prompt, 5 import UI, 1 fallback UI); package keys={len(PROMPT_KEYS)}; UI keys={len(UI_KEYS)}; registration={registration}; CSharp/runtime/replay=NOT-RUN")
