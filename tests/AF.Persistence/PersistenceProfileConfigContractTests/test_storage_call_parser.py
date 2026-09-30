import unittest
import copy
from unittest.mock import patch
import validate_persistence_profile_config as validator

from validate_persistence_profile_config import extract_call_arguments, split_call_arguments


class StorageCallParserTests(unittest.TestCase):
    def test_civil_war_v2_catalog_and_v1_negative_control(self):
        catalog=validator.load_json(validator.FIXTURE_DIR/'persistence-catalog.json')
        bindings=validator.load_json(validator.FIXTURE_DIR/'syncdata-binding-catalog.json')
        actual=validator.discover_typed_bindings()
        rows=[row for row in actual if row['key'].startswith('_af_kingdom_civil_war_')]
        self.assertEqual(len(rows),2)
        self.assertTrue(all(row['key']=='_af_kingdom_civil_war_v2' and row['type']=='string' and row['ref']=='_civilWarJsonStorage' for row in rows))
        with patch.object(validator,'discover_typed_bindings',return_value=actual):
            validator.validate_typed_bindings(bindings,catalog)
        for bad_key in ['_af_kingdom_civil_war_v1','_af_kingdom_civil_war_v3']:
            changed=copy.deepcopy(actual)
            for row in changed:
                if row['key']=='_af_kingdom_civil_war_v2':row['key']=bad_key
            with patch.object(validator,'discover_typed_bindings',return_value=changed):
                with self.assertRaisesRegex(AssertionError,'binding catalog drifted'):
                    validator.validate_typed_bindings(bindings,catalog)

    def test_nested_source_expression_preserves_storage_key(self):
        source = 'FlattenStringDictionary(_inbox.ExportRecords(), SaveKeyRecords, "WorldEventInbox")'
        calls = list(extract_call_arguments(source, "FlattenStringDictionary"))
        self.assertEqual(len(calls), 1)
        self.assertEqual(split_call_arguments(calls[0])[1], "SaveKeyRecords")

    def test_quoted_parenthesis_does_not_truncate_call(self):
        source = 'SaveChunkedString(value, "key(x)", "label")'
        calls = list(extract_call_arguments(source, "SaveChunkedString"))
        self.assertEqual(split_call_arguments(calls[0])[1], '"key(x)"')


if __name__ == "__main__":
    unittest.main()
