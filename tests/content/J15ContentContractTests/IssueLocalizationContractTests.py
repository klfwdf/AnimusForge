import json
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
BASELINE = "ebe438790"
PROMPT_OWNER = "src/modules/AF.Module.Issue/Runtime/IssueRuntimePromptOwner.cs"
DISPLAY_TEXTS = "src/modules/AF.Module.Issue/Presentation/IssueDisplayTexts.cs"
MATCH_CATALOG = "src/modules/AF.Module.Issue/Presentation/IssueDisplayMatchCatalog.cs"
PROMPT_TEXTS = "src/modules/AF.Module.Issue/Presentation/IssuePromptTexts.cs"
MATCH_XML = ROOT / "content/modules/AF.Module.Issue/ModuleData/IssueDisplayMatchSources.xml"
EN_XML = ROOT / "content/modules/AF.Module.Issue/ModuleData/Languages/afissue_prompt_strings.xml"
ZH_XML = ROOT / "content/modules/AF.Module.Issue/ModuleData/Languages/CNs/afissue_prompt_strings-zh-CN.xml"
CONV_EN_XML = ROOT / "content/modules/AF.Module.Conversation/ModuleData/Languages/afconv_ui_strings.xml"
CONV_ZH_XML = ROOT / "content/modules/AF.Module.Conversation/ModuleData/Languages/CNs/afconv_ui_strings-zh-CN.xml"
CJK = re.compile("[㐀-鿿]")


def read(rel):
    return (ROOT / rel).read_text(encoding="utf-8-sig")


def git_source(revision, rel):
    return subprocess.check_output(["git", "show", f"{revision}:{rel}"], cwd=ROOT).decode("utf-8")


def csharp_string_literals(source):
    result = []
    i = 0
    size = len(source)
    while i < size:
        if source.startswith("//", i):
            end = source.find("\n", i + 2)
            i = size if end < 0 else end + 1
            continue
        if source.startswith("/*", i):
            end = source.find("*/", i + 2)
            i = size if end < 0 else end + 2
            continue
        if source[i] == "'":
            i += 1
            while i < size:
                if source[i] == "\\":
                    i += 2
                elif source[i] == "'":
                    i += 1
                    break
                else:
                    i += 1
            continue
        prefix_start = i
        cursor = i
        while cursor < size and source[cursor] in "$@" and cursor - prefix_start < 2:
            cursor += 1
        if cursor >= size or source[cursor] != '"':
            if source[i] == '"':
                cursor = i
            else:
                i += 1
                continue
        verbatim = "@" in source[prefix_start:cursor]
        i = cursor + 1
        chars = []
        while i < size:
            char = source[i]
            if verbatim and char == '"' and i + 1 < size and source[i + 1] == '"':
                chars.append('"')
                i += 2
            elif not verbatim and char == "\\" and i + 1 < size:
                chars.append(source[i + 1])
                i += 2
            elif char == '"':
                i += 1
                break
            else:
                chars.append(char)
                i += 1
        result.append("".join(chars))
    return result


def cjk_literals(source):
    return [value for value in csharp_string_literals(source) if CJK.search(value)]


def strings(path):
    root = ET.parse(path).getroot()
    nodes = list(root.findall("./strings/string"))
    values = {node.get("id"): node.get("text", "") for node in nodes}
    assert len(nodes) == len(values), path
    return values


def prompt_method(source, start, end):
    return source.split(start, 1)[1].split(end, 1)[0].replace("\r\n", "\n")


def render(template, variables):
    result = template
    for name, value in variables.items():
        result = result.replace("{" + name + "}", value)
    return result


def preserve_line(line, replacement):
    leading = len(line) - len(line.lstrip())
    trailing = len(line) - len(line.rstrip())
    return line[:leading] + replacement + (line[len(line) - trailing:] if trailing else "")


