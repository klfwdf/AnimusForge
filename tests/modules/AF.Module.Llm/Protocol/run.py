"""Exercise the real LLM protocol sources with synthetic, keyless fixtures."""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import stat
import subprocess
import sys
from xml.sax.saxutils import escape


HERE = Path(__file__).resolve().parent
ROOT = next((parent for parent in HERE.parents if (parent / "AnimusForge.csproj").is_file() and (parent / ".git").exists()), None)
if ROOT is None:
    raise RuntimeError("AnimusForge Git root not found")
BASE_REVISION = "99360142b9b4fa5ca309cadf2cf62b627b1cdda8"
OUTPUT_ROOT = ROOT / "artifacts/tests/llm-protocol"
spec = importlib.util.spec_from_file_location("boundary_extractor", ROOT / "tools/ChannelCutoverBoundaryTests/run.py")
extractor = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extractor)

METHODS = {
    "HasEmptyResponseRetryMarker": "private static bool HasEmptyResponseRetryMarker(",
    "IsBattleSpeechRequest": "private static bool IsBattleSpeechRequest(",
    "GetLastMessageRole": "private static string GetLastMessageRole(",
    "EnsureFinalUserTurn": "private static List<object> EnsureFinalUserTurn(",
    "BuildEmptyResponseRetryMessages": "private static List<object> BuildEmptyResponseRetryMessages(",
    "ContainsAnyIgnoreCase": "private static bool ContainsAnyIgnoreCase(",
    "LooksLikeThinkingControlError": "private static bool LooksLikeThinkingControlError(",
    "TryReadMessage": "private static bool TryReadMessage(",
}
CONSTANTS = (
    "EmptyResponseRetryMarker", "EmptyResponseRetryInstruction",
    "GenericContinuationInstruction", "BattleSpeechContinuationInstruction",
)
INVERSE_SPANS = (
    ("private const string EmptyResponseRetryMarker", "private static void LogNormalizedMessageTail("),
    ("private static List<object> BuildEmptyResponseRetryMessages(", "private static string ExtractTextFromGeminiCandidateParts("),
    ("private static bool ContainsAnyIgnoreCase(", "private static bool TryApplyPrimaryThinkingControls("),
    ("private static bool LooksLikeThinkingControlError(", "private static bool TryResolvePrimaryModelByDropdownState("),
    ("private static bool TryReadMessage(", "public static void RecordPrimaryRequestBodyForTokenStats("),
)
HOST_CALLS = {
    "GetLastMessageRole": 1, "EnsureFinalUserTurn": 4,
    "IsBattleSpeechRequest": 1, "TryReadMessage": 1,
    "LooksLikeThinkingControlError": 2, "HasEmptyResponseRetryMarker": 2,
    "BuildEmptyResponseRetryMessages": 2,
}
EXPECTED_FAILURES = {
    "drop-tail": "tail-required",
    "mutate-input": "input-list-unchanged",
    "drop-marker-detection": "retry-marker-detected",
    "thinking-or": "thinking-needs-both",
    "visible-bypass": "visible-envelope",
    "stream-duplicate": "stream-no-duplicate",
    "anthropic-empty": "anthropic-empty-user",
}


def source(path: str, ref: str | None) -> str:
    return extractor.source(path, ref).replace("\r\n", "\n")


def one_declaration(text: str, signature: str) -> str:
    if text.count(signature) != 1:
        raise ValueError(f"Declaration is not unique: {signature}")
    return extractor.declaration(text, signature)


def one_constant(text: str, name: str) -> str:
    signature = "private const string " + name
    if text.count(signature) != 1:
        raise ValueError(f"Constant is not unique: {name}")
    start = text.index(signature)
    end = text.index(";", start) + 1
    return text[start:end]


