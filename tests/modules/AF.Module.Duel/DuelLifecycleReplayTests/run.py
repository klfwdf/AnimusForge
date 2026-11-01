"""Replay the production Arena duel lifecycle with explicit native/ledger fakes.

Requires Python 3 and .NET 8. All production lifecycle methods, fields and
constructors are copied verbatim. Only unrelated spawn-frame geometry is omitted
and replaced by a deterministic external stub. Native spawn failures, callback
order and Mission clocks are simulated, not live-game evidence.
"""
import argparse
import hashlib
import json
import pathlib
import subprocess
import sys


def block(source, signature):
    start = source.index(signature)
    end = source.index("{", start) + 1
    depth = 1
    while depth:
        depth += (source[end] == "{") - (source[end] == "}")
        end += 1
    return source[start:end]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", default="dotnet")
    parser.add_argument("--output", type=pathlib.Path)
    args = parser.parse_args()
    here = pathlib.Path(__file__).resolve().parent
    root = here.parents[3]
    source_path = root / "src/modules/AF.Module.Duel/Host/DuelBehavior.cs"
    raw = source_path.read_bytes()
    arena = block(raw.decode("utf-8-sig"), "private class ArenaDuelMissionBehavior")
    geometry_start = arena.index("\t\tprivate static bool TryGetTaggedFrame(")
    geometry_end = arena.index("\t\tpublic override void OnMissionTick(", geometry_start)
    production = arena[:geometry_start] + arena[geometry_end:]
    generated = "using System;\nusing System.Reflection;\npartial class Replay\n{\n" + production + "\n}\n"
    generated_path = here / "obj/ExtractedProduction.cs"
    generated_path.parent.mkdir(parents=True, exist_ok=True)
    generated_path.write_text(generated, encoding="utf-8")
    output = args.output or here / "obj/evidence"
    output.mkdir(parents=True, exist_ok=True)
    command = [args.dotnet, "run", "--project", str(here / "DuelLifecycleReplayTests.csproj")]
    result = subprocess.run(command, capture_output=True, text=True, encoding="utf-8", errors="replace")
    log = result.stdout + result.stderr
    (output / "output.log").write_text(log, encoding="utf-8")
    (output / "ExtractedProduction.cs").write_text(generated, encoding="utf-8")
    accepted = result.returncode == 0 and "failed=0" in log
    receipt = {
        "source": str(source_path), "source_sha256": hashlib.sha256(raw).hexdigest(),
        "extracted_sha256": hashlib.sha256(generated.encode()).hexdigest(),
        "command": command, "exit_code": result.returncode, "accepted": accepted,
        "production": "Arena fields/constructors, startup/retry, exact binding, callbacks, prep/tick, result poll and complete EndDuelLocal settlement body, verbatim",
        "fakes": ["Native SpawnAgent/Team/Agent/index lookup/clock/input/controller", "typed outcome ledger and game-side effects recorded", "spawn-frame geometry deterministic stub"],
        "uncovered": ["actual streamer reproduction", "native renderer/scene loading", "real third-party callback exceptions", "real campaign death/save and economy", "dual-API assembly build"],
    }
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    print(log)
    print("EVIDENCE " + str(output / "receipt.json"))
    return 0 if accepted else 1


if __name__ == "__main__":
    sys.exit(main())
