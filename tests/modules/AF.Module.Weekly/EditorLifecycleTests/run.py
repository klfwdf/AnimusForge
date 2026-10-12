"""Prepare isolated current-source replay; --prepare-only never invokes dotnet."""
import argparse
import json
import hashlib
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

parser = argparse.ArgumentParser()
parser.add_argument("--out", type=Path, required=True)
parser.add_argument("--prepare-only", action="store_true")
args = parser.parse_args()
out = new_run_root(ROOT, "weekly-editor-lifecycle", args.out)
project = ET.parse(HERE / "EditorLifecycleTests.csproj")
items = project.getroot().find("ItemGroup")
manifest = []
for item in project.getroot().iter():
    if item.tag in ("Compile", "Content", "EmbeddedResource"):
        path = (HERE / item.get("Include")).resolve()
        assert path.is_file(), path
        item.set("Include", str(path))
    elif item.tag == "HintPath":
        path = (HERE / item.text).resolve()
        assert path.is_file(), path
        item.text = str(path)
    else:
        continue
    manifest.append({"path": path.relative_to(ROOT).as_posix(), "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
for path in sorted(HERE.glob("*.cs")):
    ET.SubElement(items, "Compile", Include=str(path))
    manifest.append({"path": path.relative_to(ROOT).as_posix(), "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
project.write(out / "Tests.csproj", encoding="utf-8", xml_declaration=True)
(out / "NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding="utf-8")
(out / "source-manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
if args.prepare_only:
    print("PREPARED", out, "(C# compilation and replay NOT-RUN)")
    raise SystemExit(0)
dotnet = resolve_dotnet(ROOT)
result = subprocess.run([str(dotnet), "run", "--project", str(out / "Tests.csproj")], cwd=out,
                        env=minimal_test_environment(dotnet, out), capture_output=True,
                        text=True, encoding="utf-8", errors="replace")
(out / "result.log").write_text(result.stdout + result.stderr, encoding="utf-8")
print(result.stdout + result.stderr)
raise SystemExit(result.returncode)
