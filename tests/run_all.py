"""J16e: single entry for every test runner in tests/, tools/ and extensions/*/tests.

Discovery (same rules as the J16 G0 baseline): tracked *.py whose name starts with
run/validate_/verify/source_/test_ or that contains __main__, and tracked *.csproj under
tests/ (or named *Tests/*SmokeTests). Per-entry extra arguments and expected non-PASS
states come from tests/runners.json; every entry runs in its own process with its own
exit code, except explicit manual business tools, which remain NEEDS_INPUT without launch.
Failures do not short-circuit; the total exit code is non-zero if any entry
ends FAIL (PASS where runners.json expects PASS, i.e. the default).

Toolchain (override by environment; missing tools make the affected entries BLOCKED_ENV):
  AF_DOTNET8   dotnet 8 SDK host        default <repo>/local/dotnet/8.0.425/dotnet.exe
  AF_DOTNET10  dotnet 10 SDK host       default G:/AFMOD/.dotnet-sdk10/dotnet.exe
  AF_PWSH      PowerShell 7             default G:/AFMOD/.pwsh7/pwsh.exe
  AF_BANNERLORD_ROOT / AF_WORKSHOP_DIR  game + workshop roots for replay/policy entries
  AF_REPLAY_14_REFS                     1.4 reference dir (default <repo>/.tmp/build_check/1.4)
  AF_REPLAY_CANDIDATE_DLL               isolated 1.4 implementation DLL for same-candidate replay
  AF_REPLAY_DEBUG_CANDIDATE_DLL         explicitly selected Debug-only seam replay candidate
  AF_REPLAY_STAGE_BIN                   isolated/private dependency bin for replay
  AF_TEST_TEMP_ROOT                     explicitly approved TEMP parent, outside the repo (required)

Usage: py -3 tests/run_all.py [--only PREFIX] [--ids FILE] [--jobs N] [--out DIR] [--list]
Writes logs and results.json under artifacts/tests/run_all/<run>/ (ignored). Read-only
for tracked files; each runner keeps its own documented output root.
"""
from __future__ import annotations

import argparse
import datetime
import hashlib
import json
import os
import re
import stat
import subprocess
import sys
import time
import uuid
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "tests" / "runners.json"
ENTRY_NAME = re.compile(r"^(run|validate_|verify|source_|test_)[^/]*\.py$")
STATES = {"PASS", "PREEXISTING_FAIL", "NEEDS_INPUT", "SUPERSEDED_BY_RUNNER", "ENV_STATE"}
EXECUTIONS = {"auto", "manual", "isolated-player-exports"}


def checked_path(path: Path) -> Path:
    """Reject redirected ancestors before creating any output (including junctions)."""
    if not path.is_absolute() or str(path).startswith(("\\\\", "//")) or ".." in path.parts:
        raise ValueError("output paths must be absolute local paths without parent traversal")
    for ancestor in (path, *path.parents):
        try:
            info = ancestor.lstat()
        except FileNotFoundError:
            continue
        if stat.S_ISLNK(info.st_mode) or getattr(info, "st_file_attributes", 0) & stat.FILE_ATTRIBUTE_REPARSE_POINT:
            raise ValueError("output paths must not contain links or reparse points")
    return path.resolve()


def env_path(name: str, default: Path) -> Path:
    value = os.environ.get(name)
    return Path(value) if value else default


DOTNET8 = env_path("AF_DOTNET8", ROOT / "local" / "dotnet" / "8.0.425" / "dotnet.exe")
DOTNET10 = env_path("AF_DOTNET10", Path(r"G:\AFMOD\.dotnet-sdk10\dotnet.exe"))
PWSH = env_path("AF_PWSH", Path(r"G:\AFMOD\.pwsh7\pwsh.exe"))
GAME = env_path("AF_BANNERLORD_ROOT", Path(r"E:\Steam\steamapps\common\Mount & Blade II Bannerlord"))
WORKSHOP = env_path("AF_WORKSHOP_DIR", Path(r"E:\Steam\steamapps\workshop\content\261550"))
REFS14 = env_path("AF_REPLAY_14_REFS", ROOT / ".tmp" / "build_check" / "1.4")
SDK8_DIR = DOTNET8.parent
NEWTONSOFT = SDK8_DIR / "sdk" / "8.0.425" / "Newtonsoft.Json.dll"
STAGE_BIN = env_path("AF_REPLAY_STAGE_BIN", ROOT / "bin" / "Debug" / "single_module_stage" / "AnimusForge" / "bin" / "Win64_Shipping_Client")
DLL14 = env_path("AF_REPLAY_CANDIDATE_DLL", ROOT / "bin" / "Debug" / "single_module_artifacts" / "versions" / "1.4" / "AnimusForge.dll")

