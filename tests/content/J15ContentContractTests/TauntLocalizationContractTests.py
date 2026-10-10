"""Static/simulation contract for the Taunt runtime localization slice."""
from collections import Counter
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
BASELINE = "8a29ce5f49a76230006cb508fd511d00a135bd8a"
SOURCE = "src/modules/AF.Module.Taunt/Host/SceneTauntBehavior.cs"
PROMPT_LOADER = "src/modules/AF.Module.Taunt/Host/TauntPromptTexts.cs"
SOURCE_LOADER = "src/modules/AF.Module.Taunt/Host/TauntSourceTexts.cs"
PROMPT_BASE = "content/modules/AF.Module.Taunt/ModuleData/TauntPromptTexts.json"
PROMPT_EN = ROOT / "content/modules/AF.Module.Taunt/ModuleData/TauntPromptTexts.en.json"
PROMPT_ZH = ROOT / PROMPT_BASE
SOURCE_JSON = ROOT / "content/modules/AF.Module.Taunt/ModuleData/TauntSourceTexts.json"
UI_EN_XML = ROOT / "content/modules/AF.Module.UI/ModuleData/Languages/af_ui_strings.xml"
UI_ZH_XML = ROOT / "content/modules/AF.Module.UI/ModuleData/Languages/CNs/af_ui_strings-zh-CN.xml"
PROMPT_KEYS = {
    "AF_TAUNT_PROMPT_PlayerFallback",
    "AF_TAUNT_PROMPT_ChildTarget",
    "AF_TAUNT_PROMPT_CriminalConflict",
    "AF_TAUNT_PROMPT_AlreadyWarnedLord",
    "AF_TAUNT_PROMPT_AlreadyWarnedOther",
    "AF_TAUNT_PROMPT_NotWarnedLord",
    "AF_TAUNT_PROMPT_NotWarnedOther",
    "AF_TAUNT_PROMPT_MeetingContext",
    "AF_TAUNT_PROMPT_MeetingFightEffect",
    "AF_TAUNT_PROMPT_LordContext",
    "AF_TAUNT_PROMPT_LordFightEffect",
    "AF_TAUNT_PROMPT_NpcContext",
    "AF_TAUNT_PROMPT_NpcFightEffect",
}
NAMED = re.compile(r"\{([A-Za-z][A-Za-z0-9_]*)\}")
NUMERIC = re.compile(r"\{\d+\}")
ACTION = re.compile(r"\[ACTION:[^\]]+\]")

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


def decode_char(raw):
    controls = {"n": "\n", "r": "\r", "t": "\t", "0": "\0"}
    return ESCAPE.sub(lambda m: chr(int(next(x for x in m.groups()[:3] if x), 16))
                      if any(m.groups()[:3]) else controls.get(m[4], m[4]), raw[1:-1])


def literal_tokens(source):
    found = []
    for match in LEX.finditer(source):
        if match.lastgroup == "string":
            kind, value = "string", decode(match[0])
        elif match.lastgroup == "char":
            kind, value = "char", decode_char(match[0])
        else:
            continue
        if has_cjk_or_fullwidth(value):
            found.append((match.start(), source.count("\n", 0, match.start()) + 1, kind, value))
    return found


def csharp_literals(source):
    values = []
    for match in LEX.finditer(source):
        if match.lastgroup == "string":
            values.append(decode(match[0]))
        elif match.lastgroup == "char":
            values.append(decode_char(match[0]))
    return values


def mask_literals(source):
    masked = list(source)
    for match in LEX.finditer(source):
        if match.lastgroup in ("comment", "string", "char"):
            for offset in range(match.start(), match.end()):
                if masked[offset] != "\n":
                    masked[offset] = " "
    return "".join(masked)


def method_region(source, signature):
    masked = mask_literals(source)
    start = masked.index(signature)
    opening = masked.index("{", start)
    depth = 0
    for offset in range(opening, len(masked)):
        if masked[offset] == "{":
            depth += 1
        elif masked[offset] == "}":
            depth -= 1
            if depth == 0:
                return source[start:offset + 1].replace("\r\n", "\n")
    raise AssertionError("unclosed method " + signature)


