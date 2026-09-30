"""No real runner is launched by these orchestration safety regressions."""
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import stat
from types import SimpleNamespace
import unittest
from unittest.mock import patch
import uuid

SPEC = importlib.util.spec_from_file_location("af_run_all", Path(__file__).with_name("run_all.py"))
runner = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(runner)


class RunAllSafetyTests(unittest.TestCase):
    def setUp(self):
        self.root = runner.ROOT / "artifacts/tests/run-all-safety" / uuid.uuid4().hex
        self.repo = self.root / "repo"
        self.repo.mkdir(parents=True)
        self.temp = self.root / "approved-temp"
        self.manifest = self.repo / "runners.json"

    def invoke(self, entries, extra=(), returncode=0):
        self.manifest.write_text(json.dumps({"entries": entries}), encoding="utf-8")
        with patch.object(runner, "ROOT", self.repo), patch.object(runner, "MANIFEST", self.manifest), \
             patch.object(runner, "discover", return_value=list(entries)), \
             patch.object(runner, "command", return_value=["fake-dotnet"]), \
             patch.object(runner, "blocked", return_value=None), \
             patch.dict(os.environ, {"AF_TEST_TEMP_ROOT": str(self.temp)}, clear=True), \
             patch.object(runner.subprocess, "run", return_value=subprocess.CompletedProcess([], returncode, "fixture", "")) as launch:
            code = runner.main(["--jobs", "1", *extra])
        return code, launch

    def test_manual_entries_are_reported_without_launch(self):
        code, launch = self.invoke({"business.py": {"expect": "NEEDS_INPUT", "execution": "manual"}})
        self.assertEqual(code, 0)
        launch.assert_not_called()
        results = json.loads(next(self.repo.glob("artifacts/tests/run_all/*/results.json")).read_text())
        self.assertEqual((results[0]["status"], results[0]["exit"]), ("NEEDS_INPUT", None))

    def test_unknown_execution_mode_is_rejected_before_writes(self):
        code, launch = self.invoke({"bad.py": {"execution": "typo"}})
        self.assertEqual(code, 2)
        launch.assert_not_called()
        self.assertFalse(self.temp.exists())

    def test_manual_cannot_hide_required_pass(self):
        code, launch = self.invoke({"bad.py": {"execution": "manual", "expect": "PASS"}})
        self.assertEqual(code, 2)
        launch.assert_not_called()

    def test_editor_gets_unique_isolated_root_and_environment(self):
        code, launch = self.invoke({"editor.csproj": {"expect": "NEEDS_INPUT", "execution": "isolated-player-exports"}})
        self.assertEqual(code, 0)
        call = launch.call_args
        self.assertEqual(call.args[0][-3:-1], ["--", "--isolated-full"])
        root = Path(call.args[0][-1])
        self.assertTrue(root.is_relative_to(self.temp))
        self.assertTrue((root / "temp").is_dir())
        self.assertEqual(call.kwargs["env"]["ANIMUSFORGE_DATA_ROOT"], str(root / "data"))
        self.assertEqual(call.kwargs["env"]["TMP"], str(root / "temp"))
        self.assertEqual(call.kwargs["env"]["TEMP"], str(root / "temp"))
        self.assertFalse((root / "data").exists())

    def test_each_entry_has_separate_temp(self):
        _, launch = self.invoke({"one.py": {}, "two.py": {}})
        paths = [c.kwargs["env"]["TEMP"] for c in launch.call_args_list]
        self.assertEqual(len(set(paths)), 2)

    def test_failure_classification_is_unchanged(self):
        code, _ = self.invoke({"expected.py": {"expect": "PREEXISTING_FAIL"}, "required.py": {}}, returncode=1)
        self.assertEqual(code, 1)
        results = json.loads(next(self.repo.glob("artifacts/tests/run_all/*/results.json")).read_text())
        self.assertEqual([r["status"] for r in results], ["PREEXISTING_FAIL", "FAIL"])

    def test_list_does_not_create_outputs(self):
        code, launch = self.invoke({"manual.py": {"expect": "NEEDS_INPUT", "execution": "manual"}}, ["--list"])
        self.assertEqual(code, 0)
        launch.assert_not_called()
        self.assertFalse(self.temp.exists())
        self.assertFalse((self.repo / "artifacts").exists())

    def test_output_outside_workspace_is_rejected_without_launch(self):
        code, launch = self.invoke({"one.py": {}}, ["--out", str(self.root / "outside")])
        self.assertEqual(code, 2)
        launch.assert_not_called()
        self.assertFalse((self.root / "outside").exists())

    def test_existing_output_is_preserved(self):
        output = self.repo / "keep"
        output.mkdir()
        marker = output / "marker"
        marker.write_text("untouched")
        code, launch = self.invoke({"one.py": {}}, ["--out", str(output)])
        self.assertEqual(code, 2)
        launch.assert_not_called()
        self.assertEqual(marker.read_text(), "untouched")

    def test_temp_inside_repository_is_rejected(self):
        self.temp = self.repo / "unsafe-temp"
        code, launch = self.invoke({"one.py": {}})
        self.assertEqual(code, 2)
        launch.assert_not_called()
        self.assertFalse(self.temp.exists())

    def test_missing_temp_authorization_is_rejected(self):
        # An empty value behaves like an absent authorization, without inheriting user environment.
        self.temp = ""
        code, launch = self.invoke({"one.py": {}})
        self.assertEqual(code, 2)
        launch.assert_not_called()

    def test_redirected_path_is_rejected_before_creation(self):
        info = SimpleNamespace(st_mode=stat.S_IFDIR, st_file_attributes=stat.FILE_ATTRIBUTE_REPARSE_POINT)
        with patch.object(Path, "lstat", return_value=info):
            with self.assertRaisesRegex(ValueError, "reparse"):
                runner.checked_path(self.repo / "redirected" / "child")

    def test_actual_manifest_keeps_business_tools_manual_and_editor_isolated(self):
        entries = json.loads(runner.MANIFEST.read_text(encoding="utf-8"))["entries"]
        for name in ("af2_migrate.py", "generate_bannerlord_history_culture_study.py",
                     "generate_bannerlord_research_grade_study.py"):
            self.assertEqual(entries["tools/" + name]["execution"], "manual")
            self.assertEqual(entries["tools/" + name]["expect"], "NEEDS_INPUT")
        editor = entries["tools/PlayerExportsEditor/tests/PlayerExportsEditor.SmokeTests/PlayerExportsEditor.SmokeTests.csproj"]
        self.assertEqual(editor["execution"], "isolated-player-exports")
        self.assertEqual(editor["expect"], "NEEDS_INPUT")


if __name__ == "__main__":
    unittest.main()
