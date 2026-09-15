"""Execute extracted production memory writer facades and their queue/copy helpers.

Game types and terminal writers are fixtures; this proves dispatch and DTO ownership,
not dialogue storage, game/provider behavior, or the unreachable legacy Scene fallback.
Generated source, exact extraction inventory, build and execution logs stay ignored.
"""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
FACADES = [
    "public static void AppendExternalDialogueHistory(",
    "public static void AppendExternalSceneDialogueHistory(",
    "public static void AppendExternalNonHeroDialogueHistory(",
    "public static void AppendExternalNonHeroSceneDialogueHistory(",
    "public static void RecordNpcActionForExternal(",
    "public static void MarkWeeklyMemoryMaterialTriggerForExternal(",
    "internal static void MarkWeeklyMemoryMaterialTriggerWithAllSnapshotsForExternal(",
    "public static void MigrateNonHeroPartyScopedMemoryForExternal(",
]
MODELS = ["public sealed class PartyTransferPromptEntry", "public enum PartyTransferEntrySection",
          "public sealed class SettlementTransferPromptEntry", "public enum SettlementTransferEntrySection",
          "public enum SettlementTransferAssetKind"]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--mutate", choices=["worker-direct", "omit-copy", "omit-volunteer-copy"])
    args = parser.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")
    spec = importlib.util.spec_from_file_location("channel_extractor", ROOT / "tools/ChannelCutoverBoundaryTests/run.py")
    extractor = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(extractor)
    inventory = []

    def read(name):
        text = (ROOT / name).read_text(encoding="utf-8-sig")
        inventory.append(dict(file=name, sha256=hashlib.sha256(text.encode()).hexdigest()))
        return text

    source = read("MyBehavior.cs")
    reward = read("RewardSystemBehavior.cs")
    extracted = []
    for signature in MODELS + FACADES:
        declaration = extractor.declaration(source, signature)
        inventory.append(dict(file="MyBehavior.cs", signature=signature,
            line=source[:source.index(declaration)].count("\n") + 1,
            sha256=hashlib.sha256(declaration.encode()).hexdigest()))
        extracted.append(declaration)
    reward_model = extractor.declaration(reward, "public class RewardItemInfo")
    inventory.append(dict(file="RewardSystemBehavior.cs", signature="public class RewardItemInfo",
        line=reward[:reward.index(reward_model)].count("\n") + 1,
        sha256=hashlib.sha256(reward_model.encode()).hexdigest()))
    writes = read("MyBehavior.MemorySourceWrites.cs")
    if args.mutate == "worker-direct":
        original = extractor.declaration(writes, "private static bool DeferMemorySourceWriteIfNeeded(")
        writes = writes.replace(original, "private static bool DeferMemorySourceWriteIfNeeded(Action<MyBehavior> write, string source) { return false; }")
    elif args.mutate == "omit-copy":
        for typename, method in [("RewardSystemBehavior.RewardItemInfo", "CopyMemoryRewardOptions"),
                ("PartyTransferPromptEntry", "CopyMemoryPartyOptions"),
                ("SettlementTransferPromptEntry", "CopyMemorySettlementOptions")]:
            original = extractor.declaration(writes, f"private static List<{typename}> {method}(")
            writes = writes.replace(original, f"private static List<{typename}> {method}(List<{typename}> values) {{ return values; }}")
    elif args.mutate == "omit-volunteer-copy":
        old = "VolunteerSlotIndices = x.VolunteerSlotIndices?.ToList()"
        if writes.count(old) != 1:
            raise ValueError("Volunteer list mutation anchor drift")
        writes = writes.replace(old, "VolunteerSlotIndices = x.VolunteerSlotIndices")
    prefix = "using System; using System.Collections.Generic; using TaleWorlds.Library; using TaleWorlds.Core; using TaleWorlds.CampaignSystem; using TaleWorlds.CampaignSystem.Party; using TaleWorlds.CampaignSystem.Settlements; using TaleWorlds.CampaignSystem.Settlements.Workshops;\nnamespace AnimusForge { public partial class MyBehavior {\n"
    production = prefix + "\n\n".join(extracted) + "\n} public partial class RewardSystemBehavior {" + reward_model + "} }"
    files = {"Facades.cs": production, "Writes.cs": writes,
             "Boundary.cs": read("MyBehavior.MemorySummaryMainThread.cs"),
             "SaveRuntimeGuard.cs": read("SaveRuntimeGuard.cs"),
             "Program.cs": (HERE / "WriterHarness.cs.txt").read_text(encoding="utf-8-sig"),
             "Proof.csproj": '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn></PropertyGroup></Project>',
             "NuGet.Config": '<configuration><packageSources><clear/></packageSources></configuration>'}
    if 'MemorySummaryDispatcher' in files.get('Boundary.cs', ''):
        for relative in ['Refactor/Contracts/IMemorySummaryDispatchHost.cs','Refactor/Runtime/MemorySummaryDispatcher.cs']:
            files[Path(relative).name]=(ROOT/relative).read_text(encoding='utf-8-sig')
    out = HERE / ".generated/writers" / (args.mutate or "current")
    out.mkdir(parents=True, exist_ok=True)
    run_scope_spec=importlib.util.spec_from_file_location('memory_run_fixture',ROOT/'tools/MemorySummaryRunOwnerTests/fixture_support.py');run_scope=importlib.util.module_from_spec(run_scope_spec);run_scope_spec.loader.exec_module(run_scope)
    run_scope.include(files, original=False)
    for name, content in files.items():
        (out / name).write_bytes(content.encode("utf-8"))
    manifest = dict(mutation=args.mutate, extraction=inventory,
        generated_sha256={name: hashlib.sha256(text.encode()).hexdigest() for name, text in files.items()},
        actual_code="8 facade bodies, 3 DTO types, actual copies/dispatch queue and SaveRuntimeGuard",
        fixture_boundaries=["TaleWorlds game identity types", "Campaign owner lookup witness", "terminal writer methods record arguments without game/storage effects"],
        limitations=["no actual storage/AFEF/weekly business", "no game/save or provider run", "reflection inventory checks declared DTO members only; future new member types fail fixture setup"])
    (out / "manifest.json").write_bytes(json.dumps(manifest, ensure_ascii=False, indent=2).encode("utf-8"))
    dotnet = Path(os.environ.get("DOTNET_EXE", r"G:\AFMOD\.dotnet-sdk\dotnet.exe"))
    env = dict(os.environ, DOTNET_ROOT=str(dotnet.parent), DOTNET_CLI_HOME=str(ROOT / ".tmp/dotnet-cli"),
        NUGET_PACKAGES=str(ROOT / ".tmp/nuget-packages"), APPDATA=str(ROOT / ".tmp/appdata"),
        DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1",
        DOTNET_GENERATE_ASPNET_CERTIFICATE="false", DOTNET_CLI_UI_LANGUAGE="en")
    build = subprocess.run([str(dotnet), "build", str(out / "Proof.csproj"), "-c", "Release", "--nologo",
        "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], cwd=ROOT, env=env,
        capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120)
    (out / "build.log").write_bytes((build.stdout + build.stderr).encode("utf-8"))
    if build.returncode:
        print(build.stdout + build.stderr)
        return 2
    run = subprocess.run([str(dotnet), str(out / "bin/Release/net8.0/Proof.dll")], cwd=ROOT, env=env,
        capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=90)
    log = run.stdout + run.stderr
    (out / "run.log").write_bytes(log.encode("utf-8"))
    print("BUILD_PASS writers=" + (args.mutate or "current"))
    print(log, end="")
    if "WRITER_RESULT" not in log:
        return 2
    return run.returncode


if __name__ == "__main__":
    raise SystemExit(main())