def before_policy(shout: str) -> tuple[str, dict[str, str]]:
    blocks = {name: one_constant(shout, name) for name in CONSTANTS}
    blocks.update({name: one_declaration(shout, signature) for name, signature in METHODS.items()})
    ordered = ("EmptyResponseRetryMarker", "EmptyResponseRetryInstruction",
               "HasEmptyResponseRetryMarker", "GenericContinuationInstruction",
               "BattleSpeechContinuationInstruction", "IsBattleSpeechRequest",
               "GetLastMessageRole", "EnsureFinalUserTurn", "BuildEmptyResponseRetryMessages",
               "ContainsAnyIgnoreCase", "LooksLikeThinkingControlError", "TryReadMessage")
    parts = []
    for name in ordered:
        block = blocks[name]
        if name in METHODS and name != "ContainsAnyIgnoreCase":
            assert block.startswith("private static ")
            block = "internal static " + block[len("private static "):]
        parts.append(block)
    header = "using System;\nusing System.Collections.Generic;\nusing System.Linq;\nusing System.Reflection;\nusing Newtonsoft.Json.Linq;\n\nnamespace AnimusForge;\n\ninternal static class PrimaryChatMessagePolicy\n{\n"
    return header + "\n\n".join(parts) + "\n}\n", blocks


def inverse_check(actual_shout: str, actual_policy: str) -> dict[str, str]:
    # J08 transport changes have their own executable old/current HTTP differential.
    # Keep this J01 check scoped to unchanged protocol extraction, not new HTTP I/O.
    transport_review = json.loads((ROOT / "tests/modules/AF.Module.Llm/NonStreamingTransport/primary-source-review.json").read_text(encoding="utf-8-sig"))
    primary_signature = "public static async Task<string> CallApiWithMessages("
    current = one_declaration(actual_shout, primary_signature)
    prior = one_declaration(source("ShoutNetwork.cs", transport_review["baseline"]), primary_signature)
    if current != prior:
        if hashlib.sha256(current.encode()).hexdigest() != transport_review["currentMethodSha256"]:
            raise ValueError("Unreviewed J08 non-stream consumer change")
        actual_shout = actual_shout.replace(current, prior, 1)
    baseline = source("ShoutNetwork.cs", BASE_REVISION)
    baseline_blocks = {name: one_constant(baseline, name) for name in CONSTANTS}
    baseline_blocks.update({name: one_declaration(baseline, signature) for name, signature in METHODS.items()})
    for name, original in baseline_blocks.items():
        signature = original.split("(", 1)[0] + "(" if name in METHODS else "private const string " + name
        if name in METHODS and name != "ContainsAnyIgnoreCase":
            signature = signature.replace("private static ", "internal static ", 1)
        current = one_declaration(actual_policy, signature) if name in METHODS else one_constant(actual_policy, name)
        normalized = current.replace("internal static ", "private static ", 1) if name in METHODS else current
        if normalized != original:
            raise ValueError(f"Policy declaration differs from baseline: {name}")
    reconstructed = actual_shout
    for name, count in HOST_CALLS.items():
        qualified = "PrimaryChatMessagePolicy." + name + "("
        if reconstructed.count(qualified) != count:
            raise ValueError(f"Host call count differs: {name}")
        reconstructed = reconstructed.replace(qualified, name + "(")
    for start, anchor in INVERSE_SPANS:
        if baseline.count(start) != 1 or baseline.count(anchor) != 1 or reconstructed.count(anchor) != 1:
            raise ValueError(f"Inverse anchor differs: {start}")
        span = baseline[baseline.index("\t" + start):baseline.index("\t" + anchor)]
        reconstructed = reconstructed.replace("\t" + anchor, span + "\t" + anchor, 1)
    if reconstructed != baseline:
        raise ValueError("ShoutNetwork inverse differs from baseline")
    return {name: hashlib.sha256(value.encode()).hexdigest() for name, value in baseline_blocks.items()}


def replace_once(text: str, old: str, new: str, mutation: str) -> str:
    if text.count(old) != 1:
        raise ValueError(f"Mutation anchor is not unique: {mutation}")
    return text.replace(old, new)


