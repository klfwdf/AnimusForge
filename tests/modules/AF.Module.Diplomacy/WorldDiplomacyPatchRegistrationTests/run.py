"""Replay the complete production patch-registration and campaign event methods."""
from pathlib import Path
import argparse
import hashlib
import importlib.util
import json
import subprocess
import sys
from html import escape

ROOT = next(p for p in Path(__file__).resolve().parents if (p / "AnimusForge.csproj").is_file())
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, minimal_test_environment, resolve_dotnet


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet")
    parser.add_argument("--framework", choices=("net8.0", "net10.0"), default="net8.0")
    parser.add_argument("--run-root", type=Path)
    args = parser.parse_args()
    dotnet = resolve_dotnet(ROOT, args.dotnet)
    out = new_run_root(ROOT, "world-diplomacy-patch-registration", args.run_root)
    spec = importlib.util.spec_from_file_location(
        "registration_extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
    extract = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(extract)
    owner = ROOT / "src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs"
    source = owner.read_text(encoding="utf-8-sig")
    signatures = ["public static void RegisterHarmonyPatches(",
                  "public override void RegisterEvents(", "private static void Log("]
    if "private static void ApplyHarmonyPatchOnce(" in source:
        signatures.append("private static void ApplyHarmonyPatchOnce(")
    declarations = [extract.declaration(source, signature) for signature in signatures]
    fields = [line for line in source.splitlines() if "private static bool _patchesApplied;" in line
              or "private static readonly HashSet<MethodBase> _installedPatchTargets =" in line]
    assert len(fields) in (1, 2) and any("_patchesApplied" in line for line in fields)
    generated = ("using System; using System.Collections.Generic; using System.Reflection; "
                 "using HarmonyLib; using TaleWorlds.CampaignSystem; using TaleWorlds.CampaignSystem.Election; "
                 "namespace AnimusForge { public sealed partial class WorldDiplomacyBehavior {\n"
                 + "\n".join(fields + declarations) + "\n}}")
    (out / "ProductionRegistration.cs").write_text(generated, encoding="utf-8")
    project = out / "WorldDiplomacyPatchRegistrationTests.csproj"
    project.write_text(
        '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
        f'<TargetFramework>{args.framework}</TargetFramework><NuGetAudit>false</NuGetAudit>'
        '</PropertyGroup><ItemGroup><Compile Include="'
        + escape(str(HERE / "Program.cs"), quote=True)
        + '" /></ItemGroup></Project>', encoding="utf-8")
    (out / "NuGet.Config").write_text(
        '<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
    command = [str(dotnet), "run", "--project", str(project), "-c", "Release"]
    result = subprocess.run(command, cwd=out, env=minimal_test_environment(dotnet, out),
                            capture_output=True, text=True, encoding="utf-8", timeout=120)
    log = result.stdout + result.stderr
    (out / "run.log").write_text(log, encoding="utf-8")
    (out / "receipt.json").write_text(json.dumps({
        "exitCode": result.returncode, "command": command, "framework": args.framework,
        "ownerSha256": hashlib.sha256(owner.read_bytes()).hexdigest(),
        "harnessSha256": hashlib.sha256((HERE / "Program.cs").read_bytes()).hexdigest(),
        "declarationSha256": {name: hashlib.sha256(text.encode()).hexdigest()
                              for name, text in zip(signatures, declarations)},
        "scope": "Unaltered whole production registration/RegisterEvents/Log/helper declarations; "
                 "controlled Harmony/game/logger/sprite leaves; no live game or historical inverse gate",
    }, indent=2), encoding="utf-8")
    print(out)
    print(log)
    return result.returncode


if __name__ == "__main__":
    raise SystemExit(main())
