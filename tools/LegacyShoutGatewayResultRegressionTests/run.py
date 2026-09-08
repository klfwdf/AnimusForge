from pathlib import Path
import argparse,hashlib,importlib.util,os,re,subprocess,sys
sys.stdout.reconfigure(encoding="utf-8")
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).resolve().parent
GATEWAY='Refactor/Contracts/LegacyShoutNetworkGateway.cs'
def main():
 p=argparse.ArgumentParser();p.add_argument('--source-ref');p.add_argument('--output-name',default='current');p.add_argument('--dotnet',default=r'G:\AFMOD\.dotnet-sdk\dotnet.exe');a=p.parse_args()
 if not re.fullmatch(r'[A-Za-z0-9_-]+',a.output_name):p.error('invalid output name')
 out=HERE/'.generated'/a.output_name;out.mkdir(parents=True,exist_ok=True)
 spec=importlib.util.spec_from_file_location('ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
 gateway=ex.source(GATEWAY,a.source_ref)
 (out/'Gateway.cs').write_text(gateway,encoding='utf-8')
 for name in ['Refactor/Contracts/InteractionContracts.cs','Refactor/Contracts/LlmContracts.cs','Refactor/Adapters/LegacyPromptPackageAdapter.cs','SaveRuntimeGuard.cs']:
  (out/Path(name).name).write_text((ROOT/name).read_text(encoding='utf-8-sig'),encoding='utf-8')
 retry=(ROOT/'LlmRetryPrompt.cs').read_text(encoding='utf-8-sig')
 detail='\n'.join(ex.declaration(retry,sig) for sig in ['public static string BuildFailureDetail(','private static string NormalizeFullText('])
 (out/'Retry.cs').write_text('using System;using System.Text;namespace AnimusForge { public static class LlmRetryPrompt { '+detail+' } }',encoding='utf-8')
 (out/'Program.cs').write_text((HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig'),encoding='utf-8')
 (out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
 (out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=os.environ.copy();env.update(DOTNET_ROOT=str(Path(a.dotnet).parent),DOTNET_CLI_HOME=str(out/'cli'),DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_NOLOGO='1',DOTNET_CLI_UI_LANGUAGE='en')
 r=subprocess.run([a.dotnet,'run','--project',str(out/'Tests.csproj'),'-c','Release'],cwd=out,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
 log='source='+(a.source_ref or 'working-tree')+'\nGateway normalized source SHA256='+hashlib.sha256(gateway.encode()).hexdigest()+'\n'+r.stdout+r.stderr
 (out/'run.log').write_text(log,encoding='utf-8');print(log);return r.returncode
if __name__=='__main__':raise SystemExit(main())
