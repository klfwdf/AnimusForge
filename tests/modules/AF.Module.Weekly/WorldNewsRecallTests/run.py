from pathlib import Path
import sys, subprocess, json, hashlib
from xml.sax.saxutils import escape
ROOT=Path(__file__).resolve().parents[4]
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
out=new_run_root(ROOT,'world-news-recall',None)
dotnet=resolve_dotnet(ROOT)
sources=[Path(__file__).with_name('Program.cs'),ROOT/'src/modules/AF.Module.Weekly/Recall/WorldNewsFactArchive.cs',ROOT/'src/modules/AF.Module.Weekly/Recall/WorldNewsRecallPolicy.cs']
(out/'Replay.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>10</LangVersion><EnableDefaultCompileItems>false</EnableDefaultCompileItems><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+escape(str(p))+'"/>' for p in sources)+'</ItemGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
(out/'manifest.json').write_text(json.dumps({str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in sources},indent=2),encoding='utf-8')
run=subprocess.run([str(dotnet),'run','--project',str(out/'Replay.csproj')],cwd=out,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace')
(out/'result.log').write_text(run.stdout+run.stderr,encoding='utf-8')
print(run.stdout+run.stderr);print(out)
raise SystemExit(run.returncode)
