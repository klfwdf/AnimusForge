"""Replay all production stream publication guards against canceled and late channel sinks."""
from pathlib import Path
import argparse
import importlib.util
import os
import re
import subprocess
import sys,json,hashlib
sys.path.insert(0,str(Path(__file__).resolve().parents[4]/"tests"))
from output_isolation import new_run_root,minimal_test_environment

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location(
    "extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--mutate", choices=["cancel-partial", "late-exception"])
parser.add_argument("--run-root",type=Path)
args = parser.parse_args()
source = (ROOT / "src/modules/AF.Module.Llm/ShoutNetwork.cs").read_text(encoding="utf-8-sig")
wrapper = extract.declaration(source, "public static async Task CallApiWithMessagesStream(")
assert wrapper.count("await CallApiWithMessagesStreamCore(")==1
assert 'DuelSettings.GetSettings()?.MainApiStreamingEnabled == true' in wrapper
assert 'cancellationToken.ThrowIfCancellationRequested();' in wrapper
assert 'SaveRuntimeGuard.IsStale(runtimeGeneration, "primary_chat_non_stream_callback") || string.IsNullOrWhiteSpace(result)' in wrapper
method = extract.declaration(source, "private static async Task CallApiWithMessagesStreamCore(")
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
out = new_run_root(ROOT,"stream-cancellation-projection",args.run_root)

template = (HERE / "Harness.cs.txt").read_text(encoding="utf-8-sig")
(out / "Program.cs").write_text(template.replace("@@BLOCKS@@", blocks), encoding="utf-8")
(out / "Tests.csproj").write_text(
    '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
    '<TargetFramework>net8.0</TargetFramework><Nullable>disable</Nullable>'
    '<ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text(
    "<configuration><packageSources><clear/></packageSources></configuration>", encoding="utf-8")
dotnet = ROOT / "local/dotnet/8.0.425/dotnet.exe"
env = minimal_test_environment(dotnet,out)
result = subprocess.run([str(dotnet), "run", "--project", str(out / "Tests.csproj"), "-c", "Release"],
                        cwd=out, env=env, capture_output=True, text=True,
                        encoding="utf-8", errors="replace", timeout=90)
log = result.stdout + result.stderr
(out / "run.log").write_text(log, encoding="utf-8")
(out / "receipt.json").write_text(json.dumps({"productionRaw":hashlib.sha256((ROOT/"src/modules/AF.Module.Llm/ShoutNetwork.cs").read_bytes()).hexdigest(),"coreBodySha":hashlib.sha256(method.encode("utf-8")).hexdigest(),"exitCode":result.returncode,"layer":"six current source-derived stream-core guard expressions, synthetic channel sinks; wrapper guard static proof; not whole transport","mutation":args.mutate},indent=2),encoding="utf-8")
print(log, end="")
raise SystemExit(result.returncode)