def if_conditions(source):
    masked = mask_literals(source)
    conditions = []
    for match in re.finditer(r"\bif\s*\(", masked):
        opening = masked.index("(", match.start())
        depth = 1
        for offset in range(opening + 1, len(masked)):
            if masked[offset] == "(":
                depth += 1
            elif masked[offset] == ")":
                depth -= 1
                if depth == 0:
                    conditions.append(re.sub(r"\s+", " ", masked[opening + 1:offset]).strip())
                    break
        else:
            raise AssertionError("unclosed if condition")
    return conditions


def read(relative):
    return (ROOT / relative).read_text(encoding="utf-8-sig")


def git_source(relative):
    return subprocess.check_output(["git", "show", f"{BASELINE}:{relative}"], cwd=ROOT).decode("utf-8")


def strings(path):
    nodes = list(ET.parse(path).getroot().findall(".//string"))
    values = {node.get("id"): node.get("text", "") for node in nodes}
    assert len(nodes) == len(values), path
    return values


def render(template, variables):
    return NAMED.sub(lambda match: variables.get(match.group(1), match.group(0)), template)


def call_ids(source, owner, methods):
    ids = []
    for method in methods:
        needle = owner + "." + method + "(\""
        cursor = 0
        while True:
            start = source.find(needle, cursor)
            if start < 0:
                break
            value_start = start + len(needle)
            value_end = source.find("\"", value_start)
            if value_end < 0:
                break
            ids.append(source[value_start:value_end])
            cursor = value_end + 1
    return ids


def extract_tags(values):
    return Counter(ACTION.findall("\n".join(values)))


old_source = git_source(SOURCE)
old_literals = literal_tokens(old_source)
han_literals = [item for item in old_literals if HAN.search(item[3])]
punctuation_only = [item for item in old_literals if not HAN.search(item[3])]
assert len(han_literals) == 119 and len({line for _, line, _, _ in han_literals}) == 73
assert len(punctuation_only) == 8 and len(old_literals) == 127

scene_prompt_start = old_source.index("internal static string BuildSceneTauntRuntimeInstructionForExternal(CharacterObject")
scene_prompt_end = old_source.index("internal static string BuildUnifiedTauntRuntimeInstructionForExternal", scene_prompt_start)
token_prompt_start = old_source.index("internal static Dictionary<string, string> BuildTauntRuntimeTokens")
token_prompt_end = old_source.index("private static void RememberSceneTauntWarning", token_prompt_start)
prompt_spans = ((scene_prompt_start, scene_prompt_end), (token_prompt_start, token_prompt_end))
prompt_literals = [item for item in old_literals if any(start <= item[0] < end for start, end in prompt_spans)]
assert len(prompt_literals) == 18 and all(HAN.search(item[3]) for item in prompt_literals)

dead_constant_start = old_source.index("internal const string WantedSceneExitNotice")
dead_constant_end = old_source.index(";", dead_constant_start) + 1
dead_constant_literals = [item for item in old_literals if dead_constant_start <= item[0] < dead_constant_end]
dead_nearby = method_region(old_source, "private void TryAppendNearbyArmedEscalationBehaviorFacts()")
dead_nearby_start = old_source.index("private void TryAppendNearbyArmedEscalationBehaviorFacts()")
dead_nearby_literals = [item for item in old_literals if dead_nearby_start <= item[0] < dead_nearby_start + len(dead_nearby)]
assert len(dead_constant_literals) == len(dead_nearby_literals) == 1
assert dead_constant_literals[0][3] == "你现在正在被通缉，无法离开当前场景！请立刻跑出地图边缘红区，摆脱追击。"
assert dead_nearby_literals[0][3] == "玩家"

