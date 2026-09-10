import importlib.util,subprocess,os,hashlib
from pathlib import Path
root=Path(__file__).resolve().parents[2]; here=root/'tools/NativeConversationAdmissionTests';out=here/'.generated/original';out.mkdir(parents=True,exist_ok=True)
spec=importlib.util.spec_from_file_location('extractor',root/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
s=subprocess.check_output(['git','show','14dec2d7:ShoutBehavior.cs'],cwd=root).decode('utf-8-sig')
entry=ex.declaration(s,'public static Task<string> SubmitNativeConversationTextForExternalAsync(string playerText, Action<string> onStreamText, string currentDialogTextOverride, Action<string> onPostprocessStarted, Action<string, Hero, CharacterObject> onMainReplyReady)')
body=ex.declaration(s,'private async Task<string> SubmitNativeConversationTextInternalAsync(')
prefix=body.split('\t\tLogger.Log("Logic", "[NativePerf] submit_start')[0]
assert 'nativeRequestConversationToken' in prefix
(out/'Program.cs').write_text((here/'OriginalEntryHarness.cs.txt').read_text(encoding='utf-8-sig').replace('@@ENTRY@@',entry).replace('@@PREFIX@@',prefix),encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
env=os.environ.copy(); env.update(DOTNET_ROOT=r'G:\AFMOD\.dotnet-sdk',DOTNET_CLI_HOME=str(root/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(root/'.tmp/nuget-packages'),DOTNET_GENERATE_ASPNET_CERTIFICATE='false',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
r=subprocess.run([r'G:\AFMOD\.dotnet-sdk\dotnet.exe','run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=root,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace')
log='baseline=14dec2d7 entrySha256='+hashlib.sha256(entry.encode()).hexdigest()+'\n'+r.stdout+r.stderr
(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)
