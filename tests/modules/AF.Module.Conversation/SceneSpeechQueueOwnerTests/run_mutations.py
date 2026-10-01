"""Compile effective mutations of the production Scene speech queue owner."""

import argparse
import os
from pathlib import Path
import subprocess
import sys


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
SOURCE = ROOT / "src/modules/AF.Module.Conversation/Channels/Scene/SceneSpeechQueueOwner.cs"

MUTATIONS = {
    "duplicate-worker": (
        "if (_workerRunning)\n\t\t\t{\n\t\t\t\treturn false;\n\t\t\t}",
        "if (false)\n\t\t\t{\n\t\t\t\treturn false;\n\t\t\t}",
        "one-worker-lease-and-fifo",
    ),
    "worker-never-retires": (
        "_workerRunning = false;\n\t\t\t\titem = default;",
        "item = default;",
        "empty-dequeue-retires-worker",
    ),
    "reset-keeps-queue": (
        "internal T[] Reset()\n\t{\n\t\tlock (_gate)\n\t\t{\n\t\t\tT[] dropped = _queue.ToArray();\n\t\t\t_queue.Clear();",
        "internal T[] Reset()\n\t{\n\t\tlock (_gate)\n\t\t{\n\t\t\tT[] dropped = _queue.ToArray();",
        "reset-clears-queue-and-worker",
    ),
}


sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", default=None)
    parser.add_argument("--run-root", type=Path)
    args = parser.parse_args()
    args.dotnet = str(resolve_dotnet(ROOT, args.dotnet))
    run_root = new_run_root(ROOT, "scene-queue-mutations", args.run_root)
    source = SOURCE.read_text(encoding="utf-8-sig")
    failed = 0

    for name, (old, new, expected_case) in MUTATIONS.items():
        if source.count(old) != 1:
            raise RuntimeError(f"mutation anchor count for {name}: {source.count(old)}")
        output = run_root / name
        output.mkdir(parents=True, exist_ok=True)
        (output / "SceneSpeechQueueOwner.cs").write_text(source.replace(old, new), encoding="utf-8")
        (output / "Program.cs").write_text((HERE / "Program.cs").read_text(encoding="utf-8-sig"), encoding="utf-8")
        (output / "Tests.csproj").write_text(
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup>"
            "<OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>"
            "<ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable>"
            "<LangVersion>latest</LangVersion></PropertyGroup></Project>",
            encoding="utf-8",
        )
        (output / "NuGet.Config").write_text(
            "<configuration><packageSources><clear/></packageSources></configuration>", encoding="utf-8"
        )
        environment = minimal_test_environment(Path(args.dotnet), output)
        result = subprocess.run(
            [args.dotnet, "run", "--project", str(output / "Tests.csproj"), "-c", "Release"],
            cwd=output,
            env=environment,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            timeout=90,
        )
        log = result.stdout + result.stderr
        (output / "run.log").write_text(log, encoding="utf-8")
        rejected = result.returncode == 1 and f"FAIL {expected_case}:" in log and "error CS" not in log
        print(("PASS" if rejected else "FAIL") + f" mutation {name} expected={expected_case}")
        if not rejected:
            failed += 1
            print(log)

    print(f"SceneSpeechQueueOwner mutations={len(MUTATIONS)} FAIL={failed}")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
