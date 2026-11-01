"""Compare actual legacy/current Arena removal and generic-death callback bodies.

Reads the pre-typed-owner Git source (2d1a1264dc) without checking it out. Native
callback order is modeled from Mission.OnAgentRemoved: set state, invoke local
Arena callback then generic callback in registration order, then clear MainAgent.
Only EndDuelLocal business effects are replaced by a result-lock recorder.
Entry-route active flags are ordinary Arena=false, standalone Wilderness=true;
the companion DuelEntryLifecycleReplayTests old/new opener replay verifies those
from actual opener bodies. This is not native-game or campaign-death execution.
"""
import argparse
import hashlib
import json
import pathlib
import subprocess
import sys

from run import block


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", default="dotnet")
    parser.add_argument("--legacy-revision", default="2d1a1264dc226775d9a09dcdbcd28d9c7669e1af")
    parser.add_argument("--current-revision", help="Compare a historical post-refactor source instead of the working tree")
    parser.add_argument("--expect-difference", action="store_true", help="Require the known three fatal-callback parity failures, for the 715097812/a7ccab043 baseline")
    parser.add_argument("--output", type=pathlib.Path)
    args = parser.parse_args()
    here = pathlib.Path(__file__).resolve().parent
    root = here.parents[3]
    legacy_bytes = subprocess.check_output([
        "git", "-c", "safe.directory=" + root.as_posix(), "-C", str(root),
        "show", args.legacy_revision + ":DuelBehavior.cs",
    ])
    current_path = root / "src/modules/AF.Module.Duel/Host/DuelBehavior.cs"
    current_bytes = subprocess.check_output([
        "git", "-c", "safe.directory=" + root.as_posix(), "-C", str(root),
        "show", args.current_revision + ":src/modules/AF.Module.Duel/Host/DuelBehavior.cs",
    ]) if args.current_revision else current_path.read_bytes()
    legacy = legacy_bytes.decode("utf-8-sig")
    current = current_bytes.decode("utf-8-sig")
    legacy_arena = block(legacy, "private class ArenaDuelMissionBehavior")
    current_arena = block(current, "private class ArenaDuelMissionBehavior")
    legacy_generic = block(legacy, "private sealed class DuelMainHeroDeathMissionBehavior")
    current_generic = block(current, "private sealed class DuelMainHeroDeathMissionBehavior")
    signature = "public override void OnAgentRemoved("
    generated = "using System;\npartial class DuelBehavior\n{\n"
    generated += "partial class LegacyArena {\n" + block(legacy_arena, signature) + "\n}\n"
    generated += "partial class CurrentArena {\n" + block(current_arena, signature) + "\n"
    generated += block(current_arena, "internal bool IsCombatParticipant(") + "\n"
    generated += block(current_arena, "private static bool IsDefeatedState(") + "\n}\n"
    generated += "partial class LegacyMainDeath {\n" + block(legacy_generic, signature) + "\n}\n"
    generated += "partial class CurrentMainDeath {\n" + block(current_generic, signature) + "\n}\n}\n"
    destination = here / "obj/LegacyCallbacks.cs"
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(generated, encoding="utf-8")
    output = args.output or here / "obj/legacy-evidence"
    output.mkdir(parents=True, exist_ok=True)
    command = [args.dotnet, "run", "--project", str(here / "LegacyCallbackOrder.csproj")]
    result = subprocess.run(command, capture_output=True, text=True, encoding="utf-8", errors="replace")
    log = result.stdout + result.stderr
    (output / "output.log").write_text(log, encoding="utf-8")
    (output / "LegacyCallbacks.cs").write_text(generated, encoding="utf-8")
    if args.expect_difference:
        accepted = result.returncode == 1 and "RESULT passed=9 failed=3" in log and all(
            "FAIL " + name in log for name in [
                "ordinary_arena/player/Unconscious", "ordinary_arena/player/Killed",
                "standalone_wilderness/player/Unconscious",
            ])
    else:
        accepted = result.returncode == 0 and "failed=0" in log
    receipt = {
        "legacy_revision": args.legacy_revision,
        "legacy_source": "DuelBehavior.cs", "legacy_sha256": hashlib.sha256(legacy_bytes).hexdigest(),
        "current_source": str(current_path), "current_revision": args.current_revision or "working_tree",
        "current_sha256": hashlib.sha256(current_bytes).hexdigest(),
        "extracted_sha256": hashlib.sha256(generated.encode()).hexdigest(),
        "command": command, "exit_code": result.returncode, "accepted": accepted,
        "expected_difference": args.expect_difference,
        "actual_methods": ["legacy/current Arena.OnAgentRemoved", "legacy/current DuelMainHeroDeathMissionBehavior.OnAgentRemoved", "current Arena.IsCombatParticipant/IsDefeatedState"],
        "simulated": ["native Mission ordered callback dispatch", "EndDuelLocal result lock, not business effects", "ForceKillMainHero call count, not actual death", "current generic owner lookup follows active exact local session and excludes recorded result", "entry flags verified by separate entry opener replay"],
        "scope": "Ordinary Arena and standalone Wilderness player/target Killed, Unconscious, Routed; old/new result and AF forced-death call parity",
    }
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    print(log)
    print("EVIDENCE " + str(output / "receipt.json"))
    return 0 if accepted else 1


if __name__ == "__main__":
    sys.exit(main())