def simulate_error_projection(raw, language, source_by_id, conversation_strings):
    if raw is None or not str(raw).strip():
        return raw
    prefix = source_by_id["TurnInProbeExceptionPrefix"]
    lines = str(raw).split("\n")
    result = []
    rows = [(source_by_id[source_id], key) for source_id, key, _ in ERROR_ROWS]
    for line in lines:
        trimmed = line.strip()
        replacement = None
        if trimmed.startswith(prefix):
            replacement = render(conversation_strings["AF_CONV_UI_Issue_TurnInProbeException"],
                                 {"REASON": trimmed[len(prefix):]})
        else:
            for literal, key in rows:
                if trimmed == literal:
                    replacement = conversation_strings[key]
                    break
        result.append(line if replacement is None else preserve_line(line, replacement))
    return "\n".join(result)


def numeric_payload(value, allow_decimal=False, allow_range=False, allow_percent=False):
    has_digit = False
    for index, char in enumerate(value):
        if char in "0123456789":
            has_digit = True
        elif allow_decimal and char in ".,":
            continue
        elif allow_range and char == "-":
            continue
        elif allow_percent and char == "%" and index == len(value) - 1:
            continue
        else:
            return False
    return has_digit


def simulate_candidate_line(raw, language, source_by_id, prompt_strings, conversation_strings):
    if raw is None or not str(raw).strip():
        return raw
    separator = " | "
    fields = str(raw).split(separator)
    if len(fields) < 2 or not fields[1].startswith("HeroId="):
        return raw
    name_prefix = "- "
    if fields[0].startswith(name_prefix):
        name = fields[0][len(name_prefix):]
        if name == source_by_id["UnknownCompanion"]:
            fields[0] = name_prefix + conversation_strings["AF_CONV_UI_Issue_UnknownCompanion"]
    for index in range(2, len(fields)):
        field = fields[index]
        value = None
        if field.startswith(source_by_id["CandidateSkillFallback"] + "="):
            candidate_value = field[len(source_by_id["CandidateSkillFallback"]) + 1:]
            if numeric_payload(candidate_value):
                value = render(prompt_strings["AF_ISSUE_PROMPT_CandidateSkill"], {"VALUE": candidate_value})
        elif field.startswith(source_by_id["CandidateFailureRiskLabel"]):
            candidate_value = field[len(source_by_id["CandidateFailureRiskLabel"]):]
            if numeric_payload(candidate_value, allow_decimal=True, allow_percent=True):
                value = render(prompt_strings["AF_ISSUE_PROMPT_CandidateRisk"], {"VALUE": candidate_value})
        elif field.startswith(source_by_id["CandidateDurationLabel"]) and field.endswith(source_by_id["CandidateDaysSuffix"]):
            candidate_value = field[len(source_by_id["CandidateDurationLabel"]):-len(source_by_id["CandidateDaysSuffix"])]
            if numeric_payload(candidate_value):
                value = render(prompt_strings["AF_ISSUE_PROMPT_CandidateDuration"], {"DAYS": candidate_value})
        elif field.startswith(source_by_id["CandidateTroopsLabel"]):
            candidate_value = field[len(source_by_id["CandidateTroopsLabel"]):]
            if numeric_payload(candidate_value):
                value = render(prompt_strings["AF_ISSUE_PROMPT_CandidateTroops"], {"VALUE": candidate_value})
        elif field.startswith(source_by_id["CandidateCasualtiesLabel"]):
            candidate_value = field[len(source_by_id["CandidateCasualtiesLabel"]):]
            if numeric_payload(candidate_value, allow_range=True):
                value = render(prompt_strings["AF_ISSUE_PROMPT_CandidateCasualties"], {"VALUE": candidate_value})
        if value is not None:
            fields[index] = value
    return separator.join(fields)


prompt_owner = read(PROMPT_OWNER)
display_texts = read(DISPLAY_TEXTS)
match_catalog = read(MATCH_CATALOG)
prompt_texts = read(PROMPT_TEXTS)
en = strings(EN_XML)
zh = strings(ZH_XML)
conv_en = strings(CONV_EN_XML)
conv_zh = strings(CONV_ZH_XML)
old_prompt_owner = git_source(BASELINE, PROMPT_OWNER)
old_display_texts = git_source(BASELINE, DISPLAY_TEXTS)
old_issue_actions = git_source(BASELINE, "src/modules/AF.Module.Issue/Actions/IssueActionOwner.cs")
old_issue_host = git_source(BASELINE, "src/modules/AF.Module.Issue/Host/VanillaIssueOfferBridge.cs")

