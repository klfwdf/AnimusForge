"""Replay source-extracted layout with synthetic font advances; not native UI acceptance."""
from pathlib import Path
import hashlib, json, subprocess, sys, xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
OUT = Path(sys.argv[1]).resolve()
DOTNET = Path(sys.argv[2]).resolve() if len(sys.argv) > 2 else ROOT / "local/dotnet/8.0.426/dotnet.exe"
OUT.mkdir(parents=True, exist_ok=True)
source_path = ROOT / "src/AF.GameAdapter.Bannerlord/UI/Common/DevMultilineEditableTextWidget.cs"
source = source_path.read_text(encoding="utf-8-sig")

def method(signature):
    start = source.index(signature)
    opening = source.index("{", start)
    depth = 1
    cursor = opening + 1
    while depth:
        depth += (source[cursor] == "{") - (source[cursor] == "}")
        cursor += 1
    return source[start:cursor]

signatures = ["private void RebuildVisualLines()", "private int FindBestLineLength(", "private int FindPreferredBreak("]
extracted = [method(s) for s in signatures]
# Name-only adapter permits the unchanged production caller to select old/new fitters.
extracted[1] = extracted[1].replace("FindBestLineLength(", "FindCurrentLineLength(", 1)
(OUT / "ExtractedLayout.cs").write_text("using System;\npartial class LayoutReplay {\n" + "\n".join(extracted) + "\n}\n", encoding="utf-8")
manifest = {"source": str(source_path.relative_to(ROOT)), "sha256": hashlib.sha256(source_path.read_bytes()).hexdigest(),
    "extractedMethods": signatures, "scope": "Production layout methods; name-only fitting adapter; synthetic glyph advances and no native widgets, fonts, renderer or game loop."}
(OUT / "extraction-manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
project = ET.Element("Project", Sdk="Microsoft.NET.Sdk")
props = ET.SubElement(project, "PropertyGroup")
for key, value in [("OutputType", "Exe"), ("TargetFramework", "net8.0"), ("LangVersion", "latest"), ("EnableDefaultCompileItems", "false")]:
    ET.SubElement(props, key).text = value
items = ET.SubElement(project, "ItemGroup")
for path in [HERE / "Program.cs", OUT / "ExtractedLayout.cs"]:
    ET.SubElement(items, "Compile", Include=str(path))
ET.ElementTree(project).write(OUT / "Tests.csproj", encoding="utf-8")
result = subprocess.run([str(DOTNET), "run", "--project", str(OUT / "Tests.csproj"), "-c", "Release", "--", str(OUT / "results.json")], capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120)
(OUT / "run.log").write_text(result.stdout + result.stderr, encoding="utf-8")
print(result.stdout + result.stderr, end="")
raise SystemExit(result.returncode)
