"""Source-overlay safety and complete isolated ZIP contract (no publishing/deployment)."""
from __future__ import annotations
import hashlib
import importlib.util
import json
import sys
import unittest
import zipfile
from pathlib import Path
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root
spec = importlib.util.spec_from_file_location("policy_overlay", ROOT / "tools/package_policy_system_source_overlay.py")
overlay = importlib.util.module_from_spec(spec)
spec.loader.exec_module(overlay)

class OverlayContractTests(unittest.TestCase):
    def test_migration_cache_exclusion_is_narrow_and_case_insensitive(self):
        for path in ("NuGet/Migrations/1", "tests/Foo/NUGET/migrations/1"):
            self.assertTrue(overlay.is_nuget_migration_cache(Path(path)))
        for path in ("NuGet.Config", "tests/Foo/NuGet.Config", "NuGet/packages/foo.cs", "Migrations/1"):
            self.assertFalse(overlay.is_nuget_migration_cache(Path(path)))
        files, _ = overlay.build_file_set()
        self.assertFalse(any(overlay.is_nuget_migration_cache(Path(name)) for name in files))

    def test_complete_zip_preserves_selection_categories_aliases_and_payloads(self):
        output = new_run_root(ROOT, "policy-overlay-contract", None)
        result = overlay.create_package(output)
        files, categories = overlay.build_file_set()
        self.assertEqual(result["fileCount"], len(files))
        self.assertEqual(sum(category == "runtime_assets" for category in categories.values()), 14)
        with zipfile.ZipFile(result["zipPath"]) as archive:
            prefix = Path(result["zipPath"]).stem + "/"
            metadata = {"README_FIRST.md", "EXCLUDED.md", "MANIFEST.json", "FILES.sha256", "SOURCE_STATUS.txt"}
            self.assertEqual(set(archive.namelist()), {prefix + name for name in metadata} | {prefix + "OVERLAY/" + name for name in files})
            manifest = json.loads(archive.read(prefix + "MANIFEST.json"))
            self.assertEqual(manifest["fileCount"], len(files))
            self.assertEqual(manifest["secretPatternHitCount"], 0)
            self.assertTrue(manifest["excludedOnnxAssets"])
            hashes = {line.split("  ", 1)[1]: line.split("  ", 1)[0] for line in archive.read(prefix + "FILES.sha256").decode().splitlines()}
            for item in manifest["files"]:
                name = item["path"]
                data = archive.read(prefix + "OVERLAY/" + name)
                self.assertEqual(data, files[name].read_bytes(), name)
                self.assertEqual(item["category"], categories[name], name)
                self.assertEqual(item["sha256"], hashlib.sha256(data).hexdigest().upper(), name)
                self.assertEqual(hashes["OVERLAY/" + name], item["sha256"], name)
                self.assertEqual(item["size"], len(data), name)

    def test_binary_generated_and_secret_inputs_fail_closed(self):
        output = new_run_root(ROOT, "policy-overlay-negative", None)
        cases = {"binary.dll": b"binary", "bin/file.cs": b"source", "NuGet/Migrations/1": b"cache", "key.txt": ("sk-" + "A" * 24).encode()}
        with patch.object(overlay, "ROOT", output):
            for name, data in cases.items():
                path = output / name
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(data)
                with self.subTest(name=name), self.assertRaises(RuntimeError):
                    overlay.validate_files({name: path})
            legal = output / "NuGet.Config"
            legal.write_text("<configuration />", encoding="utf-8")
            overlay.validate_files({"NuGet.Config": legal})

if __name__ == "__main__":
    unittest.main(verbosity=2)
