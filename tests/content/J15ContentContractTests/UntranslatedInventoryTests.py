"""Exercise inventory identity rules without modifying any product assets."""
import importlib.util
import json
import tempfile
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
print("PASS inventory: verified per-file identity, untranslated/ambiguous rejection, literal decoding")