DEBUG_DLL14 = env_path("AF_REPLAY_DEBUG_CANDIDATE_DLL", ROOT / "bin" / "Debug" / "single_module_artifacts" / "versions" / "1.4" / "AnimusForge.dll")


def candidate_dll(spec: dict) -> Path:
    return DEBUG_DLL14 if spec.get("candidateConfiguration") == "Debug" else DLL14


def replay_module(name: str, installed_name: str, workshop_id: str) -> Path:
    installed = GAME / "Modules" / installed_name
    return env_path(name, installed if installed.is_dir() else WORKSHOP / workshop_id)


HARMONY_MODULE = replay_module("AF_REPLAY_HARMONY_MODULE_PATH", "Bannerlord.Harmony", "2859188632")
MCM_MODULE = replay_module("AF_REPLAY_MCM_MODULE_PATH", "Bannerlord.MBOptionScreen", "2859238197")
UIEXTENDER_MODULE = replay_module("AF_REPLAY_UIEXTENDER_MODULE_PATH", "Bannerlord.UIExtenderEx", "2859222409")

TOKENS = {
    "{DOTNET8}": str(DOTNET8), "{NEWTONSOFT}": str(NEWTONSOFT), "{GAME}": str(GAME),
    "{WORKSHOP}": str(WORKSHOP), "{REFS14}": str(REFS14), "{STAGE_BIN}": str(STAGE_BIN),
    "{DLL14}": str(DLL14), "{ROOT}": str(ROOT),
    "{HARMONY_MODULE}": str(HARMONY_MODULE), "{MCM_MODULE}": str(MCM_MODULE),
    "{UIEXTENDER_MODULE}": str(UIEXTENDER_MODULE),
    "{CANDIDATE_ARTIFACT_ROOT}": str(DLL14.parents[2]),
}


def expand(values, run_name: str) -> list[str]:
    out = []
    for value in values:
        for token, replacement in {**TOKENS, "{RUN}": run_name}.items():
            value = value.replace(token, replacement)
        out.append(value)
    return out


def tracked(prefix: str) -> list[str]:
    raw = subprocess.run(["git", "ls-files", "-z", "--", prefix], cwd=ROOT, capture_output=True, check=True).stdout
    return [p for p in raw.decode("utf-8").split("\0") if p]


def discover() -> list[str]:
    found = []
    paths = tracked("tools") + tracked("tests") + [p for p in tracked("extensions") if "/tests/" in p]
    for path in paths:
        p = Path(path)
        if any(part in {"src", "dist", "__pycache__", "_shared"} for part in p.parts) and p.parts[0] != "extensions":
            continue
        if p.parts[0] == "extensions" and any(part in {"bin", "obj"} for part in p.parts):
            continue
        if p.suffix == ".py":
            if path == "tests/run_all.py":
                continue
            text = (ROOT / p).read_text(encoding="utf-8", errors="replace")
            if ENTRY_NAME.match(p.name) or "__main__" in text:
                found.append(path)
        elif p.suffix == ".csproj" and (re.search(r"(Tests?|SmokeTests)$", p.stem) or p.parts[0] == "tests"):
            found.append(path)
    return sorted(set(found))


