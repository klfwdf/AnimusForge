"""Replay extracted encounter and duel control flow; no game or network access."""
from pathlib import Path
import argparse
import importlib.util
import os
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, minimal_test_environment
SPEC = importlib.util.spec_from_file_location("boundary_extractor", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
EXTRACTOR = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(EXTRACTOR)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", type=Path, default=os.environ.get("DOTNET_EXE") or os.environ.get("AF_DOTNET") or ROOT / "local/dotnet/8.0.425/dotnet.exe")
    parser.add_argument("--run-root", type=Path)
    args = parser.parse_args()
    dotnet = args.dotnet.resolve()
    if not dotnet.is_file():
        parser.error(f"dotnet executable not found: {dotnet}")
    template = Path(__file__).with_name("Harness.cs.txt").read_text(encoding="utf-8")
    source = (ROOT / "src/AF.GameAdapter.Bannerlord/Encounter/LordEncounterBehavior.cs").read_text(encoding="utf-8-sig")
    handoff_signatures = [
        "private static void ClearNativeDialogueHandoff(",
        "private static void RegisterNativeDialogueHandoff(",
        "private static bool IsNativeDialogueHandoffSuppressedForCurrentEncounter(",
        "private static bool CanReturnFromNativeDialogueHandoff(",
        "private static void OnNativeDialogueHandoffEnded(",
    ]
    template = template.replace("@@HANDOFF_METHODS@@", "\n".join(
        EXTRACTOR.declaration(source, signature) for signature in handoff_signatures))
    template = template.replace("@@PLAYER_LEAVE_METHOD@@", EXTRACTOR.declaration(
        source, "internal static void PreparePlayerRequestedNativeConversationLeave("))
    signatures = [
        "private sealed class MeetingPlayerReleaseRequest",
        "private static MeetingPlayerReleaseRequest CaptureMeetingPlayerReleaseRequest(",
        "private static bool IsMeetingPlayerReleaseRequestCurrent(",
        "private static void AuthorizeMeetingPlayerRelease(",
        "private static bool ConsumeMeetingPlayerReleaseAuthorization(",
        "private static void ClearMeetingPlayerReleaseAuthorization(",
        "private static bool HasPendingNativeConversationMeetingRelease(",
        "private static void ClearPendingNativeConversationMeetingRelease(",
        "private static void TryForcePendingNativeConversationMeetingReleaseIfReady(",
    ]
    methods = "\n".join(EXTRACTOR.declaration(source, signature) for signature in signatures)
    template = template.replace("@@RELEASE_METHODS@@", methods)
    duel = (ROOT / "src/modules/AF.Module.Duel/Host/DuelBehavior.cs").read_text(encoding="utf-8-sig")
    template = template.replace("@@DUEL_METHOD@@", EXTRACTOR.declaration(duel, "public static void GlobalSourceMissionLeaveTick("))
    focus = (ROOT / "src/AF.GameAdapter.Bannerlord/Patches/Safety/InteractionComponentSafePatch.cs").read_text(encoding="utf-8-sig")
    template = template.replace("@@FOCUS_METHOD@@", EXTRACTOR.declaration(focus, "public static void EnsurePatched("))
    assert "@@" not in template
    output = new_run_root(ROOT, "encounter-lifecycle-boundary", args.run_root)
    (output / "Program.cs").write_text(template, encoding="utf-8")
    (output / "Boundary.csproj").write_text(f'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable><NoWarn>CS0649;CS0414</NoWarn></PropertyGroup><ItemGroup><Compile Include="{ROOT.as_posix()}/src/modules/AF.Module.Encounter/EncounterTargetOwner.cs" Link="EncounterTargetOwner.cs" /><Compile Include="{ROOT.as_posix()}/src/modules/AF.Module.Encounter/EncounterConversationTargetOwner.cs" Link="EncounterConversationTargetOwner.cs" /><Compile Include="{ROOT.as_posix()}/src/modules/AF.Module.Encounter/EncounterReleaseOwner.cs" Link="EncounterReleaseOwner.cs" /><Compile Include="{ROOT.as_posix()}/src/modules/AF.Module.Encounter/EncounterPendingReturnOwner.cs" Link="EncounterPendingReturnOwner.cs" /><Compile Include="{ROOT.as_posix()}/src/modules/AF.Module.Encounter/NativeDialogueReturnPolicy.cs" Link="NativeDialogueReturnPolicy.cs" /></ItemGroup></Project>', encoding="utf-8")
    (output / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
    env = minimal_test_environment(dotnet, output)
    result = subprocess.run([str(dotnet), "run", "--project", str(output / "Boundary.csproj"), "-c", "Release"], cwd=output, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120)
    (output / "run.log").write_text(result.stdout + result.stderr, encoding="utf-8")
    print(result.stdout + result.stderr)
    return result.returncode

if __name__ == "__main__":
    raise SystemExit(main())
