"""Run production market guards and roster persistence against engine boundary doubles."""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
from pathlib import Path
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, minimal_test_environment, resolve_dotnet

spec = importlib.util.spec_from_file_location(
    "market_extractor", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extractor = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extractor)

SIGNATURES = [
    "private sealed class GeneratedRewardItemRecord",
    "private sealed class GeneratedRewardRosterItemRecord",
    "private static string StablePromptKeyHash(",
    "private static string BuildGeneratedRewardItemStringId(",
    "private static bool IsGeneratedRewardItemStringId(",
    "private static bool IsGeneratedRewardPendingItem(",
    "private static bool IsGeneratedRewardMarketExcludedItem(",
    "private static bool IsGeneratedRewardMarketTransferBlockedItem(",
    "private static void RestoreLegacyNpcGiftMarketTradePermission(",
    "private static GeneratedRewardItemRecord NormalizeGeneratedRewardItemRecord(",
    "private static void RegisterGeneratedRewardManifestRecordNoLock(",
    "private static bool ShouldBlockGeneratedRewardMarketTransfer(",
    "private static bool InventoryLogicAddTransferCommandPrefix(",
    "private static bool InventoryLogicAddTransferCommandsPrefix(",
    "private static bool SellItemsActionApplyPrefix(",
    "private static List<ItemRosterElement> TakeGeneratedRewardItemsFromRoster(",
    "private static void RestoreGeneratedRewardItemsToRoster(",
    "private static int RemoveGeneratedRewardItemsFromRoster(",
    "private static string BuildGeneratedRewardMarketRosterLabel(",
    "private static void InventoryScreenHelperOpenScreenAsTradePrefix(",
    "public void OnEngineTick(",
    "private static MBReadOnlyList<ItemObject> GetGeneratedRewardFilteredEconomicPool(",
    "private static bool IsEligibleNormalWorkshopOutput(",
    "private static bool IsStableGeneratedRewardTemplateIdentity(",
    "private GeneratedRewardRosterItemRecord NormalizeGeneratedRewardRosterItemRecord(",
    "private void CaptureGeneratedRewardPlayerRosterItems(",
    "private static int CountGeneratedRewardRosterItem(",
    "private void RestoreGeneratedRewardPlayerRosterItems(",
]


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--run-root", type=Path)
    parser.add_argument("--dotnet")
    parser.add_argument("--daily-source-ref", help="Replay the daily entry prefix from a reviewed Git revision")
    parser.add_argument("--mutate", choices=["old-guard", "old-cleanup", "all-markets", "restart-round", "ignore-generation"])
    args = parser.parse_args()
    source = (ROOT / "src/modules/AF.Module.Economy/Host/RewardSystemBehavior.cs").read_text(encoding="utf-8-sig")
    methods = {signature: extractor.declaration(source, signature) for signature in SIGNATURES}
    assert "DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailyTickSettlement)" not in source
    assert "InvalidateGeneratedRewardMarketRules" not in source
    load_entry = extractor.declaration(source, "private void OnGameLoadFinished(")
    assert load_entry.count('QueueGeneratedRewardWorldMarketCleanup("game_load_finished");') == 1
    assert methods["public void OnEngineTick("].count("ProcessGeneratedRewardMarketCleanup();") == 1
    daily_path = "src/modules/AF.Module.Economy/Debt/RewardSystemBehavior.DailyEconomyLifecycle.cs"
    daily_source = (subprocess.check_output(["git", "show", args.daily_source_ref + ":" + daily_path], cwd=ROOT).decode("utf-8-sig")
                    if args.daily_source_ref else (ROOT / daily_path).read_text(encoding="utf-8-sig"))
    # Only the real entry prefix before debt processing; debts/equipment restoration are leaf doubles.
    entry = extractor.declaration(daily_source, "private void OnDailyTick(")
    entry = entry[:entry.index("if (_debts == null")]
    methods["daily_entry_prefix"] = entry.replace("OnDailyTick()", "RunDailyEntryPrefix()") + "} catch { throw; } }"
    if "RemoveGeneratedRewardItemsFromMarketRosters" in entry:
        old_host = subprocess.check_output(["git", "show", args.daily_source_ref + ":src/modules/AF.Module.Economy/Host/RewardSystemBehavior.cs"], cwd=ROOT).decode("utf-8-sig")
        for signature in ["private static int RemoveGeneratedRewardItemsFromSettlementMarket(", "private static int RemoveGeneratedRewardItemsFromMarketRosters("]:
            methods[signature] = extractor.declaration(old_host, signature)
    gift = extractor.declaration(source, "private int GenerateRpAssetToPlayer(")
    assert gift.index("giftRecord.NpcGiftMarketTradeAllowed = true;") < gift.index("QueueNpcRpItemIntroductionForExternal(")
    assert "NpcGiftMarketTradeAllowed |= existing.NpcGiftMarketTradeAllowed" in source
    prime = extractor.declaration(source, "public static bool TryPrimeGeneratedInventoryItemForExternal(")
    assert "NpcGiftMarketTradeAllowed = existingRecord?.NpcGiftMarketTradeAllowed ?? false" in prime
    if args.mutate == "old-guard":
        methods["private static bool IsGeneratedRewardMarketTransferBlockedItem("] = (
            "private static bool IsGeneratedRewardMarketTransferBlockedItem(ItemObject item) "
            "{ return IsGeneratedRewardMarketExcludedItem(item); }")
    cleanup_host = (ROOT / "src/modules/AF.Module.Economy/Host/RewardSystemBehavior.MarketCleanup.cs").read_text(encoding="utf-8-sig")
    if args.mutate == "old-cleanup":
        cleanup_host = cleanup_host.replace("IsGeneratedRewardMarketExcludedItem, IsGeneratedRewardMarketTransferBlockedItem,",
                                            "IsGeneratedRewardMarketExcludedItem, IsGeneratedRewardMarketExcludedItem,")
    output = new_run_root(ROOT, "npc-gift-market", args.run_root)
    fixture = (HERE / "Fixture.cs.in").read_text(encoding="utf-8-sig")
    (output / "Program.cs").write_text(fixture.replace("__PRODUCTION__", "\n\n".join(methods.values())), encoding="utf-8")
    (output / "MarketCleanupHost.cs").write_text(cleanup_host, encoding="utf-8")
    cleanup_owner = (ROOT / "src/modules/AF.Module.Economy/Execution/GeneratedRewardMarketCleanupOwner.cs").read_text(encoding="utf-8-sig")
    mutations = {
        "all-markets": [('if (_nextMarket < _markets.Count)\n        {', 'while (_nextMarket < _markets.Count)\n        {')],
        "restart-round": [('if (HasWork) { _rerun = true; return; }', 'if (HasWork) Reset();')],
        "ignore-generation": [('if (_boundGeneration != _generation()) { Reset(); return; }', '/* fault: accept old world */'),
                              ('if (_boundGeneration != _generation()) Reset();', '/* fault: keep old generation */')],
    }
    for old, new in mutations.get(args.mutate, []):
        assert old in cleanup_owner, old
        cleanup_owner = cleanup_owner.replace(old, new)
    (output / "MarketCleanupOwner.cs").write_text(cleanup_owner, encoding="utf-8")
    (output / "SaveRuntimeGuard.cs").write_bytes((ROOT / "src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs").read_bytes())
    (output / "MarketCleanupCases.cs").write_bytes((HERE / "MarketCleanupCases.cs.in").read_bytes())
    (output / "EngineNamespaces.cs").write_text("namespace AnimusForge.Refactor.Runtime {} namespace TaleWorlds.CampaignSystem.GameState {} namespace TaleWorlds.CampaignSystem.Roster {} namespace TaleWorlds.CampaignSystem.Settlements {} namespace TaleWorlds.Core {} namespace TaleWorlds.Library {}", encoding="utf-8")
    dotnet = resolve_dotnet(ROOT, args.dotnet)
    json_dll = ROOT / "local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll"
    if not json_dll.is_file():
        raise SystemExit("Missing local Newtonsoft.Json reference")
    project = ET.Element("Project", Sdk="Microsoft.NET.Sdk")
    props = ET.SubElement(project, "PropertyGroup")
    for name, value in [("OutputType", "Exe"), ("TargetFramework", "net8.0"), ("ImplicitUsings", "enable"), ("Nullable", "disable")]:
        ET.SubElement(props, name).text = value
    refs = ET.SubElement(project, "ItemGroup")
    ref = ET.SubElement(refs, "Reference", Include="Newtonsoft.Json")
    ET.SubElement(ref, "HintPath").text = str(json_dll)
    ET.ElementTree(project).write(output / "MarketTests.csproj", encoding="utf-8")
    (output / "NuGet.Config").write_text("<configuration><packageSources><clear/></packageSources></configuration>", encoding="utf-8")
    result = subprocess.run([str(dotnet), "run", "--project", str(output / "MarketTests.csproj"), "-c", "Release"],
        cwd=output, env=minimal_test_environment(dotnet, output), capture_output=True,
        text=True, encoding="utf-8", errors="replace", timeout=180)
    log = "sourceSha256=" + hashlib.sha256(source.encode()).hexdigest() + " dailySource=" + (args.daily_source_ref or "working-tree") + " mutation=" + str(args.mutate) + "\n" + result.stdout + result.stderr
    (output / "run.log").write_text(log, encoding="utf-8")
    print(log)
    return result.returncode


if __name__ == "__main__":
    raise SystemExit(main())
