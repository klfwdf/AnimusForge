from pathlib import Path
import argparse, importlib.util, subprocess, sys, hashlib, json
ROOT=Path(__file__).resolve().parents[4]; HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
parser=argparse.ArgumentParser();parser.add_argument('--run-root',type=Path);parser.add_argument('--harmony',type=Path,default=ROOT/'bin/Debug/net472/0Harmony.dll');parser.add_argument('--framework-ref',type=Path,default=Path.home()/'.nuget/packages/microsoft.netframework.referenceassemblies.net472/1.0.3/build/.NETFramework/v4.7.2');args=parser.parse_args()
if not args.harmony.is_file():parser.error('Pass the real --harmony path for producer-driven Illustrator hooks.')
if not (args.framework_ref/'mscorlib.dll').is_file():parser.error('Pass the installed --framework-ref net472 reference directory; this runner does not install dependencies.')
spec=importlib.util.spec_from_file_location('extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
out=new_run_root(ROOT,'native-opening-retry',args.run_root)
paths=['src/modules/AF.Module.Conversation/Channels/Native/NativeAdmissionApplicationAdapter.cs','src/modules/AF.Module.Conversation/Channels/Native/NativeConversationAdmissionOwner.cs','src/modules/AF.Module.Conversation/Channels/Native/NativeConversationDispatchClaim.cs','src/modules/AF.Module.Conversation/Internal/ConversationRequestLifetime.cs','src/modules/AF.Module.Conversation/Internal/InteractionRequestLease.cs','src/modules/AF.Module.Conversation/Proactive/NpcInitiatedOpeningRouter.cs','src/modules/AF.Module.Social/Proactive/ProactiveOpeningOwner.cs']
for path in paths:(out/Path(path).name).write_bytes((ROOT/path).read_bytes())
read=lambda path:(ROOT/path).read_text(encoding='utf-8-sig')
shape='src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeAdmission.cs'
facade='src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs'
lord='src/modules/AF.Module.Conversation/Proactive/ProactiveNpcRequestBehavior.cs';companion='src/modules/AF.Module.Conversation/Proactive/CompanionProactiveChatBehavior.cs'
code=(HERE/'Harness.cs.txt').read_text(encoding='utf8')
for token,path,signature in [('@@ADMISSION@@',shape,'internal sealed class NativeConversationAdmission'),('@@ERROR@@',shape,'internal sealed class NativeConversationAdmissionException'),('@@LORD_CONSUME@@',lord,'private bool TryConsumePendingOpening('),('@@COMPANION_CONSUME@@',companion,'private bool TryConsumePendingOpening(')]:
 code=code.replace(token,ex.declaration(read(path),signature))
producer='\n'.join(ex.declaration(read(path),signature) for path,signature in [
 (facade,'public static Task<string> SubmitNativeConversationTextForExternalAsync(string playerText, Action<string> onStreamText, string currentDialogTextOverride, Action<string> onPostprocessStarted, Action<string, Hero, CharacterObject> onMainReplyReady)'),
 (facade,'public static Task<string> SubmitNativeConversationNpcInitiatedOpeningForExternalAsync(Action<string> onStreamText, string currentDialogTextOverride, Action<string> onPostprocessStarted, Action<string, Hero, CharacterObject> onMainReplyReady)'),
 (shape,'private Task<string> SubmitNativeConversationAdmittedAsync('),
 (shape,'internal sealed class NativeConversationPresentationScope'),
 (shape,'internal static NativeConversationPresentationScope CaptureNativeConversationPresentationScopeForOverlay('),
 (shape,'internal static Task<string> SubmitNativeConversationForOverlayAsync(')])
# Fixture consumers are JITted before its local Install call. Prevent the CLR from
# baking a pre-patch facade into that caller; production method bodies stay exact.
no_inline='[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)] '
producer=producer.replace('public static Task<string> ',no_inline+'public static Task<string> ')
producer=producer.replace('internal static Task<string> SubmitNativeConversationForOverlayAsync',no_inline+'internal static Task<string> SubmitNativeConversationForOverlayAsync')
code=code.replace('@@PRODUCER@@',producer)
illustrator='extensions/AnimusForge.Illustrator/src/UI/Patches/ConversationIllustrationPatch.cs'
hooks='\n'.join(ex.declaration(read(illustrator),signature) for signature in ['private static void TryPatchHostNativeConversationReply(', 'private static void WrapNativeConversationReplyCallbackPrefix(', 'private static void WrapNativeOpeningCallbackPrefix(', 'private static void WrapReplyCallback('])
code=code.replace('@@ILLUSTRATOR_HOOKS@@',hooks)
# Production producer must retire the obligation before any accepted raw reply effects.
presentation=read('src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurnPresentation.cs')
assert presentation.index('_ports.RetireOpeningForAcceptedReply?.Invoke(admission);') < presentation.index('TryProcessNativeConversationRawMeetingTauntTags(')
composition=read('src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurn.cs')
assert 'RetireOpeningForAcceptedReply = NativeAdmissions.RetireOpeningForAcceptedReply,' in composition
reset=ex.declaration(read('src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs'),'private void ResetInstanceTransientRuntimeForLoadedSave(')
assert 'NativeAdmissions.EndConversation();' in reset
assert '@@' not in code
(out/'Program.cs').write_text(code,encoding='utf8')
from xml.sax.saxutils import escape
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net472</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn><AutomaticallyUseReferenceAssemblyPackages>false</AutomaticallyUseReferenceAssemblyPackages><FrameworkPathOverride>'+escape(str(args.framework_ref.resolve()))+'</FrameworkPathOverride></PropertyGroup><ItemGroup><Reference Include="0Harmony"><HintPath>'+escape(str(args.harmony.resolve()))+'</HintPath></Reference></ItemGroup></Project>',encoding='utf8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf8')
manifest={path:hashlib.sha256((ROOT/path).read_bytes()).hexdigest() for path in paths+[shape,facade,lord,companion,illustrator]};manifest['Harmony']=hashlib.sha256(args.harmony.read_bytes()).hexdigest();(out/'sources.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
dotnet=resolve_dotnet(ROOT);env=minimal_test_environment(dotnet,out)
p=subprocess.run([str(dotnet),'build',str(out/'Proof.csproj'),'-c','Release','--nologo'],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf8',errors='replace',timeout=120)
(out/'build.log').write_text(p.stdout+p.stderr,encoding='utf8')
if p.returncode==0:
    p=subprocess.run([str(out/'bin/Release/net472/Proof.exe')],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf8',errors='replace',timeout=120)
log=p.stdout+p.stderr;(out/'run.log').write_text(log,encoding='utf8');print(log,end='');raise SystemExit(p.returncode)
