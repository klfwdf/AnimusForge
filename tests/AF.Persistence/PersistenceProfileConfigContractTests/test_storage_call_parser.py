import unittest

from validate_persistence_profile_config import extract_call_arguments, split_call_arguments


class StorageCallParserTests(unittest.TestCase):
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