old_prompt_cjk = cjk_literals(old_prompt_owner)
old_display_cjk = cjk_literals(old_display_texts)
assert len(old_prompt_cjk) == 51, len(old_prompt_cjk)
assert len(old_display_cjk) == 15, len(old_display_cjk)
assert not cjk_literals(prompt_owner), cjk_literals(prompt_owner)
assert not cjk_literals(display_texts), cjk_literals(display_texts)
assert not cjk_literals(match_catalog) and not cjk_literals(prompt_texts)

match_root = ET.parse(MATCH_XML).getroot()
assert match_root.tag == "IssueDisplayMatchSources" and match_root.get("schemaVersion") == "1"
match_nodes = list(match_root.findall("./Match"))
match_ids = [node.get("id") for node in match_nodes]
match_values = [node.text or "" for node in match_nodes]
source_by_id = dict(zip(match_ids, match_values))
expected_ids = re.findall(r'^\s*"([A-Za-z0-9_]+)"\s*,?$', match_catalog.split("ExpectedIds", 1)[1].split("};", 1)[0], re.M)
assert match_ids == expected_ids and len(match_ids) == len(set(match_ids))
original_match = ET.fromstring(git_source("9126eca27ad8bcd5c1120b58f220f9d428eab1a7", "content/modules/AF.Module.Issue/ModuleData/IssueDisplayMatchSources.xml"))
original_nodes = original_match.findall("./Match")
assert len(original_nodes) == 23
assert [(node.get("id"), node.text or "") for node in match_nodes[:23]] == [(node.get("id"), node.text or "") for node in original_nodes], "Original 23 values/order changed"
assert match_ids[23:] == [f"Raw_{index:03}" for index in range(23, 55)], "Unexpected added compatibility identity/order"
package_cn = json.loads(read("content/modules/AF.Module.Issue/ModuleData/IssuePromptTexts.json"))
package_en = json.loads(read("content/modules/AF.Module.Issue/ModuleData/IssuePromptTexts.en.json"))
assert package_cn.keys() == package_en.keys()
for key in package_cn:
    assert re.findall(r"\{[A-Z_][A-Z_0-9]*\}", package_cn[key]) == re.findall(r"\{[A-Z_][A-Z_0-9]*\}", package_en[key]), key
    assert re.findall(r"\d+", package_cn[key]) == re.findall(r"\d+", package_en[key]), key
    assert not CJK.search(package_en[key]), key
covered_added = set()
source_files = ["Actions/IssueActionOwner.cs", "Completion/IssueCompletionReceiptOwner.cs", "Host/VanillaIssueOfferBridge.cs", "Host/VanillaIssuePromptBehavior.cs", "Runtime/IssueRuntimeStateOwner.cs"]
for suffix in source_files:
    relative = "src/modules/AF.Module.Issue/" + suffix
    current, original = read(relative), git_source(BASELINE, relative)
    consumers = re.findall(r'IssueDisplayMatchCatalog.Get\("(Raw_\d+)"\)', current)
    for key in consumers:
        assert source_by_id[key] in csharp_string_literals(original), (relative, key, "no old literal source")
    covered_added.update(consumers)
    restored_source = re.sub(r'IssueDisplayMatchCatalog.Get\("([^"\n]+)"\)', lambda match: json.dumps(source_by_id[match[1]], ensure_ascii=False), current)
    restored_source = re.sub(r'IssuePromptTexts.Get\("([^"\n]+)"\)', lambda match: json.dumps(package_cn[match[1]], ensure_ascii=False), restored_source)
    # Technical log text has been translated, but categories, field order,
    # object IDs and exception payload expressions must remain exact.
    log_pattern = r'Logger\.Log\("Logic", ("(?:\\.|[^"\\])*")([^\n]*)'
    before_logs, after_logs = re.findall(log_pattern, original), re.findall(log_pattern, restored_source)
    assert len(before_logs) == len(after_logs), relative + ": log count changed"
    for (before_literal, before_tail), (after_literal, after_tail) in zip(before_logs, after_logs):
        assert before_tail == after_tail, relative + ": diagnostic payload changed"
        before_text, after_text = json.loads(before_literal), json.loads(after_literal)
        assert re.findall(r"\[[^\]]+\]|\b[A-Za-z_]+\s*=", before_text) == re.findall(r"\[[^\]]+\]|\b[A-Za-z_]+\s*=", after_text), relative + ": diagnostic identity/fields changed"
        if before_literal != after_literal:
            assert CJK.search(before_text) and not CJK.search(after_text), relative + ": unexpected non-translation log change"
            restored_source = restored_source.replace('Logger.Log("Logic", ' + after_literal + after_tail, 'Logger.Log("Logic", ' + before_literal + before_tail, 1)
    assert restored_source == original, relative + ": fact/recognition/business changed after exact resource restoration"
