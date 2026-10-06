"""Production restore/prefab/scroll previews with vanilla hit ordering; layout and engine are doubles."""
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
    parser.add_argument("--api", choices=["1.3", "1.4"], default="1.4")
    parser.add_argument("--baseline", help="Read the old controller/prefab from this git revision without changing the worktree.")
    args = parser.parse_args()
    output = new_run_root(ROOT, "dialogue-continue-hit", args.run_root)
    spec = importlib.util.spec_from_file_location(
        "extractor", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
    extractor = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(extractor)
    controller = "src/AF.GameAdapter.Bannerlord/UI/Conversation/NativeConversationAnswerAreaController.cs"
    prefab = "extensions/AnimusForge.DialogueUI/GUI/Prefabs/AFDialogueConversation.xml"
    scroll = "extensions/AnimusForge.DialogueUI/src/Native/AFDialogueClickThroughScrollPanel.cs"
    adapter = "extensions/AnimusForge.DialogueUI/src/Native/NativeUiAdapter.cs"
    overlay = "extensions/AnimusForge.DialogueUI/GUI/Prefabs/AFDialogueNativeOverlay.xml"
    source_folder = "原版游戏本体代码1.3.x" if args.api == "1.3" else "原版游戏本体代码1.4.5"
    event_path = ROOT / source_folder / "TaleWorlds.GauntletUI/TaleWorlds/GauntletUI/EventManager.cs"
    event_source = event_path.read_text(encoding="utf-8-sig")
    def read(path):
        if args.baseline and path in (controller, prefab, adapter, overlay):
            return subprocess.check_output(["git", "show", args.baseline + ":" + path], cwd=ROOT).decode("utf-8-sig")
        return (ROOT / path).read_text(encoding="utf-8-sig")
    fixture = (HERE / "Fixture.cs.in").read_text(encoding="utf-8-sig")
    fixture = fixture.replace("__COLLECT__", extractor.declaration(event_source, "private static void CollectEnableWidgetsAt("))
    fixture = fixture.replace("__SELECT__", extractor.declaration(event_source, "private Widget GetWidgetAtPositionForEvent("))
    fixture = fixture.replace("__OVERLAY_HIT__", extractor.declaration(read(adapter), "internal bool HitTest()"))
    for name, content in [("Program.cs", fixture), ("Controller.cs", read(controller)),
                          ("Scroll.cs", read(scroll)), ("Conversation.xml", read(prefab)), ("Overlay.xml", read(overlay))]:
        (output / name).write_text(content, encoding="utf-8")
    project = ET.Element("Project", Sdk="Microsoft.NET.Sdk")
    props = ET.SubElement(project, "PropertyGroup")
    for name, value in [("OutputType", "Exe"), ("TargetFramework", "net8.0"), ("ImplicitUsings", "enable"), ("Nullable", "disable")]:
        ET.SubElement(props, name).text = value
    ET.ElementTree(project).write(output / "ContinueTests.csproj", encoding="utf-8")
    (output / "NuGet.Config").write_text("<configuration><packageSources><clear/></packageSources></configuration>", encoding="utf-8")
    dotnet = resolve_dotnet(ROOT)
    result = subprocess.run([str(dotnet), "run", "--project", str(output / "ContinueTests.csproj"), "--", str(output / "Conversation.xml"), str(output / "Overlay.xml")],
                            cwd=output, env=minimal_test_environment(dotnet, output), capture_output=True,
                            text=True, encoding="utf-8", errors="replace", timeout=120)
    log = f"api={args.api} baseline={args.baseline}\n"
    for path in (controller, prefab, scroll, adapter, overlay):
        log += path + " sha256=" + hashlib.sha256(read(path).encode("utf-8")).hexdigest() + "\n"
    log += str(event_path.relative_to(ROOT)) + " sha256=" + hashlib.sha256(event_path.read_bytes()).hexdigest() + "\n"
    log += result.stdout + result.stderr
    (output / "run.log").write_text(log, encoding="utf-8")
    print(log)
    return result.returncode


if __name__ == "__main__":
    raise SystemExit(main())