def mutate(sources: dict[str, str], mutation: str) -> None:
    policy = sources["PrimaryChatMessagePolicy.cs"]
    compat = sources["LlmApiCompat.cs"]
    normalizer = sources["LlmVisibleReplyNormalizer.cs"]
    if mutation == "drop-tail":
        policy = replace_once(policy, "result.Add(new\n", "if (false) result.Add(new\n", mutation)
    elif mutation == "mutate-input":
        policy = replace_once(policy, "result.Add(message);", "result.Add(message); if (message is IDictionary<string, object> source) source[\"content\"] = \"mutated\";", mutation)
    elif mutation == "drop-marker-detection":
        old = one_declaration(policy, "internal static bool HasEmptyResponseRetryMarker(")
        policy = replace_once(policy, old, replace_once(old, "return true;", "return false;", mutation), mutation)
    elif mutation == "thinking-or":
        policy = replace_once(policy, "return flag && flag2;", "return flag || flag2;", mutation)
    elif mutation == "visible-bypass":
        normalizer = replace_once(normalizer, "return TryNormalizeEnvelope(candidate, 0, out var normalized) ? normalized.Trim() : original;", "return original;", mutation)
    elif mutation == "stream-duplicate":
        normalizer = replace_once(normalizer, "string suffix = preview.Substring(_lastPreview.Length);", "string suffix = preview;", mutation)
    elif mutation == "anthropic-empty":
        compat = replace_once(compat, 'AddAnthropicMessage(messages, "user", " ");', 'AddAnthropicMessage(messages, "assistant", " ");', mutation)
    else:
        raise ValueError("Unknown mutation")
    sources.update({"PrimaryChatMessagePolicy.cs": policy, "LlmApiCompat.cs": compat, "LlmVisibleReplyNormalizer.cs": normalizer})


