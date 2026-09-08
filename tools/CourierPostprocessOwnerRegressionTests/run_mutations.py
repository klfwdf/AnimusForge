"""Negative controls mutate generated copies only; production files are never changed."""
import subprocess
import sys
import run

failures = []
for name in run.MUTATIONS:
    result = subprocess.run([sys.executable, "-B", str(run.HERE / "run.py"), "--mutation", name,
                             "--output-name", "mutant-" + name], cwd=run.ROOT, capture_output=True,
                            text=True, encoding="utf-8", errors="replace", timeout=130)
    path = run.HERE / ".generated" / ("mutant-" + name) / "run.log"
    log = path.read_text(encoding="utf-8") if path.exists() else result.stdout + result.stderr
    rejected = result.returncode == 1 and ("FAIL " + run.EXPECTED_FAILURES[name] + ":") in log and "error CS" not in log
    print(("PASS" if rejected else "FAIL") + " mutation " + name)
    if not rejected:
        failures.append(name)
        print(log)
print(f"CourierPostprocessOwner mutations={len(run.MUTATIONS)} FAIL={len(failures)}")
raise SystemExit(1 if failures else 0)
