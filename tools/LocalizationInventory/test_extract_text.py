import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest

from extract_text import classify, json_records, resource_records, worldbook_path

ROOT = Path(__file__).resolve().parents[2]

class InventoryTests(unittest.TestCase):
    def test_json_decodes_unicode_and_preserves_distinct_occurrences(self):
        text = '{"a": ["\\u4e2d\\u6587", "中文"], "b/c": {"~": "{NAME}"}}'
        records = json_records(text)
        values = [r for r in records if r['kind'] == 'json.value']
        self.assertEqual([r['text'] for r in values], ['中文', '中文', '{NAME}'])
        self.assertEqual([r['context'] for r in values], ['/a/0', '/a/1', '/b~1c/~0'])
        self.assertNotEqual(values[0]['offset'], values[1]['offset'])

    def test_json_empty_containers_and_numbers(self):
        self.assertEqual([r['text'] for r in json_records('[{}, [], null, true, 42, "完成"]')], ['完成'])

    def test_json_net_style_literal_newlines_are_preserved(self):
        records = json_records('{"prompt": "第一行\n第二行"}')
        self.assertEqual(records[-1]['text'], '第一行\n第二行')

    def test_worldbook_exclusion_is_narrow(self):
        self.assertTrue(worldbook_path('content/Preset/knowledge/rules/rule.json'))
        self.assertTrue(worldbook_path('content/Preset/knowledge/single_rules/rule.json'))
        self.assertTrue(worldbook_path('content/Preset/knowledge/KnowledgeRules.json'))
        self.assertFalse(worldbook_path('content/AF.Module.Weekly/WorldOpeningSummary.json'))
        self.assertFalse(worldbook_path('src/AF.Module.Knowledge/KnowledgeLibraryBehavior.cs'))

    def test_xml_entities_and_contract_classification(self):
        records = resource_records(Path('strings.xml'), '<string id="fixed" text="&#20013;文 &amp; {PLAYER}"/>')
        self.assertEqual(records[1]['text'], '中文 & {PLAYER}')
        self.assertEqual(classify(dict(records[0], path='strings.xml')), 'resource_contract_review')

    def test_roslyn_literals_comments_interpolation_and_two_api_branches(self):
        source = r'''class Sample {
const string Escaped = "\u4e2d\u6587";
const string Quoted = @"说""你好""";
const char Han = '汉';
string Format(int count) => $"{count}个单位";
// "注释不是字符串"
#if BANNERLORD_1_4_OR_GREATER
const string Version = "新版";
#else
const string Version = "旧版";
#endif
}'''
        temporary_root = ROOT / 'tools/LocalizationInventory/obj'
        temporary_root.mkdir(parents=True, exist_ok=True)
        with tempfile.TemporaryDirectory(dir=temporary_root) as scratch:
            folder = Path(scratch)
            (folder / 'Sample.cs').write_text(source, encoding='utf-8')
            (folder / 'paths.json').write_text('["Sample.cs"]', encoding='utf-8')
            exe = ROOT / 'tools/LocalizationInventory/bin/Release/net8.0/LocalizationInventory.dll'
            result = subprocess.check_output([os.environ.get('AF_INVENTORY_DOTNET', 'dotnet'), str(exe), str(folder), str(folder / 'paths.json')])
            records = [json.loads(line) for line in result.decode('utf-8-sig').splitlines()]
        strings = [r['text'] for r in records if r['kind'] == 'csharp.string']
        self.assertIn('中文', strings)
        self.assertIn('说"你好"', strings)
        self.assertIn('新版', strings)
        self.assertIn('旧版', strings)
        self.assertNotIn('注释不是字符串', strings)
        self.assertTrue(any(r['text'] == '汉' and r['kind'] == 'csharp.char' for r in records))
        self.assertTrue(any(r['text'] == '{count}个单位' and r['kind'] == 'csharp.interpolated' for r in records))

if __name__ == '__main__':
    unittest.main()
