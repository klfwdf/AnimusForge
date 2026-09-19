"""Build and run the Memory Records ledger contract against the production owner files.

SDK resolution: AF_DOTNET env var, then repository local/dotnet/8.0.425, then dotnet on PATH.
Exit code is the harness exit code; mutation switches prove the assertions are live.
"""
from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent

parser = argparse.ArgumentParser()
parser.add_argument("--mutate", choices=["window-off-by-one", "expiry-keeps-old"], help="apply a source mutation that must fail")
args = parser.parse_args()


def resolve_dotnet() -> Path:
    candidates = [os.environ.get("AF_DOTNET", ""), str(ROOT / "local/dotnet/8.0.425/dotnet.exe"), shutil.which("dotnet") or ""]
    for candidate in candidates:
        if candidate and Path(candidate).exists():
            return Path(candidate)
    raise SystemExit("NOT-RUN: no dotnet SDK found (set AF_DOTNET)")


dotnet = resolve_dotnet()
output = ROOT / "artifacts/tests/memory-j05-records" / (args.mutate or "current")
if output.exists():
    shutil.rmtree(output)
output.mkdir(parents=True)
for name in ("Program.cs", "MemoryRecordsTests.csproj"):
    shutil.copy(HERE / name, output / name)
project = (output / "MemoryRecordsTests.csproj").read_text(encoding="utf-8").replace("../../../../", (str(ROOT) + "/").replace("\\", "/"))
if args.mutate:
    src_dir = output / "mutated"
    src_dir.mkdir()
    if args.mutate == "window-off-by-one":
        text = (ROOT / "src/modules/AF.Module.Memory/Records/NpcActionLedger.cs").read_text(encoding="utf-8")
        needle = "currentDay - RecentWindowDays + 1"
        assert text.count(needle) == 1
        (src_dir / "NpcActionLedger.cs").write_text(text.replace(needle, "currentDay - RecentWindowDays"), encoding="utf-8")
        project = project.replace(str(ROOT).replace("\\", "/") + "/src/modules/AF.Module.Memory/Records/NpcActionLedger.cs", str(src_dir / "NpcActionLedger.cs").replace("\\", "/"))
    elif args.mutate == "expiry-keeps-old":
        text = (ROOT / "src/modules/AF.Module.Memory/Records/DialogueHistoryLedger.cs").read_text(encoding="utf-8")
        needle = "if (seenConversation && isSingleUseFact(item.Line))"
        assert text.count(needle) == 1
        (src_dir / "DialogueHistoryLedger.cs").write_text(text.replace(needle, "if (false)"), encoding="utf-8")
        project = project.replace(str(ROOT).replace("\\", "/") + "/src/modules/AF.Module.Memory/Records/DialogueHistoryLedger.cs", str(src_dir / "DialogueHistoryLedger.cs").replace("\\", "/"))
(output / "MemoryRecordsTests.csproj").write_text(project, encoding="utf-8")
(output / "NuGet.Config").write_text("<configuration><packageSources><clear /></packageSources></configuration>", encoding="utf-8")
env = dict(os.environ, DOTNET_ROOT=str(dotnet.parent), DOTNET_CLI_HOME=str(ROOT / ".tmp/dotnet-cli"), DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_NOLOGO="1", DOTNET_MULTILEVEL_LOOKUP="0")
build = subprocess.run([str(dotnet), "build", str(output / "MemoryRecordsTests.csproj"), "-c", "Release", "--nologo", "-p:RestoreConfigFile=" + str(output / "NuGet.Config")], cwd=output, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
(output / "build.log").write_text(build.stdout + build.stderr, encoding="utf-8")
if build.returncode != 0:
    print(build.stdout[-3000:])
    raise SystemExit("build failed: " + str(build.returncode))
run = subprocess.run([str(dotnet), str(output / "bin/Release/net8.0/MemoryRecordsTests.dll")], cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
(output / "run.log").write_text(run.stdout + run.stderr, encoding="utf-8")
print((run.stdout + run.stderr).strip()[-2000:])
sys.exit(run.returncode)
