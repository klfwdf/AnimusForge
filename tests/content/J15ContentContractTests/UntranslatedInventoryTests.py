"""Exercise inventory identity rules without modifying any product assets."""
import importlib.util
import json
import hashlib
import subprocess
import re
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
spec = importlib.util.spec_from_file_location(
    "inventory_untranslated", ROOT / "tools/localization/inventory_untranslated.py")
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)

with tempfile.TemporaryDirectory() as temporary:
    root = Path(temporary)
    books = root / "content/modules/AF.Module.Onboarding/PlayerExports"
    english = books / "Calradia Chronicles"
    chinese = books / "\u5361\u62c9\u8fea\u4e9a\u7f16\u5e74\u53f2"
    fixtures = {
        "event_data/WorldOpeningSummary.json": {},
        "personality_background/lord_1_14__Rhagaea.json": {"Background": "Empress"},
        "knowledge/rules/rule_empire__Empire.json": {"Id": "rule_empire", "Text": "Empire"},
        "knowledge/rules/rule_bad__Bad.json": {"Id": "rule_bad", "Text": "\u4e2d\u6587"},
    }
    for relative, payload in fixtures.items():
        path = english / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(payload), encoding="utf-8")
    index = audit.worldbook_english_index(root)
    assert audit.worldbook_counterpart(chinese / "event_data/WorldOpeningSummary.json", root, index)
    assert audit.worldbook_counterpart(chinese / "personality_background/lord_1_14__different.json", root, index)
    for rule_id, matches in (("rule_empire", True), ("rule_bad", False), ("rule_missing", False)):
        path = chinese / "knowledge/rules" / (rule_id + ".json")
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps({"Id": rule_id}), encoding="utf-8")
        assert bool(audit.worldbook_counterpart(path, root, index)) == matches
    duplicate = english / "personality_background/lord_1_14__duplicate.json"
    duplicate.write_text("{}", encoding="utf-8")
    assert not audit.worldbook_counterpart(chinese / "personality_background/lord_1_14__different.json",
                                          root, audit.worldbook_english_index(root))

source = '// "\u4e2d\u6587 comment"\nvar a = "\\u4e2d\\u6587"; var b = @"\u4e2d\u6587";'
tokens = audit.tokenize(source)[1]
assert len(tokens) == 2
assert all(audit.decode(match[0]) == "\u4e2d\u6587" for match, _ in tokens)

# Fixed pre-extraction input keeps every original scanner negative control live.
# Current consumers are checked separately below, so absence of inline Chinese
# cannot turn a negative-control fixture into an automatic zero-result success.
FIXTURE_REVISION = 'a911d4d0de8964a393b6bdc6eaa83705b5218b02'
def historical(path, digest):
    source = subprocess.check_output(['git', 'show', FIXTURE_REVISION + ':' + path], cwd=ROOT).decode('utf-8-sig')
    assert hashlib.sha256(source.encode()).hexdigest() == digest, path + ': immutable fixture identity'
    return source
mcm_source = historical(audit.MCM_SOURCE, 'b2bc31686b9454c8f389acc2fa6d2db36ed9924232205d0fd093e13ee026599c')
english_keys = {
    node.get("id")
    for english, _ in audit.registered_pairs(ROOT)
    for node in ET.parse(english).getroot().iter("string")
    if not audit.HAN.search(node.get("text", ""))
}


def projected(source, keys):
    code, tokens = audit.tokenize(source)
    return audit.mcm_projected_offsets(source, code, tokens, keys)


covered = projected(mcm_source, english_keys)
assert len(covered) == 19, len(covered)
assert not projected(mcm_source, set())
assert len(projected(mcm_source, english_keys - {"AF_MCM_Option_BackgroundWhite"})) == 18
assert len(projected(mcm_source, english_keys - {"AF_MCM_Option_ApiMain"})) == 17
assert len(projected(mcm_source.replace("{=AF_MCM_Option_BackgroundWhite}", "{=unregistered}"), english_keys)) == 18
comment_only_key = mcm_source.replace('"{=AF_MCM_Option_BackgroundWhite}Transparent white"',
                                      '"{=unregistered}Transparent white" /* {=AF_MCM_Option_BackgroundWhite} */')
