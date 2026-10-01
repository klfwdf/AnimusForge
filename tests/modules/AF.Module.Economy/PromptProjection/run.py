"""Run the production economy prompt projection and one compiled behavior mutation."""
from __future__ import annotations

import argparse
import importlib.util
import os
import sys
from pathlib import Path
import subprocess


ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import minimal_test_environment, new_run_root, resolve_dotnet
PROJECTION = ROOT / "src/modules/AF.Module.Economy/Projection/EconomyPromptProjection.cs"
TRUST_POLICY = ROOT / "src/modules/AF.Module.Economy/Trust/EconomyTrustPolicy.cs"
TRUST_STATE = ROOT / "src/modules/AF.Module.Economy/Trust/RewardSystemBehavior.TrustState.cs"
DEBT_LEDGER = ROOT / "src/modules/AF.Module.Economy/Debt/RewardSystemBehavior.DebtLedger.cs"


def load_declaration():
    path = ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py"
    spec = importlib.util.spec_from_file_location("economy_projection_decl", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module.declaration


def verify_live_wiring() -> None:
    declaration = load_declaration()
    reward = (ROOT / "src/modules/AF.Module.Economy/Host/RewardSystemBehavior.cs").read_text(encoding="utf-8-sig")
    debt_ledger = DEBT_LEDGER.read_text(encoding="utf-8-sig")
    trust_state = TRUST_STATE.read_text(encoding="utf-8-sig")
    production = reward + "\n" + debt_ledger + "\n" + trust_state
    expected = {
        "public string BuildTrustStatusInlineForAI(": ("EconomyPromptProjection.BuildTrustStatus(", 2),
        "public string BuildTrustPromptForAI(": ("EconomyPromptProjection.BuildTrustPrompt(", 1),
        "public string BuildDebtHintForAI(": ("EconomyPromptProjection.BuildHeroDebtHint(", 1),
        "public string BuildSettlementMerchantDebtHintForAI(": ("EconomyPromptProjection.BuildSettlementMerchantDebtHint(", 1),
    }
    for marker, (call, count) in expected.items():
        method = declaration(production, marker)
        assert method.count(call) == count, f"Production consumer delegation count drifted: {marker}"
    for marker in ("public string BuildDebtHintForAI(", "public string BuildSettlementMerchantDebtHintForAI("):
        method = declaration(production, marker)
        assert method.index("NormalizeDebtRecord(") < method.index("EconomyPromptProjection.Build"), \
            f"Debt normalization moved behind detached projection: {marker}"
        assert "new EconomyDebtPromptLine(" in method, f"Live debt values were not detached: {marker}"
    trust_calls = {
        "private static int ClampTrust(": "EconomyTrustPolicy.Clamp(",
        "public static int GetTrustLevelIndex(": "EconomyTrustPolicy.GetLevelIndex(",
        "public static string GetTrustLevelText(": "EconomyTrustPolicy.GetLevelText(",
        "public static string GetTrustBehaviorText(": "EconomyTrustPolicy.GetBehaviorText(",
        "public static string GetTrustActionGuideText(": "EconomyTrustPolicy.GetActionGuideText(",
    }
    for marker, call in trust_calls.items():
        assert declaration(production, marker).count(call) == 1, f"Trust policy wiring drifted: {marker}"
    print("PASS production capture -> detached projection wiring")


def run(dotnet: str, project: Path, output: Path) -> tuple[int, str]:
    build_root = output / ("build-" + project.parent.name + "-" + project.stem)
    build_root.mkdir(exist_ok=False)
    env = minimal_test_environment(Path(dotnet), build_root)
    command = [dotnet, "build", str(project), "-c", "Release",
               "-p:NuGetAudit=false", "-p:RestoreConfigFile=" + str(output / "NuGet.Config"),
               "-p:BaseOutputPath=" + str(build_root / "bin") + "/",
               "-p:BaseIntermediateOutputPath=" + str(build_root / "obj") + "/"]
    result = subprocess.run(command, cwd=ROOT, env=env, capture_output=True, text=True,
                            encoding="utf-8", errors="replace")
    log = result.stdout + result.stderr
    if result.returncode != 0:
        return result.returncode, log
    executable = build_root / "bin/Release/net8.0" / (project.stem + ".dll")
    assert executable.is_file(), "Fresh build did not produce runtime DLL: " + str(executable)
    result = subprocess.run([dotnet, str(executable)], cwd=ROOT, env=env,
                            capture_output=True, text=True, encoding="utf-8", errors="replace")
    return result.returncode, log + result.stdout + result.stderr


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet")
    parser.add_argument("--run-root", type=Path)
    parser.add_argument("--skip-mutation", action="store_true")
    args = parser.parse_args()
    args.dotnet = str(resolve_dotnet(ROOT, args.dotnet))
    verify_live_wiring()

    output = new_run_root(ROOT, "economy-prompt-projection", args.run_root)
    (output / "NuGet.Config").write_text(
        "<configuration><packageSources><clear /></packageSources></configuration>", encoding="utf-8")
    code, log = run(args.dotnet, HERE / "PromptProjectionTests.csproj", output)
    (output / "current.log").write_text(log, encoding="utf-8")
    print(log, end="")
    assert code == 0, "Current economy prompt projection failed"

    if not args.skip_mutation:
        projection = PROJECTION.read_text(encoding="utf-8-sig")
        trust = TRUST_POLICY.read_text(encoding="utf-8-sig")
        mutations = (
            ("merchant_confirmation", "projection",
             "【债务解除确认】若玩家本轮行为已被系统事实明确记录为偿还、豁免或免除",
             "【债务确认】已处理", "FAIL merchant confirmation"),
            ("trust_rounding", "trust", "Math.Floor(normalized * 10.0)",
             "Math.Ceiling(normalized * 10.0)", "FAIL trust level boundaries"),
        )
        for name, target, old, new, failure in mutations:
            mutant = output / ("mutant-" + name)
            mutant.mkdir(exist_ok=True)
            mutated_projection = projection.replace(old, new, 1) if target == "projection" else projection
            mutated_trust = trust.replace(old, new, 1) if target == "trust" else trust
            assert (projection if target == "projection" else trust).count(old) == 1, "Mutation anchor drifted: " + name
            (mutant / "EconomyPromptProjection.cs").write_text(mutated_projection, encoding="utf-8")
            (mutant / "EconomyTrustPolicy.cs").write_text(mutated_trust, encoding="utf-8")
            project = mutant / "Mutant.csproj"
            project.write_text(
                '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
                '<TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems>'
                '<Nullable>disable</Nullable></PropertyGroup><ItemGroup>'
                f'<Compile Include="{HERE / "Program.cs"}" Link="Program.cs" />'
                '<Compile Include="EconomyPromptProjection.cs" /><Compile Include="EconomyTrustPolicy.cs" />'
                '</ItemGroup></Project>', encoding="utf-8")
            code, log = run(args.dotnet, project, output)
            (mutant / "run.log").write_text(log, encoding="utf-8")
            assert code != 0 and failure in log and "error CS" not in log, \
                name + " mutation did not reach the expected runtime assertion\n" + log
            print("PASS behavioral mutation rejected: " + name)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
