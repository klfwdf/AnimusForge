from pathlib import Path
import hashlib,json,shutil,subprocess,sys
sys.stdout.reconfigure(encoding='utf-8')
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import resolve_dotnet,minimal_test_environment
from fixture_support import add_retry_resources
OUT=ROOT/'artifacts/localization/verification-20261011/retry-error-projection-run';OUT.mkdir(parents=True,exist_ok=True)
catalog=OUT/'catalog'
for entry in json.loads((ROOT/'content/content-map.json').read_text(encoding='utf-8'))['entries']:
    if entry['target'].startswith('ModuleData/Languages/') and entry['target'].endswith('.xml'):
        target=catalog/entry['target'];target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(ROOT/entry['source'],target)
llm=json.loads((ROOT/'content/modules/AF.Module.Llm/ModuleData/LlmSourceTexts.json').read_text(encoding='utf-8'))
retrieval=json.loads((ROOT/'content/modules/AF.Module.Prompt/ModuleData/PromptRetrievalCompatibility.json').read_text(encoding='utf-8'))
for a,b in [('RetryActionTagsMissing','Raw_2acff7afde1f'),('RetryPreprocessFormatPrefix','Raw_6a06fe656acf'),('RetryPreprocessFormatSuffix','Raw_9a8b413632be')]:
    assert llm[a]==retrieval[b],(a,b)
dotnet=resolve_dotnet(ROOT);results=[]
for api,dependency in [('1.3','_deps_auto'),('1.4','.tmp/build_check/1.4')]:
    out=OUT/api;out.mkdir(exist_ok=True);deps=ROOT/dependency
    (out/'Program.cs').write_text((HERE/'Program.cs.txt').read_text(encoding='utf-8'),encoding='utf-8')
    items=''.join('<Compile Include="'+str(ROOT/path)+'" />' for path in ['src/AF.GameAdapter.Bannerlord/UI/Errors/LlmRetryPrompt.cs','src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs'])
    items+='<EmbeddedResource Include="'+str(ROOT/'content/foundation/AF.Foundation.Localization/ModuleData/RuntimeStaleRequestCompatibility.txt')+'" LogicalName="AnimusForge.Foundation.StaleRequestError.txt" />'
    items+='<Reference Include="TaleWorlds.Core"><HintPath>'+str(deps/'TaleWorlds.Core.dll')+'</HintPath></Reference>'
    project=out/'Probe.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup><ItemGroup>'+items+'</ItemGroup></Project>',encoding='utf-8')
    add_retry_resources(ROOT,project,deps)
    (out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
    run=subprocess.run([str(dotnet),'run','--project',str(project),'-c','Release','--',str(catalog)],cwd=out,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90)
    log=run.stdout+run.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(api,log)
    results.append({'api':api,'exitCode':run.returncode,'TW_sha256':hashlib.sha256((deps/'TaleWorlds.Localization.dll').read_bytes()).hexdigest()})
(OUT/'receipt.json').write_text(json.dumps({'results':results,'wholeProductionSource':True,'uiEndpointCaptureOnly':True,'sourceSha256':hashlib.sha256((ROOT/'src/AF.GameAdapter.Bannerlord/UI/Errors/LlmRetryPrompt.cs').read_bytes()).hexdigest(),'nativeGameAcceptance':'NOT_RUN'},indent=2),encoding='utf-8')
raise SystemExit(0 if all(x['exitCode']==0 for x in results) else 1)
