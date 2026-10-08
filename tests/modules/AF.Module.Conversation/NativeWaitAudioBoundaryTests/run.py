import argparse, hashlib, json, os, re, shutil, subprocess, sys
from pathlib import Path
parser=argparse.ArgumentParser()
parser.add_argument('--run-root',required=True)
parser.add_argument('--bannerlord-api',choices=['1.3','1.4'],default='1.3')
a=parser.parse_args()
here=Path(__file__).resolve().parent
repo=next((x for x in here.parents if (x/'AGENTS.md').is_file()),None)
if repo is None: raise SystemExit('workspace AGENTS.md missing')
out=Path(a.run_root).resolve()
if out==repo or not out.is_relative_to(repo): raise SystemExit('run-root must be a fresh workspace child')
if out.exists() and any(out.iterdir()): raise SystemExit('run-root must be empty; no cleanup is performed')
out.mkdir(parents=True,exist_ok=True)
proof=json.loads((here/'source-proof.json').read_text(encoding='utf-8'))
fixture=(here/'NativeAtomicConsumer.cs').read_text(encoding='utf-8')
observed_proof=[]
for item in proof:
    relative=Path(item['path'])
    if relative.is_absolute() or '..' in relative.parts: raise SystemExit('source-proof path must be workspace-relative')
    source_path=repo/relative
    source=subprocess.check_output(['git','show','1235727e44113f3e559a59c77bd88851e195a689:'+relative.as_posix()],cwd=repo).decode('utf-8-sig').replace('\r\n','\n')
    observed_proof.append(dict(item,currentSourceSha256=hashlib.sha256(source_path.read_bytes()).hexdigest()))
    m=re.search(r'(?m)^\tprivate (?:static )?[^\n]+\b'+item['symbol']+r'\(',source)
    if m is None: raise SystemExit('historical atomic declaration missing: '+item['symbol'])
    start=m.start(); brace=source.index('{',m.end());depth=1;end=brace+1
    while depth:
        depth+=(source[end]=='{')-(source[end]=='}');end+=1
    body=source[start:end]
    if body not in fixture or hashlib.sha256(body.encode()).hexdigest()!=item['bodySha256']:
        raise SystemExit('historical atomic body drift: '+item['symbol'])
for name in ['Program.cs','NativeAtomicConsumer.cs','Stubs.cs','SceneSpeechOutputContract.cs','SceneSpeechOutputOracle.cs','NativeWholeConsumer.cs','NuGet.Config']:
    shutil.copy2(here/name,out/name)
links=['src/AF.GameAdapter.Bannerlord/Scene/NativeConversationPlaybackWaitAdapter.cs','src/AF.GameAdapter.Bannerlord/Scene/NativeConversationSpeechAdapter.cs','src/modules/AF.Module.Conversation/Channels/Scene/NpcDataPacket.cs','src/AF.GameAdapter.Bannerlord/Scene/ScenePresentationController.cs','src/AF.GameAdapter.Bannerlord/Scene/SceneAudioLipSyncController.cs','src/modules/AF.Module.Conversation/Channels/Scene/ScenePresentationPolicy.cs']
# DTO/exception identity is projected from the actual shared declaration, not a copied runtime policy.
admission_source=(repo/'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeAdmission.cs').read_text(encoding='utf-8-sig')
def dto_declaration(name):
    start=admission_source.index('    internal sealed class '+name)
    brace=admission_source.index('{',start);end=brace+1;depth=1
    while depth:
        depth+=(admission_source[end]=='{')-(admission_source[end]=='}');end+=1
    return admission_source[start:end]
