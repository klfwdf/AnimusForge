from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[3]
DEFAULT_RUN_ROOT = ROOT / "artifacts" / "j15-content" / "j15a-contracts"

EXPECTED = {
    "ModuleData/PreprocessPrompts.json": {
        "owner": "AF.Module.Prompt",
        "source": "content/modules/AF.Module.Prompt/ModuleData/PreprocessPrompts.json",
        "logicalName": "AnimusForge.Defaults.PreprocessPrompts.json",
        "withCulture": None,
        "sha256": "985FB377A96B7530396189EF5C23AE4AFB1F29A3C2E4400D1CB163EE98743099",
    },
    "ModuleData/RpItemIntroductionPrompts.json": {
        "owner": "AF.Module.Economy",
        "source": "content/modules/AF.Module.Economy/ModuleData/RpItemIntroductionPrompts.json",
        "logicalName": "AnimusForge.Defaults.RpItemIntroductionPrompts.json",
        "withCulture": None,
        "sha256": "EC30EB9B15030189F6F49E30BC3F3955848524CCC99F388A4784EE015606458D",
    },
    "ModuleData/GcczTownPrompt.zh-CN.json": {
        "owner": "AnimusForge.SiegeAftermathIntervention",
        "source": "content/modules/AnimusForge.SiegeAftermathIntervention/ModuleData/GcczTownPrompt.zh-CN.json",
        "logicalName": "AnimusForge.Defaults.GcczTownPrompt.zh-CN.json",
        "withCulture": "false",
        "sha256": "7537562A3B84A364432BFE870A7E3FABCE4B1AB7E6042B388A010685AA06C552",
    },
    "ModuleData/GcczTownEntryPresentation.zh-CN.json": {
        "owner": "AnimusForge.SiegeAftermathIntervention",
        "source": "content/modules/AnimusForge.SiegeAftermathIntervention/ModuleData/GcczTownEntryPresentation.zh-CN.json",
        "logicalName": "AnimusForge.Defaults.GcczTownEntryPresentation.zh-CN.json",
        "withCulture": "false",
        "sha256": "F99FDE95CF454D26F028B7B2B95EA694603710118E95EB4CEB2B8C2AF9FC63AB",
    },
    "ModuleData/GcczTownActionPresentation.zh-CN.json": {
        "owner": "AnimusForge.SiegeAftermathIntervention",
        "source": "content/modules/AnimusForge.SiegeAftermathIntervention/ModuleData/GcczTownActionPresentation.zh-CN.json",
        "logicalName": "AnimusForge.Defaults.GcczTownActionPresentation.zh-CN.json",
        "withCulture": "false",
        "sha256": "8F069A55C5784CD14539EA4B4C4D1763EA3A9D7B613B6A35B35A425EB484523F",
    },
    "ModuleData/GcczTownHiddenResidents.zh-CN.json": {
        "owner": "AnimusForge.SiegeAftermathIntervention",
        "source": "content/modules/AnimusForge.SiegeAftermathIntervention/ModuleData/GcczTownHiddenResidents.zh-CN.json",
        "logicalName": "AnimusForge.Defaults.GcczTownHiddenResidents.zh-CN.json",
        "withCulture": "false",
        "sha256": "11FC1D2FD52FC264B9DDBA70F1C7ADF311A9D8DC40CE60944E091959A015E2B0",
    },
    "ModuleData/GcczTownManual.zh-CN.json": {
        "owner": "AnimusForge.SiegeAftermathIntervention",
        "source": "content/modules/AnimusForge.SiegeAftermathIntervention/ModuleData/GcczTownManual.zh-CN.json",
        "logicalName": "AnimusForge.Defaults.GcczTownManual.zh-CN.json",
        "withCulture": "false",
        "sha256": "625F65983F53D817AAB96CFC88FE489979017B9C19FF0E575A8AE4F4273B790D",
    },
}


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def load_module(path: Path, name: str):
    spec = importlib.util.spec_from_file_location(name, path)
    check(spec is not None and spec.loader is not None, f"cannot import {path}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def verify_map_and_resources() -> None:
    map_path = ROOT / "content" / "content-map.json"
    payload = json.loads(map_path.read_text(encoding="utf-8"))
    check(payload.get("schemaVersion") == 1, "content map schemaVersion")
    entries = payload.get("entries")
    check(isinstance(entries, list) and len(entries) == 7, "content map must contain seven entries")
    by_target = {entry["target"]: entry for entry in entries}
    check(set(by_target) == set(EXPECTED), "content map target set")

    for target, expected in EXPECTED.items():
        entry = by_target[target]
        for field in ("owner", "source", "logicalName"):
            check(entry.get(field) == expected[field], f"{target} {field}")
        source = ROOT / expected["source"]
        check(source.is_file(), f"missing migrated source: {source}")
        digest = hashlib.sha256(source.read_bytes()).hexdigest().upper()
        check(digest == expected["sha256"], f"source hash drift: {target}")
        old = ROOT / "AnimusForge" / target.replace("/", "\\")
        check(not old.exists(), f"old editable source remains: {old}")


def verify_project_resources() -> None:
    project = ET.parse(ROOT / "AnimusForge.csproj")
    resources = {}
    for item in project.getroot().iter("EmbeddedResource"):
        include = item.attrib.get("Include", "").replace("\\", "/")
        logical = item.findtext("LogicalName")
        culture = item.findtext("WithCulture")
        if logical:
            resources[logical] = {"include": include, "culture": culture}
    expected_names = {item["logicalName"] for item in EXPECTED.values()}
    check(set(resources) == expected_names, "EmbeddedResource LogicalName set must remain exactly seven")
    for expected in EXPECTED.values():
        actual = resources[expected["logicalName"]]
        check(actual["include"] == expected["source"], f"include path: {expected['logicalName']}")
        check(actual["culture"] == expected["withCulture"], f"WithCulture: {expected['logicalName']}")


def verify_script_wiring() -> None:
    deploy = (ROOT / "一键编译覆盖推送" / "deploy_module.ps1").read_text(encoding="utf-8-sig")
    call = "Invoke-AnimusForgeContentProjection"
    check(deploy.count(call) == 2, "Stage and Deploy must each project content once")
    stage_start = deploy.index('if (-not [string]::IsNullOrWhiteSpace($StageOnlyOutputDir))')
    stage_end = deploy.index("$legacy13ModuleDir", stage_start)
    stage = deploy[stage_start:stage_end]
    check(stage.index("Invoke-Robocopy") < stage.index(call) < stage.index("Set-SingleModuleIdentity"),
          "Stage projection order")
    deploy_start = deploy.index("$sourceCopyArguments = @(")
    deploy_end = deploy.index("Set-SingleModuleIdentity", deploy_start)
    deploy_block = deploy[deploy_start:deploy_end]
    check(deploy_block.index("Invoke-Robocopy") < deploy_block.index(call) < deploy_block.index("Merge-InstalledCustomPromptsIntoStaging"),
          "Deploy projection must precede installed prompt merge")
    check("Get-AnimusForgeContentSourcePath" in deploy, "source hash lookup must use content map")


def verify_inventory_and_overlay() -> None:
    inventory = load_module(ROOT / "tools" / "repository_source_inventory.py", "j15_inventory")
    check(inventory.classify_path("content/content-map.json") == "content", "map inventory class")
    check(inventory.classify_path(EXPECTED["ModuleData/PreprocessPrompts.json"]["source"]) == "content", "prompt content class")
    check(inventory.classify_path("content/modules/Unknown/ModuleData/file.json") is None, "unknown content owner must fail closed")
    check(inventory.classify_path("content/PlayerExports/private.json") == "HOLD:user-data", "content user data hold")
    check(inventory.classify_path("content/modules/AF.Module.Knowledge/ONNX/model.json") == "HOLD:model-provenance", "content model hold")
    check(inventory.classify_path("content/modules/AF.Module.Knowledge/onnx/model.json") == "HOLD:model-provenance", "content model hold is case-insensitive")
    check(inventory.classify_path("content/modules/AF.Module.UI/AssetPackages/ui.bin") == "HOLD:asset-package-provenance", "content asset-package hold")

    overlay = load_module(ROOT / "tools" / "package_policy_system_source_overlay.py", "j15_overlay")
    files, categories = overlay.build_file_set()
    delivery = "AnimusForge/ModuleData/PreprocessPrompts.json"
    check(delivery in files, "overlay delivery path preserved")
    check(files[delivery].resolve() == (ROOT / EXPECTED["ModuleData/PreprocessPrompts.json"]["source"]).resolve(), "overlay source uses content map")
    check(categories[delivery] == "runtime_assets", "overlay category preserved")


def run_command(command: list[str]) -> None:
    completed = subprocess.run(command, cwd=ROOT, text=True, encoding="utf-8", errors="replace")
    check(completed.returncode == 0, f"command failed ({completed.returncode}): {' '.join(command)}")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--run-root", type=Path, default=DEFAULT_RUN_ROOT)
    args = parser.parse_args()
    run_root = args.run_root.resolve()
    check(run_root == DEFAULT_RUN_ROOT.resolve(), "run root must remain the authorized J15a directory")
    if run_root.exists():
        shutil.rmtree(run_root)
    run_root.mkdir(parents=True)

    verify_map_and_resources()
    verify_project_resources()
    verify_script_wiring()
    verify_inventory_and_overlay()

    pwsh = Path(r"C:\Program Files\PowerShell\7-preview\pwsh.exe")
    run_command([
        str(pwsh), "-NoLogo", "-NoProfile", "-File",
        str(Path(__file__).with_name("ContentLayoutContractTests.ps1")),
        "-ProjectRoot", str(ROOT), "-RunRoot", str(run_root),
    ])
    dotnet = ROOT / "local" / "dotnet" / "8.0.425" / "dotnet.exe"
    run_command([
        str(dotnet), "run", "--project", str(Path(__file__).with_name("GcczLoaderHarness.csproj")),
        "-c", "Release", "--", str(run_root / "gccz"),
    ])
    print("j15ContentContracts mappings=7 invalidCases=8 gcczFallbackCases=4 overlayAlias=1 PASS")
    return 0


if __name__ == "__main__":
    sys.exit(main())
