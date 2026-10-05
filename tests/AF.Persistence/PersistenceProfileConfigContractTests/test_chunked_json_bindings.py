import copy
import unittest
from pathlib import Path
from unittest.mock import patch
import validate_persistence_profile_config as validator


class CurrentChunkBindingsTests(unittest.TestCase):
    def setUp(self):
        self.catalog = validator.load_json(validator.FIXTURE_DIR / "persistence-catalog.json")

    def test_current_registered_bindings(self):
        validator.validate_json_bindings(self.catalog)

    def test_removed_read_and_write_are_rejected_for_every_binding(self):
        read = Path.read_text
        for row in self.catalog["chunkedJsonBindings"]:
            path = validator.ROOT / row["source"]
            original = read(path, encoding="utf-8-sig")
            for operation in ["SaveChunkedString", "LoadChunkedString"]:
                with self.subTest(key=row["key"], operation=operation):
                    def changed(p, *args, **kwargs):
                        return original.replace(operation, "UnchunkedRegression") if p == path else read(p, *args, **kwargs)
                    with patch.object(Path, "read_text", changed):
                        with self.assertRaises(AssertionError):
                            validator.validate_json_bindings(self.catalog)

    def test_raw_binding_reintroduced_is_rejected(self):
        read = Path.read_text
        row = self.catalog["chunkedJsonBindings"][-1]
        path = validator.ROOT / row["source"]
        original = read(path, encoding="utf-8-sig")
        text = original + '\nstore.SyncData("' + row["key"] + '", ref ' + row["variable"] + ');\n'
        with patch.object(Path, "read_text", lambda p, *a, **kw: text if p == path else read(p, *a, **kw)):
            with self.assertRaisesRegex(AssertionError, "raw SyncData"):
                validator.validate_json_bindings(self.catalog)

    def test_registered_risk_remains_visible(self):
        self.assertEqual(validator.validate_direct_json_hazards(self.catalog), 1)

    def test_new_raw_json_binding_is_rejected(self):
        records = [(row["source"], 'store.SyncData("old", ref ' + row["variable"] + ');')
                   for row in self.catalog["knownDirectJsonBindings"]]
        records.append(("src/Test/NewOwner.cs", 'store.SyncData("new", ref stateJson);'))
        with patch.object(validator, "current_json_source_texts", return_value=iter(records)):
            with self.assertRaisesRegex(AssertionError, "new unchunked JSON"):
                validator.validate_direct_json_hazards(self.catalog)


if __name__ == "__main__":
    unittest.main()