assert covered_added == set(match_ids[23:]), "An added original-value resource has no real consumer"

assert match_values[:15] == old_display_cjk

candidate_method = old_issue_actions.split("internal static bool TryBuildAlternativeCandidates", 1)[1].split("private static string GetUnavailableCompanionReason", 1)[0]
candidate_literals = cjk_literals(candidate_method)
candidate_unique = []
for value in candidate_literals:
    if value not in candidate_unique:
        candidate_unique.append(value)
assert candidate_unique == match_values[15:21], (candidate_unique, match_values[15:21])
unnamed_method = old_issue_host.split("private static string GetSafeIssueTitle", 1)[1].split("private static string GetHeroName", 1)[0]
journal_method = old_issue_host.split("private static void AppendRecentJournalLines", 1)[1].split("private static void ShowInfo", 1)[0]
assert cjk_literals(unnamed_method) == [match_values[21]]
assert cjk_literals(journal_method) == [match_values[22]]

assert set(en) == set(zh) and len(en) == 51
assert all(package_cn.get(key) == value for key, value in zh.items()), "Original 51 Chinese package values lost or changed"
assert all(package_en.get(key) == value for key, value in en.items()), "Original 51 English package values lost or changed"
assert all(key.startswith("AF_ISSUE_PROMPT_") for key in en)
for key in en:
    assert en[key] and zh[key]
    assert not CJK.search(en[key]), key
    assert re.findall(r"\{([A-Z][A-Z0-9_]*)\}", en[key]) == re.findall(r"\{([A-Z][A-Z0-9_]*)\}", zh[key]), key

used_prompt = dict(re.findall(r'IssuePromptTexts\.(?:Text|Format)\("(AF_ISSUE_PROMPT_[A-Za-z0-9_]+)"\s*,\s*"((?:\\.|[^"\\])*)"', prompt_owner + display_texts))
assert set(used_prompt) == set(en), sorted(set(en) ^ set(used_prompt))
for key, fallback in used_prompt.items():
    assert json.loads('"' + fallback + '"') == en[key], key

error_table = display_texts.split("ExactMessages =", 1)[1].split("\n\t};", 1)[0]
ERROR_ROWS = [(source_id, key, json.loads('"' + fallback + '"')) for source_id, key, fallback in re.findall(
    r'\("([A-Za-z0-9]+)"\s*,\s*"(AF_CONV_UI_Issue_[A-Za-z0-9_]+)"\s*,\s*"((?:\\.|[^"\\])*)"\)', error_table)]
assert len(ERROR_ROWS) == 13
for source_id, key, fallback in ERROR_ROWS:
    assert key in conv_en and key in conv_zh
    assert conv_en[key] == fallback, key
    assert conv_zh[key] == source_by_id[source_id], key
assert re.findall(r"\{([A-Z][A-Z0-9_]*)\}", conv_en["AF_CONV_UI_Issue_TurnInProbeException"]) == ["REASON"]
assert re.findall(r"\{([A-Z][A-Z0-9_]*)\}", conv_zh["AF_CONV_UI_Issue_TurnInProbeException"]) == ["REASON"]

