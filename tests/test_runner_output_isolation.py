"""Regression checks for the six standalone runner output directories.

The fake SDK stops at subprocess boundaries; these tests inspect filesystem
isolation only, not the C# assertions or build results.
"""
from __future__ import annotations

import os
import runpy
import shutil
import subprocess
import sys
import unittest
from pathlib import Path
from unittest.mock import patch
from uuid import uuid4

TESTS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TESTS_DIR))
from output_isolation import new_run_root


REPO = TESTS_DIR.parent
FIXTURE = REPO / "artifacts/j17b/runner-safety" / ("fixture-" + uuid4().hex)
RUNNERS = (
    ("tests/AF.Persistence/OwnerJsonStorageCodec", "OwnerJsonStorageCodecTests.csproj", ("Program.cs",)),
    ("tests/AF.Persistence/PlayerExports", "PlayerExportsTests.csproj", ("Program.cs",)),
    ("tests/modules/AF.Module.Knowledge/Entities", "KnowledgeEntitiesTests.csproj", ("Program.cs",)),
    ("tests/modules/AF.Module.Knowledge/Index", "KnowledgeIndexTests.csproj", ("Program.cs", "Stubs.cs")),
    ("tests/modules/AF.Module.Memory/Records", "MemoryRecordsTests.csproj", ("Program.cs",)),
    ("tests/modules/AF.Module.Prompt/Composition", "PromptCompositionTests.csproj", ("Program.cs", "Stubs.cs")),
)
OLD_OUTPUTS = (
    "persistence-owner-json-codec",
    "persistence-player-exports",
    "knowledge-j06-entities",
    "knowledge-j06-index",
    "memory-j05-records",
    "prompt-j04-composition",
)


class RunnerOutputIsolationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        for rel, project, other in RUNNERS:
            source = REPO / rel
            target = FIXTURE / rel
            target.mkdir(parents=True)
            for name in ("run.py", project, *other):
                shutil.copy2(source / name, target / name)
        helper = REPO / "tests/output_isolation.py"
        if helper.exists():
            shutil.copy2(helper, FIXTURE / "tests/output_isolation.py")

    def invoke(self, index: int, *args: str) -> int:
        runner = FIXTURE / RUNNERS[index][0] / "run.py"
        fake = subprocess.CompletedProcess([sys.executable], 0, "", "")
        with patch.dict(os.environ, {"AF_DOTNET": sys.executable}, clear=True), \
                patch.object(sys, "argv", [str(runner), *args]), \
                patch("subprocess.run", return_value=fake):
            try:
                runpy.run_path(str(runner), run_name="__main__")
            except SystemExit as ex:
                if isinstance(ex.code, int):
                    return ex.code
                return 1
        return 0

    def test_default_never_deletes_old_current(self) -> None:
        for index, family in enumerate(OLD_OUTPUTS):
            with self.subTest(family=family):
                parent = FIXTURE / "artifacts/tests" / family
                old = parent / "current"
                old.mkdir(parents=True, exist_ok=True)
                sentinel = old / "keep.txt"
                sentinel.write_text("old work", encoding="utf-8")
                before = set(parent.iterdir())
                self.assertEqual(self.invoke(index), 0)
                self.assertEqual(sentinel.read_text(encoding="utf-8"), "old work")
                created = set(parent.iterdir()) - before
                self.assertEqual(len(created), 1)
                self.assertNotEqual(next(iter(created)), old)

    def test_explicit_run_root_rejects_existing_and_escape(self) -> None:
        for index, _ in enumerate(RUNNERS):
            with self.subTest(index=index):
                requested = FIXTURE / "artifacts/explicit" / (str(index) + "-" + uuid4().hex)
                self.assertEqual(self.invoke(index, "--run-root", str(requested)), 0)
                marker = requested / "keep.txt"
                marker.write_text("keep", encoding="utf-8")
                self.assertNotEqual(self.invoke(index, "--run-root", str(requested)), 0)
                self.assertEqual(marker.read_text(encoding="utf-8"), "keep")
                outside = FIXTURE.parent / ("escape-" + uuid4().hex)
                self.assertNotEqual(self.invoke(index, "--run-root", str(outside)), 0)
                self.assertFalse(outside.exists())

    def test_reparse_parent_is_rejected_when_supported(self) -> None:
        real = FIXTURE / "artifacts/real"
        real.mkdir(parents=True, exist_ok=True)
        link = FIXTURE / "artifacts/link"
        try:
            link.symlink_to(real, target_is_directory=True)
        except (OSError, NotImplementedError) as ex:
            self.skipTest("symlink creation unavailable: " + str(ex))
        for index, _ in enumerate(RUNNERS):
            with self.subTest(index=index):
                requested = link / (str(index) + "-" + uuid4().hex)
                self.assertNotEqual(self.invoke(index, "--run-root", str(requested)), 0)
                self.assertFalse(requested.exists())

    def test_reparse_parent_check_without_symlink_privilege(self) -> None:
        parent = FIXTURE / "artifacts/reparse-simulated"
        parent.mkdir(parents=True, exist_ok=True)
        requested = parent / ("run-" + uuid4().hex)
        with patch.object(Path, "is_junction", lambda path: path == parent):
            with self.assertRaisesRegex(SystemExit, "reparse point"):
                new_run_root(FIXTURE, "unused", requested)
        self.assertFalse(requested.exists())


if __name__ == "__main__":
    unittest.main()
