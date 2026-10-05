"""Real CommitVictory control flow and actual session JSON, not live political actions."""
from pathlib import Path
import sys,subprocess,hashlib,json
ROOT=Path(__file__).resolve().parents[4]
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
here=Path(__file__).parent
source=ROOT/'extensions/AnimusForge.Coup/src/CoupSystem/CoupCampaignBehavior.cs'
session=source.parent/'CoupSession.cs'
text=source.read_text(encoding='utf-8-sig');start=text.index('private void CommitVictory()');opening=text.index('{',start);end=opening+1;depth=1
while depth:
    depth+=(text[end]=='{')-(text[end]=='}');end+=1
method=text[start:end]
out=new_run_root(ROOT,'coup-settlement-retry',None)
(out/'Program.cs').write_text((here/'Fixture.cs.in').read_text(encoding='utf-8-sig').replace('__COMMIT_VICTORY__',method),encoding='utf-8')
(out/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><PackageReference Include="Newtonsoft.Json" Version="13.0.3"/><Compile Include="'+str(session)+'" Link="CoupSession.cs"/></ItemGroup></Project>',encoding='utf-8')
(out/'source-manifest.json').write_text(json.dumps({str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in [source,session]},indent=2),encoding='utf-8')
dotnet=resolve_dotnet(ROOT)
code=subprocess.run([dotnet,'run','--project',str(out/'Test.csproj')],cwd=ROOT,env=minimal_test_environment(dotnet,out)).returncode
print('Output:',out)
raise SystemExit(code)
