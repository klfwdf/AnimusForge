"""Exact production appearance editor/getter/composer and real CN/EN reader/resources.
Knowledge DTO, editor surface and cache invalidation are fixtures; no save/provider/game.
"""
from pathlib import Path
import argparse
import importlib.util
import json
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

parser = argparse.ArgumentParser()
parser.add_argument("--run-root", type=Path)
args = parser.parse_args()
out = new_run_root(ROOT, "player-appearance", args.run_root)
dotnet = resolve_dotnet(ROOT)
spec = importlib.util.spec_from_file_location("extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec); spec.loader.exec_module(extract)
source = (ROOT / "src/modules/AF.Module.Knowledge/Host/KnowledgeLibraryBehavior.cs").read_text(encoding="utf-8-sig")
methods = "\n".join(extract.declaration(source, marker) for marker in (
    "private static string NormalizePlayerAppearanceForStorage(", "private string GetPlayerAppearanceForPrompt(",
    "private static string BuildPermanentPlayerAppearanceContext(", "private void OpenPlayerPersonaAppearanceEditor("))
persona_source = extract.declaration(source, "internal string BuildPersonaLoreSource(")
assert "includePlayerPersona: false" in persona_source and "PlayerAppearance" not in persona_source
editor_source = extract.declaration(source, "private void OpenPlayerPersonaAppearanceEditor(")
assert "_file.PlayerAppearance = text2" in editor_source and "TouchRuleData();" in editor_source
resources = ROOT / "content/modules/AF.Module.Knowledge/ModuleData"
cn = json.loads((resources / "KnowledgeRagPromptTexts.json").read_text(encoding="utf-8-sig"))
en = json.loads((resources / "KnowledgeRagPromptTexts.en.json").read_text(encoding="utf-8-sig"))
for key in ("NpcFallback", "PlayerAppearanceHeader", "PlayerAppearanceObservation", "PlayerAppearanceEnd"):
    assert sorted(re.findall(r"\{[A-Z_]+\}", cn[key])) == sorted(re.findall(r"\{[A-Z_]+\}", en[key]))
for key, literal in re.findall(r'KnowledgePromptTexts\.(?:Get|Format)\("([^"]+)",\s*("(?:\\.|[^"\\])*")', methods):
    assert json.loads(literal) == en[key], key
reader = ROOT / "src/modules/AF.Module.Knowledge/Host/KnowledgePromptTexts.cs"
(out / "KnowledgePromptTexts.cs").write_bytes(reader.read_bytes())
program = r'''
using System;using System.IO;using System.Text;
namespace AnimusForge {
internal static class AnimusForgeModulePaths {internal static string Root;internal static string GetModuleDataFilePath(string name)=>Path.Combine(Root,name);}
internal static class KnowledgeUiTexts {internal static string PlayerAppearanceEditorTitle="title",PlayerAppearanceEditorIntro="intro",PlayerAppearanceEditorHint="hint",Confirm="confirm",Back="back";}
internal static class DevTextEditorHelper {internal static string Initial;internal static Action<string> Confirm;internal static Action Cancel;internal static void ShowLongTextEditor(string title,string intro,string hint,string initial,Action<string> confirm,Action cancel,string yes,string no){Initial=initial;Confirm=confirm;Cancel=cancel;}}
internal class KnowledgeLibraryBehavior {
private class KnowledgeFile {public string PlayerAppearance="";}
private KnowledgeFile _file=new KnowledgeFile();private static int touches,checks;
private static void TouchRuleData(){touches++;}
static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
@@METHODS@@
static int Main(string[] args){try{
 AnimusForgeModulePaths.Root=args[0];bool en=args[1]=="en";var owner=new KnowledgeLibraryBehavior();int returns=0;
 owner.OpenPlayerPersonaAppearanceEditor(()=>returns++);Check(DevTextEditorHelper.Initial=="","empty editor reads player field");
 string raw="我有银色头发和金色眼睛。\r\nI wear a silver cloak. {NPC} {NAME}".Replace("\\r\\n","\r\n");
 DevTextEditorHelper.Confirm(raw);string normalized=raw.Replace("\r\n","\n");
 Check(owner._file.PlayerAppearance==normalized&&owner.GetPlayerAppearanceForPrompt()==normalized,"editor writes and prompt reads the same player-only field");
 Check(touches==1&&returns==1,"changed appearance invalidates recall cache once");
 owner.OpenPlayerPersonaAppearanceEditor(()=>returns++);Check(DevTextEditorHelper.Initial==normalized,"saved text is reloaded for editing");DevTextEditorHelper.Confirm(normalized);Check(touches==1,"unchanged value does not invalidate caches");
 foreach(string npc in new[]{"NPC_A_{NAME}","NPC_B"}){
  string context=BuildPermanentPlayerAppearanceContext(npc,owner.GetPlayerAppearanceForPrompt());
  Check(context.Contains(normalized),"literal player wording remains unchanged");
  Check(context.Contains(npc),"observer substitution is literal");
  Check(context.Contains(en?"belongs only to the player":"只描述玩家"),"subject is explicitly the player");
  Check(context.Contains(en?"First-person wording":"第一人称"),"first-person wording cannot mean the observer");
  Check(context.EndsWith(en?"[End of player appearance]":"【玩家外貌信息结束】"),"appearance has a closing boundary");
 }
 Check(BuildPermanentPlayerAppearanceContext("NPC"," \n ")=="","empty appearance emits no block");
 owner.OpenPlayerPersonaAppearanceEditor(()=>returns++);DevTextEditorHelper.Cancel();Check(owner.GetPlayerAppearanceForPrompt()==normalized,"cancel retains player text");
 Console.WriteLine("PASS "+args[1]+" "+checks+" appearance editor/reader/resource checks; no save/game/provider.");return 0;
}catch(Exception e){Console.WriteLine(e);return 1;}}
}}
'''.replace("@@METHODS@@", methods)
(out / "Program.cs").write_text(program, encoding="utf-8")
newtonsoft = dotnet.parent / "sdk/8.0.425/Newtonsoft.Json.dll"
(out / "Proof.csproj").write_text(f'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>{newtonsoft.as_posix()}</HintPath></Reference></ItemGroup></Project>')
(out / "NuGet.Config").write_text("<configuration><packageSources><clear/></packageSources></configuration>")
env = minimal_test_environment(dotnet, out)
build = subprocess.run([str(dotnet), "build", str(out / "Proof.csproj"), "-c", "Release", "-v:q", "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
(out / "build.log").write_text(build.stdout + build.stderr, encoding="utf-8")
if build.returncode: print(build.stdout + build.stderr); raise SystemExit(build.returncode)
for language, filename in (("zh-CN", "KnowledgeRagPromptTexts.json"), ("en", "KnowledgeRagPromptTexts.en.json")):
    package = out / language; package.mkdir()
    (package / "KnowledgeRagPromptTexts.json").write_bytes((resources / filename).read_bytes())
    result = subprocess.run([str(dotnet), str(out / "bin/Release/net8.0/Proof.dll"), str(package), language], env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
    (out / (language + ".log")).write_text(result.stdout + result.stderr, encoding="utf-8")
    print(result.stdout + result.stderr)
    if result.returncode: raise SystemExit(result.returncode)
