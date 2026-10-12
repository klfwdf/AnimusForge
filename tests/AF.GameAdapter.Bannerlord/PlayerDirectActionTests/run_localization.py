"""Actual Conversation UI reader + registered XML on both managed TW API lines.

This does not render Gauntlet or run a game. Dynamic error projection remains a
leaf substitute; the new map texts do not contain generated/provider errors.
"""
from pathlib import Path
import json
import re
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

out = new_run_root(ROOT, "manual-worldmap-localization", None)
catalog = out / "catalog"
entries = json.loads((ROOT / "content/content-map.json").read_text(encoding="utf-8-sig"))["entries"]
for entry in entries:
    if entry["target"].startswith("ModuleData/Languages/") and entry["target"].endswith(".xml"):
        target = catalog / entry["target"]
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(ROOT / entry["source"], target)

sources = [ROOT / "src/AF.GameAdapter.Bannerlord/Conversation/PlayerDirectActionController.cs",
           ROOT / "src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.PlayerDirectActions.cs",
           ROOT / "src/modules/AF.Module.WorldMap/Runtime/WorldMapPartyCommandBehavior.PlayerDirectActions.cs"]
expected = {}
for source in sources:
    for key, text in re.findall(r'(?:MapText|DirectOrderText)\("([^"]+)",\s*"((?:\\.|[^"\\])*)"', source.read_text(encoding="utf-8-sig")):
        value = json.loads('"' + text + '"')
        assert key not in expected or expected[key] == value, key
        expected[key] = value
english = {x.attrib["id"]: x.attrib["text"] for x in ET.parse(catalog / "ModuleData/Languages/afconv_ui_strings.xml").iter("string")}
chinese = {x.attrib["id"]: x.attrib["text"] for x in ET.parse(catalog / "ModuleData/Languages/CNs/afconv_ui_strings-zh-CN.xml").iter("string")}
for key, text in expected.items():
    identity = "AF_CONV_UI_Direct_Map_" + key
    assert english[identity] == text, identity + ": fallback differs from resource"
    assert identity in chinese and re.findall(r"\{[A-Z_]+\}", text) == re.findall(r"\{[A-Z_]+\}", chinese[identity]), identity
    assert not re.search(r"[\u3400-\u9fff]", text), identity
for index, filename in [("language_data.xml", "afconv_ui_strings.xml"), ("CNs/language_data.xml", "CNs/afconv_ui_strings-zh-CN.xml")]:
    assert any(x.attrib["xml_path"] == filename for x in ET.parse(catalog / "ModuleData/Languages" / index).iter("LanguageFile"))

program = r'''using System;using System.Collections.Generic;using System.Linq;using System.IO;using System.Xml.Linq;
using AnimusForge;using TaleWorlds.Localization;
namespace AnimusForge {internal static class LlmRetryPrompt {internal static string ProjectKnownErrorForUi(string value)=>value;}}
class Program {
static void Check(bool pass,string name){if(!pass)throw new Exception(name);count++;}static int count;
static void Main(string[] args){
LocalizedTextManager.LoadLocalizationXmls(new[]{args[0]});
Dictionary<string,string> Read(string file)=>XDocument.Load(Path.Combine(args[0],"ModuleData/Languages",file)).Descendants("string")
.Where(x=>((string)x.Attribute("id")).StartsWith("AF_CONV_UI_Direct_Map_")).ToDictionary(x=>(string)x.Attribute("id"),x=>(string)x.Attribute("text"));
var en=Read("afconv_ui_strings.xml");var cn=Read("CNs/afconv_ui_strings-zh-CN.xml");
foreach(var language in new[]{"English","简体中文","English"}){
Check(MBTextManager.ChangeLanguage(language),"language switch");
foreach(var pair in en){
var expected=language=="English"?pair.Value:cn[pair.Key];
var variables=System.Text.RegularExpressions.Regex.Matches(pair.Value,@"\{([A-Z_]+)\}").Select(x=>(x.Groups[1].Value,(object)"probe")).ToArray();
foreach(var variable in variables)expected=expected.Replace("{"+variable.Item1+"}","probe");
Check(ConversationUiLocalization.Text(pair.Key,pair.Value,variables)==expected,"actual reader "+language+" "+pair.Key);}
foreach(var literal in new[]{"{ACTOR}","{=AF_CONV_UI_Direct_Map_Go}literal","$1","AFCONVUILITERAL0END",null}){
var result=ConversationUiLocalization.Text("AF_CONV_UI_Direct_Map_Confirm",en["AF_CONV_UI_Direct_Map_Confirm"],
("ACTOR",literal),("TARGET","Same [town]"),("DAYS",15),("MODE","AI"),("QUEUE","append"));
var expected=(language=="English"?en:cn)["AF_CONV_UI_Direct_Map_Confirm"].Replace("{ACTOR}",literal??"").Replace("{TARGET}","Same [town]")
.Replace("{DAYS}","15").Replace("{MODE}","AI").Replace("{QUEUE}","append");
Check(result==expected,"confirmation preserves literal actor identity");}}
Console.WriteLine("PASS real TW conversation map localization "+count+" checks");}}
'''
results = []
dotnet = resolve_dotnet(ROOT)
for api in ["1.3", "1.4"]:
    project = out / api
    project.mkdir()
    deps = ROOT / ".tmp/build_check" / api
    assert (deps / "TaleWorlds.Localization.dll").is_file(), "Missing verified reference line " + api
    (project / "Program.cs").write_text(program, encoding="utf-8")
    reader = ROOT / "src/AF.GameAdapter.Bannerlord/UI/Conversation/ConversationUiLocalization.cs"
    project_text = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup><ItemGroup><Compile Include="' + escape(str(reader)) + '" />'
    for name in ["TaleWorlds.Localization", "TaleWorlds.Library"]:
        project_text += '<Reference Include="' + name + '"><HintPath>' + escape(str(deps / (name + ".dll"))) + '</HintPath></Reference>'
    (project / "Probe.csproj").write_text(project_text + "</ItemGroup></Project>", encoding="utf-8")
    (project / "NuGet.Config").write_text("<configuration><packageSources><clear/></packageSources></configuration>", encoding="utf-8")
    result = subprocess.run([str(dotnet), "run", "--project", str(project / "Probe.csproj"), "-c", "Release", "--", str(catalog)],
                            cwd=project, env=minimal_test_environment(dotnet, project), capture_output=True, text=True, encoding="utf-8", errors="replace")
    (project / "run.log").write_text(result.stdout + result.stderr, encoding="utf-8")
    print(api, result.stdout + result.stderr)
    results.append({"api": api, "exit": result.returncode})
(out / "receipt.json").write_text(json.dumps({"pairedKeys": len(expected), "managedTaleWorlds": True, "gauntletLive": "NOT_RUN", "results": results}, indent=2), encoding="utf-8")
print("Evidence:", out)
sys.exit(0 if all(x["exit"] == 0 for x in results) else 1)
