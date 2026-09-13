"""Run actual memory-completion business code; only external seams are fixtures.

Unlike run.py --original (a static detector), --original compiles and RUNS e40c92d7.
All generated sources, manifests and logs stay under ignored .generated/business/.
"""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import subprocess
import sys
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
BASELINE = "e40c92d7"
MODELS = ["DailyMemoryLine", "DailyMemoryDraft", "CompressedMemoryBlock",
          "WeeklyMemoryMaterialTrigger", "MemorySummaryJob", "MemorySummaryExecutionResult",
          "MemoryOverviewState", "MemoryOverviewJob", "MemoryOverviewExecutionResult",
          "MajorActionSummaryState", "MajorActionSummaryJob", "MajorActionSummaryExecutionResult"]
METHODS = [
    "private async Task ProcessMemorySummaryQueueAsync(",
    "private void ApplyMemorySummarySuccess(", "private void MarkMemorySummaryFailure(",
    "private void ApplyMajorActionSummarySuccess(", "private void MarkMajorActionSummaryFailure(",
    "private void ApplyMemoryOverviewSuccess(", "private void MarkMemoryOverviewFailure(",
    "private static List<MemorySummaryJob> SanitizeMemorySummaryQueue(",
    "private static List<MajorActionSummaryJob> SanitizeMajorActionSummaryQueue(",
    "private static List<MemoryOverviewJob> SanitizeMemoryOverviewQueue(",
    "private DailyMemoryDraft FindMemoryDraft(",
    "private bool HasMemorySummaryJobStillPending(",
    "private bool HasMajorActionSummaryJobStillPending(",
    "private bool HasMemoryOverviewJobStillPending(",
    "private bool HasMajorActionsNeedingSummary(", "private bool HasMemoryOverviewPendingBlocks(",
    "private static MajorActionSummaryState SanitizeMajorActionSummaryState(",
    "private static MemoryOverviewState SanitizeMemoryOverviewState(",
    "private MajorActionSummaryState GetMajorActionSummaryState(",
    "private MemoryOverviewState GetMemoryOverviewState(",
    "private static void GetMajorActionMaxCursor(",
    "private static bool IsMemoryBlockIncludedInOverview(",
    "private static int CountDailyMemorySummarySourceChars(",
    "private static string BuildCompressedMemoryBlockId(",
    "private static string NormalizeMemoryHeroId(", "private static bool IsNonHeroMemoryId(",
    "private void QueueDirtyMemoryOverviewCandidatesForDeferredScan(",
    "private void ShowCompressedMemoryBlockingPopup(",
    "private void TryStartMemorySummaryQueue(",
    "private bool ShouldScanMemoryOverviewCandidates(",
    "private void TryRunCampaignMemoryMaintenance(",
    "private void QueueAllMemoryOverviewCandidatesForDeferredScan(",
    "private static bool IsDailyMaintenanceBudgetExceeded(",
]
MUTATIONS = ["worker-primary", "worker-extra", "worker-cleanup", "worker-release",
             "omit-release", "omit-cleanup", "omit-mark-daily", "omit-mark-major",
             "omit-mark-overview", "duplicate-apply", "accept-obsolete", "ignore-owner",
             "ignore-generation", "ignore-draft-owner", "ignore-source", "miscount-obsolete",
             "worker-initial", "worker-major", "worker-extra-plan", "count-rejected-apply",
             "admission-rescan", "maintenance-rescan", "drop-forced-rescan", "dedupe-before-filter", "keep-invalid-queue", "swallow-completion-error", "bypass-completion-error-wrapper", "main-thread-failure-aggregation", "drop-failure-details"]


def replace_exact(text, old, new, count=1):
    if text.count(old) != count:
        raise ValueError(f"Extraction/mutation anchor drift: {old!r}, expected {count}")
    return text.replace(old, new)