def command(path: str, spec: dict, run_name: str, build_root: Path | None = None) -> list[str]:
    p = ROOT / path
    extra = expand(spec.get("args", []), run_name)
    candidate = candidate_dll(spec)
    if p.suffix == ".py":
        text = p.read_text(encoding="utf-8", errors="replace")
        if p.name.startswith("test_") and "__main__" not in text and "TestCase" in text:
            return [sys.executable, "-X", "utf8", "-B", "-m", "unittest", "discover",
                    "-s", str(p.parent), "-p", p.name, "-t", str(p.parent)] + extra
        return [sys.executable, "-X", "utf8", "-B", str(p)] + extra
    text = p.read_text(encoding="utf-8", errors="replace")
    sdk = DOTNET10 if "net10.0" in text else DOTNET8
    cmd = [str(sdk), "run", "--project", str(p), "-c", "Release"]
    if "DebtPythonExe" in text:
        cmd += ["-p:DebtPythonExe=" + sys.executable]
    if build_root is not None:
        # SDK-native per-project bin/obj layout also isolates ProjectReference builds.
        cmd += ["-p:UseArtifactsOutput=true", "-p:UseAppHost=false", f"-p:ArtifactsPath={build_root}",
                f"-p:ReplayOutputRoot={build_root}", f"-p:OutDir={build_root / 'bin' / 'runtime'}/"]
    if "BannerlordReplayDependencies.targets" in text:
        cmd += [f"-p:GameRoot={GAME}", f"-p:Bannerlord14ReferencePath={REFS14}",
                f"-p:ReplayHarmonyModulePath={HARMONY_MODULE}",
                f"-p:ReplayMcmModulePath={MCM_MODULE}",
                f"-p:ReplayUiExtenderModulePath={UIEXTENDER_MODULE}",
                f"-p:ReplayPrivateRuntimePath={candidate.parent if spec.get('candidateConfiguration') == 'Debug' else STAGE_BIN}"]
    cmd += expand(spec.get("msbuild", []), run_name)
    if spec.get("candidateDll") and candidate.exists():
        cmd += [f"-p:ReplayCandidateDll={candidate}"]
        cmd += ["--", str(candidate), hashlib.sha256(candidate.read_bytes()).hexdigest().upper()]
    elif extra:
        cmd += extra if extra[0] == "--" else ["--", *extra]
    return cmd


def blocked(cmd: list[str]) -> str | None:
    exe = Path(cmd[0])
    if exe.is_absolute() and not exe.exists():
        return f"missing toolchain {exe}"
    return None


def execute(cmd: list[str], env: dict, build_root: Path, timeout: int):
    """SDK8 run can launch old bin/ output despite isolated build properties."""
    options = dict(cwd=ROOT, env=env, capture_output=True, text=True,
                   encoding="utf-8", errors="replace", timeout=timeout)
    if len(cmd) < 3 or cmd[1] != "run":
        return subprocess.run(cmd, **options)
    separator = cmd.index("--") if "--" in cmd else len(cmd)
    runtime_args = cmd[separator + 1:]
    build = [cmd[0], "build", *[arg for arg in cmd[2:separator] if arg != "--project"]]
    built = subprocess.run(build, **options)
    if built.returncode:
        return built
    project = cmd[cmd.index("--project") + 1]
    configuration = cmd[cmd.index("-c") + 1]
    properties = [arg for arg in cmd[2:separator] if arg.startswith("-p:")]
    query = [cmd[0], "msbuild", project, "-nologo", f"-p:Configuration={configuration}",
             *properties, "-getProperty:TargetPath,TargetFramework"]
    evaluated = subprocess.run(query, **options)
    if evaluated.returncode:
        return evaluated
    metadata = json.loads(evaluated.stdout)["Properties"]
    target = checked_path(Path(metadata["TargetPath"]))
    if not target.is_relative_to(build_root.resolve()) or not target.is_file():
        raise ValueError("evaluated test target escaped isolated build output or is missing")
    # .NET Framework fixtures run directly; managed .NET fixtures use their selected SDK.
    launch = [str(target)] if metadata["TargetFramework"].startswith("net4") else [cmd[0], str(target)]
    done = subprocess.run([*launch, *runtime_args], **options)
    done.stdout = built.stdout + evaluated.stdout + "\n--- isolated target ---\n" + str(target) + "\n" + done.stdout
    done.stderr = built.stderr + evaluated.stderr + done.stderr
    return done


