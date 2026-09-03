from __future__ import annotations

import contextlib
import io
import json
import subprocess
import sys
import unittest
from pathlib import Path
from unittest import mock

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools"))
import PersistenceIdentityAudit as audit  # noqa: E402


class PersistenceIdentityAuditTests(unittest.TestCase):
    def test_batch_parser_reads_multiple_blobs_and_missing(self) -> None:
        payload = b"abc"
        data = b"a" * 40 + b" blob 3\n" + payload + b"\n" + b"b" * 40 + b" missing\n"
        result = audit.parse_batch_cat_file(data, expected_objects=2)
        self.assertEqual(result["a" * 40], "abc")

    def test_batch_parser_rejects_truncated_blob(self) -> None:
        with self.assertRaises(ValueError):
            audit.parse_batch_cat_file(b"a" * 40 + b" blob 4\nabc\n")

    def test_current_snapshot_is_reused(self) -> None:
        snapshot = [(Path("one.cs"), "class One : CampaignBehaviorBase {}")]
        with mock.patch.object(audit, "current_source_snapshot", side_effect=AssertionError("re-enumerated")):
            self.assertEqual(audit.current_sync(snapshot), set())
            self.assertEqual(audit.current_behaviors(snapshot), {"One"})

    def test_json_output_is_stdout_only_and_progress_is_stderr(self) -> None:
        process = subprocess.run(
            [sys.executable, str(ROOT / "tools" / "PersistenceIdentityAudit.py"), "--json"],
            cwd=ROOT, capture_output=True, text=True, timeout=180,
        )
        self.assertIn('"status"', process.stdout)
        self.assertNotIn("current source enumeration", process.stdout)
        self.assertIn("current source enumeration", process.stderr)
        quiet = subprocess.run(
            [sys.executable, str(ROOT / "tools" / "PersistenceIdentityAudit.py"), "--json", "--quiet"],
            cwd=ROOT, capture_output=True, text=True, timeout=180,
        )
        self.assertNotIn("current source enumeration", quiet.stderr)

    def test_fail_closed_on_baseline_error(self) -> None:
        with mock.patch.object(audit, "current_source_snapshot", return_value=[]), \
             mock.patch.object(audit, "baseline_source_snapshot", side_effect=RuntimeError("baseline unavailable")), \
             mock.patch.object(sys, "argv", ["PersistenceIdentityAudit.py", "--json", "--quiet"]):
            stdout = io.StringIO()
            with contextlib.redirect_stdout(stdout):
                self.assertEqual(audit.main(), 1)
            self.assertEqual(json.loads(stdout.getvalue())["status"], "FAIL")


if __name__ == "__main__":
    unittest.main(verbosity=2)
