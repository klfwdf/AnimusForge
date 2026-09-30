"""Replay all production stream publication guards against canceled and late channel sinks."""
from pathlib import Path
import argparse
import importlib.util
import os
import re
import subprocess

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location(
    "extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--mutate", choices=["cancel-partial", "late-exception"])
args = parser.parse_args()
source = (ROOT / "ShoutNetwork.cs").read_text(encoding="utf-8-sig")
method = extract.declaration(source, "public static async Task CallApiWithMessagesStream(")
guard_pattern = re.compile(
    r'if \((!cancellationToken\.IsCancellationRequested && !SaveRuntimeGuard\.IsStale\(runtimeGeneration, "([^"]+)"\))\)')
guard_matches = list(guard_pattern.finditer(method))
guards = []
for callback in re.finditer(r'onComplete\?\.Invoke\(', method):
    preceding = [match for match in guard_matches if match.end() < callback.start()]
    assert preceding and callback.start() - preceding[-1].end() < 500, "stream callback has no nearby guard"
    guards.append((preceding[-1].group(1), preceding[-1].group(2)))
expected = (
    "primary_chat_stream_partial_complete", "primary_chat_stream_fallback_complete",
    "primary_chat_stream_empty_retry_complete", "primary_chat_stream_complete",
    "primary_chat_stream_cancelled_complete", "primary_chat_stream_exception_partial_complete")
assert method.count("onComplete?.Invoke(") == len(expected), "unreviewed stream completion callback"
assert tuple(marker for _, marker in guards) == expected, "stream callback guard missing or reordered"
assert 'if (!cancellationToken.IsCancellationRequested && !SaveRuntimeGuard.IsStale(runtimeGeneration, "primary_chat_stream_chunk"))' in method
assert 'if (!cancellationToken.IsCancellationRequested && !SaveRuntimeGuard.IsStale(runtimeGeneration, "primary_chat_stream_flush"))' in method
if args.mutate == "cancel-partial":
    guards = [(condition.replace("!cancellationToken.IsCancellationRequested && ", "") if marker == "primary_chat_stream_cancelled_complete" else condition, marker)
              for condition, marker in guards]
elif args.mutate == "late-exception":
    guards = [(condition.replace('!SaveRuntimeGuard.IsStale(runtimeGeneration, "primary_chat_stream_exception_partial_complete")', 'true')
               if marker == "primary_chat_stream_exception_partial_complete" else condition, marker)
              for condition, marker in guards]
blocks = "\n".join(
    f'case {index}: if ({condition}) onComplete?.Invoke("partial"); break;'
    for index, (condition, _) in enumerate(guards))
out = ROOT / "artifacts/j17b/session-20260930/p5-channels" / (
    "stream-cancel-001" if not args.mutate else "stream-cancel-" + args.mutate + "-001")
out.mkdir(parents=True, exist_ok=True)
template = (HERE / "Harness.cs.txt").read_text(encoding="utf-8-sig")
(out / "Program.cs").write_text(template.replace("@@BLOCKS@@", blocks), encoding="utf-8")
(out / "Tests.csproj").write_text(
    '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
    '<TargetFramework>net8.0</TargetFramework><Nullable>disable</Nullable>'
    '<ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text(
    "<configuration><packageSources><clear/></packageSources></configuration>", encoding="utf-8")
dotnet = ROOT / "local/dotnet/8.0.425/dotnet.exe"
env = {
    "DOTNET_ROOT": str(dotnet.parent), "DOTNET_CLI_HOME": str(out / "cli"),
    "DOTNET_CLI_TELEMETRY_OPTOUT": "1", "DOTNET_NOLOGO": "1",
    "TEMP": str(out), "TMP": str(out), "APPDATA": str(out),
    "LOCALAPPDATA": str(out), "USERPROFILE": str(out), "HOME": str(out),
    "HOMEDRIVE": out.drive, "HOMEPATH": str(out)[len(out.drive):],
    "NUGET_PACKAGES": str(out / "packages"),
    "SystemRoot": os.environ.get("SystemRoot", r"C:\Windows"),
    "ProgramData": os.environ.get("ProgramData", r"C:\ProgramData"),
    "ALLUSERSPROFILE": os.environ.get("ALLUSERSPROFILE", r"C:\ProgramData"),
    "ProgramFiles": os.environ.get("ProgramFiles", r"C:\Program Files"),
    "ProgramFiles(x86)": os.environ.get("ProgramFiles(x86)", r"C:\Program Files (x86)"),
    "windir": os.environ.get("windir", r"C:\Windows"),
}
result = subprocess.run([str(dotnet), "run", "--project", str(out / "Tests.csproj"), "-c", "Release"],
                        cwd=out, env=env, capture_output=True, text=True,
                        encoding="utf-8", errors="replace", timeout=90)
log = result.stdout + result.stderr
(out / "run.log").write_text(log, encoding="utf-8")
print(log, end="")
raise SystemExit(result.returncode)
