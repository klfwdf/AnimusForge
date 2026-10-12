"""Compile two real consumers with the production package reader; no game/model call."""
from pathlib import Path
import importlib.util
import json
import re
import shutil
import subprocess
import sys
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
spec = importlib.util.spec_from_file_location('extract', ROOT / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
corpus = ROOT / 'content/foundation/AF.Foundation.Localization/ModuleData'
packs = {lang: json.loads((corpus / f'AdapterPromptCapture.{lang}.json').read_text(encoding='utf-8-sig')) for lang in ('zh-CN', 'en')}
adapter=(ROOT/'src/AF.GameAdapter.Bannerlord/Composition/PersonaEquipmentPromptCaptureAdapter.cs').read_text(encoding='utf-8-sig')
external=(ROOT/'src/AF.GameAdapter.Bannerlord/Composition/SceneExternalPromptCaptureAdapter.cs').read_text(encoding='utf-8-sig')
condition=adapter[adapter.index('if (!string.IsNullOrWhiteSpace(liegeDisplay)'):adapter.index('\n\t\tstring heroName')]
date=adapter[adapter.index('\t\t\tif (currentDateText.StartsWith('):adapter.index('\n\t\t}\n\t\tcatch',adapter.index('\t\t\tif (currentDateText.StartsWith('))]
reputation=re.search(r'string reputationText = (.*);',adapter).group(1)
fallback=external[external.index('if (string.IsNullOrWhiteSpace(playerName))'):external.index('\n\t\tstring layered')]
clan=re.search(r'if \(!string.IsNullOrWhiteSpace\(clan\) && clan != .*?\n\t\t\t}',adapter,re.S).group(0)
assert not re.search(r'[\u4e00-\u9fff]',adapter+external),'owned source literals cleared by genuine resources'
program=r"""
using System;using AnimusForge;
namespace AnimusForge {
internal static class AnimusForgeModulePaths { internal static string GetModuleDataFilePath(string name)=>System.IO.Path.Combine(Environment.GetEnvironmentVariable("AF_TEST_PACKAGE"),name); }
internal static class Consumer {
internal static string Liege(string factionDisplay,string liegeDisplay) { @@CONDITION@@ return factionDisplay; }
internal static string Date(string currentDateText) { @@DATE@@ return currentDateText; }
internal static string Reputation(string label,int clanTier) { string reputationText=@@REPUTATION@@; return reputationText; }
internal static string GetClanTierReputationLabel(int tier)=>Label;
internal static string Label;
internal static string Player(string playerName) { @@FALLBACK@@ return playerName; }
internal static string Clan(string clan) { string clanSuffix=AdapterPromptCaptureTexts.PcapText("equipment_clan_suffix"); @@CLAN@@ return clan; }
}}
internal static class Program {
static int checks;static void C(bool b,string m){checks++;if(!b)throw new Exception(m);}
static void Main(string[] args) {
bool en=args[0]=="en";
foreach(var excluded in new[]{"无","Ruler（本人）","None","Ruler(self)",""," "}) C(Consumer.Liege("Faction",excluded)=="Faction","old/new exclusion preserved");
string name="中文 {LEVEL} $1";
C(Consumer.Liege("Faction",name)=="Faction"+(en?" (allegiance: ":"（效忠：")+name+(en?")":"）"),"liege frame and literal name");
Consumer.Label=name;
C(Consumer.Reputation(name,3)==name+(en?" (3 level)":"（3 level）"),"package level format preserves dynamic data");
C(Consumer.Reputation(name,-1)==name+(en?" (0 level)":"（0 level）"),"negative tier still clamped");
C(Consumer.Date("当前游戏日期： 1084 春 （details）")=="1084 春","Chinese raw date prefix/parens preserved");
C(Consumer.Date("Current game date: 1084 spring (details)")=="1084 spring","English prefix/parens preserved");
C(Consumer.Date("Date {LEVEL} $1")=="Date {LEVEL} $1","unknown date content preserved");
C(Consumer.Player(null)==(en?"the player":"玩家"),"package fallback player name");
C(Consumer.Player(name)==name,"real player name preserved");
C(Consumer.Clan("无家族")=="无家族","no-clan sentinel unchanged before snapshot projection");
C(Consumer.Clan("中文家族")=="中文家族","old Chinese suffix retained as real identity");
C(Consumer.Clan(name)==name+(en?" clan":"家族"),"clan frame package choice");
Console.WriteLine("PASS Adapter equipment gate "+args[0]+" checks="+checks+"; production reader and capture snippets");
}}
""".replace('@@CONDITION@@',condition).replace('@@DATE@@',date).replace('@@REPUTATION@@',reputation).replace('@@FALLBACK@@',fallback).replace('@@CLAN@@',clan)
out = new_run_root(ROOT, 'adapter-equipment-gate', None)
(out / 'Program.cs').write_text(program, encoding='utf-8')
reader = ROOT / 'src/AF.GameAdapter.Bannerlord/Prompt/AdapterPromptCaptureTexts.cs'
compatibility = corpus / 'AdapterPromptCaptureCompatibility.json'
newtonsoft = ROOT / 'local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll'
project = f'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><Compile Include="{escape(str(reader))}"/><Reference Include="Newtonsoft.Json"><HintPath>{escape(str(newtonsoft))}</HintPath></Reference><EmbeddedResource Include="{escape(str(compatibility))}" LogicalName="AnimusForge.AdapterPromptCapture.Compatibility.json"/></ItemGroup></Project>'
(out / 'Replay.csproj').write_text(project, encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
dotnet = resolve_dotnet(ROOT); env = minimal_test_environment(dotnet, out)
build = subprocess.run([str(dotnet), 'build', str(out / 'Replay.csproj'), '-c', 'Release'], cwd=out, env=env, capture_output=True, text=True, encoding='utf-8', errors='replace')
(out / 'build.log').write_text(build.stdout + build.stderr, encoding='utf-8')
if build.returncode: print(build.stdout+build.stderr);sys.exit(build.returncode)
for language in packs:
    package = out / language;package.mkdir()
    shutil.copyfile(corpus / f'AdapterPromptCapture.{language}.json',package/'AdapterPromptCapture.json')
    result = subprocess.run([str(dotnet),str(out/'bin/Release/net8.0/Replay.dll'),language],cwd=out,env=dict(env,AF_TEST_PACKAGE=str(package)),capture_output=True,text=True,encoding='utf-8',errors='replace')
    (out/(language+'.log')).write_text(result.stdout+result.stderr,encoding='utf-8');print(result.stdout+result.stderr,end='')
    if result.returncode:sys.exit(result.returncode)
print('Evidence:',out)
