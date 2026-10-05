"""Replay real overlay pause routing/cache/focus methods and prefab bindings, not a game renderer."""
import argparse, importlib.util, json, hashlib, subprocess, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];HERE=Path(__file__).parent
spec=importlib.util.spec_from_file_location("extractor",ROOT/"tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
sys.path.insert(0,str(ROOT/"tests"))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
p=argparse.ArgumentParser();p.add_argument("--mutation",choices=["skip-paused-drain","skip-resume-repaint","steal-focus","allow-stale-text"]);args=p.parse_args()
out=new_run_root(ROOT,"native-focus-pause"+("-"+args.mutation if args.mutation else ""),None)
main=ROOT/"src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeNativeConversationOverlay.cs"
part=ROOT/"src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeNativeConversationOverlay.Interruption.cs"
pres=ROOT/"src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeNativeConversationOverlay.Presentation.cs"
helper=ROOT/"src/AF.GameAdapter.Bannerlord/UI/Conversation/ConversationHelper.cs"
s=main.read_text(encoding="utf-8-sig");methods=[]
for sig in ["public static void OnApplicationTick()","private void RunOnMainThread(Action action)","private void ProcessMainThreadActions()","private bool TickTemporarySystemUiIfNeeded(","private void BeginTemporarySystemUiInterruption()","private bool IsTemporarySystemUiBlocking(","private void HideOverlayForTemporarySystemUi()","private void RestoreOverlayAfterTemporarySystemUi()","private void FocusInputIfVisible()","private void ProcessPostRestoreNativeAnswerRestore()","private void Close(bool silent)","private bool IsSubmitGenerationCurrent(","private void StopWaitingDotsAnimation(int generation)","private void StopWaitingDotsAnimation()"]:
    methods.append(ex.declaration(s,sig))
body="\n".join(methods)
cache=part.read_text(encoding="utf-8-sig")
if args.mutation=="skip-paused-drain":body=body.replace("_activeOverlay.ProcessInterruptedPresentation();","/* compiled mutation: parked UI callbacks */")
if args.mutation=="skip-resume-repaint":body=body.replace("RestoreInterruptedPresentation();","/* compiled mutation: lost resumed text */").replace("ReapplyInterruptedDisplayText();","/* compiled mutation: lost deferred repaint */")
if args.mutation=="steal-focus":body=body.replace("_isClosed || _temporarySystemUiActive || !_dataSource", "_isClosed || !_dataSource")
if args.mutation=="allow-stale-text":cache=cache.replace("_displayTextGeneration != _submitGeneration", "_displayTextGeneration < 0")
(out/"Methods.cs").write_text("using System;using System.Threading;namespace AnimusForge { public sealed partial class AnimusForgeNativeConversationOverlay {"+body+"}}",encoding="utf-8")
(out/"Interruption.cs").write_text(cache,encoding="utf-8")
(out/"Presentation.cs").write_text(pres.read_text(encoding="utf-8-sig"),encoding="utf-8")
(out/"ConversationHelper.cs").write_text(helper.read_text(encoding="utf-8-sig"),encoding="utf-8")
(out/"Fixture.cs").write_text((HERE/"Fixture.cs").read_text(encoding="utf-8-sig"),encoding="utf-8")
(out/"Test.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>true</EnableDefaultCompileItems><Nullable>disable</Nullable></PropertyGroup></Project>',encoding="utf-8")
xml=ROOT/"content/modules/AF.Module.Conversation/GUI/Prefabs/AnimusForgeNativeConversationOverlay.xml"
import xml.etree.ElementTree as ET
node=ET.parse(xml).getroot().find('.//Widget[@Id="AFNativeConversationInputPanel"]');assert node is not None
assert node.attrib["HorizontalAlignment"]=="Left" and node.attrib["MarginLeft"]=="10" and "MarginRight" not in node.attrib
assert node.attrib["IsVisible"]=="@IsCustomAnswerVisible" and node.attrib["SuggestedWidth"]=="720"
edit=node.find("./Children/AnimusForgeNativeConversationEditableTextWidget");assert edit.attrib["RealText"]=="@InputText" and edit.attrib["Command.TextEntered"]=="ExecuteSubmit" and edit.attrib["FocusRequestId"]=="@InputFocusVersion"
for width,height in [(1280,720),(1920,1080),(2048,414),(2560,1440),(3440,1440)]:
    scale=min(width/1920,height/1080);left=float(node.attrib["MarginLeft"])*scale;panel=float(node.attrib["SuggestedWidth"])*scale
    assert left+panel<width/2, (width,height,"native AI input overlaps NPC/right half")
# These boundaries are explicitly preserved: no replacement request, background game access,
# cancellation on focus-loss, or removal of the native pause setting.
assert s.count("SetSubmissionDisplayText(generation,")==16
assert s.count("RunVisibleUiAction(generation, delegate")==2
assert s.count("RunVisibleUiAction(generation, () => LlmRetryPrompt.ShowFailurePopup(")==4
assert "ClearInterruptedPresentation();" in ex.declaration(s,"private void Close(bool silent)")
assert "ClearInterruptedPresentation();" in ex.declaration(s,"private void SetInputVisible(")
assert "StopGameOnFocusLost =" not in s+cache
manifest={str(x.relative_to(ROOT)):hashlib.sha256(x.read_bytes()).hexdigest() for x in [main,part,pres,helper,xml]}
(out/"source-manifest.json").write_text(json.dumps({"files":manifest,"mutation":args.mutation,"scope":"production routing/cache/focus/ConversationHelper, synthetic screen/pause/NPC stamps; not native renderer/network/game save"},indent=2),encoding="utf-8")
result=subprocess.run([resolve_dotnet(ROOT),"run","--project",str(out/"Test.csproj"),"-c","Release"],cwd=ROOT,env=minimal_test_environment(resolve_dotnet(ROOT),out))
print("Run output:",out)
if result.returncode==0:print("PASS prefab anchor/bindings/5 aspect-ratio projections + both submission wiring")
raise SystemExit(result.returncode)
