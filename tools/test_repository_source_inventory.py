from __future__ import annotations

import io
import json
import sys
import unittest
from contextlib import redirect_stdout
from pathlib import Path
from unittest import mock

sys.path.insert(0, str(Path(__file__).resolve().parent))
import repository_source_inventory as inventory  # noqa: E402


class RepositorySourceInventoryTests(unittest.TestCase):
    def test_sensitive_hold_rules_precede_extension_rules(self) -> None:
        examples = {
            "AnimusForge/PlayerExports/private.cs": "HOLD:user-data",
            "原版游戏本体代码1.3.x/Foo.cs": "HOLD:reference-provenance",
            "AnimusForge/ONNX/model.json": "HOLD:model-provenance",
            "_deps_auto/Some.dll": "HOLD:dependency-provenance",
            "tools/PlayerExportsEditor/dist/app.exe": "HOLD:tool-distribution",
            "docs/handoffs/2026-09-11-native-history-snapshot-team-handoff.md": "HOLD:local-only-handoff",
            ".claude/settings.local.json": "HOLD:local-settings",
            "tools/PromptLab/local.settings.json": "HOLD:local-settings",
            "tools/PromptLab/runs/session.jsonl": "HOLD:tool-output",
            "tools/SomeLab/session.jsonl": "HOLD:run-log",
            "tools/SomeLab/trace.trn": "HOLD:run-log",
            "tools/SomeLab/trace.log": "HOLD:run-log",
            "debug.log": "HOLD:run-log",
            "tools/SomeLab/browser/Profile/data.json": "HOLD:user-data",
            "tools/SomeLab/User Data/session.json": "HOLD:user-data",
        }
        for path, expected in examples.items():
            self.assertEqual(inventory.classify_path(path), expected, path)

    def test_known_planes_are_mutually_exclusive_and_unknown_fails_closed(self) -> None:
        examples = {
            "MyBehavior.cs": "source",
            "Refactor/Runtime/Host.cs": "source",
            "AnimusForge/GUI/Prefabs/Panel.xml": "content",
            "tools/ModuleFrameworkApiTests/Program.cs": "tests",
            "tools/PlayerExportsEditor/src/Program.cs": "tools",
            "tools/test_repository_source_inventory.py": "tests",
            "一键编译覆盖推送/build.ps1": "scripts",
            "docs/architecture/map.md": "docs",
            "docs/fixtures/case.json": "tests",
            "AnimusForge/AssetSources/source.png": "design",
        }
        for path, expected in examples.items():
            self.assertEqual(inventory.classify_path(path), expected, path)
        self.assertIsNone(inventory.classify_path("unknown/new.bin"))
        self.assertEqual(inventory.classify_path("Mystery.cs"), "source")  # transitional, not verified business owner

    def test_git_inventory_reads_only_git_metadata_and_hides_sensitive_paths(self) -> None:
        index = (
            b"100644 " + b"a" * 40 + b" 0\tMyBehavior.cs\0"
            b"100644 " + b"b" * 40 + b" 0\tAnimusForge/PlayerExports/private.json\0"
        )
        def fake_run(command, **kwargs):
            self.assertEqual(command[0], "git")
            if command[1:3] == ["ls-files", "--stage"]:
                return mock.Mock(stdout=index)
            if command[1] == "cat-file":
                return mock.Mock(stdout="".join(["a" * 40 + " blob 10\n", "b" * 40 + " blob 20\n"]))
            if command[1] == "ls-files":
                return mock.Mock(stdout=b"")
            self.fail(command)

        with mock.patch.object(inventory.subprocess, "run", side_effect=fake_run) as run, \
             mock.patch("builtins.open", side_effect=AssertionError("opened file content")):
            report = inventory.build_report(Path("."))
        self.assertEqual(report["tracked_count"], 2)
        self.assertEqual(report["categories"], {"HOLD:user-data": 1, "source": 1})
        self.assertEqual(report["bytes"], {"HOLD:user-data": 20, "source": 10})
        self.assertNotIn("private.json", json.dumps(report))
        self.assertEqual(run.call_count, 5)

    def test_unknown_path_returns_failure_without_disclosing_path(self) -> None:
        report = {"unknown_count": 1, "tracked_count": 1}
        out = io.StringIO()
        with mock.patch.object(inventory, "build_report", return_value=report), redirect_stdout(out):
            self.assertEqual(inventory.main([]), 1)
        self.assertNotIn("private", out.getvalue())

    def test_index_rejects_duplicate_and_unmerged_entries(self) -> None:
        row = b"100644 " + b"a" * 40 + b" 0\tMyBehavior.cs\0"
        with mock.patch.object(inventory, "git_output", return_value=row + row):
            with self.assertRaisesRegex(ValueError, "duplicate"):
                inventory.index_entries(Path("."))
        with mock.patch.object(inventory, "git_output", return_value=row.replace(b" 0\t", b" 1\t")):
            with self.assertRaisesRegex(ValueError, "Unmerged"):
                inventory.index_entries(Path("."))

    def test_blob_sizes_rejects_non_blob_and_incomplete_metadata(self) -> None:
        oid = "a" * 40
        with mock.patch.object(inventory, "git_output", return_value=f"{oid} commit 3\n"):
            with self.assertRaisesRegex(ValueError, "not a blob"):
                inventory.blob_sizes(Path("."), [oid])
        with mock.patch.object(inventory, "git_output", return_value=""):
            with self.assertRaisesRegex(ValueError, "Incomplete"):
                inventory.blob_sizes(Path("."), [oid])

    def test_untracked_groups_suppress_sensitive_and_unknown_roots(self) -> None:
        raw = b"bin/app.dll\0local/private.json\0AnimusForge/PlayerExports/private.json\0secret.txt\0"
        with mock.patch.object(inventory, "git_output", return_value=raw):
            self.assertEqual(inventory.path_group_counts(Path("."), "--others"),
                             {"bin": 1, "local": 1, "other": 2})


if __name__ == "__main__":
    unittest.main()
