"""Compile detached Prompt owner and real history DTO; isolated, no game/provider access."""
from pathlib import Path
import argparse, os, subprocess, sys
ROOT=Path(__file__).resolve().parents[4]
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
import scene_layout_review
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path);p.add_argument('--mutate',choices=['drop-fact','wrong-role','drop-scene-fact','reorder-scene-history']);a=p.parse_args()
out=new_run_root(ROOT,'message-assembly',a.run_root)
source=ROOT/'src/modules/AF.Module.Prompt/Composition/MainPromptMessageAssemblyOwner.cs'
if not source.exists(): raise SystemExit('FAIL required production owner missing')
owner=source
if a.mutate:
 text=source.read_text(encoding='utf-8-sig')
 old,new={'drop-fact':('metadata + "【过往行为】" + StripCourierPromptScopeLabel(content)','metadata + "lost fact"'), 'wrong-role':('CreateCourierChatMessage("assistant", metadata + StripCourierSpeakerPrefix(content, npcName))','CreateCourierChatMessage("user", metadata + StripCourierSpeakerPrefix(content, npcName))'), 'drop-scene-fact':('reaction = BuildSceneCompositeUserBlock("", reaction, currentFact);','reaction = BuildSceneCompositeUserBlock("", reaction);'), 'reorder-scene-history':('return new[] { privateRecent, persistedHistory, runtime, local, currentFact, trust, misc, patience, ruleBlock };','return new[] { persistedHistory, privateRecent, runtime, local, currentFact, trust, misc, patience, ruleBlock };')}[a.mutate]
 assert text.count(old)==1
 owner=out/'MutatedOwner.cs';owner.write_text(text.replace(old,new,1),encoding='utf-8')
(out/'SceneCallSites.cs').write_text(scene_layout_review.callsite_harness(),encoding='utf-8')
files=[out/'SceneCallSites.cs',owner,ROOT/'src/modules/AF.Module.Conversation/Internal/History/ConversationMessage.cs',ROOT/'src/modules/AF.Module.Prompt/Composition/ConversationRoleClassificationOwner.cs',Path(__file__).with_name('MessageAssemblyChecks.cs')]
xml='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+str(f)+'"/>' for f in files)+'</ItemGroup></Project>'
(out/'Checks.csproj').write_text(xml);(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet=resolve_dotnet(ROOT)
env=minimal_test_environment(dotnet,out)
build=subprocess.run([str(dotnet),'build',str(out/'Checks.csproj'),'-c','Release','--nologo','-p:RestoreConfigFile='+str(out/'NuGet.Config')],cwd=out,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180)
run=subprocess.run([str(dotnet),str(out/'bin/Release/net8.0/Checks.dll')],cwd=out,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=60) if build.returncode==0 else None
log=build.stdout+build.stderr+((run.stdout+run.stderr) if run else '')
(out/'run.log').write_text(log,encoding='utf-8');print(log)
raise SystemExit(build.returncode or (run.returncode if run else 0))