dead_offsets = {item[0] for item in dead_constant_literals + dead_nearby_literals}
prompt_offsets = {item[0] for item in prompt_literals}
raw_literals = [item for item in old_literals if item[0] not in dead_offsets | prompt_offsets]
assert len(raw_literals) == 107

prompt_en = json.loads(PROMPT_EN.read_text(encoding="utf-8"))
prompt_zh = json.loads(PROMPT_ZH.read_text(encoding="utf-8"))
source_texts = json.loads(SOURCE_JSON.read_text(encoding="utf-8"))
assert set(prompt_en) == set(prompt_zh) == PROMPT_KEYS
assert source_texts and all(isinstance(value, str) and value for value in source_texts.values())
for path in (PROMPT_EN, PROMPT_ZH, SOURCE_JSON):
    raw = path.read_text(encoding="utf-8")
    assert not re.search(r"\\u[0-9a-fA-F]{4}", raw), path
for key in PROMPT_KEYS:
    assert prompt_en[key] and prompt_zh[key]
    assert not has_cjk_or_fullwidth(prompt_en[key]), key
    assert not NUMERIC.search(prompt_en[key] + prompt_zh[key]), key
    assert Counter(NAMED.findall(prompt_en[key])) == Counter(NAMED.findall(prompt_zh[key])), key

prompt_corpus_zh = "\n".join(prompt_zh.values())
source_corpus = "\n".join(source_texts.values())
assert all(value in prompt_corpus_zh for _, _, _, value in prompt_literals)
missing_raw = [(line, value) for _, line, _, value in raw_literals if value not in source_corpus]
assert not missing_raw, missing_raw
assert extract_tags([value for _, _, _, value in prompt_literals]) == extract_tags(prompt_zh.values()) == extract_tags(prompt_en.values())
assert set(ACTION.findall(prompt_corpus_zh)) == {"[ACTION:SCENE_TAUNT_WARN]", "[ACTION:SCENE_TAUNT_FIGHT]"}
assert not any(re.search(r"(?i)(respond|reply|answer)\s+in\s+english|必须用英文|必须用中文", value) for value in prompt_en.values())

current_source = read(SOURCE)
prompt_loader = read(PROMPT_LOADER)
source_loader = read(SOURCE_LOADER)
current_csharp = [current_source, prompt_loader, source_loader]
assert all(not literal_tokens(code) for code in current_csharp)
assert call_ids(current_source, "TauntPromptTexts", ("Get", "Format"))
used_prompt_keys = set(call_ids(current_source, "TauntPromptTexts", ("Get", "Format")))
assert used_prompt_keys == PROMPT_KEYS
used_source_keys = set(call_ids(current_source, "TauntSourceTexts", ("Get", "Format")))
assert used_source_keys == set(source_texts), (set(source_texts) - used_source_keys, used_source_keys - set(source_texts))
current_literals = csharp_literals(current_source)
for key in PROMPT_KEYS:
    assert prompt_en[key] in current_literals, key
assert "Lazy<Dictionary<string, string>>" in prompt_loader
assert 'GetModuleDataFilePath("TauntPromptTexts.json")' in prompt_loader
assert all(name not in prompt_loader for name in ("TextObject", "CurrentLanguage", "SceneTauntUiLocalization"))
assert "Lazy<Dictionary<string, string>>" in source_loader
assert 'GetManifestResourceStream(ResourceName)' in source_loader
assert "AnimusForge.Taunt.RuntimeSources.json" in source_loader

old_prompt_method = method_region(old_source, "internal static string BuildSceneTauntRuntimeInstructionForExternal(Hero targetHero")
current_prompt_method = method_region(current_source, "internal static string BuildSceneTauntRuntimeInstructionForExternal(Hero targetHero")
assert if_conditions(old_prompt_method) == if_conditions(current_prompt_method)
old_token_method = method_region(old_source, "internal static Dictionary<string, string> BuildTauntRuntimeTokens")
current_token_method = method_region(current_source, "internal static Dictionary<string, string> BuildTauntRuntimeTokens")
assert if_conditions(old_token_method) == if_conditions(current_token_method)

