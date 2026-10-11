"""Replay verbatim meeting escort methods; native scene/spawn/supplier edges are fixtures."""
from pathlib import Path
import argparse
import hashlib
import importlib.util
import json
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import minimal_test_environment, new_run_root, resolve_dotnet

SPEC = importlib.util.spec_from_file_location(
    "escort_boundary_extractor",
    ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py",
)
EXTRACTOR = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(EXTRACTOR)
SOURCE = ROOT / "src/AF.GameAdapter.Bannerlord/Encounter/MeetingBattleLockMissionBehavior.cs"
NATIVE = {
    "1.3": ROOT / "原版游戏本体代码1.3.x/TaleWorlds.CampaignSystem/AgentOrigins",
    "1.4": ROOT / "原版游戏本体代码1.4.5/TaleWorlds.CampaignSystem/TaleWorlds/CampaignSystem/AgentOrigins",
}


def extract(source, signature, optional=False):
    return EXTRACTOR.declaration(source, signature, optional=optional)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", type=Path)
    parser.add_argument("--run-root", type=Path)
    parser.add_argument("--source-file", type=Path)
    parser.add_argument("--baseline-source", type=Path)
    parser.add_argument("--native-api", choices=("1.3", "1.4"), default="1.4")
    args = parser.parse_args()
    source_path = args.source_file or SOURCE
    source = source_path.read_text(encoding="utf-8-sig")
    output = new_run_root(ROOT, "meeting-escort-roster", args.run_root)
    dotnet = resolve_dotnet(ROOT, args.dotnet, major=10)
    signatures = [
        "private bool TryPlaceEscortGuards(",
        "private int CountHealthyNonHeroTroops(",
        "private int GetAgentEscortPriority(",
        "private int GetCharacterEscortPriority(",
        "private bool CharacterPrefersMountedEscort(",
        "private bool IsHorseArcherCharacter(",
        "private int GetAgentTier(",
        "private int GetAgentLevel(",
        "private List<(float fwdDist, float sideDist, bool faceBack)> BuildEscortSlots(",
        "private void ConfigureMeetingHoldFormations(",
        "private bool IsMeetingEscortAlreadyPositioned(",
    ]
    optional = [
        "private bool ShouldRequireEscortForSide(",
        "private int CountCurrentTeamEscortCandidates(",
        "private bool TryGetEncounterBattleTroopCountForTeam(",
        "private bool TryGetBattleSideForTeam(",
        "private MapEvent GetCurrentEncounterBattleSafe(",
        "private bool ShouldPositionEscortSide(",
        "private List<Agent> CollectNearbyMeetingEscortAgents(",
        "private bool IsMeetingEscortCandidate(",
        "private Agent ResolveMeetingEscortForSide(",
        "private Agent SelectExistingMeetingEscortAgent(",
        "private bool IsReusableMeetingEscortCandidate(",
        "private bool HasHigherMeetingEscortPriority(",
    ]
    blocks = [extract(source, signature) for signature in signatures]
    blocks += [extract(source, signature, True) for signature in optional]
    blocks = [block for block in blocks if block]
    assert all(block in source for block in blocks)
    tick = extract(source, "if (_leadersPlaced && !_escortsPlaced && _escortPlacementTimer <= 0f)")
    timing_fields = []
    for signature, legacy_fixture in (
        ("private const float MeetingEscortPlacementTimeoutSeconds", "private const float MeetingEscortPlacementTimeoutSeconds = 5f;"),
        ("private float _escortPlacementStartedAt", "private float _escortPlacementStartedAt = -1f;"),
    ):
        # Only the historical red source needs fixture fields absent from its implementation.
        timing_fields.append(next((line.strip() for line in source.splitlines()
                                   if line.strip().startswith(signature)), legacy_fixture))
    start = extract(source, "public override void AfterStart(")
    mission_tick = extract(source, "public override void OnMissionTick(")
    leader_placement = extract(mission_tick, "if (!_leadersPlaced && _mainAgent != null && _targetAgent != null)")
    origin_blocks = {}
    for name in ("PartyAgentOrigin", "PartyGroupAgentOrigin"):
        text = (NATIVE[args.native_api] / (name + ".cs")).read_text(encoding="utf-8-sig")
        origin_blocks[name] = "\n".join(extract(text, "public void " + method + "(")
                                         for method in ("SetKilled", "SetWounded"))
    unchanged = []
    if args.baseline_source:
        baseline = args.baseline_source.read_text(encoding="utf-8-sig")
        for signature in (
            "public override void OnAgentHit(", "public override void OnAgentRemoved(",
            "public override void OnScoreHit(", "private void ReleaseMeetingLocksForCombat(",
            "private void ForceAgentsIntoCombatReadiness(", "private void RestoreTargetLordControllerForCombat(",
            "private void MoveExtraMeetingCircleTroopsAway(", "private void PositionEscortAgents(",
        ):
            assert extract(source, signature) == extract(baseline, signature), signature
            unchanged.append(signature)
    template = (HERE / "Harness.cs.txt").read_text(encoding="utf-8-sig")
    harness = template.replace("@@METHODS@@", "\n\n".join(blocks)).replace("@@TICK@@", tick)
    harness = harness.replace("@@TIMING_FIELDS@@", "\n".join(timing_fields))
    for name, block in origin_blocks.items():
        harness = harness.replace("@@" + name + "@@", block)
    (output / "Harness.cs").write_text(harness, encoding="utf-8")
    (output / "Escort.csproj").write_text(
        '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
        '<TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings>'
        '<Nullable>disable</Nullable><EnableDefaultCompileItems>false</EnableDefaultCompileItems>'
        '</PropertyGroup><ItemGroup><Compile Include="Harness.cs" /></ItemGroup></Project>', encoding="utf-8")
    (output / "NuGet.Config").write_text(
        '<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
    command = [str(dotnet), "run", "--project", str(output / "Escort.csproj"), "-c", "Release"]
    result = subprocess.run(command, cwd=output, env=minimal_test_environment(dotnet, output),
                            capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120)
    wiring = not any(anchor in source for anchor in
                     ("new PartyAgentOrigin(", ".SpawnAgent(", "TrySpawnFallbackEscortsForBothSides(",
                      "SpawnEscortAgentsFromTroops("))
    wiring = wiring and "_escortPlacementStartedAt = -1f;" in start \
        and "_escortPlacementStartedAt = base.Mission.CurrentTime;" in leader_placement \
        and all(anchor in start for anchor in (
            "_meetingPlayerEscortAgent = null;", "_meetingTargetEscortAgent = null;",
            "_playerEscortPlacementFinalized = false;", "_targetEscortPlacementFinalized = false;")) \
        and mission_tick.index("if (MeetingBattleRuntime.IsCombatEscalated)") < mission_tick.index(tick)
    log = result.stdout + result.stderr + ("SOURCE WIRING PASS\n" if wiring else "SOURCE WIRING FAIL: synthetic spawn or initialization/combat boundary mismatch\n")
    code = result.returncode or (0 if wiring else 1)
    (output / "run.log").write_text(log, encoding="utf-8")
    (output / "baseline.cs.txt").write_text(source, encoding="utf-8")
    receipt = {"exitCode": code, "command": command, "sourcePath": str(source_path.resolve()),
               "sourceSha256": hashlib.sha256(source_path.read_bytes()).hexdigest(),
               "sourceWiring": wiring, "nativeApi": args.native_api, "unchangedProductionMethods": unchanged,
               "extractedMethods": len(blocks), "coverage": "Production source spans; native scene/spawn/supplier/roster edges are fixtures; no live game"}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    print(log, end="")
    print("Evidence: " + str(output))
    return code


if __name__ == "__main__":
    raise SystemExit(main())
