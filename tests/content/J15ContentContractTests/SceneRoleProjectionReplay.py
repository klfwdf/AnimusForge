"""Replay known RoleDesc values through the actual package reader, without game objects."""
import json
import os
from pathlib import Path
import subprocess
import sys
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(sys.argv[1]).resolve()
OUT.mkdir(parents=True, exist_ok=False)
BASE = ROOT / "content/foundation/AF.Foundation.Localization/ModuleData"
RAW = ROOT / "content/modules/AF.Module.Conversation/ModuleData/ConversationRuntimeCompatibility.json"
roles = json.loads(RAW.read_text(encoding="utf-8-sig"))
known = json.loads((BASE / "AdapterPromptCaptureCompatibility.json").read_text(encoding="utf-8-sig"))
keys = {v: k.removeprefix("Alias_") for k, v in known.items()
        if k.startswith(("Project_", "Alias_Project_"))}
missing = [roles[f"AF_CONV_Runtime_{i:04}"] for i in range(878, 903)
           if roles[f"AF_CONV_Runtime_{i:04}"] not in keys]
assert not missing, f"Unprojected built-in RoleDesc: {missing}"
dotnet = ROOT / "local/dotnet/8.0.425/dotnet.exe"
reader = ROOT / "src/AF.GameAdapter.Bannerlord/Prompt/AdapterPromptCaptureTexts.cs"
newtonsoft = dotnet.parent / "sdk/8.0.425/Newtonsoft.Json.dll"
def x(path):
    return escape(str(path))
(OUT / "Replay.csproj").write_text(f'''<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup>
<ItemGroup><Compile Include="{x(reader)}" Link="ActualReader.cs"/>
<Reference Include="Newtonsoft.Json"><HintPath>{x(newtonsoft)}</HintPath></Reference>
<EmbeddedResource Include="{x(BASE / 'AdapterPromptCaptureCompatibility.json')}" LogicalName="AnimusForge.AdapterPromptCapture.Compatibility.json"/>
</ItemGroup></Project>''', encoding="utf-8")
(OUT / "NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding="utf-8")
(OUT / "Program.cs").write_text('''using System;using System.IO;using Newtonsoft.Json;using System.Collections.Generic;
namespace AnimusForge {
internal static class AnimusForgeModulePaths { internal static string GetModuleDataFilePath(string name)=>Path.Combine(Environment.GetEnvironmentVariable("AF_ROLE_PACKAGE"),name); }
internal static class Program {static void Main(string[] args) {
var raw=JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText(args[0]));
var expected=JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText(args[1]));
foreach(var pair in expected) if(AdapterPromptCaptureTexts.PcapProjectKnown(pair.Key)!=pair.Value)throw new Exception("Role projection: "+pair.Key);
foreach(var original in new[]{"Modded Bard 角色", "Hur", "player authored text", "", null})
if(AdapterPromptCaptureTexts.PcapProjectKnown(original)!=original)throw new Exception("Unknown value changed");
Console.WriteLine("PASS actual reader: "+expected.Count+" roles; unknown/name/null values preserved");
}}}''', encoding="utf-8")
for lang in ("zh-CN", "en"):
    dest = OUT / lang
    dest.mkdir()
    text = (BASE / f"AdapterPromptCapture.{lang}.json").read_text(encoding="utf-8-sig")
    (dest / "AdapterPromptCapture.json").write_text(text, encoding="utf-8")
    values = json.loads(text)
    expected = {roles[f"AF_CONV_Runtime_{i:04}"]: values[keys[roles[f"AF_CONV_Runtime_{i:04}"]]] for i in range(878, 903)}
    (dest / "expected.json").write_text(json.dumps(expected, ensure_ascii=False), encoding="utf-8")
    subprocess.run([str(dotnet), "run", "--project", str(OUT / "Replay.csproj"),
                    "--", str(RAW), str(dest / "expected.json")], check=True,
                   env=dict(os.environ, AF_ROLE_PACKAGE=str(dest)))
