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
LEDGER = ROOT / "src/modules/AF.Module.Economy/Debt/RewardSystemBehavior.DebtLedger.cs"
DAILY_LIFECYCLE = ROOT / "src/modules/AF.Module.Economy/Debt/RewardSystemBehavior.DailyEconomyLifecycle.cs"
POLICY = ROOT / "src/modules/AF.Module.Economy/Debt/RewardSystemBehavior.DebtNormalizationPolicy.cs"
SCHEDULE_POLICY = ROOT / "src/modules/AF.Module.Economy/Debt/RewardSystemBehavior.DebtSchedulePolicy.cs"
QUEST_LIFECYCLE = ROOT / "src/modules/AF.Module.Economy/Debt/RewardSystemBehavior.DebtPromiseLifecycle.cs"


def load_declaration():
    path = ROOT / "tools/ChannelCutoverBoundaryTests/run.py"
    spec = importlib.util.spec_from_file_location("debt_normalization_decl", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module.declaration


def prepare_generated() -> Path:
    declaration = load_declaration()
    reward = REWARD.read_text(encoding="utf-8-sig")
    ledger = LEDGER.read_text(encoding="utf-8-sig")
    daily = DAILY_LIFECYCLE.read_text(encoding="utf-8-sig")
    debt_record = declaration(ledger, "private class DebtRecord")
    wrapper = declaration(ledger, "private void NormalizeDebtRecord(")
    has_content = declaration(reward, "private static bool HasDebtContent(")
    note = declaration(ledger, "private static string NormalizeDebtNote(")
    assert wrapper.count("EconomyDebtNormalizationPolicy.Normalize(") == 1, "Normalize wrapper drifted"
    assert "GetNowCampaignDay()" in wrapper and "BuildDebtId" in wrapper, "Live capture/ID owner drifted"
    assert has_content.count("EconomyDebtNormalizationPolicy.HasContent(") == 1, "Content wrapper drifted"
    assert note.count("EconomyDebtNormalizationPolicy.NormalizeNote(") == 1, "Note wrapper drifted"
    schedule_calls = {
        "EconomyDebtSchedulePolicy.NormalizeDueDays(": 2,
        "EconomyDebtSchedulePolicy.ComputeWeeklyOverdueTrustPenaltyByDebtValue(": 2,
        "EconomyDebtSchedulePolicy.ConsumeUnlimitedDebtTrustPenaltyUnits(": 2,
        "EconomyDebtSchedulePolicy.ShouldIncludeDebtLineInScheduledReminder(": 2,
        "EconomyDebtSchedulePolicy.ComputeWeeklyOverdueRelationPenaltyDelta(": 1,
        "EconomyDebtSchedulePolicy.ComputeOverdueElapsedWeeks(": 2,
    }
    debt_consumers = ledger + daily
    for call, count in schedule_calls.items():
        assert debt_consumers.count(call) == count, "Debt schedule production wiring drifted: " + call
    removed_schedule_declarations = (
        "private static int NormalizeDueDays(",
        "private static int ComputeWeeklyOverdueTrustPenaltyByDebtValue(",
        "private static int ConsumeUnlimitedDebtTrustPenaltyUnits(",
        "private static bool ShouldIncludeDebtLineInScheduledReminder(",
        "private static int ComputeWeeklyOverdueRelationPenaltyTotal(",
        "private static int ComputeWeeklyOverdueRelationPenaltyDelta(",
        "private static int ComputeOverdueElapsedWeeks(",
    )
    for marker in removed_schedule_declarations:
        assert marker not in reward, "Old debt schedule declaration remains in root: " + marker

    ledger_markers = (
        "public class DebtExportEntry",
        "public class DebtLineExportEntry",
        "private class DebtRecord",
        "private void NormalizeDebtRecord(",
        "public Dictionary<string, DebtExportEntry> ExportDebtEntries()",
        "public void ImportDebtEntries(",
        "public string BuildDebtHintForAI(",
        "public void SetDebt(",
        "private DebtRecord.DebtLine SetDebtForNpc(",
        "private DebtRecord.DebtLine SetDebtForSettlementMerchant(",
        "public bool ResolveDebtByIdByAgreement(",
        "public bool ResolveSettlementMerchantDebtByIdByAgreement(",
    )
    for marker in ledger_markers:
        assert ledger.count(marker) == 1, "Debt ledger owner declaration drifted: " + marker
        assert marker not in reward, "Debt ledger declaration remains in root: " + marker
    assert ledger.count("private Dictionary<string, DebtRecord> _debts") == 1
    assert "private Dictionary<string, DebtRecord> _debts" not in reward
    assert daily.count("private void OnDailyTick()") == 1 and "private void OnDailyTick()" not in reward
    assert reward.count("DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);") == 1, \
        "Daily economy lifecycle registration drifted"

    quest = QUEST_LIFECYCLE.read_text(encoding="utf-8-sig")
    quest_markers = (
        "private static string BuildDebtId()",
        "private void QueueDebtPromiseQuest(",
        "private void QueueDebtPromiseQuestsForActiveDebts()",
        "private void DrainPendingDebtPromiseQuestCreations()",
        "private static bool CanStartDebtPromiseQuest()",
        "private static bool TryParseDebtPromiseQuestKey(",
        "private bool TryGetActiveDebtPromiseQuestData(",
        "private static string BuildDebtPromiseSummary(",
        "private void EnsureDebtPromiseQuest(",
        "private void CompleteDebtPromiseQuest(",
    )
    for marker in quest_markers:
        assert quest.count(marker) == 1, "Debt quest owner declaration drifted: " + marker
        assert marker not in reward, "Debt quest declaration remains in root: " + marker
    pending_field = "private HashSet<string> _pendingDebtPromiseQuestKeys"
    assert quest.count(pending_field) == 1 and pending_field not in reward, "Debt quest queue state owner drifted"
    all_debt_consumers = reward + ledger + daily
    assert all_debt_consumers.count("QueueDebtPromiseQuestsForActiveDebts();") == 2, \
        "Load/import quest reconciliation drifted"
    assert all_debt_consumers.count("DrainPendingDebtPromiseQuestCreations();") == 1, \
        "Quest drain tick wiring drifted"
    assert ledger.count("CompleteDebtPromiseQuest(") == 2, "Debt resolution quest completion drifted"

    generated = HERE / ".generated"
    generated.mkdir(parents=True, exist_ok=True)
    (generated / "NuGet.Config").write_text(
        "<configuration><packageSources><clear /></packageSources></configuration>", encoding="utf-8")
    (generated / "ProductionDebtRecord.cs").write_text(
        "using System.Collections.Generic;\n\nnamespace AnimusForge;\n\n"
        "public partial class RewardSystemBehavior\n{\n"
        "    private const int UnlimitedDebtPenaltyReferenceValue = 100000;\n\n"
        "    private const int UnlimitedDebtReminderIntervalDays = 7;\n"
        "    private const int UnlimitedDebtPenaltyTrustUnitsPerReferencePerDay = 16;\n"
        "    private const int OverduePenaltyIntervalDays = 7;\n"
        "    private const int OverduePenaltyMaxWeeks = 12;\n"
        "    private const int OverdueTrustPenaltyPerWeekValueStep = 10000;\n"
        "    private const int OverdueRelationPenaltyPerWeekTrustStep = 5;\n\n"
        + debt_record + "\n}\n",
        encoding="utf-8")
    print("PASS production debt schema, schedule, quest owner, and live wrappers linked")
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
        normalization = POLICY.read_text(encoding="utf-8-sig")
        schedule = SCHEDULE_POLICY.read_text(encoding="utf-8-sig")
        mutations = (
            ("unlimited_due", "normalization", "if (line.IsDueUnlimited)",
             "if (false && line.IsDueUnlimited)", "FAIL unlimited due and amount"),
            ("reminder_cadence", "schedule", "elapsedDays % UnlimitedDebtReminderIntervalDays == 0",
             "elapsedDays % UnlimitedDebtReminderIntervalDays != 0", "FAIL scheduled reminder cadence"),
        )
        for name, target, old, new, failure in mutations:
            source = normalization if target == "normalization" else schedule
            assert source.count(old) == 1, "Mutation anchor drifted: " + name
            mutant = generated / ("mutant-" + name)
            mutant.mkdir(exist_ok=True)
            (mutant / "ProductionDebtRecord.cs").write_text(
                (generated / "ProductionDebtRecord.cs").read_text(encoding="utf-8"), encoding="utf-8")
            (mutant / "DebtNormalizationPolicy.cs").write_text(
                normalization.replace(old, new, 1) if target == "normalization" else normalization,
                encoding="utf-8")
            (mutant / "DebtSchedulePolicy.cs").write_text(
                schedule.replace(old, new, 1) if target == "schedule" else schedule,
                encoding="utf-8")
            project = mutant / "Mutant.csproj"
            project.write_text(
                '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
                '<TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems>'
                '<ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup><ItemGroup>'
                f'<Compile Include="{HERE / "Program.cs"}" Link="Program.cs" />'
                '<Compile Include="ProductionDebtRecord.cs" />'
                '<Compile Include="DebtNormalizationPolicy.cs" />'
                '<Compile Include="DebtSchedulePolicy.cs" />'
                '</ItemGroup></Project>', encoding="utf-8")
            code, log = run(args.dotnet, project, generated)
            (mutant / "run.log").write_text(log, encoding="utf-8")
            assert code != 0 and failure in log and "error CS" not in log, \
                name + " mutation did not reach its named runtime assertion\n" + log
            print("PASS behavioral mutation rejected: " + name)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
