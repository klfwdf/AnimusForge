"""Exercise the shared Scene/Courier rule-hit predicate and require Courier consumers to use it."""
from pathlib import Path
import argparse
import importlib.util
import os
import subprocess
import sys
sys.path.insert(0,str(Path(__file__).resolve().parents[4]/"tests"))
from output_isolation import new_run_root, minimal_test_environment

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location(
    "extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--run-root",type=Path)
parser.add_argument("--mutate", choices=["no-trim"])
args = parser.parse_args()
shout = (ROOT / "src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs").read_text(encoding="utf-8-sig")
courier = (ROOT / "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.cs").read_text(encoding="utf-8-sig")
predicate = extract.declaration(shout, "private static bool HasPreprocessRuleHit(")
owner = extract.declaration((ROOT/"src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs").read_text(encoding="utf-8-sig"),"internal static bool HasPreprocessRuleHit(")
seam = extract.declaration(shout, "internal static bool HasPreprocessRuleHitForExternal(")
assert "private static bool HasPreprocessRuleHit(" not in courier
assert courier.count("ShoutBehavior.HasPreprocessRuleHitForExternal(") >= 12
for name in ("PromptPreparation", "GenerationLifecycle"):
    source = (ROOT / "src/modules/AF.Module.Conversation/Channels/Courier" /
              ("CourierDeliveryBehavior." + name + ".cs")).read_text(encoding="utf-8-sig")
    assert "ShoutBehavior.HasPreprocessRuleHitForExternal(" in source
    assert "HasPreprocessRuleHit(selectedRuleHits," not in source.replace(
        "ShoutBehavior.HasPreprocessRuleHitForExternal(selectedRuleHits,", "")
if args.mutate:
    anchor='(x ?? "").Trim()'
    assert owner.count(anchor)==1, "Rule-hit mutation anchor changed"
    owner=owner.replace(anchor,'(x ?? "")',1)
out = new_run_root(ROOT, "courier-rule-qualification", args.run_root)
template = (HERE / "Harness.cs.txt").read_text(encoding="utf-8-sig")
(out / "Program.cs").write_text(template.replace("@@PREDICATE@@", predicate + "\nprivate static class ConversationActionPostprocessOwner { " + owner + " }").replace("@@SEAM@@", seam), encoding="utf-8")
(out / "Tests.csproj").write_text(
    '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
    '<TargetFramework>net8.0</TargetFramework><Nullable>disable</Nullable>'
    '<ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text(
    "<configuration><packageSources><clear/></packageSources></configuration>", encoding="utf-8")
dotnet = ROOT / "local/dotnet/8.0.425/dotnet.exe"
env = minimal_test_environment(dotnet,out)
result = subprocess.run([str(dotnet), "run", "--project", str(out / "Tests.csproj"), "-c", "Release"],
                        cwd=out, env=env, capture_output=True, text=True,
                        encoding="utf-8", errors="replace", timeout=90)
log = result.stdout + result.stderr
(out / "run.log").write_text(log, encoding="utf-8")
print(log, end="")
raise SystemExit(result.returncode)
