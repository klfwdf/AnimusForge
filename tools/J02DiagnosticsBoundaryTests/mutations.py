"""Compile source-linked diagnostic mutants in a unique, isolated artifacts directory."""

from pathlib import Path
import json
import os
import subprocess
import uuid


ROOT = Path(__file__).resolve().parents[2]
SOURCES = ROOT / "src/AF.Foundation.Runtime/Diagnostics"
OUTPUT = ROOT / "artifacts/tests/j02-diagnostics-a/mutations" / uuid.uuid4().hex
OUTPUT.mkdir(parents=True, exist_ok=False)
DOTNET = ROOT / "local/dotnet/8.0.425/dotnet.exe"
NAMES = ("PerformanceWindow", "DiagnosticTraceContext", "MetricWindow",
         "BoundedLogWriteQueue", "FreezeWatchState")
MUTATIONS = (
    ("perf_frame_count", "PerformanceWindow", "_frameCount++;", "_frameCount += 0;", "snapshot counters"),
    ("trace_restore", "DiagnosticTraceContext", "_current.Value = previous;", "_current.Value = null;", "nested restore"),
    ("metric_clear", "MetricWindow", "_metrics.Clear();", "/* no clear */", "metric window clears after drain"),
    ("queue_hard_cap", "BoundedLogWriteQueue", "HardMaxLogWriteQueueItems = 8192", "HardMaxLogWriteQueueItems = 8193", "normal and verbose backpressure"),
    ("queue_worker_gate", "BoundedLogWriteQueue", "_flushBatches(batches);\n\t\t\t\tInterlocked.Exchange(ref _running, 0);",
     "_flushBatches(batches);\n\t\t\t\tInterlocked.Exchange(ref _running, 1);", "queue flush"),
    ("freeze_scope_restore", "FreezeWatchState", "if (state.Parent != null && state.Parent.MainThreadScope)",
     "if (false && state.Parent != null && state.Parent.MainThreadScope)", "inner scope restores parent"),
)


def run_case(label, mutation=None):
    case = OUTPUT / label
    case.mkdir()
    (case / "Program.cs").write_bytes((ROOT / "tools/J02DiagnosticsBoundaryTests/Program.cs").read_bytes())
    for name in NAMES:
        source = (SOURCES / f"{name}.cs").read_text(encoding="utf-8-sig")
        if mutation and mutation[1] == name:
            _, _, before, after, _ = mutation
            if source.count(before) != 1:
                raise AssertionError(f"mutation anchor not unique: {label}")
            source = source.replace(before, after)
        (case / f"{name}.cs").write_text(source, encoding="utf-8")
    includes = "\n".join(f'    <Compile Include="{name}.cs" />' for name in ("Program",) + NAMES)
    (case / "Mutant.csproj").write_text(
        '<Project Sdk="Microsoft.NET.Sdk">\n'
        '  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>'
        '<EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup>\n'
        f'  <ItemGroup>\n{includes}\n  </ItemGroup>\n</Project>\n', encoding="utf-8")
    env = os.environ.copy()
    env.update(DOTNET_CLI_HOME=str(ROOT / "artifacts/tests/j02-diagnostics-a/home"),
               DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1")
    build = subprocess.run([str(DOTNET), "build", str(case / "Mutant.csproj"), "--verbosity:quiet"],
                           cwd=ROOT, env=env, capture_output=True, text=True)
    (case / "build.stdout.log").write_text(build.stdout, encoding="utf-8")
    (case / "build.stderr.log").write_text(build.stderr, encoding="utf-8")
    if build.returncode:
        raise AssertionError(f"mutant failed to compile rather than fail behavior: {label}: {build.stderr[-400:]}")
    result = subprocess.run([str(DOTNET), str(case / "bin/Debug/net8.0/Mutant.dll")],
                            cwd=ROOT, env=env, capture_output=True, text=True, timeout=30)
    (case / "run.stdout.log").write_text(result.stdout, encoding="utf-8")
    (case / "run.stderr.log").write_text(result.stderr, encoding="utf-8")
    return {"case": label, "build_exit": build.returncode, "run_exit": result.returncode,
            "expected_failure": mutation[4] if mutation else None,
            "observed_expected_failure": mutation[4] in result.stderr if mutation else None}


results = [run_case("control")]
assert results[0]["run_exit"] == 0, "unmutated source-linked control failed"
for mutation in MUTATIONS:
    label = mutation[0]
    result = run_case(label, mutation)
    results.append(result)
    assert result["run_exit"] != 0 and result["observed_expected_failure"], \
        f"behavior test did not detect its specified failure: {label}"
    print(f"KILLED {label}")
(OUTPUT / "results.json").write_text(json.dumps(results, indent=2) + "\n", encoding="utf-8")
print("PASS six compiled behavior mutations; unmutated control passes")
print("receipt=" + str(OUTPUT / "results.json"))