assert len(projected(comment_only_key, english_keys)) == 18
missing_model_call = mcm_source.replace("return BuildModelDisplayDropdown(normalizedOptions, num);", "return MissingDisplay(normalizedOptions, num);")
assert len(projected(missing_model_call, english_keys)) == 18
missing_api_call = mcm_source.replace("BuildPolicyApiSourceDisplayOptions(options.Count)", "MissingApiDisplay(options.Count)")
assert len(projected(missing_api_call, english_keys)) == 12
# Identical copy in another member is not evidence that member has a projection.
unrelated = mcm_source + '\nprivate const string OtherValue = "\u9ed1\u8272\u900f\u660e";\n'
assert len(projected(unrelated, english_keys)) == 19

current_mcm = (ROOT / audit.MCM_SOURCE).read_text(encoding='utf-8-sig')
mcm_raw = json.loads((ROOT / 'content/foundation/AF.Foundation.Localization/ModuleData/McmPromptSourceCompatibility.json').read_text(encoding='utf-8-sig'))
restored_mcm = re.sub(r'McmPromptDefaults.Raw\("([^"\n]+)"\)', lambda match: json.dumps(mcm_raw[match[1]], ensure_ascii=False), current_mcm)
symbols = ['ManualDropdownModelName', 'ShoutInputUiBackgroundBlack', 'ShoutInputUiBackgroundWhite', 'ShoutInputUiBackgroundPink', 'LogCleanupOff', 'LogCleanupOnStartup', 'LogCleanupEvery30Minutes', 'LogCleanupEveryHour', 'LogCleanupEvery6Hours', 'LogCleanupEveryDay', 'LogCleanupEvery3Days', 'LogCleanupEveryWeek']
for symbol in symbols:
    declaration = re.search(r'\b(?:public|private) static readonly string ' + symbol + r' = McmPromptDefaults.Raw\("([^"\n]+)"\);', current_mcm)
    assert declaration, symbol + ': current raw consumer'
    old = re.search(r'\b(?:public|private) const string ' + symbol + r' = ("(?:\\.|[^"\\])*");', mcm_source)
    assert old and mcm_raw[declaration[1]] == audit.decode(old[1]), symbol + ': original value conserved'
    restored_mcm = re.sub(r'\bstatic readonly string ' + symbol + r'\b', 'const string ' + symbol, restored_mcm)
assert len(projected(restored_mcm, english_keys)) == 19, 'all current getter values retain the witnessed projection/consumer boundary'
assert not projected(current_mcm, english_keys), 'externalized values are no longer inline literal candidates'

retry_source = historical(audit.RETRY_SOURCE, '8fec8fb48f7827f70f28dd91da0fd97b9405b220aa48405a645fa5bbb8b91fa9')
retry_code, retry_tokens = audit.tokenize(retry_source)
roles = [(audit.decode(match[0]), audit.retry_literal_role(retry_source, retry_code, match, frames))
         for match, frames in retry_tokens]
assert sum(role == "internal" for _, role in roles) == 1
assert sum(role == "raw_diagnostic" for _, role in roles) == 1
assert ("LLM\u8bf7\u6c42", "internal") in roles
assert ("\u672a\u77e5\u9519\u8bef", "raw_diagnostic") in roles
other_retry = retry_source + '\nprivate void Other() { NormalizeFullText("", "\u672a\u77e5\u9519\u8bef"); var stage = "LLM\u8bf7\u6c42"; }\n'
other_code, other_tokens = audit.tokenize(other_retry)
assert all(audit.retry_literal_role(other_retry, other_code, match, frames) is None
           for match, frames in other_tokens if match.start() >= len(retry_source))
current_retry = (ROOT / audit.RETRY_SOURCE).read_text(encoding='utf-8-sig')
llm_raw = json.loads((ROOT / 'content/modules/AF.Module.Llm/ModuleData/LlmSourceTexts.json').read_text(encoding='utf-8-sig'))
assert llm_raw['RetryStageRequest'] == 'LLM请求' and llm_raw['RetryUnknownError'] == '未知错误'
code, tokens = audit.tokenize(current_retry)
for method, expression in [('RetryStageDisplay', 'stage == LlmSourceTexts.Get("RetryStageRequest")'), ('BuildFailureDetail', 'NormalizeFullText(reason, LlmSourceTexts.Get("RetryUnknownError"))')]:
    region = audit.method_region(current_retry, code, method)
    assert region and expression in current_retry[slice(*region)], method + ': current exact raw getter consumer'
assert all(audit.retry_literal_role(current_retry, code, token, frames) is None for token, frames in tokens)
print("PASS inventory: identity, literal decoding, 19 witnessed MCM projections, missing-key/call negative controls, retry raw/internal boundaries")