def build_sources(original, mutation):
    spec = importlib.util.spec_from_file_location("channel_extractor", ROOT / "tools/ChannelCutoverBoundaryTests/run.py")
    extractor = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(extractor)
    source = extractor.source("MyBehavior.cs", BASELINE if original else None)
    signatures = [f"private sealed class {name}" for name in MODELS] + ["private class NpcActionEntry"] + METHODS
    constant = re.search(r'private const string NonHeroMemoryIdPrefix = [^;]+;', source)
    if constant is None:
        raise ValueError("Missing nonhero identity constant")
    throttle = re.search(r'private const double MemoryOverviewCandidateScanThrottleSeconds = [^;]+;', source)
    if throttle is None: raise ValueError("Missing overview throttle constant")
    declarations = [constant.group(), throttle.group()]
    positions = []
    for signature in signatures:
        # e40c92d7 Apply methods were void; candidate returns its actual acceptance receipt.
        if not original and signature.startswith("private void Apply"):
            signature = signature.replace("private void Apply", "private bool Apply", 1)
        block = extractor.declaration(source, signature)
        begin = source.index(block)
        positions.append(dict(file="MyBehavior.cs", signature=signature, line=source[:begin].count("\n") + 1,
                              lines=block.count("\n") + 1, sha256=hashlib.sha256(block.encode()).hexdigest()))
        # Test-only entry probes do not replace any Apply/Mark operation or condition.
        if signature.startswith(("private void Apply", "private bool Apply", "private void Mark")):
            name = re.search(r"(\w+)\($", signature)[1]
            opening = block.index("{") + 1
            block = block[:opening] + f'\n Witness.Touch("{name}");' + block[opening:]
        if signature == "private void MarkMemorySummaryFailure(":
            # Fault injection after the real queue retry mutation, not a replacement Mark method.
            block = replace_exact(block, "DailyMemoryDraft draft = FindMemoryDraft(job);",
                'if (throwAfterRetryMark) throw new InvalidOperationException("scripted failure after retry mark");\nDailyMemoryDraft draft = FindMemoryDraft(job);')
        if signature == "private DailyMemoryDraft FindMemoryDraft(" and mutation == "ignore-draft-owner":
            block = replace_exact(block,
                " && string.Equals(NormalizeMemoryHeroId(x.HeroId), text, StringComparison.OrdinalIgnoreCase)", "")
        if signature == "private void TryStartMemorySummaryQueue(":
            if mutation == "admission-rescan":
                block = replace_exact(block, "bool hasMemoryJobs = (_memorySummaryQueue?.Count ?? 0) > 0;",
                    "bool hasMemoryJobs = (_memorySummaryQueue?.Any(HasMemorySummaryJobStillPending) ?? false);")
            elif mutation == "drop-forced-rescan":
                block = replace_exact(block, "ProcessMemorySummaryQueueAsync(forceOverviewCandidateScan);", "ProcessMemorySummaryQueueAsync();")
        if signature == "private void TryRunCampaignMemoryMaintenance(" and mutation == "maintenance-rescan":
            block = replace_exact(block, "(_memorySummaryQueue?.Count ?? 0) > 0", "(_memorySummaryQueue?.Any(HasMemorySummaryJobStillPending) ?? false)")
        if signature == METHODS[0]:
            block = replace_exact(block, "await Task.Delay(60000);", "await WaitOverviewWindowAsync();")
            block = replace_exact(block, "_memorySummaryProcessing = false;",
                                  'Witness.Touch("release"); _memorySummaryProcessing = false;')
            if not original:
                aggregate = '"以下日结压缩任务重试 3 次后仍失败：\\n\\n" + string.Join("\\n", failures) + "\\n\\n请修复 API 或调低记忆总结 RPM 后重试。"'
                block = replace_exact(block, aggregate, "ObserveFailureAggregation(() => " + aggregate + ")")
                if mutation == "main-thread-failure-aggregation":
                    block = replace_exact(block, "failures.Count == 0 ? null : await Task.Run(() =>", "failures.Count == 0 ? null : await RunFailureAggregationOnMainForFaultAsync(() =>")
                elif mutation == "drop-failure-details":
                    block = replace_exact(block, 'string.Join("\\n", failures)', '""')
            if mutation and mutation.startswith("worker-") and mutation != "worker-extra-plan":
                # Mutate the actual caller, not merely the scheduler helper.
                marker = r"await RunMemorySummary(?:MainThread|Completion)Async\(runtimeGeneration, delegate"
                matches = list(re.finditer(marker, block))
                if len(matches) != 7:
                    raise ValueError("Expected initial/daily/major/overview/extra-plan/cleanup/release dispatches")
                which = {"worker-initial": 0, "worker-primary": 1, "worker-major": 2,
                         "worker-extra": 3, "worker-extra-plan": 4, "worker-cleanup": 5, "worker-release": 6}[mutation]
                hit = matches[which]
                block = block[:hit.start()] + "await Task.Run(delegate" + block[hit.end():]
            elif mutation == "bypass-completion-error-wrapper":
                block = replace_exact(block, "await RunMemorySummaryCompletionAsync(runtimeGeneration, delegate", "await RunMemorySummaryMainThreadAsync(runtimeGeneration, delegate", count=6)
            elif mutation == "omit-release":
                block = replace_exact(block, "_memorySummaryProcessing = false;", "/* fault: no processing release */")
            elif mutation == "omit-cleanup":
                block = replace_exact(block, "if (await BuildMemorySummaryPlanAsync(runtimeGeneration, cleanupOnly: true) == null) return;", "/* fault: no terminal cleanup */")
            elif mutation and mutation.startswith("omit-mark-"):
                call = {"omit-mark-daily": "MarkMemorySummaryFailure(result.Job, result.Error);",
                        "omit-mark-major": "MarkMajorActionSummaryFailure(result.Job, result.Error);",
                        "omit-mark-overview": "MarkMemoryOverviewFailure(result.Job, result.Error);"}[mutation]
                block = replace_exact(block, call, "/* fault: omitted terminal failure */")
            elif mutation == "duplicate-apply":
                call = "if (ApplyMemorySummarySuccess(result.Job, result.Block)) appliedDaily++;"
                block = replace_exact(block, call, "if (ApplyMemorySummarySuccess(result.Job, result.Block)) { ApplyMemorySummarySuccess(result.Job, result.Block); appliedDaily++; }")
            elif mutation == "count-rejected-apply":
                for name, payload, counter in [("ApplyMemorySummarySuccess", "Block", "appliedDaily"),
                        ("ApplyMajorActionSummarySuccess", "State", "appliedMajor"), ("ApplyMemoryOverviewSuccess", "State", "appliedOverview")]:
                    call = f"{name}(result.Job, result.{payload})"
                    block = replace_exact(block, f"if ({call}) {counter}++;", f"{call}; {counter}++;")
            elif mutation == "accept-obsolete":
                block = replace_exact(block, " || result.IsObsolete", "", count=3)
            elif mutation == "ignore-source":
                block = replace_exact(block, " || !IsMemorySummaryInputCurrent(result.Source)", "", count=3)
            elif mutation == "miscount-obsolete":
                # Count rejected successful payloads without applying them: the UI must catch this.
                guard = "if (result == null || result.Job == null || result.IsObsolete || !IsMemorySummaryInputCurrent(result.Source)) return true;"
                if block.count(guard) != 3:
                    raise ValueError("Expected three actual acceptance guards")
                for counter in ["appliedDaily", "appliedMajor", "appliedOverview"]:
                    block = block.replace(guard,
                        f"if (result != null && result.IsObsolete && result.Success) {{ {counter}++; return true; }}\n" +
                        "if (result == null || result.Job == null || !IsMemorySummaryInputCurrent(result.Source)) return true;", 1)
        declarations.append(block)
    if not original:
        helper_source = extractor.source("MyBehavior.MemorySummaryInput.cs", None)
        clone = extractor.declaration(helper_source, "private static T CloneMemorySummarySource<T>(")
        declarations.append(clone)
        positions.append(dict(file="MyBehavior.MemorySummaryInput.cs", signature="private static T CloneMemorySummarySource<T>(",
            line=helper_source[:helper_source.index(clone)].count("\n") + 1, lines=clone.count("\n") + 1,
            sha256=hashlib.sha256(clone.encode()).hexdigest()))
        digest = extractor.declaration(helper_source, "private static string ComputeMemorySummaryFingerprint(")
        declarations.append(digest)
        positions.append(dict(file="MyBehavior.MemorySummaryInput.cs", signature="private static string ComputeMemorySummaryFingerprint(",
            line=helper_source[:helper_source.index(digest)].count("\n")+1, lines=digest.count("\n")+1, sha256=hashlib.sha256(digest.encode()).hexdigest()))
        allowance = re.search(r"private const int DailyMaintenanceMaxJobsPerTick = [^;]+;", source)
        if allowance is None: raise ValueError("Missing planner allowance")
        declarations.append(allowance.group())
    boundary = extractor.source("MyBehavior.MemorySummaryMainThread.cs", None)
    if mutation == "swallow-completion-error":
        boundary = replace_exact(boundary, "if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();", "/* fault: swallowed partial execution error */")
    elif mutation == "ignore-owner":
        boundary = boundary.replace("ReferenceEquals(Instance, this)", "true")
        boundary = boundary.replace("ReferenceEquals(Campaign.Current?.GetCampaignBehavior<MyBehavior>(), this)", "true")
    elif mutation == "ignore-generation":
        boundary = boundary.replace("SaveRuntimeGuard.IsCurrentGeneration(generation)", "true")
    prefix = "using Newtonsoft.Json; using System; using System.IO; using System.Text; using System.Security.Cryptography; using System.Diagnostics; using System.Collections.Generic; using System.Linq; using System.Threading.Tasks;\nusing TaleWorlds.CampaignSystem; using TaleWorlds.CampaignSystem.Settlements; using TaleWorlds.Library;\nnamespace AnimusForge { public partial class MyBehavior {\n"
    manifest = dict(source_revision=BASELINE if original else subprocess.check_output(
        ["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
        production_file_sha256=hashlib.sha256(source.encode()).hexdigest(), declarations=positions,
        mutation=mutation, test_only_seams=["60s delay -> controlled asynchronous clock gate",
        "entry trace at six actual Apply/Mark methods", "optional test fault after actual MarkDaily queue retry update", "trace before actual processing-release assignment",
        "completion source predicate -> independently invalidatable fixture key (not real hash)",
        "real TryStart/TryRunMaintenance/ShouldScan/QueueAll; past-draft seal, busy/current-day and candidate-ID terminal are fixtures"],
        limitations=["Not full MyBehavior/EngineTick", "provider executor and lower game/storage/weekly/UI boundaries are fixtures",
                     "No exact source fingerprint, provider retries/RPM, game/save or record/time budget proof"],
        has_input_source=not original)
    return prefix + "\n\n".join(declarations) + "\n}}\n", boundary, manifest


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    group = parser.add_mutually_exclusive_group()
    group.add_argument("--original", action="store_true")
    group.add_argument("--mutate", choices=MUTATIONS)
    args = parser.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")
    product, boundary, manifest = build_sources(args.original, args.mutate)
    out = HERE / ".generated/business" / ("original" if args.original else args.mutate or "current")
    out.mkdir(parents=True, exist_ok=True)
    dependency = ROOT / ".tmp/nuget-packages/newtonsoft.json/13.0.3/lib/net6.0/Newtonsoft.Json.dll"
    if not dependency.is_file():
        raise ValueError("Existing Newtonsoft DLL missing; no dependency download attempted")
    files = {"Business.cs": product, "Boundary.cs": boundary,
             "Program.cs": ("#define HAS_INPUT_SOURCE\n" if manifest["has_input_source"] else "") +
                 (HERE / "BusinessHarness.cs.txt").read_text(encoding="utf-8-sig"),
             "SaveRuntimeGuard.cs": (ROOT / "SaveRuntimeGuard.cs").read_text(encoding="utf-8-sig"),
             "Proof.csproj": '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>' + escape(str(dependency)) + '</HintPath></Reference></ItemGroup></Project>',
             "NuGet.Config": '<configuration><packageSources><clear/></packageSources></configuration>'}
    if not args.original:
        planning = (ROOT / "MyBehavior.MemorySummaryPlanning.cs").read_text(encoding="utf-8-sig")
        manifest["planning_sha256"] = hashlib.sha256(planning.encode()).hexdigest()
        if args.mutate == "keep-invalid-queue":
            planning = replace_exact(planning, "source[index] = null;", "source[index] = job;")
        elif args.mutate == "dedupe-before-filter":
            planning = replace_exact(planning, "bool deferred = false, hasHoles = false;", "bool deferred = false, hasHoles = false; var prematureSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);")
            planning = replace_exact(planning, "bool pending = isPending(job);", "if (!prematureSeen.Add(DescribeMemorySummaryJob(job, index).Key)) { source[index] = null; hasHoles = true; continue; }\n bool pending = isPending(job);")
        elif args.mutate == "worker-extra-plan":
            # The extra plan no longer scans inside the coordinator callback. Corrupt
            # the actual shared scanner's dispatch, not the now-empty old callback.
            planning = replace_exact(planning, "await RunMemorySummaryCompletionAsync(generation, delegate", "await Task.Run(delegate", count=4)
        files["Planning.cs"] = planning
    manifest["generated_sha256"] = {name: hashlib.sha256(data.encode()).hexdigest() for name, data in files.items()}
    for name, data in files.items():
        (out / name).write_bytes(data.encode("utf-8"))
    (out / "manifest.json").write_bytes(json.dumps(manifest, ensure_ascii=False, indent=2).encode("utf-8"))
    dotnet = Path(os.environ.get("DOTNET_EXE", str(ROOT.parent / ".dotnet-sdk/dotnet.exe")))
    env = dict(os.environ, DOTNET_ROOT=str(dotnet.parent), DOTNET_CLI_HOME=str(ROOT / ".tmp/dotnet-cli"),
               NUGET_PACKAGES=str(ROOT / ".tmp/nuget-packages"), APPDATA=str(ROOT / ".tmp/appdata"),
               DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1",
               DOTNET_GENERATE_ASPNET_CERTIFICATE="false", DOTNET_CLI_UI_LANGUAGE="en")
    # Separate build from execution: compiler/extractor failure is NOT an expected red test.
    build = subprocess.run([str(dotnet), "build", str(out / "Proof.csproj"), "-c", "Release", "--nologo",
                            "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], cwd=ROOT, env=env,
                           capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120)
    (out / "build.log").write_bytes((build.stdout + build.stderr).encode())
    if build.returncode:
        print(build.stdout + build.stderr)
        return 2
    run = subprocess.run([str(dotnet), str(out / "bin/Release/net8.0/Proof.dll")], cwd=ROOT, env=env,
                         capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=90)
    log = run.stdout + run.stderr
    (out / "run.log").write_bytes(log.encode())
    print("BUILD_PASS business=" + (BASELINE if args.original else args.mutate or "current"))
    print(log, end="")
    if "BUSINESS_RESULT" not in log:
        return 2
    return run.returncode


if __name__ == "__main__":
    try:
        result = main()
    except Exception as exc:
        print("BUSINESS_TOOL_ERROR " + type(exc).__name__ + ": " + str(exc))
        result = 2
    raise SystemExit(result)
