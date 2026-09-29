"""Exercise the default inbound delivery and legacy memory facade from current source."""
from pathlib import Path
import argparse
import importlib.util
import os

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent

spec = importlib.util.spec_from_file_location(
    "extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
spec = importlib.util.spec_from_file_location(
    "dotnet_util", ROOT / "tests/AF.Contracts/ModuleFrameworkApiTests/run.py")
dotnet_util = importlib.util.module_from_spec(spec)
spec.loader.exec_module(dotnet_util)

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--dotnet", default=str(ROOT / "local/dotnet/8.0.425/dotnet.exe"))
parser.add_argument("--mutate", choices=["skip-history"])
args = parser.parse_args()

delivery_source = (ROOT / "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.SessionTransport.cs").read_text(encoding="utf-8-sig")
memory_source = (ROOT / "MyBehavior.cs").read_text(encoding="utf-8-sig")
delivery = extract.declaration(delivery_source, "private void DeliverInboundLetterToPlayer(")
memory = extract.declaration(memory_source, "public static void AppendExternalDialogueHistory(")
history_call = "MyBehavior.AppendExternalDialogueHistory(sender, null, historyLine, session.DeliveryFactText);"
assert delivery.count(history_call) == 1
assert delivery.index("session.DeliveryApplied = true;") < delivery.index(history_call)
assert "(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.AppendDialogueHistory(" in memory
assert memory.index("DeferMemorySourceWriteIfNeeded(") < memory.index("try")
if args.mutate:
    delivery = delivery.replace(history_call, "// mutation: default inbound history call removed", 1)

fixture = (HERE / "Harness.cs.txt").read_text(encoding="utf-8-sig")
assert fixture.count("@@DELIVERY@@") == fixture.count("@@MEMORY@@") == 1
fixture = fixture.replace("@@DELIVERY@@", delivery).replace("@@MEMORY@@", memory)
out = ROOT / "tmp" / "j17a-courier-inbound-delivery-repro" / (args.mutate or "current")
assert out.resolve().is_relative_to(ROOT.resolve())
out.mkdir(parents=True, exist_ok=True)
(out / "NuGet.Config").write_text("<configuration><packageSources><clear/></packageSources></configuration>", encoding="utf-8")
(out / "Program.cs").write_text(fixture, encoding="utf-8")
project = dotnet_util.project(out, "CourierInboundDeliveryFailure", [out / "Program.cs"], executable=True)
code, log = dotnet_util.run_dotnet(args.dotnet, ["run", "--project", str(project), "-c", "Release"], out)
(out / "run.log").write_text(log, encoding="utf-8")
print(log, end="")
raise SystemExit(code)
