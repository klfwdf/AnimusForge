"""Actual redraw editor and VMs with UI/engine doubles; no native rendering or network."""
from __future__ import annotations
import argparse
import hashlib
import importlib.util
from pathlib import Path
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import minimal_test_environment, new_run_root, resolve_dotnet


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--run-root", type=Path)
    args = parser.parse_args()
    output = new_run_root(ROOT, "illustrator-player-redraw", args.run_root)
    spec = importlib.util.spec_from_file_location("extractor", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
    extract = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(extract)
    card = ROOT / "extensions/AnimusForge.Illustrator/src/UI/Overlays/IllustrationCardVM.cs"
    editor = card.with_name("IllustrationRedrawPromptEditor.cs")
    weekly = ROOT / "extensions/AnimusForge.Illustrator/src/UI/Patches/WeeklyReportPopupIllustrationPatch.cs"
    host = ROOT / "src/modules/AF.Module.Weekly/Panel/WorldBulletinPanelVM.cs"
    director = ROOT / "extensions/AnimusForge.Illustrator/src/Core/VisualDirectorEngine.cs"
    popup = ROOT / "src/AF.GameAdapter.Bannerlord/UI/Weekly/DevWeeklyReportPopup.cs"
    fixture = (HERE / "Fixture.cs.in").read_text(encoding="utf-8-sig")
    for token, path, signature in [
        ("__WEEKLY_VM__", weekly, "public sealed class WeeklyReportIllustrationOverlayVM"),
        ("__BULLETIN_VM__", host, "public sealed class WorldBulletinIllustrationVM"),
        ("__DIRECTOR_GUARD__", director, "internal static void RequirePlayerRedrawDirector("),
        ("__SET_EDITING__", popup, "private void SetIllustrationPromptEditing("),
        ("__ESCAPE_GUARD__", popup, "private bool ShouldCloseForEscapeKey()")]:
        fixture = fixture.replace(token, extract.declaration(path.read_text(encoding="utf-8-sig"), signature))
    (output / "Program.cs").write_text(fixture, encoding="utf-8")
    project = ET.Element("Project", Sdk="Microsoft.NET.Sdk")
    props = ET.SubElement(project, "PropertyGroup")
    for name, value in [("OutputType", "Exe"), ("TargetFramework", "net8.0"), ("ImplicitUsings", "enable"), ("Nullable", "disable")]:
        ET.SubElement(props, name).text = value
    items = ET.SubElement(project, "ItemGroup")
    for path in (card, editor):
        ET.SubElement(items, "Compile", Include=str(path))
    ref = ET.SubElement(items, "Reference", Include="Newtonsoft.Json")
    ET.SubElement(ref, "HintPath").text = str(ROOT / "local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll")
    ET.ElementTree(project).write(output / "RedrawTests.csproj", encoding="utf-8")
    (output / "NuGet.Config").write_text("<configuration><packageSources><clear/></packageSources></configuration>", encoding="utf-8")
    dotnet = resolve_dotnet(ROOT)
    result = subprocess.run([str(dotnet), "run", "--project", str(output / "RedrawTests.csproj"), "--", str(ROOT)],
        cwd=output, env=minimal_test_environment(dotnet, output), capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120)
    log = "".join(str(path.relative_to(ROOT)) + " sha256=" + hashlib.sha256(path.read_bytes()).hexdigest() + "\n" for path in (card, editor, weekly, host, director, popup))
    log += result.stdout + result.stderr
    (output / "run.log").write_text(log, encoding="utf-8")
    print(str(output))
    print(log)
    return result.returncode


if __name__ == "__main__":
    raise SystemExit(main())
