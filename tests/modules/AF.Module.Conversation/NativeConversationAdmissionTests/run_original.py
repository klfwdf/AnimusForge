import argparse,sys,importlib.util,subprocess,os,hashlib
from pathlib import Path
root=Path(__file__).resolve().parents[4]; here=root/'tests/modules/AF.Module.Conversation/NativeConversationAdmissionTests';
sys.path.insert(0,str(root/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path);args=p.parse_args()
out=new_run_root(root,'native-admission-original',args.run_root)
spec=importlib.util.spec_from_file_location('extractor',root/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
s=subprocess.check_output(['git','show','14dec2d7:ShoutBehavior.cs'],cwd=root).decode('utf-8-sig')
entry=ex.declaration(s,'public static Task<string> SubmitNativeConversationTextForExternalAsync(string playerText, Action<string> onStreamText, string currentDialogTextOverride, Action<string> onPostprocessStarted, Action<string, Hero, CharacterObject> onMainReplyReady)')
body=ex.declaration(s,'private async Task<string> SubmitNativeConversationTextInternalAsync(')
prefix=body.split('\t\tLogger.Log("Logic", "[NativePerf] submit_start')[0]
assert 'nativeRequestConversationToken' in prefix
(out/'Program.cs').write_text((here/'OriginalEntryHarness.cs.txt').read_text(encoding='utf-8-sig').replace('@@ENTRY@@',entry).replace('@@PREFIX@@',prefix),encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet=str(resolve_dotnet(root))
env=minimal_test_environment(Path(dotnet),out)
import json,hashlib
(out/'generated-inputs-before-build.json').write_text(json.dumps({str(f.name):hashlib.sha256(f.read_bytes()).hexdigest() for f in out.iterdir() if f.is_file() and f.suffix in ('.cs','.csproj','.Config')},indent=2),encoding='utf-8')
r=subprocess.run([dotnet,'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=root,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace')
log='baseline=14dec2d7 entrySha256='+hashlib.sha256(entry.encode()).hexdigest()+'\n'+r.stdout+r.stderr
(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)
