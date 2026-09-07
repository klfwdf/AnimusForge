"""Replay extracted encounter and duel control flow; no game or network access."""
from pathlib import Path
import importlib.util
import os
import subprocess

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("boundary_extractor", ROOT / "tools/ChannelCutoverBoundaryTests/run.py")
EXTRACTOR = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(EXTRACTOR)

def main():
    template = Path(__file__).with_name("Harness.cs.txt").read_text(encoding="utf-8")
    source = (ROOT / "LordEncounterBehavior.cs").read_text(encoding="utf-8-sig")
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
    duel = (ROOT / "DuelBehavior.cs").read_text(encoding="utf-8-sig")
    template = template.replace("@@DUEL_METHOD@@", EXTRACTOR.declaration(duel, "public static void GlobalSourceMissionLeaveTick("))
    focus = (ROOT / "InteractionComponentSafePatch.cs").read_text(encoding="utf-8-sig")
    template = template.replace("@@FOCUS_METHOD@@", EXTRACTOR.declaration(focus, "public static void EnsurePatched("))
    assert "@@" not in template
    output = ROOT / ".tmp/encounter-lifecycle-boundary"
    output.mkdir(parents=True, exist_ok=True)
    (output / "Program.cs").write_text(template, encoding="utf-8")
    (output / "Boundary.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable><NoWarn>CS0649;CS0414</NoWarn></PropertyGroup></Project>', encoding="utf-8")
    (output / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
    dotnet = ROOT.parent / ".dotnet-sdk/dotnet.exe"
    env = dict(os.environ, DOTNET_ROOT=str(dotnet.parent), DOTNET_CLI_HOME=str(ROOT / ".tmp/dotnet-cli"), DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_CLI_UI_LANGUAGE="en")
    result = subprocess.run([str(dotnet), "run", "--project", str(output / "Boundary.csproj"), "-c", "Release"], cwd=output, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120)
    (output / "run.log").write_text(result.stdout + result.stderr, encoding="utf-8")
    print(result.stdout + result.stderr)
    return result.returncode

if __name__ == "__main__":
    raise SystemExit(main())
