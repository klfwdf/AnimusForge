"""Packaging boundary regressions; temporary text fixtures, no runtime/model copying."""
from pathlib import Path
import tempfile
import unittest
from package_addon import runtime_entries


class PackagingChecks(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.module = Path(self.temp.name)
        self.runtime = self.module / 'AF-GPTSOVITS-U'
        self.runtime.mkdir()
        (self.runtime / 'server.py').write_text('# fixture', encoding='utf-8')

    def tearDown(self):
        self.temp.cleanup()

    def test_old_documentation_not_rearchived(self):
        docs = self.runtime / 'Documentation'
        docs.mkdir()
        for name in ('archive-files.json', 'User-Guide.html', 'stale-guide.txt'):
            (docs / name).write_text('old fixture', encoding='utf-8')
        self.assertEqual(set(runtime_entries(self.module, self.runtime)),
                         {'AnimusForge/AF-GPTSOVITS-U/server.py'})

    def test_python_cache_not_rearchived(self):
        cache = self.runtime / '__pycache__'
        cache.mkdir()
        (cache / 'test.pyc').write_bytes(b'cache fixture')
        self.assertEqual(len(runtime_entries(self.module, self.runtime)), 1)

    def test_private_environment_rejected(self):
        (self.runtime / '.env').write_text('fixture', encoding='utf-8')
        with self.assertRaisesRegex(ValueError, 'unexpected_private_file'):
            runtime_entries(self.module, self.runtime)


if __name__ == '__main__':
    unittest.main(verbosity=2)
