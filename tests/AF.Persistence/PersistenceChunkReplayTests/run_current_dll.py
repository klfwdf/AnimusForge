"""Finite current-DLL save compatibility evidence; frozen historical gates stay intact."""
from pathlib import Path
import argparse
import hashlib
import json
import re
import subprocess
import sys
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import minimal_test_environment, new_run_root, resolve_dotnet


def war_schema(source):
    fields = {name: kind for kind, name in re.findall(r"private\s+(List<\w+>|int)\s+(_\w+)\s*(?:=|;)", source)}
    fields["recentBattleSequence"] = "int"
    return {key: fields[name] for key, name in re.findall(r'dataStore.SyncData\("([^"\n]+)", ref (\w+)\)', source)}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--candidate-dll", type=Path, required=True)
    parser.add_argument("--baseline-ref", required=True)
    parser.add_argument("--incoming-ref", required=True)
    parser.add_argument("--run-root", type=Path)
    args = parser.parse_args()
    out = new_run_root(ROOT, "current-dll-persistence", args.run_root)
    sdk = resolve_dotnet(ROOT)
    source = "WarStats/AfWarStatsBehavior.cs"
    historical = lambda ref: subprocess.check_output(["git", "show", f"{ref}:{source}"], cwd=ROOT).decode("utf-8-sig")
    old = war_schema(historical(args.baseline_ref))
    incoming = war_schema(historical(args.incoming_ref))
    current = war_schema((ROOT / source).read_text(encoding="utf-8-sig"))
    assert all(current.get(key) == value for key, value in old.items()), "old WarStats key/type changed"
    assert current == incoming, "current WarStats schema differs from approved incoming schema"
    assert set(current) - set(old) == {"_af_war_stats_active_weariness_v6", "_af_war_stats_history_weariness_v6"}, "unexpected new save keys"
    dll = args.candidate_dll.resolve(strict=True)
    assert dll.is_relative_to(ROOT.parent), "candidate must stay inside task workspace"
    digest = hashlib.sha256(dll.read_bytes()).hexdigest()
    program = Path(__file__).with_name("CurrentDllCompatibilityReplay.cs")
    project = out / "Replay.csproj"
    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><PlatformTarget>x64</PlatformTarget></PropertyGroup><ItemGroup><Compile Include="' + escape(str(program)) + '"/></ItemGroup></Project>', encoding="utf-8")
    (out / "NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding="utf-8")
    env = minimal_test_environment(sdk, out)
    permissions = (sdk.parent / "sdk/8.0.425/DotnetTools/dotnet-format/System.Security.Permissions.dll").resolve(strict=True)
    commands = [[str(sdk), "build", str(project), "-nologo", "-p:RestoreConfigFile=" + str(out / "NuGet.Config")],
                [str(sdk), str(out / "bin/Debug/net8.0/Replay.dll"), str(dll), digest, str(permissions)]]
    receipts = []
    for index, command in enumerate(commands):
        result = subprocess.run(command, cwd=ROOT, env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, encoding="utf-8", errors="replace")
        (out / f"step-{index}.log").write_text(result.stdout, encoding="utf-8")
        receipts.append(dict(command=command, exit=result.returncode))
        print(result.stdout)
        if result.returncode:
            break
    (out / "receipt.json").write_text(json.dumps(dict(scope="current-DLL-compatibility-not-live-save", baseline=args.baseline_ref, incoming=args.incoming_ref, oldKeys=len(old), newKeys=len(current)-len(old), schema=current, dllSha256=digest, permissionsSha256=hashlib.sha256(permissions.read_bytes()).hexdigest(), inputs={str(program.relative_to(ROOT)): hashlib.sha256(program.read_bytes()).hexdigest()}, commands=receipts), indent=2), encoding="utf-8")
    return receipts[-1]["exit"]


if __name__ == "__main__":
    raise SystemExit(main())
