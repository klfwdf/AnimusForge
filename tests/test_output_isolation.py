"""Offline regression for per-run tool selection and credential-free environments."""
import os
from pathlib import Path
import sys
import unittest
from unittest.mock import patch
from uuid import uuid4

from output_isolation import minimal_test_environment, new_run_root, resolve_dotnet


ROOT = Path(__file__).resolve().parents[1]


class EnvironmentTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.output = new_run_root(ROOT, "output-isolation", None)
        cls.host = cls.output / "dotnet.exe"
        cls.host.write_bytes(b"fixture only, never execute")

    def test_explicit_selection(self):
        with patch.dict(os.environ, {}, clear=True):
            self.assertEqual(resolve_dotnet(ROOT, self.host), self.host.resolve())

    def test_invalid_explicit_selection_does_not_fallback(self):
        with patch.dict(os.environ, {"AF_DOTNET8": str(self.host)}, clear=True):
            with self.assertRaisesRegex(SystemExit, "BLOCKED_ENV"):
                resolve_dotnet(ROOT, self.output / "missing.exe")

    def test_major_specific_override_has_priority(self):
        with patch.dict(os.environ, {"AF_DOTNET8": str(self.host), "DOTNET_EXE": "missing"}, clear=True):
            self.assertEqual(resolve_dotnet(ROOT), self.host.resolve())

    def test_legacy_override(self):
        with patch.dict(os.environ, {"DOTNET_EXE": str(self.host)}, clear=True):
            self.assertEqual(resolve_dotnet(ROOT), self.host.resolve())

    def test_invalid_environment_override_does_not_fallback(self):
        with patch.dict(os.environ, {"AF_DOTNET8": "missing"}, clear=True):
            with self.assertRaisesRegex(SystemExit, "BLOCKED_ENV"):
                resolve_dotnet(ROOT)

    def test_environment_does_not_copy_arbitrary_variables(self):
        with patch.dict(os.environ, {"UNRELATED_PRIVATE_VALUE": "fixture", "SystemRoot": "fixture-windows",
                                     "TEMP": "unapproved-user-temp"}, clear=True):
            env = minimal_test_environment(self.host, self.output / "environment")
        self.assertNotIn("UNRELATED_PRIVATE_VALUE", env)
        self.assertEqual(env["SystemRoot"], "fixture-windows")
        self.assertEqual(env["PATH"], str(self.host.parent))
        self.assertEqual(env["TEMP"], str(self.output / "environment/temp"))
        self.assertEqual(env["USERPROFILE"], str(self.output / "environment/home"))
        self.assertEqual(env["NUGET_PACKAGES"], str(self.output / "environment/nuget-packages"))

    def test_explicit_synthetic_temp(self):
        with patch.dict(os.environ, {}, clear=True):
            temp = self.output / "approved-synthetic"
            env = minimal_test_environment(self.host, self.output / "environment-explicit", temp)
        self.assertEqual(env["TEMP"], str(temp))
        self.assertEqual(env["TMP"], str(temp))

    def test_new_output_is_inside_repository(self):
        output = new_run_root(ROOT, "output-isolation", None)
        self.assertTrue(output.is_relative_to(ROOT))

    def test_existing_output_is_rejected(self):
        with self.assertRaisesRegex(SystemExit, "new directory"):
            new_run_root(ROOT, "ignored", self.output)

    def test_outside_output_is_rejected_before_write(self):
        with self.assertRaisesRegex(SystemExit, "inside the repository"):
            new_run_root(ROOT, "ignored", ROOT.parent / ("not-created-" + uuid4().hex))

    def test_parent_traversal_is_rejected(self):
        with self.assertRaisesRegex(SystemExit, "parent directories"):
            new_run_root(ROOT, "ignored", ROOT / "artifacts/../rejected")


if __name__ == "__main__":
    unittest.main()
