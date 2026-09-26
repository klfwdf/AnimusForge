"""Compile and run effective mutations of the production Scene scope owner."""

import argparse
import os
from pathlib import Path
import subprocess
import sys


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
SOURCE = ROOT / "src/modules/AF.Module.Conversation/Channels/Scene/SceneShoutConversationScope.cs"

MUTATIONS = {
    "epoch-guard": (
        "if (ConversationEpoch != currentConversationEpoch)",
        "if (false)",
        "epoch-mismatch-rejected",
    ),
    "agent-reference-guard": (
        "if (!ReferenceEquals(entry.AgentReference, liveAgent))",
        "if (false)",
        "reference-reuse-rejected",
    ),
    "origin-merge": (
        "existing.OriginFlags |= origin;",
        "existing.OriginFlags = origin;",
        "origin-flags-merged",
    ),
}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", default=r"G:\AFMOD\.dotnet-sdk\dotnet.exe")
    args = parser.parse_args()

    source = SOURCE.read_text(encoding="utf-8-sig")
    failed = 0
    for name, (old, new, expected_case) in MUTATIONS.items():
        if source.count(old) != 1:
            raise RuntimeError(f"mutation anchor count for {name}: {source.count(old)}")

        output = HERE / ".generated" / name
        output.mkdir(parents=True, exist_ok=True)
        (output / "SceneShoutConversationScope.cs").write_text(source.replace(old, new), encoding="utf-8")
        (output / "Program.cs").write_text((HERE / "Program.cs").read_text(encoding="utf-8-sig"), encoding="utf-8")
        (output / "Stubs.cs").write_text((HERE / "Stubs.cs").read_text(encoding="utf-8-sig"), encoding="utf-8")
        (output / "Tests.csproj").write_text(
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup>"
            "<OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>"
            "<ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable>"
            "<LangVersion>latest</LangVersion></PropertyGroup></Project>",
            encoding="utf-8",
        )
        (output / "NuGet.Config").write_text(
            "<configuration><packageSources><clear/></packageSources></configuration>", encoding="utf-8"
        )

        environment = os.environ.copy()
        environment["DOTNET_ROOT"] = str(Path(args.dotnet).parent)
        environment["DOTNET_CLI_HOME"] = str(output / "cli")
        environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
        environment["DOTNET_NOLOGO"] = "1"
        result = subprocess.run(
            [args.dotnet, "run", "--project", str(output / "Tests.csproj"), "-c", "Release"],
            cwd=output,
            env=environment,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            timeout=90,
        )
        log = result.stdout + result.stderr
        (output / "run.log").write_text(log, encoding="utf-8")
        rejected = result.returncode == 1 and f"FAIL {expected_case}:" in log and "error CS" not in log
        print(("PASS" if rejected else "FAIL") + f" mutation {name} expected={expected_case}")
        if not rejected:
            failed += 1
            print(log)

    print(f"SceneConversationScope mutations={len(MUTATIONS)} FAIL={failed}")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
