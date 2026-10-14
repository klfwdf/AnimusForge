"""Offline guards for the approved diplomacy writing-style change.

Checks package resources and real source consumers, preserving selected
mechanical contracts against the intent checkpoint. This is not a C# replay,
provider-quality evaluation, or live-game acceptance test.
"""
from pathlib import Path
from functools import cache
import json
import re
import subprocess


ROOT = Path(__file__).resolve().parents[3]
# This is a change-audit guard, like the adjacent historical contract tests.
# A source ZIP or shallow checkout without this object cannot prove preservation.
BASELINE = "76fc6ed40b1d4a6a81959a77dcf8d7f1955f2956"
DOMAIN = "src/modules/AF.Module.Diplomacy/Domain/"
APP = "src/modules/AF.Module.Diplomacy/Application/"
DATA = "content/modules/AF.Module.Diplomacy/ModuleData/"
CHECKS = 0


def require(condition, description):
    global CHECKS
    CHECKS += 1
    if not condition:
        raise AssertionError(description)


def read(path):
    return (ROOT / path).read_text(encoding="utf-8-sig").replace("\r\n", "\n")


@cache
def old(path):
    result = subprocess.run(
        ["git", "show", BASELINE + ":" + path], cwd=ROOT,
        capture_output=True, check=False,
    )
    if result.returncode:
        raise SystemExit("BLOCKED_ENV: writing-style change audit requires Git checkpoint "
                         + BASELINE + " and path " + path + "; no preservation PASS was produced.")
    return result.stdout.decode("utf-8-sig").replace("\r\n", "\n")


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, "duplicate resource key: " + key)
        result[key] = value
    return result


def package(name):
    return json.loads(read(DATA + name), object_pairs_hook=unique_object)


CS_NON_CODE = re.compile(
    r'@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\''
    r"|//[^\n]*|/\*[\s\S]*?\*/"
)


def method(source, name):
    """Get the one block-bodied overload without counting braces in strings."""
    masked = CS_NON_CODE.sub(lambda m: " " * len(m[0]), source)
    declaration = re.compile(
        r"\b(?:public|private|internal)\s+(?:static\s+)?[^;{}\n]*\b"
        + re.escape(name) + r"\s*\("
    )
    bodies = []
    for match in declaration.finditer(masked):
        start = match.end()
        delimiter = re.search(r"[;{]", masked[start:])
        if delimiter is None or delimiter[0] == ";":
            continue
        opening = start + delimiter.start()
        depth, end = 1, opening + 1
        while depth:
            depth += (masked[end] == "{") - (masked[end] == "}")
            end += 1
        bodies.append(source[opening:end])
    require(len(bodies) == 1, name + ": unique block-bodied production method")
    return bodies[0]


def unchanged_methods(path, names):
    before, after = old(path), read(path)
    for name in names:
        require(method(after, name) == method(before, name), name + ": preserved behavior")


prompt_cn = package("DiplomacyPromptTexts.json")
prompt_en = package("DiplomacyPromptTexts.en.json")
misc_cn = package("DiplomacyMiscPromptTexts.json")
misc_en = package("DiplomacyMiscPromptTexts.en.json")
for label, cn, en in (("prompt", prompt_cn, prompt_en), ("misc", misc_cn, misc_en)):
    require(cn.keys() == en.keys(), label + ": identical CN/EN keys")
    for key in cn:
        require(isinstance(cn[key], str) and isinstance(en[key], str), key + ": string resources")
        require(set(re.findall(r"\{[A-Z][A-Z0-9_]*\}", cn[key]))
                == set(re.findall(r"\{[A-Z][A-Z0-9_]*\}", en[key])), key + ": placeholders")

for key in ("Declaration_DefaultStyle", "Declaration_SemanticStance"):
    require(bool(misc_cn[key].strip()) and bool(misc_en[key].strip()), key + ": nonempty pair")
    require(not re.search(r"[\u3400-\u9fff]", misc_en[key]), key + ": English projection")
require("玩家的自定义文风优先" in misc_cn["Declaration_DefaultStyle"]
        and "player writing preferences take precedence" in misc_en["Declaration_DefaultStyle"],
        "shared default does not override custom writing preferences")
require("讽刺" in misc_cn["Declaration_SemanticStance"]
        and "sarcasm" in misc_en["Declaration_SemanticStance"]
        and "hostile" in misc_cn["Declaration_SemanticStance"]
        and "hostile" in misc_en["Declaration_SemanticStance"],
        "semantic guard distinguishes rhetoric from actual hostility")
require("真实威胁" in misc_cn["Declaration_SemanticStance"]
        and "genuine threats" in misc_en["Declaration_SemanticStance"],
        "semantic guard still recognizes genuine threats")