old_offer = old_prompt_owner.split("private static string BuildOfferPromptBlock", 1)[1].split("private static string BuildInProgressPromptBlock", 1)[0]
dead_values = [value for value in re.findall(r'\btext6\s*=\s*"((?:\\.|[^"\\])*)"\s*;', old_offer) if CJK.search(value)]
assert len(dead_values) == 1 and dead_values[0] in old_prompt_cjk
assert not re.search(r"AppendLine\s*\([^)]*\btext6\b", old_offer)
visible_old_prompt = [value for value in old_prompt_cjk if value != dead_values[0]]
for value in visible_old_prompt:
    normalized = value.replace("{text}", "{GIVER}")
    assert any(normalized in localized for localized in zh.values()), value

for key in ("AF_ISSUE_PROMPT_OfferEligible", "AF_ISSUE_PROMPT_OfferReject", "AF_ISSUE_PROMPT_OfferAlternativeConsent",
            "AF_ISSUE_PROMPT_OfferAskCompanion", "AF_ISSUE_PROMPT_OfferCandidateList", "AF_ISSUE_PROMPT_InProgressInstruction",
            "AF_ISSUE_PROMPT_TurnInReady", "AF_ISSUE_PROMPT_TurnInUnclear", "AF_ISSUE_PROMPT_RecentScope",
            "AF_ISSUE_PROMPT_RecentOtherQuests"):
    assert key in en and key in zh
assert "post-processing" in en["AF_ISSUE_PROMPT_OfferEligible"]
assert "ask a follow-up question" in en["AF_ISSUE_PROMPT_OfferAskCompanion"]
assert "never invent a companion" in en["AF_ISSUE_PROMPT_OfferCandidateList"]
assert "do not state that the quest has been inspected and completed" in en["AF_ISSUE_PROMPT_TurnInUnclear"]
required_tags = ["[ACTION:ISSUE_ACCEPT_SELF]", "[ACTION:ISSUE_ACCEPT_ALT:*]", "[ACTION:QUEST_TURN_IN]"]
assert re.findall(r"\[ACTION:[^\]]+\]", en["AF_ISSUE_PROMPT_NoActionTags"]) == required_tags
assert re.findall(r"\[ACTION:[^\]]+\]", zh["AF_ISSUE_PROMPT_NoActionTags"]) == required_tags
assert set(required_tags).issubset(set(re.findall(r"\[ACTION:[^\]]+\]", "\n".join(old_prompt_cjk))))

old_postprocess = prompt_method(old_prompt_owner, "internal static List<PostprocessRuleEntry> BuildRuntimePostprocessRules", "internal static bool TryBuildRuntimePromptBlock")
new_postprocess = prompt_method(prompt_owner, "internal static List<PostprocessRuleEntry> BuildRuntimePostprocessRules", "internal static bool TryBuildRuntimePromptBlock")
assert old_postprocess == new_postprocess
assert "if (flag)" in prompt_owner and "if (flag2 && flag3)" in prompt_owner
assert "!issueQuest.IsOngoing" in prompt_owner and "probe == null" in prompt_owner
assert "consumeOnRead: true" in prompt_owner
assert "AppendRecentJournalLinesForPrompt" in prompt_owner
assert "ports.BuildTriggeredRules(contextFlags)" in read("src/AF.GameAdapter.Bannerlord/Composition/PromptContextCaptureBannerlordAdapter.cs")
assert "Full game-thread capture/effect sequence" in read("src/AF.GameAdapter.Bannerlord/Composition/PromptContextCaptureBannerlordAdapter.cs")
assert "Game-thread capture of every runtime rule body" in read("src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs")
assert "await " not in prompt_owner and "Task.Run" not in prompt_owner

source_by_key = {key: value for key, value in zip(match_ids, match_values)}
for source_id, key, _ in ERROR_ROWS:
    raw = source_by_key[source_id]
    assert simulate_error_projection(raw, en, source_by_key, conv_en) == conv_en[key]
    assert simulate_error_projection(raw, zh, source_by_key, conv_zh) == conv_zh[key]
