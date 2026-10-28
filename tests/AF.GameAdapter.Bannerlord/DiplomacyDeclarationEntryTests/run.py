"""Offline actual-DLL/widget checks. No game, graphics backend or provider is started."""
from pathlib import Path
import argparse
import hashlib
import json
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

parser = argparse.ArgumentParser()
parser.add_argument("--candidate", type=Path, required=True)
parser.add_argument("--api", choices=["1.3", "1.4"], required=True)
parser.add_argument("--refs", type=Path, action="append", required=True,
                    help="Dependency directories in resolution order; keep native DLL versions consistent.")
parser.add_argument("--framework", type=Path, required=True)
parser.add_argument("--run-root", type=Path)
args = parser.parse_args()
candidate = args.candidate.resolve(strict=True)
refs = [p.resolve(strict=True) for p in args.refs]
framework = args.framework.resolve(strict=True)
digest = hashlib.sha256(candidate.read_bytes()).hexdigest().upper()
marker = json.loads(candidate.with_suffix(".build.json").read_text(encoding="utf-8-sig"))
assert marker["Sha256"] == digest and marker["BannerlordApi"] == args.api
out = new_run_root(ROOT, "diplomacy-declaration-entry", args.run_root)
dotnet = resolve_dotnet(ROOT)
env = minimal_test_environment(dotnet, out)

project = ET.Element("Project", Sdk="Microsoft.NET.Sdk")
group = ET.SubElement(project, "PropertyGroup")
for key, value in dict(OutputType="Exe", TargetFramework="net472", LangVersion="latest",
                       PlatformTarget="x64", EnableDefaultCompileItems="false",
                       AutomaticallyUseReferenceAssemblyPackages="false",
                       FrameworkPathOverride=str(framework), NuGetAudit="false").items():
    ET.SubElement(group, key).text = value
items = ET.SubElement(project, "ItemGroup")
ET.SubElement(items, "Compile", Include=str(Path(__file__).with_name("Program.cs")))
libraries = ["0Harmony", "TaleWorlds.Localization", "TaleWorlds.Library",
             "TaleWorlds.GauntletUI", "TaleWorlds.TwoDimension", "TaleWorlds.MountAndBlade.GauntletUI.Widgets"]
for name in libraries + ["netstandard"]:
    path = framework / "Facades/netstandard.dll" if name == "netstandard" else next(
        (folder / (name + ".dll") for folder in refs if (folder / (name + ".dll")).is_file()), None)
    assert path and path.is_file(), name
    node = ET.SubElement(items, "Reference", Include=name)
    ET.SubElement(node, "HintPath").text = str(path)
    ET.SubElement(node, "Private").text = "false"
ET.ElementTree(project).write(out / "Probe.csproj", encoding="utf-8")
(out / "NuGet.Config").write_text("<configuration><packageSources><clear/></packageSources></configuration>")
commands = [
    ([str(dotnet), "build", str(out / "Probe.csproj"), "--nologo", "-v:q",
      "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], "build.log"),
    ([str(out / "bin/Debug/net472/Probe.exe"), str(candidate), str(ROOT), *map(str, refs)], "run.log")
]
for command, name in commands:
    result = subprocess.run(command, capture_output=True, text=True, encoding="utf-8",
                            errors="replace", env=env, cwd=out)
    (out / name).write_text(result.stdout + result.stderr, encoding="utf-8")
    if result.returncode:
        print(result.stdout + result.stderr)
        raise SystemExit(result.returncode)
(out / "receipt.json").write_text(json.dumps(dict(api=args.api, candidate=str(candidate), sha256=digest,
    scope="Actual DLL and managed widget tree; campaign eligibility, render/font assets, tooltip display and editor opening boundaries are fixtures. No game/provider/deploy."), indent=2))
print((out / "run.log").read_text(encoding="utf-8"))
