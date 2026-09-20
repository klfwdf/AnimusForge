"""Actual taunt/observation slice with a game port that rejects worker reads."""
from pathlib import Path
import argparse,importlib.util,os,subprocess
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('extract',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
p=argparse.ArgumentParser();p.add_argument('--baseline',action='store_true');p.add_argument('--mutate',choices=['move-back-to-worker','duplicate-observation','skip-target','tts-back-to-worker']);a=p.parse_args()
s=(subprocess.check_output(['git','show','00574541:ShoutBehavior.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n') if a.baseline else (ROOT/'ShoutBehavior.cs').read_text(encoding='utf-8-sig'))
body=ex.declaration(s,'private async Task<string> SubmitNativeConversationTextInternalAsync(')
marker='\t\tstring cleaned = "";' if '\t\tstring cleaned = "";' in body else '\t\tstring nativeMainReplyTargetUnavailableReason = "";'
start=body.index(marker);end=body.index('\t\tstring nativePostprocessStartTargetUnavailableReason = "";',start);piece=body[start:end]
call='SubmitNativeConversationSceneActionObservation(postprocessReply, nativeTargetAgentIndex);'
if a.mutate=='move-back-to-worker':
 assert piece.count(call)==1;piece=piece.replace(call,';',1)+'\n'+call+'\n'
if a.mutate=='duplicate-observation':assert piece.count(call)==1;piece=piece.replace(call,call+'\n'+call,1)
if a.mutate=='skip-target':
 before='if (!IsNativeConversationAdmissionCurrent(admission, out nativeMainReplyTargetUnavailableReason))';assert piece.count(before)==1;piece=piece.replace(before,'if (false)',1)
if a.mutate=='tts-back-to-worker':
 block=ex.declaration(piece,'if (nativeTargetAgentIndex < 0 &&');assert piece.count(block)==1;piece=piece.replace(block,';',1)+'\n'+block+'\n'
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig').replace('@@SLICE@@',piece).replace('@@OBSERVER@@',ex.declaration(s,'private static void SubmitNativeConversationSceneActionObservation('));assert '@@' not in code
out=HERE/'.generated'/('baseline' if a.baseline else a.mutate or 'current');out.mkdir(parents=True,exist_ok=True);(out/'Program.cs').write_text(code,encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion>'+('<DefineConstants>BASELINE</DefineConstants>' if a.baseline else '')+'</PropertyGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet=Path(os.environ.get('AF_DOTNET') or ROOT/'local/dotnet/8.0.425/dotnet.exe');env=os.environ.copy();env.update(DOTNET_ROOT=str(dotnet.parent),DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(ROOT/'.tmp/nuget-packages'),DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
r=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=150)
log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)
