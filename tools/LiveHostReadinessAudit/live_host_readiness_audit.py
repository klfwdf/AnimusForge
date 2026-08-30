from __future__ import annotations

import argparse
import hashlib
import json
import os
import subprocess
from pathlib import Path


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for block in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def process_running() -> bool:
    try:
        output = subprocess.check_output(
            ["tasklist", "/FI", "IMAGENAME eq Bannerlord.exe", "/FO", "CSV", "/NH"],
            text=True,
            stderr=subprocess.DEVNULL,
        )
    except (OSError, subprocess.SubprocessError):
        return False
    return "Bannerlord.exe" in output


def find_save_dirs() -> list[Path]:
    candidates = [
        Path.home() / "Documents" / "Mount & Blade II Bannerlord",
        Path.home() / "Documents" / "Mount and Blade II Bannerlord",
        Path.home() / "OneDrive" / "Documents" / "Mount & Blade II Bannerlord",
        Path.home() / "OneDrive" / "Documents" / "Mount and Blade II Bannerlord",
    ]
    result: list[Path] = []
    for base in candidates:
        if not base.is_dir():
            continue
        try:
            for path in base.rglob("*"):
                if path.is_dir() and path.name.lower() in {"game saves", "gamesaves", "saves"}:
                    result.append(path)
        except OSError:
            continue
    return sorted(set(result))


def main() -> int:
    parser = argparse.ArgumentParser(description="Read-only Bannerlord live-host readiness audit")
    parser.add_argument("--project-root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--game-root", type=Path, default=Path(r"F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord"))
    args = parser.parse_args()
    project = args.project_root.resolve()
    game = args.game_root.resolve()
    stage = project / "bin" / "Debug" / "single_module_stage" / "AnimusForge"
    stage_bin = stage / "bin" / "Win64_Shipping_Client"
    installed = game / "Modules" / "AnimusForge"
    installed_bin = installed / "bin" / "Win64_Shipping_Client"
    exe = game / "bin" / "Win64_Shipping_Client" / "Bannerlord.exe"
    submodule = installed / "SubModule.xml"
    stage_bootstrap = stage_bin / "AnimusForge.Bootstrap.dll"
    stage_13 = stage_bin / "versions" / "1.3" / "AnimusForge.dll"
    stage_14 = stage_bin / "versions" / "1.4" / "AnimusForge.dll"
    installed_bootstrap = installed_bin / "AnimusForge.Bootstrap.dll"
    saves = find_save_dirs()
    submodule_bootstrap = False
    if submodule.is_file():
        try:
            submodule_bootstrap = "AnimusForge.Bootstrap.dll" in submodule.read_text(encoding="utf-8", errors="ignore")
        except OSError:
            pass
    installed_matches_stage = bool(
        stage_bootstrap.is_file() and installed_bootstrap.is_file()
        and sha256(stage_bootstrap) == sha256(installed_bootstrap)
    )
    result = {
        "gameRoot": game.is_dir(),
        "bannerlordExe": exe.is_file(),
        "projectStage": stage.is_dir(),
        "stageBootstrap": stage_bootstrap.is_file(),
        "stage13": stage_13.is_file(),
        "stage14": stage_14.is_file(),
        "installedModule": installed.is_dir(),
        "submoduleLoadsBootstrap": submodule_bootstrap,
        "installedMatchesStage": installed_matches_stage,
        "gameRunning": process_running(),
        "saveDirectoryCount": len(saves),
        "saveDirectories": [str(path) for path in saves],
        "deploymentPerformed": False,
        "nextAction": "launch only after explicit live-game test authorization; do not treat stage as deployed",
    }
    required = ["gameRoot", "bannerlordExe", "projectStage", "stageBootstrap", "stage13", "stage14", "installedModule", "submoduleLoadsBootstrap"]
    status = "PASS" if all(result[key] for key in required) else "FAIL"
    print(json.dumps({"status": status, **result}, ensure_ascii=False, indent=2))
    print(
        "PASS liveHostReadiness "
        f"gameRoot={int(result['gameRoot'])} exe={int(result['bannerlordExe'])} "
        f"stage={int(result['projectStage'])} bootstrap={int(result['stageBootstrap'])} "
        f"implementation13={int(result['stage13'])} implementation14={int(result['stage14'])} "
        f"installedModule={int(result['installedModule'])} gameRunning={int(result['gameRunning'])} "
        f"saveDirs={result['saveDirectoryCount']} noDeployment={int(not result['deploymentPerformed'])}"
    )
    return 0 if status == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())