# Preserve all preexisting resource text outside specifically approved writing
# changes: MODE, JSON, authority, actions, facts, progress/closure and reputation.
changed_prompt = {
    "AF_DIP_Prompt_CommonCore", "AF_DIP_Prompt_PreferenceGuard",
    "AF_DIP_Prompt_Composer_017", "AF_DIP_Prompt_Composer_168",
    "OralGeneration_System", "OralGeneration_UserHeader",
    *["AF_DIP_Contract_" + suffix for suffix in ("002", "003", "004", "005", "006", "007", "088")],
}
changed_misc = {"OralGeneration_System", "PlayerDraft_LengthRule", "PlayerDraft_LengthHeader"}
for filename, current, approved in (
    ("DiplomacyPromptTexts.json", prompt_cn, changed_prompt),
    ("DiplomacyPromptTexts.en.json", prompt_en, changed_prompt),
    ("DiplomacyMiscPromptTexts.json", misc_cn, changed_misc),
    ("DiplomacyMiscPromptTexts.en.json", misc_en, changed_misc),
):
    previous = json.loads(old(DATA + filename))
    for key, value in previous.items():
        if key == "PlayerDraft_Contract":
            expected = value.replace("只扩写结构和表达", "只调整结构和表达").replace(
                "only expanding upon structure and expression", "only adjusting structure and expression")
            require(current.get(key) == expected, filename + "/" + key + ": only expansion requirement softened")
            continue
        if key not in approved:
            require(current.get(key) == value, filename + "/" + key + ": preserved contract")
for suffix in ("006", "007", "088"):
    key = "AF_DIP_Contract_" + suffix
    require(key not in prompt_cn and key not in prompt_en, key + ": retired mandatory style rule")

rules = read(DOMAIN + "WorldDiplomacyPromptContractRules.cs")
writing = method(rules, "AppendDiplomaticDeclarationWritingContract")
declare = method(rules, "BuildDiplomaticDeclarationModeContract")
analysis = method(rules, "BuildAnalysisSystemPrompt")
canonical = method(rules, "BuildCanonicalHistorySystemPrompt")
draft_source = read(DOMAIN + "WorldDiplomacyPlayerDraftRules.cs")
draft = method(draft_source, "BuildMessages")
require('GetRequired("Declaration_DefaultStyle")' in writing
        and 'GetRequired("Declaration_DefaultStyle")' in draft,
        "generation and scribe consume one package-selected style resource")
require('GetRequired("Declaration_SemanticStance")' in declare
        and 'GetRequired("Declaration_SemanticStance")' in analysis,
        "generation and published-player analysis share the stance guard")
for name in ("BuildGenerationSystemPrompt", "BuildRelayGenerationSystemPrompt"):
    require("BuildCanonicalHistorySystemPrompt(" in method(rules, name), name + ": shared system builder")
require("AppendDiplomaticDeclarationWritingContract(" in canonical
        and "BuildDiplomaticDeclarationModeContract(" in canonical
        and "PromptComposerTexts.ApplyOutputLanguage(" in canonical,
        "actual generation system retains writing, actions and language closure")
require("PromptComposerTexts.ApplyOutputLanguage(" in analysis
        and '"role"] = "system"' in draft
        and "PromptComposerTexts.ApplyOutputLanguage(system)" in draft,
        "analysis and scribe close the final system in package language")
require(draft.index('GetRequired("Declaration_DefaultStyle")') < draft.index("system +=")
        and "prefHeader + preference" in draft and "userHeader + input.OriginalBody" in draft,
        "scribe retains custom preferences and exact player source material")
require("建议" in prompt_cn["AF_DIP_Contract_002"]
        and "Suggested" in prompt_en["AF_DIP_Contract_002"]
        and "not a minimum or maximum for publication" in prompt_en["AF_DIP_Contract_004"],
        "configured declaration length is a writing preference")
require("Simple replies may be shorter" in misc_en["PlayerDraft_LengthRule"]
        and "{MIN}" in misc_en["PlayerDraft_LengthRule"] and "{MAX}" in misc_en["PlayerDraft_LengthRule"],
        "scribe retains variable length hints without mandatory padding")
composer = read(APP + "WorldDiplomacyPromptComposer.cs")
for key in ("AF_DIP_Prompt_Composer_017", "AF_DIP_Prompt_Composer_168"):
    match = re.search(r'LocalizePrompt\("' + key + r'", ("(?:\\.|[^"\\])*")\)', composer)
    require(match is not None and json.loads(match[1]) == prompt_en[key], key + ": real consumer fallback matches EN")
