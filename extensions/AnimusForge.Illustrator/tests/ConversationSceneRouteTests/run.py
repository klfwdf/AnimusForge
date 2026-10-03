"""Exercise production capture source/dispatch with engine boundary doubles; no native rendering."""
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
    parser.add_argument("--dotnet")
    parser.add_argument("--mutate", choices=["mission-screen"])
    args = parser.parse_args()
    spec = importlib.util.spec_from_file_location(
        "route_extractor", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
    extractor = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(extractor)
    source_path = ROOT / "extensions/AnimusForge.Illustrator/src/Engine/MapConversationSceneCapture.cs"
    dispatch_path = ROOT / "extensions/AnimusForge.Illustrator/src/Engine/SceneReferenceCapture.cs"
    popup_path = ROOT / "extensions/AnimusForge.Illustrator/src/UI/Overlays/IllustrationCardPopup.cs"
    dispatch = extractor.declaration(dispatch_path.read_text(encoding="utf-8-sig"),
                                     "internal static async Task<ConversationSceneReferenceCapture> CaptureConversationSceneReferencesAsync(")
    popup = popup_path.read_text(encoding="utf-8-sig")
    assert "if (probeSource?.RequiresMissionPanorama != true) probeSource = null;" in popup
    assert "else if (probeSource == null)" in popup
    assert "source?.RequiresMissionPanorama != true" in popup
    assert "!sceneSource.RequiresMissionPanorama" in popup
    output = new_run_root(ROOT, "illustrator-conversation-scene-route", args.run_root)
    if args.mutate:
        old = "_mission != null && _mission.GetMissionBehavior<ConversationMissionLogic>() == null"
        value = source_path.read_text(encoding="utf-8-sig")
        assert old in value
        source_path = output / "MutatedCaptureSource.cs"
        source_path.write_text(value.replace(old, "_mission != null"), encoding="utf-8")
    fixture = (HERE / "Fixture.cs.in").read_text(encoding="utf-8-sig")
    (output / "Program.cs").write_text(fixture.replace("__DISPATCH__", dispatch), encoding="utf-8")
    project = ET.Element("Project", Sdk="Microsoft.NET.Sdk")
    props = ET.SubElement(project, "PropertyGroup")
    for name, value in [("OutputType", "Exe"), ("TargetFramework", "net8.0"), ("ImplicitUsings", "enable"), ("Nullable", "disable")]:
        ET.SubElement(props, name).text = value
    refs = ET.SubElement(project, "ItemGroup")
    if not args.mutate:
        ET.SubElement(refs, "Compile", Include=str(source_path), Link="MapConversationSceneCapture.cs")
    ref = ET.SubElement(refs, "Reference", Include="Newtonsoft.Json")
    ET.SubElement(ref, "HintPath").text = str(ROOT / "local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll")
    ET.ElementTree(project).write(output / "RouteTests.csproj", encoding="utf-8")
    (output / "NuGet.Config").write_text("<configuration><packageSources><clear/></packageSources></configuration>", encoding="utf-8")
    dotnet = resolve_dotnet(ROOT, args.dotnet)
    result = subprocess.run([str(dotnet), "run", "--project", str(output / "RouteTests.csproj")],
                            cwd=output, env=minimal_test_environment(dotnet, output), capture_output=True,
                            text=True, encoding="utf-8", errors="replace", timeout=120)
    log = "mutation=" + str(args.mutate) + "\n"
    for path in [source_path, dispatch_path, popup_path]:
        log += str(path.relative_to(ROOT)) + " sha256=" + hashlib.sha256(path.read_bytes()).hexdigest() + "\n"
    log += result.stdout + result.stderr
    (output / "run.log").write_text(log, encoding="utf-8")
    print(log)
    return result.returncode


if __name__ == "__main__":
    raise SystemExit(main())
