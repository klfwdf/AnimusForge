"""Compile verbatim production methods with fake campaign objects; no live-game claim."""
from pathlib import Path
import argparse
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment


def method(source, signature):
    start = source.index("\t" + signature)
    end = source.index("\n\t}\n", start) + len("\n\t}")
    return source[start:end]


parser = argparse.ArgumentParser()
parser.add_argument("--mutation", choices=["conversion", "initiative", "refresh"])
args = parser.parse_args()
owner = (ROOT / "src/modules/AF.Module.WorldMap/Runtime/WorldMapPartyCommandBehavior.cs").read_text(encoding="utf-8-sig")
model = (ROOT / "CourierMobilePartyAIModel.cs").read_text(encoding="utf-8-sig")
signatures = [
    "internal static bool ShouldSuppressSettlementStayInitiative(",
    "private static bool IsPartyHoldingInsideCommandSettlement(",
    "private static bool TryBuildGoToSettlementAttackCommand(",
    "private static bool TryResolveHostileBesiegerParty(",
    "private static bool TryConvertCurrentGoToSettlementCommand(",
    "private static bool ShouldConvertGoToSettlementCommands(",
    "private static PartyCommandEntry GetCurrentCommand(",
    "private static bool IsStopPending(",
    "private static bool IsKind(",
    "private static bool IsPartyUsable(",
    "private static double GetCommandHoldUntilDay(",
    "private void TickGoToSettlement(",
]
methods = "\n".join(method(owner, signature) for signature in signatures)
model_method = method(model, "public override bool ShouldPartyCheckInitiativeBehavior(")
if args.mutation == "conversion":
    before = "\t\tif (IsPartyHoldingInsideCommandSettlement(party, command))\n\t\t{\n\t\t\treturn false;\n\t\t}\n"
    assert methods.count(before) == 1
    methods = methods.replace(before, "", 1)
elif args.mutation == "initiative":
    model_method = "public override bool ShouldPartyCheckInitiativeBehavior(MobileParty mobileParty) { return _inner.ShouldPartyCheckInitiativeBehavior(mobileParty); }"
elif args.mutation == "refresh":
    before = "\n\t\t\t|| !IsAiDecisionLockActive(party) || !IsPartyVisitingSettlement(party, settlement)"
    assert methods.count(before) == 1
    methods = methods.replace(before, "", 1)

generated = (HERE / "Fixture.cs").read_text(encoding="utf-8").replace("// OWNER_METHODS", methods).replace("// MODEL_METHOD", model_method)
output = new_run_root(ROOT, "settlement-stay", None)
(output / "Program.cs").write_text(generated, encoding="utf-8")
(output / "Probe.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup><Compile Include="Program.cs" /></ItemGroup></Project>', encoding="utf-8")
(output / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
dotnet = resolve_dotnet(ROOT)
result = subprocess.run([str(dotnet), "run", "--project", str(output / "Probe.csproj"), "-c", "Release"], cwd=output, env=minimal_test_environment(dotnet, output), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, encoding="utf-8", errors="replace")
(output / "result.log").write_text(result.stdout, encoding="utf-8")
print(result.stdout)
print("Evidence:", output)
if args.mutation:
    if result.returncode == 0 or "FAIL:" not in result.stdout:
        raise SystemExit("Mutation was not killed by a behavioral assertion")
    print("Mutation killed:", args.mutation)
else:
    raise SystemExit(result.returncode)