for suffix in ("002", "003", "004", "005"):
    key = "AF_DIP_Contract_" + suffix
    match = re.search(r'ContractText\(localize, "' + key + r'", ("(?:\\.|[^"\\])*")\)', rules)
    require(match is not None and json.loads(match[1]) == prompt_en[key], key + ": real consumer fallback matches EN")
for key in ("AF_DIP_Prompt_CommonCore", "AF_DIP_Prompt_Composer_017", "AF_DIP_Prompt_Composer_168"):
    require("不是君主私聊" not in prompt_cn[key] and "不是君主即时聊天" not in prompt_cn[key]
            and "not live ruler chat" not in prompt_en[key] and "not private ruler chat" not in prompt_en[key],
            key + ": retired blanket conversation ban")
for filename, current, start, end in (
    ("DiplomacyPromptTexts.json", prompt_cn, "【事实】", "【模式与输出】"),
    ("DiplomacyPromptTexts.en.json", prompt_en, "[Facts]", "[Mode and output]"),
):
    before_core = json.loads(old(DATA + filename))["AF_DIP_Prompt_CommonCore"]
    core = current["AF_DIP_Prompt_CommonCore"]
    require(core[core.index(start):core.index(end)]
            == before_core[before_core.index(start):before_core.index(end)],
            filename + ": common facts/action/negotiation-closure sections unchanged")

# Validator removal must preserve numeric/internal leakage, realm identity,
# treaty disclosure and action legality.
text_source = read(DOMAIN + "WorldDiplomacyTextRules.cs")
text_before = old(DOMAIN + "WorldDiplomacyTextRules.cs")
immersion = method(text_source, "TryGetImmersionViolation")
old_immersion = method(text_before, "TryGetImmersionViolation")
old_protected = old_immersion[:old_immersion.index("\n\t\tint privateFirstPersonCount")]
require(immersion.startswith(old_protected), "prior metric and internal-round guards remain unchanged")
require("private_chat_style_in_public_declaration" not in text_source
        and "ConversationalDiplomacyPhraseRegex" not in text_source
        and "PrivateFirstPersonRegex" not in text_source and "DirectSecondPersonRegex" not in text_source,
        "personal pronouns and conversational tone no longer reject publication")
require("private_chat_style_in_public_declaration" not in rules,
        "semantic repair no longer forces institutional voice")
def without_repair_branch(body, reason):
    pattern = (r'\n[ \t]*else if \(string.Equals\(reason, "' + re.escape(reason)
               + r'", StringComparison.OrdinalIgnoreCase\)\)\s*\{[\s\S]*?\n[ \t]*\}')
    result, count = re.subn(pattern, "", body)
    require(count == 1, reason + ": exactly one explicitly reviewed repair branch")
    return result

repair_before = method(old(DOMAIN + "WorldDiplomacyPromptContractRules.cs"), "AppendGeneratedRepairCorrection")
repair_after = method(rules, "AppendGeneratedRepairCorrection")
require(without_repair_branch(repair_before, "private_chat_style_in_public_declaration")
        == without_repair_branch(repair_after, "public_document_capacity_exceeded"),
        "every preexisting non-style semantic repair branch remains unchanged")
require('GetRequired("Declaration_CapacityRepair")' in repair_after
        and "6000" in misc_cn["Declaration_CapacityRepair"] and "6000" in misc_en["Declaration_CapacityRepair"]
        and "MaxGeneratedDraftRepairAttempts = 1;" in rules,
        "capacity rejection reuses bounded repair with paired preservation guidance")
unchanged_methods(DOMAIN + "WorldDiplomacyGenerationValidationRules.cs", (
    "TryGetPublicPeaceTermsDisclosureViolation", "TryGetDiplomaticStateViolation",
    "TryGetDiplomaticThreatIntentViolation", "TryGetRealmIdentityViolation",
    "TryGetPlayerWorldStateIntentViolation", "TryGetGeneratedIntentLegalityViolation",
    "TryGetGeneratedSingleActionLegalityViolation", "TryApplyGeneratedSemanticEnvelope",
    "TryApplyGeneratedSingleActionSemanticEnvelope",
))
unchanged_methods(DOMAIN + "WorldDiplomacyPromptContractRules.cs", (
    "BuildCanonicalHistoryCompressionModeContract", "AppendRoundSubstantiveProgressRequirement",
    "BuildAnalysisModeContract", "BuildCurrentLegalDiplomaticOptions",
    "AppendOpenOfferAnswerRequirement", "BuildPolicySignalContext",
))
unchanged_methods(DOMAIN + "WorldDiplomacyPlayerDraftRules.cs", ("Parse", "GetOutputTokenBudget"))
require("Limit(text, 6000)" in method(text_source, "NormalizeBody"), "stored-body capacity remains 6000")
oral = read(APP + "WorldDiplomacyOrchestration.OralGeneration.cs")
require("body.Length > 6000" in oral and "body.Length < range.minimum" not in oral
        and "body.Length > range.maximum" not in oral,
        "oral declaration softens preferred length but retains technical capacity")
