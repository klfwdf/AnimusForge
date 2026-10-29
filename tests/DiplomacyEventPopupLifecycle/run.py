"""Replay production popup lifecycle with synthetic native screen/input boundaries."""
from pathlib import Path
import argparse
import hashlib
import json
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

parser = argparse.ArgumentParser()
parser.add_argument("--run-root", type=Path)
args = parser.parse_args()
out = new_run_root(ROOT, "diplomacy-event-popup-lifecycle", args.run_root)
dotnet = resolve_dotnet(ROOT)
sources = [Path(__file__).with_name("Program.cs"), Path(__file__).with_name("Stubs.cs")]
sources += [ROOT / p for p in [
    "src/AF.GameAdapter.Bannerlord/UI/Diplomacy/DiplomacyEventPopup.cs",
    "src/AF.GameAdapter.Bannerlord/UI/Diplomacy/DiplomacyEventPopupVM.cs",
    "src/AF.GameAdapter.Bannerlord/UI/Common/DevPopupInputLease.cs",
    "src/AF.Contracts/Internal/DiplomacyEventPresentation.cs"]]
project = ET.Element("Project", Sdk="Microsoft.NET.Sdk")
group = ET.SubElement(project, "PropertyGroup")
for key, value in dict(OutputType="Exe", TargetFramework="net8.0", ImplicitUsings="enable",
                      EnableDefaultCompileItems="false", NuGetAudit="false", NoWarn="0649").items():
    ET.SubElement(group, key).text = value
items = ET.SubElement(project, "ItemGroup")
for path in sources:
    ET.SubElement(items, "Compile", Include=str(path))
ET.ElementTree(project).write(out / "Replay.csproj", encoding="utf-8")
(out / "NuGet.Config").write_text("<configuration><packageSources><clear/></packageSources></configuration>")
result = subprocess.run([str(dotnet), "run", "--project", str(out / "Replay.csproj"),
                         "-p:RestoreConfigFile=" + str(out / "NuGet.Config")],
                        cwd=out, env=minimal_test_environment(dotnet, out), capture_output=True, text=True,
                        encoding="utf-8", errors="replace")
(out / "run.log").write_text(result.stdout + result.stderr, encoding="utf-8")
(out / "receipt.json").write_text(json.dumps(dict(exit_code=result.returncode,
    sources={str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest() for p in sources},
    limitations="Production popup/VM/input-lease source; native screen/input/rendering are fixtures. No game/provider/deployment."), indent=2))
print(result.stdout + result.stderr)
raise SystemExit(result.returncode)
