from pathlib import Path
import argparse,hashlib,importlib.util,os,re,subprocess,sys
sys.stdout.reconfigure(encoding='utf-8')
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).resolve().parent
V2='extensions/AnimusForge.XihaiAction/src/Runtime/BattleSpeechMissionBehavior.V2.cs';COMPAT='extensions/AnimusForge.XihaiAction/src/Runtime/AfCompatV130.cs'
def main():
 p=argparse.ArgumentParser();p.add_argument('--source-ref');p.add_argument('--output-name',default='current');p.add_argument('--dotnet',default=r'G:\AFMOD\.dotnet-sdk\dotnet.exe');p.add_argument('--mutation',choices=['omit-completion-check','omit-final-check']);a=p.parse_args()
 if not re.fullmatch(r'[A-Za-z0-9_-]+',a.output_name):p.error('invalid output name')
 spec=importlib.util.spec_from_file_location('ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
 scene=ex.source('ShoutBehavior.cs',None);compat=ex.source(COMPAT,None);v2=ex.source(V2,a.source_ref)
 h='\n'.join(ex.declaration(scene,sig) for sig in ['private sealed class ScenePlayerShoutRequest','internal object CaptureScenePlayerShoutRequestForReplay(','private ScenePlayerShoutRequest CaptureScenePlayerShoutRequest(','private bool IsScenePlayerShoutRequestCurrent(','internal bool IsCapturedScenePlayerShoutRequestCurrent('])
 c='\n'.join(ex.declaration(compat,sig) for sig in ['private static void BindOptionalCapturedPlayerShoutMethods(','internal static bool IsCapturedPlayerShoutCurrent('])
 tick=ex.declaration(v2,'private void ProcessV2ClassifierCompletions(');marker='            while (_planCompletions.TryDequeue';assert tick.count(marker)==1;tick=tick.split(marker)[0]+'\n        }'
 action=ex.declaration(v2,'private void ApplyClassifiedTrigger(')
 if a.mutation=='omit-completion-check':tick=tick.replace('|| !AfCompatV130.IsCapturedPlayerShoutCurrent(trigger.Input)','')
 if a.mutation=='omit-final-check':action=action.replace('if (!AfCompatV130.IsCapturedPlayerShoutCurrent(input)) { return; }','')
 async_method=ex.declaration(v2,'private async Task RunTriggerClassificationAsync(')
 src=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig').replace('@@HOST@@',h).replace('@@COMPAT@@',c).replace('@@TRIGGER@@',tick+'\n'+action+'\n'+async_method)
 out=HERE/'.generated'/a.output_name;out.mkdir(parents=True,exist_ok=True);(out/'Program.cs').write_text(src,encoding='utf-8');(out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>');(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=os.environ.copy();env.update(DOTNET_ROOT=str(Path(a.dotnet).parent),DOTNET_CLI_HOME=str(out/'cli'),DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_NOLOGO='1',DOTNET_CLI_UI_LANGUAGE='en')
 r=subprocess.run([a.dotnet,'run','--project',str(out/'Tests.csproj'),'-c','Release'],cwd=out,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=100)
 log='trigger_source='+(a.source_ref or 'working-tree')+'\nhost_and_compat_source=working-tree\nextracted_trigger_SHA256='+hashlib.sha256((tick+action+async_method).encode()).hexdigest()+'\n'+r.stdout+r.stderr
 (out/'run.log').write_text(log,encoding='utf-8');print(log);return r.returncode
if __name__=='__main__':raise SystemExit(main())
