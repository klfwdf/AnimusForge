"""Actual TriggerShout menu + exact SceneWheel classifier + real Harmony show-prefix replay."""
from pathlib import Path
import argparse, hashlib, importlib.util, json, subprocess
ROOT=Path(__file__).resolve().parents[4]; HERE=Path(__file__).resolve().parent
p=argparse.ArgumentParser();p.add_argument('--out',type=Path,required=True);p.add_argument('--harmony',type=Path,required=True);p.add_argument('--baseline-classifier',action='store_true');a=p.parse_args()
out=a.out.resolve();out.mkdir(parents=True,exist_ok=False)
spec=importlib.util.spec_from_file_location('extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
producer=ROOT/'src/AF.GameAdapter.Bannerlord/Scene/SceneShoutInputController.cs'
wheel=ROOT/'extensions/AnimusForge.DialogueUI/src/Scene/SceneWheel.cs'
s=producer.read_text(encoding='utf-8-sig'); w=wheel.read_text(encoding='utf-8-sig')
fields=s[s.index('    private MultiSelectionInquiryData _modeInquiry;'):s.index('    // Exact menu ownership,')]
methods='\n'.join(ex.declaration(s,sig) for sig in ['internal bool OwnsShoutModeInquiry(','private bool ConsumeShoutModeInquiry(','internal void TriggerShout('])
wheel_type=ex.declaration(w,'internal static class SceneWheel')
if a.baseline_classifier:
 old=subprocess.check_output(['git','show','44510c91f:'+wheel.relative_to(ROOT).as_posix()],cwd=ROOT).decode('utf-8-sig')
 wheel_type=wheel_type.replace(ex.declaration(w,'private static bool IsHostSceneMenu('),ex.declaration(old,'private static bool IsHostSceneMenu('))
facade=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_text(encoding='utf-8-sig')
legacy_entry=ex.declaration(facade,'public void TriggerShout(')
fixture=(HERE/'Fixture.cs.in').read_text(encoding='utf-8').replace('__FIELDS__',fields).replace('__PRODUCER__',methods).replace('__WHEEL__',wheel_type).replace('__LEGACY_ENTRY__',legacy_entry)
(out/'Program.cs').write_text(fixture,encoding='utf-8')
(out/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net472</TargetFramework><LangVersion>10.0</LangVersion><EnableDefaultCompileItems>false</EnableDefaultCompileItems><NoWarn>CS0649;CS0414;CS0169</NoWarn></PropertyGroup><ItemGroup><Compile Include="Program.cs"/><PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies.net472" Version="1.0.3" PrivateAssets="all"/><Reference Include="0Harmony"><HintPath>'+str(a.harmony.resolve())+'</HintPath></Reference></ItemGroup></Project>',encoding='utf-8')
(out/'receipt.json').write_text(json.dumps({'baselineClassifier':a.baseline_classifier,'producer':str(producer),'producerSha256':hashlib.sha256(producer.read_bytes()).hexdigest(),'wheel':str(wheel),'wheelSha256':hashlib.sha256(wheel.read_bytes()).hexdigest(),'harmonySha256':hashlib.sha256(a.harmony.read_bytes()).hexdigest(),'scope':'production TriggerShout + SceneWheel class; only engine/GPU/session leafs controlled'},indent=2),encoding='utf-8')
for name,command in [('build.log',['dotnet','build',str(out/'Test.csproj'),'--nologo']),('run.log',[str(out/'bin/Debug/net472/Test.exe')])]:
 result=subprocess.run(command,cwd=ROOT,capture_output=True,text=True,encoding='utf-8',errors='replace'); text=result.stdout+result.stderr;(out/name).write_text(text,encoding='utf-8');print(text)
 if result.returncode:raise SystemExit(result.returncode)