def entry_log_path(out: Path, entry: str) -> Path:
    name = re.sub(r"[^A-Za-z0-9._-]", "_", entry)
    if len(str(out / (name + ".log"))) >= 240:
        name = name[:80] + "-" + hashlib.sha256(entry.encode("utf-8")).hexdigest()[:12]
    return out / (name + ".log")


def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    ap.add_argument("--only")
    ap.add_argument("--ids")
    ap.add_argument("--jobs", type=int, default=4)
    ap.add_argument("--out")
    ap.add_argument("--list", action="store_true")
    args = ap.parse_args(argv)

    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    specs: dict = manifest["entries"]
    bad_state = {k: v.get("expect") for k, v in specs.items() if v.get("expect", "PASS") not in STATES}
    if bad_state:
        print("runners.json: unknown expect state", bad_state)
        return 2
    invalid_execution = [k for k, v in specs.items()
                         if v.get("execution", "auto") not in EXECUTIONS
                         or (v.get("execution") == "manual" and v.get("expect") != "NEEDS_INPUT")
                         or (v.get("execution") == "isolated-player-exports" and
                             (v.get("args") or v.get("candidateDll") or not k.endswith(".csproj")))]
    if invalid_execution:
        print("runners.json: invalid execution mode", invalid_execution)
        return 2
    invalid_candidates = [k for k, v in specs.items()
                          if v.get("candidateConfiguration") not in (None, "Debug")
                          or (v.get("candidateConfiguration") and not v.get("candidateDll"))]
    if invalid_candidates:
        print("runners.json: invalid candidate configuration", invalid_candidates)
        return 2
    entries = discover()
    stale = sorted(set(specs) - set(entries))
    if stale:
        print("runners.json lists entries that no longer exist:", stale)
        return 2
    if args.ids:
        wanted = set(Path(args.ids).read_text(encoding="utf-8").split())
        entries = [e for e in entries if e in wanted]
    if args.only:
        entries = [e for e in entries if e.startswith(args.only)]
    if args.list:
        for e in entries:
            print(specs.get(e, {}).get("expect", "PASS"), e)
        return 0

    run_name = datetime.datetime.now().strftime("%Y%m%d-%H%M%S") + "-" + uuid.uuid4().hex[:12]
    out = Path(args.out) if args.out else ROOT / "artifacts" / "tests" / "run_all" / run_name
    # TEMP must live outside the repository: data-root guards (AnimusForgeDataPaths) correctly
    # reject any root below a directory containing AnimusForge.csproj.
    try:
        out = checked_path(out)
        if not out.is_relative_to(ROOT.resolve()) or out.exists():
            raise ValueError("--out must be a new directory inside the workspace")
        approved = os.environ.get("AF_TEST_TEMP_ROOT")
        if not approved:
            raise ValueError("AF_TEST_TEMP_ROOT must name an explicitly approved synthetic TEMP parent")
        parent = checked_path(Path(approved))
        if parent == Path(parent.anchor) or parent.is_relative_to(ROOT.resolve()) or ROOT.resolve().is_relative_to(parent):
            raise ValueError("AF_TEST_TEMP_ROOT must be outside, and not contain, the workspace")
        # Keep deeply nested synthetic fixtures below Windows MAX_PATH without changing data-root guards.
        temp_name = "af2-" + hashlib.sha256(str(ROOT.resolve()).encode("utf-8")).hexdigest()[:8] + "-" + uuid.uuid4().hex[:8]
        tmp = checked_path(parent / temp_name)
        if tmp.exists():
            raise ValueError("synthetic TEMP run directory already exists")
    except (ValueError, OSError) as ex:
        print("unsafe output configuration:", ex)
        return 2
    out.mkdir(parents=True, exist_ok=False)
    tmp.mkdir(parents=True, exist_ok=False)
    env = os.environ.copy()
    env.update({
        "TMP": str(tmp), "TEMP": str(tmp), "PYTHONUTF8": "1",
        "AF_DOTNET": str(DOTNET8), "DOTNET_EXE": str(DOTNET8), "AF_J15_DOTNET8": str(DOTNET8),
        "AF_J15_PWSH": str(PWSH), "AF_NEWTONSOFT": str(NEWTONSOFT), "NEWTONSOFT_JSON_PATH": str(NEWTONSOFT),
        "DOTNET_CLI_HOME": str(out / "dotnet-home"), "DOTNET_NOLOGO": "1", "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
        "NUGET_PACKAGES": str(out / "nuget-packages"), "NUGET_HTTP_CACHE_PATH": str(out / "nuget-http-cache"),
        "NUGET_PLUGINS_CACHE_PATH": str(out / "nuget-plugin-cache"),
        "AF_REPLAY_REPO_ROOT": str(ROOT),
        "DefaultItemExcludes": "**/bin/**;**/obj/**;**/artifacts/**;**/local/**",
        "PATH": str(SDK8_DIR) + os.pathsep + os.environ.get("PATH", ""),
    })

    def run(entry: str) -> dict:
        spec = specs.get(entry, {})
        expect = spec.get("expect", "PASS")
        if spec.get("execution") == "manual":
            message = "Not launched: business tool requires explicit inputs and separate authorization."
            entry_log_path(out, entry).write_text(message, encoding="utf-8")
            return {"id": entry, "exit": None, "status": "NEEDS_INPUT", "expect": expect,
                    "seconds": 0, "last": message}
        entry_root = tmp / hashlib.sha256(entry.encode("utf-8")).hexdigest()[:20]
        entry_temp = entry_root / "temp"
        entry_temp.mkdir(parents=True, exist_ok=False)
        entry_env = {**env, "TEMP": str(entry_temp), "TMP": str(entry_temp)}
        build_root = out / "entry-builds" / hashlib.sha256(entry.encode("utf-8")).hexdigest()[:20]
        cmd = command(entry, spec, run_name, build_root)
        if spec.get("execution") == "isolated-player-exports":
            entry_env["ANIMUSFORGE_DATA_ROOT"] = str(entry_root / "data")
            cmd += ["--", "--isolated-full", str(entry_root)]
        reason = blocked(cmd)
        candidate = candidate_dll(spec)
        if spec.get("candidateDll") and not candidate.is_file():
            reason = f"missing {spec.get('candidateConfiguration', 'default')} replay candidate {candidate}"
        start = time.time()
        if reason:
            code, text = None, reason
        else:
            try:
                done = execute(cmd, entry_env, build_root, spec.get("timeout", 1200))
                code, text = done.returncode, done.stdout + "\n--- stderr ---\n" + done.stderr
            except subprocess.TimeoutExpired as ex:
                code, text = "TIMEOUT", str(ex)
            except (OSError, ValueError) as ex:
                code, text = 1, f"isolated execution rejected: {ex}"
        entry_log_path(out, entry).write_text(text, encoding="utf-8")
        if reason:
            status = "BLOCKED_ENV"
        elif code == 0:
            status = "PASS"
        elif expect != "PASS":
            status = expect
        else:
            status = "FAIL"
        tail = [line for line in text.strip().splitlines() if line.strip()][-1:] or [""]
        return {"id": entry, "exit": code, "status": status, "expect": expect,
                "seconds": round(time.time() - start, 1), "last": tail[0][:240],
                "command": cmd,
                **({"candidateConfiguration": spec.get("candidateConfiguration", "default"),
                    "candidateDll": str(candidate),
                    "candidateSha256": hashlib.sha256(candidate.read_bytes()).hexdigest().upper() if candidate.is_file() else None}
                   if spec.get("candidateDll") else {})}

    with ThreadPoolExecutor(max_workers=max(1, args.jobs)) as pool:
        results = sorted(pool.map(run, entries), key=lambda r: r["id"])
    (out / "results.json").write_text(json.dumps(results, indent=1, ensure_ascii=False), encoding="utf-8")
    counts: dict[str, int] = {}
    for r in results:
        counts[r["status"]] = counts.get(r["status"], 0) + 1
    for r in results:
        if r["status"] in {"FAIL", "BLOCKED_ENV"}:
            print(f"{r['status']:<11} {r['id']} | {r['last'][:150]}")
    print(f"entries={len(results)} " + " ".join(f"{k}={v}" for k, v in sorted(counts.items())) + f" out={out}")
    return 1 if counts.get("FAIL") else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
