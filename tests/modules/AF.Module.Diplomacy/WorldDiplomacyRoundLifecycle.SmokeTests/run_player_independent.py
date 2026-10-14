"""Compile current production diplomacy owners with campaign/transport fakes."""
from pathlib import Path
import argparse, hashlib, json, os, subprocess, sys
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

p = argparse.ArgumentParser()
p.add_argument('--run-root', type=Path)
p.add_argument('--language', choices=['cn', 'en'], default='en')
p.add_argument('--mode', default='--player-independent')
a = p.parse_args()
out = new_run_root(ROOT, 'player-independent', a.run_root)
dotnet = resolve_dotnet(ROOT)
original = ET.parse(HERE / 'WorldDiplomacyRoundLifecycle.SmokeTests.csproj').getroot()
sources = {(HERE / node.attrib['Include']).resolve() for node in original.findall('./ItemGroup/Compile') if 'Include' in node.attrib}
sources.update(path.resolve() for path in HERE.glob('*.cs') if path.name != 'PersistenceRecordStubs.cs')
project = ET.Element('Project', Sdk='Microsoft.NET.Sdk')
props = ET.SubElement(project, 'PropertyGroup')
for k, v in {'OutputType':'Exe','TargetFramework':'net8.0','ImplicitUsings':'enable','Nullable':'enable','EnableDefaultCompileItems':'false','WarningLevel':'0'}.items():
    ET.SubElement(props,k).text=v
items=ET.SubElement(project,'ItemGroup')
for source in sorted(sources): ET.SubElement(items,'Compile',Include=str(source))
newtonsoft_candidates=list((dotnet.parent/'sdk').glob('*/Newtonsoft.Json.dll'))
if not newtonsoft_candidates: raise SystemExit('SDK Newtonsoft reference unavailable')
refs={'Newtonsoft.Json':newtonsoft_candidates[-1]}
for reference in original.findall('./ItemGroup/Reference'):
    name=reference.attrib['Include']
    file=ROOT/'_deps_auto'/(name+'.dll')
    if not file.is_file(): raise SystemExit('Missing local reference '+str(file))
    refs[name]=file
for name,file in refs.items():
    reference=ET.SubElement(items,'Reference',Include=name)
    ET.SubElement(reference,'HintPath').text=str(file)
for resource in original.findall('./ItemGroup/EmbeddedResource'):
    attrs=dict(resource.attrib);attrs['Include']=str((HERE/attrs['Include']).resolve())
    ET.SubElement(items,'EmbeddedResource',**attrs)
ET.ElementTree(project).write(out/'Replay.csproj',encoding='utf-8',xml_declaration=True)
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
(out/'sources.json').write_text(json.dumps({str(s):hashlib.sha256(s.read_bytes()).hexdigest() for s in sorted(sources)},indent=2),encoding='utf-8')
package=out/'package';package.mkdir()
for file in (ROOT/'content/modules/AF.Module.Diplomacy/ModuleData').glob('*.json'):
    if file.name.endswith('.en.json'): continue
    en=file.with_name(file.stem+'.en.json')
    selected=en if a.language=='en' and en.is_file() else file
    (package/file.name).write_bytes(selected.read_bytes())
for name in ['PromptComposerTexts.json', 'PromptRuleBlockTexts.json']:
    candidates=list((ROOT/'content').glob('**/'+name)) or list((ROOT/'content').glob('**/'+name[:-5]+'.zh-CN.json'))
    if candidates:
        file=candidates[0];en=file.with_name(name[:-5]+'.en.json')
        (package/name).write_bytes((en if a.language=='en' and en.is_file() else file).read_bytes())
env=minimal_test_environment(dotnet,out)
env['AF_SMOKE_PACKAGE_ROOT']=str(package)
env['AF_SMOKE_PACKAGE_LANGUAGE']=a.language
env['AF_SMOKE_UI_LANGUAGE']='English'
env['AF_SMOKE_UI_MODULE']=str(ROOT/'content/modules/AF.Module.Diplomacy')
for command, log in [([str(dotnet),'build',str(out/'Replay.csproj'),'--nologo','-v:q','-p:RestoreConfigFile='+str(out/'NuGet.Config')],'build.log'),
    ([str(dotnet),str(out/'bin/Debug/net8.0/Replay.dll'),a.mode] if a.mode else [str(dotnet),str(out/'bin/Debug/net8.0/Replay.dll')],'run.log')]:
    result=subprocess.run(command,cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180)
    output=result.stdout+result.stderr;(out/log).write_text(output,encoding='utf-8')
    print(output[-12000:])
    if result.returncode: raise SystemExit(result.returncode)
print('OUTPUT',out)
