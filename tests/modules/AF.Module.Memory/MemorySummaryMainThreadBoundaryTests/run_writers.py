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
import sys as _relocation_sys
_relocation_sys.path.insert(0, str(Path(__file__).resolve().parents[4] / "tests"))
from output_isolation import current_source_path, resolve_dotnet, minimal_test_environment
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
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
    parser.add_argument("--run-root", type=Path)
    args = parser.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")
    spec = importlib.util.spec_from_file_location("channel_extractor", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
    extractor = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(extractor)
    inventory = []

    def read(name):
        text = (current_source_path(ROOT, name)).read_text(encoding="utf-8-sig")
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
    identity_source=read("src/AF.GameAdapter.Bannerlord/Memory/MemoryEntityIdentityBannerlordAdapter.cs")
    copies=[]
    for signature in ["internal static List<PartyTransferPromptEntry> CopyMemoryPartyOptions(","internal static List<SettlementTransferPromptEntry> CopyMemorySettlementOptions("]:
        body=extractor.declaration(identity_source,signature)
        inventory.append(dict(file="src/AF.GameAdapter.Bannerlord/Memory/MemoryEntityIdentityBannerlordAdapter.cs",signature=signature,sha256=hashlib.sha256(body.encode()).hexdigest()))
        copies.append(body)
    identity_copies="using System;using System.Linq;using System.Collections.Generic;using static AnimusForge.MyBehavior;namespace AnimusForge {internal static class MemoryEntityIdentityBannerlordAdapter {"+"\n".join(copies)+"}}"

    if args.mutate == "worker-direct":
        original = extractor.declaration(writes, "private static bool DeferMemorySourceWriteIfNeeded(")
        writes = writes.replace(original, "private static bool DeferMemorySourceWriteIfNeeded(Action<MyBehavior> write, string source) { return false; }")
    elif args.mutate == "omit-copy":
        for typename, method in [("RewardSystemBehavior.RewardItemInfo", "CopyMemoryRewardOptions"),
                ("PartyTransferPromptEntry", "CopyMemoryPartyOptions"),
                ("SettlementTransferPromptEntry", "CopyMemorySettlementOptions")]:
            if method=='CopyMemoryRewardOptions':
                original = extractor.declaration(writes, f"private static List<{typename}> {method}(")
                writes = writes.replace(original, f"private static List<{typename}> {method}(List<{typename}> values) {{ return values; }}")
            else:
                signature=f"internal static List<{typename}> {method}("
                original=extractor.declaration(identity_copies,signature)
                identity_copies=identity_copies.replace(original,f"internal static List<MyBehavior.{typename}> {method}(List<MyBehavior.{typename}> values) {{ return values; }}")
    elif args.mutate == "omit-volunteer-copy":
        old = "VolunteerSlotIndices = x.VolunteerSlotIndices?.ToList()"
        if identity_copies.count(old) != 1:
            raise ValueError("Volunteer list mutation anchor drift")
        identity_copies = identity_copies.replace(old, "VolunteerSlotIndices = x.VolunteerSlotIndices")
    prefix = "using System; using System.Collections.Generic; using TaleWorlds.Library; using TaleWorlds.Core; using TaleWorlds.CampaignSystem; using TaleWorlds.CampaignSystem.Party; using TaleWorlds.CampaignSystem.Settlements; using TaleWorlds.CampaignSystem.Settlements.Workshops;\nnamespace AnimusForge { public partial class MyBehavior {\n"
    production = prefix + "\n\n".join(extracted) + "\n} public partial class RewardSystemBehavior {" + reward_model + "} }"
    files = {"Facades.cs": production, "Writes.cs": writes, "IdentityCopies.cs": identity_copies,
             "Boundary.cs": read("MyBehavior.MemorySummaryMainThread.cs"),
             "SaveRuntimeGuard.cs": read("src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs"),
             "Program.cs": (HERE / "WriterHarness.cs.txt").read_text(encoding="utf-8-sig"),
             "Proof.csproj": '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn></PropertyGroup></Project>',
             "NuGet.Config": '<configuration><packageSources><clear/></packageSources></configuration>'}
    if 'MemorySummaryDispatcher' in files.get('Boundary.cs', ''):
        for relative in ['src/modules/AF.Module.Memory/Summary/IMemorySummaryDispatchHost.cs','src/modules/AF.Module.Memory/Summary/MemorySummaryDispatcher.cs']:
            files[Path(relative).name]=(current_source_path(ROOT, relative)).read_text(encoding='utf-8-sig')
    out = args.run_root.resolve() if args.run_root else HERE / ".generated/writers" / (args.mutate or "current")
    out.relative_to(ROOT)
    out.mkdir(parents=True, exist_ok=True)
    run_scope_spec=importlib.util.spec_from_file_location('memory_run_fixture',ROOT/'tests/modules/AF.Module.Memory/MemorySummaryRunOwnerTests/fixture_support.py');run_scope=importlib.util.module_from_spec(run_scope_spec);run_scope_spec.loader.exec_module(run_scope)
    run_scope.include(files, original=False)
    for name, content in files.items():
        (out / name).write_bytes(content.encode("utf-8"))
    manifest = dict(mutation=args.mutate, extraction=inventory,
        generated_sha256={name: hashlib.sha256(text.encode()).hexdigest() for name, text in files.items()},
        actual_code="8 facade bodies, 3 DTO types, actual copies/dispatch queue and SaveRuntimeGuard",
        fixture_boundaries=["TaleWorlds game identity types", "Campaign owner lookup witness", "terminal writer methods record arguments without game/storage effects"],
        limitations=["no actual storage/AFEF/weekly business", "no game/save or provider run", "reflection inventory checks declared DTO members only; future new member types fail fixture setup"])
    (out / "manifest.json").write_bytes(json.dumps(manifest, ensure_ascii=False, indent=2).encode("utf-8"))
    dotnet = resolve_dotnet(ROOT)
    env = minimal_test_environment(dotnet, out)
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
