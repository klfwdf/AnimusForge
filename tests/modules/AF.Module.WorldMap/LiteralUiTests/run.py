from pathlib import Path
import sys,subprocess,json,shutil,xml.etree.ElementTree as ET,hashlib
R=Path(__file__).resolve().parents[4];H=Path(__file__).parent;sys.path.insert(0,str(R/'tests'))
from output_isolation import resolve_dotnet,minimal_test_environment
O=R/'artifacts/localization/verification-20261011/worldmap-literal-ui-run';O.mkdir(parents=True,exist_ok=True)
catalog=O/'catalog';entries=json.loads((R/'content/content-map.json').read_text(encoding='utf-8'))['entries']
for entry in entries:
 if entry['target'].startswith('ModuleData/Languages/') and entry['target'].endswith('.xml'):
  target=catalog/entry['target'];target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(R/entry['source'],target)
for language,directory,prefix in [('English','','EN'),('简体中文','CNs','中')]:
 folder=catalog/'ModuleData/Languages'/directory;index=ET.parse(folder/'language_data.xml');ET.SubElement(index.getroot(),'LanguageFile',{'xml_path':(directory+'/' if directory else '')+'probe.xml'});index.write(folder/'language_data.xml',encoding='utf-8',xml_declaration=True)
 base=ET.Element('base',{'type':'string'});tags=ET.SubElement(base,'tags');ET.SubElement(tags,'tag',{'language':language});strings=ET.SubElement(base,'strings')
 for key,value in [('AF_TEST_Literal',prefix+'<{NAME}>|{SECOND}|{NAME}'),('AF_TEST_Repeat',prefix+'<{NAME}>|{NAME}'),('AF_TEST_Embedded','INNER')]:ET.SubElement(strings,'string',{'id':key,'text':value})
 ET.ElementTree(base).write(folder/'probe.xml',encoding='utf-8',xml_declaration=True)
d=resolve_dotnet(R);results=[]
for api,dep in [('1.3','_deps_auto'),('1.4','.tmp/build_check/1.4')]:
 out=O/api;out.mkdir(exist_ok=True);(out/'Program.cs').write_text((H/'Program.cs.txt').read_text(encoding='utf-8'),encoding='utf-8');deps=R/dep
 project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup><ItemGroup><Compile Include="'+str(R/'src/modules/AF.Module.WorldMap/Runtime/WorldMapUiText.cs')+'"/>'+''.join('<Reference Include="'+name+'"><HintPath>'+str(deps/(name+'.dll'))+'</HintPath></Reference>' for name in ['TaleWorlds.Localization','TaleWorlds.Library'])+'</ItemGroup></Project>'
 (out/'Probe.csproj').write_text(project);(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 run=subprocess.run([str(d),'run','--project',str(out/'Probe.csproj'),'-c','Release','--',str(catalog)],cwd=out,env=minimal_test_environment(d,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90);(out/'run.log').write_text(run.stdout+run.stderr,encoding='utf-8');print(api,run.stdout+run.stderr);results.append({'api':api,'exit_code':run.returncode,'tw_sha256':hashlib.sha256((deps/'TaleWorlds.Localization.dll').read_bytes()).hexdigest()})
(O/'receipt.json').write_text(json.dumps({'results':results,'source_sha256':hashlib.sha256((R/'src/modules/AF.Module.WorldMap/Runtime/WorldMapUiText.cs').read_bytes()).hexdigest(),'actual_managed_TW':True,'native_game_UI':'NOT_RUN'},indent=2));raise SystemExit(0 if all(x['exit_code']==0 for x in results) else 1)