def token_assignments(method):
    return re.findall(r'dictionary\["(warnTag|fightTag)"\]\s*=\s*"([A-Z_]+)"', method)

expected_token_assignments = [
    ("warnTag", "MEETING_TAUNT_BATTLE"),
    ("fightTag", "MEETING_TAUNT_BATTLE"),
    ("warnTag", "SCENE_TAUNT_WARN"),
    ("fightTag", "SCENE_TAUNT_FIGHT"),
    ("warnTag", "SCENE_TAUNT_WARN"),
    ("fightTag", "SCENE_TAUNT_FIGHT"),
]
assert token_assignments(old_token_method) == token_assignments(current_token_method) == expected_token_assignments

old_conditions = Counter(if_conditions(old_source))
old_conditions["string.IsNullOrWhiteSpace(text)"] -= 1
if old_conditions["string.IsNullOrWhiteSpace(text)"] == 0:
    del old_conditions["string.IsNullOrWhiteSpace(text)"]
assert Counter(if_conditions(current_source)) == old_conditions
current_dead_nearby = method_region(current_source, "private void TryAppendNearbyArmedEscalationBehaviorFacts()")
assert "BuildPlayerPublicDisplayNameForExternal" not in current_dead_nearby
assert "BuildGuardReactionFactText()" in current_dead_nearby
assert not re.search(r"\bconst\s+string\s+WantedSceneExitNotice\b", current_source)
assert 'SceneTauntUiLocalization.Text("AF_TAUNT_UI_WantedExitBlocked"' in current_source

ui_en = strings(UI_EN_XML)
ui_zh = strings(UI_ZH_XML)
assert ui_en["AF_TAUNT_UI_WantedExitBlocked"] == "You are wanted and cannot leave this scene. Run beyond the red map-edge zone to escape pursuit."
assert ui_zh["AF_TAUNT_UI_WantedExitBlocked"] == dead_constant_literals[0][3]

sample_player = "玩家-unknown{PLAYER}"
for language in (prompt_en, prompt_zh):
    blank_player = " "
    fallback_player = language["AF_TAUNT_PROMPT_PlayerFallback"] if not blank_player.strip() else blank_player
    assert fallback_player == language["AF_TAUNT_PROMPT_PlayerFallback"]
    output = render(language["AF_TAUNT_PROMPT_CriminalConflict"], {"PLAYER": sample_player})
    assert sample_player in output
    assert render(language["AF_TAUNT_PROMPT_ChildTarget"], {})
    assert render(language["AF_TAUNT_PROMPT_NotWarnedLord"], {"PLAYER": sample_player})


def simulate_prompt(language, child=False, applicable=True, criminal=False, warned=False, is_lord=False, player=""):
    player = player if player and player.strip() else language["AF_TAUNT_PROMPT_PlayerFallback"]
    if child:
        return language["AF_TAUNT_PROMPT_ChildTarget"]
    if not applicable:
        return ""
    if criminal:
        return render(language["AF_TAUNT_PROMPT_CriminalConflict"], {"PLAYER": player})
    if warned:
        key = "AF_TAUNT_PROMPT_AlreadyWarnedLord" if is_lord else "AF_TAUNT_PROMPT_AlreadyWarnedOther"
    else:
        key = "AF_TAUNT_PROMPT_NotWarnedLord" if is_lord else "AF_TAUNT_PROMPT_NotWarnedOther"
    return render(language[key], {"PLAYER": player})


for language in (prompt_en, prompt_zh):
    assert simulate_prompt(language, child=True)
    assert simulate_prompt(language, applicable=False) == ""
    assert language["AF_TAUNT_PROMPT_PlayerFallback"] in simulate_prompt(language, player=None)
    assert language["AF_TAUNT_PROMPT_PlayerFallback"] in simulate_prompt(language, player="   ")
    assert sample_player in simulate_prompt(language, criminal=True, player=sample_player)
    assert sample_player in simulate_prompt(language, warned=True, is_lord=True, player=sample_player)
    assert sample_player in simulate_prompt(language, warned=False, is_lord=False, player=sample_player)

