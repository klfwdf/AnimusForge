import argparse
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).parent
parser = argparse.ArgumentParser()
parser.add_argument("--original", action="store_true")
parser.add_argument("--mutate", choices=["ignore-generation", "ignore-owner", "unbounded-drain"])
args = parser.parse_args()

boundary_path = ROOT / "MyBehavior.MemorySummaryMainThread.cs"
if args.original:
    original = subprocess.check_output(
        ["git", "show", "e40c92d7:MyBehavior.cs"], cwd=ROOT,
        text=True, encoding="utf-8-sig", errors="strict")
    start = original.index("\tprivate async Task ProcessMemorySummaryQueueAsync()")
    end = original.index("\n\tprivate async Task RunDailySummaryQueueItemsAsync", start)
    method = original[start:end]
    assert "await RunDailySummaryQueueItemsAsync" in method
    assert "RunMemorySummaryMainThreadAsync" not in method
    assert "ApplyMemorySummarySuccess(result.Job, result.Block)" in method
    print("FAIL MemorySummaryMainThread baseline=e40c92d7 direct_post_await_writer=true")
    raise SystemExit(1)
if not boundary_path.exists():
    print("FAIL MemorySummaryMainThread missing production boundary")
    raise SystemExit(1)

boundary = boundary_path.read_text(encoding="utf-8-sig")
process = (ROOT / "MyBehavior.cs").read_text(encoding="utf-8-sig")
required_process_fragments = [
    "await RunMemorySummaryMainThreadAsync(runtimeGeneration",
    "ApplyMemorySummarySuccess(result.Job, result.Block)",
    "ApplyMajorActionSummarySuccess(result2.Job, result2.State)",
    "ApplyMemoryOverviewSuccess(result3.Job, result3.State)",
    "MarkMemorySummaryFailure(result.Job, result.Error)",
    "MarkMajorActionSummaryFailure(result2.Job, result2.Error)",
    "MarkMemoryOverviewFailure(result3.Job, result3.Error)",
]
for fragment in required_process_fragments:
    assert fragment in process, fragment
assert "ProcessMemorySummaryMainThreadActions();" in process
assert process.count("ResetMemorySummaryMainThreadActions();") >= 2

mutations = {
    "ignore-generation": ("|| !SaveRuntimeGuard.IsCurrentGeneration(generation)", "|| false"),
    "ignore-owner": ("ReferenceEquals(Instance, this)", "true"),
    "unbounded-drain": ("processed < MemorySummaryMainThreadActionsPerTick", "processed < int.MaxValue"),
}
if args.mutate:
    old, new = mutations[args.mutate]
    assert old in boundary
    boundary = boundary.replace(old, new)

out = HERE / ".generated" / (args.mutate or "current")
out.mkdir(parents=True, exist_ok=True)
(out / "Boundary.cs").write_text(boundary, encoding="utf-8")
(out / "Program.cs").write_text((HERE / "Harness.cs.txt").read_text(encoding="utf-8-sig"), encoding="utf-8")
(out / "SaveRuntimeGuard.cs").write_text((ROOT / "SaveRuntimeGuard.cs").read_text(encoding="utf-8-sig"), encoding="utf-8")
(out / "Proof.csproj").write_text(
    '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
    '<TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion>'
    '</PropertyGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding="utf-8")

dotnet = Path(os.environ.get("DOTNET_EXE", r"C:\Program Files\dotnet\dotnet.exe"))
env = os.environ.copy()
env.update(DOTNET_ROOT=str(dotnet.parent), DOTNET_CLI_HOME=str(ROOT / ".tmp/dotnet-cli"),
           NUGET_PACKAGES=str(ROOT / ".tmp/nuget-packages"), DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1",
           DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_CLI_UI_LANGUAGE="en",
           APPDATA=str(ROOT / ".tmp/appdata"))
run = subprocess.run([str(dotnet), "run", "--project", str(out / "Proof.csproj"), "-c", "Release",
                      "-p:RestoreConfigFile=" + str(out / "NuGet.Config")],
                     cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=180)
log = run.stdout + run.stderr
(out / "run.log").write_text(log, encoding="utf-8")
print(log)
raise SystemExit(run.returncode)
