from __future__ import annotations

import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "tools" / "LiveHostReadinessAudit" / "live_host_readiness_audit.py"


class LiveHostReadinessAuditCliTests(unittest.TestCase):
    def run_cli(self, *arguments: str, home: Path | None = None) -> subprocess.CompletedProcess[str]:
        environment = os.environ.copy()
        if home is not None:
            # Path.home() uses USERPROFILE on Windows and HOME on other platforms.
            environment["USERPROFILE"] = str(home)
            environment["HOME"] = str(home)
        return subprocess.run(
            [sys.executable, "-B", str(SCRIPT), *arguments],
            cwd=ROOT,
            env=environment,
            capture_output=True,
            text=True,
            check=False,
        )

    @staticmethod
    def parse_payload(result: subprocess.CompletedProcess[str]) -> dict:
        lines = result.stdout.splitlines()
        summary_index = next(
            (index for index, line in enumerate(lines) if line.endswith(" liveHostReadiness ") or " liveHostReadiness " in line),
            len(lines),
        )
        return json.loads("\n".join(lines[:summary_index]))

    @staticmethod
    def make_fixture(root: Path) -> tuple[Path, Path]:
        project = root / "project"
        game = root / "game"
        stage_bin = project / "bin" / "Debug" / "single_module_stage" / "AnimusForge" / "bin" / "Win64_Shipping_Client"
        installed_bin = game / "Modules" / "AnimusForge" / "bin" / "Win64_Shipping_Client"
        (stage_bin / "versions" / "1.3").mkdir(parents=True)
        (stage_bin / "versions" / "1.4").mkdir(parents=True)
        (installed_bin).mkdir(parents=True)
        (game / "bin" / "Win64_Shipping_Client").mkdir(parents=True)
        (game / "Modules" / "AnimusForge").mkdir(parents=True, exist_ok=True)
        bootstrap = b"fixture-bootstrap"
        (stage_bin / "AnimusForge.Bootstrap.dll").write_bytes(bootstrap)
        (installed_bin / "AnimusForge.Bootstrap.dll").write_bytes(bootstrap)
        (stage_bin / "versions" / "1.3" / "AnimusForge.dll").write_bytes(b"fixture-13")
        (stage_bin / "versions" / "1.4" / "AnimusForge.dll").write_bytes(b"fixture-14")
        (game / "bin" / "Win64_Shipping_Client" / "Bannerlord.exe").write_bytes(b"fixture-exe")
        (game / "Modules" / "AnimusForge" / "SubModule.xml").write_text(
            "<Module><DependedModule Id=\"AnimusForge.Bootstrap.dll\" /></Module>",
            encoding="utf-8",
        )
        return project, game

    def test_game_root_is_required(self) -> None:
        result = self.run_cli()
        self.assertEqual(result.returncode, 2)
        self.assertIn("--game-root", result.stderr)
        self.assertNotIn("liveHostReadiness", result.stdout)

    def test_explicit_fixture_roots_pass(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            project, game = self.make_fixture(root)
            result = self.run_cli(
                "--project-root",
                str(project),
                "--game-root",
                str(game),
                home=root / "profile",
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn("PASS liveHostReadiness", result.stdout)
            payload = self.parse_payload(result)
            self.assertEqual(payload["status"], "PASS")
            self.assertTrue(payload["installedMatchesStage"])
            self.assertEqual(payload["saveDirectoryCount"], 0)

    def test_missing_game_root_fails_closed(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            result = self.run_cli(
                "--project-root",
                str(root / "project"),
                "--game-root",
                str(root / "missing-game"),
                home=root / "profile",
            )
            self.assertEqual(result.returncode, 1)
            self.assertIn("FAIL liveHostReadiness", result.stdout)
            payload = self.parse_payload(result)
            self.assertEqual(payload["status"], "FAIL")
            self.assertFalse(payload["gameRoot"])

    def test_no_machine_bound_paths_remain(self) -> None:
        source = SCRIPT.read_text(encoding="utf-8")
        readme = SCRIPT.with_name("README.md").read_text(encoding="utf-8")
        for document in (source, readme):
            self.assertNotIn("F:\\SteamLibrary", document)
            self.assertNotIn("F:\\AF测试重构", document)


if __name__ == "__main__":
    unittest.main()
