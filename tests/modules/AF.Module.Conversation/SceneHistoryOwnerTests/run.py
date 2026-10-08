from pathlib import Path
import sys,importlib.util,argparse,subprocess,json,hashlib
R=Path(__file__).resolve().parents[4];H=Path(__file__).parent
sys.path.insert(0,str(R/'tests'))
from output_isolation import minimal_test_environment
s=importlib.util.spec_from_file_location('ex',R/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(s);s.loader.exec_module(ex)
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path,required=True);args=p.parse_args();out=args.run_root.resolve();out.relative_to(R);out.mkdir(parents=True,exist_ok=True)
host=(R/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_text(encoding='utf-8-sig')
for item in json.loads((H/'TerminalConsumers.json').read_text(encoding='utf-8')):
    if 'exact' in item:
        consumer=(R/item['source']).read_text(encoding='utf-8-sig') if 'source' in item else host
        assert consumer.count(item['exact'])==1,item['symbol']
        if 'binding_source' in item:
            assert (R/item['binding_source']).read_text(encoding='utf-8-sig').count(item['binding_exact'])==1,item['symbol']+' binding'
    elif 'absent' in item:assert item['absent'] not in host,item['symbol']
    else:
        import re
        match=re.search(r'(?:private|public|internal)[^\n]*\b'+item['symbol']+r'\([^\n]*\)',host);assert match,item['symbol']
        body=ex.declaration(host,match.group(0));assert body.count(item['required'])==1,item['symbol']
assert '_npcConversationHistory' not in host and '_publicConversationHistory' not in host
owner=(R/'src/modules/AF.Module.Conversation/Internal/History/SceneConversationHistoryOwner.cs').read_text(encoding='utf-8-sig')
assert 'private readonly Dictionary<int,List<ConversationMessage>> _npcHistory' in owner
assert 'private readonly List<ConversationMessage> _publicHistory' in owner
capture_path=R/'src/AF.GameAdapter.Bannerlord/Prompt/SceneHistoryPromptCaptureAdapter.cs'
assert 'nativeSessions.Append(key, NativeHistoryIdentityProjectionOwner.BuildEntry(' in capture_path.read_text(encoding='utf-8-sig')
print('PASS 48 actual terminal history consumers; sole private scene stores; existing native SessionOwner append')
main=(R/'src/modules/AF.Module.Prompt/Composition/MainPromptMessageAssemblyOwner.cs').read_text(encoding='utf-8-sig');utils=(R/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutUtils.cs').read_text(encoding='utf-8-sig')
program=(H/'Harness.cs.txt').read_text(encoding='utf-8').replace('@@CREATE@@',ex.declaration(main,'internal static object CreateCourierChatMessage('))
for placeholder,signature in [('@@METADATA@@','public static string StripConversationMetadataPrefix('),('@@STRIP@@','public static string StripNamePrefixedLineSafely('),('@@SPLIT@@','public static bool TrySplitNamePrefixedLineSafely(')]:program=program.replace(placeholder,ex.declaration(utils,signature))
presentation=H/'PresentationHarness.cs.txt'
assert presentation.is_file(),'actual presentation consumer fixture is required'
program=program.replace('@@PRESENTATION@@','PresentationHistoryContract.Run(gate,owner);' if presentation.exists() else '')
capture=(R/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.HistoryCapture.cs').read_text(encoding='utf-8-sig')
append=ex.declaration(capture,'private static void AppendNativeConversationSessionHistoryCaptured(').replace('private static','internal static',1)
actual_append=ex.declaration(capture_path.read_text(encoding='utf-8-sig'),'internal static void AppendNativeConversationSessionHistoryCaptured(')
(out/'NativeAppendProbe.cs').write_text((H/'NativeAppendHarness.cs.txt').read_text(encoding='utf-8').replace('@@APPEND@@',append).replace('@@CURRENT_APPEND@@',actual_append),encoding='utf-8')
(out/'Program.cs').write_text(program,encoding='utf-8');(out/'LegacyProjection.cs').write_text((H/'LegacyProjection.cs.txt').read_text(encoding='utf-8'),encoding='utf-8')
if presentation.exists():(out/'PresentationProbe.cs').write_text(presentation.read_text(encoding='utf-8'),encoding='utf-8')
paths=['src/modules/AF.Module.Conversation/Internal/History/SceneConversationHistoryOwner.cs','src/modules/AF.Module.Conversation/Internal/History/SceneHistoryProjectionOwner.cs','src/modules/AF.Module.Conversation/Internal/History/NativeHistoryIdentityProjectionOwner.cs','src/modules/AF.Module.Prompt/Composition/HistorySectionProjectionOwner.cs','src/modules/AF.Module.Prompt/Composition/SceneHistoryMessageAssemblyOwner.cs','src/modules/AF.Module.Prompt/Composition/ConversationRoleClassificationOwner.cs','src/modules/AF.Module.Conversation/Internal/History/ConversationMessage.cs','src/modules/AF.Module.Conversation/Internal/History/ConversationSpeechTextRules.cs','src/modules/AF.Module.Conversation/Channels/Native/NativeConversationSessionOwner.cs','src/modules/AF.Module.Memory/Records/AnimusForgeDialogueHistoryEntry.cs','src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs']
(out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+str(R/x)+'" />' for x in paths)+'</ItemGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
d=R/'local/dotnet/8.0.425/dotnet.exe'
try:r=subprocess.run([str(d),'run','--project',str(out/'Tests.csproj'),'-c','Release'],cwd=out,env=minimal_test_environment(d,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
except subprocess.TimeoutExpired as error:
    output=(error.stdout or b'')+(error.stderr or b'')
    if isinstance(output,bytes):output=output.decode('utf-8',errors='replace')
    (out/'run.log').write_text(output+'\nTIMEOUT: 120 seconds\n',encoding='utf-8');print(output);raise
(out/'run.log').write_text(r.stdout+r.stderr,encoding='utf-8');print(r.stdout+r.stderr,end='')
(out/'receipt.json').write_text(json.dumps({'exit_code':r.returncode,'sources':{x:hashlib.sha256((R/x).read_bytes()).hexdigest() for x in paths},'native_capture_sha256':hashlib.sha256(capture_path.read_bytes()).hexdigest(),'native_capture_scope':'actual complete native append declaration; native SessionOwner and scene event sequence real; game clocks/key and bridge destination controlled leaves','adapter_sha256':hashlib.sha256((R/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.HistoryCapture.cs').read_bytes()).hexdigest(),'main_sha256':hashlib.sha256((R/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_bytes()).hexdigest(),'test_sha256':{x.name:hashlib.sha256(x.read_bytes()).hexdigest() for x in H.iterdir() if x.is_file()},'terminal_consumers':48,'presentation_fixture':presentation.exists(),'live':'NOT_RUN'},indent=2),encoding='utf-8');sys.exit(r.returncode)