def check_output_path(name: str) -> Path:
    if not re.fullmatch(r"[A-Za-z0-9_-]+", name) or ".." in name:
        raise ValueError("Unsafe output name")
    output = OUTPUT_ROOT / name
    root = ROOT.resolve(strict=True)
    chain = (ROOT, ROOT / "artifacts", ROOT / "artifacts/tests", OUTPUT_ROOT, output)
    for path in chain:
        if os.path.lexists(path):
            info = path.lstat()
            if not stat.S_ISDIR(info.st_mode) or stat.S_ISLNK(info.st_mode) or getattr(info, "st_file_attributes", 0) & stat.FILE_ATTRIBUTE_REPARSE_POINT:
                raise ValueError(f"Output ancestor is not a plain directory: {path}")
    if os.path.commonpath((str(output.resolve()), str(root))) != str(root):
        raise ValueError("Output path escapes workspace")
    if os.path.lexists(output):
        pending = [output]
        while pending:
            current = pending.pop()
            with os.scandir(current) as entries:
                for entry in entries:
                    info = entry.stat(follow_symlinks=False)
                    if stat.S_ISLNK(info.st_mode) or getattr(info, "st_file_attributes", 0) & stat.FILE_ATTRIBUTE_REPARSE_POINT:
                        raise ValueError(f"Output contains a reparse point: {entry.path}")
                    if stat.S_ISDIR(info.st_mode):
                        pending.append(Path(entry.path))
    return output


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", required=True)
    parser.add_argument("--newtonsoft", required=True)
    parser.add_argument("--source-ref")
    parser.add_argument("--phase", required=True, choices=("before", "after"))
    parser.add_argument("--layout", choices=("extracted", "relocated"))
    parser.add_argument("--output-name", required=True)
    parser.add_argument("--mutation", choices=sorted(EXPECTED_FAILURES))
    args = parser.parse_args()
    if args.phase == "before" and args.layout:
        parser.error("--layout is only valid after extraction")
    if args.phase == "after" and args.source_ref:
        parser.error("--source-ref is only valid for before")
    for label, value in (("dotnet", args.dotnet), ("newtonsoft", args.newtonsoft)):
        path = Path(value)
        if not path.is_absolute() or not path.is_file():
            parser.error(f"{label} must name an existing absolute file")
    try:
        output = check_output_path(args.output_name)
        current_paths = ("LlmApiCompat.cs", "LlmVisibleReplyNormalizer.cs") if args.phase == "before" or args.layout == "extracted" else (
            "src/modules/AF.Module.Llm/Protocol/LlmApiCompat.cs",
            "src/modules/AF.Module.Llm/Protocol/LlmVisibleReplyNormalizer.cs")
        ref = args.source_ref if args.phase == "before" else None
        compat = source(current_paths[0], ref)
        normalizer = source(current_paths[1], ref)
        if args.phase == "before":
            policy, blocks = before_policy(source("ShoutNetwork.cs", ref))
            block_hashes = {name: hashlib.sha256(value.encode()).hexdigest() for name, value in blocks.items()}
        else:
            policy = source("src/modules/AF.Module.Llm/Protocol/PrimaryChatMessagePolicy.cs", None)
            block_hashes = inverse_check(source("ShoutNetwork.cs", None), policy)
        sources = {"LlmApiCompat.cs": compat, "LlmVisibleReplyNormalizer.cs": normalizer, "PrimaryChatMessagePolicy.cs": policy}
        if args.mutation:
            mutate(sources, args.mutation)
        program = (HERE / "Program.cs").read_text(encoding="utf-8-sig")
        fingerprint = hashlib.sha256((program + "\n" + "\n".join(sources.values())).encode()).hexdigest()
        marker = output / "source-fingerprints.json"
        if os.path.lexists(output):
            raise ValueError("Output already exists; choose a fresh output name")
        output.mkdir(parents=True, exist_ok=True)
        generated = args.phase == "before" or bool(args.mutation)
        for name, content in sources.items():
            if generated:
                (output / name).write_text(content, encoding="utf-8")
        (output / "Program.cs").write_text(program, encoding="utf-8")
        include_paths = {name: output / name if generated else ROOT / path for name, path in zip(sources, (*current_paths, "src/modules/AF.Module.Llm/Protocol/PrimaryChatMessagePolicy.cs"))}
        refs = '<Reference Include="Newtonsoft.Json"><HintPath>' + escape(str(Path(args.newtonsoft).resolve())) + '</HintPath></Reference>'
        refs += ''.join('<Compile Include="' + escape(str(path)) + '" Link="' + name + '"/>' for name, path in include_paths.items())
        project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup>' + refs + '<Compile Include="Program.cs"/></ItemGroup></Project>'
        (output / "Tests.csproj").write_text(project, encoding="utf-8")
        (output / "NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding="utf-8")
        meta = {"phase": args.phase, "layout": args.layout, "sourceRef": ref, "mutation": args.mutation, "fingerprint": fingerprint, "blockSha256": block_hashes}
        marker.write_text(json.dumps(meta, indent=2, sort_keys=True), encoding="utf-8")
        sdk = Path(args.dotnet).resolve().parent
        env = os.environ.copy()
        env.update(DOTNET_ROOT=str(sdk), DOTNET_CLI_HOME=str(ROOT / ".tmp/dotnet-cli"), NUGET_PACKAGES=str(ROOT / ".tmp/nuget-packages"), DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1", DOTNET_GENERATE_ASPNET_CERTIFICATE="false", DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE="true")
        env["PATH"] = str(sdk) + os.pathsep + env.get("PATH", "")
        steps = (("restore", [args.dotnet, "restore", "Tests.csproj", "--configfile", "NuGet.Config"]),
                 ("build", [args.dotnet, "build", "Tests.csproj", "-c", "Release", "--no-restore"]),
                 ("run", [args.dotnet, str(output / "bin/Release/net8.0/Tests.dll"), *( ["--only", EXPECTED_FAILURES[args.mutation]] if args.mutation else [] )]))
        for step, command in steps:
            result = subprocess.run(command, cwd=output, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=180)
            log = result.stdout + result.stderr
            (output / f"{step}.log").write_text(log, encoding="utf-8")
            print(f"{step} exit={result.returncode} log={output / (step + '.log')}")
            if step != "run" and result.returncode != 0:
                return result.returncode
            if step == "run":
                if args.mutation:
                    marker_text = "FAIL:" + EXPECTED_FAILURES[args.mutation]
                    if result.returncode == 0 or marker_text not in log:
                        raise RuntimeError("Mutation was not rejected by its designated assertion")
                    print("EXPECTED_MUTATION_REJECTED " + EXPECTED_FAILURES[args.mutation])
                elif result.returncode != 0:
                    return result.returncode
        return 0
    except (ValueError, FileNotFoundError, subprocess.CalledProcessError, subprocess.TimeoutExpired, RuntimeError) as error:
        print(f"protocol runner error: {error}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
