"""Exercise the shared Scene/Courier rule-hit predicate and require Courier consumers to use it."""
from pathlib import Path
import argparse
import importlib.util
import os
import subprocess

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location(
    "extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--mutate", choices=["no-trim"])
args = parser.parse_args()
shout = (ROOT / "src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs").read_text(encoding="utf-8-sig")
courier = (ROOT / "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.cs").read_text(encoding="utf-8-sig")
predicate = extract.declaration(shout, "private static bool HasPreprocessRuleHit(")
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
    predicate = predicate.replace("(x ?? \"\").Trim()", "(x ?? \"\")", 1)
out = ROOT / "artifacts/j17b/session-20260930/p5-channels" / (
    "courier-rule-qualification-mutant-001" if args.mutate else "courier-rule-qualification-001")
out.mkdir(parents=True, exist_ok=True)
template = (HERE / "Harness.cs.txt").read_text(encoding="utf-8-sig")
(out / "Program.cs").write_text(template.replace("@@PREDICATE@@", predicate).replace("@@SEAM@@", seam), encoding="utf-8")
(out / "Tests.csproj").write_text(
    '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
    '<TargetFramework>net8.0</TargetFramework><Nullable>disable</Nullable>'
    '<ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text(
    "<configuration><packageSources><clear/></packageSources></configuration>", encoding="utf-8")
dotnet = ROOT / "local/dotnet/8.0.425/dotnet.exe"
env = {
    "DOTNET_ROOT": str(dotnet.parent), "DOTNET_CLI_HOME": str(out / "cli"),
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
result = subprocess.run([str(dotnet), "run", "--project", str(out / "Tests.csproj"), "-c", "Release"],
                        cwd=out, env=env, capture_output=True, text=True,
                        encoding="utf-8", errors="replace", timeout=90)
log = result.stdout + result.stderr
(out / "run.log").write_text(log, encoding="utf-8")
print(log, end="")
raise SystemExit(result.returncode)
