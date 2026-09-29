"""Replay the prior-crash party skip bridge from the current production methods."""
from pathlib import Path
import argparse
import importlib.util

ROOT = Path(__file__).resolve().parents[3]
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
parser.add_argument("--mutate", choices=["ignore-skip"])
args = parser.parse_args()

diagnostics = (ROOT / "CampaignTickDiagnosticsPatch.cs").read_text(encoding="utf-8-sig")
safety = (ROOT / "AnimusForgeMobilePartyAiSafetyPatch.cs").read_text(encoding="utf-8-sig")
consume = extract.declaration(diagnostics, "public static bool ConsumePriorCrashSuspectPartySkip(")
load = extract.declaration(diagnostics, "private static void LoadPriorCrashSuspectFromCheckpoint(")
prefix = extract.declaration(safety, "public static bool PartyHourlyAiTickPrefix(")
assert "PriorCrashSuspectPartySkips[partyId] = PriorCrashSuspectSkipCount;" in load
assert 'lastLine.Contains("CampaignEventDispatcher.TickPartialHourlyAi")' in load
assert "private const int PriorCrashSuspectSkipCount = 18;" in diagnostics
needle = "if (CampaignTickDiagnosticsPatch.ConsumePriorCrashSuspectPartySkip(party, out string priorCrashReason))"
assert prefix.count(needle) == 1
if args.mutate:
    prefix = prefix.replace(needle, needle[:-1] + " && false)", 1)

fixture = (HERE / "Harness.cs.txt").read_text(encoding="utf-8-sig")
assert fixture.count("@@CONSUME@@") == fixture.count("@@PREFIX@@") == 1
fixture = fixture.replace("@@CONSUME@@", consume).replace("@@PREFIX@@", prefix)
out = ROOT / "tmp" / "j17a-campaign-tick-recovery-repro" / (args.mutate or "current")
assert out.resolve().is_relative_to(ROOT.resolve())
out.mkdir(parents=True, exist_ok=True)
(out / "NuGet.Config").write_text("<configuration><packageSources><clear/></packageSources></configuration>", encoding="utf-8")
(out / "Program.cs").write_text(fixture, encoding="utf-8")
project = dotnet_util.project(out, "CampaignTickRecovery", [out / "Program.cs"], executable=True)
code, log = dotnet_util.run_dotnet(args.dotnet, ["run", "--project", str(project), "-c", "Release"], out)
(out / "run.log").write_text(log, encoding="utf-8")
print(log, end="")
raise SystemExit(code)
