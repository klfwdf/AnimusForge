"""Mutation-check extracted real source without changing production files."""
import argparse
from pathlib import Path
import subprocess
import sys
import os
import shutil
import run


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet")
    parser.add_argument("--run-root", type=Path)
    args = parser.parse_args()
    args.dotnet = str(run.resolve_dotnet(run.ROOT,args.dotnet))
    out = run.new_run_root(run.ROOT,"scene-request-lifetime-mutations",args.run_root)
    git = shutil.which("git")
    if not git or not Path(git).is_file():
        raise SystemExit("BLOCKED_ENV: Git executable required for current inverse and fixed UI oracle")
    environment = run.minimal_test_environment(Path(args.dotnet),out)
    # Only the verified Git executable directory is added, not the ambient PATH
    # or user credentials. Child Python still validates the current owner inverse.
    environment["PATH"] += os.pathsep + str(Path(git).resolve().parent)
    failed = 0
    for name, (_, _, expected_case) in run.MUTATIONS.items():
        result = subprocess.run(
            [sys.executable, "-B", str(run.HERE / "run.py"), "--dotnet", args.dotnet,
             "--mutation", name, "--output-name", "mutant-" + name, "--run-root", str(out / name)],
            cwd=run.ROOT, env=environment, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120)
        child_log = out / name / "run.log"
        if not child_log.is_file():
            raise RuntimeError("mutation did not reach compilation: " + name + "\n" + result.stdout + result.stderr)
        log = child_log.read_text(encoding="utf-8")
        rejected = result.returncode == 1 and ("FAIL " + expected_case + ":") in log and "error CS" not in log
        print(("PASS" if rejected else "FAIL") + " mutation " + name + " expected=" + expected_case)
        if not rejected:
            failed += 1
            print(log)
    print(f"SceneRequestLifetime mutations={len(run.MUTATIONS)} FAIL={failed}")
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