raw_player = "玩家{RAW}"
knockdown = render(source_texts["AFEF_OwnedSettlementKnockdown"], {"PLAYER": raw_player})
expected_knockdown = "[AFEF NPC行为补充] " + raw_player + "在自己的定居点内把你击倒了。这里属于" + raw_player + "的家族，" + raw_player + "是此地领主/主人；你已经被领主打倒，正在原地抱头，不会反击。你必须把" + raw_player + "当作你的领主大人来回应，只能恐惧、服从、求饶、认错，说出类似“领主大人饶命”“求您宽恕我”的话。不要强硬回嘴、威胁、挑衅、摆贵族架子、宣称对方不敢动你、指责对方会遭报应，也不要用民族或国家立场顶嘴。不要生成犯罪值相关内容。"
assert knockdown == expected_knockdown
assert render(source_texts["AFEF_DirectArmed"], {"PLAYER": raw_player, "WEAPON": "刀{RAW}"}) == "[AFEF NPC行为补充] ，" + raw_player + "一刀砍向了你，而你现在也拔出了刀{RAW}与" + raw_player + "厮杀了起来。"
assert render(source_texts["PlayerSceneRecentAction"], {"SETTLEMENT": "某城", "LOCATION": "的市集", "VERB": source_texts["ActionVerbDamage"], "VICTIM": "某人{RAW}"}) == "你在某城的市集攻击并打伤了某人{RAW}。"
assert render(source_texts["DayFallback"], {"DAY": "17"}) == "第 17 日"

scene_history_capture = read("src/AF.GameAdapter.Bannerlord/Prompt/SceneHistoryPromptCaptureAdapter.cs")
prompt_rule_capture = read("src/AF.GameAdapter.Bannerlord/Prompt/PromptRuleCaptureBannerlordAdapter.cs")
prompt_context_capture = read("src/AF.GameAdapter.Bannerlord/Composition/PromptContextCaptureBannerlordAdapter.cs")
assert "SceneTauntBehavior.BuildSceneTauntRuntimeInstructionForExternal" in scene_history_capture
assert "SceneTauntBehavior.BuildUnifiedTauntRuntimeInstructionForExternal" in prompt_rule_capture
assert "ports.BuildTriggeredRules(contextFlags)" in prompt_context_capture
assert "Full game-thread capture/effect sequence" in prompt_context_capture

content_map = json.loads(read("content/content-map.json"))
entries = content_map["entries"]
prompt_wired = any(entry.get("source") == PROMPT_BASE and entry.get("target") == "ModuleData/TauntPromptTexts.json" for entry in entries)
en_source = PROMPT_BASE[:-5] + ".en.json"
assert not any(entry.get("source") == en_source for entry in entries)
csproj = read("AnimusForge.csproj")
source_include = 'EmbeddedResource Include="content\\modules\\AF.Module.Taunt\\ModuleData\\TauntSourceTexts.json"'
source_wired = source_include in csproj and "<LogicalName>AnimusForge.Taunt.RuntimeSources.json</LogicalName>" in csproj
registration = "WIRED" if prompt_wired and source_wired else "NOT-WIRED"

if "--dump" in sys.argv:
    for _, line, kind, value in old_literals:
        print(f"{line}\t{kind}\t{value!r}")
    print(f"Taunt baseline Han={len(han_literals)} punctuation-only={len(punctuation_only)} prompt={len(prompt_literals)} raw-source={len(raw_literals)} dead={len(dead_constant_literals) + len(dead_nearby_literals)}")
else:
    print(f"PASS Taunt localization: baseline Han literals=119, punctuation-only=8; translated source literals=18 across {len(PROMPT_KEYS)} prompt keys; original-value-only=107; dead values removed=2; action tags/branch conditions/raw facts preserved; registration={registration}; CSharp/runtime/replay=NOT-RUN")
