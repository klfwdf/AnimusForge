"""Shipped worldbooks are the reviewed Aug-30 defaults, never local player exports."""
from __future__ import annotations

import hashlib
import json
from pathlib import Path
import subprocess
import unittest

ROOT = Path(__file__).resolve().parents[3]
BASELINE = "d4cb1467376c6e923f4295dcefc7878c11dbc7c1"
SOURCE_ROOT = "content/modules/AF.Module.Onboarding/PlayerExports/"


def expected_entries(root: Path = ROOT) -> dict[str, dict[str, str]]:
    records = subprocess.check_output(
        ["git", "ls-tree", "-rz", BASELINE, "--", "AnimusForge/PlayerExports"], cwd=root)
    result = {}
    for record in records.split(b"\0"):
        if not record:
            continue
        metadata, path = record.split(b"\t", 1)
        relative = path.decode("utf-8").removeprefix("AnimusForge/PlayerExports/")
        result["PlayerExports/" + relative] = {
            "owner": "AF.Module.Onboarding", "source": SOURCE_ROOT + relative,
            "gitBlob": metadata.split()[2].decode("ascii"),
        }
    return result


def verify_builtin_worldbooks(root: Path = ROOT) -> None:
    expected = expected_entries(root)
    assert len(expected) == 3139, "Reviewed worldbook baseline changed"
    entries = json.loads((root / "content/content-map.json").read_text(encoding="utf-8-sig"))["entries"]
    actual = {e["target"]: e for e in entries if e["target"].startswith("PlayerExports/")}
    assert actual.keys() == expected.keys(), "Stage/ZIP map must include exactly the reviewed defaults"
    for target, entry in expected.items():
        assert actual[target]["owner"] == entry["owner"], target
        assert actual[target]["source"] == entry["source"], target
        data = (root / entry["source"]).read_bytes().replace(b"\r\n", b"\n")
        blob = hashlib.sha1(b"blob " + str(len(data)).encode() + b"\0" + data).hexdigest()
        assert blob == entry["gitBlob"], "Default content drift: " + target
    packages = {target.split("/")[1] for target in expected}
    assert len(packages) == 4, "All four original worldbooks must ship"
    for package in packages:
        for section in ("knowledge", "personality_background", "unnamed_persona",
                        "kingdom_profiles", "event_data", "voice_mapping"):
            assert any(t.startswith(f"PlayerExports/{package}/{section}/") for t in expected), (package, section)


class BuiltinWorldbooksTests(unittest.TestCase):
    def test_exact_reviewed_defaults_are_mapped_and_complete(self) -> None:
        verify_builtin_worldbooks()


if __name__ == "__main__":
    unittest.main()
