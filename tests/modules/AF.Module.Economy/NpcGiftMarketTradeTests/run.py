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
    "private static int RemoveGeneratedRewardItemsFromSettlementMarket(",
    "private static int RemoveGeneratedRewardItemsFromMarketRosters(",
    "private static void InventoryScreenHelperOpenScreenAsTradePrefix(",
    "private void OnDailyTickSettlement(",
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
    parser.add_argument("--mutate", choices=["old-guard", "old-cleanup"])
    args = parser.parse_args()
    source = (ROOT / "src/modules/AF.Module.Economy/Host/RewardSystemBehavior.cs").read_text(encoding="utf-8-sig")
    methods = {signature: extractor.declaration(source, signature) for signature in SIGNATURES}
    gift = extractor.declaration(source, "private int GenerateRpAssetToPlayer(")
    assert gift.index("giftRecord.NpcGiftMarketTradeAllowed = true;") < gift.index("QueueNpcRpItemIntroductionForExternal(")
    assert "NpcGiftMarketTradeAllowed |= existing.NpcGiftMarketTradeAllowed" in source
    prime = extractor.declaration(source, "public static bool TryPrimeGeneratedInventoryItemForExternal(")
    assert "NpcGiftMarketTradeAllowed = existingRecord?.NpcGiftMarketTradeAllowed ?? false" in prime
    if args.mutate == "old-guard":
        methods["private static bool IsGeneratedRewardMarketTransferBlockedItem("] = (
            "private static bool IsGeneratedRewardMarketTransferBlockedItem(ItemObject item) "
            "{ return IsGeneratedRewardMarketExcludedItem(item); }")
    if args.mutate == "old-cleanup":
        signature = "private static int RemoveGeneratedRewardItemsFromRoster("
        methods[signature] = methods[signature].replace(
            "IsGeneratedRewardMarketTransferBlockedItem(element.EquipmentElement.Item)",
            "IsGeneratedRewardMarketExcludedItem(element.EquipmentElement.Item)")
    output = new_run_root(ROOT, "npc-gift-market", args.run_root)
    fixture = (HERE / "Fixture.cs.in").read_text(encoding="utf-8-sig")
    (output / "Program.cs").write_text(fixture.replace("__PRODUCTION__", "\n\n".join(methods.values())), encoding="utf-8")
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
    result = subprocess.run([str(dotnet), "run", "--project", str(output / "MarketTests.csproj")],
        cwd=output, env=minimal_test_environment(dotnet, output), capture_output=True,
        text=True, encoding="utf-8", errors="replace", timeout=180)
    log = "sourceSha256=" + hashlib.sha256(source.encode()).hexdigest() + " mutation=" + str(args.mutate) + "\n" + result.stdout + result.stderr
    (output / "run.log").write_text(log, encoding="utf-8")
    print(log)
    return result.returncode


if __name__ == "__main__":
    raise SystemExit(main())
