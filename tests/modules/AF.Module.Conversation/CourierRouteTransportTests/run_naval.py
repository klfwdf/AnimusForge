"""Execute current Courier naval methods with explicit game/model leaf fakes."""
from pathlib import Path
import argparse
import hashlib
import importlib.util
import json
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

spec = importlib.util.spec_from_file_location(
    "extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--run-root", type=Path)
args = parser.parse_args()
out = new_run_root(ROOT, "CourierNavalRoute", args.run_root)
dotnet = resolve_dotnet(ROOT)
route_path = ROOT / "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.RouteTransport.cs"
main_path = route_path.with_name("CourierDeliveryBehavior.cs")
route = route_path.read_text(encoding="utf-8-sig")
main = main_path.read_text(encoding="utf-8-sig")
signatures = [
    "private static CourierRoutePlan BuildCourierRoutePlan(",
    "private static bool ShouldUseNavalRoute(",
    "private static string BuildNavalRouteReason(",
    "private static MobileParty.NavigationType GetEffectiveCourierNavigationType(",
    "private static bool EnsureCourierNavalReadiness(",
    "private static bool IsCourierSafeForTemporaryShipOwner(",
    "private static void MarkExistingCourierTemporaryShips(",
    "private static ShipHull SelectCourierTemporaryShipHull(",
    "private static List<ShipHull> GetLoadedShipHulls(",
    "private static void AddLoadedShipHulls(",
    "private static bool IsNavalRuntimeAvailable(",
    "private static bool DefaultLandPathExists(",
    "private static bool IsValidCampaignPosition(",
    "private static void DestroyCourierTemporaryShips(",
    "private static bool IsCourierTemporaryShip(",
]
bodies = [extract.declaration(route, signature) for signature in signatures]
boat_name = next(line for line in main.splitlines() if "private const string TemporaryCourierShipName =" in line)
production = "\n".join([
    "using System; using System.Collections.Generic; using System.Linq; using TaleWorlds.ModuleManager;",
    "namespace AnimusForge { public sealed partial class CourierDeliveryBehavior {",
    boat_name,
    extract.declaration(main, "private sealed class CourierRoutePlan"),
    *bodies,
    "} }",
])
(out / "Production.cs").write_text(production, encoding="utf-8")
(out / "Harness.cs").write_bytes((HERE / "NavalHarness.cs.txt").read_bytes())
(out / "Naval.csproj").write_text('''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType>
<Nullable>disable</Nullable><NuGetAudit>false</NuGetAudit>
</PropertyGroup></Project>''', encoding="utf-8")
(out / "NuGet.Config").write_text(
    '<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
(out / "inputs.json").write_text(json.dumps({
    "sourceClass": "current-production-method-extraction",
    "methods": signatures,
    "rawSha256": {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest()
                  for p in [route_path, main_path, HERE / "NavalHarness.cs.txt"]},
    "gamePathfindingAndSaveLoad": "NOT_RUN",
}, indent=2), encoding="utf-8")
result = subprocess.run([str(dotnet), "run", "--project", str(out / "Naval.csproj"), "-c", "Release"],
    cwd=out, env=minimal_test_environment(dotnet, out), capture_output=True,
    text=True, encoding="utf-8", errors="replace", timeout=180)
log = result.stdout + result.stderr
(out / "run.log").write_text(log, encoding="utf-8")
print(log, end="")
if result.returncode:
    raise SystemExit(result.returncode)

# Every routed consumer must provision before submitting its existing native command.
for signature in ["private void RouteToRecipient(", "private void RouteToSender(",
                  "private bool TryRouteCourierAwayFromBanditThreat(",
                  "private void RouteToSafeSettlementForThreat(",
                  "private void RouteToSafeSettlement("]:
    body = extract.declaration(route, signature)
    assert body.index("BuildCourierRoutePlan(") < body.index("EnsureCourierNavalReadiness(") < body.index("courier.SetMoveGoTo"), signature
available = extract.declaration(route, "private static bool IsNavalRuntimeAvailable(")
assert all(token not in available for token in ["GetLoadedShipHulls(", "Settlement.All", "MobileParty.All", "GetType()"])
print("PASS naval consumer wiring and constant-time DLC gate; native pathfinding/save-load live=NOT_RUN")
