"""Exercise inventory identity rules without modifying any product assets."""
import importlib.util
import json
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

mcm_source = (ROOT / audit.MCM_SOURCE).read_text(encoding="utf-8-sig")
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

retry_source = (ROOT / audit.RETRY_SOURCE).read_text(encoding="utf-8-sig")
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
print("PASS inventory: identity, literal decoding, 19 witnessed MCM projections, missing-key/call negative controls, retry raw/internal boundaries")
