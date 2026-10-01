"""Runner selection regression; actual Debug/Release seam behavior is separately replayed."""
from __future__ import annotations
import hashlib
import json
import sys
import unittest
from pathlib import Path
from unittest.mock import patch
from uuid import uuid4

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tests"))
import run_all
from output_isolation import new_run_root
ENTRY = "tests/replay/PrimaryLlmGatewayReplayTests/PrimaryLlmGatewayReplayTests.csproj"

class CandidateConfigurationTests(unittest.TestCase):
    def test_primary_manifest_requires_debug_only_for_this_entry(self):
        entries = json.loads((ROOT / "tests/runners.json").read_text(encoding="utf-8"))["entries"]
        self.assertEqual(entries[ENTRY]["candidateConfiguration"], "Debug")
        self.assertEqual([name for name, spec in entries.items() if spec.get("candidateConfiguration")], [ENTRY])

    def test_debug_and_default_candidates_do_not_contaminate_each_other(self):
        output = new_run_root(ROOT, "primary-candidate-selection", None)
        debug, release = output / "debug.dll", output / "release.dll"
        debug.write_bytes(b"debug fixture")
        release.write_bytes(b"release fixture")
        with patch.object(run_all, "DLL14", release), patch.object(run_all, "DEBUG_DLL14", debug):
            for spec, expected in [({"candidateDll": True, "candidateConfiguration": "Debug"}, debug), ({"candidateDll": True}, release)]:
                command = run_all.command(ENTRY, spec, "synthetic", output / uuid4().hex)
                separator = command.index("--")
                self.assertEqual(command[separator + 1:], [str(expected), hashlib.sha256(expected.read_bytes()).hexdigest().upper()])
                self.assertIn("-p:ReplayCandidateDll=" + str(expected), command)
                self.assertEqual(command[command.index("-c") + 1], "Release")  # harness, not candidate
                if spec.get("candidateConfiguration"):
                    self.assertIn("-p:ReplayPrivateRuntimePath=" + str(debug.parent), command)
                    self.assertEqual(sum(value.startswith("-p:ReplayPrivateRuntimePath=") for value in command), 1)

    def test_missing_debug_never_falls_back_to_release(self):
        missing = ROOT / "artifacts" / ("missing-debug-" + uuid4().hex) / "AnimusForge.dll"
        with patch.object(run_all, "DEBUG_DLL14", missing):
            spec = {"candidateDll": True, "candidateConfiguration": "Debug"}
            self.assertEqual(run_all.candidate_dll(spec), missing)
            self.assertFalse(run_all.candidate_dll(spec).is_file())
            self.assertFalse(any(value.startswith("-p:ReplayCandidateDll=") for value in run_all.command(ENTRY, spec, "synthetic")))

    def test_invalid_requirement_is_rejected_before_discovery_or_execution(self):
        for spec in ({"candidateDll": True, "candidateConfiguration": "Release"}, {"candidateConfiguration": "Debug"}):
            with self.subTest(spec=spec), patch.object(Path, "read_text", return_value=json.dumps({"entries": {ENTRY: spec}})), patch.object(run_all, "discover") as discover:
                self.assertEqual(run_all.main(["--list"]), 2)
                discover.assert_not_called()

if __name__ == "__main__":
    unittest.main(verbosity=2)