(out/'ActualAdmissionDtos.cs').write_text('using System;using TaleWorlds.CampaignSystem;using TaleWorlds.CampaignSystem.Conversation;using TaleWorlds.MountAndBlade;using AnimusForge.Refactor.Modules;\nnamespace AnimusForge;public partial class ShoutBehavior {\n'+dto_declaration('NativeConversationAdmission')+'\n'+dto_declaration('NativeConversationAdmissionException')+'\n}',encoding='utf-8')
# Newly required presentation cleanup is extracted unchanged; only VM rendering is a fixture leaf.
import ast
extract_path=repo/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py'
node=next(n for n in ast.parse(extract_path.read_text(encoding='utf-8-sig')).body if isinstance(n,ast.FunctionDef) and n.name=='declaration')
scope={'re':re};exec(compile(ast.Module(body=[node],type_ignores=[]),str(extract_path),'exec'),scope);declaration=scope['declaration']
interruption_path=repo/'src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeNativeConversationOverlay.Interruption.cs'
interruption=interruption_path.read_text(encoding='utf-8-sig')
fields=interruption[interruption.index('    private readonly Queue<Action>'):interruption.index('    // ApplicationTick')]
helper_path=repo/'src/AF.GameAdapter.Bannerlord/UI/Conversation/ConversationHelper.cs'
helper=helper_path.read_text(encoding='utf-8-sig')
cleanup='using System;using System.Collections.Generic;namespace AnimusForge {public sealed partial class AnimusForgeNativeConversationOverlay {'+fields+declaration(interruption,'private void ClearInterruptedPresentation')+'} public static partial class ConversationHelper {'+declaration(helper,'internal static void ClearForOwner')+declaration(helper,'internal static void EndStreaming(object owner)')+'}}'
(out/'ActualPresentationCleanup.cs').write_text(cleanup,encoding='utf-8')
(out/'presentation-cleanup-source-proof.json').write_text(json.dumps({str(q.relative_to(repo)):hashlib.sha256(q.read_bytes()).hexdigest() for q in [interruption_path,helper_path]},indent=2),encoding='utf-8')
links += ['src/modules/AF.Module.Conversation/Channels/Native/NativeAdmissionApplicationAdapter.cs','src/modules/AF.Module.Conversation/Channels/Native/NativeConversationAdmissionOwner.cs','src/modules/AF.Module.Conversation/Channels/Native/NativeConversationDispatchClaim.cs','src/modules/AF.Module.Conversation/Internal/ConversationRequestLifetime.cs','src/modules/AF.Module.Conversation/Internal/InteractionRequestLease.cs','src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeNativeConversationOverlay.Presentation.cs','src/modules/AF.Module.Conversation/Channels/Native/NativeConversationMainReplyStage.cs','src/modules/AF.Module.Conversation/Channels/Native/NativeConversationMainReplyContracts.cs','src/modules/AF.Module.Llm/Protocol/LlmVisibleReplyNormalizer.cs']
# Current composition must preserve the original cancellation completion-token cleanup flag.
composition_path=repo/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs'
cancel_wiring='ClearPendingBubble = index => CurrentInstance?._j17SceneSpeechOutputQueueController.ClearPendingTtsBubbleSyncForAgent(index, true),'
if composition_path.read_text(encoding='utf-8-sig').count(cancel_wiring)!=1:
    raise SystemExit('current Native cancellation token cleanup wiring drift')
(out/'current-consumer-wiring-proof.json').write_text(json.dumps({'path':composition_path.relative_to(repo).as_posix(),'sha256':hashlib.sha256(composition_path.read_bytes()).hexdigest(),'verifiedSnippet':cancel_wiring,'scope':'current cancellation leaf binding only; not complete host execution'},indent=2),encoding='utf-8')
defines='<DefineConstants>$(DefineConstants);BANNERLORD_1_4_OR_GREATER</DefineConstants>' if a.bannerlord_api=='1.4' else ''
project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><UseAppHost>false</UseAppHost>'+defines+'</PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+str(repo/x)+'" Link="'+Path(x).name+'"/>' for x in links)+'<Reference Include="Newtonsoft.Json"><HintPath>'+str(Path(os.environ.get('AF_NEWTONSOFT') or repo/'local/bannerlord-refs/1.4.7.117484/Newtonsoft.Json.dll'))+'</HintPath></Reference></ItemGroup></Project>'
(out/'Diagnostic.csproj').write_text(project,encoding='utf-8')
(out/'current-source-proof.json').write_text(json.dumps({'bannerlordApi':a.bannerlord_api,'atomOracleBaseline':'1235727e44113f3e559a59c77bd88851e195a689','atomic':observed_proof,'wholeLinks':[{'path':x,'sha256':hashlib.sha256((repo/x).read_bytes()).hexdigest()} for x in links]},indent=2),encoding='utf-8')
env={key:os.environ[key] for key in ('SystemRoot','WINDIR','PATH','COMSPEC','PATHEXT','PROCESSOR_ARCHITECTURE','ProgramFiles','ProgramFiles(x86)','ProgramW6432','ProgramData','ALLUSERSPROFILE') if key in os.environ}
env.update(USERPROFILE=str(out),APPDATA=str(out/'appdata'),LOCALAPPDATA=str(out/'localappdata'),TEMP=str(out),TMP=str(out),DOTNET_CLI_HOME=str(out),NUGET_PACKAGES=str(out/'packages'),DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_GENERATE_ASPNET_CERTIFICATE='false',DOTNET_ADD_GLOBAL_TOOLS_TO_PATH='false',DOTNET_NOLOGO='1',DOTNET_CLI_UI_LANGUAGE='en-US')
sys.path.insert(0,str(repo/'tests'))
from output_isolation import resolve_dotnet
dotnet=resolve_dotnet(repo)
for label,args in [('build',[str(dotnet),'build',str(out/'Diagnostic.csproj'),'--configfile',str(out/'NuGet.Config')]),('run',[str(dotnet),str(out/'bin/Debug/net8.0/Diagnostic.dll')])]:
    try:r=subprocess.run(args,env=env,cwd=repo,text=True,encoding='utf-8',errors='replace',capture_output=True,timeout=90)
    except subprocess.TimeoutExpired as e:
        text=str(e)+'\n'+str(e.stdout or '')+'\n'+str(e.stderr or '');(out/(label+'.log')).write_text(text,encoding='utf-8');raise SystemExit(1)
    (out/(label+'.log')).write_text(r.stdout+'\n'+r.stderr,encoding='utf-8')
    if r.returncode!=0:print(r.stdout+r.stderr);raise SystemExit(r.returncode)
    if label=='run':print(r.stdout.strip())
