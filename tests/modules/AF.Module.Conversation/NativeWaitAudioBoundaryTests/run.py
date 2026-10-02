import argparse, hashlib, json, os, re, shutil, subprocess, sys
from pathlib import Path
parser=argparse.ArgumentParser()
parser.add_argument('--run-root',required=True)
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
    source=source_path.read_text(encoding='utf-8-sig')
    observed_proof.append(dict(item,currentSourceSha256=hashlib.sha256(source_path.read_bytes()).hexdigest()))
    m=re.search(r'(?m)^\tprivate (?:static )?[^\n]+\b'+item['symbol']+r'\(',source)
    if m is None: raise SystemExit('current atomic declaration missing: '+item['symbol'])
    start=m.start(); brace=source.index('{',m.end());depth=1;end=brace+1
    while depth:
        depth+=(source[end]=='{')-(source[end]=='}');end+=1
    body=source[start:end]
    if body not in fixture or hashlib.sha256(body.encode()).hexdigest()!=item['bodySha256']:
        raise SystemExit('current atomic body drift: '+item['symbol'])
for name in ['Program.cs','NativeAtomicConsumer.cs','Stubs.cs','SceneSpeechOutputContract.cs','SceneSpeechOutputOracle.cs','NuGet.Config']:
    shutil.copy2(here/name,out/name)
links=['src/modules/AF.Module.Conversation/Channels/Scene/NpcDataPacket.cs','src/AF.GameAdapter.Bannerlord/Scene/ScenePresentationController.cs','src/AF.GameAdapter.Bannerlord/Scene/SceneAudioLipSyncController.cs','src/modules/AF.Module.Conversation/Channels/Scene/ScenePresentationPolicy.cs']
project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><UseAppHost>false</UseAppHost></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+str(repo/x)+'" Link="'+Path(x).name+'"/>' for x in links)+'</ItemGroup></Project>'
(out/'Diagnostic.csproj').write_text(project,encoding='utf-8')
(out/'current-source-proof.json').write_text(json.dumps({'atomic':observed_proof,'wholeLinks':[{'path':x,'sha256':hashlib.sha256((repo/x).read_bytes()).hexdigest()} for x in links]},indent=2),encoding='utf-8')
env={key:os.environ[key] for key in ('SystemRoot','WINDIR','PATH','COMSPEC','PATHEXT','PROCESSOR_ARCHITECTURE','ProgramFiles','ProgramFiles(x86)','ProgramW6432','ProgramData','ALLUSERSPROFILE') if key in os.environ}
env.update(USERPROFILE=str(out),APPDATA=str(out/'appdata'),LOCALAPPDATA=str(out/'localappdata'),TEMP=str(out),TMP=str(out),DOTNET_CLI_HOME=str(out),NUGET_PACKAGES=str(out/'packages'),DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_GENERATE_ASPNET_CERTIFICATE='false',DOTNET_ADD_GLOBAL_TOOLS_TO_PATH='false',DOTNET_NOLOGO='1',DOTNET_CLI_UI_LANGUAGE='en-US')
dotnet=repo/'local/dotnet/8.0.425/dotnet.exe'
for label,args in [('build',[str(dotnet),'build',str(out/'Diagnostic.csproj'),'--configfile',str(out/'NuGet.Config')]),('run',[str(dotnet),str(out/'bin/Debug/net8.0/Diagnostic.dll')])]:
    try:r=subprocess.run(args,env=env,cwd=repo,text=True,encoding='utf-8',errors='replace',capture_output=True,timeout=90)
    except subprocess.TimeoutExpired as e:
        text=str(e)+'\n'+str(e.stdout or '')+'\n'+str(e.stderr or '');(out/(label+'.log')).write_text(text,encoding='utf-8');raise SystemExit(1)
    (out/(label+'.log')).write_text(r.stdout+'\n'+r.stderr,encoding='utf-8')
    if r.returncode!=0:print(r.stdout+r.stderr);raise SystemExit(r.returncode)
    if label=='run':print(r.stdout.strip())
