"""Mutation-check extracted real source without changing production files."""
import argparse
from pathlib import Path
import subprocess
import sys
import run


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", default=r"G:\AFMOD\.dotnet-sdk\dotnet.exe")
    args = parser.parse_args()
    failed = 0
    for name, (_, _, expected_case) in run.MUTATIONS.items():
        result = subprocess.run(
            [sys.executable, "-B", str(run.HERE / "run.py"), "--dotnet", args.dotnet,
             "--mutation", name, "--output-name", "mutant-" + name],
            cwd=run.ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120)
        log = (run.HERE / ".generated" / ("mutant-" + name) / "run.log").read_text(encoding="utf-8")
        rejected = result.returncode == 1 and ("FAIL " + expected_case + ":") in log and "error CS" not in log
        print(("PASS" if rejected else "FAIL") + " mutation " + name + " expected=" + expected_case)
        if not rejected:
            failed += 1
            print(log)
    print(f"SceneRequestLifetime mutations={len(run.MUTATIONS)} FAIL={failed}")
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
