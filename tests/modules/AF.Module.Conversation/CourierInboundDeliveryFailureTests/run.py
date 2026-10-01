"""Compile the current default delivery and durable memory-only owner against synthetic failures."""
from pathlib import Path
import argparse
import importlib.util
import os
import subprocess
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
OWNER = ROOT / "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DeliveredMemory.cs"
SOURCE = ROOT / "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.SessionTransport.cs"
ROOT_HOST = ROOT / "CourierDeliveryBehavior.cs"
spec = importlib.util.spec_from_file_location(
    "extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--dotnet", default=str(ROOT / "local/dotnet/8.0.425/dotnet.exe"))
parser.add_argument("--run-root", type=Path)
parser.add_argument("--mutate", choices=["skip-history", "current-date"])
args = parser.parse_args()
delivery = extract.declaration(SOURCE.read_text(encoding="utf-8-sig"),
                               "private void DeliverInboundLetterToPlayer(")
root_host = ROOT_HOST.read_text(encoding="utf-8-sig")
storage = extract.declaration(root_host, "private sealed class NpcDiplomacyLetterStorage")
assert "public Dictionary<string, CourierInboundDeliveredMemoryIntent> PendingInboundDeliveredMemoryIntents" in root_host
assert "PendingInboundDeliveredMemoryIntents = CapturePendingInboundDeliveredMemoryIntents()" in root_host
assert "RestorePendingInboundDeliveredMemoryIntents(npcStorage?.PendingInboundDeliveredMemoryIntents);" in root_host
assert "RestorePendingInboundDeliveredMemoryIntents(null);" in root_host
memory_call = "ProcessPendingInboundDeliveredMemoryIntent(session.Id);"
assert delivery.count(memory_call) == 1
assert delivery.index("TryReserveInboundDeliveredMemoryIntent(") < delivery.index("session.DeliveryApplied = true;")
assert delivery.index("session.DeliveryApplied = true;") < delivery.index(memory_call)
assert delivery.index(memory_call) < delivery.index("AddCourierLetterToPlayerInventory(")
if args.mutate == "skip-history":
    delivery = delivery.replace(memory_call, "// mutation: memory-only commit removed", 1)

out = (args.run_root or (ROOT / "artifacts/j17b/session-20260930/p5-channels" /
        ("courier-delivered-" + str(os.getpid())))).resolve()
if not out.is_relative_to((ROOT / "artifacts").resolve()):
    parser.error("--run-root must be under workspace artifacts")
out.mkdir(parents=True, exist_ok=True)
program = (HERE / "DeliveredMemoryHarness.cs.txt").read_text(encoding="utf-8-sig")
memory_source = (ROOT / "MyBehavior.MemoryRecovery.cs").read_text(encoding="utf-8-sig")
seed_builder = extract.declaration(memory_source, "private InteractionMemoryRecoverySeed BuildInteractionMemoryRecoverySeed(")
assert "string originDate = ResolveInteractionMemoryOriginGameDate(originDay, currentDay);" in seed_builder
memory_date = extract.declaration(memory_source, "private static string ResolveInteractionMemoryOriginGameDate(")
if args.mutate == "current-date":
    assert memory_date.count("CampaignTime.Days(Math.Max(0, originDay))") == 1
    memory_date = memory_date.replace("CampaignTime.Days(Math.Max(0, originDay))", "CampaignTime.Days(Math.Max(0, currentDay))", 1)
# The host calendar is synthetic; the helper itself is the actual production code.
memory_date = memory_date.replace("CampaignTime.", "TaleWorlds.CampaignSystem.CampaignTime.")
assert program.count("@@MEMORYDATE@@") == 1
program = program.replace("@@MEMORYDATE@@", memory_date)
assert program.count("@@DELIVERY@@") == 1
assert program.count("@@STORAGE@@") == 1
(out / "Program.cs").write_text(program.replace("@@DELIVERY@@", delivery).replace("@@STORAGE@@", storage), encoding="utf-8")
newtonsoft = Path(os.environ.get("AF_NEWTONSOFT") or os.environ.get("NEWTONSOFT_JSON_PATH") or str(ROOT / ".tmp/nuget-packages/newtonsoft.json/13.0.3/lib/net6.0/Newtonsoft.Json.dll"))
if not newtonsoft.is_file():
    parser.error("local Newtonsoft.Json.dll is required for actual save JSON compatibility")
(out / "Tests.csproj").write_text(
    '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
    '<TargetFramework>net8.0</TargetFramework><Nullable>disable</Nullable>'
    '<ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup><Compile Include="'
    + escape(str(OWNER)) + '" /><Reference Include="Newtonsoft.Json"><HintPath>'
    + escape(str(newtonsoft)) + '</HintPath></Reference></ItemGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text(
    "<configuration><packageSources><clear/></packageSources></configuration>", encoding="utf-8")
env = {
    "DOTNET_ROOT": str(Path(args.dotnet).resolve().parent),
    "DOTNET_CLI_HOME": str(out / "cli"),
    "DOTNET_CLI_TELEMETRY_OPTOUT": "1", "DOTNET_NOLOGO": "1",
    "TEMP": str(out), "TMP": str(out), "APPDATA": str(out),
    "LOCALAPPDATA": str(out), "USERPROFILE": str(out), "HOME": str(out),
    "HOMEDRIVE": out.drive, "HOMEPATH": str(out)[len(out.drive):],
    "NUGET_PACKAGES": str(out / "packages"),
    "SystemRoot": os.environ.get("SystemRoot", r"C:\Windows"),
    "ProgramData": os.environ.get("ProgramData", r"C:\ProgramData"),
    "ALLUSERSPROFILE": os.environ.get("ALLUSERSPROFILE", r"C:\ProgramData"),
    "ProgramFiles": os.environ.get("ProgramFiles", r"C:\Program Files"),
    "ProgramFiles(x86)": os.environ.get("ProgramFiles(x86)", r"C:\Program Files (x86)"),
    "windir": os.environ.get("windir", r"C:\Windows"),
}
result = subprocess.run(
    [args.dotnet, "run", "--project", str(out / "Tests.csproj"), "-c", "Release"],
    cwd=out, env=env, capture_output=True, text=True,
    encoding="utf-8", errors="replace", timeout=90)
log = result.stdout + result.stderr
(out / "run.log").write_text(log, encoding="utf-8")
print(log, end="")
raise SystemExit(result.returncode)
