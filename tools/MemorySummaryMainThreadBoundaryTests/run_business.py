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
]
MUTATIONS = ["worker-primary", "worker-extra", "worker-cleanup", "worker-release",
             "omit-release", "omit-cleanup", "omit-mark-daily", "omit-mark-major",
             "omit-mark-overview", "duplicate-apply", "accept-obsolete", "ignore-owner",
             "ignore-generation", "ignore-draft-owner"]


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
    declarations = [constant.group()]
    positions = []
    for signature in signatures:
        block = extractor.declaration(source, signature)
        begin = source.index(block)
        positions.append(dict(signature=signature, line=source[:begin].count("\n") + 1,
                              lines=block.count("\n") + 1, sha256=hashlib.sha256(block.encode()).hexdigest()))
        # Test-only entry probes do not replace any Apply/Mark operation or condition.
        if signature.startswith(("private void Apply", "private void Mark")):
            name = re.search(r"(\w+)\($", signature)[1]
            opening = block.index("{") + 1
            block = block[:opening] + f'\n Witness.Touch("{name}");' + block[opening:]
        if signature == "private DailyMemoryDraft FindMemoryDraft(" and mutation == "ignore-draft-owner":
            block = replace_exact(block,
                " && string.Equals(NormalizeMemoryHeroId(x.HeroId), text, StringComparison.OrdinalIgnoreCase)", "")
        if signature == METHODS[0]:
            block = replace_exact(block, "await Task.Delay(60000);", "await WaitOverviewWindowAsync();")
            block = replace_exact(block, "_memorySummaryProcessing = false;",
                                  'Witness.Touch("release"); _memorySummaryProcessing = false;')
            if mutation and mutation.startswith("worker-"):
                # Mutate the actual caller, not merely the scheduler helper.
                anchors = re.findall(r"await RunMemorySummaryMainThreadAsync\(runtimeGeneration, delegate", block)
                if len(anchors) != 4:
                    raise ValueError("Expected primary/extra/cleanup/release dispatches")
                which = ["worker-primary", "worker-extra", "worker-cleanup", "worker-release"].index(mutation)
                matches = list(re.finditer(re.escape(anchors[0]), block))
                hit = matches[which]
                block = block[:hit.start()] + "await Task.Run(delegate" + block[hit.end():]
            elif mutation == "omit-release":
                block = replace_exact(block, "_memorySummaryProcessing = false;", "/* fault: no processing release */")
            elif mutation == "omit-cleanup":
                for field in ["_memorySummaryQueue", "_npcMajorActionSummaryQueue", "_memoryOverviewQueue"]:
                    # Only the terminal cleanup, not startup filtering or the real Apply remove.
                    pattern = rf"{field} = Sanitize[^;]+\(\({field}[^;]+;"
                    block, count = re.subn(pattern, "/* fault: no terminal cleanup */", block)
                    if count != 1:
                        raise ValueError(f"Cleanup anchor drift: {field}: {count}")
            elif mutation and mutation.startswith("omit-mark-"):
                call = {"omit-mark-daily": "MarkMemorySummaryFailure(result.Job, result.Error);",
                        "omit-mark-major": "MarkMajorActionSummaryFailure(result2.Job, result2.Error);",
                        "omit-mark-overview": "MarkMemoryOverviewFailure(result3.Job, result3.Error);"}[mutation]
                block = replace_exact(block, call, "/* fault: omitted terminal failure */")
            elif mutation == "duplicate-apply":
                call = "ApplyMemorySummarySuccess(result.Job, result.Block);"
                block = replace_exact(block, call, call + "\n" + call)
            elif mutation == "accept-obsolete":
                for result in ["result", "result2", "result3", "result4"]:
                    block = replace_exact(block, f" || {result}.IsObsolete", "")
        declarations.append(block)
    boundary = extractor.source("MyBehavior.MemorySummaryMainThread.cs", None)
    if mutation == "ignore-owner":
        boundary = boundary.replace("ReferenceEquals(Instance, this)", "true")
        boundary = boundary.replace("ReferenceEquals(Campaign.Current?.GetCampaignBehavior<MyBehavior>(), this)", "true")
    elif mutation == "ignore-generation":
        boundary = boundary.replace("SaveRuntimeGuard.IsCurrentGeneration(generation)", "true")
    prefix = "using System; using System.Collections.Generic; using System.Linq; using System.Threading.Tasks;\nusing TaleWorlds.CampaignSystem; using TaleWorlds.CampaignSystem.Settlements; using TaleWorlds.Library;\nnamespace AnimusForge { public partial class MyBehavior {\n"
    manifest = dict(source_revision=BASELINE if original else subprocess.check_output(
        ["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
        production_file_sha256=hashlib.sha256(source.encode()).hexdigest(), declarations=positions,
        mutation=mutation, test_only_seams=["60s delay -> controlled asynchronous clock gate",
        "entry trace at six actual Apply/Mark methods", "trace before actual processing-release assignment"],
        limitations=["Not full MyBehavior/EngineTick", "provider executor and lower game/storage/weekly/UI boundaries are fixtures",
                     "No exact source fingerprint, provider retries/RPM, game/save or frame-time proof"])
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
    files = {"Business.cs": product, "Boundary.cs": boundary,
             "Program.cs": (HERE / "BusinessHarness.cs.txt").read_text(encoding="utf-8-sig"),
             "SaveRuntimeGuard.cs": (ROOT / "SaveRuntimeGuard.cs").read_text(encoding="utf-8-sig"),
             "Proof.csproj": '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn></PropertyGroup></Project>',
             "NuGet.Config": '<configuration><packageSources><clear/></packageSources></configuration>'}
    manifest["generated_sha256"] = {name: hashlib.sha256(data.encode()).hexdigest() for name, data in files.items()}
    for name, data in files.items():
        (out / name).write_bytes(data.encode("utf-8"))
    (out / "manifest.json").write_bytes(json.dumps(manifest, ensure_ascii=False, indent=2).encode("utf-8"))
    dotnet = Path(os.environ.get("DOTNET_EXE", r"C:\Program Files\dotnet\dotnet.exe"))
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
    raise SystemExit(main())
