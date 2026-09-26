from pathlib import Path
import argparse,os,subprocess
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).resolve().parent
p=argparse.ArgumentParser();p.add_argument('--mutate',choices=['leak-response','skip-line-accept','duplicate-content','ignore-cancel','unbounded-raw']);a=p.parse_args();out=HERE/'.generated'/(a.mutate or 'current');out.mkdir(parents=True,exist_ok=True)
files=['src/modules/AF.Module.Llm/Streaming/LlmStreamingTransport.cs','src/modules/AF.Module.Llm/Transport/LlmNonStreamingTransport.cs','src/modules/AF.Module.Llm/Protocol/LlmApiCompat.cs']
for f in files:
 s=(ROOT/f).read_text(encoding='utf-8-sig')
 if f.endswith('LlmStreamingTransport.cs'):
  if a.mutate=='leak-response':s=s.replace('using (HttpResponseMessage response = await sender(request, cancellationToken).ConfigureAwait(false))','HttpResponseMessage response = await sender(request, cancellationToken).ConfigureAwait(false); using (null as HttpResponseMessage)',1)
  if a.mutate=='skip-line-accept':s=s.replace('acceptLine != null && !acceptLine(readSequence, line)','acceptLine != null && !acceptLine(readSequence, line) && false',1)
  if a.mutate=='duplicate-content':s=s.replace('content.Append(contentDelta);','content.Append(contentDelta); content.Append(contentDelta);',1)
  if a.mutate=='ignore-cancel':s=s.replace('cancellationToken.ThrowIfCancellationRequested();',';',1)
  if a.mutate=='unbounded-raw':s=s.replace('int remaining = maxChars - raw.Length;','maxChars = int.MaxValue; int remaining = maxChars - raw.Length;',1)
 (out/Path(f).name).write_text(s,encoding='utf-8')
(out/'Program.cs').write_text((HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig'),encoding='utf-8');newton=os.environ.get('AF_NEWTONSOFT','G:/AFMOD/.dotnet-sdk/sdk/8.0.422/Newtonsoft.Json.dll');(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>'+newton+'</HintPath></Reference></ItemGroup></Project>');(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>');dotnet=os.environ.get('AF_DOTNET','G:/AFMOD/.dotnet-sdk/dotnet.exe');env=os.environ.copy();env.update(DOTNET_ROOT=str(Path(dotnet).parent),DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(ROOT/'.tmp/nuget-packages'));q=subprocess.run([dotnet,'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90);log=q.stdout+q.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(q.returncode)
