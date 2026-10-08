import argparse
import sys as _legacy_sys
from pathlib import Path as _LegacyPath
_legacy_sys.path.insert(0, str(_LegacyPath(__file__).resolve().parents[4] / "tests"))
from af2_terminal_migration_review import historical_source
AF2_FIXTURE_METADATA = {"sourceClass": "legacy-oracle-extraction", "currentOwnerReplayProjected": False}
import os
import subprocess
from pathlib import Path
import sys as _relocation_sys
_relocation_sys.path.insert(0, str(Path(__file__).resolve().parents[4] / "tests"))
from output_isolation import current_source_path, new_run_root, resolve_dotnet, minimal_test_environment

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).parent
parser = argparse.ArgumentParser()
parser.add_argument("--original", action="store_true")
parser.add_argument("--run-root", type=Path)
parser.add_argument("--source-baseline", choices=["9617f96a"])
parser.add_argument("--mutate", choices=["ignore-generation", "ignore-owner", "unbounded-drain", "unbounded-inline", "ignore-time-budget", "omit-time-charge", "omit-time-reset"])
args = parser.parse_args()

boundary_path = current_source_path(ROOT, "MyBehavior.MemorySummaryMainThread.cs")
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

boundary = (subprocess.check_output(["git", "show", args.source_baseline + ":MyBehavior.MemorySummaryMainThread.cs"], cwd=ROOT).decode("utf-8-sig").replace("\r\n", "\n") if args.source_baseline else boundary_path.read_text(encoding="utf-8-sig"))
runtime = (ROOT / "src/modules/AF.Module.Memory/Summary/MemorySummaryDispatcher.cs").read_text(encoding="utf-8-sig") if "MemorySummaryDispatcher" in boundary else None
process = historical_source("MyBehavior.cs") # Legacy wiring assertions, not dispatcher Compile input.
required_process_fragments = [
    "await RunMemorySummaryRunPhaseAsync(run, runtimeGeneration",
    "ApplyMemorySummarySuccess(result.Job, result.Block)",
    "ApplyMajorActionSummarySuccess(result.Job, result.State)",
    "ApplyMemoryOverviewSuccess(result.Job, result.State)",
    "MarkMemorySummaryFailure(result.Job, result.Error)",
    "MarkMajorActionSummaryFailure(result.Job, result.Error)",
    "MarkMemoryOverviewFailure(result.Job, result.Error)",
]
for fragment in required_process_fragments:
    assert fragment in process, fragment
assert "ProcessMemorySummaryMainThreadActions();" in process
assert process.count("ResetMemorySummaryMainThreadActions();") >= 2

host_mutations = {
    "ignore-generation": ("&& SaveRuntimeGuard.IsCurrentGeneration(generation)", "&& true"),
    "ignore-owner": ("ReferenceEquals(Instance, _owner)", "true"),
}
runtime_mutations = {
    "unbounded-drain": ("while (HasAllowance()", "while (true"),
    "unbounded-inline": ("&& HasAllowance())", "&& true)"),
    "ignore-time-budget": ("< _host.GetBudgetMilliseconds();", "< double.MaxValue;"),
    "omit-time-charge": ("_elapsedTicks += Stopwatch.GetTimestamp() - started;", "/* fault: executed time not charged */"),
    "omit-time-reset": ("_elapsedTicks = 0;", "/* fault: elapsed time not reset on tick */"),
}
if args.mutate:
    assert not args.source_baseline, 'Baseline and mutation are exclusive'
    if args.mutate in host_mutations:
        old, new = host_mutations[args.mutate]; assert old in boundary; boundary = boundary.replace(old, new)
    else:
        old, new = runtime_mutations[args.mutate]; assert runtime is not None and old in runtime; runtime = runtime.replace(old, new)

out = new_run_root(ROOT, "memory-main-thread", args.run_root)
(out / "Boundary.cs").write_text(boundary, encoding="utf-8")
harness = ("#define DISPATCH_OWNER\n" if runtime is not None else "") + (HERE / "Harness.cs.txt").read_text(encoding="utf-8-sig")
if args.source_baseline:
    harness = harness.replace('MemorySummaryDispatch.PendingCount', '_memorySummaryMainThreadActions.Count')
(out / "Program.cs").write_text(harness, encoding="utf-8")
extra = ''
if runtime is not None:
    (out / 'MemorySummaryDispatcher.cs').write_text(runtime, encoding='utf-8')
    (out / 'IMemorySummaryDispatchHost.cs').write_text((ROOT / 'src/modules/AF.Module.Memory/Summary/IMemorySummaryDispatchHost.cs').read_text(encoding='utf-8-sig'), encoding='utf-8')
    extra = '<Compile Include="MemorySummaryDispatcher.cs"/><Compile Include="IMemorySummaryDispatchHost.cs"/>'

(out / "SaveRuntimeGuard.cs").write_text((ROOT / "src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs").read_text(encoding="utf-8-sig"), encoding="utf-8")
(out / "Proof.csproj").write_text(
    '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><EnableDefaultCompileItems>false</EnableDefaultCompileItems><OutputType>Exe</OutputType>'
    '<TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion>'
    '</PropertyGroup><ItemGroup><Compile Include="Boundary.cs"/><Compile Include="Program.cs"/><Compile Include="SaveRuntimeGuard.cs"/>' + extra + '</ItemGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding="utf-8")

dotnet = resolve_dotnet(ROOT)
env = minimal_test_environment(dotnet, out)
for key in ("DOTNET_CLI_HOME", "USERPROFILE", "APPDATA", "LOCALAPPDATA", "TEMP", "TMP"):
    Path(env[key]).mkdir(parents=True, exist_ok=True)
# Compilation failure never counts as a successful negative control.
build = subprocess.run([str(dotnet), "build", str(out / "Proof.csproj"), "-c", "Release", "--nologo",
                        "-p:RestoreConfigFile=" + str(out / "NuGet.Config")],
                       cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=180)
(out / "build.log").write_text(build.stdout + build.stderr, encoding="utf-8")
if build.returncode:
    print(build.stdout + build.stderr)
    raise SystemExit(2)
run = subprocess.run([str(dotnet), str(out / "bin/Release/net8.0/Proof.dll")],
                     cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=90)
log = run.stdout + run.stderr
(out / "run.log").write_text(log, encoding="utf-8")
print("BUILD_PASS helper=" + (args.mutate or ("source-baseline-" + args.source_baseline if args.source_baseline else "current")))
print(log)
raise SystemExit(run.returncode if "MemorySummaryMainThread checks=" in log else 2)
