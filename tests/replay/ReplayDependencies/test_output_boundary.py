"""Synthetic offline checks for the explicit replay artifact output boundary."""
import os
from pathlib import Path
import subprocess
import sys
import unittest

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import minimal_test_environment, new_run_root, resolve_dotnet


class ReplayOutputBoundaryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.root = new_run_root(ROOT, "replay-output-boundary", None)
        cls.powershell = Path(os.environ.get("SystemRoot", "C:/Windows")) / "System32/WindowsPowerShell/v1.0/powershell.exe"
        if not cls.powershell.is_file():
            raise unittest.SkipTest("BLOCKED_ENV: Windows PowerShell 5.1 is unavailable")
        cls.inputs = cls.root / "inputs"
        for name in ("game/bin/Win64_Shipping_Client", "refs", "private", "project"):
            (cls.inputs / name).mkdir(parents=True)
        cls.modules = {}
        for name in ("Bannerlord.Harmony", "Bannerlord.MBOptionScreen", "Bannerlord.UIExtenderEx"):
            module = cls.inputs / name
            (module / "bin/Win64_Shipping_Client").mkdir(parents=True)
            (module / "SubModule.xml").write_text(f'<Module><Id value="{name}"/></Module>', encoding="utf-8")
            cls.modules[name] = module
        # ValidateOnly checks the output boundary, not CLR metadata or production loading.
        cls.candidate = cls.inputs / "fixture-not-production.dll"
        cls.candidate.write_bytes(b"output boundary fixture")
        cls.env = minimal_test_environment(resolve_dotnet(ROOT), cls.root / "environment")

    def validate(self, output_root, output):
        values = {"GameRoot": self.inputs / "game", "ReferencePath": self.inputs / "refs",
                  "PrivateRuntimePath": self.inputs / "private", "ProjectDirectory": self.inputs / "project",
                  "HarmonyModulePath": self.modules["Bannerlord.Harmony"],
                  "McmModulePath": self.modules["Bannerlord.MBOptionScreen"],
                  "UiExtenderModulePath": self.modules["Bannerlord.UIExtenderEx"],
                  "ImplementationPath": self.candidate, "OutputDirectory": output, "OutputRoot": output_root}
        cmd = [str(self.powershell), "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File",
               str(Path(__file__).with_name("Copy-ReplayDependencies.ps1"))]
        for name, value in values.items():
            cmd += ["-" + name, str(value)]
        result = subprocess.run(cmd + ["-ValidateOnly"], cwd=ROOT, env=self.env, capture_output=True,
                                text=True, encoding="utf-8", errors="replace", timeout=30)
        return result.returncode, result.stdout + result.stderr

    def test_sdk_artifact_layout_is_accepted_without_copying(self):
        output_root = self.root / "valid"
        output = output_root / "bin/TestProject/release_net8.0"
        code, log = self.validate(output_root, output)
        self.assertEqual(code, 0, log)
        self.assertIn("REPLAY_OUTPUT_BOUNDARY_PASS", log)
        self.assertFalse(output.exists())

    def test_old_output_is_not_reused(self):
        output_root = self.root / "old"
        output = output_root / "bin/TestProject/release_net8.0"
        output.mkdir(parents=True)
        code, log = self.validate(output_root, output)
        self.assertEqual(code, 1, log)
        self.assertIn("existing output is not reused", log)

    def test_root_outside_artifacts_is_rejected_without_write(self):
        output_root = ROOT / "tests/rejected-output"
        code, log = self.validate(output_root, output_root / "bin/project")
        self.assertEqual(code, 1, log)
        self.assertIn("below repository artifacts", log)
        self.assertFalse(output_root.exists())

    def test_output_outside_selected_bin_is_rejected(self):
        code, log = self.validate(self.root / "selected", self.root / "not-selected/bin/project")
        self.assertEqual(code, 1, log)
        self.assertIn("REPLAY_OUTPUT_BOUNDARY", log)

    def test_output_in_an_input_directory_is_rejected(self):
        code, log = self.validate(self.inputs / "private", self.inputs / "private/bin/project")
        self.assertEqual(code, 1, log)
        self.assertIn("overlaps an input directory", log)


if __name__ == "__main__":
    unittest.main()
