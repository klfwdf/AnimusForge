"""Replay startup order, SETS/native registry, intimacy qualification and EN native literals. No network/game."""
from pathlib import Path
import argparse,hashlib,importlib.util,json,subprocess,sys
from xml.sax.saxutils import escape
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path);a=p.parse_args();out=new_run_root(ROOT,'scene-repair-boundary',a.run_root)
spec=importlib.util.spec_from_file_location('extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
inputs={}
def read(p):
 data=(ROOT/p).read_bytes();inputs[p]=hashlib.sha256(data).hexdigest();return data.decode('utf-8-sig')
def method(p,s):return ex.declaration(read(p),s)
core=ROOT/'extensions/AnimusForge.XihaiAction/src/CoreProject'
items=''
for path in core.glob('*.cs'):
 read(path.relative_to(ROOT).as_posix());items+='<Compile Include="'+escape(str(path))+'" />'
for filename,logical in [('SceneActionVocabulary.xml','AnimusForge.Defaults.SceneActionVocabulary.xml'),('Languages/afscene_catalog_strings.xml','AnimusForge.SceneActions.Catalog.en.xml'),('Languages/CNs/afscene_catalog_strings-zh-CN.xml','AnimusForge.SceneActions.Catalog.zh-CN.xml'),('Languages/afscene_speech_strings.xml','AnimusForge.SceneActions.Speech.en.xml'),('Languages/CNs/afscene_speech_strings-zh-CN.xml','AnimusForge.SceneActions.Speech.zh-CN.xml')]:
 path='content/modules/AnimusForge.XihaiAction/ModuleData/'+filename;read(path);items+='<EmbeddedResource Include="'+escape(str(ROOT/path))+'" LogicalName="'+logical+'" />'
text=read('src/AF.GameAdapter.Bannerlord/Prompt/NativePromptLiteralProjection.cs');(out/'NativeLiteral.cs').write_text(text,encoding='utf-8')
social='src/modules/AF.Module.Social/Host/SexualConceptionBehavior.cs'
methods=method(social,'public static List<PostprocessRuleEntry> BuildRuntimePostprocessRules(')+method(social,'public static string NormalizePostprocessTags(')
(out/'ActualIntimacy.cs').write_text('using System;using System.Collections.Generic;using System.Linq;using System.Text.RegularExpressions;namespace AnimusForge;static partial class SexualConceptionBehavior {'+methods+'}',encoding='utf-8')
sets='src/AF.GameAdapter.Bannerlord/SettlementEntry/SettlementEntryTroopSelectionBehavior.cs'
controller='src/AF.GameAdapter.Bannerlord/SceneActions/SceneActionDirectiveController.cs'
(out/'ActualDirective.cs').write_text('using System;using System.Collections.Generic;using System.Linq;using AnimusForge.SceneActions.Core;namespace AnimusForge;static class SceneActionDirectiveController {'+method(controller,'internal static List<PostprocessRuleEntry> BuildSceneActionDirectiveRules(')+'}',encoding='utf-8')
(out/'ActualSets.cs').write_text('using System;namespace AnimusForge;partial class SetsOwner{'+method(sets,'internal void EnsureCivilianUsableDetachmentRegistered(')+'}',encoding='utf-8')
(out/'Program.cs').write_text((HERE/'Program.cs.txt').read_text('utf-8'),encoding='utf-8')
(out/'Stubs.cs').write_text((HERE/'Stubs.cs.txt').read_text('utf-8'),encoding='utf-8')
(out/'Replay.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NoWarn>0649</NoWarn></PropertyGroup><ItemGroup>'+items+'</ItemGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
for lang,suffix in [('cn',''),('en','.en')]:
 source='content/modules/AnimusForge.XihaiAction/ModuleData/SceneActionPromptTexts'+suffix+'.xml';(out/('scene-'+lang+'.xml')).write_text(read(source),encoding='utf-8')
 source='content/modules/AF.Module.Prompt/ModuleData/PromptComposerTexts'+('.zh-CN' if lang == 'cn' else '.en')+'.json';(out/('prompt-'+lang+'.json')).write_text(read(source),encoding='utf-8')
 source='content/modules/AF.Module.Social/ModuleData/SocialPromptTexts'+suffix+'.json';(out/('social-'+lang+'.json')).write_text(read(source),encoding='utf-8')
dotnet=resolve_dotnet(ROOT);env=minimal_test_environment(dotnet,out)
r=subprocess.run([str(dotnet),'build',str(out/'Replay.csproj'),'-c','Release'],cwd=out,env=env,capture_output=True,text=True,encoding='utf-8',timeout=120);(out/'build.log').write_text(r.stdout+r.stderr,encoding='utf-8')
if r.returncode:print(r.stdout+r.stderr);raise SystemExit(r.returncode)
for lang in ['cn','en']:
 r=subprocess.run([str(dotnet),str(out/'bin/Release/net8.0/Replay.dll'),str(out/('scene-'+lang+'.xml')),str(out/('social-'+lang+'.json')),lang],cwd=out,env=env,capture_output=True,text=True,encoding='utf-8',timeout=30);(out/(lang+'.log')).write_text(r.stdout+r.stderr,encoding='utf-8');print(r.stdout+r.stderr)
 if r.returncode:raise SystemExit(r.returncode)
assert all(hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==h for p,h in inputs.items())
(out/'inputs.json').write_text(json.dumps({'hashes':inputs,'uncovered':['native in-game animation','live pregnancy/provider classification','equipment exception without caller stack']},indent=2),encoding='utf-8')
