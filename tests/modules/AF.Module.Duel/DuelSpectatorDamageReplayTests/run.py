"""Replay extracted production damage guards against fake agents, not a live Mission.

Requires Python 3 and a .NET 8 SDK. No Bannerlord assemblies or NuGet packages.
Example: python run.py --dotnet <dotnet.exe> --output <evidence-directory>
Use --source-revision <git-revision> --expect-failure to verify a regression baseline.
The two production helper methods and OnAgentHit are copied verbatim. OnScoreHit
is its real prefix through the spectator early return; the unrelated continuation
is replaced by a counter. Freezing, snapshot lookup and native objects are fakes.
"""

import argparse
import hashlib
import json
import pathlib
import subprocess
import sys


def extract_method(source, signature):
    start = source.index(signature)
    brace = source.index("{", start)
    depth = 1
    end = brace + 1
    while depth:
        if source[end] == "{":
            depth += 1
        elif source[end] == "}":
            depth -= 1
        end += 1
    return source[start:end]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", default="dotnet")
    parser.add_argument("--output", type=pathlib.Path)
    parser.add_argument("--source-revision")
    parser.add_argument("--expect-failure", action="store_true")
    args = parser.parse_args()
    here = pathlib.Path(__file__).resolve().parent
    root = here.parents[3]
    relative_source = "src/AF.GameAdapter.Bannerlord/Encounter/MeetingBattleLockMissionBehavior.cs"
    source_path = root / relative_source
    if args.source_revision:
        source_bytes = subprocess.check_output([
            "git", "-c", "safe.directory=" + root.as_posix(), "-C", str(root),
            "show", args.source_revision + ":" + relative_source,
        ])
    else:
        source_bytes = source_path.read_bytes()
    source = source_bytes.decode("utf-8-sig")
    helper = extract_method(source, "\tprivate bool TryHandleFormalDuelSpectatorDamage(")
    restore = extract_method(source, "\tprivate void RestoreAgentHealthFromSnapshot(")
    on_hit = extract_method(source, "\tpublic override void OnAgentHit(")
    on_score = extract_method(source, "\tpublic override void OnScoreHit(")
    continuation = "\n\t\tif (MeetingBattleRuntime.IsCombatEscalated"
    prefix_end = on_score.index(continuation)
    on_score = on_score[:prefix_end] + "\n\t\tScoreContinuationCount++;\n\t}"
    generated = "using System;\npartial class Replay\n{\n" + "\n\n".join(
        [helper, restore, on_hit, on_score]) + "\n}\n"
    generated_path = here / "obj" / "ExtractedProduction.cs"
    generated_path.parent.mkdir(parents=True, exist_ok=True)
    generated_path.write_text(generated, encoding="utf-8")
    output = args.output or here / "obj" / "evidence"
    output.mkdir(parents=True, exist_ok=True)
    command = [args.dotnet, "run", "--project", str(here / "DuelSpectatorDamageReplayTests.csproj")]
    result = subprocess.run(command, capture_output=True, text=True, encoding="utf-8", errors="replace")
    log = result.stdout + result.stderr
    (output / "output.log").write_text(log, encoding="utf-8")
    (output / "ExtractedProduction.cs").write_text(generated, encoding="utf-8")
    expected_failure_observed = result.returncode == 1 and "FAIL attacker_single_compensation" in log and "RESULT " in log
    accepted = expected_failure_observed if args.expect_failure else result.returncode == 0 and "failed=0" in log
    receipt = {
        "source": str(source_path), "revision": args.source_revision or "working_tree",
        "source_sha256": hashlib.sha256(source_bytes).hexdigest(),
        "extracted_sha256": hashlib.sha256(generated.encode()).hexdigest(),
        "command": command, "exit_code": result.returncode,
        "expected_failure": args.expect_failure, "accepted": accepted,
        "scope": "Verbatim production helpers and OnAgentHit; production OnScoreHit prefix only. Fake agents, freezing and snapshot lookup; native deduction and callback order modeled, not live Bannerlord.",
        "uncovered": ["Fatal damage resurrection intentionally unchanged", "native collision/hit generation", "mission lifecycle", "third-party hooks", "full dual-API compilation"],
    }
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    print(log)
    print("EVIDENCE " + str(output / "receipt.json"))
    return 0 if accepted else 1


if __name__ == "__main__":
    sys.exit(main())
