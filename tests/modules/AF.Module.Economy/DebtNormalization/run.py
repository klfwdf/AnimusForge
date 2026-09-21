"""Compile the production debt normalizer with its real private ledger schema."""
from __future__ import annotations

import argparse
import importlib.util
import os
from pathlib import Path
import subprocess


ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
REWARD = ROOT / "RewardSystemBehavior.cs"
POLICY = ROOT / "src/modules/AF.Module.Economy/Debt/RewardSystemBehavior.DebtNormalizationPolicy.cs"


def load_declaration():
    path = ROOT / "tools/ChannelCutoverBoundaryTests/run.py"
    spec = importlib.util.spec_from_file_location("debt_normalization_decl", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module.declaration


def prepare_generated(policy_text: str | None = None) -> Path:
    declaration = load_declaration()
    reward = REWARD.read_text(encoding="utf-8-sig")
    debt_record = declaration(reward, "private class DebtRecord")
    wrapper = declaration(reward, "private void NormalizeDebtRecord(")
    has_content = declaration(reward, "private static bool HasDebtContent(")
    note = declaration(reward, "private static string NormalizeDebtNote(")
    assert wrapper.count("EconomyDebtNormalizationPolicy.Normalize(") == 1, "Normalize wrapper drifted"
    assert "GetNowCampaignDay()" in wrapper and "BuildDebtId" in wrapper, "Live capture/ID owner drifted"
    assert has_content.count("EconomyDebtNormalizationPolicy.HasContent(") == 1, "Content wrapper drifted"
    assert note.count("EconomyDebtNormalizationPolicy.NormalizeNote(") == 1, "Note wrapper drifted"

    generated = HERE / ".generated"
    generated.mkdir(parents=True, exist_ok=True)
    (generated / "NuGet.Config").write_text(
        "<configuration><packageSources><clear /></packageSources></configuration>", encoding="utf-8")
    (generated / "ProductionDebtRecord.cs").write_text(
        "using System.Collections.Generic;\n\nnamespace AnimusForge;\n\n"
        "public partial class RewardSystemBehavior\n{\n"
        "    private const int UnlimitedDebtPenaltyReferenceValue = 100000;\n\n"
        + debt_record + "\n}\n",
        encoding="utf-8")
    if policy_text is not None:
        (generated / "MutantDebtNormalizationPolicy.cs").write_text(policy_text, encoding="utf-8")
    print("PASS production debt schema and live wrappers linked")
    return generated


def run(dotnet: str, project: Path, generated: Path) -> tuple[int, str]:
    env = dict(os.environ)
    env["DOTNET_CLI_HOME"] = str(ROOT / ".dotnet-cli-home")
    env["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
    env["DOTNET_NOLOGO"] = "1"
    command = [dotnet, "run", "--project", str(project), "-c", "Release",
               "-p:NuGetAudit=false", "-p:RestoreConfigFile=" + str(generated / "NuGet.Config")]
    result = subprocess.run(command, cwd=ROOT, env=env, capture_output=True, text=True,
                            encoding="utf-8", errors="replace")
    return result.returncode, result.stdout + result.stderr


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", default=os.environ.get("DOTNET_EXE", r"G:\AFMOD\.dotnet-sdk\dotnet.exe"))
    parser.add_argument("--skip-mutation", action="store_true")
    args = parser.parse_args()

    generated = prepare_generated()
    code, log = run(args.dotnet, HERE / "DebtNormalizationTests.csproj", generated)
    (generated / "current.log").write_text(log, encoding="utf-8")
    print(log, end="")
    assert code == 0, "Current production debt normalization failed"

    if not args.skip_mutation:
        policy = POLICY.read_text(encoding="utf-8-sig")
        old = "if (line.IsDueUnlimited)"
        new = "if (false && line.IsDueUnlimited)"
        assert policy.count(old) == 1, "Unlimited due mutation anchor drifted"
        mutant_policy = policy.replace(old, new, 1)
        generated = prepare_generated(mutant_policy)
        project = generated / "Mutant.csproj"
        project.write_text(
            '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
            '<TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems>'
            '<ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup><ItemGroup>'
            f'<Compile Include="{HERE / "Program.cs"}" Link="Program.cs" />'
            '<Compile Include="ProductionDebtRecord.cs" />'
            '<Compile Include="MutantDebtNormalizationPolicy.cs" />'
            '</ItemGroup></Project>', encoding="utf-8")
        code, log = run(args.dotnet, project, generated)
        (generated / "mutant.log").write_text(log, encoding="utf-8")
        assert code != 0 and "FAIL unlimited due and amount" in log and "error CS" not in log, \
            "Unlimited due mutation did not reach its named runtime assertion\n" + log
        print("PASS behavioral mutation rejected: unlimited_due")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
