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
memory = (ROOT / 'src/AF.GameAdapter.Bannerlord/Memory/MemorySummaryApplicationAdapter.cs').read_text(encoding='utf-8-sig')
scene = (ROOT / 'src/AF.GameAdapter.Bannerlord/Prompt/SceneMechanismPromptCaptureAdapter.cs').read_text(encoding='utf-8-sig')
methods = extract.declaration(memory, 'internal static string BuildMemoryOverviewContextById(') + '\n' + extract.declaration(scene, 'internal static void AppendSceneUnifiedTargetPromptSection(')
assert packs['zh-CN']['memory_overview_header'] == '【过往记忆总览】\n'
assert packs['zh-CN']['scene_guide_summon_list_header'] == '【带路与传唤NPC清单】：'
encounter = (ROOT / 'src/AF.GameAdapter.Bannerlord/Encounter/LordEncounterBehavior.cs').read_text(encoding='utf-8-sig')
calls = dict(re.findall(r'PcapFormat\("(encounter_release_(?:inactive_scene|inactive_turn|negotiable|forbidden))", ("(?:[^"\\]|\\.)*")', encounter))
assert len(calls) == 4
for key, fallback in calls.items():
    assert json.loads(fallback) == packs['en'][key], key
    assert sorted(re.findall(r'\{[A-Z_]+\}|\[ACTION:[^\]]+\]', packs['en'][key])) == sorted(re.findall(r'\{[A-Z_]+\}|\[ACTION:[^\]]+\]', packs['zh-CN'][key])), key

program = r'''
using System;using System.Collections.Generic;using System.Text;using AnimusForge;
namespace AnimusForge {
internal static class AnimusForgeModulePaths { internal static string GetModuleDataFilePath(string name)=>System.IO.Path.Combine(Environment.GetEnvironmentVariable("AF_TEST_PACKAGE"),name); }
internal class SceneSummonPromptTarget { public int PromptId; public string DisplayName; public string LocationCode; }
internal class SceneGuidePromptTarget { public int PromptId; public string DisplayName; public string LocationCode; }
internal class CompressedMemoryBlock {}
internal class MemoryOverviewState { public string LastError; public string Summary; }
internal static class MemoryRecordRules { internal static string NormalizeMemoryHeroId(string s)=>s; }
internal class MemoryBusinessStateOwner {
 internal MemoryOverviewState State=new MemoryOverviewState(); internal List<CompressedMemoryBlock> Blocks=new List<CompressedMemoryBlock>(); internal bool Pending;
 internal List<CompressedMemoryBlock> LoadBlocks(string id)=>Blocks;
 internal MemoryOverviewState GetMemoryOverviewState(string id)=>State;
 internal bool HasMemoryOverviewPendingBlocks(string id,List<CompressedMemoryBlock> blocks)=>Pending;
}
internal static class Consumers { @@METHODS@@ }
}
internal static class Program {
 static int checks; static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static void Main(string[] args){
  bool en=args[0]=="en";
  string data="Lady {NAME} $1 中文";
  var memory=new MemoryBusinessStateOwner();memory.State.Summary=data;
  string prefix=en?"[Past-memory overview]\n":"【过往记忆总览】\n";
  Check(Consumers.BuildMemoryOverviewContextById(memory,"id",_=>true,()=>1)==prefix+data,"ready overview translates frame only");
  Check(memory.State.Summary==data,"overview raw data never overwritten");
  Check(Consumers.BuildMemoryOverviewContextById(memory,"id",_=>false,()=>1)=="","ineligible overview empty");
  memory.State.LastError="failure";memory.Pending=true;memory.Blocks.Add(new CompressedMemoryBlock());
  string pending=Consumers.BuildMemoryOverviewContextById(memory,"id",_=>true,()=>1);
  Check(pending.StartsWith(prefix)&&!pending.Contains(data),"pending overview keeps failure exclusion");
  var sb=new StringBuilder();
  var summon=new List<SceneSummonPromptTarget>{new SceneSummonPromptTarget{PromptId=1,DisplayName=data,LocationCode="tavern"},new SceneSummonPromptTarget{PromptId=0,DisplayName="invalid"}};
  var guide=new List<SceneGuidePromptTarget>{new SceneGuidePromptTarget{PromptId=1,DisplayName="duplicate"},new SceneGuidePromptTarget{PromptId=2,DisplayName="Other",LocationCode="keep"}};
  Consumers.AppendSceneUnifiedTargetPromptSection(sb,summon,guide);
  string output=sb.ToString().Replace("\r\n","\n");
  Check(output==(en?"[Guide & Summon NPC List]:\n":"【带路与传唤NPC清单】：\n")+"1 "+data+" tavern\n2 Other keep\n","target order/id filtering and literal names");
  Check(AdapterPromptCaptureTexts.PcapText("rule_current_following").Contains(en?"[Guide & Summon NPC List]":"【带路与传唤NPC清单】"),"instruction names the supplied target list");
  sb.Clear();Consumers.AppendSceneUnifiedTargetPromptSection(sb,null,null);Check(sb.Length==0,"no targets creates no list");
  Check(AdapterPromptCaptureTexts.PcapText("referral_header").StartsWith(en?"[Referral]":"【转介】"),"referral detector prefix");
  foreach(string key in new[]{"encounter_release_inactive_scene","encounter_release_inactive_turn","encounter_release_negotiable","encounter_release_forbidden"}){
   string text=AdapterPromptCaptureTexts.PcapFormat(key,"",("PLAYER",data),("QUOTE","Quote $1 {PLAYER}"));
   Check(text.StartsWith(en?"[Encounter release rules]":"【遭遇放走规则】")&&text.Contains(data),"encounter package frame and literal player");
  }
  Console.WriteLine("PASS Adapter prompt headings "+args[0]+" checks="+checks+"; actual reader + consumers, game leaves synthetic");
 }
}
'''.replace('@@METHODS@@', methods)
out = new_run_root(ROOT, 'adapter-prompt-headings', None)
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
