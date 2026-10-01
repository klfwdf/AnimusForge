"""Actual complete system speech body, shared effect ports, dispatcher and registry."""
import argparse,importlib.util,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
p=argparse.ArgumentParser();p.add_argument('--run-root',required=True,type=Path);p.add_argument('--mutation',choices=['force-queue','registry-claim','mission','generation','epoch','session','owner','agent-reference','history-order']);a=p.parse_args()
src=ROOT/'src/modules/AF.Module.Conversation/Channels/Scene';native=ROOT/'src/modules/AF.Module.Conversation/Channels/Native'
runtime=(src/'SceneSystemNpcShoutRuntime.cs').read_text(encoding='utf-8-sig')
dispatcher=(native/'ConversationGameThreadDispatcher.cs').read_text(encoding='utf-8-sig')
mutations={
 'force-queue':('forceQueue: true','forceQueue: false'),
 'mission':('!ReferenceEquals(Mission.Current, sourceMission)','false'),
 'generation':('!SaveRuntimeGuard.IsCurrentGeneration(generation)','false'),
 'epoch':('epoch != _conversationEpoch()','false'),
 'session':('sessionId != _sceneSessionId()','false'),
 'owner':('!_isOwnerCurrent()','false'),
 'agent-reference':('!ReferenceEquals(agent, speakerAgent)','false'),
 'history-order':('_ports.RecordResponseForAllNearbySafe(allNpcData, speakerData.AgentIndex, speakerData.Name, aiResponse);\n\t\t\t\t\t\t_ports.PersistNpcSpeechToNamedHeroes(speakerData.AgentIndex, speakerData.Name, aiResponse, allNpcData);','_ports.PersistNpcSpeechToNamedHeroes(speakerData.AgentIndex, speakerData.Name, aiResponse, allNpcData);\n\t\t\t\t\t\t_ports.RecordResponseForAllNearbySafe(allNpcData, speakerData.AgentIndex, speakerData.Name, aiResponse);')}
if a.mutation=='registry-claim':
 old='if (Interlocked.CompareExchange(ref state, 1, 0) != 0) return;';assert dispatcher.count(old)==1;dispatcher=dispatcher.replace(old,'if (false) return;',1)
elif a.mutation:
 old,new=mutations[a.mutation];assert runtime.count(old)==1;runtime=runtime.replace(old,new,1)
effect=(src/'SceneSpeechEffectController.cs').read_text(encoding='utf-8-sig');effect=effect[effect.index('internal sealed class SceneSpeechEffectController'):]
item=ex.declaration((src/'ShoutBehavior.SpeechExecution.cs').read_text(encoding='utf-8-sig'),'internal sealed class SceneSpeechQueueItem')
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8').replace('@@EFFECT@@',effect).replace('@@ITEM@@',item)
out=new_run_root(ROOT,'system-npc-shout-runtime',a.run_root)
modules=ex.declaration(code,'internal static class TeamModuleServices')
code=code.replace(modules,'',1)
(out/'ModuleGameLeaves.cs').write_text('using System;using System.Collections.Generic;using TaleWorlds.CampaignSystem;namespace AnimusForge.Refactor.Modules { '+modules+' }',encoding='utf-8')
library=[]
for sig in ['internal class Color','internal class InformationMessage','internal static class InformationManager']:
 leaf=ex.declaration(code,sig);library.append(leaf);code=code.replace(leaf,'',1)
(out/'LibraryGameLeaves.cs').write_text('namespace TaleWorlds.Library { '+'\n'.join(library).replace('SystemProbe.Atom','AnimusForge.SystemProbe.Atom')+' }',encoding='utf-8')
code='using AnimusForge.Refactor.Modules;using TaleWorlds.Library;\n'+code
(out/'Program.cs').write_text(code,encoding='utf-8')
# Keep the actual namespace imports: game substitutes live in their real namespaces.
(out/'SystemRuntime.cs').write_text(runtime,encoding='utf-8')
(out/'Dispatcher.cs').write_text(dispatcher,encoding='utf-8')
(out/'Registry.cs').write_bytes((ROOT/'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs').read_bytes())
for name in ['SceneSpeechEffectPorts.cs','SceneSpeechCompletionController.cs','SceneSpeechQueueOwner.cs','SceneSpeechExecutionRuntime.cs']:
 (out/name).write_bytes((src/name).read_bytes())
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649;CS0169;CS0414</NoWarn></PropertyGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
dotnet=resolve_dotnet(ROOT);result=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90)
log=result.stdout+result.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(result.returncode)
