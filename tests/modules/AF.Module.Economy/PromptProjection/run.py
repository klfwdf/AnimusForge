"""Run the production economy prompt projection and one compiled behavior mutation."""
from __future__ import annotations

import argparse
import importlib.util
import os
from pathlib import Path
import subprocess


ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
PRODUCTION = ROOT / "src/modules/AF.Module.Economy/Projection/EconomyPromptProjection.cs"


def load_declaration():
    path = ROOT / "tools/ChannelCutoverBoundaryTests/run.py"
    spec = importlib.util.spec_from_file_location("economy_projection_decl", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module.declaration


def verify_live_wiring() -> None:
    declaration = load_declaration()
    reward = (ROOT / "RewardSystemBehavior.cs").read_text(encoding="utf-8-sig")
    expected = {
        "public string BuildTrustStatusInlineForAI(": ("EconomyPromptProjection.BuildTrustStatus(", 2),
        "public string BuildTrustPromptForAI(": ("EconomyPromptProjection.BuildTrustPrompt(", 1),
        "public string BuildDebtHintForAI(": ("EconomyPromptProjection.BuildHeroDebtHint(", 1),
        "public string BuildSettlementMerchantDebtHintForAI(": ("EconomyPromptProjection.BuildSettlementMerchantDebtHint(", 1),
    }
    for marker, (call, count) in expected.items():
        method = declaration(reward, marker)
        assert method.count(call) == count, f"Production consumer delegation count drifted: {marker}"
    for marker in ("public string BuildDebtHintForAI(", "public string BuildSettlementMerchantDebtHintForAI("):
        method = declaration(reward, marker)
        assert method.index("NormalizeDebtRecord(") < method.index("EconomyPromptProjection.Build"), \
            f"Debt normalization moved behind detached projection: {marker}"
        assert "new EconomyDebtPromptLine(" in method, f"Live debt values were not detached: {marker}"
    print("PASS production capture -> detached projection wiring")


def run(dotnet: str, project: Path, output: Path) -> tuple[int, str]:
    env = dict(os.environ)
    env["DOTNET_CLI_HOME"] = str(ROOT / ".dotnet-cli-home")
    env["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
    env["DOTNET_NOLOGO"] = "1"
    command = [dotnet, "run", "--project", str(project), "-c", "Release",
               "-p:NuGetAudit=false", "-p:RestoreConfigFile=" + str(output / "NuGet.Config")]
    result = subprocess.run(command, cwd=ROOT, env=env, capture_output=True, text=True,
                            encoding="utf-8", errors="replace")
    return result.returncode, result.stdout + result.stderr


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", default=os.environ.get("DOTNET_EXE", r"G:\AFMOD\.dotnet-sdk\dotnet.exe"))
    parser.add_argument("--skip-mutation", action="store_true")
    args = parser.parse_args()
    verify_live_wiring()

    output = HERE / ".generated"
    output.mkdir(parents=True, exist_ok=True)
    (output / "NuGet.Config").write_text(
        "<configuration><packageSources><clear /></packageSources></configuration>", encoding="utf-8")
    code, log = run(args.dotnet, HERE / "PromptProjectionTests.csproj", output)
    (output / "current.log").write_text(log, encoding="utf-8")
    print(log, end="")
    assert code == 0, "Current economy prompt projection failed"

    if not args.skip_mutation:
        mutant = output / "mutant"
        mutant.mkdir(exist_ok=True)
        source = PRODUCTION.read_text(encoding="utf-8-sig")
        old = "【债务解除确认】若玩家本轮行为已被系统事实明确记录为偿还、豁免或免除"
        assert source.count(old) == 1, "Mutation anchor drifted"
        (mutant / "EconomyPromptProjection.cs").write_text(source.replace(old, "【债务确认】已处理", 1), encoding="utf-8")
        project = mutant / "Mutant.csproj"
        project.write_text(
            '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
            '<TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems>'
            '<Nullable>disable</Nullable></PropertyGroup><ItemGroup>'
            f'<Compile Include="{HERE / "Program.cs"}" Link="Program.cs" />'
            '<Compile Include="EconomyPromptProjection.cs" /></ItemGroup></Project>', encoding="utf-8")
        code, log = run(args.dotnet, project, output)
        (mutant / "run.log").write_text(log, encoding="utf-8")
        assert code != 0 and "FAIL merchant confirmation" in log and "error CS" not in log, \
            "Merchant confirmation mutation did not reach the expected runtime assertion\n" + log
        print("PASS behavioral mutation rejected: merchant_confirmation")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
