from pathlib import Path
import importlib.util,subprocess,sys
R=Path(__file__).resolve().parents[4];H=Path(__file__).parent;out=Path(sys.argv[1]).resolve();out.mkdir(parents=True,exist_ok=True)
spec=importlib.util.spec_from_file_location('extract',R/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
lease=(R/'src/AF.GameAdapter.Bannerlord/UI/Common/DevPopupInputLease.cs').read_text(encoding='utf-8-sig')
lease='\n'.join(line for line in lease.splitlines() if not line.startswith('using ')).replace('namespace AnimusForge;','namespace AnimusForge {')+'\n}'
body=(H/'Program.cs.in').read_text(encoding='utf-8').replace('@@LEASE@@',lease)
(out/'Program.cs').write_text(body,encoding='utf-8')
proj='''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="Program.cs" /></ItemGroup></Project>'''
(out/'Tests.csproj').write_text(proj,encoding='utf-8')
dotnet=R/'local/dotnet/8.0.425/dotnet.exe';r=subprocess.run([str(dotnet),'run','--project',str(out/'Tests.csproj'),'-c','Release'],cwd=out,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90);(out/'run.log').write_text(r.stdout+r.stderr,encoding='utf-8');print(r.stdout+r.stderr,end='');raise SystemExit(r.returncode)