exception_text = source_by_key["TurnInProbeExceptionPrefix"] + "unique exception detail"
assert simulate_error_projection(exception_text, en, source_by_key, conv_en) == render(conv_en["AF_CONV_UI_Issue_TurnInProbeException"], {"REASON": "unique exception detail"})
unknown_error = source_by_key["NoQuestOrGiver"] + " / new producer detail"
assert simulate_error_projection(unknown_error, en, source_by_key, conv_en) == unknown_error
assert simulate_error_projection("", en, source_by_key, conv_en) == ""
assert simulate_error_projection(None, zh, source_by_key, conv_zh) is None

real_cjk_name = source_by_key["UnknownCompanion"] + " Q"
assert real_cjk_name != source_by_key["UnknownCompanion"]
assert conv_en["AF_CONV_UI_Issue_UnknownCompanion"] != source_by_key["UnknownCompanion"]
assert conv_zh["AF_CONV_UI_Issue_UnknownCompanion"] != source_by_key["UnknownCompanion"]

candidate = "- " + source_by_key["UnknownCompanion"] + " | HeroId=comp_1 | " + source_by_key["CandidateSkillFallback"] + "=53 | " + source_by_key["CandidateFailureRiskLabel"] + "3.5% | " + source_by_key["CandidateDurationLabel"] + "5" + source_by_key["CandidateDaysSuffix"] + " | " + source_by_key["CandidateTroopsLabel"] + "4 | " + source_by_key["CandidateCasualtiesLabel"] + "1-2"
projected_candidate_en = simulate_candidate_line(candidate, en, source_by_key, en, conv_en)
assert "HeroId=comp_1" in projected_candidate_en and "Skill=53" in projected_candidate_en
assert "Failure risk=3.5%" in projected_candidate_en and "Estimated duration=5 days" in projected_candidate_en
assert "Required soldiers=4" in projected_candidate_en and "Estimated casualties=1-2" in projected_candidate_en
assert conv_en["AF_CONV_UI_Issue_UnknownCompanion"] in projected_candidate_en
custom_candidate = "- " + real_cjk_name + " | HeroId=comp_2 | " + source_by_key["CandidateTroopsLabel"] + "text"
custom_projected = simulate_candidate_line(custom_candidate, en, source_by_key, en, conv_en)
assert custom_projected.startswith("- " + real_cjk_name + " | HeroId=comp_2 | ")
assert custom_projected.endswith(source_by_key["CandidateTroopsLabel"] + "text")
malformed_candidate = "unknown custom candidate text"
assert simulate_candidate_line(malformed_candidate, en, source_by_key, en, conv_en) == malformed_candidate

replay = read("tests/replay/PhaseEightParityReplayTests/IssueActionOwnerReplay.cs")
assert "candidates[1] is not IList list || list.Count != 0" in replay
assert "positive < 90 || negative >= 0" in replay

content_map = json.loads(read("content/content-map.json"))
csproj = read("AnimusForge.csproj")
lang_en = read("content/foundation/AF.Foundation.Localization/ModuleData/Languages/language_data.xml")
lang_zh = read("content/foundation/AF.Foundation.Localization/ModuleData/Languages/CNs/language_data.xml")
map_sources = {entry.get("source", "") for entry in content_map.get("entries", [])}
resource_name = "AnimusForge.Issue.DisplayMatchSources.xml"
registration_ready = (
    resource_name in csproj
    and "afissue_prompt_strings.xml" in lang_en
    and "CNs/afissue_prompt_strings-zh-CN.xml" in lang_zh
    and "content/modules/AF.Module.Issue/ModuleData/Languages/afissue_prompt_strings.xml" in map_sources
    and "content/modules/AF.Module.Issue/ModuleData/Languages/CNs/afissue_prompt_strings-zh-CN.xml" in map_sources
)
print(f"PASS IssueLocalizationContractTests baseline={BASELINE} prompt_literals=51 dead_unemitted=1 display_literals=15 match_sources={len(match_ids)} prompt_keys={len(en)} cases=simulation registration={'WIRED' if registration_ready else 'NOT-WIRED'}; CSharp/runtime/replay=NOT-RUN")