require("oral_declaration_changes_agreed_action_or_terms" in oral
        and "TryGetPublicPeaceTermsDisclosureViolation" in oral,
        "oral declaration still protects agreed action and disclosed peace terms")
completion = method(read(APP + "WorldDiplomacyGeneratedCompletionApplication.cs"), "Commit")
require(completion.index("tryGetLegalityViolation(") < completion.index("if (body.Length > 6000)")
        < completion.index("body = WorldDiplomacyTextRules.NormalizeBody(body);")
        < completion.index("WorldDiplomacyDocument document = createDocument?.Invoke("),
        "ordinary generation keeps legality checks and rejects capacity overflow before storage truncation")
require('rejectDraft?.Invoke(job, raw, authorId, targetId, "public_document_capacity_exceeded", json);' in completion,
        "capacity repair receives the uncut source and full parsed action envelope")

# Preserve actual sampling/defaults, allowing independently maintained UI text.
client_path = "src/modules/AF.Module.Diplomacy/World/WorldDiplomacyLlmClient.cs"
unchanged_methods(client_path, ("BuildRequestBody", "ResolveTemperature", "TryResolveApiConfig", "ResolveConfiguredOutputTokenLimit"))
settings_path = "src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.cs"
settings, settings_before = read(settings_path), old(settings_path)
for name in ("MainApiTemperature", "EventAndRebellionApiTemperature",
             "WorldDiplomacyDeclarationMinCharacters", "WorldDiplomacyDeclarationMaxCharacters"):
    pattern = r"public (?:float|int) " + name + r"\s*\{[^\n]*\}[^\n]*"
    require(re.search(pattern, settings)[0] == re.search(pattern, settings_before)[0], name + ": persisted default unchanged")
for name in ("WorldDiplomacyDeclarationCharactersMin", "WorldDiplomacyDeclarationCharactersMax",
             "DefaultWorldDiplomacyDeclarationMinCharacters", "DefaultWorldDiplomacyDeclarationMaxCharacters"):
    pattern = r"public const int " + name + r"\s*=\s*[^;]+;"
    if name == "DefaultWorldDiplomacyDeclarationMaxCharacters":
        require(re.search(pattern, settings)[0] == "public const int " + name + " = 300;",
                name + ": approved suggested upper bound is 300")
        require(re.search(pattern, settings_before)[0] == "public const int " + name + " = 500;",
                name + ": original default remains recorded as 500")
    else:
        require(re.search(pattern, settings)[0] == re.search(pattern, settings_before)[0], name + ": range/default unchanged")

# Analysis jobs do not use canonical affinity: both version gates must advance.
host = read("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs")
require('CanonicalHistoryCacheAffinityKey = "diplomacy-history:v30"' in rules,
        "queued canonical requests invalidate the old writing contract")
require("DiplomacyPromptContractVersion = 30;" in host,
        "load-time migration rebuilds queued analysis as well as generation")
migration = read("src/modules/AF.Module.Diplomacy/Persistence/WorldDiplomacyStorageMigration.cs")
migration_body = method(migration, "MigrateDiplomacyPromptContractIfNeeded")
require('IsJobOfKind(job, "analyze")' in migration_body
        and "tryRebuildPendingJob?.Invoke(job)" in migration_body,
        "existing migration covers analysis through its real rebuild owner")
common_getter = method(host, "GetCommonDiplomacyContract")
require("return BuildCommonDiplomacySystemPrefix();" in common_getter
        and "CommonContractSnapshot" not in common_getter
        and "LocalizeWorldDiplomacyContractForPrompt" in method(host, "BuildCommonDiplomacySystemPrefix"),
        "rebuild uses current localized common contract, not old round snapshots")
require("TryExtractCommonContractFromJob" in method(host, "ResolveCommonContractForCacheDiagnostics")
        and host.count("TryExtractCommonContractFromJob(") == 1,
        "old-job common-contract extraction is diagnostics only")

print(f"PASS diplomacy writing style: {CHECKS} static assertions; paired resources, shared system consumers, "
      "protected action/MODE/closure guards, soft length with 6000 capacity, unchanged sampling and reviewed defaults, "
      "queued-prompt migration. C# replay/provider/live-game acceptance NOT RUN by this script.")
